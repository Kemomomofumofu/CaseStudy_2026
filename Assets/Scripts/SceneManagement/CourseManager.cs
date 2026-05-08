using UnityEngine;

/// <summary>
/// コース内の進行（レース中か、ゴールしたか）を管理し、勝敗に応じたリザルトUIを制御する監督クラス。
/// </summary>
public class CourseManager : MonoBehaviour
{
    [Header("【UI設定：大元】")]
    [Tooltip("ゲーム開始時は非表示にしておく、リザルト画面の大元（Canvasなど）を入れます")]
    public GameObject resultUI;

    [Header("【UI設定：勝敗で切り替えるパーツ】")]
    [Tooltip("勝った時だけ押せるようにする『NEXT STAGE』ボタンのオブジェクトを入れます")]
    public GameObject nextStageButton;

    // ゲームが現在進行中かどうかを判定するフラグ
    private bool isRacing = false;

    // ----------------------------------------------------
    // ① ゲーム開始時の初期化
    // ----------------------------------------------------
    private void Start()
    {
        if (resultUI == null)
        {
            Debug.LogError("【致命的エラー】CourseManagerにResult UIがセットされていません！");
            return;
        }

        resultUI.SetActive(false);
        isRacing = true;
    }

    // ----------------------------------------------------
    // ② 勝利した時（ダミーゴールに触れた時など）に呼ばれる
    // ----------------------------------------------------
    public void OnPlayerWin()
    {
        if (!isRacing) return;
        isRacing = false;

        // リザルト画面全体を表示
        resultUI.SetActive(true);

        // --- ここからが「最後のステージ」対策の追加コード ---
        int currentBuildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        // 次のシーンが Build Settings に存在するか確認
        if (currentBuildIndex + 1 < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            // 次があるなら、NEXTボタンを表示する
            if (nextStageButton != null) nextStageButton.SetActive(true);
        }
        else
        {
            // 次がない（最後）なら、NEXTボタンは絶対に隠したままにする！
            if (nextStageButton != null) nextStageButton.SetActive(false);
            Debug.Log("最終ステージのため、NEXTボタンを表示しませんでした。");
        }
        // --- ここまで ---
    }

    // ----------------------------------------------------
    // ③ 敗北した時（後日、タイムアップ時などに呼ばれる）
    // ----------------------------------------------------
    public void OnPlayerLose()
    {
        if (!isRacing) return;
        isRacing = false;

        Debug.Log("プレイヤーの敗北...NEXTボタンを隠してリザルトを表示します。");

        // 負けたのでNEXTボタンを非表示（オフ）にする
        if (nextStageButton != null) nextStageButton.SetActive(false);

        resultUI.SetActive(true);
    }
}