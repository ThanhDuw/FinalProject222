using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScreenModeController — Handles FullScreen/Window toggle in OptionPanel_UI.
/// Persists display mode across sessions via PlayerPrefs.
///
/// Setup:
///   1. Attach to OptionPanel_UI
///   2. fullScreenButton → FullScreen_Button
///   3. windowButton     → Window_Button
/// </summary>
public class ScreenModeController : MonoBehaviour
{
    private const string KeyFullscreen = "display_fullscreen";

    [Header("Display Mode Buttons")]
    [SerializeField] private Button fullScreenButton;
    [SerializeField] private Button windowButton;

    private void Start()
    {
        // Restore persisted display mode
        bool isFullscreen = PlayerPrefs.GetInt(KeyFullscreen, 1) == 1;
        ApplyDisplayMode(isFullscreen);

        if (fullScreenButton != null)
            fullScreenButton.onClick.AddListener(SetFullScreen);
        if (windowButton != null)
            windowButton.onClick.AddListener(SetWindowed);
    }

    private void OnDestroy()
    {
        if (fullScreenButton != null) fullScreenButton.onClick.RemoveListener(SetFullScreen);
        if (windowButton     != null) windowButton.onClick.RemoveListener(SetWindowed);
    }

    public void SetFullScreen()
    {
        ApplyDisplayMode(true);
        PlayerPrefs.SetInt(KeyFullscreen, 1);
        PlayerPrefs.Save();
        Debug.Log("[ScreenModeController] Switched to FullScreen.");
    }

    public void SetWindowed()
    {
        ApplyDisplayMode(false);
        PlayerPrefs.SetInt(KeyFullscreen, 0);
        PlayerPrefs.Save();
        Debug.Log("[ScreenModeController] Switched to Windowed.");
    }

    private void ApplyDisplayMode(bool fullscreen)
    {
        Screen.fullScreenMode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
        Screen.fullScreen = fullscreen;
    }
}
