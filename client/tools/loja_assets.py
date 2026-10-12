"""loja_assets.py - artes da pagina da Google Play do Dungeon Courier (Python 3 + Pillow, fora do Unity e do Blender).

Nada de arte nova: so composicao dos sprites do jogo (client/Assets/_DC/Resources/Sprites e Textures) e das fotos reais do
build (-shot, docs/LOJA.md sec. 5).

Uso (da raiz do repositorio):
  python -I client/tools/loja_assets.py [--fotos client/Builds/validation_v1/shots] [--saida client/Builds/loja]
  python -I client/tools/loja_assets.py --autoteste      (fotos sinteticas numa pasta temporaria; confere tamanhos e formatos)

Saida em client/Builds/loja/ (fora do git):
  icone_512.png          courier com a caixa nas maos sobre o piso do jogo; a carga com o contorno limao (a marca da carga no jogo)
  feature_1024x500.png   logo da tela de titulo + Ogro, ladrao, morcego e o courier com a caixa (sem texto pequeno)
  logo.png               logo recortado da foto da tela de titulo, borda esfumada (o fundo da tela e o Bg); o criativos.py usa no CTA
  capturas/NN_<nome>_<lang>.png   as 8 capturas de docs/LOJA.md sec. 5 em 1080x1920 RGB. Foto: <foto>_<lang>.png; sem ela,
                         <foto>.png (a do pt de hoje). Foto que falta e avisada (FALTA) e o resto sai.
"""
import argparse, json, os, shutil, sys, tempfile
from PIL import Image, ImageDraw, ImageFilter

CLIENT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(CLIENT, "Assets", "_DC", "Resources")
SPRITES = os.path.join(RES, "Sprites")
FOTOS = os.path.join(CLIENT, "Builds", "validation_v1", "shots")
SAIDA = os.path.join(CLIENT, "Builds", "loja")
LANGS = ("pt", "en", "es", "id")
# paleta do jogo (Art.cs = ART_BIBLE sec. 3): fundo, carga (limao: so na carga), sombra
BG, CARGO, DARK = (0x14, 0x16, 0x1F), (0xFF, 0xE1, 0x4D), (0x0E, 0x0F, 0x18)
# docs/LOJA.md sec. 5, na ordem da loja: (nome da captura, fotos do -shot sem o sufixo _<lang>: a da loja primeiro, depois a que ja
# existe em validation_v1, usada enquanto a da loja nao for tirada)
CAPTURAS = [
    ("01_ladrao_quer_a_carga", ("c12_chamando",)),
    ("02_pousar_para_lutar", ("c1_ambiente",)),
    ("03_cliente_abre_a_caixa", ("c1_fim_24", "c1_fim_23")),
    ("04_reliquia_fragil", ("c8_rachaduras", "c8_flecha")),
    ("05_barril_de_polvora", ("c13_barril",)),
    ("06_chefe_ogro", ("c24_ogro_b1", "c24s_45")),
    ("07_quadro_de_contratos", ("tela_quadro",)),
    ("08_guilda_sem_energia", ("tela_guilda",)),
]
# logo na foto da tela de titulo, em fracao da foto (icone + DUNGEON COURIER, sem o slogan, que muda com o idioma)
LOGO_BOX = (0.04, 0.13, 0.96, 0.605)


# ------------------------------------------------------------------ sprites

def meta(ator):
    with open(os.path.join(SPRITES, ator, "meta.json")) as f:
        return json.load(f)


def ator(nome, clip=None, linha=0, i=0, margem=0):
    """Quadro i da linha (0 = de frente; 1 = SW; 2 = W) do clipe (o 1o se None), numa tela com `margem` px em volta.
    Devolve (imagem RGBA, pe (x, y) na imagem, clipe, meta)."""
    m = meta(nome)
    c = next(c for c in m["clips"] if clip in (None, c["name"]))
    s = m["size"]
    folha = Image.open(os.path.join(SPRITES, nome, c["file"])).convert("RGBA")
    out = Image.new("RGBA", (s + 2 * margem, s + 2 * margem), (0, 0, 0, 0))
    out.alpha_composite(folha.crop((i * s, linha * s, i * s + s, linha * s + s)), (margem, margem))
    return out, (margem + m["pivot_px"][0], margem + s - m["pivot_px"][1]), c, m


def contorno(img, cor, px):
    """Contorno de px pixels na cor em volta do que e opaco (como a linha limao da carga no jogo)."""
    big = Image.new("RGBA", (img.width + 2 * px, img.height + 2 * px), (0, 0, 0, 0))
    big.alpha_composite(img, (px, px))
    a = big.getchannel("A").point(lambda v: 255 if v > 40 else 0).filter(ImageFilter.MaxFilter(2 * px + 1))
    out = Image.new("RGBA", big.size, cor + (255,))
    out.putalpha(a)
    out.alpha_composite(big)
    return out


def courier_com_caixa(clip="CarryIdle", linha=0, i=0, limao=6, quem="courier_icone", carga="caixa_icone"):
    """Courier com a caixa na ancora `hands` do quadro (por cima se frente = 1, por baixo se 0), como a View e o folhas.py.
    Padrao: o par do icone (RENDER_SPRITES sec. 4): `courier_icone` (512 px, 320 px/m, 3/4 de frente) com a `caixa_icone` no ponto
    `hands`. Com quem="courier"/carga="caixa", a folha do jogo (224 px, 120 px/m). Devolve (imagem RGBA, pe)."""
    M = 64
    corpo, pe, c, m = ator(quem, clip, linha, i, M)
    caixa = Image.open(os.path.join(SPRITES, carga, meta(carga)["clips"][0]["file"])).convert("RGBA")
    caixa = contorno(caixa.crop(caixa.getbbox()), CARGO, limao)
    hx, hy, frente = c["hands"][(linha * c["frames"] + i) * 3:][:3]
    pos = (int(round(M + hx - caixa.width / 2)), int(round(M + m["size"] - hy - caixa.height / 2)))
    out = Image.new("RGBA", corpo.size, (0, 0, 0, 0))
    if not frente:
        out.alpha_composite(caixa, pos)
    out.alpha_composite(corpo)
    if frente:
        out.alpha_composite(caixa, pos)
    return out, pe


def escala(img, pe, k):
    img = img.resize((max(1, round(img.width * k)), max(1, round(img.height * k))), Image.LANCZOS)
    return img, (pe[0] * k, pe[1] * k)


def cola(fundo, img, pe, onde, sombra=0.0):
    """Cola a imagem RGBA no fundo RGB com o pe em `onde`; sombra = largura (px) da elipse escura no chao."""
    if sombra:
        s = Image.new("L", fundo.size, 0)
        ImageDraw.Draw(s).ellipse((onde[0] - sombra / 2, onde[1] - sombra / 7, onde[0] + sombra / 2, onde[1] + sombra / 7), fill=150)
        fundo.paste(Image.new("RGB", fundo.size, DARK), (0, 0), s.filter(ImageFilter.GaussianBlur(sombra / 14)))
    fundo.paste(img, (int(round(onde[0] - pe[0])), int(round(onde[1] - pe[1]))), img)


def piso(w, h, claro=0.55):
    """Textura real do piso (Resources/Textures/piso.png) recortada no centro, escurecida para o Bg."""
    t = Image.open(os.path.join(RES, "Textures", "piso.png")).convert("RGB")
    k = max(w / t.width, h / t.height)
    t = t.resize((round(t.width * k), round(t.height * k)), Image.LANCZOS)
    x, y = (t.width - w) // 2, (t.height - h) // 2
    return Image.blend(Image.new("RGB", (w, h), BG), t.crop((x, y, x + w, y + h)), claro)


# ------------------------------------------------------------------ artes

def icone():
    """512x512 sem transparencia (a Play aplica a mascara): piso com vinheta, courier com a caixa grande no centro."""
    W = 512
    fundo = piso(W, W)
    vinheta = Image.radial_gradient("L").resize((W, W)).point(lambda v: int(v * 0.85))
    fundo = Image.composite(Image.new("RGB", (W, W), BG), fundo, vinheta)
    img, pe = courier_com_caixa()   # courier_icone + caixa_icone, sem ampliar (1:1 ate ~430 px)
    box = img.getbbox()
    img, pe = img.crop(box), (pe[0] - box[0], pe[1] - box[1])
    img, pe = escala(img, pe, min(400 / img.width, 430 / img.height))
    x, y = (W - img.width) / 2, (W - img.height) / 2 + 6   # centrado (a sombra cai no pe)
    cola(fundo, img, pe, (x + pe[0], y + pe[1]), sombra=img.width * 0.8)
    return fundo


def logo(fotos):
    """Logo da foto da tela de titulo (-tela titulo): icone + DUNGEON COURIER, borda esfumada para sumir sobre fundo Bg."""
    f, _ = foto(fotos, ("tela_titulo",), "pt")
    if not f:
        return None
    im = Image.open(f).convert("RGB")
    W, H = im.size
    lg = im.crop(tuple(round(v * (W if j % 2 == 0 else H)) for j, v in enumerate(LOGO_BOX))).convert("RGBA")
    F = max(4, lg.width // 22)
    a = Image.new("L", lg.size, 0)
    ImageDraw.Draw(a).rectangle((F, F, lg.width - F - 1, lg.height - F - 1), fill=255)
    lg.putalpha(a.filter(ImageFilter.GaussianBlur(F / 2)))
    return lg


def feature(lg):
    """1024x500: logo a esquerda sobre o Bg; a direita o piso real, o Dragao Anciao (chefe final) ao fundo, o ladrao vindo e o
    courier com a caixa (par do icone)."""
    W, H = 1024, 500
    fundo = piso(W, H, 0.6)
    grad = Image.linear_gradient("L").rotate(-90, expand=True).resize((W, H))   # 255 na esquerda, 0 na direita
    grad = grad.point(lambda v: max(0, min(255, int((v - 93) * 5.1))))          # Bg ate x ~450, piso a partir de ~650
    fundo = Image.composite(Image.new("RGB", (W, H), BG), fundo, grad)
    dragao, pe, _, _ = ator("dragao", "Roar", 0, 3)
    cola(fundo, *escala(dragao, pe, 0.85), (880, 300), sombra=230)
    ladrao, pe, _, _ = ator("ladrao", "Run", 2, 0)                     # linha W: corre para a esquerda, atras da carga
    cola(fundo, *escala(ladrao, pe, 1.6), (975, 482), sombra=70)
    courier, pe = courier_com_caixa()
    cola(fundo, *escala(courier, pe, 0.70), (625, 468), sombra=120)
    if lg is not None:
        k = min(430 / lg.width, 450 / lg.height)
        lg2 = lg.resize((round(lg.width * k), round(lg.height * k)), Image.LANCZOS)
        fundo.paste(lg2, (int(245 - lg2.width / 2), int(H / 2 - lg2.height / 2)), lg2)
    return fundo


def foto(fotos, nomes, lang):
    """(caminho, no idioma?): a 1a foto <nome>_<lang>.png da lista; senao a 1a <nome>.png (as fotos de hoje sao pt, sem sufixo)."""
    for cands, exato in (([n + "_" + lang for n in nomes], True), (list(nomes), lang == "pt")):
        for n in cands:
            p = os.path.join(fotos, n + ".png")
            if os.path.isfile(p):
                return p, exato
    return None, False


def captura(path):
    """Foto -shot (540x960) em 1080x1920 RGB: lanczos; fora de 9:16, cabe e completa com o Bg."""
    im = Image.open(path).convert("RGB")
    k = min(1080 / im.width, 1920 / im.height)
    im = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
    out = Image.new("RGB", (1080, 1920), BG)
    out.paste(im, ((1080 - im.width) // 2, (1920 - im.height) // 2))
    return out


def gerar(fotos, saida):
    os.makedirs(os.path.join(saida, "capturas"), exist_ok=True)
    falta = []
    icone().save(os.path.join(saida, "icone_512.png"))
    lg = logo(fotos)
    if lg is None:
        falta.append("tela_titulo")
    else:
        lg.save(os.path.join(saida, "logo.png"))
    feature(lg).save(os.path.join(saida, "feature_1024x500.png"))
    n, outra = 0, []
    for nome, nomes in CAPTURAS:
        for lang in LANGS:
            p, exato = foto(fotos, nomes, lang)
            if not p:
                if nomes[0] not in falta:
                    falta.append(nomes[0])
                continue
            if not exato:
                outra.append("%s_%s" % (nome[:2], lang))
            captura(p).save(os.path.join(saida, "capturas", "%s_%s.png" % (nome, lang)))
            n += 1
    print("LOJA %s: icone_512, feature_1024x500, logo%s, %d capturas 1080x1920" % (saida, "" if lg else " (FALTA)", n))
    if outra:
        print("AVISO %d capturas com a foto pt (HUD em portugues): %s. Foto por idioma: -lang <l> e sufixo _<l>" % (len(outra), " ".join(outra)))
    for f in falta:
        print("FALTA foto %s.png (ou %s_<lang>.png) em %s: comando em docs/LOJA.md sec. 5" % (f, f, fotos))
    return falta


# ------------------------------------------------------------------ autoteste

def abre(p):
    """Le a imagem inteira e solta o arquivo (no Windows, arquivo aberto impede apagar a pasta temporaria)."""
    with Image.open(p) as im:
        return im.copy()


def autoteste():
    tmp = tempfile.mkdtemp(prefix="dc_loja_")
    try:
        fotos, saida = os.path.join(tmp, "fotos"), os.path.join(tmp, "loja")
        os.makedirs(fotos)
        t = Image.new("RGB", (540, 960), BG)
        ImageDraw.Draw(t).rectangle((60, 150, 480, 560), fill=(243, 226, 192))   # "logo" no meio do recorte
        t.save(os.path.join(fotos, "tela_titulo.png"))
        for j, (_, nomes) in enumerate(CAPTURAS[:-1]):          # a ultima falta de proposito; so a foto de hoje (a ultima da lista)
            Image.new("RGB", (540, 960), (40 + j * 20, 60, 90)).save(os.path.join(fotos, nomes[-1] + ".png"))
        Image.new("RGB", (600, 900), (200, 60, 60)).save(os.path.join(fotos, CAPTURAS[0][1][0] + "_en.png"))   # fora de 9:16
        Image.new("RGB", (540, 960), (9, 99, 9)).save(os.path.join(fotos, CAPTURAS[2][1][0] + "_es.png"))     # a da loja ganha da de hoje
        falta = gerar(fotos, saida)
        assert falta == [CAPTURAS[-1][1][0]], falta
        es = abre(os.path.join(saida, "capturas", CAPTURAS[2][0] + "_es.png"))
        assert es.getpixel((540, 960)) == (9, 99, 9), "a foto da loja no idioma devia ganhar da foto de hoje"

        ic = abre(os.path.join(saida, "icone_512.png"))
        assert ic.size == (512, 512) and ic.mode == "RGB", (ic.size, ic.mode)
        assert os.path.getsize(os.path.join(saida, "icone_512.png")) < 1024 * 1024, "icone > 1 MB (limite da Play)"
        px = ic.load()
        limao = sum(1 for y in range(60, 452, 2) for x in range(60, 452, 2) if px[x, y][0] > 230 and px[x, y][1] > 200 and px[x, y][2] < 140)
        assert limao > 50, "sem o contorno limao da carga no icone (%d px)" % limao
        assert px[0, 0] == px[511, 511] or max(px[0, 0]) < 60, "canto do icone devia ser o fundo escuro"

        fg = abre(os.path.join(saida, "feature_1024x500.png"))
        assert fg.size == (1024, 500) and fg.mode == "RGB", (fg.size, fg.mode)
        assert fg.getpixel((245, 250)) != BG, "logo ausente no feature graphic"

        lg = abre(os.path.join(saida, "logo.png"))
        assert lg.mode == "RGBA" and lg.getpixel((0, 0))[3] < 10 and lg.getpixel((lg.width // 2, lg.height // 2))[3] > 245

        caps = sorted(os.listdir(os.path.join(saida, "capturas")))
        assert len(caps) == (len(CAPTURAS) - 1) * len(LANGS), caps
        for c in caps:
            im = abre(os.path.join(saida, "capturas", c))
            assert im.size == (1080, 1920) and im.mode == "RGB", (c, im.size, im.mode)
        en = abre(os.path.join(saida, "capturas", CAPTURAS[0][0] + "_en.png"))
        assert en.getpixel((540, 960)) == (200, 60, 60) and en.getpixel((540, 2)) == BG, "foto _en ou o preenchimento do 9:16 errado"
        pt = abre(os.path.join(saida, "capturas", CAPTURAS[0][0] + "_pt.png"))
        assert pt.getpixel((540, 960)) == (40, 60, 90), "o pt devia cair na foto sem sufixo"
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    print("autoteste OK")


if __name__ == "__main__":
    ap = argparse.ArgumentParser(description="Artes da pagina da Google Play (icone, feature graphic, capturas) do Dungeon Courier.")
    ap.add_argument("--fotos", default=FOTOS, help="pasta das fotos -shot (padrao: client/Builds/validation_v1/shots)")
    ap.add_argument("--saida", default=SAIDA, help="pasta de saida (padrao: client/Builds/loja, fora do git)")
    ap.add_argument("--autoteste", action="store_true")
    o = ap.parse_args()
    if o.autoteste:
        autoteste()
    else:
        gerar(o.fotos, o.saida)
