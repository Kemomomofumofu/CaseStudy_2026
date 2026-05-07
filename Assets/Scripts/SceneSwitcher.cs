using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // デバッグログを有効化すると、ボタンから呼ばれたか確認できます
    public bool enableLogs = true;
    // シーン名で読み込み
    public void LoadSceneByName(string sceneName)
    {
        if (enableLogs) Debug.Log($"SceneSwitcher.LoadSceneByName called: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    // ビルド設定上のインデックスで読み込み
    public void LoadSceneByIndex(int index)
    {
        if (enableLogs) Debug.Log($"SceneSwitcher.LoadSceneByIndex called: {index}");
        SceneManager.LoadScene(index);
    }

    // 次のシーンを読み込む（最後なら先頭に戻る）
    public void LoadNextScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int count = SceneManager.sceneCountInBuildSettings;
        if (enableLogs) Debug.Log($"SceneSwitcher.LoadNextScene called. current={current}, sceneCountInBuildSettings={count}");
        if (count <= 0)
        {
            if (enableLogs) Debug.LogError("No scenes in Build Settings. Add your scenes to File > Build Settings.");
            return;
        }
        int next = (current + 1) % count;
        if (enableLogs) Debug.Log($"Loading next scene index: {next}");
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
