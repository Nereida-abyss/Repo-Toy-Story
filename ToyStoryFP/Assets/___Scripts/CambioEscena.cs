using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    
    public void StartGame()
    {
        SceneManager.LoadScene("Gameplay");
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1f;
    }

    public void RestartGamePlay()
    {
        string escenaActual = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(escenaActual);
        Time.timeScale = 1f;
    }

    public void NextScene()
    {
        int siguiente = SceneManager.GetActiveScene().buildIndex + 1;
        if (siguiente < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(siguiente);
        }
    }

    public void EndGame()
    {
        SceneManager.LoadScene("EndMenu");
    }

    public void ExitGame()
    {
        Debug.Log("Has cerrado el juego");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
