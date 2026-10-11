namespace DC.Core
{
    /// <summary>Regiao 4 "Forja do Dragao" (CONTEUDO_REGIOES §4): 14 salas (R4-1..R4-10, S4-1, M4, B4, D4; ids LOCAIS 0-13) e os 16 contratos
    /// M4-1..M4-16. Verbo do lugar: empurrar e ritmar (lava e parede baixa, respiros no ritmo, fogo no chao). Comportamentos em
    /// Run.Regiao4.cs. O chefe desta regiao (ultimo contrato da campanha) destrava o desafio sem fim.
    /// Cargas que estreiam aqui: Flytrap (M4-2), TreasureMap (M4-3). Inimigos: Salamander, Imp, KoboldBomber, MagmaElemental (+ MagmaShard);
    /// mini-chefe KoboldForeman (M4); chefe ElderDragon (B4, + SalamanderEgg).</summary>
    public static class Regiao4
    {
        public const int Id = 4;
        public const string Name = "Forja do Dragão", Theme = "forja";
        /// <summary>Estrelas para destravar (alem do chefe da regiao 3).</summary>
        public const int Stars = 95;
        /// <summary>Contrato gerado da regiao (CONTEUDO_REGIOES §1.2, PROVISORIO): pagamento = GenPay + 22 x camadas; tempos por sala (s) ate a
        /// entrega: 3a estrela, ovo, gelo (MeltTime) e mensagem. TierBase = nivel base dos gerados (= base da campanha 16 + 6).</summary>
        public const int GenPay = 220; public const float GenPar = 22f, GenEgg = 28f, GenMelt = 30f, GenLimit = 14f;
        public const int TierBase = 22;

        public const int R1 = 0, R2 = 1, R3 = 2, R4 = 3, R5 = 4, R6 = 5, R7 = 6, R8 = 7, R9 = 8, R10 = 9, S1 = 10, M = 11, B = 12, D = 13;
        /// <summary>Salas normais (ids locais) para o sorteio dos contratos gerados e do sem fim.</summary>
        public static readonly int[] Normal = { R1, R2, R3, R4, R5, R6, R7, R8, R9, R10 };

        static Rect O(float cx, float cy, float hx, float hy) => new Rect(cx, cy, hx, hy);
        static Spawn S(EnemyType t, float x, float y) => new Spawn(t, x, y);
        static V2 P(float x, float y) => new V2(x, y);
        static Vent V(float x, float y, float r, float phase) => new Vent(x, y, r, phase);
        static BuriedChest X(float x, float y, int coins) => new BuriedChest(x, y, coins);
        const EnemyType Bat = EnemyType.Bat, Thief = EnemyType.Thief, Guard = EnemyType.Guard, Brute = EnemyType.Brute, Slime = EnemyType.Slime,
                        Salamander = EnemyType.Salamander, Imp = EnemyType.Imp, KoboldBomber = EnemyType.KoboldBomber, MagmaElemental = EnemyType.MagmaElemental,
                        KoboldForeman = EnemyType.KoboldForeman, ElderDragon = EnemyType.ElderDragon;
        const RoomTrigger Half = RoomTrigger.Half, Down = RoomTrigger.CargoDown;

        // Icone de ameaca = terco do dano medio por visita do bot (docs/BALANCE_REGIAO4.md): 1 = R4-1, R4-5, R4-9 · 2 = R4-2, R4-3, R4-4, R4-6 ·
        // 3 = R4-7, R4-8, R4-10 (o desenho chutava R4-5 = 2, R4-6 = 3, R4-7 = 2). Lava ou respiro = icone Pancada (RoomDef.Risk). Projetos de equipamento (CONTEUDO_REGIOES §8): Armadilha de
        // urso em R4-3, R4-6 e S4-1; o 3o do Gancho em R4-4. Regras de desenho em CoreTestsRegiao4.LevelDesign_Regiao4_*.
        public static readonly RoomDef[] Rooms =
        {
            new RoomDef(R1, "R4-1", "Ponte de Basalto", "lava é parede baixa: não se anda nela, mas tiro, flecha e frasco passam por cima; inimigo empurrado na lava pelo dash leva 3 de dano", 1,
                new[] { O(4.5f, 10.5f, 0.4f, 0.4f) },
                new[] { S(Salamander, 4.5f, 9.4f), S(KoboldBomber, 7.5f, 11.5f) }, P(2.0f, 2.5f), P(4.5f, 5.0f), 35,
                wave: new[] { S(Salamander, 0.8f, 3.6f) }, trigger: Half)
            { Lava = new[] { O(0.7f, 7.0f, 0.7f, 0.7f), O(4.5f, 7.0f, 1.5f, 0.7f), O(8.3f, 7.0f, 0.7f, 0.7f) } },
            new RoomDef(R2, "R4-2", "Respiros", "respiro brilha 0,8 s e solta fogo por 1 s (2 de dano; racha a carga no chão; derrete gelo; 3 de dano em inimigo): passe logo depois do fogo", 2,
                new[] { O(1.0f, 8.5f, 0.5f, 0.3f), O(8.0f, 8.5f, 0.5f, 0.3f) },
                new[] { S(Bat, 1.5f, 11.0f), S(Slime, 7.5f, 10.5f) }, P(2.0f, 2.5f), P(8.2f, 6.6f), 40,
                wave: new[] { S(Bat, 8.2f, 3.6f) }, trigger: Half)
            {
                Vents = new[] { V(2.0f, 5.0f, 1.0f, 0.0f), V(4.5f, 5.0f, 1.0f, 2.0f), V(7.0f, 5.0f, 1.0f, 0.0f), V(3.2f, 8.5f, 1.0f, 1.0f), V(5.8f, 8.5f, 1.0f, 3.0f), V(4.5f, 11.0f, 1.0f, 2.0f) },
                Buried = new[] { X(0.8f, 3.4f, 40) },
            },
            new RoomDef(R3, "R4-3", "Ninho de Salamandras", "a salamandra deixa rastro de fogo por 3 s (1 de dano ao pisar; derrete o gelo) e circula você a 2–3 m: não deixe ela fechar o círculo em volta da carga; parede rachada à esquerda", 2,
                new[] { O(2.5f, 7.0f, 0.6f, 0.6f), O(6.5f, 7.0f, 0.6f, 0.6f), O(4.5f, 10.5f, 1.0f, 0.4f) },
                new[] { S(Salamander, 1.5f, 10.0f), S(Salamander, 7.5f, 10.0f), S(Thief, 8.0f, 12.0f) }, P(2.0f, 2.5f), P(0.8f, 12.0f), 40,
                wave: new[] { S(Salamander, 8.2f, 3.6f) }, trigger: Half, secret: S1, secretPos: P(0f, 5.0f))
            { Lava = new[] { O(4.5f, 4.6f, 1.0f, 0.4f) }, Blueprint = Gear.Trap },
            new RoomDef(R4, "R4-4", "Oficina do Diabrete", "o diabrete troca de lugar com a carga no chão (redemoinho de 1 s nos dois pontos; precisa ver a carga): acerte-o no redemoinho ou pouse atrás de uma bigorna", 2,
                new[] { O(2.0f, 6.5f, 0.6f, 0.4f), O(7.0f, 6.5f, 0.6f, 0.4f), O(4.5f, 9.5f, 0.6f, 0.4f) },
                new[] { S(Imp, 7.5f, 11.0f), S(Imp, 2.0f, 12.0f), S(Bat, 4.5f, 12.5f) }, P(2.0f, 2.5f), P(8.2f, 9.0f), 45,
                wave: new[] { S(Imp, 8.2f, 3.6f) }, trigger: Half)
            { Lava = new[] { O(0.6f, 10.5f, 0.6f, 1.0f) }, Buried = new[] { X(4.5f, 11.5f, 45) }, Blueprint = Gear.Hook },
            new RoomDef(R5, "R4-5", "Bancada dos Kobolds", "o kobold joga frasco em arco (círculo de aviso 0,9 s) que vira poça de fogo de 1 m por 4 s: não pouse onde o frasco cai, mude de lugar", 1,
                new[] { O(2.0f, 6.0f, 0.6f, 0.4f), O(7.0f, 6.0f, 0.6f, 0.4f), O(4.5f, 8.5f, 1.4f, 0.3f) },
                new[] { S(KoboldBomber, 2.0f, 11.5f), S(KoboldBomber, 7.0f, 11.5f), S(Thief, 8.0f, 9.6f) }, P(2.0f, 2.5f), P(0.8f, 9.0f), 40,
                wave: new[] { S(Salamander, 1.0f, 12.5f), S(Bat, 8.0f, 12.5f) }, trigger: Down),
            new RoomDef(R6, "R4-6", "Fundição", "o elemental de magma ao morrer se divide em 2 brasas; lava dos lados: empurre as brasas na lava com o dash", 2,
                new[] { O(4.5f, 10.8f, 0.8f, 0.4f) },
                new[] { S(MagmaElemental, 4.5f, 9.0f), S(MagmaElemental, 2.6f, 11.6f), S(Bat, 7.0f, 11.6f) }, P(2.0f, 2.5f), P(8.2f, 4.0f), 45,
                wave: new[] { S(Salamander, 0.8f, 3.6f), S(Salamander, 8.2f, 3.6f) }, trigger: Half)
            { Lava = new[] { O(1.0f, 7.5f, 1.0f, 1.5f), O(8.0f, 7.5f, 1.0f, 1.5f) }, Buried = new[] { X(4.5f, 5.5f, 45) }, Blueprint = Gear.Trap },
            new RoomDef(R7, "R4-7", "Guarda da Forja", "luta obrigatória entre dois respiros: pouse a carga no lado frio e conte o ritmo", 3,
                new[] { O(4.5f, 6.0f, 1.4f, 0.4f) },
                new[] { S(Guard, 4.5f, 12.4f), S(KoboldBomber, 1.5f, 10.5f), S(Imp, 7.5f, 10.5f) }, P(7.0f, 2.5f),
                wave: new[] { S(Imp, 8.0f, 12.5f) }, trigger: Down)
            { Vents = new[] { V(2.5f, 8.2f, 1.0f, 0.0f), V(6.5f, 8.2f, 1.0f, 2.0f) } },
            new RoomDef(R8, "R4-8", "Emboscada de Brasa", "emboscada: passar da metade traz um elemental e dois diabretes e a porta fecha; os respiros acertam inimigos também", 3,
                new[] { O(2.5f, 5.5f, 0.6f, 0.4f), O(6.5f, 5.5f, 0.6f, 0.4f), O(4.5f, 9.0f, 1.2f, 0.4f) },
                new[] { S(Salamander, 8.0f, 10.5f), S(KoboldBomber, 1.5f, 11.5f) }, P(2.0f, 2.5f),
                wave: new[] { S(MagmaElemental, 4.5f, 12.5f), S(Imp, 1.5f, 12.8f), S(Imp, 7.5f, 12.8f) }, trigger: Half, waveLocks: true)
            { Vents = new[] { V(1.5f, 8.0f, 1.0f, 1.0f), V(7.5f, 8.0f, 1.0f, 3.0f) }, Buried = new[] { X(8.2f, 3.8f, 50) } },
            new RoomDef(R9, "R4-9", "Sala das Correntes", "baú enterrado (X) e parede rachada disfarçada só aparecem com o Mapa ou o perk Faro de baú; ladrões correm 30% mais atrás do Mapa", 1,
                new[] { O(4.5f, 7.5f, 0.6f, 0.4f), O(2.0f, 10.0f, 0.5f, 0.5f), O(7.0f, 10.0f, 0.5f, 0.5f) },
                new[] { S(Thief, 1.0f, 8.0f), S(Thief, 8.0f, 8.0f), S(Imp, 4.5f, 12.0f) }, P(2.0f, 2.5f),
                wave: new[] { S(Thief, 8.0f, 12.5f) }, trigger: Down, secret: S1, secretPos: P(Balance.RoomW, 5.5f))
            { Buried = new[] { X(1.0f, 5.5f, 60) }, SecretHidden = true },
            new RoomDef(R10, "R4-10", "Coração da Forja", "Bruto, elemental, kobold e salamandra entre respiros e um poço de lava; passar da metade chama reforço e fecha a porta", 3,
                new[] { O(1.5f, 10.5f, 0.5f, 0.5f), O(7.5f, 10.5f, 0.5f, 0.5f) },
                new[] { S(Brute, 3.0f, 11.8f), S(MagmaElemental, 6.0f, 11.8f), S(KoboldBomber, 8.2f, 8.5f) }, P(2.0f, 2.5f), P(0.8f, 8.5f), 55,
                wave: new[] { S(Imp, 1.0f, 12.8f), S(Salamander, 8.0f, 12.8f), S(KoboldBomber, 4.5f, 12.8f) }, trigger: Half, waveLocks: true)
            {
                Lava = new[] { O(4.5f, 9.5f, 1.0f, 0.5f) },
                Vents = new[] { V(2.0f, 6.5f, 1.0f, 0.0f), V(7.0f, 6.5f, 1.0f, 2.0f) },
                Buried = new[] { X(8.2f, 11.6f, 55) },
            },
            new RoomDef(S1, "S4-1", "Cofre do Ferreiro", "sala secreta: baú grande, um baú enterrado e um diabrete", 1,
                new[] { O(2.0f, 6.0f, 0.5f, 0.5f), O(7.0f, 6.0f, 0.5f, 0.5f) },
                new[] { S(Imp, 6.5f, 11.0f) }, P(2.0f, 2.5f), P(4.5f, 10.0f), 110)
            { Buried = new[] { X(1.0f, 11.5f, 50) }, Blueprint = Gear.Trap },
            new RoomDef(M, "M4", "Pátio do Capataz", "mini-chefe: chicote de 3 m, chama kobolds e joga frascos; respiros dos lados", 3,
                new[] { O(4.5f, 6.0f, 0.8f, 0.4f) },
                new[] { S(KoboldForeman, 4.5f, 11.0f), S(KoboldBomber, 8.0f, 12.0f) }, P(2.0f, 2.5f))
            { Vents = new[] { V(2.0f, 8.4f, 1.0f, 0.0f), V(7.0f, 8.4f, 1.0f, 2.0f) } },
            new RoomDef(B, "B4", "Ninho do Dragão", "chefe final: sopro em cone (os pilares cobrem), voo e pouso em área, ovos de salamandra; fissuras de lava na fase 2", 3,
                new[] { O(2.0f, 6.0f, 0.6f, 0.6f), O(7.0f, 6.0f, 0.6f, 0.6f), O(2.0f, 9.5f, 0.6f, 0.6f), O(7.0f, 9.5f, 0.6f, 0.6f) },
                new[] { S(ElderDragon, 4.5f, 11.0f) }, P(4.5f, 3.0f))
            {
                Lava = new[] { O(0.4f, 11.4f, 0.4f, 0.7f), O(8.6f, 11.4f, 0.4f, 0.7f) },
                Vents = new[] { V(4.5f, 7.8f, 1.2f, 0.0f), V(1.0f, 4.2f, 1.0f, 2.0f), V(8.0f, 4.2f, 1.0f, 4.0f) },
            },
            new RoomDef(D, "D4", "Entrega", "o cliente espera no topo", 0,
                new[] { O(2.0f, 7.0f, 0.5f, 0.5f), O(7.0f, 7.0f, 0.5f, 0.5f) },
                new Spawn[0], P(2.0f, 2.5f)),
        };

        static int[] L(params int[] rooms) => rooms;
        /// <summary>= Content.Make, sem passar pelo Content: chamar o Content daqui fecharia o ciclo de inicializacao estatica (o Content le
        /// Regiao4.Contracts no construtor dele; quem tocasse Regiao4 antes veria Contracts nulo).</summary>
        static ContractDef Make(string code, CargoType cargo, int pay, string note, float par, float egg, float limit, int tier, bool waves, int delivery, params int[][] layers)
            => new ContractDef(-1, code, cargo, pay, egg, note, delivery, layers) { ParTime = par, TimeLimit = limit, Tier = tier, Waves = waves, Name = note };
        const CargoType Box = CargoType.Box, Relic = CargoType.Relic, Egg = CargoType.Egg, Cake = CargoType.Cake, Msg = CargoType.Message, Heavy = CargoType.Heavy,
                        Barrel = CargoType.Barrel, Ice = CargoType.IceCrystal, Balloon = CargoType.Balloon, Flytrap = CargoType.Flytrap, Map = CargoType.TreasureMap;

        // Contratos (CONTEUDO_REGIOES §4.1): Tier = 16 + indice local; os 3 que ensinam sem reforco; M4-8 = mini-chefe, M4-16 = chefe final.
        // Tempos pela medicao do bot (docs/BALANCE_REGIAO4.md; regra da regiao 1): ParTime = 1,6x a duracao do bot, ovo 2x, gelo (MeltTime no
        // campo do ovo) 2,2x, mensagem 2,5x. O desenho chutava 60/90/85/155/160/185/170/225/195/190/195/85/195/205/210/355.
        public static readonly ContractDef[] Contracts =
        {
            Make("M4-1", Box, 310, "região 4: lava e respiros", 35f, 0f, 0f, 16, false, D, L(R1), L(R2, R3)),
            Make("M4-2", Flytrap, 313, "planta carnívora: torre que morde todo mundo", 65f, 0f, 0f, 17, false, D, L(R3), L(R1, R4), L(R2, R5)),
            Make("M4-3", Map, 316, "mapa do tesouro: baús enterrados e ladrões mais rápidos", 60f, 0f, 0f, 18, false, D, L(R9), L(R1, R5), L(R4, R3)),
            Make("M4-4", Relic, 320, "relíquia na forja", 80f, 0f, 0f, 19, true, D, L(R2), L(R4, R1), L(R5, R3), L(R7, R9), L(R6, R2)),
            Make("M4-5", Ice, 324, "cristal perto da lava: derrete 3x mais rápido", 155f, 215f, 0f, 20, true, D, L(R1), L(R5, R9), L(R3, R4), L(R7, R2), L(R10, R6)),
            Make("M4-6", Balloon, 328, "balão entre respiros: fogo fura", 150f, 0f, 0f, 21, true, D, L(R4), L(R2, R9), L(R1, R6), L(R3, R5), L(R8, R7), L(R4, R1)),
            Make("M4-7", Flytrap, 332, "planta em 5 camadas", 155f, 0f, 0f, 22, true, D, L(R7), L(R1, R8), L(R3, R5, R2), L(R6, R9), L(R10, R4)),
            Make("M4-8", Box, 380, "mini-chefe: Capataz Kobold", 185f, 0f, 0f, 23, true, D, L(R5), L(R3, R6), L(R9, R1), L(R7, R4), L(R2, R8), L(M)),
            Make("M4-9", Egg, 340, "ovo na forja", 105f, 135f, 0f, 24, true, D, L(R1), L(R6, R2), L(R10, R3), L(R4, R9), L(R5, R7), L(R8, R6)),
            Make("M4-10", Map, 344, "mapa em 6 camadas", 170f, 0f, 0f, 25, true, D, L(R9), L(R4, R2), L(R6, R10), L(R1, R5), L(R7, R3), L(R9, R8)),
            Make("M4-11", Barrel, 348, "barril perto do fogo: fogo no chão acende o pavio", 205f, 0f, 0f, 26, true, D, L(R3), L(R7, R1), L(R5, R10), L(R2, R9), L(R8, R4), L(R6, R3)),
            Make("M4-12", Msg, 352, "mensagem na forja", 55f, 0f, 85f, 27, true, D, L(R4), L(R6, R9), L(R1, R8, R5), L(R10, R2), L(R7, R3), L(R4, R6)),
            Make("M4-13", Heavy, 356, "caixa pesada entre respiros", 130f, 0f, 0f, 28, true, D, L(R2), L(R9, R5), L(R8, R3), L(R4, R6), L(R1, R10), L(R7, R2)),
            Make("M4-14", Flytrap, 360, "planta em 6 camadas", 155f, 0f, 0f, 29, true, D, L(R6), L(R3, R8), L(R10, R1), L(R5, R7, R4), L(R2, R9), L(R8, R3)),
            Make("M4-15", Cake, 364, "bolo na forja", 180f, 0f, 0f, 30, true, D, L(R10), L(R2, R7), L(R4, R9), L(R6, R1, R5), L(R3, R8), L(R10, R4)),
            Make("M4-16", Box, 480, "chefe final: Dragão Ancião", 415f, 0f, 0f, 31, true, D, L(R10), L(R8, R7), L(R6, R5, R2), L(R3, R4), L(B)),
        };
    }
}
