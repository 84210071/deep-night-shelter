using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player inventory data. UI reads this; it does not store items itself.
/// AddItem / RemoveItem / HasItem are left for you to implement.
/// </summary>
public class Inventory : MonoBehaviour
{
    public const int NormalSlotCount = 16;

    [SerializeField] InventorySlot[] normalSlots = new InventorySlot[NormalSlotCount];
    [SerializeField] List<InventorySlot> keySlots = new List<InventorySlot>();

    public int KeySlotCount => keySlots != null ? keySlots.Count : 0;

    void Awake()
    {
        EnsureNormalSlots();
    }

    public InventorySlot GetNormalSlot(int index)
    {
        EnsureNormalSlots();
        if (index < 0 || index >= normalSlots.Length)
        {
            return null;
        }

        if (normalSlots[index] == null)
        {
            normalSlots[index] = new InventorySlot();
        }

        return normalSlots[index];
    }

    public InventorySlot GetKeySlot(int index)
    {
        if (keySlots == null || index < 0 || index >= keySlots.Count)
        {
            return null;
        }

        return keySlots[index];
    }

    public bool AddItem(ItemData item, int amount)
    {
        // TODO: 实现添加物品。
        // 规则：
        // 1. item 为空或 amount <= 0 时返回 false。
        // 2. Key / Quest 不占普通 16 格，写入 keySlots。
        // 3. 普通物品（Consumable / Document）占用 normalSlots。
        // 4. 相同 ItemData 且未达到 maxStack 的格子优先堆叠。
        // 5. 堆叠满了再找空格子。
        // 6. 16 格都放不下时返回 false。
        return false;
    }

    public bool RemoveItem(ItemData item, int amount)
    {
        // TODO: 实现移除物品。
        // 规则：
        // 1. item 为空或 amount <= 0 时返回 false。
        // 2. 先用 HasItem 判断是否够用，不够则返回 false，不要只扣一部分。
        // 3. Key / Quest 从 keySlots 扣；普通物品从 normalSlots 扣。
        // 4. 某格扣到 0 后调用 slot.Clear()。
        return false;
    }

    public bool HasItem(ItemData item, int amount)
    {
        // TODO: 实现数量检查。
        // 规则：
        // 1. item 为空或 amount <= 0 时返回 false。
        // 2. 按 ItemData 引用比较，不要用 displayName 字符串。
        // 3. 把所有格子里该物品的 amount 加起来，总数 >= amount 才返回 true。
        return false;
    }

    void EnsureNormalSlots()
    {
        if (normalSlots == null || normalSlots.Length != NormalSlotCount)
        {
            InventorySlot[] resized = new InventorySlot[NormalSlotCount];
            int copy = normalSlots != null ? Mathf.Min(normalSlots.Length, NormalSlotCount) : 0;
            for (int i = 0; i < copy; i++)
            {
                resized[i] = normalSlots[i];
            }

            normalSlots = resized;
        }

        for (int i = 0; i < normalSlots.Length; i++)
        {
            if (normalSlots[i] == null)
            {
                normalSlots[i] = new InventorySlot();
            }
        }

        if (keySlots == null)
        {
            keySlots = new List<InventorySlot>();
        }
    }
}
