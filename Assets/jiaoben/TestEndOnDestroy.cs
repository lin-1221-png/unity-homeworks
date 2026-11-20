using UnityEngine;

/// <summary>
/// 挂到任意物体上：这个物体一旦被摧毁，就弹出"测试到此结束"，存一次档，10 秒后退出游戏。
/// （一般挂在 Boss 或者通关目标上）
/// </summary>
public class TestEndOnDestroy : MonoBehaviour
{
    [Header("结束提示")]
    [SerializeField] private string message = "测试到此结束";

    [Tooltip("弹出提示后多久退出游戏（秒）")]
    [SerializeField, Min(0f)] private float quitDelay = 10f;

    [Header("收尾动作")]
    [Tooltip("退出前自动存一次档")]
    [SerializeField] private bool saveBeforeQuit = true;

    [Tooltip("弹出提示时把游戏暂停住（时间流速归零），免得这 10 秒里还在被打")]
    [SerializeField] private bool freezeGame = true;

    // 整个游戏只触发一次，防止多个物体同时被摧毁时弹出好几个
    private static bool ended;
    private bool quitting;

    private void OnApplicationQuit()
    {
        quitting = true;
    }

    private void OnDestroy()
    {
        // 退出游戏 / 退出播放模式时也会触发 OnDestroy，这些情况不算"被摧毁"
        if (ended || quitting || !Application.isPlaying)
        {
            return;
        }

        ended = true;

        // 自分马上要被销毁了，所以另起一个不会被销毁的物体来跑倒计时
        GameObject host = new GameObject("TestEnd");
        DontDestroyOnLoad(host);

        TestEndRunner runner = host.AddComponent<TestEndRunner>();
        runner.Begin(message, quitDelay, saveBeforeQuit, freezeGame);
    }
}

/// <summary>显示"测试到此结束"并在倒计时结束后退出。由 TestEndOnDestroy 自动创建，不用手动挂。</summary>
public class TestEndRunner : MonoBehaviour
{
    private const string PlayerName = "player";

    private string message = "测试到此结束";
    private float quitAt;
    private float savedTimeScale = 1f;
    private GUIStyle titleStyle;
    private GUIStyle subStyle;
    private bool quit;

    public void Begin(string msg, float delay, bool save, bool freeze)
    {
        message = msg;
        quitAt = Time.unscaledTime + Mathf.Max(0f, delay);

        // 先存档
        if (save)
        {
            Transform player = FindPlayer();
            if (player != null)
            {
                SaveSystem.SaveGame(player, 0);
            }
            else
            {
                Debug.LogWarning("TestEnd：场景里没找到 player，跳过存档");
            }
        }

        // 暂停住游戏（倒计时本身用的是真实时间，不受影响）
        if (freeze)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        Debug.Log($"{message}，{Mathf.RoundToInt(delay)} 秒后退出");
    }

    private static Transform FindPlayer()
    {
        GameObject found = GameObject.Find(PlayerName);
        return found != null ? found.transform : null;
    }

    private void Update()
    {
        if (!quit && Time.unscaledTime >= quitAt)
        {
            QuitNow();
        }
    }

    private void QuitNow()
    {
        quit = true;
        RestoreTimeScale();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = savedTimeScale <= 0f ? 1f : savedTimeScale;
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        // 半透明黑底，盖住整个屏幕
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;

        float width = Mathf.Min(560f, Screen.width - 40f);
        float height = 150f;
        Rect box = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        GUI.Box(box, GUIContent.none);
        GUI.Label(new Rect(box.x, box.y + 26f, box.width, 50f), message, titleStyle);

        int remain = Mathf.Max(0, Mathf.CeilToInt(quitAt - Time.unscaledTime));
        GUI.Label(new Rect(box.x, box.y + 82f, box.width, 34f), $"{remain} 秒后退出", subStyle);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        // 用系统字体，保证中文能显示出来（内置字体不一定有中文）
        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 28);

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        subStyle = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
        };
    }
}
