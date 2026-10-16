"""roupas_cor.py - a troca de cor das roupas do courier (sem Blender): usada pelo roupas_courier.py (no basecolor do modelo) e pelo
roupas_2d.py (direto nas folhas renderizadas do courier, com luz flat: o pixel da folha e' a cor da textura)."""
import numpy as np


# roupa -> azul (casaco), marrom (couro), fantasma, chapeu: (matiz em graus, saturacao, fator de brilho); None = fica.
# Fora das cores de papel do ART_BIBLE sec. 3 (limao da carga, roxo, vermelho, laranja-ferrugem, musgo do ogro, verde)
ROUPAS = {
    "classico": (None, None, False, None),                             # o `courier` (so a aba, se pedida)
    "noturno": ((245, 0.55, 0.55), (25, 0.30, 0.45), False, None),     # anil escuro, couro quase preto
    "real": ((280, 0.65, 0.85), (45, 0.75, 1.25), False, None),        # roxo real, acessorios dourados
    "explorador": ((42, 0.35, 1.30), (28, 0.55, 0.80), False, None),   # caqui, couro escuro
    "forja": ((10, 0.85, 1.10), (20, 0.25, 0.45), False, None),        # vermelho-fogo, couro carvao (bombeiro de forja)
    "fantasma": (None, None, True, None),                              # tudo palido ciano (a View ainda aplica o alfa)
    # mineiro: capacete ambar (a lanterna de mina; nao o limao da carga nem o amarelo do goblin), macacao marrom
    # escuro (o grupo do couro: calca, botas, bolsa, o que aparece a 60 graus) e casaco caramelo (os detalhes laranja)
    "mineiro": ((28, 0.70, 1.00), (24, 0.60, 0.70), False, (36, 0.80, 1.30)),
    # coveiro: casaco e chapeu chumbo claro (mais claro que o piso azul-cinza), couro salvia (o verde-cinza da cripta,
    # longe do musgo do ogro e do verde morto-vivo)
    "coveiro": ((215, 0.10, 0.80), (100, 0.20, 0.95), False, None),
}


def hsv(rgb):
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = mx - mn
    h = np.zeros_like(mx)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    m = d > 1e-6
    h = np.where(m & (mx == r), ((g - b) / np.maximum(d, 1e-6)) % 6, h)
    h = np.where(m & (mx == g), (b - r) / np.maximum(d, 1e-6) + 2, h)
    h = np.where(m & (mx == b), (r - g) / np.maximum(d, 1e-6) + 4, h)
    return h * 60.0, np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0.0), mx


def rgb(h, s, v):
    c = v * s
    x = c * (1 - np.abs((h / 60.0) % 2 - 1))
    z = np.zeros_like(h)
    k = (h // 60).astype(int) % 6
    r = np.choose(k, [c, x, z, z, x, c])
    g = np.choose(k, [x, c, c, x, z, z])
    b = np.choose(k, [z, z, x, c, c, x])
    return np.stack([r, g, b], -1) + (v - c)[..., None]


def recolor(px, blue, brown, ghost, hat=None, hat_px=None):
    out = px.copy()
    h, s, v = hsv(px)
    if ghost:
        gray = px @ np.array([0.3, 0.59, 0.11])
        out = (0.45 + 0.55 * gray)[..., None] * np.array([0.82, 1.0, 1.0])
        return np.clip(out, 0, 1)
    is_blue = (h >= 180) & (h <= 250) & (s > 0.2) & (v > 0.08)
    groups = [(is_blue & ~hat_px if hat_px is not None else is_blue, blue),
              ((h >= 0) & (h <= 45) & (s > 0.35) & (v > 0.08), brown)]
    if hat_px is not None:
        groups.append((is_blue & hat_px, hat))
    for m, tgt in groups:
        if tgt is None:
            continue
        th, ts, tv = tgt
        out[m] = rgb(np.full(m.sum(), float(th)), np.full(m.sum(), ts), np.clip(v[m] * tv, 0, 1))
    return np.clip(out, 0, 1)
