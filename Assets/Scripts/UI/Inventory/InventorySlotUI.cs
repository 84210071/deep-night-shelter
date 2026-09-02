using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text amountText;
    [SerializeField] Image highlight;
    [SerializeField] Button button;

    InventoryUI _owner;
    int _index;

    public int Index => _index;

    void Awake()
    {
        EnsureVisuals();
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Bind(InventoryUI owner, int index)
    {
        _owner = owner;
        _index = index;
        EnsureVisuals();
    }

    public void Refresh(InventorySlot slot, bool selected)
    {
        EnsureVisuals();

        bool hasItem = slot != null && !slot.IsEmpty;
        if (icon != null)
        {
            icon.enabled = hasItem && slot.item.Icon != null;
            icon.sprite = hasItem ? slot.item.Icon : null;
            icon.color = Color.white;
        }

        if (amountText != null)
        {
            bool showAmount = hasItem && slot.amount > 1;
            amountText.enabled = showAmount;
            amountText.text = showAmount ? slot.amount.ToString() : string.Empty;
        }

        if (highlight != null)
        {
            highlight.enabled = selected;
        }
    }

    void HandleClick()
    {
        if (_owner != null)
        {
            _owner.SelectSlot(_index);
        }
    }

    public void EnsureVisuals()
    {
        Image background = GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.13f, 0.12f, 0.95f);
            background.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        if (highlight == null)
        {
            Transform existing = transform.Find("Highlight");
            highlight = existing != null ? existing.GetComponent<Image>() : CreateChildImage("Highlight", new Color(1f, 1f, 1f, 0.35f));
            highlight.raycastTarget = false;
            highlight.enabled = false;
            RectTransform highlightRect = highlight.rectTransform;
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = new Vector2(-2f, -2f);
            highlightRect.offsetMax = new Vector2(2f, 2f);
            highlight.transform.SetAsFirstSibling();
        }

        if (icon == null)
        {
            Transform existing = transform.Find("Icon");
            icon = existing != null ? existing.GetComponent<Image>() : CreateChildImage("Icon", Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.15f, 0.22f);
            iconRect.anchorMax = new Vector2(0.85f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            icon.enabled = false;
        }

        if (amountText == null)
        {
            Transform existing = transform.Find("Amount");
            amountText = existing != null ? existing.GetComponent<TMP_Text>() : CreateChildText("Amount", 18, TextAlignmentOptions.BottomRight);
            amountText.raycastTarget = false;
            RectTransform amountRect = amountText.rectTransform;
            amountRect.anchorMin = Vector2.zero;
            amountRect.anchorMax = Vector2.one;
            amountRect.offsetMin = new Vector2(6f, 4f);
            amountRect.offsetMax = new Vector2(-6f, -4f);
        }
    }

    Image CreateChildImage(string childName, Color color)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(transform, false);
        Image image = child.GetComponent<Image>();
        image.color = color;
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        return image;
    }

    TMP_Text CreateChildText(string childName, float size, TextAlignmentOptions align)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        child.transform.SetParent(transform, false);
        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;
        text.text = string.Empty;
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }
}
