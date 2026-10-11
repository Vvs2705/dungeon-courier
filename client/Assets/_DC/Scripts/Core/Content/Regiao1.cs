namespace DC.Core
{
    /// <summary>Regiao 1 "Poroes da Guilda" (CONTRATO_V1, feita): 21 salas (R1-R16, S1, S2, M1, B1, D1) e os contratos C1-C24. Ids de sala
    /// e indices de contrato sao estaveis (vao para o save) e sao GLOBAIS (a regiao 1 e a primeira do registro, base 0). Movida de
    /// Content sem mudar nenhum numero; Content.R1.., Content.Rooms e Content.Contracts continuam valendo (atalhos).</summary>
    public static class Regiao1
    {
        public const int Id = 1;
        public const string Name = "Porões da Guilda", Theme = "poroes";
        public const int R1 = 0, R2 = 1, R3 = 2, R4 = 3, R5 = 4, R6 = 5, B1 = 6, D1 = 7;
        public const int R7 = 8, R8 = 9, R9 = 10, R10 = 11, R11 = 12, R12 = 13, R13 = 14, R14 = 15, R15 = 16, R16 = 17, S1 = 18, S2 = 19, M1 = 20;
        /// <summary>Salas normais da regiao (sorteio dos contratos gerados).</summary>
        public static readonly int[] Normal = { R1, R2, R3, R4, R5, R6, R7, R8, R9, R10, R11, R12, R13, R14, R15, R16 };

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
    }
}
