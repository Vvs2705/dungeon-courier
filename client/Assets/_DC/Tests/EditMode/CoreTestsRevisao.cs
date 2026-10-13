using DC.Core;
using NUnit.Framework;

namespace DC.Tests
{
    /// <summary>Revisao do caminho do dinheiro e dos anuncios (compra, rewarded, interstitial): cada teste prende um bug achado na revisao.</summary>
    public class CoreTestsRevisaoCompra
    {
        [Test]
        public void Iap_RoupaExploradorJaVeioNoStarter_NaoSeVendeDeNovo()
        {
            var sim = new Sim(1) { Reputation = 3 };
            Assert.IsTrue(sim.Purchase(Product.StarterPack));
            Assert.IsTrue(sim.OwnsOutfit(Outfit.Explorer));
            Assert.IsTrue(sim.Owns(Product.OutfitExplorer), "a loja mostra COMPRADO: a roupa ja veio no Pacote inicial");
            Assert.IsFalse(sim.Purchase(Product.OutfitExplorer), "nada novo a conceder");
            Assert.IsTrue(Sim.Load(sim.Save()).Owns(Product.OutfitExplorer));
            var b = new Sim(1) { Reputation = 3 };   // a ordem inversa: o Starter continua valendo (moedas e projetos)
            Assert.IsTrue(b.Purchase(Product.OutfitExplorer)); Assert.IsTrue(b.Purchase(Product.StarterPack));
            Assert.AreEqual(Balance.StarterCoins, b.Coins);
        }

        [Test]
        public void Iap_StarterComEquipamentoJaDestravado_ProjetoQueNaoCabeViraMoedas()
        {
            var sim = new Sim(1) { Reputation = 3 };
            sim.DevProjects(Gear.None, Balance.GearBlueprints);   // fim de jogo: os 4 equipamentos ja tem os 3 projetos
            sim.DevProjects((Gear)1, 1);                          // menos o 1o, que ainda cabe 2
            Assert.IsTrue(sim.Purchase(Product.StarterPack));
            Assert.AreEqual(2, sim.Blueprints((Gear)1), "o que cabe vira projeto");
            Assert.AreEqual(Balance.StarterCoins + 3 * Balance.StarterBlueprintCoins, sim.Coins, "os 3 que nao cabem viram moedas");
        }

        [Test]
        public void Iap_PacoteDeEventoReentregueNaSemanaSeguinte_MesmoPedidoNaoConcedeDeNovo()
        {
            var sim = new Sim(1) { Reputation = Sim.Contracts.Length };
            sim.SeeDate(20261018);   // domingo
            Assert.IsTrue(sim.Purchase(Product.EventPack, "GPA.0001"));
            Assert.IsTrue(sim.PurchaseGranted(Product.EventPack, "GPA.0001"));
            Sim b = Sim.Load(sim.Save());   // o app fechou antes do ConfirmPurchase (ou confirmar falhou sem rede): a loja reentrega o pedido
            b.SeeDate(20261019);            // segunda: semana nova
            Assert.IsFalse(b.Purchase(Product.EventPack, "GPA.0001"), "o mesmo pedido nao concede de novo");
            Assert.AreEqual(Balance.EventContracts, b.EventSlots(20261019));
            Assert.IsTrue(b.PurchaseGranted(Product.EventPack, "GPA.0001"), "ja concedido: a View confirma (consome) o pedido");
            Assert.IsFalse(b.PurchaseGranted(Product.EventPack, "GPA.0002"));
            Assert.IsTrue(b.Purchase(Product.EventPack, "GPA.0002"), "pedido novo na semana nova vale");
            Assert.AreEqual(Balance.EventContracts + Balance.EventPackContracts, b.EventSlots(20261019));
        }

        [Test]
        public void Rewarded_BauDoDia_NaoSaiDaListaDeIdsNoMesmoDia()
        {
            var sim = new Sim(7) { Reputation = Sim.Contracts.Length };
            sim.SeeDate(20261012);
            Assert.IsTrue(sim.GrantReward(Reward.DailyChest));
            for (int i = 0; i < Balance.RewardIds; i++)   // 64 rotas seguras no mesmo dia empurram os ids antigos para fora da lista
            {
                Assert.IsNotNull(sim.StartContract(0, false));
                Assert.IsTrue(sim.GrantReward(Reward.SafeRoute), "rota segura " + i);
                sim.Abandon();
            }
            Assert.IsFalse(sim.RewardOffer(Reward.DailyChest), "o bau de hoje ja foi resgatado");
            Assert.IsFalse(Sim.Load(sim.Save()).GrantReward(Reward.DailyChest), "nem depois de fechar e reabrir");
        }
    }
}
