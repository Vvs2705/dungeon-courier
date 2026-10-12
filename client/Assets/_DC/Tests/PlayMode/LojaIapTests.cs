using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Purchasing;
using UnityEngine.TestTools;

namespace DC.PlayTests
{
    /// <summary>
    /// Caminho do dinheiro (docs/MONETIZACAO.md §3): com o Game de verdade (Hud + LojaIap na loja falsa do Unity IAP), a compra da roupa
    /// Noturno vira pendente -> a LojaIap concede pelo Core -> e so entao confirma; depois a compra volta no FetchPurchases (reinstalar).
    /// So com o editor em -testsession (nao le nem grava o save do PlayerPrefs):
    ///   Unity -batchmode -projectPath client -runTests -testPlatform PlayMode -assemblyNames DC.PlayTests -testsession -testResults r.xml
    /// </summary>
    public class LojaIapTests
    {
        const string Night = "br.com.vstack.dungeoncourier.outfit_night";

        [UnityTest]
        public IEnumerator LojaIap_PendenteConcedePeloCoreEConfirma()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-testsession") < 0) Assert.Ignore("rode com -testsession (protege o save do editor)");
            Game g = null;
            for (int i = 0; i < 600 && (g == null || g.Sim == null); i++) { g = UnityEngine.Object.FindAnyObjectByType<Game>(); yield return null; }
            Assert.IsNotNull(g, "o Game nasceu");
            var s = UnityIAPServices.StoreController("fake");   // a mesma loja falsa da LojaIap (os eventos chegam aos dois)
            Product night = null;
            for (int i = 0; i < 600 && (night = s.GetProductById(Night)) == null; i++) yield return null;
            Assert.IsNotNull(night, "a LojaIap trouxe o catalogo");
            Order conf = null; string erro = null; bool volta = false;
            s.OnPurchaseConfirmed += o => conf = o;
            s.OnPurchaseFailed += f => erro = f.FailureReason + " " + f.Details;
            Assert.IsFalse(g.Sim.Owns(Core.Product.OutfitNight), "ainda nao comprou");
            s.PurchaseProduct(night);   // quem concede e confirma e a LojaIap do jogo
            for (int i = 0; i < 600 && conf == null && erro == null; i++) yield return null;
            Assert.IsNull(erro);
            Assert.IsInstanceOf<ConfirmedOrder>(conf, "a LojaIap confirmou");
            Assert.IsTrue(g.Sim.Owns(Core.Product.OutfitNight) && g.Sim.OwnsOutfit(Core.Outfit.Night), "o Core concedeu antes de confirmar");
            s.OnPurchasesFetched += os => volta = os.ConfirmedOrders.Count > 0;
            s.FetchPurchases();   // o que a LojaIap faz ao abrir e no RESTAURAR da loja falsa
            for (int i = 0; i < 300 && !volta; i++) yield return null;
            Assert.IsTrue(volta, "a compra volta no FetchPurchases");
        }
    }
}
