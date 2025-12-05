using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
 public void StartGame()
 {
     SceneManager.LoadScene("Test");

    }

    public void QuitGame()
        {
        Debug.Log("Quit!");
        Application.Quit();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
