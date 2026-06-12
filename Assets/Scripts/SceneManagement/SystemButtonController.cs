using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// ゲーム全体のシステムボタン（遷移・再試行・終了）を管理する汎用モジュール。
/// チームメンバーへ：UIボタンの「OnClick()」にこのクラスのメソッドを割り当ててください。
/// </summary>
public class SystemButtonController : MonoBehaviour
{
    // ====================================================
    // 【設定項目】インスペクターで表示される変数群
    // ====================================================
    [Header("【プロの安全装置】")]
    [Tooltip("チェックを入れると、最後のステージではこのボタンが自動で消滅します（NEXTボタン専用）")]
    public bool autoHideIfLastStage = false;

    [Header("【セレクト画面用：飛び先のシーン番号】")]
    [Tooltip("OnClickLoadSpecificScene() を使う時、ここに入力した番号のシーンへ飛びます")]
    public int targetSceneIndex = 0;


    // ====================================================
    // ① 初期化処理と常時監視システム
    // ====================================================
    private void Start()
    {
        // もしインスペクターでチェックが入っており、かつ「次のシーン」が存在しない場合（NEXTボタン用）
        if (autoHideIfLastStage)
        {
            int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
            if (currentBuildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log("最後のステージのため、このボタンを自動で非表示にします。");
                gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // Input SystemでのESCキー監視
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // 今いるのがタイトル画面（Index 0）なら、そのままゲーム終了
            if (currentSceneIndex == 0)
            {
                Debug.Log("ESCキーが押されました。ゲームを終了します。");
                OnClickExit();
            }
        }
    }

    // ====================================================
    // ② 各ボタンに割り当てる機能（メソッド）群
    // ====================================================

    /// <summary>
    /// PLAY START ボタン用：タイトル画面からセレクト画面（Build Index 1）へ進む
    /// </summary>
    public void OnClickPlayStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(1); // プロのルール：1番は絶対にセレクト画面
    }

    /// <summary>
    /// NEXT STAGE ボタン用：今のシーンの「次」の番号のシーンをロードする
    /// </summary>
    public void OnClickNextStage()
    {
        Time.timeScale = 1f;
        int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;

        // 次のシーンがBuild Settingsに登録されているか安全確認
        if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextBuildIndex);
        }
        else
        {
            Debug.LogError("【警告】これ以上次のステージが登録されていません！");
        }
    }

    /// <summary>
    /// セレクト画面汎用：インスペクターで指定した番号（targetSceneIndex）のシーンへ直接飛ぶ
    /// </summary>
    public void OnClickLoadSpecificScene()
    {
        Time.timeScale = 1f;

        if (targetSceneIndex >= 0 && targetSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(targetSceneIndex);
            Debug.LogError($"{targetSceneIndex}シーンをロード");
        }
        else
        {
            Debug.LogError($"【エラー】指定されたシーン番号 ({targetSceneIndex}) は存在しません！File > Build Profiles > Scene Listを確認してください。");
        }
    }

    /// <summary>
    /// RETRY / RESTART ボタン用：今のシーンを最初から読み込み直す
    /// </summary>
    public void OnClickRetry()
    {
        Time.timeScale = 1f;
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentBuildIndex);
    }

    /// <summary>
    /// TITLE ボタン用：タイトル画面（Build Index 0）へ戻る
    /// </summary>
    public void OnClickReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// STAGE SELECT ボタン用：リザルト等からセレクト画面（Build Index 1）へ戻る
    /// </summary>
    public void OnClickReturnToSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }

    /// <summary>
    /// EXIT ボタン用：ゲーム自体を終了する
    /// </summary>
    public void OnClickExit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); 
#endif
    }
}