namespace DC.Core
{
    /// <summary>Regiao 2 "Minas Alagadas" (CONTRATO_LANCAMENTO §2-§4; desenho em docs/CONTEUDO_REGIOES.md). ESQUELETO da Leva A: a raia R2
    /// preenche aqui as salas e os 16 contratos (M2-1..M2-16) e os comportamentos novos em Run.Regiao2.cs. Sem conteudo a regiao fica
    /// fechada (RegionDef.HasContent falso) e nada quebra.
    /// Regras do arquivo:
    ///  - ids de sala LOCAIS 0..n-1, na ordem do array Rooms (o registro em Content soma a base global; o Secret tambem e local);
    ///  - Code da sala: R2-1..R2-10 normais, S2-1 secreta, M2 mini-chefe, B2 chefe, D2 entrega (a 1a letra e o tipo);
    ///  - contratos com Content.Make(code, carga, pagamento, nota, par, ovo, limite, tier, reforcos, D, camadas...) em ids locais;
    ///  - so crescem no fim (o save guarda ids globais): ver o aviso em Content;
    ///  - inimigos e cargas da regiao ja estao nos enums (EnemyDef/CargoDef provisorios: ajuste os numeros e desligue Provisional).
    /// Cargas que estreiam aqui: ArcaneHeart, IceCrystal, GriffinCage. Inimigos: GoblinMiner, Mole, BogFrog, BombBeetle, Minecart;
    /// mini-chefe MoleQueen; chefe StoneGolem.</summary>
    public static class Regiao2
    {
        public const int Id = 2;
        public const string Name = "Minas Alagadas", Theme = "minas";
        /// <summary>Estrelas para destravar (alem do Ogro vencido = ultimo contrato da regiao 1).</summary>
        public const int Stars = 30;
        /// <summary>Nivel base dos contratos gerados da regiao (dia/codigo/raro/evento): vida dos inimigos x Balance.HpMul. PROVISORIO.</summary>
        /// <summary>Contrato gerado da regiao (CONTEUDO_REGIOES §1.2, PROVISORIO): pagamento = GenPay + 22 x camadas; tempos por sala (s) ate a
        /// entrega: 3a estrela, ovo, gelo (MeltTime) e mensagem.</summary>
        public const int GenPay = 110; public const float GenPar = 20f, GenEgg = 25f, GenMelt = 27f, GenLimit = 13f;
        public const int TierBase = 14;

        // ids locais (exemplo): public const int R1 = 0, R2 = 1, ..., S1 = 10, M = 11, B = 12, D = 13;
        public static readonly RoomDef[] Rooms = { };
        /// <summary>Salas normais (ids locais) para o sorteio dos contratos gerados.</summary>
        public static readonly int[] Normal = { };
        public static readonly ContractDef[] Contracts = { };
    }
}
