using System;
using System.Globalization;

// Definicoes dos sistemas do lancamento (CONTRATO_LANCAMENTO §6-§7). Numeros sao hipoteses (PROVISORIOS ate o bot/playtest medir);
// a logica fica em Meta.cs (partial de Sim). Eventos novos (Ev) e o A/B de cada um:
//   SecondWind (Segundo folego salvou) · GearUse A=(int)Gear B=nivel · GearUnlock A=(int)Gear · GearUpgrade A=(int)Gear B=nivel ·
//   BlueprintFound A=(int)Gear B=blueprints · MasteryUp A=(int)CargoType B=nivel · Milestone A=indice B=estrelas ·
//   ModifierPick A=(int)Modifier (mascara) B=contrato · EventStart A=(int)LiveEvent B=vaga · EventEnd A=(int)LiveEvent B=(int)Outcome ·
//   EventReward A=moedas B=vaga · SeasonProgress A=objetivo B=progresso · SeasonClaim A=objetivo B=temporada · RareOffer A=data ·
//   RareStart A=data · Reroll A=0 dia/1 raro B=preco · EndlessStart A=semana · EndlessEnd A=camadas B=recorde ·
//   RewardedOffer A=(int)Reward (a View emite com sim.Log ao mostrar) · RewardedGrant A=(int)Reward B=id do contrato ou data ·
//   RewardedResume A=id do contrato B=sala · InterstitialShow A=contratos hoje · IapPurchase A=(int)Product ·
//   DeviceFps A=balde (Sim.FpsBucket; a View emite a cada 60 s com sim.Log) · OutfitEquip A=(int)Outfit.
//   CurrencySource B novos: 4 = Cacador, 5 = compra (Starter Pack), 6 = evento, 7 = sem fim, 8 = rewarded, 9 = marco/temporada.
//   CurrencySink B novos: 20 = reroll do dia, 21 = reroll do raro, 30 + (int)Gear = nivel de equipamento.

namespace DC.Core
{
    /// <summary>Template de evento semanal (GDD §9): rodizio pela semana (segunda a domingo).</summary>
    public enum LiveEvent { None, FragileWeek, Midnight, NoDamage, Treasure, BossRun }

    /// <summary>Modificadores (GDD §5 D14): 0-2 por contrato, cada um soma pagamento.</summary>
    [Flags] public enum Modifier { None = 0, Fog = 1, Rush = 2, Horde = 4, Fragile = 8, Stingy = 16 }

    /// <summary>Equipamento lateral (1 espaco alem da arma).</summary>
    public enum Gear { None, Smoke, Hook, Trap, Boots }

    /// <summary>Roupas do courier (cosmetico). Classic = a de sempre; Ghost so por temporada/marco.</summary>
    public enum Outfit { Classic, Night, Royal, Explorer, Forge, Ghost }

    /// <summary>Produtos de IAP (conteudo fixo e listado; nada aleatorio). O Core so recebe "compra confirmada" (Sim.Purchase).</summary>
    public enum Product { RemoveAds, StarterPack, OutfitNight, OutfitRoyal, OutfitExplorer, OutfitForge, EventPack }

    /// <summary>Recompensas de rewarded (id de transacao rw-&lt;contrato&gt;-&lt;tipo&gt;, gravado e nao repetivel).</summary>
    public enum Reward { Resume, DoubleBonus, SafeRoute, DailyChest }

    /// <summary>Tipo de objetivo do quadro de temporada.</summary>
    public enum Goal { Deliver, DeliverCargo, Hatched, Chests, ThreeStars, BossClean, Secrets, Daily, Rescue, Kills, WithModifier, Event, Code, Intact, Rare }

    public sealed class ModifierDef
    {
        public readonly Modifier Type; public readonly string Name, Text; public readonly int Pay;
        ModifierDef(Modifier t, string name, string text, int pay) { Type = t; Name = name; Text = text; Pay = pay; }
        /// <summary>Indice = bit (Fog = 0 ... Stingy = 4). Pay = +% no pagamento. PROVISORIO.</summary>
        public static readonly ModifierDef[] All =
        {
            new ModifierDef(Modifier.Fog, "Neblina", "visão curta", 15),
            new ModifierDef(Modifier.Rush, "Pressa", "tempo-alvo -30%", 20),
            new ModifierDef(Modifier.Horde, "Horda", "+1 inimigo por sala", 25),
            new ModifierDef(Modifier.Fragile, "Frágil", "a carga quebra com 2 rachaduras em vez de 3", 25),
            new ModifierDef(Modifier.Stingy, "Pão-duro", "baús -50%", 20),
        };
        public static int Count(Modifier m) { int n = 0; for (int i = 0; i < All.Length; i++) if ((m & All[i].Type) != 0) n++; return n; }
        public static int PayOf(Modifier m) { int p = 0; foreach (ModifierDef d in All) if ((m & d.Type) != 0) p += d.Pay; return p; }
    }

    public sealed class EventDef
    {
        public readonly LiveEvent Type; public readonly string Name, Text; public readonly int PayPct;
        EventDef(LiveEvent t, string name, string text, int pct) { Type = t; Name = name; Text = text; PayPct = pct; }
        /// <summary>Indice = (int)LiveEvent (0 = None). PayPct = multiplicador do pagamento em %.</summary>
        public static readonly EventDef[] All =
        {
            new EventDef(LiveEvent.None, "", "", 100),
            new EventDef(LiveEvent.FragileWeek, "Semana Frágil", "só cargas frágeis; +50% de pagamento", 150),
            new EventDef(LiveEvent.Midnight, "Entrega da Meia-Noite", "todas as salas escuras; lanterna obrigatória", 100),
            new EventDef(LiveEvent.NoDamage, "Contrato Sem Dano", "1 de vida; pagamento x3", 300),
            new EventDef(LiveEvent.Treasure, "Desvio do Tesouro", "baús dobrados e paredes rachadas abertas", 100),
            new EventDef(LiveEvent.BossRun, "Corrida dos Chefes", "os chefes já vencidos em sequência", 100),
        };
    }

    /// <summary>Equipamento lateral: destrava com Balance.GearBlueprints blueprints e sobe de nivel com Coins (Cost[n] = preco do nivel n+1).</summary>
    public sealed class GearDef
    {
        public readonly Gear Type; public readonly string Name, Text; public readonly int[] Cost;
        GearDef(Gear t, string name, string text, int[] cost) { Type = t; Name = name; Text = text; Cost = cost; }
        /// <summary>Indice = (int)Gear (0 = None).</summary>
        public static readonly GearDef[] All =
        {
            new GearDef(Gear.None, "", "", new[] { 0, 0, 0 }),
            new GearDef(Gear.Smoke, "Bomba de fumaça", "ladrões perdem a carga de vista por 3 s", new[] { 0, 350, 800 }),
            new GearDef(Gear.Hook, "Gancho", "puxa a carga de até 5 m para as mãos", new[] { 0, 350, 800 }),
            new GearDef(Gear.Trap, "Armadilha de urso", "prende o inimigo mais perto por 3 s", new[] { 0, 350, 800 }),
            new GearDef(Gear.Boots, "Botas de lama", "lama e água não freiam (passivo)", new[] { 0, 350, 800 }),
        };
    }

    public sealed class OutfitDef
    {
        public readonly Outfit Type; public readonly string Name, Source;
        OutfitDef(Outfit t, string name, string source) { Type = t; Name = name; Source = source; }
        public static readonly OutfitDef[] All =
        {
            new OutfitDef(Outfit.Classic, "Clássico", "inicial"),
            new OutfitDef(Outfit.Night, "Noturno", "loja"),
            new OutfitDef(Outfit.Royal, "Real", "loja"),
            new OutfitDef(Outfit.Explorer, "Explorador", "loja / Starter Pack"),
            new OutfitDef(Outfit.Forge, "Bombeiro de forja", "loja"),
            new OutfitDef(Outfit.Ghost, "Fantasma", "temporada / marco de 95 estrelas"),
        };
    }

    /// <summary>Objetivo do quadro de temporada: Target vezes; recompensa fixa (moedas, blueprint e/ou roupa).</summary>
    public sealed class GoalDef
    {
        public readonly Goal Kind; public readonly CargoType Cargo; public readonly int Target, Coins; public readonly Gear Blueprint; public readonly Outfit Outfit;
        public readonly string Text;
        public GoalDef(Goal kind, int target, int coins, string text, CargoType cargo = CargoType.Box, Gear blueprint = Gear.None, Outfit outfit = Outfit.Classic)
        { Kind = kind; Target = target; Coins = coins; Text = text; Cargo = cargo; Blueprint = blueprint; Outfit = outfit; }
    }

    /// <summary>Quadro de temporada (GDD §5 D30): 28 dias pela data, 20 objetivos com recompensa fixa, sem passe pago. PROVISORIO.</summary>
    public static class Season
    {
        public static readonly GoalDef[] Goals =
        {
            new GoalDef(Goal.Deliver, 5, 100, "entregue 5 contratos"),
            new GoalDef(Goal.Chests, 10, 100, "abra 10 baús"),
            new GoalDef(Goal.DeliverCargo, 3, 120, "entregue 3 relíquias", CargoType.Relic),
            new GoalDef(Goal.Kills, 30, 120, "derrote 30 inimigos"),
            new GoalDef(Goal.Hatched, 5, 150, "entregue 5 ovos chocados"),
            new GoalDef(Goal.ThreeStars, 5, 150, "faça 3 estrelas em 5 contratos"),
            new GoalDef(Goal.Daily, 3, 150, "complete 3 contratos do dia"),
            new GoalDef(Goal.Secrets, 2, 0, "ache 2 salas secretas", blueprint: Gear.Hook),
            new GoalDef(Goal.Intact, 5, 150, "entregue 5 cargas intactas"),
            new GoalDef(Goal.Rescue, 2, 0, "resgate 2 cargas perdidas", blueprint: Gear.Smoke),
            new GoalDef(Goal.DeliverCargo, 3, 150, "entregue 3 barris de pólvora", CargoType.Barrel),
            new GoalDef(Goal.DeliverCargo, 2, 150, "entregue 2 mensagens urgentes", CargoType.Message),
            new GoalDef(Goal.BossClean, 1, 250, "vença um chefe sem dano"),
            new GoalDef(Goal.WithModifier, 1, 0, "entregue um contrato com modificador", blueprint: Gear.Trap),
            new GoalDef(Goal.Event, 2, 200, "entregue 2 contratos de evento"),
            new GoalDef(Goal.Code, 1, 0, "entregue um contrato por código", blueprint: Gear.Boots),
            new GoalDef(Goal.Deliver, 15, 300, "entregue 15 contratos"),
            new GoalDef(Goal.Kills, 100, 300, "derrote 100 inimigos"),
            new GoalDef(Goal.Rare, 1, 250, "entregue um contrato raro"),
            new GoalDef(Goal.Deliver, 25, 200, "entregue 25 contratos", outfit: Outfit.Ghost),
        };
    }

    public static partial class Balance
    {
        // ---- lancamento (CONTRATO_LANCAMENTO §6-§7). PROVISORIOS: o bot/playtest mede e o BALANCE.md registra.
        /// <summary>Modificadores destravam com 40 estrelas; ate 2 por contrato. Neblina: visao de 3,5 m (View). Pressa: tempo-alvo x0,7.
        /// Pao-duro: baus x0,5.</summary>
        public const int ModifiersStars = 40, MaxModifiers = 2, StingyChestPct = 50;
        public const float RushPar = 0.7f, FogRadius = 3.5f;
        /// <summary>Eventos: 3 contratos por semana (+2 com o pacote de evento); recompensa fixa por vaga (paga 1x por semana).</summary>
        public const int EventContracts = 3, EventPackContracts = 2, TreasureChestPct = 200;
        public static readonly int[] EventRewards = { 150, 200, 300, 150, 150 };
        public const int SeasonDays = 28;
        /// <summary>Contrato raro: pagamento x2 (o intervalo de 8 h esta no Config).</summary>
        public const int RarePct = 200;
        /// <summary>Equipamento lateral: 3 blueprints destravam; nivel 1-3; abre depois do C6. Efeito +0,5 s/m por nivel.</summary>
        public const int GearBlueprints = 3, GearMaxLevel = 3, GearOpenAfter = 6;
        public const float SmokeTime = 3f, HookRange = 5f, TrapTime = 3f, GearPerLevel = 0.5f, TrapRange = 4f;
        /// <summary>Maestria por carga: entregas para os niveis 1-5; +2% de pagamento por nivel naquela carga.</summary>
        public static readonly int[] MasterySteps = { 3, 8, 15, 25, 40 };
        public const int MasteryPct = 2;
        /// <summary>Marcos de reputacao (estrelas) e o premio (moedas; o de 95 da a roupa Fantasma). Resgatados pelo jogador.</summary>
        public static readonly int[] Milestones = { 10, 30, 60, 95, 150 }, MilestoneCoins = { 50, 150, 300, 500, 1000 };
        public const int MilestoneOutfit = 3;
        /// <summary>Desafio sem fim: 100 camadas (na pratica infinito), nivel 10 + 2 por camada, 15 moedas por camada vencida.</summary>
        public const int EndlessLayers = 100, EndlessTierBase = 10, EndlessTierPerLayer = 2, EndlessCoinsPerLayer = 15;
        // perks novos (§5)
        public const float PerkSpring = 1.4f, PerkInsulatedEgg = 1.2f, PerkInsulatedMelt = 0.6f, PerkWhistle = 1.5f, PerkRunner = 1.2f, PerkCatEyes = 1.6f;
        public const int PerkSecondWindHp = 2, PerkHunterCoins = 2, PerkFirstAid = 1;
        // monetizacao (§7)
        /// <summary>Starter Pack: moedas fixas + roupa Explorador + 1 blueprint de cada equipamento. Bau do dia (rewarded): 80-120 moedas
        /// pela data (+ 1 blueprint se o equipamento estiver aberto). Retomar: a mensagem volta com pelo menos 20 s.</summary>
        public const int StarterCoins = 1500, DailyChestCoins = 80, ResumeMinTime = 20, RewardIds = 64;
    }

    /// <summary>Remote config local (CONTRATO_LANCAMENTO §6.9): padroes no build (Resources/config.json) e, opcional, uma URL lida pela
    /// View. O Core so recebe os valores: sim.Config = Config.Parse(texto). Chave desconhecida ou valor invalido = padrao.</summary>
    public sealed class Config
    {
        public bool InterstitialOn = true, RewardedOn = true, EventsOn = true;
        /// <summary>Interstitial: nunca nos AdFreeContracts primeiros contratos do dia; no maximo 1 a cada AdEveryContracts contratos e
        /// AdEverySeconds s.</summary>
        public int AdFreeContracts = 3, AdEveryContracts = 3, AdEverySeconds = 240;
        public int RareHours = 8, RerollBase = 60, RerollStep = 60, StarterAfter = 3;

        /// <summary>JSON plano {"chave": valor, ...} (numeros, true/false). ponytail: parser so de objeto plano; troque por um de verdade
        /// se o config ganhar listas ou objetos.</summary>
        public static Config Parse(string json)
        {
            var c = new Config();
            if (string.IsNullOrEmpty(json)) return c;
            foreach (string part in json.Trim().Trim('{', '}').Split(','))
            {
                int k = part.IndexOf(':');
                if (k < 0) continue;
                string key = part.Substring(0, k).Trim().Trim('"'), val = part.Substring(k + 1).Trim().Trim('"');
                bool b = val == "true";
                bool isInt = int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 0;
                switch (key)
                {
                    case "interstitial_on": if (b || val == "false") c.InterstitialOn = b; break;
                    case "rewarded_on": if (b || val == "false") c.RewardedOn = b; break;
                    case "events_on": if (b || val == "false") c.EventsOn = b; break;
                    case "ad_free_contracts": if (isInt) c.AdFreeContracts = n; break;
                    case "ad_every_contracts": if (isInt) c.AdEveryContracts = n; break;
                    case "ad_every_seconds": if (isInt) c.AdEverySeconds = n; break;
                    case "rare_hours": if (isInt) c.RareHours = n; break;
                    case "reroll_base": if (isInt) c.RerollBase = n; break;
                    case "reroll_step": if (isInt) c.RerollStep = n; break;
                    case "starter_after": if (isInt) c.StarterAfter = n; break;
                }
            }
            return c;
        }
    }
}
