# 背包系统接口说明

## 一句话数据流

```
bag（唯一数据源） ──OnInventoryChanged 事件──> ui.Refresh() ──> 读 bag.slots 重画格子
```

UI **不再保存副本**，所有显示都直接从 `bag.slots` 读。

---

## bag（挂在 player 上）

### 字段

| 字段 | 类型 | 说明 |
|---|---|---|
| `SlotCount` | `int` | 格子总数，默认 35。`Awake` 时按这个值补齐 `slots` |
| `slots` | `ItemSlot[]` | 格子数据。**可以在 Inspector 里预置初始物品**，`init()` 会保留它们 |
| `OnInventoryChanged` | `event Action` | 背包内容变化（加 / 减 / 清空）时触发一次 |

### 方法

| 方法 | 说明 | 返回 |
|---|---|---|
| `init()` | 保证 `slots.Length == SlotCount`：长度不对才重建（保留原元素），长度对就只补 null。`Awake` 已自动调用一次 | `void` |
| `AddItem(ItemDate item, int count = 1)` | 放入物品。可堆叠的先叠进已有同类格，剩下的再开新格；不可堆叠的每格 1 个 | `bool`：`true` = 全部放下，`false` = 背包满了只放进去一部分 |
| `RemoveItem(ItemDate item, int count = 1)` | 按物品移除，从后往前扣；数量不够时**不做任何修改** | `bool`：是否成功 |
| `RemoveAt(int index, int count = 1)` | 按格子下标移除（用于“丢弃这一格”） | `bool`：是否成功 |
| `ClearAll()` | 清空所有格子（保留格子对象） | `void` |
| `GetItemCount(ItemDate item)` | 某物品的总数量 | `int` |
| `GetSlot(int index)` | 取某一格，越界返回 `null` | `ItemSlot` |
| `Count` | 格子数量（= `slots.Length`） | `int` |
| `HasEmptySlot` | 是否还有空格子 | `bool` |
| `IsFull` | 是否所有格子都被占用（可堆叠物品仍可能叠进已有格） | `bool` |

---

## ui（挂在 Canvas1 上）

| 成员 | 说明 |
|---|---|
| `slotPrefab` | 格子预制体（现在是 `Assets/ditu.prefab`） |
| `gridParent` | 格子父节点（`bookroot/Image/right/grid`，上面有 GridLayoutGroup 7 列） |
| `bags` | 数据源，拖 player 上的 `bag` |
| `Refresh()` | 按 `bag.slots` 重画全部 35 格；可以反复调用，不会重复实例化 |
| `summer()` | `Refresh()` 的旧名字，保留兼容 |

界面在 `OnEnable` 自动订阅 `OnInventoryChanged`、`OnDisable` 自动退订。因为 `Canvas1` 是 `uimanager` 打开背包时才激活的，所以**打开背包的那一刻会自动按最新数据重画**。

---

## 调用示例

```csharp
public class 示例用法 : MonoBehaviour
{
    public bag bag;             // Inspector 拖 player
    public ItemDate potion;     // Inspector 拖 Assets/Itemlist/治疗药剂.asset

    void Start()
    {
        bag.AddItem(potion, 3);                 // 放 3 个
        Debug.Log(bag.GetItemCount(potion));    // 3
        bag.RemoveItem(potion, 1);              // 用掉 1 个
        Debug.Log(bag.GetItemCount(potion));    // 2
    }
}
```

监听背包变化（任务计数、存档、合成界面都可以这么接）：

```csharp
void OnEnable()  => bag.OnInventoryChanged += OnBagChanged;
void OnDisable() => bag.OnInventoryChanged -= OnBagChanged;

void OnBagChanged()
{
    // 需要时读 bag.slots / bag.GetItemCount(...)，不要缓存副本
}
```

判断能不能放下再放：

```csharp
if (bag.HasEmptySlot || bag.GetItemCount(potion) > 0)
    bag.AddItem(potion, 10);
```

---

## 数据结构

`ItemSlot`（普通可序列化类，不是 MonoBehaviour）：

```csharp
public ItemDate item;   // 物品定义，null = 空格子
public int count;       // 数量
public bool IsEmpty => item == null;
```

`ItemDate`（ScriptableObject，右键 **Create → ItemDate/Item** 新建）：

```csharp
public int id;
public string type;      // 消耗品 / 武器 / 材料
public Sprite icon;
public bool canstack;    // 能否堆叠
public int max;          // 每格上限
```

---

## 三条约定（避开之前的坑）

1. **不要给 UI 再存一份 List**。之前的 `ui.ItemList` 就是副本，`bag` 改数据它看不到。
2. **不要用 `new ItemSlot(...)` 替换数组元素**，直接改 `slot.item` / `slot.count` 字段 —— 不然 UI 手里还是旧引用。
3. **数据变化只走 `OnInventoryChanged`**，不要到处手动调 `Refresh()`（手动调只在做编辑器工具时需要）。

## 和旧代码的差异（迁移时注意）

- 删除了 `ui.ItemList`、`ui.tool`、`ui.re()`
- `bag.init()` 从 `ui.Start()` 移到 `bag.Awake()`
- `bag.slots` 现在会**保留 Inspector 里预置的数据**（以前一进 Play 就被清空）
- `AddItem` 不再替换 `ItemSlot` 对象；不可堆叠物品每格 1 个（以前会按 `max` 塞满一格）
- `CountText` 角标：预制体 `Assets/ditu.prefab` 里改成了最后一个子物体（不再被图标盖住），白字带描边、字号 26、贴右下角
