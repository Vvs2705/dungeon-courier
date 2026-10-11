using System;
using System.Collections.Generic;

namespace DC.Core
{
    // Regiao 4 (Forja do Dragao): lava, respiros e fogo no chao (CONTEUDO_REGIOES §1.1), salamandra, diabrete, kobold incendiario, elemental
    // de magma e brasa, ovo de salamandra, Capataz Kobold e Dragao Anciao (§5), planta carnivora e mapa do tesouro (§6). Numeros em
    // Balance (fim deste arquivo); medicao em docs/BALANCE_REGIAO4.md. Tudo vale em qualquer sala que tenha o terreno/inimigo (sem fim).
    //
    // Para a View (so ler; quem muda e o Run):
    //   run.Forge.Marks   avisos no chao (ForgeMark). Kind: Flask = circulo do frasco (Pos, Radius); Whip = faixa do chicote (Pos = origem,
    //                     Dir, Length, Radius = meia-largura); Spin = giro do Capataz (circulo em Pos); Breath = cone do sopro (Pos = boca,
    //                     Dir, Length; meio-angulo de 30 graus = Balance.BreathCos); Land = onde o Dragao vai pousar (circulo); Swap =
    //                     redemoinho do diabrete em cima da carga; Blink = onde o diabrete vai surgir. Left = s ate o golpe, Total = aviso
    //                     inteiro (barra = 1 - Left/Total); Active > 0 = o sopro esta saindo (na fase 3 o Dir gira 90 graus em 1,5 s).
    //   run.Forge.Fires   fogo no chao (FirePatch: Pos, Radius, Left; Source 0 = rastro da salamandra, 1 = poca do frasco, 2 = brasa do sopro).
    //   run.Forge.Phase   fase do chefe da sala (0 = sem Capataz/Dragao; 1-3). Flying = Dragao no ar (intocavel; Enemy.Pos = a sombra);
    //                     HeadDown = s restantes da cabeca baixa depois do pouso (dano x2).
    //   run.VentState(i)  respiro i da sala: 0 apagado, 1 brilhando (aviso de 0,8 s), 2 fogo (1,0 s); VentFireIn(i) = s ate o proximo fogo.
    //                     No B4 os respiros (fissuras) so ligam na fase 2.
    //   run.NearFire(p)   a 1,5 m de lava, fogo no chao ou respiro em fogo (o cristal de gelo derrete x3: raia R2).
    //   Mapa: run.Revealed (nucleo) mostra baus enterrados e a parede disfarcada; a porta pode mostrar pa/rachadura pelos Buried/Secret da sala
    //   de destino (Content.Rooms[door.Room]).
    // Eventos (A/B; Pos = onde): BossPhase A=id do chefe B=fase nova (2-3) · TakeOff A=id do Dragao · Land A=id B=1 se acertou o courier ·
    //   PlantBite A=id do mordido (0 = courier) B=id da carga · LavaBurn A=0 inimigo/2 carga B=id · VentFire A=indice do respiro (no inicio
    //   do fogo) · EggHatch A=id do ovo B=id da salamandra · ForemanCall A=id do Capataz B=kobolds chamados. Reusados (B = (int)EnemyType):
    //   Strike Salamander = corte; Telegraph/Strike Imp = redemoinho ou sumico / troca ou surge; Telegraph KoboldBomber = arremesso;
    //   Strike KoboldBomber = frasco cai (Pos = impacto; o do Capataz tambem); Telegraph/Strike KoboldForeman = chicote ou giro;
    //   Telegraph/Strike ElderDragon = aviso / sopro; EnemyDie MagmaElemental = divide em brasas; Stun = diabrete acertado no redemoinho.
    // Gelo (raia R2): o fogo daqui chama ForgeIce(carga, pontos de 0-100) - parcial: implemente em Run.Regiao2.cs.
    // Deterministico: so o rng da sala; nada de Math.Sin/Cos (o giro do sopro usa seno/cosseno fixos de 2 graus).

    public enum ForgeMarkKind { Flask, Whip, Spin, Breath, Land, Swap, Blink }

    /// <summary>Aviso no chao da regiao 4 (ver o comentario do topo de Run.Regiao4.cs).</summary>
    public sealed class ForgeMark
    {
        public ForgeMarkKind Kind; public V2 Pos, Dir; public float Radius, Length, Left, Total, Active; public int Owner;
        internal Cargo Target; internal bool Bound, Sweep; internal int Damage; internal float Pool, Ember;
        /// <summary>Quem ja levou este golpe (0 = courier, id = inimigo, -id-1 = carga).</summary>
        internal readonly List<int> Hit = new List<int>();
    }

    /// <summary>Fogo no chao (rastro da salamandra, poca do frasco, brasa do sopro).</summary>
    public sealed class FirePatch { public V2 Pos; public float Radius, Left; public int Source; }

    /// <summary>Estado da regiao 4 lido pela View.</summary>
    public sealed class ForgeState
    {
        public readonly List<ForgeMark> Marks = new List<ForgeMark>();
        public readonly List<FirePatch> Fires = new List<FirePatch>();
        public int Phase; public bool Flying; public V2 Shadow; public float HeadDown;
    }

    public sealed partial class Run
    {
        public readonly ForgeState Forge = new ForgeState();

        sealed class Plant4 { public float Warn, Cd, Near, CourierCd, CarryT; public int Target; }
        float ventClock4; bool ventsOn4; int[] ventLast4 = new int[0]; readonly List<List<int>> ventHit4 = new List<List<int>>();
        readonly Dictionary<int, int> pushSeen4 = new Dictionary<int, int>();
        readonly Dictionary<int, float> fireCd4 = new Dictionary<int, float>();
        readonly Dictionary<int, Plant4> plant4 = new Dictionary<int, Plant4>();
        readonly List<int> called4 = new List<int>();
        readonly List<Enemy> split4 = new List<Enemy>();
        float flaskT4, callT4, eggT4, flyT4, airT4, fireStay4; int flyStage4;

        /// <summary>Gelo do cristal (raia R2): o fogo da regiao 4 tira `points` (0-100) do gelo da carga no chao.</summary>
        partial void ForgeIce(Cargo g, float points);

        partial void EnterRoomR4()
        {
            Forge.Marks.Clear(); Forge.Fires.Clear(); Forge.Phase = 0; Forge.Flying = false; Forge.HeadDown = 0f;
            ventClock4 = 0f; ventsOn4 = true;
            foreach (Enemy e in Enemies) if (e.Type == EnemyType.ElderDragon) ventsOn4 = false;   // B4: fissuras so na fase 2
            ventLast4 = new int[Room.Vents.Length];
            for (int i = 0; i < ventLast4.Length; i++) ventLast4[i] = -1;
            ventHit4.Clear(); for (int i = 0; i < Room.Vents.Length; i++) ventHit4.Add(new List<int>());
            pushSeen4.Clear(); fireCd4.Clear(); plant4.Clear(); called4.Clear(); split4.Clear();
            flaskT4 = 0f; callT4 = 0f; fireStay4 = 0f; eggT4 = Balance.DragonEggs1 * 0.5f; flyT4 = 0f; airT4 = 0f; flyStage4 = 0;
            foreach (Enemy e in Enemies) if (e.Type == EnemyType.KoboldForeman || e.Type == EnemyType.ElderDragon) Forge.Phase = 1;
        }

        // ------------------------------------------------------------------ terreno: respiros, lava, fogo no chao

        float VentT4(int i) => (ventClock4 + Room.Vents[i].Phase) % Balance.VentCycle;

        /// <summary>Respiro i: 0 apagado, 1 brilhando (vai sair fogo), 2 fogo.</summary>
        public int VentState(int i)
        {
            if (!ventsOn4 || i < 0 || i >= Room.Vents.Length) return 0;
            float t = VentT4(i);
            return t < Balance.VentOff ? 0 : t < Balance.VentOff + Balance.VentGlow ? 1 : 2;
        }

        /// <summary>Segundos ate o proximo fogo do respiro i (0 = em fogo; MaxValue = desligado).</summary>
        public float VentFireIn(int i)
        {
            if (!ventsOn4 || i < 0 || i >= Room.Vents.Length) return float.MaxValue;
            float t = VentT4(i), fire = Balance.VentOff + Balance.VentGlow;
            return t >= fire ? 0f : fire - t;
        }

        /// <summary>Ponto a Balance.FireNear (1,5 m) de lava, fogo no chao ou respiro em fogo.</summary>
        public bool NearFire(V2 p)
        {
            if (Hits(Room.Lava, p, Balance.FireNear)) return true;
            foreach (FirePatch f in Forge.Fires) if (V2.Dist(p, f.Pos) <= f.Radius + Balance.FireNear) return true;
            for (int i = 0; i < Room.Vents.Length; i++) if (VentState(i) == 2 && V2.Dist(p, Room.Vents[i].Pos) <= Room.Vents[i].Radius + Balance.FireNear) return true;
            return false;
        }

        /// <summary>Circulo (p, r) encostando em lava.</summary>
        public bool InLava(V2 p, float r) => Hits(Room.Lava, p, r);

        /// <summary>Criaturas de fogo: imunes a respiro e fogo no chao (a brasa morre na lava; salamandra, elemental e Dragao nao).</summary>
        static bool FireImmune(EnemyType t) => t == EnemyType.Salamander || t == EnemyType.MagmaElemental || t == EnemyType.MagmaShard
                                                || t == EnemyType.ElderDragon || t == EnemyType.SalamanderEgg;

        void AddFire4(V2 p, float r, float time, int source)
        {
            if (InWater(p)) return;   // agua apaga
            Forge.Fires.Add(new FirePatch { Pos = p, Radius = r, Left = time, Source = source });
        }

        /// <summary>Golpe de fogo/pancada na carga no chao: barril detona (fogo direto: respiro, frasco, sopro) ou fica com 1 s de pavio (fogo no
        /// chao: "acende o pavio", M4-11), lanterna apaga, gelo perde pontos, balao fura (fogo: 1 no minimo), o resto racha `cracks` vezes.</summary>
        void Burn4(Cargo g, int cracks, float ice, bool fire, bool ground = false)
        {
            if (g.State != CargoState.Ground) return;
            if (fire && g.Type == CargoType.Barrel)
            {
                if (!ground) { Detonate(g); return; }
                if (g.Fuse < Balance.BarrelFuse - Balance.FireFuse) { g.Fuse = Balance.BarrelFuse - Balance.FireFuse; g.Armed = true; Emit(Ev.FuseLit, g.Id, 1, g.Pos); }
                return;
            }
            if (g.Type == CargoType.Lantern) g.Out = true;
            if (g.Type == CargoType.IceCrystal) { if (ice > 0f) ForgeIce(g, ice); return; }
            int k = g.Type == CargoType.Balloon && fire ? Math.Max(1, cracks) : cracks;
            for (int i = 0; i < k && Running && g.State == CargoState.Ground; i++) Crack(g);
        }

        /// <summary>Empurra a carga no chao (pouso, chicote) em passos de 0,1 m: obstaculo para; lava para na borda com 1 rachadura.</summary>
        void PushCargo4(Cargo g, V2 dir, float dist)
        {
            V2 p = g.Pos; bool lava = false;
            for (int i = 0; i < (int)(dist / 0.1f); i++)
            {
                V2 q = p + dir * 0.1f;
                ClampRoom(ref q, Balance.CargoR);
                if (Hits(Room.Obstacles, q, Balance.CargoR)) break;
                if (Hits(Room.Lava, q, Balance.CargoR)) { lava = true; break; }
                p = q;
            }
            g.Pos = p;
            if (lava) { Emit(Ev.LavaBurn, 2, g.Id, g.Pos); Crack(g); }
        }

        void StepVents4()
        {
            if (Room.Vents.Length == 0) return;
            bool dragon = false;
            foreach (Enemy e in Enemies) if (e.Alive && e.Type == EnemyType.ElderDragon) dragon = true;
            ventsOn4 = !dragon || Forge.Phase >= 2;   // B4: as fissuras so ligam na fase 2
            if (!ventsOn4) return;
            ventClock4 += Balance.Step;
            var c = Courier;
            for (int i = 0; i < Room.Vents.Length && Running; i++)
            {
                if (VentState(i) != 2) continue;
                Vent v = Room.Vents[i];
                int cycle = (int)((ventClock4 + v.Phase) / Balance.VentCycle);
                List<int> hit = ventHit4[i];
                if (ventLast4[i] != cycle) { ventLast4[i] = cycle; hit.Clear(); Emit(Ev.VentFire, i, 0, v.Pos); }
                if (c.State != CourierState.Dead && !hit.Contains(0) && V2.Dist(c.Pos, v.Pos) <= v.Radius) { hit.Add(0); DamageCourier(Balance.VentCourierDamage, true); if (!Running) return; }   // fogo no pe: nao racha a reliquia nas maos
                for (int k = 0; k < Enemies.Count; k++)
                {
                    Enemy e = Enemies[k];
                    if (!e.Alive || e.Def.Hazard || e.Type == EnemyType.Bat || FireImmune(e.Type) || hit.Contains(e.Id) || V2.Dist(e.Pos, v.Pos) > v.Radius) continue;
                    hit.Add(e.Id); HitEnv4(e, Balance.VentEnemyDamage);
                }
                for (int k = 0; k < Cargos.Count && Running; k++)
                {
                    Cargo g = Cargos[k];
                    if (g.State != CargoState.Ground || hit.Contains(-g.Id - 1) || V2.Dist(g.Pos, v.Pos) > v.Radius) continue;
                    hit.Add(-g.Id - 1); Burn4(g, 1, Balance.VentIce, true);
                }
            }
        }

        void StepFires4()
        {
            const float dt = Balance.Step;
            for (int i = Forge.Fires.Count - 1; i >= 0; i--) { FirePatch f = Forge.Fires[i]; f.Left -= dt; if (f.Left <= 1e-4f) Forge.Fires.RemoveAt(i); }
            if (fireCd4.Count > 0) foreach (int k in new List<int>(fireCd4.Keys)) fireCd4[k] = Math.Max(0f, fireCd4[k] - dt);
            if (Forge.Fires.Count == 0) return;
            var c = Courier;
            bool inside = c.State != CourierState.Dead && InFire4(c.Pos, 0.2f);
            fireStay4 = inside ? fireStay4 + dt : 0f;
            if (inside && fireStay4 >= Balance.FireGrace - 1e-4f && Ready4(0))   // passar rapido nao queima; ficar queima 1 por segundo
            {
                fireCd4[0] = BootsLevel >= Balance.GearMaxLevel ? 2f * Balance.FireTick : Balance.FireTick;   // Botas nivel 5: 1 a cada 2 s
                DamageCourier(1, true);   // fogo no pe: nao racha a reliquia nas maos
                if (!Running) return;
            }
            for (int k = 0; k < Enemies.Count; k++)
            {
                Enemy e = Enemies[k];
                if (!e.Alive || e.Def.Hazard || e.Type == EnemyType.Bat || FireImmune(e.Type) || !InFire4(e.Pos, 0f) || !Ready4(e.Id)) continue;
                fireCd4[e.Id] = Balance.FireTick; HitEnv4(e, 1);
            }
            for (int k = 0; k < Cargos.Count && Running; k++)
            {
                Cargo g = Cargos[k]; int key = -g.Id - 1;
                if (g.State != CargoState.Ground || !InFire4(g.Pos, 0f) || !Ready4(key)) continue;
                fireCd4[key] = Balance.FireTick; Burn4(g, 0, Balance.FireIce, true, true);
            }
        }

        /// <summary>Dano do ambiente (respiro, fogo no chao) ou da planta: nao conta como "lutou nesta sala" (H1 mede a luta escolhida).</summary>
        void HitEnv4(Enemy e, int dmg) { bool f = FoughtHere; Hit(e, dmg); FoughtHere = f; }

        bool Ready4(int key) => !fireCd4.TryGetValue(key, out float t) || t <= 1e-4f;
        bool InFire4(V2 p, float extra) { foreach (FirePatch f in Forge.Fires) if (V2.Dist(p, f.Pos) <= f.Radius + extra) return true; return false; }

        /// <summary>Quem o dash empurrou neste passo: contra a lava leva 3 (brasa morre; salamandra, elemental e Dragao imunes); diabrete no
        /// redemoinho com Contragolpe fica atordoado 1 s e a carga a 1,5 m volta as maos.</summary>
        void StepPushes4()
        {
            var c = Courier;
            for (int k = 0; k < Enemies.Count; k++)
            {
                Enemy e = Enemies[k];
                if (!e.Alive || e.PushedDash != c.DashCount || (pushSeen4.TryGetValue(e.Id, out int seen) && seen == c.DashCount)) continue;
                pushSeen4[e.Id] = c.DashCount;
                if (e.Type == EnemyType.Imp && Has(Perk.Counter))
                    foreach (ForgeMark m in Forge.Marks)
                        if (m.Owner == e.Id && m.Kind == ForgeMarkKind.Swap)
                        {
                            m.Left = 0f; m.Bound = true; e.Stun = 1f; Emit(Ev.Stun, e.Id, 0, e.Pos);
                            Cargo g = m.Target;
                            if (g != null && g.State == CargoState.Ground && V2.Dist(g.Pos, c.Pos) <= 1.5f)
                            {
                                g.State = CargoState.Held; g.Pos = c.Pos; g.AloneT = 0f; g.PickT = 0f; g.Armed = true;
                                Emit(Ev.CargoPickup, g.Id, 2, c.Pos);
                            }
                        }
                if (Room.Lava.Length == 0 || e.Type == EnemyType.Bat || e.Def.Hazard || (FireImmune(e.Type) && e.Type != EnemyType.MagmaShard)) continue;
                if (!Hits(Room.Lava, e.Pos, e.Def.Radius + 0.15f)) continue;
                Emit(Ev.LavaBurn, 0, e.Id, e.Pos);
                Hit(e, e.Type == EnemyType.MagmaShard ? Math.Max(1, e.Hp) : Balance.LavaDamage);
            }
        }

        // ------------------------------------------------------------------ avisos no chao (frasco, chicote, giro, sopro, pouso, diabrete)

        ForgeMark Mark4(ForgeMarkKind kind, Enemy owner, V2 pos, float radius, float left, bool bound)
        {
            var m = new ForgeMark { Kind = kind, Owner = owner.Id, Pos = pos, Radius = radius, Left = left, Total = left, Bound = bound };
            Forge.Marks.Add(m);
            return m;
        }

        Enemy Find4(int id) { foreach (Enemy e in Enemies) if (e.Id == id && e.Alive) return e; return null; }

        void StepMarks4()
        {
            const float dt = Balance.Step;
            var c = Courier;
            for (int i = 0; i < Forge.Marks.Count && Running; i++)
            {
                ForgeMark m = Forge.Marks[i];
                Enemy o = Find4(m.Owner);
                if (m.Left > 0f)
                {
                    if (m.Bound && (o == null || o.Stun > 0f || o.State != EnemyState.Telegraph)) { m.Left = 0f; m.Active = 0f; m.Total = -1f; continue; }   // cancelado
                    if (m.Kind == ForgeMarkKind.Swap && m.Target != null) { if (m.Target.State != CargoState.Ground) { m.Left = 0f; m.Total = -1f; continue; } m.Pos = m.Target.Pos; }
                    m.Left -= dt;
                    if (m.Left > 1e-4f) continue;
                    m.Left = 0f;
                    Strike4(m, o);
                    continue;
                }
                if (m.Active > 0f && m.Kind == ForgeMarkKind.Breath)
                {
                    if (o == null) { m.Active = 0f; continue; }
                    Breathe4(m, o);
                    m.Active -= dt;
                    if (m.Active <= 1e-4f) m.Active = 0f;
                }
            }
            Forge.Marks.RemoveAll(m => m.Left <= 0f && m.Active <= 0f);
        }

        /// <summary>O aviso acabou: o golpe acontece.</summary>
        void Strike4(ForgeMark m, Enemy o)
        {
            var c = Courier;
            switch (m.Kind)
            {
                case ForgeMarkKind.Flask:
                    Emit(Ev.Strike, m.Owner, (int)EnemyType.KoboldBomber, m.Pos);
                    if (c.State != CourierState.Dead && V2.Dist(c.Pos, m.Pos) <= m.Radius + 0.2f) { DamageCourier(m.Damage); if (!Running) return; fireCd4[0] = Balance.FireTick; }   // a poca so queima 1 s depois
                    if (Pet != null && V2.Dist(Pet.Pos, m.Pos) <= m.Radius + Balance.PetR) { DamagePet(); if (!Running) return; }
                    for (int k = 0; k < Cargos.Count && Running; k++) if (Cargos[k].State == CargoState.Ground && V2.Dist(Cargos[k].Pos, m.Pos) <= m.Radius + Balance.CargoR) Burn4(Cargos[k], 1, Balance.FlaskIce, true);
                    AddFire4(m.Pos, m.Pool, Balance.FlaskPool, 1);
                    break;
                case ForgeMarkKind.Whip:
                case ForgeMarkKind.Spin:
                {
                    if (o == null) return;
                    Emit(Ev.Strike, o.Id, (int)o.Type, m.Pos);
                    bool whip = m.Kind == ForgeMarkKind.Whip;
                    V2 end = m.Pos + m.Dir * m.Length;
                    float dc = whip ? V2.SegDist(c.Pos, m.Pos, end) : V2.Dist(c.Pos, m.Pos);
                    if (c.State != CourierState.Dead && dc <= m.Radius + (whip ? Balance.CourierR : 0.2f)) { DamageCourier(o.Def.Damage); if (!Running) return; }
                    for (int k = 0; k < Cargos.Count && Running; k++)
                    {
                        Cargo g = Cargos[k];
                        if (g.State != CargoState.Ground) continue;
                        float dg = whip ? V2.SegDist(g.Pos, m.Pos, end) : V2.Dist(g.Pos, m.Pos);
                        if (dg > m.Radius + Balance.CargoR) continue;
                        if (whip) { PushCargo4(g, (o.Pos - g.Pos).Norm, Balance.WhipPull); CargoGrabbed(o); }   // o chicote puxa a carga para ele
                        Burn4(g, 1, Balance.SmashIce, false);
                    }
                    break;
                }
                case ForgeMarkKind.Breath:
                    if (o == null) return;
                    Emit(Ev.Strike, o.Id, (int)o.Type, o.Pos);
                    m.Active = m.Sweep ? Balance.SweepTime : Balance.BreathTime;
                    if (m.Sweep) m.Dir = new V2((m.Dir.X + m.Dir.Y) * 0.70710678f, (m.Dir.Y - m.Dir.X) * 0.70710678f);   // comeca 45 graus antes
                    m.Ember = 0f;
                    break;
                case ForgeMarkKind.Land:
                {
                    if (o == null) return;
                    o.Pos = FreeNear(Room, m.Pos, o.Def.Radius);
                    Forge.Flying = false; Forge.HeadDown = Balance.HeadDown; flyStage4 = 0;
                    o.State = EnemyState.Recover; o.Cd = Math.Max(o.Cd, Balance.HeadDown);
                    bool hit = c.State != CourierState.Dead && V2.Dist(c.Pos, m.Pos) <= m.Radius + 0.2f;
                    Emit(Ev.Land, o.Id, hit ? 1 : 0, m.Pos);
                    if (hit) { DamageCourier(o.Def.Damage); if (!Running) return; }
                    for (int k = 0; k < Cargos.Count && Running; k++)
                    {
                        Cargo g = Cargos[k];
                        if (g.State != CargoState.Ground || V2.Dist(g.Pos, m.Pos) > m.Radius + Balance.CargoR) continue;
                        V2 away = (g.Pos - m.Pos).Norm; if (away.Len < 0.5f) away = V2.Up * -1f;
                        PushCargo4(g, away, Balance.LandPush);
                        Burn4(g, 1, Balance.SmashIce, false);
                    }
                    break;
                }
                case ForgeMarkKind.Swap:
                {
                    Cargo g = m.Target;
                    if (o == null || g == null || g.State != CargoState.Ground) return;
                    V2 was = o.Pos;
                    o.Pos = FreeNear(Room, g.Pos, o.Def.Radius);
                    g.Pos = FreeNear(Room, was, Balance.CargoR);
                    Emit(Ev.Strike, o.Id, (int)o.Type, o.Pos);
                    CargoGrabbed(o);
                    break;
                }
                case ForgeMarkKind.Blink:
                    if (o == null) return;
                    o.Pos = FreeNear(Room, m.Pos, o.Def.Radius);
                    Emit(Ev.Strike, o.Id, (int)o.Type, o.Pos);
                    if (c.State != CourierState.Dead && V2.Dist(o.Pos, c.Pos) - Balance.CourierR <= o.Def.Reach) DamageCourier(o.Def.Damage);
                    break;
            }
        }

        /// <summary>Um passo do sopro: courier e carga no cone (pilar faz sombra) levam o golpe 1x; brasas a cada 0,5 s; na varredura o cone gira.</summary>
        void Breathe4(ForgeMark m, Enemy o)
        {
            var c = Courier;
            m.Pos = o.Pos;
            if (m.Sweep) m.Dir = new V2(m.Dir.X * Balance.Cos2 - m.Dir.Y * Balance.Sin2, m.Dir.X * Balance.Sin2 + m.Dir.Y * Balance.Cos2).Norm;   // +2 graus por passo
            if (c.State != CourierState.Dead && !m.Hit.Contains(0) && InCone4(m, c.Pos, Balance.CourierR)) { m.Hit.Add(0); DamageCourier(o.Def.Damage); if (!Running) return; }
            for (int k = 0; k < Cargos.Count && Running; k++)
            {
                Cargo g = Cargos[k];
                if (g.State != CargoState.Ground || m.Hit.Contains(-g.Id - 1) || !InCone4(m, g.Pos, Balance.CargoR)) continue;
                m.Hit.Add(-g.Id - 1);
                Burn4(g, g.Type == CargoType.Balloon ? 3 : 1, Balance.BreathIce, true);   // o balao estoura
            }
            m.Ember -= Balance.Step;
            if (m.Ember <= 1e-4f)
            {
                m.Ember = 0.5f;
                for (float d = 1.8f; d <= m.Length; d += 1.6f) { V2 p = m.Pos + m.Dir * d; if (nav.ShotClear(m.Pos, p, 0.1f)) AddFire4(p, Balance.EmberR, Balance.EmberTime, 2); }
            }
        }

        bool InCone4(ForgeMark m, V2 p, float r)
        {
            V2 v = p - m.Pos; float d = v.Len;
            if (d > m.Length + r) return false;
            if (d > 0.05f && V2.Dot(v * (1f / d), m.Dir) < Balance.BreathCos) return false;
            return nav.ShotClear(m.Pos, p, 0.1f);   // pilar faz sombra (a lava nao)
        }

        // ------------------------------------------------------------------ inimigos

        partial void EnemyR4(Enemy e, ref bool handled)
        {
            switch (e.Type)
            {
                case EnemyType.Salamander: Salamander4(e); handled = true; break;
                case EnemyType.Imp: Imp4(e); handled = true; break;
                case EnemyType.KoboldBomber: Kobold4(e); handled = true; break;
                case EnemyType.MagmaElemental:
                {
                    bool strike = e.State == EnemyState.Telegraph && e.Tele - Balance.Step <= 1e-4f;
                    Melee(e);   // pancada do nucleo no courier; a carga no chao no alcance racha (como o Bruto) e o gelo perde 20
                    if (strike) for (int k = 0; k < Cargos.Count && Running; k++) if (Cargos[k].State == CargoState.Ground && V2.Dist(e.Pos, Cargos[k].Pos) <= e.Def.Reach) Burn4(Cargos[k], 1, Balance.SmashIce, false);
                    handled = true; break;
                }
                case EnemyType.SalamanderEgg:
                    e.State = EnemyState.Idle; e.GrabT += Balance.Step;   // GrabT = idade do ovo
                    if (e.GrabT >= e.Def.Tele - 1e-4f)
                    {
                        e.Hp = 0;
                        Enemy s = Spawn(EnemyType.Salamander, e.Pos); s.Wave = e.Wave;
                        Emit(Ev.EggHatch, e.Id, s.Id, e.Pos);
                    }
                    handled = true; break;
                case EnemyType.KoboldForeman: Foreman4(e); handled = true; break;
                case EnemyType.ElderDragon: Dragon4(e); handled = true; break;
            }
        }

        partial void EnemyDiedR4(Enemy e)
        {
            if (e.Type == EnemyType.MagmaElemental) split4.Add(e);   // divide no StepR4: aqui o nucleo pode estar percorrendo a lista de inimigos
        }

        void Split4()
        {
            foreach (Enemy e in split4)
                for (int k = -1; k <= 1; k += 2)   // 2 brasas a +-0,8 m, se o teto de vivos deixar
                {
                    if (!CanSpawn(EnemyType.MagmaShard)) break;
                    Enemy s = Spawn(EnemyType.MagmaShard, e.Pos + new V2(k * Balance.ShardSpread, 0f)); s.Wave = e.Wave;
                }
            split4.Clear();
        }

        partial void HitMulR4(Enemy e, ref int mul)
        {
            if (e.Type != EnemyType.ElderDragon) return;
            if (Forge.Flying) mul = 0; else if (Forge.HeadDown > 0f) mul *= 2;
        }

        /// <summary>Salamandra: circula o courier a 2,5 m por 3 s (GrabT = relogio do rastro; Tele = tempo do estado), corta na direcao dele
        /// (toque 1, recarga 1,2 s) e volta a circular; deixa fogo no chao a cada 0,5 s andando.</summary>
        void Salamander4(Enemy e)
        {
            var c = Courier; const float dt = Balance.Step;
            if (c.State == CourierState.Dead) return;
            V2 p0 = e.Pos;
            if (e.State != EnemyState.Chase && e.State != EnemyState.Lurk) { e.State = EnemyState.Lurk; e.Tele = Balance.SalamanderCircle; }
            e.Tele -= dt;
            V2 to = e.Pos - c.Pos; float d = to.Len; V2 radial = d > 0.01f ? to * (1f / d) : V2.Up;
            if (e.State == EnemyState.Chase)
            {
                MoveWalker(e, c.Pos, e.Def.Speed);
                if (e.Tele <= 1e-4f) { e.State = EnemyState.Lurk; e.Tele = Balance.SalamanderCircle; }
            }
            else
            {
                V2 tan = e.Phase < 0.5f ? radial.Right : radial.Right * -1f;
                V2 dir = (tan + radial * ((Balance.SalamanderOrbit - d) * 1.2f)).Norm;
                e.Pos = e.Pos + dir * (e.Def.Speed * dt);
                Collide(Room, ref e.Pos, e.Def.Radius);
                if (e.Tele <= 1e-4f && e.Cd <= 0f) { e.State = EnemyState.Chase; e.Tele = Balance.SalamanderCut; Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos); }
            }
            if (e.Cd <= 0f && V2.Dist(e.Pos, c.Pos) <= e.Def.Radius + Balance.CourierR)
            {
                e.Cd = e.Def.Cooldown; e.State = EnemyState.Lurk; e.Tele = Balance.SalamanderCircle;
                DamageCourier(e.Def.Damage);
                if (!Running) return;
            }
            if (V2.Dist(p0, e.Pos) > 0.01f)
            {
                e.GrabT += dt;
                if (e.GrabT >= Balance.SalamanderTrail - 1e-4f) { e.GrabT = 0f; AddFire4(e.Pos, Balance.TrailR, Balance.TrailTime, 0); }
            }
        }

        /// <summary>Diabrete: fica a 4-7 m. Com a carga no chao e linha livre ate ela: redemoinho 1 s e troca de lugar com ela (recarga 5 s); um
        /// golpe no redemoinho cancela e atordoa 0,5 s (Hits = vida no inicio do aviso). Sem carga no chao: some e surge 1 m atras do courier
        /// (aviso 0,5 s) e arranha (1); recarga 3 s.</summary>
        void Imp4(Enemy e)
        {
            var c = Courier;
            if (e.State == EnemyState.Telegraph)
            {
                if (e.Hp < e.Hits) { e.State = EnemyState.Recover; e.Stun = Balance.ImpHitStun; Emit(Ev.Stun, e.Id, 0, e.Pos); return; }   // acertado no redemoinho
                e.Tele -= Balance.Step;
                if (e.Tele <= 1e-4f) e.State = EnemyState.Recover;
                return;
            }
            if (c.State == CourierState.Dead) return;
            Cargo g = null; float best = float.MaxValue;
            foreach (Cargo k in Cargos)
            {
                if (k.State != CargoState.Ground) continue;
                float dk = V2.Dist(e.Pos, k.Pos);
                if (dk < best && nav.ShotClear(e.Pos, k.Pos, 0.1f)) { best = dk; g = k; }
            }
            float d = V2.Dist(e.Pos, c.Pos);
            if (e.Cd <= 0f && g != null)
            {
                e.State = EnemyState.Telegraph; e.Tele = e.Def.Tele; e.Hits = e.Hp; e.Cd = e.Def.Cooldown + e.Def.Tele;
                Mark4(ForgeMarkKind.Swap, e, g.Pos, 0.6f, e.Def.Tele, true).Target = g;
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            bool down = false; foreach (Cargo k in Cargos) if (k.State == CargoState.Ground) down = true;
            if (e.Cd <= 0f && !down && d <= Balance.ImpFar + 1f)
            {
                e.State = EnemyState.Telegraph; e.Tele = Balance.ImpBlinkTele; e.Hits = e.Hp; e.Cd = Balance.ImpBlinkCd + Balance.ImpBlinkTele;
                V2 back = c.Facing.Len > 0.5f ? c.Facing : V2.Up;
                Mark4(ForgeMarkKind.Blink, e, FreeNear(Room, c.Pos - back * Balance.ImpBehind, e.Def.Radius), 0.5f, Balance.ImpBlinkTele, true);
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            if (d < Balance.ImpNear) { e.State = EnemyState.Flee; MoveWalker(e, e.Pos + (e.Pos - c.Pos).Norm * 1.5f, e.Def.Speed); }
            else if (d > Balance.ImpFar) { e.State = EnemyState.Chase; MoveWalker(e, c.Pos, e.Def.Speed); }
            else e.State = e.Cd > 0f ? EnemyState.Recover : EnemyState.Idle;
        }

        /// <summary>Kobold incendiario: fica a 3,5 m ou mais; aviso 0,9 s (circulo de 1 m onde o courier estava) e o frasco cai: 1 de dano,
        /// poca de fogo de 1 m por 4 s, carga no chao embaixo racha (barril detona). Recarga 3 s. O dash no arremesso cancela o frasco.</summary>
        void Kobold4(Enemy e)
        {
            var c = Courier;
            if (e.State == EnemyState.Telegraph)
            {
                e.Tele -= Balance.Step;
                if (e.Tele <= 1e-4f) { e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown; }
                return;
            }
            if (c.State == CourierState.Dead) return;
            float d = V2.Dist(e.Pos, c.Pos), edge = d - Balance.CourierR;
            if (d < Balance.KoboldKeep) { e.State = EnemyState.Flee; MoveWalker(e, e.Pos + (e.Pos - c.Pos).Norm * 1.5f, e.Def.Speed); return; }
            if (e.Cd <= 0f && edge <= e.Def.Trigger)
            {
                e.State = EnemyState.Telegraph; e.Tele = e.Def.Tele;
                ForgeMark m = Mark4(ForgeMarkKind.Flask, e, c.Pos, Balance.FlaskR, e.Def.Tele, true);
                m.Damage = e.Def.Damage; m.Pool = Balance.FlaskR;
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            if (edge > e.Def.Trigger) { e.State = EnemyState.Chase; MoveWalker(e, c.Pos, e.Def.Speed); }
            else e.State = e.Cd > 0f ? EnemyState.Recover : EnemyState.Idle;
        }

        int MaxHp4(Enemy e) => Math.Max(1, (int)Math.Round(e.Def.Hp * Balance.HpMul(Tier)));

        void Phase4(Enemy e, int phase)
        {
            if (phase <= Forge.Phase) return;
            Forge.Phase = phase;
            if (e.Type == EnemyType.ElderDragon) flyT4 = Math.Max(flyT4, (phase >= 3 ? Balance.DragonFly3 : Balance.DragonFly2) - Balance.DragonFirstFly);   // 1o voo logo
            Emit(Ev.BossPhase, e.Id, phase, e.Pos);
        }

        /// <summary>Capataz Kobold (M4): chicote de 3 m (aviso 0,7 s; puxa a carga 1,5 m e racha), frasco a cada 5 s (poca de 1,2 m), apito a cada
        /// 14 s chama 2 kobolds (no maximo 2 vivos dele). Fase 2 (50%): alterna chicote e giro (3 m, aviso 1 s); frasco a cada 3,5 s.
        /// Hits = contador de golpes (alterna).</summary>
        void Foreman4(Enemy e)
        {
            var c = Courier; const float dt = Balance.Step;
            if (e.Hp * 2 <= MaxHp4(e)) Phase4(e, 2);
            bool two = Forge.Phase >= 2;
            if (c.State == CourierState.Dead) return;
            flaskT4 += dt;
            if (flaskT4 >= (two ? Balance.ForemanFlask2 : Balance.ForemanFlask) - 1e-4f && V2.Dist(e.Pos, c.Pos) <= Balance.ForemanFlaskRange)
            {
                flaskT4 = 0f;
                ForgeMark m = Mark4(ForgeMarkKind.Flask, e, c.Pos, Balance.ForemanFlaskR, EnemyDef.All[(int)EnemyType.KoboldBomber].Tele, false);
                m.Damage = 1; m.Pool = Balance.ForemanFlaskR;
            }
            callT4 += dt;
            if (callT4 >= Balance.ForemanCallEvery - 1e-4f)
            {
                callT4 = 0f;
                called4.RemoveAll(id => Find4(id) == null);
                int n = 0;
                for (int k = 0; k < 2 && called4.Count < Balance.ForemanCalls; k++)
                {
                    if (!CanSpawn(EnemyType.KoboldBomber)) break;
                    Enemy kb = Spawn(EnemyType.KoboldBomber, new V2(k == 0 ? 1.0f : Balance.RoomW - 1.0f, 11.5f));
                    called4.Add(kb.Id); n++;
                }
                Emit(Ev.ForemanCall, e.Id, n, e.Pos);
            }
            if (e.State == EnemyState.Telegraph)
            {
                e.Tele -= dt;
                if (e.Tele <= 1e-4f) { e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown; }
                return;
            }
            float edge = V2.Dist(e.Pos, c.Pos) - Balance.CourierR;
            if (e.Cd <= 0f && edge <= e.Def.Trigger)
            {
                bool spin = two && e.Hits++ % 2 == 1;
                e.State = EnemyState.Telegraph; e.Tele = spin ? Balance.SpinTele : e.Def.Tele;
                if (spin) Mark4(ForgeMarkKind.Spin, e, e.Pos, Balance.SpinR, e.Tele, true);
                else
                {
                    ForgeMark m = Mark4(ForgeMarkKind.Whip, e, e.Pos, Balance.WhipHalf, e.Tele, true);
                    m.Dir = (c.Pos - e.Pos).Norm; m.Length = e.Def.Reach;
                }
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            if (edge > e.Def.Trigger * 0.7f) { e.State = EnemyState.Chase; MoveWalker(e, c.Pos, e.Def.Speed); }
            else e.State = e.Cd > 0f ? EnemyState.Recover : EnemyState.Idle;
        }

        /// <summary>Dragao Anciao (B4). Fase 1 (100-60%): sopro em cone (aviso 1,4 s, 60 graus, 6 m; pilar faz sombra) e 2 ovos a cada 20 s.
        /// Fase 2 (60-30%): + voo a cada 18 s (decola 0,6 s, 2,5 s no ar seguindo o courier a 2 m/s, circulo de 2,2 m por 1,2 s, pousa: 3 de
        /// dano, empurra a carga 2 m; cabeca baixa 3 s = dano x2) e as fissuras ligam. Fase 3 (30%): sopro em varredura, ovos a cada 12 s,
        /// voo a cada 14 s. Anda devagar na metade de cima (y >= 9).</summary>
        void Dragon4(Enemy e)
        {
            var c = Courier; const float dt = Balance.Step;
            int max = MaxHp4(e);
            if (e.Hp * 100 <= max * Balance.DragonPhase3) Phase4(e, 3);
            else if (e.Hp * 100 <= max * Balance.DragonPhase2) Phase4(e, 2);
            if (Forge.Phase < 1) Forge.Phase = 1;
            Forge.HeadDown = Math.Max(0f, Forge.HeadDown - dt);
            if (flyStage4 > 0) { Fly4(e); return; }
            if (c.State == CourierState.Dead) return;
            eggT4 += dt;
            if (eggT4 >= (Forge.Phase >= 3 ? Balance.DragonEggs3 : Balance.DragonEggs1) - 1e-4f) { eggT4 = 0f; LayEggs4(); }
            if (e.State == EnemyState.Telegraph)
            {
                e.Tele -= dt;
                if (e.Tele <= 1e-4f) { e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown + (Forge.Phase >= 3 ? Balance.SweepTime : Balance.BreathTime); }
                return;
            }
            bool breathing = false; foreach (ForgeMark m in Forge.Marks) if (m.Owner == e.Id && m.Kind == ForgeMarkKind.Breath) breathing = true;
            if (Forge.Phase >= 2 && !breathing && Forge.HeadDown <= 0f)
            {
                flyT4 += dt;
                if (flyT4 >= (Forge.Phase >= 3 ? Balance.DragonFly3 : Balance.DragonFly2) - 1e-4f)
                {
                    flyT4 = 0f; flyStage4 = 1; airT4 = Balance.FlyTakeOff;
                    Forge.Flying = true; Forge.Shadow = e.Pos; e.State = EnemyState.Flee;
                    Emit(Ev.TakeOff, e.Id, 0, e.Pos);
                    return;
                }
            }
            if (Forge.HeadDown > 0f || breathing) { e.State = EnemyState.Recover; return; }
            float edge = V2.Dist(e.Pos, c.Pos) - Balance.CourierR;
            if (e.Cd <= 0f && edge <= e.Def.Trigger)
            {
                e.State = EnemyState.Telegraph; e.Tele = e.Def.Tele;
                ForgeMark m = Mark4(ForgeMarkKind.Breath, e, e.Pos, 0f, e.Tele, true);
                m.Dir = (c.Pos - e.Pos).Norm; m.Length = e.Def.Reach; m.Sweep = Forge.Phase >= 3;
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            V2 goal = new V2(c.Pos.X, Math.Max(Balance.DragonMinY, c.Pos.Y));
            if (V2.Dist(goal, e.Pos) > 0.3f) { e.State = EnemyState.Chase; MoveWalker(e, goal, e.Def.Speed); }
            else e.State = e.Cd > 0f ? EnemyState.Recover : EnemyState.Idle;
        }

        void Fly4(Enemy e)
        {
            const float dt = Balance.Step;
            airT4 -= dt;
            if (flyStage4 == 2)   // no ar: a sombra segue o courier
            {
                V2 to = Courier.Pos - Forge.Shadow; float l = to.Len, step = Balance.FlyShadowSpeed * dt;
                Forge.Shadow = l <= step ? Courier.Pos : Forge.Shadow + to * (step / l);
                ClampRoom(ref Forge.Shadow, e.Def.Radius);
                e.Pos = Forge.Shadow;
            }
            if (airT4 > 1e-4f) return;
            if (flyStage4 == 1) { flyStage4 = 2; airT4 = Balance.FlyAir; }
            else if (flyStage4 == 2)
            {
                flyStage4 = 3; airT4 = Balance.LandTele;
                Forge.Shadow = FreeNear(Room, Forge.Shadow, e.Def.Radius); e.Pos = Forge.Shadow;
                Mark4(ForgeMarkKind.Land, e, Forge.Shadow, Balance.LandR, Balance.LandTele, false);
            }
            // estagio 3: o aviso do pouso (StepMarks4) pousa e zera flyStage4
        }

        /// <summary>Ovos de salamandra em pontos livres a 3 m+ do courier (no maximo 2 vivos; teto de vivos da sala).</summary>
        void LayEggs4()
        {
            for (int n = Count(EnemyType.SalamanderEgg); n < Balance.DragonEggs; n++)
                for (int t = 0; t < 24; t++)
                {
                    V2 p = new V2(rng.Range(1f, Balance.RoomW - 1f), rng.Range(3.5f, 11.5f));
                    if (Blocked(Room, p, 0.4f) || V2.Dist(p, Courier.Pos) < Balance.EggMinDist) continue;
                    bool vent = false; foreach (Vent v in Room.Vents) if (V2.Dist(p, v.Pos) <= v.Radius + 0.3f) vent = true;
                    if (vent) continue;
                    if (CanSpawn(EnemyType.SalamanderEgg)) Spawn(EnemyType.SalamanderEgg, p);
                    break;
                }
        }

        // ------------------------------------------------------------------ cargas: planta carnivora e mapa

        /// <summary>Planta no chao: morde o inimigo mais perto a 1,2 m (aviso 0,3 s, 2 de dano, recarga 1 s; chefes tambem) e o courier parado
        /// sem carga a 1,2 m por 0,8 s (1 de dano, recarga 1,5 s). Ladrao que a carrega leva 1 por segundo (e a solta). Nas maos nao morde.</summary>
        void StepPlants4()
        {
            const float dt = Balance.Step;
            var c = Courier;
            for (int i = 0; i < Cargos.Count && Running; i++)
            {
                Cargo g = Cargos[i];
                if (g.Type != CargoType.Flytrap) continue;
                if (!plant4.TryGetValue(g.Id, out Plant4 s)) plant4[g.Id] = s = new Plant4();
                s.CourierCd = Math.Max(0f, s.CourierCd - dt);
                if (g.State == CargoState.Stolen)
                {
                    s.CarryT += dt;
                    if (s.CarryT < Balance.PlantCarry - 1e-4f) continue;
                    s.CarryT = 0f;
                    foreach (Enemy e in Enemies) if (e.Alive && e.Carry == g) { Emit(Ev.PlantBite, e.Id, g.Id, e.Pos); HitEnv4(e, 1); break; }
                    continue;
                }
                s.CarryT = 0f;
                if (g.State != CargoState.Ground) { s.Warn = 0f; s.Near = 0f; continue; }
                if (s.Warn > 0f)
                {
                    s.Warn -= dt;
                    if (s.Warn <= 1e-4f)
                    {
                        s.Warn = 0f; s.Cd = Balance.PlantCd;
                        Enemy t = Find4(s.Target);
                        if (t != null && V2.Dist(t.Pos, g.Pos) - t.Def.Radius <= Balance.PlantReach + 0.3f) { Emit(Ev.PlantBite, t.Id, g.Id, g.Pos); HitEnv4(t, Balance.PlantDamage); }
                    }
                }
                else
                {
                    s.Cd = Math.Max(0f, s.Cd - dt);
                    if (s.Cd <= 0f)
                    {
                        Enemy t = null; float best = float.MaxValue;
                        foreach (Enemy e in Enemies)
                        {
                            if (!e.Alive || e.Def.Hazard || HitMul(e) <= 0) continue;
                            float d = V2.Dist(e.Pos, g.Pos) - e.Def.Radius;
                            if (d <= Balance.PlantReach && d < best) { best = d; t = e; }
                        }
                        if (t != null) { s.Target = t.Id; s.Warn = Balance.PlantTele; }
                    }
                }
                bool idle = c.State == CourierState.Planted && !HasHeld && V2.Dist(c.Pos, g.Pos) - Balance.CourierR <= Balance.PlantReach;
                s.Near = idle ? s.Near + dt : 0f;
                if (idle && s.Near >= Balance.PlantIdle - 1e-4f && s.CourierCd <= 0f)
                {
                    s.CourierCd = Balance.PlantCourierCd;
                    Emit(Ev.PlantBite, 0, g.Id, g.Pos);
                    DamageCourier(1);
                }
            }
        }

        /// <summary>Mapa no chao: ladroes correm 30% mais atras dele e o ladrao da carga sozinha nasce em 6 s (em vez de 8).</summary>
        void StepMaps4()
        {
            for (int i = 0; i < Cargos.Count; i++)
            {
                Cargo g = Cargos[i];
                if (g.Type != CargoType.TreasureMap || g.State != CargoState.Ground) continue;
                if (g.AloneT > 0f) g.AloneT += Balance.Step * (Balance.AloneTime / Balance.MapAlone - 1f);
                foreach (Enemy e in Enemies)
                    if (e.Alive && e.Type == EnemyType.Thief && e.Carry == null && e.State == EnemyState.Chase && e.Stun <= 0f && !e.Asleep)
                        MoveWalker(e, g.Pos, e.Def.Speed * Balance.MapThiefSpeed);
            }
        }

        partial void StepR4()
        {
            if (split4.Count > 0) Split4();
            if (Room.Vents.Length > 0) StepVents4();
            if (!Running) return;
            StepPushes4();
            StepMarks4();
            if (!Running) return;
            StepFires4();
            if (!Running) return;
            StepPlants4();
            if (!Running) return;
            StepMaps4();
        }

        // ------------------------------------------------------------------ dicas

        partial void HintR4(ref Hint h)
        {
            var c = Courier; Hint mine = Hint.None;
            foreach (ForgeMark m in Forge.Marks) if (m.Kind == ForgeMarkKind.Swap && m.Left > 0f) mine = Hint.ImpSwap;
            if (mine == Hint.None)
                for (int i = 0; i < Room.Vents.Length; i++)
                    if (VentState(i) == 1 && V2.Dist(c.Pos, Room.Vents[i].Pos) <= Room.Vents[i].Radius + 1.2f) mine = Hint.Vent;
            if (mine == Hint.None && c.State == CourierState.Planted && !HasHeld)
                foreach (Cargo g in Cargos) if (g.Type == CargoType.Flytrap && g.State == CargoState.Ground && V2.Dist(c.Pos, g.Pos) - Balance.CourierR <= Balance.PlantReach) mine = Hint.PlantBite;
            if (mine == Hint.None && Room.Lava.Length > 0 && c.DashReady)
                foreach (Enemy e in Enemies)
                    if (e.Alive && !e.Def.Locks && e.Type != EnemyType.Bat && !e.Def.Hazard && (!FireImmune(e.Type) || e.Type == EnemyType.MagmaShard)
                        && V2.Dist(e.Pos, c.Pos) <= 2.5f && Hits(Room.Lava, e.Pos, e.Def.Radius + 1.2f)) { mine = Hint.Lava; break; }
            if (mine == Hint.None) return;
            bool urgent = mine == Hint.ImpSwap || mine == Hint.Vent;
            if (h == Hint.None || h == Hint.PickUp || h == Hint.Chest || h == Hint.SecretWall || h == Hint.MapReveal || h == Hint.Asleep || h == Hint.Dark
                || (urgent && h != Hint.DragToWalk && h != Hint.BarrelFuse)) h = mine;
        }
    }

    public static partial class Balance
    {
        // ---- regiao 4 (CONTEUDO_REGIOES §1.1, §5, §6). HIPOTESES; medicao em docs/BALANCE_REGIAO4.md.
        /// <summary>Lava: inimigo empurrado contra ela leva 3 (1x por empurrao; brasa morre). Respiro: ciclo de 4 s = 2,2 apagado + 0,8 brilhando
        /// + 1,0 de fogo; fogo tira 2 do courier e 3 do inimigo (1x por erupcao), racha a carga no chao e tira 25 do gelo.</summary>
        public const int LavaDamage = 3, VentCourierDamage = 2, VentEnemyDamage = 3;
        public const float VentCycle = 4f, VentOff = 2.2f, VentGlow = 0.8f;
        /// <summary>Fogo no chao: 1 de dano por segundo dentro (Botas nivel 5: a cada 2 s); gelo x3 a 1,5 m de fogo ou lava. Gelo perdido por golpe
        /// (pontos de 0-100): respiro 25, frasco 20, sopro 40, pancada/pouso/chicote 20, fogo no chao 10 por segundo.</summary>
        public const float FireTick = 1f, FireNear = 1.5f, VentIce = 25f, FlaskIce = 20f, BreathIce = 40f, SmashIce = 20f, FireIce = 10f;
        /// <summary>Barril no fogo do chao: o pavio fica com 1 s (respiro, frasco e sopro detonam na hora).</summary>
        public const float FireFuse = 1f;
        /// <summary>Courier no fogo do chao: o 1o dano depois de FireGrace s dentro (atravessar rapido nao queima), depois 1 por segundo.</summary>
        public const float FireGrace = 0.5f;
        /// <summary>Salamandra: circula a 2,5 m por 3 s, corta por ate 1,5 s; rastro de fogo de 0,45 m por 3 s a cada 0,5 s andando.</summary>
        public const float SalamanderOrbit = 2.5f, SalamanderCircle = 3f, SalamanderCut = 1.5f, SalamanderTrail = 0.5f, TrailR = 0.45f, TrailTime = 3f;
        /// <summary>Diabrete: 4-7 m do courier; some e surge 1 m atras dele (aviso 0,5 s, recarga 3 s); golpe no redemoinho atordoa 0,5 s.</summary>
        public const float ImpNear = 4f, ImpFar = 7f, ImpBlinkTele = 0.5f, ImpBlinkCd = 3f, ImpBehind = 1f, ImpHitStun = 0.5f;
        /// <summary>Kobold: fica a 3,5 m+; frasco de 1 m que vira poca de fogo por 4 s. Elemental: 2 brasas a +-0,8 m.</summary>
        public const float KoboldKeep = 3.5f, FlaskR = 1f, FlaskPool = 4f, ShardSpread = 0.8f;
        /// <summary>Capataz: chicote 3 x 0,6 m puxa a carga 1,5 m; giro de 3 m (aviso 1 s, fase 2); frasco de 1,2 m a cada 5 s (3,5 na fase 2);
        /// apito a cada 14 s chama ate 2 kobolds.</summary>
        public const float WhipHalf = 0.3f, WhipPull = 1.5f, SpinR = 3f, SpinTele = 1f, ForemanFlask = 5f, ForemanFlask2 = 3.5f, ForemanFlaskR = 1.2f, ForemanCallEvery = 14f, ForemanFlaskRange = 8f;
        public const int ForemanCalls = 2;
        /// <summary>Dragao: cone de 60 graus (cos 30 = 0,866) e 6 m; sopro 0,8 s (varredura 1,5 s girando 2 graus por passo); brasa de 0,7 m por
        /// 2 s. Voo: decola 0,6 s, 2,5 s no ar, sombra a 2 m/s, pouso em 2,2 m com aviso de 1,2 s, carga empurrada 2 m, cabeca baixa 3 s.
        /// Fases em 60% e 30% da vida; 2 ovos a cada 20 s (o 1o aos 10 s; 12 na fase 3), voo a cada 18 s (14 na fase 3; o 1o 4 s depois de entrar
        /// na fase 2); anda em y >= 9; ovo a 3 m+ do courier.</summary>
        public const float BreathCos = 0.8660254f, BreathTime = 0.8f, SweepTime = 1.5f, Cos2 = 0.99939083f, Sin2 = 0.0348995f, EmberR = 0.7f, EmberTime = 2f;
        public const float FlyTakeOff = 0.6f, FlyAir = 2.5f, FlyShadowSpeed = 2f, LandTele = 1.2f, LandR = 2.2f, LandPush = 2f, HeadDown = 3f;
        public const float DragonEggs1 = 20f, DragonEggs3 = 12f, DragonFly2 = 18f, DragonFly3 = 14f, DragonMinY = 9f, EggMinDist = 3f, DragonFirstFly = 4f;
        public const int DragonEggs = 2, DragonPhase2 = 60, DragonPhase3 = 30;
        /// <summary>Planta: morde a 1,2 m (aviso 0,3 s, 2 de dano, recarga 1 s); courier parado ao lado por 0,8 s leva 1 (recarga 1,5 s); ladrao
        /// carregando leva 1 por segundo. Mapa: ladroes +30% de velocidade atras dele; ladrao da carga sozinha em 6 s.</summary>
        public const float PlantReach = 1.2f, PlantTele = 0.3f, PlantCd = 1f, PlantIdle = 0.8f, PlantCourierCd = 1.5f, PlantCarry = 1f, MapThiefSpeed = 0.3f, MapAlone = 6f;
        public const int PlantDamage = 2;
    }
}
