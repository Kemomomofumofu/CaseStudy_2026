using UnityEngine;

public class SmoothWiggle : MonoBehaviour
{
    [Header("【揺れの強さ】")]
    public Vector3 positionStrength = new Vector3(0.2f, 0.2f, 0.2f); // 位置の揺れ幅（メートル単位）
    public Vector3 rotationStrength = new Vector3(2.0f, 2.0f, 2.0f); // 回転の揺れ幅（度数単位）

    [Header("【揺れるスピード】")]
    public float speed = 1.0f;

    // 初期状態の座標と回転を記憶する変数
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // ノード（不規則な揺れ）の計算に使うランダムな開始位置
    private float seedX;
    private float seedY;
    private float seedZ;

    // 揺れの基準となる初期姿勢とランダムシードを記録する。
    void Start()
    {
        // ゲーム開始時の位置と回転を記録
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        // オブジェクトごとに揺れのタイミングをずらすためのランダム値
        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(100f, 200f);
        seedZ = Random.Range(200f, 300f);
    }

    // 毎フレーム、パーリンノイズで位置と回転の揺れを反映する。
    void Update()
    {
        // 時間の経過にスピードを乗算
        float timeX = Time.time * speed + seedX;
        float timeY = Time.time * speed + seedY;
        float timeZ = Time.time * speed + seedZ;

        // ─── 1. 位置の揺れ計算 ───
        // パーリンノイズは 0.0〜1.0 の値を返すので、-0.5〜0.5 に補正して中央を基準にする
        float noiseX = Mathf.PerlinNoise(timeX, 0f) - 0.5f;
        float noiseY = Mathf.PerlinNoise(0f, timeY) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(timeX, timeY) - 0.5f;

        Vector3 offsetPosition = new Vector3(
            noiseX * positionStrength.x,
            noiseY * positionStrength.y,
            noiseZ * positionStrength.z
        );
        transform.localPosition = initialPosition + offsetPosition;

        // ─── 2. 回転の揺れ計算 ───
        float rotX = (Mathf.PerlinNoise(timeY, timeZ) - 0.5f) * rotationStrength.x;
        float rotY = (Mathf.PerlinNoise(timeZ, timeX) - 0.5f) * rotationStrength.y;
        float rotZ = (Mathf.PerlinNoise(timeX, timeZ) - 0.5f) * rotationStrength.z;

        Quaternion offsetRotation = Quaternion.Euler(rotX, rotY, rotZ);
        transform.localRotation = initialRotation * offsetRotation;
    }
}