using UnityEngine;

/// <summary>
/// ゴール判定管理（CourseManagerと連携）
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    // 勝敗確定済みか
    private bool isFinished = false;

    // ★追加：あなたの作った監督を呼ぶための枠
    [SerializeField] private CourseManager courseManager;

    private void OnTriggerEnter(Collider other)
    {
        if (isFinished) return;

        // Player が先にゴール
        if (other.CompareTag("Player"))
        {
            isFinished = true;
            Debug.Log("===== YOU WIN =====");

            // ★変更：シーン遷移ではなく、監督に「勝ち」を報告する！
            if (courseManager != null) courseManager.OnPlayerWin();
        }
        // Enemy が先にゴール
        else if (other.CompareTag("Enemy"))
        {
            isFinished = true;
            Debug.Log("===== YOU LOSE =====");

            // ★変更：シーン遷移ではなく、監督に「負け」を報告する！
            if (courseManager != null) courseManager.OnPlayerLose();
        }
    }
}