using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelPausa;
    public GameObject panelVictoria;
    public GameObject panelDerrota;

    [Header("Referencia")]

    public CambioEscena cambioEscena;

    public void AbrirPausa()
    {
        panelPausa.SetActive(true);
        Time.timeScale = 0f;
    }

    public void MostrarVictoria()
    {
        panelVictoria.SetActive(true);
        Time.timeScale = 0f;
    }

    public void MostrarDerrota()
    {
        panelDerrota.SetActive(true);
        Time.timeScale = 0f;
    }



}