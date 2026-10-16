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
    /// cadeado da porta, aneis, numeros. Conteudo da v1.0-beta (inimigos, cargas, armas, salas, ambiente, VFX) em WorldViewV1.cs.
    /// </summary>
    public sealed partial class WorldView
    {
        // projecao: tela_y = Tilt*y + Lift*z (z = altura sobre o chao)
        const float Tilt = 0.866f, Lift = 0.5f;
        // o que a camera precisa mostrar: x e o pe em m da sala; o topo vai ate as placas das portas (_plateS, em m de tela).
        // Largura 9,7 m = sala de 9 m + paredes de 0,35 m, rente (ART_BIBLE §2: ~112 px/m em 1080 px); em 16:9 a altura quase empata
        // (9,73 m em 540x960); medida e conta em docs/VIEW_SPRITES.md §3
        const float ViewL = -WallT, ViewR = 9f + WallT, ViewB = -0.4f, ViewPad = 0.14f;
        const float WallT = 0.35f, TopWall = 0.5f, PlateH = 0.62f, PlateGap = 0.12f, Blink = 2f, PulseTime = 0.35f;
        // medido no PNG do `arco` (448 px a 120 px/m, pivo no pe; VIEW_SPRITES §2), em m de tela acima da base da parede: topo do arco 1,78;
        // vao escuro OPACO de 1,21 m de largura do chao ate 0,55 (quinas curvas desde 0,45) -> a grade vai por cima do sprite, no vao
        const float ArchTop = 1.78f, ArchOpen = 0.52f, ArchOpenW = 1.2f;
        // arremesso: solta no quadro 3 de 8 do courier/Throw (ASSETS §5); resto do golpe; clipe Hit; largura de projeto do bau
        const float ThrowRelease = 3f / 8f, StrikeHold = 0.15f, HitTime = 0.3f, BauW = 0.8f, HitFlashGap = 0.3f;
        // altura de voo do morcego e do projetil e do pulo do filhote (m, altura real: x Lift na tela); contorno limao da carga (m)
        const float BatZ = 1.0f, ShotZ = 0.9f, PetHop = 0.15f, CargoLine = 0.06f, WaveTime = 2.5f;
        // ordens de desenho: decalques do chao (sombra/anel dos atores 7-9) < porta (o pe do arco cobre o chao atras dele) < placa
        // (a cavalo no arco) < OActor; dentro de um SortingGroup as ordens dos filhos sao so relativas
        const int OFloor = 0, OMark = 1, OShadow = 2, OBlock = 3, OWall = 4, OActShadow = 7, ORing = 8, ODoor = 10, OPlate = 14, OChest = 10,
            OCargo = 12, OPet = 14, OEnemy = 16, OCourier = 20, OActor = 100, OShot = 4000, OPart = 4100;

        // Tabelas por indice do enum (EnemyType / CargoType). Leia SEMPRE por At(): valor novo do enum sem linha aqui cai no default
        // (sem folha = procedural), nunca IndexOutOfRange. Lote 3 (ASSETS §8): arqueiro, limo, capitao; bolo, mensagem, caixa_pesada, barril.
        // Regioes 2-4 (CONTEUDO_REGIOES §12; nomes de pasta pelo RENDER_SPRITES: `goblin_mineiro`, `rainha_toupeira`). "a|b" = o 1o que
        // existir (as pastas das regioes 3-4 ainda podem mudar de nome quando a raia G renderizar).
        static readonly string[] EnemySheet =
        {
            "morcego", "ladrao", "guarda", "bruto", "ogro", "arqueiro", "limo", "capitao",
            "goblin_mineiro|mineiro", "toupeira", "sapo", "besouro", "vagonete", "fantasma", "esqueleto", "aranha", "mumia", "gargula",
            "salamandra", "diabrete", "kobold|kobold_incendiario", "magma|elemental_magma",
            "rainha_toupeira|rainha", "golem", "viuva|viuva_aranha", "necromante", "capataz|capataz_kobold", "dragao|dragao_anciao",
            "brasa|brasa_magma", null,   // o ovo de salamandra usa a folha `ovo` tingida (PaintRegiao)
        };
        static readonly string[] AttackClip = { null, null, "Bash", "Smash", "Slam" };
        // quadro de impacto / quadros do golpe, medido na folha (ASSETS §5): guarda/Bash 6 de 12; mineiro/Throw 7 de 12, rainha/Swipe 6 de
        // 12, golem/Slam 5 de 9 (RENDER_SPRITES §9). ponytail: bruto/Smash e ogro/Slam 0,6, arqueiro 0,7, Capitao 0,5 e as regioes 3-4
        // pelo CONTEUDO_REGIOES §12 sao estimativa ate medir as folhas
        static readonly float[] ImpactFrac =
        {
            0f, 0f, 6f / 12f, 0.6f, 0.6f, 0.7f, 0f, 0.5f,
            7f / 12f, 0.5f, 0.5f, 0f, 0f, 0.5f, 0.5f, 0.5f, 0.6f, 0.5f, 0.5f, 0.5f, 0.6f, 0.6f,
            6f / 12f, 5f / 9f, 0.5f, 0.8f, 0.6f, 0.5f, 0.5f, 0f,
        };
        /// <summary>Altura de projeto por papel (ART_BIBLE §4 / CONTEUDO_REGIOES §12): barra de vida acima da cabeca do sprite.</summary>
        static readonly float[] EnemyH =
        {
            0.7f, 1.0f, 1.5f, 1.8f, 2.8f, 1.3f, 0.6f, 1.25f,
            1.0f, 0.9f, 0.6f, 0.45f, 0.9f, 0.9f, 1.35f, 0.5f, 1.5f, 1.4f, 0.4f, 0.9f, 1.0f, 1.9f,
            1.6f, 2.6f, 1.0f, 1.6f, 1.4f, 2.2f, 1.1f, 0.5f,
        };
        internal static readonly string[] CargoSheet =
            { "caixa", "reliquia", "ovo", "bolo", "mensagem", "caixa_pesada", "barril", "coracao", "cristal", "gaiola", "lanterna", "espelho", "balao", "planta", "mapa" };
        // lado da carga na tela (m): rachaduras e contorno de reserva, e o passo da pilha nas maos. Caixa medida no PNG (silhueta 0,58 x
        // 0,69 m); ponytail: reliquia, ovo e as dos Lotes 3-6 pela ART_BIBLE §4 / prompts (ASSETS §8, CONTEUDO_REGIOES §12) ate medir
        static readonly float[] CargoSize = { 0.6f, 0.5f, 0.5f, 0.6f, 0.45f, 0.7f, 0.55f, 0.45f, 0.5f, 0.5f, 0.45f, 0.5f, 0.6f, 0.55f, 0.45f };

        static T At<T>(T[] a, int i, T fallback) => i >= 0 && i < a.Length ? a[i] : fallback;
        /// <summary>Folha do papel/carga i, ou null (sem nome na tabela ou sem pasta): o procedural. "a|b" tenta na ordem (so ao pintar).</summary>
        static SpriteSheet SheetAt(string[] names, int i)
        {
            string n = At(names, i, null);
            if (n == null) return null;
            if (n.IndexOf('|') < 0) return SpriteSheet.Get(n);
            foreach (string k in n.Split('|')) { SpriteSheet s = SpriteSheet.Get(k); if (s != null) return s; }
            return null;
        }
        // flash sem o shader DC/SpriteFlash (FxMat null): o Sprites/Default so multiplica a cor, entao vira tinta
        static readonly Color Flash = new Color(1f, 0.55f, 0.55f), Ghost = new Color(1f, 1f, 1f, 0.45f);

        sealed class Ent
        {
            public Transform Root, Up, Dir; public SortingGroup G;
            public SpriteRenderer Body, Deco, Eyes, Pupils, Ring, Fill, HpBg, HpFill, Arc, Line, Shadow, Spr;
            // v1: mira do arqueiro (chao), enfeite procedural (arco do arqueiro, chapeu do Capitao), estrelas de atordoado
            public SpriteRenderer AimLine, AimFill, Extra, StunA, StunB;
            // regioes 2-4 (WorldViewRegioes): olhos no escuro, zZ, ponto fraco do Golem, projetil em arco (picareta/frasco) e a sombra dele
            public SpriteRenderer Glow, ZA, ZB, Weak, Lob, LobShadow;
            public int Type = -1, Id = -1; public Color BodyC = Color.white;
            public SpriteAnim Anim;   // null = procedural
            public Vector2 Last, Hand; public bool HasHand, HandFront = true, FlipX; public float StrikeT, RoarT, SpawnT;
            /// <summary>HitT = clipe Hit; WakeT = clipe Wake (WasAsleep = para tocar); Shown = corpo visivel (escuro, por baixo da terra, no
            /// teto, no ar); StepT = ritmo das ondas/rastro; TeleT0/TeleDur = relogio proprio do aviso (TeleK) quando o Core nao desconta Tele.</summary>
            public bool WasAsleep, WasTele, WasHead, Shown = true; public float HitT, WakeT, StepT, TeleT0, TeleDur;
        }
        sealed class CargoV
        {
            public Transform Root, Up, Vis; public SortingGroup G;
            public SpriteRenderer Range, Outline, Body, Deco, Cracks, Seal, Star, Count, Spr;
            // v1: alcance e enchimento da explosao do barril, faisca do pavio, brilho da carga acolchoada, contagem 3-2-1
            public SpriteRenderer Blast, BlastFill, Spark, Pad; public Text Num; public int NumShown = -1; public float NumPop;
            // regioes 2-4: anel do coracao (4 m), luz da lanterna, fio do balao, barra do gelo
            public SpriteRenderer Aura, Lamp, Str, IceBg, IceFill;
            public CargoType Type; public bool Rescue, Painted; public float HitT, PulseT; public SpriteSheet Sheet;
        }
        sealed class DoorV { public SpriteRenderer Frame, Bars, Glow, Lock, Env; public Door D; public bool Was; public Vector2 LockAt; }
        sealed class Part { public SpriteRenderer R; public Vector2 P, V; public float Age, Life, S0, S1, K = 1f; public Color C; public bool Live; }
        sealed class Floater { public Text T; public Vector3 W; public float Age; public bool Live; }

        readonly Camera _cam;
        readonly RectTransform _labels;
        readonly Transform _root, _roomRoot;
        readonly List<Text> _roomLabels = new List<Text>();
        readonly List<Vector3> _roomLabelPos = new List<Vector3>();
        readonly SpriteSheet _sCourierFim, _sArco, _sBau, _sBauAberto, _sCliente, _sOgroFim;
        SpriteSheet _sCourier;   // a folha da roupa equipada (Roupa)
        Outfit _roupa = Outfit.Classic; Color _roupaC = Color.white;
        readonly float _plateS;   // y de tela do centro das placas das portas
        Run _run; int _roomKey = -1;
        Vector3 _camBase; float _shakeT, _shakeDur = 1f, _shakeA, _time;

        readonly Ent _courier; readonly SpriteRenderer _plantRing; float _courierHit, _throwT = 99f, _courierSpd; Vector2 _lastCourier; V2 _throwDir = V2.Up;
        readonly CargoV[] _cargos = new CargoV[2];
        readonly Ent _pet; float _petHit;
        SpriteRenderer _chest, _chestLock, _chestArc, _deliverRing; Sprite _chestShut, _chestOpen;
        SpriteAnim _client, _corpse; float _waveT;
        /// <summary>Clipe tocado uma vez fora de um ator (o Split do magma): some no fim.</summary>
        sealed class OneShot { public SpriteAnim A; public float Left; }
        readonly List<OneShot> _oneShots = new List<OneShot>();
        readonly List<DoorV> _doors = new List<DoorV>();
        readonly List<Ent> _enemies = new List<Ent>();
        readonly Dictionary<int, float> _enemyHit = new Dictionary<int, float>();
        readonly List<Part> _parts = new List<Part>();
        readonly List<Floater> _floaters = new List<Floater>();

        public WorldView(Camera cam, RectTransform labels)
        {
            _cam = cam; _labels = labels;
            _sCourier = SpriteSheet.Get("courier"); _sCourierFim = SpriteSheet.Get("courier_fim"); _sArco = SpriteSheet.Get("arco");
            _sBau = SpriteSheet.Get("bau"); _sBauAberto = SpriteSheet.Get("bau_aberto"); _sCliente = SpriteSheet.Get("cliente");
            _sOgroFim = SpriteSheet.Get("ogro_fim");
            // placa: sem arco, acima do tampo da parede; com o arco, a cavalo na borda de cima dele (metade sobre a pedra), o que poupa
            // ~0,6 m de altura e deixa os 9,7 m de largura caberem tambem em 16:9 (VIEW_SPRITES §3)
            _plateS = _sArco != null ? Tilt * Balance.RoomH + ArchTop : Tilt * (Balance.RoomH + TopWall) + PlateGap + PlateH / 2f;
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
            SpriteSheet pet = SpriteSheet.Get("filhote");
            if (pet != null) UseSprite(_pet, pet, Balance.PetR);
            BuildCourierFx();
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
            DrawCourierFx(run, dt);
            DrawEnemies(run, dt);   // antes das cargas: a carga roubada vai na ancora do ladrao deste quadro
            DrawCargos(run, dt);
            DrawPet(run, dt);
            DrawShots(run);
            DrawArrows(run);
            DrawPuddles(run);
            DrawRoomState(run, dt);
            DrawRoomV1(run, dt);
            DrawRegiao(run, dt);
            TickParts(dt);
            TickFloaters(dt);
            for (int i = 0; i < _roomLabels.Count; i++) _roomLabels[i].rectTransform.position = _cam.WorldToScreenPoint(_roomLabelPos[i]);
        }

        void DrawCourier(Run run, float dt)
        {
            Courier c = run.Courier;
            Vector2 p = V(c.Pos);
            float step = (p - _lastCourier).magnitude;
            bool moved = step > 1e-3f;
            _lastCourier = p;
            // velocidade real (m/s, inclui o dedo a meio caminho e a parede): a Sim anda em passos de 1/30 s e o quadro e de ~1/60,
            // entao media movel de ~0,1 s; o teto de 20 m/s corta o salto de sala (o ritmo ja tem teto de 2,3x)
            if (dt > 0f) _courierSpd = Mathf.Lerp(_courierSpd, Mathf.Min(step / dt, 20f), 1f - Mathf.Exp(-dt / 0.1f));
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
                bool flash = _courierHit > 0.52f;   // 0,08 s de branco (shader) ou de tinta (sem ele), depois pisca
                // na poca do limo: tinta rosada (com a gosma no pe e as gotas do DrawCourierFx)
                _courier.Spr.color = flash ? Art.FxMat != null ? Color.white : Flash : Mul(blink ? Ghost : c.Slowed ? SlowTint : Color.white, _roupaC);
                Art.Fx(_courier.Spr, flash ? 1f : 0f);
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
            Color body = c.State == CourierState.Dead ? Art.Dim : Mul(c.Slowed ? Color.Lerp(Art.Courier, SlimePink, 0.35f) : Art.Courier, _roupaC);
            _courier.Body.color = _courierHit > 0.52f ? Color.white : blink ? Art.A(body, 0.45f) : body;
        }

        /// <summary>Estado do Core -> clipe do courier: Dying/Cheering (courier_fim) no fim; Dash no dash (esticado nos 0,18 s); Hit ao
        /// levar dano sem carga (com carga so pisca, para a carga nao pular das maos); Throw parado atirando, no compasso do tiro;
        /// CarryRun/Run andando; CarryIdle/Idle parado (CarryIdle tambem antes de pousar e na entrada da sala).</summary>
        void AnimCourier(Run run, Courier c, float dt)
        {
            Ent e = _courier; SpriteAnim a = e.Anim; SpriteSheet s = _sCourier;
            _throwT += dt;
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
            a.Step(dt, _courierSpd);   // Run/CarryRun pela passada (stride_mps 3,56: 4,2 m/s = 1,18x); o resto no fps do meta
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
            float stack = 0f;
            for (int i = 0; i < _cargos.Length; i++)
            {
                CargoV v = _cargos[i];
                Cargo g = i < run.Cargos.Count ? run.Cargos[i] : null;
                bool on = g != null && g.State != CargoState.Gone;
                if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
                if (!on) continue;
                if (!v.Painted || v.Type != g.Type || v.Rescue != g.Rescue) PaintCargo(v, g.Type, g.Rescue);
                bool ground = g.State == CargoState.Ground, spr = v.Sheet != null, fx = spr && Art.FxMat != null;
                float size = At(CargoSize, (int)g.Type, 0.5f);
                // nas maos (courier ou ladrao) pela ancora `hands`; no chao pelo pe, ordenada por y como os atores
                int order;
                Vector2 pos;
                if (g.State == CargoState.Held)
                {
                    pos = Carried(_courier, V(c.Pos), held++, stack, 0.3f, out order); stack += 0.8f * size;
                    if (g.Type == CargoType.Balloon) pos += new Vector2(0f, BalloonLift / Tilt);   // balao: 1 m acima das maos, preso pelo fio
                }
                else if (g.State == CargoState.Stolen)
                {
                    Ent t = null;
                    for (int k = 0; k < run.Enemies.Count && k < _enemies.Count; k++) if (run.Enemies[k].Carry == g) t = _enemies[k];
                    if (t != null) pos = Carried(t, V(g.Pos), 0, 0f, 0.35f, out order);
                    else { pos = V(g.Pos) + new Vector2(0f, 0.35f / Tilt); order = Depth(g.Pos.Y) + 1; }
                }
                else { pos = V(g.Pos); order = Depth(g.Pos.Y); }
                v.Root.localPosition = pos;
                v.G.sortingOrder = order;
                v.HitT = Mathf.Max(0f, v.HitT - dt);
                float pop = v.HitT > 0.18f ? 1f + (v.HitT - 0.18f) / 0.12f * 0.3f : 1f;   // rachou: pop 1,3 -> 1
                // procedural: a forma inteira escala; com sprite (1:1), so o contorno de reserva e as rachaduras, centrados no objeto
                v.Vis.localScale = Vector3.one * ((spr ? size : ground ? 0.62f : 0.5f) * pop);
                v.Vis.localPosition = spr && ground ? (Vector3)v.Sheet.Center : Vector3.zero;
                if (spr)
                {
                    v.Spr.transform.localScale = Vector3.one * pop;
                    v.Spr.transform.localPosition = ground ? Vector3.zero : (Vector3)(-v.Sheet.Center);   // nas maos: centro na ancora
                }
                // ovo nos ultimos 5 s antes de chocar: balanca +-8 graus a 4 Hz; bolo com o dash negado: +-14 graus a 6 Hz morrendo em 0,6 s
                float wob = g.Type == CargoType.Egg && run.EggLeft > 0f && run.EggLeft < 5f ? Mathf.Sin(_time * 25f) * 8f : 0f;
                if (g.Wobble > 0f) wob = Mathf.Sin(_time * 40f) * 14f * (g.Wobble / Balance.CakeWobble);
                if (g.State == CargoState.Held && g.Type == CargoType.Heavy) wob += Mathf.Sin(_time * 14f) * 3f * Mathf.Clamp01(_courierSpd / 2f);   // peso balancando
                v.Vis.localRotation = v.Spr.transform.localRotation = Quaternion.Euler(0f, 0f, wob);
                // rachou: flash branco de 0,06 s (shader) ou a tinta de sempre (procedural branco; sprite sem shader, Flash)
                bool flash = v.HitT > 0.24f;
                Color baseC = spr ? Color.white : Art.CargoColor(g.Type), tint = flash && !fx ? spr ? Flash : Color.white : baseC;
                if (g.Type == CargoType.Relic && g.Cracks == Balance.RelicBreak - 1)   // a proxima quebra: pulsa branco e vermelho
                    tint = Color.Lerp(Color.white, Art.Bad, 0.5f + 0.5f * Mathf.Sin(_time * 10f));
                // barril aceso (Fuse > 0 so depois de armado): pisca vermelho cada vez mais rapido, de 2 a 8 Hz
                float fuse = g.Type == CargoType.Barrel && ground ? g.Fuse : 0f;
                if (fuse > 0f && Mathf.Repeat(_time * (2f + 6f * fuse / Balance.BarrelFuse), 1f) < 0.5f) tint = Color.Lerp(tint, Art.Bad, 0.55f);
                if (spr) v.Spr.color = tint; else v.Body.color = tint;
                v.Cracks.enabled = g.Cracks > 0;
                if (g.Cracks > 0) v.Cracks.sprite = Art.Cracks(g.Cracks);
                float top = v.Vis.localPosition.y + 0.45f * (spr ? size : ground ? 0.62f : 0.5f);   // topo do objeto (m de tela, no EmPe)
                DrawCargoV1(run, v, g, ground, pos, top, dt);
                DrawCargoRegiao(run, v, g, ground, pos, dt);

                // no chao: contorno limao piscando a 2 Hz quando longe (> AloneDist); vermelho pulsando dentro de um telegraph ou com o
                // pavio aceso. Com o shader, segue a silhueta do sprite (CargoLine de largura); sem ele, o retangulo atras da carga
                float far = V2.Dist(g.Pos, c.Pos), pr = run.Sim.PickupRange;   // raio de pegar com a Mochila
                bool alone = ground && far > Balance.AloneDist, danger = ground && (fuse > 0f || InTelegraph(run, g.Pos) || InTeleRegiao(run, g.Pos));
                float a = !ground ? 0f : danger ? 0.6f + 0.4f * Mathf.Sin(_time * 18f) : alone ? (Mathf.Repeat(_time * Blink, 1f) < 0.5f ? 1f : 0.15f) : 0.9f;
                Color line = Art.A(danger ? Art.Bad : Art.Cargo, a);
                v.Outline.color = line;
                v.Outline.enabled = !fx;
                if (fx) Art.Fx(v.Spr, flash ? 1f : 0f, ground ? CargoLine * v.Sheet.Ppu : 0f, line);
                // anel de contagem nos ultimos 3 s antes de nascer o ladrao (do limao ao roxo; com o Vigia o prazo e 4 s maior); caixa
                // pesada sendo pega (+0,2 s): o mesmo anel, limao, enchendo no PickT
                float left = Balance.AloneTime + (run.Has(Perk.Lookout) ? Balance.PerkLookout : 0f) - g.AloneT;
                bool pick = ground && g.PickT > 0f, count = ground && alone && left < 3f;
                v.Count.enabled = pick || count;
                if (pick) { v.Count.sprite = Art.Arc(g.PickT / Mathf.Max(0.05f, (g.Type == CargoType.Heavy ? Balance.HeavyPick : 0f) + g.PickExtra)); v.Count.color = Art.Cargo; }   // + teia/casulo
                else if (count)
                {
                    float k = 1f - left / 3f;
                    v.Count.sprite = Art.Arc(k);
                    v.Count.color = Color.Lerp(Art.Cargo, Art.Role(EnemyType.Thief), k);
                }
                // anel de pegar (sim.PickupRange): expande ao pousar (0,35 s) e fica fraco enquanto a carga esta no chao
                v.PulseT = Mathf.Max(0f, v.PulseT - dt);
                v.Range.enabled = ground && c.State != CourierState.Dead;
                float pk = v.PulseT > 0f ? 1f - v.PulseT / PulseTime : 1f;
                v.Range.transform.localScale = Vector3.one * (2f * pr * pk);
                v.Range.color = Art.A(Art.Cargo, v.PulseT > 0f ? 0.9f * (1f - pk) + 0.2f : far <= pr ? 0.4f : 0.18f);
            }
            DrawPadded(run);
        }

        /// <summary>Carga nas maos de `who` (no pe `at`): na ancora `hands` do quadro, logo acima do portador (frente) ou logo abaixo
        /// (costas), a k-esima empilhada `stack` m de tela acima (0,8 x a altura das de baixo); sem ancora, o lugar procedural de sempre
        /// (`procY` m de tela acima do pe), na frente. Devolve a posicao no espaco do _root.</summary>
        static Vector2 Carried(Ent who, Vector2 at, int k, float stack, float procY, out int order)
        {
            int o = who.G.sortingOrder;
            if (who.Anim != null && who.HasHand)
            {
                order = o + (who.HandFront ? 1 + k : -1 - k);
                return at + new Vector2(who.Hand.x, (who.Hand.y + stack) / Tilt);
            }
            order = o + 1 + k;
            return at + new Vector2(0f, (procY + 0.4f * k) / Tilt);
        }

        /// <summary>A carga no chao em p leva o golpe em preparo? Bruto/Ogro: dentro do circulo do Reach (Run.Strike); arqueiro: na linha
        /// da mira (a flecha acerta a carga no chao). Guarda e Capitao so acertam o courier.</summary>
        static bool InTelegraph(Run run, V2 p)
        {
            foreach (Enemy e in run.Enemies)
            {
                if (!e.Alive || e.State != EnemyState.Telegraph) continue;
                if (e.Type == EnemyType.Archer)
                {
                    float len = AimLen(run.Room, e.Pos, e.Aim);
                    if (V2.SegDist(p, e.Pos, e.Pos + e.Aim * len) <= Balance.CargoR + Balance.ArrowR + 0.1f) return true;
                }
                else if ((e.Type == EnemyType.Brute || e.Type == EnemyType.Ogre) && V2.Dist(e.Pos, p) <= e.Def.Reach) return true;
            }
            return false;
        }

        void DrawPet(Run run, float dt)
        {
            Pet p = run.Pet;
            bool on = p != null && p.Hp > 0;
            if (_pet.Root.gameObject.activeSelf != on) _pet.Root.gameObject.SetActive(on);
            if (!on) return;
            float hop = Mathf.Abs(Mathf.Sin(_time * 9f)), squash = 1f - hop;   // pulinhos de ~0,35 s, squash ao tocar o chao
            Vector2 pp = V(p.Pos);
            _pet.G.sortingOrder = Depth(p.Pos.Y);
            _petHit = Mathf.Max(0f, _petHit - dt);
            if (_pet.Anim != null)
            {
                // folha `filhote` (Static): pulo de PetHop no sprite (a sombra fica no chao), squash pelo pe, flipX pelo movimento
                SpriteAnim a = _pet.Anim;
                a.Play(a.Sheet, a.Sheet.Pick("Static"));
                a.Show(0);
                if (Mathf.Abs(pp.x - _pet.Last.x) > 1e-4f) _pet.FlipX = pp.x > _pet.Last.x;
                _pet.Spr.flipX = _pet.FlipX;
                _pet.Root.localPosition = pp;
                _pet.Spr.transform.localPosition = new Vector3(0f, Lift * PetHop * hop, 0f);
                _pet.Spr.transform.localScale = new Vector3(1f + 0.08f * squash, 1f - 0.06f * squash, 1f);
                float k = _petHit / 0.3f;   // flash que desbota em 0,3 s
                _pet.Spr.color = k > 0.8f && Art.FxMat == null ? Flash : Color.white;
                Art.Fx(_pet.Spr, k);
            }
            else
            {
                _pet.Root.localPosition = pp + new Vector2(0f, 0.08f * hop / Tilt);
                _pet.Up.localScale = UpScale(1f + 0.08f * squash, 1f - 0.06f * squash);
                _pet.Body.color = _petHit > 0f ? Color.Lerp(Art.Pet, Color.white, _petHit / 0.3f) : Art.Pet;
            }
            _pet.Last = pp;
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
                if (v.Type != (int)e.Type) PaintEnemy(v, e.Type, e.Def);
                Vector2 p = V(e.Pos);
                if (v.Id != e.Id)   // lista encolheu (outro inimigo neste lugar) ou inimigo novo
                {
                    v.Id = e.Id; v.Last = p; v.StrikeT = v.RoarT = v.SpawnT = 0f;
                    if (v.Anim != null) v.Anim.Clip = -1;
                    v.Up.localScale = UpScale();
                    if (_seen.Add(e.Id) && e.Wave) SpawnFx(v, e);   // reforco: entra com fumaca e pop
                }
                Vector2 vel = p - v.Last;
                v.Last = p;
                v.StrikeT = Mathf.Max(0f, v.StrikeT - dt);
                v.RoarT = Mathf.Max(0f, v.RoarT - dt);
                if (v.SpawnT > 0f)
                {
                    v.SpawnT = Mathf.Max(0f, v.SpawnT - dt);
                    float k = 1f - v.SpawnT / SpawnTime, sc = 1f + 0.25f * Mathf.Sin(k * Mathf.PI) - 0.7f * (1f - k) * (1f - k);   // 0,3 -> 1,1 -> 1
                    v.Up.localScale = UpScale(sc, sc);
                }
                v.G.sortingOrder = Depth(e.Pos.Y);
                TeleClock(run, v, e);   // relogio proprio do aviso (regioes 2-4)
                EnemyDef d = e.Def;
                float hit = _enemyHit.TryGetValue(e.Id, out float h) ? h : 0f;
                if (hit > 0f) _enemyHit[e.Id] = Mathf.Max(0f, hit - dt);
                bool tele = e.State == EnemyState.Telegraph, warn = tele && e.Tele < 0.15f && Mathf.Repeat(_time * 12f, 1f) < 0.5f;
                if (v.Anim != null)
                {
                    v.Root.localPosition = p;
                    AnimEnemy(v, e, p, vel, cp, dt);
                    // flash branco de 0,06 s no golpe recebido (no maximo 1 a cada HitFlashGap s; no chefe a 60%: o bumerangue acerta varias vezes
                    // por segundo e o chefe virava um vulto branco); tinta vermelha a 30% piscando nos ultimos 0,15 s do telegraph
                    bool flash = hit > HitFlashGap - 0.06f;
                    v.Spr.color = flash && Art.FxMat == null ? Flash : warn ? Color.Lerp(Color.white, Art.Bad, 0.3f) : Color.white;
                    Art.Fx(v.Spr, flash ? d.Boss ? 0.6f : 1f : 0f);
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
                    else if (e.Type == EnemyType.Slime)   // limo: squash e estica a ~1 Hz (mais rapido andando)
                    {
                        float k = Mathf.Sin(_time * (vel.sqrMagnitude > 1e-6f ? 9f : 5f) + e.Phase * 6f);
                        v.Body.transform.localScale = new Vector3(2.2f * d.Radius * (1f + 0.12f * k), 1.8f * d.Radius * (1f - 0.12f * k), 1f);
                    }
                    else if (e.Type == EnemyType.Archer)   // arco virado para a mira (fixa no telegraph) ou para o courier
                    {
                        V2 to = tele || v.StrikeT > 0f ? e.Aim : run.Courier.Pos - e.Pos;
                        float ang = Mathf.Atan2(Tilt * to.Y, to.X) * Mathf.Rad2Deg;
                        v.Extra.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                        v.Extra.transform.localPosition = Quaternion.Euler(0f, 0f, ang) * new Vector3(0.62f * d.Radius, 0f, 0f);
                    }
                    else if (e.Type >= EnemyType.GoblinMiner) AnimProcRegiao(v, e, vel, tele);   // regioes 2-4: squash, flutuar, asas...
                    v.Body.color = hit > HitFlashGap - 0.06f ? Color.white : warn ? Color.Lerp(v.BodyC, Art.Bad, 0.3f) : v.BodyC;
                }
                // telegraph (ART_BIBLE §7): circulo de raio Reach no chao; enchimento a 25% crescendo com TeleProgress; borda a 90%, branca no
                // ultimo 0,1 s. Arqueiro: a mesma regra numa LINHA de mira ate a parede/obstaculo (o circulo de 7 m cobriria a sala)
                // Regioes 2-4: forma propria (circulo no alvo, faixa, cone, linha; o vagonete avisa no trilho) em TeleRegiao
                bool aim = tele && e.Type == EnemyType.Archer;
                if (!TeleRegiao(run, v, e, p, tele))
                {
                    v.Ring.enabled = v.Fill.enabled = tele && !aim;
                    v.AimLine.enabled = v.AimFill.enabled = aim;
                    if (aim) DrawAim(run, v, e);
                    else if (tele)
                    {
                        float r2 = 2f * d.Reach;
                        v.Ring.sprite = Art.ThinRing(); v.Fill.sprite = Art.Disc();
                        v.Ring.transform.localPosition = v.Fill.transform.localPosition = Vector3.zero;
                        v.Ring.transform.localRotation = v.Fill.transform.localRotation = Quaternion.identity;
                        v.Ring.transform.localScale = Vector3.one * r2;
                        v.Fill.transform.localScale = Vector3.one * (r2 * Mathf.Max(0.05f, e.TeleProgress));
                        v.Ring.color = e.Tele < 0.1f ? Color.white : Art.A(Art.Tele, 0.9f);
                        v.Fill.color = Art.A(Art.Tele, 0.25f);
                    }
                }
                // vida: so depois do 1o golpe (sempre em quem tranca a porta); com sprite, acima da cabeca. Maximo com o nivel do contrato
                // (Balance.HpMul): sem ele a barra passava de 100% do C4 em diante
                int max = MaxHp(e, run);
                float headY = v.Anim != null ? Lift * At(EnemyH, (int)e.Type, 1f) * v.Anim.Sheet.Escala : d.Radius;   // ator maior (escala da folha): barra acima da cabeca
                bool bar = e.Hp < max || d.Locks;
                v.HpBg.enabled = v.HpFill.enabled = bar;
                if (bar)
                {
                    float w = Mathf.Max(0.6f, 2.2f * d.Radius), f = (float)e.Hp / max, y = headY + (v.Anim != null ? 0.25f : 0.22f);
                    v.HpBg.transform.localPosition = new Vector3(0f, y, 0f);
                    v.HpBg.transform.localScale = new Vector3(w + 0.06f, 0.14f, 1f);
                    v.HpFill.transform.localPosition = new Vector3(-w * 0.5f + w * f * 0.5f, y, 0f);
                    v.HpFill.transform.localScale = new Vector3(w * f, 0.08f, 1f);
                }
                // atordoado (Bota de ferro): 2 estrelas douradas girando sobre a cabeca
                bool stun = e.Stun > 0f;
                v.StunA.enabled = v.StunB.enabled = stun;
                if (stun)
                {
                    float sa = _time * 9f, sy = headY + 0.12f;
                    v.StunA.transform.localPosition = new Vector3(0.3f * Mathf.Cos(sa), sy + 0.08f * Mathf.Sin(sa), 0f);
                    v.StunB.transform.localPosition = new Vector3(-0.3f * Mathf.Cos(sa), sy - 0.08f * Mathf.Sin(sa), 0f);
                }
                // ladrao e Capitao: anel roxo pegando a carga (0,5 s), saindo pela borda (1 s), e linha roxa de intencao ate a carga no chao
                bool thief = e.Type == EnemyType.Thief || e.Type == EnemyType.Captain;
                float arc = e.State == EnemyState.Grab ? e.GrabT / Balance.ThiefGrab : e.Carry != null && e.ExitT > 0f ? e.ExitT / Balance.ThiefExit : 0f;
                v.Arc.enabled = arc > 0f;
                if (arc > 0f) { v.Arc.sprite = Art.Arc(arc); v.Arc.transform.localScale = Vector3.one * (2f * d.Radius + 0.5f); }
                Cargo target = thief && e.Carry == null && (e.State == EnemyState.Chase || e.State == EnemyState.Grab) ? NearestGround(run, e.Pos) : null;
                v.Line.enabled = target != null;
                if (target != null)
                {
                    Vector2 to = V(target.Pos) - p;
                    v.Line.transform.localPosition = to * 0.5f;
                    v.Line.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
                    v.Line.transform.localScale = new Vector3(to.magnitude, 0.08f, 1f);
                }
                DrawEnemyRegiao(run, v, e, p, vel, headY, dt);   // escuro (olhos e sombra), zZ, ponto fraco, arremesso, agua
            }
        }

        /// <summary>Vida cheia do inimigo no contrato (a mesma conta do Run.Spawn: Def.Hp x Balance.HpMul(Tier)).</summary>
        static int MaxHp(Enemy e, Run run) => Mathf.Max(e.Hp, System.Math.Max(1, (int)System.Math.Round(e.Def.Hp * Balance.HpMul(run.Contract.Tier))));

        /// <summary>Estado do Core -> clipe do inimigo com folha. Morcego: estatico, asa por escala X 1,0 -> 0,65 a 10 Hz, flutua a 1 m
        /// (+-0,06 a 3 Hz) sobre a sombra, flipX pelo movimento. Limo: estatico, squash e estica por codigo, flipX pelo movimento. Ladrao:
        /// Run correndo para a carga, Lurk rondando, Grab pegando (o clipe inteiro nos 0,5 s do Core), CarryRun fugindo com ela. Capitao: o
        /// mesmo do ladrao + golpe (Attack/Slash/Punch/Bash) pelo telegraph e grito (Roar/Call/Shout) ao chamar ladroes. Arqueiro: Idle;
        /// Walk/Run recuando de frente para o courier; tiro (Shoot/Aim/Draw) com [0, soltar) esticado nos 0,8 s da mira e o resto em 0,15 s
        /// depois do ArrowShot. Guarda/Bruto/Ogro: Idle; Walk perseguindo; golpe com os quadros [0, impacto) esticados pelo TeleProgress e
        /// [impacto, fim) em 0,15 s depois do Strike (ART_BIBLE §7); ogro: Roar ao chamar o ladrao. Locomocao no ritmo da passada
        /// (stride_mps) na velocidade do Core. Clipe que a folha nao tem cai no Pick (Idle / 1o clipe), nunca em erro.</summary>
        void AnimEnemy(Ent v, Enemy e, Vector2 p, Vector2 vel, Vector2 cp, float dt)
        {
            SpriteAnim a = v.Anim; SpriteSheet s = a.Sheet;
            Vector2 face = vel;
            int clip, frame = -1;
            bool tele = e.State == EnemyState.Telegraph, captain = e.Type == EnemyType.Captain;
            float impact = At(ImpactFrac, (int)e.Type, 0.6f);
            // m/s do Core (quem anda, anda na velocidade cheia)
            float speed = e.Carry != null ? captain ? Balance.CaptainFlee : Balance.ThiefFlee : e.Def.Speed;
            switch (e.Type)
            {
                case EnemyType.Bat:
                    clip = s.Pick("Static");
                    if (Mathf.Abs(vel.x) > 1e-4f) v.FlipX = vel.x > 0f;
                    float bob = 0.06f * Mathf.Sin(_time * 19f + e.Phase * 6f);
                    v.Up.localPosition = new Vector3(0f, (Lift * BatZ + bob) / Tilt, 0f);
                    v.Spr.transform.localScale = new Vector3(0.825f + 0.175f * Mathf.Sin(_time * 62.83f + e.Phase * 6f), 1f, 1f);
                    break;
                case EnemyType.Slime:
                    clip = s.Pick("Static");
                    if (Mathf.Abs(vel.x) > 1e-4f) v.FlipX = vel.x > 0f;
                    float sq = Mathf.Sin(_time * (vel.sqrMagnitude > 1e-6f ? 9f : 5f) + e.Phase * 6f);   // pivo no pe: estica sem sair do chao
                    v.Spr.transform.localScale = new Vector3(1f + 0.1f * sq, 1f - 0.1f * sq, 1f);
                    break;
                case EnemyType.Thief:
                case EnemyType.Captain:
                    if (e.Carry != null) clip = s.Pick("CarryRun", "Run");
                    else if (e.State == EnemyState.Grab && (clip = s.Clip("Grab")) >= 0)
                        frame = (int)(e.GrabT / Balance.ThiefGrab * s.Frames(clip));   // termina no Hold, que emenda no CarryRun
                    else if (captain && (tele || v.StrikeT > 0f) && (clip = First(s, CaptainHit)) >= 0)
                    {
                        frame = StrikeFrame(s, clip, impact, tele, e.TeleProgress, v.StrikeT);
                        face = cp - p;
                    }
                    else if (captain && v.RoarT > 0f && (clip = First(s, Shout)) >= 0) face = cp - p;
                    else if (e.State == EnemyState.Chase) clip = s.Pick("Run", "Walk");
                    else { clip = s.Pick("Lurk"); if (captain) face = cp - p; }
                    break;
                case EnemyType.Archer:
                    if ((tele || v.StrikeT > 0f) && (clip = First(s, ArcherShoot)) >= 0)
                    {
                        frame = StrikeFrame(s, clip, impact, tele, e.TeleProgress, v.StrikeT);
                        face = V(e.Aim);
                    }
                    else if (e.State == EnemyState.Flee) { clip = s.Pick("Walk", "Run"); face = cp - p; }   // recua de frente para o courier
                    else { clip = s.Pick("Idle"); face = cp - p; }
                    break;
                default:
                    if (AnimRegiao(v, e, ref s, p, vel, cp, tele, impact, ref face, ref speed, out clip, out frame)) break;   // regioes 2-4
                    clip = tele || v.StrikeT > 0f ? s.Clip(At(AttackClip, (int)e.Type, null) ?? "Attack") : -1;
                    if (clip >= 0)
                    {
                        frame = StrikeFrame(s, clip, impact, tele, e.TeleProgress, v.StrikeT);
                        face = cp - p;
                    }
                    else if (v.RoarT > 0f && (clip = s.Clip("Roar")) >= 0) face = cp - p;   // o clipe inteiro, uma vez (OnEvent)
                    else if (e.State == EnemyState.Chase) clip = s.Pick("Walk", "Run");
                    else { clip = s.Pick("Idle"); face = cp - p; }   // parado olha para o courier
                    break;
            }
            a.Play(s, clip);
            a.Step(dt, speed);
            a.Face(face.x, face.y);
            a.Show(frame);
            if (e.Type == EnemyType.Bat || e.Type == EnemyType.Slime || PropRegiao(e.Type)) v.Spr.flipX = v.FlipX;
            v.HasHand = a.Hands(out v.Hand, out v.HandFront);
        }

        /// <summary>Quadro do golpe: [0, impacto) esticados pelo telegraph; depois [impacto, fim) em StrikeHold (ART_BIBLE §7).</summary>
        static int StrikeFrame(SpriteSheet s, int clip, float impact, bool tele, float progress, float strikeT)
        {
            int n = s.Frames(clip), k = Mathf.Max(1, Mathf.Min(n - 1, Mathf.RoundToInt(impact * n)));
            return tele ? Mathf.Min((int)(progress * k), k - 1) : k + (int)((1f - strikeT / StrikeHold) * (n - k));
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
            if (_corpse != null) { _corpse.T += dt; _corpse.Show(); }   // ogro_fim/Dying uma vez, para no ultimo quadro
            for (int i = _oneShots.Count - 1; i >= 0; i--)
            {
                OneShot o = _oneShots[i];
                o.A.T += dt; o.Left -= dt;
                if (o.Left > 0f) { o.A.Show(); continue; }
                Object.Destroy(o.A.R.transform.parent.gameObject);
                _oneShots.RemoveAt(i);
            }
        }

        /// <summary>0 rachaduras Cheering, 1 Thankful, 2+ Shrug; ovo chocado Thankful.</summary>
        static string Reaction(Run run) => run.Hatched ? "Thankful" : run.Main.Cracks <= 0 ? "Cheering" : run.Main.Cracks == 1 ? "Thankful" : "Shrug";

        // ------------------------------------------------------------------ sala (estatico, refeito a cada sala)

        void BuildRoom(Run run, int key)
        {
            _run = run; _roomKey = key;
            Roupa(run);
            for (int i = _roomRoot.childCount - 1; i >= 0; i--) Object.Destroy(_roomRoot.GetChild(i).gameObject);
            foreach (Text t in _roomLabels) Object.Destroy(t.gameObject);
            _roomLabels.Clear(); _roomLabelPos.Clear(); _doors.Clear(); _enemyHit.Clear();
            _chest = _chestLock = _chestArc = _deliverRing = null; _chestShut = _chestOpen = null; _client = _corpse = null; _oneShots.Clear();
            foreach (CargoV v in _cargos) v.PulseT = 0f;
            ClearRoomV1();
            Transform r = _roomRoot;
            Transform up = Upright(r, "EmPe", Vector2.zero);   // placas e arcos (sem SortingGroup: ficam atras dos atores)
            RoomDef room = run.Room;
            float W = Balance.RoomW, H = Balance.RoomH;

            // cenario pela regiao (ART_BIBLE §3 e §12; CONTEUDO_REGIOES §2-§4): piso e parede de fundo texturizados (ou a textura da
            // regiao 1 tingida, ou as lajes procedurais), paredes laterais com tampo, obstaculo com tampo e aresta (ou o prop em pe da
            // regiao: pilar, escora, lapide, sarcofago, bigorna). Sempre sem cor de papel
            _tema = TemaDe(room.Region);
            Tema tm = _tema;
            SpriteRenderer floor = Art.NewSprite(r, "Piso", PisoDe(tm, out Color ft), ft, OFloor, new Vector2(W / 2f, H / 2f), Vector2.one);
            floor.drawMode = SpriteDrawMode.Tiled; floor.size = new Vector2(W, H);
            if (tm.EarthA > 0f) Rect(r, "VeuDeTerra", W / 2f, H / 2f, W, H, Art.A(tm.Earth, tm.EarthA), OFloor);   // minas: apaga as pocas pintadas
            Rect(r, "ParedeEsq", -WallT / 2f, H / 2f, WallT, H + 2f * WallT, tm.WallTop, OWall);
            Rect(r, "ParedeDir", W + WallT / 2f, H / 2f, WallT, H + 2f * WallT, tm.WallTop, OWall);
            Rect(r, "ParedeBaixo", W / 2f, -WallT / 2f, W + 2f * WallT, WallT, tm.WallTop, OWall);
            BuildBackWall(up);
            Rect(r, "Rodape", W / 2f, H + 0.04f, W, 0.08f, tm.Skirt, OWall + 1);
            Rect(r, "Entrada", Balance.Entrance.X, -WallT / 2f, 2f * Balance.DoorHalf, WallT, Art.DoorIn, OWall + 1);
            foreach (Core.Rect o in room.Obstacles)
            {
                Rect(r, "Sombra", o.C.X + 0.1f, o.C.Y - 0.1f, 2f * o.H.X, 2f * o.H.Y, Art.A(Art.Dark, 0.35f), OShadow);
                if (BuildPillar(r, o)) continue;   // obstaculo com prop em pe da regiao (pilar, escora, lapide...) no lugar do bloco
                float top = Mathf.Max(0.1f, 2f * o.H.Y - 0.3f);   // tampo em cima, face de 0,3 m embaixo
                Sprite pedra = ParedeDe(tm, out Color pt);         // a textura da parede da regiao: o bloco e' de pedra, nao chapado
                if (pedra != null)
                {
                    Pedra(r, "Face", pedra, Mul(pt, new Color(0.62f, 0.62f, 0.66f)), o.C.X, o.C.Y, 2f * o.H.X, 2f * o.H.Y, OBlock);
                    Pedra(r, "Tampo", pedra, Mul(pt, new Color(1.18f, 1.18f, 1.2f)), o.C.X, o.C.Y + o.H.Y - top / 2f, 2f * o.H.X, top, OBlock + 1);
                }
                else
                {
                    Rect(r, "Face", o.C.X, o.C.Y, 2f * o.H.X, 2f * o.H.Y, tm.Block, OBlock);
                    Rect(r, "Tampo", o.C.X, o.C.Y + o.H.Y - top / 2f, 2f * o.H.X, top, tm.BlockTop, OBlock + 1);
                }
                Rect(r, "Aresta", o.C.X, o.C.Y + o.H.Y - 0.03f, 2f * o.H.X, 0.06f, tm.BlockEdge, OBlock + 2);
                Rect(r, "Contorno", o.C.X, o.C.Y - o.H.Y + 0.025f, 2f * o.H.X, 0.05f, Art.A(Art.Dark, 0.7f), OBlock + 2);   // pe do bloco
            }
            foreach (Door d in run.Doors) BuildDoor(r, up, d);
            if (run.Chest != null) PaintChest(r, run.Chest.Pos);
            if (room.Delivery) PaintClient(r);
            BuildRoomV1(run, r, up);
            BuildRegiao(run, r, up);   // terreno (agua, trilho, lava, respiro, teia, bau enterrado), tochas da sala escura
            _root.gameObject.SetActive(true);
        }

        /// <summary>Porta (ART_BIBLE §8): vao de 1,6 m com grade de aco, cadeado vermelho do guarda, luz verde aberta; com o sprite `arco`,
        /// o arco de pedra (vao escuro opaco), a grade POR CIMA dele no vao medido e o batente colorido vira soleira no chao; placa de
        /// pedra (a cavalo no arco) com 3 encaixes de caveira (ameaca N de 3), bau com as moedas, bandeira verde da entrega e chifres do chefe.</summary>
        void BuildDoor(Transform r, Transform up, Door d)
        {
            float x = d.Pos.X, w = 2f * Balance.DoorHalf, H = Balance.RoomH;
            bool arch = _sArco != null;
            float gw = arch ? ArchOpenW : w, gh = arch ? ArchOpen / Tilt : TopWall - 0.06f, gy = arch ? H + gh / 2f : H + TopWall / 2f - 0.03f;
            var dv = new DoorV { D = d };
            dv.Glow = Art.NewSprite(r, "Luz", Art.Glow(), Art.A(Art.Good, 0.4f), OMark, new Vector2(x, H - 0.3f), new Vector2(w * 1.6f, 1.6f));
            dv.Frame = arch ? Rect(r, "Soleira", x, H - 0.3f, w + 0.16f, 0.14f, Art.Cargo, ODoor)
                : Rect(r, "Batente", x, H + TopWall / 2f, w + 0.16f, TopWall + 0.02f, Art.Cargo, ODoor);
            if (arch) Art.NewSprite(up, "Arco", _sArco.Frame(_sArco.Pick("Static"), 0, 0), _tema.Arch, ODoor + 1, new Vector2(x, Tilt * H), Vector2.one);   // pedra tingida pela regiao
            else Rect(r, "Vao", x, gy, w, gh, Art.DoorIn, ODoor + 1);
            dv.Bars = Art.NewSprite(r, "Grade", Art.Bars(), Art.Steel, ODoor + 2, new Vector2(x, gy), new Vector2(gw, gh));
            dv.Lock = Art.NewSprite(up, "Cadeado", Art.Lock(), Art.Role(EnemyType.Guard), ODoor + 3, new Vector2(x, Tilt * gy), Vector2.one * 0.38f);
            // mensagem: envelope ao lado do cadeado do Guarda/reforco ("a mensagem abre esta porta"); o cadeado que cai sai daqui
            dv.Env = Art.NewSprite(up, "Envelope", Art.Envelope(), Art.CargoColor(CargoType.Message), ODoor + 3, new Vector2(x + 0.42f, Tilt * gy + 0.05f), new Vector2(0.36f, 0.3f));
            dv.Env.enabled = false;
            dv.LockAt = new Vector2(x, gy); dv.Was = d.Locked;
            _doors.Add(dv);

            RoomDef to = Content.Rooms[d.Room];
            const float icon = 0.36f, gap = 0.04f;
            bool special = to.Delivery || to.Boss || to.Mini;   // chifres tambem no Capitao (M1)
            float pw = 3 * (icon + gap) + 0.16f + (special ? icon + gap : 0f);
            RiskBadge(up, x, pw, d.Risk);   // risco para a carga (CONTRATO_V1 §7), separado das caveiras
            PlateRegiao(up, x, pw, d);      // pa (bau enterrado) e rachadura (parede disfarcada) reveladas pelo Mapa/Faro
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

        /// <summary>Retangulo com textura repetida (Tiled), como o piso: face e tampo dos blocos de pedra.</summary>
        static void Pedra(Transform parent, string name, Sprite tex, Color c, float cx, float cy, float w, float h, int order)
        {
            SpriteRenderer s = Art.NewSprite(parent, name, tex, c, order, new Vector2(cx, cy), Vector2.one);
            s.drawMode = SpriteDrawMode.Tiled; s.size = new Vector2(w, h);
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
            e.AimFill = Art.NewSprite(e.Root, "MiraCheia", Art.Square(), Art.A(Art.Tele, 0.25f), ORing - 1, Vector2.zero, Vector2.one);
            e.AimLine = Art.NewSprite(e.Root, "Mira", Art.Square(), Art.Tele, ORing, Vector2.zero, Vector2.one);
            // em pe
            e.Body = Art.NewSprite(e.Dir, "Corpo", Art.Disc(), Color.white, order, Vector2.zero, Vector2.one);
            e.Deco = Art.NewSprite(e.Dir, "Enfeite", Art.Disc(), Color.white, order + 1, Vector2.zero, Vector2.one);
            e.Extra = Art.NewSprite(e.Dir, "Extra", Art.Square(), Color.white, order + 4, Vector2.zero, Vector2.one);
            e.Eyes = Art.NewSprite(e.Up, "Olhos", Art.Eyes(), Color.white, order + 2, Vector2.zero, Vector2.one);
            e.Pupils = Art.NewSprite(e.Up, "Pupilas", Art.Eyes(), Art.Tinta, order + 3, Vector2.zero, Vector2.one);
            e.HpBg = Art.NewSprite(e.Up, "VidaFundo", Art.Square(), Art.A(Art.Tinta, 0.85f), order + 4, Vector2.zero, Vector2.one);
            e.HpFill = Art.NewSprite(e.Up, "Vida", Art.Square(), Art.Bad, order + 5, Vector2.zero, Vector2.one);
            e.StunA = Art.NewSprite(e.Up, "Tonto", Art.Star(), Art.Gold, order + 6, Vector2.zero, Vector2.one * 0.18f);
            e.StunB = Art.NewSprite(e.Up, "Tonto", Art.Star(), Art.Gold, order + 6, Vector2.zero, Vector2.one * 0.14f);
            e.Spr = Art.NewSprite(e.Up, "Sprite", null, Color.white, order, Vector2.zero, Vector2.one);
            if (Art.FxMat != null) e.Spr.sharedMaterial = Art.FxMat;   // flash branco (Art.Fx)
            e.Ring.enabled = e.Fill.enabled = e.Line.enabled = e.HpBg.enabled = e.HpFill.enabled = e.Arc.enabled = e.Shadow.enabled = e.Spr.enabled = false;
            e.AimLine.enabled = e.AimFill.enabled = e.Extra.enabled = e.StunA.enabled = e.StunB.enabled = false;
            NewEntRegiao(e, order);
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

        /// <summary>Roupa equipada (Sim.OutfitEquipped): a folha `courier_<roupa>` (Hud.FolhaRoupa) quando existe; senao a do courier
        /// com a Hud.TintaRoupa (o mesmo par do retrato na tela de roupas). Troca so ao montar a sala.</summary>
        void Roupa(Run run)
        {
            Outfit o = run.Sim.OutfitEquipped;
            if (o == _roupa && _sCourier != null) return;
            _roupa = o;
            SpriteSheet s = SpriteSheet.Get(Hud.FolhaRoupa(o));
            _roupaC = s != null ? Hud.CorComFolha(o) : Hud.TintaRoupa(o);   // a Fantasma fica translucida tambem com a folha propria
            s = s ?? SpriteSheet.Get("courier");
            if (s != _sCourier) { _sCourier = s; UseSprite(_courier, s, Balance.CourierR); }
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
        /// de pedra; Ogro = disco musgo grande com chifres de osso. v1 (ASSETS §8): Arqueiro = manto ciano-petroleo com caveira de osso
        /// (os olhos sao os furos dela) e o arco virado para a mira; Limo = gota rosa-chiclete com brilho, squash e estica; Capitao = capuz
        /// roxo maior com tricornio dourado. Papel sem desenho proprio (enum novo): disco na cor do papel. Com a folha do papel
        /// (Resources/Sprites/&lt;preset&gt;), o sprite.</summary>
        static void PaintEnemy(Ent v, EnemyType t, EnemyDef def)
        {
            v.Type = (int)t;
            v.Id = -1;
            v.BodyC = Papel(t);
            float r = def.Radius, d = 2f * r;
            UseSprite(v, SheetAt(EnemySheet, (int)t), r);
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
            v.Extra.enabled = false;
            v.Extra.transform.localRotation = Quaternion.identity;
            v.Extra.sortingOrder = v.Body.sortingOrder + 4;   // o Ent e reaproveitado entre papeis: desfaz o que outro papel mudou
            v.Body.transform.localRotation = v.Spr.transform.localRotation = Quaternion.identity;
            v.Dir.localPosition = Vector3.zero;
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
                case EnemyType.Ogre:
                    v.Deco.sprite = Art.Horns(); v.Deco.color = Art.Bone;
                    v.Deco.sortingOrder = v.Body.sortingOrder - 1;
                    v.Deco.transform.localScale = Vector3.one * (d * 1.45f);
                    break;
                case EnemyType.Archer:
                    v.Deco.sprite = Art.Skull(); v.Deco.color = Art.Bone;
                    v.Deco.transform.localPosition = new Vector3(0f, 0.1f * r, 0f); v.Deco.transform.localScale = Vector3.one * (d * 0.72f);
                    v.Eyes.enabled = v.Pupils.enabled = false;   // os furos da caveira mostram o manto ciano: olhos acesos
                    v.Extra.enabled = v.Anim == null;
                    v.Extra.sprite = Art.Arc(0.5f); v.Extra.color = ArcherBow;   // meio anel com a barriga para +x: girado para a mira
                    v.Extra.transform.localScale = Vector3.one * (d * 0.95f);
                    break;
                case EnemyType.Slime:
                    v.Deco.color = Art.A(Color.white, 0.7f);   // brilho de geleia
                    v.Deco.transform.localPosition = new Vector3(-0.32f * r, 0.28f * r, 0f); v.Deco.transform.localScale = Vector3.one * (d * 0.26f);
                    v.Eyes.transform.localScale = Vector3.one * (d * 0.5f);
                    break;
                case EnemyType.Captain:
                    v.Body.sprite = Art.Hood(); v.Body.transform.localScale = Vector3.one * (d * 1.15f);
                    v.Deco.color = Art.ThiefSkin; v.Deco.transform.localPosition = new Vector3(0f, -0.08f * r, 0f); v.Deco.transform.localScale = Vector3.one * (d * 0.6f);
                    v.Extra.enabled = v.Anim == null;
                    v.Extra.sprite = Art.Triangle(); v.Extra.color = Art.Gold;   // tricornio dourado (roxo = roubo, dourado = chefe)
                    v.Extra.transform.localPosition = new Vector3(0f, 0.95f * r, 0f); v.Extra.transform.localScale = new Vector3(d * 1.25f, d * 0.5f, 1f);
                    break;
                default:   // regioes 2-4 (WorldViewRegioes); papel sem desenho: disco na cor do papel, com olhos
                    if (!PaintRegiao(v, t, def)) v.Deco.enabled = false;
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
            // barril aceso: o alcance real da explosao (2,5 m) com o enchimento crescendo no pavio, como um telegraph
            v.BlastFill = Art.NewSprite(v.Root, "ExplosaoCheia", Art.Disc(), Art.A(Art.Bad, 0.16f), ORing - 1, Vector2.zero, Vector2.one);
            v.Blast = Art.NewSprite(v.Root, "Explosao", Art.ThinRing(), Art.Bad, ORing, Vector2.zero, Vector2.one * (2f * Balance.BarrelRadius));
            // em pe: Vis = forma procedural (escala = tamanho da carga) e, com sprite, o contorno e as rachaduras por cima dele
            v.Up = Upright(v.Root, "EmPe", Vector2.zero, out v.G);
            v.Pad = Art.NewSprite(v.Up, "Acolchoada", Art.Glow(), Art.Cream, -1, Vector2.zero, Vector2.one);
            v.Spark = Art.NewSprite(v.Up, "Pavio", Art.Glow(), Flame, 6, Vector2.zero, Vector2.one * 0.3f);
            v.Blast.enabled = v.BlastFill.enabled = v.Pad.enabled = v.Spark.enabled = false;
            NewCargoRegiao(v);
            v.Vis = new GameObject("Forma").transform; v.Vis.SetParent(v.Up, false);
            v.Outline = Art.NewSprite(v.Vis, "Contorno", Art.Rounded(), Art.Cargo, 0, Vector2.zero, Vector2.one * 1.22f);
            v.Body = Art.NewSprite(v.Vis, "Corpo", Art.Rounded(), Color.white, 1, Vector2.zero, Vector2.one);
            v.Spr = Art.NewSprite(v.Up, "Sprite", null, Color.white, 1, Vector2.zero, Vector2.one);
            if (Art.FxMat != null) v.Spr.sharedMaterial = Art.FxMat;   // flash branco e contorno pela silhueta (Art.Fx)
            v.Deco = Art.NewSprite(v.Vis, "Enfeite", Art.Square(), Color.white, 2, Vector2.zero, Vector2.one);
            v.Cracks = Art.NewSprite(v.Vis, "Rachaduras", Art.Cracks(1), Art.Crack, 3, Vector2.zero, Vector2.one * 0.95f);
            v.Seal = Art.NewSprite(v.Vis, "SeloFundo", Art.Disc(), Art.Tinta, 4, new Vector2(0.48f, 0.48f), Vector2.one * 0.62f);
            v.Star = Art.NewSprite(v.Seal.transform, "Selo", Art.Star(), Art.Cream, 5, Vector2.zero, Vector2.one * 0.8f);
            v.Count.enabled = v.Spr.enabled = false;
            v.Root.gameObject.SetActive(false);
            return v;
        }

        /// <summary>Carga (ART_BIBLE §3/§6): caixa de madeira com cinta, reliquia de porcelana com gema, ovo com pintas (escala 1 = tamanho
        /// dela); v1: bolo de 2 andares com coracao, envelope com lacre dourado, caixa pesada com chapa de ferro, barril com caveira pintada.
        /// Resgate: selo creme numa medalha escura (o roxo e so do ladrao). Com a folha (CargoSheet), o sprite, e o contorno segue a
        /// silhueta pelo shader (DrawCargos); sem o shader, o retangulo arredondado atras dele.</summary>
        static void PaintCargo(CargoV v, CargoType t, bool rescue)
        {
            v.Painted = true; v.Type = t; v.Rescue = rescue;
            v.Body.transform.localScale = Vector3.one; v.Deco.transform.localRotation = Quaternion.identity;   // a lanterna/o mapa mudam
            v.Sheet = SheetAt(CargoSheet, (int)t);
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
                case CargoType.Cake:
                    v.Deco.sprite = Art.Heart(); v.Deco.color = CakeHeart;
                    v.Deco.transform.localPosition = new Vector3(0f, 0.3f, 0f); v.Deco.transform.localScale = Vector3.one * 0.22f;   // topo do andar de cima
                    break;
                case CargoType.Message:
                    v.Deco.sprite = Art.Disc(); v.Deco.color = Art.Gold;   // lacre de cera dourado
                    v.Deco.transform.localPosition = new Vector3(0f, 0.05f, 0f); v.Deco.transform.localScale = Vector3.one * 0.3f;
                    break;
                case CargoType.Heavy:
                    v.Deco.sprite = Art.Square(); v.Deco.color = Art.SteelDark;   // chapa de ferro rebitada no meio
                    v.Deco.transform.localPosition = Vector3.zero; v.Deco.transform.localScale = new Vector3(0.92f, 0.22f, 1f);
                    break;
                case CargoType.Barrel:
                    v.Deco.sprite = Art.Skull(); v.Deco.color = Art.Bone;   // caveira pintada (polvora)
                    v.Deco.transform.localPosition = Vector3.zero; v.Deco.transform.localScale = Vector3.one * 0.42f;
                    break;
                case CargoType.Egg:
                    v.Deco.sprite = Art.Disc(); v.Deco.color = Art.EggSpot;
                    v.Deco.transform.localPosition = new Vector3(0.14f, 0.12f, 0f); v.Deco.transform.localScale = Vector3.one * 0.22f;
                    break;
                default:   // regioes 2-4 (WorldViewRegioes); carga nova sem desenho: a forma de reserva do Art.CargoSprite
                    PaintCargoRegiao(v, t);
                    break;
            }
            v.Seal.enabled = v.Star.enabled = rescue;
        }

        /// <summary>Filhote: coral (continua as pintas do ovo), bico dourado e a vida (2) numa barrinha. Com a folha `filhote` (Lote 2), o
        /// sprite no lugar do corpo (UseSprite no construtor; pulo e squash no DrawPet).</summary>
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
            // perk Ima: o raio maior de abrir (2,7 m) tracejado em volta do bau
            _chestRange = Art.NewSprite(r, "Ima", Art.DashedRing(), Art.A(Art.Coin, 0.3f), OMark + 1, p, Vector2.one * (2f * Balance.PerkMagnetRange));
            _chestRange.enabled = false;
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

        /// <summary>Chefe caido (`ogro_fim`, `golem_fim`, `rainha_toupeira_fim`...: Dying, 1 direcao) onde morreu, ordenado pelo pe como os
        /// atores; some com a sala.</summary>
        void Corpse(Vector2 at, SpriteSheet fim, EnemyType t)
        {
            Transform up = Upright(_roomRoot, "ChefeCaido", at, out _);
            ContactShadow(_roomRoot, At(EnemyDef.All, (int)t, EnemyDef.All[(int)EnemyType.Ogre]).Radius).transform.localPosition = at;
            _corpse = new SpriteAnim(fim, Art.NewSprite(up, "Chefe", null, Color.white, 0, Vector2.zero, Vector2.one));
            _corpse.Play(fim, fim.Pick("Dying"));
            _corpse.Show();
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
                case Ev.CargoBroken:   // reliquia, espelho, balao (estoura) ou qualquer carga com o Fragil
                {
                    CargoType bt = Tipo(run, e.A);
                    Float(p, bt == CargoType.Balloon ? Textos.Estourou : Textos.Quebrou, Art.Bad, 56);
                    Burst(p, CargoCor(bt), 14, 4f, 0.18f, 0.6f); Burst(p, Art.Gold, 8, 3f, 0.12f, 0.5f); Shake(0.2f, 0.3f);
                    break;
                }
                case Ev.EnemyHit: if (!_enemyHit.TryGetValue(e.A, out float h0) || h0 <= 0f) _enemyHit[e.A] = HitFlashGap; Burst(p, Color.white, 3, 2f, 0.1f, 0.2f); break;
                case Ev.EnemyDie:
                    Burst(p, Papel((EnemyType)e.B), 12, 3.5f, 0.2f, 0.45f);
                    SpriteSheet fim = e.B == (int)EnemyType.Ogre ? _sOgroFim : FimDe((EnemyType)e.B);   // chefes com folha `<ator>_fim`
                    if (fim != null) Corpse(p, fim, (EnemyType)e.B);
                    break;
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
                case Ev.ThiefSpawn:
                    Burst(p, Art.A(Art.Role(EnemyType.Thief), 0.8f), 10, 2f, 0.22f, 0.5f); Float(p, "!", Art.Role(EnemyType.Thief), 56);
                    // o ogro chama um ladrao a cada 10 s (Run, B1): Roar inteiro. ponytail: o evento nao diz quem fez nascer, entao o
                    // ladrao da carga sozinha com o ogro vivo tambem o faz rugir; separar se o Core ganhar um evento proprio
                    foreach (Ent x in _enemies)
                        if (x.Type == (int)EnemyType.Ogre && x.Anim != null && x.Root.gameObject.activeSelf)
                        {
                            int k = x.Anim.Sheet.Clip("Roar");
                            if (k >= 0) x.RoarT = x.Anim.Sheet.Duration(k);
                        }
                    break;
                case Ev.ThiefEscape: Float(p, Textos.Fugiu, Art.Role(EnemyType.Thief), 56); Burst(p, Art.A(Art.Role(EnemyType.Thief), 0.8f), 14, 2.5f, 0.25f, 0.6f); break;
                case Ev.ChestOpen: Float(p, "+" + e.A, Art.Coin, 52); Burst(p, Art.Coin, 12, 3f, 0.14f, 0.5f); break;
                case Ev.Hatch: Float(p, Textos.Chocou, Art.Pet, 52); Burst(p, Art.CargoColor(CargoType.Egg), 12, 3f, 0.14f, 0.5f); break;
                case Ev.PetHit: _petHit = 0.3f; Float(p, "-1", Art.Bad); break;
                case Ev.Deliver:   // confete na cor da carga (courier em Cheering e cliente reagindo saem do Outcome)
                    Burst(V(run.Courier.Pos), Art.Cargo, 22, 5f, 0.18f, 0.8f);
                    Burst(V(run.Courier.Pos), Art.Good, 14, 4f, 0.14f, 0.7f);
                    break;
            }
            OnEventV1(e, run, p);
            OnEventRegiao(e, run, p);
        }

        // ------------------------------------------------------------------ particulas e numeros flutuantes

        void Burst(Vector2 at, Color c, int n, float speed, float size, float life)
        {
            for (int i = 0; i < n; i++)
                Puff(Art.Disc(), at, Random.insideUnitCircle * speed, c, size * Random.Range(0.8f, 1.3f), size * 0.3f, life * Random.Range(0.7f, 1.2f));
        }

        /// <summary>Uma particula do pool (no chao achatado do _root): sprite, velocidade (amortece), cor (alfa some no fim), escala s0 -> s1
        /// e ordem (decalque do chao = OMark; o resto OPart). Pool cheio: a particula some, nunca trava nem aloca.</summary>
        void Puff(Sprite s, Vector2 at, Vector2 vel, Color c, float s0, float s1, float life, int order = OPart, float rot = 0f, float aspect = 1f)
        {
            Part p = null;
            foreach (Part x in _parts) if (!x.Live) { p = x; break; }
            if (p == null)
            {
                if (_parts.Count >= MaxParts) return;   // ponytail: teto do pool; pico de efeitos perde particulas, nunca trava
                p = new Part { R = Art.NewSprite(_root, "Particula", s, Color.white, order, Vector2.zero, Vector2.one) };
                _parts.Add(p);
            }
            p.Live = true; p.R.enabled = true; p.R.sprite = s; p.R.sortingOrder = order; p.Age = 0f; p.Life = Mathf.Max(0.01f, life);
            p.P = at; p.V = vel; p.C = c; p.S0 = s0; p.S1 = s1; p.K = aspect;   // aspect = altura / largura (feixe, faixa)
            p.R.transform.localPosition = at; p.R.transform.localScale = new Vector3(s0, s0 * aspect, 1f); p.R.color = c;
            p.R.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
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
                float sc = Mathf.Lerp(p.S0, p.S1, k);
                p.R.transform.localScale = new Vector3(sc, sc * p.K, 1f);
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
            // x preso dentro da sala: texto perto da parede nao sai cortado na borda da tela (~1,6 m = meia palavra de 50 px)
            f.Live = true; f.Age = 0f; f.W = new Vector3(Mathf.Clamp(at.x, 1.6f, Balance.RoomW - 1.6f), at.y + 0.5f, 0f);
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
        /// <summary>Ponto da sala (m) subindo `alto` m de tela (o corpo em pe) -> px de tela: os alvos do tour (Hud, Tour.cs).</summary>
        public Vector2 NaTela(V2 sala, float alto = 0f) => ToScreen(new Vector3(sala.X, sala.Y + alto / Tilt, 0f));

        void HideLabels()
        {
            foreach (Floater f in _floaters) { f.Live = false; f.T.enabled = false; }
            foreach (Text t in _roomLabels) Object.Destroy(t.gameObject);
            _roomLabels.Clear(); _roomLabelPos.Clear();
            _roomKey = -1;
            HideLabelsV1();
        }

        static Vector2 V(V2 v) => new Vector2(v.X, v.Y);
        /// <summary>Angulo (graus) para girar algo desenhado apontando para +Y ate a direcao d.</summary>
        static float Angle(V2 d) => Mathf.Atan2(d.Y, d.X) * Mathf.Rad2Deg - 90f;
    }
}
