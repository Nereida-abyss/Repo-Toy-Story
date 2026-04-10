using UnityEngine;

public static class UIFxUtility
{
    // Actualiza panel activo.
    public static void SetPanelActive(GameObject panel, bool isActive, bool animated = true)
    {
        if (panel == null)
        {
            return;
        }

        if (panel.activeSelf == isActive)
        {
            return;
        }

        if (!animated)
        {
            panel.SetActive(isActive);
            return;
        }

        UIPanelFx panelFx = panel.GetComponent<UIPanelFx>();

        if (panelFx == null)
        {
            panel.SetActive(isActive);
            return;
        }

        if (isActive)
        {
            panelFx.Show();
        }
        else
        {
            panelFx.Hide();
        }
    }

    // Oculta inmediato.
    public static void HideImmediate(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}
