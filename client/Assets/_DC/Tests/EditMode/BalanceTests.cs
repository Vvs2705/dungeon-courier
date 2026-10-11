using System.Globalization;
using System.Text;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Medicao do bot (docs/BALANCE.md, secao v1). Asserts de faixa larga e de sanidade, nao de afinacao fina. A tabela sai no log
    /// do teste Tabela_ParaBalanceMd (dotnet test client/tools/coretests --logger "console;verbosity=detailed").
    /// Tabela realista: cada contrato medido a partir do save que a campanha do bot tinha antes da 1a tentativa dele (guilda, armas e
    /// estrelas de quem chegou ali), Camps campanhas x CampSeeds sementes.</summary>
    public class BalanceTests
    {
        const int Seeds = 200, Camps = 6, CampSeeds = 20, CampaignRuns = 100, RouteSeeds = 6;
        static Bot.Stats[] table; static Bot.Campaign[] camps; static Bot.Route route;

        static Bot.Stats[] Table() { if (table == null) table = Bot.MeasureCampaignTable(Camps, CampSeeds); return table; }
        static Bot.Campaign[] Campaigns()
        {
            if (camps != null) return camps;
            camps = new Bot.Campaign[CampaignRuns];
            for (int s = 0; s < CampaignRuns; s++) camps[s] = Bot.PlayCampaign(5000 + s);
            return camps;
        }
        static Bot.Route Route() { if (route == null) route = Bot.MeasureRoute(RouteSeeds); return route; }
        static bool IsMessage(int c) => Sim.Contracts[c].Cargo == CargoType.Message;

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
        public void Contratos_C1aC24_RodamSemExcecao_ComNumerosSaos()
        {
            Bot.Stats[] t = Table();
            Assert.AreEqual(24, t.Length);
            for (int c = 0; c < t.Length; c++)
            {
                Bot.Stats s = t[c]; string k = Sim.Contracts[c].Code;
                Assert.AreEqual(Camps * CampSeeds, s.Runs, k);
                Assert.AreEqual(0, s.Timeout, k + ": bot preso (desistiu por tempo)");
                Assert.That(s.AvgTime, Is.InRange(5.0, 360.0), k + ": duracao media fora de 5 s..6 min");
                Assert.GreaterOrEqual(s.Pct(s.Delivered), 40.0, k + ": quase ninguem entrega com a guilda da campanha");
                Assert.Less(s.Pct(s.Died), 60.0, k + ": morte demais");
                Assert.That(s.FightPct, Is.InRange(10.0, 100.0), k + ": % de salas com luta fora da faixa de sanidade");
            }
            for (int c = 0; c < 3; c++) Assert.GreaterOrEqual(t[c].Pct(t[c].Delivered), 85.0, Sim.Contracts[c].Code + ": tutorial");
        }

        /// <summary>CONTRATO_V1 §2: alvo do bot 1,5-3 min por contrato do C4 em diante; tutoriais curtos. Faixa larga aqui (o alvo fino e o
        /// que BALANCE.md registra): cada contrato do C4 em diante entre 20 s e 5 min, media sem a mensagem (que e corrida por desenho)
        /// de pelo menos 60 s, e o chefe com pelo menos 90 s.</summary>
        [Test]
        public void Duracao_TutoriaisCurtos_DoC4EmDianteContratosLongos()
        {
            Bot.Stats[] t = Table();
            for (int c = 0; c < 3; c++) Assert.Less(t[c].AvgTime, 60.0, Sim.Contracts[c].Code + ": tutorial longo demais");
            double sum = 0; int n = 0;
            for (int c = 3; c < t.Length; c++)
            {
                Assert.That(t[c].AvgTime, Is.InRange(20.0, 300.0), Sim.Contracts[c].Code);
                if (!IsMessage(c)) { sum += t[c].AvgTime; n++; }
            }
            Assert.GreaterOrEqual(sum / n, 60.0, "contratos do C4 em diante curtos demais (alvo do bot: 90-180 s)");
            Assert.GreaterOrEqual(t[23].AvgTime, 90.0, "C24 (chefe)");
        }

        [Test]
        public void ContratosGerados_DoDiaEPorCodigo_RodamSemExcecao()
        {
            Bot.Stats g = Bot.MeasureGenerated(60);
            Assert.AreEqual(60, g.Runs);
            Assert.AreEqual(0, g.Timeout, "bot preso num contrato gerado");
            Assert.That(g.AvgTime, Is.InRange(5.0, 360.0));
            Assert.Greater(g.Delivered, 0);
        }

        [Test]
        public void Lutar_EEscolha_AgressividadeMudaOPercentualDeLuta_EPassarDireto_EViavel()
        {
            int fLow = 0, nLow = 0, fHigh = 0, nHigh = 0;
            for (int c = 0; c < Sim.Contracts.Length; c++)
            {
                if (c < 3)
                {
                    Bot.Stats lo0 = Bot.Measure(c, 60, aggression: 0.1f);
                    Assert.GreaterOrEqual(lo0.Pct(lo0.Delivered), 90.0, Sim.Contracts[c].Code + ": evitar lutas tem de ser viavel no comeco");
                    continue;
                }
                Bot.Stats lo = Bot.Measure(c, 20, aggression: 0.1f, guild: 3, weaponLevel: 2), hi = Bot.Measure(c, 20, aggression: 0.9f, guild: 3, weaponLevel: 2);
                fLow += lo.RoomsFought; nLow += lo.RoomsNormal; fHigh += hi.RoomsFought; nHigh += hi.RoomsNormal;
            }
            double low = 100.0 * fLow / nLow, high = 100.0 * fHigh / nHigh;
            Assert.Greater(high - low, 15.0, "se a agressividade nao muda a luta, lutar nao e escolha (H1)");
        }

        [Test]
        public void Reliquia_VerboAparece_SemTornarOsContratosInviaveis()
        {
            double cracks = 0; int n = 0;
            for (int c = 0; c < Table().Length; c++)   // regiao 1 (a tabela)
            {
                if (Sim.Contracts[c].Cargo != CargoType.Relic) continue;
                Bot.Stats s = Table()[c];
                cracks += s.AvgCracks; n++;
                Assert.GreaterOrEqual(s.Pct(s.Delivered), 40.0, Sim.Contracts[c].Code + " inviavel");
            }
            Assert.GreaterOrEqual(n, 3);
            Assert.GreaterOrEqual(cracks / n, 0.8, "rachadura media na reliquia < 0,8: o verbo nao aparece");
            Assert.Less(Table()[0].AvgCracks + Table()[1].AvgCracks, 0.05, "Caixa nao racha por queda/golpe no courier");
        }

        /// <summary>O icone de caveiras da porta tem de contar a verdade (H2): a media de dano por visita sobe com o icone (folga de 0,3) e
        /// nenhuma sala passa da media do icone vizinho (sala de icone 1 abaixo da media do 2; de icone 3 acima da media do 2). Com 16 salas o
        /// pareamento estrito sala a sala da v0.1 (6 salas) vira ruido. B1/M1 ficam fora (chefes, porta unica) e D1 nao tem inimigos.</summary>
        [Test]
        public void IconeDeAmeaca_AcompanhaODanoMedido()
        {
            var dmg = new double[Content.Rooms.Length]; var visits = new int[Content.Rooms.Length];
            foreach (Bot.Stats s in Table())
                for (int r = 0; r < dmg.Length; r++) { dmg[r] += s.RoomDamage[r]; visits[r] += s.RoomVisits[r]; }
            var mean = new double[4]; var count = new int[4];
            foreach (RoomDef a in Content.Rooms)
            {
                if (!a.Normal || a.Region != 1) continue;   // a tabela e da regiao 1 (as outras: BALANCE_REGIAON.md)
                Assert.GreaterOrEqual(visits[a.Id], 60, a.Code + ": poucas visitas para medir");
                mean[a.Threat] += dmg[a.Id] / visits[a.Id]; count[a.Threat]++;
            }
            for (int t = 1; t <= 3; t++) { Assert.Greater(count[t], 0, "icone " + t + " sem sala"); mean[t] /= count[t]; }
            for (int t = 1; t < 3; t++) Assert.Greater(mean[t + 1] - mean[t], 0.3, "icones " + t + " e " + (t + 1) + " sem folga de dano");
            foreach (RoomDef a in Content.Rooms)
            {
                if (!a.Normal || a.Region != 1) continue;   // a tabela e da regiao 1 (as outras: BALANCE_REGIAON.md)
                double d = dmg[a.Id] / visits[a.Id];
                if (a.Threat < 3) Assert.Less(d, mean[a.Threat + 1], a.Code + " (icone " + a.Threat + ") tira mais vida que a media do icone " + (a.Threat + 1));
                if (a.Threat > 1) Assert.Greater(d, mean[a.Threat - 1], a.Code + " (icone " + a.Threat + ") tira menos vida que a media do icone " + (a.Threat - 1));
            }
        }

        [Test]
        public void Campanha_FechaOs24Contratos_ESobraRaloNaGuilda()
        {
            int finished = 0, attempts = 0, levels = 0, stars = 0, both = 0;
            foreach (Bot.Campaign cp in Campaigns())
            {
                if (cp.Finished) finished++;
                attempts += cp.Attempts; stars += cp.Stars;
                foreach (int l in cp.Levels) levels += l;
                if (cp.UnlockAt[1] >= 0 && cp.UnlockAt[2] >= 0) both++;
                Assert.GreaterOrEqual(cp.Coins, 0);
            }
            double n = CampaignRuns;
            Assert.GreaterOrEqual(finished, 90, "a campanha do bot tem de fechar os 24 contratos");
            Assert.LessOrEqual(attempts / n, 60.0, "tentativas demais para fechar a campanha");
            Assert.Less(levels / n, Balance.UpgradeLines * Balance.UpgradeMax, "a guilda nao pode esgotar nos 24 contratos (ralo some)");
            Assert.Greater(levels / n, 8.0, "a economia nao paga a guilda");
            Assert.GreaterOrEqual(both, 90, "as 2 armas destravam ao longo da campanha");
            Assert.That(stars / n, Is.InRange(48.0, 72.0), "estrelas medias da campanha");
        }

        /// <summary>H2 (a carga muda a rota), medido trocando a carga dos 24 contratos: na maioria das bifurcacoes a porta escolhida depende
        /// da carga, e com carga que sofre com ladrao (reliquia, barril) a porta com o icone de ladrao e evitada.</summary>
        [Test]
        public void Rota_ACargaMudaAPorta_H2()
        {
            Bot.Route r = Route();
            Assert.Greater(r.ForkCount, 50);
            Assert.Less(r.SameDoorPct, 50.0, "a mesma porta em mais de 80% das vezes qualquer que seja a carga: H2 refutada");
            double box = r.ThiefPct[(int)CargoType.Box];
            Assert.LessOrEqual(r.ThiefPct[(int)CargoType.Relic] + 15.0, box, "reliquia nao evita a porta do ladrao");
            Assert.LessOrEqual(r.ThiefPct[(int)CargoType.Barrel] + 15.0, box, "barril nao evita a porta do ladrao");
        }

        [Test]
        public void Tabela_ParaBalanceMd()
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("| Contrato | Carga | Salas | % concluído | % morte | % falha de carga | duração média (s) | mediana (s) | % salas com luta | moedas médias | estrelas | 3 estrelas % | rachaduras |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            Bot.Stats[] t = Table();
            for (int c = 0; c < t.Length; c++)
            {
                Bot.Stats s = t[c]; ContractDef d = Sim.Contracts[c];
                sb.AppendLine(string.Format(ci, "| {0} | {1} | {2} | {3:0} | {4:0} | {5:0} | {6:0} | {7:0} | {8:0} | {9:0} | {10:0.00} | {11:0} | {12:0.00} |",
                    d.Code, CargoDef.All[(int)d.Cargo].Name, d.Rooms, s.Pct(s.Delivered), s.Pct(s.Died), s.Pct(s.CargoFail),
                    s.AvgTime, s.MedianTime, s.FightPct, s.AvgCoins, s.AvgStars, s.Pct(s.ThreeStars), s.AvgCracks));
            }
            sb.AppendLine();
            var dmg = new double[Content.Rooms.Length]; var vis = new int[Content.Rooms.Length]; var tim = new double[Content.Rooms.Length]; var fi = new int[Content.Rooms.Length];
            foreach (Bot.Stats s in t) for (int r = 0; r < dmg.Length; r++) { dmg[r] += s.RoomDamage[r]; vis[r] += s.RoomVisits[r]; tim[r] += s.RoomTime[r]; fi[r] += s.RoomFights[r]; }
            sb.AppendLine("| Sala | Ícone | Risco da carga | Visitas | Dano por visita | Tempo por visita (s) | % luta |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (RoomDef room in Content.Rooms)
                if (vis[room.Id] > 0 && !room.Delivery)
                    sb.AppendLine(string.Format(ci, "| {0} {1} | {2} | {3} | {4} | {5:0.00} | {6:0.0} | {7:0} |", room.Code, room.Name, room.Threat, room.Risk, vis[room.Id],
                        dmg[room.Id] / vis[room.Id], tim[room.Id] / vis[room.Id], room.Normal ? 100.0 * fi[room.Id] / vis[room.Id] : 0));
            sb.AppendLine();
            int fin = 0, att = 0, deaths = 0, earned = 0, spent = 0, stars = 0; float time = 0f;
            var lv = new double[Balance.UpgradeLines]; var wl = new double[WeaponDef.All.Length]; var per = new double[24]; var tper = new double[24]; var gat = new double[24];
            double u1 = 0, u2 = 0; int nu1 = 0, nu2 = 0;
            foreach (Bot.Campaign cp in Campaigns())
            {
                if (cp.Finished) fin++;
                att += cp.Attempts; deaths += cp.Deaths; earned += cp.Earned; spent += cp.Spent; stars += cp.Stars; time += cp.Time;
                for (int i = 0; i < lv.Length; i++) lv[i] += cp.Levels[i];
                for (int i = 0; i < wl.Length; i++) wl[i] += cp.WeaponLevels[i];
                for (int i = 0; i < 24; i++) { per[i] += cp.AttemptsPer[i]; tper[i] += cp.TimePer[i]; gat[i] += cp.GuildAt[i]; }
                if (cp.UnlockAt[1] >= 0) { u1 += cp.UnlockAt[1] + 1; nu1++; }
                if (cp.UnlockAt[2] >= 0) { u2 += cp.UnlockAt[2] + 1; nu2++; }
            }
            double n = CampaignRuns;
            sb.AppendLine(string.Format(ci, "Campanha ({0} sementes): fecha {1}% · tentativas {2:0.0} · mortes {3:0.0} · tempo de jogo do bot {4:0.0} min · moedas ganhas {5:0} · gastas {6:0} · estrelas {7:0.0}/72",
                CampaignRuns, 100 * fin / CampaignRuns, att / n, deaths / n, time / n / 60f, earned / n, spent / n, stars / n));
            sb.AppendLine(string.Format(ci, "Guilda no fim (Vida/Dash/Bolsa/Mochila): {0:0.0}/{1:0.0}/{2:0.0}/{3:0.0} de 8 · armas (Estilingue/Bumerangue/Bombinha) nível {4:0.0}/{5:0.0}/{6:0.0} · Bumerangue destrava no C{7:0.0}, Bombinha no C{8:0.0}",
                lv[0] / n, lv[1] / n, lv[2] / n, lv[3] / n, wl[0] / n, wl[1] / n, wl[2] / n, nu1 > 0 ? u1 / nu1 : 0, nu2 > 0 ? u2 / nu2 : 0));
            var line = new StringBuilder("Por contrato (tentativas · tempo da entrega s · níveis da guilda somados):");
            for (int i = 0; i < 24; i++) line.Append(string.Format(ci, " C{0}={1:0.00}·{2:0}·{3:0.0}", i + 1, per[i] / n, tper[i] / n, gat[i] / n));
            sb.AppendLine(line.ToString());
            Bot.Route r0 = Route();
            var h2 = new StringBuilder(string.Format(ci, "H2: bifurcações em que a carga não muda a porta {0:0.0}% de {1} · porta mais escolhida {2:0.0}% · porta com ladrão escolhida:", r0.SameDoorPct, r0.ForkCount, r0.ModalPct));
            for (int i = 0; i < RegionDef.Of(1).CargoCount; i++) h2.Append(string.Format(ci, " {0} {1:0}%", CargoDef.All[i].Name, r0.ThiefPct[i]));
            sb.AppendLine(h2.ToString());
            var h1 = new StringBuilder("H1 (guilda 3, estilingue 2, C4-C24, 20 sementes): % salas com luta por agressividade");
            foreach (float ag in new[] { 0.1f, 0.5f, 0.9f })
            {
                int f = 0, nn = 0, dl = 0, rr = 0;
                for (int c = 3; c < 24; c++) { Bot.Stats s = Bot.Measure(c, 20, aggression: ag, guild: 3, weaponLevel: 2); f += s.RoomsFought; nn += s.RoomsNormal; dl += s.Delivered; rr += s.Runs; }
                h1.Append(string.Format(ci, " · {0:0.0}: {1:0}% (entrega {2:0}%)", ag, 100.0 * f / nn, 100.0 * dl / rr));
            }
            sb.AppendLine(h1.ToString());
            var wpn = new StringBuilder("Armas no C12 (guilda 3, 40 sementes): tempo médio s / % entrega");
            foreach (Weapon w in new[] { Weapon.Sling, Weapon.Boomerang, Weapon.Bomb })
                foreach (int l in new[] { 1, 3 })
                {
                    Bot.Stats s = Bot.Measure(11, 40, guild: 3, weapon: w, weaponLevel: l);
                    wpn.Append(string.Format(ci, " · {0} {1}: {2:0}s/{3:0}%", WeaponDef.All[(int)w].Name, l, s.AvgTime, s.Pct(s.Delivered)));
                }
            sb.AppendLine(wpn.ToString());
            Bot.Stats g = Bot.MeasureGenerated(60);
            sb.AppendLine(string.Format(ci, "Gerados (código, guilda 0, 60 payloads): entrega {0:0}% · morte {1:0}% · {2:0}s", g.Pct(g.Delivered), g.Pct(g.Died), g.AvgTime));
            TestContext.WriteLine(sb.ToString());
            Assert.AreEqual(RegionDef.Of(1).Contracts.Length, t.Length);
        }
    }
}
