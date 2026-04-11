using UnityEngine;

[System.Obsolete("Use SceneFlowController or SceneFlow instead.")]
public class CambioEscena : MonoBehaviour
{
    // Inicia juego.
    public void StartGame()
    {
        SceneFlow.LoadGameplay();
    }

    // Gestiona volver al menu.
    public void VolverAlMenu()
    {
        SceneFlow.LoadMainMenu();
    }

    // Gestiona restart juego play.
    public void RestartGamePlay()
    {
        SceneFlow.ReloadActiveScene();
    }

    // Gestiona siguiente escena.
    public void NextScene()
    {
        SceneFlow.LoadNextScene();
    }

    // Gestiona end juego.
    public void EndGame()
    {
        SceneFlow.LoadEndMenu();
    }

    // Desactiva juego.
    public void ExitGame()
    {
        SceneFlow.ExitApplication();
    }

    // Carga escena safely.
    public static bool LoadSceneSafely(string sceneName)
    {
        return SceneFlow.LoadSceneSafely(sceneName);
    }

    // Gestiona preparar para escena change.
    private static void PrepareForSceneChange()
    {
        SceneFlow.PrepareForSceneChange();
    }
}
