using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Rect = UnityEngine.Rect;

namespace DC
{
    /// <summary>
    /// Folha pre-renderizada do Blender (client/tools/render_sprites.py; contrato em docs/RENDER_SPRITES.md §4):
    /// Resources/Sprites/&lt;preset&gt;/meta.json + 1 PNG por clipe, linha = direcao (linha 0 no TOPO do PNG), coluna = quadro.
    /// Carrega 1 vez por nome (inclusive a falha) e cria cada Sprite na 1a vez que e pedido (cache por clipe/linha/quadro: o caminho
    /// quente nao aloca). Sem folha (pasta vazia, meta ruim, -nographics) o Get devolve null e a WorldView desenha o procedural.
    /// Clipe sem PNG e pulado sozinho (os outros da folha continuam valendo). Padrao do SpriteSheet do Forge Street.
    /// </summary>
    public sealed class SpriteSheet
    {
        [Serializable] public sealed class ClipMeta { public string name, file; public int frames; public bool loop; public float fps, stride_mps; public float[] hands; }

        /// <summary>Teto e piso do ritmo da locomocao pela passada (teto 2,3x = receita do COE; piso: corrida travada na parede nao congela).</summary>
        public const float MaxRate = 2.3f, MinRate = 0.5f;
        [Serializable] public sealed class Meta { public int size, dirs, rows; public bool mirror; public float ppu, escala; public float[] pivot, pivot_px; public List<ClipMeta> clips; }

        static readonly Dictionary<string, SpriteSheet> Cache = new Dictionary<string, SpriteSheet>();

        /// <summary>Direcoes logicas (1, 4, 8) e linhas na folha (com espelho: 8 -> 5, 4 -> 3).</summary>
        public readonly int Dirs, Rows;
        public readonly bool Mirror;
        /// <summary>px por metro da textura (1 unidade = 1 m, sem fator por personagem).</summary>
        public readonly float Ppu, Escala;
        /// <summary>Pivo normalizado (pe do modelo; origem no canto inferior esquerdo da celula).</summary>
        public readonly Vector2 Pivot;
        /// <summary>Centro da celula relativo ao pivo (m): props sao centrados na celula pelo render, entao e o centro do objeto.</summary>
        public readonly Vector2 Center;

        readonly float _size;
        readonly Vector2 _pivotPx;
        readonly ClipMeta[] _clips;
        readonly Texture2D[] _tex;
        readonly Sprite[][] _spr;

        SpriteSheet(Meta m, Texture2D[] tex)
        {
            Escala = m.escala > 0f ? m.escala : 1f;   // ator mostrado N vezes maior (render_sprites.py --escala): sprite e ancoras pelo mesmo ppu
            Dirs = m.dirs; Rows = m.rows; Mirror = m.mirror; Ppu = m.ppu / Escala; _size = m.size;
            Pivot = m.pivot != null && m.pivot.Length >= 2 ? new Vector2(m.pivot[0], m.pivot[1]) : new Vector2(0.5f, 0f);
            _pivotPx = m.pivot_px != null && m.pivot_px.Length >= 2 ? new Vector2(m.pivot_px[0], m.pivot_px[1]) : Pivot * m.size;
            Center = new Vector2((0.5f - Pivot.x) * m.size / m.ppu, (0.5f - Pivot.y) * m.size / m.ppu);
            _clips = m.clips.ToArray(); _tex = tex; _spr = new Sprite[tex.Length][];
        }

        /// <summary>Folha do preset (null = nao ha: desenhar o procedural). Carrega na 1a chamada; depois e so o dicionario.</summary>
        public static SpriteSheet Get(string name)
        {
            if (!Cache.TryGetValue(name, out SpriteSheet s)) Cache[name] = s = Load(name);
            return s;
        }

        static SpriteSheet Load(string name)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return null;   // -batchmode -nographics: procedural
            var json = Resources.Load<TextAsset>("Sprites/" + name + "/meta");
            if (json == null) return null;
            Meta m = null;
            try { m = JsonUtility.FromJson<Meta>(json.text); }
            catch (ArgumentException) { }
            int want = m == null ? 0 : m.mirror ? m.dirs / 2 + 1 : m.dirs;
            if (m != null && m.rows <= 0) m.rows = want;
            if (m == null || m.size <= 0 || m.ppu <= 0f || (m.dirs != 1 && m.dirs != 4 && m.dirs != 8) || m.rows != want || m.clips == null || m.clips.Count == 0)
            {
                Debug.LogWarning("SpriteSheet " + name + ": meta.json invalido, usando procedural");
                return null;
            }
            var tex = new Texture2D[m.clips.Count];
            int ok = 0;
            for (int i = 0; i < tex.Length; i++)
            {
                ClipMeta c = m.clips[i];
                string file = string.IsNullOrEmpty(c.file) ? c.name : Path.GetFileNameWithoutExtension(c.file);
                Texture2D t = c.frames > 0 ? Resources.Load<Texture2D>("Sprites/" + name + "/" + file) : null;
                if (t == null || t.width < c.frames || t.height < m.rows)
                {
                    Debug.LogWarning("SpriteSheet " + name + ": clipe " + c.name + " sem PNG ou fora do meta, pulado");
                    continue;
                }
                if (c.hands != null && c.hands.Length < m.rows * c.frames * 3) c.hands = null;   // ancora incompleta: sem ancora
                tex[i] = t; ok++;
            }
            return ok > 0 ? new SpriteSheet(m, tex) : null;
        }

        /// <summary>Indice do clipe (com PNG) ou -1.</summary>
        public int Clip(string name)
        {
            for (int i = 0; i < _clips.Length; i++) if (_tex[i] != null && _clips[i].name == name) return i;
            return -1;
        }

        /// <summary>1o clipe que existe entre `a`, `b`, Idle e o 1o da folha (props: Static).</summary>
        public int Pick(string a, string b = null)
        {
            int i = Clip(a);
            if (i < 0 && b != null) i = Clip(b);
            if (i < 0) i = Clip("Idle");
            if (i >= 0) return i;
            for (i = 0; _tex[i] == null; i++) { }   // o Load garante ao menos 1 clipe com PNG
            return i;
        }

        public int Frames(int clip) => _clips[clip].frames;
        float Fps(int clip) => _clips[clip].fps > 0f ? _clips[clip].fps : 12f;   // fps POR CLIPE (o Idle do placeholder e 4,9)
        public float Duration(int clip) => _clips[clip].frames / Fps(clip);

        /// <summary>Multiplicador do fps do clipe para a velocidade `speed` (m/s da Sim): speed / stride_mps (o pe nao patina), entre
        /// MinRate e MaxRate (RENDER_SPRITES §4). Clipe sem `stride_mps` (sem root motion medido): 1, o fps do meta.</summary>
        public float Rate(int clip, float speed)
        {
            float s = _clips[clip].stride_mps;
            return s > 0f ? Mathf.Clamp(speed / s, MinRate, MaxRate) : 1f;
        }

        /// <summary>Quadro no tempo `t` (s desde o inicio do clipe): laco pelo `loop` do meta (ou `forceLoop`); sem laco para no ultimo.</summary>
        public int FrameAt(int clip, float t, bool forceLoop = false)
        {
            int n = _clips[clip].frames, f = Mathf.Max(0, (int)(t * Fps(clip)));
            return _clips[clip].loop || forceLoop ? f % n : Mathf.Min(f, n - 1);
        }

        /// <summary>Quadro 0 recortado em volta do ator, para icone da interface (roupa, chefe do mapa): cx, cy = centro em fracao da celula
        /// a partir do canto de cima; lado = fracao da celula (as medidas saem do contact sheet; o ator nao enche a celula).</summary>
        public Sprite Retrato(int clip, int row, float cx, float cy, float lado)
        {
            row = Mathf.Clamp(row, 0, Rows - 1);
            int k = clip * 64 + row;
            if (_retratos.TryGetValue(k, out Sprite s)) return s;
            Texture2D t = _tex[clip];
            float cw = t.width / (float)_clips[clip].frames, ch = t.height / (float)Rows, w = lado * cw, h = lado * ch;
            var r = new Rect(Mathf.Clamp(cx * cw - w / 2f, 0f, cw - w), (Rows - 1 - row) * ch + Mathf.Clamp((1f - cy) * ch - h / 2f, 0f, ch - h), w, h);
            return _retratos[k] = Sprite.Create(t, r, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        readonly Dictionary<int, Sprite> _retratos = new Dictionary<int, Sprite>();

        public Sprite Frame(int clip, int row, int frame)
        {
            ClipMeta c = _clips[clip];
            row = Mathf.Clamp(row, 0, Rows - 1); frame = Mathf.Clamp(frame, 0, c.frames - 1);
            Sprite[] arr = _spr[clip] ?? (_spr[clip] = new Sprite[Rows * c.frames]);
            int k = row * c.frames + frame;
            if (arr[k] != null) return arr[k];
            // celula pela textura real: se o import reduziu a folha (maxTextureSize), recorte e ppu acompanham
            Texture2D t = _tex[clip];
            float cw = t.width / (float)c.frames, ch = t.height / (float)Rows;
            return arr[k] = Sprite.Create(t, new Rect(frame * cw, (Rows - 1 - row) * ch, cw, ch), Pivot, Ppu * cw / _size, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Direcao logica (0 = S, sentido horario) para o vetor (vx, vy) da Sim (+y = para cima na tela); parado devolve `last`.</summary>
        public int Dir(float vx, float vy, int last)
        {
            if (Dirs == 1) return 0;
            if (vx * vx + vy * vy < 1e-8f) return last;
            // ponytail: sem histerese; dedo parado perto da fronteira de 2 setores pode alternar. Adicionar se o video mostrar
            return (Mathf.RoundToInt(-Mathf.Atan2(vx, -vy) * Mathf.Rad2Deg / (360f / Dirs)) % Dirs + Dirs) % Dirs;
        }

        /// <summary>Linha da folha e espelho da direcao logica d (contrato: row = d &lt;= n/2 ? d : n - d; flipX = d &gt; n/2).</summary>
        public int Row(int d) => Mirror && d > Dirs / 2 ? Dirs - d : Mathf.Min(d, Rows - 1);
        public bool Flip(int d) => Mirror && d > Dirs / 2;

        /// <summary>Ancora das maos no quadro, em m de tela a partir do pe (x espelhado no flip); front = carga desenha na frente do
        /// portador. false se o clipe nao tem `hands`.</summary>
        public bool Hands(int clip, int row, int frame, bool flip, out Vector2 off, out bool front)
        {
            ClipMeta c = _clips[clip];
            if (c.hands == null) { off = Vector2.zero; front = true; return false; }
            int k = (row * c.frames + frame) * 3;
            float hx = flip ? _size - c.hands[k] : c.hands[k];
            off = new Vector2((hx - _pivotPx.x) / Ppu, (c.hands[k + 1] - _pivotPx.y) / Ppu);
            front = c.hands[k + 2] > 0.5f;
            return true;
        }
    }

    /// <summary>Animacao de 1 entidade com folha: clipe, tempo e direcao logica (parado guarda a ultima). Quem usa avanca T, escolhe o
    /// clipe (Play), a direcao (Face) e chama Show.</summary>
    public sealed class SpriteAnim
    {
        public SpriteSheet Sheet;
        public readonly SpriteRenderer R;
        public int Clip = -1, D, Row, Frame;
        public bool Flip;
        public float T;

        public SpriteAnim(SpriteSheet sheet, SpriteRenderer r) { Sheet = sheet; R = r; }

        /// <summary>Troca de folha ou de clipe recomeca o tempo; o mesmo clipe continua.</summary>
        public void Play(SpriteSheet sheet, int clip)
        {
            if (sheet != Sheet) { Sheet = sheet; Clip = -1; }
            if (clip != Clip) { Clip = clip; T = 0f; }
        }

        /// <summary>Avanca o tempo do clipe atual (chamar depois do Play); locomocao com `stride_mps` no ritmo da passada.</summary>
        public void Step(float dt, float speed = 0f) => T += dt * Sheet.Rate(Clip, speed);

        public void Face(float vx, float vy) => D = Sheet.Dir(vx, vy, D);

        /// <summary>Aplica o quadro (frame &lt; 0: pelo tempo e fps do meta, em laco se `loop`) e o espelho.</summary>
        public void Show(int frame = -1, bool loop = false)
        {
            Frame = frame >= 0 ? Mathf.Min(frame, Sheet.Frames(Clip) - 1) : Sheet.FrameAt(Clip, T, loop);
            Row = Sheet.Row(D); Flip = Sheet.Flip(D);
            R.sprite = Sheet.Frame(Clip, Row, Frame);
            R.flipX = Flip;
        }

        public bool Hands(out Vector2 off, out bool front) => Sheet.Hands(Clip, Row, Frame, Flip, out off, out front);
    }
}
