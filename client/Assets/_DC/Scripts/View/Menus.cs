using DC.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DC
{
    /// <summary>
    /// Telas fora do contrato (parte do Hud, CONTRATO_V1 §11 + CONTRATO_LANCAMENTO §6-§7): titulo (nome desenhado com tipografia, sem
    /// imagem), menu (bau do dia, Jogar e a grade Guilda/Loja, Temporada/Marcos, Roupas/Configuracoes, Creditos), quadro de contratos (regiao
    /// escolhida no mapa, contrato do dia e contrato raro com TROCAR, Eventos, Sem fim, Codigo, lista da campanha da regiao com estrelas,
    /// cadeado e selo de maestria), entrada de codigo (teclado do aparelho), guilda com abas Melhorias/Armas/Equipamento, escolha de perk (1
    /// de 3, cartoes grandes), configuracoes (Som, Musica, Vibracao, Idioma, Exportar diario, versao) e creditos. Nada aqui roda por quadro,
    /// exceto o piscar do titulo e o pulso da guilda/loja (sem alocar).
    /// Dev (fotos 540x960 sem clicar): -tela titulo|menu|quadro|codigo|guilda|armas|config|creditos|perk e, do lancamento, mapa|raro|
    /// modificadores|eventos|temporada|equipamento|marcos|maestria|semfim|roupas|loja|compra|anuncio|intersticial|oferta|bonus|bau.
    /// </summary>
    public sealed partial class Hud
    {
        const float RowH = 176f, RowGap = 12f;   // linha do quadro em px de referencia (>= 144 = 48 dp)

        // titulo e menu
        Text _tapText, _menuCoins, _menuStars, _menuNotice;
        Button _menuGuild, _menuLoja;
        Image _menuGuildLock;
        Especial _bau;

        // quadro
        sealed class Row { public Button B; public CanvasGroup G; public Image Badge, Lock, Seal; public Text BadgeText, SealText; public readonly Image[] Stars = new Image[3]; }
        /// <summary>Cartao destacado do quadro/menu (contrato do dia, contrato raro, bau do dia): faixa, carga/icone, linha, selo de estado.</summary>
        sealed class Especial { public Button B; public CanvasGroup G; public Image Icon, Edge, Badge, Lock, Check; public Text Title, Line, BadgeT; }
        /// <summary>Botao TROCAR (reroll 1x por dia) com o preco em moedas.</summary>
        sealed class Troca { public Button B; public Text Cost; public Image Coin; }
        Row[] _rows;
        Text _boardCoins, _boardStars, _notice, _regionT;
        ScrollRect _scroll;
        RectTransform _scrollContent;
        Button _codeBtn, _evBtn, _sfBtn;
        Image _codeLock, _evLock, _sfBtnLock;
        Especial _dia, _raro;
        Troca _diaTroca, _raroTroca;
        /// <summary>Regiao mostrada no quadro (0 = a do proximo contrato da campanha); o mapa troca.</summary>
        int _boardRegion;

        // codigo
        InputField _codeField;
        Text _codeError;

        // guilda (aba 0 melhorias, 1 armas, 2 equipamento lateral)
        sealed class UpRow { public Text Level, Cost; public Image[] Dots; public Button Buy; public Text BuyText; public Image CostCoin; }
        sealed class WRow { public CanvasGroup G; public Image Lock, Check, UpCoin; public Image[] Dots; public Text Level, Need, Cost, EquipText, UpText; public Button Equip, Up; }
        UpRow[] _ups;
        WRow[] _weps;
        WRow[] _gears;
        Text _guildCoins, _guildStars;
        RectTransform _guildUps, _guildWeps, _guildGear;
        Button _tabUps, _tabWeps, _tabGear;
        Image _tabUpsLine, _tabWepsLine, _tabGearLine;
        int _guildTab;

        // perk
        readonly Image[] _perkIcon = new Image[3];
        readonly Text[] _perkName = new Text[3], _perkEff = new Text[3];
        readonly GameObject[] _perkCard = new GameObject[3];

        // configuracoes
        Text _volSomT, _volMusT, _exportStatus, _versionT;
        readonly Image[] _barSom = new Image[Ajustes.VolMax], _barMus = new Image[Ajustes.VolMax];
        Button _vibraBtn;
        readonly Button[] _langBtn = new Button[4];   // ordem de Textos.Lingua: Português, English / Español, Bahasa Indonesia
        readonly Image[] _langCheck = new Image[4];

        // ------------------------------------------------------------------ mostrar

        public void ShowMenu()
        {
            Current = Tela.Menu; Only(_menu); _g.SeeDate(); RefreshMenu(); Audio.Music("menu");
            if (_bau.B.gameObject.activeInHierarchy && S.RewardOffer(Reward.DailyChest)) LogOferta(Reward.DailyChest);
        }

        public void ShowBoard()
        {
            Current = Tela.Board; Only(_board);
            Audio.Music("menu");
            _g.SeeDate();
            RefreshBoard();
            RolaQuadro();
        }

        /// <summary>Rola a lista da regiao do quadro ate o proximo contrato da campanha (a 1/3 do topo).</summary>
        void RolaQuadro()
        {
            Canvas.ForceUpdateCanvases();
            float view = _scroll.viewport.rect.height, step = RowH + RowGap;
            int reg = BoardRegion(), next = 0;
            for (int i = 0; i < _rows.Length && i < S.Reputation; i++) if (Sim.Contracts[i].Region == reg) next++;
            _scrollContent.anchoredPosition = new Vector2(0f, Mathf.Clamp(next * step - view * 0.35f, 0f, Mathf.Max(0f, _scrollContent.sizeDelta.y - view)));
            _scroll.velocity = Vector2.zero;
        }

        /// <summary>Regiao do quadro: a escolhida no mapa (se aberta) ou a do proximo contrato da campanha (se aberta) ou a 1.</summary>
        int BoardRegion()
        {
            if (_boardRegion > 0 && S.RegionOpen(_boardRegion)) return _boardRegion;
            int r = Sim.Contracts.Length > 0 ? Sim.Contracts[Mathf.Clamp(S.Reputation, 0, Sim.Contracts.Length - 1)].Region : 1;
            return S.RegionOpen(r) ? r : 1;
        }

        void ShowGuild(int tab) { _guildTab = tab; RefreshGuild(); _guild.gameObject.SetActive(true); }

        void ShowCode()
        {
            _codeError.text = ""; _codeField.text = "";
            _code.gameObject.SetActive(true);
            _codeField.ActivateInputField();   // abre o teclado do aparelho
        }

        void ShowSettings() { _exportStatus.text = ""; RefreshSettings(); _settings.gameObject.SetActive(true); }
        void ShowCredits() => _credits.gameObject.SetActive(true);

        void ShowPerk(Perk[] offer)
        {
            for (int i = 0; i < _perkCard.Length; i++)
            {
                bool on = i < offer.Length;
                _perkCard[i].SetActive(on);
                if (!on) continue;
                Perk p = offer[i];
                _perkIcon[i].sprite = PerkIcon(p, out Color c); _perkIcon[i].color = c;
                _perkName[i].text = Textos.PerkNome(p);
                _perkEff[i].text = Textos.PerkEfeito(p);
            }
            _perk.gameObject.SetActive(true);
            if (S.Run != null)   // diario: oferta mostrada (taxa de escolha por perk no diario_report)
            {
                var ids = new string[offer.Length];
                for (int i = 0; i < offer.Length; i++) ids[i] = ((int)offer[i]).ToString(System.Globalization.CultureInfo.InvariantCulture);
                _g.Log("perk_offer", string.Join("|", ids), "", S.Run.Contract.Code, S.Run.Room.Code);
            }
        }

        /// <summary>Moedas, estrelas e travas mudaram (compra, equipar, fim de contrato, resgate de meta): redesenha menu, quadro, guilda e
        /// os paineis de meta abertos.</summary>
        public void RefreshMeta() { RefreshMenu(); RefreshBoard(); RefreshGuild(); RefreshPaineis(); }

        /// <summary>-tela X (dev): abre a tela para a foto. As de meta pedem o -dev-desbloqueia para mostrar tudo aberto.</summary>
        public void Dev(string tela)
        {
            int next = Mathf.Clamp(S.Reputation, 0, Sim.Contracts.Length - 1);
            switch (tela)
            {
                case "titulo": ShowTitle(); break;
                case "menu": case "bau": ShowMenu(); break;
                case "quadro": case "raro": ShowBoard(); break;
                case "codigo": ShowBoard(); ShowCode(); break;
                case "guilda": ShowMenu(); ShowGuild(0); break;
                case "armas": ShowMenu(); ShowGuild(1); break;
                case "equipamento": ShowMenu(); ShowGuild(2); break;
                case "config": ShowMenu(); ShowSettings(); break;
                case "creditos": ShowMenu(); ShowCredits(); break;
                case "perk": ShowPerk(new[] { Perk.DoubleDash, Perk.Magnet, Perk.IronBoot }); break;   // o -bot escolhe sozinho: foto so por aqui
                // lancamento
                case "mapa": ShowBoard(); Abre(_pMapa); break;
                case "modificadores": ShowBoard(); ShowMods(Sim.Contracts[next].Code, m => _g.Play(next, m)); break;
                case "eventos": ShowBoard(); ShowEventos(); break;
                case "semfim": ShowBoard(); ShowSemFim(); break;
                case "temporada": ShowMenu(); Abre(_pTemporada); break;
                case "marcos": ShowMenu(); _pMarcos.Tab = 0; Abre(_pMarcos); break;
                case "maestria": ShowMenu(); _pMarcos.Tab = 1; Abre(_pMarcos); break;
                case "roupas": ShowMenu(); Abre(_pRoupas); break;
                case "loja": ShowMenu(); ShowLoja(); break;
                case "compra": ShowMenu(); ShowLoja(); Compra(Product.StarterPack); break;
                case "anuncio": ShowMenu(); _ads.Recompensado(Textos.PremioBau(Sim.DailyChestCoins(_g.Hoje), S.DailyChestBlueprint(_g.Hoje)), _ => { }); break;
                case "intersticial": ShowMenu(); _ads.Intersticial(_ => { }); break;
                case "oferta": DevResultado(false); break;
                case "cena": ShowEnd(ResultadoDev(true)); break;   // cena da entrega (bau abrindo)
                case "bonus": DevResultado(true); break;
            }
        }

        /// <summary>-tela oferta|bonus: resultado de exemplo (falha na 2a sala / entrega com bonus) com a oferta de rewarded forcada, so para a
        /// foto (o Core so oferece de verdade depois de um contrato jogado).</summary>
        void DevResultado(bool entregue)
        {
            _res = ResultadoDev(entregue);
            _devOferta = true;
            ShowResult();
        }

        ContractResult ResultadoDev(bool entregue)
        {
            ContractDef c = Sim.Contracts[Mathf.Clamp(S.Reputation, 0, Sim.Contracts.Length - 1)];
            var r = new ContractResult
            {
                Def = c, Contract = c.Index, Kind = c.Kind, Time = 74.3f, ChestCoins = 18, RunId = 1, Base = c.Pay, CargoMul = 1f, IntegrityMul = 1.2f, BagMul = 1f,
                Outcome = entregue ? Outcome.Delivered : Outcome.Died, DeathRoom = c.Layers.Length > 1 ? c.Layers[1][0] : -1,
                StarDelivered = entregue, Stars = entregue ? 1 : 0,
            };
            if (entregue) { r.Payment = (int)(c.Pay * 1.2f); r.Bonus = r.Payment - c.Pay; }
            r.Total = r.Payment + r.ChestCoins;
            return r;
        }

        // ------------------------------------------------------------------ atualizar

        void TickTitle() => _tapText.color = Art.A(Art.Ink, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f)));

        /// <summary>Pulso do menu: GUILDA quando algo cabe no bolso (melhoria, arma, equipamento); LOJA com o Pacote inicial recem-oferecido
        /// (ate a loja ser aberta na sessao).</summary>
        void PulseGuild()
        {
            float pulse = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 6f);
            bool shop = S.StarterOffered && !_lojaVista && !S.SaveRefused;
            _menuLoja.transform.localScale = Vector3.one * (shop ? pulse : 1f);
            if (!_menuGuild.interactable) return;
            bool any = false;
            for (int u = 0; u < Balance.UpgradeLines && !any; u++) any = S.CanBuy((Upgrade)u);
            for (int w = 0; w < WeaponDef.All.Length && !any; w++) any = S.CanUpgradeWeapon((Weapon)w);
            for (int g = 1; g < GearDef.All.Length && !any; g++) any = S.CanUpgradeGear((Gear)g);
            _menuGuild.transform.localScale = Vector3.one * (any ? pulse : 1f);
        }

        void RefreshMenu()
        {
            if (_menuCoins == null) return;
            _menuCoins.text = Textos.Num(S.Coins);
            _menuStars.text = Textos.Estrelas(S.Stars, Sim.Contracts.Length * 3);
            bool open = S.GuildOpen && !S.SaveRefused;
            _menuGuild.interactable = open;
            _menuGuildLock.enabled = !open;
            if (!open) _menuGuild.transform.localScale = Vector3.one;
            _menuNotice.text = S.SaveRefused ? Textos.SaveRecusado : S.Pending != null ? Textos.Pendente(S.Pending) : !open ? Textos.GuildaFechada : "";
            _menuNotice.color = S.SaveRefused ? Art.Bad : S.Pending != null ? Art.Cargo : Art.Dim;
            // bau do dia (rewarded): o conteudo aparece ANTES de assistir; some sem anuncio com recompensa (remote config) ou save recusado
            bool chest = S.Config.RewardedOn && !S.SaveRefused && S.LastDateSeen != 0;
            _bau.G.gameObject.SetActive(chest);
            if (!chest) return;
            bool avail = S.RewardOffer(Reward.DailyChest);
            int d = S.LastDateSeen;
            _bau.Title.text = Textos.BauDia;
            _bau.Line.text = avail ? Textos.PremioBau(Sim.DailyChestCoins(d), S.DailyChestBlueprint(d)) : Textos.BauVolte;
            PintaEspecial(_bau, avail, true, avail ? Textos.VerAnuncio : Textos.Aberto, !avail);
        }

        public void RefreshBoard()
        {
            if (_rows == null) return;
            _boardCoins.text = Textos.Num(S.Coins);
            _boardStars.text = Textos.Estrelas(S.Stars, _rows.Length * 3);
            if (S.SaveRefused) { _notice.text = Textos.SaveRecusado; _notice.color = Art.Bad; }
            else if (S.Pending != null) { _notice.text = Textos.Pendente(S.Pending); _notice.color = Art.Cargo; }
            else _notice.text = "";
            int reg = BoardRegion(), shown = 0;
            _regionT.text = Textos.NomeRegiao(reg);
            for (int i = 0; i < _rows.Length; i++)
            {
                Row row = _rows[i];
                var holder = (RectTransform)row.G.transform;
                bool here = Sim.Contracts[i].Region == reg;
                holder.gameObject.SetActive(here);
                if (!here) continue;
                holder.anchoredPosition = new Vector2(0f, -shown++ * (RowH + RowGap));
                bool open = S.Unlocked(i) && !S.SaveRefused;
                row.B.interactable = open;
                row.G.alpha = open ? 1f : 0.55f;
                bool done = i < S.Reputation;
                row.Lock.enabled = !open;
                row.BadgeText.enabled = open;
                row.BadgeText.text = done ? Textos.Repetir : Textos.Jogar;
                row.Badge.color = !open ? Art.Off : done ? Art.Gold : Art.Good;
                int st = S.StarsOf(i);
                for (int k = 0; k < 3; k++)
                {
                    row.Stars[k].sprite = k < st ? Art.Star() : Art.StarHollow();
                    row.Stars[k].color = k < st ? Art.Gold : Art.A(Art.Tinta, 0.4f);
                }
                int lv = S.MasteryLevel(Sim.Contracts[i].Cargo);   // selo de maestria da carga (cosmetico, CONTRATO_LANCAMENTO §6.7)
                row.Seal.gameObject.SetActive(lv > 0);
                if (lv > 0) row.SealText.text = Textos.Num(lv);
            }
            _scrollContent.sizeDelta = new Vector2(0f, shown * (RowH + RowGap));
            RefreshEspeciais();
            bool code = S.CodeOpen && !S.SaveRefused;
            _codeBtn.interactable = code;
            _codeLock.enabled = !code;
            bool ev = S.EventsOpen && !S.SaveRefused;
            _evLock.enabled = !ev;
            ((Image)_evBtn.targetGraphic).color = ev ? Art.Paper : Art.PaperEdge;
            bool sf = S.EndlessOpen && !S.SaveRefused;
            _sfBtnLock.enabled = !sf;
            ((Image)_sfBtn.targetGraphic).color = sf ? Art.Paper : Art.PaperEdge;
        }

        /// <summary>Cartoes do contrato do dia (DailyFor: regiao sorteada entre as abertas, com o modificador do dia) e do contrato raro, com
        /// o TROCAR de cada um (1x por dia, preco visivel).</summary>
        void RefreshEspeciais()
        {
            int hoje = _g.Hoje;
            bool open = S.DailyOpen && !S.SaveRefused, avail = open && S.DailyAvailable(hoje);
            ContractDef d = open ? S.DailyFor(hoje) : null;
            bool regions = S.OpenRegions().Length > 1;
            _dia.Title.text = d != null && regions ? Textos.ComRegiao(Textos.Dia, d.Region) : Textos.Dia;
            Carga(_dia, d);
            string mod = null; int pct = 0;
            if (d != null && S.ModifiersOpen)
            {
                Modifier m = Sim.DailyModifier(hoje);
                mod = Textos.ModNome(m); pct = ModifierDef.PayOf(m);
            }
            _dia.Line.text = !open ? Textos.DiaFechado : !avail ? Textos.DiaFeito : d == null ? Textos.EmBreve
                : mod != null ? Textos.DiaLinhaMod(Textos.Carga(d.Cargo), mod, pct, Balance.DailyBonus) : Textos.DiaLinha(Textos.Carga(d.Cargo), Balance.DailyBonus);
            PintaEspecial(_dia, avail && d != null, open, avail ? Textos.Jogar : Textos.Feito, !avail && open);
            PintaTroca(_diaTroca, false, avail);

            bool rOpen = S.RareOpen && !S.SaveRefused, rAvail = rOpen && S.RareAvailable;
            ContractDef r = rAvail ? S.RareContract : null;
            _raro.Title.text = r != null && regions ? Textos.ComRegiao(Textos.Raro, r.Region) : Textos.Raro;
            Carga(_raro, r);
            _raro.Line.text = !rOpen ? Textos.DiaFechado : r != null ? Textos.Carga(r.Cargo) + " · " + Textos.Verbo(r)
                : S.RareDone ? Textos.RaroFeito(S.Config.RareHours) : Textos.RaroEspera(S.Config.RareHours);
            PintaEspecial(_raro, r != null, rOpen, r != null ? Textos.Jogar : S.RareDone ? Textos.Feito : "", S.RareDone && r == null);
            PintaTroca(_raroTroca, true, r != null);
        }

        static void Carga(Especial e, ContractDef c)
        {
            e.Icon.enabled = e.Edge.enabled = c != null;
            if (c == null) return;
            e.Icon.sprite = CargaIcone(c.Cargo, out Color cor);
            e.Icon.color = cor;
            e.Edge.sprite = Art.CargoSprite(c.Cargo);
            e.Edge.enabled = cor != Color.white;   // o quadro renderizado ja tem contorno proprio
        }

        /// <summary>Estado do cartao: jogavel (verde), feito (dourado + visto), fechado (cadeado, apagado) ou so aviso.</summary>
        static void PintaEspecial(Especial e, bool play, bool open, string badge, bool done)
        {
            e.B.interactable = play;
            e.G.alpha = open ? 1f : 0.55f;
            e.Lock.enabled = !open;
            e.Check.enabled = open && done;
            e.BadgeT.enabled = open && !string.IsNullOrEmpty(badge);
            e.BadgeT.text = badge ?? "";
            e.BadgeT.rectTransform.anchorMin = new Vector2(done ? 0.3f : 0f, 0f);
            e.Badge.color = !open ? Art.Off : play ? Art.Good : done ? Art.Gold : Art.PaperEdge;
        }

        void PintaTroca(Troca t, bool rare, bool show)
        {
            t.B.gameObject.SetActive(show);
            if (!show) return;
            bool can = S.CanReroll(rare), done = S.RerolledToday(rare);
            t.B.interactable = can;
            t.Coin.enabled = !done;
            t.Cost.text = done ? Textos.TrocaAmanha : Textos.Num(S.RerollCost);   // ja trocou hoje: so amanha
            t.Cost.color = !done && S.Coins < S.RerollCost ? Art.Bad : Art.Tinta;   // falta moeda: preco em vermelho
            ((Image)t.B.targetGraphic).color = can ? Art.Paper : Art.Off;
        }

        void Reroll(bool rare)
        {
            if (S.Reroll(rare)) _g.AfterMeta();   // reroll, currency_sink; o cartao mostra o contrato novo
        }

        void RefreshGuild()
        {
            if (_ups == null) return;
            _guildCoins.text = Textos.Num(S.Coins);
            _guildStars.text = Textos.Num(S.Stars);
            _guildUps.gameObject.SetActive(_guildTab == 0);
            _guildWeps.gameObject.SetActive(_guildTab == 1);
            _guildGear.gameObject.SetActive(_guildTab == 2);
            Tab(_tabUps, _tabUpsLine, _guildTab == 0);
            Tab(_tabWeps, _tabWepsLine, _guildTab == 1);
            Tab(_tabGear, _tabGearLine, _guildTab == 2);
            for (int i = 0; i < _gears.Length; i++)
            {
                WRow row = _gears[i];
                var g = (Gear)(i + 1);
                bool un = S.GearUnlocked(g), eq = S.GearEquipped == g;
                int bp = S.Blueprints(g), lv = S.GearLevel(g), cost = S.GearCost(g), cap = S.GearCap;
                bool dragon = un && cost < 0 && lv >= cap && cap < Balance.GearMaxLevel;   // no 5: os niveis 6-8 abrem com o Dragao
                row.G.alpha = un ? 1f : 0.6f;
                row.Lock.enabled = !un;
                // travado: os 3 projetos (cheio = achado); destravado: os 8 niveis, os acima do teto (6-8 antes do Dragao) quase apagados
                for (int k = 0; k < row.Dots.Length; k++)
                {
                    row.Dots[k].enabled = un || k < Balance.GearBlueprints;
                    row.Dots[k].color = k < (un ? lv : bp) ? Art.Courier : un && k >= cap ? Art.A(Art.Tinta, 0.08f) : Art.A(Art.Tinta, 0.25f);
                }
                row.Level.text = un ? Textos.ArmaNivel(lv, Balance.GearMaxLevel) : Textos.Projetos(bp, Balance.GearBlueprints);
                row.Equip.interactable = un && !S.InContract;   // tocar no equipado tira (espaco vazio)
                row.EquipText.text = eq ? Textos.Equipado : Textos.Equipar;
                row.Check.enabled = eq;
                ((Image)row.Equip.targetGraphic).color = eq ? Art.Gold : un ? Art.Good : Art.Off;
                bool can = S.CanUpgradeGear(g);
                row.Up.gameObject.SetActive(un);
                row.Up.interactable = can;
                row.UpText.text = dragon ? Textos.GearDragao : cost < 0 ? Textos.Maximo : Textos.Melhorar;
                row.Cost.text = cost < 0 ? "" : Textos.Num(cost);
                row.UpCoin.enabled = cost >= 0;
                ((Image)row.Up.targetGraphic).color = can ? Art.Good : Art.Off;
            }
            for (int u = 0; u < _ups.Length; u++)
            {
                UpRow row = _ups[u];
                int lv = S.Levels[u], cost = S.UpgradeCost((Upgrade)u);
                row.Level.text = Textos.Nivel(lv);
                for (int k = 0; k < row.Dots.Length; k++) row.Dots[k].color = k < lv ? Art.Courier : Art.A(Art.Tinta, 0.25f);
                row.Cost.text = cost < 0 ? "" : Textos.Num(cost);
                row.CostCoin.enabled = cost >= 0;
                bool can = S.CanBuy((Upgrade)u);
                row.Buy.interactable = can;
                row.BuyText.text = cost < 0 ? Textos.Maximo : Textos.Comprar;
                ((Image)row.Buy.targetGraphic).color = can ? Art.Good : Art.Off;
            }
            for (int w = 0; w < _weps.Length; w++)
            {
                WRow row = _weps[w];
                Weapon wp = (Weapon)w;
                bool unlocked = S.WeaponUnlocked(wp), equipped = S.Equipped == wp;
                int lv = S.WeaponLevel(wp), cost = S.WeaponCost(wp);
                row.G.alpha = unlocked ? 1f : 0.6f;
                row.Lock.enabled = !unlocked;
                row.Need.text = unlocked ? "" : Textos.PrecisaEstrelas(WeaponDef.All[w].Stars);
                row.Level.text = unlocked ? Textos.ArmaNivel(lv, WeaponDef.MaxLevel) : "";
                for (int k = 0; k < row.Dots.Length; k++)
                {
                    row.Dots[k].enabled = unlocked;
                    row.Dots[k].color = k < lv ? Art.Courier : Art.A(Art.Tinta, 0.25f);
                }
                row.Equip.interactable = unlocked && !S.InContract;   // equipada fica "acesa" (dourada + visto); tocar de novo nao faz nada
                row.EquipText.text = equipped ? Textos.Equipada : Textos.Equipar;
                row.Check.enabled = equipped;
                ((Image)row.Equip.targetGraphic).color = equipped ? Art.Gold : unlocked ? Art.Good : Art.Off;
                bool can = S.CanUpgradeWeapon(wp);
                row.Up.interactable = can;
                row.Up.gameObject.SetActive(unlocked);
                row.UpText.text = cost < 0 ? Textos.Maximo : Textos.Melhorar;
                row.Cost.text = cost < 0 ? "" : Textos.Num(cost);
                row.UpCoin.enabled = cost >= 0;
                ((Image)row.Up.targetGraphic).color = can ? Art.Good : Art.Off;
            }
        }

        /// <summary>Aba selecionada: papel claro + sublinhado de tinta (forma, nao so cor).</summary>
        static void Tab(Button b, Image line, bool on)
        {
            ((Image)b.targetGraphic).color = on ? Art.Paper : Art.PaperEdge;
            line.enabled = on;
            b.GetComponentInChildren<Text>().fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
        }

        void RefreshSettings()
        {
            if (_volSomT == null) return;
            _volSomT.text = Textos.Volume(Textos.Som, Ajustes.VolSom * 100 / Ajustes.VolMax);
            _volMusT.text = Textos.Volume(Textos.Musica, Ajustes.VolMusica * 100 / Ajustes.VolMax);
            for (int k = 0; k < Ajustes.VolMax; k++)
            {
                _barSom[k].color = k < Ajustes.VolSom ? Art.Coin : Art.A(Art.Tinta, 0.18f);
                _barMus[k].color = k < Ajustes.VolMusica ? Art.Coin : Art.A(Art.Tinta, 0.18f);
            }
            _vibraBtn.interactable = Ajustes.TemVibra;
            _vibraBtn.GetComponentInChildren<Text>().text = Ajustes.TemVibra ? Textos.Vibra(Ajustes.Vibra) : Textos.VibraSoCelular;
            for (int i = 0; i < _langBtn.Length; i++)
            {
                bool on = (int)Ajustes.Idioma == i;
                _langCheck[i].enabled = on;
                ((Image)_langBtn[i].targetGraphic).color = on ? Art.Good : Art.PaperEdge;
            }
            _versionT.text = Textos.Versao(_g.Version) + "  ·  " + Textos.IdAnonimo(_g.InstallShort);
        }

        // ------------------------------------------------------------------ acoes

        void SubmitCode()
        {
            string c = ContractCode.Normalize(_codeField.text ?? "");
            if (!ContractCode.IsValid(c) || Sim.CodeContract(c) == null) { _codeError.text = Textos.CodigoInvalido; return; }
            _code.gameObject.SetActive(false);
            if (S.ModifiersOpen && !S.SaveRefused) ShowMods(c, m => _g.PlayCode(c, m));
            else _g.PlayCode(c);
        }

        void Vol(bool music, int d)
        {
            if (music) Ajustes.VolMusica += d; else Ajustes.VolSom += d;
            RefreshSettings();
            _g.Log("settings", music ? "musica" : "som", Textos.Num(music ? Ajustes.VolMusica : Ajustes.VolSom));
        }

        void ToggleVibra()
        {
            if (!Ajustes.TemVibra) return;
            Ajustes.Vibra = !Ajustes.Vibra;
            Ajustes.Vibrar(0f);
            RefreshSettings();
            _g.Log("settings", "vibra", Ajustes.Vibra ? "1" : "0");
        }

        void Export()
        {
            Exportar.Status st = _g.ExportarDiario();
            _exportStatus.text = st == Exportar.Status.Pasta ? Textos.ExportPasta : st == Exportar.Status.Escolha ? Textos.ExportEscolha
                : st == Exportar.Status.Vazio ? Textos.ExportVazio : Textos.ExportErro;
            _exportStatus.color = st == Exportar.Status.Erro ? Art.BadInk : Art.Tinta;
        }

        static Sprite PerkIcon(Perk p, out Color c)
        {
            switch (p)
            {
                case Perk.LightStep: c = Art.Shot; return Art.Chevrons();
                case Perk.QuickPlant: c = Art.Cargo; return Art.Flag();
                case Perk.DoubleDash: c = Art.Shot; return Art.Chevrons();
                case Perk.Padded: c = Art.Cargo; return Art.Rounded();
                case Perk.QuickHand: c = Art.Shot; return Art.Sling();
                case Perk.Magnet: c = Art.Coin; return Art.CoinShape();
                case Perk.ExtraHeart: c = Art.Bad; return Art.Heart();
                case Perk.Lookout: c = Art.Ink; return Art.Eyes();
                case Perk.IronBoot: c = Art.Steel; return Art.Fists();
                case Perk.ChestLuck: c = Art.Coin; return Art.ChestShape();
                default: c = Art.Ink; return Art.Star();
            }
        }

        /// <summary>Icone da arma: o pintado (Resources/UI) ou, sem ele, a silhueta procedural na cor do projetil.</summary>
        static Sprite WeaponIcon(Weapon w, out Color c)
        {
            Sprite arte = Art.Ui(w == Weapon.Boomerang ? "icone_bumerangue" : w == Weapon.Bomb ? "icone_bomba" : "icone_estilingue");
            c = arte != null ? Color.white : Art.Shot;
            return arte ?? (w == Weapon.Boomerang ? Art.Boomerang() : w == Weapon.Bomb ? Art.Bomb() : Art.Sling());
        }

        static Sprite UpgradeIcon(Upgrade u, out Color c)
        {
            Sprite arte = Art.Ui(u == Upgrade.Life ? "icone_vida" : u == Upgrade.Dash ? "icone_dash" : u == Upgrade.Bag ? "icone_bolsa" : "icone_mochila");
            if (arte != null) { c = Color.white; return arte; }
            switch (u)
            {
                case Upgrade.Life: c = Art.Bad; return Art.Heart();
                case Upgrade.Dash: c = Art.Courier; return Art.Chevrons();
                case Upgrade.Bag: c = Art.Coin; return Art.Bag();
                default: c = Art.Leather; return Art.Rounded();   // Mochila
            }
        }

        // ------------------------------------------------------------------ construcao: titulo e menu

        RectTransform BuildTitle()
        {
            RectTransform t = Art.Node(_safe, "Titulo", Vector2.zero, Vector2.one);
            Image bg = t.gameObject.AddComponent<Image>(); bg.color = Art.Bg;
            Button tap = t.gameObject.AddComponent<Button>();
            tap.targetGraphic = bg; tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => { Audio.Play("confirmar"); ShowMenu(); });
            FundoMasmorra(t, 0.4f);
            LogoArte(t, new Vector2(0.05f, 0.39f), new Vector2(0.95f, 0.95f));
            Text slogan = Loc(Art.NewText(t, "Slogan", 50, new Vector2(0.05f, 0.33f), new Vector2(0.95f, 0.385f)), () => Textos.Slogan);
            slogan.font = Art.Titulo(); slogan.color = Art.Gold; Art.Letreiro(slogan, 50);
            _tapText = Loc(Art.Letreiro(Art.NewText(t, "Tocar", 56, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.26f)), 56), () => Textos.TocarParaComecar);
            _tapText.font = Art.Titulo();
            Text priv = Loc(Art.NewText(t, "Privacidade", 30, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.145f)), () => Textos.PrivacidadeCurta);
            priv.color = Art.Dim; Shrink(priv, 20);
            Text ver = Loc(Art.NewText(t, "Versao", 28, new Vector2(0.05f, 0.012f), new Vector2(0.95f, 0.05f)), () => Textos.Versao(_g.Version));
            ver.color = Art.Dim;
            return t;
        }

        /// <summary>Selo da guilda (ART_BIBLE §12): envelope de papel cruzado por uma chave dourada, lacre azul com estrela.</summary>
        static void Emblem(Transform parent, float y0, float y1)
        {
            RectTransform e = Art.Node(parent, "Selo", new Vector2(0.18f, y0), new Vector2(0.82f, y1));
            Image glow = Art.Img(e, "Brilho", Art.Glow(), Art.A(Art.Gold, 0.22f), new Vector2(-0.15f, -0.25f), new Vector2(1.15f, 1.25f));
            glow.preserveAspect = false;
            Art.Img(e, "Envelope", Art.Envelope(), Art.Paper, new Vector2(0.08f, 0f), new Vector2(0.92f, 1f));
            Art.Img(e, "Chave", Art.Key(), Art.Gold, new Vector2(0.02f, -0.06f), new Vector2(0.98f, 1.06f));
            Image seal = Art.Img(e, "Lacre", Art.Disc(), Art.Courier, new Vector2(0.4f, 0.3f), new Vector2(0.6f, 0.62f));
            Art.Img(seal.transform, "Estrela", Art.Star(), Art.Gold, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
        }

        /// <summary>Logo do jogo (o courier saindo da masmorra com o letreiro; Resources/UI/logo.png, docs/PROVENIENCIA.md §4) com um halo
        /// de tocha atras.</summary>
        static void LogoArte(Transform parent, Vector2 min, Vector2 max)
        {
            Image glow = Art.Img(parent, "LogoBrilho", Art.Glow(), Art.A(Art.Coin, 0.18f), min, max);
            glow.preserveAspect = false;
            Art.Img(parent, "Logo", Art.Ui("logo"), Color.white, min, max);
        }

        /// <summary>Fundo pintado da masmorra (Resources/UI/fundo_menu.png) cobrindo a tela sem distorcer, escurecido por `escuro` para os
        /// botoes lerem por cima. Sem o arquivo, fica a cor lisa da tela.</summary>
        static void FundoMasmorra(Transform tela, float escuro)
        {
            Sprite s = Art.Ui("fundo_menu");
            if (s == null) return;
            RectTransform caixa = Art.Node(tela, "Fundo", Vector2.zero, Vector2.one);
            caixa.gameObject.AddComponent<RectMask2D>();
            caixa.SetAsFirstSibling();
            float v = 1f - escuro;
            Image f = Art.Img(caixa, "Masmorra", s, new Color(v, v, v * 1.06f), Vector2.zero, Vector2.one);
            f.preserveAspect = false;
            var fit = f.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = s.rect.width / s.rect.height;
        }

        /// <summary>Icone do botao a esquerda (Resources/UI/icone_*.png) e o rotulo empurrado para a direita.</summary>
        static void Icone(Button b, string nome)
        {
            Sprite s = Art.Ui("icone_" + nome);
            if (s == null) return;
            Art.Img(b.transform, "Icone", s, Color.white, new Vector2(0.03f, 0.12f), new Vector2(0.27f, 0.94f));
            b.GetComponentInChildren<Text>().rectTransform.anchorMin = new Vector2(0.26f, 0f);
        }

        RectTransform BuildMenu()
        {
            RectTransform m = Art.Node(_safe, "Menu", Vector2.zero, Vector2.one);
            Image bg = m.gameObject.AddComponent<Image>(); bg.color = Art.Bg;
            FundoMasmorra(m, 0.5f);
            LogoArte(m, new Vector2(0.2f, 0.795f), new Vector2(0.8f, 0.995f));
            _menuCoins = Pill(m, Art.Moeda(), Art.MoedaCor, new Vector2(0.06f, 0.735f), new Vector2(0.47f, 0.79f));
            _menuStars = Pill(m, Art.Star(), Art.Gold, new Vector2(0.53f, 0.735f), new Vector2(0.94f, 0.79f));
            _menuNotice = Shrink(Art.Outlined(Art.NewText(m, "Aviso", 28, new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.735f))), 18);
            // bau do dia (rewarded): conteudo escrito antes de assistir; icone pintado (Resources/UI) no lugar do contorno + silhueta
            _bau = NovoEspecial(m, new Vector2(0.06f, 0.592f), new Vector2(0.94f, 0.697f), Art.Chest, PedeBau);
            _bau.Icon.sprite = _bau.Edge.sprite = Art.ChestShape();
            _bau.Icon.color = Art.Chest; _bau.Edge.color = Art.Coin;   // limao e so da carga (ART_BIBLE §3)
            Sprite bauArte = Art.Ui("icone_bau");
            if (bauArte != null)
            {
                _bau.Edge.enabled = false;
                _bau.Icon.sprite = bauArte; _bau.Icon.color = Color.white;
                _bau.Icon.rectTransform.anchorMin = new Vector2(0.035f, 0.04f); _bau.Icon.rectTransform.anchorMax = new Vector2(0.15f, 0.56f);
            }
            Btn(m, () => Textos.MenuJogar, 72, Art.Good, new Vector2(0.1f, 0.47f), new Vector2(0.9f, 0.582f), ShowBoard, "confirmar");
            // grade 2 x 3 + creditos (cada botao ~163 px de referencia = 48 dp), icone pintado a esquerda
            _menuGuild = Btn(m, () => Textos.MenuGuilda, 44, Art.Paper, new Vector2(0.06f, 0.37f), new Vector2(0.49f, 0.455f), () => ShowGuild(0));
            Icone(_menuGuild, "guilda");
            _menuGuildLock = Art.Img(_menuGuild.transform, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.82f, 0.22f), new Vector2(0.96f, 0.78f));
            _menuLoja = Btn(m, () => Textos.LojaBotao, 44, Art.Gold, new Vector2(0.51f, 0.37f), new Vector2(0.94f, 0.455f), ShowLoja);
            Icone(_menuLoja, "loja");
            Icone(Btn(m, () => Textos.TemporadaBotao, 40, Art.Paper, new Vector2(0.06f, 0.275f), new Vector2(0.49f, 0.36f), () => Abre(_pTemporada)), "temporada");
            Icone(Btn(m, () => Textos.MarcosBotao, 40, Art.Paper, new Vector2(0.51f, 0.275f), new Vector2(0.94f, 0.36f), () => Abre(_pMarcos)), "marcos");
            Icone(Btn(m, () => Textos.RoupasBotao, 40, Art.PaperEdge, new Vector2(0.06f, 0.18f), new Vector2(0.49f, 0.265f), () => Abre(_pRoupas)), "roupas");
            Icone(Btn(m, () => Textos.MenuConfig, 40, Art.PaperEdge, new Vector2(0.51f, 0.18f), new Vector2(0.94f, 0.265f), ShowSettings), "config");
            Icone(Btn(m, () => Textos.MenuCreditos, 40, Art.PaperEdge, new Vector2(0.25f, 0.085f), new Vector2(0.75f, 0.17f), ShowCredits), "creditos");
            Text ver = Loc(Art.NewText(m, "Versao", 28, new Vector2(0.05f, 0.012f), new Vector2(0.95f, 0.05f)), () => Textos.Versao(_g.Version));
            ver.color = Art.Dim;
            return m;
        }

        /// <summary>Pilula escura com icone e numero (moedas, estrelas).</summary>
        static Text Pill(Transform parent, Sprite icon, Color c, Vector2 min, Vector2 max)
        {
            Image bg = Art.Panel(parent, "Pilula", Art.Hex(0x2B2335), min, max);
            bg.sprite = Art.Botao(); bg.raycastTarget = false;
            Art.Img(bg.transform, "Icone", icon, c, new Vector2(0.05f, 0.18f), new Vector2(0.25f, 0.9f));
            Text t = Art.Letreiro(Art.NewText(bg.transform, "Valor", 50, new Vector2(0.28f, 0.12f), new Vector2(0.97f, 1f), TextAnchor.MiddleLeft), 50);
            t.font = Art.Titulo();
            return t;
        }

        // ------------------------------------------------------------------ construcao: quadro de contratos

        RectTransform BuildBoard()
        {
            RectTransform b = Art.Node(_safe, "Quadro", Vector2.zero, Vector2.one);
            Image bg = b.gameObject.AddComponent<Image>(); bg.color = Art.Bg;
            FundoMasmorra(b, 0.62f);
            Btn(b, () => Textos.Voltar, 40, Art.PaperEdge, new Vector2(0.02f, 0.92f), new Vector2(0.25f, 0.996f), ShowMenu);
            // titulo = regiao do quadro; tocar abre o mapa das regioes (bandeira = mapa)
            Button region = Art.NewButton(b, "", 40, Art.Paper, new Vector2(0.265f, 0.92f), new Vector2(0.73f, 0.996f), () => Abre(_pMapa));
            Art.Img(region.transform, "Mapa", Art.Flag(), Art.Tinta, new Vector2(0.03f, 0.2f), new Vector2(0.16f, 0.8f));
            _regionT = region.GetComponentInChildren<Text>();
            _regionT.rectTransform.anchorMin = new Vector2(0.16f, 0f);
            Art.Img(b, "Moeda", Art.Moeda(), Art.MoedaCor, new Vector2(0.745f, 0.937f), new Vector2(0.795f, 0.98f));
            _boardCoins = Art.Outlined(Art.NewText(b, "Moedas", 46, new Vector2(0.8f, 0.92f), new Vector2(0.99f, 0.996f), TextAnchor.MiddleLeft));
            _boardCoins.fontStyle = FontStyle.Bold;
            Art.Img(b, "Estrela", Art.Star(), Art.Gold, new Vector2(0.03f, 0.881f), new Vector2(0.085f, 0.914f));
            _boardStars = Art.Outlined(Art.NewText(b, "Estrelas", 40, new Vector2(0.09f, 0.876f), new Vector2(0.36f, 0.918f), TextAnchor.MiddleLeft));
            _boardStars.fontStyle = FontStyle.Bold;
            _notice = Shrink(Art.NewText(b, "Aviso", 26, new Vector2(0.37f, 0.874f), new Vector2(0.98f, 0.92f)), 16);
            _notice.fontStyle = FontStyle.Bold;
            // contrato do dia e contrato raro (cada um com TROCAR: 1x por dia, preco crescente)
            _dia = NovoEspecial(b, new Vector2(0.02f, 0.777f), new Vector2(0.76f, 0.871f), Art.Gold, () => _g.PlayDaily());
            _diaTroca = NovaTroca(b, new Vector2(0.775f, 0.777f), new Vector2(0.98f, 0.871f), false);
            _raro = NovoEspecial(b, new Vector2(0.02f, 0.677f), new Vector2(0.76f, 0.771f), Art.Coin, () => _g.PlayRare());
            _raroTroca = NovaTroca(b, new Vector2(0.775f, 0.677f), new Vector2(0.98f, 0.771f), true);
            // eventos | sem fim | codigo (~146 px de referencia de altura)
            _evBtn = Btn(b, () => Textos.Eventos, 36, Art.Paper, new Vector2(0.02f, 0.596f), new Vector2(0.33f, 0.672f), ShowEventos);
            _evLock = Art.Img(_evBtn.transform, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.78f, 0.6f), new Vector2(0.96f, 0.95f));
            _sfBtn = Btn(b, () => Textos.SemFimBotao, 36, Art.Paper, new Vector2(0.345f, 0.596f), new Vector2(0.655f, 0.672f), ShowSemFim);
            _sfBtnLock = Art.Img(_sfBtn.transform, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.78f, 0.6f), new Vector2(0.96f, 0.95f));
            _codeBtn = Btn(b, () => Textos.DigitarCodigo, 32, Art.Paper, new Vector2(0.67f, 0.596f), new Vector2(0.98f, 0.672f), ShowCode);
            _codeLock = Art.Img(_codeBtn.transform, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.78f, 0.6f), new Vector2(0.96f, 0.95f));

            // lista rolavel dos contratos da campanha (so os da regiao do quadro: RefreshBoard posiciona)
            RectTransform view = Art.Node(b, "Lista", new Vector2(0.02f, 0.008f), new Vector2(0.98f, 0.588f));
            view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // pega o arrasto entre os cartoes
            view.gameObject.AddComponent<RectMask2D>();
            _scrollContent = Art.Node(view, "Conteudo", new Vector2(0f, 1f), new Vector2(1f, 1f));
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            int n = Sim.Contracts.Length;
            _scrollContent.sizeDelta = new Vector2(0f, n * (RowH + RowGap));
            _scroll = view.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _scrollContent; _scroll.viewport = view;
            _scroll.horizontal = false; _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Elastic; _scroll.scrollSensitivity = 60f;
            _rows = new Row[n];
            for (int i = 0; i < n; i++)
            {
                RectTransform holder = Art.Node(_scrollContent, "Linha", new Vector2(0f, 1f), new Vector2(1f, 1f));
                holder.pivot = new Vector2(0.5f, 1f);
                holder.anchoredPosition = new Vector2(0f, -i * (RowH + RowGap));
                holder.sizeDelta = new Vector2(0f, RowH);
                _rows[i] = BuildRow(holder, i);
            }
            return b;
        }

        Row BuildRow(RectTransform holder, int i)
        {
            ContractDef c = Sim.Contracts[i];
            Image face = Card(holder, "Contrato " + c.Code, Vector2.zero, Vector2.one);
            var row = new Row { G = holder.gameObject.AddComponent<CanvasGroup>() };
            row.B = face.gameObject.AddComponent<Button>();
            row.B.targetGraphic = face;
            int k = i;
            row.B.onClick.AddListener(() => { Audio.Play("confirmar"); Escolhe(k); });
            Transform f = face.transform;
            Text code = Shrink(Ink(f, "Codigo", 44, new Vector2(0.01f, 0f), new Vector2(0.115f, 1f), TextAnchor.MiddleCenter, true), 26);
            code.text = c.Code;
            bool boss = false, mini = false;
            foreach (int[] layer in c.Layers) { RoomDef r = Content.Rooms[layer[0]]; boss |= r.Boss; mini |= r.Mini; }
            if (boss || mini)   // icone do chefe (ART_BIBLE §10): chifres dourados (chefe) ou roxos (Capitao Ladrao)
                Art.Img(f, "Chefe", Art.Horns(), boss ? Art.Gold : Art.Role(EnemyType.Captain), new Vector2(0.02f, 0.68f), new Vector2(0.105f, 0.98f));
            Sprite carga = CargaIcone(c.Cargo, out Color corCarga);   // quadro renderizado da carga; sem a folha, a forma com contorno limao
            if (corCarga != Color.white) Art.Img(f, "CargaContorno", carga, Art.Cargo, new Vector2(0.12f, 0.12f), new Vector2(0.22f, 0.88f));
            Art.Img(f, "Carga", carga, corCarga, corCarga == Color.white ? new Vector2(0.115f, 0.04f) : new Vector2(0.13f, 0.2f),
                corCarga == Color.white ? new Vector2(0.225f, 0.96f) : new Vector2(0.21f, 0.8f));
            // selo de maestria da carga (nivel 1-5; some no 0)
            row.Seal = Art.Img(f, "Maestria", Art.Disc(), Art.Gold, new Vector2(0.185f, 0.6f), new Vector2(0.235f, 0.97f));
            row.SealText = Ink(row.Seal.transform, "Nivel", 24, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, true);
            row.Seal.gameObject.SetActive(false);
            Shrink(Loc(Ink(f, "Linha1", 34, new Vector2(0.235f, 0.5f), new Vector2(0.66f, 0.96f), TextAnchor.MiddleLeft, true),
                () => Textos.Carga(c.Cargo) + " · " + Textos.Salas(c)), 20);
            Text l2 = Shrink(Loc(Ink(f, "Linha2", 30, new Vector2(0.235f, 0.04f), new Vector2(0.66f, 0.5f), TextAnchor.MiddleLeft), () => Textos.Verbo(c)), 18);
            l2.color = Art.A(Art.Tinta, 0.78f);
            Art.Img(f, "Moeda", Art.Moeda(), Art.MoedaCor, new Vector2(0.665f, 0.28f), new Vector2(0.715f, 0.72f));
            Text pay = Ink(f, "Pagamento", 36, new Vector2(0.72f, 0f), new Vector2(0.81f, 1f), TextAnchor.MiddleLeft, true);
            pay.text = Textos.Num(c.Pay);
            row.Badge = Selo(f, new Vector2(0.815f, 0.42f), new Vector2(0.99f, 0.93f), 26, 14, out row.BadgeText);
            row.Lock = Art.Img(row.Badge.transform, "Cadeado", Art.Lock(), Art.Ink, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f));
            for (int s = 0; s < 3; s++)
                row.Stars[s] = Art.Img(f, "Estrela", Art.StarHollow(), Art.Tinta, new Vector2(0.82f + s * 0.057f, 0.06f), new Vector2(0.872f + s * 0.057f, 0.38f));
            return row;
        }

        /// <summary>Cartao destacado (contrato do dia, contrato raro, bau do dia): faixa e estrela na cor do destaque, carga/icone, titulo,
        /// linha e selo de estado (cadeado = fechado, visto = feito). Os textos sao escritos no refresh.</summary>
        Especial NovoEspecial(Transform parent, Vector2 min, Vector2 max, Color band, System.Action click)
        {
            Image face = Card(parent, "Especial", min, max);
            var e = new Especial { G = face.transform.parent.parent.gameObject.AddComponent<CanvasGroup>() };
            e.B = face.gameObject.AddComponent<Button>();
            e.B.targetGraphic = face;
            e.B.onClick.AddListener(() => { Audio.Play("confirmar"); click(); });
            Transform f = face.transform;
            Image stripe = Art.Img(f, "Destaque", Art.Square(), band, new Vector2(0f, 0f), new Vector2(0.03f, 1f));
            stripe.preserveAspect = false;
            Art.Img(f, "Estrela", Art.Star(), band, new Vector2(0.045f, 0.56f), new Vector2(0.14f, 0.95f));
            e.Title = Shrink(Ink(f, "Titulo", 34, new Vector2(0.15f, 0.55f), new Vector2(0.75f, 0.97f), TextAnchor.MiddleLeft, true), 18);
            e.Edge = Art.Img(f, "CargaContorno", Art.Rounded(), Art.Cargo, new Vector2(0.045f, 0.06f), new Vector2(0.14f, 0.52f));
            e.Icon = Art.Img(f, "Carga", Art.Rounded(), Color.white, new Vector2(0.058f, 0.12f), new Vector2(0.127f, 0.46f));
            e.Line = Shrink(Ink(f, "Linha", 28, new Vector2(0.15f, 0.04f), new Vector2(0.75f, 0.55f), TextAnchor.MiddleLeft), 16);
            e.Badge = Selo(f, new Vector2(0.76f, 0.2f), new Vector2(0.98f, 0.8f), 28, 13, out e.BadgeT);
            e.Lock = Art.Img(e.Badge.transform, "Cadeado", Art.Lock(), Art.Ink, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f));
            e.Check = Art.Img(e.Badge.transform, "Visto", Art.Check(), Art.Tinta, new Vector2(0.04f, 0.2f), new Vector2(0.3f, 0.8f));
            return e;
        }

        /// <summary>Selo de estado dos cartoes (JOGAR, REPETIR, VER ANUNCIO, cadeado): mesmo relevo dos botoes, rotulo claro com contorno.
        /// A cor vem do refresh.</summary>
        static Image Selo(Transform f, Vector2 min, Vector2 max, int size, int minSize, out Text rotulo)
        {
            Image b = Art.Panel(f, "Selo", Art.Good, min, max);
            b.sprite = Art.Botao(); b.raycastTarget = false;
            rotulo = Shrink(Art.Letreiro(Art.NewText(b.transform, "Rotulo", size, Vector2.zero, Vector2.one), size), minSize);
            rotulo.font = Art.Titulo();
            rotulo.rectTransform.offsetMin = new Vector2(6f, 12f); rotulo.rectTransform.offsetMax = new Vector2(-6f, -3f);
            return b;
        }

        /// <summary>TROCAR em cima, moeda + preco embaixo.</summary>
        Troca NovaTroca(Transform parent, Vector2 min, Vector2 max, bool rare)
        {
            Button b = Btn(parent, () => Textos.Trocar, 30, Art.Paper, min, max, () => Reroll(rare));
            b.GetComponentInChildren<Text>().rectTransform.anchorMin = new Vector2(0f, 0.5f);
            var t = new Troca { B = b };
            t.Coin = Art.Img(b.transform, "Moeda", Art.Moeda(), Art.MoedaCor, new Vector2(0.1f, 0.12f), new Vector2(0.36f, 0.48f));
            t.Cost = Ink(b.transform, "Preco", 30, new Vector2(0.38f, 0.04f), new Vector2(0.97f, 0.52f), TextAnchor.MiddleLeft, true);
            return t;
        }

        // ------------------------------------------------------------------ construcao: codigo

        RectTransform BuildCode()
        {
            RectTransform m = Modal("Codigo");
            // metade de cima da tela: o teclado do aparelho cobre a de baixo
            Image face = Card(m, "Cartao", new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.96f));
            Transform f = face.transform;
            Loc(Ink(f, "Titulo", 50, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f), TextAnchor.MiddleCenter, true), () => Textos.CodigoTitulo);
            Shrink(Loc(Ink(f, "Corpo", 30, new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.86f)), () => Textos.CodigoCorpo), 20);
            Image edge = Art.Panel(f, "CampoBorda", Art.Tinta, new Vector2(0.14f, 0.45f), new Vector2(0.86f, 0.67f));
            edge.raycastTarget = false;
            Image field = Art.Panel(edge.transform, "Campo", Color.white, Vector2.zero, Vector2.one);
            field.rectTransform.offsetMin = new Vector2(5f, 5f); field.rectTransform.offsetMax = new Vector2(-5f, -5f);
            Text txt = Ink(field.transform, "Texto", 80, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, true);
            txt.supportRichText = false;
            Text ph = Loc(Ink(field.transform, "Exemplo", 56, Vector2.zero, Vector2.one), () => Textos.CodigoDica);
            ph.color = Art.A(Art.Tinta, 0.4f); ph.fontStyle = FontStyle.Italic;
            _codeField = field.gameObject.AddComponent<InputField>();
            _codeField.targetGraphic = field;
            _codeField.textComponent = txt;
            _codeField.placeholder = ph;
            _codeField.contentType = InputField.ContentType.Alphanumeric;
            _codeField.lineType = InputField.LineType.SingleLine;
            _codeField.characterLimit = 6;
            _codeField.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            // so os simbolos do codigo (ContractCode.Alphabet: sem I, O, 0, 1), em maiusculas; o Core confere o digito de verificacao no JOGAR
            _codeField.onValidateInput = (s, i, ch) => ContractCode.Alphabet.IndexOf(char.ToUpperInvariant(ch)) >= 0 ? char.ToUpperInvariant(ch) : '\0';
            _codeError = Shrink(Ink(f, "Erro", 28, new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.44f), TextAnchor.MiddleCenter, true), 18);
            _codeError.color = Art.BadInk;
            Btn(f, () => Textos.JogarCodigo, 40, Art.Good, new Vector2(0.05f, 0.04f), new Vector2(0.6f, 0.3f), SubmitCode, "confirmar");
            Btn(f, () => Textos.Voltar, 40, Art.PaperEdge, new Vector2(0.63f, 0.04f), new Vector2(0.95f, 0.3f), () => _code.gameObject.SetActive(false));
            return m;
        }

        // ------------------------------------------------------------------ construcao: guilda (melhorias + armas)

        RectTransform BuildGuild()
        {
            RectTransform m = Modal("Guilda");
            Image face = Card(m, "Cartao", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
            Transform f = face.transform;
            Loc(Ink(f, "Titulo", 56, new Vector2(0.05f, 0.925f), new Vector2(0.95f, 0.99f), TextAnchor.MiddleCenter, true), () => Textos.Guilda);
            Art.Img(f, "Moeda", Art.Moeda(), Art.MoedaCor, new Vector2(0.2f, 0.873f), new Vector2(0.26f, 0.915f));
            _guildCoins = Ink(f, "Moedas", 42, new Vector2(0.27f, 0.865f), new Vector2(0.48f, 0.923f), TextAnchor.MiddleLeft, true);
            Art.Img(f, "Estrela", Art.Star(), Art.Gold, new Vector2(0.54f, 0.873f), new Vector2(0.6f, 0.915f));
            _guildStars = Ink(f, "Estrelas", 42, new Vector2(0.61f, 0.865f), new Vector2(0.85f, 0.923f), TextAnchor.MiddleLeft, true);
            _tabUps = Btn(f, () => Textos.AbaMelhorias, 36, Art.PaperEdge, new Vector2(0.03f, 0.77f), new Vector2(0.33f, 0.855f), () => { _guildTab = 0; RefreshGuild(); });
            _tabWeps = Btn(f, () => Textos.AbaArmas, 36, Art.PaperEdge, new Vector2(0.35f, 0.77f), new Vector2(0.65f, 0.855f), () => { _guildTab = 1; RefreshGuild(); });
            _tabGear = Btn(f, () => Textos.AbaEquip, 36, Art.PaperEdge, new Vector2(0.67f, 0.77f), new Vector2(0.97f, 0.855f), () => { _guildTab = 2; RefreshGuild(); });
            _tabUpsLine = Underline(_tabUps.transform);
            _tabWepsLine = Underline(_tabWeps.transform);
            _tabGearLine = Underline(_tabGear.transform);

            _guildUps = Art.Node(f, "Melhorias", new Vector2(0.03f, 0.115f), new Vector2(0.97f, 0.755f));
            _ups = new UpRow[Balance.UpgradeLines];
            for (int u = 0; u < _ups.Length; u++) _ups[u] = BuildUpRow(_guildUps, u, _ups.Length);
            _guildWeps = Art.Node(f, "Armas", new Vector2(0.03f, 0.115f), new Vector2(0.97f, 0.755f));
            _weps = new WRow[WeaponDef.All.Length];
            for (int w = 0; w < _weps.Length; w++) _weps[w] = BuildWeaponRow(_guildWeps, w, _weps.Length);
            _guildGear = Art.Node(f, "Equipamento", new Vector2(0.03f, 0.115f), new Vector2(0.97f, 0.755f));
            _gears = new WRow[GearDef.All.Length - 1];
            for (int i = 0; i < _gears.Length; i++) _gears[i] = BuildGearRow(_guildGear, i, _gears.Length);

            Btn(f, () => Textos.Voltar, 40, Art.PaperEdge, new Vector2(0.25f, 0.015f), new Vector2(0.75f, 0.1f), () => _guild.gameObject.SetActive(false));
            return m;
        }

        static Image Underline(Transform tab)
        {
            Image line = Art.Img(tab, "Sublinhado", Art.Square(), Art.Tinta, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.12f));
            line.preserveAspect = false;
            return line;
        }

        UpRow BuildUpRow(Transform parent, int u, int count)
        {
            float h = 1f / count, y1 = 1f - u * h, y0 = y1 - h + 0.015f;
            RectTransform r = Art.Node(parent, "Linha", new Vector2(0f, y0), new Vector2(1f, y1));
            Image sep = Art.Panel(r, "Fundo", Art.A(Art.PaperEdge, 0.35f), Vector2.zero, Vector2.one); sep.raycastTarget = false;
            Upgrade up = (Upgrade)u;
            Art.Img(r, "Icone", UpgradeIcon(up, out Color tint), tint, new Vector2(0.02f, 0.2f), new Vector2(0.15f, 0.8f));
            var row = new UpRow { Dots = new Image[Balance.UpgradeMax] };
            Shrink(Loc(Ink(r, "Nome", 38, new Vector2(0.17f, 0.62f), new Vector2(0.6f, 0.95f), TextAnchor.MiddleLeft, true), () => Textos.UpNome(up)), 24);
            Text eff = Shrink(Loc(Ink(r, "Efeito", 26, new Vector2(0.17f, 0.36f), new Vector2(0.585f, 0.62f), TextAnchor.MiddleLeft), () => Textos.UpEfeito(up)), 16);   // termina antes da moeda do preco (0,6)
            eff.color = Art.A(Art.Tinta, 0.8f);
            float dw = 0.27f / row.Dots.Length;
            for (int k = 0; k < row.Dots.Length; k++)
                row.Dots[k] = Art.Img(r, "Nivel", Art.Disc(), Art.Off, new Vector2(0.17f + k * dw, 0.08f), new Vector2(0.17f + (k + 0.8f) * dw, 0.3f));
            row.Level = Shrink(Ink(r, "NivelTexto", 24, new Vector2(0.45f, 0.04f), new Vector2(0.62f, 0.34f), TextAnchor.MiddleLeft), 16);
            row.Level.color = Art.A(Art.Tinta, 0.75f);
            row.CostCoin = Art.Img(r, "Moeda", Art.Moeda(), Art.MoedaCor, new Vector2(0.6f, 0.3f), new Vector2(0.66f, 0.7f));
            row.Cost = Ink(r, "Custo", 36, new Vector2(0.665f, 0f), new Vector2(0.77f, 1f), TextAnchor.MiddleLeft, true);
            row.Buy = Art.NewButton(r, "", 30, Art.Good, new Vector2(0.77f, 0.14f), new Vector2(0.99f, 0.86f), () => _g.Buy(up), "confirmar");
            row.BuyText = row.Buy.GetComponentInChildren<Text>();
            return row;
        }

        WRow BuildWeaponRow(Transform parent, int w, int count)
        {
            float h = 1f / count, y1 = 1f - w * h, y0 = y1 - h + 0.015f;
            RectTransform r = Art.Node(parent, "Arma", new Vector2(0f, y0), new Vector2(1f, y1));
            Image sep = Art.Panel(r, "Fundo", Art.A(Art.PaperEdge, 0.35f), Vector2.zero, Vector2.one); sep.raycastTarget = false;
            Weapon wp = (Weapon)w;
            var row = new WRow { G = r.gameObject.AddComponent<CanvasGroup>(), Dots = new Image[WeaponDef.MaxLevel] };
            Image disc = Art.Img(r, "Fundo", Art.Disc(), Art.Tinta, new Vector2(0.02f, 0.25f), new Vector2(0.15f, 0.75f));
            Art.Img(disc.transform, "Icone", WeaponIcon(wp, out Color wc), wc, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            row.Lock = Art.Img(r, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.1f, 0.15f), new Vector2(0.17f, 0.42f));
            Shrink(Loc(Ink(r, "Nome", 40, new Vector2(0.17f, 0.66f), new Vector2(0.63f, 0.96f), TextAnchor.MiddleLeft, true), () => Textos.ArmaNome(wp)), 24);
            Text eff = Shrink(Loc(Ink(r, "Efeito", 26, new Vector2(0.17f, 0.36f), new Vector2(0.63f, 0.66f), TextAnchor.MiddleLeft), () => Textos.ArmaEfeito(wp)), 16);
            eff.color = Art.A(Art.Tinta, 0.8f);
            for (int k = 0; k < row.Dots.Length; k++)
                row.Dots[k] = Art.Img(r, "Nivel", Art.Disc(), Art.Off, new Vector2(0.17f + k * 0.05f, 0.08f), new Vector2(0.21f + k * 0.05f, 0.3f));
            row.Level = Shrink(Ink(r, "NivelTexto", 24, new Vector2(0.34f, 0.04f), new Vector2(0.63f, 0.34f), TextAnchor.MiddleLeft), 16);
            row.Level.color = Art.A(Art.Tinta, 0.75f);
            row.Need = Shrink(Ink(r, "Precisa", 28, new Vector2(0.17f, 0.04f), new Vector2(0.63f, 0.36f), TextAnchor.MiddleLeft, true), 18);
            row.Equip = Art.NewButton(r, "", 32, Art.Good, new Vector2(0.65f, 0.53f), new Vector2(0.99f, 0.95f), () => _g.Equip(wp), "confirmar");
            row.EquipText = row.Equip.GetComponentInChildren<Text>();
            row.EquipText.rectTransform.anchorMin = new Vector2(0.2f, 0f);
            row.Check = Art.Img(row.Equip.transform, "Visto", Art.Check(), Art.Tinta, new Vector2(0.04f, 0.2f), new Vector2(0.2f, 0.8f));
            row.Up = Art.NewButton(r, "", 30, Art.Good, new Vector2(0.65f, 0.05f), new Vector2(0.99f, 0.47f), () => _g.UpgradeWeapon(wp), "confirmar");
            row.UpText = row.Up.GetComponentInChildren<Text>();
            row.UpText.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            row.UpCoin = Art.Img(row.Up.transform, "Moeda", Art.Moeda(), Art.MoedaCor, new Vector2(0.62f, 0.25f), new Vector2(0.74f, 0.75f));
            row.Cost = Ink(row.Up.transform, "Custo", 32, new Vector2(0.75f, 0f), new Vector2(0.98f, 1f), TextAnchor.MiddleLeft, true);
            return row;
        }

        /// <summary>Linha do equipamento lateral (4 linhas de ~289 px): icone, nome, efeito, projetos 0/3 ou niveis 1-8, e EQUIPAR | MELHORAR
        /// lado a lado (cada um ~188 x 220 px de referencia, acima de 48 dp). Tocar no equipado tira (espaco vazio).</summary>
        WRow BuildGearRow(Transform parent, int i, int count)
        {
            var g = (Gear)(i + 1);
            float h = 1f / count, y1 = 1f - i * h, y0 = y1 - h + 0.012f;
            RectTransform r = Art.Node(parent, "Equipamento", new Vector2(0f, y0), new Vector2(1f, y1));
            Image sep = Art.Panel(r, "Fundo", Art.A(Art.PaperEdge, 0.35f), Vector2.zero, Vector2.one); sep.raycastTarget = false;
            var row = new WRow { G = r.gameObject.AddComponent<CanvasGroup>(), Dots = new Image[Balance.GearMaxLevel] };
            Image disc = Art.Img(r, "Fundo", Art.Disc(), Art.Tinta, new Vector2(0.015f, 0.27f), new Vector2(0.155f, 0.73f));
            Art.Img(disc.transform, "Icone", GearIcon(g, out Color c), c, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            row.Lock = Art.Img(r, "Cadeado", Art.Lock(), Art.Tinta, new Vector2(0.1f, 0.15f), new Vector2(0.17f, 0.42f));
            Shrink(Loc(Ink(r, "Nome", 36, new Vector2(0.17f, 0.69f), new Vector2(0.59f, 0.97f), TextAnchor.MiddleLeft, true), () => Textos.GearNome(g)), 20);
            Text eff = Shrink(Loc(Ink(r, "Efeito", 24, new Vector2(0.17f, 0.32f), new Vector2(0.59f, 0.69f), TextAnchor.MiddleLeft), () => Textos.GearEfeito(g)), 14);
            eff.color = Art.A(Art.Tinta, 0.8f);
            for (int k = 0; k < row.Dots.Length; k++)   // 8 pontos (niveis 1-8; 6-8 depois do Dragao)
                row.Dots[k] = Art.Img(r, "Ponto", Art.Disc(), Art.Off, new Vector2(0.17f + k * 0.0285f, 0.08f), new Vector2(0.194f + k * 0.0285f, 0.25f));
            row.Level = Shrink(Ink(r, "NivelTexto", 24, new Vector2(0.405f, 0.04f), new Vector2(0.59f, 0.3f), TextAnchor.MiddleLeft), 13);
            row.Level.color = Art.A(Art.Tinta, 0.75f);
            row.Equip = Art.NewButton(r, "", 28, Art.Good, new Vector2(0.6f, 0.1f), new Vector2(0.785f, 0.9f), () => EquipGear(g), "confirmar");
            row.EquipText = row.Equip.GetComponentInChildren<Text>();
            row.EquipText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            row.Check = Art.Img(row.Equip.transform, "Visto", Art.Check(), Art.Tinta, new Vector2(0.25f, 0.52f), new Vector2(0.75f, 0.92f));
            row.Up = Art.NewButton(r, "", 28, Art.Good, new Vector2(0.8f, 0.1f), new Vector2(0.99f, 0.9f), () => UpGear(g), "confirmar");
            row.UpText = row.Up.GetComponentInChildren<Text>();
            row.UpText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            row.UpCoin = Art.Img(row.Up.transform, "Moeda", Art.Moeda(), Art.MoedaCor, new Vector2(0.06f, 0.12f), new Vector2(0.34f, 0.46f));
            row.Cost = Ink(row.Up.transform, "Custo", 28, new Vector2(0.36f, 0.04f), new Vector2(0.98f, 0.5f), TextAnchor.MiddleLeft, true);
            return row;
        }

        void EquipGear(Gear g)
        {
            if (S.EquipGear(S.GearEquipped == g ? Gear.None : g)) _g.AfterMeta();
        }

        void UpGear(Gear g)
        {
            if (S.UpgradeGear(g)) _g.AfterMeta();   // gear_upgrade, currency_sink; som "compra"
        }

        // ------------------------------------------------------------------ construcao: perk

        RectTransform BuildPerk()
        {
            RectTransform m = Modal("Perk", 0.8f);
            Text title = Loc(Art.Outlined(Art.NewText(m, "Titulo", 64, new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.95f)), 3f), () => Textos.PerkTitulo);
            title.fontStyle = FontStyle.Bold;
            Text sub = Loc(Art.NewText(m, "Sub", 32, new Vector2(0.04f, 0.815f), new Vector2(0.96f, 0.86f)), () => Textos.PerkSub);
            sub.color = Art.Dim;
            for (int i = 0; i < _perkCard.Length; i++)
            {
                float y1 = 0.79f - i * 0.257f, y0 = y1 - 0.237f;
                Image face = Card(m, "Perk", new Vector2(0.05f, y0), new Vector2(0.95f, y1));
                _perkCard[i] = face.transform.parent.parent.gameObject;
                Button b = face.gameObject.AddComponent<Button>();
                b.targetGraphic = face;
                int k = i;
                b.onClick.AddListener(() => { _perk.gameObject.SetActive(false); _g.PickPerk(k); });   // som: "perk" no Ev.PerkPick
                Image disc = Art.Img(face.transform, "Fundo", Art.Disc(), Art.Tinta, new Vector2(0.03f, 0.15f), new Vector2(0.25f, 0.85f));
                _perkIcon[i] = Art.Img(disc.transform, "Icone", Art.Star(), Art.Ink, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
                _perkName[i] = Shrink(Ink(face.transform, "Nome", 56, new Vector2(0.28f, 0.5f), new Vector2(0.97f, 0.92f), TextAnchor.MiddleLeft, true), 30);
                _perkEff[i] = Shrink(Ink(face.transform, "Efeito", 36, new Vector2(0.28f, 0.08f), new Vector2(0.97f, 0.5f), TextAnchor.MiddleLeft), 22);
            }
            return m;
        }

        // ------------------------------------------------------------------ construcao: configuracoes e creditos

        RectTransform BuildSettings()
        {
            RectTransform m = Modal("Configuracoes", 0.75f);
            Image face = Card(m, "Cartao", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
            Transform f = face.transform;
            Loc(Ink(f, "Titulo", 56, new Vector2(0.05f, 0.925f), new Vector2(0.95f, 0.99f), TextAnchor.MiddleCenter, true), () => Textos.Config);
            _volSomT = VolRow(f, false, 0.828f, 0.918f, _barSom);
            _volMusT = VolRow(f, true, 0.73f, 0.82f, _barMus);
            _vibraBtn = Art.NewButton(f, "", 40, Art.PaperEdge, new Vector2(0.04f, 0.632f), new Vector2(0.96f, 0.722f), ToggleVibra);
            Loc(Ink(f, "Idioma", 34, new Vector2(0.05f, 0.59f), new Vector2(0.95f, 0.628f), TextAnchor.MiddleLeft, true), () => Textos.Idioma);
            // 4 idiomas em grade 2x2, cada um no proprio idioma; linhas de 0,082 do cartao = ~147 px de referencia (alvo de 48 dp)
            for (int i = 0; i < _langBtn.Length; i++)
            {
                Textos.Lingua l = (Textos.Lingua)i;
                float x0 = i % 2 == 0 ? 0.04f : 0.51f, y1 = i < 2 ? 0.585f : 0.495f;
                _langBtn[i] = Art.NewButton(f, Textos.Nomes[i], 40, Art.PaperEdge, new Vector2(x0, y1 - 0.082f), new Vector2(x0 + 0.45f, y1), () => _g.SetIdioma(l));
                RectTransform label = _langBtn[i].GetComponentInChildren<Text>().rectTransform;
                label.anchorMin = new Vector2(0.18f, 0f); label.anchorMax = new Vector2(0.97f, 1f);   // o visto fica a esquerda, fora do texto
                _langCheck[i] = Art.Img(_langBtn[i].transform, "Visto", Art.Check(), Art.Tinta, new Vector2(0.03f, 0.22f), new Vector2(0.17f, 0.78f));
            }
            Btn(f, () => Textos.ExportarDiario, 42, Art.Gold, new Vector2(0.04f, 0.323f), new Vector2(0.96f, 0.405f), Export);
            Text help = Shrink(Loc(Ink(f, "Ajuda", 26, new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.32f)), () => Textos.ExportarAjuda), 18);
            help.color = Art.A(Art.Tinta, 0.82f);
            _exportStatus = Shrink(Ink(f, "Status", 28, new Vector2(0.05f, 0.237f), new Vector2(0.95f, 0.27f), TextAnchor.MiddleCenter, true), 18);
            Text priv = Shrink(Loc(Ink(f, "Privacidade", 26, new Vector2(0.05f, 0.152f), new Vector2(0.95f, 0.236f)), () => Textos.Privacidade), 18);
            priv.color = Art.A(Art.Tinta, 0.82f);
            _versionT = Shrink(Ink(f, "Versao", 26, new Vector2(0.05f, 0.106f), new Vector2(0.95f, 0.15f)), 16);
            _versionT.color = Art.A(Art.Tinta, 0.75f);
            Btn(f, () => Textos.Voltar, 42, Art.PaperEdge, new Vector2(0.25f, 0.015f), new Vector2(0.75f, 0.1f), () => _settings.gameObject.SetActive(false));
            return m;
        }

        /// <summary>Linha de volume: rotulo com a %, [-], 10 barras, [+]. Botoes grandes em vez de slider (alvo de toque e precisao).</summary>
        Text VolRow(Transform parent, bool music, float y0, float y1, Image[] bar)
        {
            RectTransform r = Art.Node(parent, music ? "Musica" : "Som", new Vector2(0.04f, y0), new Vector2(0.96f, y1));
            Text label = Shrink(Ink(r, "Rotulo", 36, new Vector2(0f, 0f), new Vector2(0.34f, 1f), TextAnchor.MiddleLeft, true), 22);
            Art.NewButton(r, "-", 72, Art.PaperEdge, new Vector2(0.35f, 0.03f), new Vector2(0.51f, 0.97f), () => Vol(music, -1));
            float w = 0.29f / bar.Length;
            for (int k = 0; k < bar.Length; k++)
            {
                bar[k] = Art.Img(r, "Nivel", Art.Rounded(), Art.Off, new Vector2(0.525f + k * w, 0.2f), new Vector2(0.525f + (k + 0.78f) * w, 0.8f));
                bar[k].preserveAspect = false;
            }
            Art.NewButton(r, "+", 72, Art.PaperEdge, new Vector2(0.83f, 0.03f), new Vector2(0.99f, 0.97f), () => Vol(music, +1));
            return label;
        }

        RectTransform BuildCredits()
        {
            RectTransform m = Modal("Creditos", 0.75f);
            Image face = Card(m, "Cartao", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            Transform f = face.transform;
            Loc(Ink(f, "Titulo", 56, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f), TextAnchor.MiddleCenter, true), () => Textos.Creditos);
            Text game = Ink(f, "Jogo", 60, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.9f), TextAnchor.MiddleCenter, true);
            game.text = "Dungeon Courier";
            Shrink(Loc(Ink(f, "Corpo", 36, new Vector2(0.06f, 0.4f), new Vector2(0.94f, 0.82f)), () => Textos.CreditosCorpo), 22);
            Text priv = Shrink(Loc(Ink(f, "Privacidade", 28, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.38f)), () => Textos.Privacidade), 18);
            priv.color = Art.A(Art.Tinta, 0.82f);
            Btn(f, () => Textos.Voltar, 42, Art.PaperEdge, new Vector2(0.25f, 0.02f), new Vector2(0.75f, 0.115f), () => _credits.gameObject.SetActive(false));
            return m;
        }
    }
}
