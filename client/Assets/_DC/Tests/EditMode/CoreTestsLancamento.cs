using System.Collections.Generic;
using System.Globalization;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Regras do lancamento (CONTRATO_LANCAMENTO §2-§7, CONTEUDO_REGIOES §0-§10 no que e do nucleo): multirregiao, inimigos e
    /// cargas novos no padrao, terreno comum, perks novos, modificadores, eventos, temporada, raro, rerolls, equipamento, maestria,
    /// marcos, sem fim, rewarded, interstitial, IAP, remote config, telemetria e save v3. So API publica.</summary>
    public partial class CoreTests
    {
        // ------------------------------------------------------------------ helpers do lancamento

        const long T0 = 1800000000;   // segundos Unix de exemplo (a View passa o relogio)

        static string Fp(ContractDef d)
        {
            var sb = new System.Text.StringBuilder(d.Cargo + "|" + d.ShareCode + "|");
            foreach (int[] l in d.Layers) sb.Append(string.Join(".", l)).Append('/');
            return sb.Append('|').Append(d.Tier).Append('|').Append(d.ParTime.ToString(CultureInfo.InvariantCulture)).ToString();
        }

        static int AddDays(int date, int n)
        {
            var t = new System.DateTime(date / 10000, date / 100 % 100, date % 100).AddDays(n);
            return t.Year * 10000 + t.Month * 100 + t.Day;
        }

        static int Monday(int date)
        {
            var t = new System.DateTime(date / 10000, date / 100 % 100, date % 100);
            return AddDays(date, -(((int)t.DayOfWeek + 6) % 7));
        }

        /// <summary>As `n` primeiras segundas-feiras (a partir de `from`) cuja semana tem o template `ev`.</summary>
        static List<int> WeeksOf(LiveEvent ev, int n, int from = 20261012)
        {
            var l = new List<int>();
            for (int w = 0; l.Count < n && w < 400; w++) { int d = AddDays(Monday(from), 7 * w); if (Sim.EventOf(d) == ev) l.Add(d); }
            Assert.AreEqual(n, l.Count);
            return l;
        }

        /// <summary>Passa pela porta `door` da sala atual (sem inimigos), escolhendo perk quando houver.</summary>
        static void Cross(Sim sim, int door = 0)
        {
            Run r = sim.Run;
            if (r.PerkOffer != null) sim.PickPerk(0);
            int layer = r.Layer;
            r.Courier.Pos = new V2(r.Doors[System.Math.Min(door, r.Doors.Count - 1)].Pos.X, 13.4f);
            for (int i = 0; i < 150 && r.Layer == layer && sim.InContract; i++) { r.Enemies.Clear(); Steps(sim, 1, Up); }
            Assert.Greater(r.Layer, layer, "nao passou pela porta em " + r.Room.Code);
            if (r.PerkOffer != null) sim.PickPerk(0);
        }

        static void Die(Sim sim)
        {
            Run r = sim.Run;
            r.Enemies.Clear(); r.Courier.Hp = 1;
            r.Spawn(EnemyType.Bat, r.Courier.Pos);
            Steps(sim, 2 + StepsFor(Balance.TouchGuard), V2.Zero);   // toques nao empilham (o Segundo folego acaba de levar um)
            Assert.IsFalse(sim.InContract, "nao morreu");
        }

        /// <summary>Contrato da campanha retomado pelo save na sala `room`, com perks (o save aplica os perks antes de montar a sala).</summary>
        static Sim InRoomWith(int contract, int layer, int room, CargoType cargo, params Perk[] perks)
        {
            int mask = 0; foreach (Perk p in perks) mask |= 1 << (int)p;
            Sim s = Sim.Load("v=3\nseed=7\nrep=24\nrun=" + contract + "," + layer + "," + room + ",20,0,99,0,0,0,0,0,0,0,0\nrk=0,0,0," + mask + ",0,-1,-1,-1\ncg0=1," + (int)cargo + ",0,0,0\n");
            Assert.IsNotNull(s.Run, "save invalido para a sala " + room);
            return s;
        }

        /// <summary>Sim com o equipamento `g` destravado no nivel lv e equipado (pelo save: blueprints e niveis).</summary>
        static Sim Geared(Gear g, int lv = 1) =>
            Sim.Load("v=3\nseed=7\nrep=24\ngear=" + (int)g + ",0,3,3,3,3,0," + lv + "," + lv + "," + lv + "," + lv + "\n");

        // ------------------------------------------------------------------ multirregiao

        [Test]
        public void Regioes_Registro_Regiao1IntactaNaFrente_EsqueletosFechadosSemQuebrar()
        {
            Assert.AreEqual(4, RegionDef.All.Length);
            for (int i = 0; i < Content.Rooms.Length; i++) Assert.AreEqual(i, Content.Rooms[i].Id, "id global = indice");
            for (int i = 0; i < Content.Contracts.Length; i++)
            {
                ContractDef c = Content.Contracts[i];
                Assert.AreEqual(i, c.Index);
                Assert.IsTrue(Content.Rooms[c.Layers[c.Layers.Length - 1][0]].Delivery, c.Code + ": termina na entrega");
                foreach (int[] l in c.Layers) foreach (int id in l) Assert.AreEqual(c.Region, Content.Rooms[id].Region, c.Code + ": sala de outra regiao");
            }
            RegionDef r1 = RegionDef.Of(1);
            Assert.AreEqual(Regiao1.Name, r1.Name); Assert.AreEqual("poroes", r1.Theme); Assert.AreEqual(0, r1.First);
            Assert.AreEqual(21, r1.Rooms.Length); Assert.AreEqual(24, r1.Contracts.Length); Assert.IsTrue(r1.HasContent);
            Assert.AreEqual(Content.M1, r1.Mini); Assert.AreEqual(Content.B1, r1.Boss); Assert.AreEqual(Content.D1, r1.Delivery);
            Assert.AreEqual(11, r1.MiniAt); Assert.AreEqual(23, r1.BossAt);
            CollectionAssert.AreEqual(new[] { Content.S1, Content.S2 }, r1.Secrets);
            Assert.AreEqual("R1", Content.Rooms[Content.R1].Code); Assert.AreEqual("B1", Content.Rooms[6].Code); Assert.AreEqual("D1", Content.Rooms[7].Code);
            Assert.AreEqual("R7", Content.Rooms[8].Code); Assert.AreEqual("M1", Content.Rooms[20].Code);
            int[] cargos = { 7, 10, 13, 15 }, stars = { 0, 30, 60, 95 };
            for (int r = 1; r <= 4; r++) { Assert.AreEqual(cargos[r - 1], RegionDef.Of(r).CargoCount); Assert.AreEqual(stars[r - 1], RegionDef.Of(r).Stars); }
            var open = new List<int> { 1 };
            for (int r = 2; r <= 4; r++)   // Leva B: regiao com conteudo = 14 salas (M, B, D) e 16 contratos; sem conteudo fica fechada
            {
                RegionDef g = RegionDef.Of(r);
                if (!g.HasContent) continue;
                open.Add(r);
                Assert.AreEqual(14, g.Rooms.Length, g.Name); Assert.AreEqual(16, g.Contracts.Length, g.Name);
                Assert.GreaterOrEqual(g.Mini, 0); Assert.GreaterOrEqual(g.Boss, 0); Assert.GreaterOrEqual(g.Delivery, 0);
            }
            Assert.IsNull(RegionDef.Of(0)); Assert.IsNull(RegionDef.Of(5));

            var sim = new Sim(1) { Reputation = Sim.Contracts.Length };
            sim.DevStars(999);
            Assert.IsTrue(sim.RegionOpen(1));
            for (int r = 2; r <= 4; r++) Assert.AreEqual(RegionDef.Of(r).HasContent, sim.RegionOpen(r), "regiao sem conteudo fica fechada mesmo com estrelas e chefe");
            Assert.IsFalse(sim.RegionOpen(0)); Assert.IsFalse(sim.RegionOpen(5));
            CollectionAssert.AreEqual(open.ToArray(), sim.OpenRegions());

            // enums so cresceram no fim (salvos por indice)
            Assert.AreEqual(6, (int)CargoType.Barrel); Assert.AreEqual(7, (int)EnemyType.Captain); Assert.AreEqual(9, (int)Perk.ChestLuck); Assert.AreEqual(8, (int)Outcome.Exploded);
            Assert.AreEqual(15, CargoDef.All.Length); Assert.AreEqual(30, EnemyDef.All.Length); Assert.AreEqual(25, PerkDef.All.Length);
            // provisorio = regiao 2-4 ainda sem conteudo (a raia da regiao mede e desliga)
            for (int i = 0; i < CargoDef.All.Length; i++) { Assert.AreEqual((CargoType)i, CargoDef.All[i].Type); Assert.AreEqual(CargoDef.All[i].Region > 1 && !RegionDef.Of(CargoDef.All[i].Region).HasContent, CargoDef.All[i].Provisional, CargoDef.All[i].Name); }
            for (int i = 0; i < EnemyDef.All.Length; i++) { Assert.AreEqual((EnemyType)i, EnemyDef.All[i].Type); Assert.AreEqual(EnemyDef.All[i].Region > 1 && !RegionDef.Of(EnemyDef.All[i].Region).HasContent, EnemyDef.All[i].Provisional, EnemyDef.All[i].Name); }
            for (int i = 0; i < PerkDef.All.Length; i++) Assert.AreEqual((Perk)i, PerkDef.All[i].Type);
            int bosses = 0;
            for (int i = (int)EnemyType.MoleQueen; i <= (int)EnemyType.ElderDragon; i++) if (EnemyDef.All[i].Boss && EnemyDef.All[i].Locks) bosses++;
            Assert.AreEqual(6, bosses, "6 chefes novos trancam a porta");
            Assert.AreEqual("", Hints.Text(Hint.None)); Assert.AreNotEqual("", Hints.Text(Hint.ImpSwap)); Assert.AreNotEqual("", Hints.Text(Hint.Water));
        }

        [Test]
        public void Lancamento_InimigoNovoPersegueEGolpeia_ChefeNovoTranca_InertesParados_CargaNovaEntrega()
        {
            for (int t = (int)EnemyType.GoblinMiner; t < EnemyDef.All.Length; t++)
            {
                if (!EnemyDef.All[t].Provisional) continue;   // com comportamento proprio: testes da raia da regiao (CoreTestsRegiaoN)
                var sim = NewSim(); Run r = Start(sim, 0); r.Courier.Hp = 99;
                Steps(sim, 1, Up);
                Enemy e = r.Spawn((EnemyType)t, new V2(4.5f, 6f)); e.Hp = 999;
                EnemyDef d = EnemyDef.All[t];
                V2 p0 = e.Pos;
                for (int i = 0; i < StepsFor(8f); i++) Steps(sim, 1, i % 20 < 10 ? Left : Right);   // andando com a carga: nao atira
                string k = d.Name;
                if (d.Speed > 0f && d.Damage > 0) Assert.Less(r.Courier.Hp, 99, k + ": sem comportamento devia perseguir e golpear");
                else { Assert.AreEqual(99, r.Courier.Hp, k); Assert.AreEqual(p0.X, e.Pos.X, 1e-4f, k + ": inerte"); Assert.AreEqual(p0.Y, e.Pos.Y, 1e-4f, k); }
                if (d.Boss) Assert.IsTrue(r.Doors[0].Locked, k + ": chefe vivo tranca a porta");
                if (d.Hazard) { r.Courier.Pos = e.Pos + new V2(0f, -2f); Assert.IsFalse(r.CanHit(e), "o vagonete nao e alvo"); }
            }
            // carga nova sem verbo ainda = carga normal (Midnight Delivery leva a lanterna)
            int d0 = WeeksOf(LiveEvent.Midnight, 1)[0];
            var s2 = new Sim(5) { Reputation = Balance.OpenAfter };
            Run rl = s2.StartEvent(d0, 0, false);
            Assert.AreEqual(CargoType.Lantern, rl.Main.Type);
            ContractResult res = DeliverFrom(s2);
            Assert.AreEqual(Outcome.Delivered, res.Outcome); Assert.Greater(res.Payment, 0);
        }

        [Test]
        public void Codigo_RegiaoVemDoCodigo_Regiao1IgualABeta_RegiaoSemConteudoRecusa_DiaDoJogador()
        {
            // impressao digital dos gerados da v1.0-beta (codigo original, antes do lancamento): a regiao 1 nao mudou
            var beta = new Dictionary<int, string>
            {
                { 0, "Message|AAAAAV|0/15.2/4.8/16.1/14.13/3.12.9/7/|16|126" },
                { 1, "Barrel|AAAABP|17/10.14.3/8.12/16.15/5.11/13.2.4/7/|16|126" },
                { 777, "Message|AAA2KT|8/16.13/4.15.10/9.0/17.3/7/|13|108" },
                { 4242, "Heavy|AAEEUA|2/16.14/10.3.12/4.9.15/8.17/5.11/7/|16|126" },
                { 12345, "Egg|AANB3L|8/5.15.13/0.1.12/9.3/14.2.4/17.11/7/|16|126" },
            };
            foreach (var kv in beta) Assert.AreEqual(kv.Value, Fp(Sim.CodeContract(ContractCode.Encode(kv.Key))), "payload " + kv.Key);
            foreach (int p in new[] { 0, 777, 4242, ContractCode.MaxPayload })
                for (int r = 1; r <= 4; r++) Assert.AreEqual(r, Content.RegionOf(Content.WithRegion(p, r)));
            string c2 = ContractCode.Encode(Content.WithRegion(4242, 2));
            Assert.IsTrue(ContractCode.IsValid(c2));
            Assert.AreEqual(RegionDef.Of(2).HasContent, Sim.CodeContract(c2) != null, "codigo da regiao 2: contrato so com conteudo");
            var sim = new Sim(1) { Reputation = 24 };
            Assert.IsNull(sim.StartCode(c2, false));
            for (uint e = 0; e < 50; e++)
            {
                string c = sim.NewCode(e);
                Assert.AreEqual(ContractCode.Length, c.Length);
                ContractDef a = Sim.CodeContract(c), b = Sim.CodeContract(c);
                Assert.AreEqual(1, a.Region, "codigo novo so em regiao destravada");
                Assert.AreEqual(Fp(a), Fp(b), "mesmo codigo, mesmo contrato");
            }
            Assert.AreEqual(Fp(Sim.DailyContract(20261010)), Fp(sim.DailyFor(20261010)), "so com a regiao 1, o dia do jogador = o atalho");
        }

        // ------------------------------------------------------------------ terreno comum (dados das regioes 2-4)

        [Test]
        public void Terreno_LavaSolidaParaAndarTransparenteParaTiro_RiscoPeloTerreno_Dormindo()
        {
            var room = new RoomDef(0, "R9-9", "Teste", "", 1, new Rect[0], new Spawn[0], new V2(2f, 2.5f)) { Lava = new[] { new Rect(4.5f, 7f, 1.5f, 0.5f) } };
            Assert.IsTrue(Run.Blocked(room, new V2(4.5f, 7f), 0.3f), "lava e solida para andar");
            Assert.IsFalse(Run.ShotBlocked(room, new V2(4.5f, 7f), 0.1f), "lava e transparente para tiro");
            V2 p = new V2(4.5f, 7.2f); Run.Collide(room, ref p, 0.4f);
            Assert.IsFalse(Run.Blocked(room, p, 0.39f), "a colisao tira de cima da lava");
            var nav = new Nav(room);
            Assert.IsFalse(nav.Clear(new V2(4.5f, 4f), new V2(4.5f, 10f), 0.4f));
            Assert.IsTrue(nav.ShotClear(new V2(4.5f, 4f), new V2(4.5f, 10f), 0.1f));
            Assert.Less(nav.Steps(new V2(4.5f, 4f), new V2(4.5f, 10f)), int.MaxValue, "da para contornar");
            Assert.AreEqual(CargoRisk.Smash, room.Risk, "lava mostra Pancada");
            var vent = new RoomDef(0, "R9-8", "Teste", "", 1, new Rect[0], new Spawn[0], new V2(2f, 2.5f)) { Vents = new[] { new Vent(4.5f, 6f, 1f, 0f) } };
            Assert.AreEqual(CargoRisk.Smash, vent.Risk, "respiro mostra Pancada");
            Assert.AreEqual(CargoRisk.None, new RoomDef(0, "R9-7", "Teste", "", 1, new Rect[0], new Spawn[0], new V2(2f, 2.5f)).Risk);
            Assert.IsTrue(new Spawn(EnemyType.GoblinMiner, 1f, 10f, true).Asleep);

            // dormindo: nao age; acorda com o courier a 2 m; o 1o golpe dobra
            var sim = NewSim(); Run r = Start(sim, 0); r.Courier.Hp = 99;
            Steps(sim, 1, Up);
            Enemy b = r.Spawn(EnemyType.Brute, new V2(4.5f, 10f)); b.Asleep = true; b.Hp = 99;
            V2 b0 = b.Pos;
            for (int i = 0; i < StepsFor(3f); i++) Steps(sim, 1, i % 20 < 10 ? Left : Right);
            Assert.IsTrue(b.Asleep); Assert.AreEqual(b0.Y, b.Pos.Y, 1e-4f, "dormindo nao anda");
            Assert.AreEqual(Hint.None, r.Hint);
            r.Courier.Pos = b.Pos + new V2(0f, -1.9f);
            Steps(sim, 1, Left);
            Assert.IsFalse(b.Asleep, "o courier a 2 m acorda");
            var s2 = NewSim(); Run r2 = Start(s2, 0); r2.Courier.Hp = 99;
            Steps(s2, 1, Up);
            Enemy g = r2.Spawn(EnemyType.Guard, r2.Courier.Pos + new V2(0f, 4.5f)); g.Asleep = true;
            int hp = g.Hp; bool first = true;
            for (int i = 0; i < StepsFor(3f); i++)
                foreach (SimEvent e in Steps(s2, 1, V2.Zero))
                    if (e.Kind == Ev.EnemyHit && e.A == g.Id)
                    {
                        Assert.AreEqual(first ? hp - 2 : hp - 1, e.B, first ? "1o golpe num dormindo: x2" : "acordado: dano normal");
                        hp = e.B; first = false;
                    }
            Assert.IsFalse(first, "nao atirou no dormindo");
            // explosao a 4 m acorda (bombinha no Guarda; o dormindo fica a 3,5 m dele: fora do raio e do vizinho)
            var sb = ArmedSim(Weapon.Bomb); Run rb = Start(sb, 0); rb.Courier.Hp = 99;
            Steps(sb, 1, Up);
            Enemy tg = rb.Spawn(EnemyType.Guard, rb.Courier.Pos + new V2(0f, 3f)); tg.Hp = 99;
            Enemy sl = rb.Spawn(EnemyType.Brute, tg.Pos + new V2(3.5f, 0f)); sl.Asleep = true; sl.Hp = 99;
            bool boom = false;
            for (int i = 0; i < StepsFor(0.95f) && !boom; i++) boom = Has(Steps(sb, 1, V2.Zero), Ev.Explosion);
            Assert.IsTrue(boom, "a bombinha nao explodiu antes de 1 s");
            Assert.IsFalse(sl.Asleep, "explosao a 4 m acorda");
            // vizinho acordado a 3 m por 1 s acorda
            var s3 = NewSim(); Run r3 = Start(s3, 0);
            Enemy z = r3.Spawn(EnemyType.Brute, new V2(2f, 11f)); z.Asleep = true;
            r3.Spawn(EnemyType.Guard, new V2(4f, 11f));
            for (int i = 0; i < StepsFor(0.9f); i++) Steps(s3, 1, i % 20 < 10 ? Left : Right);
            Assert.IsTrue(z.Asleep, "antes de 1 s");
            for (int i = 0; i < StepsFor(0.2f); i++) Steps(s3, 1, i % 20 < 10 ? Left : Right);
            Assert.IsFalse(z.Asleep, "vizinho acordado a 3 m por 1 s");
        }

        [Test]
        public void Escuro_MiraSoNoQueEstaNaLuz_TochasDaMeiaNoite_NeblinaCurta()
        {
            int d = WeeksOf(LiveEvent.Midnight, 1)[0];
            var sim = new Sim(5) { Reputation = Balance.OpenAfter };
            Run r = sim.StartEvent(d, 0, false); r.Enemies.Clear();
            Assert.IsTrue(r.Dark); Assert.IsTrue(r.Lit);
            CollectionAssert.AreEqual(Balance.MidnightTorches, r.Torches, "sala sem tocha ganha as da meia-noite");
            // lanterna nas maos ilumina 5,5 m; no chao, 4 m em volta dela (apagada, nada); tocha 2,5 m
            Assert.AreEqual(CargoType.Lantern, r.Main.Type); Assert.AreEqual(CargoState.Held, r.Main.State);
            r.Courier.Pos = new V2(4.5f, 0.9f);
            Assert.IsTrue(r.InLight(new V2(4.5f, 6.2f)), "lanterna nas maos: 5,5 m");
            Assert.IsFalse(r.InLight(new V2(4.5f, 7.0f)), "alem da lanterna e longe das tochas");
            Steps(sim, 1, Up); Steps(sim, 7, V2.Zero);
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            r.Courier.Pos = new V2(1f, 12f);
            V2 lit = r.Main.Pos + new V2(0f, 3.8f);
            Assert.IsTrue(r.InLight(lit), "lanterna no chao: 4 m");
            r.Main.Out = true;
            Assert.IsFalse(r.InLight(lit), "lanterna apagada nao ilumina");
            Assert.IsTrue(r.InLight(new V2(0.8f, 5.3f)), "perto da tocha");
            Assert.IsFalse(r.InLight(new V2(4.5f, 9.4f)), "fora da luz do courier (3,5 m)");
            Assert.IsTrue(r.InLight(new V2(1f, 8.6f)), "dentro da luz do courier");
            // Neblina: so 3,5 m
            var f = NewSim(); f.DevStars(Balance.ModifiersStars); Run rf = f.StartContract(0, false, Modifier.Fog); rf.Enemies.Clear();
            Assert.IsTrue(rf.Lit);
            Enemy g = rf.Spawn(EnemyType.Guard, rf.Courier.Pos + new V2(0f, 4.5f));
            Assert.IsFalse(rf.CanHit(g), "Neblina: 4,5 m esta fora da luz");
            var n = NewSim(); Run rn = Start(n, 0);
            Assert.IsFalse(rn.Lit); Assert.IsTrue(rn.CanHit(rn.Spawn(EnemyType.Guard, rn.Courier.Pos + new V2(0f, 4.5f))), "sem escuro mira normal");
        }

        // ------------------------------------------------------------------ perks novos

        [Test]
        public void PerksNovos_SoDaRegiao2EmDiante_NoSorteio()
        {
            for (int seed = 1; seed <= 40; seed++)
                foreach (Perk p in ToOffer(NewSim(seed), 3).PerkOffer) Assert.Less((int)p, (int)Perk.Spring, "regiao 1 so sorteia os 10 da v1");
            Assert.AreEqual(1, PerkDef.All[(int)Perk.ChestLuck].Region); Assert.AreEqual(2, PerkDef.All[(int)Perk.Spring].Region); Assert.AreEqual(3, PerkDef.All[(int)Perk.CatEyes].Region);
        }

        [Test]
        public void PerksNovos_CadaUmMudaARun()
        {
            // Mola: dash 4,2 m
            var s1 = NewSim(); Run r1 = Start(s1, 0); r1.Perks.Add(Perk.Spring);
            Steps(s1, 5, Up); V2 st = r1.Courier.Pos; Steps(s1, 1, Up, true); Steps(s1, 5, Up);
            Assert.AreEqual(Balance.DashDist * Balance.PerkSpring, r1.Courier.Pos.Y - st.Y, 0.01f, "Mola");

            // Segundo folego: 1x volta com 2 de vida
            var s2 = NewSim(); Run r2 = Start(s2, 0); r2.Perks.Add(Perk.SecondWind);
            r2.Courier.Hp = 1; r2.Spawn(EnemyType.Bat, r2.Courier.Pos);
            List<SimEvent> evs = Steps(s2, 2, V2.Zero);
            Assert.IsTrue(s2.InContract, "Segundo folego"); Assert.AreEqual(Balance.PerkSecondWindHp, r2.Courier.Hp); Assert.IsTrue(Has(evs, Ev.SecondWind)); Assert.IsTrue(r2.SecondWindUsed);
            Die(s2);

            // Faro de bau e Batedor: a porta mostra o bau exato e quantos inimigos
            Sim s3 = InRoomWith(3, 0, Content.R5, CargoType.Box, Perk.ChestSense, Perk.Scout);
            foreach (Door d in s3.Run.Doors) { Assert.IsTrue(d.ChestKnown); Assert.AreEqual(Content.Rooms[d.Room].Spawns.Length, d.Enemies); }
            Sim s3b = InRoomWith(3, 0, Content.R5, CargoType.Box);
            foreach (Door d in s3b.Run.Doors) { Assert.IsFalse(d.ChestKnown); Assert.AreEqual(-1, d.Enemies); }

            // Pe firme: poca nao freia
            var s4 = NewSim(); Run r4 = Start(s4, 0); r4.Perks.Add(Perk.SureFoot);
            Steps(s4, 1, Up);
            r4.Puddles.Add(new Puddle { Pos = r4.Courier.Pos, Radius = 1.5f, Left = 4f });
            Steps(s4, 1, Up); Assert.IsFalse(r4.Courier.Slowed, "Pe firme");
            V2 a4 = r4.Courier.Pos; Steps(s4, 3, Right);
            Assert.AreEqual(Balance.CarrySpeed * 3 * Dt, V2.Dist(a4, r4.Courier.Pos), 0.01f);

            // Isolamento: ovo choca 20% mais tarde; gelo derrete 40% mais devagar
            var s5 = NewSim(); Run r5 = Start(s5, 2); r5.Perks.Add(Perk.Insulated);
            Assert.AreEqual(Sim.Contracts[2].EggTarget * Balance.PerkInsulatedEgg, r5.EggTarget, 1e-3f);
            r5.Time = Sim.Contracts[2].EggTarget + 1f; Steps(s5, 1, Up);
            Assert.IsFalse(r5.Hatched, "Isolamento: ainda nao choca");
            r5.Time = r5.EggTarget; Steps(s5, 1, Up);
            Assert.IsTrue(r5.Hatched);
            Assert.AreEqual(Balance.PerkInsulatedMelt, r5.MeltMul);

            // Alca firme: o 1o dash da sala leva a carga; o 2o larga
            var s6 = NewSim(); Run r6 = Start(s6, 0); r6.Perks.Add(Perk.FirmGrip);
            Steps(s6, 3, Up); Steps(s6, 1, Up, true); Steps(s6, 6, Up);
            Assert.AreEqual(1, r6.Courier.DashCount); Assert.AreEqual(CargoState.Held, r6.Main.State, "Alca firme");
            r6.Courier.DashCharges = 1; r6.Courier.DashCd = 0f;
            Steps(s6, 1, Up, true);
            Assert.AreEqual(CargoState.Ground, r6.Main.State, "so o 1o dash da sala");

            // Contragolpe: dash no Capitao com a carga a derruba de primeira
            foreach (bool counter in new[] { false, true })
            {
                var s7 = NewSim(); Run r7 = Start(s7, 0); r7.Courier.Hp = 99;
                if (counter) r7.Perks.Add(Perk.Counter);
                Steps(s7, 1, Up); Steps(s7, 7, V2.Zero);
                Enemy cap = r7.Spawn(EnemyType.Captain, r7.Main.Pos + new V2(0.3f, 0f));
                r7.Courier.Pos = new V2(1f, 12f);
                for (int i = 0; i < StepsFor(1f) && cap.Carry == null; i++) Steps(s7, 1, i % 20 < 10 ? Left : Right);
                Assert.IsNotNull(cap.Carry);
                r7.Enemies.RemoveAll(e => e != cap);
                r7.Courier.Pos = cap.Pos - new V2(0f, 1.5f); r7.Courier.DashCd = 0f;
                Steps(s7, 4, Up, true);
                Assert.AreEqual(counter ? CargoState.Ground : CargoState.Stolen, r7.Main.State, counter ? "Contragolpe derruba" : "sem o perk o Capitao aguenta o 1o golpe");
            }

            // Cacador: +2 moedas por inimigo
            var s8 = NewSim(); Run r8 = Start(s8, 0); r8.Perks.Add(Perk.Hunter); r8.Courier.Hp = 99;
            Steps(s8, 1, Up);
            r8.Spawn(EnemyType.Bat, r8.Courier.Pos + new V2(0f, 2.5f)).Hp = 1;
            int coins = s8.Coins;
            evs = Steps(s8, StepsFor(1.5f), V2.Zero);
            Assert.AreEqual(1, r8.Kills); Assert.AreEqual(coins + 2, s8.Coins, "Cacador"); Assert.AreEqual(2, r8.HuntCoins);
            Assert.IsTrue(evs.Exists(e => e.Kind == Ev.CurrencySource && e.B == 4));

            // Couro grosso: golpe de 2 tira 1
            var s9 = NewSim(); Run r9 = Start(s9, 0); r9.Perks.Add(Perk.ThickSkin); r9.Courier.Hp = 99;
            r9.Courier.Pos = new V2(2.0f, 5.4f); Steps(s9, 2, Up);
            r9.Spawn(EnemyType.Brute, new V2(3.1f, 5.6f)).Hp = 999;
            evs = new List<SimEvent>();
            for (int i = 0; i < StepsFor(0.8f); i++) evs.AddRange(Steps(s9, 1, Up));
            SimEvent hit = evs.Find(e => e.Kind == Ev.DamageTaken);
            Assert.AreEqual(1, hit.A, "Couro grosso: 2 - 1");

            // Olhos de gato: luz x1,6
            var s10 = NewSim(); Run r10 = Start(s10, 0); r10.Perks.Add(Perk.CatEyes);
            Assert.AreEqual(Balance.PerkCatEyes, r10.LightMul);

            // Assobio: o ladrao que pega a carga fica parado 1,5 s
            var s11 = NewSim(); Run r11 = Start(s11, 1); r11.Perks.Add(Perk.Whistle);
            Steps(s11, 3, Up); Steps(s11, 6, V2.Zero);
            Enemy th = r11.Spawn(EnemyType.Thief, r11.Main.Pos + new V2(0.2f, 0f));
            r11.Courier.Pos = new V2(1f, 12f);
            for (int i = 0; i < StepsFor(1f) && th.Carry == null; i++) Steps(s11, 1, i % 20 < 10 ? Left : Right);
            Assert.IsNotNull(th.Carry); Assert.Greater(th.Stun, 1.4f, "Assobio");
            V2 tp = th.Pos;
            for (int i = 0; i < StepsFor(1.3f); i++) Steps(s11, 1, i % 20 < 10 ? Left : Right);
            Assert.AreEqual(tp.X, th.Pos.X, 1e-4f, "parado"); Assert.AreEqual(tp.Y, th.Pos.Y, 1e-4f);

            // Primeiros socorros: +1 de vida ao abrir bau (C4 comeca na R5)
            var s12 = NewSim(); Run r12 = Start(s12, 3); r12.Perks.Add(Perk.FirstAid);
            Steps(s12, 1, Up); r12.Courier.Hp = 3;
            r12.Courier.Pos = r12.Chest.Pos + new V2(0.6f, 0f);
            Steps(s12, StepsFor(1.4f), V2.Zero);
            Assert.IsTrue(r12.Chest.Opened); Assert.AreEqual(4, r12.Courier.Hp, "Primeiros socorros");

            // Corredor: +20% sem a carga
            var s13 = NewSim(); Run r13 = Start(s13, 0); r13.Perks.Add(Perk.Runner);
            Steps(s13, 3, Up); Steps(s13, 6, V2.Zero);
            r13.Courier.Pos = new V2(4.5f, 6f); V2 a13 = r13.Courier.Pos; Steps(s13, 10, Up);
            Assert.AreEqual(Balance.CarrySpeed * Balance.FreeSpeedMul * Balance.PerkRunner * 10 * Dt, V2.Dist(a13, r13.Courier.Pos), 0.01f, "Corredor");

            // Escudo da carga: o 1o golpe por sala na carga pousada e ignorado (mesma semente, com e sem)
            int[] cracks = new int[2];
            for (int k = 0; k < 2; k++)
            {
                var s = NewSim(); Run r = Start(s, 0); r.Courier.Hp = 99;
                if (k == 1) r.Perks.Add(Perk.CargoShield);
                Steps(s, 3, Up); Steps(s, 6, V2.Zero);
                r.Spawn(EnemyType.Brute, r.Main.Pos + new V2(0f, 1f)).Hp = 999;
                Steps(s, StepsFor(10f), V2.Zero);
                cracks[k] = r.Main.Cracks;
            }
            Assert.Greater(cracks[0], 1);
            Assert.AreEqual(cracks[0] - 1, cracks[1], "Escudo da carga: 1 golpe a menos");
        }

        // ------------------------------------------------------------------ modificadores

        [Test]
        public void Modificadores_Com40Estrelas_Ate2_PagamentoExtra_CadaUmMudaARun()
        {
            var none = NewSim();
            Assert.IsFalse(none.ModifiersOpen); Assert.IsNull(none.StartContract(0, false, Modifier.Rush), "antes de 40 estrelas");
            var sim = NewSim(); sim.DevStars(Balance.ModifiersStars);
            Assert.IsTrue(sim.ModifiersOpen);
            Assert.IsNull(sim.StartContract(0, false, Modifier.Rush | Modifier.Horde | Modifier.Fog), "3 modificadores");
            Run r = sim.StartContract(0, false, Modifier.Rush | Modifier.Horde);
            Assert.IsNotNull(r); Assert.AreEqual(Modifier.Rush | Modifier.Horde, r.Mods);
            Assert.IsTrue(Has(sim.Events, Ev.ModifierPick)); Assert.AreEqual("modifier_pick", SimEvent.DiaryName(Ev.ModifierPick));
            Assert.AreEqual(Content.Rooms[Content.R1].Spawns.Length + 1, r.Enemies.Count, "Horda: +1 perseguidor");
            Assert.AreEqual(EnemyType.Bat, r.Enemies[r.Enemies.Count - 1].Type, "regiao 1: morcego");
            Assert.AreEqual(Sim.Contracts[0].ParTime * Balance.RushPar, r.ParTime, 1e-3f);
            r.Enemies.Clear(); r.Time = Sim.Contracts[0].ParTime * 0.85f;
            ContractResult res = DeliverFrom(sim);
            Assert.IsFalse(res.StarTime, "Pressa: o tempo-alvo encolheu");
            Assert.AreEqual(100 + 20 + 25, res.PayPct);
            Assert.AreEqual(87, res.Payment, "40 x 1,0 x 1,5 x 1,45 = 87");

            // Horda: o reforco vem com +1 (C5 nivel 4, R1: reforco com a carga 3 s no chao)
            int ci = IndexOf(CargoType.Relic);
            var h = NewSim(); h.DevStars(40); Run rh = h.StartContract(ci, false, Modifier.Horde); rh.Enemies.Clear(); rh.Courier.Hp = 99;
            Steps(h, 1, Up);
            for (int i = 0; i < StepsFor(4f) && rh.WavePending == 0; i++) { rh.Enemies.Clear(); Steps(h, 1, V2.Zero); }
            Assert.AreEqual(Content.Rooms[Content.R1].Wave.Length + 1, rh.WavePending, "o reforco da Horda tem +1");

            // Fragil: qualquer carga quebra com 2 rachaduras (a caixa inclusive)
            var f = NewSim(); f.DevStars(40); Run rf = f.StartContract(0, false, Modifier.Fragile); rf.Enemies.Clear(); rf.Courier.Hp = 99;
            Assert.AreEqual(2, rf.BreakAt);
            Steps(f, 3, Up); Steps(f, 6, V2.Zero);
            rf.Spawn(EnemyType.Brute, rf.Main.Pos + new V2(0f, 1f)).Hp = 999;
            for (int i = 0; i < StepsFor(10f) && f.InContract; i++) Steps(f, 1, V2.Zero);
            Assert.AreEqual(Outcome.CargoBroken, f.LastResult.Outcome, "Fragil: a caixa quebrou com 2");
            Assert.AreEqual(2, f.LastResult.Cracks);
            Assert.AreEqual(Balance.FragileIce, NewSim().StartContract(0, false).IceFailAt + Balance.FragileIce, 1e-6f);

            // Pao-duro: bau pela metade (C4 comeca na R5, bau de 35)
            var s = NewSim(); s.DevStars(40); Run rs = s.StartContract(3, false, Modifier.Stingy); rs.Enemies.Clear(); rs.Courier.Hp = 99;
            Steps(s, 1, Up); rs.Courier.Pos = rs.Chest.Pos + new V2(0.6f, 0f);
            int coins = s.Coins; Steps(s, StepsFor(1.4f), V2.Zero);
            Assert.IsTrue(rs.Chest.Opened); Assert.AreEqual(coins + 18, s.Coins, "35 x 0,5 = 17,5 -> 18");

            // Neblina: visao curta
            var n = NewSim(); n.DevStars(40);
            Assert.AreEqual(Balance.FogRadius, n.StartContract(0, false, Modifier.Fog).VisionRadius);
            Assert.AreEqual(0f, NewSim().StartContract(0, false).VisionRadius);

            // contrato do dia: 1 modificador pela data quando abertos
            var d = new Sim(3) { Reputation = Balance.OpenAfter }; d.DevStars(40);
            Run rd = d.StartDaily(20261010, false);
            Assert.AreEqual(Sim.DailyModifier(20261010), rd.Mods); Assert.AreEqual(1, ModifierDef.Count(rd.Mods));
            var d0 = new Sim(3) { Reputation = Balance.OpenAfter };
            Assert.AreEqual(Modifier.None, d0.StartDaily(20261010, false).Mods, "sem 40 estrelas o dia vem sem modificador");
        }

        // ------------------------------------------------------------------ eventos

        [Test]
        public void Eventos_UmPorSemanaEmRodizio_QuadroCom3_RecompensaFixa1x_ProjetoDaSemana_RelogioNaoRepete()
        {
            int mon = Monday(20261012);
            var seen = new HashSet<LiveEvent>();
            for (int w = 0; w < 5; w++) seen.Add(Sim.EventOf(AddDays(mon, 7 * w)));
            Assert.AreEqual(5, seen.Count, "5 templates em 5 semanas");
            for (int k = 1; k < 7; k++) Assert.AreEqual(Sim.EventOf(mon), Sim.EventOf(AddDays(mon, k)), "segunda a domingo");
            Assert.AreNotEqual(Sim.EventOf(mon), Sim.EventOf(AddDays(mon, -1)));
            Assert.IsFalse(new Sim(4).EventsOpen, "antes do C3");

            var sim = new Sim(4) { Reputation = Balance.OpenAfter };
            int d = mon; sim.SeeDate(d);
            Assert.AreEqual(Balance.EventContracts, sim.EventSlots(d));
            Assert.IsFalse(sim.EventAvailable(d, 3), "so 3 vagas sem o pacote");
            int[] shape = { 4, 5, 6 };
            for (int k = 0; k < 3; k++)
            {
                ContractDef def = sim.EventContract(d, k);
                Assert.AreEqual(Sim.EventOf(d), def.Event); Assert.AreEqual(ContractKind.Event, def.Kind); Assert.AreEqual(-1, def.Index); Assert.AreEqual(k, def.Slot);
                if (def.Event != LiveEvent.BossRun) Assert.AreEqual(shape[k] + 1, def.Layers.Length, "vaga " + k);
                Assert.AreEqual(Fp(def), Fp(sim.EventContract(AddDays(d, 3), k)), "mesma vaga na mesma semana = mesmo contrato");
            }
            Assert.AreNotEqual(Fp(sim.EventContract(d, 1)), Fp(sim.EventContract(AddDays(d, 7), 1)));

            // sem resgate em evento: a carga perdida fica pendente
            Run lost = Start(sim, 0); Die(sim);
            Assert.IsNotNull(sim.Pending);
            Run r = sim.StartEvent(d, 1, true);
            Assert.IsFalse(r.RescueOffered); Assert.IsNotNull(sim.Pending, "evento nao consome o resgate");
            Assert.IsTrue(Has(sim.Events, Ev.EventStart)); Assert.AreEqual("event_start", SimEvent.DiaryName(Ev.EventStart));
            int coins = sim.Coins;
            ContractResult res = DeliverFrom(sim);
            Assert.AreEqual(Outcome.Delivered, res.Outcome);
            Assert.AreEqual(Sim.EventReward(1), res.EventReward);
            Assert.AreEqual(coins + res.Total, sim.Coins);
            Assert.IsTrue(sim.EventDone(d, 1)); Assert.IsFalse(sim.EventAvailable(d, 1)); Assert.IsNull(sim.StartEvent(AddDays(d, 2), 1, false), "1x por semana");
            Assert.AreEqual(Gear.None, res.EventProject);
            sim.StartEvent(d, 0, false); Assert.AreEqual(Gear.None, DeliverFrom(sim).EventProject);
            sim.StartEvent(d, 2, false); res = DeliverFrom(sim);
            var weekGear = (Gear)(1 + Sim.Week(d) % 4);
            Assert.AreEqual(weekGear, res.EventProject, "as 3 vagas dao o projeto da semana");
            Assert.AreEqual(1, sim.Blueprints(weekGear));

            int next = AddDays(d, 7); sim.SeeDate(next);
            Assert.IsTrue(sim.EventAvailable(next, 1), "semana nova");
            Assert.IsFalse(sim.EventAvailable(d, 0), "relogio voltando nao reabre a semana passada");
            var fresh = new Sim(4) { Reputation = Balance.OpenAfter }; fresh.SeeDate(next);
            Assert.IsFalse(fresh.EventAvailable(d, 0), "data anterior a maior vista: relogio voltando");
            Assert.IsNull(fresh.StartEvent(d, 0, false));
            Sim b = Sim.Load(sim.Save());
            Assert.IsFalse(b.EventAvailable(d, 1)); Assert.IsTrue(b.EventAvailable(next, 1));
        }

        [Test]
        public void Eventos_CadaTemplateMudaOContrato()
        {
            // Fragile Week: so carga fragil, x1,5
            int fw = WeeksOf(LiveEvent.FragileWeek, 1)[0];
            var f = new Sim(4) { Reputation = Balance.OpenAfter };
            for (int k = 0; k < 3; k++) Assert.IsTrue(CargoDef.All[(int)f.EventContract(fw, k).Cargo].Fragile, "Fragile Week vaga " + k);
            f.StartEvent(fw, 0, false);
            Assert.AreEqual(150, DeliverFrom(f).PayPct);

            // Midnight: lanterna e escuro
            int md = WeeksOf(LiveEvent.Midnight, 1)[0];
            var m = new Sim(4) { Reputation = Balance.OpenAfter };
            Run rm = m.StartEvent(md, 0, false);
            Assert.AreEqual(CargoType.Lantern, rm.Main.Type); Assert.IsTrue(rm.Dark);

            // No-Damage: 1 de vida, x3, sem cura por sala
            int nd = WeeksOf(LiveEvent.NoDamage, 1)[0];
            var n = new Sim(4) { Reputation = Balance.OpenAfter };
            Run rn = n.StartEvent(nd, 0, false);
            Assert.AreEqual(1, rn.Courier.MaxHp); Assert.AreEqual(1, rn.Courier.Hp);
            rn.Courier.MaxHp = 3; Cross(n);
            Assert.AreEqual(1, rn.Courier.Hp, "No-Damage nao cura ao entrar na sala");
            rn.Courier.MaxHp = 1;
            Assert.AreEqual(300, DeliverFrom(n).PayPct);
            int offers = 0;
            foreach (int dd in WeeksOf(LiveEvent.NoDamage, 12))   // sem perks de vida no sorteio
                for (int k = 0; k < 3; k++)
                {
                    var s = new Sim(10 + k) { Reputation = Balance.OpenAfter };
                    Run r = s.StartEvent(dd, k, false);
                    for (int g = 0; g < 2 && s.InContract; g++)   // atravessa 2 portas sem escolher: a oferta abre depois da camada 2
                    {
                        int layer = r.Layer;
                        r.Courier.Pos = new V2(r.Doors[0].Pos.X, 13.4f);
                        for (int i = 0; i < 150 && r.Layer == layer; i++) { r.Enemies.Clear(); Steps(s, 1, Up); }
                    }
                    Assert.IsNotNull(r.PerkOffer);
                    offers++;
                    foreach (Perk p in r.PerkOffer) Assert.AreNotEqual(Perk.ExtraHeart, p, "No-Damage sorteou Coracao extra");
                }
            Assert.AreEqual(36, offers);

            // Treasure Detour: bau x2; parede rachada abre encostando
            bool chest = false, secret = false;
            foreach (int td in WeeksOf(LiveEvent.Treasure, 40))
                for (int k = 0; k < 3 && !(chest && secret); k++)
                {
                    var s = new Sim(4) { Reputation = Balance.OpenAfter };
                    ContractDef def = s.EventContract(td, k);
                    RoomDef first = Content.Rooms[def.Layers[0][0]];
                    if (!chest && first.ChestCoins > 0)
                    {
                        Run r = s.StartEvent(td, k, false); r.Enemies.Clear(); r.Courier.Hp = 99;
                        Steps(s, 1, Up); r.Courier.Pos = r.Chest.Pos + new V2(0.6f, 0f);
                        int c0 = s.Coins; Steps(s, StepsFor(1.4f), V2.Zero);
                        Assert.IsTrue(r.Chest.Opened); Assert.AreEqual(c0 + first.ChestCoins * 2, s.Coins, "Treasure: bau x2");
                        chest = true;
                    }
                    else if (!secret && first.Secret >= 0)
                    {
                        Run r = s.StartEvent(td, k, false);
                        bool right = first.SecretPos.X > 1f;
                        r.Courier.Pos = new V2(right ? 8.6f : 0.4f, first.SecretPos.Y - 1.6f);
                        Quiet(s, 25, Up);   // subindo rente a parede rachada
                        Assert.IsTrue(r.Room.IsSecret, "Treasure: a parede rachada abre encostando");
                        secret = true;
                    }
                }
            Assert.IsTrue(chest && secret, "nao achei sala com bau/parede rachada nas semanas de Treasure");

            // Boss Courier Run: os chefes vencidos em fila; cura 2 por sala
            int br = WeeksOf(LiveEvent.BossRun, 1)[0];
            var b = new Sim(4) { Reputation = RegionDef.Of(1).Contracts.Length };   // so os chefes da regiao 1 vencidos
            ContractDef bd = b.EventContract(br, 0);
            Assert.AreEqual(3, bd.Layers.Length); Assert.AreEqual(Content.M1, bd.Layers[0][0]); Assert.AreEqual(Content.B1, bd.Layers[1][0]);
            Run rb = b.StartEvent(br, 0, false); rb.Courier.Hp = 1; Cross(b);
            Assert.AreEqual(1 + Balance.BossRunHeal, rb.Courier.Hp, "Boss Run cura 2");
            var b0 = new Sim(4) { Reputation = Balance.OpenAfter };
            foreach (int[] l in b0.EventContract(br, 0).Layers) foreach (int id in l) Assert.IsFalse(Content.Rooms[id].Boss || Content.Rooms[id].Mini, "sem chefe vencido: gerado comum");
        }

        // ------------------------------------------------------------------ temporada

        [Test]
        public void Temporada_28Dias_ObjetivosContam_RecompensaFixa1x_TemporadaNovaZera_RelogioNaoVolta()
        {
            var sim = new Sim(5) { Reputation = Sim.Contracts.Length };
            Assert.AreEqual(-1, sim.SeasonId, "sem data, sem temporada");
            int d = 20261010; sim.SeeDate(d);
            Assert.AreEqual(Sim.SeasonOf(d), sim.SeasonId);
            Assert.AreEqual(20, Season.Goals.Length);
            for (int i = 0; i < 5; i++) Deliver(sim, 0);
            Assert.AreEqual(5, sim.SeasonProgress(0)); Assert.IsTrue(sim.SeasonReady(0));
            Assert.AreEqual(5, sim.SeasonProgress(16)); Assert.IsFalse(sim.SeasonReady(16));
            Assert.AreEqual(5, sim.SeasonProgress(5), "3 estrelas");
            int coins = sim.Coins;
            Assert.IsTrue(sim.ClaimSeason(0)); Assert.AreEqual(coins + Season.Goals[0].Coins, sim.Coins);
            Assert.IsTrue(Has(sim.Events, Ev.SeasonClaim));
            Assert.IsFalse(sim.ClaimSeason(0), "1x"); Assert.IsFalse(sim.ClaimSeason(16), "incompleto");
            // inimigos derrotados contam mesmo sem entregar
            Run r = Start(sim, 0); r.Courier.Hp = 99; Steps(sim, 1, Up);
            for (int i = 0; i < 3; i++) r.Spawn(EnemyType.Bat, r.Courier.Pos + new V2(-1f + i, 2.5f)).Hp = 1;
            Steps(sim, StepsFor(3f), V2.Zero);
            sim.Abandon();
            Assert.AreEqual(3, sim.SeasonProgress(3));
            // salas secretas: o objetivo da um projeto (Gancho)
            for (int k = 0; k < 2; k++)
            {
                Run rs = Start(sim, 3); Cross(sim, 1);
                Assert.AreEqual(Content.R3, rs.Room.Id);
                rs.Courier.Pos = new V2(8.0f, 3.2f); Quiet(sim, 6, Right);
                Assert.IsTrue(rs.Room.IsSecret);
                sim.Abandon();
            }
            Assert.IsTrue(sim.SeasonReady(7));
            Assert.IsTrue(sim.ClaimSeason(7)); Assert.AreEqual(1, sim.Blueprints(Gear.Hook));
            // temporada nova zera; relogio voltando nao volta
            Sim b = Sim.Load(sim.Save());
            Assert.AreEqual(sim.SeasonProgress(3), b.SeasonProgress(3)); Assert.IsTrue(b.SeasonClaimed(0));
            int next = AddDays(d, Balance.SeasonDays);
            sim.SeeDate(next);
            Assert.AreEqual(Sim.SeasonOf(next), sim.SeasonId); Assert.AreEqual(0, sim.SeasonProgress(0)); Assert.IsFalse(sim.SeasonClaimed(0));
            sim.SeeDate(d);
            Assert.AreEqual(Sim.SeasonOf(next), sim.SeasonId, "relogio voltando");
        }

        // ------------------------------------------------------------------ raro e rerolls

        [Test]
        public void ContratoRaro_SoDepoisDe8h_PagaODobro_1x_RelogioVoltandoNaoOferece()
        {
            var sim = new Sim(6) { Reputation = Balance.OpenAfter };
            int d = 20261010, d2 = 20261011;
            sim.OpenSession(T0, d);
            Assert.AreEqual(0, sim.RareDate, "1a sessao: nada"); Assert.IsTrue(sim.FirstSession);
            sim.Touch(T0 + 600);
            sim.OpenSession(T0 + 600 + 7 * 3600, d);
            Assert.AreEqual(0, sim.RareDate, "menos de 8 h");
            long t1 = T0 + 600 + 7 * 3600 + 8 * 3600;
            sim.OpenSession(t1, d2);
            Assert.AreEqual(d2, sim.RareDate); Assert.IsTrue(sim.RareAvailable); Assert.IsTrue(Has(sim.Events, Ev.RareOffer));
            ContractDef def = sim.RareContract;
            Assert.AreEqual(ContractKind.Rare, def.Kind); Assert.AreEqual(1, def.Region);
            Run r = sim.StartRare(false);
            Assert.IsNotNull(r); Assert.IsTrue(Has(sim.Events, Ev.RareStart));
            ContractResult res = DeliverFrom(sim);
            Assert.AreEqual(Balance.RarePct, res.PayPct, "pagamento x2");
            Assert.IsTrue(sim.RareDone); Assert.IsNull(sim.StartRare(false), "1x");
            Assert.IsTrue(Sim.Load(sim.Save()).RareDone);
            var back = new Sim(6) { Reputation = 3 };
            back.OpenSession(T0, d); back.OpenSession(T0 - 20 * 3600, d);
            Assert.AreEqual(0, back.RareDate, "relogio voltando");
            back.OpenSession(T0 + 3600, d);
            Assert.AreEqual(0, back.RareDate, "o relogio voltando nao recua o ultimo instante ativo");
            var early = new Sim(6);
            early.OpenSession(T0, d); early.OpenSession(T0 + 9 * 3600, d2);
            Assert.AreEqual(0, early.RareDate, "antes do C3");
        }

        [Test]
        public void Reroll_DoDiaEDoRaro_1xPorDia_PrecoCrescente_TrocaOContrato()
        {
            var sim = new Sim(6) { Reputation = Balance.OpenAfter, Coins = 1000 };
            int d = 20261010;
            sim.OpenSession(T0, d); sim.OpenSession(T0 + 9 * 3600, d);
            Assert.IsTrue(sim.RareAvailable);
            string daily0 = Fp(sim.DailyFor(d)), rare0 = Fp(sim.RareContract);
            Assert.AreEqual(60, sim.RerollCost);
            Assert.IsTrue(sim.Reroll(false)); Assert.AreEqual(940, sim.Coins);
            Assert.AreNotEqual(daily0, Fp(sim.DailyFor(d)), "o reroll troca o contrato do dia");
            Assert.IsFalse(sim.CanReroll(false), "1x por dia"); Assert.IsFalse(sim.Reroll(false));
            Assert.AreEqual(120, sim.RerollCost, "preco crescente");
            Assert.IsTrue(sim.Reroll(true)); Assert.AreEqual(820, sim.Coins);
            Assert.AreNotEqual(rare0, Fp(sim.RareContract)); Assert.IsFalse(sim.CanReroll(true));
            Assert.AreEqual("reroll", SimEvent.DiaryName(Ev.Reroll));
            string rolled = sim.DailyFor(d).ShareCode;
            Assert.AreEqual(rolled, sim.StartDaily(d, false).Contract.ShareCode, "o dia jogado e o rerolado");
            sim.Abandon();
            Sim b = Sim.Load(sim.Save());
            Assert.AreEqual(rolled, b.DailyFor(d).ShareCode); Assert.IsFalse(b.CanReroll(false));
            sim.SeeDate(20261011);
            Assert.AreEqual(60, sim.RerollCost); Assert.IsTrue(sim.CanReroll(false), "dia novo");
            var poor = new Sim(6) { Reputation = 3, Coins = 59 }; poor.SeeDate(d);
            Assert.IsFalse(poor.Reroll(false));
        }

        // ------------------------------------------------------------------ equipamento lateral

        [Test]
        public void Equipamento_3ProjetosDestravam_5NiveisComMoedas_RegiaoUmSemProjetoNaPorta()
        {
            Sim g = Sim.Load("v=3\nseed=3\nrep=24\ncoins=20000\ngear=0,0,2,3,0,0,0,0,0,0,0\n");
            Assert.AreEqual(2, g.Blueprints(Gear.Smoke)); Assert.IsFalse(g.GearUnlocked(Gear.Smoke)); Assert.IsTrue(g.GearUnlocked(Gear.Hook));
            Assert.AreEqual(0, g.GearLevel(Gear.Smoke)); Assert.AreEqual(1, g.GearLevel(Gear.Hook));
            Assert.IsFalse(g.EquipGear(Gear.Smoke), "bloqueado"); Assert.IsTrue(g.EquipGear(Gear.Hook)); Assert.AreEqual(Gear.Hook, g.GearEquipped);
            int[] cost = { 1000, 2500, 4000, 5000 };   // raia E (BALANCE.md "Economia da campanha"): 12.500 por equipamento
            int coins = g.Coins;
            for (int lv = 1; lv < Balance.GearCampaignLevel; lv++)
            {
                Assert.AreEqual(cost[lv - 1], g.GearCost(Gear.Hook));
                Assert.IsTrue(g.UpgradeGear(Gear.Hook)); coins -= cost[lv - 1];
                Assert.AreEqual(coins, g.Coins); Assert.AreEqual(lv + 1, g.GearLevel(Gear.Hook));
            }
            Assert.AreEqual(-1, g.GearCost(Gear.Hook), "nivel 5 e o maximo"); Assert.IsFalse(g.UpgradeGear(Gear.Hook));
            Assert.IsTrue(g.Purchase(Product.StarterPack));
            Assert.IsTrue(g.GearUnlocked(Gear.Smoke), "3o projeto destrava"); Assert.IsTrue(Has(g.Events, Ev.GearUnlock));
            Run r = g.StartContract(0, false);
            Assert.IsFalse(g.EquipGear(Gear.Smoke), "troca so fora do contrato");
            Assert.AreEqual(Gear.Hook, r.Gear); Assert.AreEqual(5, r.GearLevel);
            foreach (Door d in r.Doors) Assert.AreEqual(Gear.None, d.Blueprint, "os projetos dos baus comecam na regiao 2");
            foreach (RoomDef room in Content.Rooms) Assert.IsFalse(g.ProjectTaken(room.Id));
            Sim b = Sim.Load(g.Save());
            Assert.AreEqual(5, b.GearLevel(Gear.Hook)); Assert.AreEqual(Gear.Hook, b.GearEquipped); Assert.IsTrue(b.GearUnlocked(Gear.Smoke));
        }

        [Test]
        public void Equipamento_DisparaSozinho_GanchoFumacaArmadilhaBotas()
        {
            // Gancho: andando sem carga na direcao dela (3 m) por 0,25 s, ela vem para as maos; recarga 10 s no nivel 1
            var h = Geared(Gear.Hook); Run r = Start(h, 0);
            Assert.AreEqual(Gear.Hook, r.Gear);
            Steps(h, 1, Up); Steps(h, 7, V2.Zero);
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            r.Courier.Pos = r.Main.Pos + new V2(0f, 3f);
            List<SimEvent> evs = Steps(h, 8, Down);
            Assert.AreEqual(CargoState.Held, r.Main.State, "Gancho"); Assert.IsTrue(evs.Exists(e => e.Kind == Ev.GearUse && e.A == (int)Gear.Hook));
            Assert.AreEqual(GearDef.HookCooldown[0], r.HookCd, 0.05f);
            Steps(h, 7, V2.Zero);
            r.Courier.Pos = r.Main.Pos + new V2(0f, 3f);
            Steps(h, 8, Down);
            Assert.AreEqual(CargoState.Ground, r.Main.State, "recarga");
            var away = Geared(Gear.Hook); Run ra = Start(away, 0);
            Steps(away, 1, Up); Steps(away, 7, V2.Zero);
            ra.Courier.Pos = ra.Main.Pos + new V2(0f, 3f);
            Steps(away, 10, Up);
            Assert.AreEqual(CargoState.Ground, ra.Main.State, "andando para longe nao puxa");

            // Fumaca: quem pega a carga a solta e fica parado; 1 uso por contrato no nivel 1
            var s = Geared(Gear.Smoke); Run rs = Start(s, 1);
            Assert.AreEqual(1, rs.SmokeLeft);
            Steps(s, 3, Up); Steps(s, 6, V2.Zero);
            Enemy t = rs.Spawn(EnemyType.Thief, rs.Main.Pos + new V2(0.2f, 0f));
            rs.Courier.Pos = new V2(1f, 12f);
            for (int i = 0; i < StepsFor(1f) && !Has(s.Events, Ev.ThiefGrab); i++) Steps(s, 1, i % 20 < 10 ? Left : Right);
            Assert.IsNull(t.Carry, "a fumaca fez soltar"); Assert.AreEqual(CargoState.Ground, rs.Main.State);
            Assert.AreEqual(GearDef.SmokeTime[0], t.Stun, 0.05f); Assert.AreEqual(0, rs.SmokeLeft);
            for (int i = 0; i < StepsFor(6f) && t.Carry == null; i++) Steps(s, 1, i % 20 < 10 ? Left : Right);
            Assert.IsNotNull(t.Carry, "sem fumaca restante o ladrao leva");

            // Armadilha: no 1o pouso da sala, do outro lado da carga; prende o 1o que passar
            var a = Geared(Gear.Trap); Run rt = Start(a, 0); rt.Courier.Hp = 99;
            Steps(a, 1, Up); Steps(a, 7, V2.Zero);
            Assert.AreEqual(1, rt.Traps.Count, "armou no 1o pouso");
            Assert.AreEqual(Balance.PlantOffset + Balance.TrapOffset, V2.Dist(rt.Traps[0].Pos, rt.Courier.Pos), 0.05f);
            Enemy bat = rt.Spawn(EnemyType.Bat, rt.Traps[0].Pos);
            Steps(a, 1, V2.Zero);
            Assert.AreEqual(0f, bat.Stun, "voador nao cai"); Assert.AreEqual(1, rt.Traps[0].Catches);
            rt.Enemies.Clear();
            Enemy br = rt.Spawn(EnemyType.Brute, rt.Traps[0].Pos); br.Hp = 999;
            Steps(a, 1, V2.Zero);
            Assert.AreEqual(GearDef.TrapTime[0], br.Stun, 0.05f, "preso"); Assert.AreEqual(0, rt.Traps[0].Catches);
            rt.Courier.Pos = rt.Main.Pos + new V2(0.5f, 0f); Steps(a, 1, Up); Steps(a, 7, V2.Zero);   // pega e pousa de novo
            Assert.AreEqual(1, rt.Traps.Count, "1 armadilha por sala");

            // Botas: agua e poca nao freiam; nada a acionar
            var bo = Geared(Gear.Boots); Run rb = Start(bo, 0);
            Assert.IsTrue(rb.SureFooted); Assert.AreEqual(1, rb.BootsLevel);
            Steps(bo, 1, Up);
            rb.Puddles.Add(new Puddle { Pos = rb.Courier.Pos, Radius = 1.5f, Left = 4f });
            Steps(bo, 1, Up); Assert.IsFalse(rb.Courier.Slowed);
            Assert.IsFalse(Start(NewSim(), 0).SureFooted);
        }

        // ------------------------------------------------------------------ maestria e marcos

        [Test]
        public void Maestria_PorEntregas_2PorCentoPorNivel_MarcosResgatados1x()
        {
            var sim = NewSim();
            for (int i = 0; i < 3; i++) Assert.AreEqual(60, Deliver(sim, 0).Payment, "nivel 0");
            Assert.AreEqual(3, sim.MasteryCount(CargoType.Box)); Assert.AreEqual(1, sim.MasteryLevel(CargoType.Box));
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == Ev.MasteryUp && e.A == (int)CargoType.Box && e.B == 1));
            ContractResult r4 = Deliver(sim, 0);
            Assert.AreEqual(102, r4.PayPct); Assert.AreEqual(61, r4.Payment, "40 x 1,5 x 1,02 = 61,2 -> 61");
            Assert.AreEqual(0, sim.MasteryLevel(CargoType.Relic), "por carga");
            Assert.AreEqual(4, Sim.Load(sim.Save()).MasteryCount(CargoType.Box));

            var m = new Sim(1);
            m.DevStars(9); Assert.IsFalse(m.MilestoneReady(0));
            m.DevStars(10); Assert.IsTrue(m.MilestoneReady(0)); Assert.IsFalse(m.MilestoneReady(1));
            Assert.IsTrue(m.ClaimMilestone(0)); Assert.AreEqual(Balance.MilestoneCoins[0], m.Coins); Assert.IsTrue(Has(m.Events, Ev.Milestone));
            Assert.IsFalse(m.ClaimMilestone(0), "1x");
            m.DevStars(60); Assert.IsTrue(m.ClaimMilestone(1)); Assert.IsTrue(m.ClaimMilestone(2));
            Assert.AreEqual(Balance.MilestoneCoins[0] + Balance.MilestoneCoins[1] + Balance.MilestoneCoins[2], m.Coins);
            Sim b = Sim.Load(m.Save()); Assert.IsTrue(b.MilestoneClaimed(2)); Assert.IsFalse(b.MilestoneClaimed(3));
        }

        // ------------------------------------------------------------------ desafio sem fim

        [Test]
        public void SemFim_AbreNoFimDaCampanha_SemanaIgualParaTodos_ChefesNoRitmo_DificuldadeSobe_RecordeETetoDiario()
        {
            Assert.IsFalse(new Sim(1) { Reputation = Sim.Contracts.Length - 1 }.EndlessOpen);
            var sim = new Sim(8) { Reputation = Sim.Contracts.Length };
            int d = Monday(20261012); sim.SeeDate(d);
            ContractDef a = sim.EndlessContract(d), b = new Sim(99) { Reputation = Sim.Contracts.Length }.EndlessContract(AddDays(d, 3)), c = sim.EndlessContract(AddDays(d, 7));
            Assert.AreEqual(Fp(a), Fp(b), "mesma semana, mesmo desafio para todos"); Assert.AreNotEqual(a.ShareCode, c.ShareCode);
            Assert.AreEqual(Balance.EndlessLayers + 1, a.Layers.Length); Assert.AreEqual(ContractKind.Endless, a.Kind);
            Assert.AreEqual(Content.M1, a.Layers[4][0], "mini-chefe a cada 5 camadas"); Assert.AreEqual(Content.B1, a.Layers[9][0], "chefe a cada 10");
            Run r = sim.StartEndless(d);
            Assert.IsTrue(Has(sim.Events, Ev.EndlessStart));
            int t0 = r.Tier;
            for (int i = 0; i < 3; i++) Cross(sim);
            Assert.AreEqual(3, r.Layer); Assert.AreEqual(t0 + 3 * Balance.EndlessTierPerLayer, r.Tier);
            Enemy g = r.Spawn(EnemyType.Guard, new V2(4.5f, 12f));
            Assert.AreEqual((int)System.Math.Round(16 * Balance.HpMul(r.Tier)), g.Hp, "a vida sobe com a camada");
            Die(sim);
            ContractResult res = sim.LastResult;
            Assert.AreEqual(3, res.EndlessLayers); Assert.AreEqual(45, res.EndlessCoins); Assert.AreEqual(3, sim.EndlessBest); Assert.AreEqual(3, sim.EndlessWeekBest);
            Assert.IsNull(sim.Pending, "sem fim nao deixa carga perdida");
            Assert.AreEqual("endless_end", SimEvent.DiaryName(Ev.EndlessEnd));
            sim.StartEndless(d); sim.Abandon();
            Assert.AreEqual(3, sim.EndlessBest, "o recorde nao cai"); Assert.AreEqual(0, sim.LastResult.EndlessLayers);
            // teto de 800 moedas por dia
            sim.StartEndless(d);
            for (int i = 0; i < 56; i++) Cross(sim);
            sim.Abandon();
            Assert.AreEqual(Balance.EndlessDailyCap - 45, sim.LastResult.EndlessCoins, "teto diario");
            Assert.AreEqual(0, sim.EndlessCoinsLeft); Assert.AreEqual(56, sim.EndlessBest);
            Sim s2 = Sim.Load(sim.Save());
            Assert.AreEqual(56, s2.EndlessBest); Assert.AreEqual(0, s2.EndlessCoinsLeft);
            s2.SeeDate(AddDays(d, 7));
            Assert.AreEqual(Balance.EndlessDailyCap, s2.EndlessCoinsLeft, "dia novo");
            s2.StartEndless(AddDays(d, 7)); s2.Abandon();
            Assert.AreEqual(0, s2.EndlessWeekBest, "semana nova zera o recorde da semana"); Assert.AreEqual(56, s2.EndlessBest);
        }

        // ------------------------------------------------------------------ rewarded

        [Test]
        public void Rewarded_Retomar_DaEntradaDaSala_1xPorContrato_NuncaNa1aSessao_BauNaoPaga2x_IdNoSave()
        {
            var sim = NewSim(); sim.OpenSession(T0, 20261010); sim.OpenSession(T0 + 100, 20261010);
            Assert.IsFalse(sim.FirstSession);
            Run r = Start(sim, 1);   // C2: R2 -> R4 (bau) | R3
            int id = r.Id;
            r.Courier.Hp = 3;
            Cross(sim, 0);
            Assert.AreEqual(4, r.Courier.Hp);
            Assert.AreEqual(Content.R4, r.Room.Id);
            Steps(sim, 1, Up);
            r.Courier.Pos = r.Chest.Pos + new V2(0.6f, 0f); r.Courier.Hp = 99;
            Steps(sim, StepsFor(1.4f), V2.Zero);
            Assert.IsTrue(r.Chest.Opened);
            Die(sim);
            Assert.AreEqual(Outcome.Died, sim.LastResult.Outcome); Assert.IsNotNull(sim.Pending);
            Assert.IsTrue(sim.RewardOffer(Reward.Resume));
            Assert.AreEqual("rw-" + id + "-resume", sim.RewardId(Reward.Resume));
            Assert.IsTrue(sim.GrantReward(Reward.Resume));
            Run n = sim.Run;
            Assert.IsTrue(sim.InContract); Assert.AreEqual(1, n.Layer); Assert.AreEqual(Content.R4, n.Room.Id); Assert.AreEqual(id, n.Id);
            Assert.AreEqual(sim.MaxHp, n.Courier.Hp); Assert.AreEqual(Balance.Entrance.Y, n.Courier.Pos.Y, 0.01f);
            Assert.AreEqual(CargoState.Held, n.Main.State); Assert.IsNull(sim.Pending, "a carga voltou para as maos");
            Assert.IsTrue(n.Chest.Opened, "o bau aberto antes de falhar nao paga 2x");
            Assert.IsTrue(Has(sim.Events, Ev.RewardedResume)); Assert.IsTrue(Has(sim.Events, Ev.RewardedGrant));
            Assert.AreEqual("rewarded_resume", SimEvent.DiaryName(Ev.RewardedResume));
            Die(sim);
            Assert.IsFalse(sim.RewardOffer(Reward.Resume), "1x por contrato"); Assert.IsFalse(sim.GrantReward(Reward.Resume));
            Assert.IsTrue(Sim.Load(sim.Save()).RewardGranted("rw-" + id + "-resume"), "o id fica no save");

            var first = NewSim(); first.OpenSession(T0, 20261010);
            Start(first, 0); Die(first);
            Assert.IsFalse(first.RewardOffer(Reward.Resume), "nunca na 1a sessao");
            var ab = NewSim(); ab.OpenSession(T0, 20261010); ab.OpenSession(T0 + 1, 20261010);
            Start(ab, 0); ab.Abandon();
            Assert.IsFalse(ab.RewardOffer(Reward.Resume), "desistir nao e falha que se retoma");
        }

        [Test]
        public void Rewarded_DobrarBonus_RotaSegura_BauDoDiaVisivel_1xPorId_FecharEReabrirNaoRepete()
        {
            var sim = NewSim();
            ContractResult res = Deliver(sim, 0);   // C1: 40 x 1,5 = 60; basico 40 -> bonus 20
            Assert.AreEqual(20, res.Bonus);
            int coins = sim.Coins, total = res.Total;
            Assert.IsTrue(sim.RewardOffer(Reward.DoubleBonus)); Assert.IsTrue(sim.GrantReward(Reward.DoubleBonus));
            Assert.AreEqual(coins + 20, sim.Coins); Assert.IsTrue(res.BonusDoubled); Assert.AreEqual(total + 20, res.Total);
            Assert.IsFalse(sim.GrantReward(Reward.DoubleBonus), "1x");
            Start(sim, 0); Die(sim);
            Assert.IsFalse(sim.RewardOffer(Reward.DoubleBonus), "falha nao tem bonus");

            Run r = Start(sim, 0);   // R1: portas para R2 (2 caveiras) e R3 (1)
            Assert.IsTrue(sim.RewardOffer(Reward.SafeRoute)); Assert.IsTrue(sim.GrantReward(Reward.SafeRoute));
            Assert.AreEqual(Content.R3, r.Doors[r.SafeDoor].Room);
            Assert.IsFalse(sim.RewardOffer(Reward.SafeRoute), "1x por contrato");
            sim.Abandon();

            var d = NewSim(); d.SeeDate(20261010);
            int c0 = Sim.DailyChestCoins(20261010);
            Assert.That(c0, Is.InRange(80, 120));
            Gear bp = d.DailyChestBlueprint(20261010);
            Assert.AreNotEqual(Gear.None, bp, "equipamento aberto: o bau do dia mostra 1 projeto");
            Assert.IsTrue(d.GrantReward(Reward.DailyChest));
            Assert.AreEqual(c0, d.Coins); Assert.AreEqual(1, d.Blueprints(bp));
            Assert.IsFalse(d.GrantReward(Reward.DailyChest));
            Sim back = Sim.Load(d.Save());
            Assert.IsFalse(back.RewardOffer(Reward.DailyChest), "fechar e reabrir nao repete");
            back.SeeDate(20261011);
            Assert.IsTrue(back.RewardOffer(Reward.DailyChest), "dia novo");
            back.Config = Config.Parse("{\"rewarded_on\": false}");
            Assert.IsFalse(back.RewardOffer(Reward.DailyChest), "remote config desliga");
        }

        // ------------------------------------------------------------------ interstitial e IAP

        [Test]
        public void Interstitial_EntreContratos_Nunca3PrimeirosDoDia_Teto3Contratos4Min_RemoveAds_Config()
        {
            var sim = NewSim(); sim.SeeDate(20261010);
            for (int i = 0; i < 3; i++) { Deliver(sim, 0); Assert.IsFalse(sim.InterstitialDue(T0), "contrato " + (i + 1) + " do dia"); }
            Deliver(sim, 0);
            Assert.AreEqual(4, sim.ContractsToday); Assert.IsTrue(sim.InterstitialDue(T0));
            Start(sim, 0); Assert.IsFalse(sim.InterstitialDue(T0), "nunca dentro do contrato"); sim.Abandon();
            sim.InterstitialShown(T0);
            Assert.IsTrue(Has(sim.Events, Ev.InterstitialShow));
            Deliver(sim, 0); Deliver(sim, 0);
            Assert.IsFalse(sim.InterstitialDue(T0 + 1000), "1 a cada 3 contratos");
            Deliver(sim, 0);
            Assert.IsFalse(sim.InterstitialDue(T0 + 100), "1 a cada 4 min");
            Assert.IsTrue(sim.InterstitialDue(T0 + 240));
            Assert.IsFalse(sim.InterstitialDue(T0 - 10), "relogio voltando");
            Sim b = Sim.Load(sim.Save());
            Assert.IsTrue(b.InterstitialDue(T0 + 240));
            b.SeeDate(20261011); Assert.IsFalse(b.InterstitialDue(T0 + 9999), "dia novo: 3 primeiros de novo");
            var off = Sim.Load(sim.Save()); off.Config = Config.Parse("{\"interstitial_on\": false}");
            Assert.IsFalse(off.InterstitialDue(T0 + 240));
            Assert.IsTrue(sim.Purchase(Product.RemoveAds)); Assert.IsTrue(sim.RemoveAds);
            Assert.IsFalse(sim.InterstitialDue(T0 + 240), "Remove Ads");
            Assert.IsTrue(sim.RewardOffer(Reward.DailyChest), "rewarded continua com Remove Ads");
        }

        [Test]
        public void Iap_ConteudoFixo_StarterDepoisDoC3_NaoConcedeDuasVezes_RoupasEPacoteDeEvento()
        {
            var sim = new Sim(1) { Reputation = 2 };
            Assert.IsFalse(sim.StarterOffered);
            sim.Reputation = 3; Assert.IsTrue(sim.StarterOffered);
            Assert.IsTrue(sim.Purchase(Product.StarterPack));
            Assert.AreEqual(Balance.StarterCoins, sim.Coins); Assert.IsTrue(sim.OwnsOutfit(Outfit.Explorer));
            for (int g = 1; g < GearDef.All.Length; g++) Assert.AreEqual(1, sim.Blueprints((Gear)g));
            Assert.IsFalse(sim.StarterOffered);
            Assert.IsFalse(sim.Purchase(Product.StarterPack), "restaurar nao concede de novo"); Assert.AreEqual(Balance.StarterCoins, sim.Coins);
            Assert.IsTrue(sim.Purchase(Product.OutfitRoyal)); Assert.IsTrue(sim.EquipOutfit(Outfit.Royal)); Assert.AreEqual(Outfit.Royal, sim.OutfitEquipped);
            Assert.IsTrue(Has(sim.Events, Ev.OutfitEquip));
            Assert.IsFalse(sim.EquipOutfit(Outfit.Night), "roupa nao comprada"); Assert.IsFalse(sim.OwnsOutfit(Outfit.Ghost));
            int d = Monday(20261012); sim.SeeDate(d);
            Assert.AreEqual(3, sim.EventSlots(d));
            Assert.IsTrue(sim.Purchase(Product.EventPack));
            Assert.AreEqual(5, sim.EventSlots(d)); Assert.IsTrue(sim.EventAvailable(d, 4)); Assert.IsNotNull(sim.EventContract(d, 4));
            Assert.IsFalse(sim.Purchase(Product.EventPack), "1x por semana");
            Assert.AreEqual(3, sim.EventSlots(AddDays(d, 7)), "so a semana da compra");
            Assert.AreEqual(3, sim.Reputation); Assert.AreEqual(0, sim.Stars, "nada pago vira Reputation nem estrela");
            Assert.AreEqual("iap_purchase", SimEvent.DiaryName(Ev.IapPurchase));
            Sim b = Sim.Load(sim.Save());
            Assert.IsTrue(b.Owns(Product.StarterPack)); Assert.IsTrue(b.OwnsOutfit(Outfit.Royal)); Assert.AreEqual(Outfit.Royal, b.OutfitEquipped); Assert.AreEqual(5, b.EventSlots(d));
            Assert.IsFalse(b.Purchase(Product.OutfitRoyal));
        }

        // ------------------------------------------------------------------ remote config e telemetria

        [Test]
        public void RemoteConfig_JsonPlano_PadroesIguaisAoArquivo_IgnoraLixo_OCoreUsa()
        {
            Config d = Config.Parse(null);
            Assert.IsTrue(d.InterstitialOn); Assert.AreEqual(3, d.AdFreeContracts); Assert.AreEqual(240, d.AdEverySeconds); Assert.AreEqual(8, d.RareHours);
            Config c = Config.Parse("{ \"interstitial_on\": false, \"ad_every_seconds\": 300, \"rare_hours\": 1, \"reroll_base\": \"x\", \"desconhecida\": 5 }");
            Assert.IsFalse(c.InterstitialOn); Assert.AreEqual(300, c.AdEverySeconds); Assert.AreEqual(1, c.RareHours);
            Assert.AreEqual(60, c.RerollBase, "valor invalido = padrao");
            Assert.AreEqual(new Config().RerollStep, Config.Parse("lixo").RerollStep);
            var sim = new Sim(6) { Reputation = 3, Config = c };
            sim.OpenSession(T0, 20261010); sim.OpenSession(T0 + 3700, 20261010);
            Assert.AreNotEqual(0, sim.RareDate, "rare_hours = 1");
            string file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Here()), "..", "..", "Resources", "config.json");
            if (System.IO.File.Exists(file))
            {
                Config f = Config.Parse(System.IO.File.ReadAllText(file)), n = new Config();
                Assert.AreEqual(n.InterstitialOn, f.InterstitialOn); Assert.AreEqual(n.AdFreeContracts, f.AdFreeContracts); Assert.AreEqual(n.AdEveryContracts, f.AdEveryContracts);
                Assert.AreEqual(n.AdEverySeconds, f.AdEverySeconds); Assert.AreEqual(n.RareHours, f.RareHours); Assert.AreEqual(n.RerollBase, f.RerollBase);
                Assert.AreEqual(n.RerollStep, f.RerollStep); Assert.AreEqual(n.StarterAfter, f.StarterAfter); Assert.AreEqual(n.RewardedOn, f.RewardedOn); Assert.AreEqual(n.EventsOn, f.EventsOn);
            }
        }

        static string Here([System.Runtime.CompilerServices.CallerFilePath] string p = "") => p;

        [Test]
        public void Telemetria_NomesNovosNoDiario_SemRepetir_FpsBucket()
        {
            var names = new HashSet<string>();
            foreach (Ev e in System.Enum.GetValues(typeof(Ev))) { string n = SimEvent.DiaryName(e); if (n != null) Assert.IsTrue(names.Add(n), "nome repetido: " + n); }
            foreach (string n in new[] { "device_fps_bucket", "route_choice", "room_fight_choice", "rewarded_offer", "rewarded_grant", "rewarded_resume", "interstitial_show",
                                         "iap_purchase", "event_start", "event_end", "event_reward", "season_progress", "season_claim", "modifier_pick", "endless_start",
                                         "endless_end", "rare_offer", "rare_start", "reroll", "gear_use", "blueprint_found", "mastery_up" })
                Assert.IsTrue(names.Contains(n), n);
            Assert.AreEqual(0, Sim.FpsBucket(15f)); Assert.AreEqual(1, Sim.FpsBucket(25f)); Assert.AreEqual(2, Sim.FpsBucket(40f)); Assert.AreEqual(3, Sim.FpsBucket(50f)); Assert.AreEqual(4, Sim.FpsBucket(60f));
            var sim = new Sim(1);
            sim.Log(Ev.DeviceFps, Sim.FpsBucket(58f));
            Assert.AreEqual(Ev.DeviceFps, sim.Events[0].Kind); Assert.AreEqual(4, sim.Events[0].A);
        }

        // ------------------------------------------------------------------ save v3

        [Test]
        public void SaveV3_IdaEVolta_TodosOsSistemas_EContratoComModificadorEEvento()
        {
            var sim = new Sim(11) { Reputation = Sim.Contracts.Length, Coins = 5000 };
            sim.DevStars(45);
            int d = Monday(20261012);
            sim.OpenSession(T0, d); sim.OpenSession(T0 + 9 * 3600, d);
            Assert.IsTrue(sim.Reroll(false));
            Deliver(sim, 0); Deliver(sim, 0); Deliver(sim, 0);
            Assert.IsTrue(sim.ClaimMilestone(0));
            Assert.IsTrue(sim.Purchase(Product.StarterPack)); Assert.IsTrue(sim.Purchase(Product.OutfitNight)); Assert.IsTrue(sim.EquipOutfit(Outfit.Night));
            Assert.IsTrue(sim.Purchase(Product.EventPack));
            Assert.IsTrue(sim.GrantReward(Reward.DailyChest));
            sim.InterstitialShown(T0 + 9 * 3600);
            sim.StartEndless(d); Cross(sim); sim.Abandon();
            Run r = sim.StartEvent(d, 4, false);
            Assert.IsNotNull(r);
            string a = sim.Save();
            StringAssert.StartsWith("v=3\n", a);
            Sim b = Sim.Load(a);
            Assert.IsFalse(b.Dirty);
            Assert.AreEqual(a, b.Save(), "ida e volta exata");
            Assert.AreEqual(2, b.Sessions); Assert.AreEqual(d, b.RareDate); Assert.AreEqual(3, b.MasteryCount(CargoType.Box)); Assert.IsTrue(b.MilestoneClaimed(0));
            Assert.IsTrue(b.Owns(Product.StarterPack)); Assert.AreEqual(Outfit.Night, b.OutfitEquipped); Assert.AreEqual(5, b.EventSlots(d));
            Assert.IsTrue(b.RewardGranted("rw-" + d + "-chest")); Assert.AreEqual(1, b.EndlessBest); Assert.AreEqual(sim.SeasonProgress(0), b.SeasonProgress(0));
            Assert.AreEqual(ContractKind.Event, b.Run.Contract.Kind); Assert.AreEqual(4, b.Run.Contract.Slot); Assert.AreEqual(Fp(r.Contract), Fp(b.Run.Contract));

            var m = NewSim(); m.DevStars(40);
            Run rm = m.StartContract(4, false, Modifier.Fragile | Modifier.Rush);
            rm.Main.Cracks = 1;
            Sim mb = Sim.Load(m.Save());
            Assert.AreEqual(Modifier.Fragile | Modifier.Rush, mb.Run.Mods); Assert.AreEqual(2, mb.Run.BreakAt); Assert.AreEqual(1, mb.Run.Main.Cracks);
            var e = new Sim(8) { Reputation = Sim.Contracts.Length };
            e.StartEndless(d); Cross(e); Cross(e);
            Sim eb = Sim.Load(e.Save());
            Assert.AreEqual(ContractKind.Endless, eb.Run.Contract.Kind); Assert.AreEqual(2, eb.Run.Layer); Assert.AreEqual(e.Run.Tier, eb.Run.Tier);
            var rr = new Sim(6) { Reputation = 3 }; rr.OpenSession(T0, 20261010); rr.OpenSession(T0 + 9 * 3600, 20261010);
            rr.StartRare(false);
            Sim rb = Sim.Load(rr.Save());
            Assert.AreEqual(ContractKind.Rare, rb.Run.Contract.Kind); Assert.AreEqual(rr.Run.Contract.ShareCode, rb.Run.Contract.ShareCode);
        }

        [Test]
        public void SaveV2_MigraParaV3_MantemContratoEmCurso_DiaDaBetaNaRegiao1_SistemasNoPadrao()
        {
            const string v2 = "v=2\nseed=4\nruns=6\nnid=9\ncoins=500\nrep=6\nlv=1,1,1,1\nstars=333222\nwpn=0\nwlv=1,1,1\ndseen=20261010\nddone=0\n" +
                              "run=4,0,0,6,12.5,77,0,0,0,0,0,0,0,0\nrk=0,0,0,0,0,-1,-1,-1\ncg0=8,1,1,0,0\n";
            Sim s = Sim.Load(v2);
            Assert.IsFalse(s.SaveRefused); Assert.IsTrue(s.Dirty, "migracao tem de regravar em v3");
            Assert.IsTrue(s.InContract, "o contrato em curso da v2 continua (a regiao 1 nao mudou)");
            Assert.AreEqual(4, s.Run.Contract.Index); Assert.AreEqual(1, s.Run.Main.Cracks); Assert.AreEqual(6, s.Run.Courier.Hp); Assert.AreEqual(5, s.Run.Id);
            Assert.AreEqual(15, s.Stars); Assert.AreEqual(Modifier.None, s.Run.Mods);
            Assert.AreEqual(0, s.Sessions); Assert.AreEqual(Gear.None, s.GearEquipped); Assert.AreEqual(Sim.SeasonOf(20261010), s.SeasonId);
            Assert.AreEqual(0, s.MasteryCount(CargoType.Box)); Assert.IsFalse(s.Owns(Product.RemoveAds));
            string v3 = s.Save();
            StringAssert.StartsWith("v=3\n", v3);
            Assert.AreEqual(v3, Sim.Load(v3).Save());

            // contrato do dia da beta em curso: o payload antigo (bits altos sorteados) volta na regiao 1, igual ao da beta
            int d = 20261010;
            while (Content.RegionOf(Sim.DailyPayload(d)) == 1) d = AddDays(d, 1);
            int p = Sim.DailyPayload(d);
            ContractDef beta = Content.Generate(p, ContractKind.Daily, d, 1);
            string day = "v=2\nseed=4\nrep=3\nrun=-1,0," + beta.Layers[0][0] + ",6,3,77,0,0,0,0,0,0,0,0\nrk=1," + p + "," + d + ",0,0,-1,-1,-1\ncg0=3," + (int)beta.Cargo + ",0,0,0\n";
            Sim sd = Sim.Load(day);
            Assert.IsTrue(sd.InContract, "dia da beta em curso");
            Assert.AreEqual(ContractCode.Encode(p), sd.Run.Contract.ShareCode); Assert.AreEqual(1, sd.Run.Contract.Region); Assert.AreEqual(Fp(beta), Fp(sd.Run.Contract));
            Assert.IsNull(Sim.Load(day.Replace("v=2", "v=3")).Run, "em v3 o mesmo payload aponta para outra regiao (sem conteudo)");
        }
    }
}
