using System;
using System.Collections.Generic;

namespace DC.Core
{
    // ------------------------------------------------------------------ estado lido pela View (campos publicos; quem muda e o Run)

    public sealed class Courier
    {
        public V2 Pos = Balance.Entrance, Facing = V2.Up, DashDir = V2.Up;
        public int Hp, MaxHp;
        public CourierState State;
        /// <summary>DashCd = recarga restante (s); DashCdMax = recarga total com upgrade; IdleT = tempo de dedo solto.</summary>
        public float DashCd, DashCdMax, DashT, IdleT, ShotCd;
        public int DashCount;
        public bool DashReady => DashCd <= 0f && State != CourierState.Dashing;
    }

    public sealed class Cargo
    {
        public int Id; public CargoType Type; public CargoState State; public V2 Pos; public int Cracks;
        /// <summary>Carga perdida de um contrato anterior, trazida pelo resgate.</summary>
        public bool Rescue;
        /// <summary>Tempo no chao a mais de 3 m do courier (8 s = nasce um ladrao).</summary>
        public float AloneT;
    }

    public sealed class Enemy
    {
        public int Id; public EnemyType Type; public EnemyDef Def; public V2 Pos; public int Hp; public EnemyState State;
        /// <summary>Tele = telegraph restante (s); Cd = recarga do ataque; GrabT = progresso do ladrao pegando (0..0,5 s);
        /// ExitT = ladrao na borda saindo (0..1 s).</summary>
        public float Tele, Cd, GrabT, ExitT, Phase;
        public Cargo Carry; public V2 FleeGoal; public int PushedDash = -1;
        public bool Alive => Hp > 0;
        public float TeleProgress => State == EnemyState.Telegraph && Def.Tele > 0f ? 1f - Tele / Def.Tele : 0f;
    }

    public sealed class Shot { public V2 Pos; public Enemy Target; }

    /// <summary>Porta na parede de cima. Threat/ChestCoins = icones (da sala de destino). Locked = Guarda/Ogro vivo;
    /// Open = atravessavel agora (destrancada e com a carga nas maos, ou o filhote perto).</summary>
    public sealed class Door { public int Index; public V2 Pos; public int Room, Threat, ChestCoins; public bool Locked, Open; }

    public sealed class Chest { public V2 Pos; public int Coins; public bool Opened; public float Progress; }

    public sealed class Pet { public V2 Pos; public int Hp; }

    /// <summary>Um contrato em curso. Criado por Sim.StartContract (ou Sim.Load ao retomar); avancado por Sim.Tick.</summary>
    public sealed class Run
    {
        public readonly Sim Sim; public readonly ContractDef Contract; public readonly int Seed;
        public int Layer; public RoomDef Room; public float Time; public Outcome Outcome; public Hint Hint;
        public readonly Courier Courier = new Courier();
        /// <summary>[0] = carga do contrato; [1] = carga do resgate (se aceito).</summary>
        public readonly List<Cargo> Cargos = new List<Cargo>();
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Shot> Shots = new List<Shot>();
        public readonly List<Door> Doors = new List<Door>();
        public Chest Chest; public Pet Pet; public bool Hatched;
        public bool RescueOffered, RescueAccepted; public int RescueValue;
        public int ChestCoins, RoomsNormal, RoomsFought, DeathRoom = -1;
        /// <summary>Nesta sala ja matou ao menos 1 inimigo nao-Guarda (room_fight_choice).</summary>
        public bool FoughtHere;

        public Cargo Main => Cargos[0];
        public Cargo RescueCargo => Cargos.Count > 1 ? Cargos[1] : null;
        public bool Running => Outcome == Outcome.Running;
        public int LayerCount => Contract.Layers.Length;
        /// <summary>Segundos ate o ovo chocar (0 se nao e ovo ou ja chocou).</summary>
        public float EggLeft => Contract.EggTarget > 0f && !Hatched ? Math.Max(0f, Contract.EggTarget - Time) : 0f;
        /// <summary>Condicao da porta e da entrega: carga do contrato nas maos (ou, chocado, o filhote a ate 3 m).</summary>
        public bool CarryOk => Hatched ? Pet != null && V2.Dist(Pet.Pos, Courier.Pos) <= Balance.PetRange : Main.State == CargoState.Held;

        float acc; bool dashQueued; Rng rng = new Rng(1); Nav nav; int nextId, lastBlocked = -1; float summonT;
        static readonly Predicate<Enemy> IsDead = e => e.Hp <= 0;

        internal Run(Sim sim, ContractDef contract, int seed, int hp)
        {
            Sim = sim; Contract = contract; Seed = seed;
            Courier.MaxHp = sim.MaxHp; Courier.Hp = Math.Max(1, Math.Min(hp, Courier.MaxHp)); Courier.DashCdMax = sim.DashCooldown;
        }

        internal Cargo AddCargo(int id, CargoType type, int cracks, bool rescue, CargoState state)
        {
            var c = new Cargo { Id = id, Type = type, Cracks = cracks, Rescue = rescue, State = state, Pos = Courier.Pos };
            Cargos.Add(c);
            return c;
        }

        void Emit(Ev k, int a = 0, int b = 0) => Sim.Emit(k, a, b, Courier.Pos);
        void Emit(Ev k, int a, int b, V2 pos) => Sim.Emit(k, a, b, pos);

        // ------------------------------------------------------------------ salas

        /// <summary>Monta a sala na camada atual com o courier na entrada. Inimigos nascem de novo (retomar nunca devolve sala limpa
        /// nem dano). Cargas Held vem junto; as outras ficam onde o chamador deixou.</summary>
        internal void EnterRoom(int roomId, bool chestTaken)
        {
            Room = Content.Rooms[roomId];
            rng = new Rng(Rng.Hash(Seed, Layer * 64 + roomId));
            nav = new Nav(Room);
            Enemies.Clear(); Shots.Clear(); Doors.Clear(); FoughtHere = false; summonT = 0f; lastBlocked = -1;
            var c = Courier;
            c.Pos = Balance.Entrance; c.Facing = V2.Up; c.IdleT = 0f; c.DashT = 0f; c.ShotCd = 0f;
            awaitMove = true;   // FTUE (GDD s3): ao entrar, a 1a licao e arrastar com a carga; o pouso so conta depois de mexer
            foreach (Spawn s in Room.Spawns)
            {
                bool fixedSpot = EnemyDef.All[(int)s.Type].Locks;   // Guarda/Ogro no lugar exato; o resto varia +-0,4 m por semente
                Spawn(s.Type, fixedSpot ? s.Pos : s.Pos + new V2(rng.Range(-0.4f, 0.4f), rng.Range(-0.4f, 0.4f)));
            }
            if (Layer + 1 < LayerCount)
            {
                int[] next = Contract.Layers[Layer + 1];
                for (int i = 0; i < next.Length; i++)
                {
                    RoomDef to = Content.Rooms[next[i]];
                    Doors.Add(new Door { Index = i, Pos = new V2(Balance.DoorX(next.Length, i), Balance.RoomH), Room = to.Id, Threat = to.Threat, ChestCoins = to.ChestCoins });
                }
            }
            Chest = Room.ChestCoins > 0 ? new Chest { Pos = Room.ChestPos, Coins = Room.ChestCoins, Opened = chestTaken } : null;
            foreach (Cargo g in Cargos) if (g.State == CargoState.Held) g.Pos = c.Pos;
            c.State = HasHeld ? CourierState.Carrying : CourierState.Walking;
            if (Pet != null) Pet.Pos = c.Pos + new V2(0.8f, 0f);
            UpdateDoors();
            Emit(Ev.RoomEnter, roomId, Layer);
        }

        /// <summary>Poe um inimigo na sala (usado pelos spawns, pelo ladrao da carga sozinha, pelo Ogro e pelos testes).</summary>
        public Enemy Spawn(EnemyType type, V2 pos)
        {
            EnemyDef d = EnemyDef.All[(int)type];
            var e = new Enemy { Id = ++nextId, Type = type, Def = d, Pos = pos, Hp = d.Hp, State = EnemyState.Idle, Phase = rng.Float() };
            if (type == EnemyType.Bat) ClampRoom(ref e.Pos, d.Radius); else e.Pos = FreeNear(Room, e.Pos, d.Radius);
            Enemies.Add(e);
            return e;
        }

        // ------------------------------------------------------------------ tick

        /// <summary>Acumula dt e avanca em passos fixos de 1/30 s. dash fica guardado ate o proximo passo.</summary>
        internal void Tick(float dt, V2 move, bool dash)
        {
            if (dash) dashQueued = true;
            if (!Running) return;
            acc += Math.Min(Math.Max(dt, 0f), Balance.MaxFrame);
            while (acc >= Balance.Step - 1e-6f && Running)
            {
                acc -= Balance.Step;
                Step(move, dashQueued);
                dashQueued = false;
            }
        }

        /// <summary>Verdadeiro da entrada na sala ate o 1o movimento ou dash: dedo solto ainda nao pousa a carga.</summary>
        public bool AwaitingMove => awaitMove;
        bool awaitMove;

        bool HasHeld { get { foreach (Cargo g in Cargos) if (g.State == CargoState.Held) return true; return false; } }

        void Step(V2 move, bool dash)
        {
            const float dt = Balance.Step;
            Time += dt;
            var c = Courier;
            c.DashCd = c.DashCd - dt < 1e-4f ? 0f : c.DashCd - dt;
            c.ShotCd = c.ShotCd - dt < 1e-4f ? 0f : c.ShotCd - dt;
            float ml = move.Len;
            bool moving = ml > 0.05f;
            V2 dir = moving ? move * (1f / ml) : V2.Zero;

            if (dash && c.State != CourierState.Dashing && c.DashCd <= 0f) { awaitMove = false; StartDash(moving ? dir : c.Facing); }

            if (c.State == CourierState.Dashing)
            {
                MoveCourier(c.DashDir * (Balance.DashDist / Balance.DashTime * Math.Min(dt, c.DashT)));   // 3 m exatos em 0,18 s
                PushEnemies();
                c.DashT -= dt;
                if (c.DashT <= 1e-4f && c.State == CourierState.Dashing) { c.State = HasHeld ? CourierState.Carrying : CourierState.Walking; c.IdleT = 0f; }
            }
            else if (moving)
            {
                c.IdleT = 0f; c.Facing = dir; awaitMove = false;
                float speed = Balance.CarrySpeed * (HasHeld ? 1f : Balance.FreeSpeedMul) * Math.Min(1f, ml);
                MoveCourier(dir * (speed * dt));
                PickUp();
                c.State = HasHeld ? CourierState.Carrying : CourierState.Walking;
            }
            else if (!awaitMove)
            {
                c.IdleT += dt;
                if (c.State != CourierState.Planted && c.IdleT >= Balance.PlantDelay - 1e-4f) Plant();
            }
            if (!Running) return;
            foreach (Cargo g in Cargos) if (g.State == CargoState.Held) g.Pos = c.Pos;

            if (c.State == CourierState.Planted) Attack();
            UpdateChest();
            UpdateShots();
            UpdateEnemies();
            Enemies.RemoveAll(IsDead);
            if (!Running) return;
            UpdateCargos();
            UpdatePet();
            if (!Running) return;
            UpdateDoors();
            if (Room.Delivery && CarryOk && V2.Dist(c.Pos, Balance.Client) <= Balance.DeliverRange)
            {
                Emit(Ev.Deliver, Contract.Index, 0);
                Finish(Outcome.Delivered);
            }
            Hint = ComputeHint();
        }

        internal void Finish(Outcome o)
        {
            if (!Running) return;
            Outcome = o;
            CloseRoom();   // a sala onde o contrato acabou (morte, roubo) tambem conta para o H1
        }

        void CloseRoom()
        {
            if (!Room.Normal) return;
            RoomsNormal++;
            if (FoughtHere) RoomsFought++;
            Emit(Ev.RoomFightChoice, FoughtHere ? 1 : 0, Room.Id);
        }

        // ------------------------------------------------------------------ courier

        void MoveCourier(V2 delta)
        {
            var c = Courier;
            V2 p = c.Pos + delta;
            int touching = -1;
            if (p.Y >= Balance.RoomH - Balance.CourierR - 0.02f)
                foreach (Door d in Doors)
                    if (Math.Abs(p.X - d.Pos.X) <= Balance.DoorHalf)
                    {
                        if (d.Open) { GoThrough(d); return; }
                        touching = d.Index;
                        if (lastBlocked != d.Index) Emit(Ev.DoorBlocked, d.Index, (int)(d.Locked ? Hint.GuardDoor : Hatched ? Hint.WaitPet : Hint.DoorNeedsCargo));
                    }
            lastBlocked = touching;
            Collide(Room, ref p, Balance.CourierR);
            c.Pos = p;
        }

        void GoThrough(Door d)
        {
            CloseRoom();
            Emit(Ev.RouteChoice, d.Index, d.Room);
            foreach (Cargo g in Cargos) if (g.State != CargoState.Held) g.State = CargoState.Gone;   // resgate deixado para tras
            Layer++;
            EnterRoom(d.Room, false);
            Sim.Dirty = true;
        }

        void Plant()
        {
            var c = Courier;
            c.State = CourierState.Planted;
            Emit(Ev.Plant);
            int k = 0;
            foreach (Cargo g in Cargos)
            {
                if (g.State != CargoState.Held) continue;
                V2 p = c.Pos + c.Facing.Right * (k == 0 ? Balance.PlantOffset : -Balance.PlantOffset);
                g.Pos = FreeNear(Room, p, Balance.CargoR);
                g.State = CargoState.Ground; g.AloneT = 0f;
                Emit(Ev.CargoDrop, g.Id, 0, g.Pos);
                k++;
            }
        }

        void PickUp()
        {
            foreach (Cargo g in Cargos)
                if (g.State == CargoState.Ground && V2.Dist(g.Pos, Courier.Pos) <= Balance.PickupRange + 1e-4f)
                {
                    g.State = CargoState.Held; g.AloneT = 0f;
                    Emit(Ev.CargoPickup, g.Id, 0, g.Pos);
                }
        }

        void StartDash(V2 dir)
        {
            var c = Courier;
            foreach (Cargo g in Cargos)
                if (g.State == CargoState.Held)
                {
                    g.State = CargoState.Ground; g.Pos = c.Pos; g.AloneT = 0f;   // larga no ponto de partida
                    Emit(Ev.CargoDrop, g.Id, 1, g.Pos);
                    if (g.Type == CargoType.Relic) Crack(g);   // reliquia que cai racha (pousar nao: e pousar com cuidado)
                }
            c.State = CourierState.Dashing; c.DashDir = dir; c.Facing = dir; c.DashT = Balance.DashTime;
            c.DashCd = c.DashCdMax; c.DashCount++;
            Emit(Ev.Dash);
        }

        void PushEnemies()
        {
            var c = Courier;
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive || e.Def.Locks || e.PushedDash == c.DashCount) continue;
                if (V2.Dist(e.Pos, c.Pos) > e.Def.Radius + Balance.CourierR + Balance.DashPushReach) continue;
                V2 away = (e.Pos - c.Pos).Norm;
                if (away.Len < 0.5f) away = c.DashDir;
                e.Pos = e.Pos + away * Balance.DashPush;
                if (e.Type == EnemyType.Bat) ClampRoom(ref e.Pos, e.Def.Radius); else Collide(Room, ref e.Pos, e.Def.Radius);
                e.PushedDash = c.DashCount;
                if (e.Carry != null) DropFromThief(e, 2);   // o dash e um golpe: o ladrao derruba a carga
                if (e.State == EnemyState.Grab) { e.GrabT = 0f; e.State = EnemyState.Chase; }
                if (e.State == EnemyState.Telegraph) { e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown * 0.5f; }
            }
        }

        void Attack()
        {
            var c = Courier;
            if (c.ShotCd > 0f) return;
            Enemy best = null; float bd = Balance.ShotRange + 1e-4f;
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive) continue;
                float d = V2.Dist(e.Pos, c.Pos);
                if (d <= bd) { bd = d; best = e; }
            }
            if (best == null) return;
            Shots.Add(new Shot { Pos = c.Pos, Target = best });
            c.ShotCd = Balance.ShotCooldown;
            Emit(Ev.Shot, best.Id, 0);
        }

        void UpdateShots()
        {
            float step = Balance.ShotSpeed * Balance.Step;
            for (int i = Shots.Count - 1; i >= 0; i--)
            {
                Shot s = Shots[i];
                if (!s.Target.Alive) { Shots.RemoveAt(i); continue; }
                V2 d = s.Target.Pos - s.Pos; float len = d.Len;
                if (len <= step + s.Target.Def.Radius) { Shots.RemoveAt(i); Hit(s.Target, Balance.ShotDamage); }
                else s.Pos = s.Pos + d * (step / len);
            }
        }

        void Hit(Enemy e, int dmg)
        {
            if (!e.Alive) return;
            e.Hp -= dmg;
            Emit(Ev.EnemyHit, e.Id, Math.Max(0, e.Hp), e.Pos);
            if (e.Carry != null) DropFromThief(e, 2);
            if (e.Hp <= 0)
            {
                Emit(Ev.EnemyDie, e.Id, (int)e.Type, e.Pos);
                if (!e.Def.Locks) FoughtHere = true;
            }
        }

        /// <summary>cargoHit = o golpe ja rachou a carga nas maos (Bruto pelas costas): a reliquia nao racha duas vezes no mesmo golpe.</summary>
        void DamageCourier(int dmg, bool cargoHit = false)
        {
            var c = Courier;
            if (c.State == CourierState.Dead) return;
            c.Hp -= dmg;
            Emit(Ev.DamageTaken, dmg, Math.Max(0, c.Hp));
            Sim.Dirty = true;
            if (c.Hp <= 0)
            {
                c.Hp = 0; c.State = CourierState.Dead; DeathRoom = Room.Id;
                Emit(Ev.DeathRoom, Room.Id, 0);
                Finish(Outcome.Died);
                return;
            }
            if (!cargoHit)   // qualquer golpe no courier (toque de morcego inclusive) racha a reliquia que ele carrega
                foreach (Cargo g in Cargos) if (g.State == CargoState.Held && g.Type == CargoType.Relic) Crack(g);
        }

        void DamagePet()
        {
            if (Pet == null || Pet.Hp <= 0) return;
            Pet.Hp--;
            Emit(Ev.PetHit, 0, Pet.Hp, Pet.Pos);
            Sim.Dirty = true;
            if (Pet.Hp <= 0) Finish(Outcome.PetDied);
        }

        void Crack(Cargo g)
        {
            if (g.State == CargoState.Gone) return;
            g.Cracks++;
            Emit(Ev.CargoIntegrity, g.Id, g.Cracks, g.Pos);
            Sim.Dirty = true;
            if (g.Type == CargoType.Relic && g.Cracks >= Balance.RelicBreak)
            {
                g.State = CargoState.Gone;
                Emit(Ev.CargoBroken, g.Id, 0, g.Pos);
                if (g == Main) Finish(Outcome.CargoBroken);
            }
        }

        // ------------------------------------------------------------------ inimigos

        void UpdateEnemies()
        {
            for (int i = 0; i < Enemies.Count && Running; i++)
            {
                Enemy e = Enemies[i];
                if (!e.Alive) continue;
                e.Cd = Math.Max(0f, e.Cd - Balance.Step);
                if (e.Type == EnemyType.Bat) Bat(e);
                else if (e.Type == EnemyType.Thief) Thief(e);
                else Melee(e);
            }
            if (Room.Boss && Running && AliveLocker() != null)
            {
                summonT += Balance.Step;
                if (summonT >= Balance.OgreSummon - 1e-4f)
                {
                    summonT = 0f;
                    if (Count(EnemyType.Thief) < Balance.MaxThieves)
                    {
                        var t = Spawn(EnemyType.Thief, new V2(rng.Float() < 0.5f ? 0.6f : Balance.RoomW - 0.6f, rng.Range(5f, 9f)));
                        Emit(Ev.ThiefSpawn, t.Id, 0, t.Pos);
                    }
                }
            }
        }

        void Bat(Enemy e)
        {
            var c = Courier;
            V2 to = c.Pos - e.Pos; float d = to.Len;
            e.State = EnemyState.Chase;
            if (d > 0.05f)
            {
                V2 dir = to * (1f / d);
                float w = Time * 0.5f + e.Phase; w -= (float)Math.Floor(w);                          // zigue-zague de morcego: onda
                V2 v = (dir + dir.Right * (0.35f * (1f - 4f * Math.Abs(w - 0.5f)))).Norm;            // triangular de 2 s (sem Math.Sin)
                e.Pos = e.Pos + v * (e.Def.Speed * Balance.Step);
                ClampRoom(ref e.Pos, e.Def.Radius);
            }
            if (e.Cd > 0f) return;
            if (c.State != CourierState.Dead && V2.Dist(e.Pos, c.Pos) <= e.Def.Radius + Balance.CourierR) { e.Cd = e.Def.Cooldown; DamageCourier(e.Def.Damage); }
            else if (Pet != null && V2.Dist(e.Pos, Pet.Pos) <= e.Def.Radius + Balance.PetR) { e.Cd = e.Def.Cooldown; DamagePet(); }
        }

        void Thief(Enemy e)
        {
            if (e.Carry != null)
            {
                e.State = EnemyState.Flee;
                if (V2.Dist(e.Pos, e.FleeGoal) > 0.25f) { e.ExitT = 0f; MoveWalker(e, e.FleeGoal, Balance.ThiefFlee); }
                else { e.ExitT += Balance.Step; if (e.ExitT >= Balance.ThiefExit - 1e-4f) { Escape(e); return; } }
                e.Carry.Pos = e.Pos;
                return;
            }
            Cargo t = null; float best = float.MaxValue;
            foreach (Cargo g in Cargos)
            {
                if (g.State != CargoState.Ground) continue;
                float d = V2.Dist(e.Pos, g.Pos);
                if (d < best) { best = d; t = g; }
            }
            if (t == null)
            {
                e.GrabT = 0f; e.State = EnemyState.Lurk;   // ronda o courier esperando a carga ir ao chao
                if (V2.Dist(e.Pos, Courier.Pos) > Balance.ThiefLurk) MoveWalker(e, Courier.Pos, e.Def.Speed);
                return;
            }
            if (best > Balance.ThiefGrabRange) { e.State = EnemyState.Chase; e.GrabT = 0f; MoveWalker(e, t.Pos, e.Def.Speed); return; }
            e.State = EnemyState.Grab;
            e.GrabT += Balance.Step;
            if (e.GrabT >= Balance.ThiefGrab - 1e-4f)
            {
                t.State = CargoState.Stolen; e.Carry = t; e.GrabT = 0f; e.ExitT = 0f;
                float side = e.Pos.X < Balance.RoomW * 0.5f ? e.Def.Radius + 0.05f : Balance.RoomW - e.Def.Radius - 0.05f;   // foge para a borda mais perto
                e.FleeGoal = FreeNear(Room, new V2(side, e.Pos.Y), e.Def.Radius);
                Emit(Ev.ThiefGrab, t.Id, e.Id, e.Pos);
            }
        }

        void DropFromThief(Enemy e, int reason)
        {
            Cargo g = e.Carry;
            e.Carry = null; e.State = EnemyState.Chase; e.ExitT = 0f;
            g.State = CargoState.Ground; g.Pos = FreeNear(Room, e.Pos, Balance.CargoR); g.AloneT = 0f;
            Emit(Ev.CargoDrop, g.Id, reason, g.Pos);
            if (g.Type == CargoType.Relic) Crack(g);   // caiu da mao do ladrao (tiro ou empurrao do dash)
        }

        void Escape(Enemy e)
        {
            Cargo g = e.Carry;
            e.Carry = null; e.Hp = 0;   // sai da sala (nao conta como luta)
            g.State = CargoState.Gone;
            Emit(Ev.ThiefEscape, g.Id, 0, e.Pos);
            Sim.Dirty = true;
            if (g == Main) Finish(Outcome.CargoStolen);
        }

        void Melee(Enemy e)
        {
            EnemyDef d = e.Def; var c = Courier;
            if (e.State == EnemyState.Telegraph)
            {
                e.Tele -= Balance.Step;
                if (e.Tele <= 1e-4f) { Strike(e); e.State = EnemyState.Recover; e.Cd = d.Cooldown; }
                return;
            }
            if (c.State == CourierState.Dead) return;
            float edge = V2.Dist(e.Pos, c.Pos) - Balance.CourierR;
            if (e.Cd <= 0f && edge <= d.Trigger)
            {
                e.State = EnemyState.Telegraph; e.Tele = d.Tele;
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            if (d.Speed > 0f && edge > d.Trigger * 0.7f) { e.State = EnemyState.Chase; MoveWalker(e, c.Pos, d.Speed); }
            else e.State = e.Cd > 0f ? EnemyState.Recover : EnemyState.Idle;
        }

        void Strike(Enemy e)
        {
            EnemyDef d = e.Def; var c = Courier;
            Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
            if (c.State != CourierState.Dead && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= d.Reach)
            {
                // golpe do Bruto pelas costas com a carga nas maos acerta tambem a carga
                bool back = e.Type == EnemyType.Brute && V2.Dot(c.Facing, e.Pos - c.Pos) < 0f;
                if (back) foreach (Cargo g in Cargos) if (g.State == CargoState.Held) Crack(g);
                DamageCourier(d.Damage, back);
            }
            if (e.Type != EnemyType.Guard)   // Bruto e Ogro acertam a carga no chao dentro do alcance
                foreach (Cargo g in Cargos) if (g.State == CargoState.Ground && V2.Dist(e.Pos, g.Pos) <= d.Reach) Crack(g);
            if (Pet != null && V2.Dist(e.Pos, Pet.Pos) - Balance.PetR <= d.Reach) DamagePet();
        }

        Enemy AliveLocker() { foreach (Enemy e in Enemies) if (e.Alive && e.Def.Locks) return e; return null; }
        int Count(EnemyType t) { int n = 0; foreach (Enemy e in Enemies) if (e.Alive && e.Type == t) n++; return n; }

        void MoveWalker(Enemy e, V2 goal, float speed)
        {
            V2 to = goal - e.Pos; float step = speed * Balance.Step;
            if (to.Len <= step) e.Pos = goal;
            else e.Pos = e.Pos + nav.Dir(e.Pos, goal) * step;
            Collide(Room, ref e.Pos, e.Def.Radius);
        }

        // ------------------------------------------------------------------ cargas, filhote, bau, portas

        void UpdateCargos()
        {
            var c = Courier;
            foreach (Cargo g in Cargos)
            {
                if (g.State != CargoState.Ground) { g.AloneT = 0f; continue; }
                if (V2.Dist(g.Pos, c.Pos) <= Balance.AloneDist) { g.AloneT = 0f; continue; }
                g.AloneT += Balance.Step;
                if (g.AloneT < Balance.AloneTime - 1e-4f) continue;
                g.AloneT = 0f;
                if (Count(EnemyType.Thief) >= Balance.MaxThieves) continue;
                float dir = g.Pos.X < Balance.RoomW * 0.5f ? -1f : 1f;   // "perto dela", do lado da parede mais proxima
                float x = Math.Max(0.6f, Math.Min(Balance.RoomW - 0.6f, g.Pos.X + dir * Balance.ThiefSpawnDist));
                if (Math.Abs(x - g.Pos.X) < 1.5f) x = g.Pos.X - dir * Balance.ThiefSpawnDist;
                var t = Spawn(EnemyType.Thief, new V2(x, g.Pos.Y));
                Emit(Ev.ThiefSpawn, t.Id, 0, t.Pos);
            }
            if (Contract.EggTarget > 0f && !Hatched && Main.State != CargoState.Gone && Time >= Contract.EggTarget - 1e-4f) Hatch();
        }

        void Hatch()
        {
            Cargo m = Main;
            V2 p = m.State == CargoState.Held ? Courier.Pos : m.Pos;
            foreach (Enemy e in Enemies) if (e.Carry == m) { e.Carry = null; e.State = EnemyState.Chase; }
            m.State = CargoState.Gone;
            Hatched = true;
            Pet = new Pet { Pos = p, Hp = Balance.PetHp };
            if (Courier.State == CourierState.Carrying && !HasHeld) Courier.State = CourierState.Walking;
            Sim.Dirty = true;
            Emit(Ev.Hatch, 0, 0, p);
        }

        void UpdatePet()
        {
            if (Pet == null) return;
            V2 to = Courier.Pos - Pet.Pos;
            if (to.Len <= Balance.PetFollow) return;
            float step = Balance.CarrySpeed * Balance.FreeSpeedMul * Balance.Step;
            Pet.Pos = Pet.Pos + nav.Dir(Pet.Pos, Courier.Pos) * Math.Min(step, to.Len - Balance.PetFollow);
            Collide(Room, ref Pet.Pos, Balance.PetR);
        }

        void UpdateChest()
        {
            if (Chest == null || Chest.Opened) return;
            var c = Courier;
            if (c.State != CourierState.Planted || V2.Dist(c.Pos, Chest.Pos) > Balance.ChestRange) { Chest.Progress = 0f; return; }
            Chest.Progress += Balance.Step;
            if (Chest.Progress < Balance.ChestTime - 1e-4f) return;
            Chest.Opened = true;
            int coins = Sim.Credit(Chest.Coins);
            ChestCoins += coins;
            Emit(Ev.ChestOpen, coins, 0, Chest.Pos);
            Emit(Ev.OptionalLoot, coins, Room.Id, Chest.Pos);
            Emit(Ev.CurrencySource, coins, 1, Chest.Pos);
            Sim.Dirty = true;
        }

        void UpdateDoors()
        {
            bool locked = AliveLocker() != null, ok = CarryOk;
            foreach (Door d in Doors) { d.Locked = locked; d.Open = !locked && ok; }
        }

        Hint ComputeHint()
        {
            var c = Courier;
            if (awaitMove && Main.State == CargoState.Held) return Hint.DragToWalk;
            foreach (Enemy e in Enemies)
                if (e.Alive && e.Type == EnemyType.Thief && (e.Carry != null || e.State == EnemyState.Grab || e.State == EnemyState.Chase)) return Hint.Thief;
            foreach (Door d in Doors)
                if (!d.Open && V2.Dist(c.Pos, d.Pos) <= 2f) return d.Locked ? Hint.GuardDoor : Hatched ? Hint.WaitPet : Hint.DoorNeedsCargo;
            if (AliveLocker() != null && Main.State == CargoState.Held && c.State != CourierState.Planted) return Hint.PlantToFight;
            foreach (Enemy e in Enemies)
                if (e.Alive && e.State == EnemyState.Telegraph && c.DashReady && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach + 0.5f) return Hint.Dash;
            if (Chest != null && !Chest.Opened && V2.Dist(c.Pos, Chest.Pos) <= 2.5f) return Hint.Chest;
            if (!Hatched && Main.State == CargoState.Ground)
            {
                foreach (Enemy e in Enemies) if (e.Alive && V2.Dist(e.Pos, c.Pos) <= Balance.ShotRange) return Hint.None;
                return Hint.PickUp;
            }
            return Hint.None;
        }

        // ------------------------------------------------------------------ geometria e navegacao

        /// <summary>Direcao (unitaria) para ir de `from` ate `to` contornando obstaculos (linha reta se livre; senao campo de fluxo).</summary>
        public V2 PathDir(V2 from, V2 to) => nav.Dir(from, to);

        public static bool Blocked(RoomDef room, V2 p, float r)
        {
            foreach (Rect o in room.Obstacles)
            {
                V2 q = o.Closest(p); float dx = p.X - q.X, dy = p.Y - q.Y;
                if (dx * dx + dy * dy < r * r) return true;
            }
            return false;
        }

        static void ClampRoom(ref V2 p, float r)
        {
            p.X = Math.Max(r, Math.Min(Balance.RoomW - r, p.X));
            p.Y = Math.Max(r, Math.Min(Balance.RoomH - r, p.Y));
        }

        /// <summary>Empurra o circulo (p, r) para fora dos obstaculos e para dentro da sala.</summary>
        public static void Collide(RoomDef room, ref V2 p, float r)
        {
            for (int pass = 0; pass < 2; pass++)
                foreach (Rect o in room.Obstacles)
                {
                    V2 q = o.Closest(p); V2 d = p - q; float l = d.Len;
                    if (l >= r) continue;
                    if (l > 1e-5f) { p = q + d * (r / l); continue; }
                    float left = p.X - (o.C.X - o.H.X), right = o.C.X + o.H.X - p.X, down = p.Y - (o.C.Y - o.H.Y), up = o.C.Y + o.H.Y - p.Y;
                    float m = Math.Min(Math.Min(left, right), Math.Min(down, up));
                    if (m == left) p.X = o.C.X - o.H.X - r;
                    else if (m == right) p.X = o.C.X + o.H.X + r;
                    else if (m == down) p.Y = o.C.Y - o.H.Y - r;
                    else p.Y = o.C.Y + o.H.Y + r;
                }
            ClampRoom(ref p, r);
        }

        /// <summary>Ponto livre mais perto de p (procura em aneis de 0,25 m).</summary>
        public static V2 FreeNear(RoomDef room, V2 p, float r)
        {
            ClampRoom(ref p, r);
            if (!Blocked(room, p, r)) return p;
            for (int k = 1; k <= 16; k++)
                foreach (V2 dir in V2.Dirs8)
                {
                    V2 q = p + dir * (0.25f * k);
                    ClampRoom(ref q, r);
                    if (!Blocked(room, q, r)) return q;
                }
            return p;
        }
    }

    /// <summary>Navegacao em grade de 0,5 m (18 x 28): linha reta quando livre; senao desce um campo BFS ate o alvo, guardado por
    /// celula-alvo (no maximo 504 campos por sala). ponytail: grade fixa e BFS 4-vizinhos; se as salas crescerem, trocar por A*.</summary>
    public sealed class Nav
    {
        public const float Cell = 0.5f; public const int W = 18, H = 28;
        const float Inflate = 0.45f;
        readonly RoomDef room;
        readonly bool[] blocked = new bool[W * H];
        readonly int[] queue = new int[W * H];
        readonly Dictionary<int, int[]> cache = new Dictionary<int, int[]>();

        public Nav(RoomDef r)
        {
            room = r;
            for (int i = 0; i < blocked.Length; i++) blocked[i] = Run.Blocked(r, Center(i), Inflate);
        }

        static V2 Center(int i) => new V2((i % W + 0.5f) * Cell, (i / W + 0.5f) * Cell);
        static int CellOf(V2 p)
        {
            int x = Math.Max(0, Math.Min(W - 1, (int)(p.X / Cell))), y = Math.Max(0, Math.Min(H - 1, (int)(p.Y / Cell)));
            return y * W + x;
        }

        /// <summary>Segmento a-b livre para um circulo de raio r?</summary>
        public bool Clear(V2 a, V2 b, float r)
        {
            V2 d = b - a; int n = (int)(d.Len / 0.25f) + 1;
            for (int i = 1; i <= n; i++) if (Run.Blocked(room, a + d * ((float)i / n), r)) return false;
            return true;
        }

        public V2 Dir(V2 from, V2 to)
        {
            if (Clear(from, to, 0.4f)) return (to - from).Norm;
            int[] f = Field(CellOf(to));
            int c = CellOf(from), cx = c % W, cy = c / W, best = -1, bv = f[c];
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                    int n = ny * W + nx;
                    if (blocked[n]) continue;
                    if (dx != 0 && dy != 0 && (blocked[cy * W + nx] || blocked[ny * W + cx])) continue;   // sem cortar quina
                    if (f[n] < bv) { bv = f[n]; best = n; }
                }
            return best < 0 ? (to - from).Norm : (Center(best) - from).Norm;
        }

        /// <summary>Distancia em passos de grade ate o alvo (int.MaxValue = inalcancavel).</summary>
        public int Steps(V2 from, V2 to) => Field(CellOf(to))[CellOf(from)];

        int[] Field(int goal)
        {
            if (cache.TryGetValue(goal, out int[] f)) return f;
            f = new int[W * H];
            for (int i = 0; i < f.Length; i++) f[i] = int.MaxValue;
            int g = goal;
            if (blocked[g])   // alvo dentro de obstaculo inflado: parte da celula livre mais perto
            {
                float bd = float.MaxValue; V2 gc = Center(goal);
                for (int i = 0; i < blocked.Length; i++) if (!blocked[i]) { float d = V2.Dist(Center(i), gc); if (d < bd) { bd = d; g = i; } }
            }
            int head = 0, tail = 0;
            f[g] = 0; queue[tail++] = g;
            while (head < tail)
            {
                int c = queue[head++], cx = c % W, cy = c / W, v = f[c] + 1;
                if (cx > 0 && Visit(f, c - 1, v)) queue[tail++] = c - 1;
                if (cx < W - 1 && Visit(f, c + 1, v)) queue[tail++] = c + 1;
                if (cy > 0 && Visit(f, c - W, v)) queue[tail++] = c - W;
                if (cy < H - 1 && Visit(f, c + W, v)) queue[tail++] = c + W;
            }
            cache[goal] = f;
            return f;
        }

        bool Visit(int[] f, int n, int v)
        {
            if (blocked[n] || f[n] <= v) return false;
            f[n] = v;
            return true;
        }
    }
}
