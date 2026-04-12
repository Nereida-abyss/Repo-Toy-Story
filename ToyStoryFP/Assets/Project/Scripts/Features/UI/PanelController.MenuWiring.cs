using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class PanelController
{
    public void OpenCreditsFromButton()
    {
        if (isSequenceRunning)
        {
            return;
        }

        StartSequence(PlayCreditsSequence(allowSkip: true));
    }

    private void StartSequence(IEnumerator routine)
    {
        if (routine == null || isSequenceRunning)
        {
            return;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "No se pudo iniciar la secuencia porque el objeto Controlador esta inactivo o deshabilitado.",
                this);
            return;
        }

        activeSequence = StartCoroutine(RunManagedSequence(routine));
    }

    private IEnumerator RunManagedSequence(IEnumerator routine)
    {
        isSequenceRunning = true;
        yield return routine;
        isSequenceRunning = false;
        activeSequence = null;
    }

    private void BindListeners()
    {
        if (listenersBound)
        {
            return;
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveListener(OpenCreditsFromButton);
            creditsButton.onClick.AddListener(OpenCreditsFromButton);
            listenersBound = true;
            return;
        }

        GameDebug.Advertencia(
            "EndMenu",
            "No se encontrÃ³ CreditsButton. Asigna la referencia en Inspector o mantÃ©n el nombre 'CreditsButton'.",
            this);
    }

    private void UnbindListeners()
    {
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveListener(OpenCreditsFromButton);
        }

        listenersBound = false;
    }

    private void EnsureScorePanelController()
    {
        if (scorePanelController == null)
        {
            ConfigureScorePanelController();
            return;
        }

        if (scorePanelController == null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "No se encontrÃ³ ScorePanelController en el controlador. AÃ±Ã¡delo en la escena EndMenu.",
                this);
            return;
        }

        scorePanelController.ConfigureIfNeeded(panelButtons, panelScore);
    }

    private void ConfigureScorePanelController()
    {
        if (scorePanelController == null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "PanelController necesita una referencia explicita a ScorePanelController en el inspector.",
                this);
            return;
        }

        scorePanelController.ConfigureIfNeeded(panelButtons, panelScore);
    }

    private Transform ResolveCreditsTextRoot()
    {
        if (creditsTextRoot != null)
        {
            return creditsTextRoot;
        }

        if (panelCredits != null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "PanelController necesita una referencia explicita a creditsTextRoot en el inspector.",
                this);
            return panelCredits.transform;
        }

        return null;
    }

    private void ValidateReferences()
    {
        ValidatePanelReference(panelGameOver, "panelGameOver");
        ValidatePanelReference(panelCredits, "panelCredits");
        ValidatePanelReference(panelButtons, "panelButtons");
        ValidatePanelReference(panelSetting, "panelSetting");
        ValidatePanelReference(panelScore, "panelScore");

        if (panelGameOver != null)
        {
            RequireCanvasGroup(panelGameOver, panelGameOver.name);
        }

        if (panelCredits != null)
        {
            RequireCanvasGroup(panelCredits, panelCredits.name);
        }

        if (panelButtons != null)
        {
            RequireCanvasGroup(panelButtons, panelButtons.name);
        }

        if (creditsTextRoot == null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "PanelController necesita creditsTextRoot asignado desde el inspector.",
                this);
        }

        if (creditsAudioSource == null)
        {
            GameDebug.Advertencia(
                "EndMenu",
                "PanelController necesita creditsAudioSource asignado desde el inspector.",
                this);
        }
    }

    private void ValidatePanelReference(GameObject panel, string fieldName)
    {
        if (panel != null)
        {
            return;
        }

        GameDebug.Advertencia(
            "EndMenu",
            $"PanelController no tiene la referencia '{fieldName}' asignada en el inspector.",
            this);
    }
}
