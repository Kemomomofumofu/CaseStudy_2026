using UnityEngine;
using UnityEngine.InputSystem; // Input Systemを使う場合

public class PauseManager : MonoBehaviour
{
    [Header("【ポーズ画面のUI】")]
    [Tooltip("ポーズ時に表示するUI（PauseUI_Prefab）を入れます")]
    public GameObject pauseUI;

    // 現在ポーズ中かどうかを記憶するフラグ
    private bool isPaused = false;

    private void Start()
    {
        // 念のため、ゲーム開始時はポーズUIを隠しておく
        if (pauseUI != null)
        {
            pauseUI.SetActive(false);
        }
    }

    private void Update()
    {
        // --- ESCキーが押されたか監視する ---

        // Input Systemの場合（SystemButtonControllerに合わせています）
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    // ポーズのON/OFFを切り替えるメソッド
    public void TogglePause()
    {
        // もしポーズUIが設定されていなければ、エラーを出して何もしない
        if (pauseUI == null)
        {
            Debug.LogError("PauseManagerにPauseUIがセットされていません！");
            return;
        }

        // 状態を反転させる（trueならfalse、falseならtrue）
        isPaused = !isPaused;

        if (isPaused)
        {
            // ポーズON：UIを表示して、時間を止める
            pauseUI.SetActive(true);
            Time.timeScale = 0f;
            Debug.Log("ゲームポーズ");
        }
        else
        {
            // ポーズOFF（再開）：UIを隠して、時間を動かす
            pauseUI.SetActive(false);
            Time.timeScale = 1f;
            Debug.Log("ゲーム再開");
        }
    }
}