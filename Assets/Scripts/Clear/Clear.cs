using UnityEngine;

public class Goal : MonoBehaviour
{
    [SerializeField] private GameObject clearUI;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerRoot")) //プレイヤーモデル　タグ
        {
            clearUI.SetActive(true);
            Time.timeScale = 0f; // ゲーム停止（任意）
        }
    }
}