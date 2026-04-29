using UnityEngine;
using UnityEngine.SceneManagement;

// Attach this to any GameObject in your game scene.
// When Fusion loads the scene additively, Unity switches the active scene
// to the new copy, losing RenderSettings. This component forces the scene
// it lives in back to being the active scene, restoring skybox and lighting.
public class SceneLightingAnchor : MonoBehaviour
{
    void Awake()
    {
        Scene myScene = gameObject.scene;
        if (SceneManager.GetActiveScene() != myScene)
        {
            SceneManager.SetActiveScene(myScene);
            DynamicGI.UpdateEnvironment();
        }
    }
}