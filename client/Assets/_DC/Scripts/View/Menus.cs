using DC.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DC
{
    /// <summary>
    /// Telas fora do contrato (parte do Hud, CONTRATO_V1 §11): titulo (nome desenhado com tipografia, sem imagem), menu, quadro de
    /// contratos (estrelas, cadeado, contrato do dia em destaque, botao de codigo), entrada de codigo (teclado do aparelho), guilda com
    /// abas Melhorias/Armas, escolha de perk (1 de 3, cartoes grandes), configuracoes (Som, Musica, Vibracao, Idioma, Exportar diario,
    /// versao) e creditos. Nada aqui roda por quadro, exceto o piscar do titulo e o pulso da guilda (sem alocar).
    /// Dev: -tela titulo|menu|quadro|codigo|guilda|armas|config|creditos abre a tela direto (fotos 540x960 sem clicar).
    /// </summary>
    public sealed partial class Hud
    {
        const float RowH = 176f, RowGap = 12f;   // linha do quadro em px de referencia (>= 144 = 48 dp)

        // titulo e menu
        Text _tapText, _menuCoins, _menuStars, _menuNotice, _menuGuildNote;
        Button _menuGuild;
        Image _menuGuildLock;

        // quadro
        sealed class Row { public Button B; public CanvasGroup G; public Image Badge, Lock; public Text BadgeText; public readonly Image[] Stars = new Image[3]; }
        Row[] _rows;
        Text _boardCoins, _boardStars, _notice;
        ScrollRect _scroll;
        RectTransform _scrollContent;
        Button _dayBtn, _codeBtn;
        CanvasGroup _dayG;
        Image _dayIcon, _dayEdge, _dayBadge, _dayLock, _codeLock;
        Text _dayLine, _dayBadgeText;

        // codigo
        InputField _codeField;
        Text _codeError;

        // guilda
        sealed class UpRow { public Text Level, Cost; public Image[] Dots; public Button Buy; public Text BuyText; public Image CostCoin; }
        sealed class WRow { public CanvasGroup G; public Image Lock, Check, UpCoin; public Image[] Dots; public Text Level, Need, Cost, EquipText, UpText; public Button Equip, Up; }
        UpRow[] _ups;
        WRow[] _weps;
        Text _guildCoins, _guildStars;
        RectTransform _guildUps, _guildWeps;
        Button _tabUps, _tabWeps;
        Image _tabUpsLine, _tabWepsLine;
        bool _guildWeapons;

        // perk
        readonly Image[] _perkIcon = new Image[3];
        readonly Text[] _perkName = new Text[3], _perkEff = new Text[3];
        readonly GameObject[] _perkCard = new GameObject[3];

        // configuracoes
        Text _volSomT, _volMusT, _exportStatus, _versionT;
        readonly Image[] _barSom = new Image[Ajustes.VolMax], _barMus = new Image[Ajustes.VolMax];
        Button _vibraBtn, _ptBtn, _enBtn;
        Image _ptCheck, _enCheck;

        // ------------------------------------------------------------------ mostrar

        public void ShowMenu() { Current = Tela.Menu; Only(_menu); _g.SeeDate(); RefreshMenu(); Audio.Music("menu"); }

        public void ShowBoard()
        {
            Current = Tela.Board; Only(_board);
            Audio.Music("menu");
            _g.SeeDate();
            RefreshBoard();
            // rola a lista ate o proximo contrato da campanha (a 1/3 do topo)
            Canvas.ForceUpdateCanvases();
            float view = _scroll.viewport.rect.height, step = RowH + RowGap;
            int next = Mathf.Clamp(S.Reputation, 0, _rows.Length - 1);
            _scrollContent.anchoredPosition = new Vector2(0f, Mathf.Clamp(next * step - view * 0.35f, 0f, Mathf.Max(0f, _scrollContent.sizeDelta.y - view)));
            _scroll.velocity = Vector2.zero;
        }

        void ShowGuild(bool weapons) { _guildWeapons = weapons; RefreshGuild(); _guild.gameObject.SetActive(true); }

        void ShowCode()
        {
            _codeError.text = ""; _codeField.text = "";
            _code.gameObject.SetActive(true);
            _codeField.ActivateInputField();   // abre o teclado do aparelho
        }

        void ShowSettings() { _exportStatus.text = ""; RefreshSettings(); _settings.gameObject.SetActive(true); }
        void ShowCredits() => _credits.gameObject.SetActive(true);

        void ShowPerk(Perk[] offer)
        {
            for (int i = 0; i < _perkCard.Length; i++)
            {
                bool on = i < offer.Length;
                _perkCard[i].SetActive(on);
                if (!on) continue;
                Perk p = offer[i];
                _perkIcon[i].sprite = PerkIcon(p, out Color c); _perkIcon[i].color = c;
                _perkName[i].text = Textos.PerkNome(p);
                _perkEff[i].text = Textos.PerkEfeito(p);
            }
            _perk.gameObject.SetActive(true);
            if (S.Run != null)   // diario: oferta mostrada (taxa de escolha por perk no diario_report)
            {
                var ids = new string[offer.Length];
                for (int i = 0; i < offer.Length; i++) ids[i] = ((int)offer[i]).ToString(System.Globalization.CultureInfo.InvariantCulture);
                _g.Log("perk_offer", string.Join("|", ids), "", S.Run.Contract.Code, S.Run.Room.Code);
            }
        }

        /// <summary>Moedas, estrelas e travas mudaram (compra, equipar, fim de contrato): redesenha menu, quadro e guilda.</summary>
        public void RefreshMeta() { RefreshMenu(); RefreshBoard(); RefreshGuild(); }

        /// <summary>-tela X (dev): abre a tela para a foto.</summary>
        public void Dev(string tela)
        {
            switch (tela)
            {
                case "titulo": ShowTitle(); break;
                case "menu": ShowMenu(); break;
                case "quadro": ShowBoard(); break;
                case "codigo": ShowBoard(); ShowCode(); break;
                case "guilda": ShowMenu(); ShowGuild(false); break;
                case "armas": ShowMenu(); ShowGuild(true); break;
                case "config": ShowMenu(); ShowSettings(); break;
                case "creditos": ShowMenu(); ShowCredits(); break;
                case "perk": ShowPerk(new[] { Perk.DoubleDash, Perk.Magnet, Perk.IronBoot }); break;   // o -bot escolhe sozinho: foto so por aqui
            }
        }

        // ------------------------------------------------------------------ atualizar

        void TickTitle() => _tapText.color = Art.A(Art.Ink, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f)));

        void PulseGuild()
        {
            if (!_menuGuild.interactable) return;
            bool any = false;
            for (int u = 0; u < Balance.UpgradeLines && !any; u++) any = S.CanBuy((Upgrade)u);
            for (int w = 0; w < WeaponDef.All.Length && !any; w++) any = S.CanUpgradeWeapon((Weapon)w);
            _menuGuild.transform.localScale = Vector3.one * (any ? 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 6f) : 1f);
        }

        void RefreshMenu()
        {
            if (_menuCoins == null) return;
            _menuCoins.text = Textos.Num(S.Coins);
            _menuStars.text = Textos.Estrelas(S.Stars, Sim.Contracts.Length * 3);
            bool open = S.GuildOpen && !S.SaveRefused;
            _menuGuild.interactable = open;
            _menuGuildLock.enabled = !open;
            _menuGuildNote.gameObject.SetActive(!open && !S.SaveRefused);
            if (!open) _menuGuild.transform.localScale = Vector3.one;
            _menuNotice.text = S.SaveRefused ? Textos.SaveRecusado : S.Pending != null ? Textos.Pendente(S.Pending) : "";
            _menuNotice.color = S.SaveRefused ? Art.Bad : Art.Cargo;
        }

        public void RefreshBoard()
        {
            if (_rows == null) return;
            _boardCoins.text = Textos.Num(S.Coins);
            _boardStars.text = Textos.Estrelas(S.Stars, _rows.Length * 3);
            if (S.SaveRefused) { _notice.text = Textos.SaveRecusado; _notice.color = Art.Bad; }
            else if (S.Pending != null) { _notice.text = Textos.Pendente(S.Pending); _notice.color = Art.Cargo; }
            else _notice.text = "";
            for (int i = 0; i < _rows.Length; i++)
            {
                Row row = _rows[i];
                bool open = S.Unlocked(i) && !S.SaveRefused;
                row.B.interactable = open;
                row.G.alpha = open ? 1f : 0.55f;
                bool done = i < S.Reputation;
                row.Lock.enabled = !open;
                row.BadgeText.enabled = open;
                row.BadgeText.text = done ? Textos.Repetir : Textos.Jogar;
                row.Badge.color = !open ? Art.Off : done ? Art.Gold : Art.Good;
                int st = S.StarsOf(i);
                for (int k = 0; k < 3; k++)
                {
                    row.Stars[k].sprite = k < st ? Art.Star() : Art.StarHollow();
                    row.Stars[k].color = k < st ? Art.Gold : Art.A(Art.Tinta, 0.4f);
                }
            }
            RefreshDaily();
            bool code = S.CodeOpen && !S.SaveRefused;
            _codeBtn.interactable = code;
            _codeLock.enabled = !code;
        }

        void RefreshDaily()
        {
            int hoje = _g.Hoje;
            bool open = S.DailyOpen && !S.SaveRefused, avail = open && S.DailyAvailable(hoje);
            ContractDef d = open ? Sim.DailyContract(hoje) : null;
            _dayBtn.interactable = avail;
            _dayG.alpha = open ? 1f : 0.55f;
            _dayIcon.enabled = _dayEdge.enabled = d != null;
            if (d != null)
            {
                _dayIcon.sprite = _dayEdge.sprite = Art.CargoSprite(d.Cargo);
                _dayIcon.color = Art.CargoColor(d.Cargo);
            }
            _dayLine.text = !open ? Textos.DiaFechado : !avail ? Textos.DiaFeito : Textos.DiaLinha(Textos.Carga(d.Cargo), Balance.DailyBonus);
            _dayLock.enabled = !open;
            _dayBadgeText.enabled = open;
            _dayBadgeText.text = avail ? Textos.Jogar : Textos.Feito;
            _dayBadge.color = !open ? Art.Off : avail ? Art.Good : Art.Gold;
        }

        void RefreshGuild()
        {
            if (_ups == null) return;
            _guildCoins.text = Textos.Num(S.Coins);
            _guildStars.text = Textos.Num(S.Stars);
            _guildUps.gameObject.SetActive(!_guildWeapons);
            _guildWeps.gameObject.SetActive(_guildWeapons);
            Tab(_tabUps, _tabUpsLine, !_guildWeapons);
            Tab(_tabWeps, _tabWepsLine, _guildWeapons);
            for (int u = 0; u < _ups.Length; u++)
            {
                UpRow row = _ups[u];
                int lv = S.Levels[u], cost = S.UpgradeCost((Upgrade)u);
                row.Level.text = Textos.Nivel(lv);
                for (int k = 0; k < row.Dots.Length; k++) row.Dots[k].color = k < lv ? Art.Courier : Art.A(Art.Tinta, 0.25f);
                row.Cost.text = cost < 0 ? "" : Textos.Num(cost);
                row.CostCoin.enabled = cost >= 0;
                bool can = S.CanBuy((Upgrade)u);
                row.Buy.interactable = can;
                row.BuyText.text = cost < 0 ? Textos.Maximo : Textos.Comprar;
                ((Image)row.Buy.targetGraphic).color = can ? Art.Good : Art.Off;
            }
            for (int w = 0; w < _weps.Length; w++)
            {
                WRow row = _weps[w];
                Weapon wp = (Weapon)w;
                bool unlocked = S.WeaponUnlocked(wp), equipped = S.Equipped == wp;
                int lv = S.WeaponLevel(wp), cost = S.WeaponCost(wp);
                row.G.alpha = unlocked ? 1f : 0.6f;
                row.Lock.enabled = !unlocked;
                row.Need.text = unlocked ? "" : Textos.PrecisaEstrelas(WeaponDef.All[w].Stars);
                row.Level.text = unlocked ? Textos.ArmaNivel(lv, WeaponDef.MaxLevel) : "";
                for (int k = 0; k < row.Dots.Length; k++)
                {
                    row.Dots[k].enabled = unlocked;
                    row.Dots[k].color = k < lv ? Art.Courier : Art.A(Art.Tinta, 0.25f);
                }
                row.Equip.interactable = unlocked && !S.InContract;   // equipada fica "acesa" (dourada + visto); tocar de novo nao faz nada
                row.EquipText.text = equipped ? Textos.Equipada : Textos.Equipar;
                row.Check.enabled = equipped;
                ((Image)row.Equip.targetGraphic).color = equipped ? Art.Gold : unlocked ? Art.Good : Art.Off;
                bool can = S.CanUpgradeWeapon(wp);
                row.Up.interactable = can;
                row.Up.gameObject.SetActive(unlocked);
                row.UpText.text = cost < 0 ? Textos.Maximo : Textos.Melhorar;
                row.Cost.text = cost < 0 ? "" : Textos.Num(cost);
                row.UpCoin.enabled = cost >= 0;
                ((Image)row.Up.targetGraphic).color = can ? Art.Good : Art.Off;
            }
        }

        /// <summary>Aba selecionada: papel claro + sublinhado de tinta (forma, nao so cor).</summary>
        static void Tab(Button b, Image line, bool on)
        {
            ((Image)b.targetGraphic).color = on ? Art.Paper : Art.PaperEdge;
            line.enabled = on;
            b.GetComponentInChildren<Text>().fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
        }

        void RefreshSettings()
        {
            if (_volSomT == null) return;
            _volSomT.text = Textos.Volume(Textos.Som, Ajustes.VolSom * 100 / Ajustes.VolMax);
            _volMusT.text = Textos.Volume(Textos.Musica, Ajustes.VolMusica * 100 / Ajustes.VolMax);
            for (int k = 0; k < Ajustes.VolMax; k++)
            {
                _barSom[k].color = k < Ajustes.VolSom ? Art.Courier : Art.A(Art.Tinta, 0.2f);
                _barMus[k].color = k < Ajustes.VolMusica ? Art.Courier : Art.A(Art.Tinta, 0.2f);
            }
            _vibraBtn.interactable = Ajustes.TemVibra;
            _vibraBtn.GetComponentInChildren<Text>().text = Ajustes.TemVibra ? Textos.Vibra(Ajustes.Vibra) : Textos.VibraSoCelular;
            bool en = Ajustes.Ingles;
            _ptCheck.enabled = !en; _enCheck.enabled = en;
            ((Image)_ptBtn.targetGraphic).color = en ? Art.PaperEdge : Art.Good;
            ((Image)_enBtn.targetGraphic).color = en ? Art.Good : Art.PaperEdge;
            _versionT.text = Textos.Versao(_g.Version) + "  ·  " + Textos.IdAnonimo(_g.InstallShort);
        }

        // ------------------------------------------------------------------ acoes

        void SubmitCode()
        {
            string c = ContractCode.Normalize(_codeField.text ?? "");
            if (!ContractCode.IsValid(c) || Sim.CodeContract(c) == null) { _codeError.text = Textos.CodigoInvalido; return; }
            _code.gameObject.SetActive(false);
            _g.PlayCode(c);
        }

        void Vol(bool music, int d)
        {
            if (music) Ajustes.VolMusica += d; else Ajustes.VolSom += d;
            RefreshSettings();
            _g.Log("settings", music ? "musica" : "som", Textos.Num(music ? Ajustes.VolMusica : Ajustes.VolSom));
        }

        void ToggleVibra()
        {
            if (!Ajustes.TemVibra) return;
            Ajustes.Vibra = !Ajustes.Vibra;
            Ajustes.Vibrar(0f);
            RefreshSettings();
            _g.Log("settings", "vibra", Ajustes.Vibra ? "1" : "0");
        }

        void Export()
        {
            Exportar.Status st = _g.ExportarDiario();
            _exportStatus.text = st == Exportar.Status.Pasta ? Textos.ExportPasta : st == Exportar.Status.Escolha ? Textos.ExportEscolha
                : st == Exportar.Status.Vazio ? Textos.ExportVazio : Textos.ExportErro;
            _exportStatus.color = st == Exportar.Status.Erro ? Art.BadInk : Art.Tinta;
        }

        static Sprite PerkIcon(Perk p, out Color c)
        {
            switch (p)
            {
                case Perk.LightStep: c = Art.Shot; return Art.Chevrons();
                case Perk.QuickPlant: c = Art.Cargo; return Art.Flag();
                case Perk.DoubleDash: c = Art.Shot; return Art.Chevrons();
                case Perk.Padded: c = Art.Cargo; return Art.Rounded();
                case Perk.QuickHand: c = Art.Shot; return Art.Sling();
                case Perk.Magnet: c = Art.Coin; return Art.CoinShape();
                case Perk.ExtraHeart: c = Art.Bad; return Art.Heart();
                case Perk.Lookout: c = Art.Ink; return Art.Eyes();
                case Perk.IronBoot: c = Art.Steel; return Art.Fists();
                case Perk.ChestLuck: c = Art.Coin; return Art.ChestShape();
                default: c = Art.Ink; return Art.Star();
            }
        }

        static Sprite WeaponIcon(Weapon w) => w == Weapon.Boomerang ? Art.Boomerang() : w == Weapon.Bomb ? Art.Bomb() : Art.Sling();

        static Sprite UpgradeIcon(Upgrade u, out Color c)
        {
            switch (u)
            {
                case Upgrade.Life: c = Art.Bad; return Art.Heart();
                case Upgrade.Dash: c = Art.Courier; return Art.Chevrons();
                case Upgrade.Bag: c = Art.Coin; return Art.Bag();
                default: c = Art.Leather; return Art.Rounded();   // Mochila
            }
        }

        // ------------------------------------------------------------------ construcao: titulo e menu

        RectTransform BuildTitle()
        {
            RectTransform t = Art.Node(_safe, "Titulo", Vector2.zero, Vector2.one);
            Image bg = t.gameObject.AddComponent<Image>(); bg.color = Art.Bg;
            Button tap = t.gameObject.AddComponent<Button>();
            tap.targetGraphic = bg; tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => { Audio.Play("confirmar"); ShowMenu(); });
            Emblem(t, 0.62f, 0.86f);
            Logo(t, 0.4f, 0.6f, 170);
            Text slogan = Loc(Art.NewText(t, "Slogan", 44, new Vector2(0.05f, 0.335f), new Vector2(0.95f, 0.39f)), () => Textos.Slogan);
            slogan.color = Art.Dim; slogan.fontStyle = FontStyle.Bold;
            _tapText = Loc(Art.Outlined(Art.NewText(t, "Tocar", 52, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.26f))), () => Textos.TocarParaComecar);
            _tapText.fontStyle = FontStyle.Bold;
            Text priv = Loc(Art.NewText(t, "Privacidade", 30, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.145f)), () => Textos.PrivacidadeCurta);
            priv.color = Art.Dim; Shrink(priv, 20);
            Text ver = Loc(Art.NewText(t, "Versao", 28, new Vector2(0.05f, 0.012f), new Vector2(0.95f, 0.05f)), () => Textos.Versao(_g.Version));
            ver.color = Art.Dim;
            return t;
        }

        /// <summary>Selo da guilda (ART_BIBLE §12): envelope de papel cruzado por uma chave dourada, lacre azul com estrela.</summary>
        static void Emblem(Transform parent, float y0, float y1)
        {
            RectTransform e = Art.Node(parent, "Selo", new Vector2(0.18f, y0), new Vector2(0.82f, y1));
            Image glow = Art.Img(e, "Brilho", Art.Glow(), Art.A(Art.Gold, 0.22f), new Vector2(-0.15f, -0.25f), new Vector2(1.15f, 1.25f));
            glow.preserveAspect = false;
            Art.Img(e, "Envelope", Art.Envelope(), Art.Paper, new Vector2(0.08f, 0f), new Vector2(0.92f, 1f));
            Art.Img(e, "Chave", Art.Key(), Art.Gold, new Vector2(0.02f, -0.06f), new Vector2(0.98f, 1.06f));
            Image seal = Art.Img(e, "Lacre", Art.Disc(), Art.Courier, new Vector2(0.4f, 0.3f), new Vector2(0.6f, 0.62f));
            Art.Img(seal.transform, "Estrela", Art.Star(), Art.Gold, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
        }

        /// <summary>Nome do jogo em 2 linhas, negrito com contorno grosso e sombra (tipografia, sem imagem externa).</summary>
        static void Logo(Transform parent, float y0, float y1, int size)
        {
            float mid = (y0 + y1) * 0.5f;
            Text a = Art.NewText(parent, "Nome1", size, new Vector2(0.03f, mid), new Vector2(0.97f, y1));
            Text b = Art.NewText(parent, "Nome2", size, new Vector2(0.03f, y0), new Vector2(0.97f, mid));
            a.text = Textos.Nome1; b.text = Textos.Nome2;
            a.color = Art.Ink; b.color = Art.Gold;
            foreach (Text t in new[] { a, b })
            {
                t.fontStyle = FontStyle.Bold;
                Shrink(t, size / 2);
                Art.Outlined(t, size / 25f);
                Shadow sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = Art.A(Art.Dark, 0.9f); sh.effectDistance = new Vector2(0f, -size / 14f);
            }
        }

        RectTransform BuildMenu()
        {
            RectTransform m = Art.Node(_safe, "Menu", Vector2.zero, Vector2.one);
            Image bg = m.gameObject.AddComponent<Image>(); bg.color = Art.Bg;
            Logo(m, 0.81f, 0.975f, 120);
            _menuCoins = Pill(m, Art.CoinShape(), Art.Coin, new Vector2(0.06f, 0.725f), new Vector2(0.47f, 0.79f));
            _menuStars = Pill(m, Art.Star(), Art.Gold, new Vector2(0.53f, 0.725f), new Vector2(0.94f, 0.79f));
            _menuNotice = Shrink(Art.NewText(m, "Aviso", 28, new Vector2(0.05f, 0.655f), new Vector2(0.95f, 0.718f)), 20);
            _menuNotice.fontStyle = FontStyle.Bold;
            Btn(m, () => Textos.MenuJogar, 64, Art.Good, new Vector2(0.1f, 0.525f), new Vector2(0.9f, 0.645f), ShowBoard, "confirmar");
            _menuGuild = Btn(m, () => Textos.MenuGuilda, 52, Art.Paper, new Vector2(0.1f, 0.405f), new Vector2(0.9f, 0.51f), () => ShowGuild(false));
            _menuGuildLock = Art.Img(_menuGuild.transform, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.05f, 0.2f), new Vector2(0.14f, 0.8f));
            _menuGuildNote = Loc(Art.NewText(m, "GuildaNota", 28, new Vector2(0.05f, 0.362f), new Vector2(0.95f, 0.402f)), () => Textos.GuildaFechada);
            _menuGuildNote.color = Art.Dim;
            Btn(m, () => Textos.MenuConfig, 46, Art.PaperEdge, new Vector2(0.1f, 0.245f), new Vector2(0.9f, 0.35f), ShowSettings);
            Btn(m, () => Textos.MenuCreditos, 46, Art.PaperEdge, new Vector2(0.1f, 0.125f), new Vector2(0.9f, 0.23f), ShowCredits);
            Text ver = Loc(Art.NewText(m, "Versao", 28, new Vector2(0.05f, 0.012f), new Vector2(0.95f, 0.05f)), () => Textos.Versao(_g.Version));
            ver.color = Art.Dim;
            return m;
        }

        /// <summary>Pilula escura com icone e numero (moedas, estrelas).</summary>
        static Text Pill(Transform parent, Sprite icon, Color c, Vector2 min, Vector2 max)
        {
            Image bg = Art.Panel(parent, "Pilula", Art.A(Art.Tinta, 0.85f), min, max);
            bg.raycastTarget = false;
            Art.Img(bg.transform, "Icone", icon, c, new Vector2(0.05f, 0.14f), new Vector2(0.25f, 0.86f));
            Text t = Art.Outlined(Art.NewText(bg.transform, "Valor", 50, new Vector2(0.28f, 0f), new Vector2(0.97f, 1f), TextAnchor.MiddleLeft));
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        // ------------------------------------------------------------------ construcao: quadro de contratos

        RectTransform BuildBoard()
        {
            RectTransform b = Art.Node(_safe, "Quadro", Vector2.zero, Vector2.one);
            Image bg = b.gameObject.AddComponent<Image>(); bg.color = Art.Bg;
            Btn(b, () => Textos.Voltar, 40, Art.PaperEdge, new Vector2(0.02f, 0.918f), new Vector2(0.27f, 0.996f), ShowMenu);
            Text title = Loc(Art.Outlined(Art.NewText(b, "Titulo", 50, new Vector2(0.28f, 0.918f), new Vector2(0.72f, 0.996f))), () => Textos.Regiao);
            title.fontStyle = FontStyle.Bold; Shrink(title, 30);
            Art.Img(b, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.735f, 0.935f), new Vector2(0.795f, 0.98f));
            _boardCoins = Art.Outlined(Art.NewText(b, "Moedas", 46, new Vector2(0.8f, 0.918f), new Vector2(0.99f, 0.996f), TextAnchor.MiddleLeft));
            _boardCoins.fontStyle = FontStyle.Bold;
            Art.Img(b, "Estrela", Art.Star(), Art.Gold, new Vector2(0.03f, 0.872f), new Vector2(0.085f, 0.908f));
            _boardStars = Art.Outlined(Art.NewText(b, "Estrelas", 40, new Vector2(0.09f, 0.864f), new Vector2(0.36f, 0.914f), TextAnchor.MiddleLeft));
            _boardStars.fontStyle = FontStyle.Bold;
            _notice = Shrink(Art.NewText(b, "Aviso", 26, new Vector2(0.37f, 0.862f), new Vector2(0.98f, 0.916f)), 18);
            _notice.fontStyle = FontStyle.Bold;
            BuildDaily(b, new Vector2(0.02f, 0.745f), new Vector2(0.66f, 0.855f));
            _codeBtn = Btn(b, () => Textos.DigitarCodigo, 38, Art.Paper, new Vector2(0.68f, 0.745f), new Vector2(0.98f, 0.855f), ShowCode);
            _codeLock = Art.Img(_codeBtn.transform, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.78f, 0.6f), new Vector2(0.96f, 0.95f));

            // lista rolavel dos contratos da campanha
            RectTransform view = Art.Node(b, "Lista", new Vector2(0.02f, 0.008f), new Vector2(0.98f, 0.735f));
            view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // pega o arrasto entre os cartoes
            view.gameObject.AddComponent<RectMask2D>();
            _scrollContent = Art.Node(view, "Conteudo", new Vector2(0f, 1f), new Vector2(1f, 1f));
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            int n = Sim.Contracts.Length;
            _scrollContent.sizeDelta = new Vector2(0f, n * (RowH + RowGap));
            _scroll = view.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _scrollContent; _scroll.viewport = view;
            _scroll.horizontal = false; _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Elastic; _scroll.scrollSensitivity = 60f;
            _rows = new Row[n];
            for (int i = 0; i < n; i++)
            {
                RectTransform holder = Art.Node(_scrollContent, "Linha", new Vector2(0f, 1f), new Vector2(1f, 1f));
                holder.pivot = new Vector2(0.5f, 1f);
                holder.anchoredPosition = new Vector2(0f, -i * (RowH + RowGap));
                holder.sizeDelta = new Vector2(0f, RowH);
                _rows[i] = BuildRow(holder, i);
            }
            return b;
        }

        Row BuildRow(RectTransform holder, int i)
        {
            ContractDef c = Sim.Contracts[i];
            Image face = Card(holder, "Contrato " + c.Code, Vector2.zero, Vector2.one);
            var row = new Row { G = holder.gameObject.AddComponent<CanvasGroup>() };
            row.B = face.gameObject.AddComponent<Button>();
            row.B.targetGraphic = face;
            int k = i;
            row.B.onClick.AddListener(() => { Audio.Play("confirmar"); _g.Play(k); });
            Transform f = face.transform;
            Text code = Shrink(Ink(f, "Codigo", 44, new Vector2(0.01f, 0f), new Vector2(0.115f, 1f), TextAnchor.MiddleCenter, true), 26);
            code.text = c.Code;
            bool boss = false, mini = false;
            foreach (int[] layer in c.Layers) { RoomDef r = Content.Rooms[layer[0]]; boss |= r.Boss; mini |= r.Mini; }
            if (boss || mini)   // icone do chefe (ART_BIBLE §10): chifres dourados (chefe) ou roxos (Capitao Ladrao)
                Art.Img(f, "Chefe", Art.Horns(), boss ? Art.Gold : Art.Role(EnemyType.Captain), new Vector2(0.02f, 0.68f), new Vector2(0.105f, 0.98f));
            Art.Img(f, "CargaContorno", Art.CargoSprite(c.Cargo), Art.Cargo, new Vector2(0.12f, 0.12f), new Vector2(0.22f, 0.88f));
            Art.Img(f, "Carga", Art.CargoSprite(c.Cargo), Art.CargoColor(c.Cargo), new Vector2(0.13f, 0.2f), new Vector2(0.21f, 0.8f));
            Shrink(Loc(Ink(f, "Linha1", 34, new Vector2(0.235f, 0.5f), new Vector2(0.66f, 0.96f), TextAnchor.MiddleLeft, true),
                () => Textos.Carga(c.Cargo) + " · " + Textos.Salas(c)), 20);
            Text l2 = Shrink(Loc(Ink(f, "Linha2", 30, new Vector2(0.235f, 0.04f), new Vector2(0.66f, 0.5f), TextAnchor.MiddleLeft), () => Textos.Verbo(c)), 18);
            l2.color = Art.A(Art.Tinta, 0.78f);
            Art.Img(f, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.665f, 0.28f), new Vector2(0.715f, 0.72f));
            Text pay = Ink(f, "Pagamento", 36, new Vector2(0.72f, 0f), new Vector2(0.81f, 1f), TextAnchor.MiddleLeft, true);
            pay.text = Textos.Num(c.Pay);
            row.Badge = Art.Panel(f, "Selo", Art.Good, new Vector2(0.815f, 0.42f), new Vector2(0.99f, 0.93f));
            row.Badge.raycastTarget = false;
            row.BadgeText = Shrink(Ink(row.Badge.transform, "Rotulo", 26, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, true), 14);
            row.Lock = Art.Img(row.Badge.transform, "Cadeado", Art.Lock(), Art.Ink, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f));
            for (int s = 0; s < 3; s++)
                row.Stars[s] = Art.Img(f, "Estrela", Art.StarHollow(), Art.Tinta, new Vector2(0.82f + s * 0.057f, 0.06f), new Vector2(0.872f + s * 0.057f, 0.38f));
            return row;
        }

        /// <summary>Cartao do contrato do dia: faixa e estrela douradas (destaque), carga do dia, recompensa extra e estado.</summary>
        void BuildDaily(Transform parent, Vector2 min, Vector2 max)
        {
            Image face = Card(parent, "ContratoDoDia", min, max);
            _dayG = face.transform.parent.parent.gameObject.AddComponent<CanvasGroup>();
            _dayBtn = face.gameObject.AddComponent<Button>();
            _dayBtn.targetGraphic = face;
            _dayBtn.onClick.AddListener(() => { Audio.Play("confirmar"); _g.PlayDaily(); });
            Transform f = face.transform;
            Image band = Art.Img(f, "Destaque", Art.Square(), Art.Gold, new Vector2(0f, 0f), new Vector2(0.03f, 1f));
            band.preserveAspect = false;
            Art.Img(f, "Estrela", Art.Star(), Art.Gold, new Vector2(0.045f, 0.56f), new Vector2(0.14f, 0.95f));
            Shrink(Loc(Ink(f, "Titulo", 36, new Vector2(0.15f, 0.55f), new Vector2(0.74f, 0.97f), TextAnchor.MiddleLeft, true), () => Textos.Dia), 22);
            _dayEdge = Art.Img(f, "CargaContorno", Art.Rounded(), Art.Cargo, new Vector2(0.045f, 0.06f), new Vector2(0.14f, 0.52f));
            _dayIcon = Art.Img(f, "Carga", Art.Rounded(), Color.white, new Vector2(0.058f, 0.12f), new Vector2(0.127f, 0.46f));
            _dayLine = Shrink(Ink(f, "Linha", 28, new Vector2(0.15f, 0.04f), new Vector2(0.75f, 0.54f), TextAnchor.MiddleLeft), 18);
            _dayBadge = Art.Panel(f, "Selo", Art.Good, new Vector2(0.76f, 0.2f), new Vector2(0.98f, 0.8f));
            _dayBadge.raycastTarget = false;
            _dayBadgeText = Shrink(Ink(_dayBadge.transform, "Rotulo", 28, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, true), 14);
            _dayLock = Art.Img(_dayBadge.transform, "Cadeado", Art.Lock(), Art.Ink, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f));
        }

        // ------------------------------------------------------------------ construcao: codigo

        RectTransform BuildCode()
        {
            RectTransform m = Modal("Codigo");
            // metade de cima da tela: o teclado do aparelho cobre a de baixo
            Image face = Card(m, "Cartao", new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.96f));
            Transform f = face.transform;
            Loc(Ink(f, "Titulo", 50, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f), TextAnchor.MiddleCenter, true), () => Textos.CodigoTitulo);
            Shrink(Loc(Ink(f, "Corpo", 30, new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.86f)), () => Textos.CodigoCorpo), 20);
            Image edge = Art.Panel(f, "CampoBorda", Art.Tinta, new Vector2(0.14f, 0.45f), new Vector2(0.86f, 0.67f));
            edge.raycastTarget = false;
            Image field = Art.Panel(edge.transform, "Campo", Color.white, Vector2.zero, Vector2.one);
            field.rectTransform.offsetMin = new Vector2(5f, 5f); field.rectTransform.offsetMax = new Vector2(-5f, -5f);
            Text txt = Ink(field.transform, "Texto", 80, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, true);
            txt.supportRichText = false;
            Text ph = Loc(Ink(field.transform, "Exemplo", 56, Vector2.zero, Vector2.one), () => Textos.CodigoDica);
            ph.color = Art.A(Art.Tinta, 0.4f); ph.fontStyle = FontStyle.Italic;
            _codeField = field.gameObject.AddComponent<InputField>();
            _codeField.targetGraphic = field;
            _codeField.textComponent = txt;
            _codeField.placeholder = ph;
            _codeField.contentType = InputField.ContentType.Alphanumeric;
            _codeField.lineType = InputField.LineType.SingleLine;
            _codeField.characterLimit = 6;
            _codeField.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            // so os simbolos do codigo (ContractCode.Alphabet: sem I, O, 0, 1), em maiusculas; o Core confere o digito de verificacao no JOGAR
            _codeField.onValidateInput = (s, i, ch) => ContractCode.Alphabet.IndexOf(char.ToUpperInvariant(ch)) >= 0 ? char.ToUpperInvariant(ch) : '\0';
            _codeError = Shrink(Ink(f, "Erro", 28, new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.44f), TextAnchor.MiddleCenter, true), 18);
            _codeError.color = Art.BadInk;
            Btn(f, () => Textos.JogarCodigo, 40, Art.Good, new Vector2(0.05f, 0.04f), new Vector2(0.6f, 0.3f), SubmitCode, "confirmar");
            Btn(f, () => Textos.Voltar, 40, Art.PaperEdge, new Vector2(0.63f, 0.04f), new Vector2(0.95f, 0.3f), () => _code.gameObject.SetActive(false));
            return m;
        }

        // ------------------------------------------------------------------ construcao: guilda (melhorias + armas)

        RectTransform BuildGuild()
        {
            RectTransform m = Modal("Guilda");
            Image face = Card(m, "Cartao", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
            Transform f = face.transform;
            Loc(Ink(f, "Titulo", 56, new Vector2(0.05f, 0.925f), new Vector2(0.95f, 0.99f), TextAnchor.MiddleCenter, true), () => Textos.Guilda);
            Art.Img(f, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.2f, 0.873f), new Vector2(0.26f, 0.915f));
            _guildCoins = Ink(f, "Moedas", 42, new Vector2(0.27f, 0.865f), new Vector2(0.48f, 0.923f), TextAnchor.MiddleLeft, true);
            Art.Img(f, "Estrela", Art.Star(), Art.Gold, new Vector2(0.54f, 0.873f), new Vector2(0.6f, 0.915f));
            _guildStars = Ink(f, "Estrelas", 42, new Vector2(0.61f, 0.865f), new Vector2(0.85f, 0.923f), TextAnchor.MiddleLeft, true);
            _tabUps = Btn(f, () => Textos.AbaMelhorias, 38, Art.PaperEdge, new Vector2(0.04f, 0.77f), new Vector2(0.49f, 0.855f), () => { _guildWeapons = false; RefreshGuild(); });
            _tabWeps = Btn(f, () => Textos.AbaArmas, 38, Art.PaperEdge, new Vector2(0.51f, 0.77f), new Vector2(0.96f, 0.855f), () => { _guildWeapons = true; RefreshGuild(); });
            _tabUpsLine = Underline(_tabUps.transform);
            _tabWepsLine = Underline(_tabWeps.transform);

            _guildUps = Art.Node(f, "Melhorias", new Vector2(0.03f, 0.115f), new Vector2(0.97f, 0.755f));
            _ups = new UpRow[Balance.UpgradeLines];
            for (int u = 0; u < _ups.Length; u++) _ups[u] = BuildUpRow(_guildUps, u, _ups.Length);
            _guildWeps = Art.Node(f, "Armas", new Vector2(0.03f, 0.115f), new Vector2(0.97f, 0.755f));
            _weps = new WRow[WeaponDef.All.Length];
            for (int w = 0; w < _weps.Length; w++) _weps[w] = BuildWeaponRow(_guildWeps, w, _weps.Length);

            Btn(f, () => Textos.Voltar, 40, Art.PaperEdge, new Vector2(0.25f, 0.015f), new Vector2(0.75f, 0.1f), () => _guild.gameObject.SetActive(false));
            return m;
        }

        static Image Underline(Transform tab)
        {
            Image line = Art.Img(tab, "Sublinhado", Art.Square(), Art.Tinta, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.12f));
            line.preserveAspect = false;
            return line;
        }

        UpRow BuildUpRow(Transform parent, int u, int count)
        {
            float h = 1f / count, y1 = 1f - u * h, y0 = y1 - h + 0.015f;
            RectTransform r = Art.Node(parent, "Linha", new Vector2(0f, y0), new Vector2(1f, y1));
            Image sep = Art.Panel(r, "Fundo", Art.A(Art.PaperEdge, 0.35f), Vector2.zero, Vector2.one); sep.raycastTarget = false;
            Upgrade up = (Upgrade)u;
            Art.Img(r, "Icone", UpgradeIcon(up, out Color tint), tint, new Vector2(0.02f, 0.2f), new Vector2(0.15f, 0.8f));
            var row = new UpRow { Dots = new Image[Balance.UpgradeMax] };
            Shrink(Loc(Ink(r, "Nome", 38, new Vector2(0.17f, 0.62f), new Vector2(0.6f, 0.95f), TextAnchor.MiddleLeft, true), () => Textos.UpNome(up)), 24);
            Text eff = Shrink(Loc(Ink(r, "Efeito", 26, new Vector2(0.17f, 0.36f), new Vector2(0.62f, 0.62f), TextAnchor.MiddleLeft), () => Textos.UpEfeito(up)), 16);
            eff.color = Art.A(Art.Tinta, 0.8f);
            float dw = 0.27f / row.Dots.Length;
            for (int k = 0; k < row.Dots.Length; k++)
                row.Dots[k] = Art.Img(r, "Nivel", Art.Disc(), Art.Off, new Vector2(0.17f + k * dw, 0.08f), new Vector2(0.17f + (k + 0.8f) * dw, 0.3f));
            row.Level = Shrink(Ink(r, "NivelTexto", 24, new Vector2(0.45f, 0.04f), new Vector2(0.62f, 0.34f), TextAnchor.MiddleLeft), 16);
            row.Level.color = Art.A(Art.Tinta, 0.75f);
            row.CostCoin = Art.Img(r, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.6f, 0.3f), new Vector2(0.66f, 0.7f));
            row.Cost = Ink(r, "Custo", 36, new Vector2(0.665f, 0f), new Vector2(0.77f, 1f), TextAnchor.MiddleLeft, true);
            row.Buy = Art.NewButton(r, "", 30, Art.Good, new Vector2(0.77f, 0.14f), new Vector2(0.99f, 0.86f), () => _g.Buy(up), "confirmar");
            row.BuyText = row.Buy.GetComponentInChildren<Text>();
            return row;
        }

        WRow BuildWeaponRow(Transform parent, int w, int count)
        {
            float h = 1f / count, y1 = 1f - w * h, y0 = y1 - h + 0.015f;
            RectTransform r = Art.Node(parent, "Arma", new Vector2(0f, y0), new Vector2(1f, y1));
            Image sep = Art.Panel(r, "Fundo", Art.A(Art.PaperEdge, 0.35f), Vector2.zero, Vector2.one); sep.raycastTarget = false;
            Weapon wp = (Weapon)w;
            var row = new WRow { G = r.gameObject.AddComponent<CanvasGroup>(), Dots = new Image[WeaponDef.MaxLevel] };
            Image disc = Art.Img(r, "Fundo", Art.Disc(), Art.Tinta, new Vector2(0.02f, 0.25f), new Vector2(0.15f, 0.75f));
            Art.Img(disc.transform, "Icone", WeaponIcon(wp), Art.Shot, new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
            row.Lock = Art.Img(r, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.1f, 0.15f), new Vector2(0.17f, 0.42f));
            Shrink(Loc(Ink(r, "Nome", 40, new Vector2(0.17f, 0.66f), new Vector2(0.63f, 0.96f), TextAnchor.MiddleLeft, true), () => Textos.ArmaNome(wp)), 24);
            Text eff = Shrink(Loc(Ink(r, "Efeito", 26, new Vector2(0.17f, 0.36f), new Vector2(0.63f, 0.66f), TextAnchor.MiddleLeft), () => Textos.ArmaEfeito(wp)), 16);
            eff.color = Art.A(Art.Tinta, 0.8f);
            for (int k = 0; k < row.Dots.Length; k++)
                row.Dots[k] = Art.Img(r, "Nivel", Art.Disc(), Art.Off, new Vector2(0.17f + k * 0.05f, 0.08f), new Vector2(0.21f + k * 0.05f, 0.3f));
            row.Level = Shrink(Ink(r, "NivelTexto", 24, new Vector2(0.34f, 0.04f), new Vector2(0.63f, 0.34f), TextAnchor.MiddleLeft), 16);
            row.Level.color = Art.A(Art.Tinta, 0.75f);
            row.Need = Shrink(Ink(r, "Precisa", 28, new Vector2(0.17f, 0.04f), new Vector2(0.63f, 0.36f), TextAnchor.MiddleLeft, true), 18);
            row.Equip = Art.NewButton(r, "", 32, Art.Good, new Vector2(0.65f, 0.53f), new Vector2(0.99f, 0.95f), () => _g.Equip(wp), "confirmar");
            row.EquipText = row.Equip.GetComponentInChildren<Text>();
            row.EquipText.rectTransform.anchorMin = new Vector2(0.2f, 0f);
            row.Check = Art.Img(row.Equip.transform, "Visto", Art.Check(), Art.Tinta, new Vector2(0.04f, 0.2f), new Vector2(0.2f, 0.8f));
            row.Up = Art.NewButton(r, "", 30, Art.Good, new Vector2(0.65f, 0.05f), new Vector2(0.99f, 0.47f), () => _g.UpgradeWeapon(wp), "confirmar");
            row.UpText = row.Up.GetComponentInChildren<Text>();
            row.UpText.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            row.UpCoin = Art.Img(row.Up.transform, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.62f, 0.25f), new Vector2(0.74f, 0.75f));
            row.Cost = Ink(row.Up.transform, "Custo", 32, new Vector2(0.75f, 0f), new Vector2(0.98f, 1f), TextAnchor.MiddleLeft, true);
            return row;
        }

        // ------------------------------------------------------------------ construcao: perk

        RectTransform BuildPerk()
        {
            RectTransform m = Modal("Perk", 0.8f);
            Text title = Loc(Art.Outlined(Art.NewText(m, "Titulo", 64, new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.95f)), 3f), () => Textos.PerkTitulo);
            title.fontStyle = FontStyle.Bold;
            Text sub = Loc(Art.NewText(m, "Sub", 32, new Vector2(0.04f, 0.815f), new Vector2(0.96f, 0.86f)), () => Textos.PerkSub);
            sub.color = Art.Dim;
            for (int i = 0; i < _perkCard.Length; i++)
            {
                float y1 = 0.79f - i * 0.257f, y0 = y1 - 0.237f;
                Image face = Card(m, "Perk", new Vector2(0.05f, y0), new Vector2(0.95f, y1));
                _perkCard[i] = face.transform.parent.parent.gameObject;
                Button b = face.gameObject.AddComponent<Button>();
                b.targetGraphic = face;
                int k = i;
                b.onClick.AddListener(() => { _perk.gameObject.SetActive(false); _g.PickPerk(k); });   // som: "perk" no Ev.PerkPick
                Image disc = Art.Img(face.transform, "Fundo", Art.Disc(), Art.Tinta, new Vector2(0.03f, 0.15f), new Vector2(0.25f, 0.85f));
                _perkIcon[i] = Art.Img(disc.transform, "Icone", Art.Star(), Art.Ink, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
                _perkName[i] = Shrink(Ink(face.transform, "Nome", 56, new Vector2(0.28f, 0.5f), new Vector2(0.97f, 0.92f), TextAnchor.MiddleLeft, true), 30);
                _perkEff[i] = Shrink(Ink(face.transform, "Efeito", 36, new Vector2(0.28f, 0.08f), new Vector2(0.97f, 0.5f), TextAnchor.MiddleLeft), 22);
            }
            return m;
        }

        // ------------------------------------------------------------------ construcao: configuracoes e creditos

        RectTransform BuildSettings()
        {
            RectTransform m = Modal("Configuracoes", 0.75f);
            Image face = Card(m, "Cartao", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
            Transform f = face.transform;
            Loc(Ink(f, "Titulo", 56, new Vector2(0.05f, 0.925f), new Vector2(0.95f, 0.99f), TextAnchor.MiddleCenter, true), () => Textos.Config);
            _volSomT = VolRow(f, false, 0.82f, 0.91f, _barSom);
            _volMusT = VolRow(f, true, 0.715f, 0.805f, _barMus);
            _vibraBtn = Art.NewButton(f, "", 40, Art.PaperEdge, new Vector2(0.04f, 0.61f), new Vector2(0.96f, 0.7f), ToggleVibra);
            Loc(Ink(f, "Idioma", 34, new Vector2(0.05f, 0.555f), new Vector2(0.95f, 0.6f), TextAnchor.MiddleLeft, true), () => Textos.Idioma);
            _ptBtn = Art.NewButton(f, Textos.Portugues, 40, Art.PaperEdge, new Vector2(0.04f, 0.46f), new Vector2(0.49f, 0.55f), () => _g.SetIdioma(false));
            _enBtn = Art.NewButton(f, Textos.English, 40, Art.PaperEdge, new Vector2(0.51f, 0.46f), new Vector2(0.96f, 0.55f), () => _g.SetIdioma(true));
            _ptCheck = Art.Img(_ptBtn.transform, "Visto", Art.Check(), Art.Tinta, new Vector2(0.04f, 0.22f), new Vector2(0.18f, 0.78f));
            _enCheck = Art.Img(_enBtn.transform, "Visto", Art.Check(), Art.Tinta, new Vector2(0.04f, 0.22f), new Vector2(0.18f, 0.78f));
            Btn(f, () => Textos.ExportarDiario, 42, Art.Gold, new Vector2(0.04f, 0.355f), new Vector2(0.96f, 0.445f), Export);
            Text help = Shrink(Loc(Ink(f, "Ajuda", 26, new Vector2(0.05f, 0.29f), new Vector2(0.95f, 0.35f)), () => Textos.ExportarAjuda), 18);
            help.color = Art.A(Art.Tinta, 0.82f);
            _exportStatus = Shrink(Ink(f, "Status", 28, new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.29f), TextAnchor.MiddleCenter, true), 18);
            Text priv = Shrink(Loc(Ink(f, "Privacidade", 26, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.245f)), () => Textos.Privacidade), 18);
            priv.color = Art.A(Art.Tinta, 0.82f);
            _versionT = Shrink(Ink(f, "Versao", 26, new Vector2(0.05f, 0.105f), new Vector2(0.95f, 0.15f)), 16);
            _versionT.color = Art.A(Art.Tinta, 0.75f);
            Btn(f, () => Textos.Voltar, 42, Art.PaperEdge, new Vector2(0.25f, 0.015f), new Vector2(0.75f, 0.1f), () => _settings.gameObject.SetActive(false));
            return m;
        }

        /// <summary>Linha de volume: rotulo com a %, [-], 10 barras, [+]. Botoes grandes em vez de slider (alvo de toque e precisao).</summary>
        Text VolRow(Transform parent, bool music, float y0, float y1, Image[] bar)
        {
            RectTransform r = Art.Node(parent, music ? "Musica" : "Som", new Vector2(0.04f, y0), new Vector2(0.96f, y1));
            Text label = Shrink(Ink(r, "Rotulo", 36, new Vector2(0f, 0f), new Vector2(0.34f, 1f), TextAnchor.MiddleLeft, true), 22);
            Art.NewButton(r, "-", 72, Art.PaperEdge, new Vector2(0.35f, 0.03f), new Vector2(0.51f, 0.97f), () => Vol(music, -1));
            float w = 0.29f / bar.Length;
            for (int k = 0; k < bar.Length; k++)
            {
                bar[k] = Art.Img(r, "Nivel", Art.Square(), Art.Off, new Vector2(0.525f + k * w, 0.22f), new Vector2(0.525f + (k + 0.75f) * w, 0.78f));
                bar[k].preserveAspect = false;
            }
            Art.NewButton(r, "+", 72, Art.PaperEdge, new Vector2(0.83f, 0.03f), new Vector2(0.99f, 0.97f), () => Vol(music, +1));
            return label;
        }

        RectTransform BuildCredits()
        {
            RectTransform m = Modal("Creditos", 0.75f);
            Image face = Card(m, "Cartao", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            Transform f = face.transform;
            Loc(Ink(f, "Titulo", 56, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f), TextAnchor.MiddleCenter, true), () => Textos.Creditos);
            Text game = Ink(f, "Jogo", 60, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.9f), TextAnchor.MiddleCenter, true);
            game.text = "Dungeon Courier";
            Shrink(Loc(Ink(f, "Corpo", 36, new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.82f)), () => Textos.CreditosCorpo), 22);
            Text priv = Shrink(Loc(Ink(f, "Privacidade", 28, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.38f)), () => Textos.Privacidade), 18);
            priv.color = Art.A(Art.Tinta, 0.82f);
            Btn(f, () => Textos.Voltar, 42, Art.PaperEdge, new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.115f), () => _credits.gameObject.SetActive(false));
            return m;
        }
    }
}
