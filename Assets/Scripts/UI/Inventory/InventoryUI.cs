using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Inventory view. Reads Inventory data and never stores items itself.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    const string PlayerMapName = "Player";
    const string InventoryActionName = "Inventory";

    [Header("Data")]
    [SerializeField] Inventory inventory;
    [SerializeField] FirstPersonController playerController;
    [SerializeField] InputActionAsset inputActions;

    [Header("Root")]
    [SerializeField] GameObject inventoryRoot;
    [SerializeField] GameObject darkOverlay;

    [Header("Tabs")]
    [SerializeField] Button normalItemsTab;
    [SerializeField] Button keyItemsTab;
    [SerializeField] Image normalItemsTabImage;
    [SerializeField] Image keyItemsTabImage;

    [Header("Grid")]
    [SerializeField] Transform itemGrid;
    [SerializeField] InventorySlotUI[] slots;

    [Header("Detail")]
    [SerializeField] Image detailIcon;
    [SerializeField] TMP_Text itemNameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text amountText;
    [SerializeField] Button useButton;

    [Header("Header")]
    [SerializeField] Button closeButton;
    [SerializeField] TMP_Text controlHint;
    [SerializeField] TMP_Text titleText;

    InputAction _inventoryAction;
    bool _isOpen;
    bool _showingKeyItems;
    int _selectedIndex = -1;

    public bool IsOpen => _isOpen;

    void Awake()
    {
        ResolveReferences();
        EnsureLayout();
        BindSlots();
        BindButtons();
    }

    void OnEnable()
    {
        ResolveActions();
        if (_inventoryAction != null)
        {
            _inventoryAction.performed += OnInventoryPerformed;
            _inventoryAction.Enable();
        }
    }

    void OnDisable()
    {
        if (_inventoryAction != null)
        {
            _inventoryAction.performed -= OnInventoryPerformed;
        }

        if (_isOpen)
        {
            Close();
        }
    }

    void Update()
    {
        if (!_isOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            ShowTab(false);
        }
        else if (keyboard.eKey.wasPressedThisFrame)
        {
            ShowTab(true);
        }
    }

    public void Open()
    {
        if (_isOpen)
        {
            return;
        }

        _isOpen = true;
        if (inventoryRoot != null)
        {
            inventoryRoot.SetActive(true);
        }

        if (playerController != null)
        {
            playerController.SetGameplayInputEnabled(false);
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.sendNavigationEvents = false;
        }

        Refresh();
    }

    public void Close()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        if (inventoryRoot != null)
        {
            inventoryRoot.SetActive(false);
        }

        if (playerController != null)
        {
            playerController.SetGameplayInputEnabled(true);
        }
    }

    public void Toggle()
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void SelectSlot(int index)
    {
        _selectedIndex = index;
        Refresh();
    }

    public void Refresh()
    {
        if (inventory == null || slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlotUI slotUi = slots[i];
            if (slotUi == null)
            {
                continue;
            }

            InventorySlot data = _showingKeyItems ? inventory.GetKeySlot(i) : inventory.GetNormalSlot(i);
            slotUi.Refresh(data, i == _selectedIndex);
        }

        RefreshTabs();
        RefreshDetail();
    }

    void OnInventoryPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        Toggle();
    }

    void ShowTab(bool keyItems)
    {
        if (_showingKeyItems == keyItems)
        {
            return;
        }

        _showingKeyItems = keyItems;
        _selectedIndex = -1;
        Refresh();
    }

    void OnUseClicked()
    {
        InventorySlot slot = GetSelectedSlot();
        if (slot == null || slot.IsEmpty || slot.item.ItemType != ItemType.Consumable)
        {
            return;
        }

        // Reserved for Health later. Do not consume or restore HP in this version.
    }

    InventorySlot GetSelectedSlot()
    {
        if (inventory == null || _selectedIndex < 0)
        {
            return null;
        }

        return _showingKeyItems ? inventory.GetKeySlot(_selectedIndex) : inventory.GetNormalSlot(_selectedIndex);
    }

    void RefreshTabs()
    {
        Color active = new Color(0.22f, 0.28f, 0.2f, 0.96f);
        Color idle = new Color(0.1f, 0.11f, 0.1f, 0.92f);
        if (normalItemsTabImage != null)
        {
            normalItemsTabImage.color = _showingKeyItems ? idle : active;
        }

        if (keyItemsTabImage != null)
        {
            keyItemsTabImage.color = _showingKeyItems ? active : idle;
        }
    }

    void RefreshDetail()
    {
        InventorySlot slot = GetSelectedSlot();
        bool hasItem = slot != null && !slot.IsEmpty;

        if (detailIcon != null)
        {
            detailIcon.enabled = hasItem && slot.item.Icon != null;
            detailIcon.sprite = hasItem ? slot.item.Icon : null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = hasItem ? slot.item.DisplayName : "选择一个物品";
        }

        if (descriptionText != null)
        {
            descriptionText.text = hasItem ? slot.item.Description : string.Empty;
        }

        if (amountText != null)
        {
            amountText.text = hasItem ? "数量：" + slot.amount : string.Empty;
        }

        if (useButton != null)
        {
            bool showUse = hasItem && slot.item.ItemType == ItemType.Consumable;
            useButton.gameObject.SetActive(showUse);
        }
    }

    void ResolveReferences()
    {
        if (inventory == null)
        {
            inventory = FindObjectOfType<Inventory>();
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
        }

        if (inputActions == null && playerController != null)
        {
            inputActions = playerController.InputActions;
        }
    }

    void ResolveActions()
    {
        if (inputActions == null || _inventoryAction != null)
        {
            return;
        }

        InputActionMap map = inputActions.FindActionMap(PlayerMapName, false);
        if (map != null)
        {
            _inventoryAction = map.FindAction(InventoryActionName, false);
        }
    }

    void BindSlots()
    {
        if (itemGrid == null)
        {
            return;
        }

        if (slots == null || slots.Length != Inventory.NormalSlotCount)
        {
            slots = itemGrid.GetComponentsInChildren<InventorySlotUI>(true);
        }

        if (slots.Length != Inventory.NormalSlotCount)
        {
            CreateMissingSlots();
            slots = itemGrid.GetComponentsInChildren<InventorySlotUI>(true);
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].Bind(this, i);
            }
        }
    }

    void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (useButton != null)
        {
            useButton.onClick.RemoveListener(OnUseClicked);
            useButton.onClick.AddListener(OnUseClicked);
        }

        if (normalItemsTab != null)
        {
            normalItemsTab.onClick.RemoveAllListeners();
            normalItemsTab.onClick.AddListener(() => ShowTab(false));
        }

        if (keyItemsTab != null)
        {
            keyItemsTab.onClick.RemoveAllListeners();
            keyItemsTab.onClick.AddListener(() => ShowTab(true));
        }
    }

    void CreateMissingSlots()
    {
        InventorySlotUI[] existing = itemGrid.GetComponentsInChildren<InventorySlotUI>(true);
        for (int i = existing.Length; i < Inventory.NormalSlotCount; i++)
        {
            GameObject slotObject = new GameObject("Slot_" + i.ToString("00"), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(InventorySlotUI));
            slotObject.transform.SetParent(itemGrid, false);
            InventorySlotUI slotUi = slotObject.GetComponent<InventorySlotUI>();
            slotUi.EnsureVisuals();
        }
    }

    void EnsureLayout()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
        }

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (inventoryRoot == null)
        {
            Transform existing = transform.Find("InventoryRoot");
            inventoryRoot = existing != null ? existing.gameObject : CreateChild("InventoryRoot", transform).gameObject;
        }

        if (darkOverlay == null)
        {
            Transform existing = inventoryRoot.transform.Find("DarkOverlay");
            darkOverlay = existing != null ? existing.gameObject : CreateStretchImage("DarkOverlay", inventoryRoot.transform, new Color(0f, 0f, 0f, 0.62f)).gameObject;
        }

        Transform mainPanel = inventoryRoot.transform.Find("MainPanel");
        if (mainPanel == null)
        {
            mainPanel = CreatePanel("MainPanel", inventoryRoot.transform, new Vector2(1180f, 720f), new Color(0.07f, 0.08f, 0.07f, 0.94f));
        }

        Transform header = mainPanel.Find("Header");
        if (header == null)
        {
            header = CreateAnchored("Header", mainPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -78f), new Vector2(-24f, -16f));
        }

        EnsureText(header, "Title", "背包", 36, TextAlignmentOptions.Left, ref titleText);

        if (closeButton == null)
        {
            Transform existing = header.Find("CloseButton");
            closeButton = existing != null ? existing.GetComponent<Button>() : CreateButton("CloseButton", header, "×", new Vector2(48f, 48f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f));
        }

        Transform tabs = mainPanel.Find("Tabs");
        if (tabs == null)
        {
            tabs = CreateAnchored("Tabs", mainPanel, new Vector2(0f, 1f), new Vector2(0.68f, 1f), new Vector2(24f, -136f), new Vector2(-12f, -88f));
            HorizontalLayoutGroup tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 12f;
            tabLayout.childAlignment = TextAnchor.MiddleLeft;
            tabLayout.childForceExpandWidth = false;
            tabLayout.childForceExpandHeight = true;
            tabLayout.childControlWidth = false;
            tabLayout.childControlHeight = true;
        }

        if (normalItemsTab == null)
        {
            Transform existing = tabs.Find("NormalItems");
            GameObject tabObject = existing != null ? existing.gameObject : CreateTab("NormalItems", tabs, "物品");
            normalItemsTab = tabObject.GetComponent<Button>();
            normalItemsTabImage = tabObject.GetComponent<Image>();
        }

        if (keyItemsTab == null)
        {
            Transform existing = tabs.Find("KeyItems");
            GameObject tabObject = existing != null ? existing.gameObject : CreateTab("KeyItems", tabs, "钥匙物品");
            keyItemsTab = tabObject.GetComponent<Button>();
            keyItemsTabImage = tabObject.GetComponent<Image>();
        }

        if (itemGrid == null)
        {
            Transform existing = mainPanel.Find("ItemGrid");
            itemGrid = existing != null ? existing : CreateAnchored("ItemGrid", mainPanel, new Vector2(0f, 0f), new Vector2(0.68f, 1f), new Vector2(24f, 72f), new Vector2(-12f, -148f));
            GridLayoutGroup grid = itemGrid.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = itemGrid.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.cellSize = new Vector2(96f, 96f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.padding = new RectOffset(4, 4, 4, 4);
        }

        Transform detail = mainPanel.Find("DetailPanel");
        if (detail == null)
        {
            detail = CreateAnchored("DetailPanel", mainPanel, new Vector2(0.68f, 0f), new Vector2(1f, 1f), new Vector2(12f, 72f), new Vector2(-24f, -88f));
            Image detailBg = detail.GetComponent<Image>();
            if (detailBg == null)
            {
                detailBg = detail.gameObject.AddComponent<Image>();
            }

            detailBg.color = new Color(0.05f, 0.06f, 0.05f, 0.9f);
            VerticalLayoutGroup vertical = detail.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(20, 20, 20, 20);
            vertical.spacing = 12f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
        }

        if (detailIcon == null)
        {
            Transform existing = detail.Find("ItemIcon");
            detailIcon = existing != null ? existing.GetComponent<Image>() : CreateLayoutImage("ItemIcon", detail, new Vector2(160f, 160f), new Color(0.16f, 0.17f, 0.15f, 1f));
            detailIcon.preserveAspect = true;
            detailIcon.enabled = false;
        }

        EnsureText(detail, "ItemName", "选择一个物品", 30, TextAlignmentOptions.Center, ref itemNameText);
        EnsureText(detail, "Description", string.Empty, 20, TextAlignmentOptions.TopLeft, ref descriptionText);
        if (descriptionText != null)
        {
            LayoutElement descriptionLayout = descriptionText.GetComponent<LayoutElement>();
            if (descriptionLayout == null)
            {
                descriptionLayout = descriptionText.gameObject.AddComponent<LayoutElement>();
            }

            descriptionLayout.minHeight = 120f;
            descriptionLayout.preferredHeight = 140f;
        }

        EnsureText(detail, "Amount", string.Empty, 22, TextAlignmentOptions.Left, ref amountText);

        if (useButton == null)
        {
            Transform existing = detail.Find("UseButton");
            useButton = existing != null ? existing.GetComponent<Button>() : CreateButton("UseButton", detail, "使用", new Vector2(180f, 48f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f));
            useButton.gameObject.SetActive(false);
        }

        if (controlHint == null)
        {
            Transform existing = inventoryRoot.transform.Find("ControlHint");
            if (existing == null)
            {
                RectTransform hint = CreateAnchored("ControlHint", inventoryRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-420f, 24f), new Vector2(-32f, 64f));
                controlHint = childText(hint, "[Tab] 打开/关闭   [Esc] 关闭背包", 20, TextAlignmentOptions.Right);
            }
            else
            {
                controlHint = existing.GetComponent<TMP_Text>();
            }
        }

        inventoryRoot.SetActive(false);
    }

    void EnsureText(Transform parent, string childName, string value, float size, TextAlignmentOptions align, ref TMP_Text field)
    {
        if (field != null)
        {
            return;
        }

        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            field = existing.GetComponent<TMP_Text>();
            return;
        }

        field = childText(CreateAnchored(childName, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), value, size, align);
    }

    TMP_Text childText(RectTransform parent, string value, float size, TextAlignmentOptions align)
    {
        TextMeshProUGUI existing = parent.GetComponent<TextMeshProUGUI>();
        if (existing == null)
        {
            existing = parent.gameObject.AddComponent<TextMeshProUGUI>();
        }

        existing.text = value;
        existing.fontSize = size;
        existing.alignment = align;
        existing.color = Color.white;
        existing.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
        {
            existing.font = TMP_Settings.defaultFontAsset;
        }

        return existing;
    }

    RectTransform CreateChild(string childName, Transform parent)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    Image CreateStretchImage(string childName, Transform parent, Color color)
    {
        RectTransform rect = CreateChild(childName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = BuiltinUiSprite();
        return image;
    }

    RectTransform CreatePanel(string childName, Transform parent, Vector2 size, Color color)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        child.GetComponent<Image>().color = color;
        child.GetComponent<Image>().sprite = BuiltinUiSprite();
        return rect;
    }

    RectTransform CreateAnchored(string childName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    Image CreateLayoutImage(string childName, Transform parent, Vector2 size, Color color)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        Image image = child.GetComponent<Image>();
        image.color = color;
        image.sprite = BuiltinUiSprite();
        LayoutElement layout = child.AddComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.minHeight = size.y;
        return image;
    }

    GameObject CreateTab(string childName, Transform parent, string label)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180f, 40f);
        LayoutElement layout = child.AddComponent<LayoutElement>();
        layout.preferredWidth = 180f;
        layout.preferredHeight = 40f;
        Image image = child.GetComponent<Image>();
        image.color = new Color(0.1f, 0.11f, 0.1f, 0.92f);
        image.sprite = BuiltinUiSprite();
        Button button = child.GetComponent<Button>();
        button.targetGraphic = image;
        RectTransform labelRect = CreateAnchored("Label", child.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        childText(labelRect, label, 22, TextAlignmentOptions.Center);
        return child;
    }

    Button CreateButton(string childName, Transform parent, string label, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        Image image = child.GetComponent<Image>();
        image.color = new Color(0.18f, 0.22f, 0.16f, 1f);
        image.sprite = BuiltinUiSprite();
        Button button = child.GetComponent<Button>();
        button.targetGraphic = image;
        RectTransform labelRect = CreateAnchored("Label", child.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        childText(labelRect, label, 22, TextAlignmentOptions.Center);
        LayoutElement layout = child.AddComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.minHeight = size.y;
        return button;
    }

    static Sprite BuiltinUiSprite()
    {
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
    }
}
