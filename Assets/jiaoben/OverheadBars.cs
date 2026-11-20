using UnityEngine;

/// <summary>
/// 头顶血条 + 技能条，纯代码生成（1x1 白色像素拉伸），不需要任何美术素材。
/// 挂在任意有 CharacterStats 的角色上即可；玩家额外勾 Show Bullet Time 显示白色的子弹时间冷却条。
/// </summary>
public class OverheadBars : MonoBehaviour
{
    [Header("显示哪些条")]
    [Tooltip("红色血条。敌人、玩家都要勾")]
    [SerializeField] private bool showHealth = true;

    [Tooltip("白色技能条（子弹时间冷却）。只有玩家需要勾")]
    [SerializeField] private bool showBulletTime = false;

    [Header("位置和大小")]
    [Tooltip("血条离角色原点的高度。角色身上有 Collider2D 时会自动按碰撞体顶部来算")]
    [SerializeField] private float heightOffset = 1.2f;

    [SerializeField, Min(0.1f)] private float barWidth = 0.9f;
    [SerializeField, Min(0.02f)] private float barHeight = 0.12f;
    [SerializeField, Min(0f)] private float barGap = 0.06f;

    [Header("颜色")]
    [SerializeField] private Color healthColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color skillColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);

    [Tooltip("画在所有 Sprite 上面的排序值，越大越靠前")]
    [SerializeField] private int sortingOrder = 100;

    private CharacterStats stats;
    private BulletTime bulletTime;
    private PixelBar healthBar;
    private PixelBar skillBar;
    private Transform barRoot;
    private float topOffset;

    private void Start()
    {
        stats = GetComponentInParent<CharacterStats>();

        // 子弹时间通常挂在相机上，所以在整个场景里找一个
        bulletTime = FindFirstObjectByType<BulletTime>();

        // 头顶高度：优先用碰撞体顶部，没有再用手填的值
        topOffset = heightOffset;
        Collider2D body = GetComponentInChildren<Collider2D>();
        if (body != null)
        {
            topOffset = body.bounds.max.y - transform.position.y + 0.25f;
        }

        // 所有条挂在一个空物体上，方便整体跟随和翻转校正
        GameObject root = new GameObject("OverheadBars");
        barRoot = root.transform;
        barRoot.SetParent(transform, false);

        if (showHealth)
        {
            healthBar = new PixelBar(barRoot, 0f, barWidth, barHeight, healthColor, backgroundColor, sortingOrder);
            RefreshHealth();
        }

        if (showBulletTime && bulletTime != null)
        {
            float y = showHealth ? -(barHeight + barGap) : 0f;
            skillBar = new PixelBar(barRoot, y, barWidth, barHeight, skillColor, backgroundColor, sortingOrder - 2);
        }

        if (stats != null)
        {
            stats.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (healthBar != null)
        {
            healthBar.SetValue(max > 0 ? (float)current / max : 0f);
        }
    }

    private void RefreshHealth()
    {
        if (stats != null && healthBar != null)
        {
            healthBar.SetValue(stats.MaxHealth > 0 ? (float)stats.CurrentHealth / stats.MaxHealth : 0f);
        }
    }

    private void LateUpdate()
    {
        if (barRoot != null)
        {
            // 用世界坐标定位 + 强制不旋转：
            // move.cs 翻转角色是旋转 180°，如果跟着转，血条会左右颠倒
            barRoot.SetPositionAndRotation(transform.position + Vector3.up * topOffset, Quaternion.identity);
        }

        UpdateSkillBar();
    }

    private void UpdateSkillBar()
    {
        if (skillBar == null || bulletTime == null)
        {
            return;
        }

        float t;

        if (bulletTime.IsActive)
        {
            // 生效中：条从满慢慢减到空
            t = bulletTime.Duration > 0f ? bulletTime.DurationRemaining / bulletTime.Duration : 0f;
        }
        else if (bulletTime.CooldownRemaining > 0f)
        {
            // 冷却中：条从空慢慢涨回满，满了就代表能用
            t = bulletTime.Cooldown > 0f ? 1f - bulletTime.CooldownRemaining / bulletTime.Cooldown : 0f;
        }
        else
        {
            t = 1f;
        }

        skillBar.SetValue(t);
    }
}

/// <summary>一个用像素画的条：背景 + 填充。填充从左往右。</summary>
public class PixelBar
{
    private static Sprite pixelSprite;

    private readonly Transform fill;
    private readonly float width;
    private readonly float height;

    public PixelBar(Transform parent, float localY, float width, float height,
                    Color fillColor, Color backColor, int sortingOrder)
    {
        this.width = width;
        this.height = height;

        GameObject root = new GameObject("Bar");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, localY, 0f);

        if (backColor.a > 0f)
        {
            GameObject back = new GameObject("Background");
            back.transform.SetParent(root.transform, false);
            SpriteRenderer backRenderer = back.AddComponent<SpriteRenderer>();
            backRenderer.sprite = GetPixelSprite();
            backRenderer.color = backColor;
            backRenderer.sortingOrder = sortingOrder;
            back.transform.localScale = new Vector3(width + 0.05f, height + 0.05f, 1f);
        }

        GameObject bar = new GameObject("Fill");
        bar.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = bar.AddComponent<SpriteRenderer>();
        renderer.sprite = GetPixelSprite();
        renderer.color = fillColor;
        renderer.sortingOrder = sortingOrder + 1;

        fill = bar.transform;
        SetValue(1f);
    }

    /// <summary>0 = 空，1 = 满</summary>
    public void SetValue(float value01)
    {
        float t = Mathf.Clamp01(value01);
        float w = width * t;

        fill.localScale = new Vector3(w, height, 1f);
        // 左端固定：条的中心要往右挪半个宽度
        fill.localPosition = new Vector3(-width * 0.5f + w * 0.5f, 0f, 0f);
    }

    /// <summary>整张图就一个白点，所有条共用</summary>
    private static Sprite GetPixelSprite()
    {
        if (pixelSprite != null)
        {
            return pixelSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        pixelSprite.name = "Pixel";

        return pixelSprite;
    }
}
