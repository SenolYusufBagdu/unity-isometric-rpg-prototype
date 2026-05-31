using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class DeathDissolve : MonoBehaviour
{
    [Header("Bileşenler")]
    public Renderer[] targetRenderers;
    public VisualEffect vfxGraph;

    [Header("Ayarlar")]
    public float dissolveTime = 2f;
    public float destroyDelay = 0.5f;
    public float startValue = 0f;
    public float endValue = 1f;

    [Header("Shader Parametre Adı")]
    public string shaderParamName = "Particle Edge"; // Boşluklu yaz

    private Material[] instanceMaterials;
    private bool isDissolving = false;

    void Awake()
    {
        // Renderer'ları otomatik bul
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        // Her renderer için instance material oluştur
        instanceMaterials = new Material[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
            {
                instanceMaterials[i] = targetRenderers[i].material;
                instanceMaterials[i].SetFloat(shaderParamName, startValue);
            }
        }

        // VFX başlangıçta durdur
        if (vfxGraph == null)
            vfxGraph = GetComponentInChildren<VisualEffect>();

        if (vfxGraph != null)
        {
            vfxGraph.SetFloat(shaderParamName, startValue);
            vfxGraph.Stop();
        }
    }

    public void StartDissolve()
    {
        if (isDissolving) return;
        isDissolving = true;
        StartCoroutine(DissolveCoroutine());
    }

    IEnumerator DissolveCoroutine()
    {
        // VFX'i başlat
        if (vfxGraph != null)
            vfxGraph.Play();

        float elapsed = 0f;

        while (elapsed < dissolveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dissolveTime);
            float smoothT = t * t * (3f - 2f * t);
            float currentValue = Mathf.Lerp(startValue, endValue, smoothT);

            SetEdgeValue(currentValue);

            yield return null;
        }

        SetEdgeValue(endValue);

        if (vfxGraph != null)
            vfxGraph.Stop();

        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    void SetEdgeValue(float value)
    {
        foreach (var mat in instanceMaterials)
        {
            if (mat != null)
                mat.SetFloat(shaderParamName, value);
        }

        if (vfxGraph != null)
            vfxGraph.SetFloat(shaderParamName, value);
    }
}