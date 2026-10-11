using System;
using System.Collections.Generic;
using DC.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DC
{
    /// <summary>
    /// Telas de meta do lancamento (CONTRATO_LANCAMENTO §6, parte do Hud): mapa das regioes, modificadores antes do contrato, quadro de
    /// eventos, quadro de temporada, marcos e maestria, desafio sem fim e roupas do courier; o som de meta (docs/AUDIO.md, "Meta e loja") e
    /// o -dev-desbloqueia das fotos. As listas usam o mesmo Painel: cartao com titulo, cabecalho, lista rolavel de Linhas de 176 px (48 dp:
    /// a linha inteira e o alvo de toque e o selo da direita diz a acao), rodape e VOLTAR (+ botao principal). Estado nunca so por cor:
    /// cadeado, visto, texto no selo.
    /// Equipamento lateral: aba da guilda (Menus.cs). Contrato raro, rerolls e bau do dia: cartoes do quadro e do menu (Menus.cs). Loja:
    /// Loja.cs. Anuncios e ofertas de rewarded: Anuncios.cs.
    /// </summary>
    public sealed partial class Hud
    {
        const float LinhaH = 176f, LinhaGap = 10f;

        sealed class Linha
        {
            public RectTransform R; public CanvasGroup G; public Button B; public Action Click;
            public Image Icon, Lock, Check, Coin, Bar, BarBg, Badge; public Text Title, Sub, Value, BadgeT; public Image[] Pips;
        }

        sealed class Painel
        {
            public RectTransform M, Content; public ScrollRect Scroll; public Text Head, Foot; public Linha[] Rows;
            public Button Main; public Button[] Tabs; public Image[] Lines; public int Tab;
            /// <summary>Toca a faixa "evento" enquanto aberto (quadro de eventos e de temporada); fechou, volta o "menu".</summary>
            public bool Festa;
            public Action<Painel> Refresh;
        }

        Painel _pMapa, _pMods, _pEventos, _pTemporada, _pMarcos, _pRoupas;
        readonly List<Painel> _paineis = new List<Painel>();
        RectTransform _semFim;
        Text _sfBody; Button _sfPlay; Image _sfLock;
        Modifier _mods; Action<Modifier> _modsGo; string _modsWhat;

        /// <summary>Monta os paineis de meta (chamado pelo construtor, na ordem das camadas: a loja fica por cima das roupas e dos eventos).</summary>
        void BuildPaineis()
        {
            _pMapa = NovoPainel("Mapa", () => Textos.Mapa, RegionDef.All.Length, RefreshMapa);
            _pEventos = NovoPainel("Eventos", () => Textos.QuadroEventos, Balance.EventRewards.Length, RefreshEventos, main: () => Textos.PacoteEvento, onMain: ShowLoja);
            _pEventos.Festa = true;
            _pTemporada = NovoPainel("Temporada", () => Textos.Temporada, Season.Goals.Length, RefreshTemporada);
            _pTemporada.Festa = true;
            _pMarcos = NovoPainel("Marcos", () => Textos.Marcos, Math.Max(Balance.Milestones.Length, CargoDef.All.Length), RefreshMarcos,
                new Func<string>[] { () => Textos.AbaMarcos, () => Textos.AbaMaestria });
            _pRoupas = NovoPainel("Roupas", () => Textos.Roupas, OutfitDef.All.Length, RefreshRoupas);
            _semFim = BuildSemFim();
            _pLoja = NovoPainel("Loja", () => Textos.Loja, Vitrine.Length, RefreshLoja);
            _pMods = NovoPainel("Modificadores", () => Textos.Modificadores, ModifierDef.All.Length, RefreshMods, main: () => Textos.Jogar, onMain: ModsGo);
        }

        // ------------------------------------------------------------------ painel generico

        Painel NovoPainel(string name, Func<string> title, int rows, Action<Painel> refresh, Func<string>[] tabs = null, Func<string> main = null, Action onMain = null)
        {
            var p = new Painel { M = Modal(name, 0.75f), Rows = new Linha[rows], Refresh = refresh };
            Image face = Card(p.M, "Cartao", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
            Transform f = face.transform;
            Shrink(Loc(Ink(f, "Titulo", 52, new Vector2(0.05f, 0.935f), new Vector2(0.95f, 0.99f), TextAnchor.MiddleCenter, true), title), 28);
            p.Head = Shrink(Ink(f, "Cabeca", 28, new Vector2(0.05f, 0.873f), new Vector2(0.95f, 0.935f)), 17);
            p.Head.color = Art.A(Art.Tinta, 0.85f);
            float top = 0.865f;
            if (tabs != null)
            {
                p.Tabs = new Button[tabs.Length]; p.Lines = new Image[tabs.Length];
                float w = 0.92f / tabs.Length;
                for (int i = 0; i < tabs.Length; i++)
                {
                    int k = i;
                    p.Tabs[i] = Btn(f, tabs[i], 36, Art.PaperEdge, new Vector2(0.04f + i * w, 0.783f), new Vector2(0.03f + (i + 1) * w, 0.868f), () => { p.Tab = k; p.Refresh(p); });
                    p.Lines[i] = Underline(p.Tabs[i].transform);
                }
                top = 0.775f;
            }
            p.Foot = Shrink(Ink(f, "Rodape", 28, new Vector2(0.05f, 0.105f), new Vector2(0.95f, 0.168f), TextAnchor.MiddleCenter, true), 17);
            if (main != null) p.Main = Btn(f, main, 40, Art.Good, new Vector2(0.37f, 0.015f), new Vector2(0.96f, 0.1f), onMain, "confirmar");
            Btn(f, () => Textos.Voltar, 40, Art.PaperEdge, new Vector2(main != null ? 0.04f : 0.25f, 0.015f), new Vector2(main != null ? 0.35f : 0.75f, 0.1f), () => Fecha(p));

            RectTransform view = Art.Node(f, "Lista", new Vector2(0.02f, 0.172f), new Vector2(0.98f, top));
            view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // pega o arrasto entre as linhas
            view.gameObject.AddComponent<RectMask2D>();
            p.Content = Art.Node(view, "Conteudo", new Vector2(0f, 1f), new Vector2(1f, 1f));
            p.Content.pivot = new Vector2(0.5f, 1f);
            p.Scroll = view.gameObject.AddComponent<ScrollRect>();
            p.Scroll.content = p.Content; p.Scroll.viewport = view;
            p.Scroll.horizontal = false; p.Scroll.movementType = ScrollRect.MovementType.Elastic; p.Scroll.scrollSensitivity = 60f;
            for (int i = 0; i < rows; i++) p.Rows[i] = NovaLinha(p.Content, i);
            _paineis.Add(p);
            return p;
        }

        Linha NovaLinha(RectTransform content, int i)
        {
            RectTransform holder = Art.Node(content, "Linha", new Vector2(0f, 1f), new Vector2(1f, 1f));
            holder.pivot = new Vector2(0.5f, 1f);
            holder.anchoredPosition = new Vector2(0f, -i * (LinhaH + LinhaGap));
            holder.sizeDelta = new Vector2(0f, LinhaH);
            var l = new Linha { R = holder, G = holder.gameObject.AddComponent<CanvasGroup>(), Pips = new Image[5] };
            Image face = Card(holder, "Face", Vector2.zero, Vector2.one);
            l.B = face.gameObject.AddComponent<Button>();
            l.B.targetGraphic = face;
            l.B.onClick.AddListener(() => { if (l.Click == null) return; Audio.Play("clique"); l.Click(); });
            Transform f = face.transform;
            Image disc = Art.Img(f, "Fundo", Art.Disc(), Art.Tinta, new Vector2(0.012f, 0.1f), new Vector2(0.138f, 0.9f));
            l.Icon = Art.Img(disc.transform, "Icone", Art.Star(), Art.Ink, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f));
            l.Title = Shrink(Ink(f, "Titulo", 32, new Vector2(0.15f, 0.54f), new Vector2(0.68f, 0.97f), TextAnchor.MiddleLeft, true), 18);
            l.Sub = Shrink(Ink(f, "Linha", 26, new Vector2(0.15f, 0.19f), new Vector2(0.68f, 0.55f), TextAnchor.MiddleLeft), 15);
            l.Sub.color = Art.A(Art.Tinta, 0.8f);
            l.BarBg = Art.Panel(f, "Barra", Art.A(Art.Tinta, 0.2f), new Vector2(0.15f, 0.05f), new Vector2(0.66f, 0.17f));
            l.BarBg.raycastTarget = false;
            l.Bar = Art.Img(l.BarBg.transform, "Cheio", Art.Square(), Art.Good, Vector2.zero, Vector2.one);
            l.Bar.preserveAspect = false; l.Bar.type = Image.Type.Filled; l.Bar.fillMethod = Image.FillMethod.Horizontal;
            for (int k = 0; k < l.Pips.Length; k++)
                l.Pips[k] = Art.Img(f, "Ponto", Art.Disc(), Art.Off, new Vector2(0.15f + k * 0.04f, 0.04f), new Vector2(0.18f + k * 0.04f, 0.18f));
            l.Coin = Art.Img(f, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.69f, 0.6f), new Vector2(0.74f, 0.92f));
            l.Value = Shrink(Ink(f, "Valor", 32, new Vector2(0.745f, 0.55f), new Vector2(0.985f, 0.97f), TextAnchor.MiddleLeft, true), 15);
            l.Badge = Art.Panel(f, "Selo", Art.Good, new Vector2(0.69f, 0.07f), new Vector2(0.985f, 0.52f));
            l.Badge.raycastTarget = false;
            l.BadgeT = Shrink(Ink(l.Badge.transform, "Rotulo", 26, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), TextAnchor.MiddleCenter, true), 13);
            l.Lock = Art.Img(l.Badge.transform, "Cadeado", Art.Lock(), Art.Ink, new Vector2(0.04f, 0.14f), new Vector2(0.28f, 0.86f));
            l.Check = Art.Img(l.Badge.transform, "Visto", Art.Check(), Art.Tinta, new Vector2(0.04f, 0.14f), new Vector2(0.28f, 0.86f));
            return l;
        }

        /// <summary>Preenche uma linha. value = texto da direita em cima (com moeda se coin); badge = acao/estado no selo; locked = cadeado e
        /// apagada (sem toque); check = visto (feito/equipado/escolhido); click null = so informacao.</summary>
        static void Pinta(Linha l, Sprite icon, Color ic, string title, string sub, string value, bool coin, string badge, Color badgeC, bool locked,
                          bool check, Action click)
        {
            l.Icon.sprite = icon; l.Icon.color = ic;
            l.Title.text = title; l.Sub.text = sub ?? "";
            l.Value.text = value ?? "";
            bool c = coin && !string.IsNullOrEmpty(value);
            l.Coin.enabled = c;
            l.Value.rectTransform.anchorMin = new Vector2(c ? 0.745f : 0.69f, 0.55f);
            l.Badge.color = badgeC;
            l.BadgeT.text = badge ?? "";
            l.Lock.enabled = locked; l.Check.enabled = check && !locked;
            l.BadgeT.rectTransform.anchorMin = new Vector2(locked || check ? 0.28f : 0.04f, 0f);
            l.Click = locked ? null : click;
            l.B.interactable = !locked;
            l.G.alpha = locked ? 0.6f : 1f;
            l.BarBg.gameObject.SetActive(false);
            for (int k = 0; k < l.Pips.Length; k++) l.Pips[k].enabled = false;
        }

        static void Barra(Linha l, float frac, Color c)
        {
            l.BarBg.gameObject.SetActive(true);
            l.Bar.fillAmount = Mathf.Clamp01(frac); l.Bar.color = c;
        }

        static void Pontos(Linha l, int n, int max)
        {
            for (int k = 0; k < l.Pips.Length; k++)
            {
                l.Pips[k].enabled = k < max;
                l.Pips[k].color = k < n ? Art.Courier : Art.A(Art.Tinta, 0.25f);
            }
        }

        static void Mostra(Painel p, int n)
        {
            for (int i = 0; i < p.Rows.Length; i++) p.Rows[i].R.gameObject.SetActive(i < n);
            p.Content.sizeDelta = new Vector2(0f, n * (LinhaH + LinhaGap));
        }

        void Abre(Painel p)
        {
            p.Refresh(p);
            p.M.gameObject.SetActive(true);
            p.Content.anchoredPosition = Vector2.zero; p.Scroll.velocity = Vector2.zero;
            if (p.Festa) Audio.Music("evento");
        }

        void Fecha(Painel p)
        {
            p.M.gameObject.SetActive(false);
            if (p.Festa && !_pEventos.M.gameObject.activeSelf && !_pTemporada.M.gameObject.activeSelf) Audio.Music("menu");
        }

        /// <summary>Redesenha os paineis abertos (compra, resgate, troca de idioma) e o desafio sem fim.</summary>
        void RefreshPaineis()
        {
            foreach (Painel p in _paineis) if (p.M.gameObject.activeSelf) p.Refresh(p);
            if (_semFim != null && _semFim.gameObject.activeSelf) RefreshSemFim();
        }

        static void Abas(Painel p)
        {
            for (int i = 0; i < p.Tabs.Length; i++) Tab(p.Tabs[i], p.Lines[i], p.Tab == i);
        }

        // ------------------------------------------------------------------ icones

        /// <summary>Icone da carga: o quadro estatico da folha (Resources/Sprites, WorldView.CargoSheet) ou o desenho procedural.</summary>
        static Sprite CargaIcone(CargoType t, out Color c)
        {
            string sheet = (int)t >= 0 && (int)t < WorldView.CargoSheet.Length ? WorldView.CargoSheet[(int)t] : null;
            SpriteSheet s = sheet != null ? SpriteSheet.Get(sheet) : null;
            if (s != null) { c = Color.white; return s.Frame(s.Pick("Static"), 0, 0); }
            c = Art.CargoColor(t);
            return Art.CargoSprite(t);
        }

        static Sprite GearIcon(Gear g, out Color c)
        {
            switch (g)
            {
                case Gear.Smoke: c = Art.Dim; return Art.Glow();
                case Gear.Hook: c = Art.Steel; return Art.Key();
                case Gear.Trap: c = Art.Steel; return Art.Horns();
                default: c = Art.Leather; return Art.Fists();   // botas: as duas solas
            }
        }

        static Sprite ModIcon(Modifier m, out Color c)
        {
            switch (m)
            {
                case Modifier.Fog: c = Art.Dim; return Art.Eyes();
                case Modifier.Rush: c = Art.Bad; return Art.Clock();
                case Modifier.Horde: c = Art.Bad; return Art.Skull();
                case Modifier.Fragile: c = Art.Ink; return Art.Cracks(2);
                default: c = Art.Coin; return Art.ChestShape();
            }
        }

        // ------------------------------------------------------------------ roupas (cosmetico)

        static readonly string[] RoupaChave = { "classico", "noturno", "real", "explorador", "forja", "fantasma" };

        /// <summary>Nome da folha de sprites da roupa: "courier" (Classica) ou "courier_&lt;roupa&gt;" (render_sprites.py com troca de material,
        /// CONTRATO_LANCAMENTO §8). Enquanto a folha nao existe, vale a do courier com a TintaRoupa. A WorldView deve usar o mesmo par.</summary>
        public static string FolhaRoupa(Outfit o) => o == Outfit.Classic || (int)o < 0 || (int)o >= RoupaChave.Length ? "courier" : "courier_" + RoupaChave[(int)o];

        /// <summary>Tinta procedural de reserva por roupa (multiplica o sprite do courier; Classica = branco, sem tinta).</summary>
        public static Color TintaRoupa(Outfit o)
        {
            switch (o)
            {
                case Outfit.Night: return Art.Hex(0x7C86D8);
                case Outfit.Royal: return Art.Hex(0xD7A0F0);
                case Outfit.Explorer: return Art.Hex(0xE2CC98);
                case Outfit.Forge: return Art.Hex(0xF2905C);
                case Outfit.Ghost: return new Color(0.8f, 1f, 1f, 0.6f);
                default: return Color.white;
            }
        }

        /// <summary>Retrato da roupa: quadro parado da folha courier_&lt;roupa&gt; ou, sem ela, o do courier com a tinta; sem folha nenhuma, um
        /// disco azul do courier tingido.</summary>
        static Sprite RoupaIcone(Outfit o, out Color c)
        {
            SpriteSheet s = SpriteSheet.Get(FolhaRoupa(o));
            bool tinta = s == null;
            if (tinta) s = SpriteSheet.Get("courier");
            if (s != null) { c = tinta ? TintaRoupa(o) : Color.white; return s.Frame(s.Pick("Idle"), 0, 0); }
            c = Art.Courier * TintaRoupa(o);
            return Art.Disc();
        }

        void RefreshRoupas(Painel p)
        {
            p.Head.text = Textos.RoupasCabeca;
            p.Foot.text = "";
            int n = OutfitDef.All.Length;
            for (int i = 0; i < n; i++)
            {
                var o = (Outfit)i;
                Product? prod = ProdutoDaRoupa(o);
                bool own = S.OwnsOutfit(o), eq = S.OutfitEquipped == o, sale = prod.HasValue;
                Action click = eq ? null : own ? () => { if (S.EquipOutfit(o)) _g.AfterMeta(); } : sale ? (Action)ShowLoja : null;
                Pinta(p.Rows[i], RoupaIcone(o, out Color c), c, Textos.RoupaNome(o), Textos.RoupaOrigem(o), !own && sale ? _loja.Preco(prod.Value) : null, false,
                    eq ? Textos.Equipada : own ? Textos.Equipar : sale ? Textos.LojaBotao : "", eq ? Art.Gold : own ? Art.Good : sale ? Art.Paper : Art.Off,
                    !own && !sale, eq, click);
            }
            Mostra(p, n);
        }

        static Product? ProdutoDaRoupa(Outfit o)
        {
            switch (o)
            {
                case Outfit.Night: return Product.OutfitNight;
                case Outfit.Royal: return Product.OutfitRoyal;
                case Outfit.Explorer: return Product.OutfitExplorer;
                case Outfit.Forge: return Product.OutfitForge;
                default: return null;
            }
        }

        // ------------------------------------------------------------------ mapa das regioes (no quadro)

        /// <summary>Chefe que fecha cada regiao (o da anterior destrava a seguinte).</summary>
        static readonly EnemyType[] ChefeDaRegiao = { EnemyType.Ogre, EnemyType.StoneGolem, EnemyType.Necromancer, EnemyType.ElderDragon };

        void RefreshMapa(Painel p)
        {
            p.Head.text = Textos.MapaCabeca;
            p.Foot.text = "";
            int n = RegionDef.All.Length, board = BoardRegion();
            for (int i = 0; i < n; i++)
            {
                RegionDef r = RegionDef.All[i];
                bool open = S.RegionOpen(r.Id) && !S.SaveRefused, sel = open && r.Id == board;
                int done = 0, st = 0;
                foreach (ContractDef c in r.Contracts) { if (c.Index < S.Reputation) done++; st += S.StarsOf(c.Index); }
                string sub = !r.HasContent ? Textos.EmBreve : open ? Textos.RegiaoProgresso(done, r.Contracts.Length, st, 3 * r.Contracts.Length)
                    : Textos.RegiaoTrava(r.Stars, Textos.Inimigo(ChefeDaRegiao[Mathf.Clamp(r.Id - 2, 0, ChefeDaRegiao.Length - 1)]));
                bool beaten = r.BossAt >= 0 && S.Reputation > r.BossAt;
                int id = r.Id;
                Pinta(p.Rows[i], Art.Horns(), beaten ? Art.Gold : open ? Art.Ink : Art.Off, Textos.NomeRegiao(r.Id), sub, null, false,
                    sel ? Textos.NoQuadro : open ? Textos.Ver : "", sel ? Art.Gold : open ? Art.Good : Art.Off, !open, sel,
                    () => { _boardRegion = id; Fecha(p); RefreshBoard(); RolaQuadro(); });
            }
            Mostra(p, n);
        }

        // ------------------------------------------------------------------ modificadores (antes do contrato; o do dia vem pela data)

        /// <summary>Escolha de 0-2 modificadores antes de comecar (what = rotulo do contrato); go(mods) segue para o resgate e o contrato.</summary>
        void ShowMods(string what, Action<Modifier> go)
        {
            _mods = Modifier.None; _modsGo = go; _modsWhat = what;
            Abre(_pMods);
        }

        void RefreshMods(Painel p)
        {
            p.Head.text = Textos.ModCabeca(_modsWhat ?? "");
            int n = ModifierDef.All.Length, count = ModifierDef.Count(_mods);
            for (int i = 0; i < n; i++)
            {
                ModifierDef d = ModifierDef.All[i];
                Modifier m = d.Type;
                bool on = (_mods & m) != 0, full = !on && count >= Balance.MaxModifiers;
                Pinta(p.Rows[i], ModIcon(m, out Color c), c, Textos.ModNome(m), Textos.ModEfeito(m), "+" + d.Pay + "%", false,
                    on ? Textos.Escolhido : Textos.Escolher, on ? Art.Gold : Art.PaperEdge, full, on, () => { _mods ^= m; RefreshMods(p); });
            }
            Mostra(p, n);
            p.Foot.text = Textos.ModTotal(ModifierDef.PayOf(_mods));
        }

        void ModsGo()
        {
            _pMods.M.gameObject.SetActive(false);
            Action<Modifier> go = _modsGo; _modsGo = null;
            go?.Invoke(_mods);
        }

        /// <summary>Contrato da campanha tocado no quadro: com os modificadores abertos, escolhe antes; senao segue direto.</summary>
        void Escolhe(int i)
        {
            if (S.ModifiersOpen && !S.SaveRefused) ShowMods(Sim.Contracts[i].Code, m => _g.Play(i, m));
            else _g.Play(i);
        }

        // ------------------------------------------------------------------ quadro de eventos (1 por semana, 3 vagas + 2 do pacote)

        void ShowEventos() => Abre(_pEventos);

        void RefreshEventos(Painel p)
        {
            int hoje = _g.Hoje;
            LiveEvent ev = Sim.EventOf(hoje);
            bool open = S.EventsOpen && !S.SaveRefused;
            int dias = 7 - (Sim.DayNumber(hoje) + 5) % 7;
            p.Head.text = open ? Textos.EventoCabeca(Textos.EventoNome(ev), Textos.EventoRegra(ev), dias) : Textos.EventoFechado;
            int n = open ? Math.Min(S.EventSlots(hoje), p.Rows.Length) : 0;
            for (int k = 0; k < n; k++)
            {
                ContractDef c = S.EventContract(hoje, k);
                bool done = S.EventDone(hoje, k), avail = c != null && S.EventAvailable(hoje, k);
                int slot = k;
                Color ic = Art.Off;
                Sprite icon = c != null ? CargaIcone(c.Cargo, out ic) : Art.Star();
                Pinta(p.Rows[k], icon, ic, Textos.Vaga(k + 1, c != null ? Textos.NomeRegiao(c.Region) : Textos.EmBreve),
                    c != null ? Textos.VagaLinha(c.Layers.Length - 1, Textos.Carga(c.Cargo)) : "", "+" + Sim.EventReward(k), true,
                    done ? Textos.Feito : avail ? Textos.Jogar : "", done ? Art.Gold : avail ? Art.Good : Art.Off, !avail && !done, done,
                    avail ? (Action)(() => { Fecha(p); _g.PlayEvent(slot); }) : null);
            }
            Mostra(p, n);
            bool all = open && S.EventDone(hoje, 0) && S.EventDone(hoje, 1) && S.EventDone(hoje, 2);
            Gear g = ProjetoDaSemana(hoje);
            p.Foot.text = !open ? "" : all ? Textos.ProjetoGanho : g == Gear.None ? Textos.ProjetoSemanaMoedas(Balance.EventProjectCoins) : Textos.ProjetoSemana(Textos.GearNome(g));
            p.Main.gameObject.SetActive(open && !S.Owns(Product.EventPack));
        }

        /// <summary>O projeto que as 3 vagas da semana dao (a mesma conta do Core: equipamento da semana ou o proximo que falta; None = moedas).</summary>
        Gear ProjetoDaSemana(int date)
        {
            int w = Sim.Week(date);
            for (int i = 0; i < 4; i++) { var g = (Gear)(1 + (w + i) % 4); if (S.Blueprints(g) < Balance.GearBlueprints) return g; }
            return Gear.None;
        }

        // ------------------------------------------------------------------ quadro de temporada (28 dias, 20 objetivos)

        void RefreshTemporada(Painel p)
        {
            p.Head.text = Textos.TemporadaCabeca(Sim.SeasonDaysLeft(Math.Max(_g.Hoje, S.LastDateSeen)));
            p.Foot.text = "";
            int n = Season.Goals.Length;
            for (int i = 0; i < n; i++)
            {
                GoalDef g = Season.Goals[i];
                int prog = S.SeasonProgress(i), k = i;
                bool claimed = S.SeasonClaimed(i), ready = S.SeasonReady(i);
                string premio = g.Blueprint != Gear.None ? Textos.PremioProjeto(g.Blueprint) : g.Outfit != Outfit.Classic ? Textos.PremioRoupa(g.Outfit) : null;
                Color ic = Art.Gold;
                Sprite icon = g.Blueprint != Gear.None ? GearIcon(g.Blueprint, out ic) : g.Outfit != Outfit.Classic ? RoupaIcone(g.Outfit, out ic) : Art.Star();
                Pinta(p.Rows[i], icon, ic, Textos.ObjetivoTemporada(g), premio, g.Coins > 0 ? "+" + g.Coins : null, true,
                    claimed ? Textos.Coletado : ready ? Textos.Coletar : Textos.Progresso(prog, g.Target), claimed ? Art.Gold : ready ? Art.Good : Art.PaperEdge,
                    false, claimed, ready ? (Action)(() => { if (S.ClaimSeason(k)) _g.AfterMeta(); }) : null);
                Barra(p.Rows[i], prog / (float)Math.Max(1, g.Target), prog >= g.Target ? Art.Gold : Art.Good);
            }
            Mostra(p, n);
        }

        // ------------------------------------------------------------------ marcos (estrelas) e maestria (por carga)

        void RefreshMarcos(Painel p)
        {
            Abas(p);
            p.Head.text = Textos.MarcosCabeca;
            p.Foot.text = "";
            if (p.Tab == 0)
            {
                int n = Balance.Milestones.Length, stars = S.Stars;
                for (int i = 0; i < n; i++)
                {
                    int need = Balance.Milestones[i], k = i;
                    bool claimed = S.MilestoneClaimed(i), ready = S.MilestoneReady(i);
                    string sub = i == Balance.MilestoneOutfit ? Textos.VoceTem(stars) + " · " + Textos.PremioRoupa(Outfit.Ghost) : Textos.VoceTem(stars);
                    Pinta(p.Rows[i], Art.Star(), claimed || ready ? Art.Gold : Art.Ink, Textos.MarcoNome(need), sub, "+" + Balance.MilestoneCoins[i], true,
                        claimed ? Textos.Coletado : ready ? Textos.Coletar : Textos.Progresso(Math.Min(stars, need), need),
                        claimed ? Art.Gold : ready ? Art.Good : Art.PaperEdge, false, claimed,
                        ready ? (Action)(() => { if (S.ClaimMilestone(k)) _g.AfterMeta(); }) : null);
                    Barra(p.Rows[i], stars / (float)need, stars >= need ? Art.Gold : Art.Good);
                }
                Mostra(p, n);
                return;
            }
            int m = CargoDef.All.Length, max = Balance.MasterySteps.Length;
            for (int i = 0; i < m; i++)
            {
                var t = (CargoType)i;
                int lv = S.MasteryLevel(t), cnt = S.MasteryCount(t);
                Pinta(p.Rows[i], CargaIcone(t, out Color c), c, Textos.Carga(t), lv >= max ? Textos.EntregasMax(cnt) : Textos.Entregas(cnt, Balance.MasterySteps[lv]),
                    lv > 0 ? Textos.PagamentoMais(lv * Balance.MasteryPct) : null, false, Textos.NivelCurto(lv), lv > 0 ? Art.Gold : Art.PaperEdge, false, false, null);
                Pontos(p.Rows[i], lv, max);
            }
            Mostra(p, m);
        }

        // ------------------------------------------------------------------ desafio sem fim (D90)

        RectTransform BuildSemFim()
        {
            RectTransform m = Modal("SemFim", 0.75f);
            Image face = Card(m, "Cartao", new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.82f));
            Transform f = face.transform;
            Shrink(Loc(Ink(f, "Titulo", 52, new Vector2(0.05f, 0.87f), new Vector2(0.95f, 0.97f), TextAnchor.MiddleCenter, true), () => Textos.SemFim), 28);
            Art.Img(f, "Icone", Art.Horns(), Art.Gold, new Vector2(0.4f, 0.72f), new Vector2(0.6f, 0.86f));
            _sfBody = Shrink(Ink(f, "Corpo", 30, new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.71f)), 17);
            _sfPlay = Btn(f, () => Textos.Jogar, 44, Art.Good, new Vector2(0.05f, 0.04f), new Vector2(0.6f, 0.19f), () => { m.gameObject.SetActive(false); _g.PlayEndless(); }, "confirmar");
            _sfLock = Art.Img(_sfPlay.transform, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.05f, 0.2f), new Vector2(0.2f, 0.8f));
            Btn(f, () => Textos.Voltar, 40, Art.PaperEdge, new Vector2(0.63f, 0.04f), new Vector2(0.95f, 0.19f), () => m.gameObject.SetActive(false));
            return m;
        }

        void ShowSemFim() { RefreshSemFim(); _semFim.gameObject.SetActive(true); }

        void RefreshSemFim()
        {
            int hoje = _g.Hoje;
            bool open = S.EndlessOpen && !S.SaveRefused;
            string body = Textos.SemFimRegras(Balance.EndlessMiniEvery, Balance.EndlessBossEvery, Balance.EndlessCoinsPerLayer, Balance.EndlessDailyCap);
            if (open)
            {
                ContractDef d = S.EndlessContract(hoje);
                int week = S.EndlessWeek == Sim.Week(hoje) ? S.EndlessWeekBest : 0;
                body += "\n\n" + Textos.SemFimRecorde(S.EndlessBest, week) + "\n" + Textos.SemFimMoedas(S.EndlessCoinsLeft);
                if (d != null && !string.IsNullOrEmpty(d.ShareCode)) body += "\n" + Textos.SemFimCodigo(d.ShareCode);
            }
            else body += "\n\n" + Textos.SemFimFechado;
            _sfBody.text = body;
            _sfPlay.interactable = open;
            _sfLock.enabled = !open;
            ((Image)_sfPlay.targetGraphic).color = open ? Art.Good : Art.Off;
        }

        // ------------------------------------------------------------------ som de meta e dev

        /// <summary>Som de meta (docs/AUDIO.md, "Meta e loja"; haptica: nenhuma, pela tabela). O Game.HandleEvents chama para cada evento.</summary>
        public static void SomMeta(SimEvent e)
        {
            switch (e.Kind)
            {
                case Ev.EventStart: Audio.Play("evento"); break;
                case Ev.SeasonProgress: Audio.Play("temporada", 1f, 0.3f); break;
                case Ev.EventReward: case Ev.SeasonClaim: case Ev.Milestone: case Ev.RewardedGrant: Audio.Play("recompensa", 1f, 0.2f); break;
                case Ev.BlueprintFound: Audio.Play("blueprint"); break;
                case Ev.IapPurchase: Audio.Play("compra_loja"); break;
                case Ev.GearUpgrade: case Ev.GearUnlock: case Ev.MasteryUp: Audio.Play("compra", 1f, 0.2f); break;
            }
        }

        static bool Flag(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

        /// <summary>-testsession -dev-desbloqueia (so fotos; o -testsession nao grava nada): campanha inteira aberta, estrelas cheias, +5000
        /// moedas, 2a sessao (o "retomar" do rewarded so vale fora da 1a) e o ultimo instante ativo 9 h atras, para o OpenSession do Game
        /// oferecer o contrato raro. ponytail: projetos e equipamento ficam de fora (o Core nao tem atalho de teste para isso).</summary>
        void DevDesbloqueia()
        {
            if (!Flag("-testsession") || !Flag("-dev-desbloqueia")) return;
            S.Reputation = Sim.Contracts.Length;
            S.DevStars(3 * Sim.Contracts.Length);
            S.Coins += 5000;
            S.Sessions = Math.Max(S.Sessions, 1);
            S.LastActive = _g.Agora - 9 * 3600L;
        }
    }
}
