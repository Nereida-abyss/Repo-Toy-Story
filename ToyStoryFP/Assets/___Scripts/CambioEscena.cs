using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string GamePlaySceneName = "GamePlay";
    private const string EndMenuSceneName = "EndMenu";

    public void StartGame()
    {
        LoadSceneSafely(GamePlaySceneName);
    }

    public void VolverAlMenu()
    {
        LoadSceneSafely(MainMenuSceneName);
    }

    public void RestartGamePlay()
    {
        string escenaActual = SceneManager.GetActiveScene().name;
        LoadSceneSafely(escenaActual);
    }

    public void NextScene()
    {
        int siguiente = SceneManager.GetActiveScene().buildIndex + 1;
        if (siguiente < SceneManager.sceneCountInBuildSettings)
        {
            string nextScenePath = SceneUtility.GetScenePathByBuildIndex(siguiente);
            string nextSceneName = Path.GetFileNameWithoutExtension(nextScenePath);

            if (!string.IsNullOrWhiteSpace(nextSceneName))
            {
                LoadSceneSafely(nextSceneName);
                return;
            }

            PrepareForSceneChange();
            SceneManager.LoadScene(siguiente);
        }
    }

    public void EndGame()
    {
        LoadSceneSafely(EndMenuSceneName);
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

    public static bool LoadSceneSafely(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("CambioEscena received an empty scene name.");
            return false;
        }

        PrepareForSceneChange();

        if (!SceneTransitionFade.TryFadeOutAndLoadScene(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }

        return true;
    }

    private static void PrepareForSceneChange()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
