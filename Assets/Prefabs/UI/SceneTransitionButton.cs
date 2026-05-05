using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionButton : MonoBehaviour
{
    [Header("このボタンを押した時の遷移先")]
    [Tooltip("プルダウンから遷移先のシーンを選んでください")]
    public SceneName nextScene;

    // バグ予防：遷移中フラグ
    private bool isTransitioning = false;

    public void OnClickTransition()
    {
        // ① 二重ロード防止
        if (isTransitioning) return;
        isTransitioning = true;

        // ② ポーズ状態の解除
        Time.timeScale = 1f;

        // ③ 例外処理（try-catch）を用いた安全なシーン遷移
        Debug.Log($"シーンをロードします: {nextScene}");

        try
        {
            // シーンのロードを試みる
            SceneManager.LoadScene(nextScene.ToString());
        }
        catch (System.Exception e)
        {
            // Build Settingsの登録忘れ等でエラーが起きた場合の復帰処理
            Debug.LogError($"シーン遷移に失敗しました。Build Settingsを確認してください。エラー詳細: {e.Message}");

            // フラグを戻して、再度ボタンを押せるように救済する
            isTransitioning = false;
        }
    }
}