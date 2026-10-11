namespace DC.Core
{
    /// <summary>Regiao 2 "Minas Alagadas" (CONTRATO_LANCAMENTO §2-§4; desenho em docs/CONTEUDO_REGIOES.md §2; medicao em
    /// docs/BALANCE_REGIAO2.md): 14 salas (R2-1..R2-10, S2-1, M2, B2, D2) e os contratos M2-1..M2-16. Comportamentos em Run.Regiao2.cs.
    /// Regras do arquivo:
    ///  - ids de sala LOCAIS 0..n-1, na ordem do array Rooms (o registro em Content soma a base global; o Secret tambem e local);
    ///  - Code da sala: R2-1..R2-10 normais, S2-1 secreta, M2 mini-chefe, B2 chefe, D2 entrega (a 1a letra e o tipo);
    ///  - contratos com Content.Make(code, carga, pagamento, nota, par, ovo/gelo, limite, tier, reforcos, D, camadas...) em ids locais;
    ///  - so crescem no fim (o save guarda ids globais): ver o aviso em Content.</summary>
    public static class Regiao2
    {
        public const int Id = 2;
        public const string Name = "Minas Alagadas", Theme = "minas";
        /// <summary>Estrelas para destravar (alem do Ogro vencido = ultimo contrato da regiao 1).</summary>
        public const int Stars = 30;
        /// <summary>Contrato gerado da regiao (CONTEUDO_REGIOES §1.2): pagamento = GenPay + 22 x camadas; tempos por sala (s) ate a entrega:
        /// 3a estrela, ovo, gelo (MeltTime) e mensagem. Nivel base dos gerados = base da regiao (8) + 6.</summary>
        public const int GenPay = 110; public const float GenPar = 20f, GenEgg = 25f, GenMelt = 27f, GenLimit = 13f;
        public const int TierBase = 14;

        public const int R1 = 0, R2 = 1, R3 = 2, R4 = 3, R5 = 4, R6 = 5, R7 = 6, R8 = 7, R9 = 8, R10 = 9, S1 = 10, M = 11, B = 12, D = 13;
        /// <summary>Salas normais (ids locais) para o sorteio dos contratos gerados.</summary>
        public static readonly int[] Normal = { R1, R2, R3, R4, R5, R6, R7, R8, R9, R10 };

        static Rect O(float cx, float cy, float hx, float hy) => new Rect(cx, cy, hx, hy);
        static Spawn S(EnemyType t, float x, float y) => new Spawn(t, x, y);
        /// <summary>Spawn dormindo (zZ).</summary>
        static Spawn Z(EnemyType t, float x, float y) => new Spawn(t, x, y, true);
        static V2 P(float x, float y) => new V2(x, y);
        static Rail T(float x1, float y1, float x2, float y2, float phase) => new Rail(x1, y1, x2, y2, phase);
        static BuriedChest Bc(float x, float y, int coins) => new BuriedChest(x, y, coins);
        const EnemyType Bat = EnemyType.Bat, Thief = EnemyType.Thief, Guard = EnemyType.Guard, Brute = EnemyType.Brute, Archer = EnemyType.Archer,
                        Slime = EnemyType.Slime, GoblinMiner = EnemyType.GoblinMiner, Mole = EnemyType.Mole, BogFrog = EnemyType.BogFrog,
                        BombBeetle = EnemyType.BombBeetle, Minecart = EnemyType.Minecart, MoleQueen = EnemyType.MoleQueen, StoneGolem = EnemyType.StoneGolem;
        const RoomTrigger Half = RoomTrigger.Half, Down = RoomTrigger.CargoDown;

        // Ameaca (caveiras) = hipotese do desenho (CONTEUDO_REGIOES §2.1); a medicao do bot esta em BALANCE_REGIAO2.md.
        // Regras de desenho (CoreTestsRegiao2.LevelDesign_Regioes_*): as da regiao 1 + agua a 1,2 m+ da entrada; trilho em 4 <= y <= 11,5 sem
        // obstaculo em cima; nenhum spawn, bau, bau enterrado ou resgate a menos de 1 m de um trilho; vagonete numa ponta de cada trilho.
        public static readonly RoomDef[] Rooms =
        {
            new RoomDef(R1, "R2-1", "Galeria Alagada", "água freia todos 25% (courier, ladrão, limo): pouse a carga na poça e o ladrão chega mais devagar; o cristal recongela na água; parede rachada à direita", 1,
                new[] { O(1.5f, 8.5f, 0.5f, 0.5f), O(7.5f, 8.5f, 0.5f, 0.5f), O(4.5f, 11.0f, 0.8f, 0.4f) },
                new[] { S(Thief, 8.0f, 10.0f), S(Bat, 1.5f, 11.5f) }, P(2.0f, 3.0f), P(8.2f, 6.5f), 30,
                wave: new[] { S(Slime, 0.8f, 3.6f) }, trigger: Half, secret: S1, secretPos: P(Balance.RoomW, 4.0f))
            { Water = new[] { O(4.5f, 6.5f, 2.5f, 1.4f) }, Blueprint = Gear.Boots },
            new RoomDef(R2, "R2-2", "Trilho do Vagonete", "o vagonete cruza a sala a cada ~4,7 s (sino e trilho acesos 1,2 s antes): atravesse logo depois dele; ele atropela inimigos: traga o ladrão até o trilho", 2,
                new[] { O(2.0f, 9.6f, 0.6f, 0.6f), O(7.0f, 9.6f, 0.6f, 0.6f), O(4.5f, 4.0f, 0.8f, 0.3f) },
                new[] { S(Minecart, 0.6f, 6.5f), S(Thief, 1.5f, 11.5f), S(GoblinMiner, 7.0f, 11.6f), S(Bat, 4.5f, 11.8f) }, P(1.5f, 3.0f),
                wave: new[] { S(Thief, 8.0f, 12.5f) }, trigger: Down)
            { Rails = new[] { T(0.0f, 6.5f, 9.0f, 6.5f, 0.0f) } },
            new RoomDef(R3, "R2-3", "Mina Adormecida", "mineiros dormindo (zZ) só acordam com o courier a 2 m, grito, explosão ou golpe: o corredor do meio passa em silêncio; o baú fica colado num dorminhoco", 1,
                new[] { O(2.5f, 6.0f, 1.0f, 0.4f), O(6.5f, 6.0f, 1.0f, 0.4f), O(4.5f, 9.5f, 0.6f, 0.6f) },
                new[] { Z(GoblinMiner, 0.8f, 10.5f), Z(GoblinMiner, 8.2f, 10.5f), S(Bat, 4.5f, 12.0f) }, P(2.0f, 3.0f), P(8.2f, 12.0f), 40,
                wave: new[] { S(Bat, 0.8f, 3.6f) }, trigger: Half),
            new RoomDef(R4, "R2-4", "Toca da Toupeira", "rastro de terra = toupeira por baixo: ela sai embaixo da carga no chão e a empurra 1,5 m; carregue a carga ou acerte-a nos 2 s em que fica fora; na água ela não sai", 2,
                new[] { O(2.0f, 7.5f, 0.6f, 0.6f), O(7.0f, 7.5f, 0.6f, 0.6f) },
                new[] { S(Mole, 4.5f, 10.0f), S(Mole, 1.5f, 11.5f), S(Bat, 7.5f, 11.5f) }, P(1.5f, 3.0f), P(0.8f, 9.5f), 35,
                wave: new[] { S(Mole, 8.0f, 12.0f) }, trigger: Down)
            { Water = new[] { O(6.8f, 4.6f, 1.4f, 1.1f) }, Buried = new[] { Bc(8.2f, 12.0f, 30) } },
            new RoomDef(R5, "R2-5", "Lodaçal do Sapo", "o sapo puxa a carga do chão a até 3 m (linha de aviso 0,7 s): pouse fora do anel dele e abata-o de 3 a 5 m", 2,
                new[] { O(4.5f, 5.0f, 0.6f, 0.6f), O(4.5f, 10.6f, 0.6f, 0.6f) },
                new[] { S(BogFrog, 2.0f, 9.5f), S(BogFrog, 7.0f, 7.0f), S(Thief, 8.0f, 11.5f) }, P(2.0f, 3.0f), P(0.8f, 6.0f), 35,
                wave: new[] { S(Slime, 0.8f, 3.6f) }, trigger: Half)
            { Water = new[] { O(2.0f, 9.5f, 1.4f, 1.2f), O(7.0f, 7.0f, 1.4f, 1.2f) }, Blueprint = Gear.Boots },
            new RoomDef(R6, "R2-6", "Corredor dos Besouros", "o besouro corre até você, brilha 1 s e explode (1,5 m): atire de longe ou dê dash; a explosão acerta inimigos: leve-o até o Bruto", 3,
                new[] { O(2.0f, 6.0f, 0.5f, 0.5f), O(7.0f, 6.0f, 0.5f, 0.5f), O(4.5f, 8.5f, 0.5f, 0.5f) },
                new[] { S(BombBeetle, 1.5f, 11.0f), S(BombBeetle, 7.5f, 11.0f), S(Brute, 4.5f, 11.0f) }, P(2.0f, 3.0f), P(8.2f, 9.0f), 40,
                wave: new[] { S(BombBeetle, 0.8f, 3.6f), S(BombBeetle, 8.2f, 3.6f) }, trigger: Half),
            new RoomDef(R7, "R2-7", "Cruzamento de Trilhos", "dois trilhos com ritmos opostos: espere na ilha do meio; o ladrão que corre para a carga cruza o trilho e pode ser atropelado; parede rachada à esquerda", 2,
                new[] { O(2.5f, 7.2f, 0.6f, 0.5f), O(6.5f, 7.2f, 0.6f, 0.5f), O(4.5f, 11.6f, 0.8f, 0.3f) },
                new[] { S(Minecart, 0.6f, 4.8f), S(Minecart, 8.4f, 9.6f), S(Thief, 8.0f, 7.2f), S(Archer, 7.5f, 11.6f), S(Bat, 1.5f, 11.8f) }, P(1.5f, 2.8f), P(0.8f, 11.2f), 40,
                wave: new[] { S(Thief, 8.0f, 12.5f) }, trigger: Down, secret: S1, secretPos: P(0f, 7.2f))
            { Rails = new[] { T(0.0f, 4.8f, 9.0f, 4.8f, 0.0f), T(0.0f, 9.6f, 9.0f, 9.6f, 2.35f) }, Blueprint = Gear.Smoke },
            new RoomDef(R8, "R2-8", "Eclusa do Guarda", "luta obrigatória: pouse a carga na água (ladrão mais lento, a toupeira não sai ali) e derrube o Guarda", 2,
                new[] { O(1.2f, 9.5f, 0.4f, 1.0f), O(7.8f, 9.5f, 0.4f, 1.0f) },
                new[] { S(Guard, 4.5f, 12.4f), S(Thief, 1.0f, 6.5f), S(Mole, 7.8f, 6.0f) }, P(7.0f, 2.8f),
                wave: new[] { S(Thief, 8.0f, 12.5f) }, trigger: Down)
            { Water = new[] { O(4.5f, 7.0f, 2.2f, 1.2f) } },
            new RoomDef(R9, "R2-9", "Desmoronamento", "emboscada: passar da metade faz mineiros e um besouro caírem do teto e a porta fecha; a explosão do besouro acerta os mineiros", 3,
                new[] { O(2.5f, 5.5f, 0.6f, 0.6f), O(6.5f, 5.5f, 0.6f, 0.6f), O(4.5f, 9.0f, 1.2f, 0.4f) },
                new[] { Z(GoblinMiner, 8.0f, 7.6f), S(Bat, 1.5f, 10.5f) }, P(2.0f, 2.8f),
                wave: new[] { S(GoblinMiner, 1.5f, 12.5f), S(GoblinMiner, 7.5f, 12.5f), S(BombBeetle, 4.5f, 12.8f) }, trigger: Half, waveLocks: true)
            { Buried = new[] { Bc(8.2f, 4.0f, 35) } },
            new RoomDef(R10, "R2-10", "Salão da Bomba d'Água", "Bruto, sapo e mineiro juntos: o Bruto empurra você para o anel do sapo; carga 3 s no chão chama reforço e fecha a porta", 3,
                new[] { O(1.5f, 5.5f, 0.6f, 0.6f), O(7.5f, 5.5f, 0.6f, 0.6f), O(1.5f, 10.0f, 0.6f, 0.6f), O(7.5f, 10.0f, 0.6f, 0.6f) },
                new[] { S(Brute, 2.6f, 11.8f), S(BogFrog, 4.5f, 8.0f), S(GoblinMiner, 7.5f, 12.0f) }, P(2.0f, 2.8f), P(0.8f, 8.0f), 45,
                wave: new[] { S(BombBeetle, 1.0f, 12.5f), S(Thief, 8.0f, 12.5f), S(Bat, 4.5f, 12.8f) }, trigger: Down, waveLocks: true)
            { Water = new[] { O(4.5f, 8.0f, 1.6f, 1.2f) } },
            new RoomDef(S1, "S2-1", "Veio de Ouro", "sala secreta: baú grande ao lado de um mineiro dormindo", 0,
                new[] { O(2.0f, 6.0f, 0.5f, 0.5f), O(7.0f, 6.0f, 0.5f, 0.5f) },
                new[] { Z(GoblinMiner, 6.0f, 10.5f) }, P(2.0f, 2.5f), P(4.5f, 10.0f), 90)
            { Blueprint = Gear.Boots },
            new RoomDef(M, "M2", "Ninho da Rainha Toupeira", "mini-chefe: a Rainha cava, sai embaixo da carga no chão e chama toupeiras; nas poças ela não sai", 3,
                new[] { O(2.5f, 9.5f, 0.5f, 0.5f), O(6.5f, 9.5f, 0.5f, 0.5f) },
                new[] { S(MoleQueen, 4.5f, 10.8f), S(Mole, 7.5f, 11.5f) }, P(2.0f, 2.5f))
            { Water = new[] { O(1.6f, 6.0f, 1.0f, 1.0f), O(7.4f, 6.0f, 1.0f, 1.0f) } },
            new RoomDef(B, "B2", "Câmara do Golem", "chefe: pancada em área, pedras que fecham rotas, ponto fraco nas costas; o vagonete acerta o Golem (6 de dano e atordoa)", 3,
                new[] { O(1.8f, 7.6f, 0.5f, 0.5f), O(7.2f, 7.6f, 0.5f, 0.5f), O(1.8f, 11.2f, 0.5f, 0.5f), O(7.2f, 11.2f, 0.5f, 0.5f) },
                new[] { S(StoneGolem, 4.5f, 9.5f), S(Minecart, 0.6f, 4.6f) }, P(2.0f, 2.5f))
            { Rails = new[] { T(0.0f, 4.6f, 9.0f, 4.6f, 0.0f) } },
            new RoomDef(D, "D2", "Entrega", "o cliente espera no topo", 0,
                new[] { O(2.0f, 7.0f, 0.5f, 0.5f), O(7.0f, 7.0f, 0.5f, 0.5f) },
                new Spawn[0], P(2.0f, 2.5f)),
        };

        static int[] L(params int[] rooms) => rooms;
        /// <summary>= Content.Make, sem passar pelo Content: chamar o Content daqui fecharia o ciclo de inicializacao estatica (Content le
        /// Regiao2.Contracts no construtor dele) e quem tocasse Regiao2 antes do Content veria Contracts nulo.</summary>
        static ContractDef Make(string code, CargoType cargo, int pay, string note, float par, float egg, float limit, int tier, bool waves, int delivery, params int[][] layers)
            => new ContractDef(-1, code, cargo, pay, egg, note, delivery, layers) { ParTime = par, TimeLimit = limit, Tier = tier, Waves = waves, Name = note };
        const CargoType Box = CargoType.Box, Relic = CargoType.Relic, Egg = CargoType.Egg, Cake = CargoType.Cake, Msg = CargoType.Message,
                        Heavy = CargoType.Heavy, Barrel = CargoType.Barrel, Heart = CargoType.ArcaneHeart, Ice = CargoType.IceCrystal, Cage = CargoType.GriffinCage;

        // Nivel (Tier) = base 8 + indice local; os 3 primeiros ensinam (sem reforco). Tempos pela estimativa do desenho (CONTEUDO_REGIOES §10.2)
        // com as regras da decisao 36 (ParTime 1,6x, ovo 2x, gelo 2,2x = MeltTime no campo do ovo, mensagem 2,5x); medicao: BALANCE_REGIAO2.md.
        public static readonly ContractDef[] Contracts =
        {
            Make("M2-1", Box, 195, "região 2: água e vagonete; mineiros dormindo", 50f, 0f, 0f, 8, false, D, L(R1), L(R2, R3)),
            Make("M2-2", Egg, 200, "toupeira e sapo; o ovo choca no tempo-alvo", 80f, 100f, 0f, 9, false, D, L(R4), L(R3, R5), L(R2, R1)),
            Make("M2-3", Ice, 205, "cristal de gelo: derrete; a água recongela", 85f, 115f, 0f, 10, false, D, L(R1), L(R5, R8), L(R4, R7)),
            Make("M2-4", Relic, 210, "relíquia entre picaretas e trilhos", 145f, 0f, 0f, 11, true, D, L(R6), L(R3, R2), L(R5, R4), L(R9, R7), L(R1, R8)),
            Make("M2-5", Heart, 214, "coração arcano: ímã de moedas e de inimigos", 140f, 0f, 0f, 12, true, D, L(R3), L(R6, R1), L(R4, R8), L(R7, R5), L(R10, R2)),
            Make("M2-6", Barrel, 218, "barril com besouros: a explosão detona o barril", 165f, 0f, 0f, 13, true, D, L(R1), L(R2, R5), L(R8, R3), L(R4, R6), L(R7, R9), L(R5, R1)),
            Make("M2-7", Ice, 222, "cristal em 5 camadas", 155f, 210f, 0f, 14, true, D, L(R8), L(R1, R7), L(R5, R3, R4), L(R6, R2), L(R10, R9)),
            Make("M2-8", Box, 270, "mini-chefe: Rainha Toupeira", 200f, 0f, 0f, 15, true, D, L(R2), L(R4, R6), L(R9, R1), L(R8, R5), L(R3, R7), L(M)),
            Make("M2-9", Msg, 230, "mensagem: trilhos não esperam", 75f, 0f, 120f, 16, true, D, L(R5), L(R8, R3), L(R10, R2), L(R4, R9), L(R1, R6), L(R7, R8)),
            Make("M2-10", Cage, 234, "gaiola do grifinho: o grito acorda a mina", 140f, 0f, 0f, 17, true, D, L(R3), L(R1, R4), L(R9, R7), L(R2, R5), L(R6, R8)),
            Make("M2-11", Heart, 238, "coração em 6 camadas", 175f, 0f, 0f, 18, true, D, L(R7), L(R9, R3), L(R6, R10), L(R5, R2), L(R8, R4), L(R1, R7)),
            Make("M2-12", Cake, 242, "bolo sobre trilhos", 175f, 0f, 0f, 19, true, D, L(R4), L(R6, R1), L(R2, R10), L(R9, R8), L(R3, R5), L(R7, R4)),
            Make("M2-13", Heavy, 246, "caixa pesada na água", 170f, 0f, 0f, 20, true, D, L(R1), L(R5, R7), L(R8, R10, R3), L(R2, R6), L(R9, R4), L(R5, R1)),
            Make("M2-14", Cage, 250, "gaiola em 6 camadas", 175f, 0f, 0f, 21, true, D, L(R6), L(R3, R10), L(R7, R2), L(R4, R9), L(R8, R1), L(R5, R3)),
            Make("M2-15", Ice, 254, "cristal em 6 camadas", 190f, 260f, 0f, 22, true, D, L(R10), L(R2, R8), L(R6, R5), L(R9, R1, R4), L(R3, R7), L(R10, R6)),
            Make("M2-16", Box, 340, "chefe: Golem de Pedra", 315f, 0f, 0f, 23, true, D, L(R10), L(R8, R7), L(R9, R6, R3), L(R5, R2), L(R4, R1), L(B)),
        };
    }
}
