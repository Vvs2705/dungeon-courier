# VIEW_V1_MUNDO — o mundo da v1.0-beta na View (Dungeon Courier)

**Estado em 2026-10-10 (raia K).** Compilado só pelo `viewcheck` (`dotnet build client/tools/viewcheck/view`, 0/0). **Não rodado no Unity:** as fotos são do coordenador (§5).

**Arquivos:** `client/Assets/_DC/Scripts/View/WorldView.cs` (ganchos, tabelas por enum, inimigos, cargas, portas) e `WorldViewV1.cs` (parcial novo: conteúdo da v1, VFX e pools). Nada no Core, na HUD nem no `Art.cs`.

## 1. Bloqueador resolvido

`EnemySheet`, `AttackClip`, `ImpactFrac`, `EnemyH`, `CargoSheet` e `CargoSize` são lidos por `At(tabela, (int)enum, padrão)`. Valor novo do enum sem linha cai no padrão: sem folha → procedural, nunca `IndexOutOfRange`. O `PaintEnemy` usa o `Enemy.Def` (não `EnemyDef.All[...]`), e papel sem desenho vira um disco na cor do papel.

## 2. O que cada coisa nova mostra

| Conteúdo | No mundo |
|---|---|
| Arqueiro | Folha `arqueiro` (Idle; Walk/Run recuando de frente para o courier; tiro `Shoot`/`Aim`/`Draw`/`Attack` com [0, soltar) esticado nos 0,8 s e o resto em 0,15 s depois do `ArrowShot`). Procedural: manto ciano-petróleo, caveira de osso (os furos são os olhos) e arco virado para a mira. **Linha de mira** no chão até a parede/obstáculo (borda 90 %, branca no último 0,1 s; enchimento 25 % crescendo); sem o círculo de 7 m. Flecha em voo a 0,8 m com sombra, aura vermelha, haste de osso e pena ciano. Carga no chão na linha = contorno vermelho |
| Limo | Folha `limo` (Static) ou gota rosa-chiclete com brilho; squash e estica por código. **Poça** no raio real com borda escura e anel do tempo restante (pisca no último 1 s). Courier na poça: gosma rosa no pé, gotas e tinta rosada |
| Capitão Ladrão | Folha `capitao` (clipes do ladrão + golpe `Attack`/`Slash`/`Punch`/`Bash` + grito `Roar`/`Call`/`Shout`) ou capuz roxo maior com tricórnio dourado. Carga na âncora `hands`. `CaptainCall`: anel roxo, "!!" e sacudida. Golpe com a carga: "1/2" |
| Bolo | Forma de 2 andares com coração (ou `bolo`). Toque rápido negado: balança ±14° e o ícone **"sem dash"** (setas riscadas) sobre o courier; a frase vai na dica da HUD |
| Mensagem | Envelope com lacre dourado (ou `mensagem`). Segundos sobre o courier nos últimos 30 s (vermelho e pulsando nos 10). `TimeWarning`: "10 s". `MessageDoor`: chave dourada subindo das portas. Mensagem fora das mãos: envelope piscando ao lado do cadeado que ela abre. Cadeado cai e quebra ao destrancar (qualquer motivo) |
| Caixa pesada | Quadrado com chapa de ferro (ou `caixa_pesada`). Anel limão enchendo ao pegar (+0,2 s), poeira no pé andando, corpo achatado, carga balançando; baque com poeira ao pousar |
| Barril | Tonel com caveira (ou `barril`). Aceso: alcance real de 2,5 m enchendo como telegraph, faísca no pavio, **3-2-1** grande piscando cada vez mais rápido, carga piscando vermelha. Explosão: chão chamuscado, alcance em vermelho, onda laranja, clarão, fagulhas, fumaça, sacudida e "BUM!" |
| Armas | Estilingue: o de sempre. Bumerangue: V azul girando 3 voltas/s, rastro para trás. Bombinha: arco de 1,3 m, sombra crescendo e o raio de 1,5 m onde cai (vermelho: racha o ovo e detona o barril) |
| Perks | Dash duplo: 2 marcas no pé. Recarga do dash: arco azul no pé. Acolchoada: brilho creme que estoura no 1º golpe absorvido. Ímã: raio tracejado de 2,7 m no baú. Bota de ferro: 2 estrelas girando no atordoado. Vigia: a contagem do ladrão respeita os +4 s. Mochila: o anel de pegar usa `sim.PickupRange` |
| Salas | Parede rachada: pedra escura com trincas, fresta, luz âmbar e entulho; pulsa clara com a carga perto; ao entrar na secreta, entulho pela entrada e "passagem secreta!". Reforço: "reforço!", brilho vermelho no topo e cada inimigo entra com fumaça e pop. **Selo de risco para a carga** sob a placa (borda limão; saco roxo = ladrão, flecha vermelha = arqueiro, trinca + impacto laranja = pancada). Chifres na placa do M1. **Barra do chefe** (Ogro/Capitão) no topo, à esquerda do arco, com nome e rastro do dano |
| Ambiente | Piso `piso` (lajes de 1 m, tinta 0,90/0,83/1 → ~#3A3F51) e parede de fundo `parede` (fiadas de 0,32 m, tinta → ~#353A4A) subindo até 0,3 m abaixo do topo do arco. Até 4 tochas (`tocha` ou suporte com chama) com brilho e poça de luz âmbar tremulando. `pilar` nos blocos quadrados de 1–1,2 m, ordenado pelo pé e esmaecendo a 45 % com o courier ou a carga atrás |
| VFX | Pool de 200 partículas (`Puff`/`Burst`): poeira ao pousar e pegar, faíscas de golpe, explosão, moedas pulando do baú. Flechas, poças e tiros em pool próprio; texto só muda quando o número muda |

Correções de quebra junto: a barra de vida usava `Def.Hp` e passava de 100 % do C4 em diante (`Balance.HpMul`); o anel de contagem do ladrão ignorava o Vigia; o anel de pegar ignorava a Mochila; o contorno vermelho de "carga no golpe" valia para Guarda/Capitão (que não acertam a carga).

## 3. A medir quando as folhas da raia G chegarem (1 número cada)

- Nomes dos clipes do `arqueiro` e do `capitao` (listas `ArcherShoot`, `CaptainHit`, `Shout` no `WorldViewV1.cs`).
- `ImpactFrac`: arqueiro 0,7 e Capitão 0,5 (estimativa).
- `CargoSize`: bolo 0,6, mensagem 0,45, caixa pesada 0,7, barril 0,55 (pelo prompt).
- `EnemyH`: arqueiro 1,3, limo 0,6, Capitão 1,25.
- `PilarW` = 1,2 m (`RENDER_SPRITES.md` §2) e altura do pilar ≈ 2 × centro da célula; chama da tocha a 1,7 × o centro da célula.
- Textura: m por repetição (`FloorMetres` 8, `WallMetres` 2,56) e tintas (`FloorTint`, `WallTint`). Multiplicar não clareia: o tampo das paredes segue procedural.

## 4. Sem folha / sem textura

Tudo cai no procedural: sem `Resources/Textures/*` o piso de lajes e a parede lisa de antes; sem `tocha`, a chama procedural; sem `pilar`, o bloco de sempre; sem o arco, a parede de fundo volta à faixa de 0,5 m e o selo de risco vai à esquerda da placa. Em `-nographics`, nada de textura (como o `SpriteSheet`).

## 5. Fotos (`-testsession -contract K -bot -shot f.png -shotdelay s`)

Tempos medidos com `Sim(1)`/`Bot(1)` (a mesma partida do `-bot`) no Core de 2026-10-10 17:27; conferir `t=` e `sala=` na linha `SHOT` do log.

| K | s | O que conferir |
|---|---|---|
| 1 | 1,0 | piso e parede de fundo texturizados, tochas, pilares (R1), selo de risco "ladrão" nas 2 placas |
| 2 | 4,5 | parede rachada (R3, à direita) |
| 8 | 1,2 | arqueiro mirando: linha no chão até o pilar/parede (R7, 4 pilares) |
| 8 | 1,7 | flecha em voo com sombra |
| 8 | 3,6 | reforço: "reforço!" e inimigos entrando (pop de 0,35 s) |
| 9 | 1,0 | caixa pesada nas mãos andando: poeira no pé e corpo achatado (R8); o anel de pegar só dura 0,2 s (~2,8 s) |
| 10 | 3,4 | mensagem abre a porta (R6): chave subindo |
| 13 | 5,0 | barril aceso: alcance de 2,5 m, faísca e "3" (R8) |
| 14 | 4,3 | poça do limo com o anel do tempo (R12) |
| 14 | 7,3 | courier na poça: gosma rosa |
| 4 | 19,8 | sala secreta (S1): entulho e "passagem secreta!" |
| 12 | 44,5 | barra do Capitão no topo (M1) e chifres |
| 12 | 46,8 | Capitão com a carga chamando ladrões: anel roxo e "!!" |

**Não sai com o bot de teste:** explosão do barril e bolo balançando (o bot nunca larga o barril de propósito nem toca com o bolo), bumerangue/bombinha (destravam com 12/24 estrelas; não há flag de arma no `-testsession`), cronômetro ≤ 30 s (o bot entrega a mensagem antes) e o Ogro (o bot sem guilda perde no C24). Para esses: jogar no PC ou um save com estrelas.
