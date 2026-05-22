using UnityEngine;

public class CarFollowCamera : MonoBehaviour
{
    [Header("【追尾するターゲット（車）】")]
    public Transform target;

    [Header("【カメラの位置・引きの調整】")]
    [Tooltip("車からどれくらい後ろに離すか")]
    public float distance = 6.0f;

    [Tooltip("車からどれくらい高い位置に置くか")]
    public float height = 2.5f;

    [Header("【カメラの動きの滑らかさ】")]
    [Tooltip("位置が追いつくスピード（小さいほどヌルッと遅れてついてくる）")]
    public float positionSmoothSpeed = 5.0f;

    [Tooltip("回転が追いつくスピード")]
    public float rotationSmoothSpeed = 5.0f;

    [Header("【見下ろし角度の微調整】")]
    [Tooltip("車を少し見下ろすための注視点の高さオフセット")]
    public float lookAtHeightOffset = 0.5f;

    void LateUpdate()
    {
        // ターゲットが設定されていない場合は何もしない
        if (!target) return;

        // ─── 1. 目標となるカメラの位置を計算 ───
        // 車の後ろ方向ベクトルをベースに、引き算して位置を決める
        Vector3 wantPosition = target.position - (target.forward * distance);
        wantPosition.y = target.position.y + height;

        // ─── 2. カメラの位置をなめらかに移動（Lerp） ───
        transform.position = Vector3.Lerp(transform.position, wantPosition, positionSmoothSpeed * Time.deltaTime);

        // ─── 3. カメラの回転をなめらかに調整 ───
        // 車の少し上（注視点）を向くように計算
        Vector3 targetLookAtPos = target.position + (Vector3.up * lookAtHeightOffset);
        Vector3 direction = targetLookAtPos - transform.position;
        Quaternion wantRotation = Quaternion.LookRotation(direction);

        // 回転をなめらかに補間（Slerp）
        transform.rotation = Quaternion.Slerp(transform.rotation, wantRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}