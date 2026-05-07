using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // シーン名で読み込み
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // ビルド設定上のインデックスで読み込み
    public void LoadSceneByIndex(int index)
    {
        SceneManager.LoadScene(index);
    }

    // 次のシーンを読み込む（最後なら先頭に戻る）
    public void LoadNextScene()
    {
        int next = (SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings;
        SceneManager.LoadScene(next);
    }

    // 現在のシーンをリロード
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 非同期読み込みの簡易実装（必要ならロードUIと組み合わせてください）
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadAsync(sceneName));
    }

    private System.Collections.IEnumerator LoadAsync(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            yield return null;
        }
    }
}
