using UnityEngine;

/// <summary>
/// ゴール判定管理（CourseManagerと連携）
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    // 勝敗確定済みか
    private bool isFinished = false;

<<<<<<< Updated upstream
    // ★追加：あなたの作った監督を呼ぶための枠
=======
    // ゲーム全体の進行・UIを管理するCourseManagerへの参照
>>>>>>> Stashed changes
    [SerializeField] private CourseManager courseManager;

    private void OnTriggerEnter(Collider other)
    {
<<<<<<< Updated upstream
=======
        // 既に勝敗が決まっているなら処理しない
>>>>>>> Stashed changes
        if (isFinished) return;

        // Player が先にゴール
        if (other.CompareTag("Player"))
        {
            isFinished = true;
            Debug.Log("===== YOU WIN =====");

<<<<<<< Updated upstream
            // ★変更：シーン遷移ではなく、監督に「勝ち」を報告する！
=======
            // CourseManagerに勝利を通知し、リザルトを表示させる
>>>>>>> Stashed changes
            if (courseManager != null) courseManager.OnPlayerWin();
        }
        // Enemy が先にゴール
        else if (other.CompareTag("Enemy"))
        {
            isFinished = true;
            Debug.Log("===== YOU LOSE =====");

<<<<<<< Updated upstream
            // ★変更：シーン遷移ではなく、監督に「負け」を報告する！
=======
            // CourseManagerに敗北を通知し、リザルトを表示させる
>>>>>>> Stashed changes
            if (courseManager != null) courseManager.OnPlayerLose();
        }
    }
}