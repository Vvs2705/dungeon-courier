using System;
using System.Collections.Generic;
using System.Text;

namespace DC.Core
{
    /// <summary>Sistemas de longo prazo e monetizacao do lancamento (CONTRATO_LANCAMENTO §6-§7). O Core nao le relogio nem fala com SDK:
    /// a View passa a data (AAAAMMDD) e o instante (segundos Unix) e avisa "anuncio assistido" (GrantReward), "interstitial mostrado"
    /// (InterstitialShown) e "compra confirmada" (Purchase). Tudo grava no save v3.</summary>
    public sealed partial class Sim
    {
        /// <summary>Remote config (padroes no build; a View pode trocar: sim.Config = Config.Parse(json)).</summary>
        public Config Config = new Config();

        // ------------------------------------------------------------------ sessao, datas e telemetria

        /// <summary>Sessoes abertas (OpenSession) e o ultimo instante ativo (segundos Unix vindos da View).</summary>
        public int Sessions; public long LastActive;
        /// <summary>1a sessao (o rewarded de retomar nunca aparece nela).</summary>
        public bool FirstSession => Sessions <= 1;

        /// <summary>Chame ao abrir o app (e ao voltar do fundo): conta a sessao, guarda a data e, se faz Config.RareHours h ou mais desde o
        /// ultimo instante ativo, oferece um contrato raro novo (sorteado pela data). Relogio voltando nao oferece nada.</summary>
        public void OpenSession(long now, int date)
        {
            if (SaveRefused) return;
            SeeDate(date);
            Sessions++;
            if (now >= LastActive)
            {
                if (LastActive > 0 && now - LastActive >= Config.RareHours * 3600L && RareOpen && IsDate(date) && date >= LastDateSeen) NewRare(date);
                LastActive = now;
            }
            Dirty = true;
        }

        /// <summary>Chame ao pausar/sair (e de tempos em tempos): o "fim da sessao" para a conta das 8 h do contrato raro.</summary>
        public void Touch(long now)
        {
            if (SaveRefused || now <= LastActive) return;
            LastActive = now;
            Dirty = true;
        }

        /// <summary>Dias desde 2000-01-01 (dia invalido do mes vira o ultimo dia do mes).</summary>
        public static int DayNumber(int date)
        {
            if (!IsDate(date)) return 0;
            int y = date / 10000, m = date / 100 % 100, d = Math.Min(date % 100, DateTime.DaysInMonth(y, m));
            return (int)(new DateTime(y, m, d) - new DateTime(2000, 1, 1)).TotalDays;
        }
        /// <summary>Semana (segunda a domingo, a semana ISO) contada desde 2000: chave dos eventos e do desafio sem fim.</summary>
        public static int Week(int date) => (DayNumber(date) + 5) / 7;
        public static int SeasonOf(int date) => DayNumber(date) / Balance.SeasonDays;
        public static int SeasonDaysLeft(int date) => Balance.SeasonDays - DayNumber(date) % Balance.SeasonDays;

        /// <summary>Evento originado na View que vai para o diario (DeviceFps a cada 60 s, RewardedOffer ao mostrar a oferta).</summary>
        public void Log(Ev kind, int a = 0, int b = 0) => Emit(kind, a, b, V2.Zero);
        /// <summary>Balde do device_fps_bucket: 0 = &lt;20, 1 = 20-29, 2 = 30-44, 3 = 45-54, 4 = 55+.</summary>
        public static int FpsBucket(float fps) => fps < 20f ? 0 : fps < 30f ? 1 : fps < 45f ? 2 : fps < 55f ? 3 : 4;

        // ------------------------------------------------------------------ modificadores (D14)

        public bool ModifiersOpen => Stars >= Balance.ModifiersStars;
        /// <summary>O modificador do contrato do dia (pela data).</summary>
        public static Modifier DailyModifier(int date) => ModifierDef.All[(int)(Rng.Hash(date, 0x30D) % (uint)ModifierDef.All.Length)].Type;
        bool ModsOk(Modifier m) => m == Modifier.None || (ModifiersOpen && ((int)m & ~31) == 0 && ModifierDef.Count(m) <= Balance.MaxModifiers);

        // ------------------------------------------------------------------ maestria e marcos

        readonly int[] mastery = new int[CargoDef.All.Length];
        public int MasteryCount(CargoType t) => mastery[(int)t];
        /// <summary>0-5 pelas entregas (Balance.MasterySteps); +2% de pagamento por nivel naquela carga.</summary>
        public int MasteryLevel(CargoType t) { int n = 0; foreach (int s in Balance.MasterySteps) if (mastery[(int)t] >= s) n++; return n; }

        int milestones;
        public bool MilestoneClaimed(int i) => i >= 0 && i < Balance.Milestones.Length && (milestones & 1 << i) != 0;
        public bool MilestoneReady(int i) => i >= 0 && i < Balance.Milestones.Length && !MilestoneClaimed(i) && Stars >= Balance.Milestones[i] && !SaveRefused;
        /// <summary>Resgata o marco i (10/30/60/95/150 estrelas): moedas fixas (+ roupa Fantasma no de 95).</summary>
        public bool ClaimMilestone(int i)
        {
            if (!MilestoneReady(i)) return false;
            milestones |= 1 << i;
            Coins += Balance.MilestoneCoins[i];
            if (i == Balance.MilestoneOutfit) GiveOutfit(Outfit.Ghost);
            Emit(Ev.Milestone, i, Stars, V2.Zero);
            Emit(Ev.CurrencySource, Balance.MilestoneCoins[i], 9, V2.Zero);
            Dirty = true;
            return true;
        }

        // ------------------------------------------------------------------ quadro de temporada (D30)

        int seasonId = -1, seasonClaimed;
        readonly int[] seasonProg = new int[Season.Goals.Length];
        /// <summary>Temporada em curso (pela maior data vista; -1 = nenhuma data ainda).</summary>
        public int SeasonId => seasonId;
        public int SeasonProgress(int i) => i >= 0 && i < seasonProg.Length ? seasonProg[i] : 0;
        public bool SeasonClaimed(int i) => i >= 0 && i < seasonProg.Length && (seasonClaimed & 1 << i) != 0;
        public bool SeasonReady(int i) => seasonId >= 0 && i >= 0 && i < seasonProg.Length && !SeasonClaimed(i) && seasonProg[i] >= Season.Goals[i].Target && !SaveRefused;

        /// <summary>Resgata o objetivo i da temporada: moedas, blueprint e/ou roupa fixos.</summary>
        public bool ClaimSeason(int i)
        {
            if (!SeasonReady(i)) return false;
            GoalDef g = Season.Goals[i];
            seasonClaimed |= 1 << i;
            if (g.Coins > 0) { Coins += g.Coins; Emit(Ev.CurrencySource, g.Coins, 9, V2.Zero); }
            if (g.Blueprint != Gear.None) AddBlueprint(g.Blueprint, 1);
            if (g.Outfit != Outfit.Classic) GiveOutfit(g.Outfit);
            Emit(Ev.SeasonClaim, i, seasonId, V2.Zero);
            Dirty = true;
            return true;
        }

        /// <summary>Temporada nova (28 dias) zera o progresso; o relogio voltando nao volta a temporada (usa a maior data vista).</summary>
        void SeasonSync()
        {
            if (LastDateSeen == 0) return;
            int s = SeasonOf(LastDateSeen);
            if (s <= seasonId) return;
            seasonId = s; seasonClaimed = 0;
            Array.Clear(seasonProg, 0, seasonProg.Length);
        }

        void SeasonTrack(ContractResult res, Run r)
        {
            SeasonSync();
            if (seasonId < 0) return;
            for (int i = 0; i < Season.Goals.Length; i++)
            {
                GoalDef g = Season.Goals[i];
                int add = GoalAmount(g, res, r);
                if (add <= 0 || seasonProg[i] >= g.Target) continue;
                seasonProg[i] = Math.Min(g.Target, seasonProg[i] + add);
                Emit(Ev.SeasonProgress, i, seasonProg[i], V2.Zero);
            }
        }

        static int GoalAmount(GoalDef g, ContractResult res, Run r)
        {
            bool d = res.Delivered; ContractKind k = res.Kind;
            switch (g.Kind)
            {
                case Goal.Deliver: return d ? 1 : 0;
                case Goal.DeliverCargo: return d && res.Def.Cargo == g.Cargo ? 1 : 0;
                case Goal.Hatched: return d && res.Hatched ? 1 : 0;
                case Goal.Chests: return r.ChestsOpened;
                case Goal.ThreeStars: return res.Stars == 3 ? 1 : 0;
                case Goal.BossClean: return r.BossesClean;
                case Goal.Secrets: return r.SecretsFound;
                case Goal.Daily: return d && k == ContractKind.Daily ? 1 : 0;
                case Goal.Rescue: return res.RescueDelivered ? 1 : 0;
                case Goal.Kills: return r.Kills;
                case Goal.WithModifier: return d && res.Mods != Modifier.None ? 1 : 0;
                case Goal.Event: return d && k == ContractKind.Event ? 1 : 0;
                case Goal.Code: return d && k == ContractKind.Code ? 1 : 0;
                case Goal.Intact: return res.StarIntact ? 1 : 0;
                case Goal.Rare: return d && k == ContractKind.Rare ? 1 : 0;
            }
            return 0;
        }

        // ------------------------------------------------------------------ eventos (5 templates, 1 por semana)

        int evWeek, evMask, evPackWeek = -1;
        /// <summary>Template da semana da data (rodizio dos 5).</summary>
        public static LiveEvent EventOf(int date) => (LiveEvent)(1 + Week(date) % 5);
        public bool EventsOpen => Config.EventsOn && Reputation >= Balance.OpenAfter;
        /// <summary>Vagas do quadro na semana: 3 (+2 com o pacote de evento comprado nesta semana).</summary>
        public int EventSlots(int date) => Balance.EventContracts + (evPackWeek == Week(date) ? Balance.EventPackContracts : 0);
        public bool EventDone(int date, int k) => k >= 0 && k < 31 && evWeek == Week(date) && (evMask & 1 << k) != 0;
        /// <summary>Vaga k jogavel: quadro aberto, data nao anterior a maior vista (relogio), vaga existe e ainda nao entregue nesta semana.</summary>
        public bool EventAvailable(int date, int k) => EventsOpen && !SaveRefused && IsDate(date) && date >= LastDateSeen && Week(date) >= evWeek
                                                       && k >= 0 && k < EventSlots(date) && !EventDone(date, k);
        /// <summary>Recompensa fixa da vaga (moedas; paga 1x por semana na entrega), mostrada antes.</summary>
        public static int EventReward(int k) => k >= 0 && k < Balance.EventRewards.Length ? Balance.EventRewards[k] : 0;

        /// <summary>Contrato da vaga k do quadro da semana (CONTEUDO_REGIOES §9.1): vaga 0 = regiao 1 com 4 camadas; 1 e 2 = a regiao mais alta
        /// destravada com 5 e 6 camadas; do pacote, 3 = regiao sorteada com 5 e 4 = a mais alta com 6.</summary>
        public static int EventLayers(int k) => k == 0 ? 4 : k == 2 || k == 4 ? 6 : 5;

        public ContractDef EventContract(int date, int k)
        {
            if (!IsDate(date) || k < 0 || k >= Balance.EventRewards.Length) return null;
            int w = Week(date);
            int[] open = OpenRegions();
            int region = k == 0 ? 1 : k == 3 ? PickRegion(w * 8 + k, 0xE7E1) : open[open.Length - 1];
            int layers = EventLayers(k);
            int p = Content.WithRegion((int)(Rng.Hash(w, 0xE7E0 + k) & ContractCode.MaxPayload), region);
            ContractDef def = Content.EventContract(EventOf(date), p, date, BeatenBosses(), layers);
            if (def != null) def.Slot = k;
            return def;
        }

        /// <summary>Projeto da semana da data (o premio de completar as 3 vagas do evento; mostre antes no quadro): o equipamento da semana ou,
        /// se ele ja esta completo, outro que ainda falte. Gear.None = nenhum projeto faltando: paga Balance.EventProjectCoins moedas.</summary>
        public Gear WeekProject(int date)
        {
            int w = Week(date);
            var g = (Gear)(1 + w % 4);
            return Blueprints(g) >= Balance.GearBlueprints ? NeededBlueprint((uint)w) : g;
        }

        public Run StartEvent(int date, int k, bool rescue)
        {
            if (InContract || !EventAvailable(date, k)) return null;
            SeeDate(date);
            return Begin(EventContract(date, k), rescue, Modifier.None);
        }

        /// <summary>Salas de mini-chefe (mini = true) ou de chefe ja vencidas, na ordem das regioes (desafio sem fim).</summary>
        internal int[] Beaten(bool mini)
        {
            var l = new List<int>();
            foreach (RegionDef r in RegionDef.All) { int at = mini ? r.MiniAt : r.BossAt; if (at >= 0 && Reputation > at) l.Add(mini ? r.Mini : r.Boss); }
            return l.ToArray();
        }

        /// <summary>Salas de chefe ja vencidas (M e B de cada regiao, na ordem): a Boss Courier Run.</summary>
        internal int[] BeatenBosses()
        {
            var l = new List<int>();
            foreach (RegionDef r in RegionDef.All)
            {
                if (r.MiniAt >= 0 && Reputation > r.MiniAt) l.Add(r.Mini);
                if (r.BossAt >= 0 && Reputation > r.BossAt) l.Add(r.Boss);
            }
            return l.ToArray();
        }

        // ------------------------------------------------------------------ contrato raro e rerolls

        /// <summary>Data do contrato raro oferecido (0 = nenhum) e se ja foi entregue (1x).</summary>
        public int RareDate; public bool RareDone;
        int rarePayload, rrDate, rrDaily, rrRare;
        public bool RareOpen => Reputation >= Balance.OpenAfter;
        public bool RareAvailable => RareDate != 0 && !RareDone && !SaveRefused;
        /// <summary>O contrato raro oferecido (carga e regiao sorteadas pela data; pagamento x2) ou null.</summary>
        public ContractDef RareContract => RareDate == 0 ? null : Content.Generate(rarePayload, ContractKind.Rare, RareDate);

        void NewRare(int date)
        {
            RareDate = date; RareDone = false;
            rarePayload = Content.WithRegion((int)(Rng.Hash(date, 0x7A2E) & ContractCode.MaxPayload), PickRegion(date, 0x7A2F));
            Emit(Ev.RareOffer, date, 0, V2.Zero);
        }

        public Run StartRare(bool rescue)
        {
            if (InContract || !RareAvailable) return null;
            Run r = Begin(RareContract, rescue, Modifier.None);
            if (r != null) Emit(Ev.RareStart, RareDate, 0, Balance.Entrance);
            return r;
        }

        int RerollsToday => rrDate == LastDateSeen ? rrDaily + rrRare : 0;
        /// <summary>Preco do proximo reroll (cresce com os rerolls do dia: base, base + passo).</summary>
        public int RerollCost => Config.RerollBase + Config.RerollStep * RerollsToday;
        /// <summary>Trocar o contrato do dia (rare = false) ou o raro (true): 1x por dia cada, por Coins (fecha "rerolar ate a carga facil").</summary>
        public bool CanReroll(bool rare)
        {
            if (InContract || SaveRefused || LastDateSeen == 0 || Coins < RerollCost) return false;
            return rare ? RareAvailable && !RerolledToday(true) : DailyAvailable(LastDateSeen) && !RerolledToday(false);
        }

        /// <summary>Ja trocou hoje o do dia (rare = false) ou o raro (true)? Separa "ja trocou hoje" de "falta moeda" quando CanReroll e falso.</summary>
        public bool RerolledToday(bool rare) => LastDateSeen != 0 && rrDate == LastDateSeen && (rare ? rrRare : rrDaily) > 0;

        public bool Reroll(bool rare)
        {
            if (!CanReroll(rare)) return false;
            int cost = RerollCost;
            Coins -= cost;
            if (rrDate != LastDateSeen) { rrDate = LastDateSeen; rrDaily = 0; rrRare = 0; }
            if (rare)
            {
                rrRare++;
                rarePayload = Content.WithRegion((int)(Rng.Hash(rarePayload, 0x2E201) & ContractCode.MaxPayload), PickRegion(rarePayload, 0x2E202));
            }
            else rrDaily++;
            Emit(Ev.Reroll, rare ? 1 : 0, cost, V2.Zero);
            Emit(Ev.CurrencySink, cost, rare ? 21 : 20, V2.Zero);
            Dirty = true;
            return true;
        }

        // ------------------------------------------------------------------ equipamento lateral e blueprints

        /// <summary>Equipamento no espaco lateral (None = vazio). Troca so fora do contrato.</summary>
        public Gear GearEquipped;
        readonly int[] blueprints = new int[GearDef.All.Length], gearLv = new int[GearDef.All.Length];
        /// <summary>Baus com blueprint aparecem depois do C6 (Balance.GearOpenAfter).</summary>
        public bool GearOpen => Reputation >= Balance.GearOpenAfter;
        public int Blueprints(Gear g) => g > Gear.None && (int)g < blueprints.Length ? blueprints[(int)g] : 0;
        public bool GearUnlocked(Gear g) => Blueprints(g) >= Balance.GearBlueprints;
        /// <summary>0 = bloqueado; 1-8 (6-8 so depois do Dragao: GearCap).</summary>
        public int GearLevel(Gear g) => GearUnlocked(g) ? Math.Max(1, gearLv[(int)g]) : 0;
        /// <summary>Preco do proximo nivel (-1 = bloqueado ou no maximo).</summary>
        public int GearCost(Gear g) { int lv = GearLevel(g); return lv <= 0 || lv >= GearCap ? -1 : GearDef.All[(int)g].Cost[lv]; }
        /// <summary>Venceu o Dragao (o chefe da regiao 4): abre os niveis 6-8 do equipamento (fim de jogo).</summary>
        public bool DragonBeaten { get { RegionDef r = RegionDef.Of(4); return r != null && r.BossAt >= 0 && Reputation > r.BossAt; } }
        /// <summary>Nivel maximo do equipamento agora: Balance.GearCampaignLevel (5) ate vencer o Dragao, depois Balance.GearMaxLevel (8). A tela
        /// mostra os niveis acima do teto travados ("depois do Dragao").</summary>
        public int GearCap => DragonBeaten ? Balance.GearMaxLevel : Balance.GearCampaignLevel;
        public bool CanUpgradeGear(Gear g) { int c = GearCost(g); return GuildOpen && !InContract && !SaveRefused && c >= 0 && Coins >= c; }

        public bool UpgradeGear(Gear g)
        {
            if (!CanUpgradeGear(g)) return false;
            int cost = GearCost(g);
            Coins -= cost;
            gearLv[(int)g] = GearLevel(g) + 1;
            Emit(Ev.GearUpgrade, (int)g, gearLv[(int)g], V2.Zero);
            Emit(Ev.CurrencySink, cost, 30 + (int)g, V2.Zero);
            Dirty = true;
            return true;
        }

        public bool EquipGear(Gear g)
        {
            if (InContract || SaveRefused || (g != Gear.None && !GearUnlocked(g)) || (int)g < 0 || (int)g >= GearDef.All.Length) return false;
            GearEquipped = g;
            Dirty = true;
            return true;
        }

        /// <summary>SO teste/fotos (como o DevStars): poe `n` projetos (0-3) no equipamento `g` (Gear.None = nos 4), so na memoria: sem evento
        /// e sem Dirty. Com 3 o equipamento destrava (nivel 1, ou o que ja tinha); equipado que perde o projeto sai do espaco. Nao use no jogo normal.</summary>
        public void DevProjects(Gear g, int n)
        {
            for (int i = 1; i < blueprints.Length; i++)
            {
                if (g != Gear.None && i != (int)g) continue;
                blueprints[i] = Math.Max(0, Math.Min(Balance.GearBlueprints, n));
                gearLv[i] = blueprints[i] >= Balance.GearBlueprints ? Math.Max(1, gearLv[i]) : 0;
            }
            if (GearEquipped != Gear.None && !GearUnlocked(GearEquipped)) GearEquipped = Gear.None;
        }

        /// <summary>O equipamento que ainda precisa de blueprint, comecando de h % 4 (None = todos completos).</summary>
        internal Gear NeededBlueprint(uint h)
        {
            for (int i = 0; i < 4; i++) { var g = (Gear)(1 + (int)((h + (uint)i) % 4u)); if (blueprints[(int)g] < Balance.GearBlueprints) return g; }
            return Gear.None;
        }

        readonly HashSet<int> projects = new HashSet<int>();
        /// <summary>O projeto do bau desta sala (RoomDef.Blueprint) ja foi pego (sai 1 vez; a porta deixa de mostrar).</summary>
        public bool ProjectTaken(int room) => projects.Contains(room);
        internal void TakeProject(int room, Gear g) { if (projects.Add(room)) AddBlueprint(g, 1); }

        internal void AddBlueprint(Gear g, int n)
        {
            if (g <= Gear.None || (int)g >= blueprints.Length) return;
            bool was = GearUnlocked(g);
            blueprints[(int)g] = Math.Min(Balance.GearBlueprints, blueprints[(int)g] + n);
            Emit(Ev.BlueprintFound, (int)g, blueprints[(int)g], V2.Zero);
            if (!was && GearUnlocked(g)) { gearLv[(int)g] = 1; Emit(Ev.GearUnlock, (int)g, 0, V2.Zero); }
            Dirty = true;
        }

        // ------------------------------------------------------------------ roupas (cosmetico)

        int outfits = 1;   // Classico sempre
        public Outfit OutfitEquipped;
        public bool OwnsOutfit(Outfit o) => (int)o >= 0 && (int)o < OutfitDef.All.Length && (outfits & 1 << (int)o) != 0;
        public bool EquipOutfit(Outfit o)
        {
            if (SaveRefused || !OwnsOutfit(o)) return false;
            OutfitEquipped = o;
            Emit(Ev.OutfitEquip, (int)o, 0, V2.Zero);
            Dirty = true;
            return true;
        }
        void GiveOutfit(Outfit o) { outfits |= 1 << (int)o; Dirty = true; }

        /// <summary>SO teste/fotos (-testsession -roupa, como o DevStars): da e veste a roupa so na memoria, sem evento e sem Dirty (o teste A/B
        /// de roupa dos criativos). Nao use no jogo normal.</summary>
        public void DevOutfit(Outfit o)
        {
            if ((int)o < 0 || (int)o >= OutfitDef.All.Length) return;
            outfits |= 1 << (int)o;
            OutfitEquipped = o;
        }

        // ------------------------------------------------------------------ IAP (direitos; o SDK fica na View)

        int owned;
        static readonly int ProductCount = Enum.GetValues(typeof(Product)).Length;
        /// <summary>Bits gravados em "iap": todos os produtos menos o pacote de evento (vale pela semana em que foi comprado: evPackWeek).</summary>
        static readonly int OwnedMask = ((1 << ProductCount) - 1) & ~(1 << (int)Product.EventPack);
        /// <summary>Produto nao consumivel comprado (o pacote de evento vale para a semana em que foi comprado).</summary>
        public bool Owns(Product p) => p == Product.EventPack ? LastDateSeen != 0 && evPackWeek == Week(LastDateSeen) : (owned & 1 << (int)p) != 0;
        public bool RemoveAds => Owns(Product.RemoveAds);
        /// <summary>Mostrar a oferta do Starter Pack: depois do 3o contrato e ainda nao comprado.</summary>
        public bool StarterOffered => Reputation >= Config.StarterAfter && !Owns(Product.StarterPack);

        /// <summary>"Compra confirmada" pela loja: concede o conteudo fixo. Nao consumivel comprado de novo (restaurar) nao concede de novo;
        /// o pacote de evento so 1x por semana e com o quadro aberto. Falso = nada concedido.</summary>
        public bool Purchase(Product p)
        {
            if (SaveRefused || (int)p < 0 || (int)p >= ProductCount) return false;
            if (p == Product.EventPack)
            {
                if (LastDateSeen == 0 || !EventsOpen || Owns(p)) return false;
                evPackWeek = Week(LastDateSeen);
            }
            else
            {
                if (Owns(p)) return false;
                owned |= 1 << (int)p;
                switch (p)
                {
                    case Product.StarterPack:
                        Coins += Balance.StarterCoins;
                        Emit(Ev.CurrencySource, Balance.StarterCoins, 5, V2.Zero);
                        GiveOutfit(Outfit.Explorer);
                        for (int g = 1; g < GearDef.All.Length; g++) AddBlueprint((Gear)g, 1);
                        break;
                    case Product.OutfitNight: GiveOutfit(Outfit.Night); break;
                    case Product.OutfitRoyal: GiveOutfit(Outfit.Royal); break;
                    case Product.OutfitExplorer: GiveOutfit(Outfit.Explorer); break;
                    case Product.OutfitForge: GiveOutfit(Outfit.Forge); break;
                    case Product.OutfitMiner: GiveOutfit(Outfit.Miner); break;
                    case Product.OutfitGravekeeper: GiveOutfit(Outfit.Gravekeeper); break;
                }
            }
            Emit(Ev.IapPurchase, (int)p, 0, V2.Zero);
            Dirty = true;
            return true;
        }

        // ------------------------------------------------------------------ rewarded (id de transacao, nao repetivel)

        readonly List<string> rewardIds = new List<string>();
        string checkpoint;

        /// <summary>Guarda o estado do contrato na entrada da sala atual (retomar do rewarded volta para ca).</summary>
        internal void Checkpoint()
        {
            if (Run == null) { checkpoint = null; return; }
            var sb = new StringBuilder();
            WriteRun(sb, Run);
            checkpoint = sb.ToString();
        }

        static string RewardName(Reward t) => t == Reward.Resume ? "resume" : t == Reward.DoubleBonus ? "double" : t == Reward.SafeRoute ? "route" : "chest";

        /// <summary>Id da concessao: rw-&lt;id da tentativa&gt;-&lt;tipo&gt; (bau do dia: rw-&lt;data&gt;-chest). null = sem contrato/data.</summary>
        public string RewardId(Reward t)
        {
            int key;
            if (t == Reward.DailyChest) key = LastDateSeen;
            else if (t == Reward.SafeRoute) key = InContract ? Run.Id : -1;
            else key = LastResult != null ? LastResult.RunId : -1;
            return key <= 0 && t == Reward.DailyChest || key < 0 ? null : "rw-" + key + "-" + RewardName(t);
        }

        public bool RewardGranted(string id) => id != null && rewardIds.Contains(id);

        /// <summary>Pode oferecer este rewarded agora? Retomar: depois de falha (morte, carga perdida/quebrada, tempo, explosao, filhote),
        /// nunca na 1a sessao nem no sem fim. Dobrar bonus: depois de entregar com bonus. Rota segura: no contrato, com 2+ portas. Bau do dia:
        /// 1x por data. Todos 1x por id.</summary>
        public bool RewardOffer(Reward t)
        {
            if (!Config.RewardedOn || SaveRefused) return false;
            string id = RewardId(t);
            if (id == null || RewardGranted(id)) return false;
            ContractResult res = LastResult;
            switch (t)
            {
                case Reward.Resume:
                    return !InContract && !FirstSession && res != null && !res.Delivered && res.Outcome != Outcome.Abandoned && res.Kind != ContractKind.Endless
                           && checkpoint != null && Run != null && Run.Id == res.RunId;
                case Reward.DoubleBonus: return !InContract && res != null && res.Delivered && res.Bonus > 0 && !res.BonusDoubled && res.Kind != ContractKind.Endless;
                case Reward.SafeRoute: return InContract && Run.PerkOffer == null && Run.Doors.Count >= 2 && Run.SafeDoor < 0;
                default: return !InContract;
            }
        }

        /// <summary>"Anuncio assistido ate o fim": concede e grava o id (fechar e reabrir nao repete). Falso = oferta invalida, nada concedido.</summary>
        public bool GrantReward(Reward t)
        {
            if (!RewardOffer(t)) return false;
            string id = RewardId(t);
            int key = t == Reward.DailyChest ? LastDateSeen : t == Reward.SafeRoute ? Run.Id : LastResult.RunId;
            switch (t)
            {
                case Reward.Resume:
                {
                    Run failed = Run;
                    Run nr = LoadRun(this, Parse(checkpoint), SaveVersion);
                    if (nr == null) return false;
                    nr.Courier.Hp = nr.Courier.MaxHp;
                    if (failed.Room.Id == nr.Room.Id)   // bau (e enterrado) aberto na tentativa que falhou continua aberto: nao paga 2x
                    {
                        if (failed.Chest != null && failed.Chest.Opened && nr.Chest != null) nr.Chest.Opened = true;
                        for (int i = 0; i < nr.Buried.Count && i < failed.Buried.Count; i++) nr.Buried[i].Opened |= failed.Buried[i].Opened;
                    }
                    if (nr.HasTimer && nr.TimeLeft < Balance.ResumeMinTime) nr.Time = Math.Max(0f, nr.Contract.TimeLimit - Balance.ResumeMinTime);
                    nr.Kills = 0; nr.ChestsOpened = 0; nr.SecretsFound = 0; nr.BossesClean = 0; nr.HuntCoins = 0;   // ja contados na falha
                    if (Pending != null && Pending.Id == failed.Main.Id) Pending = null;
                    if (adToday > 0) adToday--;
                    if (adSince > 0) adSince--;
                    Run = nr; LastResult = null;
                    Emit(Ev.RewardedResume, nr.Id, nr.Room.Id, Balance.Entrance);
                    break;
                }
                case Reward.DoubleBonus:
                {
                    ContractResult res = LastResult;
                    Coins += res.Bonus; res.Total += res.Bonus; res.BonusDoubled = true;
                    Emit(Ev.CurrencySource, res.Bonus, 8, V2.Zero);
                    break;
                }
                case Reward.SafeRoute: Run.RevealSafe(); break;
                case Reward.DailyChest:
                {
                    int coins = DailyChestCoins(LastDateSeen);
                    Gear bp = DailyChestBlueprint(LastDateSeen);
                    Coins += coins;
                    Emit(Ev.CurrencySource, coins, 8, V2.Zero);
                    if (bp != Gear.None) AddBlueprint(bp, 1);
                    break;
                }
            }
            rewardIds.Add(id);
            while (rewardIds.Count > Balance.RewardIds) rewardIds.RemoveAt(0);
            Emit(Ev.RewardedGrant, (int)t, key, V2.Zero);
            Dirty = true;
            return true;
        }

        /// <summary>Conteudo do bau do dia (visivel antes de assistir): moedas pela data e, com o equipamento aberto, 1 blueprint do que falta.</summary>
        public static int DailyChestCoins(int date) => Balance.DailyChestCoins + 10 * (int)(Rng.Hash(date, 0xC4E5) % 5u);
        public Gear DailyChestBlueprint(int date) => GearOpen ? NeededBlueprint(Rng.Hash(date, 0xC4E6)) : Gear.None;

        // ------------------------------------------------------------------ interstitial

        long adLast; int adSince, adDate, adToday;
        /// <summary>Contratos terminados hoje (qualquer resultado).</summary>
        public int ContractsToday => adDate == LastDateSeen ? adToday : 0;
        /// <summary>Mostrar interstitial agora? So entre contratos, nunca nos 3 primeiros do dia, no maximo 1 a cada 3 contratos e 4 min, nunca
        /// com Remove Ads (Config muda os numeros e pode desligar). Relogio voltando = nao.</summary>
        public bool InterstitialDue(long now) => Config.InterstitialOn && !RemoveAds && !InContract && !SaveRefused && ContractsToday > Config.AdFreeContracts
                                                 && adSince >= Config.AdEveryContracts && (adLast == 0 || now - adLast >= Config.AdEverySeconds);
        /// <summary>A View mostrou o interstitial.</summary>
        public void InterstitialShown(long now)
        {
            if (SaveRefused) return;
            adLast = now; adSince = 0;
            Emit(Ev.InterstitialShow, ContractsToday, 0, V2.Zero);
            Dirty = true;
        }

        // ------------------------------------------------------------------ desafio sem fim (D90)

        /// <summary>Recorde de camadas (geral e da semana EndlessWeek).</summary>
        public int EndlessBest, EndlessWeekBest, EndlessWeek;
        /// <summary>Abre com o ultimo contrato da campanha entregue (com a regiao 4 cheia, o chefe dela).</summary>
        public bool EndlessOpen => Contracts.Length > 0 && Reputation >= Contracts.Length;
        /// <summary>O desafio da semana da data (o mesmo para todos com as mesmas regioes; ShareCode compara).</summary>
        public ContractDef EndlessContract(int date) => Content.Endless(Week(date), OpenRegions(), Beaten(true), Beaten(false));
        int endDay, endToday;
        /// <summary>Moedas que o sem fim ainda paga hoje (teto Balance.EndlessDailyCap por dia, antes da Bolsa).</summary>
        public int EndlessCoinsLeft => Math.Max(0, Balance.EndlessDailyCap - (endDay == LastDateSeen ? endToday : 0));

        public Run StartEndless(int date)
        {
            if (InContract || !EndlessOpen || SaveRefused || !IsDate(date) || date < LastDateSeen) return null;
            SeeDate(date);
            Run r = Begin(EndlessContract(date), false, Modifier.None);
            if (r != null) Emit(Ev.EndlessStart, Week(date), 0, Balance.Entrance);
            return r;
        }

        // ------------------------------------------------------------------ fechamento do contrato (chamado pelo Settle)

        void SettleDelivered(ContractResult res, Run r)
        {
            ContractDef def = r.Contract;
            int before = MasteryLevel(def.Cargo);
            mastery[(int)def.Cargo]++;
            res.Mastery = MasteryLevel(def.Cargo);
            if (res.Mastery > before) Emit(Ev.MasteryUp, (int)def.Cargo, res.Mastery, Balance.Client);
            if (def.Kind == ContractKind.Event && !EventDone(def.Date, def.Slot))
            {
                int w = Week(def.Date);
                if (evWeek != w) { evWeek = w; evMask = 0; }
                int was = evMask;
                evMask |= 1 << def.Slot;
                res.EventReward = EventReward(def.Slot);
                if ((evMask & 7) == 7 && (was & 7) != 7)   // as 3 vagas da semana: o projeto da semana (sem projeto faltando = 300 moedas)
                {
                    Gear g = WeekProject(def.Date);
                    if (g == Gear.None) res.EventReward += Balance.EventProjectCoins; else { AddBlueprint(g, 1); res.EventProject = g; }
                }
                Coins += res.EventReward;
                Emit(Ev.EventReward, res.EventReward, def.Slot, Balance.Client);
                Emit(Ev.CurrencySource, res.EventReward, 6, Balance.Client);
            }
            if (def.Kind == ContractKind.Rare && def.Date == RareDate) RareDone = true;
        }

        void SettleMeta(ContractResult res, Run r)
        {
            if (r.Contract.Kind == ContractKind.Endless)
            {
                int layers = r.Layer;
                res.EndlessLayers = layers;
                int raw = Math.Min(layers * Balance.EndlessCoinsPerLayer, EndlessCoinsLeft);   // teto diario
                if (endDay != LastDateSeen) { endDay = LastDateSeen; endToday = 0; }
                endToday += raw;
                if (raw > 0) { res.EndlessCoins = Credit(raw); Emit(Ev.CurrencySource, res.EndlessCoins, 7, r.Courier.Pos); }
                if (r.Contract.Date != EndlessWeek) { EndlessWeek = r.Contract.Date; EndlessWeekBest = 0; }
                EndlessWeekBest = Math.Max(EndlessWeekBest, layers);
                EndlessBest = Math.Max(EndlessBest, layers);
                Emit(Ev.EndlessEnd, layers, EndlessBest, r.Courier.Pos);
            }
            SeasonTrack(res, r);
            if (adDate != LastDateSeen) { adDate = LastDateSeen; adToday = 0; }
            adToday++; adSince++;
        }

        // ------------------------------------------------------------------ save v3 (linhas novas)

        void SaveMeta(StringBuilder sb)
        {
            sb.Append("ses=").Append(Sessions).Append(',').Append(LastActive).Append('\n');
            sb.Append("rare=").Append(RareDate).Append(',').Append(rarePayload).Append(',').Append(RareDone ? 1 : 0).Append('\n');
            sb.Append("rr=").Append(rrDate).Append(',').Append(rrDaily).Append(',').Append(rrRare).Append('\n');
            sb.Append("ev=").Append(evWeek).Append(',').Append(evMask).Append(',').Append(evPackWeek).Append('\n');
            sb.Append("ssn=").Append(seasonId).Append(',').Append(seasonClaimed).Append(',').Append(string.Join(",", seasonProg)).Append('\n');
            sb.Append("mst=").Append(string.Join(",", mastery)).Append('\n');
            sb.Append("mls=").Append(milestones).Append('\n');
            sb.Append("gear=").Append((int)GearEquipped).Append(',').Append(string.Join(",", blueprints)).Append(',').Append(string.Join(",", gearLv)).Append('\n');
            sb.Append("fit=").Append(outfits).Append(',').Append((int)OutfitEquipped).Append('\n');
            sb.Append("iap=").Append(owned).Append('\n');
            sb.Append("ads=").Append(adLast).Append(',').Append(adSince).Append(',').Append(adDate).Append(',').Append(adToday).Append('\n');
            sb.Append("end=").Append(EndlessBest).Append(',').Append(EndlessWeekBest).Append(',').Append(EndlessWeek).Append(',').Append(endDay).Append(',').Append(endToday).Append('\n');
            if (projects.Count > 0) { var ids = new List<int>(projects); ids.Sort(); sb.Append("bpr=").Append(string.Join(",", ids)).Append('\n'); }
            if (rewardIds.Count > 0) sb.Append("rw=").Append(string.Join(",", rewardIds)).Append('\n');
        }

        void LoadMeta(Dictionary<string, string> kv)
        {
            string[] a = Get(kv, "ses").Split(',');
            Sessions = Math.Max(0, I(a[0], 0)); LastActive = a.Length > 1 ? Math.Max(0L, L(a[1], 0L)) : 0L;
            a = Get(kv, "rare").Split(',');
            if (a.Length >= 3 && IsDate(I(a[0], 0))) { RareDate = I(a[0], 0); rarePayload = I(a[1], 0) & ContractCode.MaxPayload; RareDone = a[2] == "1"; }
            a = Get(kv, "rr").Split(',');
            if (a.Length >= 3 && IsDate(I(a[0], 0))) { rrDate = I(a[0], 0); rrDaily = Clamp(I(a[1], 0), 0, 1); rrRare = Clamp(I(a[2], 0), 0, 1); }
            a = Get(kv, "ev").Split(',');
            if (a.Length >= 3) { evWeek = Math.Max(0, I(a[0], 0)); evMask = Math.Max(0, I(a[1], 0)) & 31; evPackWeek = Math.Max(-1, I(a[2], -1)); }
            a = Get(kv, "ssn").Split(',');
            if (a.Length >= 2 + seasonProg.Length)
            {
                seasonId = Math.Max(-1, I(a[0], -1)); seasonClaimed = Math.Max(0, I(a[1], 0));
                for (int i = 0; i < seasonProg.Length; i++) seasonProg[i] = Clamp(I(a[2 + i], 0), 0, Season.Goals[i].Target);
            }
            SeasonSync();
            a = Get(kv, "mst").Split(',');
            for (int i = 0; i < Math.Min(a.Length, mastery.Length); i++) mastery[i] = Math.Max(0, I(a[i], 0));
            milestones = Math.Max(0, I(Get(kv, "mls"), 0)) & 31;
            a = Get(kv, "gear").Split(',');
            int n = GearDef.All.Length;
            if (a.Length >= 1 + 2 * n)
                for (int i = 1; i < n; i++) { blueprints[i] = Clamp(I(a[1 + i], 0), 0, Balance.GearBlueprints); gearLv[i] = Clamp(I(a[1 + n + i], 0), 0, Balance.GearMaxLevel); }
            int ge = I(a[0], 0);
            GearEquipped = ge > 0 && ge < n && GearUnlocked((Gear)ge) ? (Gear)ge : Gear.None;
            a = Get(kv, "fit").Split(',');
            outfits = (Math.Max(0, I(a[0], 1)) | 1) & ((1 << OutfitDef.All.Length) - 1);
            int fe = a.Length > 1 ? I(a[1], 0) : 0;
            OutfitEquipped = fe >= 0 && fe < OutfitDef.All.Length && OwnsOutfit((Outfit)fe) ? (Outfit)fe : Outfit.Classic;
            owned = Math.Max(0, I(Get(kv, "iap"), 0)) & OwnedMask;
            a = Get(kv, "ads").Split(',');
            if (a.Length >= 4) { adLast = Math.Max(0L, L(a[0], 0L)); adSince = Math.Max(0, I(a[1], 0)); adDate = I(a[2], 0); adToday = Math.Max(0, I(a[3], 0)); }
            a = Get(kv, "end").Split(',');
            if (a.Length >= 5) { EndlessBest = Math.Max(0, I(a[0], 0)); EndlessWeekBest = Math.Max(0, I(a[1], 0)); EndlessWeek = Math.Max(0, I(a[2], 0)); endDay = I(a[3], 0); endToday = Math.Max(0, I(a[4], 0)); }
            foreach (string id in Get(kv, "bpr").Split(',')) { int r = I(id, -1); if (r >= 0 && r < Content.Rooms.Length && Content.Rooms[r].Blueprint != Gear.None) projects.Add(r); }
            foreach (string id in Get(kv, "rw").Split(',')) if (id.StartsWith("rw-", StringComparison.Ordinal) && rewardIds.Count < Balance.RewardIds) rewardIds.Add(id);
        }
    }
}
