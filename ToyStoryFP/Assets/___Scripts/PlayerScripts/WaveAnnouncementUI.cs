using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WaveAnnouncementUI : MonoBehaviour
{
    [SerializeField] private float visibleDuration = 2.5f;

    private GameObject panelRoot;
    private TMP_Text announcementText;
    private Graphic[] graphics;
    private Coroutine hideRoutine;

    void Awake()
    {
        ResolveReferences();
        HideImmediate();
    }

    void OnValidate()
    {
        ResolveReferences();
    }

    public void ShowWave(int waveNumber)
    {
        ResolveReferences();

        if (panelRoot == null || announcementText == null)
        {
            Debug.LogWarning("WaveAnnouncementUI is missing its panel or text reference.", this);
            return;
        }

        announcementText.text = $"OLEADA {waveNumber}";
        SetVisible(true);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, visibleDuration));
        HideImmediate();
        hideRoutine = null;
    }

    private void HideImmediate()
    {
        SetVisible(false);
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (announcementText == null)
        {
            announcementText = GetComponentInChildren<TMP_Text>(true);
        }

        if (graphics == null || graphics.Length == 0)
        {
            graphics = GetComponentsInChildren<Graphic>(true);
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (graphics == null || graphics.Length == 0)
        {
            return;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].enabled = isVisible;
            }
        }
    }
}
