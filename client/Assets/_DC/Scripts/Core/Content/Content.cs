using System.Collections.Generic;

namespace DC.Core
{
    /// <summary>Regiao (CONTRATO_LANCAMENTO §2). Ids globais (salas em Content.Rooms, contratos em Content.Contracts). Stars = estrelas para
    /// destravar (alem do chefe da regiao anterior, que e o ultimo contrato dela: a campanha e linear). Theme = id do tema para a View
    /// (piso, parede, paleta, musica). Mini/Boss/Delivery = ids das salas M/B/D (-1 = ainda nao ha). MiniAt/BossAt = indice global do
    /// contrato com o M/B (-1). CargoCount = cargas que o sorteio dos gerados desta regiao usa (as que estrearam ate ela).</summary>
    public sealed class RegionDef
    {
        public readonly int Id, Stars, First, Mini, Boss, Delivery, MiniAt, BossAt, CargoCount, TierBase;
        public readonly string Name, Theme;
        public readonly int[] Rooms, Normal, Secrets;
        public readonly ContractDef[] Contracts;
        /// <summary>Tem salas normais e contratos (as regioes 2-4 nascem vazias na Leva A).</summary>
        public bool HasContent => Contracts.Length > 0 && Normal.Length > 0 && Delivery >= 0;
        public static RegionDef[] All => Content.Regions;
        /// <summary>Regiao pelo id 1-4 (null fora).</summary>
        public static RegionDef Of(int id) => id >= 1 && id <= Content.Regions.Length ? Content.Regions[id - 1] : null;

        internal RegionDef(int id, string name, string theme, int stars, int first, int[] rooms, int[] normal, ContractDef[] contracts, int tierBase)
        {
            Id = id; Name = name; Theme = theme; Stars = stars; First = first; Rooms = rooms; Normal = normal; Contracts = contracts; TierBase = tierBase;
            Mini = Boss = Delivery = MiniAt = BossAt = -1;
            var secrets = new List<int>();
            foreach (int r in rooms)
            {
                RoomDef d = Content.RoomsBuilding[r];
                if (d.IsSecret) secrets.Add(r);
                else if (d.Mini) Mini = r;
                else if (d.Boss) Boss = r;
                else if (d.Delivery) Delivery = r;
            }
            Secrets = secrets.ToArray();
            foreach (ContractDef c in contracts)
                foreach (int[] layer in c.Layers)
                {
                    if (MiniAt < 0 && System.Array.IndexOf(layer, Mini) >= 0 && Mini >= 0) MiniAt = c.Index;
                    if (BossAt < 0 && System.Array.IndexOf(layer, Boss) >= 0 && Boss >= 0) BossAt = c.Index;
                }
            foreach (CargoDef c in CargoDef.All) if (c.Region <= id) CargoCount++;
        }
    }

    /// <summary>Registro do conteudo das 4 regioes (Content/RegiaoN.cs). Content.Rooms e Content.Contracts sao a concatenacao na ordem das
    /// regioes; a regiao 1 e a primeira, entao os ids/indices dela nao mudam (R1 = 0 ... M1 = 20; C1 = 0 ... C24 = 23). As regioes 2-4 usam
    /// ids LOCAIS no proprio arquivo e o registro soma a base: so crescem no fim da propria regiao. ATENCAO: o save guarda ids globais,
    /// entao mudar o NUMERO de salas ou de contratos de uma regiao desloca as regioes seguintes (combinado: 16 contratos por regiao 2-4;
    /// salas novas depois do lancamento, so na ultima regiao ou com migracao).</summary>
    public static class Content
    {
        // ---- atalhos da regiao 1 (a View e os testes usam; ids estaveis)
        public const string RegionName = Regiao1.Name;
        public const int R1 = Regiao1.R1, R2 = Regiao1.R2, R3 = Regiao1.R3, R4 = Regiao1.R4, R5 = Regiao1.R5, R6 = Regiao1.R6, B1 = Regiao1.B1, D1 = Regiao1.D1;
        public const int R7 = Regiao1.R7, R8 = Regiao1.R8, R9 = Regiao1.R9, R10 = Regiao1.R10, R11 = Regiao1.R11, R12 = Regiao1.R12, R13 = Regiao1.R13,
                         R14 = Regiao1.R14, R15 = Regiao1.R15, R16 = Regiao1.R16, S1 = Regiao1.S1, S2 = Regiao1.S2, M1 = Regiao1.M1;
        /// <summary>Salas normais da regiao 1 (sorteio dos contratos gerados da regiao 1). Das outras: RegionDef.Of(n).Normal.</summary>
        public static readonly int[] NormalRooms = Regiao1.Normal;

        /// <summary>Todas as salas (indice = RoomDef.Id) e todos os contratos da campanha (indice = ContractDef.Index), regiao 1 primeiro.</summary>
        public static readonly RoomDef[] Rooms;
        public static readonly ContractDef[] Contracts;
        public static readonly RegionDef[] Regions;
        internal static List<RoomDef> RoomsBuilding;

        static Content()
        {
            RoomsBuilding = new List<RoomDef>();
            var cons = new List<ContractDef>();
            Regions = new[]
            {
                Add(cons, Regiao1.Id, Regiao1.Name, Regiao1.Theme, 0, Regiao1.Rooms, Regiao1.Normal, Regiao1.Contracts, Balance.GenTierBase),
                Add(cons, Regiao2.Id, Regiao2.Name, Regiao2.Theme, Regiao2.Stars, Regiao2.Rooms, Regiao2.Normal, Regiao2.Contracts, Regiao2.TierBase),
                Add(cons, Regiao3.Id, Regiao3.Name, Regiao3.Theme, Regiao3.Stars, Regiao3.Rooms, Regiao3.Normal, Regiao3.Contracts, Regiao3.TierBase),
                Add(cons, Regiao4.Id, Regiao4.Name, Regiao4.Theme, Regiao4.Stars, Regiao4.Rooms, Regiao4.Normal, Regiao4.Contracts, Regiao4.TierBase),
            };
            Rooms = RoomsBuilding.ToArray();
            Contracts = cons.ToArray();
            RoomsBuilding = null;
        }

        /// <summary>Soma a base global nas salas (Id, Secret) e nos contratos (Index, salas das camadas) da regiao e registra.</summary>
        static RegionDef Add(List<ContractDef> cons, int id, string name, string theme, int stars, RoomDef[] rooms, int[] normal, ContractDef[] contracts, int tierBase)
        {
            int b = RoomsBuilding.Count, first = cons.Count;
            var ids = new int[rooms.Length];
            for (int i = 0; i < rooms.Length; i++)
            {
                RoomDef r = rooms[i];
                if (r.Id != i) throw new System.InvalidOperationException("Regiao " + id + ": sala " + r.Code + " com id local " + r.Id + " na posicao " + i);
                r.Id = b + i; r.Region = id;
                if (r.Secret >= 0) r.Secret += b;
                ids[i] = r.Id;
                RoomsBuilding.Add(r);
            }
            var norm = new int[normal.Length];
            for (int i = 0; i < normal.Length; i++) norm[i] = normal[i] + b;
            for (int i = 0; i < contracts.Length; i++)
            {
                ContractDef c = contracts[i];
                c.Index = first + i; c.Region = id;
                if (b > 0) foreach (int[] layer in c.Layers) for (int k = 0; k < layer.Length; k++) layer[k] += b;
                cons.Add(c);
            }
            return new RegionDef(id, name, theme, stars, first, ids, norm, contracts, tierBase);
        }

        /// <summary>Contrato de campanha de uma regiao 2-4, com ids LOCAIS (o registro soma a base). code = "M2-1".. (CONTRATO_LANCAMENTO
        /// §2); delivery = id local da sala D; Tier = nivel (vida dos inimigos x Balance.HpMul(Tier)). Tutoriais sem reforco: waves = false.</summary>
        public static ContractDef Make(string code, CargoType cargo, int pay, string note, float par, float egg, float limit, int tier, bool waves, int delivery, params int[][] layers)
            => new ContractDef(-1, code, cargo, pay, egg, note, delivery, layers) { ParTime = par, TimeLimit = limit, Tier = tier, Waves = waves, Name = note };

        // ------------------------------------------------------------------ contratos gerados

        /// <summary>Os 2 bits altos do payload (25 bits) dizem a regiao: 0 = regiao 1 ... 3 = regiao 4. Codigos da beta com esses bits
        /// diferentes de 0 passam a apontar para outra regiao (a regiao 1 so com bits 0).</summary>
        public const int RegionShift = 23;
        const int Low = (1 << RegionShift) - 1;
        public static int RegionOf(int payload) => ((payload & ContractCode.MaxPayload) >> RegionShift) + 1;
        public static int WithRegion(int payload, int region) => (payload & Low) | ((region - 1) & 3) << RegionShift;

        /// <summary>Contrato gerado (dia, codigo, raro, evento) a partir do payload: carga, 4-6 camadas de 2-3 salas normais da regiao,
        /// pagamento e tempos pelas taxas medidas (Balance.Gen*). Regiao = a dos 2 bits altos (ou `region` > 0). null = regiao sem conteudo.
        /// `cargo` forca a carga (Fragile Week, Midnight) sem mudar as salas. Deterministico: mesmo payload = mesmo contrato em qualquer
        /// aparelho; na regiao 1 e identico ao da v1.0-beta.</summary>
        public static ContractDef Generate(int payload, ContractKind kind, int date = 0, int region = 0, CargoType? cargo = null)
        {
            payload &= ContractCode.MaxPayload;
            RegionDef reg = RegionDef.Of(region > 0 ? region : RegionOf(payload));
            if (reg == null || !reg.HasContent) return null;
            var rng = new Rng(Rng.Hash(payload, 0x6E4));
            var c = (CargoType)rng.Int(reg.CargoCount);
            if (cargo.HasValue) c = cargo.Value;
            int layers = 4 + rng.Int(3);
            var pool = new List<int>();
            var ls = new int[layers][];
            for (int l = 0; l < layers; l++)
            {
                int n = l == 0 ? 1 : 2 + rng.Int(2);
                ls[l] = new int[n];
                for (int i = 0; i < n; i++)
                {
                    if (pool.Count == 0) pool.AddRange(reg.Normal);
                    int k = rng.Int(pool.Count);
                    ls[l][i] = pool[k];
                    pool.RemoveAt(k);
                }
            }
            string code = ContractCode.Encode(payload);
            return Build(kind, code, c, layers, ls, payload, date, reg);
        }

        static ContractDef Build(ContractKind kind, string code, CargoType c, int layers, int[][] ls, int payload, int date, RegionDef reg)
        {
            int rooms = ls.Length + 1, pay = 40 + 22 * layers;
            string label = kind == ContractKind.Daily ? "DIA" : kind == ContractKind.Rare ? "RARO" : kind == ContractKind.Event ? "EVENTO" : kind == ContractKind.Endless ? "SEMFIM" : code;
            string note = (kind == ContractKind.Daily ? "contrato do dia: " : kind == ContractKind.Rare ? "contrato raro: " : kind == ContractKind.Event ? "evento: "
                          : kind == ContractKind.Endless ? "desafio sem fim: " : "contrato " + code + ": ") + CargoDef.All[(int)c].Name;
            return new ContractDef(-1, label, c, pay, c == CargoType.Egg ? Balance.GenEggPerRoom * rooms : 0f, note, reg.Delivery, ls)
            {
                Kind = kind, ShareCode = kind == ContractKind.Event ? null : code, Payload = payload, Date = date, ParTime = Balance.GenParPerRoom * rooms,
                Tier = reg.TierBase + 3 * (layers - 4), TimeLimit = c == CargoType.Message ? Balance.GenLimitPerRoom * rooms : 0f, Region = reg.Id, Name = note,
            };
        }

        /// <summary>Contrato do quadro de eventos (CONTRATO_LANCAMENTO §6.1): gerado do payload com a regra do template. Fragile Week = carga
        /// fragil; Midnight = lanterna (salas escuras); Boss Courier Run = as salas de chefe ja vencidas em sequencia (`bosses`; vazio = gerado
        /// comum); No-Damage e Treasure Detour mudam o Run (vida 1; baus x2 e paredes rachadas abertas).</summary>
        public static ContractDef EventContract(LiveEvent ev, int payload, int date, int[] bosses)
        {
            payload &= ContractCode.MaxPayload;
            RegionDef reg = RegionDef.Of(RegionOf(payload));
            if (reg == null || !reg.HasContent) return null;
            CargoType? cargo = null;
            var rng = new Rng(Rng.Hash(payload, 0xE7E));
            if (ev == LiveEvent.FragileWeek)
            {
                var fr = new List<CargoType>();
                foreach (CargoDef d in CargoDef.All) if (d.Fragile && d.Region <= reg.Id) fr.Add(d.Type);
                cargo = fr[rng.Int(fr.Count)];
            }
            else if (ev == LiveEvent.Midnight) cargo = CargoType.Lantern;
            ContractDef def;
            if (ev == LiveEvent.BossRun && bosses != null && bosses.Length > 0)
            {
                var ls = new int[bosses.Length][];
                for (int i = 0; i < bosses.Length; i++) ls[i] = new[] { bosses[i] };
                def = Build(ContractKind.Event, ContractCode.Encode(payload), CargoType.Box, 4 + bosses.Length, ls, payload, date, reg);
                def.ParTime = Balance.GenParPerRoom * 4f * (bosses.Length + 1);
            }
            else def = Generate(payload, ContractKind.Event, date, 0, cargo);
            def.Event = ev;
            return def;
        }

        /// <summary>Desafio sem fim (§6.8): Balance.EndlessLayers camadas de 2 salas misturando as salas normais das `regions`, a dificuldade
        /// sobe por camada (Run.Tier). Mesma semana = mesmo desafio para todos (payload pela semana); ShareCode compara.</summary>
        public static ContractDef Endless(int week, int[] regions)
        {
            int payload = (int)(Rng.Hash(week, 0xE4D) & ContractCode.MaxPayload);
            var rng = new Rng(Rng.Hash(payload, 0x6E4));
            var pool0 = new List<int>(); RegionDef top = Regions[0];
            foreach (int r in regions) { RegionDef d = RegionDef.Of(r); if (d == null || !d.HasContent) continue; pool0.AddRange(d.Normal); if (d.Id > top.Id) top = d; }
            var c = (CargoType)rng.Int(top.CargoCount);
            var pool = new List<int>();
            var ls = new int[Balance.EndlessLayers][];
            for (int l = 0; l < ls.Length; l++)
            {
                int n = l == 0 ? 1 : 2;
                ls[l] = new int[n];
                for (int i = 0; i < n; i++)
                {
                    if (pool.Count == 0) pool.AddRange(pool0);
                    int k = rng.Int(pool.Count);
                    ls[l][i] = pool[k];
                    pool.RemoveAt(k);
                }
            }
            ContractDef def = Build(ContractKind.Endless, ContractCode.Encode(payload), c, 4, ls, payload, 0, top);
            def.Tier = Balance.EndlessTierBase; def.Date = week;
            return def;
        }
    }
}
