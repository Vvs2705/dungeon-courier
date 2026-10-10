"""folhas.py - folhas de conferencia dos sprites do Dungeon Courier (Python 3 + Pillow, fora do Blender e do Unity).

Uso (da raiz do repositorio):
  python client/tools/folhas.py <Resources/Sprites/ator> [...] [--carga <Resources/Sprites/caixa>] [--escala 0.5]
  python client/tools/folhas.py <ator1> <ator2> ... --elenco              (fila do elenco na mesma escala)
Saida: client/Builds/sprites_contact/<ator>_folhas.png (uma faixa por clipe) ou elenco.png.

Cada faixa: xadrez cinza, todas as linhas (direcoes) x quadros, linha do pivo (pe) em vermelho e rotulo com
quadros, fps, duracao, laco e passada; imprime a margem minima ate a borda (0 = recorte). Nos clipes Carry*, a carga (quadro Static do conjunto --carga) entra
no ponto das maos, por cima do portador se frente = 1 e por baixo se 0, como a View faz (RENDER_SPRITES.md sec. 4).
--escala 0.5 deixa a celula de 224 px com ~112 px: o tamanho do courier na tela de 1080 px.
"""
import argparse, json, os, sys
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Builds", "sprites_contact")


def load(d):
    with open(os.path.join(d, "meta.json")) as f:
        return json.load(f)


def checker(w, h, n=16):
    img = Image.new("RGBA", (w, h), (150, 150, 158, 255))
    dr = ImageDraw.Draw(img)
    for y in range(0, h, n):
        for x in range((y // n) % 2 * n, w, 2 * n):
            dr.rectangle([x, y, x + n - 1, y + n - 1], fill=(126, 126, 134, 255))
    return img


def strip(d, meta, clip, carga):
    s, rows, n = meta["size"], meta["rows"], clip["frames"]
    sheet = Image.open(os.path.join(d, clip["file"])).convert("RGBA")
    if sheet.size != (n * s, rows * s):
        sys.exit("ERRO %s: folha %s, esperado %dx%d" % (clip["file"], sheet.size, n * s, rows * s))
    img = checker(n * s, rows * s + 22)
    dr = ImageDraw.Draw(img)
    label = "%s  %d quadros x %d linhas (%s)  %.1f fps  %.2f s  %s" % (
        clip["name"], n, rows, ",".join(meta["dir_order"]), clip["fps"], clip["duration_s"], "laco" if clip["loop"] else "1x")
    if "stride_mps" in clip:
        label += "  passada %.2f m/s" % clip["stride_mps"]
    dr.rectangle([0, 0, n * s, 21], fill=(27, 23, 36, 255))
    dr.text((6, 5), label, fill=(247, 243, 234, 255))
    # margem do conteudo ate a borda da celula: 0 = encosta (recorte; suba a celula do preset). Contorno incluso
    al = sheet.split()[-1].point(lambda v: 255 if v > 8 else 0)
    boxes = [al.crop((i * s, r * s, i * s + s, r * s + s)).getbbox() for r in range(rows) for i in range(n)]
    margin = min((min(b[0], b[1], s - b[2], s - b[3]) for b in boxes if b), default=s)
    print("  %s margem minima %d px%s" % (clip["name"], margin, "  AVISO encosta na borda da celula" if margin <= 0 else ""))
    hands = clip.get("hands") if (carga and clip["name"].startswith("Carry")) else None  # so quem leva a carga
    if hands:  # a flag "frente" nao pode piscar dentro de uma linha (carga por cima/por baixo a cada quadro)
        print("  %s frente: %s" % (clip["name"], " ".join("%s=%s" % (meta["dir_order"][r], "".join(
            str(int(hands[(r * n + i) * 3 + 2])) for i in range(n))) for r in range(rows))))
    for r in range(rows):
        for i in range(n):
            x0, y0 = i * s, 22 + r * s
            cell = img.crop((x0, y0, x0 + s, y0 + s))
            k = (r * n + i) * 3
            box = None
            if hands:
                hx, hy, front = hands[k:k + 3]
                box = (int(round(hx - carga.width / 2)), int(round(s - hy - carga.height / 2)), front)
            if box and not box[2]:
                cell.alpha_composite(carga, (box[0], box[1]))
            cell.alpha_composite(sheet.crop((i * s, r * s, i * s + s, r * s + s)))
            if box and box[2]:
                cell.alpha_composite(carga, (box[0], box[1]))
            img.paste(cell, (x0, y0))
            py = y0 + s - int(round(meta["pivot_px"][1]))
            dr.line([x0, py, x0 + s - 1, py], fill=(255, 59, 48, 160))
            dr.rectangle([x0, y0, x0 + s - 1, y0 + s - 1], outline=(90, 90, 98, 255))
    return img


def carga_sprite(d):
    """Celula Static do conjunto da carga, recortada no conteudo (pivo no centro, como a View usa)."""
    m = load(d)
    img = Image.open(os.path.join(d, m["clips"][0]["file"])).convert("RGBA")
    return img.crop(img.getbbox())


def folhas(d, carga, escala):
    meta = load(d)
    strips = [strip(d, meta, c, carga) for c in meta["clips"]]
    w, h = max(s.width for s in strips), sum(s.height + 8 for s in strips)
    out = Image.new("RGBA", (w, h), (60, 62, 72, 255))
    y = 0
    for s in strips:
        out.paste(s, (0, y))
        y += s.height + 8
    if escala != 1:
        out = out.resize((max(1, int(out.width * escala)), max(1, int(out.height * escala))), Image.LANCZOS)
    path = os.path.join(OUT, os.path.basename(os.path.normpath(d)) + "_folhas.png")
    out.save(path)
    print("FOLHA %s %dx%d px (%d clipes)" % (path, out.width, out.height, len(strips)))


def elenco(dirs, escala):
    """1o quadro, linha 0 (S), do Idle de cada ator (ou do 1o clipe), pe na mesma linha; traco a cada 0,5 m."""
    cells = []
    for d in dirs:
        m = load(d)
        c = next((c for c in m["clips"] if c["name"] == "Idle"), m["clips"][0])
        img = Image.open(os.path.join(d, c["file"])).convert("RGBA").crop((0, 0, m["size"], m["size"]))
        cells.append((os.path.basename(os.path.normpath(d)), img, m["pivot_px"][1], m["ppu"]))
    ppu = cells[0][3]
    if any(abs(c[3] - ppu) > 0.01 for c in cells):
        print("AVISO ppu diferente no elenco: %s" % [(c[0], c[3]) for c in cells])
    top = max(c[1].height - c[2] for c in cells)
    bot = max(c[2] for c in cells)
    w = sum(c[1].width for c in cells)
    out = checker(w, int(top + bot) + 22)
    dr = ImageDraw.Draw(out)
    base = 22 + top
    for j in range(0, int(top / ppu * 2) + 1):
        y = base - j * ppu / 2
        dr.line([0, y, w, y], fill=(255, 59, 48, 255) if j == 0 else (60, 62, 72, 255))
        dr.text((2, y - 11), "%.1f m" % (j / 2), fill=(27, 23, 36, 255))
    x = 0
    for name, img, piv, _ in cells:
        out.alpha_composite(img, (x, int(base - (img.height - piv))))
        dr.text((x + 30, 5), name, fill=(27, 23, 36, 255))
        x += img.width
    if escala != 1:
        out = out.resize((int(out.width * escala), int(out.height * escala)), Image.LANCZOS)
    path = os.path.join(OUT, "elenco.png")
    out.save(path)
    print("ELENCO %s %dx%d px" % (path, out.width, out.height))


def main():
    p = argparse.ArgumentParser()
    p.add_argument("dirs", nargs="+")
    p.add_argument("--carga", help="conjunto do prop da carga (ex.: Resources/Sprites/caixa)")
    p.add_argument("--escala", type=float, default=0.5)
    p.add_argument("--elenco", action="store_true")
    a = p.parse_args()
    os.makedirs(OUT, exist_ok=True)
    if a.elenco:
        elenco(a.dirs, a.escala)
        return
    carga = carga_sprite(a.carga) if a.carga else None
    for d in a.dirs:
        folhas(d, carga, a.escala)


main()
