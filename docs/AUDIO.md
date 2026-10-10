# Dungeon Courier — Áudio (v1.0-beta, CONTRATO_V1 §12)

Todo som e toda música são **sintetizados** por `client/tools/audio_gen.py` (numpy/scipy, semente fixa, sem asset de terceiros nem serviço externo). Licença própria. Uma linha por arquivo em `PROVENIENCIA.md` §4.1.

- **Regenerar:** `python client/tools/audio_gen.py` (≈15 s). O script confere tudo e falha se algo sair do limite: pico verdadeiro < −1 dBTP, DC < 0,001, sem silêncio no começo do SFX e laço da música sem emenda. Com `--md`, imprime as linhas do PROVENIENCIA. Mudou o script: atualize o §4.1.
- **Saída:** `client/Assets/_DC/Resources/Audio/sfx/<nome>_<n>.wav` (65 arquivos, 37 nomes, 44,1 kHz mono 16 bit, 2,6 MB) e `musica/<faixa>.wav` (3 faixas, 22,05 kHz mono 16 bit, 7,8 MB). No total, 10,6 MB no repositório. No APK fica bem menos, porque o Unity comprime em Vorbis.
- **Por que WAV e não OGG:** nem a stdlib nem o scipy têm encoder Vorbis, e um encoder externo seria dependência nova. O Unity faz a compressão no import.
- **Código:** `client/Assets/_DC/Scripts/View/Audio.cs`.
  - `Audio.Play(nome, pitch, minGap)` escolhe uma variação sem repetir a anterior e varia o pitch ±4%. Limita a 3 vozes do mesmo som e 12 no total, roubando a mais antiga.
  - `Audio.Music(faixa)` troca a faixa com crossfade de 1,2 s.
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

## 2. Música (laço)

| Faixa | Quando | Estilo | Andamento | Duração |
|---|---|---|---|---|
| `menu` | título, quadro de contratos, guilda, configurações | valsa de caixinha de música e flauta, lá menor, calma | 96 bpm, 3/4 | 60,0 s (32 compassos) |
| `masmorra` | salas normais do contrato | furtiva com pulso leve: baixo saltitante, chimbal, kick só a partir do 2º bloco, ré menor | 112 bpm, 4/4 | 68,6 s (32 compassos) |
| `chefe` | salas M1 (Capitão Ladrão) e B1 (Ogro) | marcha tensa em mi frígio, baixo em colcheias, metais sincopados, tons | 140 bpm, 4/4 | 54,9 s (32 compassos) |
| silêncio (`Audio.Music(null)`) | fim do contrato, para o jingle de entrega/falha soar limpo | — | — | — |

- Os acordes foram conferidos por FFT compasso a compasso: as notas mais fortes de cada compasso batem com o acorde escrito.
- **Laço sem emenda:** a cauda das notas que passa do fim soma no começo, e o eco/sala é circular (`np.roll`). O salto entre a última e a primeira amostra fica abaixo do salto típico entre amostras vizinhas (p99,9): menu 0,002 contra 0,081, masmorra 0,001 contra 0,106, chefe 0,007 contra 0,120.

## 3. Volumes e mix

- **Música:** −22 LUFS integrado (BS.1770 com gating). Pico entre −5,4 e −6,7 dBTP.
- **SFX:** alvo por categoria em LUFS momentâneo máximo (janela de 400 ms, ou o clipe inteiro se for menor). É uma aproximação, sem medidor certificado. Os eventos fortes que estouravam o pico passam por um limitador suave (tanh, no máximo 6 dB).

  | Categoria | Alvo | Sons |
  |---|---|---|
  | Grandes | −14 | `explosao`, `pancada`, `rugido`, `quebra` |
  | Recompensa e dano | −15 a −16 | `entrega`, `dano`, `perk`, `bau`, `compra` |
  | Jogo | −17 a −19 | `golpe`, `morte`, `ladrao`, `moedas`, `porta`, `limo` |
  | Frequentes | −20 a −22 | `tiro`, `tique`, `clique` |
  | Passo | −26 | `passo` |

  Ficaram abaixo do alvo, por serem transientes de ruído e baterem no limite de pico: `racha` (−17,9 / −19,9) e `carga` (≈ −16,5). Os números de cada arquivo saem na tabela que o script imprime.
- **Distância música → SFX:** ~6 dB. A música fica abaixo do SFX de jogo e nunca disputa com o feedback do jogador.
- **Volumes do jogador:** `Ajustes.VolSom` e `Ajustes.VolMusica` (0–10, `dc_vol_som`/`dc_vol_mus`, raia de UI) são a fonte de verdade.
  - `Ajustes.AplicarVolumes()` chama `Audio.SetVolumes(som/10, musica/10)`.
  - O `Audio` também espelha os valores em `dc.som`/`dc.musica` (padrão 1,0 / 0,8 se o `Ajustes` não mandar nada).
- **Celular:** o alto-falante não toca sub. Impactos e baixos têm harmônicos e médio (200–1200 Hz), e o script corta tudo abaixo de 40 Hz nos SFX.

## 4. Import no Unity (`.meta` gerados pelo script, só se faltarem)

| Clipe | Load Type | Formato | Qualidade | Preload | Load In Background | Por quê |
|---|---|---|---|---|---|---|
| SFX | Decompress On Load | Vorbis | 0,7 | sim | não | curto e frequente: sem custo de CPU ao tocar; ~2,6 MB de PCM na memória |
| Música | Streaming | Vorbis | 0,5 | não | sim | longo: memória mínima, uma faixa por vez (duas durante o crossfade) |

Se o Unity reescrever um `.meta` (versão de serialização), ele não é recriado. Para mudar a regra, apague os `.meta` de `Resources/Audio/` e rode o script de novo.
