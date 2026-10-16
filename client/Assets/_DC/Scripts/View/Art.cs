using System;
using System.Collections.Generic;
using DC.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rect = UnityEngine.Rect;

namespace DC
{
    /// <summary>
    /// Arte 100% gerada por codigo (zero asset), padrao do Art.cs do Forge Street: mascaras com 4 amostras de antialias, cache por
    /// chave, 1 unidade de mundo = sprite inteiro. Cada entidade tem cor E forma: o papel do inimigo nunca e so a cor (morcego asas,
    /// ladrao mascara, guarda elmo quadrado, bruto chifres, ogro chifres dourados e maior). A troca por sprite real e por entidade, nos
    /// metodos Paint* do WorldView.
    /// </summary>
    public static class Art
    {
        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
        public static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        // Paleta = docs/ART_BIBLE.md §3 (decisao do coordenador, 2026-10-09). Regras: azul so no courier (projetil e anel do dash);
        // amarelo-limao so na CARGA (contorno, alerta, icone); roxo so no ladrao e no roubo; moeda e ouro-laranja, nunca o limao;
        // cenario so azul-violeta-cinza.
        // interface e feedback
        public static readonly Color Ink = Hex(0xF7F3EA), Tinta = Hex(0x1B1724), Bad = Hex(0xFF3B30), Good = Hex(0x45C46B),
            Coin = Hex(0xF5A623), Gold = Hex(0xE0B23A), Paper = Hex(0xF3E2C0), PaperEdge = Hex(0xC48A4E),
            Bg = Hex(0x14161F), Dim = Hex(0x8A90A8), Off = Hex(0x4F5866), Dark = Hex(0x0E0F18),
            BadInk = Hex(0xA61E22);   // erro escrito sobre o papel (o #FF3B30 da 2,8:1 no papel; este da 5,8:1)
        // cenario
        public static readonly Color FloorA = Hex(0x3E4458), FloorB = Hex(0x394052), Joint = Hex(0x24273A), WallTop = Hex(0x5E6680),
            Wall = Hex(0x474D63), Skirt = Hex(0x2C3042), BlockTop = Hex(0x6B7390), Block = Hex(0x4E5570), BlockEdge = Hex(0x8A93B0),
            DoorIn = Hex(0x0E0F18), Plate = Hex(0x2E3242), Bone = Hex(0xEDE6D6), Steel = Hex(0x8C96A6), SteelDark = Hex(0x4F5866);
        // papeis
        public static readonly Color Courier = Hex(0x2F6FE4), Cream = Hex(0xF2E6CC), Leather = Hex(0x7A4A2A), Shot = Hex(0x7FC4FF),
            Cargo = Hex(0xFFE14D), BoxStrap = Hex(0x6E7487), Gem = Hex(0x5CE1E6), EggSpot = Hex(0xE8735A), Pet = Hex(0xE8735A),
            ThiefSkin = Hex(0x8DB255), BatWing = Hex(0xB5546A),
            Client = Hex(0x3FAE5A), Tele = Hex(0xFF3B30), Crack = Hex(0x1B1724), Chest = Hex(0x9A6432);
        /// <summary>Cor dominante por PAPEL (indice de EnemyType): morcego vinho, ladrao roxo, guarda vermelho, bruto laranja-ferrugem, ogro musgo;
        /// v1 pelo ASSETS §8 (Lote 3): arqueiro ciano-petroleo, limo rosa-chiclete, Capitao Ladrao roxo fundo (familia do ladrao).</summary>
        static readonly Color[] RoleC = { Hex(0x5B2A3A), Hex(0x8E44AD), Hex(0xB3262E), Hex(0xC8642B), Hex(0x6F8A4B), Hex(0x1E7A80), Hex(0xF26DB3), Hex(0x5E2A84) };
        /// <summary>Corpo por carga: caixa madeira, reliquia porcelana, ovo casca; v1: bolo glace rosa, mensagem pergaminho, caixa pesada
        /// madeira escura, barril siena.</summary>
        static readonly Color[] CargoC = { Hex(0xC98A4B), Hex(0xF1EEE4), Hex(0xF3E3C3), Hex(0xF6D7E0), Hex(0xEADFC6), Hex(0x7D5A3C), Hex(0xA0522D) };
        /// <summary>Indice fora da tabela (enum novo sem cor ainda) cai na ultima cor: nunca IndexOutOfRange no desenho.</summary>
        public static Color Role(EnemyType t) => RoleC[Mathf.Clamp((int)t, 0, RoleC.Length - 1)];
        public static Color CargoColor(CargoType t) => CargoC[Mathf.Clamp((int)t, 0, CargoC.Length - 1)];
        /// <summary>Forma por carga (a forma, nao so a cor, diz qual e): caixa = quadrado arredondado, reliquia = losango, ovo = oval, bolo =
        /// dois andares, mensagem = envelope, caixa pesada = quadrado com cintas, barril = tonel com aros.</summary>
        public static Sprite CargoSprite(CargoType t)
        {
            switch (t)
            {
                case CargoType.Box: return Rounded();
                case CargoType.Relic: return Diamond();
                case CargoType.Cake: return Get("cake", (x, y) => (Mathf.Abs(x) < 0.85f && y > -0.85f && y < -0.1f) || (Mathf.Abs(x) < 0.55f && y >= -0.1f && y < 0.42f)
                    || x * x + (y - 0.6f) * (y - 0.6f) < 0.03f);
                case CargoType.Message: return Envelope();
                case CargoType.Heavy: return Get("heavy", (x, y) => RoundedIn(x, y) && Mathf.Abs(Mathf.Abs(y) - 0.42f) > 0.07f);
                case CargoType.Barrel: return Get("barrel", (x, y) => Mathf.Abs(y) < 0.92f && Mathf.Abs(x) < 0.6f + 0.2f * (1f - y * y) && Mathf.Abs(Mathf.Abs(y) - 0.5f) > 0.06f);
                default: return Egg();
            }
        }

        // ---------- mascaras (x, y em [-1, 1]) ----------
        const int Side = 128;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Square() => Get("sq", (x, y) => true);
        public static Sprite Disc() => Get("disc", (x, y) => x * x + y * y <= 0.96f);
        public static Sprite Ring() => Get("ring", (x, y) => { float d = x * x + y * y; return d <= 0.96f && d >= 0.66f; });
        public static Sprite ThinRing() => Get("ring2", (x, y) => { float d = x * x + y * y; return d <= 0.96f && d >= 0.84f; });
        /// <summary>Anel tracejado (16 tracos): area de entrega e alcance de pegar a carga.</summary>
        public static Sprite DashedRing() => Get("dashring", (x, y) =>
        {
            float d = x * x + y * y;
            return d <= 0.96f && d >= 0.82f && Mathf.Repeat(Mathf.Atan2(y, x) / (Mathf.PI * 2f) * 16f, 1f) < 0.6f;
        });
        public static Sprite Rounded() => Get("rounded", RoundedIn);
        static bool RoundedIn(float x, float y)
        {
            const float r = 0.3f;
            float qx = Mathf.Max(Mathf.Abs(x) - (0.96f - r), 0f), qy = Mathf.Max(Mathf.Abs(y) - (0.96f - r), 0f);
            return qx * qx + qy * qy <= r * r;
        }
        public static Sprite Diamond() => Get("dia", (x, y) => Mathf.Abs(x) * 0.8f + Mathf.Abs(y) < 0.96f);
        /// <summary>Triangulo apontando para +Y (nariz do courier, seta).</summary>
        public static Sprite Triangle() => Get("tri", (x, y) => y > -0.8f && Mathf.Abs(x) < (0.9f - y) * 0.58f);
        public static Sprite Egg() => Get("egg", (x, y) =>
        {
            float rx = 0.74f - 0.14f * y;
            return x * x / (rx * rx) + y * y / 0.92f <= 1f;
        });
        public static Sprite Star() => Get("star", (x, y) => StarIn(x, y));
        public static Sprite Heart() => Get("heart", (x, y) =>
        {
            float X = x * 1.2f, Y = y * 1.2f + 0.15f, a = X * X + Y * Y - 1f;
            return a * a * a - X * X * Y * Y * Y <= 0f;
        });
        public static Sprite Skull() => Get("skull", (x, y) =>
        {
            float ax = Mathf.Abs(x);
            bool head = x * x + (y - 0.15f) * (y - 0.15f) <= 0.6f || (ax < 0.44f && y > -0.9f && y < -0.2f);
            bool eye = (ax - 0.3f) * (ax - 0.3f) + (y - 0.08f) * (y - 0.08f) < 0.05f;
            bool nose = y < -0.12f && y > -0.36f && ax < (y + 0.36f) * 0.5f;
            bool teeth = y < -0.58f && Mathf.Repeat((x + 0.44f) / 0.22f, 1f) < 0.2f;
            return head && !eye && !nose && !teeth;
        });
        public static Sprite Bat() => Get("bat", (x, y) =>
        {
            float ax = Mathf.Abs(x);
            bool body = x * x + (y + 0.05f) * (y + 0.05f) <= 0.18f;
            bool ear = y > 0.2f && y < 0.62f - 1.7f * Mathf.Abs(ax - 0.2f);
            bool wing = ax >= 0.25f && ax <= 0.98f && y <= 0.45f - 0.36f * (ax - 0.25f)
                && y >= -0.22f + 0.24f * Mathf.Abs(Mathf.Sin((ax - 0.25f) * 8.6f));
            return body || ear || wing;
        });
        /// <summary>Capuz pontudo (silhueta do ladrao): disco com a ponta para cima.</summary>
        public static Sprite Hood() => Get("hood", (x, y) => x * x + (y + 0.18f) * (y + 0.18f) <= 0.62f || InTri(x, y, -0.5f, 0.3f, 0.5f, 0.3f, 0.08f, 0.98f));
        /// <summary>Duas manoplas de pedra dos lados (silhueta do bruto: ombros largos, punhos grandes).</summary>
        public static Sprite Fists() => Get("fists", (x, y) => (Mathf.Abs(x) - 0.72f) * (Mathf.Abs(x) - 0.72f) + (y + 0.25f) * (y + 0.25f) <= 0.07f);
        /// <summary>Dois chifres para fora (ogro; icone do chefe na placa), desenhados por cima do corpo em escala maior.</summary>
        public static Sprite Horns() => Get("horns", (x, y) =>
        {
            float s = x < 0f ? -1f : 1f, ax = x * s;
            return InTri(ax, y, 0.22f, 0.4f, 0.58f, 0.4f, 0.86f, 0.98f);
        });
        /// <summary>Dois olhos (pupila feita por um segundo Eyes menor e escuro).</summary>
        public static Sprite Eyes() => Get("eyes", (x, y) => (Mathf.Abs(x) - 0.45f) * (Mathf.Abs(x) - 0.45f) + y * y < 0.2f);
        /// <summary>Rachaduras: 1 a 3 trincas em zigue-zague saindo do centro (contorno escuro desenhado por cima da carga).</summary>
        public static Sprite Cracks(int n)
        {
            n = Mathf.Clamp(n, 1, 3);
            return CrackS[n] != null ? CrackS[n] : (CrackS[n] = MakeCracks(n));   // chamado todo quadro: cache sem closure nem string
        }
        static readonly Sprite[] CrackS = new Sprite[4];
        static Sprite MakeCracks(int n) => Get("crack" + n, (x, y) =>
        {
            for (int i = 0; i < n; i++)
            {
                float a = 0.7f + i * 2.1f, c = Mathf.Cos(a), s = Mathf.Sin(a);
                float u = x * c + y * s, v = -x * s + y * c;
                if (u < -0.05f || u > 0.95f) continue;
                float t = u * 4f, tri = 4f * Mathf.Abs(t - Mathf.Floor(t + 0.5f)) - 1f;
                if (Mathf.Abs(v - 0.14f * tri) < 0.09f) return true;
            }
            return false;
        });
        public static Sprite ChestShape() => Get("chest", (x, y) =>
            Mathf.Abs(x) < 0.88f && ((y > -0.72f && y < 0.18f) || (y > 0.26f && y < 0.66f && RoundedIn(x, (y - 0.3f) * 1.6f))));
        public static Sprite Lock() => Get("lock", (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + (y - 0.15f) * (y - 0.15f));
            bool shackle = y > 0.15f && d > 0.32f && d < 0.5f;
            bool body = Mathf.Abs(x) < 0.62f && y > -0.85f && y <= 0.18f;
            bool hole = x * x + (y + 0.3f) * (y + 0.3f) < 0.025f;
            return (shackle || body) && !hole;
        });
        public static Sprite Bars() => Get("bars", (x, y) => Mathf.Repeat((x + 1f) / 0.5f, 1f) < 0.3f || Mathf.Abs(y) < 0.1f);
        public static Sprite Pause() => Get("pause", (x, y) => Mathf.Abs(Mathf.Abs(x) - 0.36f) < 0.17f && Mathf.Abs(y) < 0.72f);
        public static Sprite Clock() => Get("clock", (x, y) =>
        {
            float d = x * x + y * y;
            return (d <= 0.96f && d >= 0.62f) || (Mathf.Abs(x) < 0.09f && y > -0.05f && y < 0.55f) || (Mathf.Abs(y) < 0.09f && x > -0.05f && x < 0.42f);
        });
        public static Sprite Flag() => Get("flag", (x, y) =>
            (Mathf.Abs(x + 0.62f) < 0.09f && Mathf.Abs(y) < 0.92f) || (x > -0.56f && x < 0.75f && y > 0.06f && y < 0.86f && !(x > 0.45f && Mathf.Abs(y - 0.46f) < 0.75f - x)));
        public static Sprite CoinShape() => Get("coin", (x, y) => { float d = x * x + y * y; return d <= 0.96f && (d < 0.38f || d > 0.5f); });
        public static Sprite Bag() => Get("bag", (x, y) =>
            x * x + (y + 0.28f) * (y + 0.28f) <= 0.42f || (Mathf.Abs(x) < 0.22f && y > 0.25f && y < 0.5f) || InTri(x, y, -0.45f, 0.82f, 0.45f, 0.82f, 0f, 0.46f));
        /// <summary>Dash: duas setas "&gt;&gt;" apontando para cima.</summary>
        public static Sprite Chevrons() => Get("chev", (x, y) =>
        {
            float ax = Mathf.Abs(x);
            return ax < 0.8f && (Mathf.Abs(y - (0.55f - 0.75f * ax)) < 0.17f || Mathf.Abs(y - (-0.15f - 0.75f * ax)) < 0.17f);
        });
        public static Sprite Smile() => Get("smile", (x, y) => { float d = Mathf.Sqrt(x * x + (y - 0.3f) * (y - 0.3f)); return Mathf.Abs(d - 0.7f) < 0.13f && y < 0.05f; });
        public static Sprite Frown() => Get("frown", (x, y) => { float d = Mathf.Sqrt(x * x + (y + 0.9f) * (y + 0.9f)); return Mathf.Abs(d - 0.7f) < 0.13f && y > -0.45f; });
        public static Sprite Flat() => Get("flat", (x, y) => Mathf.Abs(y + 0.2f) < 0.12f && Mathf.Abs(x) < 0.55f);
        /// <summary>Brilho suave (disco com queda): luz da porta aberta, rastro do dash.</summary>
        public static Sprite Glow() => Soft("glow", (x, y) => { float d = Mathf.Min(1f, Mathf.Sqrt(x * x + y * y)); return (1f - d) * (1f - d); });
        /// <summary>Raios de luz (12 fachos que somem para a borda): o tesouro saindo do bau na cena da entrega.</summary>
        public static Sprite Raios() => Soft("raios", (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y), f = Mathf.Repeat(Mathf.Atan2(y, x) / (Mathf.PI * 2f) * 12f, 1f);
            return Mathf.Clamp01(1f - Mathf.Abs(f - 0.5f) * 4f) * Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 4f);
        });
        /// <summary>Vinheta: so as bordas (alfa 0 no centro): flash de dano em tela cheia.</summary>
        public static Sprite Vignette() => Soft("vignette", (x, y) => Mathf.Clamp01((x * x + y * y - 0.45f) / 1.1f));

        /// <summary>Arco de progresso (anel cheio no sentido horario a partir do topo ate frac). ponytail: 24 degraus em cache.</summary>
        public static Sprite Arc(float frac)
        {
            int k = Mathf.Clamp(Mathf.CeilToInt(frac * 24f), 0, 24);
            return Arcs[k] != null ? Arcs[k] : (Arcs[k] = MakeArc(k));   // a closure fica no MakeArc: o caminho quente nao aloca
        }
        static Sprite MakeArc(int k) => Get("arc" + k, (x, y) =>
        {
            float d = x * x + y * y;
            return d <= 0.96f && d >= 0.66f && Mathf.Repeat(Mathf.Atan2(x, y) / (Mathf.PI * 2f), 1f) < k / 24f;
        });
        static readonly Sprite[] Arcs = new Sprite[25];

        // ---------- icones das telas (menu, estrelas, armas, selo da guilda) ----------
        static bool StarIn(float x, float y)
        {
            const float seg = Mathf.PI * 2f / 5f;
            float f = Mathf.Abs(Mathf.Repeat(Mathf.Atan2(x, y), seg) / seg * 2f - 1f);
            return x * x + y * y <= Mathf.Pow(Mathf.Lerp(0.45f, 0.98f, f * f), 2f);
        }
        /// <summary>Estrela vazada (so o contorno): estrela NAO ganha. A forma, e nao so a cor, diz ganhou/nao ganhou.</summary>
        public static Sprite StarHollow() => Get("starO", (x, y) => StarIn(x, y) && !StarIn(x * 1.55f, y * 1.55f + 0.04f));
        /// <summary>Visto (selecionado).</summary>
        public static Sprite Check() => Get("check", (x, y) => SegD(x, y, -0.7f, 0f, -0.2f, -0.55f) < 0.15f || SegD(x, y, -0.2f, -0.55f, 0.75f, 0.6f) < 0.15f);
        /// <summary>Estilingue: forquilha em Y com o elastico.</summary>
        public static Sprite Sling() => Get("sling", (x, y) => SegD(x, y, 0f, -0.9f, 0f, -0.1f) < 0.13f || SegD(x, y, 0f, -0.1f, -0.55f, 0.75f) < 0.12f
            || SegD(x, y, 0f, -0.1f, 0.55f, 0.75f) < 0.12f || SegD(x, y, -0.55f, 0.75f, 0f, 0.35f) < 0.05f || SegD(x, y, 0.55f, 0.75f, 0f, 0.35f) < 0.05f);
        /// <summary>Bumerangue: barra em V aberto.</summary>
        public static Sprite Boomerang() => Get("boom", (x, y) => SegD(x, y, -0.85f, -0.45f, 0f, 0.55f) < 0.17f || SegD(x, y, 0f, 0.55f, 0.85f, -0.45f) < 0.17f);
        /// <summary>Bombinha: bola com pavio.</summary>
        public static Sprite Bomb() => Get("bomb", (x, y) => (x + 0.1f) * (x + 0.1f) + (y + 0.15f) * (y + 0.15f) <= 0.5f
            || SegD(x, y, 0.3f, 0.45f, 0.55f, 0.8f) < 0.08f || (x - 0.66f) * (x - 0.66f) + (y - 0.86f) * (y - 0.86f) < 0.025f);
        /// <summary>Envelope com a aba em V (selo da guilda: envelope lacrado cruzado por uma chave, ART_BIBLE §12).</summary>
        public static Sprite Envelope() => Get("envelope", (x, y) => Mathf.Abs(x) < 0.95f && Mathf.Abs(y) < 0.64f && Mathf.Abs(y - (0.64f - 0.75f * Mathf.Abs(x))) > 0.06f);
        /// <summary>Chave na diagonal (argola em cima a direita, dentes embaixo a esquerda).</summary>
        public static Sprite Key() => Get("key", (x, y) =>
        {
            float u = (x + y) * 0.7071f, v = (y - x) * 0.7071f, d = Mathf.Sqrt((u - 0.62f) * (u - 0.62f) + v * v);
            return (Mathf.Abs(v) < 0.08f && u > -0.8f && u < 0.4f) || (d > 0.17f && d < 0.33f)
                || (u > -0.8f && u < -0.66f && v < 0f && v > -0.3f) || (u > -0.56f && u < -0.44f && v < 0f && v > -0.24f);
        });

        static float SegD(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            float ex = px - (ax + t * dx), ey = py - (ay + t * dy);
            return Mathf.Sqrt(ex * ex + ey * ey);
        }

        static bool InTri(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
            float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
            float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
            bool neg = d1 < 0f || d2 < 0f || d3 < 0f, pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(neg && pos);
        }

        // cache conferido ANTES de criar a closure (licao do Forge Street: lambda capturada aloca na entrada do metodo)
        static Sprite Get(string key, Func<float, float, bool> inside) => Cached(key) ?? Mask(key, Supersample(inside));
        static Func<int, int, byte> Supersample(Func<float, float, bool> inside) => (x, y) =>
        {
            int n = 0;
            for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                    if (inside((x + 0.25f + sx * 0.5f) / Side * 2f - 1f, (y + 0.25f + sy * 0.5f) / Side * 2f - 1f)) n++;
            return (byte)(n * 63 + (n == 4 ? 3 : 0));
        };
        static Sprite Soft(string key, Func<float, float, float> f) => Cached(key) ?? Mask(key, (x, y) =>
            (byte)(255f * Mathf.Clamp01(f((x + 0.5f) / Side * 2f - 1f, (y + 0.5f) / Side * 2f - 1f))));
        static Sprite Cached(string key) => Cache.TryGetValue(key, out Sprite s) && s != null ? s : null;

        static Sprite Mask(string key, Func<int, int, byte> alpha)
        {
            var tex = new Texture2D(Side, Side, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontUnloadUnusedAsset };
            var px = new Color32[Side * Side];
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++) px[y * Side + x] = new Color32(255, 255, 255, alpha(x, y));
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, Side, Side), new Vector2(0.5f, 0.5f), Side);
            Cache[key] = s;
            return s;
        }

        /// <summary>Piso de lajes de 1 m (xadrez com junta) para SpriteRenderer Tiled: 1 repeticao = 2 x 2 m.</summary>
        public static Sprite Floor()
        {
            if (Cache.TryGetValue("floor", out Sprite s) && s != null) return s;
            const int ppm = 32, n = 2 * ppm;
            Color32 a = FloorA, b = FloorB, j = Joint;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++) px[y * n + x] = x % ppm < 2 || y % ppm < 2 ? j : (x / ppm + y / ppm) % 2 == 0 ? a : b;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontUnloadUnusedAsset };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), ppm, 0, SpriteMeshType.FullRect);
            Cache["floor"] = s;
            return s;
        }

        // ---------- sprite com flash branco e contorno (Assets/_DC/Shaders/SpriteFlash.shader) ----------
        static Material _fx;
        static bool _fxLoaded;
        static MaterialPropertyBlock _fxBlock;
        static readonly int FlashId = Shader.PropertyToID("_Flash"), OutlineId = Shader.PropertyToID("_Outline"), OutlineColorId = Shader.PropertyToID("_OutlineColor");

        /// <summary>Material dos sprites dos atores e das cargas (Resources/Materials/SpriteFlash.mat: e ele que leva o shader para o
        /// build) ou null (sem material ou shader sem suporte: a View volta a tingir com o Sprites/Default, como antes).</summary>
        public static Material FxMat
        {
            get
            {
                if (_fxLoaded) return _fx;
                _fxLoaded = true;
                var m = Resources.Load<Material>("Materials/SpriteFlash");
                _fx = m != null && m.shader != null && m.shader.isSupported ? m : null;
                if (_fx != null) Debug.Log("DC SpriteFlash: ok (" + m.shader.name + ")");
                else Debug.LogWarning("DC SpriteFlash: material ausente ou shader sem suporte, flash por tinta");
                return _fx;
            }
        }

        /// <summary>Flash (0..1, mistura a textura para branco antes da tinta do renderer) e contorno (largura em texels da folha, 0 =
        /// sem; cor com alfa) num sprite que usa o FxMat. Sem FxMat nao faz nada. Sem alocacao (um bloco so, copiado pelo renderer).
        /// Chamar DEPOIS de trocar o sprite do quadro.</summary>
        public static void Fx(SpriteRenderer r, float flash, float outline = 0f, Color outlineColor = default)
        {
            if (FxMat == null) return;
            if (_fxBlock == null) _fxBlock = new MaterialPropertyBlock();
            r.GetPropertyBlock(_fxBlock);
            _fxBlock.SetFloat(FlashId, flash);
            _fxBlock.SetFloat(OutlineId, outline);
            _fxBlock.SetColor(OutlineColorId, outlineColor);
            r.SetPropertyBlock(_fxBlock);
        }

        public static SpriteRenderer NewSprite(Transform parent, string name, Sprite sprite, Color color, int order, Vector2 pos, Vector2 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.color = color; r.sortingOrder = order;
            return r;
        }

        // ---------- uGUI por codigo (sem TextMeshPro) ----------
        // Fontes = docs/ART_BIBLE.md §10 (SIL OFL 1.1, Resources/Fonts, docs/PROVENIENCIA.md): Nunito ExtraBold no texto corrido e
        // Lilita One em titulos, botoes e numeros. Sem os arquivos, a fonte embutida do Unity.
        static Font _font, _titulo;
        public static Font Font()
        {
            if (_font == null) _font = Resources.Load<Font>("Fonts/Nunito-ExtraBold");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }
        public static Font Titulo()
        {
            if (_titulo == null) _titulo = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            return _titulo != null ? _titulo : Font();
        }

        /// <summary>Arte da interface em Resources/UI (logo, icones dos botoes, fundo dos menus; client/tools/ui_assets.py). null = sem o arquivo.</summary>
        public static Sprite Ui(string nome)
        {
            string key = "ui/" + nome;
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;
            var t = Resources.Load<Texture2D>("UI/" + nome);
            if (t == null) return null;
            s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache[key] = s;
            return s;
        }

        /// <summary>Moeda da interface: o icone pintado (Resources/UI/icone_moeda) ou, sem ele, a silhueta procedural na cor da moeda.</summary>
        public static Sprite Moeda() => Ui("icone_moeda") ?? CoinShape();
        public static Color MoedaCor => Ui("icone_moeda") != null ? Color.white : Coin;

        /// <summary>Distancia com sinal ate a borda de um retangulo de cantos redondos que ocupa a textura s x s (negativa = dentro).</summary>
        static float CaixaDist(float x, float y, int s, float raio)
        {
            float h = s * 0.5f - 0.5f, qx = Mathf.Abs(x - s * 0.5f) - (h - raio), qy = Mathf.Abs(y - s * 0.5f) - (h - raio);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - raio;
        }

        /// <summary>Sprite 9-fatias s x s pintado por pixel (x, y a partir de baixo), 100 px por unidade = 1 px da referencia de 1080.</summary>
        static Sprite Nove(string key, int s, int borda, Func<int, int, Color32> pixel)
        {
            if (Cache.TryGetValue(key, out Sprite sp) && sp != null) return sp;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++) px[y * s + x] = pixel(x, y);
            t.SetPixels32(px);
            t.Apply(false, true);
            sp = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(borda, borda, borda, borda));
            Cache[key] = sp;
            return sp;
        }

        static Color32 C32(Color c, float a) => new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(Mathf.Clamp01(a) * 255f));

        /// <summary>Botao de jogo em cinza, tingido pela cor do Image: contorno escuro de 4 px, face com degrade (a cor cheia no alto,
        /// 16% mais escura embaixo) e aba de relevo de 12 px na base.</summary>
        public static Sprite Botao() => Nove("botao9", 96, 36, (x, y) =>
        {
            float d = CaixaDist(x + 0.5f, y + 0.5f, 96, 26f), dentro = -d;
            float v = dentro < 4f ? 0.16f : y + 0.5f < 16f ? 0.6f : Mathf.Lerp(0.84f, 1f, (y + 0.5f) / 96f);
            return C32(new Color(v, v, v), 0.5f - d);
        });

        /// <summary>Destaque do tour (Tour.cs): contorno arredondado de ~7 px com brilho suave dos dois lados, tingido pelo Image.</summary>
        public static Sprite Contorno() => Nove("contorno9", 64, 26, (x, y) =>
        {
            float d = CaixaDist(x + 0.5f, y + 0.5f, 64, 22f) + 9f;   // 0 = o meio do traco, a 9 px da borda
            return C32(Color.white, Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(d) / 3.5f), 0.35f * Mathf.Clamp01(1f - Mathf.Abs(d) / 9f)));
        });

        /// <summary>Balao de fala do tour (cartao do cliente): branco com contorno de tinta de 4 px e cantos de 26 px; tingido pelo Image.</summary>
        public static Sprite Balao() => Nove("balao9", 96, 34, (x, y) =>
        {
            float dd = CaixaDist(x + 0.5f, y + 0.5f, 96, 26f), dentro = -dd;
            return C32(dentro < 4f ? Tinta : Color.white, 0.5f - dd);
        });

        /// <summary>Seta do tour apontando para baixo (cabeca triangular embaixo, haste em cima); o Tour gira 180 graus quando o alvo esta
        /// acima do cartao.</summary>
        public static Sprite Seta() => Soft("seta", (x, y) =>
        {
            float cabeca = Mathf.Clamp01(((y + 0.95f) * 0.95f - Mathf.Abs(x)) * 12f) * Mathf.Clamp01((0.1f - y) * 12f);
            float haste = Mathf.Clamp01((0.24f - Mathf.Abs(x)) * 12f) * Mathf.Clamp01((y + 0.05f) * 12f) * Mathf.Clamp01((0.95f - y) * 12f);
            return Mathf.Max(cabeca, haste);
        });

        static readonly Color MadeiraClara = Hex(0x9C6638), MadeiraEscura = Hex(0x633B1D), MadeiraBrilho = Hex(0xC98E55),
            MadeiraFundo = Hex(0x3F2512), Rebite = Hex(0xF2C14E), Pergaminho0 = Hex(0xF6E7C6), Pergaminho1 = Hex(0xD9BD8C);

        /// <summary>Moldura de madeira dos cartoes (contorno de tinta, tabua de 11 px com fio de luz por fora e sombra por dentro, rebites
        /// dourados nos cantos). O miolo fica coberto pelo pergaminho do cartao.</summary>
        public static Sprite Moldura() => Nove("moldura9", 128, 40, (x, y) =>
        {
            float px = x + 0.5f, py = y + 0.5f, d = CaixaDist(px, py, 128, 22f), dentro = -d;
            Color c;
            if (dentro < 4f) c = Tinta;
            else if (dentro < 15f)
            {
                c = Color.Lerp(MadeiraEscura, MadeiraClara, py / 128f);
                if (dentro < 6f) c = MadeiraBrilho;
                else if (dentro > 13f) c = MadeiraFundo;
                float rx = Mathf.Min(px, 128f - px) - 14f, ry = Mathf.Min(py, 128f - py) - 14f, r2 = rx * rx + ry * ry;   // rebite no canto
                if (r2 < 30f) c = r2 < 6f && rx < 0f && ry > 0f ? Color.Lerp(Rebite, Color.white, 0.55f) : r2 < 20f ? Rebite : Tinta;
            }
            else c = Pergaminho0;
            return C32(c, 0.5f - d);
        });

        /// <summary>Face de pergaminho dos cartoes: papel claro que escurece perto da moldura (sombra por dentro).</summary>
        public static Sprite Pergaminho() => Nove("pergaminho9", 64, 20, (x, y) =>
        {
            float d = CaixaDist(x + 0.5f, y + 0.5f, 64, 12f), t = Mathf.Clamp01(1f + d / 12f);
            return C32(Color.Lerp(Pergaminho0, Pergaminho1, t * t * 0.8f), 0.5f - d);
        });

        public static RectTransform Node(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image i = Node(parent, name, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            i.sprite = sprite; i.color = color; i.preserveAspect = true; i.raycastTarget = false;
            return i;
        }

        public static Text NewText(Transform parent, string name, int size, Vector2 anchorMin, Vector2 anchorMax, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Node(parent, name, anchorMin, anchorMax).gameObject.AddComponent<Text>();
            t.font = Font(); t.fontSize = size; t.color = Ink; t.alignment = align; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Contorno escuro + sombra: le sobre qualquer fundo.</summary>
        public static Text Outlined(Text t, float px = 2f)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = A(Tinta, 0.95f); o.effectDistance = new Vector2(px, -px);
            return t;
        }

        /// <summary>Retangulo arredondado 9-fatias para uGUI (canto de ~20 px na referencia).</summary>
        public static Sprite Box()
        {
            if (Cache.TryGetValue("box9", out Sprite s) && s != null) return s;
            Sprite m = Get("box9mask", (x, y) =>
            {
                const float r = 0.62f;
                float qx = Mathf.Max(Mathf.Abs(x) - (1f - r), 0f), qy = Mathf.Max(Mathf.Abs(y) - (1f - r), 0f);
                return qx * qx + qy * qy <= r * r;
            });
            s = Sprite.Create(m.texture, new Rect(0, 0, Side, Side), new Vector2(0.5f, 0.5f), 200f, 0, SpriteMeshType.FullRect, new Vector4(44, 44, 44, 44));
            Cache["box9"] = s;
            return s;
        }

        public static Image Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var img = Node(parent, name, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            img.sprite = Box(); img.type = Image.Type.Sliced; img.color = color;
            return img;
        }

        /// <summary>Botao: painel arredondado + rotulo em negrito. Estados normal/aceso/apertado/desligado pelo ColorTint padrao do uGUI.
        /// som = "clique" (docs/AUDIO.md §1) ou "confirmar" nos botoes principais (jogar, comprar).</summary>
        public static Button NewButton(Transform parent, string label, int size, Color color, Vector2 anchorMin, Vector2 anchorMax, Action onClick, string som = "clique")
        {
            var img = Node(parent, "Btn " + label, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            img.sprite = Botao(); img.type = Image.Type.Sliced; img.color = color;
            Image brilho = Node(img.transform, "Brilho", new Vector2(0f, 0.56f), Vector2.one).gameObject.AddComponent<Image>();   // verniz da metade de cima
            brilho.sprite = Box(); brilho.type = Image.Type.Sliced; brilho.color = new Color(1f, 1f, 1f, 0.17f); brilho.raycastTarget = false;
            brilho.rectTransform.offsetMin = new Vector2(8f, 0f); brilho.rectTransform.offsetMax = new Vector2(-8f, -7f);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cores = b.colors; cores.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = cores;
            img.gameObject.AddComponent<Aperta>();
            b.onClick.AddListener(() => { Audio.Play(som); onClick(); });
            Text t = NewText(img.transform, "Rotulo", size, Vector2.zero, Vector2.one);
            t.rectTransform.offsetMin = new Vector2(10f, 14f); t.rectTransform.offsetMax = new Vector2(-10f, -4f);   // fora do contorno e da aba
            t.text = label; t.font = Titulo(); t.color = Ink;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(10, size / 2); t.resizeTextMaxSize = size;
            Letreiro(t, size);
            return b;
        }

        /// <summary>Texto claro de botao/titulo: contorno de tinta proporcional ao corpo e sombra caida (le sobre qualquer cor de botao).</summary>
        public static Text Letreiro(Text t, int size)
        {
            Outlined(t, Mathf.Max(2f, size / 16f));
            Shadow sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = A(Tinta, 0.85f); sh.effectDistance = new Vector2(0f, -Mathf.Max(2f, size / 12f));
            return t;
        }

        /// <summary>Aumenta a area de toque de um botao pequeno sem mudar o desenho: filho transparente que recebe o raycast (o clique
        /// sobe para o Button do pai). pad em px de referencia para cada lado.</summary>
        public static void Toque(Component botao, float padX, float padY)
        {
            Image hit = Node(botao.transform, "AreaDeToque", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.rectTransform.offsetMin = new Vector2(-padX, -padY); hit.rectTransform.offsetMax = new Vector2(padX, padY);
            hit.rectTransform.SetAsFirstSibling();
        }
    }

    /// <summary>Botao afunda um pouco enquanto o dedo esta em cima (resposta ao toque alem da cor; so se o botao estiver ativo).</summary>
    public sealed class Aperta : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Selectable _s;
        public void OnPointerDown(PointerEventData e)
        {
            if (_s == null) _s = GetComponent<Selectable>();
            if (_s == null || _s.IsInteractable()) transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        }
        public void OnPointerUp(PointerEventData e) => transform.localScale = Vector3.one;
        public void OnPointerExit(PointerEventData e) => transform.localScale = Vector3.one;
    }

    /// <summary>Ajustes do jogador no PlayerPrefs, fora do save de progresso (valem no -testsession): volume de Som e de Musica (o Audio
    /// guarda 0..1 em dc.som/dc.musica; a tela mostra 10 degraus), Vibracao (dc_vibra; so no Android, Handheld.Vibrate; no PC aparece
    /// "so no celular") e Idioma (dc_idioma "pt"/"en"/"es"/"id"; sem escolha gravada vale o idioma do aparelho: portugues, espanhol,
    /// indonesio ou, nos outros, ingles).</summary>
    public static class Ajustes
    {
        const string VibraKey = "dc_vibra", IdiomaKey = "dc_idioma", SomV01 = "dc_som";
        public const int VolMax = 10;
        public static readonly bool TemVibra = Application.platform == RuntimePlatform.Android;
        static int _vibra = -1;
        static float _last = -99f;

        /// <summary>Le o idioma; o som desligado da v0.1 (dc_som = 0) vira volume 0. Chamar no Awake, antes de montar as telas.</summary>
        public static void Init()
        {
            Textos.Lang = Textos.DeCodigo(PlayerPrefs.GetString(IdiomaKey, "")) ?? DoAparelho();
            if (!PlayerPrefs.HasKey("dc.som") && PlayerPrefs.GetInt(SomV01, 1) == 0) SetVol(0, VolMusica);
        }

        static Textos.Lingua DoAparelho()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Portuguese: return Textos.Lingua.Pt;
                case SystemLanguage.Spanish: return Textos.Lingua.Es;
                case SystemLanguage.Indonesian: return Textos.Lingua.Id;
                default: return Textos.Lingua.En;
            }
        }

        /// <summary>Escolha do jogador (Configuracoes): troca e grava.</summary>
        public static Textos.Lingua Idioma
        {
            get => Textos.Lang;
            set { Textos.Lang = value; PlayerPrefs.SetString(IdiomaKey, Textos.Codigos[(int)value]); PlayerPrefs.Save(); }
        }

        /// <summary>0..10 (degraus da tela) sobre o Audio.Som/Musica 0..1.</summary>
        public static int VolSom { get => Mathf.RoundToInt(Audio.Som * VolMax); set => SetVol(value, VolMusica); }
        public static int VolMusica { get => Mathf.RoundToInt(Audio.Musica * VolMax); set => SetVol(VolSom, value); }

        static void SetVol(int som, int musica)
        {
            Audio.SetVolumes(Mathf.Clamp(som, 0, VolMax) / (float)VolMax, Mathf.Clamp(musica, 0, VolMax) / (float)VolMax);
            PlayerPrefs.Save();   // o Audio.SetVolumes nao grava sozinho
        }

        public static bool Vibra
        {
            get { if (_vibra < 0) _vibra = PlayerPrefs.GetInt(VibraKey, 1); return TemVibra && _vibra == 1; }
            set { _vibra = value ? 1 : 0; PlayerPrefs.SetInt(VibraKey, _vibra); PlayerPrefs.Save(); }
        }

        /// <summary>Vibra (carga atingida, explosao, pancada do Ogro, entrega perfeita), no maximo 1 a cada `gap` s. ponytail:
        /// Handheld.Vibrate tem duracao fixa (~0,4 s); se ficar longo demais no aparelho, trocar por Vibrator.vibrate(ms) via JNI como o
        /// Ajustes.Pulso do Forge Street.</summary>
        public static void Vibrar(float gap = 0.3f)
        {
            if (!Vibra || Time.unscaledTime - _last < gap) return;
            _last = Time.unscaledTime;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
