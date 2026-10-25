namespace DC.Core
{
    /// <summary>Regiao 3 "Cripta Assombrada" (CONTRATO_LANCAMENTO §2-§4; desenho em docs/historico/CONTEUDO_REGIOES.md §3; medicao em
    /// docs/historico/BALANCE_REGIAO3.md). 14 salas (R3-1..R3-10, S3-1, M3, B3, D3) e os 16 contratos M3-1..M3-16, com ids LOCAIS (o registro em
    /// Content soma a base). Todas as salas sao escuras (Dark + tochas), menos a entrega D3. Comportamentos em Run.Regiao3.cs.
    /// Cargas que estreiam aqui: Lantern (M3-1), Mirror (M3-3), Balloon (M3-5). Inimigos: Ghost, Skeleton, Spider, Mummy, Gargoyle;
    /// mini-chefe SpiderWidow (M3); chefe Necromancer (B3).</summary>
    public static class Regiao3
    {
        public const int Id = 3;
        public const string Name = "Cripta Assombrada", Theme = "cripta";
        /// <summary>Estrelas para destravar (alem do chefe da regiao 2).</summary>
        public const int Stars = 60;
        /// <summary>Contrato gerado da regiao (CONTEUDO_REGIOES §1.2): pagamento = GenPay + 22 x camadas; tempos por sala (s) ate a entrega:
        /// 3a estrela, ovo, gelo (MeltTime) e mensagem. Nivel base dos gerados = base da regiao (12) + 6.</summary>
        public const int GenPay = 165; public const float GenPar = 22f, GenEgg = 28f, GenMelt = 30f, GenLimit = 14f;
        public const int TierBase = 18;

        public const int R1 = 0, R2 = 1, R3 = 2, R4 = 3, R5 = 4, R6 = 5, R7 = 6, R8 = 7, R9 = 8, R10 = 9, S1 = 10, M = 11, B = 12, D = 13;
        /// <summary>Salas normais (ids locais) para o sorteio dos contratos gerados.</summary>
        public static readonly int[] Normal = { R1, R2, R3, R4, R5, R6, R7, R8, R9, R10 };

        static Rect O(float cx, float cy, float hx, float hy) => new Rect(cx, cy, hx, hy);
        static Spawn S(EnemyType t, float x, float y) => new Spawn(t, x, y);
        static V2 P(float x, float y) => new V2(x, y);
        static Web C(float x, float y, float r) => new Web(x, y, r);
        static BuriedChest Bu(float x, float y, int coins) => new BuriedChest(x, y, coins);
        const EnemyType Bat = EnemyType.Bat, Thief = EnemyType.Thief, Guard = EnemyType.Guard, Archer = EnemyType.Archer, Ghost = EnemyType.Ghost,
                        Skeleton = EnemyType.Skeleton, Spider = EnemyType.Spider, Mummy = EnemyType.Mummy, Gargoyle = EnemyType.Gargoyle,
                        SpiderWidow = EnemyType.SpiderWidow, Necromancer = EnemyType.Necromancer;
        const RoomTrigger Half = RoomTrigger.Half, Down = RoomTrigger.CargoDown;

        // Icone de ameaca: ver BALANCE_REGIAO3.md (dano medio por visita do bot). Risco da carga: fantasma/Viuva = Ladrao, arqueiro/Necromante
        // = Arqueiro, gargula = Pancada; esqueleto, aranha e mumia miram o courier (sem icone). Regras de desenho (CoreTestsRegiao3.LevelDesign_*):
        // as da regiao 1 + teia a mais de 1,2 m da entrada e fora da faixa das portas; tocha so nas paredes laterais (x = 0,3 ou 8,7);
        // sala escura com 2+ tochas; ponto de salto do Necromante fora de obstaculo e a 4 m+ da entrada.
        public static readonly RoomDef[] Rooms =
        {
            new RoomDef(R1, "R3-1", "Corredor das Tochas", "no escuro a mira só pega o que está na luz (sua luz 3,5 m, tocha 2,5 m, lanterna 5,5 m): ande de tocha em tocha", 1,
                new[] { O(2.5f, 5.0f, 0.6f, 0.6f), O(6.5f, 5.0f, 0.6f, 0.6f), O(2.5f, 9.0f, 0.6f, 0.6f), O(6.5f, 9.0f, 0.6f, 0.6f) },
                new[] { S(Skeleton, 4.5f, 10.5f) }, P(1.5f, 2.5f),
                wave: new[] { S(Bat, 8.2f, 3.6f) }, trigger: Half)
            { Dark = true, Torches = new[] { P(0.3f, 4.0f), P(8.7f, 7.0f), P(0.3f, 10.5f) } },
            new RoomDef(R2, "R3-2", "Ossário", "esqueleto derrubado se remonta em 4 s, a menos que a carga esteja a 3 m dele ou que o dash espalhe os ossos: lute perto da carga", 1,
                new[] { O(2.0f, 7.0f, 0.6f, 0.3f), O(7.0f, 7.0f, 0.6f, 0.3f), O(4.5f, 9.5f, 0.6f, 0.3f) },
                new[] { S(Skeleton, 2.5f, 10.5f), S(Skeleton, 6.5f, 10.5f), S(Thief, 8.0f, 8.5f) }, P(2.0f, 2.5f), P(0.8f, 9.0f), 40,
                wave: new[] { S(Skeleton, 0.8f, 3.6f) }, trigger: Half)
            { Dark = true, Torches = new[] { P(0.3f, 5.5f), P(8.7f, 5.5f), P(0.3f, 11.0f) }, Blueprint = Gear.Hook },
            new RoomDef(R3, "R3-3", "Teia Grande", "teia prende 1 s quem passa carregando e freia 50% sem carga; carga pousada na teia leva +1 s para pegar; o dash rasga a teia", 1,
                new[] { O(1.0f, 8.5f, 0.4f, 0.4f), O(8.0f, 8.5f, 0.4f, 0.4f) },
                new[] { S(Spider, 4.5f, 11.5f), S(Spider, 1.5f, 9.6f) }, P(1.5f, 2.5f),
                wave: new[] { S(Spider, 8.2f, 3.6f) }, trigger: Half)
            {
                Dark = true, Torches = new[] { P(0.3f, 4.0f), P(8.7f, 4.0f), P(8.7f, 10.0f) },
                Webs = new[] { C(2.2f, 6.0f, 0.9f), C(6.8f, 6.0f, 0.9f), C(4.5f, 8.5f, 1.0f), C(2.2f, 11.0f, 0.9f), C(6.8f, 11.0f, 0.9f) },
            },
            new RoomDef(R4, "R3-4", "Capela Fantasma", "fantasma atravessa parede e possui a carga no chão: ela flutua até a parede e some em 2 s; pegue de volta ou acerte-o (só na luz); parede rachada à direita", 2,
                new[] { O(2.4f, 6.0f, 1.2f, 0.3f), O(6.6f, 6.0f, 1.2f, 0.3f), O(2.4f, 8.5f, 1.2f, 0.3f), O(6.6f, 8.5f, 1.2f, 0.3f), O(4.5f, 10.8f, 0.8f, 0.4f) },
                new[] { S(Ghost, 4.5f, 12.5f), S(Ghost, 1.0f, 10.0f), S(Archer, 7.5f, 11.8f) }, P(2.0f, 2.5f), P(8.2f, 10.0f), 40,
                wave: new[] { S(Ghost, 0.8f, 3.6f) }, trigger: Half, secret: S1, secretPos: P(Balance.RoomW, 7.2f))
            { Dark = true, Torches = new[] { P(0.3f, 4.5f), P(8.7f, 4.5f), P(0.3f, 11.5f), P(8.7f, 11.5f) }, Blueprint = Gear.Smoke },
            new RoomDef(R5, "R3-5", "Sarcófagos", "múmia é lenta (1 m/s) mas agarra: segura até levar 2 golpes ou até o dash (que larga a carga); não deixe ela encostar com a carga nas mãos", 1,
                new[] { O(2.0f, 7.5f, 0.4f, 0.9f), O(7.0f, 7.5f, 0.4f, 0.9f), O(4.5f, 10.5f, 0.9f, 0.4f) },
                new[] { S(Mummy, 2.0f, 10.5f), S(Mummy, 7.0f, 10.5f) }, P(2.0f, 2.5f), P(4.5f, 8.0f), 45,
                wave: new[] { S(Skeleton, 8.2f, 3.6f) }, trigger: Half)
            { Dark = true, Torches = new[] { P(0.3f, 5.0f), P(8.7f, 5.0f), P(8.7f, 11.0f) }, Buried = new[] { Bu(4.5f, 5.0f, 35) }, Blueprint = Gear.Hook },
            new RoomDef(R6, "R3-6", "Galeria das Gárgulas", "gárgula é pedra (invulnerável) até o courier passar a 3 m COM a carga; aí mergulha em linha (aviso 0,9 s) e fica 2,5 s no chão, vulnerável; sem a carga nas mãos ela dorme", 2,
                new[] { O(2.8f, 5.5f, 0.5f, 0.5f), O(6.2f, 5.5f, 0.5f, 0.5f) },
                new[] { S(Gargoyle, 1.0f, 9.0f), S(Gargoyle, 8.0f, 9.0f), S(Archer, 4.5f, 12.0f) }, P(2.0f, 2.5f), P(0.8f, 11.5f), 45,
                wave: new[] { S(Bat, 0.8f, 3.6f), S(Bat, 8.2f, 3.6f) }, trigger: Half)
            { Dark = true, Torches = new[] { P(0.3f, 6.0f), P(8.7f, 6.0f), P(0.3f, 12.0f), P(8.7f, 12.0f) }, Blueprint = Gear.Smoke },
            new RoomDef(R7, "R3-7", "Emboscada na Cripta", "emboscada: passar da metade ergue dois esqueletos e um fantasma por cima e a porta fecha; mate perto da carga ou eles voltam", 2,
                new[] { O(2.0f, 6.0f, 0.6f, 0.3f), O(7.0f, 6.0f, 0.6f, 0.3f), O(2.0f, 9.0f, 0.6f, 0.3f), O(7.0f, 9.0f, 0.6f, 0.3f) },
                new[] { S(Spider, 4.5f, 10.5f), S(Bat, 1.5f, 11.5f) }, P(2.0f, 2.5f),
                wave: new[] { S(Skeleton, 1.5f, 12.5f), S(Skeleton, 7.5f, 12.5f), S(Ghost, 4.5f, 12.8f) }, trigger: Half, waveLocks: true)
            { Dark = true, Torches = new[] { P(0.3f, 7.5f), P(8.7f, 7.5f), P(0.3f, 3.5f) } },
            new RoomDef(R8, "R3-8", "Guardião do Mausoléu", "luta obrigatória no escuro: pouse a carga na luz (o fantasma só aparece nela) e longe das teias", 2,
                new[] { O(4.5f, 6.0f, 1.4f, 0.4f) },
                new[] { S(Guard, 4.5f, 12.4f), S(Spider, 1.5f, 8.0f), S(Ghost, 8.0f, 7.0f) }, P(7.0f, 2.5f),
                wave: new[] { S(Ghost, 8.0f, 12.5f) }, trigger: Down)
            { Dark = true, Torches = new[] { P(0.3f, 11.0f), P(8.7f, 11.0f), P(0.3f, 4.0f) }, Webs = new[] { C(2.5f, 9.6f, 0.9f), C(6.5f, 9.6f, 0.9f) } },
            new RoomDef(R9, "R3-9", "Biblioteca Sombria", "três estantes em zigue-zague, só as passagens têm tocha; o fantasma corta por dentro das estantes; carga 3 s no chão chama reforço e fecha a porta; parede rachada à esquerda", 2,
                new[] { O(3.0f, 4.5f, 3.0f, 0.3f), O(6.0f, 8.0f, 3.0f, 0.3f), O(3.0f, 11.0f, 3.0f, 0.3f) },
                new[] { S(Ghost, 1.5f, 6.5f), S(Skeleton, 7.5f, 9.6f), S(Bat, 4.5f, 12.5f) }, P(7.0f, 2.5f), P(0.8f, 9.6f), 45,
                wave: new[] { S(Skeleton, 1.0f, 12.5f), S(Spider, 8.0f, 12.5f) }, trigger: Down, waveLocks: true, secret: S1, secretPos: P(0f, 2.6f))
            { Dark = true, Torches = new[] { P(8.7f, 4.5f), P(0.3f, 8.0f), P(8.7f, 11.0f) }, Buried = new[] { Bu(4.5f, 9.6f, 40) }, Blueprint = Gear.Hook },
            new RoomDef(R10, "R3-10", "Nave da Catedral", "múmia, arqueiro, gárgula e esqueleto; passar da metade chama reforço e fecha a porta", 3,
                new[] { O(2.0f, 6.0f, 0.6f, 0.6f), O(7.0f, 6.0f, 0.6f, 0.6f), O(4.5f, 9.0f, 0.6f, 0.6f) },
                new[] { S(Mummy, 2.5f, 10.5f), S(Archer, 7.5f, 11.5f), S(Gargoyle, 4.5f, 11.8f), S(Skeleton, 6.2f, 8.4f) }, P(2.0f, 2.5f), P(8.2f, 8.0f), 50,
                wave: new[] { S(Bat, 1.0f, 12.5f), S(Skeleton, 8.0f, 12.5f), S(Ghost, 4.5f, 12.8f) }, trigger: Half, waveLocks: true)
            { Dark = true, Torches = new[] { P(0.3f, 6.0f), P(8.7f, 6.0f), P(0.3f, 11.0f), P(8.7f, 11.0f) } },
            new RoomDef(S1, "S3-1", "Cripta do Tesouro", "sala secreta: baú grande guardado por um esqueleto", 1,
                new[] { O(2.0f, 6.0f, 0.6f, 0.3f), O(7.0f, 6.0f, 0.6f, 0.3f) },
                new[] { S(Skeleton, 4.5f, 11.0f) }, P(2.0f, 2.5f), P(4.5f, 9.0f), 100)
            { Dark = true, Torches = new[] { P(0.3f, 9.0f), P(8.7f, 9.0f) }, Blueprint = Gear.Smoke },
            new RoomDef(M, "M3", "Covil da Viúva", "mini-chefe: teias pela sala, puxa a carga com o fio, despenca do teto em cima da carga", 3,
                new[] { O(2.0f, 10.0f, 0.5f, 0.5f), O(7.0f, 10.0f, 0.5f, 0.5f) },
                new[] { S(SpiderWidow, 4.5f, 11.2f), S(Spider, 1.5f, 12.0f) }, P(2.0f, 2.5f))
            {
                Dark = true, Torches = new[] { P(0.3f, 4.0f), P(8.7f, 4.0f), P(0.3f, 10.0f), P(8.7f, 10.0f) },
                Webs = new[] { C(2.0f, 7.0f, 0.9f), C(7.0f, 7.0f, 0.9f), C(4.5f, 5.0f, 0.8f) },
            },
            new RoomDef(B, "B3", "Altar do Necromante", "chefe: ergue esqueletos das lápides em ondas, raio telegrafado, salta de lápide em lápide quando você chega perto", 3,
                new[] { O(1.8f, 5.5f, 0.5f, 0.3f), O(7.2f, 5.5f, 0.5f, 0.3f), O(1.8f, 8.5f, 0.5f, 0.3f), O(7.2f, 8.5f, 0.5f, 0.3f), O(1.8f, 11.5f, 0.5f, 0.3f), O(7.2f, 11.5f, 0.5f, 0.3f), O(4.5f, 9.8f, 0.8f, 0.4f) },
                new[] { S(Necromancer, 4.5f, 11.6f) }, P(4.5f, 3.0f))
            {
                Dark = true, Torches = new[] { P(0.3f, 4.0f), P(8.7f, 4.0f), P(0.3f, 10.0f), P(8.7f, 10.0f) },
                Blinks = new[] { P(3.0f, 5.5f), P(6.0f, 5.5f), P(3.0f, 8.5f), P(6.0f, 8.5f), P(3.0f, 11.6f), P(6.0f, 11.6f) },
            },
            new RoomDef(D, "D3", "Entrega", "o cliente espera no topo", 0,
                new[] { O(2.0f, 7.0f, 0.5f, 0.5f), O(7.0f, 7.0f, 0.5f, 0.5f) },
                new Spawn[0], P(2.0f, 2.5f)),
        };

        static int[] L(params int[] rooms) => rooms;
        static ContractDef K(string code, CargoType cargo, int pay, string note, float par, float egg, float limit, int tier, bool waves, params int[][] layers)
            => Content.Make(code, cargo, pay, note, par, egg, limit, tier, waves, D, layers);
        const CargoType Box = CargoType.Box, Relic = CargoType.Relic, Egg = CargoType.Egg, Msg = CargoType.Message, Barrel = CargoType.Barrel,
                        Heart = CargoType.ArcaneHeart, Cage = CargoType.GriffinCage, Lantern = CargoType.Lantern, Mirror = CargoType.Mirror, Balloon = CargoType.Balloon;

        // M3-1..M3-3 ensinam (sem reforco); nivel = 12 + indice local; M3-8 = mini-chefe; M3-16 = chefe. Tempos pela mediana do bot na
        // campanha da regiao 3 (BALANCE_REGIAO3.md): ParTime (3a estrela) = 1,6x; ovo = 2x; mensagem = 2,5x.
        public static readonly ContractDef[] Contracts =
        {
            K("M3-1",  Lantern, 255, "região 3: escuro; lanterna ilumina 5,5 m", 35f, 0f, 0f, 12, false, L(R1), L(R2, R3)),
            K("M3-2",  Box,     258, "esqueletos, teias e fantasmas sem lanterna", 60f, 0f, 0f, 13, false, L(R1), L(R4, R5), L(R2, R3)),
            K("M3-3",  Mirror,  261, "espelho encantado: reflete flecha e teia", 45f, 0f, 0f, 14, false, L(R3), L(R6, R4), L(R1, R8)),
            K("M3-4",  Egg,     265, "ovo no escuro", 145f, 180f, 0f, 15, true, L(R2), L(R5, R9), L(R3, R6), L(R7, R4), L(R9, R8), L(R10, R5)),
            K("M3-5",  Balloon, 269, "balão de gás: leve, mas escapa do chão em 4 s", 145f, 0f, 0f, 16, true, L(R5), L(R9, R4), L(R8, R2), L(R3, R6), L(R10, R9), L(R7, R2)),
            K("M3-6",  Relic,   273, "relíquia entre múmias e gárgulas", 165f, 0f, 0f, 17, true, L(R4), L(R2, R6), L(R5, R1), L(R7, R3), L(R8, R9), L(R4, R2)),
            K("M3-7",  Lantern, 277, "lanterna em 6 camadas", 175f, 0f, 0f, 18, true, L(R8), L(R1, R7), L(R3, R5, R4), L(R6, R2), L(R10, R9), L(R5, R2)),
            K("M3-8",  Box,     325, "mini-chefe: Viúva Aranha", 195f, 0f, 0f, 19, true, L(R2), L(R4, R6), L(R9, R1), L(R8, R5), L(R3, R7), L(M)),
            K("M3-9",  Mirror,  285, "espelho em 6 camadas", 155f, 0f, 0f, 20, true, L(R2), L(R6, R3), L(R10, R2), L(R4, R9), L(R5, R8), L(R7, R6)),
            K("M3-10", Msg,     289, "mensagem no escuro", 75f, 0f, 115f, 21, true, L(R5), L(R8, R3), L(R10, R2), L(R4, R9), L(R1, R6), L(R7, R8)),
            K("M3-11", Balloon, 293, "balão em 6 camadas", 165f, 0f, 0f, 22, true, L(R7), L(R9, R3), L(R6, R10), L(R5, R2), L(R8, R4), L(R1, R7)),
            K("M3-12", Heart,   297, "coração no escuro: a horda vem para a luz", 170f, 0f, 0f, 23, true, L(R4), L(R6, R1), L(R2, R10), L(R9, R8), L(R3, R5), L(R7, R4)),
            K("M3-13", Barrel,  301, "barril entre esqueletos", 180f, 0f, 0f, 24, true, L(R1), L(R5, R7), L(R8, R10, R3), L(R2, R6), L(R9, R4), L(R5, R1)),
            K("M3-14", Cage,    305, "gaiola na cripta: o grito acorda as gárgulas", 150f, 0f, 0f, 25, true, L(R6), L(R3, R10), L(R7, R2), L(R4, R9), L(R8, R1), L(R5, R3)),
            K("M3-15", Mirror,  309, "espelho em 6 camadas", 170f, 0f, 0f, 26, true, L(R10), L(R2, R8), L(R6, R5), L(R9, R1, R4), L(R3, R7), L(R10, R6)),
            K("M3-16", Lantern, 410, "chefe: Necromante", 265f, 0f, 0f, 27, true, L(R1), L(R4, R8), L(R9, R6, R3), L(R5, R2), L(B)),
        };
    }
}
