using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Raia E (Leva C): regras novas da economia e a API pedida pela View. So API publica.</summary>
    public partial class CoreTests
    {
        /// <summary>Cada equipamento tem 4 baus de projeto para 3 projetos (1 de reserva: pular um bau nao trava o equipamento) e o Gancho
        /// completa na regiao 3; porta e bau so mostram o projeto enquanto o equipamento precisa dele (depois o bau paga so as moedas).</summary>
        [Test]
        public void Projetos_4BausPara3_GanchoNaRegiao3_ProjetoSobrandoViraSoMoeda()
        {
            var count = new Dictionary<Gear, int>(); int hookR3 = 0;
            foreach (RoomDef room in Content.Rooms)
            {
                if (room.Blueprint == Gear.None) continue;
                count[room.Blueprint] = (count.TryGetValue(room.Blueprint, out int n) ? n : 0) + 1;
                if (room.Blueprint == Gear.Hook && room.Region == 3) hookR3++;
                Assert.Greater(room.ChestCoins, 0, room.Code + ": projeto sem bau");
                Assert.Greater(room.Region, 1, room.Code + ": os projetos dos baus comecam na regiao 2");
            }
            for (int g = 1; g < GearDef.All.Length; g++) Assert.AreEqual(Balance.GearBlueprints + 1, count[(Gear)g], GearDef.All[g].Name + ": 4 baus para 3 projetos");
            Assert.AreEqual(Balance.GearBlueprints, hookR3, "o gancho completa na regiao 3");

            int m22 = ContractOf("M2-2"), r25 = RoomOf("R2-5");   // M2-2: R2-4 (bau com Botas) e a porta para a R2-5 (bau com Botas)
            Run r = new Sim(5).DevStartAt(m22, "R2-4");
            Assert.AreEqual(Gear.Boots, r.Chest.Blueprint); Assert.AreEqual(Gear.Boots, r.Doors.Find(d => d.Room == r25).Blueprint);
            var done = new Sim(5); done.DevProjects(Gear.Boots, Balance.GearBlueprints);
            r = done.DevStartAt(m22, "R2-4");
            Assert.AreEqual(Gear.None, r.Chest.Blueprint, "botas completas: o bau so paga moedas"); Assert.Greater(r.Chest.Coins, 0);
            Assert.AreEqual(Gear.None, r.Doors.Find(d => d.Room == r25).Blueprint, "a porta nao promete projeto que nao serve");
        }

        /// <summary>Fim de jogo (raia E2): os niveis 6-8 do equipamento so abrem depois do Dragao, custam cada vez mais, dao ganho pequeno (cada
        /// tabela por nivel anda no maximo o que andou do 4 ao 5) e nenhum deixa a carga indestrutivel (a reliquia quebra com qualquer peca no 8).</summary>
        [Test]
        public void Equipamento_Niveis6a8_SoDepoisDoDragao_CustoCrescente_SemCargaIndestrutivel()
        {
            int boss = RegionDef.Of(4).BossAt;
            Sim a = Sim.Load("v=3\nseed=3\nrep=" + boss + "\ncoins=500000\n"); a.DevProjects(Gear.Hook, Balance.GearBlueprints);
            Assert.IsFalse(a.DragonBeaten); Assert.AreEqual(Balance.GearCampaignLevel, a.GearCap);
            while (a.UpgradeGear(Gear.Hook)) { }
            Assert.AreEqual(Balance.GearCampaignLevel, a.GearLevel(Gear.Hook), "antes do Dragao o teto e o 5"); Assert.AreEqual(-1, a.GearCost(Gear.Hook));
            Sim b = Sim.Load("v=3\nseed=3\nrep=" + (boss + 1) + "\ncoins=500000\n"); b.DevProjects(Gear.Hook, Balance.GearBlueprints);
            Assert.IsTrue(b.DragonBeaten, "M4-16 entregue"); Assert.AreEqual(Balance.GearMaxLevel, b.GearCap);
            int prev = 0;
            for (int lv = 1; lv < Balance.GearMaxLevel; lv++) { int c = b.GearCost(Gear.Hook); Assert.Greater(c, prev, "custo crescente no nivel " + (lv + 1)); prev = c; Assert.IsTrue(b.UpgradeGear(Gear.Hook)); }
            Assert.AreEqual(Balance.GearMaxLevel, b.GearLevel(Gear.Hook)); Assert.AreEqual(-1, b.GearCost(Gear.Hook));
            Assert.AreEqual(Balance.GearMaxLevel, Sim.Load(b.Save()).GearLevel(Gear.Hook), "o save guarda o 8");

            float[][] f = { GearDef.SmokeTime, GearDef.TrapTime, GearDef.BootsFire };
            foreach (float[] t in f)
            {
                Assert.AreEqual(Balance.GearMaxLevel, t.Length);
                float step = 0f; for (int i = 1; i < Balance.GearCampaignLevel; i++) step = System.Math.Max(step, t[i] - t[i - 1]);
                for (int i = Balance.GearCampaignLevel; i < Balance.GearMaxLevel; i++) Assert.That(t[i] - t[i - 1], Is.InRange(0f, step + 1e-4f), "ganho pequeno: no maximo o maior passo da campanha");
            }
            Assert.AreEqual(Balance.GearMaxLevel, GearDef.HookCooldown.Length); for (int i = 5; i < 8; i++) Assert.LessOrEqual(GearDef.HookCooldown[i - 1] - GearDef.HookCooldown[i], 1f + 1e-4f);
            for (int i = 5; i < 8; i++) { Assert.LessOrEqual(GearDef.SmokeUses[i] - GearDef.SmokeUses[i - 1], 1); Assert.LessOrEqual(GearDef.TrapCatches[i] - GearDef.TrapCatches[i - 1], 1); }

            for (int g = 1; g < GearDef.All.Length; g++)
            {
                Sim sim = Sim.Load("v=3\nseed=7\nrep=24\ngear=" + g + ",0,3,3,3,3,0,8,8,8,8\n");
                Run r = Start(sim, 4);   // C5: reliquia
                Assert.AreEqual((Gear)g, r.Gear); Assert.AreEqual(Balance.GearMaxLevel, r.GearLevel);
                r.Courier.Hp = 99;
                Steps(sim, 3, Up); Steps(sim, 6, V2.Zero);
                Enemy brute = r.Spawn(EnemyType.Brute, r.Main.Pos + new V2(0f, 1f)); brute.Hp = 999;
                for (int i = 0; i < StepsFor(30f) && sim.InContract; i++) Steps(sim, 1, V2.Zero);
                Assert.AreEqual(Outcome.CargoBroken, sim.LastResult?.Outcome, GearDef.All[g].Name + " no 8: a reliquia ainda quebra");
            }
        }

        /// <summary>RerolledToday separa "ja trocou hoje" (do dia e do raro, cada um) de "falta moeda" quando CanReroll e falso.</summary>
        [Test]
        public void Reroll_RerolledToday_SeparaJaTrocouDeFaltaMoeda()
        {
            int d = 20261010;
            var sim = new Sim(6) { Reputation = Balance.OpenAfter, Coins = 1000 };
            sim.OpenSession(T0, d); sim.OpenSession(T0 + 9 * 3600, d);   // 8 h fora: raro oferecido
            Assert.IsTrue(sim.RareAvailable);
            Assert.IsFalse(sim.RerolledToday(false)); Assert.IsFalse(sim.RerolledToday(true));
            Assert.IsTrue(sim.Reroll(false));
            Assert.IsTrue(sim.RerolledToday(false), "trocou o do dia"); Assert.IsFalse(sim.RerolledToday(true), "o raro conta separado");
            Assert.IsFalse(sim.CanReroll(false)); Assert.GreaterOrEqual(sim.Coins, sim.RerollCost, "tem moeda: o falso e por ja ter trocado");
            Assert.IsTrue(sim.Reroll(true)); Assert.IsTrue(sim.RerolledToday(true));
            Sim b = Sim.Load(sim.Save());
            Assert.IsTrue(b.RerolledToday(false)); Assert.IsTrue(b.RerolledToday(true));
            sim.SeeDate(20261011);
            Assert.IsFalse(sim.RerolledToday(false), "dia novo"); Assert.IsFalse(sim.RerolledToday(true));
            var poor = new Sim(6) { Reputation = Balance.OpenAfter, Coins = 10 }; poor.SeeDate(d);
            Assert.IsFalse(poor.CanReroll(false)); Assert.IsFalse(poor.RerolledToday(false), "falso por falta de moeda, nao por ter trocado");
            Assert.IsFalse(new Sim(6).RerolledToday(false), "sem data vista");
        }

        /// <summary>WeekProject(data) mostra antes o premio das 3 vagas do evento: o equipamento da semana, outro que falte se ele ja esta
        /// completo, ou nenhum (Balance.EventProjectCoins moedas) com os 4 completos.</summary>
        [Test]
        public void Evento_WeekProject_MostraAntesOPremioDasTresVagas()
        {
            int d = Monday(20261012);
            var sim = new Sim(4) { Reputation = Balance.OpenAfter }; sim.SeeDate(d);
            Gear shown = sim.WeekProject(d);
            Assert.AreEqual((Gear)(1 + Sim.Week(d) % 4), shown);
            Assert.AreEqual(shown, sim.WeekProject(AddDays(d, 6)), "mesma semana");
            ContractResult res = null;
            for (int k = 0; k < 3; k++) { Assert.IsNotNull(sim.StartEvent(d, k, false)); res = DeliverFrom(sim); }
            Assert.AreEqual(shown, res.EventProject, "o premio e o que a tela mostrou");

            var full = new Sim(4) { Reputation = Balance.OpenAfter }; full.SeeDate(d);
            full.DevProjects(shown, Balance.GearBlueprints);
            Gear other = full.WeekProject(d);
            Assert.AreNotEqual(shown, other); Assert.AreNotEqual(Gear.None, other, "outro que ainda falta");
            full.DevProjects(Gear.None, Balance.GearBlueprints);
            Assert.AreEqual(Gear.None, full.WeekProject(d));
            for (int k = 0; k < 3; k++) { full.StartEvent(d, k, false); res = DeliverFrom(full); }
            Assert.AreEqual(Gear.None, res.EventProject);
            Assert.AreEqual(Sim.EventReward(2) + Balance.EventProjectCoins, res.EventReward, "nenhum projeto faltando: moedas");
        }

        /// <summary>DevProjects (so teste/fotos, como o DevStars): projetos so na memoria, sem evento nem Dirty; equipado que perde o projeto sai.</summary>
        [Test]
        public void DevProjects_SoNaMemoria_SemEventoNemDirty()
        {
            var s = new Sim(3) { Reputation = 24 };
            s.Dirty = false; s.Events.Clear();
            s.DevProjects(Gear.Hook, 3);
            Assert.IsTrue(s.GearUnlocked(Gear.Hook)); Assert.AreEqual(1, s.GearLevel(Gear.Hook)); Assert.AreEqual(0, s.Blueprints(Gear.Smoke));
            Assert.IsFalse(s.Dirty); Assert.AreEqual(0, s.Events.Count);
            Assert.IsTrue(s.EquipGear(Gear.Hook));
            s.DevProjects(Gear.Hook, 1);
            Assert.IsFalse(s.GearUnlocked(Gear.Hook)); Assert.AreEqual(Gear.None, s.GearEquipped, "equipado que perde o projeto sai do espaco");
            s.DevProjects(Gear.None, 99);
            for (int g = 1; g < GearDef.All.Length; g++) Assert.AreEqual(Balance.GearBlueprints, s.Blueprints((Gear)g), "Gear.None = os 4, no maximo 3");
            Assert.IsNotNull(s.StartContract(0, false), "contrato com o equipamento destravado pelo atalho");
        }

        /// <summary>A loja vende 6 roupas (Noturno, Real, Explorador, Bombeiro de forja, Mineiro, Coveiro), cada uma por um Product de conteudo
        /// fixo; a Classica e de graca e a Fantasma nao se compra. O save guarda as novas (bits depois do pacote de evento).</summary>
        [Test]
        public void Roupas_SeisAVendaNaLoja_ClassicaGratis_FantasmaSoPorMarco()
        {
            var sold = new List<Outfit>(); var all = new Sim(2);
            foreach (Product p in Enum.GetValues(typeof(Product)))
            {
                if (!p.ToString().StartsWith("Outfit", StringComparison.Ordinal)) continue;
                var s = new Sim(2);
                Assert.IsTrue(s.Purchase(p), p.ToString()); Assert.IsTrue(s.Owns(p)); Assert.IsFalse(s.Purchase(p), "restaurar nao concede de novo");
                for (int o = 1; o < OutfitDef.All.Length; o++) if (s.OwnsOutfit((Outfit)o)) sold.Add((Outfit)o);
                Assert.IsTrue(all.Purchase(p));
            }
            CollectionAssert.AreEquivalent(new[] { Outfit.Night, Outfit.Royal, Outfit.Explorer, Outfit.Forge, Outfit.Miner, Outfit.Gravekeeper }, sold);
            Assert.IsTrue(new Sim(2).OwnsOutfit(Outfit.Classic), "Classica de graca");
            Assert.IsFalse(sold.Contains(Outfit.Ghost), "Fantasma so por marco/temporada");
            Assert.AreEqual("loja", OutfitDef.All[(int)Outfit.Miner].Source); Assert.AreEqual("loja", OutfitDef.All[(int)Outfit.Gravekeeper].Source);
            Assert.IsTrue(all.EquipOutfit(Outfit.Gravekeeper));
            Sim b = Sim.Load(all.Save());
            Assert.IsTrue(b.Owns(Product.OutfitMiner)); Assert.IsTrue(b.Owns(Product.OutfitGravekeeper)); Assert.IsTrue(b.OwnsOutfit(Outfit.Miner));
            Assert.AreEqual(Outfit.Gravekeeper, b.OutfitEquipped); Assert.IsFalse(b.Owns(Product.EventPack), "o pacote de evento continua por semana");
            Assert.IsFalse(new Sim(2).Purchase((Product)99), "produto desconhecido");
        }

        /// <summary>DevStartAt (so teste/fotos, -room): comeca direto na camada da sala, sem jogar ate ela; o contrato roda dali.</summary>
        [Test]
        public void DevStartAt_ComecaDiretoNaSalaDoChefe()
        {
            var s = new Sim(9);
            Run r = s.DevStartAt(-1, " b2 ");
            Assert.IsNotNull(r); Assert.AreEqual("B2", r.Room.Code); Assert.AreEqual(ContractOf("M2-16"), r.Contract.Index, "1o contrato que passa pela B2");
            Assert.AreEqual(r.Contract.Layers.Length - 2, r.Layer, "camada do chefe");
            Assert.IsTrue(r.Enemies.Exists(e => e.Type == EnemyType.StoneGolem)); Assert.AreEqual(Balance.Entrance, r.Courier.Pos);
            Assert.IsTrue(Has(s.Events, Ev.RoomEnter));
            Steps(s, 30, Up);
            Assert.IsTrue(s.InContract); Assert.Greater(r.Time, 0f);
            Assert.IsNull(s.DevStartAt(-1, "M4"), "contrato em curso");
            s.Abandon();
            Run m = s.DevStartAt(ContractOf("M4-8"), "m4");
            Assert.AreEqual("M4", m.Room.Code); Assert.AreEqual(ContractOf("M4-8"), m.Contract.Index); s.Abandon();
            Assert.IsNull(new Sim(9).DevStartAt(ContractOf("M2-1"), "B2"), "sala fora do contrato");
            Assert.IsNull(new Sim(9).DevStartAt(-1, "X9")); Assert.IsNull(new Sim(9).DevStartAt(-1, null)); Assert.IsNull(new Sim(9).DevStartAt(Sim.Contracts.Length, "B2"));
        }
    }

    /// <summary>Economia da campanha inteira (docs/BALANCE.md, "Economia da campanha"): o bot joga C1..M4-16 numa rodada so, do zero, comprando
    /// como o Bot.PlayCampaign. Mede as moedas paradas na virada de cada regiao (depois das compras), no fim e "sem destino" (nada mais a
    /// comprar), e a duracao da regiao 2 a partir dos saves reais da campanha. Imprime as tabelas:
    /// dotnet test client/tools/coretests --filter EconomiaTests --logger "console;verbosity=detailed".</summary>
    public class EconomiaTests
    {
        public const int Seeds = 16, BaseSeed = 8000, R2Seeds = 5;
        static Bot.Campaign[] camps; static Bot.Stats[] r2;

        public static Bot.Campaign[] Full()
        {
            if (camps != null) return camps;
            camps = new Bot.Campaign[Seeds];
            for (int s = 0; s < Seeds; s++) camps[s] = Bot.PlayCampaign(BaseSeed + s, region: 4, maxAttempts: 400);
            return camps;
        }

        /// <summary>Tabela da regiao 2 a partir do save que cada campanha completa tinha antes da 1a tentativa de cada contrato.</summary>
        static Bot.Stats[] R2Table()
        {
            if (r2 != null) return r2;
            RegionDef g = RegionDef.Of(2);
            r2 = new Bot.Stats[g.Contracts.Length];
            for (int c = 0; c < r2.Length; c++) r2[c] = new Bot.Stats { Contract = g.First + c };
            for (int k = 0; k < Seeds; k++)
                for (int c = 0; c < r2.Length; c++) Bot.MeasureFrom(Full()[k].Before[g.First + c], g.First + c, R2Seeds, 1000 + 100 * k, 0.5f, r2[c]);
            return r2;
        }

        /// <summary>Moedas paradas ao fim da regiao `region` (1-4): na entrada da seguinte, depois das compras; na 4, o save do fim mais uma
        /// rodada de compras.</summary>
        public static Sim EndOf(Bot.Campaign cp, int region)
        {
            if (region < 4) return Sim.Load(cp.Before[RegionDef.Of(region + 1).First]);
            Sim s = Sim.Load(cp.End); Bot.BuyAll(s); return s;
        }

        /// <summary>Nada mais a comprar (guilda, armas e equipamento no maximo ou bloqueados): moeda parada de verdade.</summary>
        public static bool NothingToBuy(Sim s)
        {
            for (int u = 0; u < Balance.UpgradeLines; u++) if (s.UpgradeCost((Upgrade)u) >= 0) return false;
            for (int w = 0; w < WeaponDef.All.Length; w++) if (s.WeaponCost((Weapon)w) >= 0) return false;
            for (int g = 1; g < GearDef.All.Length; g++) if (s.GearCost((Gear)g) >= 0) return false;
            return true;
        }

        /// <summary>Por regiao: media (entre as campanhas) das moedas no fim, maior moeda guardada antes de um contrato (pico) e maior moeda
        /// guardada sem nada para comprar (sem destino).</summary>
        static void Idle(Bot.Campaign[] cs, RegionDef reg, out double end, out double peak, out double stuck, out int endMax)
        {
            end = peak = stuck = 0; endMax = 0;
            foreach (Bot.Campaign cp in cs)
            {
                int p = 0, k = 0;
                for (int c = reg.First; c < reg.First + reg.Contracts.Length; c++)
                {
                    Sim s = Sim.Load(cp.Before[c]);
                    p = Math.Max(p, s.Coins);
                    if (NothingToBuy(s)) k = Math.Max(k, s.Coins);
                }
                Sim e = EndOf(cp, reg.Id);
                if (NothingToBuy(e)) k = Math.Max(k, e.Coins);
                end += e.Coins; endMax = Math.Max(endMax, e.Coins); peak += Math.Max(p, e.Coins); stuck += k;
            }
            end /= cs.Length; peak /= cs.Length; stuck /= cs.Length;
        }

        static string Report(Bot.Campaign[] cs)
        {
            var ci = CultureInfo.InvariantCulture; var sb = new StringBuilder();
            sb.AppendLine("| Região | tentativas | tempo do bot (min) | ganhas | gastas | paradas no fim (média / máx) | pico guardado | parado sem destino | guilda | armas | equipamento (nível) |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (RegionDef reg in RegionDef.All)
            {
                double att = 0, t = 0, earn = 0, spent = 0;
                foreach (Bot.Campaign cp in cs)
                {
                    int last = reg.First + reg.Contracts.Length - 1;
                    for (int c = reg.First; c <= last; c++) { att += cp.AttemptsPer[c]; earn += cp.EarnedPer[c]; spent += cp.SpentPer[c]; }
                    if (reg.Id == 4) spent += Sim.Load(cp.End).Coins - EndOf(cp, 4).Coins;   // a ultima rodada de compras
                    t += cp.ClockAt[last] - (reg.First > 0 ? cp.ClockAt[reg.First - 1] : 0f);
                }
                Idle(cs, reg, out double end, out double peak, out double stuck, out int endMax);
                Sim e = EndOf(cs[0], reg.Id);
                string gear = "";
                for (int g = 1; g < GearDef.All.Length; g++) gear += (g > 1 ? " · " : "") + GearDef.All[g].Name.Split(' ')[0] + " " + e.GearLevel((Gear)g);
                double n = cs.Length;
                sb.AppendLine(string.Format(ci, "| {0} | {1:0.0} | {2:0.0} | {3:0} | {4:0} | {5:0} / {6} | {7:0} | {8:0} | {9} | {10}/{11}/{12} | {13} |", reg.Id, att / n, t / n / 60f, earn / n,
                    spent / n, end, endMax, peak, stuck, string.Join("/", e.Levels), e.WeaponLevel(Weapon.Sling), e.WeaponLevel(Weapon.Boomerang), e.WeaponLevel(Weapon.Bomb), gear));
            }
            double fin = 0, at = 0, tm = 0, st = 0, de = 0;
            foreach (Bot.Campaign cp in cs) { if (cp.Finished) fin++; at += cp.Attempts; tm += cp.Time; st += cp.Stars; de += cp.Deaths; }
            sb.AppendLine(string.Format(ci, "Campanha completa ({0} sementes de {1}, do zero): fecha {2:0}% · tentativas {3:0.0} · mortes {4:0.0} · tempo do bot {5:0.0} min · estrelas {6:0.0}/216",
                cs.Length, BaseSeed, 100 * fin / cs.Length, at / cs.Length, de / cs.Length, tm / cs.Length / 60f, st / cs.Length));
            var seeds = new StringBuilder("Por semente (moedas no fim · projetos fumaça/gancho/armadilha/botas):");
            for (int i = 0; i < cs.Length; i++)
            {
                Sim e = EndOf(cs[i], 4);
                seeds.Append(string.Format(ci, " {0}={1}·{2}/{3}/{4}/{5}", BaseSeed + i, e.Coins, e.Blueprints(Gear.Smoke), e.Blueprints(Gear.Hook), e.Blueprints(Gear.Trap), e.Blueprints(Gear.Boots)));
            }
            sb.AppendLine(seeds.ToString());
            return sb.ToString();
        }

        static double R2Mean(Bot.Stats[] t)
        {
            RegionDef g = RegionDef.Of(2); double sum = 0; int n = 0;
            for (int c = 3; c < t.Length; c++) if (g.Contracts[c].Cargo != CargoType.Message) { sum += t[c].AvgTime; n++; }
            return sum / n;
        }

        /// <summary>A campanha completa (72 contratos) pelo bot, do zero numa rodada so: fecha, e as moedas paradas ficam no alvo do desenho
        /// (no maximo 1 nivel de equipamento: o nivel 5 custa 5.000) no fim de cada regiao e sem nada para comprar.</summary>
        [Test]
        public void CampanhaCompleta_C1aM4_16_PeloBot_FechaESobraNoMaximo1NivelDeEquipamento()
        {
            Bot.Campaign[] cs = Full();
            TestContext.WriteLine(Report(cs));
            for (int s = 0; s < cs.Length; s++) Assert.IsTrue(cs[s].Finished, "o bot fecha os 72 do zero (semente " + (BaseSeed + s) + ")");
            int level5 = GearDef.All[(int)Gear.Hook].Cost[Balance.GearCampaignLevel - 1];
            foreach (RegionDef reg in RegionDef.All)
            {
                Idle(cs, reg, out double end, out double peak, out double stuck, out int endMax);
                Assert.LessOrEqual(end, 3000.0, "regiao " + reg.Id + ": moedas paradas no fim (media)");
                Assert.LessOrEqual(stuck, 3000.0, "regiao " + reg.Id + ": moedas sem nada para comprar (media do pico)");
                Assert.LessOrEqual(peak, level5, "regiao " + reg.Id + ": pico guardado acima de 1 nivel de equipamento");
            }
        }

        /// <summary>Regiao 2 a partir dos saves reais da campanha completa: do 4o contrato em diante (sem a mensagem) a media fica em 90-180 s
        /// (alvo do bot) e nenhum contrato fica inviavel.</summary>
        [Test]
        public void Regiao2_DoQuartoEmDiante_Media90a180s_SemQuebrarAEntrega()
        {
            Bot.Stats[] t = R2Table(); RegionDef g = RegionDef.Of(2);
            var ci = CultureInfo.InvariantCulture; var sb = new StringBuilder("| Contrato | Carga | Salas | % concluído | % morte | % falha de carga | duração média (s) | mediana (s) | estrelas | 3 estrelas % |\n|---|---|---|---|---|---|---|---|---|---|\n");
            for (int c = 0; c < t.Length; c++)
            {
                Bot.Stats s = t[c]; ContractDef d = g.Contracts[c];
                sb.AppendLine(string.Format(ci, "| {0} | {1} | {2} | {3:0} | {4:0} | {5:0} | {6:0} | {7:0} | {8:0.00} | {9:0} |", d.Code, CargoDef.All[(int)d.Cargo].Name, d.Rooms,
                    s.Pct(s.Delivered), s.Pct(s.Died), s.Pct(s.CargoFail), s.AvgTime, s.MedianTime, s.AvgStars, s.Pct(s.ThreeStars)));
                Assert.AreEqual(0, s.Timeout, d.Code + ": bot preso");
                Assert.GreaterOrEqual(s.Pct(s.Delivered), 40.0, d.Code + ": quase ninguem entrega");
            }
            double mean = R2Mean(t);
            sb.AppendLine(string.Format(ci, "Media M2-4..M2-16 sem a mensagem: {0:0} s ({1} campanhas x {2} sementes por contrato)", mean, Seeds, R2Seeds));
            TestContext.WriteLine(sb.ToString());
            Assert.That(mean, Is.InRange(90.0, 180.0), "regiao 2: media do 4o contrato em diante (alvo do bot)");
        }

        // ------------------------------------------------------------------ fim de jogo (depois do Dragao)

        /// <summary>Fontes de moeda do fim de jogo, na ordem do relatorio.</summary>
        public static readonly string[] Sources = { "marcos", "bau do dia", "dia", "raro", "evento", "sem fim", "temporada" };
        public const int EndDays = 56, EndSeeds = 4;
        const long EndT0 = 1800000000;

        /// <summary>Um jogador diario depois da campanha (o bot, partindo do fim de uma campanha completa): 1 sessao por dia (o raro aparece, 8 h
        /// fora), resgata marcos e temporada, abre o bau do dia (rewarded), joga o contrato do dia, o raro, as 3 vagas do evento da semana (ate 2
        /// tentativas por dia cada) e 1 desafio sem fim; compra como a campanha (Bot.BuyAll). Sem reroll, sem modificador escolhido, sem o
        /// rewarded de dobrar bonus. Income[d, s] = moedas do dia d pela fonte s; Levels[d] = soma dos niveis dos 4 equipamentos no fim do dia d.</summary>
        public sealed class Endgame { public readonly double[,] Income = new double[EndDays, Sources.Length]; public readonly int[] Levels = new int[EndDays], Coins = new int[EndDays]; }

        public static Endgame PlayEndgame(string from, int seed)
        {
            var eg = new Endgame(); Sim sim = Sim.Load(from); sim.Seed = seed; var bot = new Bot(seed * 7 + 3);
            for (int d = 0; d < EndDays; d++)
            {
                DateTime day = new DateTime(2026, 10, 12).AddDays(d);
                int date = day.Year * 10000 + day.Month * 100 + day.Day; long now = EndT0 + d * 86400L;
                sim.OpenSession(now, date);
                int c0 = sim.Coins;
                for (int i = 0; i < Balance.Milestones.Length; i++) sim.ClaimMilestone(i);
                eg.Income[d, 0] += sim.Coins - c0; c0 = sim.Coins;
                if (sim.GrantReward(Reward.DailyChest)) eg.Income[d, 1] += sim.Coins - c0;
                for (int t = 0; t < 3 && sim.DailyAvailable(date); t++) eg.Income[d, 2] += Play(sim, bot, sim.DailyFor(date), () => sim.StartDaily(date, sim.Pending != null));
                for (int t = 0; t < 3 && sim.RareAvailable; t++) eg.Income[d, 3] += Play(sim, bot, sim.RareContract, () => sim.StartRare(sim.Pending != null));
                for (int k = 0; k < sim.EventSlots(date); k++)
                    for (int t = 0; t < 2 && sim.EventAvailable(date, k); t++) eg.Income[d, 4] += Play(sim, bot, sim.EventContract(date, k), () => sim.StartEvent(date, k, false));
                if (sim.EndlessOpen) eg.Income[d, 5] += Play(sim, bot, sim.EndlessContract(date), () => sim.StartEndless(date));
                c0 = sim.Coins;
                for (int i = 0; i < Season.Goals.Length; i++) sim.ClaimSeason(i);
                eg.Income[d, 6] += sim.Coins - c0;
                sim.Touch(now + 3600);
                Bot.BuyAll(sim);
                for (int g = 1; g < GearDef.All.Length; g++) eg.Levels[d] += sim.GearLevel((Gear)g);
                eg.Coins[d] = sim.Coins;
            }
            return eg;
        }

        static int Play(Sim sim, Bot bot, ContractDef def, Func<Run> start)
        {
            int c0 = sim.Coins;
            if (def != null) sim.Equip(Bot.WeaponFor(sim, def));
            if (start() == null) return 0;
            Bot.Finish(sim, bot);
            return sim.Coins - c0;
        }

        static Endgame[] ends;
        public static Endgame[] Ends()
        {
            if (ends != null) return ends;
            ends = new Endgame[EndSeeds];
            for (int s = 0; s < EndSeeds; s++) { Sim e = EndOf(Full()[s], 4); ends[s] = PlayEndgame(e.Save(), BaseSeed + s); }
            return ends;
        }

        /// <summary>Dia (1-based) em que os 4 equipamentos chegam ao nivel 8, ou 0 se nao chegam nos EndDays dias.</summary>
        static int DayFull(Endgame e) { for (int d = 0; d < EndDays; d++) if (e.Levels[d] >= 4 * Balance.GearMaxLevel) return d + 1; return 0; }

        /// <summary>Fim de jogo pelo bot: os niveis 6-8 do equipamento so abrem depois do Dragao e sao o ralo do pos-campanha (sem fim, eventos,
        /// temporada e bau do dia). Alvo (BALANCE.md, "Economia da campanha"): o jogador diario leva mais de 1 temporada (28 dias) e menos de
        /// 2 (56) para fechar os 4 no nivel 8, e ate la nao fica sem ter o que comprar.</summary>
        [Test]
        public void FimDeJogo_Niveis6a8DepoisDoDragao_DuramEntreUmaEDuasTemporadas()
        {
            Endgame[] es = Ends();
            var ci = CultureInfo.InvariantCulture; var sb = new StringBuilder();
            sb.AppendLine(string.Format(ci, "Fim de jogo ({0} jogadores diarios do fim de campanhas completas, {1} dias): moedas por dia, media dos dias 1-28 / 29-56", es.Length, EndDays));
            sb.AppendLine("| Fonte | dias 1-28 | dias 29-56 |\n|---|---|---|");
            double tot1 = 0, tot2 = 0;
            for (int s = 0; s < Sources.Length; s++)
            {
                double a = 0, b = 0;
                foreach (Endgame e in es) for (int d = 0; d < EndDays; d++) { if (d < 28) a += e.Income[d, s]; else b += e.Income[d, s]; }
                a /= es.Length * 28.0; b /= es.Length * 28.0; tot1 += a; tot2 += b;
                sb.AppendLine(string.Format(ci, "| {0} | {1:0} | {2:0} |", Sources[s], a, b));
            }
            sb.AppendLine(string.Format(ci, "| **total** | **{0:0}** | **{1:0}** |", tot1, tot2));
            var full = new StringBuilder("Dia em que os 4 equipamentos chegam ao 8 (0 = nao chegou) · niveis somados nos dias 7/14/28/56 · moedas no dia 56:");
            foreach (Endgame e in es) full.Append(string.Format(ci, " {0}·{1}/{2}/{3}/{4}·{5}", DayFull(e), e.Levels[6], e.Levels[13], e.Levels[27], e.Levels[EndDays - 1], e.Coins[EndDays - 1]));
            sb.AppendLine(full.ToString());
            int sink = 0; for (int lv = Balance.GearCampaignLevel; lv < Balance.GearMaxLevel; lv++) sink += GearDef.All[(int)Gear.Hook].Cost[lv];
            sb.AppendLine(string.Format(ci, "Ralo do fim de jogo: {0} por equipamento, {1} nos 4", sink, 4 * sink));
            TestContext.WriteLine(sb.ToString());
            foreach (Endgame e in es)
            {
                int day = DayFull(e);
                Assert.That(day == 0 ? int.MaxValue : day, Is.GreaterThan(28), "fecha os niveis 6-8 em menos de 1 temporada: ralo curto");
                Assert.That(day, Is.InRange(1, EndDays), "nao fecha os niveis 6-8 em 2 temporadas: ralo longo demais");
                for (int d = 0; d < day - 1; d++) Assert.Less(e.Coins[d], GearDef.All[(int)Gear.Hook].Cost[Balance.GearMaxLevel - 1], "dia " + (d + 1) + ": moeda parada acima do nivel mais caro");
            }
        }
    }
}
