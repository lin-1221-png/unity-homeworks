using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 行为：每 3 秒跳到玩家头顶砸地一次，每 5 秒朝玩家冲刺一次。
/// 挂在 Boss 身上，需要它自带 Rigidbody2D；血量交给 CharacterStats。
/// 两个技能不会同时进行，一个放完才轮下一个。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("目标")]
    [Tooltip("留空会自动找场景里叫 player 的物体")]
    [SerializeField] private Transform player;

    [Header("砸地（默认每 3 秒）")]
    [Tooltip("多久砸一次地")]
    [SerializeField, Min(0.5f)] private float slamInterval = 3f;

    [Tooltip("飞到玩家头顶多高")]
    [SerializeField, Min(0.5f)] private float hoverHeight = 4f;

    [Tooltip("上升速度")]
    [SerializeField, Min(1f)] private float riseSpeed = 14f;

    [Tooltip("砸下来的速度")]
    [SerializeField, Min(1f)] private float slamSpeed = 35f;

    [Tooltip("落地时对范围内的角色造成的伤害")]
    [SerializeField, Min(0)] private int slamDamage = 15;

    [Tooltip("落地冲击的判定半径")]
    [SerializeField, Min(0.1f)] private float slamRadius = 1.8f;

    [Tooltip("到头顶后停顿多久再砸（给玩家反应时间）")]
    [SerializeField, Min(0f)] private float hoverPause = 0.15f;

    [Header("冲刺（默认每 5 秒）")]
    [Tooltip("多久冲刺一次")]
    [SerializeField, Min(0.5f)] private float dashInterval = 5f;

    [SerializeField, Min(1f)] private float dashSpeed = 28f;

    [Tooltip("冲刺持续多久")]
    [SerializeField, Min(0.05f)] private float dashDuration = 0.35f;

    [Tooltip("冲刺撞到人造成的伤害")]
    [SerializeField, Min(0)] private int dashDamage = 10;

    [Tooltip("冲刺撞人的判定半径")]
    [SerializeField, Min(0.1f)] private float dashHitRadius = 1f;

    [Header("通用")]
    [Tooltip("这些层上的角色会被打到。默认全部；想只打玩家就单独建一层再勾")]
    [SerializeField] private LayerMask damageLayers = ~0;

    private Rigidbody2D rb;
    private CharacterStats selfStats;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfStats = GetComponentInParent<CharacterStats>();
    }

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

        if (player == null)
        {
            Debug.LogWarning("BossController：没有找到玩家，Boss 不会行动", this);
            return;
        }

        StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
    {
        float nextSlam = Time.time + slamInterval;
        float nextDash = Time.time + dashInterval;

        while (true)
        {
            if (player == null)
            {
                yield return null;
                continue;
            }

            // 先到点先放；一次只放一个
            if (Time.time >= nextSlam)
            {
                nextSlam = Time.time + slamInterval;
                yield return SlamRoutine();
            }
            else if (Time.time >= nextDash)
            {
                nextDash = Time.time + dashInterval;
                yield return DashRoutine();
            }

            yield return null;
        }
    }

    /// <summary>飞到玩家头顶 → 停一下 → 砸下来 → 落地范围伤害</summary>
    private IEnumerator SlamRoutine()
    {
        // 1、朝玩家头顶飞，飞的途中还跟着玩家横向移动
        rb.gravityScale = 0f;
        float timer = 0f;
        while (timer < 2.5f)
        {
            if (player == null)
            {
                break;
            }

            Vector2 target = (Vector2)player.position + Vector2.up * hoverHeight;
            Vector2 delta = target - rb.position;

            if (delta.magnitude < 0.15f)
            {
                break;
            }

            rb.linearVelocity = delta.normalized * riseSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        // 2、空中停顿
        if (hoverPause > 0f)
        {
            yield return new WaitForSeconds(hoverPause);
        }

        // 3、砸下来：一直落到速度被地面挡停为止
        rb.gravityScale = 1f;
        rb.linearVelocity = new Vector2(0f, -slamSpeed);

        timer = 0f;
        while (timer < 3f && Mathf.Abs(rb.linearVelocity.y) > 1f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        // 4、落地冲击
        DamageAt(rb.position, slamRadius, slamDamage, "砸地");
        yield return new WaitForSeconds(0.35f);
    }

    /// <summary>朝玩家的方向冲刺，撞到就造成伤害</summary>
    private IEnumerator DashRoutine()
    {
        float dir = Mathf.Sign(player.position.x - rb.position.x);
        if (Mathf.Approximately(dir, 0f))
        {
            dir = 1f;
        }

        bool hitOnce = false;
        float timer = 0f;

        while (timer < dashDuration)
        {
            rb.linearVelocity = new Vector2(dir * dashSpeed, rb.linearVelocity.y);
            timer += Time.deltaTime;

            if (!hitOnce && player != null &&
                Vector2.Distance(rb.position, player.position) <= dashHitRadius)
            {
                hitOnce = true;
                DamageAt(rb.position, dashHitRadius, dashDamage, "冲刺");
            }

            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>对范围内的所有角色造成伤害（不打自己）</summary>
    private void DamageAt(Vector2 center, float radius, int damage, string skill)
    {
        if (damage <= 0)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, damageLayers);
        bool any = false;

        foreach (Collider2D hit in hits)
        {
            CharacterStats target = hit.GetComponentInParent<CharacterStats>();

            if (target == null || target == selfStats || target.IsDead)
            {
                continue;
            }

            target.TakeDamage(damage, gameObject);
            any = true;
        }

        if (any)
        {
            Debug.Log($"Boss {skill}命中，造成 {damage} 点伤害", this);
        }
    }

    private void OnDisable()
    {
        // 被禁用/打死时别把重力改坏了
        if (rb != null)
        {
            rb.gravityScale = 1f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}
