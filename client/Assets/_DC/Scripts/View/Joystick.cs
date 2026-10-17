using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DC
{
    /// <summary>
    /// Logica PURA do joystick (JoystickLogica do ARKANA, via Forge Street): deslocamento do dedo em px a partir da ancora e raio util ->
    /// direcao 0..1 com zona morta REESCALADA (cruzou a zona morta, sai de 0, sem degrau).
    /// </summary>
    public sealed class JoystickLogica
    {
        public float Deadzone, Curva;
        public JoystickLogica(float deadzone, float curva) { Deadzone = deadzone; Curva = curva; }

        public Vector2 Direcao(Vector2 deslocPx, float raioPx)
        {
            if (raioPx <= 0f) return Vector2.zero;
            Vector2 v = deslocPx / raioPx;
            float len = v.magnitude;
            if (len > 1f) { v /= len; len = 1f; }
            if (len < Deadzone || len <= 0f) return Vector2.zero;
            float util = (len - Deadzone) / (1f - Deadzone);
            return v / len * Mathf.Pow(Mathf.Clamp01(util), Curva <= 0f ? 1f : Curva);
        }
    }

    /// <summary>
    /// Gesto de UM dedo (GDD §2, CONTRATO §2): arrastar = direcao pelo deslocamento desde o ponto onde o dedo tocou (joystick flutuante
    /// visivel so depois de passar da zona morta); toque rapido (&lt; 150 ms sem passar da zona morta) = Tap naquele quadro (dash); dedo
    /// solto = zero (o Core conta os 200 ms para pousar). Le Pointer.current (mouse no PC, toque no Android), sem EventSystem, porque o
    /// alvo e a tela inteira menos a faixa da HUD de cima. Vive num Canvas de escala 1 (px = px) para usar dp.
    /// </summary>
    public sealed class Joystick
    {
        public const float TapMax = 0.15f, DeadDp = 10f, RadiusDp = 56f;

        public Vector2 Direction { get; private set; }
        /// <summary>Toque rapido terminou neste quadro.</summary>
        public bool Tap { get; private set; }
        /// <summary>Tela modal ou fora do contrato: nada de joystick.</summary>
        public bool Blocked;
        /// <summary>Base da faixa da HUD (px de tela): toque que COMECA acima disto nao e do joystick.</summary>
        public float TopPx = float.MaxValue;
        /// <summary>Desenha o joystick (base + miolo) no dedo. Desligado = o mesmo controle, sem nada na tela (padrao: o desenho tampava o
        /// courier; Ajustes.Joystick, nas Configuracoes).</summary>
        public bool Visivel;

        readonly JoystickLogica _logic = new JoystickLogica(DeadDp / RadiusDp, 1f);
        readonly RectTransform _base, _knob;
        Vector2 _anchor;
        float _t0;
        bool _active, _drag;

        public Joystick(Transform canvasEscala1)
        {
            float side = Dp.Px(RadiusDp * 2.4f), knob = Dp.Px(46f);
            _base = Art.Node(canvasEscala1, "Joystick", Vector2.zero, Vector2.zero);
            _base.sizeDelta = new Vector2(side, side);
            Image fundo = _base.gameObject.AddComponent<Image>();
            fundo.sprite = Art.Disc(); fundo.color = new Color(0.03f, 0.04f, 0.08f, 0.35f); fundo.raycastTarget = false;
            Image anel = Art.Node(_base, "Anel", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            anel.sprite = Art.Ring(); anel.color = new Color(0.82f, 0.88f, 1f, 0.45f); anel.raycastTarget = false;
            _knob = Art.Node(_base, "Miolo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _knob.sizeDelta = new Vector2(knob, knob);
            Image miolo = _knob.gameObject.AddComponent<Image>();
            miolo.sprite = Art.Disc(); miolo.color = new Color(0.9f, 0.93f, 1f, 0.9f); miolo.raycastTarget = false;
            _base.gameObject.SetActive(false);
        }

        public void Tick()
        {
            Tap = false;
            Pointer p = Pointer.current;
            if (p == null || Blocked) { Release(); return; }
            Vector2 pos = p.position.ReadValue();
            if (!_active)
            {
                if (!p.press.wasPressedThisFrame || pos.y > TopPx) return;
                _active = true; _drag = false; _anchor = pos; _t0 = Time.unscaledTime;
                _base.anchoredPosition = pos;
            }
            if (!p.press.isPressed)
            {
                Tap = !_drag && Time.unscaledTime - _t0 < TapMax;
                Release();
                return;
            }
            Vector2 d = pos - _anchor;
            float r = Dp.Px(RadiusDp);
            if (!_drag && d.magnitude > Dp.Px(DeadDp)) { _drag = true; _base.gameObject.SetActive(Visivel); }
            Direction = _drag ? _logic.Direcao(d, r) : Vector2.zero;
            _knob.anchoredPosition = Vector2.ClampMagnitude(d, r);
        }

        void Release()
        {
            _active = _drag = false;
            Direction = Vector2.zero;
            _knob.anchoredPosition = Vector2.zero;
            if (_base.gameObject.activeSelf) _base.gameObject.SetActive(false);
        }
    }
}
