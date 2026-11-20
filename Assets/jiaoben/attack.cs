using System.Collections.Generic;
using UnityEngine;

public class attack : MonoBehaviour
{
    public move movescript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int attack1;

    // ↓ 这几个是纯运行时状态：不进 Inspector，也不会被存进场景。
    //   之前 isrushing 被场景存成了 1，导致刚进游戏第一下按 J 就被当成冲刺攻击。
    [System.NonSerialized] public int attacktime;
    [System.NonSerialized] public bool isattacking = false;
    [System.NonSerialized] public bool isrushing = false;
    [System.NonSerialized] public bool isrushattack = false;
    [SerializeField] private Animator anim;
    [SerializeField] private Rigidbody2D rb;
    public bool mo;

    [Header("伤害")]
    [Tooltip("自己的属性（血量/攻击力）。留空会自动在自身和父物体上找 CharacterStats")]
    [SerializeField] private CharacterStats stats;

    [Tooltip("这一刀能打到哪些层。默认全部；把敌人放到单独一层再勾选，就不会误伤自己人")]
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Tooltip("判定圆心相对自己的偏移（沿面朝方向，往右为正）")]
    [SerializeField] private float hitOffset = 1f;

    [Tooltip("判定半径。越大越容易打到人")]
    [SerializeField] private float hitRadius = 0.8f;

    [Tooltip("冲刺攻击的伤害倍率。2 = 两倍普通攻击伤害")]
    [SerializeField] private float dashDamageMultiplier = 2f;

    [Tooltip("没找到 CharacterStats 时用的攻击力，只用来兜底")]
    [SerializeField] private int fallbackAttackPower = 10;

    // 一次攻击里已经打过的目标，避免同一刀连续几帧反复扣血
    private readonly HashSet<CharacterStats> hitThisSwing = new HashSet<CharacterStats>();

    // 冲刺开始时间 + 最长持续时间，用来防止动画事件丢失导致人物卡在冲刺状态
    private float rushStartTime;
    private const float MaxRushDuration = 3f;

    public void Rushattack()
    {
        isrushing = true;
        isrushattack = true;
        float xinput = movescript.xinput;
        rb.linearVelocity = new Vector2(xinput * 40f, rb.linearVelocity.y);
        anim.SetBool("isrushattack", isrushattack);

        // 新的一次冲刺，清空命中记录；真正扣血在 Update 里每帧判定，
        // 这样冲刺途中撞到的敌人都能打到
        hitThisSwing.Clear();
        rushStartTime = Time.time;
    }

    public void Endrushattack()
    {
        isrushattack = false;
        isrushing = false;
        rb.linearVelocity = new Vector2(0f, 0f);
        anim.SetBool("isrushattack", isrushattack);

        Debug.Log("ok");
    }

    public void first()
    {
        isattacking = true;
        attacktime = 0;
        attacktime++;
        anim.SetTrigger("isattack");
        Debug.Log("attack");

        // 普通攻击：挥出去的瞬间判定一次
        hitThisSwing.Clear();
        ApplyHit();
    }

    private void Awake()
    {
        // 兜底：就算以前场景里存了脏数据，进游戏也强制回到干净状态
        isrushing = false;
        isrushattack = false;
        isattacking = false;
        attacktime = 0;
        hitThisSwing.Clear();
    }

    void Start()
    {
        if (stats == null)
        {
            stats = GetComponentInParent<CharacterStats>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 冲刺攻击期间持续判定，撞到谁扣谁的血（每个目标一次冲刺只扣一次）
        if (isrushattack)
        {
            // 万一动画事件没触发（动画被打断、切换丢失等），最多 3 秒自动收尾，防止人物卡住不能动
            if (Time.time - rushStartTime > MaxRushDuration)
            {
                Endrushattack();
            }
            else
            {
                ApplyDashHit();
                // 冲刺没结束前不接受新的攻击输入，避免冲刺途中又触发一次普通攻击
                return;
            }
        }

        // 每帧直接读键盘，而不是靠"按下/松开"两个事件自己记状态。
        // 这样切窗口丢焦点、松开事件没收到，也不会让 isrushing 卡在 true。
        isrushing = Input.GetKey(KeyCode.LeftShift);

        // 鼠标左键 = 攻击（原来的是 J 键）
        bool attackPressed = Input.GetMouseButtonDown(0);

        if (attackPressed && isrushing == true)
        {
            Rushattack();
            Debug.Log("isrushattach");
        }
        else if (attackPressed && isrushing == false)
        {
            first();
            isattacking = true;
        }
    }

    public void FinishAttack()
    {
        isattacking = false;
    }

    /// <summary>普通攻击命中判定。也可以挂到动画事件上，在真正的挥刀帧调用</summary>
    public void ApplyHit()
    {
        DealDamage(1f);
    }

    /// <summary>冲刺攻击命中判定（倍率取 dashDamageMultiplier）</summary>
    public void ApplyDashHit()
    {
        DealDamage(dashDamageMultiplier);
    }

    /// <summary>在面朝方向做一次圆形判定，打到的目标按攻击力扣血</summary>
    private void DealDamage(float multiplier)
    {
        if (stats == null)
        {
            stats = GetComponentInParent<CharacterStats>();
        }

        int power = stats != null ? stats.AttackPower : fallbackAttackPower;
        int damage = Mathf.RoundToInt(power * Mathf.Max(0f, multiplier));
        if (damage <= 0)
        {
            return;
        }

        // transform.right 会跟着 move.cs 那个 180° 翻转一起变，所以自动朝面向的方向打
        Vector2 center = (Vector2)transform.position + (Vector2)(transform.right * hitOffset);

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius, enemyLayers);
        foreach (Collider2D hit in hits)
        {
            CharacterStats target = hit.GetComponentInParent<CharacterStats>();

            // 没有属性组件、或者打到自己，就跳过
            if (target == null || target == stats || target.IsDead)
            {
                continue;
            }

            // 同一刀只扣一次
            if (hitThisSwing.Contains(target))
            {
                continue;
            }

            hitThisSwing.Add(target);
            target.TakeDamage(damage, gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = (Vector2)transform.position + (Vector2)(transform.right * hitOffset);
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(center, hitRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, center);
    }
}
