using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneFlow
{
    public const string MainMenuSceneName = "MainMenu";
    public const string GamePlaySceneName = "GamePlay";
    public const string EndMenuSceneName = "EndMenu";

    public static void LoadGameplay()
    {
        LoadSceneSafely(GamePlaySceneName);
    }

    public static void LoadMainMenu()
    {
        LoadSceneSafely(MainMenuSceneName);
    }

    public static void ReloadActiveScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        LoadSceneSafely(currentSceneName);
    }

    public static void LoadNextScene()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
            string nextSceneName = Path.GetFileNameWithoutExtension(nextScenePath);

            if (!string.IsNullOrWhiteSpace(nextSceneName))
            {
                LoadSceneSafely(nextSceneName);
                return;
            }

            PrepareForSceneChange();
            SceneManager.LoadScene(nextIndex);
        }
    }

    public static void LoadEndMenu()
    {
        LoadSceneSafely(EndMenuSceneName);
    }

    public static void ExitApplication()
    {
        GameDebug.Info("Escenas", "Has cerrado el juego");

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
            GameDebug.Advertencia("Escenas", "SceneFlow recibio un nombre de escena vacio.");
            return false;
        }

        PrepareForSceneChange();

        if (!SceneTransitionFade.TryFadeOutAndLoadScene(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }

        return true;
    }

    public static void PrepareForSceneChange()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
