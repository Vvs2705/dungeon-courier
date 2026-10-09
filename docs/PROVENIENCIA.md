# Dungeon Courier — Proveniência e licença de arte

## Regras

- **Sem linha, sem build:** cada asset gerado ou de terceiros entra aqui (ferramenta, plano, prompt, data, task id, SHA-256, licença). Sem essa linha, o asset não entra em build comercial (`ART_BIBLE.md` §13). O formato é o do `03_FORGE_STREET/docs/PROVENIENCIA.md`.
- **Abrir antes, completar depois:**
  - A linha da peça é aberta **antes** de gerar, com prompt e créditos previstos.
  - Depois se completa com task id, arquivo baixado, SHA-256, créditos gastos e saldo.
  - O saldo se lê no site antes e depois de cada geração.
- **Aval por lote:** créditos só com aval explícito do Vinicius para aquele lote (guardrail). O teto do lote vai na 1ª linha do livro-razão, e o saldo final não pode passar dele.
- **Hash:** SHA-256 com `certutil -hashfile <arquivo> SHA256` (Windows) ou `sha256sum <arquivo>`.
- **Onde ficam os arquivos crus:** `arte/tripo/` e `arte/mixamo/`, fora do git (`.gitignore`).

**Estado (2026-10-09):** Lote 1 aprovado pelo Vinicius neste chat ("Aprovo 485 (teto 550)"), em produção pelo coordenador no Chrome deste PC.

## 1. Livro-razão de créditos (Tripo, conta do estúdio, plano Max)

| Data/hora | Evento | Saldo visto no site | Gasto | Obs |
|---|---|---|---|---|
| 2026-10-09 ~15:20 | Abertura do Lote 1 (aval do Vinicius em 2026-10-09: "Aprovo 485 (teto 550)"; orçado 485, teto 550) | **20 720** | 0 | painel conferido: "Gerar 45", 8K desligado, Privado, H3.1 - Máx. qualidade. Teto: saldo final ≥ **20 170** |
| 2026-10-09 15:11 | Courier: imagem #3 de 4 (Nano Banana 2, 1:1, grátis) → Modelo HD H3.1 | 20 675 | 45 | task `74de35fe-bcd3-48aa-94ee-dc0e02c8f7d0` · 7 976 faces |
| 2026-10-09 15:13 | Ladrão: imagem #3 de 4 → Modelo HD | 20 630 | 45 | task `4879e1d4-070e-4bad-a3d8-593ef0abc3b6` · 7 575 faces |
| 2026-10-09 15:16 | Guarda v1 (com escudo e maça juntos): imagem #3 de 4 → Modelo HD | 20 585 | 45 | task `7f6e0866-137f-4422-b094-221799c3f5af` · 7 733 faces · **DESCARTADO** pela regra do Vinicius de 2026-10-09 (itens separados do personagem) |
| 2026-10-09 15:19 | Morcego: imagem #4 de 4 → Modelo HD | 20 540 | 45 | task `c5223f88-f2d7-44d0-a912-ef848e8beef0` · 7 816 faces |
| 2026-10-09 15:20 | Cliente: imagem #3 de 4 → Modelo HD | 20 495 | 45 | task `a8045cba-4afa-4393-bbdb-8661361e3930` · 7 536 faces |
| 2026-10-09 15:22 | Caixa: imagem #2 de 4 → Modelo HD | 20 450 | 45 | task `4eefcbef-6735-4804-948c-f619f6653cf8` · 7 334 faces |
| 2026-10-09 15:23 | Baú: imagem #1 de 4 → Modelo HD | 20 405 | 45 | task `9d6ba381-8afc-4eec-a9ea-27ee897d35b6` · 7 407 faces |
| 2026-10-09 15:26 | Guarda v2 (sem escudo e sem maça): imagem #4 de 4 → Modelo HD | 20 360 | 45 | task `42fee414-ea6f-42be-9e3b-7ce474c6bfa8` · 7 675 faces |
| 2026-10-09 15:29 | Escudo-torre (prop separado): 1ª rodada de imagens saiu com 2 peças (frente+verso), refeita com "only one object"; imagem #2 de 4 → Modelo HD | 20 315 | 45 | task `16003d6a-e49f-47c7-987c-c3e6c9b7d548` · 7 284 faces |
| 2026-10-09 15:31 | Arco da porta: imagem #2 de 4 → Modelo HD | 20 270 | 45 | task `cf719c6f-5e9a-4c39-be8a-f853a3c61da9` |
| 2026-10-09 15:31 | **Subtotal modelos: 10 × 45 = 450.** Baú aberto cortado. Faltam 4 rigs (80) após o veredito do Vinicius → previsto 530 | 20 270 | 450 | teto: saldo final ≥ 20 170 |
| 2026-10-09 ~15:35 | **Veredito do Vinicius** sobre a folha `client/Builds/arte_lote1/veredito_lote1.png`: personagens "Todos aprovados"; props "Todos aprovados" | 20 270 | 0 | libera o rig dos 4 personagens e o export dos 5 props |
| 2026-10-09 15:46–15:55 | Auto Rig humanoide, predefinição de esqueleto **Mixamo**, nos 4 personagens (courier, ladrão, guarda v2, cliente), cada um pela URL `workspace/rigging/<task>` | **20 190** | 80 | export FBX predefinição Mixamo 2K com esqueleto |
| 2026-10-09 15:47–16:00 | 9 exports e downloads (aval do Vinicius: "Pode baixar os 9"); props com FBX predefinição Blender 2K; export não cobra | 20 190 | 0 | arquivos em `06_DUNGEON_COURIER/arte/tripo/<asset>/` do **checkout principal** (o worktree pode ser apagado; `arte/tripo/` fica fora do git) |
| 2026-10-09 16:00 | **Lote 1 FECHADO: 530 créditos** (10 modelos × 45 + 4 rigs × 20); 20 720 → 20 190 | 20 190 | 530 | dentro do teto de 550 |
| | | | | |

## 2. Registro por peça (Tripo)

Preencha cada linha. Prompt e parâmetros completos vão no bloco do §6, se a linha não comportar.

| Asset (pasta) | Lote/# | Data | Ferramenta (imagem → modelo → rig) | Imagem escolhida | Task id | Prompt (texto exato) | Créditos (previsto / gasto) | Arquivo baixado | SHA-256 do arquivo baixado | Licença | Visibilidade |
|---|---|---|---|---|---|---|---|---|---|---|---|
| courier | 1/#1 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → Auto Rig Mixamo → FBX Mixamo 2K | #3 de 4 (Nano Banana 2) | `74de35fe-bcd3-48aa-94ee-dc0e02c8f7d0` | §2.1 (bloco de prompts) | 65 / 65 | `arte/tripo/courier/courier.zip` (21369870 B) → `tripo_convert_f2e9a770-d4e2-417b-94a7-bd84d10485c1.fbx` | zip `657fc11fa2527bae0afc20c796a38e6a44e612a66b479ea97cc22e958980ec24` · fbx `21b7bafc218e391d9c0559e9cf057e548d60cfba6e2120fb1211ca969bda8f9b` | tripo_pago | privada |
| ladrao | 1/#2 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → Auto Rig Mixamo → FBX Mixamo 2K | #3 de 4 (Nano Banana 2) | `4879e1d4-070e-4bad-a3d8-593ef0abc3b6` | §2.1 (bloco de prompts) | 65 / 65 | `arte/tripo/ladrao/ladrao.zip` (25269739 B) → `tripo_convert_78892eb1-aa12-43cb-80af-43f6950596eb.fbx` | zip `9aacd806d4b4b740ba75d6f467192244c5b4f29c4464c35d8fb03c3f4ac381ff` · fbx `a72377a93eddd5a7f1e0b23d229debdcb891b8d64f51f0ebf3f8f398d39fb5ff` | tripo_pago | privada |
| guarda | 1/#3 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → Auto Rig Mixamo → FBX Mixamo 2K | #4 (v2, sem escudo) de 4 (Nano Banana 2) | `42fee414-ea6f-42be-9e3b-7ce474c6bfa8` | §2.1 (bloco de prompts) | 65 / 65 (+45 do v1 descartado) | `arte/tripo/guarda/guarda.zip` (30061916 B) → `tripo_convert_77906b18-8099-4aba-b7dd-a24cc0661e2a.fbx` | zip `90ba4622fe3dc37a49131b922a78c01ec79d58479413ed9378131142f13d1df3` · fbx `b7e80be116a5f9a5fcaa329f3e3623d7d8c6c47f508a153487a7271c0dd960b2` | tripo_pago | privada |
| escudo | 1/#3b | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K (prop separado, regra do Vinicius) | #2 de 4 (rodada 2) | `16003d6a-e49f-47c7-987c-c3e6c9b7d548` | §2.1 | 45 / 45 | `arte/tripo/escudo/escudo.zip` (3537690 B) → `tripo_convert_80524815-ce06-403f-b6a3-dc652d2dadea.fbx` | zip `dc81f567cb03fb6cbce31f058afcd58853f0e0f83350ef633cda994858e37a9e` · fbx `11e7df9c6c7ff68056d77957d5a659afcf8b4f15c4bc27e377d4a808bf96c04d` | tripo_pago | privada |
| cliente | 1/#4 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → Auto Rig Mixamo → FBX Mixamo 2K | #3 de 4 (Nano Banana 2) | `a8045cba-4afa-4393-bbdb-8661361e3930` | §2.1 (bloco de prompts) | 65 / 65 | `arte/tripo/cliente/cliente.zip` (21060862 B) → `tripo_convert_cb64fb7a-2375-43bc-8e76-f6380b2d17a2.fbx` | zip `cd987e578a9fad4c4ec593dd34a7ae293648f99aef57c5d0ecb233d8e2d0e0f9` · fbx `9dded3b6df654a2aa621c21f9de46a7162e10fc9343ebc741d3bc1a9fc14125b` | tripo_pago | privada |
| morcego | 1/#5 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #4 de 4 (Nano Banana 2) | `c5223f88-f2d7-44d0-a912-ef848e8beef0` | §2.1 (bloco de prompts) | 45 / 45 | `arte/tripo/morcego/morcego.zip` (3158718 B) → `tripo_convert_ad567423-22eb-4b0c-9287-36c0966f07b2.fbx` | zip `50897532a29f82e053600e33f5671c2248c2168dc4a328a902b438be1ec5b404` · fbx `793e39a1cf594884b433c0345ae22dff1e4cf9dcd084f572189bc11cc74dd230` | tripo_pago | privada |
| caixa | 1/#6 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #2 de 4 (Nano Banana 2) | `4eefcbef-6735-4804-948c-f619f6653cf8` | §2.1 (bloco de prompts) | 45 / 45 | `arte/tripo/caixa/caixa.zip` (3100175 B) → `tripo_convert_dc019326-bfa0-40ce-ab02-2b4e8aae5eb6.fbx` | zip `589933f0546ab70e937b9b9de5f764df2bb28fa6ac9786c5b3e5cc7ebe03f396` · fbx `9ee57b8646823923b652e7a9344d2fc79d76fbfa1814336b7c60566bd09ced1c` | tripo_pago | privada |
| bau | 1/#7 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #1 de 4 (Nano Banana 2) | `9d6ba381-8afc-4eec-a9ea-27ee897d35b6` | §2.1 (bloco de prompts) | 45 / 45 | `arte/tripo/bau/bau.zip` (4151987 B) → `tripo_convert_70aa8043-4833-4392-a538-6816ecb51a47.fbx` | zip `2672c74a19e3248d8a742d76ad57dbd291e7571aa8f78eb64f59f39cc12ad14f` · fbx `c3e05b9194cb26ba0c3453c6e9e684c1b436c83620f80cf8ed46f80e133717e0` | tripo_pago | privada |
| bau_aberto | 1/#8 | — | **cortado** (abertura procedural) | — | — | — | 45 / 0 | — | — | — | — |
| arco | 1/#9 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #2 de 4 (Nano Banana 2) | `cf719c6f-5e9a-4c39-be8a-f853a3c61da9` | §2.1 (bloco de prompts) | 45 / 45 | `arte/tripo/arco/arco.zip` (3148165 B) → `tripo_convert_5ef411ad-d800-46d7-b5ff-a5ac007f7260.fbx` | zip `8f4be7af2a07e3497b7146cfe2d5af25a76c4b4cf174b6e86e50ac308090d9a0` · fbx `6eda50930fb34712252532cf9f7832a09a82666718d9e1f5a9b7999e15e326ca` | tripo_pago | privada |
| bruto | 2/#10 | | | | | | 65 / | `arte/tripo/bruto/` | | | |
| ogro | 2/#11 | | | | | | 65 / | `arte/tripo/ogro/` | | | |
| reliquia | 2/#12 | | | | | | 45 / | `arte/tripo/reliquia/` | | | |
| ovo | 2/#13 | | | | | | 45 / | `arte/tripo/ovo/` | | | |
| filhote | 2/#14 | | | | | | 45 / | `arte/tripo/filhote/` | | | |

Os prompts previstos estão em `ASSETS.md` §0–§3 (assunto + sufixos).
- Registre aqui o **texto exato usado**: no Forge, o Lote 4 ficou sem o prompt exato no ledger e precisou ser reconstruído.
- Registre um SHA para o zip e outro para o FBX extraído.


### 2.1 Prompts exatos do Lote 1 (imagem, Nano Banana 2, 1:1, 4 imagens, grátis)
Sufixos usados exatamente como em `ASSETS.md` §0 (`[S]`+`[T]`+`[I]` nos personagens, `[P]`+`[IP]` nos props). Parâmetros 3D iguais em todas as peças: HD H3.1 - Máx. qualidade, Ultra, textura 2K, remover iluminação, PBR, triângulos, 8 000 polígonos, 8K desligado, Gerar em Partes desligado, Privado. Sem Multi-View (custo não confirmado; receita de 1 imagem validada no Forge).
- courier: "young human courier of an adventurers' delivery guild, friendly determined face, big wide-brim blue courier cap with a small brass guild badge, short blue tunic over a cream shirt with rolled sleeves, bulky brown leather cross-body messenger satchel with buckles, short cream scarf, fingerless leather gloves, sturdy brown boots, " + [S] + [T] + [I]
- ladrao: "asneaky goblin thief, olive green skin, long pointy ears, big greedy grin, slightly hunched, deep purple pointed hood and short ragged purple cape, dark leather vest, huge empty burlap sack tied on his back, wrapped bare feet, chunky proportions 3 heads tall with a big head, " + [S] + [T] + [I] (o "a" a mais no início foi erro de digitação do operador; ficou assim)
- guarda v1 (descartado): "dungeon gate guard, heavy steel plate armor, closed great helm with a short dark red crest, tall rectangular steel tower shield with a dark red band strapped flat to the left forearm (not touching the body), dark red tabard, short mace hanging on the belt, " + [S] + [T] + [I]
- guarda v2: "dungeon gate guard, heavy steel plate armor, closed great helm with a short dark red crest, dark red tabard, empty hands, no weapon, no shield, " + [S] + [T] + [I]
- cliente: "plump cheerful merchant collector, round spectacles, curly grey mustache, wide-brim green hat with a long feather, green coat ending above the knees with gold buttons, leather boots, " + [S] + [T] + [I]
- morcego: "chubby cartoon dungeon bat, round body, big wing membranes spread wide, large pointy ears, small fangs, big white eyes, burgundy fur and rose-pink wings, flying pose, " + [P] + [IP]
- caixa: "sturdy wooden delivery crate, chunky planks, iron corner brackets, crossed rope straps with a carry knot on top, round blue wax guild seal on the front, blank paper label, " + [P] + [IP]
- bau: "small dungeon treasure chest, closed, rounded wooden lid, iron bands, big brass lock, studs, " + [P] + [IP]
- escudo (rodada 1, descartada por sair com 2 peças): "tall rectangular steel tower shield with a dark red horizontal band, slightly curved, inner arm straps on the back, front view, " + [P] + [IP]
- escudo (rodada 2, usada): "one single tall rectangular steel tower shield standing upright, dark red horizontal band across the middle, slightly curved, riveted steel rim, seen from the front at a slight three-quarter angle, only one object in the image, stylized fantasy comic game prop, chunky beveled shapes, flat colors with soft two-tone shading, centered, no background, no text, plain light grey background, soft even light, no ground shadow"
- arco: "dungeon doorway, chunky grey-blue stone block archway, rounded arch top with a keystone, empty dark opening, no door, " + [P] + [IP]

## 3. Clipes Mixamo

**Estado (2026-10-09, raia D):** nenhum clipe foi baixado para o DC.

**Reuso:** aval do Vinicius em 2026-10-09 para reusar os clipes Mixamo já no disco em outros projetos dele, como cópia e sem alterar a origem. Valem duas notas:
- **"reuso 03_FORGE_STREET"**: arquivo, nome e SHA conferidos com `03_FORGE_STREET/docs/PROVENIENCIA.md`, bloco "clips Mixamo". São X Bot, FBX Binary, Without Skin, 30 fps, sem redução, baixados em 2026-10-06.
- **"reuso ARKANA"**: origem em `ARKANA/mobile-unity/Assets/_Arkana/Resources/mixamo-*.fbx`, nome exibido conforme `ARKANA/design/pipeline/MIXAMO.md`. São Y Bot, FBX for Unity, Without Skin, 30 fps, baixados em 2026-10-04. Os dois runs vêm **com root motion**, apesar do "In Place" do MIXAMO.md: o quadril anda 2,2 m e 3,1 m. O `render_sprites.py` tira a deriva.

O esqueleto `mixamorig:` (65 ossos, repouso em T) é o mesmo nos dois bonecos e no rig do Tripo, conferido no Blender.

**Clipes criados no Blender** (o que não havia no disco): keyframes por script em `client/tools/anim_blender.py`, sobre o armature do X Bot de `courier/Idle.fbx` (reuso Forge).
- SHA do script: `e77d98c92d9799952680c930ec6f4dfcc50a6b83c48e210fea7a394a467ddfa3`.
- Saída canônica em `arte/mixamo/_blender/`, copiada para o conjunto.
- Licença: animação original do estúdio; nomes e repouso do esqueleto Mixamo.
- O SHA do FBX muda a cada export (o FBX grava data). Re-rodar o script gera um arquivo equivalente, não idêntico.

| Arquivo (`arte/mixamo/...`) | Conjunto | Origem (nome exibido) | Configuração | Data | SHA-256 |
|---|---|---|---|---|---|
| `Hold.fbx` (= `_blender/Hold.fbx`) | courier, ladrao (`--hold`) | **criado no Blender** (`anim_blender.py`, clipe `Hold`) | 10 quadros, 30 fps, só armature | 2026-10-09 | `b7813e2469fbb2346a3f6c8832f3d975f903ad472020fc921b7c970cedc62674` |
| `courier/Run.fbx`, `courier/CarryRun.fbx` | courier | **reuso ARKANA**: `mixamo-loc-sprint.fbx` = "Sprint — Standard Sprint" | Y Bot, root motion (tirado no render) | 2026-10-09 (origem 2026-10-04) | `fd00884dc16483779fe2d0eec446dae812a26b6bb86b3497a7f44459aeba4ad7` |
| `courier/Idle.fbx`, `courier/CarryIdle.fbx`, `cliente/Idle.fbx` | courier, cliente | **reuso 03_FORGE_STREET**: `arte/mixamo/Idle.fbx` = Breathing Idle | X Bot | 2026-10-09 (origem 2026-10-06) | `befaa0fa9c5684a4e82dd229b04710588b1f32bc026704cc1856cae9c80f4220` |
| `courier/Throw.fbx` (= `_blender/Throw.fbx`) | courier | **criado no Blender** (`Throw`) | 21 quadros | 2026-10-09 | `1e2df0fde14c2ba16feba09323a3a6e81bd7b78931e60f32b72a92f9fc0585c4` |
| `courier/Dash.fbx` (= `_blender/Dash.fbx`) | courier | **criado no Blender** (`Dash`) | 16 quadros | 2026-10-09 | `881ec5c391fe7224fde56f33c2ad705c8fbc1f89fff3f2ca45e13256b08ed19d` |
| `courier/Hit.fbx` (= `_blender/Hit.fbx`) | courier | **criado no Blender** (`Hit`) | 15 quadros | 2026-10-09 | `da459460e82ccf9d1b430d57138f576821ad76f568386b6bbac2b49dceefece5` |
| `courier_fim/Cheering.fbx`, `cliente/Cheering.fbx` | courier_fim, cliente | **reuso 03_FORGE_STREET**: `Cheering.fbx` = Cheering (Male Cheering With Two Fists Pump) | X Bot | 2026-10-09 | `239cecccd1f25fd48e4ae1cbf88d79957402afe32032cb7bd1dbdc4d66c72985` |
| `courier_fim/Dying.fbx` | courier_fim | **reuso ARKANA**: `mixamo-standing-react-death-backward.fbx` = "Standing React Death Backward" (Pro Magic Pack) | Y Bot | 2026-10-09 | `9b78551d6a64216f444b7b46b10b567e3e9bd02a5916317195e531e20d3c04ac` |
| `ladrao/Run.fbx`, `ladrao/CarryRun.fbx` | ladrao | **reuso ARKANA**: `mixamo-loc-correr-frente.fbx` = "Unarmed Run Forward — Running Forward" | Y Bot, root motion (tirado no render) | 2026-10-09 | `797cd01c22279321164ae497a5552fd4102df974bd8910db8e355d0930f3143d` |
| `ladrao/Lurk.fbx`, `cliente/Shrug.fbx` | ladrao, cliente | **reuso 03_FORGE_STREET**: `LookingAround.fbx` = Looking Around (Idle Stand Looking Around); no cliente, substitui o "Shrug", que não existe no disco | X Bot | 2026-10-09 | `9c80441200a9e6c3c13b570e65183b7d3faeb6ff0f0dc1a94785a4dab144d930` |
| `ladrao/Grab.fbx` (= `_blender/Grab.fbx`) | ladrao | **criado no Blender** (`Grab`, pega com as 2 mãos e termina no `Hold`) | 24 quadros | 2026-10-09 | `a88cc18285f76bf5091ac2ad7cb06cfcd2944874a17c1864eb092acefda18364` |
| `guarda/Idle.fbx` (= `_blender/GuardIdle.fbx`) | guarda | **criado no Blender** (`GuardIdle`, escudo à frente, laço) | 60 quadros | 2026-10-09 | `ea85fc1f5ff678efbe974e73202138a0d876584bb8f596cdd9ca87451651e838` |
| `guarda/Bash.fbx` (= `_blender/Bash.fbx`) | guarda | **criado no Blender** (`Bash`) | 30 quadros | 2026-10-09 | `57cfdd68fe81201e4fb0d1c24d26277df195de1b8679367e27402bb55899ee24` |
| `cliente/Thankful.fbx` | cliente | **reuso 03_FORGE_STREET**: Thankful | X Bot | 2026-10-09 | `5462c4529ed06b88caaa877918be24535b2474731489daa027c365b91bfe7e88` |
| `cliente/Waving.fbx` | cliente | **reuso 03_FORGE_STREET**: Waving | X Bot | 2026-10-09 | `d8f9444653475b44541eea77beb8e64f3eb3ee9b6ea1366460480e4489b91367` |

**Testados e não usados** (ficam no ARKANA, sem cópia em `arte/mixamo/`):
- Renderizados numa folha de teste e descartados:
  - `mixamo-standing-run-forward.fbx`: braço erguido de magia.
  - `mixamo-standing-react-small-from-front.fbx`: o Hit do Blender lê melhor a ~100 px.
  - `mixamo-standing-1h-magic-attack-01.fbx`: 2,3 s e lê como feitiço.
- Só inspecionado, sem render: `mixamo-pegar.fbx` ("Picking Up An Object With One Hand"), descartado por ter 3,5 s e usar uma mão só.

Fonte dos originais: mixamo.com, nas contas Adobe do Vinicius (X Bot no Forge, Y Bot no ARKANA). Nenhum modelo nosso foi enviado.

## 4. Imagens grátis, texturas e fontes (0 crédito)

| Arquivo | Ferramenta / origem | Prompt ou URL | Escolhida | Data | SHA-256 | Licença |
|---|---|---|---|---|---|---|
| `piso` (textura) | Tripo, gerador de imagem embutido | | #_ de 4 | | | tripo_pago |
| `parede` (textura) | idem | | | | | tripo_pago |
| Lilita One | Google Fonts (github.com/google/fonts, `ofl/lilitaone`) | — | — | | | SIL OFL 1.1 |
| Nunito | Google Fonts (`ofl/nunito`) | — | — | | | SIL OFL 1.1 |

## 5. Sprites renderizados (0 crédito)

Renderizado em 2026-10-09 pela raia D: Blender 5.2.1 LTS headless, 0 crédito, sem rede.
- `render_sprites.py`: SHA-256 `6d435e163f14842947a35c314a227c8f257c27aa3ed47fa84b85848f4a6e1ec9`, o mesmo nos 9 conjuntos.
- `render_sprites.ps1`: `aa22a3a3638d40bfb0d90374ec17132afec0e3a86a549b21d1635747ec499fca`.
- Os FBX de entrada são cópias de `arte/tripo/` em `%TEMP%\dc_in\` (caminho curto). O SHA bate com o §2.
- Comandos: `RENDER_SPRITES.md` §2.
- Licença: tripo_pago + clipes do §3. Visibilidade: privada.

| Pasta `Resources/Sprites/` | Data | Entrada (FBX + SHA-256) | Clipes / Hold | SHA-256 do `render_sprites.py` | Comando | Folha de conferência |
|---|---|---|---|---|---|---|
| `courier` | 2026-10-09 | `tripo/courier/tripo_convert_f2e9a770-….fbx` `21b7bafc…8f9b` | CarryIdle, CarryRun, Dash, Hit, Idle, Run, Throw / `Hold.fbx` | `6d435e16…1ec9` | `-Nome courier -Anims mixamo\courier -Hold mixamo\Hold.fbx` | `sprites_contact/courier.png`, `courier_folhas.png` |
| `courier_fim` | 2026-10-09 | o mesmo FBX do courier | Cheering, Dying | `6d435e16…1ec9` | `-Nome courier_fim -Anims mixamo\courier_fim` | `courier_fim.png`, `courier_fim_folhas.png` |
| `ladrao` | 2026-10-09 | `tripo/ladrao/tripo_convert_78892eb1-….fbx` `a72377a9…b5ff` | CarryRun, Grab, Lurk, Run / `Hold.fbx` | `6d435e16…1ec9` | `-Nome ladrao -Anims mixamo\ladrao -Hold mixamo\Hold.fbx` | `ladrao.png`, `ladrao_folhas.png` |
| `guarda` | 2026-10-09 | `tripo/guarda/tripo_convert_77906b18-….fbx` `b7e80be1…60b2` + escudo `tripo/escudo/tripo_convert_80524815-….fbx` `11e7df9c…c04d` (`--attach`) | Bash, Idle | `6d435e16…1ec9` | `-Nome guarda -Anims mixamo\guarda -Extra @("--attach","<escudo>:LeftForeArm:0.09,0,0.06:90,180,0:0.75")` | `guarda.png`, `guarda_folhas.png` |
| `cliente` | 2026-10-09 | `tripo/cliente/tripo_convert_cb64fb7a-….fbx` `9dded3b6…125b` | Cheering, Idle, Shrug, Thankful, Waving | `6d435e16…1ec9` | `-Nome cliente -Anims mixamo\cliente` | `cliente.png`, `cliente_folhas.png` |
| `caixa` | 2026-10-09 | `tripo/caixa/tripo_convert_dc019326-….fbx` `9ee57b86…d09ced1c` | Static | `6d435e16…1ec9` | `-Nome caixa` | `caixa.png` |
| `bau` | 2026-10-09 | `tripo/bau/tripo_convert_70aa8043-….fbx` `c3e05b91…17e0` | Static | `6d435e16…1ec9` | `-Nome bau` | `bau.png` |
| `arco` | 2026-10-09 | `tripo/arco/tripo_convert_5ef411ad-….fbx` `6eda5093…26ca` | Static | `6d435e16…1ec9` | `-Nome arco` | `arco.png` |
| `morcego` | 2026-10-09 | `tripo/morcego/tripo_convert_ad567423-….fbx` `793e39a1…d230` | Static | `6d435e16…1ec9` | `-Nome morcego` | `morcego.png` |

O SHA dos PNG não é reprodutível bit a bit, porque o AA do Workbench varia alguns níveis entre renders (Forge §6). Registre o SHA da entrada e o do script.

## 6. Modelo de bloco por peça (copiar quando a linha do §2 não bastar)

```
### <asset>
- categoria / lote: Personagem | Prop — ASSETS.md Lote _ #_
- estado: PLANEJADO | GERADO | RIGADO | EXPORTADO | RENDERIZADO | APROVADO (data, operador)
- ferramenta: Tripo Studio — imagem (Nano Banana 2 | GPT Image 2, 1:1, Multi-View s/n), modelo HD H3.1, rig Auto Rig Mixamo
- plano: pago (conta do estúdio, plano Max) · comprovante_plano: nº da fatura (nunca dado de cartão)
- prompt_imagem (exato):
- prompt_vistas (Multi-View: lado / costas, exatos):
- parametros: ~8 000 polígonos, triângulos, 2K, Ultra, remover iluminação, PBR, Gerar em Partes off, 8K off, Pose T, PRIVADO
- imagem_escolhida: #_ de 4 — motivo (ART_BIBLE §4: silhueta, cor de papel, mãos vazias, pernas à mostra)
- veredito_vinicius (antes do rig): data + aprovado/reprovado
- creditos: previsto _ · gasto _ · saldo antes _ → depois _
- task_id / url:
- arquivo_baixado: arte/tripo/<asset>/<zip> (SHA-256 ___, bytes ___) → <fbx> (SHA-256 ___)
- validacao_blender: tris, altura antes de normalizar, nº de ossos e prefixo, escala do armature
- licenca: tripo_pago · termos: https://www.tripo3d.ai/terms (versão "Last updated: July 11, 2025" segundo o COE; reler no dia)
- visibilidade: privada
- obs:
```

## 7. Licenças e termos

- **Tripo:** só o plano pago permite uso comercial. O gratuito é "Non-Commercial Use" e pode publicar o modelo: `tripo_free` reprova a peça. Usa-se a conta do estúdio, plano Max (`ARKANA/design/cenario/TRIPO-STUDIO.md`, 30/09), com Privado marcado **antes** de gerar.
- **Mixamo:** o FAQ da Adobe fala em uso comercial royalty-free e proíbe redistribuir os arquivos crus. Por isso `arte/mixamo/` fica fora do git. FAQ não é termo: reler os termos completos antes do lançamento (pendência herdada do PIPELINE §5).
- **Fontes:** SIL OFL 1.1. Embutir o `OFL.txt` junto do arquivo da fonte no projeto.
- **Reuso do Forge** (baú, clipes): só com aval do Vinicius neste chat. Nesse caso, a linha copia o task id e o SHA do ledger do Forge e anota "reuso 03_FORGE_STREET".
