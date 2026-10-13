using System;
using System.Collections.Generic;

namespace DC.Core
{
    // ------------------------------------------------------------------ estado lido pela View (campos publicos; quem muda e o Run)

    public sealed class Courier
    {
        public V2 Pos = Balance.Entrance, Facing = V2.Up, DashDir = V2.Up;
        public int Hp, MaxHp;
        public CourierState State;
        /// <summary>DashCd = recarga restante da proxima carga de dash (s); DashCdMax = recarga total com upgrade; IdleT = tempo de dedo solto.</summary>
        public float DashCd, DashCdMax, DashT, IdleT, ShotCd;
        public int DashCount;
        /// <summary>Cargas de dash prontas (Dash duplo: maximo 2).</summary>
        public int DashCharges = 1, DashChargesMax = 1;
        /// <summary>Dentro de uma poca de limo (-40% de velocidade).</summary>
        public bool Slowed;
        public bool DashReady => DashCharges > 0 && State != CourierState.Dashing;
    }

    public sealed class Cargo
    {
        public int Id; public CargoType Type; public CargoState State; public V2 Pos; public int Cracks;
        /// <summary>Carga perdida de um contrato anterior, trazida pelo resgate.</summary>
        public bool Rescue;
        /// <summary>Tempo no chao a mais de 3 m do courier (8 s = nasce um ladrao).</summary>
        public float AloneT;
        /// <summary>Barril: s no chao (explode em Balance.BarrelFuse; zera ao pegar). PickT: progresso de pegar (caixa pesada).
        /// Wobble: s restantes do balanco do bolo (toque rapido negado).</summary>
        public float Fuse, PickT, Wobble;
        /// <summary>Lanterna apagada (derrubada por golpe; a raia R3 liga e reacende numa tocha): no chao nao ilumina.</summary>
        public bool Out;
        /// <summary>Tempo a mais para pegar (s): teia 1,0, casulo da Viuva 1,5 (raia R3 atualiza a cada passo). Progresso em PickT.</summary>
        public float PickExtra;
        /// <summary>O barril so arma depois de ter estado nas maos neste contrato (o barril do resgate no chao nao explode sozinho).</summary>
        internal bool Armed;
    }

    public sealed class Enemy
    {
        public int Id; public EnemyType Type; public EnemyDef Def; public V2 Pos; public int Hp; public EnemyState State;
        /// <summary>Tele = telegraph restante (s); Cd = recarga do ataque; GrabT = progresso do ladrao pegando (0..0,5 s);
        /// ExitT = ladrao na borda saindo (0..1 s); Stun = atordoado (s).</summary>
        public float Tele, Cd, GrabT, ExitT, Phase, Stun;
        public Cargo Carry; public V2 FleeGoal; public int PushedDash = -1;
        /// <summary>Arqueiro: direcao da mira (fixa durante o telegraph). Capitao: golpes levados com a carga (2 derruba).</summary>
        public V2 Aim; public int Hits;
        /// <summary>Nasceu no reforco da sala (evento).</summary>
        public bool Wave;
        /// <summary>Dormindo (zZ, Spawn.Asleep): nao age ate acordar; WakeT = tempo com vizinho acordado perto.</summary>
        public bool Asleep; public float WakeT;
        public bool Alive => Hp > 0;
        public float TeleProgress => State == EnemyState.Telegraph && Def.Tele > 0f ? 1f - Tele / Def.Tele : 0f;
    }

    /// <summary>Projetil do courier. Estilingue: persegue Target. Bumerangue: vai em Dir ate o alcance e volta ao courier, atravessando
    /// inimigos. Bombinha: arco de From ate Goal (Progress 0..1) e explode.</summary>
    public sealed class Shot
    {
        public V2 Pos; public Enemy Target;
        public Weapon Weapon; public V2 Dir, From, Goal; public float T; public bool Returning; public int Damage;
        internal readonly List<int> HitIds = new List<int>();
        public float Progress => Weapon == Weapon.Bomb ? Math.Min(1f, T / Balance.BombFlight) : 0f;
    }

    /// <summary>Flecha do arqueiro: linha reta; acerta o courier, a carga no chao ou o filhote; obstaculo bloqueia.</summary>
    public sealed class Arrow { public V2 Pos, Dir; public float Dist; public int Owner; }

    /// <summary>Poca do limo morto: courier dentro anda Balance.PuddleSlow mais devagar ate Left acabar.</summary>
    public sealed class Puddle { public V2 Pos; public float Radius, Left; }

    /// <summary>Parede rachada na parede lateral: encostar nela com a carga leva a sala secreta Room.</summary>
    public sealed class SecretWall { public V2 Pos; public int Room; }

    /// <summary>Porta na parede de cima. Threat/ChestCoins/Risk = icones (da sala de destino). Locked = Guarda/Ogro/Capitao/emboscada viva
    /// (a mensagem nas maos destranca Guarda e emboscada); Open = atravessavel agora (destrancada e com a carga nas maos, ou o filhote perto).</summary>
    public sealed class Door
    {
        public int Index; public V2 Pos; public int Room, Threat, ChestCoins; public bool Locked, Open; public CargoRisk Risk;
        /// <summary>Lancamento: Blueprint = o bau da sala de destino tem este blueprint (conteudo visivel; None = so moedas). ChestKnown = Faro de
        /// bau (mostre as moedas exatas). Enemies = inimigos da sala de destino (Batedor; -1 = sem o perk).</summary>
        public Gear Blueprint; public bool ChestKnown; public int Enemies = -1;
    }

    /// <summary>Armadilha de urso armada na sala (equipamento lateral): prende quem passar a Balance.TrapReach; Catches = quantos ainda prende.</summary>
    public sealed class Trap { public V2 Pos; public int Catches; internal readonly System.Collections.Generic.List<int> Caught = new System.Collections.Generic.List<int>(); }

    /// <summary>Blueprint = projeto de equipamento que sai deste bau (None = so moedas).</summary>
    public sealed class Chest { public V2 Pos; public int Coins; public bool Opened; public float Progress; public Gear Blueprint; }

    public sealed class Pet { public V2 Pos; public int Hp; }

    /// <summary>Um contrato em curso. Criado por Sim.StartContract/StartDaily/StartCode (ou Sim.Load ao retomar); avancado por Sim.Tick.
    /// partial: os comportamentos novos de cada regiao ficam em Run.RegiaoN.cs (ganchos no fim deste arquivo).</summary>
    public sealed partial class Run
    {
        public readonly Sim Sim; public readonly ContractDef Contract; public readonly int Seed;
        public int Layer; public RoomDef Room; public float Time; public Outcome Outcome; public Hint Hint;
        public readonly Courier Courier = new Courier();
        /// <summary>[0] = carga do contrato; [1] = carga do resgate (se aceito).</summary>
        public readonly List<Cargo> Cargos = new List<Cargo>();
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Shot> Shots = new List<Shot>();
        public readonly List<Door> Doors = new List<Door>();
        public readonly List<Arrow> Arrows = new List<Arrow>();
        public readonly List<Puddle> Puddles = new List<Puddle>();
        public Chest Chest; public Pet Pet; public bool Hatched;
        public bool RescueOffered, RescueAccepted; public int RescueValue;
        public int ChestCoins, RoomsNormal, RoomsFought, DeathRoom = -1;
        /// <summary>Nesta sala ja matou ao menos 1 inimigo nao-Guarda (room_fight_choice).</summary>
        public bool FoughtHere;
        /// <summary>Arma do contrato (a equipada na guilda ao comecar) e o nivel dela.</summary>
        public readonly Weapon Weapon; public readonly int WeaponLevel;
        /// <summary>Perks escolhidos nesta run; PerkOffer = as 3 da escolha aberta (null = sem escolha). Com escolha aberta o Tick nao avanca.</summary>
        public readonly List<Perk> Perks = new List<Perk>();
        public Perk[] PerkOffer;
        /// <summary>Parede rachada desta sala (null = nao ha ou ja e a sala secreta).</summary>
        public SecretWall Secret;
        /// <summary>Reforcos que ja vieram nesta visita (teto Balance.MaxWaves); WaveDone = chegou no teto.</summary>
        public int Waves;
        public bool WaveDone => Waves >= Balance.MaxWaves(Tier);
        /// <summary>Inimigos do reforco que ainda vao chegar (fila de Balance.WaveGap s).</summary>
        public int WavePending => pending.Count;
        readonly List<Spawn> pending = new List<Spawn>(); float pendT;
        /// <summary>Carga acolchoada ja usada (o 1o golpe na carga foi ignorado).</summary>
        public bool PadUsed;

        // ---- lancamento (CONTRATO_LANCAMENTO §2-§7)
        /// <summary>Id da tentativa (Sim.RunsStarted ao comecar): base dos ids de rewarded rw-&lt;Id&gt;-&lt;tipo&gt;.</summary>
        public int Id { get; internal set; }
        /// <summary>Modificadores escolhidos (0-2; o do dia vem com 1).</summary>
        public Modifier Mods { get; internal set; }
        public bool Has(Modifier m) => (Mods & m) != 0;
        public int Region => Contract.Region;
        /// <summary>Tempo-alvo da 3a estrela (Pressa: x0,7).</summary>
        public float ParTime => Contract.ParTime * (Has(Modifier.Rush) ? Balance.RushPar : 1f);
        /// <summary>Tempo-alvo do ovo (Isolamento: x1,2).</summary>
        public float EggTarget => Contract.EggTarget * (Has(Perk.Insulated) ? Balance.PerkInsulatedEgg : 1f);
        /// <summary>Nivel efetivo (vida dos inimigos e teto de reforcos): no desafio sem fim sobe a cada camada.</summary>
        public int Tier => Contract.Tier + (Contract.Kind == ContractKind.Endless ? Layer * Balance.EndlessTierPerLayer : 0);
        /// <summary>Rachaduras que quebram a reliquia (Fragil: 2 em vez de 3). O espelho (R3) usa o mesmo numero.</summary>
        public int BreakAt => Balance.RelicBreak - (Has(Modifier.Fragile) ? 1 : 0);
        /// <summary>Sala escura (Midnight Delivery; a raia R3 liga nas salas da cripta). VisionRadius = Neblina (0 = sem limite). LightMul =
        /// Olhos de gato. MeltMul = Isolamento no cristal de gelo (R2). SureFooted = lama/agua/teia nao freiam (Pe firme ou Botas de lama).</summary>
        public bool Dark;
        public float VisionRadius => Has(Modifier.Fog) ? Balance.FogRadius : 0f;
        public float LightMul => Has(Perk.CatEyes) ? Balance.PerkCatEyes : 1f;
        public float MeltMul => Has(Perk.Insulated) ? Balance.PerkInsulatedMelt : 1f;
        public bool SureFooted => Has(Perk.SureFoot) || Gear == Gear.Boots;
        /// <summary>Nivel das Botas de lama (0 = sem): a raia R3 usa nos niveis 2-5 (teia, agarrao, fogo).</summary>
        public int BootsLevel => Gear == Gear.Boots ? GearLevel : 0;
        /// <summary>Mira so no que esta na luz: sala escura ou Neblina. InLight(p) = dentro da luz do courier (lanterna nas maos maior), de uma tocha
        /// ou da lanterna acesa no chao.</summary>
        public bool Lit => Dark || Has(Modifier.Fog);
        /// <summary>Tochas desta sala (as da sala; na Midnight, sala sem tocha ganha Balance.MidnightTorches).</summary>
        public V2[] Torches => Room.Torches.Length > 0 || Contract.Event != LiveEvent.Midnight ? Room.Torches : Balance.MidnightTorches;
        /// <summary>Baus enterrados e parede rachada disfarcada aparecem: carga Mapa, perk Faro de bau ou Treasure Detour.</summary>
        public bool Revealed => Contract.Cargo == CargoType.TreasureMap || Has(Perk.ChestSense) || Contract.Event == LiveEvent.Treasure;
        /// <summary>Cristal de gelo: tempo para derreter (Isolamento: /0,6) e o minimo antes de falhar (Fragil: 40%). O verbo e da raia R2.</summary>
        public float MeltTime => Contract.MeltTime / MeltMul;
        public float IceFailAt => Has(Modifier.Fragile) ? Balance.FragileIce : 0f;
        /// <summary>Baus enterrados da sala (como o bau: Opened/Progress); so contam com Revealed.</summary>
        public readonly List<Chest> Buried = new List<Chest>();
        /// <summary>Estatisticas da tentativa (quadro de temporada): inimigos derrotados, baus, salas secretas, chefes vencidos sem dano, moedas
        /// do Cacador.</summary>
        public int Kills, ChestsOpened, SecretsFound, BossesClean, HuntCoins;
        /// <summary>Segundo folego ja usado.</summary>
        public bool SecondWindUsed;
        /// <summary>Equipamento lateral da tentativa e o nivel (1-5). Dispara sozinho (sem botao): SmokeLeft = fumacas que restam no contrato;
        /// HookCd = recarga do gancho (s); Traps = armadilhas armadas nesta sala.</summary>
        public readonly Gear Gear; public readonly int GearLevel; public int SmokeLeft; public float HookCd;
        public readonly List<Trap> Traps = new List<Trap>();
        bool trapSet; float hookT;
        /// <summary>Porta da rota mais segura revelada pelo rewarded (-1 = nao revelada; vale so nesta sala).</summary>
        public int SafeDoor = -1;
        bool gripUsed, shieldUsed; int roomDamage, huntRoom;

        public Cargo Main => Cargos[0];
        public Cargo RescueCargo => Cargos.Count > 1 ? Cargos[1] : null;
        public bool Running => Outcome == Outcome.Running;
        public int LayerCount => Contract.Layers.Length;
        /// <summary>Segundos ate o ovo chocar (0 se nao e ovo ou ja chocou).</summary>
        public float EggLeft => Contract.EggTarget > 0f && Contract.Cargo == CargoType.Egg && !Hatched ? Math.Max(0f, EggTarget - Time) : 0f;
        /// <summary>Mensagem urgente: cronometro do contrato.</summary>
        public bool HasTimer => Contract.HasTimer;
        public float TimeLeft => HasTimer ? Math.Max(0f, Contract.TimeLimit - Time) : 0f;
        public bool Has(Perk p) => Perks.Contains(p);
        /// <summary>Condicao da porta e da entrega: carga do contrato nas maos (ou, chocado, o filhote a ate 3 m).</summary>
        public bool CarryOk => Hatched ? Pet != null && V2.Dist(Pet.Pos, Courier.Pos) <= Balance.PetRange : Main.State == CargoState.Held;

        float acc; bool dashQueued; Rng rng = new Rng(1); Nav nav; int nextId, lastBlocked = -1; float summonT, downT; bool warned, msgDoor;
        static readonly Predicate<Enemy> IsDead = e => e.Hp <= 0;

        internal Run(Sim sim, ContractDef contract, int seed, int hp)
        {
            Sim = sim; Contract = contract; Seed = seed;
            Weapon = sim.WeaponUnlocked(sim.Equipped) ? sim.Equipped : Weapon.Sling;
            WeaponLevel = Math.Max(1, sim.WeaponLevel(Weapon));
            Gear = sim.GearUnlocked(sim.GearEquipped) ? sim.GearEquipped : Gear.None;
            GearLevel = sim.GearLevel(Gear);
            SmokeLeft = Gear == Gear.Smoke ? GearDef.SmokeUses[GearLevel - 1] : 0;
            Courier.MaxHp = contract.Event == LiveEvent.NoDamage ? 1 : sim.MaxHp;   // No-Damage Contract: 1 de vida
            Courier.Hp = Math.Max(1, Math.Min(hp, Courier.MaxHp)); Courier.DashCdMax = sim.DashCooldown;
        }

        internal Cargo AddCargo(int id, CargoType type, int cracks, bool rescue, CargoState state)
        {
            var c = new Cargo { Id = id, Type = type, Cracks = cracks, Rescue = rescue, State = state, Pos = Courier.Pos, Armed = state == CargoState.Held };
            Cargos.Add(c);
            return c;
        }

        void Emit(Ev k, int a = 0, int b = 0) => Sim.Emit(k, a, b, Courier.Pos);
        void Emit(Ev k, int a, int b, V2 pos) => Sim.Emit(k, a, b, pos);

        // ------------------------------------------------------------------ salas

        /// <summary>Monta a sala na camada atual com o courier na entrada. Inimigos nascem de novo (retomar nunca devolve sala limpa
        /// nem dano). Cargas Held vem junto; as outras ficam onde o chamador deixou.</summary>
        internal void EnterRoom(int roomId, bool chestTaken, int buriedTaken = 0)
        {
            Room = Content.Rooms[roomId];
            rng = new Rng(Rng.Hash(Seed, Layer * 64 + roomId));
            nav = new Nav(Room);
            Enemies.Clear(); Shots.Clear(); Doors.Clear(); Arrows.Clear(); Puddles.Clear();
            FoughtHere = false; summonT = 0f; lastBlocked = -1; Waves = 0; downT = 0f; msgDoor = false; pending.Clear(); pendT = 0f;
            gripUsed = false; shieldUsed = false; roomDamage = 0; huntRoom = 0; SafeDoor = -1; Dark = Room.Dark || Contract.Event == LiveEvent.Midnight;
            Traps.Clear(); trapSet = false; hookT = 0f;
            var c = Courier;
            c.Pos = Balance.Entrance; c.Facing = V2.Up; c.IdleT = 0f; c.DashT = 0f; c.ShotCd = 0f; c.Slowed = false;
            if (c.State == CourierState.Dashing) c.State = CourierState.Walking;
            awaitMove = true;   // FTUE (GDD s3): ao entrar, a 1a licao e arrastar com a carga; o pouso so conta depois de mexer
            foreach (Spawn s in Room.Spawns)
            {
                bool fixedSpot = EnemyDef.All[(int)s.Type].Locks;   // Guarda/Ogro/Capitao no lugar exato; o resto varia +-0,4 m por semente
                Spawn(s.Type, fixedSpot ? s.Pos : s.Pos + new V2(rng.Range(-0.4f, 0.4f), rng.Range(-0.4f, 0.4f))).Asleep = s.Asleep;
            }
            if (Has(Modifier.Horde) && !Room.Delivery) Spawn(HordeType(Room.Region), new V2(4.5f, 12f));   // Horda: +1 perseguidor da regiao
            bool scout = Has(Perk.Scout), sense = Has(Perk.ChestSense);
            if (Layer + 1 < LayerCount)
            {
                int[] next = Contract.Layers[Layer + 1];
                for (int i = 0; i < next.Length; i++)
                {
                    RoomDef to = Content.Rooms[next[i]];
                    Doors.Add(new Door { Index = i, Pos = new V2(Balance.DoorX(next.Length, i), Balance.RoomH), Room = to.Id, Threat = to.Threat, ChestCoins = to.ChestCoins, Risk = to.Risk,
                                         Blueprint = ProjectAt(to.Id), ChestKnown = sense, Enemies = scout ? to.Spawns.Length + (Has(Modifier.Horde) ? 1 : 0) : -1 });
                }
            }
            Secret = Room.Secret >= 0 && (!Room.SecretHidden || Revealed) ? new SecretWall { Pos = Room.SecretPos, Room = Room.Secret } : null;
            Chest = Room.ChestCoins > 0 ? new Chest { Pos = Room.ChestPos, Coins = Room.ChestCoins, Opened = chestTaken, Blueprint = chestTaken ? Gear.None : ProjectAt(Room.Id) } : null;
            Buried.Clear();
            for (int i = 0; i < Room.Buried.Length; i++) Buried.Add(new Chest { Pos = Room.Buried[i].Pos, Coins = Room.Buried[i].Coins, Opened = (buriedTaken & 1 << i) != 0 });
            foreach (Cargo g in Cargos) if (g.State == CargoState.Held) g.Pos = c.Pos;
            c.State = HasHeld ? CourierState.Carrying : CourierState.Walking;
            if (Pet != null) Pet.Pos = c.Pos + new V2(0.8f, 0f);
            EnterRoomR2(); EnterRoomR3(); EnterRoomR4();
            UpdateDoors();
            Emit(Ev.RoomEnter, roomId, Layer);
        }

        /// <summary>Projeto de equipamento do bau da sala (RoomDef.Blueprint), enquanto nao foi pego (Sim.ProjectTaken) e o equipamento ainda
        /// precisa dele (cada equipamento tem 4 baus para 3 projetos: o que sobra vira so moedas): visivel na porta.</summary>
        Gear ProjectAt(int roomId)
        {
            Gear g = Content.Rooms[roomId].Blueprint;
            return g != Gear.None && !Sim.ProjectTaken(roomId) && !Sim.GearUnlocked(g) ? g : Gear.None;
        }

        /// <summary>Perseguidor da Horda e do reforco extra dela, pela regiao da sala (CONTEUDO_REGIOES §9.2).</summary>
        static EnemyType HordeType(int region) => region == 2 ? EnemyType.BombBeetle : region == 3 ? EnemyType.Skeleton : region == 4 ? EnemyType.Salamander : EnemyType.Bat;

        /// <summary>Perseguidores frageis ficam fora da vida por nivel (morcego, limo, besouro, fantasma, salamandra, brasa, ovo de salamandra).</summary>
        static bool FlatHp(EnemyType t) => t == EnemyType.Bat || t == EnemyType.Slime || t == EnemyType.BombBeetle || t == EnemyType.Ghost
                                            || t == EnemyType.Salamander || t == EnemyType.MagmaShard || t == EnemyType.SalamanderEgg || t == EnemyType.Minecart;

        /// <summary>Poe um inimigo na sala (usado pelos spawns, reforcos, ladroes chamados e pelos testes).</summary>
        public Enemy Spawn(EnemyType type, V2 pos)
        {
            EnemyDef d = EnemyDef.All[(int)type];
            var e = new Enemy { Id = ++nextId, Type = type, Def = d, Pos = pos, Hp = FlatHp(type) ? d.Hp : Math.Max(1, (int)Math.Round(d.Hp * Balance.HpMul(Tier))), State = EnemyState.Idle, Phase = rng.Float() };
            if (type == EnemyType.Bat) ClampRoom(ref e.Pos, d.Radius); else e.Pos = FreeNear(Room, e.Pos, d.Radius);
            Enemies.Add(e);
            return e;
        }

        int AliveCount() { int n = 0; foreach (Enemy e in Enemies) if (e.Alive && !e.Def.Hazard) n++; return n; }   // o vagonete nao conta
        /// <summary>Teto de inimigos vivos para tudo que nasce durante a sala (reforco, ladrao da carga sozinha, chamados do Ogro/Capitao).</summary>
        bool CanSpawn(EnemyType t) => AliveCount() < Balance.MaxAlive && (t != EnemyType.Thief || Count(EnemyType.Thief) < Balance.MaxThieves);

        // ------------------------------------------------------------------ tick

        /// <summary>Acumula dt e avanca em passos fixos de 1/30 s. dash fica guardado ate o proximo passo. Com escolha de perk aberta nada avanca.</summary>
        internal void Tick(float dt, V2 move, bool dash)
        {
            if (dash) dashQueued = true;
            if (!Running) return;
            if (PerkOffer != null) { acc = 0f; dashQueued = false; return; }
            acc += Math.Min(Math.Max(dt, 0f), Balance.MaxFrame);
            while (acc >= Balance.Step - 1e-6f && Running && PerkOffer == null)
            {
                acc -= Balance.Step;
                Step(move, dashQueued);
                dashQueued = false;
            }
        }

        /// <summary>Verdadeiro da entrada na sala ate o 1o movimento ou dash: dedo solto ainda nao pousa a carga.</summary>
        public bool AwaitingMove => awaitMove;
        bool awaitMove;

        bool HasHeld { get { foreach (Cargo g in Cargos) if (g.State == CargoState.Held) return true; return false; } }
        /// <summary>Alguma carga deste tipo nas maos?</summary>
        public bool Holds(CargoType t) { foreach (Cargo g in Cargos) if (g.State == CargoState.Held && g.Type == t) return true; return false; }

        /// <summary>Tempo de dedo solto para pousar agora: 0,2 s (Pouso rapido 0,1 s), -0,02 s por nivel de Mochila (min 0,04 s), +0,2 s
        /// com a caixa pesada nas maos.</summary>
        public float PlantDelayNow => Math.Max(Balance.MinPlantDelay, (Has(Perk.QuickPlant) ? Balance.PerkQuickPlant : Balance.PlantDelay)
                                               - Balance.PackPlantPerLevel * Sim.Levels[(int)Upgrade.Pack]) + (Holds(CargoType.Heavy) ? Balance.HeavyPlant : 0f);

        void Step(V2 move, bool dash)
        {
            const float dt = Balance.Step;
            Time += dt;
            var c = Courier;
            if (c.DashCharges < c.DashChargesMax)
            {
                c.DashCd -= dt;
                if (c.DashCd <= 1e-4f) { c.DashCharges++; c.DashCd = c.DashCharges < c.DashChargesMax ? c.DashCdMax : 0f; }
            }
            else c.DashCd = 0f;
            c.ShotCd = c.ShotCd - dt < 1e-4f ? 0f : c.ShotCd - dt;
            if (HasTimer)
            {
                float left = Contract.TimeLimit - Time;
                if (!warned && left <= Balance.MessageWarn) { warned = true; Emit(Ev.TimeWarning, (int)Math.Ceiling(Math.Max(0f, left))); }
                if (left <= 1e-4f) { Finish(Outcome.TimeUp); return; }
            }
            float ml = move.Len;
            bool moving = ml > 0.05f;
            V2 dir = moving ? move * (1f / ml) : V2.Zero;

            if (dash && c.State != CourierState.Dashing)
            {
                if (Holds(CargoType.Cake))   // o bolo proibe o dash: o toque rapido so balanca
                {
                    awaitMove = false;
                    foreach (Cargo g in Cargos) if (g.State == CargoState.Held && g.Type == CargoType.Cake) g.Wobble = Balance.CakeWobble;
                    Emit(Ev.CakeWobble);
                }
                else if (c.DashCharges > 0) { awaitMove = false; StartDash(moving ? dir : c.Facing); }
            }

            if (c.State == CourierState.Dashing)
            {
                MoveCourier(c.DashDir * (Balance.DashDist * (Has(Perk.Spring) ? Balance.PerkSpring : 1f) / Balance.DashTime * Math.Min(dt, c.DashT)));   // 3 m exatos em 0,18 s (Mola: +40%)
                if (!Running) return;
                PushEnemies();
                c.DashT -= dt;
                if (c.DashT <= 1e-4f && c.State == CourierState.Dashing) { c.State = HasHeld ? CourierState.Carrying : CourierState.Walking; c.IdleT = 0f; }
            }
            else if (moving)
            {
                c.IdleT = 0f; c.Facing = dir; awaitMove = false;
                bool held = HasHeld;
                float mul = held ? (Holds(CargoType.Heavy) ? Balance.HeavySpeedMul : 1f) * (Has(Perk.LightStep) ? Balance.PerkLightStep : 1f)
                                 : Balance.FreeSpeedMul * (Has(Perk.Runner) ? Balance.PerkRunner : 1f);
                float region = 1f; SpeedR2(ref region); SpeedR3(ref region); SpeedR4(ref region);
                if (!SureFooted && InWater(c.Pos)) region *= Balance.WaterSlow;   // agua freia (Pe firme e Botas nao)
                float speed = Balance.CarrySpeed * mul * Math.Min(1f, ml) * (c.Slowed ? 1f - Balance.PuddleSlow : 1f) * region;
                MoveCourier(dir * (speed * dt));
                if (!Running) return;
                PickUp();
                c.State = HasHeld ? CourierState.Carrying : CourierState.Walking;
            }
            else if (!awaitMove)
            {
                c.IdleT += dt;
                if (c.State != CourierState.Planted && c.IdleT >= PlantDelayNow - 1e-4f) Plant();
            }
            if (!moving) foreach (Cargo g in Cargos) g.PickT = 0f;
            if (Gear == Gear.Hook) UpdateHook(moving && c.State != CourierState.Dashing, dir);
            if (!Running) return;
            foreach (Cargo g in Cargos) if (g.State == CargoState.Held) g.Pos = c.Pos;

            if (c.State == CourierState.Planted) Attack();
            UpdateChest();
            UpdateShots();
            UpdateArrows();
            if (!Running) return;
            UpdateEnemies();
            Enemies.RemoveAll(IsDead);
            if (!Running) return;
            UpdatePuddles();
            UpdateWave();
            if (Traps.Count > 0) UpdateTraps();
            UpdateCargos();
            if (!Running) return;
            StepR2(); StepR3(); StepR4();
            if (!Running) return;
            UpdatePet();
            if (!Running) return;
            UpdateDoors();
            if (Room.Delivery && CarryOk && V2.Dist(c.Pos, Balance.Client) <= Balance.DeliverRange)
            {
                Emit(Ev.Deliver, Contract.Index, 0);
                Finish(Outcome.Delivered);
            }
            Hint h = ComputeHint();
            HintR2(ref h); HintR3(ref h); HintR4(ref h);
            Hint = h;
        }

        internal void Finish(Outcome o)
        {
            if (!Running) return;
            Outcome = o;
            PerkOffer = null;
            CloseRoom();   // a sala onde o contrato acabou (morte, roubo) tambem conta para o H1
        }

        void CloseRoom()
        {
            if ((Room.Boss || Room.Mini) && roomDamage == 0 && AliveLocker() == null) BossesClean++;   // chefe vencido sem dano (temporada)
            if (!Room.Normal) return;
            RoomsNormal++;
            if (FoughtHere) RoomsFought++;
            Emit(Ev.RoomFightChoice, FoughtHere ? 1 : 0, Room.Id);
        }

        // ------------------------------------------------------------------ perks

        void OfferPerks()
        {
            var r = new Rng(Rng.Hash(Seed, 4242 + Layer));
            var pool = new List<Perk>();
            bool noDamage = Contract.Event == LiveEvent.NoDamage;   // No-Damage: sem os perks de vida
            for (int i = 0; i < PerkDef.All.Length; i++)
                if (!Perks.Contains((Perk)i) && PerkDef.All[i].Region <= Contract.Region && !(noDamage && (i == (int)Perk.SecondWind || i == (int)Perk.ExtraHeart || i == (int)Perk.FirstAid))) pool.Add((Perk)i);
            if (pool.Count == 0) return;
            var offer = new Perk[Math.Min(Balance.PerkOfferSize, pool.Count)];
            for (int i = 0; i < offer.Length; i++) { int k = r.Int(pool.Count); offer[i] = pool[k]; pool.RemoveAt(k); }
            PerkOffer = offer;
            Emit(Ev.PerkOffer, Layer, 0);
        }

        /// <summary>Escolhe a perk i (0..2) da oferta aberta. Falso se nao ha oferta ou i fora.</summary>
        internal bool Pick(int i)
        {
            if (PerkOffer == null || i < 0 || i >= PerkOffer.Length) return false;
            Perk p = PerkOffer[i];
            PerkOffer = null;
            Apply(p, true);
            Emit(Ev.PerkPick, (int)p, Layer);
            Sim.Dirty = true;
            return true;
        }

        /// <summary>fresh = escolhida agora (cura e carga de dash na hora); falso = retomando do save (a vida gravada ja inclui).</summary>
        internal void Apply(Perk p, bool fresh)
        {
            if (Perks.Contains(p)) return;
            Perks.Add(p);
            var c = Courier;
            if (p == Perk.ExtraHeart) { c.MaxHp += Balance.PerkHeartHp; if (fresh) c.Hp = Math.Min(c.MaxHp, c.Hp + Balance.PerkHeartHp); }
            if (p == Perk.DoubleDash) { c.DashChargesMax = 2; if (fresh) { c.DashCharges = Math.Min(2, c.DashCharges + 1); if (c.DashCharges >= 2) c.DashCd = 0f; } else c.DashCharges = 2; }
        }

        internal void Reoffer(Perk[] offer) { PerkOffer = offer; }

        // ------------------------------------------------------------------ courier

        void MoveCourier(V2 delta)
        {
            var c = Courier;
            V2 p = c.Pos + delta;
            int touching = -1;
            if (p.Y >= Balance.RoomH - Balance.CourierR - 0.02f)
                foreach (Door d in Doors)
                    if (Math.Abs(p.X - d.Pos.X) <= Balance.DoorHalf)
                    {
                        if (d.Open && CarryOk) { GoThrough(d); return; }   // Open e do fim do passo anterior: o dash deste passo pode ter largado a carga
                        touching = d.Index;
                        if (lastBlocked != d.Index) Emit(Ev.DoorBlocked, d.Index, (int)(d.Locked ? Hint.GuardDoor : Hatched ? Hint.WaitPet : Hint.DoorNeedsCargo));
                    }
            lastBlocked = touching;
            float into = Secret == null ? 0f : (Secret.Pos.X < Balance.RoomW * 0.5f ? -delta.X : delta.X) / Math.Max(1e-5f, delta.Len);
            float push = Contract.Event == LiveEvent.Treasure ? -1f : Balance.SecretPush;   // Treasure Detour: paredes rachadas abertas (encostar basta)
            if (Secret != null && CarryOk && into >= push && Math.Abs(p.Y - Secret.Pos.Y) <= Balance.DoorHalf   // empurrar CONTRA a parede (passar rente nao abre)
                && (Secret.Pos.X < Balance.RoomW * 0.5f ? p.X <= Balance.CourierR + 0.02f : p.X >= Balance.RoomW - Balance.CourierR - 0.02f))
            {
                EnterSecret();
                return;
            }
            Collide(Room, ref p, Balance.CourierR);
            c.Pos = p;
        }

        void GoThrough(Door d)
        {
            CloseRoom();
            Emit(Ev.RouteChoice, d.Index, d.Room);
            foreach (Cargo g in Cargos) if (g.State != CargoState.Held) g.State = CargoState.Gone;   // resgate deixado para tras
            Layer++;
            EnterRoom(d.Room, false);
            Heal();
            if (Balance.PerkLayer(Layer) && !Room.Delivery) OfferPerks();
            Sim.Dirty = true;
            Sim.Checkpoint();
        }

        /// <summary>Parede rachada: sala secreta na mesma camada (as portas dela levam as mesmas salas da proxima camada).</summary>
        void EnterSecret()
        {
            int to = Secret.Room;
            CloseRoom();
            SecretsFound++;
            Emit(Ev.SecretFound, to, 0);
            foreach (Cargo g in Cargos) if (g.State != CargoState.Held) g.State = CargoState.Gone;
            EnterRoom(to, false);
            Heal();
            Sim.Dirty = true;
            Sim.Checkpoint();
        }

        /// <summary>Sala nova (porta ou parede rachada): +Balance.RoomHeal de vida, ate o maximo.</summary>
        void Heal()
        {
            int heal = Contract.Event == LiveEvent.NoDamage ? 0 : Contract.Event == LiveEvent.BossRun ? Balance.BossRunHeal : Balance.RoomHeal;   // Boss Run cura 2; No-Damage nao cura
            Courier.Hp = Math.Min(Courier.MaxHp, Courier.Hp + heal);
        }

        void ToGround(Cargo g, V2 pos, int reason)
        {
            g.State = CargoState.Ground; g.Pos = pos; g.AloneT = 0f; g.PickT = 0f;
            Emit(Ev.CargoDrop, g.Id, reason, g.Pos);
            if (g.Type == CargoType.Barrel && g.Armed) Emit(Ev.FuseLit, g.Id, 0, g.Pos);
            DropR3(g, reason);   // regiao 3: queda apaga a lanterna e racha o espelho
        }

        void Plant()
        {
            var c = Courier;
            c.State = CourierState.Planted;
            Emit(Ev.Plant);
            int k = 0;
            foreach (Cargo g in Cargos)
            {
                if (g.State != CargoState.Held) continue;
                V2 p = c.Pos + c.Facing.Right * (k == 0 ? Balance.PlantOffset : -Balance.PlantOffset);
                ToGround(g, FreeNear(Room, p, Balance.CargoR), 0);
                k++;
            }
            if (Gear == Gear.Trap && !trapSet && k > 0)   // Armadilha de urso: no 1o pouso da sala, 0,6 m do outro lado da carga
            {
                trapSet = true;
                Cargo g0 = Cargos[0].State == CargoState.Ground ? Cargos[0] : Cargos[Cargos.Count - 1];
                V2 away = (g0.Pos - c.Pos).Norm;
                Traps.Add(new Trap { Pos = FreeNear(Room, g0.Pos + away * Balance.TrapOffset, 0.2f), Catches = GearDef.TrapCatches[GearLevel - 1] });
                Emit(Ev.GearUse, (int)Gear.Trap, GearLevel, Traps[Traps.Count - 1].Pos);
            }
        }

        void PickUp()
        {
            float range = Sim.PickupRange + 1e-4f;
            foreach (Cargo g in Cargos)
            {
                if (g.State != CargoState.Ground) continue;
                if (V2.Dist(g.Pos, Courier.Pos) > range) { g.PickT = 0f; continue; }
                float need = (g.Type == CargoType.Heavy ? Balance.HeavyPick : 0f) + g.PickExtra;   // caixa pesada: +0,2 s; teia/casulo (R3)
                if (need > 0f) { g.PickT += Balance.Step; if (g.PickT < need - 1e-4f) continue; }
                g.State = CargoState.Held; g.AloneT = 0f; g.PickT = 0f; g.Fuse = 0f; g.Armed = true;
                Emit(Ev.CargoPickup, g.Id, 0, g.Pos);
            }
        }

        void StartDash(V2 dir)
        {
            var c = Courier;
            bool grip = Has(Perk.FirmGrip) && !gripUsed && HasHeld;   // Alca firme: o 1o dash de cada sala leva a carga junto
            if (grip) gripUsed = true;
            foreach (Cargo g in Cargos)
                if (g.State == CargoState.Held && !grip)
                {
                    ToGround(g, c.Pos, 1);   // larga no ponto de partida
                    if (g.Type == CargoType.Relic) Crack(g);   // reliquia que cai racha (pousar nao: e pousar com cuidado)
                }
            c.State = CourierState.Dashing; c.DashDir = dir; c.Facing = dir; c.DashT = Balance.DashTime;
            if (c.DashCharges >= c.DashChargesMax) c.DashCd = c.DashCdMax;
            c.DashCharges--; c.DashCount++;
            Emit(Ev.Dash);
        }

        void PushEnemies()
        {
            var c = Courier;
            bool iron = Has(Perk.IronBoot);
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive || (e.Def.Locks && e.Type != EnemyType.Captain) || e.PushedDash == c.DashCount) continue;
                if (V2.Dist(e.Pos, c.Pos) > e.Def.Radius + Balance.CourierR + Balance.DashPushReach) continue;
                V2 away = (e.Pos - c.Pos).Norm;
                if (away.Len < 0.5f) away = c.DashDir;
                e.Pos = e.Pos + away * (iron ? Balance.PerkIronPush : Balance.DashPush);
                if (e.Type == EnemyType.Bat) ClampRoom(ref e.Pos, e.Def.Radius); else Collide(Room, ref e.Pos, e.Def.Radius);
                e.PushedDash = c.DashCount;
                if (e.Carry != null) { if (Has(Perk.Counter)) DropFromThief(e, 2); else Knock(e); }   // o dash e um golpe: o ladrao derruba (o Capitao conta 1; Contragolpe: derruba ja)
                if (e.State == EnemyState.Grab) { e.GrabT = 0f; e.State = EnemyState.Chase; }
                if (e.State == EnemyState.Telegraph) { e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown * 0.5f; }
                if (iron) { e.Stun = Balance.PerkIronStun; Emit(Ev.Stun, e.Id, 0, e.Pos); }
            }
        }

        void Attack()
        {
            var c = Courier;
            if (c.ShotCd > 0f) return;
            WeaponDef w = WeaponDef.All[(int)Weapon];
            int lv = Math.Max(0, Math.Min(WeaponDef.MaxLevel - 1, WeaponLevel - 1));
            Enemy best = null; float bd = w.Range + 1e-4f;
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive || !CanHit(e)) continue;
                float d = V2.Dist(e.Pos, c.Pos);
                if (d <= bd) { bd = d; best = e; }
            }
            if (best == null) return;
            var s = new Shot { Pos = c.Pos, From = c.Pos, Target = best, Weapon = Weapon, Damage = w.Damage[lv] };
            if (Weapon == Weapon.Boomerang) s.Dir = (best.Pos - c.Pos).Norm;
            else if (Weapon == Weapon.Bomb) s.Goal = best.Pos;
            Shots.Add(s);
            c.ShotCd = w.Cooldown[lv] / (Has(Perk.QuickHand) ? Balance.PerkQuickHand : 1f);
            Emit(Ev.Shot, best.Id, (int)Weapon);
        }

        /// <summary>A arma equipada alcanca este inimigo daqui? Estilingue (teleguiado) e bombinha (arco) passam por cima de obstaculo;
        /// o bumerangue precisa de linha livre (o pilar o faz voltar). A mira automatica so escolhe alvo atingivel.</summary>
        public bool CanHit(Enemy e) => e.Alive && !e.Def.Hazard && V2.Dist(e.Pos, Courier.Pos) <= WeaponDef.All[(int)Weapon].Range + 1e-4f
                                       && (Weapon != Weapon.Boomerang || nav.ShotClear(Courier.Pos, e.Pos, 0.1f)) && (!Lit || InLight(e.Pos)) && HitMul(e) > 0;

        void UpdateShots()
        {
            const float dt = Balance.Step;
            for (int i = Shots.Count - 1; i >= 0; i--)
            {
                if (i >= Shots.Count) continue;
                Shot s = Shots[i];
                if (s.Weapon == Weapon.Sling)
                {
                    float step = Balance.ShotSpeed * dt;
                    if (!s.Target.Alive) { Shots.RemoveAt(i); continue; }
                    V2 d = s.Target.Pos - s.Pos; float len = d.Len;
                    if (len <= step + s.Target.Def.Radius) { Shots.RemoveAt(i); Hit(s.Target, s.Damage); }
                    else s.Pos = s.Pos + d * (step / len);
                }
                else if (s.Weapon == Weapon.Boomerang)
                {
                    s.T += dt;
                    float range = WeaponDef.All[(int)Weapon.Boomerang].Range, step = Balance.BoomerangSpeed * dt;
                    V2 goal = s.Returning ? Courier.Pos : s.From + s.Dir * range;
                    V2 d = goal - s.Pos; float len = d.Len;
                    if (len <= step)
                    {
                        s.Pos = goal;
                        if (s.Returning) { Shots.RemoveAt(i); continue; }
                        s.Returning = true; s.HitIds.Clear();
                    }
                    else s.Pos = s.Pos + d * (step / len);
                    if (!s.Returning && ShotBlocked(Room, s.Pos, 0.1f)) { s.Returning = true; s.HitIds.Clear(); }
                    foreach (Enemy e in Enemies)
                        if (e.Alive && !s.HitIds.Contains(e.Id) && V2.Dist(e.Pos, s.Pos) <= e.Def.Radius + 0.25f) { s.HitIds.Add(e.Id); Hit(e, s.Damage); }
                    if (s.T > Balance.BoomerangLife && i < Shots.Count) Shots.RemoveAt(i);
                }
                else
                {
                    s.T += dt;
                    float k = Math.Min(1f, s.T / Balance.BombFlight);
                    s.Pos = s.From + (s.Goal - s.From) * k;
                    if (k >= 1f) { Shots.RemoveAt(i); Explode(s.Goal, WeaponDef.All[(int)Weapon.Bomb].Radius, s.Damage, 1); }
                }
            }
        }

        void Hit(Enemy e, int dmg)
        {
            if (!e.Alive) return;
            int mul = HitMul(e); if (mul <= 0) return; dmg *= mul;   // regioes: intocavel (0) ou janela de dano (2)
            if (e.Asleep) { dmg *= Balance.SleepHitMul; Wake(e); }   // 1o golpe num dormindo: dano x2
            e.Hp -= dmg;
            Emit(Ev.EnemyHit, e.Id, Math.Max(0, e.Hp), e.Pos);
            if (e.Carry != null) { if (e.Hp <= 0) DropFromThief(e, 2); else Knock(e); }
            if (e.Hp <= 0)
            {
                Emit(Ev.EnemyDie, e.Id, (int)e.Type, e.Pos);
                Kills++;
                if (!e.Def.Locks && !(e.Wave && Room.WaveLocks)) FoughtHere = true;   // H1: Guarda/chefe e emboscada sao luta obrigatoria
                if (e.Type == EnemyType.Slime) { Puddles.Add(new Puddle { Pos = e.Pos, Radius = Balance.PuddleR, Left = Balance.PuddleTime }); Emit(Ev.PuddleSpawn, e.Id, 0, e.Pos); }
                if (Has(Perk.Hunter) && huntRoom < Balance.PerkHunterCap)   // teto por visita de sala
                {
                    huntRoom += Balance.PerkHunterCoins;
                    int k = Sim.Credit(Balance.PerkHunterCoins); HuntCoins += k; Emit(Ev.CurrencySource, k, 4, e.Pos);
                }
                EnemyDiedR2(e); EnemyDiedR3(e); EnemyDiedR4(e);
            }
        }

        /// <summary>Golpe (tiro ou dash) em quem leva a carga: o ladrao derruba; o Capitao so com o 2o golpe.</summary>
        void Knock(Enemy e)
        {
            if (e.Type == EnemyType.Captain && ++e.Hits < Balance.CaptainDropHits) return;
            DropFromThief(e, 2);
        }

        /// <summary>Explosao (barril = fonte 0, bombinha = 1): dano em area nos inimigos; o barril fere o courier; o filhote leva 1;
        /// carga no chao: barril detona, ovo e reliquia racham.</summary>
        void Explode(V2 pos, float radius, int dmg, int source)
        {
            Emit(Ev.Explosion, source, (int)Math.Round(radius * 100f), pos);
            foreach (Enemy e in Enemies) if (e.Asleep && V2.Dist(e.Pos, pos) <= Balance.WakeBlast) Wake(e);
            for (int i = 0; i < Enemies.Count; i++) { Enemy e = Enemies[i]; if (e.Alive && V2.Dist(e.Pos, pos) <= radius + e.Def.Radius) Hit(e, dmg); }
            if (source == 0 && Courier.State != CourierState.Dead && V2.Dist(Courier.Pos, pos) <= radius + Balance.CourierR) DamageCourier(Balance.BarrelDamage);
            if (Pet != null && V2.Dist(Pet.Pos, pos) <= radius + Balance.PetR) DamagePet();
            for (int i = 0; i < Cargos.Count; i++)
            {
                Cargo g = Cargos[i];
                if (g.State != CargoState.Ground || V2.Dist(g.Pos, pos) > radius + Balance.CargoR) continue;
                if (g.Type == CargoType.Barrel) Detonate(g);
                else if (g.Type == CargoType.Egg || g.Type == CargoType.Relic) Crack(g);
            }
        }

        void Detonate(Cargo g)
        {
            g.State = CargoState.Gone; g.Fuse = 0f;
            Sim.Dirty = true;
            if (g == Main) Finish(Outcome.Exploded);
            Explode(g.Pos, Balance.BarrelRadius, Balance.BarrelEnemyDamage, 0);
        }

        /// <summary>cargoHit = o golpe ja rachou a carga nas maos (Bruto pelas costas): a reliquia nao racha duas vezes no mesmo golpe.</summary>
        void DamageCourier(int dmg, bool cargoHit = false)
        {
            var c = Courier;
            if (c.State == CourierState.Dead) return;
            if (dmg >= 2 && Has(Perk.ThickSkin)) dmg--;   // Couro grosso
            c.Hp -= dmg; roomDamage += dmg;
            Emit(Ev.DamageTaken, dmg, Math.Max(0, c.Hp));
            Sim.Dirty = true;
            if (c.Hp <= 0 && Has(Perk.SecondWind) && !SecondWindUsed)   // Segundo folego: 1x por contrato volta com 2 de vida
            {
                SecondWindUsed = true; c.Hp = Math.Min(c.MaxHp, Balance.PerkSecondWindHp);
                Emit(Ev.SecondWind);
            }
            if (c.Hp <= 0)
            {
                c.Hp = 0; c.State = CourierState.Dead; DeathRoom = Room.Id;
                Emit(Ev.DeathRoom, Room.Id, 0);
                Finish(Outcome.Died);
                return;
            }
            if (!cargoHit)   // qualquer golpe no courier (toque de morcego inclusive) racha a reliquia que ele carrega (o bolo nao: o verbo dele e o dash)
                foreach (Cargo g in Cargos) if (g.State == CargoState.Held && g.Type == CargoType.Relic) Crack(g);
        }

        void DamagePet()
        {
            if (Pet == null || Pet.Hp <= 0) return;
            Pet.Hp--;
            Emit(Ev.PetHit, 0, Pet.Hp, Pet.Pos);
            Sim.Dirty = true;
            if (Pet.Hp <= 0) Finish(Outcome.PetDied);
        }

        void Crack(Cargo g)
        {
            if (g.State == CargoState.Gone) return;
            if (g.State == CargoState.Ground && Has(Perk.CargoShield) && !shieldUsed) { shieldUsed = true; return; }   // Escudo da carga: 1o golpe por sala no chao
            if (Has(Perk.Padded) && !PadUsed) { PadUsed = true; Sim.Dirty = true; return; }   // Carga acolchoada: 1o golpe ignorado
            g.Cracks++;
            Emit(Ev.CargoIntegrity, g.Id, g.Cracks, g.Pos);
            Sim.Dirty = true;
            if (Breaks(g))
            {
                g.State = CargoState.Gone;
                Emit(Ev.CargoBroken, g.Id, 0, g.Pos);
                if (g == Main) Finish(Outcome.CargoBroken);
            }
        }

        // ------------------------------------------------------------------ inimigos

        void UpdateEnemies()
        {
            for (int i = 0; i < Enemies.Count && Running; i++)
            {
                Enemy e = Enemies[i];
                if (!e.Alive) continue;
                e.Cd = Math.Max(0f, e.Cd - Balance.Step);
                if (e.Asleep && !WakeCheck(e)) continue;
                if (e.Stun > 0f) { e.Stun = Math.Max(0f, e.Stun - Balance.Step); continue; }
                switch (e.Type)
                {
                    case EnemyType.Bat: Bat(e); break;
                    case EnemyType.Thief: Thief(e); break;
                    case EnemyType.Captain: Thief(e); break;
                    case EnemyType.Archer: Archer(e); break;
                    case EnemyType.Slime: Slime(e); break;
                    default:   // inimigos das regioes 2-4: o gancho da regiao trata; sem comportamento ainda = persegue e golpeia
                        bool handled = false;
                        EnemyR2(e, ref handled); if (!handled) EnemyR3(e, ref handled); if (!handled) EnemyR4(e, ref handled);
                        if (!handled && (e.Def.Speed > 0f || e.Def.Damage > 0)) Melee(e);   // parado e sem dano (vagonete, ovo) = inerte
                        break;
                }
            }
            if (Running && Count(EnemyType.Ogre) > 0)   // o Ogro chama um ladrao a cada 10 s (so ele: os chefes novos tem regra propria)
            {
                summonT += Balance.Step;
                if (summonT >= Balance.OgreSummon - 1e-4f)
                {
                    summonT = 0f;
                    if (CanSpawn(EnemyType.Thief))
                    {
                        var t = Spawn(EnemyType.Thief, new V2(rng.Float() < 0.5f ? 0.6f : Balance.RoomW - 0.6f, rng.Range(5f, 9f)));
                        Emit(Ev.ThiefSpawn, t.Id, 0, t.Pos);
                    }
                }
            }
        }

        void Bat(Enemy e)
        {
            V2 to = ChaseTarget(e) - e.Pos; float d = to.Len;
            e.State = EnemyState.Chase;
            if (d > 0.05f)
            {
                V2 dir = to * (1f / d);
                float w = Time * 0.5f + e.Phase; w -= (float)Math.Floor(w);                          // zigue-zague de morcego: onda
                V2 v = (dir + dir.Right * (0.35f * (1f - 4f * Math.Abs(w - 0.5f)))).Norm;            // triangular de 2 s (sem Math.Sin)
                e.Pos = e.Pos + v * (e.Def.Speed * Balance.Step);
                ClampRoom(ref e.Pos, e.Def.Radius);
            }
            Touch(e);
        }

        /// <summary>Toque de morcego/limo: 1 de dano no courier (ou no filhote), com recarga.</summary>
        void Touch(Enemy e)
        {
            if (e.Cd > 0f) return;
            var c = Courier;
            if (c.State != CourierState.Dead && V2.Dist(e.Pos, c.Pos) <= e.Def.Radius + Balance.CourierR)
            {
                e.Cd = e.Def.Cooldown;
                if (e.Type == EnemyType.Slime) { V2 away = (e.Pos - c.Pos).Norm; e.Pos = e.Pos + (away.Len < 0.5f ? V2.Up : away) * Balance.SlimeRecoil; Collide(Room, ref e.Pos, e.Def.Radius); }   // rebote
                DamageCourier(e.Def.Damage);
            }
            else if (Pet != null && V2.Dist(e.Pos, Pet.Pos) <= e.Def.Radius + Balance.PetR) { e.Cd = e.Def.Cooldown; DamagePet(); }
        }

        void Slime(Enemy e)
        {
            var c = Courier;
            e.State = EnemyState.Chase;
            if (c.State != CourierState.Dead && V2.Dist(e.Pos, c.Pos) > e.Def.Radius + Balance.CourierR - 0.05f) MoveWalker(e, ChaseTarget(e), e.Def.Speed);
            Touch(e);
        }

        /// <summary>Arqueiro: recua se o courier chega a 3 m; com linha livre mira no courier (telegraph 0,8 s, mira fixa) e atira; sem linha,
        /// sai da cobertura. A flecha acerta o primeiro que cruzar: courier, carga no chao ou filhote (risco da carga).</summary>
        void Archer(Enemy e)
        {
            var c = Courier;
            if (e.State == EnemyState.Telegraph)
            {
                e.Tele -= Balance.Step;
                if (e.Tele <= 1e-4f)
                {
                    Arrows.Add(new Arrow { Pos = e.Pos + e.Aim * (e.Def.Radius + 0.05f), Dir = e.Aim, Owner = e.Id });
                    Emit(Ev.ArrowShot, e.Id, 0, e.Pos);
                    e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown;
                }
                return;
            }
            if (c.State == CourierState.Dead) return;
            float dc = V2.Dist(e.Pos, c.Pos);
            if (dc < Balance.ArcherKeep)
            {
                e.State = EnemyState.Flee;
                MoveWalker(e, e.Pos + (e.Pos - c.Pos).Norm * 1.5f, e.Def.Speed);
                return;
            }
            if (e.Cd <= 0f)
            {
                if (dc <= e.Def.Reach && nav.ShotClear(e.Pos, c.Pos, Balance.ArrowR))
                {
                    e.State = EnemyState.Telegraph; e.Tele = e.Def.Tele; e.Aim = (c.Pos - e.Pos).Norm;
                    Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                    return;
                }
                e.State = EnemyState.Chase;   // sem linha: sai da cobertura em direcao ao courier (para no limite de 3 m)
                MoveWalker(e, c.Pos, e.Def.Speed);
                return;
            }
            e.State = e.Cd > 0f ? EnemyState.Recover : EnemyState.Idle;
        }

        void UpdateArrows()
        {
            float step = Balance.ArrowSpeed * Balance.Step;
            var c = Courier;
            for (int i = Arrows.Count - 1; i >= 0 && Running; i--)
            {
                if (i >= Arrows.Count) continue;
                Arrow a = Arrows[i];
                a.Pos = a.Pos + a.Dir * step; a.Dist += step;
                if (c.State != CourierState.Dead && V2.Dist(a.Pos, c.Pos) <= Balance.CourierR + Balance.ArrowR)
                { Arrows.RemoveAt(i); Emit(Ev.ArrowHit, 0, 0, a.Pos); DamageCourier(Balance.ArrowDamage); continue; }
                Cargo hit = null;
                foreach (Cargo g in Cargos) if (g.State == CargoState.Ground && V2.Dist(a.Pos, g.Pos) <= Balance.CargoR + Balance.ArrowR) { hit = g; break; }
                if (hit != null) { Arrows.RemoveAt(i); Emit(Ev.ArrowHit, 1, 0, a.Pos); Crack(hit); continue; }
                if (Pet != null && V2.Dist(a.Pos, Pet.Pos) <= Balance.PetR + Balance.ArrowR) { Arrows.RemoveAt(i); Emit(Ev.ArrowHit, 3, 0, a.Pos); DamagePet(); continue; }
                if (a.Dist > Balance.ArrowRange || a.Pos.X < 0f || a.Pos.Y < 0f || a.Pos.X > Balance.RoomW || a.Pos.Y > Balance.RoomH || ShotBlocked(Room, a.Pos, Balance.ArrowR))
                { Arrows.RemoveAt(i); Emit(Ev.ArrowHit, 2, 0, a.Pos); }
            }
        }

        /// <summary>Ladrao e Capitao. Com a carga: foge para a borda mais perto e some em 1 s. Sem carga: corre para a carga no chao e pega
        /// em 0,5 s (o Capitao chama 2 ladroes ao pegar). Sem carga no chao: o ladrao ronda o courier a 3 m; o Capitao luta (Melee).</summary>
        void Thief(Enemy e)
        {
            bool captain = e.Type == EnemyType.Captain;
            if (e.Carry != null)
            {
                e.State = EnemyState.Flee;
                if (V2.Dist(e.Pos, e.FleeGoal) > 0.25f) { e.ExitT = 0f; MoveWalker(e, e.FleeGoal, captain ? Balance.CaptainFlee : Balance.ThiefFlee); }
                else { e.ExitT += Balance.Step; if (e.ExitT >= Balance.ThiefExit - 1e-4f) { Escape(e); return; } }
                e.Carry.Pos = e.Pos;
                return;
            }
            if (captain && e.State == EnemyState.Telegraph) { Melee(e); return; }
            Cargo t = null; float best = float.MaxValue;
            foreach (Cargo g in Cargos)
            {
                if (g.State != CargoState.Ground || g.Type == CargoType.Balloon) continue;   // o balao (R3) flutua: ladrao e Capitao nao alcancam
                float d = V2.Dist(e.Pos, g.Pos);
                if (d < best) { best = d; t = g; }
            }
            if (t == null)
            {
                e.GrabT = 0f;
                if (captain) { Melee(e); return; }
                e.State = EnemyState.Lurk;   // ronda o courier esperando a carga ir ao chao
                if (V2.Dist(e.Pos, Courier.Pos) > Balance.ThiefLurk) MoveWalker(e, Courier.Pos, e.Def.Speed);
                return;
            }
            if (best > Balance.ThiefGrabRange) { e.State = EnemyState.Chase; e.GrabT = 0f; MoveWalker(e, t.Pos, e.Def.Speed); return; }
            e.State = EnemyState.Grab;
            e.GrabT += Balance.Step;
            if (e.GrabT >= Balance.ThiefGrab - 1e-4f)
            {
                t.State = CargoState.Stolen; e.Carry = t; e.GrabT = 0f; e.ExitT = 0f; e.Hits = 0;
                float side = e.Pos.X < Balance.RoomW * 0.5f ? e.Def.Radius + 0.05f : Balance.RoomW - e.Def.Radius - 0.05f;   // foge para a borda mais perto
                e.FleeGoal = FreeNear(Room, new V2(side, e.Pos.Y), e.Def.Radius);
                Emit(Ev.ThiefGrab, t.Id, e.Id, e.Pos);
                CargoGrabbed(e);
                if (captain)
                {
                    int n = 0;
                    for (int k = 0; k < Balance.CaptainCalls; k++)
                        if (CanSpawn(EnemyType.Thief))
                        {
                            var th = Spawn(EnemyType.Thief, new V2(k == 0 ? 0.6f : Balance.RoomW - 0.6f, rng.Range(6f, 10f)));
                            Emit(Ev.ThiefSpawn, th.Id, 0, th.Pos);
                            n++;
                        }
                    Emit(Ev.CaptainCall, e.Id, n, e.Pos);
                }
            }
        }

        void DropFromThief(Enemy e, int reason)
        {
            Cargo g = e.Carry;
            e.Carry = null; e.State = EnemyState.Chase; e.ExitT = 0f; e.Hits = 0;
            ToGround(g, FreeNear(Room, e.Pos, Balance.CargoR), reason);
            if (g.Type == CargoType.Relic || g.Type == CargoType.Cake) Crack(g);   // caiu da mao do ladrao (tiro ou empurrao do dash)
        }

        void Escape(Enemy e)
        {
            Cargo g = e.Carry;
            e.Carry = null; e.Hp = 0;   // sai da sala (nao conta como luta)
            g.State = CargoState.Gone;
            Emit(Ev.ThiefEscape, g.Id, 0, e.Pos);
            Sim.Dirty = true;
            if (g == Main) Finish(Outcome.CargoStolen);
        }

        void Melee(Enemy e)
        {
            EnemyDef d = e.Def; var c = Courier;
            if (e.State == EnemyState.Telegraph)
            {
                e.Tele -= Balance.Step;
                if (e.Tele <= 1e-4f) { Strike(e); e.State = EnemyState.Recover; e.Cd = d.Cooldown; }
                return;
            }
            if (c.State == CourierState.Dead) return;
            float edge = V2.Dist(e.Pos, c.Pos) - Balance.CourierR;
            if (e.Cd <= 0f && edge <= d.Trigger)
            {
                e.State = EnemyState.Telegraph; e.Tele = d.Tele;
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            if (d.Speed > 0f && edge > d.Trigger * 0.7f) { e.State = EnemyState.Chase; MoveWalker(e, ChaseTarget(e), d.Speed); }
            else e.State = e.Cd > 0f ? EnemyState.Recover : EnemyState.Idle;
        }

        void Strike(Enemy e)
        {
            EnemyDef d = e.Def; var c = Courier;
            Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
            if (c.State != CourierState.Dead && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= d.Reach)
            {
                // golpe do Bruto pelas costas com a carga nas maos acerta tambem a carga
                bool back = e.Type == EnemyType.Brute && V2.Dot(c.Facing, e.Pos - c.Pos) < 0f;
                if (back) foreach (Cargo g in Cargos) if (g.State == CargoState.Held) Crack(g);
                DamageCourier(d.Damage, back);
            }
            if (e.Type == EnemyType.Brute || e.Type == EnemyType.Ogre)   // Bruto e Ogro acertam a carga no chao dentro do alcance
                foreach (Cargo g in Cargos) if (g.State == CargoState.Ground && V2.Dist(e.Pos, g.Pos) <= d.Reach) Crack(g);
            if (Pet != null && V2.Dist(e.Pos, Pet.Pos) - Balance.PetR <= d.Reach) DamagePet();
        }

        Enemy AliveLocker() { foreach (Enemy e in Enemies) if (e.Alive && e.Def.Locks) return e; return null; }
        int Count(EnemyType t) { int n = 0; foreach (Enemy e in Enemies) if (e.Alive && e.Type == t) n++; return n; }

        void MoveWalker(Enemy e, V2 goal, float speed)
        {
            V2 to = goal - e.Pos; float step = speed * Balance.Step;
            if (to.Len <= step) e.Pos = goal;
            else
            {
                if (!e.Def.Boss && InWater(e.Pos)) step *= Balance.WaterSlow;   // agua freia (chefes nao)
                e.Pos = e.Pos + nav.Dir(e.Pos, goal) * step;
            }
            Collide(Room, ref e.Pos, e.Def.Radius);
        }

        // ------------------------------------------------------------------ sala: reforco e pocas

        /// <summary>Evento da sala (CONTRATO_V1 §2): o reforco vem quando o courier passa da metade da sala (Half) ou quando a carga fica
        /// Balance.WaveDownTime s no chao (CargoDown); no maximo Balance.MaxWaves por visita (o seguinte so depois que o anterior caiu) e
        /// nunca passa de Balance.MaxAlive inimigos vivos.</summary>
        void UpdateWave()
        {
            if (pending.Count > 0)
            {
                pendT -= Balance.Step;
                if (pendT > 1e-4f) return;
                if (!CanSpawn(pending[0].Type)) { pendT = Balance.WaveGap; return; }
                Spawn sp = pending[0]; pending.RemoveAt(0);
                Enemy w = Spawn(sp.Type, sp.Pos + new V2(rng.Range(-0.3f, 0.3f), rng.Range(-0.2f, 0f)));
                w.Wave = true; pendT = Balance.WaveGap;
                return;
            }
            if (WaveDone || Room.WaveTrigger == RoomTrigger.None || !Contract.Waves) return;
            if (Waves > 0) foreach (Enemy e in Enemies) if (e.Alive && e.Wave) return;
            bool fire;
            if (Room.WaveTrigger == RoomTrigger.Half) fire = Courier.Pos.Y >= Balance.RoomH * 0.5f;
            else
            {
                bool down = false;
                foreach (Cargo g in Cargos) if (g.State == CargoState.Ground) down = true;
                if (down) downT += Balance.Step;
                fire = downT >= Balance.WaveDownTime - 1e-4f;
            }
            if (!fire) return;
            Waves++; downT = 0f;
            pending.AddRange(Room.Wave); pendT = 0f;
            if (Has(Modifier.Horde)) pending.Add(new Spawn(HordeType(Room.Region), 4.5f, 12f));   // Horda: cada reforco vem com +1
            Emit(Ev.RoomEvent, Room.Id, Room.Wave.Length);
        }

        void UpdatePuddles()
        {
            bool slowed = false;
            for (int i = Puddles.Count - 1; i >= 0; i--)
            {
                Puddle p = Puddles[i];
                p.Left -= Balance.Step;
                if (p.Left <= 1e-4f) { Puddles.RemoveAt(i); continue; }
                if (V2.Dist(p.Pos, Courier.Pos) <= p.Radius) slowed = true;
            }
            Courier.Slowed = slowed && !SureFooted;
        }

        // ------------------------------------------------------------------ cargas, filhote, bau, portas

        void UpdateCargos()
        {
            var c = Courier;
            float alone = Balance.AloneTime + (Has(Perk.Lookout) ? Balance.PerkLookout : 0f);
            for (int i = 0; i < Cargos.Count && Running; i++)
            {
                Cargo g = Cargos[i];
                if (g.Wobble > 0f) g.Wobble = Math.Max(0f, g.Wobble - Balance.Step);
                if (g.State == CargoState.Ground && g.Type == CargoType.Barrel && g.Armed)   // barril no chao: 3 s e explode
                {
                    g.Fuse += Balance.Step;
                    if (g.Fuse >= Balance.BarrelFuse - 1e-4f) { Detonate(g); continue; }
                }
                if (g.State != CargoState.Ground) { g.AloneT = 0f; continue; }
                if (V2.Dist(g.Pos, c.Pos) <= Balance.AloneDist) { g.AloneT = 0f; continue; }
                g.AloneT += Balance.Step;
                if (g.AloneT < alone - 1e-4f) continue;
                g.AloneT = 0f;
                if (!CanSpawn(EnemyType.Thief)) continue;
                float dir = g.Pos.X < Balance.RoomW * 0.5f ? -1f : 1f;   // "perto dela", do lado da parede mais proxima
                float x = Math.Max(0.6f, Math.Min(Balance.RoomW - 0.6f, g.Pos.X + dir * Balance.ThiefSpawnDist));
                if (Math.Abs(x - g.Pos.X) < 1.5f) x = g.Pos.X - dir * Balance.ThiefSpawnDist;
                var t = Spawn(EnemyType.Thief, new V2(x, g.Pos.Y));
                Emit(Ev.ThiefSpawn, t.Id, 0, t.Pos);
            }
            if (!Running) return;
            if (Contract.EggTarget > 0f && Contract.Cargo == CargoType.Egg && !Hatched && Main.State != CargoState.Gone && Time >= EggTarget - 1e-4f) Hatch();   // no gelo o campo e o MeltTime
        }

        void Hatch()
        {
            Cargo m = Main;
            V2 p = m.State == CargoState.Held ? Courier.Pos : m.Pos;
            foreach (Enemy e in Enemies) if (e.Carry == m) { e.Carry = null; e.State = EnemyState.Chase; }
            m.State = CargoState.Gone;
            Hatched = true;
            Pet = new Pet { Pos = p, Hp = Balance.PetHp };
            if (Courier.State == CourierState.Carrying && !HasHeld) Courier.State = CourierState.Walking;
            Sim.Dirty = true;
            Emit(Ev.Hatch, 0, 0, p);
        }

        void UpdatePet()
        {
            if (Pet == null) return;
            V2 to = Courier.Pos - Pet.Pos;
            if (to.Len <= Balance.PetFollow) return;
            float step = Balance.CarrySpeed * Balance.FreeSpeedMul * Balance.Step;
            Pet.Pos = Pet.Pos + nav.Dir(Pet.Pos, Courier.Pos) * Math.Min(step, to.Len - Balance.PetFollow);
            Collide(Room, ref Pet.Pos, Balance.PetR);
        }

        void UpdateChest()
        {
            if (Chest != null && !Chest.Opened) OpenChest(Chest, Balance.ChestTime, false);
            if (Buried.Count > 0 && Revealed)
                foreach (Chest b in Buried) if (!b.Opened) OpenChest(b, Has(Perk.ChestSense) ? Balance.BuriedSenseTime : Balance.BuriedTime, true);
        }

        /// <summary>Parado perto do bau (1,2 m; Ima 2,7) pelo tempo: abre. Moedas com Sorte de bau, Pao-duro e Treasure Detour, e a Bolsa;
        /// Primeiros socorros cura 1; o projeto do bau (1a abertura) vai para o Sim.</summary>
        void OpenChest(Chest ch, float time, bool buried)
        {
            var c = Courier;
            float range = Has(Perk.Magnet) ? Balance.PerkMagnetRange : Balance.ChestRange;
            if (c.State != CourierState.Planted || V2.Dist(c.Pos, ch.Pos) > range) { ch.Progress = 0f; return; }
            ch.Progress += Balance.Step;
            if (ch.Progress < time - 1e-4f) return;
            ch.Opened = true;
            double raw = ch.Coins * (Has(Perk.ChestLuck) ? Balance.Pct(Balance.PerkChestLuck) : 100) / 100.0;
            if (Has(Modifier.Stingy)) raw = raw * Balance.StingyChestPct / 100.0;                          // Pao-duro
            if (Contract.Event == LiveEvent.Treasure) raw = raw * Balance.TreasureChestPct / 100.0;       // Treasure Detour
            int coins = Sim.Credit(raw);
            ChestCoins += coins; ChestsOpened++;
            Emit(Ev.ChestOpen, coins, buried ? 1 : 0, ch.Pos);
            Emit(Ev.OptionalLoot, coins, Room.Id, ch.Pos);
            Emit(Ev.CurrencySource, coins, 1, ch.Pos);
            if (Has(Perk.FirstAid)) Courier.Hp = Math.Min(Courier.MaxHp, Courier.Hp + Balance.PerkFirstAid);
            if (ch.Blueprint != Gear.None) { Sim.TakeProject(Room.Id, ch.Blueprint); ch.Blueprint = Gear.None; }
            Sim.Dirty = true;
        }

        /// <summary>Trancada por chefe (Ogro, Capitao) sempre; por Guarda ou emboscada viva so sem a mensagem nas maos.</summary>
        void UpdateDoors()
        {
            bool boss = false, guard = false;
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive) continue;
                if (e.Def.Locks) { if (e.Def.Boss) boss = true; else guard = true; }
                else if (e.Wave && Room.WaveLocks) guard = true;
            }
            if (pending.Count > 0 && Room.WaveLocks) guard = true;
            bool msg = guard && !boss && Holds(CargoType.Message);
            bool locked = boss || (guard && !msg), ok = CarryOk;
            foreach (Door d in Doors) { d.Locked = locked; d.Open = !locked && ok; }
            if (msg && !msgDoor && Doors.Count > 0) { msgDoor = true; Emit(Ev.MessageDoor, 0, 0, Doors[0].Pos); }
        }

        Hint ComputeHint()
        {
            var c = Courier;
            if (awaitMove && Main.State == CargoState.Held) return Hint.DragToWalk;
            foreach (Cargo g in Cargos) if (g.State == CargoState.Ground && g.Type == CargoType.Barrel && g.Armed) return Hint.BarrelFuse;
            foreach (Cargo g in Cargos) if (g.Wobble > 0f) return Hint.CakeNoDash;
            foreach (Enemy e in Enemies)
                if (e.Alive && (e.Type == EnemyType.Thief || e.Type == EnemyType.Captain) && (e.Carry != null || e.State == EnemyState.Grab || (e.Type == EnemyType.Thief && e.State == EnemyState.Chase))) return Hint.Thief;
            foreach (Enemy e in Enemies)
                if (e.Alive && e.Type == EnemyType.Archer && e.State == EnemyState.Telegraph && V2.SegDist(c.Pos, e.Pos, e.Pos + e.Aim * Balance.ArrowRange) <= Balance.CourierR + 0.4f) return Hint.ArcherAim;
            bool lockedByGuard = AliveLocker() != null;
            foreach (Door d in Doors)
            {
                if (V2.Dist(c.Pos, d.Pos) > 2f) continue;
                if (!d.Open) return d.Locked ? Hint.GuardDoor : Hatched ? Hint.WaitPet : Hint.DoorNeedsCargo;
                if (msgDoor && lockedByGuard) return Hint.MessageDoor;
            }
            if (AliveLocker() != null && Main.State == CargoState.Held && c.State != CourierState.Planted && !msgDoor) return Hint.PlantToFight;
            foreach (Enemy e in Enemies)
                if (e.Alive && e.State == EnemyState.Telegraph && e.Type != EnemyType.Archer && c.DashReady && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach + 0.5f) return Hint.Dash;
            if (Chest != null && !Chest.Opened && V2.Dist(c.Pos, Chest.Pos) <= 2.5f) return Hint.Chest;
            if (Secret != null && V2.Dist(c.Pos, Secret.Pos) <= 2.5f) return Hint.SecretWall;
            if (Revealed) foreach (Chest b in Buried) if (!b.Opened && V2.Dist(c.Pos, b.Pos) <= 2.5f) return Hint.MapReveal;
            foreach (Enemy e in Enemies) if (e.Alive && e.Asleep && V2.Dist(e.Pos, c.Pos) <= Balance.WakeRange + 1f) return Hint.Asleep;
            if (Lit && c.State == CourierState.Planted)
                foreach (Enemy e in Enemies) if (e.Alive && !e.Def.Hazard && V2.Dist(e.Pos, c.Pos) <= Balance.ShotRange && !InLight(e.Pos)) return Hint.Dark;
            if (!Hatched && Main.State == CargoState.Ground)
            {
                foreach (Enemy e in Enemies) if (e.Alive && V2.Dist(e.Pos, c.Pos) <= Balance.ShotRange) return Hint.None;
                return Hint.PickUp;
            }
            return Hint.None;
        }

        // ------------------------------------------------------------------ equipamento lateral e rota segura (lancamento)

        /// <summary>Gancho: andando sem carga na direcao da carga do contrato no chao (+-30 graus, 1,5-5 m) por 0,25 s, ela vem para as maos (nao
        /// racha). No nivel 5 tambem tira da mao de quem a levou. Recarga GearDef.HookCooldown pelo nivel.</summary>
        void UpdateHook(bool moving, V2 dir)
        {
            if (HookCd > 0f) { HookCd = Math.Max(0f, HookCd - Balance.Step); hookT = 0f; return; }
            Cargo m = Main; var c = Courier;
            bool aim = false;
            if (moving && !HasHeld && (m.State == CargoState.Ground || (m.State == CargoState.Stolen && GearLevel >= Balance.GearCampaignLevel)))
            {
                V2 to = m.Pos - c.Pos; float d = to.Len;
                aim = d >= Balance.HookMin && d <= Balance.HookRange && V2.Dot(to * (1f / d), dir) >= Balance.HookCone;
            }
            if (!aim) { hookT = 0f; return; }
            hookT += Balance.Step;
            if (hookT < Balance.HookAim - 1e-4f) return;
            hookT = 0f; HookCd = GearDef.HookCooldown[GearLevel - 1];
            foreach (Enemy e in Enemies) if (e.Carry == m) { e.Carry = null; e.State = EnemyState.Chase; e.ExitT = 0f; }
            m.State = CargoState.Held; m.Pos = c.Pos; m.AloneT = 0f; m.PickT = 0f; m.Fuse = 0f; m.Armed = true;
            c.State = CourierState.Carrying; c.IdleT = 0f;
            Emit(Ev.GearUse, (int)Gear.Hook, GearLevel);
            Emit(Ev.CargoPickup, m.Id, 1, c.Pos);
            Sim.Dirty = true;
        }

        /// <summary>Armadilha: o 1o inimigo que nao voa nem e chefe a Balance.TrapReach fica preso GearDef.TrapTime s (nivel 5: mini-chefe 1,5 s).</summary>
        void UpdateTraps()
        {
            foreach (Trap t in Traps)
            {
                if (t.Catches <= 0) continue;
                foreach (Enemy e in Enemies)
                {
                    if (!e.Alive || e.Type == EnemyType.Bat || e.Def.Hazard || t.Caught.Contains(e.Id) || V2.Dist(e.Pos, t.Pos) > Balance.TrapReach + e.Def.Radius) continue;
                    bool mini = e.Def.Mini && GearLevel >= Balance.GearCampaignLevel;
                    if (e.Def.Boss && !mini) continue;
                    e.Stun = Math.Max(e.Stun, mini ? Balance.TrapMini : GearDef.TrapTime[GearLevel - 1]);
                    t.Caught.Add(e.Id); t.Catches--;
                    Emit(Ev.Stun, e.Id, 1, e.Pos);
                    if (t.Catches <= 0) break;
                }
            }
        }

        /// <summary>Um inimigo pegou a carga (ladrao, Capitao; as raias chamam isto para fantasma, diabrete, sapo, Viuva, Capataz). Assobio:
        /// fica parado 1,5 s. Bomba de fumaca (se restar uso): quem esta na nuvem de 2,5 m solta a carga e fica sem ve-la GearDef.SmokeTime s.</summary>
        internal void CargoGrabbed(Enemy e)
        {
            if (Has(Perk.Whistle)) e.Stun = Balance.PerkWhistle;   // Assobio
            if (Gear != Gear.Smoke || SmokeLeft <= 0) return;
            SmokeLeft--;
            float t = GearDef.SmokeTime[GearLevel - 1];
            foreach (Enemy o in Enemies)
            {
                if (!o.Alive || V2.Dist(o.Pos, e.Pos) > Balance.SmokeRadius) continue;
                if (o.Carry != null) DropFromThief(o, 3);
                if (o.Type == EnemyType.Thief || o.Type == EnemyType.Captain || o == e) o.Stun = Math.Max(o.Stun, t);
            }
            Emit(Ev.GearUse, (int)Gear.Smoke, GearLevel, e.Pos);
            Sim.Dirty = true;
        }

        /// <summary>Acorda o dormindo (golpe, explosao, grito da gaiola: a raia R2 chama).</summary>
        internal void Wake(Enemy e) { e.Asleep = false; e.WakeT = 0f; }

        bool WakeCheck(Enemy e)
        {
            if (V2.Dist(e.Pos, Courier.Pos) <= Balance.WakeRange) { Wake(e); return true; }
            bool near = false;
            foreach (Enemy o in Enemies) if (o != e && o.Alive && !o.Asleep && !o.Def.Hazard && V2.Dist(o.Pos, e.Pos) <= Balance.WakeNeighbor) { near = true; break; }
            e.WakeT = near ? e.WakeT + Balance.Step : 0f;
            if (e.WakeT < Balance.WakeDelay - 1e-4f) return false;
            Wake(e);
            return true;
        }

        /// <summary>Quebra: reliquia, espelho e balao (R3) com BreakAt rachaduras; com o Fragil, qualquer carga com 2.</summary>
        internal bool Breaks(Cargo g) => ((g.Type == CargoType.Relic || g.Type == CargoType.Mirror || g.Type == CargoType.Balloon) && g.Cracks >= BreakAt)
                                         || (Has(Modifier.Fragile) && g.Cracks >= Balance.FragileBreak);

        /// <summary>Ponto dentro de agua da sala.</summary>
        public bool InWater(V2 p) { foreach (Rect w in Room.Water) { V2 q = w.Closest(p); if (q.X == p.X && q.Y == p.Y) return true; } return false; }

        /// <summary>Ponto iluminado: luz do courier (3,5 m; lanterna nas maos 5,5; x Olhos de gato), tocha (2,5 m) ou lanterna acesa no chao (4 m).</summary>
        public bool InLight(V2 p)
        {
            bool lamp = false; foreach (Cargo g in Cargos) if (g.Type == CargoType.Lantern && g.State == CargoState.Held && !g.Out) lamp = true;   // apagada nas maos nao ilumina
            float own = (lamp ? Balance.LightLantern : Balance.LightCourier) * LightMul - LightCut;   // LightCut/TorchCut: Necromante fase 3, Neblina na cripta (R3)
            if (V2.Dist(p, Courier.Pos) <= own) return true;
            foreach (V2 t in Torches) if (V2.Dist(p, t) <= Balance.LightTorch - TorchCut) return true;
            foreach (Cargo g in Cargos) if (g.Type == CargoType.Lantern && g.State == CargoState.Ground && !g.Out && V2.Dist(p, g.Pos) <= Balance.LightLanternGround) return true;
            return false;
        }

        /// <summary>Rewarded "revelar rota segura": a porta com menos caveiras e menos riscos para a carga (empate = a da esquerda).</summary>
        internal void RevealSafe()
        {
            int bs = int.MaxValue;
            foreach (Door d in Doors)
            {
                int risk = 0; for (int k = (int)d.Risk; k != 0; k &= k - 1) risk++;
                int s = d.Threat * 10 + risk;
                if (s < bs) { bs = s; SafeDoor = d.Index; }
            }
        }

        // ------------------------------------------------------------------ ganchos das regioes 2-4 (implemente em Run.RegiaoN.cs)
        // Metodos parciais classicos: sem implementacao, o compilador os remove (custo zero). Cada regiao implementa so os seus.
        //   EnterRoomRn()                     ao montar a sala (depois dos spawns, antes das portas): perigos da sala, Dark, trilhos...
        //   StepRn()                          1x por passo de 1/30 s, depois das cargas: verbos das cargas novas, poças de fogo, gelo...
        //   EnemyRn(e, ref handled)           comportamento do inimigo novo; handled = true se tratou (senao vira Melee)
        //   SpeedRn(ref mul)                  multiplicador da velocidade do courier andando (agua -25%, balao +15%...)
        //   EnemyDiedRn(e)                    inimigo derrotado (esqueleto remonta, elemental divide...)
        //   HintRn(ref h)                     dica da regiao (Hint.Water, CartBell...): troque h se a sua tiver prioridade
        // Uteis: Wake(e) (grito acorda), CargoGrabbed(e) (quem pegar/possuir/trocar/puxar a carga: Assobio e fumaca), Breaks(g), InWater(p),
        // InLight(p), Dark/Torches/Revealed, MeltTime/IceFailAt (gelo), BootsLevel, Finish(Outcome.Melted/Escaped).
        partial void EnterRoomR2(); partial void EnterRoomR3(); partial void EnterRoomR4();
        partial void StepR2(); partial void StepR3(); partial void StepR4();
        partial void EnemyR2(Enemy e, ref bool handled); partial void EnemyR3(Enemy e, ref bool handled); partial void EnemyR4(Enemy e, ref bool handled);
        partial void SpeedR2(ref float mul); partial void SpeedR3(ref float mul); partial void SpeedR4(ref float mul);
        partial void EnemyDiedR2(Enemy e); partial void EnemyDiedR3(Enemy e); partial void EnemyDiedR4(Enemy e);
        partial void HintR2(ref Hint h); partial void HintR3(ref Hint h); partial void HintR4(ref Hint h);
        /// <summary>Multiplicador do dano que o inimigo leva (tiro, explosao, lava...): 0 = intocavel (nao e alvo da mira nem leva dano: Dragao no
        /// ar, toupeira por baixo...), 2 = janela de dano (Dragao de cabeca baixa). Cada regiao muda `mul` no seu HitMulRn.</summary>
        int HitMul(Enemy e) { int m = 1; HitMulR2(e, ref m); HitMulR3(e, ref m); HitMulR4(e, ref m); return m; }
        /// <summary>Intocavel agora (HitMul 0): nao e alvo da mira nem leva dano (a View e o bot leem).</summary>
        public bool Untouchable(Enemy e) => HitMul(e) <= 0;
        /// <summary>Carga foi ao chao (ToGround): reason 0 pousou, 1 dash, 2 ladrao atingido, 3 fumaca (raia R3: lanterna apaga, espelho racha).</summary>
        partial void DropR3(Cargo g, int reason);
        partial void HitMulR2(Enemy e, ref int mul); partial void HitMulR3(Enemy e, ref int mul); partial void HitMulR4(Enemy e, ref int mul);
        /// <summary>Para onde quem persegue anda (morcego, limo, Bruto...): o courier, ou o que a regiao manda no TargetRn (coracao arcano no chao,
        /// grito da gaiola: R2). Sem regiao = o courier (regiao 1 igual).</summary>
        internal V2 ChaseTarget(Enemy e) { V2 t = Courier.Pos; TargetR2(e, ref t); TargetR3(e, ref t); TargetR4(e, ref t); return t; }
        partial void TargetR2(Enemy e, ref V2 t); partial void TargetR3(Enemy e, ref V2 t); partial void TargetR4(Enemy e, ref V2 t);

        // ------------------------------------------------------------------ geometria e navegacao

        /// <summary>Direcao (unitaria) para ir de `from` ate `to` contornando obstaculos (linha reta se livre; senao campo de fluxo).</summary>
        public V2 PathDir(V2 from, V2 to) => nav.Dir(from, to);
        /// <summary>Segmento a-b livre de obstaculos para um circulo de raio r (linha de visada da flecha: r = Balance.ArrowR).</summary>
        public bool LineClear(V2 a, V2 b, float r) => nav.ShotClear(a, b, r);

        /// <summary>Solido para andar: obstaculos e lava.</summary>
        public static bool Blocked(RoomDef room, V2 p, float r) => Hits(room.Obstacles, p, r) || Hits(room.Lava, p, r);
        /// <summary>Solido para tiro, flecha e bumerangue: so obstaculos (a lava e transparente).</summary>
        public static bool ShotBlocked(RoomDef room, V2 p, float r) => Hits(room.Obstacles, p, r);

        static bool Hits(Rect[] rects, V2 p, float r)
        {
            foreach (Rect o in rects)
            {
                V2 q = o.Closest(p); float dx = p.X - q.X, dy = p.Y - q.Y;
                if (dx * dx + dy * dy < r * r) return true;
            }
            return false;
        }

        static void ClampRoom(ref V2 p, float r)
        {
            p.X = Math.Max(r, Math.Min(Balance.RoomW - r, p.X));
            p.Y = Math.Max(r, Math.Min(Balance.RoomH - r, p.Y));
        }

        /// <summary>Empurra o circulo (p, r) para fora dos obstaculos e para dentro da sala.</summary>
        public static void Collide(RoomDef room, ref V2 p, float r)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (Rect o in room.Obstacles) Push(ref p, o, r);
                foreach (Rect o in room.Lava) Push(ref p, o, r);
            }
            ClampRoom(ref p, r);
        }

        static void Push(ref V2 p, Rect o, float r)
        {
            V2 q = o.Closest(p); V2 d = p - q; float l = d.Len;
            if (l >= r) return;
            if (l > 1e-5f) { p = q + d * (r / l); return; }
            float left = p.X - (o.C.X - o.H.X), right = o.C.X + o.H.X - p.X, down = p.Y - (o.C.Y - o.H.Y), up = o.C.Y + o.H.Y - p.Y;
            float m = Math.Min(Math.Min(left, right), Math.Min(down, up));
            if (m == left) p.X = o.C.X - o.H.X - r;
            else if (m == right) p.X = o.C.X + o.H.X + r;
            else if (m == down) p.Y = o.C.Y - o.H.Y - r;
            else p.Y = o.C.Y + o.H.Y + r;
        }

        /// <summary>Ponto livre mais perto de p (procura em aneis de 0,25 m).</summary>
        public static V2 FreeNear(RoomDef room, V2 p, float r)
        {
            ClampRoom(ref p, r);
            if (!Blocked(room, p, r)) return p;
            for (int k = 1; k <= 16; k++)
                foreach (V2 dir in V2.Dirs8)
                {
                    V2 q = p + dir * (0.25f * k);
                    ClampRoom(ref q, r);
                    if (!Blocked(room, q, r)) return q;
                }
            return p;
        }
    }

    /// <summary>Navegacao em grade de 0,5 m (18 x 28): linha reta quando livre; senao desce um campo BFS ate o alvo, guardado por
    /// celula-alvo (no maximo 504 campos por sala). ponytail: grade fixa e BFS 4-vizinhos; se as salas crescerem, trocar por A*.</summary>
    public sealed class Nav
    {
        public const float Cell = 0.5f; public const int W = 18, H = 28;
        const float Inflate = 0.45f;
        readonly RoomDef room;
        readonly bool[] blocked = new bool[W * H];
        readonly int[] queue = new int[W * H];
        readonly Dictionary<int, int[]> cache = new Dictionary<int, int[]>();

        public Nav(RoomDef r)
        {
            room = r;
            for (int i = 0; i < blocked.Length; i++) blocked[i] = Run.Blocked(r, Center(i), Inflate);
        }

        static V2 Center(int i) => new V2((i % W + 0.5f) * Cell, (i / W + 0.5f) * Cell);
        static int CellOf(V2 p)
        {
            int x = Math.Max(0, Math.Min(W - 1, (int)(p.X / Cell))), y = Math.Max(0, Math.Min(H - 1, (int)(p.Y / Cell)));
            return y * W + x;
        }

        /// <summary>Segmento a-b livre para um circulo de raio r?</summary>
        public bool Clear(V2 a, V2 b, float r)
        {
            V2 d = b - a; int n = (int)(d.Len / 0.25f) + 1;
            for (int i = 1; i <= n; i++) if (Run.Blocked(room, a + d * ((float)i / n), r)) return false;
            return true;
        }

        /// <summary>Linha de tiro livre (a lava nao bloqueia tiro).</summary>
        public bool ShotClear(V2 a, V2 b, float r)
        {
            V2 d = b - a; int n = (int)(d.Len / 0.25f) + 1;
            for (int i = 1; i <= n; i++) if (Run.ShotBlocked(room, a + d * ((float)i / n), r)) return false;
            return true;
        }

        public V2 Dir(V2 from, V2 to)
        {
            if (Clear(from, to, 0.4f)) return (to - from).Norm;
            int[] f = Field(CellOf(to));
            int c = CellOf(from), cx = c % W, cy = c / W, best = -1, bv = f[c];
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                    int n = ny * W + nx;
                    if (blocked[n]) continue;
                    if (dx != 0 && dy != 0 && (blocked[cy * W + nx] || blocked[ny * W + cx])) continue;   // sem cortar quina
                    if (f[n] < bv) { bv = f[n]; best = n; }
                }
            return best < 0 ? (to - from).Norm : (Center(best) - from).Norm;
        }

        /// <summary>Distancia em passos de grade ate o alvo (int.MaxValue = inalcancavel).</summary>
        public int Steps(V2 from, V2 to) => Field(CellOf(to))[CellOf(from)];

        int[] Field(int goal)
        {
            if (cache.TryGetValue(goal, out int[] f)) return f;
            f = new int[W * H];
            for (int i = 0; i < f.Length; i++) f[i] = int.MaxValue;
            int g = goal;
            if (blocked[g])   // alvo dentro de obstaculo inflado: parte da celula livre mais perto
            {
                float bd = float.MaxValue; V2 gc = Center(goal);
                for (int i = 0; i < blocked.Length; i++) if (!blocked[i]) { float d = V2.Dist(Center(i), gc); if (d < bd) { bd = d; g = i; } }
            }
            int head = 0, tail = 0;
            f[g] = 0; queue[tail++] = g;
            while (head < tail)
            {
                int c = queue[head++], cx = c % W, cy = c / W, v = f[c] + 1;
                if (cx > 0 && Visit(f, c - 1, v)) queue[tail++] = c - 1;
                if (cx < W - 1 && Visit(f, c + 1, v)) queue[tail++] = c + 1;
                if (cy > 0 && Visit(f, c - W, v)) queue[tail++] = c - W;
                if (cy < H - 1 && Visit(f, c + W, v)) queue[tail++] = c + W;
            }
            cache[goal] = f;
            return f;
        }

        bool Visit(int[] f, int n, int v)
        {
            if (blocked[n] || f[n] <= v) return false;
            f[n] = v;
            return true;
        }
    }
}
