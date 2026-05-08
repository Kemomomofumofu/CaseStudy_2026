using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの操作を管理するクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("初期状態")]
    [Tooltip("プレイヤーの初期レーン")]
    [SerializeField] private Lane initialLane;

    [Tooltip("プレイヤーの初期位置")]
    [SerializeField] private float initialS = 0.0f;

    [Header("プレイヤー設定")]
    [Tooltip("移動速度")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("回転速度")]
    [SerializeField] private float rotateSpeed = 8.0f;

    [Space(0.5f)]
    [Tooltip("判定")]
    [SerializeField] private float obstacleCheckMargin = 0.2f;

    [Header("障害物設定")]
    [Tooltip("生成する障害物Prefab")]
    [SerializeField] private LaneObstacle obstaclePrefab;

    [Tooltip("障害物の長さ")]
    [SerializeField] private float obstacleLength = 2.0f;

    [Tooltip("障害物が消えるまでの時間")]
    [SerializeField] private float obstacleLifetime = 5.0f;

    [Tooltip("障害物に衝突した際の停止時間")]
    [SerializeField] private float obstacleStopDuration = 1.0f;

    private bool isStopping = false;
    private float stopTimer = 0.0f;

    [Tooltip("状態")]
    [SerializeField] private PlayerPathState pathState = new();

    [Tooltip("CPU切り替え")]
    [SerializeField] private bool isCPU = false;

    // --- CPU (AI) 設定 ---
    [Header("CPU設定")]
    [Tooltip("AI の判断間隔（秒）")]
    [SerializeField] private float aiDecisionInterval = 0.6f;

    [Tooltip("判断ごとのレーン移動確率（0..1）")]
    [SerializeField, Range(0f, 1f)] private float aiLaneShiftChance = 0.08f;

    [Tooltip("交差点での左右選択確率（0..1）")]
    [SerializeField, Range(0f, 1f)] private float aiTurnBias = 0.5f;

    [Tooltip("障害物回避の先読み距離（s差）")]
    [SerializeField] private float aiAvoidLookahead = 4.0f;

    [Tooltip("AIが障害物を置く確率（判断ごと）")]
    [SerializeField, Range(0f, 1f)] private float aiPlaceObstacleChance = 0.06f;

    [Tooltip("AIが障害物を置く際のクールダウン（秒）")]
    [SerializeField] private float aiPlaceCooldown = 3.0f;

    private float aiDecisionTimer = 0.0f;
    private float aiPlaceTimer = 0.0f;

    private TurnDirection queuedTurnDirection = TurnDirection.Straight;
    public TurnDirection QueuedTurnDirection => queuedTurnDirection;

    [Tooltip("車線変更のクールタイム（秒）")]
    [SerializeField] private float laneShiftCooldown = 1.0f;

    private float laneShiftTimer = 0.0f;

    // ------------------------------
    // ゲーム開始カウント
    // ------------------------------
    private bool isGameStarted = false;
    private float startTimer = 3.0f;
    private int lastCountdown = 4;

    private void Start()
    {
        ResetToInitialState();
        SyncTransformToLane();

        isGameStarted = false;
        startTimer = 3.0f;
        lastCountdown = 4;
    }

    /// <summary>
    /// プレイヤーの入力（またはCPUの意思決定）を管理
    /// </summary>
    private void Update()
    {
        // ------------------------------
        // ゲーム開始3秒待機
        // ------------------------------
        if (!isGameStarted)
        {
            startTimer -= Time.deltaTime;

            int currentCountdown = Mathf.CeilToInt(startTimer);

            // 3・2・1 を1回だけ表示
            if (currentCountdown > 0 && currentCountdown != lastCountdown)
            {
                Debug.Log(currentCountdown);
                lastCountdown = currentCountdown;
            }

            // スタート
            if (startTimer <= 0.0f)
            {
                isGameStarted = true;
                Debug.Log("3秒カウント後スタート");
            }

            return;
        }

        UpdateStopTimer();
        UpdateLaneShiftTimer();

        // AI 用クールダウンを毎フレーム更新
        if (aiPlaceTimer > 0f)
        {
            aiPlaceTimer -= Time.deltaTime;

            if (aiPlaceTimer < 0f)
            {
                aiPlaceTimer = 0f;
            }
        }

        UpdateInput();
        UpdateMovement();
        SyncTransformToLane();
    }

    /// <summary>
    /// 停止時間のタイマーを更新
    /// </summary>
    private void UpdateStopTimer()
    {
        if (!isStopping)
        {
            return;
        }

        stopTimer -= Time.deltaTime;

        if (stopTimer <= 0.0f)
        {
            stopTimer = 0.0f;
            isStopping = false;
        }
    }

    /// <summary>
    /// 車線変更クールタイムを更新
    /// </summary>
    private void UpdateLaneShiftTimer()
    {
        if (laneShiftTimer <= 0f)
        {
            return;
        }

        laneShiftTimer -= Time.deltaTime;

        if (laneShiftTimer < 0f)
        {
            laneShiftTimer = 0f;
        }
    }

    private void UpdateInput()
    {
        if (isStopping)
        {
            return;
        }

        if (isCPU)
        {
            UpdateAI();
            return;
        }

        var keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // 交差点での進行方向予約
        if (keyboard.wKey.wasPressedThisFrame)
        {
            queuedTurnDirection = TurnDirection.Straight;
        }
        else if (keyboard.aKey.wasPressedThisFrame)
        {
            queuedTurnDirection = TurnDirection.Left;
        }
        else if (keyboard.dKey.wasPressedThisFrame)
        {
            queuedTurnDirection = TurnDirection.Right;
        }
        else if (keyboard.sKey.wasPressedThisFrame)
        {
            queuedTurnDirection = TurnDirection.Back;
        }

        // レーン変更
        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            TryShiftLane(1);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            TryShiftLane(-1);
        }

        // 障害物生成
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            TrySpawnObstacleAtLaneEnd();
        }
    }

    /// <summary>
    /// 簡易 AI 判断処理
    /// </summary>
    private void UpdateAI()
    {
        aiDecisionTimer -= Time.deltaTime;

        if (aiDecisionTimer > 0f)
        {
            return;
        }

        aiDecisionTimer = aiDecisionInterval;

        Lane currentLane = pathState.CurrentLane;

        if (currentLane == null)
        {
            return;
        }

        // --- 障害物回避 ---
        float lookStart = pathState.CurrentS + obstacleCheckMargin;
        float lookEnd = pathState.CurrentS + Mathf.Max(aiAvoidLookahead, obstacleCheckMargin + 0.01f);

        bool obstacleAhead = currentLane.HasObstacleInRange(lookStart, lookEnd);

        if (obstacleAhead)
        {
            Lane leftLane = currentLane.ParentWay != null
                ? currentLane.ParentWay.GetLane(currentLane.LaneIndex + 1)
                : null;

            Lane rightLane = currentLane.ParentWay != null
                ? currentLane.ParentWay.GetLane(currentLane.LaneIndex - 1)
                : null;

            bool leftClear = leftLane != null &&
                             !leftLane.HasObstacleInRange(pathState.CurrentS,
                                 pathState.CurrentS + aiAvoidLookahead);

            bool rightClear = rightLane != null &&
                              !rightLane.HasObstacleInRange(pathState.CurrentS,
                                  pathState.CurrentS + aiAvoidLookahead);

            if (leftClear && rightClear)
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    TryShiftLane(1);
                }
                else
                {
                    TryShiftLane(-1);
                }

                return;
            }
            else if (leftClear)
            {
                TryShiftLane(1);
                return;
            }
            else if (rightClear)
            {
                TryShiftLane(-1);
                return;
            }
        }

        // --- 時々レーン変更 ---
        if (UnityEngine.Random.value < aiLaneShiftChance)
        {
            int dir = UnityEngine.Random.value < 0.5f ? 1 : -1;
            TryShiftLane(dir);
            return;
        }

        // --- 障害物生成 ---
        if (obstaclePrefab != null &&
            aiPlaceTimer <= 0f &&
            UnityEngine.Random.value < aiPlaceObstacleChance)
        {
            TrySpawnObstacleAtLaneEnd();
            aiPlaceTimer = aiPlaceCooldown;
        }
    }

    /// <summary>
    /// 左右レーンへ即時移動を試行
    /// </summary>
    private void TryShiftLane(int _laneOffset)
    {
        if (laneShiftTimer > 0f)
        {
            return;
        }

        Lane currentLane = pathState.CurrentLane;

        if (currentLane == null || currentLane.ParentWay == null)
        {
            return;
        }

        int targetLaneIndex = currentLane.LaneIndex + _laneOffset;
        Lane targetLane = currentLane.ParentWay.GetLane(targetLaneIndex);

        if (targetLane == null)
        {
            Debug.Log("隣のレーンが存在しない");
            return;
        }

        pathState.CurrentLane = targetLane;
        laneShiftTimer = laneShiftCooldown;

        SyncTransformToLane();
    }

    /// <summary>
    /// プレイヤーの移動と回転を更新
    /// </summary>
    private void UpdateMovement()
    {
        if (isStopping)
        {
            return;
        }

        Lane currentLane = pathState.CurrentLane;

        if (currentLane == null)
        {
            return;
        }

        float nextS = pathState.CurrentS + moveSpeed * Time.deltaTime;

        // 障害物チェック
        LaneObstacle hitObstacle;

        if (currentLane.TryGetObstacleAt(nextS + obstacleCheckMargin, out hitObstacle))
        {
            OnHitObstacle(hitObstacle);
            return;
        }

        float laneLength = currentLane.Length;

        // レーン終端処理
        if (nextS >= laneLength)
        {
            float remain = nextS - laneLength;

            Lane nextLane = currentLane.GetNextLane(queuedTurnDirection);

            if (nextLane == null)
            {
                OnInvalidTurn();
                return;
            }

            pathState.CurrentLane = nextLane;
            pathState.CurrentS = remain;
            queuedTurnDirection = TurnDirection.Straight;

            return;
        }

        pathState.CurrentS = nextS;
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private void ResetToInitialState()
    {
        pathState.Reset(initialLane, initialS);

        queuedTurnDirection = TurnDirection.Straight;
        aiDecisionTimer = 0f;
        aiPlaceTimer = 0f;
        laneShiftTimer = 0f;
    }

    /// <summary>
    /// プレイヤーの位置と回転をレーンに合わせて更新
    /// </summary>
    private void SyncTransformToLane()
    {
        Lane currentLane = pathState.CurrentLane;

        if (currentLane == null)
        {
            return;
        }

        Vector3 position = currentLane.GetPositionByS(pathState.CurrentS);
        Vector3 forward = currentLane.GetForwardByS(pathState.CurrentS);

        transform.position = position;

        if (forward.sqrMagnitude > 1e-4f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// 障害物に衝突した場合の処理
    /// </summary>
    private void OnHitObstacle(LaneObstacle _hitObstacle)
    {
        if (_hitObstacle)
        {
            Destroy(_hitObstacle.gameObject);
        }

        isStopping = true;
        stopTimer = obstacleStopDuration;

        pathState.CurrentS = Mathf.Max(0.0f, pathState.CurrentS - 1.0f);

        SyncTransformToLane();

        Debug.Log("障害物に衝突");
    }

    /// <summary>
    /// 進行可能な道がない場合の処理
    /// </summary>
    private void OnInvalidTurn()
    {
        HandleReset("進行可能な道がない。");
    }

    /// <summary>
    /// ログ出力後に初期状態へ戻す共通処理
    /// </summary>
    private void HandleReset(string message)
    {
        Debug.Log(message);

        ResetToInitialState();
        SyncTransformToLane();
    }

    /// <summary>
    /// 現在のLane終端位置に障害物を生成する
    /// </summary>
    private void TrySpawnObstacleAtLaneEnd()
    {
        Lane currentLane = pathState.CurrentLane;

        if (currentLane == null)
        {
            return;
        }

        if (obstaclePrefab == null)
        {
            Debug.Log("障害物Prefabが設定されていない。");
            return;
        }

        float laneLength = currentLane.Length;

        if (laneLength <= 0.0f)
        {
            Debug.Log("Laneの長さが不正なため障害物を生成できない。");
            return;
        }

        float clampedObstacleLength = Mathf.Max(0.1f, obstacleLength);

        float sEnd = laneLength;
        float sStart = Mathf.Max(0.0f, sEnd - clampedObstacleLength);

        if (currentLane.HasObstacleInRange(sStart, sEnd))
        {
            Debug.Log("Lane終端に既存障害物があるため生成しない。");
            return;
        }

        SpawnObstacle(currentLane, sStart, sEnd);
    }

    /// <summary>
    /// 障害物を生成してLaneへ登録する
    /// </summary>
    private void SpawnObstacle(Lane _lane, float _sStart, float _sEnd)
    {
        LaneObstacle obstacle = Instantiate(obstaclePrefab);

        obstacle.Setup(_lane, _sStart, _sEnd);

        _lane.AddObstacle(obstacle);

        obstacle.SyncVisual();

        Destroy(obstacle.gameObject, obstacleLifetime);
    }

    /// <summary>
    /// 外部から CPU を切り替える
    /// </summary>
    public void SetIsCPU(bool cpu)
    {
        isCPU = cpu;
        aiDecisionTimer = 0f;
    }
}