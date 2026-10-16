"""saco_ladrao.py - Blender headless: saco do ladrao menor (feedback "o saco do ladrao domina", 2026-10-16), 0 credito.

O saco de estopa e' parte da malha do Tripo (roupa/bolsa vestida conta como corpo, ASSETS.md). Os vertices do saco = a cor do
fundo do saco (o quarto mais atras do modelo; o Mixamo olha para -Y) no basecolor, atras das costas: pega a esfera e o no' no
ombro, nao os bracos (marrom mais escuro) nem a capa. Eles se aproximam do ponto em que o saco encosta nas costas nesse fator; o peso passa por umas rodadas de
media pelas arestas para a costura nao rasgar. Vale no repouso do rig: todos os clipes herdam. Salva um .blend para o
render_sprites.py (--model).
Uso: blender.exe -b --factory-startup --python-exit-code 1 --python saco_ladrao.py -- <ladrao.fbx> <saida.blend> [fator=0.7]
"""
import os, sys
import bpy
import numpy as np

args = sys.argv[sys.argv.index("--") + 1:]
src, out = args[0], args[1]
fator = float(args[2]) if len(args) > 2 else 0.7

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.abspath(src), use_anim=False)
me = next(o for o in bpy.data.objects if o.type == "MESH")
M = np.array(me.matrix_world)
n = len(me.data.vertices)
co = np.empty(n * 3)
me.data.vertices.foreach_get("co", co)
p = co.reshape(-1, 3) @ M[:3, :3].T + M[:3, 3]

# cor de cada vertice: o basecolor no UV de um loop dele
img = next(i for i in bpy.data.images if "basecolor" in (i.filepath + i.name).lower() or "diffuse" in i.name.lower())
w, h = img.size
px = np.empty(w * h * 4, np.float32)
img.pixels.foreach_get(px)
px = px.reshape(h, w, 4)
uv = np.empty(len(me.data.loops) * 2)
me.data.uv_layers.active.data.foreach_get("uv", uv)
uv = uv.reshape(-1, 2)
lv = np.empty(len(me.data.loops), np.int64)
me.data.loops.foreach_get("vertex_index", lv)
cor = np.zeros((n, 3))
cor[lv] = px[np.clip((uv[:, 1] * h).astype(int), 0, h - 1), np.clip((uv[:, 0] * w).astype(int), 0, w - 1), :3]
z0, z1 = p[:, 2].min(), p[:, 2].max()
y_meio, y_fim = np.median(p[:, 1]), p[:, 1].max()
fundo = p[:, 1] > y_meio + 0.75 * (y_fim - y_meio)   # o fundo do saco: so estopa la atras (o Mixamo olha para -Y)
ref = np.median(cor[fundo], 0)
saco = (np.linalg.norm(cor - ref, axis=1) < 0.16) & (p[:, 1] > y_meio + 0.12 * (y_fim - y_meio))
print("SACO %d de %d vertices (cor %s); altura %.3f-%.3f m; Y do corpo %.3f" % (saco.sum(), n, (ref * 255).astype(int), z0, z1, y_meio))
if saco.sum() < 50:
    sys.exit("ERRO saco nao achado (poucos vertices de estopa atras do tronco)")
s = p[saco]
print("SACO caixa X %.3f..%.3f Y %.3f..%.3f Z %.3f..%.3f" % (*np.c_[s.min(0), s.max(0)].ravel(),))

# peso suave: 1 no saco, media pelas arestas (6 rodadas) para a borda nao rasgar
ed = np.empty(len(me.data.edges) * 2, np.int64)
me.data.edges.foreach_get("vertices", ed)
ed = ed.reshape(-1, 2)
k = saco.astype(float)
for _ in range(6):
    soma, cont = np.zeros(n), np.zeros(n)
    np.add.at(soma, ed[:, 0], k[ed[:, 1]]); np.add.at(soma, ed[:, 1], k[ed[:, 0]])
    np.add.at(cont, ed[:, 0], 1); np.add.at(cont, ed[:, 1], 1)
    k = np.maximum(saco, soma / np.maximum(cont, 1))
# ancora: onde o saco encosta nas costas, no centro da esfera (X e Z do meio da metade de tras: o no' no ombro nao puxa)
tras = s[s[:, 1] > y_meio + 0.6 * (y_fim - y_meio)]
anc = np.array([(tras[:, 0].min() + tras[:, 0].max()) / 2, np.percentile(s[:, 1], 5), (tras[:, 2].min() + tras[:, 2].max()) / 2])
f = 1.0 - (1.0 - fator) * k
p = anc + (p - anc) * f[:, None]
Minv = np.linalg.inv(M)
me.data.vertices.foreach_set("co", (p @ Minv[:3, :3].T + Minv[:3, 3]).ravel())
me.data.update()
for i in bpy.data.images:
    i.pack()
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(out))
print("SACO_OK x%.2f ancora %.3f %.3f %.3f -> %s" % (fator, *anc, out))
