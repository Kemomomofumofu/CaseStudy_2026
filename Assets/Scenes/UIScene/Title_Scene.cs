using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Title_Scene : MonoBehaviour
{
    // スタート処理（ここからステージセレクトシーンへ）
    public void On_StartButton()
    {
        //SceneLoad用の関数なので、これをコピペすると、簡単にシーン遷移
        SceneManager.LoadScene("StageSelect_Scene");
        Debug.Log("スタート判定"); // コンソールに表示
    }

    // スタート処理（ここからステージセレクトシーンへ）
    public void On_ExitButton()
    {
        Debug.Log("エグジット判定"); // コンソールに表示
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}