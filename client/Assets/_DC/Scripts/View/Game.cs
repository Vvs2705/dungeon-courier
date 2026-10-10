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
    /// (Hud) e mundo (WorldView). App em segundo plano: grava e abre a pausa (sem Tick ate o jogador continuar).
    /// Flags de dev (CONTRATO §11):
    ///   -autoplay N   sem render: o Bot do Core joga N contratos em sequencia (compra o upgrade mais barato antes de cada um, aceita o
    ///                 resgate), loga 1 linha por contrato e AUTOPLAY OK|FAIL; sai 0/1. Usar com -batchmode -nographics.
    ///   -bot          o Bot dirige a partida com render (e avanca quadro -> resultado -> proximo contrato sozinho).
    ///   -speed N      multiplica o dt (0,1..20).
    ///   -shot f.png [-shotdelay s]   foto da tela depois de s segundos reais (padrao 1) e sai 0.
    ///   -contract K   K 1-based (C1..C8): libera ate K NA MEMORIA e comeca direto (aceita o resgate se houver). Use com -testsession.
    ///   -reset        apaga o save antes de carregar.   -testsession  nao le nem grava save nem diario (semente fixa 1).
    /// </summary>
    public sealed class Game : MonoBehaviour
    {
        const string SaveKey = "dc.save";
        const float EndDelay = 1.1f, MaxChunk = 0.2f;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public Sim Sim { get; private set; }
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

            _sid = Guid.NewGuid().ToString("N").Substring(0, 8);
            _diary = Path.Combine(Application.persistentDataPath, "diario.csv");
            _speed = Mathf.Clamp(ArgF("-speed", 1f), 0.1f, 20f);
            Sfx.Init(gameObject);
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
            Log("session_start", Application.version, SystemInfo.deviceModel.Replace(",", " "));
            if (Sim.SaveRefused) Log("save_refused");
            _botDrive = Arg("-bot") != null;
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
                    StartContract(k, Sim.Pending != null);
                }
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

            bool playing = _mode == Mode.Play && !_hud.PauseOpen && !_paused;
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
                if (_endT <= 0f) { _mode = Mode.After; _hud.ShowEnd(Sim.LastResult); }
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
                // o Bot joga um passo fixo por Act (Act ja chama sim.Tick)
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
                } while (dt > 0f && Sim.InContract);
            }
            if (!Sim.InContract) { _mode = Mode.Ending; _endT = EndDelay; }
        }

        // ------------------------------------------------------------------ acoes chamadas pela HUD

        /// <summary>Toque num contrato do quadro: com carga pendente abre a oferta de resgate; senao comeca.</summary>
        public void Play(int i) => _hud.ShowOffer(i);

        public void StartContract(int i, bool rescue)
        {
            if (Sim.StartContract(i, rescue) == null) { _hud.RefreshBoard(); return; }
            EnterPlay();
            HandleEvents();   // contract_start, cargo_rescue_offer, room_enter
            Save();
        }

        void EnterPlay() { _mode = Mode.Play; _botAcc = 0f; _hud.ShowPlay(); }

        public void Buy(Upgrade u)
        {
            if (!Sim.Buy(u)) return;
            HandleEvents();   // guild_upgrade, currency_sink
            Save();
            _hud.RefreshBoard();
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

        // ------------------------------------------------------------------ eventos do Core -> som, efeito, vibracao, diario

        void HandleEvents()
        {
            Run run = Sim.Run;
            foreach (SimEvent e in Sim.Events)
            {
                string name = SimEvent.DiaryName(e.Kind);
                if (name != null) Diary(name, e);
                _view.OnEvent(e, run);
                switch (e.Kind)
                {
                    case Ev.Plant: Sfx.Play("pousar"); break;
                    case Ev.CargoPickup: Sfx.Play("pegar"); break;
                    case Ev.CargoIntegrity: Sfx.Play("carga"); Ajustes.Vibrar(); break;
                    case Ev.ThiefGrab: Sfx.Play("ladrao"); break;
                    case Ev.ThiefSpawn: Sfx.Play("ladrao", 1.3f); break;
                    case Ev.ThiefEscape: Sfx.Play("ladrao", 0.7f); break;
                    case Ev.Dash: Sfx.Play("dash"); break;
                    case Ev.Shot: Sfx.Play("tiro", 1f, 0.08f); break;
                    case Ev.EnemyHit: Sfx.Play("golpe"); break;
                    case Ev.EnemyDie: Sfx.Play("morte"); break;
                    case Ev.DamageTaken: Sfx.Play("dano"); _hud.Hurt(); break;
                    case Ev.PetHit: Sfx.Play("dano", 1.4f); break;
                    case Ev.Telegraph: Sfx.Play("aviso", e.B == (int)EnemyType.Ogre ? 0.7f : 1f); break;
                    case Ev.Strike: Sfx.Play("golpe", 0.6f); break;
                    case Ev.RouteChoice: Sfx.Play("porta"); break;
                    case Ev.RoomEnter: _hud.FadeIn(); break;
                    case Ev.DoorBlocked: Sfx.Play("bloqueio", 1f, 0.3f); break;
                    case Ev.Deliver: Sfx.Play("entrega"); break;
                    case Ev.ChestOpen: Sfx.Play("moedas"); break;
                    case Ev.CurrencySource: if (e.B != 1) Sfx.Play("moedas", 1.2f, 0.2f); break;   // o bau ja tocou no ChestOpen
                    case Ev.Hatch: Sfx.Play("choca"); break;
                    case Ev.CargoBroken: Sfx.Play("quebra"); break;
                    case Ev.GuildUpgrade: Sfx.Play("compra"); break;
                    case Ev.ContractEnd:
                        ContractResult r = Sim.LastResult;
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

        /// <summary>Diario CSV de playtest: utc, sessao, s desde a abertura, s do contrato, evento, a, b, contrato, sala.</summary>
        public void Log(string evt, string a = "", string b = "", string contract = "", string room = "")
        {
            if (_test || _diary == null) return;
            try
            {
                if (!File.Exists(_diary)) File.AppendAllText(_diary, "utc,sessao,t_sessao,t_contrato,evento,a,b,contrato,sala\n");
                float t = Sim != null && Sim.Run != null && _mode != Mode.Board ? Sim.Run.Time : 0f;
                File.AppendAllText(_diary, string.Format(Inv, "{0:yyyy-MM-ddTHH:mm:ss},{1},{2:0.0},{3:0.0},{4},{5},{6},{7},{8}\n",
                    DateTime.UtcNow, _sid, Time.realtimeSinceStartup, t, evt, a, b, contract, room));
            }
            catch (Exception) { }   // ponytail: diario e best-effort; disco cheio nunca derruba o jogo
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
        void OnApplicationQuit() => Save();

        /// <summary>App saiu da frente: grava e abre a pausa (o contrato fica sem Tick ate CONTINUAR). Voltou: descarta o dt do quadro.</summary>
        void Away(bool away)
        {
            if (_headless || Sim == null || !_started) return;
            if (away)
            {
                Save();
                if (_mode == Mode.Play && !_botDrive) _hud.ShowPause(false);
            }
            else _skipDt = true;
            _paused = away;
        }

        /// <summary>Esc no PC / voltar do Android: fecha o que estiver aberto; no contrato abre a pausa; no quadro grava e sai.</summary>
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

        /// <summary>-bot: no quadro compra o mais barato e comeca o proximo contrato; no resultado volta ao quadro depois de 2,5 s.</summary>
        void BotFlow()
        {
            if (_mode == Mode.Board && _hud.TimeInScreen > 1f)
            {
                BuyCheapest(Sim, Buy);
                StartContract(Math.Min(Sim.Reputation, Sim.Contracts.Length - 1), Sim.Pending != null);
                if (_mode == Mode.Board) _botDrive = false;   // nao comecou (save recusado): o bot para
            }
            else if (_mode == Mode.After && _hud.Current == Hud.Tela.Result && _hud.TimeInScreen > 2.5f) ToBoard();
        }

        /// <summary>Compra o upgrade mais barato que couber, ate nao caber nenhum (empate: Vida, Dash, Bolsa), como o Bot.PlayCampaign.</summary>
        static void BuyCheapest(Sim s, Action<Upgrade> buy)
        {
            while (true)
            {
                int pick = -1, cost = int.MaxValue;
                for (int u = 0; u < 3; u++) { int k = s.UpgradeCost((Upgrade)u); if (s.CanBuy((Upgrade)u) && k < cost) { cost = k; pick = u; } }
                if (pick < 0) return;
                buy((Upgrade)pick);
            }
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
                    BuyCheapest(sim, u => sim.Buy(u));
                    int c = Math.Min(sim.Reputation, Sim.Contracts.Length - 1);
                    bool rescue = sim.Pending != null;
                    ContractResult r = Bot.Play(sim, c, rescue, bot);
                    if (r == null) { ok = false; Debug.Log("AUTOPLAY #" + (i + 1) + " " + Sim.Contracts[c].Code + " nao comecou"); break; }
                    if (r.Delivered) delivered++;
                    time += r.Time;
                    Debug.Log(string.Format(Inv, "AUTOPLAY #{0} {1} {2} t={3:0.0}s pago={4} baus={5} resgate={6} total={7} moedas={8} rep={9} vida={10}/{11} niveis={12}/{13}/{14}{15}",
                        i + 1, Sim.Contracts[c].Code, r.Outcome, r.Time, r.Payment, r.ChestCoins, r.RescueBonus, r.Total, sim.Coins, sim.Reputation,
                        sim.Run.Courier.Hp, sim.Run.Courier.MaxHp, sim.Levels[0], sim.Levels[1], sim.Levels[2],
                        rescue ? (r.RescueDelivered ? " resgate=entregue" : " resgate=oferecido") : ""));
                }
                Sim back = Sim.Load(sim.Save());
                bool round = back.Coins == sim.Coins && back.Reputation == sim.Reputation && back.Levels[0] == sim.Levels[0]
                    && back.Levels[1] == sim.Levels[1] && back.Levels[2] == sim.Levels[2] && (back.Pending == null) == (sim.Pending == null);
                if (!round) Debug.Log("AUTOPLAY save ida-e-volta diferente");
                ok &= round && sim.Reputation >= 1 && delivered * 2 >= n;
            }
            catch (Exception ex) { ok = false; Debug.LogError("AUTOPLAY excecao: " + ex); }
            Debug.Log(string.Format(Inv, "AUTOPLAY {0} contratos={1} entregues={2} rep={3}/{4} moedas={5} tempo_jogo={6:0}s",
                ok ? "OK" : "FAIL", n, delivered, sim.Reputation, Sim.Contracts.Length, sim.Coins, time));
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
