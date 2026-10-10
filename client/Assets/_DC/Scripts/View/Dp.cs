using UnityEngine;

namespace DC
{
    /// <summary>
    /// dp -> px (copiado do Forge Street / ARKANA). Numero que o DEDO sente vive em dp: px = dp * dpi / 160. O Canvas do joystick
    /// usa escala 1 (px de tela = px de canvas), entao nao ha segunda conversao.
    /// </summary>
    public static class Dp
    {
        public const float DpiPadrao = 160f;

        public static float Dpi
        {
            get
            {
                float d = Screen.dpi;
                return d > 0f ? d : DpiPadrao;   // editor/headless devolve 0
            }
        }

        public static float Px(float dp) => dp * Dpi / DpiPadrao;

        /// <summary>Piso do alvo de toque.</summary>
        public const float AlvoMinimoDp = 48f;
    }
}
