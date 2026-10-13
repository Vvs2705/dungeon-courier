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
    /// HUD e telas por codigo (uGUI, referencia 1080 x 1920 casando a LARGURA, dentro da area segura). Fluxo (CONTRATO_V1 §11):
    /// titulo -> menu (Jogar, Guilda, Configuracoes, Creditos) -> quadro de contratos (estrelas, cadeado, contrato do dia, codigo) ->
    /// contrato (faixa de cima, escolha de perk, pausa) -> cena da entrega -> resultado (estrelas, pagamento, codigo para compartilhar).
    /// Este arquivo: estrutura, faixa do contrato, oferta de resgate, pausa, cena e resultado. Menus.cs: titulo, menu, quadro, codigo,
    /// guilda/armas, perk, configuracoes e creditos.
    /// Textos: Textos.cs (pt-BR/EN/ES/ID; trocar o idioma chama Relocalizar). Cores: docs/ART_BIBLE.md §3/§10 (cartoes de papel com borda de
    /// tinta). Alvo de toque: botao com >= 144 px de referencia de altura (= 48 dp num aparelho de 360 dp de largura, porque o canvas
    /// casa a largura) ou area de toque estendida (Art.Toque). Estado nunca so por cor: cadeado, estrela vazada, visto, sublinhado.
    /// </summary>
    public sealed partial class Hud
    {
        /// <summary>Fracao de cima da area segura reservada a HUD: a camera e o joystick nao usam essa faixa.</summary>
        public const float Band = 0.15f;
        const float CutTime = 3.6f;
        // caixa da entrega com o sprite: centro (o da caixa procedural), lado da celula em px de referencia e centro da tampa (fracao da
        // celula acima do centro). O topo da silhueta, medido no PNG, fica a 0,375 (linha 14 de 112); a tampa de 26 px assenta nele
        static readonly Vector2 CratePos = new Vector2(0.5f, 0.395f);
        const float CrateCell = 450f, CrateTop = 0.36f;
        public enum Tela { Title, Menu, Board, Play, Cutscene, Result }

        readonly Game _g;
        Sim S => _g.Sim;
        public Tela Current { get; private set; }
        public RectTransform Labels { get; private set; }
        /// <summary>Base da faixa da HUD em px de tela (o mundo e o joystick ficam abaixo).</summary>
        public float TopPx { get; private set; }
        /// <summary>Tela por cima do contrato que segura o jogo (pausa, configuracoes, escolha de perk, anuncio, loja de teste): sem Tick e
        /// sem joystick.</summary>
        public bool Blocking => _pause.gameObject.activeSelf || _settings.gameObject.activeSelf || _perk.gameObject.activeSelf || _ads.Aberto || _lojaTeste.Aberto;
        /// <summary>Segundos na tela atual (o -bot avanca sozinho por ele).</summary>
        public float TimeInScreen { get; private set; }

        readonly RectTransform _safe, _top, _title, _menu, _board, _guild, _code, _offer, _perk, _pause, _settings, _credits, _cut, _result;
        readonly RectTransform[] _screens, _modals;
        /// <summary>Linhas do pagamento no resultado (Textos.Pagamento cabe nelas).</summary>
        const int ResLinhas = 14;
        readonly Image _vignette, _fade;
        float _vigT, _fadeT;

        // textos que dependem do idioma: reaplicados no Relocalizar
        readonly List<Text> _locT = new List<Text>();
        readonly List<Func<string>> _locF = new List<Func<string>>();

        // faixa de cima
        Image[] _hearts;
        readonly Image[] _pips = new Image[3];
        readonly Image[] _layers = new Image[10];
        Image _cargoIcon, _cargoEdge, _rescueIcon, _clock, _dashFill, _dashIcon, _iceBg, _iceFill;
        Text _coins, _hint, _egg, _dashN, _floorT, _stat, _faixa;
        int _statKey = int.MinValue; float _faixaT;
        const float FaixaTime = 2.4f;
        static readonly Color IceHi = Art.Hex(0xE6FAFF), Ice = Art.Hex(0xBFEFFF);
        int _hp = -1, _max = -1, _coinShown = -1, _eggShown = -1, _dashShown = -1, _layerN = -1, _fuseShown = -1, _floorShown = -1; string _hintShown; float _heartPop; int _heartPopI = -1;

        // oferta, pausa, cena, resultado
        Action<bool> _offerGo;
        Text _offerBody, _pauseTitle, _pauseBody;
        Image _offerIcon;
        RectTransform _pauseMain, _pauseConfirm;
        Button _pauseRoute;
        ContractResult _res;
        float _cutT, _cutItemK = 1f;
        Vector2 _cutLidAt;
        Image _cutMouth, _cutLid, _cutItem, _cutItemCracks, _cutClient, _cutBox, _cutStrap, _cutCrate;
        GameObject _cutProc;
        SpriteSheet _cutSheet;
        readonly Image[] _sparks = new Image[5];
        Text _cutText;
        Text _resTitle, _resSub, _resNote, _resCode, _resShared, _resOfferText;
        Button _resOffer;
        readonly Text[] _resLabel = new Text[ResLinhas], _resValue = new Text[ResLinhas];
        readonly Image[] _resStar = new Image[3];
        readonly bool[] _starGot = new bool[3];
        static readonly float[] StarPitch = { 1f, 1.12f, 1.26f };
        float _starT; int _starNext, _starSounds;
        readonly Text[] _resStarText = new Text[3];
        RectTransform _resShare;
        Button _resContinue, _resGuild;

        public Hud(Game g)
        {
            _g = g;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
            es.pixelDragThreshold = Mathf.RoundToInt(Dp.Px(8f));   // padrao de 10 px e nada numa tela densa: rolar a lista "comia" toques
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

            // ordem = camada: telas cheias primeiro, modais por cima (configuracoes por cima da pausa, oferta por cima do codigo)
            _top = BuildTop();
            _title = BuildTitle();
            _menu = BuildMenu();
            _board = BuildBoard();
            _cut = BuildCutscene();
            _result = BuildResult();
            _guild = BuildGuild();
            _code = BuildCode();
            BuildPaineis();   // mapa, eventos, temporada, marcos, roupas, sem fim, loja, modificadores (MetaTelas.cs)
            _offer = BuildOffer();
            _perk = BuildPerk();
            _pause = BuildPause();
            _settings = BuildSettings();
            _credits = BuildCredits();
            // por cima de tudo: a janela da loja de teste e o anuncio (ou o simulador dele)
            _lojaTeste = new LojaTeste(this); _loja = CriaLoja();   // Unity IAP (Loja.cs, LojaIap.cs)
            _ads = CriaAnuncios();                                   // LevelPlay com app key no Android; senao o simulador (Anuncios.cs)
            _screens = new[] { _top, _title, _menu, _board, _cut, _result };
            var modals = new List<RectTransform> { _guild, _code, _offer, _perk, _pause, _settings, _credits, _semFim };
            foreach (Painel p in _paineis) modals.Add(p.M);
            _modals = modals.ToArray();
            DevDesbloqueia();   // so -testsession -dev-desbloqueia (fotos)
            Fit();
            ShowTitle();
        }

        public void Fit()
        {
            AreaSegura.AncorarDentro(_safe);
            Rect sa = Screen.safeArea;
            TopPx = sa.yMax - Band * sa.height;
        }

        // ------------------------------------------------------------------ idioma

        Text Loc(Text t, Func<string> f)
        {
            t.text = f();
            _locT.Add(t); _locF.Add(f);
            return t;
        }

        /// <summary>Botao com rotulo que acompanha o idioma.</summary>
        Button Btn(Transform parent, Func<string> label, int size, Color color, Vector2 min, Vector2 max, Action onClick, string som = "clique")
        {
            Button b = Art.NewButton(parent, "", size, color, min, max, onClick, som);
            Loc(b.GetComponentInChildren<Text>(), label);
            return b;
        }

        /// <summary>Idioma trocado: rele todos os textos fixos e redesenha a tela aberta.</summary>
        public void Relocalizar()
        {
            for (int i = 0; i < _locT.Count; i++) _locT[i].text = _locF[i]();
            _hintShown = null; _eggShown = -1; _floorShown = -1; _statKey = int.MinValue;
            RefreshMenu(); RefreshBoard(); RefreshGuild(); RefreshSettings(); RefreshPaineis();
            if (_perk.gameObject.activeSelf && S.Run != null && S.Run.PerkOffer != null) ShowPerk(S.Run.PerkOffer);
            if (_pause.gameObject.activeSelf && _pauseMain.gameObject.activeSelf) PauseText(false);
            if (Current == Tela.Result) PintaResultado();
        }

        // ------------------------------------------------------------------ telas

        void Only(RectTransform screen)
        {
            for (int i = 0; i < _screens.Length; i++) _screens[i].gameObject.SetActive(_screens[i] == screen);
            for (int i = 0; i < _modals.Length; i++) _modals[i].gameObject.SetActive(false);
            TimeInScreen = 0f;
        }

        public void ShowTitle() { Current = Tela.Title; Only(_title); Audio.Music("menu"); }

        public void ShowPlay()
        {
            Current = Tela.Play; Only(_top);
            _hp = _max = _coinShown = _eggShown = _dashShown = _layerN = _floorShown = -1; _hintShown = null; _statKey = int.MinValue;
            _faixaT = 0f; _faixa.gameObject.SetActive(false);
        }

        /// <summary>Evento do contrato que a HUD mostra (o Game chama para cada evento): faixa da fase do chefe (BossPhase).</summary>
        public void EventoMundo(SimEvent e, Run run)
        {
            if (e.Kind != Ev.BossPhase || run == null) return;
            string s = null;
            foreach (Enemy x in run.Enemies) if (x.Id == e.A) s = Textos.FaseChefe(x.Type, e.B);
            if (s == null) return;
            _faixa.text = s; _faixaT = FaixaTime;
            _faixa.gameObject.SetActive(true);
        }

        /// <summary>Pausa (botao, voltar, app em segundo plano). resumed = contrato reaberto do save.</summary>
        public void ShowPause(bool resumed)
        {
            if (Current != Tela.Play || S.Run == null) return;
            PauseText(resumed);
            _pauseMain.gameObject.SetActive(true); _pauseConfirm.gameObject.SetActive(false);
            _pause.gameObject.SetActive(true);
        }

        void PauseText(bool resumed)
        {
            Run r = S.Run;
            if (r == null) return;
            _pauseTitle.text = resumed ? Textos.Retomado : Textos.Pausado;
            _pauseBody.text = Textos.PausaLinha(Textos.Rotulo(r.Contract), Textos.Carga(r.Contract.Cargo), Textos.Tempo(r.Time));
            if (r.SafeDoor >= 0 && r.SafeDoor < r.Doors.Count) _pauseBody.text += "\n" + Textos.RotaSegura(r.Doors[r.SafeDoor].Pos.X);
            // rewarded: rota segura da sala (1x por contrato; so com 2+ portas e sem perk na mesa)
            bool route = !_g.BotDrive && S.RewardOffer(Reward.SafeRoute);
            _pauseRoute.gameObject.SetActive(route);
            if (route) LogOferta(Reward.SafeRoute);
        }

        /// <summary>Fim do contrato: entregue = cena do cliente abrindo a caixa e depois o resultado; senao, o resultado direto.</summary>
        public void ShowEnd(ContractResult r)
        {
            _res = r; _devOferta = false;
            if (r == null) { ShowBoard(); return; }
            if (!r.Delivered) { ShowResult(); return; }
            Current = Tela.Cutscene; Only(_cut);
            _cutT = 0f;
            CargoType t = r.Def != null ? r.Def.Cargo : CargoType.Box;
            // item que sobe: sprite da carga/filhote quando a folha existe (a caixa de dentro da caixa fica procedural)
            string sheet = r.Hatched ? "filhote" : t != CargoType.Box && (int)t < WorldView.CargoSheet.Length ? WorldView.CargoSheet[(int)t] : null;
            SpriteSheet item = sheet != null ? SpriteSheet.Get(sheet) : null;
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
            // estrelas: cheia = ganhou, vazada = nao (forma + cor); o criterio fica escrito embaixo de cada uma
            _starGot[0] = r.StarDelivered; _starGot[1] = r.StarIntact; _starGot[2] = r.StarTime;
            for (int i = 0; i < 3; i++)
            {
                _resStar[i].sprite = _starGot[i] ? Art.Star() : Art.StarHollow();
                _resStar[i].color = _starGot[i] ? Art.Gold : Art.A(Art.Tinta, 0.45f);
                _resStar[i].transform.localScale = Vector3.one;
                _resStarText[i].color = _starGot[i] ? Art.Tinta : Art.A(Art.Tinta, 0.55f);
            }
            _starT = 0f; _starNext = 0; _starSounds = 0;
            PintaResultado();
        }

        /// <summary>Textos do resultado (titulo, pagamento, codigo, nota e a oferta de rewarded): de novo depois do bonus dobrado e na troca
        /// de idioma, sem refazer as estrelas.</summary>
        void PintaResultado()
        {
            ContractResult r = _res;
            if (r == null) return;
            ContractDef c = r.Def;
            _resTitle.text = Textos.Rotulo(c) + ": " + Textos.Titulo(r);
            _resTitle.color = r.Delivered ? Art.Good : Art.Bad;
            _resSub.text = r.Delivered ? Textos.Reacao(r) : Textos.Motivo(r);
            string[][] lines = Textos.Pagamento(r, c);
            for (int i = 0; i < _resLabel.Length; i++)
            {
                bool on = i < lines.Length;
                _resLabel[i].gameObject.SetActive(on); _resValue[i].gameObject.SetActive(on);
                if (!on) continue;
                _resLabel[i].text = lines[i][0]; _resValue[i].text = lines[i][1];
                bool strong = ReferenceEquals(lines[i][0], Textos.RotuloPagamento) || ReferenceEquals(lines[i][0], Textos.RotuloTotal);
                _resLabel[i].fontStyle = _resValue[i].fontStyle = strong ? FontStyle.Bold : FontStyle.Normal;
            }
            // sem fim: o codigo da semana fica na tela do desafio (o convite fala de estrelas e o codigo abriria um contrato comum)
            string code = r.Kind == ContractKind.Endless ? null : !string.IsNullOrEmpty(r.ShareCode) ? r.ShareCode : c.ShareCode;
            _resShare.gameObject.SetActive(!string.IsNullOrEmpty(code));
            if (!string.IsNullOrEmpty(code)) _resCode.text = Textos.CodigoDoContrato(code);
            _resShared.text = "";
            // atalho para gastar o que ganhou (guilda/armas) sem passar pelo menu; sem guilda aberta, CONTINUAR fica no centro
            bool guild = S.GuildOpen && !S.SaveRefused;
            _resGuild.gameObject.SetActive(guild);
            RectTransform cont = (RectTransform)_resContinue.transform;
            cont.anchorMin = new Vector2(guild ? 0.05f : 0.2f, 0.015f); cont.anchorMax = new Vector2(guild ? 0.6f : 0.8f, 0.105f);
            OfertaResultado();   // rewarded: retomar a sala (falha) ou dobrar o bonus (entrega)
            bool resume = _resOffer.gameObject.activeSelf && _resOfferT == Reward.Resume;
            bool pending = !r.Delivered && S.Pending != null && S.Pending.Contract == r.Contract;
            bool rare = S.RareOpen && !S.RareAvailable && !S.SaveRefused;   // fim da sessao: "volte amanha para o contrato raro"
            _resNote.text = resume ? Textos.ProtejaRetome : pending ? Textos.AvisoPendente : r.NewBest && r.Stars > 0 ? Textos.NovoRecorde : rare ? Textos.VolteAmanha : "";
            _resNote.color = resume || pending ? Art.Tinta : r.NewBest && r.Stars > 0 ? Art.Good : Art.A(Art.Tinta, 0.75f);
        }

        /// <summary>Antes de comecar: com carga pendente abre a oferta de resgate (go(true) = resgatar); sem, comeca direto (go(false)).
        /// region &gt; 0 = regiao do contrato: o Core so leva o resgate num contrato da mesma regiao (a carga continua pendente), entao em
        /// outra regiao nem pergunta.</summary>
        public void ShowOffer(Action<bool> go, int region = 0)
        {
            LostCargo p = S.Pending;
            if (p == null || (region > 0 && p.Region != region)) { go(false); return; }
            _offerGo = go;
            int bonus = (int)Math.Round(p.Value * Balance.RescueBonus * S.CoinMul, MidpointRounding.AwayFromZero);
            _offerBody.text = Textos.Oferta(p, bonus);
            _offerIcon.sprite = Art.CargoSprite(p.Type); _offerIcon.color = Art.CargoColor(p.Type);
            _offer.gameObject.SetActive(true);
        }

        /// <summary>Voltar (Esc / botao do Android): fecha o que estiver por cima. false = nada para fechar (titulo ou menu: o Game grava e
        /// sai; no contrato sem nada aberto: o Game abre a pausa).</summary>
        public bool Back()
        {
            if (_ads.Voltar() || _lojaTeste.Voltar()) return true;   // anuncio e loja falsa ficam por cima de tudo
            if (Close(_credits) || Close(_settings) || Close(_code)) return true;
            if (_pLoja.M.gameObject.activeSelf) { Fecha(_pLoja); return true; }
            if (_pMods.M.gameObject.activeSelf) { _modsGo = null; Fecha(_pMods); return true; }
            if (_pause.gameObject.activeSelf)
            {
                if (_pauseConfirm.gameObject.activeSelf) { _pauseConfirm.gameObject.SetActive(false); _pauseMain.gameObject.SetActive(true); PauseText(false); }
                else _pause.gameObject.SetActive(false);
                return true;
            }
            if (_perk.gameObject.activeSelf) return true;   // escolha obrigatoria: o voltar nao pula o perk
            if (Current == Tela.Cutscene) { ShowResult(); return true; }
            if (Close(_guild) || Close(_offer) || Close(_semFim)) return true;
            for (int i = _paineis.Count - 1; i >= 0; i--) if (_paineis[i].M.gameObject.activeSelf) { Fecha(_paineis[i]); return true; }
            if (Current == Tela.Result) { Continuar(); return true; }
            if (Current == Tela.Board) { ShowMenu(); return true; }
            return false;
        }

        static bool Close(RectTransform modal)
        {
            if (!modal.gameObject.activeSelf) return false;
            modal.gameObject.SetActive(false);
            return true;
        }

        public void Hurt() => _vigT = 0.4f;
        public void FadeIn() => _fadeT = 0.3f;

        // ------------------------------------------------------------------ quadro a quadro (sem alocar)

        public void Refresh(float dt)
        {
            TimeInScreen += dt;
            _ads.Tick();   // contagem do simulador / espera do anuncio real (tempo real)
            _vigT = Mathf.Max(0f, _vigT - dt);
            _vignette.color = Art.A(Art.Bad, 0.25f * 2.5f * _vigT);   // vinheta de dano a 25% (ART_BIBLE §9)
            _fadeT = Mathf.Max(0f, _fadeT - dt);
            _fade.color = new Color(0f, 0f, 0f, _fadeT / 0.3f);
            switch (Current)
            {
                case Tela.Play:
                    Run r = S.Run;
                    if (r == null) break;
                    RefreshTop(r, dt);
                    // escolha de perk: o Core para o Tick com a oferta aberta; o -bot escolhe sozinho (Bot.Act)
                    bool offer = r.Running && r.PerkOffer != null && !_g.BotDrive;
                    if (offer != _perk.gameObject.activeSelf) { if (offer) ShowPerk(r.PerkOffer); else _perk.gameObject.SetActive(false); }
                    break;
                case Tela.Cutscene: TickCut(dt); break;
                case Tela.Result: TickStars(dt); break;
                case Tela.Title: TickTitle(); break;
                case Tela.Menu: PulseGuild(); break;
            }
        }

        void RefreshTop(Run r, float dt)
        {
            Courier c = r.Courier;
            if (c.Hp != _hp || c.MaxHp != _max)
            {
                if (_hp > c.Hp && _max == c.MaxHp) { _heartPop = 0.25f; _heartPopI = c.Hp; }   // o coracao perdido estoura
                if (c.MaxHp != _max) LayoutHearts(c.MaxHp);
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
                // as 7 cargas da v1 tem forma procedural; as do lancamento (coracao..mapa) usam o quadro da folha, como as telas de meta (antes
                // caiam no ovo marrom), sobre o encaixe limao arredondado
                bool forma = m.Type <= CargoType.Barrel;
                Color icC = Art.CargoColor(m.Type);
                Sprite ic = forma ? Art.CargoSprite(m.Type) : CargaIcone(m.Type, out icC);
                _cargoIcon.sprite = ic; _cargoIcon.color = icC;
                _cargoIcon.preserveAspect = true;
                _cargoEdge.sprite = forma ? ic : Art.Rounded();
                bool far = m.State == CargoState.Ground && V2.Dist(m.Pos, c.Pos) > Balance.AloneDist;
                bool fuse = m.State == CargoState.Ground && m.Fuse > 0f;   // barril no chao: pisca rapido em vermelho e faz tique 1x/s
                int fs = fuse ? Mathf.CeilToInt(Balance.BarrelFuse - m.Fuse) : -1;
                if (fs != _fuseShown) { _fuseShown = fs; if (fs >= 0) Audio.Play("tique", 1.25f, 0f); }
                _cargoEdge.color = m.State == CargoState.Stolen ? Art.Role(EnemyType.Thief)
                    : fuse ? (Mathf.Repeat(Time.unscaledTime * 6f, 1f) < 0.5f ? Art.Bad : Art.Cargo)
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
            if (rescue)
            {
                Color rcC = Art.CargoColor(rc.Type);
                _rescueIcon.sprite = rc.Type <= CargoType.Barrel ? Art.CargoSprite(rc.Type) : CargaIcone(rc.Type, out rcC);
                _rescueIcon.color = rcC; _rescueIcon.preserveAspect = true;
            }

            // relogio: cronometro da mensagem urgente, tempo-alvo do ovo (no cristal o EggTarget e o MeltTime: sem relogio), grito da
            // gaiola (vermelho e pulsando no ultimo 1 s) e o balao no chao (0-4 s, tique por segundo)
            bool timer = r.HasTimer, urgent = false;
            bool egg = r.Contract.Cargo == CargoType.Egg && r.Contract.EggTarget > 0f && !r.Hatched;
            float cargoLeft = timer ? -1f : RelogioCarga(r, m, out urgent);
            bool clock = timer || egg || cargoLeft >= 0f;
            if (_clock.gameObject.activeSelf != clock) { _clock.gameObject.SetActive(clock); _egg.gameObject.SetActive(clock); }
            if (clock)
            {
                float left = timer ? r.TimeLeft : cargoLeft >= 0f ? cargoLeft : r.EggLeft;
                int sec = Mathf.CeilToInt(left);
                if (sec != _eggShown)
                {
                    bool tick = timer ? sec <= 10 : cargoLeft >= 0f && m.Type == CargoType.Balloon;
                    if (tick && _eggShown > sec && left > 0f) Audio.Play("tique", 1f, 0f);   // mensagem acabando, balao subindo (AUDIO.md)
                    _eggShown = sec; _egg.text = Textos.Segundos(left);
                }
                _egg.color = _clock.color = (cargoLeft >= 0f ? urgent : sec <= 10) ? Art.Bad : Art.Ink;
                _egg.transform.localScale = Vector3.one * (urgent ? 1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 12f)) : 1f);
            }
            EstadoCarga(r, m, clock);
            TickFaixa(dt);

            // anel de recarga do dash (azul: so courier, projetil e dash) + cargas do Dash duplo
            bool ready = c.DashReady;
            _dashFill.fillAmount = c.DashCdMax > 0f ? 1f - c.DashCd / c.DashCdMax : 1f;
            _dashFill.color = ready ? Art.Shot : Art.A(Art.Courier, 0.8f);
            _dashIcon.color = ready ? Art.Ink : Art.A(Art.Ink, 0.35f);
            int charges = c.DashChargesMax > 1 ? c.DashCharges : -1;
            if (charges != _dashShown) { _dashShown = charges; _dashN.text = charges >= 0 ? Textos.Num(charges) : ""; }

            // camadas: feitas = verde, atual = clara e maior, futuras = apagadas; a ultima e a bandeira da entrega. No sem fim: "piso N · recorde M"
            bool endless = r.Contract.Kind == ContractKind.Endless;
            if (_floorT.gameObject.activeSelf != endless) _floorT.gameObject.SetActive(endless);
            if (endless && r.Layer != _floorShown) { _floorShown = r.Layer; _floorT.text = Textos.SemFimPiso(r.Layer + 1, Mathf.Max(S.EndlessBest, r.Layer)); }
            int n = endless ? 0 : Mathf.Min(r.LayerCount, _layers.Length);
            if (n != _layerN) LayoutLayers(n);
            for (int i = 0; i < n; i++)
            {
                Image l = _layers[i];
                l.color = i < r.Layer ? Art.Good : i == r.Layer ? Art.Ink : Art.A(Art.Off, 0.9f);
                float sc = i == r.Layer ? 1f : 0.7f;
                if (l.transform.localScale.x != sc) l.transform.localScale = Vector3.one * sc;
            }

            // dica do Core > rota segura revelada (rewarded) > objetivo da sala
            string h = r.Hint != Hint.None ? Textos.Dica(r.Hint) : r.SafeDoor >= 0 && r.SafeDoor < r.Doors.Count ? Textos.RotaSegura(r.Doors[r.SafeDoor].Pos.X) : Textos.Objetivo(r);
            if (!ReferenceEquals(h, _hintShown)) { _hintShown = h; _hint.text = h; _hint.color = r.Hint != Hint.None ? Art.Ink : Art.A(Art.Ink, 0.6f); }
        }

        /// <summary>Segundos do relogio da carga nova: ate o grito da gaiola (run.CageLeft; urgente no ultimo Balance.CageWarn) ou ate o
        /// balao no chao escapar (Balance.BalloonRise - Fuse; sempre urgente). -1 = sem relogio.</summary>
        static float RelogioCarga(Run r, Cargo m, out bool urgent)
        {
            urgent = false;
            if (r.Hatched || m == null || m.State == CargoState.Gone) return -1f;
            if (m.Type == CargoType.GriffinCage) { float l = r.CageLeft; urgent = l >= 0f && l <= Balance.CageWarn; return l; }
            if (m.Type == CargoType.Balloon && m.State == CargoState.Ground && m.Fuse > 0f) { urgent = true; return Mathf.Max(0f, Balance.BalloonRise - m.Fuse); }
            return -1f;
        }

        /// <summary>Estado da carga nova no lugar do relogio (sem relogio): barra do gelo com as marcas de 70 e 40 (run.IceOf), "N/3
        /// rachaduras" do espelho (run.BreakAt) e "lanterna apagada" piscando (Cargo.Out). Refeito so quando a chave muda.</summary>
        void EstadoCarga(Run r, Cargo m, bool clock)
        {
            int key = -1;
            float ice = 0f;
            if (!clock && !r.Hatched && m != null && m.State != CargoState.Gone)
            {
                if (m.Type == CargoType.IceCrystal) { ice = r.IceOf(m); key = 1000 + Mathf.CeilToInt(ice); }
                else if (m.Type == CargoType.Mirror) key = 2000 + m.Cracks * 10 + r.BreakAt;
                else if (m.Type == CargoType.Lantern && m.Out) key = 3000;
            }
            if (key == 3000) _stat.enabled = Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.7f;
            if (key == _statKey) return;
            _statKey = key;
            bool bar = key >= 1000 && key < 2000;
            if (_stat.gameObject.activeSelf != key >= 0) _stat.gameObject.SetActive(key >= 0);
            if (_iceBg.gameObject.activeSelf != bar) _iceBg.gameObject.SetActive(bar);
            if (key < 0) return;
            _stat.enabled = true;
            RectTransform st = _stat.rectTransform;   // com a barra, o rotulo vai para cima dela
            st.anchorMin = new Vector2(0.315f, bar ? 0.4f : 0.27f); st.anchorMax = new Vector2(0.525f, bar ? 0.57f : 0.54f);
            _stat.text = bar ? Textos.Gelo : key == 3000 ? Textos.LanternaApagada : Textos.EspelhoRachaduras(m.Cracks, r.BreakAt);
            _stat.color = key == 3000 || (!bar && m.Cracks > 0) ? Art.Bad : Art.Ink;
            if (bar)
            {
                _iceFill.fillAmount = Mathf.Clamp01(ice / 100f);
                _iceFill.color = ice >= Balance.IceIntact ? IceHi : ice >= Balance.IceCracked ? Ice : Art.Bad;
            }
        }

        /// <summary>Faixa da fase do chefe: entra crescendo (0,2 s), fica e some nos ultimos 0,4 s.</summary>
        void TickFaixa(float dt)
        {
            if (_faixaT <= 0f) return;
            _faixaT -= dt;
            float t = FaixaTime - _faixaT;
            _faixa.transform.localScale = Vector3.one * (t < 0.2f ? 0.6f + 2f * t : 1f);
            _faixa.color = Art.A(Art.Cargo, Mathf.Clamp01(_faixaT / 0.4f));
            if (_faixaT <= 0f) _faixa.gameObject.SetActive(false);
        }

        /// <summary>Coracoes em 1 fileira ate 8; acima disso (guilda 8 niveis + perk), 2 fileiras.</summary>
        void LayoutHearts(int max)
        {
            int rows = max > 8 ? 2 : 1, per = Mathf.Max(1, Mathf.CeilToInt(max / (float)rows));
            float w = 1f / Mathf.Max(per, 6);
            for (int i = 0; i < _hearts.Length; i++)
            {
                int row = i / per, col = i % per;
                float y1 = 1f - row / (float)rows, y0 = y1 - 1f / rows;
                RectTransform rt = _hearts[i].rectTransform;
                rt.anchorMin = new Vector2(col * w, y0 + 0.06f / rows); rt.anchorMax = new Vector2((col + 1) * w, y1 - 0.06f / rows);
            }
        }

        void LayoutLayers(int n)
        {
            _layerN = n;
            float step = Mathf.Min(0.058f, 0.35f / Mathf.Max(1, n));
            for (int i = 0; i < _layers.Length; i++)
            {
                Image l = _layers[i];
                bool on = i < n;
                l.gameObject.SetActive(on);
                if (!on) continue;
                l.sprite = i == n - 1 ? Art.Flag() : Art.Disc();
                l.rectTransform.anchorMin = new Vector2(0.64f + i * step, 0.3f); l.rectTransform.anchorMax = new Vector2(0.64f + i * step + step * 0.86f, 0.51f);
            }
        }

        /// <summary>Estrelas ganhas "batem" uma a uma (som estrela subindo de tom + pulo de escala).</summary>
        void TickStars(float dt)
        {
            _starT += dt;
            for (; _starNext < 3 && _starT >= 0.35f + 0.25f * _starNext; _starNext++)
            {
                if (!_starGot[_starNext]) continue;
                Audio.Play("estrela", StarPitch[Mathf.Min(_starSounds++, 2)], 0f);
                _resStar[_starNext].transform.localScale = Vector3.one * 1.4f;
            }
            for (int i = 0; i < 3; i++)
            {
                Transform t = _resStar[i].transform;
                if (t.localScale.x > 1f) t.localScale = Vector3.one * Mathf.Max(1f, t.localScale.x - 2f * dt);
            }
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

        // ------------------------------------------------------------------ construcao: faixa do contrato

        RectTransform BuildTop()
        {
            RectTransform top = Art.Node(_safe, "Topo", new Vector2(0f, 1f - Band), Vector2.one);
            Image bg = top.gameObject.AddComponent<Image>(); bg.color = Art.A(Art.Tinta, 0.85f); bg.raycastTarget = false;

            // linha A: coracoes | moedas | pausa
            RectTransform hearts = Art.Node(top, "Coracoes", new Vector2(0.02f, 0.55f), new Vector2(0.58f, 0.97f));
            _hearts = new Image[Balance.MaxHpCap];
            for (int i = 0; i < _hearts.Length; i++) _hearts[i] = Art.Img(hearts, "Coracao", Art.Heart(), Art.Bad, Vector2.zero, Vector2.one);
            LayoutHearts(Balance.BaseHp);
            Art.Img(top, "Moeda", Art.CoinShape(), Art.Coin, new Vector2(0.6f, 0.6f), new Vector2(0.67f, 0.92f));
            _coins = Art.Outlined(Art.NewText(top, "Moedas", 52, new Vector2(0.68f, 0.55f), new Vector2(0.85f, 0.97f), TextAnchor.MiddleLeft));
            _coins.fontStyle = FontStyle.Bold;
            Image pb = Art.Panel(top, "Pausa", Art.Paper, new Vector2(0.865f, 0.56f), new Vector2(0.98f, 0.97f));
            Button pause = pb.gameObject.AddComponent<Button>();
            pause.targetGraphic = pb;
            pause.onClick.AddListener(() => { Audio.Play("clique"); ShowPause(false); });
            Art.Img(pb.transform, "Icone", Art.Pause(), Art.Tinta, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            Art.Toque(pb, 16f, 26f);   // ~124 x 118 px de desenho -> ~156 x 170 de toque (48 dp)

            // linha B: carga + rachaduras | resgate | relogio (ovo/mensagem) | dash | camadas
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
            _egg = Art.Outlined(Art.NewText(top, "Tempo", 38, new Vector2(0.385f, 0.27f), new Vector2(0.52f, 0.54f), TextAnchor.MiddleLeft));
            _egg.fontStyle = FontStyle.Bold;
            // carga nova sem relogio: o mesmo lugar mostra o gelo (barra com as marcas de 70 e 40), o espelho ou a lanterna apagada
            _stat = Shrink(Art.Outlined(Art.NewText(top, "EstadoCarga", 34, new Vector2(0.315f, 0.27f), new Vector2(0.525f, 0.54f))), 18);
            _stat.fontStyle = FontStyle.Bold;
            _stat.gameObject.SetActive(false);
            _iceBg = Art.Img(top, "GeloFundo", Art.Square(), Art.A(Art.Off, 0.9f), new Vector2(0.33f, 0.3f), new Vector2(0.52f, 0.4f));
            _iceBg.preserveAspect = false;
            _iceFill = Art.Img(_iceBg.transform, "Gelo", Art.Square(), Ice, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.8f));
            _iceFill.preserveAspect = false; _iceFill.type = Image.Type.Filled; _iceFill.fillMethod = Image.FillMethod.Horizontal;
            foreach (float mark in new[] { Balance.IceIntact, Balance.IceCracked })   // abaixo de 70 = 1 rachadura; abaixo de 40 = 2
            {
                float x = 0.03f + 0.94f * mark / 100f;
                Art.Img(_iceBg.transform, "Marca", Art.Square(), Art.Ink, new Vector2(x - 0.012f, 0f), new Vector2(x + 0.012f, 1f)).preserveAspect = false;
            }
            _iceBg.gameObject.SetActive(false);
            Art.Img(top, "DashFundo", Art.Ring(), Art.A(Art.Off, 0.9f), new Vector2(0.53f, 0.27f), new Vector2(0.61f, 0.54f));
            _dashFill = Art.Img(top, "Dash", Art.Ring(), Art.Shot, new Vector2(0.53f, 0.27f), new Vector2(0.61f, 0.54f));
            _dashFill.type = Image.Type.Filled; _dashFill.fillMethod = Image.FillMethod.Radial360;
            _dashFill.fillOrigin = (int)Image.Origin360.Top; _dashFill.fillClockwise = true;
            _dashIcon = Art.Img(top, "DashIcone", Art.Chevrons(), Art.Ink, new Vector2(0.55f, 0.33f), new Vector2(0.59f, 0.48f));
            _dashN = Art.Outlined(Art.NewText(top, "DashCargas", 30, new Vector2(0.595f, 0.22f), new Vector2(0.64f, 0.38f)));
            _dashN.fontStyle = FontStyle.Bold;
            for (int i = 0; i < _layers.Length; i++) _layers[i] = Art.Img(top, "Camada", Art.Disc(), Art.Off, Vector2.zero, Vector2.zero);
            _floorT = Shrink(Art.Outlined(Art.NewText(top, "Piso", 32, new Vector2(0.64f, 0.27f), new Vector2(0.99f, 0.54f), TextAnchor.MiddleLeft)), 18);
            _floorT.fontStyle = FontStyle.Bold;
            _floorT.gameObject.SetActive(false);

            // linha C: a dica (1 linha)
            _hint = Art.Outlined(Art.NewText(top, "Dica", 36, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.26f)));
            _hint.fontStyle = FontStyle.Bold;
            _hint.resizeTextForBestFit = true; _hint.resizeTextMinSize = 20; _hint.resizeTextMaxSize = 36;
            _hint.horizontalOverflow = HorizontalWrapMode.Wrap; _hint.verticalOverflow = VerticalWrapMode.Truncate;

            // faixa da fase do chefe, logo abaixo da faixa de cima (sobre o mundo; nao pega toque)
            _faixa = Art.Outlined(Art.NewText(top, "FaseDoChefe", 56, new Vector2(0.03f, -0.62f), new Vector2(0.97f, -0.12f)), 4f);
            _faixa.fontStyle = FontStyle.Bold; _faixa.raycastTarget = false;
            _faixa.resizeTextForBestFit = true; _faixa.resizeTextMinSize = 30; _faixa.resizeTextMaxSize = 56;
            _faixa.gameObject.SetActive(false);
            return top;
        }

        // ------------------------------------------------------------------ construcao: oferta, pausa, cena, resultado

        RectTransform BuildOffer()
        {
            RectTransform m = Modal("Oferta");
            Image face = Card(m, "Cartao", new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.73f));
            Text title = Art.NewText(face.transform, "Titulo", 52, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.97f));
            Loc(title, () => Textos.OfertaTitulo); title.color = Art.Tinta; title.fontStyle = FontStyle.Bold;
            Art.Img(face.transform, "Contorno", Art.Rounded(), Art.Cargo, new Vector2(0.4f, 0.62f), new Vector2(0.6f, 0.84f));
            _offerIcon = Art.Img(face.transform, "Carga", Art.Rounded(), Color.white, new Vector2(0.42f, 0.64f), new Vector2(0.58f, 0.82f));
            Image seal = Art.Img(_offerIcon.transform, "SeloFundo", Art.Disc(), Art.Tinta, new Vector2(0.6f, 0.6f), new Vector2(1.1f, 1.1f));
            Art.Img(seal.transform, "Selo", Art.Star(), Art.Cream, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
            _offerBody = Art.NewText(face.transform, "Corpo", 30, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.61f));
            _offerBody.color = Art.Tinta;
            _offerBody.resizeTextForBestFit = true; _offerBody.resizeTextMinSize = 18; _offerBody.resizeTextMaxSize = 30;
            _offerBody.verticalOverflow = VerticalWrapMode.Truncate;
            Btn(face.transform, () => Textos.Resgatar, 40, Art.Good, new Vector2(0.05f, 0.04f), new Vector2(0.48f, 0.23f), () => Go(true), "confirmar");
            Btn(face.transform, () => Textos.Recusar, 40, Art.PaperEdge, new Vector2(0.52f, 0.04f), new Vector2(0.95f, 0.23f), () => Go(false));
            return m;
        }

        void Go(bool rescue)
        {
            _offer.gameObject.SetActive(false);
            Action<bool> go = _offerGo;
            _offerGo = null;
            go?.Invoke(rescue);
        }

        RectTransform BuildPause()
        {
            RectTransform m = Modal("Pausa");
            // cartao de ~1230 px de referencia: 4 botoes de ~0,14 (~172 px, acima de 48 dp)
            Image face = Card(m, "Cartao", new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.82f));
            _pauseTitle = Art.NewText(face.transform, "Titulo", 54, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));
            _pauseTitle.color = Art.Tinta; _pauseTitle.fontStyle = FontStyle.Bold;
            _pauseBody = Shrink(Art.NewText(face.transform, "Corpo", 30, new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.88f)), 18);
            _pauseBody.color = Art.Tinta;
            _pauseMain = Art.Node(face.transform, "Principal", Vector2.zero, Vector2.one);
            Btn(_pauseMain, () => Textos.Continuar, 46, Art.Good, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.71f), () => _pause.gameObject.SetActive(false));
            // rewarded: a porta mais segura desta sala (anuncio de teste)
            _pauseRoute = Btn(_pauseMain, () => Textos.OfertaRota, 34, Art.Gold, new Vector2(0.08f, 0.39f), new Vector2(0.92f, 0.54f), PedeRota);
            Btn(_pauseMain, () => Textos.MenuConfig, 38, Art.PaperEdge, new Vector2(0.08f, 0.215f), new Vector2(0.92f, 0.365f), ShowSettings);
            Btn(_pauseMain, () => Textos.Desistir, 34, Art.Bad, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.19f), () =>
            {
                _pauseTitle.text = Textos.DesistirTitulo; _pauseBody.text = Textos.DesistirCorpo;
                _pauseMain.gameObject.SetActive(false); _pauseConfirm.gameObject.SetActive(true);
            });
            _pauseConfirm = Art.Node(face.transform, "Confirmar", Vector2.zero, Vector2.one);
            Btn(_pauseConfirm, () => Textos.DesistirSim, 38, Art.Bad, new Vector2(0.08f, 0.29f), new Vector2(0.92f, 0.46f), () => { _pause.gameObject.SetActive(false); _g.Abandon(); });
            Btn(_pauseConfirm, () => Textos.Voltar, 38, Art.PaperEdge, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.21f), () => ShowPause(false));
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
            Text hint = Art.NewText(c, "Pular", 30, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.1f));
            Loc(hint, () => Textos.Pular); hint.color = Art.Dim;
            return c;
        }

        RectTransform BuildResult()
        {
            RectTransform m = Modal("Resultado");
            Image face = Card(m, "Cartao", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
            Transform f = face.transform;
            _resTitle = Art.NewText(f, "Titulo", 58, new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.99f));
            _resTitle.fontStyle = FontStyle.Bold;
            _resTitle.resizeTextForBestFit = true; _resTitle.resizeTextMinSize = 30; _resTitle.resizeTextMaxSize = 58;
            _resSub = Art.NewText(f, "Sub", 32, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.92f));
            _resSub.color = Art.Tinta;
            _resSub.resizeTextForBestFit = true; _resSub.resizeTextMinSize = 22; _resSub.resizeTextMaxSize = 32;
            // 3 estrelas com o criterio escrito embaixo (CONTRATO_V1 §7)
            Func<string>[] crit = { () => Textos.EstrelaEntregue, () => Textos.EstrelaIntacta, () => Textos.EstrelaTempo };
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.04f + i * 0.31f;
                _resStar[i] = Art.Img(f, "Estrela", Art.StarHollow(), Art.Gold, new Vector2(x0 + 0.08f, 0.775f), new Vector2(x0 + 0.22f, 0.855f));
                _resStarText[i] = Loc(Art.NewText(f, "Criterio", 28, new Vector2(x0, 0.73f), new Vector2(x0 + 0.3f, 0.775f)), crit[i]);
                _resStarText[i].fontStyle = FontStyle.Bold;
                _resStarText[i].resizeTextForBestFit = true; _resStarText[i].resizeTextMinSize = 18; _resStarText[i].resizeTextMaxSize = 28;
            }
            // pagamento: ate ResLinhas linhas de 0,0275 do cartao (~50 px de referencia) entre as estrelas e o codigo
            for (int i = 0; i < _resLabel.Length; i++)
            {
                float y1 = 0.72f - i * 0.0275f, y0 = y1 - 0.026f;
                _resLabel[i] = Shrink(Art.NewText(f, "Rotulo", 30, new Vector2(0.06f, y0), new Vector2(0.72f, y1), TextAnchor.MiddleLeft), 18);
                _resLabel[i].color = Art.Tinta;
                _resValue[i] = Art.NewText(f, "Valor", 32, new Vector2(0.72f, y0), new Vector2(0.94f, y1), TextAnchor.MiddleRight);
                _resValue[i].color = Art.Tinta;
            }
            // codigo do contrato para compartilhar (some quando o contrato nao tem codigo)
            _resShare = Art.Node(f, "Codigo", new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.33f));
            _resCode = Art.NewText(_resShare, "Texto", 40, new Vector2(0f, 0.3f), new Vector2(0.56f, 1f), TextAnchor.MiddleLeft);
            _resCode.color = Art.Tinta; _resCode.fontStyle = FontStyle.Bold;
            _resShared = Art.NewText(_resShare, "Copiado", 24, new Vector2(0f, 0f), new Vector2(0.56f, 0.3f), TextAnchor.MiddleLeft);
            _resShared.color = Art.Good; _resShared.fontStyle = FontStyle.Bold;
            Btn(_resShare, () => Textos.Compartilhar, 36, Art.Gold, new Vector2(0.58f, 0f), new Vector2(1f, 1f), Share);
            _resNote = Art.NewText(f, "Nota", 30, new Vector2(0.06f, 0.193f), new Vector2(0.94f, 0.238f));
            _resNote.color = Art.Tinta; _resNote.fontStyle = FontStyle.Bold;
            _resNote.resizeTextForBestFit = true; _resNote.resizeTextMinSize = 18; _resNote.resizeTextMaxSize = 30;
            // oferta de rewarded (retomar / dobrar o bonus): ~0,085 do cartao = ~153 px de referencia
            _resOffer = Btn(f, () => Textos.OfertaRetomar, 36, Art.Gold, new Vector2(0.05f, 0.108f), new Vector2(0.95f, 0.19f), PedeOfertaResultado);
            _resOfferText = _resOffer.GetComponentInChildren<Text>();
            _resOffer.gameObject.SetActive(false);
            _resContinue = Btn(f, () => Textos.Continuar, 46, Art.Good, new Vector2(0.2f, 0.015f), new Vector2(0.8f, 0.103f), Continuar);
            _resGuild = Btn(f, () => Textos.MenuGuilda, 40, Art.Paper, new Vector2(0.63f, 0.015f), new Vector2(0.95f, 0.103f), () => ShowGuild(0));
            return m;
        }

        void Share()
        {
            ContractResult r = _res;
            if (r == null) return;
            string code = !string.IsNullOrEmpty(r.ShareCode) ? r.ShareCode : r.Def.ShareCode;
            if (string.IsNullOrEmpty(code)) return;
            bool chooser = Exportar.Texto(Textos.Convite(code, r.Stars), Textos.Compartilhar);
            _resShared.text = chooser ? "" : Textos.Copiado;
            _g.Log("share_code", code, r.Stars.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        // ------------------------------------------------------------------ pecas

        /// <summary>Tela modal: escurece o resto e segura os toques. Comeca fechada.</summary>
        RectTransform Modal(string name, float dim = 0.62f)
        {
            RectTransform m = Art.Node(_safe, name, Vector2.zero, Vector2.one);
            m.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, dim);
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

        /// <summary>Texto escuro sobre o papel.</summary>
        static Text Ink(Transform parent, string name, int size, Vector2 min, Vector2 max, TextAnchor align = TextAnchor.MiddleCenter, bool bold = false)
        {
            Text t = Art.NewText(parent, name, size, min, max, align);
            t.color = Art.Tinta;
            if (bold) t.fontStyle = FontStyle.Bold;
            return t;
        }

        /// <summary>Texto que encolhe para caber (minimo legivel).</summary>
        static Text Shrink(Text t, int min)
        {
            t.resizeTextForBestFit = true; t.resizeTextMinSize = min; t.resizeTextMaxSize = t.fontSize;
            return t;
        }
    }
}
