using UnityEngine;

public class Pose_Scene : MonoBehaviour
{
    private bool isPause = false;

    void Update()
    {
        // Escキーでポーズ切り替え
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPause = !isPause;

        if (isPause)
        {
            Time.timeScale = 0f;
            Debug.Log("ポーズ");
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("再開");
        }
    }

    public void ResumeGame()
    {
        isPause = false;
        Time.timeScale = 1f;
        Debug.Log("再開");
    }   
}