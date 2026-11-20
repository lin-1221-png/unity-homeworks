using UnityEngine;

/// <summary>
/// 存档点：把这段脚本挂到场景里任意一个物体上（比如一个像篝火/石碑的物体），
/// 玩家走进圆圈范围内，按 E 就会存档。
/// 判定用的是距离，不需要给这个物体加 Collider，摆好位置、调一下半径就行。
/// </summary>
public class SavePoint : MonoBehaviour
{
    [Header("范围")]
    [Tooltip("玩家离多近算「靠近」。选中这个物体时 Scene 里能看到绿圈")]
    [SerializeField, Min(0.1f)] private float saveRadius = 1.5f;

    [Tooltip("按哪个键存档，默认 E")]
    [SerializeField] private KeyCode saveKey = KeyCode.E;

    [Tooltip("存到第几个存档位。多个存档点想分开存就填不同数字")]
    [SerializeField, Min(0)] private int slot = 0;

    [Header("玩家")]
    [Tooltip("留空会自动找场景里叫 player 的物体")]
    [SerializeField] private Transform player;

    [Header("提示")]
    [Tooltip("进入/离开范围时在 Console 打印提示（以后有 UI 可以换成显示文字）")]
    [SerializeField] private bool logHint = true;

    private bool playerInRange;

    private void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.Find("player");
            if (found != null)
            {
                player = found.transform;
            }
        }
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        bool near = Vector2.Distance(player.position, transform.position) <= saveRadius;

        // 进出范围时提示一次，不用每帧刷屏
        if (near != playerInRange)
        {
            playerInRange = near;

            if (logHint)
            {
                Debug.Log(near ? "靠近存档点，按 E 存档" : "离开了存档点");
            }
        }

        if (playerInRange && Input.GetKeyDown(saveKey))
        {
            DoSave();
        }
    }

    /// <summary>存档。也可以绑到 UI 按钮上</summary>
    public void DoSave()
    {
        if (SaveSystem.SaveGame(player, slot))
        {
            if (logHint)
            {
                Debug.Log("存档成功");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, saveRadius);
    }
}
