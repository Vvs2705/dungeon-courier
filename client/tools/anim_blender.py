"""anim_blender.py - Blender headless: clipes que o Mixamo nao tem, animados por keyframe sobre o esqueleto Mixamo.

Uso (Blender 5.2):
  blender.exe -b --factory-startup --python-exit-code 1 --python anim_blender.py --
      --rig <qualquer clipe Mixamo Without Skin (X Bot)>.fbx --out <pasta> [--only Hold,Throw]
Saida: <pasta>/<Clipe>.fbx (so o armature, 1 take, 30 fps) para o render_sprites.py (--anims / --hold).

Como a pose e escrita (POSES abaixo): o personagem olha -Y, +X = esquerda dele, +Z = cima (repouso em T do X Bot).
  ("rot", (eixo, graus), ...)    rotacao relativa ao pai, em eixos do mundo do repouso, aplicadas na ordem
  ("aim", ref, (x, y, z), tw)    o osso aponta para (x, y, z) no referencial do osso `ref` ("" = mundo),
                                 com `tw` graus de giro no proprio eixo (palma da mao); filhos sem spec seguem o pai
  "loc": (x, y, z)               deslocamento do quadril em m (escala do X Bot, 1,8 m); o z final vem das pernas: o pe
                                 mais baixo fica no chao em todo quadro (agachar = dobrar o joelho)
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
FOOT = ("LeftFoot", "RightFoot")
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
SOFT = legs((0.06, -0.1, -0.99), (0.06, 0.08, -1), (-0.06, -0.1, -0.99), (-0.06, 0.08, -1))  # joelhos so cedendo
KNEES = legs((0.08, -0.25, -0.97), (0.06, 0.15, -1), (-0.08, -0.25, -0.97), (-0.06, 0.15, -1))  # joelhos dobrados
SQUAT = legs((0.15, -0.75, -0.65), (0.08, 0.6, -0.8), (-0.15, -0.75, -0.65), (-0.08, 0.6, -0.8))  # agachado fundo
STEP = legs((0.1, -0.5, -0.86), (0.06, -0.05, -1), (-0.08, 0.35, -0.94), (-0.06, 0.6, -0.8))  # pe esquerdo a frente
LUNGE = legs((0.1, -0.7, -0.7), (0.06, -0.05, -1), (-0.08, 0.55, -0.83), (-0.06, 0.8, -0.6))  # passo longo


def pose(*parts, **kw):
    d = {}
    for p in parts:
        d.update(p)
    d.update({k: v for k, v in kw.items() if k != "loc"})
    return d, kw.get("loc", (0, 0, 0))


READY = pose(ARMS_DOWN, KNEES, loc=(0, 0, -0.04))
CALM = pose(ARMS_DOWN, STAND)  # cliente em pe, como no Breathing Idle
GUARD = pose(GUARD_ARMS, KNEES, Spine=R(("X", 4)), loc=(0, 0, -0.04))
GUARD_IN = pose(GUARD_ARMS, KNEES, Spine=R(("X", 2)), Spine1=R(("X", -2)), loc=(0, 0, -0.025))  # respiracao
# antebraco do escudo na horizontal (inclinado, ele girava o escudo no proprio plano), recolhido / empurrado
SHIELD = {"LeftForeArm": A("Spine2", (-0.82, -0.55, 0.0), -90), "LeftHand": A("Spine2", (-0.82, -0.55, 0.0), -90)}
SHIELD_OUT = {"LeftForeArm": A("Spine2", (-0.75, -0.65, 0.12), -90), "LeftHand": A("Spine2", (-0.75, -0.65, 0.12), -90)}
# palmas para cima: no repouso em T a palma olha -Z; giro de 180 no eixo do antebraco a vira para +Z
SHRUG_ARMS = both({"LeftArm": A("Spine2", (0.35, 0.15, -0.92)), "LeftForeArm": A("Spine2", (0.7, -0.6, 0.35), 180),
                   "LeftHand": A("Spine2", (0.75, -0.4, 0.55), 180)})

# bruto e ogro: base larga, curvados, bracos pesados a frente (o ogro abre os bracos por causa da barriga)
WIDE = legs((0.16, -0.2, -0.97), (0.1, 0.15, -0.99), (-0.16, -0.2, -0.97), (-0.1, 0.15, -0.99))
WIDE_SQUAT = legs((0.3, -0.6, -0.75), (0.12, 0.45, -0.88), (-0.3, -0.6, -0.75), (-0.12, 0.45, -0.88))
BRUTE = pose(WIDE, Spine=R(("X", 10)), Spine1=R(("X", 6)), Neck=R(("X", -10)),
             **both({"LeftArm": A("Spine2", (0.35, -0.3, -0.89)), "LeftForeArm": A("Spine2", (0.25, -0.55, -0.8)),
                     "LeftHand": A("Spine2", (0.2, -0.6, -0.77))}))
OGRE = pose(WIDE, Spine=R(("X", 6)), Neck=R(("X", -12)),
            **both({"LeftArm": A("Spine2", (0.45, -0.2, -0.87)), "LeftForeArm": A("Spine2", (0.35, -0.45, -0.82))}))

# clipe -> [(quadro, pose, suavizacao ate esta chave)]. As chaves caem nos quadros que o render amostra
# (RENDER_SPRITES.md sec. 9): pose extrema fora de um quadro amostrado some da folha.
CLIPS = {
    # pose parada de segurar (fonte do --hold): 10 quadros iguais
    "Hold": [(0, pose(HOLD_ARMS, STAND, Spine1=R(("X", -4))), "io"), (9, pose(HOLD_ARMS, STAND, Spine1=R(("X", -4))), "io")],
    # arremesso por cima, mao direita; 10 quadros de 2 em 2. A View toca em laco no compasso do tiro e solta o
    # projetil na metade (ThrowRelease 0,5 = chave 10); o fim (20) emenda no comeco (0)
    "Throw": [
        (0, READY, "io"),
        # antecipacao: afunda, o tronco comeca a girar para a direita e o braco direito desce para tras (arco)
        (4, pose(KNEES, Spine=R(("Z", -10)), Spine1=R(("Z", -6)), Neck=R(("Z", 8)),
                 LeftArm=A("Spine2", (0.35, -0.8, -0.45)), LeftForeArm=A("Spine2", (0.25, -0.95, -0.1), -60),
                 RightArm=A("Spine2", (-0.4, 0.45, -0.8)), RightForeArm=A("Spine2", (-0.25, 0.6, -0.75), 40),
                 loc=(0, 0.02, -0.07)), "io"),
        # carregado: tronco torcido e inclinado para tras, mao direita alta atras da cabeca, mao esquerda aponta o alvo
        (8, pose(legs((0.1, -0.25, -0.97), (0.06, 0.0, -1), (-0.08, 0.15, -0.98), (-0.06, 0.35, -0.94)),
                 Spine=R(("Z", -24), ("X", -6)), Spine1=R(("Z", -12), ("X", -4)), Neck=R(("Z", 22)), Head=R(("X", -4)),
                 LeftArm=A("Spine2", (0.35, -0.95, 0.15)), LeftForeArm=A("Spine2", (0.2, -1, 0.2), -60),
                 RightArm=A("Spine2", (-0.6, 0.5, 0.6)), RightForeArm=A("Spine2", (-0.05, 0.3, 1), 60),
                 RightHand=A("Spine2", (0, 0.5, 1), 60), loc=(0, 0.05, -0.04)), "io"),
        # solta (impacto): chicote do tronco, braco direito estendido a frente, a mao esquerda volta ao quadril
        (10, pose(STEP, Spine=R(("Z", 16), ("X", 10)), Spine1=R(("Z", 10), ("X", 8)), Neck=R(("Z", -12), ("X", -8)),
                  LeftArm=A("Spine2", (0.45, 0.45, -0.75)), LeftForeArm=A("Spine2", (0.3, -0.1, -0.95)),
                  RightArm=A("Spine2", (-0.2, -0.95, 0.25)), RightForeArm=A("Spine2", (-0.05, -1, 0.15), 30),
                  RightHand=A("Spine2", (0, -1, 0.05), 30), loc=(0, -0.06, -0.06)), "out"),
        # acompanha: o braco cruza o corpo ate o quadril esquerdo e o tronco passa do ponto
        (14, pose(legs((0.1, -0.55, -0.83), (0.06, -0.05, -1), (-0.08, 0.4, -0.92), (-0.06, 0.65, -0.76)),
                  Spine=R(("Z", 26), ("X", 16)), Spine1=R(("Z", 12), ("X", 10)), Neck=R(("Z", -18), ("X", -12)),
                  LeftArm=A("Spine2", (0.5, 0.5, -0.7)), LeftForeArm=A("Spine2", (0.35, 0.2, -0.9)),
                  RightArm=A("Spine2", (0.15, -0.75, -0.65)), RightForeArm=A("Spine2", (0.45, -0.5, -0.75), 20),
                  RightHand=A("Spine2", (0.5, -0.4, -0.75), 20), loc=(0, -0.08, -0.08)), "io"),
        # volta com sobra: o tronco quica um pouco para tras e os bracos balancam para baixo
        (17, pose(KNEES, Spine=R(("Z", 6), ("X", -3)), Spine1=R(("Z", 2)), Neck=R(("Z", -4)),
                  LeftArm=A("Spine2", (0.3, 0.15, -0.95)), LeftForeArm=A("Spine2", (0.2, -0.2, -0.95)),
                  RightArm=A("Spine2", (-0.2, -0.3, -0.93)), RightForeArm=A("Spine2", (-0.1, -0.55, -0.83)),
                  loc=(0, -0.03, -0.05)), "io"),
        (20, READY, "io"),
    ],
    # avanco curto (a View toca o clipe inteiro nos 0,18 s do dash; 6 quadros em 0; 2,5; 5 ... 12,5)
    "Dash": [
        (0, READY, "io"),
        # antecipacao: agacha fundo com o tronco a frente; os bracos vem a frente (contrapeso)
        (3, pose(legs((0.1, -0.45, -0.89), (0.06, 0.3, -0.95), (-0.1, -0.45, -0.89), (-0.06, 0.3, -0.95)),
                 Spine=R(("X", 18)), Spine1=R(("X", 10)), Neck=R(("X", 4)), loc=(0, 0.02, -0.12),
                 **both({"LeftArm": A("Spine2", (0.15, -0.6, -0.78)), "LeftForeArm": A("Spine2", (0.1, -0.85, -0.5))})), "io"),
        # impulso: corpo projetado, perna de tras estendida, bracos jogados para tras, cabeca erguida olhando a frente.
        # Bracos em eixos do MUNDO (ref ""): com o tronco inclinado, "para tras" no peito virava "para cima"
        (5, pose(legs((0.08, -0.75, -0.65), (0.06, 0.05, -1), (-0.08, 0.6, -0.8), (-0.06, 0.85, -0.5)),
                 Spine=R(("X", 24)), Spine1=R(("X", 16)), Spine2=R(("X", 8)), Neck=R(("X", -26)), Head=R(("X", -8)),
                 loc=(0, -0.1, -0.13),
                 **both({"LeftArm": A("", (0.12, 0.85, -0.5)), "LeftForeArm": A("", (0.1, 0.95, -0.3), -30),
                         "LeftHand": A("", (0.1, 0.95, -0.25), -30)})), "out"),
        # deslize: estica mais, o pe da frente busca o chao e os bracos vao mais para tras
        (8, pose(legs((0.08, -0.8, -0.6), (0.06, -0.3, -0.95), (-0.08, 0.7, -0.7), (-0.06, 0.9, -0.4)),
                 Spine=R(("X", 28)), Spine1=R(("X", 18)), Spine2=R(("X", 8)), Neck=R(("X", -30)), Head=R(("X", -8)),
                 loc=(0, -0.14, -0.14),
                 **both({"LeftArm": A("", (0.1, 0.95, -0.3)), "LeftForeArm": A("", (0.08, 1, -0.1), -30),
                         "LeftHand": A("", (0.08, 1, -0.05), -30)})), "io"),
        # passagem: o tronco sobe e os bracos passam por baixo, junto do quadril (sem esta chave o slerp entre "para
        # tras" e "a frente" abria os bracos em T de frente para a camera)
        (10, pose(legs((0.08, -0.65, -0.75), (0.06, -0.2, -1), (-0.08, 0.5, -0.86), (-0.06, 0.7, -0.7)),
                  Spine=R(("X", 10)), Spine1=R(("X", 6)), Neck=R(("X", -8)), loc=(0, -0.08, -0.1),
                  **both({"LeftArm": A("Spine2", (0.1, 0.15, -1)), "LeftForeArm": A("Spine2", (0.08, 0.05, -1))})), "io"),
        # freada: pe da frente cravado, o tronco volta e passa um pouco para tras; cabeca e bracos seguem a frente
        (12, pose(legs((0.08, -0.55, -0.83), (0.06, -0.1, -1), (-0.08, 0.3, -0.95), (-0.06, 0.5, -0.87)),
                  Spine=R(("X", -6)), Spine1=R(("X", -4)), Neck=R(("X", 10)), Head=R(("X", 4)), loc=(0, -0.02, -0.07),
                  **both({"LeftArm": A("Spine2", (0.2, -0.6, -0.78)), "LeftForeArm": A("Spine2", (0.12, -0.9, -0.4))})), "io"),
        (15, READY, "io"),
    ],
    # recuo de dano (a View toca em 0,3 s): tranco -> rebote a frente -> assenta. Bracos soltos a frente, nunca em T
    "Hit": [
        (0, READY, "io"),
        # tranco (impacto): cabeca e tronco jogados para tras, quadril recua, bracos sobem a frente com os cotovelos
        # dobrados (ficam para tras do corpo que recua)
        (2, pose(legs((0.08, 0.1, -1), (0.06, 0.35, -0.95), (-0.08, -0.15, -1), (-0.06, 0.2, -1)),
                 Spine=R(("X", -14)), Spine1=R(("X", -10), ("Z", 8)), Spine2=R(("X", -4)), Neck=R(("X", -22)),
                 Head=R(("X", -12), ("Y", -6)), loc=(0, 0.08, -0.05),
                 LeftArm=A("Spine2", (0.3, -0.8, -0.45)), LeftForeArm=A("Spine2", (0.15, -0.95, 0.2), -40),
                 LeftHand=A("Spine2", (0.1, -0.9, 0.35), -40),
                 RightArm=A("Spine2", (-0.35, -0.75, -0.5)), RightForeArm=A("Spine2", (-0.25, -0.95, 0.05), 40),
                 RightHand=A("Spine2", (-0.2, -0.9, 0.2), 40)), "out"),
        # rebote: o tronco dobra a frente, a cabeca cai, os joelhos absorvem, os bracos desabam
        (5, pose(legs((0.08, -0.3, -0.95), (0.06, 0.25, -0.97), (-0.08, -0.3, -0.95), (-0.06, 0.25, -0.97)),
                 Spine=R(("X", 10)), Spine1=R(("X", 6), ("Z", -4)), Neck=R(("X", 10)), Head=R(("X", 6), ("Y", 4)),
                 loc=(0, 0.03, -0.07),
                 **both({"LeftArm": A("Spine2", (0.35, -0.35, -0.87)), "LeftForeArm": A("Spine2", (0.25, -0.7, -0.6))})), "io"),
        # assenta com uma sobra leve para tras
        (9, pose(KNEES, Spine=R(("X", -3)), Neck=R(("X", -4)), loc=(0, 0.02, -0.04),
                 **both({"LeftArm": A("Spine2", (0.3, -0.15, -0.95)), "LeftForeArm": A("Spine2", (0.2, -0.45, -0.85))})), "io"),
        (14, READY, "io"),
    ],
    # guarda parado com o escudo a frente, respirando (laco: ultima chave = primeira)
    "GuardIdle": [(0, GUARD, "io"), (30, GUARD_IN, "io"), (59, GUARD, "io")],
    # golpe de escudo, 12 quadros. A View estica [0, 6) pelo telegraph e toca [6, 12) em 0,15 s (ImpactFrac do guarda =
    # 6/12, WorldView): a carga (9-12) fecha o telegraph no quadro 5 (12,1) e o impacto (chave 15) cai no quadro 6 (14,5)
    "Bash": [
        (0, GUARD, "io"),
        # antecipacao 1: ergue o peito e recolhe o escudo junto ao corpo
        (5, pose(KNEES, Spine=R(("X", -4)), Spine1=R(("X", -3)), Neck=R(("X", -4)),
                 LeftArm=A("Spine2", (0.5, -0.4, -0.75)), **SHIELD,
                 **mirror({"LeftArm": A("Spine2", (0.3, 0.2, -0.93)), "LeftForeArm": A("Spine2", (0.2, -0.1, -0.97))}),
                 loc=(0, 0.02, -0.03)), "io"),
        # carga: agacha, gira o ombro esquerdo para tras com o escudo; a cabeca segue olhando o alvo por cima dele
        (9, pose(legs((0.08, -0.15, -1), (0.06, 0.3, -0.95), (-0.1, -0.3, -0.95), (-0.06, 0.2, -1)),
                  Spine=R(("Z", 24), ("X", -4)), Spine1=R(("Z", 12), ("X", -6)), Neck=R(("Z", -26)),
                  LeftArm=A("Spine2", (0.62, 0.25, -0.74)), **SHIELD,
                  **mirror({"LeftArm": A("Spine2", (0.35, -0.25, -0.9)), "LeftForeArm": A("Spine2", (0.2, -0.65, -0.73))}),
                  loc=(0, 0.07, -0.11)), "io"),
        # segura a carga (fim do telegraph): um pouco mais fundo, a cabeca abaixa atras do escudo
        (12, pose(legs((0.08, -0.15, -1), (0.06, 0.35, -0.94), (-0.1, -0.35, -0.93), (-0.06, 0.2, -1)),
                  Spine=R(("Z", 27), ("X", -2)), Spine1=R(("Z", 13), ("X", -6)), Neck=R(("Z", -28), ("X", 6)),
                  LeftArm=A("Spine2", (0.62, 0.28, -0.73)), **SHIELD,
                  **mirror({"LeftArm": A("Spine2", (0.35, -0.3, -0.88)), "LeftForeArm": A("Spine2", (0.2, -0.7, -0.68))}),
                  loc=(0, 0.08, -0.12)), "io"),
        # impacto: avanca com passo longo e empurra o escudo a frente; o braco direito vai para tras
        (15, pose(LUNGE, Spine=R(("Z", -18), ("X", 16)), Spine1=R(("Z", -10), ("X", 10)), Neck=R(("Z", 24), ("X", -16)),
                  LeftArm=A("Spine2", (0.3, -0.95, -0.1)), **SHIELD_OUT,
                  **mirror({"LeftArm": A("Spine2", (0.4, 0.4, -0.82)), "LeftForeArm": A("Spine2", (0.25, 0.15, -0.95))}),
                  loc=(0, -0.16, -0.09)), "out"),
        # passa do ponto: o peso continua, o escudo vai mais longe e a cabeca abaixa
        (18, pose(legs((0.1, -0.72, -0.68), (0.06, -0.08, -1), (-0.08, 0.6, -0.8), (-0.06, 0.82, -0.57)),
                  Spine=R(("Z", -22), ("X", 20)), Spine1=R(("Z", -12), ("X", 12)), Neck=R(("Z", 26), ("X", -18)),
                  Head=R(("X", 6)), LeftArm=A("Spine2", (0.25, -1, -0.05)), **SHIELD_OUT,
                  **mirror({"LeftArm": A("Spine2", (0.4, 0.45, -0.8)), "LeftForeArm": A("Spine2", (0.25, 0.2, -0.95))}),
                  loc=(0, -0.19, -0.1)), "io"),
        # recolhe: o peso volta e o escudo desce para a guarda
        (22, pose(legs((0.1, -0.45, -0.89), (0.06, 0.0, -1), (-0.08, 0.3, -0.95), (-0.06, 0.45, -0.89)),
                  Spine=R(("Z", -6), ("X", 8)), Spine1=R(("Z", -3), ("X", 4)), Neck=R(("Z", 8), ("X", -6)),
                  LeftArm=A("Spine2", (0.4, -0.75, -0.5)), LeftForeArm=A("Spine2", (-0.8, -0.6, 0.08), -90),
                  LeftHand=A("Spine2", (-0.8, -0.6, 0.08), -90),
                  **mirror({"LeftArm": A("Spine2", (0.35, 0.15, -0.92)), "LeftForeArm": A("Spine2", (0.2, -0.15, -0.97))}),
                  loc=(0, -0.08, -0.06)), "io"),
        (29, GUARD, "io"),
    ],
    # ladrao pega do chao com as duas maos em 0,5 s (= ThiefGrab do Core; a View troca para CarryRun nesse instante):
    # olha a carga -> agacha e alcanca -> fecha as maos -> ergue com sobra para tras -> pose do Hold
    "Grab": [
        (0, READY, "io"),
        # antecipacao: sobe um pouco e olha para a carga no chao
        (2, pose(STAND, Spine=R(("X", 6)), Neck=R(("X", 18)), loc=(0, 0, -0.01),
                 **both({"LeftArm": A("Spine2", (0.3, -0.25, -0.92)), "LeftForeArm": A("Spine2", (0.15, -0.5, -0.85))})), "io"),
        # alcanca: agacha fundo, tronco a frente, as maos descem dos lados da carga
        (5, pose(SQUAT, Spine=R(("X", 28)), Spine1=R(("X", 18)), Spine2=R(("X", 8)), Neck=R(("X", -22)),
                 loc=(0, 0.06, -0.33),
                 **both({"LeftArm": A("Spine2", (0.3, -0.35, -0.9)), "LeftForeArm": A("Spine2", (-0.05, -0.3, -1), -90),
                         "LeftHand": A("Spine2", (-0.1, -0.3, -1), -90)})), "io"),
        # agarra: afunda mais um pouco e fecha as maos na carga
        (7, pose(SQUAT, Spine=R(("X", 31)), Spine1=R(("X", 20)), Spine2=R(("X", 8)), Neck=R(("X", -25)),
                 loc=(0, 0.06, -0.35),
                 **both({"LeftArm": A("Spine2", (0.25, -0.4, -0.9)), "LeftForeArm": A("Spine2", (-0.15, -0.35, -1), -90),
                         "LeftHand": A("Spine2", (-0.2, -0.35, -1), -90)})), "io"),
        # ergue com sobra: o corpo sobe e inclina para tras com o peso da carga ja no peito
        (10, pose(HOLD_ARMS, SOFT, Spine=R(("X", -8)), Spine1=R(("X", -6)), Neck=R(("X", 6)), loc=(0, 0.03, -0.005)), "io"),
        (15, pose(HOLD_ARMS, KNEES, Spine1=R(("X", -3)), loc=(0, 0, -0.04)), "io"),
    ],
    # cliente "dar de ombros" (2 rachaduras), de frente para a camera; a View toca em laco: o fim emenda no comeco.
    # 12 quadros de 4 em 4: 8 antecipa, 16-28 ombros no alto com palmas para cima e cabeca de lado, 38 solta
    "Shrug": [
        (0, CALM, "io"),
        # antecipacao: os ombros caem, os joelhos cedem, os antebracos comecam a abrir
        (8, pose(SOFT, Neck=R(("X", 6)), loc=(0, 0, -0.005),
                 **both({"LeftShoulder": R(("Y", 5)), "LeftArm": A("Spine2", (0.3, 0.0, -0.95)),
                         "LeftForeArm": A("Spine2", (0.35, -0.55, -0.75), 120)})), "io"),
        # dar de ombros: ombros sobem, peito abre, cotovelos junto do corpo, antebracos para fora com as palmas para
        # cima e a cabeca inclinada de lado
        (16, pose(STAND, SHRUG_ARMS, Spine1=R(("X", -4)), Spine2=R(("X", -3)), Neck=R(("Y", 14), ("X", -4)),
                  Head=R(("Y", 10)), **both({"LeftShoulder": R(("Y", -22))})), "out"),
        # segura (moving hold): a cabeca inclina mais e as maos abrem mais um pouco
        (28, pose(STAND, Spine1=R(("X", -5)), Spine2=R(("X", -3)), Neck=R(("Y", 18), ("X", -5)), Head=R(("Y", 12)),
                  **both({"LeftShoulder": R(("Y", -25)), "LeftArm": A("Spine2", (0.4, 0.15, -0.9)),
                          "LeftForeArm": A("Spine2", (0.78, -0.5, 0.38), 180),
                          "LeftHand": A("Spine2", (0.8, -0.3, 0.55), 180)})), "io"),
        # solta: os ombros caem com sobra para baixo, os bracos descem, os joelhos cedem
        (38, pose(SOFT, Neck=R(("Y", 3), ("X", 3)), loc=(0, 0, -0.005),
                  **both({"LeftShoulder": R(("Y", 5)), "LeftArm": A("Spine2", (0.3, 0.0, -0.95)),
                          "LeftForeArm": A("Spine2", (0.3, -0.35, -0.88), 60)})), "io"),
        (48, CALM, "io"),
    ],
    # bruto: golpe de cima para baixo com a manopla direita, 12 quadros. A View estica [0, 7) pelo telegraph e toca
    # [7, 12) em 0,15 s (ImpactFrac 0,6 x 12 = quadro 7 = chave 17); a manopla no alto (11-15) fecha o telegraph
    "Smash": [
        (0, BRUTE, "io"),
        # antecipacao 1: endireita e traz os dois punhos ao peito
        (6, pose(WIDE, Spine1=R(("X", -4)), Neck=R(("X", -6)),
                 **both({"LeftArm": A("Spine2", (0.5, -0.45, -0.74)), "LeftForeArm": A("Spine2", (-0.1, -0.8, 0.45))})), "io"),
        # carga: manopla direita no alto atras da cabeca, tronco torcido e inclinado para tras, braco esquerdo a frente
        (11, pose(legs((0.14, -0.1, -0.99), (0.1, 0.05, -1), (-0.16, 0.15, -0.98), (-0.1, 0.25, -0.97)),
                  Spine=R(("Z", -16), ("X", -10)), Spine1=R(("Z", -8), ("X", -8)), Neck=R(("Z", 10), ("X", 8)),
                  LeftArm=A("Spine2", (0.45, -0.75, -0.5)), LeftForeArm=A("Spine2", (0.1, -0.95, 0.2)),
                  RightArm=A("Spine2", (-0.3, 0.25, 0.92)), RightForeArm=A("Spine2", (-0.1, 0.55, 0.83), 30),
                  RightHand=A("Spine2", (-0.05, 0.6, 0.8), 30)), "io"),
        # segura no alto (fim do telegraph)
        (15, pose(legs((0.14, -0.1, -0.99), (0.1, 0.05, -1), (-0.16, 0.18, -0.97), (-0.1, 0.28, -0.96)),
                  Spine=R(("Z", -18), ("X", -13)), Spine1=R(("Z", -9), ("X", -10)), Neck=R(("Z", 11), ("X", 10)),
                  LeftArm=A("Spine2", (0.45, -0.78, -0.45)), LeftForeArm=A("Spine2", (0.1, -0.95, 0.25)),
                  RightArm=A("Spine2", (-0.25, 0.35, 0.9)), RightForeArm=A("Spine2", (-0.1, 0.6, 0.79), 30),
                  RightHand=A("Spine2", (-0.05, 0.65, 0.76), 30)), "io"),
        # impacto: a manopla desce no chao a frente, passo com o pe esquerdo, o braco esquerdo vai para tras
        (17, pose(legs((0.18, -0.6, -0.78), (0.1, 0.0, -1), (-0.16, 0.4, -0.9), (-0.1, 0.6, -0.8)),
                  Spine=R(("Z", 14), ("X", 24)), Spine1=R(("Z", 8), ("X", 16)), Neck=R(("Z", -8), ("X", -20)),
                  LeftArm=A("Spine2", (0.45, 0.4, -0.8)), LeftForeArm=A("Spine2", (0.3, 0.1, -0.95)),
                  RightArm=A("Spine2", (-0.15, -0.8, -0.58)), RightForeArm=A("Spine2", (-0.05, -0.5, -0.86)),
                  RightHand=A("Spine2", (-0.05, -0.4, -0.92))), "out"),
        # passa do ponto: dobra mais, o punho apoiado no chao
        (20, pose(legs((0.2, -0.68, -0.7), (0.1, 0.1, -0.99), (-0.18, 0.45, -0.87), (-0.1, 0.68, -0.73)),
                  Spine=R(("Z", 16), ("X", 30)), Spine1=R(("Z", 8), ("X", 18)), Neck=R(("Z", -8), ("X", -24)),
                  LeftArm=A("Spine2", (0.45, 0.45, -0.77)), LeftForeArm=A("Spine2", (0.3, 0.15, -0.94)),
                  RightArm=A("Spine2", (-0.1, -0.6, -0.8)), RightForeArm=A("Spine2", (0, -0.2, -0.98)),
                  RightHand=A("Spine2", (0, -0.1, -0.99))), "io"),
        # recolhe: o tronco sobe e os bracos voltam a pender
        (24, pose(legs((0.18, -0.4, -0.9), (0.1, 0.1, -0.99), (-0.17, 0.2, -0.96), (-0.1, 0.35, -0.93)),
                  Spine=R(("Z", 6), ("X", 16)), Spine1=R(("Z", 3), ("X", 8)), Neck=R(("X", -14)),
                  LeftArm=A("Spine2", (0.35, 0.0, -0.94)), LeftForeArm=A("Spine2", (0.25, -0.3, -0.92)),
                  RightArm=A("Spine2", (-0.3, -0.5, -0.8)), RightForeArm=A("Spine2", (-0.2, -0.6, -0.77))), "io"),
        (29, BRUTE, "io"),
    ],
    # ogro: pancada no chao com os dois punhos, 9 quadros de 3,33 em 3,33 (preset ogro: 448 px x 9). A View estica [0, 5)
    # pelo telegraph e toca [5, 9) em 0,15 s (ImpactFrac 0,6 x 9 = quadro 5 = 16,7 -> chave 17); punhos no alto (11-14)
    "Slam": [
        (0, OGRE, "io"),
        # antecipacao: afunda e joga os dois bracos para baixo e para tras (eixos do mundo)
        (4, pose(legs((0.22, -0.4, -0.89), (0.1, 0.3, -0.95), (-0.22, -0.4, -0.89), (-0.1, 0.3, -0.95)),
                 Spine=R(("X", 18)), Spine1=R(("X", 6)), Neck=R(("X", -6)),
                 **both({"LeftArm": A("", (0.25, 0.4, -0.88)), "LeftForeArm": A("", (0.2, 0.5, -0.84))})), "io"),
        # os bracos voltam pela frente: passam por baixo (6) e chegam a frente (8). O slerp do delta sobre o T entre
        # chaves muito distantes levava os bracos pelos lados (T/Y de frente para a camera); chaves a menos de 90 graus
        (6, pose(legs((0.2, -0.3, -0.93), (0.1, 0.22, -0.97), (-0.2, -0.3, -0.93), (-0.1, 0.22, -0.97)),
                 Spine=R(("X", 12)), Spine1=R(("X", 2)), Neck=R(("X", -4)),
                 **both({"LeftArm": A("Spine2", (0.2, -0.1, -0.97)), "LeftForeArm": A("Spine2", (0.15, -0.25, -0.96))})), "io"),
        (8, pose(legs((0.18, -0.2, -0.96), (0.1, 0.15, -0.99), (-0.18, -0.2, -0.96), (-0.1, 0.15, -0.99)),
                 Spine=R(("X", 2)), Spine1=R(("X", -4)), Neck=R(("X", 2)),
                 **both({"LeftArm": A("Spine2", (0.2, -0.95, 0.2)), "LeftForeArm": A("Spine2", (0.05, -0.85, 0.52)),
                         "LeftHand": A("Spine2", (0.0, -0.8, 0.6))})), "io"),
        # sobe: os dois punhos juntos acima da cabeca. Tronco quase reto: inclinado para tras, os ombros saiam do eixo
        # e a 60 graus "para tras" sobe na tela; os punhos passavam do topo da celula de 448 px
        (11, pose(legs((0.16, -0.05, -0.99), (0.1, 0.02, -1), (-0.16, -0.05, -0.99), (-0.1, 0.02, -1)),
                  Spine=R(("X", -4)), Spine1=R(("X", -3)), Neck=R(("X", 4)),
                  **both({"LeftArm": A("Spine2", (0.25, 0.15, 0.96)), "LeftForeArm": A("Spine2", (-0.35, 0.05, 0.94)),
                          "LeftHand": A("Spine2", (-0.4, 0.05, 0.92))})), "io"),
        # segura no alto (fim do telegraph)
        (14, pose(legs((0.16, -0.05, -0.99), (0.1, 0.02, -1), (-0.16, -0.05, -0.99), (-0.1, 0.02, -1)),
                  Spine=R(("X", -6)), Spine1=R(("X", -4)), Neck=R(("X", 6)),
                  **both({"LeftArm": A("Spine2", (0.22, 0.25, 0.94)), "LeftForeArm": A("Spine2", (-0.35, 0.15, 0.92)),
                          "LeftHand": A("Spine2", (-0.4, 0.15, 0.9))})), "io"),
        # impacto: os dois punhos no chao a frente, agachado largo
        (17, pose(WIDE_SQUAT, Spine=R(("X", 30)), Spine1=R(("X", 18)), Spine2=R(("X", 8)), Neck=R(("X", -26)),
                  **both({"LeftArm": A("Spine2", (0.2, -0.7, -0.68)), "LeftForeArm": A("Spine2", (-0.05, -0.35, -0.94)),
                          "LeftHand": A("Spine2", (-0.05, -0.2, -0.98))})), "out"),
        # passa do ponto: afunda mais com o peso
        (20, pose(legs((0.32, -0.66, -0.68), (0.12, 0.5, -0.86), (-0.32, -0.66, -0.68), (-0.12, 0.5, -0.86)),
                  Spine=R(("X", 34)), Spine1=R(("X", 20)), Spine2=R(("X", 8)), Neck=R(("X", -28)),
                  **both({"LeftArm": A("Spine2", (0.22, -0.6, -0.77)), "LeftForeArm": A("Spine2", (-0.05, -0.15, -0.99)),
                          "LeftHand": A("Spine2", (-0.05, -0.05, -1))})), "io"),
        # levanta: o tronco sobe e os bracos voltam a pender
        (24, pose(legs((0.2, -0.35, -0.91), (0.1, 0.25, -0.96), (-0.2, -0.35, -0.91), (-0.1, 0.25, -0.96)),
                  Spine=R(("X", 18)), Spine1=R(("X", 6)), Neck=R(("X", -14)),
                  **both({"LeftArm": A("Spine2", (0.4, -0.35, -0.85)), "LeftForeArm": A("Spine2", (0.3, -0.5, -0.81))})), "io"),
        (30, OGRE, "io"),
    ],
    # ogro chama um ladrao (a View toca o clipe inteiro uma vez): enche o peito -> ruge projetando a cabeca, com os
    # punhos fechados a frente do peito -> segura tremendo -> volta. Bracos a frente, nao abertos: abertos liam como T
    # de frente/de costas e o raio (1,9 m) nao cabia na celula
    "Roar": [
        (0, OGRE, "io"),
        # inspira: tronco e cabeca para tras, cotovelos para tras junto do corpo
        (8, pose(legs((0.16, -0.08, -0.99), (0.1, 0.04, -1), (-0.16, -0.08, -0.99), (-0.1, 0.04, -1)),
                 Spine=R(("X", -10)), Spine1=R(("X", -8)), Neck=R(("X", -14)),
                 **both({"LeftArm": A("Spine2", (0.5, 0.45, -0.74)), "LeftForeArm": A("Spine2", (0.3, -0.3, -0.9))})), "io"),
        # ruge: tronco a frente, cabeca projetada olhando o alvo, punhos fechados a frente do peito
        (16, pose(legs((0.24, -0.35, -0.9), (0.1, 0.25, -0.96), (-0.24, -0.35, -0.9), (-0.1, 0.25, -0.96)),
                  Spine=R(("X", 18)), Spine1=R(("X", 12)), Neck=R(("X", -24)),
                  **both({"LeftArm": A("Spine2", (0.5, -0.65, -0.57)), "LeftForeArm": A("Spine2", (0.05, -0.75, 0.66), -40),
                          "LeftHand": A("Spine2", (0.0, -0.7, 0.7), -40)})), "out"),
        # segura o rugido com um tremor: o tronco torce um pouco e os punhos sobem
        (28, pose(legs((0.24, -0.35, -0.9), (0.1, 0.25, -0.96), (-0.24, -0.35, -0.9), (-0.1, 0.25, -0.96)),
                  Spine=R(("X", 20)), Spine1=R(("X", 12), ("Z", 4)), Neck=R(("X", -26), ("Z", 6)),
                  **both({"LeftArm": A("Spine2", (0.5, -0.7, -0.5)), "LeftForeArm": A("Spine2", (0.05, -0.7, 0.71), -40),
                          "LeftHand": A("Spine2", (0.0, -0.65, 0.76), -40)})), "io"),
        # volta: o tronco sobe e os bracos descem
        (34, pose(WIDE, Spine=R(("X", 8)), Neck=R(("X", -12)),
                  **both({"LeftArm": A("Spine2", (0.5, -0.25, -0.83)), "LeftForeArm": A("Spine2", (0.4, -0.45, -0.8))})), "io"),
        (40, OGRE, "io"),
    ],
    # arqueiro: puxa o arco (preso a mao esquerda pelo --attach) e solta. 12 quadros de 3 em 3. A View estica [0, 8) pela
    # mira de 0,8 s e toca [8, 12) em 0,15 s (ImpactFrac do arqueiro 0,7 x 12 = quadro 8 = chave 24, a soltura); o fim
    # emenda no comeco. Braco do arco em eixos do MUNDO: aponta o alvo mesmo com o tronco de lado
    "Shoot": [
        (0, READY, "io"),
        # ergue o arco a frente e leva a mao direita a corda
        (6, pose(KNEES, Spine=R(("Z", -15)), Neck=R(("Z", 12)),
                 LeftArm=A("", (0.25, -0.85, -0.45)), LeftForeArm=A("", (0.1, -0.95, -0.25), -90),
                 LeftHand=A("", (0.05, -0.97, -0.2), -90),
                 RightArm=A("Spine2", (-0.45, -0.55, -0.7)), RightForeArm=A("Spine2", (0.4, -0.8, 0.3)),
                 loc=(0, 0, -0.04)), "io"),
        # encaixa: braco do arco estendido no alvo, mao direita na corda junto ao arco, tronco de lado
        (12, pose(STEP, Spine=R(("Z", -30)), Spine1=R(("Z", -8)), Neck=R(("Z", 30)),
                  LeftArm=A("", (0.05, -1, 0.1)), LeftForeArm=A("", (0.0, -1, 0.1), -90), LeftHand=A("", (0.0, -1, 0.1), -90),
                  RightArm=A("Spine2", (-0.6, -0.75, 0.1)), RightForeArm=A("Spine2", (0.5, -0.85, 0.1)),
                  RightHand=A("Spine2", (0.5, -0.85, 0.1)), loc=(0, 0, -0.04)), "io"),
        # puxa: cotovelo direito alto para tras, mao direita na bochecha
        (18, pose(STEP, Spine=R(("Z", -35), ("X", -3)), Spine1=R(("Z", -10)), Neck=R(("Z", 36)),
                  LeftArm=A("", (0.05, -1, 0.1)), LeftForeArm=A("", (0.0, -1, 0.1), -90), LeftHand=A("", (0.0, -1, 0.1), -90),
                  RightArm=A("Spine2", (-0.9, 0.3, 0.15)), RightForeArm=A("Spine2", (0.75, -0.6, 0.2)),
                  RightHand=A("Spine2", (0.7, -0.65, 0.2)), loc=(0, 0, -0.04)), "io"),
        # mira (fim do telegraph): segura o arco tenso, um pouco mais
        (21, pose(STEP, Spine=R(("Z", -37), ("X", -4)), Spine1=R(("Z", -10)), Neck=R(("Z", 38)),
                  LeftArm=A("", (0.05, -1, 0.1)), LeftForeArm=A("", (0.0, -1, 0.1), -90), LeftHand=A("", (0.0, -1, 0.1), -90),
                  RightArm=A("Spine2", (-0.88, 0.38, 0.17)), RightForeArm=A("Spine2", (0.75, -0.6, 0.2)),
                  RightHand=A("Spine2", (0.7, -0.65, 0.2)), loc=(0, 0, -0.04)), "io"),
        # solta: a mao direita escapa para tras da orelha, o arco da um tranco a frente
        (24, pose(STEP, Spine=R(("Z", -38), ("X", -2)), Spine1=R(("Z", -10)), Neck=R(("Z", 38)),
                  LeftArm=A("", (0.05, -1, 0.14)), LeftForeArm=A("", (0.0, -1, 0.15), -90), LeftHand=A("", (0.0, -1, 0.15), -90),
                  RightArm=A("Spine2", (-0.85, 0.45, 0.2)), RightForeArm=A("Spine2", (-0.6, 0.7, 0.3)),
                  RightHand=A("Spine2", (-0.55, 0.75, 0.3)), loc=(0, 0.01, -0.04)), "out"),
        # acompanha e baixa o arco
        (29, pose(KNEES, Spine=R(("Z", -20)), Spine1=R(("Z", -5)), Neck=R(("Z", 20)),
                  LeftArm=A("", (0.2, -0.85, -0.4)), LeftForeArm=A("", (0.1, -0.95, -0.25), -90),
                  LeftHand=A("", (0.05, -0.97, -0.2), -90),
                  RightArm=A("Spine2", (-0.5, 0.2, -0.84)), RightForeArm=A("Spine2", (-0.3, -0.2, -0.93)),
                  loc=(0, 0, -0.04)), "io"),
        (36, READY, "io"),
    ],
    # capitao chama os ladroes (a View toca uma vez no CaptainCall): enche o peito -> grita com a mao direita na boca e
    # o braco esquerdo erguido acenando -> acena de novo -> volta
    "Call": [
        (0, READY, "io"),
        # antecipacao: inspira para tras, a mao direita sobe para a boca
        (6, pose(KNEES, Spine=R(("X", -8)), Spine1=R(("X", -5)), Neck=R(("X", -8)),
                 RightArm=A("Spine2", (-0.45, -0.55, -0.7)), RightForeArm=A("Spine2", (0.3, -0.7, 0.65)),
                 LeftArm=A("Spine2", (0.3, -0.45, -0.84)), LeftForeArm=A("Spine2", (0.2, -0.6, -0.77)), loc=(0, 0, -0.04)), "io"),
        # o braco esquerdo sobe pela frente (de tras para o alto direto ele passava pelo lado, em T)
        (9, pose(KNEES, Spine=R(("X", 2)), Spine1=R(("X", 2)), Neck=R(("X", -12)),
                 RightArm=A("Spine2", (-0.5, -0.6, -0.3)), RightForeArm=A("Spine2", (0.5, -0.6, 0.6)),
                 RightHand=A("Spine2", (0.55, -0.5, 0.65)),
                 LeftArm=A("Spine2", (0.3, -0.9, 0.3)), LeftForeArm=A("Spine2", (0.2, -0.8, 0.56)), loc=(0, 0, -0.04)), "io"),
        # grita: tronco a frente, mao direita em concha na boca, braco esquerdo erguido a frente
        (12, pose(KNEES, Spine=R(("X", 12)), Spine1=R(("X", 8)), Neck=R(("X", -16)),
                  RightArm=A("Spine2", (-0.5, -0.6, -0.1)), RightForeArm=A("Spine2", (0.55, -0.6, 0.55)),
                  RightHand=A("Spine2", (0.6, -0.5, 0.6)),
                  LeftArm=A("Spine2", (0.35, -0.45, 0.82)), LeftForeArm=A("Spine2", (0.1, -0.35, 0.93)), loc=(0, 0, -0.04)), "out"),
        # acena: o antebraco esquerdo abre para fora
        (18, pose(KNEES, Spine=R(("X", 12)), Spine1=R(("X", 8)), Neck=R(("X", -16)),
                  RightArm=A("Spine2", (-0.5, -0.6, -0.1)), RightForeArm=A("Spine2", (0.55, -0.6, 0.55)),
                  RightHand=A("Spine2", (0.6, -0.5, 0.6)),
                  LeftArm=A("Spine2", (0.4, -0.4, 0.82)), LeftForeArm=A("Spine2", (0.55, -0.2, 0.81)), loc=(0, 0, -0.04)), "io"),
        # acena de volta para dentro
        (24, pose(KNEES, Spine=R(("X", 10)), Spine1=R(("X", 6)), Neck=R(("X", -14)),
                  RightArm=A("Spine2", (-0.5, -0.6, -0.15)), RightForeArm=A("Spine2", (0.5, -0.6, 0.6)),
                  RightHand=A("Spine2", (0.55, -0.5, 0.65)),
                  LeftArm=A("Spine2", (0.35, -0.45, 0.82)), LeftForeArm=A("Spine2", (0.0, -0.45, 0.89)), loc=(0, 0, -0.04)), "io"),
        # desce o braco pela frente
        (27, pose(KNEES, Spine=R(("X", 4)), Spine1=R(("X", 2)), Neck=R(("X", -6)),
                  RightArm=A("Spine2", (-0.4, -0.45, -0.8)), RightForeArm=A("Spine2", (-0.2, -0.65, -0.73)),
                  LeftArm=A("Spine2", (0.3, -0.8, -0.5)), LeftForeArm=A("Spine2", (0.2, -0.85, -0.48)), loc=(0, 0, -0.04)), "io"),
        (30, READY, "io"),
    ],
    # capitao: soco direto (CaptainHit da View), 12 quadros. A View estica [0, 6) pelo telegraph de 0,5 s e toca [6, 12)
    # em 0,15 s (ImpactFrac do capitao 0,5 x 12 = quadro 6 = 14,5 -> chave 15); o punho armado (5-12) fecha o telegraph
    "Punch": [
        (0, READY, "io"),
        # o antebraco passa pela frente (de baixo direto para o alto ele passava pelo lado, em T)
        (2, pose(KNEES, Spine=R(("Z", -8)), Neck=R(("Z", 6)),
                 RightArm=A("Spine2", (-0.35, 0.2, -0.9)), RightForeArm=A("Spine2", (-0.1, -0.95, -0.2)),
                 LeftArm=A("Spine2", (0.3, -0.4, -0.86)), LeftForeArm=A("Spine2", (0.15, -0.8, -0.2)), loc=(0, 0.01, -0.05)), "io"),
        # arma: afunda, tronco torcido para a direita, punho direito recolhido junto ao ombro, braco esquerdo em guarda
        (5, pose(KNEES, Spine=R(("Z", -18)), Spine1=R(("Z", -8)), Neck=R(("Z", 14)),
                 RightArm=A("Spine2", (-0.45, 0.5, -0.74)), RightForeArm=A("Spine2", (0.1, -0.55, 0.83)),
                 LeftArm=A("Spine2", (0.35, -0.7, -0.62)), LeftForeArm=A("Spine2", (0.0, -0.75, 0.66)), loc=(0, 0.02, -0.06)), "io"),
        # segura armado (fim do telegraph)
        (12, pose(KNEES, Spine=R(("Z", -22), ("X", -3)), Spine1=R(("Z", -10)), Neck=R(("Z", 18)),
                  RightArm=A("Spine2", (-0.42, 0.58, -0.7)), RightForeArm=A("Spine2", (0.1, -0.5, 0.86)),
                  LeftArm=A("Spine2", (0.35, -0.72, -0.6)), LeftForeArm=A("Spine2", (0.0, -0.75, 0.66)), loc=(0, 0.03, -0.06)), "io"),
        # soco: braco direito esticado a frente, passo, tronco gira para a esquerda, o braco esquerdo volta
        (15, pose(STEP, Spine=R(("Z", 20), ("X", 8)), Spine1=R(("Z", 10), ("X", 6)), Neck=R(("Z", -14)),
                  RightArm=A("Spine2", (-0.12, -1, 0.08)), RightForeArm=A("Spine2", (-0.05, -1, 0.06)),
                  RightHand=A("Spine2", (-0.05, -1, 0.04)),
                  LeftArm=A("Spine2", (0.4, 0.35, -0.85)), LeftForeArm=A("Spine2", (0.2, -0.4, -0.9)), loc=(0, -0.08, -0.06)), "out"),
        # passa do ponto
        (19, pose(STEP, Spine=R(("Z", 26), ("X", 12)), Spine1=R(("Z", 12), ("X", 8)), Neck=R(("Z", -18), ("X", -6)),
                  RightArm=A("Spine2", (-0.05, -1, 0.0)), RightForeArm=A("Spine2", (0.05, -1, -0.02)),
                  RightHand=A("Spine2", (0.05, -1, -0.04)),
                  LeftArm=A("Spine2", (0.42, 0.4, -0.82)), LeftForeArm=A("Spine2", (0.25, -0.3, -0.92)), loc=(0, -0.1, -0.07)), "io"),
        # recolhe
        (24, pose(KNEES, Spine=R(("Z", 6), ("X", 3)), Neck=R(("Z", -4)),
                  RightArm=A("Spine2", (-0.3, -0.5, -0.82)), RightForeArm=A("Spine2", (-0.2, -0.7, -0.68)),
                  LeftArm=A("Spine2", (0.3, 0.0, -0.95)), LeftForeArm=A("Spine2", (0.2, -0.4, -0.9)), loc=(0, -0.03, -0.05)), "io"),
        (29, READY, "io"),
    ],
}


# ---- Lote 4 (regiao 2, 2026-10-11) ------------------------------------------------------------------------------
# toupeira e rainha: curvadas, garras a frente e para cima
MOLE = pose(KNEES, Spine=R(("X", 12)), Neck=R(("X", -10)),
            **both({"LeftArm": A("Spine2", (0.45, -0.55, -0.7)), "LeftForeArm": A("Spine2", (0.25, -0.85, 0.45)),
                    "LeftHand": A("Spine2", (0.2, -0.6, 0.75))}), loc=(0, 0, -0.02))
# sentado no chao, pernas a frente, encostado para tras (o pe no chao baixa o quadril ate sentar)
SIT = legs((0.12, -0.95, -0.28), (0.1, -0.85, -0.52), (-0.1, -0.97, -0.22), (-0.12, -0.9, -0.42))
SLEEP = pose(SIT, Spine=R(("X", -18)), Spine1=R(("X", -4)), Neck=R(("X", 28), ("Z", 10)), Head=R(("X", 8)),
             **both({"LeftArm": A("Spine2", (0.35, -0.35, -0.87)), "LeftForeArm": A("Spine2", (0.1, -0.9, -0.4))}))
# joelhos para cima (pe perto do corpo) e bracos apoiados a frente: com as pernas esticadas para a camera e os bracos
# abertos o golem sentado passava da borda da celula de 448 e lia como T
GOLEM_SIT = legs((0.2, -0.95, -0.2), (0.15, -0.55, -0.82), (-0.2, -0.95, -0.2), (-0.15, -0.55, -0.82))
GOLEM_LIMP = both({"LeftArm": A("Spine2", (0.4, -0.3, -0.87)), "LeftForeArm": A("Spine2", (0.3, -0.6, -0.74))})
# corrida do golem (Charge): tronco inclinado, braco oposto a perna
RUN_LEAN = dict(Spine=R(("X", 25)), Spine1=R(("X", 10)), Neck=R(("X", -25)))
CONTACT_L = legs((0.12, -0.6, -0.79), (0.08, -0.15, -0.99), (-0.12, 0.5, -0.87), (-0.08, 0.85, -0.53))
PASS_L = legs((0.12, -0.05, -0.99), (0.08, 0.15, -0.99), (-0.12, -0.4, -0.92), (-0.08, 0.45, -0.89))
CONTACT_R = legs((0.12, 0.5, -0.87), (0.08, 0.85, -0.53), (-0.12, -0.6, -0.79), (-0.08, -0.15, -0.99))
PASS_R = legs((0.12, -0.4, -0.92), (0.08, 0.45, -0.89), (-0.12, -0.05, -0.99), (-0.08, 0.15, -0.99))
FIST_FWD_R = {"RightArm": A("Spine2", (-0.15, -0.75, -0.6)), "RightForeArm": A("Spine2", (-0.1, -0.9, 0.1))}
FIST_BACK_L = {"LeftArm": A("Spine2", (0.15, 0.6, -0.74)), "LeftForeArm": A("Spine2", (0.12, 0.2, -0.95))}
ARMS_HANG = both({"LeftArm": A("Spine2", (0.15, 0.1, -0.98)), "LeftForeArm": A("Spine2", (0.12, -0.1, -0.98))})

CLIPS.update({
    # goblin mineiro dormindo, sentado e encostado (laco: 60 = 0); respira e a cabeca balanca
    "Sleep": [(0, SLEEP, "io"),
              (30, pose(SIT, Spine=R(("X", -18)), Spine1=R(("X", -8)), Neck=R(("X", 24), ("Z", 8)), Head=R(("X", 6)),
                        **both({"LeftArm": A("Spine2", (0.35, -0.35, -0.87)), "LeftForeArm": A("Spine2", (0.1, -0.9, -0.35))})), "io"),
              (60, SLEEP, "io")],
    # acorda de susto em 0,4 s: endireita com os bracos a frente -> agacha para levantar -> pronto
    "Wake": [(0, SLEEP, "io"),
             (3, pose(legs((0.15, -0.7, -0.6), (0.1, 0.2, -0.98), (-0.15, -0.7, -0.6), (-0.1, 0.2, -0.98)),
                      Spine=R(("X", 2)), Neck=R(("X", -12)),
                      **both({"LeftArm": A("Spine2", (0.35, -0.75, 0.4)), "LeftForeArm": A("Spine2", (0.2, -0.6, 0.7))})), "out"),
             (7, pose(SQUAT, Spine=R(("X", 25)), Spine1=R(("X", 10)), Neck=R(("X", -20)),
                      **both({"LeftArm": A("Spine2", (0.3, -0.6, -0.74)), "LeftForeArm": A("Spine2", (0.2, -0.8, -0.3))})), "io"),
             (12, READY, "io")],
    # toupeira/rainha sobem do chao em 0,3 s (sem trava do pe: o corpo comeca enterrado; a View recorta pelo chao)
    "Emerge": [(0, pose(KNEES, Spine=R(("X", 20)), loc=(0, 0, -1.75),
                        **both({"LeftArm": A("Spine2", (0.25, -0.2, 0.95)), "LeftForeArm": A("Spine2", (0.1, -0.3, 0.95))})), "io"),
               (3, pose(KNEES, Spine=R(("X", 8)), loc=(0, 0, -1.0),
                        **both({"LeftArm": A("Spine2", (0.45, -0.2, 0.87)), "LeftForeArm": A("Spine2", (0.3, -0.3, 0.9))})), "io"),
               (6, pose(STAND, Spine=R(("X", -6)), Neck=R(("X", -8)), loc=(0, 0, 0.12),
                        **both({"LeftArm": A("Spine2", (0.55, -0.35, 0.76)), "LeftForeArm": A("Spine2", (0.35, -0.45, 0.82))})), "out"),
               (9, MOLE, "io")],
    # o inverso: pula e mergulha de cabeca, as garras primeiro
    "Burrow": [(0, MOLE, "io"),
               (3, pose(STAND, Spine=R(("X", 25)), Neck=R(("X", 10)), loc=(0, 0, 0.1),
                        **both({"LeftArm": A("Spine2", (0.15, -0.6, 0.78)), "LeftForeArm": A("Spine2", (0.05, -0.5, 0.86))})), "out"),
               (6, pose(KNEES, Spine=R(("X", 60)), Spine1=R(("X", 10)), Neck=R(("X", 10)), loc=(0, -0.1, -0.9),
                        **both({"LeftArm": A("Spine2", (0.15, -0.95, 0.2)), "LeftForeArm": A("Spine2", (0.05, -0.97, 0.2))})), "io"),
               (9, pose(KNEES, Spine=R(("X", 70)), Spine1=R(("X", 10)), Neck=R(("X", 10)), loc=(0, -0.15, -1.85),
                        **both({"LeftArm": A("Spine2", (0.15, -0.95, 0.2)), "LeftForeArm": A("Spine2", (0.05, -0.97, 0.2))})), "io")],
    # rainha: patada com a garra direita, 12 quadros; impacto no quadro 6 (ImpactFrac 0,5 x 12 = 14,5 -> chave 15)
    "Swipe": [(0, MOLE, "io"),
              (2, pose(KNEES, Spine=R(("X", 6), ("Z", -6)), Neck=R(("X", -8)),
                       RightArm=A("Spine2", (-0.6, -0.3, -0.74)), RightForeArm=A("Spine2", (-0.45, -0.6, 0.66)),
                       LeftArm=A("Spine2", (0.45, -0.55, -0.7)), LeftForeArm=A("Spine2", (0.25, -0.85, 0.45)), loc=(0, 0, -0.03)), "io"),
              (5, pose(KNEES, Spine=R(("X", -8), ("Z", -15)), Spine1=R(("Z", -6)), Neck=R(("X", -6), ("Z", 12)),
                       RightArm=A("Spine2", (-0.7, 0.3, 0.65)), RightForeArm=A("Spine2", (-0.3, 0.2, 0.93)),
                       LeftArm=A("Spine2", (0.45, -0.7, -0.55)), LeftForeArm=A("Spine2", (0.2, -0.9, 0.3)), loc=(0, 0.02, -0.04)), "io"),
              (12, pose(KNEES, Spine=R(("X", -10), ("Z", -20)), Spine1=R(("Z", -8)), Neck=R(("X", -6), ("Z", 16)),
                        RightArm=A("Spine2", (-0.68, 0.38, 0.63)), RightForeArm=A("Spine2", (-0.3, 0.28, 0.91)),
                        LeftArm=A("Spine2", (0.45, -0.7, -0.55)), LeftForeArm=A("Spine2", (0.2, -0.9, 0.3)), loc=(0, 0.03, -0.05)), "io"),
              (15, pose(STEP, Spine=R(("X", 15), ("Z", 22)), Spine1=R(("Z", 10)), Neck=R(("X", -14), ("Z", -14)),
                        RightArm=A("Spine2", (0.2, -0.9, -0.4)), RightForeArm=A("Spine2", (0.5, -0.75, -0.4)),
                        LeftArm=A("Spine2", (0.45, 0.3, -0.84)), LeftForeArm=A("Spine2", (0.3, -0.2, -0.93)), loc=(0, -0.06, -0.05)), "out"),
              (19, pose(STEP, Spine=R(("X", 18), ("Z", 28)), Spine1=R(("Z", 12)), Neck=R(("X", -16), ("Z", -18)),
                        RightArm=A("Spine2", (0.45, -0.6, -0.65)), RightForeArm=A("Spine2", (0.6, -0.3, -0.75)),
                        LeftArm=A("Spine2", (0.45, 0.35, -0.82)), LeftForeArm=A("Spine2", (0.3, -0.15, -0.94)), loc=(0, -0.08, -0.06)), "io"),
              (24, pose(KNEES, Spine=R(("X", 8), ("Z", 6)), Neck=R(("X", -10)),
                        RightArm=A("Spine2", (-0.2, -0.6, -0.78)), RightForeArm=A("Spine2", (0.0, -0.9, 0.2)),
                        LeftArm=A("Spine2", (0.45, -0.4, -0.8)), LeftForeArm=A("Spine2", (0.25, -0.85, 0.4)), loc=(0, -0.03, -0.03)), "io"),
              (29, MOLE, "io")],
    # golem investe correndo inclinado (laco de 2 passos: 20 = 0; no lugar, a Sim move)
    "Charge": [(0, pose(CONTACT_L, FIST_FWD_R, FIST_BACK_L, **RUN_LEAN), "io"),
               (5, pose(PASS_L, ARMS_HANG, **RUN_LEAN), "io"),
               (10, pose(CONTACT_R, mirror(FIST_FWD_R), mirror(FIST_BACK_L), **RUN_LEAN), "io"),
               (15, pose(PASS_R, ARMS_HANG, **RUN_LEAN), "io"),
               (20, pose(CONTACT_L, FIST_FWD_R, FIST_BACK_L, **RUN_LEAN), "io")],
    # golem atordoado, sentado, a cabeca rodando (laco: 40 = 0)
    "Stun": [(0, pose(GOLEM_SIT, GOLEM_LIMP, Spine=R(("X", 10), ("Z", 5)), Spine1=R(("X", 8)), Neck=R(("X", 8), ("Z", 12))), "io"),
             (10, pose(GOLEM_SIT, GOLEM_LIMP, Spine=R(("X", 12)), Spine1=R(("X", 8)), Neck=R(("X", 18))), "io"),
             (20, pose(GOLEM_SIT, GOLEM_LIMP, Spine=R(("X", 10), ("Z", -5)), Spine1=R(("X", 8)), Neck=R(("X", 8), ("Z", -12))), "io"),
             (30, pose(GOLEM_SIT, GOLEM_LIMP, Spine=R(("X", 6)), Spine1=R(("X", 6)), Neck=R(("X", -2))), "io"),
             (40, pose(GOLEM_SIT, GOLEM_LIMP, Spine=R(("X", 10), ("Z", 5)), Spine1=R(("X", 8)), Neck=R(("X", 8), ("Z", 12))), "io")],
    # golem desmorona (golem_fim/Dying): cambaleia para tras -> joelhos cedem -> cai a frente nos punhos -> de bruços
    "Collapse": [(0, BRUTE, "io"),
                 (8, pose(KNEES, Spine=R(("X", -12)), Spine1=R(("X", -8)), Neck=R(("X", -14)),
                          **both({"LeftArm": A("Spine2", (0.3, -0.75, 0.45)), "LeftForeArm": A("Spine2", (0.2, -0.6, 0.77))})), "out"),
                 (16, pose(legs((0.15, -0.75, -0.65), (0.1, 0.95, -0.3), (-0.15, -0.75, -0.65), (-0.1, 0.95, -0.3)),
                           Spine=R(("X", 15)), Spine1=R(("X", 8)), Neck=R(("X", -6)),
                           **both({"LeftArm": A("Spine2", (0.15, -0.35, -0.92)), "LeftForeArm": A("Spine2", (0.1, -0.5, -0.86))})), "io"),
                 (26, pose(legs((0.15, -0.3, -0.94), (0.1, 0.97, -0.2), (-0.15, -0.3, -0.94), (-0.1, 0.97, -0.2)),
                           Spine=R(("X", 55)), Spine1=R(("X", 12)), Neck=R(("X", -20)),
                           **both({"LeftArm": A("", (0.35, -0.3, -0.89)), "LeftForeArm": A("", (0.3, -0.35, -0.89))})), "io"),
                 (36, pose(legs((0.15, 0.6, -0.79), (0.1, 0.98, -0.15), (-0.15, 0.6, -0.79), (-0.1, 0.98, -0.15)),
                           Spine=R(("X", 82)), Spine1=R(("X", 6)), Neck=R(("X", -10)),
                           **both({"LeftArm": A("", (0.75, -0.55, -0.35)), "LeftForeArm": A("", (0.6, -0.75, -0.25))})), "out"),
                 (45, pose(legs((0.15, 0.9, -0.42), (0.1, 0.99, -0.1), (-0.15, 0.9, -0.42), (-0.1, 0.99, -0.1)),
                           Spine=R(("X", 86)), Spine1=R(("X", 4)), Neck=R(("X", -6), ("Z", 10)),
                           **both({"LeftArm": A("", (0.8, -0.5, -0.33)), "LeftForeArm": A("", (0.65, -0.72, -0.25))})), "io")],
})
# Throw do mineiro: as mesmas poses do Throw do courier, retimadas para soltar no quadro 7 de 12 (ImpactFrac 0,6 x 12;
# chave 17) e com a carga segurada nas chaves 10-15 (fim do telegraph no quadro 6 = 14,5). A picareta vai na mao direita
_T = {f: (p, e) for f, p, e in CLIPS["Throw"]}
CLIPS["PickThrow"] = [(0,) + _T[0], (5,) + _T[4], (10,) + _T[8], (15, _T[8][0], "io"), (17,) + _T[10], (21,) + _T[14],
                      (25,) + _T[17], (29,) + _T[20]]
# clipes sem trava do pe no chao (o corpo entra e sai do chao)
FREE = {"Emerge", "Burrow"}


# ---- Lote 5 (regiao 3, 2026-10-11) ------------------------------------------------------------------------------
# mumia: curvada, bracos esticados a frente (a pose classica); gargula agachada no pedestal (maos no chao a frente)
MUMMY = pose(KNEES, Spine=R(("X", 12)), Neck=R(("X", -8)),
             **both({"LeftArm": A("Spine2", (0.25, -0.95, -0.15)), "LeftForeArm": A("Spine2", (0.2, -0.97, 0.0))}))
GARG_CROUCH = pose(SQUAT, Spine=R(("X", 30)), Spine1=R(("X", 10)), Neck=R(("X", -30)),
                   **both({"LeftArm": A("", (0.25, -0.5, -0.83)), "LeftForeArm": A("", (0.15, -0.35, -0.93))}))
# necromante: antebraco direito a frente com a mao a frente e a palma para dentro (giro +90): o cajado preso a mao
# (--attach) fica em pe; com o braco caido do Breathing Idle ele ficaria deitado, por isso o Idle e daqui
NECRO_STAFF = {"RightArm": A("Spine2", (-0.25, -0.05, -0.97)), "RightForeArm": A("Spine2", (-0.05, -1, 0.05)),
               "RightHand": A("", (0, -1, 0), 90)}
NECRO_LEFT = {"LeftArm": A("Spine2", (0.25, -0.05, -0.97)), "LeftForeArm": A("Spine2", (0.15, -0.35, -0.92))}
NECRO_IDLE = pose(STAND, NECRO_STAFF, NECRO_LEFT, Spine=R(("X", 8)), Neck=R(("X", -6)))
SKEL_HEAP = pose(legs((0.45, -0.6, -0.66), (0.3, 0.75, -0.6), (-0.45, -0.6, -0.66), (-0.3, 0.75, -0.6)),
                 Spine=R(("X", 70)), Spine1=R(("X", 12)), Neck=R(("X", 30), ("Z", 15)),
                 **both({"LeftArm": A("", (0.6, -0.3, -0.74)), "LeftForeArm": A("", (0.5, -0.5, -0.71))}))

CLIPS.update({
    # esqueleto: clava de cima para baixo, 12 quadros; impacto no quadro 6 (ImpactFrac 0,5 x 12 = 14,5 -> chave 15)
    "SkelAttack": [
        (0, READY, "io"),
        # o braco da clava sobe pela frente (de baixo direto para tras-alto ele passava pelo lado, em T)
        (2, pose(KNEES, Spine=R(("Z", -6)), RightArm=A("Spine2", (-0.25, -0.75, 0.6)), RightForeArm=A("Spine2", (-0.15, -0.5, 0.85)),
                 LeftArm=A("Spine2", (0.3, -0.4, -0.87)), LeftForeArm=A("Spine2", (0.15, -0.7, -0.7))), "io"),
        (5, pose(KNEES, Spine=R(("Z", -14), ("X", -6)), Spine1=R(("Z", -6)), Neck=R(("Z", 10)),
                 RightArm=A("Spine2", (-0.35, 0.3, 0.88)), RightForeArm=A("Spine2", (-0.1, 0.6, 0.8)),
                 LeftArm=A("Spine2", (0.35, -0.6, -0.72)), LeftForeArm=A("Spine2", (0.15, -0.9, 0.2))), "io"),
        (12, pose(KNEES, Spine=R(("Z", -18), ("X", -9)), Spine1=R(("Z", -8), ("X", -4)), Neck=R(("Z", 12), ("X", 6)),
                  RightArm=A("Spine2", (-0.3, 0.38, 0.87)), RightForeArm=A("Spine2", (-0.1, 0.7, 0.71)),
                  LeftArm=A("Spine2", (0.35, -0.65, -0.67)), LeftForeArm=A("Spine2", (0.15, -0.9, 0.25))), "io"),
        (15, pose(STEP, Spine=R(("Z", 14), ("X", 20)), Spine1=R(("Z", 8), ("X", 12)), Neck=R(("Z", -8), ("X", -18)),
                  RightArm=A("Spine2", (-0.1, -0.85, -0.5)), RightForeArm=A("Spine2", (-0.05, -0.7, -0.7)),
                  LeftArm=A("Spine2", (0.4, 0.35, -0.85)), LeftForeArm=A("Spine2", (0.25, 0.1, -0.96))), "out"),
        (19, pose(STEP, Spine=R(("Z", 18), ("X", 24)), Spine1=R(("Z", 8), ("X", 14)), Neck=R(("Z", -8), ("X", -20)),
                  RightArm=A("Spine2", (-0.05, -0.7, -0.7)), RightForeArm=A("Spine2", (0.05, -0.4, -0.92)),
                  LeftArm=A("Spine2", (0.4, 0.4, -0.82)), LeftForeArm=A("Spine2", (0.25, 0.15, -0.95))), "io"),
        (24, pose(KNEES, Spine=R(("Z", 5), ("X", 6)), Neck=R(("X", -4)),
                  RightArm=A("Spine2", (-0.3, -0.4, -0.86)), RightForeArm=A("Spine2", (-0.15, -0.65, -0.74)),
                  LeftArm=A("Spine2", (0.3, 0.0, -0.95)), LeftForeArm=A("Spine2", (0.2, -0.3, -0.93))), "io"),
        (29, READY, "io"),
    ],
    # esqueleto desmonta em pilha em 0,3 s (o ultimo quadro amostrado ja e a pilha: chave repetida em 7 e 9)
    "BoneCollapse": [(0, READY, "io"),
                     (2, pose(KNEES, Spine=R(("X", -8)), Neck=R(("X", -14)),
                              **both({"LeftArm": A("Spine2", (0.35, -0.6, -0.72)), "LeftForeArm": A("Spine2", (0.25, -0.7, -0.67))})), "out"),
                     (5, pose(SQUAT, Spine=R(("X", 40)), Spine1=R(("X", 10)), Neck=R(("X", 20)),
                              **both({"LeftArm": A("", (0.5, -0.4, -0.77)), "LeftForeArm": A("", (0.45, -0.5, -0.74))})), "io"),
                     (7, SKEL_HEAP, "io"), (9, SKEL_HEAP, "io")],
    # o inverso (a View estica para 4 s): pilha -> agacha -> de pe
    "BoneRise": [(0, SKEL_HEAP, "io"), (2, SKEL_HEAP, "io"),
                 (4, pose(SQUAT, Spine=R(("X", 40)), Spine1=R(("X", 10)), Neck=R(("X", 20)),
                          **both({"LeftArm": A("", (0.5, -0.4, -0.77)), "LeftForeArm": A("", (0.45, -0.5, -0.74))})), "io"),
                 (7, READY, "out"), (9, READY, "io")],
    # mumia agarra: 12 quadros; impacto no quadro 7 (ImpactFrac 0,6 x 12 = 16,9 -> chave 17)
    "MummyGrab": [
        (0, MUMMY, "io"),
        (6, pose(KNEES, Spine=R(("X", -6)), Neck=R(("X", -12)), loc=(0, 0.04, 0),
                 **both({"LeftArm": A("Spine2", (0.4, -0.6, 0.6)), "LeftForeArm": A("Spine2", (0.3, -0.5, 0.81))})), "io"),
        (11, pose(KNEES, Spine=R(("X", -10)), Neck=R(("X", -14)), loc=(0, 0.06, 0),
                  **both({"LeftArm": A("Spine2", (0.45, -0.5, 0.74)), "LeftForeArm": A("Spine2", (0.3, -0.4, 0.87))})), "io"),
        (15, pose(KNEES, Spine=R(("X", -11)), Neck=R(("X", -15)), loc=(0, 0.06, 0),
                  **both({"LeftArm": A("Spine2", (0.45, -0.52, 0.72)), "LeftForeArm": A("Spine2", (0.3, -0.42, 0.86))})), "io"),
        (17, pose(LUNGE, Spine=R(("X", 22)), Spine1=R(("X", 10)), Neck=R(("X", -20)), loc=(0, -0.12, 0),
                  **both({"LeftArm": A("Spine2", (0.15, -0.97, 0.1)), "LeftForeArm": A("Spine2", (-0.1, -0.97, 0.15))})), "out"),
        (21, pose(LUNGE, Spine=R(("X", 24)), Spine1=R(("X", 12)), Neck=R(("X", -20)), loc=(0, -0.13, 0),
                  **both({"LeftArm": A("Spine2", (0.12, -0.97, 0.15)), "LeftForeArm": A("Spine2", (-0.2, -0.95, 0.2))})), "io"),
        (25, pose(STEP, Spine=R(("X", 16)), Neck=R(("X", -12)), loc=(0, -0.08, 0),
                  **both({"LeftArm": A("Spine2", (0.2, -0.95, -0.1)), "LeftForeArm": A("Spine2", (0.1, -0.97, 0.0))})), "io"),
        (29, MUMMY, "io"),
    ],
    # mumia segura apertando (laco: 20 = 0)
    "MummyHold": [(0, pose(STEP, Spine=R(("X", 16)), Neck=R(("X", -14)),
                            **both({"LeftArm": A("Spine2", (0.15, -0.97, 0.1)), "LeftForeArm": A("Spine2", (-0.25, -0.95, 0.2))})), "io"),
                  (10, pose(STEP, Spine=R(("X", 22)), Spine1=R(("X", 6)), Neck=R(("X", -10)),
                            **both({"LeftArm": A("Spine2", (0.1, -0.97, 0.2)), "LeftForeArm": A("Spine2", (-0.4, -0.88, 0.25))})), "io"),
                  (20, pose(STEP, Spine=R(("X", 16)), Neck=R(("X", -14)),
                            **both({"LeftArm": A("Spine2", (0.15, -0.97, 0.1)), "LeftForeArm": A("Spine2", (-0.25, -0.95, 0.2))})), "io")],
    # gargula de pedra no pedestal: 1 quadro agachado (o preset pede 1 quadro)
    "GargStatue": [(0, GARG_CROUCH, "io"), (9, GARG_CROUCH, "io")],
    # acorda (telegraph de 0,9 s): agachada -> levanta com os bracos em V (asas) e a cabeca para tras -> inclina para mergulhar
    "GargWake": [(0, GARG_CROUCH, "io"),
                 (4, pose(SQUAT, Spine=R(("X", 15)), Neck=R(("X", -26)),
                          **both({"LeftArm": A("Spine2", (0.3, -0.8, 0.5)), "LeftForeArm": A("Spine2", (0.2, -0.6, 0.77))})), "io"),
                 (10, pose(KNEES, Spine=R(("X", -10)), Spine1=R(("X", -6)), Neck=R(("X", -24)),
                           **both({"LeftArm": A("Spine2", (0.38, 0.05, 0.92)), "LeftForeArm": A("Spine2", (0.25, 0.0, 0.97))})), "out"),
                 (18, pose(KNEES, Spine=R(("X", -12)), Spine1=R(("X", -8)), Neck=R(("X", -28)),
                           **both({"LeftArm": A("Spine2", (0.4, 0.1, 0.91)), "LeftForeArm": A("Spine2", (0.28, 0.05, 0.96))})), "io"),
                 (23, pose(KNEES, Spine=R(("X", 10)), Spine1=R(("X", 4)), Neck=R(("X", -30)),
                           **both({"LeftArm": A("Spine2", (0.3, 0.75, 0.3)), "LeftForeArm": A("Spine2", (0.25, 0.8, 0.2))})), "io"),
                 (27, pose(SQUAT, Spine=R(("X", 35)), Spine1=R(("X", 12)), Neck=R(("X", -35)),
                           **both({"LeftArm": A("", (0.2, 0.6, -0.77)), "LeftForeArm": A("", (0.15, 0.7, -0.7))})), "io")],
    # mergulho: corpo na horizontal, pernas para tras, garras a frente (sem trava do pe: voa a ~0,6 m do chao), 6 quadros
    "GargDive": [(0, pose(legs((0.1, 0.95, -0.3), (0.08, 0.98, 0.1), (-0.1, 0.95, -0.3), (-0.08, 0.98, 0.1)),
                          Spine=R(("X", 78)), Spine1=R(("X", 6)), Neck=R(("X", -55)), loc=(0, 0, -0.45),
                          **both({"LeftArm": A("", (0.25, -0.9, -0.35)), "LeftForeArm": A("", (0.2, -0.95, -0.25))})), "io"),
                 (7, pose(legs((0.1, 0.95, -0.25), (0.08, 0.98, 0.15), (-0.1, 0.95, -0.25), (-0.08, 0.98, 0.15)),
                          Spine=R(("X", 82)), Spine1=R(("X", 6)), Neck=R(("X", -58)), loc=(0, 0, -0.5),
                          **both({"LeftArm": A("", (0.3, -0.85, -0.42)), "LeftForeArm": A("", (0.25, -0.9, -0.36))})), "io"),
                 (15, pose(legs((0.1, 0.95, -0.3), (0.08, 0.98, 0.1), (-0.1, 0.95, -0.3), (-0.08, 0.98, 0.1)),
                           Spine=R(("X", 78)), Spine1=R(("X", 6)), Neck=R(("X", -55)), loc=(0, 0, -0.45),
                           **both({"LeftArm": A("", (0.25, -0.9, -0.35)), "LeftForeArm": A("", (0.2, -0.95, -0.25))})), "io")],
    # pousada no fim da faixa, agachada ofegando (laco: 24 = 0)
    "GargPerch": [(0, GARG_CROUCH, "io"),
                  (12, pose(SQUAT, Spine=R(("X", 24)), Spine1=R(("X", 6)), Neck=R(("X", -24)),
                            **both({"LeftArm": A("", (0.25, -0.5, -0.83)), "LeftForeArm": A("", (0.15, -0.35, -0.93))})), "io"),
                  (24, GARG_CROUCH, "io")],
    # necromante parado com o cajado em pe (laco: 60 = 0)
    "NecroIdle": [(0, NECRO_IDLE, "io"),
                  (30, pose(STAND, NECRO_STAFF, NECRO_LEFT, Spine=R(("X", 6)), Spine1=R(("X", -3)), Neck=R(("X", -8))), "io"),
                  (60, NECRO_IDLE, "io")],
    # raio: ergue o cajado e carrega -> aponta o cajado ao alvo (mao fletida para baixo: o cranio vai a frente);
    # 12 quadros, impacto no quadro 10 (ImpactFrac 0,8 x 12 = 24,2 -> chave 24)
    "NecroCast": [(0, NECRO_IDLE, "io"),
                  (8, pose(STAND, NECRO_LEFT, Spine=R(("X", -6)), Spine1=R(("X", -4)), Neck=R(("X", -10)),
                           RightArm=A("Spine2", (-0.3, -0.5, 0.8)), RightForeArm=A("Spine2", (-0.1, -0.7, 0.7)),
                           RightHand=A("", (0, -1, 0.05), 90)), "io"),
                  (16, pose(STAND, Spine=R(("X", -9)), Spine1=R(("X", -5)), Neck=R(("X", -12)),
                            RightArm=A("Spine2", (-0.3, -0.45, 0.84)), RightForeArm=A("Spine2", (-0.1, -0.65, 0.75)),
                            RightHand=A("", (0, -1, 0.05), 90),
                            LeftArm=A("Spine2", (0.2, -0.95, 0.1)), LeftForeArm=A("Spine2", (0.1, -0.95, 0.3))), "io"),
                  (22, pose(STAND, Spine=R(("X", -10)), Spine1=R(("X", -5)), Neck=R(("X", -12)),
                            RightArm=A("Spine2", (-0.3, -0.42, 0.86)), RightForeArm=A("Spine2", (-0.1, -0.62, 0.78)),
                            RightHand=A("", (0, -1, 0.05), 90),
                            LeftArm=A("Spine2", (0.2, -0.96, 0.12)), LeftForeArm=A("Spine2", (0.1, -0.95, 0.32))), "io"),
                  (24, pose(STEP, Spine=R(("X", 14)), Spine1=R(("X", 8)), Neck=R(("X", -16)),
                            RightArm=A("Spine2", (-0.1, -0.95, 0.3)), RightForeArm=A("Spine2", (0.0, -0.95, 0.3)),
                            RightHand=A("", (0, -0.6, -0.8), 90),
                            LeftArm=A("Spine2", (0.35, 0.2, -0.92)), LeftForeArm=A("Spine2", (0.25, -0.1, -0.96))), "out"),
                  (27, pose(STEP, Spine=R(("X", 16)), Spine1=R(("X", 8)), Neck=R(("X", -16)),
                            RightArm=A("Spine2", (-0.1, -0.95, 0.28)), RightForeArm=A("Spine2", (0.0, -0.96, 0.28)),
                            RightHand=A("", (0, -0.62, -0.78), 90),
                            LeftArm=A("Spine2", (0.35, 0.22, -0.91)), LeftForeArm=A("Spine2", (0.25, -0.1, -0.96))), "io"),
                  (29, pose(STEP, Spine=R(("X", 12)), Spine1=R(("X", 6)), Neck=R(("X", -12)),
                            RightArm=A("Spine2", (-0.15, -0.9, 0.2)), RightForeArm=A("Spine2", (0.0, -0.97, 0.2)),
                            RightHand=A("", (0, -0.7, -0.7), 90),
                            LeftArm=A("Spine2", (0.3, 0.1, -0.95)), LeftForeArm=A("Spine2", (0.2, -0.2, -0.96))), "io")],
    # erguer os esqueletos: abaixa -> ergue o cajado acima da cabeca com as duas maos -> segura tremendo -> volta
    "NecroSummon": [(0, NECRO_IDLE, "io"),
                    (8, pose(KNEES, NECRO_STAFF, Spine=R(("X", 18)), Neck=R(("X", 10)),
                             LeftArm=A("Spine2", (0.3, -0.5, -0.81)), LeftForeArm=A("Spine2", (0.15, -0.8, -0.58))), "io"),
                    # os bracos sobem pela frente (de baixo direto para o alto eles passavam pelos lados, em T)
                    (12, pose(STAND, Spine=R(("X", 4)), Neck=R(("X", -6)),
                              RightArm=A("Spine2", (-0.25, -0.8, 0.5)), RightForeArm=A("Spine2", (-0.05, -0.6, 0.8)),
                              RightHand=A("", (0, -0.6, 0.8), 90),
                              LeftArm=A("Spine2", (0.25, -0.8, 0.5)), LeftForeArm=A("Spine2", (0.05, -0.6, 0.8))), "io"),
                    (16, pose(STAND, Spine=R(("X", -10)), Spine1=R(("X", -6)), Neck=R(("X", -20)),
                              RightArm=A("Spine2", (-0.3, -0.15, 0.94)), RightForeArm=A("Spine2", (0.05, -0.1, 0.99)),
                              RightHand=A("", (0.05, -0.1, 0.99), 90),
                              LeftArm=A("Spine2", (0.3, -0.15, 0.94)), LeftForeArm=A("Spine2", (-0.05, -0.1, 0.99))), "out"),
                    (24, pose(STAND, Spine=R(("X", -12)), Spine1=R(("X", -6), ("Z", 3)), Neck=R(("X", -22), ("Z", -4)),
                              RightArm=A("Spine2", (-0.28, -0.12, 0.95)), RightForeArm=A("Spine2", (0.05, -0.08, 0.99)),
                              RightHand=A("", (0.05, -0.08, 0.99), 90),
                              LeftArm=A("Spine2", (0.28, -0.12, 0.95)), LeftForeArm=A("Spine2", (-0.05, -0.08, 0.99))), "io"),
                    (28, pose(STAND, Spine=R(("X", 2)), Neck=R(("X", -8)),
                              RightArm=A("Spine2", (-0.25, -0.85, 0.45)), RightForeArm=A("Spine2", (-0.05, -0.7, 0.7)),
                              RightHand=A("", (0, -0.7, 0.7), 90),
                              LeftArm=A("Spine2", (0.25, -0.85, 0.45)), LeftForeArm=A("Spine2", (0.05, -0.7, 0.7))), "io"),
                    (31, pose(KNEES, NECRO_STAFF, NECRO_LEFT, Spine=R(("X", 4)), Neck=R(("X", -4))), "io"),
                    (36, NECRO_IDLE, "io")],
})
FREE.add("GargDive")  # voa: o pe nao fica no chao


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
    # tornozelo mais baixo em relacao ao repouso (m, mundo): > 0 flutua, < 0 afunda no chao
    return min((W @ pose_m[b.name].translation).z - (W @ b.head_local).z for b in order if short(b.name) in FOOT)


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
        L = L0.lerp(L1, x)
        # pe no chao: a altura do quadril sai das pernas (o pe mais baixo fica na altura do repouso), entao agachar e
        # dobrar o joelho; o z de "loc" e so o ponto de partida (menos nos clipes FREE, que entram e saem do chao). Sem isto o quadril baixo afundava o pe (-11 cm no Grab)
        # e o alto fazia o boneco flutuar (+5 cm no Dash). Sem view_layer.update(): ele reavaliaria a action e apagaria a pose
        apply(arm, D, L - Vector((0, 0, 0 if name in FREE else apply(arm, D, L, order))), order)
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
