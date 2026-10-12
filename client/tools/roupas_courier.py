"""roupas_courier.py - Blender headless: as roupas do courier (cosmetico, 0 credito) por troca de cor no MESMO modelo.

Le o courier ja importado (.blend do render_sprites; o FBX tambem serve), muda o basecolor e salva um .blend por roupa
para o render_sprites.py (-Nome courier_<roupa> -Preset courier). O casaco e o chapeu sao os pixels azuis do basecolor
(matiz 180-250 graus); bolsa, botas, cinto e luvas sao os marrons (matiz 0-45, saturados). Pele e camisa (pouca
saturacao) ficam. O sombreado da textura fica: so matiz, saturacao e um fator de brilho mudam.
Chapeu com cor propria (mineiro): os pixels azuis dentro das faces do chapeu (faces com todos os vertices acima de
HAT_Z da altura do modelo), achados no mapa UV.
Nomes = Hud.FolhaRoupa (MetaTelas.cs: courier_noturno, _real, _explorador, _forja, _fantasma, _mineiro, _coveiro); a
Classica e o `courier`.

Uso: blender.exe -b --factory-startup --python-exit-code 1 --python roupas_courier.py -- <courier.blend|fbx> <pasta saida>
     [roupa1,roupa2]   (sem a lista: todas)
"""
import os, sys
import bpy
import numpy as np

# roupa -> azul (casaco), marrom (couro), fantasma, chapeu: (matiz em graus, saturacao, fator de brilho); None = fica.
# Fora das cores de papel do ART_BIBLE sec. 3 (limao da carga, roxo, vermelho, laranja-ferrugem, musgo do ogro, verde)
ROUPAS = {
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
HAT_Z = 0.80  # fracao da altura do modelo acima da qual a face e do chapeu (aba larga do courier)


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


def hat_mask(w, h):
    """Pixels do mapa UV cobertos pelas faces do chapeu (todos os vertices acima de HAT_Z da altura)."""
    me = next(o for o in bpy.data.objects if o.type == "MESH")
    M = np.array(me.matrix_world)
    co = np.empty(len(me.data.vertices) * 3)
    me.data.vertices.foreach_get("co", co)
    z = (co.reshape(-1, 3) @ M[:3, :3].T + M[:3, 3])[:, 2]
    top = z.min() + HAT_Z * (z.max() - z.min())
    me.data.calc_loop_triangles()
    uv = me.data.uv_layers.active.data
    mask = np.zeros((h, w), bool)
    n = 0
    for lt in me.data.loop_triangles:
        if min(z[i] for i in lt.vertices) < top:
            continue
        n += 1
        t = np.array([uv[l].uv[:] for l in lt.loops]) * (w, h)
        x0, y0 = np.floor(t.min(0)).astype(int)
        x1, y1 = np.ceil(t.max(0)).astype(int)
        gx, gy = np.meshgrid(np.arange(max(x0, 0), min(x1, w)) + 0.5, np.arange(max(y0, 0), min(y1, h)) + 0.5)
        (ax, ay), (bx, by), (cx, cy) = t
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-12:
            continue
        l1 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den
        l2 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den
        ins = (l1 >= -0.01) & (l2 >= -0.01) & (1 - l1 - l2 >= -0.01)
        mask[gy[ins].astype(int), gx[ins].astype(int)] = True
    print("CHAPEU %d triangulos acima de %.3f m; %d pixels no UV" % (n, top, int(mask.sum())))
    return mask.ravel()


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


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    src, outdir = args[0], args[1]
    only = args[2].split(",") if len(args) > 2 else list(ROUPAS)
    os.makedirs(outdir, exist_ok=True)
    for nome in only:
        blue, brown, ghost, hat = ROUPAS[nome]
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
            px[:, :3] = recolor(px[:, :3].astype(np.float64), blue, brown, ghost, hat, hat_mask(w, hgt) if hat else None)
            img.pixels.foreach_set(px.ravel())
            img.pack()
        out = os.path.join(os.path.abspath(outdir), "courier_%s.blend" % nome)
        bpy.ops.wm.save_as_mainfile(filepath=out)
        print("ROUPA_OK", nome, out)


main()
