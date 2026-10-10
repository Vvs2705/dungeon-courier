using System;
using System.Collections.Generic;
using System.Globalization;
using DC.Core;

namespace DC
{
    /// <summary>
    /// Textos do jogador em pt-BR e EN, sem UnityEngine: um lugar so para revisar, medir e traduzir. Tabela de 2 colunas: cada texto e
    /// L("pt", "en") com a frase INTEIRA nas duas linguas (nada de montar frase com pedacos; numero entra por {0}). O idioma vem do
    /// Ajustes.Ingles (PlayerPrefs) e troca na hora: as telas releem tudo (Hud.Relocalizar).
    /// Conteudo do Core (dicas, cargas, salas, perks, armas) e traduzido AQUI pelo enum/codigo, sem tocar no Core: o pt vem do proprio
    /// Core (Hints.Text, CargoDef.Name, RoomDef.Name, PerkDef, WeaponDef); o EN sai da tabela; sem traducao cai no texto do Core.
    /// Nada de emoji: icones sao sprites do Art.
    /// </summary>
    public static class Textos
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>true = ingles. So o Ajustes muda (e o Hud reaplica as telas).</summary>
        public static bool En;
        static string L(string pt, string en) => En ? en : pt;
        static string F(string pt, string en, object a) => string.Format(Inv, En ? en : pt, a);
        static string F(string pt, string en, object a, object b) => string.Format(Inv, En ? en : pt, a, b);
        static string F(string pt, string en, object a, object b, object c) => string.Format(Inv, En ? en : pt, a, b, c);

        // ------------------------------------------------------------------ geral
        public static string Voltar => L("VOLTAR", "BACK");
        public static string Continuar => L("CONTINUAR", "CONTINUE");
        public static string Jogar => L("JOGAR", "PLAY");
        public static string Repetir => L("REPETIR", "REPLAY");
        public static string Resgatar => L("RESGATAR", "RESCUE");
        public static string Recusar => L("RECUSAR", "DECLINE");
        public static string Comprar => L("COMPRAR", "BUY");
        public static string Maximo => L("MÁX", "MAX");
        public static string Versao(string v) => F("versão {0}", "version {0}", v);

        // ------------------------------------------------------------------ titulo e menu
        public const string Nome1 = "DUNGEON", Nome2 = "COURIER";   // nome do jogo: igual nas duas linguas
        public static string Slogan => L("Entregas pela masmorra", "Deliveries through the dungeon");
        public static string TocarParaComecar => L("toque para começar", "tap to start");
        public static string PrivacidadeCurta => L("Seu progresso e o diário de teste ficam só neste aparelho. Nada é enviado sem você mandar.",
            "Your progress and the playtest log stay on this device. Nothing is sent unless you send it.");
        public static string MenuJogar => L("JOGAR", "PLAY");
        public static string MenuGuilda => L("GUILDA", "GUILD");
        public static string MenuConfig => L("CONFIGURAÇÕES", "SETTINGS");
        public static string MenuCreditos => L("CRÉDITOS", "CREDITS");
        public static string GuildaFechada => L("a guilda abre depois do 1º contrato", "the guild opens after the 1st contract");

        // ------------------------------------------------------------------ quadro de contratos
        public static string Regiao => L("Porões da Guilda", "Guild Cellars");
        public static string Estrelas(int tem, int total) => F("{0}/{1}", "{0}/{1}", tem, total);
        public static string Dia => L("Contrato do dia", "Daily contract");
        /// <summary>Rotulo curto do contrato nas telas: C12 / Dia / K7M2QX (o Core chama o do dia de "DIA"; o diario guarda o codigo cru).</summary>
        public static string Rotulo(ContractDef c) => c.Kind == ContractKind.Daily ? L("Dia", "Daily") : c.Code;
        public static string DiaLinha(string carga, int moedas) => F("{0} · recompensa extra +{1} moedas", "{0} · extra reward +{1} coins", carga, moedas);
        public static string DiaFeito => L("feito hoje: volte amanhã", "done today: come back tomorrow");
        public static string DiaFechado => L("abre depois do C3", "opens after C3");
        public static string Feito => L("FEITO", "DONE");
        public static string DigitarCodigo => L("DIGITAR CÓDIGO", "ENTER CODE");

        // ------------------------------------------------------------------ contrato por codigo
        public static string CodigoTitulo => L("Contrato por código", "Contract by code");
        public static string CodigoCorpo => L("Digite o código de 6 caracteres que um amigo mandou (letras e números 2-9; não tem I, O, 0 nem 1).",
            "Type the 6-character code a friend sent you (letters and digits 2-9; no I, O, 0 or 1).");
        public static string CodigoDica => L("ex.: K7M2QX", "e.g. K7M2QX");
        public static string CodigoInvalido => L("Código inválido: confira os 6 caracteres com quem mandou.",
            "Invalid code: check the 6 characters with whoever sent it.");
        public static string JogarCodigo => L("JOGAR CÓDIGO", "PLAY CODE");

        // ------------------------------------------------------------------ guilda e armas
        public static string Guilda => L("Guilda", "Guild");
        public static string AbaMelhorias => L("MELHORIAS", "UPGRADES");
        public static string AbaArmas => L("ARMAS", "WEAPONS");
        public static string Equipar => L("EQUIPAR", "EQUIP");
        public static string Equipada => L("EQUIPADA", "EQUIPPED");
        public static string Melhorar => L("MELHORAR", "UPGRADE");
        public static string PrecisaEstrelas(int n) => F("precisa de {0} estrelas", "needs {0} stars", n);
        public static string ArmaNivel(int lv, int max) => F("nível {0}/{1}", "level {0}/{1}", lv, max);

        public static string UpNome(Upgrade u)
        {
            switch (u)
            {
                case Upgrade.Life: return L("Vida", "Health");
                case Upgrade.Dash: return "Dash";
                case Upgrade.Bag: return L("Bolsa", "Purse");
                case Upgrade.Pack: return L("Mochila", "Backpack");
                default: return u.ToString();
            }
        }

        public static string UpEfeito(Upgrade u)
        {
            switch (u)
            {
                case Upgrade.Life: return L("+1 de vida máxima", "+1 max health");
                case Upgrade.Dash: return L("-10% na recarga do dash", "-10% dash cooldown");
                case Upgrade.Bag: return L("+10% de moedas", "+10% coins");
                case Upgrade.Pack: return L("pega de mais longe e pousa mais rápido", "longer pickup reach, faster landing");
                default: return "";
            }
        }

        public static string Nivel(int lv) => F("nível {0}/{1}", "level {0}/{1}", lv, Balance.UpgradeMax);

        /// <summary>Nome da arma: pt do Core (WeaponDef.Name); EN pela tabela.</summary>
        public static string ArmaNome(Weapon w)
        {
            if (!En) return WeaponDef.All[(int)w].Name;
            switch (w)
            {
                case Weapon.Sling: return "Slingshot";
                case Weapon.Boomerang: return "Boomerang";
                case Weapon.Bomb: return "Little bomb";
                default: return WeaponDef.All[(int)w].Name;
            }
        }

        public static string ArmaEfeito(Weapon w)
        {
            if (!En) return WeaponDef.All[(int)w].Text;
            switch (w)
            {
                case Weapon.Sling: return "homing shot";
                case Weapon.Boomerang: return "flies out and back in a line, piercing enemies";
                case Weapon.Bomb: return "slow arcing area blast, weak vs fast foes; cracks the egg and sets off the keg";
                default: return WeaponDef.All[(int)w].Text;
            }
        }

        // ------------------------------------------------------------------ perks (1 de 3, so na run)
        public static string PerkTitulo => L("Escolha 1 vantagem", "Pick 1 perk");
        public static string PerkSub => L("vale até o fim deste contrato", "lasts until the end of this contract");

        /// <summary>Nome do perk: pt do Core (PerkDef.Name); EN pela tabela.</summary>
        public static string PerkNome(Perk p)
        {
            if (!En) return PerkDef.All[(int)p].Name;
            switch (p)
            {
                case Perk.LightStep: return "Light step";
                case Perk.QuickPlant: return "Quick landing";
                case Perk.DoubleDash: return "Double dash";
                case Perk.Padded: return "Padded cargo";
                case Perk.QuickHand: return "Quick hand";
                case Perk.Magnet: return "Magnet";
                case Perk.ExtraHeart: return "Extra heart";
                case Perk.Lookout: return "Lookout";
                case Perk.IronBoot: return "Iron boot";
                case Perk.ChestLuck: return "Chest luck";
                default: return PerkDef.All[(int)p].Name;
            }
        }

        public static string PerkEfeito(Perk p)
        {
            if (!En) return PerkDef.All[(int)p].Text;
            switch (p)
            {
                case Perk.LightStep: return "+10% speed while carrying";
                case Perk.QuickPlant: return "sets down in 0.1 s";
                case Perk.DoubleDash: return "2 dash charges";
                case Perk.Padded: return "the 1st hit on the cargo is ignored";
                case Perk.QuickHand: return "+20% fire rate";
                case Perk.Magnet: return "opens chests from farther away";
                case Perk.ExtraHeart: return "+2 max health and heals 2";
                case Perk.Lookout: return "the thief takes +4 s to reach unattended cargo";
                case Perk.IronBoot: return "dash pushes harder and stuns for 0.6 s";
                case Perk.ChestLuck: return "+30% chest coins";
                default: return PerkDef.All[(int)p].Text;
            }
        }

        // ------------------------------------------------------------------ contrato em curso
        public static string Pausado => L("Pausado", "Paused");
        public static string Retomado => L("Contrato em curso", "Contract in progress");
        public static string Desistir => L("DESISTIR DO CONTRATO", "ABANDON CONTRACT");
        public static string DesistirTitulo => L("Desistir do contrato?", "Abandon the contract?");
        public static string DesistirCorpo => L("O contrato falha e a carga NÃO fica na masmorra para resgate. As moedas dos baús ficam com você.",
            "The contract fails and the cargo will NOT stay in the dungeon for rescue. You keep the coins from chests.");
        public static string DesistirSim => L("SIM, DESISTIR", "YES, ABANDON");
        public static string PausaLinha(string codigo, string carga, string tempo) => F("{0} · {1} · {2}", "{0} · {1} · {2}", codigo, carga, tempo);
        public static string Pular => L("toque para pular", "tap to skip");
        public static string SaveRecusado => L("Este save é de uma versão mais nova do jogo. Atualize: nada será jogado nem gravado aqui.",
            "This save is from a newer version of the game. Please update: nothing will be played or saved here.");
        public static string OfertaTitulo => L("Carga perdida", "Lost cargo");
        public static string AvisoPendente => L("A carga ficou na masmorra: o próximo contrato oferece o resgate.",
            "The cargo was left in the dungeon: the next contract offers a rescue.");
        // rotulos que flutuam no mundo (WorldView)
        public static string Rachou => L("rachou!", "cracked!");
        public static string Quebrou => L("quebrou!", "broke!");
        public static string Ladrao => L("ladrão!", "thief!");
        public static string Fugiu => L("fugiu!", "got away!");
        public static string Chocou => L("chocou!", "hatched!");

        /// <summary>Dica da HUD pelo enum Hint do Core. pt = Hints.Text (fonte do Core); EN pela tabela; sem traducao = texto do Core.
        /// So literais: a HUD chama todo quadro e compara por referencia, nada aloca.</summary>
        public static string Dica(Hint h)
        {
            if (!En) return Hints.Text(h);
            switch (h)
            {
                case Hint.PlantToFight: return "release your finger to set down and fight";
                case Hint.PickUp: return "drag near the cargo to pick it up";
                case Hint.Dash: return "quick tap = dash (drops the cargo)";
                case Hint.Thief: return "thief! it wants your cargo";
                case Hint.DoorNeedsCargo: return "the door only opens with the cargo in hand";
                case Hint.GuardDoor: return "defeat the guard to open the door";
                case Hint.Chest: return "stand still next to the chest to open it";
                case Hint.WaitPet: return "the door only opens with the chick nearby";
                case Hint.DragToWalk: return "drag your finger to walk with the cargo";
                case Hint.CakeNoDash: return "the cake can't take a dash: just walk";
                case Hint.BarrelFuse: return "a keg on the ground explodes in 3 s: pick it back up!";
                case Hint.ArcherAim: return "archer aiming: get out of the line";
                case Hint.SecretWall: return "cracked wall: walk through it with the cargo";
                case Hint.MessageDoor: return "the message opens the locked door";
                default: return Hints.Text(h);
            }
        }

        /// <summary>Linha da dica quando o Core nao tem dica (Hint.None): o objetivo da sala.</summary>
        public static string Objetivo(Run r) => r.Room.Delivery ? L("leve a carga até o cliente", "bring the cargo to the client")
            : L("escolha uma porta no topo", "pick a door at the top");

        // ------------------------------------------------------------------ conteudo do Core (cargas, salas)

        /// <summary>Nome da carga: pt do Core (CargoDef.Name); EN pela chave do enum.</summary>
        public static string Carga(CargoType t)
        {
            string pt = (int)t >= 0 && (int)t < CargoDef.All.Length ? CargoDef.All[(int)t].Name : t.ToString();
            if (!En) return pt;
            switch (t)
            {
                case CargoType.Box: return "Crate";
                case CargoType.Relic: return "Fragile relic";
                case CargoType.Egg: return "Egg";
                case CargoType.Cake: return "Wedding cake";
                case CargoType.Message: return "Urgent message";
                case CargoType.Heavy: return "Heavy crate";
                case CargoType.Barrel: return "Powder keg";
                default: return pt;
            }
        }

        /// <summary>Nome da sala: pt do Core (RoomDef.Name); EN pelo codigo da sala; sem traducao, o proprio nome do Core.</summary>
        public static string Sala(RoomDef r)
        {
            if (!En) return r.Name;
            switch (r.Code)
            {
                case "R1": return "Guard's Room";
                case "R2": return "1st Thief's Room";
                case "R3": return "Long Route with Chest";
                case "R4": return "Swarm";
                case "R5": return "Brute's Room";
                case "R6": return "Guard and Thief";
                case "R7": return "Archer's Corridor";
                case "R8": return "Slime Puddle";
                case "R9": return "Archers' Gallery";
                case "R10": return "Ambush";
                case "R11": return "Brute and Archer";
                case "R12": return "Puddle Guard";
                case "R13": return "Thieves in Pairs";
                case "R14": return "Maze";
                case "R15": return "Double Guard";
                case "R16": return "Final Hall";
                case "S1": return "Secret Storeroom";
                case "S2": return "Hidden Treasure";
                case "M1": return "Captain's Den";
                case "B1": return "Ogre's Lair";
                case "D1": return "Delivery";
                default: return r.Name;
            }
        }

        /// <summary>"2 salas + entrega" / "4 salas + mini-chefe + entrega" / "5 salas + chefe + entrega" (lista curta da linha do quadro).</summary>
        public static string Salas(ContractDef c)
        {
            int n = 0; bool boss = false, mini = false;
            foreach (int[] layer in c.Layers)
            {
                RoomDef r = Content.Rooms[layer[0]];
                if (r.Boss) boss = true; else if (r.Mini) mini = true; else if (!r.Delivery) n++;
            }
            string salas = n == 1 ? L("1 sala", "1 room") : F("{0} salas", "{0} rooms", n);
            string extra = boss ? L(" + chefe", " + boss") : mini ? L(" + mini-chefe", " + mini-boss") : "";
            return salas + extra + L(" + entrega", " + delivery");
        }

        /// <summary>O verbo que a carga muda, numa linha (linha 2 do quadro).</summary>
        public static string Verbo(ContractDef c)
        {
            switch (c.Cargo)
            {
                case CargoType.Relic: return L("frágil: 3 rachaduras = quebra", "fragile: 3 cracks = broken");
                case CargoType.Egg: return F("choca em {0} s: o filhote tem de chegar", "hatches in {0} s: the chick must arrive", c.EggTarget.ToString("0", Inv));
                case CargoType.Cake: return L("sem dash: o toque rápido só balança", "no dash: a quick tap only wobbles");
                case CargoType.Message: return L("contra o relógio; abre portas trancadas", "against the clock; opens locked doors");
                case CargoType.Heavy: return L("-25% de velocidade carregando", "-25% speed while carrying");
                case CargoType.Barrel: return L("no chão por 3 s, explode", "explodes after 3 s on the ground");
                default: return c.Index == 0 ? L("primeiro contrato", "first contract") : L("entregue inteira: bônus", "deliver it intact: bonus");
            }
        }

        public static string Pendente(LostCargo p) => p.Contract >= 0
            ? F("Carga perdida: {0} (C{1}). O próximo contrato oferece o resgate.", "Lost cargo: {0} (C{1}). The next contract offers a rescue.", Carga(p.Type), p.Contract + 1)
            : F("Carga perdida: {0}. O próximo contrato oferece o resgate.", "Lost cargo: {0}. The next contract offers a rescue.", Carga(p.Type));

        public static string Oferta(LostCargo p, int bonus)
        {
            string origem = p.Contract >= 0 ? F("{0} do C{1}", "The {0} from C{1}", Carga(p.Type), p.Contract + 1) : F("{0} do último contrato", "The {0} from the last contract", Carga(p.Type));
            if (p.Cracks > 0)
                return F("{0} ficou na masmorra com {1}.\nAceite e ela aparece no chão da 1ª sala. Entregue as duas e ganhe +{2} moedas.\nRecusou, ela se perde.",
                    "{0} was left in the dungeon with {1}.\nAccept and it shows up on the floor of the 1st room. Deliver both to earn +{2} coins.\nDecline and it is lost.",
                    origem, Rachaduras(p.Cracks), bonus);
            return F("{0} ficou na masmorra.\nAceite e ela aparece no chão da 1ª sala. Entregue as duas e ganhe +{1} moedas.\nRecusou, ela se perde.",
                "{0} was left in the dungeon.\nAccept and it shows up on the floor of the 1st room. Deliver both to earn +{1} coins.\nDecline and it is lost.",
                origem, bonus);
        }

        public static string Rachaduras(int n) => n == 1 ? L("1 rachadura", "1 crack") : F("{0} rachaduras", "{0} cracks", n);

        // ------------------------------------------------------------------ fim do contrato

        public static string Titulo(Outcome o)
        {
            switch (o)
            {
                case Outcome.Delivered: return L("Entregue!", "Delivered!");
                case Outcome.Died: return L("Você caiu", "You fell");
                case Outcome.CargoStolen: return L("Roubaram a carga", "The cargo was stolen");
                case Outcome.CargoBroken: return L("A relíquia quebrou", "The relic broke");
                case Outcome.PetDied: return L("O filhote não resistiu", "The chick didn't make it");
                case Outcome.Abandoned: return L("Contrato abandonado", "Contract abandoned");
                case Outcome.TimeUp: return L("O tempo acabou", "Time ran out");
                case Outcome.Exploded: return L("O barril explodiu", "The keg exploded");
                default: return L("Contrato falhou", "Contract failed");
            }
        }

        public static string Motivo(ContractResult r)
        {
            switch (r.Outcome)
            {
                case Outcome.Died:
                    return r.DeathRoom >= 0 && r.DeathRoom < Content.Rooms.Length
                        ? F("O courier caiu na sala {0}: {1}.", "The courier fell in room {0}: {1}.", Content.Rooms[r.DeathRoom].Code, Sala(Content.Rooms[r.DeathRoom]))
                        : L("O courier caiu.", "The courier fell.");
                case Outcome.CargoStolen: return L("O ladrão escapou pela borda da sala com a carga.", "The thief escaped through the edge of the room with the cargo.");
                case Outcome.CargoBroken: return L("A 3ª rachadura quebrou a relíquia.", "The 3rd crack broke the relic.");
                case Outcome.PetDied: return L("O filhote foi atingido duas vezes.", "The chick was hit twice.");
                case Outcome.Abandoned: return L("Você desistiu do contrato.", "You abandoned the contract.");
                case Outcome.TimeUp: return L("A mensagem urgente chegou tarde demais.", "The urgent message arrived too late.");
                case Outcome.Exploded: return L("O barril ficou 3 s no chão e explodiu.", "The keg sat on the ground for 3 s and exploded.");
                default: return "";
            }
        }

        /// <summary>Reacao do cliente ao abrir a caixa (cena da entrega).</summary>
        public static string Reacao(ContractResult r)
        {
            if (r.Hatched) return L("Chocou no caminho, mas o filhote chegou!", "It hatched on the way, but the chick made it!");
            if (r.Cracks <= 0) return L("Perfeito! Nenhuma rachadura.", "Perfect! Not a single crack.");
            if (r.Cracks == 1) return L("Bom! Só 1 rachadura.", "Good! Just 1 crack.");
            return F("Hmm... {0} rachaduras.", "Hmm... {0} cracks.", r.Cracks);
        }

        // estrelas do resultado (CONTRATO_V1 §7): entregue · carga intacta · sem morrer e no tempo-alvo
        public static string EstrelaEntregue => L("Entregue", "Delivered");
        public static string EstrelaIntacta => L("Carga intacta", "Cargo intact");
        public static string EstrelaTempo => L("Sem cair e no tempo-alvo", "No falls, within target time");
        public static string NovoRecorde => L("Novo recorde de estrelas neste contrato!", "New star record on this contract!");
        public static string CodigoDoContrato(string code) => F("Código: {0}", "Code: {0}", code);
        public static string Compartilhar => L("COMPARTILHAR", "SHARE");
        public static string Copiado => L("código copiado", "code copied");
        public static string Convite(string code, int estrelas) => F("Fiz {1}/3 estrelas no contrato {0} do Dungeon Courier. Consegue mais? Código: {0}",
            "I got {1}/3 stars on Dungeon Courier contract {0}. Can you beat it? Code: {0}", code, estrelas);

        public static string Mul(float m) => "×" + (En ? m.ToString("0.0#", Inv) : m.ToString("0.0#", Inv).Replace('.', ','));
        public static string Tempo(float s) => ((int)(s / 60f)) + ":" + ((int)(s % 60f)).ToString("00", Inv);

        public static string RotuloPagamento => L("Pagamento", "Payment");
        public static string RotuloTotal => L("Total", "Total");

        /// <summary>Pagamento detalhado (rotulo | valor) do ContractResult: base x carga x integridade x bolsa = pagamento; + baus + resgate
        /// (+ bonus do contrato do dia). No maximo 10 linhas (Hud._resLabel).</summary>
        public static string[][] Pagamento(ContractResult r, ContractDef c)
        {
            if (!r.Delivered)
                return new[] { new[] { L("Tempo", "Time"), Tempo(r.Time) }, new[] { L("Moedas dos baús", "Chest coins"), "+" + r.ChestCoins }, new[] { RotuloTotal, "+" + r.Total } };
            var rows = new List<string[]>
            {
                new[] { L("Pagamento base", "Base pay"), r.Base.ToString(Inv) },
                new[] { r.Hatched ? F("Carga: {0} (chocou)", "Cargo: {0} (hatched)", Carga(c.Cargo)) : F("Carga: {0}", "Cargo: {0}", Carga(c.Cargo)), Mul(r.CargoMul) },
                new[] { F("Integridade: {0}", "Integrity: {0}", Rachaduras(r.Cracks)), Mul(r.IntegrityMul) },
                new[] { L("Bolsa da guilda", "Guild purse"), Mul(r.BagMul) },
                new[] { RotuloPagamento, r.Payment.ToString(Inv) },
                new[] { L("Baús", "Chests"), "+" + r.ChestCoins },
                new[] { r.RescueAccepted && !r.RescueDelivered ? L("Resgate (não entregue)", "Rescue (not delivered)") : L("Resgate", "Rescue"), "+" + r.RescueBonus },
            };
            if (r.DailyBonus > 0) rows.Add(new[] { L("Bônus do contrato do dia", "Daily contract bonus"), "+" + r.DailyBonus });
            rows.Add(new[] { L("Tempo", "Time"), Tempo(r.Time) });
            rows.Add(new[] { RotuloTotal, "+" + r.Total });
            return rows.ToArray();
        }

        // ------------------------------------------------------------------ configuracoes, creditos, privacidade
        public static string Config => L("Configurações", "Settings");
        public static string Som => L("Som", "Sound");
        public static string Musica => L("Música", "Music");
        public static string Volume(string nome, int pct) => F("{0}: {1}%", "{0}: {1}%", nome, pct);
        public static string Vibra(bool on) => on ? L("Vibração: ligada", "Vibration: on") : L("Vibração: desligada", "Vibration: off");
        public static string VibraSoCelular => L("Vibração: só no celular", "Vibration: phone only");
        public static string Idioma => L("Idioma", "Language");
        public const string Portugues = "Português", English = "English";   // cada idioma no proprio idioma
        public static string ExportarDiario => L("EXPORTAR DIÁRIO", "EXPORT LOG");
        public static string ExportarAjuda => L("O diário (CSV) registra eventos do jogo para o teste, sem dados pessoais. Só sai do aparelho se você exportar.",
            "The log (CSV) records game events for the playtest, with no personal data. It only leaves the device if you export it.");
        public static string ExportPasta => L("Pasta do diário aberta.", "Log folder opened.");
        public static string ExportEscolha => L("Escolha o app para enviar o diário.", "Pick an app to send the log.");
        public static string ExportVazio => L("Ainda não há diário: jogue um contrato primeiro.", "No log yet: play a contract first.");
        public static string ExportErro => L("Não foi possível exportar o diário.", "Could not export the log.");
        public static string IdAnonimo(string id) => F("id anônimo do teste: {0}", "anonymous test id: {0}", id);

        public static string Creditos => L("Créditos", "Credits");
        public static string CreditosCorpo => L(
            "Um jogo da V-STACK\n\nCriação, direção e produção\nVinicius (V-STACK)\n\nModelos 3D gerados com IA (Tripo)\nAnimações Mixamo e Blender\nSons e músicas sintetizados por código\nFeito com Unity",
            "A V-STACK game\n\nCreated, directed and produced by\nVinicius (V-STACK)\n\n3D models generated with AI (Tripo)\nAnimations by Mixamo and Blender\nSounds and music synthesized in code\nMade with Unity");
        public static string Privacidade => L("Privacidade: o jogo não coleta dados pessoais e não usa a internet. O progresso e o diário de teste ficam só neste aparelho; nada sai dele sem você tocar em Exportar diário.",
            "Privacy: the game collects no personal data and does not use the internet. Progress and the playtest log stay on this device; nothing leaves it unless you tap Export log.");

        public static string Num(int v) => v.ToString(Inv);
        public static string Segundos(float s) => Math.Max(0, (int)Math.Ceiling(s)).ToString(Inv) + " s";
    }
}
