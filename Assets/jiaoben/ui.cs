using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 背包界面的渲染器：只读 <see cref="bag"/> 的数据，自己不保存副本。
/// 数据变化由 bag.OnInventoryChanged 通知，这里只负责把 35 个格子的图标 / 数量刷新一遍。
/// </summary>
public class ui : MonoBehaviour
{
    [Header("格子配置")]
    public GameObject slotPrefab;
    public Transform gridParent;

    [Header("数据源")]
    public bag bags;

    readonly List<GameObject> views = new List<GameObject>();
    readonly List<Image> icons = new List<Image>();
    readonly List<TextMeshProUGUI> counts = new List<TextMeshProUGUI>();

    void OnEnable()
    {
        if (bags != null) bags.OnInventoryChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (bags != null) bags.OnInventoryChanged -= Refresh;
    }

    /// <summary>兼容旧调用名，等价于 <see cref="Refresh"/>。</summary>
    public void summer() => Refresh();

    /// <summary>按 bag.slots 重画全部格子。可以反复调用，不会重复实例化。</summary>
    public void Refresh()
    {
        if (bags == null)
        {
            Debug.LogWarning("[ui] 没有绑定 bag（Inspector 里的 Data Source 字段）", this);
            return;
        }
        if (bags.slots == null) bags.init();

        BuildViews();

        for (int i = 0; i < views.Count; i++)
        {
            var data = bags.GetSlot(i);
            bool hasItem = data != null && !data.IsEmpty;

            // 图标
            if (icons[i] != null)
            {
                icons[i].gameObject.SetActive(hasItem);
                if (hasItem) icons[i].sprite = data.item.icon;
            }

            // 数量角标：只有数量 > 1 才显示
            if (counts[i] != null)
            {
                bool showCount = hasItem && data.count > 1;
                counts[i].gameObject.SetActive(showCount);
                if (showCount) counts[i].text = data.count.ToString();
            }
        }
    }

    /// <summary>按需实例化格子预制体，只建一次。</summary>
    void BuildViews()
    {
        int need = bags.Count;

        while (views.Count < need)
        {
            var go = Instantiate(slotPrefab, gridParent);
            views.Add(go);
            icons.Add(go.transform.Find("Icon") != null
                ? go.transform.Find("Icon").GetComponent<Image>() : null);
            counts.Add(go.transform.Find("CountText") != null
                ? go.transform.Find("CountText").GetComponent<TextMeshProUGUI>() : null);
        }

        for (int i = 0; i < views.Count; i++)
            if (views[i].activeSelf != (i < need)) views[i].SetActive(i < need);
    }
}
