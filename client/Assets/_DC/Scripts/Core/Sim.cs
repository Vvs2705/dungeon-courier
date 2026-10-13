using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// API do nucleo para a View (DC.Core, C# puro, deterministico, passo fixo de 1/30 s). Tudo em metros/segundos, origem no canto
// inferior esquerdo da sala (9 x 14).
//
//   Abrir:      var sim = Sim.Load(texto) (ou new Sim(semente)). sim.SaveRefused => save de versao futura: avise e NUNCA grave
//               (sim.Save() devolve null e sim.Dirty fica falso).
//   Meta:       sim.Coins, sim.Reputation (contratos concluidos em sequencia), sim.Unlocked(i), sim.GuildOpen, sim.Levels[(int)Upgrade.X],
//               sim.UpgradeCost(u) (-1 = no maximo), sim.CanBuy(u), sim.Buy(u), sim.MaxHp, sim.DashCooldown, sim.CoinMul,
//               sim.Pending (LostCargo a resgatar, ou null).
//   Conteudo:   Sim.Contracts[i] (ContractDef: Code, Cargo, Pay, EggTarget, Layers, Note), Content.Rooms[id] (RoomDef: Obstacles,
//               Spawns, ChestPos/ChestCoins, Threat, Delivery), CargoDef.All, EnemyDef.All, Balance.*, Hints.Text(hint).
//   Jogar:      sim.StartContract(i, resgate) -> Run; a cada quadro sim.Tick(dt, move, dash) (move = V2.Zero com o dedo solto; dash = true
//               no quadro do toque rapido). Ler sim.Run: Room, Layer, Doors, Chest, Courier, Cargos, Pet, Enemies, Shots, Time,
//               EggLeft, Hint, Outcome. sim.Events = eventos desde o ultimo Tick (limpa no inicio de cada Tick; Start/Buy/Abandon
//               acrescentam). sim.Abandon() desiste.
//   Fim:        sim.Run.Outcome != Running => sim.LastResult (ContractResult) ja aplicado: moedas, reputacao, carga pendente.
//   Gravar:     if (sim.Dirty && !sim.SaveRefused) gravar(sim.Save()). Dirty liga a cada dano, rachadura, sala, bau, fim e compra.
//               Reabrir no meio do contrato retoma na ENTRADA da sala atual com a vida e as rachaduras gravadas.
//   Diario:     SimEvent.DiaryName(e.Kind) da o nome do GDD §14 (null = evento so de SFX); A/B de cada tipo no comentario de Ev.
//   Fixos:      Balance.Entrance, Balance.Client (D1), Balance.RoomW/RoomH, Balance.DoorHalf, Balance.Step (1/30 s).
//   Bot:        -autoplay: Bot.Play(sim, Math.Min(sim.Reputation, Sim.Contracts.Length - 1), sim.Pending != null, new Bot(semente)) (null =
//               nao comecou). -bot com render: acumule o dt e
//               chame bot.Act(sim) a cada Balance.Step (Act ja chama sim.Tick).
//
// ---------------------------------------------------------------------------------------------------------------------------------
// v1.0-beta (CONTRATO_V1 §2-§10) — ACRESCIMOS. Nada acima foi renomeado nem removido; nomes abaixo sao estaveis (so crescem).
// ---------------------------------------------------------------------------------------------------------------------------------
//   Enums (novos valores sempre no FIM; a View precisa de arte/texto para eles: WorldView.CargoSheet/EnemySheet sao por indice):
//               CargoType.Cake (bolo), .Message (mensagem urgente), .Heavy (caixa pesada), .Barrel (barril de polvora)
//               EnemyType.Archer (arqueiro), .Slime (limo), .Captain (Capitao Ladrao, mini-chefe)
//               Upgrade.Pack (Mochila) -> sim.Levels tem 4 linhas (Balance.UpgradeLines = 4); Balance.UpgradeMax = 8
//               Outcome.TimeUp (mensagem: tempo esgotado), Outcome.Exploded (barril explodiu)
//               Hint.CakeNoDash, .BarrelFuse, .ArcherAim, .SecretWall, .MessageDoor (texto em Hints.Text)
//               novos: Weapon {Sling, Boomerang, Bomb} · Perk {LightStep, QuickPlant, DoubleDash, Padded, QuickHand, Magnet, ExtraHeart,
//               Lookout, IronBoot, ChestLuck} · [Flags] CargoRisk {None, Thief=1, Archer=2, Smash=4} · ContractKind {Campaign, Daily, Code}
//               · RoomTrigger {None, Half, CargoDown}.
//   Guilda:     4 linhas x 8 niveis (Vida, Dash, Bolsa, Mochila); laco com Balance.UpgradeLines, nao com 3. sim.PickupRange (raio de pegar
//               com Mochila), sim.PlantDelay (pouso com Mochila). Balance.MaxHpCap = maior vida possivel (guilda + perk) p/ os coracoes.
//   Estrelas:   sim.StarsOf(i) (0-3, melhor resultado do contrato i da campanha), sim.Stars (soma; e a "reputacao" mostrada na UI e
//               destrava armas). sim.Reputation CONTINUA = contratos concluidos em sequencia (indice do proximo); Unlocked(i) igual.
//               sim.DevStars(n) = SO teste/fotos (-testsession -weapon W): estrelas em ordem ate somar n (max 3 por contrato), so na
//               memoria, sem evento e sem Dirty (destrava as armas sem jogar).
//               ContractResult: Stars, StarDelivered, StarIntact, StarTime, NewBest, Def (ContractDef jogado), Kind, DailyBonus, ShareCode.
//               Estrela 1 = entregue; 2 = carga intacta (0 rachadura; ovo sem chocar); 3 = entregue dentro de ContractDef.ParTime.
//   Armas:      WeaponDef.All[(int)w] (Name, Text, Stars = estrelas p/ destravar, Damage[3], Cooldown[3], Range, Radius, Cost[3] = preco
//               p/ subir ao nivel 2 e 3 em [1],[2]). sim.WeaponUnlocked(w), sim.WeaponLevel(w) (0 = bloqueada; 1-3), sim.WeaponCost(w)
//               (-1 = bloqueada ou no maximo), sim.CanUpgradeWeapon(w), sim.UpgradeWeapon(w), sim.Equipped, sim.Equip(w) (so fora do
//               contrato e com a arma destravada; emite WeaponEquip). run.Weapon / run.WeaponLevel = arma do contrato em curso.
//   Perks:      run.PerkOffer (Perk[3] ou null). Com oferta aberta o Tick NAO avanca (jogo parado na entrada da sala): mostre as 3 e chame
//               sim.PickPerk(0..2). run.Perks (lista escolhida), run.Has(perk). PerkDef.All[(int)p] (Name, Text). Oferta depois das
//               camadas 2 e 4 (nunca na sala de entrega); sorteio pela semente do contrato.
//   Do dia:     data = int AAAAMMDD vinda da View (o Core nao le relogio). sim.SeeDate(data) ao abrir o menu (guarda a maior data vista);
//               sim.DailyOpen (apos o C3), sim.DailyAvailable(data) (falso se ja entregou nesse dia OU data < sim.LastDateSeen: voltar o
//               relogio nao repete), Sim.DailyContract(data) (ContractDef p/ mostrar antes), Balance.DailyBonus (moedas extras, 1x por
//               data), sim.StartDaily(data, resgate) -> Run (null = indisponivel). sim.DailyDone = ultima data entregue.
//   Codigo:     ContractCode.Alphabet (32 simbolos: A-Z sem I/O + 2-9), ContractCode.Normalize(s), ContractCode.IsValid(s),
//               ContractCode.Encode(payload) / TryDecode(s, out payload) (payload 0..ContractCode.MaxPayload, 6 caracteres com digito de
//               verificacao), ContractCode.Random(entropia). Sim.CodeContract(codigo) (ContractDef ou null), sim.CodeOpen (apos o C3),
//               sim.StartCode(codigo, resgate) -> Run (null = codigo invalido/fechado). ContractDef.ShareCode = codigo p/ compartilhar.
//   Contratos:  ContractDef novos: Kind, Name, ParTime (tempo-alvo da 3a estrela), TimeLimit (cronometro da mensagem; 0 = sem),
//               ShareCode (null na campanha), Payload. Contrato gerado (dia/codigo) tem Index = -1: use sim.Run.Contract ou
//               sim.LastResult.Def; NUNCA Sim.Contracts[Index] com Index < 0. Gerado nao mexe em Reputation nem em estrelas.
//               Content.RegionName; Content.Rooms (21: R1-R16, S1, S2, M1, B1, D1; ids estaveis, constantes Content.R1..R16/S1/S2/M1).
//   Salas:      RoomDef novos: Risk (CargoRisk da sala), Secret (id da sala secreta ou -1), SecretPos (ponto da parede rachada na
//               parede lateral), Wave (Spawn[] do reforco), WaveTrigger (RoomTrigger), WaveLocks (porta fecha ate o reforco morrer),
//               IsSecret, Mini. Door.Risk (CargoRisk da sala de destino), alem de Door.Threat. run.Secret (SecretWall: Pos, Room) ou null:
//               empurrar CONTRA ela com a carga (Balance.SecretPush) leva a sala secreta (mesma camada; portas iguais). run.Waves (reforcos que ja
//               vieram), run.WaveDone (chegou no teto), run.WavePending (inimigos do reforco ainda na fila, 1 por Balance.WaveGap s).
//               Teto: Balance.MaxWaves(nivel) reforcos por visita (1 ate o C4, 2 depois) e Balance.MaxAlive inimigos vivos. ContractDef.Waves
//               (tutoriais C1-C3 sem reforco), ContractDef.Tier (vida dos inimigos x Balance.HpMul(Tier); morcego e limo fora).
//   Cargas:     Cargo.Fuse (s do barril no chao; explode em Balance.BarrelFuse; zera ao pegar), Cargo.PickT (pegando: caixa pesada leva
//               Balance.HeavyPick), Cargo.Wobble (s restantes do balanco do bolo). run.TimeLeft (mensagem; 0 = sem cronometro),
//               run.HasTimer. Bolo: o toque rapido nao da dash (CakeWobble). Mensagem: abre porta trancada por Guarda/reforco.
//               run.Holds(tipo) = carga desse tipo nas maos; run.PlantDelayNow = tempo de pouso agora (perk, Mochila, caixa pesada).
//               Sala nova (porta ou parede rachada) cura Balance.RoomHeal de vida.
//   Inimigos:   Enemy.Aim (direcao da mira; arqueiro em State Telegraph = linha de aviso), Enemy.Stun (s atordoado), Enemy.Hits (Capitao:
//               golpes levados com a carga; 2 derruba), Enemy.Wave (nasceu no reforco). run.Arrows (Arrow: Pos, Dir), run.Puddles
//               (Puddle: Pos, Radius, Left), run.Shots (Shot: + Weapon, Dir, Returning, Goal, Progress 0..1 do arco da bombinha).
//               run.CanHit(e) = a arma equipada alcanca e (o bumerangue precisa de linha livre); run.LineClear(a, b, r) = linha de visada.
//               O limo rebate Balance.SlimeRecoil a cada toque. Risco/arte: EnemyDef.Boss (Ogro, Capitao), EnemyDef.Risk.
//   Courier:    Courier.DashCharges / DashChargesMax (Dash duplo = 2), Courier.Slowed (na poca do limo).
//   Eventos novos (A/B no comentario de Ev): PerkOffer, PerkPick, WeaponEquip, WeaponUpgrade, WeaponUnlock, ContractStars, DailyStart,
//               DailyEnd, CodeStart, RoomEvent, Explosion, ArrowShot, ArrowHit, PuddleSpawn, CakeWobble, SecretFound, FuseLit, TimeWarning,
//               MessageDoor, Stun, CaptainCall. Diario (DiaryName): contract_stars, perk_pick, weapon_equip, daily_start, daily_end,
//               code_start. Ev.Shot ganhou B = (int)Weapon. CurrencySource B: 3 = bonus do dia. CurrencySink B: 10 + (int)Weapon = nivel de arma.
//   Save:       v=2 (Sim.SaveVersion). Le v1 (Mochila 0, estrelas 1 por contrato concluido, contrato em curso descartado porque os
//               contratos mudaram) e v0. (Lancamento: v=3, ver abaixo.)
//   Bot:        bot.Act(sim) ja escolhe perk quando ha oferta (Bot.ChoosePerk). Bot.WeaponFor(sim, def) = arma que a campanha leva.
//               Bot.PlayCampaign joga os 24 (compra guilda e armas, troca a arma pela carga). Bot.Finish(sim, bot) joga um contrato ja
//               comecado (dia/codigo). Medicao: Bot.MeasureCampaignTable, Bot.MeasureRoute (H2), Bot.MeasureGenerated.
//
// ---------------------------------------------------------------------------------------------------------------------------------
// LANCAMENTO, Leva A (CONTRATO_LANCAMENTO §2-§7; CONTEUDO_REGIOES no que e do nucleo) — ACRESCIMOS. Nada acima foi renomeado; o que
// mudou de comportamento esta marcado (MUDOU). Numeros novos sao PROVISORIOS (Balance/MetaDefs.cs; BALANCE.md "Lancamento").
// ---------------------------------------------------------------------------------------------------------------------------------
//   Regioes:    RegionDef.All[0..3] / RegionDef.Of(1..4): Id, Name, Theme ("poroes", "minas", "cripta", "forja": a View escolhe piso,
//               parede, paleta e musica por ele), Stars (para destravar), First (indice global do 1o contrato), Contracts, Rooms/Normal/
//               Secrets (ids globais), Mini/Boss/Delivery (-1 = nao ha), MiniAt/BossAt, CargoCount, TierBase, GenPay/GenPar/GenEgg/
//               GenMelt/GenLimit (gerado da regiao), HasContent (as regioes 2-4 nascem vazias e ficam fechadas). sim.RegionOpen(r) =
//               conteudo + ultimo contrato da anterior entregue + RegionDef.Stars; sim.OpenRegions(). ContractDef.Region, RoomDef.Region,
//               run.Region. Content.Rooms/Content.Contracts = concatenacao (regiao 1 primeiro: ids e indices da v1 iguais); Sim.Contracts
//               = todos. (MUDOU) sim.Unlocked(i) tambem pede RegionOpen. Conteudo novo: Content/RegiaoN.cs (ids LOCAIS; Content.Make) e
//               comportamento em Run.RegiaoN.cs (ganchos parciais no fim de Run.cs: EnterRoomRn, StepRn, EnemyRn, SpeedRn, EnemyDiedRn,
//               HintRn; uteis: Wake, CargoGrabbed, Breaks, InWater, InLight, Finish(Outcome.Melted/Escaped)).
//   Enums (no fim): CargoType +8 (ArcaneHeart..TreasureMap), EnemyType +14 +6 chefes + MagmaShard, SalamanderEgg, Perk +15,
//               ContractKind.Rare/Event/Endless, Outcome.Melted/Escaped, Hint +23 (texto em Hints.Text), Ev (A/B no topo de MetaDefs.cs).
//               Tipos novos: LiveEvent, Modifier ([Flags]), Gear, Outfit, Product, Reward, Goal, Web, Vent, Rail, BuriedChest, Trap.
//               EnemyDef/CargoDef novos com Provisional = true: sem comportamento o inimigo persegue e golpeia (Melee; parado e sem dano =
//               inerte) e a carga e normal. EnemyDef.Boss (os 6 novos), .Mini, .Hazard (vagonete: nao e alvo nem conta no teto), .Region.
//               CargoDef.Region/.Fragile. PerkDef.Region: os novos so entram no sorteio de contrato da regiao 2 em diante (Olhos de gato,
//               3): a regiao 1 sorteia como na v1.
//   Terreno:    RoomDef no inicializador: Dark, Torches, Water, Lava, Webs, Vents, Rails, Buried, SecretHidden, Blinks, Blueprint.
//               Spawn.Asleep / Enemy.Asleep (zZ). (MUDOU) RoomDef.Risk virou propriedade (lava ou respiro = Pancada). O nucleo ja faz:
//               lava solida para andar e transparente para tiro (Run.Blocked x Run.ShotBlocked, Nav.ShotClear; run.LineClear e a de
//               tiro), agua x0,75 (run.InWater; chefes, voadores, Pe firme e Botas nao), escuro (run.Dark, run.Lit, run.InLight(p),
//               run.Torches; Cargo.Out = lanterna apagada; a mira, run.CanHit, so pega o que esta na luz), dormindo (acorda com o courier a
//               2 m, explosao a 4 m, vizinho acordado a 3 m por 1 s; o 1o golpe dobra), bau enterrado (run.Buried; so com run.Revealed) e
//               parede rachada disfarcada. Trilho, respiro, teia e fogo no chao sao das raias.
//   Run novo:   run.Id, run.Mods/Has(Modifier), run.ParTime (Pressa), run.EggTarget (Isolamento; so ovo), run.MeltTime/IceFailAt (gelo:
//               ContractDef.MeltTime = o campo EggTarget no cristal), run.Tier (sem fim sobe por camada), run.BreakAt, run.VisionRadius
//               (Neblina), run.LightMul, run.SureFooted, run.BootsLevel, run.Kills/ChestsOpened/SecretsFound/BossesClean/HuntCoins,
//               run.SecondWindUsed, run.Gear/GearLevel/SmokeLeft/HookCd/Traps (o equipamento dispara sozinho, sem botao: fumaca quando
//               pegam a carga, gancho andando para a carga no chao, armadilha no 1o pouso da sala, botas passivas), run.SafeDoor,
//               Door.Blueprint/ChestKnown/Enemies, Chest.Blueprint.
//   Relogio:    o Core nao le relogio: data AAAAMMDD e instante em segundos Unix vem da View. sim.OpenSession(agora, data) ao abrir e ao
//               voltar do fundo (conta a sessao; depois de Config.RareHours h oferece o raro), sim.Touch(agora) ao pausar/sair.
//               Sim.Week(data) (segunda a domingo), Sim.SeasonOf(data), Sim.SeasonDaysLeft(data), Sim.DayNumber(data).
//   Contratos:  Sim.DailyContract(data) continua = o do dia NA REGIAO 1 (atalho da v1); o do jogador e sim.DailyFor(data) (regiao
//               sorteada entre as destravadas, muda com o reroll) e e o que StartDaily joga: a View deve mostrar DailyFor. Com os
//               modificadores abertos o dia vem com Sim.DailyModifier(data). Codigo: a regiao sai dos 2 bits altos do payload
//               (Content.RegionOf/WithRegion; regiao 1 = igual a beta); sim.NewCode(entropia) gera numa regiao destravada; StartCode
//               recusa regiao fechada. Raro: sim.RareAvailable, sim.RareDate, sim.RareContract (x2), sim.StartRare(resgate), sim.RareDone.
//               Reroll: sim.RerollCost, sim.CanReroll(raro), sim.Reroll(raro) (1x por dia cada; o preco cresce no dia). Evento:
//               Sim.EventOf(data), sim.EventsOpen, sim.EventSlots(data) (3; +2 com o pacote), sim.EventContract(data, k), Sim.EventReward(k),
//               sim.EventAvailable/EventDone, sim.StartEvent(data, k, resgate) (sem resgate em evento); as 3 vagas na semana dao
//               ContractResult.EventProject. Sem fim: sim.EndlessOpen, sim.EndlessContract(data), sim.StartEndless(data),
//               sim.EndlessBest/EndlessWeekBest/EndlessCoinsLeft. Modificadores: sim.ModifiersOpen, sim.StartContract(i, resgate, mods) e
//               sim.StartCode(codigo, resgate, mods) com 0-2 (null se invalido), ModifierDef.All (Name, Text, Pay %).
//   Meta:       temporada: Season.Goals[20], sim.SeasonId, sim.SeasonProgress(i), SeasonReady(i), ClaimSeason(i), SeasonClaimed(i).
//               Maestria: sim.MasteryCount/MasteryLevel(carga) (+2% no pagamento por nivel). Marcos: Balance.Milestones,
//               sim.MilestoneReady/ClaimMilestone/MilestoneClaimed(i). Equipamento: GearDef.All (+ SmokeTime, SmokeUses, HookCooldown,
//               TrapTime, TrapCatches por nivel), sim.GearOpen, sim.Blueprints(g), GearUnlocked, GearLevel (0-8), GearCost, CanUpgradeGear,
//               UpgradeGear, EquipGear (Gear.None = vazio; so fora do contrato), GearEquipped, ProjectTaken(sala). Roupas: OutfitDef.All,
//               sim.OwnsOutfit, sim.EquipOutfit, sim.OutfitEquipped.
//   Monetizacao (o Core nao fala com SDK): rewarded = sim.RewardOffer(t) (mostra o botao?), sim.RewardId(t) (rw-<contrato>-<tipo>) e,
//               depois do anuncio assistido ate o fim, sim.GrantReward(t): Resume volta para a ENTRADA da sala da falha com vida cheia
//               (1x por contrato, nunca na 1a sessao, nao no sem fim nem depois de desistir); DoubleBonus paga ContractResult.Bonus de novo;
//               SafeRoute liga run.SafeDoor; DailyChest paga Sim.DailyChestCoins(data) + sim.DailyChestBlueprint(data) (mostre antes).
//               Ids gravados no save: fechar e reabrir nao repete. Ao mostrar a oferta: sim.Log(Ev.RewardedOffer, (int)t). Interstitial:
//               sim.InterstitialDue(agora) entre contratos; depois de mostrar, sim.InterstitialShown(agora). IAP: com a compra
//               confirmada, sim.Purchase(produto) (falso = nada a conceder, ex.: restaurar o que ja tem); sim.Owns, sim.RemoveAds,
//               sim.StarterOffered (mostrar a oferta depois do C3).
//   Config:     sim.Config = Config.Parse(texto de Resources/config.json ou da URL opcional). Chave desconhecida ou invalida = padrao.
//   Diario:     nomes novos em SimEvent.DiaryName. device_fps_bucket: a cada 60 s, sim.Log(Ev.DeviceFps, Sim.FpsBucket(fps)).
//   Resultado:  ContractResult.RunId, PayPct, Mods, Mastery, EventReward, EventProject, HuntCoins, EndlessLayers, EndlessCoins, Bonus,
//               BonusDoubled; Total soma tudo. LostCargo.Region: o resgate so aparece num contrato da mesma regiao.
//   Save:       v=3 (Sim.SaveVersion). Le v2 (contrato em curso mantido; dia/codigo em curso da beta regenerados na regiao 1), v1 e v0
//               (migracoes explicitas, Dirty = true); v4+ = SaveRefused. Linhas novas: ses, rare, rr, ev, ssn, mst, mls, gear, fit, iap,
//               ads, end, rw, bpr; no contrato em curso: rx.
//   Bot:        Bot.PlayCampaign(semente, ..., region: 1) e Bot.MeasureCampaignTable(campanhas, sementes, region: 1) jogam e medem ate o
//               fim da regiao pedida (padrao a 1, como antes); MeasureRoute (H2) so na regiao 1. O bot nao usa equipamento, modificador,
//               evento nem rewarded. PlayCampaign(..., from: save) comeca de um save (ex.: Bot.Regiao3EndSave, fim estimado da regiao 3) e
//               compra nivel de equipamento destravado (regiao 1: nenhum). Regiao 4: Bot.MeasureRegion4; API da View no topo de Run.Regiao4.cs
//               (run.Forge, VentState/VentFireIn, NearFire, InLava; eventos BossPhase, TakeOff, Land, PlantBite, LavaBurn, VentFire, EggHatch,
//               ForemanCall). run.Untouchable(e) = intocavel (Dragao no ar; gancho HitMulRn no fim de Run.cs, 2 = dano x2 na cabeca baixa).
//
// ---------------------------------------------------------------------------------------------------------------------------------
// LANCAMENTO, Leva C, raia E (economia; BALANCE.md "Economia da campanha") — ACRESCIMOS. Nada acima foi renomeado nem removido.
// ---------------------------------------------------------------------------------------------------------------------------------
//   Reroll:     sim.RerolledToday(raro) = ja trocou hoje (CanReroll falso por isso, e nao por falta de moeda: compare com Coins < RerollCost).
//   Evento:     sim.WeekProject(data) = projeto que as 3 vagas da semana dao (Gear.None = nenhum faltando: Balance.EventProjectCoins moedas).
//   Roupas:     Outfit.Miner (Mineiro) e .Gravekeeper (Coveiro) no fim do enum, a venda por Product.OutfitMiner / .OutfitGravekeeper (no fim do
//               enum, depois do EventPack): 6 roupas na loja; Classica gratis e Fantasma so por marco/temporada.
//   Equipamento (MUDOU): niveis custam 1.000 / 2.500 / 4.000 / 5.000 (GearDef.Cost); cada equipamento tem 4 baus de projeto para 3
//               projetos (o Gancho completa na regiao 3) e a porta/bau so mostram o projeto enquanto o equipamento precisa dele
//               (Door.Blueprint / Chest.Blueprint = None depois de destravado; o bau sobrando paga so as moedas).
//   Dev (so teste/fotos, como o DevStars): sim.DevProjects(gear, n) poe n projetos (Gear.None = nos 4) so na memoria; sim.DevStartAt(contrato,
//               "B2") comeca o contrato direto na camada da sala (contrato -1 = o 1o que passa por ela). Game.cs: -testsession -room B2.
//   Bot:        Bot.BuyAll(sim) = a regra de compra da campanha; Campaign.EarnedPer/SpentPer (por contrato) e Campaign.End (save do fim).
//   Fim de jogo (raia E2, MUDOU): Balance.GearMaxLevel = 8 (era 5) e Balance.GearCampaignLevel = 5. sim.GearCap = 5 ate vencer o Dragao
//               (sim.DragonBeaten: M4-16 entregue), depois 8; GearCost = -1 no teto. Niveis 6-8 custam 10.000 / 13.000 / 18.000; as tabelas
//               por nivel (GearDef.SmokeTime/SmokeUses/HookCooldown/TrapTime/TrapCatches e o novo BootsFire) tem 8 entradas. A tela desenha
//               8 pontos e os acima de sim.GearCap travados ("6-8: DRAGAO"). O efeito extra do nivel 5 (GearCampaignLevel) continua no 5.
//   Exploits (revisao do GDD §28, MUDOU): a linha cgN do save ganha um 6o campo opcional = gelo exato do cristal ou 1 = lanterna apagada
//               (reabrir o app nao devolve gelo nem reacende; save sem o campo = topo da faixa, como antes; o rewarded retomar continua assim).
//               Cacador paga no maximo Balance.PerkHunterCap (20, antes da Bolsa) por visita de sala (CONTEUDO_REGIOES §7).

namespace DC.Core
{
    /// <summary>Carga perdida (morte ou ladrao que escapou), pendente para resgate no proximo contrato DA MESMA REGIAO. Value = pagamento
    /// base dela (Pay x multiplicador da carga): o bonus do resgate e Balance.RescueBonus disso. Contract = -1 se veio de contrato gerado.</summary>
    public sealed class LostCargo
    {
        public int Id; public CargoType Type; public int Cracks, Room, Value, Contract;
        /// <summary>Regiao da sala onde ficou (o resgate so aparece num contrato desta regiao).</summary>
        public int Region => Room >= 0 && Room < Content.Rooms.Length ? Content.Rooms[Room].Region : 1;
    }

    /// <summary>Resultado de um contrato, com o pagamento detalhado (Total = Payment + RescueBonus + ChestCoins + DailyBonus + EventReward +
    /// HuntCoins + EndlessCoins; o rewarded "dobrar bonus" soma Bonus de novo e liga BonusDoubled).</summary>
    public sealed class ContractResult
    {
        public int Contract; public Outcome Outcome; public float Time;
        public int Cracks; public bool Hatched;
        public int Base; public float CargoMul, IntegrityMul, BagMul;
        public int Payment, ChestCoins, RescueBonus, Total;
        public bool RescueOffered, RescueAccepted, RescueDelivered;
        public int DeathRoom = -1, RoomsNormal, RoomsFought;
        public bool Delivered => Outcome == Outcome.Delivered;
        // v1.0-beta
        /// <summary>O contrato jogado (use este, nao Sim.Contracts[Contract]: gerado tem Contract = -1).</summary>
        public ContractDef Def; public ContractKind Kind;
        /// <summary>Estrelas (0-3): entregue + carga intacta + dentro de Def.ParTime. NewBest = melhorou o recorde do contrato (campanha).</summary>
        public int Stars; public bool StarDelivered, StarIntact, StarTime, NewBest;
        /// <summary>Bonus do contrato do dia (ja creditado, com Bolsa); 0 nos outros.</summary>
        public int DailyBonus;
        /// <summary>Codigo para compartilhar (dia e codigo); null na campanha.</summary>
        public string ShareCode;
        /// <summary>Perks escolhidos na run.</summary>
        public Perk[] Perks;
        public Weapon Weapon;
        // lancamento
        /// <summary>Id da tentativa (rewarded rw-&lt;RunId&gt;-&lt;tipo&gt;). PayPct = multiplicador extra do pagamento em % (modificadores,
        /// maestria, evento, raro; 100 = nenhum). Mods = modificadores. Mastery = nivel de maestria da carga depois da entrega.</summary>
        public int RunId, PayPct = 100, Mastery; public Modifier Mods;
        /// <summary>Recompensa fixa do evento (1x por vaga e semana), moedas do Cacador, camadas e moedas do desafio sem fim.</summary>
        public int EventReward, HuntCoins, EndlessLayers, EndlessCoins;
        /// <summary>Bonus do contrato = pagamento acima do basico (integridade, modificadores, maestria, evento, raro) + bonus do dia: o que o
        /// rewarded "dobrar bonus" paga de novo.</summary>
        public int Bonus; public bool BonusDoubled;
        /// <summary>Projeto da semana ganho ao completar as 3 vagas do evento (None = nao).</summary>
        public Gear EventProject;
    }

    public sealed partial class Sim
    {
        public const int SaveVersion = 3;
        public static ContractDef[] Contracts => Content.Contracts;

        public int Seed, RunsStarted, NextCargoId = 1;
        public int Coins, Reputation;
        public readonly int[] Levels = new int[Balance.UpgradeLines];
        public LostCargo Pending;
        /// <summary>Ids de carga que ja pagaram bonus de resgate (o bonus sai uma vez por id).</summary>
        public readonly HashSet<int> RescuedIds = new HashSet<int>();
        public bool SaveRefused, Dirty;
        public Run Run; public ContractResult LastResult;
        public readonly List<SimEvent> Events = new List<SimEvent>();
        /// <summary>Arma equipada (troca so fora do contrato). LastDateSeen = maior data AAAAMMDD ja vista (anti-relogio); DailyDone =
        /// ultima data em que o contrato do dia foi entregue.</summary>
        public Weapon Equipped;
        public int LastDateSeen, DailyDone;
        readonly int[] best = new int[Content.Contracts.Length];
        readonly int[] weaponLv = { 1, 1, 1 };

        public Sim(int seed = 1) { Seed = seed; }

        public bool InContract => Run != null && Run.Running;
        /// <summary>Contrato i da campanha aberto: em sequencia (i &lt;= Reputation) e a regiao dele destravada.</summary>
        public bool Unlocked(int i) => i >= 0 && i < Contracts.Length && i <= Reputation && RegionOpen(Contracts[i].Region);
        /// <summary>A guilda abre depois do 1º contrato (o 1º upgrade sai antes do C2).</summary>
        public bool GuildOpen => Reputation >= 1;
        public int MaxHp => Balance.BaseHp + Balance.LifePerLevel * Levels[(int)Upgrade.Life];
        public float DashCooldown => Balance.DashCooldown * (1f - Balance.DashCdPerLevel * Levels[(int)Upgrade.Dash]);
        public float CoinMul => 1f + Balance.BagPerLevel * Levels[(int)Upgrade.Bag];
        /// <summary>Raio de pegar a carga com a Mochila (1,5 m + 0,15 m por nivel).</summary>
        public float PickupRange => Balance.PickupRange + Balance.PackRangePerLevel * Levels[(int)Upgrade.Pack];
        /// <summary>Pouso com a Mochila (0,2 s - 0,02 s por nivel), sem perk nem caixa pesada (esses estao em Run.PlantDelayNow).</summary>
        public float PlantDelay => Math.Max(Balance.MinPlantDelay, Balance.PlantDelay - Balance.PackPlantPerLevel * Levels[(int)Upgrade.Pack]);

        public int UpgradeCost(Upgrade u) => Levels[(int)u] >= Balance.UpgradeMax ? -1 : Balance.UpgradeCost(Levels[(int)u]);
        public bool CanBuy(Upgrade u) { int c = UpgradeCost(u); return GuildOpen && !InContract && !SaveRefused && c >= 0 && Coins >= c; }

        public bool Buy(Upgrade u)
        {
            if (!CanBuy(u)) return false;
            int cost = UpgradeCost(u);
            Coins -= cost;
            Levels[(int)u]++;
            Emit(Ev.GuildUpgrade, (int)u, Levels[(int)u], V2.Zero);
            Emit(Ev.CurrencySink, cost, (int)u, V2.Zero);
            Dirty = true;
            return true;
        }

        // ------------------------------------------------------------------ regioes

        /// <summary>Regiao destravada: a 1 sempre; as outras com conteudo, o ultimo contrato da anterior entregue (o chefe dela: a campanha e
        /// linear, entao Reputation &gt;= First) e RegionDef.Stars estrelas.</summary>
        public bool RegionOpen(int region)
        {
            RegionDef r = RegionDef.Of(region);
            if (r == null) return false;
            return region == 1 || (r.HasContent && Reputation >= r.First && Stars >= r.Stars);
        }

        /// <summary>Ids (1-4) das regioes destravadas, em ordem (sempre comeca com 1).</summary>
        public int[] OpenRegions()
        {
            var l = new List<int>();
            foreach (RegionDef r in RegionDef.All) if (RegionOpen(r.Id)) l.Add(r.Id);
            return l.ToArray();
        }

        /// <summary>Regiao sorteada pela semente entre as destravadas (dia, raro, evento, codigo novo).</summary>
        int PickRegion(int seed, int salt) { int[] open = OpenRegions(); return open[(int)(Rng.Hash(seed, salt) % (uint)open.Length)]; }

        // ------------------------------------------------------------------ estrelas e armas

        /// <summary>Melhor resultado (0-3 estrelas) do contrato i da campanha.</summary>
        public int StarsOf(int i) => i >= 0 && i < best.Length ? best[i] : 0;
        /// <summary>Soma das estrelas da campanha: a "reputacao" mostrada na UI; destrava armas, regioes, modificadores e marcos.</summary>
        public int Stars { get { int s = 0; foreach (int b in best) s += b; return s; } }

        /// <summary>SO teste/fotos (-testsession -weapon W): preenche as estrelas em ordem ate a soma dar n (no maximo 3 por contrato), so na
        /// memoria: sem evento e sem Dirty. Nao use no jogo normal.</summary>
        public void DevStars(int n)
        {
            for (int i = 0; i < best.Length; i++) { int k = Math.Max(0, Math.Min(3, n)); best[i] = k; n -= k; }
        }

        /// <summary>SO teste/fotos (-testsession -room B2): comeca o contrato `index` da campanha (index &lt; 0 = o 1o que passa pela sala) direto
        /// na sala de codigo `room` ("B2", "M4", "R3-5"; sem diferenca de maiuscula), na camada dela, sem resgate e sem pedir o contrato
        /// destravado. As perks das camadas anteriores nao sao oferecidas. null = sala fora do contrato, contrato em curso ou save recusado.</summary>
        public Run DevStartAt(int index, string room)
        {
            if (InContract || SaveRefused || string.IsNullOrEmpty(room) || index >= Contracts.Length) return null;
            for (int i = Math.Max(0, index); i < (index < 0 ? Contracts.Length : index + 1); i++)
            {
                ContractDef def = Contracts[i];
                for (int l = 0; l < def.Layers.Length; l++)
                    foreach (int id in def.Layers[l])
                    {
                        if (!string.Equals(Content.Rooms[id].Code, room.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                        Run r = Begin(def, false, Modifier.None);
                        r.Layer = l;
                        r.EnterRoom(id, false);
                        Checkpoint();
                        return r;
                    }
            }
            return null;
        }

        public bool WeaponUnlocked(Weapon w) => (int)w >= 0 && (int)w < WeaponDef.All.Length && Stars >= WeaponDef.All[(int)w].Stars;
        /// <summary>0 = bloqueada; 1-3.</summary>
        public int WeaponLevel(Weapon w) => WeaponUnlocked(w) ? weaponLv[(int)w] : 0;
        /// <summary>Preco do proximo nivel (-1 = bloqueada ou no maximo).</summary>
        public int WeaponCost(Weapon w) { int lv = WeaponLevel(w); return lv <= 0 || lv >= WeaponDef.MaxLevel ? -1 : WeaponDef.All[(int)w].Cost[lv]; }
        public bool CanUpgradeWeapon(Weapon w) { int c = WeaponCost(w); return GuildOpen && !InContract && !SaveRefused && c >= 0 && Coins >= c; }

        public bool UpgradeWeapon(Weapon w)
        {
            if (!CanUpgradeWeapon(w)) return false;
            int cost = WeaponCost(w);
            Coins -= cost;
            weaponLv[(int)w]++;
            Emit(Ev.WeaponUpgrade, (int)w, weaponLv[(int)w], V2.Zero);
            Emit(Ev.CurrencySink, cost, 10 + (int)w, V2.Zero);
            Dirty = true;
            return true;
        }

        /// <summary>Equipa a arma (so fora do contrato e destravada). Emite WeaponEquip (diario weapon_equip).</summary>
        public bool Equip(Weapon w)
        {
            if (InContract || SaveRefused || !WeaponUnlocked(w)) return false;
            Equipped = w;
            Emit(Ev.WeaponEquip, (int)w, WeaponLevel(w), V2.Zero);
            Dirty = true;
            return true;
        }

        internal void Emit(Ev k, int a, int b, V2 pos) => Events.Add(new SimEvent(k, a, b, pos));
        static int Round(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);

        /// <summary>Credita moedas com a Bolsa, arredondando uma vez so no fim; devolve o valor creditado.</summary>
        internal int Credit(double coins)
        {
            int v = Round(coins * Balance.Pct(CoinMul) / 100.0);
            Coins += v;
            return v;
        }

        // ------------------------------------------------------------------ contrato do dia e por codigo

        /// <summary>AAAAMMDD plausivel (ano 2000-2999, mes 1-12, dia 1-31).</summary>
        public static bool IsDate(int d) => d / 10000 >= 2000 && d / 10000 <= 2999 && d / 100 % 100 >= 1 && d / 100 % 100 <= 12 && d % 100 >= 1 && d % 100 <= 31;
        public bool DailyOpen => Reputation >= Balance.OpenAfter;
        public bool CodeOpen => Reputation >= Balance.OpenAfter;

        /// <summary>Chame ao abrir o menu: guarda a maior data vista (voltar o relogio nao libera o dia de novo). Vira a temporada.</summary>
        public void SeeDate(int date)
        {
            if (SaveRefused || !IsDate(date) || date <= LastDateSeen) return;
            LastDateSeen = date;
            SeasonSync();
            Dirty = true;
        }

        /// <summary>O contrato do dia pode ser jogado nesta data? Falso se ja foi entregue nessa data (ou numa posterior) ou se a data e
        /// anterior a maior data ja vista (relogio voltou).</summary>
        public bool DailyAvailable(int date) => DailyOpen && !SaveRefused && IsDate(date) && date >= LastDateSeen && date > DailyDone;

        public static int DailyPayload(int date) => (int)(Rng.Hash(date, 0xDA11) & ContractCode.MaxPayload);
        /// <summary>O contrato do dia da data NA REGIAO 1 (sem reroll): atalho da v1. Com mais regioes destravadas ou reroll, o do jogador e
        /// sim.DailyFor(data) (e o que StartDaily joga).</summary>
        public static ContractDef DailyContract(int date) => Content.Generate(Content.WithRegion(DailyPayload(date), 1), ContractKind.Daily, date);
        /// <summary>O contrato do dia deste jogador: regiao sorteada pela data entre as destravadas; muda com o reroll do dia.</summary>
        public ContractDef DailyFor(int date)
        {
            int n = date == rrDate ? rrDaily : 0;
            int p = (int)(Rng.Hash(date, 0xDA11 + n * 0x9E37) & ContractCode.MaxPayload);
            return Content.Generate(Content.WithRegion(p, PickRegion(date, 0xDA12)), ContractKind.Daily, date);
        }
        /// <summary>Contrato do codigo (6 caracteres; maiusculas/espacos/tracos aceitos) ou null se invalido (ou regiao sem conteudo). A
        /// regiao sai do codigo (RegionOf do payload).</summary>
        public static ContractDef CodeContract(string code) => ContractCode.TryDecode(code, out int p) ? Content.Generate(p, ContractKind.Code) : null;
        /// <summary>Codigo novo para compartilhar, numa regiao destravada (a entropia vem da View).</summary>
        public string NewCode(uint entropy) => ContractCode.Encode(Content.WithRegion((int)(Rng.Hash((int)entropy, 0x5EED) & ContractCode.MaxPayload), PickRegion((int)entropy, 0x5EEE)));

        /// <summary>Contrato do dia (vem com 1 modificador pela data quando os modificadores estao abertos).</summary>
        public Run StartDaily(int date, bool rescue)
        {
            if (InContract || !DailyAvailable(date)) return null;
            SeeDate(date);
            Run r = Begin(DailyFor(date), rescue, ModifiersOpen ? DailyModifier(date) : Modifier.None);
            if (r != null) Emit(Ev.DailyStart, date, 0, Balance.Entrance);
            return r;
        }

        public Run StartCode(string code, bool rescue) => StartCode(code, rescue, Modifier.None);

        /// <summary>Contrato por codigo (null = invalido, fechado ou regiao do codigo ainda bloqueada para este jogador).</summary>
        public Run StartCode(string code, bool rescue, Modifier mods)
        {
            if (InContract || !CodeOpen || SaveRefused || !ModsOk(mods)) return null;
            ContractDef def = CodeContract(code);
            if (def == null || !RegionOpen(def.Region)) return null;
            Run r = Begin(def, rescue, mods);
            if (r != null) Emit(Ev.CodeStart, def.Payload, 0, Balance.Entrance);
            return r;
        }

        /// <summary>Escolhe a perk i (0..2) da oferta aberta (sim.Run.PerkOffer). Falso se nao ha oferta.</summary>
        public bool PickPerk(int i) => InContract && Run.Pick(i);

        // ------------------------------------------------------------------ contrato

        /// <summary>Comeca o contrato i (precisa estar desbloqueado e nenhum outro em curso). rescue = aceitar a carga perdida pendente:
        /// ela aparece no chao da 1ª sala. A oferta expira aqui, aceita ou nao (so num contrato da mesma regiao da carga).</summary>
        public Run StartContract(int index, bool rescue) => StartContract(index, rescue, Modifier.None);

        /// <summary>Com modificadores (0-2, depois de Balance.ModifiersStars estrelas; null se invalido).</summary>
        public Run StartContract(int index, bool rescue, Modifier mods)
        {
            if (!Unlocked(index) || !ModsOk(mods)) return null;
            return Begin(Contracts[index], rescue, mods);
        }

        /// <summary>Medicao do bot (H2): comeca um contrato montado na hora, sem resgate.</summary>
        internal Run StartDef(ContractDef def) => Begin(def, false, Modifier.None);

        Run Begin(ContractDef def, bool rescue, Modifier mods)
        {
            if (SaveRefused || InContract || def == null) return null;
            var run = new Run(this, def, (int)Rng.Hash(Seed, RunsStarted), MaxHp) { Id = RunsStarted, Mods = mods };
            RunsStarted++;
            run.AddCargo(NextCargoId++, def.Cargo, 0, false, CargoState.Held);
            LostCargo lost = Pending;
            if (lost != null && (def.Kind == ContractKind.Endless || def.Kind == ContractKind.Event || lost.Region != def.Region)) lost = null;   // resgate so na mesma regiao e fora de evento/sem fim (fica pendente)
            else Pending = null;
            bool take = rescue && lost != null && !RescuedIds.Contains(lost.Id);
            run.RescueOffered = lost != null; run.RescueAccepted = take; run.RescueValue = take ? lost.Value : 0;
            if (take) run.AddCargo(lost.Id, lost.Type, lost.Cracks, true, CargoState.Ground);
            Run = run; LastResult = null;
            Emit(Ev.ContractStart, def.Index, take ? 1 : 0, Balance.Entrance);
            if (lost != null) Emit(Ev.CargoRescueOffer, lost.Id, take ? 1 : 0, Balance.Entrance);
            if (mods != Modifier.None) Emit(Ev.ModifierPick, (int)mods, def.Index, Balance.Entrance);
            if (def.Event != LiveEvent.None) Emit(Ev.EventStart, (int)def.Event, def.Slot, Balance.Entrance);
            run.EnterRoom(def.Layers[0][0], false);
            if (take) run.RescueCargo.Pos = run.Room.RescueSpot;
            Dirty = true;
            Checkpoint();
            return run;
        }

        public void Tick(float dt, V2 move, bool dash)
        {
            Events.Clear();
            if (!InContract) return;
            Run.Tick(dt, move, dash);
            if (!Run.Running) Settle();
        }

        /// <summary>Desiste do contrato em curso: falha sem carga pendente.</summary>
        public void Abandon()
        {
            if (!InContract) return;
            Run.Finish(Outcome.Abandoned);
            Settle();
        }

        /// <summary>Multiplicador extra do pagamento (%): 100 + modificadores + 2% por nivel de maestria da carga; x evento (Fragile Week 150%,
        /// No-Damage 300%); x2 no raro.</summary>
        int PayPct(Run r)
        {
            ContractDef def = r.Contract;
            int pct = 100 + ModifierDef.PayOf(r.Mods) + Balance.MasteryPct * MasteryLevel(def.Cargo);
            pct = pct * EventDef.All[(int)def.Event].PayPct / 100;
            if (def.Kind == ContractKind.Rare) pct = pct * Balance.RarePct / 100;
            return pct;
        }

        void Settle()
        {
            Run r = Run; ContractDef def = r.Contract; Cargo main = r.Main;
            bool delivered = r.Outcome == Outcome.Delivered;
            var res = new ContractResult
            {
                Contract = def.Index, Outcome = r.Outcome, Time = r.Time, Cracks = main.Cracks, Hatched = r.Hatched, Base = def.Pay,
                CargoMul = r.Hatched ? CargoDef.All[(int)def.Cargo].HatchedMul : CargoDef.All[(int)def.Cargo].PayMul,
                IntegrityMul = Balance.IntegrityMul(main.Cracks), BagMul = CoinMul, ChestCoins = r.ChestCoins,
                RescueOffered = r.RescueOffered, RescueAccepted = r.RescueAccepted, DeathRoom = r.DeathRoom,
                RoomsNormal = r.RoomsNormal, RoomsFought = r.RoomsFought,
                Def = def, Kind = def.Kind, ShareCode = def.ShareCode, Perks = r.Perks.ToArray(), Weapon = r.Weapon,
                StarDelivered = delivered, StarIntact = delivered && main.Cracks == 0 && !r.Hatched,
                StarTime = delivered && r.ParTime > 0f && r.Time <= r.ParTime + 1e-3f,
                RunId = r.Id, Mods = r.Mods, PayPct = PayPct(r), HuntCoins = r.HuntCoins,
            };
            res.Stars = (res.StarDelivered ? 1 : 0) + (res.StarIntact ? 1 : 0) + (res.StarTime ? 1 : 0);
            if (delivered)
            {
                int cargoPct = Balance.Pct(res.CargoMul);
                res.Payment = Credit((long)def.Pay * cargoPct * Balance.Pct(res.IntegrityMul) * res.PayPct / 1000000.0);
                res.Bonus = Math.Max(0, res.Payment - Round(def.Pay * cargoPct / 100.0 * Balance.Pct(CoinMul) / 100.0));
                Emit(Ev.CurrencySource, res.Payment, 0, Balance.Client);
                Cargo rc = r.RescueCargo;
                if (rc != null && rc.State == CargoState.Held && !RescuedIds.Contains(rc.Id))
                {
                    res.RescueDelivered = true;
                    res.RescueBonus = Credit(r.RescueValue * Balance.Pct(Balance.RescueBonus) / 100.0);
                    RescuedIds.Add(rc.Id);
                    Emit(Ev.CurrencySource, res.RescueBonus, 2, Balance.Client);
                }
                if (def.Kind == ContractKind.Campaign && def.Index == Reputation) Reputation++;
                if (def.Kind == ContractKind.Daily && def.Date > DailyDone)   // bonus do dia: 1 vez por data
                {
                    DailyDone = def.Date;
                    res.DailyBonus = Credit(Balance.DailyBonus);
                    res.Bonus += res.DailyBonus;
                    Emit(Ev.CurrencySource, res.DailyBonus, 3, Balance.Client);
                }
                SettleDelivered(res, r);
            }
            else if ((r.Outcome == Outcome.Died || r.Outcome == Outcome.CargoStolen) && def.Kind != ContractKind.Endless)
            {
                // a carga do contrato fica na masmorra (so 1 pendente: a mais nova substitui)
                Pending = new LostCargo { Id = main.Id, Type = main.Type, Cracks = main.Cracks, Room = r.Room.Id, Contract = def.Index, Value = Round(def.Pay * Balance.Pct(CargoDef.All[(int)main.Type].PayMul) / 100.0) };
            }
            if (def.Kind == ContractKind.Campaign && def.Index >= 0 && def.Index < best.Length && res.Stars > best[def.Index])
            {
                var was = new bool[WeaponDef.All.Length];
                for (int w = 0; w < was.Length; w++) was[w] = WeaponUnlocked((Weapon)w);
                best[def.Index] = res.Stars; res.NewBest = true;
                for (int w = 0; w < was.Length; w++) if (!was[w] && WeaponUnlocked((Weapon)w)) Emit(Ev.WeaponUnlock, w, Stars, Balance.Client);
            }
            SettleMeta(res, r);
            res.Total = res.Payment + res.RescueBonus + res.ChestCoins + res.DailyBonus + res.EventReward + res.HuntCoins + res.EndlessCoins;
            Emit(Ev.ContractStars, res.Stars, def.Index, r.Courier.Pos);
            Emit(Ev.ContractEnd, def.Index, (int)r.Outcome, r.Courier.Pos);
            if (def.Kind == ContractKind.Daily) Emit(Ev.DailyEnd, def.Date, (int)r.Outcome, r.Courier.Pos);
            if (def.Event != LiveEvent.None) Emit(Ev.EventEnd, (int)def.Event, (int)r.Outcome, r.Courier.Pos);
            if (r.RescueAccepted) Emit(Ev.CargoRescueResult, r.RescueCargo.Id, res.RescueDelivered ? 1 : 0, r.Courier.Pos);
            LastResult = res;
            Dirty = true;
        }

        // ------------------------------------------------------------------ save (chave=valor; lixo vira estado inicial)

        /// <summary>Texto do save (v=3). null se SaveRefused (nao grave por cima de um save de versao futura). Limpa Dirty.</summary>
        public string Save()
        {
            if (SaveRefused) return null;
            // Append(int) usa a cultura do aparelho: sv/nb/fi escrevem o menos como U+2212 (ar/fa com marca de direcao) e o Load le invariante
            CultureInfo cur = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            try { return SaveText(); }
            finally { CultureInfo.CurrentCulture = cur; }
        }

        string SaveText()
        {
            var sb = new StringBuilder();
            sb.Append("v=").Append(SaveVersion).Append('\n');
            sb.Append("seed=").Append(Seed).Append('\n');
            sb.Append("runs=").Append(RunsStarted).Append('\n');
            sb.Append("nid=").Append(NextCargoId).Append('\n');
            sb.Append("coins=").Append(Coins).Append('\n');
            sb.Append("rep=").Append(Reputation).Append('\n');
            sb.Append("lv=").Append(string.Join(",", Levels)).Append('\n');
            sb.Append("stars=");
            foreach (int b in best) sb.Append((char)('0' + b));
            sb.Append('\n');
            sb.Append("wpn=").Append((int)Equipped).Append('\n');
            sb.Append("wlv=").Append(string.Join(",", weaponLv)).Append('\n');
            sb.Append("dseen=").Append(LastDateSeen).Append('\n');
            sb.Append("ddone=").Append(DailyDone).Append('\n');
            if (Pending != null)
                sb.Append("pend=").Append(Pending.Id).Append(',').Append((int)Pending.Type).Append(',').Append(Pending.Cracks).Append(',')
                  .Append(Pending.Room).Append(',').Append(Pending.Value).Append(',').Append(Pending.Contract).Append('\n');
            if (RescuedIds.Count > 0)
            {
                var ids = new List<int>(RescuedIds);
                ids.Sort();
                sb.Append("resc=").Append(string.Join(",", ids)).Append('\n');
            }
            SaveMeta(sb);
            if (InContract) WriteRun(sb, Run);
            Dirty = false;
            return sb.ToString();
        }

        /// <summary>Linhas do contrato em curso (run, rk, rx, cgN): o save e o ponto de retomada do rewarded (Checkpoint). cargo = grava o estado
        /// exato da carga (gelo, lanterna apagada); o Checkpoint nao grava: retomar pelo rewarded volta com o gelo no topo da faixa e a lanterna acesa.</summary>
        static void WriteRun(StringBuilder sb, Run r, bool cargo = true)
        {
            var ci = CultureInfo.InvariantCulture;
            sb.Append("run=").Append(r.Contract.Index).Append(',').Append(r.Layer).Append(',').Append(r.Room.Id).Append(',').Append(r.Courier.Hp).Append(',')
              .Append(r.Time.ToString("0.###", ci)).Append(',').Append(r.Seed).Append(',').Append(r.Chest != null && r.Chest.Opened ? 1 : 0).Append(',')
              .Append(r.Hatched ? 1 : 0).Append(',').Append(r.Pet != null ? r.Pet.Hp : 0).Append(',').Append(r.RescueValue).Append(',')
              .Append(r.RoomsNormal).Append(',').Append(r.RoomsFought).Append(',').Append(r.ChestCoins).Append(',').Append(r.RescueOffered ? 1 : 0).Append('\n');
            int mask = 0;
            foreach (Perk p in r.Perks) mask |= 1 << (int)p;
            Perk[] off = r.PerkOffer;
            // rk = tipo, payload, data, perks (bits), acolchoada usada, oferta aberta (3 perks ou -1)
            sb.Append("rk=").Append((int)r.Contract.Kind).Append(',').Append(r.Contract.Payload).Append(',').Append(r.Contract.Date).Append(',')
              .Append(mask).Append(',').Append(r.PadUsed ? 1 : 0);
            for (int i = 0; i < Balance.PerkOfferSize; i++) sb.Append(',').Append(off != null && i < off.Length ? (int)off[i] : -1);
            sb.Append('\n');
            int buried = 0;
            for (int i = 0; i < r.Buried.Count && i < 30; i++) if (r.Buried[i].Opened) buried |= 1 << i;
            // rx (v3) = id da tentativa, modificadores, vaga do evento, segundo folego, fumacas restantes, inimigos, baus, secretas, chefes sem dano,
            // Cacador, baus enterrados abertos na sala (bits), regiao do contrato (dia/codigo da v2 migrado: regiao 1 com bits altos no payload)
            sb.Append("rx=").Append(r.Id).Append(',').Append((int)r.Mods).Append(',').Append(r.Contract.Slot).Append(',').Append(r.SecondWindUsed ? 1 : 0).Append(',')
              .Append(r.SmokeLeft).Append(',').Append(r.Kills).Append(',').Append(r.ChestsOpened).Append(',').Append(r.SecretsFound).Append(',')
              .Append(r.BossesClean).Append(',').Append(r.HuntCoins).Append(',').Append(buried).Append(',').Append(r.Contract.Region).Append('\n');
            for (int i = 0; i < r.Cargos.Count; i++)
            {
                Cargo g = r.Cargos[i];
                // ao retomar: 0 = nas maos, 1 = no chao no ponto de resgate (resgate ainda nao pego na 1ª sala), 2 = fora do jogo
                int st = g.State == CargoState.Gone ? 2 : g.Rescue && r.Layer == 0 && g.State == CargoState.Ground ? 1 : 0;
                sb.Append("cg").Append(i).Append('=').Append(g.Id).Append(',').Append((int)g.Type).Append(',').Append(g.Cracks).Append(',')
                  .Append(g.Rescue ? 1 : 0).Append(',').Append(st);
                // 6o campo (GDD §28: reabrir nao restaura a carga): gelo exato do cristal (0-100) ou 1 = lanterna apagada
                if (cargo && g.Type == CargoType.IceCrystal) sb.Append(',').Append(r.IceOf(g).ToString("G9", ci));
                else if (cargo && g.Type == CargoType.Lantern && g.Out) sb.Append(",1");
                sb.Append('\n');
            }
        }

        /// <summary>Le o save. Migracoes explicitas (Dirty = true para regravar em v3): v0 (sem linha v: so coins, lv, rep) · v1 (sem
        /// estrelas/armas/Mochila: estrela 1 em cada contrato concluido; contrato em curso descartado porque os contratos da v1.0 mudaram) ·
        /// v2 (sem os sistemas do lancamento: tudo no padrao; contrato em curso mantido, dia/codigo gerados na regiao 1 como na beta).
        /// Versao futura = estado novo com SaveRefused (o texto original nao e tocado). Valores fora da faixa sao corrigidos; contrato em
        /// curso invalido e descartado.</summary>
        public static Sim Load(string text)
        {
            var sim = new Sim();
            if (string.IsNullOrEmpty(text)) return sim;
            Dictionary<string, string> kv = Parse(text);
            int v = kv.TryGetValue("v", out string vs) ? I(vs, 0) : 0;
            if (v > SaveVersion) { sim.SaveRefused = true; return sim; }
            if (v < SaveVersion) sim.Dirty = true;

            sim.Coins = Math.Max(0, I(Get(kv, "coins"), 0));
            sim.Reputation = Clamp(I(Get(kv, "rep"), 0), 0, Contracts.Length);
            string[] lv = Get(kv, "lv").Split(',');
            for (int i = 0; i < Math.Min(lv.Length, sim.Levels.Length); i++) sim.Levels[i] = Clamp(I(lv[i], 0), 0, Balance.UpgradeMax);
            if (v < 2)
                for (int i = 0; i < sim.Reputation && i < sim.best.Length; i++) sim.best[i] = 1;   // migracao: concluido = 1 estrela
            if (v <= 0) return sim;   // migracao v0 -> v3: v0 nao tinha contrato em curso nem carga pendente

            sim.Seed = I(Get(kv, "seed"), 1);
            sim.RunsStarted = Math.Max(0, I(Get(kv, "runs"), 0));
            sim.NextCargoId = Math.Max(1, I(Get(kv, "nid"), 1));
            foreach (string s in Get(kv, "resc").Split(',')) { int id = I(s, 0); if (id > 0) sim.RescuedIds.Add(id); }
            string[] p = Get(kv, "pend").Split(',');
            if (p.Length >= 6)
            {
                int id = I(p[0], 0), type = I(p[1], -1);
                if (id > 0 && type >= 0 && type < CargoDef.All.Length)
                    sim.Pending = new LostCargo { Id = id, Type = (CargoType)type, Cracks = Clamp(I(p[2], 0), 0, Balance.RelicBreak - 1), Room = Clamp(I(p[3], 0), 0, Content.Rooms.Length - 1), Value = Math.Max(0, I(p[4], 0)), Contract = I(p[5], 0) };
            }
            if (v == 1) return sim;   // migracao v1 -> v3: o contrato em curso da v1 e descartado (contratos mudaram)

            string st = Get(kv, "stars");
            for (int i = 0; i < Math.Min(st.Length, sim.best.Length); i++) sim.best[i] = st[i] >= '0' && st[i] <= '3' ? st[i] - '0' : 0;
            string[] wl = Get(kv, "wlv").Split(',');
            for (int i = 0; i < Math.Min(wl.Length, sim.weaponLv.Length); i++) sim.weaponLv[i] = Clamp(I(wl[i], 1), 1, WeaponDef.MaxLevel);
            int wp = I(Get(kv, "wpn"), 0);
            sim.Equipped = wp >= 0 && wp < WeaponDef.All.Length && sim.WeaponUnlocked((Weapon)wp) ? (Weapon)wp : Weapon.Sling;
            int ds = I(Get(kv, "dseen"), 0), dd = I(Get(kv, "ddone"), 0);
            sim.LastDateSeen = IsDate(ds) ? ds : 0;
            sim.DailyDone = IsDate(dd) ? dd : 0;
            if (v >= 3) sim.LoadMeta(kv);   // v2 -> v3: sistemas do lancamento no padrao
            else sim.SeasonSync();
            sim.Run = LoadRun(sim, kv, v);
            if (sim.Run != null) sim.Checkpoint();
            return sim;
        }

        static Dictionary<string, string> Parse(string text)
        {
            var kv = new Dictionary<string, string>();
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq > 0) kv[raw.Substring(0, eq).Trim()] = raw.Substring(eq + 1).Trim();
            }
            return kv;
        }

        /// <summary>Contrato em curso a partir das linhas run/rk/rx/cgN (null = invalido). v = versao do save (v2: gerados na regiao 1).</summary>
        static Run LoadRun(Sim sim, Dictionary<string, string> kv, int v)
        {
            string[] p = Get(kv, "run").Split(',');
            string[] k = Get(kv, "rk").Split(',');
            string[] x = Get(kv, "rx").Split(',');
            if (p.Length < 14 || k.Length < 5 + Balance.PerkOfferSize) return null;
            int ci = I(p[0], -2), layer = I(p[1], -1), room = I(p[2], -1), hp = I(p[3], 0), kind = I(k[0], -1), payload = I(k[1], 0), date = I(k[2], 0);
            if (hp <= 0) return null;
            // v2: dia/codigo da beta eram sempre da regiao 1; v3 grava a regiao no rx (o migrado continua na 1 depois de regravar)
            int old = v < 3 ? 1 : x.Length >= 12 ? I(x[11], 0) : 0;
            ContractDef def;
            switch ((ContractKind)kind)
            {
                case ContractKind.Campaign: if (ci < 0 || ci >= Contracts.Length) return null; def = Contracts[ci]; break;
                case ContractKind.Daily: if (!IsDate(date)) return null; def = Content.Generate(payload, ContractKind.Daily, date, old); break;
                case ContractKind.Code: def = Content.Generate(payload, ContractKind.Code, 0, old); break;
                case ContractKind.Rare: if (!IsDate(date)) return null; def = Content.Generate(payload, ContractKind.Rare, date); break;
                case ContractKind.Event:
                    if (!IsDate(date)) return null;
                    int slot = Clamp(x.Length > 2 ? I(x[2], 0) : 0, 0, Balance.EventRewards.Length - 1);
                    def = Content.EventContract(EventOf(date), payload, date, sim.BeatenBosses(), EventLayers(slot));
                    if (def != null) def.Slot = slot;
                    break;
                case ContractKind.Endless: def = Content.Endless(date, sim.OpenRegions(), sim.Beaten(true), sim.Beaten(false)); break;
                default: return null;
            }
            if (def == null || layer < 0 || layer >= def.Layers.Length || room < 0 || room >= Content.Rooms.Length) return null;
            bool inLayer = Array.IndexOf(def.Layers[layer], room) >= 0;
            if (!inLayer && Content.Rooms[room].IsSecret)   // sala secreta: vale se alguma sala da camada tem a parede rachada para ela
                foreach (int r0 in def.Layers[layer]) if (Content.Rooms[r0].Secret == room) inLayer = true;
            if (!inLayer) return null;
            bool rx = x.Length >= 11;
            int mods = rx ? I(x[1], 0) & 31 : 0;
            var run = new Run(sim, def, I(p[5], 1), hp)
            {
                Layer = layer, Time = Math.Max(0f, F(p[4], 0f)), Hatched = p[7] == "1" && def.Cargo == CargoType.Egg,
                RescueValue = Math.Max(0, I(p[9], 0)), RoomsNormal = Math.Max(0, I(p[10], 0)), RoomsFought = Math.Max(0, I(p[11], 0)),
                ChestCoins = Math.Max(0, I(p[12], 0)), RescueOffered = p[13] == "1", PadUsed = k[4] == "1",
                Id = rx ? Math.Max(0, I(x[0], 0)) : Math.Max(0, sim.RunsStarted - 1), Mods = (Modifier)mods,
            };
            if (rx)
            {
                run.SecondWindUsed = x[3] == "1"; run.SmokeLeft = Clamp(I(x[4], 0), 0, run.SmokeLeft);
                run.Kills = Math.Max(0, I(x[5], 0)); run.ChestsOpened = Math.Max(0, I(x[6], 0)); run.SecretsFound = Math.Max(0, I(x[7], 0));
                run.BossesClean = Math.Max(0, I(x[8], 0)); run.HuntCoins = Math.Max(0, I(x[9], 0));
            }
            int mask = I(k[3], 0);
            for (int i = 0; i < PerkDef.All.Length; i++) if ((mask & (1 << i)) != 0) run.Apply((Perk)i, false);
            run.Courier.Hp = Clamp(hp, 1, run.Courier.MaxHp);
            for (int i = 0; i < 2; i++)
            {
                string[] g = Get(kv, "cg" + i).Split(',');
                if (g.Length < 5) break;
                int type = I(g[1], -1), st = I(g[4], 2);
                if (type < 0 || type >= CargoDef.All.Length) return null;
                bool rescue = i == 1 && g[3] == "1";
                if (i == 0 && (CargoType)type != def.Cargo) return null;
                CargoState state = st == 0 ? CargoState.Held : st == 1 && rescue && layer == 0 ? CargoState.Ground : CargoState.Gone;
                if (i == 0 && state != CargoState.Held && !run.Hatched) return null;   // carga do contrato fora das maos so se chocou
                Cargo c = run.AddCargo(Math.Max(1, I(g[0], 1)), (CargoType)type, Math.Max(0, I(g[2], 0)), rescue, state);
                if (g.Length > 5 && c.Type == CargoType.IceCrystal) { float ice = F(g[5], -1f); if (ice >= 0f) run.SetIce(c, ice); }   // sem o campo: topo da faixa
                if (g.Length > 5 && c.Type == CargoType.Lantern) c.Out = g[5] == "1";
                if (i == 0 && run.Hatched) c.State = CargoState.Gone;
                if (run.Breaks(c)) return null;
            }
            if (run.Cargos.Count == 0) return null;
            run.RescueAccepted = run.RescueCargo != null;
            if (run.Hatched) run.Pet = new Pet { Hp = Clamp(I(p[8], Balance.PetHp), 1, Balance.PetHp) };
            run.EnterRoom(room, p[6] == "1", rx ? Math.Max(0, I(x[10], 0)) : 0);
            if (run.RescueCargo != null && run.RescueCargo.State == CargoState.Ground) run.RescueCargo.Pos = run.Room.RescueSpot;
            var offer = new List<Perk>();
            for (int i = 0; i < Balance.PerkOfferSize; i++) { int q = I(k[5 + i], -1); if (q >= 0 && q < PerkDef.All.Length && !run.Has((Perk)q)) offer.Add((Perk)q); }
            if (offer.Count > 0) run.Reoffer(offer.ToArray());
            return run;
        }

        static string Get(Dictionary<string, string> kv, string k) => kv.TryGetValue(k, out string s) ? s : "";
        static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));
        static int I(string s, int d) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        static long L(string s, long d) => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : d;
        static float F(string s, float d) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : d;
    }
}
