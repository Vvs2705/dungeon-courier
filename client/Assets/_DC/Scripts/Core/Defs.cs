using System;
using System.Globalization;

namespace DC.Core
{
    // ------------------------------------------------------------------ tipos basicos

    /// <summary>Vetor 2D em metros (origem no canto inferior esquerdo da sala, Y para cima). Sem UnityEngine.</summary>
    public struct V2
    {
        public float X, Y;
        public V2(float x, float y) { X = x; Y = y; }
        public static readonly V2 Zero = new V2(0f, 0f);
        public static readonly V2 Up = new V2(0f, 1f);
        /// <summary>8 direcoes unitarias (sem Math.Sin/Cos na simulacao: o resultado delas pode variar entre x64 e ARM64).</summary>
        public static readonly V2[] Dirs8 =
        {
            new V2(1f, 0f), new V2(0.70710678f, 0.70710678f), new V2(0f, 1f), new V2(-0.70710678f, 0.70710678f),
            new V2(-1f, 0f), new V2(-0.70710678f, -0.70710678f), new V2(0f, -1f), new V2(0.70710678f, -0.70710678f),
        };
        public float Len => (float)Math.Sqrt(X * X + Y * Y);
        public V2 Norm { get { float l = Len; return l > 1e-6f ? new V2(X / l, Y / l) : Zero; } }
        /// <summary>Perpendicular a direita (sentido horario).</summary>
        public V2 Right => new V2(Y, -X);
        public static V2 operator +(V2 a, V2 b) => new V2(a.X + b.X, a.Y + b.Y);
        public static V2 operator -(V2 a, V2 b) => new V2(a.X - b.X, a.Y - b.Y);
        public static V2 operator *(V2 a, float k) => new V2(a.X * k, a.Y * k);
        public static float Dot(V2 a, V2 b) => a.X * b.X + a.Y * b.Y;
        public static float Dist(V2 a, V2 b) => (a - b).Len;
        public override string ToString() => X.ToString("0.00", CultureInfo.InvariantCulture) + "," + Y.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>Retangulo alinhado aos eixos: centro e meia-medida (m). Obstaculo solido da sala.</summary>
    public struct Rect
    {
        public V2 C, H;
        public Rect(float cx, float cy, float hx, float hy) { C = new V2(cx, cy); H = new V2(hx, hy); }
        public V2 Closest(V2 p) => new V2(Math.Max(C.X - H.X, Math.Min(p.X, C.X + H.X)), Math.Max(C.Y - H.Y, Math.Min(p.Y, C.Y + H.Y)));
    }

    /// <summary>RNG proprio (xorshift32): mesma semente = mesma partida em qualquer plataforma. Nada de System.Random.</summary>
    public sealed class Rng
    {
        uint s;
        public Rng(uint seed) { s = seed == 0 ? 0x9E3779B9u : seed; }
        public uint Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        /// <summary>[0, 1).</summary>
        public float Float() => (Next() >> 8) * (1f / 16777216f);
        public float Range(float a, float b) => a + (b - a) * Float();
        public int Int(int n) => n <= 0 ? 0 : (int)(Next() % (uint)n);
        /// <summary>Mistura duas chaves numa semente (splitmix32).</summary>
        public static uint Hash(int a, int b)
        {
            unchecked
            {
                uint x = (uint)a * 0x9E3779B1u ^ (uint)b * 0x85EBCA77u;
                x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
                return x == 0 ? 1u : x;
            }
        }
    }

    // ------------------------------------------------------------------ enums (salvos por indice: so crescem no FIM)

    public enum CargoType { Box, Relic, Egg }
    public enum EnemyType { Bat, Thief, Guard, Brute, Ogre }
    public enum Upgrade { Life, Dash, Bag }

    /// <summary>Carrying = carga nas maos (andando ou nos 0,2 s de dedo solto); Walking = sem carga nas maos, andando (ou parado ha
    /// menos de 0,2 s); Planted = parado ha 0,2 s ou mais (pousou a carga e ataca sozinho); Dashing; Dead.</summary>
    public enum CourierState { Carrying, Walking, Planted, Dashing, Dead }

    /// <summary>Held = nas maos do courier; Ground = no chao; Stolen = nas maos de um ladrao; Gone = fora do jogo (fugiu, chocou,
    /// quebrou ou ficou para tras).</summary>
    public enum CargoState { Held, Ground, Stolen, Gone }

    public enum EnemyState { Idle, Chase, Lurk, Grab, Flee, Telegraph, Recover }

    public enum Outcome { Running, Delivered, Died, CargoStolen, CargoBroken, PetDied, Abandoned }

    /// <summary>Dica contextual da FTUE (texto em Hints.Text).</summary>
    public enum Hint { None, PlantToFight, PickUp, Dash, Thief, DoorNeedsCargo, GuardDoor, Chest, WaitPet }

    public static class Hints
    {
        public static string Text(Hint h)
        {
            switch (h)
            {
                case Hint.PlantToFight: return "solte o dedo para pousar e lutar";
                case Hint.PickUp: return "arraste perto da carga para pegá-la";
                case Hint.Dash: return "toque rápido = dash (larga a carga)";
                case Hint.Thief: return "ladrão! ele quer a sua carga";
                case Hint.DoorNeedsCargo: return "a porta só abre com a carga nas mãos";
                case Hint.GuardDoor: return "derrote o guarda para abrir a porta";
                case Hint.Chest: return "fique parado ao lado do baú para abrir";
                case Hint.WaitPet: return "a porta só abre com o filhote por perto";
                default: return "";
            }
        }
    }

    // ------------------------------------------------------------------ eventos

    /// <summary>Tipos de evento. Os 17 primeiros sao os do diario (GDD §14, CONTRATO §9); o resto e so para SFX/efeitos.
    /// A e B por tipo (Pos = onde aconteceu):
    /// ContractStart A=contrato B=resgate aceito(1/0) · ContractEnd A=contrato B=(int)Outcome · RoomEnter A=sala B=camada ·
    /// RouteChoice A=porta B=sala destino · DamageTaken A=dano B=vida restante · CargoIntegrity A=id da carga B=rachaduras ·
    /// OptionalLoot A=moedas B=sala · DeathRoom A=sala · RoomFightChoice A=lutou(1/0) B=sala · CargoDrop A=id B=motivo(0 pousou,
    /// 1 dash, 2 ladrao atingido) · CargoPickup A=id · ThiefGrab A=id da carga B=id do ladrao · CargoRescueOffer A=id B=aceito(1/0) ·
    /// CargoRescueResult A=id B=entregue(1/0) · GuildUpgrade A=(int)Upgrade B=novo nivel · CurrencySource A=moedas B=fonte(0 pagamento,
    /// 1 bau, 2 resgate) · CurrencySink A=moedas B=(int)Upgrade · Shot A=id do alvo · EnemyHit A=id B=vida · EnemyDie A=id B=(int)tipo ·
    /// Telegraph A=id B=(int)tipo · Strike A=id B=(int)tipo · Dash · Plant · DoorBlocked A=porta B=(int)Hint · ThiefSpawn A=id ·
    /// ThiefEscape A=id da carga · Hatch · PetHit B=vida do filhote · CargoBroken A=id · Deliver A=contrato · ChestOpen A=moedas.</summary>
    public enum Ev
    {
        ContractStart, ContractEnd, RoomEnter, RouteChoice, DamageTaken, CargoIntegrity, OptionalLoot, DeathRoom, RoomFightChoice,
        CargoDrop, CargoPickup, ThiefGrab, CargoRescueOffer, CargoRescueResult, GuildUpgrade, CurrencySource, CurrencySink,
        Shot, EnemyHit, EnemyDie, Telegraph, Strike, Dash, Plant, DoorBlocked, ThiefSpawn, ThiefEscape, Hatch, PetHit, CargoBroken,
        Deliver, ChestOpen
    }

    public struct SimEvent
    {
        public Ev Kind; public int A, B; public V2 Pos;
        public SimEvent(Ev kind, int a, int b, V2 pos) { Kind = kind; A = a; B = b; Pos = pos; }

        /// <summary>Nome do evento no diario CSV (GDD §14), ou null se o evento nao vai para o diario.</summary>
        public static string DiaryName(Ev e)
        {
            switch (e)
            {
                case Ev.ContractStart: return "contract_start";
                case Ev.ContractEnd: return "contract_end";
                case Ev.RoomEnter: return "room_enter";
                case Ev.RouteChoice: return "route_choice";
                case Ev.DamageTaken: return "damage_taken";
                case Ev.CargoIntegrity: return "cargo_integrity";
                case Ev.OptionalLoot: return "optional_loot";
                case Ev.DeathRoom: return "death_room";
                case Ev.RoomFightChoice: return "room_fight_choice";
                case Ev.CargoDrop: return "cargo_drop";
                case Ev.CargoPickup: return "cargo_pickup";
                case Ev.ThiefGrab: return "thief_grab";
                case Ev.CargoRescueOffer: return "cargo_rescue_offer";
                case Ev.CargoRescueResult: return "cargo_rescue_result";
                case Ev.GuildUpgrade: return "guild_upgrade";
                case Ev.CurrencySource: return "currency_source";
                case Ev.CurrencySink: return "currency_sink";
                default: return null;
            }
        }
    }

    // ------------------------------------------------------------------ numeros (CONTRATO_V01 §3-§7; hipoteses medidas em BALANCE.md)

    public static class Balance
    {
        public const float Step = 1f / 30f;                 // passo fixo da simulacao
        public const float MaxFrame = 0.25f;                // dt maior que isto e cortado (app voltando do fundo)

        public const float RoomW = 9f, RoomH = 14f;
        public static readonly V2 Entrance = new V2(4.5f, 0.9f);
        public const float DoorHalf = 0.8f;                 // meia-largura da porta na parede de cima
        public static readonly V2 Client = new V2(4.5f, 12.8f);
        public const float DeliverRange = 1.3f;

        public const float CourierR = 0.4f, CargoR = 0.3f;
        public const float CarrySpeed = 4.2f, FreeSpeedMul = 1.15f;
        public const int BaseHp = 6;
        public const float PlantDelay = 0.2f, PlantOffset = 0.6f, PickupRange = 1.5f;
        public const float DashDist = 3f, DashTime = 0.18f, DashCooldown = 1.6f, DashPush = 1.5f, DashPushReach = 0.3f;
        public const float ShotRange = 5f, ShotCooldown = 0.45f, ShotSpeed = 12f; public const int ShotDamage = 1;

        public const float AloneDist = 3f, AloneTime = 8f;  // carga sozinha -> ladrao (§28)
        public const float ThiefGrab = 0.5f, ThiefGrabRange = 0.5f, ThiefFlee = 1.6f, ThiefExit = 1.0f, ThiefLurk = 3f, ThiefSpawnDist = 3f;
        public const int MaxThieves = 3;
        public const float OgreSummon = 10f;
        public const float ChestTime = 1f, ChestRange = 1.2f;
        /// <summary>Reliquia fragil: racha ao CAIR (dash que a larga, ladrao atingido ou empurrado) e a cada golpe no courier que a carrega
        /// (toque de morcego inclusive); pousar nao racha. Quebra com 3.</summary>
        public const int RelicBreak = 3;
        public const int PetHp = 2; public const float PetRange = 3f, PetFollow = 0.9f, PetR = 0.3f;

        /// <summary>Multiplicador por integridade na entrega: 0 rachaduras x1,5; 1 x1,2; 2+ x1,0.</summary>
        public static float IntegrityMul(int cracks) => cracks <= 0 ? 1.5f : cracks == 1 ? 1.2f : 1.0f;

        public const float RescueBonus = 0.5f;              // +50% do pagamento da carga perdida (< 100%: §28)

        /// <summary>Multiplicador em porcentagem inteira (1,4 -> 140): conta de moedas sem erro de float (1,4f em double = 1,3999...).</summary>
        public static int Pct(float mul) => (int)Math.Round(mul * 100f);

        public const int UpgradeMax = 5;
        public const int LifePerLevel = 1; public const float DashCdPerLevel = 0.1f, BagPerLevel = 0.1f;
        /// <summary>Custo do proximo nivel: 60 x 1,6^nivel, arredondado a 5 (60, 95, 155, 245, 395).</summary>
        public static int UpgradeCost(int level) => (int)Math.Round(60.0 * Math.Pow(1.6, level) / 5.0, MidpointRounding.AwayFromZero) * 5;

        /// <summary>X das portas na parede de cima: 1 porta no centro; 2 em 2,5/6,5; 3 em 1,5/4,5/7,5.</summary>
        public static float DoorX(int count, int i) => count <= 1 ? 4.5f : count == 2 ? 2.5f + 4f * i : 1.5f + 3f * i;
    }

    // ------------------------------------------------------------------ defs de conteudo

    public sealed class CargoDef
    {
        public readonly CargoType Type; public readonly string Name; public readonly float PayMul, HatchedMul;
        public CargoDef(CargoType t, string name, float payMul, float hatchedMul = 0f) { Type = t; Name = name; PayMul = payMul; HatchedMul = hatchedMul; }
        public static readonly CargoDef[] All =
        {
            new CargoDef(CargoType.Box, "Caixa", 1.0f),
            new CargoDef(CargoType.Relic, "Relíquia frágil", 1.4f),
            new CargoDef(CargoType.Egg, "Ovo", 1.2f, 0.8f),
        };
    }

    /// <summary>Reach = alcance do golpe a partir do centro do inimigo ate a BORDA do alvo; Trigger = distancia (centro a borda) em que
    /// comeca o telegraph. Locks = enquanto vivo, as portas da sala ficam fechadas (luta obrigatoria).</summary>
    public sealed class EnemyDef
    {
        public readonly EnemyType Type; public readonly string Name; public readonly int Hp, Damage;
        public readonly float Speed, Radius, Reach, Trigger, Tele, Cooldown; public readonly bool Locks;
        public EnemyDef(EnemyType t, string name, int hp, float speed, float radius, int dmg, float reach, float trigger, float tele, float cd, bool locks)
        { Type = t; Name = name; Hp = hp; Speed = speed; Radius = radius; Damage = dmg; Reach = reach; Trigger = trigger; Tele = tele; Cooldown = cd; Locks = locks; }
        public static readonly EnemyDef[] All =
        {
            new EnemyDef(EnemyType.Bat,   "Morcego", 2,  2.4f, 0.30f, 1, 0f,   0f,   0f,   1.0f, false),
            new EnemyDef(EnemyType.Thief, "Ladrão",  3,  2.0f, 0.35f, 0, 0f,   0f,   0f,   0f,   false),
            new EnemyDef(EnemyType.Guard, "Guarda",  5,  0f,   0.45f, 1, 1.2f, 1.2f, 0.5f, 1.0f, true),
            new EnemyDef(EnemyType.Brute, "Bruto",   6,  1.6f, 0.55f, 2, 1.2f, 1.0f, 0.6f, 1.2f, false),
            new EnemyDef(EnemyType.Ogre,  "Ogro",    30, 1.2f, 0.90f, 3, 2.0f, 1.6f, 1.0f, 2.0f, true),
        };
    }

    public struct Spawn
    {
        public EnemyType Type; public V2 Pos;
        public Spawn(EnemyType t, float x, float y) { Type = t; Pos = new V2(x, y); }
    }

    /// <summary>Sala 9 x 14 m. Entrada embaixo no centro (Balance.Entrance); as portas da parede de cima saem do contrato (uma por sala
    /// da proxima camada). Threat = caveiras no icone da porta (0-3); ChestCoins = moedas do bau (0 = sem bau).</summary>
    public sealed class RoomDef
    {
        public readonly int Id; public readonly string Code, Name, Lesson; public readonly int Threat;
        public readonly Rect[] Obstacles; public readonly Spawn[] Spawns;
        public readonly V2 ChestPos; public readonly int ChestCoins; public readonly V2 RescueSpot;
        public bool Delivery => Code == "D1";
        public bool Boss => Code == "B1";
        public bool Normal => Code[0] == 'R';
        public RoomDef(int id, string code, string name, string lesson, int threat, Rect[] obstacles, Spawn[] spawns, V2 rescueSpot, V2 chestPos = default, int chestCoins = 0)
        { Id = id; Code = code; Name = name; Lesson = lesson; Threat = threat; Obstacles = obstacles; Spawns = spawns; RescueSpot = rescueSpot; ChestPos = chestPos; ChestCoins = chestCoins; }
    }

    /// <summary>Contrato = camadas de salas (cada porta leva a uma sala da camada seguinte) terminando na entrega (D1). Pay = pagamento
    /// base em Coins antes dos multiplicadores de carga, integridade e Bolsa. EggTarget = tempo-alvo do ovo (s; 0 = nao e ovo).</summary>
    public sealed class ContractDef
    {
        public readonly int Index; public readonly string Code, Note; public readonly CargoType Cargo; public readonly int Pay; public readonly float EggTarget;
        public readonly int[][] Layers;
        public ContractDef(int index, CargoType cargo, int pay, float eggTarget, string note, params int[][] layers)
        {
            Index = index; Code = "C" + (index + 1); Cargo = cargo; Pay = pay; EggTarget = eggTarget; Note = note;
            Layers = new int[layers.Length + 1][];
            Array.Copy(layers, Layers, layers.Length);
            Layers[layers.Length] = new[] { Content.D1 };
        }
    }

    /// <summary>Conteudo da v0.1 como dados. Ids de sala e indices de contrato sao estaveis (vao para o save): so crescem no fim.</summary>
    public static class Content
    {
        public const int R1 = 0, R2 = 1, R3 = 2, R4 = 3, R5 = 4, R6 = 5, B1 = 6, D1 = 7;

        static Rect O(float cx, float cy, float hx, float hy) => new Rect(cx, cy, hx, hy);
        static Spawn S(EnemyType t, float x, float y) => new Spawn(t, x, y);
        static V2 P(float x, float y) => new V2(x, y);
        const EnemyType Bat = EnemyType.Bat, Thief = EnemyType.Thief, Guard = EnemyType.Guard, Brute = EnemyType.Brute, Ogre = EnemyType.Ogre;

        // Icone de ameaca (Threat) = ordem do dano medio por visita do bot (BalanceTests.IconeDeAmeaca_AcompanhaODanoMedido):
        // 1 = R1, R3 · 2 = R5, R2 · 3 = R6, R4. B1 fica 3 (chefe, porta unica, fora da trava).
        // Regras de desenho (medidas em CoreTests.LevelDesign_*): nada solido na faixa das portas (y > 12,2) nem na entrada; spawn a 4 m+ da
        // entrada; toda porta, bau e ponto de resgate alcancaveis; so Guarda/Ogro obrigam a lutar, o resto se atravessa andando.
        public static readonly RoomDef[] Rooms =
        {
            new RoomDef(R1, "R1", "Sala do Guarda", "FTUE: solte o dedo para pousar e lutar; voltar a andar pega a carga", 1,
                new[] { O(2.0f, 6.5f, 0.6f, 0.6f), O(7.0f, 6.5f, 0.6f, 0.6f) },
                new[] { S(Guard, 4.5f, 12.4f) }, P(2.0f, 3.0f)),
            new RoomDef(R2, "R2", "Sala do 1º Ladrão", "FTUE: o ladrão corre para a carga pousada; lute perto dela ou leve-a; dash", 2,
                new[] { O(4.5f, 7.0f, 1.4f, 0.5f) },
                new[] { S(Thief, 7.8f, 4.2f), S(Bat, 2.0f, 11.0f), S(Bat, 7.0f, 11.5f) }, P(2.0f, 3.0f)),
            new RoomDef(R3, "R3", "Rota longa com baú", "bifurcação: baú custa parar 1 s com a carga no chão", 1,
                new[] { O(3.0f, 5.0f, 3.0f, 0.3f), O(6.0f, 9.5f, 3.0f, 0.3f) },
                new[] { S(Thief, 8.0f, 7.5f), S(Bat, 4.5f, 12.0f) }, P(1.5f, 2.5f), P(0.9f, 11.0f), 25),
            new RoomDef(R4, "R4", "Enxame", "morcegos enchem a sala: atravessar correndo ou pousar e limpar", 3,
                new[] { O(2.5f, 5.5f, 0.5f, 0.5f), O(6.5f, 5.5f, 0.5f, 0.5f), O(4.5f, 9.0f, 0.8f, 0.8f) },
                new[] { S(Bat, 1.5f, 11.0f), S(Bat, 3.5f, 12.0f), S(Bat, 5.5f, 12.0f), S(Bat, 7.5f, 11.0f) }, P(1.5f, 2.5f), P(8.2f, 3.0f), 20),
            new RoomDef(R5, "R5", "Sala do Bruto", "telegraph: saia do golpe; golpe pelas costas acerta a carga (1 morcego só: ver BALANCE.md)", 2,
                new[] { O(2.2f, 7.0f, 1.2f, 0.4f), O(6.8f, 7.0f, 1.2f, 0.4f) },
                new[] { S(Brute, 4.5f, 10.5f), S(Bat, 7.5f, 12.0f) }, P(2.0f, 2.5f), P(0.8f, 9.5f), 35),
            new RoomDef(R6, "R6", "Guarda e Ladrão", "luta obrigatória com a carga no chão: o ladrão vem buscá-la", 3,
                new[] { O(4.5f, 6.0f, 1.6f, 0.4f) },
                new[] { S(Guard, 4.5f, 12.4f), S(Thief, 1.0f, 8.5f), S(Bat, 7.5f, 10.0f) }, P(7.0f, 2.5f)),
            new RoomDef(B1, "B1", "Covil do Ogro", "chefe: pancada em área (2 m) e chama ladrões a cada 10 s", 3,
                new[] { O(2.0f, 4.5f, 0.5f, 0.5f), O(7.0f, 4.5f, 0.5f, 0.5f), O(2.0f, 9.5f, 0.5f, 0.5f), O(7.0f, 9.5f, 0.5f, 0.5f) },
                new[] { S(Ogre, 4.5f, 10.5f) }, P(2.0f, 2.5f)),
            new RoomDef(D1, "D1", "Entrega", "o cliente espera no topo", 0,
                new[] { O(2.0f, 7.0f, 0.5f, 0.5f), O(7.0f, 7.0f, 0.5f, 0.5f) },
                new Spawn[0], P(2.0f, 2.5f)),
        };

        public static readonly ContractDef[] Contracts =
        {
            new ContractDef(0, CargoType.Box,   40, 0f,   "FTUE: pousar para lutar; 1ª bifurcação; 1º ladrão", new[] { R1 }, new[] { R2, R3 }),
            new ContractDef(1, CargoType.Box,   45, 0f,   "1º upgrade da guilda liberado antes", new[] { R2 }, new[] { R4, R3 }),
            new ContractDef(2, CargoType.Egg,   50, 30f,  "ovo: tempo-alvo 30 s (CONTRATO dizia 90 s; ver BALANCE.md)", new[] { R1 }, new[] { R2, R4 }),
            new ContractDef(3, CargoType.Box,   60, 0f,   "Bruto aparece", new[] { R5 }, new[] { R4, R3 }, new[] { R2, R1 }),
            new ContractDef(4, CargoType.Relic, 65, 0f,   "relíquia frágil", new[] { R1 }, new[] { R5, R3 }, new[] { R4, R2 }),
            new ContractDef(5, CargoType.Egg,   70, 45f,  "Guarda + Ladrão juntos (1ª derrota provável); ovo 45 s", new[] { R6 }, new[] { R4, R3 }, new[] { R5, R2 }),
            new ContractDef(6, CargoType.Relic, 80, 0f,   "relíquia com Bruto e Guarda", new[] { R4 }, new[] { R6, R5 }, new[] { R3, R2 }),
            new ContractDef(7, CargoType.Box,   110, 0f,  "chefe", new[] { R3 }, new[] { R5, R6 }, new[] { R4, R2 }, new[] { B1 }),
        };
    }
}
