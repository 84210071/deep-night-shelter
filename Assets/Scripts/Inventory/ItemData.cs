using UnityEngine;

public enum ItemType
{
    Consumable,
    Key,
    Quest,
    Document
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Deep Night Shelter/Item Data")]
public class ItemData : ScriptableObject
{
    [SerializeField] string itemId;
    [SerializeField] string displayName;
    [SerializeField] Sprite icon;
    [TextArea(2, 6)]
    [SerializeField] string description;
    [SerializeField] ItemType itemType;
    [SerializeField] int maxStack = 1;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public ItemType ItemType => itemType;
    public int MaxStack => Mathf.Max(1, maxStack);

    public bool OccupiesNormalSlot => itemType != ItemType.Key && itemType != ItemType.Quest;
}
