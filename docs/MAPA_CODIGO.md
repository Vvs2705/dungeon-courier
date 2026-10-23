# Mapa do código — Dungeon Courier

Como usar: ache o arquivo aqui, depois dê Grep pelo nome do método (`Grep "void Mummy3"`) e leia só o trecho; não abra arquivo de 800-1500 linhas inteiro.
Mantenha este mapa no mesmo PR que cria, move ou divide arquivo. Contagem de linhas: 2026-10-17 (1.4.1-beta), aproximada.

## Core — `client/Assets/_DC/Scripts/Core/` (C# puro, namespace DC.Core, sem UnityEngine, passo fixo 1/30 s)

| Arquivo (linhas) | Para quê | Pontos de entrada |
|---|---|---|
| Defs.cs (707) | Tipos base, enums, eventos, Balance, defs de carga/inimigo/sala/contrato | `V2`, `Ev`/`SimEvent`, `Balance`, `ContractCode.Encode` |
| MetaDefs.cs (250) | Defs da meta: equipamento, roupa, produto IAP, temporada, Config | `Gear`, `Product`, `Season`, `Config.Parse` |
| Sim.cs (811) | Sim: meta, contratos, estrelas, armas, save/load (API que a View chama) | `Sim.StartContract`, `Sim.Tick`, `Sim.Save`, `Sim.Load` |
| Meta.cs (710) | Partial de Sim: sessão, eventos, raro, equipamento, IAP, rewarded, sem fim | `Sim.Purchase`, `Sim.GrantReward`, `Sim.StartEvent`, `Sim.Log` |
| Run.cs (1640) | O contrato em curso: salas, courier, carga, inimigos da região 1, dano, Nav | `Run.Tick`, `Run.Step`, `Run.EnterRoom`, `Run.DamageCourier`, `Run.Guard`, `Run.NavFor` |
| Run.Regiao2.cs (829), Run.Regiao3.cs (913), Run.Regiao4.cs (856) | Partials de Run: R2 minas (água, trilho, gelo), R3 cripta (escuro), R4 forja (lava) | `Run.R2Of`, `Run.R3Of`, `Run.Forge`, ganchos `EnemyR2/R3/R4` |
| Bot.cs (621) | Jogador heurístico determinístico e medição de balanceamento | `Bot.Act`, `Bot.Play`, `Bot.Measure`, `Bot.PlayCampaign` |
| Bot.Regiao2.cs (178), Bot.Regiao3.cs (147), Bot.Regiao4.cs (222) | Partials de Bot: regras e medição da região (R3: save inicial, R4: save fim R3) | `PlanR2/R3/R4`, `Bot.PlayRegion3`, `Bot.MeasureRegion4` |
| Content/Content.cs (242) | Registro de regiões, salas e contratos; contratos gerados (dia, código, evento, sem fim) | `RegionDef.Of`, `Content.Generate`, `Content.Endless` |
| Content/Regiao1.cs (152), Regiao2.cs (141), Regiao3.cs (143), Regiao4.cs (145) | Salas e contratos de cada região (um arquivo por região) | `Regiao2.Rooms`, `Regiao2.Contracts`, `Content.Make` |

Ids de sala e de contrato vão para o save: só crescem no fim do array.

## View — `client/Assets/_DC/Scripts/View/` (Unity, namespace DC; a View só lê o Core)

| Arquivo (linhas) | Para quê | Pontos de entrada |
|---|---|---|
| Game.cs (745) | Único MonoBehaviour: loop, input, Sim.Tick, eventos, save, diário, args `-autoplay`/`-record` | `Game.Awake`, `Game.Update`, `Game.Simulate`, `Game.HandleEvents` |
| Hud.cs (987) | Hud (partial): estrutura uGUI, faixa do contrato, pausa, cena da entrega, resultado | `Hud.Refresh`, `Hud.ShowPlay`, `Hud.ShowEnd`, `Hud.Back` |
| Menus.cs (1097) | Partial Hud: título, menu, quadro de contratos, guilda, perk, configurações, créditos | `Hud.ShowMenu`, `Hud.ShowBoard`, `Hud.RefreshGuild`, `Hud.Dev` |
| MetaTelas.cs (581) | Partial Hud: mapa das regiões, modificadores, eventos, temporada, marcos, sem fim, roupas | `Hud.RefreshMapa`, `Hud.ShowMods`, `Hud.RefreshRoupas`, `Hud.SomMeta` |
| Tour.cs (439) | Partial Hud: tutorial guiado (passos, gatilhos, anel e cartão), `-tutorial` | `Hud.TickTour`, `Hud.Gatilhos`, `Hud.GatilhosJogo`, `Hud.ReverTour` |
| Anuncios.cs (221), AnunciosLevelPlay.cs (152) | `IAnuncios` + simulador (Anuncios.cs); SDK LevelPlay só com `DC_LEVELPLAY` (AnunciosLevelPlay.cs) | `IAnuncios`, `Hud.CriaAnuncios`, `Hud.PedeRecompensa` |
| Loja.cs (179), LojaIap.cs (151) | `ILoja` + tela da loja + loja de teste (Loja.cs); Unity IAP 5 em 2 passos (LojaIap.cs) | `ILoja`, `Hud.CriaLoja`, `Hud.Compra`, `Hud.Concede` |
| WorldView.cs (1379) | Mundo (partial): câmera, sala, courier, carga, pet, inimigos da R1, partículas, `OnEvent` | `WorldView.Refresh`, `WorldView.OnEvent`, `DrawEnemies`, `PaintEnemy` |
| WorldViewV1.cs (765) | Partial WorldView: tiros, flechas, poças, tochas, barra do chefe, selo de risco, explosão | `DrawShots`, `DrawBoss`, `BuildTorches`, `OnEventV1` |
| WorldViewRegioes.cs (1085) | Partial WorldView: tema/terreno das regiões 2-4 (água, trilho, lava, respiro), luz, sons | `BuildRegiao`, `DrawRegiao`, `DrawVeil`, `WorldView.Som` |
| WorldViewElenco.cs (896) | Partial WorldView: inimigos, chefes e cargas das regiões 2-4 (pintura, animação, aviso) | `PaintRegiao`, `AnimRegiao`, `DrawEnemyRegiao`, `DrawCargoRegiao` |
| SpriteSheet.cs (219) | Folha pré-renderizada: meta.json, clipe/direção/quadro, cache | `SpriteSheet.Get`, `SpriteSheet.Frame`, `SpriteAnim.Play` |
| Art.cs (640) | Arte procedural (formas, cores), `Art.Ui`; `Ajustes` (idioma, volumes, vibração, joystick) | `Art.Role`, `Art.Ui`, `Ajustes.Init`, `Aperta` |
| Textos.cs (1305) | Todo texto do jogador em pt/en/es/id, inclusive conteúdo do Core por enum | `Textos.L`, `Textos.Lang`, `Textos.Dica`, `Textos.Carga` |
| Audio.cs (162) | Banco de SFX e música (Resources/Audio), crossfade, volumes | `Audio.Init`, `Audio.Play`, `Audio.Music`, `Audio.MusicaRegiao` |
| Joystick.cs (104) | Joystick de um dedo: lógica pura e entrada do toque | `JoystickLogica.Direcao`, `Joystick.Tick` |
| ConfigLocal.cs (50), Exportar.cs (145) | Remote config sem backend (config.json); exportar diário/código (Android) | `ConfigLocal.Texto`, `Exportar.Diario`, `Exportar.Texto` |
| Dp.cs (27), AreaSegura.cs (18) | Conversão dp para px; ancorar UI na área segura (notch) | `Dp`, `AreaSegura.AncorarDentro` |

## Editor — `client/Assets/_DC/Editor/`

| Arquivo (linhas) | Para quê | Pontos de entrada |
|---|---|---|
| Setup.cs (223) | Settings, cena e builds por script (Windows, APK, AAB assinado); tira AD_ID do manifesto | `Setup.Apply`, `Setup.BuildAndroidRelease`, `Setup.BuildAndroidBundle`, `SemIdDePublicidade` |
| SpriteImport.cs (54) | AssetPostprocessor: import das folhas de sprite e da arte da UI | `SpriteImport.GetVersion` |

## Testes — `client/Assets/_DC/Tests/` (EditMode roda também sem Unity: `dotnet test client/tools/coretests`)

| Arquivo (linhas) | O que cobre | Classe |
|---|---|---|
| EditMode/CoreTests.cs (883) | Regras do contrato v0.1 e catálogo de exploits (GDD §28), helpers dos testes | `CoreTests` (partial) |
| EditMode/CoreTestsV1.cs (846) | Cargas, inimigos, armas, perks, sala secreta, dia/código, save v2 | `CoreTests` (partial) |
| EditMode/CoreTestsEconomia.cs (435) | Economia: projetos e níveis de equipamento, fim de jogo | `CoreTests` (partial) |
| EditMode/CoreTestsLancamento.cs (1115) | Lançamento: multirregião, modificadores, eventos, IAP, rewarded, save v3 | `CoreTests` (partial) |
| EditMode/CoreTestsRegiao2.cs (673) | Região 2: salas, terreno, inimigos, chefes, cargas | `CoreTests` (partial) |
| EditMode/CoreTestsRegiao3.cs (726) | Região 3: escuro, teia, mortos-vivos, Viúva, Necromante, campanha | `CoreTests` (partial) |
| EditMode/CoreTestsRegiao4.cs (598) | Região 4: lava, respiro, Capataz, Dragão, planta, mapa, sem fim | `CoreTests` (partial) |
| EditMode/CoreTestsRevisao.cs (67) | Regressões do caminho do dinheiro (compra, rewarded, interstitial) | `CoreTestsRevisaoCompra` |
| EditMode/CoreTestsRevisaoExploits.cs (98) | Exploits do GDD §28 nas regiões 2-4 | `CoreTestsRevisaoExploits` |
| EditMode/CoreTestsRevisaoSave.cs (96) | Regressões do save v3, migração e datas | `CoreTestsRevisaoSave` |
| EditMode/CoreTestsCombate.cs (170) | Revisão de combate: corpo grande não trava em fresta (todas as salas), Guarda sentinela | `CoreTestsCombate` |
| EditMode/BalanceTests.cs (252) | Medição do bot (faixas largas); tabela do BALANCE.md sai no log | `BalanceTests` |
| PlayMode/LojaIapTests.cs (45) | Compra da roupa com Game real e loja falsa do IAP (`-testsession`) | `LojaIapTests` |

## tools — `client/tools/` (fora do Unity; Blender 5.2 headless e Python 3 com Pillow/numpy)

| Item | Para quê | Como rodar |
|---|---|---|
| coretests/ | Projeto net10.0: Core + testes EditMode, sem Unity | `dotnet test client/tools/coretests` |
| viewcheck/ (core, view, unity_refs.props) | Compila Core e View contra as DLLs do Unity, sem abrir o editor | `dotnet build client/tools/viewcheck/view -nologo -v q` |
| render_sprites.py, render_sprites.ps1 | Blender: personagem 3D rigado e clipes para folhas 2D; o .ps1 chama o Blender | `render_sprites.ps1 -Nome <ator> -Model <fbx>` |
| anim_blender.py | Blender: clipes que o Mixamo não tem, por keyframe sobre o esqueleto Mixamo | `blender -b --python anim_blender.py -- --rig --out` |
| dragao_blender.py | Blender: rig do dragão do Tripo (mandíbula, pesos, 7 clipes) em .blend | `blender -b --python dragao_blender.py -- --fbx --out` |
| bau_abrindo.py, planta_aberta.py, saco_ladrao.py | Blender: baú abrindo, planta de boca aberta, saco do ladrão menor (0 crédito) | `blender -b --python <script> -- <fbx> <saída>` |
| roupas_courier.py, roupas_cor.py, roupas_2d.py | Roupas do courier: no Blender, tabela de cores, ou direto nas folhas (sem Blender) | `python client/tools/roupas_2d.py` |
| elenco_hd.sh | Re-render HD do elenco via `render_sprites.py --hd` (um por vez) | `bash client/tools/elenco_hd.sh <nome>` |
| folhas.py | Folhas de conferência dos sprites (Pillow) em Builds/sprites_contact | `python client/tools/folhas.py <Sprites/ator>` |
| audio_gen.py | Sintetiza todo SFX e música determinísticos (numpy/scipy) em Resources/Audio | `python client/tools/audio_gen.py` |
| ui_assets.py, icone_app.py | Arte da interface para Resources/UI; ícone adaptativo do app a partir do logo | `python client/tools/ui_assets.py` |
| loja_assets.py, loja_fotos.sh | Artes e 8 capturas da página da Play (fotos do build Windows) | `bash loja_fotos.sh`, depois `python -I loja_assets.py` |
| criativos.py | Monta vídeos de UA a partir de gravações `-record` do jogo real (ffmpeg) | `python -I client/tools/criativos.py --listar` |
| diario_report.py | Relatório do playtest a partir de diários CSV exportados | `python client/tools/diario_report.py <pasta>` |
| regioes_check.py | Dados das regiões 2-4 e checagem do level design em Python | `python client/tools/regioes_check.py` |

## Fluxo principal

- `Game.Awake` (RuntimeInitializeOnLoadMethod cria o `Game`) carrega o save (`Sim.Load`), monta `Hud`, `WorldView` e `Joystick`; `Game.Update` roda todo quadro.
- `Game.Simulate` lê o joystick/teclado e chama `Sim.Tick(dt, move, dash)`; com `-bot` o `Bot.Act` joga no lugar (`Game.BotFlow`).
- `Sim.Tick` limpa `Sim.Events` e chama `Run.Tick`, que acumula dt e roda `Run.Step` em passos de 1/30 s (ganchos de região `EnemyR2/R3/R4`, `StepR2/R3/R4`).
- Ao fim do contrato `Sim.Settle` aplica moedas, reputação e carga pendente (`Sim.LastResult`); `Game.Save` grava `Sim.Save()` no PlayerPrefs.
- `Game.HandleEvents` repassa cada `SimEvent` ao `WorldView.OnEvent`, ao `Hud.EventoMundo`, ao `Audio` e ao diário CSV.
- Depois `WorldView.Refresh(Sim.Run, dt)` e `Hud.Refresh(dt)` só leem o estado do Core; as telas chamam de volta `Game.Play`, `Game.Buy`, `Game.PickPerk`.
