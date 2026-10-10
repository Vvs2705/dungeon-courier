using System.Collections.Generic;
using DC.Core;
using UnityEngine;
using UnityEngine.UI;
using SortingGroup = UnityEngine.Rendering.SortingGroup;

namespace DC
{
    /// <summary>
    /// Mundo da v1.0-beta (CONTRATO_V1 §3-§7, ASSETS §8, ART_BIBLE §6-§9), parte do WorldView: so le o Core, sem regra nenhuma.
    /// - Inimigos: linha de mira do arqueiro (telegraph 0,8 s), flecha em voo com sombra, poca do limo com o tempo restante, courier
    ///   lento na poca (gosma, gotas, tinta rosada), aviso do Capitao chamando ladroes, "1/2" golpes para soltar a carga, estrelas de
    ///   atordoado, entrada do reforco (fumaca + pop).
    /// - Cargas: alcance (2,5 m) e contagem 3-2-1 piscando do barril aceso, faisca do pavio, explosao (clarao, onda, alcance, chao
    ///   chamuscado, sacudida); "sem dash" sobre o courier quando o bolo balanca; cronometro da mensagem sobre o courier nos ultimos
    ///   30 s; envelope no cadeado que a mensagem abre e a chave subindo; poeira e peso da caixa pesada.
    /// - Armas: estilingue (o de sempre), bumerangue girando (vai e volta), bombinha em arco com sombra e o raio de 1,5 m onde cai.
    /// - Perks: 2 marcas de dash no pe (Dash duplo), recarga do dash, brilho da carga acolchoada que estoura no 1o golpe, raio do Ima.
    /// - Salas: parede de fundo de tijolos (`parede`), piso (`piso`), tochas (`tocha`) com luz ambar, pilares (`pilar`) nos blocos
    ///   quadrados de 1-1,2 m (esmaecem com o courier/carga atras), parede rachada da sala secreta, selo de risco para a carga sob a
    ///   placa da porta, barra de vida do chefe (Ogro/Capitao) no topo.
    /// Sem alocacao por quadro: tudo em pool (particulas, flechas, pocas, tiros) e texto so muda quando o numero muda.
    /// </summary>
    public sealed partial class WorldView
    {
        const int MaxParts = 200;
        // reforco: pop de entrada; pilar de projeto (RENDER_SPRITES §2: --width 1.2); textura: m por repeticao (piso 8 x 8 lajes de 1 m;
        // parede 8 fiadas de 0,32 m); parede de fundo para abaixo do topo do arco; arco da bombinha (m); flecha a 0,8 m do chao
        const float SpawnTime = 0.35f, PilarW = 1.2f, FloorMetres = 8f, WallMetres = 2.56f, BackDrop = 0.3f, BombArc = 1.3f, ArrowZ = 0.8f;
        const float TimerShow = 30f, BossH = 0.26f;

        // cores de papel do Lote 3 (ASSETS §8; o Art.Role ainda tem a proposta antiga para o arqueiro e o limo) e de efeito
        static readonly Color ArcherTeal = Art.Hex(0x1E7A80), ArcherBow = Art.Hex(0x8A5A32), SlimePink = Art.Hex(0xF26DB3), SlimeDark = Art.Hex(0xB8457F),
            CakeHeart = Art.Hex(0xE8577A), Flame = Art.Hex(0xFFB347), TorchLight = Art.Hex(0xFFB45A), Boom = Art.Hex(0xFF8A2B), FlashC = Art.Hex(0xFFF2C0),
            Smoke = Art.Hex(0x3A3F55), BombC = Art.Hex(0x2A2D3A);
        static readonly Color SlowTint = new Color(1f, 0.72f, 0.88f);
        // textura tingida para a paleta (ART_BIBLE §3): media do piso (64,76,81) -> ~#3A3F51 (matiz do #3E4458, um pouco mais escuro);
        // parede (55,68,74) -> ~#353A4A, abaixo do piso para separar os planos. Multiplicar nao clareia: por isso o tampo segue procedural
        static readonly Color FloorTint = new Color(0.90f, 0.83f, 1f), WallTint = new Color(0.97f, 0.85f, 1f);

        // clipes das folhas do Lote 3 (nomes ainda nao fixados pela raia G): o 1o que existir; nenhum = o Pick de sempre
        static readonly string[] ArcherShoot = { "Shoot", "Aim", "Draw", "Attack" }, CaptainHit = { "Attack", "Slash", "Punch", "Bash", "Smash" },
            Shout = { "Roar", "Call", "Shout", "Whistle" };
        static readonly string[] Digits = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        static readonly float[] TorchSlots = { 0.9f, 8.1f, 3.0f, 6.0f, 4.5f, 2.0f, 7.0f };

        sealed class ShotV { public Transform Root, Air; public SpriteRenderer Shadow, Trail, Halo, Core, Icon, Spark, Goal, GoalFill; }
        sealed class ArrowV { public Transform Root, Air; public SpriteRenderer Shadow; }
        sealed class PuddleV { public Transform Root; public SpriteRenderer Fill, Rim, Clock; }
        sealed class TorchV { public SpriteRenderer Halo, Pool; public Transform Flame; public float Seed; }
        /// <summary>Back = borda de tras (y da sala); Reach = ate onde (y da sala) o pilar em pe cobre quem esta atras dele.</summary>
        sealed class PillarV { public SpriteRenderer R; public float X, HalfW, Back, Reach; }

        static Sprite _floorTex, _wallTex;
        static bool _texLoaded;

        readonly HashSet<int> _seen = new HashSet<int>();
        readonly List<ShotV> _shotV = new List<ShotV>();
        readonly List<ArrowV> _arrowV = new List<ArrowV>();
        readonly List<PuddleV> _puddleV = new List<PuddleV>();
        readonly List<TorchV> _torches = new List<TorchV>();
        readonly List<PillarV> _pillars = new List<PillarV>();
        readonly List<SpriteRenderer> _bossParts = new List<SpriteRenderer>();
        readonly float[] _torchX = new float[4];
        readonly SpriteRenderer[] _dashPips = new SpriteRenderer[2];
        SpriteRenderer _slowGoo, _dashArc, _noDash, _noDashBar, _chestRange, _secretCracks, _secretGlow, _bossFill, _bossGhost;
        Text _timerT, _bossName;
        int _timerShown = -1;
        float _bossX0, _bossW, _bossY, _bossGhostF, _dripT, _dustT, _sparkT;
        bool _padWas;

        static string T(string pt, string en) => Textos.En ? en : pt;
        /// <summary>Cor do papel no mundo: Lote 3 pelo ASSETS §8 (arqueiro ciano-petroleo, limo rosa-chiclete); o resto pelo Art.Role.</summary>
        static Color Papel(EnemyType t) => t == EnemyType.Archer ? ArcherTeal : t == EnemyType.Slime ? SlimePink : Art.Role(t);
        /// <summary>1o clipe da lista que a folha tem, ou -1 (sem alocar: foreach em array).</summary>
        static int First(SpriteSheet s, string[] names) { foreach (string n in names) { int k = s.Clip(n); if (k >= 0) return k; } return -1; }
        /// <summary>Angulo de tela (graus, desenho apontando para +Y) da direcao d da Sim: o y encolhe pela projecao.</summary>
        static float ScreenAngle(V2 d) => Mathf.Atan2(Tilt * d.Y, d.X) * Mathf.Rad2Deg - 90f;
        float CourierHeadY => _courier.Anim != null ? Lift * 1.3f : 0.45f;

        Text NewLabel(int size)
        {
            Text t = Art.Outlined(Art.NewText(_labels, "Rotulo", size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)), 3f);
            t.rectTransform.sizeDelta = new Vector2(400f, 90f); t.horizontalOverflow = HorizontalWrapMode.Overflow; t.fontStyle = FontStyle.Bold;
            t.enabled = false;
            return t;
        }

        // ------------------------------------------------------------------ courier: poca, peso, dash, bolo, cronometro

        void BuildCourierFx()
        {
            Transform cr = _courier.Root;   // no chao (achatado)
            _slowGoo = Art.NewSprite(cr, "Gosma", Art.Disc(), Art.A(SlimePink, 0.55f), ORing - 1, Vector2.zero, Vector2.one);
            _dashArc = Art.NewSprite(cr, "RecargaDash", Art.Arc(0f), Art.A(Art.Shot, 0.65f), ORing, Vector2.zero, Vector2.one);
            for (int i = 0; i < 2; i++)
                _dashPips[i] = Art.NewSprite(cr, "CargaDash", Art.Disc(), Art.Shot, ORing + 1, new Vector2(i == 0 ? -0.62f : 0.62f, -0.12f), Vector2.one * 0.16f);
            // "sem dash": as setas do dash riscadas de vermelho, em pe sobre a cabeca
            _noDash = Art.NewSprite(_courier.Up, "SemDash", Art.Chevrons(), Art.Ink, OCourier + 10, Vector2.zero, Vector2.one * 0.42f);
            _noDashBar = Art.NewSprite(_noDash.transform, "Risco", Art.Square(), Art.Bad, OCourier + 11, Vector2.zero, new Vector2(1.2f, 0.17f));
            _noDashBar.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _slowGoo.enabled = _dashArc.enabled = _dashPips[0].enabled = _dashPips[1].enabled = _noDash.enabled = _noDashBar.enabled = false;
        }

        void DrawCourierFx(Run run, float dt)
        {
            Courier c = run.Courier;
            bool alive = c.State != CourierState.Dead && run.Running;
            Vector2 feet = V(c.Pos);
            // poca do limo (-40%): gosma rosa no pe e gotas (a tinta rosada do sprite sai do DrawCourier)
            bool slow = alive && c.Slowed;
            _slowGoo.enabled = slow;
            if (slow)
            {
                float k = Mathf.Sin(_time * 9f);
                _slowGoo.transform.localScale = new Vector3(1.15f + 0.08f * k, 0.8f - 0.06f * k, 1f);
                if ((_dripT -= dt) <= 0f)
                {
                    _dripT = 0.1f;
                    Puff(Art.Disc(), feet + new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(-0.1f, 0.1f)), new Vector2(0f, 1.2f), Art.A(SlimePink, 0.9f), 0.12f, 0.04f, 0.45f);
                }
            }
            // caixa pesada (-25%): poeira no pe andando e o corpo achatado pelo peso
            bool heavy = alive && run.Holds(CargoType.Heavy);
            if (heavy && _courierSpd > 0.5f && (_dustT -= dt) <= 0f)
            {
                _dustT = 0.18f;
                Puff(Art.Disc(), feet + new Vector2(Random.Range(-0.3f, 0.3f), -0.15f), Random.insideUnitCircle * 0.6f, Art.A(Art.Dim, 0.6f), 0.18f, 0.34f, 0.5f);
            }
            if (_courier.Anim != null) _courier.Spr.transform.localScale = heavy ? new Vector3(1.05f, 0.95f + 0.02f * Mathf.Sin(_time * 14f), 1f) : Vector3.one;
            // dash: anel azul de recarga no pe (ART_BIBLE §9) e, com o Dash duplo, as 2 marcas (acesa = carga pronta)
            bool rech = alive && c.DashCharges < c.DashChargesMax && c.DashCdMax > 0f;
            _dashArc.enabled = rech;
            if (rech) _dashArc.sprite = Art.Arc(1f - c.DashCd / c.DashCdMax);
            bool two = alive && c.DashChargesMax > 1;
            for (int i = 0; i < _dashPips.Length; i++)
            {
                _dashPips[i].enabled = two;
                if (two) _dashPips[i].color = i < c.DashCharges ? Art.Shot : Art.A(Art.Off, 0.8f);
            }
            // bolo: o toque rapido negado mostra o "sem dash" enquanto ele balanca (a frase vai na dica da HUD)
            float wob = 0f;
            foreach (Cargo g in run.Cargos) if (g.State == CargoState.Held && g.Wobble > wob) wob = g.Wobble;
            bool nd = alive && wob > 0f;
            _noDash.enabled = _noDashBar.enabled = nd;
            if (nd)
            {
                float k = wob / Balance.CakeWobble;
                _noDash.transform.localPosition = new Vector3(0f, CourierHeadY + 0.35f, 0f);
                _noDash.transform.localScale = Vector3.one * (0.42f * (1f + 0.4f * k * k));
            }
            // mensagem: segundos restantes sobre o courier nos ultimos 30 s (vermelho e pulsando nos 10 finais; o relogio fica na HUD)
            bool tm = alive && run.HasTimer && run.TimeLeft <= TimerShow;
            if (tm && _timerT == null) _timerT = NewLabel(52);
            if (_timerT == null) return;
            if (_timerT.enabled != tm) _timerT.enabled = tm;
            if (!tm) return;
            int sec = Mathf.CeilToInt(run.TimeLeft);
            if (sec != _timerShown) { _timerShown = sec; _timerT.text = Textos.Segundos(sec); }
            bool hot = sec <= 10;
            _timerT.color = hot ? Art.Bad : Art.Ink;
            _timerT.rectTransform.localScale = Vector3.one * (hot ? 1f + 0.18f * Mathf.Abs(Mathf.Sin(_time * Mathf.PI * 2f)) : 1f);
            _timerT.rectTransform.position = ToScreen(new Vector3(feet.x, feet.y + (CourierHeadY + 0.45f) / Tilt, 0f));
        }

        // ------------------------------------------------------------------ cargas: barril aceso, carga acolchoada

        /// <summary>Barril no chao com o pavio aceso (Fuse > 0): alcance real de 2,5 m enchendo como telegraph, faisca e contagem 3-2-1
        /// piscando cada vez mais rapido sobre ele. `pos` = pe da carga (m da sala); `top` = topo do objeto (m de tela).</summary>
        void DrawCargoV1(Run run, CargoV v, Cargo g, bool ground, Vector2 pos, float top, float dt)
        {
            float f = g.Type == CargoType.Barrel && ground ? g.Fuse : 0f;
            bool lit = f > 0f && run.Running;
            v.Blast.enabled = v.BlastFill.enabled = v.Spark.enabled = lit;
            if (!lit)
            {
                if (v.Num != null && v.Num.enabled) v.Num.enabled = false;
                v.NumShown = -1;
                return;
            }
            float k = Mathf.Clamp01(f / Balance.BarrelFuse);
            v.Blast.color = Art.A(Art.Bad, 0.6f + 0.35f * Mathf.Sin(_time * (8f + 16f * k)));
            v.BlastFill.transform.localScale = Vector3.one * (2f * Balance.BarrelRadius * Mathf.Max(0.05f, k));
            v.Spark.transform.localPosition = new Vector3(0.06f, top + 0.06f, 0f);
            v.Spark.transform.localScale = Vector3.one * (0.28f + 0.14f * Mathf.PerlinNoise(_time * 25f, g.Id));
            if ((_sparkT -= dt) <= 0f)
            {
                _sparkT = 0.05f;
                Puff(Art.Disc(), pos + new Vector2(0.06f, (top + 0.06f) / Tilt), Random.insideUnitCircle * 1.6f + new Vector2(0f, 0.8f), Random.value < 0.5f ? Flame : Art.Gold, 0.07f, 0.02f, 0.3f);
            }
            int n = Mathf.Clamp(Mathf.CeilToInt(Balance.BarrelFuse - f - 1e-3f), 0, Digits.Length - 1);
            if (v.Num == null) v.Num = NewLabel(72);
            if (n != v.NumShown) { v.NumShown = n; v.Num.text = Digits[n]; v.NumPop = 0.25f; }
            v.NumPop = Mathf.Max(0f, v.NumPop - dt);
            v.Num.enabled = Mathf.Repeat(_time * (2f + 6f * k), 1f) < 0.72f;
            v.Num.color = n <= 1 ? Art.Bad : Color.Lerp(Art.Ink, Art.Bad, k);
            v.Num.rectTransform.localScale = Vector3.one * (1f + 2.4f * v.NumPop);
            v.Num.rectTransform.position = ToScreen(new Vector3(pos.x, pos.y + (top + 0.55f) / Tilt, 0f));
        }

        /// <summary>Carga acolchoada: brilho creme pulsando em toda carga ate o 1o golpe ser absorvido (o Core nao emite evento: a View
        /// ve o PadUsed virar e estoura a bolha).</summary>
        void DrawPadded(Run run)
        {
            bool pad = run.Has(Perk.Padded) && !run.PadUsed;
            for (int i = 0; i < _cargos.Length; i++)
            {
                CargoV v = _cargos[i];
                bool on = pad && v.Root.gameObject.activeSelf;
                v.Pad.enabled = on;
                if (!on) continue;
                float s = Mathf.Sin(_time * 4f);
                v.Pad.transform.localPosition = v.Vis.localPosition;
                v.Pad.transform.localScale = Vector3.one * (1.25f + 0.1f * s);
                v.Pad.color = Art.A(Art.Cream, 0.4f + 0.15f * s);
            }
            if (_padWas && !pad && run.Has(Perk.Padded))
            {
                Cargo m = run.Main;
                Vector2 at = V(m.State == CargoState.Held ? run.Courier.Pos : m.Pos) + new Vector2(0f, 0.4f / Tilt);
                Puff(Art.ThinRing(), at, Vector2.zero, Art.A(Art.Cream, 0.9f), 0.5f, 1.8f, 0.35f);
                Burst(at, Art.Cream, 10, 3f, 0.1f, 0.35f);
            }
            _padWas = pad;
        }

        // ------------------------------------------------------------------ inimigos: mira do arqueiro, entrada do reforco

        /// <summary>Linha de mira do arqueiro no chao (ART_BIBLE §7 numa linha): borda fina a 90% (branca no ultimo 0,1 s) ate onde a flecha
        /// para, e o enchimento largo a 25% crescendo com o TeleProgress.</summary>
        void DrawAim(Run run, Ent v, Enemy e)
        {
            float r0 = e.Def.Radius, len = Mathf.Max(r0 + 0.1f, AimLen(run.Room, e.Pos, e.Aim)), full = len - r0;
            Vector2 dir = V(e.Aim);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Line(v.AimLine, dir, r0, full, 0.06f, ang);
            Line(v.AimFill, dir, r0, full * Mathf.Max(0.05f, e.TeleProgress), 0.26f, ang);
            v.AimLine.color = e.Tele < 0.1f ? Color.white : Art.A(Art.Tele, 0.9f);
        }

        static void Line(SpriteRenderer r, Vector2 dir, float from, float len, float w, float ang)
        {
            r.transform.localPosition = dir * (from + len * 0.5f);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            r.transform.localScale = new Vector3(len, w, 1f);
        }

        /// <summary>Distancia (m) ate a flecha parar: obstaculo, parede ou Balance.ArrowRange (passo de 0,2 m, como o Run.UpdateArrows).</summary>
        static float AimLen(RoomDef room, V2 from, V2 dir)
        {
            const float step = 0.2f;
            for (float t = step; t < Balance.ArrowRange; t += step)
            {
                V2 p = from + dir * t;
                if (p.X < 0f || p.Y < 0f || p.X > Balance.RoomW || p.Y > Balance.RoomH || Run.Blocked(room, p, Balance.ArrowR)) return t;
            }
            return Balance.ArrowRange;
        }

        /// <summary>Inimigo do reforco: nasce pequeno e cresce (0,35 s) numa fumaca com anel na cor do papel.</summary>
        void SpawnFx(Ent v, Enemy e)
        {
            v.SpawnT = SpawnTime;
            v.Up.localScale = UpScale(0.3f, 0.3f);
            Vector2 p = V(e.Pos);
            Puff(Art.ThinRing(), p, Vector2.zero, Art.A(Papel(e.Type), 0.9f), 0.3f, 2.2f * e.Def.Radius + 0.8f, 0.35f, ORing);
            for (int i = 0; i < 7; i++) Puff(Art.Disc(), p, Random.insideUnitCircle * 1.6f, Art.A(Smoke, 0.85f), 0.25f, 0.5f, 0.5f);
            Burst(p, Papel(e.Type), 5, 2.5f, 0.1f, 0.35f);
        }

        // ------------------------------------------------------------------ projeteis: armas do courier, flechas, pocas

        ShotV NewShot()
        {
            var v = new ShotV { Root = new GameObject("Tiro").transform };
            v.Root.SetParent(_root, false);
            v.Shadow = Art.NewSprite(v.Root, "Sombra", Art.Disc(), Art.A(Art.Dark, 0.35f), OActShadow, Vector2.zero, new Vector2(0.24f, 0.14f));
            v.Air = Upright(v.Root, "Ar", Vector2.zero);
            // estilingue (ART_BIBLE §9): nucleo branco com halo azul de 0,25 m e rastro de 0,4 m
            v.Trail = Art.NewSprite(v.Air, "Rastro", Art.Glow(), Art.A(Art.Shot, 0.7f), OShot, new Vector2(0f, -0.2f), new Vector2(0.22f, 0.5f));
            v.Halo = Art.NewSprite(v.Air, "Halo", Art.Glow(), Art.Shot, OShot + 1, Vector2.zero, Vector2.one * 0.4f);
            v.Core = Art.NewSprite(v.Air, "Nucleo", Art.Disc(), Color.white, OShot + 2, Vector2.zero, Vector2.one * 0.14f);
            // bumerangue / bombinha (o pavio vai preso a bomba: gira junto)
            v.Icon = Art.NewSprite(v.Air, "Arma", Art.Boomerang(), Art.Shot, OShot + 3, Vector2.zero, Vector2.one * 0.42f);
            v.Spark = Art.NewSprite(v.Icon.transform, "Pavio", Art.Glow(), Flame, OShot + 4, new Vector2(0.33f, 0.43f), Vector2.one * 0.6f);
            // onde a bombinha cai: o raio (1,5 m) no chao
            v.GoalFill = Art.NewSprite(_root, "AlvoBomba", Art.Disc(), Art.A(Art.Bad, 0.15f), ORing - 1, Vector2.zero, Vector2.one);
            v.Goal = Art.NewSprite(_root, "AlvoBombaBorda", Art.ThinRing(), Art.Bad, ORing, Vector2.zero, Vector2.one);
            return v;
        }

        /// <summary>Tiros do courier pela arma (Shot.Weapon): estilingue = nucleo azul voando para o alvo; bumerangue = V azul girando 3
        /// voltas/s, rastro para tras da direcao (vai em Dir, volta ao courier); bombinha = bola escura de borda clara num arco de 1,3 m
        /// (Progress), sombra crescendo no chao e o raio de dano onde ela cai. Arma nova sem desenho: o estilingue.</summary>
        void DrawShots(Run run)
        {
            List<Shot> list = run.Shots;
            while (_shotV.Count < list.Count) _shotV.Add(NewShot());
            bool spr = _courier.Anim != null;   // com sprite, voa na altura do peito (0,9 m); procedural, rente ao chao
            for (int i = 0; i < _shotV.Count; i++)
            {
                ShotV v = _shotV[i];
                Shot s = i < list.Count ? list[i] : null;
                bool on = s != null;
                if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
                bool bomb = on && s.Weapon == Weapon.Bomb, boom = on && s.Weapon == Weapon.Boomerang, sling = on && !bomb && !boom;
                v.Goal.enabled = v.GoalFill.enabled = bomb;
                if (!on) continue;
                v.Root.localPosition = V(s.Pos);
                float z = spr ? ShotZ : 0f;
                v.Core.enabled = sling; v.Trail.enabled = !bomb; v.Icon.enabled = !sling; v.Spark.enabled = bomb; v.Shadow.enabled = spr || bomb;
                v.Shadow.transform.localScale = new Vector3(0.24f, 0.14f, 1f);
                if (sling)
                {
                    v.Air.localRotation = Quaternion.Euler(0f, 0f, ScreenAngle(s.Target != null ? s.Target.Pos - s.Pos : V2.Up));
                    v.Halo.color = Art.Shot; v.Halo.transform.localScale = Vector3.one * 0.4f;
                }
                else if (boom)
                {
                    v.Air.localRotation = Quaternion.Euler(0f, 0f, ScreenAngle(s.Returning ? run.Courier.Pos - s.Pos : s.Dir));
                    v.Icon.sprite = Art.Boomerang(); v.Icon.color = Art.Shot;
                    v.Icon.transform.localScale = Vector3.one * 0.42f;
                    v.Icon.transform.localRotation = Quaternion.Euler(0f, 0f, -_time * 1080f);
                    v.Halo.color = Art.A(Color.white, 0.35f); v.Halo.transform.localScale = Vector3.one * 0.55f;
                }
                else
                {
                    float k = s.Progress;
                    z = (spr ? ShotZ : 0.3f) * (1f - k) + 4f * BombArc * k * (1f - k);
                    v.Air.localRotation = Quaternion.identity;
                    v.Icon.sprite = Art.Bomb(); v.Icon.color = BombC;
                    v.Icon.transform.localScale = Vector3.one * 0.36f;
                    v.Icon.transform.localRotation = Quaternion.Euler(0f, 0f, -k * 300f);   // tomba no ar
                    v.Halo.color = Art.A(Color.white, 0.5f); v.Halo.transform.localScale = Vector3.one * 0.5f;   // borda clara: bomba escura no piso escuro
                    v.Spark.transform.localScale = Vector3.one * (0.5f + 0.25f * Mathf.PerlinNoise(_time * 30f, i));
                    v.Shadow.transform.localScale = new Vector3(0.2f + 0.16f * k, 0.12f + 0.1f * k, 1f);
                    float r2 = 2f * WeaponDef.All[(int)Weapon.Bomb].Radius;   // racha o ovo e detona o barril: vermelho
                    Vector2 goal = V(s.Goal);
                    v.Goal.transform.localPosition = v.GoalFill.transform.localPosition = goal;
                    v.Goal.transform.localScale = Vector3.one * r2;
                    v.GoalFill.transform.localScale = Vector3.one * (r2 * Mathf.Max(0.05f, k));
                    v.Goal.color = Art.A(Art.Bad, 0.35f + 0.55f * k);
                }
                v.Air.localPosition = new Vector3(0f, Lift * z / Tilt, 0f);
            }
        }

        ArrowV NewArrow()
        {
            var v = new ArrowV { Root = new GameObject("Flecha").transform };
            v.Root.SetParent(_root, false);
            v.Shadow = Art.NewSprite(v.Root, "Sombra", Art.Disc(), Art.A(Art.Dark, 0.4f), OActShadow, Vector2.zero, new Vector2(0.6f, 0.1f));
            v.Air = Upright(v.Root, "Ar", Vector2.zero);
            // apontando para +x: aura vermelha (perigo), haste de osso, ponta de aco e pena ciano-petroleo (a cor do arqueiro)
            Art.NewSprite(v.Air, "Perigo", Art.Glow(), Art.A(Art.Bad, 0.45f), OShot, new Vector2(-0.08f, 0f), new Vector2(0.9f, 0.34f));
            Art.NewSprite(v.Air, "Haste", Art.Square(), Art.Bone, OShot + 1, new Vector2(-0.1f, 0f), new Vector2(0.5f, 0.055f));
            Art.NewSprite(v.Air, "Ponta", Art.Triangle(), Art.Steel, OShot + 2, new Vector2(0.19f, 0f), Vector2.one * 0.17f).transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            Art.NewSprite(v.Air, "Pena", Art.Triangle(), ArcherTeal, OShot + 2, new Vector2(-0.33f, 0f), new Vector2(0.17f, 0.15f)).transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            return v;
        }

        /// <summary>Flechas em voo (Run.Arrows): a 0,8 m do chao (com sprites) com a sombra fina no chao, girada pela Dir projetada.</summary>
        void DrawArrows(Run run)
        {
            List<Arrow> list = run.Arrows;
            while (_arrowV.Count < list.Count) _arrowV.Add(NewArrow());
            float h = Lift * (_courier.Anim != null ? ArrowZ : 0.3f) / Tilt;
            for (int i = 0; i < _arrowV.Count; i++)
            {
                ArrowV v = _arrowV[i];
                bool on = i < list.Count;
                if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
                if (!on) continue;
                Arrow a = list[i];
                v.Root.localPosition = V(a.Pos);
                v.Air.localPosition = new Vector3(0f, h, 0f);
                v.Air.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(Tilt * a.Dir.Y, a.Dir.X) * Mathf.Rad2Deg);
                v.Shadow.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(a.Dir.Y, a.Dir.X) * Mathf.Rad2Deg);
            }
        }

        PuddleV NewPuddle()
        {
            var v = new PuddleV { Root = new GameObject("Poca").transform };
            v.Root.SetParent(_root, false);
            v.Fill = Art.NewSprite(v.Root, "Poca", Art.Disc(), SlimePink, OMark, Vector2.zero, Vector2.one);
            v.Rim = Art.NewSprite(v.Root, "Borda", Art.ThinRing(), SlimeDark, OShadow, Vector2.zero, Vector2.one);
            v.Clock = Art.NewSprite(v.Root, "Tempo", Art.Arc(1f), Art.Ink, OShadow, Vector2.zero, Vector2.one);
            return v;
        }

        /// <summary>Pocas do limo (Run.Puddles): rosa no chao no raio real, borda escura, anel do tempo restante (Left / 4 s) no meio;
        /// pisca no ultimo 1 s e some nos ultimos 0,5 s.</summary>
        void DrawPuddles(Run run)
        {
            List<Puddle> list = run.Puddles;
            while (_puddleV.Count < list.Count) _puddleV.Add(NewPuddle());
            for (int i = 0; i < _puddleV.Count; i++)
            {
                PuddleV v = _puddleV[i];
                bool on = i < list.Count;
                if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
                if (!on) continue;
                Puddle p = list[i];
                float d = 2f * p.Radius, wob = 1f + 0.035f * Mathf.Sin(_time * 4f + i * 1.7f), fade = Mathf.Clamp01(p.Left / 0.5f);
                bool end = p.Left < 1f && Mathf.Repeat(_time * 6f, 1f) < 0.5f;
                v.Root.localPosition = V(p.Pos);
                v.Fill.transform.localScale = new Vector3(d * wob, d / wob, 1f);
                v.Rim.transform.localScale = Vector3.one * d;
                v.Clock.transform.localScale = Vector3.one * (d * 0.55f);
                v.Clock.sprite = Art.Arc(p.Left / Balance.PuddleTime);
                v.Fill.color = Art.A(SlimePink, (end ? 0.25f : 0.45f) * fade);
                v.Rim.color = Art.A(SlimeDark, 0.85f * fade);
                v.Clock.color = Art.A(Art.Ink, 0.8f * fade);
            }
        }

        // ------------------------------------------------------------------ sala: textura, parede de fundo, tochas, pilares, segredo, chefe

        static void LoadTextures()
        {
            if (_texLoaded) return;
            _texLoaded = true;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;   // -nographics: procedural
            _floorTex = Tiling("Textures/piso", FloorMetres);
            _wallTex = Tiling("Textures/parede", WallMetres);
        }

        /// <summary>Sprite inteiro da textura de Resources para SpriteRenderer Tiled (FullRect), `metres` m por repeticao. ponytail: o
        /// import e o padrao do projeto 3D (Default, Repeat, com mipmap); sem a textura, null e o procedural de sempre.</summary>
        static Sprite Tiling(string path, float metres)
        {
            var t = Resources.Load<Texture2D>(path);
            return t == null ? null : Sprite.Create(t, new UnityEngine.Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.width / metres, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Altura da parede de fundo (m de tela): com o arco, sobe ate 0,3 m abaixo do topo dele (os arcos coroam a parede e o
        /// fundo deixa de ser vazio); sem o arco, a faixa de 0,5 m de sempre.</summary>
        float BackWallH => _sArco != null ? ArchTop - BackDrop : Tilt * TopWall;

        void BuildBackWall(Transform up)
        {
            float W = Balance.RoomW, y0 = Tilt * Balance.RoomH, h = BackWallH, w = W + 2f * WallT;
            bool tex = _wallTex != null;
            // abaixo das paredes laterais (OWall): elas cobrem os cantos de baixo da parede de fundo
            SpriteRenderer wall = Art.NewSprite(up, "ParedeFundo", tex ? _wallTex : Art.Square(), tex ? WallTint : Art.Wall, OWall - 1, new Vector2(W / 2f, y0 + h / 2f), tex ? Vector2.one : new Vector2(w, h));
            if (tex) { wall.drawMode = SpriteDrawMode.Tiled; wall.size = new Vector2(w, h); }
            Rect(up, "TampoFundo", W / 2f, y0 + h - 0.05f, w, 0.1f, Art.WallTop, OWall + 1);
            Rect(up, "SombraFundo", W / 2f, y0 + 0.08f, W, 0.16f, Art.A(Art.Dark, 0.3f), OWall + 1);
        }

        /// <summary>Bloco quadrado de 1-1,2 m com a folha `pilar`: o pilar em pe (escala lado / 1,2 m), ordenado pelo pe como os atores.</summary>
        bool BuildPillar(Transform r, Core.Rect o)
        {
            SpriteSheet s = SpriteSheet.Get("pilar");
            float side = 2f * o.H.X;
            if (s == null || Mathf.Abs(o.H.X - o.H.Y) > 0.01f || side < 0.95f || side > 1.25f) return false;
            float k = side / PilarW;
            Transform pu = Upright(r, "Pilar", new Vector2(o.C.X, o.C.Y), out SortingGroup _);
            _pillars.Add(new PillarV
            {
                R = Art.NewSprite(pu, "Pilar", s.Frame(s.Pick("Static"), 0, 0), Color.white, 0, Vector2.zero, Vector2.one * k),
                X = o.C.X, HalfW = o.H.X, Back = o.C.Y + o.H.Y, Reach = o.C.Y + 2f * s.Center.y * k / Tilt,   // topo ~ 2x o centro da celula
            });
            return true;
        }

        void ClearRoomV1()
        {
            _seen.Clear(); _torches.Clear(); _pillars.Clear(); _bossParts.Clear();
            _bossFill = _bossGhost = _chestRange = _secretCracks = _secretGlow = null;
            _bossName = null;   // destruido junto com os _roomLabels
        }

        void BuildRoomV1(Run run, Transform r, Transform up)
        {
            BuildTorches(run, r, up);
            if (run.Secret != null) BuildSecret(r, run.Secret);
            if (run.Room.Boss || run.Room.Mini) BuildBossBar(up, run.Room);
        }

        /// <summary>Ate 4 tochas na parede de fundo, longe dos arcos (1,45 m) e entre si (1,8 m); na sala do chefe, so a direita (a barra
        /// de vida fica a esquerda). Folha `tocha` ou suporte de ferro com chama procedural; brilho ambar e poca de luz no chao (ART_BIBLE
        /// §3: #FFB45A a 18-25%) tremulando no DrawRoomV1, abaixo dos atores.</summary>
        void BuildTorches(Run run, Transform r, Transform up)
        {
            SpriteSheet s = SpriteSheet.Get("tocha");
            bool arch = _sArco != null, boss = run.Room.Boss || run.Room.Mini;
            float clear = arch ? 1.45f : 1.0f, y0 = Tilt * Balance.RoomH + (arch ? 0.55f : 0.05f);
            int n = 0;
            foreach (float x in TorchSlots)
            {
                if (n >= _torchX.Length) break;
                if (boss && x < 3.4f) continue;
                bool ok = true;
                foreach (Door d in run.Doors) if (Mathf.Abs(d.Pos.X - x) < clear) ok = false;
                for (int k = 0; k < n; k++) if (Mathf.Abs(_torchX[k] - x) < 1.8f) ok = false;
                if (!ok) continue;
                _torchX[n++] = x;
                var t = new TorchV { Seed = n * 3.7f };
                float fy;   // chama (m de tela)
                if (s != null)
                {
                    Art.NewSprite(up, "Tocha", s.Frame(s.Pick("Static"), 0, 0), Color.white, OWall + 3, new Vector2(x, y0), Vector2.one);
                    fy = y0 + 1.7f * s.Center.y;   // ponytail: chama perto do topo do prop; medir na folha da raia G
                }
                else
                {
                    Rect(up, "Suporte", x, y0 + 0.12f, 0.08f, 0.26f, Art.SteelDark, OWall + 3);
                    Rect(up, "Copa", x, y0 + 0.27f, 0.22f, 0.08f, Art.SteelDark, OWall + 3);
                    fy = y0 + 0.44f;
                    SpriteRenderer f = Art.NewSprite(up, "Chama", Art.Triangle(), Flame, OWall + 4, new Vector2(x, fy), new Vector2(0.22f, 0.34f));
                    Art.NewSprite(f.transform, "Miolo", Art.Triangle(), Art.Cream, OWall + 5, new Vector2(0f, -0.15f), new Vector2(0.5f, 0.55f));
                    t.Flame = f.transform;
                }
                t.Halo = Art.NewSprite(up, "Brilho", Art.Glow(), Art.A(TorchLight, 0.35f), OWall + 2, new Vector2(x, fy), Vector2.one * 1.3f);
                t.Pool = Art.NewSprite(r, "PocaDeLuz", Art.Glow(), Art.A(TorchLight, 0.16f), OMark, new Vector2(x, Balance.RoomH - 0.9f), new Vector2(3.4f, 2.6f));
                _torches.Add(t);
            }
        }

        /// <summary>Parede rachada (Run.Secret) na parede lateral: pedra escura com trincas, fresta escura na face de dentro, luz ambar
        /// vazando e entulho no chao; pulsa clara quando o courier chega com a carga (DrawRoomV1). Quebra no SecretFound (OnEventV1).</summary>
        void BuildSecret(Transform r, SecretWall s)
        {
            bool left = s.Pos.X < Balance.RoomW * 0.5f;
            float wx = left ? -WallT / 2f : Balance.RoomW + WallT / 2f, inner = left ? 0.05f : Balance.RoomW - 0.05f, side = left ? 1f : -1f;
            float y = s.Pos.Y, h = 2f * Balance.DoorHalf;
            Rect(r, "PedraRachada", wx, y, WallT, h, Art.Skirt, OWall + 1);
            _secretCracks = Art.NewSprite(r, "Trincas", Art.Cracks(3), Art.Tinta, OWall + 2, new Vector2(wx, y), new Vector2(0.6f, h));
            Rect(r, "Fresta", inner, y, 0.08f, 0.8f * h, Art.Tinta, OWall + 2);
            _secretGlow = Art.NewSprite(r, "LuzDaFresta", Art.Glow(), Art.A(TorchLight, 0.2f), OMark, new Vector2(inner + side * 0.45f, y), new Vector2(1.2f, 1.2f * h));
            for (int i = 0; i < 4; i++)
                Art.NewSprite(r, "Entulho", Art.Disc(), i % 2 == 0 ? Art.Block : Art.BlockTop, OShadow, new Vector2(inner + side * (0.18f + 0.13f * i), y - 0.55f + 0.37f * i), Vector2.one * (0.12f + 0.03f * (i % 3)));
        }

        /// <summary>Barra de vida do chefe (Ogro no B1, Capitao no M1) no topo, a esquerda do arco (a porta unica fica no centro): nome,
        /// chifres dourados, vida em vermelho e o rastro claro do dano.</summary>
        void BuildBossBar(Transform up, RoomDef room)
        {
            const float x0 = 0.5f, x1 = 3.15f;
            float y = _plateS, cx = (x0 + x1) / 2f, w = x1 - x0;
            _bossX0 = x0; _bossW = w; _bossY = y; _bossGhostF = 1f;
            _bossParts.Add(Rect(up, "ChefeBorda", cx, y, w + 0.1f, BossH + 0.1f, Art.Tinta, OPlate));
            _bossParts.Add(Rect(up, "ChefeFundo", cx, y, w, BossH, Art.A(Art.Off, 0.9f), OPlate + 1));
            _bossParts.Add(_bossGhost = Rect(up, "ChefeRastro", cx, y, w, BossH, Art.Ink, OPlate + 2));
            _bossParts.Add(_bossFill = Rect(up, "ChefeVida", cx, y, w, BossH, Art.Bad, OPlate + 3));
            _bossParts.Add(Art.NewSprite(up, "ChefeIcone", Art.Horns(), Art.Gold, OPlate + 4, new Vector2(x0 - 0.28f, y), Vector2.one * 0.46f));
            _bossName = NewLabel(30);
            _bossName.text = room.Boss ? T("Ogro", "Ogre") : T("Capitão Ladrão", "Thief Captain");
            _bossName.enabled = true;
            _roomLabels.Add(_bossName); _roomLabelPos.Add(up.TransformPoint(new Vector3(cx, y, 0f)));
        }

        void DrawRoomV1(Run run, float dt)
        {
            // portas: o cadeado cai e quebra quando destranca (Guarda morto, reforco morto ou a mensagem nas maos, ART_BIBLE §8); com a
            // mensagem fora das maos, o envelope pisca ao lado do cadeado que ela abre (Guarda/reforco; chefe nao)
            bool boss = false;
            foreach (Enemy e in run.Enemies) if (e.Alive && e.Def.Locks && e.Def.Boss) boss = true;
            Cargo m = run.Main;
            bool key = m.Type == CargoType.Message && m.State != CargoState.Held && m.State != CargoState.Gone;
            foreach (DoorV dv in _doors)
            {
                Door d = dv.D;
                if (dv.Was && !d.Locked)
                {
                    Puff(Art.Lock(), dv.LockAt, new Vector2(0f, -3f), Art.Role(EnemyType.Guard), 0.38f, 0.3f, 0.35f);
                    Burst(dv.LockAt, Art.Role(EnemyType.Guard), 6, 2.5f, 0.08f, 0.3f);
                    Burst(dv.LockAt, Art.Gold, 4, 3f, 0.06f, 0.25f);
                }
                dv.Was = d.Locked;
                bool env = d.Locked && !boss && key;
                dv.Env.enabled = env;
                if (env) dv.Env.color = Art.A(Art.CargoColor(CargoType.Message), 0.55f + 0.45f * Mathf.Sin(_time * 5f));
            }
            if (_chestRange != null) _chestRange.enabled = run.Chest != null && !run.Chest.Opened && run.Has(Perk.Magnet);
            foreach (TorchV t in _torches)
            {
                float n = Mathf.PerlinNoise(_time * 3.3f, t.Seed);
                t.Halo.color = Art.A(TorchLight, 0.26f + 0.16f * n);
                t.Pool.color = Art.A(TorchLight, 0.12f + 0.08f * n);
                if (t.Flame != null) t.Flame.localScale = new Vector3(0.22f * (0.92f + 0.12f * n), 0.34f * (0.85f + 0.3f * n), 1f);
            }
            if (_secretCracks != null && run.Secret != null)
            {
                bool near = run.CarryOk && V2.Dist(run.Courier.Pos, run.Secret.Pos) <= 3f;
                float k = near ? 0.5f + 0.5f * Mathf.Sin(_time * 6f) : 0f;
                _secretCracks.color = Color.Lerp(Art.Tinta, Art.Ink, 0.8f * k);
                _secretGlow.color = Art.A(TorchLight, 0.16f + 0.14f * k + 0.04f * Mathf.Sin(_time * 2f));
            }
            // pilar em pe: esmaece a 45% com o courier ou uma carga no chao escondidos atras dele
            foreach (PillarV p in _pillars)
            {
                bool behind = Behind(p, run.Courier.Pos);
                foreach (Cargo g in run.Cargos) if (g.State == CargoState.Ground && Behind(p, g.Pos)) behind = true;
                p.R.color = Art.A(Color.white, Mathf.MoveTowards(p.R.color.a, behind ? 0.45f : 1f, dt * 4f));
            }
            DrawBoss(run, dt);
        }

        static bool Behind(PillarV p, V2 at) => at.Y > p.Back && at.Y < p.Reach && Mathf.Abs(at.X - p.X) < p.HalfW + 0.35f;

        void DrawBoss(Run run, float dt)
        {
            if (_bossFill == null) return;
            Enemy b = null;
            foreach (Enemy e in run.Enemies) if (e.Alive && e.Def.Boss) { b = e; break; }
            float f = b != null ? Mathf.Clamp01((float)b.Hp / MaxHp(b, run)) : 0f;
            _bossGhostF = _bossGhostF < f ? f : Mathf.MoveTowards(_bossGhostF, f, dt * 0.6f);
            bool show = b != null || _bossGhostF > 0.001f;   // morto: o rastro desce ate zero e a barra some
            foreach (SpriteRenderer r in _bossParts) if (r.enabled != show) r.enabled = show;
            if (_bossName != null && _bossName.enabled != show) _bossName.enabled = show;
            if (!show) return;
            BossBar(_bossFill, f);
            BossBar(_bossGhost, _bossGhostF);
        }

        void BossBar(SpriteRenderer r, float f)
        {
            r.transform.localPosition = new Vector3(_bossX0 + _bossW * f * 0.5f, _bossY, 0f);
            r.transform.localScale = new Vector3(_bossW * f, BossH, 1f);
        }

        /// <summary>Selo de risco para a CARGA (Door.Risk) sob a placa, separado das caveiras (risco do courier): borda limao (a cor da
        /// carga) e 1 icone por risco, cada um = a carga (quadrado limao) + o que acontece com ela: saco roxo (ladrao/Capitao leva),
        /// flecha vermelha (arqueiro acerta), trinca e impacto laranja (Bruto/Ogro esmaga). Sem o arco, ao lado esquerdo da placa.</summary>
        void RiskBadge(Transform up, float x, float plateW, CargoRisk risk)
        {
            int n = 0;
            for (int b = 1; b <= (int)CargoRisk.Smash; b <<= 1) if ((risk & (CargoRisk)b) != 0) n++;
            if (n == 0) return;
            const float ri = 0.3f, rg = 0.06f;
            float bw = n * ri + (n - 1) * rg + 0.12f, bh = ri + 0.1f;
            float bx = _sArco != null ? x : x - plateW / 2f - 0.12f - bw / 2f, by = _sArco != null ? _plateS - PlateH / 2f - 0.07f - bh / 2f : _plateS;
            Rect(up, "RiscoBorda", bx, by, bw + 0.06f, bh + 0.06f, Art.Cargo, OPlate);
            Rect(up, "Risco", bx, by, bw, bh, Art.Plate, OPlate + 1);
            float rx = bx - bw / 2f + 0.06f + ri / 2f;
            if ((risk & CargoRisk.Thief) != 0) { RiskIcon(up, rx, by, ri, CargoRisk.Thief); rx += ri + rg; }
            if ((risk & CargoRisk.Archer) != 0) { RiskIcon(up, rx, by, ri, CargoRisk.Archer); rx += ri + rg; }
            if ((risk & CargoRisk.Smash) != 0) RiskIcon(up, rx, by, ri, CargoRisk.Smash);
        }

        void RiskIcon(Transform up, float x, float y, float s, CargoRisk k)
        {
            Art.NewSprite(up, "RiscoCarga", Art.Rounded(), Art.Cargo, OPlate + 2, new Vector2(x - 0.04f, y - 0.03f), Vector2.one * (s * 0.62f));
            switch (k)
            {
                case CargoRisk.Thief:
                    Art.NewSprite(up, "RiscoLadrao", Art.Bag(), Art.Role(EnemyType.Thief), OPlate + 3, new Vector2(x + 0.07f, y + 0.05f), Vector2.one * (s * 0.62f));
                    break;
                case CargoRisk.Archer:   // flecha descendo da esquerda para a direita, atravessando a carga
                    Art.NewSprite(up, "RiscoFlecha", Art.Square(), Art.Bad, OPlate + 3, new Vector2(x, y), new Vector2(s * 0.95f, s * 0.13f)).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
                    Vector2 tip = new Vector2(x, y) + new Vector2(Mathf.Cos(-35f * Mathf.Deg2Rad), Mathf.Sin(-35f * Mathf.Deg2Rad)) * (s * 0.48f);
                    Art.NewSprite(up, "RiscoPonta", Art.Triangle(), Art.Bad, OPlate + 3, tip, Vector2.one * (s * 0.4f)).transform.localRotation = Quaternion.Euler(0f, 0f, -125f);
                    break;
                default:
                    Art.NewSprite(up, "RiscoTrinca", Art.Cracks(3), Art.Tinta, OPlate + 3, new Vector2(x - 0.04f, y - 0.03f), Vector2.one * (s * 0.62f));
                    Art.NewSprite(up, "RiscoImpacto", Art.Star(), Art.Role(EnemyType.Brute), OPlate + 3, new Vector2(x + 0.08f, y + 0.07f), Vector2.one * (s * 0.45f));
                    break;
            }
        }

        // ------------------------------------------------------------------ eventos da v1 -> efeitos

        void OnEventV1(SimEvent e, Run run, Vector2 p)
        {
            switch (e.Kind)
            {
                case Ev.Explosion: Explosion(p, e.B / 100f, e.A == 0); break;
                case Ev.FuseLit: Burst(p + new Vector2(0f, 0.3f / Tilt), Flame, 6, 2f, 0.08f, 0.3f); break;
                case Ev.ArrowShot:
                    foreach (Ent x in _enemies) if (x.Id == e.A) x.StrikeT = StrikeHold;   // o resto do tiro (soltar e acompanhar) em 0,15 s
                    Burst(p + new Vector2(0f, 0.5f / Tilt), ArcherTeal, 3, 1.5f, 0.08f, 0.2f);
                    break;
                case Ev.ArrowHit:
                    if (e.A == 2) Burst(p, Art.Bone, 5, 2.5f, 0.07f, 0.3f);   // lascas na pedra
                    else Burst(p, Color.white, 4, 2.5f, 0.09f, 0.2f);
                    break;
                case Ev.PuddleSpawn:
                    Burst(p, SlimePink, 10, 2.5f, 0.16f, 0.45f);
                    Puff(Art.Disc(), p, Vector2.zero, Art.A(SlimePink, 0.6f), 0.3f, 2f * Balance.PuddleR, 0.25f, OMark);
                    break;
                case Ev.CakeWobble: Burst(V(run.Courier.Pos) + new Vector2(0f, 0.6f / Tilt), Art.CargoColor(CargoType.Cake), 3, 1.5f, 0.07f, 0.3f); break;
                case Ev.SecretFound:   // a sala ja e a secreta: entulho saindo pela entrada
                {
                    Vector2 at = V(Balance.Entrance);
                    for (int i = 0; i < 14; i++)
                        Puff(Art.Square(), at + new Vector2(Random.Range(-0.8f, 0.8f), -0.2f), new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(1.5f, 4.5f)),
                             Random.value < 0.5f ? Art.Block : Art.WallTop, Random.Range(0.12f, 0.22f), 0.06f, 0.7f);
                    Burst(at, Art.A(Art.Dim, 0.8f), 10, 2.5f, 0.3f, 0.6f);
                    Float(at + new Vector2(0f, 1.2f), T("passagem secreta!", "secret passage!"), Art.Gold, 52);
                    Shake(0.15f, 0.3f);
                    break;
                }
                case Ev.TimeWarning: Float(V(run.Courier.Pos), Textos.Segundos(e.A), Art.Bad, 64); Shake(0.06f, 0.2f); break;
                case Ev.MessageDoor:   // portas da sala nova (o _doors ainda e o da sala anterior neste ponto): chave dourada subindo
                    foreach (Door d in run.Doors)
                    {
                        Vector2 at = new Vector2(d.Pos.X, Balance.RoomH + 0.3f);
                        Burst(at, Art.CargoColor(CargoType.Message), 8, 2.5f, 0.12f, 0.45f);
                        Puff(Art.Key(), at, new Vector2(0f, 1.4f), Art.Gold, 0.45f, 0.5f, 0.8f);
                    }
                    Float(V(run.Courier.Pos), T("a mensagem abre a porta!", "the message opens the door!"), Art.CargoColor(CargoType.Message), 40);
                    break;
                case Ev.Stun: Burst(p + new Vector2(0f, 0.6f / Tilt), Art.Gold, 5, 2f, 0.08f, 0.3f); break;
                case Ev.CaptainCall:
                    Puff(Art.ThinRing(), p, Vector2.zero, Art.A(Art.Role(EnemyType.Thief), 0.9f), 0.6f, 7f, 0.55f, ORing);
                    Float(p + new Vector2(0f, 1.1f / Tilt), "!!", Art.Role(EnemyType.Thief), 64);   // acima da cabeca: nao cobre o "ladrao!" do ThiefGrab no mesmo ponto
                    Shake(0.12f, 0.25f);
                    foreach (Ent x in _enemies)
                        if (x.Id == e.A && x.Anim != null) { int k = First(x.Anim.Sheet, Shout); if (k >= 0) x.RoarT = x.Anim.Sheet.Duration(k); }
                    break;
                case Ev.RoomEvent:
                    if (e.B <= 0) break;
                    Float(new Vector2(Balance.RoomW / 2f, Balance.RoomH - 2.5f), T("reforço!", "reinforcements!"), Art.Bad, 60);
                    Puff(Art.Glow(), new Vector2(Balance.RoomW / 2f, Balance.RoomH - 0.5f), Vector2.zero, Art.A(Art.Bad, 0.45f), 9f, 11f, 0.5f, OMark);
                    Shake(0.1f, 0.25f);
                    break;
                case Ev.EnemyHit:
                    Burst(p + new Vector2(0f, 0.3f / Tilt), Art.Gold, 3, 4f, 0.06f, 0.16f);   // faiscas do golpe
                    foreach (Enemy x in run.Enemies)   // Capitao com a carga: golpes para soltar
                        if (x.Id == e.A && x.Type == EnemyType.Captain && x.Carry != null && x.Hits > 0)
                            Float(p, x.Hits + "/" + Balance.CaptainDropHits, Art.Role(EnemyType.Thief), 50);
                    break;
                case Ev.ChestOpen:   // moedas pulando do bau
                    for (int i = 0; i < 8; i++)
                        Puff(Art.CoinShape(), p + new Vector2(0f, 0.3f / Tilt), new Vector2(Random.Range(-1.8f, 1.8f), Random.Range(2.5f, 4.5f)), Art.Coin, 0.22f, 0.16f, 0.7f);
                    break;
                case Ev.CargoDrop:   // caixa pesada no chao: baque com mais poeira
                    foreach (Cargo g in run.Cargos)
                        if (g.Id == e.A && g.Type == CargoType.Heavy) { Burst(p, Art.A(Art.Dim, 0.8f), 8, 2f, 0.16f, 0.4f); Shake(0.05f, 0.12f); }
                    break;
                case Ev.CargoPickup: Burst(p, Art.A(Art.Dim, 0.7f), 3, 1.2f, 0.12f, 0.3f); break;   // poeira de onde ela saiu
                case Ev.ContractEnd:
                    if (e.B == (int)Outcome.TimeUp)
                    {
                        Float(V(run.Courier.Pos), T("tempo esgotado!", "time's up!"), Art.Bad, 56);
                        Burst(V(run.Courier.Pos), Art.CargoColor(CargoType.Message), 12, 3f, 0.12f, 0.6f);
                    }
                    break;
            }
        }

        /// <summary>Explosao (barril 2,5 m / bombinha 1,5 m): chao chamuscado (2,5 s), o alcance exato em vermelho, onda de choque laranja,
        /// clarao, fagulhas, fumaca subindo e sacudida (barril forte).</summary>
        void Explosion(Vector2 at, float radius, bool barrel)
        {
            float d = 2f * radius;
            Puff(Art.Disc(), at, Vector2.zero, Art.A(Art.Dark, 0.55f), d * 0.55f, d * 0.6f, 2.5f, OMark);
            Puff(Art.ThinRing(), at, Vector2.zero, Art.A(Art.Bad, 0.9f), d, d, 0.45f, ORing);
            Puff(Art.ThinRing(), at, Vector2.zero, Boom, 0.3f, d, 0.3f, ORing + 1);
            Puff(Art.Glow(), at + new Vector2(0f, 0.4f / Tilt), Vector2.zero, Art.A(FlashC, 0.95f), d * 0.7f, d * 1.25f, 0.22f);
            Burst(at, Boom, barrel ? 18 : 10, 6f, 0.2f, 0.45f);
            Burst(at, Art.Gold, barrel ? 10 : 6, 7f, 0.1f, 0.3f);
            for (int i = 0, n = barrel ? 10 : 5; i < n; i++)
                Puff(Art.Disc(), at + Random.insideUnitCircle * (0.4f * radius), Random.insideUnitCircle * 1.5f + new Vector2(0f, 0.8f), Art.A(Smoke, 0.85f), 0.25f, 0.65f, Random.Range(0.6f, 1f));
            Shake(barrel ? 0.35f : 0.16f, barrel ? 0.4f : 0.22f);
            if (barrel) Float(at, T("BUM!", "BOOM!"), Boom, 64);
        }

        void HideLabelsV1()
        {
            if (_timerT != null) _timerT.enabled = false;
            foreach (CargoV v in _cargos) if (v.Num != null) { v.Num.enabled = false; v.NumShown = -1; }
            _bossName = null;   // destruido com os _roomLabels
        }
    }
}
