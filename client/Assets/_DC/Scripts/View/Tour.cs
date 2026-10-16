using System;
using System.Collections.Generic;
using DC.Core;
using UnityEngine;
using UnityEngine.UI;
using Rect = UnityEngine.Rect;

namespace DC
{
    /// <summary>
    /// Tour guiado (pedido do Vinicius, 2026-10-16: "congela a tela, fica apenas uma area sinalizada e mostra qual botao aperta, por que e
    /// pra que"). Cada passo aparece uma vez (PlayerPrefs "dc_tour"; o REVER TUTORIAL das configuracoes zera):
    ///  - explicacao: escurece tudo menos o alvo, cartao com o que e/quando/por que e ENTENDI. No contrato o Hud.Blocking segura a Sim e o
    ///    joystick (a tela "congela"; as animacoes seguem);
    ///  - acao: o alvo fica aberto ao toque (seta + anel pulsando) e o passo termina quando a acao acontece (ex.: abriu o quadro).
    /// Os gatilhos leem o estado (tela, modal aberto, dica do Core, ladrao vivo, portas abertas...) a cada quadro. Desligado no
    /// -testsession (fotos, gravacoes), no -bot e no -autoplay; -tutorial liga (sem gravar no -testsession; com o -bot o ENTENDI se aperta
    /// sozinho em 2 s).
    /// </summary>
    public sealed partial class Hud
    {
        sealed class Passo
        {
            public string Id;
            public Func<Rect?> Alvo;           // px de tela, recalculado a cada quadro; null = sem buraco (cartao no meio)
            public Func<string> Titulo, Texto;
            public Func<bool> Feito;           // acao: termina sozinho quando for verdade; null = explicacao (ENTENDI)
            public bool Arrastar;              // mostra o dedo arrastando de um lado para o outro no alvo
            public Tela Tela;                  // a tela em que o passo nasceu (Enfileira)
        }

        /// <summary>Game: tour ligado (desligado no -testsession sem -tutorial, no -bot e na gravacao).</summary>
        public bool TourLigado { get; set; }
        /// <summary>Game: grava os passos vistos (falso no -testsession).</summary>
        public bool TourGrava { get; set; }
        public const string TourKey = "dc_tour";

        readonly HashSet<string> _vistos = new HashSet<string>();
        readonly Queue<Passo> _fila = new Queue<Passo>();
        Passo _passo;
        int _passoN, _passoTotal;
        float _passoT, _tourEspera;
        Tela _tourTela;
        RectTransform _tour, _tourCard, _tourDedo;
        readonly Image[] _tourVeu = new Image[4];
        Image _tourBorda, _tourTampa, _tourSeta, _tourAnel;
        Text _tourTitulo, _tourTexto, _tourConta;
        Button _tourOk;
        Button _menuJogar, _menuTemporada, _menuMarcos, _menuRoupas, _menuConfig, _pauseBtn;

        bool TourAberto => _tour != null && _tour.gameObject.activeSelf;

        // ------------------------------------------------------------------ construcao

        void BuildTour(RectTransform canvas)
        {
            foreach (string id in PlayerPrefs.GetString(TourKey, "").Split(',')) if (id.Length > 0) _vistos.Add(id);
            _tour = Art.Node(canvas, "Tour", Vector2.zero, Vector2.one);   // irmao depois da AreaSegura: por cima de tudo
            for (int i = 0; i < _tourVeu.Length; i++)
            {
                _tourVeu[i] = Art.Img(_tour, "Veu", Art.Square(), new Color(0f, 0f, 0f, 0.74f), Vector2.zero, Vector2.zero);
                _tourVeu[i].preserveAspect = false; _tourVeu[i].raycastTarget = true;   // segura o toque fora do alvo
            }
            _tourTampa = Art.Img(_tour, "Tampa", Art.Square(), new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.zero);
            _tourTampa.preserveAspect = false; _tourTampa.raycastTarget = true;   // explicacao: o alvo aparece mas nao recebe o toque
            _tourBorda = Art.Img(_tour, "Borda", Art.Contorno(), Art.Gold, Vector2.zero, Vector2.zero);
            _tourBorda.type = Image.Type.Sliced; _tourBorda.preserveAspect = false;
            _tourAnel = Art.Img(_tour, "Anel", Art.Ring(), Art.Gold, Vector2.zero, Vector2.zero);
            _tourSeta = Art.Img(_tour, "Seta", Art.Seta(), Art.Gold, Vector2.zero, Vector2.zero);
            _tourDedo = Art.Img(_tour, "Dedo", Art.Disc(), Art.A(Color.white, 0.85f), Vector2.zero, Vector2.zero).rectTransform;
            Art.Img(_tourDedo, "Halo", Art.Ring(), Art.Gold, new Vector2(-0.35f, -0.35f), new Vector2(1.35f, 1.35f));

            Image face = Card(_tour, "Cartao", new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.62f));
            _tourCard = (RectTransform)face.transform.parent.parent;
            Transform f = face.transform;
            _tourTitulo = Art.Letreiro(Ink(f, "Titulo", 52, new Vector2(0.05f, 0.72f), new Vector2(0.8f, 0.97f), TextAnchor.MiddleLeft, true), 52);
            _tourTitulo.font = Art.Titulo(); _tourTitulo.color = Art.Gold;
            _tourConta = Ink(f, "Conta", 30, new Vector2(0.8f, 0.74f), new Vector2(0.96f, 0.95f), TextAnchor.MiddleRight, true);
            _tourConta.color = Art.A(Art.Tinta, 0.6f);
            _tourTexto = Shrink(Ink(f, "Texto", 34, new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.73f), TextAnchor.UpperLeft), 22);
            Button pular = Art.NewButton(f, "", 28, Art.PaperEdge, new Vector2(0.04f, 0.04f), new Vector2(0.44f, 0.24f), TourPula);
            Loc(pular.GetComponentInChildren<Text>(), () => Textos.TourPular);
            _tourOk = Btn(f, () => Textos.TourOk, 40, Art.Good, new Vector2(0.56f, 0.03f), new Vector2(0.96f, 0.25f), TourOk, "confirmar");
            _tour.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ por quadro

        void TickTour(float dt)
        {
            if (!TourLigado || _tour == null) return;
            if (Current != _tourTela) { _tourTela = Current; _tourEspera = 0f; }
            _tourEspera += dt;
            if (_passo == null)
            {
                if (_fila.Count == 0) Gatilhos();
                if (_fila.Count > 0 && _tourEspera > 0.45f) ProximoPasso();
                return;
            }
            _passoT += dt;
            if (_passo.Feito != null && _passo.Feito()) { Visto(_passo.Id, "acao"); ProximoPasso(); return; }
            // acao cuja tela mudou ou cujo botao sumiu (ex.: o resultado fechou): cancela sem marcar visto; volta na proxima vez
            if (_passo.Feito != null && (Current != _passo.Tela || _passo.Alvo() == null)) { CancelaTour(); return; }
            if (_g.BotDrive && _passo.Feito == null && _passoT > 2f) { TourOk(); return; }   // -bot -tutorial: fotos e gravacao
            DesenhaPasso();
        }

        void ProximoPasso()
        {
            _passo = null;
            while (_fila.Count > 0 && _passo == null)
            {
                Passo p = _fila.Dequeue();
                if (!_vistos.Contains(p.Id)) _passo = p;
            }
            if (_passo == null) { _tour.gameObject.SetActive(false); _passoTotal = 0; return; }
            _passoN++; _passoT = 0f;
            _tour.SetAsLastSibling();
            _tour.gameObject.SetActive(true);
            _tourTitulo.text = _passo.Titulo();
            _tourTexto.text = _passo.Texto();
            _tourConta.text = _passoTotal > 1 ? Textos.TourConta(_passoN, _passoTotal) : "";
            _tourOk.gameObject.SetActive(_passo.Feito == null);
            _tourTampa.gameObject.SetActive(_passo.Feito == null);
            Audio.Play("clique");
            DesenhaPasso();
        }

        void Enfileira(params Passo[] passos)
        {
            _fila.Clear();
            foreach (Passo p in passos) if (!_vistos.Contains(p.Id)) { p.Tela = Current; _fila.Enqueue(p); }
            _passoN = 0; _passoTotal = _fila.Count;
        }

        void TourOk()
        {
            if (_passo == null) { _tour.gameObject.SetActive(false); return; }
            Visto(_passo.Id, "ok");
            ProximoPasso();
        }

        void CancelaTour()
        {
            _fila.Clear(); _passo = null; _passoTotal = 0;
            _tour.gameObject.SetActive(false);
        }

        void TourPula()
        {
            foreach (string id in TodosPassos) _vistos.Add(id);
            if (_passo != null) _g.Log("tour", _passo.Id, "pular");
            GravaTour();
            _fila.Clear(); _passo = null; _passoTotal = 0;
            _tour.gameObject.SetActive(false);
        }

        void Visto(string id, string como)
        {
            if (!_vistos.Add(id)) return;
            _g.Log("tour", id, como);
            GravaTour();
        }

        void GravaTour()
        {
            if (!TourGrava) return;
            PlayerPrefs.SetString(TourKey, string.Join(",", _vistos));
            PlayerPrefs.Save();
        }

        /// <summary>Configuracoes: o tour volta do comeco (a fila do menu aparece na proxima tela).</summary>
        void ReverTour()
        {
            _vistos.Clear(); _fila.Clear(); _passo = null; _passoTotal = 0;
            if (_tour != null) _tour.gameObject.SetActive(false);
            if (TourGrava) { PlayerPrefs.DeleteKey(TourKey); PlayerPrefs.Save(); }
            _exportStatus.text = Textos.TourRevisto; _exportStatus.color = Art.Tinta;
        }

        // ------------------------------------------------------------------ desenho: veu com buraco, borda, seta, anel, dedo, cartao

        void DesenhaPasso()
        {
            Rect tela = _tour.rect;
            Rect? alvo = _passo.Alvo?.Invoke();
            Rect b = alvo.HasValue ? ParaLocal(alvo.Value) : new Rect(tela.center, Vector2.zero);
            b = Rect.MinMaxRect(Mathf.Max(b.xMin, tela.xMin), Mathf.Max(b.yMin, tela.yMin), Mathf.Min(b.xMax, tela.xMax), Mathf.Min(b.yMax, tela.yMax));
            Poe(_tourVeu[0], Rect.MinMaxRect(tela.xMin, b.yMax, tela.xMax, tela.yMax));   // cima
            Poe(_tourVeu[1], Rect.MinMaxRect(tela.xMin, tela.yMin, tela.xMax, b.yMin));   // baixo
            Poe(_tourVeu[2], Rect.MinMaxRect(tela.xMin, b.yMin, b.xMin, b.yMax));         // esquerda
            Poe(_tourVeu[3], Rect.MinMaxRect(b.xMax, b.yMin, tela.xMax, b.yMax));         // direita
            Poe(_tourTampa, b);
            bool tem = alvo.HasValue && b.width > 1f && b.height > 1f;
            float pulso = 0.5f + 0.5f * Mathf.Sin(_passoT * 6f);
            _tourBorda.enabled = tem;
            if (tem) { Poe(_tourBorda, new Rect(b.x - 14f, b.y - 14f, b.width + 28f, b.height + 28f)); _tourBorda.color = Art.A(Art.Gold, 0.65f + 0.35f * pulso); }

            // cartao do lado com mais espaco; a seta aponta do cartao para o alvo
            bool cartaoEmCima = !tem || b.center.y < tela.center.y;
            float ch = tela.height * 0.25f, cw = tela.width * 0.9f, margem = 40f;
            float cy = !tem ? tela.center.y
                : cartaoEmCima ? Mathf.Min(b.yMax + margem + 90f + ch / 2f, tela.yMax - 60f - ch / 2f)
                : Mathf.Max(b.yMin - margem - 90f - ch / 2f, tela.yMin + 60f + ch / 2f);
            Poe(_tourCard, new Rect(tela.center.x - cw / 2f, cy - ch / 2f, cw, ch));

            bool acao = _passo.Feito != null;
            _tourSeta.enabled = tem;
            if (tem)
            {
                float s = 90f, quica = 18f * pulso;
                float sy = cartaoEmCima ? b.yMax + margem / 2f + s / 2f + quica : b.yMin - margem / 2f - s / 2f - quica;
                Poe(_tourSeta, new Rect(b.center.x - s / 2f, sy - s / 2f, s, s));
                _tourSeta.rectTransform.localRotation = Quaternion.Euler(0f, 0f, cartaoEmCima ? 0f : 180f);   // a seta desenhada aponta para baixo
            }
            _tourAnel.enabled = tem && acao;
            if (_tourAnel.enabled)
            {
                float k = Mathf.Repeat(_passoT, 1.1f) / 1.1f, r = Mathf.Min(b.width, b.height) * (0.5f + 0.6f * k);
                Poe(_tourAnel, new Rect(b.center.x - r, b.center.y - r, 2f * r, 2f * r));
                _tourAnel.color = Art.A(Art.Gold, 0.9f * (1f - k));
            }
            _tourDedo.gameObject.SetActive(tem && _passo.Arrastar);
            if (_tourDedo.gameObject.activeSelf)
            {
                float d = 70f, x = b.center.x + Mathf.Sin(_passoT * 2.2f) * Mathf.Min(b.width * 0.32f, 260f);
                Poe(_tourDedo, new Rect(x - d / 2f, b.center.y - d / 2f, d, d));
            }
        }

        /// <summary>Retangulo em px de tela -> coordenadas locais do Tour (Canvas Overlay: camera null).</summary>
        Rect ParaLocal(Rect px)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_tour, px.min, null, out Vector2 a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_tour, px.max, null, out Vector2 b);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        static void Poe(Graphic g, Rect r) => Poe(g.rectTransform, r);

        static void Poe(RectTransform t, Rect r)
        {
            t.anchorMin = t.anchorMax = t.pivot = new Vector2(0.5f, 0.5f);
            t.anchoredPosition = r.center; t.sizeDelta = new Vector2(Mathf.Max(0f, r.width), Mathf.Max(0f, r.height));
        }

        // ------------------------------------------------------------------ alvos

        static readonly Vector3[] Cantos = new Vector3[4];

        /// <summary>Retangulo em px de tela de um elemento da interface (Canvas Overlay: os cantos de mundo ja sao px), com folga.</summary>
        static Rect? Ui(Component c, float folga = 10f)
        {
            if (c == null || !c.gameObject.activeInHierarchy) return null;
            ((RectTransform)c.transform).GetWorldCorners(Cantos);
            return Rect.MinMaxRect(Cantos[0].x - folga, Cantos[0].y - folga, Cantos[2].x + folga, Cantos[2].y + folga);
        }

        static Rect? Junta(Rect? a, Rect? b)
        {
            if (!a.HasValue) return b;
            if (!b.HasValue) return a;
            return Rect.MinMaxRect(Mathf.Min(a.Value.xMin, b.Value.xMin), Mathf.Min(a.Value.yMin, b.Value.yMin),
                Mathf.Max(a.Value.xMax, b.Value.xMax), Mathf.Max(a.Value.yMax, b.Value.yMax));
        }

        /// <summary>Area no mundo (metros da sala) em px de tela: centro em `p` subindo `alto` m (o corpo), w x h m.</summary>
        Rect? Mundo(V2 p, float w, float h, float alto = 0.6f)
        {
            WorldView v = _g.Mundo;
            if (v == null) return null;
            Vector2 c = v.NaTela(p, alto), d = v.NaTela(new V2(p.X + 1f, p.Y), alto);
            float m = Mathf.Max(1f, Mathf.Abs(d.x - c.x));   // px por metro na horizontal
            return new Rect(c.x - w * m / 2f, c.y - h * m / 2f, w * m, h * m);
        }

        // ------------------------------------------------------------------ gatilhos (o que mostrar agora)

        static readonly string[] TodosPassos =
        {
            "jogo_voce", "jogo_arrastar", "jogo_vida", "jogo_carga", "jogo_pausa", "jogo_pousar", "jogo_ladrao", "jogo_dash", "jogo_portas",
            "jogo_bau", "jogo_entrega", "menu_jogar", "quadro_c1", "resultado", "perk", "menu_guilda", "guilda_comprar", "menu_bau",
            "menu_temporada", "menu_marcos", "menu_roupas", "menu_loja", "menu_config", "quadro_novidades",
        };

        bool Visto(string id) => _vistos.Contains(id);

        bool AlgumModal()
        {
            foreach (RectTransform m in _modals) if (m.gameObject.activeSelf) return true;
            return _ads.Aberto || _lojaTeste.Aberto;
        }

        void Gatilhos()
        {
            if (_guild.gameObject.activeSelf)   // a guilda abre do menu ou do atalho do resultado
            {
                if (S.GuildOpen && !Visto("guilda_comprar"))
                    Enfileira(Explica("guilda_comprar", () => Junta(Ui(_ups[0].Buy), Ui(_ups[0].Level)), () => Textos.TourGuildaComprarT, () => Textos.TourGuildaComprar));
                return;
            }
            switch (Current)
            {
                case Tela.Play: GatilhosJogo(); break;
                case Tela.Menu:
                    if (AlgumModal()) break;
                    if (S.Reputation == 0 && !Visto("menu_jogar"))
                        Enfileira(Acao("menu_jogar", () => Ui(_menuJogar), () => Textos.TourJogarT, () => Textos.TourJogar, () => Current == Tela.Board));
                    else if (S.GuildOpen && !Visto("menu_guilda"))
                        Enfileira(Acao("menu_guilda", () => Ui(_menuGuild), () => Textos.TourGuildaT, () => Textos.TourGuilda, () => _guild.gameObject.activeSelf));
                    else if (S.GuildOpen && Visto("menu_guilda") && !Visto("menu_config"))   // o giro do menu (config e o ultimo)
                        Enfileira(Explica("menu_bau", () => Ui(_bau.B), () => Textos.TourBauDiaT, () => Textos.TourBauDia),
                            Explica("menu_temporada", () => Ui(_menuTemporada), () => Textos.TourTemporadaT, () => Textos.TourTemporada),
                            Explica("menu_marcos", () => Ui(_menuMarcos), () => Textos.TourMarcosT, () => Textos.TourMarcos),
                            Explica("menu_roupas", () => Ui(_menuRoupas), () => Textos.TourRoupasT, () => Textos.TourRoupas),
                            Explica("menu_loja", () => Ui(_menuLoja), () => Textos.TourLojaT, () => Textos.TourLoja),
                            Explica("menu_config", () => Ui(_menuConfig), () => Textos.TourConfigT, () => Textos.TourConfig));
                    break;
                case Tela.Board:
                    if (AlgumModal()) break;
                    if (S.Reputation == 0 && !Visto("quadro_c1") && _rows.Length > 0)
                        Enfileira(Acao("quadro_c1", () => Ui(_rows[0].B), () => Textos.TourC1T, () => Textos.TourC1, () => Current != Tela.Board || AlgumModal()));
                    else if (S.Reputation >= Balance.OpenAfter && !Visto("quadro_novidades"))
                        Enfileira(Explica("quadro_novidades", () => Junta(Ui(_dia.B), Ui(_codeBtn)), () => Textos.TourNovidadesT, () => Textos.TourNovidades));
                    break;
                case Tela.Result:
                    if (AlgumModal()) break;
                    if (!Visto("resultado"))
                        Enfileira(Explica("resultado", () => Junta(Junta(Ui(_resStar[0]), Ui(_resStar[2])), Ui(_resStarText[1])), () => Textos.TourEstrelasT, () => Textos.TourEstrelas));
                    else if (S.GuildOpen && !Visto("menu_guilda") && _resGuild.gameObject.activeInHierarchy)   // o CONTINUAR vai ao quadro: a guilda se apresenta aqui
                        Enfileira(Acao("menu_guilda", () => Ui(_resGuild), () => Textos.TourGuildaT, () => Textos.TourGuilda, () => _guild.gameObject.activeSelf));
                    break;
            }
        }

        void GatilhosJogo()
        {
            Run r = S.Run;
            if (r == null || !r.Running || _pause.gameObject.activeSelf || _settings.gameObject.activeSelf || _ads.Aberto || _lojaTeste.Aberto) return;
            if (_perk.gameObject.activeSelf)
            {
                if (!Visto("perk"))
                    Enfileira(Acao("perk", () => Junta(Ui(_perkCard[0].transform), Ui(_perkCard[_perkCard.Length - 1].transform)), () => Textos.TourPerkT,
                        () => Textos.TourPerk, () => !_perk.gameObject.activeSelf));
                return;
            }
            if (!Visto("jogo_voce"))
            {
                Enfileira(Explica("jogo_voce", () => Mundo(r.Courier.Pos, 2.2f, 2.4f), () => Textos.TourVoceT, () => Textos.TourVoce),
                    new Passo { Id = "jogo_arrastar", Alvo = () => Mundo(r.Courier.Pos, 7f, 3f, 0.2f), Titulo = () => Textos.TourArrastarT, Texto = () => Textos.TourArrastar, Arrastar = true },
                    Explica("jogo_vida", () => Ui(_hearts[0].transform.parent), () => Textos.TourVidaT, () => Textos.TourVida),
                    Explica("jogo_carga", () => Junta(Ui(_cargoEdge), Ui(_pips[2].transform.parent)), () => Textos.TourCargaT, () => Textos.TourCarga),
                    Explica("jogo_pausa", () => Ui(_pauseBtn), () => Textos.TourPausaT, () => Textos.TourPausa));
                return;
            }
            if (r.Hint == Hint.PlantToFight && !Visto("jogo_pousar"))
            {
                Enemy g = Perto(r, e => e.Type == EnemyType.Guard);
                Enfileira(Explica("jogo_pousar", () => Junta(Mundo(r.Courier.Pos, 2f, 2.4f), g != null ? Mundo(g.Pos, 2f, 2.4f) : null), () => Textos.TourPousarT, () => Textos.TourPousar));
                return;
            }
            Enemy ladrao = Perto(r, e => e.Type == EnemyType.Thief);
            if (ladrao != null && !Visto("jogo_ladrao"))
            {
                Enfileira(Explica("jogo_ladrao", () => Mundo(ladrao.Pos, 2.2f, 2.4f), () => Textos.TourLadraoT, () => Textos.TourLadrao));
                return;
            }
            if (r.Hint == Hint.Dash && !Visto("jogo_dash"))
            {
                Enfileira(Explica("jogo_dash", () => Junta(Ui(_dashFill), Ui(_dashIcon)), () => Textos.TourDashT, () => Textos.TourDash));
                return;
            }
            if (r.Hint == Hint.Chest && r.Chest != null && !r.Chest.Opened && !Visto("jogo_bau"))
            {
                Enfileira(Explica("jogo_bau", () => Mundo(r.Chest.Pos, 2f, 2f, 0.3f), () => Textos.TourBauT, () => Textos.TourBau));
                return;
            }
            if (r.Room.Delivery && !Visto("jogo_entrega"))
            {
                Enfileira(Explica("jogo_entrega", () => Mundo(Balance.Client, 2f * Balance.DeliverRange + 0.6f, 2f * Balance.DeliverRange + 0.6f, 0f), () => Textos.TourEntregaT, () => Textos.TourEntrega));
                return;
            }
            if (!r.Room.Delivery && r.Doors.Count > 0 && !Visto("jogo_portas") && !r.Doors.Exists(d => d.Locked) && Perto(r, e => true) == null)
                Enfileira(Explica("jogo_portas", () => Portas(r), () => Textos.TourPortasT, () => Textos.TourPortas));
        }

        /// <summary>O inimigo vivo do tipo pedido (o primeiro), ou null.</summary>
        static Enemy Perto(Run r, Predicate<Enemy> tipo)
        {
            foreach (Enemy e in r.Enemies) if (e.Alive && tipo(e)) return e;
            return null;
        }

        Rect? Portas(Run r)
        {
            Rect? a = null;
            foreach (Door d in r.Doors) a = Junta(a, Mundo(d.Pos, 2f, 2.6f, 1.1f));
            return a;
        }

        static Passo Explica(string id, Func<Rect?> alvo, Func<string> titulo, Func<string> texto) =>
            new Passo { Id = id, Alvo = alvo, Titulo = titulo, Texto = texto };

        static Passo Acao(string id, Func<Rect?> alvo, Func<string> titulo, Func<string> texto, Func<bool> feito) =>
            new Passo { Id = id, Alvo = alvo, Titulo = titulo, Texto = texto, Feito = feito };
    }
}
