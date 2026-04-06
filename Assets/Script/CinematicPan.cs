using UnityEngine;

public class CinematicPan : MonoBehaviour
{
    [Header("Sinematik Hareket Ayarlarý")]
    [Tooltip("Kameranýn yavaþça ileri kayma hýzý")]
    public float ileriGitmeHizi = 0.5f;

    [Tooltip("Kameranýn manzarayý süzmesi için saða dönme hýzý")]
    public float donmeHizi = 1.5f;

    void Update()
    {
        // Kamerayý yumuþakça ileri doðru kaydýr (Dolly etkisi)
        transform.Translate(Vector3.forward * ileriGitmeHizi * Time.deltaTime);

        // Kamerayý yavaþça saða doðru çevir (Pan etkisi)
        // Düzgün bir ufuk çizgisi dönüþü için Space.World kullanýyoruz
        transform.Rotate(Vector3.up, donmeHizi * Time.deltaTime, Space.World);
    }
}