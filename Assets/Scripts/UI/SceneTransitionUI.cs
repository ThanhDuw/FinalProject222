using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SceneTransitionUI -- UI Layer (Singleton, DontDestroyOnLoad)
///
/// Handles full-screen fade transitions when switching scenes.
/// Attach to the TravelManager GameObject alongside TravelManager.
///
/// Setup in Inspector:
///   1. Assign _fadePanel (an Image component covering full screen)
///   2. The Image's GameObject must also have a CanvasGroup component
///   3. The parent Canvas must be Screen Space - Overlay, Sort Order 999
///
/// Dependency flow:
///   TravelManager -> SceneTransitionUI.FadeOut() -> SceneManager.LoadScene()
///   SceneManager.sceneLoaded -> SceneTransitionUI auto FadeIn
/// </summary>
public class SceneTransitionUI : MonoBehaviour
{
    // -- Singleton ------------------------------------------------------------

    public static SceneTransitionUI Instance { get; private set; }

    // -- Inspector References -------------------------------------------------

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup _fadePanel;
    [SerializeField] private float _fadeDuration = 0.5f;

    // -- Lifecycle ------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure panel starts invisible and does not block input
        if (_fadePanel != null)
        {
            _fadePanel.alpha          = 0f;
            _fadePanel.blocksRaycasts = false;
            _fadePanel.interactable   = false;
        }
    }

    // -- Public API -----------------------------------------------------------

    /// <summary>
    /// Fades the screen to black, then invokes onComplete.
    /// TravelManager calls this before SceneManager.LoadScene().
    /// </summary>
    public void FadeOut(Action onComplete)
    {
        StartCoroutine(DoFade(0f, 1f, onComplete));
    }

    /// <summary>
    /// Fades the screen back to clear.
    /// Called automatically by TravelManager inside RestoreAndNotify()
    /// after inventory, equipment, and health have been restored.
    /// </summary>
    public void FadeIn()
    {
        StartCoroutine(DoFade(1f, 0f, null));
    }

    // -- Private --------------------------------------------------------------

    private IEnumerator DoFade(float fromAlpha, float toAlpha, Action onComplete)
    {
        if (_fadePanel == null)
        {
            Debug.LogWarning("[SceneTransitionUI] _fadePanel is not assigned in the Inspector.");
            onComplete?.Invoke();
            yield break;
        }

        // Block raycasts while fading in (screen going dark)
        _fadePanel.blocksRaycasts = (toAlpha > 0f);
        _fadePanel.interactable   = false;

        float elapsed = 0f;
        _fadePanel.alpha = fromAlpha;

        while (elapsed < _fadeDuration)
        {
            elapsed         += Time.deltaTime;
            _fadePanel.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / _fadeDuration);
            yield return null;
        }

        _fadePanel.alpha = toAlpha;

        // When fully faded in (alpha = 0), allow input again
        if (toAlpha <= 0f)
        {
            _fadePanel.blocksRaycasts = false;
        }

        onComplete?.Invoke();
    }
}
