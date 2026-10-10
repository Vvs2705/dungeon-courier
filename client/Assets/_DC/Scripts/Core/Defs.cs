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
        /// <summary>Distancia do ponto p ao segmento a-b.</summary>
        public static float SegDist(V2 p, V2 a, V2 b)
        {
            V2 ab = b - a; float l2 = Dot(ab, ab);
            float t = l2 < 1e-8f ? 0f : Math.Max(0f, Math.Min(1f, Dot(p - a, ab) / l2));
            return Dist(p, a + ab * t);
        }
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

    public enum CargoType { Box, Relic, Egg, Cake, Message, Heavy, Barrel }
    public enum EnemyType { Bat, Thief, Guard, Brute, Ogre, Archer, Slime, Captain }
    public enum Upgrade { Life, Dash, Bag, Pack }
    public enum Weapon { Sling, Boomerang, Bomb }
    public enum Perk { LightStep, QuickPlant, DoubleDash, Padded, QuickHand, Magnet, ExtraHeart, Lookout, IronBoot, ChestLuck }
    public enum ContractKind { Campaign, Daily, Code }
    /// <summary>Quando o reforco da sala chega: Half = o courier passou da metade da sala; CargoDown = a carga do contrato ficou
    /// Balance.WaveDownTime s no chao nesta sala.</summary>
    public enum RoomTrigger { None, Half, CargoDown }

    /// <summary>Icone de risco para a CARGA na porta (alem das caveiras, que medem o risco para o courier): Thief = ladrao/Capitao
    /// (leva a carga do chao); Archer = flecha (acerta a carga no chao); Smash = pancada de Bruto/Ogro (acerta a carga no chao).</summary>
    [Flags] public enum CargoRisk { None = 0, Thief = 1, Archer = 2, Smash = 4 }

    /// <summary>Carrying = carga nas maos (andando ou nos 0,2 s de dedo solto); Walking = sem carga nas maos, andando (ou parado ha
    /// menos de 0,2 s); Planted = parado ha 0,2 s ou mais (pousou a carga e ataca sozinho); Dashing; Dead.</summary>
    public enum CourierState { Carrying, Walking, Planted, Dashing, Dead }

    /// <summary>Held = nas maos do courier; Ground = no chao; Stolen = nas maos de um ladrao; Gone = fora do jogo (fugiu, chocou,
    /// quebrou, explodiu ou ficou para tras).</summary>
    public enum CargoState { Held, Ground, Stolen, Gone }

    public enum EnemyState { Idle, Chase, Lurk, Grab, Flee, Telegraph, Recover }

    public enum Outcome { Running, Delivered, Died, CargoStolen, CargoBroken, PetDied, Abandoned, TimeUp, Exploded }

    /// <summary>Dica contextual da FTUE (texto em Hints.Text).</summary>
    public enum Hint { None, PlantToFight, PickUp, Dash, Thief, DoorNeedsCargo, GuardDoor, Chest, WaitPet, DragToWalk, CakeNoDash, BarrelFuse, ArcherAim, SecretWall, MessageDoor }

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
                case Hint.DragToWalk: return "arraste o dedo para andar com a carga";
                case Hint.CakeNoDash: return "o bolo não aguenta dash: só ande";
                case Hint.BarrelFuse: return "barril no chão explode em 3 s: pegue de volta!";
                case Hint.ArcherAim: return "arqueiro mirando: saia da linha";
                case Hint.SecretWall: return "parede rachada: atravesse com a carga";
                case Hint.MessageDoor: return "a mensagem abre a porta trancada";
                default: return "";
            }
        }
    }

    // ------------------------------------------------------------------ eventos

    /// <summary>Tipos de evento. Os 17 primeiros sao os do diario (GDD §14, CONTRATO §9); o resto e so para SFX/efeitos, menos os do
    /// diario da v1 (ContractStars, PerkPick, WeaponEquip, DailyStart, DailyEnd, CodeStart). A e B por tipo (Pos = onde aconteceu):
    /// ContractStart A=contrato(-1 gerado) B=resgate aceito(1/0) · ContractEnd A=contrato B=(int)Outcome · RoomEnter A=sala B=camada ·
    /// RouteChoice A=porta B=sala destino · DamageTaken A=dano B=vida restante · CargoIntegrity A=id da carga B=rachaduras ·
    /// OptionalLoot A=moedas B=sala · DeathRoom A=sala · RoomFightChoice A=lutou(1/0) B=sala · CargoDrop A=id B=motivo(0 pousou,
    /// 1 dash, 2 ladrao atingido) · CargoPickup A=id · ThiefGrab A=id da carga B=id do ladrao · CargoRescueOffer A=id B=aceito(1/0) ·
    /// CargoRescueResult A=id B=entregue(1/0) · GuildUpgrade A=(int)Upgrade B=novo nivel · CurrencySource A=moedas B=fonte(0 pagamento,
    /// 1 bau, 2 resgate, 3 bonus do dia) · CurrencySink A=moedas B=(int)Upgrade ou 10+(int)Weapon · Shot A=id do alvo B=(int)Weapon ·
    /// EnemyHit A=id B=vida · EnemyDie A=id B=(int)tipo · Telegraph A=id B=(int)tipo · Strike A=id B=(int)tipo · Dash · Plant ·
    /// DoorBlocked A=porta B=(int)Hint · ThiefSpawn A=id · ThiefEscape A=id da carga · Hatch · PetHit B=vida do filhote · CargoBroken A=id ·
    /// Deliver A=contrato · ChestOpen A=moedas ·
    /// (v1) PerkOffer A=camada · PerkPick A=(int)Perk B=camada · WeaponEquip A=(int)Weapon B=nivel · WeaponUpgrade A=(int)Weapon B=novo
    /// nivel · WeaponUnlock A=(int)Weapon B=estrelas · ContractStars A=estrelas(0-3) B=contrato(-1 gerado) · DailyStart A=data AAAAMMDD ·
    /// DailyEnd A=data B=(int)Outcome · CodeStart A=payload do codigo · RoomEvent A=sala B=inimigos do reforco · Explosion A=fonte(0 barril,
    /// 1 bombinha) B=raio em cm · ArrowShot A=id do arqueiro · ArrowHit A=alvo(0 courier, 1 carga, 2 obstaculo/parede, 3 filhote) ·
    /// PuddleSpawn A=id do limo · CakeWobble (toque rapido com o bolo: sem dash) · SecretFound A=sala secreta · FuseLit A=id do barril
    /// (foi ao chao: explode em 3 s) · TimeWarning A=segundos restantes · MessageDoor A=porta (destrancada pela mensagem) · Stun A=id ·
    /// CaptainCall A=id do Capitao B=ladroes chamados.</summary>
    public enum Ev
    {
        ContractStart, ContractEnd, RoomEnter, RouteChoice, DamageTaken, CargoIntegrity, OptionalLoot, DeathRoom, RoomFightChoice,
        CargoDrop, CargoPickup, ThiefGrab, CargoRescueOffer, CargoRescueResult, GuildUpgrade, CurrencySource, CurrencySink,
        Shot, EnemyHit, EnemyDie, Telegraph, Strike, Dash, Plant, DoorBlocked, ThiefSpawn, ThiefEscape, Hatch, PetHit, CargoBroken,
        Deliver, ChestOpen,
        PerkOffer, PerkPick, WeaponEquip, WeaponUpgrade, WeaponUnlock, ContractStars, DailyStart, DailyEnd, CodeStart,
        RoomEvent, Explosion, ArrowShot, ArrowHit, PuddleSpawn, CakeWobble, SecretFound, FuseLit, TimeWarning, MessageDoor, Stun, CaptainCall
    }

    public struct SimEvent
    {
        public Ev Kind; public int A, B; public V2 Pos;
        public SimEvent(Ev kind, int a, int b, V2 pos) { Kind = kind; A = a; B = b; Pos = pos; }

        /// <summary>Nome do evento no diario CSV (GDD §14 + CONTRATO_V1 §10), ou null se o evento nao vai para o diario.</summary>
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
                case Ev.ContractStars: return "contract_stars";
                case Ev.PerkPick: return "perk_pick";
                case Ev.WeaponEquip: return "weapon_equip";
                case Ev.DailyStart: return "daily_start";
                case Ev.DailyEnd: return "daily_end";
                case Ev.CodeStart: return "code_start";
                default: return null;
            }
        }
    }

    // ------------------------------------------------------------------ numeros (CONTRATO_V01 §3-§7 + CONTRATO_V1; hipoteses medidas em BALANCE.md)

    public static class Balance
    {
        public const float Step = 1f / 30f;                 // passo fixo da simulacao
        public const float MaxFrame = 0.25f;                // dt maior que isto e cortado (app voltando do fundo)

        public const float RoomW = 9f, RoomH = 14f;
        public static readonly V2 Entrance = new V2(4.5f, 0.9f);
        public const float DoorHalf = 0.8f;                 // meia-largura da porta na parede de cima (e da parede rachada)
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

        // guilda (CONTRATO_V1 §8): 4 linhas x 8 niveis
        public const int UpgradeLines = 4;
        public const int UpgradeMax = 8;
        public const int LifePerLevel = 1; public const float DashCdPerLevel = 0.06f, BagPerLevel = 0.1f;
        /// <summary>Mochila: +0,15 m no raio de pegar e -0,02 s no pouso por nivel.</summary>
        public const float PackRangePerLevel = 0.15f, PackPlantPerLevel = 0.02f, MinPlantDelay = 0.04f;
        /// <summary>Custo do proximo nivel: 60 x 1,5^nivel, arredondado a 5 (60, 90, 135, 205, 305, 455, 685, 1025).</summary>
        public static int UpgradeCost(int level) => (int)Math.Round(60.0 * Math.Pow(1.5, level) / 5.0, MidpointRounding.AwayFromZero) * 5;

        /// <summary>X das portas na parede de cima: 1 porta no centro; 2 em 2,5/6,5; 3 em 1,5/4,5/7,5.</summary>
        public static float DoorX(int count, int i) => count <= 1 ? 4.5f : count == 2 ? 2.5f + 4f * i : 1.5f + 3f * i;

        // ---- v1.0-beta (CONTRATO_V1 §3-§8)
        /// <summary>Maior vida maxima possivel (guilda no maximo + Coracao extra): tamanho da fileira de coracoes na View.</summary>
        public const int MaxHpCap = BaseHp + LifePerLevel * UpgradeMax + PerkHeartHp;

        // cargas
        public const float HeavySpeedMul = 0.75f, HeavyPlant = 0.2f, HeavyPick = 0.2f;      // caixa pesada
        public const float BarrelFuse = 3f, BarrelRadius = 2.5f; public const int BarrelDamage = 3, BarrelEnemyDamage = 6;
        public const float CakeWobble = 0.6f;                                                // bolo: balanco do dash negado
        public const float MessageWarn = 10f;                                                // mensagem: aviso de tempo
        // inimigos novos
        public const float ArrowSpeed = 8f, ArrowRange = 9f, ArrowR = 0.1f, ArcherKeep = 3f;  // arqueiro recua se o courier chega a 3 m
        public const int ArrowDamage = 1;
        public const float PuddleTime = 4f, PuddleR = 1.0f, PuddleSlow = 0.4f;              // limo: poca de 4 s, -40% de velocidade
        /// <summary>O limo rebate 1,5 m depois de cada toque (recarga 1,6 s): 1 de dano a cada ~1,6 s em vez de grudar; parado da para atirar.</summary>
        public const float SlimeRecoil = 1.5f;
        public const float CaptainFlee = 1.9f; public const int CaptainDropHits = 2, CaptainCalls = 2;
        // eventos de sala (reforco com teto)
        public const int MaxAlive = 10; public const float WaveDownTime = 3f;
        /// <summary>Parede rachada: so atravessa empurrando contra ela (componente do movimento para a parede >= 0,7), nao passando rente.</summary>
        public const float SecretPush = 0.7f;
        /// <summary>Teto de reforcos por visita de sala: 1 nos contratos C1-C4; 2 do C5 em diante (a 2a so vem quando a 1a cai com o
        /// courier ainda na sala e a condicao vale de novo).</summary>
        public static int MaxWaves(int tier) => tier >= 4 ? 2 : 1;
        /// <summary>O reforco chega em fila: 1 inimigo a cada WaveGap s (na emboscada, a porta so abre depois do ultimo).</summary>
        public const float WaveGap = 1.0f;
        // armas
        public const float BoomerangSpeed = 9f, BoomerangLife = 2.5f, BombFlight = 0.6f;
        // perks
        public const int PerkOfferSize = 3;
        public const float PerkLightStep = 1.1f, PerkQuickPlant = 0.1f, PerkQuickHand = 1.2f, PerkMagnetRange = 2.7f, PerkLookout = 4f;
        public const float PerkIronPush = 2.5f, PerkIronStun = 0.6f, PerkChestLuck = 1.3f;
        public const int PerkHeartHp = 2;
        /// <summary>Camadas depois das quais vem a escolha de perk (entrando na 3a e na 5a sala do caminho).</summary>
        public static bool PerkLayer(int layerEntered) => layerEntered == 2 || layerEntered == 4;
        // contrato do dia e por codigo
        public const int DailyBonus = 100;
        /// <summary>Contrato do dia e por codigo abrem depois dos 3 tutoriais (C1-C3).</summary>
        public const int OpenAfter = 3;
        /// <summary>Tempos dos contratos gerados (dia/codigo), por sala ate a entrega (inclui D1): bot no meio da campanha leva ~11,4 s/sala
        /// (mensagem ~5 s/sala); 3a estrela = 1,6x, ovo = 2x, mensagem = 2,5x (BALANCE.md v1).</summary>
        public const float GenParPerRoom = 18f, GenEggPerRoom = 23f, GenLimitPerRoom = 12f;
        public const int GenTierBase = 10;
        /// <summary>Vida dos inimigos por nivel de contrato: x(1 + TierHp x (nivel - 1)) do C4 em diante (C4 = x1,24; C24 = x3,64); morcego e limo ficam
        /// fora (perseguidores frageis). Alonga a luta sem aumentar o dano por segundo, acompanhando o dano das armas e da guilda (alvo de 1,5-3 min
        /// por contrato do bot, CONTRATO_V1 §2).</summary>
        public const float TierHp = 0.12f;
        public static float HpMul(int tier) => tier < 3 ? 1f : 1f + TierHp * (tier - 1);
        /// <summary>Cura ao entrar numa sala nova (porta ou parede rachada): segura contratos de 5-7 salas com reforcos.</summary>
        public const int RoomHeal = 1;
    }

    // ------------------------------------------------------------------ defs de conteudo

    public sealed class CargoDef
    {
        public readonly CargoType Type; public readonly string Name, Verb; public readonly float PayMul, HatchedMul;
        public CargoDef(CargoType t, string name, float payMul, float hatchedMul = 0f, string verb = "")
        { Type = t; Name = name; PayMul = payMul; HatchedMul = hatchedMul; Verb = verb; }
        public static readonly CargoDef[] All =
        {
            new CargoDef(CargoType.Box, "Caixa", 1.0f, 0f, "nenhum: aprenda a carregar"),
            new CargoDef(CargoType.Relic, "Relíquia frágil", 1.4f, 0f, "racha com golpe e queda; 3 rachaduras = quebra"),
            new CargoDef(CargoType.Egg, "Ovo", 1.2f, 0.8f, "choca no tempo-alvo; explosão perto racha"),
            new CargoDef(CargoType.Cake, "Bolo de casamento", 1.3f, 0f, "proíbe o dash"),
            new CargoDef(CargoType.Message, "Mensagem urgente", 1.25f, 0f, "cronômetro; abre portas trancadas"),
            new CargoDef(CargoType.Heavy, "Caixa pesada", 1.6f, 0f, "-25% de velocidade; pousar e pegar mais lentos"),
            new CargoDef(CargoType.Barrel, "Barril de pólvora", 1.5f, 0f, "no chão por 3 s explode"),
        };
    }

    /// <summary>Reach = alcance do golpe a partir do centro do inimigo ate a BORDA do alvo (arqueiro: alcance do tiro); Trigger = distancia
    /// (centro a borda) em que comeca o telegraph. Locks = enquanto vivo, as portas da sala ficam fechadas (luta obrigatoria).</summary>
    public sealed class EnemyDef
    {
        public readonly EnemyType Type; public readonly string Name; public readonly int Hp, Damage;
        public readonly float Speed, Radius, Reach, Trigger, Tele, Cooldown; public readonly bool Locks;
        public EnemyDef(EnemyType t, string name, int hp, float speed, float radius, int dmg, float reach, float trigger, float tele, float cd, bool locks)
        { Type = t; Name = name; Hp = hp; Speed = speed; Radius = radius; Damage = dmg; Reach = reach; Trigger = trigger; Tele = tele; Cooldown = cd; Locks = locks; }
        /// <summary>Chefe (Ogro, Capitao): a mensagem nao abre a porta dele e o dash do Ogro nao empurra.</summary>
        public bool Boss => Type == EnemyType.Ogre || Type == EnemyType.Captain;
        /// <summary>Risco que este inimigo traz para a carga (icone da porta).</summary>
        public CargoRisk Risk => Type == EnemyType.Thief || Type == EnemyType.Captain ? CargoRisk.Thief : Type == EnemyType.Archer ? CargoRisk.Archer
                               : Type == EnemyType.Brute || Type == EnemyType.Ogre ? CargoRisk.Smash : CargoRisk.None;
        public static readonly EnemyDef[] All =
        {
            new EnemyDef(EnemyType.Bat,     "Morcego",        2,  2.4f, 0.30f, 1, 0f,   0f,   0f,   1.0f, false),
            new EnemyDef(EnemyType.Thief,   "Ladrão",         3,  2.0f, 0.35f, 0, 0f,   0f,   0f,   0f,   false),
            new EnemyDef(EnemyType.Guard,   "Guarda",         16, 0f,   0.45f, 1, 1.2f, 1.2f, 0.5f, 1.0f, true),
            new EnemyDef(EnemyType.Brute,   "Bruto",          6,  1.6f, 0.55f, 2, 1.2f, 1.0f, 0.6f, 1.2f, false),
            new EnemyDef(EnemyType.Ogre,    "Ogro",           45, 1.2f, 0.90f, 3, 2.0f, 1.6f, 1.0f, 2.0f, true),
            new EnemyDef(EnemyType.Archer,  "Arqueiro",       3,  1.4f, 0.35f, 1, 7.0f, 7.0f, 0.8f, 2.2f, false),
            new EnemyDef(EnemyType.Slime,   "Limo",           4,  1.2f, 0.40f, 1, 0f,   0f,   0f,   1.6f, false),
            new EnemyDef(EnemyType.Captain, "Capitão Ladrão", 24, 2.6f, 0.50f, 1, 1.0f, 0.9f, 0.5f, 1.2f, true),
        };
    }

    /// <summary>Arma (CONTRATO_V1 §5). Indices [0..2] = nivel 1..3. Cost[n] = preco para subir AO nivel n+1 (Cost[0] = 0: o nivel 1 vem com
    /// o desbloqueio por estrelas). Range = alcance do alvo; Radius = area da bombinha.</summary>
    public sealed class WeaponDef
    {
        public readonly Weapon Type; public readonly string Name, Text; public readonly int Stars;
        public readonly int[] Damage, Cost; public readonly float[] Cooldown; public readonly float Range, Radius;
        public WeaponDef(Weapon t, string name, string text, int stars, int[] dmg, float[] cd, float range, float radius, int[] cost)
        { Type = t; Name = name; Text = text; Stars = stars; Damage = dmg; Cooldown = cd; Range = range; Radius = radius; Cost = cost; }
        public const int MaxLevel = 3;
        public static readonly WeaponDef[] All =
        {
            new WeaponDef(Weapon.Sling, "Estilingue", "tiro teleguiado", 0, new[] { 1, 1, 2 }, new[] { 0.45f, 0.35f, 0.55f }, 5f, 0f, new[] { 0, 300, 700 }),
            new WeaponDef(Weapon.Boomerang, "Bumerangue", "vai e volta em linha e atravessa inimigos", 12, new[] { 1, 1, 2 }, new[] { 0.8f, 0.62f, 1.0f }, 5f, 0f, new[] { 0, 400, 900 }),
            new WeaponDef(Weapon.Bomb, "Bombinha", "arco com dano em área; racha o ovo e detona o barril", 24, new[] { 2, 2, 3 }, new[] { 1.1f, 0.85f, 1.05f }, 5f, 1.5f, new[] { 0, 500, 1100 }),
        };
    }

    /// <summary>Perk de run (CONTRATO_V1 §6): so vale no contrato em curso; sem pagamento.</summary>
    public sealed class PerkDef
    {
        public readonly Perk Type; public readonly string Name, Text;
        public PerkDef(Perk t, string name, string text) { Type = t; Name = name; Text = text; }
        public static readonly PerkDef[] All =
        {
            new PerkDef(Perk.LightStep, "Passo leve", "+10% de velocidade carregando"),
            new PerkDef(Perk.QuickPlant, "Pouso rápido", "pousa em 0,1 s"),
            new PerkDef(Perk.DoubleDash, "Dash duplo", "2 cargas de dash"),
            new PerkDef(Perk.Padded, "Carga acolchoada", "o 1º golpe na carga é ignorado"),
            new PerkDef(Perk.QuickHand, "Mão rápida", "+20% de cadência"),
            new PerkDef(Perk.Magnet, "Ímã", "abre baú de mais longe"),
            new PerkDef(Perk.ExtraHeart, "Coração extra", "+2 de vida máxima e cura 2"),
            new PerkDef(Perk.Lookout, "Vigia", "o ladrão da carga sozinha demora +4 s"),
            new PerkDef(Perk.IronBoot, "Bota de ferro", "dash empurra mais e atordoa 0,6 s"),
            new PerkDef(Perk.ChestLuck, "Sorte de baú", "+30% de moedas de baú"),
        };
    }

    public struct Spawn
    {
        public EnemyType Type; public V2 Pos;
        public Spawn(EnemyType t, float x, float y) { Type = t; Pos = new V2(x, y); }
    }

    /// <summary>Sala 9 x 14 m. Entrada embaixo no centro (Balance.Entrance); as portas da parede de cima saem do contrato (uma por sala
    /// da proxima camada). Threat = caveiras no icone da porta (0-3); Risk = icone de risco para a carga; ChestCoins = moedas do bau
    /// (0 = sem bau). Wave = reforco (1 por visita) quando WaveTrigger acontece; WaveLocks = as portas fecham ate o reforco morrer.
    /// Secret = sala secreta atras da parede rachada em SecretPos (parede lateral), ou -1.</summary>
    public sealed class RoomDef
    {
        public readonly int Id; public readonly string Code, Name, Lesson; public readonly int Threat;
        public readonly Rect[] Obstacles; public readonly Spawn[] Spawns;
        public readonly V2 ChestPos; public readonly int ChestCoins; public readonly V2 RescueSpot;
        public readonly Spawn[] Wave; public readonly RoomTrigger WaveTrigger; public readonly bool WaveLocks;
        public readonly int Secret; public readonly V2 SecretPos;
        public readonly CargoRisk Risk;
        public bool Delivery => Code == "D1";
        public bool Boss => Code == "B1";
        public bool Mini => Code == "M1";
        public bool IsSecret => Code[0] == 'S';
        public bool Normal => Code[0] == 'R';
        public RoomDef(int id, string code, string name, string lesson, int threat, Rect[] obstacles, Spawn[] spawns, V2 rescueSpot, V2 chestPos = default, int chestCoins = 0,
                       Spawn[] wave = null, RoomTrigger trigger = RoomTrigger.None, bool waveLocks = false, int secret = -1, V2 secretPos = default)
        {
            Id = id; Code = code; Name = name; Lesson = lesson; Threat = threat; Obstacles = obstacles; Spawns = spawns; RescueSpot = rescueSpot; ChestPos = chestPos; ChestCoins = chestCoins;
            Wave = wave ?? new Spawn[0]; WaveTrigger = wave == null ? RoomTrigger.None : trigger; WaveLocks = waveLocks; Secret = secret; SecretPos = secretPos;
            foreach (Spawn s in Spawns) Risk |= EnemyDef.All[(int)s.Type].Risk;
            foreach (Spawn s in Wave) Risk |= EnemyDef.All[(int)s.Type].Risk;
            if (code == "B1") Risk |= CargoRisk.Thief;   // o Ogro chama ladroes
        }
    }

    /// <summary>Contrato = camadas de salas (cada porta leva a uma sala da camada seguinte) terminando na entrega (D1). Pay = pagamento
    /// base em Coins antes dos multiplicadores de carga, integridade e Bolsa. EggTarget = tempo-alvo do ovo (s; 0 = nao e ovo).
    /// ParTime = tempo-alvo da 3a estrela; TimeLimit = cronometro da mensagem (0 = sem). Gerados (dia/codigo): Index = -1.</summary>
    public sealed class ContractDef
    {
        public readonly int Index; public readonly string Code, Note; public readonly CargoType Cargo; public readonly int Pay; public readonly float EggTarget;
        public readonly int[][] Layers;
        public ContractKind Kind { get; internal set; }
        public string Name { get; internal set; }
        public float ParTime { get; internal set; }
        public float TimeLimit { get; internal set; }
        /// <summary>Codigo de 6 caracteres para compartilhar (dia e codigo); null na campanha.</summary>
        public string ShareCode { get; internal set; }
        /// <summary>Payload do codigo (0..ContractCode.MaxPayload) nos gerados; Date = AAAAMMDD do contrato do dia (0 nos outros).</summary>
        public int Payload { get; internal set; }
        public int Date { get; internal set; }
        /// <summary>Nivel do contrato: a vida dos inimigos e x Balance.HpMul(Tier). Campanha = indice; gerado = pelas camadas.</summary>
        public int Tier { get; internal set; }
        /// <summary>Reforcos de sala ligados (os tutoriais C1-C3 nao tem).</summary>
        public bool Waves { get; internal set; } = true;
        public bool HasTimer => TimeLimit > 0f;
        public ContractDef(int index, CargoType cargo, int pay, float eggTarget, string note, params int[][] layers)
        {
            Index = index; Code = "C" + (index + 1); Cargo = cargo; Pay = pay; EggTarget = eggTarget; Note = note; Name = note; Tier = Math.Max(0, index);
            Layers = new int[layers.Length + 1][];
            Array.Copy(layers, Layers, layers.Length);
            Layers[layers.Length] = new[] { Content.D1 };
        }
        internal ContractDef(string code, CargoType cargo, int pay, float eggTarget, string note, int[][] layers) : this(-1, cargo, pay, eggTarget, note, layers) { Code = code; }
        /// <summary>Salas ate a entrega, contando a D1.</summary>
        public int Rooms => Layers.Length;
    }

    /// <summary>Codigo de contrato (CONTRATO_V1 §7): 6 caracteres do alfabeto de 32 simbolos (A-Z sem I e O, 2-9) = 30 bits = 25 de
    /// payload + 5 de verificacao (pega erro de digitacao). Ida e volta exata: TryDecode(Encode(p)) == p.</summary>
    public static class ContractCode
    {
        public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        public const int Length = 6;
        public const int MaxPayload = (1 << 25) - 1;

        static int Check(int payload) => (int)(Rng.Hash(payload, 0xC0DE) & 31u);

        public static string Encode(int payload)
        {
            payload &= MaxPayload;
            int v = payload << 5 | Check(payload);
            var c = new char[Length];
            for (int i = Length - 1; i >= 0; i--) { c[i] = Alphabet[v & 31]; v >>= 5; }
            return new string(c);
        }

        /// <summary>Maiusculas, sem espacos nem tracos (o jogador pode digitar "abc-234").</summary>
        public static string Normalize(string s)
        {
            if (s == null) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char ch in s) if (ch != ' ' && ch != '-' && ch != '\t') sb.Append(char.ToUpperInvariant(ch));
            return sb.ToString();
        }

        public static bool TryDecode(string s, out int payload)
        {
            payload = 0;
            s = Normalize(s);
            if (s.Length != Length) return false;
            int v = 0;
            foreach (char ch in s)
            {
                int d = Alphabet.IndexOf(ch);
                if (d < 0) return false;
                v = v << 5 | d;
            }
            int p = v >> 5;
            if (Check(p) != (v & 31)) return false;
            payload = p;
            return true;
        }

        public static bool IsValid(string s) => TryDecode(s, out _);

        /// <summary>Codigo novo a partir de qualquer entropia da View (ex.: ticks do relogio): o Core continua deterministico.</summary>
        public static string Random(uint entropy) => Encode((int)(Rng.Hash((int)entropy, 0x5EED) & MaxPayload));
    }

    /// <summary>Conteudo como dados. Ids de sala e indices de contrato sao estaveis (vao para o save): so crescem no fim.</summary>
    public static class Content
    {
        public const string RegionName = "Porões da Guilda";
        public const int R1 = 0, R2 = 1, R3 = 2, R4 = 3, R5 = 4, R6 = 5, B1 = 6, D1 = 7;
        public const int R7 = 8, R8 = 9, R9 = 10, R10 = 11, R11 = 12, R12 = 13, R13 = 14, R14 = 15, R15 = 16, R16 = 17, S1 = 18, S2 = 19, M1 = 20;
        /// <summary>Salas normais da regiao (sorteio dos contratos gerados).</summary>
        public static readonly int[] NormalRooms = { R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, R11, R12, R13, R14, R15, R16 };

        static Rect O(float cx, float cy, float hx, float hy) => new Rect(cx, cy, hx, hy);
        static Spawn S(EnemyType t, float x, float y) => new Spawn(t, x, y);
        static V2 P(float x, float y) => new V2(x, y);
        const EnemyType Bat = EnemyType.Bat, Thief = EnemyType.Thief, Guard = EnemyType.Guard, Brute = EnemyType.Brute, Ogre = EnemyType.Ogre,
                        Archer = EnemyType.Archer, Slime = EnemyType.Slime, Captain = EnemyType.Captain;
        const RoomTrigger Half = RoomTrigger.Half, Down = RoomTrigger.CargoDown;

        // Icone de ameaca (Threat) = terco do dano medio por visita do bot na campanha (BalanceTests.IconeDeAmeaca_AcompanhaODanoMedido):
        // 1 = R3, R7, R12, R13 · 2 = R1, R2, R5, R6, R9, R10, R14, R15 · 3 = R4, R8, R11, R16. B1/M1 = 3 (chefes, porta unica).
        // Risk (icone de risco da carga) sai dos inimigos da sala e do reforco: ladrao em R2, R3, R6, R9, R10, R13, R16 (+B1, M1, S2);
        // sem risco: R1, R4, R8, R12, R14.
        // Reforco de "metade da sala" sem emboscada chega POR TRAS (cantos de baixo): quem corre para a porta escapa (lutar e escolha, H1);
        // emboscada e reforco de carga no chao chegam por cima, perto das portas.
        // Regras de desenho (medidas em CoreTests.LevelDesign_*): nada solido na faixa das portas (y > 12,2) nem na entrada; spawn (e
        // reforco) a 4 m+ da entrada; toda porta, bau, ponto de resgate e parede rachada alcancaveis; so Guarda/Ogro/Capitao/emboscada
        // obrigam a lutar, o resto se atravessa andando.
        public static readonly RoomDef[] Rooms =
        {
            new RoomDef(R1, "R1", "Sala do Guarda", "FTUE: solte o dedo para pousar e lutar; voltar a andar pega a carga", 2,
                new[] { O(2.0f, 6.5f, 0.6f, 0.6f), O(7.0f, 6.5f, 0.6f, 0.6f) },
                new[] { S(Guard, 4.5f, 12.4f) }, P(2.0f, 3.0f),
                wave: new[] { S(Bat, 1.5f, 12.5f), S(Slime, 7.5f, 12.5f) }, trigger: Down),
            new RoomDef(R2, "R2", "Sala do 1º Ladrão", "FTUE: o ladrão corre para a carga pousada; lute perto dela ou leve-a; dash", 2,
                new[] { O(4.5f, 7.0f, 1.4f, 0.5f) },
                new[] { S(Thief, 7.8f, 4.2f), S(Bat, 2.0f, 11.0f), S(Bat, 7.0f, 11.5f) }, P(2.0f, 3.0f),
                wave: new[] { S(Thief, 8.0f, 12.5f), S(Bat, 1.0f, 12.8f) }, trigger: Down),
            new RoomDef(R3, "R3", "Rota longa com baú", "bifurcação: baú custa parar 1 s com a carga no chão; parede rachada à direita", 1,
                new[] { O(3.0f, 5.0f, 3.0f, 0.3f), O(6.0f, 9.5f, 3.0f, 0.3f) },
                new[] { S(Thief, 8.0f, 7.5f), S(Bat, 4.5f, 12.0f) }, P(1.5f, 2.5f), P(0.9f, 11.0f), 25,
                wave: new[] { S(Slime, 0.8f, 3.5f) }, trigger: Half, secret: S1, secretPos: P(Balance.RoomW, 3.2f)),
            new RoomDef(R4, "R4", "Enxame", "morcegos enchem a sala: atravessar correndo ou pousar e limpar", 3,
                new[] { O(2.5f, 5.5f, 0.5f, 0.5f), O(6.5f, 5.5f, 0.5f, 0.5f), O(4.5f, 9.0f, 0.8f, 0.8f) },
                new[] { S(Bat, 1.5f, 11.0f), S(Bat, 3.5f, 12.0f), S(Bat, 5.5f, 12.0f), S(Bat, 7.5f, 11.0f) }, P(1.5f, 2.5f), P(8.2f, 3.0f), 20,
                wave: new[] { S(Bat, 0.8f, 3.6f), S(Slime, 8.2f, 3.6f) }, trigger: Half),
            new RoomDef(R5, "R5", "Sala do Bruto", "telegraph: saia do golpe; golpe pelas costas acerta a carga (1 morcego só: ver BALANCE.md)", 2,
                new[] { O(2.2f, 7.0f, 1.2f, 0.4f), O(6.8f, 7.0f, 1.2f, 0.4f) },
                new[] { S(Brute, 4.5f, 10.5f), S(Bat, 7.5f, 12.0f) }, P(2.0f, 2.5f), P(0.8f, 9.5f), 35,
                wave: new[] { S(Slime, 1.0f, 12.5f), S(Bat, 8.0f, 12.8f) }, trigger: Down),
            new RoomDef(R6, "R6", "Guarda e Ladrão", "luta obrigatória com a carga no chão: o ladrão vem buscá-la", 2,
                new[] { O(4.5f, 6.0f, 1.6f, 0.4f) },
                new[] { S(Guard, 4.5f, 12.4f), S(Thief, 1.0f, 8.5f), S(Bat, 7.5f, 10.0f) }, P(7.0f, 2.5f),
                wave: new[] { S(Thief, 8.0f, 12.5f) }, trigger: Down),
            new RoomDef(B1, "B1", "Covil do Ogro", "chefe: pancada em área (2 m) e chama ladrões a cada 10 s", 3,
                new[] { O(2.0f, 4.5f, 0.5f, 0.5f), O(7.0f, 4.5f, 0.5f, 0.5f), O(2.0f, 9.5f, 0.5f, 0.5f), O(7.0f, 9.5f, 0.5f, 0.5f) },
                new[] { S(Ogre, 4.5f, 10.5f) }, P(2.0f, 2.5f)),
            new RoomDef(D1, "D1", "Entrega", "o cliente espera no topo", 0,
                new[] { O(2.0f, 7.0f, 0.5f, 0.5f), O(7.0f, 7.0f, 0.5f, 0.5f) },
                new Spawn[0], P(2.0f, 2.5f)),
            // ---- v1.0-beta: R7-R16, S1-S2, M1 (CONTRATO_V1 §7)
            new RoomDef(R7, "R7", "Corredor do Arqueiro", "a flecha não atravessa pilar: avance de cobertura em cobertura", 1,
                new[] { O(2.5f, 5.0f, 0.6f, 0.6f), O(6.5f, 5.0f, 0.6f, 0.6f), O(2.5f, 8.5f, 0.6f, 0.6f), O(6.5f, 8.5f, 0.6f, 0.6f) },
                new[] { S(Archer, 4.5f, 11.2f), S(Bat, 1.5f, 10.5f) }, P(1.5f, 2.5f),
                wave: new[] { S(Archer, 8.2f, 3.2f), S(Bat, 0.8f, 3.2f) }, trigger: Half),
            new RoomDef(R8, "R8", "Poça de Limo", "o limo morto deixa poça que atrasa: não pouse em cima dela", 3,
                new[] { O(4.5f, 6.5f, 2.0f, 0.4f) },
                new[] { S(Slime, 2.5f, 9.5f), S(Slime, 6.5f, 9.5f) }, P(1.5f, 2.5f), P(8.2f, 4.0f), 30,
                wave: new[] { S(Slime, 0.8f, 3.5f), S(Slime, 8.2f, 3.5f) }, trigger: Half),
            new RoomDef(R9, "R9", "Galeria dos Arqueiros", "dois arqueiros cruzam a sala; parede rachada à esquerda", 2,
                new[] { O(2.0f, 7.0f, 1.2f, 0.4f), O(7.0f, 7.0f, 1.2f, 0.4f), O(4.5f, 10.0f, 0.6f, 0.6f) },
                new[] { S(Archer, 1.5f, 11.5f), S(Archer, 7.5f, 11.5f), S(Bat, 4.5f, 12.5f) }, P(7.0f, 2.5f),
                wave: new[] { S(Thief, 1.0f, 12.8f), S(Bat, 8.0f, 12.8f) }, trigger: Down, secret: S1, secretPos: P(0f, 4.0f)),
            new RoomDef(R10, "R10", "Emboscada", "passar da metade chama reforço e a porta fecha até ele cair", 2,
                new[] { O(2.5f, 6.0f, 0.5f, 0.5f), O(6.5f, 6.0f, 0.5f, 0.5f) },
                new[] { S(Bat, 2.0f, 10.0f) }, P(2.0f, 2.5f),
                wave: new[] { S(Thief, 1.5f, 12.5f), S(Bat, 7.5f, 12.5f), S(Thief, 4.5f, 12.8f) }, trigger: Half, waveLocks: true),
            new RoomDef(R11, "R11", "Bruto e Arqueiro", "o arqueiro cobre o Bruto: não pouse na linha dele", 3,
                new[] { O(4.5f, 7.5f, 0.8f, 0.8f), O(1.5f, 10.0f, 0.5f, 0.5f) },
                new[] { S(Brute, 4.5f, 10.5f), S(Archer, 7.5f, 12.0f) }, P(2.0f, 2.5f), P(0.8f, 7.5f), 40,
                wave: new[] { S(Bat, 0.8f, 3.5f), S(Slime, 8.2f, 3.5f) }, trigger: Half),
            new RoomDef(R12, "R12", "Guarda das Poças", "luta obrigatória entre limos: escolha onde pousar", 1,
                new[] { O(2.5f, 5.5f, 0.5f, 0.5f), O(6.5f, 5.5f, 0.5f, 0.5f) },
                new[] { S(Guard, 4.5f, 12.4f), S(Slime, 2.0f, 9.0f) }, P(2.0f, 2.5f),
                wave: new[] { S(Slime, 7.0f, 12.0f) }, trigger: Down),
            new RoomDef(R13, "R13", "Ladrões em Dupla", "dois ladrões e um arqueiro: leve a carga ou guarde-a; parede rachada à direita", 1,
                new[] { O(4.5f, 6.0f, 1.5f, 0.4f) },
                new[] { S(Thief, 1.0f, 8.0f), S(Thief, 8.0f, 9.0f), S(Archer, 4.5f, 12.0f) }, P(2.0f, 2.5f), P(0.8f, 11.0f), 30,
                wave: new[] { S(Bat, 1.0f, 12.8f), S(Slime, 8.0f, 12.5f) }, trigger: Down, secret: S2, secretPos: P(Balance.RoomW, 10.5f)),
            new RoomDef(R14, "R14", "Labirinto", "três paredes em zigue-zague: carga 3 s no chão chama reforço e fecha a porta; parede rachada à esquerda", 2,
                new[] { O(3.0f, 4.5f, 3.0f, 0.3f), O(6.0f, 8.0f, 3.0f, 0.3f), O(3.0f, 11.0f, 3.0f, 0.3f) },
                new[] { S(Bat, 1.5f, 6.5f), S(Slime, 7.5f, 9.6f), S(Bat, 4.5f, 12.5f) }, P(7.0f, 2.5f), P(0.8f, 9.6f), 35,
                wave: new[] { S(Slime, 1.0f, 12.5f), S(Slime, 8.0f, 12.5f) }, trigger: Down, waveLocks: true, secret: S2, secretPos: P(0f, 2.6f)),
            new RoomDef(R15, "R15", "Guarda Duplo", "dois guardas e um arqueiro: mate um de cada vez fora do alcance", 2,
                new[] { O(4.5f, 7.0f, 1.0f, 0.5f) },
                new[] { S(Guard, 2.5f, 12.4f), S(Guard, 6.5f, 12.4f), S(Archer, 4.5f, 10.4f) }, P(2.0f, 2.5f),
                wave: new[] { S(Bat, 4.5f, 12.8f), S(Slime, 1.0f, 11.0f) }, trigger: Down),
            new RoomDef(R16, "R16", "Salão Final", "Bruto, arqueiro e limo; passar da metade chama reforço e fecha a porta", 3,
                new[] { O(2.0f, 6.0f, 0.6f, 0.6f), O(7.0f, 6.0f, 0.6f, 0.6f), O(4.5f, 9.0f, 0.6f, 0.6f) },
                new[] { S(Brute, 3.0f, 10.5f), S(Archer, 7.5f, 11.5f), S(Slime, 6.0f, 8.5f) }, P(2.0f, 2.5f), P(8.2f, 8.0f), 45,
                wave: new[] { S(Bat, 1.0f, 12.5f), S(Slime, 8.0f, 12.5f), S(Thief, 4.5f, 12.8f) }, trigger: Half, waveLocks: true),
            new RoomDef(S1, "S1", "Depósito Secreto", "sala secreta: baú extra", 0,
                new[] { O(2.0f, 6.0f, 0.5f, 0.5f), O(7.0f, 6.0f, 0.5f, 0.5f) },
                new[] { S(Bat, 2.0f, 11.0f) }, P(2.0f, 2.5f), P(4.5f, 9.0f), 60),
            new RoomDef(S2, "S2", "Tesouro Escondido", "sala secreta: baú grande guardado por um ladrão", 1,
                new[] { O(4.5f, 6.5f, 1.2f, 0.4f) },
                new[] { S(Thief, 8.0f, 8.0f) }, P(2.0f, 2.5f), P(4.5f, 10.0f), 80),
            new RoomDef(M1, "M1", "Toca do Capitão", "mini-chefe: o Capitão rouba a carga do chão e chama ladrões; acerte-o 2x para soltar", 3,
                new[] { O(2.0f, 5.0f, 0.5f, 0.5f), O(7.0f, 5.0f, 0.5f, 0.5f), O(4.5f, 8.0f, 1.0f, 0.4f) },
                new[] { S(Captain, 4.5f, 11.5f), S(Bat, 1.5f, 11.0f), S(Bat, 7.5f, 11.0f) }, P(2.0f, 2.5f)),
        };

        static ContractDef C(int index, CargoType cargo, int pay, string note, float par, float egg, float limit, params int[][] layers)
            => new ContractDef(index, cargo, pay, egg, note, layers) { ParTime = par, TimeLimit = limit, Waves = index >= Balance.OpenAfter };
        static int[] L(params int[] rooms) => rooms;
        const CargoType Box = CargoType.Box, Relic = CargoType.Relic, Egg = CargoType.Egg, Cake = CargoType.Cake, Msg = CargoType.Message,
                        Heavy = CargoType.Heavy, Barrel = CargoType.Barrel;

        // C1-C3 tutoriais (2-3 camadas); C4+ com 5-6 camadas; cada carga nova estreia num contrato proprio (Reliquia C5, Bolo C7,
        // Caixa pesada C9, Mensagem C10, Barril C13); C12 = mini-chefe; C24 = chefe. Tempos pela mediana do bot na campanha (BALANCE.md
        // v1): ParTime (3a estrela) = 1,6x; EggTarget = 2x (quem joga na metade do ritmo do bot choca ~50%); TimeLimit = 2,5x.
        public static readonly ContractDef[] Contracts =
        {
            C(0,   Box,   40,  "FTUE: pousar para lutar; 1ª bifurcação; 1º ladrão", 35f, 0f, 0f, L(R1), L(R2, R3)),
            C(1,   Box,   45,  "1º upgrade da guilda liberado antes", 25f, 0f, 0f, L(R2), L(R4, R3)),
            C(2,   Egg,   50,  "ovo: choca no tempo-alvo; 1º perk", 45f, 55f, 0f, L(R1), L(R2, R4), L(R3, R5)),
            C(3,   Box,   70,  "Bruto aparece; 1ª emboscada", 85f, 0f, 0f, L(R5), L(R4, R3), L(R2, R1), L(R6, R10), L(R3, R4)),
            C(4,   Relic, 75,  "relíquia frágil", 125f, 0f, 0f, L(R1), L(R5, R3), L(R8, R2), L(R12, R10), L(R1, R4)),
            C(5,   Egg,   80,  "arqueiro aparece; ovo com Guarda e Ladrão", 100f, 125f, 0f, L(R6), L(R7, R3), L(R5, R2, R4), L(R10, R1), L(R7, R9)),
            C(6,   Cake,  85,  "bolo de casamento: sem dash", 105f, 0f, 0f, L(R1), L(R7, R4), L(R9, R5), L(R10, R2), L(R3, R6)),
            C(7,   Relic, 95,  "relíquia entre arqueiros", 115f, 0f, 0f, L(R7), L(R9, R1), L(R8, R5), L(R2, R12), L(R15, R4)),
            C(8,   Heavy, 100, "caixa pesada; limo aparece", 105f, 0f, 0f, L(R8), L(R4, R3), L(R12, R7), L(R5, R10), L(R2, R6), L(R11, R9)),
            C(9,   Msg,   105, "mensagem urgente: cronômetro, abre portas trancadas", 45f, 0f, 70f, L(R1), L(R6, R12), L(R15, R4), L(R10, R8), L(R3, R5), L(R13, R16)),
            C(10,  Egg,   115, "ovo em 6 camadas", 90f, 115f, 0f, L(R8), L(R9, R3), L(R11, R5, R7), L(R14, R6), L(R16, R2), L(R12, R4)),
            C(11,  Box,   140, "mini-chefe: Capitão Ladrão", 120f, 0f, 0f, L(R4), L(R13, R3), L(R11, R8), L(R10, R14), L(R9, R7), L(M1)),
            C(12,  Barrel, 120, "barril de pólvora: no chão explode", 155f, 0f, 0f, L(R2), L(R4, R8), L(R7, R14), L(R9, R5), L(R3, R16), L(R13, R1)),
            C(13,  Cake,  130, "bolo em 6 camadas", 135f, 0f, 0f, L(R12), L(R13, R4), L(R7, R11, R8), L(R3, R16), L(R10, R9), L(R2, R15)),
            C(14,  Relic, 135, "relíquia em 6 camadas", 145f, 0f, 0f, L(R7), L(R2, R14), L(R13, R8), L(R9, R11), L(R12, R15), L(R1, R3)),
            C(15,  Msg,   140, "mensagem com Guardas", 55f, 0f, 90f, L(R15), L(R10, R12), L(R6, R1), L(R13, R16, R3), L(R4, R8), L(R14, R9)),
            C(16,  Heavy, 145, "caixa pesada em 6 camadas", 130f, 0f, 0f, L(R11), L(R14, R9), L(R3, R16), L(R12, R7), L(R10, R13), L(R4, R8)),
            C(17,  Egg,   150, "ovo com Bruto e arqueiros", 130f, 160f, 0f, L(R16), L(R7, R4), L(R11, R8), L(R14, R13, R3), L(R6, R12), L(R9, R2)),
            C(18,  Barrel, 160, "barril em 6 camadas", 145f, 0f, 0f, L(R13), L(R9, R10), L(R5, R14), L(R16, R8), L(R11, R2), L(R12, R7)),
            C(19,  Cake,  170, "bolo em 6 camadas", 170f, 0f, 0f, L(R3), L(R11, R13), L(R16, R12), L(R9, R6, R2), L(R14, R15), L(R10, R7)),
            C(20,  Relic, 180, "relíquia em 6 camadas", 140f, 0f, 0f, L(R9), L(R13, R14), L(R8, R15), L(R11, R12), L(R16, R1), L(R7, R4)),
            C(21,  Msg,   185, "mensagem em 6 camadas", 55f, 0f, 85f, L(R12), L(R15, R6), L(R16, R13), L(R1, R10), L(R11, R14, R8), L(R9, R4)),
            C(22,  Barrel, 195, "barril em 6 camadas", 170f, 0f, 0f, L(R14), L(R16, R11), L(R13, R9), L(R12, R10), L(R15, R8), L(R7, R3)),
            C(23,  Box,   260, "chefe: Ogro", 270f, 0f, 0f, L(R16), L(R15, R13), L(R11, R14, R4), L(R12, R10), L(R9, R7), L(B1)),
        };

        /// <summary>Contrato gerado (dia ou codigo) a partir do payload: carga, 4-6 camadas de 2-3 salas normais da regiao, pagamento e
        /// tempos pelas taxas medidas (Balance.Gen*). Deterministico: mesmo payload = mesmo contrato em qualquer aparelho.</summary>
        public static ContractDef Generate(int payload, ContractKind kind, int date = 0)
        {
            payload &= ContractCode.MaxPayload;
            var rng = new Rng(Rng.Hash(payload, 0x6E4));
            var cargo = (CargoType)rng.Int(CargoDef.All.Length);
            int layers = 4 + rng.Int(3);
            var pool = new System.Collections.Generic.List<int>();
            var ls = new int[layers][];
            for (int l = 0; l < layers; l++)
            {
                int n = l == 0 ? 1 : 2 + rng.Int(2);
                ls[l] = new int[n];
                for (int i = 0; i < n; i++)
                {
                    if (pool.Count == 0) pool.AddRange(NormalRooms);
                    int k = rng.Int(pool.Count);
                    ls[l][i] = pool[k];
                    pool.RemoveAt(k);
                }
            }
            int rooms = layers + 1, pay = 40 + 22 * layers;
            string code = ContractCode.Encode(payload);
            var def = new ContractDef(kind == ContractKind.Daily ? "DIA" : code, cargo, pay, cargo == CargoType.Egg ? Balance.GenEggPerRoom * rooms : 0f,
                                      (kind == ContractKind.Daily ? "contrato do dia: " : "contrato " + code + ": ") + CargoDef.All[(int)cargo].Name, ls)
            {
                Kind = kind, ShareCode = code, Payload = payload, Date = date, ParTime = Balance.GenParPerRoom * rooms, Tier = Balance.GenTierBase + 3 * (layers - 4),
                TimeLimit = cargo == CargoType.Message ? Balance.GenLimitPerRoom * rooms : 0f,
            };
            return def;
        }
    }
}
