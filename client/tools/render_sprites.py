"""render_sprites.py - Blender headless: personagem 3D rigado + clipes -> folhas de sprite 2D (Dungeon Courier).

Copia adaptada de 03_FORGE_STREET/client/tools/render_sprites.py (2026-10-06/08: retarget por delta de repouso, modo
estatico para props). O que o DC acrescenta (docs/RENDER_SPRITES.md):
  --preset <ator>    escala, direcoes e celula do ator (tabela PRESETS = docs/ART_BIBLE.md sec. 4-5 e docs/ASSETS.md)
  --height/--width   normaliza o modelo para a medida de projeto (a altura do Tripo muda de modelo para modelo)
  --mirror           renderiza so as direcoes de S a N pelo oeste; o lado leste e o espelho (flipX na View)
  --hold FBX         os clipes "Carry*" levam a pose dos bracos (ombro..dedos) do quadro --hold-frame deste clipe
  --outline PX       contorno de tinta (INK) assado na folha, por baixo do sprite
  --contact PNG      folha de conferencia fora de Resources (Resources vai inteiro para o APK)
  --attach SPEC      prop preso a um osso (escudo no antebraco): FBX:osso:dx,dy,dz:rx,ry,rz:tamanho (repetivel)
  meta.json "hands"  por clipe: ponto entre as maos (x, y em px na celula; 1 = na frente do corpo) por direcao x quadro
  meta.json "stride_mps"  clipe em laco com deslocamento do quadril: velocidade natural (m/s) na escala do ator
  preset "clips"     por clipe: frames = quadros fixos (corrida sem subquadro); range = faixa do clipe de origem
                     (janela de laco do Breathing Idle, que inteiro tem 9,9 s); vai para o meta como "src_range"
  --align-arms       rig em pose A (bruto, ogro): os ossos do braco partem do repouso em T do clipe; sem a opcao, liga
                     sozinho se o braco do rig estiver a mais de 15 graus do clipe (AVISO no log)
  preset ground_cut  clipes cortados no chao (z = 0) no render: Emerge/Burrow da toupeira e da rainha
Quadril: o deslocamento horizontal comeca na origem (1o quadro) e, em clipe em laco, a deriva linear sai (clipe com
root motion vira "in place"; o pe nao patina se a View tocar no ritmo de stride_mps).

Uso (Blender 5.2; render_sprites.ps1 ao lado monta esta linha):
  blender.exe -b --factory-startup --python-exit-code 1 --python render_sprites.py --
      --model Model.fbx --out saida/ (--anims pasta_com_fbx/ | --self) [--preset courier] [--hold Segurar.fbx]
      [--elev 60] [--dirs 4|8] [--mirror] [--fps 12] [--size 128] [--scale PX_POR_METRO] [--height M | --width M]
      [--yaw 0] [--light flat|matcap|studio] [--shadow] [--aa 8] [--outline 3] [--src-fps 30] [--contact x.png]
      [--attach Escudo.fbx:LeftForeArm:0.09,0,0.06:90,180,0:0.75]
Saida: <Clipe>.png (linha = direcao, coluna = quadro; linha 0 em cima), meta.json, contact.png.
Direcoes em sentido horario a partir de S (de frente para a camera = para baixo na tela):
  4 -> S, W, N, E      8 -> S, SW, W, NW, N, NE, E, SE      --mirror: so S..N pelo oeste (4 -> 3 linhas, 8 -> 5)
Pivo = origem do modelo (pe no chao), em px a partir do canto inferior esquerdo da celula.
Tecnica: UM render por clipe. Cada celula e uma copia da malha avaliada no frame (pose aplicada), girada
em Z e transladada no plano da camera (em ORTHO a translacao no plano nao muda a imagem). Sem PIL.
"""
import argparse, glob, json, math, os, re, sys, time
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

DIRS = {1: ["S"], 4: ["S", "W", "N", "E"], 8: ["S", "SW", "W", "NW", "N", "NE", "E", "SE"]}
LIGHT = {"flat": ("FLAT", None), "matcap": ("MATCAP", "toon_light.exr"), "studio": ("STUDIO", "Default")}
BONE_RE = re.compile(r'pose\.bones\["([^"]+)"\]')
LOOP_RE = re.compile(r"idle|walk|run|lurk|look|sleep|stun|charge|perch|hold|fly", re.I)  # ponytail: laco por nome; o Unity pode sobrescrever
ARM_RE = re.compile(r"(Left|Right)(Shoulder|\w*Arm|Hand)")  # ombro, braco, antebraco, mao, dedos (Mixamo e placeholder COE)
HAND_RE = re.compile(r"(Left|Right)Hand$")
ALIGN_SEEN = set()  # clipes alinhados sozinhos (rig em pose A sem --align-arms); vai para o meta
# FBX:osso:dx,dy,dz:rx,ry,rz:tamanho (o osso pode vir com prefixo "mixamorig:"; caminho do Windows tem "C:")
ATTACH_RE = re.compile(r"^(.+?\.(?:fbx|glb|gltf)):(?:\w+:)?(\w+):([-\d.]+,[-\d.]+,[-\d.]+):([-\d.]+,[-\d.]+,[-\d.]+):([\d.]+)$", re.I)
INK = (0x1B / 255.0, 0x17 / 255.0, 0x24 / 255.0)  # tinta do contorno, ART_BIBLE sec. 3

# Padroes genericos e tabela de atores do DC. Um argumento na linha vence o preset, que vence BASE.
# ponytail: escala por ator numa tabela so; mudar aqui = re-render daquele ator, nada muda no Core
BASE = dict(elev=60.0, dirs=4, fps=12.0, size=128, scale=0.0, yaw=0.0, light="flat", aa="8", max_frames=32,
            height=0.0, width=0.0, mirror=False, outline=0, clips={}, align_arms=False, ground_cut=(), footprint=(),
            tilt=0.0)
DC = dict(scale=120.0, outline=3, max_frames=12)  # 120 px/m na textura; a View mostra ~112 px/m (9,6 m em 1080 px)
# Por clipe (preset "clips"): frames = quadros fixos (vence fps e teto); range = (ini, fim) quadros do clipe de origem.
# BREATH: janela de laco do Breathing Idle (Forge, 1-299 = 9,9 s; tocado inteiro em 12 quadros dava 1,2 fps). 199-289 =
# 3,0 s com a menor costura entre as janelas de 2-3 s: 2,2 cm na mao direita (X Bot de 1,8 m; ~1,6 cm no courier)
BREATH = dict(range=(199, 289))
EMERGE = dict(frames=6)  # sobe/mergulha em 0,3 s: 6 quadros de 1,5 em 1,5 (o padrao daria 4)
RUN8 = dict(frames=8)  # Sprint do ARKANA: ciclo de 16 quadros de 2 em 2, sem subquadro (6 quadros pulavam 2,67)
SIX = dict(frames=6)  # clipes de 0,3 s com o ultimo quadro amostrado ja na pose final
PRESETS = {
    "courier": dict(DC, height=1.30, dirs=8, mirror=True, size=224,
                    clips={"Idle": BREATH, "CarryIdle": BREATH, "Run": RUN8, "CarryRun": RUN8, "Throw": dict(frames=10)}),
    "courier_fim": dict(DC, height=1.30, dirs=1, size=320),
    "ladrao": dict(DC, height=1.00, dirs=4, mirror=True, size=160),
    "guarda": dict(DC, height=1.50, dirs=4, mirror=True, size=224),
    "cliente": dict(DC, height=1.35, dirs=1, size=192, clips={"Idle": BREATH}),
    # bruto e ogro: rig do Tripo feito de imagem em pose A (braco em repouso a -49,5 / -22 graus; X Bot a 0): align_arms
    # bruto: celula 336 (era 256) porque os bracos longos no alto do Smash e o punho a frente saiam da celula (2026-10-10)
    "bruto": dict(DC, height=1.80, dirs=4, mirror=True, size=336, align_arms=True, clips={"Idle": BREATH}),
    # ogro: 448 x 9 (era 384 x 10): o chefe de 2,8 m com os punhos no alto do Slam nao cabia em 384 (2026-10-10)
    "ogro": dict(DC, height=2.80, dirs=4, mirror=True, size=448, max_frames=9, align_arms=True, clips={"Idle": BREATH}),
    # ogro_fim: 576 x 7 (era 448 x 9): o chefe deitado de costas mede ~4,3 m na tela (2026-10-10)
    "ogro_fim": dict(DC, height=2.80, dirs=1, size=576, max_frames=7, align_arms=True),
    # Lote 3 (2026-10-10): arqueiro com o arco preso a mao esquerda (--attach na linha), capitao com o Hold do ladrao
    "arqueiro": dict(DC, height=1.35, dirs=4, mirror=True, size=224, clips={"Idle": BREATH}),
    "capitao": dict(DC, height=1.25, dirs=4, mirror=True, size=192, clips={"Idle": BREATH}),
    "limo": dict(DC, height=0.60, dirs=1, size=128),
    "bolo": dict(DC, width=0.60, dirs=1, size=112),
    "mensagem": dict(DC, width=0.45, dirs=1, size=96),
    "caixa_pesada": dict(DC, width=0.70, dirs=1, size=128),
    "barril": dict(DC, height=0.55, dirs=1, size=96),
    "tocha": dict(DC, height=0.70, dirs=1, size=112),
    "pilar": dict(DC, width=1.20, dirs=1, size=288),  # 1,2 m de largura -> ~2,2 m de altura (proporcao do modelo)
    # Lote 4 (regiao 2, 2026-10-11): pasta = nome do zip (RENDER_SPRITES.md tem a correspondencia com o CONTEUDO_REGIOES)
    "goblin_mineiro": dict(DC, height=1.00, dirs=4, mirror=True, size=192, clips={"Idle": BREATH}),
    # toupeira e rainha: Emerge/Burrow entram no chao; ground_cut corta a parte enterrada no render (a celula nao cresce)
    "toupeira": dict(DC, height=0.90, dirs=4, mirror=True, size=192, clips=dict(Idle=BREATH, Emerge=EMERGE, Burrow=EMERGE),
                     ground_cut=("Emerge", "Burrow")),
    "rainha_toupeira": dict(DC, height=1.60, dirs=4, mirror=True, size=304, clips=dict(Idle=BREATH, Emerge=EMERGE, Burrow=EMERGE),
                            ground_cut=("Emerge", "Burrow")),
    "rainha_toupeira_fim": dict(DC, height=1.60, dirs=1, size=320),  # 12 x 320 = 3 840 px (384 passava de 4 096)
    "golem": dict(DC, height=2.60, dirs=4, mirror=True, size=448, max_frames=9, clips={"Idle": BREATH}),
    "golem_fim": dict(DC, height=2.60, dirs=1, size=576, max_frames=7),
    "picareta": dict(DC, height=0.45, dirs=1, size=96),
    "sapo": dict(DC, width=0.90, dirs=1, size=144),
    "besouro": dict(DC, width=0.60, dirs=1, size=112),
    "vagonete": dict(DC, width=1.20, dirs=1, size=208),
    "coracao": dict(DC, height=0.45, dirs=1, size=96),
    "cristal": dict(DC, width=0.50, dirs=1, size=96),
    "gaiola": dict(DC, height=0.50, dirs=1, size=96),
    "lanterna_mina": dict(DC, height=0.50, dirs=1, size=96),
    "escora": dict(DC, width=1.20, dirs=1, size=176),
    # Lote 5 (regiao 3, 2026-10-11): clava e cajado presos a RightHand pelo --attach na linha
    "esqueleto": dict(DC, height=1.35, dirs=4, mirror=True, size=256, clips=dict(Idle=BREATH, Collapse=SIX, Rise=SIX)),
    "mumia": dict(DC, height=1.50, dirs=4, mirror=True, size=256, clips=dict(Idle=BREATH, Hold=dict(frames=8))),
    "gargula": dict(DC, height=1.40, dirs=4, mirror=True, size=288, clips=dict(Statue=dict(frames=1))),
    "necromante": dict(DC, height=1.60, dirs=4, mirror=True, size=320),
    "necromante_fim": dict(DC, height=1.60, dirs=1, size=320),
    "clava": dict(DC, width=0.49, dirs=1, size=96),
    "cajado": dict(DC, height=1.40, dirs=1, size=144),
    "fantasma": dict(DC, height=0.90, dirs=1, size=128),
    "aranha": dict(DC, width=0.80, dirs=1, size=128),
    "aranha_empinada": dict(DC, width=0.80, dirs=1, size=144, tilt=35.0),
    "viuva": dict(DC, width=2.00, dirs=1, size=320),
    "viuva_empinada": dict(DC, width=2.00, dirs=1, size=352, tilt=30.0),
    "lanterna": dict(DC, height=0.45, dirs=1, size=96),
    "lanterna_apagada": dict(DC, height=0.45, dirs=1, size=96),  # o mesmo FBX com o basecolor da chama apagada
    "espelho": dict(DC, height=0.50, dirs=1, size=96),
    "balao": dict(DC, width=0.60, dirs=1, size=128),
    "lapide": dict(DC, width=1.20, dirs=1, size=176),
    "vela": dict(DC, height=0.70, dirs=1, size=112),
    # sarcofago: planta 0,8 x 1,8 (o modelo e 1,4:1); 4 direcoes porque a sala R3-5 tem o obstaculo em pe e deitado
    "sarcofago": dict(DC, footprint=(0.80, 1.80), dirs=4, mirror=True, size=288),
    # icone da loja (loja_assets.py): 3/4 de frente (SW), camera mais baixa (35 graus), escala de icone; caixa a parte
    "courier_icone": dict(DC, height=1.30, dirs=1, size=512, scale=320.0, elev=35.0, yaw=-45.0, outline=6,
                          clips=dict(CarryIdle=dict(range=(199, 289), frames=1))),
    "caixa_icone": dict(DC, width=0.50, dirs=1, size=256, scale=320.0, elev=35.0, yaw=-45.0, outline=6),
    # Lote 6 (regiao 4, 2026-10-11): frasco na RightHand do kobold e chicote na do Capataz pelo --attach na linha;
    # brasa = o modelo do magma a 1,10 m (0 credito)
    "diabrete": dict(DC, height=0.90, dirs=4, mirror=True, size=192, clips=dict(Idle=BREATH, Swap=SIX, Vanish=SIX)),
    "kobold": dict(DC, height=1.00, dirs=4, mirror=True, size=192, clips={"Idle": BREATH}),
    "magma": dict(DC, height=1.90, dirs=4, mirror=True, size=336, clips={"Idle": BREATH}),
    "brasa": dict(DC, height=1.10, dirs=4, mirror=True, size=208, clips={"Idle": BREATH}),
    "capataz": dict(DC, height=1.40, dirs=4, mirror=True, size=256, clips={"Idle": BREATH}),
    "capataz_fim": dict(DC, height=1.40, dirs=1, size=320),
    "frasco": dict(DC, height=0.30, dirs=1, size=96),
    "chicote": dict(DC, width=0.60, dirs=1, size=112),
    "salamandra": dict(DC, width=0.80, dirs=1, size=128),
    "planta": dict(DC, height=0.55, dirs=1, size=112),
    "planta_aberta": dict(DC, height=0.596, dirs=1, size=128),  # boca escancarada (planta_aberta.py); 0,596 = a mesma escala da planta (o lobulo aberto subiu a altura de 0,981 para 1,063)
    "mapa": dict(DC, width=0.45, dirs=1, size=96),
    "bigorna": dict(DC, width=1.20, dirs=1, size=176),  # veste o obstaculo 1,2 x 0,8 da forja
    "braseiro": dict(DC, height=0.70, dirs=1, size=176),  # a bacia e mais larga que alta: 112 recortava
    "respiro": dict(DC, width=2.00, dirs=1, size=272),
    "corrente": dict(DC, height=1.50, dirs=1, size=160),
    # dragao: rig refeito e clipes do dragao_blender.py (--self); 3,5 m de comprimento; 576 x 7 quadros = 4 032 px
    "dragao": dict(DC, width=3.50, dirs=4, mirror=True, size=576, max_frames=7,
                   clips=dict(Idle=dict(frames=4), Fly=dict(frames=6), Land=dict(frames=6))),
    "dragao_fim": dict(DC, width=3.50, dirs=1, size=576, max_frames=7, ground_cut=("Dying",)),
    "morcego": dict(DC, width=0.70, dirs=1, size=112),
    "filhote": dict(DC, height=0.50, dirs=1, size=96),
    "caixa": dict(DC, width=0.50, dirs=1, size=112),
    "reliquia": dict(DC, height=0.55, dirs=1, size=96),
    "ovo": dict(DC, height=0.55, dirs=1, size=96),
    "bau": dict(DC, width=0.80, dirs=1, size=160),
    "bau_aberto": dict(DC, width=0.80, dirs=1, size=160),
    "arco": dict(DC, width=2.20, dirs=1, size=448),
}


def die(msg):
    print("ERRO " + msg)
    sys.exit(1)


def parse_args():
    p = argparse.ArgumentParser()
    p.add_argument("--model", required=True, help="FBX/GLB rigado (ou o FBX de um clipe do placeholder); .blend = o FBX ja importado e salvo")
    p.add_argument("--out", required=True)
    p.add_argument("--anims", help="pasta com 1 FBX por clipe (Mixamo Without Skin ou placeholder)")
    p.add_argument("--self", dest="self_", action="store_true", help="usar as actions ja no modelo")
    p.add_argument("--preset", choices=sorted(PRESETS), help="ator do DC (tabela PRESETS)")
    p.add_argument("--hold", help="FBX de um clipe com as maos segurando a carga; os clipes Carry* levam os bracos dele")
    p.add_argument("--hold-frame", type=float, help="quadro do --hold (padrao: o primeiro)")
    p.add_argument("--contact", help="caminho do contact.png (padrao: <out>/contact.png)")
    p.add_argument("--src-fps", type=int, default=30, help="fps dos clipes de origem")
    p.add_argument("--shadow", action="store_true", help="auto-sombra do Workbench (nao ha sombra no chao)")
    p.add_argument("--attach", action="append", default=[],
                   help="prop preso a osso: FBX:osso:dx,dy,dz:rx,ry,rz:tamanho; deslocamento (m) a partir da cabeca do "
                        "osso e giro (graus, Euler XYZ) em eixos do mundo NO REPOUSO (T) do modelo ja normalizado; "
                        "tamanho = maior medida do prop em m; o centro da caixa do prop fica no ponto")
    # sem valor na linha: vem do --preset, senao de BASE
    p.add_argument("--elev", type=float, help="graus da camera sobre o chao (90 = de cima)")
    p.add_argument("--dirs", type=int, choices=(1, 4, 8))
    p.add_argument("--mirror", action="store_true", default=None, help="so S..N pelo oeste; leste = espelho")
    p.add_argument("--fps", type=float)
    p.add_argument("--size", type=int, help="px por celula")
    p.add_argument("--scale", type=float, help="px por metro (0 = ajustar ao personagem)")
    p.add_argument("--height", type=float, help="altura do modelo em m depois de normalizar (0 = como veio)")
    p.add_argument("--width", type=float, help="maior medida horizontal em m (props; vale se --height = 0)")
    p.add_argument("--yaw", type=float, help="graus somados a toda direcao (modelo que nao olha -Y)")
    p.add_argument("--light", choices=tuple(LIGHT))
    p.add_argument("--aa", help="OFF|FXAA|5|8|11|16|32")
    p.add_argument("--max-frames", type=int, help="teto de quadros por clipe (largura da folha)")
    p.add_argument("--outline", type=int, help="px de contorno de tinta (0 = sem)")
    p.add_argument("--align-arms", action="store_true", default=None,
                   help="rig em pose A: os ossos do braco partem do repouso do clipe (T), nao do proprio")
    a = p.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    for k, v in dict(BASE, **PRESETS.get(a.preset, {})).items():
        if getattr(a, k, None) is None:
            setattr(a, k, v)
    return a


def import_file(path):
    if path.lower().endswith(".blend"):  # FBX do Tripo ja importado e salvo (2026-10-11): o import do FBX levava 10+ min
        with bpy.data.libraries.load(path) as (src, dst):  # com o PC sem memoria e estourava o corte de 900 s
            dst.objects, dst.actions = list(src.objects), list(src.actions)  # actions: o dragao vem com os clipes (--self)
        for o in dst.objects:
            bpy.context.scene.collection.objects.link(o)
        return list(dst.objects)
    before = set(bpy.data.objects)
    (bpy.ops.import_scene.gltf if path.lower().endswith((".glb", ".gltf")) else bpy.ops.import_scene.fbx)(filepath=path)
    return [o for o in bpy.data.objects if o not in before]


def action_bones(act):  # API em camadas do 5.x (action.fcurves nao existe mais)
    paths = " ".join(fc.data_path for ly in act.layers for st in ly.strips for cb in st.channelbags for fc in cb.fcurves)
    return set(BONE_RE.findall(paths))


def set_action(arm, act):
    if arm is None or act is None:  # prop estatico: nada a animar
        bpy.context.view_layer.update()
        return
    ad = arm.animation_data or arm.animation_data_create()
    ad.action = act
    if act.slots:
        ad.action_slot = act.slots[0]
    bpy.context.view_layer.update()


def bone_order(arm):
    out = []

    def walk(b):
        out.append(b.name)
        for c in b.children:
            walk(c)
    for b in arm.data.bones:
        if b.parent is None:
            walk(b)
    return out


def model_points(meshes):
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    return [ev.matrix_world @ v.co for ev in (m.evaluated_get(dg) for m in meshes) for v in ev.data.vertices]


def transform_roots(model, M):
    for o in model:
        if o.parent is None:
            o.matrix_world = M @ o.matrix_world
    bpy.context.view_layer.update()


def normalize(model, meshes, a):
    """Escala uniforme (em volta da origem = pe) para --height (altura) ou --width (maior medida horizontal).
    Preset footprint=(largura, comprimento): escala NAO uniforme da planta (o comprimento vai para o eixo Y, a altura
    acompanha a largura), para o prop vestir um obstaculo de medida dada (sarcofago 0,8 x 1,8 com o modelo 1,4:1).
    Preset tilt=graus: depois da escala, gira a frente (-Y) para cima e apoia o mais baixo no chao (aranha empinada)."""
    if not (a.height or a.width or a.footprint):
        return 1.0
    pts = model_points(meshes)
    span = lambda axis: max(p[axis] for p in pts) - min(p[axis] for p in pts)
    if a.footprint:
        w, l = a.footprint
        ex, ey = span(0), span(1)
        turn = Matrix.Rotation(math.pi / 2, 4, "Z") if ex > ey else Matrix()
        k = w / min(ex, ey)
        transform_roots(model, Matrix.Diagonal((k, l / max(ex, ey), k, 1.0)) @ turn)
        print("PLANTA %.3f x %.3f m -> %.2f x %.2f m (comprimento em Y; altura x%.4f)" % (min(ex, ey), max(ex, ey), w, l, k))
    else:
        now = span(2) if a.height else max(span(0), span(1))
        if now < 1e-6:
            die("modelo sem medida para normalizar")
        k = (a.height or a.width) / now
        transform_roots(model, Matrix.Scale(k, 4))
        print("ESCALA x%.4f (%s %.3f m -> %.3f m)" % (k, "altura" if a.height else "largura", now, a.height or a.width))
    if a.tilt:
        transform_roots(model, Matrix.Rotation(math.radians(-a.tilt), 4, "X"))
        transform_roots(model, Matrix.Translation((0, 0, -min(p.z for p in model_points(meshes)))))
        print("EMPINA %.0f graus (frente para cima, apoiado no chao)" % a.tilt)
    return k


def attach(spec, arm):
    """Prende um prop (escudo) a um osso. O lugar e dado no REPOUSO: deslocamento a partir da cabeca do osso e giro,
    em eixos do mundo, com o modelo ja normalizado. Pai = osso (o Blender usa a CAUDA do osso como origem do pai);
    o inverso do pai e o do repouso, entao em cada quadro o prop recebe o mesmo delta do osso."""
    m = ATTACH_RE.match(spec)
    if not m:
        die("--attach invalido (FBX:osso:dx,dy,dz:rx,ry,rz:tamanho): " + spec)
    path, bone, off, rot, size = m.groups()
    b = next((b for b in arm.data.bones if b.name.split(":")[-1] == bone), None) if arm else None
    if b is None:
        die("--attach: osso %s nao existe no modelo" % bone)
    objs = import_file(os.path.abspath(path))
    meshes = [o for o in objs if o.type == "MESH"]
    if not meshes:
        die("--attach sem malha: " + path)
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
    lo, hi = (Vector([f(p[i] for p in pts) for i in range(3)]) for f in (min, max))
    k = float(size) / max(hi - lo)
    M = (Matrix.Translation(arm.matrix_world @ b.head_local + Vector([float(x) for x in off.split(",")]))
         @ Euler([math.radians(float(x)) for x in rot.split(",")], "XYZ").to_matrix().to_4x4()
         @ Matrix.Scale(k, 4) @ Matrix.Translation(-(lo + hi) / 2))
    P = arm.matrix_world @ b.matrix_local @ Matrix.Translation((0, b.length, 0))  # pai no repouso
    for o in objs:
        if o.parent is None:
            mw = M @ o.matrix_world
            o.parent, o.parent_type, o.parent_bone = arm, "BONE", b.name
            o.matrix_parent_inverse = P.inverted()
            o.matrix_basis = mw
    print("ATTACH %s -> %s: tamanho %.2f m (x%.3f)" % (os.path.basename(path), b.name, float(size), k))
    return objs, meshes


def hold_pose(path, frame):
    """Rotacao de cada osso do braco relativa ao pai (delta sobre o repouso, no mundo) num quadro do clipe de segurar."""
    objs = import_file(path)
    src = next((o for o in objs if o.type == "ARMATURE" and o.animation_data and o.animation_data.action), None)
    if src is None:
        die("--hold sem armature animado: " + path)
    bpy.context.scene.frame_set(int(round(src.animation_data.action.frame_range[0] if frame is None else frame)))
    Ws, sb = src.matrix_world, src.data.bones
    d = {b.name: (Ws @ src.pose.bones[b.name].matrix).to_quaternion() @ (Ws @ b.matrix_local).to_quaternion().inverted()
         for b in sb}
    hold = {n: d[sb[n].parent.name].inverted() @ q for n, q in d.items() if ARM_RE.search(n) and sb[n].parent}
    bpy.data.batch_remove(objs)
    if not hold:
        die("--hold sem ossos de braco reconhecidos (Left/Right + Shoulder/Arm/Hand): " + path)
    print("HOLD %s: %d osso(s) de braco" % (os.path.basename(path), len(hold)))
    return hold


def retarget(src, tgt, name, hold=None, rng=None, align_arms=False):
    """Leva o clipe do armature `src` para `tgt` pelo DELTA de rotacao de cada osso em relacao ao proprio repouso
    (no mundo), e nao pela rotacao local crua. Copiar a action so funciona com repouso/roll identicos; o rig
    Mixamo do Tripo e o X Bot do Mixamo diferem (bracos subiam acima da cabeca, 2026-10-06). O quadril tambem
    leva o deslocamento, escalado pela razao das alturas do quadril. Com `hold`, os ossos do braco trocam o delta
    do clipe pelo delta do pai neste quadro x a pose relativa do clipe de segurar: pernas e tronco correm, bracos
    seguram a carga.
    Quadril: o deslocamento horizontal parte da origem no 1o quadro e, em clipe em laco, perde a deriva linear (os
    runs do ARKANA vem com root motion: o quadril anda 2,2 m e o boneco sairia da celula). A deriva retirada, na
    escala do alvo, vira action["stride_mps"] = velocidade em que o pe nao patina. Com `rng` = (ini, fim), so essa
    faixa do clipe de origem entra (janela de laco de um idle longo)."""
    sact = src.animation_data.action
    s, e = (int(round(x)) for x in sact.frame_range)
    if rng and not s <= rng[0] < rng[1] <= e:  # outro FBX com o mesmo nome (ex.: Idle do placeholder no -Teste)
        print("AVISO faixa %d-%d fora do clipe %s (%d-%d): usa o clipe inteiro" % (rng[0], rng[1], name, s, e))
        rng = None
    if rng:
        s, e = rng
    tb, sb = tgt.data.bones, src.data.bones
    # Rig em pose A (Tripo a partir de imagem em A): o delta sobre o repouso levaria o braco do A tanto quanto leva o do
    # T, e o braco entraria no corpo (~50 graus no bruto). Com align_arms, cada osso do braco primeiro gira do repouso
    # do alvo para a direcao do repouso do clipe (arco minimo) e so entao recebe o delta: a direcao do braco = a do clipe.
    # Sem a opcao, alinha sozinho se o braco do rig estiver a mais de 15 graus do repouso do clipe (rig novo em pose A:
    # no Lote 4 a inspecao a parte custava uma importacao a mais por rig com o PC sem memoria).
    align, worst = {}, 0.0
    for n in bone_order(tgt):
        if n in sb and ARM_RE.search(n):
            ut = ((tgt.matrix_world @ tb[n].tail_local) - (tgt.matrix_world @ tb[n].head_local)).normalized()
            us = ((src.matrix_world @ sb[n].tail_local) - (src.matrix_world @ sb[n].head_local)).normalized()
            align[n] = ut.rotation_difference(us)
            if re.search(r"(Arm|ForeArm)$", n):
                worst = max(worst, math.degrees(ut.angle(us)))
    if not align_arms and worst > 15.0:
        if not ALIGN_SEEN:
            print("AVISO rig em pose A: braco a %.0f graus do repouso do clipe; alinhando os bracos (--align-arms)" % worst)
        ALIGN_SEEN.add(name)
    elif not align_arms:
        align = {}
    order = bone_order(tgt)
    found = [n for n in order if n in sb]
    if len(found) < len(order) // 2:
        die("armature nao bate no clipe %s: so %d de %d osso(s) do modelo existem no clipe (faltam p.ex. %s)" % (
            name, len(found), len(order), ", ".join([n for n in order if n not in sb][:8])))
    Ws, Wt = src.matrix_world, tgt.matrix_world
    qWt_inv, Wt3_inv = Wt.to_quaternion().inverted(), Wt.inverted().to_3x3()
    root = order[0]
    hs = (Ws @ sb[root].head_local).z if root in sb else 0.0
    k = (Wt @ tb[root].head_local).z / hs if hs > 1e-6 else 1.0

    def root_xy(f):
        bpy.context.scene.frame_set(f)
        v = (Ws @ src.pose.bones[root].matrix).translation - (Ws @ sb[root].matrix_local).translation
        return Vector((v.x, v.y, 0.0))
    anchor = root_xy(s) if root in sb else Vector()
    drift = root_xy(e) - anchor if (root in sb and LOOP_RE.search(name)) else Vector()
    ad = tgt.animation_data or tgt.animation_data_create()
    ad.action = bpy.data.actions.new(name)
    for pb in tgt.pose.bones:
        pb.rotation_mode = "QUATERNION"
    for f in range(s, e + 1):
        bpy.context.scene.frame_set(f)
        pose, deltas = {}, {}
        for n in order:
            b, pb = tb[n], tgt.pose.bones[n]
            attach = pose[b.parent.name] @ b.parent.matrix_local.inverted() @ b.matrix_local if b.parent else b.matrix_local.copy()
            m = attach
            if n in sb:
                srest, spose = Ws @ sb[n].matrix_local, Ws @ src.pose.bones[n].matrix
                d = spose.to_quaternion() @ srest.to_quaternion().inverted()
                if hold and n in hold and b.parent and b.parent.name in deltas:
                    d = deltas[b.parent.name] @ hold[n]
                deltas[n] = d
                rot = qWt_inv @ d @ align.get(n, Quaternion()) @ (Wt @ b.matrix_local).to_quaternion()
                head = attach.translation
                if not b.parent:
                    off = spose.translation - srest.translation - anchor - drift * ((f - s) / max(e - s, 1))
                    head = head + Wt3_inv @ (off * k)
                m = Matrix.Translation(head) @ rot.to_matrix().to_4x4()
            pose[n] = m
            pb.matrix_basis = attach.inverted() @ m
            pb.keyframe_insert("rotation_quaternion", frame=f)
            if not b.parent:
                pb.keyframe_insert("location", frame=f)
    ad.action["stride_mps"] = drift.length * k * bpy.context.scene.render.fps / max(e - s, 1)
    if rng:
        ad.action["src_range"] = list(rng)
    return ad.action


def load_clips(a, arm, hold):
    """[(nome, action)] vindos de --anims (1 FBX = 1 clipe, retargetado) ou das actions do proprio modelo (--self)."""
    clips = [(act.name.split("|")[-1], act) for act in bpy.data.actions if action_bones(act)] if a.self_ else []
    skip = {os.path.abspath(a.model)} | ({os.path.abspath(a.hold)} if a.hold else set())
    for f in [] if a.self_ else sorted(glob.glob(os.path.join(a.anims or "", "*.fbx"))):
        if os.path.abspath(f) in skip:
            continue
        objs = import_file(f)
        srcs = [o for o in objs if o.type == "ARMATURE" and o.animation_data and o.animation_data.action]
        name = os.path.splitext(os.path.basename(f))[0]
        if srcs:
            clips.append((name, retarget(srcs[0], arm, name, hold if name.lower().startswith("carry") else None,
                                         a.clips.get(name, {}).get("range"), a.align_arms)))
        else:
            print("AVISO sem animacao: " + f)
        bpy.data.batch_remove(objs)
    if not clips:
        die("nenhum clipe: use --anims <pasta com FBX> ou --self")
    return clips


def sample_frames(act, a, name):
    s, e = act.frame_range
    span = max(e - s, 1.0)  # em clipe em laco a pose do fim == inicio: amostra [s, e)
    # ponytail: teto de quadros por clipe (folha <= max_frames*size px; 4096 e o teto seguro de textura no celular);
    # clipe longo fica com fps menor, gravado por clipe no meta.json; "frames" do preset fixa o numero do clipe
    n = a.clips.get(name, {}).get("frames") or max(1, min(a.max_frames, int(round(span / a.src_fps * a.fps))))
    return [s + i * span / n for i in range(n)]


def eval_meshes(meshes, fr):
    bpy.context.scene.frame_set(int(fr), subframe=fr - int(fr))
    dg = bpy.context.evaluated_depsgraph_get()
    return [ob.evaluated_get(dg) for ob in meshes]


def bounds(arm, meshes, clips, e, thetas, cut=()):
    """Meia largura maxima e extremos verticais na tela, pelos vertices reais, so nas direcoes renderizadas (`thetas`;
    o espelho tem a mesma medida). Ate 2026-10-10 usava o raio horizontal (vale para qualquer giro) e superestimava
    o ator que cai deitado: o ogro_fim (Dying para tras) dava 8,2 m e o pivo ia para o meio da celula, cortando o corpo."""
    import numpy as np
    half, vmin, vmax = 0.0, 0.0, 0.0
    for name, act, frames in clips:
        set_action(arm, act)
        for fr in frames:
            for ev in eval_meshes(meshes, fr):
                n = len(ev.data.vertices)
                co = np.empty(n * 3, np.float64)
                ev.data.vertices.foreach_get("co", co)
                M = np.array(ev.matrix_world)
                p = co.reshape(n, 3) @ M[:3, :3].T + M[:3, 3]
                if name in cut:  # a parte enterrada nao entra na folha (place corta no chao)
                    p = p[p[:, 2] >= 0.0]
                    if not len(p):
                        continue
                for th in thetas:  # giro em Z como em place(); tela: x = right, y = up = (0, sen e, cos e)
                    x = p[:, 0] * math.cos(th) - p[:, 1] * math.sin(th)
                    v = (p[:, 0] * math.sin(th) + p[:, 1] * math.cos(th)) * math.sin(e) + p[:, 2] * math.cos(e)
                    half, vmin, vmax = max(half, float(np.abs(x).max())), min(vmin, float(v.min())), max(vmax, float(v.max()))
    return half, vmin, vmax


def place(meshes, fr, theta, shift, keep, cut=False):
    for ev in eval_meshes(meshes, fr):
        me, M = bpy.data.meshes.new_from_object(ev), ev.matrix_world
        if cut:  # clipe que entra e sai do chao (Emerge/Burrow): o chao corta a malha e a parte enterrada some do sprite
            import bmesh
            me.transform(M)
            M = Matrix()
            bm = bmesh.new()
            bm.from_mesh(me)
            bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(0, 0, 0), plane_no=(0, 0, 1),
                                   clear_inner=True)
            bm.to_mesh(me)
            bm.free()
        cp = bpy.data.objects.new("cell", me)
        cp.matrix_world = Matrix.Translation(shift) @ Matrix.Rotation(theta, 4, "Z") @ M
        bpy.context.scene.collection.objects.link(cp)
        keep.append(cp)


def hand_anchor(arm, hands, root, theta, ppu, size, pivot_y, right, up):
    """Ponto entre as palmas na celula (px do canto inferior esquerdo) + 1 se fica na frente do corpo (mais perto da
    camera que o quadril), 0 se atras. A pose ja esta avaliada: place() acabou de fazer frame_set."""
    R, W = Matrix.Rotation(theta, 4, "Z"), arm.matrix_world
    p = R @ (sum((W @ h.head + W @ h.tail for h in hands), Vector()) / (2.0 * len(hands)))
    hip = R @ (W @ root.head)
    # de perfil (W) a mao fica quase no plano do quadril e a corrida a leva para tras e para a frente: sem folga a
    # carga piscava por cima/por baixo de um quadro para o outro. Ate 15% da altura do quadril atras ainda e frente.
    tol = 0.15 * hip.z
    return [round(size / 2.0 + p.dot(right) * ppu, 1), round(pivot_y + p.dot(up) * ppu, 1), 1 if p.y < hip.y + tol else 0]


def ink(path, px):
    """Contorno de `px` pixels na cor INK: dilata o alfa (disco) e compoe a tinta POR BAIXO do sprite (alfa reto)."""
    import numpy as np
    img = bpy.data.images.load(path)
    w, h = img.size
    buf = np.empty(w * h * 4, np.float32)
    img.pixels.foreach_get(buf)
    rgba = buf.reshape(h, w, 4)  # vista de buf: escrever aqui escreve em buf
    al = rgba[..., 3].copy()
    pad, dil = np.pad(al, px), np.zeros_like(al)
    for dy in range(-px, px + 1):
        for dx in range(-px, px + 1):
            if dx * dx + dy * dy <= px * px:
                dil = np.maximum(dil, pad[px + dy:px + dy + h, px + dx:px + dx + w])
    under = dil * (1.0 - al)
    out = al + under
    for c in range(3):
        rgba[..., c] = (rgba[..., c] * al + INK[c] * under) / np.maximum(out, 1e-6)
    rgba[..., 3] = out
    img.pixels.foreach_set(buf)
    img.save()
    bpy.data.images.remove(img)


def render(cols, rows, cell, size, path, keep, outline):
    sc = bpy.context.scene
    sc.camera.data.ortho_scale = cols * cell
    sc.render.resolution_x, sc.render.resolution_y, sc.render.filepath = cols * size, rows * size, path
    if cols * size > 4096 or rows * size > 4096:
        print("AVISO folha %dx%d px passa de 4096 (teto seguro no celular): --max-frames ou --size menor" % (cols * size, rows * size))
    bpy.ops.render.render(write_still=True)
    bpy.data.batch_remove([cp.data for cp in keep] + keep)
    keep.clear()
    if outline:
        ink(path, outline)


def main():
    sys.stdout.reconfigure(line_buffering=True)  # o log fica legivel mesmo se o .ps1 cortar no timeout
    t_ini = time.time()
    etapa = lambda s: print("ETAPA %-8s %5.0f s" % (s, time.time() - t_ini))  # onde vai o tempo (PC sem memoria)
    a = parse_args()
    out = os.path.abspath(a.out)
    os.makedirs(out, exist_ok=True)
    contact = os.path.abspath(a.contact) if a.contact else os.path.join(out, "contact.png")
    os.makedirs(os.path.dirname(contact), exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.fps = a.src_fps  # o importador FBX converte tempo -> frame com o fps da cena
    model = import_file(os.path.abspath(a.model))
    arms, meshes = [o for o in model if o.type == "ARMATURE"], [o for o in model if o.type == "MESH"]
    if len(arms) > 1 or not meshes:
        die("modelo precisa de malha(s) e no maximo 1 armature; achei %d armature(s) e %d malha(s)" % (len(arms), len(meshes)))
    arm = arms[0] if arms else None
    etapa("modelo")
    k_model = normalize(model, meshes, a)
    for spec in a.attach:  # depois de normalizar: o prop tem tamanho proprio e nao entra na altura do ator
        objs, extra = attach(spec, arm)
        model += objs
        meshes += extra
    hold =hold_pose(os.path.abspath(a.hold), a.hold_frame) if (a.hold and arm and not a.self_) else None
    # sem armature = prop estatico: 1 quadro "Static", mesma camera e escala dos personagens
    clips = [(n, act, sample_frames(act, a, n)) for n, act in load_clips(a, arm, hold)] if arm else [("Static", None, [0.0])]
    etapa("clipes")
    rows = DIRS[a.dirs][: a.dirs // 2 + 1] if (a.mirror and a.dirs > 1) else DIRS[a.dirs]
    hands = [b for b in arm.pose.bones if HAND_RE.search(b.name)] if arm else []
    root = arm.pose.bones[bone_order(arm)[0]] if arm else None
    if arm and not hands:
        print("AVISO sem ossos LeftHand/RightHand: meta.json sai sem 'hands'")

    e = math.radians(a.elev)
    fwd, up, right = Vector((0, math.cos(e), -math.sin(e))), Vector((0, math.sin(e), math.cos(e))), Vector((1, 0, 0))
    half, vmin, vmax = bounds(arm, meshes, clips, e, [math.radians(a.yaw - d * 360.0 / a.dirs) for d in range(len(rows))],
                              a.ground_cut)
    etapa("limites")
    extent = max(2 * half, vmax - vmin) * 1.1
    ppu = a.scale or a.size / extent
    cell = a.size / ppu  # metros por celula
    if extent > cell:
        print("AVISO recorte: personagem ocupa %.2f m e a celula so %.2f m (--scale menor ou --size maior)" % (extent, cell))
    vc = (vmin + vmax) / 2.0  # centro vertical do personagem na tela; o pivo (origem) fica abaixo dele
    pivot_y = a.size / 2.0 - vc * ppu

    sc.render.engine, sc.render.film_transparent = "BLENDER_WORKBENCH", True
    sc.render.image_settings.file_format, sc.render.image_settings.color_mode = "PNG", "RGBA"
    sc.view_settings.view_transform, sc.display.render_aa = "Standard", a.aa
    sh = sc.display.shading
    sh.light, studio = LIGHT[a.light]
    if studio:
        sh.studio_light = studio
    sh.color_type, sh.show_object_outline, sh.show_cavity, sh.show_shadows = "TEXTURE", True, True, a.shadow
    cam = bpy.data.objects.new("SpriteCam", bpy.data.cameras.new("SpriteCam"))
    cam.data.type, cam.data.sensor_fit, cam.data.clip_end = "ORTHO", "HORIZONTAL", 1000.0
    cam.location, cam.rotation_euler = -fwd * 50.0, (math.pi / 2 - e, 0.0, 0.0)
    sc.collection.objects.link(cam)
    sc.camera = cam
    for o in model:
        o.hide_render = True

    meta = {"size": a.size, "fps": a.fps, "dirs": a.dirs, "rows": len(rows), "dir_order": rows, "mirror": len(rows) < a.dirs,
            "row0": "top", "elev": a.elev, "yaw": a.yaw, "ppu": round(ppu, 2), "pivot_px": [a.size / 2.0, round(pivot_y, 1)],
            "pivot": [0.5, round(pivot_y / a.size, 4)], "light": a.light, "outline_px": a.outline, "src_fps": a.src_fps,
            "preset": a.preset or "", "model_scale": round(k_model, 4), "hold": os.path.basename(a.hold) if hold else "",
            "attach": [os.path.basename(s) for s in a.attach], "align_arms": bool(a.align_arms or ALIGN_SEEN), "clips": []}  # listas planas: JsonUtility nao le lista de listas
    keep = []
    for name, act, frames in clips:
        t0 = time.time()
        set_action(arm, act)
        anchors = []
        for d in range(len(rows)):
            theta = math.radians(a.yaw - d * 360.0 / a.dirs)
            for i, fr in enumerate(frames):
                shift = right * ((i + 0.5 - len(frames) / 2.0) * cell) + up * ((len(rows) / 2.0 - d - 0.5) * cell - vc)
                place(meshes, fr, theta, shift, keep, name in a.ground_cut)
                if hands:
                    anchors += hand_anchor(arm, hands, root, theta, ppu, a.size, pivot_y, right, up)
        render(len(frames), len(rows), cell, a.size, os.path.join(out, name + ".png"), keep, a.outline)
        loop = bool(LOOP_RE.search(name))
        span = act.frame_range[1] - act.frame_range[0] if act else 0.0
        dur = max(span, 1.0) / a.src_fps
        clip = {"name": name, "file": name + ".png", "frames": len(frames), "loop": loop,
                "fps": round(len(frames) / dur, 3), "duration_s": round(dur, 3), "src_frames": int(round(span))}
        if act and act.get("src_range"):
            clip["src_range"] = [int(x) for x in act["src_range"]]  # rastreio: faixa do clipe de origem
        if name in a.ground_cut:
            clip["ground_cut"] = True  # a parte abaixo do chao ja foi cortada no render (a View nao precisa recortar)
        if hands:
            clip["hands"] = anchors  # (row * frames + frame) * 3 -> x, y, frente
        stride = act.get("stride_mps", 0.0) if act else 0.0
        if loop and stride > 0.3:  # ponytail: so locomocao com root motion; clipe "in place" de origem fica sem
            clip["stride_mps"] = round(stride, 3)
        meta["clips"].append(clip)
        print("CLIP %-20s frames=%2d linhas=%d folha=%dx%d px loop=%s passada=%.2fm/s t=%.1fs" % (
            name, len(frames), len(rows), len(frames) * a.size, len(rows) * a.size, loop, stride, time.time() - t0))
    for j, (name, act, frames) in enumerate(clips):  # contact.png: 1o frame de cada clipe, direcao S
        set_action(arm, act)
        place(meshes, frames[0], math.radians(a.yaw), right * ((j + 0.5 - len(clips) / 2.0) * cell) - up * vc, keep,
              name in a.ground_cut)
    render(len(clips), 1, cell, a.size, contact, keep, a.outline)
    with open(os.path.join(out, "meta.json"), "w") as f:
        json.dump(meta, f, indent=1)
    print("SPRITES_OK %s clipes=%d linhas=%d ppu=%.1f pivo=(%d,%d)px" % (out, len(clips), len(rows), ppu, a.size / 2, round(pivot_y)))


main()
