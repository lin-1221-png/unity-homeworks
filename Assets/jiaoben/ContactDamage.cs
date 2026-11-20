using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在敌人身上：玩家碰到它就会被扣血。
/// 需要这个物体自己有 Collider2D（BoxCollider2D / CircleCollider2D 都行），
/// 并且被碰到的角色身上要有 CharacterStats。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ContactDamage : MonoBehaviour
{
    [Header("伤害")]
    [Tooltip("碰到一次扣多少血")]
    [SerializeField, Min(1)] private int damage = 1;

    [Tooltip("隔多久扣一次。贴着站的时候每 1 秒掉一次；0.5 就是每半秒")]
    [SerializeField, Min(0.05f)] private float damageInterval = 1f;

    [Tooltip("只会伤到这些层的角色。默认全部；以后敌人多了，把这里改成只勾玩家所在的层")]
    [SerializeField] private LayerMask victimLayers = ~0;

    [Tooltip("击退力度，0 = 不击退。想让玩家被弹开可以填 8~15")]
    [SerializeField, Min(0f)] private float knockbackForce = 0f;

    // 每个受害者下一次可以被扣血的时间，避免贴着敌人时每帧都掉血
    private readonly Dictionary<CharacterStats, float> nextHitTime = new Dictionary<CharacterStats, float>();

    private CharacterStats self;

    private void Awake()
    {
        self = GetComponentInParent<CharacterStats>();
    }

    // 实心碰撞和触发器两种都接上，不管谁的 Collider 勾了 Is Trigger 都能用
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHit(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryHit(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        // 层过滤
        if ((victimLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        CharacterStats victim = other.GetComponentInParent<CharacterStats>();

        // 没有血量组件、或者碰到的就是自己，跳过
        if (victim == null || victim == self || victim.IsDead)
        {
            return;
        }

        // 冷却没到
        if (nextHitTime.TryGetValue(victim, out float next) && Time.time < next)
        {
            return;
        }

        nextHitTime[victim] = Time.time + damageInterval;
        victim.TakeDamage(damage, gameObject);

        if (knockbackForce > 0f)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body != null)
            {
                Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position);
                dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
                body.linearVelocity = new Vector2(dir.x * knockbackForce, body.linearVelocity.y);
            }
        }
    }
}
