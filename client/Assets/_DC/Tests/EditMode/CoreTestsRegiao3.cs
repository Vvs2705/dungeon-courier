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
        // ------------------------------------------------------------------ helpers da regiao 3

        /// <summary>Sim com a campanha toda aberta (estrelas para a regiao 3), guilda 0, estilingue 1, sem equipamento.</summary>
        static Sim SimR3(int seed = 7) { var s = new Sim(seed) { Reputation = Sim.Contracts.Length }; s.DevStars(999); return s; }
        static int CR3(int k) => RegionDef.Of(3).First + k;
        static int RoomR3(int local) => RegionDef.Of(3).Rooms[local];
        /// <summary>Comeca o M3-(k+1) sem os inimigos da 1a sala e com vida de sobra.</summary>
        static Run StartR3(Sim s, int k) { Run r = s.StartContract(CR3(k), false); Assert.IsNotNull(r, "M3-" + (k + 1)); r.Enemies.Clear(); r.Courier.Hp = 99; return r; }
        /// <summary>Parado (pousa a carga) por `s` segundos; shoot = false segura o tiro.</summary>
        static List<SimEvent> StillR3(Sim sim, float s, bool shoot = true)
        {
            var evs = new List<SimEvent>();
            for (int i = 0; i < StepsFor(s) && sim.InContract; i++) { if (!shoot) sim.Run.Courier.ShotCd = 1f; evs.AddRange(Steps(sim, 1, V2.Zero)); }
            return evs;
        }
        /// <summary>Andando quase parado (carga nas maos, sem atirar) por `s` segundos na direcao `dir`.</summary>
        static List<SimEvent> InchR3(Sim sim, float s, V2 dir)
        {
            var evs = new List<SimEvent>();
            for (int i = 0; i < StepsFor(s) && sim.InContract; i++) evs.AddRange(Steps(sim, 1, dir * 0.06f));
            return evs;
        }
        static void PickR3(Sim sim) { Run r = sim.Run; r.Courier.Pos = r.Main.Pos + new V2(-0.3f, 0f); Steps(sim, 1, new V2(0f, 0.2f)); Assert.AreEqual(CargoState.Held, r.Main.State, "pegar"); }
        static int CountR3(List<SimEvent> evs, Ev k, int b = int.MinValue) { int n = 0; foreach (SimEvent e in evs) if (e.Kind == k && (b == int.MinValue || e.B == b)) n++; return n; }
        static Enemy FindR3(Run r, EnemyType t) { foreach (Enemy e in r.Enemies) if (e.Alive && e.Type == t) return e; return null; }

        // ------------------------------------------------------------------ registro e level design

        [Test]
        public void Regiao3_Registro_SalasContratosCargasEInimigos()
        {
            RegionDef reg = RegionDef.Of(3);
            Assert.IsTrue(reg.HasContent); Assert.AreEqual("cripta", reg.Theme); Assert.AreEqual(Regiao3.Name, reg.Name); Assert.AreEqual(60, reg.Stars);
            Assert.AreEqual(14, reg.Rooms.Length); Assert.AreEqual(16, reg.Contracts.Length); Assert.AreEqual(10, reg.Normal.Length);
            string[] codes = { "R3-1", "R3-2", "R3-3", "R3-4", "R3-5", "R3-6", "R3-7", "R3-8", "R3-9", "R3-10", "S3-1", "M3", "B3", "D3" };
            for (int i = 0; i < codes.Length; i++) { Assert.AreEqual(codes[i], Content.Rooms[reg.Rooms[i]].Code); Assert.AreEqual(3, Content.Rooms[reg.Rooms[i]].Region); }
            Assert.AreEqual(RoomR3(Regiao3.M), reg.Mini); Assert.AreEqual(RoomR3(Regiao3.B), reg.Boss); Assert.AreEqual(RoomR3(Regiao3.D), reg.Delivery);
            CollectionAssert.AreEqual(new[] { RoomR3(Regiao3.S1) }, reg.Secrets);
            Assert.AreEqual(CR3(7), reg.MiniAt, "Viuva no M3-8"); Assert.AreEqual(CR3(15), reg.BossAt, "Necromante no M3-16");
            Assert.AreEqual(RoomR3(Regiao3.S1), Content.Rooms[RoomR3(Regiao3.R4)].Secret); Assert.AreEqual(RoomR3(Regiao3.S1), Content.Rooms[RoomR3(Regiao3.R9)].Secret);
            var bp = new Dictionary<Gear, int>();
            foreach (int id in reg.Rooms) { Gear g = Content.Rooms[id].Blueprint; if (g != Gear.None) { bp[g] = (bp.TryGetValue(g, out int n) ? n : 0) + 1; Assert.Greater(Content.Rooms[id].ChestCoins, 0, "projeto sem bau"); } }
            Assert.AreEqual(3, bp[Gear.Hook], "gancho completa na regiao 3: R3-2, R3-5, R3-9 (raia E)"); Assert.AreEqual(3, bp[Gear.Smoke], "fumaca: R3-4, R3-6, S3-1"); Assert.AreEqual(2, bp.Count);

            int pay = 0; var debut = new Dictionary<CargoType, int>();
            for (int k = 0; k < reg.Contracts.Length; k++)
            {
                ContractDef d = reg.Contracts[k];
                Assert.AreEqual("M3-" + (k + 1), d.Code); Assert.AreEqual(CR3(k), d.Index); Assert.AreEqual(3, d.Region);
                Assert.AreEqual(12 + k, d.Tier, d.Code + ": nivel = 12 + indice"); Assert.AreEqual(k >= 3, d.Waves, d.Code + ": os 3 que ensinam sem reforco");
                Assert.Greater(d.ParTime, 0f, d.Code); Assert.AreEqual(d.Cargo == CargoType.Message, d.HasTimer, d.Code); Assert.AreEqual(d.Cargo == CargoType.Egg, d.EggTarget > 0f, d.Code);
                Assert.AreEqual(reg.Delivery, d.Layers[d.Layers.Length - 1][0], d.Code + ": termina na D3");
                int layers = d.Layers.Length - 1; bool boss = false;
                foreach (int id in d.Layers[layers - 1]) if (Content.Rooms[id].Boss || Content.Rooms[id].Mini) boss = true;
                if (k < 3) Assert.That(layers, Is.InRange(2, 3), d.Code + ": tutorial"); else Assert.That(layers - (boss ? 1 : 0), Is.InRange(4, 6), d.Code);
                for (int l = 0; l < layers; l++) foreach (int id in d.Layers[l]) Assert.IsTrue(Content.Rooms[id].Normal || (l == layers - 1 && boss), d.Code + ": camada " + l);
                Assert.AreEqual(k == 7, System.Array.IndexOf(d.Layers[layers - 1], reg.Mini) >= 0, d.Code + ": Viuva so no M3-8");
                Assert.AreEqual(k == 15, System.Array.IndexOf(d.Layers[layers - 1], reg.Boss) >= 0, d.Code + ": Necromante so no M3-16");
                pay += d.Pay;
                if (!debut.ContainsKey(d.Cargo)) debut[d.Cargo] = k;
            }
            Assert.AreEqual(4672, pay, "pagamento somado da regiao 3 (CONTEUDO_REGIOES §10.3)");
            Assert.AreEqual(0, debut[CargoType.Lantern]); Assert.AreEqual(2, debut[CargoType.Mirror]); Assert.AreEqual(4, debut[CargoType.Balloon]);

            foreach (EnemyDef d in EnemyDef.All) if (d.Region == 3) Assert.IsFalse(d.Provisional, d.Name + ": numeros medidos");
            EnemyDef gh = EnemyDef.All[(int)EnemyType.Ghost], sk = EnemyDef.All[(int)EnemyType.Skeleton], sp = EnemyDef.All[(int)EnemyType.Spider], mu = EnemyDef.All[(int)EnemyType.Mummy],
                     ga = EnemyDef.All[(int)EnemyType.Gargoyle], wi = EnemyDef.All[(int)EnemyType.SpiderWidow], ne = EnemyDef.All[(int)EnemyType.Necromancer];
            Assert.AreEqual(3, gh.Hp); Assert.AreEqual(1.8f, gh.Speed); Assert.AreEqual(CargoRisk.Thief, gh.Risk);
            Assert.AreEqual(5, sk.Hp); Assert.AreEqual(1.5f, sk.Speed); Assert.AreEqual(CargoRisk.None, sk.Risk);
            Assert.AreEqual(3, sp.Hp); Assert.AreEqual(2.0f, sp.Speed); Assert.AreEqual(CargoRisk.None, sp.Risk);
            Assert.AreEqual(9, mu.Hp); Assert.AreEqual(1.0f, mu.Speed); Assert.AreEqual(CargoRisk.None, mu.Risk);
            Assert.AreEqual(6, ga.Hp); Assert.AreEqual(0f, ga.Speed); Assert.AreEqual(2, ga.Damage); Assert.AreEqual(CargoRisk.Smash, ga.Risk);
            Assert.AreEqual(20, wi.Hp); Assert.IsTrue(wi.Locks && wi.Boss && wi.Mini); Assert.AreEqual(CargoRisk.Thief, wi.Risk);
            Assert.AreEqual(36, ne.Hp); Assert.IsTrue(ne.Locks && ne.Boss && !ne.Mini); Assert.AreEqual(CargoRisk.Archer, ne.Risk);
            CargoDef la = CargoDef.All[(int)CargoType.Lantern], mi = CargoDef.All[(int)CargoType.Mirror], ba = CargoDef.All[(int)CargoType.Balloon];
            Assert.AreEqual(120, Balance.Pct(la.PayMul)); Assert.AreEqual(145, Balance.Pct(mi.PayMul)); Assert.AreEqual(135, Balance.Pct(ba.PayMul));
            foreach (CargoDef c in new[] { la, mi, ba }) { Assert.IsFalse(c.Provisional, c.Name); Assert.AreEqual(3, c.Region, c.Name); }
            Assert.IsTrue(mi.Fragile, "espelho entra na Fragile Week"); Assert.IsFalse(ba.Fragile, "balao nao");
            foreach (Hint h in new[] { Hint.LanternOut, Hint.Web, Hint.GhostCargo, Hint.SkeletonRise, Hint.MummyGrab, Hint.GargoyleWake, Hint.MirrorFront, Hint.BalloonRise })
                Assert.AreNotEqual("", Hints.Text(h), h.ToString());
        }

        [Test]
        public void LevelDesign_Regiao3_EscuroComTochas_TeiasForaDaEntradaEDasPortas_TudoAlcancavel()
        {
            foreach (int id in RegionDef.Of(3).Rooms)
            {
                RoomDef room = Content.Rooms[id]; string k = room.Code;
                Assert.AreEqual(!room.Delivery, room.Dark, k + ": escura (menos a entrega)");
                if (room.Dark) Assert.GreaterOrEqual(room.Torches.Length, 2, k + ": sala escura com 2+ tochas");
                foreach (V2 t in room.Torches) { Assert.IsTrue(t.X == 0.3f || t.X == 8.7f, k + ": tocha fora da parede lateral"); Assert.That(t.Y, Is.InRange(2f, 12.2f), k + ": tocha"); }
                var points = new List<Spawn>(room.Spawns); points.AddRange(room.Wave);
                foreach (Spawn s in points)
                {
                    Assert.GreaterOrEqual(V2.Dist(s.Pos, Balance.Entrance), 4f, k + ": spawn/reforco em cima da entrada");
                    Assert.IsFalse(Run.Blocked(room, s.Pos, EnemyDef.All[(int)s.Type].Radius), k + ": spawn/reforco dentro de obstaculo");
                }
                foreach (Rect o in room.Obstacles) Assert.LessOrEqual(o.C.Y + o.H.Y, 12.2f, k + ": obstaculo na faixa das portas");
                Assert.IsFalse(Run.Blocked(room, Balance.Entrance, 1.2f), k + ": obstaculo na entrada");
                foreach (Web w in room.Webs)
                {
                    Assert.GreaterOrEqual(V2.Dist(w.Pos, Balance.Entrance), w.Radius + 1.2f, k + ": teia na entrada");
                    Assert.LessOrEqual(w.Pos.Y + w.Radius, 12.2f, k + ": teia na faixa das portas");
                    Assert.Greater(V2.Dist(w.Pos, room.RescueSpot), w.Radius, k + ": resgate dentro da teia");
                }
                foreach (V2 b in room.Blinks) { Assert.IsFalse(Run.Blocked(room, b, 0.5f), k + ": ponto de salto em obstaculo"); Assert.GreaterOrEqual(V2.Dist(b, Balance.Entrance), 4f, k + ": salto na entrada"); }
                var nav = new Nav(room);
                for (int n = 1; n <= 3; n++) for (int i = 0; i < n; i++) Assert.Less(nav.Steps(Balance.Entrance, new V2(Balance.DoorX(n, i), 13.5f)), int.MaxValue, k + ": porta inalcancavel");
                var spots = new List<V2> { room.RescueSpot };
                if (room.ChestCoins > 0) spots.Add(room.ChestPos);
                foreach (BuriedChest b in room.Buried) spots.Add(b.Pos);
                foreach (V2 p in spots) { Assert.IsFalse(Run.Blocked(room, p, 0.3f), k + ": bau/resgate dentro de obstaculo"); Assert.Less(nav.Steps(Balance.Entrance, p), int.MaxValue, k + ": bau/resgate inalcancavel"); }
                if (room.Secret < 0) continue;
                Assert.IsTrue(room.SecretPos.X == 0f || room.SecretPos.X == Balance.RoomW, k + ": parede rachada fora da lateral");
                Assert.That(room.SecretPos.Y, Is.InRange(1.5f, 12f), k);
                V2 inside = new V2(room.SecretPos.X == 0f ? 0.5f : Balance.RoomW - 0.5f, room.SecretPos.Y);
                Assert.IsFalse(Run.Blocked(room, inside, Balance.CourierR), k + ": parede rachada atras de obstaculo");
                Assert.Less(nav.Steps(Balance.Entrance, inside), int.MaxValue, k + ": parede rachada inalcancavel");
            }
        }

        // ------------------------------------------------------------------ escuro e lanterna

        [Test]
        public void Lanterna_IluminaNoEscuro_PousarNaoApaga_CairApaga_SoReacendeNaTocha_GolpeForteApaga()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 0);   // M3-1: lanterna na R3-1 (tochas em (0,3; 4), (8,7; 7), (0,3; 10,5))
            Assert.IsTrue(r.Dark); Assert.AreEqual(CargoType.Lantern, r.Main.Type);
            Steps(sim, 1, Up);
            r.Courier.Pos = new V2(4.5f, 2.0f);
            V2 far = new V2(4.5f, 6.8f);   // 4,8 m do courier e longe das tochas
            Assert.IsTrue(r.InLight(far), "acesa nas maos: 5,5 m");
            List<SimEvent> ev = Steps(sim, 8, V2.Zero);
            Assert.AreEqual(CargoState.Ground, r.Main.State); Assert.IsFalse(r.Main.Out, "pousar nao apaga"); Assert.IsFalse(Has(ev, Ev.LanternOut));
            PickR3(sim);
            ev = Steps(sim, 1, Up, true);   // o dash larga
            Assert.AreEqual(CargoState.Ground, r.Main.State); Assert.IsTrue(r.Main.Out, "cair apaga"); Assert.IsTrue(Has(ev, Ev.LanternOut));
            V2 drop = r.Main.Pos;
            Steps(sim, 8, V2.Zero);
            r.Courier.Pos = new V2(8.0f, 12.0f);
            Assert.IsFalse(r.InLight(drop + new V2(0f, 3f)), "apagada no chao nao ilumina");
            PickR3(sim);
            Assert.IsTrue(r.Main.Out, "pegar de volta nao reacende");
            r.Courier.Pos = new V2(4.5f, 2.0f);
            Assert.IsFalse(r.InLight(far), "apagada nas maos: so a luz do courier (3,5 m)");
            Steps(sim, 1, Right * 0.2f);
            Assert.AreEqual(Hint.LanternOut, r.Hint);
            // reacende carregada a 1 m da tocha por 0,5 s
            r.Courier.Pos = new V2(0.8f, 4.0f);
            ev = new List<SimEvent>();
            for (int i = 0; i < StepsFor(0.4f); i++) ev.AddRange(Steps(sim, 1, Left * 0.2f));
            Assert.IsTrue(r.Main.Out, "antes de 0,5 s ainda apagada");
            for (int i = 0; i < StepsFor(0.2f); i++) ev.AddRange(Steps(sim, 1, Left * 0.2f));
            Assert.IsFalse(r.Main.Out, "reacendeu na tocha"); Assert.IsTrue(Has(ev, Ev.LanternLit));
            Assert.IsTrue(r.InLight(r.Courier.Pos + new V2(5f, 0f)), "acesa de novo: 5,5 m");
            // golpe de 2 com ela nas maos apaga
            r.Courier.Pos = new V2(4.5f, 3.0f);
            Enemy b = r.Spawn(EnemyType.Brute, r.Courier.Pos + new V2(0f, 1.4f)); b.Hp = 99;
            bool heavy = false;
            for (int i = 0; i < StepsFor(4f) && !heavy; i++)
                foreach (SimEvent e in Steps(sim, 1, (i % 20 < 10 ? Left : Right) * 0.06f)) if (e.Kind == Ev.DamageTaken && e.A >= 2) heavy = true;
            Assert.IsTrue(heavy, "o Bruto nao acertou"); Assert.IsTrue(r.Main.Out, "golpe de 2+ com a lanterna nas maos apaga");
            // fora do escuro e so uma carga leve
            Sim s2 = SimR3(); Run r2 = StartR3(s2, 0);
            Steps(s2, 1, Up); r2.Dark = false;
            Steps(s2, 1, Up, true);
            Assert.AreEqual(CargoState.Ground, r2.Main.State); Assert.IsFalse(r2.Main.Out, "fora do escuro nao apaga");
        }

        // ------------------------------------------------------------------ espelho

        [Test]
        public void Espelho_RefleteAFlechaDeFrente_RachaACadaReflexo_PorTrasAcertaOCourier_CairRacha_TresQuebram()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 2);   // M3-3: espelho na R3-3
            Assert.AreEqual(CargoType.Mirror, r.Main.Type);
            Steps(sim, 1, Up);
            r.Courier.Pos = new V2(4.5f, 3.0f);
            Enemy a = r.Spawn(EnemyType.Archer, new V2(4.5f, 9.5f)); a.Hp = 99;
            int hp = r.Courier.Hp; bool refl = false;
            for (int i = 0; i < StepsFor(4f) && !refl; i++) foreach (SimEvent e in Steps(sim, 1, Up * 0.06f)) if (e.Kind == Ev.Reflect && e.A == 0) refl = true;
            Assert.IsTrue(refl, "a flecha de frente volta"); Assert.AreEqual(1, r.Main.Cracks, "cada reflexo racha 1"); Assert.AreEqual(hp, r.Courier.Hp, "de frente o courier nao leva");
            InchR3(sim, 1.2f, Up);
            Assert.AreEqual(99 - Balance.MirrorArrowDamage, a.Hp, "a flecha refletida tira 2 do arqueiro");
            // por tras: acerta o courier e nao racha
            a.Cd = 0f; bool hit = false;
            for (int i = 0; i < StepsFor(4f) && !hit; i++) foreach (SimEvent e in Steps(sim, 1, Down * 0.06f)) if (e.Kind == Ev.DamageTaken) hit = true;
            Assert.IsTrue(hit, "por tras a flecha acerta o courier"); Assert.AreEqual(1, r.Main.Cracks, "e nao racha o espelho");
            Assert.AreEqual(Hint.MirrorFront, HintWhileAiming(sim, a));
            // mais 2 reflexos: 3 rachaduras quebram
            for (int n = 0; n < 2 && sim.InContract; n++)
            {
                a.Cd = 0f; bool rf = false;
                for (int i = 0; i < StepsFor(3f) && !rf && sim.InContract; i++) foreach (SimEvent e in Steps(sim, 1, Up * 0.06f)) if (e.Kind == Ev.Reflect) rf = true;
                Assert.IsTrue(rf, "reflexo " + (n + 2));
            }
            Assert.IsFalse(sim.InContract); Assert.AreEqual(Outcome.CargoBroken, sim.LastResult.Outcome, "3 rachaduras quebram o espelho");
            // cair (dash) racha; pousar nao
            Sim s2 = SimR3(); Run r2 = StartR3(s2, 2);
            Steps(s2, 1, Up); Steps(s2, 8, V2.Zero);
            Assert.AreEqual(0, r2.Main.Cracks, "pousar nao racha");
            PickR3(s2); Steps(s2, 1, Up, true);
            Assert.AreEqual(1, r2.Main.Cracks, "cair racha o espelho");
        }

        /// <summary>Dica com o arqueiro mirando no courier (espelho nas maos).</summary>
        static Hint HintWhileAiming(Sim sim, Enemy a)
        {
            a.Cd = 0f;
            for (int i = 0; i < StepsFor(1f) && sim.InContract; i++) { Steps(sim, 1, Left * 0.06f); if (a.State == EnemyState.Telegraph) return sim.Run.Hint; }
            return Hint.None;
        }

        // ------------------------------------------------------------------ balao

        [Test]
        public void Balao_MaisRapidoCarregando_EscapaEm4sNoChao_PegarZera_LadraoNaoAlcanca()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 4);   // M3-5: balao na R3-5
            Assert.AreEqual(CargoType.Balloon, r.Main.Type);
            Steps(sim, 1, Right); V2 p0 = r.Courier.Pos; Steps(sim, 15, Right);
            Assert.AreEqual(Balance.CarrySpeed * Balance.BalloonSpeed * 15 * Dt, r.Courier.Pos.X - p0.X, 0.02f, "+15% carregando");
            Steps(sim, 8, V2.Zero);
            Assert.AreEqual(CargoState.Ground, r.Main.State); Assert.AreEqual(Hint.BalloonRise, r.Hint);
            Steps(sim, StepsFor(2.5f), V2.Zero);
            Assert.IsTrue(sim.InContract); Assert.Greater(r.Main.Fuse, 2.4f, "o contador sobe no chao");
            PickR3(sim);
            Assert.AreEqual(0f, r.Main.Fuse, "pegar zera");
            Steps(sim, 8, V2.Zero);
            Enemy t = r.Spawn(EnemyType.Thief, r.Main.Pos + new V2(1f, 0f)); t.Hp = 99;
            StillR3(sim, 1.5f);
            Assert.AreEqual(CargoState.Ground, r.Main.State, "o ladrao nao alcanca o balao"); Assert.IsNull(t.Carry);
            StillR3(sim, 3f);
            Assert.IsFalse(sim.InContract); Assert.AreEqual(Outcome.Escaped, sim.LastResult.Outcome, "4 s no chao: escapou");
        }

        // ------------------------------------------------------------------ fantasma

        [Test]
        public void Fantasma_AtravessaObstaculo_PossuiACargaNoChao_LevaAteAParedeESome()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 1);   // M3-2: caixa na R3-1 (pilar em (2,5; 9,0))
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 2.0f); Steps(sim, 8, V2.Zero);
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            r.Courier.Pos = new V2(8.2f, 13.0f);   // longe: fora da luz e do alcance
            Enemy g = r.Spawn(EnemyType.Ghost, new V2(2.5f, 10.0f));   // a linha ate a carga corta o pilar
            bool inside = false; var ev = new List<SimEvent>();
            for (int i = 0; i < StepsFor(8f) && r.Main.State == CargoState.Ground; i++) { ev.AddRange(Steps(sim, 1, V2.Zero)); if (Run.Blocked(r.Room, g.Pos, 0.05f)) inside = true; }
            Assert.IsTrue(inside, "o fantasma atravessa obstaculo");
            Assert.AreEqual(CargoState.Stolen, r.Main.State, "possuiu a carga");
            Assert.Greater(CountR3(ev, Ev.Telegraph, (int)EnemyType.Ghost), 0, "aura (aviso) antes de possuir"); Assert.IsTrue(Has(ev, Ev.ThiefGrab));
            Assert.AreEqual(Hint.GhostCargo, r.Hint);
            float x0 = r.Main.Pos.X; Steps(sim, StepsFor(1f), V2.Zero);
            Assert.AreEqual(Balance.GhostFloat, r.Main.Pos.X - x0, 0.05f, "flutua a 0,8 m/s para a parede lateral mais perto");
            for (int i = 0; i < StepsFor(9f) && sim.InContract; i++) Steps(sim, 1, V2.Zero);
            Assert.IsFalse(sim.InContract); Assert.AreEqual(Outcome.CargoStolen, sim.LastResult.Outcome, "some com a carga na parede");
        }

        [Test]
        public void Fantasma_PegarDeVoltaQuebraAPossessaoERepele_AcertarNaLuzDerrubaACarga_SemCargaNoChaoPersegueETocaOCourier()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 1);
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 2.0f); Steps(sim, 8, V2.Zero);
            Enemy g = r.Spawn(EnemyType.Ghost, r.Main.Pos + new V2(0f, 0.7f)); g.Hp = 99;
            for (int i = 0; i < StepsFor(1.5f) && r.Main.State != CargoState.Stolen; i++) StillR3(sim, Dt, false);
            Assert.AreEqual(CargoState.Stolen, r.Main.State);
            StillR3(sim, 0.5f, false);
            r.Courier.Pos = r.Main.Pos + new V2(-1.0f, 0f);
            Steps(sim, 1, Right * 0.3f);
            Assert.AreEqual(CargoState.Held, r.Main.State, "chegar a carga pega de volta"); Assert.IsNull(g.Carry);
            Assert.Greater(V2.Dist(g.Pos, r.Courier.Pos), 1.5f, "a possessao quebrada repele o fantasma"); Assert.Greater(g.Cd, 1f, "e o toque recarrega");
            // acertar o fantasma na luz derruba a carga
            Steps(sim, 8, V2.Zero);
            for (int i = 0; i < StepsFor(3f) && r.Main.State != CargoState.Stolen; i++) StillR3(sim, Dt, false);
            Assert.AreEqual(CargoState.Stolen, r.Main.State);
            List<SimEvent> ev = new List<SimEvent>();
            for (int i = 0; i < StepsFor(2f) && r.Main.State == CargoState.Stolen; i++) ev.AddRange(Steps(sim, 1, V2.Zero));
            Assert.AreEqual(CargoState.Ground, r.Main.State, "o tiro derruba a carga"); Assert.IsTrue(ev.Exists(e => e.Kind == Ev.CargoDrop && e.B == 2));
            // sem carga no chao: persegue o courier e toca (1, recarga 1,5 s)
            Sim s2 = SimR3(); Run r2 = StartR3(s2, 1);
            Steps(s2, 1, Up);
            Enemy g2 = r2.Spawn(EnemyType.Ghost, r2.Courier.Pos + new V2(0f, 3f)); g2.Hp = 99;
            int hp = r2.Courier.Hp;
            List<SimEvent> e2 = InchR3(s2, 3.2f, Left);
            Assert.AreEqual(2, CountR3(e2, Ev.DamageTaken), "toque a cada 1,5 s"); Assert.AreEqual(hp - 2, r2.Courier.Hp);
        }

        // ------------------------------------------------------------------ esqueleto

        [Test]
        public void Esqueleto_LongeDaCargaRemontaEm4s_PertoDelaMorreDeVez_DashEspalhaOsOssos_NoMaximo2Remontes()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 1);   // caixa na R3-1 (tocha em (8,7; 7))
            Steps(sim, 1, Up); r.Courier.Pos = new V2(7.0f, 2.5f); Steps(sim, 8, V2.Zero);   // carga em (7,6; 2,5)
            V2 far = new V2(7.8f, 7.2f);   // na luz da tocha, a 4,7 m da carga e no alcance do tiro
            Enemy s = r.Spawn(EnemyType.Skeleton, far); s.Hp = 1; s.Stun = 99f;
            float died = -1f, rose = -1f; Enemy back = null;
            for (int i = 0; i < StepsFor(7f) && rose < 0f; i++)
                foreach (SimEvent e in Steps(sim, 1, V2.Zero))
                {
                    if (e.Kind == Ev.EnemyDie && e.A == s.Id) died = r.Time;
                    if (e.Kind == Ev.EnemyRevive) { rose = r.Time; foreach (Enemy x in r.Enemies) if (x.Id == e.A) back = x; }
                    if (died >= 0f && rose < 0f && e.Kind == Ev.EnemyDie && e.A == s.Id) Assert.AreEqual(1, r.Bones.Count, "vira pilha de ossos");
                }
            Assert.Greater(died, 0f, "nao matou"); Assert.Greater(rose, 0f, "longe da carga remonta");
            Assert.AreEqual(Balance.BonesRise, rose - died, 0.1f, "em 4 s");
            Assert.IsNotNull(back); Assert.AreEqual((int)System.Math.Round(5 * Balance.HpMul(r.Tier)), back.Hp, "com a vida cheia");
            Assert.AreEqual(1, r.R3Of(back).Revives);
            // 2o remonte e a 3a morte e definitiva
            back.Hp = 1; back.Stun = 99f; back.Pos = far;
            Enemy back2 = null;
            for (int i = 0; i < StepsFor(7f) && back2 == null; i++)
                foreach (SimEvent e in Steps(sim, 1, V2.Zero)) if (e.Kind == Ev.EnemyRevive) foreach (Enemy x in r.Enemies) if (x.Id == e.A) back2 = x;
            Assert.IsNotNull(back2, "2o remonte"); Assert.AreEqual(2, r.R3Of(back2).Revives);
            back2.Hp = 1; back2.Stun = 99f; back2.Pos = far;
            List<SimEvent> ev = StillR3(sim, 2f);
            Assert.IsTrue(Has(ev, Ev.BonesScatter), "3a morte: espalha"); Assert.AreEqual(0, r.Bones.Count); Assert.IsNull(FindR3(r, EnemyType.Skeleton));
            // perto da carga (3 m) morre de vez
            Enemy near = r.Spawn(EnemyType.Skeleton, r.Main.Pos + new V2(0f, 2f)); near.Hp = 1; near.Stun = 99f;
            ev = StillR3(sim, 1.5f);
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.EnemyDie && e.A == near.Id)); Assert.IsTrue(Has(ev, Ev.BonesScatter), "perto da carga nao volta"); Assert.AreEqual(0, r.Bones.Count);
            // o dash na pilha espalha os ossos
            Enemy d = r.Spawn(EnemyType.Skeleton, far); d.Hp = 1; d.Stun = 99f;
            for (int i = 0; i < StepsFor(2f) && r.Bones.Count == 0; i++) Steps(sim, 1, V2.Zero);
            Assert.AreEqual(1, r.Bones.Count);
            Assert.AreEqual(Hint.SkeletonRise, r.Hint, "lute perto da carga");
            r.Courier.Pos = r.Bones[0].Pos + new V2(0f, -2f);
            ev = Steps(sim, 1, Up, true); ev.AddRange(Steps(sim, 6, Up));
            Assert.IsTrue(Has(ev, Ev.BonesScatter), "o dash espalha"); Assert.AreEqual(0, r.Bones.Count);
        }

        // ------------------------------------------------------------------ aranha e teias

        [Test]
        public void Aranha_CuspeComAvisoPrende1s_DashSolta_ErrandoViraTeiaDe6s_EspelhoDevolveEPrendeAAranha()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 1);   // caixa na R3-1
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 2.5f);
            Enemy sp = r.Spawn(EnemyType.Spider, new V2(4.5f, 6.5f)); sp.Hp = 99;   // 4 m, linha livre
            bool tele = false, strip = false; var ev = new List<SimEvent>();
            for (int i = 0; i < StepsFor(2f) && r.Webbed <= 0f; i++)
            {
                ev.AddRange(Steps(sim, 1, Left * 0.06f));
                foreach (var w in r.Warns) if (w.Kind == WarnKind.Strip && w.Owner == sp.Id) strip = true;
            }
            tele = CountR3(ev, Ev.Telegraph, (int)EnemyType.Spider) > 0;
            Assert.IsTrue(tele && strip, "aviso em linha antes do cuspe"); Assert.Greater(r.Webbed, 0f, "a teia prende"); Assert.IsTrue(Has(ev, Ev.Webbed));
            V2 p = r.Courier.Pos; Steps(sim, 10, Left);
            Assert.AreEqual(p.X, r.Courier.Pos.X, 1e-3f, "preso nao anda"); Assert.AreEqual(Hint.Web, r.Hint);
            Steps(sim, 1, Left, true);
            Assert.AreEqual(0f, r.Webbed, "o dash solta"); Assert.AreEqual(CargoState.Ground, r.Main.State, "e larga a carga");
            // errando: vira teia de 0,6 m por 6 s onde parou
            Sim s2 = SimR3(); Run r2 = StartR3(s2, 1);
            Steps(s2, 1, Up); r2.Courier.Pos = new V2(4.5f, 2.5f);
            Enemy sp2 = r2.Spawn(EnemyType.Spider, new V2(4.5f, 6.5f)); sp2.Hp = 99;
            for (int i = 0; i < StepsFor(1f) && r2.Warns.Count == 0; i++) Steps(s2, 1, Left * 0.06f);
            Assert.Greater(r2.Warns.Count, 0);
            Steps(s2, StepsFor(0.6f), Right);   // sai da linha
            for (int i = 0; i < StepsFor(2f) && r2.Spits.Count > 0; i++) Steps(s2, 1, Right * 0.06f);
            Assert.AreEqual(0f, r2.Webbed, "saiu da linha");
            Cobweb web = r2.Webs.Find(w => w.Left > 0f);
            Assert.IsNotNull(web, "o cuspe que erra vira teia"); Assert.AreEqual(Balance.SpitWeb, web.Radius);
            Assert.Greater(web.Left, Balance.SpitWebTime - 2f);
            StillR3(s2, Balance.SpitWebTime, false);
            Assert.IsFalse(r2.Webs.Contains(web), "some em 6 s");
            // o espelho devolve: a aranha fica presa 2 s e o espelho racha
            Sim s3 = SimR3(); Run r3 = StartR3(s3, 2);   // M3-3: espelho na R3-3 (linha x = 4,5 livre)
            Steps(s3, 1, Up); r3.Courier.Pos = new V2(4.5f, 2.5f);
            Enemy sp3 = r3.Spawn(EnemyType.Spider, new V2(4.5f, 6.5f)); sp3.Hp = 99;
            bool refl = false;
            for (int i = 0; i < StepsFor(2f) && !refl; i++) foreach (SimEvent e in Steps(s3, 1, Up * 0.06f)) if (e.Kind == Ev.Reflect && e.A == 1) refl = true;
            Assert.IsTrue(refl, "a teia de frente volta"); Assert.Greater(sp3.Stun, 1.5f, "prende a aranha 2 s"); Assert.AreEqual(1, r3.Main.Cracks); Assert.AreEqual(0f, r3.Webbed);
        }

        [Test]
        public void Teia_PrendeQuemPassaCarregando1s_DepoisImune_FreiaSemCarga_DashRasga_CargaNaTeiaDemora1sAMais()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 2);   // M3-3 na R3-3 (teia central em (4,5; 8,5), raio 1,0)
            Steps(sim, 1, Up);
            r.Courier.Pos = new V2(4.5f, 6.8f);
            var ev = new List<SimEvent>();
            for (int i = 0; i < 10 && r.Webbed <= 0f; i++) ev.AddRange(Steps(sim, 1, Up));
            Assert.Greater(r.Webbed, 0f, "carregando prende"); Assert.IsTrue(Has(ev, Ev.Webbed));
            V2 p = r.Courier.Pos; Steps(sim, StepsFor(0.9f), Up);
            Assert.AreEqual(p.Y, r.Courier.Pos.Y, 1e-3f, "preso 1 s");
            Steps(sim, StepsFor(0.7f), Up);
            Assert.Greater(r.Courier.Pos.Y, p.Y + 0.8f, "depois de 1 s atravessa (imune 1,5 s)");
            // sem carga: x0,5
            Sim s1 = SimR3(); Run r1 = StartR3(s1, 2);
            Steps(s1, 1, Up); r1.Courier.Pos = new V2(4.5f, 2.0f); Steps(s1, 8, V2.Zero);
            r1.Courier.Pos = new V2(2.2f, 5.3f); Steps(s1, 1, Up);
            float y0 = r1.Courier.Pos.Y; Steps(s1, 10, Up);
            Assert.AreEqual(0f, r1.Webbed, "sem carga nao prende");
            Assert.AreEqual(Balance.CarrySpeed * Balance.FreeSpeedMul * Balance.WebSlow * 10 * Dt, r1.Courier.Pos.Y - y0, 0.02f, "sem carga: x0,5");
            // o dash rasga a teia (ate o fim da visita)
            r1.Courier.Pos = new V2(4.5f, 6.8f);
            ev = Steps(s1, 1, Up, true); ev.AddRange(Steps(s1, 6, Up));
            Assert.IsTrue(Has(ev, Ev.WebTorn)); Assert.IsFalse(r1.Webs.Exists(w => V2.Dist(w.Pos, new V2(4.5f, 8.5f)) < 0.1f), "rasgada");
            // carga dentro da teia: pegar leva 1 s a mais
            r1.Main.Pos = new V2(6.8f, 6.0f); Steps(s1, 1, V2.Zero);
            Assert.AreEqual(Balance.WebPick, r1.Main.PickExtra, 1e-4f);
            r1.Courier.Pos = new V2(6.8f, 4.7f);
            for (int i = 0; i < StepsFor(0.9f); i++) Steps(s1, 1, (i % 2 == 0 ? Left : Right) * 0.1f);
            Assert.AreEqual(CargoState.Ground, r1.Main.State, "na teia ainda nao pegou");
            for (int i = 0; i < StepsFor(0.25f); i++) Steps(s1, 1, (i % 2 == 0 ? Left : Right) * 0.1f);
            Assert.AreEqual(CargoState.Held, r1.Main.State, "pegou com +1 s");
        }

        // ------------------------------------------------------------------ mumia

        [Test]
        public void Mumia_AgarraEPrende_UmDeDanoACada12s_DoisGolpesSoltam_DashSolta()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 1);
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 2.5f);
            Enemy m = r.Spawn(EnemyType.Mummy, new V2(4.5f, 4.2f)); m.Hp = 99;
            for (int i = 0; i < StepsFor(4f) && r.GrabbedBy == null; i++) Steps(sim, 1, Up * 0.06f);
            Assert.AreSame(m, r.GrabbedBy, "agarrou"); Assert.AreEqual(Hint.MummyGrab, r.Hint); Assert.AreEqual(R3Act.Hold, r.R3Of(m).Act);
            int hp = r.Courier.Hp; V2 p = r.Courier.Pos;
            Steps(sim, StepsFor(1.25f), Left);
            Assert.AreEqual(p.X, r.Courier.Pos.X, 1e-3f, "agarrado nao anda"); Assert.AreEqual(hp - 1, r.Courier.Hp, "1 de dano a cada 1,2 s");
            for (int i = 0; i < StepsFor(3f) && r.GrabbedBy != null; i++) Steps(sim, 1, V2.Zero);   // parado: pousa e atira
            Assert.IsNull(r.GrabbedBy, "2 golpes soltam"); Assert.AreEqual(97, m.Hp);
            // o dash solta (e larga a carga)
            PickR3(sim);
            m.Cd = 0f; r.Courier.Pos = m.Pos + new V2(0f, -1.3f);
            for (int i = 0; i < StepsFor(3f) && r.GrabbedBy == null; i++) Steps(sim, 1, Up * 0.06f);
            Assert.AreSame(m, r.GrabbedBy);
            Steps(sim, 1, Down, true);
            Assert.IsNull(r.GrabbedBy, "o dash solta"); Assert.AreEqual(CargoState.Ground, r.Main.State);
        }

        // ------------------------------------------------------------------ gargula

        [Test]
        public void Gargula_PedraIntocavel_AcordaSoComACargaA3m_MergulhaNaFaixa_EmpoleiradaVulneravel_VoltaAoPedestal()
        {
            Sim sim = SimR3(); Run r = StartR3(sim, 1);
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 2.0f); Steps(sim, 8, V2.Zero);   // carga no chao
            Enemy g = r.Spawn(EnemyType.Gargoyle, new V2(4.5f, 7.0f)); V2 home = g.Pos; int ghp = g.Hp;
            r.Courier.Pos = new V2(4.5f, 5.6f);   // a 1,4 m, sem a carga
            for (int i = 0; i < StepsFor(1f); i++) Steps(sim, 1, (i % 20 < 10 ? Left : Right) * 0.2f);
            Assert.AreEqual(R3Act.Statue, r.R3Of(g).Act, "sem a carga nas maos ela dorme");
            Assert.IsTrue(r.Untouchable(g)); Assert.IsFalse(r.CanHit(g), "pedra nao e alvo");
            StillR3(sim, 1f);
            Assert.AreEqual(ghp, g.Hp, "pedra nao leva dano");
            // com a carga a 3 m: acorda (0,9 s), mergulha na faixa e tira 2
            PickR3(sim);
            r.Courier.Pos = new V2(4.5f, 4.3f);
            Steps(sim, 1, Left * 0.06f);
            Assert.AreEqual(R3Act.Wake, r.R3Of(g).Act, "acorda"); Assert.IsTrue(r.Warns.Exists(w => w.Kind == WarnKind.Strip && w.Owner == g.Id), "faixa no chao");
            int hp = r.Courier.Hp; List<SimEvent> ev = InchR3(sim, 1.6f, Left);
            Assert.IsTrue(Has(ev, Ev.Strike)); Assert.AreEqual(hp - 2, r.Courier.Hp, "o mergulho tira 2");
            Assert.AreEqual(R3Act.Perch, r.R3Of(g).Act); Assert.IsFalse(r.Untouchable(g), "empoleirada: vulneravel");
            InchR3(sim, Balance.GargPerch, Left);
            Assert.AreEqual(R3Act.Fly, r.R3Of(g).Act); Assert.IsTrue(r.Untouchable(g), "voltando: intocavel");
            InchR3(sim, Balance.GargBack + 0.1f, Left);
            Assert.AreEqual(R3Act.Statue, r.R3Of(g).Act); Assert.AreEqual(home.X, g.Pos.X, 1e-3f); Assert.AreEqual(home.Y, g.Pos.Y, 1e-3f);
        }

        // ------------------------------------------------------------------ Viuva Aranha (M3)

        static Sim WidowR3(out Enemy w)
        {
            Sim sim = InRoomWith(CR3(7), 5, RoomR3(Regiao3.M), CargoType.Box); Run r = sim.Run;   // M3-8 no Covil da Viuva
            Assert.AreEqual("M3", r.Room.Code);
            w = FindR3(r, EnemyType.SpiderWidow);
            Enemy keep = w; r.Enemies.RemoveAll(e => e != keep);
            r.Courier.Hp = 99;
            return sim;
        }

        [Test]
        public void Viuva_FioPuxaACargaDoChao_PegarCorta_ChegandoViraCasulo_LequeDe3Cuspes()
        {
            Sim sim = WidowR3(out Enemy w); Run r = sim.Run;
            Steps(sim, 1, Up); r.Courier.Pos = new V2(5.5f, 5.5f); Steps(sim, 8, V2.Zero);   // carga a ~6 m dela
            float d0 = V2.Dist(r.Main.Pos, w.Pos); bool thread = false;
            for (int i = 0; i < StepsFor(1.2f) && !thread; i++) { StillR3(sim, Dt, false); thread = r.Warns.Exists(x => x.Kind == WarnKind.Thread); }
            Assert.IsTrue(thread, "aviso do fio");
            StillR3(sim, 1.5f, false);
            Assert.AreEqual(R3Act.Pull, r.R3Of(w).Act); Assert.Less(V2.Dist(r.Main.Pos, w.Pos), d0 - 1f, "puxa a carga");
            r.Courier.Pos = r.Main.Pos + new V2(-1.0f, 0f); Steps(sim, 1, Right * 0.3f);
            Assert.AreEqual(CargoState.Held, r.Main.State, "pegar corta o fio"); Assert.AreNotEqual(R3Act.Pull, r.R3Of(w).Act);
            // ate ela: casulo (pegar leva 1,5 s)
            Sim s2 = WidowR3(out Enemy w2); Run r2 = s2.Run;
            Steps(s2, 1, Up); r2.Courier.Pos = new V2(5.5f, 5.5f); Steps(s2, 8, V2.Zero);
            r2.Courier.Pos = new V2(8.2f, 2.0f);   // longe (o fio corre sem o courier)
            for (int i = 0; i < StepsFor(6f) && r2.Main.PickExtra < Balance.WidowCocoon; i++) StillR3(s2, Dt, false);
            Assert.AreEqual(Balance.WidowCocoon, r2.Main.PickExtra, 1e-4f, "casulo");
            Assert.Less(V2.Dist(r2.Main.Pos, w2.Pos), w2.Def.Radius + 0.6f, "a carga chegou nela");
            // leque: 3 cuspes a cada 6 s (o 1o em 2 s)
            Sim s3 = WidowR3(out Enemy w3); Run r3 = s3.Run;
            Steps(s3, 1, Up); r3.Courier.Pos = new V2(4.5f, 3.0f);
            int strips = 0, spits = 0;
            for (int i = 0; i < StepsFor(3f) && spits < 3; i++)
            {
                Steps(s3, 1, (i % 20 < 10 ? Left : Right) * 0.06f);
                strips = System.Math.Max(strips, r3.Warns.FindAll(x => x.Kind == WarnKind.Strip && x.Owner == w3.Id).Count);
                spits = System.Math.Max(spits, r3.Spits.FindAll(x => x.Owner == w3.Id).Count);
            }
            Assert.AreEqual(3, strips, "aviso do leque: 3 faixas"); Assert.AreEqual(3, spits, "3 cuspes");
        }

        [Test]
        public void Viuva_Fase2_ChamaAranhas_SobeAoTetoIntocavel_DespencaNoCirculo()
        {
            Sim sim = WidowR3(out Enemy w); Run r = sim.Run;
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 3.0f);
            w.Hp = w.Hp / 2;
            List<SimEvent> ev = InchR3(sim, 0.2f, Left);
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.BossPhase && e.A == w.Id && e.B == 2), "fase 2 a 50%");
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.Summon && e.A == w.Id && e.B == Balance.WidowCalls), "chama 2 aranhas");
            r.Enemies.RemoveAll(e => e != w);
            for (int i = 0; i < StepsFor(2f) && r.R3Of(w).Act != R3Act.Ceiling; i++) InchR3(sim, Dt, Left);
            Assert.AreEqual(R3Act.Ceiling, r.R3Of(w).Act); Assert.IsTrue(r.Untouchable(w), "no teto: intocavel");
            bool circle = false; ev = new List<SimEvent>();
            for (int i = 0; i < StepsFor(Balance.WidowCeil + Balance.WidowDropTele + 0.3f) && r.R3Of(w).Act != R3Act.None; i++)
            {
                ev.AddRange(InchR3(sim, Dt, i % 20 < 10 ? Left : Right));
                if (r.Warns.Exists(x => x.Kind == WarnKind.Circle && x.Owner == w.Id)) circle = true;
            }
            Assert.IsTrue(circle, "circulo de 1,3 m no chao antes de despencar");
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.DamageTaken && e.A == Balance.WidowDropDamage), "despencou em cima: 2");
            Assert.IsFalse(r.Untouchable(w));
        }

        // ------------------------------------------------------------------ Necromante (B3)

        static Sim NecroR3(out Enemy n)
        {
            Sim sim = InRoomWith(CR3(15), 4, RoomR3(Regiao3.B), CargoType.Lantern); Run r = sim.Run;   // M3-16 no Altar
            Assert.AreEqual("B3", r.Room.Code);
            n = FindR3(r, EnemyType.Necromancer); r.Courier.Hp = 99;
            return sim;
        }

        [Test]
        public void Necromante_RaioComAvisoNaFaixa_LapideProtege_EspelhoDevolve()
        {
            Sim sim = NecroR3(out Enemy n); Run r = sim.Run;
            Steps(sim, 1, Up);
            n.Pos = new V2(3.0f, 8.5f); r.Courier.Pos = new V2(3.0f, 3.0f);   // linha livre (lapides em x 1,3-2,3)
            bool strip = false; var ev = Steps(sim, 8, V2.Zero);
            for (int i = 0; i < StepsFor(2.5f) && !ev.Exists(e => e.Kind == Ev.DamageTaken); i++)
            {
                ev.AddRange(StillR3(sim, Dt, false));
                if (r.Warns.Exists(x => x.Kind == WarnKind.Strip && x.Owner == n.Id)) strip = true;
            }
            Assert.Greater(CountR3(ev, Ev.Telegraph, (int)EnemyType.Necromancer), 0); Assert.IsTrue(strip, "faixa verde no chao");
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.DamageTaken && e.A == 2), "o raio tira 2");
            Assert.IsTrue(r.Warns.Exists(x => x.Kind == WarnKind.Beam), "raio visivel 0,3 s");
            // atras da lapide o raio para
            n.Cd = 0f; n.Pos = new V2(1.8f, 7.6f); r.Courier.Pos = new V2(1.8f, 3.6f);
            ev = StillR3(sim, 2.5f, false);
            Assert.Greater(CountR3(ev, Ev.Strike, (int)EnemyType.Necromancer), 0, "disparou");
            Assert.IsFalse(Has(ev, Ev.DamageTaken), "a lapide protege");
            // o espelho devolve: 6 de dano e atordoa 1 s
            Sim sm = SimR3(); Run rm = StartR3(sm, 2);   // M3-3: espelho na R3-3 (linha x = 4,5 livre)
            Steps(sm, 1, Up); rm.Courier.Pos = new V2(4.5f, 3.0f);
            Enemy nm = rm.Spawn(EnemyType.Necromancer, new V2(4.5f, 9.5f)); int hp0 = nm.Hp;
            bool refl = false;
            for (int i = 0; i < StepsFor(2.5f) && !refl; i++) foreach (SimEvent e in Steps(sm, 1, Up * 0.06f)) if (e.Kind == Ev.Reflect && e.A == 2) refl = true;
            Assert.IsTrue(refl, "o raio de frente volta"); Assert.AreEqual(hp0 - Balance.NecroReflectDamage, nm.Hp); Assert.Greater(nm.Stun, 0.5f, "atordoado");
            Assert.AreEqual(1, rm.Main.Cracks); Assert.AreEqual(99, rm.Courier.Hp);
        }

        [Test]
        public void Necromante_ErgueEsqueletosComRuna_NoMaximo4_SaltaComOCourierPerto_Fase3Escurece_MortoDerrubaTudo()
        {
            Sim sim = NecroR3(out Enemy n); Run r = sim.Run;
            Steps(sim, 1, Up); r.Courier.Pos = new V2(4.5f, 3.0f);   // atras do altar: o raio para nele
            bool runes = false; var ev = new List<SimEvent>();
            for (int i = 0; i < StepsFor(Balance.NecroFirstRaise + Balance.NecroRune + 0.2f); i++)
            {
                ev.AddRange(StillR3(sim, Dt, false));
                if (r.Warns.FindAll(x => x.Kind == WarnKind.Rune).Count == Balance.NecroRaise) runes = true;
            }
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.Raise && e.B == Balance.NecroRaise), "ergue 3"); Assert.IsTrue(runes, "runa 1 s antes");
            int sk = 0; foreach (Enemy e in r.Enemies) if (e.Alive && e.Type == EnemyType.Skeleton) { sk++; Assert.AreEqual(EnemyDef.All[(int)EnemyType.Skeleton].Hp, e.Hp, "erguido: vida sem nivel"); }
            Assert.AreEqual(Balance.NecroRaise, sk);
            ev = StillR3(sim, Balance.NecroRaiseEvery + 0.5f, false);
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.Raise && e.B == 1), "no maximo 4 vivos: ergue so 1");
            // salta com o courier a 3 m: fumaca 0,5 s e o ponto de salto mais longe
            Sim s2 = NecroR3(out Enemy n2); Run r2 = s2.Run;
            Steps(s2, 1, Up); n2.Pos = new V2(3.0f, 5.5f); r2.Courier.Pos = new V2(3.0f, 3.0f);
            ev = StillR3(s2, 0.2f, false);
            Assert.AreEqual(R3Act.Smoke, r2.R3Of(n2).Act, "fumaca");
            ev.AddRange(StillR3(s2, 0.5f, false));
            Assert.IsTrue(Has(ev, Ev.Blink)); Assert.AreEqual(6.0f, n2.Pos.X, 1e-3f); Assert.AreEqual(11.6f, n2.Pos.Y, 1e-3f, "o ponto mais longe do courier");
            // fase 3 (20%): luz do courier -1 m e tochas -0,5 m
            Sim s3 = NecroR3(out Enemy n3); Run r3 = s3.Run;
            Steps(s3, 1, Up); r3.Courier.Pos = new V2(4.5f, 3.0f);
            int full = n3.Hp; n3.Hp = full / 5;
            ev = StillR3(s3, 0.1f, false);
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.BossPhase && e.B == 3)); Assert.AreEqual(Balance.NecroLightCut, r3.LightCut); Assert.AreEqual(Balance.NecroTorchCut, r3.TorchCut);
            Assert.IsFalse(r3.InLight(r3.Courier.Pos + new V2(0f, 5f)), "a luz encolhe (lanterna nas maos: 5,5 - 1 m)");
            // morto: todos os esqueletos caem de vez e a luz volta
            Enemy a = r3.Spawn(EnemyType.Skeleton, new V2(1.0f, 3.0f)), b = r3.Spawn(EnemyType.Skeleton, new V2(8.0f, 3.0f));
            a.Stun = b.Stun = 99f;
            n3.Hp = 1; n3.Stun = 99f; n3.Pos = r3.Courier.Pos + new V2(0f, 2.0f);
            ev = StillR3(s3, 2f);
            Assert.IsTrue(ev.Exists(e => e.Kind == Ev.EnemyDie && e.A == n3.Id), "nao matou o Necromante");
            Assert.IsFalse(a.Alive || b.Alive, "os esqueletos caem com ele"); Assert.AreEqual(0, r3.Bones.Count);
            Assert.AreEqual(0f, r3.LightCut); Assert.AreEqual(0f, r3.TorchCut);
        }

        // ------------------------------------------------------------------ campanha da regiao 3 pelo bot (BALANCE_REGIAO3.md)

        const int CampsR3 = 10, SeedsR3 = 10;
        static Bot.Campaign[] campsR3; static Bot.Stats[] tableR3;
        static Bot.Campaign[] CampaignsR3() => campsR3 ?? (campsR3 = Bot.PlayRegion3(CampsR3));
        static Bot.Stats[] TableR3() => tableR3 ?? (tableR3 = Bot.MeasureRegion3Table(CampaignsR3(), SeedsR3));

        /// <summary>Alvos (CONTRATO_LANCAMENTO + GDD §28): a campanha da regiao 3 fecha (do fim estimado da regiao 2); do 4o contrato em diante
        /// 1,5-3 min por contrato (a mensagem e corrida por desenho); H1 entre 30% e 70%; nenhum contrato inviavel; o bot nunca fica preso.</summary>
        [Test]
        public void Campanha_Regiao3_PeloBot_Fecha_DuracaoDoQuartoEmDiante_H1_SemContratoInviavel()
        {
            RegionDef reg = RegionDef.Of(3);
            int fin = 0;
            foreach (Bot.Campaign cp in CampaignsR3()) { if (cp.Finished) fin++; Assert.GreaterOrEqual(cp.Coins, 0); }
            Assert.GreaterOrEqual(fin * 10, CampsR3 * 9, "a campanha da regiao 3 tem de fechar");
            double sum = 0; int n = 0;
            for (int k = 3; k < reg.Contracts.Length; k++)
            {
                if (reg.Contracts[k].Cargo == CargoType.Message) continue;
                double t = 0; int m = 0;
                foreach (Bot.Campaign cp in CampaignsR3()) if (cp.TimePer[reg.First + k] > 0f) { t += cp.TimePer[reg.First + k]; m++; }
                Assert.Greater(m, 0, reg.Contracts[k].Code);
                t /= m; sum += t; n++;
                Assert.That(t, Is.InRange(75.0, 200.0), reg.Contracts[k].Code + ": duracao da entrega na campanha");
            }
            Assert.That(sum / n, Is.InRange(90.0, 180.0), "do 4o contrato em diante: 1,5-3 min");
            int fought = 0, normal = 0;
            foreach (Bot.Stats s in TableR3())
            {
                string k = Sim.Contracts[s.Contract].Code;
                fought += s.RoomsFought; normal += s.RoomsNormal;
                Assert.AreEqual(CampsR3 * SeedsR3, s.Runs, k); Assert.AreEqual(0, s.Timeout, k + ": bot preso");
                Assert.GreaterOrEqual(s.Pct(s.Delivered), 40.0, k + ": quase ninguem entrega"); Assert.Less(s.Pct(s.Died), 40.0, k + ": morte demais");
            }
            Assert.That(100.0 * fought / normal, Is.InRange(30.0, 70.0), "H1: luta escolhida em 30-70% das salas");
        }

        /// <summary>O icone de caveiras das salas da regiao 3 conta a verdade (a mesma regra da regiao 1): o dano medio por visita sobe com o icone
        /// (folga de 0,3) e nenhuma sala passa da media do icone vizinho.</summary>
        [Test]
        public void IconeDeAmeaca_Regiao3_AcompanhaODanoMedido()
        {
            var dmg = new double[Content.Rooms.Length]; var vis = new int[Content.Rooms.Length];
            foreach (Bot.Stats s in TableR3()) for (int r = 0; r < dmg.Length; r++) { dmg[r] += s.RoomDamage[r]; vis[r] += s.RoomVisits[r]; }
            var mean = new double[4]; var count = new int[4];
            foreach (int id in RegionDef.Of(3).Normal) { Assert.GreaterOrEqual(vis[id], 60, Content.Rooms[id].Code + ": poucas visitas"); mean[Content.Rooms[id].Threat] += dmg[id] / vis[id]; count[Content.Rooms[id].Threat]++; }
            for (int t = 1; t <= 3; t++) { Assert.Greater(count[t], 0, "icone " + t + " sem sala"); mean[t] /= count[t]; }
            for (int t = 1; t < 3; t++) Assert.Greater(mean[t + 1] - mean[t], 0.3, "icones " + t + " e " + (t + 1) + " sem folga de dano");
            foreach (int id in RegionDef.Of(3).Normal)
            {
                RoomDef a = Content.Rooms[id]; double d = dmg[id] / vis[id];
                if (a.Threat < 3) Assert.Less(d, mean[a.Threat + 1], a.Code + " (icone " + a.Threat + ") tira mais vida que a media do icone " + (a.Threat + 1));
                if (a.Threat > 1) Assert.Greater(d, mean[a.Threat - 1], a.Code + " (icone " + a.Threat + ") tira menos vida que a media do icone " + (a.Threat - 1));
            }
        }

        /// <summary>Imprime as tabelas do BALANCE_REGIAO3.md (dotnet test ... --filter Name~Tabela_Regiao3 --logger "console;verbosity=detailed").</summary>
        [Test]
        public void Tabela_Regiao3_ParaBalanceMd()
        {
            var ci = CultureInfo.InvariantCulture;
            RegionDef reg = RegionDef.Of(3);
            Bot.Campaign[] camps = CampaignsR3(); Bot.Stats[] t = TableR3();
            var sb = new StringBuilder();
            sb.AppendLine("| Contrato | Carga | Salas | % concluído | % morte | % falha de carga | duração média (s) | mediana (s) | % salas com luta | moedas médias | estrelas | 3 estrelas % | rachaduras |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            int fought = 0, normal = 0;
            foreach (Bot.Stats s in t)
            {
                ContractDef d = Sim.Contracts[s.Contract];
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
            var per = new double[reg.Contracts.Length]; var tper = new double[reg.Contracts.Length]; var nper = new int[reg.Contracts.Length];
            foreach (Bot.Campaign cp in camps)
            {
                if (cp.Finished) fin++;
                att += cp.Attempts; deaths += cp.Deaths; earned += cp.Earned; spent += cp.Spent; stars += cp.Stars - Bot.Region3StartStars; time += cp.Time; coins += cp.Coins;
                for (int i = 0; i < per.Length; i++) { per[i] += cp.AttemptsPer[reg.First + i]; if (cp.TimePer[reg.First + i] > 0f) { tper[i] += cp.TimePer[reg.First + i]; nper[i]++; } }
            }
            double n = camps.Length;
            sb.AppendLine(string.Format(ci, "Campanha da regiao 3 ({0} sementes, do fim estimado da regiao 2): fecha {1:0}% · tentativas {2:0.0} · mortes {3:0.0} · tempo de jogo do bot {4:0.0} min · moedas ganhas {5:0} · gastas {6:0} · paradas no fim {7:0} · estrelas {8:0.0}/48",
                n, 100.0 * fin / n, att / n, deaths / n, time / n / 60f, earned / n, spent / n, coins / n, stars / n));
            var line = new StringBuilder("Por contrato (tentativas · tempo da entrega s):");
            for (int i = 0; i < per.Length; i++) line.Append(string.Format(ci, " {0}={1:0.00}·{2:0}", reg.Contracts[i].Code, per[i] / n, nper[i] > 0 ? tper[i] / nper[i] : 0));
            sb.AppendLine(line.ToString());
            TestContext.WriteLine(sb.ToString());
            Assert.AreEqual(16, t.Length);
        }
    }
}