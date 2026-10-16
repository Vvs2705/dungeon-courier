"""roupas_courier.py - Blender headless: as roupas do courier (cosmetico, 0 credito) por troca de cor no MESMO modelo.

Le o courier ja importado (.blend do render_sprites; o FBX tambem serve), muda o basecolor e salva um .blend por roupa
para o render_sprites.py (-Nome courier_<roupa> -Preset courier). O casaco e o chapeu sao os pixels azuis do basecolor
(matiz 180-250 graus); bolsa, botas, cinto e luvas sao os marrons (matiz 0-45, saturados). Pele e camisa (pouca
saturacao) ficam. O sombreado da textura fica: so matiz, saturacao e um fator de brilho mudam.
Chapeu com cor propria (mineiro): os pixels azuis dentro das faces do chapeu (faces com todos os vertices acima de
HAT_Z da altura do modelo), achados no mapa UV.
Nomes = Hud.FolhaRoupa (MetaTelas.cs: courier_noturno, _real, _explorador, _forja, _fantasma, _mineiro, _coveiro); a
Classica e o `courier`.

Aba do chapeu menor (2026-10-15, feedback "o chapeu cobre o tronco a 60 graus"): com o 4o argumento < 1, os vertices acima de
HAT_Z se aproximam do eixo da cabeca nesse fator (so X e Y; rampa de HAT_RAMPA de altura para nao rasgar a malha na testa). Vale no
repouso do rig, entao todos os clipes herdam. "classico" = sem troca de cor (o `courier`).

Uso: blender.exe -b --factory-startup --python-exit-code 1 --python roupas_courier.py -- <courier.blend|fbx> <pasta saida>
     [roupa1,roupa2] [fator_da_aba]   (sem a lista ou com "todas": todas)
"""
import os, sys
import bpy
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roupas_cor import ROUPAS, recolor  # noqa: E402  (cores e troca, sem bpy: o roupas_2d.py usa as mesmas)
HAT_Z = 0.80  # fracao da altura do modelo acima da qual a face e do chapeu (aba larga do courier)
HAT_RAMPA = 0.03  # fracao da altura em que o encolhimento da aba vai de 0 a inteiro


def aba_menor(fator):
    """Encolhe a aba (X e Y) em volta do eixo da cabeca, no repouso do rig."""
    me = next(o for o in bpy.data.objects if o.type == "MESH")
    M = np.array(me.matrix_world)
    co = np.empty(len(me.data.vertices) * 3)
    me.data.vertices.foreach_get("co", co)
    p = co.reshape(-1, 3) @ M[:3, :3].T + M[:3, 3]
    z0, z1 = p[:, 2].min(), p[:, 2].max()
    top = z0 + HAT_Z * (z1 - z0)
    cab = p[p[:, 2] > top]
    cx, cy = np.median(cab[:, 0]), np.median(cab[:, 1])
    k = np.clip((p[:, 2] - top) / (HAT_RAMPA * (z1 - z0)), 0.0, 1.0)
    k = k * k * (3 - 2 * k)
    f = 1.0 - (1.0 - fator) * k
    p[:, 0] = cx + (p[:, 0] - cx) * f
    p[:, 1] = cy + (p[:, 1] - cy) * f
    Minv = np.linalg.inv(M)
    me.data.vertices.foreach_set("co", (p @ Minv[:3, :3].T + Minv[:3, 3]).ravel())
    me.data.update()
    print("ABA x%.2f acima de %.3f m (eixo %.3f, %.3f)" % (fator, top, cx, cy))


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


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    src, outdir = args[0], args[1]
    only = args[2].split(",") if len(args) > 2 and args[2] != "todas" else [r for r in ROUPAS if r != "classico"]
    fator = float(args[3]) if len(args) > 3 else 1.0
    os.makedirs(outdir, exist_ok=True)
    for nome in only:
        blue, brown, ghost, hat = ROUPAS[nome]
        bpy.ops.wm.read_factory_settings(use_empty=True)
        if src.lower().endswith(".blend"):
            bpy.ops.wm.open_mainfile(filepath=os.path.abspath(src))
        else:
            bpy.ops.import_scene.fbx(filepath=os.path.abspath(src), use_anim=False)
        if fator < 1.0:
            aba_menor(fator)
        imgs = [i for i in bpy.data.images if "basecolor" in (i.filepath + i.name).lower()]  # o import do FBX chama de "Diffuse Texture.00n"
        if nome == "classico":
            for img in imgs:
                img.pack()
            imgs = None
        elif not imgs:
            print("ERRO nenhuma imagem basecolor no modelo")
            sys.exit(1)
        for img in imgs or []:  # o Tripo liga o basecolor em 2 nos de imagem: muda os dois
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
