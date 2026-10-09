using System;

namespace DC.Core
{
    /// <summary>Jogador heuristico deterministico (semente) que joga contratos inteiros no Core sem render. Aggression (0..1) = chance
    /// de escolher lutar numa sala e de aceitar porta perigosa; Greed (0..1) = chance de ir ao bau e de preferir a porta com bau;
    /// Reaction = handicap humano: o plano (para onde ir, parar, dash) so e refeito a cada ~Reaction s (+-0,08 s); entre um plano e
    /// outro o bot so segue o caminho. ponytail: heuristica de regras, nao busca; serve para medir, nao para jogar bem.</summary>
    public sealed class Bot
    {
        public readonly float Aggression, Reaction, Greed;
        readonly Rng rng;
        int wait, roomKey = -1, door;
        bool fightRoom, wantChest, stop;
        float roomStart;
        V2 goal;

        public Bot(int seed, float aggression = 0.5f, float reaction = 0.25f, float greed = 0.5f)
        {
            rng = new Rng(Rng.Hash(seed, 7919));
            Aggression = aggression; Reaction = reaction; Greed = greed;
        }

        /// <summary>Joga um passo fixo: replaneja se passou o tempo de reacao e chama sim.Tick(Balance.Step, move, dash).</summary>
        public void Act(Sim sim)
        {
            Run r = sim.Run;
            if (r == null || !r.Running) return;
            int key = r.Layer * 16 + r.Room.Id;
            if (key != roomKey) { roomKey = key; roomStart = r.Time; OnRoom(r); wait = 0; }
            if (r.Time - roomStart > 40f) { fightRoom = false; wantChest = false; }   // valvula: nada de ficar preso numa sala
            bool dash = false;
            if (--wait <= 0)
            {
                dash = Plan(r);
                wait = Math.Max(1, (int)Math.Round((Reaction + rng.Range(-0.08f, 0.08f)) / Balance.Step));
            }
            V2 move = stop ? V2.Zero : r.PathDir(r.Courier.Pos, goal);
            if (dash) move = (goal - r.Courier.Pos).Norm;
            sim.Tick(Balance.Step, move, dash);
        }

        void OnRoom(Run r)
        {
            bool thief = false;
            foreach (Enemy e in r.Enemies) if (e.Type == EnemyType.Thief) thief = true;
            fightRoom = rng.Float() < Aggression * (thief ? 0.7f : 1f);
            wantChest = r.Chest != null && !r.Chest.Opened && rng.Float() < Greed;
            door = 0; float best = float.MinValue;
            foreach (Door d in r.Doors)
            {
                float s = -d.Threat * (1.2f - Aggression) + (d.ChestCoins > 0 ? Greed * 1.5f : 0f) + rng.Range(0f, 1.2f);
                if (s > best) { best = s; door = d.Index; }
            }
        }

        static Enemy Nearest(Run r, V2 p, bool thievesOnly = false)
        {
            Enemy best = null; float bd = float.MaxValue;
            foreach (Enemy e in r.Enemies)
            {
                if (!e.Alive || (thievesOnly && e.Type != EnemyType.Thief)) continue;
                float d = V2.Dist(e.Pos, p);
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        void Go(V2 to) { stop = false; goal = to; }

        /// <summary>Melhor ponto a 3 m entre 8 direcoes: longe da ameaca e longe das paredes (nao se encurralar).</summary>
        static V2 Escape(Run r, V2 from, V2 threat)
        {
            V2 best = from; float bs = float.MinValue;
            foreach (V2 dir in V2.Dirs8)
            {
                V2 p = Run.FreeNear(r.Room, from + dir * 3f, Balance.CourierR);
                float wall = Math.Min(Math.Min(p.X, Balance.RoomW - p.X), Math.Min(p.Y, Balance.RoomH - p.Y));
                float sc = V2.Dist(p, threat) + 0.6f * Math.Min(wall, 2f);
                if (sc > bs) { bs = sc; best = p; }
            }
            return best;
        }

        /// <summary>Decide o plano; devolve true para dash (na direcao de `goal`).</summary>
        bool Plan(Run r)
        {
            Courier c = r.Courier;
            if (c.State == CourierState.Dashing) return false;
            Enemy near = Nearest(r, c.Pos);
            float nearD = near != null ? V2.Dist(near.Pos, c.Pos) : float.MaxValue;
            Enemy locker = null;
            foreach (Enemy e in r.Enemies) if (e.Alive && e.Def.Locks) locker = e;

            // 1) golpe telegrafado que pega o courier: sair (dash se esta em cima da hora)
            foreach (Enemy e in r.Enemies)
            {
                if (!e.Alive || e.State != EnemyState.Telegraph || V2.Dist(e.Pos, c.Pos) - Balance.CourierR > e.Def.Reach + 0.4f) continue;
                Go(Escape(r, c.Pos, e.Pos));
                return e.Tele < 0.3f && c.DashReady;
            }

            // 2) ladrao com uma carga nossa: alcancar (dash empurra = derruba) ou parar e atirar
            foreach (Enemy e in r.Enemies)
            {
                if (!e.Alive || e.Carry == null) continue;
                float d = V2.Dist(e.Pos, c.Pos);
                Go(e.Pos);
                if (c.DashReady && d <= 3.2f && d > 0.6f) return true;
                if (d <= Balance.ShotRange - 1f && near == e) stop = true;
                return false;
            }

            // 3) carga nossa no chao: lutar parado perto dela enquanto houver inimigo perto; senao, ir pegar
            Cargo ground = null;
            foreach (Cargo g in r.Cargos) if (g.State == CargoState.Ground) { ground = g; break; }
            if (ground != null)
            {
                bool atChest = wantChest && r.Chest != null && !r.Chest.Opened && V2.Dist(c.Pos, r.Chest.Pos) <= Balance.ChestRange;
                if (atChest && c.State == CourierState.Planted && Nearest(r, ground.Pos, true) == null) { stop = true; return false; }
                bool fight = locker != null || fightRoom || Nearest(r, ground.Pos, true) != null;
                if (c.State == CourierState.Planted && fight && nearD <= Balance.ShotRange && V2.Dist(c.Pos, ground.Pos) <= Balance.PickupRange) { stop = true; return false; }
                Go(ground.Pos);
                return false;
            }

            // 4) luta obrigatoria: parar a ~3 m do Guarda/Ogro (fora do golpe, dentro do tiro)
            if (locker != null)
            {
                float want = Math.Min(Balance.ShotRange - 0.8f, locker.Def.Reach + 1.8f);
                if (locker.Def.Speed > 0f && V2.Dist(c.Pos, locker.Pos) < want - 1f) { Go(Escape(r, c.Pos, locker.Pos)); return false; }   // chefe chegando: abrir distancia
                if (V2.Dist(c.Pos, locker.Pos) <= want + 0.3f) { stop = true; return false; }
                Go(Run.FreeNear(r.Room, locker.Pos + (c.Pos - locker.Pos).Norm * want, Balance.CourierR));
                return false;
            }

            // 5) luta opcional: pousa quando ha inimigo perto (nao-ladrao)
            if (fightRoom && near != null && near.Type != EnemyType.Thief && nearD <= Balance.ShotRange - 0.3f)
            {
                if (near.Def.Speed > 0f && near.Def.Reach > 0f && nearD < near.Def.Trigger + Balance.CourierR + 0.8f) Go(Escape(r, c.Pos, near.Pos));   // Bruto colado: recuar antes de pousar
                else stop = true;
                return false;
            }

            // 6) bau
            if (wantChest && r.Chest != null && !r.Chest.Opened)
            {
                if (V2.Dist(c.Pos, r.Chest.Pos) <= Balance.ChestRange - 0.3f) stop = true; else Go(r.Chest.Pos);
                return false;
            }

            // 7) filhote longe: esperar por ele
            if (r.Hatched && r.Pet != null && V2.Dist(r.Pet.Pos, c.Pos) > Balance.PetRange - 1f) { Go(r.Pet.Pos); return false; }

            // 8) entrega ou porta escolhida
            if (r.Room.Delivery) Go(Balance.Client);
            else if (door < r.Doors.Count) Go(new V2(r.Doors[door].Pos.X, Balance.RoomH));
            return false;
        }

        // ------------------------------------------------------------------ medicao (BalanceTests, BALANCE.md)

        /// <summary>Joga o contrato ate o fim (ou desiste em maxSeconds de jogo). null se nao pode comecar.</summary>
        public static ContractResult Play(Sim sim, int contract, bool rescue, Bot bot, float maxSeconds = 600f)
        {
            if (sim.StartContract(contract, rescue) == null) return null;
            while (sim.InContract)
            {
                if (sim.Run.Time >= maxSeconds) { sim.Abandon(); break; }
                bot.Act(sim);
            }
            return sim.LastResult;
        }

        public sealed class Stats
        {
            public int Contract, Runs, Delivered, Died, CargoFail, Timeout, RoomsNormal, RoomsFought, Hatched;
            public double TimeSum, CoinSum, HpLeftSum;
            public double Pct(int n) => Runs == 0 ? 0 : 100.0 * n / Runs;
            public double AvgTime => Runs == 0 ? 0 : TimeSum / Runs;
            public double AvgCoins => Runs == 0 ? 0 : CoinSum / Runs;
            public double FightPct => RoomsNormal == 0 ? 0 : 100.0 * RoomsFought / RoomsNormal;
        }

        /// <summary>Contrato isolado, guilda nivel 0, todos os contratos desbloqueados, `seeds` sementes.</summary>
        public static Stats Measure(int contract, int seeds, float aggression = 0.5f, float reaction = 0.25f, int baseSeed = 1000)
        {
            var st = new Stats { Contract = contract };
            for (int s = 0; s < seeds; s++)
            {
                var sim = new Sim(baseSeed + s) { Reputation = Sim.Contracts.Length };
                ContractResult res = Play(sim, contract, false, new Bot(baseSeed * 31 + s, aggression, reaction));
                st.Runs++;
                st.TimeSum += res.Time; st.CoinSum += res.Total; st.RoomsNormal += res.RoomsNormal; st.RoomsFought += res.RoomsFought;
                if (res.Hatched) st.Hatched++;
                switch (res.Outcome)
                {
                    case Outcome.Delivered: st.Delivered++; st.HpLeftSum += sim.Run.Courier.Hp; break;
                    case Outcome.Died: st.Died++; break;
                    case Outcome.Abandoned: st.Timeout++; break;
                    default: st.CargoFail++; break;
                }
            }
            return st;
        }

        public sealed class Campaign
        {
            public int Attempts, Deaths, CargoFails, Rescues, RescueBonus, Coins, Earned;
            public float Time;
            public bool Finished;
            public readonly int[] Levels = new int[3];
            public readonly int[] AttemptsPer = new int[Sim.Contracts.Length];
        }

        /// <summary>Campanha do zero: joga o proximo contrato bloqueado (aceitando o resgate quando ha pendente), refaz o que falha e
        /// compra o upgrade mais barato que couber (empate: Vida, Dash, Bolsa) antes de cada contrato.</summary>
        public static Campaign PlayCampaign(int seed, float aggression = 0.5f, float reaction = 0.25f, int maxAttempts = 60)
        {
            var sim = new Sim(seed); var bot = new Bot(seed * 13 + 5, aggression, reaction); var cp = new Campaign();
            while (sim.Reputation < Sim.Contracts.Length && cp.Attempts < maxAttempts)
            {
                while (true)
                {
                    int pick = -1, cost = int.MaxValue;
                    for (int u = 0; u < 3; u++) { int k = sim.UpgradeCost((Upgrade)u); if (sim.CanBuy((Upgrade)u) && k < cost) { cost = k; pick = u; } }
                    if (pick < 0) break;
                    sim.Buy((Upgrade)pick);
                }
                int c = sim.Reputation;
                int before = sim.Coins;
                ContractResult res = Play(sim, c, sim.Pending != null, bot);
                cp.Attempts++; cp.AttemptsPer[c]++; cp.Time += res.Time; cp.Earned += sim.Coins - before;
                if (res.Outcome == Outcome.Died) cp.Deaths++;
                else if (!res.Delivered) cp.CargoFails++;
                if (res.RescueDelivered) { cp.Rescues++; cp.RescueBonus += res.RescueBonus; }
            }
            cp.Finished = sim.Reputation >= Sim.Contracts.Length;
            cp.Coins = sim.Coins;
            Array.Copy(sim.Levels, cp.Levels, 3);
            return cp;
        }
    }
}
