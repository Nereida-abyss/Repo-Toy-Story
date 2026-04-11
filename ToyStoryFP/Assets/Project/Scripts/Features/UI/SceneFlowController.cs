using UnityEngine;

[AddComponentMenu("Flow/Scene Flow Controller")]
public class SceneFlowController : MonoBehaviour
{
    public void LoadGameplay()
    {
        SceneFlow.LoadGameplay();
    }

    public void LoadMainMenu()
    {
        SceneFlow.LoadMainMenu();
    }

    public void ReloadActiveScene()
    {
        SceneFlow.ReloadActiveScene();
    }

    public void LoadNextScene()
    {
        SceneFlow.LoadNextScene();
    }

    public void LoadEndMenu()
    {
        SceneFlow.LoadEndMenu();
    }

    public void ExitApplication()
    {
        SceneFlow.ExitApplication();
    }
}
