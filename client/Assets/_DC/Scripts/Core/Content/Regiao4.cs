namespace DC.Core
{
    /// <summary>Regiao 4 "Forja do Dragao" (CONTRATO_LANCAMENTO §2-§4). ESQUELETO da Leva A: a raia R4 preenche as salas e os 16 contratos
    /// (M4-1..M4-16) aqui e os comportamentos em Run.Regiao4.cs, com as mesmas regras de Regiao2 (ids locais; codigos R4-n, S4-1, M4, B4,
    /// D4). O chefe desta regiao (ultimo contrato da campanha) destrava o desafio sem fim.
    /// Cargas que estreiam aqui: Flytrap, TreasureMap. Inimigos: Salamander, Imp, KoboldBomber, MagmaElemental; mini-chefe KoboldForeman;
    /// chefe ElderDragon.</summary>
    public static class Regiao4
    {
        public const int Id = 4;
        public const string Name = "Forja do Dragão", Theme = "forja";
        /// <summary>Estrelas para destravar (alem do chefe da regiao 3).</summary>
        public const int Stars = 95;
        /// <summary>Nivel base dos gerados da regiao. PROVISORIO.</summary>
        public const int TierBase = 22;

        public static readonly RoomDef[] Rooms = { };
        public static readonly int[] Normal = { };
        public static readonly ContractDef[] Contracts = { };
    }
}
