using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class WaterSurfaceWave : MonoBehaviour
{
    [Header("【波の設定】")]
    public float waveHeight = 0.2f;    // 波の高さ（凹凸の強さ）
    public float waveLength = 0.5f;    // 波の細かさ（小さいほど大波、大きいほどさざ波）
    public float waveSpeed = 1.0f;     // 波の動くスピード

    private MeshFilter meshFilter;
    private Mesh baseMesh;
    private Vector3[] baseVertices;
    private Vector3[] workingVertices;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();

        // 元のメッシュの形を壊さないようにクローンを作る
        baseMesh = meshFilter.sharedMesh;
        Mesh animatedMesh = Instantiate(baseMesh);
        meshFilter.mesh = animatedMesh;

        // 初期状態の頂点座標を記憶
        baseVertices = baseMesh.vertices;
        workingVertices = new Vector3[baseVertices.Length];
    }

    void Update()
    {
        float timeOffset = Time.time * waveSpeed;

        // 全ての頂点をループ処理して、それぞれの高さを計算する
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];

            // 世界座標（またはローカル座標）をベースにノードの位置を決定
            // 縦・横（XとZ）の位置に応じた波のうねりを作る
            float noiseX = (vertex.x + transform.position.x) * waveLength + timeOffset;
            float noiseZ = (vertex.z + transform.position.z) * waveLength + timeOffset;

            // パーリンノイズで滑らかな高低差（Y軸の動き）を計算
            float y = Mathf.PerlinNoise(noiseX, noiseZ) * waveHeight;

            // 頂点の位置を更新（元々のY座標にプラスする）
            workingVertices[i] = new Vector3(vertex.x, vertex.y + y, vertex.z);
        }

        // 動かした頂点データをメッシュに反映
        meshFilter.mesh.vertices = workingVertices;

        // 光の反射（法線）を再計算して、見た目の凹凸を滑らかにする
        meshFilter.mesh.RecalculateNormals();
        meshFilter.mesh.RecalculateBounds();
    }
}