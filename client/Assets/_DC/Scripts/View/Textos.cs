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
        static string F(string pt, string en, string es, string id, object a, object b, object c, object d) => string.Format(Inv, L(pt, en, es, id), a, b, c, d);

        // ------------------------------------------------------------------ geral
        public static string Voltar => L("VOLTAR", "BACK", "VOLVER", "KEMBALI");
        public static string Continuar => L("CONTINUAR", "CONTINUE", "CONTINUAR", "LANJUT");
        public static string Jogar => L("JOGAR", "PLAY", "JUGAR", "MAIN");
        public static string Repetir => L("REPETIR", "REPLAY", "REPETIR", "ULANGI");
        public static string Resgatar => L("RESGATAR", "RESCUE", "RESCATAR", "SELAMATKAN");
        public static string Recusar => L("RECUSAR", "DECLINE", "RECHAZAR", "TOLAK");
        public static string Comprar => L("COMPRAR", "BUY", "COMPRAR", "BELI");
        public static string Maximo => L("MÁX", "MAX", "MÁX", "MAKS");
        /// <summary>Equipamento no 5 antes de vencer o Dragao: os niveis 6-8 (fim de jogo) abrem depois dele.</summary>
        public static string GearDragao => L("6-8: DRAGÃO", "6-8: DRAGON", "6-8: DRAGÓN", "6-8: NAGA");
        public static string Versao(string v) => F("versão {0}", "version {0}", "versión {0}", "versi {0}", v);

        // ------------------------------------------------------------------ titulo e menu
        public static string Slogan => L("Entregas pela masmorra", "Deliveries through the dungeon", "Entregas por la mazmorra", "Antar paket menembus dungeon");
        public static string TocarParaComecar => L("toque para começar", "tap to start", "toca para empezar", "ketuk untuk mulai");
        /// <summary>O Hud liga quando o anuncio real (LevelPlay, com app key) nasceu: os textos de privacidade passam a citar os anuncios
        /// (nao personalizados, sem ID de publicidade) e a politica publica (docs/LOJA.md §4).</summary>
        public static bool AnunciosReais;
        const string Politica = "vvs2705.github.io/dungeon-courier";
        public static string PrivacidadeCurta => AnunciosReais
            ? L("Seu progresso fica só neste aparelho. Os anúncios usam a internet e não são personalizados.",
                "Your progress stays on this device. Ads use the internet and are not personalized.",
                "Tu progreso queda solo en este dispositivo. Los anuncios usan internet y no son personalizados.",
                "Progresmu hanya ada di perangkat ini. Iklan memakai internet dan tidak dipersonalisasi.")
            : L("Seu progresso e o diário de teste ficam só neste aparelho. Nada é enviado sem você mandar.",
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
        public static string Rotulo(ContractDef c)
        {
            switch (c.Kind)
            {
                case ContractKind.Daily: return L("Dia", "Daily", "Día", "Harian");
                case ContractKind.Rare: return L("Raro", "Rare", "Raro", "Langka");
                case ContractKind.Event: return L("Evento", "Event", "Evento", "Event");
                case ContractKind.Endless: return L("Sem fim", "Endless", "Sin fin", "Tanpa akhir");
                default: return c.Code;
            }
        }
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
        public static string Estourou => L("estourou!", "popped!", "¡reventó!", "meletus!");
        public static string DanoEmDobro => L("dano em dobro!", "double damage!", "¡daño doble!", "pukulan x2!");

        // HUD das cargas novas (regioes 2-4): no lugar do relogio da faixa de cima (cabe em ~200 px: encolhe ate 18)
        public static string Gelo => L("gelo", "ice", "hielo", "es");
        public static string LanternaApagada => L("lanterna apagada", "lantern out", "linterna apagada", "lentera padam");
        public static string EspelhoRachaduras(int n, int max) => F("{0}/{1} rachaduras", "{0}/{1} cracks", "{0}/{1} grietas", "{0}/{1} retakan", n, max);

        /// <summary>Faixa da fase do chefe (BossPhase: A = chefe, B = fase nova), CONTEUDO_REGIOES §5. null = sem faixa.</summary>
        public static string FaseChefe(EnemyType t, int fase)
        {
            switch (t)
            {
                case EnemyType.MoleQueen: return L("a Rainha emerge duas vezes!", "the Queen bursts out twice!", "¡la Reina emerge dos veces!", "Ratu muncul dua kali!");
                case EnemyType.StoneGolem: return L("o Golem vai investir!", "the Golem is about to charge!", "¡el Gólem va a embestir!", "Golem akan menyeruduk!");
                case EnemyType.SpiderWidow: return L("a Viúva sobe ao teto!", "the Widow climbs to the ceiling!", "¡la Viuda sube al techo!", "Janda naik ke langit-langit!");
                case EnemyType.Necromancer: return fase >= 3
                    ? L("o Necromante apaga a luz!", "the Necromancer dims the light!", "¡el Nigromante apaga la luz!", "Nekromanser meredupkan cahaya!")
                    : L("o Necromante dispara raio duplo!", "the Necromancer fires twin beams!", "¡el Nigromante lanza rayo doble!", "Nekromanser menembak sinar ganda!");
                case EnemyType.KoboldForeman: return L("o Capataz gira o chicote!", "the Foreman spins his whip!", "¡el Capataz gira el látigo!", "Mandor memutar cambuknya!");
                case EnemyType.ElderDragon: return fase >= 3
                    ? L("o Dragão varre o fogo!", "the Dragon sweeps its fire!", "¡el Dragón barre el fuego!", "Naga menyapu api!")
                    : L("o Dragão levanta voo!", "the Dragon takes flight!", "¡el Dragón alza el vuelo!", "Naga terbang!");
                default: return null;
            }
        }

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
                // o pt do Core ("de um passo") nao diz como: o toque rapido (dash) e o jeito de sair do alcance
                case Hint.PlantBite: return L("a planta morde quem fica perto: toque rápido para se afastar", "the plant bites anyone close: quick tap to dash away",
                    "la planta muerde a quien está cerca: toque rápido para alejarte", "tanaman menggigit yang dekat: ketuk cepat untuk menjauh");
                case Hint.MapReveal: return L(pt, "the map shows buried chests", "el mapa muestra cofres enterrados", "peta menunjukkan peti terkubur");
                case Hint.ImpSwap: return L(pt, "the imp is about to swap places with the cargo", "el diablillo va a cambiar de lugar con la carga",
                    "iblis kecil akan bertukar tempat dengan paket");
                default: return pt;
            }
        }

        /// <summary>Linha da dica quando o Core nao tem dica (Hint.None): o objetivo da sala.</summary>
        public static string Objetivo(Run r) => r.Room.Delivery ? L("leve a carga até o cliente", "bring the cargo to the client", "lleva la carga al cliente", "antar paket ke pelanggan")
            : r.Doors.Count > 0 && r.Doors[0].Locked
                ? r.Room.Boss || r.Room.Mini ? L("derrote o chefe para abrir a porta", "defeat the boss to open the door", "vence al jefe para abrir la puerta", "kalahkan bos untuk membuka pintu")
                    : L("derrote os guardas para abrir a porta", "defeat the guards to open the door", "vence a los guardias para abrir la puerta", "kalahkan para penjaga untuk membuka pintu")
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
                // lancamento (CONTRATO_LANCAMENTO §3: o verbo de cada carga nova)
                case CargoType.ArcaneHeart: return L("atrai moedas e inimigos", "attracts coins and enemies", "atrae monedas y enemigos", "menarik koin dan musuh");
                case CargoType.IceCrystal: return L("derrete com o tempo; a água recongela", "melts over time; water refreezes it",
                    "se derrite con el tiempo; el agua lo recongela", "mencair seiring waktu; air membekukannya lagi");
                case CargoType.GriffinCage: return L("o grifinho grita e atrai inimigos; o dash acalma", "the chick screams and draws enemies; dash calms it",
                    "el grifo grita y atrae enemigos; el dash lo calma", "anak grifon menjerit dan menarik musuh; dash menenangkan");
                case CargoType.Lantern: return L("ilumina o escuro; se cair, apaga", "lights the dark; goes out if knocked down", "ilumina la oscuridad; si cae, se apaga",
                    "menerangi gelap; padam jika jatuh");
                case CargoType.Mirror: return L("reflete tiros de frente; 3 reflexos = quebra", "reflects shots from the front; 3 reflections = broken",
                    "refleja disparos de frente; 3 reflejos = se rompe", "memantulkan tembakan dari depan; 3 pantulan = pecah");
                case CargoType.Balloon: return L("+15% de velocidade; no chão escapa em 4 s", "+15% speed; floats off after 4 s on the ground",
                    "+15% de velocidad; en el suelo escapa en 4 s", "+15% kecepatan; di lantai lepas dalam 4 dtk");
                case CargoType.Flytrap: return L("no chão morde inimigos; não fique perto sem ela", "bites enemies on the ground; don't linger near it",
                    "en el suelo muerde enemigos; no te quedes cerca sin ella", "di lantai menggigit musuh; jangan dekat tanpa membawanya");
                case CargoType.TreasureMap: return L("revela baús enterrados; ladrões o preferem", "reveals buried chests; thieves love it",
                    "revela cofres enterrados; los ladrones lo prefieren", "menunjukkan peti terkubur; pencuri mengincarnya");
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

        /// <summary>Titulo do resultado: CargoBroken pelo nome da carga quebrada (reliquia, espelho, balao; com o Fragil, qualquer carga).</summary>
        public static string Titulo(ContractResult r)
        {
            if (r.Outcome != Outcome.CargoBroken) return Titulo(r.Outcome);
            switch (r.Def != null ? r.Def.Cargo : CargoType.Relic)
            {
                case CargoType.Relic: return Titulo(r.Outcome);
                case CargoType.Mirror: return L("O espelho quebrou", "The mirror broke", "El espejo se rompió", "Cermin pecah");
                case CargoType.Balloon: return L("O balão estourou", "The balloon popped", "El globo reventó", "Balon meletus");
                default: return L("A carga quebrou", "The cargo broke", "La carga se rompió", "Paket pecah");
            }
        }

        static string Quebra(ContractResult r)
        {
            int n = r.Cracks > 0 ? r.Cracks : Balance.RelicBreak;
            switch (r.Def != null ? r.Def.Cargo : CargoType.Relic)
            {
                case CargoType.Relic: return F("A {0}ª rachadura quebrou a relíquia.", "Crack number {0} broke the relic.", "La {0}.ª grieta rompió la reliquia.",
                    "Retakan ke-{0} memecahkan relik.", n);
                case CargoType.Mirror: return F("A {0}ª rachadura quebrou o espelho.", "Crack number {0} broke the mirror.", "La {0}.ª grieta rompió el espejo.",
                    "Retakan ke-{0} memecahkan cermin.", n);
                case CargoType.Balloon: return F("O balão estourou na {0}ª rachadura.", "The balloon popped at crack number {0}.", "El globo reventó en la {0}.ª grieta.",
                    "Balon meletus pada retakan ke-{0}.", n);
                default: return F("A {0}ª rachadura quebrou a carga.", "Crack number {0} broke the cargo.", "La {0}.ª grieta rompió la carga.",
                    "Retakan ke-{0} memecahkan paket.", n);
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
                case Outcome.CargoBroken: return Quebra(r);
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

        /// <summary>Pagamento detalhado (rotulo | valor) do ContractResult: base x carga x integridade x bolsa x extras = pagamento; + baus +
        /// resgate (+ dia, evento, Cacador, bonus dobrado; no sem fim, pisos e moedas). No maximo Hud.ResLinhas linhas.</summary>
        public static string[][] Pagamento(ContractResult r, ContractDef c)
        {
            string tempo = L("Tempo", "Time", "Tiempo", "Waktu");
            string bau = L("Moedas dos baús", "Chest coins", "Monedas de cofres", "Koin dari peti");
            var rows = new List<string[]>();
            if (!r.Delivered)
            {
                rows.Add(new[] { tempo, Tempo(r.Time) });
                if (r.Kind == ContractKind.Endless)
                {
                    rows.Add(new[] { L("Pisos vencidos", "Floors cleared", "Pisos superados", "Lantai dilewati"), r.EndlessLayers.ToString(Inv) });
                    rows.Add(new[] { L("Moedas do sem fim", "Endless coins", "Monedas sin fin", "Koin tanpa akhir"), "+" + r.EndlessCoins });
                }
                rows.Add(new[] { bau, "+" + r.ChestCoins });
                if (r.HuntCoins > 0) rows.Add(new[] { PerkNome(Perk.Hunter), "+" + r.HuntCoins });
                rows.Add(new[] { RotuloTotal, "+" + r.Total });
                return rows.ToArray();
            }
            rows.Add(new[] { L("Pagamento base", "Base pay", "Pago base", "Bayaran dasar"), r.Base.ToString(Inv) });
            rows.Add(new[] { r.Hatched ? F("Carga: {0} (chocou)", "Cargo: {0} (hatched)", "Carga: {0} (eclosionó)", "Paket: {0} (menetas)", Carga(c.Cargo))
                : F("Carga: {0}", "Cargo: {0}", "Carga: {0}", "Paket: {0}", Carga(c.Cargo)), Mul(r.CargoMul) });
            rows.Add(new[] { F("Integridade: {0}", "Integrity: {0}", "Integridad: {0}", "Keutuhan: {0}", Rachaduras(r.Cracks)), Mul(r.IntegrityMul) });
            rows.Add(new[] { L("Bolsa da guilda", "Guild purse", "Bolsa del gremio", "Dompet guild"), Mul(r.BagMul) });
            if (r.PayPct != 100)   // modificadores, maestria, evento, raro
                rows.Add(new[] { L("Extras (modificador, maestria, evento)", "Extras (modifier, mastery, event)", "Extras (modificador, maestría, evento)",
                    "Ekstra (pengubah, keahlian, event)"), Mul(r.PayPct / 100f) });
            rows.Add(new[] { RotuloPagamento, r.Payment.ToString(Inv) });
            rows.Add(new[] { L("Baús", "Chests", "Cofres", "Peti"), "+" + r.ChestCoins });
            if (r.RescueAccepted)
                rows.Add(new[] { !r.RescueDelivered ? L("Resgate (não entregue)", "Rescue (not delivered)", "Rescate (no entregado)", "Penyelamatan (tak terkirim)")
                    : L("Resgate", "Rescue", "Rescate", "Penyelamatan"), "+" + r.RescueBonus });
            if (r.DailyBonus > 0) rows.Add(new[] { L("Bônus do contrato do dia", "Daily contract bonus", "Bono del contrato del día", "Bonus kontrak harian"), "+" + r.DailyBonus });
            if (r.EventReward > 0) rows.Add(new[] { L("Recompensa do evento", "Event reward", "Recompensa del evento", "Hadiah event"), "+" + r.EventReward });
            if (r.EventProject != Gear.None) rows.Add(new[] { F("Projeto da semana: {0}", "Weekly blueprint: {0}", "Plano de la semana: {0}", "Cetak biru minggu ini: {0}",
                GearNome(r.EventProject)), "+1" });
            if (r.HuntCoins > 0) rows.Add(new[] { PerkNome(Perk.Hunter), "+" + r.HuntCoins });
            if (r.BonusDoubled) rows.Add(new[] { L("Bônus dobrado (anúncio)", "Double bonus (ad)", "Bono doble (anuncio)", "Bonus ganda (iklan)"), "+" + r.Bonus });
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
        public static string JoystickAjuste(bool visivel) => visivel ? L("Controle na tela: visível", "On-screen stick: shown", "Control en pantalla: visible", "Kontrol layar: tampil")
            : L("Controle na tela: escondido", "On-screen stick: hidden", "Control en pantalla: oculto", "Kontrol layar: sembunyi");
        public static string VibraSoCelular => L("Vibração: só no celular", "Vibration: phone only", "Vibración: solo en el celular", "Getar: khusus ponsel");
        public static string Idioma => L("Idioma", "Language", "Idioma", "Bahasa");
        public static string ExportarDiario => L("EXPORTAR DIÁRIO", "EXPORT LOG", "EXPORTAR REGISTRO", "EKSPOR CATATAN");

        // ------------------------------------------------------------------ tour guiado (Tour.cs): o que e, quando e por que
        public static string TourOk => L("ENTENDI", "GOT IT", "ENTENDIDO", "MENGERTI");
        public static string TourPular => L("Pular tutorial", "Skip tutorial", "Saltar tutorial", "Lewati tutorial");
        public static string TourRever => L("REVER TUTORIAL", "REPLAY TUTORIAL", "VER TUTORIAL", "ULANG TUTORIAL");
        public static string TourRevisto => L("O tutorial vai aparecer de novo.", "The tutorial will show again.", "El tutorial se mostrará de nuevo.", "Tutorial akan muncul lagi.");
        public static string TourConta(int n, int total) => n.ToString(Inv) + "/" + total.ToString(Inv);

        public static string TourVoceT => L("Você é o courier", "You are the courier", "Eres el courier", "Kamu si kurir");
        public static string TourVoce => L("O entregador da Guilda. Leve a carga até o cliente, que espera no fim da masmorra. Entregue inteira para ganhar mais moedas.",
            "The Guild's delivery runner. Carry the cargo to the client waiting at the end of the dungeon. Deliver it intact to earn more coins.",
            "El mensajero del Gremio. Lleva la carga hasta el cliente, que espera al final de la mazmorra. Entrégala intacta para ganar más monedas.",
            "Pengantar dari Guild. Bawa paket ke klien yang menunggu di ujung dungeon. Antar dalam keadaan utuh untuk dapat lebih banyak koin.");
        public static string TourClienteNome => L("O cliente", "The client", "El cliente", "Klien");
        public static string TourClienteFala => L("Você é o novo entregador da Guilda? Preciso desta caixa do outro lado da masmorra. Inteira, por favor!",
            "Are you the Guild's new courier? I need this box on the other side of the dungeon. In one piece, please!",
            "¿Eres el nuevo mensajero del Gremio? Necesito esta caja al otro lado de la mazmorra. ¡Entera, por favor!",
            "Kamu kurir baru dari Guild? Aku butuh kotak ini sampai di sisi lain dungeon. Utuh, ya!");
        public static string TourArrastarT => L("Arraste para andar", "Drag to move", "Arrastra para moverte", "Geser untuk berjalan");
        public static string TourArrastar => L("Arraste o dedo em qualquer lugar da tela: o courier anda com a caixa nas mãos. Solte o dedo e ele pousa a caixa no chão.",
            "Drag your finger anywhere on the screen: the courier walks with the box in hand. Lift your finger and he sets the box down.",
            "Arrastra el dedo en cualquier lugar de la pantalla: el mensajero camina con la caja en las manos. Suelta el dedo y deja la caja en el suelo.",
            "Geser jarimu di mana saja di layar: kurir berjalan sambil membawa kotak. Angkat jarimu dan ia menaruh kotak di lantai.");
        public static string TourVidaT => L("Sua vida", "Your health", "Tu vida", "Nyawamu");
        public static string TourVida => L("Cada golpe tira um coração. Se acabarem, você cai e a carga fica na sala, esperando o resgate no próximo contrato.",
            "Each hit takes a heart. If they run out, you fall and the cargo stays in the room, waiting for a rescue on your next contract.",
            "Cada golpe quita un corazón. Si se acaban, caes y la carga se queda en la sala, esperando el rescate en el próximo contrato.",
            "Setiap serangan mengurangi satu hati. Jika habis, kamu jatuh dan paket tertinggal di ruangan, menunggu diselamatkan di kontrak berikutnya.");
        public static string TourCargaT => L("A carga", "The cargo", "La carga", "Paket");
        public static string TourCarga => L("Golpes na carga viram rachaduras. Com 3 rachaduras ela quebra. Menos rachaduras, mais moedas na entrega.",
            "Hits on the cargo become cracks. At 3 cracks it breaks. Fewer cracks, more coins on delivery.",
            "Los golpes a la carga se vuelven grietas. Con 3 grietas se rompe. Menos grietas, más monedas en la entrega.",
            "Serangan ke paket menjadi retakan. Dengan 3 retakan paket hancur. Makin sedikit retakan, makin banyak koin saat diantar.");
        public static string TourPausaT => L("Pausa", "Pause", "Pausa", "Jeda");
        public static string TourPausa => L("Toque aqui para pausar o contrato, mudar o som e o idioma ou desistir.",
            "Tap here to pause the contract, change sound and language, or give up.",
            "Toca aquí para pausar el contrato, cambiar el sonido y el idioma o rendirte.",
            "Ketuk di sini untuk menjeda kontrak, mengubah suara dan bahasa, atau menyerah.");
        public static string TourPousarT => L("Pouse para lutar", "Set down to fight", "Suelta para pelear", "Taruh untuk bertarung");
        public static string TourPousar => L("Com a caixa nas mãos você não ataca. Solte o dedo: o courier pousa a caixa e atira sozinho no inimigo mais perto. Depois arraste de novo para pegar a caixa.",
            "With the box in hand you can't attack. Lift your finger: the courier sets the box down and shoots the nearest enemy on his own. Then drag again to pick the box up.",
            "Con la caja en las manos no atacas. Suelta el dedo: el mensajero deja la caja y dispara solo al enemigo más cercano. Después arrastra otra vez para recoger la caja.",
            "Saat membawa kotak kamu tidak bisa menyerang. Angkat jarimu: kurir menaruh kotak dan otomatis menembak musuh terdekat. Lalu geser lagi untuk mengambil kotak.");
        public static string TourLadraoT => L("Ladrão!", "Thief!", "¡Ladrón!", "Pencuri!");
        public static string TourLadrao => L("Ele corre para a carga pousada e foge com ela. Fique perto da caixa ou leve-a com você. Se ele pegar, acerte-o para a caixa cair.",
            "He runs for cargo left on the floor and flees with it. Stay near the box or carry it. If he grabs it, hit him to make him drop it.",
            "Corre hacia la carga en el suelo y huye con ella. Quédate cerca de la caja o llévala contigo. Si la toma, golpéalo para que la suelte.",
            "Ia berlari ke paket yang ditaruh dan kabur membawanya. Tetap dekat kotak atau bawa kotaknya. Jika ia mengambilnya, serang dia agar kotaknya jatuh.");
        public static string TourDashT => L("Dash", "Dash", "Dash", "Dash");
        public static string TourDash => L("Um toque rápido na tela é o dash: um impulso que desvia do golpe e empurra o inimigo. O anel mostra quando ele recarrega.",
            "A quick tap on the screen is a dash: a burst that dodges the hit and pushes the enemy back. The ring shows when it recharges.",
            "Un toque rápido en la pantalla es el dash: un impulso que esquiva el golpe y empuja al enemigo. El anillo muestra cuándo se recarga.",
            "Ketuk cepat di layar untuk dash: dorongan yang menghindari serangan dan mendorong musuh. Lingkaran menunjukkan kapan dash terisi lagi.");
        public static string TourPortasT => L("Escolha a porta", "Pick a door", "Elige la puerta", "Pilih pintu");
        public static string TourPortas => L("As caveiras mostram o perigo da sala seguinte e o ícone mostra o que tem lá dentro, como baú ou ladrão. Leve a carga até a porta para passar.",
            "Skulls show how dangerous the next room is and the icon shows what's inside, like a chest or a thief. Bring the cargo to the door to go through.",
            "Las calaveras muestran el peligro de la sala siguiente y el icono muestra lo que hay dentro, como un cofre o un ladrón. Lleva la carga hasta la puerta para pasar.",
            "Tengkorak menunjukkan bahaya ruangan berikutnya dan ikonnya menunjukkan isinya, seperti peti atau pencuri. Bawa paket ke pintu untuk lewat.");
        public static string TourBauT => L("Baú", "Chest", "Cofre", "Peti");
        public static string TourBau => L("Fique parado ao lado do baú para abrir. As moedas pagam as melhorias da guilda.",
            "Stand still next to the chest to open it. Coins pay for guild upgrades.",
            "Quédate quieto junto al cofre para abrirlo. Las monedas pagan las mejoras del gremio.",
            "Diam di samping peti untuk membukanya. Koin dipakai untuk peningkatan guild.");
        public static string TourEntregaT => L("O cliente", "The client", "El cliente", "Klien");
        public static string TourEntrega => L("Leve a carga até o círculo do cliente para entregar. Quanto mais inteira a carga, maior o pagamento.",
            "Bring the cargo into the client's circle to deliver. The more intact the cargo, the bigger the pay.",
            "Lleva la carga al círculo del cliente para entregarla. Cuanto más intacta, mayor el pago.",
            "Bawa paket ke lingkaran klien untuk mengantar. Makin utuh paketnya, makin besar bayarannya.");
        public static string TourJogarT => L("Comece aqui", "Start here", "Empieza aquí", "Mulai di sini");
        public static string TourJogar => L("Toque em JOGAR para ver o quadro de contratos da guilda.",
            "Tap PLAY to see the guild's contract board.",
            "Toca JUGAR para ver el tablón de contratos del gremio.",
            "Ketuk MAIN untuk melihat papan kontrak guild.");
        public static string TourC1T => L("Seu primeiro contrato", "Your first contract", "Tu primer contrato", "Kontrak pertamamu");
        public static string TourC1 => L("Cada contrato pede uma carga e um caminho. A moeda mostra o pagamento e as estrelas, os desafios. Toque no contrato para começar.",
            "Each contract asks for a cargo and a route. The coin shows the pay and the stars show the challenges. Tap the contract to start.",
            "Cada contrato pide una carga y un camino. La moneda muestra el pago y las estrellas, los desafíos. Toca el contrato para empezar.",
            "Setiap kontrak meminta satu paket dan satu rute. Koin menunjukkan bayaran dan bintang menunjukkan tantangan. Ketuk kontrak untuk mulai.");
        public static string TourEstrelasT => L("Estrelas", "Stars", "Estrellas", "Bintang");
        public static string TourEstrelas => L("Cada contrato dá até 3 estrelas: entregar, carga intacta e sem cair, dentro do tempo-alvo. As estrelas abrem novas regiões e prêmios.",
            "Each contract gives up to 3 stars: deliver, intact cargo, and no fall within the target time. Stars unlock new regions and rewards.",
            "Cada contrato da hasta 3 estrellas: entregar, carga intacta y sin caer, dentro del tiempo objetivo. Las estrellas abren nuevas regiones y premios.",
            "Setiap kontrak memberi hingga 3 bintang: antar, paket utuh, dan tidak jatuh dalam waktu target. Bintang membuka wilayah dan hadiah baru.");
        public static string TourPerkT => L("Vantagem", "Perk", "Ventaja", "Keuntungan");
        public static string TourPerk => L("Escolha uma vantagem para este contrato. Ela vale até a entrega.",
            "Pick a perk for this contract. It lasts until the delivery.",
            "Elige una ventaja para este contrato. Dura hasta la entrega.",
            "Pilih satu keuntungan untuk kontrak ini. Berlaku sampai paket diantar.");
        public static string TourGuildaT => L("A guilda abriu!", "The guild is open!", "¡El gremio abrió!", "Guild sudah buka!");
        public static string TourGuilda => L("Gaste as moedas dos contratos em melhorias permanentes: vida, dash, bolsa e mochila. Toque em GUILDA.",
            "Spend contract coins on permanent upgrades: health, dash, purse and backpack. Tap GUILD.",
            "Gasta las monedas de los contratos en mejoras permanentes: vida, dash, bolsa y mochila. Toca GREMIO.",
            "Pakai koin kontrak untuk peningkatan permanen: nyawa, dash, kantong, dan ransel. Ketuk GUILD.");
        public static string TourGuildaComprarT => L("Melhorias", "Upgrades", "Mejoras", "Peningkatan");
        public static string TourGuildaComprar => L("Cada melhoria tem 8 níveis e vale para sempre. Comece pela vida: +1 coração em todos os contratos.",
            "Each upgrade has 8 levels and lasts forever. Start with health: +1 heart on every contract.",
            "Cada mejora tiene 8 niveles y dura para siempre. Empieza por la vida: +1 corazón en todos los contratos.",
            "Setiap peningkatan punya 8 level dan berlaku selamanya. Mulai dari nyawa: +1 hati di setiap kontrak.");
        public static string TourBauDiaT => L("Baú do dia", "Daily chest", "Cofre del día", "Peti harian");
        public static string TourBauDia => L("Uma vez por dia: assista a um anúncio e ganhe moedas. É opcional.",
            "Once a day: watch an ad and get coins. It's optional.",
            "Una vez al día: mira un anuncio y gana monedas. Es opcional.",
            "Sekali sehari: tonton iklan dan dapatkan koin. Ini opsional.");
        public static string TourTemporadaT => L("Temporada", "Season", "Temporada", "Musim");
        public static string TourTemporada => L("Objetivos de 28 dias com prêmios fixos. Sem passe pago.",
            "28-day goals with fixed rewards. No paid pass.",
            "Objetivos de 28 días con premios fijos. Sin pase de pago.",
            "Target 28 hari dengan hadiah tetap. Tanpa pass berbayar.");
        public static string TourMarcosT => L("Marcos e maestria", "Milestones and mastery", "Hitos y maestría", "Pencapaian dan keahlian");
        public static string TourMarcos => L("As estrelas somadas dão moedas e uma roupa. Entregar a mesma carga várias vezes aumenta o pagamento dela.",
            "Your total stars give coins and an outfit. Delivering the same cargo many times raises its pay.",
            "Las estrellas sumadas dan monedas y un traje. Entregar la misma carga muchas veces sube su pago.",
            "Total bintangmu memberi koin dan satu kostum. Mengantar paket yang sama berkali-kali menaikkan bayarannya.");
        public static string TourRoupasT => L("Roupas", "Outfits", "Trajes", "Kostum");
        public static string TourRoupas => L("Visuais para o courier. Só aparência: não mudam o jogo.",
            "Looks for the courier. Cosmetic only: they don't change the game.",
            "Aspectos para el mensajero. Solo apariencia: no cambian el juego.",
            "Tampilan untuk kurir. Hanya kosmetik: tidak mengubah permainan.");
        public static string TourLojaT => L("Loja", "Shop", "Tienda", "Toko");
        public static string TourLoja => L("Remover anúncios, pacotes e roupas. Tudo com conteúdo fixo, nada aleatório.",
            "Remove ads, packs and outfits. Everything has fixed contents, nothing random.",
            "Quitar anuncios, paquetes y trajes. Todo con contenido fijo, nada aleatorio.",
            "Hapus iklan, paket, dan kostum. Semua isinya tetap, tidak ada yang acak.");
        public static string TourConfigT => L("Configurações", "Settings", "Ajustes", "Pengaturan");
        public static string TourConfig => L("Som, música, vibração, idioma e o botão para rever este tutorial.",
            "Sound, music, vibration, language and the button to replay this tutorial.",
            "Sonido, música, vibración, idioma y el botón para ver este tutorial otra vez.",
            "Suara, musik, getar, bahasa, dan tombol untuk mengulang tutorial ini.");
        public static string TourNovidadesT => L("Novidades no quadro", "New on the board", "Novedades en el tablón", "Baru di papan");
        public static string TourNovidades => L("Agora abrem o contrato do dia, o contrato raro, os eventos, o desafio sem fim e o código para jogar o contrato de um amigo.",
            "The daily contract, rare contract, events, endless challenge and the code to play a friend's contract are now open.",
            "Ahora se abren el contrato del día, el contrato raro, los eventos, el desafío sin fin y el código para jugar el contrato de un amigo.",
            "Sekarang terbuka kontrak harian, kontrak langka, event, tantangan tanpa akhir, dan kode untuk memainkan kontrak teman.");
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
        public static string Privacidade => AnunciosReais
            ? L("Privacidade: o jogo não pede dados pessoais. O progresso e o diário ficam neste aparelho; o diário só sai se você tocar em Exportar diário. Os anúncios (Unity LevelPlay) usam a internet e não são personalizados, sem ID de publicidade. Política: " + Politica,
                "Privacy: the game asks for no personal data. Progress and the log stay on this device; the log only leaves if you tap Export log. Ads (Unity LevelPlay) use the internet and are not personalized, with no advertising ID. Policy: " + Politica,
                "Privacidad: el juego no pide datos personales. El progreso y el registro quedan en este dispositivo; el registro solo sale si tocas Exportar registro. Los anuncios (Unity LevelPlay) usan internet y no son personalizados, sin ID de publicidad. Política: " + Politica,
                "Privasi: game ini tidak meminta data pribadi. Progres dan catatan ada di perangkat ini; catatan hanya keluar jika kamu ketuk Ekspor catatan. Iklan (Unity LevelPlay) memakai internet dan tidak dipersonalisasi, tanpa ID iklan. Kebijakan: " + Politica)
            : L("Privacidade: o jogo não coleta dados pessoais e não usa a internet. O progresso e o diário de teste ficam só neste aparelho; nada sai dele sem você tocar em Exportar diário.",
            "Privacy: the game collects no personal data and does not use the internet. Progress and the playtest log stay on this device; nothing leaves it unless you tap Export log.",
            "Privacidad: el juego no recopila datos personales y no usa internet. El progreso y el registro de prueba quedan solo en este dispositivo; nada sale de él si no tocas Exportar registro.",
            "Privasi: game ini tidak mengumpulkan data pribadi dan tidak memakai internet. Progres dan catatan uji hanya ada di perangkat ini; tak ada yang keluar kecuali kamu ketuk Ekspor catatan.");

        // ------------------------------------------------------------------ lancamento (CONTRATO_LANCAMENTO §6-§7): regioes, raro,
        // modificadores, eventos, temporada, equipamento, marcos e maestria, sem fim, roupas, loja e anuncios. Glossario novo: projeto =
        // blueprint / plano / cetak biru; equipamento = gear / equipo / perlengkapan; camada (contrato) = floor / piso / lantai.

        /// <summary>Nome da regiao 1-4 (glossario do docs/UI_V1.md); fora disso, o do Core.</summary>
        public static string NomeRegiao(int r)
        {
            switch (r)
            {
                case 1: return L("Porões da Guilda", "Guild Cellars", "Sótanos del Gremio", "Ruang Bawah Guild");
                case 2: return L("Minas Alagadas", "Flooded Mines", "Minas Inundadas", "Tambang Banjir");
                case 3: return L("Cripta Assombrada", "Haunted Crypt", "Cripta Embrujada", "Kripta Berhantu");
                case 4: return L("Forja do Dragão", "Dragon's Forge", "Forja del Dragón", "Tempaan Naga");
                default: RegionDef d = RegionDef.Of(r); return d != null ? d.Name : "";
            }
        }

        // mapa das regioes
        public static string Mapa => L("Mapa das regiões", "Region map", "Mapa de regiones", "Peta wilayah");
        public static string MapaCabeca => L("Toque numa região aberta para ver os contratos dela no quadro.", "Tap an open region to see its contracts on the board.",
            "Toca una región abierta para ver sus contratos en el tablero.", "Ketuk wilayah yang terbuka untuk melihat kontraknya di papan.");
        public static string RegiaoProgresso(int feitos, int total, int est, int estMax) => F("{0}/{1} contratos · {2}/{3} estrelas", "{0}/{1} contracts · {2}/{3} stars",
            "{0}/{1} contratos · {2}/{3} estrellas", "{0}/{1} kontrak · {2}/{3} bintang", feitos, total, est, estMax);
        public static string RegiaoTrava(int estrelas, string chefe) => F("abre com {0} estrelas e vencendo: {1}", "opens at {0} stars after beating: {1}",
            "abre con {0} estrellas y venciendo a: {1}", "terbuka di {0} bintang setelah mengalahkan: {1}", estrelas, chefe);
        public static string EmBreve => L("em breve", "coming soon", "próximamente", "segera hadir");
        public static string NoQuadro => L("NO QUADRO", "ON BOARD", "EN TABLERO", "DI PAPAN");
        public static string Ver => L("VER", "VIEW", "VER", "LIHAT");

        // quadro: botoes e contrato do dia com modificador
        public static string Eventos => L("EVENTOS", "EVENTS", "EVENTOS", "EVENT");
        public static string SemFimBotao => L("SEM FIM", "ENDLESS", "SIN FIN", "TANPA AKHIR");
        public static string DiaLinhaMod(string carga, string mod, int pct, int moedas) => F("{0} · {1} +{2}% · extra +{3} moedas", "{0} · {1} +{2}% · extra +{3} coins",
            "{0} · {1} +{2}% · extra +{3} monedas", "{0} · {1} +{2}% · ekstra +{3} koin", carga, mod, pct, moedas);
        /// <summary>"Contrato do dia · Minas Alagadas" (titulo do cartao com a regiao; lista curta, nao frase).</summary>
        public static string ComRegiao(string titulo, int regiao) => titulo + " · " + NomeRegiao(regiao);

        // contrato raro e rerolls (§6.4, §6.5)
        public static string Raro => L("Contrato raro ×2", "Rare contract ×2", "Contrato raro ×2", "Kontrak langka ×2");
        public static string RaroEspera(int h) => F("volte depois de {0} h fora para ganhar um", "come back after {0} h away to get one",
            "vuelve tras {0} h fuera para recibir uno", "kembali setelah {0} jam pergi untuk dapat satu", h);
        public static string RaroFeito(int h) => F("entregue! o próximo vem depois de {0} h fora", "delivered! the next one comes after {0} h away",
            "¡entregado! el próximo llega tras {0} h fuera", "terkirim! berikutnya datang setelah {0} jam pergi", h);
        public static string VolteAmanha => L("Volte amanhã para o contrato raro.", "Come back tomorrow for the rare contract.",
            "Vuelve mañana por el contrato raro.", "Kembali besok untuk kontrak langka.");
        public static string Trocar => L("TROCAR", "SWAP", "CAMBIAR", "GANTI");
        /// <summary>No lugar do preco do TROCAR quando ja trocou hoje (sim.RerolledToday); falta de moeda fica com o preco em vermelho.</summary>
        public static string TrocaAmanha => L("amanhã", "tomorrow", "mañana", "besok");

        // modificadores (§6.2)
        public static string Modificadores => L("Modificadores", "Modifiers", "Modificadores", "Pengubah");
        public static string ModCabeca(string contrato) => F("{0}: escolha até 2 (opcional). Cada um deixa o contrato mais difícil e paga mais.",
            "{0}: pick up to 2 (optional). Each one makes the contract harder and pays more.",
            "{0}: elige hasta 2 (opcional). Cada uno hace el contrato más difícil y paga más.",
            "{0}: pilih sampai 2 (opsional). Masing-masing membuat kontrak lebih sulit dan bayaran lebih besar.", contrato);
        public static string ModTotal(int pct) => F("Pagamento extra: +{0}%", "Extra payment: +{0}%", "Pago extra: +{0}%", "Bayaran ekstra: +{0}%", pct);
        public static string Escolhido => L("ESCOLHIDO", "ON", "ACTIVO", "AKTIF");
        public static string Escolher => L("ESCOLHER", "PICK", "ELEGIR", "PILIH");

        static ModifierDef ModDef(Modifier m) { foreach (ModifierDef d in ModifierDef.All) if (d.Type == m) return d; return null; }

        /// <summary>Nome do modificador: pt do Core (ModifierDef.Name); o resto pela tabela.</summary>
        public static string ModNome(Modifier m)
        {
            ModifierDef d = ModDef(m);
            string pt = d != null ? d.Name : m.ToString();
            switch (m)
            {
                case Modifier.Fog: return L(pt, "Fog", "Niebla", "Kabut");
                case Modifier.Rush: return L(pt, "Rush", "Prisa", "Buru-buru");
                case Modifier.Horde: return L(pt, "Horde", "Horda", "Gerombolan");
                case Modifier.Fragile: return L(pt, "Fragile", "Frágil", "Rapuh");
                case Modifier.Stingy: return L(pt, "Stingy", "Tacaño", "Pelit");
                default: return pt;
            }
        }

        public static string ModEfeito(Modifier m)
        {
            ModifierDef d = ModDef(m);
            string pt = d != null ? d.Text : "";
            switch (m)
            {
                case Modifier.Fog: return L(pt, "short sight", "visión corta", "pandangan pendek");
                case Modifier.Rush: return L(pt, "target time -30%", "tiempo objetivo -30%", "waktu target -30%");
                case Modifier.Horde: return L(pt, "+1 enemy per room", "+1 enemigo por sala", "+1 musuh per ruang");
                case Modifier.Fragile: return L(pt, "the cargo breaks at 2 cracks instead of 3", "la carga se rompe con 2 grietas en vez de 3",
                    "paket pecah di 2 retakan, bukan 3");
                case Modifier.Stingy: return L(pt, "chests -50%", "cofres -50%", "peti -50%");
                default: return pt;
            }
        }

        // eventos (§6.1)
        public static string QuadroEventos => L("Quadro de eventos", "Event board", "Tablero de eventos", "Papan event");
        public static string EventoNome(LiveEvent e)
        {
            string pt = EventDef.All[(int)e].Name;
            switch (e)
            {
                case LiveEvent.FragileWeek: return L(pt, "Fragile Week", "Semana Frágil", "Pekan Rapuh");
                case LiveEvent.Midnight: return L(pt, "Midnight Delivery", "Entrega de Medianoche", "Antaran Tengah Malam");
                case LiveEvent.NoDamage: return L(pt, "No-Damage Contract", "Contrato Sin Daño", "Kontrak Tanpa Luka");
                case LiveEvent.Treasure: return L(pt, "Treasure Detour", "Desvío del Tesoro", "Jalan Pintas Harta");
                case LiveEvent.BossRun: return L(pt, "Boss Courier Run", "Carrera de Jefes", "Lomba Para Bos");
                default: return pt;
            }
        }

        public static string EventoRegra(LiveEvent e)
        {
            string pt = EventDef.All[(int)e].Text;
            switch (e)
            {
                case LiveEvent.FragileWeek: return L(pt, "fragile cargo only; +50% payment", "solo cargas frágiles; +50% de pago", "hanya paket rapuh; +50% bayaran");
                case LiveEvent.Midnight: return L(pt, "every room is dark; you carry the lantern", "todas las salas a oscuras; llevas la linterna",
                    "semua ruang gelap; kamu membawa lentera");
                case LiveEvent.NoDamage: return L(pt, "1 health; payment ×3", "1 de vida; pago ×3", "1 nyawa; bayaran ×3");
                case LiveEvent.Treasure: return L(pt, "double chests and cracked walls open", "cofres dobles y paredes agrietadas abiertas",
                    "peti ganda dan dinding retak terbuka");
                case LiveEvent.BossRun: return L(pt, "the bosses you beat, back to back", "los jefes que venciste, uno tras otro", "bos yang sudah dikalahkan, berturut-turut");
                default: return pt;
            }
        }

        public static string EventoCabeca(string nome, string regra, int dias) => F("Evento da semana: {0} ({1}). Termina em {2} dias.",
            "Event of the week: {0} ({1}). Ends in {2} days.", "Evento de la semana: {0} ({1}). Termina en {2} días.",
            "Event minggu ini: {0} ({1}). Berakhir dalam {2} hari.", nome, regra, dias);
        public static string EventoFechado => L("O quadro de eventos abre depois do C3.", "The event board opens after C3.",
            "El tablero de eventos abre después del C3.", "Papan event terbuka setelah C3.");
        public static string Vaga(int k, string regiao) => F("Vaga {0} · {1}", "Slot {0} · {1}", "Puesto {0} · {1}", "Slot {0} · {1}", k, regiao);
        public static string VagaLinha(int camadas, string carga) => F("{0} camadas · {1}", "{0} floors · {1}", "{0} pisos · {1}", "{0} lantai · {1}", camadas, carga);
        public static string ProjetoSemana(string equip) => F("As 3 primeiras vagas na semana: +1 projeto ({0}).", "First 3 slots this week: +1 blueprint ({0}).",
            "Los 3 primeros puestos en la semana: +1 plano ({0}).", "3 slot pertama minggu ini: +1 cetak biru ({0}).", equip);
        public static string ProjetoSemanaMoedas(int m) => F("As 3 primeiras vagas na semana: +{0} moedas.", "First 3 slots this week: +{0} coins.",
            "Los 3 primeros puestos en la semana: +{0} monedas.", "3 slot pertama minggu ini: +{0} koin.", m);
        public static string ProjetoGanho => L("Projeto da semana ganho! Volte na próxima semana.", "Weekly blueprint earned! Come back next week.",
            "¡Plano de la semana ganado! Vuelve la próxima semana.", "Cetak biru minggu ini didapat! Kembali minggu depan.");
        public static string PacoteEvento => L("PACOTE DE EVENTO", "EVENT PACK", "PAQUETE DE EVENTO", "PAKET EVENT");

        // quadro de temporada (§6.3)
        public static string Temporada => L("Quadro de temporada", "Season board", "Tablero de temporada", "Papan musim");
        public static string TemporadaBotao => L("TEMPORADA", "SEASON", "TEMPORADA", "MUSIM");
        public static string TemporadaCabeca(int dias) => F("Temporada de 28 dias: faltam {0}. Recompensas fixas, sem passe pago.",
            "28-day season: {0} days left. Fixed rewards, no paid pass.", "Temporada de 28 días: faltan {0}. Recompensas fijas, sin pase de pago.",
            "Musim 28 hari: sisa {0} hari. Hadiah tetap, tanpa pass berbayar.", dias);
        public static string Coletar => L("COLETAR", "CLAIM", "RECLAMAR", "AMBIL");
        public static string Coletado => L("COLETADO", "CLAIMED", "RECLAMADO", "DIAMBIL");
        public static string Progresso(int tem, int alvo) => string.Format(Inv, "{0}/{1}", tem, alvo);
        public static string PremioProjeto(Gear g) => F("projeto: {0}", "blueprint: {0}", "plano: {0}", "cetak biru: {0}", GearNome(g));
        public static string PremioRoupa(Outfit o) => F("roupa: {0}", "outfit: {0}", "traje: {0}", "pakaian: {0}", RoupaNome(o));

        /// <summary>Objetivo da temporada: pt do Core (GoalDef.Text, com a 1a letra maiuscula); o resto pelo tipo e pelo alvo.</summary>
        public static string ObjetivoTemporada(GoalDef g)
        {
            if (Lang == Lingua.Pt) return g.Text.Length > 0 ? char.ToUpperInvariant(g.Text[0]) + g.Text.Substring(1) : "";
            int n = g.Target;
            switch (g.Kind)
            {
                case Goal.Deliver: return F(null, "Deliver {0} contracts", "Entrega {0} contratos", "Antar {0} kontrak", n);
                case Goal.DeliverCargo: return F(null, "Deliver {0} × {1}", "Entrega {0} × {1}", "Antar {0} × {1}", n, Carga(g.Cargo));
                case Goal.Hatched: return F(null, "Deliver {0} hatched eggs", "Entrega {0} huevos eclosionados", "Antar {0} telur yang menetas", n);
                case Goal.Chests: return F(null, "Open {0} chests", "Abre {0} cofres", "Buka {0} peti", n);
                case Goal.ThreeStars: return F(null, "Get 3 stars on {0} contracts", "Logra 3 estrellas en {0} contratos", "Raih 3 bintang di {0} kontrak", n);
                case Goal.BossClean: return F(null, "Bosses beaten without damage: {0}", "Jefes vencidos sin daño: {0}", "Bos dikalahkan tanpa luka: {0}", n);
                case Goal.Secrets: return F(null, "Find {0} secret rooms", "Encuentra {0} salas secretas", "Temukan {0} ruang rahasia", n);
                case Goal.Daily: return F(null, "Complete {0} daily contracts", "Completa {0} contratos del día", "Selesaikan {0} kontrak harian", n);
                case Goal.Rescue: return F(null, "Rescue {0} lost cargos", "Rescata {0} cargas perdidas", "Selamatkan {0} paket hilang", n);
                case Goal.Kills: return F(null, "Defeat {0} enemies", "Derrota a {0} enemigos", "Kalahkan {0} musuh", n);
                case Goal.WithModifier: return F(null, "Contracts delivered with a modifier: {0}", "Contratos entregados con modificador: {0}",
                    "Kontrak terkirim dengan pengubah: {0}", n);
                case Goal.Event: return F(null, "Deliver {0} event contracts", "Entrega {0} contratos de evento", "Antar {0} kontrak event", n);
                case Goal.Code: return F(null, "Contracts delivered by code: {0}", "Contratos entregados por código: {0}", "Kontrak lewat kode terkirim: {0}", n);
                case Goal.Intact: return F(null, "Deliver {0} intact cargos", "Entrega {0} cargas intactas", "Antar {0} paket utuh", n);
                case Goal.Rare: return F(null, "Rare contracts delivered: {0}", "Contratos raros entregados: {0}", "Kontrak langka terkirim: {0}", n);
                default: return g.Text;
            }
        }

        // marcos e maestria (§6.7)
        public static string Marcos => L("Marcos e maestria", "Milestones & mastery", "Logros y maestría", "Pencapaian & keahlian");
        public static string MarcosBotao => L("MARCOS", "MILESTONES", "LOGROS", "PENCAPAIAN");
        public static string AbaMarcos => L("MARCOS", "MILESTONES", "LOGROS", "PENCAPAIAN");
        public static string AbaMaestria => L("MAESTRIA", "MASTERY", "MAESTRÍA", "KEAHLIAN");
        public static string MarcosCabeca => L("Marcos de estrelas dão moedas (e uma roupa). Maestria: entregas da mesma carga dão +2% de pagamento por nível.",
            "Star milestones give coins (and an outfit). Mastery: deliveries of the same cargo give +2% payment per level.",
            "Los logros de estrellas dan monedas (y un traje). Maestría: las entregas de la misma carga dan +2% de pago por nivel.",
            "Pencapaian bintang memberi koin (dan pakaian). Keahlian: pengantaran paket yang sama memberi +2% bayaran per level.");
        public static string MarcoNome(int n) => F("{0} estrelas", "{0} stars", "{0} estrellas", "{0} bintang", n);
        public static string VoceTem(int n) => F("você tem {0}", "you have {0}", "tienes {0}", "kamu punya {0}", n);
        public static string Entregas(int n, int prox) => F("{0} entregas · próximo nível com {1}", "{0} deliveries · next level at {1}",
            "{0} entregas · siguiente nivel con {1}", "{0} pengantaran · level berikut di {1}", n, prox);
        public static string EntregasMax(int n) => F("{0} entregas · nível máximo", "{0} deliveries · max level", "{0} entregas · nivel máximo",
            "{0} pengantaran · level maksimal", n);
        public static string PagamentoMais(int pct) => F("+{0}% pagamento", "+{0}% pay", "+{0}% de pago", "+{0}% bayaran", pct);
        public static string NivelCurto(int n) => F("NÍVEL {0}", "LEVEL {0}", "NIVEL {0}", "LEVEL {0}", n);

        // desafio sem fim (§6.8)
        public static string SemFim => L("Desafio sem fim", "Endless challenge", "Desafío sin fin", "Tantangan tanpa akhir");
        public static string SemFimRegras(int mini, int chefe, int moedas, int teto) => F(
            "Pisos sem fim misturando as regiões abertas, cada vez mais difíceis. Mini-chefe a cada {0} pisos e chefe a cada {1}. {2} moedas por piso (até {3} por dia).",
            "Endless floors mixing the open regions, harder and harder. A mini-boss every {0} floors and a boss every {1}. {2} coins per floor (up to {3} a day).",
            "Pisos sin fin que mezclan las regiones abiertas, cada vez más difíciles. Minijefe cada {0} pisos y jefe cada {1}. {2} monedas por piso (hasta {3} al día).",
            "Lantai tanpa akhir dari wilayah yang terbuka, makin lama makin sulit. Mini-bos tiap {0} lantai dan bos tiap {1}. {2} koin per lantai (maks {3} per hari).",
            mini, chefe, moedas, teto);
        public static string SemFimRecorde(int best, int semana) => F("Recorde: {0} pisos · nesta semana: {1}", "Best: {0} floors · this week: {1}",
            "Récord: {0} pisos · esta semana: {1}", "Rekor: {0} lantai · minggu ini: {1}", best, semana);
        public static string SemFimCodigo(string c) => F("Código da semana: {0} (o mesmo desafio para todos)", "Code of the week: {0} (the same challenge for everyone)",
            "Código de la semana: {0} (el mismo desafío para todos)", "Kode minggu ini: {0} (tantangan sama untuk semua)", c);
        public static string SemFimMoedas(int n) => F("Moedas que ainda rendem hoje: {0}", "Coins still available today: {0}", "Monedas que aún rinden hoy: {0}",
            "Koin yang masih bisa didapat hari ini: {0}", n);
        public static string SemFimFechado => L("Abre ao vencer o chefe da última região.", "Opens after beating the last region's boss.",
            "Se abre al vencer al jefe de la última región.", "Terbuka setelah mengalahkan bos wilayah terakhir.");
        /// <summary>Faixa do contrato no sem fim (no lugar das bolinhas das camadas).</summary>
        public static string SemFimPiso(int piso, int best) => F("piso {0} · recorde {1}", "floor {0} · best {1}", "piso {0} · récord {1}", "lantai {0} · rekor {1}", piso, best);

        // equipamento lateral (§6.6; aba da guilda)
        public static string AbaEquip => L("EQUIPAMENTO", "GEAR", "EQUIPO", "PERLENGKAPAN");
        public static string Equipado => L("EQUIPADO", "EQUIPPED", "EQUIPADO", "DIPAKAI");
        public static string Projetos(int n, int max) => F("projetos {0}/{1}", "blueprints {0}/{1}", "planos {0}/{1}", "cetak biru {0}/{1}", n, max);

        /// <summary>Nome do equipamento: pt do Core (GearDef.Name); o resto pela tabela.</summary>
        public static string GearNome(Gear g)
        {
            string pt = (int)g >= 0 && (int)g < GearDef.All.Length ? GearDef.All[(int)g].Name : g.ToString();
            switch (g)
            {
                case Gear.Smoke: return L(pt, "Smoke bomb", "Bomba de humo", "Bom asap");
                case Gear.Hook: return L(pt, "Grappling hook", "Gancho", "Kait");
                case Gear.Trap: return L(pt, "Bear trap", "Trampa para osos", "Perangkap beruang");
                case Gear.Boots: return L(pt, "Mud boots", "Botas de barro", "Sepatu lumpur");
                default: return pt;
            }
        }

        public static string GearEfeito(Gear g)
        {
            string pt = (int)g >= 0 && (int)g < GearDef.All.Length ? GearDef.All[(int)g].Text : "";
            switch (g)
            {
                case Gear.Smoke: return L(pt, "whoever grabs the cargo drops it and loses sight of it for 3 s",
                    "quien toma la carga la suelta y la pierde de vista 3 s", "yang mengambil paket menjatuhkannya dan kehilangan jejak 3 dtk");
                case Gear.Hook: return L(pt, "walk toward the cargo on the ground (1.5-5 m) and it flies to your hands",
                    "camina hacia la carga en el suelo (1,5-5 m) y vuelve a tus manos", "jalan ke arah paket di lantai (1,5-5 m), paket melayang ke tangan");
                case Gear.Trap: return L(pt, "on the 1st set-down in a room: holds the 1st enemy that passes for 3 s",
                    "al dejar la carga por 1.ª vez en la sala: atrapa al primer enemigo que pase 3 s",
                    "saat pertama menaruh paket di ruang: menjebak musuh pertama yang lewat 3 dtk");
                case Gear.Boots: return L(pt, "water and slime puddles don't slow you (passive)", "el agua y los charcos de limo no te frenan (pasivo)",
                    "air dan genangan slime tak memperlambat (pasif)");
                default: return pt;
            }
        }

        // roupas do courier (cosmetico)
        public static string Roupas => L("Roupas do courier", "Courier outfits", "Trajes del courier", "Pakaian kurir");
        public static string RoupasBotao => L("ROUPAS", "OUTFITS", "TRAJES", "PAKAIAN");
        public static string RoupasCabeca => L("Só visual: não muda o jogo.", "Cosmetic only: no gameplay change.", "Solo estético: no cambia el juego.",
            "Hanya tampilan: tak mengubah permainan.");

        /// <summary>Nome da roupa: pt do Core (OutfitDef.Name); o resto pela tabela.</summary>
        public static string RoupaNome(Outfit o)
        {
            string pt = (int)o >= 0 && (int)o < OutfitDef.All.Length ? OutfitDef.All[(int)o].Name : o.ToString();
            switch (o)
            {
                case Outfit.Classic: return L(pt, "Classic", "Clásico", "Klasik");
                case Outfit.Night: return L(pt, "Night", "Nocturno", "Malam");
                case Outfit.Royal: return L(pt, "Royal", "Real", "Bangsawan");
                case Outfit.Explorer: return L(pt, "Explorer", "Explorador", "Penjelajah");
                case Outfit.Forge: return L(pt, "Forge firefighter", "Bombero de forja", "Pemadam tempaan");
                case Outfit.Ghost: return L(pt, "Ghost", "Fantasma", "Hantu");
                case Outfit.Miner: return L(pt, "Miner", "Minero", "Penambang");
                case Outfit.Gravekeeper: return L(pt, "Gravekeeper", "Sepulturero", "Penjaga makam");
                default: return pt;
            }
        }

        public static string RoupaOrigem(Outfit o)
        {
            switch (o)
            {
                case Outfit.Classic: return L("a de sempre (grátis)", "the usual one (free)", "el de siempre (gratis)", "yang biasa (gratis)");
                case Outfit.Explorer: return L("na loja ou no Pacote inicial", "in the shop or the Starter Pack", "en la tienda o el Paquete inicial", "di toko atau Paket pemula");
                case Outfit.Ghost: return L("marco de 95 estrelas ou temporada (não se compra)", "95-star milestone or season (not for sale)",
                    "logro de 95 estrellas o temporada (no se compra)", "pencapaian 95 bintang atau musim (tak dijual)");
                default: return L("na loja", "in the shop", "en la tienda", "di toko");
            }
        }

        // loja (IAP de teste; conteudo fixo e listado, nada aleatorio)
        public static string Loja => L("Loja", "Shop", "Tienda", "Toko");
        public static string LojaBotao => L("LOJA", "SHOP", "TIENDA", "TOKO");
        public static string LojaCabeca => L("Compras de teste: nada é cobrado. Conteúdo fixo e listado, nada aleatório.",
            "Test purchases: nothing is charged. Fixed, listed content, nothing random.",
            "Compras de prueba: no se cobra nada. Contenido fijo y listado, nada aleatorio.",
            "Pembelian uji: tidak ada tagihan. Isi tetap dan tertulis, tanpa acak.");
        public static string Comprado => L("COMPRADO", "OWNED", "COMPRADO", "DIMILIKI");
        public static string Preco(string valor) => F("R$ {0} (teste)", "R$ {0} (test)", "R$ {0} (prueba)", "R$ {0} (uji)", valor);
        public static string StarterDepois(int c) => F("aparece depois do C{0}", "shows up after C{0}", "aparece después del C{0}", "muncul setelah C{0}", c);
        public static string PacoteSemana => L("comprado: vale até domingo", "owned: valid until Sunday", "comprado: vale hasta el domingo", "dimiliki: berlaku sampai Minggu");
        public static string LojaTeste => L("Loja de teste", "Test store", "Tienda de prueba", "Toko uji");
        public static string ConfirmarCompra(string nome, string preco) => F("Comprar {0} por {1}?\n\nCompra de teste: nada é cobrado.",
            "Buy {0} for {1}?\n\nTest purchase: nothing is charged.", "¿Comprar {0} por {1}?\n\nCompra de prueba: no se cobra nada.",
            "Beli {0} seharga {1}?\n\nPembelian uji: tidak ada tagihan.", nome, preco);
        public static string Confirmar => L("CONFIRMAR", "CONFIRM", "CONFIRMAR", "KONFIRMASI");
        public static string Cancelar => L("CANCELAR", "CANCEL", "CANCELAR", "BATAL");
        // loja real (Unity IAP no Google Play; MONETIZACAO.md)
        public static string LojaCabecaReal => L("Conteúdo fixo e listado, nada aleatório.", "Fixed, listed content, nothing random.",
            "Contenido fijo y listado, nada aleatorio.", "Isi tetap dan tertulis, tanpa acak.");
        public static string LojaCarregando => "…";
        public static string LojaIndisponivel => L("indisponível", "unavailable", "no disponible", "tidak tersedia");
        public static string CompraPendente => L("Compra pendente: o conteúdo chega quando o pagamento for aprovado.",
            "Purchase pending: the content arrives when the payment is approved.", "Compra pendiente: el contenido llega cuando se apruebe el pago.",
            "Pembelian tertunda: isi masuk setelah pembayaran disetujui.");
        public static string CompraFalhou => L("A compra não foi concluída.", "The purchase was not completed.", "La compra no se completó.",
            "Pembelian tidak selesai.");
        public static string Restaurar => L("Restaurar compras", "Restore purchases", "Restaurar compras", "Pulihkan pembelian");
        public static string RestaurarSub => L("Devolve o que já foi comprado nesta conta da loja.", "Brings back what this store account already bought.",
            "Devuelve lo que ya se compró con esta cuenta de la tienda.", "Mengembalikan yang sudah dibeli akun toko ini.");
        public static string RestaurarBotao => L("RESTAURAR", "RESTORE", "RESTAURAR", "PULIHKAN");
        public static string Restaurado => L("Compras restauradas.", "Purchases restored.", "Compras restauradas.", "Pembelian dipulihkan.");
        public static string RestaurarFalhou => L("Não deu para restaurar agora. Tente de novo com internet.", "Could not restore now. Try again online.",
            "No se pudo restaurar ahora. Inténtalo de nuevo con internet.", "Belum bisa memulihkan. Coba lagi saat online.");

        public static string ProdutoNome(Product p)
        {
            switch (p)
            {
                case Product.RemoveAds: return L("Remover anúncios", "Remove ads", "Quitar anuncios", "Hapus iklan");
                case Product.StarterPack: return L("Pacote inicial", "Starter Pack", "Paquete inicial", "Paket pemula");
                case Product.EventPack: return L("Pacote de evento", "Event pack", "Paquete de evento", "Paket event");
                default: return F("Roupa {0}", "{0} outfit", "Traje {0}", "Pakaian {0}", RoupaNome(RoupaDoProduto(p)));
            }
        }

        /// <summary>Roupa que o produto da (so os Product.Outfit*).</summary>
        public static Outfit RoupaDoProduto(Product p)
        {
            switch (p)
            {
                case Product.OutfitNight: return Outfit.Night;
                case Product.OutfitRoyal: return Outfit.Royal;
                case Product.OutfitExplorer: return Outfit.Explorer;
                case Product.OutfitMiner: return Outfit.Miner;
                case Product.OutfitGravekeeper: return Outfit.Gravekeeper;
                default: return Outfit.Forge;
            }
        }

        /// <summary>O que vem na compra, sempre fixo e listado (guardrail: nada aleatorio pago).</summary>
        public static string ProdutoConteudo(Product p)
        {
            switch (p)
            {
                case Product.RemoveAds: return L("Tira os anúncios entre contratos. Os anúncios com recompensa continuam opcionais.",
                    "Removes ads between contracts. Reward ads stay optional.", "Quita los anuncios entre contratos. Los anuncios con recompensa siguen siendo opcionales.",
                    "Menghapus iklan antar kontrak. Iklan berhadiah tetap opsional.");
                case Product.StarterPack: return F("{0} moedas + roupa Explorador + 1 projeto de cada equipamento (4; já destravado = {1} moedas)",
                    "{0} coins + Explorer outfit + 1 blueprint of each gear (4; already unlocked = {1} coins)",
                    "{0} monedas + traje Explorador + 1 plano de cada equipo (4; ya desbloqueado = {1} monedas)",
                    "{0} koin + pakaian Penjelajah + 1 cetak biru tiap perlengkapan (4; sudah terbuka = {1} koin)", Balance.StarterCoins, Balance.StarterBlueprintCoins);
                case Product.EventPack: return F("+{0} vagas no evento desta semana (+{1} e +{2} moedas)", "+{0} slots in this week's event (+{1} and +{2} coins)",
                    "+{0} puestos en el evento de esta semana (+{1} y +{2} monedas)", "+{0} slot di event minggu ini (+{1} dan +{2} koin)",
                    Balance.EventPackContracts, Sim.EventReward(3), Sim.EventReward(4));
                default: return L("Só visual: troca a roupa do courier.", "Cosmetic only: changes the courier's outfit.", "Solo estético: cambia el traje del courier.",
                    "Hanya tampilan: mengganti pakaian kurir.");
            }
        }

        // anuncios (simulador de teste) e ofertas de rewarded (§7)
        public static string AnuncioTeste => L("ANÚNCIO DE TESTE", "TEST AD", "ANUNCIO DE PRUEBA", "IKLAN UJI");
        // anuncio real (LevelPlay): espera do rewarded
        public static string AnuncioCarregando => L("Carregando o anúncio…", "Loading the ad…", "Cargando el anuncio…", "Memuat iklan…");
        public static string AnuncioSemAnuncio => L("Nenhum anúncio disponível agora.\nTente de novo mais tarde.", "No ad available right now.\nTry again later.",
            "No hay anuncios disponibles ahora.\nInténtalo más tarde.", "Belum ada iklan sekarang.\nCoba lagi nanti.");
        public static string AnuncioGanha(string premio) => F("Assista até o fim para ganhar:\n{0}", "Watch to the end to get:\n{0}",
            "Mira hasta el final para ganar:\n{0}", "Tonton sampai habis untuk dapat:\n{0}", premio);
        public static string AnuncioEntre => L("Anúncio entre contratos.\nRemover anúncios tira este.", "Ad between contracts.\nRemove ads turns these off.",
            "Anuncio entre contratos.\nQuitar anuncios los desactiva.", "Iklan antar kontrak.\nHapus iklan mematikannya.");
        public static string AnuncioSemSdk => L("simulador no PC: nenhum anúncio real", "PC simulator: no real ad", "simulador en PC: ningún anuncio real",
            "simulator PC: bukan iklan asli");
        public static string Fechar => L("FECHAR", "CLOSE", "CERRAR", "TUTUP");
        public static string PularSemPremio => L("PULAR (sem recompensa)", "SKIP (no reward)", "SALTAR (sin recompensa)", "LEWATI (tanpa hadiah)");
        public static string PremioRetomar => L("voltar à entrada da sala com vida cheia", "back to the room entrance with full health",
            "volver a la entrada de la sala con vida llena", "kembali ke pintu masuk ruang dengan nyawa penuh");
        public static string PremioDobro(int n) => F("+{0} moedas (bônus dobrado)", "+{0} coins (double bonus)", "+{0} monedas (bono doble)", "+{0} koin (bonus ganda)", n);
        public static string PremioRota => L("a porta mais segura desta sala", "the safest door in this room", "la puerta más segura de esta sala", "pintu paling aman di ruang ini");
        public static string PremioBau(int moedas, Gear g) => g == Gear.None
            ? F("{0} moedas", "{0} coins", "{0} monedas", "{0} koin", moedas)
            : F("{0} moedas + 1 projeto: {1}", "{0} coins + 1 blueprint: {1}", "{0} monedas + 1 plano: {1}", "{0} koin + 1 cetak biru: {1}", moedas, GearNome(g));
        public static string OfertaRetomar => L("RETOMAR DA SALA\nver anúncio de teste", "RESUME THE ROOM\nwatch a test ad", "RETOMAR LA SALA\nver anuncio de prueba",
            "LANJUTKAN RUANG\ntonton iklan uji");
        public static string OfertaDobro(int n) => F("DOBRAR BÔNUS +{0}\nver anúncio de teste", "DOUBLE BONUS +{0}\nwatch a test ad",
            "DOBLAR BONO +{0}\nver anuncio de prueba", "GANDAKAN BONUS +{0}\ntonton iklan uji", n);
        public static string OfertaRota => L("ROTA SEGURA\nver anúncio de teste", "SAFE ROUTE\nwatch a test ad", "RUTA SEGURA\nver anuncio de prueba",
            "RUTE AMAN\ntonton iklan uji");
        public static string ProtejaRetome => L("Proteja a carga e retome: volte à entrada da sala com vida cheia.",
            "Protect the cargo and resume: go back to the room entrance with full health.",
            "Protege la carga y retoma: vuelve a la entrada de la sala con vida llena.",
            "Lindungi paket dan lanjutkan: kembali ke pintu masuk ruang dengan nyawa penuh.");
        public static string BauDia => L("Baú do dia", "Daily chest", "Cofre del día", "Peti harian");
        public static string VerAnuncio => L("VER ANÚNCIO", "WATCH AD", "VER ANUNCIO", "TONTON IKLAN");
        public static string Aberto => L("ABERTO", "OPENED", "ABIERTO", "DIBUKA");
        public static string BauVolte => L("aberto hoje: volte amanhã", "opened today: come back tomorrow", "abierto hoy: vuelve mañana", "sudah dibuka: kembali besok");

        /// <summary>Dica da HUD com a rota segura revelada (rewarded): a porta pela posicao no topo da sala (9 m). So literais.</summary>
        public static string RotaSegura(float x) => x < 3f
            ? L("rota segura: a porta da esquerda", "safe route: the left door", "ruta segura: la puerta de la izquierda", "rute aman: pintu kiri")
            : x > 6f ? L("rota segura: a porta da direita", "safe route: the right door", "ruta segura: la puerta de la derecha", "rute aman: pintu kanan")
            : L("rota segura: a porta do meio", "safe route: the middle door", "ruta segura: la puerta del medio", "rute aman: pintu tengah");

        public static string Num(int v) => v.ToString(Inv);
        /// <summary>Relogio da HUD: "s" (simbolo SI) em todas as linguas, cabe no espaco curto da faixa; o texto corrido do ID usa "dtk".</summary>
        public static string Segundos(float s) => Math.Max(0, (int)Math.Ceiling(s)).ToString(Inv) + " s";
    }
}
