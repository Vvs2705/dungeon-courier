"""ui_assets.py [pasta arte/] : arte da interface (logo, icones dos botoes, fundo dos menus) -> client/Assets/_DC/Resources/UI/.

Entradas cruas (geradas no Tripo, Nano Banana 2 sobre fundo azul-marinho liso; fora do git, docs/PROVENIENCIA.md §4):
  arte/logo/logo_b2_original.png  -> UI/logo.png (logo inteiro, fundo transparente)
  arte/ui/icones_folha.png        -> UI/icone_<nome>.png (grade 4 x 2, na ordem de ICONES: botoes do menu)
  arte/ui/icones_guilda_folha.png -> UI/icone_<nome>.png (grade 4 x 3, na ordem de ICONES_GUILDA: melhorias, armas, equipamento, moeda)
  arte/ui/fundo_masmorra.png      -> UI/fundo_menu.png (copia; o escurecimento e' feito no jogo)
O fundo liso sai pela cor mediana da borda: so a regiao parecida com ela que toca a borda (ilhas como as estrelinhas ficam),
com alfa suave pela distancia de cor e a cor do fundo descontada do pixel meio transparente. Uso: python client/tools/ui_assets.py
"""
import os
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

T0, T1, MARGEM = 14.0, 40.0, 12   # distancia de cor: ate T0 = fundo puro; a partir de T1 = opaco
ICONES = ["guilda", "loja", "temporada", "marcos", "roupas", "config", "creditos", "bau"]
ICONES_GUILDA = ["vida", "dash", "bolsa", "mochila", "estilingue", "bumerangue", "bomba", "fumaca", "gancho", "armadilha", "botas", "moeda"]


def recorta(rgb):
    borda = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
    fundo = np.median(borda, axis=0)
    dist = np.linalg.norm(rgb - fundo, axis=2)
    rotulos, _ = ndimage.label(dist < T1)
    tocam = np.unique(np.concatenate([rotulos[0], rotulos[-1], rotulos[:, 0], rotulos[:, -1]]))
    regiao = np.isin(rotulos, tocam[tocam > 0])
    alfa = np.ones(dist.shape)
    alfa[regiao] = np.clip((dist[regiao] - T0) / (T1 - T0), 0, 1)
    a = alfa[..., None]
    cor = np.where(a > 0.01, (rgb - (1 - a) * fundo) / np.maximum(a, 0.01), 0)
    return np.dstack([np.clip(cor, 0, 255), alfa * 255]).astype(np.uint8)


def apara(rgba, margem=MARGEM):
    ys, xs = np.nonzero(rgba[..., 3] > 8)
    return rgba[max(ys.min() - margem, 0):ys.max() + margem + 1, max(xs.min() - margem, 0):xs.max() + margem + 1]


def le(caminho):
    return np.asarray(Image.open(caminho).convert("RGB"), dtype=np.float64)


def salva(rgba, caminho):
    Image.fromarray(rgba, "RGBA").save(caminho)
    print(caminho, rgba.shape[1], "x", rgba.shape[0])


if __name__ == "__main__":
    aqui = os.path.dirname(os.path.abspath(__file__))
    arte = sys.argv[1] if len(sys.argv) > 1 else os.path.join(aqui, "..", "..", "..", "..", "..", "arte")   # worktree -> checkout principal
    if not os.path.isdir(arte):
        arte = os.path.join(aqui, "..", "..", "arte")
    saida = os.path.join(aqui, "..", "Assets", "_DC", "Resources", "UI")
    os.makedirs(saida, exist_ok=True)

    logo = apara(recorta(le(os.path.join(arte, "logo", "logo_b2_original.png"))))
    salva(logo, os.path.join(saida, "logo.png"))

    for arquivo, linhas, nomes in (("icones_folha.png", 2, ICONES), ("icones_guilda_folha.png", 3, ICONES_GUILDA)):
        folha = le(os.path.join(arte, "ui", arquivo))
        h, w = folha.shape[0] // linhas, folha.shape[1] // 4
        for i, nome in enumerate(nomes):
            cel = folha[(i // 4) * h:(i // 4 + 1) * h, (i % 4) * w:(i % 4 + 1) * w]
            salva(apara(recorta(cel), 4), os.path.join(saida, "icone_" + nome + ".png"))

    Image.open(os.path.join(arte, "ui", "fundo_masmorra.png")).convert("RGB").save(os.path.join(saida, "fundo_menu.png"))
    print(os.path.join(saida, "fundo_menu.png"))
