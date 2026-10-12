# RENDER_SPRITES — modelo 3D rigado → folhas de sprite 2D (Dungeon Courier)

## Estado em 2026-10-11, noite (raia G, Leva C — Lotes 5 e 6, dragão, ícone e roupas)

- **Todos com exit 0 e `SPRITES_OK`**, conferidos nas folhas (`client/Builds/sprites_contact/`): sem braço em T (exceções no §7), item preso à mão, margem ≥ 4 px (a menor é 6 px, num prop do Lote 5; nos personagens, 13 px, no Land do dragão), passada no `stride_mps`. Ainda sem aprovação visual do Vinicius.
  - **Lote 5 (região 3):** `esqueleto` (clava), `mumia`, `gargula`, `necromante` + `necromante_fim` (cajado); props `clava`, `cajado`, `fantasma`, `aranha` + `aranha_empinada`, `viuva` + `viuva_empinada`, `lanterna` + `lanterna_apagada`, `espelho`, `balao`, `lapide`, `vela`, `sarcofago`.
  - **Lote 6 (região 4):** `diabrete`, `kobold` (frasco), `magma` + `brasa` (o mesmo modelo a 1,10 m), `capataz` + `capataz_fim` (chicote); props `frasco`, `chicote`, `salamandra`, `planta` + `planta_aberta`, `mapa`, `bigorna`, `braseiro`, `respiro`, `corrente`.
  - **Dragão:** `dragao` + `dragao_fim`, rig refeito e 7 clipes no Blender (§10).
  - **Ajustes pedidos:** `arqueiro` re-renderizado com o Walk = correr para trás (0,59 → 1,40 m/s: 1,0× na Sim); `courier_icone` + `caixa_icone` para o `loja_assets.py` (§4); posição das costas do golem para o brilho da View (§7).
  - **Roupas do courier:** `courier_noturno`, `courier_real`, `courier_explorador`, `courier_forja`, `courier_fantasma` (os 7 clipes, troca de cor no mesmo modelo; §11). A Clássica é o `courier`.
- **Texturas** `piso_cripta`/`parede_cripta` e `piso_forja`/`parede_forja` copiadas cruas para `Resources/Textures/`; ainda sem `.meta` (o Unity cria ao abrir).
- **Scripts novos:** `dragao_blender.py` (§10), `planta_aberta.py` (§7) e `roupas_courier.py` (§11). No `render_sprites.py`: `footprint`, `tilt`, `--model` em `.blend`, actions do `.blend` com `--self`, laço `perch`/`hold`/`fly`, `ETAPA` no log (§1).
- **O que travou e como saiu:** o import do FBX do Tripo levou de 7 a 14 min por personagem com o PC sem memória, e múmia, gárgula e necromante pararam no corte de 900 s. Cada personagem passou a ser importado uma vez e salvo em `.blend`; com ele, o render de um personagem leva de 3 a 12 s (§2).

**Correspondência com o `CONTEUDO_REGIOES.md` §12.2–12.3 e a View (`WorldView.EnemySheet`):** os nomes são os do zip e a View já procura todos (`kobold`, `magma`, `brasa`, `capataz`, `dragao` são o 1º nome de cada par). Clipes com os nomes que a View pede (`WorldViewElenco.AnimRegiao`): `Statue`, `Wake`, `Dive`, `Perch`, `Hold`, `Grab`, `Rise`, `Cast`, `Summon`, `Swap`, `Scratch`, `Stun`, `Toss`, `Smash`, `Punch`, `Whip`, `Spin`, `Call`, `Breath`, `Fly`, `Land`. O `Vanish` do diabrete e o `Split` do magma ainda não são chamados pela View.

## Estado em 2026-10-11 (raia G, Lote 4 — região 2 "Minas Alagadas")

- **15 conjuntos novos, todos com exit 0 e `SPRITES_OK`**, conferidos nas folhas: sem braço em T, picareta presa à mão, margem ≥ 4 px (a menor é 10 px), passada no `stride_mps`. Ainda sem aprovação visual do Vinicius.
  - Personagens: `goblin_mineiro` (picareta na mão direita por `--attach`), `toupeira`, `rainha_toupeira` + `rainha_toupeira_fim`, `golem` + `golem_fim`.
  - Props: `picareta`, `sapo`, `besouro`, `vagonete`, `coracao`, `cristal`, `gaiola`, `lanterna_mina`, `escora`.
- **Clipes:** reuso do disco (Breathing Idle, Looking Around, ARKANA andar/correr para a frente e para trás, Death Backward, o Slam e o Roar do ogro, o Hit) e 9 clipes novos no Blender (§9).
- **No `render_sprites.py`:**
  - `--align-arms` liga sozinho quando o braço do rig está a mais de 15° do clipe (toupeira 32°, rainha 28°, golem 22°).
  - `ground_cut` corta no chão a parte enterrada do Emerge/Burrow.
  - O laço por nome passou a incluir `look`, `sleep`, `stun` e `charge`.
- **Texturas** `piso_mina.png` e `parede_mina.png` copiadas cruas para `Resources/Textures/`, como `piso`/`parede`. As opções de import delas ficam para quem abrir o Unity.

**Nomes:** a pasta e o preset seguem o nome do zip. Correspondência com o `CONTEUDO_REGIOES.md` §12.1 (raia M):

| `CONTEUDO_REGIOES` §12.1 | Pasta `Resources/Sprites/` | Observação |
|---|---|---|
| `mineiro` | `goblin_mineiro` | `Walk` = **correr para trás** (recua; o andar para trás dava 3,9× e patinava) e **`Run` a mais** = correr para a frente (o mineiro também persegue) |
| `rainha` / `rainha_fim` | `rainha_toupeira` / `rainha_toupeira_fim` | `Call` = o `Roar` do ogro, como pede o §12.1 |
| `toupeira`, `golem` / `golem_fim`, `picareta`, `sapo`, `besouro`, `vagonete`, `coracao`, `cristal`, `gaiola`, `lanterna_mina`, `escora` | o mesmo nome | `golem_fim/Dying` = o clipe `Collapse` do Blender |

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
| `align_arms` automático (2026-10-11) | Sem a opção, o retarget mede o ângulo entre o braço do rig e o do clipe. Acima de 15°, alinha e escreve `AVISO rig em pose A` no log; o meta registra `align_arms`. Os rigs em T (≤ 9°) não mudam. Evita uma importação só para inspecionar o rig (2 a 10 min cada, com o PC sem memória) |
| `ground_cut` no preset (2026-10-11) | Clipes que entram e saem do chão (Emerge/Burrow da toupeira e da rainha). A malha é cortada em z = 0 (bmesh `bisect_plane`), e a parte enterrada não entra na folha nem nos limites. O contorno fecha a borda do buraco. O clipe vai com `"ground_cut": true` no meta, e a View não precisa recortar |
| Laço por nome com `look`, `sleep`, `stun`, `charge` (2026-10-11) | Look da toupeira, Sleep do mineiro, Stun e Charge do golem são laços |
| Laço por nome com `perch`, `hold` e `fly` (Lotes 5 e 6) | Perch da gárgula, Hold da múmia (segura o courier) e Fly do dragão são laços. O `Hold.fbx` do `--hold` continua fora da pasta `-Anims`, então não vira clipe |
| `footprint=(largura, comprimento)` no preset (Lote 5) | Escala **não uniforme** da planta: o eixo longo do modelo vai para Y, o comprimento vira `comprimento` e a largura e a altura viram `largura`. O sarcófago do Tripo (~1,4:1) veste o obstáculo de 0,8 × 1,8 m da sala R3-5 sem crédito novo. O log escreve `PLANTA ...` |
| `tilt=graus` no preset (Lote 5) | Depois da escala, gira a frente (−Y) para cima em volta de X e apoia o ponto mais baixo no chão. Faz a aranha e a viúva **empinadas** (35° e 30°) a partir do mesmo modelo, sem crédito. O log escreve `EMPINA ...` |
| `--model` aceita `.blend` (Lote 5) | Com o PC sem memória (0,3 a 0,7 GB livres), o import do FBX do Tripo passou de 10 min e o `.ps1` cortou três personagens em 900 s. O FBX é importado uma vez e salvo em `.blend` (comando no §2); o render lê o `.blend` (`bpy.data.libraries.load`) em segundos. O resultado é o mesmo do FBX: são os mesmos objetos, salvos logo depois do import (`model_scale` do esqueleto 1,4770 pelos dois caminhos). As actions do `.blend` também vêm, e com `--self` viram os clipes (o dragão, §10) |
| `ETAPA` no log (Lote 5) | Linhas `ETAPA modelo/clipes/limites <s>` e saída sem buffer: se o `.ps1` cortar no timeout, o log mostra onde o tempo foi |

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

**Comandos do Lote 4 (2026-10-11).** FBX e `.fbm` (só o basecolor) extraídos dos zips para `%TEMP%\dc_in\<nome>\`; os zips não foram tocados. Rodados em sequência por um script de lote, um Blender por vez.

```
blender.exe -b --factory-startup --python-exit-code 1 --python client\tools\anim_blender.py -- --rig %TEMP%\dc_arte\mixamo\courier\Idle.fbx --out %TEMP%\dc_anim_l4 --only PickThrow,Sleep,Wake,Emerge,Burrow,Swipe,Charge,Stun,Collapse
.\client\tools\render_sprites.ps1 -Nome goblin_mineiro -Model $env:TEMP\dc_in\goblin_mineiro\tripo_convert_4af31bf3-....fbx -Anims $env:TEMP\dc_arte\mixamo\goblin_mineiro `
    -Extra @("--attach", "$env:TEMP\dc_in\picareta\tripo_convert_828ff8b7-....fbx:RightHand:-0.04,-0.229,-0.02:31.6,-67.3,98.4:0.45")
.\client\tools\render_sprites.ps1 -Nome toupeira            -Model $env:TEMP\dc_in\toupeira\tripo_convert_5144e7cf-....fbx        -Anims $env:TEMP\dc_arte\mixamo\toupeira
.\client\tools\render_sprites.ps1 -Nome rainha_toupeira     -Model $env:TEMP\dc_in\rainha_toupeira\tripo_convert_e0f11c2e-....fbx -Anims $env:TEMP\dc_arte\mixamo\rainha_toupeira
.\client\tools\render_sprites.ps1 -Nome rainha_toupeira_fim -Model (o da rainha)                                                  -Anims $env:TEMP\dc_arte\mixamo\rainha_toupeira_fim
.\client\tools\render_sprites.ps1 -Nome golem               -Model $env:TEMP\dc_in\golem\tripo_convert_a14abc19-....fbx           -Anims $env:TEMP\dc_arte\mixamo\golem
.\client\tools\render_sprites.ps1 -Nome golem_fim           -Model (o do golem)                                                   -Anims $env:TEMP\dc_arte\mixamo\golem_fim
.\client\tools\render_sprites.ps1 -Nome picareta -Model $env:TEMP\dc_in\picareta\tripo_convert_828ff8b7-....fbx  (sapo, besouro, vagonete, coracao, cristal, gaiola, lanterna_mina, escora: idem)
```

**Comandos do Lote 5 (2026-10-11).** Mesma extração em `%TEMP%\dc_in\<nome>\`. Com o PC sem memória, cada personagem foi antes convertido para `.blend` (um Blender por vez, também com corte de 900 s):

```
blender.exe -b --factory-startup --python-exit-code 1 --python-expr "import bpy,sys; a=sys.argv[sys.argv.index('--')+1:]; bpy.ops.wm.read_factory_settings(use_empty=True); bpy.context.scene.render.fps=30; bpy.ops.import_scene.fbx(filepath=a[0]); bpy.ops.file.pack_all(); bpy.ops.wm.save_as_mainfile(filepath=a[1])" -- %TEMP%\dc_in\esqueleto\tripo_convert_c1aedaea-....fbx %TEMP%\dc_in\esqueleto\esqueleto.blend
blender.exe -b --factory-startup --python-exit-code 1 --python client\tools\anim_blender.py -- --rig %TEMP%\dc_arte\mixamo\courier\Idle.fbx --out %TEMP%\dc_anim_l5 --only SkelAttack,BoneCollapse,BoneRise,MummyGrab,MummyHold,GargStatue,GargWake,GargDive,GargPerch,NecroIdle,NecroCast,NecroSummon
.\client\tools\render_sprites.ps1 -Nome esqueleto  -Model $env:TEMP\dc_in\esqueleto\esqueleto.blend -Anims $env:TEMP\dc_arte\mixamo\esqueleto `
    -Extra @("--attach", "$env:TEMP\dc_in\clava\tripo_convert_....fbx:RightHand:-0.04,-0.25,-0.02:31.9,44.1,-78.0:0.49")
.\client\tools\render_sprites.ps1 -Nome mumia      -Model $env:TEMP\dc_in\mumia\mumia.blend          -Anims $env:TEMP\dc_arte\mixamo\mumia
.\client\tools\render_sprites.ps1 -Nome gargula    -Model $env:TEMP\dc_in\gargula\gargula.blend      -Anims $env:TEMP\dc_arte\mixamo\gargula
.\client\tools\render_sprites.ps1 -Nome necromante -Model $env:TEMP\dc_in\necromante\necromante.blend -Anims $env:TEMP\dc_arte\mixamo\necromante `
    -Extra @("--attach", "$env:TEMP\dc_in\cajado\tripo_convert_....fbx:RightHand:-0.04,0.251,-0.02:-180,90,90:1.40")
.\client\tools\render_sprites.ps1 -Nome necromante_fim -Model (o mesmo .blend) -Anims $env:TEMP\dc_arte\mixamo\necromante_fim -Extra (o mesmo cajado)
.\client\tools\render_sprites.ps1 -Nome arqueiro   -Model $env:TEMP\dc_in\arqueiro\arqueiro.blend    -Anims $env:TEMP\dc_arte\mixamo\arqueiro -Extra (o arco do Lote 3)
.\client\tools\render_sprites.ps1 -Nome courier_icone -Model $env:TEMP\dc_in\courier\courier.blend  -Anims $env:TEMP\dc_arte\mixamo\courier_icone -Hold $env:TEMP\dc_arte\mixamo\Hold.fbx
.\client\tools\render_sprites.ps1 -Nome caixa_icone -Model $env:TEMP\dc_in\caixa\tripo_convert_dc019326-....fbx
.\client\tools\render_sprites.ps1 -Nome aranha_empinada -Model $env:TEMP\dc_in\aranha\tripo_convert_....fbx   (viuva_empinada: o FBX da viúva)
.\client\tools\render_sprites.ps1 -Nome lanterna_apagada -Model $env:TEMP\dc_in\lanterna_apagada\tripo_convert_8b1eb15f-....fbx   (cópia da lanterna com o basecolor editado, §7)
.\client\tools\render_sprites.ps1 -Nome clava -Model $env:TEMP\dc_in\clava\tripo_convert_....fbx   (cajado, fantasma, aranha, viuva, lanterna, espelho, balao, lapide, vela, sarcofago: idem)
```

**Comandos do Lote 6, do dragão e das roupas (2026-10-11, noite).** A conversão para `.blend` dos personagens do Lote 6 passou a importar sem a animação do modelo (`use_anim=False` no `import_scene.fbx`: o render troca a action de qualquer jeito). O kobold estourou os 900 s com a animação e passou em 626 s sem ela; o resto do tempo é o import da malha com pele.

```
blender.exe -b --factory-startup --python-exit-code 1 --python client\tools\anim_blender.py -- --rig %TEMP%\dc_arte\mixamo\courier\Idle.fbx --out %TEMP%\dc_anim_l6 --only ImpSwap,ImpVanish,KoboldToss,MagmaSplit,ForemanWhip,ForemanSpin
.\client\tools\render_sprites.ps1 -Nome diabrete -Model $env:TEMP\dc_in\diabrete\diabrete.blend -Anims $env:TEMP\dc_arte\mixamo\diabrete
.\client\tools\render_sprites.ps1 -Nome kobold   -Model $env:TEMP\dc_in\kobold\kobold.blend     -Anims $env:TEMP\dc_arte\mixamo\kobold `
    -Extra @("--attach", "$env:TEMP\dc_in\frasco\tripo_convert_5e5251a0-....fbx:RightHand:-0.05,0,-0.03:90,0,0:0.30")
.\client\tools\render_sprites.ps1 -Nome magma    -Model $env:TEMP\dc_in\magma\magma.blend       -Anims $env:TEMP\dc_arte\mixamo\magma
.\client\tools\render_sprites.ps1 -Nome brasa    -Model $env:TEMP\dc_in\magma\magma.blend       -Anims $env:TEMP\dc_arte\mixamo\brasa
.\client\tools\render_sprites.ps1 -Nome capataz  -Model $env:TEMP\dc_in\capataz\capataz.blend   -Anims $env:TEMP\dc_arte\mixamo\capataz `
    -Extra @("--attach", "$env:TEMP\dc_in\chicote\tripo_convert_39a6f622-....fbx:RightHand:-0.04,-0.276,-0.02:1.1,9.0,153.2:0.60")
.\client\tools\render_sprites.ps1 -Nome capataz_fim -Model (o mesmo .blend) -Anims $env:TEMP\dc_arte\mixamo\capataz_fim -Extra (o mesmo chicote)
blender.exe -b --factory-startup --python-exit-code 1 --python client\tools\planta_aberta.py -- %TEMP%\dc_in\planta\tripo_convert_779e2e97-....fbx %TEMP%\dc_in\planta_aberta\planta_aberta.blend
.\client\tools\render_sprites.ps1 -Nome planta_aberta -Model $env:TEMP\dc_in\planta_aberta\planta_aberta.blend
.\client\tools\render_sprites.ps1 -Nome frasco -Model $env:TEMP\dc_in\frasco\tripo_convert_5e5251a0-....fbx   (chicote, salamandra, planta, mapa, bigorna, braseiro, respiro, corrente: idem)
(dragão: §10)
blender.exe -b --factory-startup --python-exit-code 1 --python client\tools\roupas_courier.py -- %TEMP%\dc_in\courier\courier.blend %TEMP%\dc_in\roupas
.\client\tools\render_sprites.ps1 -Nome courier_noturno -Preset courier -Model $env:TEMP\dc_in\roupas\courier_noturno.blend -Anims $env:TEMP\dc_arte\mixamo\courier -Hold $env:TEMP\dc_arte\mixamo\Hold.fbx   (real, explorador, forja, fantasma: idem)
```

O lote rodou por um script que renderiza em `<nome>_novo` e só no fim copia PNG + `meta.json` por cima da pasta (os `.meta` do Unity ficam): um commit de segurança no meio do render não pega um conjunto pela metade.

**Tempo medido** (PC de 7,7 GB):
- Prop: 3 a 30 s.
- Personagem: 2 a 3,5 min em 2026-10-09; de 2 a 9 min em 2026-10-10 (122 a 532 s), com ~1 GB livre; de 5,5 a 11 min em 2026-10-11 (321 a 665 s), com 0,5 a 0,9 GB livre (o Blender chegou a ficar paginado, com 1 s de CPU a cada 10 s).
- O tempo vai quase todo no import do FBX do Tripo, que tem malha com pele. O `.ps1` corta em 900 s.
- **Desde 2026-10-11 (noite), com o `.blend`:** a conversão leva de 434 a 834 s por personagem (uma vez só) e o render de um personagem, de 3 a 12 s (`ETAPA modelo 0 s`). O import do FBX gasta CPU (mais de 200 s de CPU no necromante, disputando o processador com as outras raias), não só memória: as page faults ficaram baixas.

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
| `goblin_mineiro` | altura 1,00 m | 4 com espelho (3) | 192 | 2 304 px | Idle 199–289; picareta pelo `--attach` na linha |
| `toupeira` | altura 0,90 m | 4 com espelho (3) | 192 (160 recortava) | 2 304 px | Idle 199–289; Emerge e Burrow 6 quadros e `ground_cut`; rig em pose A (automático) |
| `rainha_toupeira` | altura 1,60 m | 4 com espelho (3) | 304 | 3 648 px | Idle 199–289; Emerge e Burrow 6 quadros e `ground_cut`; rig em pose A (automático) |
| `rainha_toupeira_fim` | altura 1,60 m | 1 | 320 (384 × 12 passava de 4 096) | 3 840 px | rig em pose A (automático) |
| `golem` | altura 2,60 m | 4 com espelho (3) | 448, máx. 9 | 4 032 px | Idle 199–289; rig em pose A (automático) |
| `golem_fim` | altura 2,60 m | 1 | 576, máx. 7 | 4 032 px | rig em pose A (automático) |
| `picareta` / `coracao` / `gaiola` / `lanterna_mina` | altura 0,45 / 0,45 / 0,50 / 0,50 m | 1 | 96 | — | Static |
| `sapo` / `besouro` / `vagonete` / `cristal` / `escora` | largura 0,90 / 0,60 / 1,20 / 0,50 / 1,20 m | 1 | 144 / 112 / 208 / 96 / 176 | — | Static |
| `esqueleto` | altura 1,35 m | 4 com espelho (3) | **256** (224 recortava a clava no alto e o monte do Collapse) | 3 072 px | Idle 199–289; Collapse e Rise 6 quadros; clava pelo `--attach` na linha |
| `mumia` | altura 1,50 m | 4 com espelho (3) | **256** (224 recortava o bote do Grab) | 3 072 px | Idle 199–289; Hold 8 quadros; rig a 16° (alinhado sozinho) |
| `gargula` | altura 1,40 m | 4 com espelho (3) | **288** (256 recortava o Dive) | 3 168 px | Statue 1 quadro |
| `necromante` / `necromante_fim` | altura 1,60 m | 4 com espelho (3) / 1 | 320 / 320 | 3 840 px | cajado pelo `--attach` na linha (nos dois) |
| `clava` / `cajado` | largura 0,49 m / altura 1,40 m | 1 | 96 / 144 | — | Static (o item solto; na mão vem do `--attach`) |
| `fantasma` / `lanterna` / `lanterna_apagada` / `espelho` / `vela` | altura 0,90 / 0,45 / 0,45 / 0,50 / 0,70 m | 1 | 128 / 96 / 96 / 96 / 112 | — | Static; `lanterna_apagada` = o mesmo modelo com a chama pintada de vidro escuro no basecolor (§7) |
| `aranha` / `aranha_empinada` | largura 0,80 m | 1 | 128 / 144 | — | Static; `tilt` 35° |
| `viuva` / `viuva_empinada` | largura 2,00 m | 1 | 320 / 352 | — | Static; `tilt` 30° |
| `balao` / `lapide` | largura 0,60 / 1,20 m | 1 | 128 / 176 | — | Static |
| `sarcofago` | `footprint` 0,80 × 1,80 m | 4 com espelho (3) | 288 | 288 × 864 px | Static em 3 linhas: S/N = comprido em profundidade, W = atravessado (as duas orientações do obstáculo) |
| `courier_icone` | altura 1,30 m, **320 px/m** | 1, câmera a 35° e −45° (3/4 de frente) | 512 | 512 px | CarryIdle 1 quadro (janela 199–289) com `--hold`; contorno 6 px. Para o ícone da loja (`loja_assets.py`) |
| `caixa_icone` | largura 0,50 m, 320 px/m | 1, 35° e −45° | 256 | — | Static; a caixa na mesma escala e câmera do `courier_icone` |
| `diabrete` | altura 0,90 m | 4 com espelho (3) | 192 | 2 304 px | Idle 199–289; Swap e Vanish 6 quadros |
| `kobold` | altura 1,00 m | 4 com espelho (3) | 192 | 2 304 px | Idle 199–289; frasco pelo `--attach` na linha; rig a 16° (alinhado sozinho) |
| `magma` / `brasa` | altura 1,90 / 1,10 m (o mesmo modelo) | 4 com espelho (3) | 336 / 208 | 4 032 / 2 496 px | Idle 199–289; rig a 23° (alinhado sozinho) |
| `capataz` / `capataz_fim` | altura 1,40 m | 4 com espelho (3) / 1 | 256 / 320 | 3 072 / 3 840 px | Idle 199–289; chicote pelo `--attach` na linha (nos dois) |
| `dragao` / `dragao_fim` | largura 3,50 m (comprimento) | 4 com espelho (3) / 1 | 576, máx. 7 | 4 032 px | Idle 4, Fly 6, Land 6 quadros; `--self` (clipes no `.blend`, §10); `_fim` com `ground_cut` no Dying |
| `frasco` / `chicote` / `salamandra` / `mapa` | altura 0,30 m / largura 0,60 / 0,80 / 0,45 m | 1 | 96 / 112 / 128 / 96 | — | Static |
| `planta` / `planta_aberta` | altura 0,55 / **0,596** m (a mesma escala: o lóbulo aberto sobe a altura do modelo) | 1 | 112 / 128 | — | Static; a aberta vem do `planta_aberta.py` (§7) |
| `bigorna` / `braseiro` / `respiro` / `corrente` | largura 1,20 m / altura 0,70 m / largura 2,00 m / altura 1,50 m | 1 | 176 / **176** (112 recortava a bacia) / 272 / 160 | — | Static; a bigorna veste o obstáculo 1,2 × 0,8 da forja |
| `courier_<roupa>` (noturno, real, explorador, forja, fantasma) | o preset `courier` (`-Preset courier`) | 8 com espelho (5) | 224 | 2 688 px | os 7 clipes do courier; modelo do `roupas_courier.py` (§11) |

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
| `clips[]` | `name`, `file`, `frames`, `loop` (pelo nome: idle, walk, run, lurk, look, sleep, stun, charge, perch, hold), `fps` (real do clipe), `duration_s`, `src_frames`, `hands` (só em ator com mãos), `stride_mps` (opcional, ver abaixo), `src_range` (opcional, 2026-10-10: [início, fim] do clipe de origem quando o preset usa só uma faixa) e `ground_cut` (opcional, 2026-10-11: `true` quando a parte abaixo do chão já foi cortada no render) |

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

**Ícone da loja (`courier_icone` + `caixa_icone`, 2026-10-11)** — para o `client/tools/loja_assets.py`, que hoje compõe o ícone com o CarryIdle do `courier` (224, 120 px/m) e a caixa:
- `courier_icone/CarryIdle.png`: 1 quadro de 512 × 512, o courier segurando a carga em 3/4 de frente (câmera a 35° e −45°: ele olha para a frente-esquerda da tela), **320 px/m**, contorno de 6 px, sem a caixa. Pivô (256; 105,3) px.
- `caixa_icone/Static.png`: a caixa de 256 × 256 na mesma câmera e escala (320 px/m, contorno 6 px), com pivô no centro de baixo como os outros props (128; 68,3).
- **Composição:** a caixa vai no ponto `hands` do meta do ícone: `clips[0].hands = [195.5, 268.2, 1]` (x, y em px a partir do canto inferior esquerdo da célula; frente = 1 → a caixa por cima do courier). É a mesma conta da View (§4, `hands`), só que com a escala de 320 px/m nas duas folhas.
- Medidas (alfa, px a partir do canto inferior esquerdo): o courier ocupa x 126–353 e y 50–462 da célula; a caixa, 228 × 244 px na célula dela. Centrada no ponto `hands`, a caixa vai de x ~81 a ~310, então o recorte do ícone composto começa à esquerda do courier (x ~80).

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
- **Rig em pose A:** desde 2026-10-11 o render alinha sozinho acima de 15° e avisa no log (bruto −49,5°, ogro −22°, toupeira 32°, rainha 28°, golem 22°; arqueiro −9°, capitão −3° e mineiro −4° ficam como estão).
- **Andar para trás é lento para ator pequeno:** o Walking Backwards do ARKANA dá 0,89 m/s no X Bot e 0,36 m/s no goblin de 1,0 m. A 1,4 m/s da Sim, seriam 3,9× (teto da View 2,3×). O mineiro usa o Running Backwards (0,86 m/s → 1,63×). O arqueiro (Lote 3) continua com o andar para trás (2,38×); a troca é o mesmo arquivo e um re-render.
- **Golem do Tripo diferente do prompt:** a malha não tem os cristais ciano nas costas (0 pixel ciano no basecolor). Tem uma gema âmbar no **peito** e a pedra é bege, não cinza-azulada. O ponto fraco do Core é **por trás** (×2), e o sprite sugere o contrário. Precisa de um brilho procedural nas costas na View ou de um modelo novo.
  - **Decisão (2026-10-11, coordenador):** a View desenha o brilho por código; o modelo fica. **Onde fica o ponto das costas na folha do `golem`** (célula 448, 120 px/m, pivô em (224; 145,8) px), estimado pela geometria: altura do `Spine2` ≈ 1,6 m e 0,35 m atrás do eixo, projetado a 60°. **Deslocamento a partir do pivô, em px da célula (x para a direita, y para cima):**
    - linha N (de costas para a câmera): (0; +60), **desenhar por cima** do sprite;
    - linha W (de perfil, olhando para a esquerda): (+42; +96), desenhar por cima (fica na borda das costas);
    - linha S (de frente): (0; +132), **atrás do corpo**: desenhar por baixo do sprite ou não desenhar;
    - E (espelho do W): (−42; +96).
  - É estimativa geométrica (±10 px: o tronco balança no Walk e se curva no Slam e no Charge). Se a View precisar do ponto exato por quadro, o caminho é um campo como o `hands` com o osso `Spine2` deslocado, num re-render do golem.
- **Sentado visto de frente a 60°:** as pernas para a câmera descem na tela, e o mineiro dormindo (Sleep) e o golem atordoado (Stun) quase não diminuem de altura na linha S. A leitura de "sentado" vem do perfil e do tronco caído. Os joelhos do Stun foram dobrados para a perna não passar da célula.
- **O `--attach` usa o repouso:** deslocamento e giro valem na pose em T. A conta para o escudo está em §9; se a pose do braço do guarda mudar, refaça a conta e confira a folha.
- **Chapéu do courier a 60°:** a aba larga cobre o tronco nas vistas S e N, e na corrida S só se veem o chapéu e um pé. É do modelo e da câmera (`ART_BIBLE.md` §2), não do clipe. Com a caixa por cima (frente = 1), a leitura "courier com carga" continua.
- **Morte de costas parece de pé a 60° (Lotes 5 e 6):** o corpo deitado ao longo da profundidade (pés para a câmera) fica com 0,87 × o comprimento na tela, mais alto que o ator de pé (0,5 × a altura). No fim do Dying do `capataz_fim` e do `necromante_fim`, o boneco deitado lê como "em pé de braços abertos". É o mesmo clipe do `courier_fim` e do `ogro_fim`. A View pode baixar o alfa ou girar o sprite no fim, se incomodar.
- **Tiras soltas da múmia:** o modelo tem tiras de bandagem penduradas nos braços, rígidas no osso. Com o braço erguido à frente, elas saíam para os lados como varetas (lia como braço em T). O Grab gira o braço e o antebraço no próprio eixo (−26° a −45°) para as tiras penderem para baixo.
- **Asas da gárgula:** não são puxadas pelos ombros. No Hit, as asas dobradas aparecem de quina na linha S (duas barras finas acima dos ombros), que é a silhueta do modelo de frente. No Wake, os braços sobem em V estreito (0,25 para o lado): mais aberto, de costas parecia um T.
- **Cajado do necromante (o giro da mão):** o cajado preso à mão fica em pé quando a mão aponta para a frente com giro de +90° (o Idle). Com a mão apontando outra direção, o giro que mantém o cajado no lugar muda, porque o `aim` parte do arco mínimo desde o repouso. A conta (numpy, fora do Blender) deu: mão para baixo e à frente (0; −0,3; −0,95) com giro 17,5° = cajado apontado à frente com o crânio 17° acima (o disparo do Cast); mão (0; −1; 0,1) com giro 95,7° = cajado em pé com o antebraço para o alto (o Summon). Com 90° fixo, o cajado ficava deitado de lado.
- **Lanterna apagada:** troca de material = o basecolor com a chama pintada de vidro escuro (o laranja vira cinza; o metal fica). A 60°, a chama quase não aparece (o globo é visto de cima), então a diferença entre acesa e apagada é sutil no sprite. A leitura forte é a luz da View.
- **Sarcófago esticado:** a planta 0,8 × 1,8 m é escala não uniforme (o modelo é ~1,4:1): a tampa e o rosto entalhado ficam ~60% mais compridos em proporção (2,25:1 contra ~1,4:1). Linhas S e N: comprido em profundidade; W: atravessado (E = espelho). Cobre o obstáculo em pé e deitado da sala R3-5.
- **Planta de boca aberta:** o modelo do Tripo já veio de boca entreaberta. A `planta_aberta` escancara: o lóbulo de cima gira 28° para cima em volta da dobradiça de trás (y 0,07; z 0,75 no FBX), com transição de 5 cm (z 0,73–0,78) para não rasgar a malha. 869 dos 3 923 vértices se movem.
- **Necromante:** o Idle também é do Blender. Com o braço caído do Breathing Idle, o cajado ficava deitado. O Cast e o Summon não usam os clipes de magia do ARKANA sugeridos pela raia T, pelo mesmo motivo e pelo quadro do disparo.
- **Giro do Capataz (Spin):** o braço do chicote vai esticado à frente-baixo enquanto o corpo gira; nos quadros de lado (linhas S e N no meio do giro) ele aparece horizontal. É o golpe, não um T de repouso.
- **Roupas do courier:** a Noturno (anil escuro, couro quase preto) é a de menor contraste com o piso escuro; o contorno de 3 px segura a silhueta. A Fantasma é a textura inteira em cinza-ciano claro; a transparência fica na View (a `TintaRoupa` dela tem alfa 0,6; com a folha `courier_fantasma` a View passa a usar branco e perde o alfa, então convém manter o alfa só para essa roupa).

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
| `PickThrow` → `goblin_mineiro/Throw` | 30 (0,97) → 12 | mineiro | As poses do `Throw` do courier, retimadas: pronto → antecipação (5) → carregado (10–15 = fim do telegraph de 0,9 s no quadro 6) → **solta a picareta (17, "out") = quadro 7** (`round(0,6 × 12)`) → acompanha (21) → volta (25) → pronto (29) |
| `Sleep` | 61 (2,00), laço → 12 | mineiro dormindo | Sentado no chão, pernas à frente, encostado para trás, cabeça caída de lado; respira (0 → 30 → 60 = 0) |
| `Wake` | 13 (0,40) → 5 | mineiro acorda | Dormindo → endireita de susto, braços à frente (3, "out") → agacha para levantar (7) → pronto (12) |
| `Emerge` | 10 (0,30) → 6 (preset) | toupeira e rainha | **Sem trava do pé (`FREE`)**: enterrado (quadril −1,75 m no X Bot) com as garras para cima (0) → metade fora (3) → salta acima do chão com as garras abertas (6, "out") → pose de garras (9). No render, `ground_cut` |
| `Burrow` | 10 (0,30) → 6 (preset) | toupeira e rainha | O inverso, sem trava do pé: pose de garras → pula e mergulha de cabeça com as garras à frente (3) → metade dentro (6) → some (9) |
| `Swipe` | 30 (0,97) → 12 | rainha | Garras → ergue a garra direita para trás e para cima, tronco torcido (5–12 = fim do telegraph) → **patada cruzando o corpo com passo (15, "out") = quadro 6** (`0,5 × 12`) → passa do ponto (19) → recolhe (24) → garras (29) |
| `Charge` | 21 (0,67), laço → 8 | golem (investida, fase 2) | 2 passos no lugar, tronco a 35° à frente, braço oposto à perna, braços junto do corpo (abertos, liam como T com os ombros largos do golem) |
| `Stun` | 41 (1,33), laço → 9 | golem atordoado | Sentado, joelhos para cima, braços apoiados à frente, cabeça rodando (0 → 10 → 20 → 30 → 40 = 0) |
| `Collapse` → `golem_fim/Dying` | 46 (1,50) → 7 | golem morre | Cambaleia para trás com os braços à frente (8, "out") → os joelhos cedem (16) → cai à frente nos punhos (26) → de bruços (36, "out") → assenta (45) |
| `Call` | 31 (1,00) → 12 | capitão (`CaptainCall`); Capataz (o apito) | Pronto → inspira, mão direita à boca (6) → braço esquerdo sobe pela frente (9) → **grita: mão em concha na boca e braço esquerdo erguido (12, "out")** → acena para fora (18) e para dentro (24) → desce o braço pela frente (27) → pronto (30) |
| `SkelAttack` → `esqueleto/Attack` | 30 (0,97) → 12 | esqueleto (clava) | Pronto → a clava sobe pela frente (2) → no alto atrás da cabeça, tronco torcido (5–12 = telegraph) → **pancada no chão com passo (15, "out") = quadro 6** → passa do ponto (19) → recolhe (24) → pronto (29) |
| `BoneCollapse` / `BoneRise` → `Collapse` / `Rise` | 10 (0,30) → 6 | esqueleto | Desaba: tranco para trás (2) → agacha (5) → monte agachado com as mãos no chão (7 = 9). O Rise é o inverso; o último quadro de um é o primeiro do outro |
| `MummyGrab` → `mumia/Grab` | 30 (0,97) → 12 | múmia | Curvada, braços à frente → **braços erguidos à frente** e girados (−26°/−41°) para as tiras penderem (6–15 = telegraph) → **bote em passo longo, braços esticados (17, "out") = quadro 7** → segura (21) → volta (25–29) |
| `MummyHold` → `mumia/Hold` | 21 (0,67), laço → 8 | múmia segurando o courier | Passo à frente, braços à frente apertando (0 → 10 → 20 = 0) |
| `GargStatue` / `GargPerch` | 10 / 25 (laço) → 1 / 10 | gárgula no pedestal / pousada | Agachada com as mãos no chão à frente; o Perch ofega (0 → 12 → 24 = 0) |
| `GargWake` → `gargula/Wake` | 28 (0,93) → 11 | gárgula acorda (aviso de 0,9 s) | Agachada → levanta com a cabeça para trás (4) → **braços em V estreito no alto (10–18)** → inclina para o mergulho com os braços para trás (23–27) |
| `GargDive` → `gargula/Dive` | 16 (0,53) → 6 | gárgula no ar (`FREE`, sem pé no chão) | Corpo na horizontal a ~0,45 m (X Bot) do chão, pernas para trás, garras à frente; balança pouco |
| `NecroIdle` → `necromante/Idle` | 61 (2,0), laço → 12 | necromante | Cajado em pé à frente (antebraço à frente, mão com giro +90°), respira |
| `NecroCast` → `necromante/Cast` | 30 (0,97) → 12 | necromante (raio) | Ergue o cajado em pé (8–22 = telegraph) → **aponta o cajado com o crânio à frente, em passo (24, "out") = quadro 10** (`round(0,8 × 12)`) → segura (27) → volta (29) |
| `NecroSummon` → `necromante/Summon` | 37 (1,2) → 12 | necromante ergue os esqueletos | Abaixa (8) → os braços sobem pela frente (12) → **cajado em pé acima da cabeça, as duas mãos (16–24)** → desce pela frente (28) → pronto (36) |
| `ImpSwap` → `diabrete/Swap` | 13 (0,40) → 6 | diabrete (redemoinho na carga) | Gira 90° por quadro amostrado (o quadril em Z; o pé segue a canela), agachando e encolhendo os braços; termina com os braços no alto |
| `ImpVanish` → `diabrete/Vanish` | 16 (0,50) → 6 | diabrete pisca (aviso de 0,5 s) | Agacha com os braços para trás (5) → salta encolhido (10, "out") → mais alto (15). `FREE`; a View apaga com alfa |
| `KoboldToss` → `kobold/Toss` e `capataz/Toss` | 30 (0,97) → 12 | kobold (frasco na mão direita), Capataz | Pronto → braço desce para trás (5) → carregado agachado (10–15 = telegraph) → passa por baixo (16) → **solta à frente e para o alto (17, "out") = quadro 7** (`round(0,6 × 12)`) → acompanha (21) → volta (25–29) |
| `MagmaSplit` → `magma/Split` | 19 (0,60) → 7 | magma estoura em brasas | Cambaleia para trás (4, "out") → encolhe com os braços cruzados (9) → braços sobem pela frente (11) → **estoura com os braços em V à frente (14–18)**; a View troca pelas brasas |
| `ForemanWhip` → `capataz/Whip` | 30 (0,97) → 12 | Capataz (chicote na mão direita) | As poses do `SkelAttack` retimadas: o chicote sobe pela frente (2) → no alto atrás (6–14 = telegraph) → **chicotada à frente (17, "out") = quadro 7** → (21–29) |
| `ForemanSpin` → `capataz/Spin` | 30 (0,97) → 12 | Capataz (giro de 3 m) | Arma torcido com o chicote cruzado (6–12 = telegraph) → gira 360° em 90° por chave (14, **17 "out" = quadro 7**, 20, 23) com o chicote esticado à frente-baixo → pronto (29) |

**Reusados nos Lotes 5 e 6 sem mudar as chaves:** `Hit` (todos), `Swipe` → `diabrete/Scratch` (impacto no quadro 6), `Stun` do golem → `diabrete/Stun`, `Smash` do bruto → `magma/Smash` (quadro 7), `Punch` do capitão → `brasa/Punch` (quadro 6), `Call` do capitão → `capataz/Call`.

**Itens presos à mão (`--attach`, contas de 2026-10-11):** mesma regra da picareta (abaixo): no repouso em T a mão direita aponta −X com a palma para −Z; o punho fecha ~0,04 m ao longo da mão e ~0,02 m para o lado da palma; os eixos do prop vêm de uma PCA dos vértices (`dc_geo.py`, fora do repositório).

| Item → osso | Tamanho | Deslocamento (m) | Giro (Euler XYZ) | Por quê |
|---|---|---|---|---|
| clava → `RightHand` do esqueleto | 0,49 m | (−0,04; −0,25; −0,02) | (31,9; 44,1; −78,0) | Cabo atravessa o punho para a frente, com a cabeça da clava à frente; o centro da caixa fica 0,25 m à frente do punho |
| cajado → `RightHand` do necromante | 1,40 m | (−0,04; 0,251; −0,02) | (−180; 90; 90) (resolvido numericamente: trava de cardan) | Cajado ao longo do eixo do polegar, com o crânio para cima quando a mão aponta à frente com giro +90°; o punho fica 0,25 m abaixo do centro |
| frasco → `RightHand` do kobold | 0,30 m | (−0,05; 0; −0,03) | (90; 0; 0) | O frasco em pé no modelo (gargalo para +Z) vai com o gargalo para o lado do polegar (−Y no T); o centro fica no punho |
| chicote → `RightHand` do Capataz | 0,60 m | (−0,04; −0,276; −0,02) | (1,1; 9,0; 153,2) | Chicote enrolado: o cabo (a ponta estreita da PCA) no punho, o rolo à frente (−Y no T) e o plano do rolo com a normal para +Z; o centro fica 0,276 m à frente do punho |

Conferido nas folhas: a clava, o cajado, o frasco e o chicote ficam na mão direita em todos os quadros (`*_folhas.png`).

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

**Picareta do goblin mineiro (`--attach`, conta de 2026-10-11):**
- **Modelo do Tripo inclinado:** a caixa mede 0,88 × 0,83 × 0,98, e foi preciso uma PCA dos vértices para achar os eixos.
  - Cabo em (−0,38; 0,60; 0,70), com a cabeça na ponta + (a seção alarga de 0,2 para 0,5).
  - Barra da cabeça em (0,92; 0,20; 0,33); a normal da face plana é o terceiro eixo.
- **Empunhadura no repouso em T:** a mão direita aponta −X, com a palma para −Z. O cabo atravessa o punho para a frente (−Y), com a cabeça à frente, e a barra fica em pé (+Z).
  - A rotação que leva os eixos do modelo a esses dá Euler XYZ (31,6; −67,3; 98,4), conferida pela matriz.
- **Ponto:** o punho fecha a 0,5 do centro para o cabo, perto do fim. Com `tamanho` 0,45 m (escala 0,459, cabo de 0,55 m), o centro da caixa fica 0,229 m à frente do punho. A palma fica 0,04 m ao longo da mão e 0,02 m para o lado da palma, o que dá o deslocamento (−0,04; −0,229; −0,02).
- **Conferido na folha `goblin_mineiro_folhas.png`:** a picareta fica na mão direita nos 7 clipes. No Throw ela vai ao alto atrás da cabeça (quadros 3–6) e para a frente na soltura (quadro 7). O projétil é o sprite `picareta` girando por código; durante o acompanhamento, o sprite da mão continua mostrando a picareta.

## 10. Dragão (`dragao_blender.py`, 2026-10-11)

O FBX do dragão veio com o rig "Outro" do Tripo: 43 ossos `bone_0..42`, sem mandíbula e sem nome. Não é Mixamo, então o retarget do `render_sprites.py` não serve. O `client/tools/dragao_blender.py` refaz o rig e anima por keyframe, e o render lê o `.blend` com `--self`.

```
blender.exe -b --factory-startup --python-exit-code 1 --python client\tools\dragao_blender.py -- --fbx %TEMP%\dc_in\dragao\tripo_convert_c0f56bd1-....fbx --out %TEMP%\dc_in\dragao
.\client\tools\render_sprites.ps1 -Nome dragao     -Model $env:TEMP\dc_in\dragao\dragao.blend     -Extra @("--self")
.\client\tools\render_sprites.ps1 -Nome dragao_fim -Model $env:TEMP\dc_in\dragao\dragao_fim.blend -Extra @("--self")
```

Leva ~2 s (malha de 3 813 vértices; o FBX tem 0,34 MB).

**O rig como veio** (inspeção no Blender; no FBX o dragão olha +X e o lado esquerdo é +Y):
- a hierarquia faz sentido: corpo, pescoço de 3 ossos, cabeça com 2 chifres e uma "garganta" de 2 ossos, 4 pernas, 2 asas e cauda de 4 ossos;
- **os pesos não:** 1 065 dos 3 813 vértices não tinham peso nenhum (ficariam parados com o corpo andando) e o grupo `bone_42` (a pata traseira direita) pegava o corpo de ponta a ponta;
- as asas são assimétricas (5 ossos à esquerda, 7 à direita) e ficam erguidas atrás, com a membrana no plano do corpo.

**O que o script faz:**

| Passo | Como |
|---|---|
| Nomes | Tabela `NAMES`: `root`, `spine_0..1`, `neck_0..2`, `head`, `throat_0..1`, `horn_L/R_0..1`, `leg_FL/FR_0..3` (dianteiras), `leg_HL_0..2` e `leg_HR_0..1` (traseiras), `wing_L_0..4`, `wing_R_0..6`, `pelvis`, `tail_0..3` |
| Mandíbula | Osso novo `jaw`, filho de `head`, da dobradiça atrás do canto da boca (0,37; 0; 0,50) ao queixo (0,47; 0; 0,44), em unidades do FBX. Pega os 71 vértices abaixo da linha dos dentes de cima, com transição de 3 cm na dobradiça |
| Pesos | Refeitos do zero: inverso da distância⁴ aos 2 ossos mais próximos (segmento cabeça–cauda do osso). Osso de um lado (`_L_`, `FL`, `HL` / `_R_`, `FR`, `HR`) não pega vértice do outro lado. `root` fica sem peso |
| Frente | Gira −90° em Z e aplica: o dragão olha −Y, como os rigs Mixamo (sem `--yaw` no preset) |
| Clipes | Pose = giro de cada osso em eixos do mundo do repouso, relativo ao pai (a convenção do `anim_blender.py`: X = esquerda, Y = trás, Z = cima). Chaves nos quadros que o render amostra |

**Clipes** (30 fps na fonte, folha a até 7 quadros):

| Clipe | Quadros → folha | O que faz |
|---|---|---|
| `Idle` | 30 (1,0 s), laço → 4 | Respira: peito, pescoço, asas e cauda |
| `Walk` | 36 (1,2 s), laço → 7 | Passo diagonal lento (dianteira esquerda com traseira direita), joelho dobra na volta da perna, cauda e pescoço balançam. Sem `stride_mps` (no lugar, sem root motion): a View acerta o ritmo pela velocidade |
| `Breath` | 34 (1,13 s) → 7 | Recua e inspira (1–2, a garganta acesa aparece) → **sopro no quadro 4 de 7** (≈ 0,6): pescoço à frente, focinho erguido e boca aberta → segura (5) → volta. Com a cabeça para baixo a boca sumia da câmera de 60° |
| `Fly` | 18 (0,6 s), laço → 6 | Asas abertas em V batendo de 33° a 77° acima da horizontal, dedos de trás abertos no plano da asa, pernas recolhidas. No lugar: a altura do voo e a sombra são da View |
| `Land` | 24 (0,8 s) → 6 | Do voo ao chão: asas fecham, o corpo afunda e a cabeça vai ao chão (a janela de dano `HeadDown`); o último quadro segura |
| `Roar` | 36 (1,2 s) → 7 | Empina o pescoço, cabeça para o alto, boca aberta, asas meio abertas |
| `Dying` (`dragao_fim`) | 45 (1,5 s) → 7 | Cambaleia rugindo → as pernas abrem e o corpo desce de barriga → pescoço e asas caem no chão. `ground_cut`: o que passa do chão é cortado |

**Presets:** `dragao` 3,50 m de comprimento (`width`), 4 direções com espelho, célula 576, até 7 quadros (7 × 576 = 4 032 px); `dragao_fim` 1 direção, 576 × 7. A ~4,5 m na tela, a célula de 576 (4,8 m) deixa 13 a 34 px de margem. O `AVISO recorte` do log (4,96 m) é a medida com 10% de folga.

**Limites conhecidos:**
- A boca aberta só aparece com a cabeça erguida: a 60°, de frente, o focinho no nível mostra o alto da cabeça.
- A mandíbula é pequena no sprite: os dentes aparecem no Breath de frente, mas o que vende o sopro é o cone da View.
- O voo é no lugar e sem sombra própria.
- Os dedos da asa direita (7 ossos) e da esquerda (5) não são simétricos; na folha N do `Fly` a ponta direita aparece um pouco mais longa.

## 11. Roupas do courier (`roupas_courier.py`, 2026-10-11)

Cosmético, 0 crédito: o mesmo modelo do courier com o basecolor recolorido, um `.blend` por roupa, renderizado com o preset `courier` (os 7 clipes, 5 linhas). Os nomes são os do `Hud.FolhaRoupa` (`MetaTelas.cs`): `courier_noturno`, `courier_real`, `courier_explorador`, `courier_forja` (Bombeiro de forja) e `courier_fantasma`. A Clássica continua sendo o `courier`. Mineiro e Coveiro (fim do enum `Outfit`) ainda não têm chave no `RoupaChave` da View e não foram feitos.

```
blender.exe -b --factory-startup --python-exit-code 1 --python client\tools\roupas_courier.py -- %TEMP%\dc_in\courier\courier.blend %TEMP%\dc_in\roupas
.\client\tools\render_sprites.ps1 -Nome courier_<roupa> -Preset courier -Model %TEMP%\dc_in\roupas\courier_<roupa>.blend -Anims %TEMP%\dc_arte\mixamo\courier -Hold %TEMP%\dc_arte\mixamo\Hold.fbx
```

**Como recolore** (HSV no numpy, o sombreado da textura fica):
- casaco e chapéu = os pixels azuis do basecolor (matiz 180–250°, saturação > 0,2);
- bolsa, botas, cinto e luvas = os marrons saturados (matiz 0–45°, saturação > 0,35);
- pele e camisa (pouca saturação) não mudam.

| Roupa | Azul vira | Marrom vira | Tinta de reserva da View (`TintaRoupa`) |
|---|---|---|---|
| `noturno` | anil escuro (245°) | couro quase preto | `#7C86D8` |
| `real` | roxo real (280°) | dourado (45°) | `#D7A0F0` |
| `explorador` | cáqui (42°) | couro escuro | `#E2CC98` |
| `forja` | vermelho-fogo (10°) | couro carvão | `#F2905C` |
| `fantasma` | tudo cinza-ciano claro | — | branco-ciano com alfa 0,6 |

O Tripo liga o basecolor em dois nós de imagem do material ("Diffuse Texture.003" e ".005"); o script muda os dois.

**Memória:** cada roupa tem a mesma folha do courier (~6,9 MB em ASTC, 5,2 MB de PNG). Só a roupa equipada carrega na corrida; no APK, as 5 somam ~26 MB de PNG. Se o tamanho do APK apertar, o corte é renderizar só Idle, CarryIdle, Run e CarryRun por roupa (e a View cair na folha `courier` para Throw, Dash e Hit), ou ficar só com a tinta de reserva.
