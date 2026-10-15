"""roupas_2d.py [roupa,roupa] : as roupas do courier direto nas folhas renderizadas (Resources/Sprites/courier -> courier_<roupa>), 0 Blender.

O render do courier e' de luz flat: o pixel da folha e' a cor do basecolor, entao a mesma troca de matiz do roupas_courier.py
(roupas_cor.recolor) vale na folha. O contorno de tinta (matiz ~258) fica fora das faixas trocadas. Fica de fora a roupa com cor
propria do chapeu (mineiro: a mascara do chapeu e' do mapa UV, entao ela sai do Blender). Copia o meta.json da Classica.
Uso: python client/tools/roupas_2d.py            (noturno, real, explorador, forja, fantasma, coveiro)
"""
import json, os, shutil, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roupas_cor import ROUPAS, recolor  # noqa: E402

SPR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_DC", "Resources", "Sprites")
so = sys.argv[1].split(",") if len(sys.argv) > 1 else [r for r, v in ROUPAS.items() if r != "classico" and v[3] is None]
src = os.path.join(SPR, "courier")
meta = json.load(open(os.path.join(src, "meta.json"), encoding="utf-8"))
for nome in so:
    azul, couro, fantasma, chapeu = ROUPAS[nome]
    if chapeu is not None:
        sys.exit("ERRO %s tem chapeu com cor propria: sai do roupas_courier.py no Blender" % nome)
    out = os.path.join(SPR, "courier_" + nome)
    os.makedirs(out, exist_ok=True)
    for c in meta["clips"]:
        im = np.asarray(Image.open(os.path.join(src, c["file"])).convert("RGBA"), dtype=np.float64) / 255.0
        vis = im[..., 3] > 0
        im[vis, :3] = recolor(im[vis, :3], azul, couro, fantasma)
        Image.fromarray((np.clip(im, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(os.path.join(out, c["file"]))
    shutil.copyfile(os.path.join(src, "meta.json"), os.path.join(out, "meta.json"))
    print("ROUPA_2D_OK", nome, out)
