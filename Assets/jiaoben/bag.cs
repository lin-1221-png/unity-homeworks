using System;
using UnityEngine;

/// <summary>
/// 背包数据模型（只管数据，不管显示）。
/// 全工程只有这一份背包数据，UI 通过 <see cref="OnInventoryChanged"/> 事件 + <see cref="slots"/> 读取。
/// </summary>
public class bag : MonoBehaviour
{
    [Header("背包格子")]
    [Tooltip("格子总数。Awake 时会按这个值补齐 slots，已有数据不会丢。")]
    public int SlotCount = 35;

    [Tooltip("可以在 Inspector 里预置初始物品；运行时 init() 会保留它们并补齐空格子")]
    public ItemSlot[] slots;

    /// <summary>背包内容发生变化（加 / 减 / 清空）时触发。UI 订阅它来重画。</summary>
    public event Action OnInventoryChanged;

    void Awake() => init();

    // ==================== 初始化 ====================

    /// <summary>
    /// 保证 slots 的长度等于 SlotCount：
    /// 长度不对才重建，重建时保留原有元素；长度已对时只补 null 元素。
    /// </summary>
    public void init()
    {
        if (SlotCount < 0) SlotCount = 0;

        if (slots == null || slots.Length != SlotCount)
        {
            var old = slots;
            slots = new ItemSlot[SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = (old != null && i < old.Length && old[i] != null)
                    ? old[i]                      // 保留 Inspector / 之前塞进去的数据
                    : new ItemSlot();             // 缺的补空格子
            }
        }
        else
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == null) slots[i] = new ItemSlot();
        }
    }

    // ==================== 查询 ====================

    /// <summary>格子数量（= slots.Length）。</summary>
    public int Count => slots != null ? slots.Length : 0;

    /// <summary>取某格的数据，越界返回 null。</summary>
    public ItemSlot GetSlot(int index)
        => (slots != null && index >= 0 && index < slots.Length) ? slots[index] : null;

    /// <summary>某物品在背包里的总数量。</summary>
    public int GetItemCount(ItemDate item)
    {
        if (item == null || slots == null) return 0;
        int total = 0;
        for (int i = 0; i < slots.Length; i++)
            if (!slots[i].IsEmpty && slots[i].item == item)
                total += slots[i].count;
        return total;
    }

    /// <summary>是否还有空格子（可用于判断"能不能再放新物品"）。</summary>
    public bool HasEmptySlot
    {
        get
        {
            if (slots == null) return true;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsEmpty) return true;
            return false;
        }
    }

    /// <summary>所有格子都被占用（注意：可堆叠物品仍可能叠进已有格子）。</summary>
    public bool IsFull => !HasEmptySlot;

    // ==================== 增删 ====================

    /// <summary>
    /// 放入物品。可堆叠的先进已有的同类格子，剩下的再开新格子。
    /// </summary>
    /// <returns>true = 全部放下；false = 背包满了，只放进去一部分（或完全没放）。</returns>
    public bool AddItem(ItemDate item, int count = 1)
    {
        if (item == null || count <= 0) return false;
        if (slots == null) init();

        int perSlot = item.canstack ? Mathf.Max(1, item.max) : 1;   // 不可堆叠：每格只能 1 个
        int remaining = count;
        bool changed = false;

        // 1) 先堆叠到已有的同类格子（原地改字段，引用这个对象的地方都能看到）
        if (item.canstack)
        {
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.item != item || slot.count >= perSlot) continue;

                int add = Mathf.Min(perSlot - slot.count, remaining);
                slot.count += add;
                remaining -= add;
                changed = true;
            }
        }

        // 2) 剩下的放进空格子（复用 ItemSlot 对象，不 new，避免 UI 拿着旧引用）
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            var slot = slots[i];
            if (!slot.IsEmpty) continue;

            int add = Mathf.Min(perSlot, remaining);
            slot.item = item;
            slot.count = add;
            remaining -= add;
            changed = true;
        }

        if (changed) OnInventoryChanged?.Invoke();
        return remaining <= 0;
    }

    /// <summary>
    /// 移除指定物品。数量不够时不做任何修改。
    /// 从后往前扣，先把零散的尾巴清掉。
    /// </summary>
    /// <returns>true = 移除成功；false = 数量不够 / 物品为空。</returns>
    public bool RemoveItem(ItemDate item, int count = 1)
    {
        if (item == null || count <= 0 || slots == null) return false;
        if (GetItemCount(item) < count) return false;

        int remaining = count;
        for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
        {
            var slot = slots[i];
            if (slot.IsEmpty || slot.item != item) continue;

            int take = Mathf.Min(slot.count, remaining);
            slot.count -= take;
            remaining -= take;
            if (slot.count <= 0) { slot.item = null; slot.count = 0; }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>按格子下标移除（用于"右键丢弃一格"之类的操作）。</summary>
    public bool RemoveAt(int index, int count = 1)
    {
        var slot = GetSlot(index);
        if (slot == null || slot.IsEmpty || count <= 0) return false;

        int take = Mathf.Min(slot.count, count);
        slot.count -= take;
        if (slot.count <= 0) { slot.item = null; slot.count = 0; }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>清空整个背包（保留格子对象，只把内容置空）。</summary>
    public void ClearAll()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].item = null;
            slots[i].count = 0;
        }
        OnInventoryChanged?.Invoke();
    }
}
