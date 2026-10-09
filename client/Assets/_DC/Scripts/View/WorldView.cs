using System.Collections.Generic;
using DC.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DC
{
    /// <summary>
    /// Desenha a sala e as entidades a partir do Run, sem regra nenhuma (so le o Core). Coordenadas de mundo = metros da sala (origem
    /// no canto inferior esquerdo). Camera ortografica fixa com a sala inteira + paredes + placas das portas, abaixo da faixa da HUD e
    /// dentro da area segura (Fit). Cores e leituras: docs/ART_BIBLE.md §3, §6-§9 (contorno limao da carga piscando a 2 Hz quando longe,
    /// anel de contagem do ladrao, carga vermelha dentro de telegraph, linha roxa de intencao do ladrao, 3 encaixes de caveira).
    /// Ponto de troca por sprite real: um Paint* por entidade (courier, inimigo, carga, filhote, bau, cliente) e o BuildDoor.
    /// </summary>
    public sealed class WorldView
    {
        // o que a camera precisa mostrar (m): sala 9 x 14 + paredes (0,35) + placas de ameaca/recompensa acima das portas
        const float ViewL = -0.55f, ViewR = 9.55f, ViewB = -0.55f, ViewT = 15.45f;
        const float WallT = 0.35f, TopWall = 0.5f, PlateY = 15.0f, PlateH = 0.62f, Blink = 2f, PulseTime = 0.35f;
        // ordens de desenho
        const int OFloor = 0, OMark = 1, OShadow = 2, OBlock = 3, OWall = 4, ODoor = 5, OPlate = 6, ORing = 8, OChest = 10, OCargo = 12,
            OPet = 14, OEnemy = 16, OCourier = 20, OHeld = 26, OShot = 28, OPart = 30;

        sealed class Ent
        {
            public Transform Root, Dir; public SpriteRenderer Body, Deco, Eyes, Pupils, Ring, Fill, HpBg, HpFill, Arc, Line;
            public int Type = -1; public Color BodyC = Color.white;
        }
        sealed class CargoV
        {
            public Transform Root; public SpriteRenderer Range, Outline, Body, Deco, Cracks, Seal, Star, Count;
            public CargoType Type; public bool Rescue, Painted; public float HitT, PulseT;
        }
        sealed class DoorV { public SpriteRenderer Frame, Bars, Glow, Lock; public Door D; }
        sealed class Part { public SpriteRenderer R; public Vector2 P, V; public float Age, Life, S0, S1; public Color C; public bool Live; }
        sealed class Floater { public Text T; public Vector3 W; public float Age; public bool Live; }

        readonly Camera _cam;
        readonly RectTransform _labels;
        readonly Transform _root, _roomRoot;
        readonly List<Text> _roomLabels = new List<Text>();
        readonly List<Vector3> _roomLabelPos = new List<Vector3>();
        Run _run; int _roomKey = -1;
        Vector3 _camBase; float _shakeT, _shakeDur = 1f, _shakeA, _time;

        readonly Ent _courier; readonly SpriteRenderer _plantRing; float _courierHit; Vector2 _lastCourier;
        readonly CargoV[] _cargos = new CargoV[2];
        readonly Ent _pet; float _petHit;
        SpriteRenderer _chest, _chestLock, _chestArc, _deliverRing;
        readonly List<DoorV> _doors = new List<DoorV>();
        readonly List<Ent> _enemies = new List<Ent>();
        readonly Dictionary<int, float> _enemyHit = new Dictionary<int, float>();
        readonly List<Ent> _shots = new List<Ent>();
        readonly List<Part> _parts = new List<Part>();
        readonly List<Floater> _floaters = new List<Floater>();

        public WorldView(Camera cam, RectTransform labels)
        {
            _cam = cam; _labels = labels;
            _root = new GameObject("Mundo").transform;
            _roomRoot = new GameObject("Sala").transform;
            _roomRoot.SetParent(_root, false);
            _courier = NewEnt("Courier", OCourier);
            PaintCourier(_courier);
            _plantRing = Art.NewSprite(_courier.Root, "Pousado", Art.DashedRing(), Art.A(Art.Ink, 0.45f), ORing, Vector2.zero, Vector2.one * 1.2f);
            for (int i = 0; i < _cargos.Length; i++) _cargos[i] = NewCargo(i);
            _pet = NewEnt("Filhote", OPet);
            PaintPet(_pet);
            _root.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ camera

        /// <summary>Encaixa a sala inteira (com paredes e placas) no retangulo de tela `px` (area segura menos a faixa da HUD).</summary>
        public void Fit(UnityEngine.Rect px)
        {
            float ppu = Mathf.Max(1f, Mathf.Min(px.width / (ViewR - ViewL), px.height / (ViewT - ViewB)));
            _cam.orthographicSize = Screen.height / (2f * ppu);
            Vector2 c = px.center, sc = new Vector2(Screen.width, Screen.height) * 0.5f;
            _camBase = new Vector3((ViewL + ViewR) * 0.5f - (c.x - sc.x) / ppu, (ViewB + ViewT) * 0.5f - (c.y - sc.y) / ppu, -10f);
            _cam.transform.position = _camBase;
        }

        public void Shake(float amp, float dur)
        {
            if (amp < _shakeA * (_shakeT / _shakeDur)) return;
            _shakeA = amp; _shakeT = _shakeDur = dur;
        }

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
            DrawCargos(run, dt);
            DrawPet(run, dt);
            DrawEnemies(run, dt);
            DrawShots(run);
            DrawRoomState(run);
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
            _courier.Dir.localRotation = Quaternion.Euler(0f, 0f, Angle(c.State == CourierState.Dashing ? c.DashDir : c.Facing));
            float d = 2f * Balance.CourierR;
            Vector2 s = Vector2.one;
            if (c.State == CourierState.Dashing)
            {
                s = new Vector2(0.75f, 1.35f);
                Burst(p, Art.A(Art.Courier, 0.4f), 1, 0f, 0.75f, 0.18f);   // fantasmas azuis do dash (ART_BIBLE §9)
            }
            else if (moved && c.State != CourierState.Dead)
            {
                float k = Mathf.Sin(_time * 18f);
                s = new Vector2(1f - 0.06f * k, 1f + 0.08f * k);   // squash and stretch andando
            }
            _courier.Body.transform.localScale = new Vector3(d * s.x, d * s.y, 1f);
            _courierHit = Mathf.Max(0f, _courierHit - dt);
            Color body = c.State == CourierState.Dead ? Art.Dim : Art.Courier;
            // dano: flash branco e pisca por 0,6 s (ART_BIBLE §9)
            bool blink = _courierHit > 0f && Mathf.Repeat(_courierHit * 10f, 1f) < 0.5f;
            _courier.Body.color = _courierHit > 0.52f ? Color.white : blink ? Art.A(body, 0.45f) : body;
            _plantRing.enabled = c.State == CourierState.Planted;
            _plantRing.transform.localRotation = Quaternion.Euler(0f, 0f, _time * 60f);
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
                bool ground = g.State == CargoState.Ground;
                float size = ground ? 0.62f : 0.5f;
                Vector2 pos = g.State == CargoState.Held ? V(c.Pos) + new Vector2(0f, 0.3f + 0.4f * held++)
                    : g.State == CargoState.Stolen ? V(g.Pos) + new Vector2(0f, 0.35f) : V(g.Pos);
                v.Root.localPosition = pos;
                v.HitT = Mathf.Max(0f, v.HitT - dt);
                float pop = v.HitT > 0.18f ? 1f + (v.HitT - 0.18f) / 0.12f * 0.3f : 1f;   // rachou: pop 1,3 -> 1
                v.Root.localScale = Vector3.one * (size * pop);
                int baseOrder = ground ? OCargo : OHeld;
                v.Outline.sortingOrder = baseOrder; v.Body.sortingOrder = baseOrder + 1;
                v.Deco.sortingOrder = v.Cracks.sortingOrder = baseOrder + 2; v.Seal.sortingOrder = baseOrder + 3; v.Star.sortingOrder = baseOrder + 4;
                // ovo nos ultimos 5 s antes de chocar: balanca +-8 graus a 4 Hz
                float wob = g.Type == CargoType.Egg && run.EggLeft > 0f && run.EggLeft < 5f ? Mathf.Sin(_time * 25f) * 8f : 0f;
                v.Root.localRotation = Quaternion.Euler(0f, 0f, wob);
                Color baseC = Art.CargoColor(g.Type);
                v.Body.color = v.HitT > 0.24f ? Color.white : baseC;
                v.Cracks.enabled = g.Cracks > 0;
                if (g.Cracks > 0) v.Cracks.sprite = Art.Cracks(g.Cracks);
                if (g.Type == CargoType.Relic && g.Cracks == Balance.RelicBreak - 1)   // a proxima quebra: pulsa branco e vermelho
                    v.Body.color = Color.Lerp(Color.white, Art.Bad, 0.5f + 0.5f * Mathf.Sin(_time * 10f));

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
                float pr = 2f * Balance.PickupRange / (size * pop), pk = v.PulseT > 0f ? 1f - v.PulseT / PulseTime : 1f;
                v.Range.transform.localScale = Vector3.one * (pr * pk);
                v.Range.color = Art.A(Art.Cargo, v.PulseT > 0f ? 0.9f * (1f - pk) + 0.2f : far <= Balance.PickupRange ? 0.4f : 0.18f);
            }
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
            _pet.Root.localPosition = V(p.Pos) + new Vector2(0f, 0.08f * hop);
            _pet.Root.localScale = new Vector3(1f + 0.08f * (1f - hop), 1f - 0.06f * (1f - hop), 1f);   // pulinhos com squash
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
                EnemyDef d = e.Def;
                Vector2 p = V(e.Pos);
                float bob = e.Type == EnemyType.Bat ? 0.06f * Mathf.Sin(_time * 19f + e.Phase * 6f) : 0f;   // morcego flutua a 3 Hz
                v.Root.localPosition = p + new Vector2(0f, bob);
                Vector2 look = (cp - p).sqrMagnitude > 1e-4f ? (cp - p).normalized : Vector2.down;
                v.Eyes.transform.localPosition = look * (0.12f * d.Radius) + new Vector2(0f, 0.15f * d.Radius);
                v.Pupils.transform.localPosition = v.Eyes.transform.localPosition + (Vector3)(look * 0.06f * d.Radius);
                if (e.Type == EnemyType.Bat)   // asa por escala X de 1,0 a 0,65 a 10 Hz
                    v.Body.transform.localScale = new Vector3(d.Radius * 4.8f * (0.65f + 0.35f * Mathf.Abs(Mathf.Sin(_time * 31.4f + e.Phase * 6f))), d.Radius * 4.8f, 1f);
                float hit = _enemyHit.TryGetValue(e.Id, out float h) ? h : 0f;
                if (hit > 0f) _enemyHit[e.Id] = Mathf.Max(0f, hit - dt);
                bool tele = e.State == EnemyState.Telegraph;
                // flash branco no golpe recebido; tinta vermelha piscando nos ultimos 0,15 s do telegraph
                v.Body.color = hit > 0.06f ? Color.white : tele && e.Tele < 0.15f && Mathf.Repeat(_time * 12f, 1f) < 0.5f ? Color.Lerp(v.BodyC, Art.Bad, 0.3f) : v.BodyC;
                // telegraph (ART_BIBLE §7): circulo de raio Reach; enchimento a 25% crescendo com TeleProgress; borda a 90%, branca no ultimo 0,1 s
                v.Ring.enabled = v.Fill.enabled = tele;
                if (tele)
                {
                    float r2 = 2f * d.Reach;
                    v.Ring.transform.localScale = Vector3.one * r2;
                    v.Fill.transform.localScale = Vector3.one * (r2 * Mathf.Max(0.05f, e.TeleProgress));
                    v.Ring.color = e.Tele < 0.1f ? Color.white : Art.A(Art.Tele, 0.9f);
                }
                // vida: so depois do 1o golpe (sempre no Guarda/Ogro, que trancam a porta)
                bool bar = e.Hp < d.Hp || d.Locks;
                v.HpBg.enabled = v.HpFill.enabled = bar;
                if (bar)
                {
                    float w = Mathf.Max(0.6f, 2.2f * d.Radius), f = (float)e.Hp / d.Hp, y = d.Radius + 0.22f;
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
            for (int i = 0; i < _shots.Count; i++)
            {
                Ent v = _shots[i];
                bool on = i < list.Count && list[i].Target != null;
                if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
                if (!on) continue;
                Shot s = list[i];
                v.Root.localPosition = V(s.Pos);
                v.Root.localRotation = Quaternion.Euler(0f, 0f, Angle(s.Target.Pos - s.Pos));
            }
        }

        void DrawRoomState(Run run)
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
                _chest.color = ch.Opened ? Art.A(Art.Chest, 0.45f) : Art.Chest;
                _chestLock.enabled = !ch.Opened;
                bool prog = !ch.Opened && ch.Progress > 0f;
                _chestArc.enabled = prog;
                if (prog) _chestArc.sprite = Art.Arc(ch.Progress / Balance.ChestTime);
            }
            if (_deliverRing != null)
            {
                _deliverRing.color = Art.A(Art.Good, run.CarryOk ? 0.6f + 0.3f * Mathf.Sin(_time * 4f) : 0.3f);
                _deliverRing.transform.localRotation = Quaternion.Euler(0f, 0f, -_time * 25f);
            }
        }

        // ------------------------------------------------------------------ sala (estatico, refeito a cada sala)

        void BuildRoom(Run run, int key)
        {
            _run = run; _roomKey = key;
            for (int i = _roomRoot.childCount - 1; i >= 0; i--) Object.Destroy(_roomRoot.GetChild(i).gameObject);
            foreach (Text t in _roomLabels) Object.Destroy(t.gameObject);
            _roomLabels.Clear(); _roomLabelPos.Clear(); _doors.Clear(); _enemyHit.Clear();
            _chest = _chestLock = _chestArc = _deliverRing = null;
            foreach (CargoV v in _cargos) v.PulseT = 0f;
            Transform r = _roomRoot;
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
            foreach (Door d in run.Doors) BuildDoor(r, d);
            if (run.Chest != null) PaintChest(r, run.Chest.Pos);
            if (room.Delivery) PaintClient(r);
            _root.gameObject.SetActive(true);
        }

        /// <summary>Porta (ART_BIBLE §8): vao de 1,6 m com grade de aco, cadeado vermelho do guarda, luz verde aberta; placa de pedra acima
        /// com 3 encaixes de caveira (ameaca N de 3), bau com as moedas, bandeira verde da entrega e chifres do chefe.</summary>
        void BuildDoor(Transform r, Door d)
        {
            float x = d.Pos.X, w = 2f * Balance.DoorHalf, y = Balance.RoomH + TopWall / 2f;
            var dv = new DoorV { D = d };
            dv.Glow = Art.NewSprite(r, "Luz", Art.Glow(), Art.A(Art.Good, 0.4f), OMark, new Vector2(x, Balance.RoomH - 0.3f), new Vector2(w * 1.6f, 1.6f));
            dv.Frame = Rect(r, "Batente", x, y, w + 0.16f, TopWall + 0.02f, Art.Cargo, ODoor);
            Rect(r, "Vao", x, y - 0.03f, w, TopWall - 0.06f, Art.DoorIn, ODoor + 1);
            dv.Bars = Art.NewSprite(r, "Grade", Art.Bars(), Art.Steel, ODoor + 2, new Vector2(x, y - 0.03f), new Vector2(w, TopWall - 0.06f));
            dv.Lock = Art.NewSprite(r, "Cadeado", Art.Lock(), Art.Role(EnemyType.Guard), ODoor + 3, new Vector2(x, y - 0.02f), Vector2.one * 0.38f);
            _doors.Add(dv);

            RoomDef to = Content.Rooms[d.Room];
            const float icon = 0.36f, gap = 0.04f;
            bool special = to.Delivery || to.Boss;
            float pw = 3 * (icon + gap) + 0.16f + (special ? icon + gap : 0f);
            Rect(r, "PlacaBorda", x, PlateY, pw + 0.08f, PlateH + 0.08f, Art.Tinta, OPlate);
            Rect(r, "Placa", x, PlateY, pw, PlateH, Art.Plate, OPlate + 1);
            float x0 = x - pw / 2f + 0.08f + icon / 2f;
            if (special)
            {
                Art.NewSprite(r, to.Delivery ? "Entrega" : "Chefe", to.Delivery ? Art.Flag() : Art.Horns(), to.Delivery ? Art.Good : Art.Bone, OPlate + 2, new Vector2(x0, PlateY), Vector2.one * icon);
                x0 += icon + gap;
            }
            for (int i = 0; i < 3; i++)
                Art.NewSprite(r, "Caveira", Art.Skull(), i < d.Threat ? Art.Bone : Art.Tinta, OPlate + 2, new Vector2(x0 + i * (icon + gap), PlateY), Vector2.one * icon);
            if (d.ChestCoins <= 0) return;
            Vector2 cp = new Vector2(x + pw / 2f + 0.3f, PlateY);   // bau ao lado da placa, com as moedas em numero
            Art.NewSprite(r, "Bau", Art.ChestShape(), Art.Chest, OPlate + 2, cp, Vector2.one * 0.45f);
            Text t = Art.Outlined(Art.NewText(_labels, "Moedas", 30, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), 3f);
            t.rectTransform.sizeDelta = new Vector2(120f, 50f); t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.text = Textos.Num(d.ChestCoins); t.color = Art.Coin; t.fontStyle = FontStyle.Bold;
            _roomLabels.Add(t); _roomLabelPos.Add(new Vector3(cp.x + 0.45f, PlateY, 0f));
        }

        SpriteRenderer Rect(Transform parent, string name, float cx, float cy, float w, float h, Color c, int order) =>
            Art.NewSprite(parent, name, Art.Square(), c, order, new Vector2(cx, cy), new Vector2(w, h));

        // ------------------------------------------------------------------ Paint*: o ponto de troca por sprite real, um por entidade

        Ent NewEnt(string name, int order)
        {
            var e = new Ent { Root = new GameObject(name).transform };
            e.Root.SetParent(_root, false);
            e.Dir = new GameObject("Dir").transform; e.Dir.SetParent(e.Root, false);
            e.Ring = Art.NewSprite(e.Root, "Telegraph", Art.ThinRing(), Art.Tele, ORing, Vector2.zero, Vector2.one);
            e.Fill = Art.NewSprite(e.Root, "TelegraphCheio", Art.Disc(), Art.A(Art.Tele, 0.25f), ORing - 1, Vector2.zero, Vector2.one);
            e.Line = Art.NewSprite(e.Root, "Intencao", Art.Square(), Art.A(Art.Role(EnemyType.Thief), 0.6f), ORing, Vector2.zero, Vector2.one);
            e.Body = Art.NewSprite(e.Dir, "Corpo", Art.Disc(), Color.white, order, Vector2.zero, Vector2.one);
            e.Deco = Art.NewSprite(e.Dir, "Enfeite", Art.Disc(), Color.white, order + 1, Vector2.zero, Vector2.one);
            e.Eyes = Art.NewSprite(e.Root, "Olhos", Art.Eyes(), Color.white, order + 2, Vector2.zero, Vector2.one);
            e.Pupils = Art.NewSprite(e.Root, "Pupilas", Art.Eyes(), Art.Tinta, order + 3, Vector2.zero, Vector2.one);
            e.HpBg = Art.NewSprite(e.Root, "VidaFundo", Art.Square(), Art.A(Art.Tinta, 0.85f), order + 4, Vector2.zero, Vector2.one);
            e.HpFill = Art.NewSprite(e.Root, "Vida", Art.Square(), Art.Bad, order + 5, Vector2.zero, Vector2.one);
            e.Arc = Art.NewSprite(e.Root, "Arco", Art.Arc(1f), Art.Role(EnemyType.Thief), order + 6, Vector2.zero, Vector2.one);
            e.Ring.enabled = e.Fill.enabled = e.Line.enabled = e.HpBg.enabled = e.HpFill.enabled = e.Arc.enabled = false;
            return e;
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
        /// de pedra; Ogro = disco musgo grande com chifres de osso.</summary>
        static void PaintEnemy(Ent v, EnemyType t)
        {
            v.Type = (int)t;
            v.BodyC = Art.Role(t);
            float r = EnemyDef.All[(int)t].Radius, d = 2f * r;
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
            v.Range = Art.NewSprite(v.Root, "Alcance", Art.DashedRing(), Art.A(Art.Cargo, 0.2f), ORing, Vector2.zero, Vector2.one);
            v.Count = Art.NewSprite(v.Root, "Contagem", Art.Arc(0f), Art.Cargo, ORing + 1, Vector2.zero, Vector2.one * 1.7f);
            v.Outline = Art.NewSprite(v.Root, "Contorno", Art.Rounded(), Art.Cargo, OCargo, Vector2.zero, Vector2.one * 1.22f);
            v.Body = Art.NewSprite(v.Root, "Corpo", Art.Rounded(), Color.white, OCargo + 1, Vector2.zero, Vector2.one);
            v.Deco = Art.NewSprite(v.Root, "Enfeite", Art.Square(), Color.white, OCargo + 2, Vector2.zero, Vector2.one);
            v.Cracks = Art.NewSprite(v.Root, "Rachaduras", Art.Cracks(1), Art.Crack, OCargo + 2, Vector2.zero, Vector2.one * 0.95f);
            v.Seal = Art.NewSprite(v.Root, "SeloFundo", Art.Disc(), Art.Tinta, OCargo + 3, new Vector2(0.48f, 0.48f), Vector2.one * 0.62f);
            v.Star = Art.NewSprite(v.Seal.transform, "Selo", Art.Star(), Art.Cream, OCargo + 4, Vector2.zero, Vector2.one * 0.8f);
            v.Count.enabled = false;
            v.Root.gameObject.SetActive(false);
            return v;
        }

        /// <summary>Carga (escala 1 = tamanho dela, ART_BIBLE §3/§6): caixa de madeira com cinta, reliquia de porcelana com gema, ovo com
        /// pintas. Resgate: selo creme numa medalha escura (o roxo e so do ladrao).</summary>
        static void PaintCargo(CargoV v, CargoType t, bool rescue)
        {
            v.Painted = true; v.Type = t; v.Rescue = rescue;
            v.Body.sprite = Art.CargoSprite(t);
            v.Outline.sprite = v.Body.sprite;
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

        /// <summary>Filhote: coral (continua as pintas do ovo), bico dourado e a vida (2) numa barrinha.</summary>
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

        void PaintChest(Transform r, V2 at)
        {
            Vector2 p = V(at);
            _chest = Art.NewSprite(r, "Bau", Art.ChestShape(), Art.Chest, OChest, p, Vector2.one * 0.8f);
            _chestLock = Art.NewSprite(r, "Fecho", Art.Square(), Art.Gold, OChest + 1, p + new Vector2(0f, 0.1f), Vector2.one * 0.14f);
            _chestArc = Art.NewSprite(r, "Abrindo", Art.Arc(0f), Art.Coin, OChest + 2, p, Vector2.one * 1.3f);
            _chestArc.enabled = false;
        }

        /// <summary>Cliente da entrega (D1): verde com chapeu dourado, num tapete de pedra, e o anel tracejado verde da area de entrega.</summary>
        void PaintClient(Transform r)
        {
            Vector2 p = V(Balance.Client);
            Art.NewSprite(r, "Tapete", Art.Rounded(), Art.Plate, OMark, p + new Vector2(0f, 0.35f), new Vector2(2.6f, 1.3f));
            _deliverRing = Art.NewSprite(r, "AreaEntrega", Art.DashedRing(), Art.Good, OMark + 1, p, Vector2.one * (2f * Balance.DeliverRange));
            Art.NewSprite(r, "Cliente", Art.Disc(), Art.Client, OCourier, p, Vector2.one * 0.9f);
            Art.NewSprite(r, "Chapeu", Art.Triangle(), Art.Gold, OCourier + 1, p + new Vector2(0f, 0.42f), new Vector2(0.7f, 0.5f));
            Art.NewSprite(r, "Olhos", Art.Eyes(), Art.Tinta, OCourier + 2, p + new Vector2(0f, 0.02f), Vector2.one * 0.4f);
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
                case Ev.Deliver:   // confete na cor da carga
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
            f.T.rectTransform.position = _cam.WorldToScreenPoint(f.W);
        }

        void TickFloaters(float dt)
        {
            foreach (Floater f in _floaters)
            {
                if (!f.Live) continue;
                f.Age += dt;
                if (f.Age >= 0.9f) { f.Live = false; f.T.enabled = false; continue; }
                f.T.rectTransform.position = _cam.WorldToScreenPoint(f.W + new Vector3(0f, 0.9f * f.Age, 0f));
                f.T.color = Art.A(f.T.color, Mathf.Clamp01((0.9f - f.Age) / 0.3f));
            }
        }

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
