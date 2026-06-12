using UnityEngine;

/// <summary>
/// ゴール判定管理（CourseManagerと連携）
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    // 勝敗確定済みか
    private bool isFinished = false;

    // ゲーム全体の進行・UIを管理するCourseManagerへの参照
    [SerializeField] private CourseManager courseManager;

    private void OnTriggerEnter(Collider other)
    {
        // 既に勝敗が決まっているなら処理しない
        if (isFinished) return;

        // Player（または PlayerRoot）が先にゴール
        if (other.CompareTag("Player") /*|| other.CompareTag("Player")*/)
        {
            isFinished = true;
            Debug.Log("===== YOU WIN =====");

            // CourseManagerに勝利を通知し、リザルトを表示させる
            if (courseManager != null) courseManager.OnPlayerWin();
        }
        // Enemy が先にゴール
        else if (other.CompareTag("Enemy"))
        {
            isFinished = true;
            Debug.Log("===== YOU LOSE =====");

            // CourseManagerに敗北を通知し、リザルトを表示させる
            if (courseManager != null) courseManager.OnPlayerLose();
        }
    }
}