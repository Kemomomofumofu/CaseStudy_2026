using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScrollMenuController : MonoBehaviour
{
    void Start()
    {
  
        Button[] buttons = GetComponentsInChildren<Button>();

        foreach (Button btn in buttons)
        {
        
            Button targetButton = btn;

           
            targetButton.onClick.AddListener(() => OnButtonClick(targetButton));
        }
    }


    public void OnButtonClick(Button clickedButton)
    {
   
        string buttonName = clickedButton.name;

        Debug.Log($"クリックされたボタンのオブジェクト名: {buttonName}");

      
        switch (buttonName)
        {
            case "Stage1":
                SceneManager.LoadScene("Stage1");
                Debug.Log("Stage1選択");
                break;

            case "Stage2":
                SceneManager.LoadScene("Stage2");
                Debug.Log("Stage2選択");
                break;

            case "Stage3":
                SceneManager.LoadScene("Stage3");
                Debug.Log("Stage3選択");
                break;

            case "Stage4":
                SceneManager.LoadScene("Stage4");
                Debug.Log("Stage4選択");
                break;

            case "Stage5":
                SceneManager.LoadScene("Stage5");
                Debug.Log("Stage5選択");
                Application.Quit();
                break;

            default:
               
                Debug.LogWarning($"未登録のボタンが押されました: {buttonName}");
                break;
        }
    }

    public void On_StartButton()
    {
        Debug.Log("スタート判定");

    }
    public void On_ExitButton()
    {
        Debug.Log("タイトルに戻る");

    }
}