using UnityEngine;

// Compat layer for scene-change buttons. Prefer this english-named wrapper for new references.
public class SceneChangeController : MonoBehaviour
{
    public void StartGame()
    {
        SceneFlow.LoadGameplay();
    }

    public void VolverAlMenu()
    {
        SceneFlow.LoadMainMenu();
    }

    public void RestartGamePlay()
    {
        SceneFlow.ReloadActiveScene();
    }

    public void NextScene()
    {
        SceneFlow.LoadNextScene();
    }

    public void EndGame()
    {
        SceneFlow.LoadEndMenu();
    }

    public void ExitGame()
    {
        SceneFlow.ExitApplication();
    }

    public static bool LoadSceneSafely(string sceneName)
    {
        return SceneFlow.LoadSceneSafely(sceneName);
    }
}
