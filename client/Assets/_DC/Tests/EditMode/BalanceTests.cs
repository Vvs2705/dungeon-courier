using System.Globalization;
using System.Text;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Medicao do bot (docs/BALANCE.md). Asserts de faixa larga e de sanidade, nao de afinacao fina. A tabela sai no log do
    /// teste Tabela_ParaBalanceMd (dotnet test client/tools/coretests --logger "console;verbosity=detailed").</summary>
    public class BalanceTests
    {
        const int Seeds = 200;
        static Bot.Stats[] table;

        /// <summary>Bot padrao: agressividade 0,5, reacao 0,25 s, guilda nivel 0, 200 sementes por contrato.</summary>
        static Bot.Stats[] Table()
        {
            if (table != null) return table;
            table = new Bot.Stats[Sim.Contracts.Length];
            for (int c = 0; c < table.Length; c++) table[c] = Bot.Measure(c, Seeds);
            return table;
        }

        [Test]
        public void Bot_ConcluiAFtueC1_EmPeloMenos90PorCento_EmDuasBasesDeSemente()
        {
            Bot.Stats a = Bot.Measure(0, Seeds, baseSeed: 1000), b = Bot.Measure(0, Seeds, baseSeed: 90000);
            Assert.GreaterOrEqual(a.Pct(a.Delivered), 90.0, "C1 base 1000");
            Assert.GreaterOrEqual(b.Pct(b.Delivered), 90.0, "C1 base 90000");
            Bot.Stats slow = Bot.Measure(0, Seeds, reaction: 0.5f);
            Assert.GreaterOrEqual(slow.Pct(slow.Delivered), 90.0, "C1 com reacao de 0,5 s");
        }

        [Test]
        public void Contratos_C1aC8_RodamSemExcecao_ComNumerosSaos()
        {
            Bot.Stats[] t = Table();
            for (int c = 0; c < t.Length; c++)
            {
                Bot.Stats s = t[c]; string k = Sim.Contracts[c].Code;
                Assert.AreEqual(Seeds, s.Runs, k);
                Assert.AreEqual(0, s.Timeout, k + ": bot preso (desistiu por tempo)");
                Assert.That(s.AvgTime, Is.InRange(5.0, 360.0), k + ": duracao media fora de 5 s..6 min");
                Assert.GreaterOrEqual(s.Pct(s.Delivered), 15.0, k + ": quase ninguem entrega (guilda nivel 0)");
                Assert.Less(s.Pct(s.Died), 85.0, k + ": morte demais");
                Assert.That(s.FightPct, Is.InRange(10.0, 95.0), k + ": % de salas com luta fora da faixa de sanidade");
            }
            Assert.GreaterOrEqual(t[1].Pct(t[1].Delivered), 80.0, "C2");
            Assert.GreaterOrEqual(t[2].Pct(t[2].Delivered), 80.0, "C3");
        }

        [Test]
        public void Lutar_EEscolha_AgressividadeMudaOPercentualDeLuta_EPassarDireto_EViavel()
        {
            int fLow = 0, nLow = 0, fHigh = 0, nHigh = 0;
            for (int c = 0; c < Sim.Contracts.Length; c++)
            {
                Bot.Stats lo = Bot.Measure(c, 60, aggression: 0.1f), hi = Bot.Measure(c, 60, aggression: 0.9f);
                fLow += lo.RoomsFought; nLow += lo.RoomsNormal; fHigh += hi.RoomsFought; nHigh += hi.RoomsNormal;
                if (c < 3) Assert.GreaterOrEqual(lo.Pct(lo.Delivered), 90.0, Sim.Contracts[c].Code + ": evitar lutas tem de ser viavel no comeco");
            }
            double low = 100.0 * fLow / nLow, high = 100.0 * fHigh / nHigh;
            Assert.Greater(high - low, 15.0, "se a agressividade nao muda a luta, lutar nao e escolha (H1)");
        }

        [Test]
        public void Campanha_FechaOs8Contratos_ESobraRaloNaGuilda()
        {
            int n = 100, finished = 0, attempts = 0, levels = 0;
            for (int s = 0; s < n; s++)
            {
                Bot.Campaign cp = Bot.PlayCampaign(5000 + s);
                if (cp.Finished) finished++;
                attempts += cp.Attempts;
                levels += cp.Levels[0] + cp.Levels[1] + cp.Levels[2];
                Assert.GreaterOrEqual(cp.Coins, 0);
            }
            Assert.GreaterOrEqual(finished, 90, "a campanha do bot tem de fechar os 8 contratos");
            Assert.LessOrEqual(attempts / (double)n, 20.0, "tentativas demais para fechar a campanha");
            Assert.Less(levels / (double)n, 3 * Balance.UpgradeMax, "a guilda nao pode esgotar nos 8 contratos (ralo some)");
            Assert.Greater(levels / (double)n, 1.0, "nenhum upgrade comprado: a economia nao paga a guilda");
        }

        [Test]
        public void Tabela_ParaBalanceMd()
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("| Contrato | Carga | Camadas | % concluído | % morte | % falha de carga | duração média (s) | % salas com luta | moedas médias | vida na entrega |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            Bot.Stats[] t = Table();
            for (int c = 0; c < t.Length; c++)
            {
                Bot.Stats s = t[c]; ContractDef d = Sim.Contracts[c];
                sb.AppendLine(string.Format(ci, "| {0} | {1} | {2} | {3:0} | {4:0} | {5:0} | {6:0.0} | {7:0} | {8:0} | {9:0.0} |",
                    d.Code, CargoDef.All[(int)d.Cargo].Name, d.Layers.Length - 1, s.Pct(s.Delivered), s.Pct(s.Died), s.Pct(s.CargoFail),
                    s.AvgTime, s.FightPct, s.AvgCoins, s.Delivered > 0 ? s.HpLeftSum / s.Delivered : 0));
            }
            sb.AppendLine();
            int n = 100, fin = 0, att = 0, deaths = 0, earned = 0; float time = 0f; int[] lv = new int[3]; int[] per = new int[Sim.Contracts.Length];
            for (int s = 0; s < n; s++)
            {
                Bot.Campaign cp = Bot.PlayCampaign(5000 + s);
                if (cp.Finished) fin++;
                att += cp.Attempts; deaths += cp.Deaths; earned += cp.Earned; time += cp.Time;
                for (int i = 0; i < 3; i++) lv[i] += cp.Levels[i];
                for (int i = 0; i < per.Length; i++) per[i] += cp.AttemptsPer[i];
            }
            sb.AppendLine(string.Format(ci, "Campanha ({0} sementes): fecha {1}% · tentativas {2:0.0} · mortes {3:0.0} · tempo de jogo {4:0.0} min · moedas ganhas {5:0} · níveis Vida/Dash/Bolsa {6:0.0}/{7:0.0}/{8:0.0}",
                n, 100 * fin / n, att / (double)n, deaths / (double)n, time / n / 60f, earned / (double)n, lv[0] / (double)n, lv[1] / (double)n, lv[2] / (double)n));
            var line = new StringBuilder("Tentativas por contrato:");
            for (int i = 0; i < per.Length; i++) line.Append(string.Format(ci, " C{0}={1:0.00}", i + 1, per[i] / (double)n));
            sb.AppendLine(line.ToString());
            TestContext.WriteLine(sb.ToString());
            Assert.AreEqual(Sim.Contracts.Length, t.Length);
        }
    }
}
