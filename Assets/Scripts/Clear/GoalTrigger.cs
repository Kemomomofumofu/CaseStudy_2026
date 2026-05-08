using UnityEngine;

/// <summary>
/// ゴール判定管理
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    private bool isFinished = false;

    private void OnTriggerEnter(Collider other)
    {
        // 既に勝敗が決まっているなら無視
        if (isFinished)
        {
            return;
        }

        // Player(Car) が先に到着
        if (other.gameObject.name == "PlayerBody")
        {
            isFinished = true;
            Debug.Log("クリア");
        }

        // Enemy が先に到着
        else if (other.gameObject.name == "EnemyBody")
        {
            isFinished = true;
            Debug.Log("ゲームオーバー");
        }
    }
}