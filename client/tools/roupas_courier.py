"""roupas_courier.py - Blender headless: as roupas do courier (cosmetico, 0 credito) por troca de cor no MESMO modelo.

Le o courier ja importado (.blend do render_sprites; o FBX tambem serve), muda o basecolor e salva um .blend por roupa
para o render_sprites.py (-Nome courier_<roupa> -Preset courier). O casaco e o chapeu sao os pixels azuis do basecolor
(matiz 180-250 graus); bolsa, botas, cinto e luvas sao os marrons (matiz 0-45, saturados). Pele e camisa (pouca
saturacao) ficam. O sombreado da textura fica: so matiz, saturacao e um fator de brilho mudam.
Nomes = Hud.FolhaRoupa (MetaTelas.cs: courier_noturno, _real, _explorador, _forja, _fantasma); a Classica e o `courier`.

Uso: blender.exe -b --factory-startup --python-exit-code 1 --python roupas_courier.py -- <courier.blend|fbx> <pasta saida>
"""
import os, sys
import bpy
import numpy as np

# roupa -> (azul: matiz em graus, saturacao, fator de brilho), (marrom: idem; None = fica), tudo cinza-ciano (fantasma)
ROUPAS = {
    "noturno": ((245, 0.55, 0.55), (25, 0.30, 0.45), False),     # anil escuro, couro quase preto
    "real": ((280, 0.65, 0.85), (45, 0.75, 1.25), False),        # roxo real, acessorios dourados
    "explorador": ((42, 0.35, 1.30), (28, 0.55, 0.80), False),   # caqui, couro escuro
    "forja": ((10, 0.85, 1.10), (20, 0.25, 0.45), False),        # vermelho-fogo, couro carvao (bombeiro de forja)
    "fantasma": (None, None, True),                              # tudo palido ciano (a View ainda aplica o alfa)
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


def recolor(px, blue, brown, ghost):
    out = px.copy()
    h, s, v = hsv(px)
    if ghost:
        gray = px @ np.array([0.3, 0.59, 0.11])
        out = (0.45 + 0.55 * gray)[..., None] * np.array([0.82, 1.0, 1.0])
        return np.clip(out, 0, 1)
    for (h0, h1, smin), tgt in (((180, 250, 0.2), blue), ((0, 45, 0.35), brown)):
        if tgt is None:
            continue
        m = (h >= h0) & (h <= h1) & (s > smin) & (v > 0.08)
        th, ts, tv = tgt
        out[m] = rgb(np.full(m.sum(), float(th)), np.full(m.sum(), ts), np.clip(v[m] * tv, 0, 1))
    return np.clip(out, 0, 1)


def main():
    src, outdir = sys.argv[sys.argv.index("--") + 1:][:2]
    os.makedirs(outdir, exist_ok=True)
    for nome, (blue, brown, ghost) in ROUPAS.items():
        bpy.ops.wm.read_factory_settings(use_empty=True)
        if src.lower().endswith(".blend"):
            bpy.ops.wm.open_mainfile(filepath=os.path.abspath(src))
        else:
            bpy.ops.import_scene.fbx(filepath=os.path.abspath(src), use_anim=False)
        imgs = [i for i in bpy.data.images if "basecolor" in (i.filepath + i.name).lower()]  # o import do FBX chama de "Diffuse Texture.00n"
        if not imgs:
            print("ERRO nenhuma imagem basecolor no modelo")
            sys.exit(1)
        for img in imgs:  # o Tripo liga o basecolor em 2 nos de imagem: muda os dois
            w, hgt = img.size
            buf = np.empty(w * hgt * 4, np.float32)
            img.pixels.foreach_get(buf)
            px = buf.reshape(-1, 4)
            px[:, :3] = recolor(px[:, :3].astype(np.float64), blue, brown, ghost)
            img.pixels.foreach_set(px.ravel())
            img.pack()
        out = os.path.join(os.path.abspath(outdir), "courier_%s.blend" % nome)
        bpy.ops.wm.save_as_mainfile(filepath=out)
        print("ROUPA_OK", nome, out)


main()
