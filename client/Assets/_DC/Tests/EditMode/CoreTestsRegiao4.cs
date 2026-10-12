using System.Collections.Generic;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Regiao 4 "Forja do Dragao" (CONTEUDO_REGIOES §4-§6): level design das salas, terreno (lava, respiro, fogo no chao), inimigos,
    /// Capataz, Dragao, planta, mapa, a campanha pelo bot e o sem fim com o conteudo da regiao. So API publica.</summary>
    public partial class CoreTests
    {
        // ------------------------------------------------------------------ helpers da regiao 4

        /// <summary>Sim com a campanha toda aberta (estrelas para a regiao 4).</summary>
        static Sim Sim4(int seed = 7) { var s = new Sim(seed) { Reputation = Sim.Contracts.Length }; s.DevStars(999); return s; }
        static int C4(int k) => RegionDef.Of(4).First + k;
        static int Room4(int local) => RegionDef.Of(4).Rooms[local];
        static Enemy Only4(Run r, EnemyType t, V2 p) { r.Enemies.Clear(); return r.Spawn(t, p); }
        static int CountType4(Run r, EnemyType t) { int n = 0; foreach (Enemy e in r.Enemies) if (e.Alive && e.Type == t) n++; return n; }
        static int CountKind4(List<SimEvent> evs, Ev k, int b) { int n = 0; foreach (SimEvent e in evs) if (e.Kind == k && e.B == b) n++; return n; }

        /// <summary>Courier parado (pousa a carga) e sem atirar por `s` segundos.</summary>
        static List<SimEvent> Hold4(Sim sim, float s, bool shoot = false)
        {
            var evs = new List<SimEvent>();
            for (int i = 0; i < StepsFor(s) && sim.InContract; i++) { if (!shoot) sim.Run.Courier.ShotCd = 1f; evs.AddRange(Steps(sim, 1, V2.Zero)); }
            return evs;
        }

        // ------------------------------------------------------------------ level design

        [Test]
        public void LevelDesign_Regiao4_SalasContratosLavaRespiroTrilho()
        {
            RegionDef reg = RegionDef.Of(4);
            Assert.IsTrue(reg.HasContent); Assert.AreEqual("forja", reg.Theme); Assert.AreEqual(Regiao4.Name, reg.Name); Assert.AreEqual(95, reg.Stars);
            Assert.AreEqual(14, reg.Rooms.Length); Assert.AreEqual(16, reg.Contracts.Length); Assert.AreEqual(10, reg.Normal.Length);
            string[] codes = { "R4-1", "R4-2", "R4-3", "R4-4", "R4-5", "R4-6", "R4-7", "R4-8", "R4-9", "R4-10", "S4-1", "M4", "B4", "D4" };
            for (int i = 0; i < codes.Length; i++) { Assert.AreEqual(codes[i], Content.Rooms[reg.Rooms[i]].Code); Assert.AreEqual(4, Content.Rooms[reg.Rooms[i]].Region); }
            Assert.AreEqual(Room4(Regiao4.M), reg.Mini); Assert.AreEqual(Room4(Regiao4.B), reg.Boss); Assert.AreEqual(Room4(Regiao4.D), reg.Delivery);
            CollectionAssert.AreEqual(new[] { Room4(Regiao4.S1) }, reg.Secrets);
            Assert.AreEqual(C4(7), reg.MiniAt, "Capataz no M4-8"); Assert.AreEqual(C4(15), reg.BossAt, "Dragao no M4-16");
            Assert.AreEqual(Room4(Regiao4.S1), Content.Rooms[Room4(Regiao4.R3)].Secret); Assert.AreEqual(Room4(Regiao4.S1), Content.Rooms[Room4(Regiao4.R9)].Secret);
            Assert.IsTrue(Content.Rooms[Room4(Regiao4.R9)].SecretHidden, "R4-9: parede disfarcada");
            var bp = new Dictionary<Gear, int>();
            foreach (int id in reg.Rooms) { Gear g = Content.Rooms[id].Blueprint; if (g != Gear.None) bp[g] = (bp.TryGetValue(g, out int n) ? n : 0) + 1; }
            Assert.AreEqual(4, bp[Gear.Trap], "armadilha: R4-3, R4-4, R4-6, S4-1"); Assert.AreEqual(1, bp[Gear.Hook], "gancho de reserva na R4-1 (raia E)"); Assert.AreEqual(2, bp.Count);

            foreach (int id in reg.Rooms)
            {
                RoomDef room = Content.Rooms[id]; string k = room.Code;
                Assert.AreEqual(0, room.Rails.Length, k + ": trilho e da regiao 2");
                var points = new List<Spawn>(room.Spawns); points.AddRange(room.Wave);
                foreach (Spawn s in points)
                {
                    Assert.GreaterOrEqual(V2.Dist(s.Pos, Balance.Entrance), 4f, k + ": spawn/reforco em cima da entrada");
                    Assert.IsFalse(Run.Blocked(room, s.Pos, EnemyDef.All[(int)s.Type].Radius), k + ": spawn/reforco dentro de obstaculo ou lava");
                    foreach (Vent v in room.Vents) Assert.Greater(V2.Dist(s.Pos, v.Pos), v.Radius, k + ": spawn em cima de respiro");
                }
                foreach (Rect o in room.Obstacles) Assert.LessOrEqual(o.C.Y + o.H.Y, 12.2f, k + ": obstaculo na faixa das portas");
                foreach (Rect o in room.Lava) Assert.LessOrEqual(o.C.Y + o.H.Y, 12.2f, k + ": lava na faixa das portas");
                Assert.IsFalse(Run.Blocked(room, Balance.Entrance, 1.2f), k + ": obstaculo ou lava a 1,2 m da entrada");
                foreach (Vent v in room.Vents)
                {
                    Assert.Greater(V2.Dist(v.Pos, Balance.Entrance) - v.Radius, 1.2f, k + ": respiro a menos de 1,2 m da entrada");
                    Assert.LessOrEqual(v.Pos.Y + v.Radius, 12.2f, k + ": respiro na faixa das portas");
                }
                var nav = new Nav(room);
                var spots = new List<V2> { room.RescueSpot };
                if (room.ChestCoins > 0) spots.Add(room.ChestPos);
                foreach (BuriedChest b in room.Buried) { spots.Add(b.Pos); Assert.Greater(b.Coins, 0, k); }
                foreach (V2 p in spots)
                {
                    Assert.IsFalse(Run.Blocked(room, p, Balance.CargoR), k + ": bau/resgate dentro de obstaculo ou lava");
                    foreach (Vent v in room.Vents) Assert.Greater(V2.Dist(p, v.Pos), v.Radius, k + ": bau/resgate em cima de respiro");
                    Assert.Less(nav.Steps(Balance.Entrance, p), int.MaxValue, k + ": bau/resgate inalcancavel");
                }
                for (int n = 1; n <= 3; n++) for (int i = 0; i < n; i++) Assert.Less(nav.Steps(Balance.Entrance, new V2(Balance.DoorX(n, i), 13.5f)), int.MaxValue, k + ": porta inalcancavel");
                if (room.Secret >= 0)
                {
                    V2 inside = new V2(room.SecretPos.X == 0f ? 0.5f : Balance.RoomW - 0.5f, room.SecretPos.Y);
                    Assert.IsFalse(Run.Blocked(room, inside, Balance.CourierR), k + ": parede rachada atras de obstaculo");
                    Assert.Less(nav.Steps(Balance.Entrance, inside), int.MaxValue, k + ": parede rachada inalcancavel");
                }
                if (room.Lava.Length > 0 || room.Vents.Length > 0) Assert.IsTrue((room.Risk & CargoRisk.Smash) != 0, k + ": lava/respiro = Pancada");
            }
            Assert.AreEqual(1, Content.Rooms[Room4(Regiao4.R2)].Buried.Length);

            var debut = new Dictionary<CargoType, int>(); var used = new HashSet<int>();
            for (int i = 0; i < 16; i++)
            {
                ContractDef d = reg.Contracts[i]; string k = d.Code;
                Assert.AreEqual("M4-" + (i + 1), k); Assert.AreEqual(C4(i), d.Index); Assert.AreEqual(4, d.Region); Assert.AreEqual(16 + i, d.Tier, k);
                Assert.AreEqual(i >= 3, d.Waves, k + ": os 3 que ensinam sem reforco");
                Assert.AreEqual(Room4(Regiao4.D), d.Layers[d.Layers.Length - 1][0], k);
                int layers = d.Layers.Length - 1;
                bool boss = System.Array.IndexOf(d.Layers[layers - 1], reg.Mini) >= 0 || System.Array.IndexOf(d.Layers[layers - 1], reg.Boss) >= 0;
                Assert.That(layers - (boss ? 1 : 0), i < 3 ? Is.InRange(2, 3) : Is.InRange(4, 6), k);
                Assert.AreEqual(i == 7, System.Array.IndexOf(d.Layers[layers - 1], reg.Mini) >= 0, k + ": Capataz so no M4-8");
                Assert.AreEqual(i == 15, System.Array.IndexOf(d.Layers[layers - 1], reg.Boss) >= 0, k + ": Dragao so no M4-16");
                Assert.Greater(d.ParTime, 0f, k); Assert.Greater(d.Pay, 300, k);
                Assert.AreEqual(d.Cargo == CargoType.Message, d.HasTimer, k);
                Assert.AreEqual(d.Cargo == CargoType.Egg || d.Cargo == CargoType.IceCrystal, d.EggTarget > 0f, k);
                if (!debut.ContainsKey(d.Cargo)) debut[d.Cargo] = i;
                foreach (int[] l in d.Layers) foreach (int id in l) used.Add(id);
            }
            Assert.AreEqual(1, debut[CargoType.Flytrap], "planta estreia no M4-2"); Assert.AreEqual(2, debut[CargoType.TreasureMap], "mapa estreia no M4-3");
            foreach (int id in reg.Normal) Assert.IsTrue(used.Contains(id), Content.Rooms[id].Code + ": sala normal fora da campanha");
        }

        // ------------------------------------------------------------------ terreno

        [Test]
        public void Lava_DashEmpurraContraALava_3DeDano1xPorEmpurrao_BrasaMorre_SalamandraImune()
        {
            foreach (EnemyType t in new[] { EnemyType.Brute, EnemyType.MagmaShard, EnemyType.Salamander, EnemyType.Bat })
            {
                var sim = Sim4(); Run r = Start(sim, C4(0)); r.Courier.Hp = 99;   // R4-1: lava em (4,5; 7), 3 x 1,4 m
                Assert.AreEqual("R4-1", r.Room.Code);
                Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 4.6f); Hold4(sim, 0.3f);   // pousou: o dash nao larga nada
                Enemy e = Only4(r, t, new V2(4.5f, 5.6f)); e.Stun = 3f;
                int hp0 = e.Hp;
                Hold4(sim, Dt);
                if (t == EnemyType.Brute) Assert.AreEqual(Hint.Lava, r.Hint, "dica: inimigo perto da lava e dash pronto");
                List<SimEvent> evs = Steps(sim, 1, Up, true);
                evs.AddRange(Hold4(sim, 1f));
                int burns = CountKind4(evs, Ev.LavaBurn, e.Id);
                switch (t)
                {
                    case EnemyType.Brute: Assert.AreEqual(1, burns, "1x por empurrao"); Assert.AreEqual(hp0 - Balance.LavaDamage, e.Hp, "3 de dano"); break;
                    case EnemyType.MagmaShard: Assert.IsFalse(e.Alive, "a brasa morre na lava"); break;
                    default: Assert.AreEqual(0, burns, t + " imune"); Assert.AreEqual(hp0, e.Hp, t + " imune"); break;
                }
            }
            // carga empurrada (pouso do Dragao) para na borda da lava com 1 rachadura: coberto em Dragao_*; aqui so a solidez
            Assert.IsTrue(Run.Blocked(Content.Rooms[Room4(Regiao4.R1)], new V2(4.5f, 7f), 0.1f));
        }

        [Test]
        public void Respiro_Ciclo4s_Aviso08_Fogo1s_CourierCargaEInimigo1xPorErupcao_ForjaImune_B4SoNaFase2()
        {
            var sim = Sim4(); Run r = Start(sim, C4(3));   // M4-4 comeca na R4-2 (6 respiros)
            Assert.AreEqual("R4-2", r.Room.Code);
            r.Enemies.Clear(); r.Courier.Hp = 99;
            Assert.AreEqual(0, r.VentState(0));
            float tGlow = -1f, tFire = -1f, tOff = -1f, tGlow1 = -1f;
            for (int i = 0; i < StepsFor(4.2f); i++)
            {
                Steps(sim, 1, V2.Zero); r.Enemies.Clear(); r.Courier.Pos = Balance.Entrance;
                int s = r.VentState(0);
                if (s == 1 && tGlow < 0f) tGlow = r.Time;
                if (s == 2 && tFire < 0f) tFire = r.Time;
                if (s == 0 && tFire > 0f && tOff < 0f) tOff = r.Time;
                if (r.VentState(1) == 1 && tGlow1 < 0f) tGlow1 = r.Time;
            }
            Assert.AreEqual(2.2f, tGlow, 0.05f, "apagado 2,2 s"); Assert.AreEqual(3.0f, tFire, 0.05f, "brilha 0,8 s"); Assert.AreEqual(4.0f, tOff, 0.05f, "fogo 1,0 s");
            Assert.AreEqual(0.2f, tGlow1, 0.05f, "fase de 2 s adianta o respiro 1");

            // courier no respiro 0, Bruto (atordoado) no 2 e elemental (atordoado) no 1; carga pousada dentro do 0
            var s2 = Sim4(); Run r2 = Start(s2, C4(3)); r2.Courier.Hp = 99;
            Steps(s2, 1, Up); r2.Courier.Pos = r2.Room.Vents[0].Pos;
            Enemy brute = Only4(r2, EnemyType.Brute, r2.Room.Vents[2].Pos); brute.Stun = 99f; brute.Hp = 50;
            Enemy mag = r2.Spawn(EnemyType.MagmaElemental, r2.Room.Vents[1].Pos); mag.Stun = 99f; mag.Hp = 50;
            List<SimEvent> evs = Hold4(s2, 2.1f);
            Assert.AreEqual(CargoState.Ground, r2.Main.State);
            Assert.Less(V2.Dist(r2.Main.Pos, r2.Room.Vents[0].Pos), r2.Room.Vents[0].Radius, "carga pousada dentro do respiro");
            evs = Hold4(s2, 0.5f);
            Assert.AreEqual(Hint.Vent, r2.Hint, "respiro brilhando perto");
            int hp = r2.Courier.Hp;
            evs = Hold4(s2, 1.5f);
            Assert.AreEqual(hp - Balance.VentCourierDamage, r2.Courier.Hp, "2 de dano, 1x por erupcao");
            Assert.AreEqual(1, r2.Main.Cracks, "carga no chao dentro racha 1x");
            Assert.AreEqual(50 - Balance.VentEnemyDamage, brute.Hp, "3 de dano no inimigo");
            Assert.AreEqual(50, mag.Hp, "elemental (forja) imune");
            Assert.IsTrue(Has(evs, Ev.VentFire));
            // barril pousado no respiro: o fogo direto detona na hora (M4-11 passa pela R4-2 na camada 3)
            Sim s6 = InRoomWith(C4(10), 3, Room4(Regiao4.R2), CargoType.Barrel); Run r6 = s6.Run; r6.Enemies.Clear(); r6.Courier.Hp = 99;
            Steps(s6, 1, Up); Hold4(s6, 0.25f);
            r6.Main.Pos = r6.Room.Vents[1].Pos; r6.Main.Fuse = 0f;   // respiro 1 (fase 2 s): fogo entre 1 e 2 s
            for (int i = 0; i < StepsFor(2f) && s6.InContract; i++) { r6.Main.Fuse = 0f; r6.Enemies.Clear(); Hold4(s6, Dt); }
            Assert.AreEqual(Outcome.Exploded, s6.LastResult.Outcome, "respiro detona o barril");
            // cristal de gelo no respiro: -25 de gelo (o M4-5 passa pela R4-2 na camada 3; o gelo e da raia R2: IceOf/MeltIce)
            Sim s7 = InRoomWith(C4(4), 3, Room4(Regiao4.R2), CargoType.IceCrystal); Run r7 = s7.Run; r7.Enemies.Clear(); r7.Courier.Hp = 99;
            Steps(s7, 1, Up); Hold4(s7, 0.25f);
            r7.Main.Pos = r7.Room.Vents[1].Pos;
            float ice0 = r7.IceOf(r7.Main), melt = 100f / r7.MeltTime * 2.2f;   // derrete sozinho ~2 s (x3 so perto de lava)
            for (int i = 0; i < StepsFor(2f) && s7.InContract; i++) { r7.Enemies.Clear(); Hold4(s7, Dt); }
            Assert.Less(r7.IceOf(r7.Main), ice0 - Balance.VentIce + 1f, "o fogo do respiro tira 25 de gelo");
            Assert.Greater(r7.IceOf(r7.Main), ice0 - Balance.VentIce - melt * 3f - 1f);

            // B4: as fissuras so ligam na fase 2 do Dragao
            Sim s3 = InRoomWith(C4(15), 4, Room4(Regiao4.B), CargoType.Box);
            Run r3 = s3.Run; r3.Courier.Hp = 99;
            Enemy drag = null; foreach (Enemy e in r3.Enemies) if (e.Type == EnemyType.ElderDragon) drag = e;
            Assert.IsNotNull(drag); Assert.AreEqual(1, r3.Forge.Phase);
            for (int i = 0; i < StepsFor(5f); i++) { Steps(s3, 1, V2.Zero); r3.Courier.Pos = Balance.Entrance; for (int v = 0; v < 3; v++) Assert.AreEqual(0, r3.VentState(v), "fase 1: fissuras apagadas"); }
            drag.Hp = drag.Hp / 2;
            bool on = false;
            for (int i = 0; i < StepsFor(4.5f); i++) { Steps(s3, 1, V2.Zero); r3.Courier.Pos = Balance.Entrance; for (int v = 0; v < 3; v++) if (r3.VentState(v) != 0) on = true; }
            Assert.AreEqual(2, r3.Forge.Phase); Assert.IsTrue(on, "fase 2: fissuras ligam");
        }

        [Test]
        public void FogoNoChao_FrascoDoKobold_1PorSegundo_BarrilDetona_ForjaImune()
        {
            // kobold: aviso de 0,9 s onde o courier estava; o frasco tira 1 e vira poca de fogo de 1 m por 4 s; a carga embaixo racha
            var sim = Sim4(); Run r = Start(sim, C4(0)); r.Courier.Hp = 99;
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 3f);
            Enemy k = Only4(r, EnemyType.KoboldBomber, new V2(8.2f, 4f)); k.Hp = 99;
            List<SimEvent> evs = Hold4(sim, 0.2f);
            ForgeMark m = null;
            for (int i = 0; i < StepsFor(3f) && m == null; i++) { evs.AddRange(Hold4(sim, Dt)); foreach (ForgeMark x in r.Forge.Marks) if (x.Kind == ForgeMarkKind.Flask) m = x; }
            Assert.IsNotNull(m, "kobold nao jogou o frasco");
            Assert.AreEqual(Balance.FlaskR, m.Radius); Assert.Less(V2.Dist(m.Pos, r.Courier.Pos), 0.3f, "circulo onde o courier estava");
            Assert.AreEqual(1, CountKind4(evs, Ev.Telegraph, (int)EnemyType.KoboldBomber));
            int hp = r.Courier.Hp;
            evs = Hold4(sim, 1.0f);
            Assert.AreEqual(1, CountKind4(evs, Ev.Strike, (int)EnemyType.KoboldBomber), "o frasco caiu");
            Assert.AreEqual(hp - 1, r.Courier.Hp, "impacto: 1 de dano (o fogo so conta depois de 1 s)");
            Assert.AreEqual(1, r.Main.Cracks, "carga embaixo racha");
            Assert.AreEqual(1, r.Forge.Fires.Count); Assert.AreEqual(1, r.Forge.Fires[0].Source);
            r.Enemies.Clear(); hp = r.Courier.Hp;
            evs = Hold4(sim, 2.5f);
            Assert.AreEqual(hp - 2, r.Courier.Hp, "fogo no chao: 1 de dano por segundo");
            Assert.AreEqual(1, r.Forge.Fires.Count); Hold4(sim, 1.3f);
            Assert.AreEqual(0, r.Forge.Fires.Count, "poca de 4 s");

            // inimigo comum queima, salamandra (forja) nao; barril no fogo detona na hora
            var s2 = Sim4(); Run r2 = Start(s2, C4(0)); r2.Courier.Hp = 99;
            Enemy th = Only4(r2, EnemyType.Brute, new V2(2f, 10f)); th.Stun = 99f; int th0 = th.Hp;
            Enemy sal = r2.Spawn(EnemyType.Salamander, new V2(7f, 10f)); sal.Stun = 99f;
            r2.Forge.Fires.Add(new FirePatch { Pos = th.Pos, Radius = 1f, Left = 9f }); r2.Forge.Fires.Add(new FirePatch { Pos = sal.Pos, Radius = 1f, Left = 9f });
            Steps(s2, StepsFor(2.05f), V2.Zero);
            Assert.AreEqual(th0 - 3, th.Hp, "1 por segundo (na hora, 1 s e 2 s)"); Assert.AreEqual(EnemyDef.All[(int)EnemyType.Salamander].Hp, sal.Hp, "salamandra imune");
            var s3 = Sim4(); Run r3 = Start(s3, C4(10));   // M4-11: barril
            Assert.AreEqual(CargoType.Barrel, r3.Main.Type); r3.Enemies.Clear(); r3.Courier.Hp = 99;
            Steps(s3, 1, Up); Steps(s3, StepsFor(0.3f), V2.Zero);
            Assert.AreEqual(CargoState.Ground, r3.Main.State);
            r3.Forge.Fires.Add(new FirePatch { Pos = r3.Main.Pos, Radius = 0.5f, Left = 3f });
            Steps(s3, 2, V2.Zero);
            Assert.IsTrue(s3.InContract, "fogo no chao acende o pavio (nao detona na hora)");
            Assert.GreaterOrEqual(r3.Main.Fuse, Balance.BarrelFuse - Balance.FireFuse - 0.01f, "o pavio fica com 1 s");
            Steps(s3, StepsFor(Balance.FireFuse), V2.Zero);
            Assert.AreEqual(Outcome.Exploded, s3.LastResult.Outcome, "1 s depois explode");
            // courier: atravessar rapido nao queima (0,5 s de folga), ficar queima
            var s5 = Sim4(); Run r5 = Start(s5, C4(0)); r5.Enemies.Clear(); r5.Courier.Hp = 99;
            r5.Forge.Fires.Add(new FirePatch { Pos = new V2(4.5f, 2.2f), Radius = 0.45f, Left = 9f });
            Steps(s5, StepsFor(0.6f), Up);
            Assert.Greater(r5.Courier.Pos.Y, 3f); Assert.AreEqual(99, r5.Courier.Hp, "passou pelo fogo sem parar");
        }

        // ------------------------------------------------------------------ inimigos

        [Test]
        public void Salamandra_CirculaA25m_DeixaRastroDeFogo_CortaETocaOCourier()
        {
            var sim = Sim4(); Run r = Start(sim, C4(0)); r.Courier.Hp = 99;
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 3f);
            Enemy s = Only4(r, EnemyType.Salamander, new V2(1.2f, 3.5f));
            Assert.AreEqual(4, s.Hp, "vida 4 sem escala por nivel");
            List<SimEvent> evs = Hold4(sim, 2.4f);
            Assert.That(V2.Dist(s.Pos, r.Courier.Pos), Is.InRange(1.8f, 3.2f), "circula a ~2,5 m");
            Assert.Greater(r.Forge.Fires.Count, 2, "rastro de fogo a cada 0,5 s");
            foreach (FirePatch f in r.Forge.Fires) { Assert.AreEqual(0, f.Source); Assert.AreEqual(Balance.TrailR, f.Radius); }
            int hp = r.Courier.Hp;
            evs.AddRange(Hold4(sim, 3f));
            Assert.GreaterOrEqual(CountKind4(evs, Ev.Strike, (int)EnemyType.Salamander), 1, "corta na direcao do courier");
            Assert.Less(r.Courier.Hp, hp, "toque da salamandra");
        }

        [Test]
        public void Diabrete_TrocaDeLugarComACargaNoChao_GolpeNoRedemoinhoCancela_SemCargaSurgeAtras()
        {
            // troca: carga no chao, diabrete a ~5,6 m (fora do tiro), linha livre
            Sim sim = InRoomWith(C4(7), 3, Room4(Regiao4.R4), CargoType.Box); Run r = sim.Run; r.Courier.Hp = 99;   // R4-4 pelo M4-8 (caixa)
            Assert.AreEqual("R4-4", r.Room.Code);
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 3f);
            Hold4(sim, 0.3f);
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            Enemy imp = Only4(r, EnemyType.Imp, new V2(4.5f, 8.6f)); imp.Cd = 0f;
            V2 cargo0 = r.Main.Pos, imp0 = imp.Pos;
            List<SimEvent> evs = Hold4(sim, 0.2f);
            Assert.AreEqual(EnemyState.Telegraph, imp.State, "redemoinho");
            Assert.AreEqual(Hint.ImpSwap, r.Hint);
            ForgeMark sw = null; foreach (ForgeMark m in r.Forge.Marks) if (m.Kind == ForgeMarkKind.Swap) sw = m;
            Assert.IsNotNull(sw); Assert.Less(V2.Dist(sw.Pos, cargo0), 0.05f, "redemoinho na carga");
            evs.AddRange(Hold4(sim, 1.0f));
            Assert.AreEqual(1, CountKind4(evs, Ev.Strike, (int)EnemyType.Imp), "trocou");
            Assert.Less(V2.Dist(r.Main.Pos, imp0), 0.6f, "a carga aparece onde ele estava");
            Assert.Less(V2.Dist(imp.Pos, cargo0), 0.6f, "ele aparece onde a carga estava");
            Assert.AreEqual(CargoState.Ground, r.Main.State);

            // golpe no redemoinho: cancela e atordoa 0,5 s
            Sim s2 = InRoomWith(C4(7), 3, Room4(Regiao4.R4), CargoType.Box); Run r2 = s2.Run; r2.Courier.Hp = 99;
            Steps(s2, 1, Up); r2.Courier.Pos = new V2(4.5f, 3f); Hold4(s2, 0.3f);
            Enemy i2 = Only4(r2, EnemyType.Imp, new V2(4.5f, 7.6f)); i2.Cd = 0f; i2.Hp = 50;
            V2 c2 = r2.Main.Pos;
            evs = Hold4(s2, 0.1f);
            Assert.AreEqual(EnemyState.Telegraph, i2.State);
            r2.Courier.ShotCd = 0f;
            evs.AddRange(Hold4(s2, 1.2f, shoot: true));
            Assert.AreEqual(0, CountKind4(evs, Ev.Strike, (int)EnemyType.Imp), "golpe no redemoinho cancela a troca");
            Assert.IsTrue(Has(evs, Ev.Stun));
            Assert.Less(V2.Dist(r2.Main.Pos, c2), 0.05f, "a carga ficou");

            // sem linha (carga atras da bigorna): nao troca
            Sim s4 = InRoomWith(C4(7), 3, Room4(Regiao4.R4), CargoType.Box); Run r4 = s4.Run; r4.Courier.Hp = 99;
            Steps(s4, 1, Up); r4.Courier.Pos = new V2(1.4f, 5.2f); Hold4(s4, 0.3f);
            Enemy i4 = Only4(r4, EnemyType.Imp, new V2(2.0f, 10.5f)); i4.Cd = 0f; i4.Stun = 0f;
            r4.Main.Pos = new V2(2.0f, 5.6f);   // atras da bigorna (2; 6,5)
            evs = Hold4(s4, 0.5f);
            Assert.AreNotEqual(EnemyState.Telegraph, i4.State, "sem ver a carga nao troca");

            // sem carga no chao: some e surge 1 m atras do courier (aviso 0,5 s) e arranha
            Sim s3 = InRoomWith(C4(7), 3, Room4(Regiao4.R4), CargoType.Box); Run r3 = s3.Run; r3.Courier.Hp = 99;   // parado antes do 1o movimento: carga nas maos
            Enemy i3 = Only4(r3, EnemyType.Imp, new V2(4.5f, 6f)); i3.Cd = 0f;
            int hp = r3.Courier.Hp;
            evs = Steps(s3, StepsFor(0.7f), V2.Zero);
            Assert.AreEqual(CargoState.Held, r3.Main.State);
            Assert.AreEqual(1, CountKind4(evs, Ev.Telegraph, (int)EnemyType.Imp)); Assert.AreEqual(1, CountKind4(evs, Ev.Strike, (int)EnemyType.Imp));
            Assert.Less(i3.Pos.Y, r3.Courier.Pos.Y, "surge atras (o courier olha para cima)");
            Assert.AreEqual(hp - 1, r3.Courier.Hp, "arranha");
        }

        [Test]
        public void Elemental_PancadaRachaACargaNoChao_MorreEDivideEmDuasBrasas_OvoChocaEm6s()
        {
            Sim sim = InRoomWith(C4(7), 1, Room4(Regiao4.R6), CargoType.Box); Run r = sim.Run; r.Courier.Hp = 99;   // R4-6 pelo M4-8 (caixa: a planta morderia as brasas)
            Assert.AreEqual("R4-6", r.Room.Code);
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 4f); Hold4(sim, 0.3f);
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            Enemy m = Only4(r, EnemyType.MagmaElemental, r.Main.Pos + new V2(0f, 1.0f)); m.Hp = 99;
            List<SimEvent> evs = Hold4(sim, 1.2f);
            Assert.AreEqual(1, CountKind4(evs, Ev.Strike, (int)EnemyType.MagmaElemental));
            Assert.AreEqual(1, r.Main.Cracks, "a pancada racha a carga no chao no alcance");
            m.Hp = 1; r.Courier.ShotCd = 0f;
            for (int i = 0; i < StepsFor(1.5f) && m.Alive; i++) Hold4(sim, Dt, shoot: true);
            Assert.IsFalse(m.Alive);
            Assert.AreEqual(2, CountType4(r, EnemyType.MagmaShard), "divide em 2 brasas");
            foreach (Enemy e in r.Enemies) if (e.Type == EnemyType.MagmaShard) Assert.AreEqual(4, e.Hp, "brasa: 4 sem escala");

            var s2 = Sim4(); Run r2 = Start(s2, C4(0)); r2.Courier.Hp = 99;   // parado antes do 1o movimento: nao atira
            Enemy egg = Only4(r2, EnemyType.SalamanderEgg, new V2(4.5f, 9f));
            evs = Steps(s2, StepsFor(5.9f), V2.Zero);
            Assert.IsTrue(egg.Alive); Assert.AreEqual(0, CountType4(r2, EnemyType.Salamander));
            evs = Steps(s2, StepsFor(0.2f), V2.Zero);
            Assert.IsFalse(egg.Alive); Assert.AreEqual(1, CountType4(r2, EnemyType.Salamander), "choca em 6 s numa salamandra");
            Assert.IsTrue(Has(evs, Ev.EggHatch));
        }

        // ------------------------------------------------------------------ chefes

        [Test]
        public void Capataz_ChicotePuxaACargaERacha_Frasco_Apito14sChama2Kobolds_Fase2Giro_TrancaAPorta()
        {
            Sim sim = InRoomWith(C4(7), 5, Room4(Regiao4.M), CargoType.Box);
            Run r = sim.Run; r.Courier.Hp = 99;
            Enemy f = null; foreach (Enemy e in r.Enemies) if (e.Type == EnemyType.KoboldForeman) f = e;
            Assert.IsNotNull(f); r.Enemies.RemoveAll(e => e != f);
            Assert.AreEqual((int)System.Math.Round(18 * Balance.HpMul(23)), f.Hp, "66 de vida no M4-8");
            Assert.AreEqual(1, r.Forge.Phase);
            Steps(sim, 1, Left);
            r.Courier.Pos = new V2(4.5f, 8.8f);
            List<SimEvent> evs = Hold4(sim, 0.25f);
            Assert.IsTrue(r.Doors[0].Locked, "chefe tranca");
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            r.Main.Pos = new V2(4.5f, 9.4f); f.Pos = new V2(4.5f, 11f); f.Cd = 0f;
            float y0 = r.Main.Pos.Y; int hp = r.Courier.Hp;
            evs = Hold4(sim, 1.0f);
            Assert.AreEqual(1, CountKind4(evs, Ev.Strike, (int)EnemyType.KoboldForeman), "chicote");
            Assert.AreEqual(hp - 2, r.Courier.Hp, "chicote: 2 de dano");
            Assert.Greater(r.Main.Pos.Y, y0 + 1.0f, "o chicote puxa a carga para ele");
            Assert.AreEqual(1, r.Main.Cracks);
            // frasco a cada 5 s e apito a cada 14 s (2 kobolds)
            bool flask = false; int calls = 0;
            for (int i = 0; i < StepsFor(13.5f) && sim.InContract; i++)
            {
                r.Courier.Hp = 99; f.Hp = 99;
                evs = Hold4(sim, Dt);
                foreach (ForgeMark m in r.Forge.Marks) if (m.Kind == ForgeMarkKind.Flask && m.Owner == f.Id) { flask = true; Assert.AreEqual(Balance.ForemanFlaskR, m.Radius); }
                foreach (SimEvent e in evs) if (e.Kind == Ev.ForemanCall) { calls++; Assert.AreEqual(2, e.B, "chama 2 kobolds"); }
            }
            Assert.IsTrue(flask, "frasco do Capataz"); Assert.AreEqual(1, calls, "apito aos 14 s");
            Assert.AreEqual(2, CountType4(r, EnemyType.KoboldBomber));
            // fase 2 (50%): alterna chicote e giro de 3 m
            r.Enemies.RemoveAll(e => e != f);
            f.Hp = 30; bool spin = false;
            for (int i = 0; i < StepsFor(8f) && !spin && sim.InContract; i++)
            {
                r.Courier.Hp = 99; r.Courier.Pos = f.Pos + new V2(0f, -2.2f);
                evs = Hold4(sim, Dt);
                if (Has(evs, Ev.BossPhase)) Assert.AreEqual(2, r.Forge.Phase);
                foreach (ForgeMark m in r.Forge.Marks) if (m.Kind == ForgeMarkKind.Spin) { spin = true; Assert.AreEqual(Balance.SpinR, m.Radius); }
            }
            Assert.AreEqual(2, r.Forge.Phase); Assert.IsTrue(spin, "fase 2: giro");
        }

        [Test]
        public void Dragao_SoproEmCone_PilarProtege_Ovos_VooIntocavel_PousoECabecaBaixaX2_Fase3Varre()
        {
            Sim sim = InRoomWith(C4(15), 4, Room4(Regiao4.B), CargoType.Box);
            Run r = sim.Run; r.Courier.Hp = 99;
            Enemy d = null; foreach (Enemy e in r.Enemies) if (e.Type == EnemyType.ElderDragon) d = e;
            Assert.AreEqual((int)System.Math.Round(48 * Balance.HpMul(31)), d.Hp, "221 de vida no M4-16");
            Assert.IsTrue(r.Doors.Count == 1 && r.Doors[0].Locked);
            // sopro: aviso de 1,4 s em cone para onde o courier estava; 3 de dano
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 4.7f);
            List<SimEvent> evs = Hold4(sim, 0.1f);
            ForgeMark b = null; foreach (ForgeMark m in r.Forge.Marks) if (m.Kind == ForgeMarkKind.Breath) b = m;
            Assert.IsNotNull(b, "aviso do sopro"); Assert.AreEqual(1.4f, b.Total, 1e-4f); Assert.Less(b.Dir.Y, -0.9f, "cone na direcao do courier");
            int hp = r.Courier.Hp;
            evs = Hold4(sim, 2.4f);
            Assert.AreEqual(1, CountKind4(evs, Ev.Strike, (int)EnemyType.ElderDragon));
            Assert.AreEqual(hp - 3, r.Courier.Hp, "sopro: 3 de dano (1x)");
            Assert.IsTrue(r.Forge.Fires.Exists(x => x.Source == 2), "brasas do sopro");
            // atras do pilar (2; 9,5) o cone nao pega
            Sim s2 = InRoomWith(C4(15), 4, Room4(Regiao4.B), CargoType.Box); Run r2 = s2.Run; r2.Courier.Hp = 99;
            Enemy d2 = null; foreach (Enemy e in r2.Enemies) if (e.Type == EnemyType.ElderDragon) d2 = e;
            Steps(s2, 1, Up); r2.Courier.Pos = new V2(1.6f, 8.3f); d2.Pos = new V2(3.4f, 11f);
            hp = r2.Courier.Hp;
            evs = Hold4(s2, 2.6f);
            Assert.AreEqual(1, CountKind4(evs, Ev.Strike, (int)EnemyType.ElderDragon), "soprou");
            Assert.AreEqual(hp, r2.Courier.Hp, "a sombra do pilar protege");
            // ovos: 2 a cada 20 s (o 1o aos 10 s), a 3 m+ do courier; chocam em salamandra
            int eggs = 0;
            for (int i = 0; i < StepsFor(10f) && s2.InContract; i++) { r2.Courier.Hp = 99; Hold4(s2, Dt); eggs = System.Math.Max(eggs, CountType4(r2, EnemyType.SalamanderEgg)); }
            Assert.AreEqual(Balance.DragonEggs, eggs, "2 ovos");
            foreach (Enemy e in r2.Enemies) if (e.Type == EnemyType.SalamanderEgg) Assert.GreaterOrEqual(V2.Dist(e.Pos, r2.Courier.Pos), 2.5f);

            // fase 2: voa (intocavel), pousa onde a sombra parou (3 de dano), cabeca baixa = dano x2
            d.Hp = d.Hp / 2; bool took = false, flyChecked = false, landed = false, doubled = false;
            r.Courier.Pos = new V2(4.5f, 4f);
            for (int i = 0; i < StepsFor(14f) && sim.InContract && !doubled; i++)
            {
                r.Courier.Hp = 99; r.Enemies.RemoveAll(e => e != d);
                evs = Hold4(sim, Dt, shoot: true);
                if (Has(evs, Ev.TakeOff)) took = true;
                if (r.Forge.Flying && took && V2.Dist(d.Pos, r.Courier.Pos) < 3f) { flyChecked = true; Assert.IsFalse(r.CanHit(d), "no ar nao e alvo"); }
                if (r.Forge.Flying) foreach (SimEvent e in evs) Assert.IsFalse(e.Kind == Ev.EnemyHit && e.A == d.Id, "no ar nao leva dano");
                foreach (SimEvent e in evs) if (e.Kind == Ev.Land) { landed = true; Assert.AreEqual(1, e.B, "o pouso acertou o courier parado"); }
                if (landed && r.Forge.HeadDown > 0f) foreach (SimEvent e in evs) if (e.Kind == Ev.EnemyHit && e.A == d.Id) doubled = true;
            }
            Assert.AreEqual(2, r.Forge.Phase); Assert.IsTrue(took, "decolou"); Assert.IsTrue(flyChecked); Assert.IsTrue(landed, "pousou");
            Assert.IsTrue(doubled, "acertou na cabeca baixa");
            // dano x2 medido direto
            var s4 = InRoomWith(C4(15), 4, Room4(Regiao4.B), CargoType.Box); Run r4 = s4.Run;
            Enemy d4 = null; foreach (Enemy e in r4.Enemies) if (e.Type == EnemyType.ElderDragon) d4 = e;
            r4.Forge.HeadDown = 2f; r4.Courier.Pos = d4.Pos + new V2(0f, -4f); r4.Courier.Hp = 99;
            Steps(s4, 1, Up); int before = d4.Hp; bool hit = false;
            for (int i = 0; i < StepsFor(1.2f) && !hit; i++) foreach (SimEvent e in Hold4(s4, Dt, shoot: true)) if (e.Kind == Ev.EnemyHit && e.A == d4.Id) { Assert.AreEqual(before - 2, e.B, "cabeca baixa: x2"); hit = true; }
            Assert.IsTrue(hit);

            // fase 3 (30%): o sopro varre (o cone gira)
            d.Hp = d.Hp * 30 / 100 - 1; if (d.Hp < 1) d.Hp = 1;
            ForgeMark sweep = null; V2 dir0 = V2.Zero; bool turned = false;
            for (int i = 0; i < StepsFor(16f) && sim.InContract && !turned; i++)
            {
                r.Courier.Hp = 99; r.Enemies.RemoveAll(e => e != d); d.Hp = System.Math.Max(d.Hp, 2);
                Hold4(sim, Dt);
                foreach (ForgeMark m in r.Forge.Marks)
                    if (m.Kind == ForgeMarkKind.Breath && m.Active > 0f)
                    {
                        if (sweep != m) { sweep = m; dir0 = m.Dir; }
                        else if (V2.Dot(m.Dir, dir0) < 0.9f) turned = true;
                    }
            }
            Assert.AreEqual(3, r.Forge.Phase); Assert.IsTrue(turned, "fase 3: sopro em varredura");
        }

        // ------------------------------------------------------------------ cargas

        [Test]
        public void Planta_NasMaosNaoMorde_NoChaoMordeInimigoA12m_MordeOCourierParadoAoLado_LadraoQueALevaSolta()
        {
            var sim = Sim4(); Run r = Start(sim, C4(1)); r.Courier.Hp = 99;   // M4-2: planta
            Assert.AreEqual(CargoType.Flytrap, r.Main.Type);
            Enemy br = Only4(r, EnemyType.Brute, r.Courier.Pos + new V2(1.0f, 0.4f)); br.Stun = 99f; int b0 = br.Hp;
            List<SimEvent> evs = Steps(sim, StepsFor(1.5f), V2.Zero);   // nas maos (antes do 1o movimento)
            Assert.AreEqual(b0, br.Hp, "nas maos nao morde"); Assert.IsFalse(Has(evs, Ev.PlantBite));
            Steps(sim, 1, Up); evs = Hold4(sim, 0.25f);
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            br.Pos = r.Main.Pos + new V2(1.0f, 0f);
            evs = Hold4(sim, 0.5f);
            Assert.AreEqual(b0 - Balance.PlantDamage, br.Hp, "morde o inimigo a 1,2 m (2 de dano)");
            Assert.AreEqual(Hint.PlantBite, r.Hint, "o courier parado ao lado: dica");
            int hp = r.Courier.Hp;
            evs = Hold4(sim, 0.6f);
            Assert.AreEqual(hp - 1, r.Courier.Hp, "morde o courier parado ao lado depois de 0,8 s");
            Assert.IsTrue(CountKind4(evs, Ev.PlantBite, r.Main.Id) >= 1);
            // o "passo" e o dash: longe dela, nao morde
            r.Enemies.Clear();
            Steps(sim, 1, Left, true); Steps(sim, StepsFor(0.3f), V2.Zero);
            Assert.Greater(V2.Dist(r.Courier.Pos, r.Main.Pos), 2.5f);
            hp = r.Courier.Hp; Hold4(sim, 2f);
            Assert.AreEqual(hp, r.Courier.Hp, "longe da planta nao morde");
            // ladrao que a pega leva 1 por segundo e a solta
            Enemy th = r.Spawn(EnemyType.Thief, r.Main.Pos + new V2(0.3f, 0f)); th.Hp = 30;
            bool grabbed = false, dropped = false;
            for (int i = 0; i < StepsFor(3f) && !dropped; i++)
                foreach (SimEvent e in Hold4(sim, Dt)) { if (e.Kind == Ev.ThiefGrab) grabbed = true; if (grabbed && e.Kind == Ev.CargoDrop && e.B == 2) dropped = true; }
            Assert.IsTrue(grabbed); Assert.IsTrue(dropped, "a planta morde o ladrao e ele solta");
        }

        [Test]
        public void Mapa_RevelaBauEnterradoEParedeDisfarcada_LadroesMaisRapidosAtrasDele_LadraoDaCargaSozinhaEm6s()
        {
            var sim = Sim4(); Run r = Start(sim, C4(2));   // M4-3: mapa, comeca na R4-9
            Assert.AreEqual("R4-9", r.Room.Code); Assert.IsTrue(r.Revealed);
            Assert.IsNotNull(r.Secret, "parede disfarcada aparece com o mapa"); Assert.AreEqual(1, r.Buried.Count);
            Sim other = InRoomWith(C4(3), 3, Room4(Regiao4.R9), CargoType.Relic);   // M4-4 passa pela R4-9 com a reliquia
            Assert.IsFalse(other.Run.Revealed); Assert.IsNull(other.Run.Secret, "sem mapa a parede fica disfarcada");
            // abre o bau enterrado parado 1,5 s
            r.Enemies.Clear(); r.Courier.Hp = 99;
            Steps(sim, 1, Up); r.Courier.Pos = r.Buried[0].Pos + new V2(0.6f, 0f);
            List<SimEvent> evs = Hold4(sim, 1.8f);
            Assert.IsTrue(r.Buried[0].Opened); Assert.AreEqual(1, CountKind4(evs, Ev.ChestOpen, 1));

            // ladrao +30% correndo para o mapa no chao (comparado com a caixa no mesmo lugar)
            float[] dist = new float[2];
            CargoType[] kinds = { CargoType.TreasureMap, CargoType.Box };
            for (int k = 0; k < 2; k++)
            {
                Sim s = k == 0 ? InRoomWith(C4(2), 0, Room4(Regiao4.R9), CargoType.TreasureMap) : InRoomWith(C4(7), 2, Room4(Regiao4.R9), CargoType.Box);
                Run rr = s.Run; rr.Courier.Hp = 99;
                Steps(s, 1, Up); rr.Courier.Pos = new V2(4.5f, 3f); Hold4(s, 0.3f);
                Enemy th = Only4(rr, EnemyType.Thief, new V2(8.2f, 5.5f));
                V2 p0 = th.Pos; Hold4(s, 0.8f);
                dist[k] = V2.Dist(th.Pos, p0);
            }
            Assert.AreEqual(1.3f, dist[0] / dist[1], 0.08f, "ladrao 30% mais rapido atras do mapa");
            // carga sozinha (a mais de 3 m): ladrao em 6 s com o mapa
            var s3 = Sim4(); Run r3 = Start(s3, C4(2)); r3.Enemies.Clear(); r3.Courier.Hp = 99;
            Steps(s3, 1, Up); Hold4(s3, 0.3f);
            r3.Courier.Pos = new V2(4.5f, 9f);
            float spawn = -1f;
            for (int i = 0; i < StepsFor(9f) && spawn < 0f; i++) { r3.Courier.Pos = new V2(4.5f, 9f); if (Has(Hold4(s3, Dt), Ev.ThiefSpawn)) spawn = r3.Time; }
            Assert.That(spawn - 0.3f - Dt, Is.InRange(5.5f, 6.4f), "ladrao da carga sozinha em 6 s (8 sem o mapa)");
        }

        // ------------------------------------------------------------------ campanha e sem fim

        /// <summary>A campanha da regiao 4 pelo bot, partindo do estado estimado do fim da regiao 3 (Bot.Regiao3EndSave): fecha os 16 e vence o Dragao.</summary>
        [Test]
        public void Campanha_Regiao4_PeloBot_FechaOs16_DragaoVencivel()
        {
            RegionDef reg = RegionDef.Of(4);
            int finished = 0, attempts = 0;
            for (int s = 0; s < 4; s++)
            {
                Bot.Campaign cp = Bot.PlayCampaign(7100 + s, region: 4, from: Bot.Regiao3EndSave(7100 + s), maxAttempts: 80);
                if (cp.Finished) finished++;
                attempts += cp.Attempts;
                for (int c = reg.First; c < reg.First + reg.Contracts.Length; c++) if (cp.Finished) Assert.GreaterOrEqual(cp.AttemptsPer[c], 1, Sim.Contracts[c].Code);
            }
            Assert.GreaterOrEqual(finished, 3, "o bot fecha a regiao 4 (Dragao vencivel)");
            Assert.LessOrEqual(attempts / 4.0, 16 * 2.0, "tentativas demais por contrato");
        }

        [Test]
        public void SemFim_UsaAsSalasEOsChefesDaRegiao4_CapatazEDragaoNoRitmo()
        {
            var sim = Sim4(8);
            int d = Monday(20261012); sim.SeeDate(d);
            Assert.IsTrue(sim.EndlessOpen); Assert.IsTrue(sim.RegionOpen(4));
            ContractDef e = sim.EndlessContract(d);
            Assert.AreEqual(4, e.Region, "regiao mais alta: a 4 (cargas e entrega dela)");
            RegionDef reg = RegionDef.Of(4);
            int mini = -1, boss = -1, normal4 = 0;
            for (int l = 0; l < e.Layers.Length - 1; l++)
                foreach (int id in e.Layers[l])
                {
                    if (id == reg.Mini && mini < 0) mini = l;
                    if (id == reg.Boss && boss < 0) boss = l;
                    if (System.Array.IndexOf(reg.Normal, id) >= 0) normal4++;
                }
            Assert.Greater(normal4, 10, "salas normais da regiao 4 no sorteio");
            Assert.GreaterOrEqual(mini, 0, "Capataz no sem fim"); Assert.GreaterOrEqual(boss, 0, "Dragao no sem fim");
            Assert.AreEqual(0, (mini + 1) % Balance.EndlessMiniEvery); Assert.AreNotEqual(0, (mini + 1) % Balance.EndlessBossEvery);
            Assert.AreEqual(0, (boss + 1) % Balance.EndlessBossEvery);
            Run r = sim.StartEndless(d);
            bool sawForeman = false, sawDragon = false;
            for (int l = 0; l <= boss && sim.InContract; l++)
            {
                if (r.Room.Id == reg.Mini || r.Room.Id == reg.Boss)
                {
                    EnemyType t = r.Room.Id == reg.Mini ? EnemyType.KoboldForeman : EnemyType.ElderDragon;
                    Enemy b = null; foreach (Enemy x in r.Enemies) if (x.Type == t) b = x;
                    Assert.IsNotNull(b, t + " na sala dele");
                    Assert.AreEqual(1, r.Forge.Phase); Assert.IsTrue(r.Doors[0].Locked);
                    Assert.AreEqual((int)System.Math.Round(EnemyDef.All[(int)t].Hp * Balance.HpMul(r.Tier)), b.Hp, "vida pelo nivel da camada");
                    if (t == EnemyType.KoboldForeman) sawForeman = true; else sawDragon = true;
                }
                r.Courier.Hp = r.Courier.MaxHp;
                Cross(sim);
            }
            Assert.IsTrue(sawForeman); Assert.IsTrue(sawDragon);
        }
    }
}
