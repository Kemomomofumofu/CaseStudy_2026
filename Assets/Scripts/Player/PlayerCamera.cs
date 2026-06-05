using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーカメラ
/// </summary>
public class PlayerCamera : MonoBehaviour
{

    private Camera cam;

    #region メンバ変数
    [Tooltip("プレイヤーオブジェクト")]
    [SerializeField] private Transform target;

    //プレイヤー状態取得
    [SerializeField] private PlayerController playerController;

    [Tooltip("カメラオフセット")]
    [SerializeField] private Vector3 followOffset = new(0f, 2f, -5f);
    private Vector3 followOffsetFlex;

    [Tooltip("カメラ回転オフセット")]
    [SerializeField] private Vector3 followRotationOffset = new(0f, 5f, 90f);

    [Tooltip("注視点オフセット（プレイヤーローカル座標）")]
    [SerializeField] private Vector3 lookAtOffset = new(0f, 1f, 0f);

    [Tooltip("位置追従のスムーズさ（プレイヤーへの遅延）")]
    [SerializeField] private float positionSmoothTime = 0.1f;

    [Tooltip("回転追従のスムーズさ")]
    [SerializeField] private float rotationSmoothTime = 0.15f;

    [Tooltip("視野角")]
    [SerializeField] public float defaultFov = 40;
    [SerializeField] public float HighFov = 70;
    [SerializeField] public float LowFov = 10;
    [SerializeField] public float FovlerpSpeed = 5f;

    [Tooltip("カメラバンク")]
    [SerializeField] public float DefaultZRot = 0;
    [SerializeField] public float TargetZRot = 20;
    [SerializeField] public float ZRotlerpSpeed = 5f;
    [SerializeField] public float ZRotlerpSpeedBack = 5f;
    public float CameraZrotBuffer = 0; 

    [Tooltip("カメラパン")]
    [SerializeField] public float DefaultYRot = 0;
    [SerializeField] public float TargetYRot = 20;
    [SerializeField] public float YRotlerpSpeed = 5f;
    [SerializeField] public float YRotlerpSpeedBack = 5f;
    [SerializeField] public float CameraYrotBuffer = 0;
    
    [Tooltip("カメラX")]
    [SerializeField] public float DefaultXRot = 0;
    [SerializeField] public float TargetXRot = 20;
    [SerializeField] public float XRotlerpSpeed = 5f;
    [SerializeField] public float XRotlerpSpeedBack = 5f;
    [SerializeField] public float CameraXrotBuffer = 0;
    
    private Vector3 currentLocalOffset;                 // ポジション保持用
    private Vector3 offsetVelocity = Vector3.zero;      // カメラオフセット切り替え用
    private Vector3 worldFollowVelocity = Vector3.zero; //　車線移動時の揺れに対応する用

    #endregion // メンバ変数

    #region Unityイベント
    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("[PlayerCamera] ターゲットが未設定。");
            return;
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        cam = GetComponent<Camera>();

        // 初期状態をセット
        cam.fieldOfView = defaultFov;

        currentLocalOffset = followOffset;

        // 初期位置
        transform.position = target.TransformPoint(currentLocalOffset);

        // 初期回転
        var lookTarget = target.TransformPoint(lookAtOffset);
        transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        TurnDirection nextDirection = playerController.QueuedTurnDirection;
        bool isStopping = playerController.isStopping;

        Vector3 targetLocalOffset = followOffset;

        // 衝突時のカメラ処理（FOVとカメラポジション移動）
        if (isStopping)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, LowFov, Time.deltaTime * FovlerpSpeed);
            CameraZrotBuffer = Mathf.Lerp(CameraZrotBuffer, DefaultZRot, Time.deltaTime * ZRotlerpSpeed);
            targetLocalOffset = new Vector3(0.0f, 2.0f, -1.0f);
        }

        // 方向別の処理（位置の目標設定と、FOV・XYZ回転の計算、カメラポジション移動）
        switch (nextDirection)
        {
            case TurnDirection.Straight:
                if (!isStopping)
                {
                    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, HighFov, Time.deltaTime * FovlerpSpeed);
                    CameraZrotBuffer = Mathf.Lerp(CameraZrotBuffer, DefaultZRot, Time.deltaTime * ZRotlerpSpeedBack);
                    CameraYrotBuffer = Mathf.Lerp(CameraYrotBuffer, DefaultYRot, Time.deltaTime * YRotlerpSpeed);
                    CameraXrotBuffer = Mathf.Lerp(CameraXrotBuffer, DefaultXRot, Time.deltaTime * XRotlerpSpeed);
                }
                targetLocalOffset = new Vector3(0.0f, 2.0f, -2.0f);
                break;

            case TurnDirection.Back:
                if (!isStopping)
                {
                    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, defaultFov, Time.deltaTime * FovlerpSpeed);
                    CameraZrotBuffer = Mathf.Lerp(CameraZrotBuffer, DefaultZRot, Time.deltaTime * ZRotlerpSpeedBack);
                    CameraYrotBuffer = Mathf.Lerp(CameraYrotBuffer, DefaultYRot, Time.deltaTime * YRotlerpSpeed);
                    CameraXrotBuffer = Mathf.Lerp(CameraXrotBuffer, DefaultXRot, Time.deltaTime * XRotlerpSpeed);
                }
                targetLocalOffset = new Vector3(0.0f, 1.0f, -1.0f);
                break;

            case TurnDirection.Right:
                if (!isStopping)
                {
                    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, defaultFov, Time.deltaTime * FovlerpSpeed);
                    CameraZrotBuffer = Mathf.Lerp(CameraZrotBuffer, -TargetZRot, Time.deltaTime * ZRotlerpSpeed);
                    CameraYrotBuffer = Mathf.Lerp(CameraYrotBuffer, TargetYRot, Time.deltaTime * YRotlerpSpeed);
                    //CameraXrotBuffer = Mathf.Lerp(CameraXrotBuffer, TargetXRot, Time.deltaTime * XRotlerpSpeed);

                }
                targetLocalOffset = new Vector3(1.0f, 0.3f, -1.2f);
                break;

            case TurnDirection.Left:
                if (!isStopping)
                {
                    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, defaultFov, Time.deltaTime * FovlerpSpeed);
                    CameraZrotBuffer = Mathf.Lerp(CameraZrotBuffer, TargetZRot, Time.deltaTime * ZRotlerpSpeed);
                    CameraYrotBuffer = Mathf.Lerp(CameraYrotBuffer, -TargetYRot, Time.deltaTime * YRotlerpSpeed);
                    //CameraXrotBuffer = Mathf.Lerp(CameraXrotBuffer, TargetXRot, Time.deltaTime * XRotlerpSpeed);
                }
                targetLocalOffset = new Vector3(-1.0f, 0.3f, -1.2f);
                break;
        }

        // 
        currentLocalOffset = Vector3.SmoothDamp(
            currentLocalOffset,
            targetLocalOffset,
            ref offsetVelocity,
            0.1f
        );


    
        Vector3 targetWorldPosition = target.TransformPoint(currentLocalOffset);

        // 車線切り替え時のカメラ揺れ処理
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetWorldPosition,
            ref worldFollowVelocity,  
            positionSmoothTime,      
            60.0f
        );

        // 視点ターゲット設定
        var lookTarget = target.TransformPoint(lookAtOffset);

        // 基本の向き
        Quaternion baseRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

        // 各ロール軸方向決定
        Quaternion Zroll = Quaternion.AngleAxis(CameraZrotBuffer, Vector3.forward);
        Quaternion Yroll = Quaternion.AngleAxis(CameraYrotBuffer, Vector3.up);
        Quaternion Xroll = Quaternion.AngleAxis(CameraXrotBuffer, Vector3.right);

        // ロール軸軸合成、Y X Z の順でないとおそらくジンバルロック的なものが発生するかも
        Quaternion desiredRotation = baseRotation * Yroll * Xroll * Zroll;

        //　回転力をスムーズにする
        float t = ToSmoothLerpFactor(rotationSmoothTime, Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);



        Debug.Log($"CurrentPos: {nextDirection}");
    }
    #endregion // Unityイベント

    #region privateメソッド
    private static float ToSmoothLerpFactor(float smoothTime, float deltaTime)
    {
        if (smoothTime <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Exp(-deltaTime / smoothTime);
    }
    #endregion // privateメソッド
}