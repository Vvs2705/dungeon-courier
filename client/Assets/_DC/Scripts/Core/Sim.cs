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
//               contratos mudaram) e v0. v3+ = SaveRefused como antes.
//   Bot:        bot.Act(sim) ja escolhe perk quando ha oferta (Bot.ChoosePerk). Bot.WeaponFor(sim, def) = arma que a campanha leva.
//               Bot.PlayCampaign joga os 24 (compra guilda e armas, troca a arma pela carga). Bot.Finish(sim, bot) joga um contrato ja
//               comecado (dia/codigo). Medicao: Bot.MeasureCampaignTable, Bot.MeasureRoute (H2), Bot.MeasureGenerated.

namespace DC.Core
{
    /// <summary>Carga perdida (morte ou ladrao que escapou), pendente para resgate no proximo contrato. Value = pagamento base dela
    /// (Pay x multiplicador da carga): o bonus do resgate e Balance.RescueBonus disso. Contract = -1 se veio de contrato gerado.</summary>
    public sealed class LostCargo
    {
        public int Id; public CargoType Type; public int Cracks, Room, Value, Contract;
    }

    /// <summary>Resultado de um contrato, com o pagamento detalhado (Total = Payment + RescueBonus + ChestCoins + DailyBonus).</summary>
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
    }

    public sealed class Sim
    {
        public const int SaveVersion = 2;
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
        public bool Unlocked(int i) => i >= 0 && i < Contracts.Length && i <= Reputation;
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

        // ------------------------------------------------------------------ estrelas e armas

        /// <summary>Melhor resultado (0-3 estrelas) do contrato i da campanha.</summary>
        public int StarsOf(int i) => i >= 0 && i < best.Length ? best[i] : 0;
        /// <summary>Soma das estrelas da campanha: a "reputacao" mostrada na UI; destrava armas.</summary>
        public int Stars { get { int s = 0; foreach (int b in best) s += b; return s; } }

        /// <summary>SO teste/fotos (-testsession -weapon W): preenche as estrelas em ordem ate a soma dar n (no maximo 3 por contrato), so na
        /// memoria: sem evento e sem Dirty. Nao use no jogo normal.</summary>
        public void DevStars(int n)
        {
            for (int i = 0; i < best.Length; i++) { int k = Math.Max(0, Math.Min(3, n)); best[i] = k; n -= k; }
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

        /// <summary>Chame ao abrir o menu: guarda a maior data vista (voltar o relogio nao libera o dia de novo).</summary>
        public void SeeDate(int date)
        {
            if (SaveRefused || !IsDate(date) || date <= LastDateSeen) return;
            LastDateSeen = date;
            Dirty = true;
        }

        /// <summary>O contrato do dia pode ser jogado nesta data? Falso se ja foi entregue nessa data (ou numa posterior) ou se a data e
        /// anterior a maior data ja vista (relogio voltou).</summary>
        public bool DailyAvailable(int date) => DailyOpen && !SaveRefused && IsDate(date) && date >= LastDateSeen && date > DailyDone;

        public static int DailyPayload(int date) => (int)(Rng.Hash(date, 0xDA11) & ContractCode.MaxPayload);
        /// <summary>O contrato do dia da data (o mesmo para todos os jogadores).</summary>
        public static ContractDef DailyContract(int date) => Content.Generate(DailyPayload(date), ContractKind.Daily, date);
        /// <summary>Contrato do codigo (6 caracteres; maiusculas/espacos/tracos aceitos) ou null se invalido.</summary>
        public static ContractDef CodeContract(string code) => ContractCode.TryDecode(code, out int p) ? Content.Generate(p, ContractKind.Code) : null;

        public Run StartDaily(int date, bool rescue)
        {
            if (InContract || !DailyAvailable(date)) return null;
            SeeDate(date);
            Run r = Begin(DailyContract(date), rescue);
            if (r != null) Emit(Ev.DailyStart, date, 0, Balance.Entrance);
            return r;
        }

        public Run StartCode(string code, bool rescue)
        {
            if (InContract || !CodeOpen || SaveRefused) return null;
            ContractDef def = CodeContract(code);
            if (def == null) return null;
            Run r = Begin(def, rescue);
            if (r != null) Emit(Ev.CodeStart, def.Payload, 0, Balance.Entrance);
            return r;
        }

        /// <summary>Escolhe a perk i (0..2) da oferta aberta (sim.Run.PerkOffer). Falso se nao ha oferta.</summary>
        public bool PickPerk(int i) => InContract && Run.Pick(i);

        // ------------------------------------------------------------------ contrato

        /// <summary>Comeca o contrato i (precisa estar desbloqueado e nenhum outro em curso). rescue = aceitar a carga perdida pendente:
        /// ela aparece no chao da 1ª sala. A oferta expira aqui, aceita ou nao.</summary>
        public Run StartContract(int index, bool rescue)
        {
            if (!Unlocked(index)) return null;
            return Begin(Contracts[index], rescue);
        }

        /// <summary>Medicao do bot (H2): comeca um contrato montado na hora, sem resgate.</summary>
        internal Run StartDef(ContractDef def) => Begin(def, false);

        Run Begin(ContractDef def, bool rescue)
        {
            if (SaveRefused || InContract) return null;
            var run = new Run(this, def, (int)Rng.Hash(Seed, RunsStarted), MaxHp);
            RunsStarted++;
            run.AddCargo(NextCargoId++, def.Cargo, 0, false, CargoState.Held);
            LostCargo lost = Pending;
            Pending = null;
            bool take = rescue && lost != null && !RescuedIds.Contains(lost.Id);
            run.RescueOffered = lost != null; run.RescueAccepted = take; run.RescueValue = take ? lost.Value : 0;
            if (take) run.AddCargo(lost.Id, lost.Type, lost.Cracks, true, CargoState.Ground);
            Run = run; LastResult = null;
            Emit(Ev.ContractStart, def.Index, take ? 1 : 0, Balance.Entrance);
            if (lost != null) Emit(Ev.CargoRescueOffer, lost.Id, take ? 1 : 0, Balance.Entrance);
            run.EnterRoom(def.Layers[0][0], false);
            if (take) run.RescueCargo.Pos = run.Room.RescueSpot;
            Dirty = true;
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
                StarTime = delivered && def.ParTime > 0f && r.Time <= def.ParTime + 1e-3f,
            };
            res.Stars = (res.StarDelivered ? 1 : 0) + (res.StarIntact ? 1 : 0) + (res.StarTime ? 1 : 0);
            if (delivered)
            {
                res.Payment = Credit(def.Pay * Balance.Pct(res.CargoMul) * Balance.Pct(res.IntegrityMul) / 10000.0);
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
                    Emit(Ev.CurrencySource, res.DailyBonus, 3, Balance.Client);
                }
            }
            else if (r.Outcome == Outcome.Died || r.Outcome == Outcome.CargoStolen)
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
            res.Total = res.Payment + res.RescueBonus + res.ChestCoins + res.DailyBonus;
            Emit(Ev.ContractStars, res.Stars, def.Index, r.Courier.Pos);
            Emit(Ev.ContractEnd, def.Index, (int)r.Outcome, r.Courier.Pos);
            if (def.Kind == ContractKind.Daily) Emit(Ev.DailyEnd, def.Date, (int)r.Outcome, r.Courier.Pos);
            if (r.RescueAccepted) Emit(Ev.CargoRescueResult, r.RescueCargo.Id, res.RescueDelivered ? 1 : 0, r.Courier.Pos);
            LastResult = res;
            Dirty = true;
        }

        // ------------------------------------------------------------------ save (chave=valor; lixo vira estado inicial)

        /// <summary>Texto do save (v=2). null se SaveRefused (nao grave por cima de um save de versao futura). Limpa Dirty.</summary>
        public string Save()
        {
            if (SaveRefused) return null;
            var ci = CultureInfo.InvariantCulture;
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
            if (InContract)
            {
                Run r = Run;
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
                for (int i = 0; i < r.Cargos.Count; i++)
                {
                    Cargo g = r.Cargos[i];
                    // ao retomar: 0 = nas maos, 1 = no chao no ponto de resgate (resgate ainda nao pego na 1ª sala), 2 = fora do jogo
                    int st = g.State == CargoState.Gone ? 2 : g.Rescue && r.Layer == 0 && g.State == CargoState.Ground ? 1 : 0;
                    sb.Append("cg").Append(i).Append('=').Append(g.Id).Append(',').Append((int)g.Type).Append(',').Append(g.Cracks).Append(',')
                      .Append(g.Rescue ? 1 : 0).Append(',').Append(st).Append('\n');
                }
            }
            Dirty = false;
            return sb.ToString();
        }

        /// <summary>Le o save. Sem linha v = v0 (so meta: coins, lv, rep); v1 = sem estrelas/armas/Mochila (estrela 1 em cada contrato
        /// concluido; contrato em curso descartado, porque os contratos da v1.0 mudaram). As duas migram para v2 (Dirty = true para
        /// regravar). Versao futura = estado novo com SaveRefused (o texto original nao e tocado). Valores fora da faixa sao corrigidos;
        /// contrato em curso invalido e descartado.</summary>
        public static Sim Load(string text)
        {
            var sim = new Sim();
            if (string.IsNullOrEmpty(text)) return sim;
            var kv = new Dictionary<string, string>();
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq > 0) kv[raw.Substring(0, eq).Trim()] = raw.Substring(eq + 1).Trim();
            }
            int v = kv.TryGetValue("v", out string vs) ? I(vs, 0) : 0;
            if (v > SaveVersion) { sim.SaveRefused = true; return sim; }

            sim.Coins = Math.Max(0, I(Get(kv, "coins"), 0));
            sim.Reputation = Clamp(I(Get(kv, "rep"), 0), 0, Contracts.Length);
            string[] lv = Get(kv, "lv").Split(',');
            for (int i = 0; i < Math.Min(lv.Length, sim.Levels.Length); i++) sim.Levels[i] = Clamp(I(lv[i], 0), 0, Balance.UpgradeMax);
            if (v < 2)
                for (int i = 0; i < sim.Reputation && i < sim.best.Length; i++) sim.best[i] = 1;   // migracao: concluido = 1 estrela
            if (v <= 0) { sim.Dirty = true; return sim; }   // migracao v0 -> v2: v0 nao tinha contrato em curso nem carga pendente

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
            if (v == 1) { sim.Dirty = true; return sim; }   // migracao v1 -> v2: o contrato em curso da v1 e descartado (contratos mudaram)

            string st = Get(kv, "stars");
            for (int i = 0; i < Math.Min(st.Length, sim.best.Length); i++) sim.best[i] = st[i] >= '0' && st[i] <= '3' ? st[i] - '0' : 0;
            string[] wl = Get(kv, "wlv").Split(',');
            for (int i = 0; i < Math.Min(wl.Length, sim.weaponLv.Length); i++) sim.weaponLv[i] = Clamp(I(wl[i], 1), 1, WeaponDef.MaxLevel);
            int wp = I(Get(kv, "wpn"), 0);
            sim.Equipped = wp >= 0 && wp < WeaponDef.All.Length && sim.WeaponUnlocked((Weapon)wp) ? (Weapon)wp : Weapon.Sling;
            int ds = I(Get(kv, "dseen"), 0), dd = I(Get(kv, "ddone"), 0);
            sim.LastDateSeen = IsDate(ds) ? ds : 0;
            sim.DailyDone = IsDate(dd) ? dd : 0;
            LoadRun(sim, kv);
            return sim;
        }

        static void LoadRun(Sim sim, Dictionary<string, string> kv)
        {
            string[] p = Get(kv, "run").Split(',');
            string[] k = Get(kv, "rk").Split(',');
            if (p.Length < 14 || k.Length < 5 + Balance.PerkOfferSize) return;
            int ci = I(p[0], -2), layer = I(p[1], -1), room = I(p[2], -1), hp = I(p[3], 0), kind = I(k[0], -1);
            if (hp <= 0) return;
            ContractDef def;
            if (kind == (int)ContractKind.Campaign) { if (ci < 0 || ci >= Contracts.Length) return; def = Contracts[ci]; }
            else if (kind == (int)ContractKind.Daily) { int date = I(k[2], 0); if (!IsDate(date)) return; def = DailyContract(date); }
            else if (kind == (int)ContractKind.Code) def = Content.Generate(I(k[1], 0), ContractKind.Code);
            else return;
            if (layer < 0 || layer >= def.Layers.Length || room < 0 || room >= Content.Rooms.Length) return;
            bool inLayer = Array.IndexOf(def.Layers[layer], room) >= 0;
            if (!inLayer && Content.Rooms[room].IsSecret)   // sala secreta: vale se alguma sala da camada tem a parede rachada para ela
                foreach (int r0 in def.Layers[layer]) if (Content.Rooms[r0].Secret == room) inLayer = true;
            if (!inLayer) return;
            var run = new Run(sim, def, I(p[5], 1), hp)
            {
                Layer = layer, Time = Math.Max(0f, F(p[4], 0f)), Hatched = p[7] == "1" && def.Cargo == CargoType.Egg,
                RescueValue = Math.Max(0, I(p[9], 0)), RoomsNormal = Math.Max(0, I(p[10], 0)), RoomsFought = Math.Max(0, I(p[11], 0)),
                ChestCoins = Math.Max(0, I(p[12], 0)), RescueOffered = p[13] == "1", PadUsed = k[4] == "1",
            };
            int mask = I(k[3], 0);
            for (int i = 0; i < PerkDef.All.Length; i++) if ((mask & (1 << i)) != 0) run.Apply((Perk)i, false);
            run.Courier.Hp = Clamp(hp, 1, run.Courier.MaxHp);
            for (int i = 0; i < 2; i++)
            {
                string[] g = Get(kv, "cg" + i).Split(',');
                if (g.Length < 5) break;
                int type = I(g[1], -1), st = I(g[4], 2);
                if (type < 0 || type >= CargoDef.All.Length) return;
                bool rescue = i == 1 && g[3] == "1";
                if (i == 0 && (CargoType)type != def.Cargo) return;
                CargoState state = st == 0 ? CargoState.Held : st == 1 && rescue && layer == 0 ? CargoState.Ground : CargoState.Gone;
                if (i == 0 && state != CargoState.Held && !run.Hatched) return;   // carga do contrato fora das maos so se chocou
                Cargo c = run.AddCargo(Math.Max(1, I(g[0], 1)), (CargoType)type, Math.Max(0, I(g[2], 0)), rescue, state);
                if (i == 0 && run.Hatched) c.State = CargoState.Gone;
                if (c.Type == CargoType.Relic && c.Cracks >= Balance.RelicBreak) return;
            }
            if (run.Cargos.Count == 0) return;
            run.RescueAccepted = run.RescueCargo != null;
            if (run.Hatched) run.Pet = new Pet { Hp = Clamp(I(p[8], Balance.PetHp), 1, Balance.PetHp) };
            run.EnterRoom(room, p[6] == "1");
            if (run.RescueCargo != null && run.RescueCargo.State == CargoState.Ground) run.RescueCargo.Pos = run.Room.RescueSpot;
            var offer = new List<Perk>();
            for (int i = 0; i < Balance.PerkOfferSize; i++) { int q = I(k[5 + i], -1); if (q >= 0 && q < PerkDef.All.Length && !run.Has((Perk)q)) offer.Add((Perk)q); }
            if (offer.Count > 0) run.Reoffer(offer.ToArray());
            sim.Run = run;
        }

        static string Get(Dictionary<string, string> kv, string k) => kv.TryGetValue(k, out string s) ? s : "";
        static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));
        static int I(string s, int d) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        static float F(string s, float d) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : d;
    }
}
