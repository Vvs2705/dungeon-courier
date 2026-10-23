using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Regiao 2 "Minas Alagadas" (CONTEUDO_REGIOES §2, §5, §6; Run.Regiao2.cs): desenho das salas, terreno, inimigos, chefes e cargas.
    /// So API publica. Cada sala e montada pelo save (contrato em curso na entrada da sala), como no jogo.</summary>
    public partial class CoreTests
    {
        // ------------------------------------------------------------------ helpers da regiao 2

        static int RoomOf(string code) { foreach (RoomDef d in Content.Rooms) if (d.Code == code) return d.Id; Assert.Fail("sala " + code); return -1; }
        static int ContractOf(string code) { foreach (ContractDef d in Sim.Contracts) if (d.Code == code) return d.Index; Assert.Fail("contrato " + code); return -1; }

        /// <summary>Contrato da regiao 2 em curso na entrada da sala `room` (com a carga `cargo` se pedida), courier com 99 de vida.</summary>
        static Sim R2At(string room, CargoType? cargo = null)
        {
            int id = RoomOf(room);
            foreach (ContractDef c in RegionDef.Of(2).Contracts)
            {
                if (cargo.HasValue && c.Cargo != cargo.Value) continue;
                for (int l = 0; l < c.Layers.Length; l++)
                {
                    if (System.Array.IndexOf(c.Layers[l], id) < 0) continue;
                    Sim s = Sim.Load("v=3\nseed=7\nrep=" + Sim.Contracts.Length + "\nrun=" + c.Index + "," + l + "," + id + ",20,0,99,0,0,0,0,0,0,0,0\nrk=0,0,0,0,0,-1,-1,-1\ncg0=1," + (int)c.Cargo + ",0,0,0\n");
                    Assert.IsNotNull(s.Run, room);
                    s.Run.Courier.MaxHp = 99; s.Run.Courier.Hp = 99;
                    return s;
                }
            }
            Assert.Fail("nenhum contrato da regiao 2 passa por " + room + " com " + cargo);
            return null;
        }

        static void Ground(Run r, V2 p) { r.Main.State = CargoState.Ground; r.Main.Pos = p; }
        /// <summary>Passos tirando os ladroes (a carga sozinha no chao chama um em 8 s): isola a regra testada.</summary>
        static List<SimEvent> NoThief(Sim sim, int n, V2 move, bool dashFirst = false)
        {
            var evs = new List<SimEvent>();
            for (int i = 0; i < n && sim.InContract; i++) { sim.Run.Enemies.RemoveAll(e => e.Type == EnemyType.Thief); sim.Tick(Dt, move, dashFirst && i == 0); evs.AddRange(sim.Events); }
            return evs;
        }
        static int CountEv(List<SimEvent> evs, Ev k, int a) { int n = 0; foreach (SimEvent e in evs) if (e.Kind == k && e.A == a) n++; return n; }
        static float WaterDist(RoomDef room, V2 p) { float d = float.MaxValue; foreach (Rect w in room.Water) d = System.Math.Min(d, V2.Dist(p, w.Closest(p))); return d; }

        // ------------------------------------------------------------------ desenho

        [Test]
        public void LevelDesign_Regioes_Regiao2_SalasContratosETerreno()
        {
            RegionDef g = RegionDef.Of(2);
            Assert.IsTrue(g.HasContent); Assert.AreEqual("minas", g.Theme); Assert.AreEqual(30, g.Stars); Assert.AreEqual(24, g.First);
            Assert.AreEqual(14, g.Rooms.Length); Assert.AreEqual(10, g.Normal.Length); Assert.AreEqual(1, g.Secrets.Length); Assert.AreEqual(16, g.Contracts.Length);
            string[] codes = { "R2-1", "R2-2", "R2-3", "R2-4", "R2-5", "R2-6", "R2-7", "R2-8", "R2-9", "R2-10", "S2-1", "M2", "B2", "D2" };
            for (int i = 0; i < codes.Length; i++) Assert.AreEqual(codes[i], Content.Rooms[g.Rooms[i]].Code, "ids so crescem no fim (save)");
            Assert.AreEqual(RoomOf("M2"), g.Mini); Assert.AreEqual(RoomOf("B2"), g.Boss); Assert.AreEqual(RoomOf("D2"), g.Delivery);
            Assert.AreEqual(ContractOf("M2-8"), g.MiniAt); Assert.AreEqual(ContractOf("M2-16"), g.BossAt);
            int buriedSeen = 0, rails = 0, water = 0, asleep = 0;
            foreach (int id in g.Rooms)
            {
                RoomDef room = Content.Rooms[id]; string k = room.Code;
                Assert.AreEqual(2, room.Region, k);
                var nav = new Nav(room);
                foreach (Spawn s in room.Wave)
                {
                    Assert.GreaterOrEqual(V2.Dist(s.Pos, Balance.Entrance), 4f, k + ": reforco em cima da entrada");
                    Assert.IsFalse(Run.Blocked(room, s.Pos, EnemyDef.All[(int)s.Type].Radius), k + ": reforco dentro de obstaculo");
                }
                foreach (Spawn s in room.Spawns)
                {
                    if (s.Asleep) asleep++;
                    if (EnemyDef.All[(int)s.Type].Locks) Assert.IsTrue(!room.Normal || s.Type == EnemyType.Guard, k + ": so Guarda, chefes e emboscada obrigam a lutar");
                }
                // agua: nao solida, a 1,2 m+ da entrada
                foreach (Rect w in room.Water) { water++; Assert.GreaterOrEqual(V2.Dist(w.Closest(Balance.Entrance), Balance.Entrance), 1.2f, k + ": agua na entrada"); }
                // trilho: 4 <= y <= 11,5, de parede a parede, sem obstaculo em cima, vagonete numa ponta
                for (int i = 0; i < room.Rails.Length; i++)
                {
                    Rail t = room.Rails[i]; rails++;
                    Assert.That(t.A.Y, Is.InRange(4f, 11.5f), k + ": trilho fora de 4-11,5"); Assert.That(t.B.Y, Is.InRange(4f, 11.5f), k);
                    Assert.AreEqual(Balance.RoomW, V2.Dist(t.A, t.B), 1e-3f, k + ": trilho de parede a parede");
                    foreach (Rect o in room.Obstacles) Assert.GreaterOrEqual(System.Math.Abs(o.C.Y - t.A.Y), o.H.Y + 0.6f, k + ": obstaculo em cima do trilho");
                    int carts = 0;
                    foreach (Spawn s in room.Spawns) if (s.Type == EnemyType.Minecart && System.Math.Min(V2.Dist(s.Pos, t.A), V2.Dist(s.Pos, t.B)) <= 1f) carts++;
                    Assert.AreEqual(1, carts, k + ": um vagonete numa ponta de cada trilho");
                    var marks = new List<V2> { room.RescueSpot };
                    if (room.ChestCoins > 0) marks.Add(room.ChestPos);
                    foreach (BuriedChest b in room.Buried) marks.Add(b.Pos);
                    foreach (Spawn s in room.Spawns) if (s.Type != EnemyType.Minecart) marks.Add(s.Pos);
                    foreach (Spawn s in room.Wave) marks.Add(s.Pos);
                    foreach (V2 p in marks) Assert.GreaterOrEqual(V2.SegDist(p, t.A, t.B), 1f, k + ": spawn/bau/resgate a menos de 1 m do trilho em " + p);
                }
                foreach (Spawn s in room.Spawns) if (s.Type == EnemyType.Minecart) Assert.Greater(room.Rails.Length, 0, k + ": vagonete sem trilho");
                // bau enterrado: fora de obstaculo, alcancavel
                foreach (BuriedChest b in room.Buried)
                {
                    buriedSeen++;
                    Assert.IsFalse(Run.Blocked(room, b.Pos, 0.3f), k + ": bau enterrado dentro de obstaculo");
                    Assert.Less(nav.Steps(Balance.Entrance, b.Pos), int.MaxValue, k + ": bau enterrado inalcancavel");
                    Assert.Greater(b.Coins, 0, k);
                }
                // parede rachada na lateral, alcancavel, leva a sala secreta da regiao
                if (room.Secret >= 0)
                {
                    Assert.IsTrue(Content.Rooms[room.Secret].IsSecret && Content.Rooms[room.Secret].Region == 2, k);
                    Assert.IsTrue(room.SecretPos.X == 0f || room.SecretPos.X == Balance.RoomW, k + ": parede rachada fora da lateral");
                    V2 inside = new V2(room.SecretPos.X == 0f ? 0.5f : Balance.RoomW - 0.5f, room.SecretPos.Y);
                    Assert.IsFalse(Run.Blocked(room, inside, Balance.CourierR), k + ": parede rachada atras de obstaculo");
                    Assert.Less(nav.Steps(Balance.Entrance, inside), int.MaxValue, k + ": parede rachada inalcancavel");
                }
            }
            Assert.AreEqual(4, rails, "trilhos: R2-2, R2-7 (2) e B2"); Assert.GreaterOrEqual(water, 7); Assert.AreEqual(2, buriedSeen); Assert.AreEqual(4, asleep, "mineiros dormindo: R2-3 (2), R2-9, S2-1");
            Assert.AreEqual(Gear.Boots, Content.Rooms[RoomOf("R2-1")].Blueprint); Assert.AreEqual(Gear.Boots, Content.Rooms[RoomOf("R2-5")].Blueprint);
            Assert.AreEqual(Gear.Boots, Content.Rooms[RoomOf("S2-1")].Blueprint); Assert.AreEqual(Gear.Smoke, Content.Rooms[RoomOf("R2-7")].Blueprint);

            // contratos: 3 tutoriais sem reforco, 4-6 camadas do 4o em diante, nivel 8 + indice, cargas novas estreiam no contrato delas
            var debut = new Dictionary<CargoType, int>();
            for (int i = 0; i < g.Contracts.Length; i++)
            {
                ContractDef d = g.Contracts[i];
                int layers = d.Layers.Length - 1;
                bool boss = d.Layers[layers - 1][0] == g.Mini || d.Layers[layers - 1][0] == g.Boss;
                Assert.AreEqual("M2-" + (i + 1), d.Code); Assert.AreEqual(g.First + i, d.Index); Assert.AreEqual(2, d.Region); Assert.AreEqual(8 + i, d.Tier, d.Code);
                Assert.AreEqual(i >= 3, d.Waves, d.Code + ": tutoriais sem reforco");
                if (i < 3) Assert.That(layers, Is.InRange(2, 3), d.Code); else Assert.That(layers - (boss ? 1 : 0), Is.InRange(4, 6), d.Code);
                Assert.AreEqual(i == 7, boss && d.Layers[layers - 1][0] == g.Mini, d.Code + ": Rainha so no M2-8");
                Assert.AreEqual(i == 15, boss && d.Layers[layers - 1][0] == g.Boss, d.Code + ": Golem so no M2-16");
                Assert.AreEqual(g.Delivery, d.Layers[layers][0], d.Code);
                Assert.Greater(d.ParTime, 0f, d.Code); Assert.AreEqual(d.Cargo == CargoType.Message, d.HasTimer, d.Code);
                Assert.AreEqual(d.Cargo == CargoType.Egg || d.Cargo == CargoType.IceCrystal, d.EggTarget > 0f, d.Code);
                if (d.Cargo == CargoType.IceCrystal) Assert.AreEqual(d.EggTarget, d.MeltTime, d.Code);
                if (!debut.ContainsKey(d.Cargo)) debut[d.Cargo] = i;
            }
            Assert.AreEqual(2, debut[CargoType.IceCrystal], "cristal estreia no M2-3"); Assert.AreEqual(4, debut[CargoType.ArcaneHeart], "coracao no M2-5");
            Assert.AreEqual(9, debut[CargoType.GriffinCage], "gaiola no M2-10");
            Assert.AreEqual(10, RegionDef.Of(2).CargoCount);
        }

        /// <summary>Achado do bot da regiao 2 (o dash da gaiola): a porta aberta no passo anterior nao deixa passar o courier cujo dash acabou
        /// de largar a carga (antes ele atravessava sem ela e o contrato travava na sala seguinte).</summary>
        [Test]
        public void Porta_DashQueLargaACargaNaoAtravessa()
        {
            Sim s = R2At("R2-1"); Run r = s.Run; r.Enemies.Clear();
            r.Courier.Pos = new V2(r.Doors[0].Pos.X, 13.2f);   // a um passo de dash do limiar da porta
            Steps(s, 1, V2.Zero);
            Assert.IsTrue(r.Doors[0].Open, "carga nas maos: porta aberta");
            int layer = r.Layer;
            Steps(s, 6, Up, dashFirst: true);
            Assert.AreEqual(layer, r.Layer, "o dash largou a carga: nao atravessa sem ela");
            Assert.AreEqual(CargoState.Ground, r.Main.State); Assert.IsTrue(s.InContract);
        }

        // ------------------------------------------------------------------ inimigos

        [Test]
        public void Vagonete_Sino12sAntes_PassaA6ms_AtropelaCourierCargaEInimigo_NaoEAlvo()
        {
            Sim s = R2At("R2-2"); Run r = s.Run;
            r.Enemies.RemoveAll(e => e.Type != EnemyType.Minecart);
            Enemy cart = r.Enemies[0]; R2Foe f = r.R2Of(cart);
            Assert.AreEqual(0, f.Rail); Assert.AreEqual(R2Act.Tunnel, f.Act);
            Ground(r, new V2(1.5f, 3.0f));
            r.Courier.Pos = new V2(4.5f, 6.5f);   // em cima do trilho, parado (sem mexer nao pousa nem atira)
            Assert.IsFalse(r.CanHit(cart), "o vagonete nao e alvo");
            var evs = NoThief(s, StepsFor(1.9f), V2.Zero);
            Assert.AreEqual(0, CountEv(evs, Ev.Telegraph, cart.Id), "tunel 2 s");
            evs = NoThief(s, StepsFor(0.2f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Telegraph, cart.Id), "sino aos 2 s"); Assert.AreEqual(R2Act.Bell, f.Act);
            Assert.AreEqual(Hint.CartBell, r.Hint);
            Enemy brute = r.Spawn(EnemyType.Brute, new V2(7.2f, 6.5f)); brute.Stun = 99f; brute.Hp = 99;
            evs = NoThief(s, StepsFor(1.9f), V2.Zero);   // ate 4,0 s
            Assert.AreEqual(1, CountEv(evs, Ev.Strike, cart.Id), "passa aos 3,2 s");
            int dmg = 0; foreach (SimEvent e in evs) if (e.Kind == Ev.DamageTaken) dmg += e.A;
            Assert.AreEqual(cart.Def.Damage, dmg, "atropelou o courier: 2");
            Assert.GreaterOrEqual(System.Math.Abs(r.Courier.Pos.Y - 6.5f), 1.4f, "empurrado para fora do trilho");
            NoThief(s, StepsFor(0.7f), V2.Zero);   // 4,7 s: chegou ao outro lado (9 m em 1,5 s)
            Assert.AreEqual(99 - Balance.CartEnemyDamage, brute.Hp, "inimigo leva 6 (sem escala)");
            Assert.GreaterOrEqual(System.Math.Abs(brute.Pos.Y - 6.5f), 1.4f);
            Assert.AreEqual(Balance.RoomW, cart.Pos.X, 0.25f);
            NoThief(s, StepsFor(0.3f), V2.Zero);
            Assert.AreEqual(R2Act.Tunnel, f.Act);
            // carga no chao no trilho: empurrada 1,5 m + 1 rachadura (volta: sino aos 6,7 s, passa aos 7,9 s, em x = 3 aos 8,9 s)
            Ground(r, new V2(3f, 6.5f)); r.Courier.Pos = new V2(2f, 2f);
            NoThief(s, StepsFor(5.2f), V2.Zero);
            Assert.AreEqual(1, r.Main.Cracks); Assert.GreaterOrEqual(System.Math.Abs(r.Main.Pos.Y - 6.5f), 1.4f);
            Assert.AreEqual(cart.Def.Hp, cart.Hp); Assert.IsTrue(cart.Alive);
        }

        [Test]
        public void Mineiro_CirculoOndeOCourierEstava_SairEscapa_RachaACargaNoChao_RecuaA3m()
        {
            // parado no circulo: leva 1
            Sim s = R2At("R2-1"); Run r = s.Run; r.Enemies.Clear();
            r.Courier.Pos = new V2(1.5f, 3.5f);
            Enemy m = r.Spawn(EnemyType.GoblinMiner, new V2(1.0f, 9.5f)); m.Hp = 99;
            var evs = Steps(s, 2, V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Telegraph, m.Id)); R2Foe f = r.R2Of(m);
            Assert.AreEqual(R2Act.Throw, f.Act); Assert.Less(V2.Dist(f.Spot, r.Courier.Pos), 0.01f, "circulo onde o courier estava"); Assert.AreEqual(Balance.MinerCircle, f.Radius);
            evs = Steps(s, StepsFor(0.9f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Strike, m.Id)); Assert.IsTrue(Has(evs, Ev.DamageTaken), "ficou no circulo");
            // saiu do circulo no aviso: nada; a carga deixada no circulo racha
            Sim s2 = R2At("R2-1"); Run r2 = s2.Run; r2.Enemies.Clear();
            r2.Courier.Pos = new V2(1.5f, 3.5f);
            Enemy m2 = r2.Spawn(EnemyType.GoblinMiner, new V2(1.0f, 9.5f)); m2.Hp = 99;
            Steps(s2, 2, V2.Zero);
            Ground(r2, r2.R2Of(m2).Spot); r2.Courier.Pos = new V2(7.5f, 2.5f);
            evs = Steps(s2, StepsFor(0.95f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Strike, m2.Id));
            Assert.IsFalse(Has(evs, Ev.DamageTaken), "fora do circulo nao leva");
            Assert.AreEqual(1, r2.Main.Cracks, "a picareta racha a carga no chao no circulo");
            // recua com o courier a 3 m
            Sim s3 = R2At("R2-1"); Run r3 = s3.Run; r3.Enemies.Clear();
            Enemy m3 = r3.Spawn(EnemyType.GoblinMiner, new V2(2.0f, 3.0f)); m3.Hp = 99; m3.Cd = 99f;   // ja arremessou: recarregando, recua
            r3.Courier.Pos = new V2(2.0f, 1.2f);
            float d0 = V2.Dist(m3.Pos, r3.Courier.Pos);
            Steps(s3, StepsFor(0.5f), V2.Zero);
            Assert.AreEqual(EnemyState.Flee, m3.State); Assert.Greater(V2.Dist(m3.Pos, r3.Courier.Pos), d0 + 0.4f, "recua");
            m3.Cd = 0f; r3.Courier.Pos = m3.Pos + new V2(0f, -1.8f);
            Steps(s3, 1, V2.Zero);
            Assert.AreEqual(EnemyState.Telegraph, m3.State, "colado e recarregado: arremessa antes de fugir");
        }

        [Test]
        public void Toupeira_PorBaixoIntocavel_SaiSobACargaNoChao_Empurra15mParaLongeDoCourier_NaAguaNaoSai()
        {
            Sim s = R2At("R2-4", CargoType.Box); Run r = s.Run; r.Enemies.Clear();
            Enemy m = r.Spawn(EnemyType.Mole, new V2(4.5f, 10f)); m.Hp = 99;
            V2 c0 = new V2(3.0f, 5.0f);
            Ground(r, c0); r.Courier.Pos = new V2(5.0f, 6.5f);
            Assert.IsTrue(r.Underground(m)); Assert.IsFalse(r.CanHit(m), "por baixo nao e alvo");
            NoThief(s, 1, Up);   // mexeu: parado depois disso, atira em quem for alvo
            bool mound = false, hitUnder = false; List<SimEvent> evs = null;
            for (int i = 0; i < StepsFor(6f) && !mound; i++)
            {
                evs = NoThief(s, 1, V2.Zero);
                if (CountEv(evs, Ev.EnemyHit, m.Id) > 0) hitUnder = true;
                if (CountEv(evs, Ev.Telegraph, m.Id) > 0) mound = true;
            }
            Assert.IsTrue(mound, "nao fez o monte"); Assert.IsFalse(hitUnder, "levou tiro por baixo");
            Assert.Less(V2.Dist(r.R2Of(m).Spot, c0), 0.5f, "monte embaixo da carga"); Assert.AreEqual(Hint.MoleTrail, r.Hint);
            V2 cp = r.Courier.Pos;
            evs = NoThief(s, StepsFor(0.85f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Strike, m.Id));
            V2 moved = r.Main.Pos - c0;
            Assert.GreaterOrEqual(moved.Len, 1.3f, "empurrada 1,5 m"); Assert.Greater(V2.Dot(moved, c0 - cp), 0f, "para longe do courier");
            Assert.AreEqual(0, r.Main.Cracks, "caixa empurrada nao racha");
            Assert.AreEqual(R2Act.Up, r.R2Of(m).Act); Assert.IsFalse(r.Underground(m)); Assert.IsTrue(r.CanHit(m), "fora (2 s) e alvo");
            evs = NoThief(s, StepsFor(2.1f), V2.Zero);
            Assert.Greater(CountEv(evs, Ev.EnemyHit, m.Id), 0, "fora leva tiro");
            Assert.AreEqual(1, CountEv(evs, Ev.Burrow, m.Id)); Assert.IsTrue(r.Underground(m));
            // agua: nao sai; espera a 1,5 m da borda
            Sim s2 = R2At("R2-4", CargoType.Box); Run r2 = s2.Run; r2.Enemies.Clear();
            Enemy m2 = r2.Spawn(EnemyType.Mole, new V2(4.5f, 10f));
            Ground(r2, new V2(7.4f, 4.6f)); r2.Courier.Pos = new V2(6.2f, 4.6f);
            Assert.IsTrue(r2.InWater(r2.Main.Pos) && r2.InWater(r2.Courier.Pos));
            evs = NoThief(s2, StepsFor(7f), V2.Zero);
            Assert.AreEqual(0, CountEv(evs, Ev.Telegraph, m2.Id), "na agua nao emerge");
            Assert.GreaterOrEqual(WaterDist(r2.Room, m2.Pos), Balance.MoleWaterWait - 0.15f);
        }

        [Test]
        public void Sapo_PuxaACargaDoChaoAte08mDele_SoNoAnelDe3m_SemCargaNoChaoALinguaAcertaOCourier()
        {
            Sim s = R2At("R2-5", CargoType.Box); Run r = s.Run; r.Enemies.Clear();
            Enemy f = r.Spawn(EnemyType.BogFrog, new V2(2.0f, 9.5f)); f.Hp = 99;
            Ground(r, new V2(4.5f, 9.2f)); r.Courier.Pos = new V2(7.5f, 2.5f);
            var evs = Steps(s, 2, V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Telegraph, f.Id), "aviso da lingua");
            Assert.AreEqual(Hint.FrogTongue, r.Hint);
            evs = Steps(s, StepsFor(0.75f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Strike, f.Id));
            Assert.LessOrEqual(V2.Dist(r.Main.Pos, f.Pos), Balance.FrogPull + 0.05f, "puxou ate 0,8 m dele");
            Assert.AreEqual(0, r.Main.Cracks, "arrastar nao racha"); Assert.AreEqual(CargoState.Ground, r.Main.State);
            // fora do anel: nada
            Sim s2 = R2At("R2-5", CargoType.Box); Run r2 = s2.Run; r2.Enemies.Clear();
            Enemy f2 = r2.Spawn(EnemyType.BogFrog, new V2(2.0f, 9.5f));
            Ground(r2, new V2(5.6f, 9.2f)); r2.Courier.Pos = new V2(7.5f, 2.5f);
            V2 p0 = r2.Main.Pos;
            evs = Steps(s2, StepsFor(2f), V2.Zero);
            Assert.AreEqual(0, CountEv(evs, Ev.Telegraph, f2.Id)); Assert.AreEqual(p0.X, r2.Main.Pos.X, 1e-4f);
            // carga nas maos e o courier no anel: a lingua acerta o courier
            Sim s3 = R2At("R2-5", CargoType.Box); Run r3 = s3.Run; r3.Enemies.Clear();
            Enemy f3 = r3.Spawn(EnemyType.BogFrog, new V2(2.0f, 9.5f));
            r3.Courier.Pos = new V2(4.5f, 8.5f);
            evs = Steps(s3, StepsFor(0.9f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Strike, f3.Id)); Assert.IsTrue(Has(evs, Ev.DamageTaken));
        }

        [Test]
        public void Besouro_Pavio1sExplode_FereCourierEInimigos_RachaACargaNoChao_TiroExplodeNaHora_DashNaoDesarma()
        {
            Sim s = R2At("R2-6", CargoType.Box); Run r = s.Run; r.Enemies.Clear();
            r.Courier.Pos = new V2(4.5f, 3.0f);
            Enemy b = r.Spawn(EnemyType.BombBeetle, new V2(4.5f, 4.25f));
            Enemy brute = r.Spawn(EnemyType.Brute, new V2(5.6f, 4.4f)); brute.Stun = 99f; brute.Hp = 99;
            Ground(r, new V2(3.7f, 4.6f));
            var evs = Steps(s, 2, V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Telegraph, b.Id), "pavio"); Assert.AreEqual(R2Act.Fuse, r.R2Of(b).Act);
            Assert.AreEqual(Hint.BeetleFuse, r.Hint);
            evs = Steps(s, StepsFor(0.9f), V2.Zero);
            Assert.IsFalse(Has(evs, Ev.Explosion), "antes de 1 s");
            evs = Steps(s, StepsFor(0.15f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Explosion, 2), "explodiu (fonte 2 = besouro)");
            int dmg = 0; foreach (SimEvent e in evs) if (e.Kind == Ev.DamageTaken) dmg += e.A;
            Assert.AreEqual(2, dmg); Assert.AreEqual(99 - Balance.BeetleEnemyDamage, brute.Hp, "inimigo no raio leva 4"); Assert.AreEqual(1, r.Main.Cracks);
            Assert.IsFalse(r.Enemies.Contains(b), "o besouro vai junto");
            // morto por tiro: explode na hora onde estiver
            Sim s2 = R2At("R2-6", CargoType.Box); Run r2 = s2.Run; r2.Enemies.Clear();
            Enemy b2 = r2.Spawn(EnemyType.BombBeetle, new V2(4.5f, 4.6f)); b2.Stun = 99f;
            Steps(s2, 1, Up); evs = Steps(s2, StepsFor(2f), V2.Zero);
            bool died = false; V2 at = new V2(-9f, -9f);
            foreach (SimEvent e in evs) { if (e.Kind == Ev.EnemyDie && e.A == b2.Id) died = true; if (e.Kind == Ev.Explosion && e.A == 2 && died) at = e.Pos; }
            Assert.IsTrue(died); Assert.Less(V2.Dist(at, b2.Pos), 0.05f, "explodiu onde morreu");
            // o dash o empurra sem desarmar
            Sim s3 = R2At("R2-6", CargoType.Box); Run r3 = s3.Run; r3.Enemies.Clear();
            r3.Courier.Pos = new V2(4.5f, 3.0f);
            Enemy b3 = r3.Spawn(EnemyType.BombBeetle, new V2(4.5f, 4.25f));
            Steps(s3, 2, V2.Zero);
            Assert.AreEqual(R2Act.Fuse, r3.R2Of(b3).Act);
            evs = Steps(s3, StepsFor(1.2f), Up, dashFirst: true);
            Assert.IsTrue(Has(evs, Ev.Dash)); Assert.AreEqual(1, CountEv(evs, Ev.Explosion, 2), "empurrado, mas explode");
        }

        // ------------------------------------------------------------------ chefes

        [Test]
        public void RainhaToupeira_SaiSobACargaForaDaAgua_Empurra25ERacha_ChamaToupeiras_Fase2DuasEmersoes_NaPocaNaoSai()
        {
            Sim s = R2At("M2"); Run r = s.Run;
            Enemy q = r.Enemies.Find(e => e.Type == EnemyType.MoleQueen);
            r.Enemies.RemoveAll(e => e.Type == EnemyType.Mole);
            q.Hp = 999;   // vida alta: sem fase 2 aqui
            Assert.IsTrue(r.Doors[0].Locked, "tranca");
            V2 c0 = new V2(4.5f, 5.0f);
            Ground(r, c0); r.Courier.Pos = new V2(4.5f, 2.6f);
            var evs = NoThief(s, StepsFor(2.9f), V2.Zero);
            Assert.AreEqual(0, CountEv(evs, Ev.Telegraph, q.Id), "3 s por baixo"); Assert.IsFalse(r.CanHit(q));
            evs = NoThief(s, StepsFor(0.2f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Telegraph, q.Id)); Assert.Less(V2.Dist(r.R2Of(q).Spot, c0), 0.3f, "monte embaixo da carga");
            evs = NoThief(s, StepsFor(1.0f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Strike, q.Id));
            Assert.GreaterOrEqual(V2.Dist(r.Main.Pos, c0), 2.3f, "empurrada 2,5 m"); Assert.AreEqual(1, r.Main.Cracks, "+1 rachadura");
            Assert.IsTrue(r.CanHit(q), "fora: alvo");
            int calls = 0;
            for (int i = 0; i < StepsFor(12f); i++) { evs = NoThief(s, 1, V2.Zero); calls += CountEv(evs, Ev.Summon, q.Id); }
            Assert.AreEqual(1, calls, "chama toupeiras na 2a emersao");
            Assert.That(r.Enemies.FindAll(e => e.Type == EnemyType.Mole).Count, Is.InRange(1, Balance.QueenMoles));
            // fase 2 (<= 50%): 2 s por baixo e 2 emersoes seguidas (2 de dano cada), o 2o monte 0,6 s depois
            Sim s2 = R2At("M2"); Run r2 = s2.Run;
            Enemy q2 = r2.Enemies.Find(e => e.Type == EnemyType.MoleQueen);
            r2.Enemies.RemoveAll(e => e.Type == EnemyType.Mole);
            q2.Hp = q2.Hp / 2;
            evs = Steps(s2, 1, V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.BossPhase, q2.Id));
            var hits = new List<float>();
            for (int i = 0; i < StepsFor(4.0f); i++) foreach (SimEvent e in Steps(s2, 1, V2.Zero)) if (e.Kind == Ev.DamageTaken && e.A == 2) hits.Add(r2.Time);
            Assert.AreEqual(2, hits.Count, "duas emersoes no courier"); Assert.AreEqual(Balance.QueenSecond, hits[1] - hits[0], 0.1f);
            Assert.Less(hits[0], 2.0f + 1.0f + 0.15f, "2 s por baixo na fase 2");
            // carga e courier nas pocas: o monte sai na borda seca e nao empurra o que esta na agua
            Sim s3 = R2At("M2"); Run r3 = s3.Run;
            Enemy q3 = r3.Enemies.Find(e => e.Type == EnemyType.MoleQueen); q3.Hp = 999;
            r3.Enemies.RemoveAll(e => e.Type == EnemyType.Mole);
            Ground(r3, new V2(1.6f, 6.0f)); r3.Courier.Pos = new V2(7.4f, 6.0f);
            NoThief(s3, StepsFor(3.1f), V2.Zero);
            Assert.AreEqual(R2Act.Mound, r3.R2Of(q3).Act);
            Assert.IsFalse(r3.InWater(r3.R2Of(q3).Spot), "na agua nao sai");
            V2 cargo = r3.Main.Pos;
            NoThief(s3, StepsFor(1.0f), V2.Zero);
            Assert.AreEqual(cargo.X, r3.Main.Pos.X, 1e-4f, "o que esta na agua nao e empurrado");
        }

        [Test]
        public void Golem_PancadaEmArea_DuasPedras_PontoFracoNasCostas_Fase2InvestidaBateEAtordoa_VagoneteAcerta()
        {
            Sim s = R2At("B2"); Run r = s.Run;
            Enemy g = r.Enemies.Find(e => e.Type == EnemyType.StoneGolem);
            r.Enemies.RemoveAll(e => e.Type == EnemyType.Minecart);
            Assert.IsTrue(r.Doors[0].Locked);
            // pancada: 3 de dano na area de 2,2 m, depois 2 pedras (sombra 0,8 s, obstaculo)
            r.Courier.Pos = new V2(4.5f, 7.6f);
            var evs = Steps(s, StepsFor(1.2f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Telegraph, g.Id)); Assert.AreEqual(1, CountEv(evs, Ev.Strike, g.Id));
            int dmg = 0; foreach (SimEvent e in evs) if (e.Kind == Ev.DamageTaken) dmg += e.A;
            Assert.AreEqual(3, dmg);
            Assert.AreEqual(2, r.Rocks.Count); foreach (Rock k in r.Rocks) { Assert.IsFalse(k.Landed); Assert.GreaterOrEqual(V2.Dist(k.Pos, r.Courier.Pos), Balance.RockMinDist); }
            Steps(s, StepsFor(0.85f), V2.Zero);
            foreach (Rock k in r.Rocks) { Assert.IsTrue(k.Landed); Assert.IsTrue(Run.FreeNear(r.Room, k.Pos, 0.3f).X == k.Pos.X); }
            // ponto fraco: de frente meio ponto por tiro, pelas costas 2 pontos (vida em meios pontos)
            Sim s2 = R2At("B2"); Run r2 = s2.Run;
            Enemy g2 = r2.Enemies.Find(e => e.Type == EnemyType.StoneGolem);
            r2.Enemies.RemoveAll(e => e.Type == EnemyType.Minecart);
            r2.Courier.Pos = new V2(4.5f, 5.0f);
            Steps(s2, 1, Up);
            int front = 0, back = 0, nf = 0, nb = 0;
            for (int i = 0; i < StepsFor(6f); i++)
            {
                g2.Pos = new V2(4.5f, 9.5f); g2.Cd = 99f;
                r2.R2Of(g2).Dir = i < StepsFor(3f) ? new V2(0f, -1f) : new V2(0f, 1f);
                int h0 = g2.Hp;
                foreach (SimEvent e in Steps(s2, 1, V2.Zero)) if (e.Kind == Ev.EnemyHit && e.A == g2.Id) { if (i < StepsFor(3f)) { front += h0 - e.B; nf++; } else { back += h0 - e.B; nb++; } }
            }
            Assert.Greater(nf, 2); Assert.Greater(nb, 2);
            Assert.AreEqual(nf, front, "de frente: metade (1 meio ponto por tiro de 1)"); Assert.AreEqual(4 * nb, back, "pelas costas: x2 (4 meios pontos)");
            // fase 2: investida; bate na parede e fica atordoado 2 s (dano x2 de qualquer lado)
            Sim s3 = R2At("B2"); Run r3 = s3.Run;
            Enemy g3 = r3.Enemies.Find(e => e.Type == EnemyType.StoneGolem);
            r3.Enemies.RemoveAll(e => e.Type == EnemyType.Minecart);
            g3.Hp = g3.Hp / 2;
            r3.Courier.Pos = new V2(8.2f, 9.5f);
            Steps(s3, 1, V2.Zero);
            r3.R2Of(g3).Dir = new V2(1f, 0f);
            bool aim = false;
            for (int i = 0; i < StepsFor(1f) && !aim; i++) { Steps(s3, 1, V2.Zero); aim = r3.R2Of(g3).Act == R2Act.ChargeAim; }
            Assert.IsTrue(aim, "investida avisada");
            r3.Courier.Pos = new V2(2.0f, 2.5f);   // sai da faixa
            evs = Steps(s3, StepsFor(2f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Stun, g3.Id), "bateu na parede"); Assert.Greater(g3.Pos.X, 6.5f);
            Assert.Greater(g3.Stun, 0f);
            // o vagonete acerta o Golem: 6 cheios (12 meios pontos) e atordoa
            Sim s4 = R2At("B2"); Run r4 = s4.Run;
            Enemy g4 = r4.Enemies.Find(e => e.Type == EnemyType.StoneGolem);
            r4.Courier.Pos = new V2(1.5f, 2.0f);
            int hit = 0, stun = 0;
            for (int i = 0; i < StepsFor(4.5f); i++)
            {
                g4.Pos = new V2(4.5f, 4.6f); g4.Cd = 99f;
                int h0 = g4.Hp;
                foreach (SimEvent e in Steps(s4, 1, V2.Zero)) { if (e.Kind == Ev.EnemyHit && e.A == g4.Id) hit += h0 - e.B; if (e.Kind == Ev.Stun && e.A == g4.Id) stun++; }
            }
            Assert.AreEqual(2 * Balance.CartEnemyDamage, hit, "vagonete: 6 cheios"); Assert.AreEqual(1, stun);
        }

        // ------------------------------------------------------------------ cargas

        [Test]
        public void Coracao_PerseguidorVaiAteEle_MoedasNoAnelComTeto30_ReforcoMaior_AcordaQuemDorme()
        {
            // dormindo no anel de 4 m acorda (o courier a mais de 2 m)
            Sim s = R2At("R2-3", CargoType.ArcaneHeart); Run r = s.Run;
            Enemy z = r.Enemies.Find(e => e.Asleep);
            r.Courier.Pos = z.Pos + new V2(z.Pos.X < 4.5f ? 1.5f : -1.5f, -2.2f);
            Assert.Greater(V2.Dist(z.Pos, r.Courier.Pos), Balance.WakeRange);
            Steps(s, 1, V2.Zero);
            Assert.IsFalse(z.Asleep, "o coracao acorda quem dorme no anel");
            // morcego vai ate o coracao no chao
            Sim s2 = R2At("R2-3", CargoType.ArcaneHeart); Run r2 = s2.Run; r2.Enemies.Clear();
            Ground(r2, new V2(4.5f, 7.5f)); r2.Courier.Pos = new V2(8.0f, 2.0f);
            Enemy bat = r2.Spawn(EnemyType.Bat, new V2(4.5f, 11.0f));
            NoThief(s2, StepsFor(2.5f), V2.Zero);
            Assert.Less(V2.Dist(bat.Pos, r2.Main.Pos), 1.0f, "foi ao coracao, nao ao courier");
            Assert.AreEqual(Hint.HeartPull, r2.Hint);
            // +3 por inimigo derrotado no anel, teto de 30 na visita
            Sim s3 = R2At("R2-3", CargoType.ArcaneHeart); Run r3 = s3.Run; r3.Enemies.Clear();
            r3.Courier.Pos = new V2(4.5f, 3.0f);
            for (int k = 0; k < 12; k++) { Enemy t = r3.Spawn(EnemyType.Thief, new V2(2.5f + 0.5f * k, 5.0f)); t.Hp = 1; t.Stun = 99f; }
            Steps(s3, 1, Up);
            var evs = Steps(s3, StepsFor(9f), V2.Zero);
            int coins = 0, n = 0; foreach (SimEvent e in evs) if (e.Kind == Ev.CurrencySource && e.B == 10) { coins += e.A; n++; }
            Assert.AreEqual(12, CountEv(evs, Ev.EnemyDie)); Assert.AreEqual(10, n, "teto de 30 = 10 inimigos"); Assert.AreEqual(Balance.HeartCoinCap, coins);
            Assert.GreaterOrEqual(r3.HuntCoins, 30, "entra no Total");
            // reforco maior: +1 (repete o 1o do reforco)
            Sim s4 = R2At("R2-3", CargoType.ArcaneHeart); Run r4 = s4.Run; r4.Enemies.Clear();
            for (int i = 0; i < StepsFor(3f); i++) { r4.Enemies.RemoveAll(e => !e.Wave); Steps(s4, 1, Up); }
            Assert.AreEqual(r4.Room.Wave.Length + 1, r4.Enemies.FindAll(e => e.Wave).Count);
        }

        [Test]
        public void Gelo_DerreteNoTempo_AguaRecongela_GolpeTira10_FaixasDeIntegridade_ZeroDerrete()
        {
            Sim s = R2At("R2-1", CargoType.IceCrystal); Run r = s.Run; r.Enemies.Clear();
            Assert.AreEqual(115f, r.MeltTime, 1e-3f); Assert.AreEqual(100f, r.IceOf(r.Main));
            Steps(s, StepsFor(10f), V2.Zero);   // nas maos, fora d'agua
            Assert.AreEqual(100f - 10f * 100f / 115f, r.IceOf(r.Main), 0.2f, "derrete 100/MeltTime por s");
            float v0 = r.IceOf(r.Main);
            r.Courier.Pos = new V2(4.5f, 6.5f); Assert.IsTrue(r.InWater(r.Courier.Pos));
            Steps(s, StepsFor(0.5f), V2.Zero);
            Assert.AreEqual(v0 + 0.5f * Balance.IceWater, r.IceOf(r.Main), 0.2f, "nas maos na agua: +10/s");
            // golpe na carga no chao: -10 (alem do derreter)
            r.Courier.Pos = new V2(2.0f, 3.0f); Ground(r, new V2(1.2f, 3.0f));
            Enemy b = r.Spawn(EnemyType.Brute, new V2(1.2f, 4.2f)); b.Hp = 99;
            float before = r.IceOf(r.Main);
            bool smash = false;
            for (int i = 0; i < StepsFor(1.5f) && !smash; i++) smash = Has(NoThief(s, 1, V2.Zero), Ev.Strike);
            Assert.IsTrue(smash);
            r.Enemies.Clear();
            Assert.Less(r.IceOf(r.Main), before - Balance.IceHit + 0.01f, "golpe tira 10");
            Assert.AreEqual(0, r.Main.Cracks, "acima de 70 = intacta");
            // faixas: < 70 = 1 rachadura; < 40 = 2; 0 = derreteu (falha sem carga pendente)
            r.Courier.Pos = new V2(8f, 2f); Ground(r, new V2(2f, 2.5f));
            bool one = false, two = false;
            for (int i = 0; i < StepsFor(120f) && s.InContract; i++)
            {
                r.Enemies.Clear(); Steps(s, 1, V2.Zero);
                if (r.Main.State == CargoState.Gone) break;
                float v = r.IceOf(r.Main);
                Assert.AreEqual(v >= 70f ? 0 : v >= 40f ? 1 : 2, r.Main.Cracks, "faixa do gelo em " + v);
                if (r.Main.Cracks == 1) one = true; if (r.Main.Cracks == 2) two = true;
            }
            Assert.IsTrue(one && two); Assert.IsFalse(s.InContract);
            Assert.AreEqual(Outcome.Melted, s.LastResult.Outcome); Assert.IsNull(s.Pending);
        }

        [Test]
        public void Gaiola_GritaA6s_AcordaEAtrai_ODashComElaNasMaosAcalma_O2oGritoChamaOReforco()
        {
            Sim s = R2At("R2-3", CargoType.GriffinCage); Run r = s.Run;
            Enemy z = r.Enemies.Find(e => e.Asleep && e.Pos.X < 4.5f);
            r.Enemies.RemoveAll(e => e != z);
            r.Courier.Pos = new V2(2.5f, 6.4f);   // abaixo da metade (o reforco e de metade da sala)
            Assert.Greater(V2.Dist(z.Pos, r.Courier.Pos), Balance.WakeRange); Assert.LessOrEqual(V2.Dist(z.Pos, r.Courier.Pos), Balance.CageRadius);
            Assert.AreEqual(Balance.CageEvery, r.CageLeft, 0.01f);
            var evs = Steps(s, StepsFor(5.2f), V2.Zero);
            Assert.AreEqual(0, CountEv(evs, Ev.CageScream, r.Main.Id)); Assert.IsTrue(z.Asleep);
            evs = Steps(s, StepsFor(0.9f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.CageScream, r.Main.Id)); Assert.IsFalse(z.Asleep, "o grito acorda");
            Assert.IsFalse(Has(evs, Ev.RoomEvent));
            evs = Steps(s, StepsFor(6.1f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.CageScream, r.Main.Id)); Assert.IsTrue(Has(evs, Ev.RoomEvent), "o 2o grito chama o reforco");
            // atrai por 3 s: o morcego vai a gaiola no chao, nao ao courier
            Sim s2 = R2At("R2-3", CargoType.GriffinCage); Run r2 = s2.Run; r2.Enemies.Clear();
            Ground(r2, new V2(2.0f, 6.4f)); r2.Courier.Pos = new V2(7.5f, 2.0f);
            NoThief(s2, StepsFor(5.9f), V2.Zero);
            Assert.AreEqual(Hint.CageScream, r2.Hint, "aviso no ultimo 1 s");
            Enemy bat = r2.Spawn(EnemyType.Bat, new V2(4.5f, 8.5f));
            NoThief(s2, StepsFor(2.0f), V2.Zero);
            Assert.Less(V2.Dist(bat.Pos, r2.Main.Pos), V2.Dist(bat.Pos, r2.Courier.Pos), "atraido pela gaiola");
            // o dash com a gaiola nas maos zera o contador
            Sim s3 = R2At("R2-3", CargoType.GriffinCage); Run r3 = s3.Run; r3.Enemies.Clear();
            r3.Courier.Pos = new V2(4.5f, 2.0f);
            Steps(s3, StepsFor(5.5f), V2.Zero);
            evs = NoThief(s3, 1, Right, dashFirst: true);
            Assert.AreEqual(1, CountEv(evs, Ev.CageCalm, r3.Main.Id)); Assert.AreEqual(Balance.CageEvery, r3.CageLeft, 0.1f);
            evs = NoThief(s3, StepsFor(3f), V2.Zero);
            Assert.AreEqual(0, CountEv(evs, Ev.CageScream, r3.Main.Id), "acalmado: sem grito aos 6 s");
        }
    }

    /// <summary>Medicao do bot na regiao 2 (docs/BALANCE_REGIAO2.md). A campanha da regiao 2 parte do estado do fim da regiao 1 medido pelo
    /// bot (BALANCE.md: 62,7 estrelas, guilda 7/7/7/6,8, armas 2,4/2,1/2,0, sobra de 1.089 moedas) e compra como o Bot.PlayCampaign; cada
    /// contrato e medido a partir do save que a campanha tinha antes da 1a tentativa dele. Asserts de faixa larga; o alvo fino fica no doc.</summary>
    public class BalanceTestsRegiao2
    {
        const int Camps = 6, Seeds = 10;
        static Bot.Campaign[] camps; static Bot.Stats[] table, lo, hi;

        /// <summary>Save do fim da regiao 1 (estrelas pelo DevStars, guilda e armas pela media do bot da regiao 1).</summary>
        public static string EndOfRegion1(int seed)
        {
            Sim s = Sim.Load("v=3\nseed=" + seed + "\nrep=" + RegionDef.Of(2).First + "\ncoins=1089\nlv=7,7,7,7\nwlv=2,2,2\n");
            s.DevStars(63);
            return s.Save();
        }

        static void Measure()
        {
            if (table != null) return;
            RegionDef g = RegionDef.Of(2); int n = g.Contracts.Length;
            camps = new Bot.Campaign[Camps]; table = new Bot.Stats[n]; lo = new Bot.Stats[n]; hi = new Bot.Stats[n];
            for (int c = 0; c < n; c++) { table[c] = new Bot.Stats { Contract = g.First + c }; lo[c] = new Bot.Stats { Contract = g.First + c }; hi[c] = new Bot.Stats { Contract = g.First + c }; }
            for (int k = 0; k < Camps; k++)
            {
                camps[k] = Bot.PlayCampaign(6000 + k, region: 2, from: EndOfRegion1(6000 + k));
                for (int c = 0; c < n; c++)
                {
                    string before = camps[k].Before[g.First + c];
                    if (before == null) continue;
                    Bot.MeasureFrom(before, g.First + c, Seeds, 1000 + 100 * k, 0.5f, table[c]);
                    if (c < 3) continue;   // H1 do 4o em diante (os tutoriais nao tem reforco)
                    Bot.MeasureFrom(before, g.First + c, Seeds / 2, 2000 + 100 * k, 0.1f, lo[c]);
                    Bot.MeasureFrom(before, g.First + c, Seeds / 2, 3000 + 100 * k, 0.9f, hi[c]);
                }
            }
        }

        static double FightPct(Bot.Stats[] t) { int f = 0, r = 0; for (int c = 3; c < t.Length; c++) { f += t[c].RoomsFought; r += t[c].RoomsNormal; } return r == 0 ? 0 : 100.0 * f / r; }

        [Test]
        public void Campanha_Regiao2_PeloBot_FechaOs16_DuracaoH1EMoedas()
        {
            Measure();
            RegionDef g = RegionDef.Of(2);
            foreach (Bot.Campaign cp in camps) { Assert.IsTrue(cp.Finished, "o bot fecha a regiao 2"); Assert.GreaterOrEqual(cp.Coins, 0); }
            double sum = 0; int nn = 0;
            for (int c = 0; c < table.Length; c++)
            {
                Bot.Stats s = table[c]; ContractDef d = g.Contracts[c]; string k = d.Code;
                Assert.AreEqual(Camps * Seeds, s.Runs, k);
                Assert.AreEqual(0, s.Timeout, k + ": bot preso (desistiu por tempo)");
                Assert.That(s.AvgTime, Is.InRange(5.0, 360.0), k);
                Assert.GreaterOrEqual(s.Pct(s.Delivered), 40.0, k + ": quase ninguem entrega com a guilda da campanha");
                Assert.Less(s.Pct(s.Died), 40.0, k + ": morte demais");
                if (c < 3) { Assert.Less(s.AvgTime, 60.0, k + ": tutorial longo"); Assert.GreaterOrEqual(s.Pct(s.Delivered), 85.0, k + ": tutorial"); }
                else if (d.Cargo != CargoType.Message) { sum += s.AvgTime; nn++; }
            }
            Assert.GreaterOrEqual(sum / nn, 60.0, "do 4o em diante, sem a mensagem (alvo do bot 90-180 s: BALANCE_REGIAO2.md)");
            Assert.GreaterOrEqual(table[table.Length - 1].AvgTime, 90.0, "M2-16 (chefe)");
            double low = FightPct(lo), mid = FightPct(table), high = FightPct(hi);
            Assert.Greater(high - low, 15.0, "H1: a agressividade muda a luta");
            Assert.That(mid, Is.InRange(30.0, 80.0), "H1 com agressividade 0,5");

            var ci = CultureInfo.InvariantCulture; var sb = new StringBuilder();
            sb.AppendLine("| Contrato | Carga | Salas | % concluído | % morte | % falha de carga | duração média (s) | mediana (s) | % salas com luta | moedas médias | estrelas | 3 estrelas % | rachaduras |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            for (int c = 0; c < table.Length; c++)
            {
                Bot.Stats s = table[c]; ContractDef d = g.Contracts[c];
                sb.AppendLine(string.Format(ci, "| {0} | {1} | {2} | {3:0} | {4:0} | {5:0} | {6:0} | {7:0} | {8:0} | {9:0} | {10:0.00} | {11:0} | {12:0.00} |", d.Code, CargoDef.All[(int)d.Cargo].Name,
                    d.Rooms, s.Pct(s.Delivered), s.Pct(s.Died), s.Pct(s.CargoFail), s.AvgTime, s.MedianTime, s.FightPct, s.AvgCoins, s.AvgStars, s.Pct(s.ThreeStars), s.AvgCracks));
            }
            int att = 0, deaths = 0, coins = 0, stars = 0, spent = 0, earned = 0; float time = 0f; var per = new StringBuilder();
            foreach (Bot.Campaign cp in camps) { att += cp.Attempts; deaths += cp.Deaths; coins += cp.Coins; stars += cp.Stars; spent += cp.Spent; earned += cp.Earned; time += cp.Time; }
            for (int c = 0; c < table.Length; c++) { double a = 0, t = 0; foreach (Bot.Campaign cp in camps) { a += cp.AttemptsPer[g.First + c]; t += cp.TimePer[g.First + c]; } per.Append(string.Format(ci, " {0}={1:0.00}·{2:0}", g.Contracts[c].Code, a / Camps, t / Camps)); }
            sb.AppendLine(string.Format(ci, "Campanha da regiao 2 ({0} sementes, do fim da regiao 1): fecha 100% · tentativas {1:0.0} · mortes {2:0.0} · tempo do bot {3:0.0} min · moedas ganhas {4:0} · gastas {5:0} · paradas no fim {6:0} · estrelas no fim {7:0.0}",
                Camps, att / (double)Camps, deaths / (double)Camps, time / Camps / 60f, earned / (double)Camps, spent / (double)Camps, coins / (double)Camps, stars / (double)Camps));
            sb.AppendLine("Por contrato (tentativas · tempo da entrega s):" + per);
            sb.AppendLine(string.Format(ci, "Duracao M2-4..M2-16 sem a mensagem: media {0:0} s · H1 (% de salas com luta) agressividade 0,1 / 0,5 / 0,9 = {1:0}% / {2:0}% / {3:0}%", sum / nn, low, mid, high));
            TestContext.WriteLine(sb.ToString());
        }

        /// <summary>Contratos gerados da regiao 2 (codigo com a regiao nos bits altos): 4-6 camadas das salas normais da regiao, entrega D2,
        /// cargas ate a gaiola; o bot (guilda 6, armas 2) joga sem travar.</summary>
        [Test]
        public void ContratosGerados_Regiao2_SoComSalasDaRegiao_RodamPeloBot()
        {
            RegionDef g = RegionDef.Of(2);
            var st = new Bot.Stats { Contract = -1 };
            for (int p = 0; p < 30; p++)
            {
                int payload = Content.WithRegion(4000 + 37 * p, 2);
                ContractDef d = Sim.CodeContract(ContractCode.Encode(payload));
                Assert.AreEqual(2, d.Region); Assert.Less((int)d.Cargo, g.CargoCount); Assert.AreEqual(g.Delivery, d.Layers[d.Layers.Length - 1][0]);
                Assert.AreEqual(d.Cargo == CargoType.IceCrystal ? Regiao2.GenMelt * d.Rooms : 0f, d.MeltTime, 1e-3f);
                for (int l = 0; l < d.Layers.Length - 1; l++) foreach (int id in d.Layers[l]) Assert.IsTrue(Content.Rooms[id].Normal && Content.Rooms[id].Region == 2, d.ShareCode + ": sala " + Content.Rooms[id].Code);
                Sim sim = Bot.MeasureSim(4000 + p, Weapon.Boomerang, 2, 6);
                Assert.IsNotNull(sim.StartCode(ContractCode.Encode(payload), false), "codigo da regiao 2 aberto");
                ContractResult res = Bot.Finish(sim, new Bot(4000 + p));
                Assert.AreNotEqual(Outcome.Abandoned, res.Outcome, d.ShareCode + ": bot preso");
                if (res.Delivered) st.Delivered++;
                st.Runs++;
            }
            Assert.Greater(st.Pct(st.Delivered), 50.0, "gerados da regiao 2 com guilda 6");
        }

        /// <summary>As caveiras da porta contam a verdade na regiao 2, com a regra da regiao 1 (folga de 0,3 entre as medias; nenhuma sala passa
        /// da media do icone vizinho).</summary>
        [Test]
        public void IconeDeAmeaca_Regiao2_AcompanhaODanoMedido()
        {
            Measure();
            var dmg = new double[Content.Rooms.Length]; var visits = new int[Content.Rooms.Length];
            foreach (Bot.Stats s in table) for (int r = 0; r < dmg.Length; r++) { dmg[r] += s.RoomDamage[r]; visits[r] += s.RoomVisits[r]; }
            var mean = new double[4]; var count = new int[4];
            foreach (int id in RegionDef.Of(2).Normal)
            {
                RoomDef a = Content.Rooms[id];
                Assert.GreaterOrEqual(visits[id], 30, a.Code + ": poucas visitas para medir");
                mean[a.Threat] += dmg[id] / visits[id]; count[a.Threat]++;
            }
            for (int t = 1; t <= 3; t++) { Assert.Greater(count[t], 0, "icone " + t + " sem sala"); mean[t] /= count[t]; }
            for (int t = 1; t < 3; t++) Assert.Greater(mean[t + 1] - mean[t], 0.3, "icones " + t + " e " + (t + 1) + " sem folga de dano");
            var sb = new StringBuilder("| Sala | Ícone | Risco da carga | Visitas | Dano por visita |\n|---|---|---|---|---|\n");
            foreach (int id in RegionDef.Of(2).Normal)
            {
                RoomDef a = Content.Rooms[id]; double d = dmg[id] / visits[id];
                if (a.Threat < 3) Assert.Less(d, mean[a.Threat + 1], a.Code + " (icone " + a.Threat + ") tira mais vida que a media do icone " + (a.Threat + 1));
                if (a.Threat > 1) Assert.Greater(d, mean[a.Threat - 1], a.Code + " (icone " + a.Threat + ") tira menos vida que a media do icone " + (a.Threat - 1));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} {1} | {2} | {3} | {4} | {5:0.00} |", a.Code, a.Name, a.Threat, a.Risk, visits[id], d));
            }
            TestContext.WriteLine(sb.ToString());
        }
    }
}
