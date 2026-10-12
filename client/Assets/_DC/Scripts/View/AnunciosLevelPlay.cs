// Desligado: so compila com o define DC_LEVELPLAY e o pacote com.unity.services.levelplay (docs/MONETIZACAO.md §4, "Religar o LevelPlay").
#if DC_LEVELPLAY
using System;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.UI;

namespace DC
{
    public sealed partial class Hud
    {
        /// <summary>
        /// Anuncios reais pelo LevelPlay (Ads Mediation, CONTRATO_LANCAMENTO §7, docs/MONETIZACAO.md). So existe com DC_LEVELPLAY e so nasce
        /// no Android com "levelplay_app_key" no config.json (Hud.CriaAnuncios); as unidades vem de "levelplay_rewarded" e "levelplay_interstitial" (vazia =
        /// formato desligado). Quem decide QUANDO e o Core (sim.RewardOffer / sim.InterstitialDue, id rw-*); aqui so carrega e mostra.
        /// Recompensado: anuncio pronto = mostra; senao, janela "carregando" ate 8 s e depois "nenhum anuncio agora" (FECHAR = sem
        /// recompensa). fim(true) so com OnAdRewarded (que pode chegar ate 1 s depois do OnAdClosed). Interstitial: so com anuncio carregado
        /// (IntersticialPronto); falhou ao abrir = fecha na hora. Carga falhou: tenta de novo em 30 s.
        /// </summary>
        sealed class AnunciosLevelPlay : IAnuncios
        {
            const float Espera = 8f, Recompensa = 1f, DeNovo = 30f;
            readonly RectTransform _m;
            readonly Text _body;
            readonly string _rwId, _itId;
            LevelPlayRewardedAd _rw;
            LevelPlayInterstitialAd _it;
            Action<bool> _fimR; Action _fimI;
            bool _mostrando, _ganhou;
            float _esperaT = -1f, _fechouT = -1f, _rwDeNovo = -1f, _itDeNovo = -1f;

            public AnunciosLevelPlay(Hud h, string appKey, string rewarded, string interstitial)
            {
                _m = h.Modal("AnuncioEspera", 0.8f);
                Image face = Card(_m, "Cartao", new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.62f));
                Transform f = face.transform;
                _body = Shrink(Ink(f, "Corpo", 38, new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.94f)), 20);
                h.Btn(f, () => Textos.Fechar, 40, Art.PaperEdge, new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.36f), Desiste);
                _rwId = rewarded; _itId = interstitial;
                LevelPlay.OnInitSuccess += Iniciou;
                LevelPlay.OnInitFailed += e => Debug.Log("DC LevelPlay: init falhou: " + e.ErrorMessage);
                LevelPlay.Init(appKey);
            }

            void Iniciou(LevelPlayConfiguration _)
            {
                Debug.Log("DC LevelPlay: iniciado");
                if (!string.IsNullOrEmpty(_rwId))
                {
                    _rw = new LevelPlayRewardedAd(_rwId);
                    _rw.OnAdLoadFailed += e => { Debug.Log("DC LevelPlay rewarded: " + e.ErrorMessage); _rwDeNovo = Time.unscaledTime + DeNovo; };
                    _rw.OnAdRewarded += (i, r) => _ganhou = true;
                    _rw.OnAdClosed += i => _fechouT = Time.unscaledTime;
                    _rw.OnAdDisplayFailed += (i, e) => { Debug.Log("DC LevelPlay rewarded nao abriu: " + e.ErrorMessage); _fechouT = Time.unscaledTime; };
                    _rw.LoadAd();
                }
                if (!string.IsNullOrEmpty(_itId))
                {
                    _it = new LevelPlayInterstitialAd(_itId);
                    _it.OnAdLoadFailed += e => { Debug.Log("DC LevelPlay interstitial: " + e.ErrorMessage); _itDeNovo = Time.unscaledTime + DeNovo; };
                    _it.OnAdClosed += i => FimIntersticial();
                    _it.OnAdDisplayFailed += (i, e) => { Debug.Log("DC LevelPlay interstitial nao abriu: " + e.ErrorMessage); FimIntersticial(); };
                    _it.LoadAd();
                }
            }

            public bool Aberto => _mostrando || _m.gameObject.activeSelf;
            public bool IntersticialPronto => _it != null && !_mostrando && _it.IsAdReady();

            public void Recompensado(string premio, Action<bool> fim)
            {
                if (_mostrando) { fim(false); return; }
                _fimR = fim; _ganhou = false; _fechouT = -1f;
                if (_rw != null && _rw.IsAdReady()) { Mostra(); return; }
                _body.text = Textos.AnuncioCarregando;
                _esperaT = Time.unscaledTime;
                _m.gameObject.SetActive(true);
                _m.SetAsLastSibling();
            }

            public void Intersticial(Action fim)
            {
                if (!IntersticialPronto) { fim(); return; }
                _fimI = fim; _mostrando = true;
                _it.ShowAd();
            }

            void Mostra()
            {
                _m.gameObject.SetActive(false);
                _esperaT = -1f; _mostrando = true;
                _rw.ShowAd();
            }

            public void Tick()
            {
                float t = Time.unscaledTime;
                if (_rwDeNovo >= 0f && t >= _rwDeNovo) { _rwDeNovo = -1f; _rw.LoadAd(); }
                if (_itDeNovo >= 0f && t >= _itDeNovo) { _itDeNovo = -1f; _it.LoadAd(); }
                if (_esperaT >= 0f)
                {
                    if (_rw != null && _rw.IsAdReady()) Mostra();
                    else if (t - _esperaT > Espera) { _esperaT = -1f; _body.text = Textos.AnuncioSemAnuncio; }   // fica ate o FECHAR
                }
                if (_fechouT >= 0f && (_ganhou || t - _fechouT > Recompensa)) FimRecompensado(_ganhou);
            }

            void FimRecompensado(bool ok)
            {
                _fechouT = -1f; _mostrando = false;
                Action<bool> f = _fimR; _fimR = null;
                _rw?.LoadAd();   // o proximo ja vai carregando
                f?.Invoke(ok);
            }

            void FimIntersticial()
            {
                _mostrando = false;
                Action f = _fimI; _fimI = null;
                _it.LoadAd();
                f?.Invoke();
            }

            /// <summary>FECHAR (ou voltar) na janela de espera: sem recompensa.</summary>
            void Desiste()
            {
                if (!_m.gameObject.activeSelf) return;
                _m.gameObject.SetActive(false);
                _esperaT = -1f;
                Action<bool> f = _fimR; _fimR = null;
                f?.Invoke(false);
            }

            public bool Voltar()
            {
                if (_m.gameObject.activeSelf) { Desiste(); return true; }
                return _mostrando;   // o anuncio do SDK trata o voltar dele
            }
        }
    }
}
#endif
