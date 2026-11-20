using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家死亡后自动回到存档点满血重生。
/// 挂在 player 上即可（需要 CharacterStats，会自动补；有 Rigidbody2D 更好）。
/// 有存档：回到存档时的位置，攻击力和金币也一起还原；
/// 没存档：回到进入游戏时的位置（或指定的重生点）。
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("重生")]
    [Tooltip("死亡后等多久重生（真实秒，不受子弹时间影响）")]
    [SerializeField, Min(0f)] private float respawnDelay = 1f;

    [Tooltip("没有任何存档时用的重生点。留空就用进入游戏时角色所在的位置")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("重生后给多久无敌，防止一出来就被 Boss 连着打死")]
    [SerializeField, Min(0f)] private float invincibleAfterRespawn = 1.5f;

    [Tooltip("用第几个存档位")]
    [SerializeField, Min(0)] private int slot = 0;

    private CharacterStats stats;
    private Rigidbody2D body;
    private Vector3 defaultSpawn;
    private bool respawning;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        body = GetComponent<Rigidbody2D>();

        // 玩家死了不能被销毁，否则没法重生
        stats.KeepOnDeath = true;

        defaultSpawn = transform.position;
    }

    private void Start()
    {
        stats.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnDied -= HandleDied;
        }
    }

    private void HandleDied(CharacterStats who)
    {
        if (respawning)
        {
            return;
        }

        respawning = true;
        Debug.Log("玩家死亡，准备重生", this);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSecondsRealtime(respawnDelay);

        if (SaveSystem.HasSave(slot))
        {
            // 回到存档点：位置、攻击力、金币一起还原
            SaveSystem.LoadGame(transform, slot);
        }
        else
        {
            transform.position = spawnPoint != null ? spawnPoint.position : defaultSpawn;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        // 满血重生：清掉死亡标记，并把死亡时被禁用的脚本（move、attack 等）重新打开
        stats.Revive();

        if (invincibleAfterRespawn > 0f)
        {
            stats.SetInvincible(invincibleAfterRespawn);
        }

        respawning = false;
        Debug.Log("已重生", this);
    }
}
