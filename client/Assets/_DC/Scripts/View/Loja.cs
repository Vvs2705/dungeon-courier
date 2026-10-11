using System;
using System.Globalization;
using DC.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DC
{
    /// <summary>
    /// Loja de compras no app (CONTRATO_LANCAMENTO §7): a View pede, a loja confirma e SO entao o Core concede o conteudo fixo
    /// (sim.Purchase). Hoje so ha a loja falsa (Hud.LojaTeste, PC/Editor, sem conta); o Unity IAP entra numa raia propria implementando
    /// esta interface, sem mexer nas telas.
    /// </summary>
    public interface ILoja
    {
        /// <summary>Preco para mostrar (a loja de teste diz "(teste)").</summary>
        string Preco(Product p);
        /// <summary>Abre a compra; fim(true) = compra confirmada pela loja.</summary>
        void Comprar(Product p, Action<bool> fim);
    }

    /// <summary>Tela da loja (parte do Hud): Remover anuncios, Pacote inicial (depois do C3), as 6 roupas (4 a venda; Classica gratis e
    /// Fantasma so por temporada/marco) e o pacote de evento da semana. Cada linha lista o conteudo fixo e o preco "(teste)".</summary>
    public sealed partial class Hud
    {
        ILoja _loja;
        LojaTeste _lojaTeste;
        Painel _pLoja;
        bool _lojaVista;   // o botao LOJA do menu pulsa com o Pacote inicial ate a loja ser aberta na sessao

        /// <summary>Ordem da vitrine; Product = venda, Outfit = so informacao (Classica e Fantasma nao se compram).</summary>
        static readonly object[] Vitrine =
        {
            Product.RemoveAds, Product.StarterPack, Outfit.Classic, Product.OutfitNight, Product.OutfitRoyal, Product.OutfitExplorer,
            Product.OutfitForge, Outfit.Ghost, Product.EventPack,
        };

        void ShowLoja() { _lojaVista = true; Abre(_pLoja); }

        void RefreshLoja(Painel p)
        {
            p.Head.text = Textos.LojaCabeca;
            p.Foot.text = "";
            for (int i = 0; i < Vitrine.Length; i++)
            {
                Linha l = p.Rows[i];
                if (Vitrine[i] is Outfit o)
                {
                    bool own = S.OwnsOutfit(o);
                    Pinta(l, RoupaIcone(o, out Color oc), oc, Textos.RoupaNome(o), Textos.RoupaOrigem(o), null, false, own ? Textos.Comprado : "",
                        own ? Art.Gold : Art.Off, !own, own, own ? (Action)(() => Abre(_pRoupas)) : null);
                    continue;
                }
                var prod = (Product)Vitrine[i];
                bool owns = S.Owns(prod);
                bool avail = !S.SaveRefused && (prod == Product.StarterPack ? S.StarterOffered : prod == Product.EventPack ? S.EventsOpen && S.LastDateSeen != 0 : true);
                string sub = !owns && !avail && prod == Product.StarterPack ? Textos.StarterDepois(S.Config.StarterAfter)
                    : !owns && !avail && prod == Product.EventPack ? Textos.EventoFechado
                    : owns && prod == Product.EventPack ? Textos.PacoteSemana : Textos.ProdutoConteudo(prod);
                Pinta(l, ProdutoIcone(prod, out Color c), c, Textos.ProdutoNome(prod), sub, owns ? null : _loja.Preco(prod), false,
                    owns ? Textos.Comprado : avail ? Textos.Comprar : "", owns ? Art.Gold : avail ? Art.Good : Art.Off, !owns && !avail, owns,
                    !owns && avail ? (Action)(() => Compra(prod)) : null);
            }
            Mostra(p, Vitrine.Length);
        }

        /// <summary>Compra pela loja: confirmada, o Core concede (falso = nada a conceder, ex.: ja tem); cancelada, so o diario.</summary>
        void Compra(Product p) => _loja.Comprar(p, ok =>
        {
            if (!ok) { _g.Log("iap_cancel", ((int)p).ToString(CultureInfo.InvariantCulture)); return; }
            if (S.Purchase(p)) _g.AfterMeta();   // iap_purchase, moedas, roupa, projetos; redesenha a loja aberta
        });

        Sprite ProdutoIcone(Product p, out Color c)
        {
            switch (p)
            {
                case Product.RemoveAds: c = Art.Good; return Art.Check();
                case Product.StarterPack: c = Art.Coin; return Art.Bag();
                case Product.EventPack: c = Art.Gold; return Art.Star();
                case Product.OutfitNight: return RoupaIcone(Outfit.Night, out c);
                case Product.OutfitRoyal: return RoupaIcone(Outfit.Royal, out c);
                case Product.OutfitExplorer: return RoupaIcone(Outfit.Explorer, out c);
                default: return RoupaIcone(Outfit.Forge, out c);
            }
        }

        /// <summary>Loja falsa do PC (sem conta, sem cobranca): janela de confirmacao com o nome, o preco "(teste)" e CONFIRMAR/CANCELAR.
        /// ponytail: precos fixos de mentira ate os produtos existirem na Play Console (raia IAP).</summary>
        sealed class LojaTeste : ILoja
        {
            static readonly string[] Precos = { "9,90", "4,90", "4,90", "4,90", "4,90", "4,90", "2,90" };   // ordem do enum Product
            readonly RectTransform _m;
            readonly Text _body;
            Action<bool> _fim;

            public LojaTeste(Hud h)
            {
                _m = h.Modal("LojaTeste", 0.8f);
                Image face = Card(_m, "Cartao", new Vector2(0.06f, 0.3f), new Vector2(0.94f, 0.7f));
                Transform f = face.transform;
                h.Loc(Ink(f, "Titulo", 50, new Vector2(0.05f, 0.8f), new Vector2(0.95f, 0.96f), TextAnchor.MiddleCenter, true), () => Textos.LojaTeste);
                _body = Shrink(Ink(f, "Corpo", 34, new Vector2(0.06f, 0.33f), new Vector2(0.94f, 0.8f)), 20);
                h.Btn(f, () => Textos.Confirmar, 40, Art.Good, new Vector2(0.05f, 0.05f), new Vector2(0.55f, 0.3f), () => Fim(true), "confirmar");
                h.Btn(f, () => Textos.Cancelar, 40, Art.PaperEdge, new Vector2(0.58f, 0.05f), new Vector2(0.95f, 0.3f), () => Fim(false));
            }

            public bool Aberto => _m.gameObject.activeSelf;
            public string Preco(Product p) => Textos.Preco(Precos[Mathf.Clamp((int)p, 0, Precos.Length - 1)]);

            public void Comprar(Product p, Action<bool> fim)
            {
                _fim = fim;
                _body.text = Textos.ConfirmarCompra(Textos.ProdutoNome(p), Preco(p));
                _m.gameObject.SetActive(true);
                _m.SetAsLastSibling();
            }

            void Fim(bool ok)
            {
                _m.gameObject.SetActive(false);
                Action<bool> f = _fim; _fim = null;
                f?.Invoke(ok);
            }

            /// <summary>Voltar do aparelho = cancelar.</summary>
            public bool Voltar()
            {
                if (!Aberto) return false;
                Fim(false);
                return true;
            }
        }
    }
}
