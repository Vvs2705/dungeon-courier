using System;
using System.Collections;
using System.Globalization;
using System.IO;
using DC.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Rect = UnityEngine.Rect;

namespace DC
{
    /// <summary>
    /// Fluxo do jogo, o unico MonoBehaviour (nasce sozinho na cena vazia por RuntimeInitializeOnLoadMethod): carrega/grava o Sim no
    /// PlayerPrefs, le o gesto de um dedo (Joystick) e WASD/setas + Espaco no PC, chama sim.Tick com o dt do quadro (o Core faz o passo
    /// fixo), transforma os eventos do Core em SFX, efeitos, vibracao e diario CSV (persistentDataPath/diario.csv), e liga HUD/telas
    /// (Hud + Menus) e mundo (WorldView). App em segundo plano: grava e abre a pausa (sem Tick ate o jogador continuar).
    /// Diario (CONTRATO_V1 §10): cada linha leva o id anonimo da instalacao (GUID local no PlayerPrefs, sem dado pessoal) e a versao do
    /// app; nada sai do aparelho sem o jogador tocar em Exportar diario (Exportar.cs).
    /// Flags de dev (CONTRATO §11):
    ///   -autoplay N   sem render: o Bot do Core joga N contratos em sequencia (antes de cada um compra o mais barato da guilda/armas e
    ///                 equipa a arma da carga, como o Bot.PlayCampaign; aceita o resgate), loga 1 linha por contrato e AUTOPLAY OK|FAIL;
    ///                 sai 0/1. Usar com -batchmode -nographics.
    ///   -bot          o Bot dirige a partida com render (e avanca quadro -> resultado -> proximo contrato sozinho; escolhe o perk).
    ///   -speed N      multiplica o dt (0,1..20).
    ///   -shot f.png [-shotdelay s]   foto da tela depois de s segundos reais (padrao 1) e sai 0.
    ///   -contract K   K 1-based (C1..C24): libera ate K NA MEMORIA e comeca direto (aceita o resgate se houver). Use com -testsession.
    ///   -tela X       abre uma tela direto para a foto: titulo|menu|quadro|codigo|guilda|armas|config|creditos.
    ///   -pausa        (com -testsession -contract) abre a pausa logo no inicio (foto).   -tela perk: escolha de perk de exemplo.
    ///   -weapon W     (com -testsession -contract) arma 0 estilingue, 1 bumerangue, 2 bombinha: destrava so na memoria e equipa.
    ///   -reset        apaga o save antes de carregar.   -testsession  nao le nem grava save nem diario (semente fixa 1).
    /// </summary>
    public sealed class Game : MonoBehaviour
    {
        const string SaveKey = "dc.save", InstallKey = "dc_instalacao";
        const string DiaryHeader = "utc,instalacao,versao,sessao,t_sessao,t_contrato,evento,a,b,contrato,sala";
        const float EndDelay = 1.1f, MaxChunk = 0.2f;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public Sim Sim { get; private set; }
        /// <summary>Versao do app (Application.version = Setup.Version), mostrada nas telas e gravada no diario.</summary>
        public string Version { get; private set; } = "";
        /// <summary>Id anonimo desta instalacao ("-" no -testsession).</summary>
        public string InstallId { get; private set; } = "-";
        public string InstallShort => InstallId.Length > 8 ? InstallId.Substring(0, 8) : InstallId;
        public bool BotDrive => _botDrive;
        /// <summary>Data local AAAAMMDD do contrato do dia (o Core nao le relogio; voltar o relogio nao repete: sim.SeeDate).</summary>
        public int Hoje { get { DateTime d = DateTime.Now; return d.Year * 10000 + d.Month * 100 + d.Day; } }

        Bot _bot;
        WorldView _view;
        Hud _hud;
        Joystick _joy;
        Camera _cam;
        float _speed = 1f, _botAcc, _endT, _backT;
        bool _test, _headless, _botDrive, _paused, _skipDt, _started;
        string _diary, _sid;
        Vector2Int _screen;
        enum Mode { Board, Play, Ending, After }
        Mode _mode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<Game>() == null) new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            _headless = Arg("-autoplay") != null;
            _test = _headless || Arg("-testsession") != null;
            if (_headless) return;   // o Start roda o autoplay sem montar camera, HUD nem som

            Version = Application.version;
            _sid = Guid.NewGuid().ToString("N").Substring(0, 8);
            _diary = Path.Combine(Application.persistentDataPath, "diario.csv");
            if (!_test) { InstallId = Install(); RotateDiary(); }
            _speed = Mathf.Clamp(ArgF("-speed", 1f), 0.1f, 20f);
            Audio.Init(gameObject);
            Ajustes.Init();   // idioma e volumes antes de montar as telas
            if (!_test && Arg("-reset") != null) { PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); }
            string saved = _test ? "" : PlayerPrefs.GetString(SaveKey, "");
            // jogador novo: semente aleatoria (variedade entre jogadores); -testsession: semente 1 (fotos repetiveis)
            Sim = string.IsNullOrEmpty(saved) ? new Sim(_test ? 1 : UnityEngine.Random.Range(1, int.MaxValue)) : Sim.Load(saved);
            _bot = new Bot(Sim.Seed);

            _cam = new GameObject("Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Art.Bg;
            _cam.nearClipPlane = 0.1f; _cam.farClipPlane = 50f;
            _hud = new Hud(this);
            _view = new WorldView(_cam, _hud.Labels);
            var joy = new GameObject("JoystickCanvas", typeof(Canvas)).GetComponent<Canvas>();   // escala 1: px = px (dp direto)
            joy.renderMode = RenderMode.ScreenSpaceOverlay;
            joy.sortingOrder = 2;
            _joy = new Joystick(joy.transform);
            Fit();
        }

        void Start()
        {
            if (_headless) { Autoplay(); return; }
            _botDrive = Arg("-bot") != null;   // antes do 1o Log: partida do bot nao entra no diario do playtest
            // privacidade (TESTE_JOGADORES): faixa de RAM em vez do modelo do aparelho (modelo raro identificaria o testador)
            int ram = SystemInfo.systemMemorySize;
            Log("session_start", ram <= 0 ? "ram?" : ram <= 3072 ? "ram<=3GB" : ram <= 6144 ? "ram3-6GB" : "ram>6GB", Application.platform.ToString());
            if (Sim.SaveRefused) Log("save_refused");
            SeeDate();
            if (Sim.InContract)
            {
                // contrato retomado do save: comeca na entrada da sala gravada, pausado
                EnterPlay();
                HandleEvents();
                if (!_botDrive) _hud.ShowPause(true);
            }
            else
            {
                Sim.Events.Clear();
                if (int.TryParse(Arg("-contract"), NumberStyles.Integer, Inv, out int k1))
                {
                    int k = Mathf.Clamp(k1 - 1, 0, Sim.Contracts.Length - 1);
                    Sim.Reputation = Math.Max(Sim.Reputation, k);   // dev: libera ate K so na memoria (com -testsession nada e gravado)
                    if (_test && int.TryParse(Arg("-weapon"), NumberStyles.Integer, Inv, out int w) && w >= 0 && w < WeaponDef.All.Length)
                    {
                        Sim.DevStars(WeaponDef.All[w].Stars);   // dev: so na memoria, para fotografar bumerangue/bombinha
                        Sim.Equip((Weapon)w);
                    }
                    StartContract(k, Sim.Pending != null);
                    if (_test && Arg("-pausa") != null) _hud.ShowPause(false);   // dev: foto da pausa
                }
                else if (!string.IsNullOrEmpty(Arg("-tela"))) _hud.Dev(Arg("-tela"));
            }
            _started = true;
            string shot = Arg("-shot");
            if (!string.IsNullOrEmpty(shot)) StartCoroutine(Shot(shot));
        }

        void Update()
        {
            if (_headless) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (_skipDt) { dt = 0f; _skipDt = false; }
            if (_screen.x != Screen.width || _screen.y != Screen.height) Fit();
            BackKey();

            bool playing = _mode == Mode.Play && !_hud.Blocking && !_paused;
            _joy.Blocked = !playing || _botDrive;
            _joy.TopPx = _hud.TopPx;
            _joy.Tick();
            if (playing)
            {
                // botao clicado fica "selecionado" no EventSystem e o Espaco (Submit) o apertaria de novo: no contrato, nada selecionado
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null) EventSystem.current.SetSelectedGameObject(null);
                Simulate(dt * _speed);
            }
            else if (_mode == Mode.Ending)
            {
                _endT -= dt;
                if (_endT <= 0f) { _mode = Mode.After; _hud.ShowEnd(Sim.LastResult); _hud.RefreshMeta(); }
            }
            if (_botDrive) BotFlow();

            _view.Refresh(_mode == Mode.Board ? null : Sim.Run, dt);
            _hud.Refresh(dt);
            if (Sim.Dirty) Save();
        }

        void Simulate(float dt)
        {
            if (_botDrive)
            {
                // o Bot joga um passo fixo por Act (Act ja chama sim.Tick e escolhe o perk quando ha oferta)
                _botAcc += dt;
                while (_botAcc >= Balance.Step && Sim.InContract) { _botAcc -= Balance.Step; _bot.Act(Sim); HandleEvents(); }
            }
            else
            {
                Vector2 mv = _joy.Direction;
                bool dash = _joy.Tap;
                Keyboard k = Keyboard.current;
                if (k != null)
                {
                    float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
                    float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
                    if (x != 0f || y != 0f) mv = new Vector2(x, y).normalized;
                    dash |= k.spaceKey.wasPressedThisFrame;
                }
                var move = new V2(mv.x, mv.y);   // dedo solto = V2.Zero: o Core conta os 200 ms para pousar
                // o Core corta dt > 0,25 s; com -speed alto o quadro vira varios Tick (e o dash vai so no 1o; o Core guarda ate o passo)
                do
                {
                    float s = Mathf.Min(dt, MaxChunk);
                    dt -= s;
                    Sim.Tick(s, move, dash);
                    dash = false;
                    HandleEvents();
                } while (dt > 0f && Sim.InContract && Sim.Run.PerkOffer == null);
            }
            if (!Sim.InContract) { _mode = Mode.Ending; _endT = EndDelay; }
        }

        // ------------------------------------------------------------------ acoes chamadas pelas telas

        /// <summary>Contrato i da campanha (toque no quadro): com carga pendente abre a oferta de resgate; senao comeca.</summary>
        public void Play(int i) => _hud.ShowOffer(rescue => Begin(Sim.StartContract(i, rescue)));

        /// <summary>Contrato do dia (data local de hoje).</summary>
        public void PlayDaily()
        {
            int d = Hoje;
            _hud.ShowOffer(rescue => Begin(Sim.StartDaily(d, rescue)));
        }

        /// <summary>Contrato por codigo (ja normalizado e validado pela tela).</summary>
        public void PlayCode(string code) => _hud.ShowOffer(rescue => Begin(Sim.StartCode(code, rescue)));

        /// <summary>-contract e -bot: comeca direto.</summary>
        public void StartContract(int i, bool rescue) => Begin(Sim.StartContract(i, rescue));

        bool Begin(Run run)
        {
            if (run == null) { _hud.RefreshMeta(); return false; }   // fechado ou save recusado: a tela continua onde estava
            EnterPlay();
            HandleEvents();   // contract_start (+ daily_start/code_start), cargo_rescue_offer, room_enter
            Save();
            return true;
        }

        void EnterPlay() { _mode = Mode.Play; _botAcc = 0f; _hud.ShowPlay(); Musica(Sim.Run.Room); }

        /// <summary>Faixa do contrato (docs/AUDIO.md §2): chefe no M1/B1, masmorra no resto. Fora do contrato as telas tocam "menu".</summary>
        static void Musica(RoomDef room) => Audio.Music(room != null && (room.Boss || room.Mini) ? "chefe" : "masmorra");

        public void Buy(Upgrade u)
        {
            if (!Sim.Buy(u)) return;
            AfterMeta();   // guild_upgrade, currency_sink
        }

        public void UpgradeWeapon(Weapon w)
        {
            if (!Sim.CanUpgradeWeapon(w)) return;
            Sim.UpgradeWeapon(w);
            AfterMeta();   // currency_sink (B = 10 + arma)
        }

        public void Equip(Weapon w)
        {
            if (Sim.InContract || !Sim.WeaponUnlocked(w) || Sim.Equipped == w) return;
            Sim.Equip(w);
            AfterMeta();   // weapon_equip
        }

        void AfterMeta()
        {
            HandleEvents();
            Save();
            _hud.RefreshMeta();
        }

        /// <summary>Escolha de perk (1 de 3): o Core volta a rodar o Tick.</summary>
        public void PickPerk(int i)
        {
            if (!Sim.InContract || Sim.Run.PerkOffer == null) return;
            Sim.PickPerk(i);
            HandleEvents();   // perk_pick
            Save();
        }

        /// <summary>Desistir (confirmado na pausa): o contrato falha sem carga pendente.</summary>
        public void Abandon()
        {
            if (!Sim.InContract) return;
            Sim.Abandon();
            HandleEvents();
            _mode = Mode.Ending; _endT = 0.3f;
        }

        public void ToBoard() { _mode = Mode.Board; _hud.ShowBoard(); }

        /// <summary>Guarda a maior data vista (contrato do dia: voltar o relogio nao repete). Menu e quadro chamam ao abrir.</summary>
        public void SeeDate()
        {
            if (Sim != null && !Sim.SaveRefused) Sim.SeeDate(Hoje);
        }

        public void SetIdioma(bool ingles)
        {
            if (Ajustes.Ingles == ingles) return;
            Ajustes.Ingles = ingles;
            _hud.Relocalizar();
            Log("settings", "idioma", ingles ? "en" : "pt");
        }

        /// <summary>Configuracoes > Exportar diario: Android compartilha o CSV; Windows abre a pasta.</summary>
        public Exportar.Status ExportarDiario()
        {
            Log("diary_export", Application.platform.ToString());
            return Exportar.Diario(_test ? null : _diary, Textos.ExportarDiario);
        }

        // ------------------------------------------------------------------ eventos do Core -> som, efeito, vibracao, diario

        void HandleEvents()
        {
            Run run = Sim.Run;
            foreach (SimEvent e in Sim.Events)
            {
                string name = SimEvent.DiaryName(e.Kind);
                if (name != null) Diary(name, e);
                _view.OnEvent(e, run);
                // som e haptica: tabela do docs/AUDIO.md §1 (banco do Audio.cs; nome sem clipe nao toca nada)
                switch (e.Kind)
                {
                    case Ev.Plant: Audio.Play("pousar"); break;
                    case Ev.CargoPickup: Audio.Play("pegar"); break;
                    case Ev.CargoIntegrity: Audio.Play("racha"); Ajustes.Vibrar(); break;
                    case Ev.CargoBroken: Audio.Play("quebra"); Ajustes.Vibrar(0f); break;
                    case Ev.ThiefGrab: Audio.Play("ladrao"); break;
                    case Ev.ThiefSpawn: Audio.Play("ladrao", 1.3f); break;
                    case Ev.ThiefEscape: Audio.Play("ladrao", 0.7f); break;
                    case Ev.Dash: Audio.Play("dash"); break;
                    case Ev.CakeWobble: Audio.Play("balanca", 1f, 0.3f); break;
                    case Ev.Shot: Audio.Play(e.B == (int)Weapon.Bomb ? "bomba" : e.B == (int)Weapon.Boomerang ? "bumerangue" : "tiro", 1f, 0.08f); break;
                    case Ev.Explosion: Audio.Play("explosao"); Ajustes.Vibrar(0f); break;
                    case Ev.EnemyHit: Audio.Play("golpe"); break;
                    case Ev.EnemyDie: Audio.Play(e.B == (int)EnemyType.Slime ? "limo" : "morte"); break;
                    case Ev.PuddleSpawn: Audio.Play("limo", 0.8f, 0.2f); break;
                    case Ev.DamageTaken: Audio.Play("dano"); _hud.Hurt(); break;
                    case Ev.PetHit: Audio.Play("dano", 1.4f); break;
                    case Ev.Telegraph:
                        if (e.B == (int)EnemyType.Archer) Audio.Play("mira");
                        else Audio.Play("aviso", e.B == (int)EnemyType.Ogre ? 0.7f : 1f);
                        break;
                    case Ev.ArrowShot: Audio.Play("flecha"); break;
                    case Ev.Strike:
                        if (e.B == (int)EnemyType.Ogre) { Audio.Play("pancada"); Ajustes.Vibrar(); }
                        else Audio.Play("golpe", 0.6f);
                        break;
                    case Ev.CaptainCall: Audio.Play("rugido", 1.3f); break;
                    case Ev.RouteChoice: Audio.Play("porta"); break;
                    case Ev.MessageDoor: Audio.Play("porta", 1.2f); break;
                    case Ev.RoomEnter:
                        _hud.FadeIn();
                        if (e.A >= 0 && e.A < Content.Rooms.Length)
                        {
                            RoomDef room = Content.Rooms[e.A];
                            Musica(room);
                            if (room.Boss || room.Mini) Audio.Play("rugido", room.Mini ? 1.3f : 1f);
                        }
                        break;
                    case Ev.RoomEvent: Audio.Play("aviso", 0.8f); break;
                    case Ev.DoorBlocked: Audio.Play("bloqueio", 1f, 0.3f); break;
                    case Ev.SecretFound: Audio.Play("segredo"); break;
                    case Ev.ChestOpen: Audio.Play("bau"); break;
                    case Ev.CurrencySource: if (e.B != 1) Audio.Play("moedas", 1.2f, 0.2f); break;   // o bau ja tocou no ChestOpen
                    case Ev.Hatch: Audio.Play("choca"); break;
                    case Ev.Deliver: Audio.Play(run != null && run.Main.Cracks > 0 ? "entrega_rachada" : "entrega"); break;
                    case Ev.Stun: Audio.Play("golpe", 0.5f); break;
                    case Ev.PerkPick: Audio.Play("perk"); break;
                    case Ev.GuildUpgrade: case Ev.WeaponUpgrade: Audio.Play("compra"); break;
                    case Ev.ContractEnd:
                        Audio.Music(null);   // silencio para o jingle de entrega/falha soar limpo
                        ContractResult r = Sim.LastResult;
                        if (r != null && !r.Delivered) Audio.Play("falha");
                        if (r != null && r.Delivered && r.Cracks == 0 && !r.Hatched) Ajustes.Vibrar(0f);   // entrega perfeita
                        break;
                }
            }
            Sim.Events.Clear();   // consumidos: Start/Buy/Abandon acrescentam fora do Tick e nao podem ser tratados duas vezes
        }

        void Diary(string evt, SimEvent e)
        {
            bool inRun = _mode == Mode.Play && Sim.Run != null;
            Log(evt, e.A.ToString(Inv), e.B.ToString(Inv), inRun ? Sim.Run.Contract.Code : "", inRun ? Sim.Run.Room.Code : "");
        }

        /// <summary>Diario CSV de playtest (DiaryHeader): utc, instalacao, versao, sessao, s desde a abertura, s do contrato, evento, a, b,
        /// contrato, sala.</summary>
        public void Log(string evt, string a = "", string b = "", string contract = "", string room = "")
        {
            if (_test || _botDrive || _diary == null) return;
            try
            {
                if (!File.Exists(_diary)) File.AppendAllText(_diary, DiaryHeader + "\n");
                float t = Sim != null && Sim.Run != null && _mode != Mode.Board ? Sim.Run.Time : 0f;
                File.AppendAllText(_diary, string.Format(Inv, "{0:yyyy-MM-ddTHH:mm:ss},{1},{2},{3},{4:0.0},{5:0.0},{6},{7},{8},{9},{10}\n",
                    DateTime.UtcNow, InstallId, Version, _sid, Time.realtimeSinceStartup, t, evt, a, b, contract, room));
            }
            catch (Exception) { }   // ponytail: diario e best-effort; disco cheio nunca derruba o jogo
        }

        /// <summary>GUID gerado na 1ª abertura e guardado no PlayerPrefs (fora do save; o -reset nao apaga). Nao identifica a pessoa.</summary>
        static string Install()
        {
            string id = PlayerPrefs.GetString(InstallKey, "");
            if (id.Length == 0) { id = Guid.NewGuid().ToString("N"); PlayerPrefs.SetString(InstallKey, id); PlayerPrefs.Save(); }
            return id;
        }

        /// <summary>Diario com o cabecalho da v0.1 (sem instalacao/versao) vira diario_anterior.csv: colunas novas nao se misturam com
        /// as velhas. ponytail: so guarda 1 anterior (troca de cabecalho e rara).</summary>
        void RotateDiary()
        {
            try
            {
                if (!File.Exists(_diary)) return;
                string first;
                using (var rd = new StreamReader(_diary)) first = rd.ReadLine();
                if (first == DiaryHeader) return;
                string old = Path.Combine(Path.GetDirectoryName(_diary), "diario_anterior.csv");
                if (File.Exists(old)) File.Delete(old);
                File.Move(_diary, old);
            }
            catch (Exception) { }
        }

        // ------------------------------------------------------------------ save, pausa, voltar

        void Save()
        {
            if (_test || Sim == null || Sim.SaveRefused) return;   // save de versao futura: nunca gravar por cima
            string s = Sim.Save();
            if (s == null) return;
            PlayerPrefs.SetString(SaveKey, s);
            PlayerPrefs.Save();
        }

        void OnApplicationPause(bool paused) => Away(paused);
        void OnApplicationFocus(bool focus) => Away(!focus);
        void OnApplicationQuit() { Log("session_end", Time.realtimeSinceStartup.ToString("0", Inv)); Save(); }

        /// <summary>App saiu da frente: grava e abre a pausa (o contrato fica sem Tick ate CONTINUAR). Voltou: descarta o dt do quadro.</summary>
        void Away(bool away)
        {
            if (_headless || Sim == null || !_started) return;
            if (away)
            {
                if (!_paused) Log("app_background", Time.realtimeSinceStartup.ToString("0", Inv));   // fim de sessao no Android (o quit raramente chega)
                Save();
                if (_mode == Mode.Play && !_botDrive) _hud.ShowPause(false);
            }
            else _skipDt = true;
            _paused = away;
        }

        /// <summary>Esc no PC / voltar do Android: fecha a tela de cima (Hud.Back); no contrato abre a pausa; no titulo/menu grava e sai.</summary>
        void BackKey()
        {
            bool back = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
            back |= Input.GetKeyDown(KeyCode.Escape);   // com o GameActivity o voltar do Android so chega ao Input legado (Setup: Both)
#endif
            if (!back || Time.unscaledTime < _backT) return;
            _backT = Time.unscaledTime + 0.4f;   // o mesmo voltar pode chegar pelos dois sistemas em 2 quadros (Forge Street)
            if (_hud.Back()) return;
            if (_mode == Mode.Play) _hud.ShowPause(false);
            else if (_mode == Mode.Board) { Save(); Application.Quit(); }
        }

        void Fit()
        {
            _screen = new Vector2Int(Screen.width, Screen.height);
            _hud.Fit();
            Rect sa = Screen.safeArea;
            _view.Fit(new Rect(sa.xMin, sa.yMin, sa.width, Mathf.Max(1f, _hud.TopPx - sa.yMin)));
        }

        // ------------------------------------------------------------------ dev: bot, autoplay, foto

        /// <summary>-bot: no quadro faz as compras e equipa a arma (Shop) e comeca o proximo contrato; no resultado volta ao quadro depois
        /// de 2,5 s.</summary>
        void BotFlow()
        {
            if (_mode == Mode.Board && _hud.TimeInScreen > 1f)
            {
                int c = Math.Min(Sim.Reputation, Sim.Contracts.Length - 1);
                Shop(Sim, c);
                AfterMeta();   // diario (guild_upgrade, weapon_equip...), save e telas
                StartContract(c, Sim.Pending != null);
                if (_mode == Mode.Board) _botDrive = false;   // nao comecou (save recusado): o bot para
            }
            else if (_mode == Mode.After && _hud.Current == Hud.Tela.Result && _hud.TimeInScreen > 2.5f) ToBoard();
        }

        /// <summary>Como o Bot.PlayCampaign: compra o mais barato que couber (guilda ou nivel de arma destravada; empate = guilda, linha de
        /// cima) ate nao caber nada, e equipa a arma boa para a carga do contrato c.</summary>
        static void Shop(Sim s, int c)
        {
            while (true)
            {
                int pick = -1, cost = int.MaxValue;
                for (int u = 0; u < Balance.UpgradeLines; u++) { int k = s.UpgradeCost((Upgrade)u); if (s.CanBuy((Upgrade)u) && k < cost) { cost = k; pick = u; } }
                for (int w = 0; w < WeaponDef.All.Length; w++) { int k = s.WeaponCost((Weapon)w); if (s.CanUpgradeWeapon((Weapon)w) && k < cost) { cost = k; pick = 10 + w; } }
                if (pick < 0) break;
                if (pick >= 10) s.UpgradeWeapon((Weapon)(pick - 10)); else s.Buy((Upgrade)pick);
            }
            Weapon arma = Bot.WeaponFor(s, Sim.Contracts[c].Cargo);
            if (s.Equipped != arma) s.Equip(arma);
        }

        /// <summary>-autoplay N (smoke do build, sem render): N contratos do Bot em sequencia. OK = todos comecaram, entregou ao menos
        /// metade, passou do C1 e o save faz ida e volta sem perder meta.</summary>
        void Autoplay()
        {
            int n = int.TryParse(Arg("-autoplay"), NumberStyles.Integer, Inv, out int v) && v > 0 ? v : 8;
            var sim = new Sim(1);
            var bot = new Bot(1);
            int delivered = 0; float time = 0f; bool ok = true;
            try
            {
                for (int i = 0; i < n; i++)
                {
                    int c = Math.Min(sim.Reputation, Sim.Contracts.Length - 1);
                    Shop(sim, c);
                    bool rescue = sim.Pending != null;
                    ContractResult r = Bot.Play(sim, c, rescue, bot);
                    if (r == null) { ok = false; Debug.Log("AUTOPLAY #" + (i + 1) + " " + Sim.Contracts[c].Code + " nao comecou"); break; }
                    if (r.Delivered) delivered++;
                    time += r.Time;
                    Debug.Log(string.Format(Inv, "AUTOPLAY #{0} {1} {2} t={3:0.0}s pago={4} baus={5} resgate={6} total={7} moedas={8} rep={9} estrelas={10}/{11} vida={12}/{13} niveis={14}{15}",
                        i + 1, Sim.Contracts[c].Code, r.Outcome, r.Time, r.Payment, r.ChestCoins, r.RescueBonus, r.Total, sim.Coins, sim.Reputation,
                        r.Stars, sim.Stars, sim.Run.Courier.Hp, sim.Run.Courier.MaxHp, string.Join("/", sim.Levels),
                        rescue ? (r.RescueDelivered ? " resgate=entregue" : " resgate=oferecido") : ""));
                }
                Sim back = Sim.Load(sim.Save());
                bool round = back.Coins == sim.Coins && back.Reputation == sim.Reputation && back.Stars == sim.Stars && (back.Pending == null) == (sim.Pending == null);
                for (int u = 0; u < sim.Levels.Length; u++) round &= back.Levels[u] == sim.Levels[u];
                if (!round) Debug.Log("AUTOPLAY save ida-e-volta diferente");
                ok &= round && sim.Reputation >= 1 && delivered * 2 >= n;
            }
            catch (Exception ex) { ok = false; Debug.LogError("AUTOPLAY excecao: " + ex); }
            Debug.Log(string.Format(Inv, "AUTOPLAY {0} contratos={1} entregues={2} rep={3}/{4} estrelas={5} moedas={6} tempo_jogo={7:0}s",
                ok ? "OK" : "FAIL", n, delivered, sim.Reputation, Sim.Contracts.Length, sim.Stars, sim.Coins, time));
            Application.Quit(ok ? 0 : 1);
        }

        IEnumerator Shot(string path)
        {
            yield return new WaitForSecondsRealtime(ArgF("-shotdelay", 1f));
            Run r = Sim.Run;
            bool inRun = r != null && _mode != Mode.Board;
            Debug.Log(string.Format(Inv, "SHOT {0} tela={1} contrato={2} sala={3} t={4:0.0} vida={5} moedas={6} rep={7} {8}x{9}", path, _hud.Current,
                inRun ? r.Contract.Code : "-", inRun ? r.Room.Code : "-", inRun ? r.Time : 0f, inRun ? r.Courier.Hp : 0, Sim.Coins, Sim.Reputation, Screen.width, Screen.height));
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
            yield return null;
            Application.Quit(0);
        }

        static float ArgF(string name, float def) => float.TryParse(Arg(name), NumberStyles.Float, Inv, out float v) ? v : def;

        /// <summary>Valor da flag (Environment.GetCommandLineArgs): null = ausente; "" = presente sem valor.</summary>
        static string Arg(string name)
        {
            string[] a = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(a, name);
            if (i < 0) return null;
            return i + 1 < a.Length && !a[i + 1].StartsWith("-", StringComparison.Ordinal) ? a[i + 1] : "";
        }
    }
}
