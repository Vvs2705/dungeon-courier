"""anim_blender.py - Blender headless: clipes que o Mixamo nao tem, animados por keyframe sobre o esqueleto Mixamo.

Uso (Blender 5.2):
  blender.exe -b --factory-startup --python-exit-code 1 --python anim_blender.py --
      --rig <qualquer clipe Mixamo Without Skin (X Bot)>.fbx --out <pasta> [--only Hold,Throw]
Saida: <pasta>/<Clipe>.fbx (so o armature, 1 take, 30 fps) para o render_sprites.py (--anims / --hold).

Como a pose e escrita (POSES abaixo): o personagem olha -Y, +X = esquerda dele, +Z = cima (repouso em T do X Bot).
  ("rot", (eixo, graus), ...)    rotacao relativa ao pai, em eixos do mundo do repouso, aplicadas na ordem
  ("aim", ref, (x, y, z), tw)    o osso aponta para (x, y, z) no referencial do osso `ref` ("" = mundo),
                                 com `tw` graus de giro no proprio eixo (palma da mao); filhos sem spec seguem o pai
  "loc": (x, y, z)               deslocamento do quadril em m (escala do X Bot, 1,8 m)
Cada clipe = poses-chave em quadros a 30 fps; entre elas, slerp por osso com suavizacao (smoothstep, ou "out" para
golpe rapido). O delta de cada osso sobre o repouso no mundo e o mesmo que o render_sprites.py retargeta, entao o
clipe vale para qualquer rig Mixamo (Tripo) sem copiar rotacao local crua.
"""
import argparse, math, os, sys
import bpy
from mathutils import Matrix, Quaternion, Vector

FPS = 30


def R(*pairs):
    return ("rot",) + pairs


def A(ref, v, tw=0.0):
    return ("aim", ref, v, tw)


def mirror(spec):
    """Lado esquerdo -> direito: espelho no plano YZ (x troca de sinal; giro em Y e Z e o twist trocam de sinal)."""
    out = {}
    for n, s in spec.items():
        m = n.replace("Left", "#").replace("Right", "Left").replace("#", "Right")
        if s[0] == "rot":
            out[m] = ("rot",) + tuple((ax, -g if ax in "YZ" else g) for ax, g in s[1:])
        else:
            out[m] = ("aim", s[1], (-s[2][0], s[2][1], s[2][2]), -s[3])
    return out


def both(left):
    return dict(left, **mirror(left))


# ---- pecas de pose ----------------------------------------------------------------------------------------------
FEET = {"LeftFoot": A("", None), "RightFoot": A("", None)}  # None = direcao de repouso no mundo: pe plano no chao
ARMS_DOWN = both({"LeftArm": A("Spine2", (0.25, -0.05, -1)), "LeftForeArm": A("Spine2", (0.12, -0.35, -1)),
                  "LeftHand": A("Spine2", (0.1, -0.4, -1))})
# segurar a caixa a frente do peito: cotovelos dobrados, antebracos para a frente, palmas para dentro (tw -90 / +90)
HOLD_ARMS = both({"LeftArm": A("Spine2", (0.32, -0.5, -0.8)), "LeftForeArm": A("Spine2", (-0.08, -1, 0.12), -90),
                  "LeftHand": A("Spine2", (-0.15, -1, 0.05), -90)})
# guarda: escudo no antebraco esquerdo, atravessado a frente do corpo; braco direito solto
GUARD_ARMS = dict({"LeftArm": A("Spine2", (0.45, -0.6, -0.65)), "LeftForeArm": A("Spine2", (-0.8, -0.6, 0.05), -90),
                   "LeftHand": A("Spine2", (-0.8, -0.6, 0.05), -90)},
                  **mirror({"LeftArm": A("Spine2", (0.3, 0.05, -1)), "LeftForeArm": A("Spine2", (0.15, -0.3, -1)),
                            "LeftHand": A("Spine2", (0.12, -0.35, -1))}))


def legs(lt, ls, rt, rs):
    """coxa e canela (direcao no referencial do quadril) das pernas esquerda e direita."""
    return dict(FEET, LeftUpLeg=A("Hips", lt), LeftLeg=A("Hips", ls), RightUpLeg=A("Hips", rt), RightLeg=A("Hips", rs))


STAND = legs((0.06, -0.02, -1), (0.06, 0.02, -1), (-0.06, -0.02, -1), (-0.06, 0.02, -1))
KNEES = legs((0.08, -0.25, -0.97), (0.06, 0.15, -1), (-0.08, -0.25, -0.97), (-0.06, 0.15, -1))  # joelhos dobrados


def pose(*parts, **kw):
    d = {}
    for p in parts:
        d.update(p)
    d.update({k: v for k, v in kw.items() if k != "loc"})
    return d, kw.get("loc", (0, 0, 0))


READY = pose(ARMS_DOWN, KNEES, loc=(0, 0, -0.04))
GUARD = pose(GUARD_ARMS, KNEES, Spine=R(("X", 4)), loc=(0, 0, -0.04))
GUARD_IN = pose(GUARD_ARMS, KNEES, Spine=R(("X", 2)), Spine1=R(("X", -2)), loc=(0, 0, -0.025))  # respiracao

# clipe -> [(quadro, pose, suavizacao ate esta chave)]
CLIPS = {
    # pose parada de segurar (fonte do --hold): 10 quadros iguais
    "Hold": [(0, pose(HOLD_ARMS, STAND, Spine1=R(("X", -4))), "io"), (9, pose(HOLD_ARMS, STAND, Spine1=R(("X", -4))), "io")],
    # arremesso curto por cima, mao direita: pronto -> carrega atras -> solta (impacto) -> acompanha -> volta
    "Throw": [
        (0, READY, "io"),
        (6, pose(KNEES, Spine=R(("Z", -22)), Spine1=R(("X", -6), ("Z", -10)), Neck=R(("Z", 18)),
                 LeftArm=A("Spine2", (0.4, -0.9, 0.05)), LeftForeArm=A("Spine2", (0.25, -0.95, 0.15), -60),
                 RightArm=A("Spine2", (-0.65, 0.5, 0.45)), RightForeArm=A("Spine2", (-0.15, 0.25, 1), 60),
                 RightHand=A("Spine2", (-0.1, 0.45, 1), 60), loc=(0, 0.04, -0.05)), "io"),
        (10, pose(legs((0.1, -0.45, -0.9), (0.06, -0.05, -1), (-0.08, 0.3, -0.95), (-0.06, 0.55, -0.85)),
                  Spine=R(("Z", 18), ("X", 10)), Spine1=R(("X", 8), ("Z", 8)), Neck=R(("Z", -14), ("X", -10)),
                  LeftArm=A("Spine2", (0.5, 0.3, -0.8)), LeftForeArm=A("Spine2", (0.3, -0.3, -0.9)),
                  RightArm=A("Spine2", (-0.25, -0.95, 0.3)), RightForeArm=A("Spine2", (-0.05, -1, 0.25), 30),
                  RightHand=A("Spine2", (0, -1, 0.1), 30), loc=(0, -0.06, -0.06)), "out"),
        (14, pose(legs((0.1, -0.45, -0.9), (0.06, -0.05, -1), (-0.08, 0.3, -0.95), (-0.06, 0.55, -0.85)),
                  Spine=R(("Z", 24), ("X", 16)), Spine1=R(("X", 10), ("Z", 8)), Neck=R(("Z", -18), ("X", -14)),
                  LeftArm=A("Spine2", (0.5, 0.35, -0.8)), LeftForeArm=A("Spine2", (0.3, -0.2, -0.95)),
                  RightArm=A("Spine2", (0.05, -0.8, -0.6)), RightForeArm=A("Spine2", (0.25, -0.6, -0.75), 20),
                  RightHand=A("Spine2", (0.3, -0.5, -0.8), 20), loc=(0, -0.07, -0.07)), "io"),
        (20, READY, "io"),
    ],
    # avanco curto: agacha -> projeta o corpo inclinado com os bracos para tras -> freia -> pronto
    "Dash": [
        (0, READY, "io"),
        # bracos em eixos do MUNDO (ref ""): com o tronco inclinado 46 graus, "para tras" no peito virava "para cima"
        (3, pose(KNEES, Spine=R(("X", 12)), Spine1=R(("X", 8)), loc=(0, 0, -0.1),
                 **both({"LeftArm": A("", (0.3, 0.35, -0.9)), "LeftForeArm": A("", (0.2, 0.3, -0.95))})), "io"),
        (7, pose(legs((0.08, -0.75, -0.65), (0.06, 0.05, -1), (-0.08, 0.6, -0.8), (-0.06, 0.85, -0.5)),
                 Spine=R(("X", 22)), Spine1=R(("X", 16)), Spine2=R(("X", 8)), Neck=R(("X", -25)), Head=R(("X", -10)),
                 loc=(0, -0.08, -0.14),
                 **both({"LeftArm": A("", (0.25, 0.85, -0.45)), "LeftForeArm": A("", (0.2, 0.95, -0.25), -30),
                         "LeftHand": A("", (0.2, 0.95, -0.2), -30)})), "out"),
        (11, pose(legs((0.08, -0.6, -0.8), (0.06, 0.1, -1), (-0.08, 0.45, -0.9), (-0.06, 0.7, -0.7)),
                  Spine=R(("X", 14)), Spine1=R(("X", 8)), Neck=R(("X", -12)), loc=(0, -0.04, -0.1),
                  **both({"LeftArm": A("Spine2", (0.45, -0.55, -0.7)), "LeftForeArm": A("Spine2", (0.3, -0.85, -0.3))})), "io"),
        (15, READY, "io"),
    ],
    # recuo de dano: tranco para tras com bracos soltos, volta
    "Hit": [
        (0, READY, "io"),
        (3, pose(legs((0.08, 0.1, -1), (0.06, 0.35, -0.95), (-0.08, -0.15, -1), (-0.06, 0.2, -1)),
                 Spine=R(("X", -14)), Spine1=R(("X", -10), ("Z", 6)), Neck=R(("X", -18)), Head=R(("X", -10)),
                 loc=(0, 0.07, -0.06),
                 **both({"LeftArm": A("Spine2", (0.75, -0.45, -0.35)), "LeftForeArm": A("Spine2", (0.6, -0.75, 0.1), -40)})), "out"),
        (8, pose(KNEES, Spine=R(("X", -5)), Spine1=R(("X", -4)), Neck=R(("X", -6)), loc=(0, 0.04, -0.06),
                 **both({"LeftArm": A("Spine2", (0.45, -0.25, -0.85)), "LeftForeArm": A("Spine2", (0.3, -0.55, -0.75))})), "io"),
        (14, READY, "io"),
    ],
    # guarda parado com o escudo a frente, respirando (laco: ultima chave = primeira)
    "GuardIdle": [(0, GUARD, "io"), (30, GUARD_IN, "io"), (59, GUARD, "io")],
    # golpe de escudo: pronto -> recolhe o escudo e gira o ombro esquerdo para tras (antecipacao longa) ->
    # empurra o escudo a frente com passo (impacto, chave 17) -> segura -> pronto
    "Bash": [
        (0, GUARD, "io"),
        (12, pose(legs((0.08, -0.15, -1), (0.06, 0.3, -0.95), (-0.1, -0.3, -0.95), (-0.06, 0.2, -1)),
                  Spine=R(("Z", 22), ("X", -4)), Spine1=R(("Z", 12), ("X", -6)), Neck=R(("Z", -26)),
                  # antebraco na horizontal: inclinado, ele girava o escudo no proprio plano
                  LeftArm=A("Spine2", (0.6, 0.25, -0.75)), LeftForeArm=A("Spine2", (-0.82, -0.55, 0.0), -90),
                  LeftHand=A("Spine2", (-0.82, -0.55, 0.0), -90),
                  **mirror({"LeftArm": A("Spine2", (0.35, -0.2, -0.9)), "LeftForeArm": A("Spine2", (0.2, -0.6, -0.8))}),
                  loc=(0, 0.06, -0.1)), "io"),
        (17, pose(legs((0.1, -0.65, -0.75), (0.06, -0.05, -1), (-0.08, 0.5, -0.85), (-0.06, 0.75, -0.65)),
                  Spine=R(("Z", -18), ("X", 14)), Spine1=R(("Z", -10), ("X", 10)), Neck=R(("Z", 24), ("X", -16)),
                  LeftArm=A("Spine2", (0.3, -0.95, -0.15)), LeftForeArm=A("Spine2", (-0.75, -0.65, 0.1), -90),
                  LeftHand=A("Spine2", (-0.75, -0.65, 0.1), -90),
                  **mirror({"LeftArm": A("Spine2", (0.4, 0.35, -0.85)), "LeftForeArm": A("Spine2", (0.25, 0.1, -1))}),
                  loc=(0, -0.14, -0.08)), "out"),
        (21, pose(legs((0.1, -0.6, -0.8), (0.06, -0.05, -1), (-0.08, 0.45, -0.9), (-0.06, 0.7, -0.7)),
                  Spine=R(("Z", -14), ("X", 12)), Spine1=R(("Z", -8), ("X", 8)), Neck=R(("Z", 20), ("X", -12)),
                  LeftArm=A("Spine2", (0.3, -0.95, -0.15)), LeftForeArm=A("Spine2", (-0.75, -0.65, 0.1), -90),
                  LeftHand=A("Spine2", (-0.75, -0.65, 0.1), -90),
                  **mirror({"LeftArm": A("Spine2", (0.4, 0.3, -0.85)), "LeftForeArm": A("Spine2", (0.25, 0.05, -1))}),
                  loc=(0, -0.12, -0.08)), "io"),
        (29, GUARD, "io"),
    ],
    # ladrao pega do chao com as duas maos: agacha e alcanca -> fecha as maos na carga -> levanta para o peito
    "Grab": [
        (0, READY, "io"),
        (8, pose(legs((0.15, -0.75, -0.65), (0.08, 0.6, -0.8), (-0.15, -0.75, -0.65), (-0.08, 0.6, -0.8)),
                 Spine=R(("X", 28)), Spine1=R(("X", 18)), Spine2=R(("X", 8)), Neck=R(("X", -24)), loc=(0, 0.06, -0.32),
                 **both({"LeftArm": A("Spine2", (0.3, -0.35, -0.9)), "LeftForeArm": A("Spine2", (-0.05, -0.3, -1), -90),
                         "LeftHand": A("Spine2", (-0.1, -0.3, -1), -90)})), "io"),
        (13, pose(legs((0.15, -0.75, -0.65), (0.08, 0.6, -0.8), (-0.15, -0.75, -0.65), (-0.08, 0.6, -0.8)),
                  Spine=R(("X", 30)), Spine1=R(("X", 20)), Spine2=R(("X", 8)), Neck=R(("X", -26)), loc=(0, 0.06, -0.34),
                  **both({"LeftArm": A("Spine2", (0.25, -0.4, -0.9)), "LeftForeArm": A("Spine2", (-0.15, -0.35, -1), -90),
                          "LeftHand": A("Spine2", (-0.2, -0.35, -1), -90)})), "io"),
        (23, pose(HOLD_ARMS, KNEES, Spine1=R(("X", -3)), loc=(0, 0, -0.04)), "io"),
    ],
}


def die(msg):
    print("ERRO " + msg)
    sys.exit(1)


def short(n):
    return n.split(":")[-1]


def deltas(arm, spec):
    """Delta de rotacao sobre o repouso, no mundo, de cada osso (pais antes dos filhos)."""
    W = arm.matrix_world
    bones = {short(b.name): b for b in arm.data.bones}
    D = {}

    def rest_dir(b):
        return ((W @ b.tail_local) - (W @ b.head_local)).normalized()

    def walk(b):
        n = short(b.name)
        dp = D[short(b.parent.name)] if b.parent else Quaternion()
        s = spec.get(n)
        if s is None:
            D[n] = dp
        elif s[0] == "rot":
            q = Quaternion()
            for ax, g in s[1:]:
                q = Quaternion(Vector({"X": (1, 0, 0), "Y": (0, 1, 0), "Z": (0, 0, 1)}[ax]), math.radians(g)) @ q
            D[n] = dp @ q
        else:
            _, ref, v, tw = s
            u = rest_dir(b)
            dref = D[ref] if ref else Quaternion()
            t = u if v is None else Vector(v).normalized()
            D[n] = dref @ u.rotation_difference(t) @ Quaternion(u, math.radians(tw))
        for c in b.children:
            walk(c)
    for b in arm.data.bones:
        if b.parent is None:
            walk(b)
    missing = [k for k in spec if k not in bones]
    if missing:
        die("ossos da pose nao existem no rig: " + ", ".join(missing))
    return D


def apply(arm, D, loc, order):
    """Escreve matrix_basis de cada osso a partir dos deltas (mesma conta do retarget do render_sprites.py)."""
    W = arm.matrix_world
    qW_inv, W3_inv = W.to_quaternion().inverted(), W.inverted().to_3x3()
    pose_m = {}
    for b in order:
        pb = arm.pose.bones[b.name]
        attach = pose_m[b.parent.name] @ b.parent.matrix_local.inverted() @ b.matrix_local if b.parent else b.matrix_local.copy()
        rot = qW_inv @ D[short(b.name)] @ (W @ b.matrix_local).to_quaternion()
        head = attach.translation + (W3_inv @ Vector(loc) if not b.parent else Vector())
        m = Matrix.Translation(head) @ rot.to_matrix().to_4x4()
        pose_m[b.name] = m
        pb.matrix_basis = attach.inverted() @ m


def ease(x, kind):
    x = min(max(x, 0.0), 1.0)
    return 1 - (1 - x) ** 3 if kind == "out" else x * x * (3 - 2 * x)


def dirs(ob, posed):
    W = ob.matrix_world
    if posed:
        return {p.name: ((W @ p.tail) - (W @ p.head)).normalized() for p in ob.pose.bones if p.length > 1e-6}
    return {b.name: ((W @ b.tail_local) - (W @ b.head_local)).normalized() for b in ob.data.bones if b.length > 1e-6}


def worst(a, b):
    return min(v.dot(b[n]) for n, v in a.items() if n in b)


def verify(arm, path, D0, D1):
    """Prova do export: o repouso reimportado e o T do rig de origem (se nao, o render poe bracos em T) e o 1o e o
    ultimo quadro reimportados batem com a 1a e a ultima pose-chave (delta x repouso), nao com a action, que pode
    estar errada pelo mesmo motivo."""
    sc = bpy.context.scene
    rest = dirs(arm, False)
    want = [{n: D[short(n)] @ u for n, u in rest.items()} for D in (D0, D1)]
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    objs = [o for o in bpy.data.objects if o not in before]
    new = next(o for o in objs if o.type == "ARMATURE")
    act = new.animation_data.action
    got = []
    for f in act.frame_range:
        sc.frame_set(int(round(f)))
        got.append(dirs(new, True))
    rest = worst(rest, dirs(new, False))
    pose = min(worst(w, g) for w, g in zip(want, got))
    bpy.data.batch_remove(objs + [new.data, act])
    if rest < 0.999:
        die("export de %s: repouso reimportado difere do rig (pior cos %.3f): a pose virou repouso" % (path, rest))
    if pose < 0.995:
        die("export de %s: 1o/ultimo quadro reimportado difere da pose escrita (pior cos %.3f)" % (path, pose))


def build(arm, name, keys, out):
    order = []

    def walk(b):
        order.append(b)
        for c in b.children:
            walk(c)
    for b in arm.data.bones:
        if b.parent is None:
            walk(b)
    ks = [(f, deltas(arm, p[0]), Vector(p[1]), e) for f, p, e in keys]
    ad = arm.animation_data or arm.animation_data_create()
    ad.action = bpy.data.actions.new(name)
    for pb in arm.pose.bones:
        pb.rotation_mode = "QUATERNION"
    # O exportador FBX grava como repouso do osso (Lcl) a pose do quadro corrente quando nao ha malha com bind pose;
    # sem isto o clipe reimportado vem com repouso = pose e rotacao identidade (bracos em T no render). Chave de
    # repouso 2 quadros antes do clipe (fora da faixa exportada) e export parado nela.
    f_rest = ks[0][0] - 2
    for b in order:
        pb = arm.pose.bones[b.name]
        pb.matrix_basis = Matrix()
        pb.keyframe_insert("rotation_quaternion", frame=f_rest)
        if not b.parent:
            pb.keyframe_insert("location", frame=f_rest)
    for f in range(ks[0][0], ks[-1][0] + 1):
        j = max(i for i, k in enumerate(ks) if k[0] <= f)
        f0, D0, L0, _ = ks[j]
        f1, D1, L1, e1 = ks[min(j + 1, len(ks) - 1)]
        x = ease((f - f0) / (f1 - f0), e1) if f1 > f0 else 0.0
        D = {n: D0[n].slerp(D1[n], x) for n in D0}
        apply(arm, D, L0.lerp(L1, x), order)  # sem view_layer.update(): ele reavaliaria a action e apagaria a pose
        for b in order:
            pb = arm.pose.bones[b.name]
            pb.keyframe_insert("rotation_quaternion", frame=f)
            if not b.parent:
                pb.keyframe_insert("location", frame=f)
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = ks[0][0], ks[-1][0]
    bpy.context.scene.frame_set(f_rest)
    path = os.path.join(out, name + ".fbx")
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"ARMATURE"}, add_leaf_bones=False,
                             bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0)
    verify(arm, path, ks[0][1], ks[-1][1])
    print("CLIPE %-10s quadros=%d (%.2f s) -> %s" % (name, ks[-1][0] - ks[0][0] + 1, (ks[-1][0] - ks[0][0]) / FPS, path))


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--rig", required=True, help="FBX Mixamo Without Skin (o armature vira a base dos clipes)")
    p.add_argument("--out", required=True)
    p.add_argument("--only", default="", help="lista de clipes separada por virgula (padrao: todos)")
    a = p.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    names = [n for n in a.only.split(",") if n] or list(CLIPS)
    bad = [n for n in names if n not in CLIPS]
    if bad:
        die("clipe desconhecido: " + ", ".join(bad))
    os.makedirs(a.out, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = FPS
    bpy.ops.import_scene.fbx(filepath=os.path.abspath(a.rig))
    arm = next((o for o in bpy.data.objects if o.type == "ARMATURE"), None)
    if arm is None:
        die("--rig sem armature: " + a.rig)
    for o in [o for o in bpy.data.objects if o is not arm]:
        bpy.data.objects.remove(o)
    if arm.animation_data:
        arm.animation_data.action = None
    for act in list(bpy.data.actions):
        bpy.data.actions.remove(act)
    for n in names:
        build(arm, n, CLIPS[n], a.out)
    print("ANIM_OK %d clipe(s) em %s" % (len(names), a.out))


main()
