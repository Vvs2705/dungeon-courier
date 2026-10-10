"""Relatorio do teste com jogadores reais do Dungeon Courier (v1.0-beta) a partir de 1..N diarios exportados.

Uso (da raiz do repositorio; os dados ficam em playtest/, fora do git):
  python client/tools/diario_report.py                                  # le o diario do build Windows deste PC
  python client/tools/diario_report.py playtest/rodada1                 # pasta (todos os *.csv e *.txt) ou arquivos
  python client/tools/diario_report.py playtest/rodada1 --saida playtest/rodada1/relatorio.md
  python client/tools/diario_report.py --autoteste                      # diarios sinteticos: calculos, portoes e casos de borda
Opcoes: --versao 1.0.0-beta (so essa versao) | --plataforma Android|WindowsPlayer | --fuso -3 (horas somadas ao UTC para a
data local da retencao; Brasil = -3) | --defs caminho/Defs.cs (salas e contratos; padrao = o deste repositorio).

Diario (Game.Log, docs/UI_V1.md sec.4), uma linha por evento, virgula crua, sem aspas:
  utc,instalacao,versao,sessao,t_sessao,t_contrato,evento,a,b,contrato,sala
  instalacao = GUID local (sem dado pessoal); sessao = 8 hex por abertura do app; t_sessao = s desde a abertura;
  t_contrato = Run.Time (s de jogo do contrato, atravessa sessoes); contrato = C1..C24 | DIA | codigo de 6 letras;
  sala = sala no momento da GRAVACAO (depois do passo: no route_choice ja e a sala de destino; use a/b).
Eventos usados (a/b: comentario de Ev em Defs.cs):
  session_start a=aparelho b=plataforma | contract_start a=indice(-1 gerado) b=resgate aceito | daily_start a=AAAAMMDD |
  code_start a=payload | room_enter a=sala b=camada | route_choice a=porta b=sala destino | room_fight_choice a=lutou b=sala |
  damage_taken a=dano b=vida | cargo_integrity a=id b=rachaduras | cargo_drop a=id b=motivo | cargo_pickup a=id |
  cargo_rescue_offer a=id b=aceito | cargo_rescue_result a=id b=entregue | contract_stars a=0-3 | contract_end b=Outcome |
  perk_pick a=Perk | weapon_equip a=Weapon | guild_upgrade a=linha b=nivel | currency_source a=moedas b=fonte |
  currency_sink a=moedas b=linha ou 10+arma | share_code, settings, diary_export, save_refused (contagem).
Um diario com o cabecalho da v0.1 (9 colunas, sem instalacao) e contado e ignorado: e outro jogo (8 contratos).
"""
import argparse
import glob
import math
import os
import re
import statistics
import sys
import tempfile
from collections import Counter, defaultdict
from datetime import datetime, timedelta

AQUI = os.path.dirname(os.path.abspath(__file__))
PADRAO = os.path.join(os.environ.get("USERPROFILE", ""), "AppData", "LocalLow", "V-STACK", "Dungeon Courier", "diario.csv")
DEFS = os.path.normpath(os.path.join(AQUI, "..", "Assets", "_DC", "Scripts", "Core", "Defs.cs"))
CAB_V1 = "utc,instalacao,versao,sessao,t_sessao,t_contrato,evento,a,b,contrato,sala"
CAB_V01 = "utc,sessao,t_sessao,t_contrato,evento,a,b,contrato,sala"

# enums do Core (Defs.cs; gravados por indice, so crescem no fim). Nomes so para exibir.
FIM = {1: "entregue", 2: "morreu", 3: "carga roubada", 4: "carga quebrada", 5: "filhote morto", 6: "desistiu",
       7: "tempo esgotado", 8: "explodiu"}
FALHA_CARGA = {3, 4, 5, 7, 8}
PENDENTE = {2, 3}      # morreu / carga roubada: a carga fica na masmorra e o proximo contrato oferece o resgate (Sim.Settle)
CARGAS = ["Caixa", "Relíquia frágil", "Ovo", "Bolo de casamento", "Mensagem urgente", "Caixa pesada", "Barril de pólvora"]
PERKS = ["Passo leve", "Pouso rápido", "Dash duplo", "Carga acolchoada", "Mão rápida", "Ímã", "Coração extra", "Vigia",
         "Bota de ferro", "Sorte de baú"]
CORACAO_EXTRA = 6
ARMAS = ["Estilingue", "Bumerangue", "Bombinha"]
GUILDA = ["Vida", "Dash", "Bolsa", "Mochila"]
FONTES = ["pagamento", "baú", "resgate", "bônus do dia"]
# risco que pesa para cada carga (Bot.RiskCost > 0), bits do CargoRisk: ladrao 1, arqueiro 2, pancada 4
RISCO_CARGA = [0, 7, 6, 7, 0, 1, 5]
INIMIGO_RISCO = {"Thief": 1, "Captain": 1, "Archer": 2, "Brute": 4, "Ogre": 4}
CARGA_CS = {"Box": 0, "Relic": 1, "Egg": 2, "Cake": 3, "Msg": 4, "Heavy": 5, "Barrel": 6}
SO_EXPORTAR = {"session_start", "settings", "diary_export", "save_refused"}   # sessao so com isto nao e dia ativo
M32, MAXP = 0xFFFFFFFF, (1 << 25) - 1


# ------------------------------------------------------------------ conteudo (Defs.cs) e contratos gerados

def conteudo(caminho):
    """Defs.cs -> {"salas": {id: (codigo, ameaca 0-3, risco CargoRisk, moedas do bau)}, "contratos": [(carga, camadas)],
    "normais": [ids], "d1": id} ou None. Le o codigo-fonte (fonte unica) em vez de copiar tabelas que envelhecem; o
    --autoteste prende o formato. ponytail: regex no C#; se o Content mudar de forma, o autoteste fica vermelho."""
    try:
        with open(caminho, encoding="utf-8") as f:
            txt = f.read()
        txt = txt[txt.index("public static class Content"):]
    except (OSError, ValueError):
        return None
    consts = " ".join(re.findall(r"public const int ([^;]*);", txt))   # so a declaracao: comentario "B1/M1 = 3" nao conta
    ids = {k: int(v) for k, v in re.findall(r"\b([RSMBD]\d{1,2}) = (\d+)\b", consts)}
    salas = {}
    for bloco in txt.split("new RoomDef(")[1:]:
        bloco = bloco.split("};")[0]
        m = re.match(r'\s*(\w+), "(\w+)", "[^"]*", "[^"]*", (\d+)', bloco)
        if not m or m.group(1) not in ids:
            return None
        risco = 0
        for t in re.findall(r"\bS\((\w+),", bloco):
            risco |= INIMIGO_RISCO.get(t, 0)
        if m.group(2) == "B1":
            risco |= 1   # o Ogro chama ladroes (RoomDef)
        bau = re.search(r"P\([^)]*\),\s*P\([^)]*\),\s*(\d+)", bloco)
        salas[ids[m.group(1)]] = (m.group(2), int(m.group(3)), risco, int(bau.group(1)) if bau else 0)
    contratos = {}
    for idx, carga, resto in re.findall(r'\bC\((\d+),\s*(\w+),\s*\d+,\s*"[^"]*",[^,]*,[^,]*,[^,]*,(.*)\),\s*$', txt, re.M):
        camadas = [[ids.get(n.strip(), -1) for n in grupo.split(",")] for grupo in re.findall(r"L\(([^)]*)\)", resto)]
        contratos[int(idx)] = (CARGA_CS.get(carga), camadas + [[ids.get("D1", -1)]])
    normais = re.search(r"NormalRooms = \{([^}]*)\}", txt)
    if not normais or "D1" not in ids or sorted(contratos) != list(range(len(contratos))):
        return None
    cont = {"salas": salas, "contratos": [contratos[i] for i in range(len(contratos))], "d1": ids["D1"],
            "normais": [ids.get(n.strip(), -1) for n in normais.group(1).split(",")]}
    todas = [s for _, cam in cont["contratos"] for c in cam for s in c] + cont["normais"]
    return cont if all(s in salas for s in todas) and all(c is not None for c, _ in cont["contratos"]) else None


def hash32(a, b):
    """Rng.Hash (splitmix32) do Core."""
    x = ((a & M32) * 0x9E3779B1 ^ (b & M32) * 0x85EBCA77) & M32
    x ^= x >> 16
    x = (x * 0x7FEB352D) & M32
    x ^= x >> 15
    x = (x * 0x846CA68B) & M32
    x ^= x >> 16
    return x or 1


class Rng:
    """xorshift32 do Core (Rng.Next/Int)."""
    def __init__(self, seed):
        self.s = seed or 0x9E3779B9

    def int(self, n):
        x = self.s
        x ^= (x << 13) & M32
        x ^= x >> 17
        x ^= (x << 5) & M32
        self.s = x
        return 0 if n <= 0 else x % n


def pagamento_do_dia(data):
    return hash32(data, 0xDA11) & MAXP   # Sim.DailyPayload


def gerar(payload, cont):
    """Content.Generate (contrato do dia e por codigo) -> (carga, camadas com a D1). Porta exata do C# (so inteiros);
    se o Generate ou o NormalRooms mudarem, os vetores do --autoteste ficam vermelhos."""
    rng = Rng(hash32(payload & MAXP, 0x6E4))
    carga = rng.int(len(CARGAS))
    n = 4 + rng.int(3)
    pool, camadas = [], []
    for l in range(n):
        cam = []
        for _ in range(1 if l == 0 else 2 + rng.int(2)):
            if not pool:
                pool.extend(cont["normais"])
            cam.append(pool.pop(rng.int(len(pool))))
        camadas.append(cam)
    return carga, camadas + [[cont["d1"]]]


# ------------------------------------------------------------------ leitura

def _data(s):
    try:
        return datetime.strptime(s, "%Y-%m-%dT%H:%M:%S")
    except ValueError:
        return None


def _f(s):
    try:
        v = float(s)
    except ValueError:
        return None
    return v if math.isfinite(v) else None


def _i(s):
    try:
        return int(s)
    except ValueError:
        return None


def parse(bruta):
    """Linha -> (inst, versao, sessao, utc, t_ses, t_con, ev, a, b, contrato, sala), "v01" ou None. O jogo grava
    best-effort: linha cortada, colada na seguinte, campo a mais/menos ou numero podre e lixo contado, nunca excecao."""
    r = bruta.split(",")
    if len(r) == 9 and _data(r[0]) and re.fullmatch(r"[a-z_]+", r[4]):
        return "v01"
    if len(r) != 11 or not r[1] or not r[3] or not re.fullmatch(r"[a-z_]+", r[6]):
        return None
    utc, ts, tc = _data(r[0]), _f(r[4]), _f(r[5])
    if utc is None or ts is None or tc is None:
        return None
    return (r[1], r[2], r[3], utc, ts, tc, r[6], r[7], r[8], r[9], r[10])


def ler(caminhos):
    """-> (linhas, qualidade). linha = parse(...) + (ordem,). Deduplica por (linha inteira, n-esima repeticao dela no
    arquivo): a linha leva instalacao e sessao, e duas exportacoes do mesmo aparelho se sobrepoem. ordem = (arquivo,
    linha) com os arquivos pela 1a data: o diario e append-only, entao essa ordem e a cronologica."""
    q, arquivos = Counter(), []
    for c in caminhos:
        arquivos += sorted(glob.glob(os.path.join(c, "*.csv")) + glob.glob(os.path.join(c, "*.txt"))) if os.path.isdir(c) else [c]
    lidos = []
    for arq in arquivos:
        q["arquivos"] += 1
        with open(arq, encoding="utf-8-sig", errors="replace") as f:
            texto = f.read().replace("\0", "\n")   # escrita cortada pode deixar NUL: vira quebra, a linha seguinte se salva
        if not texto.strip():
            q["arquivos vazios"] += 1
            continue
        ps, oc = [], Counter()
        for bruta in texto.splitlines():
            bruta = bruta.strip()
            if not bruta or bruta in (CAB_V1, CAB_V01):
                continue
            p = parse(bruta)
            if p == "v01":
                q["linhas v0.1 ignoradas"] += 1
            elif p is None:
                q["linhas ruins"] += 1
            else:
                oc[bruta] += 1
                ps.append(((bruta, oc[bruta]), p))
        lidos.append((min((p[3] for _, p in ps), default=datetime.max), arq, ps))
    vistas, linhas = set(), []
    for n, (_, _, ps) in enumerate(sorted(lidos)):
        for i, (chave, p) in enumerate(ps):
            if chave in vistas:
                q["linhas duplicadas"] += 1
                continue
            vistas.add(chave)
            linhas.append(p + ((n, i),))
    q["linhas válidas"] = len(linhas)
    return linhas, q


# ------------------------------------------------------------------ analise

def camadas_de(run, cont):
    if not cont:
        return None
    if run["kind"] == "camp":
        return cont["contratos"][run["idx"]][1] if run["idx"] < len(cont["contratos"]) else None
    return gerar(run["payload"], cont)[1] if run["payload"] is not None else None


def jogador(inst, linhas, cont, fuso):
    """Reconstroi as tentativas (runs) de uma instalacao. Um contrato pode comecar numa sessao e acabar noutra (o save
    guarda o contrato em curso): o estado e por instalacao, nao por sessao."""
    J = {"id": inst, "runs": [], "sessoes": {}, "dias": set(), "guilda": [0] * len(GUILDA), "fontes": Counter(),
         "ralos": Counter(), "equip": Counter(), "arma_nivel": Counter(), "outros": Counter(), "chao": [], "x": Counter(),
         "versoes": Counter(), "plataforma": "?", "obs_fim": None}
    aberto = ultimo = None
    arma, sala, camada, sessao = 0, -1, -1, None
    vida, folga, rachas, chao_ini = None, 0, {}, {}
    dia_entregue, dano_ts, ultima_ts, ultima_utc = 0, None, 0.0, None
    for (_, versao, ses, utc, ts, tc, ev, a, b, contrato, _sala, _) in linhas:
        A, B = _i(a), _i(b)
        J["versoes"][versao] += 1
        local = utc + timedelta(hours=fuso)
        if ses != sessao:   # troca de sessao
            if aberto is not None:
                J["x"]["contratos retomados noutra sessão"] += 1
                if dano_ts is not None and ultima_ts - dano_ts <= 5:
                    J["x"]["saiu do app até 5 s depois de dano/rachadura"] += 1
            if ultima_utc is not None and utc < ultima_utc - timedelta(seconds=60):
                J["x"]["relógio voltou entre sessões"] += 1
            sessao, dano_ts = ses, None
            J["sessoes"].setdefault(ses, {"ini": local, "jogo": False, "plataforma": "?"})
        S = J["sessoes"][ses]
        ultima_ts, ultima_utc = ts, utc
        J["obs_fim"] = max(J["obs_fim"] or local, local)
        if ev not in SO_EXPORTAR:
            S["jogo"] = True
        if ev == "session_start":
            S["plataforma"] = J["plataforma"] = b or "?"
        elif ev == "contract_start":
            if aberto is not None:
                J["x"]["contratos sem fim (novo começo antes do fim)"] += 1
            idx = A if A is not None else -1
            kind = "camp" if idx >= 0 else "dia" if contrato == "DIA" else "codigo"
            carga = cont["contratos"][idx][0] if cont and kind == "camp" and idx < len(cont["contratos"]) else None
            aberto = {"cod": contrato or (f"C{idx + 1}" if idx >= 0 else "?"), "idx": idx, "kind": kind, "carga": carga,
                      "payload": None, "data": None, "ses_ini": ses, "ses_fim": None, "dia": local.date(), "fim": None,
                      "t": None, "estrelas": None, "lutas": [], "rotas": [], "salas": set(), "perks": [], "arma": arma,
                      "oferta": False, "aceitou": B == 1, "resgate": None, "saiu_r1": False}
            J["runs"].append(aberto)
            vida, folga, chao_ini = None, 0, {}
        elif aberto is None:
            pass   # eventos de contrato soltos (diario que comeca no meio de um contrato) caem aqui e no bloco de meta abaixo
        elif ev in ("daily_start", "code_start") and A is not None:
            aberto["payload"] = pagamento_do_dia(A) if ev == "daily_start" else A
            if ev == "daily_start":
                aberto["data"] = A
                if A <= dia_entregue:
                    J["x"]["contrato do dia repetido (data já entregue)"] += 1
            if cont:
                aberto["carga"] = gerar(aberto["payload"], cont)[0]
        elif ev == "cargo_rescue_offer":
            aberto["oferta"] = True
        elif ev == "room_enter" and A is not None:
            sala, camada = A, B
            folga += 1
            if cont and A in cont["salas"]:
                aberto["salas"].add(cont["salas"][A][0])
            if aberto["cod"] == "C1" and B is not None and B >= 1:
                aberto["saiu_r1"] = True
        elif ev == "route_choice" and A is not None:
            aberto["rotas"].append((sala, camada, A, B))
        elif ev == "room_fight_choice" and A in (0, 1):
            aberto["lutas"].append(A)
        elif ev == "perk_pick" and A is not None:
            aberto["perks"].append(A)
            folga += 2 if A == CORACAO_EXTRA else 0
        elif ev == "damage_taken" and A is not None and B is not None:
            # vida antes do golpe nunca passa da ultima vida + curas desde ela (1 por sala nova, 2 do Coracao extra);
            # B = 0 e golpe que matou (vida cortada em 0): fica de fora
            if vida is not None and B > 0 and A + B > vida + folga:
                J["x"]["vida voltou depois de reabrir"] += 1
            vida, folga, dano_ts = B, 0, ts
        elif ev == "cargo_integrity" and A is not None and B is not None:
            if B <= rachas.get(A, 0):
                J["x"]["rachadura voltou depois de reabrir"] += 1
            rachas[A], dano_ts = B, ts
        elif ev == "cargo_drop" and A is not None:
            chao_ini[A] = tc
        elif ev == "cargo_pickup" and A in chao_ini:
            J["chao"].append(tc - chao_ini.pop(A))
        elif ev == "contract_stars":
            aberto["estrelas"] = A
        elif ev == "contract_end":
            aberto.update(fim=B, t=tc, ses_fim=ses)
            if aberto["kind"] == "dia" and B == 1 and aberto["data"]:
                dia_entregue = max(dia_entregue, aberto["data"])
            ultimo, aberto = aberto, None
        # meta (fora do contrato) e eventos que chegam depois do contract_end
        if ev == "cargo_rescue_result" and ultimo is not None:
            ultimo["resgate"] = B
        elif ev == "guild_upgrade" and A is not None and B is not None and 0 <= A < len(GUILDA):
            J["guilda"][A] = max(J["guilda"][A], B)
        elif ev == "currency_source" and A is not None and B is not None:
            J["fontes"][B] += A
        elif ev == "currency_sink" and A is not None and B is not None:
            J["ralos"]["armas" if B >= 10 else "guilda"] += A
            if B >= 10:
                J["arma_nivel"][B - 10] += 1
        elif ev == "weapon_equip" and A is not None:
            arma = A
            J["equip"][A] += 1
        elif ev in ("share_code", "settings", "diary_export", "save_refused"):
            J["outros"][ev] += 1
    J["dias"] = {S["ini"].date() for S in J["sessoes"].values() if S["jogo"]}
    if aberto is not None:
        J["x"]["contratos em aberto no fim do diário"] += 1
    primeiro = next((r for r in J["runs"]), None)
    J["completo"] = primeiro is None or primeiro["cod"] == "C1"   # sem o C1 no comeco = diario cortado ou save antigo
    return J


def analisar(linhas, cont, fuso=-3.0, versao=None, plataforma=None):
    if versao:
        linhas = [l for l in linhas if l[1] == versao]
    por = defaultdict(list)
    for l in linhas:
        por[l[0]].append(l)
    js = [jogador(i, sorted(ls, key=lambda l: l[-1]), cont, fuso) for i, ls in sorted(por.items())]
    if plataforma:
        js = [J for J in js if J["plataforma"] == plataforma]
    return js


# ------------------------------------------------------------------ metricas

def mediana(v):
    return statistics.median(v) if v else None


def p75(v):
    v = sorted(v)
    return None if not v else v[0] if len(v) == 1 else statistics.quantiles(v, n=4, method="inclusive")[2]


def pc(n, d):
    return None if not d else 100.0 * n / d


def fp(v):
    return "-" if v is None else f"{v:.0f}%"


def num(v):
    return "-" if v is None else f"{v:g}"


def mmss(s):
    return "-" if s is None else f"{int(s) // 60}:{int(s) % 60:02d}"


def rotulo(r):
    return r["cod"] if r["kind"] == "camp" else "dia" if r["kind"] == "dia" else "código"


FUNIL = [
    ("abriu o jogo", lambda J, rs: True),
    ("começou o C1", lambda J, rs: any(r["cod"] == "C1" for r in rs)),
    ("saiu da 1ª sala do C1 (tutorial)", lambda J, rs: any(r["saiu_r1"] for r in rs)),
    ("entregou o C1", lambda J, rs: any(r["cod"] == "C1" and r["fim"] == 1 for r in rs)),
    ("começou um 2º contrato depois de entregar o C1", lambda J, rs: _depois_c1(rs) is not None),
    ("começou o C4 (fim dos tutoriais)", lambda J, rs: any(r["cod"] == "C4" for r in rs)),
    ("chegou ao mini-chefe (sala M1)", lambda J, rs: any("M1" in r["salas"] for r in rs)),
    ("chegou ao chefe (sala B1)", lambda J, rs: any("B1" in r["salas"] for r in rs)),
    ("venceu o chefe (C24 entregue)", lambda J, rs: any(r["cod"] == "C24" and r["fim"] == 1 for r in rs)),
]


def _depois_c1(rs):
    """Contrato comecado depois da 1a entrega do C1 (ou None)."""
    k = next((i for i, r in enumerate(rs) if r["cod"] == "C1" and r["fim"] == 1), None)
    return None if k is None or k + 1 >= len(rs) else rs[k + 1] | {"c1": rs[k]}


def funil(js):
    """Conta por engajamento: cada jogador conta ate o 1o passo que nao fez, entao nenhum passo passa do anterior.
    So entram diarios com o comeco (1o contrato = C1)."""
    n = [0] * len(FUNIL)
    for J in js:
        if not J["completo"]:
            continue
        for k, (_, f) in enumerate(FUNIL):
            if not f(J, J["runs"]):
                break
            n[k] += 1
    assert all(n[k] <= n[k - 1] for k in range(1, len(n))), "funil com razão > 100%: relatório inválido"
    return n


def h2(js, cont):
    """Escolha de porta (H2), como o Bot.MeasureRoute: bifurcacao = (contrato, camada, sala atual) com 2+ portas."""
    visitas, carga, perdidas = defaultdict(Counter), defaultdict(Counter), 0
    for J in js:
        for r in J["runs"]:
            cam = camadas_de(r, cont)
            for sala, camada, porta, destino in r["rotas"]:
                ops = cam[camada + 1] if cam and camada is not None and 0 <= camada + 1 < len(cam) else None
                if not ops or porta >= len(ops) or ops[porta] != destino:
                    perdidas += 1   # versao diferente do Defs.cs lido, ou diario cortado
                    continue
                if len(ops) < 2:
                    continue
                visitas[(r["cod"] if r["kind"] == "camp" else r["payload"], camada, sala)][porta] += 1
                info = [cont["salas"][o] for o in ops]
                c, m = carga[r["carga"]], RISCO_CARGA[r["carga"]] if r["carga"] is not None else 0
                c["n"] += 1
                if m and any(i[2] & m == 0 for i in info):            # havia porta sem risco para esta carga
                    c["fr"] += 1
                    c["er"] += bool(info[porta][2] & m)
                if any(i[2] == 0 for i in info) and any(i[2] for i in info):
                    c["fi"] += 1
                    c["ei"] += bool(info[porta][2])
                if len({i[1] for i in info}) > 1:
                    c["fa"] += 1
                    c["ea"] += info[porta][1] == max(i[1] for i in info)
                if any(i[3] for i in info) and not all(i[3] for i in info):
                    c["fb"] += 1
                    c["eb"] += bool(info[porta][3])
    boas = [v for v in visitas.values() if sum(v.values()) >= 3]   # bifurcacao vista 1-2 vezes e sempre "modal"
    modal = pc(sum(max(v.values()) for v in boas), sum(sum(v.values()) for v in boas))
    return modal, len(boas), sum(sum(v.values()) for v in boas), carga, perdidas


def h3(js):
    """Derrota involuntaria -> comecou o proximo contrato? Com pendencia (morreu/roubada: a oferta de resgate vem no
    proximo) x sem (falha de carga). Desistir fica de fora (nao e derrota sofrida)."""
    g = {k: Counter() for k in ("com", "sem")}
    for J in js:
        for i, r in enumerate(J["runs"]):
            if r["fim"] is None or r["fim"] in (1, 6):
                continue
            prox = J["runs"][i + 1] if i + 1 < len(J["runs"]) else None
            c = g["com" if r["fim"] in PENDENTE else "sem"]
            c["n"] += 1
            c["mesma"] += prox is not None and prox["ses_ini"] == r["ses_fim"]
            c["depois"] += prox is not None
    return g


def retencao(js, n):
    """Dn = jogou (sessao com evento de jogo) no dia local primeiro+n. Elegivel = o diario vai ate esse dia (o
    diary_export do fim do teste marca a data mesmo sem jogar)."""
    el = ret = 0
    for J in js:
        if not J["dias"]:
            continue
        d0 = min(J["dias"])
        if J["obs_fim"].date() >= d0 + timedelta(days=n):
            el += 1
            ret += (d0 + timedelta(days=n)) in J["dias"]
    return ret, el


def por_dia_retidos(js):
    """Mediana, entre quem jogou em 2+ dias, da media de contratos comecados por dia ativo (GDD sec.15: 3+)."""
    v = []
    for J in js:
        if len(J["dias"]) >= 2:
            c = Counter(r["dia"] for r in J["runs"])
            v.append(sum(c[d] for d in J["dias"]) / len(J["dias"]))
    return mediana(v), len(v)


def h1_jogador(J):
    l = [x for r in J["runs"] for x in r["lutas"]]
    return pc(sum(l), len(l)) if len(l) >= 5 else None   # ponytail: 5 salas minimo; abaixo disso o % e ruido


# ------------------------------------------------------------------ portoes

def status(v, ok, atencao=lambda v: False):
    return "SEM DADOS" if v is None else "OK" if ok(v) else "ATENÇÃO" if atencao(v) else "FALHA"


def portoes(js, cont):
    """-> [(chave, status, texto)]. ATENÇÃO = faixa ITERAR do GDD sec.26 quando ela existe; senao, ate 10 pp abaixo do alvo
    (convencao deste relatorio)."""
    out = []
    fn, comp = funil(js), [J for J in js if J["completo"]]

    def pct_alvo(chave, n, d, alvo, texto, fonte):
        v = pc(n, d)
        out.append((chave, status(v, lambda v: v >= alvo, lambda v: v >= alvo - 10), f"{texto} >= {alvo}%: {fp(v)} ({n}/{d}) - {fonte}"))

    pct_alvo("tutorial", fn[2], fn[1], 88, "tutorial (saiu da 1ª sala do C1, entre quem começou o C1)", "GDD §15")
    pct_alvo("c1", fn[3], fn[1], 85, "1º contrato entregue (entre quem começou o C1)", "GDD §15/§26")
    mesma = sum(1 for J in comp if (p := _depois_c1(J["runs"])) and p["ses_ini"] == p["c1"]["ses_fim"])
    pct_alvo("c2", mesma, fn[3], 55, "2º contrato na MESMA sessão da entrega do C1", "GDD §15/§26")

    dur = [r["t"] for J in js for r in J["runs"] if r["fim"] == 1 and (r["kind"] != "camp" or r["idx"] >= 3)]
    m = mediana(dur)
    out.append(("duracao", status(m, lambda v: 180 <= v <= 360, lambda v: 90 <= v <= 540),
                f"duração do contrato C4+ e gerados (mediana das entregas) 3-6 min: {mmss(m)} (n={len(dur)}) - CONTRATO_V1 §2"))

    h1 = [v for v in map(h1_jogador, js) if v is not None]
    m = mediana(h1)
    dentro = sum(1 for v in h1 if 30 <= v <= 70)
    out.append(("h1", status(m, lambda v: 30 <= v <= 70, lambda v: True),
                f"H1 luta escolhida em 30-70% das salas (mediana por jogador): {fp(m)}; {dentro}/{len(h1)} jogadores na faixa"
                " - GDD §26/§28 (fora da faixa = ITERAR)"))

    modal, nb, nv, _, _ = h2(js, cont) if cont else (None, 0, 0, None, 0)
    out.append(("h2", status(modal, lambda v: v <= 80, lambda v: True),
                f"H2 mesma porta <= 80% das escolhas (bifurcações vistas 3+ vezes): {fp(modal)} ({nb} bifurcações, {nv} escolhas)"
                " - GDD §28"))

    g = h3(js)
    com, sem = pc(g["com"]["mesma"], g["com"]["n"]), pc(g["sem"]["mesma"], g["sem"]["n"])
    v = None if g["com"]["n"] < 3 or g["sem"]["n"] < 3 else com - sem
    out.append(("h3", status(v, lambda v: v > 0, lambda v: True),
                f"H3 após derrota, começa outro contrato na mesma sessão: com resgate pendente {fp(com)} (n={g['com']['n']}) x "
                f"sem {fp(sem)} (n={g['sem']['n']}); precisa de 3+ em cada grupo - GDD §28"))

    for chave, n, alvo, atencao, fonte in (("d1", 1, 26, 20, "GDD §15/§26 (ITERAR 20-26)"), ("d7", 7, 5, 3.5, "GDD §15/§26 (ITERAR 3,5-5)")):
        ret, el = retencao(js, n)
        v = pc(ret, el)
        out.append((chave, status(v, lambda v: v >= alvo, lambda v: v >= atencao),
                    f"D{n} >= {alvo}%: {fp(v)} ({ret}/{el} elegíveis) - {fonte}; no teste é induzido pelo roteiro, só indicativo"))

    m, n = por_dia_retidos(js)
    out.append(("dia3", status(m, lambda v: v >= 3, lambda v: v >= 2),
                f"3+ contratos por dia ativo entre retidos: {'-' if m is None else f'{m:.1f}'} (n={n}) - GDD §15"))

    x = Counter()
    for J in js:
        x.update(J["x"])
    duros = ("vida voltou depois de reabrir", "rachadura voltou depois de reabrir", "contrato do dia repetido (data já entregue)")
    soma = sum(x[k] for k in duros)
    out.append(("exploit", "OK" if soma == 0 else "FALHA",
                "exploits do §28 com efeito no diário (vida/rachadura restauradas, dia repetido): "
                + (", ".join(f"{k} {x[k]}" for k in duros if x[k]) or "nenhum") + " - GDD §28"))
    for chave, texto in (
            ("carga", "\"carga/entrega\" aparece espontaneamente no feedback como diferencial (pergunta aberta) - GDD §26 CONTINUAR"),
            ("archero", "ninguém descreve como \"Archero genérico\"; maioria NÃO acha o combate \"obrigatório\" nem \"inútil\" - GDD §26/§28 H1"),
            ("crash", "crash-free > 99,5%: o diário não tem evento de crash; contar travamentos relatados/observados - GDD §15")):
        out.append((chave, "MANUAL", texto))
    return out


# ------------------------------------------------------------------ relatorio (Markdown)

def tabela(cab, linhas):
    out = ["| " + " | ".join(cab) + " |", "|" + "|".join("---" for _ in cab) + "|"]
    return out + ["| " + " | ".join(str(c) for c in l) + " |" for l in linhas]


def resultado(rs):
    """Linha da tabela por contrato/carga: tentativas, % entregue/morte/falha de carga/desistiu, duracao, estrelas."""
    fins = [r for r in rs if r["fim"] is not None]
    n, c = len(fins), Counter(r["fim"] for r in fins)
    ent = [r["t"] for r in fins if r["fim"] == 1]
    est = [r["estrelas"] for r in fins if r["estrelas"] is not None]
    return [n, fp(pc(c[1], n)), fp(pc(c[2], n)), fp(pc(sum(c[k] for k in FALHA_CARGA), n)), fp(pc(c[6], n)),
            len(rs) - n, mmss(mediana(ent)), mmss(p75(ent)), mmss(mediana([r["t"] for r in fins])),
            "-" if not est else f"{sum(est) / len(est):.1f}", fp(pc(sum(1 for e in est if e == 3), len(est)))]


CAB_RES = ["tentativas", "entregue", "morte", "falha de carga", "desistiu", "sem fim", "duração entregue (mediana)",
           "p75", "duração todas", "estrelas (média)", "3 estrelas"]


def relatorio(linhas, q, cont, fuso=-3.0, versao=None, plataforma=None):
    js = analisar(linhas, cont, fuso, versao, plataforma)
    runs = [r for J in js for r in J["runs"]]
    s = ["# Dungeon Courier - relatório do diário de playtest", "",
         f"Jogadores (instalações): **{len(js)}** | sessões: {sum(len(J['sessoes']) for J in js)} | contratos: {len(runs)} | "
         f"filtros: versão {versao or 'todas'}, plataforma {plataforma or 'todas'} | fuso da retenção: UTC{fuso:+g}", "",
         "> Amostra de playtest: portão OK = dentro do alvo NESTA amostra, não hipótese validada. Os alvos do GDD §15/§26 são "
         "para coorte de 1,5-2 mil installs; com 5-30 jogadores leia como sinal e confira com a observação e o questionário.", ""]
    vs = Counter()
    for J in js:
        vs.update(J["versoes"])
    if len(vs) > 1:
        s += [f"> **ATENÇÃO: versões misturadas** ({', '.join(f'{k}: {n} linhas' for k, n in vs.most_common())}). "
              "Populações diferentes não se somam: rode de novo com `--versao`.", ""]
    if cont is None:
        s += ["> **Sem Defs.cs** (`--defs`): carga dos contratos e H2 não calculados.", ""]

    s += ["## PORTÕES", ""]
    s += tabela(["status", "portão"], [(f"**{st}**", t) for _, st, t in portoes(js, cont)])
    s += ["", "Não se aplicam à beta: rewarded opt-in, CPI, payer (sem anúncio, sem IAP, sem compra de mídia). "
               "D3 e D30 são acompanhamento (tabela de retenção).", ""]

    s += ["## Dados lidos", ""]
    s += tabela(["item", "quantidade"], [(k, q[k]) for k in ("arquivos", "arquivos vazios", "linhas válidas", "linhas duplicadas",
                                                            "linhas ruins", "linhas v0.1 ignoradas")])
    inc = sum(1 for J in js if not J["completo"])
    perdidas = h2(js, cont)[4] if cont else 0
    s += ["", f"- Diários sem o começo (1º contrato não é o C1: diário cortado ou save de antes): {inc} (fora do funil e dos portões de FTUE).",
          f"- Escolhas de porta que não batem com o conteúdo do Defs.cs lido: {perdidas}.",
          "- Plataformas (sessões): " + (", ".join(f"{k} {n}" for k, n in Counter(S["plataforma"] for J in js for S in J["sessoes"].values()).most_common()) or "-"),
          "- Versões (linhas): " + (", ".join(f"{k} {n}" for k, n in vs.most_common()) or "-"), ""]

    fn = funil(js)
    s += ["## Funil (por jogador, só diários com o começo)", ""]
    s += tabela(["passo", "jogadores", "do passo anterior", "de quem abriu"],
                [(nome, fn[k], fp(pc(fn[k], fn[k - 1])) if k else "-", fp(pc(fn[k], fn[0]))) for k, (nome, _) in enumerate(FUNIL)])

    s += ["", "## Resultado e duração por contrato", "",
          "Alvo de duração (CONTRATO_V1 §2): 3-6 min por contrato a partir do C4 (C1-C3 são tutoriais e podem ser mais curtos). "
          "Duração = tempo de jogo do contrato (`t_contrato`: sem pausa e sem a escolha de perk).", ""]
    grupos = defaultdict(list)
    for r in runs:
        grupos[rotulo(r)].append(r)
    ordem = sorted(grupos, key=lambda k: (0, int(k[1:])) if k[0] == "C" and k[1:].isdigit() else (1, k))
    s += tabela(["contrato", "carga"] + CAB_RES,
                [[k, CARGAS[grupos[k][0]["carga"]] if grupos[k][0]["carga"] is not None and k[0] == "C" else "varia"]
                 + resultado(grupos[k]) for k in ordem])
    est = Counter(r["estrelas"] for r in runs if r["estrelas"] is not None)
    s += ["", "Estrelas por tentativa encerrada: " + " | ".join(f"{k}: {est[k]}" for k in range(4)), ""]

    s += ["## Resultado, luta (H1) e rota (H2) por carga", ""]
    pc_ = defaultdict(list)
    for r in runs:
        pc_[r["carga"]].append(r)
    _, _, _, rota, _ = h2(js, cont) if cont else (None, 0, 0, defaultdict(Counter), 0)
    s += tabela(["carga"] + CAB_RES[:6] + ["H1 % salas com luta", "escolhas de porta", "porta com risco p/ a carga (havendo sem)",
                                           "porta com ícone de risco (havendo sem)", "porta de mais caveiras", "porta com baú"],
                [[CARGAS[c] if c is not None else "?"] + resultado(pc_[c])[:6]
                 + [fp(pc(sum(x for r in pc_[c] for x in r["lutas"]), sum(len(r["lutas"]) for r in pc_[c]))), rota[c]["n"],
                    fp(pc(rota[c]["er"], rota[c]["fr"])) if RISCO_CARGA[c or 0] else "não se aplica",
                    fp(pc(rota[c]["ei"], rota[c]["fi"])), fp(pc(rota[c]["ea"], rota[c]["fa"])), fp(pc(rota[c]["eb"], rota[c]["fb"]))]
                 for c in sorted(pc_, key=lambda c: -1 if c is None else c)])
    s += ["", "H2: a carga muda a rota se as cargas sensíveis (relíquia, ovo, bolo, barril, pesada) evitam a porta com risco para elas "
              "mais que a Caixa/Mensagem evitam a porta com ícone. Mesma régua do `Bot.MeasureRoute` (BALANCE.md).", ""]

    g = h3(js)
    of = [r for r in runs if r["oferta"]]
    s += ["## H3: derrota e resgate", ""]
    s += tabela(["derrota", "n", "outro contrato na mesma sessão", "outro contrato depois (qualquer dia)"],
                [(nome, g[k]["n"], fp(pc(g[k]["mesma"], g[k]["n"])), fp(pc(g[k]["depois"], g[k]["n"])))
                 for k, nome in (("com", "com resgate pendente (morreu / carga roubada)"), ("sem", "sem pendência (carga quebrada, filhote, tempo, explosão)"))])
    s += ["", f"Ofertas de resgate: {len(of)} | aceitas: {sum(r['aceitou'] for r in of)} | resgates entregues: "
              f"{sum(1 for r in runs if r['resgate'] == 1)}. Comparação observacional (os grupos diferem pelo tipo de derrota); "
              "o A/B com e sem oferta precisa de remote config (v0.2).", ""]

    s += ["## Perks, armas e guilda", ""]
    pk = Counter(p for r in runs for p in r["perks"])
    tot = sum(pk.values())
    s += [f"Perks escolhidos ({tot}; a oferta não vai ao diário, então não há taxa de escolha por oferta):", ""]
    s += tabela(["perk", "escolhas", "%"], [(PERKS[p] if 0 <= p < len(PERKS) else p, n, fp(pc(n, tot))) for p, n in pk.most_common()])
    pa = defaultdict(list)
    for r in runs:
        pa[r["arma"]].append(r)
    s += ["", "Arma equipada no começo do contrato:", ""]
    s += tabela(["arma", "contratos", "entregue", "jogadores que equiparam", "níveis comprados"],
                [(ARMAS[a] if 0 <= a < len(ARMAS) else a, len(pa[a]), fp(pc(sum(r["fim"] == 1 for r in pa[a]), sum(r["fim"] is not None for r in pa[a]))),
                  sum(1 for J in js if J["equip"][a]), sum(J["arma_nivel"][a] for J in js)) for a in sorted(set(pa) | {0})])
    s += ["", "Guilda (nível máximo por jogador):", ""]
    s += tabela(["linha", "jogadores que compraram", "nível mediano (entre eles)", "nível máximo"],
                [(GUILDA[k], sum(1 for J in js if J["guilda"][k]), num(mediana([J["guilda"][k] for J in js if J["guilda"][k]])),
                  max((J["guilda"][k] for J in js), default=0)) for k in range(len(GUILDA))])
    fo, ra = Counter(), Counter()
    for J in js:
        fo.update(J["fontes"])
        ra.update(J["ralos"])
    s += ["", "Moedas: entrada " + ", ".join(f"{FONTES[k] if 0 <= k < len(FONTES) else k} {n}" for k, n in sorted(fo.items()))
          + f" | gasto guilda {ra['guilda']}, armas {ra['armas']}", ""]

    dia = [r for r in runs if r["kind"] == "dia"]
    cod = [r for r in runs if r["kind"] == "codigo"]
    s += ["## Contrato do dia e por código", "",
          f"- Do dia: {len(dia)} tentativas, {len({J['id'] for J in js for r in J['runs'] if r['kind'] == 'dia'})} jogadores, "
          f"{len({r['data'] for r in dia})} datas, entregue {fp(pc(sum(r['fim'] == 1 for r in dia), sum(r['fim'] is not None for r in dia)))}.",
          f"- Por código: {len(cod)} tentativas, {len({J['id'] for J in js for r in J['runs'] if r['kind'] == 'codigo'})} jogadores, "
          f"{len({r['payload'] for r in cod})} códigos, entregue {fp(pc(sum(r['fim'] == 1 for r in cod), sum(r['fim'] is not None for r in cod)))}; "
          f"códigos compartilhados (share_code): {sum(J['outros']['share_code'] for J in js)}.", ""]

    s += ["## Retenção (dia local, sessão com jogo; sessão só de exportar não conta)", ""]
    linhas_r = []
    for n in (1, 3, 7, 30):
        ret, el = retencao(js, n)
        linhas_r.append((f"D{n}", el, ret, fp(pc(ret, el)), {1: ">= 26%", 3: ">= 12% (acompanhamento)", 7: ">= 5%", 30: ">= 1,5% (acompanhamento)"}[n]))
    s += tabela(["dia", "elegíveis", "voltaram", "%", "alvo GDD §15"], linhas_r)
    m, n = por_dia_retidos(js)
    s += ["", f"Dias ativos por jogador (mediana): {num(mediana([len(J['dias']) for J in js]))} | contratos por dia ativo entre "
              f"quem voltou em outro dia: {'-' if m is None else f'{m:.1f}'} (n={n}). No teste as 2 sessões são combinadas: "
              "só o retorno FORA delas é espontâneo.", ""]

    x, chao = Counter(), [t for J in js for t in J["chao"]]
    for J in js:
        x.update(J["x"])
    morte_resgate = sum(1 for J in js for a, b in zip(J["runs"], J["runs"][1:]) if a["fim"] in PENDENTE and b["aceitou"] and b["resgate"] == 1)
    rerola = sum(1 for r in cod if r["fim"] == 6 and r["t"] is not None and r["t"] < 30)
    s += ["## Sinais de exploit (GDD §28)", ""]
    s += tabela(["sinal", "contagem", "leitura"], [
        ("vida voltou depois de reabrir", x["vida voltou depois de reabrir"], "FALHA se > 0: o save devia gravar a vida a cada dano"),
        ("rachadura voltou depois de reabrir", x["rachadura voltou depois de reabrir"], "FALHA se > 0: idem para a integridade"),
        ("contrato do dia repetido (data já entregue)", x["contrato do dia repetido (data já entregue)"], "FALHA se > 0: relógio voltado"),
        ("relógio voltou entre sessões", x["relógio voltou entre sessões"], "investigar (também distorce a retenção)"),
        ("saiu do app até 5 s depois de dano/rachadura", x["saiu do app até 5 s depois de dano/rachadura"], "sinal: tentativa de restaurar"),
        ("contratos retomados noutra sessão", x["contratos retomados noutra sessão"], "informativo"),
        ("morte seguida de resgate entregue", morte_resgate, "sinal se um jogador repete muito (bônus < pagamento perdido)"),
        ("código abandonado em menos de 30 s", rerola, "sinal: rerolar o código até sair carga fácil"),
        ("carga no chão > 8 s (ladrão nasce)", f"{sum(1 for t in chao if t > 8)} de {len(chao)}", "estratégia válida; acompanhar"),
        ("save recusado (versão futura)", sum(J["outros"]["save_refused"] for J in js), "deve ser 0 no teste"),
        ("contratos sem fim / em aberto", x["contratos sem fim (novo começo antes do fim)"] + x["contratos em aberto no fim do diário"],
         "diário cortado, app morto ou ainda jogando ao exportar"),
    ])

    s += ["", "## Por jogador (id = 8 primeiros caracteres, o mesmo das Configurações)", ""]
    s += tabela(["id", "plataforma", "sessões", "dias ativos", "contratos", "entregues", "maior C entregue", "H1", "diário com o começo"],
                [(J["id"][:8], J["plataforma"], len(J["sessoes"]), len(J["dias"]), len(J["runs"]), sum(r["fim"] == 1 for r in J["runs"]),
                  max((r["cod"] for r in J["runs"] if r["fim"] == 1 and r["kind"] == "camp"), key=lambda c: int(c[1:]), default="-"),
                  fp(h1_jogador(J)), "sim" if J["completo"] else "não") for J in js])
    s += ["", "Não medido pelo diário: fim de sessão (não há `session_end`: a duração da sessão vai até o último evento), crash, "
              "oferta de perk, desbloqueio de arma, nascimento de ladrão, quadros por segundo.", ""]
    return "\n".join(s)


# ------------------------------------------------------------------ autoteste

def autoteste():
    cont = conteudo(DEFS)
    assert cont, f"não consegui ler o conteúdo de {DEFS}"
    sl, ct = cont["salas"], cont["contratos"]
    cod = {v[0]: k for k, v in sl.items()}
    # formato do Defs.cs: 16 normais + B1, D1, S1, S2, M1; 24 contratos; C12 com o M1 e C24 com o B1 antes da entrega
    assert len(sl) == 21 and len(cont["normais"]) == 16 and len(ct) == 24, (len(sl), len(cont["normais"]), len(ct))
    assert cod["M1"] in ct[11][1][-2] and cod["B1"] in ct[23][1][-2] and all(c[1][-1] == [cont["d1"]] for c in ct)
    assert sl[cont["d1"]][1] == 0 and sl[cod["B1"]][2] & 1 and all(0 <= v[1] <= 3 and 0 <= v[2] <= 7 for v in sl.values())
    assert {b for v in sl.values() for b in (1, 2, 4) if v[2] & b} == {1, 2, 4} and any(v[3] for v in sl.values())
    assert ct[0][0] == 0 and set(c for c, _ in ct) == set(range(len(CARGAS)))   # C1 = Caixa (FTUE); as 7 cargas aparecem
    # Content.Generate: vetores impressos pelo C# (Defs.cs de 2026-10-10). Mudou o Generate ou o NormalRooms? refaca.
    vet = {0: (4, "R1/R14+R3/R5+R7/R15+R2/R13+R12/R4+R11+R8/D1"), 1: (6, "R16/R9+R13+R4/R7+R11/R15+R14/R6+R10/R12+R3+R5/D1"),
           12345: (2, "R7/R6+R14+R12/R1+R2+R11/R8+R4/R13+R3+R5/R16+R10/D1"), MAXP: (3, "R10/R3+R4+R1/R2+R8/R15+R5/D1"),
           pagamento_do_dia(20261010): (3, "R13/R3+R4+R15/R5+R8+R1/R11+R16/R14+R6/D1"),
           pagamento_do_dia(20261011): (6, "R8/R14+R6/R3+R4+R1/R12+R9/D1")}
    assert pagamento_do_dia(20261010) == 18215868 and pagamento_do_dia(20261011) == 32657306
    for p, (carga, txt) in vet.items():
        c, cam = gerar(p, cont)
        assert (c, "/".join("+".join(sl[x][0] for x in l) for l in cam)) == (carga, txt), (p, c, cam)

    class Diario:
        """Escreve um diario v1 sintetico coerente com o conteudo real (portas e salas das camadas)."""
        def __init__(self, inst, versao="1.0.0-beta"):
            self.inst, self.versao, self.l, self.lutas = inst, versao, [], [0, 0]

        def sessao(self, sid, utc, plataforma="Android"):
            self.sid, self.utc, self.ts = sid, datetime.strptime(utc, "%Y-%m-%dT%H:%M:%S"), 0.5
            self.ln("session_start", "Pixel 7", plataforma)

        def ln(self, ev, a="", b="", con="", sala="", tc=0.0):
            self.l.append(f"{self.utc:%Y-%m-%dT%H:%M:%S},{self.inst},{self.versao},{self.sid},{self.ts:.1f},{tc:.1f},{ev},{a},{b},{con},{sala}")
            self.ts += 1
            self.utc += timedelta(seconds=1)

        def contrato(self, rot, fim=1, dur=240.0, salas=None, portas=0, lutas=(1, 0), estrelas=None, oferta=None, perks=(), resgate=None):
            """rot = C1..C24 | DIA:AAAAMMDD | COD:payload. salas = quantas salas atravessa antes do fim (padrao: ate a D1)."""
            if rot[1:].isdigit():
                idx, con, extra = int(rot[1:]) - 1, rot, None
                cam = ct[idx][1]
            else:
                tipo, v = rot.split(":")
                idx, v = -1, int(v)
                cam = gerar(pagamento_do_dia(v) if tipo == "DIA" else v, cont)[1]
                con, extra = ("DIA", ("daily_start", v)) if tipo == "DIA" else ("ABCDEF", ("code_start", v))
            ultima = len(cam) - 1 if salas is None else salas
            passo, tc, k = dur / (2 * ultima + 4), 0.0, 0
            self.ln("contract_start", idx, 1 if oferta else 0, con)
            if oferta is not None:
                self.ln("cargo_rescue_offer", 99, 1 if oferta else 0, con)
            sala = cam[0][0]
            self.ln("room_enter", sala, 0, con)
            if extra:
                self.ln(extra[0], extra[1], 0, con)
            for p in perks:
                self.ln("perk_pick", p, 2, con, tc=tc)
            for camada in range(ultima):
                ops = cam[camada + 1]
                porta = (portas(camada, ops) if callable(portas) else portas) % len(ops)
                tc += passo
                if sl[sala][0][0] == "R":
                    self.luta(lutas[k % len(lutas)], sala, con, tc)
                    k += 1
                self.ln("route_choice", porta, ops[porta], con, tc=tc)
                sala = ops[porta]
                self.ln("room_enter", sala, camada + 1, con, tc=tc)
                tc += passo
            if fim != 1 and sl[sala][0][0] == "R":   # Finish fecha a sala onde o contrato acabou (H1)
                self.luta(lutas[k % len(lutas)], sala, con, tc)
            if fim == 2:
                self.ln("death_room", sala, 0, con, tc=dur)
            e = (3 if dur <= 300 else 2) if fim == 1 else 0
            self.ln("contract_stars", e if estrelas is None else estrelas, idx, con, tc=dur)
            if fim == 1:
                self.ln("currency_source", 100, 0, con, tc=dur)
            self.ln("contract_end", idx, fim, con, tc=dur)
            if resgate is not None:
                self.ln("cargo_rescue_result", 99, resgate, con, tc=dur)

        def luta(self, x, sala, con, tc):
            self.ln("room_fight_choice", x, sala, con, tc=tc)
            self.lutas[0] += x
            self.lutas[1] += 1

    def bom(k):
        """Joga 3 dias (D0, D1, D7) e exporta no D8. Portas variam por jogador e camada; H3: morre no C6 e recomeca na hora,
        quebra a reliquia no C5 e para."""
        d = Diario(f"{k:02d}" + "ab" * 15)
        portas = lambda camada, ops: (k >> camada) % len(ops)
        d.sessao("aaaa0000", "2026-10-01T15:00:00")
        d.contrato("C1", dur=80, portas=portas)
        d.contrato("C2", dur=90, portas=portas)
        d.ln("guild_upgrade", 0, 1)
        d.ln("currency_sink", 60, 0)
        d.contrato("C3", dur=100, portas=portas, perks=(3,))
        d.contrato("C4", dur=240, portas=portas, perks=(6,))
        d.sessao("bbbb1111", "2026-10-02T15:00:00")
        d.contrato("C5", dur=250, portas=portas)
        d.contrato("C6", fim=2, dur=60, salas=0)
        d.contrato("C6", dur=260, portas=portas, oferta=True, resgate=1)
        d.contrato("C5", fim=4, dur=120, salas=1, portas=portas)
        d.sessao("cccc2222", "2026-10-08T15:00:00")
        d.ln("weapon_equip", 1, 1)
        d.contrato("DIA:20261008", dur=300, portas=portas)
        d.contrato("COD:12345", dur=280, portas=portas)
        d.contrato("C7", dur=200, portas=portas)
        d.sessao("dddd3333", "2026-10-09T15:00:00")
        d.ln("settings", "idioma", "pt")
        d.ln("diary_export", "Android")
        return d

    def ruim(k):
        """Comeca o C1, morre na 1a sala e so volta no D8 para exportar."""
        d = Diario(f"{k:02d}" + "cd" * 15)
        d.sessao("eeee0000", "2026-10-01T15:00:00")
        d.contrato("C1", fim=2, dur=40, salas=0, lutas=(0,))
        d.sessao("ffff1111", "2026-10-09T15:00:00")
        d.ln("diary_export", "Android")
        return d

    with tempfile.TemporaryDirectory() as tmp:
        def rodar(arquivos, versao=None, fuso=-3.0, cabecalho=True):
            for f in glob.glob(os.path.join(tmp, "*")):
                os.remove(f)
            for i, ls in enumerate(arquivos):
                with open(os.path.join(tmp, f"j{i:02d}.csv"), "w", encoding="utf-8", newline="\n") as f:
                    f.write("\n".join(([CAB_V1] if cabecalho and ls and not ls[0].startswith("utc,") else []) + ls) + ("\n" if ls else ""))
            linhas, q = ler([tmp])
            js = analisar(linhas, cont, fuso, versao)
            return js, {c: st for c, st, _ in portoes(js, cont)}, relatorio(linhas, q, cont, fuso, versao), q

        # 10 bons: tudo OK
        bons = [bom(k) for k in range(10)]
        js, p, texto, q = rodar([d.l for d in bons])
        assert p == {"tutorial": "OK", "c1": "OK", "c2": "OK", "duracao": "OK", "h1": "OK", "h2": "OK", "h3": "OK", "d1": "OK",
                     "d7": "OK", "dia3": "OK", "exploit": "OK", "carga": "MANUAL", "archero": "MANUAL", "crash": "MANUAL"}, p
        assert funil(js) == [10, 10, 10, 10, 10, 10, 0, 0, 0], funil(js)
        assert all(abs(h1_jogador(J) - 100 * d.lutas[0] / d.lutas[1]) < 1e-9 for J, d in zip(js, bons))
        # D1 10/10, D3 0/10 (elegiveis: exportou no D8), D7 10/10, D30 0 elegiveis; contratos/dia: (4 + 4 + 3) / 3 = 3,7
        assert [retencao(js, n) for n in (1, 3, 7, 30)] == [(10, 10), (0, 10), (10, 10), (0, 0)]
        assert abs(por_dia_retidos(js)[0] - 11 / 3) < 1e-9
        g = h3(js)
        assert (g["com"]["n"], g["com"]["mesma"], g["sem"]["n"], g["sem"]["mesma"], g["sem"]["depois"]) == (10, 10, 10, 0, 10)
        assert f"| C6 | {CARGAS[ct[5][0]]} | 20 | 50% | 50% | 0% | 0% | 0 | 4:20 | 4:20 | 2:40 |" in texto, texto
        assert f"| C1 | {CARGAS[ct[0][0]]} | 10 | 100% | 0% | 0% | 0% | 0 | 1:20 | 1:20 | 1:20 | 3.0 | 100% |" in texto
        assert "| dia | varia | 10 | 100%" in texto and "| código | varia | 10 | 100%" in texto
        assert "| Bumerangue | 30 | 100% | 10 | 0 |" in texto and "| Carga acolchoada | 10 | 50% |" in texto
        assert "| Vida | 10 | 1 | 1 |" in texto and "Ofertas de resgate: 10 | aceitas: 10 | resgates entregues: 10" in texto
        assert "| D1 | 10 | 10 | 100% |" in texto and "| D3 | 10 | 0 | 0% |" in texto and "versões misturadas" not in texto

        # 10 ruins: FTUE e retencao FALHA; o resto sem dados
        js, p, texto, _ = rodar([ruim(k).l for k in range(10)])
        assert p == {"tutorial": "FALHA", "c1": "FALHA", "c2": "SEM DADOS", "duracao": "SEM DADOS", "h1": "SEM DADOS", "h2": "SEM DADOS",
                     "h3": "SEM DADOS", "d1": "FALHA", "d7": "FALHA", "dia3": "SEM DADOS", "exploit": "OK",
                     "carga": "MANUAL", "archero": "MANUAL", "crash": "MANUAL"}, p
        assert funil(js) == [10, 10, 0, 0, 0, 0, 0, 0, 0]

        # fronteiras: 9+1 = 90% (OK); 8+2 = 80% (tutorial ATENCAO >= 78; C1 ATENCAO >= 75); 7+3 = 70% (FALHA nos dois)
        for nb, tut, c1 in ((9, "OK", "OK"), (8, "ATENÇÃO", "ATENÇÃO"), (7, "FALHA", "FALHA")):
            p = rodar([bom(k).l for k in range(nb)] + [ruim(k).l for k in range(10 - nb)])[1]
            assert (p["tutorial"], p["c1"]) == (tut, c1), (nb, p)

        # H2 isolado: 1a bifurcacao de um contrato da campanha em que uma porta tem risco para a carga e a outra nao
        # (achada no conteudo, nao fixada: o balanceamento das salas pode mudar sem quebrar o teste)
        def arriscada(o, m):
            return bool(sl[o][2] & m)
        k6, cg, ops = next((i, c, cam[1]) for i, (c, cam) in enumerate(ct) if RISCO_CARGA[c] and len(cam[1]) == 2
                           and arriscada(cam[1][0], RISCO_CARGA[c]) != arriscada(cam[1][1], RISCO_CARGA[c]))
        ruim_, bom_ = (0, 1) if arriscada(ops[0], RISCO_CARGA[cg]) else (1, 0)

        def h2_teste(escolhas):
            ds = []
            for k, porta in enumerate(escolhas):
                d = Diario(f"h2{k:02d}" + "ef" * 14)
                d.sessao("abcd0000", "2026-10-01T15:00:00")
                d.contrato(f"C{k6 + 1}", fim=6, dur=30, salas=1, portas=porta)
                ds.append(d.l)
            js = rodar(ds)[0]
            m, nb, nv, carga, perd = h2(js, cont)
            return m, nb, nv, pc(carga[cg]["er"], carga[cg]["fr"]), perd
        m, nb, nv, risco, perd = h2_teste([bom_, bom_, ruim_])
        assert (round(m), nb, nv, round(risco), perd) == (67, 1, 3, 33, 0), (m, nb, nv, risco, perd)
        assert h2_teste([0, 0, 0])[0] == 100 and h2_teste([0, 1])[1] == 0   # 2 visitas: fora do modal

        # casos de borda: vazio, cabecalho da v0.1, linhas podres, exportacao repetida (inteira e prefixo), texto do
        # Android 8-9 sem cabecalho e com a 1a linha cortada. O resultado tem de ser o mesmo dos 10 bons.
        v01 = [CAB_V01, "2026-09-30T10:00:00,a1b2c3d4,1.0,0.0,session_start,PC,WindowsPlayer,,",
               "2026-09-30T10:00:05,a1b2c3d4,5.0,2.0,contract_start,0,0,C1,R1", "2026-09-30T10:00:09,a1b2c3d4,9.0,6.0,room_enter,0,0,C1,R1"]
        troca = lambda linha, i, v: ",".join(linha.split(",")[:i] + [v] + linha.split(",")[i + 1:])
        lixo = [bons[0].l[3][:25],                                                 # cortada no meio
                troca(bons[0].l[3], 4, "nan"),                                     # t_sessao nao finito
                bons[0].l[7][:30] + "\0\0\0\0",                                    # NUL de escrita cortada
                bons[0].l[4] + bons[0].l[5],                                       # duas coladas
                troca(bons[0].l[6], 6, "contr@ct")]                                # evento podre
        android89 = [bons[9].l[0][17:]] + bons[9].l[1:]                            # sem cabecalho, 1a linha cortada
        arquivos = [d.l for d in bons] + [[], v01, bons[0].l + lixo, bons[0].l[:40], bons[0].l]
        js, p2, texto, q = rodar(arquivos)
        ref = rodar([d.l for d in bons])
        assert p2 == ref[1] and funil(js) == funil(ref[0]), (p2, ref[1])
        assert (q["arquivos"], q["arquivos vazios"], q["linhas v0.1 ignoradas"], q["linhas ruins"]) == (15, 1, 3, 5), q
        assert q["linhas duplicadas"] == 2 * len(bons[0].l) + 40, q   # o mesmo aparelho exportado mais 3 vezes
        js, p3, _, q = rodar([d.l for d in bons[:9]] + [android89], cabecalho=False)
        assert p3 == ref[1] and q["linhas ruins"] == 1 and len(js) == 10, (p3, q)

        # versoes misturadas: aviso; --versao separa e volta ao resultado dos 10 bons
        outra = ruim(77)
        outra.versao = "1.0.1-beta"
        outra.l = [l.replace(",1.0.0-beta,", ",1.0.1-beta,") for l in outra.l]
        js, p4, texto, _ = rodar([d.l for d in bons] + [outra.l])
        assert "versões misturadas" in texto and len(js) == 11 and p4["c1"] == "OK" and funil(js)[1] == 11
        js, p5, texto, _ = rodar([d.l for d in bons] + [outra.l], versao="1.0.0-beta")
        assert len(js) == 10 and p5 == ref[1] and "versões misturadas" not in texto

        # exploits: vida e rachadura restauradas ao reabrir, dia repetido, relogio voltando. O 2o jogador reabre sem trapaca.
        x = Diario("ee" * 16)
        x.sessao("11110000", "2026-10-01T15:00:00")
        x.ln("contract_start", 3, 0, "C4")
        x.ln("room_enter", cod["R5"], 0, "C4")
        x.ln("damage_taken", 2, 4, "C4", tc=10)
        x.ln("cargo_integrity", 7, 1, "C4", tc=11)
        x.sessao("11111111", "2026-10-01T15:10:00")                    # matou o app 2 s depois: contrato retomado
        x.ln("room_enter", cod["R5"], 0, "C4", tc=11)
        x.ln("damage_taken", 1, 5, "C4", tc=15)                         # vida antes 6 > 4 + 1 de folga: restaurou
        x.ln("cargo_integrity", 7, 1, "C4", tc=16)                      # rachadura 1 de novo: restaurou
        x.ln("contract_stars", 1, 3, "C4", tc=100)
        x.ln("contract_end", 3, 1, "C4", tc=100)
        x.contrato("DIA:20261001", dur=200)
        x.sessao("11112222", "2026-10-01T09:00:00")                    # relogio voltou
        x.contrato("DIA:20261001", dur=200)                             # mesmo dia de novo
        h = Diario("ff" * 16)
        h.sessao("22220000", "2026-10-01T15:00:00")
        h.ln("contract_start", 3, 0, "C4")
        h.ln("room_enter", cod["R5"], 0, "C4")
        h.ln("damage_taken", 2, 4, "C4", tc=10)
        h.ln("cargo_integrity", 7, 1, "C4", tc=11)
        h.ts += 10
        h.ln("cargo_drop", 7, 0, "C4", tc=21)                           # ainda jogou 11 s depois do dano
        h.sessao("22221111", "2026-10-01T16:00:00")
        h.ln("room_enter", cod["R5"], 0, "C4", tc=11)
        h.ln("damage_taken", 1, 4, "C4", tc=15)                         # vida antes 5 <= 4 + 1: ok (cura ao reentrar)
        h.ln("cargo_integrity", 7, 2, "C4", tc=16)
        h.ln("contract_end", 3, 2, "C4", tc=20)                         # morreu (e diario sem o C1: fora do funil)
        js, p, texto, _ = rodar([x.l, h.l])
        assert p["exploit"] == "FALHA", p
        xs = {J["id"][:2]: J["x"] for J in js}
        assert xs["ee"] == Counter({"vida voltou depois de reabrir": 1, "rachadura voltou depois de reabrir": 1,
                                    "contrato do dia repetido (data já entregue)": 1, "relógio voltou entre sessões": 1,
                                    "contratos retomados noutra sessão": 1, "saiu do app até 5 s depois de dano/rachadura": 1}), xs["ee"]
        assert xs["ff"] == Counter({"contratos retomados noutra sessão": 1}), xs["ff"]
        assert funil(js) == [0] * len(FUNIL) and "ee" * 16 not in texto and "| eeeeeeee | Android |" in texto   # so o id curto

        # fuso: 22:00Z e 01:30Z do dia seguinte sao o mesmo dia em Brasilia (19:00 e 22:30): D1 so existe em UTC
        f = Diario("99" * 16)
        f.sessao("33330000", "2026-10-10T22:00:00")
        f.contrato("C1", dur=80)
        f.sessao("33331111", "2026-10-11T01:30:00")
        f.contrato("C2", dur=80)
        f.sessao("33332222", "2026-10-20T15:00:00")
        f.ln("diary_export", "Android")
        assert retencao(rodar([f.l])[0], 1) == (0, 1) and retencao(rodar([f.l], fuso=0)[0], 1) == (1, 1)

        # diario vazio e Defs.cs ausente: SEM DADOS, nunca excecao
        js, p, texto, _ = rodar([[]])
        assert set(p.values()) <= {"SEM DADOS", "OK", "MANUAL"} and p["exploit"] == "OK" and funil(js) == [0] * len(FUNIL)
        rodar([d.l for d in bons])
        assert "Sem Defs.cs" in relatorio(*ler([tmp]), None) and conteudo(os.path.join(tmp, "nada.cs")) is None

        print(rodar([bom(k).l for k in range(8)] + [ruim(k).l for k in range(2)])[2])
    print("\nautoteste OK")


if __name__ == "__main__":
    sys.stdout.reconfigure(errors="replace")   # console cp1252 nao derruba o relatorio
    ap = argparse.ArgumentParser(description="Relatório do diário de playtest do Dungeon Courier (v1.0-beta).")
    ap.add_argument("caminhos", nargs="*", help="arquivos .csv/.txt ou pastas; padrão: o diário do build Windows deste PC")
    ap.add_argument("--autoteste", action="store_true")
    ap.add_argument("--versao")
    ap.add_argument("--plataforma", help="Android ou WindowsPlayer (b do session_start)")
    ap.add_argument("--fuso", type=float, default=-3.0, help="horas somadas ao UTC para a data local (padrão -3, Brasil)")
    ap.add_argument("--defs", default=DEFS, help="Defs.cs com salas e contratos")
    ap.add_argument("--saida", help="grava o Markdown neste arquivo (UTF-8) em vez de imprimir")
    o = ap.parse_args()
    if o.autoteste:
        autoteste()
        sys.exit(0)
    caminhos = o.caminhos or [PADRAO]
    faltando = [c for c in caminhos if not os.path.exists(c)]
    if faltando:
        print("não encontrei:", ", ".join(faltando))
        sys.exit(2)
    md = relatorio(*ler(caminhos), conteudo(o.defs), o.fuso, o.versao, o.plataforma)
    if o.saida:
        with open(o.saida, "w", encoding="utf-8") as f:
            f.write(md + "\n")
        print("relatório gravado em", o.saida)
    else:
        print(md)
