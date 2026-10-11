# Dungeon Courier — Assets da v0.1 (PROPOSTO, 2026-10-09)

**Status:** PROPOSTO pela raia B. Nada foi gerado e nenhum crédito foi gasto.

**Aval:** cada lote só começa com aval explícito do Vinicius ao total daquele lote (guardrail do estúdio; decisão de 2026-10-09). A raia não gera nada: planeja, e o coordenador pede o aval.

**Estilo:** `ART_BIBLE.md`. **Render:** `RENDER_SPRITES.md`. **Livro-razão:** `PROVENIENCIA.md`.

## REGRA do Vinicius (2026-10-09): itens separados do personagem

Personagem com item a mais (espada, escudo, maça, cajado, ferramenta) é gerado **sem o item** (prompt com "empty hands, no weapon, no shield"). O item sai como **prop separado** no Tripo e é **preso ao osso no Blender** (mão ou antebraço do rig Mixamo) antes de virar sprite no Unity. Motivo, nas palavras dele: "fazer tudo junto não está dando certo" (o Tripo funde item e mão e o rig arrasta o item errado). Roupa e bolsa vestidas (bolsa a tiracolo do courier, saco nas costas do ladrão) contam como corpo.

**Animação (Vinicius, 2026-10-09):** "movimentação pode ser utilizado o Mixamo ou criar diretamente no Blender". Clipe que o Mixamo não tem (segurar a caixa, dash, arremesso, pegar do chão) pode ser animado por keyframe no Blender sobre o rig Mixamo do Tripo, com o mesmo nome de arquivo/clipe que o `render_sprites.py` espera.

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
| 3 | Guarda (`guarda`) | personagem rigado, **sem escudo e sem maça** (regra acima) | dungeon gate guard, heavy steel plate armor, closed great helm with a short dark red crest, dark red tabard, empty hands, no weapon, no shield `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
| 3b | Escudo-torre (`escudo`) | prop preso ao `LeftForeArm` no Blender | tall rectangular steel tower shield with a dark red horizontal band, slightly curved, inner arm straps on the back, front view `[P][IP]` | — | **45** |
| 4 | Cliente (`cliente`) | personagem rigado | plump cheerful merchant collector, round spectacles, curly grey mustache, wide-brim green hat with a long feather, green coat ending above the knees with gold buttons, leather boots `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
| 5 | Morcego (`morcego`) | prop (animado por código, §4) | chubby cartoon dungeon bat, round body, big wing membranes spread wide, large pointy ears, small fangs, big white eyes, burgundy fur and rose-pink wings, flying pose `[P][IP]` | — | **45** |
| 6 | Caixa (`caixa`) | prop (carga) | sturdy wooden delivery crate, chunky planks, iron corner brackets, crossed rope straps with a carry knot on top, round blue wax guild seal on the front, blank paper label `[P][IP]` | — | **45** |
| 7 | Baú fechado (`bau`) | prop | small dungeon treasure chest, closed, rounded wooden lid, iron bands, big brass lock, studs `[P][IP]` | — | **45** |
| 8 | ~~Baú aberto (`bau_aberto`)~~ | **CORTADO em 2026-10-09** para pagar o escudo separado dentro do teto de 550: a abertura usa o `bau` com tampa e moedas procedurais | — | — | **0** |
| 9 | Arco da porta (`arco`) | prop | dungeon doorway, chunky grey-blue stone block archway, rounded arch top with a keystone, empty dark opening, no door `[P][IP]` (a grade levadiça é procedural) | — | **45** |

> **Lote 1 revisto (2026-10-09, regra dos itens separados): 4 personagens × 65 + 5 props × 45 (morcego, caixa, baú, arco, escudo) + 45 do 1º guarda com escudo junto (descartado) = 530 créditos**, dentro do teto aprovado de 550. Original: 485.
>
> **Lote 1 original: 4 personagens × 65 + 5 props × 45 = 485 créditos.**
> Teto pedido no aval: **550** (485 + uma refação de personagem). Sem o baú aberto: 440.

**Clipes Mixamo do Lote 1** (0 crédito, ~15 downloads; CarryRun e CarryIdle são cópias): §4 para courier, ladrão, guarda e cliente, mais o `Hold.fbx`.

**Mão de obra estimada** (HIPÓTESE):
- ~4 h 30 de operador (Tripo + Mixamo + downloads + ledger) e ~10 min de render.
- Referências: Forge, 8 modelos + 7 clipes em ~1 h com 3 abas; COE, 6 NPCs em um dia.

## 2. Lote 2: resto da v0.1

| # | Asset | Tipo | Prompt (assunto; sufixos) | Pose / rig | Cr |
|---|---|---|---|---|---|
| 10 | Bruto (`bruto`) | personagem rigado | hulking brute warrior, very broad shoulders twice the head size, long arms reaching the knees, huge stone-and-iron gauntlets on both fists, small bald head with a heavy brow, rust-orange leather and fur armor, grey stone-colored skin, no weapon in the hands `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
| 11 | Ogro, chefe (`ogro`, `ogro_fim`) | personagem rigado | giant dungeon ogre boss, huge round belly, enormous fists, small head with two lower tusks, mossy green skin, dark brown leather loincloth and belt, necklace of big bones, empty hands, no weapon, no shackle, no chain (2026-10-09: algema e corrente tiradas pela regra dos itens separados; se voltarem, vêm como prop preso ao osso) `[S][T][I]` | T-pose · Auto Rig Mixamo | **65** |
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
| `guarda` | #3 + `arte/mixamo/guarda/` + escudo (`--attach`) | 3 | 224 | 1,50 m | Idle, Bash |
| `cliente` | #4 + `arte/mixamo/cliente/` | 1 | 192 | 1,35 m | Idle, Cheering, Thankful, Shrug, Waving |
| `bruto` | #10 + `arte/mixamo/bruto/` | 3 | **336** (era 256: recortava) | 1,80 m | Walk, Idle, Smash |
| `ogro` / `ogro_fim` | #11 + `arte/mixamo/ogro/` e `ogro_fim/` | 3 / 1 | **448 × 9 quadros / 576 × 7** (eram 384 × 10 / 448 × 9: recortavam) | 2,80 m | Walk, Idle, Slam, Roar / Dying |
| `arqueiro` | #21 + `arte/mixamo/arqueiro/` + arco #22 (`--attach`) | 3 | 224 | 1,35 m | Idle, Walk (recua), Shoot, Hit |
| `capitao` | #23 + `arte/mixamo/capitao/` + `Hold.fbx` | 3 | 192 | 1,25 m | Idle, Run, Grab, CarryRun, Call, Punch |
| `morcego`, `filhote`, `limo` | #5, #14, #24 | 1 | 112 / 96 / 128 | largura 0,70 m / altura 0,50 m / altura 0,60 m | Static |
| `caixa`, `reliquia`, `ovo` | #6, #12, #13 | 1 | 112 / 96 / 96 | largura 0,50 m / altura 0,55 m / altura 0,55 m (o ovo inclui o ninho: ~0,73 m de largura) | Static |
| `bolo`, `mensagem`, `caixa_pesada`, `barril` | #25–#28 | 1 | 112 / 96 / 128 / 96 | largura 0,60 / 0,45 / 0,70 m (= `CargoSize` da View) / altura 0,55 m | Static |
| `bau`, `bau_aberto`, `arco` | #7, #8, #9 | 1 | 160 / 160 / 448 | largura 0,80 / 0,80 / 2,20 m | Static |
| `tocha`, `pilar` | #29, #30 | 1 | 112 / 288 | altura 0,70 m / largura 1,20 m (`PilarW`; ~2,2 m de altura) | Static |

**Medidas nas folhas** (`client/Builds/sprites_contact/<ator>_folhas.png`), quadros contados a partir de 0. Re-medidas em 2026-10-10 (raia G) com os clipes novos. O quadro de impacto do Blender foi posto de propósito em `round(ImpactFrac × quadros)` da View (`WorldView.cs`), então a View não precisa mudar:

| Clipe | Quadro de impacto / soltura | Passada natural (m/s) |
|---|---|---|
| courier/Throw (10 quadros) | **5** = soltura (`ThrowRelease` 0,5 × 10): braço estendido à frente. 0–4: pronto, antecipação, mão alta atrás da cabeça; 6–9: acompanha cruzando o corpo e volta, emendando no 0 (a View toca em laço) | — |
| guarda/Bash (12) | **6** (= `ImpactFrac` 6/12): escudo empurrado à frente com passo longo. 0–5: recolhe o escudo e arma (telegraph 0,5 s); 6–11 em 0,15 s | — |
| bruto/Smash (12) | **7** (= round(0,6 × 12)): manopla no chão à frente. 0–6: punhos ao peito e manopla no alto (telegraph 0,6 s); 7–11: passa do ponto e recolhe | — |
| ogro/Slam (9) | **5** (= round(0,6 × 9)): dois punhos no chão, agachado. 0–4: braços sobem pela frente até o alto (telegraph 1 s); 5–8: afunda e levanta | — |
| arqueiro/Shoot (12) | **8** = soltura (= round(0,7 × 12)): a mão da corda escapa para trás. 0–7: ergue o arco, puxa até a bochecha e mira (telegraph 0,8 s); 8–11: acompanha e baixa | — |
| capitao/Punch (12) | **6** (= 0,5 × 12): braço direito esticado à frente. 0–5: arma o punho no ombro (telegraph 0,5 s) | — |
| courier/CarryRun e Run (Sprint do ARKANA, **8 quadros**, ciclo 0,53 s) | — | **3,56**, no `meta.json` como `stride_mps`. Na Sim: 4,2 → **1,18×**; 4,83 (+15%) → **1,36×** |
| ladrao/Run e CarryRun (Running do ARKANA, 10 quadros, ciclo 0,80 s) | — | **1,02**. Na Sim: 2,0 → **1,96×**; fuga 1,6 → **1,57×** |
| capitao/Run e CarryRun (Running do ARKANA, 10 quadros, 0,80 s) | — | **1,64**. Na Sim: 2,6 → **1,58×** |
| bruto/Walk (Walking Forward do ARKANA, 12 quadros, 1,37 s) | — | **0,93**. Na Sim: 1,6 → **1,72×** |
| ogro/Walk (o mesmo, 9 quadros) | — | **1,40**. Na Sim: 1,2 → **0,86×** |
| arqueiro/Walk (Walking Backwards do ARKANA, 12 quadros, 1,30 s) | — | **0,59** (para trás). Na Sim: 1,4 → 2,38×, acima do teto de 2,3× da View: o pé patina ~3% |

**Idle:** courier (Idle, CarryIdle), cliente, bruto, ogro, arqueiro e capitão usam a janela 199–289 do Breathing Idle: laço de **3,0 s** (antes 9,9 s a 1,2 fps), 12 quadros a 4 fps (ogro: 9 a 3 fps). A costura do laço é de ~1 px na tela.

**Memória em ASTC 6×6** a 0,444 B/px (HIPÓTESE até medir no aparelho). Conjuntos re-renderizados em 2026-10-10.

Lote 1:

| Conjunto | Clipes (quadros) | Fonte | Folha mais larga | Memória ASTC |
|---|---|---|---|---|
| `courier` (5 × 224) | CarryIdle 12, CarryRun 8, Dash 6, Hit 6, Idle 12, Run 8, Throw 10 | Run/CarryRun = reuso ARKANA (Sprint); Idle/CarryIdle = reuso Forge (Breathing Idle, janela 199–289); Throw, Dash, Hit e `Hold` = Blender | 2 688 px | ~6,9 MB |
| `courier_fim` (1 × 320) | Cheering 12, Dying 12 | Cheering = reuso Forge; Dying = reuso ARKANA (Standing React Death Backward) | 3 840 px | ~1,1 MB |
| `ladrao` (3 × 160) | CarryRun 10, Grab 6, Lurk 12, Run 10 | Run/CarryRun = reuso ARKANA (Running); Lurk = reuso Forge (Looking Around); Grab = Blender (0,5 s, termina no `Hold`) | 1 920 px | ~1,3 MB |
| `guarda` (3 × 224) | Bash 12, Idle 12 | Idle e Bash = Blender; escudo do Tripo preso ao `LeftForeArm` (`--attach`) | 2 688 px | ~1,6 MB |
| `cliente` (1 × 192) | Cheering 12, Idle 12, Shrug 12, Thankful 12, Waving 6 | Idle (janela 199–289), Cheering, Thankful, Waving = reuso Forge; **Shrug = Blender** (dar de ombros de frente) | 2 304 px | ~0,9 MB |
| `caixa`, `bau`, `arco`, `morcego` | Static 1 | Tripo | 448 px | ~0,1 MB |

Lote 2:

| Conjunto | Clipes (quadros) | Fonte | Folha mais larga | Memória ASTC |
|---|---|---|---|---|
| `bruto` (3 × 336) | Idle 12, Smash 12, Walk 12 | Walk = reuso ARKANA (Walking Forward); Idle = reuso Forge (janela 199–289); Smash = Blender; rig em pose A (`align_arms`) | 4 032 px | ~5,4 MB |
| `ogro` (3 × 448) | Idle 9, Roar 9, Slam 9, Walk 9 | Walk = reuso ARKANA (Walking Forward); Idle = reuso Forge (janela 199–289); Slam e Roar = Blender; rig em pose A (`align_arms`); sem algema nem corrente | 4 032 px | ~9,6 MB |
| `ogro_fim` (1 × 576) | Dying 7 | Dying = reuso ARKANA (Standing React Death Backward) | 4 032 px | ~1,0 MB |
| `reliquia`, `ovo`, `filhote` | Static 1 | Tripo | 96 px | <0,1 MB |

Lote 3:

| Conjunto | Clipes (quadros) | Fonte | Folha mais larga | Memória ASTC |
|---|---|---|---|---|
| `arqueiro` (3 × 224) | Hit 6, Idle 12, Shoot 12, Walk 12 | Walk = reuso ARKANA (Walking Backwards: recua de frente); Idle = reuso Forge (janela 199–289); Shoot e Hit = Blender; arco do Tripo preso à `LeftHand` (`--attach`) | 2 688 px | ~2,8 MB |
| `capitao` (3 × 192) | Call 12, CarryRun 10, Grab 6, Idle 12, Punch 12, Run 10 | Run/CarryRun = reuso ARKANA (Running) + `Hold`; Idle = reuso Forge (janela 199–289); Grab, Call e Punch = Blender | 2 304 px | ~3,0 MB |
| `limo`, `bolo`, `mensagem`, `caixa_pesada`, `barril`, `tocha`, `pilar` | Static 1 | Tripo | 288 px | ~0,1 MB |

**Totais:** Lote 1 ~11,9 MB, Lote 2 ~16,1 MB, Lote 3 ~5,9 MB → **~33,9 MB** em ASTC. PNG em disco: 27,3 MB.
- O Lote 1 subiu de ~11,3 para ~11,9 MB: a corrida passou a ter 8 quadros e o Throw 10.
- O ogro sozinho tem ~9,6 MB e só aparece no B1.
- Se faltar memória, o primeiro corte é `clips` no preset do ogro (Idle e Walk com 6 quadros, por exemplo), sem mexer no golpe.

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

## 8. Lote 3 da v1.0-beta (2026-10-10) — conteúdo novo do CONTRATO_V1

Aval: mensagem do Vinicius em 2026-10-10 ("sem pausas, sem solicitações ... quero avanço sempre continue, seja na arte"). Teto do lote: **550 créditos**. Cores de papel (ART_BIBLE §3): arqueiro = osso + **ciano-petróleo** (cor livre no elenco); limo = **rosa-chiclete** (cor livre); Capitão Ladrão = **roxo** com dourado (família do ladrão: roxo = roubo). Itens separados do corpo (regra do Vinicius): o arco do arqueiro sai como prop e é preso à mão no Blender.

| # | Asset | Tipo | Assunto do prompt (+ sufixos `[S][T][I]` ou `[P][IP]`) | Cr |
|---|---|---|---|---|
| 21 | `arqueiro` | personagem rigado | skinny skeleton archer, bone-white skull with glowing teal eye sockets, tattered dark teal hooded cloak, leather quiver on the back, bony hands, empty hands, no bow, no weapon | 65 |
| 22 | `arcoflecha` | prop (item da mão esquerda do arqueiro) | wooden short bow with dark teal grip wrappings, string attached, single object | 45 |
| 23 | `capitao` | personagem rigado | goblin thief captain, taller and leaner than a goblin, deep purple long coat with gold trim, purple tricorn hat with a feather, eye patch, big greedy grin, olive green skin, empty hands, no weapon | 65 |
| 24 | `limo` | prop (animado por código: squash e estica) | cute bubblegum-pink slime blob monster, glossy jelly body, two big eyes and a small mouth, a few bubbles inside, rounded dome shape | 45 |
| 25 | `bolo` | prop (carga) | three-tier white wedding cake with pink frosting flowers and a small heart topper, on a round golden tray with two handles | 45 |
| 26 | `mensagem` | prop (carga) | rolled royal parchment scroll letter with a big gold wax seal and a gold ribbon, urgent delivery | 45 |
| 27 | `caixa_pesada` | prop (carga) | massive reinforced cargo crate with thick riveted iron plates over dark wood, very heavy looking, two big side handles | 45 |
| 28 | `barril` | prop (carga) | small wooden gunpowder barrel with iron hoops, painted skull mark and a short fuse on top, black powder dusting | 45 |
| 29 | `tocha` | prop (decoração) | iron wall sconce holding a burning torch with a stylized flame | 45 |
| 30 | `pilar` | prop (decoração) | squat dungeon stone pillar made of chunky grey-blue blocks with cracked edges | 45 |
| — | texturas `piso`, `parede` | imagem grátis (gerador do Tripo) | seamless tileable stylized dungeon floor slabs / brick wall, top-down, grey-blue, hand-painted, flat light | 0 |

> **Lote 3: 2 × 65 + 8 × 45 = 490 créditos** (teto 550).

## 9. Lote 4 da v1.0 (2026-10-10) — região 2 "Minas Alagadas" (CONTRATO_LANCAMENTO §4 e §8)

Aval: mensagem do Vinicius em 2026-10-10 ("sem pausas, sem solicitações ... seja na arte") e o pedido do jogo completo pelo GDD. Teto do lote: **750 créditos**. Cores de papel (ART_BIBLE §3, cores livres no elenco): goblin mineiro = **amarelo-capacete**; toupeira = **marrom**; Rainha Toupeira = **ameixa** com manto roxo e coroa dourada; golem = **pedra + âmbar**; sapo = **oliva**; besouro = **preto + laranja**. Itens separados do corpo (regra do Vinicius): a picareta do goblin mineiro sai como prop e é presa à mão no Blender.

**Texto exato enviado ao gerador** (Nano Banana 2, 1:1, 4 imagens, grátis): os trechos abaixo foram unidos por ", ", nesta ordem.
- Personagem: assunto + `[S]` + `[T]` + `[I]` (sufixos do §0, sem mudança).
- Prop: assunto + "only one object in the image" + `[P]` + `[IP]`.
- Modelo: HD H3.1 - Máx. qualidade, Ultra, 2K, PBR, remover iluminação, triângulos, 8 000 polígonos, 8K off, Gerar em Partes off, Privado ("Gerar 45" conferido em cada peça). Multi-View não usado ("Gerar multivisões" tem custo próprio).

| # | Asset | Tipo | Assunto do prompt (texto exato, antes dos sufixos) | Imagem | Cr |
|---|---|---|---|---|---|
| 31 | `goblin_mineiro` | personagem rigado | small goblin miner, green skin, dented yellow miner helmet with a little lamp, brown work overalls, rolled sleeves, big ears, empty hands, no tool | #2 | 65 |
| 32 | `picareta` | prop (item da mão do goblin mineiro) | one single miner pickaxe with a wooden handle and a dented iron head, single object | #1 | 45 |
| 33 | `toupeira` | personagem rigado | chubby humanoid mole creature standing on two legs, dark brown velvety fur, pink nose, tiny eyes with round goggles on the forehead, big digging claws, empty hands | #2 | 65 |
| 34 | `rainha_toupeira` | personagem rigado (mini-chefe) | large humanoid mole queen, dark plum fur, small golden crown, royal purple mantle with dirt stains, huge digging claws, empty hands | #4 | 65 |
| 35 | `golem` | personagem rigado (chefe, ~2,8 m no jogo) | hulking stone golem, chunky mossy grey-brown rock body, glowing amber crystal core in the chest, small head, huge rock fists, empty hands | #2 (a única em pose T) | 65 |
| 36 | `sapo` | prop (animado por código) | fat swamp toad monster, muddy olive-green warty skin, wide mouth, big yellow eyes, sitting pose | #2 | 45 |
| 37 | `besouro` | prop (animado por código) | round bomb beetle, glossy black shell with an orange glowing fuse-like horn, six short legs | #4 | 45 |
| 38 | `vagonete` | prop (perigo móvel nos trilhos) | wooden mine cart with iron wheels and rim, rusty, empty, side view readable | #4 | 45 |
| 39 | `coracao` | prop (carga) | arcane heart cargo, a glowing magenta crystal heart floating inside a small brass cage with a carry handle | #3 | 45 |
| 40 | `cristal` | prop (carga) | ice crystal cargo, a big clear-blue ice crystal cluster on a small wooden pallet with rope handles | #4 | 45 |
| 41 | `gaiola` | prop (carga) | birdcage cargo with a small fluffy baby griffin inside (eagle head, lion body), brass cage with a top ring handle | #1 | 45 |
| 42 | `lanterna_mina` | prop (decoração) | hanging mine lantern on a short wooden post, warm yellow glass, iron frame | #2 | 45 |
| 43 | `escora` | prop (decoração) | mine support frame, two thick wooden posts and a crossbeam with iron brackets | #1 | 45 |
| — | texturas `piso_mina`, `parede_mina` | imagem grátis (gerador do Tripo) | texto exato em `PROVENIENCIA.md` §4 (wet mine floor, brown dirt + flat stones + teal puddles / mine wall, brown rock + horizontal wooden planks) | #2 / #3 | 0 |

Na picareta, o assunto já trazia "single object" antes do "only one object in the image". O trilho do CONTRATO §8 ficou de fora: é procedural, como os obstáculos.

> **Lote 4: 4 × 65 + 9 × 45 = 665 créditos** (teto 750). Saldo 19 435 → 18 770 (`PROVENIENCIA.md` §1). Sem refação e sem duplicata.
