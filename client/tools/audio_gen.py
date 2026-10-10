"""Banco de audio do Dungeon Courier (CONTRATO_V1 §12, docs/AUDIO.md): todo som e toda musica sintetizados aqui, sem asset de
terceiros nem servico externo. So numpy + scipy. Deterministico: cada som usa uma semente fixa (crc32 do nome).

Uso:  python client/tools/audio_gen.py          gera tudo e confere (pico, DC, laco)
      python client/tools/audio_gen.py --md     idem e imprime as linhas do PROVENIENCIA §4.1

Saida (client/Assets/_DC/Resources/Audio/):
  sfx/<nome>_<n>.wav    44,1 kHz mono 16 bit, silencio inicial cortado, loudness por categoria (LUFS momentaneo maximo, aprox.)
  musica/<faixa>.wav    22,05 kHz mono 16 bit, laco exato (as caudas dao a volta no buffer), -22 LUFS integrado (BS.1770)
  *.meta                import do Unity (SFX: Decompress On Load + Vorbis; musica: Streaming + Vorbis), criado so se faltar.
ponytail: musica em WAV e nao OGG (sem encoder Vorbis na stdlib/scipy); o Unity comprime em Vorbis no import.
"""
import hashlib
import sys
import wave
import zlib
from pathlib import Path

import numpy as np
from scipy import signal

OUT = Path(__file__).resolve().parents[1] / "Assets" / "_DC" / "Resources" / "Audio"
SR, MSR = 44100, 22050
PEAK_DB = -1.5          # teto de pico verdadeiro (4x), abaixo do -1 dBFS pedido
MUSIC_LUFS = -22.0


# ------------------------------------------------------------------ blocos de sintese

def midi(m):
    return 440.0 * 2.0 ** ((m - 69) / 12.0)


def n_of(d, sr=SR):
    return max(1, int(round(d * sr)))


def sweep(f0, f1, n):
    """Glide exponencial f0 -> f1 em n amostras."""
    return f0 * (f1 / f0) ** np.linspace(0.0, 1.0, n)


def ph(freq, sr=SR):
    return 2.0 * np.pi * np.cumsum(freq) / sr


def osc(freq, kind="sine", sr=SR, duty=0.5, hmax=40):
    """Oscilador limitado em banda (aditivo): sine, tri, square, saw, pulse. freq: escalar ou array por amostra."""
    freq = np.asarray(freq, float)
    p = ph(freq, sr)
    if kind == "sine":
        return np.sin(p)
    out = np.zeros_like(p)
    for k in range(1, hmax + 1):
        if kind in ("tri", "square") and k % 2 == 0:
            continue
        if kind == "tri":
            a, w = (8 / np.pi ** 2) * (-1) ** ((k - 1) // 2) / k ** 2, np.sin(k * p)
        elif kind == "square":
            a, w = (4 / np.pi) / k, np.sin(k * p)
        elif kind == "saw":
            a, w = (2 / np.pi) / k, np.sin(k * p)
        else:   # pulse sem DC
            a, w = (2 / np.pi) * np.sin(np.pi * k * duty) / k, np.cos(k * p)
        mask = k * freq < 0.45 * sr
        if not mask.any():
            break
        out += a * w * mask
    return out


def ad(n, a, tau, sr=SR):
    """Ataque linear de `a` s e queda exponencial com constante `tau` s."""
    t = np.arange(n) / sr
    return np.minimum(1.0, t / max(a, 1e-5)) * np.exp(-np.maximum(t - a, 0.0) / tau)


def adsr(gate, a, d, s, r, sr):
    ng = n_of(gate, sr)
    t = np.arange(ng) / sr
    e = np.where(t < a, t / max(a, 1e-5), s + (1 - s) * np.exp(-(t - a) / d))
    tail = e[-1] * np.exp(-np.arange(n_of(5 * r, sr)) / (r * sr))
    return np.concatenate([e, tail])


def _sos(kind, f, sr, order=2):
    return signal.butter(order, f, kind, fs=sr, output="sos")


def lp(x, f, sr=SR, order=2):
    return signal.sosfilt(_sos("lowpass", min(f, 0.45 * sr), sr, order), x)


def hp(x, f, sr=SR, order=2):
    return signal.sosfilt(_sos("highpass", f, sr, order), x)


def bp(x, lo, hi, sr=SR, order=2):
    return signal.sosfilt(_sos("bandpass", [lo, min(hi, 0.45 * sr)], sr, order), x)


def svf(x, fc, q=0.7, mode="bp", sr=SR):
    """Filtro de estado variavel (Chamberlin) com corte variando por amostra (varreduras de vento, limo, explosao)."""
    fc = np.broadcast_to(np.asarray(fc, float), x.shape)
    f = (2.0 * np.sin(np.pi * np.minimum(fc, sr / 6.5) / sr)).tolist()
    damp, low, band, out = 1.0 / q, 0.0, 0.0, []
    for xi, fi in zip(x.tolist(), f):
        high = xi - low - damp * band
        band += fi * high
        low += fi * band
        out.append(band if mode == "bp" else low)
    return np.array(out)


def modes(n, freqs, taus, amps=None, sr=SR):
    """Soma de senoides amortecidas: madeira, metal, sino, moeda."""
    t = np.arange(n) / sr
    amps = amps or [1.0] * len(freqs)
    return sum(a * np.sin(2 * np.pi * f * t) * np.exp(-t / tau) for f, tau, a in zip(freqs, taus, amps))


def pluck(f, dur, rng, damp=0.996, sr=SR):
    """Karplus-Strong: elastico do estilingue, corda do arco."""
    n, period = n_of(dur, sr), max(2, int(sr / f))
    buf, out, j = rng.uniform(-1, 1, period).tolist(), [], 0
    for _ in range(n):
        v = buf[j]
        out.append(v)
        buf[j] = damp * 0.5 * (v + buf[(j + 1) % period])
        j = (j + 1) % period
    return np.array(out)


def mix(*xs):
    out = np.zeros(max(len(x) for x in xs))
    for x in xs:
        out[:len(x)] += x
    return out


def place(buf, x, at):
    at = int(at)
    m = min(len(x), len(buf) - at)
    if m > 0:
        buf[at:at + m] += x[:m]


def ticks(rate, n, rng, sr=SR):
    """Trem de impulsos com taxa variavel (atrito de rangido: porta, arco)."""
    p = np.cumsum(np.broadcast_to(rate, (n,))) / sr
    return (np.diff(np.floor(p), prepend=0.0) > 0) * rng.uniform(0.5, 1.0, n)


def bell(f, dur, sr=SR):
    n = n_of(dur, sr)
    return modes(n, [f, 2 * f, 3.01 * f, 5.4 * f], [0.5 * dur, 0.22 * dur, 0.1 * dur, 0.03], [1, 0.3, 0.1, 0.08], sr) \
        * np.minimum(1, np.arange(n) / (0.002 * sr))


def notes(seq, sr, voice, gain=1.0):
    """[(t, midi, dur)] -> buffer com a voz `voice(f, dur)` em cada nota."""
    end = max(t + d for t, _, d in seq) + 1.5
    buf = np.zeros(n_of(end, sr))
    for t, m, d in seq:
        place(buf, voice(midi(m), d) * gain, t * sr)
    return buf


P3 = (1.0, 0.91, 1.1)   # fator de altura por variacao


# ------------------------------------------------------------------ SFX (um por momento do GDD §12 / CONTRATO_V1 §3-§7)

def s_passo(rng, v):
    p, n = P3[v], n_of(0.09)
    x = lp(rng.standard_normal(n), 700 * p) * ad(n, 0.002, 0.018)
    x += 0.5 * np.sin(ph(sweep(160 * p, 90 * p, n))) * ad(n, 0.001, 0.025)
    return x + 0.15 * hp(rng.standard_normal(n), 3000) * ad(n, 0.0005, 0.004)


def s_pousar(rng, v):
    p, n = (1.0, 0.9)[v], n_of(0.25)
    x = np.sin(ph(sweep(170 * p, 60 * p, n))) * ad(n, 0.002, 0.06)
    x += 0.6 * lp(rng.standard_normal(n), 900 * p) * ad(n, 0.001, 0.03)
    return x + 0.25 * modes(n, [310 * p, 720 * p, 1130 * p], [0.05, 0.03, 0.02])


def s_pegar(rng, v):
    p, n = (1.0, 1.12)[v], n_of(0.14)
    x = osc(sweep(420 * p, 860 * p, n), "pulse", duty=0.3, hmax=12) * ad(n, 0.003, 0.05)
    return x + 0.3 * modes(n, [1720 * p], [0.04])


def s_carga(rng, v):
    p, n = P3[v], n_of(0.26)
    x = modes(n, [220 * p, 485 * p, 960 * p, 1530 * p], [0.06, 0.04, 0.025, 0.015], [1, 0.7, 0.5, 0.3])
    x += 0.7 * bp(rng.standard_normal(n), 400, 3000) * ad(n, 0.0005, 0.012)
    return x + 0.5 * np.sin(ph(sweep(140 * p, 70 * p, n))) * ad(n, 0.001, 0.04)


def s_racha(rng, v):
    p, n = (1.0, 0.88)[v], n_of(0.4)
    x = np.zeros(n)
    m = n_of(0.006)
    for _ in range(int(rng.integers(5, 8))):
        burst = hp(rng.standard_normal(m), 2500) * np.exp(-np.arange(m) / (0.0012 * SR)) * rng.uniform(0.4, 1.0)
        place(x, burst, rng.uniform(0, 0.09) * SR)
    x += 0.35 * modes(n, [2350 * p, 3720 * p, 5150 * p], [0.12, 0.08, 0.05], [1, 0.6, 0.4])
    return x + 0.4 * lp(rng.standard_normal(n), 1200) * ad(n, 0.0005, 0.01)


def s_quebra(rng, v):
    n = n_of(0.9)
    x = np.sin(ph(sweep(150, 50, n))) * ad(n, 0.001, 0.07)
    x += 0.8 * bp(rng.standard_normal(n), 300, 4000) * ad(n, 0.0005, 0.04)
    for _ in range(18):
        f = rng.uniform(2200, 6000)
        place(x, modes(n_of(0.12), [f, f * 1.47], [0.03, 0.02]) * rng.uniform(0.15, 0.5), rng.uniform(0, 0.5) ** 1.6 * SR)
    return x


def s_ladrao(rng, v):
    p, n = (1.0, 1.08)[v], n_of(0.4)
    t = np.arange(n) / SR
    whoosh = svf(rng.standard_normal(n), sweep(600, 3000, n), q=2) * ad(n, 0.05, 0.08)
    f = (700 + 500 * np.sin(np.pi * np.minimum(t / 0.3, 1))) * p * (1 + 0.03 * np.sin(2 * np.pi * 9 * t))
    return 0.6 * whoosh + 0.7 * osc(f, "tri", hmax=9) * ad(n, 0.02, 0.15)


def s_dash(rng, v):
    p, n = (1.0, 1.1)[v], n_of(0.26)
    u = np.linspace(0, 1, n)
    return svf(rng.standard_normal(n), (500 + 2200 * np.sin(np.pi * u ** 0.7)) * p, q=1.5) * ad(n, 0.04, 0.07)


def s_golpe(rng, v):
    p, n = P3[v], n_of(0.16)
    x = np.sin(ph(sweep(220 * p, 70 * p, n))) * ad(n, 0.0008, 0.035)
    x += 0.8 * bp(rng.standard_normal(n), 800, 5000) * ad(n, 0.0003, 0.012)
    x += 0.35 * osc(sweep(900 * p, 500 * p, n), "square", hmax=8) * ad(n, 0.0005, 0.02)
    return np.tanh(2.0 * x)


def s_tiro(rng, v):
    p, n = P3[v], n_of(0.12)
    x = np.sin(ph(sweep(500 * p, 1800 * p, n))) * ad(n, 0.001, 0.025)
    x += 0.5 * pluck(330 * p, 0.12, rng) * ad(n, 0.0005, 0.04)
    return x + 0.3 * hp(rng.standard_normal(n), 2000) * ad(n, 0.0003, 0.006)


def s_flecha(rng, v):
    p, n = (1.0, 1.08)[v], n_of(0.3)
    x = pluck(196 * p, 0.3, rng, damp=0.995) * ad(n, 0.0005, 0.09)
    return x + 0.5 * svf(rng.standard_normal(n), sweep(5000, 1500, n), q=3) * ad(n, 0.02, 0.06)


def s_mira(rng, v):
    n = n_of(0.75)
    u = np.linspace(0, 1, n)
    creak = bp(ticks(25 + 35 * u, n, rng), 700, 2600)
    tone = osc(sweep(400, 900, n), "tri", hmax=9) * 0.25
    x = (creak + tone) * (0.3 + 0.7 * u)
    x[-n_of(0.02):] *= np.linspace(1, 0, n_of(0.02))
    return x


def s_bumerangue(rng, v):
    n = n_of(0.6)
    u = np.linspace(0, 1, n)
    am = 0.55 + 0.45 * np.sin(ph(14 + 6 * np.sin(np.pi * u)))
    return svf(rng.standard_normal(n), 900 + 900 * np.sin(np.pi * u), q=4) * am * np.sin(np.pi * u) ** 0.7


def s_bomba(rng, v):
    p, n = (1.0, 0.92)[v], n_of(0.45)
    m = n_of(0.06)
    x = np.zeros(n)
    place(x, np.sin(ph(sweep(300 * p, 900 * p, m))) * ad(m, 0.002, 0.02), 0)
    flick = np.clip(lp(rng.standard_normal(n), 40) * 3 + 0.5, 0, 1)
    return x + 0.35 * hp(rng.standard_normal(n), 3500) * flick * ad(n, 0.03, 0.3)


def s_explosao(rng, v):
    p, n = (1.0, 0.9)[v], n_of(1.1)
    u = np.arange(n) / n
    body = svf(rng.standard_normal(n), (2500 * np.exp(-u * 5) + 180) * p, q=0.8, mode="lp") * ad(n, 0.002, 0.25)
    sub = np.sin(ph(sweep(140 * p, 45 * p, n))) * ad(n, 0.003, 0.15)   # o celular nao toca sub: o peso vem do medio
    mid = bp(rng.standard_normal(n), 200, 1200) * ad(n, 0.002, 0.12)
    crack = hp(rng.standard_normal(n), 1500) * ad(n, 0.0003, 0.015)
    x = np.tanh(1.6 * (body + 0.5 * sub + 0.8 * mid + crack))
    for _ in range(14):   # destrocos
        f = rng.uniform(900, 3500)
        place(x, modes(n_of(0.05), [f], [0.01]) * rng.uniform(0.05, 0.2), rng.uniform(0.15, 0.8) * SR)
    return x


def s_morte(rng, v):
    p, n = P3[v], n_of(0.3)
    x = 0.6 * osc(sweep(620 * p, 140 * p, n), "square", hmax=10) * ad(n, 0.002, 0.09)
    return x + 0.6 * lp(rng.standard_normal(n), 1800) * ad(n, 0.01, 0.07)


def s_dano(rng, v):
    p, n = (1.0, 1.1)[v], n_of(0.22)
    u = np.linspace(0, 1, n)
    src = osc((260 - 80 * u) * p, "saw", hmax=40)
    x = (bp(src, 350, 700) + 0.5 * bp(src, 800, 1200)) * ad(n, 0.008, 0.08)
    return x + 0.4 * lp(rng.standard_normal(n), 1000) * ad(n, 0.0005, 0.01)


def s_aviso(rng, v):
    n, m = n_of(0.2), n_of(0.07)
    x = np.zeros(n)
    for k in range(2):
        place(x, osc(988.0 * np.ones(m), "tri", hmax=9) * ad(m, 0.002, 0.025), k * 0.08 * SR)
    return x


def s_porta(rng, v):
    p, n = (1.0, 0.9)[v], n_of(0.55)
    u = np.linspace(0, 1, n)
    creak = bp(ticks((35 + 25 * np.sin(np.pi * u * 1.3)) * p, n, rng), 600 * p, 2200 * p) * ad(n, 0.05, 0.25)
    m = n_of(0.15)
    thunk = modes(m, [180 * p, 410 * p], [0.05, 0.03]) + 0.5 * lp(rng.standard_normal(m), 700) * ad(m, 0.001, 0.02)
    place(creak, 0.8 * thunk, 0.38 * SR)
    place(creak, 0.5 * modes(n_of(0.05), [1200 * p, 2600 * p], [0.01, 0.006]), 0)   # trinco: resposta ja no toque
    return creak


def s_bloqueio(rng, v):
    p, n = (1.0, 0.93)[v], n_of(0.35)
    x = np.zeros(n)
    for k in range(3):
        knock = modes(n_of(0.12), [820 * p * (1 + 0.03 * k), 1370 * p, 2240 * p], [0.03, 0.02, 0.012], [1, 0.6, 0.4])
        place(x, knock * (1 - 0.2 * k), k * 0.055 * SR)
    return x + 0.6 * np.sin(ph(sweep(140, 90, n))) * ad(n, 0.002, 0.05)


def s_bau(rng, v):
    n = n_of(0.95)
    x = np.zeros(n)
    m = n_of(0.2)
    place(x, modes(m, [380, 900], [0.03, 0.02]), 0)   # trinco abrindo
    place(x, bp(ticks(45, m, rng), 500, 1800) * ad(m, 0.03, 0.08), 0.02 * SR)
    for k, nota in enumerate((84, 88, 91, 96)):
        place(x, 0.6 * bell(midi(nota), 0.45), (0.12 + 0.06 * k) * SR)
    return x


def s_moedas(rng, v):
    p, n = P3[v], n_of(0.42)
    x, at = np.zeros(n), 0.0
    for k in range(3):
        f = rng.uniform(1900, 2400) * p
        place(x, modes(n_of(0.25), [f, f * 2.32, f * 3.89, f * 5.4], [0.09, 0.05, 0.03, 0.02], [1, 0.6, 0.4, 0.2]) * (1 - 0.25 * k), at)
        at += rng.uniform(0.035, 0.07) * SR
    return x


def lead(f, d, sr=SR):
    """Pulso 25% suave com vibrato atrasado (fanfarras e musica)."""
    e = adsr(d * 0.9, 0.005, 0.12, 0.6, 0.05, sr)
    t = np.arange(len(e)) / sr
    vib = 1 + 0.005 * np.sin(2 * np.pi * 5.5 * t) * np.clip((t - 0.18) / 0.2, 0, 1)
    return lp(osc(f * vib, "pulse", sr, duty=0.25, hmax=14), 3500, sr) * e


def tri_bass(f, d, sr=SR):
    e = adsr(d * 0.9, 0.004, 0.3, 0.0, 0.03, sr)
    return osc(np.full(len(e), f), "tri", sr, hmax=9) * e


def s_entrega(rng, v):
    melo = [(0.0, 67, 0.09), (0.09, 72, 0.09), (0.18, 76, 0.09), (0.27, 79, 0.09), (0.40, 84, 0.6)]
    return mix(notes(melo, SR, lead), notes([(0.0, 48, 0.3), (0.40, 48, 0.6)], SR, tri_bass, 0.5),
               notes([(0.40, 76, 0.6), (0.40, 79, 0.6)], SR, lead, 0.25))


def s_entrega_rachada(rng, v):
    x = notes([(0.0, 67, 0.09), (0.09, 72, 0.09), (0.18, 76, 0.09)], SR, lead)
    e = adsr(0.55, 0.005, 0.15, 0.7, 0.08, SR)
    t = np.arange(len(e)) / SR
    f = midi(75) * 2 ** (-np.clip((t - 0.15) / 0.5, 0, 1) / 12) * (1 + 0.012 * np.sin(2 * np.pi * 6 * t))   # cai 1 semitom e treme
    place(x, lp(osc(f, "pulse", duty=0.25, hmax=14), 3000) * e, 0.30 * SR)
    return mix(x, notes([(0.0, 48, 0.25), (0.30, 47, 0.6)], SR, tri_bass, 0.5))


def s_falha(rng, v):
    """Tres notas caindo e um suspiro final deslizando (trombone sintetizado)."""
    n = n_of(1.7)
    x = np.zeros(n)
    for k, m in enumerate((64, 63, 62)):
        e = adsr(0.24, 0.02, 0.1, 0.8, 0.04, SR)
        tone = osc(np.full(len(e), midi(m)), "saw", hmax=18) * e
        place(x, lp(tone, 900 + 600 * (k == 0)), k * 0.3 * SR)
    e = adsr(0.7, 0.02, 0.2, 0.8, 0.06, SR)
    t = np.arange(len(e)) / SR
    f = midi(61) * 2 ** (-4 * np.clip(t / 0.7, 0, 1) / 12) * (1 + 0.02 * np.sin(2 * np.pi * 5 * t) * (t > 0.15))
    place(x, lp(osc(f, "saw", hmax=18), 900) * e, 0.9 * SR)
    return x


def s_perk(rng, v):
    seq = [(0.06 * k, m, 0.35) for k, m in enumerate((81, 85, 88, 92, 95))]
    x = mix(notes(seq, SR, lambda f, d: bell(f, 0.6), 0.6), notes(seq, SR, lambda f, d: bell(f * 1.006, 0.5), 0.18))
    n = len(x)
    return x + 0.08 * hp(rng.standard_normal(n), 6000) * ad(n, 0.1, 0.25)


def s_clique(rng, v):
    f, n = (1500, 1250)[v], n_of(0.05)
    return np.sin(ph(sweep(f, f * 0.7, n))) * ad(n, 0.0005, 0.008) + 0.3 * hp(rng.standard_normal(n), 3000) * ad(n, 0.0002, 0.002)


def s_confirmar(rng, v):
    m = n_of(0.12)
    x = np.zeros(n_of(0.26))
    for k, nota in enumerate((88, 95)):
        place(x, osc(np.full(m, midi(nota)), "pulse", duty=0.4, hmax=8) * ad(m, 0.002, 0.05), k * 0.07 * SR)
    return x


def s_estrela(rng, v):
    n = n_of(0.65)
    x = bell(1568.0, 0.65)[:n]
    t = np.arange(n) / SR
    return x * (1 + 0.25 * np.sin(2 * np.pi * 18 * t)) + 0.05 * hp(rng.standard_normal(n), 7000) * ad(n, 0.005, 0.1)


def s_choca(rng, v):
    x = np.zeros(n_of(0.75))
    for k in range(2):
        place(x, 0.6 * s_racha(rng, 1)[:n_of(0.12)], k * 0.1 * SR)
    m = n_of(0.1)
    t = np.arange(m) / SR
    for k in range(2):
        place(x, np.sin(ph(sweep(2500, 3300, m) * (1 + 0.04 * np.sin(2 * np.pi * 40 * t)))) * ad(m, 0.005, 0.04), (0.33 + 0.16 * k) * SR)
    return x


def s_limo(rng, v):
    p, n = P3[v], n_of(0.3)
    u = np.linspace(0, 1, n)
    x = svf(rng.standard_normal(n), (250 + 700 * np.sin(np.pi * u)) * p, q=6) * ad(n, 0.01, 0.08)
    return x + 0.5 * np.sin(ph(sweep(180 * p, 420 * p, n))) * ad(n, 0.005, 0.05)


def s_rugido(rng, v):
    n = n_of(1.3)
    u = np.linspace(0, 1, n)
    f0 = (85 + 30 * np.sin(np.pi * np.minimum(u * 1.5, 1))) * (1 + 0.6 * lp(rng.standard_normal(n), 20))
    src = osc(f0, "saw", hmax=60) + 0.4 * rng.standard_normal(n)
    am = 1 + 0.35 * np.sin(ph(np.full(n, 23.0)))
    env = np.minimum(1, u / 0.1) * np.minimum(1, (1 - u) / 0.45)
    return np.tanh(1.5 * (bp(src, 250, 650) + 0.6 * bp(src, 900, 1600) + 0.5 * lp(src, 200)) * am * env)


def s_pancada(rng, v):
    p, n = (1.0, 0.9)[v], n_of(0.9)
    noise = rng.standard_normal(n)
    sub = np.sin(ph(sweep(90 * p, 32 * p, n))) * ad(n, 0.002, 0.15)
    body = lp(noise, 1200) * ad(n, 0.001, 0.08)
    crunch = bp(noise, 250 * p, 900 * p) * ad(n, 0.001, 0.05)
    x = np.tanh(1.8 * (0.6 * sub + 0.8 * body + crunch))
    for _ in range(10):
        place(x, bp(rng.standard_normal(n_of(0.02)), 1000, 4000) * rng.uniform(0.05, 0.15), rng.uniform(0.1, 0.6) * SR)
    return x


def s_compra(rng, v):
    x = np.zeros(n_of(0.7))
    place(x, modes(n_of(0.06), [1800, 3900], [0.008, 0.005]), 0)
    for k, nota in enumerate((88, 95)):
        place(x, 0.6 * bell(midi(nota), 0.5), (0.05 + 0.08 * k) * SR)
    place(x, 0.5 * s_moedas(rng, 0)[:n_of(0.4)], 0.1 * SR)
    return x


def s_tique(rng, v):
    p, n = (1.0, 0.8)[v], n_of(0.08)
    return modes(n, [1700 * p, 4200 * p], [0.015, 0.006], [1, 0.4]) * np.minimum(1, np.arange(n) / 20)


def s_segredo(rng, v):
    seq = [(0.07 * k, m, 0.4) for k, m in enumerate((72, 74, 76, 78, 80, 82, 84))]
    x = notes(seq, SR, lambda f, d: bell(f, 0.7), 0.5)
    d = n_of(0.18)   # eco
    y = x.copy()
    place(y, 0.35 * x, d)
    place(y, 0.12 * x, 2 * d)
    return y


def s_balanca(rng, v):
    n = n_of(0.45)
    t = np.arange(n) / SR
    f = 330 * (1 + 0.15 * np.exp(-t / 0.15) * np.sin(2 * np.pi * 11 * t))
    return (np.sin(ph(f)) + 0.3 * osc(f, "tri", hmax=7)) * ad(n, 0.005, 0.15)


# nome -> (receita, variacoes, LUFS alvo aprox.). Passo/clique mais baixos (frequentes); eventos grandes mais altos.
SFX = {
    "passo": (s_passo, 3, -26), "pousar": (s_pousar, 2, -18), "pegar": (s_pegar, 2, -18), "carga": (s_carga, 3, -16),
    "racha": (s_racha, 2, -16), "quebra": (s_quebra, 1, -14), "ladrao": (s_ladrao, 2, -17), "dash": (s_dash, 2, -19),
    "golpe": (s_golpe, 3, -17), "tiro": (s_tiro, 3, -21), "flecha": (s_flecha, 2, -18), "mira": (s_mira, 1, -19),
    "bumerangue": (s_bumerangue, 1, -20), "bomba": (s_bomba, 2, -19), "explosao": (s_explosao, 2, -14),
    "morte": (s_morte, 3, -17), "dano": (s_dano, 2, -15), "aviso": (s_aviso, 1, -18), "porta": (s_porta, 2, -19),
    "bloqueio": (s_bloqueio, 2, -18), "bau": (s_bau, 1, -16), "moedas": (s_moedas, 3, -18), "entrega": (s_entrega, 1, -15),
    "entrega_rachada": (s_entrega_rachada, 1, -16), "falha": (s_falha, 1, -16), "perk": (s_perk, 1, -16),
    "clique": (s_clique, 2, -22), "confirmar": (s_confirmar, 1, -19), "estrela": (s_estrela, 1, -17), "choca": (s_choca, 1, -17),
    "limo": (s_limo, 3, -19), "rugido": (s_rugido, 1, -14), "pancada": (s_pancada, 2, -14), "compra": (s_compra, 1, -16),
    "tique": (s_tique, 2, -20), "segredo": (s_segredo, 1, -17), "balanca": (s_balanca, 1, -18),
}


# ------------------------------------------------------------------ musica

NOTE = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
QUAL = {"": (0, 4, 7), "m": (0, 3, 7), "7": (0, 4, 7, 10)}


def m_of(s):
    k, i = NOTE[s[0]], 1
    while s[i] in "#b":
        k += 1 if s[i] == "#" else -1
        i += 1
    return k + 12 * (int(s[i:]) + 1)


def melody(text, start=0.0):
    """'E5:2 C5:1 r:1' -> [(beat, midi, beats)], fim."""
    out, b = [], start
    for tok in text.split():
        name, d = tok.split(":")
        if name != "r":
            out.append((b, m_of(name), float(d)))
        b += float(d)
    return out, b


def chord(name):
    """'Bb' -> (classe de altura da raiz, intervalos)."""
    k, i = NOTE[name[0]], 1
    if len(name) > 1 and name[1] in "#b":
        k += 1 if name[1] == "#" else -1
        i = 2
    return k % 12, QUAL[name[i:]]


class Song:
    def __init__(self, bpm, beats, seed):
        self.spb = 60.0 / bpm
        self.L = int(round(beats * self.spb * MSR))
        self.tracks = {}
        self.rng = np.random.default_rng(seed)

    def add(self, track, beat, x, gain=1.0):
        buf = self.tracks.setdefault(track, np.zeros(self.L + n_of(4.0, MSR)))
        place(buf, x * gain * self.rng.uniform(0.9, 1.0), int(round(beat * self.spb * MSR)))

    def line(self, track, seq, voice, gain=1.0, oct_shift=0):
        for b, m, d in seq:
            self.add(track, b, voice(midi(m + 12 * oct_shift), d * self.spb), gain)

    def loop(self, track):
        """Buffer em laco: o que passa do fim (caudas) soma no comeco, como no laco real."""
        buf = self.tracks[track]
        x = buf[:self.L].copy()
        tail = buf[self.L:]
        x[:len(tail)] += tail
        return x


def roll_echo(x, delay, gains):
    """Eco circular (np.roll): o laco continua sem emenda."""
    y = x.copy()
    for k, g in enumerate(gains, 1):
        y += g * np.roll(x, k * delay)
    return y


def room(x):
    """Sala de pedra curta: taps circulares de um sinal passa-baixas (filtrado em 3 voltas para nao ter emenda)."""
    L = len(x)
    w = lp(np.tile(x, 3), 2500, MSR)[L:2 * L]
    y = x.copy()
    for d, g in zip((0.031, 0.047, 0.067, 0.089, 0.113, 0.149), (0.16, 0.14, 0.11, 0.09, 0.07, 0.05)):
        y += g * np.roll(w, n_of(d, MSR))
    return y


# vozes da musica (MSR)
def v_box(f, d):
    return bell(f, 1.6, MSR)


def v_flute(f, d):
    e = adsr(d * 0.92, 0.04, 0.2, 0.8, 0.08, MSR)
    t = np.arange(len(e)) / MSR
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5 * t) * np.clip((t - 0.15) / 0.25, 0, 1)
    return osc(f * vib, "tri", MSR, hmax=8) * e


def v_lead(f, d):
    return lead(f, d, MSR)


def v_brass(f, d):
    e = adsr(d * 0.88, 0.02, 0.15, 0.7, 0.06, MSR)
    return np.tanh(1.5 * lp(osc(np.full(len(e), f), "saw", MSR, hmax=16), 2400, MSR) * e)


def v_tri_bass(f, d):
    return tri_bass(f, d, MSR)


def v_pulse_bass(f, d):
    e = adsr(min(d * 0.7, 0.2), 0.003, 0.08, 0.5, 0.03, MSR)
    return lp(osc(np.full(len(e), f), "pulse", MSR, duty=0.5, hmax=10), 1200, MSR) * e


def v_stab(f, d=0):
    n = n_of(0.25, MSR)
    return osc(np.full(n, f), "square", MSR, hmax=8) * ad(n, 0.003, 0.07, MSR)


def v_arp(f, d=0):
    n = n_of(0.3, MSR)
    return osc(np.full(n, f), "pulse", MSR, duty=0.35, hmax=6) * ad(n, 0.002, 0.08, MSR)


def drums(rng):
    n = n_of(0.35, MSR)
    t = np.arange(n) / MSR
    kick = np.sin(ph(sweep(150, 45, n), MSR)) * ad(n, 0.001, 0.11, MSR) + 0.3 * hp(rng.standard_normal(n), 2000, MSR) * ad(n, 0.0002, 0.002, MSR)
    snare = bp(rng.standard_normal(n), 1500, 6000, MSR) * ad(n, 0.001, 0.07, MSR) + 0.5 * np.sin(2 * np.pi * 185 * t) * ad(n, 0.001, 0.05, MSR)
    hats = [hp(rng.standard_normal(n), 6000, MSR) * ad(n, 0.0005, 0.018, MSR) for _ in range(4)]
    rim = modes(n, [1700, 3100], [0.012, 0.008], sr=MSR)
    shaker = bp(rng.standard_normal(n), 3000, 8000, MSR) * ad(n, 0.015, 0.03, MSR)
    tom = lambda f0: np.sin(ph(sweep(f0, f0 * 0.62, n), MSR)) * ad(n, 0.001, 0.14, MSR) + 0.2 * lp(rng.standard_normal(n), 1500, MSR) * ad(n, 0.0005, 0.02, MSR)
    return {"kick": kick, "snare": snare, "hats": hats, "rim": rim, "shaker": shaker, "tom_hi": tom(200), "tom_lo": tom(130)}


def song_menu():
    """Valsa de caixinha de musica, Lá menor, 96 bpm, 3/4, 32 compassos = 60 s."""
    s = Song(96, 96, 101)
    dr = drums(s.rng)
    a = "E5:2 C5:1 A4:2 C5:1 G4:1 E5:1 D5:1 D5:2 B4:1 C5:1 E5:1 A5:1 A5:2 G5:0.5 F5:0.5 E5:1 D5:1 B4:1 G#4:3"
    b = "A4:1 C5:1 E5:1 F5:2 E5:0.5 D5:0.5 D5:1 B4:1 G4:1 E5:2 C5:1 C5:1 F5:1 A5:1 G5:1 F5:1 D5:1 E5:1 D5:1 B4:1 A4:3"
    chords = "Am F C G Am F E E7 Am Dm G C F Dm E Am".split() * 2
    mel, _ = melody(a + " " + b + " " + a + " " + b)
    for b0, m, d in mel:
        if b0 < 48:
            s.add("box", b0, v_box(midi(m), d), 0.55)
        else:
            s.add("lead", b0, v_flute(midi(m), d * s.spb), 0.5)
            s.add("box", b0, v_box(midi(m + 12), d), 0.12)
    for bar, c in enumerate(chords):
        root, iv = chord(c)
        b0 = bar * 3
        bass = 45 + (root - 9) % 12 - (12 if (root - 9) % 12 > 6 else 0)   # perto de A2
        s.add("bass", b0, v_tri_bass(midi(bass + (7 if bar >= 16 and bar % 2 else 0)), 1.0 * s.spb), 0.75)
        for k in (1, 2):   # "pa-pa" da valsa
            for i in iv[:3]:
                s.add("stab", b0 + k, v_stab(midi(60 + (root + i - 0) % 12), 0), 0.06)
        if bar >= 16:
            tones = [60 + 12 + (root + i) % 12 for i in iv[:3]]
            for k, m in enumerate(tones + [tones[0] + 12] + tones[2:0:-1]):
                s.add("arp", b0 + 0.5 * k, v_arp(midi(m), 0), 0.07)
            for k in range(3):
                s.add("drums", b0 + k, dr["shaker"], 0.12 if k else 0.2)
    x = s.loop("box") + roll_echo(s.loop("lead"), n_n(s, 0.75), (0.25, 0.08)) + s.loop("bass") + s.loop("stab") + s.loop("arp") + s.loop("drums")
    return room(x)


def n_n(s, beats):
    return int(round(beats * s.spb * MSR))


def song_masmorra():
    """Ré menor furtivo com pulso leve, 112 bpm, 4/4, 32 compassos = 68,6 s."""
    s = Song(112, 128, 202)
    dr = drums(s.rng)
    a = ("D5:0.5 r:0.5 F5:0.5 r:0.5 A5:0.5 r:0.5 G5:0.5 F5:0.5 E5:0.5 r:0.5 D5:0.5 r:0.5 A4:2 "
         "D5:0.5 r:0.5 F5:0.5 r:0.5 Bb5:0.5 r:0.5 A5:0.5 G5:0.5 A5:1.5 G5:0.5 F5:0.5 E5:0.5 C#5:1 "
         "D5:0.5 r:0.5 F5:0.5 r:0.5 A5:0.5 r:0.5 D6:1 C6:0.5 A5:0.5 r:0.5 F5:0.5 r:0.5 D5:1.5 "
         "Bb5:0.5 r:0.5 A5:0.5 r:0.5 G5:0.5 r:0.5 F5:0.5 E5:0.5 E5:1 C#5:1 A4:2")
    b = "Bb5:2 A5:1 G5:1 F5:3 A5:1 D6:2 C6:1 Bb5:1 A5:4 G5:1 Bb5:1 D6:1 Bb5:1 A5:2 F5:2 G5:1 F5:1 D5:1 F5:1 E5:2 C#5:1 E5:1"
    ca = "Dm Dm Bb A Dm Dm Gm A".split()
    cb = "Gm Dm Bb A Gm Dm Bb A".split()
    sections = [("A1", a, ca), ("A2", a, ca), ("B", b, cb), ("A3", a, ca)]
    for si, (name, text, chords) in enumerate(sections):
        b0 = si * 32
        mel, _ = melody(text, b0)
        if name == "B":
            s.line("lead", mel, v_flute, 0.55)
        else:
            s.line("lead", mel, v_lead, 0.42)
            if name == "A3":
                s.line("lead", mel, v_lead, 0.16, oct_shift=-1)
        for bar, c in enumerate(chords):
            root, iv = chord(c)
            bb = b0 + bar * 4
            r2 = 38 + (root - 2) % 12 - (12 if (root - 2) % 12 > 7 else 0)   # perto de D2
            pat = (0, 7, 12, 7) if name == "A1" else (0, 12, 7, 12, 0, 12, 7, 12)
            step = 4 / len(pat)
            for k, iv_ in enumerate(pat):
                s.add("bass", bb + k * step, v_pulse_bass(midi(r2 + iv_), step * s.spb), 0.5)
            if name in ("B", "A3"):
                tones = [62 + (root + i - 2) % 12 for i in iv[:3]]
                for k in range(8):
                    s.add("arp", bb + 0.5 * k, v_arp(midi((tones + [tones[0] + 12])[k % 4] + 12)), 0.06)
            for k in range(8):   # chimbal nos contratempos, mais forte a partir do A2
                if k % 2 or name != "A1":
                    s.add("drums", bb + 0.5 * k, dr["hats"][k % 4], (0.10 if k % 2 else 0.05) * (0.7 if name == "A1" else 1))
            s.add("drums", bb + 3, dr["rim"] if name == "A1" else dr["snare"], 0.18 if name == "A1" else 0.22)
            if name != "A1":
                s.add("drums", bb, dr["kick"], 0.5)
                s.add("drums", bb + 2, dr["kick"], 0.42)
                if name != "A2":
                    s.add("drums", bb + 1, dr["snare"], 0.16)
            if name == "A3" and bar == 7:   # virada de volta ao comeco
                for k, tom in enumerate(("tom_hi", "tom_hi", "tom_lo", "tom_lo")):
                    s.add("drums", bb + 2 + 0.5 * k, dr[tom], 0.3)
    x = roll_echo(s.loop("lead"), n_n(s, 0.75), (0.22, 0.06)) + s.loop("bass") + s.loop("arp") + s.loop("drums")
    return room(x)


def song_chefe():
    """Mi frígio tenso (marcha de ogro), 140 bpm, 4/4, 32 compassos = 54,9 s."""
    s = Song(140, 128, 303)
    dr = drums(s.rng)
    a = ("E5:0.5 r:0.5 E5:0.5 F5:0.5 G5:1 B5:1 A5:0.5 G5:0.5 F5:0.5 E5:0.5 F5:2 "
         "F5:0.5 r:0.5 F5:0.5 G5:0.5 A5:1 C6:1 B5:1.5 A5:0.5 G5:1 E5:1 "
         "E6:1 D6:0.5 C6:0.5 B5:1 G5:1 A5:0.5 B5:0.5 C6:1 B5:2 D6:1 B5:1 C6:1 A5:1 G#5:1 F5:1 E5:2")
    b = "C6:2 B5:1 A5:1 B5:2 G5:2 A5:1 G5:1 F5:1 A5:1 F#5:2 D#5:2 E6:2 D6:1 C6:1 B5:2 E5:2 F5:1 A5:1 C6:1 A5:1 B5:1 A5:1 F#5:1 D#5:1"
    ca = ["Em", "Em", "F", "Em", "Em", "Em", ("G", "F"), "E"]
    cb = "Am Em F B7 Am Em F B7".split()
    sections = [("A1", a, ca), ("A2", a, ca), ("B", b, cb), ("A3", a, ca)]
    for si, (name, text, chords) in enumerate(sections):
        b0 = si * 32
        mel, _ = melody(text, b0)
        s.line("lead", mel, v_brass, 0.38)
        if name != "A1":
            s.line("lead", mel, v_lead, 0.14, oct_shift=-1)
        for bar, cc in enumerate(chords):
            bb = b0 + bar * 4
            for half, c in enumerate(cc if isinstance(cc, tuple) else (cc,)):
                root, iv = chord(c)
                r2 = 40 + (root - 4) % 12 - (12 if (root - 4) % 12 > 7 else 0)   # perto de E2
                pat = (0, 0, 12, 0, 1, 0, 3, 1) if c in ("Em", "E") else (0, 0, 12, 0, 0, 12, 7, 12)
                span = 4 if not isinstance(cc, tuple) else 2
                for k in range(int(span * 2)):
                    s.add("bass", bb + half * 2 + 0.5 * k, v_pulse_bass(midi(r2 + pat[k]), 0.5 * s.spb), 0.55)
                if name != "A1":   # metais sincopados no 1 e no "e" do 2
                    for at in ((0, 1.5) if span == 4 else (0,)):
                        for i in iv[:3]:
                            s.add("stab", bb + half * 2 + at, v_brass(midi(64 + (root + i - 4) % 12), 0.2 * s.spb), 0.07)
            for k in range(8):
                s.add("drums", bb + 0.5 * k, dr["hats"][k % 4], 0.10 if k % 2 else 0.06)
            for k in (1, 3):
                s.add("drums", bb + k, dr["snare"], 0.26)
            kicks = (0, 1, 2, 3) if name in ("B", "A3") else (0, 2, 2.5)
            for k in kicks:
                s.add("drums", bb + k, dr["kick"], 0.55)
            if name == "B":
                for k in (0, 0.75, 2):
                    s.add("drums", bb + k, dr["tom_lo"], 0.25)
            if bar == 7 and name in ("A2", "A3"):
                for k in range(4):
                    s.add("drums", bb + 2 + 0.5 * k, dr["tom_hi" if k < 2 else "tom_lo"], 0.3)
    x = roll_echo(s.loop("lead"), n_n(s, 0.5), (0.18, 0.05)) + s.loop("bass") + s.loop("stab") + s.loop("drums")
    return room(x)


MUSIC = {"menu": song_menu, "masmorra": song_masmorra, "chefe": song_chefe}


# ------------------------------------------------------------------ medida, normalizacao, escrita

def kweight(x, sr):
    """Pre-filtro K da BS.1770 (prateleira +4 dB em 1,68 kHz + passa-altas 38 Hz), formulas RBJ."""
    out = x
    for kind, fc, q, g in (("shelf", 1681.974450955533, 0.7071752369554196, 3.999843853973347), ("hp", 38.13547087602444, 0.5003270373238773, 0)):
        w0 = 2 * np.pi * fc / sr
        alpha, cw = np.sin(w0) / (2 * q), np.cos(w0)
        if kind == "shelf":
            A = 10 ** (g / 40)
            sa = 2 * np.sqrt(A) * alpha
            b = [A * ((A + 1) + (A - 1) * cw + sa), -2 * A * ((A - 1) + (A + 1) * cw), A * ((A + 1) + (A - 1) * cw - sa)]
            a = [(A + 1) - (A - 1) * cw + sa, 2 * ((A - 1) - (A + 1) * cw), (A + 1) - (A - 1) * cw - sa]
        else:
            b = [(1 + cw) / 2, -(1 + cw), (1 + cw) / 2]
            a = [1 + alpha, -2 * cw, 1 - alpha]
        out = signal.lfilter(b, a, out)
    return out


def lufs(x, sr, gated):
    """gated: integrado da BS.1770 (musica). Senao: maior loudness momentanea (janela de 400 ms, ou o clipe inteiro se for
    mais curto), que e o que se ouve num SFX sem a cauda puxar a media para baixo."""
    y = kweight(x, sr) ** 2
    blk, hop = min(int(0.4 * sr), len(y)), int(0.01 * sr if not gated else 0.1 * sr)
    c = np.concatenate([[0.0], np.cumsum(y)])
    starts = np.arange(0, len(y) - blk + 1, hop)
    ms = (c[starts + blk] - c[starts]) / blk
    if not gated:
        return -0.691 + 10 * np.log10(np.max(ms) + 1e-20)
    ms = ms[-0.691 + 10 * np.log10(ms + 1e-20) > -70]
    rel = -0.691 + 10 * np.log10(np.mean(ms)) - 10
    ms = ms[-0.691 + 10 * np.log10(ms) > rel]
    return -0.691 + 10 * np.log10(np.mean(ms))


def true_peak_db(x):
    return 20 * np.log10(np.max(np.abs(signal.resample_poly(x, 4, 1))) + 1e-20)


def normalize(x, sr, target, gated):
    g = min(10 ** ((target - lufs(x, sr, gated)) / 20), 10 ** ((PEAK_DB - true_peak_db(x)) / 20))
    return x * g


def finish_sfx(x, target):
    x = hp(np.asarray(x, float), 40)                 # sem DC nem sub que o alto-falante do celular nao toca
    # transiente forte demais para chegar ao alvo sem passar do pico: limitador suave (tanh), no maximo 6 dB
    short = (target - lufs(x, SR, False)) - (PEAK_DB - true_peak_db(x))
    if short > 1:
        x = hp(np.tanh(10 ** (min(short, 6) / 20) * x / np.max(np.abs(x))), 10)   # o tanh assimetrico cria DC: tira so o DC
    thr = np.max(np.abs(x)) * 10 ** (-50 / 20)      # corta silencio do comeco e do fim (-50 dB do pico)
    loud = np.abs(x) > thr
    x = x[np.argmax(loud):len(x) - np.argmax(loud[::-1])].copy()
    fi, fo = min(32, len(x) // 4), min(n_of(0.005), len(x) // 4)
    x[:fi] *= np.linspace(0, 1, fi)
    x[-fo:] *= np.linspace(1, 0, fo)
    return normalize(x, SR, target, gated=False)


def write_wav(path, x, sr):
    pcm = np.clip(np.round(x * 32767), -32768, 32767).astype("<i2")
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(pcm.tobytes())
    return pcm.astype(float) / 32768


def guid(rel):
    return hashlib.md5(("dc-audio:" + rel).encode()).hexdigest()


def write_meta(path, rel, music):
    """Import do Unity (persona 37/38): SFX curto = Decompress On Load; musica = Streaming + Load In Background. So se faltar."""
    meta = path.with_name(path.name + ".meta")
    if meta.exists():
        return
    if music is None:
        body = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
    else:
        lt, q, pre, bg = (2, 0.5, 0, 1) if music else (0, 0.7, 1, 0)
        body = ("AudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n    serializedVersion: 2\n"
                f"    loadType: {lt}\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 1\n"
                f"    quality: {q}\n    conversionMode: 0\n    preloadAudioData: {pre}\n  platformSettingOverrides: {{}}\n"
                f"  forceToMono: 0\n  normalize: 0\n  preloadAudioData: {pre}\n  loadInBackground: {bg}\n  ambisonic: 0\n  3D: 0\n")
    meta.write_text(f"fileFormatVersion: 2\nguid: {guid(rel)}\n{body}  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
                    encoding="utf-8", newline="\n")


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    rows, problems = [], []
    for d in ("", "sfx", "musica"):
        p = OUT / d if d else OUT
        p.mkdir(parents=True, exist_ok=True)
        write_meta(p, "folder:" + d, None)
    print(f"{'arquivo':28} {'dur s':>6} {'pico dBTP':>9} {'LUFS':>6} {'DC':>8}")
    for name, (fn, nvar, target) in SFX.items():
        for v in range(nvar):
            rng = np.random.default_rng(zlib.crc32(f"{name}#{v}".encode()))
            rel = f"sfx/{name}_{v + 1}.wav"
            y = write_wav(OUT / rel, finish_sfx(fn(rng, v), target), SR)
            write_meta(OUT / rel, rel, False)
            rows.append(check(rel, y, SR, False, problems))
    for name, fn in MUSIC.items():
        x = fn()
        x = normalize(x - x.mean(), MSR, MUSIC_LUFS, gated=True)
        rel = f"musica/{name}.wav"
        y = write_wav(OUT / rel, x, MSR)
        write_meta(OUT / rel, rel, True)
        rows.append(check(rel, y, MSR, True, problems))
    total = sum((OUT / r).stat().st_size for r, *_ in rows)
    print(f"{len(rows)} arquivos, {total / 1e6:.2f} MB")
    if problems:
        sys.exit("FALHOU:\n" + "\n".join(problems))
    print("OK: pico < -1 dBTP, sem DC, sem silencio inicial, lacos sem emenda")
    if "--md" in sys.argv:
        s = hashlib.sha256(Path(__file__).read_bytes().replace(b"\r\n", b"\n")).hexdigest()   # mesmo hash com checkout CRLF
        print(f"\naudio_gen.py SHA-256 {s}\n")
        for rel, *_ in rows:
            print(f"| `Audio/{rel}` | `client/tools/audio_gen.py` (numpy/scipy) | `{s[:8]}…{s[-4:]}` | `{sha(OUT / rel)}` | própria (sintetizado) |")


def check(rel, y, sr, music, problems):
    dur, tp, dc = len(y) / sr, true_peak_db(y), float(np.mean(y))
    lu = lufs(y, sr, gated=music)
    print(f"{rel:28} {dur:6.2f} {tp:9.2f} {lu:6.1f} {dc:8.5f}")
    if tp >= -1.0:
        problems.append(f"{rel}: pico {tp:.2f} dBTP")
    if abs(dc) > 1e-3:
        problems.append(f"{rel}: DC {dc:.5f}")
    if music:
        jump = abs(y[0] - y[-1])
        typical = np.percentile(np.abs(np.diff(y)), 99.9)
        if not 45 <= dur <= 90 or jump > typical:
            problems.append(f"{rel}: dur {dur:.1f} s, emenda {jump:.4f} > p99,9 {typical:.4f}")
    elif abs(y[0]) > 0.01 or np.max(np.abs(y[:n_of(0.01)])) < 0.01:
        problems.append(f"{rel}: comeco com silencio ou estalo")
    return rel, dur, tp, lu, dc


if __name__ == "__main__":
    main()
