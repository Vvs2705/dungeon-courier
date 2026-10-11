using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Regiao 3 "Cripta Assombrada" (CONTEUDO_REGIOES §1.1, §3, §5, §6): registro e level design das salas, escuro e lanterna,
    /// espelho, balao, teias, fantasma, esqueleto, aranha, mumia, gargula, Viuva Aranha, Necromante e a campanha da regiao pelo bot
    /// (docs/BALANCE_REGIAO3.md). So API publica.</summary>
    public partial class CoreTests
    {
        // ------------------------------------------------------------------ medicao (BALANCE_REGIAO3.md)

        [Test]
        public void Diag_R3_Rachaduras()   // TEMPORARIO: de onde vem as rachaduras
        {
            var causes = new Dictionary<string, int>();
            foreach (int k in new[] { 2, 5 })
            {
                int c = RegionDef.Of(3).First + k;
                for (int s = 0; s < 12; s++)
                {
                    Sim sim = Sim.Load(Bot.Regiao3StartSave(500 + s)); sim.Reputation = c; sim.Seed = 500 + s;
                    sim.Equip(Bot.WeaponFor(sim, Sim.Contracts[c]));
                    var bot = new Bot(77 + s);
                    if (sim.StartContract(c, false) == null) { causes["nao comecou"] = 1; continue; }
                    var prev = new List<SimEvent>();
                    int guard = 0;
                    while (sim.InContract && ++guard < 20000)
                    {
                        Run r = sim.Run; CourierState st = r.Courier.State; string room = r.Room.Code; CargoState cs = r.Main.State;
                        bot.Act(sim);
                        foreach (SimEvent e in sim.Events)
                        {
                            if (e.Kind == Ev.CargoIntegrity && e.A == r.Main.Id)
                            {
                                string why = "?";
                                foreach (SimEvent p in sim.Events) { if (p.Kind == Ev.DamageTaken) why = "golpe no courier"; else if (p.Kind == Ev.CargoDrop) why = "queda " + p.B; else if (p.Kind == Ev.ArrowHit && p.A == 1) why = "flecha no chao"; else if (p.Kind == Ev.Reflect) why = "reflexo " + p.A; else if (p.Kind == Ev.Strike) why = why == "?" ? "strike " + (EnemyType)p.B : why; }
                                Enemy near = null; float nd = 99f; foreach (Enemy en in r.Enemies) if (en.Alive && V2.Dist(en.Pos, r.Courier.Pos) < nd) { nd = V2.Dist(en.Pos, r.Courier.Pos); near = en; }
                                string key = Sim.Contracts[c].Code + " " + room + " " + why + " cs=" + cs + " st=" + st + (r.Webbed > 0 ? " PRESO" : "") + (r.GrabbedBy != null ? " AGARRADO" : "") + " perto=" + (near != null ? near.Type + "/" + near.State : "-");
                                causes[key] = (causes.TryGetValue(key, out int n) ? n : 0) + 1;
                            }
                        }
                    }
                    string end = Sim.Contracts[c].Code + " fim " + sim.LastResult.Outcome;
                    causes[end] = (causes.TryGetValue(end, out int m) ? m : 0) + 1;
                }
            }
            var keys = new List<string>(causes.Keys); keys.Sort();
            foreach (string k in keys) TestContext.WriteLine(causes[k] + "  " + k);
        }

        [Test]
        public void Tabela_Regiao3_ParaBalanceMd()
        {
            var ci = CultureInfo.InvariantCulture;
            RegionDef reg = RegionDef.Of(3);
            Bot.Campaign[] camps = Bot.PlayRegion3(8);
            Bot.Stats[] t = Bot.MeasureRegion3Table(camps, 10);
            var sb = new StringBuilder();
            sb.AppendLine("| Contrato | Carga | Salas | % concluído | % morte | % falha de carga | duração média (s) | mediana (s) | % salas com luta | moedas médias | estrelas | 3 estrelas % | rachaduras |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            int fought = 0, normal = 0;
            for (int c = 0; c < t.Length; c++)
            {
                Bot.Stats s = t[c]; ContractDef d = Sim.Contracts[s.Contract];
                fought += s.RoomsFought; normal += s.RoomsNormal;
                sb.AppendLine(string.Format(ci, "| {0} | {1} | {2} | {3:0} | {4:0} | {5:0} | {6:0} | {7:0} | {8:0} | {9:0} | {10:0.00} | {11:0} | {12:0.00} |",
                    d.Code, CargoDef.All[(int)d.Cargo].Name, d.Rooms, s.Pct(s.Delivered), s.Pct(s.Died), s.Pct(s.CargoFail),
                    s.AvgTime, s.MedianTime, s.FightPct, s.AvgCoins, s.AvgStars, s.Pct(s.ThreeStars), s.AvgCracks));
            }
            sb.AppendLine(string.Format(ci, "H1 (agressividade 0,5): {0:0}% das salas com luta escolhida", normal == 0 ? 0 : 100.0 * fought / normal));
            var dmg = new double[Content.Rooms.Length]; var vis = new int[Content.Rooms.Length]; var tim = new double[Content.Rooms.Length]; var fi = new int[Content.Rooms.Length];
            foreach (Bot.Stats s in t) for (int r = 0; r < dmg.Length; r++) { dmg[r] += s.RoomDamage[r]; vis[r] += s.RoomVisits[r]; tim[r] += s.RoomTime[r]; fi[r] += s.RoomFights[r]; }
            sb.AppendLine("| Sala | Ícone | Risco da carga | Visitas | Dano por visita | Tempo por visita (s) | % luta |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (int id in reg.Rooms)
            {
                RoomDef room = Content.Rooms[id];
                if (vis[id] > 0 && !room.Delivery)
                    sb.AppendLine(string.Format(ci, "| {0} {1} | {2} | {3} | {4} | {5:0.00} | {6:0.0} | {7:0} |", room.Code, room.Name, room.Threat, room.Risk, vis[id],
                        dmg[id] / vis[id], tim[id] / vis[id], room.Normal ? 100.0 * fi[id] / vis[id] : 0));
            }
            int fin = 0, att = 0, deaths = 0, earned = 0, spent = 0, stars = 0, coins = 0; float time = 0f;
            var per = new double[reg.Contracts.Length]; var tper = new double[reg.Contracts.Length];
            foreach (Bot.Campaign cp in camps)
            {
                if (cp.Finished) fin++;
                att += cp.Attempts; deaths += cp.Deaths; earned += cp.Earned; spent += cp.Spent; stars += cp.Stars - Bot.Region3StartStars; time += cp.Time; coins += cp.Coins;
                for (int i = 0; i < per.Length; i++) { per[i] += cp.AttemptsPer[reg.First + i]; tper[i] += cp.TimePer[reg.First + i]; }
            }
            double n = camps.Length;
            sb.AppendLine(string.Format(ci, "Campanha da regiao 3 ({0} sementes, do fim estimado da regiao 2): fecha {1:0}% · tentativas {2:0.0} · mortes {3:0.0} · tempo de jogo do bot {4:0.0} min · moedas ganhas {5:0} · gastas {6:0} · paradas no fim {7:0} · estrelas {8:0.0}/48",
                n, 100.0 * fin / n, att / n, deaths / n, time / n / 60f, earned / n, spent / n, coins / n, stars / n));
            var line = new StringBuilder("Por contrato (tentativas · tempo da entrega s):");
            for (int i = 0; i < per.Length; i++) line.Append(string.Format(ci, " {0}={1:0.00}·{2:0}", reg.Contracts[i].Code, per[i] / n, tper[i] / n));
            sb.AppendLine(line.ToString());
            TestContext.WriteLine(sb.ToString());
            Assert.AreEqual(16, t.Length);
        }
    }
}
