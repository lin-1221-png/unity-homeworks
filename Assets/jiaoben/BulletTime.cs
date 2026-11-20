using UnityEngine;

/// <summary>
/// 鼠标右键：子弹时间。按下后全场变慢，持续 5 秒，结束后进入 10 秒冷却。
/// 所有计时都用 Time.unscaledTime（真实时间），否则时间一慢，自己的计时也跟着变慢。
/// </summary>
public class BulletTime : MonoBehaviour
{
    [Header("触发")]
    [Tooltip("0 = 鼠标左键，1 = 鼠标右键，2 = 中键")]
    [SerializeField] private int mouseButton = 1;

    [Header("时间设置")]
    [Tooltip("子弹时间里的时间流速。0.3 = 三成速度，0.05 最慢；不能设 0（会完全停住）")]
    [SerializeField, Range(0.05f, 1f)] private float slowTimeScale = 0.3f;

    [Tooltip("效果持续多久（真实秒）")]
    [SerializeField, Min(0.1f)] private float duration = 5f;

    [Tooltip("冷却多久（真实秒）")]
    [SerializeField, Min(0f)] private float cooldown = 10f;

    [Tooltip("勾上：冷却从按下那一刻算起（总周期 10 秒）；不勾：冷却从效果结束算起（总周期 15 秒）")]
    [SerializeField] private bool cooldownFromActivation = false;

    /// <summary>子弹时间是否正在生效</summary>
    public bool IsActive { get; private set; }

    /// <summary>一次子弹时间持续多久（秒）</summary>
    public float Duration => duration;

    /// <summary>冷却多久（秒）</summary>
    public float Cooldown => cooldown;

    /// <summary>还剩多少冷却（秒）</summary>
    public float CooldownRemaining => Mathf.Max(0f, readyTime - Time.unscaledTime);

    /// <summary>本次子弹时间还剩多少（秒）</summary>
    public float DurationRemaining => IsActive ? Mathf.Max(0f, slowEndTime - Time.unscaledTime) : 0f;

    private float slowEndTime;
    private float readyTime;
    private float savedTimeScale = 1f;
    private float savedFixedDeltaTime = 0.02f;

    private void Update()
    {
        float now = Time.unscaledTime;

        // 右键且不在冷却中
        if (!IsActive && Input.GetMouseButtonDown(mouseButton) && now >= readyTime)
        {
            BeginSlowMotion(now);
        }

        // 到时间了自己结束（用 unscaledTime 判断，不受减速影响）
        if (IsActive && now >= slowEndTime)
        {
            EndSlowMotion(now, true);
        }
    }

    private void BeginSlowMotion(float now)
    {
        savedTimeScale = Time.timeScale;
        savedFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = slowTimeScale;
        // 物理步长一起缩放，否则减速后刚体会变得"一顿一顿"甚至穿透
        Time.fixedDeltaTime = savedFixedDeltaTime * slowTimeScale;

        IsActive = true;
        slowEndTime = now + duration;

        if (cooldownFromActivation)
        {
            readyTime = now + cooldown;
        }

        Debug.Log($"子弹时间开始：{slowTimeScale} 倍速，持续 {duration} 秒");
    }

    private void EndSlowMotion(float now, bool startCooldown)
    {
        Time.timeScale = savedTimeScale;
        Time.fixedDeltaTime = savedFixedDeltaTime;

        IsActive = false;

        // 冷却从效果结束开始算
        if (startCooldown && !cooldownFromActivation)
        {
            readyTime = now + cooldown;
        }

        Debug.Log(startCooldown ? $"子弹时间结束，冷却 {cooldown} 秒" : "子弹时间结束");
    }

    // 脚本被禁用/物体被销毁时，务必把时间恢复正常，
    // 否则会一直卡在慢动作里（整个编辑器都变慢）
    private void OnDisable()
    {
        if (IsActive)
        {
            EndSlowMotion(Time.unscaledTime, false);
        }
    }

    private void OnDestroy()
    {
        if (IsActive)
        {
            Time.timeScale = savedTimeScale;
            Time.fixedDeltaTime = savedFixedDeltaTime;
            IsActive = false;
        }
    }
}
