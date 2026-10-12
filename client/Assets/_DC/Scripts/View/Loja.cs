using System;
using System.Globalization;
using System.Text.RegularExpressions;
using DC.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DC
{
    /// <summary>
    /// Loja de compras no app (CONTRATO_LANCAMENTO §7): a View pede, a loja confirma e SO entao o Core concede o conteudo fixo
    /// (sim.Purchase, via Hud.Concede). Implementacoes: Hud.LojaIap (Unity IAP 5: loja falsa no PC/Editor e no build de teste, Google Play
    /// quando o config.json ligar) e Hud.LojaTeste (janela de confirmacao; reserva da loja falsa se o IAP nao conectar). docs/MONETIZACAO.md.
    /// </summary>
    public interface ILoja
    {
        /// <summary>Compras de teste (nada e cobrado): o cabecalho da loja avisa.</summary>
        bool Teste { get; }
        /// <summary>Preco para mostrar: o localizado da loja; nas de teste, "R$ x,xx (teste)".</summary>
        string Preco(Product p);
        /// <summary>Abre a compra; fim(true) = compra confirmada pela loja (a View concede pelo Core).</summary>
        void Comprar(Product p, Action<bool> fim);
        /// <summary>Pede a loja as compras ja feitas nesta conta (cada uma volta como compra confirmada); fim(ok).</summary>
        void Restaurar(Action<bool> fim);
    }

    /// <summary>Tela da loja (parte do Hud): Remover anuncios, Pacote inicial (depois do C3), as 8 roupas (6 a venda; Classica gratis e
    /// Fantasma so por temporada/marco), o pacote de evento da semana e RESTAURAR COMPRAS. Cada linha lista o conteudo fixo e o preco.</summary>
    public sealed partial class Hud
    {
        ILoja _loja;
        LojaTeste _lojaTeste;
        Painel _pLoja;
        bool _lojaVista;   // o botao LOJA do menu pulsa com o Pacote inicial ate a loja ser aberta na sessao

        const string RestaurarLinha = "restaurar";
        /// <summary>Ordem da vitrine; Product = venda, Outfit = so informacao (Classica e Fantasma nao se compram), "restaurar" = ultima linha.</summary>
        static readonly object[] Vitrine =
        {
            Product.RemoveAds, Product.StarterPack, Outfit.Classic, Product.OutfitNight, Product.OutfitRoyal, Product.OutfitExplorer,
            Product.OutfitForge, Product.OutfitMiner, Product.OutfitGravekeeper, Outfit.Ghost, Product.EventPack, RestaurarLinha,
        };

        /// <summary>Loja do jogo: Unity IAP (loja falsa no PC/Editor; no Android tambem, ate o config.json ter "iap_google_play": true).
        /// A LojaTeste so assume se o IAP nao nascer na loja falsa; no Google Play nunca (nada se concede sem a loja).</summary>
        ILoja CriaLoja()
        {
            bool google = Application.platform == RuntimePlatform.Android && ConfigValor("iap_google_play") == "true";
            try { return new LojaIap(this, _lojaTeste, !google); }
            catch (Exception e) when (!google) { Debug.Log("DC IAP indisponivel, loja de teste: " + e.Message); return _lojaTeste; }
        }

        /// <summary>Valor cru de uma chave do Resources/config.json ("" se faltar). ponytail: JSON plano, como o Config.Parse do Core.</summary>
        static string ConfigValor(string chave)
        {
            Match m = Regex.Match(ConfigLocal.Texto(), "\"" + chave + "\"\\s*:\\s*\"?([^\",}]*)");
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }

        void ShowLoja() { _lojaVista = true; Abre(_pLoja); }

        void RefreshLoja(Painel p)
        {
            p.Head.text = _loja.Teste ? Textos.LojaCabeca : Textos.LojaCabecaReal;
            p.Foot.text = "";
            for (int i = 0; i < Vitrine.Length; i++)
            {
                Linha l = p.Rows[i];
                if (Vitrine[i] is string)
                {
                    Pinta(l, Art.Chevrons(), Art.Ink, Textos.Restaurar, Textos.RestaurarSub, null, false, Textos.RestaurarBotao, Art.Paper, false, false, Restaura);
                    continue;
                }
                if (Vitrine[i] is Outfit o)
                {
                    bool own = S.OwnsOutfit(o);
                    Pinta(l, RoupaIcone(o, out Color oc), oc, Textos.RoupaNome(o), Textos.RoupaOrigem(o), null, false, own ? Textos.Comprado : "",
                        own ? Art.Gold : Art.Off, !own, own, own ? (Action)(() => { Fecha(p); Abre(_pRoupas); }) : null);   // as roupas ficam abaixo da loja
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

        /// <summary>Compra pela loja: confirmada, o Core concede; cancelada, so o diario.</summary>
        void Compra(Product p) => _loja.Comprar(p, ok =>
        {
            if (ok) Concede(p);
            else _g.Log("iap_cancel", ((int)p).ToString(CultureInfo.InvariantCulture));
        });

        /// <summary>Compra confirmada pela loja (agora, pendente de outra sessao ou restaurada): o Core concede o conteudo fixo (falso = nada a
        /// conceder, ex.: ja tem) e o Game grava o save (iap_purchase, moedas, roupa, projetos) e redesenha a loja aberta. tx = id do pedido na loja.</summary>
        void Concede(Product p, string tx = null)
        {
            if (S.Purchase(p, tx)) _g.AfterMeta();
        }

        void Restaura() => _loja.Restaurar(ok => AvisoLoja(ok ? Textos.Restaurado : Textos.RestaurarFalhou));

        /// <summary>Recado no rodape da loja aberta (restaurar, compra pendente/falhou).</summary>
        void AvisoLoja(string t)
        {
            if (_pLoja.M.gameObject.activeSelf) _pLoja.Foot.text = t;
        }

        Sprite ProdutoIcone(Product p, out Color c)
        {
            switch (p)
            {
                case Product.RemoveAds: c = Art.Good; return Art.Check();
                case Product.StarterPack: c = Art.Coin; return Art.Bag();
                case Product.EventPack: c = Art.Gold; return Art.Star();
                default: return RoupaIcone(Textos.RoupaDoProduto(p), out c);
            }
        }

        /// <summary>Loja de teste do PC (sem conta, sem cobranca): janela de confirmacao com o nome, o preco "(teste)" e CONFIRMAR/CANCELAR.
        /// A LojaIap usa a mesma janela na loja falsa do Unity IAP. ponytail: precos fixos (a sugestao para a Play Console, MONETIZACAO.md).</summary>
        sealed class LojaTeste : ILoja
        {
            /// <summary>Ordem do enum Product.</summary>
            static readonly string[] Precos = { "9,90", "4,90", "4,90", "4,90", "4,90", "4,90", "2,90", "4,90", "4,90" };
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

            public bool Teste => true;
            public bool Aberto => _m.gameObject.activeSelf;
            public string Preco(Product p) => Textos.Preco(Precos[Mathf.Clamp((int)p, 0, Precos.Length - 1)]);
            public void Comprar(Product p, Action<bool> fim) => Pergunta(p, fim);
            public void Restaurar(Action<bool> fim) => fim(true);   // compras de teste vivem so no save

            /// <summary>"Comprar X por R$ x,xx (teste)?" CONFIRMAR/CANCELAR; fim(true) = confirmou.</summary>
            public void Pergunta(Product p, Action<bool> fim)
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
