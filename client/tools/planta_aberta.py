"""planta_aberta.py - Blender headless: 2a pose da planta carnivora (Lote 6) sem credito: a boca escancarada.

O modelo do Tripo ja vem de boca entreaberta (a armadilha olha -Y). Aqui o lobulo de cima gira para cima em volta da
dobradica de tras; os vertices perto da dobradica giram so em parte (sem rasgar a malha). Medidas em unidades do FBX
(planta de ~1 m de altura antes do render), lidas no perfil lateral da malha (RENDER_SPRITES.md sec. 7).

Uso: blender.exe -b --factory-startup --python-exit-code 1 --python planta_aberta.py -- <planta.fbx> <saida.blend> [graus]
"""
import math, os, sys
import bpy
import numpy as np
from mathutils import Matrix

args = sys.argv[sys.argv.index("--") + 1:]
src, out = args[0], args[1]
ang = float(args[2]) if len(args) > 2 else 28.0
HINGE_Y, HINGE_Z = 0.07, 0.75  # dobradica atras da cabeca
LIP_Z0, LIP_Z1 = 0.73, 0.78  # abaixo de 0,73 fica parado (lobulo de baixo, caule); acima de 0,78 gira inteiro

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.abspath(src))
me = next(o for o in bpy.data.objects if o.type == "MESH")
if any(o.type == "ARMATURE" for o in bpy.data.objects):
    print("ERRO a planta veio com armature: o script espera um prop parado")
    sys.exit(1)
M = np.array(me.matrix_world)
co = np.empty(len(me.data.vertices) * 3)
me.data.vertices.foreach_get("co", co)
p = co.reshape(-1, 3) @ M[:3, :3].T + M[:3, 3]  # mundo
k = np.clip((p[:, 2] - LIP_Z0) / (LIP_Z1 - LIP_Z0), 0.0, 1.0)
k = k * k * (3 - 2 * k)  # smoothstep
a = -np.radians(ang) * k  # giro em X negativo = a frente (-Y) sobe
dy, dz = p[:, 1] - HINGE_Y, p[:, 2] - HINGE_Z
p[:, 1] = HINGE_Y + dy * np.cos(a) - dz * np.sin(a)
p[:, 2] = HINGE_Z + dy * np.sin(a) + dz * np.cos(a)
Minv = np.linalg.inv(M)
loc = p @ Minv[:3, :3].T + Minv[:3, 3]
me.data.vertices.foreach_set("co", loc.ravel())
me.data.update()
for img in bpy.data.images:  # so o basecolor foi extraido; o Workbench so usa ele
    if img.filepath and not img.packed_file and os.path.exists(bpy.path.abspath(img.filepath)):
        img.pack()
os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(out))
print("PLANTA_OK %s: %d de %d vertices giraram (%d inteiros), %.0f graus" % (
    out, int((k > 0).sum()), len(p), int((k >= 1).sum()), ang))
