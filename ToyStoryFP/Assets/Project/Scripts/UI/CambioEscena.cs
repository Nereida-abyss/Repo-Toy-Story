using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioEscena : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string GamePlaySceneName = "GamePlay";
    private const string EndMenuSceneName = "EndMenu";

    // Inicia juego.
    public void StartGame()
    {
        LoadSceneSafely(GamePlaySceneName);
    }

    // Gestiona volver al menu.
    public void VolverAlMenu()
    {
        LoadSceneSafely(MainMenuSceneName);
    }

    // Gestiona restart juego play.
    public void RestartGamePlay()
    {
        string escenaActual = SceneManager.GetActiveScene().name;
        LoadSceneSafely(escenaActual);
    }

    // Gestiona siguiente escena.
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

    // Gestiona end juego.
    public void EndGame()
    {
        LoadSceneSafely(EndMenuSceneName);
    }

    // Desactiva juego.
    public void ExitGame()
    {
        GameDebug.Info("Escenas", "Has cerrado el juego");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Carga escena safely.
    public static bool LoadSceneSafely(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            GameDebug.Advertencia("Escenas", "CambioEscena recibio un nombre de escena vacio.");
            return false;
        }

        PrepareForSceneChange();

        if (!SceneTransitionFade.TryFadeOutAndLoadScene(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }

        return true;
    }

    // Gestiona preparar para escena change.
    private static void PrepareForSceneChange()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
