using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    
    public void StartGame()
    {
        SceneManager.LoadScene("Gameplay");
    }

    public void ExitGame()
    {
         Application.Quit();
    }
}
