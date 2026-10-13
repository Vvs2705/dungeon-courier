using System.Collections.Generic;
using DC.Core;
using UnityEngine;
using SortingGroup = UnityEngine.Rendering.SortingGroup;

namespace DC
{
    /// <summary>
    /// Mundo das regioes 2-4 (CONTEUDO_REGIOES §1-§4, ART_BIBLE §12, AUDIO §1.1), parte do WorldView: so le o Core.
    /// - Tema por regiao (RoomDef.Region): piso e parede (`piso_mina`/`parede_mina`; cripta e forja quando a raia G copiar, senao a
    ///   textura da regiao 1 tingida para a paleta), cores de parede e bloco, arco tingido, decoracao da parede de fundo (`tocha`,
    ///   `lanterna_mina`, `vela`, `braseiro`) e props nos obstaculos (`pilar`, `escora`, `lapide`, `sarcofago`, `bigorna`).
    /// - Terreno pelo RoomDef: agua (brilho, ondas no pe, respingo e som `agua` no passo), trilho (dormentes, aviso vermelho e sino com o
    ///   vagonete em Telegraph), lava (crosta, miolo pulsando, onda e bolhas), respiro (run.VentState: brilho de aviso e fogo),
    ///   bau enterrado (X com brilho so quando run.Revealed; arco de progresso; buraco aberto), tochas de jogo da sala escura.
    /// - Escuro (run.Lit: sala escura, Midnight ou Neblina): veu #0B0D16 a 70% fora da luz (textura 36 x 56 refeita por quadro, sem
    ///   alocar), com as mesmas luzes do Core (run.InLight: courier, tocha, lanterna no chao). Inimigo fora da luz = olhos e sombra.
    ///   Telegraph, projeteis, teia e lava ficam acima do veu (sempre visiveis).
    /// - Listas da visita publicadas pelas raias (Run.Regiao2-4.cs): teias (run.Webs), avisos (run.Warns, run.Forge.Marks), fogo no chao
    ///   (run.Forge.Fires), pedras do Golem (run.Rocks), ossos (run.Bones), cuspe (run.Spits).
    /// - Eventos das raias de regiao ligados pelo NOME (Enum.TryParse no carregamento): renomear um evento no Core nao quebra a View.
    /// </summary>
    public sealed partial class WorldView
    {
        // veu do escuro acima do chao, dos blocos e das paredes laterais; abaixo de teia/lava (6), sombras de contato (7) e telegraph (8+)
        const int OVeil = 5, OOver = 6;
        const int VeilW = 36, VeilH = 56;   // celulas de 0,25 m na sala de 9 x 14 m
        // balao: 1 m acima das maos
        const float BalloonLift = 1.0f;
        const float CollapseTime = 0.3f;   // o Collapse do esqueleto desmonta em 0,3 s (CONTEUDO_REGIOES §12.2)

        // ------------------------------------------------------------------ tema por regiao

        /// <summary>Paleta e arte do cenario de uma regiao (ART_BIBLE §12: sempre sem cor de papel).</summary>
        sealed class Tema
        {
            public string Piso, Parede, Decor;
            public float PisoM, ParedeM, EarthA;
            /// <summary>PisoTint/ParedeTint = tinta da textura propria; PisoFall/ParedeFall = tinta da textura da regiao 1 quando a propria
            /// ainda nao existe (multiplicar so escurece: a media de `piso` (64,76,81) e de `parede` (55,68,74) vai para a paleta).</summary>
            public Color PisoTint, PisoFall, ParedeTint, ParedeFall, WallTop, Wall, Skirt, Block, BlockTop, BlockEdge, Arch, Light, Earth;
        }

        static readonly Tema[] Temas =
        {
            new Tema   // 1 Poroes da Guilda (ART_BIBLE §3)
            {
                Piso = "piso", Parede = "parede", Decor = "tocha", PisoM = 8f, ParedeM = 2.56f,
                PisoTint = new Color(0.90f, 0.83f, 1f), PisoFall = Color.white, ParedeTint = new Color(0.97f, 0.85f, 1f), ParedeFall = new Color(0.97f, 0.85f, 1f),
                WallTop = Art.WallTop, Wall = Art.Wall, Skirt = Art.Skirt, Block = Art.Block, BlockTop = Art.BlockTop, BlockEdge = Art.BlockEdge,
                Arch = Color.white, Light = Art.Hex(0xFFB45A),
            },
            new Tema   // 2 Minas Alagadas: terra e madeira; o `piso_mina` tem pocas turquesa pintadas que pareciam a agua de jogo: a tinta
            {          // quente tira o azul (turquesa vira lama esverdeada) e o veu de terra a 38% apaga o brilho
                Piso = "piso_mina", Parede = "parede_mina", Decor = "lanterna_mina", PisoM = 6f, ParedeM = 3f, EarthA = 0.38f, Earth = Art.Hex(0x4A4540),
                PisoTint = new Color(1f, 0.8f, 0.62f), PisoFall = new Color(1f, 0.9f, 0.79f), ParedeTint = new Color(0.95f, 0.95f, 0.95f), ParedeFall = new Color(1f, 0.85f, 0.75f),
                WallTop = Art.Hex(0x5A5249), Wall = Art.Hex(0x433D37), Skirt = Art.Hex(0x2A2622), Block = Art.Hex(0x433D37), BlockTop = Art.Hex(0x5A5249), BlockEdge = Art.Hex(0x7A6E60),
                Arch = new Color(1f, 0.88f, 0.75f), Light = Art.Hex(0xFFD27A),
            },
            new Tema   // 3 Cripta Assombrada: lajes frias e ambar
            {
                Piso = "piso_cripta", Parede = "parede_cripta", Decor = "vela", PisoM = 8f, ParedeM = 2.56f,
                PisoTint = new Color(0.9f, 0.9f, 0.95f), PisoFall = new Color(0.73f, 0.67f, 0.79f), ParedeTint = new Color(0.9f, 0.9f, 0.95f), ParedeFall = new Color(0.95f, 0.88f, 1f),
                WallTop = Art.Hex(0x3A3F4F), Wall = Art.Hex(0x2C303C), Skirt = Art.Hex(0x1C1F28), Block = Art.Hex(0x3A3F4F), BlockTop = Art.Hex(0x454B5C), BlockEdge = Art.Hex(0x6E7385),
                Arch = new Color(0.8f, 0.85f, 1f), Light = Art.Hex(0xFFB347),
            },
            new Tema   // 4 Forja do Dragao: pedra escura e laranja de lava
            {
                Piso = "piso_forja", Parede = "parede_forja", Decor = "braseiro", PisoM = 8f, ParedeM = 2.56f,
                PisoTint = new Color(0.9f, 0.9f, 0.9f), PisoFall = new Color(0.83f, 0.63f, 0.72f), ParedeTint = new Color(0.9f, 0.9f, 0.9f), ParedeFall = new Color(1f, 0.85f, 0.86f),
                WallTop = Art.Hex(0x423A40), Wall = Art.Hex(0x332D33), Skirt = Art.Hex(0x201C22), Block = Art.Hex(0x3E3640), BlockTop = Art.Hex(0x4A4350), BlockEdge = Art.Hex(0x6A5F6E),
                Arch = new Color(0.85f, 0.75f, 0.75f), Light = Art.Hex(0xFF8C1A),
            },
        };

        Tema _tema = Temas[0];
        static Tema TemaDe(int region) => Temas[Mathf.Clamp(region - 1, 0, Temas.Length - 1)];

        static readonly Dictionary<string, Sprite> Tex = new Dictionary<string, Sprite>();
        /// <summary>Textura de Resources/Textures (cache, inclusive a falta) como sprite Tiled; -nographics = null (procedural).</summary>
        static Sprite TexDe(string name, float metres)
        {
            if (Tex.TryGetValue(name, out Sprite s)) return s;
            s = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null ? null : Tiling("Textures/" + name, metres);
            Tex[name] = s;
            return s;
        }

        static Sprite PisoDe(Tema t, out Color tint)
        {
            Sprite s = TexDe(t.Piso, t.PisoM);
            if (s != null) { tint = t.PisoTint; return s; }
            s = TexDe("piso", FloorMetres);
            tint = s != null ? Mul(Temas[0].PisoTint, t.PisoFall) : t.PisoFall;
            return s != null ? s : Art.Floor();
        }

        static Sprite ParedeDe(Tema t, out Color tint)
        {
            Sprite s = TexDe(t.Parede, t.ParedeM);
            if (s != null) { tint = t.ParedeTint; return s; }
            s = TexDe("parede", WallMetres);
            tint = t.ParedeFall;
            return s;
        }

        static Color Mul(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);

        /// <summary>Prop em pe para o obstaculo pela regiao e pela medida (CONTEUDO_REGIOES §12): quadrado de 1-1,2 m = `pilar` (`escora` nas
        /// minas); cripta 1,2 x 0,6 = `lapide` e 0,8 x 1,8 = `sarcofago`; forja 1,2 x 0,8 = `bigorna`. k = escala do sprite. null = bloco.</summary>
        SpriteSheet PropDe(Core.Rect o, out float k)
        {
            float sx = 2f * o.H.X, sy = 2f * o.H.Y;
            int reg = _run != null ? _run.Room.Region : 1;
            k = 1f;
            if (Mathf.Abs(sx - sy) < 0.02f && sx >= 0.95f && sx <= 1.25f)
            {
                k = sx / PilarW;
                return reg == 2 ? SpriteSheet.Get("escora") ?? SpriteSheet.Get("pilar") : SpriteSheet.Get("pilar");
            }
            if (reg == 3 && Near(sx, 1.2f) && Near(sy, 0.6f)) { k = sx / 1.2f; return SpriteSheet.Get("lapide"); }
            if (reg == 3 && Near(sx, 0.8f) && Near(sy, 1.8f)) return SpriteSheet.Get("sarcofago");
            if (reg == 4 && Near(sx, 1.2f) && Near(sy, 0.8f)) return SpriteSheet.Get("bigorna");
            return null;
        }

        static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.06f;

        /// <summary>Lapide procedural da cripta (sem a folha `lapide`): pedra #6E7385 de topo redondo com caveira entalhada e musgo, em pe e
        /// ordenada pelo pe; vale para os obstaculos baixos e largos (0,8-1,3 x 0,5-0,7 m), que no B3 sao as lapides do Necromante.</summary>
        bool LapideProcedural(Transform r, Core.Rect o)
        {
            float sx = 2f * o.H.X, sy = 2f * o.H.Y;
            if (_run == null || _run.Room.Region != 3 || sx < 0.8f || sx > 1.3f || sy < 0.5f || sy > 0.7f) return false;
            Rect(r, "Base", o.C.X, o.C.Y, sx, sy, Art.Hex(0x2C303C), OBlock);
            Transform pu = Upright(r, "Lapide", new Vector2(o.C.X, o.C.Y + o.H.Y * 0.4f), out SortingGroup _);
            float w = sx * 0.85f, h = 0.95f;
            SpriteRenderer stone = Art.NewSprite(pu, "Pedra", Tomb(), Art.Hex(0x6E7385), 0, new Vector2(0f, h / 2f), new Vector2(w, h));
            var pv = new PillarV
            {
                R = stone, Base = stone.color, X = o.C.X, HalfW = o.H.X, Back = o.C.Y + o.H.Y, Reach = o.C.Y + h / Tilt,
                More = new[]
                {
                    Art.NewSprite(pu, "Luz", Tomb(), Art.Hex(0x858AA0), 1, new Vector2(-0.04f, h / 2f + 0.04f), new Vector2(w * 0.82f, h * 0.88f)),
                    Art.NewSprite(pu, "Caveira", Art.Skull(), Art.Hex(0x3A3F4F), 2, new Vector2(0f, h * 0.55f), Vector2.one * 0.3f),
                    Art.NewSprite(pu, "Musgo", Art.Disc(), Art.Hex(0x4E6A4A), 2, new Vector2(w * 0.3f, 0.12f), new Vector2(0.22f, 0.12f)),
                },
            };
            pv.MoreBase = new Color[pv.More.Length];
            for (int i = 0; i < pv.More.Length; i++) pv.MoreBase[i] = pv.More[i].color;
            _pillars.Add(pv);
            return true;
        }

        /// <summary>Chefe da sala (o 1o spawn com EnemyDef.Boss), ou null.</summary>
        static EnemyType? BossDe(RoomDef room)
        {
            foreach (Spawn s in room.Spawns) if ((int)s.Type < EnemyDef.All.Length && EnemyDef.All[(int)s.Type].Boss) return s.Type;
            return null;
        }

        /// <summary>Marcas das fases na barra do chefe (CONTEUDO_REGIOES §5.2): Rainha/Golem/Capataz 50%; Viuva 50 e 25 (chama aranhas);
        /// Necromante 50 e 20; Dragao 60 e 30.</summary>
        void BossTicks(Transform up, EnemyType boss, float x0, float w, float y)
        {
            float a = 0f, b = 0f;
            switch (boss)
            {
                case EnemyType.MoleQueen: case EnemyType.StoneGolem: case EnemyType.KoboldForeman: a = 0.5f; break;
                case EnemyType.SpiderWidow: a = 0.5f; b = 0.25f; break;
                case EnemyType.Necromancer: a = 0.5f; b = 0.2f; break;
                case EnemyType.ElderDragon: a = 0.6f; b = 0.3f; break;
            }
            if (a > 0f) _bossParts.Add(Rect(up, "Fase", x0 + w * a, y, 0.05f, BossH + 0.12f, Art.Ink, OPlate + 5));
            if (b > 0f) _bossParts.Add(Rect(up, "Fase", x0 + w * b, y, 0.05f, BossH + 0.12f, Art.Ink, OPlate + 5));
        }

        // folha do chefe caido por tipo (`<ator>_fim`, Dying); o Ogro tem a sua (_sOgroFim)
        static readonly string[] FimSheet =
        {
            null, null, null, null, "ogro_fim", null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            "rainha_toupeira_fim|rainha_fim", "golem_fim", "viuva_fim", "necromante_fim", "capataz_fim", "dragao_fim", null, null,
        };
        static SpriteSheet FimDe(EnemyType t) => SheetAt(FimSheet, (int)t);

        /// <summary>Placa da porta: pa (bau enterrado) e rachadura (parede disfarcada) na sala de destino, so com run.Revealed (Mapa, Faro de
        /// bau, Treasure Detour), a esquerda da placa (o bau com as moedas fica a direita).</summary>
        void PlateRegiao(Transform up, float x, float pw, Door d)
        {
            if (_run == null || !_run.Revealed || d.Room < 0 || d.Room >= Content.Rooms.Length) return;
            RoomDef to = Content.Rooms[d.Room];
            float ix = x - pw / 2f - 0.3f;
            if (to.Buried.Length > 0)
            {
                Art.NewSprite(up, "PaFundo", Art.Disc(), Art.Tinta, OPlate, new Vector2(ix, _plateS), Vector2.one * 0.44f);
                Art.NewSprite(up, "Pa", Shovel(), Art.Coin, OPlate + 1, new Vector2(ix, _plateS), Vector2.one * 0.36f);
                ix -= 0.48f;
            }
            if (to.Secret >= 0 && to.SecretHidden)
            {
                Art.NewSprite(up, "RachaFundo", Art.Disc(), Art.Tinta, OPlate, new Vector2(ix, _plateS), Vector2.one * 0.44f);
                Art.NewSprite(up, "Racha", Art.Cracks(3), Art.Gold, OPlate + 1, new Vector2(ix, _plateS), Vector2.one * 0.36f);
            }
        }

        // ------------------------------------------------------------------ mascaras procedurais (o Art.Get e privado: mesmo metodo, cache proprio)

        static readonly Dictionary<string, Sprite> Masks = new Dictionary<string, Sprite>();
        static Sprite Mask(string key, System.Func<float, float, bool> inside)
        {
            if (Masks.TryGetValue(key, out Sprite s) && s != null) return s;
            const int n = 128;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int k = 0;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                            if (inside((x + 0.25f + sx * 0.5f) / n * 2f - 1f, (y + 0.25f + sy * 0.5f) / n * 2f - 1f)) k++;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(k * 63 + (k == 4 ? 3 : 0)));
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontUnloadUnusedAsset };
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            Masks[key] = s;
            return s;
        }

        static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay, t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            float ex = px - (ax + t * dx), ey = py - (ay + t * dy);
            return Mathf.Sqrt(ex * ex + ey * ey);
        }

        /// <summary>Teia: 8 raios e 3 aneis finos.</summary>
        static Sprite WebShape() => Mask("web", (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            if (d > 0.96f) return false;
            float a = Mathf.Atan2(y, x) / (Mathf.PI * 2f) * 8f, spoke = Mathf.Abs(Mathf.Repeat(a + 0.5f, 1f) - 0.5f) * d * 0.785f;
            return spoke < 0.022f || Mathf.Abs(Mathf.Repeat(d * 3.2f + 0.15f * Mathf.Abs(Mathf.Repeat(a, 1f) - 0.5f), 1f) - 0.5f) > 0.45f;
        });
        /// <summary>Fantasma de lencol: cabeca redonda e barra ondulada.</summary>
        static Sprite GhostShape() => Mask("ghost", (x, y) =>
            x * x + (y - 0.2f) * (y - 0.2f) <= 0.5f || (Mathf.Abs(x) < 0.7f && y < 0.2f && y > -0.72f + 0.16f * Mathf.Sin(x * 11f)));
        /// <summary>8 pernas articuladas da aranha (por baixo do corpo).</summary>
        static Sprite Legs() => Mask("legs", (x, y) =>
        {
            float ax = Mathf.Abs(x);
            for (int i = 0; i < 4; i++)
            {
                float y0 = 0.3f - 0.22f * i, ky = y0 + 0.28f - 0.06f * i, fy = y0 - 0.25f - 0.12f * i;
                if (Seg(ax, y, 0.15f, y0, 0.6f, ky) < 0.06f || Seg(ax, y, 0.6f, ky, 0.95f, fy) < 0.05f) return true;
            }
            return false;
        });
        static Sprite Zed() => Mask("zed", (x, y) =>
            Mathf.Abs(x) < 0.6f && (Mathf.Abs(y - 0.62f) < 0.16f || Mathf.Abs(y + 0.62f) < 0.16f || Mathf.Abs(y - 1.03f * x) < 0.2f));
        /// <summary>Grade redonda do respiro: aro e 4 barras.</summary>
        static Sprite Grate() => Mask("grate", (x, y) =>
        {
            float d = x * x + y * y;
            return d <= 0.96f && (d >= 0.7f || (d < 0.7f && (Mathf.Abs(Mathf.Repeat((x + 1f) / 0.4f, 1f) - 0.5f) > 0.36f || Mathf.Abs(y) < 0.07f)));
        });
        static Sprite Shovel() => Mask("shovel", (x, y) =>
            Seg(x, y, -0.55f, 0.55f, 0.2f, -0.2f) < 0.09f || Mathf.Abs(x + y + 0.6f) < 0.36f && Seg(x, y, 0.15f, -0.15f, 0.62f, -0.62f) < 0.3f
            || Seg(x, y, -0.75f, 0.45f, -0.45f, 0.75f) < 0.08f);
        /// <summary>Lapide: retangulo com topo redondo.</summary>
        static Sprite Tomb() => Mask("tomb", (x, y) => Mathf.Abs(x) < 0.9f && (y < 0.1f && y > -0.96f || x * x + (y - 0.1f) * (y - 0.1f) < 0.81f));
        /// <summary>Cone de 60 graus apontando para +x (sopro do dragao) e a borda dele.</summary>
        static Sprite Cone() => Mask("cone", (x, y) => x > 0f && x * x + y * y <= 0.96f && Mathf.Abs(Mathf.Atan2(y, x)) <= Mathf.PI / 6f);
        static Sprite ConeEdge() => Mask("cone2", (x, y) =>
        {
            float d = x * x + y * y, a = Mathf.Abs(Mathf.Atan2(y, x));
            return x > 0f && d <= 0.96f && a <= Mathf.PI / 6f && (d >= 0.84f || a > Mathf.PI / 6f - 0.05f / Mathf.Max(0.1f, Mathf.Sqrt(d)));
        });

        // ------------------------------------------------------------------ terreno

        sealed class WaterV { public SpriteRenderer R; public Core.Rect Box; public float Phase; }
        sealed class RailV { public V2 A, B; public int Index; public SpriteRenderer Warn, BellA, BellB; }
        sealed class LavaV { public SpriteRenderer Core, Wave; public Core.Rect Box; }
        sealed class VentV { public Vent V; public SpriteRenderer Grate, Glow, Fire; public bool Proc; }
        sealed class BuriedV { public SpriteRenderer X1, X2, Glow, Arc, Hole; }
        /// <summary>Peca de pool sem dono fixo (teia, aviso, fogo no chao, pedra, ossos, cuspe): A/B/C renderers, Root no _root.</summary>
        sealed class PoolV { public Transform Root; public SpriteRenderer A, B, C; }

        readonly List<WaterV> _water = new List<WaterV>();
        readonly List<RailV> _rails = new List<RailV>();
        readonly List<LavaV> _lava = new List<LavaV>();
        readonly List<VentV> _vents = new List<VentV>();
        readonly List<BuriedV> _buried = new List<BuriedV>();
        readonly List<PoolV> _webV = new List<PoolV>(), _markV = new List<PoolV>(), _fireV = new List<PoolV>(), _rockV = new List<PoolV>(),
            _boneV = new List<PoolV>(), _spitV = new List<PoolV>();
        float _splashT, _bubbleT, _fireT, _dropT, _sparkleT, _lampT;
        bool _wasRefreeze;
        SpriteRenderer _stuck;   // teia sobre o courier preso (run.Webbed)

        static readonly Color WaterC = Art.Hex(0x2D5F73), WaterHi = Art.Hex(0x7FC9E0), RailC = Art.Hex(0x5E6670), Sleeper = Art.Hex(0x5C4632),
            LavaC = Art.Hex(0xFF6A00), LavaCore = Art.Hex(0xFFC14D), Crust = Art.Hex(0x7A2E12), VentOffC = Art.Hex(0x5A4A44), VentHot = Art.Hex(0xFF8C1A),
            WebC = Art.Hex(0xE8E8F0), Undead = Art.Hex(0x7CFF9E), Lilac = Art.Hex(0xB58CFF), Veil = Art.Hex(0x0B0D16), IceC = Art.Hex(0xBFEFFF),
            RockC = Art.Hex(0x6E7A8A), Dirt = Art.Hex(0x6A4A30);

        /// <summary>Monta o terreno fixo da sala (agua, trilho, lava, respiro, bau enterrado) e as tochas de jogo do escuro. Chamado no fim do
        /// BuildRoom. O que muda na visita (teias, avisos, fogo, pedras, ossos, cuspes) vem das listas do Core em pools (DrawRegiao).</summary>
        void BuildRegiao(Run run, Transform r, Transform up)
        {
            _water.Clear(); _rails.Clear(); _lava.Clear(); _vents.Clear(); _buried.Clear();
            RoomDef room = run.Room;
            foreach (Core.Rect w in room.Water)
            {
                Rect(r, "Margem", w.C.X, w.C.Y, 2f * w.H.X + 0.16f, 2f * w.H.Y + 0.16f, Art.A(Art.Hex(0x34302C), 0.85f), OMark);
                Rect(r, "Agua", w.C.X, w.C.Y, 2f * w.H.X, 2f * w.H.Y, Art.A(WaterC, 0.92f), OShadow);
                int n = Mathf.Clamp(Mathf.RoundToInt(w.H.X * w.H.Y * 2f), 2, 6);
                for (int i = 0; i < n; i++)
                    _water.Add(new WaterV { Box = w, Phase = i * 0.37f + w.C.X, R = Art.NewSprite(r, "Brilho", Art.Glow(), Art.A(WaterHi, 0.5f), OShadow + 1, V(w.C), new Vector2(0.7f, 0.16f)) });
            }
            for (int i = 0; i < room.Rails.Length; i++) { RailV rv = BuildRail(r, room.Rails[i]); rv.Index = i; _rails.Add(rv); }
            foreach (Core.Rect lv in room.Lava)
            {
                Rect(r, "Calor", lv.C.X, lv.C.Y, 2f * lv.H.X + 0.9f, 2f * lv.H.Y + 0.9f, Art.A(LavaC, 0.12f), OShadow);
                Rect(r, "Crosta", lv.C.X, lv.C.Y, 2f * lv.H.X + 0.14f, 2f * lv.H.Y + 0.14f, Crust, OOver);
                Rect(r, "Lava", lv.C.X, lv.C.Y, 2f * lv.H.X, 2f * lv.H.Y, LavaC, OOver);
                _lava.Add(new LavaV
                {
                    Box = lv,
                    Core = Rect(r, "Miolo", lv.C.X, lv.C.Y, Mathf.Max(0.1f, 2f * lv.H.X - 0.4f), Mathf.Max(0.1f, 2f * lv.H.Y - 0.4f), LavaCore, OOver + 1),
                    Wave = Art.NewSprite(r, "Onda", Art.Glow(), Art.A(LavaCore, 0.6f), OOver + 1, V(lv.C), new Vector2(0.8f, 2f * lv.H.Y)),
                });
            }
            SpriteSheet vs = SpriteSheet.Get("respiro");
            foreach (Vent vt in room.Vents)
            {
                Vector2 p = V(vt.Pos);
                var vv = new VentV { V = vt };
                if (vs != null)
                {
                    Transform vu = Upright(r, "Respiro", p);
                    float k = 2f * vt.Radius / 2f;   // folha de 2,0 m de diametro
                    vv.Grate = Art.NewSprite(vu, "Grade", vs.Frame(vs.Pick("Static"), 0, 0), Color.white, OShadow, -vs.Center * k, Vector2.one * k);
                }
                else { vv.Grate = Art.NewSprite(r, "Grade", Grate(), VentOffC, OShadow, p, Vector2.one * (2f * vt.Radius)); vv.Proc = true; }
                vv.Glow = Art.NewSprite(r, "Brasa", Art.Glow(), Art.A(VentHot, 0f), ORing - 1, p, Vector2.one * (2.6f * vt.Radius));
                vv.Fire = Art.NewSprite(r, "Fogo", Art.Disc(), Art.A(VentHot, 0f), ORing - 1, p, Vector2.one * (2f * vt.Radius));
                _vents.Add(vv);
            }
            foreach (Chest b in run.Buried)
            {
                Vector2 p = V(b.Pos);
                var bv = new BuriedV
                {
                    Hole = Art.NewSprite(r, "Buraco", Art.Disc(), Art.A(Art.Dark, 0.7f), OOver, p, new Vector2(0.8f, 0.6f)),
                    Glow = Art.NewSprite(r, "Brilho", Art.Glow(), Art.A(Art.Coin, 0.3f), OOver, p, Vector2.one * 1.4f),
                    X1 = Art.NewSprite(r, "X", Art.Square(), Art.Hex(0x5C3A1E), OOver + 1, p, new Vector2(0.7f, 0.12f)),
                    X2 = Art.NewSprite(r, "X", Art.Square(), Art.Hex(0x5C3A1E), OOver + 1, p, new Vector2(0.7f, 0.12f)),
                    Arc = Art.NewSprite(r, "Cavando", Art.Arc(0f), Art.Coin, ORing, p, Vector2.one * 1.3f),
                };
                bv.X1.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
                bv.X2.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
                _buried.Add(bv);
            }
            // tochas de jogo (sala escura): na parede lateral, com a poca de luz acima do veu
            if (run.Lit) foreach (V2 t in run.Torches) BuildWallTorch(r, t);
        }

        RailV BuildRail(Transform r, Rail rl)
        {
            Vector2 a = V(rl.A), b = V(rl.B), d = b - a;
            float len = d.magnitude, ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Vector2 dir = len > 1e-4f ? d / len : Vector2.right, nrm = new Vector2(-dir.y, dir.x);
            for (float t = 0.25f; t < len; t += 0.5f)
                Art.NewSprite(r, "Dormente", Art.Square(), Sleeper, OMark, a + dir * t, new Vector2(0.14f, 0.85f)).transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            for (int s = -1; s <= 1; s += 2)
                Art.NewSprite(r, "Trilho", Art.Square(), RailC, OShadow, (a + b) * 0.5f + nrm * (0.3f * s), new Vector2(len, 0.07f)).transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            var rv = new RailV { A = rl.A, B = rl.B };
            rv.Warn = Art.NewSprite(r, "Aviso", Art.Square(), Art.A(Art.Bad, 0f), ORing - 1, (a + b) * 0.5f, new Vector2(len, 1.2f));
            rv.Warn.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            rv.BellA = BellIcon(r, a + dir * 0.5f);
            rv.BellB = BellIcon(r, b - dir * 0.5f);
            return rv;
        }

        SpriteRenderer BellIcon(Transform r, Vector2 at)
        {
            Transform bu = Upright(r, "Sino", at);
            SpriteRenderer s = Art.NewSprite(bu, "Sino", Art.Lock(), Art.Gold, ORing + 1, new Vector2(0f, 0.55f), Vector2.one * 0.38f);
            s.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);   // o cadeado de ponta-cabeca le como sino
            s.enabled = false;
            return s;
        }

        void BuildWallTorch(Transform r, V2 at)
        {
            bool left = at.X < Balance.RoomW * 0.5f;
            Vector2 p = new Vector2(left ? 0.05f : Balance.RoomW - 0.05f, at.Y);
            Transform tu = Upright(r, "Tocha", p, out SortingGroup _);
            SpriteSheet s = SpriteSheet.Get("tocha");
            var t = new TorchV { Seed = at.Y * 1.7f, C = Flame };
            float fy;
            if (s != null) { Art.NewSprite(tu, "Tocha", s.Frame(s.Pick("Static"), 0, 0), Color.white, 1, Vector2.zero, Vector2.one); fy = 1.7f * s.Center.y; }
            else
            {
                Art.NewSprite(tu, "Suporte", Art.Square(), Art.SteelDark, 1, new Vector2(0f, 0.25f), new Vector2(0.08f, 0.5f));
                SpriteRenderer f = Art.NewSprite(tu, "Chama", Art.Triangle(), Flame, 2, new Vector2(0f, 0.62f), new Vector2(0.24f, 0.36f));
                Art.NewSprite(f.transform, "Miolo", Art.Triangle(), Art.Cream, 3, new Vector2(0f, -0.15f), new Vector2(0.5f, 0.55f));
                t.Flame = f.transform; fy = 0.62f;
            }
            t.Halo = Art.NewSprite(tu, "Brilho", Art.Glow(), Art.A(Flame, 0.4f), 0, new Vector2(0f, fy), Vector2.one * 1.2f);
            t.Pool = Art.NewSprite(r, "Luz", Art.Glow(), Art.A(Flame, 0.14f), OOver, V(at), Vector2.one * (2f * Balance.LightTorch));
            _torches.Add(t);
        }

        PoolV PoolAt(List<PoolV> pool, int i, string name)
        {
            while (pool.Count <= i)
            {
                var v = new PoolV { Root = new GameObject(name).transform };
                v.Root.SetParent(_root, false);
                v.A = Art.NewSprite(v.Root, "A", Art.Disc(), Color.white, OOver, Vector2.zero, Vector2.one);
                v.B = Art.NewSprite(v.Root, "B", Art.Disc(), Color.white, OOver, Vector2.zero, Vector2.one);
                v.C = Art.NewSprite(v.Root, "C", Art.Disc(), Color.white, OOver, Vector2.zero, Vector2.one);
                pool.Add(v);
            }
            PoolV p = pool[i];
            if (!p.Root.gameObject.activeSelf) p.Root.gameObject.SetActive(true);
            return p;
        }

        static void PoolOff(List<PoolV> pool, int from)
        {
            for (int i = from; i < pool.Count; i++) if (pool[i].Root.gameObject.activeSelf) pool[i].Root.gameObject.SetActive(false);
        }

        static void Set(SpriteRenderer s, Sprite sp, Color c, int order, Vector2 pos, Vector2 scale, float rot = 0f)
        {
            s.enabled = true; s.sprite = sp; s.color = c; s.sortingOrder = order;
            s.transform.localPosition = pos; s.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            s.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        /// <summary>Quadro a quadro do terreno, das listas da visita, do escuro e dos efeitos de passo (depois do DrawRoomV1).</summary>
        void DrawRegiao(Run run, float dt)
        {
            Courier c = run.Courier;
            // agua: brilhos andando dentro da poca; respingo, onda e som no passo do courier
            foreach (WaterV w in _water)
            {
                float u = Mathf.Repeat(_time * 0.12f + w.Phase, 1f), v2 = Mathf.Repeat(w.Phase * 2.3f, 1f);
                w.R.transform.localPosition = new Vector2(w.Box.C.X - w.Box.H.X + 0.35f + u * Mathf.Max(0.1f, 2f * w.Box.H.X - 0.7f), w.Box.C.Y - w.Box.H.Y + 0.2f + v2 * Mathf.Max(0.1f, 2f * w.Box.H.Y - 0.4f));
                w.R.color = Art.A(WaterHi, 0.55f * Mathf.Sin(u * Mathf.PI));
            }
            bool alive = c.State != CourierState.Dead && run.Running;
            if (alive && _courierSpd > 0.6f && run.Room.Water.Length > 0 && run.InWater(c.Pos) && (_splashT -= dt) <= 0f)
            {
                _splashT = 0.28f;
                Vector2 f = V(c.Pos);
                Puff(Art.ThinRing(), f, Vector2.zero, Art.A(WaterHi, 0.8f), 0.3f, 1.1f, 0.45f, ORing);
                for (int i = 0; i < 3; i++) Puff(Art.Disc(), f, Random.insideUnitCircle * 1.6f + new Vector2(0f, 0.6f), Art.A(WaterHi, 0.9f), 0.08f, 0.03f, 0.3f);
                Audio.Play("agua", 1f, 0.2f);
            }
            // trilho: acende em vermelho e o sino aparece com o vagonete no sino (R2Act.Bell)
            foreach (RailV rv in _rails)
            {
                bool warn = false; V2 cart = rv.A;
                foreach (Enemy e in run.Enemies)
                {
                    if (!e.Alive || e.Type != EnemyType.Minecart) continue;
                    R2Foe f = run.R2Of(e);
                    if (f != null ? f.Rail != rv.Index : V2.SegDist(e.Pos, rv.A, rv.B) > 0.9f) continue;
                    cart = e.Pos;
                    if (f != null ? f.Act == R2Act.Bell : e.State == EnemyState.Telegraph) warn = true;
                }
                float k = warn ? 0.35f + 0.25f * Mathf.Sin(_time * 18f) : 0f;
                rv.Warn.color = Art.A(Art.Bad, k);
                bool nearA = V2.Dist(cart, rv.A) <= V2.Dist(cart, rv.B);
                rv.BellA.enabled = warn && nearA; rv.BellB.enabled = warn && !nearA;
                if (warn) (nearA ? rv.BellA : rv.BellB).transform.localRotation = Quaternion.Euler(0f, 0f, 180f + 18f * Mathf.Sin(_time * 30f));
            }
            // lava: miolo pulsando, onda clara atravessando e bolhas
            foreach (LavaV lv in _lava)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(_time * 2.2f + lv.Box.C.X);
                lv.Core.color = Art.A(LavaCore, 0.55f + 0.35f * k);
                float u = Mathf.Repeat(_time * 0.25f + lv.Box.C.Y * 0.3f, 1f);
                lv.Wave.transform.localPosition = new Vector2(lv.Box.C.X - lv.Box.H.X + u * 2f * lv.Box.H.X, lv.Box.C.Y);
                lv.Wave.color = Art.A(LavaCore, 0.5f * Mathf.Sin(u * Mathf.PI));
            }
            if (_lava.Count > 0 && (_bubbleT -= dt) <= 0f)
            {
                _bubbleT = 0.18f;
                LavaV lv = _lava[Random.Range(0, _lava.Count)];
                Vector2 at = new Vector2(lv.Box.C.X + Random.Range(-lv.Box.H.X, lv.Box.H.X), lv.Box.C.Y + Random.Range(-lv.Box.H.Y, lv.Box.H.Y));
                Puff(Art.Disc(), at, new Vector2(0f, 0.5f), Art.A(LavaCore, 0.9f), 0.06f, 0.2f, 0.4f, OOver + 2);
            }
            // respiro: estado do Core (run.VentState: 0 apagado, 1 brilhando = aviso, 2 fogo)
            for (int i = 0; i < _vents.Count; i++)
            {
                VentV vv = _vents[i];
                int st = run.VentState(i);
                float g = st == 1 ? Mathf.Clamp01(1f - run.VentFireIn(i) / Balance.VentGlow) : 0f;
                vv.Glow.color = Art.A(VentHot, st == 1 ? 0.25f + 0.55f * g * (0.7f + 0.3f * Mathf.Sin(_time * 30f)) : st == 2 ? 0.6f : 0f);
                vv.Fire.color = Art.A(Color.Lerp(VentHot, LavaCore, 0.5f + 0.5f * Mathf.Sin(_time * 25f)), st == 2 ? 0.75f : 0f);
                vv.Grate.color = vv.Proc ? Color.Lerp(VentOffC, VentHot, st == 1 ? g : st == 2 ? 1f : 0f) : Color.Lerp(Color.white, VentHot, st == 1 ? 0.5f * g : st == 2 ? 0.5f : 0f);
                if (st == 2 && Random.value < 0.6f)
                    Puff(Art.Disc(), V(vv.V.Pos) + Random.insideUnitCircle * vv.V.Radius * 0.7f, new Vector2(0f, 2.2f), Random.value < 0.5f ? VentHot : LavaCore, 0.18f, 0.04f, 0.45f, ORing);
            }
            // bau enterrado: so revelado; arco de cavar; aberto = buraco
            for (int i = 0; i < _buried.Count; i++)
            {
                BuriedV bv = _buried[i];
                Chest b = i < run.Buried.Count ? run.Buried[i] : null;
                bool show = b != null && run.Revealed, open = show && b.Opened;
                bv.Hole.enabled = open;
                bv.X1.enabled = bv.X2.enabled = bv.Glow.enabled = show && !open;
                bool dig = show && !open && b.Progress > 0f;
                bv.Arc.enabled = dig;
                if (dig) bv.Arc.sprite = Art.Arc(b.Progress / (run.Has(Perk.ChestSense) ? Balance.BuriedSenseTime : Balance.BuriedTime));
                if (show && !open) bv.Glow.color = Art.A(Art.Coin, 0.22f + 0.15f * Mathf.Sin(_time * 4f + i));
            }
            DrawLists(run, dt);
            _biteT = Mathf.Max(0f, _biteT - dt);
            // rota segura (rewarded, run.SafeDoor): a porta brilha dourada no chao
            foreach (DoorV dv in _doors)
                if (dv.D.Index == run.SafeDoor) { dv.Glow.enabled = true; dv.Glow.color = Art.A(Art.Gold, 0.4f + 0.25f * Mathf.Sin(_time * 5f)); }
            // courier preso na teia (run.Webbed) ou agarrado pela mumia (run.GrabbedBy): teia por cima e tremor
            bool stuck = alive && run.Webbed > 0f, grabbed = alive && run.GrabbedBy != null;
            if (_stuck == null && stuck) _stuck = Art.NewSprite(_courier.Up, "Preso", WebShape(), Art.A(WebC, 0.85f), OCourier + 9, new Vector2(0f, 0.45f), Vector2.one * 1.2f);
            if (_stuck != null) _stuck.enabled = stuck;
            _courier.Up.localPosition = stuck || grabbed ? new Vector3(0.04f * Mathf.Sin(_time * 50f), 0f, 0f) : Vector3.zero;
            // props em pe (pilar, lapide...) ficam acima do veu: no escuro, fora da luz, escurecem a ~35%
            foreach (PillarV pv in _pillars) Dim(pv, run.Lit && !run.InLight(new V2(pv.X, pv.Back)));
            DrawVeil(run);
        }

        /// <summary>Listas da visita publicadas pelas raias: teias (run.Webs), avisos (run.Warns e run.Forge.Marks, sempre acima do veu),
        /// fogo no chao (run.Forge.Fires), pedras do Golem (run.Rocks), pilhas de ossos (run.Bones) e cuspe de teia (run.Spits).</summary>
        void DrawLists(Run run, float dt)
        {
            int n = 0;
            foreach (Cobweb w in run.Webs)   // teia branca a 70% (mais fraca fora da luz); a cuspida some no fim
            {
                PoolV v = PoolAt(_webV, n++, "Teia");
                v.Root.localPosition = V(w.Pos);
                float a = (!run.Lit || run.InLight(w.Pos) ? 0.7f : 0.35f) * (w.Left >= 0f ? Mathf.Clamp01(w.Left / 0.5f) : 1f);
                Set(v.A, WebShape(), Art.A(WebC, a), OOver, Vector2.zero, Vector2.one * (2f * w.Radius));
                v.B.enabled = v.C.enabled = false;
            }
            PoolOff(_webV, n);
            n = 0;
            foreach (Warn w in run.Warns) DrawWarn(PoolAt(_markV, n++, "Aviso"), w);
            foreach (ForgeMark m in run.Forge.Marks) DrawMark(PoolAt(_markV, n++, "Aviso"), m);
            PoolOff(_markV, n);
            n = 0;
            foreach (FirePatch f in run.Forge.Fires)   // fogo no chao: laranja com chamas (acima do veu)
            {
                PoolV v = PoolAt(_fireV, n++, "Fogo");
                v.Root.localPosition = V(f.Pos);
                float fade = Mathf.Clamp01(f.Left / 0.5f), k = 0.5f + 0.5f * Mathf.Sin(_time * 12f + n);
                Set(v.A, Art.Disc(), Art.A(LavaC, 0.5f * fade), OOver, Vector2.zero, Vector2.one * (2f * f.Radius));
                Set(v.B, Art.Glow(), Art.A(LavaCore, (0.35f + 0.25f * k) * fade), OOver + 1, Vector2.zero, Vector2.one * (2.2f * f.Radius));
                v.C.enabled = false;
            }
            PoolOff(_fireV, n);
            if (run.Forge.Fires.Count > 0 && (_fireT -= dt) <= 0f)
            {
                _fireT = 0.08f;
                FirePatch f = run.Forge.Fires[Random.Range(0, run.Forge.Fires.Count)];
                Puff(Art.Disc(), V(f.Pos) + Random.insideUnitCircle * f.Radius * 0.7f, new Vector2(0f, 1.8f), Random.value < 0.5f ? VentHot : LavaCore, 0.14f, 0.03f, 0.4f, ORing);
            }
            n = 0;
            foreach (Rock rk in run.Rocks)   // pedra do Golem: sombra crescendo enquanto cai; depois bloco
            {
                PoolV v = PoolAt(_rockV, n++, "Pedra");
                v.Root.localPosition = V(rk.Pos);
                float s = 2f * Balance.RockHalf;
                if (!rk.Landed)
                {
                    float k = 1f - Mathf.Clamp01(rk.Fall / Balance.RockFall), y = 3f * (1f - k) / Tilt;
                    Set(v.A, Art.Disc(), Art.A(Art.Dark, 0.3f + 0.4f * k), OOver, Vector2.zero, new Vector2(s * (0.4f + 0.6f * k), s * 0.7f * (0.4f + 0.6f * k)));
                    Set(v.B, Art.Disc(), RockC, OShot, new Vector2(0f, y), Vector2.one * s * 0.9f);
                    Set(v.C, Art.Cracks(2), Art.A(Art.Dark, 0.6f), OShot + 1, new Vector2(0f, y), Vector2.one * s * 0.7f);
                }
                else
                {
                    float fade = Mathf.Clamp01(rk.Left / 0.5f);
                    Set(v.A, Art.Rounded(), Art.A(Color.Lerp(RockC, Art.Dark, 0.35f), fade), OOver, Vector2.zero, Vector2.one * s);
                    Set(v.B, Art.Rounded(), Art.A(RockC, fade), OOver + 1, new Vector2(0f, 0.12f), new Vector2(s * 0.95f, s * 0.75f));
                    Set(v.C, Art.Cracks(2), Art.A(Art.Dark, 0.5f * fade), OOver + 2, new Vector2(0f, 0.12f), Vector2.one * s * 0.6f);
                }
            }
            PoolOff(_rockV, n);
            n = 0;
            foreach (BonePile b in run.Bones)   // pilha de ossos; anel verde enchendo ate remontar
            {
                PoolV v = PoolAt(_boneV, n++, "Ossos");
                v.Root.localPosition = V(b.Pos);
                float k = 1f - Mathf.Clamp01(b.Left / Balance.BonesRise);
                SpriteSheet sk = SpriteSheet.Get("esqueleto");
                int col = sk != null ? sk.Clip("Collapse") : -1;
                if (col >= 0)
                {
                    Set(v.A, sk.Frame(col, 0, Frac(sk, col, (Balance.BonesRise - b.Left) / CollapseTime)), Color.white, OOver + 1, Vector2.zero, new Vector2(1f, 1f / Tilt));
                    v.B.enabled = false;
                }
                else
                {
                    Set(v.A, Art.Disc(), Art.Bone, OOver + 1, Vector2.zero, new Vector2(0.6f, 0.3f), 20f);
                    Set(v.B, Art.Disc(), Art.Bone, OOver + 1, new Vector2(0.18f, 0.12f), Vector2.one * 0.28f);
                }
                Set(v.C, Art.Arc(k), Art.A(Undead, 0.5f + 0.4f * k), ORing, Vector2.zero, Vector2.one * 1.1f);
            }
            PoolOff(_boneV, n);
            n = 0;
            foreach (Arrow s in run.Spits)   // cuspe de teia em voo (projetil: sempre visivel)
            {
                PoolV v = PoolAt(_spitV, n++, "Cuspe");
                v.Root.localPosition = V(s.Pos) + new Vector2(0f, 0.35f / Tilt);
                Set(v.A, Art.Glow(), Art.A(Art.Bad, 0.4f), OShot, Vector2.zero, Vector2.one * 0.6f);
                Set(v.B, Art.Disc(), WebC, OShot + 1, Vector2.zero, Vector2.one * 0.3f);
                v.C.enabled = false;
            }
            PoolOff(_spitV, n);
        }

        /// <summary>Aviso da cripta (run.Warns): faixa/fio/raio de From a To com Width; circulo/runa em From com Radius. Faixa e circulo em
        /// vermelho de telegraph; fio lilas (mexe na carga); raio e runa em verde de morto-vivo.</summary>
        void DrawWarn(PoolV v, Warn w)
        {
            v.Root.localPosition = V(w.From);
            float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 14f);
            switch (w.Kind)
            {
                case WarnKind.Circle:
                case WarnKind.Rune:
                {
                    Color cc = w.Kind == WarnKind.Rune ? Undead : Art.Bad;
                    Set(v.A, Art.Disc(), Art.A(cc, 0.18f + 0.12f * pulse), ORing - 1, Vector2.zero, Vector2.one * (2f * w.Radius));
                    Set(v.B, Art.ThinRing(), Art.A(cc, 0.9f), ORing, Vector2.zero, Vector2.one * (2f * w.Radius));
                    if (w.Kind == WarnKind.Rune) Set(v.C, Art.Arc(0.75f), Art.A(Undead, 0.8f), ORing + 1, Vector2.zero, Vector2.one * (1.4f * w.Radius), _time * 200f);
                    else v.C.enabled = false;
                    break;
                }
                default:
                {
                    Vector2 d = V(w.To) - V(w.From);
                    float len = Mathf.Max(0.05f, d.magnitude), ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, wd = Mathf.Max(0.05f, w.Width);
                    Color cc = w.Kind == WarnKind.Thread ? Lilac : w.Kind == WarnKind.Beam ? Undead : Art.Bad;
                    float fa = w.Kind == WarnKind.Beam ? 0.85f : w.Kind == WarnKind.Thread ? 0.8f : 0.2f + 0.12f * pulse;
                    Set(v.A, Art.Square(), Art.A(cc, fa), ORing - 1, d * 0.5f, new Vector2(len, wd), ang);
                    if (w.Kind == WarnKind.Strip) Set(v.B, Art.Square(), Art.A(cc, 0.9f), ORing, d * 0.5f, new Vector2(len, 0.05f), ang);
                    else v.B.enabled = false;
                    if (w.Kind == WarnKind.Beam) Set(v.C, Art.Glow(), Art.A(Undead, 0.6f), ORing + 1, d * 0.5f, new Vector2(len, wd * 3f), ang);
                    else v.C.enabled = false;
                    break;
                }
            }
        }

        /// <summary>Aviso da forja (run.Forge.Marks): circulo (frasco, giro, pouso, troca, surgir), faixa do chicote, cone do sopro; o
        /// enchimento cresce com 1 - Left/Total e a borda fica branca no fim; troca/surgir em lilas; sopro saindo (Active) em fogo.</summary>
        void DrawMark(PoolV v, ForgeMark m)
        {
            v.Root.localPosition = V(m.Pos);
            float k = m.Total > 0f ? Mathf.Clamp01(1f - m.Left / m.Total) : 1f;
            bool last = m.Left < 0.1f, lilac = m.Kind == ForgeMarkKind.Swap || m.Kind == ForgeMarkKind.Blink;
            Color cc = lilac ? Lilac : Art.Bad;
            switch (m.Kind)
            {
                case ForgeMarkKind.Whip:
                {
                    float ang = Mathf.Atan2(m.Dir.Y, m.Dir.X) * Mathf.Rad2Deg;
                    Vector2 dir = V(m.Dir);
                    Set(v.A, Art.Square(), Art.A(cc, 0.2f), ORing - 1, dir * (m.Length * 0.5f), new Vector2(m.Length, 2f * m.Radius), ang);
                    Set(v.B, Art.Square(), Art.A(cc, 0.4f), ORing - 1, dir * (m.Length * k * 0.5f), new Vector2(Mathf.Max(0.05f, m.Length * k), 2f * m.Radius), ang);
                    Set(v.C, Art.Square(), last ? Color.white : Art.A(cc, 0.9f), ORing, dir * (m.Length * 0.5f), new Vector2(m.Length, 0.05f), ang);
                    break;
                }
                case ForgeMarkKind.Breath:
                {
                    float ang = Mathf.Atan2(m.Dir.Y, m.Dir.X) * Mathf.Rad2Deg;
                    bool fire = m.Active > 0f;
                    Set(v.A, Cone(), Art.A(fire ? LavaC : cc, fire ? 0.6f : 0.3f), ORing - 1, Vector2.zero, Vector2.one * (2f * m.Length * (fire ? 1f : Mathf.Max(0.05f, k))), ang);
                    Set(v.B, ConeEdge(), last || fire ? Color.white : Art.A(cc, 0.9f), ORing, Vector2.zero, Vector2.one * (2f * m.Length), ang);
                    if (fire && Random.value < 0.7f)
                    {
                        float a = Random.Range(-30f, 30f) * Mathf.Deg2Rad, s = Random.Range(3f, 8f);
                        Vector2 r = new Vector2(m.Dir.X * Mathf.Cos(a) - m.Dir.Y * Mathf.Sin(a), m.Dir.X * Mathf.Sin(a) + m.Dir.Y * Mathf.Cos(a));
                        Puff(Art.Disc(), V(m.Pos) + r * 0.6f, r * s, Random.value < 0.5f ? LavaC : LavaCore, 0.3f, 0.08f, 0.5f);
                    }
                    v.C.enabled = false;
                    break;
                }
                default:   // Flask, Spin, Land, Swap, Blink: circulo em Pos
                    Set(v.A, Art.Disc(), Art.A(cc, 0.25f), ORing - 1, Vector2.zero, Vector2.one * (2f * m.Radius * Mathf.Max(0.05f, k)));
                    Set(v.B, Art.ThinRing(), last ? Color.white : Art.A(cc, 0.9f), ORing, Vector2.zero, Vector2.one * (2f * m.Radius));
                    if (lilac) Set(v.C, Art.Arc(0.75f), Art.A(Lilac, 0.8f), ORing + 1, Vector2.zero, Vector2.one * (1.4f * m.Radius), -_time * 400f);
                    else v.C.enabled = false;
                    break;
            }
        }

        static void Dim(PillarV pv, bool dark)
        {
            float k = dark ? 0.35f : 1f;
            pv.R.color = new Color(pv.Base.r * k, pv.Base.g * k, pv.Base.b * k, pv.A);
            if (pv.More == null) return;
            for (int i = 0; i < pv.More.Length; i++) { Color b = pv.MoreBase[i]; pv.More[i].color = new Color(b.r * k, b.g * k, b.b * k, pv.A); }
        }

        // ------------------------------------------------------------------ escuro: veu com buracos de luz (as luzes do Core)

        SpriteRenderer _veil; Texture2D _veilTex; Color32[] _veilPx;
        readonly float[] _lx = new float[16], _ly = new float[16], _lr = new float[16];

        /// <summary>Veu #0B0D16 a 70% fora da luz (nunca preto), com as mesmas luzes do run.InLight: courier (lanterna acesa nas maos; x
        /// LightMul; - run.LightCut), tochas (- run.TorchCut) e lanterna acesa no chao; borda suave nos ultimos 30% do raio.</summary>
        void DrawVeil(Run run)
        {
            bool on = run.Lit;
            if (_veil == null)
            {
                if (!on) return;
                _veilTex = new Texture2D(VeilW, VeilH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                _veilPx = new Color32[VeilW * VeilH];
                Color32 vc = Veil;
                for (int i = 0; i < _veilPx.Length; i++) _veilPx[i] = new Color32(vc.r, vc.g, vc.b, 0);
                Sprite s = Sprite.Create(_veilTex, new UnityEngine.Rect(0, 0, VeilW, VeilH), new Vector2(0.5f, 0.5f), VeilW / Balance.RoomW, 0, SpriteMeshType.FullRect);
                _veil = Art.NewSprite(_root, "Escuro", s, Color.white, OVeil, new Vector2(Balance.RoomW / 2f, Balance.RoomH / 2f), Vector2.one);
            }
            if (_veil.enabled != on) _veil.enabled = on;
            if (!on) return;
            int n = 0;
            bool lamp = false;
            foreach (Cargo g in run.Cargos) if (g.Type == CargoType.Lantern && g.State == CargoState.Held && !g.Out) lamp = true;
            AddLight(ref n, run.Courier.Pos, (lamp ? Balance.LightLantern : Balance.LightCourier) * run.LightMul - run.LightCut);
            foreach (V2 t in run.Torches) AddLight(ref n, t, Balance.LightTorch - run.TorchCut);
            foreach (Cargo g in run.Cargos) if (g.Type == CargoType.Lantern && g.State == CargoState.Ground && !g.Out) AddLight(ref n, g.Pos, Balance.LightLanternGround);
            float cell = Balance.RoomW / VeilW;
            for (int y = 0; y < VeilH; y++)
            {
                float py = (y + 0.5f) * cell;
                for (int x = 0; x < VeilW; x++)
                {
                    float px = (x + 0.5f) * cell, l = 0f;
                    for (int i = 0; i < n; i++)
                    {
                        float dx = px - _lx[i], dy = py - _ly[i], d2 = dx * dx + dy * dy, r = _lr[i];
                        if (d2 >= r * r) continue;
                        float k = Mathf.Clamp01((r - Mathf.Sqrt(d2)) / (0.3f * r));
                        if (k > l) { l = k; if (l >= 1f) break; }
                    }
                    _veilPx[y * VeilW + x].a = (byte)(178f * (1f - l));   // 70% fora da luz (nunca preto)
                }
            }
            _veilTex.SetPixels32(_veilPx);
            _veilTex.Apply(false);
        }

        void AddLight(ref int n, V2 p, float r)
        {
            if (n >= _lx.Length || r <= 0.05f) return;
            _lx[n] = p.X; _ly[n] = p.Y; _lr[n] = r; n++;
        }

        // ------------------------------------------------------------------ eventos das regioes -> efeitos

        /// <summary>Eventos das raias R2-R4 ligados pelo NOME no carregamento (Enum.TryParse): renomear ou faltar um evento no Core nao quebra
        /// a View (so o efeito some). A/B: comentarios do topo de Run.Regiao2/3/4.cs.</summary>
        enum Nx
        {
            CageScream, CageCalm, Burrow, Summon, RockFall, EnemyRevive, BonesScatter, Webbed, Raise, Blink, LanternOut, LanternLit, Reflect, WebTorn,
            BossPhase, TakeOff, Land, PlantBite, LavaBurn, VentFire, EggHatch, ForemanCall,
        }
        static readonly Dictionary<int, Nx> Named = MakeNamed();

        static Dictionary<int, Nx> MakeNamed()
        {
            var d = new Dictionary<int, Nx>();
            foreach (Nx n in (Nx[])System.Enum.GetValues(typeof(Nx)))
                if (System.Enum.TryParse(n.ToString(), out Ev k) && !d.ContainsKey((int)k)) d[(int)k] = n;
            return d;
        }

        static Enemy EnemyById(Run run, int id)
        {
            if (run != null) foreach (Enemy x in run.Enemies) if (x.Id == id) return x;
            return null;
        }

        Ent EntById(int id)
        {
            foreach (Ent x in _enemies) if (x.Id == id) return x;
            return null;
        }

        /// <summary>Efeitos das regioes 2-4 (depois do OnEventV1).</summary>
        void OnEventRegiao(SimEvent e, Run run, Vector2 p)
        {
            if (Named.TryGetValue((int)e.Kind, out Nx nx)) { NamedFx(nx, e, run, p); return; }
            switch (e.Kind)
            {
                case Ev.Strike: if (e.B >= (int)EnemyType.GoblinMiner) StrikeFx(run, e.A, (EnemyType)e.B, p); break;
                case Ev.EnemyHit: if (EntById(e.A) is Ent h) h.HitT = 0.3f; break;
                case Ev.EnemyDie: DieFx((EnemyType)e.B, p); if (e.B == (int)EnemyType.MagmaElemental) SplitFx(e.A, p); break;
                case Ev.Explosion: if (e.A == 2) Burst(p, VentHot, 16, 5f, 0.18f, 0.5f); break;   // besouro-bomba (fonte 2)
                case Ev.CargoDrop:
                    foreach (Cargo g in run.Cargos)
                        if (g.Id == e.A && run.Room.Water.Length > 0 && run.InWater(g.Pos))
                            Puff(Art.ThinRing(), V(g.Pos), Vector2.zero, Art.A(WaterHi, 0.9f), 0.3f, 1.3f, 0.5f, ORing);
                    break;
                case Ev.RoomEnter:   // o mapa revelou algo nesta sala
                    if (run.Revealed) foreach (Chest b in run.Buried) Burst(V(b.Pos), Art.Coin, 8, 2.5f, 0.1f, 0.6f);
                    break;
            }
        }

        /// <summary>Golpe dos papeis das regioes 2-4 (Strike; Pos = onde o Core diz).</summary>
        void StrikeFx(Run run, int id, EnemyType t, Vector2 p)
        {
            Enemy en = EnemyById(run, id);
            Vector2 from = en != null ? V(en.Pos) : p;
            switch (t)
            {
                case EnemyType.GoblinMiner: Burst(p, Art.A(Art.Dim, 0.9f), 8, 2.5f, 0.14f, 0.35f); Shake(0.06f, 0.12f); break;
                case EnemyType.KoboldBomber: Burst(p, VentHot, 12, 3f, 0.16f, 0.45f); break;
                case EnemyType.Mole:
                case EnemyType.MoleQueen:
                    Burst(p, Dirt, t == EnemyType.MoleQueen ? 18 : 10, 3f, 0.16f, 0.45f);
                    if (t == EnemyType.MoleQueen) Shake(0.14f, 0.25f);
                    break;
                case EnemyType.BogFrog:
                case EnemyType.SpiderWidow:
                case EnemyType.KoboldForeman:   // lingua / fio / chicote: a linha encolhe ate quem puxou
                {
                    R2Foe f = en != null ? run.R2Of(en) : null;
                    Vector2 to = f != null && f.Act == R2Act.Tongue ? V(f.Spot) : p, d = to - from;
                    float len = d.magnitude;
                    if (len > 0.2f) Puff(Art.Square(), from + d * 0.5f, Vector2.zero, Art.A(t == EnemyType.KoboldForeman ? Art.Hex(0x7A4A2A) : t == EnemyType.BogFrog ? Art.Hex(0xE07A8A) : Lilac, 0.95f), len, len * 0.6f, 0.25f, ORing + 1, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0.12f / len);
                    break;
                }
                case EnemyType.Gargoyle:   // mergulho: poeira pela faixa ate onde pousou
                {
                    Vector2 d = p - from;
                    for (int i = 0; i < 8; i++) Puff(Art.Disc(), from + d * (i / 8f), Random.insideUnitCircle, Art.A(Art.Dim, 0.8f), 0.3f, 0.5f, 0.5f);
                    Shake(0.08f, 0.15f);
                    break;
                }
                case EnemyType.Necromancer: Puff(Art.Glow(), from, Vector2.zero, Art.A(Undead, 0.8f), 1f, 2f, 0.3f); break;   // o raio e o Warn Beam
                case EnemyType.ElderDragon: Shake(0.2f, 0.3f); break;   // o sopro sai pelo ForgeMark Breath (Active)
                case EnemyType.StoneGolem:
                case EnemyType.MagmaElemental:
                    Shake(t == EnemyType.StoneGolem ? 0.22f : 0.12f, 0.22f);
                    Burst(p, Art.A(t == EnemyType.StoneGolem ? Art.Hex(0x8A93B0) : VentHot, 0.85f), 12, 4f, 0.16f, 0.35f);
                    break;
                case EnemyType.Minecart: for (int i = 0; i < 6; i++) Puff(Art.Disc(), p, Random.insideUnitCircle * 4f, Art.Gold, 0.07f, 0.02f, 0.25f); break;
                case EnemyType.Mummy: Burst(p, Art.Hex(0xCDBE9A), 6, 2f, 0.12f, 0.3f); break;
                case EnemyType.Ghost:
                case EnemyType.Imp: Burst(p, Lilac, 8, 2.5f, 0.12f, 0.4f); break;
                case EnemyType.Salamander: Burst(p, VentHot, 6, 2.5f, 0.1f, 0.3f); break;
                case EnemyType.Spider: Burst(p, WebC, 5, 2f, 0.08f, 0.3f); break;
            }
        }

        void DieFx(EnemyType t, Vector2 p)
        {
            switch (t)
            {
                case EnemyType.MagmaElemental: Burst(p, LavaCore, 14, 4f, 0.14f, 0.5f); break;
                case EnemyType.SalamanderEgg: Burst(p, Art.Hex(0x2A2426), 10, 3f, 0.12f, 0.4f); Burst(p, VentHot, 6, 3f, 0.08f, 0.3f); break;
                case EnemyType.Ghost: Puff(Art.Glow(), p + new Vector2(0f, 0.6f), new Vector2(0f, 1.5f), Art.A(Art.Hex(0xBFF3FF), 0.8f), 0.8f, 1.4f, 0.6f); break;
                case EnemyType.Skeleton: Burst(p, Art.Bone, 8, 3f, 0.1f, 0.4f); break;   // a pilha vem do run.Bones
            }
        }

        /// <summary>Elemental de magma estourando (clipe Split da folha `magma`, RENDER_SPRITES §9) uma vez onde morreu, virado como estava; as
        /// brasas do Core nascem em volta. Sem o clipe, fica so o DieFx.</summary>
        void SplitFx(int id, Vector2 p)
        {
            SpriteSheet s = SheetAt(EnemySheet, (int)EnemyType.MagmaElemental); int k;
            if (s == null || (k = s.Clip("Split")) < 0) return;
            Transform up = Upright(_roomRoot, "Estouro", p, out SortingGroup _);
            var a = new SpriteAnim(s, Art.NewSprite(up, "Magma", null, Color.white, 0, Vector2.zero, Vector2.one));
            if (EntById(id) is Ent x && x.Anim != null) a.D = x.Anim.D;
            a.Play(s, k); a.Show();
            _oneShots.Add(new OneShot { A = a, Left = s.Duration(k) });
        }

        void RoarOn(int id)
        {
            if (EntById(id) is Ent x && x.Anim != null) { int k = First(x.Anim.Sheet, SummonClip); if (k >= 0) x.RoarT = x.Anim.Sheet.Duration(k); }
        }

        void NamedFx(Nx n, SimEvent e, Run run, Vector2 p)
        {
            switch (n)
            {
                case Nx.CageScream: Puff(Art.ThinRing(), p, Vector2.zero, Art.A(Art.Cargo, 0.9f), 0.6f, 2f * Balance.CageRadius, 0.6f, ORing); Shake(0.06f, 0.2f); break;
                case Nx.CageCalm: Burst(p + new Vector2(0f, 0.4f), Art.Cream, 6, 1.5f, 0.08f, 0.4f); break;
                case Nx.Burrow: Burst(p, Dirt, 10, 3f, 0.16f, 0.45f); break;
                case Nx.Summon:
                case Nx.ForemanCall:   // chamado: anel lilas, "!!" e clipe Summon/Call
                    Puff(Art.ThinRing(), p, Vector2.zero, Art.A(Lilac, 0.9f), 0.6f, 6f, 0.5f, ORing);
                    Float(p + new Vector2(0f, 1.2f / Tilt), "!!", Lilac, 60);
                    RoarOn(e.A);
                    break;
                case Nx.RockFall: if (e.B == 1) { Burst(p, Art.A(Art.Dim, 0.9f), 10, 3f, 0.18f, 0.5f); Shake(0.1f, 0.15f); } break;
                case Nx.EnemyRevive: _rise[e.A] = RiseTime; Puff(Art.ThinRing(), p, Vector2.zero, Art.A(Undead, 0.9f), 0.3f, 1.6f, 0.6f, ORing); Burst(p, Undead, 8, 2f, 0.1f, 0.6f); break;
                case Nx.Raise: Burst(p, Undead, 10, 2f, 0.1f, 0.6f); RoarOn(e.A); break;
                case Nx.BonesScatter: Burst(p, Art.Bone, 12, 4f, 0.12f, 0.5f); break;
                case Nx.Webbed:
                case Nx.WebTorn: Burst(p, WebC, 10, 3f, 0.08f, 0.5f); break;
                case Nx.Blink: Burst(p, Art.A(Lilac, 0.8f), 12, 2f, 0.25f, 0.5f); break;   // Pos = destino
                case Nx.LanternOut: for (int i = 0; i < 5; i++) Puff(Art.Disc(), p + new Vector2(0f, 0.4f), new Vector2(Random.Range(-0.3f, 0.3f), 1f), Art.A(Art.Dim, 0.7f), 0.12f, 0.35f, 0.8f); break;
                case Nx.LanternLit: Puff(Art.Glow(), p + new Vector2(0f, 0.4f), Vector2.zero, Art.A(Flame, 0.9f), 0.5f, 2.5f, 0.4f); break;
                case Nx.Reflect: Puff(Art.Glow(), p + new Vector2(0f, 0.5f / Tilt), Vector2.zero, Color.white, 0.7f, 0.2f, 0.3f); Shake(0.05f, 0.1f); break;
                case Nx.BossPhase: Puff(Art.Glow(), p, Vector2.zero, Art.A(Art.Bad, 0.5f), 4f, 9f, 0.5f); Shake(0.25f, 0.4f); break;
                case Nx.TakeOff: Puff(Art.ThinRing(), p, Vector2.zero, Art.A(Art.Dim, 0.8f), 1f, 5f, 0.5f, ORing); Burst(p, Art.A(Art.Dim, 0.8f), 12, 4f, 0.2f, 0.5f); break;
                case Nx.Land:
                    Puff(Art.ThinRing(), p, Vector2.zero, Art.A(Art.Bad, 0.9f), 1f, 2f * Balance.LandR, 0.4f, ORing);
                    Burst(p, Art.A(Art.Dim, 0.9f), 16, 5f, 0.22f, 0.5f); Shake(0.3f, 0.35f);
                    break;
                case Nx.PlantBite: Burst(p, Art.Hex(0x5FA83A), 8, 2.5f, 0.1f, 0.3f); _biteId = e.B; _biteT = BiteTime; break;
                case Nx.LavaBurn: Burst(p, VentHot, 12, 3f, 0.14f, 0.45f); Burst(p, LavaCore, 6, 2f, 0.1f, 0.3f); break;
                case Nx.VentFire: if (e.A >= 0 && e.A < _vents.Count) Burst(V(_vents[e.A].V.Pos), VentHot, 10, 3f, 0.16f, 0.4f); break;
                case Nx.EggHatch: DieFx(EnemyType.SalamanderEgg, p); break;
            }
        }

        // ------------------------------------------------------------------ som e vibracao das regioes 2-4 (AUDIO §1.1)

        /// <summary>Som/vibracao do evento nas regioes 2-4, chamado pelo Game antes do som generico. true = tocou o som proprio (o Game pula o
        /// generico); false = o generico continua (o proprio, se houver, toca junto: agua, coracao, balao subindo, balao escapou).</summary>
        public static bool Som(SimEvent e, Run run)
        {
            if (Named.TryGetValue((int)e.Kind, out Nx nx)) return SomNamed(nx, e, run);
            EnemyType t = (EnemyType)e.B;
            switch (e.Kind)
            {
                case Ev.Strike:
                    switch (t)
                    {
                        case EnemyType.GoblinMiner: Audio.Play("picareta"); return true;
                        case EnemyType.Mole: Audio.Play("toupeira_sai"); return true;
                        case EnemyType.MoleQueen: Audio.Play("toupeira_sai", 0.75f); Ajustes.Vibrar(); return true;
                        case EnemyType.BogFrog: Audio.Play("lingua"); return true;
                        case EnemyType.Minecart: Audio.Play("vagonete", 1f, 0.3f); return true;
                        case EnemyType.Ghost: Audio.Play("fantasma"); return true;
                        case EnemyType.Spider: Audio.Play("teia"); return true;
                        case EnemyType.Mummy: Audio.Play("mumia"); Ajustes.Vibrar(); return true;
                        case EnemyType.Gargoyle: Audio.Play("mergulho"); return true;
                        case EnemyType.Salamander: Audio.Play("salamandra"); return true;
                        case EnemyType.Imp: Audio.Play("diabrete"); return true;
                        case EnemyType.KoboldBomber: Audio.Play("frasco"); return true;
                        case EnemyType.MagmaElemental: Audio.Play("pancada", 1.15f); return true;
                        case EnemyType.StoneGolem: Audio.Play("golem"); Ajustes.Vibrar(); return true;
                        case EnemyType.SpiderWidow: Audio.Play("viuva"); return true;
                        case EnemyType.Necromancer: Audio.Play("necro_raio"); return true;
                        case EnemyType.KoboldForeman: Audio.Play("chicote"); return true;
                        case EnemyType.ElderDragon: Audio.Play("sopro"); Ajustes.Vibrar(); return true;
                    }
                    return false;
                case Ev.Telegraph:
                    switch (t)
                    {
                        case EnemyType.BombBeetle: Audio.Play("besouro_chia"); return true;
                        case EnemyType.Minecart: Audio.Play("sino", 1f, 0.3f); return true;
                        case EnemyType.Ghost: Audio.Play("fantasma", 1.3f); return true;
                        case EnemyType.SpiderWidow: Audio.Play("teia", 0.8f); return true;
                        case EnemyType.Gargoyle: Audio.Play("gargula_acorda"); return true;
                        case EnemyType.Imp: Audio.Play("diabrete", 1.2f); return true;
                    }
                    return false;
                case Ev.EnemyDie:
                    switch (t)
                    {
                        case EnemyType.Skeleton: Audio.Play("ossos_cai"); return true;
                        case EnemyType.MagmaElemental: Audio.Play("magma_divide"); return true;
                        case EnemyType.SalamanderEgg: Audio.Play("choca", 0.8f); return true;
                    }
                    return false;
                case Ev.Explosion:
                    if (e.A != 2) return false;
                    Audio.Play("besouro_explode"); Ajustes.Vibrar();
                    return true;
                case Ev.Stun:
                    if (EnemyById(run, e.A) is Enemy g && g.Type == EnemyType.StoneGolem) { Audio.Play("golem", 0.8f); return true; }
                    return false;
                case Ev.CargoIntegrity:
                    if (Tipo(run, e.A) != CargoType.Mirror) return false;
                    Audio.Play("espelho_racha"); Ajustes.Vibrar();
                    return true;
                case Ev.CargoBroken:
                    if (Tipo(run, e.A) != CargoType.Balloon) return false;
                    Audio.Play("balao_estoura"); Ajustes.Vibrar();
                    return true;
                case Ev.CurrencySource:
                    if (e.B != 10) return false;   // ima do coracao
                    Audio.Play("coracao", 1f, 0.5f);
                    return true;
                case Ev.CargoDrop:
                    if (run == null) return false;
                    foreach (Cargo c in run.Cargos)
                    {
                        if (c.Id != e.A) continue;
                        if (c.Type == CargoType.ArcaneHeart) Audio.Play("coracao", 1f, 0.5f);
                        if (c.Type == CargoType.Balloon) Audio.Play("balao_sobe", 1f, 0.3f);
                        if (run.Room.Water.Length > 0 && run.InWater(c.Pos)) Audio.Play("agua", 0.8f, 0.2f);
                    }
                    return false;
                case Ev.ContractEnd:
                    if (e.B == (int)Outcome.Escaped) Audio.Play("balao_escapa");
                    return false;
            }
            return false;
        }

        static CargoType Tipo(Run run, int id)
        {
            if (run != null) foreach (Cargo g in run.Cargos) if (g.Id == id) return g.Type;
            return CargoType.Box;
        }

        static bool SomNamed(Nx n, SimEvent e, Run run)
        {
            switch (n)
            {
                case Nx.CageScream: Audio.Play("grifinho"); return true;
                case Nx.CageCalm: return true;
                case Nx.Burrow: Audio.Play("toupeira_cava", 1f, 0.6f); return true;
                case Nx.Summon:
                {
                    EnemyType t = EnemyById(run, e.A) is Enemy x ? x.Type : EnemyType.MoleQueen;
                    Audio.Play(t == EnemyType.SpiderWidow ? "viuva" : t == EnemyType.StoneGolem ? "rugido" : "rainha", t == EnemyType.StoneGolem ? 0.6f : 1f);
                    return true;
                }
                case Nx.ForemanCall: Audio.Play("apito"); return true;
                case Nx.RockFall: if (e.B == 1) Audio.Play("golem", 1.25f, 0.15f); return true;
                case Nx.EnemyRevive: Audio.Play("ossos_monta"); return true;
                case Nx.BonesScatter: Audio.Play("ossos_cai", 0.8f); return true;
                case Nx.Webbed: Audio.Play("teia", 0.7f); return true;
                case Nx.WebTorn: Audio.Play("teia", 1.2f); return true;
                case Nx.Raise: Audio.Play("necro_ergue"); return true;
                case Nx.Blink: Audio.Play("diabrete", 0.7f); return true;
                case Nx.LanternOut: Audio.Play("lanterna_apaga"); return true;
                case Nx.LanternLit: Audio.Play("lanterna_acende"); return true;
                case Nx.Reflect: Audio.Play("espelho_reflete"); return true;
                case Nx.BossPhase:
                    if (EnemyById(run, e.A) is Enemy b && b.Type == EnemyType.ElderDragon) { Audio.Play("dragao"); Ajustes.Vibrar(); }
                    else Audio.Play("rugido", 0.8f);
                    return true;
                case Nx.TakeOff: Audio.Play("voo"); return true;
                case Nx.Land: Audio.Play("pouso"); Ajustes.Vibrar(); return true;
                case Nx.PlantBite: Audio.Play("planta", 1f, 0.15f); return true;
                case Nx.LavaBurn: Audio.Play("lava", 1f, 0.15f); return true;
                case Nx.VentFire: Audio.Play("respiro", 1f, 0.3f); return true;
                case Nx.EggHatch: Audio.Play("choca", 0.8f); return true;
            }
            return false;
        }

        /// <summary>Som da entrada na sala de chefe (AUDIO §1.1): Rainha `rainha`, Golem `rugido` grave, Viuva `viuva`, Necromante
        /// `necro_ergue`, Capataz `apito`, Dragao `dragao` (com vibracao); M1/B1 o `rugido` de sempre. Mapa com algo revelado: `mapa`.</summary>
        public static void SomEntrada(RoomDef room, Run run)
        {
            if (room.Boss || room.Mini)
            {
                switch (BossDe(room) ?? EnemyType.Ogre)
                {
                    case EnemyType.MoleQueen: Audio.Play("rainha"); break;
                    case EnemyType.StoneGolem: Audio.Play("rugido", 0.6f); break;
                    case EnemyType.SpiderWidow: Audio.Play("viuva"); break;
                    case EnemyType.Necromancer: Audio.Play("necro_ergue"); break;
                    case EnemyType.KoboldForeman: Audio.Play("apito"); break;
                    case EnemyType.ElderDragon: Audio.Play("dragao"); Ajustes.Vibrar(); break;
                    default: Audio.Play("rugido", room.Mini ? 1.3f : 1f); break;
                }
            }
            if (run != null && run.Revealed && run.Buried.Count > 0) Audio.Play("mapa");
        }
    }
}
