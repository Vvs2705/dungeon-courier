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

| Arquivo (`arte/mixamo/...`) | Conjunto | Clipe no Mixamo (nome exibido) | Configuração | Data | SHA-256 |
|---|---|---|---|---|---|
| `Hold.fbx` | courier, ladrao | | FBX Binary, Without Skin, 30 fps, sem redução, X Bot | | |
| `courier/CarryRun.fbx` | courier | Running (cópia de `courier/Run.fbx`) | In Place | | |
| | | | | | |

Fonte: mixamo.com, na conta Adobe do Vinicius, personagem X Bot (nenhum modelo nosso é enviado).

## 4. Imagens grátis, texturas e fontes (0 crédito)

| Arquivo | Ferramenta / origem | Prompt ou URL | Escolhida | Data | SHA-256 | Licença |
|---|---|---|---|---|---|---|
| `piso` (textura) | Tripo, gerador de imagem embutido | | #_ de 4 | | | tripo_pago |
| `parede` (textura) | idem | | | | | tripo_pago |
| Lilita One | Google Fonts (github.com/google/fonts, `ofl/lilitaone`) | — | — | | | SIL OFL 1.1 |
| Nunito | Google Fonts (`ofl/nunito`) | — | — | | | SIL OFL 1.1 |

## 5. Sprites renderizados (0 crédito)

| Pasta `Resources/Sprites/` | Data | Entrada (FBX + SHA-256) | Clipes / Hold | SHA-256 do `render_sprites.py` | Comando | Folha de conferência |
|---|---|---|---|---|---|---|
| | | | | | | |

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
