using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ItemPickup.cs → Yerdeki item prefabına ekle
///
/// PREFAB KURULUMU:
/// 1. 3D model oluştur (küçük bir silah / kutu vb.)
/// 2. Bu scripti ekle
/// 3. itemData alanına ItemData asset'ini sürükle
/// 4. Sphere Collider ekle → Is Trigger: AÇIK, Radius: 1.5
/// 5. Prefab yap → Project'e sürükle
///
/// ÇALIŞMA:
/// - Player yaklaşınca "[F] Topla" yazısı çıkar
/// - F tuşuna basınca Inventory'ye eklenir, obje yok olur
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("Item")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Ayarlar")]
    public float pickupRadius = 1.8f;
    public float bobSpeed = 1.5f;
    public float bobHeight = 0.15f;
    public float rotateSpeed = 60f;

    // UI
    private GameObject promptCanvas;
    private bool playerNearby = false;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        BuildPromptUI();
    }

    void Update()
    {
        // Zıplama + dönme animasyonu
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // Toplama
        if (playerNearby && Input.GetKeyDown(KeyCode.F))
            TryPickup();

        // Prompt pozisyonunu güncelle
        if (promptCanvas != null)
            promptCanvas.transform.position = transform.position + Vector3.up * 0.8f;
    }

    void TryPickup()
    {
        if (itemData == null) return;

        // Inventory yoksa direkt ekle diyemeyiz
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("⚠️ Inventory bulunamadı!");
            return;
        }

        if (Inventory.Instance.AddItem(itemData, amount))
        {
            Debug.Log($"✅ TOPLANDI: {itemData.itemName} x{amount}");
            Destroy(promptCanvas);
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("❌ Envanter dolu!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = true;
        if (promptCanvas != null) promptCanvas.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = false;
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    void BuildPromptUI()
    {
        // World Space Canvas
        promptCanvas = new GameObject("PickupPrompt");
        promptCanvas.transform.SetParent(null);

        Canvas canvas = promptCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        CanvasScaler cs = promptCanvas.AddComponent<CanvasScaler>();
        cs.dynamicPixelsPerUnit = 10f;

        RectTransform cRT = promptCanvas.GetComponent<RectTransform>();
        cRT.sizeDelta = new Vector2(2f, 0.5f);
        promptCanvas.transform.localScale = Vector3.one * 0.02f;

        // Arka plan
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(promptCanvas.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.75f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Metin
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(promptCanvas.transform, false);
        Text txt = textGO.AddComponent<Text>();
        txt.text = itemData != null ? $"[F]  {itemData.itemName}" : "[F]  Topla";
        txt.fontSize = 18;
        txt.color = new Color(1f, 0.9f, 0.5f, 1f);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontStyle = FontStyle.Bold;
        RectTransform tRT = textGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(5f, 2f); tRT.offsetMax = new Vector2(-5f, -2f);

        promptCanvas.SetActive(false);

        // Kameraya bak (billboard)
        StartCoroutine(FaceCamera());
    }

    System.Collections.IEnumerator FaceCamera()
    {
        Camera cam = Camera.main;
        while (promptCanvas != null)
        {
            if (cam != null)
                promptCanvas.transform.rotation = cam.transform.rotation;
            yield return null;
        }
    }
}