using System;
using System.Collections.Generic;
using DC.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Rect = UnityEngine.Rect;

namespace DC
{
    /// <summary>
    /// HUD e telas por codigo (uGUI, referencia 1080 x 1920 casando a LARGURA, dentro da area segura). No contrato: faixa de cima
    /// (Band) com coracoes, moedas, pausa, icone da carga com as rachaduras, relogio do ovo, anel de recarga do dash, camadas (pontos +
    /// bandeira da entrega) e UMA linha de dica (Run.Hint -> Hints.Text). Fora do contrato: quadro dos 8 contratos, oferta de resgate,
    /// guilda, pausa (com desistencia confirmada na propria UI), cena da entrega (3,6 s, pulavel) e resultado com o pagamento detalhado.
    /// Cores: docs/ART_BIBLE.md §3/§10 (cartoes de papel com borda de tinta). Textos: Textos.cs.
    /// </summary>
    public sealed class Hud
    {
        /// <summary>Fracao de cima da area segura reservada a HUD: a camera e o joystick nao usam essa faixa.</summary>
        public const float Band = 0.15f;
        const float CutTime = 3.6f;
        // caixa da entrega com o sprite: centro (o da caixa procedural), lado da celula em px de referencia e centro da tampa (fracao da
        // celula acima do centro). O topo da silhueta, medido no PNG, fica a 0,375 (linha 14 de 112); a tampa de 26 px assenta nele
        static readonly Vector2 CratePos = new Vector2(0.5f, 0.395f);
        const float CrateCell = 450f, CrateTop = 0.36f;
        public enum Tela { Board, Play, Cutscene, Result }

        readonly Game _g;
        Sim S => _g.Sim;
        public Tela Current { get; private set; }
        public RectTransform Labels { get; private set; }
        /// <summary>Base da faixa da HUD em px de tela (o mundo e o joystick ficam abaixo).</summary>
        public float TopPx { get; private set; }
        public bool PauseOpen => _pause.gameObject.activeSelf;
        /// <summary>Segundos na tela atual (o -bot avanca sozinho por ele).</summary>
        public float TimeInScreen { get; private set; }

        readonly RectTransform _safe, _top, _board, _guild, _offer, _pause, _cut, _result;
        readonly Image _vignette, _fade;
        float _vigT, _fadeT;

        // faixa de cima
        readonly Image[] _hearts = new Image[Balance.BaseHp + Balance.LifePerLevel * Balance.UpgradeMax];
        readonly Image[] _pips = new Image[3];
        readonly Image[] _layers = new Image[6];
        Image _cargoIcon, _cargoEdge, _rescueIcon, _clock, _dashFill, _dashIcon;
        Text _coins, _hint, _egg;
        int _hp = -1, _max = -1, _coinShown = -1, _eggShown = -1; string _hintShown; float _heartPop; int _heartPopI = -1;

        // quadro
        sealed class Row { public Button B; public CanvasGroup G; public Image Badge, Lock; public Text BadgeText; }
        readonly Row[] _rows = new Row[8];
        Text _boardCoins, _notice;
        Button _guildBtn;
        readonly List<KeyValuePair<Button, bool>> _toggles = new List<KeyValuePair<Button, bool>>();   // (botao, e vibracao)

        // guilda
        sealed class UpRow { public Text Level, Cost; public Image[] Dots = new Image[Balance.UpgradeMax]; public Button Buy; public Text BuyText; public Image CostCoin; }
        readonly UpRow[] _ups = new UpRow[3];
        Text _guildCoins;

        // oferta, pausa, cena, resultado
        int _offerIndex;
        Text _offerBody, _pauseTitle, _pauseBody;
        Image _offerIcon;
        RectTransform _pauseMain, _pauseConfirm;
        ContractResult _res;
        float _cutT, _cutItemK = 1f;
        Vector2 _cutLidAt;
        Image _cutMouth, _cutLid, _cutItem, _cutItemCracks, _cutClient, _cutBox, _cutStrap, _cutCrate;
        GameObject _cutProc;
        SpriteSheet _cutSheet;
        readonly Image[] _sparks = new Image[5];
        Text _cutText;
        Text _resTitle, _resSub, _resNote;
        readonly Text[] _resLabel = new Text[9], _resValue = new Text[9];

        public Hud(Game g)
        {
            _g = g;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var go = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080f, 1920f);
            sc.matchWidthOrHeight = 0f;   // retrato: casa a largura
            var canvas = (RectTransform)go.transform;
            Labels = Art.Node(canvas, "Rotulos", Vector2.zero, Vector2.one);   // rotulos do mundo ficam ABAIXO da HUD e das telas
            _fade = Art.Img(canvas, "Escurecer", Art.Square(), new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one);
            _fade.preserveAspect = false;
            _vignette = Art.Img(canvas, "VinhetaDano", Art.Vignette(), Art.A(Art.Bad, 0f), Vector2.zero, Vector2.one);
            _vignette.preserveAspect = false;
            _safe = Art.Node(canvas, "AreaSegura", Vector2.zero, Vector2.one);

            _top = BuildTop();
            _board = BuildBoard();
            _guild = BuildGuild();
            _offer = BuildOffer();
            _pause = BuildPause();
            _cut = BuildCutscene();
            _result = BuildResult();
            Fit();
            ShowBoard();
        }

        public void Fit()
        {
            AreaSegura.AncorarDentro(_safe);
            Rect sa = Screen.safeArea;
            TopPx = sa.yMax - Band * sa.height;
        }

        // ------------------------------------------------------------------ telas

        void Only(RectTransform screen)
        {
            foreach (RectTransform s in new[] { _top, _board, _cut, _result }) s.gameObject.SetActive(s == screen);
            _guild.gameObject.SetActive(false); _offer.gameObject.SetActive(false); _pause.gameObject.SetActive(false);
            TimeInScreen = 0f;
        }

        public void ShowBoard() { Current = Tela.Board; Only(_board); RefreshBoard(); }

        public void ShowPlay()
        {
            Current = Tela.Play; Only(_top);
            _hp = _max = _coinShown = _eggShown = -1; _hintShown = null;
        }

        /// <summary>Pausa (botao, voltar, app em segundo plano). resumed = contrato reaberto do save.</summary>
        public void ShowPause(bool resumed)
        {
            if (Current != Tela.Play) return;
            Run r = S.Run;
            _pauseTitle.text = resumed ? Textos.Retomado : Textos.Pausado;
            _pauseBody.text = r.Contract.Code + " · " + Textos.Carga(r.Contract.Cargo) + " · " + Textos.Tempo(r.Time);
            _pauseMain.gameObject.SetActive(true); _pauseConfirm.gameObject.SetActive(false);
            RefreshToggles();
            _pause.gameObject.SetActive(true);
        }

        /// <summary>Fim do contrato: entregue = cena do cliente abrindo a caixa e depois o resultado; senao, o resultado direto.</summary>
        public void ShowEnd(ContractResult r)
        {
            _res = r;
            if (r == null) { ShowBoard(); return; }
            if (!r.Delivered) { ShowResult(); return; }
            Current = Tela.Cutscene; Only(_cut);
            _cutT = 0f;
            CargoType t = S.Run != null ? S.Run.Contract.Cargo : CargoType.Box;
            // item que sobe: sprite da reliquia/ovo/filhote quando a folha existe (a caixa de dentro da caixa fica procedural)
            SpriteSheet item = r.Hatched ? SpriteSheet.Get("filhote") : t != CargoType.Box ? SpriteSheet.Get(WorldView.CargoSheet[(int)t]) : null;
            _cutItem.sprite = item != null ? item.Frame(item.Pick("Static"), 0, 0) : r.Hatched ? Art.Disc() : Art.CargoSprite(t);
            _cutItem.color = item != null ? Color.white : r.Hatched ? Art.Pet : Art.CargoColor(t);
            _cutItemK = item != null ? 1.6f : 1f;   // a celula do sprite tem margem: o objeto ocupa ~60% dela
            // caixa da entrega: o sprite `caixa` (carga Caixa) no lugar do retangulo com cinta; a tampa procedural fica no topo dele
            SpriteSheet crate = t == CargoType.Box ? SpriteSheet.Get("caixa") : null;
            _cutBox.enabled = _cutStrap.enabled = crate == null;
            _cutCrate.enabled = crate != null;
            RectTransform lid = _cutLid.rectTransform;
            if (crate != null)
            {
                _cutCrate.sprite = crate.Frame(crate.Pick("Static"), 0, 0);
                lid.anchorMin = lid.anchorMax = CratePos;
                lid.sizeDelta = new Vector2(0.6f * CrateCell, 26f);
                _cutLidAt = new Vector2(0f, CrateTop * CrateCell);
            }
            else
            {
                lid.anchorMin = new Vector2(0.36f, 0.455f); lid.anchorMax = new Vector2(0.64f, 0.48f);
                lid.sizeDelta = Vector2.zero;
                _cutLidAt = Vector2.zero;
            }
            _cutItemCracks.enabled = !r.Hatched && r.Cracks > 0;
            if (r.Cracks > 0) _cutItemCracks.sprite = Art.Cracks(r.Cracks);
            _cutSheet = SpriteSheet.Get("cliente");
            _cutProc.SetActive(_cutSheet == null);
            _cutClient.gameObject.SetActive(_cutSheet != null);
            _cutText.text = "";
            TickCut(0f);
        }

        void ShowResult()
        {
            Current = Tela.Result; Only(_result);
            ContractResult r = _res;
            ContractDef c = Sim.Contracts[r.Contract];
            _resTitle.text = c.Code + ": " + Textos.Titulo(r.Outcome);
            _resTitle.color = r.Delivered ? Art.Good : Art.Bad;
            _resSub.text = r.Delivered ? Textos.Reacao(r) : Textos.Motivo(r);
            string[][] lines = Textos.Pagamento(r, c);
            for (int i = 0; i < _resLabel.Length; i++)
            {
                bool on = i < lines.Length;
                _resLabel[i].gameObject.SetActive(on); _resValue[i].gameObject.SetActive(on);
                if (!on) continue;
                _resLabel[i].text = lines[i][0]; _resValue[i].text = lines[i][1];
                bool strong = lines[i][0] == "Pagamento" || lines[i][0] == "Total";
                _resLabel[i].fontStyle = _resValue[i].fontStyle = strong ? FontStyle.Bold : FontStyle.Normal;
            }
            bool pending = !r.Delivered && S.Pending != null && S.Pending.Contract == r.Contract;
            _resNote.text = pending ? Textos.AvisoPendente : "";
        }

        public void ShowOffer(int i)
        {
            LostCargo p = S.Pending;
            if (p == null) { _g.StartContract(i, false); return; }
            _offerIndex = i;
            int bonus = (int)Math.Round(p.Value * Balance.RescueBonus * S.CoinMul, MidpointRounding.AwayFromZero);
            _offerBody.text = Textos.Oferta(p, bonus);
            _offerIcon.sprite = Art.CargoSprite(p.Type); _offerIcon.color = Art.CargoColor(p.Type);
            _offer.gameObject.SetActive(true);
        }

        void ShowGuild() { RefreshGuild(); _guild.gameObject.SetActive(true); }

        /// <summary>Voltar (Esc / botao do Android): fecha o que estiver por cima. false = nada para fechar (o Game decide: pausa ou sair).</summary>
        public bool Back()
        {
            if (Current == Tela.Cutscene) { ShowResult(); return true; }
            if (_guild.gameObject.activeSelf) { _guild.gameObject.SetActive(false); return true; }
            if (_offer.gameObject.activeSelf) { _offer.gameObject.SetActive(false); return true; }
            if (_pause.gameObject.activeSelf)
            {
                if (_pauseConfirm.gameObject.activeSelf) { _pauseConfirm.gameObject.SetActive(false); _pauseMain.gameObject.SetActive(true); }
                else _pause.gameObject.SetActive(false);
                return true;
            }
            if (Current == Tela.Result) { _g.ToBoard(); return true; }
            return false;
        }

        public void Hurt() => _vigT = 0.4f;
        public void FadeIn() => _fadeT = 0.3f;

        // ------------------------------------------------------------------ quadro a quadro

        public void Refresh(float dt)
        {
            TimeInScreen += dt;
            _vigT = Mathf.Max(0f, _vigT - dt);
            _vignette.color = Art.A(Art.Bad, 0.25f * 2.5f * _vigT);   // vinheta de dano a 25% (ART_BIBLE §9)
            _fadeT = Mathf.Max(0f, _fadeT - dt);
            _fade.color = new Color(0f, 0f, 0f, _fadeT / 0.3f);
            if (Current == Tela.Play && S.Run != null) RefreshTop(S.Run, dt);
            else if (Current == Tela.Cutscene) TickCut(dt);
            else if (Current == Tela.Board && _guildBtn.gameObject.activeSelf)
            {
                bool any = S.CanBuy(Upgrade.Life) || S.CanBuy(Upgrade.Dash) || S.CanBuy(Upgrade.Bag);
                _guildBtn.transform.localScale = Vector3.one * (any ? 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 6f) : 1f);
            }
        }

        void RefreshTop(Run r, float dt)
        {
            Courier c = r.Courier;
            if (c.Hp != _hp || c.MaxHp != _max)
            {
                if (_hp > c.Hp && _max == c.MaxHp) { _heartPop = 0.25f; _heartPopI = c.Hp; }   // o coracao perdido estoura
                _hp = c.Hp; _max = c.MaxHp;
                for (int i = 0; i < _hearts.Length; i++)
                {
                    _hearts[i].gameObject.SetActive(i < _max);
                    _hearts[i].color = i < _hp ? Art.Bad : Art.A(Art.Off, 0.9f);
                }
            }
            if (_heartPopI >= 0 && _heartPopI < _hearts.Length)
            {
                _heartPop = Mathf.Max(0f, _heartPop - dt);
                _hearts[_heartPopI].transform.localScale = Vector3.one * (1f + 1.6f * _heartPop);
                if (_heartPop <= 0f) _heartPopI = -1;
            }
            if (S.Coins != _coinShown) { _coinShown = S.Coins; _coins.text = Textos.Num(_coinShown); }

            // carga do contrato: icone com contorno limao (roxo se roubada), 3 encaixes de rachadura; chocado = filhote com a vida
            Cargo m = r.Main;
            if (r.Hatched)
            {
                _cargoIcon.sprite = Art.Disc(); _cargoIcon.color = Art.Pet; _cargoEdge.color = Art.A(Art.Cargo, 0f);
                for (int i = 0; i < _pips.Length; i++)
                {
                    _pips[i].enabled = i < Balance.PetHp;
                    _pips[i].sprite = Art.Heart();
                    _pips[i].color = r.Pet != null && i < r.Pet.Hp ? Art.Pet : Art.A(Art.Off, 0.9f);
                }
            }
            else
            {
                _cargoIcon.sprite = Art.CargoSprite(m.Type); _cargoIcon.color = Art.CargoColor(m.Type);
                _cargoEdge.sprite = _cargoIcon.sprite;
                bool far = m.State == CargoState.Ground && V2.Dist(m.Pos, c.Pos) > Balance.AloneDist;
                _cargoEdge.color = m.State == CargoState.Stolen ? Art.Role(EnemyType.Thief)
                    : far && Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.5f ? Art.A(Art.Cargo, 0.2f) : Art.Cargo;
                for (int i = 0; i < _pips.Length; i++)
                {
                    _pips[i].enabled = true;
                    _pips[i].sprite = Art.Cracks(i + 1);
                    _pips[i].color = i < m.Cracks ? Art.Bad : Art.A(Art.Off, 0.9f);
                }
            }
            Cargo rc = r.RescueCargo;
            bool rescue = rc != null && rc.State != CargoState.Gone;
            if (_rescueIcon.gameObject.activeSelf != rescue) _rescueIcon.gameObject.SetActive(rescue);
            if (rescue) { _rescueIcon.sprite = Art.CargoSprite(rc.Type); _rescueIcon.color = Art.CargoColor(rc.Type); }

            bool egg = r.Contract.EggTarget > 0f && !r.Hatched;
            if (_clock.gameObject.activeSelf != egg) { _clock.gameObject.SetActive(egg); _egg.gameObject.SetActive(egg); }
            if (egg)
            {
                int left = Mathf.CeilToInt(r.EggLeft);
                if (left != _eggShown) { _eggShown = left; _egg.text = Textos.Segundos(r.EggLeft); }
                _egg.color = _clock.color = left <= 10 ? Art.Bad : Art.Ink;
            }

            // anel de recarga do dash (azul: so courier, projetil e dash)
            bool ready = c.DashReady;
            _dashFill.fillAmount = c.DashCdMax > 0f ? 1f - c.DashCd / c.DashCdMax : 1f;
            _dashFill.color = ready ? Art.Shot : Art.A(Art.Courier, 0.8f);
            _dashIcon.color = ready ? Art.Ink : Art.A(Art.Ink, 0.35f);

            // camadas: feitas = verde, atual = clara e maior, futuras = apagadas; a ultima e a bandeira da entrega
            int n = Mathf.Min(r.LayerCount, _layers.Length);
            for (int i = 0; i < _layers.Length; i++)
            {
                Image l = _layers[i];
                bool on = i < n;
                if (l.gameObject.activeSelf != on) l.gameObject.SetActive(on);
                if (!on) continue;
                l.sprite = i == n - 1 ? Art.Flag() : Art.Disc();
                l.color = i < r.Layer ? Art.Good : i == r.Layer ? Art.Ink : Art.A(Art.Off, 0.9f);
                float sc = i == r.Layer ? 1f : 0.7f;
                if (l.transform.localScale.x != sc) l.transform.localScale = Vector3.one * sc;
            }

            string h = r.Hint != Hint.None ? Hints.Text(r.Hint) : Textos.Objetivo(r);
            if (!ReferenceEquals(h, _hintShown)) { _hintShown = h; _hint.text = h; _hint.color = r.Hint != Hint.None ? Art.Ink : Art.A(Art.Ink, 0.6f); }
        }

        void TickCut(float dt)
        {
            _cutT += dt;
            float t = _cutT;
            float lid = Mathf.Clamp01((t - 0.8f) / 0.5f), item = Mathf.Clamp01((t - 1.3f) / 0.6f);
            _cutLid.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 35f * lid);
            _cutLid.rectTransform.anchoredPosition = _cutLidAt + new Vector2(30f * lid, 70f * lid);
            _cutItem.rectTransform.anchoredPosition = new Vector2(0f, 150f * item);
            _cutItem.rectTransform.localScale = Vector3.one * (item * _cutItemK);
            bool react = t >= 2f;
            bool happy = _res.Hatched || _res.Cracks <= 1;
            _cutMouth.sprite = !react ? Art.Flat() : happy ? Art.Smile() : _res.Cracks >= 3 ? Art.Frown() : Art.Flat();
            if (_cutSheet != null)
            {
                // mesma reacao do mundo (WorldView): 0 rachadura comemora, 1 ou ovo chocado agradece, 2+ da de ombros
                string reacao = _res.Hatched || _res.Cracks == 1 ? "Thankful" : _res.Cracks == 0 ? "Cheering" : "Shrug";
                int clip = _cutSheet.Pick(react ? reacao : "Idle");
                _cutClient.sprite = _cutSheet.Frame(clip, 0, _cutSheet.FrameAt(clip, react ? t - 2f : t, true));
            }
            if (react && _cutText.text.Length == 0) _cutText.text = Textos.Reacao(_res);
            bool perfect = react && !_res.Hatched && _res.Cracks == 0;
            for (int i = 0; i < _sparks.Length; i++)
            {
                _sparks[i].enabled = perfect;
                if (perfect) _sparks[i].rectTransform.localScale = Vector3.one * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * 5f + i)));
            }
            if (t >= CutTime) ShowResult();
        }

        // ------------------------------------------------------------------ quadro, guilda, ajustes

        public void RefreshBoard()
        {
            _boardCoins.text = Textos.Num(S.Coins);
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
            }
            _guildBtn.gameObject.SetActive(S.GuildOpen && !S.SaveRefused);
            RefreshToggles();
            if (_guild.gameObject.activeSelf) RefreshGuild();
        }

        void RefreshGuild()
        {
            _guildCoins.text = Textos.Num(S.Coins);
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
        }

        void RefreshToggles()
        {
            foreach (KeyValuePair<Button, bool> t in _toggles)
            {
                bool vib = t.Value;
                t.Key.interactable = !vib || Ajustes.TemVibra;
                t.Key.GetComponentInChildren<Text>().text = vib ? Textos.Vibra(Ajustes.Vibra) : Textos.Som(Ajustes.Som);
            }
        }

        // ------------------------------------------------------------------ construcao

        RectTransform BuildTop()
        {
            RectTransform top = Art.Node(_safe, "Topo", new Vector2(0f, 1f - Band), Vector2.one);
            Image bg = top.gameObject.AddComponent<Image>(); bg.color = Art.A(Art.Tinta, 0.85f); bg.raycastTarget = false;

            // linha A: coracoes | moedas | pausa
            RectTransform hearts = Art.Node(top, "Coracoes", new Vector2(0.02f, 0.55f), new Vector2(0.58f, 0.97f));
            for (int i = 0; i < _hearts.Length; i++)
                _hearts[i] = Art.Img(hearts, "Coracao", Art.Heart(), Art.Bad, new Vector2((float)i / _hearts.Length, 0.08f), new Vector2((i + 1f) / _hearts.Length, 0.92f));
            Art.Img(top, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.6f, 0.6f), new Vector2(0.67f, 0.92f));
            _coins = Art.Outlined(Art.NewText(top, "Moedas", 52, new Vector2(0.68f, 0.55f), new Vector2(0.85f, 0.97f), TextAnchor.MiddleLeft));
            _coins.fontStyle = FontStyle.Bold;
            Image pb = Art.Panel(top, "Pausa", Art.Paper, new Vector2(0.865f, 0.56f), new Vector2(0.98f, 0.97f));
            Button pause = pb.gameObject.AddComponent<Button>();
            pause.targetGraphic = pb;
            pause.onClick.AddListener(() => { Sfx.Play("clique"); ShowPause(false); });
            Art.Img(pb.transform, "Icone", Art.Pause(), Art.Tinta, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));

            // linha B: carga + rachaduras | resgate | relogio do ovo | dash | camadas
            _cargoEdge = Art.Img(top, "CargaContorno", Art.Rounded(), Art.Cargo, new Vector2(0.015f, 0.27f), new Vector2(0.095f, 0.54f));
            _cargoIcon = Art.Img(top, "Carga", Art.Rounded(), Color.white, new Vector2(0.025f, 0.3f), new Vector2(0.085f, 0.51f));
            for (int i = 0; i < _pips.Length; i++)
            {
                Image bgPip = Art.Img(top, "Encaixe", Art.Disc(), Art.A(Art.Ink, 0.9f), new Vector2(0.1f + i * 0.05f, 0.31f), new Vector2(0.145f + i * 0.05f, 0.5f));
                _pips[i] = Art.Img(bgPip.transform, "Rachadura", Art.Cracks(i + 1), Art.Off, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            }
            _rescueIcon = Art.Img(top, "Resgate", Art.Rounded(), Color.white, new Vector2(0.26f, 0.31f), new Vector2(0.31f, 0.5f));
            Image seal = Art.Img(_rescueIcon.transform, "SeloFundo", Art.Disc(), Art.Tinta, new Vector2(0.5f, 0.5f), new Vector2(1.1f, 1.1f));
            Art.Img(seal.transform, "Selo", Art.Star(), Art.Cream, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
            _rescueIcon.gameObject.SetActive(false);
            _clock = Art.Img(top, "Relogio", Art.Clock(), Art.Ink, new Vector2(0.33f, 0.3f), new Vector2(0.38f, 0.51f));
            _egg = Art.Outlined(Art.NewText(top, "Ovo", 38, new Vector2(0.385f, 0.27f), new Vector2(0.52f, 0.54f), TextAnchor.MiddleLeft));
            _egg.fontStyle = FontStyle.Bold;
            Art.Img(top, "DashFundo", Art.Ring(), Art.A(Art.Off, 0.9f), new Vector2(0.53f, 0.27f), new Vector2(0.61f, 0.54f));
            _dashFill = Art.Img(top, "Dash", Art.Ring(), Art.Shot, new Vector2(0.53f, 0.27f), new Vector2(0.61f, 0.54f));
            _dashFill.type = Image.Type.Filled; _dashFill.fillMethod = Image.FillMethod.Radial360;
            _dashFill.fillOrigin = (int)Image.Origin360.Top; _dashFill.fillClockwise = true;
            _dashIcon = Art.Img(top, "DashIcone", Art.Chevrons(), Art.Ink, new Vector2(0.55f, 0.33f), new Vector2(0.59f, 0.48f));
            for (int i = 0; i < _layers.Length; i++)
                _layers[i] = Art.Img(top, "Camada", Art.Disc(), Art.Off, new Vector2(0.64f + i * 0.058f, 0.3f), new Vector2(0.69f + i * 0.058f, 0.51f));

            // linha C: a dica (1 linha)
            _hint = Art.Outlined(Art.NewText(top, "Dica", 36, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.26f)));
            _hint.fontStyle = FontStyle.Bold;
            _hint.resizeTextForBestFit = true; _hint.resizeTextMinSize = 20; _hint.resizeTextMaxSize = 36;
            _hint.horizontalOverflow = HorizontalWrapMode.Wrap; _hint.verticalOverflow = VerticalWrapMode.Truncate;
            return top;
        }

        RectTransform BuildBoard()
        {
            RectTransform b = Art.Node(_safe, "Quadro", Vector2.zero, Vector2.one);
            Image bg = b.gameObject.AddComponent<Image>(); bg.color = Art.Bg;
            Text title = Art.Outlined(Art.NewText(b, "Titulo", 60, new Vector2(0.03f, 0.93f), new Vector2(0.97f, 0.99f)));
            title.text = Textos.Quadro; title.fontStyle = FontStyle.Bold;
            Art.Img(b, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.03f, 0.877f), new Vector2(0.09f, 0.923f));
            _boardCoins = Art.Outlined(Art.NewText(b, "Moedas", 46, new Vector2(0.1f, 0.87f), new Vector2(0.38f, 0.93f), TextAnchor.MiddleLeft));
            _boardCoins.fontStyle = FontStyle.Bold;
            Toggle(b, false, new Vector2(0.4f, 0.872f), new Vector2(0.68f, 0.926f));
            Toggle(b, true, new Vector2(0.7f, 0.872f), new Vector2(0.97f, 0.926f));
            _notice = Art.NewText(b, "Aviso", 28, new Vector2(0.03f, 0.81f), new Vector2(0.97f, 0.866f));
            _notice.fontStyle = FontStyle.Bold;

            const float rowH = 0.083f, top = 0.805f;
            for (int i = 0; i < _rows.Length; i++)
            {
                float y1 = top - i * rowH, y0 = y1 - rowH + 0.006f;
                _rows[i] = BuildRow(b, i, new Vector2(0.03f, y0), new Vector2(0.97f, y1));
            }
            _guildBtn = Art.NewButton(b, Textos.Guilda, 46, Art.Paper, new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.1f), ShowGuild);
            return b;
        }

        Row BuildRow(Transform parent, int i, Vector2 min, Vector2 max)
        {
            ContractDef c = Sim.Contracts[i];
            Image face = Card(parent, "Contrato " + c.Code, min, max);
            var row = new Row { G = face.transform.parent.parent.gameObject.AddComponent<CanvasGroup>() };
            row.B = face.gameObject.AddComponent<Button>();
            row.B.targetGraphic = face;
            int k = i;
            row.B.onClick.AddListener(() => { Sfx.Play("clique"); _g.Play(k); });
            Text code = Art.NewText(face.transform, "Codigo", 46, new Vector2(0.01f, 0f), new Vector2(0.12f, 1f));
            code.text = c.Code; code.color = Art.Tinta; code.fontStyle = FontStyle.Bold;
            Art.Img(face.transform, "CargaContorno", Art.CargoSprite(c.Cargo), Art.Cargo, new Vector2(0.12f, 0.12f), new Vector2(0.22f, 0.88f));
            Art.Img(face.transform, "Carga", Art.CargoSprite(c.Cargo), Art.CargoColor(c.Cargo), new Vector2(0.13f, 0.2f), new Vector2(0.21f, 0.8f));
            Text l1 = Art.NewText(face.transform, "Linha1", 34, new Vector2(0.235f, 0.5f), new Vector2(0.7f, 0.96f), TextAnchor.MiddleLeft);
            l1.text = Textos.Carga(c.Cargo) + " · " + Textos.Salas(c); l1.color = Art.Tinta; l1.fontStyle = FontStyle.Bold;
            l1.resizeTextForBestFit = true; l1.resizeTextMinSize = 20; l1.resizeTextMaxSize = 34;
            Text l2 = Art.NewText(face.transform, "Linha2", 30, new Vector2(0.235f, 0.04f), new Vector2(0.7f, 0.5f), TextAnchor.MiddleLeft);
            l2.text = Textos.Verbo(c); l2.color = Art.A(Art.Tinta, 0.75f);
            l2.resizeTextForBestFit = true; l2.resizeTextMinSize = 18; l2.resizeTextMaxSize = 30;
            Art.Img(face.transform, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.705f, 0.25f), new Vector2(0.76f, 0.75f));
            Text pay = Art.NewText(face.transform, "Pagamento", 36, new Vector2(0.765f, 0f), new Vector2(0.86f, 1f), TextAnchor.MiddleLeft);
            pay.text = Textos.Num(c.Pay); pay.color = Art.Tinta; pay.fontStyle = FontStyle.Bold;
            row.Badge = Art.Panel(face.transform, "Selo", Art.Good, new Vector2(0.86f, 0.16f), new Vector2(0.99f, 0.84f));
            row.Badge.raycastTarget = false;
            row.BadgeText = Art.NewText(row.Badge.transform, "Rotulo", 26, Vector2.zero, Vector2.one);
            row.BadgeText.color = Art.Tinta; row.BadgeText.fontStyle = FontStyle.Bold;
            row.BadgeText.resizeTextForBestFit = true; row.BadgeText.resizeTextMinSize = 14; row.BadgeText.resizeTextMaxSize = 26;
            row.Lock = Art.Img(row.Badge.transform, "Cadeado", Art.Lock(), Art.Ink, new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.85f));
            return row;
        }

        RectTransform BuildGuild()
        {
            RectTransform m = Modal("Guilda");
            Image face = Card(m, "Cartao", new Vector2(0.04f, 0.17f), new Vector2(0.96f, 0.83f));
            Text title = Art.NewText(face.transform, "Titulo", 56, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
            title.text = Textos.Guilda; title.color = Art.Tinta; title.fontStyle = FontStyle.Bold;
            Art.Img(face.transform, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.38f, 0.815f), new Vector2(0.45f, 0.875f));
            _guildCoins = Art.NewText(face.transform, "Moedas", 42, new Vector2(0.46f, 0.81f), new Vector2(0.75f, 0.88f), TextAnchor.MiddleLeft);
            _guildCoins.color = Art.Tinta; _guildCoins.fontStyle = FontStyle.Bold;
            Sprite[] icons = { Art.Heart(), Art.Chevrons(), Art.Bag() };
            Color[] tints = { Art.Bad, Art.Courier, Art.Coin };
            for (int u = 0; u < _ups.Length; u++)
            {
                float y1 = 0.79f - u * 0.235f, y0 = y1 - 0.22f;
                RectTransform r = Art.Node(face.transform, "Linha", new Vector2(0.03f, y0), new Vector2(0.97f, y1));
                Image sep = Art.Panel(r, "Fundo", Art.A(Art.PaperEdge, 0.35f), Vector2.zero, Vector2.one); sep.raycastTarget = false;
                Art.Img(r, "Icone", icons[u], tints[u], new Vector2(0.02f, 0.2f), new Vector2(0.15f, 0.8f));
                var row = new UpRow();
                Text name = Art.NewText(r, "Nome", 38, new Vector2(0.17f, 0.62f), new Vector2(0.6f, 0.95f), TextAnchor.MiddleLeft);
                name.text = Textos.UpNome((Upgrade)u); name.color = Art.Tinta; name.fontStyle = FontStyle.Bold;
                Text eff = Art.NewText(r, "Efeito", 26, new Vector2(0.17f, 0.36f), new Vector2(0.62f, 0.62f), TextAnchor.MiddleLeft);
                eff.text = Textos.UpEfeito((Upgrade)u); eff.color = Art.A(Art.Tinta, 0.8f);
                eff.resizeTextForBestFit = true; eff.resizeTextMinSize = 16; eff.resizeTextMaxSize = 26;
                for (int k = 0; k < row.Dots.Length; k++)
                    row.Dots[k] = Art.Img(r, "Nivel", Art.Disc(), Art.Off, new Vector2(0.17f + k * 0.045f, 0.08f), new Vector2(0.205f + k * 0.045f, 0.3f));
                row.Level = Art.NewText(r, "NivelTexto", 24, new Vector2(0.42f, 0.04f), new Vector2(0.62f, 0.34f), TextAnchor.MiddleLeft);
                row.Level.color = Art.A(Art.Tinta, 0.7f);
                row.CostCoin = Art.Img(r, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.6f, 0.3f), new Vector2(0.66f, 0.7f));
                row.Cost = Art.NewText(r, "Custo", 36, new Vector2(0.665f, 0f), new Vector2(0.77f, 1f), TextAnchor.MiddleLeft);
                row.Cost.color = Art.Tinta; row.Cost.fontStyle = FontStyle.Bold;
                int k2 = u;
                row.Buy = Art.NewButton(r, Textos.Comprar, 30, Art.Good, new Vector2(0.77f, 0.18f), new Vector2(0.99f, 0.82f), () => { _g.Buy((Upgrade)k2); RefreshGuild(); });
                row.BuyText = row.Buy.GetComponentInChildren<Text>();
                _ups[u] = row;
            }
            Art.NewButton(face.transform, Textos.Voltar, 38, Art.PaperEdge, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.09f), () => _guild.gameObject.SetActive(false));
            return m;
        }

        RectTransform BuildOffer()
        {
            RectTransform m = Modal("Oferta");
            Image face = Card(m, "Cartao", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.72f));
            Text title = Art.NewText(face.transform, "Titulo", 52, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.97f));
            title.text = Textos.OfertaTitulo; title.color = Art.Tinta; title.fontStyle = FontStyle.Bold;
            Art.Img(face.transform, "Contorno", Art.Rounded(), Art.Cargo, new Vector2(0.4f, 0.6f), new Vector2(0.6f, 0.83f));
            _offerIcon = Art.Img(face.transform, "Carga", Art.Rounded(), Color.white, new Vector2(0.42f, 0.62f), new Vector2(0.58f, 0.81f));
            Image seal = Art.Img(_offerIcon.transform, "SeloFundo", Art.Disc(), Art.Tinta, new Vector2(0.6f, 0.6f), new Vector2(1.1f, 1.1f));
            Art.Img(seal.transform, "Selo", Art.Star(), Art.Cream, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
            _offerBody = Art.NewText(face.transform, "Corpo", 30, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.59f));
            _offerBody.color = Art.Tinta;
            _offerBody.resizeTextForBestFit = true; _offerBody.resizeTextMinSize = 18; _offerBody.resizeTextMaxSize = 30;
            _offerBody.verticalOverflow = VerticalWrapMode.Truncate;
            Art.NewButton(face.transform, Textos.Resgatar, 38, Art.Good, new Vector2(0.05f, 0.04f), new Vector2(0.48f, 0.2f), () => _g.StartContract(_offerIndex, true));
            Art.NewButton(face.transform, Textos.Recusar, 38, Art.PaperEdge, new Vector2(0.52f, 0.04f), new Vector2(0.95f, 0.2f), () => _g.StartContract(_offerIndex, false));
            return m;
        }

        RectTransform BuildPause()
        {
            RectTransform m = Modal("Pausa");
            Image face = Card(m, "Cartao", new Vector2(0.07f, 0.25f), new Vector2(0.93f, 0.75f));
            _pauseTitle = Art.NewText(face.transform, "Titulo", 54, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.97f));
            _pauseTitle.color = Art.Tinta; _pauseTitle.fontStyle = FontStyle.Bold;
            _pauseBody = Art.NewText(face.transform, "Corpo", 30, new Vector2(0.06f, 0.66f), new Vector2(0.94f, 0.84f));
            _pauseBody.color = Art.Tinta;
            _pauseMain = Art.Node(face.transform, "Principal", Vector2.zero, Vector2.one);
            Art.NewButton(_pauseMain, Textos.Continuar, 44, Art.Good, new Vector2(0.1f, 0.46f), new Vector2(0.9f, 0.63f), () => _pause.gameObject.SetActive(false));
            Toggle(_pauseMain, false, new Vector2(0.05f, 0.3f), new Vector2(0.48f, 0.42f));
            Toggle(_pauseMain, true, new Vector2(0.52f, 0.3f), new Vector2(0.95f, 0.42f));
            Art.NewButton(_pauseMain, Textos.Desistir, 34, Art.Bad, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.2f), () =>
            {
                _pauseTitle.text = Textos.DesistirTitulo; _pauseBody.text = Textos.DesistirCorpo;
                _pauseMain.gameObject.SetActive(false); _pauseConfirm.gameObject.SetActive(true);
            });
            _pauseConfirm = Art.Node(face.transform, "Confirmar", Vector2.zero, Vector2.one);
            Art.NewButton(_pauseConfirm, Textos.DesistirSim, 38, Art.Bad, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.46f), () => { _pause.gameObject.SetActive(false); _g.Abandon(); });
            Art.NewButton(_pauseConfirm, Textos.Voltar, 38, Art.PaperEdge, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.2f), () => ShowPause(false));
            _pauseConfirm.gameObject.SetActive(false);
            return m;
        }

        /// <summary>Cena da entrega (GDD §3, 3-4 s, pulavel): o cliente (verde) abre a caixa, o item sobe e ele reage a integridade.</summary>
        RectTransform BuildCutscene()
        {
            RectTransform c = Art.Node(_safe, "Cena", Vector2.zero, Vector2.one);
            Image dim = c.gameObject.AddComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.72f);
            Button skip = c.gameObject.AddComponent<Button>();
            skip.targetGraphic = dim; skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(() => { if (Current == Tela.Cutscene) ShowResult(); });
            for (int i = 0; i < _sparks.Length; i++)
            {
                float a = i * Mathf.PI * 2f / _sparks.Length;
                Vector2 p = new Vector2(0.5f + 0.3f * Mathf.Cos(a), 0.64f + 0.17f * Mathf.Sin(a));
                _sparks[i] = Art.Img(c, "Brilho", Art.Star(), Art.Coin, p - new Vector2(0.04f, 0.025f), p + new Vector2(0.04f, 0.025f));
            }
            // cliente: sprite do Tripo (Resources/Sprites/cliente) quando existe; o desenho procedural fica de reserva
            _cutProc = Art.Node(c, "ClienteProcedural", Vector2.zero, Vector2.one).gameObject;
            Transform p0 = _cutProc.transform;
            Art.Img(p0, "Cliente", Art.Disc(), Art.Client, new Vector2(0.32f, 0.53f), new Vector2(0.68f, 0.75f));
            Art.Img(p0, "Chapeu", Art.Triangle(), Art.Gold, new Vector2(0.36f, 0.71f), new Vector2(0.64f, 0.8f));
            Art.Img(p0, "Olhos", Art.Eyes(), Art.Tinta, new Vector2(0.4f, 0.63f), new Vector2(0.6f, 0.68f));
            _cutMouth = Art.Img(p0, "Boca", Art.Flat(), Art.Tinta, new Vector2(0.43f, 0.565f), new Vector2(0.57f, 0.615f));
            _cutClient = Art.Img(c, "ClienteSprite", Art.Disc(), Color.white, new Vector2(0.22f, 0.44f), new Vector2(0.78f, 0.86f));
            _cutItem = Art.Img(c, "Item", Art.Rounded(), Color.white, new Vector2(0.43f, 0.4f), new Vector2(0.57f, 0.47f));
            _cutItemCracks = Art.Img(_cutItem.transform, "Rachaduras", Art.Cracks(1), Art.Crack, Vector2.zero, Vector2.one);
            _cutBox = Art.Img(c, "Caixa", Art.Rounded(), Art.CargoColor(CargoType.Box), new Vector2(0.37f, 0.33f), new Vector2(0.63f, 0.46f));
            _cutBox.preserveAspect = false;
            _cutStrap = Art.Img(c, "Cinta", Art.Square(), Art.BoxStrap, new Vector2(0.485f, 0.33f), new Vector2(0.515f, 0.46f));
            _cutStrap.preserveAspect = false;
            // sprite `caixa`: celula quadrada de CrateCell px de referencia no centro da caixa procedural (a silhueta ocupa 70 x 83 de 112 px
            // e fica centrada na celula: ~280 px de largura, como o retangulo)
            _cutCrate = Art.Img(c, "CaixaSprite", null, Color.white, CratePos, CratePos);
            _cutCrate.rectTransform.sizeDelta = Vector2.one * CrateCell;
            _cutCrate.enabled = false;
            _cutLid = Art.Img(c, "Tampa", Art.Square(), Art.Hex(0x8A5A32), new Vector2(0.36f, 0.455f), new Vector2(0.64f, 0.48f));
            _cutLid.preserveAspect = false;
            _cutText = Art.Outlined(Art.NewText(c, "Reacao", 50, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.29f)), 3f);
            _cutText.fontStyle = FontStyle.Bold;
            Text hint = Art.NewText(c, "Pular", 28, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.1f));
            hint.text = Textos.Pular; hint.color = Art.Dim;
            return c;
        }

        RectTransform BuildResult()
        {
            RectTransform m = Modal("Resultado");
            Image face = Card(m, "Cartao", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
            _resTitle = Art.NewText(face.transform, "Titulo", 58, new Vector2(0.04f, 0.9f), new Vector2(0.96f, 0.985f));
            _resTitle.fontStyle = FontStyle.Bold;
            _resTitle.resizeTextForBestFit = true; _resTitle.resizeTextMinSize = 30; _resTitle.resizeTextMaxSize = 58;
            _resSub = Art.NewText(face.transform, "Sub", 32, new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.9f));
            _resSub.color = Art.Tinta;
            for (int i = 0; i < _resLabel.Length; i++)
            {
                float y1 = 0.79f - i * 0.065f, y0 = y1 - 0.06f;
                _resLabel[i] = Art.NewText(face.transform, "Rotulo", 32, new Vector2(0.06f, y0), new Vector2(0.7f, y1), TextAnchor.MiddleLeft);
                _resLabel[i].color = Art.Tinta;
                _resValue[i] = Art.NewText(face.transform, "Valor", 34, new Vector2(0.7f, y0), new Vector2(0.94f, y1), TextAnchor.MiddleRight);
                _resValue[i].color = Art.Tinta;
            }
            _resNote = Art.NewText(face.transform, "Nota", 30, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.2f));
            _resNote.color = Art.Tinta; _resNote.fontStyle = FontStyle.Bold;
            Art.NewButton(face.transform, Textos.Continuar, 44, Art.Good, new Vector2(0.2f, 0.03f), new Vector2(0.8f, 0.13f), () => _g.ToBoard());
            return m;
        }

        /// <summary>Tela modal: escurece o resto e segura os toques. Comeca fechada.</summary>
        RectTransform Modal(string name)
        {
            RectTransform m = Art.Node(_safe, name, Vector2.zero, Vector2.one);
            m.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
            m.gameObject.SetActive(false);
            return m;
        }

        /// <summary>Cartao de papel (ART_BIBLE §10): borda de tinta, chanfro embaixo, face de papel. Devolve a face.</summary>
        static Image Card(Transform parent, string name, Vector2 min, Vector2 max)
        {
            Image edge = Art.Panel(parent, name, Art.Tinta, min, max);
            Image bevel = Art.Panel(edge.transform, "Chanfro", Art.PaperEdge, Vector2.zero, Vector2.one);
            bevel.rectTransform.offsetMin = new Vector2(4f, 4f); bevel.rectTransform.offsetMax = new Vector2(-4f, -4f);
            Image face = Art.Panel(bevel.transform, "Face", Art.Paper, Vector2.zero, Vector2.one);
            face.rectTransform.offsetMin = new Vector2(0f, 8f);
            bevel.raycastTarget = false;
            return face;
        }

        void Toggle(Transform parent, bool vibra, Vector2 min, Vector2 max)
        {
            Button b = Art.NewButton(parent, vibra ? Textos.Vibra(true) : Textos.Som(true), 28, Art.PaperEdge, min, max, () =>
            {
                if (vibra) { Ajustes.Vibra = !Ajustes.Vibra; Ajustes.Vibrar(0f); }
                else Ajustes.Som = !Ajustes.Som;
                _g.Log("settings", vibra ? "vibra" : "som", (vibra ? Ajustes.Vibra : Ajustes.Som) ? "1" : "0");
                RefreshToggles();
            });
            _toggles.Add(new KeyValuePair<Button, bool>(b, vibra));
        }
    }
}
