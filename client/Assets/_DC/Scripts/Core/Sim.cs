using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// API do nucleo para a View (DC.Core, C# puro, deterministico, passo fixo de 1/30 s). Tudo em metros/segundos, origem no canto
// inferior esquerdo da sala (9 x 14).
//
//   Abrir:      var sim = Sim.Load(texto) (ou new Sim(semente)). sim.SaveRefused => save de versao futura: avise e NUNCA grave
//               (sim.Save() devolve null e sim.Dirty fica falso).
//   Meta:       sim.Coins, sim.Reputation (contratos concluidos em sequencia), sim.Unlocked(i), sim.GuildOpen, sim.Levels[(int)Upgrade.X],
//               sim.UpgradeCost(u) (-1 = no maximo), sim.CanBuy(u), sim.Buy(u), sim.MaxHp, sim.DashCooldown, sim.CoinMul,
//               sim.Pending (LostCargo a resgatar, ou null).
//   Conteudo:   Sim.Contracts[i] (ContractDef: Code, Cargo, Pay, EggTarget, Layers, Note), Content.Rooms[id] (RoomDef: Obstacles,
//               Spawns, ChestPos/ChestCoins, Threat, Delivery), CargoDef.All, EnemyDef.All, Balance.*, Hints.Text(hint).
//   Jogar:      sim.StartContract(i, resgate) -> Run; a cada quadro sim.Tick(dt, move, dash) (move = V2.Zero com o dedo solto; dash = true
//               no quadro do toque rapido). Ler sim.Run: Room, Layer, Doors, Chest, Courier, Cargos, Pet, Enemies, Shots, Time,
//               EggLeft, Hint, Outcome. sim.Events = eventos desde o ultimo Tick (limpa no inicio de cada Tick; Start/Buy/Abandon
//               acrescentam). sim.Abandon() desiste.
//   Fim:        sim.Run.Outcome != Running => sim.LastResult (ContractResult) ja aplicado: moedas, reputacao, carga pendente.
//   Gravar:     if (sim.Dirty && !sim.SaveRefused) gravar(sim.Save()). Dirty liga a cada dano, rachadura, sala, bau, fim e compra.
//               Reabrir no meio do contrato retoma na ENTRADA da sala atual com a vida e as rachaduras gravadas.
//   Diario:     SimEvent.DiaryName(e.Kind) da o nome do GDD §14 (null = evento so de SFX); A/B de cada tipo no comentario de Ev.
//   Fixos:      Balance.Entrance, Balance.Client (D1), Balance.RoomW/RoomH, Balance.DoorHalf, Balance.Step (1/30 s).
//   Bot:        -autoplay: Bot.Play(sim, Math.Min(sim.Reputation, Sim.Contracts.Length - 1), sim.Pending != null, new Bot(semente)) (null =
//               nao comecou). -bot com render: acumule o dt e
//               chame bot.Act(sim) a cada Balance.Step (Act ja chama sim.Tick).

namespace DC.Core
{
    /// <summary>Carga perdida (morte ou ladrao que escapou), pendente para resgate no proximo contrato. Value = pagamento base dela
    /// (Pay x multiplicador da carga): o bonus do resgate e Balance.RescueBonus disso.</summary>
    public sealed class LostCargo
    {
        public int Id; public CargoType Type; public int Cracks, Room, Value, Contract;
    }

    /// <summary>Resultado de um contrato, com o pagamento detalhado (Total = Payment + RescueBonus + ChestCoins).</summary>
    public sealed class ContractResult
    {
        public int Contract; public Outcome Outcome; public float Time;
        public int Cracks; public bool Hatched;
        public int Base; public float CargoMul, IntegrityMul, BagMul;
        public int Payment, ChestCoins, RescueBonus, Total;
        public bool RescueOffered, RescueAccepted, RescueDelivered;
        public int DeathRoom = -1, RoomsNormal, RoomsFought;
        public bool Delivered => Outcome == Outcome.Delivered;
    }

    public sealed class Sim
    {
        public const int SaveVersion = 1;
        public static ContractDef[] Contracts => Content.Contracts;

        public int Seed, RunsStarted, NextCargoId = 1;
        public int Coins, Reputation;
        public readonly int[] Levels = new int[3];
        public LostCargo Pending;
        /// <summary>Ids de carga que ja pagaram bonus de resgate (o bonus sai uma vez por id).</summary>
        public readonly HashSet<int> RescuedIds = new HashSet<int>();
        public bool SaveRefused, Dirty;
        public Run Run; public ContractResult LastResult;
        public readonly List<SimEvent> Events = new List<SimEvent>();

        public Sim(int seed = 1) { Seed = seed; }

        public bool InContract => Run != null && Run.Running;
        public bool Unlocked(int i) => i >= 0 && i < Contracts.Length && i <= Reputation;
        /// <summary>A guilda abre depois do 1º contrato (o 1º upgrade sai antes do C2).</summary>
        public bool GuildOpen => Reputation >= 1;
        public int MaxHp => Balance.BaseHp + Balance.LifePerLevel * Levels[(int)Upgrade.Life];
        public float DashCooldown => Balance.DashCooldown * (1f - Balance.DashCdPerLevel * Levels[(int)Upgrade.Dash]);
        public float CoinMul => 1f + Balance.BagPerLevel * Levels[(int)Upgrade.Bag];

        public int UpgradeCost(Upgrade u) => Levels[(int)u] >= Balance.UpgradeMax ? -1 : Balance.UpgradeCost(Levels[(int)u]);
        public bool CanBuy(Upgrade u) { int c = UpgradeCost(u); return GuildOpen && !InContract && !SaveRefused && c >= 0 && Coins >= c; }

        public bool Buy(Upgrade u)
        {
            if (!CanBuy(u)) return false;
            int cost = UpgradeCost(u);
            Coins -= cost;
            Levels[(int)u]++;
            Emit(Ev.GuildUpgrade, (int)u, Levels[(int)u], V2.Zero);
            Emit(Ev.CurrencySink, cost, (int)u, V2.Zero);
            Dirty = true;
            return true;
        }

        internal void Emit(Ev k, int a, int b, V2 pos) => Events.Add(new SimEvent(k, a, b, pos));
        static int Round(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);

        /// <summary>Credita moedas com a Bolsa, arredondando uma vez so no fim; devolve o valor creditado.</summary>
        internal int Credit(double coins)
        {
            int v = Round(coins * Balance.Pct(CoinMul) / 100.0);
            Coins += v;
            return v;
        }

        // ------------------------------------------------------------------ contrato

        /// <summary>Comeca o contrato i (precisa estar desbloqueado e nenhum outro em curso). rescue = aceitar a carga perdida pendente:
        /// ela aparece no chao da 1ª sala. A oferta expira aqui, aceita ou nao.</summary>
        public Run StartContract(int index, bool rescue)
        {
            if (SaveRefused || InContract || !Unlocked(index)) return null;
            ContractDef def = Contracts[index];
            var run = new Run(this, def, (int)Rng.Hash(Seed, RunsStarted), MaxHp);
            RunsStarted++;
            run.AddCargo(NextCargoId++, def.Cargo, 0, false, CargoState.Held);
            LostCargo lost = Pending;
            Pending = null;
            bool take = rescue && lost != null && !RescuedIds.Contains(lost.Id);
            run.RescueOffered = lost != null; run.RescueAccepted = take; run.RescueValue = take ? lost.Value : 0;
            if (take) run.AddCargo(lost.Id, lost.Type, lost.Cracks, true, CargoState.Ground);
            Run = run; LastResult = null;
            Emit(Ev.ContractStart, index, take ? 1 : 0, Balance.Entrance);
            if (lost != null) Emit(Ev.CargoRescueOffer, lost.Id, take ? 1 : 0, Balance.Entrance);
            run.EnterRoom(def.Layers[0][0], false);
            if (take) run.RescueCargo.Pos = run.Room.RescueSpot;
            Dirty = true;
            return run;
        }

        public void Tick(float dt, V2 move, bool dash)
        {
            Events.Clear();
            if (!InContract) return;
            Run.Tick(dt, move, dash);
            if (!Run.Running) Settle();
        }

        /// <summary>Desiste do contrato em curso: falha sem carga pendente.</summary>
        public void Abandon()
        {
            if (!InContract) return;
            Run.Finish(Outcome.Abandoned);
            Settle();
        }

        void Settle()
        {
            Run r = Run; ContractDef def = r.Contract; Cargo main = r.Main;
            var res = new ContractResult
            {
                Contract = def.Index, Outcome = r.Outcome, Time = r.Time, Cracks = main.Cracks, Hatched = r.Hatched, Base = def.Pay,
                CargoMul = r.Hatched ? CargoDef.All[(int)def.Cargo].HatchedMul : CargoDef.All[(int)def.Cargo].PayMul,
                IntegrityMul = Balance.IntegrityMul(main.Cracks), BagMul = CoinMul, ChestCoins = r.ChestCoins,
                RescueOffered = r.RescueOffered, RescueAccepted = r.RescueAccepted, DeathRoom = r.DeathRoom,
                RoomsNormal = r.RoomsNormal, RoomsFought = r.RoomsFought,
            };
            if (r.Outcome == Outcome.Delivered)
            {
                res.Payment = Credit(def.Pay * Balance.Pct(res.CargoMul) * Balance.Pct(res.IntegrityMul) / 10000.0);
                Emit(Ev.CurrencySource, res.Payment, 0, Balance.Client);
                Cargo rc = r.RescueCargo;
                if (rc != null && rc.State == CargoState.Held && !RescuedIds.Contains(rc.Id))
                {
                    res.RescueDelivered = true;
                    res.RescueBonus = Credit(r.RescueValue * Balance.Pct(Balance.RescueBonus) / 100.0);
                    RescuedIds.Add(rc.Id);
                    Emit(Ev.CurrencySource, res.RescueBonus, 2, Balance.Client);
                }
                if (def.Index == Reputation) Reputation++;
            }
            else if (r.Outcome == Outcome.Died || r.Outcome == Outcome.CargoStolen)
            {
                // a carga do contrato fica na masmorra (so 1 pendente: a mais nova substitui)
                Pending = new LostCargo { Id = main.Id, Type = main.Type, Cracks = main.Cracks, Room = r.Room.Id, Contract = def.Index, Value = Round(def.Pay * Balance.Pct(CargoDef.All[(int)main.Type].PayMul) / 100.0) };
            }
            res.Total = res.Payment + res.RescueBonus + res.ChestCoins;
            Emit(Ev.ContractEnd, def.Index, (int)r.Outcome, r.Courier.Pos);
            if (r.RescueAccepted) Emit(Ev.CargoRescueResult, r.RescueCargo.Id, res.RescueDelivered ? 1 : 0, r.Courier.Pos);
            LastResult = res;
            Dirty = true;
        }

        // ------------------------------------------------------------------ save (chave=valor; lixo vira estado inicial)

        /// <summary>Texto do save (v=1). null se SaveRefused (nao grave por cima de um save de versao futura). Limpa Dirty.</summary>
        public string Save()
        {
            if (SaveRefused) return null;
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("v=").Append(SaveVersion).Append('\n');
            sb.Append("seed=").Append(Seed).Append('\n');
            sb.Append("runs=").Append(RunsStarted).Append('\n');
            sb.Append("nid=").Append(NextCargoId).Append('\n');
            sb.Append("coins=").Append(Coins).Append('\n');
            sb.Append("rep=").Append(Reputation).Append('\n');
            sb.Append("lv=").Append(Levels[0]).Append(',').Append(Levels[1]).Append(',').Append(Levels[2]).Append('\n');
            if (Pending != null)
                sb.Append("pend=").Append(Pending.Id).Append(',').Append((int)Pending.Type).Append(',').Append(Pending.Cracks).Append(',')
                  .Append(Pending.Room).Append(',').Append(Pending.Value).Append(',').Append(Pending.Contract).Append('\n');
            if (RescuedIds.Count > 0)
            {
                var ids = new List<int>(RescuedIds);
                ids.Sort();
                sb.Append("resc=").Append(string.Join(",", ids)).Append('\n');
            }
            if (InContract)
            {
                Run r = Run;
                sb.Append("run=").Append(r.Contract.Index).Append(',').Append(r.Layer).Append(',').Append(r.Room.Id).Append(',').Append(r.Courier.Hp).Append(',')
                  .Append(r.Time.ToString("0.###", ci)).Append(',').Append(r.Seed).Append(',').Append(r.Chest != null && r.Chest.Opened ? 1 : 0).Append(',')
                  .Append(r.Hatched ? 1 : 0).Append(',').Append(r.Pet != null ? r.Pet.Hp : 0).Append(',').Append(r.RescueValue).Append(',')
                  .Append(r.RoomsNormal).Append(',').Append(r.RoomsFought).Append(',').Append(r.ChestCoins).Append(',').Append(r.RescueOffered ? 1 : 0).Append('\n');
                for (int i = 0; i < r.Cargos.Count; i++)
                {
                    Cargo g = r.Cargos[i];
                    // ao retomar: 0 = nas maos, 1 = no chao no ponto de resgate (resgate ainda nao pego na 1ª sala), 2 = fora do jogo
                    int st = g.State == CargoState.Gone ? 2 : g.Rescue && r.Layer == 0 && g.State == CargoState.Ground ? 1 : 0;
                    sb.Append("cg").Append(i).Append('=').Append(g.Id).Append(',').Append((int)g.Type).Append(',').Append(g.Cracks).Append(',')
                      .Append(g.Rescue ? 1 : 0).Append(',').Append(st).Append('\n');
                }
            }
            Dirty = false;
            return sb.ToString();
        }

        /// <summary>Le o save. Sem linha v = v0 (so meta: coins, lv, rep) e migra para v1 (Dirty = true para regravar). Versao futura =
        /// estado novo com SaveRefused (o texto original nao e tocado). Valores fora da faixa sao corrigidos; contrato em curso invalido
        /// e descartado.</summary>
        public static Sim Load(string text)
        {
            var sim = new Sim();
            if (string.IsNullOrEmpty(text)) return sim;
            var kv = new Dictionary<string, string>();
            foreach (string raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq > 0) kv[raw.Substring(0, eq).Trim()] = raw.Substring(eq + 1).Trim();
            }
            int v = kv.TryGetValue("v", out string vs) ? I(vs, 0) : 0;
            if (v > SaveVersion) { sim.SaveRefused = true; return sim; }

            sim.Coins = Math.Max(0, I(Get(kv, "coins"), 0));
            sim.Reputation = Clamp(I(Get(kv, "rep"), 0), 0, Contracts.Length);
            string[] lv = Get(kv, "lv").Split(',');
            for (int i = 0; i < Math.Min(lv.Length, sim.Levels.Length); i++) sim.Levels[i] = Clamp(I(lv[i], 0), 0, Balance.UpgradeMax);
            if (v <= 0) { sim.Dirty = true; return sim; }   // migracao v0 -> v1: v0 nao tinha contrato em curso nem carga pendente

            sim.Seed = I(Get(kv, "seed"), 1);
            sim.RunsStarted = Math.Max(0, I(Get(kv, "runs"), 0));
            sim.NextCargoId = Math.Max(1, I(Get(kv, "nid"), 1));
            foreach (string s in Get(kv, "resc").Split(',')) { int id = I(s, 0); if (id > 0) sim.RescuedIds.Add(id); }
            string[] p = Get(kv, "pend").Split(',');
            if (p.Length >= 6)
            {
                int id = I(p[0], 0), type = I(p[1], -1);
                if (id > 0 && type >= 0 && type < CargoDef.All.Length)
                    sim.Pending = new LostCargo { Id = id, Type = (CargoType)type, Cracks = Clamp(I(p[2], 0), 0, Balance.RelicBreak - 1), Room = I(p[3], 0), Value = Math.Max(0, I(p[4], 0)), Contract = I(p[5], 0) };
            }
            LoadRun(sim, kv);
            return sim;
        }

        static void LoadRun(Sim sim, Dictionary<string, string> kv)
        {
            string[] p = Get(kv, "run").Split(',');
            if (p.Length < 14) return;
            int ci = I(p[0], -1), layer = I(p[1], -1), room = I(p[2], -1), hp = I(p[3], 0);
            if (ci < 0 || ci >= Contracts.Length || hp <= 0) return;
            ContractDef def = Contracts[ci];
            if (layer < 0 || layer >= def.Layers.Length || Array.IndexOf(def.Layers[layer], room) < 0) return;
            var run = new Run(sim, def, I(p[5], 1), hp)
            {
                Layer = layer, Time = Math.Max(0f, F(p[4], 0f)), Hatched = p[7] == "1" && def.Cargo == CargoType.Egg,
                RescueValue = Math.Max(0, I(p[9], 0)), RoomsNormal = Math.Max(0, I(p[10], 0)), RoomsFought = Math.Max(0, I(p[11], 0)),
                ChestCoins = Math.Max(0, I(p[12], 0)), RescueOffered = p[13] == "1",
            };
            for (int i = 0; i < 2; i++)
            {
                string[] g = Get(kv, "cg" + i).Split(',');
                if (g.Length < 5) break;
                int type = I(g[1], -1), st = I(g[4], 2);
                if (type < 0 || type >= CargoDef.All.Length) return;
                bool rescue = i == 1 && g[3] == "1";
                if (i == 0 && (CargoType)type != def.Cargo) return;
                CargoState state = st == 0 ? CargoState.Held : st == 1 && rescue && layer == 0 ? CargoState.Ground : CargoState.Gone;
                if (i == 0 && state != CargoState.Held && !run.Hatched) return;   // carga do contrato fora das maos so se chocou
                Cargo c = run.AddCargo(Math.Max(1, I(g[0], 1)), (CargoType)type, Math.Max(0, I(g[2], 0)), rescue, state);
                if (i == 0 && run.Hatched) c.State = CargoState.Gone;
                if (c.Type == CargoType.Relic && c.Cracks >= Balance.RelicBreak) return;
            }
            if (run.Cargos.Count == 0) return;
            run.RescueAccepted = run.RescueCargo != null;
            if (run.Hatched) run.Pet = new Pet { Hp = Clamp(I(p[8], Balance.PetHp), 1, Balance.PetHp) };
            run.EnterRoom(room, p[6] == "1");
            if (run.RescueCargo != null && run.RescueCargo.State == CargoState.Ground) run.RescueCargo.Pos = run.Room.RescueSpot;
            sim.Run = run;
        }

        static string Get(Dictionary<string, string> kv, string k) => kv.TryGetValue(k, out string s) ? s : "";
        static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));
        static int I(string s, int d) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : d;
        static float F(string s, float d) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : d;
    }
}
