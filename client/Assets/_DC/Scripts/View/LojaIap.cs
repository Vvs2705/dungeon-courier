using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DC.Core;
using UnityEngine;
using Iap = UnityEngine.Purchasing;

namespace DC
{
    public sealed partial class Hud
    {
        /// <summary>
        /// Loja real com Unity IAP 5 (CONTRATO_LANCAMENTO §7, docs/MONETIZACAO.md). Catalogo = enum Product do Core, ids
        /// br.com.vstack.dungeoncourier.&lt;produto&gt; (pacote de evento consumivel; o resto nao consumivel). Compra em 2 passos: PurchaseProduct ->
        /// OnPurchasePending -> o Core concede e o save grava (Hud.Concede) -> ConfirmPurchase SO se o Core ficou com o produto (senao a loja
        /// devolve o dinheiro sozinha). Pendente de outra sessao e restaurar caem no mesmo caminho; ao conectar, as compras que a loja ja tem
        /// voltam (reinstalar devolve roupas e Remover anuncios).
        /// Loja falsa do Unity IAP (FakeAppStore, sem janela propria: define IAP_FAKE_STORE_DEFAULT no Setup) no PC/Editor e no Android enquanto
        /// o config.json nao tiver "iap_google_play": true: a confirmacao e a janela da LojaTeste e o preco e o de teste "(teste)" (a loja falsa
        /// diz $0.01 para tudo). Se o IAP nao conectar na loja falsa, a LojaTeste assume (reserva); no Google Play o produto fica indisponivel.
        /// </summary>
        sealed class LojaIap : ILoja
        {
            const string Prefixo = "br.com.vstack.dungeoncourier.";
            /// <summary>Sufixo do id na ordem do enum Product. NUNCA mudar um id depois de cadastrado na Play Console.</summary>
            static readonly string[] Ids = { "remove_ads", "starter_pack", "outfit_night", "outfit_royal", "outfit_explorer", "outfit_forge",
                "event_pack", "outfit_miner", "outfit_gravekeeper" };

            readonly Hud _h;
            readonly LojaTeste _teste;
            readonly bool _falsa;
            readonly Iap.StoreController _store;
            bool _pronta;
            Product _pedido; Action<bool> _fim;

            public static string Id(Product p) => Prefixo + Ids[(int)p];

            public LojaIap(Hud h, LojaTeste teste, bool falsa)
            {
                if (Ids.Length != Enum.GetValues(typeof(Product)).Length) throw new InvalidOperationException("LojaIap.Ids fora do enum Product");
                _h = h; _teste = teste; _falsa = falsa;
                _store = Iap.UnityIAPServices.StoreController(falsa ? "fake" : null);   // "fake" = FakeAppStore.Name (internal no pacote)
                // todos os eventos antes do Connect: pendente de outra sessao pode chegar logo
                _store.OnStoreConnected += Conectou;
                _store.OnStoreDisconnected += f => Debug.Log("DC IAP desconectada: " + f.Message);
                _store.OnProductsFetched += Produtos;
                _store.OnProductsFetchFailed += f => Debug.Log("DC IAP produtos falharam: " + f.FailureReason);
                _store.OnPurchasesFetched += Compras;
                _store.OnPurchasesFetchFailed += f => Debug.Log("DC IAP compras falharam: " + f.Message);
                _store.OnPurchasePending += Pendente;
                _store.OnPurchaseConfirmed += o => { if (o is Iap.FailedOrder f) Debug.Log("DC IAP confirmacao falhou: " + f.FailureReason + " " + f.Details); };
                _store.OnPurchaseFailed += Falhou;
                _store.OnPurchaseDeferred += _ => { _h.AvisoLoja(Textos.CompraPendente); _fim = null; };   // aprovacao pendente: chega depois como pendente
                Conecta();
            }

            /// <summary>Conecta no quadro seguinte: o Hud termina de nascer antes de qualquer evento da loja (a loja falsa responde na hora).</summary>
            async void Conecta()
            {
                try { await Task.Yield(); await _store.Connect(); }
                catch (Exception e) { Debug.Log("DC IAP sem conexao: " + e.Message); }
            }

            void Conectou()
            {
                var defs = new List<Iap.ProductDefinition>();
                for (int i = 0; i < Ids.Length; i++)
                    defs.Add(new Iap.ProductDefinition(Id((Product)i), (Product)i == Product.EventPack ? Iap.ProductType.Consumable : Iap.ProductType.NonConsumable));
                _store.FetchProducts(defs);
            }

            void Produtos(List<Iap.Product> lista)
            {
                _pronta = true;
                Debug.Log("DC IAP: " + lista.Count + " produtos (" + (_falsa ? "loja falsa" : "Google Play") + ")");
                _store.FetchPurchases();   // reinstalou / outro aparelho: as compras voltam (Compras) e as pendentes caem no Pendente
                _h.RefreshPaineis();       // precos da loja na loja e nas roupas abertas
            }

            void Compras(Iap.Orders os)
            {
                foreach (Iap.ConfirmedOrder o in os.ConfirmedOrders)
                    if (DoPedido(o) is Product p && p != Product.EventPack) _h.Concede(p);   // ja tem = nada (o Core nao concede 2x)
            }

            public bool Teste => _falsa;

            public string Preco(Product p)
            {
                if (_falsa) return _teste.Preco(p);
                Iap.Product ip = _pronta ? _store.GetProductById(Id(p)) : null;
                return !_pronta ? Textos.LojaCarregando : ip != null && ip.availableToPurchase ? ip.metadata.localizedPriceString : Textos.LojaIndisponivel;
            }

            public void Comprar(Product p, Action<bool> fim)
            {
                if (_falsa && !_pronta) { _teste.Comprar(p, fim); return; }   // reserva: o IAP nao conectou na loja falsa
                if (_fim != null) return;                                     // ja ha compra em andamento
                Iap.Product ip = _pronta ? _store.GetProductById(Id(p)) : null;
                if (ip == null || !ip.availableToPurchase) { _h.AvisoLoja(Textos.LojaIndisponivel); fim(false); return; }
                _pedido = p; _fim = fim;
                if (_falsa) _teste.Pergunta(p, ok => { if (ok) _store.PurchaseProduct(ip); else Fim(false); });
                else _store.PurchaseProduct(ip);
            }

            public void Restaurar(Action<bool> fim)
            {
                if (!_pronta) { fim(_falsa); return; }
                // a loja falsa nao tem RestoreTransactions (o pacote responde "plataforma sem restaurar"): buscar as compras faz o mesmo
                if (_falsa) { _store.FetchPurchases(); fim(true); return; }
                _store.RestoreTransactions((ok, erro) =>
                {
                    if (!ok) Debug.Log("DC IAP restaurar: " + erro);
                    fim(ok);
                });
            }

            /// <summary>Passo 1 confirmado pela loja: concede pelo Core (o save grava) e SO entao confirma (passo 2).</summary>
            void Pendente(Iap.PendingOrder o)
            {
                if (!(DoPedido(o) is Product p)) { Debug.Log("DC IAP: pedido fora do catalogo, sem confirmar"); return; }
                string tx = o.Info?.TransactionID;
                _h.Concede(p, tx);                             // com o id do pedido: o pacote de evento reentregue noutra semana nao concede 2x
                if (_fim != null && _pedido == p) Fim(true);   // a compra desta tela (o Concede da View ja nao tem o que conceder)
                if (_h.S.PurchaseGranted(p, tx)) _store.ConfirmPurchase(o);
                else Debug.Log("DC IAP: " + Id(p) + " nao concedido pelo Core; sem confirmar");
            }

            void Falhou(Iap.FailedOrder f)
            {
                if (f.FailureReason != Iap.PurchaseFailureReason.UserCancelled) _h.AvisoLoja(Textos.CompraFalhou);
                Debug.Log("DC IAP compra falhou: " + f.FailureReason + " " + f.Details);
                Fim(false);
            }

            void Fim(bool ok)
            {
                Action<bool> f = _fim; _fim = null;
                f?.Invoke(ok);
            }

            static Product? DoPedido(Iap.Order o)
            {
                string id = o.CartOrdered?.Items().FirstOrDefault()?.Product?.definition?.id;
                int i = id != null && id.StartsWith(Prefixo, StringComparison.Ordinal) ? Array.IndexOf(Ids, id.Substring(Prefixo.Length)) : -1;
                return i < 0 ? (Product?)null : (Product)i;
            }
        }
    }
}
