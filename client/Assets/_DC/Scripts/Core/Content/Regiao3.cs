namespace DC.Core
{
    /// <summary>Regiao 3 "Cripta Assombrada" (CONTRATO_LANCAMENTO §2-§4). ESQUELETO da Leva A: a raia R3 preenche as salas e os 16
    /// contratos (M3-1..M3-16) aqui e os comportamentos em Run.Regiao3.cs, com as mesmas regras de Regiao2 (ids locais; codigos R3-n, S3-1,
    /// M3, B3, D3). Escuro: Run.Dark (sala escura) e Run.LightMul (Olhos de gato) ja existem para a View e para a lanterna.
    /// Cargas que estreiam aqui: Lantern, Mirror, Balloon. Inimigos: Ghost, Skeleton, Spider, Mummy, Gargoyle; mini-chefe SpiderWidow;
    /// chefe Necromancer.</summary>
    public static class Regiao3
    {
        public const int Id = 3;
        public const string Name = "Cripta Assombrada", Theme = "cripta";
        /// <summary>Estrelas para destravar (alem do chefe da regiao 2).</summary>
        public const int Stars = 60;
        /// <summary>Nivel base dos gerados da regiao. PROVISORIO.</summary>
        /// <summary>Contrato gerado da regiao (CONTEUDO_REGIOES §1.2, PROVISORIO): pagamento = GenPay + 22 x camadas; tempos por sala (s) ate a
        /// entrega: 3a estrela, ovo, gelo (MeltTime) e mensagem.</summary>
        public const int GenPay = 165; public const float GenPar = 22f, GenEgg = 28f, GenMelt = 30f, GenLimit = 14f;
        public const int TierBase = 18;

        public static readonly RoomDef[] Rooms = { };
        public static readonly int[] Normal = { };
        public static readonly ContractDef[] Contracts = { };
    }
}
