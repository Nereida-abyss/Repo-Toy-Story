using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Obsolete("Use PanelController as the single EndMenu flow authority.")]
public class EndMenuUI : MonoBehaviour
{
    [Header("Paneles (solo compatibilidad)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject buttonsPanel;
    [SerializeField] private PanelController panelController;

    [Header("Configuracion")]
    [SerializeField] private float creditsDisplayTime = 4f;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start()
    {
        if (panelController != null && panelController.isActiveAndEnabled)
        {
            enabled = false;
            return;
        }

        StartCoroutine(EndMenuSequence());
    }

    private IEnumerator EndMenuSequence()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }

        if (buttonsPanel != null)
        {
            buttonsPanel.SetActive(false);
        }

        yield return new WaitForSecondsRealtime(2f);

        yield return FadeOut(gameOverPanel);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }

        yield return FadeIn(creditsPanel);
        yield return new WaitForSecondsRealtime(creditsDisplayTime);
        yield return FadeOut(creditsPanel);

        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }

        if (buttonsPanel != null)
        {
            buttonsPanel.SetActive(true);
        }

        yield return FadeIn(buttonsPanel);
    }

    private IEnumerator FadeOut(GameObject target)
    {
        if (!TryGetCanvasGroup(target, out CanvasGroup group))
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        group.alpha = 0f;
    }

    private IEnumerator FadeIn(GameObject target)
    {
        if (!TryGetCanvasGroup(target, out CanvasGroup group))
        {
            yield break;
        }

        group.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = elapsed / fadeDuration;
            yield return null;
        }

        group.alpha = 1f;
    }

    private bool TryGetCanvasGroup(GameObject target, out CanvasGroup canvasGroup)
    {
        canvasGroup = null;

        if (target == null)
        {
            return false;
        }

        canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            return true;
        }

        GameDebug.Advertencia(
            "EndMenu",
            $"El panel '{target.name}' necesita un CanvasGroup explicito para que EndMenuUI pueda hacer fade.",
            this);
        return false;
    }
}
