"""dragao_blender.py - Blender headless: o dragao do Tripo (rig "Outro": 43 ossos bone_0..42, sem mandibula, pesos
quebrados) vira um rig nomeado com mandibula, pesos refeitos e 7 clipes por keyframe, salvo em .blend para o
render_sprites.py --self (RENDER_SPRITES.md sec. 10).

Uso (Blender 5.2):
  blender.exe -b --factory-startup --python-exit-code 1 --python dragao_blender.py -- --fbx <dragao.fbx> --out <pasta>
Saida: <pasta>/dragao.blend (Idle, Walk, Breath, Fly, Land, Roar) e <pasta>/dragao_fim.blend (Dying).

O que o script faz, em ordem:
  1. renomeia os ossos pelo papel (tabela NAMES, lida na inspecao: o dragao olha +X no FBX, +Y e o lado esquerdo);
  2. cria o osso `jaw` (filho de `head`) na dobradica da boca;
  3. refaz os pesos: no FBX ~1 065 dos 3 813 vertices nao tinham peso nenhum (ficavam parados) e o grupo bone_42 (pata
     traseira) pegava o corpo inteiro. Peso novo = inverso da distancia^4 aos 2 ossos mais proximos, com os ossos de um
     lado so no seu lado; a mandibula pega a regiao abaixo da linha dos dentes de cima;
  4. gira o dragao para olhar -Y (como os rigs Mixamo; o render nao precisa de --yaw) e aplica as transformacoes;
  5. escreve os clipes: pose = giro de cada osso em eixos do MUNDO (X = esquerda do dragao, Y = tras, Z = cima),
     relativo ao pai, a mesma convencao do anim_blender.py. Chaves nos quadros que o render amostra.
"""
import argparse, math, os, sys
import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector

NAMES = {0: "root", 1: "spine_0", 2: "spine_1", 3: "neck_0", 4: "neck_1", 5: "neck_2", 6: "head", 7: "throat_0",
         8: "throat_1", 9: "horn_L_0", 10: "horn_L_1", 11: "horn_R_0", 12: "horn_R_1", 13: "leg_FL_0", 14: "leg_FL_1",
         15: "leg_FL_2", 16: "leg_FL_3", 17: "leg_FR_0", 18: "leg_FR_1", 19: "leg_FR_2", 20: "leg_FR_3", 21: "wing_L_0",
         22: "wing_L_1", 23: "wing_L_2", 24: "wing_L_3", 25: "wing_L_4", 26: "wing_R_0", 27: "wing_R_1", 28: "wing_R_2",
         29: "wing_R_3", 30: "wing_R_4", 31: "wing_R_5", 32: "wing_R_6", 33: "pelvis", 34: "tail_0", 35: "tail_1",
         36: "tail_2", 37: "tail_3", 38: "leg_HL_0", 39: "leg_HL_1", 40: "leg_HL_2", 41: "leg_HR_0", 42: "leg_HR_1"}
# mandibula no FBX (dragao olhando +X): dobradica atras do canto da boca, ponta no queixo; regiao = abaixo da linha
# dos dentes de cima (z = 0,47 em x = 0,38 ate 0,445 em x = 0,47), acima da garganta (z > 0,36), entrando de 0,37 a 0,40
JAW_HINGE, JAW_TIP = (0.37, 0.0, 0.50), (0.47, 0.0, 0.44)


def die(msg):
    print("ERRO " + msg)
    sys.exit(1)


def select_only(objs, active):
    for o in bpy.context.view_layer.objects:
        o.select_set(o in objs)
    bpy.context.view_layer.objects.active = active


def build(fbx):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 30
    bpy.ops.import_scene.fbx(filepath=fbx, use_anim=False)
    arm = next((o for o in bpy.data.objects if o.type == "ARMATURE"), None)
    me = next((o for o in bpy.data.objects if o.type == "MESH"), None)
    if arm is None or me is None or len(arm.data.bones) != 43:
        die("esperava 1 armature com 43 ossos e 1 malha (rig Outro do Tripo)")
    # 1-2. nomes e mandibula (modo de edicao)
    select_only([arm], arm)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm.data.edit_bones
    for i, n in NAMES.items():
        eb["bone_%d" % i].name = n
    W = arm.matrix_world.inverted()
    jaw = eb.new("jaw")
    jaw.head, jaw.tail, jaw.parent = W @ Vector(JAW_HINGE), W @ Vector(JAW_TIP), eb["head"]
    bpy.ops.object.mode_set(mode="OBJECT")
    for g in me.vertex_groups:  # o grupo segue o nome do osso
        if g.name.startswith("bone_") and int(g.name[5:]) in NAMES:
            g.name = NAMES[int(g.name[5:])]
    # 3. pesos
    co = np.empty(len(me.data.vertices) * 3)
    me.data.vertices.foreach_get("co", co)
    M = np.array(me.matrix_world)
    p = co.reshape(-1, 3) @ M[:3, :3].T + M[:3, 3]
    Wa = arm.matrix_world
    bones = [b for b in arm.data.bones if b.name not in ("root", "jaw")]
    d = np.empty((len(p), len(bones)))
    for j, b in enumerate(bones):
        h, t = np.array(Wa @ b.head_local), np.array(Wa @ b.tail_local)
        s = t - h
        u = np.clip(((p - h) @ s) / max(s @ s, 1e-9), 0.0, 1.0)
        d[:, j] = np.linalg.norm(p - (h + u[:, None] * s), axis=1)
        side = 1 if any(k in b.name for k in ("_L_", "FL", "HL")) else -1 if any(k in b.name for k in ("_R_", "FR", "HR")) else 0
        if side:  # osso de um lado so pega o seu lado (+Y = esquerda no FBX)
            d[side * p[:, 1] < -0.02, j] = 1e9
    near = np.argsort(d, axis=1)[:, :2]
    w = 1.0 / (np.take_along_axis(d, near, 1) ** 4 + 1e-12)
    w /= w.sum(1, keepdims=True)
    x, z = p[:, 0], p[:, 2]
    line = 0.47 + (x - 0.38) * (0.445 - 0.47) / (0.47 - 0.38)
    jw = np.clip((x - 0.37) / 0.03, 0.0, 1.0) * ((z < line) & (z > 0.36) & (np.abs(p[:, 1]) < 0.09))
    for g in list(me.vertex_groups):
        me.vertex_groups.remove(g)
    groups = {b.name: me.vertex_groups.new(name=b.name) for b in bones}
    gj = me.vertex_groups.new(name="jaw")
    for i in range(len(p)):
        k = 1.0 - jw[i]
        for c in range(2):
            if w[i, c] * k > 1e-3:
                groups[bones[near[i, c]].name].add([i], float(w[i, c] * k), "REPLACE")
        if jw[i] > 0:
            gj.add([i], float(jw[i]), "REPLACE")
    print("PESOS %d vertices, %d ossos, mandibula %d vertices" % (len(p), len(bones) + 1, int((jw > 0).sum())))
    # 4. olhar -Y: giro de -90 graus em Z (frente +X -> -Y; esquerda +Y -> +X) e transformacoes aplicadas
    for o in (arm, me):
        if o.parent is None:
            o.matrix_world = Matrix.Rotation(-math.pi / 2, 4, "Z") @ o.matrix_world
    select_only([arm, me], arm)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for img in bpy.data.images:  # so o basecolor importa no Workbench; os outros mapas podem nem ter sido extraidos
        if img.filepath and not img.packed_file and os.path.exists(bpy.path.abspath(img.filepath)):
            img.pack()
    return arm, me


# ---- poses ------------------------------------------------------------------------------------------------------
AX = {"X": Vector((1, 0, 0)), "Y": Vector((0, 1, 0)), "Z": Vector((0, 0, 1))}


def Q(*pairs):
    """giro em eixos do mundo (X = esquerda, Y = tras, Z = cima), aplicados na ordem; +X abaixa o que aponta a frente."""
    q = Quaternion()
    for ax, g in pairs:
        q = Quaternion(AX[ax], math.radians(g)) @ q
    return q


def apply(arm, spec, loc=(0.0, 0.0, 0.0)):
    """spec: osso -> Quaternion (relativo ao pai, eixos do mundo do repouso). Escreve matrix_basis de cada osso."""
    D, pose_m = {}, {}

    def walk(b):
        D[b.name] = (D[b.parent.name] if b.parent else Quaternion()) @ spec.get(b.name, Quaternion())
        pb = arm.pose.bones[b.name]
        attach = pose_m[b.parent.name] @ b.parent.matrix_local.inverted() @ b.matrix_local if b.parent else b.matrix_local.copy()
        head = attach.translation + (Vector(loc) if not b.parent else Vector())
        m = Matrix.Translation(head) @ (D[b.name] @ b.matrix_local.to_quaternion()).to_matrix().to_4x4()
        pose_m[b.name] = m
        pb.matrix_basis = attach.inverted() @ m
        for c in b.children:
            walk(c)
    for b in arm.data.bones:
        if b.parent is None:
            walk(b)


def chain(names, *pairs):
    """o mesmo giro repartido numa cadeia (pescoco, cauda): cada osso leva 1/n."""
    n = len(names)
    return {nm: Q(*[(ax, g / n) for ax, g in pairs]) for nm in names}


NECK = ("neck_0", "neck_1", "neck_2")
TAIL = ("tail_0", "tail_1", "tail_2", "tail_3")


def P(neck=(), head=(), jaw=0.0, spine=(), wings=0.0, wing_out=0.0, tail=(), legs=None, loc=(0, 0, 0)):
    """pose do dragao: neck/head/spine/tail = giros (eixo, graus); jaw = abre (graus); wings = abre as asas para o
    lado (graus em Y: 0 = dobradas no alto como no modelo, 90 = na horizontal); wing_out = abre o "cotovelo"."""
    s = {}
    s.update(chain(NECK, *neck))
    if head:
        s["head"] = Q(*head)
    if jaw:
        s["jaw"] = Q(("X", jaw))
    if spine:
        s["spine_1"] = Q(*spine)
    s.update(chain(TAIL, *tail))
    if wings:
        s["wing_L_0"], s["wing_R_0"] = Q(("Y", wings)), Q(("Y", -wings))
    if wing_out:
        s["wing_L_1"], s["wing_R_1"] = Q(("Y", wing_out)), Q(("Y", -wing_out))
    s.update(legs or {})
    return s, loc


def walk_legs(ph, amp=16.0, knee=18.0):
    """passo diagonal: dianteira esquerda com traseira direita; -X = a perna vai a frente."""
    a, b = math.sin(ph), math.sin(ph + math.pi)
    lift = lambda v: knee * max(0.0, math.cos(v))  # dobra o joelho na volta da perna
    return {"leg_FL_1": Q(("X", -amp * a)), "leg_HR_0": Q(("X", -amp * a)),
            "leg_FR_1": Q(("X", -amp * b)), "leg_HL_0": Q(("X", -amp * b)),
            "leg_FL_2": Q(("X", lift(ph))), "leg_FR_2": Q(("X", lift(ph + math.pi))),
            "leg_HL_1": Q(("X", -lift(ph + math.pi))), "leg_HR_1": Q(("X", -lift(ph)))}


IDLE = P()
TUCK = {"leg_FL_1": Q(("X", 35)), "leg_FR_1": Q(("X", 35)), "leg_HL_0": Q(("X", 40)), "leg_HR_0": Q(("X", 40))}


def clips():
    """nome -> (vao em quadros a 30 fps, quadros que o render amostra, [poses nos quadros amostrados (+ a do fim)])."""
    c = {}
    # respira (laco 1 s, 4 quadros)
    c["Idle"] = (30, [P(spine=(("X", -1.5 * math.sin(i * math.pi / 2)),), neck=(("X", -3 * math.sin(i * math.pi / 2)),),
                        head=(("X", 2 * math.sin(i * math.pi / 2)),), wings=4 * math.sin(i * math.pi / 2),
                        tail=(("Z", 6 * math.sin(i * math.pi / 2)),)) for i in range(5)])
    # anda devagar (laco 1,2 s, 7 quadros)
    c["Walk"] = (36, [P(legs=walk_legs(2 * math.pi * i / 7), spine=(("Y", 3 * math.sin(2 * math.pi * i / 7)),),
                        neck=(("Z", 5 * math.sin(2 * math.pi * i / 7 + 0.6)),), wings=3 * math.sin(4 * math.pi * i / 7),
                        tail=(("Z", -10 * math.sin(2 * math.pi * i / 7 - 0.8)),), loc=(0, 0, 0.004 * math.cos(4 * math.pi * i / 7)))
                      for i in range(8)])
    # sopro: recua e inspira (1-2) -> estica o pescoco a frente com o focinho erguido e abre a boca no quadro 4 de 7 (0,6)
    # -> segura -> volta. Com a cabeca para baixo (1a versao) a boca e a garganta acesa sumiam da camera de 60 graus
    c["Breath"] = (34, [IDLE,
                        P(neck=(("X", -16),), head=(("X", -8),), spine=(("X", -5),), wings=10, tail=(("Z", 4),)),
                        P(neck=(("X", -24),), head=(("X", -14),), spine=(("X", -8),), wings=14, jaw=6),
                        P(neck=(("X", -6),), head=(("X", -26),), spine=(("X", -3),), wings=8, jaw=20),
                        P(neck=(("X", 6),), head=(("X", -38),), spine=(("X", 2),), wings=6, jaw=36),
                        P(neck=(("X", 8),), head=(("X", -38),), spine=(("X", 3),), wings=6, jaw=38),
                        P(neck=(("X", 5),), head=(("X", 1),), wings=3, jaw=10),
                        IDLE])
    # voo: asas abertas em V batendo (laco 0,6 s, 6 quadros), pernas recolhidas, pescoco a frente. As asas sobem de 33 a
    # 77 graus (abaixo da horizontal viravam uma tenda vista de frente) e os dedos de tras abrem no plano da asa (X +30 no
    # repouso = para fora no voo; apontando para tras eles liam como varetas)
    fan = {"wing_L_2": Q(("X", 30)), "wing_R_2": Q(("X", 30))}
    fly = lambda i: P(wings=55 + 22 * math.sin(2 * math.pi * i / 6), wing_out=15 + 10 * math.sin(2 * math.pi * i / 6 - 0.8),
                      neck=(("X", 18),), head=(("X", -10),), spine=(("X", 3 * math.sin(2 * math.pi * i / 6)),),
                      tail=(("X", -10),), legs=dict(TUCK, **fan))
    c["Fly"] = (18, [fly(i) for i in range(7)])
    # pouso: asas descem e fecham, o corpo afunda e a cabeca bate no chao (janela de dano); segura no ultimo quadro
    c["Land"] = (24, [fly(0),
                      P(wings=70, wing_out=10, neck=(("X", 20),), legs=TUCK),
                      P(wings=30, neck=(("X", 34),), head=(("X", 16),), spine=(("X", 6),), jaw=10),
                      P(wings=10, neck=(("X", 46),), head=(("X", 24),), spine=(("X", 8),), jaw=14, tail=(("X", 8),)),
                      P(wings=6, neck=(("X", 48),), head=(("X", 26),), spine=(("X", 8),), jaw=8, tail=(("X", 8),)),
                      P(wings=6, neck=(("X", 47),), head=(("X", 25),), spine=(("X", 8),), jaw=10, tail=(("X", 8),)),
                      P(wings=6, neck=(("X", 47),), head=(("X", 25),), spine=(("X", 8),), jaw=10, tail=(("X", 8),))])
    # rugido: empina o pescoco, cabeca para o alto, boca aberta, asas meio abertas
    c["Roar"] = (36, [IDLE,
                      P(neck=(("X", 10),), head=(("X", 8),), spine=(("X", 4),), wings=6),
                      P(neck=(("X", -22),), head=(("X", -24),), spine=(("X", -8),), wings=30, jaw=26, tail=(("X", -10),)),
                      P(neck=(("X", -30),), head=(("X", -32),), spine=(("X", -10),), wings=42, jaw=40, tail=(("X", -14),)),
                      P(neck=(("X", -30), ("Z", 6)), head=(("X", -32),), spine=(("X", -10),), wings=44, jaw=40, tail=(("X", -14),)),
                      P(neck=(("X", -26), ("Z", -6)), head=(("X", -28),), spine=(("X", -9),), wings=40, jaw=36, tail=(("X", -12),)),
                      P(neck=(("X", -8),), head=(("X", -8),), spine=(("X", -3),), wings=14, jaw=10),
                      IDLE])
    # morte (dragao_fim): cambaleia rugindo -> as pernas abrem e o corpo desce de barriga -> pescoco e asas caem no chao
    splay = lambda k: {"leg_FL_0": Q(("Y", 45 * k)), "leg_FR_0": Q(("Y", -45 * k)), "leg_HL_0": Q(("Y", 40 * k), ("X", 20 * k)),
                       "leg_HR_0": Q(("Y", -40 * k), ("X", 20 * k))}
    c["Dying"] = (45, [IDLE,
                       P(neck=(("X", -20),), head=(("X", -24),), wings=30, jaw=30, spine=(("X", -6),)),
                       P(neck=(("X", -6), ("Z", 10)), head=(("X", -6),), wings=40, jaw=20, legs=splay(0.3), loc=(0, 0, -0.02)),
                       P(neck=(("X", 30), ("Z", 14)), head=(("X", 12),), wings=70, jaw=12, legs=splay(0.7), loc=(0, 0, -0.06),
                         tail=(("Z", 10),)),
                       P(neck=(("X", 52), ("Z", 16)), head=(("X", 22), ("Y", 10)), wings=95, wing_out=10, jaw=16, legs=splay(1.0),
                         loc=(0, 0, -0.09), tail=(("Z", 14), ("X", -6))),
                       P(neck=(("X", 56), ("Z", 16)), head=(("X", 24), ("Y", 12)), wings=100, wing_out=12, jaw=18,
                         legs=splay(1.05), loc=(0, 0, -0.1), tail=(("Z", 14), ("X", -8))),
                       P(neck=(("X", 56), ("Z", 16)), head=(("X", 24), ("Y", 12)), wings=100, wing_out=12, jaw=18,
                         legs=splay(1.05), loc=(0, 0, -0.1), tail=(("Z", 14), ("X", -8))),
                       P(neck=(("X", 56), ("Z", 16)), head=(("X", 24), ("Y", 12)), wings=100, wing_out=12, jaw=18,
                         legs=splay(1.05), loc=(0, 0, -0.1), tail=(("Z", 14), ("X", -8)))])
    return c


def write_clips(arm, which):
    ad = arm.animation_data or arm.animation_data_create()
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    for name, (span, poses) in clips().items():
        if name not in which:
            continue
        act = bpy.data.actions.new(name)
        ad.action = act
        if act.slots:
            ad.action_slot = act.slots[0]
        n = len(poses) - 1  # a ultima pose fecha o vao (laco: = a primeira)
        for i, (spec, loc) in enumerate(poses):
            f = i * span / n
            apply(arm, spec, loc)
            for pb in arm.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f)
                pb.keyframe_insert("location", frame=f)
        act.use_fake_user = True
        print("CLIPE %-7s vao %d quadros (%.2f s), %d poses" % (name, span, span / 30.0, len(poses)))
    ad.action = None
    for pb in arm.pose.bones:
        pb.matrix_basis = Matrix()


def main():
    a = argparse.ArgumentParser()
    a.add_argument("--fbx", required=True)
    a.add_argument("--out", required=True)
    a = a.parse_args(sys.argv[sys.argv.index("--") + 1:])
    os.makedirs(a.out, exist_ok=True)
    for blend, which in (("dragao.blend", ("Idle", "Walk", "Breath", "Fly", "Land", "Roar")), ("dragao_fim.blend", ("Dying",))):
        arm, me = build(os.path.abspath(a.fbx))
        write_clips(arm, which)
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.abspath(a.out), blend))
        print("DRAGAO_OK", blend, [x.name for x in bpy.data.actions])


main()
