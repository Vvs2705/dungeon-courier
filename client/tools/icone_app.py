"""icone_app.py [pasta arte/] : icone do app no celular a partir do logo B2 (mascote no arco da masmorra, sem o letreiro).

Saidas em client/Assets/_DC/Icon/ (fora de Resources: so o Setup.Apply usa, nao entra no jogo):
  icone_fundo.png   432x432  camada de tras do icone adaptativo do Android (azul-marinho do logo com brilho dourado no meio)
  icone_frente.png  432x432  camada da frente (mascote + arco recortados, dentro da zona segura: o Android corta ate 1/3 da borda)
  icone.png         512x512  Windows: as duas camadas juntas, sem transparencia (no Android o Unity 6 so usa o adaptativo)
e client/Builds/loja/icone_512.png (a Play pede o icone da loja igual ao do aparelho). Uso: python client/tools/icone_app.py
"""
import os
import sys
import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ui_assets import le, recorta  # noqa: E402

CORTE = (226, 40, 814, 628)   # mascote + arco no logo 1024 (o letreiro DUNGEON comeca em y ~632)
FUNDO_MEIO, FUNDO_BORDA, BRILHO = (44, 62, 102), (16, 22, 40), (255, 196, 92)


def fundo(w):
    y, x = np.mgrid[0:w, 0:w] / (w - 1) - 0.5
    r = np.clip(np.hypot(x, y) / 0.7071, 0, 1)[..., None]
    rgb = np.array(FUNDO_MEIO) * (1 - r) + np.array(FUNDO_BORDA) * r
    g = np.exp(-(x * x + (y + 0.04) ** 2) / 0.045)[..., None] * 0.35   # brilho dourado atras do mascote
    rgb = rgb * (1 - g) + np.array(BRILHO) * g
    return Image.fromarray(rgb.astype(np.uint8), "RGB").convert("RGBA")


def frente(arte, w, ocupa):
    rgba = recorta(le(os.path.join(arte, "logo", "logo_b2_original.png")))
    x0, y0, x1, y1 = CORTE
    rgba = rgba[y0:y1, x0:x1].copy()
    rgba[-24:, :, 3] = (rgba[-24:, :, 3] * np.linspace(1, 0, 24)[:, None]).astype(np.uint8)   # pe do arco some antes do letreiro
    img = Image.fromarray(rgba, "RGBA")
    lado = int(w * ocupa)
    img = img.resize((lado, lado), Image.LANCZOS)
    tela = Image.new("RGBA", (w, w), (0, 0, 0, 0))
    sombra = Image.new("RGBA", (w, w), (0, 0, 0, 0))
    sombra.paste((0, 0, 0, 110), ((w - lado) // 2, (w - lado) // 2 + w // 60), img.split()[3])
    tela.alpha_composite(sombra.filter(ImageFilter.GaussianBlur(w / 80)))
    tela.alpha_composite(img, ((w - lado) // 2, (w - lado) // 2))
    return tela


def junta(arte, w, ocupa):
    im = fundo(w)
    im.alpha_composite(frente(arte, w, ocupa))
    return im.convert("RGB")


if __name__ == "__main__":
    aqui = os.path.dirname(os.path.abspath(__file__))
    arte = sys.argv[1] if len(sys.argv) > 1 else os.path.join(aqui, "..", "..", "..", "..", "..", "arte")
    if not os.path.isdir(arte):
        arte = os.path.join(aqui, "..", "..", "arte")
    saida = os.path.join(aqui, "..", "Assets", "_DC", "Icon")
    os.makedirs(saida, exist_ok=True)
    fundo(432).convert("RGB").save(os.path.join(saida, "icone_fundo.png"))
    frente(arte, 432, 0.64).save(os.path.join(saida, "icone_frente.png"))   # 0,64: o launcher mostra o meio 2/3 (288 px) e corta em circulo
    leg = junta(arte, 512, 0.9)
    leg.save(os.path.join(saida, "icone.png"))
    loja = os.path.join(aqui, "..", "Builds", "loja")
    os.makedirs(loja, exist_ok=True)
    leg.save(os.path.join(loja, "icone_512.png"))
    print("ICONE_OK", saida)
