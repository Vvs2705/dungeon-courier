# Dungeon Courier — Assets da v0.1 (PROPOSTO, 2026-10-09)

**Status:** PROPOSTO pela raia B. Nada foi gerado e nenhum crédito foi gasto.

**Aval:** cada lote só começa com aval explícito do Vinicius ao total daquele lote (guardrail do estúdio; decisão de 2026-10-09). A raia não gera nada: planeja, e o coordenador pede o aval.

**Estilo:** `ART_BIBLE.md`. **Render:** `RENDER_SPRITES.md`. **Livro-razão:** `PROVENIENCIA.md`.

## 0. Receita e base de custo

**Base de custo** (medida no saldo do site no Forge Street): **45 créditos por modelo HD e 20 por Auto Rig**. Personagem rigado = **65**, prop = **45**.
- Fontes: `03_FORGE_STREET/docs/PROVENIENCIA.md` §1 (Lote 1 = 440 exatos), §3 (Lotes 2+3 = 640) e §7 (Lote 4 = 650: 10 × 45 + 10 × 20). Também `00_PESQUISA/PIPELINE_ARTE_LICOES.md` §4.
- As imagens de referência saem do gerador embutido e não custam crédito (4 por peça no Forge).

**Armadilha conhecida:** o painel pode voltar com 8K, textura 4K e 2 000 000 polígonos. Aí o botão mostra **"Gerar 65"** em vez de 45. Confira o botão antes de **cada** geração: se mostrar outro valor que não 45, pare e reporte.

### Passo a passo no Tripo (uma página nova por peça)

1. **Imagem de referência** no gerador embutido (Nano Banana 2 ou GPT Image 2, 1:1, 4 variações grátis), com assunto + sufixos.
   - **Personagem:** usar **Multi-View** se o painel oferecer. Gere frente, lado e costas com o mesmo prompt mais `front view`, `side view, profile` ou `back view`. A frente vai no slot frontal e o perfil verdadeiro no lateral (lição Arkana 2026-08-26). Se o Multi-View cobrar algo diferente de 45, pare e reporte.
2. **Modelo HD H3.1**, com estes parâmetros, conferidos a cada peça:
   - ~8 000 polígonos, triângulos;
   - textura 2K (nem 4K nem 8K);
   - Ultra, PBR, remover iluminação **ligado**;
   - Gerar em Partes **desligado**;
   - Pose T, se houver a opção;
   - **Privacidade = PRIVADO antes de gerar.**
3. **Veredito do Vinicius** sobre o modelo (foto do visualizador) **antes** do rig.
4. **Rig e export:**
   - **Personagem:** Auto Rig humanoide com esqueleto **Mixamo** (+20), pela URL `workspace/rigging/<task>`. Exportar FBX com predefinição Mixamo, 2K.
   - **Prop:** exportar FBX com predefinição Blender, 2K.
5. **Download** para `arte/tripo/<nome>/` (fora do git). Confira a pasta `Downloads` antes de sair da página. Registre o task id e o SHA-256 do zip e do FBX em `PROVENIENCIA.md`.

### Mixamo (0 crédito, conta Adobe do Vinicius)

- **Não suba o modelo do Tripo:** o upload tem teto de 10 MB e o FBX do Tripo tem ~12,6 MB (LICOES 2026-10-06).
- Baixe os clipes do **X Bot** como FBX Binary, **Without Skin, 30 fps, Keyframe Reduction none, In Place** nas locomoções.
- O `render_sprites.py` faz o retarget por delta de repouso.
- O nome do arquivo vira o nome do clipe no `meta.json`.
- Pastas: `arte/mixamo/<conjunto>/` (fora do git: a Adobe não permite redistribuir o arquivo cru).
- A Mixamo desloga após 24 h.

### Sufixos de prompt (inglês; colar depois do assunto)

- `[S]` personagem = "stylized fantasy comic game character, chunky proportions 3.5 heads tall, big hands and boots, flat colors with soft two-tone shading, clean readable silhouette, single character, centered, no background, no text, no logo"
- `[T]` pose = "T-pose, arms straight out horizontal, legs slightly apart, open empty hands, nothing held in the hands, neutral face, symmetrical, fitted clothes on the legs"
- `[I]` imagem de personagem = "full body front view, T-pose arms horizontal, soft even studio light, plain light grey background, 3D render look, no ground shadow" (o mesmo do Forge, validado em 14 personagens)
- `[P]` prop = "stylized fantasy comic game prop, chunky beveled shapes, flat colors with soft two-tone shading, single object, centered, no background, no text"
- `[IP]` imagem de prop = "3/4 front view product shot, plain light grey background, soft even light, no ground shadow"

## 1. Lote 1: fatia vertical mínima (C1/C2)

O que a C1 e a C2 mostram (`Defs.cs`): R1 (Guarda), R2 (1º Ladrão + **2 Morcegos**), R3 (Ladrão + Morcego + **baú**), R4 (4 Morcegos + baú) e D1 (**Cliente**), além de portas, a Caixa e o courier.

O **morcego já aparece na C1** (R2/R3), por isso entra aqui. Bruto, Ogro, Relíquia e Ovo só aparecem da C3 em diante.

| # | Asset (pasta = preset) | Tipo | Prompt (assunto; sufixos) | Pose / rig | Cr |
|---|---|---|---|---|---|
| 1 | Courier (`courier`, `courier_fim`) | personagem rigado, **Multi-View** | young human courier of an adventurers' delivery guild, friendly determined face, big wide-brim blue courier cap with a small brass guild badge, short blue tunic over a cream shirt with rolled sleeves, bulky brown leather cross-body messenger satchel with buckles, short cream scarf, fingerless leather gloves, sturdy brown boots `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
| 2 | Ladrão (`ladrao`) | personagem rigado | sneaky goblin thief, olive green skin, long pointy ears, big greedy grin, slightly hunched, deep purple pointed hood and short ragged purple cape, dark leather vest, huge empty burlap sack tied on his back, wrapped bare feet, chunky proportions 3 heads tall with a big head `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
| 3 | Guarda (`guarda`) | personagem rigado | dungeon gate guard, heavy steel plate armor, closed great helm with a short dark red crest, tall rectangular steel tower shield with a dark red band strapped flat to the left forearm (not touching the body), dark red tabard, short mace hanging on the belt `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
| 4 | Cliente (`cliente`) | personagem rigado | plump cheerful merchant collector, round spectacles, curly grey mustache, wide-brim green hat with a long feather, green coat ending above the knees with gold buttons, leather boots `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
| 5 | Morcego (`morcego`) | prop (animado por código, §4) | chubby cartoon dungeon bat, round body, big wing membranes spread wide, large pointy ears, small fangs, big white eyes, burgundy fur and rose-pink wings, flying pose `[P][IP]` | — | **45** |
| 6 | Caixa (`caixa`) | prop (carga) | sturdy wooden delivery crate, chunky planks, iron corner brackets, crossed rope straps with a carry knot on top, round blue wax guild seal on the front, blank paper label `[P][IP]` | — | **45** |
| 7 | Baú fechado (`bau`) | prop | small dungeon treasure chest, closed, rounded wooden lid, iron bands, big brass lock, studs `[P][IP]` | — | **45** |
| 8 | Baú aberto (`bau_aberto`) | prop | o mesmo assunto com "lid wide open, pile of gold coins inside with a warm glow" `[P][IP]` · **cortável:** sem ele, a abertura usa o `bau` com flash e moedas voando | — | **45** |
| 9 | Arco da porta (`arco`) | prop | dungeon doorway, chunky grey-blue stone block archway, rounded arch top with a keystone, empty dark opening, no door `[P][IP]` (a grade levadiça é procedural) | — | **45** |

> **Lote 1: 4 personagens × 65 + 5 props × 45 = 485 créditos.**
> Teto pedido no aval: **550** (485 + uma refação de personagem). Sem o baú aberto: 440.

**Clipes Mixamo do Lote 1** (0 crédito, ~15 downloads; CarryRun e CarryIdle são cópias): §4 para courier, ladrão, guarda e cliente, mais o `Hold.fbx`.

**Mão de obra estimada** (HIPÓTESE):
- ~4 h 30 de operador (Tripo + Mixamo + downloads + ledger) e ~10 min de render.
- Referências: Forge, 8 modelos + 7 clipes em ~1 h com 3 abas; COE, 6 NPCs em um dia.

## 2. Lote 2: resto da v0.1

| # | Asset | Tipo | Prompt (assunto; sufixos) | Pose / rig | Cr |
|---|---|---|---|---|---|
| 10 | Bruto (`bruto`) | personagem rigado | hulking brute warrior, very broad shoulders twice the head size, long arms reaching the knees, huge stone-and-iron gauntlets on both fists, small bald head with a heavy brow, rust-orange leather and fur armor, grey stone-colored skin, no weapon in the hands `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
| 11 | Ogro, chefe (`ogro`, `ogro_fim`) | personagem rigado | giant dungeon ogre boss, huge round belly, enormous fists, small head with two lower tusks, mossy green skin, dark brown leather loincloth and belt, necklace of big bones, iron shackle on one wrist with a broken chain end, no weapon in the hands `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
| 12 | Relíquia frágil (`reliquia`) | prop (carga) | fragile ancient relic urn, slender white porcelain body with thin gold filigree, two delicate handles, small cyan gem on the lid, looks precious and breakable `[P][IP]` | — | **45** |
| 13 | Ovo (`ovo`) | prop (carga) | big creature egg, cream shell with coral orange spots, resting in a woven straw nest basket with two handles `[P][IP]` | — | **45** |
| 14 | Filhote (`filhote`) | prop (animado por código, §4) | baby dragon hatchling, big round head, big friendly eyes, tiny wings, short tail, coral orange scales and cream belly, sitting pose `[P][IP]` | — | **45** |

> **Lote 2: 2 × 65 + 3 × 45 = 265 créditos.** Teto pedido no aval: **330**.

Os obstáculos ficam procedurais (os retângulos do Core) e custam 0.

## 3. Lote 3: opcional (polimento, depois do playtest da v0.1)

| # | Asset | Tipo | Assunto | Cr |
|---|---|---|---|---|
| 15 | Cliente 2 | personagem rigado | elderly wizard librarian, tall pointed hat, round glasses, short robe over trousers, scroll case on the back `[S][T][I]` (GDD C3: 3 clientes) | 65 |
| 16 | Cliente 3 | personagem rigado | cheerful baker woman, puffy chef hat, flour-dusted apron, rolled sleeves, rosy cheeks `[S][T][I]` | 65 |
| 17 | Caixa aberta (`caixa_aberta`) | prop | o assunto da #6 com "lid off, straw packing inside" (o cliente abre a caixa, GDD C3) | 45 |
| 18 | Tocha de parede | prop | iron wall sconce with a burning torch `[P][IP]` (alternativa: Blender por primitivas, 0) | 45 |
| 19 | Pilar | prop | squat dungeon stone pillar, chunky blocks, cracked edges `[P][IP]` (decora os obstáculos de 1,2 m) | 45 |
| 20 | Grade levadiça | prop | iron portcullis gate, thick bars, pointed bottom tips `[P][IP]` (troca a procedural) | 45 |

> **Lote 3: 310 créditos** (só se o playtest pedir).

### Totais

| Lote | Modelos | Rigs | Créditos | Teto pedido no aval |
|---|---|---|---|---|
| 1 | 9 | 4 | **485** | 550 |
| 2 | 5 | 2 | **265** | 330 |
| **v0.1 (1 + 2)** | 14 | 6 | **750** | 880 |
| 3 (opcional) | 6 | 2 | 310 | — |

**Saldo:** a última posição conhecida do saldo é 20 720 (Forge, 2026-10-08), num plano de 25 200 por mês. A v0.1 inteira cabe com folga, mas o aval continua sendo por lote.

**Economia possível** (pede o aval do Vinicius, porque copia entre repositórios):
- O baú do Forge (`arte/tripo/bau`, tampa aberta com ouro, licença tripo_pago da mesma conta) pode substituir o #8 (−45).
- Os clipes Mixamo do Forge (`arte/mixamo/`: Breathing Idle, Walking, Cheering, Looking Around, Angry, Thankful, Waving, com a mesma configuração) poupam downloads.

## 4. Clipes Mixamo por ator

**Status:**
- **estúdio** = o nome já foi baixado e registrado no COE (2026-09-30) ou no Forge (`PROVENIENCIA.md` §2).
- **buscar** = confirmar no catálogo pela palavra-chave (o catálogo não foi consultado nesta raia), com o fallback do estúdio.

**Quadros:** 12 fps com teto de 12 por clipe (preset).

**"Impacto":** o quadro onde o golpe acerta. Mede-se na folha depois do render e a View usa para esticar a antecipação pelo telegraph (`ART_BIBLE.md` §7).

**Quanto ao Hold.fbx:** o clipe de segurar fica fora da pasta de clipes e entra por `--hold`. Os clipes cujo nome começa com `Carry` recebem os braços dele (ombro até os dedos) sobre as pernas e o tronco do clipe base. Assim o courier **corre** segurando a carga a 4,2 m/s. Um clipe de andar com caixa tocado a 3× pareceria patinar.

### Courier: `arte/mixamo/courier/` → preset `courier` (8 direções, 5 linhas)

| Arquivo | Mixamo | Status | Uso (Core) | Fallback |
|---|---|---|---|---|
| `CarryRun.fbx` | Running (cópia) + `--hold` | estúdio (Running, COE) | arrastar com a carga, 4,2 m/s; a View ajusta o fps pela passada medida (teto 2,3×, como no COE) | clipe de andar com caixa do Mixamo (buscar "box", "carry"), se a passada casar |
| `CarryIdle.fbx` | Breathing Idle (cópia) + `--hold` | estúdio | ≤ 0,2 s antes de pousar; porta; entrega | — |
| `Run.fbx` | Running | estúdio | andar sem carga (+15%) | — |
| `Idle.fbx` | Breathing Idle | estúdio | parado sem alvo | — |
| `Throw.fbx` | buscar "throw" | buscar | ataque parado; a View toca o clipe em 0,45 s (`ShotCooldown`) e solta o projétil no quadro de impacto | Punching (estúdio, COE) |
| `Dash.fbx` | buscar "roll" (rolamento para a frente) | buscar | dash: o clipe inteiro em 0,18 s | sem clipe: um quadro do Run com rastro |
| `Hit.fbx` | Hit Reaction | estúdio | dano quando parado; andando é só o flash | — |
| `arte/mixamo/Hold.fbx` (fora da pasta) | clipe parado segurando caixa à frente do peito (buscar "box", "carry", "holding"; o Forge registrou que os clipes de carregar do Mixamo seguram a caixa à frente do peito) | buscar | fonte dos braços (`--hold`, `--hold-frame`) | Clapping (estúdio, Forge), num quadro com as mãos afastadas à frente do peito (escolher pela folha) |

`arte/mixamo/courier_fim/` → preset `courier_fim` (1 direção): `Dying.fbx` = Dying (estúdio, COE) · `Cheering.fbx` = Cheering (estúdio, Forge; na entrega).

### Ladrão: `arte/mixamo/ladrao/` → preset `ladrao` (4 direções, 3 linhas)

| Arquivo | Mixamo | Status | Uso | Fallback |
|---|---|---|---|---|
| `Run.fbx` | Running | estúdio | corre até a carga (2,0 m/s) | — |
| `Lurk.fbx` | Looking Around | estúdio (Forge) | espera na espreita (`ThiefLurk` 3 s) | — |
| `Grab.fbx` | buscar "pick up" | buscar | pega do chão em 0,5 s (`ThiefGrab`) | agachar procedural (escala Y 0,8 por 0,5 s) |
| `CarryRun.fbx` | Running (cópia) + `--hold` (o mesmo `Hold.fbx`) | estúdio | foge com a carga (1,6 m/s); a carga usa a âncora `hands` | — |

### Guarda: `arte/mixamo/guarda/` → preset `guarda` (4 direções, 3 linhas)

| Arquivo | Mixamo | Status | Uso | Fallback |
|---|---|---|---|---|
| `Idle.fbx` | buscar "shield idle" / "sword and shield idle" | buscar | parado na porta | Breathing Idle |
| `Bash.fbx` | buscar "shield bash" / "sword and shield attack" | buscar | golpe de 1,2 m, telegraph 0,5 s; impacto: medir | Punching (estúdio, COE) |

### Cliente: `arte/mixamo/cliente/` → preset `cliente` (1 direção)

| Arquivo | Mixamo | Status | Uso |
|---|---|---|---|
| `Idle.fbx` | Breathing Idle | estúdio | espera no topo |
| `Cheering.fbx` | Cheering | estúdio | 0 rachaduras (×1,5) |
| `Thankful.fbx` | Thankful | estúdio (Forge) | 1 rachadura (×1,2); ovo chocado |
| `Shrug.fbx` | buscar "shrug" (fallback Looking Around, estúdio) | buscar | 2 rachaduras (×1,0) |
| `Waving.fbx` | Waving | estúdio (Forge) | o courier entra na D1 |

### Bruto e Ogro (Lote 2)

| Conjunto | Arquivo | Mixamo | Status | Uso | Fallback |
|---|---|---|---|---|---|
| `bruto` | `Walk.fbx` | buscar "mutant walk" | buscar | persegue a 1,6 m/s | Walking (estúdio, Forge) |
| `bruto` | `Idle.fbx` | Breathing Idle | estúdio | recuperação | — |
| `bruto` | `Smash.fbx` | Standing Melee Attack Downward | estúdio (o Forge registrou como substituto confirmado) | golpe de 2 de dano, telegraph 0,6 s | — |
| `ogro` | `Walk.fbx` | buscar "mutant walk" | buscar | 1,2 m/s | Walking tocado a 0,6× |
| `ogro` | `Idle.fbx` | Breathing Idle | estúdio | — | — |
| `ogro` | `Slam.fbx` | buscar "jump attack" / "ground slam" | buscar | pancada em área de 2 m, telegraph 1 s | Standing Melee Attack Downward |
| `ogro` | `Roar.fbx` | buscar "roar" / "yell" | buscar | chama 1 Ladrão a cada 10 s | Angry (estúdio, Forge) |
| `ogro_fim` | `Dying.fbx` | Dying | estúdio (COE) | morte do chefe | — |

### Inimigos comuns sem clipe de morte nem de dano na v0.1

O dano é um flash branco com recuo, e a morte é tombar 90° e desbotar (`ART_BIBLE.md` §9).
- Um `Dying` desses atores obrigaria a uma célula maior (o corpo deitado sai da célula) ou a um conjunto `_fim` por ator.
- Fica para o Lote 3, com 0 crédito: é só render.

### Morcego e filhote sem rig

Não são humanoides: o Auto Rig humanoide do Tripo e os clipes do Mixamo não servem.
- Um rig próprio com keyframes à mão custaria horas para atores de ~80 px que leem bem com squash por código.
- **Morcego:** bater de asa por escala X de 1,0 ↔ 0,65 a 10 Hz e flutuação de ±0,06 m a 3 Hz.
- **Filhote:** pulos de 0,15 m com squash.
- Ambos usam 1 sprite estático com flipX pelo movimento, 0 crédito a mais.
- Se o playtest pedir mais, renderizar 2–3 poses do mesmo modelo no Blender (girar as asas por script) antes de pensar em rig.

## 5. Conjuntos de sprite (contrato com a View)

Pasta: `client/Assets/_DC/Resources/Sprites/<pasta>/`, com `<Clipe>.png` + `meta.json`. Formato em `RENDER_SPRITES.md`.

| Pasta (preset) | Fonte | Linhas | Célula | Normalização | Clipes |
|---|---|---|---|---|---|
| `courier` | #1 + `arte/mixamo/courier/` + `Hold.fbx` | 5 (8 dir., espelho) | 224 | altura 1,30 m | CarryRun, CarryIdle, Run, Idle, Throw, Dash, Hit |
| `courier_fim` | #1 + `arte/mixamo/courier_fim/` | 1 | 320 | 1,30 m | Dying, Cheering |
| `ladrao` | #2 + `arte/mixamo/ladrao/` + `Hold.fbx` | 3 (4 dir., espelho) | 160 | 1,00 m | Run, Lurk, Grab, CarryRun |
| `guarda` | #3 + `arte/mixamo/guarda/` | 3 | 224 | 1,50 m | Idle, Bash |
| `cliente` | #4 + `arte/mixamo/cliente/` | 1 | 192 | 1,35 m | Idle, Cheering, Thankful, Shrug, Waving |
| `bruto` | #10 + `arte/mixamo/bruto/` | 3 | 256 | 1,80 m | Walk, Idle, Smash |
| `ogro` / `ogro_fim` | #11 + `arte/mixamo/ogro/` e `ogro_fim/` | 3 / 1 | 384 / 448 | 2,80 m | Walk, Idle, Slam, Roar / Dying |
| `morcego`, `filhote` | #5, #14 | 1 | 112 / 96 | largura 0,70 m / altura 0,50 m | Static |
| `caixa`, `reliquia`, `ovo` | #6, #12, #13 | 1 | 112 / 96 / 96 | largura 0,50 m / altura 0,55 m / altura 0,55 m | Static |
| `bau`, `bau_aberto`, `arco` | #7, #8, #9 | 1 | 160 / 160 / 448 | largura 0,80 / 0,80 / 2,20 m | Static |

Medir na folha depois do render e anotar aqui:

| Clipe | Quadro de impacto | Passada natural (m/s) |
|---|---|---|
| courier/Throw | a medir | — |
| guarda/Bash | a medir | — |
| bruto/Smash | a medir | — |
| ogro/Slam | a medir | — |
| courier/CarryRun e Run | — | a medir |
| ladrao/Run e CarryRun | — | a medir |

## 6. Comandos de render (depois do aval, dos downloads e do veredito do modelo)

Rodar da raiz do repositório, **um ator por vez**, com o Unity fechado (PC de 7,7 GB). O `.ps1` corta o Blender em 900 s.

```
.\client\tools\render_sprites.ps1 -Nome courier     -Model arte\tripo\courier\<fbx> -Anims arte\mixamo\courier -Hold arte\mixamo\Hold.fbx
.\client\tools\render_sprites.ps1 -Nome courier_fim -Model arte\tripo\courier\<fbx> -Anims arte\mixamo\courier_fim
.\client\tools\render_sprites.ps1 -Nome ladrao      -Model arte\tripo\ladrao\<fbx>  -Anims arte\mixamo\ladrao  -Hold arte\mixamo\Hold.fbx
.\client\tools\render_sprites.ps1 -Nome guarda      -Model arte\tripo\guarda\<fbx>  -Anims arte\mixamo\guarda
.\client\tools\render_sprites.ps1 -Nome cliente     -Model arte\tripo\cliente\<fbx> -Anims arte\mixamo\cliente
.\client\tools\render_sprites.ps1 -Nome caixa       -Model arte\tripo\caixa\<fbx>         (morcego, bau, bau_aberto, arco: idem)
```

Para o `Hold.fbx` de fallback (Clapping), acrescente `-Extra @("--hold-frame","<n>")`, com o quadro escolhido na folha.

**Antes do elenco:** compare a luz `flat` com a `matcap` (`ART_BIBLE.md` §2). Rode `-Nome courier_matcap -Preset courier -Extra @("--light","matcap") ...` e apague a pasta perdedora antes do commit.
