using UnityEngine;
using UnityEngine.UI;

public class VRUIController : MonoBehaviour
{
    [Header("UI Reference")]
    public Button exitButton;

    void Start()
    {
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
            Debug.Log("Exit Button click listener added successfully.");
        }
        else
        {
            // Auto-locate exit button if not manually assigned
            Button foundButton = GetComponentInChildren<Button>();
            if (foundButton != null)
            {
                exitButton = foundButton;
                exitButton.onClick.AddListener(ExitGame);
                Debug.Log("Exit Button auto-located and initialized.");
            }
            else
            {
                Debug.LogWarning("Exit button not assigned or found. Please assign it in the Inspector.");
            }
        }
    }

    public void ExitGame()
    {
        Debug.Log("Exiting Pegasus VR Flying Game...");
        
        // Quits the build application
        Application.Quit();

        // If playing in the Unity Editor, stop execution
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
