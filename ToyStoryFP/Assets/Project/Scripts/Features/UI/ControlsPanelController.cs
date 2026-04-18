using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ControlsPanelController : MonoBehaviour
{
    private const string ControlsInputGateOwner = "ControlsPanelController.Panel";

    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject previousPanelToHide;

    private bool hasAcquiredInputGate;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
        else
        {
            GameDebug.Advertencia("UI", "ControlsPanelController necesita closeButton asignado.", this);
        }
    }

    private void OnDisable()
    {
        ReleaseInputGateIfNeeded();
    }

    private void Update()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (ProjectInput.WasUiBackPressed())
        {
            ClosePanel();
        }
    }

    public void OpenPanel()
    {
        AcquireInputGateIfNeeded();

        if (previousPanelToHide != null)
        {
            UIFxUtility.SetPanelActive(previousPanelToHide, false);
        }

        UIFxUtility.SetPanelActive(gameObject, true);
    }

    public void ClosePanel()
    {
        ReleaseInputGateIfNeeded();
        UIFxUtility.SetPanelActive(gameObject, false);

        if (previousPanelToHide != null)
        {
            UIFxUtility.SetPanelActive(previousPanelToHide, true);
        }
    }

    private void AcquireInputGateIfNeeded()
    {
        if (hasAcquiredInputGate)
        {
            return;
        }

        GameplayInputGate.Acquire(ControlsInputGateOwner);
        hasAcquiredInputGate = true;
    }

    private void ReleaseInputGateIfNeeded()
    {
        if (!hasAcquiredInputGate)
        {
            return;
        }

        GameplayInputGate.Release(ControlsInputGateOwner);
        hasAcquiredInputGate = false;
    }
}
