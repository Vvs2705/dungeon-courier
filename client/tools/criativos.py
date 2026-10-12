"""criativos.py - monta os criativos de UA do Dungeon Courier (docs/CRIATIVOS.md) a partir das gravacoes do jogo real.

So gameplay real: cada cena e um trecho de uma gravacao `-record` do build (Game.cs), o bot ou uma pessoa jogando. Por cima vai so a
legenda (drawtext) e, no fim, o CTA de 1,5 s (logo da tela de titulo + "Baixe gratis" no idioma). Som: o do jogo gravado
(audio.wav de cada gravacao); sem ele, silencio. Nenhuma musica de terceiros.

Uso (da raiz do repositorio; ffmpeg >= 6.1 e ffprobe no PATH ou no WinGet do Gyan):
  python -I client/tools/criativos.py --comandos [--roteiro 1,11] [--lang pt] [--exe <DungeonCourier.exe>]
        imprime os comandos de gravacao (uma linha por gravacao e idioma), para rodar no build Windows
  python -I client/tools/criativos.py --roteiro all --lang pt [--rec client/Builds/rec] [--saida client/Builds/criativos] [--limpar]
        monta dc_NN_<slug>_<lang>.mp4 (H.264 1080x1920 30 fps, AAC 48 kHz); --limpar apaga as pastas de quadros usadas no fim
  python -I client/tools/criativos.py --listar          roteiros, duracao e estado (bot / humano / pendente)
  python -I client/tools/criativos.py --autoteste       confere os roteiros e monta um video de 2 s de quadros sinteticos

Pastas de gravacao: <rec>/<gravacao>_<lang>/00000.png... (+ audio.wav). Sem a do idioma, usa a _en e depois a _pt (o HUD fica no
outro idioma; o ideal e gravar no idioma do video). Fonte: Inter SemiBold (SIL OFL) da instalacao do Unity, DejaVu Sans Bold no
Linux, ou --fonte.
"""
import argparse, glob, json, math, os, shutil, struct, subprocess, sys, tempfile, wave
from PIL import Image, ImageDraw, ImageFont

CLIENT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REC = os.path.join(CLIENT, "Builds", "rec")
SAIDA = os.path.join(CLIENT, "Builds", "criativos")
LOGO = os.path.join(CLIENT, "Builds", "loja", "logo.png")          # gerado pelo loja_assets.py
EXE = os.path.join(CLIENT, "Builds", "win-release", "DungeonCourier.exe")
FPS, W, H, CTA = 30, 1080, 1920, 1.5
LANGS = ("pt", "en", "es", "id")
BG, INK, GOLD, TINTA, VERDE, SOMBRA = "0x14161F", "0xF7F3EA", "0xE0B23A", "0x1B1724", "0x45C46B", "0x0E0F18"   # Art.cs
LEG_Y = {"topo": 330, "baixo": 1290}          # topo: logo abaixo do HUD; baixo: quando as portas do topo sao o assunto
LEG_TAM, LEG_MIN, LEG_LARG = 68, 44, 900      # px; largura maxima longe da coluna de botoes das redes (direita)

# Gravacoes: nome -> (flags do jogo, -recordfrom s, -recordsecs s, nota). Os tempos sao do contrato (semente 1, Bot(1), guilda 0),
# medidos em 2026-10-11 no Core da worktree feat/lancamento. Se o Core mudar, confira com 1 foto (-shot -shotdelay) antes de gravar.
G = {
    "c01": ("-contract 1 -bot", 9.5, 16.5, "R3 ladrao 14,2; entrega 20,3; cena do cliente 21,4-25,0 (perfeita)"),
    "c02": ("-contract 2 -bot", 0.5, 17.0, "R3 3,2; pousa 5,4; ladrao 7,0; dash 7,1; entrega 12,4; cena 13,5-17,1"),
    "c03": ("-contract 3 -bot", 7.5, 10.0, "portas no R1; entra no R4 (3 caveiras) 11,0; cai 13,9; resultado 15,0"),
    "c04_inicio": ("-contract 4 -bot", 0.0, 13.0, "R5 com Bruto; bau 5,4; R3 7,3; bau 11,7"),
    "c04_fim": ("-contract 4 -bot", 49.5, 17.5, "vida 2; vida 1 50,2; S1 52,7 (+1); vida 1 54,4; D1 56,4 (+1: entrega com 2); entrega 59,0; cena 60,1-63,7"),
    "c05_bumerangue": ("-contract 5 -bot -weapon 1", 20.5, 9.0, "reliquia: rachaduras 22,5 / 24,3 / 25,5 (quebra); resultado 26,6"),
    "c06_bomba": ("-contract 6 -bot -weapon 2", 0.5, 10.5, "explosoes racham o ovo 3,2 / 4,3 / 9,8"),
    "c06_bumerangue": ("-contract 6 -bot -weapon 1", 0.5, 6.5, "R6: ladrao 4,2; dash 4,9"),
    "c06_bumerangue_fim": ("-contract 6 -bot -weapon 1", 56.5, 4.5, "entrega 55,7; cena 56,8-60,4 (ovo intacto)"),
    "c07": ("-contract 7 -bot", 32.5, 15.5, "bolo, vida 1; bau 38,0; R2 40,1 (vida 3); vida 1 41,8; cai 42,8; resultado 43,9"),
    "c08": ("-contract 8 -bot", 0.0, 9.5, "R7 arqueiros; reforco 3,4; 2 rachaduras 7,5 / 7,6"),
    "c08_fim": ("-contract 8 -bot", 76.0, 5.0, "entrega 75,5; cena 76,6-80,2 (2 rachaduras)"),
    "c12_r4": ("-contract 12 -bot", 0.0, 5.0, "R4: bau 2,0; dano 3,2; reforco 4,1"),
    "c12_r14": ("-contract 12 -bot", 24.3, 5.0, "R14 24,6; dano 28,5; bau 28,8"),
    "c12_m1": ("-contract 12 -bot", 43.0, 10.5, "M1 43,5; Capitao chama 46,5-50,1; foge com a carga 51,1; resultado 52,2"),
    "c24_ogro": ("-contract 24 -bot -weapon 1", 112.0, 7.0, "B1: pancada 113,0/114,0; ladrao 114,8; dash 115,0"),
    "c24_fim": ("-contract 24 -bot -weapon 1", 185.5, 17.0, "ladrao+dash 186,1-188,8; Ogro cai ~192; entrega 197,0; cena 198,1-201,7"),
    "quadro": ("-tela quadro", 0.0, 2.0, "quadro de contratos parado"),
    "cod_ovo": ("-codigo JAAKNB -bot", 120.5, 21.5, "contrato por codigo da regiao 2 (ovo, 5 camadas): R2-6 121,2; explosoes 123,7; "
                "choca 125,0; bau 132,6; D2 134,8; entrega 137,0; cena 138,1-141,7 (chocou e o filhote chegou)"),
    "c29_coracao": ("-contract 29 -bot", 0.0, 14.0, "M2-5, coracao arcano: R2-3 reforco 1,8, dano 2,2/3,4, bau 4,1; R2-1 5,2; R2-10 8,5; "
                    "R2-8 13,2"),
    "c02_roupa": ("-contract 2 -bot -roupa noturno", 0.5, 17.0, "o c02 com a roupa Noturno (a roupa nao muda a partida)"),
}

# Roteiros (docs/CRIATIVOS.md sec. 3). cenas: (gravacao, de s, ate s) no tempo da gravacao. legendas: (de, ate, pt, en, es, id) no
# tempo do video (sem o CTA). pos: onde vai a legenda.
R = {
    1: dict(slug="carga_fragil", pos="topo", cenas=[("c08", 0.5, 8.5), ("c05_bumerangue", 0.5, 8.5)], legendas=[
        (0, 3, "Relíquia frágil. Arqueiros na sala.", "Fragile relic. Archers in the room.", "Reliquia frágil. Arqueros en la sala.",
         "Relik rapuh. Ada pemanah di ruangan."),
        (3, 8, "Cada golpe deixa uma rachadura", "Every hit leaves a crack", "Cada golpe deja una grieta", "Tiap pukulan bikin retakan"),
        (8, 12.5, "3 rachaduras...", "3 cracks...", "3 grietas...", "3 retakan..."),
        (12.5, 16, "...quebrou! Você entregaria inteira?", "...broken! Could you deliver it whole?",
         "...¡se rompió! ¿Tú la entregarías entera?", "...pecah! Kamu bisa antar utuh?")]),
    2: dict(slug="duas_portas", pos="baixo", cenas=[("c04_inicio", 0.0, 12.5), ("c04_fim", 14.3, 17.3)], legendas=[
        (0, 3, "Porta segura ou porta do tesouro?", "Safe door or treasure door?", "¿Puerta segura o puerta del tesoro?",
         "Pintu aman atau pintu harta?"),
        (3, 7.3, "Primeiro, o baú desta sala...", "First, this room's chest...", "Primero, el cofre de esta sala...",
         "Pertama, peti di ruangan ini..."),
        (7.3, 12.5, "...depois, a porta com outro baú", "...then the door with another chest", "...luego, la puerta con otro cofre",
         "...lalu pintu dengan peti lain"),
        (12.5, 15.5, "Baús pagam. Mas cada porta cobra.", "Chests pay. But every door has a price.",
         "Los cofres pagan. Pero cada puerta cobra.", "Peti memberi koin. Tapi tiap pintu ada harganya.")]),
    3: dict(slug="um_de_vida", pos="topo", cenas=[("c04_fim", 0.5, 14.2)], legendas=[
        (0, 4.4, "Quase sem vida, com a caixa nas mãos", "Almost out of health, crate in hand", "Casi sin vida, con la caja en las manos",
         "Nyawa hampir habis, kotak di tangan"),
        (4.4, 6.4, "Sobrou 1 coração...", "Down to 1 heart...", "Queda 1 corazón...", "Tinggal 1 nyawa..."),
        (6.4, 9, "Porta nova: +1 coração. Corre!", "New room: +1 heart. Run!", "Sala nueva: +1 corazón. ¡Corre!", "Ruang baru: +1 nyawa. Lari!"),
        (9, 13.7, "ENTREGUE por um triz!", "DELIVERED by a hair!", "¡ENTREGADO por un pelo!", "TERKIRIM nyaris saja!")]),
    4: dict(slug="coracao_arcano", pos="topo", cenas=[("c29_coracao", 0.0, 13.5)], legendas=[
        (0, 4, "Coração arcano: puxa moedas...", "Arcane heart: pulls in coins...", "Corazón arcano: atrae monedas...",
         "Jantung arkana: menarik koin..."),
        (4, 9, "...e puxa monstros também", "...and pulls in monsters too", "...y también atrae monstruos", "...dan juga menarik monster"),
        (9, 13.5, "Vale o risco?", "Worth the risk?", "¿Vale el riesgo?", "Sepadan risikonya?")]),
    5: dict(slug="ganancioso", pos="topo", cenas=[("c07", 0.5, 15.0)], legendas=[
        (0, 3, "1 coração e um bolo de casamento...", "1 heart and a wedding cake...", "1 corazón y un pastel de bodas...",
         "1 nyawa dan kue pengantin..."),
        (3, 7.1, "...mas olha aquele baú!", "...but look at that chest!", "...¡pero mira ese cofre!", "...tapi lihat peti itu!"),
        (7.1, 10.9, "A ganância custa caro", "Greed costs a lot", "La codicia sale cara", "Serakah itu mahal"),
        (10.9, 14.5, "A carga fica na masmorra:\nvolte para resgatar", "The cargo stays in the dungeon:\ncome back for it",
         "La carga se queda en la mazmorra:\nvuelve por ella", "Paket tertinggal di dungeon:\nkembali selamatkan")]),
    6: dict(slug="rota_lotada", pos="baixo", cenas=[("c03", 0.5, 9.5), ("c01", 0.5, 11.5)], legendas=[
        (0, 3, "3 caveiras ou 2 caveiras?", "3 skulls or 2 skulls?", "¿3 calaveras o 2 calaveras?", "3 tengkorak atau 2 tengkorak?"),
        (3, 9, "Escolheu a lotada...\ncaiu em 3 segundos", "Picked the crowded one...\ndown in 3 seconds",
         "Eligió la llena...\ncayó en 3 segundos", "Pilih yang ramai...\ntumbang dalam 3 detik"),
        (9, 13, "Agora: a porta de 1 caveira", "Now: the 1-skull door", "Ahora: la puerta de 1 calavera", "Sekarang: pintu 1 tengkorak"),
        (13, 20, "Rota certa, carga entregue", "Right route, cargo delivered", "Ruta correcta, carga entregada",
         "Rute tepat, paket terkirim")]),
    7: dict(slug="ovo_e_bombinha", pos="topo",
            cenas=[("c06_bomba", 0.5, 10.0), ("c06_bumerangue", 0.0, 6.0), ("c06_bumerangue_fim", 0.3, 3.9)], legendas=[
        (0, 3, "Bombinha perto do ovo?", "Little bomb next to the egg?", "¿Bombita junto al huevo?", "Bom kecil di dekat telur?"),
        (3, 9.5, "Cada explosão racha o ovo", "Every blast cracks the egg", "Cada explosión agrieta el huevo", "Tiap ledakan bikin telur retak"),
        (9.5, 15.5, "Troque a arma: bumerangue", "Switch weapons: boomerang", "Cambia de arma: bumerán", "Ganti senjata: bumerang"),
        (15.5, 19.1, "Ovo intacto, cliente feliz", "Egg intact, happy client", "Huevo intacto, cliente feliz", "Telur utuh, pelanggan senang")]),
    8: dict(slug="dash_e_ogro", pos="topo", cenas=[("c24_ogro", 0.5, 6.5), ("c24_fim", 0.0, 7.5), ("c24_fim", 12.6, 16.2)], legendas=[
        (0, 3, "O Ogro bate. O ladrão rouba.", "The Ogre smashes. The thief steals.", "El Ogro golpea. El ladrón roba.",
         "Ogre menghantam. Pencuri mencuri."),
        (3, 6, "Dash! A carga volta para as mãos", "Dash! The cargo is back in your hands", "¡Dash! La carga vuelve a tus manos",
         "Dash! Paket kembali ke tangan"),
        (6, 13.5, "Pouse, lute, pegue de novo", "Set it down, fight, grab it again", "Déjala, lucha, tómala otra vez",
         "Taruh, lawan, ambil lagi"),
        (13.5, 17.1, "Chefe vencido. Carga perfeita.", "Boss down. Perfect cargo.", "Jefe vencido. Carga perfecta.",
         "Bos kalah. Paket sempurna.")]),
    9: dict(slug="cliente_reage", pos="topo",
            cenas=[("c08", 5.0, 9.0), ("c08_fim", 0.6, 4.2), ("c01", 3.0, 6.0), ("c01", 11.9, 15.5)], legendas=[
        (0, 4, "Relíquia levando golpe...", "The relic is taking hits...", "La reliquia recibe golpes...", "Relik kena pukulan..."),
        (4, 7.6, "O cliente abre a caixa: 2 rachaduras", "The client opens the box: 2 cracks", "El cliente abre la caja: 2 grietas",
         "Pelanggan buka kotak: 2 retakan"),
        (7.6, 10.6, "Agora, protegendo a caixa...", "Now, guarding the crate...", "Ahora, cuidando la caja...", "Sekarang, jaga kotaknya..."),
        (10.6, 14.2, "Perfeito! O cliente comemora", "Perfect! The client cheers", "¡Perfecto! El cliente celebra",
         "Sempurna! Pelanggan bersorak")]),
    10: dict(slug="simples_vira_caos", pos="topo", cenas=[
        ("c12_m1", 3.5, 6.5), ("quadro", 0.3, 1.8), ("c12_r4", 0.0, 4.5), ("c12_r14", 0.3, 4.7), ("c12_m1", 0.5, 10.0)], legendas=[
        (0, 3, "Era só levar uma caixa...", "It was just one crate...", "Era solo llevar una caja...", "Cuma antar satu kotak..."),
        (3, 4.5, "Contrato: leve a caixa", "Contract: deliver the crate", "Contrato: lleva la caja", "Kontrak: antar kotaknya"),
        (4.5, 13.4, "Baús, emboscadas, ladrões...", "Chests, ambushes, thieves...", "Cofres, emboscadas, ladrones...",
         "Peti, sergapan, pencuri..."),
        (13.4, 22.9, "...e o Capitão Ladrão\nchama reforços!", "...and the Thief Captain\ncalls for backup!",
         "...¡y el Capitán Ladrón\npide refuerzos!", "...dan Kapten Pencuri\nmemanggil bantuan!")]),
    11: dict(slug="ladrao_e_dash", pos="topo", cenas=[("c02", 0.5, 11.9), ("c02", 13.0, 16.6)], legendas=[
        (0, 4.4, "Com a carga nas mãos, só dá dash", "Cargo in hand: you can only dash", "Con la carga en las manos, solo dash",
         "Paket di tangan: cuma bisa dash"),
        (4.4, 5.8, "Parou: pousa e luta", "Stop: set it down and fight", "Para: déjala y lucha", "Berhenti: taruh dan lawan"),
        (5.8, 8.6, "Ladrão na carga? Dash nele!", "Thief on your cargo? Dash him!", "¿Ladrón en tu carga? ¡Dash contra él!",
         "Pencuri ambil paket? Dash dia!"),
        (8.6, 11.4, "Pegue de volta e corra", "Grab it back and run", "Recupérala y corre", "Ambil lagi dan lari"),
        (11.4, 15, "Entregue!", "Delivered!", "¡Entregado!", "Terkirim!")]),
    12: dict(slug="ovo_choca", pos="topo", cenas=[("cod_ovo", 0.5, 16.5), ("cod_ovo", 17.6, 21.2)], legendas=[
        (0, 4, "O ovo choca se você demorar...", "The egg hatches if you take too long...", "El huevo eclosiona si tardas...",
         "Telurnya menetas kalau kamu lama..."),
        (4, 9, "Chocou! O filhote agora te segue", "It hatched! Now the chick follows you", "¡Eclosionó! Ahora la cría te sigue",
         "Menetas! Anak naga mengikutimu"),
        (9, 16, "Leve o filhote até o cliente", "Get the chick to the client", "Lleva la cría al cliente",
         "Antar anak naga ke pelanggan"),
        (16, 19.6, "Entregue com o filhote!", "Delivered with the chick!", "¡Entregado con la cría!", "Terkirim bersama anak naga!")]),
}
# 13 = o 11 com outra roupa do courier (o mascote saiu): mesma montagem e legendas, so a gravacao muda (teste A/B de roupa)
R[13] = dict(R[11], slug="ladrao_e_dash_roupa", cenas=[("c02_roupa", a, b) for _, a, b in R[11]["cenas"]])

CTA_TXT = {"pt": ("Baixe grátis", "Sem energia. Sem gacha."), "en": ("Download free", "No energy. No gacha."),
           "es": ("Descarga gratis", "Sin energía. Sin gacha."), "id": ("Unduh gratis", "Tanpa energi. Tanpa gacha.")}


# ------------------------------------------------------------------ utilidades

def binario(nome):
    p = shutil.which(nome)
    if p:
        return p
    for p in glob.glob(os.path.expandvars(r"%LOCALAPPDATA%\Microsoft\WinGet\Packages\Gyan.FFmpeg*\*\bin\%s.exe" % nome)):
        return p
    sys.exit("ERRO: %s nao encontrado (PATH ou WinGet Gyan.FFmpeg)" % nome)


def fonte_padrao():
    cands = sorted(glob.glob(r"C:\Program Files\Unity\Hub\Editor\*\Editor\Data\Resources\Fonts\Inter-SemiBold.ttf"), reverse=True)
    cands += ["/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", "/usr/share/fonts/TTF/DejaVuSans-Bold.ttf"]
    for c in cands:
        if os.path.isfile(c):
            return c
    sys.exit("ERRO: nenhuma fonte livre achada (Inter do Unity ou DejaVu); passe --fonte <arquivo.ttf>")


def duracao(n):
    return sum(b - a for _, a, b in R[n]["cenas"])


def quadros(t):
    return int(round(t * FPS))


def tamanho(texto, fonte, minimo=LEG_MIN):
    """Maior corpo (LEG_TAM..minimo) em que todas as linhas cabem em LEG_LARG px; None se nem o minimo cabe."""
    for tam in range(LEG_TAM, minimo - 1, -2):
        f = ImageFont.truetype(fonte, tam)
        if all(f.getbbox(l)[2] - f.getbbox(l)[0] <= LEG_LARG for l in texto.split("\n")):
            return tam
    return None


def ajusta(s, fonte):
    """(texto, corpo): a frase em 1 linha se couber com 56 px ou mais; senao, 2 linhas quebradas no espaco mais perto do meio."""
    t = tamanho(s, fonte, 56)
    if t or "\n" in s or " " not in s:
        return s, t or tamanho(s, fonte)
    i = min((k for k, c in enumerate(s) if c == " "), key=lambda k: abs(k - len(s) / 2))
    s = s[:i] + "\n" + s[i + 1:]
    return s, tamanho(s, fonte)


def pasta(rec, nome, lang):
    """Pasta da gravacao no idioma; sem ela, a _en e depois a _pt (HUD em outro idioma)."""
    for l in (lang, "en", "pt"):
        p = os.path.join(rec, "%s_%s" % (nome, l))
        if os.path.isfile(os.path.join(p, "00000.png")):
            return p
    return None


def texto(tmp, nome, s):
    with open(os.path.join(tmp, nome), "w", encoding="utf-8", newline="\n") as f:
        f.write(s)
    return nome


def drawtext(arq, tam, cor, y, extra=""):
    # caminhos relativos ao diretorio de trabalho do ffmpeg (sem escapar C:\ nem espacos no filtro)
    return ("drawtext=fontfile=fonte.ttf:textfile=%s:expansion=none:fontsize=%d:fontcolor=%s:text_align=C:line_spacing=12:"
            "x=(w-text_w)/2:y=%d%s" % (arq, tam, cor, y, extra))


# ------------------------------------------------------------------ montagem

def montar(n, lang, rec, saida, fonte, logo, filtro="lanczos"):
    """Monta o roteiro n no idioma; devolve (mp4, pastas usadas)."""
    rot = R[n]
    usadas, args, fc = [], [], []
    k = 0
    for i, (g, a, b) in enumerate(rot["cenas"]):
        d = pasta(rec, g, lang)
        if not d:
            sys.exit("ERRO roteiro %d: falta a gravacao %s_%s (python -I client/tools/criativos.py --comandos --roteiro %d --lang %s)"
                     % (n, g, lang, n, lang))
        ini, nq = quadros(a), quadros(b) - quadros(a)
        tem = len(glob.glob(os.path.join(d, "*.png")))
        if ini + nq > tem:
            sys.exit("ERRO roteiro %d: a cena pede ate o quadro %d e %s tem %d (regrave com -recordsecs maior)" % (n, ini + nq, d, tem))
        if d not in usadas:
            usadas.append(d)
        args += ["-framerate", str(FPS), "-start_number", str(ini), "-i", os.path.join(d, "%05d.png")]
        fc.append("[%d:v]trim=end_frame=%d,setpts=PTS-STARTPTS,scale=%d:%d:force_original_aspect_ratio=decrease:flags=%s,"
                  "pad=%d:%d:(ow-iw)/2:(oh-ih)/2:color=%s,setsar=1,fps=%d,format=yuv420p[v%d]" % (k, nq, W, H, filtro, W, H, BG, FPS, i))
        k += 1
        dur = nq / FPS
        wav = os.path.join(d, "audio.wav")
        if os.path.isfile(wav):
            args += ["-ss", "%.4f" % (ini / FPS), "-t", "%.4f" % dur, "-i", wav]
        else:
            args += ["-f", "lavfi", "-t", "%.4f" % dur, "-i", "anullsrc=r=48000:cl=stereo"]
        fc.append("[%d:a]aresample=48000,aformat=sample_fmts=fltp:channel_layouts=stereo,apad,atrim=0:%.4f,asetpts=PTS-STARTPTS[a%d]"
                  % (k, dur, i))
        k += 1
    jogo = sum(quadros(b) - quadros(a) for _, a, b in rot["cenas"]) / FPS

    tmp = tempfile.mkdtemp(prefix="dc_criativo_")
    try:
        shutil.copyfile(fonte, os.path.join(tmp, "fonte.ttf"))
        # legendas (sobre o jogo concatenado; o CTA vem depois e nao recebe legenda)
        leg = []
        for j, l in enumerate(rot["legendas"]):
            s, tam = ajusta(l[2 + LANGS.index(lang)], fonte)
            leg.append(drawtext(texto(tmp, "l%d.txt" % j, s), tam, INK, LEG_Y[rot["pos"]],
                                ":borderw=7:bordercolor=%s:box=1:boxcolor=%s@0.55:boxborderw=18:enable='between(t,%.3f,%.3f)'"
                                % (SOMBRA, SOMBRA, l[0], min(l[1], jogo))))
        # CTA: fundo Bg + logo da tela de titulo (ou o nome em texto) + "Baixe gratis" no botao verde + "sem energia, sem gacha"
        c1, c2 = CTA_TXT[lang]
        args += ["-f", "lavfi", "-t", str(CTA), "-i", "color=c=%s:s=%dx%d:r=%d" % (BG, W, H, FPS)]
        cta_v = k
        k += 1
        if logo and os.path.isfile(logo):
            shutil.copyfile(logo, os.path.join(tmp, "logo.png"))
            args += ["-loop", "1", "-framerate", str(FPS), "-t", str(CTA), "-i", "logo.png"]
            fc.append("[%d:v]scale=860:-1:flags=lanczos,format=rgba[lg]" % k)
            fc.append("[%d:v][lg]overlay=x=(W-w)/2:y=300[ctab]" % cta_v)
            k += 1
        else:
            fc.append("[%d:v]%s,%s[ctab]" % (cta_v, drawtext(texto(tmp, "n1.txt", "DUNGEON"), 150, INK, 520),
                                              drawtext(texto(tmp, "n2.txt", "COURIER"), 150, GOLD, 700)))
        fc.append("[ctab]%s,%s,setsar=1,format=yuv420p[vc]" % (
            drawtext(texto(tmp, "c1.txt", c1), 96, TINTA, 1250, ":box=1:boxcolor=%s:boxborderw=36" % VERDE),
            drawtext(texto(tmp, "c2.txt", c2), 56, INK, 1460)))
        args += ["-f", "lavfi", "-t", str(CTA), "-i", "anullsrc=r=48000:cl=stereo"]
        fc.append("[%d:a]aformat=sample_fmts=fltp:channel_layouts=stereo[ac]" % k)
        n_c = len(rot["cenas"])
        fc.append("%s[vc][ac]concat=n=%d:v=1:a=1[vcat][aout]" % ("".join("[v%d][a%d]" % (i, i) for i in range(n_c)), n_c + 1))
        fc.append("[vcat]%s[vout]" % ",".join(leg) if leg else "[vcat]null[vout]")

        os.makedirs(saida, exist_ok=True)
        out = os.path.abspath(os.path.join(saida, "dc_%02d_%s_%s.mp4" % (n, rot["slug"], lang)))
        cmd = [binario("ffmpeg"), "-hide_banner", "-loglevel", "error", "-y"] + args + [
            "-filter_complex", ";".join(fc), "-map", "[vout]", "-map", "[aout]",
            "-c:v", "libx264", "-preset", "medium", "-crf", "20", "-pix_fmt", "yuv420p", "-r", str(FPS),
            "-c:a", "aac", "-b:a", "128k", "-ar", "48000", "-movflags", "+faststart", out]
        r = subprocess.run(cmd, cwd=tmp, capture_output=True, text=True)
        if r.returncode != 0:
            sys.exit("ERRO ffmpeg no roteiro %d (%s):\n%s" % (n, lang, r.stderr[-3000:]))
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    info = sonda(out)
    esperado = jogo + CTA
    ok = info["w"] == W and info["h"] == H and info["fps"] == FPS and abs(info["dur"] - esperado) <= 0.1 and info["audio"]
    print("%s %s %dx%d %.2f s (esperado %.2f) %s fps, audio=%s" % ("CRIATIVO" if ok else "ERRO", out, info["w"], info["h"],
                                                                  info["dur"], esperado, info["fps"], info["audio"]))
    if not ok:
        sys.exit(1)
    return out, usadas


def sonda(mp4):
    r = subprocess.run([binario("ffprobe"), "-v", "error", "-show_entries", "stream=codec_type,codec_name,width,height,r_frame_rate,pix_fmt",
                        "-show_entries", "format=duration", "-of", "json", mp4], capture_output=True, text=True, check=True)
    j = json.loads(r.stdout)
    v = next(s for s in j["streams"] if s["codec_type"] == "video")
    num, den = v["r_frame_rate"].split("/")
    return dict(w=v["width"], h=v["height"], fps=round(int(num) / int(den), 3), dur=float(j["format"]["duration"]), codec=v["codec_name"],
                pix=v["pix_fmt"], audio=any(s["codec_type"] == "audio" and s["codec_name"] == "aac" for s in j["streams"]))


# ------------------------------------------------------------------ comandos de gravacao, lista

def estado(n):
    notas = [G[g][3] for g, _, _ in R[n]["cenas"]]
    return "pendente" if any(x.startswith("PENDENTE") for x in notas) else "humano" if any(x.startswith("HUMANO") for x in notas) else "bot"


def comandos(ns, langs, rec, exe):
    vistos = []
    for n in ns:
        for g, _, _ in R[n]["cenas"]:
            if g not in vistos:
                vistos.append(g)
    for lang in langs:
        print("# --- %s (%d gravacoes)" % (lang, len(vistos)))
        for g in vistos:
            flags, de, secs, nota = G[g]
            if nota.startswith(("HUMANO", "PENDENTE")):
                print("# %s: %s" % (g, nota))
            print('"%s" -screen-width 540 -screen-height 960 -screen-fullscreen 0 -testsession -lang %s %s -record "%s" -recordfrom %s'
                  ' -recordsecs %s' % (exe, lang, flags, os.path.join(rec, "%s_%s" % (g, lang)), de, secs))


def listar():
    for n in sorted(R):
        r = R[n]
        print("%2d %-22s %5.1f s  %-8s %s" % (n, r["slug"], duracao(n) + CTA, estado(n), " + ".join("%s[%g-%g]" % c for c in r["cenas"])))


# ------------------------------------------------------------------ autoteste

def conferir_roteiros(fonte):
    assert sorted(R) == list(range(1, 14)), "devem ser os 13 roteiros do GDD sec. 17"
    assert set(CTA_TXT) == set(LANGS) and all(all(t) for t in CTA_TXT.values())
    for n, r in R.items():
        total = duracao(n) + CTA
        assert 15 <= total <= 30, "roteiro %d com %.1f s (15-30)" % (n, total)
        for g, a, b in r["cenas"]:
            assert g in G, "roteiro %d: gravacao %s sem entrada em G" % (n, g)
            assert 0 <= a < b <= G[g][2] + 1e-6, "roteiro %d: cena %s %g-%g fora da gravacao (%g s)" % (n, g, a, b, G[g][2])
        assert r["pos"] in LEG_Y and r["legendas"] and r["legendas"][0][0] == 0, "roteiro %d: o gancho (0-3 s) precisa de legenda" % n
        for l in r["legendas"]:
            assert 0 <= l[0] < l[1] <= duracao(n) + 1e-6, "roteiro %d: legenda %s fora do video" % (n, l[:2])
            for s, lang in zip(l[2:], LANGS):
                assert s.strip(), "roteiro %d: legenda vazia em %s" % (n, lang)
                assert ajusta(s, fonte)[1], "roteiro %d (%s): '%s' nao cabe em %d px nem em 2 linhas de %d px" % (n, lang, s, LEG_LARG, LEG_MIN)
    for g, (flags, de, secs, _) in G.items():
        assert "-record" not in flags and de >= 0 and secs > 0, g


def autoteste(fonte):
    conferir_roteiros(fonte)
    tmp = tempfile.mkdtemp(prefix="dc_criativos_teste_")
    salvo = dict(R), dict(G)
    try:
        rec = os.path.join(tmp, "rec")
        for nome, n, som in (("a_pt", 12, True), ("b_en", 12, False)):    # b so em en: testa a queda es -> en; a: es -> en -> pt
            d = os.path.join(rec, nome)
            os.makedirs(d)
            for i in range(n):
                im = Image.new("RGB", (540, 960), (20 + 15 * i, 60, 120 if som else 40))
                ImageDraw.Draw(im).rectangle((200, 400, 340, 560), fill=(255, 225, 77))
                im.save(os.path.join(d, "%05d.png" % i))
            if som:
                with wave.open(os.path.join(d, "audio.wav"), "wb") as w:
                    w.setnchannels(2); w.setsampwidth(2); w.setframerate(48000)
                    w.writeframes(b"".join(struct.pack("<hh", v, v) for v in (int(8000 * math.sin(2 * math.pi * 440 * t / 48000))
                                                                               for t in range(int(48000 * n / FPS)))))
        logo = os.path.join(tmp, "logo.png")
        Image.new("RGBA", (460, 420), (243, 226, 192, 255)).save(logo)
        G["a"] = ("-contract 1 -bot", 0, 12 / FPS, "")
        G["b"] = ("-contract 1 -bot", 0, 12 / FPS, "")
        R[99] = dict(slug="autoteste", pos="topo", cenas=[("a", 0.0, 0.3), ("b", 0.1, 0.3)],
                     legendas=[(0, 0.5, "Teste: ação!", "Test: action!", "Prueba: ¡acción!", "Uji: aksi!")])
        out, usadas = montar(99, "es", rec, os.path.join(tmp, "out"), fonte, logo)
        info = sonda(out)
        assert (info["w"], info["h"], info["fps"], info["codec"], info["pix"]) == (W, H, FPS, "h264", "yuv420p"), info
        assert abs(info["dur"] - 2.0) <= 0.05 and info["audio"], info
        assert [os.path.basename(u) for u in usadas] == ["a_pt", "b_en"], usadas
        # sem logo: o CTA escreve o nome
        out2, _ = montar(99, "id", rec, os.path.join(tmp, "out"), fonte, None, "neighbor")
        assert abs(sonda(out2)["dur"] - 2.0) <= 0.05
    finally:
        R.clear(); R.update(salvo[0]); G.clear(); G.update(salvo[1])
        shutil.rmtree(tmp, ignore_errors=True)
    print("autoteste OK (13 roteiros conferidos; video de 2 s 1080x1920 30 fps H.264 + AAC)")


# ------------------------------------------------------------------

def lista(v, todos, conv=str):
    return list(todos) if v in (None, "all") else [conv(x) for x in v.split(",")]


if __name__ == "__main__":
    ap = argparse.ArgumentParser(description="Criativos de UA do Dungeon Courier (gravacoes -record do jogo real + ffmpeg).")
    ap.add_argument("--roteiro", default="all", help="1..13, lista com virgula, ou all")
    ap.add_argument("--lang", default="pt", help="pt, en, es, id, lista com virgula, ou all")
    ap.add_argument("--rec", default=REC, help="pasta das gravacoes (padrao client/Builds/rec)")
    ap.add_argument("--saida", default=SAIDA, help="pasta dos mp4 (padrao client/Builds/criativos)")
    ap.add_argument("--logo", default=LOGO, help="logo do CTA (padrao: client/Builds/loja/logo.png do loja_assets.py)")
    ap.add_argument("--fonte", default=None, help="fonte .ttf livre (padrao: Inter SemiBold do Unity ou DejaVu Sans Bold)")
    ap.add_argument("--escala", choices=("lanczos", "nearest"), default="lanczos", help="540x960 -> 1080x1920")
    ap.add_argument("--exe", default=EXE, help="build para o --comandos (padrao: client/Builds/win-release/DungeonCourier.exe)")
    ap.add_argument("--limpar", action="store_true", help="apaga as pastas de quadros usadas depois de montar tudo")
    ap.add_argument("--comandos", action="store_true")
    ap.add_argument("--listar", action="store_true")
    ap.add_argument("--autoteste", action="store_true")
    o = ap.parse_args()
    ns, langs = lista(o.roteiro, sorted(R), int), lista(o.lang, LANGS)
    for x in ns:
        if x not in R:
            sys.exit("ERRO: roteiro %s nao existe (1..13)" % x)
    for x in langs:
        if x not in LANGS:
            sys.exit("ERRO: idioma %s (pt, en, es, id)" % x)
    if o.autoteste:
        autoteste(o.fonte or fonte_padrao())
    elif o.listar:
        listar()
    elif o.comandos:
        comandos(ns, langs, o.rec, o.exe)
    else:
        fonte, todas = o.fonte or fonte_padrao(), []
        for n in ns:
            for lang in langs:
                _, usadas = montar(n, lang, o.rec, o.saida, fonte, o.logo, "neighbor" if o.escala == "nearest" else "lanczos")
                todas += [u for u in usadas if u not in todas]
        if o.limpar:
            mb = sum(os.path.getsize(os.path.join(d, f)) for d in todas for f in os.listdir(d)) / 1e6
            for d in todas:
                shutil.rmtree(d, ignore_errors=True)
            print("LIMPAR %d pastas de quadros (%.0f MB)" % (len(todas), mb))
