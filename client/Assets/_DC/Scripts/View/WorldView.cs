using System.Collections.Generic;
using DC.Core;
using UnityEngine;
using UnityEngine.UI;
using SortingGroup = UnityEngine.Rendering.SortingGroup;

namespace DC
{
    /// <summary>
    /// Desenha a sala e as entidades a partir do Run, sem regra nenhuma (so le o Core). Coordenadas de mundo = metros da sala (origem
    /// no canto inferior esquerdo). Projecao 60 graus (ART_BIBLE §2) numa transformacao so: o _root achata Y por Tilt (chao, paredes,
    /// decalques, particulas viram elipse 1 : 0,866) e tudo o que fica em pe (corpo, sprite, vida, placas) vive num no "EmPe" que desfaz
    /// o achatamento, com SortingGroup ordenado pelo pe (mais embaixo na tela = na frente). A entrada continua em coordenadas da sala.
    /// Camera ortografica fixa com a sala inteira + paredes + placas das portas, abaixo da faixa da HUD e dentro da area segura (Fit).
    /// Sprites reais (Resources/Sprites/&lt;preset&gt;, SpriteSheet) por entidade, no Paint*; sem folha, o procedural de sempre. Ficam
    /// procedurais por cima (ART_BIBLE §11): sombra de contato, contorno limao piscando da carga, rachaduras, telegraph, grade e
    /// cadeado da porta, aneis, numeros.
    /// </summary>
    public sealed class WorldView
    {
        // projecao: tela_y = Tilt*y + Lift*z (z = altura sobre o chao)
        const float Tilt = 0.866f, Lift = 0.5f;
        // o que a camera precisa mostrar: x e o pe em m da sala; o topo vai ate as placas das portas (_plateS, em m de tela)
        const float ViewL = -0.55f, ViewR = 9.55f, ViewB = -0.55f, ViewPad = 0.15f;
        const float WallT = 0.35f, TopWall = 0.5f, PlateH = 0.62f, PlateGap = 0.12f, Blink = 2f, PulseTime = 0.35f;
        // ponytail: estimativas da arte do Lote 1 ate a foto 540x960 (ASSETS §5 "a medir"): topo do arco e altura do vao acima da base
        // da parede (m de tela); fracao do clipe no quadro de impacto (golpe) e de soltar (arremesso); largura de projeto do bau
        const float ArchTop = 1.4f, ArchOpen = 1.15f, ImpactFrac = 0.6f, ThrowRelease = 0.5f, StrikeHold = 0.15f, HitTime = 0.3f, BauW = 0.8f;
        // altura de voo do morcego e do projetil (m); lado do contorno/rachaduras sobre a carga com sprite (m de tela)
        const float BatZ = 1.0f, ShotZ = 0.9f, CargoBox = 0.6f, WaveTime = 2.5f;
        // ordens de desenho: decalques do chao < OActor; dentro de um SortingGroup as ordens dos filhos sao so relativas
        const int OFloor = 0, OMark = 1, OShadow = 2, OBlock = 3, OWall = 4, ODoor = 5, OPlate = 6, OActShadow = 7, ORing = 8, OChest = 10,
            OCargo = 12, OPet = 14, OEnemy = 16, OCourier = 20, OActor = 100, OShot = 4000, OPart = 4100;

        static readonly string[] EnemySheet = { "morcego", "ladrao", "guarda", "bruto", "ogro" };
        static readonly string[] AttackClip = { null, null, "Bash", "Smash", "Slam" };
        /// <summary>Altura de projeto por papel (ART_BIBLE §4): barra de vida acima da cabeca do sprite.</summary>
        static readonly float[] EnemyH = { 0.7f, 1.0f, 1.5f, 1.8f, 2.8f };
        static readonly string[] CargoSheet = { "caixa", "reliquia", "ovo" };
        // ponytail: Sprites/Default multiplica a cor; flash branco de verdade pede shader alfa x cor num material-asset (ART_BIBLE §6)
        static readonly Color Flash = new Color(1f, 0.55f, 0.55f), Ghost = new Color(1f, 1f, 1f, 0.45f);

        sealed class Ent
        {
            public Transform Root, Up, Dir; public SortingGroup G;
            public SpriteRenderer Body, Deco, Eyes, Pupils, Ring, Fill, HpBg, HpFill, Arc, Line, Shadow, Spr;
            public int Type = -1, Id = -1; public Color BodyC = Color.white;
            public SpriteAnim Anim;   // null = procedural
            public Vector2 Last, Hand; public bool HasHand, HandFront = true, FlipX; public float StrikeT;
        }
        sealed class CargoV
        {
            public Transform Root, Up, Vis; public SortingGroup G;
            public SpriteRenderer Range, Outline, Body, Deco, Cracks, Seal, Star, Count, Spr;
            public CargoType Type; public bool Rescue, Painted; public float HitT, PulseT; public SpriteSheet Sheet;
        }
        sealed class DoorV { public SpriteRenderer Frame, Bars, Glow, Lock; public Door D; }
        sealed class Part { public SpriteRenderer R; public Vector2 P, V; public float Age, Life, S0, S1; public Color C; public bool Live; }
        sealed class Floater { public Text T; public Vector3 W; public float Age; public bool Live; }

        readonly Camera _cam;
        readonly RectTransform _labels;
        readonly Transform _root, _roomRoot;
        readonly List<Text> _roomLabels = new List<Text>();
        readonly List<Vector3> _roomLabelPos = new List<Vector3>();
        readonly SpriteSheet _sCourier, _sCourierFim, _sArco, _sBau, _sBauAberto, _sCliente;
        readonly float _plateS;   // y de tela do centro das placas das portas
        Run _run; int _roomKey = -1;
        Vector3 _camBase; float _shakeT, _shakeDur = 1f, _shakeA, _time;

        readonly Ent _courier; readonly SpriteRenderer _plantRing; float _courierHit, _throwT = 99f; Vector2 _lastCourier; V2 _throwDir = V2.Up;
        readonly CargoV[] _cargos = new CargoV[2];
        readonly Ent _pet; float _petHit;
        SpriteRenderer _chest, _chestLock, _chestArc, _deliverRing; Sprite _chestShut, _chestOpen;
        SpriteAnim _client; float _waveT;
        readonly List<DoorV> _doors = new List<DoorV>();
        readonly List<Ent> _enemies = new List<Ent>();
        readonly Dictionary<int, float> _enemyHit = new Dictionary<int, float>();
        readonly List<Ent> _shots = new List<Ent>();
        readonly List<Part> _parts = new List<Part>();
        readonly List<Floater> _floaters = new List<Floater>();

        public WorldView(Camera cam, RectTransform labels)
        {
            _cam = cam; _labels = labels;
            _sCourier = SpriteSheet.Get("courier"); _sCourierFim = SpriteSheet.Get("courier_fim"); _sArco = SpriteSheet.Get("arco");
            _sBau = SpriteSheet.Get("bau"); _sBauAberto = SpriteSheet.Get("bau_aberto"); _sCliente = SpriteSheet.Get("cliente");
            // placa acima do que for mais alto: o tampo da parede de cima ou o arco da porta
            _plateS = Mathf.Max(Tilt * (Balance.RoomH + TopWall), _sArco != null ? Tilt * Balance.RoomH + ArchTop : 0f) + PlateGap + PlateH / 2f;
            _root = new GameObject("Mundo").transform;
            _root.localScale = new Vector3(1f, Tilt, 1f);
            _roomRoot = new GameObject("Sala").transform;
            _roomRoot.SetParent(_root, false);
            _courier = NewEnt("Courier", OCourier);
            PaintCourier(_courier);
            UseSprite(_courier, _sCourier, Balance.CourierR);
            _plantRing = Art.NewSprite(_courier.Root, "Pousado", Art.DashedRing(), Art.A(Art.Ink, 0.45f), ORing, Vector2.zero, Vector2.one * 1.2f);
            for (int i = 0; i < _cargos.Length; i++) _cargos[i] = NewCargo(i);
            _pet = NewEnt("Filhote", OPet);
            PaintPet(_pet);
            _root.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ camera

        /// <summary>Encaixa a sala inteira (com paredes e placas, ja projetada) no retangulo de tela `px` (area segura menos a HUD).</summary>
        public void Fit(UnityEngine.Rect px)
        {
            float b = Tilt * ViewB, t = _plateS + PlateH / 2f + ViewPad;
            float ppu = Mathf.Max(1f, Mathf.Min(px.width / (ViewR - ViewL), px.height / (t - b)));
            _cam.orthographicSize = Screen.height / (2f * ppu);
            Vector2 c = px.center, sc = new Vector2(Screen.width, Screen.height) * 0.5f;
            _camBase = new Vector3((ViewL + ViewR) * 0.5f - (c.x - sc.x) / ppu, (b + t) * 0.5f - (c.y - sc.y) / ppu, -10f);
            _cam.transform.position = _camBase;
        }

        public void Shake(float amp, float dur)
        {
            if (amp < _shakeA * (_shakeT / _shakeDur)) return;
            _shakeA = amp; _shakeT = _shakeDur = dur;
        }

        /// <summary>Ordem de um SortingGroup pelo pe (y da Sim): mais embaixo na tela desenha na frente. Passo de 4 por 2 cm deixa a carga
        /// nas maos em +-1 e +-2 do portador sem encostar no vizinho.</summary>
        static int Depth(float y) => OActor + Mathf.Clamp((int)((Balance.RoomH + 2f - y) * 50f), 0, 900) * 4;

        // ------------------------------------------------------------------ quadro a quadro

        public void Refresh(Run run, float dt)
        {
            _time += dt;
            bool show = run != null;
            if (_root.gameObject.activeSelf != show) _root.gameObject.SetActive(show);
            if (!show) { _run = null; HideLabels(); return; }
            int key = run.Layer * 100 + run.Room.Id;
            if (run != _run || key != _roomKey) BuildRoom(run, key);

            _shakeT = Mathf.Max(0f, _shakeT - dt);
            Vector2 sh = _shakeT > 0f ? Random.insideUnitCircle * (_shakeA * _shakeT / _shakeDur) : Vector2.zero;
            _cam.transform.position = _camBase + (Vector3)sh;

            DrawCourier(run, dt);
            DrawEnemies(run, dt);   // antes das cargas: a carga roubada vai na ancora do ladrao deste quadro
            DrawCargos(run, dt);
            DrawPet(run, dt);
            DrawShots(run);
            DrawRoomState(run, dt);
            TickParts(dt);
            TickFloaters(dt);
            for (int i = 0; i < _roomLabels.Count; i++) _roomLabels[i].rectTransform.position = _cam.WorldToScreenPoint(_roomLabelPos[i]);
        }

        void DrawCourier(Run run, float dt)
        {
            Courier c = run.Courier;
            Vector2 p = V(c.Pos);
            bool moved = (p - _lastCourier).sqrMagnitude > 1e-6f;
            _lastCourier = p;
            _courier.Root.localPosition = p;
            _courier.G.sortingOrder = Depth(c.Pos.Y);
            _courierHit = Mathf.Max(0f, _courierHit - dt);
            // dano: flash e pisca por 0,6 s (ART_BIBLE §9)
            bool blink = _courierHit > 0f && Mathf.Repeat(_courierHit * 10f, 1f) < 0.5f;
            _plantRing.enabled = c.State == CourierState.Planted;
            _plantRing.transform.localRotation = Quaternion.Euler(0f, 0f, _time * 60f);
            if (c.State == CourierState.Dashing) Burst(p, Art.A(Art.Courier, 0.4f), 1, 0f, 0.75f, 0.18f);   // fantasmas azuis do dash
            if (_courier.Anim != null)
            {
                AnimCourier(run, c, dt);
                _courier.Spr.color = _courierHit > 0.52f ? Flash : blink ? Ghost : Color.white;
                return;
            }
            _courier.Dir.localRotation = Quaternion.Euler(0f, 0f, Angle(c.State == CourierState.Dashing ? c.DashDir : c.Facing));
            float d = 2f * Balance.CourierR;
            Vector2 s = Vector2.one;
            if (c.State == CourierState.Dashing) s = new Vector2(0.75f, 1.35f);
            else if (moved && c.State != CourierState.Dead)
            {
                float k = Mathf.Sin(_time * 18f);
                s = new Vector2(1f - 0.06f * k, 1f + 0.08f * k);   // squash and stretch andando
            }
            _courier.Body.transform.localScale = new Vector3(d * s.x, d * s.y, 1f);
            Color body = c.State == CourierState.Dead ? Art.Dim : Art.Courier;
            _courier.Body.color = _courierHit > 0.52f ? Color.white : blink ? Art.A(body, 0.45f) : body;
        }

        /// <summary>Estado do Core -> clipe do courier: Dying/Cheering (courier_fim) no fim; Dash no dash (esticado nos 0,18 s); Hit ao
        /// levar dano sem carga (com carga so pisca, para a carga nao pular das maos); Throw parado atirando, no compasso do tiro;
        /// CarryRun/Run andando; CarryIdle/Idle parado (CarryIdle tambem antes de pousar e na entrada da sala).</summary>
        void AnimCourier(Run run, Courier c, float dt)
        {
            Ent e = _courier; SpriteAnim a = e.Anim; SpriteSheet s = _sCourier;
            a.T += dt; _throwT += dt;
            bool held = false;
            foreach (Cargo g in run.Cargos) if (g.State == CargoState.Held) held = true;
            bool dead = c.State == CourierState.Dead, won = run.Outcome == Outcome.Delivered;
            bool moving = !run.AwaitingMove && c.IdleT <= 0f && (c.State == CourierState.Carrying || c.State == CourierState.Walking);
            V2 face = c.State == CourierState.Dashing ? c.DashDir : c.Facing;
            int clip, frame = -1;
            if ((dead || won) && _sCourierFim != null) { s = _sCourierFim; clip = s.Pick(dead ? "Dying" : "Cheering"); }
            else if (dead) clip = s.Pick("Hit");
            else if (c.State == CourierState.Dashing)
            {
                clip = s.Pick("Dash", "Run");
                frame = (int)((1f - c.DashT / Balance.DashTime) * s.Frames(clip));   // o clipe inteiro nos 0,18 s
            }
            else if (!held && _courierHit > 0.6f - HitTime && s.Clip("Hit") >= 0)
            {
                clip = s.Clip("Hit");
                frame = (int)((0.6f - _courierHit) / HitTime * s.Frames(clip));      // o clipe inteiro em HitTime
            }
            else if (c.State == CourierState.Planted && s.Clip("Throw") >= 0 && (_throwT < Balance.ShotCooldown * (1f - ThrowRelease) || InRange(run)))
            {
                // o quadro de soltar cai exatamente no evento Shot (o _throwT zera nele); entre tiros, o fim e a preparacao do proximo
                clip = s.Clip("Throw");
                int n = s.Frames(clip);
                frame = ((int)(ThrowRelease * n) + (int)(_throwT / Balance.ShotCooldown * n)) % n;
                face = _throwDir;
            }
            else if (moving) clip = held ? s.Pick("CarryRun", "Run") : s.Pick("Run");
            else clip = held ? s.Pick("CarryIdle") : s.Pick("Idle");
            a.Play(s, clip);
            a.Face(face.X, face.Y);
            a.Show(frame, won);   // Cheering em laco ate a cena da entrega
            e.HasHand = a.Hands(out e.Hand, out e.HandFront);
        }

        static bool InRange(Run run)
        {
            foreach (Enemy e in run.Enemies) if (e.Alive && V2.Dist(e.Pos, run.Courier.Pos) <= Balance.ShotRange) return true;
            return false;
        }

        void DrawCargos(Run run, float dt)
        {
            Courier c = run.Courier;
            int held = 0;
            for (int i = 0; i < _cargos.Length; i++)
            {
                CargoV v = _cargos[i];
                Cargo g = i < run.Cargos.Count ? run.Cargos[i] : null;
                bool on = g != null && g.State != CargoState.Gone;
                if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
                if (!on) continue;
                if (!v.Painted || v.Type != g.Type || v.Rescue != g.Rescue) PaintCargo(v, g.Type, g.Rescue);
                bool ground = g.State == CargoState.Ground, spr = v.Sheet != null;
                // nas maos (courier ou ladrao) pela ancora `hands`; no chao pelo pe, ordenada por y como os atores
                int order;
                Vector2 pos;
                if (g.State == CargoState.Held) pos = Carried(_courier, V(c.Pos), held++, 0.3f, out order);
                else if (g.State == CargoState.Stolen)
                {
                    Ent t = null;
                    for (int k = 0; k < run.Enemies.Count && k < _enemies.Count; k++) if (run.Enemies[k].Carry == g) t = _enemies[k];
                    if (t != null) pos = Carried(t, V(g.Pos), 0, 0.35f, out order);
                    else { pos = V(g.Pos) + new Vector2(0f, 0.35f / Tilt); order = Depth(g.Pos.Y) + 1; }
                }
                else { pos = V(g.Pos); order = Depth(g.Pos.Y); }
                v.Root.localPosition = pos;
                v.G.sortingOrder = order;
                v.HitT = Mathf.Max(0f, v.HitT - dt);
                float pop = v.HitT > 0.18f ? 1f + (v.HitT - 0.18f) / 0.12f * 0.3f : 1f;   // rachou: pop 1,3 -> 1
                // procedural: a forma inteira escala; com sprite (1:1), so o contorno e as rachaduras, centrados no objeto
                v.Vis.localScale = Vector3.one * ((spr ? CargoBox : ground ? 0.62f : 0.5f) * pop);
                v.Vis.localPosition = spr && ground ? (Vector3)v.Sheet.Center : Vector3.zero;
                if (spr)
                {
                    v.Spr.transform.localScale = Vector3.one * pop;
                    v.Spr.transform.localPosition = ground ? Vector3.zero : (Vector3)(-v.Sheet.Center);   // nas maos: centro na ancora
                }
                // ovo nos ultimos 5 s antes de chocar: balanca +-8 graus a 4 Hz
                float wob = g.Type == CargoType.Egg && run.EggLeft > 0f && run.EggLeft < 5f ? Mathf.Sin(_time * 25f) * 8f : 0f;
                v.Vis.localRotation = v.Spr.transform.localRotation = Quaternion.Euler(0f, 0f, wob);
                Color baseC = spr ? Color.white : Art.CargoColor(g.Type), tint = v.HitT > 0.24f ? spr ? Flash : Color.white : baseC;
                if (g.Type == CargoType.Relic && g.Cracks == Balance.RelicBreak - 1)   // a proxima quebra: pulsa branco e vermelho
                    tint = Color.Lerp(Color.white, Art.Bad, 0.5f + 0.5f * Mathf.Sin(_time * 10f));
                if (spr) v.Spr.color = tint; else v.Body.color = tint;
                v.Cracks.enabled = g.Cracks > 0;
                if (g.Cracks > 0) v.Cracks.sprite = Art.Cracks(g.Cracks);

                // no chao: contorno limao piscando a 2 Hz quando longe (> AloneDist); vermelho pulsando dentro de um telegraph
                float far = V2.Dist(g.Pos, c.Pos);
                bool alone = ground && far > Balance.AloneDist, danger = ground && InTelegraph(run, g.Pos);
                float a = !ground ? 0f : danger ? 0.6f + 0.4f * Mathf.Sin(_time * 18f) : alone ? (Mathf.Repeat(_time * Blink, 1f) < 0.5f ? 1f : 0.15f) : 0.9f;
                v.Outline.color = Art.A(danger ? Art.Bad : Art.Cargo, a);
                // anel de contagem nos ultimos 3 s antes de nascer o ladrao: do limao ao roxo
                float left = Balance.AloneTime - g.AloneT;
                bool count = ground && alone && left < 3f;
                v.Count.enabled = count;
                if (count)
                {
                    float k = 1f - left / 3f;
                    v.Count.sprite = Art.Arc(k);
                    v.Count.color = Color.Lerp(Art.Cargo, Art.Role(EnemyType.Thief), k);
                }
                // anel de pegar (PickupRange): expande ao pousar (0,35 s) e fica fraco enquanto a carga esta no chao
                v.PulseT = Mathf.Max(0f, v.PulseT - dt);
                v.Range.enabled = ground && c.State != CourierState.Dead;
                float pk = v.PulseT > 0f ? 1f - v.PulseT / PulseTime : 1f;
                v.Range.transform.localScale = Vector3.one * (2f * Balance.PickupRange * pk);
                v.Range.color = Art.A(Art.Cargo, v.PulseT > 0f ? 0.9f * (1f - pk) + 0.2f : far <= Balance.PickupRange ? 0.4f : 0.18f);
            }
        }

        /// <summary>Carga nas maos de `who` (no pe `at`): na ancora `hands` do quadro, logo acima do portador (frente) ou logo abaixo
        /// (costas), a k-esima empilhada 0,8 x a altura dela acima; sem ancora, o lugar procedural de sempre (`procY` m de tela acima do
        /// pe), na frente. Devolve a posicao no espaco do _root.</summary>
        static Vector2 Carried(Ent who, Vector2 at, int k, float procY, out int order)
        {
            int o = who.G.sortingOrder;
            if (who.Anim != null && who.HasHand)
            {
                order = o + (who.HandFront ? 1 + k : -1 - k);
                return at + new Vector2(who.Hand.x, (who.Hand.y + 0.8f * CargoBox * k) / Tilt);
            }
            order = o + 1 + k;
            return at + new Vector2(0f, (procY + 0.4f * k) / Tilt);
        }

        static bool InTelegraph(Run run, V2 p)
        {
            foreach (Enemy e in run.Enemies)
                if (e.Alive && e.State == EnemyState.Telegraph && e.Type != EnemyType.Guard && V2.Dist(e.Pos, p) <= e.Def.Reach) return true;
            return false;
        }

        void DrawPet(Run run, float dt)
        {
            Pet p = run.Pet;
            bool on = p != null && p.Hp > 0;
            if (_pet.Root.gameObject.activeSelf != on) _pet.Root.gameObject.SetActive(on);
            if (!on) return;
            float hop = Mathf.Abs(Mathf.Sin(_time * 9f));
            _pet.Root.localPosition = V(p.Pos) + new Vector2(0f, 0.08f * hop / Tilt);
            _pet.Up.localScale = UpScale(1f + 0.08f * (1f - hop), 1f - 0.06f * (1f - hop));   // pulinhos com squash
            _pet.G.sortingOrder = Depth(p.Pos.Y);
            _petHit = Mathf.Max(0f, _petHit - dt);
            _pet.Body.color = _petHit > 0f ? Color.Lerp(Art.Pet, Color.white, _petHit / 0.3f) : Art.Pet;
            float f = (float)p.Hp / Balance.PetHp;
            _pet.HpFill.transform.localScale = new Vector3(0.5f * f, 0.08f, 1f);
            _pet.HpFill.transform.localPosition = new Vector3(-0.25f + 0.25f * f, 0.45f, 0f);
        }

        void DrawEnemies(Run run, float dt)
        {
            List<Enemy> list = run.Enemies;
            while (_enemies.Count < list.Count) _enemies.Add(NewEnt("Inimigo", OEnemy));
            Vector2 cp = V(run.Courier.Pos);
            for (int i = 0; i < _enemies.Count; i++)
            {
                Ent v = _enemies[i];
                Enemy e = i < list.Count ? list[i] : null;
                bool on = e != null && e.Alive;
                if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
                if (!on) continue;
                if (v.Type != (int)e.Type) PaintEnemy(v, e.Type);
                Vector2 p = V(e.Pos);
                if (v.Id != e.Id) { v.Id = e.Id; v.Last = p; v.StrikeT = 0f; if (v.Anim != null) v.Anim.Clip = -1; }   // lista encolheu: outro inimigo
                Vector2 vel = p - v.Last;
                v.Last = p;
                v.StrikeT = Mathf.Max(0f, v.StrikeT - dt);
                v.G.sortingOrder = Depth(e.Pos.Y);
                EnemyDef d = e.Def;
                float hit = _enemyHit.TryGetValue(e.Id, out float h) ? h : 0f;
                if (hit > 0f) _enemyHit[e.Id] = Mathf.Max(0f, hit - dt);
                bool tele = e.State == EnemyState.Telegraph, warn = tele && e.Tele < 0.15f && Mathf.Repeat(_time * 12f, 1f) < 0.5f;
                if (v.Anim != null)
                {
                    v.Root.localPosition = p;
                    AnimEnemy(v, e, p, vel, cp, dt);
                    // flash no golpe recebido; tinta vermelha a 30% piscando nos ultimos 0,15 s do telegraph
                    v.Spr.color = hit > 0.06f ? Flash : warn ? Color.Lerp(Color.white, Art.Bad, 0.3f) : Color.white;
                }
                else
                {
                    float bob = e.Type == EnemyType.Bat ? 0.06f * Mathf.Sin(_time * 19f + e.Phase * 6f) : 0f;   // morcego flutua a 3 Hz
                    v.Root.localPosition = p + new Vector2(0f, bob / Tilt);
                    Vector2 look = (cp - p).sqrMagnitude > 1e-4f ? (cp - p).normalized : Vector2.down;
                    v.Eyes.transform.localPosition = look * (0.12f * d.Radius) + new Vector2(0f, 0.15f * d.Radius);
                    v.Pupils.transform.localPosition = v.Eyes.transform.localPosition + (Vector3)(look * 0.06f * d.Radius);
                    if (e.Type == EnemyType.Bat)   // asa por escala X de 1,0 a 0,65 a 10 Hz
                        v.Body.transform.localScale = new Vector3(d.Radius * 4.8f * (0.65f + 0.35f * Mathf.Abs(Mathf.Sin(_time * 31.4f + e.Phase * 6f))), d.Radius * 4.8f, 1f);
                    v.Body.color = hit > 0.06f ? Color.white : warn ? Color.Lerp(v.BodyC, Art.Bad, 0.3f) : v.BodyC;
                }
                // telegraph (ART_BIBLE §7): circulo de raio Reach no chao; enchimento a 25% crescendo com TeleProgress; borda a 90%, branca no ultimo 0,1 s
                v.Ring.enabled = v.Fill.enabled = tele;
                if (tele)
                {
                    float r2 = 2f * d.Reach;
                    v.Ring.transform.localScale = Vector3.one * r2;
                    v.Fill.transform.localScale = Vector3.one * (r2 * Mathf.Max(0.05f, e.TeleProgress));
                    v.Ring.color = e.Tele < 0.1f ? Color.white : Art.A(Art.Tele, 0.9f);
                }
                // vida: so depois do 1o golpe (sempre no Guarda/Ogro, que trancam a porta); com sprite, acima da cabeca
                bool bar = e.Hp < d.Hp || d.Locks;
                v.HpBg.enabled = v.HpFill.enabled = bar;
                if (bar)
                {
                    float w = Mathf.Max(0.6f, 2.2f * d.Radius), f = (float)e.Hp / d.Hp, y = v.Anim != null ? Lift * EnemyH[(int)e.Type] + 0.25f : d.Radius + 0.22f;
                    v.HpBg.transform.localPosition = new Vector3(0f, y, 0f);
                    v.HpBg.transform.localScale = new Vector3(w + 0.06f, 0.14f, 1f);
                    v.HpFill.transform.localPosition = new Vector3(-w * 0.5f + w * f * 0.5f, y, 0f);
                    v.HpFill.transform.localScale = new Vector3(w * f, 0.08f, 1f);
                }
                // ladrao: anel roxo pegando a carga (0,5 s), saindo pela borda (1 s), e linha roxa de intencao ate a carga no chao
                float arc = e.State == EnemyState.Grab ? e.GrabT / Balance.ThiefGrab : e.Carry != null && e.ExitT > 0f ? e.ExitT / Balance.ThiefExit : 0f;
                v.Arc.enabled = arc > 0f;
                if (arc > 0f) { v.Arc.sprite = Art.Arc(arc); v.Arc.transform.localScale = Vector3.one * (2f * d.Radius + 0.5f); }
                Cargo target = e.Type == EnemyType.Thief && e.Carry == null && (e.State == EnemyState.Chase || e.State == EnemyState.Grab) ? NearestGround(run, e.Pos) : null;
                v.Line.enabled = target != null;
                if (target != null)
                {
                    Vector2 to = V(target.Pos) - p;
                    v.Line.transform.localPosition = to * 0.5f;
                    v.Line.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
                    v.Line.transform.localScale = new Vector3(to.magnitude, 0.08f, 1f);
                }
            }
        }

        /// <summary>Estado do Core -> clipe do inimigo com folha. Morcego: estatico, asa por escala X 1,0 -> 0,65 a 10 Hz, flutua a 1 m
        /// (+-0,06 a 3 Hz) sobre a sombra, flipX pelo movimento. Ladrao: Run correndo para a carga, Lurk rondando, Grab pegando, CarryRun
        /// fugindo com ela. Guarda/Bruto/Ogro: Idle; Walk perseguindo; golpe com os quadros [0, impacto) esticados pelo TeleProgress e
        /// [impacto, fim) em 0,15 s depois do Strike (ART_BIBLE §7).</summary>
        void AnimEnemy(Ent v, Enemy e, Vector2 p, Vector2 vel, Vector2 cp, float dt)
        {
            SpriteAnim a = v.Anim; SpriteSheet s = a.Sheet;
            a.T += dt;
            Vector2 face = vel;
            int clip, frame = -1;
            switch (e.Type)
            {
                case EnemyType.Bat:
                    clip = s.Pick("Static");
                    if (Mathf.Abs(vel.x) > 1e-4f) v.FlipX = vel.x > 0f;
                    float bob = 0.06f * Mathf.Sin(_time * 19f + e.Phase * 6f);
                    v.Up.localPosition = new Vector3(0f, (Lift * BatZ + bob) / Tilt, 0f);
                    v.Spr.transform.localScale = new Vector3(0.825f + 0.175f * Mathf.Sin(_time * 62.83f + e.Phase * 6f), 1f, 1f);
                    break;
                case EnemyType.Thief:
                    clip = e.Carry != null ? s.Pick("CarryRun", "Run") : e.State == EnemyState.Grab ? s.Pick("Grab")
                        : e.State == EnemyState.Chase ? s.Pick("Run") : s.Pick("Lurk");
                    break;
                default:
                    bool tele = e.State == EnemyState.Telegraph;
                    clip = tele || v.StrikeT > 0f ? s.Clip(AttackClip[(int)e.Type]) : -1;
                    if (clip >= 0)
                    {
                        int n = s.Frames(clip), k = Mathf.Max(1, Mathf.Min(n - 1, Mathf.RoundToInt(ImpactFrac * n)));
                        frame = tele ? Mathf.Min((int)(e.TeleProgress * k), k - 1) : k + (int)((1f - v.StrikeT / StrikeHold) * (n - k));
                        face = cp - p;
                    }
                    else if (e.State == EnemyState.Chase) clip = s.Pick("Walk", "Run");
                    else { clip = s.Pick("Idle"); face = cp - p; }   // parado olha para o courier
                    break;
            }
            a.Play(s, clip);
            a.Face(face.x, face.y);
            a.Show(frame);
            if (e.Type == EnemyType.Bat) v.Spr.flipX = v.FlipX;
            v.HasHand = a.Hands(out v.Hand, out v.HandFront);
        }

        static Cargo NearestGround(Run run, V2 from)
        {
            Cargo best = null; float bd = float.MaxValue;
            foreach (Cargo g in run.Cargos)
            {
                if (g.State != CargoState.Ground) continue;
                float dd = V2.Dist(from, g.Pos);
                if (dd < bd) { bd = dd; best = g; }
            }
            return best;
        }

        void DrawShots(Run run)
        {
            List<Shot> list = run.Shots;
            while (_shots.Count < list.Count)
            {
                var s = new Ent { Root = new GameObject("Tiro").transform };
                s.Root.SetParent(_root, false);
                // projetil procedural (ART_BIBLE §9): nucleo branco com halo azul de 0,25 m e rastro de 0,4 m
                s.Deco = Art.NewSprite(s.Root, "Rastro", Art.Glow(), Art.A(Art.Shot, 0.7f), OShot, new Vector2(0f, -0.2f), new Vector2(0.22f, 0.5f));
                s.Ring = Art.NewSprite(s.Root, "Halo", Art.Glow(), Art.Shot, OShot + 1, Vector2.zero, Vector2.one * 0.4f);
                s.Body = Art.NewSprite(s.Root, "Nucleo", Art.Disc(), Color.white, OShot + 2, Vector2.zero, Vector2.one * 0.14f);
                _shots.Add(s);
            }
            float lift = _courier.Anim != null ? Lift * ShotZ / Tilt : 0f;   // com sprite, voa na altura do peito (0,9 m)
            for (int i = 0; i < _shots.Count; i++)
            {
                Ent v = _shots[i];
                bool on = i < list.Count && list[i].Target != null;
                if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
                if (!on) continue;
                Shot s = list[i];
                v.Root.localPosition = V(s.Pos) + new Vector2(0f, lift);
                v.Root.localRotation = Quaternion.Euler(0f, 0f, Angle(s.Target.Pos - s.Pos));
            }
        }

        void DrawRoomState(Run run, float dt)
        {
            foreach (DoorV dv in _doors)
            {
                Door d = dv.D;
                dv.Bars.enabled = !d.Open;
                dv.Lock.enabled = d.Locked;
                dv.Glow.enabled = d.Open;
                // trancada pelo guarda = vermelho-escuro; aberta = positivo; exige a carga nas maos = limao (a carga e a chave)
                dv.Frame.color = d.Locked ? Art.Role(EnemyType.Guard) : d.Open ? Art.Good : Art.Cargo;
                if (d.Open) dv.Glow.color = Art.A(Art.Good, 0.3f + 0.2f * Mathf.Sin(_time * 5f));
            }
            if (_chest != null && run.Chest != null)
            {
                Chest ch = run.Chest;
                if (_chestShut != null)   // sprite: troca para o bau_aberto (sem ele, o fechado esmaecido)
                {
                    _chest.sprite = ch.Opened && _chestOpen != null ? _chestOpen : _chestShut;
                    _chest.color = ch.Opened && _chestOpen == null ? Art.A(Color.white, 0.55f) : Color.white;
                }
                else _chest.color = ch.Opened ? Art.A(Art.Chest, 0.45f) : Art.Chest;
                _chestLock.enabled = !ch.Opened && _chestShut == null;
                bool prog = !ch.Opened && ch.Progress > 0f;
                _chestArc.enabled = prog;
                if (prog) _chestArc.sprite = Art.Arc(ch.Progress / Balance.ChestTime);
            }
            if (_deliverRing != null)
            {
                _deliverRing.color = Art.A(Art.Good, run.CarryOk ? 0.6f + 0.3f * Mathf.Sin(_time * 4f) : 0.3f);
                _deliverRing.transform.localRotation = Quaternion.Euler(0f, 0f, -_time * 25f);
            }
            if (_client != null)
            {
                // cliente: acena quando o courier entra na D1; na entrega reage pela integridade (ART_BIBLE §9)
                SpriteSheet s = _client.Sheet;
                _client.T += dt; _waveT -= dt;
                int clip = run.Outcome == Outcome.Delivered ? s.Pick(Reaction(run)) : _waveT > 0f ? s.Pick("Waving") : s.Pick("Idle");
                _client.Play(s, clip);
                _client.Show(-1, true);   // aceno e reacao em laco (o meta so poe laco em idle/walk/run/lurk)
            }
        }

        /// <summary>0 rachaduras Cheering, 1 Thankful, 2+ Shrug; ovo chocado Thankful.</summary>
        static string Reaction(Run run) => run.Hatched ? "Thankful" : run.Main.Cracks <= 0 ? "Cheering" : run.Main.Cracks == 1 ? "Thankful" : "Shrug";

        // ------------------------------------------------------------------ sala (estatico, refeito a cada sala)

        void BuildRoom(Run run, int key)
        {
            _run = run; _roomKey = key;
            for (int i = _roomRoot.childCount - 1; i >= 0; i--) Object.Destroy(_roomRoot.GetChild(i).gameObject);
            foreach (Text t in _roomLabels) Object.Destroy(t.gameObject);
            _roomLabels.Clear(); _roomLabelPos.Clear(); _doors.Clear(); _enemyHit.Clear();
            _chest = _chestLock = _chestArc = _deliverRing = null; _chestShut = _chestOpen = null; _client = null;
            foreach (CargoV v in _cargos) v.PulseT = 0f;
            Transform r = _roomRoot;
            Transform up = Upright(r, "EmPe", Vector2.zero);   // placas e arcos (sem SortingGroup: ficam atras dos atores)
            RoomDef room = run.Room;
            float W = Balance.RoomW, H = Balance.RoomH;

            // cenario so em azul-violeta-cinza (ART_BIBLE §3): piso em lajes, parede com tampo e face, obstaculo com tampo e aresta
            SpriteRenderer floor = Art.NewSprite(r, "Piso", Art.Floor(), Color.white, OFloor, new Vector2(W / 2f, H / 2f), Vector2.one);
            floor.drawMode = SpriteDrawMode.Tiled; floor.size = new Vector2(W, H);
            Rect(r, "ParedeEsq", -WallT / 2f, H / 2f, WallT, H + 2f * WallT, Art.WallTop, OWall);
            Rect(r, "ParedeDir", W + WallT / 2f, H / 2f, WallT, H + 2f * WallT, Art.WallTop, OWall);
            Rect(r, "ParedeBaixo", W / 2f, -WallT / 2f, W + 2f * WallT, WallT, Art.WallTop, OWall);
            Rect(r, "ParedeCima", W / 2f, H + TopWall / 2f, W + 2f * WallT, TopWall, Art.Wall, OWall);
            Rect(r, "TampoCima", W / 2f, H + TopWall - 0.06f, W + 2f * WallT, 0.12f, Art.WallTop, OWall + 1);
            Rect(r, "Rodape", W / 2f, H + 0.04f, W, 0.08f, Art.Skirt, OWall + 1);
            Rect(r, "Entrada", Balance.Entrance.X, -WallT / 2f, 2f * Balance.DoorHalf, WallT, Art.DoorIn, OWall + 1);
            foreach (Core.Rect o in room.Obstacles)
            {
                Rect(r, "Sombra", o.C.X + 0.1f, o.C.Y - 0.1f, 2f * o.H.X, 2f * o.H.Y, Art.A(Art.Dark, 0.35f), OShadow);
                Rect(r, "Face", o.C.X, o.C.Y, 2f * o.H.X, 2f * o.H.Y, Art.Block, OBlock);
                float top = Mathf.Max(0.1f, 2f * o.H.Y - 0.3f);   // tampo em cima, face de 0,3 m embaixo
                Rect(r, "Tampo", o.C.X, o.C.Y + o.H.Y - top / 2f, 2f * o.H.X, top, Art.BlockTop, OBlock + 1);
                Rect(r, "Aresta", o.C.X, o.C.Y + o.H.Y - 0.03f, 2f * o.H.X, 0.06f, Art.BlockEdge, OBlock + 2);
            }
            foreach (Door d in run.Doors) BuildDoor(r, up, d);
            if (run.Chest != null) PaintChest(r, run.Chest.Pos);
            if (room.Delivery) PaintClient(r);
            _root.gameObject.SetActive(true);
        }

        /// <summary>Porta (ART_BIBLE §8): vao de 1,6 m com grade de aco, cadeado vermelho do guarda, luz verde aberta; com o sprite `arco`,
        /// o arco de pedra por cima, a grade enche o vao dele e o batente colorido vira soleira no chao; placa de pedra acima com 3
        /// encaixes de caveira (ameaca N de 3), bau com as moedas, bandeira verde da entrega e chifres do chefe.</summary>
        void BuildDoor(Transform r, Transform up, Door d)
        {
            float x = d.Pos.X, w = 2f * Balance.DoorHalf, H = Balance.RoomH;
            bool arch = _sArco != null;
            float gh = arch ? ArchOpen / Tilt : TopWall - 0.06f, gy = arch ? H + gh / 2f : H + TopWall / 2f - 0.03f;
            var dv = new DoorV { D = d };
            dv.Glow = Art.NewSprite(r, "Luz", Art.Glow(), Art.A(Art.Good, 0.4f), OMark, new Vector2(x, H - 0.3f), new Vector2(w * 1.6f, 1.6f));
            dv.Frame = arch ? Rect(r, "Soleira", x, H - 0.3f, w + 0.16f, 0.14f, Art.Cargo, ODoor)
                : Rect(r, "Batente", x, H + TopWall / 2f, w + 0.16f, TopWall + 0.02f, Art.Cargo, ODoor);
            Rect(r, "Vao", x, gy, w, gh, Art.DoorIn, ODoor + 1);
            dv.Bars = Art.NewSprite(r, "Grade", Art.Bars(), Art.Steel, ODoor + 2, new Vector2(x, gy), new Vector2(w, gh));
            if (arch) Art.NewSprite(up, "Arco", _sArco.Frame(_sArco.Pick("Static"), 0, 0), Color.white, ODoor + 3, new Vector2(x, Tilt * H), Vector2.one);
            dv.Lock = Art.NewSprite(up, "Cadeado", Art.Lock(), Art.Role(EnemyType.Guard), ODoor + 4, new Vector2(x, Tilt * gy), Vector2.one * 0.38f);
            _doors.Add(dv);

            RoomDef to = Content.Rooms[d.Room];
            const float icon = 0.36f, gap = 0.04f;
            bool special = to.Delivery || to.Boss;
            float pw = 3 * (icon + gap) + 0.16f + (special ? icon + gap : 0f);
            Rect(up, "PlacaBorda", x, _plateS, pw + 0.08f, PlateH + 0.08f, Art.Tinta, OPlate);
            Rect(up, "Placa", x, _plateS, pw, PlateH, Art.Plate, OPlate + 1);
            float x0 = x - pw / 2f + 0.08f + icon / 2f;
            if (special)
            {
                Art.NewSprite(up, to.Delivery ? "Entrega" : "Chefe", to.Delivery ? Art.Flag() : Art.Horns(), to.Delivery ? Art.Good : Art.Bone, OPlate + 2, new Vector2(x0, _plateS), Vector2.one * icon);
                x0 += icon + gap;
            }
            for (int i = 0; i < 3; i++)
                Art.NewSprite(up, "Caveira", Art.Skull(), i < d.Threat ? Art.Bone : Art.Tinta, OPlate + 2, new Vector2(x0 + i * (icon + gap), _plateS), Vector2.one * icon);
            if (d.ChestCoins <= 0) return;
            Vector2 cp = new Vector2(x + pw / 2f + 0.3f, _plateS);   // bau ao lado da placa (o proprio sprite em 0,45 m), com as moedas em numero
            if (_sBau != null)
            {
                float k = 0.45f / BauW;
                Art.NewSprite(up, "Bau", _sBau.Frame(_sBau.Pick("Static"), 0, 0), Color.white, OPlate + 2, cp - _sBau.Center * k, Vector2.one * k);
            }
            else Art.NewSprite(up, "Bau", Art.ChestShape(), Art.Chest, OPlate + 2, cp, Vector2.one * 0.45f);
            Text t = Art.Outlined(Art.NewText(_labels, "Moedas", 30, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), 3f);
            t.rectTransform.sizeDelta = new Vector2(120f, 50f); t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = Textos.Num(d.ChestCoins); t.color = Art.Coin; t.fontStyle = FontStyle.Bold;
            _roomLabels.Add(t); _roomLabelPos.Add(up.TransformPoint(new Vector3(cp.x + 0.45f, _plateS, 0f)));
        }

        SpriteRenderer Rect(Transform parent, string name, float cx, float cy, float w, float h, Color c, int order) =>
            Art.NewSprite(parent, name, Art.Square(), c, order, new Vector2(cx, cy), new Vector2(w, h));

        // ------------------------------------------------------------------ em pe: o no que desfaz o achatamento do chao

        static Vector3 UpScale(float sx = 1f, float sy = 1f) => new Vector3(sx, sy / Tilt, 1f);

        /// <summary>No em pe em `pos` (m da sala, dentro de um pai achatado): dentro dele 1 unidade = 1 m de tela.</summary>
        static Transform Upright(Transform parent, string name, Vector2 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localScale = UpScale();
            return t;
        }

        /// <summary>O mesmo, com um SortingGroup ordenado pelo pe (ator ou prop que se mistura com os atores).</summary>
        static Transform Upright(Transform parent, string name, Vector2 pos, out SortingGroup g)
        {
            Transform t = Upright(parent, name, pos);
            g = t.gameObject.AddComponent<SortingGroup>();
            g.sortingOrder = Depth(pos.y);
            return t;
        }

        /// <summary>Sombra de contato elíptica (decalque do chao) sob um ator com sprite.</summary>
        static SpriteRenderer ContactShadow(Transform root, float r) =>
            Art.NewSprite(root, "Sombra", Art.Disc(), Art.A(Art.Dark, 0.35f), OActShadow, Vector2.zero, new Vector2(2.4f * r, 1.3f * r));

        // ------------------------------------------------------------------ Paint*: o ponto de troca por sprite real, um por entidade

        Ent NewEnt(string name, int order)
        {
            var e = new Ent { Root = new GameObject(name).transform };
            e.Root.SetParent(_root, false);
            e.Up = Upright(e.Root, "EmPe", Vector2.zero, out e.G);
            e.Dir = new GameObject("Dir").transform; e.Dir.SetParent(e.Up, false);
            // no chao (achatados pelo _root)
            e.Shadow = ContactShadow(e.Root, 1f);
            e.Ring = Art.NewSprite(e.Root, "Telegraph", Art.ThinRing(), Art.Tele, ORing, Vector2.zero, Vector2.one);
            e.Fill = Art.NewSprite(e.Root, "TelegraphCheio", Art.Disc(), Art.A(Art.Tele, 0.25f), ORing - 1, Vector2.zero, Vector2.one);
            e.Line = Art.NewSprite(e.Root, "Intencao", Art.Square(), Art.A(Art.Role(EnemyType.Thief), 0.6f), ORing, Vector2.zero, Vector2.one);
            e.Arc = Art.NewSprite(e.Root, "Arco", Art.Arc(1f), Art.Role(EnemyType.Thief), ORing + 1, Vector2.zero, Vector2.one);
            // em pe
            e.Body = Art.NewSprite(e.Dir, "Corpo", Art.Disc(), Color.white, order, Vector2.zero, Vector2.one);
            e.Deco = Art.NewSprite(e.Dir, "Enfeite", Art.Disc(), Color.white, order + 1, Vector2.zero, Vector2.one);
            e.Eyes = Art.NewSprite(e.Up, "Olhos", Art.Eyes(), Color.white, order + 2, Vector2.zero, Vector2.one);
            e.Pupils = Art.NewSprite(e.Up, "Pupilas", Art.Eyes(), Art.Tinta, order + 3, Vector2.zero, Vector2.one);
            e.HpBg = Art.NewSprite(e.Up, "VidaFundo", Art.Square(), Art.A(Art.Tinta, 0.85f), order + 4, Vector2.zero, Vector2.one);
            e.HpFill = Art.NewSprite(e.Up, "Vida", Art.Square(), Art.Bad, order + 5, Vector2.zero, Vector2.one);
            e.Spr = Art.NewSprite(e.Up, "Sprite", null, Color.white, order, Vector2.zero, Vector2.one);
            e.Ring.enabled = e.Fill.enabled = e.Line.enabled = e.HpBg.enabled = e.HpFill.enabled = e.Arc.enabled = e.Shadow.enabled = e.Spr.enabled = false;
            return e;
        }

        /// <summary>Liga a folha `s` na entidade (sprite + sombra de contato, esconde o corpo procedural) ou volta ao procedural (null).</summary>
        static void UseSprite(Ent e, SpriteSheet s, float r)
        {
            bool on = s != null;
            e.Anim = on ? new SpriteAnim(s, e.Spr) : null;
            e.Spr.enabled = e.Shadow.enabled = on;
            e.Dir.gameObject.SetActive(!on);
            e.Eyes.enabled = e.Pupils.enabled = !on;
            e.Shadow.transform.localScale = new Vector3(2.4f * r, 1.3f * r, 1f);
            e.Up.localPosition = Vector3.zero;
            e.Spr.transform.localScale = Vector3.one;
            e.Spr.flipX = false;
        }

        /// <summary>Courier (azul, so ele): corpo azul, bolsa de couro atras, nariz creme e olhos para onde anda.</summary>
        static void PaintCourier(Ent c)
        {
            float d = 2f * Balance.CourierR;
            c.BodyC = Art.Courier;
            c.Body.color = Art.Courier; c.Body.transform.localScale = Vector3.one * d;
            c.Deco.sprite = Art.Rounded(); c.Deco.color = Art.Leather; c.Deco.sortingOrder = OCourier - 1;
            c.Deco.transform.localPosition = new Vector3(0f, -0.27f, 0f); c.Deco.transform.localScale = new Vector3(0.55f, 0.4f, 1f);
            c.Eyes.transform.SetParent(c.Dir, false); c.Pupils.transform.SetParent(c.Dir, false);
            c.Eyes.transform.localPosition = new Vector3(0f, 0.12f, 0f); c.Eyes.transform.localScale = Vector3.one * 0.5f;
            c.Pupils.transform.localPosition = new Vector3(0f, 0.16f, 0f); c.Pupils.transform.localScale = Vector3.one * 0.26f;
            Art.NewSprite(c.Dir, "Nariz", Art.Triangle(), Art.Cream, OCourier + 4, new Vector2(0f, 0.42f), Vector2.one * 0.24f);
        }

        /// <summary>Inimigo por papel, cor E forma (ART_BIBLE §3/§4): Morcego = asas de membrana com corpo vinho; Ladrao = capuz roxo
        /// pontudo com rosto verde; Guarda = elmo quadrado vermelho com viseira de aco; Bruto = bloco laranja-ferrugem largo com manoplas
        /// de pedra; Ogro = disco musgo grande com chifres de osso. Com a folha do papel (Resources/Sprites/&lt;preset&gt;), o sprite.</summary>
        static void PaintEnemy(Ent v, EnemyType t)
        {
            v.Type = (int)t;
            v.Id = -1;
            v.BodyC = Art.Role(t);
            float r = EnemyDef.All[(int)t].Radius, d = 2f * r;
            UseSprite(v, SpriteSheet.Get(EnemySheet[(int)t]), r);
            v.Body.sprite = Art.Disc();
            v.Body.transform.localScale = Vector3.one * d;
            v.Deco.sprite = Art.Disc();
            v.Deco.enabled = true;
            v.Deco.sortingOrder = v.Body.sortingOrder + 1;
            v.Deco.transform.localPosition = Vector3.zero;
            v.Deco.transform.localScale = Vector3.one * d;
            v.Eyes.transform.localScale = Vector3.one * (d * 0.55f);
            v.Pupils.transform.localScale = Vector3.one * (d * 0.28f);
            v.Eyes.color = Color.white;
            switch (t)
            {
                case EnemyType.Bat:
                    v.BodyC = Art.BatWing;
                    v.Body.sprite = Art.Bat(); v.Body.transform.localScale = Vector3.one * (r * 4.8f);
                    v.Deco.color = Art.Role(t); v.Deco.transform.localScale = Vector3.one * (d * 0.9f);
                    v.Eyes.transform.localScale = Vector3.one * (d * 0.45f); v.Pupils.transform.localScale = Vector3.one * (d * 0.2f);
                    break;
                case EnemyType.Thief:
                    v.Body.sprite = Art.Hood(); v.Body.transform.localScale = Vector3.one * (d * 1.1f);
                    v.Deco.color = Art.ThiefSkin; v.Deco.transform.localPosition = new Vector3(0f, -0.08f * r, 0f); v.Deco.transform.localScale = Vector3.one * (d * 0.6f);
                    break;
                case EnemyType.Guard:
                    v.Body.sprite = Art.Rounded();
                    v.Deco.sprite = Art.Square(); v.Deco.color = Art.Steel;
                    v.Deco.transform.localPosition = new Vector3(0f, 0.15f * r, 0f); v.Deco.transform.localScale = new Vector3(d * 0.8f, d * 0.26f, 1f);
                    break;
                case EnemyType.Brute:
                    v.Body.sprite = Art.Rounded(); v.Body.transform.localScale = new Vector3(d * 1.15f, d * 0.92f, 1f);
                    v.Deco.sprite = Art.Fists(); v.Deco.color = Art.SteelDark; v.Deco.transform.localScale = Vector3.one * (d * 1.5f);
                    break;
                default:   // Ogro
                    v.Deco.sprite = Art.Horns(); v.Deco.color = Art.Bone;
                    v.Deco.sortingOrder = v.Body.sortingOrder - 1;
                    v.Deco.transform.localScale = Vector3.one * (d * 1.45f);
                    break;
            }
        }

        CargoV NewCargo(int i)
        {
            var v = new CargoV { Root = new GameObject("Carga" + i).transform };
            v.Root.SetParent(_root, false);
            // no chao: anel de pegar e contagem do ladrao
            v.Range = Art.NewSprite(v.Root, "Alcance", Art.DashedRing(), Art.A(Art.Cargo, 0.2f), ORing, Vector2.zero, Vector2.one);
            v.Count = Art.NewSprite(v.Root, "Contagem", Art.Arc(0f), Art.Cargo, ORing + 1, Vector2.zero, Vector2.one * 1.05f);
            // em pe: Vis = forma procedural (escala = tamanho da carga) e, com sprite, o contorno e as rachaduras por cima dele
            v.Up = Upright(v.Root, "EmPe", Vector2.zero, out v.G);
            v.Vis = new GameObject("Forma").transform; v.Vis.SetParent(v.Up, false);
            v.Outline = Art.NewSprite(v.Vis, "Contorno", Art.Rounded(), Art.Cargo, 0, Vector2.zero, Vector2.one * 1.22f);
            v.Body = Art.NewSprite(v.Vis, "Corpo", Art.Rounded(), Color.white, 1, Vector2.zero, Vector2.one);
            v.Spr = Art.NewSprite(v.Up, "Sprite", null, Color.white, 1, Vector2.zero, Vector2.one);
            v.Deco = Art.NewSprite(v.Vis, "Enfeite", Art.Square(), Color.white, 2, Vector2.zero, Vector2.one);
            v.Cracks = Art.NewSprite(v.Vis, "Rachaduras", Art.Cracks(1), Art.Crack, 3, Vector2.zero, Vector2.one * 0.95f);
            v.Seal = Art.NewSprite(v.Vis, "SeloFundo", Art.Disc(), Art.Tinta, 4, new Vector2(0.48f, 0.48f), Vector2.one * 0.62f);
            v.Star = Art.NewSprite(v.Seal.transform, "Selo", Art.Star(), Art.Cream, 5, Vector2.zero, Vector2.one * 0.8f);
            v.Count.enabled = v.Spr.enabled = false;
            v.Root.gameObject.SetActive(false);
            return v;
        }

        /// <summary>Carga (ART_BIBLE §3/§6): caixa de madeira com cinta, reliquia de porcelana com gema, ovo com pintas (escala 1 = tamanho
        /// dela). Resgate: selo creme numa medalha escura (o roxo e so do ladrao). Com a folha (caixa no Lote 1), o sprite; o contorno vira
        /// o retangulo arredondado atras dele (a silhueta exata pede o shader alfa x cor, ART_BIBLE §6).</summary>
        static void PaintCargo(CargoV v, CargoType t, bool rescue)
        {
            v.Painted = true; v.Type = t; v.Rescue = rescue;
            v.Sheet = SpriteSheet.Get(CargoSheet[(int)t]);
            bool spr = v.Sheet != null;
            v.Body.sprite = Art.CargoSprite(t);
            v.Outline.sprite = spr ? Art.Rounded() : v.Body.sprite;
            v.Spr.enabled = spr;
            v.Body.enabled = v.Deco.enabled = !spr;
            if (spr) v.Spr.sprite = v.Sheet.Frame(v.Sheet.Pick("Static"), 0, 0);
            switch (t)
            {
                case CargoType.Box:
                    v.Deco.sprite = Art.Square(); v.Deco.color = Art.BoxStrap;
                    v.Deco.transform.localPosition = Vector3.zero; v.Deco.transform.localScale = new Vector3(0.18f, 0.92f, 1f);
                    break;
                case CargoType.Relic:
                    v.Deco.sprite = Art.Diamond(); v.Deco.color = Art.Gem;
                    v.Deco.transform.localPosition = new Vector3(0f, 0.08f, 0f); v.Deco.transform.localScale = Vector3.one * 0.32f;
                    break;
                default:
                    v.Deco.sprite = Art.Disc(); v.Deco.color = Art.EggSpot;
                    v.Deco.transform.localPosition = new Vector3(0.14f, 0.12f, 0f); v.Deco.transform.localScale = Vector3.one * 0.22f;
                    break;
            }
            v.Seal.enabled = v.Star.enabled = rescue;
        }

        /// <summary>Filhote: coral (continua as pintas do ovo), bico dourado e a vida (2) numa barrinha. Procedural ate o Lote 2.</summary>
        static void PaintPet(Ent p)
        {
            float d = 2f * Balance.PetR * 1.15f;
            p.BodyC = Art.Pet;
            p.Body.color = Art.Pet; p.Body.transform.localScale = Vector3.one * d;
            p.Deco.sprite = Art.Triangle(); p.Deco.color = Art.Gold;
            p.Deco.transform.localPosition = new Vector3(0f, -0.02f, 0f); p.Deco.transform.localScale = Vector3.one * 0.18f;
            p.Deco.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            p.Eyes.transform.localPosition = new Vector3(0f, 0.1f, 0f); p.Eyes.transform.localScale = Vector3.one * 0.32f; p.Eyes.color = Art.Tinta;
            p.Pupils.enabled = false;
            p.HpBg.enabled = p.HpFill.enabled = true;
            p.HpBg.transform.localPosition = new Vector3(0f, 0.45f, 0f); p.HpBg.transform.localScale = new Vector3(0.56f, 0.14f, 1f);
            p.HpFill.color = Art.Good;
            p.Root.gameObject.SetActive(false);
        }

        /// <summary>Bau: sprite `bau` (troca para `bau_aberto` ao abrir) com sombra, ou o procedural; anel de progresso no chao.</summary>
        void PaintChest(Transform r, V2 at)
        {
            Vector2 p = V(at);
            Transform up = Upright(r, "Bau", p, out _);
            if (_sBau != null)
            {
                _chestShut = _sBau.Frame(_sBau.Pick("Static"), 0, 0);
                if (_sBauAberto != null) _chestOpen = _sBauAberto.Frame(_sBauAberto.Pick("Static"), 0, 0);
                ContactShadow(r, 0.4f).transform.localPosition = p;
            }
            _chest = Art.NewSprite(up, "Bau", _chestShut != null ? _chestShut : Art.ChestShape(), _chestShut != null ? Color.white : Art.Chest, OChest, Vector2.zero, Vector2.one * (_chestShut != null ? 1f : 0.8f));
            _chestLock = Art.NewSprite(up, "Fecho", Art.Square(), Art.Gold, OChest + 1, new Vector2(0f, 0.1f), Vector2.one * 0.14f);
            _chestArc = Art.NewSprite(r, "Abrindo", Art.Arc(0f), Art.Coin, ORing, p, Vector2.one * 1.3f);
            _chestArc.enabled = false;
        }

        /// <summary>Cliente da entrega (D1): sprite `cliente` (Idle, Waving ao entrar, reacao na entrega) ou o procedural verde com chapeu
        /// dourado; num tapete de pedra, com o anel tracejado verde da area de entrega.</summary>
        void PaintClient(Transform r)
        {
            Vector2 p = V(Balance.Client);
            Art.NewSprite(r, "Tapete", Art.Rounded(), Art.Plate, OMark, p + new Vector2(0f, 0.35f), new Vector2(2.6f, 1.3f));
            _deliverRing = Art.NewSprite(r, "AreaEntrega", Art.DashedRing(), Art.Good, OMark + 1, p, Vector2.one * (2f * Balance.DeliverRange));
            Transform up = Upright(r, "Cliente", p, out _);
            if (_sCliente != null)
            {
                ContactShadow(r, 0.4f).transform.localPosition = p;
                _client = new SpriteAnim(_sCliente, Art.NewSprite(up, "Cliente", null, Color.white, 0, Vector2.zero, Vector2.one));
                _waveT = WaveTime;
                return;
            }
            Art.NewSprite(up, "Cliente", Art.Disc(), Art.Client, 0, Vector2.zero, Vector2.one * 0.9f);
            Art.NewSprite(up, "Chapeu", Art.Triangle(), Art.Gold, 1, new Vector2(0f, 0.42f), new Vector2(0.7f, 0.5f));
            Art.NewSprite(up, "Olhos", Art.Eyes(), Art.Tinta, 2, new Vector2(0f, 0.02f), Vector2.one * 0.4f);
        }

        // ------------------------------------------------------------------ eventos -> efeitos (ART_BIBLE §9)

        public void OnEvent(SimEvent e, Run run)
        {
            if (run == null) return;
            Vector2 p = V(e.Pos);
            switch (e.Kind)
            {
                case Ev.DamageTaken:
                    _courierHit = 0.6f; Shake(0.08f, 0.18f);
                    Float(V(run.Courier.Pos), "-" + e.A, Art.Bad);
                    break;
                case Ev.Shot:   // o courier arremessa no compasso do tiro, virado para o alvo
                    _throwT = 0f;
                    foreach (Enemy x in run.Enemies) if (x.Id == e.A) _throwDir = x.Pos - run.Courier.Pos;
                    break;
                case Ev.CargoIntegrity:
                    for (int i = 0; i < run.Cargos.Count && i < _cargos.Length; i++)
                        if (run.Cargos[i].Id == e.A) { _cargos[i].HitT = 0.3f; Burst(p, Art.CargoColor(run.Cargos[i].Type), 6, 2.5f, 0.12f, 0.4f); }
                    Shake(0.12f, 0.18f);
                    Float(p, Textos.Rachou, Art.Cargo);
                    break;
                case Ev.CargoDrop:
                    for (int i = 0; i < run.Cargos.Count && i < _cargos.Length; i++) if (run.Cargos[i].Id == e.A) _cargos[i].PulseT = PulseTime;
                    Burst(p, Art.A(Art.Dim, 0.8f), 4, 1.5f, 0.1f, 0.25f);   // 4 graos de poeira
                    break;
                case Ev.CargoBroken: Float(p, Textos.Quebrou, Art.Bad, 56); Burst(p, Art.CargoColor(CargoType.Relic), 14, 4f, 0.18f, 0.6f); Burst(p, Art.Gold, 8, 3f, 0.12f, 0.5f); Shake(0.2f, 0.3f); break;
                case Ev.EnemyHit: _enemyHit[e.A] = 0.12f; Burst(p, Color.white, 3, 2f, 0.1f, 0.2f); break;
                case Ev.EnemyDie: Burst(p, Art.Role((EnemyType)e.B), 12, 3.5f, 0.2f, 0.45f); break;
                case Ev.Strike:
                    foreach (Ent x in _enemies) if (x.Id == e.A) x.StrikeT = StrikeHold;   // o resto do golpe em 0,15 s
                    if (e.B == (int)EnemyType.Ogre || e.B == (int)EnemyType.Brute)
                    {
                        Shake(e.B == (int)EnemyType.Ogre ? 0.2f : 0.12f, 0.2f);
                        Burst(p, Art.A(Art.Bone, 0.8f), 10, 4f, 0.14f, 0.3f);
                    }
                    break;
                case Ev.Dash: Burst(p, Art.A(Art.Ink, 0.7f), 6, 2f, 0.14f, 0.3f); break;
                case Ev.CargoPickup: Burst(p, Art.Cargo, 5, 2f, 0.1f, 0.25f); break;
                case Ev.ThiefGrab: Float(p, Textos.Ladrao, Art.Role(EnemyType.Thief), 50); Shake(0.08f, 0.15f); break;
                case Ev.ThiefSpawn: Burst(p, Art.A(Art.Role(EnemyType.Thief), 0.8f), 10, 2f, 0.22f, 0.5f); Float(p, "!", Art.Role(EnemyType.Thief), 56); break;
                case Ev.ThiefEscape: Float(p, Textos.Fugiu, Art.Role(EnemyType.Thief), 56); Burst(p, Art.A(Art.Role(EnemyType.Thief), 0.8f), 14, 2.5f, 0.25f, 0.6f); break;
                case Ev.ChestOpen: Float(p, "+" + e.A, Art.Coin, 52); Burst(p, Art.Coin, 12, 3f, 0.14f, 0.5f); break;
                case Ev.Hatch: Float(p, Textos.Chocou, Art.Pet, 52); Burst(p, Art.CargoColor(CargoType.Egg), 12, 3f, 0.14f, 0.5f); break;
                case Ev.PetHit: _petHit = 0.3f; Float(p, "-1", Art.Bad); break;
                case Ev.Deliver:   // confete na cor da carga (courier em Cheering e cliente reagindo saem do Outcome)
                    Burst(V(run.Courier.Pos), Art.Cargo, 22, 5f, 0.18f, 0.8f);
                    Burst(V(run.Courier.Pos), Art.Good, 14, 4f, 0.14f, 0.7f);
                    break;
            }
        }

        // ------------------------------------------------------------------ particulas e numeros flutuantes

        void Burst(Vector2 at, Color c, int n, float speed, float size, float life)
        {
            for (int i = 0; i < n; i++)
            {
                Part p = null;
                foreach (Part x in _parts) if (!x.Live) { p = x; break; }
                if (p == null)
                {
                    if (_parts.Count >= 120) return;   // ponytail: teto do pool; pico de efeitos perde particulas, nunca trava
                    p = new Part { R = Art.NewSprite(_root, "Particula", Art.Disc(), Color.white, OPart, Vector2.zero, Vector2.one) };
                    _parts.Add(p);
                }
                p.Live = true; p.R.enabled = true; p.Age = 0f; p.Life = life * Random.Range(0.7f, 1.2f);
                p.P = at; p.V = Random.insideUnitCircle * speed; p.C = c; p.S0 = size * Random.Range(0.8f, 1.3f); p.S1 = size * 0.3f;
            }
        }

        void TickParts(float dt)
        {
            foreach (Part p in _parts)
            {
                if (!p.Live) continue;
                p.Age += dt;
                if (p.Age >= p.Life) { p.Live = false; p.R.enabled = false; continue; }
                float k = p.Age / p.Life;
                p.P += p.V * dt; p.V *= Mathf.Max(0f, 1f - 4f * dt);
                p.R.transform.localPosition = p.P;
                p.R.transform.localScale = Vector3.one * Mathf.Lerp(p.S0, p.S1, k);
                p.R.color = Art.A(p.C, p.C.a * (1f - k));
            }
        }

        public void Float(Vector2 at, string s, Color c, int size = 44)
        {
            Floater f = null;
            foreach (Floater x in _floaters) if (!x.Live) { f = x; break; }
            if (f == null)
            {
                if (_floaters.Count >= 16) f = _floaters[0];   // reaproveita o mais antigo
                else
                {
                    Text t = Art.Outlined(Art.NewText(_labels, "Flutuante", size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), 3f);
                    t.rectTransform.sizeDelta = new Vector2(400f, 80f); t.horizontalOverflow = HorizontalWrapMode.Overflow; t.fontStyle = FontStyle.Bold;
                    _floaters.Add(f = new Floater { T = t });
                }
            }
            f.Live = true; f.Age = 0f; f.W = new Vector3(at.x, at.y + 0.5f, 0f);
            f.T.text = s; f.T.color = c; f.T.fontSize = size; f.T.enabled = true;
            f.T.rectTransform.position = ToScreen(f.W);
        }

        void TickFloaters(float dt)
        {
            foreach (Floater f in _floaters)
            {
                if (!f.Live) continue;
                f.Age += dt;
                if (f.Age >= 0.9f) { f.Live = false; f.T.enabled = false; continue; }
                f.T.rectTransform.position = ToScreen(f.W + new Vector3(0f, 0.9f * f.Age, 0f));
                f.T.color = Art.A(f.T.color, Mathf.Clamp01((0.9f - f.Age) / 0.3f));
            }
        }

        /// <summary>Ponto em m da sala -> px de tela (passa pela projecao do _root).</summary>
        Vector3 ToScreen(Vector3 room) => _cam.WorldToScreenPoint(_root.TransformPoint(room));

        void HideLabels()
        {
            foreach (Floater f in _floaters) { f.Live = false; f.T.enabled = false; }
            foreach (Text t in _roomLabels) Object.Destroy(t.gameObject);
            _roomLabels.Clear(); _roomLabelPos.Clear();
            _roomKey = -1;
        }

        static Vector2 V(V2 v) => new Vector2(v.X, v.Y);
        /// <summary>Angulo (graus) para girar algo desenhado apontando para +Y ate a direcao d.</summary>
        static float Angle(V2 d) => Mathf.Atan2(d.Y, d.X) * Mathf.Rad2Deg - 90f;
    }
}
