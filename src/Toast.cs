using UnityEngine;

namespace SovereignBoons
{
    /// <summary>
    /// Minimal on-screen toast — a centred banner near the top that fades after a
    /// few seconds. Self-contained IMGUI (no Keep Clarity dependency). Drawn from
    /// Plugin.OnGUI; costs nothing when no toast is active (early-return), and only
    /// a single textured label while one is showing.
    /// </summary>
    internal static class Toast
    {
        private static string _msg = "";
        private static float _until;
        private static GUIStyle? _style;
        private static Texture2D? _bg;

        /// <summary>Show a toast for <paramref name="seconds"/> (default 3).</summary>
        public static void Show(string msg, float seconds = 3f)
        {
            _msg = msg ?? "";
            _until = Time.realtimeSinceStartup + seconds;
        }

        public static void Render()
        {
            if (string.IsNullOrEmpty(_msg)) return;
            if (Time.realtimeSinceStartup > _until) { _msg = ""; return; }

            if (_style == null)
            {
                _style = new GUIStyle
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false,
                };
                _style.normal.textColor = Color.white;
            }
            if (_bg == null)
            {
                _bg = new Texture2D(1, 1);
                _bg.SetPixel(0, 0, new Color(0.10f, 0.08f, 0.14f, 0.82f)); // dark royal-purple, matches SB accent
                _bg.Apply();
            }

            const float w = 560f, h = 46f;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height * 0.10f;
            var rect = new Rect(x, y, w, h);

            // Subtle fade in the last 0.6s.
            float remaining = _until - Time.realtimeSinceStartup;
            float alpha = remaining < 0.6f ? Mathf.Clamp01(remaining / 0.6f) : 1f;
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, _bg);
            GUI.Label(rect, _msg, _style);
            GUI.color = prev;
        }
    }
}
