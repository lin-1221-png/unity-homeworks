using UnityEngine;

/// <summary>
/// 进入游戏时在屏幕上方显示一版操作提示，几秒后自动淡出，按任意键也能提前关掉。
/// 挂在任意常驻物体上即可（Main Camera 最省事），不需要 Canvas 和美术素材。
/// 提示内容可以直接在 Inspector 里改。
/// </summary>
public class ControlHint : MonoBehaviour
{
    [Header("内容")]
    [SerializeField] private string title = "操作提示";

    [SerializeField]
    private string[] lines =
    {
        "A / D  或  ← →      左右移动",
        "空格                 跳跃",
        "Shift                冲刺",
        "鼠标左键             普通攻击",
        "Shift + 左键         冲刺攻击",
        "鼠标右键             子弹时间（5 秒，冷却 10 秒）",
        "E                    靠近存档点存档",
        "Esc                  结束游戏",
    };

    [Header("显示")]
    [Tooltip("显示多久后自动淡出（真实秒）")]
    [SerializeField, Min(1f)] private float showDuration = 8f;

    [Tooltip("淡出用多久")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.8f;

    [Tooltip("按任意键/鼠标键立刻关掉")]
    [SerializeField] private bool dismissOnAnyKey = true;

    [Tooltip("按任意键的最短保护时间，防止开局随手一按就没了")]
    [SerializeField, Min(0f)] private float minShowTime = 0.6f;

    [Tooltip("显示半透明黑底，文字更清楚")]
    [SerializeField] private bool showBackground = true;

    [Header("位置")]
    [Tooltip("距离屏幕顶部的像素")]
    [SerializeField] private float topMargin = 48f;

    [Tooltip("面板宽度（会被屏幕宽度限制）")]
    [SerializeField] private float panelWidth = 560f;

    private float startTime;
    private float fadeStartTime = -1f;
    private bool fading;
    private GUIStyle titleStyle;
    private GUIStyle lineStyle;

    private void Start()
    {
        startTime = Time.unscaledTime;
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        // 按任意键提前关掉
        if (!fading && dismissOnAnyKey && now - startTime >= minShowTime && Input.anyKeyDown)
        {
            BeginFade(now);
        }

        // 到时间自动淡出
        if (!fading && now - startTime >= showDuration)
        {
            BeginFade(now);
        }

        // 淡出结束就关掉自己，不再占 OnGUI
        if (fading && now - fadeStartTime >= fadeDuration)
        {
            enabled = false;
        }
    }

    private void BeginFade(float now)
    {
        fading = true;
        fadeStartTime = now;
    }

    /// <summary>当前透明度：1 = 全显示，0 = 完全透明</summary>
    private float CurrentAlpha()
    {
        if (!fading)
        {
            return 1f;
        }

        float t = (Time.unscaledTime - fadeStartTime) / fadeDuration;
        return Mathf.Clamp01(1f - t);
    }

    private void OnGUI()
    {
        float alpha = CurrentAlpha();
        if (alpha <= 0f)
        {
            return;
        }

        EnsureStyles();

        float width = Mathf.Min(panelWidth, Screen.width - 40f);
        float lineHeight = 26f;
        float padding = 18f;
        float height = padding * 2f + 40f + lines.Length * lineHeight;

        Rect panel = new Rect((Screen.width - width) * 0.5f, topMargin, width, height);

        Color old = GUI.color;

        if (showBackground)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = old;
        }

        GUI.color = new Color(1f, 1f, 1f, alpha);

        float y = panel.y + padding;
        GUI.Label(new Rect(panel.x + padding, y, width - padding * 2f, 34f), title, titleStyle);
        y += 40f;

        for (int i = 0; i < lines.Length; i++)
        {
            GUI.Label(new Rect(panel.x + padding, y, width - padding * 2f, lineHeight), lines[i], lineStyle);
            y += lineHeight;
        }

        GUI.color = old;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        // 用系统字体，保证中文正常显示
        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 20);

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
        };

        lineStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft,
        };
    }
}
