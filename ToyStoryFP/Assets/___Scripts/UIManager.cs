using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelPause;
    public GameObject panelUI;

    public void AbrirPausa()
    {
        panelPause.SetActive(true);
        panelUI.SetActive(false);
        Time.timeScale = 0f;
    }

    public void CerrarPausa()
    {
        panelPause.SetActive(false);
        panelUI.SetActive(true);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(panelPause.activeSelf)
            {
                panelPause.SetActive(false);
                panelUI.SetActive(true);
                Time.timeScale = 1f;
            }

            else 
            {
                panelPause.SetActive(true);
                panelUI.SetActive(false);
                Time.timeScale = 0f;
            }
        }
    }
}