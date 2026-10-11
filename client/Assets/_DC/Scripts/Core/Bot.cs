using System;
using System.Collections.Generic;
using System.Globalization;

namespace DC.Core
{
    /// <summary>Jogador heuristico deterministico (semente) que joga contratos inteiros no Core sem render. Aggression (0..1) = chance
    /// de escolher lutar numa sala e de aceitar porta perigosa; Greed (0..1) = chance de ir ao bau/sala secreta e de preferir a porta com
    /// bau; Reaction = handicap humano: o plano (para onde ir, parar, dash) so e refeito a cada ~Reaction s (+-0,08 s); entre um plano e
    /// outro o bot so segue o caminho. Joga o verbo de cada carga (o que um jogador que leu a carga faria): reliquia sem dash e pousando
    /// para abater quem encosta; bolo sem dash; barril pousado no maximo ~1,6 s; mensagem sem lutar nem bau; porta escolhida pelo icone de risco
    /// da carga. ponytail: heuristica de regras, nao busca; serve para medir, nao para jogar bem.</summary>
    public sealed class Bot
    {
        public readonly float Aggression, Reaction, Greed;
        readonly Rng rng;
        int wait, roomKey = -1, door;
        bool fightRoom, wantChest, wantSecret, stop, bored, defend;
        float roomStart;
        V2 goal;

        public Bot(int seed, float aggression = 0.5f, float reaction = 0.25f, float greed = 0.5f)
        {
            rng = new Rng(Rng.Hash(seed, 7919));
            Aggression = aggression; Reaction = reaction; Greed = greed;
        }

        /// <summary>Joga um passo fixo: escolhe a perk se ha oferta; replaneja se passou o tempo de reacao e chama sim.Tick(Balance.Step, move, dash).</summary>
        public void Act(Sim sim)
        {
            Run r = sim.Run;
            if (r == null || !r.Running) return;
            if (r.PerkOffer != null) { sim.PickPerk(ChoosePerk(r)); sim.Tick(Balance.Step, V2.Zero, false); return; }   // 1 Act = 1 Tick (eventos nao repetem)
            int key = r.Layer * 64 + r.Room.Id;
            if (key != roomKey) { roomKey = key; roomStart = r.Time; OnRoom(r); wait = 0; bored = false; }
            if (r.Time - roomStart > 40f) { fightRoom = false; wantChest = false; wantSecret = false; bored = true; }   // valvula: nada de ficar preso numa sala
            bool dash = false;
            if (--wait <= 0)
            {
                dash = Plan(r);
                wait = Math.Max(1, (int)Math.Round((Reaction + rng.Range(-0.08f, 0.08f)) / Balance.Step));
            }
            // ao entrar, o Core so conta o pouso depois do 1o movimento (FTUE): quem quer parar ja na entrada da um passo curto antes
            V2 move = stop ? (r.AwaitingMove ? V2.Up * 0.3f : V2.Zero) : r.PathDir(r.Courier.Pos, goal);
            if (dash) move = (goal - r.Courier.Pos).Norm;
            sim.Tick(Balance.Step, move, dash);
        }

        // ------------------------------------------------------------------ perk, porta, arma

        static readonly Perk[] PerkRank = { Perk.ExtraHeart, Perk.QuickHand, Perk.Padded, Perk.DoubleDash, Perk.IronBoot, Perk.LightStep, Perk.QuickPlant, Perk.Lookout, Perk.ChestLuck, Perk.Magnet,
                                            // lancamento (so saem da regiao 2 em diante): depois dos da v1, para a regiao 1 nao mudar
                                            Perk.SecondWind, Perk.ThickSkin, Perk.CargoShield, Perk.Counter, Perk.Whistle, Perk.FirmGrip, Perk.Spring, Perk.SureFoot,
                                            Perk.Runner, Perk.Insulated, Perk.FirstAid, Perk.Hunter, Perk.CatEyes, Perk.Scout, Perk.ChestSense };

        /// <summary>Indice (0..2) da perk preferida: a da carga primeiro (acolchoada para reliquia/bolo/ovo; passo leve para pesada/mensagem).</summary>
        public static int ChoosePerk(Run r)
        {
            Perk[] off = r.PerkOffer;
            if (off == null || off.Length == 0) return 0;
            CargoType t = r.Contract.Cargo;
            Perk first = t == CargoType.Relic || t == CargoType.Cake || t == CargoType.Egg ? Perk.Padded
                       : t == CargoType.Heavy || t == CargoType.Message ? Perk.LightStep : Perk.ExtraHeart;
            int bestI = 0, bestR = int.MaxValue;
            for (int i = 0; i < off.Length; i++)
            {
                int rank = off[i] == first ? -1 : Array.IndexOf(PerkRank, off[i]);
                if (rank < bestR) { bestR = rank; bestI = i; }
            }
            return bestI;
        }

        /// <summary>Quanto o risco da porta pesa para esta carga (o que o icone de risco ensina).</summary>
        static float RiskCost(CargoType t, CargoRisk k, int threat = 0)
        {
            float c = t == CargoType.Relic ? 0.4f * threat : 0f;   // golpe no courier racha a reliquia nas maos
            bool thief = (k & CargoRisk.Thief) != 0, archer = (k & CargoRisk.Archer) != 0, smash = (k & CargoRisk.Smash) != 0;
            switch (t)
            {
                case CargoType.Relic: c = (thief ? 1.4f : 0f) + (archer ? 1.2f : 0f) + (smash ? 1.0f : 0f); break;
                case CargoType.Cake: c = (thief ? 0.9f : 0f) + (archer ? 0.9f : 0f) + (smash ? 0.6f : 0f); break;
                case CargoType.Egg: c = (smash ? 0.9f : 0f) + (archer ? 0.6f : 0f); break;
                case CargoType.Barrel: c = (thief ? 1.0f : 0f) + (smash ? 0.4f : 0f); break;
                case CargoType.Heavy: c = thief ? 0.6f : 0f; break;
            }
            return c;
        }

        /// <summary>Arma da campanha pela carga: bombinha racha ovo/reliquia e detona barril, entao so com as outras; e e lenta demais para o
        /// Capitao que foge com a carga (contrato com M1).</summary>
        public static Weapon WeaponFor(Sim sim, CargoType t) => WeaponFor(sim, t, false);

        public static Weapon WeaponFor(Sim sim, ContractDef def)
        {
            bool captain = false;
            foreach (int[] layer in def.Layers) if (System.Array.IndexOf(layer, Content.M1) >= 0) captain = true;
            return WeaponFor(sim, def.Cargo, captain);
        }

        static Weapon WeaponFor(Sim sim, CargoType t, bool captain)
        {
            bool fragile = t == CargoType.Egg || t == CargoType.Barrel || t == CargoType.Relic;
            if (!fragile && !captain && sim.WeaponUnlocked(Weapon.Bomb) && sim.WeaponLevel(Weapon.Bomb) >= sim.WeaponLevel(Weapon.Sling)) return Weapon.Bomb;
            if (sim.WeaponUnlocked(Weapon.Boomerang)) return Weapon.Boomerang;
            return Weapon.Sling;
        }

        void OnRoom(Run r)
        {
            bool thief = false;
            foreach (Enemy e in r.Enemies) if (e.Type == EnemyType.Thief) thief = true;
            CargoType t = r.Contract.Cargo;
            bool msg = t == CargoType.Message && !r.Hatched;
            fightRoom = !msg && rng.Float() < Aggression * (thief ? 0.7f : 1f);
            defend = rng.Float() < 0.2f + 0.8f * Aggression;   // parar para atirar em quem persegue de perto tambem e escolha
            wantChest = !msg && r.Chest != null && !r.Chest.Opened && rng.Float() < Greed;
            wantSecret = !msg && r.Secret != null && rng.Float() < Greed;
            door = 0; float best = float.MinValue;
            foreach (Door d in r.Doors)
            {
                float s = -d.Threat * (1.2f - Aggression) + (d.ChestCoins > 0 && !msg ? Greed * 1.5f : 0f) - (r.Hatched ? 0f : RiskCost(t, d.Risk, d.Threat)) + rng.Range(0f, 1.2f);
                if (s > best) { best = s; door = d.Index; }
            }
        }

        // ------------------------------------------------------------------ plano

        static Enemy Nearest(Run r, V2 p, bool thievesOnly = false)
        {
            Enemy best = null; float bd = float.MaxValue;
            foreach (Enemy e in r.Enemies)
            {
                if (!e.Alive || (thievesOnly && e.Type != EnemyType.Thief && e.Type != EnemyType.Captain)) continue;
                float d = V2.Dist(e.Pos, p);
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        void Go(V2 to) { stop = false; goal = to; }

        /// <summary>Reliquia nas maos: golpe no courier racha e o dash a larga (racha).</summary>
        static bool HoldsFragile(Run r) => r.Holds(CargoType.Relic);

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
            // alvo atras de pilar (bumerangue): parado nao adianta; andar ate ter linha
            if (near != null && !r.CanHit(near) && nearD <= Balance.ShotRange && (c.State == CourierState.Planted || stop))
            {
                bool other = false;
                foreach (Enemy e in r.Enemies) if (r.CanHit(e)) other = true;
                if (!other) { Go(near.Pos); return false; }
            }
            Enemy locker = null;
            foreach (Enemy e in r.Enemies) if (e.Alive && e.Def.Locks) locker = e;
            bool msg = r.Holds(CargoType.Message);

            // 0) barril no chao com o pavio adiantado: pegar de volta antes de tudo
            foreach (Cargo g in r.Cargos)
                if (g.State == CargoState.Ground && g.Type == CargoType.Barrel && g.Fuse >= 1.6f) { Go(g.Pos); return false; }

            // 1) golpe telegrafado que pega o courier: sair (dash se esta em cima da hora)
            foreach (Enemy e in r.Enemies)
            {
                if (!e.Alive || e.State != EnemyState.Telegraph || e.Type == EnemyType.Archer || V2.Dist(e.Pos, c.Pos) - Balance.CourierR > e.Def.Reach + 0.4f) continue;
                Go(Escape(r, c.Pos, e.Pos));
                return e.Tele < 0.3f && c.DashReady && !HoldsFragile(r) && !r.Holds(CargoType.Cake);   // o dash larga a reliquia (racha) e o bolo nao deixa: so anda
            }
            // 1b) arqueiro mirando no courier: sair da linha para o lado
            foreach (Enemy e in r.Enemies)
            {
                if (!e.Alive || e.Type != EnemyType.Archer || e.State != EnemyState.Telegraph) continue;
                if (V2.SegDist(c.Pos, e.Pos, e.Pos + e.Aim * Balance.ArrowRange) > Balance.CourierR + 0.35f) continue;
                V2 side = e.Aim.Right;
                if (V2.Dot(c.Pos - e.Pos, side) < 0f) side = side * -1f;
                Go(Run.FreeNear(r.Room, c.Pos + side * 1.5f, Balance.CourierR));
                return false;
            }

            // 1c) limo colado (lento, 1 de dano por toque): um passo para longe e volta a atirar
            bool fragileDown = false;   // fugir passaria por cima da carga fragil (pega sozinho) e o toque racharia: fica e atira
            foreach (Cargo g in r.Cargos) if (g.State == CargoState.Ground && g.Type == CargoType.Relic && V2.Dist(g.Pos, c.Pos) <= r.Sim.PickupRange + 1.5f) fragileDown = true;
            foreach (Enemy e in r.Enemies)
                if (!fragileDown && e.Alive && e.Type == EnemyType.Slime && V2.Dist(e.Pos, c.Pos) < e.Def.Radius + Balance.CourierR + 0.5f) { Go(Escape(r, c.Pos, e.Pos)); return false; }

            // 2) ladrao/Capitao com uma carga nossa: alcancar (dash empurra = derruba) ou parar e atirar
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
                if (atChest && c.State == CourierState.Planted && Nearest(r, ground.Pos, true) == null && ground.Type != CargoType.Barrel) { stop = true; return false; }
                bool fight = locker != null || fightRoom || (!bored && ground.Type == CargoType.Relic) || Nearest(r, ground.Pos, true) != null;
                if (c.State == CourierState.Planted && fight && nearD <= Balance.ShotRange && V2.Dist(c.Pos, ground.Pos) <= r.Sim.PickupRange) { stop = true; return false; }
                Go(ground.Pos);
                return false;
            }

            // 4) luta obrigatoria: parar a ~3 m do Guarda/Ogro/Capitao (fora do golpe, dentro do tiro). Com a mensagem, so o chefe obriga.
            if (locker != null && !(msg && !locker.Def.Boss))
            {
                float want = Math.Min(Balance.ShotRange - 0.8f, locker.Def.Reach + 1.8f);
                if (locker.Def.Speed > 0f && V2.Dist(c.Pos, locker.Pos) < want - 1f) { Go(Escape(r, c.Pos, locker.Pos)); return false; }   // chefe chegando: abrir distancia
                if (V2.Dist(c.Pos, locker.Pos) <= want + 0.3f) { stop = true; return false; }
                Go(Run.FreeNear(r.Room, locker.Pos + (c.Pos - locker.Pos).Norm * want, Balance.CourierR));
                return false;
            }

            // 4b) emboscada: a porta fechou ate o reforco cair (sem Guarda): lutar com tudo, ladrao inclusive (pousar atrai o ladrao)
            if (locker == null && r.Doors.Count > 0 && r.Doors[0].Locked && near != null)
            {
                if (nearD <= Balance.ShotRange - 0.5f) stop = true; else Go(near.Pos);
                return false;
            }

            // 5) luta opcional: pousa quando ha inimigo perto (nao-ladrao)
            // com reliquia/bolo nas maos, golpe racha: pousa e abate quem vem encostar; o ladrao que ronda so e cacado nas salas em que o
            // bot escolheu lutar (agressividade) ou depois que a carga ja rachou ("agora tomo cuidado"). Com a mensagem, nao luta.
            bool fragile = HoldsFragile(r) && !bored, careful = fragile && (fightRoom || r.Main.Cracks > 0);
            // perseguido de perto (morcego/limo/Bruto): qualquer um para e atira; com a mensagem, so isso
            bool chased = defend && near != null && near.Def.Speed > 0f && near.Type != EnemyType.Thief && near.Type != EnemyType.Archer && nearD <= 1.6f;
            if ((chased || (!msg && (fightRoom || fragile))) && near != null && (near.Type != EnemyType.Thief || careful) && nearD <= Balance.ShotRange - 0.3f)
            {
                if (near.Def.Speed > 0f && near.Def.Reach > 0f && near.Def.Reach < 3f && nearD < near.Def.Trigger + Balance.CourierR + 0.8f) Go(Escape(r, c.Pos, near.Pos));   // Bruto colado: recuar antes de pousar
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

            // 7b) parede rachada: sala secreta (bau extra)
            if (wantSecret && r.Secret != null && r.CarryOk)
            {
                Go(new V2(r.Secret.Pos.X < Balance.RoomW * 0.5f ? 0.1f : Balance.RoomW - 0.1f, r.Secret.Pos.Y));
                return false;
            }

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
            return Finish(sim, bot, maxSeconds);
        }

        /// <summary>Joga o contrato ja comecado (dia, codigo ou campanha) ate o fim.</summary>
        public static ContractResult Finish(Sim sim, Bot bot, float maxSeconds = 600f)
        {
            int guard = 0;   // trava de seguranca: um passo sem tempo andando (oferta de perk nao escolhida) nao pode virar laco infinito
            while (sim.InContract)
            {
                if (sim.Run.Time >= maxSeconds || ++guard > 60000) { sim.Abandon(); break; }
                bot.Act(sim);
            }
            return sim.LastResult;
        }

        public sealed class Stats
        {
            public int Contract, Runs, Delivered, Died, CargoFail, Timeout, RoomsNormal, RoomsFought, Hatched, TimeUp, Exploded, Broken, Stolen;
            public double TimeSum, CoinSum, HpLeftSum, CrackSum, StarSum;
            public int ThreeStars, WaveEvents, Secrets, Perks;
            /// <summary>Dano recebido e visitas por sala (indice = id da sala em Content.Rooms).</summary>
            public readonly double[] RoomDamage = new double[Content.Rooms.Length];
            public readonly int[] RoomVisits = new int[Content.Rooms.Length];
            /// <summary>Tempo (s) e salas com luta por sala (indice = id da sala).</summary>
            public readonly double[] RoomTime = new double[Content.Rooms.Length];
            public readonly int[] RoomFights = new int[Content.Rooms.Length];
            public readonly List<float> Times = new List<float>();
            public double DamagePerVisit(int room) => RoomVisits[room] == 0 ? 0 : RoomDamage[room] / RoomVisits[room];
            /// <summary>Rachaduras medias da carga do contrato no fim (reliquia quebrada conta 3).</summary>
            public double AvgCracks => Runs == 0 ? 0 : CrackSum / Runs;
            public double Pct(int n) => Runs == 0 ? 0 : 100.0 * n / Runs;
            public double AvgTime => Runs == 0 ? 0 : TimeSum / Runs;
            public double AvgCoins => Runs == 0 ? 0 : CoinSum / Runs;
            public double AvgStars => Runs == 0 ? 0 : StarSum / Runs;
            public double FightPct => RoomsNormal == 0 ? 0 : 100.0 * RoomsFought / RoomsNormal;
            /// <summary>Duracao mediana (s) das tentativas.</summary>
            public double MedianTime { get { if (Times.Count == 0) return 0; var t = new List<float>(Times); t.Sort(); return t[t.Count / 2]; } }
        }

        /// <summary>Sim de medicao: guilda no nivel `guild` nas 4 linhas, arma `weapon` no nivel `weaponLevel` (destravada), todos os contratos
        /// abertos. Montado pelo save (API publica), como um jogador que chegou ate ali.</summary>
        public static Sim MeasureSim(int seed, Weapon weapon = Weapon.Sling, int weaponLevel = 1, int guild = 0)
        {
            string lv = guild + "," + guild + "," + guild + "," + guild, wl = weaponLevel + "," + weaponLevel + "," + weaponLevel;
            var sim = Sim.Load("v=2\nseed=" + seed + "\nrep=" + Sim.Contracts.Length + "\nlv=" + lv + "\nstars=" + new string('3', Sim.Contracts.Length) +
                               "\nwlv=" + wl + "\nwpn=" + (int)weapon + "\n");
            return sim;
        }

        /// <summary>Contrato isolado, guilda nivel 0, estilingue, todos os contratos desbloqueados, `seeds` sementes.</summary>
        public static Stats Measure(int contract, int seeds, float aggression = 0.5f, float reaction = 0.25f, int baseSeed = 1000,
                                    Weapon weapon = Weapon.Sling, int weaponLevel = 1, int guild = 0)
        {
            var st = new Stats { Contract = contract };
            for (int s = 0; s < seeds; s++)
            {
                Sim sim = MeasureSim(baseSeed + s, weapon, weaponLevel, guild);
                var bot = new Bot(baseSeed * 31 + s, aggression, reaction);
                sim.StartContract(contract, false);
                PlayOut(sim, bot, st);
            }
            return st;
        }

        /// <summary>Mede o contrato a partir de um save (ex.: Campaign.Before[c], o estado real da campanha antes da 1a tentativa dele):
        /// mesma guilda, armas e estrelas; arma trocada pela carga como na campanha; sem resgate.</summary>
        public static Stats MeasureFrom(string save, int contract, int seeds, int baseSeed = 1000, float aggression = 0.5f, Stats into = null)
        {
            var st = into ?? new Stats { Contract = contract };
            for (int s = 0; s < seeds; s++)
            {
                Sim sim = Sim.Load(save);
                sim.Seed = baseSeed + s; sim.Pending = null;
                sim.Equip(WeaponFor(sim, Sim.Contracts[contract]));
                var bot = new Bot(baseSeed * 31 + s, aggression);
                if (sim.StartContract(contract, false) == null) continue;
                PlayOut(sim, bot, st);
            }
            return st;
        }

        /// <summary>Tabela realista: `campaigns` campanhas do bot (sementes 5000..), e cada contrato medido `seeds` vezes a partir do save
        /// de cada campanha antes da 1a tentativa dele (guilda e armas que o bot tinha ao chegar la).</summary>
        public static Stats[] MeasureCampaignTable(int campaigns, int seeds, int region = 1)
        {
            RegionDef reg = RegionDef.Of(region);
            var t = new Stats[reg.Contracts.Length];   // t[k] = contrato reg.First + k (na regiao 1, o indice e o proprio contrato)
            for (int c = 0; c < t.Length; c++) t[c] = new Stats { Contract = reg.First + c };
            for (int k = 0; k < campaigns; k++)
            {
                Campaign cp = PlayCampaign(5000 + k, region: region);
                for (int c = 0; c < t.Length; c++) if (cp.Before[reg.First + c] != null) MeasureFrom(cp.Before[reg.First + c], reg.First + c, seeds, 1000 + 100 * k, 0.5f, t[c]);
            }
            return t;
        }

        /// <summary>Contratos gerados (dia/codigo) com payloads base..base+n-1.</summary>
        public static Stats MeasureGenerated(int n, int basePayload = 777, float aggression = 0.5f)
        {
            var st = new Stats { Contract = -1 };
            for (int s = 0; s < n; s++)
            {
                Sim sim = MeasureSim(basePayload + s);
                var bot = new Bot(basePayload * 31 + s, aggression);
                if (sim.StartCode(ContractCode.Encode(basePayload + s), false) == null) continue;
                PlayOut(sim, bot, st);
            }
            return st;
        }

        static void PlayOut(Sim sim, Bot bot, Stats st)
        {
            int room = -1; float enter = 0f;
            room = Count(sim, st, room, ref enter);
            int guard = 0;
            while (sim.InContract)   // o mesmo laco do Play, contando dano por sala
            {
                if (sim.Run.Time >= 600f || ++guard > 60000) { sim.Abandon(); break; }
                bot.Act(sim);
                room = Count(sim, st, room, ref enter);
            }
            if (room >= 0) st.RoomTime[room] += sim.Run.Time - enter;
            ContractResult res = sim.LastResult;
            st.Runs++;
            st.TimeSum += res.Time; st.Times.Add(res.Time); st.CoinSum += res.Total; st.RoomsNormal += res.RoomsNormal; st.RoomsFought += res.RoomsFought; st.CrackSum += res.Cracks;
            st.StarSum += res.Stars; if (res.Stars == 3) st.ThreeStars++;
            st.Perks += res.Perks.Length;
            if (res.Hatched) st.Hatched++;
            switch (res.Outcome)
            {
                case Outcome.Delivered: st.Delivered++; st.HpLeftSum += sim.Run.Courier.Hp; break;
                case Outcome.Died: st.Died++; break;
                case Outcome.Abandoned: st.Timeout++; break;
                case Outcome.TimeUp: st.TimeUp++; st.CargoFail++; break;
                case Outcome.Exploded: st.Exploded++; st.CargoFail++; break;
                case Outcome.CargoBroken: st.Broken++; st.CargoFail++; break;
                case Outcome.CargoStolen: st.Stolen++; st.CargoFail++; break;
                default: st.CargoFail++; break;
            }
        }

        static int Count(Sim sim, Stats st, int room, ref float enter)
        {
            foreach (SimEvent e in sim.Events)
            {
                if (e.Kind == Ev.RoomEnter) { if (room >= 0) st.RoomTime[room] += sim.Run.Time - enter; enter = sim.Run.Time; room = e.A; st.RoomVisits[room]++; }
                else if (e.Kind == Ev.RoomFightChoice && e.A == 1) st.RoomFights[e.B]++;
                else if (e.Kind == Ev.DamageTaken && room >= 0) st.RoomDamage[room] += e.A;
                else if (e.Kind == Ev.RoomEvent) st.WaveEvents++;
                else if (e.Kind == Ev.SecretFound) st.Secrets++;
            }
            return room;
        }

        // ------------------------------------------------------------------ H2: a carga muda a rota

        /// <summary>Escolha de porta por carga (H2): joga cada contrato da campanha com cada uma das 7 cargas no lugar da original, `seeds`
        /// sementes, e conta em cada bifurcacao (contrato, camada, sala) a porta escolhida por carga. SameDoorPct = % das bifurcacoes em que
        /// TODAS as cargas vao para a mesma porta em mais de 80% das vezes (a carga nao muda a rota ali; H2 e refutada se for a maioria).
        /// ModalPct = % das escolhas que foram para a porta mais escolhida da bifurcacao, somando as cargas. ThiefPct[carga] = % das escolhas
        /// pela porta com icone de ladrao quando a outra porta nao tem (o icone de risco da carga mudando a rota).</summary>
        public sealed class Route
        {
            public double ModalPct, SameDoorPct;
            public int ForkCount;
            public readonly double[] ThiefPct = new double[CargoDef.All.Length];
            public readonly int[] ThiefForks = new int[CargoDef.All.Length];
        }

        public static Route MeasureRoute(int seeds, int baseSeed = 3000)
        {
            int n = RegionDef.Of(1).CargoCount;   // H2 e da regiao 1: as 7 cargas dela nos 24 contratos dela
            var counts = new Dictionary<string, int[,]>();
            var thief = new int[n]; var thiefForks = new int[n];
            foreach (ContractDef def0 in Regiao1.Contracts)
                for (int t = 0; t < n; t++)
                {
                    var def = new ContractDef("H2", (CargoType)t, def0.Pay, (CargoType)t == CargoType.Egg ? 999f : 0f, def0.Note, Trim(def0.Layers))
                    { ParTime = def0.ParTime, TimeLimit = (CargoType)t == CargoType.Message ? 999f : 0f };
                    for (int s = 0; s < seeds; s++)
                    {
                        Sim sim = MeasureSim(baseSeed + s);
                        var bot = new Bot(baseSeed * 17 + s * 7 + t);
                        if (sim.StartDef(def) == null) continue;
                        Door[] doors = sim.Run.Doors.ToArray();
                        string at = def0.Code + ":" + sim.Run.Layer + ":" + sim.Run.Room.Id;
                        int guard = 0;
                        while (sim.InContract && sim.Run.Time < 600f && ++guard < 60000)
                        {
                            bot.Act(sim);
                            foreach (SimEvent e in sim.Events)
                            {
                                if (e.Kind == Ev.RouteChoice && doors.Length >= 2)
                                {
                                    if (!counts.TryGetValue(at, out int[,] k)) counts[at] = k = new int[n, 3];
                                    k[t, e.A]++;
                                    bool withThief = false, without = false;
                                    foreach (Door d in doors) if ((d.Risk & CargoRisk.Thief) != 0) withThief = true; else without = true;
                                    if (withThief && without) { thiefForks[t]++; if ((doors[e.A].Risk & CargoRisk.Thief) != 0) thief[t]++; }
                                }
                                if (e.Kind == Ev.RoomEnter) { doors = sim.Run.Doors.ToArray(); at = def0.Code + ":" + sim.Run.Layer + ":" + sim.Run.Room.Id; }
                            }
                        }
                        if (sim.InContract) sim.Abandon();
                    }
                }
            var route = new Route();
            int total = 0, modal = 0, forks = 0, same = 0;
            foreach (int[,] k in counts.Values)
            {
                var all = new int[3];
                for (int t = 0; t < n; t++) for (int d = 0; d < 3; d++) all[d] += k[t, d];
                int m = all[0] >= all[1] && all[0] >= all[2] ? 0 : all[1] >= all[2] ? 1 : 2;
                total += all[0] + all[1] + all[2]; modal += all[m];
                bool insensitive = true; int cargos = 0;
                for (int t = 0; t < n; t++)
                {
                    int sum = k[t, 0] + k[t, 1] + k[t, 2];
                    if (sum < 3) continue;
                    cargos++;
                    if (k[t, m] * 100 <= sum * 80) insensitive = false;
                }
                if (cargos < 2) continue;
                forks++; if (insensitive) same++;
            }
            route.ModalPct = total == 0 ? 0 : 100.0 * modal / total;
            route.ForkCount = forks;
            route.SameDoorPct = forks == 0 ? 0 : 100.0 * same / forks;
            for (int t = 0; t < n; t++) { route.ThiefForks[t] = thiefForks[t]; route.ThiefPct[t] = thiefForks[t] == 0 ? 0 : 100.0 * thief[t] / thiefForks[t]; }
            return route;
        }

        static int[][] Trim(int[][] layers) { var l = new int[layers.Length - 1][]; Array.Copy(layers, l, l.Length); return l; }

        // ------------------------------------------------------------------ campanha

        public sealed class Campaign
        {
            public int Attempts, Deaths, CargoFails, Rescues, RescueBonus, Coins, Earned, Stars, Spent;
            public float Time;
            public bool Finished;
            public readonly int[] Levels = new int[Balance.UpgradeLines];
            public readonly int[] WeaponLevels = new int[WeaponDef.All.Length];
            public readonly int[] AttemptsPer = new int[Sim.Contracts.Length];
            /// <summary>Duracao (s) da tentativa que entregou cada contrato.</summary>
            public readonly float[] TimePer = new float[Sim.Contracts.Length];
            /// <summary>Soma dos niveis da guilda (4 linhas) quando cada contrato foi entregue pela 1a vez.</summary>
            public readonly int[] GuildAt = new int[Sim.Contracts.Length];
            /// <summary>Contrato (indice) em que cada arma destravou (-1 = nao destravou).</summary>
            public readonly int[] UnlockAt = { 0, -1, -1 };
            /// <summary>Save antes da 1a tentativa de cada contrato (depois das compras): base da tabela realista.</summary>
            public readonly string[] Before = new string[Sim.Contracts.Length];
            /// <summary>Tempo (s) de jogo acumulado e moedas ganhas ate entregar cada contrato.</summary>
            public readonly float[] ClockAt = new float[Sim.Contracts.Length];
        }

        /// <summary>Campanha do zero: joga o proximo contrato bloqueado (aceitando o resgate quando ha pendente), refaz o que falha, troca a
        /// arma pela carga e compra o mais barato que couber (guilda ou nivel de arma destravada) antes de cada contrato.</summary>
        public static Campaign PlayCampaign(int seed, float aggression = 0.5f, float reaction = 0.25f, int maxAttempts = 150, int region = 1)
        {
            var sim = new Sim(seed); var bot = new Bot(seed * 13 + 5, aggression, reaction); var cp = new Campaign();
            RegionDef reg = RegionDef.Of(region);
            int end = reg.First + reg.Contracts.Length;   // joga ate o fim da regiao `region` (padrao: a regiao 1, os 24 da v1)
            while (sim.Reputation < end && cp.Attempts < maxAttempts)
            {
                while (true)
                {
                    int pick = -1, cost = int.MaxValue;
                    for (int u = 0; u < Balance.UpgradeLines; u++) { int k = sim.UpgradeCost((Upgrade)u); if (sim.CanBuy((Upgrade)u) && k < cost) { cost = k; pick = u; } }
                    for (int w = 0; w < WeaponDef.All.Length; w++) { int k = sim.WeaponCost((Weapon)w); if (sim.CanUpgradeWeapon((Weapon)w) && k < cost) { cost = k; pick = 10 + w; } }
                    if (pick < 0) break;
                    cp.Spent += cost;
                    if (pick >= 10) sim.UpgradeWeapon((Weapon)(pick - 10)); else sim.Buy((Upgrade)pick);
                }
                int c = sim.Reputation;
                sim.Equip(WeaponFor(sim, Sim.Contracts[c]));
                if (cp.Before[c] == null) cp.Before[c] = sim.Save();
                int before = sim.Coins;
                ContractResult res = Play(sim, c, sim.Pending != null, bot);
                cp.Attempts++; cp.AttemptsPer[c]++; cp.Time += res.Time; cp.Earned += sim.Coins - before;
                if (res.Outcome == Outcome.Died) cp.Deaths++;
                else if (!res.Delivered) cp.CargoFails++;
                if (res.RescueDelivered) { cp.Rescues++; cp.RescueBonus += res.RescueBonus; }
                if (res.Delivered)
                {
                    cp.TimePer[c] = res.Time; cp.ClockAt[c] = cp.Time;
                    int g = 0; foreach (int l in sim.Levels) g += l;
                    cp.GuildAt[c] = g;
                    for (int w = 1; w < WeaponDef.All.Length; w++) if (cp.UnlockAt[w] < 0 && sim.WeaponUnlocked((Weapon)w)) cp.UnlockAt[w] = c;
                }
            }
            cp.Finished = sim.Reputation >= end;
            cp.Coins = sim.Coins; cp.Stars = sim.Stars;
            Array.Copy(sim.Levels, cp.Levels, cp.Levels.Length);
            for (int w = 0; w < WeaponDef.All.Length; w++) cp.WeaponLevels[w] = sim.WeaponLevel((Weapon)w);
            return cp;
        }

        /// <summary>Linha legivel de uma Stats (log de medicao).</summary>
        public static string Line(Stats s) => string.Format(CultureInfo.InvariantCulture,
            "{0}: {1:0}% entregue, {2:0}% morte, {3:0}% falha, {4:0.0}s (mediana {5:0.0}), luta {6:0}%, {7:0} moedas, {8:0.00} estrelas, 3* {9:0}%, preso {10}",
            s.Contract >= 0 ? Sim.Contracts[s.Contract].Code : "gerado", s.Pct(s.Delivered), s.Pct(s.Died), s.Pct(s.CargoFail), s.AvgTime, s.MedianTime, s.FightPct, s.AvgCoins, s.AvgStars, s.Pct(s.ThreeStars), s.Timeout);
    }
}
