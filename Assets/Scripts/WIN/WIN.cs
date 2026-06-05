using UnityEngine;

public class Goal : MonoBehaviour
{
    [SerializeField] private GameObject winUI;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerRoot")) //プレイヤーモデル　タグ
        {
            winUI.SetActive(true);
            Time.timeScale = 0f; // ゲーム停止（任意）
        }
    }
}