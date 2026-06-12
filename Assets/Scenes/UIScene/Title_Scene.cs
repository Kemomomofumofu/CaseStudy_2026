using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Title_Scene : MonoBehaviour
{
    // スタート処理（ここからステージセレクトシーンへ）
    public void On_StartButton()
    {
        SceneManager.LoadScene("StageSelect_Scene");
        Debug.Log("スタート判定"); // コンソールに表示
    }

    // スタート処理（ここからステージセレクトシーンへ）
    public void On_ExitButton()
    {
        Debug.Log("エグジット判定"); // コンソールに表示
    }
}