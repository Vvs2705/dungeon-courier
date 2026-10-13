using System;
using System.Collections.Generic;

namespace DC.Core
{
    // Regiao 2 (Minas Alagadas, CONTEUDO_REGIOES §1.1, §5, §6): inimigos, chefes, cargas e trilhos. Deterministico (rng da sala), sem Sin/Cos.
    //
    // Para a View (o que ler):
    //   run.R2Of(e) -> R2Foe (null = sem estado da regiao 2): Act = o que faz (clipe), Spot/Radius = marca no chao (circulo da picareta,
    //     monte da toupeira/Rainha, alvo da lingua, fim da faixa da investida), Dir = frente do Golem / sentido do vagonete / lingua, Rail.
    //     Toupeira e Rainha com Act Under ou Mound estao debaixo da terra (desenhe so o rastro e o monte; nao sao alvo).
    //   run.Rocks (pedras do Golem: Fall > 0 = sombra caindo; depois obstaculo 1,2 x 1,2 m por Left s), run.IceOf(carga) (gelo 0-100),
    //   run.CageLeft (s ate o grito; aviso no ultimo 1 s com anel ate Balance.CageRadius), Balance.HeartRing (anel do coracao).
    //   Golem: vida em MEIOS pontos (EnemyDef 80 = 40); a barra (Hp / Def.Hp x HpMul) continua certa.
    // Eventos novos (A/B): CageScream A=id da carga B=gritos na sala (Pos = gaiola) · CageCalm A=id da carga (o dash acalmou) · Burrow A=id
    //   (toupeira/Rainha afundou) · Summon A=id de quem chamou (Rainha, Golem) B=quantos · RockFall A=indice em run.Rocks B=0 sombra / 1 caiu.
    // Reusados: Telegraph/Strike B=(int)tipo (mineiro: aviso e impacto no circulo, Pos = circulo; toupeira/Rainha: monte e emersao; Rainha:
    //   patada; sapo: lingua; besouro: pavio; vagonete: sino e inicio da passagem; Golem: pancada e aviso da investida), Explosion A=2 (besouro),
    //   Stun A=id (Golem bateu na investida ou foi atropelado), BossPhase A=id B=2 (Rainha e Golem a 50%), CurrencySource B=10 (ima do coracao).

    /// <summary>O que o inimigo da regiao 2 esta fazendo (a View escolhe o clipe por aqui).</summary>
    public enum R2Act { None, Under, Mound, Up, Swipe, Throw, Fuse, Tongue, Tunnel, Bell, Pass, Slam, ChargeAim, Charge }

    /// <summary>Estado proprio de um inimigo da regiao 2 (run.R2Of).</summary>
    public sealed class R2Foe
    {
        public R2Act Act; public V2 Spot, Dir = new V2(0f, -1f); public float Radius;
        /// <summary>Trilho do vagonete (indice em Room.Rails).</summary>
        public int Rail = -1;
        internal float T, Up, Sw; internal int N; internal bool Twice, Phase2, Self; internal Cargo Target; internal V2 From, To;
        internal readonly List<int> HitIds = new List<int>();
    }

    /// <summary>Pedra do Golem: sombra por Fall s, depois obstaculo de 2 x Balance.RockHalf por Left s.</summary>
    public sealed class Rock { public V2 Pos; public float Fall, Left; public bool Landed => Fall <= 0f; }

    public static partial class Balance
    {
        // ---- regiao 2 (CONTEUDO_REGIOES §1.1, §5, §6). Hipoteses medidas em docs/BALANCE_REGIAO2.md.
        /// <summary>Mineiro: circulo de 0,8 m onde o courier estava; recua com o courier a 3 m.</summary>
        public const float MinerCircle = 0.8f, MinerKeep = 3f;
        /// <summary>Toupeira: monte de 0,9 m, empurra a carga 1,5 m, fica >= 2 s por baixo, espera a 1,5 m da agua.</summary>
        public const float MoleMound = 0.9f, MolePush = 1.5f, MoleUnder = 2f, MoleWaterWait = 1.5f, MoleReach = 0.4f;
        /// <summary>Rainha: monte de 1,4 m, empurra 2,5 m (+1 rachadura), 3 s por baixo (fase 2: 2 s), fora 3,5 s (2,5), 2o monte 0,6 s depois.</summary>
        public const float QueenMound = 1.4f, QueenPush = 2.5f, QueenUnder = 3f, QueenUnder2 = 2f, QueenUp2 = 2.5f, QueenSecond = 0.6f, QueenSwipe = 0.6f, QueenSwipeCd = 1f;
        public const int QueenCallEvery = 2, QueenCalls = 2, QueenMoles = 3;
        /// <summary>Sapo: puxa a carga ate 0,8 m dele (nao puxa o que ja esta a 1,2 m).</summary>
        public const float FrogPull = 0.8f, FrogNear = 1.2f;
        /// <summary>Besouro: explosao fere inimigos em 4 (o courier leva EnemyDef.Damage).</summary>
        public const int BeetleEnemyDamage = 4;
        /// <summary>Vagonete: tunel 2 s (EnemyDef.Cooldown), sino 1,2 s (Tele), passa a 6 m/s (Speed): 4,7 s por sentido num trilho de 9 m.
        /// Empurra 1,5 m para fora do trilho; inimigo leva 6 (sem escala); chefe 6 e atordoa 1,5 s.</summary>
        public const float CartPush = 1.5f, CartStun = 1.5f; public const int CartEnemyDamage = 6;
        /// <summary>Golem: gira 3 graus por passo (90 graus/s); investida: aviso 1 s, 6 m a 7 m/s; bate = atordoado 2 s; acerta o courier: 3 + 2 m.</summary>
        public const float GolemCos = 0.99862953f, GolemSin = 0.05233596f, GolemChargeTele = 1f, GolemChargeLen = 6f, GolemChargeSpeed = 7f, GolemCrash = 2f,
                           GolemChargePush = 2f, GolemChargeFrom = 2.5f, GolemCallFrom = 30f, GolemCallEvery = 12f;
        public const int GolemCalls = 2;
        /// <summary>Pedras: 2 por pancada, sombra 0,8 s, obstaculo 1,2 x 1,2 por 12 s (ou ate uma pancada a quebrar), no maximo 4, a 1,5 m+ do
        /// courier e da carga; cair no courier tira 1, na carga racha.</summary>
        public const float RockHalf = 0.6f, RockFall = 0.8f, RockLife = 12f, RockMinDist = 1.5f; public const int RockMax = 4, RockPerSlam = 2;
        /// <summary>Coracao arcano: anel de 4 m; +3 moedas (x Bolsa) por inimigo derrotado no anel, teto de 30 por visita de sala.</summary>
        public const float HeartRing = 4f; public const int HeartCoins = 3, HeartCoinCap = 30;
        /// <summary>Cristal de gelo (0-100): agua +10/s; a 1,5 m de lava x3; golpe na carga -10; integridade >= 70 = 0 rachadura, >= 40 = 1, senao 2.</summary>
        public const float IceWater = 10f, IceLavaMul = 3f, IceLavaDist = 1.5f, IceHit = 10f, IceIntact = 70f, IceCracked = 40f;
        /// <summary>Gaiola: grita a cada 6 s (aviso no ultimo 1 s); onda de 5 m acorda e atrai por 3 s; o 2o grito na sala chama o reforco.</summary>
        public const float CageEvery = 6f, CageWarn = 1f, CageRadius = 5f, CageLure = 3f;
    }

    public sealed partial class Run
    {
        readonly Dictionary<int, R2Foe> foes = new Dictionary<int, R2Foe>();
        readonly Dictionary<int, float> ice = new Dictionary<int, float>();
        readonly Dictionary<int, float> lure = new Dictionary<int, float>();
        /// <summary>Pedras do Golem nesta sala.</summary>
        public readonly List<Rock> Rocks = new List<Rock>();
        float roomT0, cageT, bossCallT; int cageScreams, heartCoins, heartWaves, dashSeen; bool cageHeld, fullHit;

        /// <summary>Estado da regiao 2 do inimigo (null = nenhum).</summary>
        public R2Foe R2Of(Enemy e) => foes.TryGetValue(e.Id, out R2Foe f) ? f : null;
        /// <summary>Gelo do cristal (0-100; 100 nas outras cargas).</summary>
        public float IceOf(Cargo g) => ice.TryGetValue(g.Id, out float v) ? v : 100f;
        /// <summary>Segundos ate o proximo grito da gaiola (-1 = sem gaiola em jogo).</summary>
        public float CageLeft => Live(CargoType.GriffinCage) != null ? Math.Max(0f, Balance.CageEvery - cageT) : -1f;
        /// <summary>Debaixo da terra (toupeira/Rainha): so o rastro aparece; nao e alvo.</summary>
        public bool Underground(Enemy e)
        {
            if (e.Type != EnemyType.Mole && e.Type != EnemyType.MoleQueen) return false;
            R2Foe f = R2Of(e);
            return f == null || f.Act == R2Act.Under || f.Act == R2Act.Mound;   // nasce por baixo
        }

        R2Foe Foe(Enemy e)
        {
            if (foes.TryGetValue(e.Id, out R2Foe f)) return f;
            f = new R2Foe { Act = e.Type == EnemyType.Mole || e.Type == EnemyType.MoleQueen ? R2Act.Under : R2Act.None };
            foes[e.Id] = f;
            return f;
        }

        Cargo Live(CargoType t) { foreach (Cargo g in Cargos) if (g.Type == t && g.State != CargoState.Gone) return g; return null; }
        V2 PosOf(Cargo g) => g.State == CargoState.Held ? Courier.Pos : g.Pos;
        int FullHp(Enemy e) => Math.Max(1, (int)Math.Round(e.Def.Hp * Balance.HpMul(Tier)));

        // ------------------------------------------------------------------ ganchos

        partial void EnterRoomR2()
        {
            foes.Clear(); lure.Clear(); Rocks.Clear();
            roomT0 = Time; cageT = 0f; bossCallT = 0f; cageScreams = 0; heartCoins = 0; heartWaves = 0;
            dashSeen = Courier.DashCount; cageHeld = Holds(CargoType.GriffinCage);
            foreach (Cargo g in Cargos)   // gelo novo cheio; retomado pelo save: o topo da faixa das rachaduras gravadas
                if (g.Type == CargoType.IceCrystal && !ice.ContainsKey(g.Id)) ice[g.Id] = g.Cracks <= 0 ? 100f : g.Cracks == 1 ? Balance.IceIntact - 0.1f : Balance.IceCracked - 0.1f;
            Rail[] rails = Room.Rails;
            foreach (Enemy e in Enemies)
            {
                if (e.Type != EnemyType.Minecart || rails.Length == 0) continue;
                int best = 0; float bd = float.MaxValue;
                for (int i = 0; i < rails.Length; i++) { float d = V2.SegDist(e.Pos, rails[i].A, rails[i].B); if (d < bd) { bd = d; best = i; } }
                R2Foe f = Foe(e); Rail r = rails[best];
                bool atA = V2.Dist(e.Pos, r.A) <= V2.Dist(e.Pos, r.B);
                f.Rail = best; f.From = atA ? r.A : r.B; f.To = atA ? r.B : r.A; f.Act = R2Act.Tunnel; f.Spot = f.From;
                e.Pos = f.From;
            }
        }

        partial void EnemyR2(Enemy e, ref bool handled)
        {
            switch (e.Type)
            {
                case EnemyType.GoblinMiner: Miner(e); break;
                case EnemyType.Mole: Mole(e); break;
                case EnemyType.MoleQueen: Queen(e); break;
                case EnemyType.BogFrog: Frog(e); break;
                case EnemyType.BombBeetle: Beetle(e); break;
                case EnemyType.Minecart: Cart(e); break;
                case EnemyType.StoneGolem: Golem(e); break;
                default: return;
            }
            handled = true;
        }

        /// <summary>Intocavel: vagonete; toupeira e Rainha debaixo da terra. Golem (vida em meios pontos): de frente x1 (= metade), pelas costas ou
        /// atordoado x4 (= x2), atropelado x2 (6 cheios).</summary>
        partial void HitMulR2(Enemy e, ref int mul)
        {
            if (e.Type == EnemyType.Minecart) { mul = 0; return; }
            if (Underground(e)) { mul = 0; return; }
            if (e.Type != EnemyType.StoneGolem) return;
            if (fullHit) { mul *= 2; return; }
            R2Foe f = R2Of(e);
            bool back = f != null && V2.Dot(f.Dir, Courier.Pos - e.Pos) < 0f;
            if (e.Stun > 0f || back) mul *= 4;
        }

        /// <summary>Coracao no chao puxa os perseguidores do anel; o grito da gaiola puxa quem ouviu por 3 s.</summary>
        partial void TargetR2(Enemy e, ref V2 t)
        {
            if (lure.TryGetValue(e.Id, out float lt) && lt > 0f) { Cargo cage = Live(CargoType.GriffinCage); if (cage != null) { t = PosOf(cage); return; } }
            if (!HeartChaser(e.Type)) return;
            foreach (Cargo g in Cargos)
                if (g.Type == CargoType.ArcaneHeart && g.State == CargoState.Ground && V2.Dist(e.Pos, g.Pos) <= Balance.HeartRing) { t = g.Pos; return; }
        }

        static bool HeartChaser(EnemyType t) => t == EnemyType.Bat || t == EnemyType.Slime || t == EnemyType.BombBeetle || t == EnemyType.Skeleton
                                                || t == EnemyType.Mummy || t == EnemyType.Salamander || t == EnemyType.MagmaShard;

        partial void EnemyDiedR2(Enemy e)
        {
            if (e.Type == EnemyType.BombBeetle) { R2Foe f = Foe(e); if (!f.Self) { f.Self = true; BeetleBlast(e.Pos); } }   // morto por tiro: explode na hora
            Cargo h = Live(CargoType.ArcaneHeart);   // ima de moedas
            if (h != null && heartCoins < Balance.HeartCoinCap && V2.Dist(PosOf(h), e.Pos) <= Balance.HeartRing)
            {
                heartCoins += Balance.HeartCoins;
                int k = Sim.Credit(Balance.HeartCoins);
                HuntCoins += k;   // moedas por inimigo derrotado (Cacador e ima do coracao) entram no Total
                Emit(Ev.CurrencySource, k, 10, e.Pos);
            }
        }

        partial void StepR2()
        {
            const float dt = Balance.Step;
            if (lure.Count > 0) { var ks = new List<int>(lure.Keys); foreach (int k in ks) { float v = lure[k] - dt; if (v <= 0f) lure.Remove(k); else lure[k] = v; } }
            if (Rocks.Count > 0) UpdateRocks();
            if (!Running) return;
            Cargo h = Live(CargoType.ArcaneHeart);
            if (h != null)
            {
                V2 hp = PosOf(h);
                foreach (Enemy e in Enemies) if (e.Alive && e.Asleep && V2.Dist(e.Pos, hp) <= Balance.HeartRing) Wake(e);   // dormindo no anel acorda
                if (Contract.Cargo == CargoType.ArcaneHeart && Waves > heartWaves) { heartWaves = Waves; if (Room.Wave.Length > 0) pending.Add(Room.Wave[0]); }   // horda maior
            }
            UpdateCage();
            UpdateIce();
        }

        partial void HintR2(ref Hint h)
        {
            if (h == Hint.DragToWalk) return;
            var c = Courier;
            foreach (Enemy e in Enemies)
            {
                R2Foe f = R2Of(e);
                if (f == null || !e.Alive) continue;
                if (e.Type == EnemyType.Minecart && (f.Act == R2Act.Bell || f.Act == R2Act.Pass) && RailDist(f.Rail, c.Pos) <= e.Def.Radius + Balance.CourierR + 0.6f) { h = Hint.CartBell; return; }
                if (e.Type == EnemyType.BombBeetle && f.Act == R2Act.Fuse && V2.Dist(e.Pos, c.Pos) <= e.Def.Reach + Balance.CourierR + 0.8f) { h = Hint.BeetleFuse; return; }
            }
            if (h == Hint.Dash && !DashThreat()) h = Hint.None;   // o Dash do nucleo mede pelo alcance do inimigo; o mineiro (6 m) e o monte sao no chao
            if (h != Hint.None && h != Hint.PickUp) return;
            foreach (Cargo g in Cargos)
            {
                if (g.State != CargoState.Ground) continue;
                foreach (Enemy e in Enemies)
                {
                    if (!e.Alive) continue;
                    if (e.Type == EnemyType.BogFrog && V2.Dist(e.Pos, g.Pos) <= e.Def.Reach + 0.5f) { h = Hint.FrogTongue; return; }
                    if ((e.Type == EnemyType.Mole || e.Type == EnemyType.MoleQueen) && Underground(e) && !InWater(g.Pos)) { h = Hint.MoleTrail; return; }
                }
            }
            Cargo cage = Live(CargoType.GriffinCage);
            if (cage != null && cageT >= Balance.CageEvery - Balance.CageWarn) { h = Hint.CageScream; return; }
            Cargo ic = Live(CargoType.IceCrystal);
            if (ic != null && IceOf(ic) < Balance.IceIntact && Room.Water.Length > 0 && !InWater(c.Pos)) { h = Hint.IceMelt; return; }
            Cargo heart = Live(CargoType.ArcaneHeart);
            if (heart != null && heart.State == CargoState.Ground)
                foreach (Enemy e in Enemies) if (e.Alive && HeartChaser(e.Type) && V2.Dist(e.Pos, heart.Pos) <= Balance.HeartRing) { h = Hint.HeartPull; return; }
            if (h == Hint.None && Room.Water.Length > 0 && InWater(c.Pos) && Main.State == CargoState.Held) h = Hint.Water;
        }

        /// <summary>O Dash do nucleo vale? Inimigo de outra regiao no criterio do nucleo, ou um aviso da regiao 2 que pega o courier.</summary>
        bool DashThreat()
        {
            var c = Courier;
            if (!c.DashReady) return false;
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive || e.State != EnemyState.Telegraph || e.Type == EnemyType.Archer) continue;
                R2Foe f = R2Of(e);
                if (f == null) { if (V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach + 0.5f) return true; continue; }
                if ((f.Act == R2Act.Throw || f.Act == R2Act.Mound) && V2.Dist(f.Spot, c.Pos) <= f.Radius + Balance.CourierR + 0.3f) return true;
                if ((f.Act == R2Act.Slam || f.Act == R2Act.Swipe) && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach + 0.5f) return true;
                if (f.Act == R2Act.ChargeAim && V2.SegDist(c.Pos, e.Pos, f.Spot) <= e.Def.Radius + Balance.CourierR + 0.3f) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ inimigos

        /// <summary>Goblin mineiro: persegue ate ter o courier a 6 m; aviso 0,9 s no circulo de 0,8 m onde o courier estava (a picareta vai em
        /// arco: sem linha de visada); acerta courier, carga no chao e filhote no circulo; recua com o courier a 3 m.</summary>
        void Miner(Enemy e)
        {
            R2Foe f = Foe(e); var c = Courier; const float dt = Balance.Step;
            if (f.Act == R2Act.Throw)
            {
                e.State = EnemyState.Telegraph;
                f.T -= dt;
                if (f.T > 1e-4f) return;
                Emit(Ev.Strike, e.Id, (int)e.Type, f.Spot);
                if (c.State != CourierState.Dead && V2.Dist(c.Pos, f.Spot) <= f.Radius + Balance.CourierR) DamageCourier(e.Def.Damage);
                foreach (Cargo g in Cargos) if (g.State == CargoState.Ground && V2.Dist(g.Pos, f.Spot) <= f.Radius + Balance.CargoR) Crack(g);
                if (Pet != null && V2.Dist(Pet.Pos, f.Spot) <= f.Radius + Balance.PetR) DamagePet();
                f.Act = R2Act.None; e.Cd = e.Def.Cooldown; e.State = EnemyState.Recover;
                return;
            }
            if (c.State == CourierState.Dead) return;
            float dc = V2.Dist(e.Pos, c.Pos);
            if (dc < Balance.MinerKeep) { e.State = EnemyState.Flee; MoveWalker(e, e.Pos + (e.Pos - c.Pos).Norm * 1.5f, e.Def.Speed); return; }
            if (e.Cd > 0f) { e.State = EnemyState.Recover; return; }
            if (dc - Balance.CourierR <= e.Def.Trigger)
            {
                f.Act = R2Act.Throw; f.T = e.Def.Tele; f.Spot = c.Pos; f.Radius = Balance.MinerCircle; e.Aim = (c.Pos - e.Pos).Norm;
                e.State = EnemyState.Telegraph; e.Tele = e.Def.Tele;
                Emit(Ev.Telegraph, e.Id, (int)e.Type, f.Spot);
                return;
            }
            e.State = EnemyState.Chase;
            MoveWalker(e, ChaseTarget(e), e.Def.Speed);
        }

        /// <summary>Alvo da toupeira/Rainha: a carga no chao fora da agua; senao o courier.</summary>
        V2 DigTarget()
        {
            foreach (Cargo g in Cargos) if (g.State == CargoState.Ground && !InWater(g.Pos)) return g.Pos;
            return Courier.Pos;
        }

        /// <summary>Distancia de p ate a agua (0 dentro).</summary>
        float WaterDist(V2 p) { float d = float.MaxValue; foreach (Rect w in Room.Water) d = Math.Min(d, V2.Dist(p, w.Closest(p))); return d; }

        void Dig(Enemy e, V2 goal, float speed, bool waitAtWater)
        {
            V2 to = goal - e.Pos; float len = to.Len, step = speed * Balance.Step;
            if (len <= 1e-4f) return;
            V2 next = len <= step ? goal : e.Pos + to * (step / len);
            if (waitAtWater && InWater(goal) && WaterDist(next) < Balance.MoleWaterWait) return;   // na agua nao sai: espera a 1,5 m da borda
            e.Pos = next;
            ClampRoom(ref e.Pos, e.Def.Radius);
        }

        /// <summary>Toupeira: por baixo (intocavel) ate a carga no chao fora da agua, senao o courier; monte 0,8 s; emerge (courier 1 de dano;
        /// carga empurrada 1,5 m para longe do courier, relíquia/bolo/espelho racham); 2 s fora e vulneravel; mergulha e fica >= 2 s por baixo.</summary>
        void Mole(Enemy e)
        {
            R2Foe f = Foe(e); const float dt = Balance.Step;
            switch (f.Act)
            {
                case R2Act.Under:
                    e.State = EnemyState.Lurk;
                    f.T += dt;
                    V2 goal = DigTarget();
                    Dig(e, goal, e.Def.Speed, true);
                    if (f.T >= Balance.MoleUnder - 1e-4f && !InWater(goal) && V2.Dist(e.Pos, goal) <= Balance.MoleReach)
                    {
                        f.Act = R2Act.Mound; f.T = e.Def.Tele; f.Spot = goal; f.Radius = Balance.MoleMound;
                        e.State = EnemyState.Telegraph; e.Tele = e.Def.Tele;
                        Emit(Ev.Telegraph, e.Id, (int)e.Type, f.Spot);
                    }
                    break;
                case R2Act.Mound:
                    e.State = EnemyState.Telegraph;
                    f.T -= dt;
                    if (f.T > 1e-4f) break;
                    Emerge(e, f, e.Def.Damage, Balance.MolePush, false);
                    f.Act = R2Act.Up; f.T = e.Def.Cooldown; e.State = EnemyState.Recover;
                    break;
                default:   // Up
                    e.State = EnemyState.Recover;
                    f.T -= dt;
                    if (f.T > 1e-4f) break;
                    f.Act = R2Act.Under; f.T = 0f; e.State = EnemyState.Lurk;
                    Emit(Ev.Burrow, e.Id, 0, e.Pos);
                    break;
            }
        }

        /// <summary>Emersao no monte f.Spot: courier no monte leva dmg; carga no chao no monte (fora da agua) e empurrada para longe do courier
        /// (crackAll: qualquer carga racha; senao so as que racham ao cair).</summary>
        void Emerge(Enemy e, R2Foe f, int dmg, float push, bool crackAll)
        {
            var c = Courier;
            e.Pos = f.Spot; ClampRoom(ref e.Pos, e.Def.Radius);
            Emit(Ev.Strike, e.Id, (int)e.Type, f.Spot);
            if (c.State != CourierState.Dead && V2.Dist(c.Pos, f.Spot) <= f.Radius + Balance.CourierR) DamageCourier(dmg);
            if (!Running) return;
            foreach (Cargo g in Cargos)
            {
                if (g.State != CargoState.Ground || InWater(g.Pos) || V2.Dist(g.Pos, f.Spot) > f.Radius + Balance.CargoR) continue;
                V2 away = (g.Pos - c.Pos).Norm; if (away.Len < 0.5f) away = (g.Pos - f.Spot).Norm; if (away.Len < 0.5f) away = V2.Up;
                g.Pos = FreeNear(Room, g.Pos + away * push, Balance.CargoR);
                if (crackAll || g.Type == CargoType.Relic || g.Type == CargoType.Cake || g.Type == CargoType.Mirror) Crack(g);
            }
            if (Pet != null && V2.Dist(Pet.Pos, f.Spot) <= f.Radius + Balance.PetR) DamagePet();
        }

        /// <summary>Rainha Toupeira (M2): 3 s por baixo -> monte 1,0 s de 1,4 m sob o alvo (na agua: na borda seca mais perto) -> emerge (courier 2;
        /// carga empurrada 2,5 m + 1 rachadura) -> 3,5 s fora (patada a 1,2 m: aviso 0,6 s, 1 de dano). A cada 2a emersao chama 2 toupeiras (no
        /// maximo 3). Fase 2 (<= 50%): 2 s por baixo, emerge 2 vezes (o 2o monte 0,6 s depois), 2,5 s fora.</summary>
        void Queen(Enemy e)
        {
            R2Foe f = Foe(e); var c = Courier; const float dt = Balance.Step;
            if (!f.Phase2 && e.Hp * 2 <= FullHp(e)) { f.Phase2 = true; Emit(Ev.BossPhase, e.Id, 2, e.Pos); }
            switch (f.Act)
            {
                case R2Act.Under:
                    e.State = EnemyState.Lurk;
                    f.T += dt;
                    V2 goal = DigTarget();
                    Dig(e, goal, e.Def.Speed, false);
                    if (f.T >= (f.Phase2 ? Balance.QueenUnder2 : Balance.QueenUnder) - 1e-4f) Mound(e, f, e.Def.Tele);
                    break;
                case R2Act.Mound:
                    e.State = EnemyState.Telegraph;
                    f.T -= dt;
                    if (f.T > 1e-4f) break;
                    Emerge(e, f, 2, Balance.QueenPush, true);
                    if (!Running) return;
                    f.N++;
                    if (f.N % Balance.QueenCallEvery == 0)
                    {
                        int n = 0;
                        for (int k = 0; k < Balance.QueenCalls; k++)
                            if (Count(EnemyType.Mole) < Balance.QueenMoles && CanSpawn(EnemyType.Mole)) { Spawn(EnemyType.Mole, e.Pos + new V2(k == 0 ? -1.2f : 1.2f, 0.4f)); n++; }
                        Emit(Ev.Summon, e.Id, n, e.Pos);
                    }
                    if (f.Phase2 && !f.Twice) { f.Twice = true; Mound(e, f, Balance.QueenSecond); break; }
                    f.Twice = false; f.Act = R2Act.Up; f.Up = f.Phase2 ? Balance.QueenUp2 : e.Def.Cooldown; f.Sw = 0f; e.State = EnemyState.Recover;
                    break;
                default:   // Up / Swipe (patada)
                    f.Up -= dt;
                    if (f.Sw > 0f)
                    {
                        f.Sw -= dt; e.State = EnemyState.Telegraph;
                        if (f.Sw <= 1e-4f)
                        {
                            f.Sw = 0f; f.Act = R2Act.Up; e.Cd = Balance.QueenSwipeCd;
                            Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
                            if (c.State != CourierState.Dead && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach) DamageCourier(1);
                            if (!Running) return;
                        }
                    }
                    else if (e.Cd <= 0f && c.State != CourierState.Dead && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Trigger)
                    {
                        f.Act = R2Act.Swipe; f.Sw = Balance.QueenSwipe; e.State = EnemyState.Telegraph; e.Tele = Balance.QueenSwipe;
                        Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                    }
                    else e.State = EnemyState.Recover;
                    if (f.Up <= 1e-4f && f.Sw <= 0f) { f.Act = R2Act.Under; f.T = 0f; e.State = EnemyState.Lurk; Emit(Ev.Burrow, e.Id, 0, e.Pos); }
                    break;
            }
        }

        /// <summary>Monte da Rainha sob o alvo; na agua, na borda seca mais perto.</summary>
        void Mound(Enemy e, R2Foe f, float tele)
        {
            V2 t = DigTarget();
            if (InWater(t)) t = DryEdge(t);
            f.Act = R2Act.Mound; f.T = tele; f.Spot = t; f.Radius = Balance.QueenMound;
            e.State = EnemyState.Telegraph; e.Tele = tele;
            Emit(Ev.Telegraph, e.Id, (int)e.Type, t);
        }

        V2 DryEdge(V2 p)
        {
            V2 best = p; float bd = float.MaxValue;
            foreach (Rect w in Room.Water)
            {
                if (w.Closest(p).X != p.X || w.Closest(p).Y != p.Y) continue;
                V2[] cand = { new V2(w.C.X - w.H.X - 0.75f, p.Y), new V2(w.C.X + w.H.X + 0.75f, p.Y), new V2(p.X, w.C.Y - w.H.Y - 0.75f), new V2(p.X, w.C.Y + w.H.Y + 0.75f) };
                foreach (V2 q0 in cand)
                {
                    V2 q = q0; ClampRoom(ref q, 0.8f);
                    if (InWater(q) || Blocked(Room, q, 0.5f)) continue;
                    float d = V2.Dist(q, p);
                    if (d < bd) { bd = d; best = q; }
                }
            }
            return best;
        }

        /// <summary>Sapo do lodo: parado. Carga no chao a ate 3 m (e a mais de 1,2 m) com linha livre: aviso 0,7 s (linha) e a lingua a puxa ate
        /// 0,8 m dele (nao racha; Assobio/fumaca valem); sem carga no chao, a lingua acerta o courier a ate 3 m (1 de dano).</summary>
        void Frog(Enemy e)
        {
            R2Foe f = Foe(e); var c = Courier;
            if (f.Act == R2Act.Tongue)
            {
                e.State = EnemyState.Telegraph;
                f.T -= Balance.Step;
                if (f.T > 1e-4f) return;
                f.Act = R2Act.None; e.Cd = e.Def.Cooldown; e.State = EnemyState.Recover;
                Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
                Cargo g = f.Target; f.Target = null;
                if (g != null)
                {
                    if (g.State == CargoState.Ground && V2.Dist(e.Pos, g.Pos) <= e.Def.Reach + Balance.CargoR + 0.5f)
                    {
                        g.Pos = FreeNear(Room, e.Pos + (g.Pos - e.Pos).Norm * Balance.FrogPull, Balance.CargoR);
                        CargoGrabbed(e);
                    }
                    return;
                }
                if (c.State != CourierState.Dead && V2.SegDist(c.Pos, e.Pos, e.Pos + f.Dir * e.Def.Reach) <= Balance.CourierR + 0.15f) DamageCourier(e.Def.Damage);
                return;
            }
            if (e.Cd > 0f) { e.State = EnemyState.Recover; return; }
            e.State = EnemyState.Idle;
            Cargo t = null; float bd = float.MaxValue;
            foreach (Cargo g in Cargos)
            {
                if (g.State != CargoState.Ground) continue;
                float d = V2.Dist(e.Pos, g.Pos);
                if (d - Balance.CargoR <= e.Def.Reach && d > Balance.FrogNear && d < bd && nav.ShotClear(e.Pos, g.Pos, 0.1f)) { bd = d; t = g; }
            }
            V2 at;
            if (t != null) at = t.Pos;
            else if (c.State != CourierState.Dead && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach && nav.ShotClear(e.Pos, c.Pos, 0.1f)) at = c.Pos;
            else return;
            f.Act = R2Act.Tongue; f.T = e.Def.Tele; f.Target = t; f.Spot = at; f.Dir = (at - e.Pos).Norm; e.Aim = f.Dir;
            e.State = EnemyState.Telegraph; e.Tele = e.Def.Tele;
            Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
        }

        /// <summary>Besouro-bomba: corre a 3 m/s ate a borda do courier a 0,8 m; pavio 1 s (o dash o empurra sem desarmar); explode.</summary>
        void Beetle(Enemy e)
        {
            R2Foe f = Foe(e); var c = Courier;
            if (f.Act == R2Act.Fuse)
            {
                e.State = EnemyState.Telegraph;
                f.T -= Balance.Step;
                if (f.T > 1e-4f) return;
                f.Self = true; e.Hp = 0;   // explode com ele (nao conta como derrotado)
                BeetleBlast(e.Pos);
                return;
            }
            if (c.State == CourierState.Dead) return;
            if (V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Trigger)
            {
                f.Act = R2Act.Fuse; f.T = e.Def.Tele; e.State = EnemyState.Telegraph; e.Tele = e.Def.Tele;
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            e.State = EnemyState.Chase;
            MoveWalker(e, ChaseTarget(e), e.Def.Speed);
        }

        /// <summary>Explosao do besouro (1,5 m): inimigos 4, courier 2, toda carga no chao racha (o barril detona), acorda quem dorme a 4 m.</summary>
        void BeetleBlast(V2 pos)
        {
            EnemyDef d = EnemyDef.All[(int)EnemyType.BombBeetle];
            Explode(pos, d.Reach, Balance.BeetleEnemyDamage, 2);   // inimigos, ovo/reliquia, barril, filhote, acordar
            if (!Running) return;
            if (Courier.State != CourierState.Dead && V2.Dist(Courier.Pos, pos) <= d.Reach + Balance.CourierR) DamageCourier(d.Damage);
            if (!Running) return;
            foreach (Cargo g in Cargos)
                if (g.State == CargoState.Ground && g.Type != CargoType.Egg && g.Type != CargoType.Relic && g.Type != CargoType.Barrel && V2.Dist(g.Pos, pos) <= d.Reach + Balance.CargoR) Crack(g);
        }

        /// <summary>Distancia de p ao trilho i (inf sem trilho).</summary>
        float RailDist(int i, V2 p) => i >= 0 && i < Room.Rails.Length ? V2.SegDist(p, Room.Rails[i].A, Room.Rails[i].B) : float.MaxValue;

        /// <summary>Vagonete: tunel 2 s -> sino 1,2 s -> passa a 6 m/s -> tunel do outro lado; o ciclo e funcao do tempo na sala + fase do trilho.
        /// Na passagem acerta tudo a 0,6 m dele uma vez: courier 2 e empurrao de 1,5 m para fora do trilho; carga no chao empurrada + 1 rachadura;
        /// inimigo 6 + empurrao; chefe 6 e atordoa 1,5 s. Sem trilho fica parado.</summary>
        void Cart(Enemy e)
        {
            e.Hp = e.Def.Hp;
            R2Foe f = Foe(e);
            if (f.Rail < 0 || f.Rail >= Room.Rails.Length) { e.State = EnemyState.Idle; return; }
            EnemyDef d = e.Def; Rail rail = Room.Rails[f.Rail];
            float len = V2.Dist(rail.A, rail.B), pass = len / d.Speed, leg = d.Cooldown + d.Tele + pass, u = Time - roomT0 + rail.Phase;
            u -= (float)Math.Floor(u / (2f * leg)) * 2f * leg;
            bool back = u >= leg; if (back) u -= leg;
            V2 from = back ? f.To : f.From, to = back ? f.From : f.To;
            R2Act was = f.Act;
            if (u < d.Cooldown) { f.Act = R2Act.Tunnel; e.Pos = from; e.State = EnemyState.Idle; }
            else if (u < d.Cooldown + d.Tele)
            {
                f.Act = R2Act.Bell; e.Pos = from; e.State = EnemyState.Telegraph; e.Tele = d.Cooldown + d.Tele - u;
                if (was != R2Act.Bell) Emit(Ev.Telegraph, e.Id, (int)e.Type, from);
            }
            else
            {
                f.Act = R2Act.Pass; e.State = EnemyState.Chase;
                e.Pos = from + (to - from) * Math.Min(1f, (u - d.Cooldown - d.Tele) / pass);
                if (was != R2Act.Pass) { f.HitIds.Clear(); f.HitIds.Add(e.Id); Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos); }
                CartHits(e, f, rail);
            }
            f.Dir = (to - from).Norm; f.Spot = e.Pos;
        }

        void CartHits(Enemy cart, R2Foe f, Rail rail)
        {
            var c = Courier; float r = cart.Def.Radius;
            V2 n = (rail.B - rail.A).Norm.Right;
            const int CourierKey = -1, PetKey = -2;
            if (c.State != CourierState.Dead && !f.HitIds.Contains(CourierKey) && V2.Dist(c.Pos, cart.Pos) <= r + Balance.CourierR)
            {
                f.HitIds.Add(CourierKey);
                DamageCourier(cart.Def.Damage);
                if (!Running) return;
                c.Pos = c.Pos + Side(c.Pos, rail, n) * Balance.CartPush; Collide(Room, ref c.Pos, Balance.CourierR);
            }
            foreach (Cargo g in Cargos)
            {
                int key = -100 - g.Id;
                if (g.State != CargoState.Ground || f.HitIds.Contains(key) || V2.Dist(g.Pos, cart.Pos) > r + Balance.CargoR) continue;
                f.HitIds.Add(key);
                g.Pos = FreeNear(Room, g.Pos + Side(g.Pos, rail, n) * Balance.CartPush, Balance.CargoR);
                Crack(g);
                if (!Running) return;
            }
            if (Pet != null && !f.HitIds.Contains(PetKey) && V2.Dist(Pet.Pos, cart.Pos) <= r + Balance.PetR) { f.HitIds.Add(PetKey); DamagePet(); if (!Running) return; }
            for (int i = 0; i < Enemies.Count && Running; i++)
            {
                Enemy o = Enemies[i];
                if (!o.Alive || o == cart || o.Def.Hazard || f.HitIds.Contains(o.Id) || HitMul(o) <= 0 || V2.Dist(o.Pos, cart.Pos) > r + o.Def.Radius) continue;
                f.HitIds.Add(o.Id);
                bool fought = FoughtHere;   // atropelado nao e luta escolhida (H1)
                if (o.Def.Boss) { fullHit = true; Hit(o, Balance.CartEnemyDamage); fullHit = false; if (o.Alive) { o.Stun = Math.Max(o.Stun, Balance.CartStun); Emit(Ev.Stun, o.Id, 0, o.Pos); } }
                else
                {
                    Hit(o, Balance.CartEnemyDamage);
                    if (o.Alive) { o.Pos = o.Pos + Side(o.Pos, rail, n) * Balance.CartPush; if (o.Type == EnemyType.Bat) ClampRoom(ref o.Pos, o.Def.Radius); else Collide(Room, ref o.Pos, o.Def.Radius); }
                }
                FoughtHere = fought;
            }
        }

        static V2 Side(V2 p, Rail rail, V2 n) => V2.Dot(p - rail.A, n) >= 0f ? n : n * -1f;

        /// <summary>Golem de Pedra (B2): gira 90 graus/s para o courier; pancada (gatilho 1,8 m, aviso 1,1 s, area 2,2 m, 3 de dano, racha a carga
        /// no chao, quebra as pedras na area) seguida de 2 pedras. Fase 2 (<= 50%): longe, investida (aviso 1 s, faixa de 6 m na frente, 7 m/s):
        /// bate em parede/pilar/pedra = atordoado 2 s; no courier 3 + 2 m. Com 30 s de luta chama 1 mineiro a cada 12 s (no maximo 2).</summary>
        void Golem(Enemy e)
        {
            R2Foe f = Foe(e); var c = Courier; EnemyDef d = e.Def; const float dt = Balance.Step;
            if (!f.Phase2 && e.Hp * 2 <= FullHp(e)) { f.Phase2 = true; Emit(Ev.BossPhase, e.Id, 2, e.Pos); }
            float fight = Time - roomT0;
            if (fight >= Balance.GolemCallFrom)
            {
                bossCallT += dt;
                if (bossCallT >= Balance.GolemCallEvery - 1e-4f)
                {
                    bossCallT = 0f;
                    int called = 0; foreach (Enemy o in Enemies) if (o.Alive && o.Type == EnemyType.GoblinMiner) called++;
                    if (called < Balance.GolemCalls && CanSpawn(EnemyType.GoblinMiner))
                    {
                        Spawn(EnemyType.GoblinMiner, new V2(rng.Float() < 0.5f ? 1.0f : Balance.RoomW - 1.0f, 12.4f));
                        Emit(Ev.Summon, e.Id, 1, e.Pos);
                    }
                }
            }
            if (f.Act != R2Act.ChargeAim && f.Act != R2Act.Charge && c.State != CourierState.Dead) Turn(f, c.Pos - e.Pos);
            switch (f.Act)
            {
                case R2Act.Slam:
                    e.State = EnemyState.Telegraph;
                    f.T -= dt;
                    if (f.T > 1e-4f) return;
                    Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
                    if (c.State != CourierState.Dead && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= d.Reach) DamageCourier(d.Damage);
                    if (!Running) return;
                    foreach (Cargo g in Cargos) if (g.State == CargoState.Ground && V2.Dist(e.Pos, g.Pos) <= d.Reach + Balance.CargoR) Crack(g);
                    if (Pet != null && V2.Dist(e.Pos, Pet.Pos) - Balance.PetR <= d.Reach) DamagePet();
                    if (!Running) return;
                    Rocks.RemoveAll(k => V2.Dist(k.Pos, e.Pos) <= d.Reach + Balance.RockHalf);   // a pancada quebra as pedras na area
                    DropRocks(e);
                    f.Act = R2Act.None; e.Cd = d.Cooldown; e.State = EnemyState.Recover;
                    return;
                case R2Act.ChargeAim:
                    e.State = EnemyState.Telegraph;
                    f.T -= dt;
                    if (f.T > 1e-4f) return;
                    f.Act = R2Act.Charge; f.T = Balance.GolemChargeLen / Balance.GolemChargeSpeed; e.State = EnemyState.Flee;
                    return;
                case R2Act.Charge:
                    e.State = EnemyState.Flee;
                    V2 next = e.Pos + f.Dir * (Balance.GolemChargeSpeed * dt);
                    float r = d.Radius;
                    if (c.State != CourierState.Dead && V2.Dist(next, c.Pos) <= r + Balance.CourierR)
                    {
                        DamageCourier(d.Damage);
                        if (!Running) return;
                        c.Pos = c.Pos + f.Dir * Balance.GolemChargePush; Collide(Room, ref c.Pos, Balance.CourierR);
                        f.Act = R2Act.None; e.Cd = d.Cooldown; e.State = EnemyState.Recover;
                        return;
                    }
                    if (Blocked(Room, next, r * 0.9f) || RockAt(next, r * 0.9f) || next.X < r || next.Y < r || next.X > Balance.RoomW - r || next.Y > Balance.RoomH - r)
                    {
                        f.Act = R2Act.None; e.Cd = d.Cooldown; e.Stun = Balance.GolemCrash; e.State = EnemyState.Recover;
                        Emit(Ev.Stun, e.Id, 0, e.Pos);
                        return;
                    }
                    e.Pos = next;
                    f.T -= dt;
                    if (f.T <= 1e-4f) { f.Act = R2Act.None; e.Cd = d.Cooldown; e.State = EnemyState.Recover; }
                    return;
            }
            if (c.State == CourierState.Dead) return;
            float edge = V2.Dist(e.Pos, c.Pos) - Balance.CourierR;
            if (e.Cd <= 0f && edge <= d.Trigger)
            {
                f.Act = R2Act.Slam; f.T = d.Tele; f.Radius = d.Reach; f.Spot = e.Pos; e.State = EnemyState.Telegraph; e.Tele = d.Tele;
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            if (e.Cd <= 0f && f.Phase2 && edge >= Balance.GolemChargeFrom)
            {
                f.Act = R2Act.ChargeAim; f.T = Balance.GolemChargeTele; f.Spot = e.Pos + f.Dir * Balance.GolemChargeLen; f.Radius = d.Radius;
                e.State = EnemyState.Telegraph; e.Tele = Balance.GolemChargeTele; e.Aim = f.Dir;
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            if (edge > d.Trigger * 0.7f) { e.State = EnemyState.Chase; MoveWalker(e, c.Pos, d.Speed); }
            else e.State = e.Cd > 0f ? EnemyState.Recover : EnemyState.Idle;
        }

        /// <summary>Gira a frente f.Dir no maximo 3 graus por passo na direcao de `to` (sem trigonometria: rotacao pelo cosseno/seno fixos).</summary>
        static void Turn(R2Foe f, V2 to)
        {
            V2 want = to.Norm;
            if (want.Len < 0.5f) return;
            if (V2.Dot(f.Dir, want) >= Balance.GolemCos) { f.Dir = want; return; }
            float cross = f.Dir.X * want.Y - f.Dir.Y * want.X, s = cross >= 0f ? Balance.GolemSin : -Balance.GolemSin;
            f.Dir = new V2(f.Dir.X * Balance.GolemCos - f.Dir.Y * s, f.Dir.X * s + f.Dir.Y * Balance.GolemCos).Norm;
        }

        // ------------------------------------------------------------------ pedras do Golem

        bool RockAt(V2 p, float r)
        {
            foreach (Rock k in Rocks)
            {
                if (!k.Landed) continue;
                V2 q = new Rect(k.Pos.X, k.Pos.Y, Balance.RockHalf, Balance.RockHalf).Closest(p);
                if (V2.Dist(p, q) < r) return true;
            }
            return false;
        }

        /// <summary>2 pedras em pontos livres a 1,5 m+ do courier e da carga, fora da faixa das portas, da entrada e dos trilhos (no maximo 4).</summary>
        void DropRocks(Enemy golem)
        {
            for (int k = 0; k < Balance.RockPerSlam && Rocks.Count < Balance.RockMax; k++)
                for (int tries = 0; tries < 24; tries++)
                {
                    var p = new V2(rng.Range(1f, Balance.RoomW - 1f), rng.Range(2.6f, 12.2f - Balance.RockHalf));
                    bool ok = !Blocked(Room, p, Balance.RockHalf + 0.3f) && V2.Dist(p, Courier.Pos) >= Balance.RockMinDist && V2.Dist(p, Balance.Entrance) >= 2f
                              && V2.Dist(p, golem.Pos) >= golem.Def.Radius + Balance.RockHalf + 0.3f;
                    foreach (Cargo g in Cargos) if (g.State != CargoState.Gone && V2.Dist(p, PosOf(g)) < Balance.RockMinDist) ok = false;
                    for (int i = 0; i < Room.Rails.Length; i++) if (RailDist(i, p) < 1f + Balance.RockHalf) ok = false;
                    foreach (Rock o in Rocks) if (V2.Dist(p, o.Pos) < 2f * Balance.RockHalf + 0.2f) ok = false;
                    if (!ok) continue;
                    Rocks.Add(new Rock { Pos = p, Fall = Balance.RockFall, Left = Balance.RockLife });
                    Emit(Ev.RockFall, Rocks.Count - 1, 0, p);
                    break;
                }
        }

        void UpdateRocks()
        {
            const float dt = Balance.Step;
            var c = Courier;
            for (int i = Rocks.Count - 1; i >= 0; i--)
            {
                Rock k = Rocks[i];
                if (k.Fall > 0f)
                {
                    k.Fall -= dt;
                    if (k.Fall > 1e-4f) continue;
                    k.Fall = 0f;
                    Emit(Ev.RockFall, i, 1, k.Pos);
                    if (c.State != CourierState.Dead && RockAt(c.Pos, Balance.CourierR)) DamageCourier(1);
                    if (!Running) return;
                    foreach (Cargo g in Cargos) if (g.State == CargoState.Ground && RockAt(g.Pos, Balance.CargoR)) Crack(g);
                    if (!Running) return;
                    continue;
                }
                k.Left -= dt;
                if (k.Left <= 1e-4f) Rocks.RemoveAt(i);
            }
            OutOfRocks(ref c.Pos, Balance.CourierR);
            foreach (Cargo g in Cargos) if (g.State == CargoState.Ground) OutOfRocks(ref g.Pos, Balance.CargoR);
            foreach (Enemy e in Enemies) if (e.Alive && e.Type != EnemyType.Bat && !e.Def.Hazard && !Underground(e)) OutOfRocks(ref e.Pos, e.Def.Radius);
            if (Pet != null) OutOfRocks(ref Pet.Pos, Balance.PetR);
        }

        void OutOfRocks(ref V2 p, float r)
        {
            if (!RockAt(p, r)) return;
            foreach (Rock k in Rocks) if (k.Landed) Push(ref p, new Rect(k.Pos.X, k.Pos.Y, Balance.RockHalf, Balance.RockHalf), r);
            Collide(Room, ref p, r);
        }

        // ------------------------------------------------------------------ cargas

        /// <summary>Gaiola do grifinho: grita a cada 6 s (desde a entrada ou o ultimo grito/calma); o dash com a gaiola nas maos acalma (zera).
        /// Grito: onda de 5 m acorda quem dorme e atrai os comuns (nao Guarda nem chefe) por 3 s; o 2o grito da sala chama o reforco.</summary>
        void UpdateCage()
        {
            Cargo cage = Live(CargoType.GriffinCage);
            if (cage == null) { dashSeen = Courier.DashCount; cageHeld = false; return; }
            if (Courier.DashCount != dashSeen)
            {
                dashSeen = Courier.DashCount;
                if (cageHeld) { cageT = 0f; Emit(Ev.CageCalm, cage.Id, 0, PosOf(cage)); }
            }
            cageHeld = cage.State == CargoState.Held;
            cageT += Balance.Step;
            if (cageT < Balance.CageEvery - 1e-4f) return;
            cageT = 0f; cageScreams++;
            V2 p = PosOf(cage);
            Emit(Ev.CageScream, cage.Id, cageScreams, p);
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive || e.Def.Hazard || V2.Dist(e.Pos, p) > Balance.CageRadius) continue;
                if (e.Asleep) Wake(e);
                if (!e.Def.Locks && !e.Def.Boss) lure[e.Id] = Balance.CageLure;
            }
            if (cageScreams >= 2 && Waves == 0 && Contract.Waves && Room.WaveTrigger != RoomTrigger.None && !WaveDone)   // o 2o grito chama o reforco
            {
                Waves++; downT = 0f;
                pending.AddRange(Room.Wave); pendT = 0f;
                if (Has(Modifier.Horde)) pending.Add(new Spawn(HordeType(Room.Region), 4.5f, 12f));
                Emit(Ev.RoomEvent, Room.Id, Room.Wave.Length);
            }
        }

        /// <summary>Cristal de gelo: derrete 100/MeltTime por s (x3 a 1,5 m de lava), recongela +10/s na agua (no chao dentro dela ou nas maos do
        /// courier que esta nela); cada golpe na carga tira 10 (o golpe vira rachadura no nucleo: aqui ela volta a ser gelo). As rachaduras
        /// seguem a faixa do gelo (integridade na entrega e no save). 0 (Fragil: abaixo de 40) = derreteu.</summary>
        void UpdateIce()
        {
            for (int i = 0; i < Cargos.Count && Running; i++)
            {
                Cargo g = Cargos[i];
                if (g.Type != CargoType.IceCrystal || g.State == CargoState.Gone) continue;
                float v = IceOf(g);
                int band = IceBand(v);
                if (g.Cracks > band) v -= Balance.IceHit * (g.Cracks - band);
                V2 p = PosOf(g);
                bool wet = (g.State == CargoState.Ground || g.State == CargoState.Held) && InWater(p);
                if (wet) v += Balance.IceWater * Balance.Step;
                else if (MeltTime > 0f) v -= 100f / MeltTime * (NearLava(p) ? Balance.IceLavaMul : 1f) * Balance.Step;
                v = Math.Max(0f, Math.Min(100f, v));
                ice[g.Id] = v;
                int nb = IceBand(v);
                if (nb != g.Cracks) { bool worse = nb > band; g.Cracks = nb; Sim.Dirty = true; if (worse) Emit(Ev.CargoIntegrity, g.Id, nb, p); }
                if (v > 1e-4f && v >= IceFailAt * 100f) continue;
                g.State = CargoState.Gone;
                foreach (Enemy e in Enemies) if (e.Carry == g) { e.Carry = null; e.State = EnemyState.Chase; }
                Sim.Dirty = true;
                if (g == Main) Finish(Outcome.Melted);
            }
        }

        static int IceBand(float v) => v >= Balance.IceIntact ? 0 : v >= Balance.IceCracked ? 1 : 2;

        bool NearLava(V2 p) { foreach (Rect l in Room.Lava) if (V2.Dist(p, l.Closest(p)) <= Balance.IceLavaDist) return true; return false; }

        /// <summary>Gelo gravado no save (Sim.LoadRun, antes de montar a sala): reabrir retoma com o valor exato, nao o topo da faixa (GDD §28).</summary>
        internal void SetIce(Cargo g, float v) { if (g.Type == CargoType.IceCrystal) ice[g.Id] = Math.Max(0f, Math.Min(100f, v)); }

        /// <summary>Tira gelo do cristal (fogo direto da regiao 4: respiro -25, frasco -20, sopro -40...): o UpdateIce aplica a falha no mesmo passo.</summary>
        internal void MeltIce(Cargo g, float amount) { if (g.Type == CargoType.IceCrystal) ice[g.Id] = Math.Max(0f, IceOf(g) - amount); }
    }
}
