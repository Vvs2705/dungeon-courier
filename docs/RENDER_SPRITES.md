# RENDER_SPRITES — modelo 3D rigado → folhas de sprite 2D (Dungeon Courier)

## Estado em 2026-10-09

- **IMPLEMENTADO, NÃO EXECUTADO.**
- Os dois scripts foram adaptados do Forge Street (`03_FORGE_STREET/client/tools/render_sprites.py`). No Forge, eles foram testados com o placeholder do COE e renderizaram 20 conjuntos reais.
- Nesta raia só se conferiu a sintaxe: `py_compile` do `.py` e parser do PowerShell 5.1 no `.ps1`. Os dois arquivos são ASCII puro.
- Blender não foi rodado (PC de 7,7 GB com o Unity aberto). Nada foi gerado em `Assets/` e nenhum crédito foi gasto.
- **Primeiro passo ao retomar:** `.\client\tools\render_sprites.ps1 -Teste` (§6), antes de qualquer arte real.

**Arquivos:**
- `client/tools/render_sprites.py` (Blender 5.2 headless).
- `client/tools/render_sprites.ps1` (wrapper com timeout, saída nas pastas do DC).

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

## 2. Como usar

```
.\client\tools\render_sprites.ps1 -Teste                                   # placeholder do COE -> %TEMP%\dc_sprites\out
.\client\tools\render_sprites.ps1 -Nome courier -Model arte\tripo\courier\<fbx> -Anims arte\mixamo\courier -Hold arte\mixamo\Hold.fbx
.\client\tools\render_sprites.ps1 -Nome caixa -Model arte\tripo\caixa\<fbx>
.\client\tools\render_sprites.ps1 -Nome pilar -Preset none -Model <fbx> -Extra @("--width","1.2","--size","192","--scale","120","--outline","3")
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --factory-startup --python-exit-code 1 --python client\tools\render_sprites.py -- --model M.fbx --anims pasta\ --out saida\ --preset courier
```

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

## 3. Presets (`PRESETS` no `.py`)

Todos os presets usam `scale` 120, `outline` 3 e `max_frames` 12, exceto onde a tabela indica outro valor.

| Preset | Normalização | Direções (linhas) | Célula | Folha mais larga |
|---|---|---|---|---|
| `courier` | altura 1,30 m | 8 com espelho (5) | 224 | 12 × 224 = 2 688 px |
| `courier_fim` | altura 1,30 m | 1 | 320 | 3 840 px |
| `ladrao` | altura 1,00 m | 4 com espelho (3) | 160 | 1 920 px |
| `guarda` | altura 1,50 m | 4 com espelho (3) | 224 | 2 688 px |
| `cliente` | altura 1,35 m | 1 | 192 | 2 304 px |
| `bruto` | altura 1,80 m | 4 com espelho (3) | 256 | 3 072 px |
| `ogro` | altura 2,80 m | 4 com espelho (3) | 384 (max 10) | 3 840 px |
| `ogro_fim` | altura 2,80 m | 1 | 448 (max 9) | 4 032 px |
| `morcego` / `filhote` | largura 0,70 m / altura 0,50 m | 1 | 112 / 96 | — |
| `caixa` / `reliquia` / `ovo` | largura 0,50 m / altura 0,55 m / altura 0,55 m | 1 | 112 / 96 / 96 | — |
| `bau` / `bau_aberto` / `arco` | largura 0,80 / 0,80 / 2,20 m | 1 | 160 / 160 / 448 | — |

As células foram calculadas pela geometria de projeto com 10% de margem.
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
| `elev`, `yaw`, `light`, `outline_px`, `src_fps`, `preset`, `model_scale`, `hold` | rastreio |
| `clips[]` | `name`, `file`, `frames`, `loop` (pelo nome: idle, walk, run, lurk), `fps` (real do clipe), `duration_s`, `src_frames` e `hands` (só em ator com mãos) |

**`hands`** (por clipe): lista plana `[x, y, frente, x, y, frente, ...]`.
- Índice = `(linha * frames + quadro) * 3`.
- `x`, `y` = px na célula a partir do canto inferior esquerdo.
- `frente` = 1 se as mãos estão mais perto da câmera que o quadril (a carga desenha **acima** do portador), 0 se atrás (desenha **abaixo**).
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
4. **Limites e escala:** raio horizontal e extremos verticais pelos vértices reais de todos os quadros, válidos para qualquer giro. Com `--scale`, `ppu` é fixo e o script só avisa recorte. Pivô = origem.
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
