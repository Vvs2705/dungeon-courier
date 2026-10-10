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
| 2026-10-09 ~17:55 | **Abertura do Lote 2** (aval do Vinicius em 2026-10-09: "pode seguir tudo que for recomendado", sobre a recomendação "Lote 2: 265 créditos, teto 330"); orçado 265 (bruto 65, ogro 65, relíquia 45, ovo 45, filhote 45) | **20 190** | 0 | teto: saldo final ≥ **19 860**; ogro sem algema/corrente (regra dos itens) |
| 2026-10-09 17:25–17:31 | 5 Modelos HD H3.1 (imagens grátis Nano Banana 2): bruto #3 (`e40525cf-36e0-40be-bf13-4967b9c5a084`, 7 805 faces), ogro #2 (`ffb744ee-0127-4426-8d2a-25cd69986d5a`, 7 530), relíquia #1 (`0948a1dd-bc40-473f-b005-2cfef384216a`, 7 752), ovo #1 (`97947289-c235-4d6b-93d8-42aa551e54c7`, 7 318), filhote #4 (`5b382495-08dc-46b0-837f-5c00ff987ebf`, 7 638) | 19 965 | 225 | receita conferida: "Gerar 45", Privado, 8K off |
| 2026-10-09 ~17:33 | **Veredito do Vinicius** sobre `client/Builds/arte_lote1/veredito_lote2.png`: "Tudo aprovado, pode baixar" | 19 965 | 0 | libera rig e downloads |
| 2026-10-09 17:34–17:36 | Auto Rig humanoide Mixamo no bruto e no ogro (pose A; o rig aceitou) | **19 925** | 40 | export FBX predefinição Mixamo 2K |
| 2026-10-09 17:36–17:41 | 5 exports e downloads; props com predefinição Blender 2K (a relíquia foi exportada 2× por dúvida de predefinição: arquivos idênticos byte a byte, duplicata apagada) | 19 925 | 0 | arquivos em `arte/tripo/<asset>/` do checkout principal |
| 2026-10-09 17:41 | **Lote 2 FECHADO: 265 créditos** (5 × 45 + 2 × 20); 20 190 → 19 925 | 19 925 | 265 | dentro do teto de 330 |
| 2026-10-10 ~16:35 | **Abertura do Lote 3** (aval do Vinicius em 2026-10-10: "sem pausas, sem solicitações ... quero avanço sempre continue, seja na arte"); orçado 490 (ASSETS §8), teto 550 | **19 925** | 0 | teto: saldo final ≥ **19 375** |
| 2026-10-10 16:30–16:40 | 10 Modelos HD H3.1 (imagens grátis Nano Banana 2): arqueiro #3 (`3e0e3a23-b2fe-412c-bc8d-10e983bb474a`, 7 704 faces), capitao #3 (`21467b7c-64d5-4901-9e49-2988fa6358e7`, 7 539), arcoflecha #4 (`0b74c8a3-fdd1-45e8-be40-aad423f5217b`), limo #2 (`472ae4d5-91f7-492b-a3d2-33d9ce806cc6`), bolo #4 (`c93b8272-ba6d-4987-b9f8-7e0082025ff9`), mensagem #2 (`1f0a3224-35b1-452a-9592-57bfa8d8fb7b`), caixa_pesada #2 (`de82a310-1f85-4891-867b-b244d5cb232c`), barril #1 (`0e6575ae-280c-49d4-b441-f392244255f3`), tocha #3 (`fa9f4dd2-732b-457d-af7e-ec05ca36c5e1`), pilar #2 (`ae5e9068-69a0-4945-8670-19edd91de1b4`) | 19 475 | 450 | receita conferida: "Gerar 45", Privado, 8K off |
| 2026-10-10 16:42 | Auto Rig humanoide Mixamo no arqueiro e no capitão (o Tripo avisou "não pronto para rigging automático"; os modelos estavam em pose T e o rig saiu coerente nos dois) | **19 435** | 40 | export FBX predefinição Mixamo 2K |
| 2026-10-10 16:44–16:54 | 10 exports e downloads (props com predefinição Blender 2K); 2 texturas grátis (piso #3, parede #3) em `arte/tripo/texturas/` | 19 435 | 0 | `limo` e `bolo` exportados 2× por clique lento/504 (duplicata idêntica apagada); o barril chegou como `barril (1).zip` porque já havia um `barril.zip` de outro projeto na pasta Downloads (não tocado) |
| 2026-10-10 16:55 | **Lote 3 FECHADO: 490 créditos** (10 × 45 + 2 × 20); 19 925 → 19 435 | 19 435 | 490 | dentro do teto de 550 |
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
| bruto | 2/#10 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → Auto Rig Mixamo → FBX Mixamo 2K | #3 de 4 (Nano Banana 2) | `e40525cf-36e0-40be-bf13-4967b9c5a084` | §2.1 | 65 / 65 | `arte/tripo/bruto/bruto.zip` (22999981 B) → `tripo_convert_c1fe3cb8-6ba4-4dc0-95bf-788b7654b593.fbx` | zip `d60df2447cdc2ab733e94a69b836c681ebf63b37d07b0084665267f896ab70f4` · fbx `70dd563ab55fbfb72fabf896dcebd45f4dfc4a7490113937e43a23f9a0fadb56` | tripo_pago | privada |
| ogro | 2/#11 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → Auto Rig Mixamo → FBX Mixamo 2K (sem algema/corrente, regra dos itens) | #2 de 4 | `ffb744ee-0127-4426-8d2a-25cd69986d5a` | §2.1 | 65 / 65 | `arte/tripo/ogro/ogro.zip` (22080047 B) → `tripo_convert_b3759572-2a4f-4828-8406-b4d37034b6ed.fbx` | zip `36f1ebfe4e0adf83ad9ec0f1e9f46b6ec75a12e31391ae140bc7041612789600` · fbx `1049c65596931b6843624b24bde40f1b4a745e3827cd015a1de5798bedccc912` | tripo_pago | privada |
| reliquia | 2/#12 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #1 de 4 | `0948a1dd-bc40-473f-b005-2cfef384216a` | §2.1 | 45 / 45 | `arte/tripo/reliquia/reliquia.zip` (4265990 B) → `tripo_convert_0a9e5d99-77bc-472c-b6fa-75f76fbe67f6.fbx` | zip `84d72bb486e05c1df320550b9ab0906ca8f47230f839c06c2ba955709fea492c` · fbx `dfbfda6edac8f1a74f739ba78254ffac0f015092a614f51e2026622161801639` | tripo_pago | privada |
| ovo | 2/#13 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #1 de 4 | `97947289-c235-4d6b-93d8-42aa551e54c7` | §2.1 | 45 / 45 | `arte/tripo/ovo/ovo.zip` (3139249 B) → `tripo_convert_85fb4d88-0e9a-4616-b2d1-e30625fb9f2b.fbx` | zip `ae4224e2f5a2e8e5e0f69804535d3ae7cdb103bcf608a68abafc90ca3a9d654e` · fbx `c1027189d6aa9533d08584677ca057d4a562ff6a9b422fd3b2a52c1a4abac0b6` | tripo_pago | privada |
| filhote | 2/#14 | 2026-10-09 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #4 de 4 | `5b382495-08dc-46b0-837f-5c00ff987ebf` | §2.1 | 45 / 45 | `arte/tripo/filhote/filhote.zip` (2550100 B) → `tripo_convert_06e11dbc-91ec-402d-a13a-925df6df8ae2.fbx` | zip `6ff001f0be9b9c2c7103c3dd1a2d471d20fc375bf8493fc22341fe2920c6da5e` · fbx `42770dbe778dadc3f94ebe8ff10ec625cc7670b07b85e690c0d704980eaf106d` | tripo_pago | privada |
| arqueiro | 3/#21 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → Auto Rig Mixamo → FBX Mixamo 2K (arco como item separado: `arcoflecha`) | #3 de 4 (Nano Banana 2) | `3e0e3a23-b2fe-412c-bc8d-10e983bb474a` | ASSETS §8 | 65 / 65 | `arte/tripo/arqueiro/arqueiro.zip` (27701976 B) → `tripo_convert_85f16e73-1d33-4f0e-96ba-1abd3556e020.fbx` | zip `c99497aa1fd12f2377cff4614d62fde7a3fe0add40f5122f2d03fad98e5b5d58` · fbx `7187bd401688c661d846b7459c91f3e5e4453b0d621e67609cda38c582d063ee` | tripo_pago | privada |
| arcoflecha | 3/#22 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K (item da mão do arqueiro) | #4 de 4 (Nano Banana 2) | `0b74c8a3-fdd1-45e8-be40-aad423f5217b` | ASSETS §8 | 45 / 45 | `arte/tripo/arcoflecha/arcoflecha.zip` (3388533 B) → `tripo_convert_c9a4beee-cb41-4ea4-82f8-07bcf0f4033b.fbx` | zip `f70f9bc5d2c0e1c1094642e279ee754c59fbac2bd4d7a50ff8a5a8f9310b00dc` · fbx `3ddcbd98bea2f96ee038a13fee36c90bd853f23becd06bfc24d91088961cb966` | tripo_pago | privada |
| capitao | 3/#23 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → Auto Rig Mixamo → FBX Mixamo 2K | #3 de 4 (Nano Banana 2) | `21467b7c-64d5-4901-9e49-2988fa6358e7` | ASSETS §8 | 65 / 65 | `arte/tripo/capitao/capitao.zip` (26121892 B) → `tripo_convert_041909b7-7bab-4de0-9d28-6afe3ac6d296.fbx` | zip `150dfc89bec20cbc9010a2807b284286648d214ec9b2afb6d941420eed28483b` · fbx `722a974c2928fe56a057131bae40204c53838c08824a6c8f6cb6e79e16f40f4f` | tripo_pago | privada |
| limo | 3/#24 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #2 de 4 (Nano Banana 2) | `472ae4d5-91f7-492b-a3d2-33d9ce806cc6` | ASSETS §8 | 45 / 45 | `arte/tripo/limo/limo.zip` (2215772 B) → `tripo_convert_abe03ed7-4684-4de3-8924-dc237855e7c8.fbx` | zip `c345270623a51bb186b42b8e23f3c72afb143803de976745ed5ab164efc797dc` · fbx `6ff02af36652904e5db4ea5b1fa62d2f702327cbc40155155d2155f7f2005eba` | tripo_pago | privada |
| bolo | 3/#25 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #4 de 4 (Nano Banana 2) | `c93b8272-ba6d-4987-b9f8-7e0082025ff9` | ASSETS §8 | 45 / 45 | `arte/tripo/bolo/bolo.zip` (3063760 B) → `tripo_convert_26283e7d-301a-4497-a1f3-0153c67eb2eb.fbx` | zip `af2382dfcf6c630613013a00264a732dd3e51762fa188c21ccfd5db895d9743a` · fbx `c7d5b90429ebcf521e0636bee87031d06c243327b4e6fea32f3eceed2af42a80` | tripo_pago | privada |
| mensagem | 3/#26 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #2 de 4 (Nano Banana 2) | `1f0a3224-35b1-452a-9592-57bfa8d8fb7b` | ASSETS §8 | 45 / 45 | `arte/tripo/mensagem/mensagem.zip` (3667107 B) → `tripo_convert_faa6da00-0aa7-4e32-9185-9d35387b1262.fbx` | zip `0dc89049da49142d97bef7ffeea03a0be21590839f48c99e2f0fd21c82977281` · fbx `c425fa80a9cc2d89f491582950b742804bd177f9b6614d57baa0bf8b08d71709` | tripo_pago | privada |
| caixa_pesada | 3/#27 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #2 de 4 (Nano Banana 2) | `de82a310-1f85-4891-867b-b244d5cb232c` | ASSETS §8 | 45 / 45 | `arte/tripo/caixa_pesada/caixa_pesada.zip` (4710099 B) → `tripo_convert_338430a8-acd6-444e-ba0b-976809db7699.fbx` | zip `f7268e972d46922bafaadbc046a246b49d63dc5dca5e22c34308812ac1cb1392` · fbx `f8f8722cc848202cb98b6aae86209185e77c33ff8296197800c92082f2e67625` | tripo_pago | privada |
| barril | 3/#28 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #1 de 4 (Nano Banana 2) | `0e6575ae-280c-49d4-b441-f392244255f3` | ASSETS §8 | 45 / 45 | `arte/tripo/barril/barril.zip` (3468737 B) → `tripo_convert_56311beb-78aa-403f-8415-33351a27b027.fbx` | zip `117932df02c3409237b6d66f4e0fd8658b10b9f84d0d10f39e5b9b4e500852c1` · fbx `0325c075e5deb11982471ba07ea05af09db8752f4b5538549aa06608b082a41f` | tripo_pago | privada |
| tocha | 3/#29 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #3 de 4 (Nano Banana 2) | `fa9f4dd2-732b-457d-af7e-ec05ca36c5e1` | ASSETS §8 | 45 / 45 | `arte/tripo/tocha/tocha.zip` (3467689 B) → `tripo_convert_34fd9e82-5f85-4270-a812-3e9b8489131f.fbx` | zip `25c05c67b02e6db4b7bc771861a1d5ca294a598aceda0bb083f661f562cf0413` · fbx `1730741ae56e1c3c491bcf81cfab1bf45896784a14a6ea82d0d5dcb0062f8606` | tripo_pago | privada |
| pilar | 3/#30 | 2026-10-10 | Tripo Studio: imagem → HD H3.1 → FBX Blender 2K | #2 de 4 (Nano Banana 2) | `ae5e9068-69a0-4945-8670-19edd91de1b4` | ASSETS §8 | 45 / 45 | `arte/tripo/pilar/pilar.zip` (4229056 B) → `tripo_convert_429920c7-f106-4bc9-8ce6-94a05d54681b.fbx` | zip `250dce793adf50e549471e97ab91bc602fd3898bdb40b7252cb35f5029d3e48e` · fbx `1e74509a7b40faebe626a122b3093318e48865d96250eee3e41f4357808b2b27` | tripo_pago | privada |

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
- bruto (Lote 2): "hulking brute warrior, very broad shoulders twice the head size, long arms reaching the knees, huge stone-and-iron gauntlets on both fists, small bald head with a heavy brow, rust-orange leather and fur armor, grey stone-colored skin, no weapon in the hands, " + [S] + [T] + [I]
- ogro (Lote 2): "giant dungeon ogre boss, huge round belly, enormous fists, small head with two lower tusks, mossy green skin, dark brown leather loincloth and belt, necklace of big bones, empty hands, no weapon, no shackle, no chain, " + [S] + [T] + [I]
- reliquia (Lote 2): "fragile ancient relic urn, slender white porcelain body with thin gold filigree, two delicate handles, small cyan gem on the lid, looks precious and breakable, " + [P] + [IP]
- ovo (Lote 2): "big creature egg, cream shell with coral orange spots, resting in a woven straw nest basket with two handles, " + [P] + [IP]
- filhote (Lote 2): "baby dragon hatchling, big round head, big friendly eyes, tiny wings, short tail, coral orange scales and cream belly, sitting pose, " + [P] + [IP]

## 3. Clipes Mixamo

**Estado (2026-10-10, raia G):** nenhum clipe foi baixado para o DC. Os clipes dos Lotes 2 e 3 vêm de reuso (Forge, ARKANA) ou foram animados no Blender. Os clipes do Blender de 2026-10-09 foram refeitos no polimento.

**Reuso:** aval do Vinicius em 2026-10-09 para reusar os clipes Mixamo já no disco em outros projetos dele, como cópia e sem alterar a origem. Valem duas notas:
- **"reuso 03_FORGE_STREET"**: arquivo, nome e SHA conferidos com `03_FORGE_STREET/docs/PROVENIENCIA.md`, bloco "clips Mixamo". São X Bot, FBX Binary, Without Skin, 30 fps, sem redução, baixados em 2026-10-06.
- **"reuso ARKANA"**: origem em `ARKANA/mobile-unity/Assets/_Arkana/Resources/mixamo-*.fbx`, nome exibido conforme `ARKANA/design/pipeline/MIXAMO.md`. São Y Bot, FBX for Unity, Without Skin, 30 fps, baixados em 2026-10-04.
  - Os runs e os walks vêm **com root motion**, apesar do "In Place" do MIXAMO.md: o quadril anda 2,2 m e 3,1 m nos runs e 1,39 m no `loc-andar-frente`.
  - O `render_sprites.py` tira a deriva e a converte em `stride_mps`.
- SHA das origens conferido em 2026-10-10: igual ao das cópias da tabela.

O esqueleto `mixamorig:` (65 ossos, repouso em T) é o mesmo nos dois bonecos e nos rigs do Tripo (conferido no Blender).
- **Exceção:** o bruto e o ogro foram rigados pelo Tripo a partir de imagem em pose A, com o braço em repouso a −49,5° e −22°.
- O render alinha os braços deles ao T do clipe (`--align-arms`, `RENDER_SPRITES.md` §1).

**Clipes criados no Blender** (o que não havia no disco): keyframes por script em `client/tools/anim_blender.py`, sobre o armature do X Bot de `courier/Idle.fbx` (reuso Forge).
- **SHA do script final:** `4422531b31abbe815e2a697267a671a468b40b218713d4fb4624a39aed0ea24e`.
- Os 12 clipes de 2026-10-10 (todos, menos o `Punch`) saíram num só export (`ANIM_OK 12 clipe(s)`, com a checagem `verify()` de repouso e de 1º e último quadro). Esse export usou a versão anterior do script, `7d61397ed10c5acc2f4066990da517402c3ffd686f8fda26368632f2d75d6b7b`.
- A versão final só acrescenta o clipe `Punch`, exportado depois dela; as chaves dos outros clipes não mudaram.
- **Exceção:** o `Hold.fbx` é o export de 2026-10-09.
- Saída canônica em `arte/mixamo/_blender/`, copiada para o conjunto.
- **Licença:** animação original do estúdio; nomes e repouso do esqueleto Mixamo.
- O SHA do FBX muda a cada export (o FBX grava data). Re-rodar o script gera um arquivo equivalente, não idêntico.

| Arquivo (`arte/mixamo/...`) | Conjunto | Origem (nome exibido) | Configuração | Data | SHA-256 |
|---|---|---|---|---|---|
| `Hold.fbx` (= `_blender/Hold.fbx`) | courier, ladrao, capitao (`--hold`) | **criado no Blender** (`Hold`; export de 2026-10-09, script `e77d98c9…ddfa3`, sem mudança) | 10 quadros, 30 fps, só armature | 2026-10-09 | `b7813e2469fbb2346a3f6c8832f3d975f903ad472020fc921b7c970cedc62674` |
| `courier/Run.fbx`, `courier/CarryRun.fbx` | courier | **reuso ARKANA**: `mixamo-loc-sprint.fbx` = "Sprint — Standard Sprint" | Y Bot, root motion (tirado no render); 8 quadros de 2 em 2 (preset) | 2026-10-09 (origem 2026-10-04) | `fd00884dc16483779fe2d0eec446dae812a26b6bb86b3497a7f44459aeba4ad7` |
| `courier/Idle.fbx`, `courier/CarryIdle.fbx`, `cliente/Idle.fbx`, `bruto/Idle.fbx`, `ogro/Idle.fbx`, `arqueiro/Idle.fbx`, `capitao/Idle.fbx` | courier, cliente, bruto, ogro, arqueiro, capitao | **reuso 03_FORGE_STREET**: `arte/mixamo/Idle.fbx` = Breathing Idle | X Bot; o render usa só os quadros 199–289 (laço de 3,0 s, preset `BREATH`) | 2026-10-09 e 2026-10-10 (origem 2026-10-06) | `befaa0fa9c5684a4e82dd229b04710588b1f32bc026704cc1856cae9c80f4220` |
| `courier/Throw.fbx` (= `_blender/Throw.fbx`) | courier | **criado no Blender** (`Throw`, refeito: antecipação, chicote, acompanhamento) | 21 quadros | 2026-10-10 | `0ceb6b0140ca7d4d369a008976007b673eea6cb2bab10e52536bb5674c9ef02b` |
| `courier/Dash.fbx` (= `_blender/Dash.fbx`) | courier | **criado no Blender** (`Dash`, refeito) | 16 quadros | 2026-10-10 | `28b796c7f2af88d8f8f26cfe79191939c384d3b694ff26c8756aa31fcdd4f466` |
| `courier/Hit.fbx`, `arqueiro/Hit.fbx` (= `_blender/Hit.fbx`) | courier, arqueiro | **criado no Blender** (`Hit`, refeito: braços à frente, sem T) | 15 quadros | 2026-10-10 | `d68760582fda4d4dcf6a444f66a6cbf7f50720ebd69969b23d2498704cb29a63` |
| `courier_fim/Cheering.fbx`, `cliente/Cheering.fbx` | courier_fim, cliente | **reuso 03_FORGE_STREET**: `Cheering.fbx` = Cheering (Male Cheering With Two Fists Pump) | X Bot | 2026-10-09 | `239cecccd1f25fd48e4ae1cbf88d79957402afe32032cb7bd1dbdc4d66c72985` |
| `courier_fim/Dying.fbx`, `ogro_fim/Dying.fbx` | courier_fim, ogro_fim | **reuso ARKANA**: `mixamo-standing-react-death-backward.fbx` = "Standing React Death Backward" (Pro Magic Pack) | Y Bot | 2026-10-09 e 2026-10-10 | `9b78551d6a64216f444b7b46b10b567e3e9bd02a5916317195e531e20d3c04ac` |
| `ladrao/Run.fbx`, `ladrao/CarryRun.fbx`, `capitao/Run.fbx`, `capitao/CarryRun.fbx` | ladrao, capitao | **reuso ARKANA**: `mixamo-loc-correr-frente.fbx` = "Unarmed Run Forward — Running Forward" | Y Bot, root motion (tirado no render) | 2026-10-09 e 2026-10-10 | `797cd01c22279321164ae497a5552fd4102df974bd8910db8e355d0930f3143d` |
| `ladrao/Lurk.fbx` | ladrao | **reuso 03_FORGE_STREET**: `LookingAround.fbx` = Looking Around (Idle Stand Looking Around) | X Bot | 2026-10-09 | `9c80441200a9e6c3c13b570e65183b7d3faeb6ff0f0dc1a94785a4dab144d930` |
| `ladrao/Grab.fbx`, `capitao/Grab.fbx` (= `_blender/Grab.fbx`) | ladrao, capitao | **criado no Blender** (`Grab`, refeito: 0,5 s = `ThiefGrab`, termina no `Hold`) | 16 quadros | 2026-10-10 | `0c431dbde330dba2aa06f8c3bc8a456316bea18f37c9003492379f3cf22684b5` |
| `guarda/Idle.fbx` (= `_blender/GuardIdle.fbx`) | guarda | **criado no Blender** (`GuardIdle`, re-exportado com o pé no chão) | 60 quadros, laço | 2026-10-10 | `0cc84d852d5c12efd2c3484601c2e494492935de0d1f2a3b477a4a07a3b6635a` |
| `guarda/Bash.fbx` (= `_blender/Bash.fbx`) | guarda | **criado no Blender** (`Bash`, refeito: impacto no quadro 6 de 12) | 30 quadros | 2026-10-10 | `3ec55e39369447e8df0ea9399306eabb54e5fe4e9d3d796f8b45512423d3f2fd` |
| `cliente/Shrug.fbx` (= `_blender/Shrug.fbx`) | cliente | **criado no Blender** (`Shrug`: dar de ombros de frente; substitui o Looking Around) | 49 quadros | 2026-10-10 | `3d8ab75e6c9c3a23bfe4ef5af12cb631cddbf4b1ec58aa8ad60d7fa6852112cd` |
| `cliente/Thankful.fbx` | cliente | **reuso 03_FORGE_STREET**: Thankful | X Bot | 2026-10-09 | `5462c4529ed06b88caaa877918be24535b2474731489daa027c365b91bfe7e88` |
| `cliente/Waving.fbx` | cliente | **reuso 03_FORGE_STREET**: Waving | X Bot | 2026-10-09 | `d8f9444653475b44541eea77beb8e64f3eb3ee9b6ea1366460480e4489b91367` |
| `bruto/Walk.fbx`, `ogro/Walk.fbx` | bruto, ogro | **reuso ARKANA**: `mixamo-loc-andar-frente.fbx` = "Unarmed Walk Forward — Walking Forward" | Y Bot, root motion 1,39 m em 1,37 s (tirado no render) | 2026-10-10 (origem 2026-10-04) | `d3c8434bae7c51771b343f80b0f4b567408d8e268b0aa2343a8454d23629967f` |
| `bruto/Smash.fbx` (= `_blender/Smash.fbx`) | bruto | **criado no Blender** (`Smash`: manopla de cima para baixo) | 30 quadros | 2026-10-10 | `2d41f9f832b2666def5ce35444bcc1a02fc428543d7788d150e287568f84961d` |
| `ogro/Slam.fbx` (= `_blender/Slam.fbx`) | ogro | **criado no Blender** (`Slam`: dois punhos no chão) | 31 quadros | 2026-10-10 | `a3d3c9990d2458550c980c7002945c04ccf9ed3a15c300c89bcf870ac812aa1e` |
| `ogro/Roar.fbx` (= `_blender/Roar.fbx`) | ogro | **criado no Blender** (`Roar`: ruge com os punhos à frente do peito) | 41 quadros | 2026-10-10 | `98d3e82c39f485bafb058ba6590ff3a05a76ea79da8cb9a30a091decd75d3476` |
| `arqueiro/Walk.fbx` | arqueiro | **reuso ARKANA**: `mixamo-loc-andar-tras.fbx` = "Unarmed Walk Back — Walking Backwards" (recua de frente para o courier) | Y Bot, root motion (tirado no render) | 2026-10-10 (origem 2026-10-04) | `031fc26f5a72d41ddef545e1f47d4615df5ebb6de848e83469e5949c3afe958b` |
| `arqueiro/Shoot.fbx` (= `_blender/Shoot.fbx`) | arqueiro | **criado no Blender** (`Shoot`: puxa o arco e solta) | 37 quadros | 2026-10-10 | `843ea4bca4ae69c698a1fd156ea84a135653031b1c4ce83505664cad6a16760f` |
| `capitao/Call.fbx` (= `_blender/Call.fbx`) | capitao | **criado no Blender** (`Call`: grita com a mão na boca e acena) | 31 quadros | 2026-10-10 | `ce937b89e6f9d9a01a19eaeeadd80ff71cbb74091d608455ede3190f3f981ef9` |
| `capitao/Punch.fbx` (= `_blender/Punch.fbx`) | capitao | **criado no Blender** (`Punch`: soco direto; o `CaptainHit` da View; script final) | 30 quadros | 2026-10-10 | `2f94ac1aae15c18c42dfe4dc3e702e78cc77c9ab84b850aa303ea9ecee541023` |

**Testados e não usados** (ficam na origem, sem cópia em `arte/mixamo/`):
- Renderizados numa folha de teste e descartados (2026-10-09):
  - `mixamo-standing-run-forward.fbx`: braço erguido de magia.
  - `mixamo-standing-react-small-from-front.fbx`: o Hit do Blender lê melhor a ~100 px.
  - `mixamo-standing-1h-magic-attack-01.fbx`: 2,3 s e lê como feitiço.
- Só inspecionados, sem render:
  - `mixamo-pegar.fbx` ("Picking Up An Object With One Hand"), descartado por ter 3,5 s e usar uma mão só.
  - 2026-10-10, na folha de palitos do `RENDER_SPRITES.md` §9:
    - Forge `Angry.fbx`: 19 s de raiva parada, fraco para o Roar.
    - ARKANA `mixamo-standing-2h-magic-area-attack-01/02.fbx`: pancada no chão com pose de feitiço; o impacto não cai em 0,6 do clipe.

Fonte dos originais: mixamo.com, nas contas Adobe do Vinicius (X Bot no Forge, Y Bot no ARKANA). Nenhum modelo nosso foi enviado.

## 4. Imagens grátis, texturas e fontes (0 crédito)

| Arquivo | Ferramenta / origem | Prompt ou URL | Escolhida | Data | SHA-256 | Licença |
|---|---|---|---|---|---|---|
| `piso` (textura) | Tripo, gerador de imagem embutido (Nano Banana 2, 1:1, 1024×1024) | "seamless tileable texture of stylized dungeon stone floor slabs seen straight from above, top-down orthographic, square grey-blue stone tiles with dark mortar lines and subtle cracks, hand-painted stylized game texture, flat even lighting, no shadows, no perspective, no objects, fills the whole image edge to edge" | #3 de 4 | 2026-10-10 | `e43eb2fd096eeba3968d2b88e1bb2e37ed2aa26ffaada0e91710e7e8e98f18d8` | tripo_pago |
| `parede` (textura) | idem | "seamless tileable texture of stylized dungeon brick wall seen straight on, orthographic, chunky grey-blue stone bricks with dark mortar, hand-painted stylized game texture, flat even lighting, no shadows, no perspective, no objects, fills the whole image edge to edge" | #3 de 4 | 2026-10-10 | `d8822c377c2b3da0e5700096fe77407c3d22e4627891549206a1d96c2922c501` | tripo_pago |
| Lilita One | Google Fonts (github.com/google/fonts, `ofl/lilitaone`) | — | — | | | SIL OFL 1.1 |
| Nunito | Google Fonts (`ofl/nunito`) | — | — | | | SIL OFL 1.1 |

### 4.1 Áudio sintetizado (raia J, 0 crédito, sem asset de terceiros)

Gerado em 2026-10-10 por `client/tools/audio_gen.py` (Python 3.14.4, numpy 2.4.6, scipy 1.18.1), sem rede, sem serviço externo, sem amostra gravada: osciladores, ruído, filtros e Karplus-Strong escritos no script. Semente fixa por arquivo (crc32 do nome): rodar de novo dá o mesmo SHA (conferido em 2 rodadas do zero).
- `audio_gen.py`: SHA-256 `124f3ce16588af128a75480c3ada511e3fc1026651dc046423f2775859c79450` (calculado com fim de linha LF, o mesmo com checkout CRLF).
- Comando: `python client/tools/audio_gen.py --md` (gera os arquivos, confere pico/DC/laço e imprime estas linhas).
- Licença: própria (sintetizado). Os `.meta` de import do Unity saem do mesmo script e não entram na tabela. Detalhe de uso em `AUDIO.md`.

| Arquivo (`Resources/`) | Ferramenta | SHA-256 do script | SHA-256 do WAV | Licença |
|---|---|---|---|---|
| `Audio/sfx/passo_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `f39a5b154edcbcce986ad141d83bbbe18f6308533592ed79cbee7d1771c86560` | própria (sintetizado) |
| `Audio/sfx/passo_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `fb4b60e6f335420ed3e37c8a64453abb8f561ca77b5b431337f879d1ed287d21` | própria (sintetizado) |
| `Audio/sfx/passo_3.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `f2644359e58b0838a04167b606f4e6a0a28cf1b63bbca509c6c9bd9fbe76e492` | própria (sintetizado) |
| `Audio/sfx/pousar_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `caba1bd2e6143d75d9693af52ec92b5afd25f43d8ba74391a15fa20beac75d69` | própria (sintetizado) |
| `Audio/sfx/pousar_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `8da83944f0517b7bf900aed0310c2e18a4c42e109e986006f26f6fa5f5fcdc76` | própria (sintetizado) |
| `Audio/sfx/pegar_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `d87e844b80d1b4cdd3da100c3e66b72ff63cd562f771d7b1d455139c5615cc0b` | própria (sintetizado) |
| `Audio/sfx/pegar_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `f4c579864f281b532d02caab15af7145de23ed3f20c96ca1c7f90e035b9dc76b` | própria (sintetizado) |
| `Audio/sfx/carga_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `24112aea47cba80316d48c91555399cb4b73993c41c5c30bc3d00ba9768076a9` | própria (sintetizado) |
| `Audio/sfx/carga_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `93a52e393f37f6476bb19e425d3161def5cc9daecb65334ee224715283c52bfa` | própria (sintetizado) |
| `Audio/sfx/carga_3.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `c7289474fddb98b39aaf074d2c67fc35f9c389b2b1185bcbf8dcf0f4eac5bc08` | própria (sintetizado) |
| `Audio/sfx/racha_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `60ff11cd1391140f92bbf6508a63466b11a57b034fc34cc067b52715cabcb1d1` | própria (sintetizado) |
| `Audio/sfx/racha_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `6df7f0a62df42575abfef7e8bd7579b592a8cc16822a68083c9caedf02f4fb98` | própria (sintetizado) |
| `Audio/sfx/quebra_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `2afad2670936675d63dc845d00c96d69dd30859bc0aa4ef82c54f05089759689` | própria (sintetizado) |
| `Audio/sfx/ladrao_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `4985289d39f229529adda7273bf5f9c9b276a84f3b6c6303a22f23de02afe594` | própria (sintetizado) |
| `Audio/sfx/ladrao_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `1c8b275418ae0e0697dc2b95fd95c00d120ad66e071cbff72ee32926a354b2ca` | própria (sintetizado) |
| `Audio/sfx/dash_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `b5cbf7b557fc299cdf33e03e7c9eb320747ca9fdefaee3cb91c57a7142140d27` | própria (sintetizado) |
| `Audio/sfx/dash_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `b5819fe0e133f07e58591a9b98a6e0bdc298e56e38dc5044d8ef95d52ef98a2d` | própria (sintetizado) |
| `Audio/sfx/golpe_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `2fac15dc7eaf2f310fcd791dbc405bff255756182be4537c9a881d8142764495` | própria (sintetizado) |
| `Audio/sfx/golpe_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `a66092a0d34894f156c1dab36b9c26897814e78b1e62d618c81145d6253ae806` | própria (sintetizado) |
| `Audio/sfx/golpe_3.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `612fd224b4f322a483a8e966df581784e782af74690ba5c8cdc5575278b2ab60` | própria (sintetizado) |
| `Audio/sfx/tiro_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `527f8eafc1b20590b77353b2cd4aefa0ea96102fd2b26d8e2f932a29b32ad4f7` | própria (sintetizado) |
| `Audio/sfx/tiro_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `f8f3ed4ef2a0d44c022e52b9cc3eb2d915607d5a8060c5c1489a8916bc92c9ca` | própria (sintetizado) |
| `Audio/sfx/tiro_3.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `3e9f14dc22dde30ba9de36a6c2ccbeb70392ff303269fadc694da1e02da0c29f` | própria (sintetizado) |
| `Audio/sfx/flecha_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `60ddff054642ad74b5f7da08239a4098166ef58e593e32ea5e76f407c80f8176` | própria (sintetizado) |
| `Audio/sfx/flecha_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `b160af4303b184e37191f2d544ee22b4a2fe55ea2f8552bf97e36eb85f234295` | própria (sintetizado) |
| `Audio/sfx/mira_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `eb014c551f0c0695995096b7d827c4851c7045458ba21f515be8490bc315a130` | própria (sintetizado) |
| `Audio/sfx/bumerangue_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `084efbebd19d010a25cdcecff0bb5dcbcceea44781868daa126ae1b862ec2897` | própria (sintetizado) |
| `Audio/sfx/bomba_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `440501e522360c7670c91258c13c860696e72d50852982a6cc3ecddf3033de89` | própria (sintetizado) |
| `Audio/sfx/bomba_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `c458288e4168e95ac82653bdef367eba15f03ab3812a09bf5a5d3514d549f044` | própria (sintetizado) |
| `Audio/sfx/explosao_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `cd5da4ed08379211d17e500a3458833527781538e8424282c15017f8803011d4` | própria (sintetizado) |
| `Audio/sfx/explosao_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `7d75fb6ea7e4223731188c88511309f2025015227f3e3530271fc9b221482dbe` | própria (sintetizado) |
| `Audio/sfx/morte_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `7eece042c8c01e63d242ed9c587f445d6cc06b01bd815ca4cb9f9c5a01b24f3c` | própria (sintetizado) |
| `Audio/sfx/morte_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `ad52a97e2efc897e39657023c0bba5531331c53ccc34dfcada599673e923af7c` | própria (sintetizado) |
| `Audio/sfx/morte_3.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `92443cf53c8233f4d0e812d367c52612cb24df68fda840acd8ec3532a14fd5f5` | própria (sintetizado) |
| `Audio/sfx/dano_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `c3935d2bf3e4f0c1728248efa495aef23a3a09aca9d246e3fe9c21aa261f9234` | própria (sintetizado) |
| `Audio/sfx/dano_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `a71dae1261d9a64f0b61e92991f050b999761909ab9a7d1210681114626a4d4f` | própria (sintetizado) |
| `Audio/sfx/aviso_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `a7f8da0a2f9bd6c30052c39515d73718bbac29140514346d6b5e70e0fe3923aa` | própria (sintetizado) |
| `Audio/sfx/porta_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `d4ae973f3b3297b3d016ce5565f90c4b808cdec7e741f90b019a34596a0b7987` | própria (sintetizado) |
| `Audio/sfx/porta_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `dcfa7c01a72255521450b2826b3a9cf3ed25c43a3cfe70ea3cb14bb98f60bac1` | própria (sintetizado) |
| `Audio/sfx/bloqueio_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `115bdeae985df153e9eb46bad5f410d1f2a4bc99793af7c07bc66959f9183adc` | própria (sintetizado) |
| `Audio/sfx/bloqueio_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `65451516b5724d65f439e14da645bf5a96f0547582e3b2fbefe1a047b2874b5c` | própria (sintetizado) |
| `Audio/sfx/bau_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `88085f10e8245dc5bebe631d1746a681566332cc1727bd876ee169fc4e51dcdb` | própria (sintetizado) |
| `Audio/sfx/moedas_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `ecc152e366309549fe810f4b4aac660554f4e9c623326dfa219576dd93dd0698` | própria (sintetizado) |
| `Audio/sfx/moedas_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `609883b6ee767f794ee7a41ae3135abef21a1aa499c6df9a5f870fd93f4fe820` | própria (sintetizado) |
| `Audio/sfx/moedas_3.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `5202e7cd769341243a8e72f061505bc018498c1e6d9116ffe8c3c297cc4ea369` | própria (sintetizado) |
| `Audio/sfx/entrega_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `6c0e9413895a163d2d85e0db9737d300c901ab1bd7359aa1eb5681c3f4bde163` | própria (sintetizado) |
| `Audio/sfx/entrega_rachada_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `dd0b4e48510c1d365d61440fb3fcf6602caa19576d3d96af89dde564cf052ba5` | própria (sintetizado) |
| `Audio/sfx/falha_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `808eb78cd82af89ba03add1f1329dd9bb9899dfd808be3557c45ecc61de1c844` | própria (sintetizado) |
| `Audio/sfx/perk_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `f2c7d207930f2180924b1c3cb5ee962c76c3c92d747b2da4c2bacd463fc96a6a` | própria (sintetizado) |
| `Audio/sfx/clique_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `c0aae326cfdd98d122de1280af05b2adf98a7492c50e2434866022a530082875` | própria (sintetizado) |
| `Audio/sfx/clique_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `19f29c0acb9f2fb4a7a513ccfd5a0cc3ea69caf4f6811b2c37b16c78ffc7b455` | própria (sintetizado) |
| `Audio/sfx/confirmar_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `88c95212c1b1a3311a4913bdd6afcd5965c3eded685b785b6f0c22f5827429cb` | própria (sintetizado) |
| `Audio/sfx/estrela_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `85bf9fd9bd8fda48d95c6fc48a1c7a7fc99020493da93de0696c559123438733` | própria (sintetizado) |
| `Audio/sfx/choca_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `dc6aea35a7dc9943f1afb007aa916f4e159db2dd10d5c000d4ba6a32dd620ba1` | própria (sintetizado) |
| `Audio/sfx/limo_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `024ac6ca45fd03524105f07d31d3204834d0a5df46050d5e5e7b2e90fd98e39b` | própria (sintetizado) |
| `Audio/sfx/limo_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `d7144f8a7ad9a90f01550eb947e646ccfe7f80826d0953c9a46a91283f165b0c` | própria (sintetizado) |
| `Audio/sfx/limo_3.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `dc53389329f9ee30cdfa87591c43198e2c6238f1dd688033583604ca9787624e` | própria (sintetizado) |
| `Audio/sfx/rugido_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `8948597d3455a177b3bb99cc930dee952dca01469d32066fb63223b985a46bd2` | própria (sintetizado) |
| `Audio/sfx/pancada_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `2e937e8ee384dfa2416936297a58a9d250085535a4914255b723217d260b6df1` | própria (sintetizado) |
| `Audio/sfx/pancada_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `2e896aea86b5fc20c62988af65837aba0684c7f37aa770d45353a6e0102647ef` | própria (sintetizado) |
| `Audio/sfx/compra_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `b6f721fe69ecb29a3e583702d59a5567634191349e4ca438676753116f358089` | própria (sintetizado) |
| `Audio/sfx/tique_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `63f45014592d6206a6a9bc42c89e0e391fc2eba66eb3bb33c81b216dda46c41e` | própria (sintetizado) |
| `Audio/sfx/tique_2.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `db10f9c7b280e3f0f1024c62c4dcf693e3ea8534b9b07adb34f341f84c251e0d` | própria (sintetizado) |
| `Audio/sfx/segredo_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `ae6d1416a7a9e98bddb54af29516b074c5a614687d7508e8dee5f317dedbb44f` | própria (sintetizado) |
| `Audio/sfx/balanca_1.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `193d2c21118e59b8c98c828163429656fa04f01f45cc085ede0088c1903b018e` | própria (sintetizado) |
| `Audio/musica/menu.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `fa788d2ac63ce783097d04adbd95e51b71fe12d5f540cad730fc031fd90d76b8` | própria (sintetizado) |
| `Audio/musica/masmorra.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `f4c8c2f281928ef8bcc7c928efd2d8a26ad1046dd60f6a182fda21f5a4c7cc2e` | própria (sintetizado) |
| `Audio/musica/chefe.wav` | `client/tools/audio_gen.py` (numpy/scipy) | `124f3ce1…9450` | `0cafb9d8deaa7ac7bd2d72e4b4229fef1d646065eea23d5e4ec987962fe36cf2` | própria (sintetizado) |

## 5. Sprites renderizados (0 crédito)

**Re-render de 2026-10-10 pela raia G** (polimento + Lotes 2 e 3): Blender 5.2.1 LTS headless, 0 crédito, sem rede. Os 24 conjuntos saíram com os mesmos scripts:
- `render_sprites.py`: SHA-256 `b463a929ceec5b25e12be59c76b898dad18876b8fb1dffa41a3a1f53fba8ea7d`.
- `render_sprites.ps1`: `aa22a3a3638d40bfb0d90374ec17132afec0e3a86a549b21d1635747ec499fca` (sem mudança).
- `folhas.py`: `0a8849115c628d3da9e2cb93de2a4852aadcd661e74b982e0d53bda8ccb9268b`.
- Clipes: tabela do §3.

Os FBX de entrada são cópias de `arte/tripo/` em `%TEMP%\dc_in\` (caminho curto). O SHA bate com o §2 nos 24 arquivos (conferido em 2026-10-10).
- Comandos: `RENDER_SPRITES.md` §2.
- Licença: tripo_pago + clipes do §3. Visibilidade: privada.
- O render de 2026-10-09 (raia D, script `6d435e16…1ec9`) foi substituído por este.

| Pasta `Resources/Sprites/` | Data | Entrada (FBX + SHA-256) | Clipes (quadros) / Hold | Comando | Folha de conferência |
|---|---|---|---|---|---|
| `courier` | 2026-10-10 | `tripo/courier/tripo_convert_f2e9a770….fbx` `21b7bafc…8f9b` | CarryIdle 12, CarryRun 8, Dash 6, Hit 6, Idle 12, Run 8, Throw 10 / `Hold.fbx` | `-Nome courier -Anims mixamo\courier -Hold mixamo\Hold.fbx` | `courier.png`, `courier_folhas.png` |
| `courier_fim` | 2026-10-10 | o mesmo FBX do courier | Cheering 12, Dying 12 | `-Nome courier_fim -Anims mixamo\courier_fim` | `courier_fim.png`, `courier_fim_folhas.png` |
| `ladrao` | 2026-10-10 | `tripo/ladrao/tripo_convert_78892eb1….fbx` `a72377a9…b5ff` | CarryRun 10, Grab 6, Lurk 12, Run 10 / `Hold.fbx` | `-Nome ladrao -Anims mixamo\ladrao -Hold mixamo\Hold.fbx` | `ladrao.png`, `ladrao_folhas.png` |
| `guarda` | 2026-10-10 | `tripo/guarda/tripo_convert_77906b18….fbx` `b7e80be1…60b2` + escudo `tripo/escudo/…` `11e7df9c…c04d` (`--attach`) | Bash 12, Idle 12 | `-Nome guarda -Anims mixamo\guarda -Extra @("--attach","<escudo>:LeftForeArm:0.09,0,0.06:90,180,0:0.75")` | `guarda.png`, `guarda_folhas.png` |
| `cliente` | 2026-10-10 | `tripo/cliente/tripo_convert_cb64fb7a….fbx` `9dded3b6…125b` | Cheering 12, Idle 12, Shrug 12, Thankful 12, Waving 6 | `-Nome cliente -Anims mixamo\cliente` | `cliente.png`, `cliente_folhas.png` |
| `bruto` | 2026-10-10 | `tripo/bruto/tripo_convert_c1fe3cb8….fbx` `70dd563a…db56` | Idle 12, Smash 12, Walk 12 | `-Nome bruto -Anims mixamo\bruto` | `bruto.png`, `bruto_folhas.png` |
| `ogro` | 2026-10-10 | `tripo/ogro/tripo_convert_b3759572….fbx` `1049c655…c912` | Idle 9, Roar 9, Slam 9, Walk 9 | `-Nome ogro -Anims mixamo\ogro` | `ogro.png`, `ogro_folhas.png` |
| `ogro_fim` | 2026-10-10 | o mesmo FBX do ogro | Dying 7 | `-Nome ogro_fim -Anims mixamo\ogro_fim` | `ogro_fim.png`, `ogro_fim_folhas.png` |
| `arqueiro` | 2026-10-10 | `tripo/arqueiro/tripo_convert_85f16e73….fbx` `7187bd40…63ee` + arcoflecha `tripo/arcoflecha/…` `3ddcbd98…b966` (`--attach`) | Hit 6, Idle 12, Shoot 12, Walk 12 | `-Nome arqueiro -Anims mixamo\arqueiro -Extra @("--attach","<arcoflecha>:LeftHand:-0.06,0,-0.015:90,180,0:0.8")` | `arqueiro.png`, `arqueiro_folhas.png` |
| `capitao` | 2026-10-10 | `tripo/capitao/tripo_convert_041909b7….fbx` `722a974c…0f4f` | Call 12, CarryRun 10, Grab 6, Idle 12, Punch 12, Run 10 / `Hold.fbx` | `-Nome capitao -Anims mixamo\capitao -Hold mixamo\Hold.fbx` | `capitao.png`, `capitao_folhas.png` |
| `caixa` | 2026-10-10 | `tripo/caixa/tripo_convert_dc019326….fbx` `9ee57b86…ed1c` | Static 1 | `-Nome caixa` | `caixa.png`, `caixa_folhas.png` |
| `bau` | 2026-10-10 | `tripo/bau/tripo_convert_70aa8043….fbx` `c3e05b91…17e0` | Static 1 | `-Nome bau` | `bau.png`, `bau_folhas.png` |
| `arco` | 2026-10-10 | `tripo/arco/tripo_convert_5ef411ad….fbx` `6eda5093…26ca` | Static 1 | `-Nome arco` | `arco.png`, `arco_folhas.png` |
| `morcego` | 2026-10-10 | `tripo/morcego/tripo_convert_ad567423….fbx` `793e39a1…d230` | Static 1 | `-Nome morcego` | `morcego.png`, `morcego_folhas.png` |
| `reliquia` | 2026-10-10 | `tripo/reliquia/tripo_convert_0a9e5d99….fbx` `dfbfda6e…1639` | Static 1 | `-Nome reliquia` | `reliquia.png`, `reliquia_folhas.png` |
| `ovo` | 2026-10-10 | `tripo/ovo/tripo_convert_85fb4d88….fbx` `c1027189…c0b6` | Static 1 | `-Nome ovo` | `ovo.png`, `ovo_folhas.png` |
| `filhote` | 2026-10-10 | `tripo/filhote/tripo_convert_06e11dbc….fbx` `42770dbe…106d` | Static 1 | `-Nome filhote` | `filhote.png`, `filhote_folhas.png` |
| `limo` | 2026-10-10 | `tripo/limo/tripo_convert_abe03ed7….fbx` `6ff02af3…5eba` | Static 1 | `-Nome limo` | `limo.png`, `limo_folhas.png` |
| `bolo` | 2026-10-10 | `tripo/bolo/tripo_convert_26283e7d….fbx` `c7d5b904…2a80` | Static 1 | `-Nome bolo` | `bolo.png`, `bolo_folhas.png` |
| `mensagem` | 2026-10-10 | `tripo/mensagem/tripo_convert_faa6da00….fbx` `c425fa80…1709` | Static 1 | `-Nome mensagem` | `mensagem.png`, `mensagem_folhas.png` |
| `caixa_pesada` | 2026-10-10 | `tripo/caixa_pesada/tripo_convert_338430a8….fbx` `f8f8722c…7625` | Static 1 | `-Nome caixa_pesada` | `caixa_pesada.png`, `caixa_pesada_folhas.png` |
| `barril` | 2026-10-10 | `tripo/barril/tripo_convert_56311beb….fbx` `0325c075…a41f` | Static 1 | `-Nome barril` | `barril.png`, `barril_folhas.png` |
| `tocha` | 2026-10-10 | `tripo/tocha/tripo_convert_34fd9e82….fbx` `1730741a…8606` | Static 1 | `-Nome tocha` | `tocha.png`, `tocha_folhas.png` |
| `pilar` | 2026-10-10 | `tripo/pilar/tripo_convert_429920c7….fbx` `1e74509a…2b27` | Static 1 | `-Nome pilar` | `pilar.png`, `pilar_folhas.png` |

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
