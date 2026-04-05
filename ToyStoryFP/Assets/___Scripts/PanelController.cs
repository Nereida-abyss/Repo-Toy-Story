using UnityEngine;
using System.Collections;

public class PanelController : MonoBehaviour
{
    public GameObject panelGameOver;
    public GameObject panelCredits;
    public GameObject panelButtons;
    public GameObject panelSetting;

    IEnumerator Start()
    {
        panelGameOver.SetActive(true);
        yield return new WaitForSeconds(3f);
        panelGameOver.SetActive(false);

        panelCredits.SetActive(true);
        yield return new WaitForSeconds(3f);
        panelCredits.SetActive(false);

        panelButtons.SetActive(true);
    }
}
