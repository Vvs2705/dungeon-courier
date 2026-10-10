# RENDER_SPRITES — modelo 3D rigado → folhas de sprite 2D (Dungeon Courier)

## Estado em 2026-10-10 (raia G, tech-art e animação)

- **Os 24 conjuntos foram re-renderizados com os scripts finais.** Todos saíram com exit 0 e `SPRITES_OK`, e o `folhas.py` conferiu a margem até a borda da célula. Ainda sem aprovação visual do Vinicius.
  - Lote 1: os 9 conjuntos.
  - Lote 2: `bruto`, `ogro`, `ogro_fim`, `reliquia`, `ovo`, `filhote`.
  - Lote 3: `arqueiro` (arco preso à mão esquerda), `capitao` (com `Punch` para o golpe que a View espera), `limo`, `bolo`, `mensagem`, `caixa_pesada`, `barril`, `tocha`, `pilar`.
- **Polimento do Lote 1:**
  - `Shrug` frontal do cliente (antes era o Looking Around, que vira o corpo de costas).
  - Idle numa janela de laço de 3,0 s (antes 9,9 s a 1,2 fps).
  - Corrida do courier com 8 quadros, sem subquadro.
  - Throw, Dash, Hit, Bash e Grab refeitos: antecipação, follow-through, tronco e cabeça, pé no chão em todo quadro (§9).
- **No `render_sprites.py`:** `clips` por preset (quadros fixos e faixa do clipe), `--align-arms` para rig em pose A (bruto e ogro) e limites medidos só nas direções renderizadas (§1).
- **Medidas para a View** (quadro de impacto ou de soltura e passada): `ASSETS.md` §5.
- **Proveniência:** `PROVENIENCIA.md` §3 e §5.

## Estado em 2026-10-09 (raia D, tech-art)

- **EXECUTADO no Lote 1, sem aprovação visual do Vinicius ainda.** O `-Teste` (§6) passou com exit 0. Os 9 conjuntos do Lote 1 saíram com exit 0 e `SPRITES_OK`: `courier`, `courier_fim`, `ladrao`, `guarda` (com o escudo preso), `cliente`, `caixa`, `bau`, `arco` e `morcego`. Ficam em `client/Assets/_DC/Resources/Sprites/`.
- **Folhas de conferência** (fora de `Resources`) em `client/Builds/sprites_contact/`:
  - `<ator>.png` é a folha do render: 1º quadro de cada clipe, direção S;
  - `<ator>_folhas.png` mostra todas as direções e quadros, a 50% (§8);
  - `elenco.png` é a fila do elenco na mesma escala.
- Nenhum crédito gasto. Nenhum clipe novo foi baixado: os que existem vêm de reuso do Forge e do ARKANA, e os que faltavam foram animados no Blender (§9). Proveniência em `PROVENIENCIA.md` §3 e §5.
- Ainda não validado: import no Unity, ASTC no aparelho e leitura na câmera do jogo (`-shot`). Esse passo é do coordenador e da raia View.

**Arquivos:**
- `client/tools/render_sprites.py` (Blender 5.2 headless).
- `client/tools/render_sprites.ps1` (wrapper com timeout, saída nas pastas do DC; não mudou).
- `client/tools/anim_blender.py` (clipes feitos por keyframe no Blender, §9).
- `client/tools/folhas.py` (folhas de conferência, Python 3 + Pillow, §8).

## 1. O que mudou em relação ao Forge

O núcleo é o mesmo:
- retarget por **delta de rotação sobre o repouso no mundo** (aplicar a action crua levantava os braços: rig do Tripo ≠ X Bot);
- modo estático para props sem armature;
- um render por clipe;
- teto de quadros por clipe.

O que entra para o DC:

| Mudança | Por quê |
|---|---|
| `--preset <ator>` (tabela `PRESETS` no `.py`) | Escala, direções e célula de cada ator ficam num lugar só (= `ART_BIBLE.md` §4–5 e `ASSETS.md` §5). Argumento na linha vence o preset |
| `--height M` / `--width M` | Normaliza o modelo no Blender para a medida de projeto. A altura do Tripo varia por modelo; com `ppu` fixo (120) e a altura normalizada, a View não precisa de fator por personagem |
| `--mirror` | Renderiza só de S a N pelo oeste (8 direções → 5 linhas, 4 → 3). O leste é o espelho: menos 37,5% / 25% de memória |
| `--hold FBX` (+ `--hold-frame`) | Os clipes cujo nome começa com `Carry` levam a pose dos braços (ombro até os dedos, relativa ao pai) de um quadro do clipe de segurar. As pernas e o tronco seguem o clipe base. Assim nasce o "correr segurando a caixa" a partir do `Running` |
| `meta.json` → `hands` | O ponto entre as palmas, por direção e quadro, mais a flag "na frente do corpo". A carga é um sprite separado (rachaduras, troca e roubo sem re-render) |
| `--outline PX` | Contorno de tinta `#1B1724` assado na folha (dilatação do alfa em numpy, por baixo do sprite). Os presets usam 3 px |
| `--contact PNG` | A folha de conferência sai de `Resources/` (que vai inteiro para o APK) |
| Aviso de folha > 4096 px | Teto seguro de textura no celular |
| `.ps1` com `-Nome` e timeout de 900 s | O PC suspenso trava o render (memória do estúdio). Caminho com espaço vai entre aspas |
| `--attach FBX:osso:dx,dy,dz:rx,ry,rz:tamanho` (2026-10-09) | Regra dos itens separados (`ASSETS.md`): o escudo é um prop do Tripo preso ao `LeftForeArm` do rig por código, não fundido ao corpo |
| Quadril ancorado no 1º quadro; laço sem deriva (2026-10-09) | Os runs do ARKANA vêm com root motion (o quadril anda 2,2 m em 0,8 s) e o boneco sairia da célula; o Cheering do Forge vem 0,27 m deslocado do pivô |
| `meta.json` → `stride_mps` (2026-10-09) | Passada natural medida na própria deriva do quadril: a View acerta o fps da corrida sem medir na folha |
| `frente` com folga de 15% da altura do quadril (2026-10-09) | De perfil (W), a mão fica no plano do quadril e a carga piscava por cima e por baixo a cada quadro (`W=111000` no CarryRun) |
| `clips` no preset (2026-10-10) | Por clipe, `frames` fixa os quadros e `range` usa só uma faixa do clipe de origem. Exemplos: Run e CarryRun do courier com 8 quadros, de 2 em 2 sem subquadro (6 pulavam 2,67); Breathing Idle de 199 a 289 = laço de 3,0 s. Faixa fora do FBX vira `AVISO` e o clipe entra inteiro (é o caso do `-Teste`) |
| `--align-arms` (presets `bruto`, `ogro`, `ogro_fim`; 2026-10-10) | O Tripo rigou o bruto e o ogro a partir de imagem em pose A: braço em repouso a −49,5° e −22°, contra 0° no X Bot. O delta sobre o repouso levava o braço do A tanto quanto o do T, e ele entrava no corpo. Agora cada osso do braço parte da direção de repouso do clipe |
| Limites só nas direções renderizadas (2026-10-10) | O raio horizontal (válido para qualquer giro) superestimava quem cai deitado. O `ogro_fim` dava 8,2 m e o pivô ia ao meio da célula, cortando o corpo. Agora é a projeção real de cada linha (numpy) |

## 2. Como usar

```
.\client\tools\render_sprites.ps1 -Teste                                   # placeholder do COE -> %TEMP%\dc_sprites\out
.\client\tools\render_sprites.ps1 -Nome courier -Model arte\tripo\courier\<fbx> -Anims arte\mixamo\courier -Hold arte\mixamo\Hold.fbx
.\client\tools\render_sprites.ps1 -Nome caixa -Model arte\tripo\caixa\<fbx>
.\client\tools\render_sprites.ps1 -Nome pilar -Preset none -Model <fbx> -Extra @("--width","1.2","--size","192","--scale","120","--outline","3")
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --factory-startup --python-exit-code 1 --python client\tools\render_sprites.py -- --model M.fbx --anims pasta\ --out saida\ --preset courier
```

**Comandos usados no Lote 1 (2026-10-09).** Os FBX são cópias em caminho curto: `%TEMP%\dc_in\<ator>\` vem de `arte\tripo\<ator>\` (FBX + `.fbm`) e `%TEMP%\dc_arte\mixamo\` vem de `arte\mixamo\`.

```
.\client\tools\render_sprites.ps1 -Nome caixa       -Model $env:TEMP\dc_in\caixa\tripo_convert_dc019326-....fbx     (bau, arco, morcego: idem)
.\client\tools\render_sprites.ps1 -Nome courier     -Model $env:TEMP\dc_in\courier\tripo_convert_f2e9a770-....fbx -Anims $env:TEMP\dc_arte\mixamo\courier -Hold $env:TEMP\dc_arte\mixamo\Hold.fbx
.\client\tools\render_sprites.ps1 -Nome courier_fim -Model (o do courier) -Anims $env:TEMP\dc_arte\mixamo\courier_fim
.\client\tools\render_sprites.ps1 -Nome ladrao      -Model $env:TEMP\dc_in\ladrao\tripo_convert_78892eb1-....fbx  -Anims $env:TEMP\dc_arte\mixamo\ladrao -Hold $env:TEMP\dc_arte\mixamo\Hold.fbx
.\client\tools\render_sprites.ps1 -Nome guarda      -Model $env:TEMP\dc_in\guarda\tripo_convert_77906b18-....fbx  -Anims $env:TEMP\dc_arte\mixamo\guarda `
    -Extra @("--attach", "$env:TEMP\dc_in\escudo\tripo_convert_80524815-....fbx:LeftForeArm:0.09,0,0.06:90,180,0:0.75")
.\client\tools\render_sprites.ps1 -Nome cliente     -Model $env:TEMP\dc_in\cliente\tripo_convert_cb64fb7a-....fbx -Anims $env:TEMP\dc_arte\mixamo\cliente
python client\tools\folhas.py client\Assets\_DC\Resources\Sprites\courier client\Assets\_DC\Resources\Sprites\ladrao --carga client\Assets\_DC\Resources\Sprites\caixa
```

**Comandos do polimento e dos Lotes 2 e 3 (2026-10-10).** Mesmas cópias em caminho curto. Os clipes do Blender saem do `anim_blender.py` (§9) e são copiados para `arte\mixamo\<conjunto>\` (tabela em `PROVENIENCIA.md` §3).

```
blender.exe -b --factory-startup --python-exit-code 1 --python client\tools\anim_blender.py -- --rig %TEMP%\dc_arte\mixamo\courier\Idle.fbx --out %TEMP%\dc_anim_v2 --only Throw,Dash,Hit,GuardIdle,Bash,Grab,Shrug,Smash,Slam,Roar,Shoot,Call
.\client\tools\render_sprites.ps1 -Nome bruto    -Model $env:TEMP\dc_in\bruto\tripo_convert_c1fe3cb8-....fbx    -Anims $env:TEMP\dc_arte\mixamo\bruto
.\client\tools\render_sprites.ps1 -Nome ogro     -Model $env:TEMP\dc_in\ogro\tripo_convert_b3759572-....fbx     -Anims $env:TEMP\dc_arte\mixamo\ogro
.\client\tools\render_sprites.ps1 -Nome ogro_fim -Model (o do ogro)                                          -Anims $env:TEMP\dc_arte\mixamo\ogro_fim
.\client\tools\render_sprites.ps1 -Nome arqueiro -Model $env:TEMP\dc_in\arqueiro\tripo_convert_85f16e73-....fbx -Anims $env:TEMP\dc_arte\mixamo\arqueiro `
    -Extra @("--attach", "$env:TEMP\dc_in\arcoflecha\tripo_convert_c9a4beee-....fbx:LeftHand:-0.06,0,-0.015:90,180,0:0.8")
.\client\tools\render_sprites.ps1 -Nome capitao  -Model $env:TEMP\dc_in\capitao\tripo_convert_041909b7-....fbx  -Anims $env:TEMP\dc_arte\mixamo\capitao -Hold $env:TEMP\dc_arte\mixamo\Hold.fbx
.\client\tools\render_sprites.ps1 -Nome reliquia -Model $env:TEMP\dc_in\reliquia\tripo_convert_0a9e5d99-....fbx  (ovo, filhote, limo, bolo, mensagem, caixa_pesada, barril, tocha, pilar: idem)
python client\tools\folhas.py client\Assets\_DC\Resources\Sprites\<ator> [--carga client\Assets\_DC\Resources\Sprites\caixa]
```

Os 9 do Lote 1 usam os comandos de cima. Os 24 conjuntos foram re-renderizados em sequência, um Blender por vez, com espera enquanto o Unity estivesse aberto.

**Tempo medido** (PC de 7,7 GB):
- Prop: 3 a 30 s.
- Personagem: 2 a 3,5 min em 2026-10-09; de 2 a 9 min em 2026-10-10 (122 a 532 s), com ~1 GB livre.
- O tempo vai quase todo no import do FBX do Tripo, que tem malha com pele. O `.ps1` corta em 900 s.

**Onde sai cada coisa:**
- Saída do `.ps1`: `client/Assets/_DC/Resources/Sprites/<Nome>/`.
- Folha de conferência: `client/Builds/sprites_contact/<Nome>.png` (`Builds/` fica fora do git).
- Log: `%TEMP%\dc_render_<Nome>.log`.

**Exit codes:**
- 0 com `SPRITES_OK` no log;
- 1 com `ERRO ...`;
- 124 em timeout.

**Regras de operação:**
- Um ator por vez, Unity fechado.
- Caminhos curtos. Se der "Please select a file", é MAX_PATH: copie o FBX para `%TEMP%`.
- O import do FBX 2K leva 2–3 min.
- O `Hold.fbx` fica **fora** da pasta `-Anims`. Se ficar dentro, o script pula o arquivo.
- `OpenImageIO write failed: Could not open file ...png` (exit 1) foi uma trava passageira no PNG recém-gravado; rodar de novo resolveu (courier, 2026-10-10).

| Parâmetro (`.py`) | Padrão (sem preset) | Presets do DC | O que faz |
|---|---|---|---|
| `--model` | — | — | FBX ou GLB rigado (1 armature + malhas); prop sem armature = 1 quadro `Static` |
| `--anims` / `--self` | — | — | Pasta com 1 FBX por clipe (Mixamo Without Skin) / actions já no modelo |
| `--preset` | — | ver §3 | Ator do DC |
| `--hold`, `--hold-frame` | — | `Hold.fbx`, 1º quadro | Braços do clipe de segurar nos clipes `Carry*` |
| `--elev` | 60 | 60 | Graus da câmera sobre o chão |
| `--dirs` / `--mirror` | 4 / não | courier 8 + espelho; inimigos 4 + espelho; resto 1 | Direções lógicas / só renderiza S..N pelo oeste |
| `--fps` / `--max-frames` | 12 / 32 | 12 / 12 (ogro 10, ogro_fim 9) | Amostragem e teto de quadros por clipe (o fps real de cada clipe vai no meta) |
| `--size` | 128 | 96–448 | px por célula quadrada |
| `--scale` | 0 (ajusta) | **120** | px por metro na textura (`ppu`) |
| `--height` / `--width` | 0 | ver §3 | Normaliza a altura / a maior medida horizontal (m) |
| `--yaw` | 0 | 0 | Use 180 se o modelo sair de costas na direção S |
| `--light` | flat | flat | `flat` (cor plana + cavidade + contorno), `matcap` (toon de 2 tons), `studio` |
| `--outline` | 0 | 3 | px de tinta `#1B1724` |
| `--aa` | 8 | 8 | Antisserrilhado do Workbench |
| `--shadow` | não | não | Auto-sombra do Workbench (não há sombra no chão: a elipse é da View) |
| `--src-fps` | 30 | 30 | fps dos clipes de origem (Mixamo = 30) |
| `--contact` | `<out>/contact.png` | `.ps1`: `client/Builds/sprites_contact/<Nome>.png` | 1º quadro de cada clipe, direção S |
| `--attach` | — | `guarda`: escudo (§2) | Prop preso a um osso, repetível. Deslocamento (m) a partir da **cabeça** do osso e giro (graus, Euler XYZ), os dois em eixos do mundo **no repouso em T** do modelo já normalizado. `tamanho` = maior medida do prop em m. O centro da caixa do prop fica no ponto. O osso pode vir com ou sem `mixamorig:` |

## 3. Presets (`PRESETS` no `.py`)

Todos os presets usam `scale` 120, `outline` 3 e `max_frames` 12, exceto onde a tabela indica outro valor.

| Preset | Normalização | Direções (linhas) | Célula | Folha mais larga | Por clipe (`clips`) e extras |
|---|---|---|---|---|---|
| `courier` | altura 1,30 m | 8 com espelho (5) | 224 | 12 × 224 = 2 688 px | Idle e CarryIdle 199–289; Run e CarryRun 8 quadros; Throw 10 |
| `courier_fim` | altura 1,30 m | 1 | 320 | 3 840 px | — |
| `ladrao` | altura 1,00 m | 4 com espelho (3) | 160 | 1 920 px | — |
| `guarda` | altura 1,50 m | 4 com espelho (3) | 224 | 2 688 px | escudo pelo `--attach` na linha |
| `cliente` | altura 1,35 m | 1 | 192 | 2 304 px | Idle 199–289 |
| `bruto` | altura 1,80 m | 4 com espelho (3) | **336** (era 256) | 4 032 px | Idle 199–289; `align_arms` |
| `ogro` | altura 2,80 m | 4 com espelho (3) | **448, máx. 9** (era 384 × 10) | 4 032 px | Idle 199–289; `align_arms` |
| `ogro_fim` | altura 2,80 m | 1 | **576, máx. 7** (era 448 × 9) | 4 032 px | `align_arms` |
| `arqueiro` | altura 1,35 m | 4 com espelho (3) | 224 | 2 688 px | Idle 199–289; arco pelo `--attach` na linha |
| `capitao` | altura 1,25 m | 4 com espelho (3) | 192 | 2 304 px | Idle 199–289; `--hold` |
| `morcego` / `filhote` / `limo` | largura 0,70 m / altura 0,50 m / altura 0,60 m | 1 | 112 / 96 / 128 | — | Static |
| `caixa` / `reliquia` / `ovo` | largura 0,50 m / altura 0,55 m / altura 0,55 m | 1 | 112 / 96 / 96 | — | Static |
| `bolo` / `mensagem` / `caixa_pesada` / `barril` | largura 0,60 / 0,45 / 0,70 m; altura 0,55 m | 1 | 112 / 96 / 128 / 96 | — | Static |
| `bau` / `bau_aberto` / `arco` | largura 0,80 / 0,80 / 2,20 m | 1 | 160 / 160 / 448 | — | Static |
| `tocha` / `pilar` | altura 0,70 m / largura 1,20 m | 1 | 112 / **288** | — | Static; o pilar de 1,2 m de largura fica com ~2,2 m de altura (proporção do modelo) |

As células foram calculadas pela geometria de projeto com 10% de margem e conferidas na folha (o `folhas.py` imprime a margem mínima).
- Em 2026-10-10, o bruto, o ogro, o ogro_fim e o pilar encostavam na borda e subiram de célula.
- O teto de 4 096 px por folha obrigou a cortar quadros no ogro (9) e no ogro_fim (7).
- A janela 199–289 do Breathing Idle vale só para esse FBX (`BREATH` no `.py`).
- Se o log mostrar `AVISO recorte`, suba o `--size` daquele ator (pela linha ou na tabela).
- Nunca baixe o `--scale`: todo o elenco compartilha os 120 px/m.

## 4. Saída e contrato com a View

**Arquivos:** `<Clipe>.png` (RGBA transparente; **linha = direção, linha 0 em cima; coluna = quadro**) + `meta.json`.

O `meta.json` usa listas planas, que o `JsonUtility` lê:

| Campo | Conteúdo |
|---|---|
| `size` | px da célula quadrada |
| `ppu` | px por metro: use no `Sprite.Create`, e 1 unidade = 1 m sem escala extra |
| `dirs` | direções lógicas (1, 4, 8) |
| `rows` | linhas na folha |
| `dir_order` | nomes das linhas (S, SW, W, NW, N…) |
| `mirror` | `true` se o leste é espelho |
| `pivot` | [0,5, y/size] (normalizado, origem no canto inferior esquerdo, como o `Sprite.Create`) |
| `pivot_px` | o mesmo em px |
| `elev`, `yaw`, `light`, `outline_px`, `src_fps`, `preset`, `model_scale`, `hold`, `attach`, `align_arms` | rastreio (`attach` = lista dos `--attach` sem a pasta do FBX, com osso, deslocamento, giro e tamanho; 2026-10-09. `align_arms`: 2026-10-10) |
| `clips[]` | `name`, `file`, `frames`, `loop` (pelo nome: idle, walk, run, lurk), `fps` (real do clipe), `duration_s`, `src_frames`, `hands` (só em ator com mãos), `stride_mps` (opcional, ver abaixo) e `src_range` (opcional, 2026-10-10: [início, fim] do clipe de origem quando o preset usa só uma faixa) |

**`stride_mps`** (por clipe; **campo novo de 2026-10-09**, opcional):
- Aparece só em clipe em laço cujo quadril andava na origem (root motion) mais de 0,3 m/s.
- É a velocidade, já na escala do ator, em que o pé não patina.
- Ritmo da locomoção: `fps × (velocidade da Sim ÷ stride_mps)`.
- Se o campo não vier, a View usa 1×.
- O `JsonUtility` ignora campo ausente, então quem já lê o meta não quebra.

**`hands`** (por clipe): lista plana `[x, y, frente, x, y, frente, ...]`.
- Índice = `(linha * frames + quadro) * 3`.
- `x`, `y` = px na célula a partir do canto inferior esquerdo.
- `frente` = 1 se as mãos estão mais perto da câmera que o quadril (a carga desenha **acima** do portador), 0 se atrás (desenha **abaixo**).
  - Desde 2026-10-09, mão até 15% da altura do quadril atrás dele ainda conta como frente. O padrão resultante é estável por linha (conferido pelo `folhas.py`):
    - S, SW e W = 1;
    - NW e N = 0.
- No clipe espelhado, `x' = size − x`.

**Pivô** = origem do modelo (pé no chão). Em câmera oblíqua, a ponta do pé que balança para a câmera aparece abaixo da linha do pivô. É o certo para 3/4.

**Direção pelo movimento** (velocidade da Sim no plano; 0 = S, em sentido horário, como no Forge):

```csharp
int n = meta.dirs;
int d = (Mathf.RoundToInt(-Mathf.Atan2(v.x, -v.y) * Mathf.Rad2Deg / (360f / n)) % n + n) % n;   // parado: guarda o ultimo d
int row = d <= n / 2 ? d : n - d;              // com mirror: NE->NW, E->W, SE->SW (8) ou E->W (4)
bool flip = meta.mirror && d > n / 2;
Rect cel = new Rect(i * meta.size, (meta.rows - 1 - row) * meta.size, meta.size, meta.size);   // linha 0 no TOPO do PNG
Sprite s = Sprite.Create(tex, cel, new Vector2(meta.pivot[0], meta.pivot[1]), meta.ppu);      // cache por (clipe, row, i)
sr.sprite = s; sr.flipX = flip;                                                                // pivo x = 0,5: o flip nao desloca
int frame = clip.loop ? (int)(t * clip.fps) % clip.frames : Mathf.Min((int)(t * clip.fps), clip.frames - 1);

// carga nas maos (sprite da carga com pivo no centro)
int k = (row * clip.frames + frame) * 3;
float hx = flip ? meta.size - clip.hands[k] : clip.hands[k];
Vector3 off = new Vector3((hx - meta.pivot_px[0]) / meta.ppu, (clip.hands[k + 1] - meta.pivot_px[1]) / meta.ppu, 0f);
carga.position = portador.position + off;      // offset ja projetado (plano da tela), nao em coordenada da Sim
carga.sortingOrder = portador.sortingOrder + (clip.hands[k + 2] > 0.5f ? 1 : -1);
```

**Ritmo:**
- Locomoção: `fps` × (velocidade da Sim ÷ passada natural medida na folha), com teto de 2,3× (receita do COE).
- Ataque com telegraph: quadros [0, impacto) esticados por `Tele` segundos, e o resto em 0,15 s (`ART_BIBLE.md` §7; o quadro de impacto está em `ASSETS.md` §5).

**Import no Unity** (dono: raia View/Editor; modelo `03_FORGE_STREET/.../Editor/SpriteImport.cs`):
- Texture2D legível pelo `Sprite.Create`;
- máximo 4096;
- **sem mipmaps**;
- clamp e bilinear;
- override Android **ASTC 6×6** (sem ASTC o Unity descomprime para RGBA32: 9×, `ART_BIBLE.md` §5).
- O halo de compressão na borda do contorno precisa ser conferido no aparelho (pendência herdada do Forge).

## 5. Como funciona (resumo técnico)

1. `read_factory_settings(use_empty=True)`; fps da cena = `--src-fps`; importa o modelo; `--height`/`--width` escala os objetos-raiz em volta da origem.
2. **`--hold`:** importa o clipe de segurar. No quadro escolhido, guarda para cada osso de braço (`(Left|Right)(Shoulder|*Arm|Hand*)`) a rotação relativa ao pai, `L = Δpai⁻¹ · Δosso`, com Δ = pose × repouso⁻¹ no mundo. Depois apaga o clipe.
3. Cada clipe é retargetado por delta. Nos clipes `Carry*`, o osso de braço usa `Δ = Δpai(quadro atual) · L`: os braços acompanham o tronco que corre e mantêm a pose de segurar.
   - **Quadril:** o deslocamento horizontal do clipe passa a contar a partir do 1º quadro.
     - Em clipe em laço, sai também a deriva linear entre o 1º e o último quadro (root motion → in place).
     - A deriva, vezes a razão das alturas do quadril e dividida pela duração, vira `stride_mps`.
     - O sobe-e-desce e o balanço do quadril ficam.
     - Clipe que não é laço (Dying) mantém o percurso, ancorado no 1º quadro.
   - **`--attach`:** depois de normalizar, o prop é importado, centrado pela caixa e escalado para `tamanho`. Em seguida é posto no repouso (cabeça do osso + deslocamento, giro) e vira filho do osso.
     - O inverso do pai é a matriz de repouso do osso, pela cauda: é a origem de pai-osso do Blender.
     - Em cada quadro o prop recebe o mesmo delta do osso.
     - O prop entra nas malhas do ator: limites, render e contorno.
4. **Limites e escala:** meia largura e extremos verticais na tela, pelos vértices reais de todos os quadros. Desde 2026-10-10, medidos só nas direções que entram na folha, já giradas como na célula (o espelho tem a mesma medida). Com `--scale`, `ppu` é fixo e o script só avisa recorte. Pivô = origem.
   - **`--align-arms`:** em rig em pose A, cada osso do braço (inclusive ombro e dedos) primeiro gira, pelo arco mínimo, da direção do próprio repouso para a do repouso do clipe. Só então recebe o delta, e a direção do braço fica igual à do clipe.
5. **Câmera e render:**
   - Workbench ortográfico a `--elev`, fundo transparente, AA.
   - Por clipe, cada célula é uma cópia da malha avaliada, girada em Z (`yaw − linha × 360/dirs`) e deslocada no plano da câmera.
   - Um único render por clipe.
   - Em seguida, o contorno: dilatação em disco do alfa, tinta por baixo, alfa reto.
6. **`hands`:** média de `head` e `tail` dos ossos `LeftHand`/`RightHand`, girada como a célula e projetada nos eixos da câmera. A flag "frente" compara a profundidade com a do quadril.

## 6. Teste (rodar antes do 1º ator real; 0 crédito)

`.\client\tools\render_sprites.ps1 -Teste`

**O que faz:**
- Gera o humanoide-placeholder do COE (1,10 m, 19 ossos com nomes do Unity, clipes com braços em T) em `%TEMP%\dc_sprites\fbx`.
- Renderiza `Idle`, `Run` e `CarryRun` (cópia do Run, com os braços do quadro 0 do `Attack1` via `--hold`) com o preset `courier`: 1,30 m, 8 direções com espelho = 5 linhas, contorno de 3 px.
- Saídas: `%TEMP%\dc_sprites\out` e `contact.png`.

**Aceite:**
- exit 0 e `SPRITES_OK ... linhas=5`;
- `ESCALA x1.18…` e `HOLD Attack1.fbx: 8 osso(s) de braço` no log;
- 3 PNG com 5 linhas;
- `meta.json` com `mirror: true` e `hands` com `5 × quadros × 3` valores por clipe;
- na folha `CarryRun`, os braços diferentes dos do `Run`;
- contorno escuro visível.

**Prova vermelha** (a do Forge continua valendo): um clipe com `mixamorig:` no placeholder dá `ERRO armature nao bate`, exit 1.

**Executado em 2026-10-09** com o script final (SHA `6d435e16…1ec9`): exit 0 em 6 s.
- No log: `ESCALA x1.1818`, `HOLD Attack1.fbx: 8 osso(s) de braco` e `SPRITES_OK ... linhas=5`.
- `meta.json`: `mirror: true` e `hands` = 5 × quadros × 3 (CarryRun 120, Idle 180, Run 120).
- Nenhum `stride_mps`: os clipes do placeholder não têm deriva de quadril.
- A prova vermelha do Forge não foi re-rodada nesta raia.

## 7. Limites e problemas conhecidos

- **O Workbench ignora nós de material:** só a textura ligada ao Base Color aparece. Sem normal map nem emissão.
- **Clipes do placeholder têm braços em T** (lição COE #1): S e N parecem uma cruz. É defeito do clipe, não do render.
- **`--hold` copia a pose de UM quadro:** a mão não balança. É o certo para segurar carga, mas não use em clipe de ataque.
  - Se o clipe de segurar vier de outro esqueleto que não o Mixamo, os nomes dos ossos não batem e os braços ficam com o clipe base. O log conta os ossos do hold.
- **`hands` usa as palmas:** cargas largas encostam nos antebraços na vista S. A carga desenha por cima e isso esconde.
  - Se ficar alta ou baixa demais, ajuste na View um desvio fixo por carga, em vez de re-renderizar.
- **Morte deitada** pede célula maior (o corpo sai do centro). Por isso existem os conjuntos `_fim`. Os inimigos comuns morrem de forma procedural na v0.1.
- **O AA do Workbench varia alguns níveis entre renders:** o SHA dos PNG não é reprodutível bit a bit (Forge §6). Registre o SHA do FBX de entrada e o do `render_sprites.py`.
- **Uma folha por clipe, sem atlas único:** são poucas texturas por ator e a memória está estimada em `ART_BIBLE.md` §5.
- **`AVISO recorte` não é mais conservador (2026-10-10):** mede só as direções renderizadas. Até 2026-10-09, usava 2 × o raio horizontal: o `courier_fim` avisava 2,96 m contra 2,67 m sem recorte real, e o `ogro_fim` dava 8,2 m e cortava o corpo. Confira a margem que o `folhas.py` imprime.
- **Slerp do delta sobre o T entre chaves distantes (2026-10-10):** com um braço que vai de "para trás" a "para frente" ou ao alto entre duas chaves, o meio do caminho passa pelo lado. De frente para a câmera, isso parece braço em T. Apareceu no Dash, no Slam, no Call e no Punch. A correção é uma chave de passagem (braço por baixo ou pela frente), com menos de 90° entre chaves. A folha de palitos do §9 mostra isso em 3 s.
- **A 60°, "para trás" sobe na tela:** braço para trás e para baixo aparece na horizontal, e tronco inclinado para trás afasta os ombros do eixo. No Slam, os punhos saíam do topo da célula do ogro; o tronco ficou quase reto no alto.
- **Janela do Idle:** o Breathing Idle não tem um laço curto perfeito. A janela 199–289 tem a menor costura (2,2 cm na mão direita do X Bot, ~1,6 cm no courier, ~1 px na tela). Com outro FBX de Idle, refaça a busca.
- **Rig em pose A:** confira a direção de repouso do braço ao receber um rig novo do Tripo (bruto −49,5°, ogro −22°, arqueiro −9°, capitão −3°). Acima de ~15°, use `align_arms` no preset.
- **O `--attach` usa o repouso:** deslocamento e giro valem na pose em T. A conta para o escudo está em §9; se a pose do braço do guarda mudar, refaça a conta e confira a folha.
- **Chapéu do courier a 60°:** a aba larga cobre o tronco nas vistas S e N, e na corrida S só se veem o chapéu e um pé. É do modelo e da câmera (`ART_BIBLE.md` §2), não do clipe. Com a caixa por cima (frente = 1), a leitura "courier com carga" continua.

## 8. Folhas de conferência (`folhas.py`)

`python client/tools/folhas.py <Resources/Sprites/ator> [...] [--carga <Resources/Sprites/caixa>] [--escala 0.5]`. Roda com Python 3 + Pillow, fora do Blender e do Unity.

**`<ator>_folhas.png`:** uma faixa por clipe, sobre xadrez cinza, com todas as linhas e quadros.
- Linha do pivô em vermelho.
- Rótulo com quadros, fps, duração, laço e `stride_mps`.
- O script imprime a **margem mínima** de cada clipe até a borda da célula (contorno incluso). Com `AVISO encosta`, suba a célula do preset (2026-10-10).
- Nos clipes `Carry*`, o quadro `Static` da caixa entra no ponto `hands`: por cima do portador com frente = 1, por baixo com 0, como a View faz.
- O script imprime a flag `frente` por linha. Exemplo: `CarryRun frente: S=111111 SW=111111 W=111111 NW=000000 N=000000`. A flag não pode piscar dentro de uma linha.
- Com `--escala 0.5`, a célula de 224 px vira ~112 px, que é o tamanho do courier na tela de 1080 px.

**`--elenco <ator1> <ator2> ...`:** gera `elenco.png`.
- 1º quadro do Idle (ou do 1º clipe), direção S, com o pé de todos na mesma linha.
- Traços a cada 0,5 m na vertical da tela (120 px/m). É altura projetada a 60°, não a altura do modelo.
- Avisa se o `ppu` diferir entre os atores.

## 9. Clipes feitos no Blender (`anim_blender.py`)

```
blender.exe -b --factory-startup --python-exit-code 1 --python client\tools\anim_blender.py -- --rig %TEMP%\dc_arte\mixamo\courier\Idle.fbx --out %TEMP%\dc_anim [--only Hold,Throw]
```

**Pé no chão (2026-10-10):** em todo quadro, a altura do quadril sai das pernas. O pé mais baixo fica na altura do repouso; o `z` de `loc` é só o ponto de partida, e agachar é dobrar o joelho.
- Antes, o quadril baixo afundava o pé (−11 cm no Grab, −8 cm na carga do Bash) e o alto fazia o boneco flutuar (+5 cm no Dash).
- Medido no X Bot: tornozelo a ±0,1 cm do repouso nas chaves. O Grab afunda 2,1 cm no subquadro 7,5 (≈ 1,4 px no ladrão).

**Base:** o armature do X Bot de qualquer clipe Mixamo Without Skin (aqui, o Breathing Idle reaproveitado do Forge). A animação do clipe de origem é descartada.

**Clipes** (`CLIPS` no `.py`; 30 fps; saída copiada para `arte/mixamo/_blender/` e depois para o conjunto):

**Chaves nos quadros amostrados:** a pose extrema que não cai num quadro que o render amostra some da folha. Quadros amostrados = `ini + i × vão / n`; o quadro de impacto da View é `round(ImpactFrac × n)`.

| Clipe | Quadros (s) → folha | Uso | Chaves (2026-10-10) |
|---|---|---|---|
| `Hold` | 10 (0,30) | `--hold` dos `Carry*` (courier, ladrão, capitão) | Pose fixa: braços à frente do peito, cotovelos dobrados, palmas para dentro (sem mudança desde 2026-10-09) |
| `Throw` | 21 (0,67) → 10 de 2 em 2 | courier; a View toca em laço no compasso do tiro | Pronto (0) → antecipação: afunda e o braço desce para trás (4) → carregado: tronco torcido para trás, mão alta atrás da cabeça, mão esquerda no alvo (8) → **solta (10, "out") = quadro 5** → acompanha cruzando o corpo (14) → volta com sobra (17) → pronto (20 = 0) |
| `Dash` | 16 (0,50) → 6 | courier, o clipe inteiro nos 0,18 s | Pronto → agacha com os braços à frente (3) → impulso: tronco projetado, braços para trás (5, "out") → desliza (8) → passagem com os braços por baixo (10) → freia com o tronco para trás e a cabeça à frente (12) → pronto (15) |
| `Hit` | 15 (0,47) → 6 | courier e arqueiro | Pronto → tranco: cabeça e tronco para trás, braços jogados à frente (2, "out") → rebote: dobra à frente e a cabeça cai (5) → assenta com sobra (9) → pronto (14) |
| `GuardIdle` → `guarda/Idle` | 60 (1,97), laço → 12 | guarda | Escudo atravessado à frente, respiração (0 = 59) |
| `Bash` | 30 (0,97) → 12 | guarda | Guarda → ergue o peito e recolhe o escudo (5) → carga: agacha, ombro para trás (9) → segura a carga, cabeça atrás do escudo (12 = fim do telegraph) → **impacto com passo longo (15, "out") = quadro 6** → passa do ponto (18) → recolhe (22) → guarda (29) |
| `Grab` | 16 (0,50) → 6 | ladrão e capitão, em 0,5 s (= `ThiefGrab`) | Pronto → olha a carga (2) → agacha e alcança (5) → fecha as mãos (7) → ergue com sobra para trás (10) → pose do `Hold` (15), que emenda no `CarryRun`. Antes tinha 0,77 s e a View cortava a subida aos 0,5 s |
| `Shrug` | 49 (1,60) → 12 de 4 em 4 | cliente, em laço (2 rachaduras) | Calmo → ombros caem e os joelhos cedem (8) → **dá de ombros: ombros sobem, palmas para cima com giro de 180°, cabeça de lado (16, "out")** → segura, inclinando mais (28) → solta com sobra (38) → calmo (48 = 0) |
| `Smash` | 30 (0,97) → 12 | bruto | Pronto → punhos ao peito (6) → manopla direita no alto atrás da cabeça (11–15 = fim do telegraph) → **soco no chão com passo (17, "out") = quadro 7** → passa do ponto (20) → recolhe (24) → pronto (29) |
| `Slam` | 31 (1,00) → 9 de 3,3 em 3,3 | ogro | Pronto → afunda e joga os braços para baixo e para trás (4) → passam por baixo (6) e pela frente (8) → punhos juntos no alto, tronco quase reto (11–14) → **pancada no chão agachado (17, "out") = quadro 5** → afunda mais (20) → levanta (24) → pronto (30) |
| `Roar` | 41 (1,33) → 9 | ogro (a View toca uma vez ao chamar um ladrão) | Pronto → inspira para trás (8) → **ruge: tronco à frente, cabeça projetada, punhos fechados à frente do peito (16, "out")** → segura tremendo (28) → volta (34) → pronto (40). Braços abertos liam como T e passavam da célula |
| `Shoot` | 37 (1,20) → 12 de 3 em 3 | arqueiro | Pronto → ergue o arco (6) → braço do arco no alvo, em eixos do mundo, e a mão na corda (12) → puxa até a bochecha (18) → mira (21 = fim do telegraph) → **solta: a mão escapa para trás da orelha (24, "out") = quadro 8** → baixa o arco (29) → pronto (36) |
| `Punch` | 30 (0,97) → 12 | capitão (`CaptainHit` da View) | Pronto → o antebraço passa pela frente (2) → arma o punho no ombro, tronco torcido (5–12 = fim do telegraph) → **soco esticado com passo (15, "out") = quadro 6** → passa do ponto (19) → recolhe (24) → pronto (29) |
| `Call` | 31 (1,00) → 12 | capitão (`CaptainCall`) | Pronto → inspira, mão direita à boca (6) → braço esquerdo sobe pela frente (9) → **grita: mão em concha na boca e braço esquerdo erguido (12, "out")** → acena para fora (18) e para dentro (24) → desce o braço pela frente (27) → pronto (30) |

**Folha de palitos para iterar** (2026-10-10, fora do repositório, em `%TEMP%`): `dc_prev.py` no Blender grava as juntas projetadas a 60° de cada FBX de clipe por direção e quadro amostrado, e imprime a altura do tornozelo. `dc_prev_draw.py` (Pillow) desenha os bonecos. Leva ~3 s, contra 3 a 8 min de um render com a malha do Tripo, e serviu para achar braço em T, pé afundando e pose fora do quadro amostrado.

**Como a pose é escrita:** o personagem olha para −Y, +X é a esquerda dele e +Z é cima.
- `("rot", (eixo, graus), ...)`: giro relativo ao pai, em eixos do mundo do repouso.
- `("aim", ref, (x, y, z), tw)`: o osso aponta para (x, y, z) no referencial do osso `ref` (`""` = mundo), com `tw` graus de giro no próprio eixo (palma).
- Entre as chaves, slerp por osso com smoothstep, ou "out" (cúbica) para o golpe rápido.
- O resultado é o mesmo delta sobre o repouso no mundo que o `render_sprites.py` retargeta.

**Armadilha do export, com prova:**
- **O problema:** o exportador FBX do Blender, num armature sem malha com bind pose, grava como repouso a pose do quadro corrente. O clipe reimportado vinha com repouso = pose e rotação identidade, e o render punha os braços em T.
- **A correção:** chave de repouso 2 quadros antes do clipe, fora da faixa exportada, e export parado nela. Também saiu um `view_layer.update()` do laço de chaves: ele reavaliava a action e apagava a pose do 1º quadro.
- **A checagem `verify()`:** reimporta cada FBX e confere duas coisas: o repouso contra o rig de origem (cos ≥ 0,999) e o 1º e o último quadro contra a 1ª e a última pose-chave (cos ≥ 0,995). Comparar com a pose-chave, e não com a action, é o que pega o defeito, porque a action errada concorda consigo mesma.
- **Prova vermelha (2026-10-09):** sem a chave de repouso, `ERRO ... repouso reimportado difere do rig (pior cos -0.387)`, exit 1. Com o `update()` de volta, `ERRO ... 1o/ultimo quadro reimportado difere da pose escrita`, exit 1.

**Escudo do guarda (`--attach`, conta de 2026-10-09):**
- **Pose da guarda** (Idle e Bash): o antebraço esquerdo aponta (−0,8, −0,6, 0) no peito, atravessado à frente do corpo, com giro −90°. A palma vira para o peito e o dorso do antebraço para fora e para a frente-esquerda.
- **Escudo do Tripo:** fica de pé (altura em Z), com a frente para −Y. Desfazendo o delta do antebraço, no repouso em T o escudo deve ficar deitado sobre o dorso do antebraço:
  - a frente vira +Z;
  - o "para cima" vira −Y;
  - o giro é Euler XYZ (90, 180, 0).
- **Ponto:** meio do antebraço (+0,09 m em X) e 0,06 m para fora (+Z).
- **Tamanho:** 0,75 m, do joelho ao queixo no guarda de 1,50 m.
- **Conferido na folha `guarda_folhas.png`:** escudo preso ao antebraço nas 3 linhas e nos 24 quadros. Na antecipação do Bash ele fica de pé, e no impacto vai à frente do corpo. Não flutua nem atravessa o tronco. Re-conferido em 2026-10-10 com o Bash novo.

**Arco do arqueiro (`--attach`, conta de 2026-10-10):**
- **Pose do tiro:** o braço esquerdo aponta o alvo em eixos do mundo (−Y), com giro −90° no antebraço e na mão. A palma fica para dentro, segurando o punho do arco na vertical.
- **Arco do Tripo:** o comprido fica em Z (0,99 m). A corda fica em +X (as pontas curvam para +X) e o punho em −X.
- **Desfazendo o delta da mão:** no repouso em T, o comprido deve ir para −Y e a corda para −X (para o corpo). Isso dá Euler XYZ (90, 180, 0), o mesmo giro do escudo, por coincidência.
- **Ponto:** o centro da caixa fica entre o punho e a corda. O punho na palma (+0,035 m em X, −0,015 m em Z) põe o centro em (−0,06; 0; −0,015) a partir da cabeça da mão.
- **Tamanho:** 0,80 m.
- **Conferido na folha `arqueiro_folhas.png`:** no Shoot (W), o arco fica na mão esquerda, com a barriga para o alvo e a corda para o arqueiro. A mão direita puxa até a bochecha e escapa para trás na soltura. No Idle e no Walk, o arco pende do lado esquerdo.
