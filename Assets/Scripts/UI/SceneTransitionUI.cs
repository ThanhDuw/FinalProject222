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
///   1. Assign _fadePanel (CanvasGroup on the FadePanel Image)
///   2. Parent Canvas: Screen Space - Overlay, Sort Order 999
///
/// Dependency flow:
///   TravelManager -> SceneTransitionUI.FadeOut() -> SceneManager.LoadScene()
///   TravelManager.RestoreAndNotify() -> SceneTransitionUI.FadeIn()
/// </summary>
public class SceneTransitionUI : MonoBehaviour
{
    // -- Singleton ------------------------------------------------------------

    public static SceneTransitionUI Instance { get; private set; }

    // -- Inspector ------------------------------------------------------------

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

        if (_fadePanel != null)
        {
            _fadePanel.alpha          = 0f;
            _fadePanel.blocksRaycasts = false;
            _fadePanel.interactable   = false;
        }
    }

    // -- Public API -----------------------------------------------------------

    /// <summary>
    /// Fades to black then invokes onComplete.
    /// Call before SceneManager.LoadScene().
    /// </summary>
    public void FadeOut(Action onComplete)
    {
        StartCoroutine(DoFade(0f, 1f, onComplete));
    }

    /// <summary>
    /// Fades back to clear.
    /// Call after the new scene is fully restored and ready.
    /// </summary>
    public void FadeIn()
    {
        StartCoroutine(DoFade(1f, 0f, null));
    }

    // -- Internal -------------------------------------------------------------

    private IEnumerator DoFade(float fromAlpha, float toAlpha, Action onComplete)
    {
        if (_fadePanel == null)
        {
            Debug.LogWarning("[SceneTransitionUI] _fadePanel not assigned in Inspector.");
            onComplete?.Invoke();
            yield break;
        }

        _fadePanel.blocksRaycasts = (toAlpha > 0f);
        _fadePanel.interactable   = false;

        float elapsed    = 0f;
        _fadePanel.alpha = fromAlpha;

        while (elapsed < _fadeDuration)
        {
            elapsed         += Time.deltaTime;
            _fadePanel.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / _fadeDuration);
            yield return null;
        }

        _fadePanel.alpha = toAlpha;

        if (toAlpha <= 0f)
            _fadePanel.blocksRaycasts = false;

        onComplete?.Invoke();
    }
}
