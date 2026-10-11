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

    /// <summary>Lancamento (CONTRATO_LANCAMENTO §3): 8 cargas novas no fim. Sem comportamento proprio ainda = carga normal (como a Caixa);
    /// as raias de regiao implementam o verbo em Run.RegiaoN.cs.</summary>
    public enum CargoType { Box, Relic, Egg, Cake, Message, Heavy, Barrel, ArcaneHeart, IceCrystal, GriffinCage, Lantern, Mirror, Balloon, Flytrap, TreasureMap }
    /// <summary>Lancamento (§4): 14 inimigos e 6 chefes novos no fim. Sem comportamento proprio ainda = persegue e golpeia (Melee).</summary>
    public enum EnemyType
    {
        Bat, Thief, Guard, Brute, Ogre, Archer, Slime, Captain,
        GoblinMiner, Mole, BogFrog, BombBeetle, Minecart, Ghost, Skeleton, Spider, Mummy, Gargoyle, Salamander, Imp, KoboldBomber, MagmaElemental,
        MoleQueen, StoneGolem, SpiderWidow, Necromancer, KoboldForeman, ElderDragon,
        // variacoes (nao contam nos 20): brasa do elemental de magma e ovo de salamandra do Dragao (CONTEUDO_REGIOES §5)
        MagmaShard, SalamanderEgg,
    }
    public enum Upgrade { Life, Dash, Bag, Pack }
    public enum Weapon { Sling, Boomerang, Bomb }
    /// <summary>Lancamento (§5): 15 perks novos no fim; so entram no sorteio de contrato da regiao 2 em diante (PerkDef.Region), para a
    /// regiao 1 continuar com as mesmas ofertas.</summary>
    public enum Perk
    {
        LightStep, QuickPlant, DoubleDash, Padded, QuickHand, Magnet, ExtraHeart, Lookout, IronBoot, ChestLuck,
        Spring, SecondWind, ChestSense, SureFoot, Insulated, FirmGrip, Counter, Hunter, ThickSkin, CatEyes, Whistle, FirstAid, Runner, CargoShield, Scout,
    }
    /// <summary>Rare = contrato raro (volta depois de 8 h); Event = contrato do quadro de eventos; Endless = desafio sem fim.</summary>
    public enum ContractKind { Campaign, Daily, Code, Rare, Event, Endless }
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

    /// <summary>Lancamento: Melted = o cristal de gelo derreteu; Escaped = o balao escapou (espelho quebrado e balao estourado = CargoBroken).</summary>
    public enum Outcome { Running, Delivered, Died, CargoStolen, CargoBroken, PetDied, Abandoned, TimeUp, Exploded, Melted, Escaped }

    /// <summary>Dica contextual da FTUE (texto em Hints.Text).</summary>
    public enum Hint
    {
        None, PlantToFight, PickUp, Dash, Thief, DoorNeedsCargo, GuardDoor, Chest, WaitPet, DragToWalk, CakeNoDash, BarrelFuse, ArcherAim, SecretWall, MessageDoor,
        // lancamento (CONTEUDO_REGIOES §1.4): as raias de regiao ligam pelo gancho HintRn (Run.cs); Dark, MapReveal e Asleep o nucleo ja liga
        Water, CartBell, Asleep, MoleTrail, FrogTongue, BeetleFuse, HeartPull, IceMelt, CageScream, Dark, LanternOut, Web, GhostCargo, SkeletonRise,
        MummyGrab, GargoyleWake, MirrorFront, BalloonRise, Lava, Vent, PlantBite, MapReveal, ImpSwap,
    }

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
                case Hint.Water: return "água freia todo mundo: o ladrão também";
                case Hint.CartBell: return "vagonete! saia do trilho";
                case Hint.Asleep: return "mineiro dormindo: passe a mais de 2 m";
                case Hint.MoleTrail: return "toupeira por baixo: carregue a carga";
                case Hint.FrogTongue: return "sapo: não pouse a menos de 3 m dele";
                case Hint.BeetleFuse: return "besouro vai explodir: dash ou atire de longe";
                case Hint.HeartPull: return "o coração atrai inimigos e moedas";
                case Hint.IceMelt: return "o gelo derrete: passe pela água";
                case Hint.CageScream: return "o grifinho vai gritar: o dash acalma";
                case Hint.Dark: return "no escuro só se mira o que está na luz";
                case Hint.LanternOut: return "a lanterna apagou: leve-a até uma tocha";
                case Hint.Web: return "teia: o dash rasga";
                case Hint.GhostCargo: return "o fantasma está levando a carga: pegue de volta";
                case Hint.SkeletonRise: return "lute perto da carga: o esqueleto não volta";
                case Hint.MummyGrab: return "agarrado: dash ou pouse e lute";
                case Hint.GargoyleWake: return "a gárgula acorda com a carga por perto";
                case Hint.MirrorFront: return "o espelho reflete o que vem de frente";
                case Hint.BalloonRise: return "balão no chão escapa em 4 s: pegue!";
                case Hint.Lava: return "empurre inimigos na lava com o dash";
                case Hint.Vent: return "respiro brilhando: vai sair fogo";
                case Hint.PlantBite: return "a planta morde quem fica perto: dê um passo";
                case Hint.MapReveal: return "o mapa mostra baús enterrados";
                case Hint.ImpSwap: return "diabrete vai trocar de lugar com a carga";
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
        RoomEvent, Explosion, ArrowShot, ArrowHit, PuddleSpawn, CakeWobble, SecretFound, FuseLit, TimeWarning, MessageDoor, Stun, CaptainCall,
        // lancamento (CONTRATO_LANCAMENTO §6-§7); A/B em Ev2 (comentario de MetaDefs.cs)
        SecondWind, GearUse, GearUnlock, GearUpgrade, BlueprintFound, MasteryUp, Milestone, ModifierPick, EventStart, EventEnd, EventReward,
        SeasonProgress, SeasonClaim, RareOffer, RareStart, Reroll, EndlessStart, EndlessEnd, RewardedOffer, RewardedGrant, RewardedResume,
        InterstitialShow, IapPurchase, DeviceFps, OutfitEquip,
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
                // lancamento (§6.9)
                case Ev.GearUse: return "gear_use";
                case Ev.GearUnlock: return "gear_unlock";
                case Ev.GearUpgrade: return "gear_upgrade";
                case Ev.BlueprintFound: return "blueprint_found";
                case Ev.MasteryUp: return "mastery_up";
                case Ev.Milestone: return "milestone_claim";
                case Ev.ModifierPick: return "modifier_pick";
                case Ev.EventStart: return "event_start";
                case Ev.EventEnd: return "event_end";
                case Ev.EventReward: return "event_reward";
                case Ev.SeasonProgress: return "season_progress";
                case Ev.SeasonClaim: return "season_claim";
                case Ev.RareOffer: return "rare_offer";
                case Ev.RareStart: return "rare_start";
                case Ev.Reroll: return "reroll";
                case Ev.EndlessStart: return "endless_start";
                case Ev.EndlessEnd: return "endless_end";
                case Ev.RewardedOffer: return "rewarded_offer";
                case Ev.RewardedGrant: return "rewarded_grant";
                case Ev.RewardedResume: return "rewarded_resume";
                case Ev.InterstitialShow: return "interstitial_show";
                case Ev.IapPurchase: return "iap_purchase";
                case Ev.DeviceFps: return "device_fps_bucket";
                case Ev.OutfitEquip: return "outfit_equip";
                default: return null;
            }
        }
    }

    // ------------------------------------------------------------------ numeros (CONTRATO_V01 §3-§7 + CONTRATO_V1; hipoteses medidas em BALANCE.md)

    /// <summary>Numeros (partial: os do lancamento estao em MetaDefs.cs).</summary>
    public static partial class Balance
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

    /// <summary>Carga. Region = regiao em que estreia (o sorteio dos contratos gerados de uma regiao usa as cargas ate ela). Fragile = carga
    /// que falha ou racha (Fragile Week). Provisional = numeros ainda nao medidos pelo bot (as raias de regiao ajustam e desligam).</summary>
    public sealed class CargoDef
    {
        public readonly CargoType Type; public readonly string Name, Verb; public readonly float PayMul, HatchedMul;
        public bool Provisional { get; internal set; }
        public CargoDef(CargoType t, string name, float payMul, float hatchedMul = 0f, string verb = "")
        { Type = t; Name = name; PayMul = payMul; HatchedMul = hatchedMul; Verb = verb; }
        public int Region => Type <= CargoType.Barrel ? 1 : Type <= CargoType.GriffinCage ? 2 : Type <= CargoType.Balloon ? 3 : 4;
        public bool Fragile => Type == CargoType.Relic || Type == CargoType.Egg || Type == CargoType.Cake || Type == CargoType.IceCrystal || Type == CargoType.Mirror;
        public static readonly CargoDef[] All =
        {
            new CargoDef(CargoType.Box, "Caixa", 1.0f, 0f, "nenhum: aprenda a carregar"),
            new CargoDef(CargoType.Relic, "Relíquia frágil", 1.4f, 0f, "racha com golpe e queda; 3 rachaduras = quebra"),
            new CargoDef(CargoType.Egg, "Ovo", 1.2f, 0.8f, "choca no tempo-alvo; explosão perto racha"),
            new CargoDef(CargoType.Cake, "Bolo de casamento", 1.3f, 0f, "proíbe o dash"),
            new CargoDef(CargoType.Message, "Mensagem urgente", 1.25f, 0f, "cronômetro; abre portas trancadas"),
            new CargoDef(CargoType.Heavy, "Caixa pesada", 1.6f, 0f, "-25% de velocidade; pousar e pegar mais lentos"),
            new CargoDef(CargoType.Barrel, "Barril de pólvora", 1.5f, 0f, "no chão por 3 s explode"),
            // ---- lancamento (CONTRATO_LANCAMENTO §3): PROVISORIAS (pagamento chutado; verbo ainda nao implementado = carga normal)
            new CargoDef(CargoType.ArcaneHeart, "Coração arcano", 1.4f, 0f, "ímã de moedas e de inimigos num raio visível") { Provisional = true },
            new CargoDef(CargoType.IceCrystal, "Cristal de gelo", 1.5f, 0f, "derrete com o tempo; lava acelera, água recongela") { Provisional = true },
            new CargoDef(CargoType.GriffinCage, "Gaiola do grifinho", 1.4f, 0f, "grita a cada ~6 s e atrai inimigos; dash acalma") { Provisional = true },
            new CargoDef(CargoType.Lantern, "Lanterna", 1.3f, 0f, "ilumina a sala escura; derrubada apaga") { Provisional = true },
            new CargoDef(CargoType.Mirror, "Espelho encantado", 1.5f, 0f, "reflete flechas de frente; 3 reflexos quebram") { Provisional = true },
            new CargoDef(CargoType.Balloon, "Balão de gás", 1.3f, 0f, "+15% de velocidade; no chão escapa em 4 s") { Provisional = true },
            new CargoDef(CargoType.Flytrap, "Planta carnívora", 1.4f, 0f, "no chão morde inimigos perto; morde o courier sem carregá-la") { Provisional = true },
            new CargoDef(CargoType.TreasureMap, "Mapa do tesouro", 1.3f, 0f, "revela baús e paredes rachadas; ladrões o preferem") { Provisional = true },
        };
    }

    /// <summary>Reach = alcance do golpe a partir do centro do inimigo ate a BORDA do alvo (arqueiro: alcance do tiro); Trigger = distancia
    /// (centro a borda) em que comeca o telegraph. Locks = enquanto vivo, as portas da sala ficam fechadas (luta obrigatoria).</summary>
    public sealed class EnemyDef
    {
        public readonly EnemyType Type; public readonly string Name; public readonly int Hp, Damage;
        public readonly float Speed, Radius, Reach, Trigger, Tele, Cooldown; public readonly bool Locks;
        /// <summary>Provisional = numeros chutados e sem comportamento proprio (persegue e golpeia como o Bruto); a raia da regiao ajusta.
        /// Region = regiao em que aparece (1-4).</summary>
        public bool Provisional { get; internal set; }
        public int Region { get; internal set; } = 1;
        readonly bool boss; readonly CargoRisk risk;
        public EnemyDef(EnemyType t, string name, int hp, float speed, float radius, int dmg, float reach, float trigger, float tele, float cd, bool locks,
                        bool boss = false, CargoRisk risk = CargoRisk.None)
        { Type = t; Name = name; Hp = hp; Speed = speed; Radius = radius; Damage = dmg; Reach = reach; Trigger = trigger; Tele = tele; Cooldown = cd; Locks = locks; this.boss = boss; this.risk = risk; }
        /// <summary>Chefe ou mini-chefe (Ogro, Capitao e os 6 novos): a mensagem nao abre a porta dele e o dash nao o empurra (menos o Capitao).</summary>
        public bool Boss => boss;
        /// <summary>Mini-chefe (Capitao, Rainha Toupeira, Viuva Aranha, Capataz Kobold).</summary>
        public bool Mini => Type == EnemyType.Captain || Type == EnemyType.MoleQueen || Type == EnemyType.SpiderWidow || Type == EnemyType.KoboldForeman;
        /// <summary>Perigo, nao inimigo (vagonete): nao e alvo, nao conta no teto de vivos nem acorda ninguem.</summary>
        public bool Hazard => Type == EnemyType.Minecart;
        /// <summary>Risco que este inimigo traz para a carga (icone da porta).</summary>
        public CargoRisk Risk => risk | (Type == EnemyType.Thief || Type == EnemyType.Captain ? CargoRisk.Thief : Type == EnemyType.Archer ? CargoRisk.Archer
                               : Type == EnemyType.Brute || Type == EnemyType.Ogre ? CargoRisk.Smash : CargoRisk.None);
        const bool L = true, B = true; const CargoRisk Th = CargoRisk.Thief, Ar = CargoRisk.Archer, Sm = CargoRisk.Smash, No = CargoRisk.None;
        public static readonly EnemyDef[] All =
        {
            new EnemyDef(EnemyType.Bat,     "Morcego",        2,  2.4f, 0.30f, 1, 0f,   0f,   0f,   1.0f, false),
            new EnemyDef(EnemyType.Thief,   "Ladrão",         3,  2.0f, 0.35f, 0, 0f,   0f,   0f,   0f,   false),
            new EnemyDef(EnemyType.Guard,   "Guarda",         16, 0f,   0.45f, 1, 1.2f, 1.2f, 0.5f, 1.0f, true),
            new EnemyDef(EnemyType.Brute,   "Bruto",          6,  1.6f, 0.55f, 2, 1.2f, 1.0f, 0.6f, 1.2f, false),
            new EnemyDef(EnemyType.Ogre,    "Ogro",           45, 1.2f, 0.90f, 3, 2.0f, 1.6f, 1.0f, 2.0f, true, B),
            new EnemyDef(EnemyType.Archer,  "Arqueiro",       3,  1.4f, 0.35f, 1, 7.0f, 7.0f, 0.8f, 2.2f, false),
            new EnemyDef(EnemyType.Slime,   "Limo",           4,  1.2f, 0.40f, 1, 0f,   0f,   0f,   1.6f, false),
            new EnemyDef(EnemyType.Captain, "Capitão Ladrão", 24, 2.6f, 0.50f, 1, 1.0f, 0.9f, 0.5f, 1.2f, true, B),
            // ---- lancamento (CONTRATO_LANCAMENTO §4): PROVISORIOS. Numeros chutados; sem comportamento = Melee (persegue e golpeia).
            //      O vagonete fica parado e sem dano ate a raia R2 o por nos trilhos. hp, vel, raio, dano, alcance, gatilho, telegraph, recarga, tranca
            new EnemyDef(EnemyType.GoblinMiner,    "Goblin mineiro",     4,  1.4f, 0.35f, 1, 1.0f, 0.9f, 0.6f, 1.4f, false, false, Ar) { Provisional = true, Region = 2 },
            new EnemyDef(EnemyType.Mole,           "Toupeira",           3,  1.8f, 0.35f, 1, 0.9f, 0.8f, 0.5f, 1.4f, false, false, Sm) { Provisional = true, Region = 2 },
            new EnemyDef(EnemyType.BogFrog,        "Sapo do lodo",       4,  1.0f, 0.40f, 1, 1.0f, 0.9f, 0.6f, 1.6f, false, false, Th) { Provisional = true, Region = 2 },
            new EnemyDef(EnemyType.BombBeetle,     "Besouro-bomba",      2,  2.6f, 0.30f, 2, 0.8f, 0.6f, 1.0f, 1.5f, false, false, Sm) { Provisional = true, Region = 2 },
            new EnemyDef(EnemyType.Minecart,       "Vagonete",           99, 0f,   0.50f, 0, 0f,   0f,   0f,   1.0f, false, false, Sm) { Provisional = true, Region = 2 },
            new EnemyDef(EnemyType.Ghost,          "Fantasma",           3,  1.2f, 0.35f, 1, 0.9f, 0.8f, 0.5f, 1.4f, false, false, Th) { Provisional = true, Region = 3 },
            new EnemyDef(EnemyType.Skeleton,       "Esqueleto",          4,  1.4f, 0.40f, 1, 1.0f, 0.9f, 0.5f, 1.2f, false, false, No) { Provisional = true, Region = 3 },
            new EnemyDef(EnemyType.Spider,         "Aranha",             3,  1.8f, 0.35f, 1, 0.9f, 0.8f, 0.5f, 1.4f, false, false, No) { Provisional = true, Region = 3 },
            new EnemyDef(EnemyType.Mummy,          "Múmia",              8,  0.9f, 0.45f, 1, 1.0f, 0.9f, 0.7f, 1.6f, false, false, No) { Provisional = true, Region = 3 },
            new EnemyDef(EnemyType.Gargoyle,       "Gárgula",            6,  2.0f, 0.45f, 2, 1.0f, 0.9f, 0.8f, 2.0f, false, false, Sm) { Provisional = true, Region = 3 },
            new EnemyDef(EnemyType.Salamander,     "Salamandra",         3,  2.0f, 0.35f, 1, 0.9f, 0.8f, 0.5f, 1.4f, false, false, No) { Provisional = true, Region = 4 },
            new EnemyDef(EnemyType.Imp,            "Diabrete",           3,  2.2f, 0.30f, 1, 0.8f, 0.7f, 0.5f, 1.4f, false, false, Th) { Provisional = true, Region = 4 },
            new EnemyDef(EnemyType.KoboldBomber,   "Kobold incendiário", 3,  1.4f, 0.35f, 1, 1.0f, 0.9f, 0.6f, 1.6f, false, false, Ar) { Provisional = true, Region = 4 },
            new EnemyDef(EnemyType.MagmaElemental, "Elemental de magma", 6,  1.2f, 0.50f, 2, 1.1f, 1.0f, 0.7f, 1.6f, false, false, Sm) { Provisional = true, Region = 4 },
            new EnemyDef(EnemyType.MoleQueen,      "Rainha Toupeira",    26, 1.8f, 0.60f, 1, 1.1f, 1.0f, 0.6f, 1.4f, L, B, Th) { Provisional = true, Region = 2 },
            new EnemyDef(EnemyType.StoneGolem,     "Golem de Pedra",     50, 1.0f, 0.90f, 3, 2.0f, 1.6f, 1.0f, 2.2f, L, B, Sm) { Provisional = true, Region = 2 },
            new EnemyDef(EnemyType.SpiderWidow,    "Viúva Aranha",       28, 1.6f, 0.60f, 1, 1.1f, 1.0f, 0.6f, 1.4f, L, B, Th) { Provisional = true, Region = 3 },
            new EnemyDef(EnemyType.Necromancer,    "Necromante",         48, 1.2f, 0.60f, 2, 1.5f, 1.3f, 0.9f, 2.0f, L, B, No) { Provisional = true, Region = 3 },
            new EnemyDef(EnemyType.KoboldForeman,  "Capataz Kobold",     30, 1.8f, 0.60f, 2, 3.0f, 2.5f, 0.7f, 1.6f, L, B, Ar) { Provisional = true, Region = 4 },
            new EnemyDef(EnemyType.ElderDragon,    "Dragão Ancião",      70, 1.2f, 1.00f, 3, 2.5f, 2.0f, 1.2f, 2.4f, L, B, Sm) { Provisional = true, Region = 4 },
            new EnemyDef(EnemyType.MagmaShard,     "Brasa de magma",     4,  1.8f, 0.40f, 1, 0.8f, 0.7f, 0.5f, 1.2f, false, false, No) { Provisional = true, Region = 4 },
            new EnemyDef(EnemyType.SalamanderEgg,  "Ovo de salamandra",  2,  0f,   0.30f, 0, 0f,   0f,   6.0f, 0f,   false, false, No) { Provisional = true, Region = 4 },
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
            new WeaponDef(Weapon.Bomb, "Bombinha", "arco com dano em área, lenta contra alvo rápido; racha o ovo e detona o barril", 24, new[] { 2, 2, 3 }, new[] { 1.1f, 0.85f, 1.05f }, 5f, 1.5f, new[] { 0, 500, 1100 }),
        };
    }

    /// <summary>Perk de run (CONTRATO_V1 §6): so vale no contrato em curso; sem pagamento.</summary>
    public sealed class PerkDef
    {
        public readonly Perk Type; public readonly string Name, Text;
        public PerkDef(Perk t, string name, string text) { Type = t; Name = name; Text = text; }
        /// <summary>Regiao a partir da qual o perk entra no sorteio: os 10 da v1 = 1; os novos = 2 (Olhos de gato = 3, a do escuro).</summary>
        public int Region => Type < Perk.Spring ? 1 : Type == Perk.CatEyes ? 3 : 2;
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
            // ---- lancamento (CONTRATO_LANCAMENTO §5)
            new PerkDef(Perk.Spring, "Mola", "dash +40% de distância"),
            new PerkDef(Perk.SecondWind, "Segundo fôlego", "1x por contrato, volta com 2 de vida"),
            new PerkDef(Perk.ChestSense, "Faro de baú", "a porta mostra o conteúdo do baú"),
            new PerkDef(Perk.SureFoot, "Pé firme", "água, lama e teia não freiam"),
            new PerkDef(Perk.Insulated, "Isolamento", "gelo derrete 40% mais devagar; o ovo choca 20% mais tarde"),
            new PerkDef(Perk.FirmGrip, "Alça firme", "o 1º dash de cada sala leva a carga junto"),
            new PerkDef(Perk.Counter, "Contragolpe", "dash em quem segura a carga a derruba"),
            new PerkDef(Perk.Hunter, "Caçador", "+2 moedas por inimigo derrotado"),
            new PerkDef(Perk.ThickSkin, "Couro grosso", "-1 no dano de golpes de 2 ou mais"),
            new PerkDef(Perk.CatEyes, "Olhos de gato", "luz do courier +60% no escuro"),
            new PerkDef(Perk.Whistle, "Assobio", "ladrão que pega a carga fica parado 1,5 s"),
            new PerkDef(Perk.FirstAid, "Primeiros socorros", "+1 de vida ao abrir baú"),
            new PerkDef(Perk.Runner, "Corredor", "+20% de velocidade sem a carga"),
            new PerkDef(Perk.CargoShield, "Escudo da carga", "o 1º golpe em cada sala na carga pousada é ignorado"),
            new PerkDef(Perk.Scout, "Batedor", "as portas mostram o número exato de inimigos"),
        };
    }

    /// <summary>Inimigo da sala. Asleep = nasce dormindo (zZ): nao age ate acordar (courier a Balance.WakeRange, golpe nele, explosao a
    /// Balance.WakeBlast, vizinho acordado a Balance.WakeNeighbor por Balance.WakeDelay s, ou Run.Wake de um grito); o 1o golpe dobra.</summary>
    public struct Spawn
    {
        public EnemyType Type; public V2 Pos; public bool Asleep;
        public Spawn(EnemyType t, float x, float y, bool asleep = false) { Type = t; Pos = new V2(x, y); Asleep = asleep; }
    }

    // ---- terreno das salas (CONTEUDO_REGIOES §1.1). So os dados; o comportamento de cada um fica com a raia da regiao (Run.RegiaoN.cs),
    //      menos o que e comum e ja esta no nucleo: lava solida para andar e transparente para tiro, agua que freia, luz/mira no escuro,
    //      bau enterrado e parede rachada disfarcada.
    /// <summary>Teia no chao (Webs): centro e raio.</summary>
    public struct Web { public V2 Pos; public float Radius; public Web(float x, float y, float r) { Pos = new V2(x, y); Radius = r; } }
    /// <summary>Respiro de fogo (Vents): centro, raio e fase do ciclo (s).</summary>
    public struct Vent { public V2 Pos; public float Radius, Phase; public Vent(float x, float y, float r, float phase) { Pos = new V2(x, y); Radius = r; Phase = phase; } }
    /// <summary>Trilho do vagonete (Rails): de A a B e fase do ciclo (s).</summary>
    public struct Rail { public V2 A, B; public float Phase; public Rail(float x1, float y1, float x2, float y2, float phase) { A = new V2(x1, y1); B = new V2(x2, y2); Phase = phase; } }
    /// <summary>Bau enterrado (Buried): so aparece com o Mapa nas maos, o perk Faro de bau ou o Treasure Detour; moedas fixas.</summary>
    public struct BuriedChest { public V2 Pos; public int Coins; public BuriedChest(float x, float y, int coins) { Pos = new V2(x, y); Coins = coins; } }

    /// <summary>Sala 9 x 14 m. Entrada embaixo no centro (Balance.Entrance); as portas da parede de cima saem do contrato (uma por sala
    /// da proxima camada). Threat = caveiras no icone da porta (0-3); Risk = icone de risco para a carga; ChestCoins = moedas do bau
    /// (0 = sem bau). Wave = reforco (1 por visita) quando WaveTrigger acontece; WaveLocks = as portas fecham ate o reforco morrer.
    /// Secret = sala secreta atras da parede rachada em SecretPos (parede lateral), ou -1.</summary>
    /// <summary>Sala. Code: a 1a letra diz o tipo (R normal, S secreta, M mini-chefe, B chefe, D entrega) e o resto o nome (R1, S2, M1,
    /// B1, D1 na regiao 1; ex. R2-3, S2-1, M2, B2, D2 nas outras). Id = indice global em Content.Rooms (nas regioes 2-4 o arquivo da regiao
    /// usa ids LOCAIS 0..n-1 e o registro soma a base; idem Secret). Region = 1-4.</summary>
    public sealed class RoomDef
    {
        public int Id { get; internal set; }
        public readonly string Code, Name, Lesson; public readonly int Threat;
        public readonly Rect[] Obstacles; public readonly Spawn[] Spawns;
        public readonly V2 ChestPos; public readonly int ChestCoins; public readonly V2 RescueSpot;
        public readonly Spawn[] Wave; public readonly RoomTrigger WaveTrigger; public readonly bool WaveLocks;
        public int Secret { get; internal set; }
        public readonly V2 SecretPos;
        readonly CargoRisk risk;
        /// <summary>Icone de risco da carga: o dos inimigos da sala e do reforco (+ Pancada se ha lava ou respiro).</summary>
        public CargoRisk Risk => risk | (Lava.Length > 0 || Vents.Length > 0 ? CargoRisk.Smash : CargoRisk.None);
        public int Region { get; internal set; } = 1;
        // terreno (CONTEUDO_REGIOES §1.1), escrito no inicializador da sala: new RoomDef(...) { Water = new[] { ... } }
        /// <summary>Sala escura (luz em volta do courier, das tochas e da lanterna; a mira so pega o que esta na luz). Torches = tochas nas paredes.</summary>
        public bool Dark { get; set; }
        public V2[] Torches { get; set; } = NoPoints;
        /// <summary>Agua (nao solida: freia quem anda nela) e lava (solida para andar, transparente para tiro).</summary>
        public Rect[] Water { get; set; } = NoRects;
        public Rect[] Lava { get; set; } = NoRects;
        public Web[] Webs { get; set; } = new Web[0];
        public Vent[] Vents { get; set; } = new Vent[0];
        public Rail[] Rails { get; set; } = new Rail[0];
        public BuriedChest[] Buried { get; set; } = new BuriedChest[0];
        /// <summary>Parede rachada disfarcada: so aparece e so abre com o Mapa, o Faro de bau ou o Treasure Detour.</summary>
        public bool SecretHidden { get; set; }
        /// <summary>Pontos de salto do Necromante (B3).</summary>
        public V2[] Blinks { get; set; } = NoPoints;
        /// <summary>Projeto de equipamento no bau desta sala (CONTEUDO_REGIOES §8): sai na 1a abertura e a porta o mostra enquanto nao foi pego.</summary>
        public Gear Blueprint { get; set; }
        static readonly V2[] NoPoints = new V2[0]; static readonly Rect[] NoRects = new Rect[0];
        public bool Delivery => Code[0] == 'D';
        public bool Boss => Code[0] == 'B';
        public bool Mini => Code[0] == 'M';
        public bool IsSecret => Code[0] == 'S';
        public bool Normal => Code[0] == 'R';
        public RoomDef(int id, string code, string name, string lesson, int threat, Rect[] obstacles, Spawn[] spawns, V2 rescueSpot, V2 chestPos = default, int chestCoins = 0,
                       Spawn[] wave = null, RoomTrigger trigger = RoomTrigger.None, bool waveLocks = false, int secret = -1, V2 secretPos = default)
        {
            Id = id; Code = code; Name = name; Lesson = lesson; Threat = threat; Obstacles = obstacles; Spawns = spawns; RescueSpot = rescueSpot; ChestPos = chestPos; ChestCoins = chestCoins;
            Wave = wave ?? new Spawn[0]; WaveTrigger = wave == null ? RoomTrigger.None : trigger; WaveLocks = waveLocks; Secret = secret; SecretPos = secretPos;
            foreach (Spawn s in Spawns) { risk |= EnemyDef.All[(int)s.Type].Risk; if (s.Type == EnemyType.Ogre) risk |= CargoRisk.Thief; }   // o Ogro chama ladroes
            foreach (Spawn s in Wave) risk |= EnemyDef.All[(int)s.Type].Risk;
        }
    }

    /// <summary>Contrato = camadas de salas (cada porta leva a uma sala da camada seguinte) terminando na entrega (D1). Pay = pagamento
    /// base em Coins antes dos multiplicadores de carga, integridade e Bolsa. EggTarget = tempo-alvo do ovo (s; 0 = nao e ovo).
    /// ParTime = tempo-alvo da 3a estrela; TimeLimit = cronometro da mensagem (0 = sem). Gerados (dia/codigo): Index = -1.</summary>
    public sealed class ContractDef
    {
        /// <summary>Indice global na campanha (Content.Contracts: regiao 1 = 0-23, depois as outras na ordem); -1 = gerado.</summary>
        public int Index { get; internal set; }
        public readonly string Code, Note; public readonly CargoType Cargo; public readonly int Pay; public readonly float EggTarget;
        public readonly int[][] Layers;
        /// <summary>Regiao (1-4): tema da View, cargas/perks do sorteio. No desafio sem fim = a maior regiao misturada.</summary>
        public int Region { get; internal set; } = 1;
        /// <summary>Template do evento (contrato do quadro de eventos) e a vaga dele no quadro (0-4); None/0 nos outros.</summary>
        public LiveEvent Event { get; internal set; }
        public int Slot { get; internal set; }
        /// <summary>Cristal de gelo: tempo (s) para derreter do cheio ao zero fora da agua = o campo EggTarget do contrato (0 nas outras cargas).</summary>
        public float MeltTime => Cargo == CargoType.IceCrystal ? EggTarget : 0f;
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
        /// <summary>Contrato com a sala de entrega `delivery` no fim (regioes 2-4 e gerados de outra regiao).</summary>
        internal ContractDef(int index, string code, CargoType cargo, int pay, float eggTarget, string note, int delivery, int[][] layers) : this(index, cargo, pay, eggTarget, note, layers)
        {
            Code = code; Layers[Layers.Length - 1] = new[] { delivery };
        }
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
}
