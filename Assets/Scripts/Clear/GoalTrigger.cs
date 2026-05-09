using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ƒS[ƒ‹”»’èŠÇ—
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    // Ÿ”sŠm’èÏ‚İ‚©
    private bool isFinished = false;

    /// <summary>
    /// Trigger‚É“ü‚Á‚½uŠÔ
    /// </summary>
    /// <param name="other">ÚG‚µ‚½‘Šè</param>
    private void OnTriggerEnter(Collider other)
    {
        // Šù‚ÉŸ”s‚ªŒˆ‚Ü‚Á‚Ä‚¢‚é‚È‚çˆ—‚µ‚È‚¢
        if (isFinished)
        {
            return;
        }

        // ÚGŠm”F
        Debug.Log("ÚG‚µ‚½ : " + other.gameObject.name);

        // TagŠm”F
        Debug.Log("Tag : " + other.gameObject.tag);

        // Player ‚ªæ‚ÉƒS[ƒ‹
        if (other.CompareTag("Player"))
        {
            isFinished = true;

            Debug.Log("===== CLEAR =====");

            // ƒV[ƒ“‘JˆÚ
            SceneManager.LoadScene("Stage1Result");
        }

        // Enemy ‚ªæ‚ÉƒS[ƒ‹
        else if (other.CompareTag("Enemy"))
        {
            isFinished = true;

            Debug.Log("===== GAME OVER =====");

            // ƒV[ƒ“‘JˆÚ
            SceneManager.LoadScene("Stage1Result");
        }
    }
}