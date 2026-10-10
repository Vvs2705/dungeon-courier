# Dados das regioes 2-4 (CONTEUDO_REGIOES.md) + validacao com as regras do CoreTests.LevelDesign_* + geracao dos blocos do doc.
# ponytail: reimplementa Blocked/Nav do Run.cs em Python so para checar o desenho; o teste de verdade e o C# da raia de regiao.
import math, sys, collections

W_, H_ = 9.0, 14.0
ENT = (4.5, 0.9)
def door_x(n, i): return 4.5 if n <= 1 else (2.5 + 4 * i if n == 2 else 1.5 + 3 * i)

RAD = dict(Bat=.30, Thief=.35, Guard=.45, Brute=.55, Ogre=.9, Archer=.35, Slime=.40, Captain=.5,
           Miner=.35, Mole=.35, Frog=.45, Beetle=.30, Cart=.6, MoleQueen=.7, Golem=1.0,
           Ghost=.35, Skeleton=.4, Spider=.35, Mummy=.45, Gargoyle=.5, Widow=.7, Necro=.5,
           Salamander=.35, Imp=.3, Kobold=.35, Magma=.6, MagmaShard=.4, Foreman=.55, Dragon=1.3)
LOCKS = {"Guard", "Ogre", "Captain", "MoleQueen", "Golem", "Widow", "Necro", "Foreman", "Dragon"}

class Room:
    def __init__(s, id_, code, name, lesson, threat, obst, spawns, rescue, chest=None, coins=0, wave=None, trigger=None,
                 locks=False, secret=None, secret_pos=None, water=(), lava=(), webs=(), vents=(), torches=(), rails=(),
                 hidden=(), dark=False, disguised=False, blink=(), note=""):
        s.__dict__.update(locals()); del s.__dict__["s"]
        s.wave = wave or []

def dist(a, b): return math.hypot(a[0] - b[0], a[1] - b[1])
def closest(r, p):
    cx, cy, hx, hy = r
    return (max(cx - hx, min(p[0], cx + hx)), max(cy - hy, min(p[1], cy + hy)))
def solids(room): return list(room.obst) + list(room.lava)
def blocked(room, p, r):
    for o in solids(room):
        q = closest(o, p)
        if (p[0] - q[0]) ** 2 + (p[1] - q[1]) ** 2 < r * r: return True
    return False

GW, GH, CELL = 18, 28, 0.5
def nav_steps(room, a, b):
    bl = [blocked(room, ((i % GW + .5) * CELL, (i // GW + .5) * CELL), 0.45) for i in range(GW * GH)]
    def cell(p):
        x = max(0, min(GW - 1, int(p[0] / CELL))); y = max(0, min(GH - 1, int(p[1] / CELL))); return y * GW + x
    g = cell(b)
    if bl[g]:
        gc = ((g % GW + .5) * CELL, (g // GW + .5) * CELL)
        g = min((i for i in range(GW * GH) if not bl[i]), key=lambda i: dist(((i % GW + .5) * CELL, (i // GW + .5) * CELL), gc))
    f = [None] * (GW * GH); f[g] = 0; q = collections.deque([g])
    while q:
        c = q.popleft(); cx, cy = c % GW, c // GW
        for n, ok in ((c - 1, cx > 0), (c + 1, cx < GW - 1), (c - GW, cy > 0), (c + GW, cy < GH - 1)):
            if ok and not bl[n] and f[n] is None: f[n] = f[c] + 1; q.append(n)
    return f[cell(a)]

def check(room, errs):
    tag = room.code
    for o in solids(room):
        if o[1] + o[3] > 12.2 + 1e-6: errs.append(f"{tag}: solido na faixa das portas {o}")
    if blocked(room, ENT, 1.2): errs.append(f"{tag}: solido na entrada")
    for t, x, y, *z in room.spawns + room.wave:
        if t == "Cart": continue
        if dist((x, y), ENT) < 4 - 1e-6: errs.append(f"{tag}: {t} a {dist((x, y), ENT):.2f} m da entrada")
        if blocked(room, (x, y), RAD[t]): errs.append(f"{tag}: {t} dentro de obstaculo ({x},{y})")
        for (x1, y1, x2, y2, ph) in room.rails:
            if abs(y - y1) < 1.0 and y1 == y2: errs.append(f"{tag}: {t} em cima do trilho")
    pts = [("resgate", room.rescue)] + ([("bau", room.chest)] if room.coins else []) + [("enterrado", (h[0], h[1])) for h in room.hidden]
    for nm, p in pts:
        if blocked(room, p, 0.3): errs.append(f"{tag}: {nm} dentro de obstaculo")
        if nav_steps(room, ENT, p) is None: errs.append(f"{tag}: {nm} inalcancavel")
        for (x1, y1, x2, y2, ph) in room.rails:
            if abs(p[1] - y1) < 1.0: errs.append(f"{tag}: {nm} em cima do trilho")
        for (vx, vy, vr, ph) in room.vents:
            if dist(p, (vx, vy)) < vr + 0.5: errs.append(f"{tag}: {nm} em cima de respiro")
    for n in (1, 2, 3):
        for i in range(n):
            if nav_steps(room, ENT, (door_x(n, i), 13.5)) is None: errs.append(f"{tag}: porta {n}/{i} inalcancavel")
    if room.secret_pos:
        sx, sy = room.secret_pos
        if sx not in (0, 9) or not (1.5 <= sy <= 12): errs.append(f"{tag}: parede rachada fora da lateral")
        inside = (0.5 if sx == 0 else 8.5, sy)
        if blocked(room, inside, 0.4): errs.append(f"{tag}: parede rachada atras de obstaculo")
        if nav_steps(room, ENT, inside) is None: errs.append(f"{tag}: parede rachada inalcancavel")
    for (cx, cy, r) in room.webs:
        if dist((cx, cy), ENT) < r + 1.2: errs.append(f"{tag}: teia na entrada")
        if cy + r > 12.2: errs.append(f"{tag}: teia na faixa das portas")
    for (vx, vy, vr, ph) in room.vents:
        if dist((vx, vy), ENT) < vr + 1.2: errs.append(f"{tag}: respiro na entrada")
        if vy + vr > 12.2: errs.append(f"{tag}: respiro na faixa das portas")
    for w in room.water:
        if dist(closest(w, ENT), ENT) < 1.2: errs.append(f"{tag}: agua na entrada")
    for (x1, y1, x2, y2, ph) in room.rails:
        if not (4.0 <= y1 <= 11.5): errs.append(f"{tag}: trilho fora de 4-11,5")
        for o in solids(room):
            if abs(o[1] - y1) < o[3] + 0.6: errs.append(f"{tag}: obstaculo no trilho {o}")
    for (tx, ty) in room.torches:
        if tx not in (0.3, 8.7) or not (2 <= ty <= 12.2): errs.append(f"{tag}: tocha fora da parede lateral")
    if room.dark and len(room.torches) < 2: errs.append(f"{tag}: sala escura com menos de 2 tochas")
    for b in room.blink:
        if blocked(room, b, 0.5) or dist(b, ENT) < 4: errs.append(f"{tag}: ponto de salto ruim {b}")

# ------------------------------------------------------------------ dados
def O(cx, cy, hx, hy): return (cx, cy, hx, hy)
def S(t, x, y): return (t, x, y)
def Z(t, x, y): return (t, x, y, "zz")

R = []
# ===== Regiao 2: Minas Alagadas (ids 21-33)
R += [
 Room(21, "R17", "Galeria Alagada", "água freia todos 25% (courier, ladrão, limo): pouse a carga na poça e o ladrão chega mais devagar; o cristal recongela na água; parede rachada à direita", 1,
      [O(1.5, 8.5, .5, .5), O(7.5, 8.5, .5, .5), O(4.5, 11.0, .8, .4)], [S("Thief", 8.0, 10.0), S("Bat", 1.5, 11.5)], (2.0, 3.0), (8.2, 6.5), 30,
      [S("Slime", .8, 3.6)], "Half", secret="S3", secret_pos=(9, 4.0), water=[O(4.5, 6.5, 2.5, 1.4)], note="baú: 1º projeto das Botas de lama"),
 Room(22, "R18", "Trilho do Vagonete", "o vagonete cruza a sala a cada ~4,7 s (sino e trilho acesos 1,2 s antes): atravesse logo depois dele; ele atropela inimigos: traga o ladrão até o trilho", 2,
      [O(2.0, 9.6, .6, .6), O(7.0, 9.6, .6, .6), O(4.5, 4.0, .8, .3)], [S("Cart", .6, 6.5), S("Thief", 1.5, 11.5), S("Miner", 7.0, 11.6), S("Bat", 4.5, 11.8)], (1.5, 3.0),
      wave=[S("Thief", 8.0, 12.5)], trigger="Down", rails=[(0, 6.5, 9, 6.5, 0.0)]),
 Room(23, "R19", "Mina Adormecida", "mineiros dormindo (zZ) só acordam com o courier a 2 m, grito, explosão ou golpe: o corredor do meio passa em silêncio; o baú fica colado num dorminhoco", 1,
      [O(2.5, 6.0, 1.0, .4), O(6.5, 6.0, 1.0, .4), O(4.5, 9.5, .6, .6)], [Z("Miner", .8, 10.5), Z("Miner", 8.2, 10.5), S("Bat", 4.5, 12.0)], (2.0, 3.0), (8.2, 12.0), 40,
      [S("Bat", .8, 3.6)], "Half"),
 Room(24, "R20", "Toca da Toupeira", "rastro de terra = toupeira por baixo: ela sai embaixo da carga no chão e a empurra 1,5 m; carregue a carga ou acerte-a nos 2 s em que fica fora; na água ela não sai", 2,
      [O(2.0, 7.5, .6, .6), O(7.0, 7.5, .6, .6)], [S("Mole", 4.5, 10.0), S("Mole", 1.5, 11.5), S("Bat", 7.5, 11.5)], (1.5, 3.0), (.8, 9.5), 35,
      [S("Mole", 8.0, 12.0)], "Down", water=[O(6.8, 4.6, 1.4, 1.1)]),
 Room(25, "R21", "Lodaçal do Sapo", "o sapo puxa a carga do chão a até 3 m (linha de aviso 0,7 s): pouse fora do anel dele e abata-o de 3 a 5 m", 2,
      [O(4.5, 5.0, .6, .6), O(4.5, 10.6, .6, .6)], [S("Frog", 2.0, 9.5), S("Frog", 7.0, 7.0), S("Thief", 8.0, 11.5)], (2.0, 3.0), (.8, 6.0), 35,
      [S("Slime", .8, 3.6)], "Half", water=[O(2.0, 9.5, 1.4, 1.2), O(7.0, 7.0, 1.4, 1.2)], note="baú: 2º projeto das Botas de lama"),
 Room(26, "R22", "Corredor dos Besouros", "o besouro corre até você, brilha 1 s e explode (1,5 m): atire de longe ou dê dash; a explosão acerta inimigos: leve-o até o Bruto", 3,
      [O(2.0, 6.0, .5, .5), O(7.0, 6.0, .5, .5), O(4.5, 8.5, .5, .5)], [S("Beetle", 1.5, 11.0), S("Beetle", 7.5, 11.0), S("Brute", 4.5, 11.0)], (2.0, 3.0), (8.2, 9.0), 40,
      [S("Beetle", .8, 3.6), S("Beetle", 8.2, 3.6)], "Half"),
 Room(27, "R23", "Cruzamento de Trilhos", "dois trilhos com ritmos opostos: espere na ilha do meio; o ladrão que corre para a carga cruza o trilho e pode ser atropelado; parede rachada à esquerda", 2,
      [O(2.5, 7.2, .6, .5), O(6.5, 7.2, .6, .5), O(4.5, 11.6, .8, .3)], [S("Cart", .6, 4.8), S("Cart", 8.4, 9.6), S("Thief", 8.0, 7.2), S("Archer", 7.5, 11.6), S("Bat", 1.5, 11.8)], (1.5, 2.8), (.8, 11.2), 40,
      [S("Thief", 8.0, 12.5)], "Down", secret="S3", secret_pos=(0, 7.2), rails=[(0, 4.8, 9, 4.8, 0.0), (0, 9.6, 9, 9.6, 2.35)], note="baú: 1º projeto da Bomba de fumaça"),
 Room(28, "R24", "Eclusa do Guarda", "luta obrigatória: pouse a carga na água (ladrão mais lento, a toupeira não sai ali) e derrube o Guarda", 2,
      [O(1.2, 9.5, .4, 1.0), O(7.8, 9.5, .4, 1.0)], [S("Guard", 4.5, 12.4), S("Thief", 1.0, 6.5), S("Mole", 7.8, 6.0)], (7.0, 2.8),
      wave=[S("Thief", 8.0, 12.5)], trigger="Down", water=[O(4.5, 7.0, 2.2, 1.2)]),
 Room(29, "R25", "Desmoronamento", "emboscada: passar da metade faz mineiros e um besouro caírem do teto e a porta fecha; a explosão do besouro acerta os mineiros", 3,
      [O(2.5, 5.5, .6, .6), O(6.5, 5.5, .6, .6), O(4.5, 9.0, 1.2, .4)], [Z("Miner", 8.0, 7.6), S("Bat", 1.5, 10.5)], (2.0, 2.8),
      wave=[S("Miner", 1.5, 12.5), S("Miner", 7.5, 12.5), S("Beetle", 4.5, 12.8)], trigger="Half", locks=True),
 Room(30, "R26", "Salão da Bomba d'Água", "Bruto, sapo e mineiro juntos: o Bruto empurra você para o anel do sapo; carga 3 s no chão chama reforço e fecha a porta", 3,
      [O(1.5, 5.5, .6, .6), O(7.5, 5.5, .6, .6), O(1.5, 10.0, .6, .6), O(7.5, 10.0, .6, .6)], [S("Brute", 2.6, 11.8), S("Frog", 4.5, 8.0), S("Miner", 7.5, 12.0)], (2.0, 2.8), (.8, 8.0), 45,
      [S("Beetle", 1.0, 12.5), S("Thief", 8.0, 12.5), S("Bat", 4.5, 12.8)], "Down", locks=True, water=[O(4.5, 8.0, 1.6, 1.2)]),
 Room(31, "S3", "Veio de Ouro", "sala secreta: baú grande ao lado de um mineiro dormindo", 0,
      [O(2.0, 6.0, .5, .5), O(7.0, 6.0, .5, .5)], [Z("Miner", 6.0, 10.5)], (2.0, 2.5), (4.5, 10.0), 90, note="baú: 3º projeto das Botas de lama"),
 Room(32, "M2", "Ninho da Rainha Toupeira", "mini-chefe: a Rainha cava, sai embaixo da carga no chão e chama toupeiras; nas poças ela não sai", 3,
      [O(2.5, 9.5, .5, .5), O(6.5, 9.5, .5, .5)], [S("MoleQueen", 4.5, 10.8), S("Mole", 7.5, 11.5)], (2.0, 2.5),
      water=[O(1.6, 6.0, 1.0, 1.0), O(7.4, 6.0, 1.0, 1.0)]),
 Room(33, "B2", "Câmara do Golem", "chefe: pancada em área, pedras que fecham rotas, ponto fraco nas costas; o vagonete acerta o Golem (6 de dano e atordoa)", 3,
      [O(1.8, 7.6, .5, .5), O(7.2, 7.6, .5, .5), O(1.8, 11.2, .5, .5), O(7.2, 11.2, .5, .5)], [S("Golem", 4.5, 9.5), S("Cart", .6, 4.6)], (2.0, 2.5),
      rails=[(0, 4.6, 9, 4.6, 0.0)]),
]
# ===== Regiao 3: Cripta Assombrada (ids 34-46) — todas escuras
D = dict(dark=True)
R += [
 Room(34, "R27", "Corredor das Tochas", "no escuro a mira só pega o que está na luz (sua luz 3,5 m, tocha 2,5 m, lanterna 5,5 m): ande de tocha em tocha", 1,
      [O(2.5, 5.0, .6, .6), O(6.5, 5.0, .6, .6), O(2.5, 9.0, .6, .6), O(6.5, 9.0, .6, .6)], [S("Skeleton", 4.5, 10.5), S("Bat", 7.5, 11.5)], (1.5, 2.5),
      wave=[S("Bat", 8.2, 3.6)], trigger="Half", torches=[(0.3, 4.0), (8.7, 7.0), (0.3, 10.5)], **D),
 Room(35, "R28", "Ossário", "esqueleto derrubado se remonta em 4 s, a menos que a carga esteja a 3 m dele ou que o dash espalhe os ossos: lute perto da carga", 2,
      [O(2.0, 7.0, .6, .3), O(7.0, 7.0, .6, .3), O(4.5, 9.5, .6, .3)], [S("Skeleton", 2.5, 10.5), S("Skeleton", 6.5, 10.5), S("Thief", 8.0, 8.5)], (2.0, 2.5), (.8, 9.0), 40,
      [S("Skeleton", .8, 3.6)], "Half", torches=[(0.3, 5.5), (8.7, 5.5), (0.3, 11.0)], note="lápides = obstáculos; baú: 1º projeto do Gancho", **D),
 Room(36, "R29", "Teia Grande", "teia prende 1 s quem passa carregando e freia 50% sem carga; carga pousada na teia leva +1 s para pegar; o dash rasga a teia", 2,
      [O(1.0, 8.5, .4, .4), O(8.0, 8.5, .4, .4)], [S("Spider", 4.5, 11.5), S("Spider", 1.5, 9.6), S("Bat", 7.5, 12.0)], (1.5, 2.5),
      wave=[S("Spider", 8.2, 3.6)], trigger="Half", webs=[(2.2, 6.0, .9), (6.8, 6.0, .9), (4.5, 8.5, 1.0), (2.2, 11.0, .9), (6.8, 11.0, .9)],
      torches=[(0.3, 4.0), (8.7, 4.0), (8.7, 10.0)], **D),
 Room(37, "R30", "Capela Fantasma", "fantasma atravessa parede e possui a carga no chão: ela flutua até a parede e some em 2 s; pegue de volta ou acerte-o (só na luz); parede rachada à direita", 2,
      [O(2.4, 6.0, 1.2, .3), O(6.6, 6.0, 1.2, .3), O(2.4, 8.5, 1.2, .3), O(6.6, 8.5, 1.2, .3), O(4.5, 10.8, .8, .4)],
      [S("Ghost", 4.5, 12.5), S("Ghost", 1.0, 10.0), S("Archer", 7.5, 11.8)], (2.0, 2.5), (8.2, 10.0), 40,
      [S("Ghost", .8, 3.6)], "Half", secret="S4", secret_pos=(9, 7.2), torches=[(0.3, 4.5), (8.7, 4.5), (0.3, 11.5), (8.7, 11.5)], note="baú: 1º projeto da Bomba de fumaça (região 3)", **D),
 Room(38, "R31", "Sarcófagos", "múmia é lenta (1 m/s) mas agarra: segura até levar 2 golpes ou até o dash (que larga a carga); não deixe ela encostar com a carga nas mãos", 2,
      [O(2.0, 7.5, .4, .9), O(7.0, 7.5, .4, .9), O(4.5, 10.5, .9, .4)], [S("Mummy", 2.0, 10.5), S("Mummy", 7.0, 10.5), S("Bat", 4.5, 12.5)], (2.0, 2.5), (4.5, 8.0), 45,
      [S("Skeleton", 8.2, 3.6)], "Half", torches=[(0.3, 5.0), (8.7, 5.0), (8.7, 11.0)], **D),
 Room(39, "R32", "Galeria das Gárgulas", "gárgula é pedra (invulnerável) até o courier passar a 3 m COM a carga; aí mergulha em linha (aviso 0,9 s) e fica 2,5 s no chão, vulnerável; sem a carga nas mãos ela dorme", 2,
      [O(2.8, 5.5, .5, .5), O(6.2, 5.5, .5, .5)], [S("Gargoyle", 1.0, 9.0), S("Gargoyle", 8.0, 9.0), S("Archer", 4.5, 12.0)], (2.0, 2.5), (.8, 11.5), 45,
      [S("Bat", .8, 3.6), S("Bat", 8.2, 3.6)], "Half", torches=[(0.3, 6.0), (8.7, 6.0), (0.3, 12.0), (8.7, 12.0)], **D),
 Room(40, "R33", "Emboscada na Cripta", "emboscada: passar da metade ergue dois esqueletos e um fantasma por cima e a porta fecha; mate perto da carga ou eles voltam", 3,
      [O(2.0, 6.0, .6, .3), O(7.0, 6.0, .6, .3), O(2.0, 9.0, .6, .3), O(7.0, 9.0, .6, .3)], [S("Spider", 4.5, 10.5), S("Bat", 1.5, 11.5)], (2.0, 2.5),
      wave=[S("Skeleton", 1.5, 12.5), S("Skeleton", 7.5, 12.5), S("Ghost", 4.5, 12.8)], trigger="Half", locks=True, torches=[(0.3, 7.5), (8.7, 7.5), (0.3, 3.5)], **D),
 Room(41, "R34", "Guardião do Mausoléu", "luta obrigatória no escuro: pouse a carga na luz (o fantasma só aparece nela) e longe das teias", 2,
      [O(4.5, 6.0, 1.4, .4)], [S("Guard", 4.5, 12.4), S("Spider", 1.5, 8.0), S("Ghost", 8.0, 7.0)], (7.0, 2.5),
      wave=[S("Ghost", 8.0, 12.5)], trigger="Down", webs=[(2.5, 9.6, .9), (6.5, 9.6, .9)], torches=[(0.3, 11.0), (8.7, 11.0), (0.3, 4.0)], **D),
 Room(42, "R35", "Biblioteca Sombria", "três estantes em zigue-zague, só as passagens têm tocha; o fantasma corta por dentro das estantes; carga 3 s no chão chama reforço e fecha a porta; parede rachada à esquerda", 2,
      [O(3.0, 4.5, 3.0, .3), O(6.0, 8.0, 3.0, .3), O(3.0, 11.0, 3.0, .3)], [S("Ghost", 1.5, 6.5), S("Skeleton", 7.5, 9.6), S("Bat", 4.5, 12.5)], (7.0, 2.5), (.8, 9.6), 45,
      [S("Skeleton", 1.0, 12.5), S("Spider", 8.0, 12.5)], "Down", locks=True, secret="S4", secret_pos=(0, 2.6), torches=[(8.7, 4.5), (0.3, 8.0), (8.7, 11.0)],
      note="baú: 2º projeto da Bomba de fumaça", **D),
 Room(43, "R36", "Nave da Catedral", "múmia, arqueiro, gárgula e esqueleto; passar da metade chama reforço e fecha a porta", 3,
      [O(2.0, 6.0, .6, .6), O(7.0, 6.0, .6, .6), O(4.5, 9.0, .6, .6)], [S("Mummy", 2.5, 10.5), S("Archer", 7.5, 11.5), S("Gargoyle", 4.5, 11.8), S("Skeleton", 6.2, 8.4)], (2.0, 2.5), (8.2, 8.0), 50,
      [S("Bat", 1.0, 12.5), S("Skeleton", 8.0, 12.5), S("Ghost", 4.5, 12.8)], "Half", locks=True, torches=[(0.3, 6.0), (8.7, 6.0), (0.3, 11.0), (8.7, 11.0)], **D),
 Room(44, "S4", "Cripta do Tesouro", "sala secreta: baú grande guardado por um esqueleto", 1,
      [O(2.0, 6.0, .6, .3), O(7.0, 6.0, .6, .3)], [S("Skeleton", 4.5, 11.0)], (2.0, 2.5), (4.5, 9.0), 100, torches=[(0.3, 9.0), (8.7, 9.0)],
      note="baú: 3º projeto da Bomba de fumaça", **D),
 Room(45, "M3", "Covil da Viúva", "mini-chefe: teias pela sala, puxa a carga com o fio, despenca do teto em cima da carga", 3,
      [O(2.0, 10.0, .5, .5), O(7.0, 10.0, .5, .5)], [S("Widow", 4.5, 11.2), S("Spider", 1.5, 12.0)], (2.0, 2.5),
      webs=[(2.0, 7.0, .9), (7.0, 7.0, .9), (4.5, 5.0, .8)], torches=[(0.3, 4.0), (8.7, 4.0), (0.3, 10.0), (8.7, 10.0)], **D),
 Room(46, "B3", "Altar do Necromante", "chefe: ergue esqueletos das lápides em ondas, raio telegrafado, salta de lápide em lápide quando você chega perto", 3,
      [O(1.8, 5.5, .5, .3), O(7.2, 5.5, .5, .3), O(1.8, 8.5, .5, .3), O(7.2, 8.5, .5, .3), O(1.8, 11.5, .5, .3), O(7.2, 11.5, .5, .3), O(4.5, 9.8, .8, .4)],
      [S("Necro", 4.5, 11.6)], (4.5, 3.0), torches=[(0.3, 4.0), (8.7, 4.0), (0.3, 10.0), (8.7, 10.0)],
      blink=[(3.0, 5.5), (6.0, 5.5), (3.0, 8.5), (6.0, 8.5), (3.0, 11.6), (6.0, 11.6)], **D),
]
# ===== Regiao 4: Forja do Dragao (ids 47-59)
R += [
 Room(47, "R37", "Ponte de Basalto", "lava é parede baixa: não se anda nela, mas tiro, flecha e frasco passam por cima; inimigo empurrado na lava pelo dash leva 3 de dano", 1,
      [O(4.5, 10.5, .4, .4)], [S("Salamander", 4.5, 9.4), S("Kobold", 7.5, 11.5)], (2.0, 2.5), (4.5, 5.0), 35,
      [S("Salamander", .8, 3.6)], "Half", lava=[O(.7, 7.0, .7, .7), O(4.5, 7.0, 1.5, .7), O(8.3, 7.0, .7, .7)]),
 Room(48, "R38", "Respiros", "respiro brilha 0,8 s e solta fogo por 1 s (2 de dano; racha a carga no chão; derrete gelo; 3 de dano em inimigo): passe logo depois do fogo", 2,
      [O(1.0, 8.5, .5, .3), O(8.0, 8.5, .5, .3)], [S("Bat", 1.5, 11.0), S("Slime", 7.5, 10.5)], (2.0, 2.5), (8.2, 6.6), 40,
      [S("Bat", 8.2, 3.6)], "Half", vents=[(2.0, 5.0, 1.0, 0.0), (4.5, 5.0, 1.0, 2.0), (7.0, 5.0, 1.0, 0.0), (3.2, 8.5, 1.0, 1.0), (5.8, 8.5, 1.0, 3.0), (4.5, 11.0, 1.0, 2.0)]),
 Room(49, "R39", "Ninho de Salamandras", "a salamandra deixa rastro de fogo por 3 s (1 de dano ao pisar; derrete o gelo) e circula você a 2–3 m: não deixe ela fechar o círculo em volta da carga; parede rachada à esquerda", 2,
      [O(2.5, 7.0, .6, .6), O(6.5, 7.0, .6, .6), O(4.5, 10.5, 1.0, .4)], [S("Salamander", 1.5, 10.0), S("Salamander", 7.5, 10.0), S("Thief", 8.0, 12.0)], (2.0, 2.5), (.8, 12.0), 40,
      [S("Salamander", 8.2, 3.6)], "Half", secret="S5", secret_pos=(0, 5.0), lava=[O(4.5, 4.6, 1.0, .4)], note="baú: 1º projeto da Armadilha de urso"),
 Room(50, "R40", "Oficina do Diabrete", "o diabrete troca de lugar com a carga no chão (redemoinho de 1 s nos dois pontos; precisa ver a carga): acerte-o no redemoinho ou pouse atrás de uma bigorna", 2,
      [O(2.0, 6.5, .6, .4), O(7.0, 6.5, .6, .4), O(4.5, 9.5, .6, .4)], [S("Imp", 7.5, 11.0), S("Imp", 2.0, 12.0), S("Bat", 4.5, 12.5)], (2.0, 2.5), (8.2, 9.0), 45,
      [S("Imp", 8.2, 3.6)], "Half", lava=[O(.6, 10.5, .6, 1.0)], note="baú: 1º projeto do Gancho (região 4)"),
 Room(51, "R41", "Bancada dos Kobolds", "o kobold joga frasco em arco (círculo de aviso 0,9 s) que vira poça de fogo de 1 m por 4 s: não pouse onde o frasco cai, mude de lugar", 2,
      [O(2.0, 6.0, .6, .4), O(7.0, 6.0, .6, .4), O(4.5, 8.5, 1.4, .3)], [S("Kobold", 2.0, 11.5), S("Kobold", 7.0, 11.5), S("Thief", 8.0, 9.6)], (2.0, 2.5), (.8, 9.0), 40,
      [S("Salamander", 1.0, 12.5), S("Bat", 8.0, 12.5)], "Down"),
 Room(52, "R42", "Fundição", "o elemental de magma ao morrer se divide em 2 brasas; lava dos lados: empurre as brasas na lava com o dash", 3,
      [O(4.5, 10.8, .8, .4)], [S("Magma", 4.5, 9.0), S("Magma", 2.6, 11.6), S("Bat", 7.0, 11.6)], (2.0, 2.5), (8.2, 4.0), 45,
      [S("Salamander", .8, 3.6), S("Salamander", 8.2, 3.6)], "Half", lava=[O(1.0, 7.5, 1.0, 1.5), O(8.0, 7.5, 1.0, 1.5)], note="baú: 2º projeto da Armadilha de urso"),
 Room(53, "R43", "Guarda da Forja", "luta obrigatória entre dois respiros: pouse a carga no lado frio e conte o ritmo", 2,
      [O(4.5, 6.0, 1.4, .4)], [S("Guard", 4.5, 12.4), S("Kobold", 1.5, 10.5), S("Imp", 7.5, 10.5)], (7.0, 2.5),
      wave=[S("Imp", 8.0, 12.5)], trigger="Down", vents=[(2.5, 8.2, 1.0, 0.0), (6.5, 8.2, 1.0, 2.0)]),
 Room(54, "R44", "Emboscada de Brasa", "emboscada: passar da metade traz um elemental e dois diabretes e a porta fecha; os respiros acertam inimigos também", 3,
      [O(2.5, 5.5, .6, .4), O(6.5, 5.5, .6, .4), O(4.5, 9.0, 1.2, .4)], [S("Salamander", 8.0, 10.5), S("Kobold", 1.5, 11.5)], (2.0, 2.5),
      wave=[S("Magma", 4.5, 12.5), S("Imp", 1.5, 12.8), S("Imp", 7.5, 12.8)], trigger="Half", locks=True, vents=[(1.5, 8.0, 1.0, 1.0), (7.5, 8.0, 1.0, 3.0)]),
 Room(55, "R45", "Sala das Correntes", "baú enterrado (X) e parede rachada disfarçada só aparecem com o Mapa ou o perk Faro de baú; ladrões correm 30% mais atrás do Mapa", 1,
      [O(4.5, 7.5, .6, .4), O(2.0, 10.0, .5, .5), O(7.0, 10.0, .5, .5)], [S("Thief", 1.0, 8.0), S("Thief", 8.0, 8.0), S("Imp", 4.5, 12.0)], (2.0, 2.5),
      wave=[S("Thief", 8.0, 12.5)], trigger="Down", secret="S5", secret_pos=(9, 5.5), disguised=True, hidden=[(1.0, 5.5, 60)],
      note="baú enterrado: 2º projeto do Gancho"),
 Room(56, "R46", "Coração da Forja", "Bruto, elemental, kobold e salamandra entre respiros e um poço de lava; passar da metade chama reforço e fecha a porta", 3,
      [O(1.5, 10.5, .5, .5), O(7.5, 10.5, .5, .5)], [S("Brute", 3.0, 11.8), S("Magma", 6.0, 11.8), S("Kobold", 8.2, 8.5)], (2.0, 2.5), (.8, 8.5), 55,
      [S("Imp", 1.0, 12.8), S("Salamander", 8.0, 12.8), S("Kobold", 4.5, 12.8)], "Half", locks=True, lava=[O(4.5, 9.5, 1.0, .5)],
      vents=[(2.0, 6.5, 1.0, 0.0), (7.0, 6.5, 1.0, 2.0)]),
 Room(57, "S5", "Cofre do Ferreiro", "sala secreta: baú grande, um baú enterrado e um diabrete", 1,
      [O(2.0, 6.0, .5, .5), O(7.0, 6.0, .5, .5)], [S("Imp", 6.5, 11.0)], (2.0, 2.5), (4.5, 10.0), 110, hidden=[(1.0, 11.5, 50)],
      note="baú: 3º projeto da Armadilha de urso; enterrado: 3º do Gancho"),
 Room(58, "M4", "Pátio do Capataz", "mini-chefe: chicote de 3 m, chama kobolds e joga frascos; respiros dos lados", 3,
      [O(4.5, 6.0, .8, .4)], [S("Foreman", 4.5, 11.0), S("Kobold", 8.0, 12.0)], (2.0, 2.5), vents=[(2.0, 8.4, 1.0, 0.0), (7.0, 8.4, 1.0, 2.0)]),
 Room(59, "B4", "Ninho do Dragão", "chefe final: sopro em cone (os pilares cobrem), voo e pouso em área, ovos de salamandra; fissuras de lava na fase 2", 3,
      [O(2.0, 6.0, .6, .6), O(7.0, 6.0, .6, .6), O(2.0, 9.5, .6, .6), O(7.0, 9.5, .6, .6)], [S("Dragon", 4.5, 11.0)], (4.5, 3.0),
      lava=[O(.4, 11.4, .4, .7), O(8.6, 11.4, .4, .7)], vents=[(4.5, 7.8, 1.2, 0.0), (1.0, 4.2, 1.0, 2.0), (8.0, 4.2, 1.0, 4.0)]),
]

ROOM = {r.code: r for r in R}


# ---- projetos (blueprints) nos baus e baus enterrados (so aparecem com Mapa / Faro de bau / Treasure Detour)
for code, note in {"R17": "baú: 1º projeto das Botas de lama", "R21": "baú: 2º projeto das Botas de lama", "S3": "baú: 3º projeto das Botas de lama",
                   "R23": "baú: 1º projeto da Bomba de fumaça", "R30": "baú: 2º projeto da Bomba de fumaça", "S4": "baú: 3º projeto da Bomba de fumaça",
                   "R28": "lápides = obstáculos; baú: 1º projeto do Gancho", "R35": "baú: 2º projeto do Gancho", "R40": "baú: 3º projeto do Gancho",
                   "R39": "baú: 1º projeto da Armadilha de urso", "R42": "baú: 2º projeto da Armadilha de urso", "S5": "baú: 3º projeto da Armadilha de urso",
                   "R45": ""}.items():
    ROOM[code].note = note
for code, h in {"R20": [(8.2, 12.0, 30)], "R25": [(8.2, 4.0, 35)], "R31": [(4.5, 5.0, 35)], "R35": [(4.5, 9.6, 40)],
                "R38": [(0.8, 3.4, 40)], "R40": [(4.5, 11.5, 45)], "R42": [(4.5, 5.5, 45)], "R44": [(8.2, 3.8, 50)], "R46": [(8.2, 11.6, 55)]}.items():
    ROOM[code].hidden = h

# ------------------------------------------------------------------ contratos
# (cargo, nota, camadas)  — camadas com codigos de sala; D1 implicita
C2 = [
 ("Box",   "região 2: água e vagonete; mineiros dormindo", ["R17"], ["R18", "R19"]),
 ("Egg",   "toupeira e sapo; o ovo choca no tempo-alvo", ["R20"], ["R19", "R21"], ["R18", "R17"]),
 ("Ice",   "cristal de gelo: derrete; a água recongela", ["R17"], ["R21", "R24"], ["R20", "R23"]),
 ("Relic", "relíquia entre picaretas e trilhos", ["R22"], ["R19", "R18"], ["R21", "R20"], ["R25", "R23"], ["R17", "R24"]),
 ("Heart", "coração arcano: ímã de moedas e de inimigos", ["R19"], ["R22", "R17"], ["R20", "R24"], ["R23", "R21"], ["R26", "R18"]),
 ("Barrel","barril com besouros: a explosão detona o barril", ["R17"], ["R18", "R21"], ["R24", "R19"], ["R20", "R22"], ["R23", "R25"], ["R21", "R17"]),
 ("Ice",   "cristal em 5 camadas", ["R24"], ["R17", "R23"], ["R21", "R19", "R20"], ["R22", "R18"], ["R26", "R25"]),
 ("Box",   "mini-chefe: Rainha Toupeira", ["R18"], ["R20", "R22"], ["R25", "R17"], ["R24", "R21"], ["R19", "R23"], ["M2"]),
 ("Msg",   "mensagem: trilhos não esperam", ["R21"], ["R24", "R19"], ["R26", "R18"], ["R20", "R25"], ["R17", "R22"], ["R23", "R24"]),
 ("Cage",  "gaiola do grifinho: o grito acorda a mina", ["R19"], ["R17", "R20"], ["R25", "R23"], ["R18", "R21"], ["R22", "R24"]),
 ("Heart", "coração em 6 camadas", ["R23"], ["R25", "R19"], ["R22", "R26"], ["R21", "R18"], ["R24", "R20"], ["R17", "R23"]),
 ("Cake",  "bolo sobre trilhos", ["R20"], ["R22", "R17"], ["R18", "R26"], ["R25", "R24"], ["R19", "R21"], ["R23", "R20"]),
 ("Heavy", "caixa pesada na água", ["R17"], ["R21", "R23"], ["R24", "R26", "R19"], ["R18", "R22"], ["R25", "R20"], ["R21", "R17"]),
 ("Cage",  "gaiola em 6 camadas", ["R22"], ["R19", "R26"], ["R23", "R18"], ["R20", "R25"], ["R24", "R17"], ["R21", "R19"]),
 ("Ice",   "cristal em 6 camadas", ["R26"], ["R18", "R24"], ["R22", "R21"], ["R25", "R17", "R20"], ["R19", "R23"], ["R26", "R22"]),
 ("Box",   "chefe: Golem de Pedra", ["R26"], ["R24", "R23"], ["R25", "R22", "R19"], ["R21", "R18"], ["R20", "R17"], ["B2"]),
]
C3 = [
 ("Lantern","região 3: escuro; lanterna ilumina 5,5 m", ["R27"], ["R28", "R29"]),
 ("Box",    "esqueletos, teias e fantasmas sem lanterna", ["R27"], ["R30", "R31"], ["R28", "R29"]),
 ("Mirror", "espelho encantado: reflete flecha e teia", ["R29"], ["R32", "R30"], ["R27", "R34"]),
 ("Egg",    "ovo no escuro", ["R28"], ["R31", "R27"], ["R29", "R32"], ["R33", "R30"], ["R35", "R34"]),
 ("Balloon","balão de gás: leve, mas escapa do chão em 4 s", ["R31"], ["R27", "R30"], ["R34", "R28"], ["R29", "R32"], ["R36", "R35"]),
 ("Relic",  "relíquia entre múmias e gárgulas", ["R30"], ["R28", "R32"], ["R31", "R27"], ["R33", "R29"], ["R34", "R35"], ["R30", "R28"]),
 ("Lantern","lanterna em 5 camadas", ["R34"], ["R27", "R33"], ["R29", "R31", "R30"], ["R32", "R28"], ["R36", "R35"]),
 ("Box",    "mini-chefe: Viúva Aranha", ["R28"], ["R30", "R32"], ["R35", "R27"], ["R34", "R31"], ["R29", "R33"], ["M3"]),
 ("Mirror", "espelho em 6 camadas", ["R27"], ["R32", "R29"], ["R36", "R28"], ["R30", "R35"], ["R31", "R34"], ["R33", "R32"]),
 ("Msg",    "mensagem no escuro", ["R31"], ["R34", "R29"], ["R36", "R28"], ["R30", "R35"], ["R27", "R32"], ["R33", "R34"]),
 ("Balloon","balão em 6 camadas", ["R33"], ["R35", "R29"], ["R32", "R36"], ["R31", "R28"], ["R34", "R30"], ["R27", "R33"]),
 ("Heart",  "coração no escuro: a horda vem para a luz", ["R30"], ["R32", "R27"], ["R28", "R36"], ["R35", "R34"], ["R29", "R31"], ["R33", "R30"]),
 ("Barrel", "barril entre esqueletos", ["R27"], ["R31", "R33"], ["R34", "R36", "R29"], ["R28", "R32"], ["R35", "R30"], ["R31", "R27"]),
 ("Cage",   "gaiola na cripta: o grito acorda as gárgulas", ["R32"], ["R29", "R36"], ["R33", "R28"], ["R30", "R35"], ["R34", "R27"], ["R31", "R29"]),
 ("Mirror", "espelho em 6 camadas", ["R36"], ["R28", "R34"], ["R32", "R31"], ["R35", "R27", "R30"], ["R29", "R33"], ["R36", "R32"]),
 ("Lantern","chefe: Necromante", ["R36"], ["R34", "R33"], ["R35", "R32", "R29"], ["R31", "R28"], ["B3"]),
]
C4 = [
 ("Box",    "região 4: lava e respiros", ["R37"], ["R38", "R39"]),
 ("Plant",  "planta carnívora: torre que morde todo mundo", ["R39"], ["R37", "R40"], ["R38", "R41"]),
 ("Map",    "mapa do tesouro: baús enterrados e ladrões mais rápidos", ["R45"], ["R37", "R41"], ["R40", "R39"]),
 ("Relic",  "relíquia na forja", ["R38"], ["R40", "R37"], ["R41", "R39"], ["R43", "R45"], ["R42", "R38"]),
 ("Ice",    "cristal perto da lava: derrete 3x mais rápido", ["R37"], ["R41", "R45"], ["R39", "R40"], ["R43", "R38"], ["R46", "R42"]),
 ("Balloon","balão entre respiros: fogo fura", ["R40"], ["R38", "R45"], ["R37", "R42"], ["R39", "R41"], ["R44", "R43"], ["R40", "R37"]),
 ("Plant",  "planta em 5 camadas", ["R43"], ["R37", "R44"], ["R39", "R41", "R38"], ["R42", "R45"], ["R46", "R40"]),
 ("Box",    "mini-chefe: Capataz Kobold", ["R41"], ["R39", "R42"], ["R45", "R37"], ["R43", "R40"], ["R38", "R44"], ["M4"]),
 ("Egg",    "ovo na forja", ["R37"], ["R42", "R38"], ["R46", "R39"], ["R40", "R45"], ["R41", "R43"], ["R44", "R42"]),
 ("Map",    "mapa em 6 camadas", ["R45"], ["R40", "R38"], ["R42", "R46"], ["R37", "R41"], ["R43", "R39"], ["R45", "R44"]),
 ("Barrel", "barril perto do fogo: fogo no chão acende o pavio", ["R39"], ["R43", "R37"], ["R41", "R46"], ["R38", "R45"], ["R44", "R40"], ["R42", "R39"]),
 ("Msg",    "mensagem na forja", ["R40"], ["R42", "R45"], ["R37", "R44", "R41"], ["R46", "R38"], ["R43", "R39"], ["R40", "R42"]),
 ("Heavy",  "caixa pesada entre respiros", ["R38"], ["R45", "R41"], ["R44", "R39"], ["R40", "R42"], ["R37", "R46"], ["R43", "R38"]),
 ("Plant",  "planta em 6 camadas", ["R42"], ["R39", "R44"], ["R46", "R37"], ["R41", "R43", "R40"], ["R38", "R45"], ["R44", "R39"]),
 ("Cake",   "bolo na forja", ["R46"], ["R38", "R43"], ["R40", "R45"], ["R42", "R37", "R41"], ["R39", "R44"], ["R46", "R40"]),
 ("Box",    "chefe final: Dragão Ancião", ["R46"], ["R44", "R43"], ["R42", "R41", "R38"], ["R39", "R40"], ["B4"]),
]
REG = [(2, C2, 8, [195, 200, 205, 210, 214, 218, 222, 270, 230, 234, 238, 242, 246, 250, 254, 340]),
       (3, C3, 12, [255, 258, 261, 265, 269, 273, 277, 325, 285, 289, 293, 297, 301, 305, 309, 410]),
       (4, C4, 16, [310, 313, 316, 320, 324, 328, 332, 380, 340, 344, 348, 352, 356, 360, 364, 480])]

# estimativa do bot: s por visita (mediana) por ameaca, + escuro/fogo; guarda/emboscada +4; chefes
BASE = {2: {0: 7, 1: 13, 2: 16, 3: 20}, 3: {0: 8, 1: 15, 2: 18, 3: 22}, 4: {0: 8, 1: 15, 2: 18, 3: 22}}
BOSS = {"M2": 35, "B2": 100, "M3": 38, "B3": 110, "M4": 40, "B4": 130}
def room_time(reg, code, cargo):
    if code in BOSS: return BOSS[code]
    r = ROOM[code]; t = BASE[reg][r.threat]
    if r.locks or any(s[0] == "Guard" for s in r.spawns): t += 4
    if cargo == "Msg": t = t * 0.42
    return t
def r5(x): return int(5 * round(x / 5.0))

def main():
    errs = []
    for r in R: check(r, errs)
    # salas usadas e regras de contrato
    for reg, CC, tier0, pays in REG:
        used = set(); debut = {}
        for i, (cargo, note, *layers) in enumerate(CC):
            n = len(layers); boss = any(c in BOSS for c in layers[-1])
            if i < 3 and not (2 <= n <= 3): errs.append(f"M{reg}-{i+1}: tutorial com {n} camadas")
            if i >= 3 and not (4 <= n - (1 if boss else 0) <= 6): errs.append(f"M{reg}-{i+1}: {n} camadas")
            if len(layers[0]) != 1: errs.append(f"M{reg}-{i+1}: 1a camada com != 1 sala")
            if (i == 7) != (f"M{reg}" in layers[-1]) or (i == 15) != (f"B{reg}" in layers[-1]): errs.append(f"M{reg}-{i+1}: chefe fora do lugar")
            for L in layers:
                for c in L:
                    if c not in ROOM: errs.append(f"M{reg}-{i+1}: sala {c} desconhecida")
                    used.add(c)
                if len(set(L)) != len(L): errs.append(f"M{reg}-{i+1}: sala repetida na camada")
            debut.setdefault(cargo, i)
        normals = {r.code for r in R if r.code.startswith("R") and r.id_ in range({2: 21, 3: 34, 4: 47}[reg], {2: 31, 3: 44, 4: 57}[reg])}
        if normals - used: errs.append(f"regiao {reg}: salas sem contrato {normals - used}")
        if pays != sorted(pays[:7]) + [pays[7]] + sorted(pays[8:15]) + [pays[15]]: errs.append(f"regiao {reg}: pagamento fora de ordem")
    print("\n".join(errs) if errs else "OK: nenhuma regra violada")
    return errs

if __name__ == "__main__":
    e = main()
    sys.exit(1 if e else 0)
