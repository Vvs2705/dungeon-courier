using UnityEngine;

namespace DC
{
    /// <summary>Area segura (notch, camera, barra de gestos), copiado do Forge Street / ARKANA. Origem Unity: canto inferior esquerdo.</summary>
    public static class AreaSegura
    {
        /// <summary>Ancora um no de tela cheia (anchors 0..1) para dentro de Screen.safeArea.</summary>
        public static void AncorarDentro(RectTransform rt)
        {
            Rect s = Screen.safeArea;
            float w = Mathf.Max(1f, Screen.width), h = Mathf.Max(1f, Screen.height);
            rt.anchorMin = new Vector2(Mathf.Max(s.xMin, 0f) / w, Mathf.Max(s.yMin, 0f) / h);
            rt.anchorMax = new Vector2(Mathf.Min(s.xMax, w) / w, Mathf.Min(s.yMax, h) / h);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
