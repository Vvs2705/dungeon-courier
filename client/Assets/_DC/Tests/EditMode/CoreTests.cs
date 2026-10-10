using System.Collections.Generic;
using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Regras do CONTRATO_V01 §3-§8 e o catalogo de exploits do GDD §28. Cada teste monta a situacao pelo estado publico do
    /// Run (posicoes, vida, inimigos) e avanca pelo mesmo Tick que a View e o bot usam.</summary>
    public class CoreTests
    {
        const float Dt = Balance.Step;
        static readonly V2 Up = new V2(0f, 1f), Left = new V2(-1f, 0f), Right = new V2(1f, 0f), Down = new V2(0f, -1f);

        static Sim NewSim(int seed = 7) => new Sim(seed) { Reputation = Sim.Contracts.Length };

        /// <summary>Comeca o contrato e (por padrao) tira os inimigos da 1ª sala.</summary>
        static Run Start(Sim sim, int contract, bool clear = true, bool rescue = false)
        {
            Run r = sim.StartContract(contract, rescue);
            Assert.IsNotNull(r);
            if (clear) r.Enemies.Clear();
            return r;
        }

        /// <summary>Avanca n passos; devolve os eventos de todos eles.</summary>
        static List<SimEvent> Steps(Sim sim, int n, V2 move, bool dashFirst = false)
        {
            var evs = new List<SimEvent>();
            for (int i = 0; i < n; i++)
            {
                sim.Tick(Dt, move, dashFirst && i == 0);
                evs.AddRange(sim.Events);
            }
            return evs;
        }

        static int StepsFor(float seconds) => (int)System.Math.Round(seconds / Dt);
        static bool Has(List<SimEvent> evs, Ev k) { foreach (SimEvent e in evs) if (e.Kind == k) return true; return false; }

        /// <summary>Atravessa o contrato sem inimigos: pega as cargas no chao, entra pela porta `door` de cada sala e entrega.</summary>
        static ContractResult Deliver(Sim sim, int contract, bool rescue = false, int door = 0)
        {
            Start(sim, contract, true, rescue);
            return DeliverFrom(sim, door);
        }

        static ContractResult DeliverFrom(Sim sim, int door = 0)
        {
            Run r = sim.Run;
            for (int guard = 0; guard < 20 && sim.InContract; guard++)
            {
                r.Enemies.Clear();
                foreach (Cargo g in r.Cargos)
                    if (g.State == CargoState.Ground) { r.Courier.Pos = g.Pos + new V2(0.3f, 0f); Steps(sim, 1, new V2(0f, 0.2f)); }
                if (r.Pet != null) r.Pet.Pos = r.Courier.Pos;
                int layer = r.Layer;
                if (r.Room.Delivery)
                {
                    r.Courier.Pos = Balance.Client - new V2(0f, 1f);
                    if (r.Pet != null) r.Pet.Pos = r.Courier.Pos;
                    Steps(sim, 2, Up);
                    break;
                }
                r.Courier.Pos = new V2(r.Doors[System.Math.Min(door, r.Doors.Count - 1)].Pos.X, 13.4f);
                if (r.Pet != null) r.Pet.Pos = r.Courier.Pos;
                for (int i = 0; i < 10 && r.Layer == layer; i++) Steps(sim, 1, Up);
                Assert.Greater(r.Layer, layer, "nao passou pela porta na sala " + r.Room.Code);
            }
            Assert.IsFalse(sim.InContract);
            return sim.LastResult;
        }

        // ------------------------------------------------------------------ carregar / pousar / pegar / dash

        [Test]
        public void Pousa_SoDepoisDe02sDeDedoSolto()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            Steps(sim, 10, Up);
            Assert.AreEqual(CourierState.Carrying, r.Courier.State);
            Steps(sim, 5, V2.Zero);   // 0,167 s
            Assert.AreEqual(CourierState.Carrying, r.Courier.State, "pousou antes de 0,2 s");
            Assert.AreEqual(CargoState.Held, r.Main.State);
            List<SimEvent> evs = Steps(sim, 1, V2.Zero);   // 0,2 s
            Assert.AreEqual(CourierState.Planted, r.Courier.State);
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            Assert.AreEqual(Balance.PlantOffset, V2.Dist(r.Main.Pos, r.Courier.Pos), 0.01f, "pousa a 0,6 m ao lado");
            Assert.IsTrue(Has(evs, Ev.CargoDrop));
        }

        [Test]
        public void AoEntrar_NaoPousaAteOPrimeiroMovimento_FTUE()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            Steps(sim, StepsFor(2f), V2.Zero);   // dedo solto desde o inicio do contrato
            Assert.AreEqual(CargoState.Held, r.Main.State, "pousou sem o jogador ter se mexido");
            Assert.AreEqual(Hint.DragToWalk, r.Hint);
            Steps(sim, 1, Up); Steps(sim, 6, V2.Zero);   // mexeu: a regra dos 0,2 s volta a valer
            Assert.AreEqual(CargoState.Ground, r.Main.State);
        }

        [Test]
        public void PegaDeVolta_Ate15m_NaoAlem()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            Steps(sim, 10, Up); Steps(sim, 6, V2.Zero);
            V2 cargo = r.Main.Pos;
            r.Courier.Pos = cargo + new V2(1.53f, 0f);   // andando para cima fica a ~1,54 m
            Steps(sim, 1, Up);
            Assert.AreEqual(CargoState.Ground, r.Main.State, "pegou alem de 1,5 m");
            Assert.AreEqual(CourierState.Walking, r.Courier.State);
            r.Courier.Pos = cargo + new V2(1.48f, 0f);   // ~1,49 m
            List<SimEvent> evs = Steps(sim, 1, Up);
            Assert.AreEqual(CargoState.Held, r.Main.State, "nao pegou a 1,5 m");
            Assert.IsTrue(Has(evs, Ev.CargoPickup));
        }

        [Test]
        public void SemCargaNasMaos_AndaMaisRapido()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            V2 a = r.Courier.Pos; Steps(sim, 10, Up);
            float carrying = V2.Dist(a, r.Courier.Pos);
            Steps(sim, 6, V2.Zero);   // pousa
            r.Courier.Pos = new V2(4.5f, 6f);
            a = r.Courier.Pos; Steps(sim, 10, Up);
            float free = V2.Dist(a, r.Courier.Pos);
            Assert.AreEqual(Balance.CarrySpeed * 10 * Dt, carrying, 0.01f);
            Assert.AreEqual(carrying * Balance.FreeSpeedMul, free, 0.01f);
        }

        [Test]
        public void Dash_LargaACargaNoPontoDePartida_E3m()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            Steps(sim, 5, Up);
            V2 start = r.Courier.Pos;
            List<SimEvent> evs = Steps(sim, 1, Up, true);
            Assert.AreEqual(CourierState.Dashing, r.Courier.State);
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            Assert.Less(V2.Dist(r.Main.Pos, start), 0.01f, "a carga fica no ponto de partida");
            Assert.IsTrue(Has(evs, Ev.Dash));
            Steps(sim, 5, Up);   // o dash dura 0,18 s = 5,4 passos: termina no 6º
            Assert.AreNotEqual(CourierState.Dashing, r.Courier.State);
            Assert.AreEqual(Balance.DashDist, r.Courier.Pos.Y - start.Y, 0.01f, "dash = 3 m");
            Assert.AreEqual(CargoState.Ground, r.Main.State, "dash nao pode pegar a carga de volta sozinho");
        }

        [Test]
        public void Dash_RespeitaARecarga_EOUpgrade()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            Steps(sim, 2, Up, true);
            Assert.AreEqual(1, r.Courier.DashCount);
            int n = StepsFor(1.5f) - 2;
            for (int i = 0; i < n; i++) Steps(sim, 1, Up, true);   // insiste no dash ate 1,5 s
            Assert.AreEqual(1, r.Courier.DashCount, "dash antes de 1,6 s");
            for (int i = 0; i < StepsFor(0.2f); i++) Steps(sim, 1, Up, true);
            Assert.AreEqual(2, r.Courier.DashCount, "dash nao voltou em 1,7 s");

            var sim2 = NewSim(); sim2.Levels[(int)Upgrade.Dash] = 5;
            Assert.AreEqual(0.8f, sim2.DashCooldown, 1e-4f);
            Run r2 = Start(sim2, 0);
            Steps(sim2, 1, Up, true);
            for (int i = 0; i < StepsFor(0.85f); i++) Steps(sim2, 1, Up, true);
            Assert.AreEqual(2, r2.Courier.DashCount, "upgrade de Dash nao reduziu a recarga");
        }

        [Test]
        public void NaoAtacaAndando_ComOuSemCarga_AtacaParado()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            r.Courier.Hp = 99;
            r.Courier.Pos = new V2(4.5f, 3f);
            r.Spawn(EnemyType.Guard, new V2(4.5f, 6.5f));   // parado a 3,5 m: alvo valido, nao chega no courier
            var evs = new List<SimEvent>();
            for (int i = 0; i < 30; i++) evs.AddRange(Steps(sim, 2, i % 2 == 0 ? Left : Right));   // carregando, vai e volta
            Assert.IsFalse(Has(evs, Ev.Shot), "atacou carregando");
            Steps(sim, 6, V2.Zero);   // pousa
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            r.Courier.Pos = new V2(1f, 3f);
            evs.Clear();
            for (int i = 0; i < 30; i++) evs.AddRange(Steps(sim, 2, i % 2 == 0 ? Down : Up));   // sem carga, andando
            Assert.AreEqual(CourierState.Walking, r.Courier.State);
            Assert.IsFalse(Has(evs, Ev.Shot), "atacou andando sem carga");
            evs = Steps(sim, StepsFor(0.5f), V2.Zero);
            Assert.IsTrue(Has(evs, Ev.Shot), "parado nao atacou");
        }

        // ------------------------------------------------------------------ portas

        [Test]
        public void Porta_NaoAbreSemCarga_NemComGuardaVivo()
        {
            var sim = NewSim(); Run r = Start(sim, 0, clear: false);   // R1: Guarda vivo
            Assert.AreEqual(EnemyType.Guard, r.Enemies[0].Type);
            r.Courier.Pos = new V2(r.Doors[0].Pos.X, 13.4f);
            List<SimEvent> evs = Steps(sim, 10, Up);
            Assert.AreEqual(0, r.Layer, "passou com o Guarda vivo");
            Assert.IsTrue(r.Doors[0].Locked);
            Assert.AreEqual(Hint.GuardDoor, r.Hint);
            Assert.IsTrue(Has(evs, Ev.DoorBlocked));

            r.Enemies.Clear();   // Guarda derrotado
            r.Courier.Pos = new V2(4.5f, 4f);
            Steps(sim, 6, V2.Zero);   // pousa a carga longe da porta
            r.Courier.Pos = new V2(r.Doors[0].Pos.X, 13.4f);
            Steps(sim, 10, Up);
            Assert.AreEqual(0, r.Layer, "passou sem a carga");
            Assert.IsFalse(r.Doors[0].Locked);
            Assert.AreEqual(Hint.DoorNeedsCargo, r.Hint);

            r.Courier.Pos = r.Main.Pos + new V2(0.5f, 0f);
            Steps(sim, 1, Up);   // pega
            r.Courier.Pos = new V2(r.Doors[0].Pos.X, 13.4f);
            evs = Steps(sim, 5, Up);
            Assert.AreEqual(1, r.Layer, "nao passou com a carga e sem Guarda");
            Assert.IsTrue(Has(evs, Ev.RouteChoice));
            Assert.IsTrue(Has(evs, Ev.RoomFightChoice));
            Assert.AreEqual(Sim.Contracts[0].Layers[1][0], r.Room.Id);
        }

        [Test]
        public void Dica_PousarParaLutar_NaSalaDoGuarda()
        {
            var sim = NewSim(); Run r = Start(sim, 0, clear: false);
            Steps(sim, 3, Up);
            Assert.AreEqual(Hint.PlantToFight, r.Hint);
            Assert.AreEqual("solte o dedo para pousar e lutar", Hints.Text(r.Hint));
        }

        // ------------------------------------------------------------------ ladrao

        [Test]
        public void Ladrao_PegaACargaDoChaoEm05s_EDerrubaAoSerAtingido()
        {
            var sim = NewSim(); Run r = Start(sim, 1);
            Steps(sim, 3, Up); Steps(sim, 6, V2.Zero);   // pousa na entrada
            V2 cargo = r.Main.Pos;
            Enemy t = r.Spawn(EnemyType.Thief, cargo + new V2(2.5f, 2f));
            r.Courier.Pos = new V2(1f, 12f);   // longe, andando (nao atira)
            float grabStart = -1f, grabbed = -1f;
            for (int i = 0; i < StepsFor(5f) && grabbed < 0f; i++)
            {
                List<SimEvent> evs = Steps(sim, 1, i % 20 < 10 ? Left : Right);
                if (grabStart < 0f && t.State == EnemyState.Grab) grabStart = r.Time - Dt;
                if (Has(evs, Ev.ThiefGrab)) grabbed = r.Time;
            }
            Assert.Greater(grabbed, 0f, "o ladrao nao pegou a carga do chao");
            Assert.AreEqual(Balance.ThiefGrab, grabbed - grabStart, 0.05f, "pegar leva 0,5 s");
            Assert.AreEqual(CargoState.Stolen, r.Main.State);
            Assert.AreSame(r.Main, t.Carry);
            Assert.AreEqual(Hint.Thief, r.Hint);

            r.Courier.Pos = t.Pos + new V2(0f, 3f);   // chega perto e para: atira no ladrao
            var all = new List<SimEvent>();
            for (int i = 0; i < StepsFor(1.5f) && r.Main.State == CargoState.Stolen; i++) all.AddRange(Steps(sim, 1, V2.Zero));
            Assert.AreEqual(CargoState.Ground, r.Main.State, "atingido, o ladrao nao derrubou a carga");
            bool dropByHit = false;
            foreach (SimEvent e in all) if (e.Kind == Ev.CargoDrop && e.B == 2) dropByHit = true;
            Assert.IsTrue(dropByHit);
            Assert.IsTrue(sim.InContract);
        }

        [Test]
        public void Ladrao_DashNeleDerrubaACarga()
        {
            var sim = NewSim(); Run r = Start(sim, 1);
            Steps(sim, 3, Up); Steps(sim, 6, V2.Zero);
            Enemy t = r.Spawn(EnemyType.Thief, r.Main.Pos + new V2(0.2f, 0f));
            r.Courier.Pos = new V2(1f, 12f);
            for (int i = 0; i < StepsFor(1f) && t.Carry == null; i++) Steps(sim, 1, i % 20 < 10 ? Left : Right);
            Assert.IsNotNull(t.Carry);
            r.Courier.Pos = t.Pos - new V2(0f, 1.5f); r.Courier.DashCd = 0f;
            Steps(sim, 4, Up, true);
            Assert.AreEqual(CargoState.Ground, r.Main.State, "o dash (empurrao) nao derrubou a carga do ladrao");
        }

        [Test]
        public void Ladrao_QueEscapa_FalhaOContrato_EGeraCargaPerdida()
        {
            var sim = NewSim(); Run r = Start(sim, 1);
            Steps(sim, 3, Up); Steps(sim, 6, V2.Zero);
            int id = r.Main.Id;
            r.Spawn(EnemyType.Thief, r.Main.Pos + new V2(0.2f, 0f));
            r.Courier.Pos = new V2(1f, 12f);
            for (int i = 0; i < StepsFor(15f) && sim.InContract; i++) Steps(sim, 1, i % 20 < 10 ? Left : Right);
            Assert.IsFalse(sim.InContract);
            Assert.AreEqual(Outcome.CargoStolen, sim.LastResult.Outcome);
            Assert.AreEqual(0, sim.LastResult.Total);
            Assert.IsNotNull(sim.Pending, "carga roubada nao virou carga perdida");
            Assert.AreEqual(id, sim.Pending.Id);
            Assert.AreEqual(Content.R2, sim.Pending.Room);
        }

        [Test]
        public void Exploit_CargaSozinhaMaisDe8s_NasceLadraoPertoDela()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            Steps(sim, 3, Up); Steps(sim, 6, V2.Zero);
            V2 cargo = r.Main.Pos;
            r.Courier.Pos = new V2(4.5f, 10f);
            var evs = new List<SimEvent>();
            for (int i = 0; i < StepsFor(7.8f); i++) evs.AddRange(Steps(sim, 1, i % 20 < 10 ? Left : Right));
            Assert.IsFalse(Has(evs, Ev.ThiefSpawn), "ladrao antes de 8 s");
            for (int i = 0; i < StepsFor(0.4f); i++) evs.AddRange(Steps(sim, 1, i % 20 < 10 ? Left : Right));
            Assert.IsTrue(Has(evs, Ev.ThiefSpawn), "carga sozinha 8 s sem ladrao");
            Enemy t = r.Enemies.Find(e => e.Type == EnemyType.Thief);
            Assert.IsNotNull(t);
            Assert.LessOrEqual(V2.Dist(t.Pos, cargo), Balance.ThiefSpawnDist + 0.6f, "o ladrao nasce perto da carga");
        }

        [Test]
        public void CargaPertoDoCourier_NaoGeraLadrao()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            Steps(sim, 3, Up); Steps(sim, 6, V2.Zero);
            List<SimEvent> evs = Steps(sim, StepsFor(10f), V2.Zero);   // parado ao lado dela
            Assert.IsFalse(Has(evs, Ev.ThiefSpawn));
        }

        // ------------------------------------------------------------------ integridade

        [Test]
        public void Reliquia_RachaComGolpeNoChao_EQuebraCom3()
        {
            var sim = NewSim(); Run r = Start(sim, 4);   // C5: reliquia
            Assert.AreEqual(CargoType.Relic, r.Main.Type);
            r.Courier.Hp = 99;
            Steps(sim, 3, Up); Steps(sim, 6, V2.Zero);
            Enemy b = r.Spawn(EnemyType.Brute, r.Main.Pos + new V2(0f, 1f));
            b.Hp = 999;
            var evs = new List<SimEvent>();
            for (int i = 0; i < StepsFor(10f) && sim.InContract; i++) evs.AddRange(Steps(sim, 1, V2.Zero));
            int cracks = 0; foreach (SimEvent e in evs) if (e.Kind == Ev.CargoIntegrity) cracks++;
            Assert.AreEqual(3, cracks);
            Assert.IsTrue(Has(evs, Ev.CargoBroken));
            Assert.AreEqual(Outcome.CargoBroken, sim.LastResult.Outcome);
            Assert.IsNull(sim.Pending, "reliquia quebrada nao vira carga perdida");
        }

        [Test]
        public void Caixa_RachadaNaoFalha_SoReduzOBonus()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            r.Courier.Hp = 99;
            Steps(sim, 3, Up); Steps(sim, 6, V2.Zero);
            Enemy b = r.Spawn(EnemyType.Brute, r.Main.Pos + new V2(0f, 1f));
            b.Hp = 999;
            for (int i = 0; i < StepsFor(10f); i++) Steps(sim, 1, V2.Zero);
            Assert.IsTrue(sim.InContract);
            Assert.GreaterOrEqual(r.Main.Cracks, 3);
            Assert.AreEqual(1.0f, Balance.IntegrityMul(r.Main.Cracks));
            Assert.AreEqual(1.5f, Balance.IntegrityMul(0)); Assert.AreEqual(1.2f, Balance.IntegrityMul(1)); Assert.AreEqual(1.0f, Balance.IntegrityMul(2));
        }

        [Test]
        public void Bruto_GolpePelasCostasComCargaNasMaos_RachaACarga_DeLadoNao()
        {
            var sim = NewSim(); Run r = Start(sim, 0);   // Caixa (a reliquia racha com qualquer golpe: teste proprio)
            r.Courier.Hp = 99;
            r.Courier.Pos = new V2(2.0f, 5.4f);
            Steps(sim, 2, Up);   // empurrando o pilar da R1 (y 5,9): anda sem sair do lugar, de frente para cima
            Assert.AreEqual(5.5f, r.Courier.Pos.Y, 0.01f);
            Enemy b = r.Spawn(EnemyType.Brute, new V2(3.1f, 5.6f));   // de lado (um pouco a frente)
            b.Hp = 999;
            for (int i = 0; i < StepsFor(0.8f); i++) Steps(sim, 1, Up);
            Assert.Less(r.Courier.Hp, 99, "o golpe de lado devia acertar o courier");
            Assert.AreEqual(0, r.Main.Cracks, "golpe de lado nao acerta a carga nas maos");
            r.Enemies.Clear();
            b = r.Spawn(EnemyType.Brute, new V2(2.0f, 4.4f));   // atras
            b.Hp = 999;
            for (int i = 0; i < StepsFor(0.8f); i++) Steps(sim, 1, Up);
            Assert.AreEqual(CargoState.Held, r.Main.State);
            Assert.AreEqual(1, r.Main.Cracks, "golpe pelas costas nao acertou a carga nas maos");
        }

        [Test]
        public void Reliquia_RachaAoCair_DashELadrao_PousarNao()
        {
            var sim = NewSim(); Run r = Start(sim, 4);   // C5: reliquia
            r.Courier.Hp = 99;
            Steps(sim, 3, Up);
            Steps(sim, 1, Up, true);   // dash larga a reliquia
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            Assert.AreEqual(1, r.Main.Cracks, "a reliquia nao rachou ao cair do dash");
            r.Courier.Pos = r.Main.Pos + new V2(0.5f, 0f); Steps(sim, 1, Up);   // pega
            Steps(sim, 6, V2.Zero);   // pousa
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            Assert.AreEqual(1, r.Main.Cracks, "pousar nao pode rachar");

            Enemy t = r.Spawn(EnemyType.Thief, r.Main.Pos + new V2(0.2f, 0f));
            r.Courier.Pos = new V2(1f, 12f);
            for (int i = 0; i < StepsFor(1f) && t.Carry == null; i++) Steps(sim, 1, i % 20 < 10 ? Left : Right);
            Assert.AreEqual(CargoState.Stolen, r.Main.State);
            Assert.AreEqual(1, r.Main.Cracks, "o ladrao pegar nao racha");
            r.Courier.Pos = t.Pos + new V2(0f, 3f);
            for (int i = 0; i < StepsFor(1.5f) && r.Main.State == CargoState.Stolen; i++) Steps(sim, 1, V2.Zero);   // atira: ele derruba
            Assert.AreEqual(CargoState.Ground, r.Main.State);
            Assert.AreEqual(2, r.Main.Cracks, "a reliquia nao rachou ao cair do ladrao atingido");

            var sim2 = NewSim(); Run r2 = Start(sim2, 4);   // empurrao do dash no ladrao que leva a reliquia
            Steps(sim2, 3, Up); Steps(sim2, 6, V2.Zero);
            Enemy t2 = r2.Spawn(EnemyType.Thief, r2.Main.Pos + new V2(0.2f, 0f));
            r2.Courier.Pos = new V2(1f, 12f);
            for (int i = 0; i < StepsFor(1f) && t2.Carry == null; i++) Steps(sim2, 1, i % 20 < 10 ? Left : Right);
            r2.Courier.Pos = t2.Pos - new V2(0f, 1.5f); r2.Courier.DashCd = 0f;
            Steps(sim2, 4, Up, true);
            Assert.AreEqual(CargoState.Ground, r2.Main.State);
            Assert.AreEqual(1, r2.Main.Cracks, "a reliquia nao rachou ao cair do ladrao empurrado");

            var box = NewSim(); Run rb = Start(box, 0);   // Caixa: cair nao racha
            Steps(box, 3, Up); Steps(box, 1, Up, true);
            Assert.AreEqual(0, rb.Main.Cracks);
        }

        [Test]
        public void Reliquia_GolpeNoCourierQueACarrega_Racha_IncluiMorcego()
        {
            var sim = NewSim(); Run r = Start(sim, 4);
            r.Courier.Hp = 99; r.Courier.Pos = new V2(4.5f, 3f);
            r.Spawn(EnemyType.Bat, r.Courier.Pos);
            List<SimEvent> evs = Steps(sim, 2, Left);   // andando com a reliquia nas maos: o morcego encosta
            Assert.IsTrue(Has(evs, Ev.DamageTaken));
            Assert.AreEqual(CargoState.Held, r.Main.State);
            Assert.AreEqual(1, r.Main.Cracks, "toque de morcego no courier nao rachou a reliquia nas maos");

            var box = NewSim(); Run rb = Start(box, 0);   // Caixa nas maos: o golpe nao racha
            rb.Courier.Hp = 99; rb.Courier.Pos = new V2(4.5f, 3f);
            rb.Spawn(EnemyType.Bat, rb.Courier.Pos);
            Assert.IsTrue(Has(Steps(box, 2, Left), Ev.DamageTaken));
            Assert.AreEqual(0, rb.Main.Cracks);

            var back = NewSim(); Run rk = Start(back, 4);   // Bruto pelas costas com a reliquia: 1 rachadura por golpe, nao 2
            rk.Courier.Hp = 99; rk.Courier.Pos = new V2(2.0f, 5.4f);
            Steps(back, 2, Up);
            Enemy b = rk.Spawn(EnemyType.Brute, new V2(2.0f, 4.4f)); b.Hp = 999;
            int strikes = 0;
            for (int i = 0; i < StepsFor(0.8f); i++) foreach (SimEvent e in Steps(back, 1, Up)) if (e.Kind == Ev.Strike) strikes++;
            Assert.AreEqual(1, strikes);
            Assert.AreEqual(1, rk.Main.Cracks, "golpe pelas costas rachou a reliquia duas vezes");
        }

        // ------------------------------------------------------------------ ovo

        [Test]
        public void Ovo_ChocaNoTempoAlvo_FilhoteSegue_EPortaEspera()
        {
            var sim = NewSim(); Run r = Start(sim, 2);   // C3: ovo
            float target = Sim.Contracts[2].EggTarget;
            Assert.Greater(target, 0f);
            r.Time = target - 0.1f;
            List<SimEvent> evs = Steps(sim, 2, Up);
            Assert.IsFalse(Has(evs, Ev.Hatch), "chocou antes do tempo-alvo");
            evs = Steps(sim, 2, Up);
            Assert.IsTrue(Has(evs, Ev.Hatch));
            Assert.IsTrue(r.Hatched);
            Assert.IsNotNull(r.Pet);
            Assert.AreEqual(Balance.PetHp, r.Pet.Hp);
            Assert.AreEqual(CargoState.Gone, r.Main.State);

            r.Courier.Pos = new V2(r.Doors[0].Pos.X, 13.4f);   // o filhote ficou la embaixo
            Steps(sim, 1, Up);
            Assert.AreEqual(0, r.Layer, "porta abriu sem o filhote perto");
            Assert.AreEqual(Hint.WaitPet, r.Hint);
            for (int i = 0; i < StepsFor(4f) && r.Layer == 0; i++) Steps(sim, 1, Up);   // espera encostado na porta
            Assert.AreEqual(1, r.Layer, "o filhote nao seguiu o courier ate a porta");
        }

        [Test]
        public void Ovo_FilhoteMorto_FalhaOContrato()
        {
            var sim = NewSim(); Run r = Start(sim, 2);
            r.Time = Sim.Contracts[2].EggTarget;
            Steps(sim, 1, Up);
            Assert.IsNotNull(r.Pet);
            r.Courier.Hp = 99; r.Pet.Hp = 1;
            r.Courier.Pos = new V2(4.5f, 6f); r.Pet.Pos = new V2(4.5f, 4f);
            r.Spawn(EnemyType.Bat, r.Pet.Pos);
            Steps(sim, 2, V2.Zero);
            Assert.IsFalse(sim.InContract);
            Assert.AreEqual(Outcome.PetDied, sim.LastResult.Outcome);
            Assert.IsNull(sim.Pending);
        }

        [Test]
        public void Ovo_Chocado_PagaMenos()
        {
            ContractResult fresh = Deliver(NewSim(), 2);
            Assert.IsFalse(fresh.Hatched);
            Assert.AreEqual(90, fresh.Payment, "ovo: 50 x 1,2 x 1,5");

            var sim = NewSim(); Run r = Start(sim, 2);
            r.Time = Sim.Contracts[2].EggTarget;
            Steps(sim, 1, Up);
            Assert.IsTrue(r.Hatched);
            ContractResult hatched = DeliverFrom(sim);
            Assert.AreEqual(Outcome.Delivered, hatched.Outcome);
            Assert.IsTrue(hatched.Hatched);
            Assert.AreEqual(60, hatched.Payment, "chocado: 50 x 0,8 x 1,5");
        }

        // ------------------------------------------------------------------ pagamento, guilda

        [Test]
        public void Pagamento_PorCargaIntegridadeEBolsa()
        {
            ContractResult c1 = Deliver(NewSim(), 0);
            Assert.AreEqual(Outcome.Delivered, c1.Outcome);
            Assert.AreEqual(60, c1.Payment, "C1: 40 x 1,0 x 1,5");
            var bag = NewSim(); bag.Levels[(int)Upgrade.Bag] = 2;
            Assert.AreEqual(72, Deliver(bag, 0).Payment, "Bolsa 2: +20%");
            var relic = NewSim(); Run r = Start(relic, 4);
            r.Main.Cracks = 1;
            ContractResult c5 = DeliverFrom(relic);
            Assert.AreEqual(109, c5.Payment, "C5: 65 x 1,4 x 1,2");
            Assert.AreEqual(137, Deliver(NewSim(), 4).Payment, "C5 perfeita: 65 x 1,4 x 1,5 = 136,5 -> 137 (sem erro de float)");
            var bag3 = NewSim(); bag3.Levels[(int)Upgrade.Bag] = 3;
            Assert.AreEqual(88, Deliver(bag3, 1).Payment, "C2 com Bolsa 3: 45 x 1,5 x 1,3 = 87,75 -> 88 (um arredondamento so)");
        }

        [Test]
        public void Reputacao_AbreOProximoContrato()
        {
            var sim = new Sim(3);
            Assert.IsTrue(sim.Unlocked(0)); Assert.IsFalse(sim.Unlocked(1));
            Assert.IsNull(sim.StartContract(1, false), "contrato bloqueado comecou");
            Deliver(sim, 0);
            Assert.AreEqual(1, sim.Reputation); Assert.IsTrue(sim.Unlocked(1));
            Deliver(sim, 0);   // repetir nao sobe a reputacao
            Assert.AreEqual(1, sim.Reputation);
        }

        [Test]
        public void Guilda_CustoDosUpgrades_ECompra()
        {
            int[] expected = { 60, 95, 155, 245, 395 };
            for (int i = 0; i < 5; i++) Assert.AreEqual(expected[i], Balance.UpgradeCost(i));
            var sim = new Sim(1) { Coins = 5000 };
            Assert.IsFalse(sim.Buy(Upgrade.Life), "guilda abre so depois do C1");
            sim.Reputation = 1;
            Assert.IsTrue(sim.Buy(Upgrade.Life));
            Assert.AreEqual(4940, sim.Coins);
            Assert.AreEqual(Balance.BaseHp + 1, sim.MaxHp);
            Assert.IsTrue(Has(sim.Events, Ev.GuildUpgrade)); Assert.IsTrue(Has(sim.Events, Ev.CurrencySink));
            for (int i = 0; i < 4; i++) Assert.IsTrue(sim.Buy(Upgrade.Life));
            Assert.AreEqual(-1, sim.UpgradeCost(Upgrade.Life));
            Assert.IsFalse(sim.Buy(Upgrade.Life), "passou do nivel 5");
            Assert.AreEqual(5000 - 950, sim.Coins);
            var poor = new Sim(1) { Coins = 59, Reputation = 1 };
            Assert.IsFalse(poor.Buy(Upgrade.Bag));
            Assert.AreEqual(59, poor.Coins);
        }

        // ------------------------------------------------------------------ carga perdida e resgate

        static Sim DieInC1(int seed = 7)
        {
            var sim = NewSim(seed); Run r = Start(sim, 0);
            r.Courier.Hp = 1;
            r.Spawn(EnemyType.Bat, r.Courier.Pos);
            Steps(sim, 2, V2.Zero);
            Assert.AreEqual(Outcome.Died, sim.LastResult.Outcome);
            return sim;
        }

        [Test]
        public void Morte_DeixaACargaPendente()
        {
            Sim sim = DieInC1();
            Assert.IsNotNull(sim.Pending);
            Assert.AreEqual(sim.Run.Main.Id, sim.Pending.Id);
            Assert.AreEqual(CargoType.Box, sim.Pending.Type);
            Assert.AreEqual(Content.R1, sim.Pending.Room);
            Assert.AreEqual(40, sim.Pending.Value);
            Assert.AreEqual(Content.R1, sim.LastResult.DeathRoom);
        }

        [Test]
        public void Resgate_AparaceNa1aSala_EntregarAsDuasPagaBonusUmaVez()
        {
            Sim sim = DieInC1();
            LostCargo lost = sim.Pending;
            Run r = sim.StartContract(0, true);
            Assert.IsNull(sim.Pending, "a oferta expira ao comecar o contrato");
            Assert.IsTrue(Has(sim.Events, Ev.CargoRescueOffer));
            Assert.IsNotNull(r.RescueCargo);
            Assert.AreEqual(lost.Id, r.RescueCargo.Id);
            Assert.AreEqual(CargoState.Ground, r.RescueCargo.State);
            Assert.AreEqual(r.Room.RescueSpot.X, r.RescueCargo.Pos.X, 0.01f);
            sim.Abandon();

            Sim s2 = DieInC1(8);
            LostCargo l2 = s2.Pending;
            ContractResult res = Deliver(s2, 0, rescue: true);
            Assert.IsTrue(res.RescueDelivered);
            Assert.AreEqual(20, res.RescueBonus, "+50% do pagamento da perdida");
            Assert.Less(res.RescueBonus, l2.Value, "bonus tem de ser menor que o pagamento perdido (§28)");
            Assert.IsTrue(s2.RescuedIds.Contains(l2.Id));

            s2.Pending = new LostCargo { Id = l2.Id, Type = l2.Type, Value = l2.Value, Room = 0 };   // mesma carga de novo
            Run again = s2.StartContract(0, true);
            Assert.IsNull(again.RescueCargo, "bonus de resgate saiu duas vezes para o mesmo id");
            s2.Abandon();
        }

        [Test]
        public void Resgate_RecusadoExpiraApos1Contrato_ESo1Pendente()
        {
            Sim sim = DieInC1();
            int first = sim.Pending.Id;
            Run r = sim.StartContract(0, false);
            Assert.IsNull(sim.Pending);
            Assert.IsNull(r.RescueCargo);
            r.Enemies.Clear();
            r.Courier.Hp = 1; r.Spawn(EnemyType.Bat, r.Courier.Pos);
            Steps(sim, 2, V2.Zero);   // morre de novo
            Assert.IsNotNull(sim.Pending);
            Assert.AreNotEqual(first, sim.Pending.Id, "so 1 pendente: a mais nova");
            Deliver(sim, 0);   // recusa o resgate e entrega
            Assert.IsNull(sim.Pending, "o resgate nao expirou depois de 1 contrato");
        }

        [Test]
        public void Exploit_MorrerDePropositoNaoCompensa()
        {
            // morrer no C1 e resgatar paga o bonus (20) + o C1 (60) = 80 em 2 contratos; entregar 2x paga 120
            Sim dead = DieInC1();
            ContractResult a = Deliver(dead, 0, rescue: true);
            int viaDeath = a.Payment + a.RescueBonus;
            Sim clean = NewSim();
            int viaDelivery = Deliver(clean, 0).Payment + Deliver(clean, 0).Payment;
            Assert.Less(viaDeath, viaDelivery);
        }

        // ------------------------------------------------------------------ save

        [Test]
        public void Save_IdaEVolta()
        {
            var sim = new Sim(77) { Coins = 321, Reputation = 4, RunsStarted = 9, NextCargoId = 12 };
            sim.Levels[0] = 2; sim.Levels[1] = 1; sim.Levels[2] = 5;
            sim.Pending = new LostCargo { Id = 11, Type = CargoType.Relic, Cracks = 2, Room = Content.R5, Value = 91, Contract = 4 };
            sim.RescuedIds.Add(3); sim.RescuedIds.Add(8);
            string a = sim.Save();
            StringAssert.StartsWith("v=1\n", a);
            Assert.IsFalse(sim.Dirty);
            Sim b = Sim.Load(a);
            Assert.AreEqual(321, b.Coins); Assert.AreEqual(4, b.Reputation); Assert.AreEqual(77, b.Seed);
            Assert.AreEqual(new[] { 2, 1, 5 }, b.Levels);
            Assert.AreEqual(11, b.Pending.Id); Assert.AreEqual(CargoType.Relic, b.Pending.Type); Assert.AreEqual(2, b.Pending.Cracks); Assert.AreEqual(91, b.Pending.Value);
            Assert.IsTrue(b.RescuedIds.Contains(3) && b.RescuedIds.Contains(8));
            Assert.AreEqual(a, b.Save());
        }

        [Test]
        public void Save_ContratoEmCurso_IdaEVolta()
        {
            var sim = NewSim(); Run r = Start(sim, 4, clear: false);
            Steps(sim, 20, Up);
            r.Main.Cracks = 2;
            string a = sim.Save();
            Sim b = Sim.Load(a);
            Assert.IsTrue(b.InContract);
            Assert.AreEqual(2, b.Run.Main.Cracks);
            Assert.AreEqual(4, b.Run.Contract.Index);
            Assert.AreEqual(a, b.Save());
        }

        [Test]
        public void Save_MigraV0()
        {
            Sim s = Sim.Load("coins=120\nlv=1,0,2\nrep=3\n");
            Assert.IsFalse(s.SaveRefused);
            Assert.AreEqual(120, s.Coins); Assert.AreEqual(3, s.Reputation);
            Assert.AreEqual(new[] { 1, 0, 2 }, s.Levels);
            Assert.IsTrue(s.Dirty, "migracao tem de regravar em v1");
            StringAssert.StartsWith("v=1\n", s.Save());
        }

        [Test]
        public void Save_VersaoFuturaRecusada_SemApagar()
        {
            const string future = "v=2\ncoins=999\nrep=8\nlv=5,5,5\n";
            Sim s = Sim.Load(future);
            Assert.IsTrue(s.SaveRefused);
            Assert.AreEqual(0, s.Coins, "devolve estado novo");
            Assert.IsNull(s.Save(), "nao pode gravar por cima do save futuro");
            Assert.IsFalse(s.Dirty);
            Assert.IsNull(s.StartContract(0, false));
            s.Coins = 999; s.Reputation = 1;
            Assert.IsFalse(s.Buy(Upgrade.Life));
        }

        [Test]
        public void Save_LixoViraEstadoValido()
        {
            Sim s = Sim.Load("v=1\ncoins=-50\nrep=999\nlv=9,x,-3\nrun=7,9,9,abc\npend=x\n");
            Assert.AreEqual(0, s.Coins); Assert.AreEqual(Sim.Contracts.Length, s.Reputation);
            Assert.AreEqual(new[] { Balance.UpgradeMax, 0, 0 }, s.Levels);
            Assert.IsNull(s.Run); Assert.IsNull(s.Pending);
        }

        [Test]
        public void Exploit_FecharOAppDepoisDoDano_RetomaNaEntradaSemRestaurar()
        {
            var sim = NewSim(); Run r = Start(sim, 0, clear: false);   // R1: Guarda
            sim.Save();
            r.Courier.Pos = new V2(4.5f, 11.2f);
            string saved = null; int hpSaved = 0;
            for (int i = 0; i < StepsFor(5f) && saved == null; i++)
            {
                List<SimEvent> evs = Steps(sim, 1, i % 2 == 0 ? Left : Right);
                if (Has(evs, Ev.DamageTaken)) { Assert.IsTrue(sim.Dirty, "dano tem de pedir save"); hpSaved = r.Courier.Hp; saved = sim.Save(); }
            }
            Assert.IsNotNull(saved, "o Guarda nao acertou");
            Assert.Less(hpSaved, sim.MaxHp);
            for (int i = 0; i < StepsFor(3f); i++) Steps(sim, 1, i % 2 == 0 ? Left : Right);   // mais dano depois do save (perdido ao fechar)

            Sim b = Sim.Load(saved);
            Assert.IsTrue(b.InContract);
            Assert.AreEqual(hpSaved, b.Run.Courier.Hp, "reabrir restaurou a vida");
            Assert.AreEqual(Balance.Entrance.X, b.Run.Courier.Pos.X, 0.01f); Assert.AreEqual(Balance.Entrance.Y, b.Run.Courier.Pos.Y, 0.01f);
            Assert.AreEqual(Content.R1, b.Run.Room.Id);
            Assert.AreEqual(CargoState.Held, b.Run.Main.State);
            Assert.AreEqual(EnemyType.Guard, b.Run.Enemies[0].Type);
            Assert.AreEqual(5, b.Run.Enemies[0].Hp, "a sala volta inteira (inimigos de novo)");
        }

        [Test]
        public void SaveACadaSala_RetomaNaSalaNova()
        {
            var sim = NewSim(); Run r = Start(sim, 0);
            sim.Save();
            Assert.IsFalse(sim.Dirty);
            r.Courier.Pos = new V2(r.Doors[1].Pos.X, 13.4f);
            Steps(sim, 4, Up);
            Assert.AreEqual(1, r.Layer);
            Assert.IsTrue(sim.Dirty, "trocar de sala tem de pedir save");
            Sim b = Sim.Load(sim.Save());
            Assert.AreEqual(1, b.Run.Layer);
            Assert.AreEqual(Sim.Contracts[0].Layers[1][1], b.Run.Room.Id);
        }

        [Test]
        public void Exploit_BauNaoPagaDuasVezesAoReabrir()
        {
            var sim = NewSim(); Run r = Start(sim, 3);   // C4 comeca na R5 (bau)
            Assert.IsNotNull(r.Chest);
            r.Courier.Hp = 99;
            Steps(sim, 1, Up);   // o pouso (e a espera no bau) so conta depois do 1o movimento na sala
            r.Courier.Pos = r.Chest.Pos + new V2(0.6f, 0f);
            int before = sim.Coins;
            List<SimEvent> evs = Steps(sim, StepsFor(1.4f), V2.Zero);
            Assert.IsTrue(r.Chest.Opened);
            Assert.IsTrue(Has(evs, Ev.OptionalLoot));
            Assert.AreEqual(before + r.Chest.Coins, sim.Coins);
            Sim b = Sim.Load(sim.Save());
            Assert.IsTrue(b.Run.Chest.Opened);
            b.Run.Enemies.Clear();
            Steps(b, 1, Up);   // sem este passo o teste passaria por construcao (dedo solto desde a entrada nao abre nada)
            b.Run.Courier.Pos = b.Run.Chest.Pos + new V2(0.6f, 0f);
            int coins = b.Coins;
            Steps(b, StepsFor(2f), V2.Zero);
            Assert.AreEqual(coins, b.Coins);
        }

        [Test]
        public void Resgate_SobreviveAoSave()
        {
            Sim sim = DieInC1();
            Run r = sim.StartContract(1, true);
            Sim b = Sim.Load(sim.Save());
            Assert.IsNotNull(b.Run.RescueCargo);
            Assert.AreEqual(CargoState.Ground, b.Run.RescueCargo.State);
            Assert.IsNull(b.Pending, "reabrir nao pode devolver a oferta");
            Assert.AreEqual(r.RescueValue, b.Run.RescueValue);
        }

        // ------------------------------------------------------------------ determinismo e passo fixo

        static string Fingerprint(int seed, int contract)
        {
            var sim = new Sim(seed) { Reputation = Sim.Contracts.Length };
            ContractResult res = Bot.Play(sim, contract, false, new Bot(seed, 0.6f, 0.25f));
            return res.Outcome + "|" + res.Time.ToString("R") + "|" + res.Total + "|" + res.RoomsFought + "|" + sim.Run.Courier.Pos + "|" + sim.Save();
        }

        [Test]
        public void Determinismo_MesmaSementeMesmoResultado()
        {
            for (int c = 0; c < Sim.Contracts.Length; c++) Assert.AreEqual(Fingerprint(42, c), Fingerprint(42, c), "C" + (c + 1));
            var set = new HashSet<string>();
            for (int s = 0; s < 6; s++) set.Add(Fingerprint(100 + s, 3));
            Assert.Greater(set.Count, 1, "sementes diferentes deviam variar a partida");
        }

        [Test]
        public void PassoFixo_DtDoQuadroNaoMudaASimulacao()
        {
            // 1/30 por Tick x 1/60 + 1/60: mesmo estado depois de 2 s
            var a = NewSim(); Run ra = Start(a, 1, clear: false);
            var b = NewSim(); Run rb = Start(b, 1, clear: false);
            for (int i = 0; i < 60; i++) a.Tick(Dt, Up, false);
            for (int i = 0; i < 120; i++) b.Tick(Dt * 0.5f, Up, false);
            Assert.AreEqual(ra.Time, rb.Time, 1e-3f);
            Assert.AreEqual(ra.Courier.Pos.Y, rb.Courier.Pos.Y, 1e-3f);
        }

        // ------------------------------------------------------------------ desenho das salas (medido)

        [Test]
        public void LevelDesign_SpawnsLongeDaEntrada_ForaDeObstaculos_FaixaDasPortasLivre()
        {
            foreach (RoomDef room in Content.Rooms)
            {
                foreach (Spawn s in room.Spawns)
                {
                    Assert.GreaterOrEqual(V2.Dist(s.Pos, Balance.Entrance), 4f, room.Code + ": spawn em cima da entrada");
                    Assert.IsFalse(Run.Blocked(room, s.Pos, EnemyDef.All[(int)s.Type].Radius), room.Code + ": spawn dentro de obstaculo");
                }
                foreach (Rect o in room.Obstacles)
                {
                    Assert.LessOrEqual(o.C.Y + o.H.Y, 12.2f, room.Code + ": obstaculo na faixa das portas");
                    Assert.IsFalse(Run.Blocked(room, Balance.Entrance, 1.2f), room.Code + ": obstaculo na entrada");
                }
                Assert.IsFalse(Run.Blocked(room, room.RescueSpot, Balance.CargoR), room.Code + ": resgate dentro de obstaculo");
                if (room.ChestCoins > 0) Assert.IsFalse(Run.Blocked(room, room.ChestPos, 0.3f), room.Code + ": bau dentro de obstaculo");
                var nav = new Nav(room);
                for (int n = 1; n <= 3; n++)
                    for (int i = 0; i < n; i++)
                        Assert.Less(nav.Steps(Balance.Entrance, new V2(Balance.DoorX(n, i), 13.5f)), int.MaxValue, room.Code + ": porta inalcancavel");
                Assert.Less(nav.Steps(Balance.Entrance, room.RescueSpot), int.MaxValue, room.Code + ": resgate inalcancavel");
                if (room.ChestCoins > 0) Assert.Less(nav.Steps(Balance.Entrance, room.ChestPos), int.MaxValue, room.Code + ": bau inalcancavel");
            }
        }

        /// <summary>Anda de verdade (PathDir + colisao) por todos os caminhos de todos os contratos, passando pelo bau quando ha, ate a
        /// entrega: nenhuma porta, bau ou cliente fica preso atras de obstaculo.</summary>
        [Test]
        public void LevelDesign_TodosOsCaminhosSaoAndaveis()
        {
            for (int c = 0; c < Sim.Contracts.Length; c++)
            {
                int[][] layers = Sim.Contracts[c].Layers;
                int paths = 1;
                for (int l = 1; l < layers.Length; l++) paths *= layers[l].Length;
                for (int p = 0; p < paths; p++)
                {
                    var sim = NewSim(); Run r = Start(sim, c);
                    int code = p;
                    while (sim.InContract)
                    {
                        r.Enemies.Clear(); r.Courier.Hp = 99;
                        int layer = r.Layer;
                        if (r.Chest != null) WalkTo(sim, r.Chest.Pos, 1.0f, "C" + (c + 1) + " " + r.Room.Code + " bau");
                        if (r.Room.Delivery) { WalkTo(sim, Balance.Client, 0.5f, "C" + (c + 1) + " cliente"); break; }
                        int door = code % r.Doors.Count; code /= r.Doors.Count;
                        V2 target = new V2(r.Doors[door].Pos.X, Balance.RoomH);
                        for (int i = 0; i < StepsFor(15f) && r.Layer == layer; i++) Steps(sim, 1, r.PathDir(r.Courier.Pos, target));
                        Assert.Greater(r.Layer, layer, "C" + (c + 1) + " " + r.Room.Code + " porta " + door + " nao alcancada");
                    }
                    Assert.AreEqual(Outcome.Delivered, sim.LastResult.Outcome, "C" + (c + 1) + " caminho " + p);
                }
            }
        }

        static void WalkTo(Sim sim, V2 target, float within, string what)
        {
            Run r = sim.Run;
            for (int i = 0; i < StepsFor(15f) && sim.InContract && V2.Dist(r.Courier.Pos, target) > within; i++) Steps(sim, 1, r.PathDir(r.Courier.Pos, target));
            Assert.IsTrue(!sim.InContract || V2.Dist(r.Courier.Pos, target) <= within, what + " inalcancavel");
        }
    }
}
