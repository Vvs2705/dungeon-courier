# Dungeon Courier — Áudio (v1.0-beta, CONTRATO_V1 §12; regiões 2–4 e lançamento, CONTRATO_LANCAMENTO §9.1)

Todo som e toda música são **sintetizados** por `client/tools/audio_gen.py` (numpy/scipy, semente fixa, sem asset de terceiros nem serviço externo). Licença própria. Uma linha por arquivo em `PROVENIENCIA.md` §4.1.

- **Regenerar:** `python client/tools/audio_gen.py` (≈ 16 s). O script confere tudo e falha se algo sair do limite: pico verdadeiro < −1 dBTP, DC < 0,001, sem silêncio no começo do SFX e laço da música sem emenda. Com `--md`, imprime as linhas do PROVENIENCIA. Mudou o script: atualize o §4.1. Determinístico: apagar todos os WAV e rodar de novo dá os mesmos 148 SHA (conferido em 2026-10-11).
- **Saída:** `client/Assets/_DC/Resources/Audio/`
  - `sfx/<nome>_<n>.wav`: 138 arquivos, 89 nomes, 44,1 kHz mono 16 bit, **6,76 MB**.
  - `musica/<faixa>.wav`: 10 faixas, 22,05 kHz mono 16 bit, **21,34 MB**.
  - **Total no repositório: 28,09 MB.** Antes do lançamento eram 10,58 MB; as regiões 2–4 somaram 17,5 MB (13,5 MB de música e 4,2 MB de SFX). No APK fica bem menos, porque o Unity comprime em Vorbis.
- **Peso (o disco já encheu):** a música é 76% do pacote. Cada faixa nova tem o mínimo que o script aceita para laço de sala (45–48 s, 2,0–2,1 MB) e o tema de evento tem 16 s (0,7 MB). Nenhum SFX novo passa de 1,7 s. Se apertar de novo, o próximo corte é a taxa da música (22,05 → 16 kHz), não a duração.
- **Por que WAV e não OGG:** nem a stdlib nem o scipy têm encoder Vorbis, e um encoder externo seria dependência nova. O Unity faz a compressão no import.
- **Código:** `client/Assets/_DC/Scripts/View/Audio.cs`.
  - `Audio.Play(nome, pitch, minGap)` escolhe uma variação sem repetir a anterior e varia o pitch ±4%. Limita a 3 vozes do mesmo som e 12 no total, roubando a mais antiga.
  - `Audio.Music(faixa)` troca a faixa com crossfade de 1,2 s.
  - `Audio.MusicaRegiao(regiao, chefe)` toca a música da sala pela região (1–4; fora disso, a 1): masmorra da região ou, com `chefe`, a do chefe dela. Mini-chefe e chefe dividem a faixa, como na região 1.
  - `Audio.SetVolumes(som, musica)` recebe valores de 0 a 1.
  - Clipe ausente não toca nada.

## 1. Evento → som

O áudio só observa o jogo e nunca decide nada. Cada som tem par visual (efeito, HUD) ou háptico, e nenhuma informação chega só pelo som.

| Momento | Evento (Core/View) | Som | pitch / minGap | Háptica |
|---|---|---|---|---|
| Passo do courier | frame de passada do Run/CarryRun (WorldView) | `passo` (3 var.) | 1 / 0,2 s | — |
| Pousar a carga | `Ev.Plant` | `pousar` (2) | | — |
| Pegar a carga | `Ev.CargoPickup` | `pegar` (2) | | — |
| Carga rachou (relíquia, ovo, bolo) | `Ev.CargoIntegrity` | `racha` (2) | | sim (`Ajustes.Vibrar`) |
| Carga atingida sem rachar (caixa, Carga acolchoada) | novo evento da raia do Core | `carga` (3) | | — |
| Relíquia quebrou | `Ev.CargoBroken` | `quebra` | | sim |
| Ladrão pegou / apareceu / fugiu | `Ev.ThiefGrab` / `ThiefSpawn` / `ThiefEscape` | `ladrao` (2) | 1 / 1,3 / 0,7 | — |
| Dash | `Ev.Dash` | `dash` (2) | | — |
| Dash proibido (bolo) | toque rápido com o bolo | `balanca` | | — |
| Tiro do estilingue | `Ev.Shot` (arma 0) | `tiro` (3) | 1 / 0,08 s | — |
| Bumerangue / bombinha lançados | `Ev.Shot` (arma 1 / 2) | `bumerangue` / `bomba` (2) | | — |
| Explosão (bombinha, barril) | evento de explosão | `explosao` (2) | | sim |
| Pavio do barril / mensagem com < 10 s | 1× por segundo | `tique` (2: tique, toque) | | — |
| Acertou inimigo | `Ev.EnemyHit` | `golpe` (3) | | — |
| Inimigo morreu | `Ev.EnemyDie` | `morte` (3) | | — |
| Courier apanhou / filhote apanhou | `Ev.DamageTaken` / `Ev.PetHit` | `dano` (2) | 1 / 1,4 | — |
| Aviso de ataque (telegraph) | `Ev.Telegraph` | `aviso` (Ogro: pitch 0,7) | | — |
| Arqueiro mirando (0,8 s) / disparo | telegraph do arqueiro / projétil | `mira` / `flecha` (2) | | — |
| Limo (toque, morte, poça) | eventos do limo | `limo` (3) | | — |
| Golpe de inimigo comum | `Ev.Strike` | `golpe` | 0,6 | — |
| Pancada do Ogro | `Ev.Strike` com `EnemyType.Ogre` | `pancada` (2) | | sim |
| Chefe/mini-chefe entra ou chama ladrões | entrada na sala B1/M1, invocação | `rugido` | Capitão: 1,3 | — |
| Atravessou a porta | `Ev.RouteChoice` | `porta` (2) | | — |
| Porta trancada | `Ev.DoorBlocked` | `bloqueio` (2) | 1 / 0,3 s | — |
| Sala secreta revelada | atravessar a parede rachada | `segredo` | | — |
| Baú | `Ev.ChestOpen` | `bau` | | — |
| Moedas | `Ev.CurrencySource` (exceto baú) | `moedas` (3) | 1,2 / 0,2 s | — |
| Ovo chocou | `Ev.Hatch` | `choca` | | — |
| Entrega perfeita | `Ev.Deliver` sem rachadura | `entrega` | | sim (no `ContractEnd`) |
| Entrega com rachadura | `Ev.Deliver` com rachadura | `entrega_rachada` | | — |
| Contrato falhou | `Ev.ContractEnd` sem entrega | `falha` | | — |
| Estrelas no resultado | 1 por estrela, 0,25 s entre elas | `estrela` | 1 / 1,12 / 1,26 | — |
| Perk escolhido | escolha 1 de 3 | `perk` | | — |
| Compra na guilda / arma | `Ev.GuildUpgrade` | `compra` | | — |
| Botão | `Art.NewButton` | `clique` (2) | | — |
| Confirmar (Jogar, comprar, perk) | botão principal | `confirmar` | | — |

### 1.1 Regiões 2–4 e lançamento (a View das regiões liga)

Os sons já estão gerados. "Novo" na coluna do evento é o nome **sugerido** para a raia da região emitir quando o `Ev` atual não basta (`Telegraph`/`Strike`/`EnemyDie` já trazem o `EnemyType` em B). Onde não diz pitch, é 1.

| Momento | Evento (Core/View) | Som | pitch / minGap | Háptica |
|---|---|---|---|---|
| **Inimigos** | | | | |
| Picareta do goblin mineiro cai no círculo | `Ev.Strike` B=`GoblinMiner` (o aviso de 0,9 s fica no `aviso`) | `picareta` (2) | | — |
| Toupeira andando por baixo (rastro) | View, enquanto a toupeira está `Under` (sem evento) | `toupeira_cava` (2) | 1 / 0,6 s | — |
| Toupeira emerge / Rainha emerge | `Ev.Strike` B=`Mole` / `MoleQueen` | `toupeira_sai` (2) | 1 / Rainha 0,75 | Rainha: sim |
| Língua do sapo | `Ev.Strike` B=`BogFrog` | `lingua` (2) | | — |
| Pavio do besouro (1,0 s) | `Ev.Telegraph` B=`BombBeetle` | `besouro_chia` | | — |
| Besouro explode | `Ev.Explosion` com fonte nova 2 (besouro) | `besouro_explode` (2) | | sim |
| Sino do trilho (1,2 s antes) | `Ev.Telegraph` B=`Minecart` | `sino` | 1 / 0,3 s | — |
| Vagonete passando | `Ev.Strike` B=`Minecart` (início da passagem) | `vagonete` | 1 / 0,3 s | — |
| Fantasma toca / começa a possuir a carga | `Ev.Strike` / `Ev.Telegraph` B=`Ghost` | `fantasma` (2) | 1 / 1,3 | — |
| Esqueleto vira pilha / espalhado de vez | `Ev.EnemyDie` B=`Skeleton` / novo `BonesScatter` | `ossos_cai` (2) | 1 / 0,8 | — |
| Esqueleto se remonta | novo `EnemyRevive` | `ossos_monta` | | — |
| Aranha cospe teia / courier preso / fio da Viúva | `Ev.Strike` B=`Spider` / novo `Webbed` / `Ev.Telegraph` B=`SpiderWidow` | `teia` (2) | 1 / 0,7 / 0,8 | — |
| Múmia agarra | `Ev.Strike` B=`Mummy` | `mumia` | | sim |
| Gárgula acorda / mergulha | `Ev.Telegraph` / `Ev.Strike` B=`Gargoyle` | `gargula_acorda` / `mergulho` | | — |
| Salamandra corta na direção do courier | `Ev.Strike` B=`Salamander` | `salamandra` (2) | | — |
| Diabrete: redemoinho / troca / surge atrás | `Ev.Telegraph` / `Ev.Strike` B=`Imp` | `diabrete` (2) | 1 / 1,2 | — |
| Frasco do kobold cai | `Ev.Strike` B=`KoboldBomber` | `frasco` (2) | | — |
| Elemental se divide em brasas | `Ev.EnemyDie` B=`MagmaElemental` | `magma_divide` | | — |
| Pancada do elemental | `Ev.Strike` B=`MagmaElemental` | `pancada` | 1,15 | — |
| Ovo de salamandra choca | `Ev.EnemyDie`/novo `EggHatch` do `SalamanderEgg` | `choca` | 0,8 | — |
| **Chefes** | | | | |
| Rainha Toupeira entra / chama toupeiras | `Ev.RoomEnter` na M2 / invocação | `rainha` | | — |
| Golem: pancada / batida da investida (atordoado) / pedra caindo | `Ev.Strike` B=`StoneGolem` / `Ev.Stun` / novo `RockFall` | `golem` (2) | 1 / 0,8 / 1,25 e 0,15 s | sim (pancada) |
| Golem entra | `Ev.RoomEnter` na B2 | `rugido` | 0,6 | — |
| Viúva entra / despenca do teto | `Ev.RoomEnter` na M3 / `Ev.Strike` B=`SpiderWidow` (fase 2) | `viuva` | | — |
| Necromante: raio dispara | `Ev.Strike` B=`Necromancer` | `necro_raio` | | — |
| Necromante entra / ergue esqueletos (runa) | `Ev.RoomEnter` na B3 / novo `Raise` | `necro_ergue` | | — |
| Necromante salta | novo `Blink` | `diabrete` | 0,7 | — |
| Capataz: chicote / apito (chama kobolds, e na entrada) | `Ev.Strike` B=`KoboldForeman` / invocação | `chicote` (2) / `apito` | | — |
| Dragão entra / muda de fase | `Ev.RoomEnter` na B4 / novo `BossPhase` | `dragao` | | sim |
| Dragão: sopro / decola / pousa | `Ev.Strike` B=`ElderDragon` / novo `TakeOff` / novo `Land` | `sopro` / `voo` / `pouso` | | sopro e pouso: sim |
| **Cargas** | | | | |
| Coração pousado / bônus do ímã de moedas | `Ev.Plant` com `ArcaneHeart` / `Ev.CurrencySource` do ímã | `coracao` | 1 / 0,5 s | — |
| Gelo derretendo depressa (×3 perto de fogo) | View, 1× por segundo enquanto acelerado | `gelo_derrete` | 1 / 0,9 s | — |
| Gelo começou a recongelar (entrou na água) | View, na transição | `gelo_congela` | | — |
| Grito do grifinho | novo `CageScream` | `grifinho` (2) | | — |
| Lanterna apaga / reacende | novo `LanternOut` / `LanternLit` | `lanterna_apaga` / `lanterna_acende` | | — |
| Espelho reflete | novo `Reflect` | `espelho_reflete` | | — |
| Espelho racha (no lugar de `racha`) | `Ev.CargoIntegrity` com `Mirror` | `espelho_racha` (2) | | sim |
| Balão pousado começa a subir | `Ev.Plant` com `Balloon` (depois de `pousar`) | `balao_sobe` | | — |
| Balão escapou (antes de `falha`) | `Ev.ContractEnd` com `Outcome.Escaped` | `balao_escapa` | | — |
| Balão estourou (no lugar de `quebra`) | `Ev.CargoBroken` com `Balloon` | `balao_estoura` | | sim |
| Planta morde | novo `PlantBite` | `planta` (3) | 1 / 0,15 s | — |
| Mapa revela baú enterrado / parede disfarçada | `Ev.RoomEnter` com o mapa e algo revelado na sala | `mapa` | | — |
| **Terreno** | | | | |
| Passo na água (no lugar de `passo`) / carga caindo na água | frame de passada com o courier na água / `Ev.Plant`/`CargoDrop` na água | `agua` (3) | 1 / 0,2 s; carga 0,8 | — |
| Empurrado na lava (inimigo, courier, carga na borda) | novo `LavaBurn` | `lava` (2) | 1 / 0,15 s | — |
| Respiro em erupção | novo `VentFire` (ou a View lê a fase do respiro) | `respiro` (2) | 1 / 0,3 s | — |
| **Meta e loja** | | | | |
| Evento começou (quadro, contrato de evento) | `Ev.EventStart` | `evento` | | — |
| Objetivo de temporada cumprido | `Ev.SeasonProgress` | `temporada` | | — |
| Recompensa resgatada | `Ev.EventReward` / `Ev.SeasonClaim` / `Ev.Milestone` / `Ev.RewardedGrant` | `recompensa` | | — |
| Projeto de equipamento encontrado | `Ev.BlueprintFound` | `blueprint` | | — |
| Compra na loja (IAP; loja falsa no PC) | `Ev.IapPurchase` | `compra_loja` | | — |
| Equipamento/maestria com moedas | `Ev.GearUpgrade` / `GearUnlock` / `MasteryUp` | `compra` | | — |
| Anúncio de teste começa / termina | simulador de anúncio (View) | `anuncio` | | — |

## 2. Música (laço)

| Faixa | Quando | Estilo | Andamento | Duração |
|---|---|---|---|---|
| `menu` | título, quadro de contratos, guilda, configurações | valsa de caixinha de música e flauta, lá menor, calma | 96 bpm, 3/4 | 60,0 s (32 compassos) |
| `masmorra` | salas normais da região 1 | furtiva com pulso leve: baixo saltitante, chimbal, kick só a partir do 2º bloco, ré menor | 112 bpm, 4/4 | 68,6 s (32 compassos) |
| `chefe` | salas M1 (Capitão Ladrão) e B1 (Ogro) | marcha tensa em mi frígio, baixo em colcheias, metais sincopados, tons | 140 bpm, 4/4 | 54,9 s (32 compassos) |
| `minas` | salas normais da região 2 (Minas Alagadas) | cantiga de mina em lá dórico: marimba (A), flauta com marimba em arpejo (B), ponte de arpejo e gotas (T); baixo dedilhado (Karplus-Strong), picareta na rocha no 2 e no 4, eco de caverna | 100 bpm, 4/4 | 48,0 s (20 compassos) |
| `cripta` | salas da região 3 (Cripta Assombrada) | lenta e assombrada em dó menor harmônico: teremim sobre órgão (A), celesta com coro (B), pedal grave, sino de capela, coração batendo; sem chimbal | 80 bpm, 4/4 | 48,0 s (16 compassos) |
| `forja` | salas normais da região 4 (Forja do Dragão) | sol frígio dominante (hijaz): metais (A), alaúde dedilhado (B), os dois com pulso uma oitava acima (C); bigorna no tempo, taiko, baixo saturado com a 2ª menor | 120 bpm, 4/4 | 48,0 s (24 compassos) |
| `chefe_golem` | salas M2 (Rainha Toupeira) e B2 (Golem de Pedra) | pisada pesada em lá eólio, meio-tempo: trompas graves, coro (B), ponte de percussão com pedras rolando | 100 bpm, 4/4 | 48,0 s (20 compassos) |
| `chefe_necromante` | salas M3 (Viúva Aranha) e B3 (Necromante) | gótica em dó menor harmônico: órgão em tercinas o tempo todo, coro cantando (A), metais (B), metais uma oitava acima com coro (C), sino de capela | 126 bpm, 4/4 | 45,7 s (24 compassos) |
| `chefe_dragao` | salas M4 (Capataz Kobold) e B4 (Dragão Ancião, final) | a mais épica, sol menor: cordas em colcheias, metais heroicos (A), coro (B), metais + coro + taiko (C), ponte com rufar crescendo (D); tímpanos, prato em cada seção | 144 bpm, 4/4 | 46,7 s (28 compassos) |
| `evento` | quadro de eventos e de temporada | fanfarra festiva em fá maior (tema curto) | 120 bpm, 4/4 | 16,0 s (8 compassos) |
| silêncio (`Audio.Music(null)`) | fim do contrato, para o jingle de entrega/falha soar limpo | — | — | — |

- **Identidade por região:** cada chefe fica no centro tonal da masmorra da sua região (lá nas Minas, dó na Cripta, sol na Forja), com andamento e timbre próprios.
- Os acordes foram conferidos por FFT compasso a compasso: as notas mais fortes de cada compasso batem com o acorde escrito. Nas faixas novas, 6 dos 140 compassos têm uma nota de passagem da melodia entre as 3 mais fortes; na `forja`, a bigorna (afinada em ré, o 5º grau) também aparece.
- **Mix interno das faixas novas** (loudness de cada trilha, conferido): a melodia é a trilha mais forte em todas, de 2 a 6 dB acima da seguinte (baixo, bateria ou acompanhamento).
- **Laço sem emenda:** a cauda das notas que passa do fim soma no começo, e o eco/sala é circular (`np.roll`). O salto entre a última e a primeira amostra fica abaixo do salto típico entre amostras vizinhas (p99,9): menu 0,002 contra 0,081, masmorra 0,001 contra 0,106, chefe 0,007 contra 0,120, minas 0,005 contra 0,246, cripta 0,009 contra 0,057, forja 0,000 contra 0,218, chefe_golem 0,001 contra 0,100, chefe_necromante 0,002 contra 0,106, chefe_dragao 0,014 contra 0,107, evento 0,005 contra 0,133.

## 3. Volumes e mix

- **Música:** −22 LUFS integrado (BS.1770 com gating) em todas as 10 faixas, as novas no mesmo nível das antigas. Pico entre −4,9 e −7,2 dBTP.
- **SFX:** alvo por categoria em LUFS momentâneo máximo (janela de 400 ms, ou o clipe inteiro se for menor). É uma aproximação, sem medidor certificado. Os eventos fortes que estouravam o pico passam por um limitador suave (tanh, no máximo 6 dB).

  | Categoria | Alvo | Sons |
  |---|---|---|
  | Grandes | −14 | `explosao`, `pancada`, `rugido`, `quebra`, `golem`, `sopro`, `pouso`, `dragao` |
  | Recompensa, dano e chefes | −15 a −16 | `entrega`, `dano`, `perk`, `bau`, `compra`, `besouro_explode`, `rainha`, `viuva`, `necro_raio`, `necro_ergue`, `voo`, `chicote`, `vagonete`, `mergulho`, `grifinho`, `espelho_racha`, `balao_estoura`, `evento`, `recompensa`, `compra_loja` |
  | Jogo | −17 a −19 | `golpe`, `morte`, `ladrao`, `moedas`, `porta`, `limo`, `picareta`, `toupeira_sai`, `lingua`, `sino`, `ossos_*`, `mumia`, `gargula_acorda`, `frasco`, `magma_divide`, `diabrete`, `fantasma`, `teia`, `salamandra`, `besouro_chia`, `apito`, `lava`, `respiro`, as cargas, `temporada`, `blueprint`, `anuncio` |
  | Frequentes | −20 a −22 | `tiro`, `tique`, `clique`, `toupeira_cava`, `gelo_derrete` |
  | Passo | −24 a −26 | `agua` (−24), `passo` (−26) |

  Ficaram abaixo do alvo, por serem transientes e baterem no limite de pico:
  - antigos: `racha` (−17,9 / −19,9) e `carga` (≈ −16,5);
  - novos, até 1,4 dB abaixo: `balao_estoura` (−17,4), `golem_2` (−15,0), `planta` (−18,4 a −18,9), `ossos_monta` (−18,7), `frasco_1` (−17,7), `lanterna_apaga` (−18,6).

  Os números de cada arquivo saem na tabela que o script imprime.
- **Distância música → SFX:** ~6 dB. A música fica abaixo do SFX de jogo e nunca disputa com o feedback do jogador.
- **Volumes do jogador:** `Ajustes.VolSom` e `Ajustes.VolMusica` (0–10, `dc_vol_som`/`dc_vol_mus`, raia de UI) são a fonte de verdade.
  - `Ajustes.AplicarVolumes()` chama `Audio.SetVolumes(som/10, musica/10)`.
  - O `Audio` também espelha os valores em `dc.som`/`dc.musica` (padrão 1,0 / 0,8 se o `Ajustes` não mandar nada).
- **Celular:** o alto-falante não toca sub. Impactos e baixos têm harmônicos e médio (200–1200 Hz), e o script corta tudo abaixo de 40 Hz nos SFX. Nas músicas novas, os baixos são saturados ou dedilhados (harmônicos no médio).

## 4. Import no Unity (`.meta` gerados pelo script, só se faltarem)

| Clipe | Load Type | Formato | Qualidade | Preload | Load In Background | Por quê |
|---|---|---|---|---|---|---|
| SFX | Decompress On Load | Vorbis | 0,7 | sim | não | curto e frequente: sem custo de CPU ao tocar; ~6,8 MB de PCM na memória com todos carregados |
| Música | Streaming | Vorbis | 0,5 | não | sim | longo: memória mínima, uma faixa por vez (duas durante o crossfade) |

Se o Unity reescrever um `.meta` (versão de serialização), ele não é recriado. Para mudar a regra, apague os `.meta` de `Resources/Audio/` e rode o script de novo.

- **Memória dos SFX:** o `Audio.Init` carrega todos os `sfx/` de uma vez (`Resources.LoadAll`). Com as regiões 2–4 são ~6,8 MB de PCM descomprimido.
  - Cabe no orçamento de hoje.
  - Se o celular de 2 GB apertar, o caminho é carregar o banco da região ao entrar nela, e não trocar o Load Type.
