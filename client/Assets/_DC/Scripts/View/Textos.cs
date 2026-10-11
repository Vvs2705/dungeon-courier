using System;
using System.Collections.Generic;
using System.Globalization;
using DC.Core;

namespace DC
{
    /// <summary>
    /// Textos do jogador em pt-BR, EN, ES (espanhol neutro latino-americano) e ID (Bahasa Indonesia), sem UnityEngine: um lugar so para
    /// revisar, medir e traduzir (regras e glossario em docs/UI_V1.md, "Localizacao"). Cada texto e L("pt", "en", "es", "id") com a frase
    /// INTEIRA em cada lingua (nada de montar frase com pedacos; numero entra por {0}); es/id ausentes caem no EN. O idioma vem do
    /// Ajustes.Idioma (PlayerPrefs) e troca na hora: as telas releem tudo (Hud.Relocalizar).
    /// Conteudo do Core (dicas, cargas, salas, perks, armas, inimigos) e traduzido AQUI pelo enum/codigo, sem tocar no Core: o pt vem do
    /// proprio Core (Hints.Text, CargoDef.Name, RoomDef.Name, PerkDef, WeaponDef, EnemyDef); as outras linguas saem da tabela; sem
    /// traducao cai no texto do Core. Nada de emoji nem caractere fora do Latin-1: icones sao sprites do Art.
    /// </summary>
    public static class Textos
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>Ordem = indice dos codigos e nomes abaixo (e da grade 2x2 das Configuracoes).</summary>
        public enum Lingua { Pt, En, Es, Id }
        /// <summary>Idioma atual. So o Ajustes muda (e o Hud reaplica as telas); o -lang do -testsession forca sem gravar.</summary>
        public static Lingua Lang = Lingua.Pt;
        /// <summary>Codigo gravado no PlayerPrefs (dc_idioma) e no diario.</summary>
        public static readonly string[] Codigos = { "pt", "en", "es", "id" };
        /// <summary>Cada idioma no proprio idioma (botoes das Configuracoes: nao traduzem).</summary>
        public static readonly string[] Nomes = { "Português", "English", "Español", "Bahasa Indonesia" };

        public static Lingua? DeCodigo(string c)
        {
            int i = Array.IndexOf(Codigos, c);
            return i < 0 ? (Lingua?)null : (Lingua)i;
        }

        /// <summary>Frase na lingua atual; es/id null = EN. Devolve o proprio literal (sem alocar).</summary>
        static string L(string pt, string en, string es = null, string id = null)
            => Lang == Lingua.Pt ? pt : Lang == Lingua.Es ? es ?? en : Lang == Lingua.Id ? id ?? en : en;
        static string F(string pt, string en, string es, string id, object a) => string.Format(Inv, L(pt, en, es, id), a);
        static string F(string pt, string en, string es, string id, object a, object b) => string.Format(Inv, L(pt, en, es, id), a, b);
        static string F(string pt, string en, string es, string id, object a, object b, object c) => string.Format(Inv, L(pt, en, es, id), a, b, c);

        // ------------------------------------------------------------------ geral
        public static string Voltar => L("VOLTAR", "BACK", "VOLVER", "KEMBALI");
        public static string Continuar => L("CONTINUAR", "CONTINUE", "CONTINUAR", "LANJUT");
        public static string Jogar => L("JOGAR", "PLAY", "JUGAR", "MAIN");
        public static string Repetir => L("REPETIR", "REPLAY", "REPETIR", "ULANGI");
        public static string Resgatar => L("RESGATAR", "RESCUE", "RESCATAR", "SELAMATKAN");
        public static string Recusar => L("RECUSAR", "DECLINE", "RECHAZAR", "TOLAK");
        public static string Comprar => L("COMPRAR", "BUY", "COMPRAR", "BELI");
        public static string Maximo => L("MÁX", "MAX", "MÁX", "MAKS");
        public static string Versao(string v) => F("versão {0}", "version {0}", "versión {0}", "versi {0}", v);

        // ------------------------------------------------------------------ titulo e menu
        public const string Nome1 = "DUNGEON", Nome2 = "COURIER";   // nome do jogo: igual em todas as linguas
        public static string Slogan => L("Entregas pela masmorra", "Deliveries through the dungeon", "Entregas por la mazmorra", "Antar paket menembus dungeon");
        public static string TocarParaComecar => L("toque para começar", "tap to start", "toca para empezar", "ketuk untuk mulai");
        public static string PrivacidadeCurta => L("Seu progresso e o diário de teste ficam só neste aparelho. Nada é enviado sem você mandar.",
            "Your progress and the playtest log stay on this device. Nothing is sent unless you send it.",
            "Tu progreso y el registro de prueba quedan solo en este dispositivo. Nada se envía si tú no lo mandas.",
            "Progres dan catatan uji hanya ada di perangkat ini. Tak ada yang terkirim tanpa izinmu.");
        public static string MenuJogar => L("JOGAR", "PLAY", "JUGAR", "MAIN");
        public static string MenuGuilda => L("GUILDA", "GUILD", "GREMIO", "GUILD");
        public static string MenuConfig => L("CONFIGURAÇÕES", "SETTINGS", "AJUSTES", "PENGATURAN");
        public static string MenuCreditos => L("CRÉDITOS", "CREDITS", "CRÉDITOS", "KREDIT");
        public static string GuildaFechada => L("a guilda abre depois do 1º contrato", "the guild opens after the 1st contract",
            "el gremio abre tras el primer contrato", "guild terbuka setelah kontrak pertama");

        // ------------------------------------------------------------------ quadro de contratos
        /// <summary>Titulo do quadro (regiao 1). Os nomes das regioes 2-4 estao no glossario do docs/UI_V1.md.</summary>
        public static string Regiao => L("Porões da Guilda", "Guild Cellars", "Sótanos del Gremio", "Ruang Bawah Guild");
        public static string Estrelas(int tem, int total) => string.Format(Inv, "{0}/{1}", tem, total);
        public static string Dia => L("Contrato do dia", "Daily contract", "Contrato del día", "Kontrak harian");
        /// <summary>Rotulo curto do contrato nas telas: C12 / Dia / K7M2QX (o Core chama o do dia de "DIA"; o diario guarda o codigo cru).</summary>
        public static string Rotulo(ContractDef c) => c.Kind == ContractKind.Daily ? L("Dia", "Daily", "Día", "Harian") : c.Code;
        public static string DiaLinha(string carga, int moedas) => F("{0} · recompensa extra +{1} moedas", "{0} · extra reward +{1} coins",
            "{0} · recompensa extra +{1} monedas", "{0} · hadiah ekstra +{1} koin", carga, moedas);
        public static string DiaFeito => L("feito hoje: volte amanhã", "done today: come back tomorrow", "hecho hoy: vuelve mañana", "selesai hari ini: kembali besok");
        public static string DiaFechado => L("abre depois do C3", "opens after C3", "abre después del C3", "terbuka setelah C3");
        public static string Feito => L("FEITO", "DONE", "HECHO", "SELESAI");
        public static string DigitarCodigo => L("DIGITAR CÓDIGO", "ENTER CODE", "INGRESAR CÓDIGO", "MASUKKAN KODE");

        // ------------------------------------------------------------------ contrato por codigo
        public static string CodigoTitulo => L("Contrato por código", "Contract by code", "Contrato por código", "Kontrak lewat kode");
        public static string CodigoCorpo => L("Digite o código de 6 caracteres que um amigo mandou (letras e números 2-9; não tem I, O, 0 nem 1).",
            "Type the 6-character code a friend sent you (letters and digits 2-9; no I, O, 0 or 1).",
            "Escribe el código de 6 caracteres que te envió un amigo (letras y números 2-9; sin I, O, 0 ni 1).",
            "Ketik kode 6 karakter dari temanmu (huruf dan angka 2-9; tanpa I, O, 0, dan 1).");
        public static string CodigoDica => L("ex.: K7M2QX", "e.g. K7M2QX", "ej.: K7M2QX", "contoh: K7M2QX");
        public static string CodigoInvalido => L("Código inválido: confira os 6 caracteres com quem mandou.",
            "Invalid code: check the 6 characters with whoever sent it.",
            "Código inválido: revisa los 6 caracteres con quien te lo envió.",
            "Kode tidak valid: cek lagi 6 karakternya dengan pengirimnya.");
        public static string JogarCodigo => L("JOGAR CÓDIGO", "PLAY CODE", "JUGAR CÓDIGO", "MAINKAN KODE");

        // ------------------------------------------------------------------ guilda e armas
        public static string Guilda => L("Guilda", "Guild", "Gremio", "Guild");
        public static string AbaMelhorias => L("MELHORIAS", "UPGRADES", "MEJORAS", "PENINGKATAN");
        public static string AbaArmas => L("ARMAS", "WEAPONS", "ARMAS", "SENJATA");
        public static string Equipar => L("EQUIPAR", "EQUIP", "EQUIPAR", "PAKAI");
        public static string Equipada => L("EQUIPADA", "EQUIPPED", "EQUIPADA", "DIPAKAI");
        public static string Melhorar => L("MELHORAR", "UPGRADE", "MEJORAR", "NAIKKAN");
        public static string PrecisaEstrelas(int n) => F("precisa de {0} estrelas", "needs {0} stars", "requiere {0} estrellas", "butuh {0} bintang", n);
        public static string ArmaNivel(int lv, int max) => F("nível {0}/{1}", "level {0}/{1}", "nivel {0}/{1}", "level {0}/{1}", lv, max);

        public static string UpNome(Upgrade u)
        {
            switch (u)
            {
                case Upgrade.Life: return L("Vida", "Health", "Vida", "Nyawa");
                case Upgrade.Dash: return "Dash";
                case Upgrade.Bag: return L("Bolsa", "Purse", "Bolsa", "Dompet");
                case Upgrade.Pack: return L("Mochila", "Backpack", "Mochila", "Ransel");
                default: return u.ToString();
            }
        }

        public static string UpEfeito(Upgrade u)
        {
            switch (u)
            {
                case Upgrade.Life: return L("+1 de vida máxima", "+1 max health", "+1 de vida máxima", "+1 nyawa maksimal");
                case Upgrade.Dash: return L("-10% na recarga do dash", "-10% dash cooldown", "-10% de recarga del dash", "-10% cooldown dash");
                case Upgrade.Bag: return L("+10% de moedas", "+10% coins", "+10% de monedas", "+10% koin");
                case Upgrade.Pack: return L("pega de mais longe e pousa mais rápido", "longer pickup reach, faster landing",
                    "recoge de más lejos y deja la carga más rápido", "ambil dari lebih jauh, taruh lebih cepat");
                default: return "";
            }
        }

        public static string Nivel(int lv) => ArmaNivel(lv, Balance.UpgradeMax);

        /// <summary>Nome da arma: pt do Core (WeaponDef.Name); o resto pela tabela.</summary>
        public static string ArmaNome(Weapon w)
        {
            string pt = WeaponDef.All[(int)w].Name;
            switch (w)
            {
                case Weapon.Sling: return L(pt, "Slingshot", "Honda", "Ketapel");
                case Weapon.Boomerang: return L(pt, "Boomerang", "Bumerán", "Bumerang");
                case Weapon.Bomb: return L(pt, "Little bomb", "Bombita", "Bom kecil");
                default: return pt;
            }
        }

        public static string ArmaEfeito(Weapon w)
        {
            string pt = WeaponDef.All[(int)w].Text;
            switch (w)
            {
                case Weapon.Sling: return L(pt, "homing shot", "disparo teledirigido", "tembakan pelacak");
                case Weapon.Boomerang: return L(pt, "flies out and back in a line, piercing enemies", "va y vuelve en línea y atraviesa enemigos",
                    "pergi-pulang lurus dan menembus musuh");
                case Weapon.Bomb: return L(pt, "slow arcing area blast, weak vs fast foes; cracks the egg and sets off the keg",
                    "arco con daño en área, lento contra blancos rápidos; agrieta el huevo y detona el barril",
                    "lemparan ledak area, lambat ke target cepat; meretakkan telur dan meledakkan tong");
                default: return pt;
            }
        }

        // ------------------------------------------------------------------ perks (1 de 3, so na run)
        public static string PerkTitulo => L("Escolha 1 vantagem", "Pick 1 perk", "Elige 1 ventaja", "Pilih 1 perk");
        public static string PerkSub => L("vale até o fim deste contrato", "lasts until the end of this contract", "dura hasta el final de este contrato",
            "berlaku sampai kontrak ini selesai");

        /// <summary>Nome do perk: pt do Core (PerkDef.Name); o resto pela tabela.</summary>
        public static string PerkNome(Perk p)
        {
            string pt = PerkDef.All[(int)p].Name;
            switch (p)
            {
                case Perk.LightStep: return L(pt, "Light step", "Paso ligero", "Langkah Ringan");
                case Perk.QuickPlant: return L(pt, "Quick landing", "Descarga rápida", "Taruh Cepat");
                case Perk.DoubleDash: return L(pt, "Double dash", "Dash doble", "Dash Ganda");
                case Perk.Padded: return L(pt, "Padded cargo", "Carga acolchada", "Paket Berbantal");
                case Perk.QuickHand: return L(pt, "Quick hand", "Mano rápida", "Tangan Cepat");
                case Perk.Magnet: return L(pt, "Magnet", "Imán", "Magnet");
                case Perk.ExtraHeart: return L(pt, "Extra heart", "Corazón extra", "Hati Ekstra");
                case Perk.Lookout: return L(pt, "Lookout", "Vigía", "Pengawas");
                case Perk.IronBoot: return L(pt, "Iron boot", "Bota de hierro", "Sepatu Besi");
                case Perk.ChestLuck: return L(pt, "Chest luck", "Suerte de cofre", "Hoki Peti");
                case Perk.Spring: return L(pt, "Spring", "Resorte", "Pegas");
                case Perk.SecondWind: return L(pt, "Second wind", "Segundo aire", "Napas Kedua");
                case Perk.ChestSense: return L(pt, "Chest sense", "Olfato de cofre", "Indra Peti");
                case Perk.SureFoot: return L(pt, "Sure foot", "Pie firme", "Kaki Kokoh");
                case Perk.Insulated: return L(pt, "Insulated", "Aislante", "Isolasi");
                case Perk.FirmGrip: return L(pt, "Firm grip", "Asa firme", "Pegangan Kuat");
                case Perk.Counter: return L(pt, "Counter", "Contragolpe", "Serangan Balik");
                case Perk.Hunter: return L(pt, "Hunter", "Cazador", "Pemburu");
                case Perk.ThickSkin: return L(pt, "Thick skin", "Piel gruesa", "Kulit Tebal");
                case Perk.CatEyes: return L(pt, "Cat eyes", "Ojos de gato", "Mata Kucing");
                case Perk.Whistle: return L(pt, "Whistle", "Silbato", "Peluit");
                case Perk.FirstAid: return L(pt, "First aid", "Primeros auxilios", "Pertolongan Pertama");
                case Perk.Runner: return L(pt, "Runner", "Corredor", "Pelari");
                case Perk.CargoShield: return L(pt, "Cargo shield", "Escudo de carga", "Perisai Paket");
                case Perk.Scout: return L(pt, "Scout", "Explorador", "Pengintai");
                default: return pt;
            }
        }

        public static string PerkEfeito(Perk p)
        {
            string pt = PerkDef.All[(int)p].Text;
            switch (p)
            {
                case Perk.LightStep: return L(pt, "+10% speed while carrying", "+10% de velocidad con la carga", "+10% kecepatan saat membawa paket");
                case Perk.QuickPlant: return L(pt, "sets down in 0.1 s", "deja la carga en 0,1 s", "menaruh paket dalam 0,1 dtk");
                case Perk.DoubleDash: return L(pt, "2 dash charges", "2 usos de dash", "2 kali dash");
                case Perk.Padded: return L(pt, "the 1st hit on the cargo is ignored", "el primer golpe a la carga se ignora", "serangan pertama ke paket diabaikan");
                case Perk.QuickHand: return L(pt, "+20% fire rate", "+20% de cadencia de tiro", "+20% kecepatan tembak");
                case Perk.Magnet: return L(pt, "opens chests from farther away", "abre cofres desde más lejos", "buka peti dari lebih jauh");
                case Perk.ExtraHeart: return L(pt, "+2 max health and heals 2", "+2 de vida máxima y cura 2", "+2 nyawa maks dan pulih 2");
                case Perk.Lookout: return L(pt, "the thief takes +4 s to reach unattended cargo", "el ladrón tarda +4 s en llegar a la carga sola",
                    "pencuri butuh +4 dtk ke paket yang ditinggal");
                case Perk.IronBoot: return L(pt, "dash pushes harder and stuns for 0.6 s", "el dash empuja más y aturde 0,6 s", "dash mendorong lebih kuat dan stun 0,6 dtk");
                case Perk.ChestLuck: return L(pt, "+30% chest coins", "+30% de monedas de cofre", "+30% koin dari peti");
                case Perk.Spring: return L(pt, "dash +40% distance", "dash +40% de distancia", "jarak dash +40%");
                case Perk.SecondWind: return L(pt, "once per contract, get back up with 2 health", "1 vez por contrato, vuelves con 2 de vida",
                    "1x per kontrak, bangkit dengan 2 nyawa");
                case Perk.ChestSense: return L(pt, "doors show what's in the chest", "la puerta muestra el contenido del cofre", "pintu menunjukkan isi peti");
                case Perk.SureFoot: return L(pt, "water, mud and webs don't slow you", "agua, lodo y telarañas no te frenan", "air, lumpur, dan jaring tak memperlambat");
                case Perk.Insulated: return L(pt, "ice melts 40% slower; the egg hatches 20% later", "el hielo se derrite 40% más lento; el huevo eclosiona 20% más tarde",
                    "es mencair 40% lebih lambat; telur menetas 20% lebih lama");
                case Perk.FirmGrip: return L(pt, "the 1st dash in each room keeps the cargo", "el primer dash de cada sala lleva la carga",
                    "dash pertama di tiap ruangan tetap membawa paket");
                case Perk.Counter: return L(pt, "dashing into whoever holds the cargo knocks it loose", "el dash contra quien lleva la carga la hace caer",
                    "dash ke yang memegang paket membuatnya jatuh");
                case Perk.Hunter: return L(pt, "+2 coins per defeated enemy", "+2 monedas por enemigo derrotado", "+2 koin per musuh yang dikalahkan");
                case Perk.ThickSkin: return L(pt, "-1 damage from hits of 2 or more", "-1 al daño de golpes de 2 o más", "-1 kerusakan dari serangan 2 atau lebih");
                case Perk.CatEyes: return L(pt, "courier light +60% in the dark", "luz del courier +60% en la oscuridad", "cahaya kurir +60% dalam gelap");
                case Perk.Whistle: return L(pt, "a thief who grabs the cargo freezes for 1.5 s", "el ladrón que toma la carga se queda quieto 1,5 s",
                    "pencuri yang ambil paket diam 1,5 dtk");
                case Perk.FirstAid: return L(pt, "+1 health when opening a chest", "+1 de vida al abrir un cofre", "+1 nyawa saat buka peti");
                case Perk.Runner: return L(pt, "+20% speed without the cargo", "+20% de velocidad sin la carga", "+20% kecepatan tanpa paket");
                case Perk.CargoShield: return L(pt, "the 1st hit per room on the set-down cargo is ignored", "el primer golpe por sala a la carga en el suelo se ignora",
                    "serangan pertama per ruangan ke paket di lantai diabaikan");
                case Perk.Scout: return L(pt, "doors show the exact number of enemies", "las puertas muestran el número exacto de enemigos",
                    "pintu menunjukkan jumlah musuh yang pasti");
                default: return pt;
            }
        }

        // ------------------------------------------------------------------ contrato em curso
        public static string Pausado => L("Pausado", "Paused", "En pausa", "Dijeda");
        public static string Retomado => L("Contrato em curso", "Contract in progress", "Contrato en curso", "Kontrak berjalan");
        public static string Desistir => L("DESISTIR DO CONTRATO", "ABANDON CONTRACT", "ABANDONAR CONTRATO", "BATALKAN KONTRAK");
        public static string DesistirTitulo => L("Desistir do contrato?", "Abandon the contract?", "¿Abandonar el contrato?", "Batalkan kontrak?");
        public static string DesistirCorpo => L("O contrato falha e a carga NÃO fica na masmorra para resgate. As moedas dos baús ficam com você.",
            "The contract fails and the cargo will NOT stay in the dungeon for rescue. You keep the coins from chests.",
            "El contrato falla y la carga NO queda en la mazmorra para rescatarla. Te quedas con las monedas de los cofres.",
            "Kontrak gagal dan paket TIDAK tertinggal di dungeon untuk diselamatkan. Koin dari peti tetap milikmu.");
        public static string DesistirSim => L("SIM, DESISTIR", "YES, ABANDON", "SÍ, ABANDONAR", "YA, BATALKAN");
        public static string PausaLinha(string codigo, string carga, string tempo) => string.Format(Inv, "{0} · {1} · {2}", codigo, carga, tempo);
        public static string Pular => L("toque para pular", "tap to skip", "toca para saltar", "ketuk untuk lewati");
        public static string SaveRecusado => L("Este save é de uma versão mais nova do jogo. Atualize: nada será jogado nem gravado aqui.",
            "This save is from a newer version of the game. Please update: nothing will be played or saved here.",
            "Esta partida guardada es de una versión más nueva del juego. Actualiza: aquí no se jugará ni se guardará nada.",
            "Data simpanan ini dari versi game yang lebih baru. Perbarui dulu: tak ada yang dimainkan atau disimpan di sini.");
        public static string OfertaTitulo => L("Carga perdida", "Lost cargo", "Carga perdida", "Paket hilang");
        public static string AvisoPendente => L("A carga ficou na masmorra: o próximo contrato oferece o resgate.",
            "The cargo was left in the dungeon: the next contract offers a rescue.",
            "La carga quedó en la mazmorra: el próximo contrato ofrece el rescate.",
            "Paket tertinggal di dungeon: kontrak berikutnya menawarkan penyelamatan.");
        // rotulos que flutuam no mundo (WorldView, WorldViewV1): curtos, porque saem centrados no courier perto da parede
        public static string Rachou => L("rachou!", "cracked!", "¡se agrietó!", "retak!");
        public static string Quebrou => L("quebrou!", "broke!", "¡se rompió!", "pecah!");
        public static string Ladrao => L("ladrão!", "thief!", "¡ladrón!", "pencuri!");
        public static string Fugiu => L("fugiu!", "got away!", "¡escapó!", "kabur!");
        public static string Chocou => L("chocou!", "hatched!", "¡eclosionó!", "menetas!");
        public static string PassagemSecreta => L("passagem secreta!", "secret passage!", "¡pasaje secreto!", "jalan rahasia!");
        public static string MensagemAbre => L("a mensagem abre a porta!", "the message opens the door!", "¡el mensaje abre la puerta!", "pesan membuka pintu!");
        public static string Reforco => L("reforço!", "reinforcements!", "¡refuerzos!", "bala bantuan!");
        public static string TempoEsgotado => L("tempo esgotado!", "time's up!", "¡sin tiempo!", "waktu habis!");
        public static string Bum => L("BUM!", "BOOM!", "¡BUM!", "DUAR!");

        /// <summary>Dica da HUD pelo enum Hint do Core. pt = Hints.Text (fonte do Core); o resto pela tabela; sem traducao = EN ou Core.
        /// So literais: a HUD chama todo quadro e compara por referencia, nada aloca.</summary>
        public static string Dica(Hint h)
        {
            string pt = Hints.Text(h);
            switch (h)
            {
                case Hint.PlantToFight: return L(pt, "release your finger to set down and fight", "suelta el dedo para dejar la carga y pelear",
                    "lepas jari untuk menaruh paket dan bertarung");
                case Hint.PickUp: return L(pt, "drag near the cargo to pick it up", "arrastra cerca de la carga para recogerla", "geser dekat paket untuk mengambilnya");
                case Hint.Dash: return L(pt, "quick tap = dash (drops the cargo)", "toque rápido = dash (suelta la carga)", "ketuk cepat = dash (paket terjatuh)");
                case Hint.Thief: return L(pt, "thief! it wants your cargo", "¡ladrón! quiere tu carga", "pencuri! dia mengincar paketmu");
                case Hint.DoorNeedsCargo: return L(pt, "the door only opens with the cargo in hand", "la puerta solo abre con la carga en las manos",
                    "pintu hanya terbuka jika paket di tangan");
                case Hint.GuardDoor: return L(pt, "defeat the guard to open the door", "vence al guardia para abrir la puerta", "kalahkan penjaga untuk membuka pintu");
                case Hint.Chest: return L(pt, "stand still next to the chest to open it", "quédate quieto junto al cofre para abrirlo", "diam di samping peti untuk membukanya");
                case Hint.WaitPet: return L(pt, "the door only opens with the chick nearby", "la puerta solo abre con la cría cerca",
                    "pintu hanya terbuka jika anak naga di dekatmu");
                case Hint.DragToWalk: return L(pt, "drag your finger to walk with the cargo", "arrastra el dedo para caminar con la carga",
                    "geser jari untuk berjalan membawa paket");
                case Hint.CakeNoDash: return L(pt, "the cake can't take a dash: just walk", "el pastel no aguanta el dash: solo camina", "kue tak tahan dash: jalan saja");
                case Hint.BarrelFuse: return L(pt, "a keg on the ground explodes in 3 s: pick it back up!", "el barril en el suelo explota en 3 s: ¡recógelo!",
                    "tong di lantai meledak dalam 3 dtk: ambil lagi!");
                case Hint.ArcherAim: return L(pt, "archer aiming: get out of the line", "arquero apuntando: sal de la línea", "pemanah membidik: keluar dari garis");
                case Hint.SecretWall: return L(pt, "cracked wall: walk through it with the cargo", "pared agrietada: atraviésala con la carga",
                    "dinding retak: tembus sambil membawa paket");
                case Hint.MessageDoor: return L(pt, "the message opens the locked door", "el mensaje abre la puerta cerrada", "pesan membuka pintu yang terkunci");
                // lancamento (regioes 2-4): o pt e o do Core; as raias de regiao ligam
                case Hint.Water: return L(pt, "water slows everyone down: the thief too", "el agua frena a todos: al ladrón también", "air memperlambat semua: pencuri juga");
                case Hint.CartBell: return L(pt, "minecart! get off the track", "¡vagoneta! sal de la vía", "lori! menjauh dari rel");
                case Hint.Asleep: return L(pt, "sleeping miner: pass more than 2 m away", "minero dormido: pasa a más de 2 m", "penambang tidur: lewat lebih dari 2 m");
                case Hint.MoleTrail: return L(pt, "mole underground: carry the cargo", "topo bajo tierra: lleva la carga", "tikus tanah di bawah: bawa paketnya");
                case Hint.FrogTongue: return L(pt, "frog: don't set down within 3 m of it", "sapo: no dejes la carga a menos de 3 m", "katak: jangan taruh paket dalam 3 m");
                case Hint.BeetleFuse: return L(pt, "beetle about to blow: dash or shoot from afar", "el escarabajo va a explotar: dash o dispara de lejos",
                    "kumbang akan meledak: dash atau tembak dari jauh");
                case Hint.HeartPull: return L(pt, "the heart attracts enemies and coins", "el corazón atrae enemigos y monedas", "jantung menarik musuh dan koin");
                case Hint.IceMelt: return L(pt, "the ice is melting: go through water", "el hielo se derrite: pasa por el agua", "es mencair: lewati air");
                case Hint.CageScream: return L(pt, "the griffin chick is about to scream: a dash calms it", "el grifo bebé va a gritar: el dash lo calma",
                    "anak grifon akan menjerit: dash menenangkannya");
                case Hint.Dark: return L(pt, "in the dark you can only aim at what's in the light", "en la oscuridad solo apuntas a lo que está en la luz",
                    "dalam gelap, hanya yang terkena cahaya bisa dibidik");
                case Hint.LanternOut: return L(pt, "the lantern went out: take it to a torch", "la linterna se apagó: llévala a una antorcha", "lentera padam: bawa ke obor");
                case Hint.Web: return L(pt, "web: dash tears through it", "telaraña: el dash la rompe", "jaring: dash merobeknya");
                case Hint.GhostCargo: return L(pt, "the ghost is taking the cargo: grab it back", "el fantasma se lleva la carga: recupérala", "hantu membawa paket: ambil kembali");
                case Hint.SkeletonRise: return L(pt, "fight near the cargo: the skeleton won't get back up", "pelea cerca de la carga: el esqueleto no se levanta",
                    "bertarung dekat paket: kerangka tak bangkit lagi");
                case Hint.MummyGrab: return L(pt, "grabbed: dash, or set down and fight", "atrapado: dash o deja la carga y pelea", "tertangkap: dash, atau taruh paket dan bertarung");
                case Hint.GargoyleWake: return L(pt, "the gargoyle wakes when the cargo is near", "la gárgola despierta con la carga cerca", "gargoyle bangun jika paket di dekatnya");
                case Hint.MirrorFront: return L(pt, "the mirror reflects what comes from the front", "el espejo refleja lo que viene de frente", "cermin memantulkan serangan dari depan");
                case Hint.BalloonRise: return L(pt, "a balloon on the ground floats off in 4 s: grab it!", "globo en el suelo escapa en 4 s: ¡agárralo!",
                    "balon di lantai terbang dalam 4 dtk: ambil!");
                case Hint.Lava: return L(pt, "dash enemies into the lava", "empuja enemigos a la lava con el dash", "dorong musuh ke lava dengan dash");
                case Hint.Vent: return L(pt, "glowing vent: fire incoming", "respiradero brillando: va a salir fuego", "lubang menyala: api akan keluar");
                case Hint.PlantBite: return L(pt, "the plant bites anyone close: step away", "la planta muerde a quien está cerca: da un paso",
                    "tanaman menggigit yang dekat: menjauh selangkah");
                case Hint.MapReveal: return L(pt, "the map shows buried chests", "el mapa muestra cofres enterrados", "peta menunjukkan peti terkubur");
                case Hint.ImpSwap: return L(pt, "the imp is about to swap places with the cargo", "el diablillo va a cambiar de lugar con la carga",
                    "iblis kecil akan bertukar tempat dengan paket");
                default: return pt;
            }
        }

        /// <summary>Linha da dica quando o Core nao tem dica (Hint.None): o objetivo da sala.</summary>
        public static string Objetivo(Run r) => r.Room.Delivery ? L("leve a carga até o cliente", "bring the cargo to the client", "lleva la carga al cliente", "antar paket ke pelanggan")
            : L("escolha uma porta no topo", "pick a door at the top", "elige una puerta arriba", "pilih pintu di atas");

        // ------------------------------------------------------------------ conteudo do Core (cargas, inimigos, salas)

        /// <summary>Nome da carga: pt do Core (CargoDef.Name); o resto pela chave do enum.</summary>
        public static string Carga(CargoType t)
        {
            string pt = (int)t >= 0 && (int)t < CargoDef.All.Length ? CargoDef.All[(int)t].Name : t.ToString();
            switch (t)
            {
                case CargoType.Box: return L(pt, "Crate", "Caja", "Kotak");
                case CargoType.Relic: return L(pt, "Fragile relic", "Reliquia frágil", "Relik rapuh");
                case CargoType.Egg: return L(pt, "Egg", "Huevo", "Telur");
                case CargoType.Cake: return L(pt, "Wedding cake", "Pastel de bodas", "Kue pengantin");
                case CargoType.Message: return L(pt, "Urgent message", "Mensaje urgente", "Pesan mendesak");
                case CargoType.Heavy: return L(pt, "Heavy crate", "Caja pesada", "Kotak berat");
                case CargoType.Barrel: return L(pt, "Powder keg", "Barril de pólvora", "Tong mesiu");
                case CargoType.ArcaneHeart: return L(pt, "Arcane heart", "Corazón arcano", "Jantung arkana");
                case CargoType.IceCrystal: return L(pt, "Ice crystal", "Cristal de hielo", "Kristal es");
                case CargoType.GriffinCage: return L(pt, "Griffin chick cage", "Jaula del grifo bebé", "Sangkar anak grifon");
                case CargoType.Lantern: return L(pt, "Lantern", "Linterna", "Lentera");
                case CargoType.Mirror: return L(pt, "Enchanted mirror", "Espejo encantado", "Cermin ajaib");
                case CargoType.Balloon: return L(pt, "Gas balloon", "Globo de gas", "Balon gas");
                case CargoType.Flytrap: return L(pt, "Flytrap", "Planta carnívora", "Tanaman karnivora");
                case CargoType.TreasureMap: return L(pt, "Treasure map", "Mapa del tesoro", "Peta harta karun");
                default: return pt;
            }
        }

        /// <summary>Nome do inimigo: pt do Core (EnemyDef.Name); o resto pela chave do enum.</summary>
        public static string Inimigo(EnemyType t)
        {
            string pt = (int)t >= 0 && (int)t < EnemyDef.All.Length ? EnemyDef.All[(int)t].Name : t.ToString();
            switch (t)
            {
                case EnemyType.Bat: return L(pt, "Bat", "Murciélago", "Kelelawar");
                case EnemyType.Thief: return L(pt, "Thief", "Ladrón", "Pencuri");
                case EnemyType.Guard: return L(pt, "Guard", "Guardia", "Penjaga");
                case EnemyType.Brute: return L(pt, "Brute", "Bruto", "Preman");
                case EnemyType.Ogre: return L(pt, "Ogre", "Ogro", "Ogre");
                case EnemyType.Archer: return L(pt, "Archer", "Arquero", "Pemanah");
                case EnemyType.Slime: return L(pt, "Slime", "Limo", "Slime");
                case EnemyType.Captain: return L(pt, "Thief Captain", "Capitán Ladrón", "Kapten Pencuri");
                case EnemyType.GoblinMiner: return L(pt, "Goblin miner", "Goblin minero", "Goblin penambang");
                case EnemyType.Mole: return L(pt, "Mole", "Topo", "Tikus tanah");
                case EnemyType.BogFrog: return L(pt, "Bog frog", "Sapo del pantano", "Katak rawa");
                case EnemyType.BombBeetle: return L(pt, "Bomb beetle", "Escarabajo bomba", "Kumbang bom");
                case EnemyType.Minecart: return L(pt, "Minecart", "Vagoneta", "Lori tambang");
                case EnemyType.Ghost: return L(pt, "Ghost", "Fantasma", "Hantu");
                case EnemyType.Skeleton: return L(pt, "Skeleton", "Esqueleto", "Kerangka");
                case EnemyType.Spider: return L(pt, "Spider", "Araña", "Laba-laba");
                case EnemyType.Mummy: return L(pt, "Mummy", "Momia", "Mumi");
                case EnemyType.Gargoyle: return L(pt, "Gargoyle", "Gárgola", "Gargoyle");
                case EnemyType.Salamander: return L(pt, "Salamander", "Salamandra", "Salamander");
                case EnemyType.Imp: return L(pt, "Imp", "Diablillo", "Iblis kecil");
                case EnemyType.KoboldBomber: return L(pt, "Kobold firebomber", "Kobold incendiario", "Kobold pembakar");
                case EnemyType.MagmaElemental: return L(pt, "Magma elemental", "Elemental de magma", "Elemental magma");
                case EnemyType.MoleQueen: return L(pt, "Mole Queen", "Reina Topo", "Ratu Tikus Tanah");
                case EnemyType.StoneGolem: return L(pt, "Stone Golem", "Gólem de Piedra", "Golem Batu");
                case EnemyType.SpiderWidow: return L(pt, "Widow Spider", "Viuda Araña", "Laba-laba Janda");
                case EnemyType.Necromancer: return L(pt, "Necromancer", "Nigromante", "Nekromanser");
                case EnemyType.KoboldForeman: return L(pt, "Kobold Foreman", "Capataz Kobold", "Mandor Kobold");
                case EnemyType.ElderDragon: return L(pt, "Elder Dragon", "Dragón Anciano", "Naga Purba");
                case EnemyType.MagmaShard: return L(pt, "Magma ember", "Brasa de magma", "Bara magma");
                case EnemyType.SalamanderEgg: return L(pt, "Salamander egg", "Huevo de salamandra", "Telur salamander");
                default: return pt;
            }
        }

        /// <summary>Nome da sala: pt do Core (RoomDef.Name); o resto pelo codigo da sala; sem traducao, o proprio nome do Core.</summary>
        public static string Sala(RoomDef r)
        {
            switch (r.Code)
            {
                case "R1": return L(r.Name, "Guard's Room", "Sala del Guardia", "Ruang Penjaga");
                case "R2": return L(r.Name, "1st Thief's Room", "Sala del Primer Ladrón", "Ruang Pencuri Pertama");
                case "R3": return L(r.Name, "Long Route with Chest", "Ruta Larga con Cofre", "Rute Panjang Berpeti");
                case "R4": return L(r.Name, "Swarm", "Enjambre", "Kawanan");
                case "R5": return L(r.Name, "Brute's Room", "Sala del Bruto", "Ruang Preman");
                case "R6": return L(r.Name, "Guard and Thief", "Guardia y Ladrón", "Penjaga dan Pencuri");
                case "R7": return L(r.Name, "Archer's Corridor", "Pasillo del Arquero", "Lorong Pemanah");
                case "R8": return L(r.Name, "Slime Puddle", "Charco de Limo", "Genangan Slime");
                case "R9": return L(r.Name, "Archers' Gallery", "Galería de Arqueros", "Galeri Pemanah");
                case "R10": return L(r.Name, "Ambush", "Emboscada", "Penyergapan");
                case "R11": return L(r.Name, "Brute and Archer", "Bruto y Arquero", "Preman dan Pemanah");
                case "R12": return L(r.Name, "Puddle Guard", "Guardia de los Charcos", "Penjaga Genangan");
                case "R13": return L(r.Name, "Thieves in Pairs", "Ladrones en Pareja", "Pencuri Berpasangan");
                case "R14": return L(r.Name, "Maze", "Laberinto", "Labirin");
                case "R15": return L(r.Name, "Double Guard", "Guardia Doble", "Penjaga Ganda");
                case "R16": return L(r.Name, "Final Hall", "Salón Final", "Aula Terakhir");
                case "S1": return L(r.Name, "Secret Storeroom", "Depósito Secreto", "Gudang Rahasia");
                case "S2": return L(r.Name, "Hidden Treasure", "Tesoro Escondido", "Harta Tersembunyi");
                case "M1": return L(r.Name, "Captain's Den", "Guarida del Capitán", "Sarang Kapten");
                case "B1": return L(r.Name, "Ogre's Lair", "Cueva del Ogro", "Gua Ogre");
                case "D1": return L(r.Name, "Delivery", "Entrega", "Pengantaran");
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
            string salas = n == 1 ? L("1 sala", "1 room", "1 sala", "1 ruang") : F("{0} salas", "{0} rooms", "{0} salas", "{0} ruang", n);
            string extra = boss ? L(" + chefe", " + boss", " + jefe", " + bos") : mini ? L(" + mini-chefe", " + mini-boss", " + minijefe", " + mini-bos") : "";
            return salas + extra + L(" + entrega", " + delivery", " + entrega", " + antar");
        }

        /// <summary>O verbo que a carga muda, numa linha (linha 2 do quadro).</summary>
        public static string Verbo(ContractDef c)
        {
            switch (c.Cargo)
            {
                case CargoType.Relic: return L("frágil: 3 rachaduras = quebra", "fragile: 3 cracks = broken", "frágil: 3 grietas = se rompe", "rapuh: 3 retakan = pecah");
                case CargoType.Egg: return F("choca em {0} s: o filhote tem de chegar", "hatches in {0} s: the chick must arrive",
                    "eclosiona en {0} s: la cría debe llegar", "menetas dalam {0} dtk: anak naga harus sampai", c.EggTarget.ToString("0", Inv));
                case CargoType.Cake: return L("sem dash: o toque rápido só balança", "no dash: a quick tap only wobbles", "sin dash: el toque rápido solo tambalea",
                    "tanpa dash: ketuk cepat cuma bergoyang");
                case CargoType.Message: return L("contra o relógio; abre portas trancadas", "against the clock; opens locked doors", "contra reloj; abre puertas cerradas",
                    "lawan waktu; membuka pintu terkunci");
                case CargoType.Heavy: return L("-25% de velocidade carregando", "-25% speed while carrying", "-25% de velocidad con la carga", "-25% kecepatan saat membawa");
                case CargoType.Barrel: return L("no chão por 3 s, explode", "explodes after 3 s on the ground", "3 s en el suelo y explota", "3 dtk di lantai, meledak");
                default: return c.Index == 0 ? L("primeiro contrato", "first contract", "primer contrato", "kontrak pertama")
                    : L("entregue inteira: bônus", "deliver it intact: bonus", "entrégala intacta: bono", "antar utuh: bonus");
            }
        }

        public static string Pendente(LostCargo p) => p.Contract >= 0
            ? F("Carga perdida: {0} (C{1}). O próximo contrato oferece o resgate.", "Lost cargo: {0} (C{1}). The next contract offers a rescue.",
                "Carga perdida: {0} (C{1}). El próximo contrato ofrece el rescate.", "Paket hilang: {0} (C{1}). Kontrak berikutnya menawarkan penyelamatan.",
                Carga(p.Type), p.Contract + 1)
            : F("Carga perdida: {0}. O próximo contrato oferece o resgate.", "Lost cargo: {0}. The next contract offers a rescue.",
                "Carga perdida: {0}. El próximo contrato ofrece el rescate.", "Paket hilang: {0}. Kontrak berikutnya menawarkan penyelamatan.", Carga(p.Type));

        public static string Oferta(LostCargo p, int bonus)
        {
            string origem = p.Contract >= 0
                ? F("{0} do C{1}", "The {0} from C{1}", "{0} del C{1}", "{0} dari C{1}", Carga(p.Type), p.Contract + 1)
                : F("{0} do último contrato", "The {0} from the last contract", "{0} del último contrato", "{0} dari kontrak terakhir", Carga(p.Type));
            if (p.Cracks > 0)
                return F("{0} ficou na masmorra com {1}.\nAceite e ela aparece no chão da 1ª sala. Entregue as duas e ganhe +{2} moedas.\nRecusou, ela se perde.",
                    "{0} was left in the dungeon with {1}.\nAccept and it shows up on the floor of the 1st room. Deliver both to earn +{2} coins.\nDecline and it is lost.",
                    "{0} quedó en la mazmorra con {1}.\nAcepta y aparecerá en el suelo de la 1.ª sala. Entrega las dos y gana +{2} monedas.\nSi la rechazas, se pierde.",
                    "{0} tertinggal di dungeon dengan {1}.\nTerima, dan paket muncul di lantai ruang pertama. Antar keduanya untuk +{2} koin.\nTolak, dan paket hilang.",
                    origem, Rachaduras(p.Cracks), bonus);
            return F("{0} ficou na masmorra.\nAceite e ela aparece no chão da 1ª sala. Entregue as duas e ganhe +{1} moedas.\nRecusou, ela se perde.",
                "{0} was left in the dungeon.\nAccept and it shows up on the floor of the 1st room. Deliver both to earn +{1} coins.\nDecline and it is lost.",
                "{0} quedó en la mazmorra.\nAcepta y aparecerá en el suelo de la 1.ª sala. Entrega las dos y gana +{1} monedas.\nSi la rechazas, se pierde.",
                "{0} tertinggal di dungeon.\nTerima, dan paket muncul di lantai ruang pertama. Antar keduanya untuk +{1} koin.\nTolak, dan paket hilang.",
                origem, bonus);
        }

        public static string Rachaduras(int n) => n == 1 ? L("1 rachadura", "1 crack", "1 grieta", "1 retakan")
            : F("{0} rachaduras", "{0} cracks", "{0} grietas", "{0} retakan", n);

        // ------------------------------------------------------------------ fim do contrato

        public static string Titulo(Outcome o)
        {
            switch (o)
            {
                case Outcome.Delivered: return L("Entregue!", "Delivered!", "¡Entregado!", "Terkirim!");
                case Outcome.Died: return L("Você caiu", "You fell", "Caíste", "Kamu tumbang");
                case Outcome.CargoStolen: return L("Roubaram a carga", "The cargo was stolen", "Robaron la carga", "Paket dicuri");
                case Outcome.CargoBroken: return L("A relíquia quebrou", "The relic broke", "La reliquia se rompió", "Relik pecah");
                case Outcome.PetDied: return L("O filhote não resistiu", "The chick didn't make it", "La cría no sobrevivió", "Anak naga tak selamat");
                case Outcome.Abandoned: return L("Contrato abandonado", "Contract abandoned", "Contrato abandonado", "Kontrak dibatalkan");
                case Outcome.TimeUp: return L("O tempo acabou", "Time ran out", "Se acabó el tiempo", "Waktu habis");
                case Outcome.Exploded: return L("O barril explodiu", "The keg exploded", "El barril explotó", "Tong meledak");
                case Outcome.Melted: return L("O cristal derreteu", "The crystal melted", "El cristal se derritió", "Kristal mencair");
                case Outcome.Escaped: return L("O balão escapou", "The balloon floated away", "El globo se escapó", "Balon terbang lepas");
                default: return L("Contrato falhou", "Contract failed", "El contrato falló", "Kontrak gagal");
            }
        }

        public static string Motivo(ContractResult r)
        {
            switch (r.Outcome)
            {
                case Outcome.Died:
                    return r.DeathRoom >= 0 && r.DeathRoom < Content.Rooms.Length
                        ? F("O courier caiu na sala {0}: {1}.", "The courier fell in room {0}: {1}.", "El courier cayó en la sala {0}: {1}.", "Kurir tumbang di ruang {0}: {1}.",
                            Content.Rooms[r.DeathRoom].Code, Sala(Content.Rooms[r.DeathRoom]))
                        : L("O courier caiu.", "The courier fell.", "El courier cayó.", "Kurir tumbang.");
                case Outcome.CargoStolen: return L("O ladrão escapou pela borda da sala com a carga.", "The thief escaped through the edge of the room with the cargo.",
                    "El ladrón escapó por el borde de la sala con la carga.", "Pencuri kabur lewat tepi ruangan membawa paket.");
                case Outcome.CargoBroken: return L("A 3ª rachadura quebrou a relíquia.", "The 3rd crack broke the relic.", "La 3.ª grieta rompió la reliquia.", "Retakan ke-3 memecahkan relik.");
                case Outcome.PetDied: return L("O filhote foi atingido duas vezes.", "The chick was hit twice.", "La cría recibió dos golpes.", "Anak naga terkena dua kali.");
                case Outcome.Abandoned: return L("Você desistiu do contrato.", "You abandoned the contract.", "Abandonaste el contrato.", "Kamu membatalkan kontrak.");
                case Outcome.TimeUp: return L("A mensagem urgente chegou tarde demais.", "The urgent message arrived too late.", "El mensaje urgente llegó demasiado tarde.",
                    "Pesan mendesak terlambat sampai.");
                case Outcome.Exploded: return L("O barril ficou 3 s no chão e explodiu.", "The keg sat on the ground for 3 s and exploded.",
                    "El barril estuvo 3 s en el suelo y explotó.", "Tong 3 dtk di lantai lalu meledak.");
                case Outcome.Melted: return L("O cristal de gelo derreteu antes da entrega.", "The ice crystal melted before delivery.",
                    "El cristal de hielo se derritió antes de la entrega.", "Kristal es mencair sebelum diantar.");
                case Outcome.Escaped: return L("O balão ficou no chão e escapou.", "The balloon was left on the ground and floated away.",
                    "El globo quedó en el suelo y se escapó.", "Balon tertinggal di lantai lalu terbang.");
                default: return "";
            }
        }

        /// <summary>Reacao do cliente ao abrir a caixa (cena da entrega).</summary>
        public static string Reacao(ContractResult r)
        {
            if (r.Hatched) return L("Chocou no caminho, mas o filhote chegou!", "It hatched on the way, but the chick made it!",
                "¡Eclosionó en el camino, pero la cría llegó!", "Menetas di jalan, tapi anak naga sampai!");
            if (r.Cracks <= 0) return L("Perfeito! Nenhuma rachadura.", "Perfect! Not a single crack.", "¡Perfecto! Ni una grieta.", "Sempurna! Tanpa retak.");
            if (r.Cracks == 1) return L("Bom! Só 1 rachadura.", "Good! Just 1 crack.", "¡Bien! Solo 1 grieta.", "Bagus! Cuma 1 retakan.");
            return F("Hmm... {0} rachaduras.", "Hmm... {0} cracks.", "Hmm... {0} grietas.", "Hmm... {0} retakan.", r.Cracks);
        }

        // estrelas do resultado (CONTRATO_V1 §7): entregue · carga intacta · sem morrer e no tempo-alvo
        public static string EstrelaEntregue => L("Entregue", "Delivered", "Entregado", "Terkirim");
        public static string EstrelaIntacta => L("Carga intacta", "Cargo intact", "Carga intacta", "Paket utuh");
        public static string EstrelaTempo => L("Sem cair e no tempo-alvo", "No falls, within target time", "Sin caer y a tiempo", "Tanpa tumbang, tepat waktu");
        public static string NovoRecorde => L("Novo recorde de estrelas neste contrato!", "New star record on this contract!",
            "¡Nuevo récord de estrellas en este contrato!", "Rekor bintang baru di kontrak ini!");
        public static string CodigoDoContrato(string code) => F("Código: {0}", "Code: {0}", "Código: {0}", "Kode: {0}", code);
        public static string Compartilhar => L("COMPARTILHAR", "SHARE", "COMPARTIR", "BAGIKAN");
        public static string Copiado => L("código copiado", "code copied", "código copiado", "kode disalin");
        public static string Convite(string code, int estrelas) => F("Fiz {1}/3 estrelas no contrato {0} do Dungeon Courier. Consegue mais? Código: {0}",
            "I got {1}/3 stars on Dungeon Courier contract {0}. Can you beat it? Code: {0}",
            "Saqué {1}/3 estrellas en el contrato {0} de Dungeon Courier. ¿Puedes superarlo? Código: {0}",
            "Aku dapat {1}/3 bintang di kontrak {0} Dungeon Courier. Bisa lebih baik? Kode: {0}", code, estrelas);

        /// <summary>×1,4 (virgula em pt, es e id; ponto em en).</summary>
        public static string Mul(float m) => "×" + (Lang == Lingua.En ? m.ToString("0.0#", Inv) : m.ToString("0.0#", Inv).Replace('.', ','));
        public static string Tempo(float s) => ((int)(s / 60f)) + ":" + ((int)(s % 60f)).ToString("00", Inv);

        public static string RotuloPagamento => L("Pagamento", "Payment", "Pago", "Bayaran");
        public static string RotuloTotal => L("Total", "Total", "Total", "Total");

        /// <summary>Pagamento detalhado (rotulo | valor) do ContractResult: base x carga x integridade x bolsa = pagamento; + baus + resgate
        /// (+ bonus do contrato do dia). No maximo 10 linhas (Hud._resLabel).</summary>
        public static string[][] Pagamento(ContractResult r, ContractDef c)
        {
            string tempo = L("Tempo", "Time", "Tiempo", "Waktu");
            if (!r.Delivered)
                return new[] { new[] { tempo, Tempo(r.Time) }, new[] { L("Moedas dos baús", "Chest coins", "Monedas de cofres", "Koin dari peti"), "+" + r.ChestCoins },
                    new[] { RotuloTotal, "+" + r.Total } };
            var rows = new List<string[]>
            {
                new[] { L("Pagamento base", "Base pay", "Pago base", "Bayaran dasar"), r.Base.ToString(Inv) },
                new[] { r.Hatched ? F("Carga: {0} (chocou)", "Cargo: {0} (hatched)", "Carga: {0} (eclosionó)", "Paket: {0} (menetas)", Carga(c.Cargo))
                    : F("Carga: {0}", "Cargo: {0}", "Carga: {0}", "Paket: {0}", Carga(c.Cargo)), Mul(r.CargoMul) },
                new[] { F("Integridade: {0}", "Integrity: {0}", "Integridad: {0}", "Keutuhan: {0}", Rachaduras(r.Cracks)), Mul(r.IntegrityMul) },
                new[] { L("Bolsa da guilda", "Guild purse", "Bolsa del gremio", "Dompet guild"), Mul(r.BagMul) },
                new[] { RotuloPagamento, r.Payment.ToString(Inv) },
                new[] { L("Baús", "Chests", "Cofres", "Peti"), "+" + r.ChestCoins },
                new[] { r.RescueAccepted && !r.RescueDelivered ? L("Resgate (não entregue)", "Rescue (not delivered)", "Rescate (no entregado)", "Penyelamatan (tak terkirim)")
                    : L("Resgate", "Rescue", "Rescate", "Penyelamatan"), "+" + r.RescueBonus },
            };
            if (r.DailyBonus > 0) rows.Add(new[] { L("Bônus do contrato do dia", "Daily contract bonus", "Bono del contrato del día", "Bonus kontrak harian"), "+" + r.DailyBonus });
            rows.Add(new[] { tempo, Tempo(r.Time) });
            rows.Add(new[] { RotuloTotal, "+" + r.Total });
            return rows.ToArray();
        }

        // ------------------------------------------------------------------ configuracoes, creditos, privacidade
        public static string Config => L("Configurações", "Settings", "Ajustes", "Pengaturan");
        public static string Som => L("Som", "Sound", "Sonido", "Suara");
        public static string Musica => L("Música", "Music", "Música", "Musik");
        public static string Volume(string nome, int pct) => string.Format(Inv, "{0}: {1}%", nome, pct);
        public static string Vibra(bool on) => on ? L("Vibração: ligada", "Vibration: on", "Vibración: activada", "Getar: aktif")
            : L("Vibração: desligada", "Vibration: off", "Vibración: desactivada", "Getar: mati");
        public static string VibraSoCelular => L("Vibração: só no celular", "Vibration: phone only", "Vibración: solo en el celular", "Getar: khusus ponsel");
        public static string Idioma => L("Idioma", "Language", "Idioma", "Bahasa");
        public static string ExportarDiario => L("EXPORTAR DIÁRIO", "EXPORT LOG", "EXPORTAR REGISTRO", "EKSPOR CATATAN");
        public static string ExportarAjuda => L("O diário (CSV) registra eventos do jogo para o teste, sem dados pessoais. Só sai do aparelho se você exportar.",
            "The log (CSV) records game events for the playtest, with no personal data. It only leaves the device if you export it.",
            "El registro (CSV) guarda eventos del juego para la prueba, sin datos personales. Solo sale del dispositivo si lo exportas.",
            "Catatan (CSV) merekam kejadian game untuk uji coba, tanpa data pribadi. Hanya keluar dari perangkat jika kamu ekspor.");
        public static string ExportPasta => L("Pasta do diário aberta.", "Log folder opened.", "Carpeta del registro abierta.", "Folder catatan dibuka.");
        public static string ExportEscolha => L("Escolha o app para enviar o diário.", "Pick an app to send the log.", "Elige la app para enviar el registro.",
            "Pilih aplikasi untuk mengirim catatan.");
        public static string ExportVazio => L("Ainda não há diário: jogue um contrato primeiro.", "No log yet: play a contract first.",
            "Aún no hay registro: juega un contrato primero.", "Belum ada catatan: mainkan kontrak dulu.");
        public static string ExportErro => L("Não foi possível exportar o diário.", "Could not export the log.", "No se pudo exportar el registro.", "Gagal mengekspor catatan.");
        public static string IdAnonimo(string id) => F("id anônimo do teste: {0}", "anonymous test id: {0}", "id anónimo de prueba: {0}", "id anonim uji: {0}", id);

        public static string Creditos => L("Créditos", "Credits", "Créditos", "Kredit");
        public static string CreditosCorpo => L(
            "Um jogo da V-STACK\n\nCriação, direção e produção\nVinicius (V-STACK)\n\nModelos 3D gerados com IA (Tripo)\nAnimações Mixamo e Blender\nSons e músicas sintetizados por código\nFeito com Unity",
            "A V-STACK game\n\nCreated, directed and produced by\nVinicius (V-STACK)\n\n3D models generated with AI (Tripo)\nAnimations by Mixamo and Blender\nSounds and music synthesized in code\nMade with Unity",
            "Un juego de V-STACK\n\nCreación, dirección y producción\nVinicius (V-STACK)\n\nModelos 3D generados con IA (Tripo)\nAnimaciones de Mixamo y Blender\nSonidos y música sintetizados por código\nHecho con Unity",
            "Game dari V-STACK\n\nKreasi, arahan, dan produksi\nVinicius (V-STACK)\n\nModel 3D dibuat dengan AI (Tripo)\nAnimasi dari Mixamo dan Blender\nSuara dan musik disintesis lewat kode\nDibuat dengan Unity");
        public static string Privacidade => L("Privacidade: o jogo não coleta dados pessoais e não usa a internet. O progresso e o diário de teste ficam só neste aparelho; nada sai dele sem você tocar em Exportar diário.",
            "Privacy: the game collects no personal data and does not use the internet. Progress and the playtest log stay on this device; nothing leaves it unless you tap Export log.",
            "Privacidad: el juego no recopila datos personales y no usa internet. El progreso y el registro de prueba quedan solo en este dispositivo; nada sale de él si no tocas Exportar registro.",
            "Privasi: game ini tidak mengumpulkan data pribadi dan tidak memakai internet. Progres dan catatan uji hanya ada di perangkat ini; tak ada yang keluar kecuali kamu ketuk Ekspor catatan.");

        public static string Num(int v) => v.ToString(Inv);
        /// <summary>Relogio da HUD: "s" (simbolo SI) em todas as linguas, cabe no espaco curto da faixa; o texto corrido do ID usa "dtk".</summary>
        public static string Segundos(float s) => Math.Max(0, (int)Math.Ceiling(s)).ToString(Inv) + " s";
    }
}
