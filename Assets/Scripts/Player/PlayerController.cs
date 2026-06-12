using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの操作を管理するクラス
/// </summary>
/// 

public class PlayerController : MonoBehaviour
{

    /// <summary>
    /// プレイヤーを初期状態へ戻し、レーン上の位置へ同期する
    /// </summary>

private void Start()
    {
        ResetToInitialState();

        Debug.Log("InitialLane = " + initialLane);

        SyncTransformToLane();
    }

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

    [Header("交差点設定")]
    [Tooltip("交差点で進行方向を決める距離（Lane終端からの距離）")]
    [SerializeField] private float intersectionDecisionDistance = 0.6f;

    [Header("レーン接続補間")]
    [Tooltip("レーン切り替え時に接続部分を曲線で補間する")]
    [SerializeField] private bool useSmoothLaneTransition = true;
    [Tooltip("次のレーンの始点から何m先で補間を終えるか")]
    [SerializeField, Min(0f)] private float laneTransitionJoinDistance = 2.0f;
    [Tooltip("接続曲線の制御点を端点から離す最大距離")]
    [SerializeField, Min(0f)] private float laneTransitionMaxHandleLength = 3.0f;
    private readonly LaneTransitionCurve laneTransitionCurve = new();

    [Header("障害物設定")]
    [Tooltip("障害物に衝突した際の停止時間")]
    [SerializeField] private float obstacleStopDuration = 1.0f;
    public bool isStopping = false; // 停止中か
    private float stopTimer = 0.0f; // 停止時間のタイマー

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
    //[Tooltip("交差点での左右選択確率（0..1） — 0.5 で左右均等")]
    //[SerializeField, Range(0f, 1f)] private float aiTurnBias = 0.5f;

    [Tooltip("障害物回避の先読み距離（s差）")]
    [SerializeField] private float aiAvoidLookahead = 4.0f;

    [Header("CPU標識設定")]
    [Tooltip("CPU が交差点手前で進行方向の標識を配置する")]
    [SerializeField] private bool aiCanPlaceRoadSigns = true;
    [Tooltip("交差点の何m手前から標識を配置するか")]
    [SerializeField] private float aiRoadSignPlacementDistance = 5.0f;
    [Tooltip("標識配置位置を丸めるグリッドサイズ。0 以下なら丸めない")]
    [SerializeField] private float aiRoadSignGridSize = 5.0f;
    [Tooltip("標識配置時の地面検出レイ高さ")]
    [SerializeField] private float aiRoadSignGroundRaycastHeight = 5.0f;
    [Tooltip("近くに既に標識がある場合の重複配置チェック半径")]
    [SerializeField] private float aiRoadSignDuplicateCheckRadius = 1.0f;
    [Tooltip("CPU が配置する標識候補。未設定ならシーン内の手札から取得")]
    [SerializeField] private List<RoadSignDefinition> aiRoadSignDefinitions = new();
    [Tooltip("標識候補を借りる手札。未設定ならシーン内から自動検索")]
    [SerializeField] private RoadSignHandController aiRoadSignSourceHand = null;

    [Header("CPU妨害標識設定")]
    [Tooltip("CPU が定期的に妨害標識の配置を判定する")]
    [SerializeField] private bool aiCanPlaceSabotageSigns = true;
    [Tooltip("妨害標識を配置するか判定する間隔（秒）")]
    [SerializeField] private float aiSabotageSignInterval = 5.0f;
    [Tooltip("判定ごとに妨害標識を配置する確率（0..1）")]
    [SerializeField, Range(0f, 1f)] private float aiSabotageSignPlaceChance = 0.5f;
    [Tooltip("現在位置からどれだけ前方に妨害標識を置くか（Lane の s 差）")]
    [SerializeField] private float aiSabotageSignPlacementDistance = 5.0f;

    private float aiDecisionTimer = 0.0f;
    private float aiSabotageSignTimer = 0.0f;
    private readonly List<RoadSignDefinition> aiRoadSignCandidateBuffer = new();
    private readonly List<RoadSignDefinition> aiSabotageSignCandidateBuffer = new();
    private readonly List<TurnDirection> aiAvailableTurnBuffer = new();
    private Lane aiRoadSignHandledLane = null;

    public TurnDirection queuedTurnDirection = TurnDirection.Straight;
    public TurnDirection QueuedTurnDirection => queuedTurnDirection;

    [Tooltip("車線変更のクールタイム（秒）")]
    [SerializeField] private float laneShiftCooldown = 1.0f;

    private float laneShiftTimer = 0.0f;

    // --- 標識関連 ---
    [Header("標識関連")]
    [Tooltip("標識の受信コンポーネント")]
    [SerializeField] private RoadSignReceiver signReceiver = null;

    private readonly PlayerSignResolver signResolver = new(); // 標識解決用

    /// <summary>
    /// 標識受信コンポーネントの参照を初期化する
    /// </summary>
    private void Awake()
    {
        if (signReceiver == null)
        {
            signReceiver = GetComponent<RoadSignReceiver>();
        }
    }




    /// <summary>
    /// プレイヤーの入力（またはCPUの意思決定）を管理
    /// </summary>
    private void Update()
    {
        UpdateStopTimer();
        UpdateLaneShiftTimer();
        UpdateAISabotageSign();

        UpdateInput();
        UpdateQueuedTurnDirectionBySign();
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

    // 新規追加
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

    /// <summary>
    /// CPUの妨害標識タイマーを更新し、一定間隔で配置するかランダムに判定する
    /// </summary>
    private void UpdateAISabotageSign()
    {
        if (!isCPU || !aiCanPlaceSabotageSigns)
        {
            return;
        }

        aiSabotageSignTimer -= Time.deltaTime;
        if (aiSabotageSignTimer > 0f)
        {
            return;
        }

        aiSabotageSignTimer = Mathf.Max(0.1f, aiSabotageSignInterval);

        if (UnityEngine.Random.value >= aiSabotageSignPlaceChance)
        {
            return;
        }

        Lane currentLane = pathState.CurrentLane;
        if (currentLane == null)
        {
            return;
        }

        TryPlaceRandomSabotageSign(currentLane);
    }

    /// <summary>
    /// プレイヤー入力またはCPUの判断に応じた操作を更新する
    /// </summary>
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

        // レーン変更
        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            TryShiftLane(1);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            TryShiftLane(-1);
        }
    }

    /// <summary>
    /// 交差点手前で標識を参照して進行方向を予約
    /// </summary>
    private void UpdateQueuedTurnDirectionBySign()
    {
        Lane currentLane = pathState.CurrentLane;
        if (currentLane == null)
        {
            return;
        }

        float laneLength = currentLane.Length;
        if (laneLength <= 0f)
        {
            return;
        }

        if (pathState.CurrentS < laneLength - intersectionDecisionDistance)
        {
            return;
        }

        queuedTurnDirection = ResolveTurnDirectionBySign();
    }

    /// <summary>
    /// 簡易 AI 判断処理
    /// - 定期的にレーン移動や交差点での進行方向を決定する
    /// - 障害物を先読みして回避を試みる
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

        TryPlaceIntersectionRoadSign(currentLane);

        // --- 障害物回避 ---
        float lookStart = pathState.CurrentS + obstacleCheckMargin;
        float lookEnd = pathState.CurrentS + Mathf.Max(aiAvoidLookahead, obstacleCheckMargin + 0.01f);

        bool obstacleAhead = currentLane.HasObstacleInRange(lookStart, lookEnd);
        if (obstacleAhead)
        {
            // 左右のレーンをチェックして空いている方へ移動
            Lane leftLane = currentLane.ParentWay != null ? currentLane.ParentWay.GetLane(currentLane.LaneIndex + 1) : null;
            Lane rightLane = currentLane.ParentWay != null ? currentLane.ParentWay.GetLane(currentLane.LaneIndex - 1) : null;

            bool leftClear = leftLane != null && !leftLane.HasObstacleInRange(pathState.CurrentS, pathState.CurrentS + aiAvoidLookahead);
            bool rightClear = rightLane != null && !rightLane.HasObstacleInRange(pathState.CurrentS, pathState.CurrentS + aiAvoidLookahead);

            if (leftClear && rightClear)
            {
                // ランダムで選ぶ
                if (UnityEngine.Random.value < 0.5f)
                {
                    TryShiftLane(1);
                }
                else
                {
                    TryShiftLane(-1);
                }
                return; // 回避優先
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
            // どちらも回避できない場合はそのまま（衝突処理は移動処理側で行う）
        }

        // --- 時々レーン変更（雑な確率） ---
        if (UnityEngine.Random.value < aiLaneShiftChance)
        {
            int dir = UnityEngine.Random.value < 0.5f ? 1 : -1;
            TryShiftLane(dir);
            return;
        }

    }

    /// <summary>
    /// 左右レーンへ即時移動を試行
    /// </summary>
    /// <param name="_laneOffset">-1:左 / +1:右</param>
    private void TryShiftLane(int _laneOffset)
    {
        if (laneTransitionCurve.IsActive)
        {
            return;
        }

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

        // 標識による速度制限を考慮
        float currentMoveSpeed = ResolveMoveSpeedBySign();
        float moveDistance = currentMoveSpeed * Time.deltaTime;

        if (laneTransitionCurve.IsActive)
        {
            AdvanceLaneTransition(moveDistance);
            return;
        }

        float nextS = pathState.CurrentS + moveDistance;

        // 障害物チェック
        LaneObstacle hitObstacle;
        if (currentLane.TryGetObstacleAt(nextS + obstacleCheckMargin, out hitObstacle))
        {
            OnHitObstacle(hitObstacle);
            return;
        }

        // レーンの終端を超える場合、次のレーンに進む
        float laneLength = currentLane.Length;
        if (nextS >= laneLength)
        {
            float remain = nextS - laneLength;
            Lane nextLane = currentLane.GetNextLane(queuedTurnDirection);

            // 次のレーンがない場合 or 標識で進行禁止の場合
            if (nextLane == null || !CanMoveBySign(queuedTurnDirection))
            {
                // 現在のレーンの終端で止まる
                pathState.CurrentS = Mathf.Min(pathState.CurrentS, laneLength);
                return;
            }

            if (useSmoothLaneTransition &&
                laneTransitionCurve.TryBegin(
                    currentLane,
                    nextLane,
                    laneTransitionJoinDistance,
                    laneTransitionMaxHandleLength))
            {
                pathState.CurrentLane = nextLane;
                pathState.CurrentS = 0f;
                queuedTurnDirection = TurnDirection.Straight;
                AdvanceLaneTransition(remain);
                return;
            }

            // 次のレーンへ移動
            pathState.CurrentLane = nextLane;
            pathState.CurrentS = remain;
            queuedTurnDirection = TurnDirection.Straight;

            return;
        }

        // 通常移動
        pathState.CurrentS = nextS;
    }

    /// <summary>
    /// レーン間の接続曲線上を進み、完了後は次のレーン上の移動へ戻す
    /// </summary>
    private void AdvanceLaneTransition(float _moveDistance)
    {
        if (!laneTransitionCurve.Advance(
                _moveDistance,
                out float overflowDistance))
        {
            return;
        }

        pathState.CurrentS =
            laneTransitionCurve.TargetS +
            overflowDistance;
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private void ResetToInitialState()
    {
        laneTransitionCurve.Clear();
        pathState.Reset(initialLane, initialS);
        queuedTurnDirection = TurnDirection.Straight;
        aiDecisionTimer = 0f;
        laneShiftTimer = 0f;
        aiSabotageSignTimer = Mathf.Max(0.1f, aiSabotageSignInterval);
        aiRoadSignHandledLane = null;
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

        Vector3 position;
        Vector3 forward;

        if (laneTransitionCurve.IsActive)
        {
            position = laneTransitionCurve.GetPosition();
            forward = laneTransitionCurve.GetForward();
        }
        else
        {
            // レーン上の位置と方向を取得
            position = currentLane.GetPositionByS(pathState.CurrentS);
            forward = currentLane.GetForwardByS(pathState.CurrentS);
        }

        transform.position = position;

        // 回転
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
        // 障害物を破壊
        if (_hitObstacle)
        {
            Destroy(_hitObstacle.gameObject);
        }
        // 一定時間停止
        isStopping = true;
        stopTimer = obstacleStopDuration;

        pathState.CurrentS = Mathf.Max(0.0f, pathState.CurrentS - 1.0f); // ぶつかり続けないように少し後退
        SyncTransformToLane();



        Debug.Log("障害物に衝突");
    }

    /// <summary>
    /// 進行可能な道がない場合の処理
    /// </summary>
    private void OnInvalidTurn()
    {
        // 初期位置に戻す
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
    /// 外部から CPU を切り替える（Inspector の切り替えでも有効）
    /// </summary>
    public void SetIsCPU(bool cpu)
    {
        isCPU = cpu;
        aiDecisionTimer = 0f;
        aiSabotageSignTimer = Mathf.Max(0.1f, aiSabotageSignInterval);
        aiRoadSignHandledLane = null;
    }

    #region --- CPU標識配置 ---
    /// <summary>
    /// CPUの前方へランダムに選んだ妨害標識を配置する
    /// </summary>
    private void TryPlaceRandomSabotageSign(Lane currentLane)
    {
        if (!TryResolveRandomAISabotageSign(out RoadSignDefinition definition))
        {
            return;
        }

        float targetS = Mathf.Min(
            currentLane.Length,
            pathState.CurrentS + Mathf.Max(0f, aiSabotageSignPlacementDistance));

        TryPlaceAIRoadSign(definition, currentLane, targetS);
    }

    /// <summary>
    /// 交差点手前で進行方向を決定し、必要に応じて方向看板を配置する
    /// </summary>
    private void TryPlaceIntersectionRoadSign(Lane currentLane)
    {
        if (!aiCanPlaceRoadSigns || currentLane == null || aiRoadSignHandledLane == currentLane)
        {
            return;
        }

        float remainingDistance = currentLane.Length - pathState.CurrentS;
        if (remainingDistance > Mathf.Max(0f, aiRoadSignPlacementDistance))
        {
            return;
        }

        aiRoadSignHandledLane = currentLane;

        if (!TryResolveAvailableTurn(currentLane, out TurnDirection turnDirection))
        {
            return;
        }

        queuedTurnDirection = turnDirection;

        if (turnDirection == TurnDirection.Straight)
        {
            return;
        }

        if (!TryResolveAIRoadSign(turnDirection, out RoadSignDefinition definition))
        {
            return;
        }

        TryPlaceAIRoadSign(definition, currentLane, currentLane.Length);
    }

    /// <summary>
    /// 現在のレーンから進める方向を収集し、CPUの進行方向を決定する
    /// </summary>
    private bool TryResolveAvailableTurn(Lane currentLane, out TurnDirection turnDirection)
    {
        turnDirection = TurnDirection.Straight;
        aiAvailableTurnBuffer.Clear();

        AddAvailableTurn(currentLane, TurnDirection.Straight);
        AddAvailableTurn(currentLane, TurnDirection.Left);
        AddAvailableTurn(currentLane, TurnDirection.Right);

        if (aiAvailableTurnBuffer.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, aiAvailableTurnBuffer.Count);
            turnDirection = aiAvailableTurnBuffer[index];
            return true;
        }

        if (currentLane.GetNextLane(TurnDirection.Back) != null
            && TryResolveAIRoadSign(TurnDirection.Back, out _))
        {
            turnDirection = TurnDirection.Back;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 道と対応看板が存在する進行方向を選択候補へ追加する
    /// </summary>
    private void AddAvailableTurn(Lane currentLane, TurnDirection turnDirection)
    {
        if (currentLane.GetNextLane(turnDirection) == null)
        {
            return;
        }

        if (turnDirection != TurnDirection.Straight
            && !TryResolveAIRoadSign(turnDirection, out _))
        {
            return;
        }

        aiAvailableTurnBuffer.Add(turnDirection);
    }

    /// <summary>
    /// 指定された進行方向に対応するCPU用看板定義を検索する
    /// </summary>
    private bool TryResolveAIRoadSign(TurnDirection turnDirection, out RoadSignDefinition definition)
    {
        definition = null;
        CollectAIRoadSignCandidates();

        if (aiRoadSignCandidateBuffer.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < aiRoadSignCandidateBuffer.Count; i++)
        {
            RoadSignDefinition candidate = aiRoadSignCandidateBuffer[i];
            if (TryGetForcedDirection(candidate, out TurnDirection candidateDirection)
                && candidateDirection == turnDirection)
            {
                definition = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// CPUが利用できる妨害標識からランダムに1つ選択する
    /// </summary>
    private bool TryResolveRandomAISabotageSign(out RoadSignDefinition definition)
    {
        definition = null;
        aiSabotageSignCandidateBuffer.Clear();
        CollectAIRoadSignCandidates();

        for (int i = 0; i < aiRoadSignCandidateBuffer.Count; i++)
        {
            RoadSignDefinition candidate = aiRoadSignCandidateBuffer[i];
            if (IsSabotageRoadSign(candidate))
            {
                aiSabotageSignCandidateBuffer.Add(candidate);
            }
        }

        if (aiSabotageSignCandidateBuffer.Count == 0)
        {
            return false;
        }

        int index = UnityEngine.Random.Range(0, aiSabotageSignCandidateBuffer.Count);
        definition = aiSabotageSignCandidateBuffer[index];
        return true;
    }

    /// <summary>
    /// Inspector設定またはシーン内の手札からCPU用標識候補を収集する
    /// </summary>
    private void CollectAIRoadSignCandidates()
    {
        aiRoadSignCandidateBuffer.Clear();

        if (aiRoadSignDefinitions != null)
        {
            for (int i = 0; i < aiRoadSignDefinitions.Count; i++)
            {
                AddAIRoadSignCandidate(aiRoadSignDefinitions[i]);
            }
        }

        if (aiRoadSignCandidateBuffer.Count == 0)
        {
            AddAIRoadSignCandidatesFromHand();
        }
    }

    /// <summary>
    /// シーン内の手札からCPUが使用できる看板候補を収集する
    /// </summary>
    private void AddAIRoadSignCandidatesFromHand()
    {
        if (aiRoadSignSourceHand == null)
        {
            aiRoadSignSourceHand = GetComponentInChildren<RoadSignHandController>(true);
        }

        if (aiRoadSignSourceHand == null)
        {
            aiRoadSignSourceHand = FindFirstObjectByType<RoadSignHandController>();
        }

        if (aiRoadSignSourceHand == null)
        {
            return;
        }

        IReadOnlyList<RoadSignHandEntry> entries = aiRoadSignSourceHand.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            RoadSignHandEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            AddAIRoadSignCandidate(entry.Definition);
        }
    }

    /// <summary>
    /// 使用可能な看板定義を重複しないよう候補へ追加する
    /// </summary>
    private void AddAIRoadSignCandidate(RoadSignDefinition candidate)
    {
        if (candidate == null || candidate.SignPrefab == null)
        {
            return;
        }

        if (aiRoadSignCandidateBuffer.Contains(candidate))
        {
            return;
        }

        aiRoadSignCandidateBuffer.Add(candidate);
    }

    /// <summary>
    /// 標識定義が所有者以外へ作用する妨害効果を持つか判定する
    /// </summary>
    private bool IsSabotageRoadSign(RoadSignDefinition definition)
    {
        if (definition == null || definition.Effects == null)
        {
            return false;
        }

        IReadOnlyList<RoadSignEffectAsset> effects = definition.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            RoadSignEffectAsset effect = effects[i];
            if (effect != null && effect.Target == RoadSignEffectTarget.NonOwnerOnly)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 看板定義から強制される進行方向を取得する
    /// </summary>
    private bool TryGetForcedDirection(RoadSignDefinition definition, out TurnDirection direction)
    {
        direction = TurnDirection.Straight;
        if (definition == null || definition.Effects == null)
        {
            return false;
        }

        IReadOnlyList<RoadSignEffectAsset> effects = definition.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] is ForceDirectionEffectAsset forceDirectionEffect)
            {
                direction = forceDirectionEffect.Direction;
                return direction != TurnDirection.Straight;
            }
        }

        return false;
    }

    /// <summary>
    /// 指定されたレーン位置へCPU所有の標識を生成する
    /// </summary>
    private bool TryPlaceAIRoadSign(RoadSignDefinition definition, Lane lane, float targetS)
    {
        if (definition == null || lane == null || definition.SignPrefab == null)
        {
            return false;
        }

        RoadSign signPrefab = definition.SignPrefab;
        Vector3 lanePosition = lane.GetPositionByS(targetS);
        Vector3 placePosition = aiRoadSignGridSize > 0f
            ? SnapAIRoadSignPosition(lanePosition)
            : lanePosition;

        placePosition.y = ResolveAIRoadSignGroundHeight(placePosition, lanePosition.y);

        if (HasRoadSignNear(placePosition))
        {
            return false;
        }

        Vector3 forward = lane.GetForwardByS(targetS);
        forward.y = 0f;
        if (forward.sqrMagnitude <= 1e-6f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        Quaternion rotation = forward.sqrMagnitude > 1e-6f
            ? Quaternion.LookRotation(forward.normalized, Vector3.up)
            : signPrefab.transform.rotation;

        RoadSign instance = Instantiate(signPrefab, placePosition, rotation);
        instance.SetDefinition(definition);
        instance.SetOwner(gameObject);
        return true;
    }

    /// <summary>
    /// 看板の配置位置を指定されたグリッド間隔へ揃える
    /// </summary>
    private Vector3 SnapAIRoadSignPosition(Vector3 worldPosition)
    {
        float gridSize = Mathf.Max(0.01f, aiRoadSignGridSize);
        float snappedX = Mathf.Round(worldPosition.x / gridSize) * gridSize;
        float snappedZ = Mathf.Round(worldPosition.z / gridSize) * gridSize;
        return new Vector3(snappedX, worldPosition.y, snappedZ);
    }

    /// <summary>
    /// レイキャストで看板を配置する地面の高さを取得する
    /// </summary>
    private float ResolveAIRoadSignGroundHeight(Vector3 position, float fallbackHeight)
    {
        float rayHeight = Mathf.Max(0.1f, aiRoadSignGroundRaycastHeight);
        Vector3 origin = position + Vector3.up * rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayHeight * 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return fallbackHeight;
    }

    /// <summary>
    /// 配置予定位置の近くに既存の看板があるか確認する
    /// </summary>
    private bool HasRoadSignNear(Vector3 position)
    {
        if (aiRoadSignDuplicateCheckRadius <= 0f)
        {
            return false;
        }

        Collider[] colliders = Physics.OverlapSphere(
            position,
            aiRoadSignDuplicateCheckRadius,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider hit = colliders[i];
            if (hit != null && hit.GetComponentInParent<RoadSign>() != null)
            {
                return true;
            }
        }

        return false;
    }
    #endregion --- CPU標識配置 ---


    #region --- 標識関連 ---
    /// <summary>
    /// 標識の評価を行う
    /// </summary>
    private RoadSignEvaluation EvaluateSigns(TurnDirection _intendedDirection)
    {
        var context = new RoadSignQueryContext
        {
            Actor = this.gameObject,
            WorldPosition = transform.position,
            IntendedDirection = _intendedDirection,
            CurrentSpeed = moveSpeed
        };

        if (signReceiver == null) return new RoadSignEvaluation();

        return signReceiver.Evaluate(context);
    }

    /// <summary>
    /// 標識を考慮した移動速度を返す
    /// </summary>
    private float ResolveMoveSpeedBySign()
    {
        RoadSignEvaluation evaluation = EvaluateSigns(queuedTurnDirection);
        return signResolver.ResolveMoveSpeed(evaluation, moveSpeed);
    }

    /// <summary>
    /// 標識を考慮して指定方向に進めるか判定する
    /// </summary>
    private bool CanMoveBySign(TurnDirection _direction)
    {
        if (signReceiver == null)
        {
            return true;
        }
        RoadSignEvaluation evaluation = EvaluateSigns(_direction);
        return signResolver.CanMove(evaluation, _direction);
    }

    /// <summary>
    /// 標識を考慮して進行方向を決定する
    /// </summary>

    private TurnDirection ResolveTurnDirectionBySign()
    {
        RoadSignEvaluation evaluation = EvaluateSigns(queuedTurnDirection);
        return signResolver.ResolveTurnDirection(evaluation, queuedTurnDirection);
    }

    /// <summary>
    /// 標識による停止効果を確認し、必要に応じてプレイヤーを停止させる
    /// </summary>

    private void ApplyStopBySign()
    {
        RoadSignEvaluation evaluation = EvaluateSigns(queuedTurnDirection);

        if (!signResolver.RequiresStop(evaluation)) return;

        isStopping = true;
        stopTimer = obstacleStopDuration;
    }
    #endregion --- 標識関連 ---
}
