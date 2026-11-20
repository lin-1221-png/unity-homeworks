using UnityEngine;

/// <summary>
/// 2D 摄像机跟随：挂在 Main Camera 上，把要跟的物体（player）拖到 Target。
/// 用 LateUpdate 跟随，避免和物理/移动脚本抢同一帧，画面不会抖。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [Tooltip("拖 player 进来。留空会自动找场景里叫 player 的物体")]
    [SerializeField] private Transform target;

    [Tooltip("摄像机相对目标的位置。2D 游戏 z 一般保持 -10 左右，太小会看不到物体")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("跟随的柔和程度（秒）。0 = 硬跟，0.1~0.2 比较舒服")]
    [SerializeField, Min(0f)] private float smoothTime = 0.15f;

    [Header("移动范围（可选）")]
    [Tooltip("勾上之后摄像机不会跑出下面的范围，适合固定关卡")]
    [SerializeField] private bool useBounds = false;

    [SerializeField] private Vector2 minBounds = new Vector2(-20f, -5f);
    [SerializeField] private Vector2 maxBounds = new Vector2(20f, 20f);

    private Vector3 velocity;

    private void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("CameraFollow：没有设置 Target，也没找到叫 player 的物体", this);
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = target.position + offset;
        desired.z = offset.z;   // z 固定住，别跟着玩家跑

        Vector3 pos = smoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime)
            : desired;

        pos.z = offset.z;

        if (useBounds)
        {
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        }

        transform.position = pos;
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Vector3 min = new Vector3(minBounds.x, minBounds.y, 0f);
        Vector3 max = new Vector3(maxBounds.x, maxBounds.y, 0f);
        Gizmos.DrawWireCube((min + max) * 0.5f, max - min);
    }
}
