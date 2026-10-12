using System;
using System.Globalization;
using DC.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DC
{
    /// <summary>
    /// Anuncios (CONTRATO_LANCAMENTO §7). O Core decide QUANDO (sim.RewardOffer, sim.InterstitialDue) e concede (sim.GrantReward); a View
    /// so mostra. Implementacoes: Hud.AnunciosTeste (simulador; o build de hoje) e Hud.AnunciosLevelPlay (SDK LevelPlay; fora do build ate
    /// haver conta: so com o define DC_LEVELPLAY, no Android e com app key no config.json). docs/MONETIZACAO.md.
    /// </summary>
    public interface IAnuncios
    {
        /// <summary>Anuncio (ou a espera dele) na tela.</summary>
        bool Aberto { get; }
        /// <summary>Ha interstitial carregado: sem ele o CONTINUAR segue sem anuncio e o Core nao conta nada.</summary>
        bool IntersticialPronto { get; }
        /// <summary>Anuncio com recompensa: fim(true) SO quando assistido ate o fim (so entao a View chama sim.GrantReward).</summary>
        void Recompensado(string premio, Action<bool> fim);
        /// <summary>Interstitial entre contratos (so quando sim.InterstitialDue deixar); fim ao fechar.</summary>
        void Intersticial(Action fim);
        /// <summary>Todo quadro (o Hud chama): contagem do simulador, espera e fim do anuncio real.</summary>
        void Tick();
        /// <summary>Voltar do aparelho: true = o anuncio tratou.</summary>
        bool Voltar();
    }

    /// <summary>
    /// Ofertas de rewarded e interstitial nas telas (parte do Hud): retomar a sala depois de falha e dobrar o bonus (resultado), rota segura
    /// (pausa), bau do dia (menu, com o conteudo escrito antes de assistir) e o interstitial no CONTINUAR do resultado. Toda oferta mostrada
    /// vai ao diario 1 vez por id (rewarded_offer); a concessao e do Core (rewarded_grant, rw-&lt;contrato&gt;-&lt;tipo&gt; no save).
    /// </summary>
    public sealed partial class Hud
    {
        IAnuncios _ads;
        Reward _resOfferT;
        bool _devOferta;   // -tela oferta/bonus: resultado de exemplo com a oferta forcada (foto)
        string _ofertaLog;

        /// <summary>Anuncio real na tela: o app vai ao fundo, mas nao e o jogador saindo (o Game nao abre sessao nova nem pausa).</summary>
        public bool AnuncioAberto => _ads.Aberto;

        /// <summary>Anuncios do jogo: o simulador. LevelPlay so com o define DC_LEVELPLAY (desligado; o SDK nem vai no build), no Android e com
        /// "levelplay_app_key" no config.json; sem isso nada de rede de anuncio e iniciado.</summary>
        IAnuncios CriaAnuncios()
        {
#if DC_LEVELPLAY
            string key = ConfigValor("levelplay_app_key");
            if (!string.IsNullOrEmpty(key) && Application.platform == RuntimePlatform.Android)
                return new AnunciosLevelPlay(this, key, ConfigValor("levelplay_rewarded"), ConfigValor("levelplay_interstitial"));
#endif
            return new AnunciosTeste(this);
        }

        /// <summary>Diario: oferta mostrada (1 vez por id de concessao).</summary>
        void LogOferta(Reward t)
        {
            string id = S.RewardId(t);
            if (id == null || id == _ofertaLog || _g.BotDrive) return;
            _ofertaLog = id;
            S.Log(Ev.RewardedOffer, (int)t);
            _g.AfterMeta();
        }

        /// <summary>Anuncio com recompensa; assistido ate o fim, o Core concede (falso = oferta ja nao vale) e o Game trata os eventos
        /// (retomar volta ao contrato). Fechado antes: so o diario.</summary>
        void PedeRecompensa(Reward t, string premio, Action depois = null)
        {
            _ads.Recompensado(premio, ok =>
            {
                if (!ok) { _g.Log("rewarded_cancel", ((int)t).ToString(CultureInfo.InvariantCulture)); return; }
                if (S.GrantReward(t)) _g.AfterMeta();
                depois?.Invoke();
            });
        }

        // ------------------------------------------------------------------ resultado: retomar / dobrar o bonus

        void OfertaResultado()
        {
            ContractResult r = _res;
            _resOfferT = r.Delivered ? Reward.DoubleBonus : Reward.Resume;
            bool on = _devOferta || (!_g.BotDrive && S.RewardOffer(_resOfferT));
            _resOffer.gameObject.SetActive(on);
            if (!on) return;
            _resOfferText.text = _resOfferT == Reward.Resume ? Textos.OfertaRetomar : Textos.OfertaDobro(r.Bonus);
            if (!_devOferta) LogOferta(_resOfferT);
        }

        void PedeOfertaResultado()
        {
            Reward t = _resOfferT;
            ContractResult r = _res;
            PedeRecompensa(t, t == Reward.Resume ? Textos.PremioRetomar : Textos.PremioDobro(r.Bonus), () =>
            {
                if (Current == Tela.Result) PintaResultado();   // dobrou: linha nova e a oferta some (retomar ja voltou ao contrato)
            });
        }

        /// <summary>CONTINUAR do resultado (e voltar): o interstitial entra aqui, entre contratos, quando o Core deixar (nunca nos 3 primeiros
        /// do dia, 1 a cada 3 contratos e 4 min, nunca com Remover anuncios nem dentro da masmorra).</summary>
        void Continuar()
        {
            long agora = _g.Agora;
            if (_g.BotDrive || !S.InterstitialDue(agora) || !_ads.IntersticialPronto) { _g.ToBoard(); return; }
            _ads.Intersticial(() =>
            {
                S.InterstitialShown(agora);
                _g.AfterMeta();   // interstitial_show
                _g.ToBoard();
            });
        }

        // ------------------------------------------------------------------ pausa: rota segura; menu: bau do dia

        void PedeRota() => PedeRecompensa(Reward.SafeRoute, Textos.PremioRota, () => { if (_pause.gameObject.activeSelf) PauseText(false); });

        void PedeBau()
        {
            if (!S.RewardOffer(Reward.DailyChest)) return;
            int d = S.LastDateSeen;
            PedeRecompensa(Reward.DailyChest, Textos.PremioBau(Sim.DailyChestCoins(d), S.DailyChestBlueprint(d)));
        }

        // ------------------------------------------------------------------ simulador de anuncio (PC/Editor)

        /// <summary>Simulador de anuncio: tela cheia "ANUNCIO DE TESTE" com contagem de 5 s. Recompensado: PULAR fecha sem recompensa e,
        /// depois dos 5 s, FECHAR concede. Interstitial: so fecha depois dos 5 s. Som "anuncio" ao abrir e ao fechar.</summary>
        sealed class AnunciosTeste : IAnuncios
        {
            const float Duracao = 5f;
            readonly RectTransform _m;
            readonly Text _body, _count, _closeT;
            readonly Image _ring;
            readonly Button _close;
            Action<bool> _fimR; Action _fimI;
            float _t0; bool _rewarded, _done; int _sec = -1;

            public AnunciosTeste(Hud h)
            {
                _m = h.Modal("AnuncioTeste", 0.96f);
                Transform f = _m;
                Text title = h.Loc(Art.Outlined(Art.NewText(f, "Titulo", 70, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.94f)), 3f), () => Textos.AnuncioTeste);
                title.fontStyle = FontStyle.Bold; title.color = Art.Gold;
                Text sdk = h.Loc(Art.NewText(f, "SemSdk", 30, new Vector2(0.05f, 0.795f), new Vector2(0.95f, 0.84f)), () => Textos.AnuncioSemSdk);
                sdk.color = Art.Dim;
                // "arte" do anuncio de mentira: o selo da guilda (nada de marca de terceiros)
                Emblem(f, 0.5f, 0.75f);
                _body = Shrink(Art.NewText(f, "Corpo", 42, new Vector2(0.06f, 0.33f), new Vector2(0.94f, 0.48f)), 22);
                _body.fontStyle = FontStyle.Bold;
                Art.Img(f, "AnelFundo", Art.Ring(), Art.A(Art.Off, 0.9f), new Vector2(0.42f, 0.2f), new Vector2(0.58f, 0.3f));
                _ring = Art.Img(f, "Anel", Art.Arc(0f), Art.Gold, new Vector2(0.42f, 0.2f), new Vector2(0.58f, 0.3f));
                _count = Art.Outlined(Art.NewText(f, "Contagem", 44, new Vector2(0.42f, 0.2f), new Vector2(0.58f, 0.3f)));
                _count.fontStyle = FontStyle.Bold;
                _close = Art.NewButton(f, "", 40, Art.Paper, new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.155f), Fecha);
                _closeT = _close.GetComponentInChildren<Text>();
            }

            public bool Aberto => _m.gameObject.activeSelf;
            public bool IntersticialPronto => true;

            public void Recompensado(string premio, Action<bool> fim)
            {
                _rewarded = true; _fimR = fim; _fimI = null;
                _body.text = Textos.AnuncioGanha(premio);
                Abre();
            }

            public void Intersticial(Action fim)
            {
                _rewarded = false; _fimI = fim; _fimR = null;
                _body.text = Textos.AnuncioEntre;
                Abre();
            }

            void Abre()
            {
                _t0 = Time.unscaledTime; _done = false; _sec = -1;
                _m.gameObject.SetActive(true);
                _m.SetAsLastSibling();
                Audio.Play("anuncio");
                Tick();
            }

            /// <summary>Contagem (o Hud chama todo quadro; so reescreve o texto quando o segundo muda).</summary>
            public void Tick()
            {
                if (!Aberto) return;
                float left = Duracao - (Time.unscaledTime - _t0);
                _ring.sprite = Art.Arc(Mathf.Clamp01(1f - left / Duracao));
                int sec = Mathf.Max(0, Mathf.CeilToInt(left));
                if (sec == _sec) return;
                _sec = sec; _done = sec == 0;
                _count.text = _done ? "" : Textos.Num(sec);
                _close.interactable = _done || _rewarded;
                _closeT.text = _done || !_rewarded ? Textos.Fechar : Textos.PularSemPremio;
                ((Image)_close.targetGraphic).color = _done ? Art.Good : _rewarded ? Art.PaperEdge : Art.Off;
            }

            void Fecha()
            {
                if (!Aberto || (!_done && !_rewarded)) return;
                bool ok = _done;
                _m.gameObject.SetActive(false);
                Audio.Play("anuncio", 0.8f);
                if (_rewarded) { Action<bool> f = _fimR; _fimR = null; f?.Invoke(ok); }
                else { Action f = _fimI; _fimI = null; f?.Invoke(); }
            }

            /// <summary>Voltar do aparelho: fecha como o botao (recompensado antes do fim = sem recompensa; interstitial so depois dos 5 s).</summary>
            public bool Voltar()
            {
                if (!Aberto) return false;
                Fecha();
                return true;
            }
        }
    }
}
