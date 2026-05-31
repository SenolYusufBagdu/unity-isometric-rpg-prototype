using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Sahne Ayarlarý")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Müzik Ayarlarý")]
    [SerializeField] private AudioSource menuMusic;
    [SerializeField] private Sprite iconMuted;    // Ses kapalý ikonu
    [SerializeField] private Sprite iconUnmuted;  // Ses açýk ikonu

    [Header("UI")]
    [SerializeField] private Image muteButtonImage; // Butonun Image component'i

    private bool isMuted = false;

    private void Start()
    {
        // Oyuncu daha önce sesi kapatmýþsa hatýrla
        isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        ApplyMute();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMute();
    }

    private void ApplyMute()
    {
        if (menuMusic != null)
            menuMusic.mute = isMuted;

        // Ýkonu güncelle
        if (muteButtonImage != null)
            muteButtonImage.sprite = isMuted ? iconMuted : iconUnmuted;
    }
}