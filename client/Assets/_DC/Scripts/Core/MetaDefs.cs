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

    /// <summary>Roupas do courier (cosmetico). Classic = a de sempre; Ghost so por temporada/marco; as outras 6 na loja (novas sempre no fim:
    /// o save guarda o indice).</summary>
    public enum Outfit { Classic, Night, Royal, Explorer, Forge, Ghost, Miner, Gravekeeper }

    /// <summary>Produtos de IAP (conteudo fixo e listado; nada aleatorio). O Core so recebe "compra confirmada" (Sim.Purchase). Novos sempre
    /// no fim (o save guarda bits por indice): 6 roupas a venda (Noturno, Real, Explorador, Bombeiro de forja, Mineiro, Coveiro).</summary>
    public enum Product { RemoveAds, StarterPack, OutfitNight, OutfitRoyal, OutfitExplorer, OutfitForge, EventPack, OutfitMiner, OutfitGravekeeper }

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

    /// <summary>Equipamento lateral (CONTEUDO_REGIOES §8): destrava com Balance.GearBlueprints projetos e sobe ate o nivel 5 com Coins (Cost[n] =
    /// preco do nivel n+1); os niveis 6-8 (fim de jogo) abrem depois do Dragao (sim.GearCap). Sem botao (controle de um dedo): dispara sozinho. Fumaca: quando um inimigo pega a carga (usos por contrato por
    /// nivel). Gancho: andando sem carga na direcao da carga do contrato no chao (+-30 graus, 1,5-5 m) por 0,25 s; recarga por nivel.
    /// Armadilha: no 1o pouso da carga em cada sala, 0,6 m do outro lado da carga. Botas: passivas.</summary>
    public sealed class GearDef
    {
        public readonly Gear Type; public readonly string Name, Text; public readonly int[] Cost;
        GearDef(Gear t, string name, string text, int[] cost) { Type = t; Name = name; Text = text; Cost = cost; }
        /// <summary>Indice = (int)Gear (0 = None). Preco dos niveis 2-5 (12.500 por equipamento): o ralo da campanha das regioes 2-4 (BALANCE.md,
        /// "Economia da campanha"; era 600 / 1.200 / 2.000 / 3.000 e sobravam ~24 mil moedas no fim). Niveis 6-8 (fim de jogo, depois do Dragao):
        /// o ralo do pos-campanha (sem fim, eventos, temporada, bau do dia).</summary>
        static readonly int[] Costs = { 0, 1000, 2500, 4000, 5000, 10000, 13000, 18000 };
        /// <summary>Por nivel (indice 0 = nivel 1): fumaca = segundos e usos por contrato; gancho = recarga (s); armadilha = segundos e quantos prende;
        /// botas = intervalo do fogo no pe (x Balance.FireTick; regiao 4). Niveis 6-8: ganho pequeno, nada torna a carga indestrutivel.</summary>
        public static readonly float[] SmokeTime = { 3f, 3.5f, 3.5f, 4f, 4f, 4.5f, 4.5f, 5f }, HookCooldown = { 10f, 9f, 8f, 7f, 6f, 5.5f, 5f, 4.5f },
                                       TrapTime = { 3f, 4f, 4f, 5f, 5f, 5.5f, 6f, 6f }, BootsFire = { 1f, 1f, 1f, 1f, 2f, 2.25f, 2.5f, 3f };
        public static readonly int[] SmokeUses = { 1, 1, 2, 2, 3, 3, 4, 4 }, TrapCatches = { 1, 1, 2, 2, 2, 2, 2, 3 };
        public static readonly GearDef[] All =
        {
            new GearDef(Gear.None, "", "", new[] { 0, 0, 0, 0, 0, 0, 0, 0 }),
            new GearDef(Gear.Smoke, "Bomba de fumaça", "quem pega a carga a solta e a perde de vista por 3 s", Costs),
            new GearDef(Gear.Hook, "Gancho", "andando para a carga no chão (1,5-5 m), ela vem para as mãos", Costs),
            new GearDef(Gear.Trap, "Armadilha de urso", "no 1º pouso da sala: prende o 1º inimigo que passar por 3 s", Costs),
            new GearDef(Gear.Boots, "Botas de lama", "água e poça de limo não freiam (passivo)", Costs),
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
            new OutfitDef(Outfit.Miner, "Mineiro", "loja"),
            new OutfitDef(Outfit.Gravekeeper, "Coveiro", "loja"),
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
        /// <summary>Equipamento lateral: 3 blueprints destravam; nivel 1-5 na campanha (GearCampaignLevel: o nivel 5 tem o efeito extra de cada
        /// peca) e 6-8 (GearMaxLevel) depois do Dragao; abre depois do C6.</summary>
        public const int GearBlueprints = 3, GearMaxLevel = 8, GearCampaignLevel = 5, GearOpenAfter = 6;
        /// <summary>Fumaca: nuvem de 2,5 m. Gancho: 1,5-5 m, cone de 30 graus (cos 0,866), 0,25 s andando. Armadilha: 0,6 m alem da carga, pega a
        /// 0,5 m; no nivel 5 prende tambem mini-chefe por 1,5 s. Projeto da semana (3 vagas do evento): sem projeto faltando = 300 moedas.</summary>
        public const float SmokeRadius = 2.5f, HookMin = 1.5f, HookRange = 5f, HookCone = 0.866f, HookAim = 0.25f, TrapOffset = 0.6f, TrapReach = 0.5f, TrapMini = 1.5f;
        public const int EventProjectCoins = 300;
        // terreno comum (CONTEUDO_REGIOES §1.1)
        /// <summary>Dormindo acorda com o courier a 2 m, explosao a 4 m ou vizinho acordado a 3 m por 1 s; 1o golpe x2.</summary>
        public const float WakeRange = 2f, WakeBlast = 4f, WakeNeighbor = 3f, WakeDelay = 1f; public const int SleepHitMul = 2;
        /// <summary>Agua: quem anda nela vai a x0,75 (nao chefes, nao voadores; Pe firme e Botas ignoram).</summary>
        public const float WaterSlow = 0.75f;
        /// <summary>Luz no escuro (e na Neblina): courier 3,5 m (lanterna nas maos 5,5), tocha 2,5, lanterna no chao 4,0. Tochas padrao da Midnight
        /// em sala sem tocha.</summary>
        public const float LightCourier = 3.5f, LightLantern = 5.5f, LightTorch = 2.5f, LightLanternGround = 4f;
        public static readonly V2[] MidnightTorches = { new V2(0.3f, 5f), new V2(8.7f, 10f) };
        /// <summary>Bau enterrado: 1,5 s parado a 1,2 m (Faro de bau: 1,0 s).</summary>
        public const float BuriedTime = 1.5f, BuriedSenseTime = 1f;
        /// <summary>Boss Courier Run: cura 2 por sala. Fragil: qualquer carga quebra com 2 rachaduras; o gelo falha abaixo de 40%.</summary>
        public const int BossRunHeal = 2, FragileBreak = 2; public const float FragileIce = 0.4f;
        /// <summary>Desafio sem fim: mini-chefe a cada 5 camadas, chefe a cada 10; teto de 800 moedas por dia.</summary>
        public const int EndlessMiniEvery = 5, EndlessBossEvery = 10, EndlessDailyCap = 800;
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
        /// <summary>Cacador: teto por visita de sala, antes da Bolsa (CONTEUDO_REGIOES §7; fecha o farm dos ladroes da carga sozinha e do Ogro).</summary>
        public const int PerkHunterCap = 20;
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
