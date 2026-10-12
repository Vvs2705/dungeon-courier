using System.Globalization;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Revisao do save v3, da migracao e das datas (2026-10-12): cada teste e um bug comprovado. So API publica.</summary>
    public class CoreTestsRevisaoSave
    {
        static int AddDays(int date, int n)
        {
            var t = new System.DateTime(date / 10000, date / 100 % 100, date % 100).AddDays(n);
            return t.Year * 10000 + t.Month * 100 + t.Day;
        }

        static void Die(Sim sim)
        {
            Run r = sim.Run;
            r.Enemies.Clear();
            r.Spawn(EnemyType.Bat, r.Courier.Pos);
            for (int i = 0; i < 300 && sim.InContract; i++) { r.Courier.Hp = 1; sim.Tick(Balance.Step, V2.Zero, false); }
            Assert.IsFalse(sim.InContract, "nao morreu");
        }

        /// <summary>Dia da beta (v2) em curso com o payload de bits altos: a migracao o regenera na regiao 1, mas o save v3 so guardava o
        /// payload; ao reabrir de novo (e no checkpoint do rewarded "retomar") a regiao vinha dos bits altos, a sala gravada nao estava no
        /// contrato e o contrato em curso sumia.</summary>
        [Test]
        public void SaveV2_DiaDaBetaEmCurso_ContinuaDepoisDeRegravarEmV3_EORetomarFunciona()
        {
            int d = 20261010;
            while (Content.RegionOf(Sim.DailyPayload(d)) == 1) d = AddDays(d, 1);
            int p = Sim.DailyPayload(d);
            ContractDef beta = Content.Generate(p, ContractKind.Daily, d, 1);
            int room = beta.Layers[0][0];
            string v2 = "v=2\nseed=4\nruns=3\nnid=4\nrep=3\nstars=111\ndseen=" + d + "\nrun=-1,0," + room + ",6,3,77,0,0,0,0,0,0,0,0\nrk=1," + p + "," + d +
                        ",0,0,-1,-1,-1\ncg0=3," + (int)beta.Cargo + ",0,0,0\n";
            Sim s = Sim.Load(v2);
            Assert.IsTrue(s.InContract, "dia da beta em curso");

            Sim b = Sim.Load(s.Save());
            Assert.IsTrue(b.InContract, "regravado em v3, o dia da beta continua em curso ao reabrir");
            Assert.AreEqual(room, b.Run.Room.Id); Assert.AreEqual(1, b.Run.Contract.Region);
            Assert.AreEqual(string.Join("/", System.Array.ConvertAll(beta.Layers, l => string.Join(".", l))),
                            string.Join("/", System.Array.ConvertAll(b.Run.Contract.Layers, l => string.Join(".", l))));

            b.OpenSession(1800000000, d); b.OpenSession(1800000100, d);   // retomar nunca na 1a sessao
            Die(b);
            Assert.IsTrue(b.RewardOffer(Reward.Resume));
            Assert.IsTrue(b.GrantReward(Reward.Resume), "o checkpoint do dia migrado volta (anuncio assistido nao fica sem premio)");
            Assert.AreEqual(room, b.Run.Room.Id);
        }

        /// <summary>Raro trocado (reroll pago) e ainda nao jogado: mais 8 h fora no MESMO dia local reofertava o raro "novo" sorteado pela mesma
        /// data, ou seja, o original de antes do reroll, e o TROCAR ja estava gasto no dia.</summary>
        [Test]
        public void ContratoRaro_RerolladoENaoJogado_NaoVoltaAoOriginal_8hDepoisNoMesmoDia()
        {
            var sim = new Sim(6) { Reputation = Balance.OpenAfter, Coins = 1000 };
            const int d = 20261010; const long t0 = 1800000000;
            sim.OpenSession(t0, d); sim.OpenSession(t0 + 9 * 3600, d);   // 00:10 e 09:10: raro oferecido
            Assert.IsTrue(sim.RareAvailable);
            Assert.IsTrue(sim.Reroll(true));
            string rolled = sim.RareContract.ShareCode;
            sim.OpenSession(t0 + 18 * 3600, d);   // 18:10 do mesmo dia: mais 8 h fora
            Assert.IsTrue(sim.RareAvailable);
            Assert.AreEqual(rolled, sim.RareContract.ShareCode, "o reroll pago nao e desfeito");
        }

        /// <summary>Save escrito com cultura cujo sinal de menos nao e '-' (sv/nb/fi = U+2212, ar = U+061C...): Append(int) usa a cultura
        /// atual e o Load le invariante. A carga pendente de contrato gerado (Contract = -1) voltava como C1.</summary>
        [Test]
        public void Save_CulturaComOutroSinalDeMenos_GravaInvariante_ECargaDoGeradoNaoViraC1()
        {
            CultureInfo old = CultureInfo.CurrentCulture;
            try
            {
                int r2 = RegionDef.Of(2).Normal[0];   // pendente na regiao 2: o dia da regiao 1 nao a consome
                Sim s = Sim.Load("v=3\nseed=7\nrep=3\ndseen=20261012\npend=5,0,0," + r2 + ",50,-1\n");
                Assert.AreEqual(-1, s.Pending.Contract);
                Assert.IsNotNull(s.StartDaily(20261012, false));   // run=-1,... e oferta -1 no contrato em curso
                Assert.IsNotNull(s.Pending);
                CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
                string a = s.Save();
                StringAssert.DoesNotContain("−", a);
                CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                Assert.AreEqual(a, s.Save(), "o texto do save nao depende da cultura do aparelho");
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Sim b = Sim.Load(a);
                Assert.IsTrue(b.InContract);
                Assert.AreEqual(-1, b.Pending.Contract, "carga do contrato gerado nao vira 'do C1'");
            }
            finally { CultureInfo.CurrentCulture = old; }
        }
    }
}
