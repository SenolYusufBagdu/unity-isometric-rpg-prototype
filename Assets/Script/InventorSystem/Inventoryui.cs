using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// InventoryUI.cs → Canvas'a ekle (veya herhangi bir GameObject)
///
/// I tuşu ile açılıp kapanır.
/// Kendi Canvas'ını otomatik oluşturur.
///
/// KURULUM:
/// 1. Player'a Inventory.cs ekle
/// 2. Herhangi bir GameObject'e bu scripti ekle
/// 3. Oynat — I tuşuna bas
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    // ─────────────────────────────────────────────────────────
    // RENK PALETİ
    // ─────────────────────────────────────────────────────────

    static readonly Color colBg = new Color(0.08f, 0.08f, 0.12f, 0.97f);
    static readonly Color colPanel = new Color(0.12f, 0.13f, 0.18f, 1.00f);
    static readonly Color colSlot = new Color(0.18f, 0.19f, 0.25f, 1.00f);
    static readonly Color colSlotHover = new Color(0.28f, 0.30f, 0.40f, 1.00f);
    static readonly Color colSlotEquip = new Color(0.20f, 0.35f, 0.22f, 1.00f);
    static readonly Color colHeader = new Color(0.85f, 0.78f, 0.55f, 1.00f);
    static readonly Color colText = new Color(0.88f, 0.88f, 0.92f, 1.00f);
    static readonly Color colSubText = new Color(0.55f, 0.55f, 0.62f, 1.00f);
    static readonly Color colClose = new Color(0.75f, 0.25f, 0.22f, 1.00f);
    static readonly Color colTooltipBg = new Color(0.06f, 0.06f, 0.10f, 0.96f);

    // ─────────────────────────────────────────────────────────
    // UI OBJELERİ
    // ─────────────────────────────────────────────────────────

    private GameObject rootPanel;
    private List<SlotUI> slotUIs = new List<SlotUI>();

    // Ekipman slotları UI
    private SlotUI headSlotUI;
    private SlotUI chestSlotUI;
    private SlotUI mainHandSlotUI;

    // Tooltip
    private GameObject tooltipPanel;
    private Text tooltipName;
    private Text tooltipDesc;
    private Text tooltipType;

    private bool isOpen = false;
    private Inventory inv;

    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        inv = Inventory.Instance;
        if (inv == null) { Debug.LogError("❌ InventoryUI: Inventory.cs bulunamadı!"); return; }

        BuildUI();
        rootPanel.SetActive(false);

        inv.OnInventoryChanged += RefreshSlots;
        inv.OnEquipmentChanged += RefreshSlots;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();
    }

    void OnDestroy()
    {
        if (inv != null)
        {
            inv.OnInventoryChanged -= RefreshSlots;
            inv.OnEquipmentChanged -= RefreshSlots;
        }
    }

    // ─────────────────────────────────────────────────────────
    // AÇMA / KAPAMA
    // ─────────────────────────────────────────────────────────

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        rootPanel.SetActive(isOpen);
        if (isOpen) RefreshSlots();
        HideTooltip();
        Debug.Log($"🎒 Envanter {(isOpen ? "açıldı" : "kapandı")}");
    }

    // ─────────────────────────────────────────────────────────
    // SLOT GÜNCELLEME
    // ─────────────────────────────────────────────────────────

    void RefreshSlots()
    {
        // Ana envanter slotları
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (i < inv.slots.Count)
                slotUIs[i].SetSlot(inv.slots[i]);
            else
                slotUIs[i].Clear();
        }

        // Ekipman slotları
        headSlotUI?.SetSlot(inv.headSlot);
        chestSlotUI?.SetSlot(inv.chestSlot);
        mainHandSlotUI?.SetSlot(inv.mainHandSlot);
    }

    // ─────────────────────────────────────────────────────────
    // TOOLTIP
    // ─────────────────────────────────────────────────────────

    public void ShowTooltip(ItemData item, Vector3 screenPos)
    {
        if (item == null || tooltipPanel == null) return;
        tooltipPanel.SetActive(true);

        tooltipName.text = item.itemName;
        tooltipDesc.text = item.description;
        tooltipType.text = item.itemType.ToString() +
            (item.equipSlot != EquipSlot.None ? $" — {item.equipSlot}" : "");

        RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
        rt.position = screenPos + new Vector3(10f, -10f, 0f);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────
    // UI BUILDER
    // ─────────────────────────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("InventoryCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Karartma overlay
        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(canvasGO.transform, false);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImg.raycastTarget = true;
        RectTransform overlayRT = overlay.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;

        // Ana panel (ortada)
        rootPanel = new GameObject("InventoryRoot");
        rootPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform rootRT = rootPanel.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0.5f);
        rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = Vector2.zero;
        rootRT.sizeDelta = new Vector2(780f, 520f);

        // Arka plan
        Image rootBg = rootPanel.AddComponent<Image>();
        rootBg.color = colBg;

        // ── BAŞLIK ──
        BuildHeader(rootPanel.transform);

        // ── SOL: Envanter Grid ──
        BuildInventoryGrid(rootPanel.transform);

        // ── SAĞ: Ekipman Paneli ──
        BuildEquipmentPanel(rootPanel.transform);

        // ── TOOLTIP ──
        BuildTooltip(canvasGO.transform);
    }

    void BuildHeader(Transform parent)
    {
        GameObject header = new GameObject("Header");
        header.transform.SetParent(parent, false);
        Image headerBg = header.AddComponent<Image>();
        headerBg.color = colPanel;
        RectTransform hRT = header.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 1f);
        hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = Vector2.zero;
        hRT.sizeDelta = new Vector2(0f, 44f);

        // Başlık yazısı
        Text title = MakeText("🎒  ENVANTER", header.transform, 16, colHeader, TextAnchor.MiddleLeft);
        RectTransform tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(16f, 0f); tRT.offsetMax = Vector2.zero;
        title.fontStyle = FontStyle.Bold;

        // I tuşu ipucu
        Text hint = MakeText("[I] Kapat", header.transform, 11, colSubText, TextAnchor.MiddleRight);
        RectTransform hiRT = hint.GetComponent<RectTransform>();
        hiRT.anchorMin = Vector2.zero; hiRT.anchorMax = Vector2.one;
        hiRT.offsetMin = Vector2.zero; hiRT.offsetMax = new Vector2(-16f, 0f);

        // Kapat butonu
        GameObject closeBtn = new GameObject("CloseBtn");
        closeBtn.transform.SetParent(header.transform, false);
        Image closeBg = closeBtn.AddComponent<Image>();
        closeBg.color = colClose;
        RectTransform cRT = closeBtn.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(1f, 0.5f); cRT.anchorMax = new Vector2(1f, 0.5f);
        cRT.pivot = new Vector2(1f, 0.5f);
        cRT.anchoredPosition = new Vector2(-8f, 0f);
        cRT.sizeDelta = new Vector2(32f, 28f);
        Text closeTxt = MakeText("✕", closeBtn.transform, 14, Color.white, TextAnchor.MiddleCenter);
        Fill(closeTxt.GetComponent<RectTransform>());
        Button closeButton = closeBtn.AddComponent<Button>();
        closeButton.onClick.AddListener(ToggleInventory);
    }

    void BuildInventoryGrid(Transform parent)
    {
        // Sol panel
        GameObject leftPanel = new GameObject("InventoryPanel");
        leftPanel.transform.SetParent(parent, false);
        Image lpBg = leftPanel.AddComponent<Image>();
        lpBg.color = colPanel;
        RectTransform lpRT = leftPanel.GetComponent<RectTransform>();
        lpRT.anchorMin = new Vector2(0f, 0f);
        lpRT.anchorMax = new Vector2(0f, 1f);
        lpRT.pivot = new Vector2(0f, 0.5f);
        lpRT.anchoredPosition = new Vector2(8f, -22f);
        lpRT.sizeDelta = new Vector2(440f, -52f);

        // "Çanta" label
        Text label = MakeText("ÇANTA", leftPanel.transform, 11, colSubText, TextAnchor.UpperLeft);
        RectTransform lRT = label.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 1f); lRT.anchorMax = new Vector2(1f, 1f);
        lRT.pivot = new Vector2(0f, 1f);
        lRT.anchoredPosition = new Vector2(10f, -8f);
        lRT.sizeDelta = new Vector2(-10f, 20f);
        label.fontStyle = FontStyle.Bold;

        // Grid container
        GameObject grid = new GameObject("Grid");
        grid.transform.SetParent(leftPanel.transform, false);
        RectTransform gridRT = grid.AddComponent<RectTransform>();
        gridRT.anchorMin = Vector2.zero; gridRT.anchorMax = Vector2.one;
        gridRT.offsetMin = new Vector2(8f, 8f);
        gridRT.offsetMax = new Vector2(-8f, -32f);

        GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(64f, 64f);
        glg.spacing = new Vector2(6f, 6f);
        glg.padding = new RectOffset(4, 4, 4, 4);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 6;

        // Slotları oluştur
        slotUIs.Clear();
        for (int i = 0; i < inv.slotCount; i++)
        {
            SlotUI slot = BuildSlot(grid.transform, false);
            int idx = i;
            slot.SetClickAction(() => OnSlotClicked(idx));
            slot.SetHoverActions(
                () => { if (!inv.slots[idx].IsEmpty) ShowTooltip(inv.slots[idx].item, slot.GetScreenPos()); },
                HideTooltip
            );
            slotUIs.Add(slot);
        }
    }

    void BuildEquipmentPanel(Transform parent)
    {
        // Sağ panel
        GameObject rightPanel = new GameObject("EquipmentPanel");
        rightPanel.transform.SetParent(parent, false);
        Image rpBg = rightPanel.AddComponent<Image>();
        rpBg.color = colPanel;
        RectTransform rpRT = rightPanel.GetComponent<RectTransform>();
        rpRT.anchorMin = new Vector2(1f, 0f);
        rpRT.anchorMax = new Vector2(1f, 1f);
        rpRT.pivot = new Vector2(1f, 0.5f);
        rpRT.anchoredPosition = new Vector2(-8f, -22f);
        rpRT.sizeDelta = new Vector2(316f, -52f);

        // "Ekipman" label
        Text label = MakeText("EKİPMAN", rightPanel.transform, 11, colSubText, TextAnchor.UpperLeft);
        RectTransform lRT = label.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 1f); lRT.anchorMax = new Vector2(1f, 1f);
        lRT.pivot = new Vector2(0f, 1f);
        lRT.anchoredPosition = new Vector2(10f, -8f);
        lRT.sizeDelta = new Vector2(-10f, 20f);
        label.fontStyle = FontStyle.Bold;

        // Ekipman slotları — dikey sıra
        float startY = -48f;
        float gap = 90f;

        headSlotUI = BuildEquipSlot(rightPanel.transform, "👑  Baş", startY, EquipSlot.Head);
        chestSlotUI = BuildEquipSlot(rightPanel.transform, "🛡  Göğüs", startY - gap, EquipSlot.Chest);
        mainHandSlotUI = BuildEquipSlot(rightPanel.transform, "⚔  El", startY - gap * 2, EquipSlot.MainHand);
    }

    SlotUI BuildEquipSlot(Transform parent, string label, float yPos, EquipSlot equipSlot)
    {
        // Satır container
        GameObject row = new GameObject($"EquipRow_{equipSlot}");
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, yPos);
        rowRT.sizeDelta = new Vector2(-16f, 78f);

        // Label
        Text lbl = MakeText(label, row.transform, 11, colSubText, TextAnchor.UpperLeft);
        RectTransform lblRT = lbl.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 1f); lblRT.anchorMax = new Vector2(1f, 1f);
        lblRT.pivot = new Vector2(0f, 1f);
        lblRT.anchoredPosition = new Vector2(0f, 0f);
        lblRT.sizeDelta = new Vector2(0f, 18f);

        // Slot
        SlotUI slot = BuildSlot(row.transform, true);
        RectTransform slotRT = slot.GetRectTransform();
        slotRT.anchorMin = new Vector2(0f, 0f);
        slotRT.anchorMax = new Vector2(0f, 0f);
        slotRT.pivot = new Vector2(0f, 0f);
        slotRT.anchoredPosition = new Vector2(0f, 0f);
        slotRT.sizeDelta = new Vector2(64f, 54f);

        // "Boş" metni
        Text emptyTxt = MakeText("Boş", row.transform, 10, colSubText, TextAnchor.MiddleLeft);
        RectTransform etRT = emptyTxt.GetComponent<RectTransform>();
        etRT.anchorMin = new Vector2(0f, 0f); etRT.anchorMax = new Vector2(1f, 1f);
        etRT.offsetMin = new Vector2(74f, 0f); etRT.offsetMax = Vector2.zero;

        EquipSlot capturedSlot = equipSlot;
        slot.SetClickAction(() => OnEquipSlotClicked(capturedSlot));
        slot.SetHoverActions(
            () =>
            {
                InventorySlot s = GetEquipSlotData(capturedSlot);
                if (s != null && !s.IsEmpty) ShowTooltip(s.item, slot.GetScreenPos());
            },
            HideTooltip
        );

        return slot;
    }

    SlotUI BuildSlot(Transform parent, bool isEquip)
    {
        GameObject go = new GameObject("Slot");
        go.transform.SetParent(parent, false);

        Image bg = go.AddComponent<Image>();
        bg.color = isEquip ? colSlotEquip : colSlot;

        // İkon
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.color = Color.white;
        iconImg.enabled = false;
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.2f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

        // Miktar
        Text amountTxt = MakeText("", go.transform, 10, Color.white, TextAnchor.LowerRight);
        amountTxt.fontStyle = FontStyle.Bold;
        RectTransform amtRT = amountTxt.GetComponent<RectTransform>();
        amtRT.anchorMin = Vector2.zero; amtRT.anchorMax = Vector2.one;
        amtRT.offsetMin = new Vector2(2f, 2f); amtRT.offsetMax = new Vector2(-2f, -2f);

        SlotUI slotUI = new SlotUI(go, bg, iconImg, amountTxt, isEquip ? colSlotEquip : colSlot);
        return slotUI;
    }

    void BuildTooltip(Transform parent)
    {
        tooltipPanel = new GameObject("Tooltip");
        tooltipPanel.transform.SetParent(parent, false);
        Image tbg = tooltipPanel.AddComponent<Image>();
        tbg.color = colTooltipBg;
        RectTransform tRT = tooltipPanel.GetComponent<RectTransform>();
        tRT.sizeDelta = new Vector2(200f, 80f);
        tRT.pivot = new Vector2(0f, 1f);

        tooltipName = MakeText("", tooltipPanel.transform, 13, colHeader, TextAnchor.UpperLeft);
        tooltipName.fontStyle = FontStyle.Bold;
        RectTransform tnRT = tooltipName.GetComponent<RectTransform>();
        tnRT.anchorMin = new Vector2(0f, 1f); tnRT.anchorMax = new Vector2(1f, 1f);
        tnRT.pivot = new Vector2(0f, 1f);
        tnRT.anchoredPosition = new Vector2(8f, -8f);
        tnRT.sizeDelta = new Vector2(-16f, 20f);

        tooltipType = MakeText("", tooltipPanel.transform, 10, colSubText, TextAnchor.UpperLeft);
        RectTransform ttRT = tooltipType.GetComponent<RectTransform>();
        ttRT.anchorMin = new Vector2(0f, 1f); ttRT.anchorMax = new Vector2(1f, 1f);
        ttRT.pivot = new Vector2(0f, 1f);
        ttRT.anchoredPosition = new Vector2(8f, -28f);
        ttRT.sizeDelta = new Vector2(-16f, 16f);

        tooltipDesc = MakeText("", tooltipPanel.transform, 10, colText, TextAnchor.UpperLeft);
        tooltipDesc.lineSpacing = 1.2f;
        RectTransform tdRT = tooltipDesc.GetComponent<RectTransform>();
        tdRT.anchorMin = new Vector2(0f, 1f); tdRT.anchorMax = new Vector2(1f, 1f);
        tdRT.pivot = new Vector2(0f, 1f);
        tdRT.anchoredPosition = new Vector2(8f, -46f);
        tdRT.sizeDelta = new Vector2(-16f, 30f);

        tooltipPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────
    // TIKLAMA OLAYLARI
    // ─────────────────────────────────────────────────────────

    void OnSlotClicked(int index)
    {
        if (index >= inv.slots.Count) return;
        InventorySlot slot = inv.slots[index];
        if (slot.IsEmpty) return;

        // Ekipman ise giy, değilse kullan
        if (slot.item.equipSlot != EquipSlot.None)
        {
            inv.EquipItem(slot.item);
            Debug.Log($"⚔️ {slot.item.itemName} kuşanıldı!");
        }
        else
        {
            Debug.Log($"🖱 {slot.item.itemName} kullanıldı (henüz aksiyon yok)");
        }
        HideTooltip();
    }

    void OnEquipSlotClicked(EquipSlot slot)
    {
        inv.UnequipSlot(slot);
        HideTooltip();
    }

    InventorySlot GetEquipSlotData(EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.Head: return inv.headSlot;
            case EquipSlot.Chest: return inv.chestSlot;
            case EquipSlot.MainHand: return inv.mainHandSlot;
            default: return null;
        }
    }

    // ─────────────────────────────────────────────────────────
    // YARDIMCI
    // ─────────────────────────────────────────────────────────

    Text MakeText(string content, Transform parent, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.raycastTarget = false;
        return t;
    }

    void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}

// ─────────────────────────────────────────────────────────
// SLOT UI YARDIMCI SINIFI
// ─────────────────────────────────────────────────────────

public class SlotUI
{
    private GameObject go;
    private Image bg;
    private Image icon;
    private Text amount;
    private Color baseColor;

    public SlotUI(GameObject go, Image bg, Image icon, Text amount, Color baseColor)
    {
        this.go = go; this.bg = bg; this.icon = icon;
        this.amount = amount; this.baseColor = baseColor;
    }

    public void SetSlot(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty) { Clear(); return; }

        icon.enabled = slot.item.icon != null;
        if (slot.item.icon != null) icon.sprite = slot.item.icon;

        amount.text = slot.item.isStackable && slot.amount > 1 ? slot.amount.ToString() : "";
        bg.color = baseColor;
    }

    public void Clear()
    {
        icon.enabled = false;
        amount.text = "";
        bg.color = baseColor;
    }

    public void SetClickAction(System.Action action)
    {
        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => action());

        ColorBlock cb = btn.colors;
        cb.normalColor = baseColor;
        cb.highlightedColor = new Color(0.28f, 0.30f, 0.40f, 1f);
        cb.pressedColor = new Color(0.35f, 0.38f, 0.50f, 1f);
        btn.colors = cb;
        btn.targetGraphic = bg;
    }

    public void SetHoverActions(System.Action onEnter, System.Action onExit)
    {
        SlotHover hover = go.GetComponent<SlotHover>();
        if (hover == null) hover = go.AddComponent<SlotHover>();
        hover.onEnter = onEnter;
        hover.onExit = onExit;
    }

    public Vector3 GetScreenPos() => go.transform.position;
    public RectTransform GetRectTransform() => go.GetComponent<RectTransform>();
}