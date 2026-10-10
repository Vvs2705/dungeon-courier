using System.Collections.Generic;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Regras da v1.0-beta (CONTRATO_V1 §2-§9): cargas novas, inimigos novos, armas, perks, reforco com teto, sala secreta,
    /// icone de risco, estrelas, contrato do dia e por codigo, save v2. So API publica (no Unity os testes sao outro assembly).</summary>
    public partial class CoreTests
    {
        // ------------------------------------------------------------------ helpers v1

        static int IndexOf(CargoType t) { for (int i = 0; i < Sim.Contracts.Length; i++) if (Sim.Contracts[i].Cargo == t) return i; return -1; }

        /// <summary>Avanca n passos tirando todos os inimigos antes de cada um (isola a regra da carga).</summary>
        static List<SimEvent> Quiet(Sim sim, int n, V2 move, bool dashFirst = false)
        {
            var evs = new List<SimEvent>();
            for (int i = 0; i < n; i++) { sim.Run.Enemies.Clear(); sim.Tick(Dt, move, dashFirst && i == 0); evs.AddRange(sim.Events); }
            return evs;
        }

        /// <summary>Avanca n passos tirando so os inimigos que nasceram de reforco.</summary>
        static List<SimEvent> NoWave(Sim sim, int n, V2 move)
        {
            var evs = new List<SimEvent>();
            for (int i = 0; i < n; i++) { sim.Run.Enemies.RemoveAll(e => e.Wave); sim.Tick(Dt, move, false); evs.AddRange(sim.Events); }
            return evs;
        }

        static int CountEv(List<SimEvent> evs, Ev k) { int n = 0; foreach (SimEvent e in evs) if (e.Kind == k) n++; return n; }

        /// <summary>Sim com todas as estrelas e a arma w no nivel lv (pelo save, como um jogador que chegou la).</summary>
        static Sim ArmedSim(Weapon w, int lv = 1) =>
            Sim.Load("v=2\nseed=7\nrep=24\nstars=" + new string('3', Sim.Contracts.Length) + "\nwpn=" + (int)w + "\nwlv=" + lv + "," + lv + "," + lv + "\n");

        /// <summary>Contrato em curso na sala `room` da camada `layer`, pelo save (retoma na entrada da sala com a carga nas maos).</summary>
        static Sim InRoom(int contract, int layer, int room, CargoType cargo)
        {
            Sim s = Sim.Load("v=2\nseed=7\nrep=24\nrun=" + contract + "," + layer + "," + room + ",20,0,99,0,0,0,0,0,0,0,0\nrk=0,0,0,0,0,-1,-1,-1\ncg0=1," + (int)cargo + ",0,0,0\n");
            Assert.IsNotNull(s.Run, "save invalido para a sala " + room);
            return s;
        }

        /// <summary>Atravessa portas (sem inimigos) ate abrir a escolha de perk.</summary>
        static Run ToOffer(Sim sim, int contract)
        {
            Run r = Start(sim, contract);
            for (int guard = 0; guard < 10 && r.PerkOffer == null && sim.InContract; guard++)
            {
                int layer = r.Layer;
                r.Courier.Pos = new V2(r.Doors[0].Pos.X, 13.4f);
                for (int i = 0; i < 150 && r.Layer == layer; i++) { r.Enemies.Clear(); Steps(sim, 1, Up); }
            }
            Assert.IsNotNull(r.PerkOffer);
            return r;
        }

        // ------------------------------------------------------------------ cargas novas

        [Test]
        public void Bolo_ToqueRapidoNaoDaDash_SoBalanca_RachaAoCairSemQuebrar()
        {
            var sim = NewSim(); Run r = Start(sim, IndexOf(CargoType.Cake));
            Quiet(sim, 3, Up);
            List<SimEvent> evs = Quiet(sim, 1, Up, true);
            Assert.AreEqual(0, r.Courier.DashCount, "deu dash com o bolo nas maos");
            Assert.AreEqual(CargoState.Held, r.Main.State);
            Assert.IsTrue(Has(evs, Ev.CakeWobble));
            Assert.Greater(r.Main.Wobble, 0f);
            Assert.AreEqual(Hint.CakeNoDash, r.Hint);
            Quiet(sim, 8, V2.Zero);   // pousa
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            Quiet(sim, 1, Up, true);
            Assert.AreEqual(1, r.Courier.DashCount, "com o bolo no chao o dash vale");
            Quiet(sim, 8, V2.Zero);
            r.Courier.Pos = r.Main.Pos + new V2(0.5f, 0f); Quiet(sim, 1, Up);   // pega
            Assert.AreEqual(CargoState.Held, r.Main.State);
            r.Courier.Hp = 99;
            r.Spawn(EnemyType.Bat, r.Courier.Pos); Steps(sim, 1, Left); r.Enemies.Clear();
            Assert.AreEqual(0, r.Main.Cracks, "golpe no courier nao racha o bolo (o verbo dele e o dash)");
            Quiet(sim, 8, V2.Zero);   // pousa; o ladrao pega e, atingido, derruba: o bolo racha ao cair
            Enemy t = r.Spawn(EnemyType.Thief, r.Main.Pos + new V2(0.2f, 0f));
            r.Courier.Pos = new V2(1f, 12f);
            for (int k = 0; k < StepsFor(1f) && t.Carry == null; k++) Steps(sim, 1, k % 20 < 10 ? Left : Right);
            Assert.AreSame(r.Main, t.Carry);
            r.Courier.Pos = t.Pos + new V2(0f, 3f);
            for (int k = 0; k < StepsFor(1.5f) && r.Main.State == CargoState.Stolen; k++) Steps(sim, 1, V2.Zero);
            Assert.AreEqual(1, r.Main.Cracks, "o bolo racha ao cair da mao do ladrao");
            r.Main.Cracks = 5;
            ContractResult res = DeliverFrom(sim);
            Assert.AreEqual(Outcome.Delivered, res.Outcome, "o bolo nao quebra: rachadura so reduz o bonus");
            Assert.AreEqual(1.0f, res.IntegrityMul);
        }

        [Test]
        public void Mensagem_CronometroAvisaEEsgota_FalhaSemCargaPendente()
        {
            int i = IndexOf(CargoType.Message);
            var sim = NewSim(); Run r = Start(sim, i);
            Assert.IsTrue(r.HasTimer);
            Assert.AreEqual(Sim.Contracts[i].TimeLimit, r.TimeLeft, 0.01f);
            r.Time = Sim.Contracts[i].TimeLimit - Balance.MessageWarn - 0.05f;
            Assert.IsTrue(Has(Quiet(sim, 3, Up), Ev.TimeWarning));
            r.Time = Sim.Contracts[i].TimeLimit - 0.05f;
            Quiet(sim, 3, Up);
            Assert.IsFalse(sim.InContract);
            Assert.AreEqual(Outcome.TimeUp, sim.LastResult.Outcome);
            Assert.AreEqual(0, sim.LastResult.Payment);
            Assert.IsNull(sim.Pending, "tempo esgotado nao vira carga perdida");
            var box = NewSim(); Run rb = Start(box, 0);
            Assert.IsFalse(rb.HasTimer);
            rb.Time = 999f; Quiet(box, 3, Up);
            Assert.IsTrue(box.InContract, "so a mensagem tem cronometro");
        }

        [Test]
        public void Mensagem_AbrePortaTrancadaPorGuarda_MasNaoADoChefe()
        {
            int i = IndexOf(CargoType.Message);
            Assert.AreEqual(Content.R1, Sim.Contracts[i].Layers[0][0]);
            var sim = NewSim(); Run r = Start(sim, i, clear: false);   // R1: Guarda vivo
            Assert.IsTrue(Has(sim.Events, Ev.MessageDoor));
            Steps(sim, 1, Up);
            Assert.IsTrue(r.Enemies.Exists(e => e.Type == EnemyType.Guard && e.Alive));
            Assert.IsFalse(r.Doors[0].Locked);
            Assert.IsTrue(r.Doors[0].Open);
            r.Courier.Pos = new V2(r.Doors[0].Pos.X, 13.4f);
            Steps(sim, 5, Up);
            Assert.AreEqual(1, r.Layer, "a mensagem nao abriu a porta do Guarda");

            var box = NewSim(); Run rb = Start(box, 0, clear: false);
            Steps(box, 1, Up);
            Assert.IsTrue(rb.Doors[0].Locked, "com a caixa o Guarda tranca");

            var cap = NewSim(); Run rc = Start(cap, i);
            rc.Spawn(EnemyType.Captain, new V2(4.5f, 11.5f));
            Steps(cap, 1, Up);
            Assert.IsTrue(rc.Doors[0].Locked, "a mensagem nao abre a porta do chefe");
        }

        [Test]
        public void CaixaPesada_25PorCentoMaisLenta_PousoEPegaMais02s()
        {
            var sim = NewSim(); Run r = Start(sim, IndexOf(CargoType.Heavy));
            V2 a = r.Courier.Pos; Quiet(sim, 10, Up);
            Assert.AreEqual(Balance.CarrySpeed * Balance.HeavySpeedMul * 10 * Dt, V2.Dist(a, r.Courier.Pos), 0.01f);
            Quiet(sim, 7, V2.Zero);   // 0,23 s: a caixa comum ja teria pousado
            Assert.AreEqual(CargoState.Held, r.Main.State, "pousou antes de 0,4 s");
            Quiet(sim, 6, V2.Zero);   // 0,43 s
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            r.Courier.Pos = r.Main.Pos + new V2(1.0f, 0f);
            Quiet(sim, 3, Up);   // 0,1 s andando perto
            Assert.AreEqual(CargoState.Ground, r.Main.State, "pegou a pesada sem os 0,2 s");
            Quiet(sim, 4, Up);
            Assert.AreEqual(CargoState.Held, r.Main.State, "nao pegou a pesada andando perto por 0,2 s");
        }

        [Test]
        public void Barril_NoChao3sExplode_FereEmArea_FalhaSemCargaPendente()
        {
            var sim = NewSim(); Run r = Start(sim, IndexOf(CargoType.Barrel));
            r.Courier.Hp = 99;
            Quiet(sim, 3, Up);
            List<SimEvent> evs = Quiet(sim, 7, V2.Zero);
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            Assert.IsTrue(Has(evs, Ev.FuseLit));
            Assert.AreEqual(Hint.BarrelFuse, r.Hint);
            Enemy g = r.Spawn(EnemyType.Guard, r.Main.Pos + new V2(0f, 2.0f));
            NoWave(sim, StepsFor(2.7f), V2.Zero);
            Assert.IsTrue(sim.InContract, "explodiu antes de 3 s");
            int hp = r.Courier.Hp, ghp = g.Hp;
            evs = NoWave(sim, StepsFor(0.3f), V2.Zero);
            Assert.IsTrue(Has(evs, Ev.Explosion));
            Assert.AreEqual(Outcome.Exploded, sim.LastResult.Outcome);
            Assert.AreEqual(hp - Balance.BarrelDamage, r.Courier.Hp, "a explosao fere o courier a 2,5 m");
            Assert.LessOrEqual(g.Hp, ghp - Balance.BarrelEnemyDamage, "a explosao fere os inimigos a 2,5 m");
            Assert.IsNull(sim.Pending, "barril explodido nao vira carga perdida");

            var s2 = NewSim(); Run r2 = Start(s2, IndexOf(CargoType.Barrel));   // pegar de volta zera o pavio
            Quiet(s2, 3, Up); Quiet(s2, 7, V2.Zero);
            Quiet(s2, StepsFor(2.0f), V2.Zero);
            r2.Courier.Pos = r2.Main.Pos + new V2(0.5f, 0f); Quiet(s2, 1, Up);
            Assert.AreEqual(CargoState.Held, r2.Main.State);
            Assert.AreEqual(0f, r2.Main.Fuse);
            Quiet(s2, 7, V2.Zero); Quiet(s2, StepsFor(2.0f), V2.Zero);
            Assert.IsTrue(s2.InContract, "pegar o barril nao zerou o pavio");
        }

        // ------------------------------------------------------------------ inimigos novos

        [Test]
        public void Arqueiro_Avisa08sAntesDeAtirar_FlechaParaNoPilar_EAcertaACargaNoChao()
        {
            const int i = 7;   // C8 comeca na R7 (pilares), com a reliquia
            Assert.AreEqual(Content.R7, Sim.Contracts[i].Layers[0][0]);
            var sim = NewSim(); Run r = Start(sim, i);
            r.Courier.Hp = 99; r.Courier.Pos = new V2(4.5f, 3f);
            Enemy a = r.Spawn(EnemyType.Archer, new V2(4.5f, 9.5f));
            float tele = -1f, shot = -1f;
            for (int k = 0; k < StepsFor(3f) && shot < 0f; k++)
                foreach (SimEvent e in Steps(sim, 1, V2.Zero))
                {
                    if (e.Kind == Ev.Telegraph && e.A == a.Id && tele < 0f) tele = r.Time;
                    if (e.Kind == Ev.ArrowShot) shot = r.Time;
                }
            Assert.Greater(tele, 0f, "o arqueiro nao mirou");
            Assert.AreEqual(EnemyDef.All[(int)EnemyType.Archer].Tele, shot - tele, 0.05f, "o aviso dura 0,8 s");
            Assert.Less(a.Aim.Y, -0.99f, "mira no courier");

            r.Enemies.Clear(); r.Arrows.Clear();
            int hp = r.Courier.Hp;
            r.Courier.Pos = new V2(2.5f, 3.2f);   // atras do pilar (2,5; 5,0)
            r.Arrows.Add(new Arrow { Pos = new V2(2.5f, 7.2f), Dir = new V2(0f, -1f) });
            List<SimEvent> evs = Steps(sim, StepsFor(1.0f), V2.Zero);
            Assert.AreEqual(hp, r.Courier.Hp, "a flecha atravessou o pilar");
            Assert.IsTrue(evs.Exists(e => e.Kind == Ev.ArrowHit && e.A == 2));
            r.Courier.Pos = new V2(4.5f, 3.2f);   // corredor livre
            r.Arrows.Add(new Arrow { Pos = new V2(4.5f, 7.2f), Dir = new V2(0f, -1f) });
            Steps(sim, StepsFor(1.0f), V2.Zero);
            Assert.Less(r.Courier.Hp, hp, "sem pilar a flecha devia acertar");

            var s2 = NewSim(); Run r2 = Start(s2, i);   // carga no chao
            Steps(s2, 1, Up); Steps(s2, 7, V2.Zero);
            Assert.AreEqual(CargoState.Ground, r2.Main.State);
            int cracks = r2.Main.Cracks;
            r2.Arrows.Add(new Arrow { Pos = r2.Main.Pos + new V2(0f, 3f), Dir = new V2(0f, -1f) });
            evs = Steps(s2, StepsFor(0.6f), V2.Zero);
            Assert.IsTrue(evs.Exists(e => e.Kind == Ev.ArrowHit && e.A == 1));
            Assert.AreEqual(cracks + 1, r2.Main.Cracks, "a flecha acerta a carga no chao");
        }

        [Test]
        public void Arqueiro_RecuaQuandoOCourierChegaPerto()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            r.Courier.Pos = new V2(4.5f, 4f);
            Enemy a = r.Spawn(EnemyType.Archer, new V2(4.5f, 6f));
            Steps(sim, StepsFor(1f), V2.Zero);
            Assert.Greater(V2.Dist(a.Pos, r.Courier.Pos), 2.5f, "o arqueiro nao recuou");
        }

        [Test]
        public void Limo_DeixaPocaDe4sQueAtrasaOCourier40PorCento()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            r.Courier.Hp = 99;
            Steps(sim, 1, Up);
            Enemy s = r.Spawn(EnemyType.Slime, r.Courier.Pos + new V2(0f, 3f)); s.Hp = 1;
            List<SimEvent> evs = Steps(sim, StepsFor(1.0f), V2.Zero);   // pousa e atira
            Assert.IsTrue(Has(evs, Ev.PuddleSpawn), "o limo morto nao deixou poca");
            Assert.AreEqual(1, r.Puddles.Count);
            Puddle p = r.Puddles[0];
            r.Courier.Pos = p.Pos; Steps(sim, 1, V2.Zero);
            Assert.IsTrue(r.Courier.Slowed);
            V2 a = r.Courier.Pos; Steps(sim, 3, Right);
            Assert.AreEqual(Balance.CarrySpeed * Balance.FreeSpeedMul * (1f - Balance.PuddleSlow) * 3 * Dt, V2.Dist(a, r.Courier.Pos), 0.01f);
            Steps(sim, StepsFor(Balance.PuddleTime), V2.Zero);
            Assert.AreEqual(0, r.Puddles.Count, "a poca durou mais de 4 s");
            Assert.IsFalse(r.Courier.Slowed);
        }

        [Test]
        public void Capitao_RoubaACargaDoChao_ChamaDoisLadroes_SoSoltaNoSegundoGolpe_TrancaAPorta()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            r.Courier.Hp = 99;
            Steps(sim, 1, Up); Steps(sim, 7, V2.Zero);   // pousa
            Enemy c = r.Spawn(EnemyType.Captain, r.Main.Pos + new V2(0.3f, 0f));
            var evs = new List<SimEvent>();
            for (int k = 0; k < StepsFor(1f) && c.Carry == null; k++) evs.AddRange(Steps(sim, 1, V2.Zero));
            Assert.AreSame(r.Main, c.Carry, "o Capitao nao pegou a carga do chao");
            Assert.IsTrue(r.Doors[0].Locked, "Capitao vivo tranca a porta");
            SimEvent call = evs.Find(e => e.Kind == Ev.CaptainCall);
            Assert.AreEqual(Balance.CaptainCalls, call.B, "chama 2 ladroes");
            Assert.AreEqual(2, r.Enemies.FindAll(e => e.Alive && e.Type == EnemyType.Thief).Count);
            int hits = 0; bool dropped = false;
            for (int k = 0; k < StepsFor(3f) && !dropped; k++)
                foreach (SimEvent e in Steps(sim, 1, V2.Zero))
                {
                    if (e.Kind == Ev.EnemyHit && e.A == c.Id) { hits++; if (hits == 1) Assert.IsNotNull(c.Carry, "soltou no 1o golpe"); }
                    if (e.Kind == Ev.CargoDrop && e.B == 2) dropped = true;
                }
            Assert.IsTrue(dropped, "o Capitao nao soltou a carga");
            Assert.AreEqual(Balance.CaptainDropHits, hits);
        }

        // ------------------------------------------------------------------ armas

        [Test]
        public void Bumerangue_VaiEVolta_AtravessaInimigos_EPrecisaDeLinhaLivre()
        {
            var sim = ArmedSim(Weapon.Boomerang); Run r = Start(sim, 0);
            Assert.AreEqual(Weapon.Boomerang, r.Weapon);
            r.Courier.Pos = new V2(4.5f, 2f);
            Steps(sim, 1, Up);
            Enemy g1 = r.Spawn(EnemyType.Guard, new V2(4.5f, 4.5f)), g2 = r.Spawn(EnemyType.Guard, new V2(4.5f, 6.0f));
            g1.Hp = 99; g2.Hp = 99;
            float t0 = -1f; int h1 = 0, h2 = 0;
            for (int k = 0; k < StepsFor(2f) && (t0 < 0f || r.Time < t0 + 0.7f); k++)
                foreach (SimEvent e in Steps(sim, 1, V2.Zero))
                {
                    if (e.Kind == Ev.Shot && t0 < 0f) { t0 = r.Time; Assert.AreEqual((int)Weapon.Boomerang, e.B); }
                    if (e.Kind == Ev.EnemyHit && e.A == g1.Id) h1++;
                    if (e.Kind == Ev.EnemyHit && e.A == g2.Id) h2++;
                }
            Assert.AreEqual(1, h1, "1o inimigo: so a ida (a volta ainda nao passou)");
            Assert.AreEqual(2, h2, "2o inimigo atras do 1o: ida e volta (atravessa)");

            var b = ArmedSim(Weapon.Boomerang); Run rb = Start(b, 0);   // R1: pilar em (2; 6,5)
            rb.Courier.Pos = new V2(2.0f, 3.5f);
            Enemy hidden = rb.Spawn(EnemyType.Guard, new V2(2.0f, 8.0f));
            Assert.IsFalse(rb.CanHit(hidden), "bumerangue atras do pilar");
            var s = ArmedSim(Weapon.Sling); Run rs = Start(s, 0);
            rs.Courier.Pos = new V2(2.0f, 3.5f);
            Assert.IsTrue(rs.CanHit(rs.Spawn(EnemyType.Guard, new V2(2.0f, 8.0f))), "estilingue teleguiado passa por cima");
        }

        [Test]
        public void Bombinha_DanoEmArea_RachaOOvo_EDetonaOBarril()
        {
            var sim = ArmedSim(Weapon.Bomb); Run r = Start(sim, 0);
            r.Courier.Pos = new V2(4.5f, 2f); Steps(sim, 1, Up);
            Enemy a = r.Spawn(EnemyType.Guard, new V2(4.5f, 5.0f)), b = r.Spawn(EnemyType.Guard, new V2(5.4f, 5.3f));
            a.Hp = 99; b.Hp = 99;
            List<SimEvent> evs = Steps(sim, StepsFor(1.0f), V2.Zero);
            Assert.AreEqual(1, CountEv(evs, Ev.Explosion));
            int dmg = WeaponDef.All[(int)Weapon.Bomb].Damage[0];
            Assert.AreEqual(99 - dmg, a.Hp); Assert.AreEqual(99 - dmg, b.Hp, "a bombinha fere quem esta na area");

            var se = ArmedSim(Weapon.Bomb); Run re = Start(se, IndexOf(CargoType.Egg));   // ovo no chao perto da explosao racha
            re.Courier.Hp = 99;
            Steps(se, 1, Up); Steps(se, 7, V2.Zero);
            Assert.AreEqual(CargoState.Ground, re.Main.State);
            Enemy t = re.Spawn(EnemyType.Guard, re.Main.Pos + new V2(0f, 1.2f)); t.Hp = 99;
            evs = Steps(se, StepsFor(1.0f), V2.Zero);
            Assert.IsTrue(Has(evs, Ev.Explosion));
            Assert.GreaterOrEqual(re.Main.Cracks, 1, "explosao perto do ovo racha");

            var sb = ArmedSim(Weapon.Bomb); Run rb = Start(sb, IndexOf(CargoType.Barrel));   // barril no chao perto da explosao detona
            rb.Courier.Hp = 99;
            Steps(sb, 1, Up); NoWave(sb, 7, V2.Zero);
            Enemy u = rb.Spawn(EnemyType.Guard, rb.Main.Pos + new V2(0f, 1.2f)); u.Hp = 99;
            evs = NoWave(sb, StepsFor(1.0f), V2.Zero);
            Assert.IsFalse(sb.InContract);
            Assert.AreEqual(Outcome.Exploded, sb.LastResult.Outcome, "a bombinha devia detonar o barril antes dos 3 s");
            Assert.AreEqual(2, CountEv(evs, Ev.Explosion), "bombinha + barril");
        }

        [Test]
        public void Armas_DestravamPorEstrelas_NivelCustaMoedas_TrocaSoForaDoContrato()
        {
            var sim = new Sim(5) { Reputation = Sim.Contracts.Length, Coins = 5000 };
            Assert.IsTrue(sim.WeaponUnlocked(Weapon.Sling)); Assert.AreEqual(1, sim.WeaponLevel(Weapon.Sling));
            Assert.IsFalse(sim.WeaponUnlocked(Weapon.Boomerang)); Assert.AreEqual(0, sim.WeaponLevel(Weapon.Boomerang));
            Assert.AreEqual(-1, sim.WeaponCost(Weapon.Boomerang));
            Assert.IsFalse(sim.Equip(Weapon.Boomerang), "equipou arma bloqueada");
            var evs = new List<SimEvent>();
            for (int c = 0; sim.Stars < WeaponDef.All[(int)Weapon.Boomerang].Stars; c++) { Deliver(sim, c); evs.AddRange(sim.Events); }
            Assert.IsTrue(evs.Exists(e => e.Kind == Ev.WeaponUnlock && e.A == (int)Weapon.Boomerang));
            Assert.IsTrue(sim.WeaponUnlocked(Weapon.Boomerang)); Assert.AreEqual(1, sim.WeaponLevel(Weapon.Boomerang));
            int cost = sim.WeaponCost(Weapon.Boomerang), coins = sim.Coins;
            Assert.AreEqual(WeaponDef.All[(int)Weapon.Boomerang].Cost[1], cost);
            Assert.IsTrue(sim.UpgradeWeapon(Weapon.Boomerang));
            Assert.AreEqual(coins - cost, sim.Coins); Assert.AreEqual(2, sim.WeaponLevel(Weapon.Boomerang));
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == Ev.CurrencySink && e.B == 10 + (int)Weapon.Boomerang));
            Assert.IsTrue(sim.Equip(Weapon.Boomerang));
            Assert.IsTrue(Has(sim.Events, Ev.WeaponEquip));
            Assert.AreEqual("weapon_equip", SimEvent.DiaryName(Ev.WeaponEquip));
            Run r = sim.StartContract(0, false);
            Assert.AreEqual(Weapon.Boomerang, r.Weapon); Assert.AreEqual(2, r.WeaponLevel);
            Assert.IsFalse(sim.Equip(Weapon.Sling), "trocou de arma no meio do contrato");
            Assert.IsFalse(sim.UpgradeWeapon(Weapon.Boomerang), "subiu arma no meio do contrato");
            sim.Abandon();
            Assert.IsTrue(sim.UpgradeWeapon(Weapon.Boomerang));
            Assert.AreEqual(-1, sim.WeaponCost(Weapon.Boomerang), "nivel 3 e o maximo");
            var poor = new Sim(5) { Reputation = 24 }; Assert.IsFalse(poor.UpgradeWeapon(Weapon.Sling));
            var dev = new Sim(1); dev.Events.Clear(); dev.DevStars(WeaponDef.All[(int)Weapon.Bomb].Stars);   // gancho de teste/fotos
            Assert.AreEqual(WeaponDef.All[(int)Weapon.Bomb].Stars, dev.Stars); Assert.AreEqual(3, dev.StarsOf(0));
            Assert.IsTrue(dev.WeaponUnlocked(Weapon.Bomb)); Assert.IsFalse(dev.Dirty); Assert.AreEqual(0, dev.Events.Count);
        }

        // ------------------------------------------------------------------ perks

        [Test]
        public void Perk_OfertaDe3DepoisDasCamadas2e4_PausaOJogo_EscolhaAplica()
        {
            var sim = NewSim(); Run r = Start(sim, 3);   // C4: 5 camadas + entrega
            var offers = new List<int>();
            for (int guard = 0; guard < 20 && sim.InContract && !r.Room.Delivery; guard++)
            {
                if (r.PerkOffer != null)
                {
                    offers.Add(r.Layer);
                    Assert.AreEqual(Balance.PerkOfferSize, r.PerkOffer.Length);
                    Assert.AreNotEqual(r.PerkOffer[0], r.PerkOffer[1]); Assert.AreNotEqual(r.PerkOffer[1], r.PerkOffer[2]); Assert.AreNotEqual(r.PerkOffer[0], r.PerkOffer[2]);
                    float t = r.Time; Steps(sim, 10, Up);
                    Assert.AreEqual(t, r.Time, "o jogo andou com a escolha aberta");
                    Perk p = r.PerkOffer[1];
                    Assert.IsTrue(sim.PickPerk(1));
                    Assert.IsTrue(Has(sim.Events, Ev.PerkPick));
                    Assert.IsTrue(r.Has(p)); Assert.IsNull(r.PerkOffer);
                    continue;
                }
                int layer = r.Layer;
                r.Courier.Pos = new V2(r.Doors[0].Pos.X, 13.4f);
                for (int i = 0; i < 150 && r.Layer == layer; i++) { r.Enemies.Clear(); Steps(sim, 1, Up); }
            }
            Assert.AreEqual(new[] { 2, 4 }, offers.ToArray(), "oferta depois das camadas 2 e 4 (nunca na entrega)");
            Assert.AreEqual(2, r.Perks.Count);
            Assert.IsFalse(sim.PickPerk(0), "escolheu sem oferta");
            Assert.AreEqual("perk_pick", SimEvent.DiaryName(Ev.PerkPick));

            Run x = ToOffer(NewSim(7), 3), y = ToOffer(NewSim(7), 3);   // sorteio pela semente
            Assert.AreEqual(x.PerkOffer, y.PerkOffer);
        }

        [Test]
        public void Perks_CadaUmMudaARun()
        {
            var s1 = NewSim(); Run r1 = Start(s1, 0); r1.Perks.Add(Perk.LightStep);   // Passo leve
            V2 a = r1.Courier.Pos; Steps(s1, 10, Up);
            Assert.AreEqual(Balance.CarrySpeed * Balance.PerkLightStep * 10 * Dt, V2.Dist(a, r1.Courier.Pos), 0.01f);

            var s2 = NewSim(); Run r2 = Start(s2, 0); r2.Perks.Add(Perk.QuickPlant);   // Pouso rapido
            Steps(s2, 1, Up); Steps(s2, 3, V2.Zero);
            Assert.AreEqual(CargoState.Ground, r2.Main.State, "Pouso rapido: pousa em 0,1 s");

            var s3 = NewSim(); Run r3 = Start(s3, IndexOf(CargoType.Relic)); r3.Perks.Add(Perk.Padded);   // Carga acolchoada
            r3.Courier.Hp = 99; r3.Courier.Pos = new V2(4.5f, 3f);
            r3.Spawn(EnemyType.Bat, r3.Courier.Pos); Steps(s3, 2, Left);
            Assert.AreEqual(0, r3.Main.Cracks, "o 1o golpe na carga devia ser ignorado"); Assert.IsTrue(r3.PadUsed);
            r3.Enemies.Clear(); r3.Spawn(EnemyType.Bat, r3.Courier.Pos); Steps(s3, 2, Left);
            Assert.AreEqual(1, r3.Main.Cracks, "so o 1o");

            var s4 = NewSim(); Run r4 = Start(s4, 0); r4.Perks.Add(Perk.QuickHand);   // Mao rapida
            r4.Courier.Pos = new V2(4.5f, 3f); Steps(s4, 1, Up);
            Enemy g = r4.Spawn(EnemyType.Guard, new V2(4.5f, 6.8f)); g.Hp = 99;
            bool shot = false;
            for (int k = 0; k < 20 && !shot; k++) shot = Has(Steps(s4, 1, V2.Zero), Ev.Shot);
            Assert.IsTrue(shot);
            Assert.AreEqual(Balance.ShotCooldown / Balance.PerkQuickHand, r4.Courier.ShotCd, 1e-3f);

            var s5 = NewSim(); Run r5 = Start(s5, 0); r5.Perks.Add(Perk.Lookout);   // Vigia: ladrao da carga sozinha em 12 s
            Steps(s5, 3, Up); Steps(s5, 6, V2.Zero);
            r5.Courier.Pos = new V2(4.5f, 10f);
            var evs = new List<SimEvent>();
            for (int i = 0; i < StepsFor(11.5f); i++) evs.AddRange(Steps(s5, 1, i % 20 < 10 ? Left : Right));
            Assert.IsFalse(Has(evs, Ev.ThiefSpawn), "Vigia: ladrao antes de 12 s");
            for (int i = 0; i < StepsFor(0.8f); i++) evs.AddRange(Steps(s5, 1, i % 20 < 10 ? Left : Right));
            Assert.IsTrue(Has(evs, Ev.ThiefSpawn));

            var s6 = NewSim(); Run r6 = Start(s6, 0); r6.Perks.Add(Perk.IronBoot);   // Bota de ferro
            r6.Courier.Pos = new V2(4.5f, 3f); r6.Courier.Hp = 99;
            Enemy br = r6.Spawn(EnemyType.Brute, new V2(4.5f, 4.3f)); br.Hp = 99;
            V2 b0 = br.Pos;
            evs = Steps(s6, 2, Up, true);
            Assert.IsTrue(evs.Exists(e => e.Kind == Ev.Stun && e.A == br.Id));
            Assert.Greater(br.Stun, 0f);
            Assert.Greater(V2.Dist(b0, br.Pos), Balance.DashPush + 0.5f, "empurra mais que o dash comum");

            var s7 = NewSim(); Run r7 = Start(s7, 3); r7.Perks.Add(Perk.Magnet); r7.Perks.Add(Perk.ChestLuck);   // Ima + Sorte de bau (R5: bau 35)
            r7.Courier.Hp = 99; Steps(s7, 1, Up);
            r7.Courier.Pos = r7.Chest.Pos + new V2(2.2f, 0f);
            int coins = s7.Coins;
            Steps(s7, StepsFor(1.5f), V2.Zero);
            Assert.IsTrue(r7.Chest.Opened, "Ima: abre o bau de 2,2 m");
            Assert.AreEqual(coins + 46, s7.Coins, "Sorte de bau: 35 x 1,3 = 45,5 -> 46");

            bool heart = false, twice = false;
            for (int seed = 1; seed < 200 && !(heart && twice); seed++)
            {
                Sim s = NewSim(seed); Run r = ToOffer(s, 3);
                int kh = System.Array.IndexOf(r.PerkOffer, Perk.ExtraHeart), kd = System.Array.IndexOf(r.PerkOffer, Perk.DoubleDash);
                if (!heart && kh >= 0)
                {
                    int max = r.Courier.MaxHp; r.Courier.Hp = max - 3;
                    s.PickPerk(kh);
                    Assert.AreEqual(max + Balance.PerkHeartHp, r.Courier.MaxHp); Assert.AreEqual(max - 1, r.Courier.Hp, "Coracao extra: +2 max e cura 2");
                    heart = true;
                }
                else if (!twice && kd >= 0)
                {
                    s.PickPerk(kd);
                    Assert.AreEqual(2, r.Courier.DashChargesMax);
                    r.Enemies.Clear(); Steps(s, 2, Up); Steps(s, 1, Up, true); Steps(s, 6, Up); Steps(s, 1, Up, true);
                    Assert.AreEqual(2, r.Courier.DashCount, "Dash duplo: 2 dashes seguidos");
                    twice = true;
                }
            }
            Assert.IsTrue(heart && twice, "nenhuma oferta com Coracao extra / Dash duplo em 200 sementes");
        }

        // ------------------------------------------------------------------ eventos de sala

        [Test]
        public void Reforco_VemPelaCondicao_EmFila_ComTetoPorVisitaEDeVivos()
        {
            int i = IndexOf(CargoType.Relic);   // C5 (nivel 4: ate 2 reforcos por visita), R1: reforco com a carga 3 s no chao
            Assert.AreEqual(Content.R1, Sim.Contracts[i].Layers[0][0]);
            Assert.AreEqual(RoomTrigger.CargoDown, Content.Rooms[Content.R1].WaveTrigger);
            Assert.AreEqual(2, Balance.MaxWaves(Sim.Contracts[i].Tier));
            var sim = NewSim(); Run r = Start(sim, i); r.Courier.Hp = 99;
            Steps(sim, 1, Up);
            var evs = new List<SimEvent>(); float plant = -1f, first = -1f; int pendingAtEvent = -1, spawnedNext = -1;
            for (int k = 0; k < StepsFor(20f); k++)
            {
                List<SimEvent> e1 = Steps(sim, 1, V2.Zero);
                if (spawnedNext == -2) spawnedNext = r.Enemies.FindAll(e => e.Wave).Count;
                foreach (SimEvent e in e1)
                {
                    if (e.Kind == Ev.Plant && plant < 0f) plant = r.Time;
                    if (e.Kind == Ev.RoomEvent && first < 0f) { first = r.Time; pendingAtEvent = r.WavePending; spawnedNext = -2; }
                }
                evs.AddRange(e1);
                if (spawnedNext >= 0) r.Enemies.RemoveAll(e => e.Wave && r.WavePending == 0);   // "mata" o reforco depois que todo chegou
            }
            Assert.AreEqual(Balance.WaveDownTime, first - plant, 0.1f, "reforco com a carga 3 s no chao");
            Assert.AreEqual(Content.Rooms[Content.R1].Wave.Length, pendingAtEvent, "o reforco entra na fila");
            Assert.LessOrEqual(spawnedNext, 1, "chega um por vez");
            Assert.AreEqual(2, CountEv(evs, Ev.RoomEvent), "teto de 2 reforcos por visita");

            var tut = NewSim(); Run rt = Start(tut, 0);   // tutorial sem reforco
            Steps(tut, 1, Up);
            Assert.IsFalse(Has(Steps(tut, StepsFor(8f), V2.Zero), Ev.RoomEvent), "C1 (tutorial) nao tem reforco");

            var cap = NewSim(); Run rc = Start(cap, i); rc.Courier.Hp = 99;   // teto de vivos
            Steps(cap, 1, Up); Steps(cap, 7, V2.Zero);   // pousa
            for (int k = 0; k < Balance.MaxAlive - 1; k++) rc.Spawn(EnemyType.Guard, new V2(0.6f + 0.9f * k, 11.5f)).Hp = 999;
            rc.Courier.Pos = new V2(4.5f, 9f);   // longe da carga, andando (nao atira)
            for (int k = 0; k < StepsFor(6f); k++) Steps(cap, 1, k % 20 < 10 ? Left : Right);
            Assert.AreEqual(Balance.MaxAlive, rc.Enemies.FindAll(e => e.Alive).Count);
            Assert.AreEqual(1, rc.Enemies.FindAll(e => e.Wave).Count, "o reforco para no teto de vivos");
            Assert.AreEqual(1, rc.WavePending, "o resto espera vaga");
        }

        [Test]
        public void Emboscada_PortaFechaAteOReforcoCair_NaoContaComoLutaEscolhida_MensagemAbre()
        {
            Assert.IsTrue(Content.Rooms[Content.R10].WaveLocks);
            Sim sim = InRoom(3, 3, Content.R10, CargoType.Box);   // C4, camada 3
            Run r = sim.Run; r.Enemies.Clear(); r.Courier.Hp = 99;
            r.Courier.Pos = new V2(4.5f, 6.8f);
            List<SimEvent> evs = Steps(sim, 3, Up);
            Assert.IsTrue(Has(evs, Ev.RoomEvent), "passar da metade chama o reforco");
            Assert.IsTrue(r.Doors[0].Locked, "a porta fecha com o reforco na fila");
            for (int k = 0; k < StepsFor(5f) && r.WavePending > 0; k++) Steps(sim, 1, Up);
            Assert.AreEqual(0, r.WavePending);
            Assert.IsTrue(r.Doors[0].Locked, "a porta fecha ate o reforco cair");
            foreach (Enemy e in r.Enemies) if (e.Wave) { e.Hp = 1; e.Pos = r.Courier.Pos + new V2(0f, 2f); }
            for (int k = 0; k < StepsFor(4f) && r.Enemies.Exists(e => e.Alive && e.Wave); k++) Steps(sim, 1, V2.Zero);   // parado: atira e derruba
            Assert.IsFalse(r.Enemies.Exists(e => e.Alive && e.Wave));
            Steps(sim, 1, V2.Zero);
            Assert.IsFalse(r.Doors[0].Locked, "reforco caiu: a porta abre");
            Assert.IsFalse(r.FoughtHere, "emboscada e luta obrigatoria (nao conta no H1)");

            int m = IndexOf(CargoType.Message);
            Assert.IsTrue(System.Array.IndexOf(Sim.Contracts[m].Layers[3], Content.R10) >= 0);
            Sim ms = InRoom(m, 3, Content.R10, CargoType.Message);
            Run mr = ms.Run; mr.Enemies.Clear(); mr.Courier.Hp = 99;
            mr.Courier.Pos = new V2(4.5f, 6.8f);
            evs = Steps(ms, 3, Up);
            Assert.IsTrue(Has(evs, Ev.RoomEvent));
            Assert.IsFalse(mr.Doors[0].Locked, "a mensagem abre a porta da emboscada");
        }

        // ------------------------------------------------------------------ sala secreta e icones

        [Test]
        public void SalaSecreta_EmpurrarAParedeRachadaComACargaLevaAoBau_PassarRenteOuSemCargaNao()
        {
            Assert.AreEqual(Content.R3, Sim.Contracts[3].Layers[1][1]);
            Sim sim = InRoom(3, 1, Content.R3, CargoType.Box);
            Run r = sim.Run; r.Enemies.Clear();
            Assert.IsNotNull(r.Secret); Assert.AreEqual(Content.S1, r.Secret.Room);
            var before = new List<int>(); foreach (Door d in r.Doors) before.Add(d.Room);
            r.Courier.Pos = new V2(8.6f, 1.6f);
            Quiet(sim, 15, Up);   // subindo rente a parede rachada
            Assert.AreEqual(Content.R3, r.Room.Id, "passar rente abriu a parede");
            r.Courier.Pos = new V2(2.0f, 2.0f); Quiet(sim, 8, V2.Zero);   // pousa longe
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            r.Courier.Pos = new V2(8.0f, 3.2f); Quiet(sim, 6, Right);
            Assert.AreEqual(Content.R3, r.Room.Id, "sem a carga nao entra");
            r.Courier.Pos = r.Main.Pos + new V2(0.5f, 0f); Quiet(sim, 1, Up);
            Assert.AreEqual(CargoState.Held, r.Main.State);
            r.Courier.Pos = new V2(8.0f, 3.2f);
            List<SimEvent> evs = Quiet(sim, 6, Right);
            Assert.IsTrue(Has(evs, Ev.SecretFound));
            Assert.AreEqual(Content.S1, r.Room.Id); Assert.AreEqual(1, r.Layer, "mesma camada");
            var after = new List<int>(); foreach (Door d in r.Doors) after.Add(d.Room);
            Assert.AreEqual(before, after, "a sala secreta leva as mesmas salas da proxima camada");
            Assert.IsNotNull(r.Chest); Assert.Greater(r.Chest.Coins, 0);
            Assert.IsNull(r.Secret);
            Sim b = Sim.Load(sim.Save());   // retomar dentro da sala secreta
            Assert.IsTrue(b.InContract); Assert.AreEqual(Content.S1, b.Run.Room.Id);
        }

        [Test]
        public void Porta_MostraORiscoDaCargaDaSalaDeDestino()
        {
            Assert.AreEqual(CargoRisk.Smash, Content.Rooms[Content.R5].Risk);
            Assert.AreEqual(CargoRisk.Archer, Content.Rooms[Content.R7].Risk);
            Assert.AreEqual(CargoRisk.None, Content.Rooms[Content.R4].Risk);
            Assert.AreEqual(CargoRisk.Thief | CargoRisk.Archer | CargoRisk.Smash, Content.Rooms[Content.R16].Risk);
            Assert.AreEqual(CargoRisk.Thief, Content.Rooms[Content.M1].Risk & CargoRisk.Thief);
            int none = 0, thief = 0;
            foreach (int id in Content.NormalRooms) { if (Content.Rooms[id].Risk == CargoRisk.None) none++; if ((Content.Rooms[id].Risk & CargoRisk.Thief) != 0) thief++; }
            Assert.GreaterOrEqual(none, 3, "salas sem risco para a carga"); Assert.GreaterOrEqual(thief, 4); Assert.LessOrEqual(thief, 10, "icone de ladrao em quase toda sala nao diz nada");
            for (int c = 0; c < Sim.Contracts.Length; c++)
            {
                var sim = NewSim(); Run r = Start(sim, c);
                foreach (Door d in r.Doors) Assert.AreEqual(Content.Rooms[d.Room].Risk, d.Risk, Sim.Contracts[c].Code);
                sim.Abandon();
            }
        }

        // ------------------------------------------------------------------ estrelas, dia, codigo

        [Test]
        public void Estrelas_EntregueIntactaNoTempo_GuardaAMelhor()
        {
            var sim = new Sim(9) { Reputation = Sim.Contracts.Length };
            ContractResult a = Deliver(sim, 0);
            Assert.AreEqual(3, a.Stars); Assert.IsTrue(a.StarDelivered && a.StarIntact && a.StarTime); Assert.IsTrue(a.NewBest);
            Assert.AreEqual(3, sim.StarsOf(0));
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == Ev.ContractStars && e.A == 3 && e.B == 0));
            Assert.AreEqual("contract_stars", SimEvent.DiaryName(Ev.ContractStars));
            Run r = Start(sim, 0); r.Main.Cracks = 1;
            ContractResult b = DeliverFrom(sim);
            Assert.AreEqual(2, b.Stars); Assert.IsFalse(b.StarIntact); Assert.IsFalse(b.NewBest);
            Assert.AreEqual(3, sim.StarsOf(0), "o recorde nao pode cair");
            r = Start(sim, 1); r.Time = Sim.Contracts[1].ParTime + 1f;
            ContractResult c = DeliverFrom(sim);
            Assert.IsFalse(c.StarTime); Assert.AreEqual(2, c.Stars);
            Start(sim, 2); sim.Abandon();
            Assert.AreEqual(0, sim.LastResult.Stars);
            Assert.AreEqual(3 + 2, sim.Stars);
            Assert.AreEqual(Sim.Contracts.Length, sim.Reputation, "repetir nao mexe na reputacao (progresso)");
            var egg = NewSim(); Run re = Start(egg, IndexOf(CargoType.Egg)); re.Time = Sim.Contracts[IndexOf(CargoType.Egg)].EggTarget; Steps(egg, 1, Up);
            Assert.IsTrue(re.Hatched);
            Assert.IsFalse(DeliverFrom(egg).StarIntact, "ovo chocado nao e carga intacta");
        }

        [Test]
        public void ContratoDoDia_UmaVezPorData_RelogioVoltandoNaoRepete_BonusUmaVez()
        {
            const int d1 = 20261010, d2 = 20261011;
            var fresh = new Sim(3);
            Assert.IsFalse(fresh.DailyOpen); Assert.IsNull(fresh.StartDaily(d1, false), "contrato do dia antes do C3");
            var sim = new Sim(3) { Reputation = Balance.OpenAfter };
            Assert.IsTrue(sim.DailyAvailable(d1));
            ContractDef def = Sim.DailyContract(d1);
            Assert.AreEqual(-1, def.Index); Assert.AreEqual(ContractKind.Daily, def.Kind); Assert.AreEqual(d1, def.Date);
            Assert.AreEqual(def.ShareCode, Sim.DailyContract(d1).ShareCode, "mesma data, mesmo contrato");
            Assert.AreEqual(def.Layers.Length, Sim.DailyContract(d1).Layers.Length);
            Assert.AreNotEqual(def.ShareCode, Sim.DailyContract(d2).ShareCode);
            Run r = sim.StartDaily(d1, false);
            Assert.IsNotNull(r);
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == Ev.DailyStart && e.A == d1));
            int rep = sim.Reputation;
            ContractResult res = DeliverFrom(sim);
            Assert.AreEqual(Outcome.Delivered, res.Outcome);
            Assert.AreEqual(Balance.DailyBonus, res.DailyBonus);
            Assert.AreEqual(res.Payment + res.ChestCoins + res.DailyBonus + res.RescueBonus, res.Total);
            Assert.AreEqual(rep, sim.Reputation, "o dia nao mexe na reputacao");
            Assert.AreEqual(0, sim.Stars, "o dia nao mexe nas estrelas da campanha");
            Assert.AreEqual(def.ShareCode, res.ShareCode);
            Assert.IsTrue(Has(sim.Events, Ev.DailyEnd));
            Assert.IsFalse(sim.DailyAvailable(d1), "repetiu o dia"); Assert.IsNull(sim.StartDaily(d1, false));
            sim.SeeDate(d2);
            Assert.IsTrue(sim.DailyAvailable(d2));
            Assert.IsFalse(sim.DailyAvailable(20261009), "voltar o relogio liberou o dia");
            Sim b = Sim.Load(sim.Save());
            Assert.AreEqual(d2, b.LastDateSeen); Assert.AreEqual(d1, b.DailyDone);
            Assert.IsFalse(b.DailyAvailable(d1));
            Assert.IsNotNull(b.StartDaily(d2, false));
            b.Abandon();
            Assert.AreEqual(0, b.LastResult.DailyBonus, "falhar nao paga o bonus");
            Assert.IsTrue(b.DailyAvailable(d2), "falhar nao gasta o dia");

            var clock = new Sim(3) { Reputation = Balance.OpenAfter };   // relogio voltando para um dia nunca jogado
            clock.SeeDate(20261015);
            Assert.IsTrue(clock.DailyAvailable(20261015));
            Assert.IsFalse(clock.DailyAvailable(20261012), "voltar o relogio liberou um dia");
            Assert.IsNull(clock.StartDaily(20261012, false));
            Sim cl = Sim.Load(clock.Save());
            Assert.AreEqual(20261015, cl.LastDateSeen);
            Assert.IsFalse(cl.DailyAvailable(20261012), "reabrir o app esqueceu a maior data vista");
        }

        [Test]
        public void ContratoPorCodigo_IdaEVolta_RecusaAmbiguoEDigitoErrado_MesmoCodigoMesmoContrato()
        {
            Assert.AreEqual(32, ContractCode.Alphabet.Length);
            foreach (char ch in "IO01") Assert.AreEqual(-1, ContractCode.Alphabet.IndexOf(ch), "caractere ambiguo no alfabeto: " + ch);
            var rng = new Rng(99);
            int[] fixedP = { 0, 1, ContractCode.MaxPayload, 12345 };
            for (int k = 0; k < 3000; k++)
            {
                int p = k < fixedP.Length ? fixedP[k] : (int)(rng.Next() & ContractCode.MaxPayload);
                string c = ContractCode.Encode(p);
                Assert.AreEqual(ContractCode.Length, c.Length);
                Assert.IsTrue(ContractCode.TryDecode(c, out int back), c);
                Assert.AreEqual(p, back, c);
            }
            string code = ContractCode.Encode(4242);
            Assert.IsTrue(ContractCode.IsValid(code.ToLowerInvariant().Insert(3, "-")), "minuscula e traco valem");
            Assert.IsFalse(ContractCode.IsValid(code.Substring(0, 5)));
            Assert.IsFalse(ContractCode.IsValid("ABCDE0")); Assert.IsFalse(ContractCode.IsValid("ABCDEI")); Assert.IsFalse(ContractCode.IsValid(null));
            int caught = 0, total = 0;
            for (int k = 0; k < 600; k++)
            {
                string c = ContractCode.Encode((int)(rng.Next() & ContractCode.MaxPayload));
                int pos = k % ContractCode.Length;
                char other = ContractCode.Alphabet[(ContractCode.Alphabet.IndexOf(c[pos]) + 1 + k % 31) % 32];
                total++;
                if (!ContractCode.IsValid(c.Remove(pos, 1).Insert(pos, other.ToString()))) caught++;
            }
            Assert.GreaterOrEqual(100 * caught / total, 90, "digito de verificacao deixa passar erro de digitacao demais");
            ContractDef a = Sim.CodeContract(code), b = Sim.CodeContract(code.ToLowerInvariant());
            Assert.AreEqual(a.ShareCode, b.ShareCode); Assert.AreEqual(a.Cargo, b.Cargo); Assert.AreEqual(a.Layers, b.Layers);
            Assert.AreEqual(-1, a.Index); Assert.AreEqual(ContractKind.Code, a.Kind);
            Assert.That(a.Layers.Length - 1, Is.InRange(4, 6));
            for (int l = 0; l < a.Layers.Length - 1; l++) foreach (int id in a.Layers[l]) Assert.IsTrue(Content.Rooms[id].Normal);
            Assert.IsNull(Sim.CodeContract("ZZZ"));
            var sim = new Sim(1) { Reputation = Balance.OpenAfter - 1 };
            Assert.IsNull(sim.StartCode(code, false), "codigo antes do C3");
            sim.Reputation = Balance.OpenAfter;
            Assert.IsNull(sim.StartCode("ABCDE0", false));
            Run r = sim.StartCode(code, false);
            Assert.IsNotNull(r);
            Assert.IsTrue(sim.Events.Exists(e => e.Kind == Ev.CodeStart && e.A == 4242));
            ContractResult res = DeliverFrom(sim);
            Assert.AreEqual(code, res.ShareCode); Assert.Greater(res.Payment, 0);
            Assert.AreEqual(Balance.OpenAfter, sim.Reputation);
        }

        // ------------------------------------------------------------------ save v2

        [Test]
        public void SaveV2_IdaEVolta_EstrelasArmasDatas_PerksEOfertaNoContrato()
        {
            var sim = new Sim(5) { Reputation = Sim.Contracts.Length, Coins = 3000 };
            for (int i = 0; i < 5; i++) Deliver(sim, i);
            Assert.IsTrue(sim.Equip(Weapon.Boomerang)); Assert.IsTrue(sim.UpgradeWeapon(Weapon.Boomerang));
            sim.SeeDate(20261012);
            Run r = ToOffer(sim, 3);
            string open = sim.Save();
            Sim o = Sim.Load(open);
            Assert.AreEqual(r.PerkOffer, o.Run.PerkOffer, "a oferta aberta sobrevive ao save");
            float t = o.Run.Time; Steps(o, 5, Up);
            Assert.AreEqual(t, o.Run.Time, "retomado com oferta aberta: jogo parado");
            Assert.AreEqual(open, o.Save());
            int k = System.Array.IndexOf(r.PerkOffer, Perk.ExtraHeart);
            sim.PickPerk(k >= 0 ? k : 0);
            r.Main.Cracks = 1; r.Courier.Hp = 2;
            string a = sim.Save();
            StringAssert.StartsWith("v=2\n", a);
            Sim b = Sim.Load(a);
            for (int i = 0; i < Sim.Contracts.Length; i++) Assert.AreEqual(sim.StarsOf(i), b.StarsOf(i));
            Assert.AreEqual(Weapon.Boomerang, b.Equipped); Assert.AreEqual(2, b.WeaponLevel(Weapon.Boomerang));
            Assert.AreEqual(20261012, b.LastDateSeen);
            Assert.IsTrue(b.InContract);
            Assert.AreEqual(r.Perks, b.Run.Perks); Assert.AreEqual(r.Courier.MaxHp, b.Run.Courier.MaxHp); Assert.AreEqual(2, b.Run.Courier.Hp);
            Assert.AreEqual(1, b.Run.Main.Cracks); Assert.AreEqual(Weapon.Boomerang, b.Run.Weapon);
            Assert.AreEqual(a, b.Save());

            var d = new Sim(4) { Reputation = Balance.OpenAfter };   // contrato do dia em curso
            d.StartDaily(20261010, false);
            Sim db = Sim.Load(d.Save());
            Assert.IsTrue(db.InContract);
            Assert.AreEqual(ContractKind.Daily, db.Run.Contract.Kind); Assert.AreEqual(20261010, db.Run.Contract.Date);
            var c = new Sim(4) { Reputation = Balance.OpenAfter };   // contrato por codigo em curso
            c.StartCode(ContractCode.Encode(777), false);
            Sim cb = Sim.Load(c.Save());
            Assert.AreEqual(c.Run.Contract.ShareCode, cb.Run.Contract.ShareCode);
        }

        [Test]
        public void SaveV1_MigraParaV2_MochilaZero_EstrelaPorContratoConcluido_ContratoEmCursoDescartado()
        {
            const string v1 = "v=1\nseed=4\nruns=3\nnid=5\ncoins=250\nrep=5\nlv=2,1,3\npend=4,1,1,4,91,4\nresc=2\nrun=1,0,1,5,3.5,77,0,0,0,0,0,0,0,0\ncg0=4,0,0,0,0\n";
            Sim s = Sim.Load(v1);
            Assert.IsFalse(s.SaveRefused);
            Assert.IsTrue(s.Dirty, "migracao tem de regravar em v2");
            Assert.AreEqual(250, s.Coins); Assert.AreEqual(5, s.Reputation);
            Assert.AreEqual(new[] { 2, 1, 3, 0 }, s.Levels, "Mochila nasce no nivel 0");
            for (int i = 0; i < Sim.Contracts.Length; i++) Assert.AreEqual(i < 5 ? 1 : 0, s.StarsOf(i), "C" + (i + 1));
            Assert.IsNull(s.Run, "contrato em curso da v1 (os contratos mudaram) e descartado");
            Assert.IsNotNull(s.Pending); Assert.IsTrue(s.RescuedIds.Contains(2));
            Assert.AreEqual(Weapon.Sling, s.Equipped);
            string v2 = s.Save();
            StringAssert.StartsWith("v=2\n", v2);
            Assert.AreEqual(v2, Sim.Load(v2).Save());
        }

        // ------------------------------------------------------------------ conteudo (medido)

        [Test]
        public void LevelDesign_V1_RegiaoComSalasContratosReforcosEParedesRachadas()
        {
            int normal = 0, secret = 0;
            foreach (RoomDef room in Content.Rooms)
            {
                if (room.Normal) normal++;
                if (room.IsSecret) { secret++; Assert.Greater(room.ChestCoins, 0, room.Code + ": sala secreta sem bau"); }
                foreach (Spawn s in room.Wave)
                {
                    Assert.GreaterOrEqual(V2.Dist(s.Pos, Balance.Entrance), 4f, room.Code + ": reforco em cima da entrada");
                    Assert.IsFalse(Run.Blocked(room, s.Pos, EnemyDef.All[(int)s.Type].Radius), room.Code + ": reforco dentro de obstaculo");
                }
                if (room.Secret < 0) continue;
                Assert.IsTrue(Content.Rooms[room.Secret].IsSecret, room.Code);
                Assert.IsTrue(room.SecretPos.X == 0f || room.SecretPos.X == Balance.RoomW, room.Code + ": parede rachada fora da parede lateral");
                Assert.That(room.SecretPos.Y, Is.InRange(1.5f, 12f), room.Code);
                V2 inside = new V2(room.SecretPos.X == 0f ? 0.5f : Balance.RoomW - 0.5f, room.SecretPos.Y);
                Assert.IsFalse(Run.Blocked(room, inside, Balance.CourierR), room.Code + ": parede rachada atras de obstaculo");
                Assert.Less(new Nav(room).Steps(Balance.Entrance, inside), int.MaxValue, room.Code + ": parede rachada inalcancavel");
            }
            Assert.AreEqual(16, normal); Assert.AreEqual(2, secret);
            Assert.IsTrue(System.Array.Exists(Content.Rooms, x => x.Mini)); Assert.IsTrue(System.Array.Exists(Content.Rooms, x => x.Boss)); Assert.IsTrue(System.Array.Exists(Content.Rooms, x => x.Delivery));
            for (int s = 0; s < Content.Rooms.Length; s++)
                if (Content.Rooms[s].IsSecret) Assert.IsTrue(System.Array.Exists(Content.Rooms, x => x.Secret == s), Content.Rooms[s].Code + ": sala secreta sem parede que leve a ela");

            Assert.AreEqual(24, Sim.Contracts.Length);
            var debut = new HashSet<CargoType>();
            for (int c = 0; c < Sim.Contracts.Length; c++)
            {
                ContractDef d = Sim.Contracts[c];
                int layers = d.Layers.Length - 1;
                bool boss = false;
                foreach (int id in d.Layers[layers - 1]) if (Content.Rooms[id].Boss || Content.Rooms[id].Mini) boss = true;
                if (c < 3) Assert.That(layers, Is.InRange(2, 3), d.Code + ": tutorial");
                else Assert.That(layers - (boss ? 1 : 0), Is.InRange(4, 6), d.Code);
                debut.Add(d.Cargo);
                Assert.AreEqual(c == 11, System.Array.IndexOf(d.Layers[layers - 1], Content.M1) >= 0, d.Code + ": mini-chefe so no C12");
                Assert.AreEqual(c == 23, System.Array.IndexOf(d.Layers[layers - 1], Content.B1) >= 0, d.Code + ": chefe so no C24");
                Assert.Greater(d.ParTime, 0f, d.Code);
                Assert.AreEqual(d.Cargo == CargoType.Message, d.HasTimer, d.Code);
                Assert.AreEqual(d.Cargo == CargoType.Egg, d.EggTarget > 0f, d.Code);
            }
            Assert.AreEqual(CargoDef.All.Length, debut.Count, "todas as cargas aparecem na campanha");
        }
    }
}
