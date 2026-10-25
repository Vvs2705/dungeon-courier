# Dungeon Courier — Art Bible (v0.1 de 2026-10-09, em uso)

**Estado (2026-10-25):** aplicada. Os Lotes 1–6 do Tripo foram gerados e viraram folhas (`RENDER_SPRITES.md`, `PROVENIENCIA.md`), e a arte v2 (1.3.0-beta: interface com Lilita One + Nunito, elenco HD, ícone do app, retratos pintados) seguiu estes pilares. O status de proposta logo abaixo é o do dia em que foi escrita.

**Status:** PROPOSTO pela raia B de direção de arte (personas 06 art director, 28 concept, 29 character 3D, 32 technical artist, 33 rigging e blender-3d). Nada foi gerado, nenhum crédito foi gasto e nada foi medido no aparelho.

**Decisões do Vinicius em 2026-10-09 (não reabrir):**
- O courier é **humano** e leva a carga nas mãos e nos braços, bem visível.
- A arte segue o pipeline do Forge Street: **Tripo** (créditos só com aval por lote) → **Mixamo** → **Blender headless** (`client/tools/render_sprites.py`) → **sprites 2D** no Unity.

Isso substitui a proposta de mascote do GDD §11 e §27 C5. O GDD ainda cita o mascote; quem atualiza é o dono do GDD.

**Fontes das regras de jogo:** `CONTRATO_V01.md` e `client/Assets/_DC/Scripts/Core/Defs.cs` (`Balance`, `EnemyDef`, `RoomDef`).
**Outros documentos:** lista de assets e prompts em `ASSETS.md`, render em `RENDER_SPRITES.md`, origem e licença em `PROVENIENCIA.md`.

## 1. Pilares visuais

1. **A carga é a estrela.** Num quadro parado se vê onde está a carga e quem está perto dela. Ela é o objeto mais claro e quente da sala, com contorno, e pisca quando fica no chão longe do courier.
2. **Cada papel tem uma cor e uma forma.**
   - Quem vem pela carga: roxo, saco vazio nas costas, corpo inclinado para a frente.
   - Quem bloqueia a porta: vermelho, escudo do tamanho de uma parede.
   - Quem bate pesado: laranja-ferrugem, ombros enormes.
   A cor de um papel não aparece em mais nada.
3. **Masmorra legível, não escura.** O clima vem da cor: pedra azul-violeta de valor médio e poças de luz quente das tochas. Nada some no preto (lição Arkana 2026-08-18: clima se faz com cor, nunca tirando luz).
4. **Stylized fantasy/comic:** volumes gordos, 3,5 cabeças, cor chapada com 2 tons e contorno de tinta assado no sprite. Nada de PBR espelhado nem de micro-detalhe, que some em ASTC numa tela de 6,5".
5. **O jogo é o anúncio.** Os criativos saem gravados do jogo real (guardrail do estúdio). A leitura tem de funcionar na câmera do jogo, sem render bonito à parte. O criativo 13 do GDD (mascote × humano) perdeu o objeto com a decisão de 2026-10-09.

## 2. Câmera, projeção e escala

**Ângulo.** Os sprites são renderizados no Blender com câmera ortográfica a **60° sobre o chão** (padrão do `render_sprites.py` e do Forge).
- A 60° aparecem o topo da carga nas mãos e o círculo de telegraph no chão.
- A 45°, cada corpo tapa o que está atrás dele.
- Custo conhecido: a vertical encolhe para 50% (cos 60°). Compensamos com 3,5 cabeças e altura por ator (§4), nunca esticando o sprite.

**Luz gravada no sprite (fixa):** Workbench `flat`, mais cavidade, mais o contorno do Workbench, mais **contorno de tinta de 3 px** (`--outline 3`).
- Na folha do courier, comparar com `matcap` (toon de 2 tons), a 0 crédito, antes de renderizar o elenco. Decide o olho do Vinicius.

**Escala.**
- **Textura:** 120 px/m (`ppu` 120), igual para todo o elenco.
- **Normalização:** cada modelo do Tripo é ajustado no Blender para a altura de projeto (`--height` / `--width` nos presets). A altura que o Tripo entrega varia de modelo para modelo (memória do estúdio), por isso nunca se corrige escala no Unity.
- **Na View:** 1 unidade = 1 m e não há fator de escala por personagem.

**Tela de referência:** 1080×1920 (16:9) e 1080×2400 (20:9).
- Largura visível de **9,6 m** (sala de 9 m + 0,3 m de parede de cada lado) = **112,5 px/m**.
- O sprite de 120 px/m aparece a 0,94×, uma redução leve que não borra. Se a sala inteira ocupar exatamente os 1080 px (120 px/m), o sprite sai 1:1.

**Projeção recomendada à View** (a decisão é dela; os sprites não mudam):
- Chão projetado a 60°, como nos sprites: `tela_y = 0,866·y + 0,5·z`. Aqui z é a altura sobre o chão: morcego 1,0 m, projétil 0,9 m.
- Com isso, a sala de 14 m ocupa 12,1 m. Com as paredes, são ~13,7 m = ~1 540 px, e sobram ~380 px para a HUD em 16:9.
- Com o chão em planta (convenção do Forge), a sala ocuparia ~15,6 m = ~1 755 px e sobrariam ~165 px em 16:9: pouco para HUD e entalhe.
- Nessa projeção, todo círculo no chão vira elipse de 1 : 0,866 (telegraph, sombra, raio de pegar). Isso casa com o pé dos sprites.

**Ordem de desenho:**
- Pelo pé (y da Sim): maior y desenha antes.
- O morcego ordena pela sombra no chão.
- A carga nas mãos fica logo acima ou logo abaixo do portador (§6).

## 3. Paleta (hex)

### Ambiente

| Uso | Base | Sombra | Luz |
|---|---|---|---|
| Piso (lajes) | `#3E4458` | `#2B2F3E` (rejunte `#24273A`) | `#56607A` |
| Parede | tampo `#5E6680` | face `#474D63` | rodapé `#2C3042` |
| Obstáculo (bloco de pedra, 9-fatias) | tampo `#6B7390` | face `#4E5570` | aresta `#8A93B0` |
| Tocha | chama `#FFB347` | — | poça de luz `#FFB45A` aditiva a 18–25% |
| Vinheta | `#0E0F18` até 15%, só na borda da tela | — | — |
| Sombra de ator | `#0E0F18` a 35% | — | — |
| **Tinta** (contorno, texto escuro) | `#1B1724` | — | — |

O cenário usa só azul-violeta-cinza e luz âmbar. As cores de papel (abaixo) nunca entram no cenário.

### Papéis (cor exclusiva)

| Papel | Dominante | Apoio | Regra |
|---|---|---|---|
| Courier (jogador) | azul `#2F6FE4` | creme `#F2E6CC`, couro `#7A4A2A`, distintivo `#E0B23A` | azul só no courier, no projétil (`#7FC4FF`) e no anel do dash |
| **Carga** (todas) | amarelo-limão `#FFE14D` | — | contorno, alerta, ícone e seta da carga. Nunca em moeda nem em cenário |
| Caixa | madeira `#C98A4B` / sombra `#8A5A32` / luz `#E3AE72` | cinta `#6E7487`, selo de cera da guilda `#2F6FE4` | — |
| Relíquia frágil | porcelana `#F1EEE4` / `#C9C3B2` | ouro `#E0B23A`, gema `#5CE1E6` | parece que quebra: clara, fina, dourada |
| Ovo | casca `#F3E3C3` / `#D6BE93` | pintas `#E8735A`, ninho de palha `#B08D57` | — |
| Filhote | coral `#E8735A` | barriga `#F7E2C4` | continua as pintas do ovo |
| **Ladrão** | roxo `#8E44AD` (capuz, capa) | pele `#8DB255`, saco de estopa `#B08D57` | roxo só para ladrão e roubo (linha de intenção, anel de pegar) |
| **Guarda** | vermelho `#B3262E` (tabardo, faixa do escudo, cadeado) | aço `#8C96A6` / `#4F5866` / `#D5DCE6` | vermelho-escuro = porta bloqueada |
| Morcego | vinho `#5B2A3A` | membrana `#B5546A`, olhos `#FFFFFF` | quente sobre o piso frio (contraste por matiz e valor) |
| **Bruto** | laranja-ferrugem `#C8642B` | pele cinza-pedra `#8E8A84`, manoplas `#4F5866` | — |
| Ogro (chefe) | musgo `#6F8A4B` | couro `#5C3A1E`, ossos `#EDE6D6` | — |
| Cliente | verde `#3FAE5A` | ouro `#E0B23A` | verde = entrega (zona de entrega também) |

### Feedback e UI

| Uso | Cor |
|---|---|
| Perigo (telegraph, vinheta de dano) | `#FF3B30` |
| Positivo, entrega, cura | `#45C46B` |
| Moeda | `#F5A623`, aro `#B7791F`, brilho `#FFE7A3` (ouro-laranja, nunca o limão da carga) |
| Texto claro / escuro | `#F7F3EA` / `#1B1724` |
| Papel dos cartões | `#F3E2C0`, chanfro `#C9A877` |

**Daltonismo:** a cor nunca é o único sinal. Roxo, vermelho e laranja se separam também pela forma: saco e capuz, escudo-parede, ombros. Teste: cada ator em preto no tamanho de tela (§4).

## 4. Proporções e silhuetas

**Humanoides:** 3,5 cabeças (cabeça ≈ 0,37 m no courier de 1,30 m); o goblin tem 3.
- Mãos e botas 1,4× o natural.
- T-pose com braços ≤ 10° da horizontal e **mãos abertas e vazias**: a carga é um sprite separado (§6).
- A arma vai nas costas ou na cintura e o escudo fica preso no antebraço. Arma segurada no T-pose gruda no quadril no skin automático (no Forge, todas as armas ficaram nas costas ou na cintura).
- Roupa justa nas pernas, capa curta e rígida. Túnica longa atrapalha o rig (mago do Forge, Mara do COE).

**Medida comum das cargas:** todas cabem numa caixa de 0,50 × 0,45 × 0,50 m. Assim um só clipe de segurar (`--hold`) serve às três.

| Ator (preset) | Altura de projeto | Raio no Core | Assinatura em preto | Cor | Na tela, 1080 px (altura × largura) |
|---|---|---|---|---|---|
| Courier (`courier`) | 1,30 m | 0,40 | boné de aba larga, bolsa a tiracolo volumosa, lenço curto | azul | ~107 × 70 px (+ caixa de ~56 px) |
| Ladrão, goblin (`ladrao`) | 1,00 m | 0,35 | corcunda inclinado para a frente, capuz pontudo, orelhas longas, saco vazio maior que a cabeça | roxo | ~90 × 62 px |
| Guarda (`guarda`) | 1,50 m | 0,45 | escudo-torre retangular do joelho ao queixo, elmo fechado com crista | vermelho + aço | ~128 × 100 px |
| Morcego (`morcego`) | envergadura 0,70 m | 0,30 | asas em M bem abertas, orelhas grandes; voa a 1,0 m com sombra no chão | vinho/rosa | ~79 × 45 px, 56 px acima da sombra |
| Bruto (`bruto`) | 1,80 m | 0,55 | ombros de 2 cabeças, braços até o joelho, manoplas de pedra, cabeça pequena | laranja-ferrugem | ~160 × 124 px |
| Ogro, chefe (`ogro`) | 2,80 m | 0,90 | barriga e punhos gigantes, colar de ossos, presas, cabeça pequena | musgo | ~255 × 203 px |
| Cliente (`cliente`) | 1,35 m | — | chapéu verde de aba larga com pena, barriga redonda, óculos redondos | verde | ~110 × 75 px |
| Filhote (`filhote`) | 0,50 m | 0,30 | cabeça grande, asinhas, cauda | coral | ~45 px |
| Caixa / Relíquia / Ovo | 0,50 / 0,40 / 0,45 m de largura | 0,30 | caixote com cordas / urna fina com alças / ovo num ninho | §3 | ~56 / 45 / 50 px de largura |
| Baú (`bau`) | 0,80 m | — | tampa redonda, cadeado de latão | madeira + ferro | ~90 × 80 px |
| Arco da porta (`arco`) | 2,20 m | — | arco redondo de blocos grossos, vão de 1,6 m | pedra do ambiente | ~248 × 195 px |

A coluna "na tela" é estimativa por geometria: 0,5·altura + 0,87·profundidade, a 112,5 px/m. O número real sai da folha (altura do quadro 0 do Idle, direção S) e se confere na foto do aparelho.

**Referência:** o ferreiro do Forge tem ~50 px numa largura de 11,4 m, e isso foi a causa de fundo do "cru" (`BENCHMARK_VISUAL.md` P2). Aqui o courier tem ~107 px, perto do herói de um Archero.

**Teste de silhueta:**
- No tamanho real de tela, em preto, courier, ladrão, guarda, bruto e morcego são identificados 5 de 5.
- Courier com carga e sem carga também se distinguem.

## 5. Direções de sprite

| Ator | Direções lógicas | Renderizadas | Por quê |
|---|---|---|---|
| Courier | **8** | **5** (S, SW, W, NW, N). NE, E e SE são espelho (flipX) | O dedo arrasta em qualquer ângulo, e o courier está sempre na tela: correr na diagonal com o sprite de lado parece deslizar |
| Ladrão, guarda, bruto, ogro | **4** | **3** (S, W, N). E é espelho | São menores, aparecem por segundos e o golpe está no telegraph do chão. 4 direções é o padrão do gênero |
| Cliente | 1 | S | fica no topo, virado para quem chega |
| Morcego, filhote, cargas, baú, arco | 1 | S | morcego e filhote usam flipX pelo movimento; o resto é estático |

**Espelho:** o desenho é quase simétrico, e os detalhes assimétricos (alça da bolsa) trocam de lado.
- No espelho, o arremesso vira canhoto. É aceito, como na maioria dos jogos 2D.
- O espelho corta 37,5% da memória do courier (5 de 8 linhas) e 25% dos inimigos (3 de 4).

**Memória** (HIPÓTESE até medir). A conta usa ASTC 6×6 ≈ 0,44 B/px, quadros por clipe com teto de 12 e as células dos presets:

| Conjunto | Linhas × célula | Quadros por linha | Memória |
|---|---|---|---|
| `courier` | 5 × 224 px | ~64 (7 clipes) | **~7,1 MB** (8 linhas sem espelho: 11,4 MB) |
| `courier_fim` | 1 × 320 px | 24 | ~1,1 MB |
| `ladrao` | 3 × 160 px | ~38 | ~1,3 MB |
| `guarda` | 3 × 224 px | ~24 | ~1,6 MB |
| `bruto` | 3 × 256 px | ~34 | ~3,0 MB |
| `ogro` + `ogro_fim` | 3 × 384 px + 1 × 448 px | 40 + 9 | ~8,7 MB (só carrega no B1) |
| `cliente` | 1 × 192 px | 60 | ~1,0 MB |
| props (8) | 1 linha cada | 1 | ~0,2 MB |
| **Total da v0.1** | | | **~24 MB** |

- Pior sala: B1 com courier + ladrões + ogro ≈ 18 MB.
- Com 8 direções sem espelho para todos, o total passaria de ~50 MB.
- **ASTC é obrigatório.** Sem ele, o Unity descomprime para RGBA32 (9×, ~215 MB). O teto proposto é ≤ 32 MB de textura de sprite residente, a medir no aparelho.
- **Cada folha ≤ 4096 px** (teto seguro no celular). Os presets já respeitam, e o script avisa se passar.

## 6. A carga

**Nas mãos:**
- A carga é um sprite separado, posto no ponto `hands` do quadro atual (`meta.json`, `RENDER_SPRITES.md`).
- O pivô fica no centro do sprite: o render centra o objeto na célula.
- `frente = 1`: a carga desenha logo acima do portador. `frente = 0` (costas para a câmera: NW, N, NE): logo abaixo.
- A carga é mais larga que o tronco, então aparece dos dois lados mesmo atrás do corpo.

**Mais de uma carga e outros portadores:**
- No resgate, com duas cargas, a segunda vai empilhada sobre a primeira, 0,8 × a altura projetada dela acima.
- O ladrão carrega a carga pela mesma âncora, no clipe `CarryRun` dele.

**No chão:**
- Pivô na base (`pivot` do meta) e sombra elíptica.
- **Pousar:** a carga desce em arco de 0,12 s até o ponto (0,6 m ao lado, `PlantOffset`), solta 4 grãos de poeira, e um anel amarelo-limão se expande até 1,5 m (`PickupRange`) em 0,35 s e some. Isso ensina o raio de pegar.

**Longe** (> 3 m, `AloneDist`):
- Contorno amarelo-limão piscando a 2 Hz: a silhueta do próprio sprite, escala 1,12, atrás dele, com um material que desenha alfa × cor (ex.: `GUI/Text Shader`). Esse shader precisa entrar no build por um material-asset: shader usado só por código some do APK (lição Arkana).
- Nos últimos 3 s antes de `AloneTime` (8 s), um anel de contagem em volta da carga vai do amarelo ao roxo: avisa que um ladrão vai nascer.
- Fora da tela, seta amarela na borda.

**Carga dentro de uma área de telegraph:** o contorno fica vermelho de perigo pulsando. É a leitura do "golpe pelas costas acerta a carga" do Bruto.

**Rachaduras (0–3):** overlay procedural sobre o sprite.
- Traço em zigue-zague `#1B1724` de 3 px com borda `#F7F3EA` de 1 px, em 3 posições fixas por carga, nesta ordem.
- Cada rachadura nova entra com pop (escala 1,3 → 1 em 0,12 s), 6 lascas na cor da carga e flash branco de 0,06 s.
- **Relíquia:** a rachadura tem brilho dourado `#FFD166` por dentro. Com 2, pulsa em branco e vermelho (a próxima quebra). Com 3, estilhaça em partículas de porcelana e ouro, e o sprite some.
- **Ovo:** as rachaduras são o ovo chocando. Nos últimos 5 s antes do tempo-alvo, ele balança ±8° a 4 Hz.

**Ladrão:**
- Enquanto corre para a carga, uma linha tracejada roxa vai dele até ela: a intenção fica legível antes do roubo.
- Ao pegar: anel roxo enchendo em 0,5 s (`ThiefGrab`) e um "!" roxo sobre ele.
- Fugindo, a carga vai nas mãos dele, e o ícone de carga da HUD fica roxo.

## 7. Telegraph de golpe

- **Área:** círculo de raio `Reach` em volta do inimigo (`Run.Strike`). Acerta o courier se o corpo encosta e a carga no chão se o centro dela está dentro. Guarda e Bruto: 1,2 m. Ogro: 2,0 m.
- **Chão:**
  - Enchimento `#FF3B30` a 25%, crescendo do centro para a borda com `TeleProgress` (0 → 1).
  - Borda de 3 px a 90% desde o início, piscando em branco no último 0,1 s.
  - No `Strike`: flash branco de 0,06 s, decal de chão rachado por 0,5 s e tremor de 6 px (Ogro: 10 px).
- **Corpo do inimigo:**
  - Os quadros de antecipação se esticam por todo o telegraph: a View toca os quadros [0, impacto) em `Tele` segundos e [impacto, fim) em 0,15 s.
  - Nos últimos 0,15 s, tinta vermelha a 30% piscando.
  - O quadro de impacto de cada clipe se mede na folha depois do render (`ASSETS.md` §4).
- **Morcego:** sem telegraph (toque, recarga de 1 s). Pisca quando encosta.

## 8. Portas e ícones

**Porta.** Fica na parede de cima, com vão de 1,6 m (`DoorHalf` 0,8), em x = `Balance.DoorX`.
- Arco de pedra: sprite `arco`, do Tripo.
- Grade levadiça procedural: barras de ferro em 2 tons (`#4F5866` / `#8C96A6`) com contorno. Sobe em 0,4 s atrás do arco, com `SpriteMask` e poeira.
- A entrada de baixo é só um vão na parede, procedural.

**Placa sobre cada porta.** Pedra `#2E3242` de 1,2 × 0,45 m, borda `#1B1724`.
- **Ameaça 0–3:** 3 encaixes de caveira. Ameaça N mostra N caveiras cor de osso `#EDE6D6`, com contorno; os outros encaixes ficam vazios, só no contorno escuro. Ameaça 0 = três encaixes vazios. Lê-se "N de 3" sem número.
- **Recompensa:** ícone do baú (o próprio sprite `bau` em 0,45 m) ao lado da placa, com as moedas em número (Lilita One 32 px). Sem baú, nada.

**Estados da porta:**
- **Trancada pelo guarda:** cadeado vermelho `#B3262E` sobre a grade. Quando o guarda morre, o cadeado cai e quebra (0,3 s), e a grade sobe.
- **Exige carga** (courier chegou sem ela): a grade treme 0,2 s e aparece um balão com o ícone da carga em amarelo-limão, sem texto. A dica do Core vai no rodapé.

**Ícones.** Caveira, cadeado, coração, moeda, rachadura e relógio do ovo são formas procedurais ou reduções dos sprites renderizados. Nenhum emoji.

## 9. Feedback (evento do Core → tela)

| Evento | Visual | Tempo | Som / haptic (GDD §12) |
|---|---|---|---|
| `DamageTaken` | courier com flash branco e piscando por 0,6 s; vinheta vermelha a 25%; o coração da HUD estoura; tremor de 4 px | 0,08 / 0,6 s | impact; haptic leve |
| `CargoIntegrity` (rachou) | §6 e tremor de 6 px | 0,12 s | package damage; haptic forte |
| `Plant` (pousar) | arco, poeira e anel amarelo (§6) | 0,12 / 0,35 s | som de pousar (distinto) |
| `CargoPickup` | arco até as mãos, pop de 1,15 e brilho | 0,12 s | som de pegar (distinto) |
| `ThiefSpawn` | poeira roxa e "!" roxo | 0,3 s | — |
| `ThiefGrab` | anel roxo enchendo e linha roxa | 0,5 s | alarme curto (GDD §2) |
| `ThiefEscape` | a carga some na borda em fumaça roxa | — | — |
| `Telegraph` / `Strike` | §7 | `Tele` | swoosh / impact |
| `Shot` | projétil procedural: núcleo branco com halo `#7FC4FF`, 0,25 m, rastro de 0,4 m | — | attack |
| `EnemyHit` | flash branco de 0,06 s e recuo de 0,1 m; barra de vida pequena só depois do 1º golpe | — | impact |
| `EnemyDie` | tomba 90° em 0,2 s, desbota e solta poeira | 0,4 s | — |
| `Dash` | 3 fantasmas azuis a 40% e poeira; anel azul de recarga no pé até `DashCooldown` | 0,18 s | dash |
| `DoorBlocked` | §8 | 0,2 s | door |
| `ChestOpen` | anel de progresso de 1 s (`ChestTime`); troca para `bau_aberto` com flash; moedas voam até a HUD | 1 / 0,6 s | reward |
| `Hatch` | o ovo explode em cascas e o filhote surge com pop | 0,3 s | — |
| `PetHit` | flash e 2 corações pequenos sobre o filhote | — | — |
| `CargoBroken` | estilhaça; a tela dessatura 30% por 0,5 s | 0,5 s | — |
| `Deliver` | cliente reage pela integridade (0 rachaduras `Cheering`, 1 `Thankful`, 2 `Shrug`, ovo chocado `Thankful`); confete na cor da carga; moedas até a HUD; courier em `Cheering` | 3–4 s, pulável | delivery; haptic de entrega perfeita |

## 10. UI

**Fontes:** livres, SIL OFL 1.1 (Google Fonts).
- **Lilita One:** títulos, números grandes, preços.
- **Nunito** ExtraBold/Bold: texto corrido.
- As duas cobrem os acentos do pt-BR. Registrar a licença em `PROVENIENCIA.md` antes de embutir.
- Sobre o jogo, texto com contorno escuro de 3–4 px `#1B1724`.
- **Sem emoji em texto renderizado:** todo símbolo é sprite.

**Cartões** (contrato, upgrade da guilda):
- Papel `#F3E2C0`, borda de 4 px `#1B1724`, cantos de 20 px e base 8 px mais escura `#C9A877` (chanfro).
- Título em Lilita One 44 px; corpo em Nunito 30 px.
- Selo de cera da guilda em azul `#2F6FE4` com ouro.
- **Cartão de contrato** ("ordem de entrega"): ícone da carga (o próprio sprite), pagamento com moeda, camadas como pontos e ícone do chefe no C8.
- **Cartão de upgrade:** ícone, nível em 5 pontos e preço.

**HUD** (faixa de cima, ~200 px em 16:9):
- Corações (vida).
- Ícone da carga com as rachaduras e o relógio do ovo.
- Pílula de moedas: fundo `#1B1724` a 85%, moeda de 56 px, número de 48 px.
- Embaixo não há nada fixo: o dedo joga em qualquer lugar.

**Dicas do Core** (`Hint`): faixa no rodapé, fundo `#1B1724` a 80%, Nunito 32 px, 1 linha. **Botões de menu:** alvo de toque ≥ 96 px.

## 11. O que é gerado e o que fica procedural

**Gera crédito** (Tripo, `ASSETS.md`): personagens rigados, morcego, cargas, filhote, baú (fechado e aberto) e arco da porta.

**0 crédito, procedural no Unity:**
- **Chão e paredes:** piso; paredes; obstáculos (os retângulos do Core em 9-fatias, com tampo e face de 0,3 m).
- **Decalques e anéis:** sombras elípticas; círculos de telegraph; anéis (pegar, baú, contagem do ladrão, recarga do dash, roubo); contorno piscando; rachaduras.
- **Porta e combate:** grade levadiça; projétil; rastro do dash.
- **Efeitos:** partículas (poeira, lascas, confete, fumaça); poças de luz das tochas; vinheta.
- **Interface:** ícones (caveira, cadeado, coração, moeda, relógio); barras de vida; setas fora da tela.
- **Animação por código:**
  - **Morcego:** asa por escala X de 1,0 ↔ 0,65 a 10 Hz e flutuação de ±0,06 m a 3 Hz.
  - **Filhote:** pulinhos com squash.

**Textura de piso e parede:**
- Saem do gerador de imagem grátis do Tripo (0 crédito, como no Forge, `PROVENIENCIA.md` §4).
- Depois, mistura com uma cópia deslocada pela metade, para não ter emenda, e redução para 256 px tileável.
- Alternativa: ruído procedural.
- Mesmo a 0 crédito, entra em `PROVENIENCIA.md`.

## 12. O que não fazer (e o que fazer no lugar)

| Não fazer | No lugar |
|---|---|
| Arma segurada na mão no T-pose | Arma nas costas ou na cintura, escudo preso no antebraço, punhos e manoplas |
| Saia, túnica ou capa longa | Roupa justa nas pernas, capa curta e rígida |
| Carga modelada no corpo do courier | Carga sempre como sprite separado: rachadura, troca e roubo sem re-render |
| Escuridão "de clima" | Cor fria com luz quente pontual; a área de jogo nunca perde valor. **Exceção (2026-10-11): o escuro da região 3 (Cripta) e dos eventos Midnight/Neblina é regra de jogo, não clima**: fora da luz o chão cai a ~30% do valor (véu `#0B0D16` a 70%, nunca preto), inimigo aparece como olhos e sombra, e **telegraph no chão e projéteis ficam sempre visíveis** (`CONTEUDO_REGIOES.md` §2) |
| Cor de papel no cenário (roxo, limão, vermelho do guarda, azul do courier) | Cenário só em azul-violeta-cinza e âmbar **na região 1**; as regiões 2–4 têm paleta de cenário própria (minas: terra e madeira; cripta: lajes frias e âmbar; forja: pedra escura e laranja de lava), definida em `CONTEUDO_REGIOES.md`, **sempre sem cor de papel** |
| Corrigir escala no Unity | `--height` no Blender (preset) |
| Texto em textura | Texto só na HUD |
| IP alheio (uniforme ou logo de transportadora real, Hermes/Mercúrio alado, criatura de franquia) | Selo próprio da guilda: envelope lacrado cruzado por uma chave |

## 13. Validação

- **Onde se aprova:** na foto do jogo (`-shot` 540×960) e no celular. Nunca no editor nem no Blender.
- **Modelo do Tripo:** veredito do Vinicius **antes** de rig, Mixamo e render (memória do estúdio).
- **Folha de conferência de cada ator** (`client/Builds/sprites_contact/<nome>.png`), olhada antes de importar:
  - pés na linha do pivô;
  - nenhum braço em T;
  - a carga das mãos cai no `hands` (prova com a caixa por cima);
  - o personagem não aparece de costas na direção S (se aparecer, `--yaw 180`).
- **Silhueta em preto** no tamanho real: 5 de 5. **Ícones:** em 32 px, 5 de 5.
- **Memória de textura e FPS:** medidos no aparelho, por CSV. Build de PC não conta.
- **Proveniência:** toda peça com linha em `PROVENIENCIA.md` antes de entrar em build.
