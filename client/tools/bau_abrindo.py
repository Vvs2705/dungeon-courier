"""bau_abrindo.py - Blender headless: o bau do Tripo abrindo, para a cena da entrega (0 credito).

Corta a malha na costura da tampa (Z_CORTE, lida no perfil: a cinta mais estreita entre o aro do corpo e o da tampa), separa a
tampa e gira em volta da dobradica de tras (+Y; a fechadura olha -Y). Por dentro, um fundo escuro (a luz dourada do tesouro e' do
jogo, por cima do sprite); por baixo, a tampa ganha um fundo de madeira escura. Renderiza os quadros em 3/4 (camera e enquadramento fixos para todos), contorno de tinta,
e monta a folha no formato do render_sprites.py (meta.json + Static.png + Open.png) para a SpriteSheet.

Uso: blender.exe -b --factory-startup --python-exit-code 1 --python bau_abrindo.py -- <bau.fbx> <Resources/Sprites/bau_hd> [px]
"""
import json, math, os, sys
import bpy, bmesh
import numpy as np
from mathutils import Matrix, Vector

args = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT = os.path.abspath(args[0]), os.path.abspath(args[1])
SIZE = int(args[2]) if len(args) > 2 else 512
Z_CORTE = 0.48                                                # m do FBX (bau de 0,887 m de altura)
ANGULOS = [0, 5, 16, 34, 56, 78, 96, 108, 102, 105]           # graus por quadro: arranca, passa um pouco e assenta
ELEV, YAW, CONTORNO, FPS = 32.0, -35.0, 5, 14.0
INK = (0x1B / 255.0, 0x17 / 255.0, 0x24 / 255.0)              # tinta do contorno (ART_BIBLE sec. 3)
FUNDO, MADEIRA = (0.07, 0.045, 0.03, 1.0), (0.16, 0.09, 0.05, 1.0)


def cor_solida(nome, rgba):
    """Material com textura de 1 px: o Workbench em modo TEXTURE pinta a imagem, como nas texturas do Tripo."""
    img = bpy.data.images.new(nome, 1, 1)
    img.pixels = list(rgba)
    mat = bpy.data.materials.new(nome)
    mat.use_nodes = True
    nt = mat.node_tree
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    mat.diffuse_color = rgba
    return mat


def quad(nome, x, y, z, mat, parent=None):
    me = bpy.data.meshes.new(nome)
    me.from_pydata([(-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)], [], [(0, 1, 2, 3)])
    me.materials.append(mat)
    o = bpy.data.objects.new(nome, me)
    bpy.context.scene.collection.objects.link(o)
    o.parent = parent
    return o


def ink(path, px):
    """Contorno de `px` pixels: dilata o alfa (disco) e compoe a tinta por baixo (igual ao render_sprites.py)."""
    img = bpy.data.images.load(path)
    w, h = img.size
    buf = np.empty(w * h * 4, np.float32)
    img.pixels.foreach_get(buf)
    rgba = buf.reshape(h, w, 4)
    al = rgba[..., 3].copy()
    pad, dil = np.pad(al, px), np.zeros_like(al)
    for dy in range(-px, px + 1):
        for dx in range(-px, px + 1):
            if dx * dx + dy * dy <= px * px:
                dil = np.maximum(dil, pad[px + dy:px + dy + h, px + dx:px + dx + w])
    under = dil * (1.0 - al)
    tot = al + under
    for c in range(3):
        rgba[..., c] = (rgba[..., c] * al + INK[c] * under) / np.maximum(tot, 1e-6)
    rgba[..., 3] = tot
    img.pixels.foreach_set(buf)
    img.save()
    bpy.data.images.remove(img)
    return rgba.copy()


bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
corpo = next(o for o in bpy.data.objects if o.type == "MESH")
corpo.data.transform(corpo.matrix_world)   # vertices em coordenadas de mundo
corpo.matrix_world = Matrix()

# corte na costura e as duas metades em objetos separados (sem face esticada entre corpo e tampa ao abrir)
bm = bmesh.new()
bm.from_mesh(corpo.data)
bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(0, 0, Z_CORTE), plane_no=(0, 0, 1))
bm.to_mesh(corpo.data)
bm.free()
tampa = corpo.copy()
tampa.data = corpo.data.copy()
bpy.context.scene.collection.objects.link(tampa)
for obj, fica_em_cima in ((corpo, False), (tampa, True)):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    fora = [f for f in bm.faces if (f.calc_center_median().z > Z_CORTE) != fica_em_cima]
    bmesh.ops.delete(bm, geom=fora, context="FACES")
    bm.to_mesh(obj.data)
    bm.free()

co = np.array([v.co[:] for v in corpo.data.vertices])
aro = co[np.abs(co[:, 2] - Z_CORTE) < 0.01]
y_dob = aro[:, 1].max()                                # dobradica no aro de tras
x_in, y_in = 0.9 * np.abs(aro[:, 0]).max(), 0.9 * np.abs(aro[:, 1]).max()
dob = bpy.data.objects.new("Dobradica", None)
bpy.context.scene.collection.objects.link(dob)
dob.location = (0, y_dob, Z_CORTE)
bpy.context.view_layer.update()   # sem isto a matrix_world da dobradica ainda e' a identidade e a tampa sobe junto com ela
tampa.parent = dob
tampa.matrix_parent_inverse = dob.matrix_world.inverted()
quad("FundoTampa", x_in, y_in, Z_CORTE + 0.002, cor_solida("madeira", MADEIRA), tampa)
quad("Fundo", x_in, y_in, Z_CORTE - 0.02, cor_solida("fundo", FUNDO), corpo)
raiz = bpy.data.objects.new("Raiz", None)
bpy.context.scene.collection.objects.link(raiz)
for o in (corpo, dob):
    o.parent = raiz
raiz.rotation_euler = (0, 0, math.radians(YAW))

sc = bpy.context.scene
sc.render.engine, sc.render.film_transparent = "BLENDER_WORKBENCH", True
sc.render.image_settings.file_format, sc.render.image_settings.color_mode = "PNG", "RGBA"
sc.render.resolution_x = sc.render.resolution_y = SIZE
sc.view_settings.view_transform, sc.display.render_aa = "Standard", "16"
sh = sc.display.shading
sh.light, sh.studio_light = "STUDIO", "Default"
sh.color_type, sh.show_cavity, sh.show_object_outline = "TEXTURE", True, False
e = math.radians(ELEV)
fwd, up, right = Vector((0, math.cos(e), -math.sin(e))), Vector((0, math.sin(e), math.cos(e))), Vector((1, 0, 0))
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
cam.data.type, cam.data.clip_end = "ORTHO", 1000.0
cam.rotation_euler = (math.pi / 2 - e, 0.0, 0.0)
sc.collection.objects.link(cam)
sc.camera = cam

# enquadramento fixo: os limites projetados de todos os quadros
pts_x, pts_y = [], []
for a in ANGULOS:
    dob.rotation_euler = (-math.radians(a), 0, 0)
    bpy.context.view_layer.update()
    for o in (corpo, tampa):
        for v in o.data.vertices:
            p = o.matrix_world @ v.co
            pts_x.append(p.dot(right)); pts_y.append(p.dot(up))
x0, x1, y0, y1 = min(pts_x), max(pts_x), min(pts_y), max(pts_y)
ext = max(x1 - x0, y1 - y0) * 1.08
cam.data.ortho_scale = ext
cam.location = right * ((x0 + x1) / 2) + up * ((y0 + y1) / 2) - fwd * 50.0
ppu = SIZE / ext
pivot_y = SIZE / 2.0 - (y0 + y1) / 2.0 * ppu           # o pe (origem) na tela, de baixo para cima

os.makedirs(OUT, exist_ok=True)
quadros = []
for i, a in enumerate(ANGULOS):
    dob.rotation_euler = (-math.radians(a), 0, 0)
    f = os.path.join(OUT, "_q%02d.png" % i)
    sc.render.filepath = f
    bpy.ops.render.render(write_still=True)
    quadros.append(ink(f, CONTORNO))
    os.remove(f)
    print("QUADRO %d tampa %d graus" % (i, a))


def folha(nome, imgs):
    h, w = imgs[0].shape[:2]
    big = np.concatenate(imgs, axis=1)
    img = bpy.data.images.new(nome, w * len(imgs), h, alpha=True)
    img.pixels.foreach_set(big.ravel())
    img.filepath_raw, img.file_format = os.path.join(OUT, nome + ".png"), "PNG"
    img.save()


folha("Static", quadros[:1])
folha("Open", quadros)
meta = {"size": SIZE, "fps": FPS, "dirs": 1, "rows": 1, "dir_order": ["S"], "mirror": False, "row0": "top", "elev": ELEV, "yaw": YAW,
        "ppu": round(ppu, 2), "pivot_px": [SIZE / 2.0, round(pivot_y, 1)], "pivot": [0.5, round(pivot_y / SIZE, 4)], "light": "studio",
        "outline_px": CONTORNO, "src_fps": 30, "preset": "bau_hd", "model_scale": 1.0, "hold": "", "attach": [], "align_arms": False,
        "clips": [{"name": "Static", "file": "Static.png", "frames": 1, "loop": False, "fps": 30.0, "duration_s": 0.033, "src_frames": 0},
                  {"name": "Open", "file": "Open.png", "frames": len(ANGULOS), "loop": False, "fps": FPS,
                   "duration_s": round(len(ANGULOS) / FPS, 3), "src_frames": 0}]}
with open(os.path.join(OUT, "meta.json"), "w") as fp:
    json.dump(meta, fp, indent=1)
print("FOLHA", OUT, "%d quadros de %d px, ppu %.1f" % (len(ANGULOS), SIZE, ppu))
