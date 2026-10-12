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

## 6. Regiões 2–4

Código: `WorldViewRegioes.cs` (tema, terreno, listas da visita, escuro, eventos, som) e `WorldViewElenco.cs` (inimigos, chefes e cargas novas). A View só lê o Core: o estado das raias R2–R4 (`run.R2Of`, `R3Of`, `Forge`, `Warns`, `Webs`, `Rocks`, `Bones`, `Spits`, `VentState`, `LightCut`, `TorchCut`, `Webbed`, `GrabbedBy`) e os eventos novos, ligados **pelo nome** (`Enum.TryParse`): renomear um evento no Core não quebra a View, só some o efeito.

- **Tema por região** (`RoomDef.Region`): piso/parede próprios (`piso_mina`/`parede_mina`, `piso_cripta`/`parede_cripta`, `piso_forja`/`parede_forja`; sem a textura, a da região 1 tingida), decoração do fundo (`tocha`, `lanterna_mina`, `vela`, `braseiro`), props nos obstáculos (`pilar`, `escora`, `lapide`, `sarcofago`, `bigorna`; lápide procedural se faltar) e música por `Audio.MusicaRegiao(região, chefe)`.
- **Terreno:** água (brilho andando, respingo e som `agua` no passo), trilho (dormentes; vermelho e sino balançando com o vagonete em `R2Act.Bell`), lava (crosta, miolo pulsando, onda e bolhas), respiro (`run.VentState`: brilho que cresce no aviso, fogo e faíscas), baú enterrado (X com brilho só com `run.Revealed`, arco de cavar, buraco aberto).
- **Listas da visita, todas acima do véu:** teia (70%, 35% fora da luz), avisos da cripta (`Warn`: faixa e círculo em vermelho, fio lilás, raio e runa em verde), avisos da forja (`ForgeMark`: o enchimento cresce até `Total`, a borda fica branca no fim; chicote em faixa, sopro em cone e em fogo quando `Active`; troca e surgir em lilás), fogo no chão, pedra do Golem (sombra crescendo enquanto cai e depois o bloco), pilha de ossos (com anel verde até remontar) e cuspe de teia.
- **Escuro** (`run.Lit`): véu `#0B0D16` a 70% (nunca preto), com as mesmas luzes de `run.InLight` (courier ou lanterna acesa × Olhos de gato − `LightCut`; tochas − `TorchCut`; lanterna acesa no chão). Fora da luz, o inimigo mostra só os olhos e a sombra; telegraph e projéteis continuam visíveis.
- **Courier preso:** com a teia (`run.Webbed`) aparece uma teia por cima e o corpo treme; agarrado pela múmia (`run.GrabbedBy`), treme e mostra a faixa de atadura.
- **Barras dos chefes novos:** Rainha, Golem e Capataz têm marca em 50%; Viúva em 50% e 25%; Necromante em 50% e 20%; Dragão em 60% e 30%.

### Fotos (`-testsession -contract K -bot -shot f.png -shotdelay s`)

Contratos 25–40 = região 2, 41–56 = região 3, 57–72 = região 4. O `-testsession` libera as estrelas da região só na memória. Os segundos são de contrato, com o mesmo bot (`Sim(1)`, `Bot(Seed)`), medidos no Core em 2026-10-11. Se a raia mexer no conteúdo, os segundos mudam.

| K | s | o que mostra |
|---|---|---|
| 26 | 3,0 | minas: piso/parede da mina, lanterna, água; toupeira saindo (s 2,8) e cavando (4,8) |
| 32 | 2,2 | goblin mineiro: círculo da picareta e golpe (2,0) |
| 32 | 3,4 | vagonete no trilho (3,2) |
| 33 | 2,3 | sapo: língua até o ponto (2,1) |
| 33 | 6,8 | trilho vermelho e sino (R2-2) |
| 38 | 3,0 | besouro-bomba explodindo (2,9) |
| 29 | 1,0 | coração arcano: anel do ímã |
| 27 | 1,0 | cristal de gelo: barra de derretimento |
| 34 | 5,0 | gaiola do grifinho: anel de aviso e calma (5,0); grito na 54 (6,0) |
| 41 | 1,0 | cripta escura: véu, luz do courier e lanterna, vela/lápide |
| 41 | 2,1 | esqueleto atacando (2,0) |
| 45 | 20,5 | ossos espalhados (S3-1) e pilha remontando |
| 47 | 2,3 | aranha: faixa vermelha, cuspe (2,9), courier preso na teia (2,2), teia rasgada (5,4) |
| 50 | 2,4 | múmia agarrando o courier |
| 51 | 2,5 | teias da sala (R3-7) |
| 54 | 3,3 | gárgula mergulhando (3,2) |
| 55 | 2,3 | espelho refletindo; fantasma (3,9) |
| 56 | 6,2 | lanterna apagando (6,1) e reacendendo (8,1) |
| 57 | 3,1 | forja: piso tingido, braseiro; salamandra (3,0) |
| 59 | 2,0 | baú enterrado revelado pelo mapa (R4-9) |
| 59 | 7,1 | aviso de troca em lilás (S4-1) |
| 62 | 5,1 | respiro soltando fogo (5,0) |
| 64 | 6,6 | lava com crosta e onda |
| 68 | 7,4 | kobold: círculo do frasco enchendo (7,3) |
| 70 | 2,1 | planta mordendo (2,0); magma (3,8); fogo no chão (6,2) |
| 71 | 2,6 | diabrete surgindo (lilás); queimadura de lava (4,5) |

**Chefes:** saem com a flag `-room` (seção 7). Não sai com o bot nem com ela: as fases 2 e 3 do Necromante (o bot cai antes, aos ~35 s).

## 7. Ligações finais (raia K3)

- **HUD das cargas novas** (faixa de cima, no lugar do relógio quando não há relógio):
  - cristal de gelo: rótulo "gelo" e barra com marcas em 70 e 40 (`run.IceOf`; acima de 70 clara, entre 70 e 40 gelo, abaixo vermelha);
  - espelho: "N/3 rachaduras" (`run.BreakAt`; vermelho a partir da 1ª);
  - lanterna: "lanterna apagada" piscando (`Cargo.Out`); no mundo, a folha `lanterna_apagada`;
  - gaiola: relógio até o grito (`run.CageLeft`), vermelho e pulsando no último 1 s;
  - balão no chão: relógio de 4 s (`Balance.BalloonRise - Cargo.Fuse`), vermelho e com tique a cada segundo.
  - O relógio do ovo agora só aparece no ovo: no cristal, o `EggTarget` é o `MeltTime`, e a HUD mostrava "0 s" vermelho.
- **Resultado:** "O espelho quebrou", "O balão estourou" e "A carga quebrou" (com o Frágil), com o motivo na rachadura certa (`r.Cracks`). No mundo, o balão "estourou!" e o estilhaço na cor da carga.
- **Faixa da fase do chefe** (`BossPhase`, abaixo da faixa de cima, 2,4 s): "a Rainha emerge duas vezes!", "o Golem vai investir!", "a Viúva sobe ao teto!", "o Necromante dispara raio duplo!" / "apaga a luz!", "o Capataz gira o chicote!", "o Dragão levanta voo!" / "varre o fogo!". A dica da planta virou "toque rápido para se afastar".
- **Elenco pelo Core:**
  - gárgula de pedra por `run.Untouchable(e)`;
  - aranha e Viúva com a pose `_empinada` no aviso;
  - múmia `Grab`/`Hold` e necromante `Cast`/`Summon`;
  - dragão: `Fly` com a sombra em `Forge.Shadow`, `Breath` pelo aviso do sopro (boca aberta com `Active`), `Land` na cabeça baixa com brilho dourado e "dano em dobro!";
  - pilha de ossos no último quadro do `Collapse` do `esqueleto`;
  - planta com `planta_aberta` na mordida.
  - Clipes das folhas que a G ainda vai entregar, pelos nomes do CONTEUDO_REGIOES §12.2–12.3. Sem a folha, o procedural continua.
- **Roupa:** o courier usa a folha `courier_<roupa>` (`Hud.FolhaRoupa`) ou o `courier` com a `Hud.TintaRoupa`, o mesmo par do retrato.
- **Rota segura:** a porta de `run.SafeDoor` brilha dourada no chão.
- **Texturas:** a cripta e a forja usam as próprias (`piso_cripta`/`parede_cripta`, `piso_forja`/`parede_forja`, copiadas pela G); sem elas, a textura da região 1 tingida. No `piso_mina`, a tinta quente e o véu de terra a 38% tiram o turquesa das poças pintadas, que se confundiam com a água de jogo.

### Fotos dos chefes (`-testsession -room X -bot -shot f.png -shotdelay s`)

A flag `-room` é da raia E e ainda não estava no `Game.cs` quando esta tabela foi feita. Os segundos são de sala (`Run.Time` num contrato montado só com a sala do chefe e a entrega, `Sim.StartDef`, `Sim(1)`, `Bot(Seed)`), medidos no Core em 2026-10-11. Se a flag montar a sala de outro jeito (nível, perks, ponto de partida), meça de novo.

| sala | s | o que mostra |
|---|---|---|
| M2 | 3,1 | Rainha por baixo: monte de terra crescendo sob o alvo |
| M2 | 4,1 | Rainha emergindo |
| M2 | 11,6 | Rainha chamando toupeiras (`Call`, anel lilás e "!!") |
| M2 | 13,4 | faixa "a Rainha emerge duas vezes!"; barra no meio (marca de 50%) |
| M2 | 26,4 | Rainha caída (`rainha_toupeira_fim`) |
| B2 | 2,4 | Golem: círculo da pancada e o brilho ciano nas costas |
| B2 | 3,4 | sombras das pedras caindo (blocos aos ~4,2 s) |
| B2 | 8,6 | Golem atordoado |
| B2 | 11,7 | faixa "o Golem vai investir!" |
| B2 | 21,3 | Golem caído (`golem_fim`) |
| M3 | 0,9 | courier preso na teia |
| M3 | 2,1 | Viúva empinada: leque (faixa vermelha) |
| M3 | 3,4 | fio lilás puxando a carga |
| M3 | 14,5 | faixa "a Viúva sobe ao teto!" e aranhas chamadas |
| M3 | 15,4 | Viúva no teto: só a sombra |
| M3 | 18,4 | círculo do despencar |
| B3 | 0,5 | Necromante mirando (`Cast`, faixa do raio) |
| B3 | 1,7 | raio disparando (verde) |
| B3 | 1,9 | fumaça do salto (alfa) |
| B3 | 5,1 | runas e esqueletos erguidos (`Summon`) |
| M4 | 1,4 | Capataz: faixa do chicote e círculo do frasco |
| M4 | 2,2 | fogo no chão |
| M4 | 11,4 | faixa "o Capataz gira o chicote!" |
| M4 | 14,1 | apito chamando kobolds |
| M4 | 15,5 | giro (círculo de 3 m) |
| M4 | 16,4 | Capataz caído (`capataz_fim`) |
| B4 | 1,0 | cone do sopro enchendo |
| B4 | 2,2 | sopro saindo e fogo no chão |
| B4 | 16,1 | faixa "o Dragão levanta voo!" e ovos chocando |
| B4 | 17,0 | fissuras de fogo ligadas |
| B4 | 24,5 | Dragão no ar e a sombra grande seguindo o courier |
| B4 | 27,5 | círculo do pouso |
| B4 | 28,6 | pouso: cabeça baixa, brilho dourado e "dano em dobro!" |
| B4 | 31,2 | faixa "o Dragão varre o fogo!" |
| B4 | 54,4 | Dragão caído (`dragao_fim`) |

As folhas `mumia`, `gargula`, `necromante`, `capataz`, `dragao` e as da região 4 ainda não estão no projeto, então as fotos desses atores saem com o procedural.
