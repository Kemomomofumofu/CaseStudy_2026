using UnityEngine;
using TMPro; // TextMeshProを使うための宣言

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

    [Tooltip("勝敗の文字（YOU WIN / YOU LOSE）を表示する TextMeshPro のオブジェクトを入れます")]
    public TextMeshProUGUI resultText;

    // ゲームが現在進行中かどうかを判定するフラグ
    private bool isRacing = false;

    // ----------------------------------------------------
    // ① ゲーム開始時の初期化
    // ----------------------------------------------------
    private void Start()
    {
        Time.timeScale = 1.0f;

        if (resultUI == null)
        {
            Debug.LogError("【致命的エラー】CourseManagerにResult UIがセットされていません！");
            return;
        }

        resultUI.SetActive(false);
        isRacing = true;
    }

    // ----------------------------------------------------
    // ② 勝利した時に呼ばれる
    // ----------------------------------------------------
    public void OnPlayerWin()
    {
        if (!isRacing) return;
        isRacing = false;

        // リザルト画面全体を表示
        resultUI.SetActive(true);

        // テキストを勝ち仕様に変更
        if (resultText != null)
        {
            resultText.text = "YOU WIN";
            resultText.color = Color.yellow;
        }

        // --- 最後のステージ対策 ---
        int currentBuildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        if (currentBuildIndex + 1 < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            if (nextStageButton != null) nextStageButton.SetActive(true);
        }
        else
        {
            if (nextStageButton != null) nextStageButton.SetActive(false);
            Debug.Log("最終ステージのため、NEXTボタンを表示しませんでした。");
        }

        // ゲーム内時間を止める
        Time.timeScale = 0f;
    }

    // ----------------------------------------------------
    // ③ 敗北した時に呼ばれる
    // ----------------------------------------------------
    public void OnPlayerLose()
    {
        if (!isRacing) return;
        isRacing = false;

        // 負けたのでNEXTボタンを非表示（オフ）にする
        if (nextStageButton != null) nextStageButton.SetActive(false);

        resultUI.SetActive(true);

        // テキストを負け仕様に変更
        if (resultText != null)
        {
            resultText.text = "YOU LOSE";
            resultText.color = new Color(0.2f, 0.5f, 1f);
        }

        // ゲーム内時間を止める
        Time.timeScale = 0f;
    }
}