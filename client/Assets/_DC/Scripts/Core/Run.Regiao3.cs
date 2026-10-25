using System;
using System.Collections.Generic;

namespace DC.Core
{
    // Regiao 3 (Cripta Assombrada, CONTEUDO_REGIOES §1.1, §3, §5, §6): escuro, teias, mortos-vivos, Viuva Aranha, Necromante, lanterna,
    // espelho e balao. Deterministico (rng da sala), sem Sin/Cos (angulos fixos em constantes).
    //
    // Para a View (o que ler):
    //   run.R3Of(e) -> R3Foe (null = sem estado da regiao 3): Act = o que faz (clipe/pose), Home = pedestal da gargula, Phase = fase do chefe.
    //   run.Untouchable(e): gargula de pedra ou voltando ao pedestal, Viuva no teto (nao e alvo; desenhe como estatua / sombra no teto).
    //   run.Webs (teias no chao: da sala e as cuspidas, Left < 0 = fixa), run.Spits (teia cuspida em voo: Arrow Pos/Dir), run.Bones (pilhas
    //   de ossos: Left = s ate remontar), run.Warns (avisos no chao, sempre visiveis no escuro: faixa da gargula, do cuspe, do leque e do raio,
    //   circulo do despencar da Viuva, runas do Necromante, fio da Viuva, raio disparando), run.Webbed (s preso na teia), run.GrabbedBy
    //   (mumia segurando o courier), Cargo.Out (lanterna apagada), Cargo.Fuse no balao = s no chao (escapa em Balance.BalloonRise),
    //   Cargo.PickExtra/PickT (teia, casulo), run.LightCut/TorchCut (luz a menos: Necromante fase 3). O tiro refletido pelo espelho e um
    //   Shot de estilingue com Damage 2.
    //   Estados: EnemyState.Telegraph so no golpe corpo a corpo (mordida, agarrao da mumia), para o bot e a dica de dash; aviso em linha ou
    //   no chao (cuspe, mergulho, leque, fio, raio) = EnemyState.Lurk + R3Foe.Act + run.Warns; fantasma possuindo = EnemyState.Grab
    //   (Act Possess); mumia segurando = Grab (Act Hold); gargula de pedra = Idle (Statue), mergulho = Chase, empoleirada = Recover,
    //   voltando = Flee; Viuva no teto = Flee (Ceiling/Drop); Necromante na fumaca = Flee (Smoke).
    // Eventos novos (A/B): EnemyRevive A=id novo B=remontes · BonesScatter A=id do esqueleto (ossos espalhados de vez) · Webbed A=0 courier /
    //   1 carga · Raise A=id do Necromante B=quantos (runas) · Blink A=id (Pos = destino) · LanternOut / LanternLit A=id da carga · Reflect A=0
    //   flecha / 1 teia / 2 raio, B=rachaduras do espelho · WebTorn A=0 (o dash rasgou; Pos = teia).
    // Reusados: Telegraph/Strike B=(int)tipo (fantasma possuindo; aranha cuspe e mordida; mumia agarra; gargula acorda/mergulha; Viuva leque,
    //   fio e despencar; Necromante raio), ThiefGrab/ThiefEscape (fantasma possui / some com a carga), BossPhase A=id B=fase (Viuva 2;
    //   Necromante 2 e 3), Summon A=id B=quantos (Viuva chama aranhas), Stun A=id (reflexo), EnemyDie/CargoDrop/CargoPickup como sempre.
    // Ajustes medidos sobre o desenho (BALANCE_REGIAO3.md): esqueletos erguidos pelo Necromante com vida sem nivel; pegar de volta a carga
    // possuida repele o fantasma 1,5 m; morcego fora da R3-1, R3-3 e R3-5; rotas do M3-4/5/7/9/16 e icones de ameaca pela medicao.

    /// <summary>O que o inimigo da regiao 3 esta fazendo (a View escolhe o clipe por aqui).</summary>
    public enum R3Act { None, Statue, Wake, Dive, Perch, Fly, Possess, Spit, Grab, Hold, Fan, Thread, Pull, Ceiling, Drop, Cast, Raise, Smoke }

    /// <summary>Estado proprio de um inimigo da regiao 3 (run.R3Of).</summary>
    public sealed class R3Foe
    {
        public R3Act Act; public V2 Home; public int Phase = 1, Revives;
        internal float T, T2, T3, RuneT, BeamT; internal int Hits, LastHp, MaxHp, Mask, Beams; internal bool Called25, Raised;
        internal Cargo Pull; internal V2 A, B, Beam0, Beam1;
        internal readonly List<V2> Runes = new List<V2>();
        internal readonly List<int> HitIds = new List<int>();
    }

    /// <summary>Teia no chao: da sala (Left &lt; 0, fica ate o dash rasgar) ou cuspida (Left = s).</summary>
    public sealed class Cobweb { public V2 Pos; public float Radius, Left = -1f; internal float Immune; }
    /// <summary>Pilha de ossos do esqueleto derrubado: remonta em Left s (no maximo Balance.BonesRevives vezes).</summary>
    public sealed class BonePile { public V2 Pos; public float Left; public int Revives; internal bool Wave, Raised; }
    public enum WarnKind { Strip, Circle, Rune, Thread, Beam }
    /// <summary>Aviso no chao: faixa/fio/raio de From a To com largura Width, ou circulo/runa em From com raio Radius. Owner = id do inimigo.</summary>
    public sealed class Warn { public WarnKind Kind; public int Owner; public V2 From, To; public float Width, Radius; }

    public static partial class Balance
    {
        // ---- regiao 3 (CONTEUDO_REGIOES §1.1, §5, §6). Hipoteses medidas em docs/historico/BALANCE_REGIAO3.md.
        /// <summary>Teia: carregando prende 1 s (depois 1,5 s imune aquela teia); sem carga x0,5; carga dentro leva +1 s para pegar (o ladrao
        /// tambem); o dash rasga. Conta como dentro com o centro a Radius + WebEdge.</summary>
        public const float WebStuck = 1f, WebImmune = 1.5f, WebSlow = 0.5f, WebPick = 1f, WebEdge = 0.2f;
        /// <summary>Cuspe da aranha: courier a 2-5 m, aviso 0,6 s, projetil 6 m/s raio 0,25 m ate 6 m; sem acertar vira teia de 0,6 m por 6 s;
        /// recarga 4 s.</summary>
        public const float SpitSpeed = 6f, SpitR = 0.25f, SpitMin = 2f, SpitMax = 5f, SpitTele = 0.6f, SpitCd = 4f, SpitRange = 6f, SpitWeb = 0.6f, SpitWebTime = 6f;
        /// <summary>Fantasma: a carga possuida flutua a 0,8 m/s ate a parede lateral e some 2 s depois; pegar de volta repele o fantasma 1,5 m
        /// (e o toque dele recarrega: sem isso o toque logo depois racharia a reliquia que acabou de voltar para as maos).</summary>
        public const float GhostFloat = 0.8f, GhostVanish = 2f, GhostRecoil = 1.5f;
        /// <summary>Esqueleto: pilha remonta em 4 s se a carga do contrato nao estiver a 3 m; dash a 0,6 m espalha; no maximo 2 remontes.</summary>
        public const float BonesRise = 4f, BonesNear = 3f, BonesDash = 0.6f; public const int BonesRevives = 2;
        /// <summary>Mumia: agarrado leva 1 a cada 1,2 s; solta com dash ou 2 golpes nela.</summary>
        public const float MummyTick = 1.2f; public const int MummyHits = 2;
        /// <summary>Gargula: faixa de 6 x 0,9 m, mergulho a 9 m/s, 2,5 s empoleirada (vulneravel), 1,5 s voltando (intocavel).</summary>
        public const float GargLen = 6f, GargWidth = 0.9f, GargSpeed = 9f, GargPerch = 3f, GargBack = 1.5f; public const int GargEnemyDamage = 2;
        /// <summary>Viuva: leque de 3 cuspes (-20, 0, +20 graus) a cada 6 s (aviso 0,8 s); fio ate 7 m (aviso 1 s) puxa a 2,5 m/s (2 golpes, o dash
        /// cruzando ou pegar cortam; recarga 4 s); casulo leva 1,5 s para pegar; fase 2 (50%): teto 3 s + circulo de 1,3 m por 1,2 s a cada 8 s;
        /// chama 2 aranhas a 50% e a 25%.</summary>
        public const float WidowFirstFan = 2f, WidowFanEvery = 6f, WidowFanTele = 0.8f, WidowFanCos = 0.93969262f, WidowFanSin = 0.34202014f,
                           WidowThread = 7f, WidowThreadTele = 1f, WidowPull = 2.5f, WidowThreadCd = 4f, WidowCocoon = 1.5f,
                           WidowCeilEvery = 8f, WidowCeil = 3f, WidowDropTele = 1.2f, WidowDropR = 1.3f;
        public const int WidowHitsCut = 2, WidowCalls = 2, WidowDropDamage = 2;
        /// <summary>Necromante: raio 9 x 0,6 m (dispara 0,3 s; fase 2 duplo a +-15 graus); ergue 3 (fase 2: 4) a cada 15 s com runa de 1 s, no
        /// maximo 4 esqueletos; salta com o courier a 3 m (fumaca 0,5 s, recarga 3 s); fase 3 (20%) salta a cada 4 s e a luz do courier perde
        /// 1 m e a das tochas 0,5 m. Raio refletido: 6 e atordoa 1 s.</summary>
        public const float NecroRayWidth = 0.6f, NecroRayFire = 0.3f, NecroTwinCos = 0.99254615f, NecroTwinSin = 0.12186934f, NecroFirstRaise = 5f,
                           NecroRaiseEvery = 15f, NecroRune = 1f, NecroBlinkNear = 3f, NecroSmoke = 0.5f, NecroBlinkCd = 3f, NecroBlinkEvery = 4f,
                           NecroLightCut = 1f, NecroTorchCut = 0.5f, NecroReflectStun = 1f;
        public const int NecroRaise = 3, NecroRaise2 = 4, NecroMaxSkeletons = 4, NecroReflectDamage = 6;
        /// <summary>Lanterna: reacende carregada a 1 m de uma tocha por 0,5 s.</summary>
        public const float LanternTorch = 1f, LanternRelight = 0.5f;
        /// <summary>Espelho: reflete o que chega de frente (cos 60 graus = 0,5 com o Facing); flecha refletida tira 2; teia refletida prende 2 s.</summary>
        public const float MirrorCone = 0.5f, MirrorWebStun = 2f; public const int MirrorArrowDamage = 2;
        /// <summary>Balao: +15% de velocidade carregando; no chao escapa em 4 s. Neblina na cripta: a luz do courier perde 1 m.</summary>
        public const float BalloonSpeed = 1.15f, BalloonRise = 4f, FogCryptCut = 1f;
    }

    public sealed partial class Run
    {
        /// <summary>Teias no chao desta visita (da sala e cuspidas).</summary>
        public readonly List<Cobweb> Webs = new List<Cobweb>();
        /// <summary>Teias cuspidas em voo (aranha, leque da Viuva): Pos, Dir, Owner.</summary>
        public readonly List<Arrow> Spits = new List<Arrow>();
        /// <summary>Pilhas de ossos esperando remontar.</summary>
        public readonly List<BonePile> Bones = new List<BonePile>();
        /// <summary>Avisos no chao deste passo (refeitos a cada passo pelo estado dos inimigos).</summary>
        public readonly List<Warn> Warns = new List<Warn>();
        /// <summary>Segundos que o courier ainda fica preso na teia (0 = livre; o dash solta).</summary>
        public float Webbed { get; private set; }
        /// <summary>Mumia que segura o courier (null = ninguem).</summary>
        public Enemy GrabbedBy { get; private set; }
        /// <summary>Luz a menos (m): courier (Necromante fase 3, Neblina na cripta) e tochas (Necromante fase 3).</summary>
        public float LightCut => lightCut3 + (Has(Modifier.Fog) && Room != null && Room.Region == Regiao3.Id ? Balance.FogCryptCut : 0f);
        public float TorchCut => torchCut3;

        readonly Dictionary<int, R3Foe> foes3 = new Dictionary<int, R3Foe>();
        readonly int[] cracks3 = new int[4];
        float lightCut3, torchCut3, relight3; bool webSlow3; int hp3; Cargo cocoon3;

        /// <summary>Estado da regiao 3 do inimigo (null = nenhum).</summary>
        public R3Foe R3Of(Enemy e) => foes3.TryGetValue(e.Id, out R3Foe f) ? f : null;

        R3Foe F3(Enemy e)
        {
            if (foes3.TryGetValue(e.Id, out R3Foe f)) return f;
            f = new R3Foe { Home = e.Pos, LastHp = e.Hp, MaxHp = e.Hp };
            if (e.Type == EnemyType.Gargoyle) f.Act = R3Act.Statue;
            if (e.Type == EnemyType.SpiderWidow) f.T = Balance.WidowFirstFan;
            if (e.Type == EnemyType.Necromancer) f.T = Balance.NecroFirstRaise;
            foes3[e.Id] = f;
            return f;
        }

        static V2 Toward3(V2 a, V2 b, float step) { V2 d = b - a; float l = d.Len; return l <= step ? b : a + d * (step / l); }
        static V2 Rot3(V2 v, float cos, float sin) => new V2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
        Enemy ById3(int id) { foreach (Enemy e in Enemies) if (e.Id == id && e.Alive) return e; return null; }
        Cargo Held3(CargoType t) { foreach (Cargo g in Cargos) if (g.State == CargoState.Held && g.Type == t) return g; return null; }

        /// <summary>A carga do contrato (nas maos, no chao, levada; chocada = o filhote) esta a 3 m de p? (o esqueleto nao volta)</summary>
        bool CargoNear3(V2 p)
        {
            Cargo m = Main; V2 at;
            if (m.State == CargoState.Held) at = Courier.Pos; else if (m.State != CargoState.Gone) at = m.Pos; else if (Pet != null) at = Pet.Pos; else return false;
            return V2.Dist(at, p) <= Balance.BonesNear;
        }

        // ------------------------------------------------------------------ ganchos

        partial void EnterRoomR3()
        {
            Webs.Clear(); Spits.Clear(); Bones.Clear(); Warns.Clear(); foes3.Clear();
            Webbed = 0f; GrabbedBy = null; lightCut3 = torchCut3 = relight3 = 0f; webSlow3 = false; cocoon3 = null;
            foreach (Web w in Room.Webs) Webs.Add(new Cobweb { Pos = w.Pos, Radius = w.Radius });
            foreach (Enemy e in Enemies) if (e.Def.Region == Regiao3.Id) F3(e);
            hp3 = Courier.Hp;
            for (int i = 0; i < Cargos.Count && i < cracks3.Length; i++) cracks3[i] = Cargos[i].Cracks;
        }

        partial void EnemyR3(Enemy e, ref bool handled)
        {
            switch (e.Type)
            {
                case EnemyType.Ghost: Ghost3(e); break;
                case EnemyType.Spider: Spider3(e); break;
                case EnemyType.Mummy: Mummy3(e); break;
                case EnemyType.Gargoyle: Gargoyle3(e); break;
                case EnemyType.SpiderWidow: Widow3(e); break;
                case EnemyType.Necromancer: Necro3(e); break;
                default: return;   // esqueleto: corpo a corpo comum (Melee); a regra dele e a pilha de ossos (EnemyDiedR3)
            }
            handled = true;
        }

        partial void HitMulR3(Enemy e, ref int mul)
        {
            if (e.Type != EnemyType.Gargoyle && e.Type != EnemyType.SpiderWidow) return;
            R3Foe f = R3Of(e);
            bool off = e.Type == EnemyType.Gargoyle ? f == null || f.Act == R3Act.Statue || f.Act == R3Act.Fly
                                                    : f != null && (f.Act == R3Act.Ceiling || f.Act == R3Act.Drop);
            if (off) mul = 0;
        }

        partial void SpeedR3(ref float mul)
        {
            if (Webbed > 0f || GrabbedBy != null) { mul = 0f; return; }   // preso: so o dash tira
            if (webSlow3) mul *= Balance.WebSlow;
            if (Holds(CargoType.Balloon)) mul *= Balance.BalloonSpeed;
        }

        partial void DropR3(Cargo g, int reason)
        {
            if (reason == 0) return;   // pousar e com cuidado: so a queda (dash, ladrao atingido, fumaca) conta
            if (g.Type == CargoType.Lantern && Dark && !g.Out) { g.Out = true; Emit(Ev.LanternOut, g.Id, 0, g.Pos); }
            if (g.Type == CargoType.Mirror) Crack(g);
        }

        partial void EnemyDiedR3(Enemy e)
        {
            if (e == GrabbedBy) GrabbedBy = null;
            if (e.Type == EnemyType.Skeleton)
            {
                R3Foe f = R3Of(e); int rev = f != null ? f.Revives : 0;
                if (rev >= Balance.BonesRevives || CargoNear3(e.Pos)) Emit(Ev.BonesScatter, e.Id, 0, e.Pos);   // morre de vez
                else Bones.Add(new BonePile { Pos = e.Pos, Left = Balance.BonesRise, Revives = rev, Wave = e.Wave, Raised = f != null && f.Raised });
            }
            else if (e.Type == EnemyType.Necromancer)   // morto: todos os esqueletos caem de vez e a luz volta
            {
                foreach (Enemy s in Enemies)
                    if (s.Alive && s.Type == EnemyType.Skeleton) { s.Hp = 0; Emit(Ev.EnemyDie, s.Id, (int)s.Type, s.Pos); Emit(Ev.BonesScatter, s.Id, 0, s.Pos); }
                Bones.Clear(); lightCut3 = torchCut3 = 0f;
            }
        }

        partial void StepR3()
        {
            var c = Courier;
            const float dt = Balance.Step;
            if (c.State == CourierState.Dashing) Webbed = 0f; else if (Webbed > 0f) Webbed = Math.Max(0f, Webbed - dt);
            if (GrabbedBy != null && (!GrabbedBy.Alive || GrabbedBy.State != EnemyState.Grab || c.State == CourierState.Dashing || c.State == CourierState.Dead))
                ReleaseGrab3(GrabbedBy);
            UpdateWebs3();
            UpdateSpits3();
            if (!Running) return;
            ReflectArrows3();
            if (!Running) return;
            UpdateBones3();
            GhostPickBack3();
            UpdateLantern3();
            UpdateBalloon3();
            if (!Running) return;
            BuildWarns3();
            hp3 = c.Hp;
            for (int i = 0; i < Cargos.Count && i < cracks3.Length; i++) cracks3[i] = Cargos[i].Cracks;
        }

        partial void HintR3(ref Hint h)
        {
            if (Webbed > 0f) { h = Hint.Web; return; }
            if (GrabbedBy != null) { h = Hint.MummyGrab; return; }
            foreach (Enemy e in Enemies) if (e.Alive && e.Type == EnemyType.Ghost && e.Carry != null) { h = Hint.GhostCargo; return; }
            foreach (Cargo g in Cargos) if (g.Type == CargoType.Balloon && g.State == CargoState.Ground) { h = Hint.BalloonRise; return; }
            if (h == Hint.ArcherAim && Holds(CargoType.Mirror)) { h = Hint.MirrorFront; return; }
            if (h != Hint.None && h != Hint.Dark && h != Hint.PickUp) return;
            var c = Courier;
            if (Dark) foreach (Cargo g in Cargos) if (g.Type == CargoType.Lantern && g.Out && g.State == CargoState.Held) { h = Hint.LanternOut; return; }
            foreach (BonePile b in Bones) if (!CargoNear3(b.Pos)) { h = Hint.SkeletonRise; return; }
            if (!HasHeld) return;
            foreach (Enemy e in Enemies)
            {
                R3Foe f = e.Alive && e.Type == EnemyType.Gargoyle ? R3Of(e) : null;
                if (f != null && (f.Act == R3Act.Statue || f.Act == R3Act.Wake) && V2.Dist(e.Pos, c.Pos) <= e.Def.Trigger + 1.5f) { h = Hint.GargoyleWake; return; }
            }
            foreach (Cobweb w in Webs) if (V2.Dist(w.Pos, c.Pos) <= w.Radius + 1.2f) { h = Hint.Web; return; }
        }

        // ------------------------------------------------------------------ terreno: teias

        void UpdateWebs3()
        {
            var c = Courier;
            const float dt = Balance.Step;
            bool held = HasHeld; webSlow3 = false;
            for (int i = Webs.Count - 1; i >= 0; i--)
            {
                Cobweb w = Webs[i];
                if (w.Left > 0f) { w.Left -= dt; if (w.Left <= 1e-4f) { Webs.RemoveAt(i); continue; } }
                if (w.Immune > 0f) w.Immune -= dt;
                if (c.State == CourierState.Dead || V2.Dist(c.Pos, w.Pos) > w.Radius + Balance.WebEdge) continue;
                if (c.State == CourierState.Dashing) { Webs.RemoveAt(i); Emit(Ev.WebTorn, 0, 0, w.Pos); continue; }   // o dash rasga (ate o fim da visita)
                if (held)
                {
                    if (w.Immune <= 0f && Webbed <= 0f && BootsLevel < 3) { Webbed = Balance.WebStuck; w.Immune = Balance.WebStuck + Balance.WebImmune; Emit(Ev.Webbed, 0, 0, c.Pos); }
                }
                else if (!Has(Perk.SureFoot) && BootsLevel < 2) webSlow3 = true;
            }
            // carga dentro da teia (ou casulo da Viuva): mais tempo para pegar; o ladrao tambem demora +1 s
            if (cocoon3 != null && cocoon3.State != CargoState.Ground) cocoon3 = null;
            foreach (Cargo g in Cargos)
            {
                g.PickExtra = 0f;
                if (g.State != CargoState.Ground) continue;
                if (g == cocoon3) { g.PickExtra = Balance.WidowCocoon; continue; }
                foreach (Cobweb w in Webs) if (V2.Dist(g.Pos, w.Pos) <= w.Radius) { g.PickExtra = Balance.WebPick; break; }
            }
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive || e.State != EnemyState.Grab || (e.Type != EnemyType.Thief && e.Type != EnemyType.Captain)) continue;
                foreach (Cargo g in Cargos)
                    if (g.State == CargoState.Ground && g.PickExtra > 0f && V2.Dist(e.Pos, g.Pos) <= Balance.ThiefGrabRange + 1e-3f)
                    { e.GrabT = Math.Max(0f, e.GrabT - dt * g.PickExtra / (Balance.ThiefGrab + g.PickExtra)); break; }   // ritmo 0,5/(0,5+extra)
            }
        }

        void UpdateSpits3()
        {
            var c = Courier;
            float step = Balance.SpitSpeed * Balance.Step;
            for (int i = Spits.Count - 1; i >= 0 && Running; i--)
            {
                if (i >= Spits.Count) continue;
                Arrow a = Spits[i];
                a.Pos = a.Pos + a.Dir * step; a.Dist += step;
                if (c.State != CourierState.Dead && V2.Dist(a.Pos, c.Pos) <= Balance.CourierR + Balance.SpitR)
                {
                    Spits.RemoveAt(i);
                    Cargo mirror = Held3(CargoType.Mirror);
                    if (mirror != null && V2.Dot(a.Dir * -1f, c.Facing) >= Balance.MirrorCone)   // de frente: volta e prende quem cuspiu
                    {
                        Enemy o = ById3(a.Owner);
                        if (o != null) { o.Stun = Math.Max(o.Stun, Balance.MirrorWebStun); Emit(Ev.Stun, o.Id, 2, o.Pos); }
                        Crack(mirror);
                        Emit(Ev.Reflect, 1, mirror.Cracks, a.Pos);
                        continue;
                    }
                    Webbed = Math.Max(Webbed, Balance.WebStuck * (BootsLevel >= 4 ? 0.5f : 1f));
                    Emit(Ev.Webbed, 0, 0, c.Pos);
                    continue;
                }
                Cargo hit = null;
                foreach (Cargo g in Cargos) if (g.State == CargoState.Ground && V2.Dist(a.Pos, g.Pos) <= Balance.CargoR + Balance.SpitR) { hit = g; break; }
                bool stop = hit != null || a.Dist >= Balance.SpitRange || a.Pos.X < 0.3f || a.Pos.Y < 0.3f || a.Pos.X > Balance.RoomW - 0.3f || a.Pos.Y > Balance.RoomH - 0.3f
                            || ShotBlocked(Room, a.Pos, Balance.SpitR);
                if (!stop) continue;
                Spits.RemoveAt(i);
                V2 at = hit != null ? hit.Pos : a.Pos - a.Dir * step;   // vira teia onde parou (antes do obstaculo)
                ClampRoom(ref at, Balance.SpitWeb);
                Webs.Add(new Cobweb { Pos = at, Radius = Balance.SpitWeb, Left = Balance.SpitWebTime });
                if (hit != null) Emit(Ev.Webbed, 1, 0, at);
            }
        }

        // ------------------------------------------------------------------ espelho: reflete a flecha que chega de frente

        void ReflectArrows3()
        {
            var c = Courier;
            Cargo mirror = Held3(CargoType.Mirror);
            if (mirror == null || c.State == CourierState.Dead) return;
            float reach = Balance.ArrowSpeed * Balance.Step + 0.25f;   // pega a flecha que acertaria no proximo passo (o courier anda antes dela)
            for (int i = Arrows.Count - 1; i >= 0 && Running; i--)
            {
                Arrow a = Arrows[i];
                if (V2.Dot(a.Dir, c.Pos - a.Pos) <= 0f || V2.SegDist(c.Pos, a.Pos, a.Pos + a.Dir * reach) > Balance.CourierR + Balance.ArrowR) continue;
                if (V2.Dot(a.Dir * -1f, c.Facing) < Balance.MirrorCone) continue;   // de lado ou por tras acerta o courier (e nao racha o espelho)
                Arrows.RemoveAt(i);
                Enemy o = ById3(a.Owner);
                if (o != null) Shots.Add(new Shot { Pos = a.Pos, From = a.Pos, Target = o, Weapon = Weapon.Sling, Damage = Balance.MirrorArrowDamage });
                Crack(mirror);
                Emit(Ev.Reflect, 0, mirror.Cracks, a.Pos);
                if (mirror.State == CargoState.Gone) return;
            }
        }

        // ------------------------------------------------------------------ esqueleto: pilhas de ossos

        void UpdateBones3()
        {
            var c = Courier;
            for (int i = Bones.Count - 1; i >= 0; i--)
            {
                BonePile b = Bones[i];
                bool dash = c.State == CourierState.Dashing && V2.Dist(c.Pos, b.Pos) <= Balance.BonesDash + Balance.CourierR;
                if (dash || CargoNear3(b.Pos)) { Bones.RemoveAt(i); Emit(Ev.BonesScatter, 0, 0, b.Pos); continue; }   // espalhados de vez
                b.Left -= Balance.Step;
                if (b.Left > 1e-4f) continue;
                if (!CanSpawn(EnemyType.Skeleton)) { b.Left = 0.5f; continue; }   // teto de vivos: espera
                Bones.RemoveAt(i);
                Enemy s = Spawn(EnemyType.Skeleton, b.Pos);
                R3Foe sf = F3(s); sf.Revives = b.Revives + 1; s.Wave = b.Wave;
                if (b.Raised) { s.Hp = s.Def.Hp; sf.Raised = true; sf.LastHp = sf.MaxHp = s.Hp; }   // erguido pelo Necromante: vida sem nivel
                Emit(Ev.EnemyRevive, s.Id, b.Revives + 1, s.Pos);
            }
        }

        // ------------------------------------------------------------------ fantasma

        void Ghost3(Enemy e)
        {
            var c = Courier; R3Foe f = F3(e);
            const float dt = Balance.Step;
            if (e.Carry != null)   // possuindo: a carga flutua ate a parede lateral mais perto, espera 2 s e some com ela
            {
                Cargo g = e.Carry;
                f.Act = R3Act.Possess; e.State = EnemyState.Flee;
                V2 wall = new V2(g.Pos.X < Balance.RoomW * 0.5f ? Balance.CargoR : Balance.RoomW - Balance.CargoR, g.Pos.Y);
                if (V2.Dist(g.Pos, wall) > 0.02f) { g.Pos = Toward3(g.Pos, wall, Balance.GhostFloat * dt); e.ExitT = 0f; }
                else { e.ExitT += dt; if (e.ExitT >= Balance.GhostVanish - 1e-4f) { Escape(e); return; } }
                e.Pos = g.Pos;
                return;
            }
            f.Act = R3Act.None;
            Cargo t = null; float best = float.MaxValue;
            foreach (Cargo g in Cargos) if (g.State == CargoState.Ground) { float d = V2.Dist(e.Pos, g.Pos); if (d < best) { best = d; t = g; } }
            float reach = e.Def.Reach + Balance.CargoR;
            if (e.State == EnemyState.Grab)   // aura lilas (0,6 s): possui no fim se a carga ainda estiver ali; o dash cancela
            {
                if (t == null || best > reach + 0.2f) { e.State = EnemyState.Chase; return; }
                f.Act = R3Act.Possess;
                e.Tele -= dt;
                if (e.Tele > 1e-4f) return;
                t.State = CargoState.Stolen; e.Carry = t; e.ExitT = 0f; e.Hits = 0; e.State = EnemyState.Flee;
                Emit(Ev.ThiefGrab, t.Id, e.Id, t.Pos);
                CargoGrabbed(e);
                return;
            }
            e.State = EnemyState.Chase;   // atravessa obstaculo e parede: anda em linha reta
            if (t != null)
            {
                if (best <= reach) { e.State = EnemyState.Grab; e.Tele = e.Def.Tele; f.Act = R3Act.Possess; Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos); return; }
                e.Pos = Toward3(e.Pos, t.Pos, e.Def.Speed * dt); ClampRoom(ref e.Pos, e.Def.Radius);
                return;
            }
            if (c.State == CourierState.Dead) return;
            if (V2.Dist(e.Pos, c.Pos) > e.Def.Radius + Balance.CourierR - 0.05f) { e.Pos = Toward3(e.Pos, ChaseTarget(e), e.Def.Speed * dt); ClampRoom(ref e.Pos, e.Def.Radius); }
            Touch(e);
        }

        /// <summary>Chegar a carga possuida a PickupRange (andando) a pega de volta e quebra a possessao.</summary>
        void GhostPickBack3()
        {
            var c = Courier;
            if (c.IdleT > 0f || (c.State != CourierState.Walking && c.State != CourierState.Carrying)) return;
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive || e.Type != EnemyType.Ghost || e.Carry == null || V2.Dist(e.Carry.Pos, c.Pos) > Sim.PickupRange + 1e-4f) continue;
                Cargo g = e.Carry;
                e.Carry = null; e.State = EnemyState.Chase; e.ExitT = 0f; e.Cd = e.Def.Cooldown;   // a possessao quebrada repele o fantasma
                V2 away = (e.Pos - c.Pos).Norm; if (away.Len < 0.5f) away = V2.Up;
                e.Pos = e.Pos + away * Balance.GhostRecoil; ClampRoom(ref e.Pos, e.Def.Radius);
                g.State = CargoState.Held; g.Pos = c.Pos; g.AloneT = 0f; g.PickT = 0f; g.Fuse = 0f; g.Armed = true;
                c.State = CourierState.Carrying;
                Emit(Ev.CargoPickup, g.Id, 0, g.Pos);
            }
        }

        // ------------------------------------------------------------------ aranha

        void Spider3(Enemy e)
        {
            var c = Courier; R3Foe f = F3(e);
            const float dt = Balance.Step;
            f.T = Math.Max(0f, f.T - dt);   // recarga do cuspe
            if (f.Act == R3Act.Spit)   // cuspe: aviso em linha (Aim; Warns), estado Lurk (o Telegraph e so o da mordida)
            {
                e.State = EnemyState.Lurk;
                e.Tele -= dt;
                if (e.Tele > 1e-4f) return;
                Spits.Add(new Arrow { Pos = e.Pos + e.Aim * (e.Def.Radius + 0.05f), Dir = e.Aim, Owner = e.Id });
                Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
                e.State = EnemyState.Recover; e.Aim = V2.Zero; f.T = Balance.SpitCd; f.Act = R3Act.None;
                return;
            }
            if (e.State != EnemyState.Telegraph && f.T <= 0f && c.State != CourierState.Dead)
            {
                float d = V2.Dist(e.Pos, c.Pos);
                if (d >= Balance.SpitMin && d <= Balance.SpitMax && nav.ShotClear(e.Pos, c.Pos, Balance.SpitR))
                {
                    e.State = EnemyState.Lurk; e.Tele = Balance.SpitTele; e.Aim = (c.Pos - e.Pos).Norm; f.Act = R3Act.Spit;
                    Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                    return;
                }
            }
            Melee(e);   // persegue e morde (aviso 0,4 s)
        }

        // ------------------------------------------------------------------ mumia

        void Mummy3(Enemy e)
        {
            var c = Courier; R3Foe f = F3(e);
            const float dt = Balance.Step;
            int hit = e.Hp < f.LastHp ? 1 : 0; f.LastHp = e.Hp;
            if (e.State == EnemyState.Grab)
            {
                if (GrabbedBy != e) { e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown; f.Act = R3Act.None; return; }
                f.Act = R3Act.Hold;
                f.Hits += hit;
                if (f.Hits >= (BootsLevel >= 4 ? 1 : Balance.MummyHits)) { ReleaseGrab3(e); return; }   // 2 golpes soltam (Botas 4: 1)
                f.T -= dt;
                if (f.T <= 1e-4f) { f.T = Balance.MummyTick; DamageCourier(e.Def.Damage); }
                return;
            }
            if (e.State == EnemyState.Telegraph)
            {
                f.Act = R3Act.Grab;
                e.Tele -= dt;
                if (e.Tele > 1e-4f) return;
                Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
                f.Act = R3Act.None;
                if (c.State != CourierState.Dead && c.State != CourierState.Dashing && GrabbedBy == null && V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= e.Def.Reach)
                {
                    e.State = EnemyState.Grab; GrabbedBy = e; f.Hits = 0; f.T = Balance.MummyTick; f.Act = R3Act.Hold;
                    DamageCourier(e.Def.Damage);
                }
                else { e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown; }
                return;
            }
            f.Act = R3Act.None;
            Melee(e);   // anda a 1 m/s e comeca o aviso; o golpe (agarrar) e o daqui
        }

        void ReleaseGrab3(Enemy e)
        {
            if (GrabbedBy == e) GrabbedBy = null;
            if (e.Alive && e.State == EnemyState.Grab) { e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown; }
            R3Foe f = R3Of(e); if (f != null) f.Act = R3Act.None;
        }

        // ------------------------------------------------------------------ gargula

        void Gargoyle3(Enemy e)
        {
            var c = Courier; R3Foe f = F3(e);
            const float dt = Balance.Step;
            switch (f.Act)
            {
                case R3Act.Wake:   // olhos vermelhos e faixa no chao (Warns)
                    e.State = EnemyState.Lurk;
                    f.T -= dt;
                    if (f.T > 1e-4f) return;
                    f.Act = R3Act.Dive; f.A = e.Pos; f.B = e.Pos + e.Aim * Balance.GargLen; ClampRoom(ref f.B, e.Def.Radius);
                    f.Mask = 0; f.HitIds.Clear();
                    Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
                    return;
                case R3Act.Dive:
                    e.State = EnemyState.Chase;
                    V2 from = e.Pos;
                    e.Pos = Toward3(e.Pos, f.B, Balance.GargSpeed * dt);
                    DiveHits3(e, f, from);
                    if (Running && V2.Dist(e.Pos, f.B) < 1e-3f) { f.Act = R3Act.Perch; f.T = Balance.GargPerch; }
                    return;
                case R3Act.Perch:   // no chao, vulneravel
                    e.State = EnemyState.Recover;
                    f.T -= dt;
                    if (f.T > 1e-4f) return;
                    f.Act = R3Act.Fly; f.A = e.Pos; f.T = Balance.GargBack;
                    return;
                case R3Act.Fly:   // volta ao pedestal (intocavel)
                    e.State = EnemyState.Flee;
                    f.T -= dt;
                    e.Pos = f.A + (f.Home - f.A) * (1f - Math.Max(0f, f.T) / Balance.GargBack);
                    if (f.T <= 1e-4f) { e.Pos = f.Home; f.Act = R3Act.Statue; e.Cd = e.Def.Cooldown; }
                    return;
                default:   // pedra: acorda com o courier a 3 m COM a carga nas maos
                    f.Act = R3Act.Statue; e.State = EnemyState.Idle; e.Pos = f.Home;
                    if (e.Cd > 0f || c.State == CourierState.Dead || !HasHeld || V2.Dist(e.Pos, c.Pos) - Balance.CourierR > e.Def.Trigger) return;
                    f.Act = R3Act.Wake; f.T = e.Def.Tele; e.State = EnemyState.Lurk;
                    e.Aim = (c.Pos - e.Pos).Norm; if (e.Aim.Len < 0.5f) e.Aim = new V2(0f, -1f);
                    Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                    return;
            }
        }

        /// <summary>Mergulho: tudo na faixa varrida neste passo leva 1 vez (courier 2, carga no chao racha 1, inimigos 2).</summary>
        void DiveHits3(Enemy e, R3Foe f, V2 from)
        {
            var c = Courier; float half = Balance.GargWidth * 0.5f;
            if ((f.Mask & 1) == 0 && c.State != CourierState.Dead && V2.SegDist(c.Pos, from, e.Pos) <= half + Balance.CourierR) { f.Mask |= 1; DamageCourier(e.Def.Damage); if (!Running) return; }
            for (int i = 0; i < Cargos.Count && i < 8; i++)
            {
                Cargo g = Cargos[i];
                if (g.State != CargoState.Ground || (f.Mask & 2 << i) != 0 || V2.SegDist(g.Pos, from, e.Pos) > half + Balance.CargoR) continue;
                f.Mask |= 2 << i; Crack(g);
                if (!Running) return;
            }
            for (int i = 0; i < Enemies.Count; i++)
            {
                Enemy o = Enemies[i];
                if (o == e || !o.Alive || o.Def.Hazard || f.HitIds.Contains(o.Id) || V2.SegDist(o.Pos, from, e.Pos) > half + o.Def.Radius) continue;
                f.HitIds.Add(o.Id);
                bool fought = FoughtHere; Hit(o, Balance.GargEnemyDamage); FoughtHere = fought;   // atropelado nao e luta escolhida (H1)
            }
        }

        // ------------------------------------------------------------------ Viuva Aranha (M3)

        void Widow3(Enemy e)
        {
            var c = Courier; R3Foe f = F3(e);
            const float dt = Balance.Step;
            int hit = e.Hp < f.LastHp ? 1 : 0; f.LastHp = e.Hp;
            if (f.Phase < 2 && e.Hp * 2 <= f.MaxHp) { f.Phase = 2; f.T3 = 1f; Emit(Ev.BossPhase, e.Id, 2, e.Pos); Call3(e, Balance.WidowCalls); }
            if (!f.Called25 && e.Hp * 4 <= f.MaxHp) { f.Called25 = true; Call3(e, Balance.WidowCalls); }
            f.T -= dt; f.T2 -= dt; if (f.Phase >= 2) f.T3 -= dt;
            switch (f.Act)
            {
                case R3Act.Fan:
                    e.State = EnemyState.Lurk;
                    e.Tele -= dt;
                    if (e.Tele > 1e-4f) return;
                    for (int k = -1; k <= 1; k++)
                    {
                        V2 d = k == 0 ? e.Aim : Rot3(e.Aim, Balance.WidowFanCos, k * Balance.WidowFanSin);
                        Spits.Add(new Arrow { Pos = e.Pos + d * (e.Def.Radius + 0.05f), Dir = d, Owner = e.Id });
                    }
                    Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
                    f.Act = R3Act.None; e.State = EnemyState.Recover; f.T = Balance.WidowFanEvery;
                    return;
                case R3Act.Thread:   // fio brilhando ate a carga
                    e.State = EnemyState.Lurk;
                    if (f.Pull == null || f.Pull.State != CargoState.Ground) { EndPull3(e, f); return; }
                    e.Tele -= dt;
                    if (e.Tele > 1e-4f) return;
                    f.Act = R3Act.Pull; f.Hits = 0; e.State = EnemyState.Grab;
                    Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
                    CargoGrabbed(e);
                    return;
                case R3Act.Pull:   // puxa a carga a 2,5 m/s; pegar, 2 golpes ou o dash cruzando o fio cortam; chegando vira casulo
                {
                    e.State = EnemyState.Grab;
                    f.Hits += hit;
                    Cargo g = f.Pull;
                    if (g == null || g.State != CargoState.Ground || f.Hits >= Balance.WidowHitsCut
                        || (c.State == CourierState.Dashing && V2.SegDist(c.Pos, e.Pos, g.Pos) <= Balance.CourierR + 0.1f)) { EndPull3(e, f); return; }
                    float stop = e.Def.Radius + Balance.CargoR + 0.1f;
                    g.Pos = Toward3(g.Pos, e.Pos, Balance.WidowPull * dt);
                    if (V2.Dist(g.Pos, e.Pos) <= stop) { g.Pos = FreeNear(Room, e.Pos + (g.Pos - e.Pos).Norm * stop, Balance.CargoR); cocoon3 = g; EndPull3(e, f); }
                    return;
                }
                case R3Act.Ceiling:   // no teto (intocavel)
                    e.State = EnemyState.Flee;
                    e.Tele -= dt;
                    if (e.Tele > 1e-4f) return;
                    f.Act = R3Act.Drop; e.Tele = Balance.WidowDropTele; f.A = DropSpot3();
                    Emit(Ev.Telegraph, e.Id, (int)e.Type, f.A);
                    return;
                case R3Act.Drop:   // circulo de 1,3 m no chao e despenca
                    e.State = EnemyState.Flee;
                    e.Tele -= dt;
                    if (e.Tele > 1e-4f) return;
                    e.Pos = FreeNear(Room, f.A, e.Def.Radius);
                    f.Act = R3Act.None; e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown; f.T3 = Balance.WidowCeilEvery;
                    Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
                    if (c.State != CourierState.Dead && V2.Dist(c.Pos, f.A) <= Balance.WidowDropR + Balance.CourierR) { DamageCourier(Balance.WidowDropDamage); if (!Running) return; }
                    for (int i = 0; i < Cargos.Count && Running; i++) if (Cargos[i].State == CargoState.Ground && V2.Dist(Cargos[i].Pos, f.A) <= Balance.WidowDropR + Balance.CargoR) Crack(Cargos[i]);
                    return;
            }
            f.Act = R3Act.None;
            if (e.State == EnemyState.Telegraph) { Melee(e); return; }   // mordida em curso
            if (c.State == CourierState.Dead) return;
            if (f.Phase >= 2 && f.T3 <= 0f) { f.Act = R3Act.Ceiling; e.Tele = Balance.WidowCeil; e.State = EnemyState.Flee; return; }
            if (f.T <= 0f)
            {
                f.Act = R3Act.Fan; e.State = EnemyState.Lurk; e.Tele = Balance.WidowFanTele; e.Aim = (c.Pos - e.Pos).Norm;
                if (e.Aim.Len < 0.5f) e.Aim = new V2(0f, -1f);
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            if (f.T2 <= 0f)
            {
                Cargo t = null; float bd = Balance.WidowThread;
                foreach (Cargo g in Cargos)
                {
                    if (g.State != CargoState.Ground || g == cocoon3) continue;
                    float d = V2.Dist(e.Pos, g.Pos);
                    if (d <= bd && d > e.Def.Radius + 1f && nav.ShotClear(e.Pos, g.Pos, 0.1f)) { bd = d; t = g; }
                }
                if (t != null)
                {
                    f.Act = R3Act.Thread; f.Pull = t; e.Tele = Balance.WidowThreadTele; e.State = EnemyState.Lurk; e.Aim = V2.Zero;
                    Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                    return;
                }
            }
            Melee(e);   // persegue e morde
        }

        void EndPull3(Enemy e, R3Foe f) { f.Act = R3Act.None; f.Pull = null; f.T2 = Balance.WidowThreadCd; e.State = EnemyState.Recover; }

        /// <summary>Onde a Viuva despenca: na carga no chao (a do contrato primeiro) ou no courier, se ele carrega.</summary>
        V2 DropSpot3()
        {
            if (Main.State == CargoState.Ground) return Main.Pos;
            foreach (Cargo g in Cargos) if (g.State == CargoState.Ground) return g.Pos;
            return Courier.Pos;
        }

        /// <summary>Chama n aranhas ao lado de quem chamou (teto de vivos).</summary>
        void Call3(Enemy e, int n)
        {
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                if (!CanSpawn(EnemyType.Spider)) break;
                Enemy s = Spawn(EnemyType.Spider, e.Pos + new V2(i == 0 ? -1.5f : 1.5f, 0f));
                F3(s); k++;
            }
            if (k > 0) Emit(Ev.Summon, e.Id, k, e.Pos);
        }

        // ------------------------------------------------------------------ Necromante (B3)

        void Necro3(Enemy e)
        {
            var c = Courier; R3Foe f = F3(e);
            const float dt = Balance.Step;
            if (f.Phase < 2 && e.Hp * 2 <= f.MaxHp) { f.Phase = 2; Emit(Ev.BossPhase, e.Id, 2, e.Pos); }
            if (f.Phase < 3 && e.Hp * 5 <= f.MaxHp) { f.Phase = 3; lightCut3 = Balance.NecroLightCut; torchCut3 = Balance.NecroTorchCut; Emit(Ev.BossPhase, e.Id, 3, e.Pos); }
            f.T -= dt; f.T2 -= dt; f.T3 -= dt;
            if (f.RuneT > 0f)   // runas: os esqueletos saem quando elas acabam
            {
                f.RuneT -= dt;
                if (f.RuneT <= 1e-4f)
                {
                    foreach (V2 p in f.Runes)
                        if (CanSpawn(EnemyType.Skeleton))
                        {
                            Enemy s = Spawn(EnemyType.Skeleton, p); s.Hp = s.Def.Hp;   // erguidos em onda: vida sem nivel (BALANCE_REGIAO3.md)
                            R3Foe sf = F3(s); sf.Raised = true; sf.LastHp = sf.MaxHp = s.Hp;
                        }
                    f.Runes.Clear();
                }
            }
            else if (f.T <= 0f) Raise3(e, f);
            if (f.Act == R3Act.Smoke)
            {
                e.State = EnemyState.Flee;
                e.Tele -= dt;
                if (e.Tele > 1e-4f) return;
                Blink3(e, f);
                return;
            }
            if (f.Act == R3Act.Cast)
            {
                e.State = EnemyState.Lurk;
                e.Tele -= dt;
                if (e.Tele > 1e-4f) return;
                Ray3(e, f);
                f.Act = R3Act.None; e.State = EnemyState.Recover; e.Cd = e.Def.Cooldown;
                return;
            }
            f.Act = f.RuneT > 0f ? R3Act.Raise : R3Act.None;
            if (c.State == CourierState.Dead) return;
            bool near = V2.Dist(e.Pos, c.Pos) - Balance.CourierR <= Balance.NecroBlinkNear;
            if (Room.Blinks.Length > 1 && ((near && f.T2 <= 0f) || (f.Phase >= 3 && f.T3 <= 0f)))   // fumaca e salta
            {
                f.Act = R3Act.Smoke; e.Tele = Balance.NecroSmoke; e.State = EnemyState.Flee;
                return;
            }
            if (e.Cd <= 0f && V2.Dist(e.Pos, c.Pos) <= e.Def.Trigger)
            {
                f.Act = R3Act.Cast; e.State = EnemyState.Lurk; e.Tele = e.Def.Tele; f.Beams = f.Phase >= 2 ? 2 : 1;
                e.Aim = (c.Pos - e.Pos).Norm; if (e.Aim.Len < 0.5f) e.Aim = new V2(0f, -1f);
                Emit(Ev.Telegraph, e.Id, (int)e.Type, e.Pos);
                return;
            }
            e.State = e.Cd > 0f ? EnemyState.Recover : EnemyState.Idle;
        }

        /// <summary>Direcoes do raio: 1 na mira; na fase 2, duas a +-15 graus.</summary>
        V2 RayDir3(Enemy e, R3Foe f, int k) => f.Beams < 2 ? e.Aim : Rot3(e.Aim, Balance.NecroTwinCos, k == 0 ? Balance.NecroTwinSin : -Balance.NecroTwinSin);

        /// <summary>Fim do raio: 9 m ou ate a lapide/obstaculo (cobertura).</summary>
        V2 RayEnd3(V2 from, V2 dir)
        {
            float len = EnemyDef.All[(int)EnemyType.Necromancer].Reach;
            for (float s = 0.3f; s <= len; s += 0.1f)
            {
                V2 p = from + dir * s;
                if (p.X < 0f || p.Y < 0f || p.X > Balance.RoomW || p.Y > Balance.RoomH || ShotBlocked(Room, p, 0.1f)) return from + dir * Math.Max(0f, s - 0.1f);
            }
            return from + dir * len;
        }

        void Ray3(Enemy e, R3Foe f)
        {
            var c = Courier; float half = Balance.NecroRayWidth * 0.5f;
            Emit(Ev.Strike, e.Id, (int)e.Type, e.Pos);
            f.BeamT = Balance.NecroRayFire;
            bool courier = false; int cargoMask = 0;
            for (int k = 0; k < f.Beams; k++)
            {
                V2 end = RayEnd3(e.Pos, RayDir3(e, f, k));
                if (k == 0) f.Beam0 = end; else f.Beam1 = end;
                if (c.State != CourierState.Dead && V2.SegDist(c.Pos, e.Pos, end) <= half + Balance.CourierR) courier = true;
                for (int i = 0; i < Cargos.Count && i < 8; i++)
                    if (Cargos[i].State == CargoState.Ground && V2.SegDist(Cargos[i].Pos, e.Pos, end) <= half + Balance.CargoR) cargoMask |= 1 << i;
            }
            if (courier)
            {
                Cargo mirror = Held3(CargoType.Mirror);
                if (mirror != null && V2.Dot((c.Pos - e.Pos).Norm * -1f, c.Facing) >= Balance.MirrorCone)   // de frente: volta nele
                {
                    Crack(mirror);
                    Emit(Ev.Reflect, 2, mirror.Cracks, c.Pos);
                    Hit(e, Balance.NecroReflectDamage);
                    if (e.Alive) { e.Stun = Math.Max(e.Stun, Balance.NecroReflectStun); Emit(Ev.Stun, e.Id, 2, e.Pos); }
                }
                else DamageCourier(e.Def.Damage);
                if (!Running) return;
            }
            for (int i = 0; i < Cargos.Count && Running; i++) if ((cargoMask & 1 << i) != 0) Crack(Cargos[i]);
        }

        /// <summary>Ergue 3 (fase 2: 4) esqueletos nas lapides mais longe do courier, ate 4 vivos; runa de 1 s antes.</summary>
        void Raise3(Enemy e, R3Foe f)
        {
            f.T = Balance.NecroRaiseEvery;
            int n = Math.Min(f.Phase >= 2 ? Balance.NecroRaise2 : Balance.NecroRaise, Balance.NecroMaxSkeletons - Count(EnemyType.Skeleton));
            if (n <= 0) return;
            var spots = new List<V2>();
            foreach (Rect o in Room.Obstacles)   // lapide = obstaculo ao lado de um ponto de salto
            {
                bool grave = false;
                foreach (V2 b in Room.Blinks) if (V2.Dist(b, o.C) <= 1.5f) grave = true;
                if (grave) spots.Add(FreeNear(Room, new V2(o.C.X, o.C.Y - o.H.Y - 0.45f), 0.4f));
            }
            if (spots.Count == 0) foreach (V2 d in V2.Dirs8) spots.Add(FreeNear(Room, e.Pos + d * 1.5f, 0.4f));   // sem lapides: em volta dele
            V2 cp = Courier.Pos;
            for (int k = 0; k < n && spots.Count > 0; k++)
            {
                int bi = 0;
                for (int i = 1; i < spots.Count; i++) if (V2.Dist(spots[i], cp) > V2.Dist(spots[bi], cp)) bi = i;
                f.Runes.Add(spots[bi]); spots.RemoveAt(bi);
            }
            f.RuneT = Balance.NecroRune;
            Emit(Ev.Raise, e.Id, f.Runes.Count, e.Pos);
        }

        /// <summary>Salta para o ponto de salto mais longe do courier.</summary>
        void Blink3(Enemy e, R3Foe f)
        {
            V2 cp = Courier.Pos, best = e.Pos; float bd = -1f;
            foreach (V2 b in Room.Blinks)
            {
                if (V2.Dist(b, e.Pos) < 0.5f) continue;
                float d = V2.Dist(b, cp);
                if (d > bd) { bd = d; best = b; }
            }
            e.Pos = best;
            f.Act = R3Act.None; e.State = EnemyState.Idle; f.T2 = Balance.NecroBlinkCd; f.T3 = Balance.NecroBlinkEvery;
            Emit(Ev.Blink, e.Id, 0, e.Pos);
        }

        // ------------------------------------------------------------------ cargas: lanterna e balao

        void UpdateLantern3()
        {
            if (!Dark) { relight3 = 0f; return; }   // fora do escuro a lanterna e so uma carga leve
            var c = Courier;
            for (int i = 0; i < Cargos.Count; i++)
            {
                Cargo g = Cargos[i];
                if (g.Type != CargoType.Lantern || g.State == CargoState.Gone) continue;
                bool hitGround = g.State == CargoState.Ground && i < cracks3.Length && g.Cracks > cracks3[i];   // golpe no chao
                bool hitHands = g.State == CargoState.Held && hp3 - c.Hp >= 2;                                // golpe de 2+ no courier
                if (!g.Out && (hitGround || hitHands)) { g.Out = true; Emit(Ev.LanternOut, g.Id, 0, PosOf3(g)); }
                if (!g.Out || g.State != CargoState.Held) continue;
                bool torch = false;
                foreach (V2 t in Torches) if (V2.Dist(t, c.Pos) <= Balance.LanternTorch) torch = true;
                relight3 = torch ? relight3 + Balance.Step : 0f;
                if (relight3 >= Balance.LanternRelight - 1e-4f) { g.Out = false; relight3 = 0f; Emit(Ev.LanternLit, g.Id, 0, c.Pos); }
            }
        }

        V2 PosOf3(Cargo g) => g.State == CargoState.Held ? Courier.Pos : g.Pos;

        void UpdateBalloon3()
        {
            for (int i = 0; i < Cargos.Count && Running; i++)
            {
                Cargo g = Cargos[i];
                if (g.Type != CargoType.Balloon || g.State != CargoState.Ground) continue;
                g.Fuse += Balance.Step;   // no balao, Fuse = s no chao
                if (g.Fuse < Balance.BalloonRise - 1e-4f) continue;
                g.State = CargoState.Gone; g.Fuse = 0f;
                Sim.Dirty = true;
                if (g == Main) Finish(Outcome.Escaped);
            }
        }

        // ------------------------------------------------------------------ avisos no chao (refeitos a cada passo)

        void BuildWarns3()
        {
            Warns.Clear();
            foreach (Enemy e in Enemies)
            {
                if (!e.Alive) continue;
                R3Foe f = R3Of(e);
                if (f == null) continue;
                switch (f.Act)
                {
                    case R3Act.Wake:
                    {
                        V2 to = e.Pos + e.Aim * Balance.GargLen; ClampRoom(ref to, e.Def.Radius);
                        Warns.Add(new Warn { Kind = WarnKind.Strip, Owner = e.Id, From = e.Pos, To = to, Width = Balance.GargWidth });
                        break;
                    }
                    case R3Act.Spit:
                        Warns.Add(new Warn { Kind = WarnKind.Strip, Owner = e.Id, From = e.Pos, To = e.Pos + e.Aim * Balance.SpitRange, Width = Balance.SpitR * 2f });
                        break;
                    case R3Act.Fan:
                        for (int k = -1; k <= 1; k++)
                        {
                            V2 d = k == 0 ? e.Aim : Rot3(e.Aim, Balance.WidowFanCos, k * Balance.WidowFanSin);
                            Warns.Add(new Warn { Kind = WarnKind.Strip, Owner = e.Id, From = e.Pos, To = e.Pos + d * Balance.SpitRange, Width = Balance.SpitR * 2f });
                        }
                        break;
                    case R3Act.Thread:
                    case R3Act.Pull:
                        if (f.Pull != null) Warns.Add(new Warn { Kind = WarnKind.Thread, Owner = e.Id, From = e.Pos, To = f.Pull.Pos, Width = 0.1f });
                        break;
                    case R3Act.Drop:
                        Warns.Add(new Warn { Kind = WarnKind.Circle, Owner = e.Id, From = f.A, To = f.A, Radius = Balance.WidowDropR });
                        break;
                    case R3Act.Cast:
                        for (int k = 0; k < f.Beams; k++)
                            Warns.Add(new Warn { Kind = WarnKind.Strip, Owner = e.Id, From = e.Pos, To = RayEnd3(e.Pos, RayDir3(e, f, k)), Width = Balance.NecroRayWidth });
                        break;
                }
                if (f.BeamT > 0f) f.BeamT -= Balance.Step;   // raio visivel 0,3 s (tambem com o Necromante atordoado)
                if (f.BeamT > 0f)
                    for (int k = 0; k < f.Beams; k++) Warns.Add(new Warn { Kind = WarnKind.Beam, Owner = e.Id, From = e.Pos, To = k == 0 ? f.Beam0 : f.Beam1, Width = Balance.NecroRayWidth });
                foreach (V2 p in f.Runes) Warns.Add(new Warn { Kind = WarnKind.Rune, Owner = e.Id, From = p, To = p, Radius = 0.5f });
            }
        }
    }
}
