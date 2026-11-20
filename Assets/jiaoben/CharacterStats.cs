using System;
using UnityEngine;

/// <summary>
/// 通用角色属性：血量 + 攻击力。
/// 玩家、敌人、NPC 都能挂这一个组件；attack.cs 造成伤害时读取这里的攻击力，
/// 被打时由这里扣血。不用改代码就能在 Inspector 里给每个角色配不同的数值。
/// </summary>
[DisallowMultipleComponent]
public class CharacterStats : MonoBehaviour
{
    [Header("血量")]
    [Tooltip("最大血量")]
    [SerializeField, Min(1)] private int maxHealth = 100;

    [Tooltip("受伤后的无敌时间（秒）。0 表示每一下都会掉血；0.5 左右可以防止一次攻击扣好几次")]
    [SerializeField, Min(0f)] private float invincibleTime = 0f;

    [Header("攻击力")]
    [Tooltip("攻击力。attack.cs 打中别人时用这个数值扣对方血量")]
    [SerializeField, Min(0)] private int attackPower = 10;

    [Header("金币")]
    [Tooltip("身上有多少金币。玩家开局填 0；敌人这个字段没用，看下面的击杀奖励")]
    [SerializeField, Min(0)] private int coins = 0;

    [Tooltip("被打死时给击杀者多少金币。敌人填 5、10 这样；玩家填 0")]
    [SerializeField, Min(0)] private int coinReward = 5;

    [Header("死亡")]
    [Tooltip("血量归零时要禁用掉的组件，比如 move、attack（敌人也可以用）")]
    [SerializeField] private Behaviour[] disableOnDeath;

    [Tooltip("血量归零后多久销毁物体（秒）。0 = 立刻消失；想播死亡动画就填 0.3~0.5")]
    [SerializeField, Min(0f)] private float destroyDelay = 0f;

    [Tooltip("死亡时保留物体不销毁。玩家这种死了不能消失的勾上；敌人不用勾")]
    [SerializeField] private bool keepOnDeath = false;

    /// <summary>最大血量</summary>
    public int MaxHealth => maxHealth;

    /// <summary>当前血量</summary>
    public int CurrentHealth { get; private set; }

    /// <summary>攻击力</summary>
    public int AttackPower => attackPower;

    /// <summary>当前金币</summary>
    public int Coins => coins;

    /// <summary>死亡时是否保留物体。玩家要重生必须为 true，敌人保持 false</summary>
    public bool KeepOnDeath
    {
        get => keepOnDeath;
        set => keepOnDeath = value;
    }

    /// <summary>是否已经死亡</summary>
    public bool IsDead => CurrentHealth <= 0;

    /// <summary>是否在无敌时间内</summary>
    public bool IsInvincible => Time.time < invincibleUntil;

    /// <summary>血量变化时触发：(当前血量, 最大血量)。做血条时接这个事件</summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>受伤时触发：实际扣掉的血量</summary>
    public event Action<int> OnDamaged;

    /// <summary>死亡时触发，参数是自己</summary>
    public event Action<CharacterStats> OnDied;

    /// <summary>金币变化时触发：当前金币。做金币显示时接这个事件</summary>
    public event Action<int> OnCoinsChanged;

    private float invincibleUntil;
    private bool deathHandled;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        // 统一广播一次，让挂在 Start 里的血条能拿到初始值
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>
    /// 扣血。攻击方调用这个（attack.cs 就是这么做的）。
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="source">伤害来源，可以不传</param>
    /// <returns>实际扣掉的血量</returns>
    public int TakeDamage(int damage, GameObject source = null)
    {
        if (damage <= 0 || IsDead)
        {
            return 0;
        }

        if (IsInvincible)
        {
            return 0;
        }

        int before = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        int actual = before - CurrentHealth;

        if (invincibleTime > 0f)
        {
            invincibleUntil = Time.time + invincibleTime;
        }

        OnDamaged?.Invoke(actual);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        Debug.Log($"{name} 受到 {actual} 点伤害，剩余血量 {CurrentHealth}/{maxHealth}"
                  + (source != null ? $"（来源：{source.name}）" : ""), this);

        if (IsDead)
        {
            Die(source);
        }

        return actual;
    }

    /// <summary>回血</summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        int before = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        if (CurrentHealth != before)
        {
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }

    /// <summary>直接把血量设成某个值（复活、读档时用）</summary>
    public void SetHealth(int value)
    {
        CurrentHealth = Mathf.Clamp(value, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>血条 UI 初始化用；也可以拿来复活</summary>
    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        invincibleUntil = 0f;
        deathHandled = false;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>
    /// 复活：清掉死亡标记、把 disableOnDeath 里禁用的组件重新打开、血量回满。
    /// 和 ResetHealth 的区别是它会恢复被禁用的脚本，重生必须用这个。
    /// </summary>
    public void Revive(int health = -1)
    {
        deathHandled = false;
        invincibleUntil = 0f;

        if (disableOnDeath != null)
        {
            foreach (Behaviour b in disableOnDeath)
            {
                if (b != null)
                {
                    b.enabled = true;
                }
            }
        }

        CurrentHealth = health > 0 ? Mathf.Clamp(health, 1, maxHealth) : maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>改最大血量；refill 为 true 时顺便回满</summary>
    public void SetMaxHealth(int value, bool refill = false)
    {
        maxHealth = Mathf.Max(1, value);
        CurrentHealth = refill ? maxHealth : Mathf.Min(CurrentHealth, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>改攻击力（换武器、加 buff 时用）</summary>
    public void SetAttackPower(int value)
    {
        attackPower = Mathf.Max(0, value);
    }

    /// <summary>加金币（打怪奖励、捡钱时用）</summary>
    public void AddCoins(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        coins = Mathf.Max(0, coins + amount);
        OnCoinsChanged?.Invoke(coins);
    }

    /// <summary>花金币：够就扣掉并返回 true，不够返回 false（商店、复活用）</summary>
    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (coins < amount)
        {
            return false;
        }

        coins -= amount;
        OnCoinsChanged?.Invoke(coins);
        return true;
    }

    /// <summary>直接设置金币（读档时用）</summary>
    public void SetCoins(int value)
    {
        coins = Mathf.Max(0, value);
        OnCoinsChanged?.Invoke(coins);
    }

    /// <summary>给一段无敌时间（重生保护、受击闪避）</summary>
    public void SetInvincible(float seconds)
    {
        if (seconds > 0f)
        {
            invincibleUntil = Time.time + seconds;
        }
    }

    private void Die(GameObject source)
    {
        // 只死一次：就算同一帧被好几刀同时打到，也不会重复播死亡、重复销毁
        if (deathHandled)
        {
            return;
        }

        deathHandled = true;

        Debug.Log($"{name} 血量归零，死亡" + (source != null ? $"（最后一击：{source.name}）" : ""), this);

        // 击杀奖励：把金币给打死它的那个角色（玩家打敌人时，source 就是玩家）
        if (coinReward > 0 && source != null && source != gameObject)
        {
            CharacterStats killer = source.GetComponentInParent<CharacterStats>();
            if (killer != null && killer != this)
            {
                killer.AddCoins(coinReward);
                Debug.Log($"{killer.name} 击杀 {name}，获得 {coinReward} 金币（现在有 {killer.Coins}）", this);
            }
        }

        OnDied?.Invoke(this);

        if (disableOnDeath != null)
        {
            foreach (Behaviour b in disableOnDeath)
            {
                if (b != null)
                {
                    b.enabled = false;
                }
            }
        }

        // 血量归零 = 死亡。敌人直接消失；勾了「保留物体」的角色（比如玩家）留着
        if (!keepOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void OnValidate()
    {
        if (maxHealth < 1)
        {
            maxHealth = 1;
        }

        if (attackPower < 0)
        {
            attackPower = 0;
        }
    }
}
