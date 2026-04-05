using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    private const string GamePlaySceneName = "GamePlay";

    public void StartGame()
    {
        LoadSceneWithFade(GamePlaySceneName);
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1f;
    }

    public void RestartGamePlay()
    {
        Time.timeScale = 1f;
        string escenaActual = SceneManager.GetActiveScene().name;
        LoadSceneWithFade(escenaActual);
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

    private void LoadSceneWithFade(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        if (!SceneTransitionFade.TryFadeOutAndLoadScene(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
