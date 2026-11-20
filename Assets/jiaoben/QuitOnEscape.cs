using UnityEngine;

/// <summary>
/// 按一下 Esc 直接结束游戏。
/// 打包后是退出程序；在编辑器里按 Esc 会停止播放（方便测试）。
/// 挂在场景里任意一个常驻物体上即可。
/// </summary>
public class QuitOnEscape : MonoBehaviour
{
    [Tooltip("按哪个键退出，默认 Esc")]
    [SerializeField] private KeyCode quitKey = KeyCode.Escape;

    private void Update()
    {
        if (Input.GetKeyDown(quitKey))
        {
            Quit();
        }
    }

    /// <summary>也可以从按钮的 OnClick 里调用</summary>
    public void Quit()
    {
        Debug.Log("退出游戏");

#if UNITY_EDITOR
        // 编辑器里 Application.Quit() 没效果，改成停止播放
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
