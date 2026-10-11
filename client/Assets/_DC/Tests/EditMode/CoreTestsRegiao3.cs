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
        public void Diag_R3_Lutas()   // TEMPORARIO: que inimigo faz a sala contar como luta escolhida
        {
            var kills = new Dictionary<string, int>(); var rooms = new Dictionary<string, int>();
            RegionDef reg = RegionDef.Of(3);
            for (int k = 3; k < 15; k++)
            {
                int c = reg.First + k;
                for (int s = 0; s < 6; s++)
                {
                    Sim sim = Sim.Load(Bot.Regiao3StartSave(700 + s)); sim.Reputation = c; sim.Seed = 700 + s;
                    sim.Equip(Bot.WeaponFor(sim, Sim.Contracts[c]));
                    var bot = new Bot(55 + s + k * 7);
                    if (sim.StartContract(c, false) == null) continue;
                    int guard = 0; var first = new Dictionary<int, string>();
                    while (sim.InContract && ++guard < 20000)
                    {
                        Run r = sim.Run; var types = new Dictionary<int, Enemy>(); foreach (Enemy e in r.Enemies) types[e.Id] = e;
                        string room = r.Room.Code; bool before = r.FoughtHere;
                        bot.Act(sim);
                        foreach (SimEvent e in sim.Events)
                            if (e.Kind == Ev.EnemyDie && !before && types.TryGetValue(e.A, out Enemy en) && !en.Def.Locks && !en.Wave)
                            { string key = room + " " + en.Type + (r.GrabbedBy != null ? " agarrado" : ""); kills[key] = (kills.TryGetValue(key, out int v) ? v : 0) + 1; before = true; }
                            else if (e.Kind == Ev.RoomFightChoice) { string key = Content.Rooms[e.B].Code + (e.A == 1 ? " luta" : " passou"); rooms[key] = (rooms.TryGetValue(key, out int v2) ? v2 : 0) + 1; }
                    }
                }
            }
            var keys = new List<string>(kills.Keys); keys.Sort(); foreach (string q in keys) TestContext.WriteLine("1a morte: " + kills[q] + "  " + q);
            keys = new List<string>(rooms.Keys); keys.Sort(); foreach (string q in keys) TestContext.WriteLine("sala: " + rooms[q] + "  " + q);
        }

        [Test]
        public void Diag_R3_Necro()   // TEMPORARIO: luta do B3
        {
            int c = RegionDef.Of(3).First + 15; int b3 = RegionDef.Of(3).Boss;
            for (int s = 0; s < 6; s++)
            {
                Sim sim = Sim.Load(Bot.Regiao3StartSave(600 + s)); sim.Reputation = c; sim.Seed = 600 + s;
                sim.Equip(Bot.WeaponFor(sim, Sim.Contracts[c]));
                var bot = new Bot(91 + s);
                sim.StartContract(c, false);
                int guard = 0; float t0 = -1f; var dmg = new Dictionary<string, int>(); int rays = 0, raise = 0, blink = 0, hits = 0, hp0 = 0, lit = 0, can = 0, steps = 0; float dsum = 0f;
                while (sim.InContract && ++guard < 30000)
                {
                    Run r = sim.Run;
                    bot.Act(sim);
                    if (r.Room.Id != b3) continue;
                    if (t0 < 0f) { t0 = r.Time; hp0 = r.Courier.Hp; }
                    Enemy n = null; foreach (Enemy e in r.Enemies) if (e.Type == EnemyType.Necromancer && e.Alive) n = e;
                    if (n != null) { steps++; dsum += V2.Dist(n.Pos, r.Courier.Pos); if (r.InLight(n.Pos)) lit++; if (r.CanHit(n)) can++; }
                    string last = "?";
                    foreach (SimEvent e in sim.Events)
                    {
                        if (e.Kind == Ev.Strike) last = ((EnemyType)e.B).ToString();
                        if (e.Kind == Ev.Strike && e.B == (int)EnemyType.Necromancer) rays++;
                        if (e.Kind == Ev.Raise) raise += e.B;
                        if (e.Kind == Ev.Blink) blink++;
                        if (e.Kind == Ev.EnemyHit && n != null && e.A == n.Id) hits++;
                        if (e.Kind == Ev.DamageTaken) { string k = last; dmg[k] = (dmg.TryGetValue(k, out int v) ? v : 0) + e.A; }
                    }
                }
                Run rr = sim.Run; Enemy nn = null; foreach (Enemy e in rr.Enemies) if (e.Type == EnemyType.Necromancer) nn = e;
                var sb = new StringBuilder();
                foreach (var kv in dmg) sb.Append(kv.Key + "=" + kv.Value + " ");
                TestContext.WriteLine("seed " + s + ": " + sim.LastResult.Outcome + " t=" + (rr.Time - t0).ToString("0") + "s hp0=" + hp0 + " necroHp=" + (nn != null ? nn.Hp : -1) + " raios=" + rays + " ergueu=" + raise + " saltos=" + blink + " acertos=" + hits
                    + " dist=" + (steps > 0 ? dsum / steps : 0).ToString("0.0") + " luz%=" + (steps > 0 ? 100 * lit / steps : 0) + " alvo%=" + (steps > 0 ? 100 * can / steps : 0) + " arma=" + rr.Weapon + " dano: " + sb);
            }
        }

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
