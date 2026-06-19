using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面右上に円形ミニマップを自動生成するコントローラー。
/// シーン内の任意のGameObjectにアタッチするだけで動作する。
/// 追跡したいオブジェクトにはMinimapMarkerを付ける。
/// </summary>
public class MinimapController : MonoBehaviour
{
    [Header("追跡対象")]
    [Tooltip("プレイヤーのTransform（アイコン向き表示に使用）")]
    [SerializeField] private Transform playerTarget;

    [Header("カメラ設定")]
    [Tooltip("マップ全体を映す固定カメラの中心（ワールド座標）。AutoFit が有効なら自動計算。")]
    [SerializeField] private Vector3 mapCenter = Vector3.zero;
    [Tooltip("直交投影の範囲（大きいほど広く映る）。AutoFit が有効なら自動計算。")]
    [SerializeField] private float orthographicSize = 100f;
    [Tooltip("カメラの高さ（Y軸）")]
    [SerializeField] private float cameraHeight = 200f;
    [Tooltip("シーン内の MinimapMarker から自動でカメラ範囲を計算する")]
    [SerializeField] private bool autoFit = true;
    [Tooltip("AutoFit 時の余白倍率（1.1 = 10% 余白）")]
    [SerializeField] private float autoFitPadding = 1.15f;

    [Header("UI設定")]
    [Tooltip("ミニマップの直径（ピクセル）")]
    [SerializeField] private float minimapDiameter = 200f;
    [Tooltip("画面右上端からのマージン（ピクセル）")]
    [SerializeField] private float margin = 20f;
    [Tooltip("ボーダーリングの太さ（ピクセル）")]
    [SerializeField] private float borderWidth = 5f;
    [Tooltip("ボーダーの色")]
    [SerializeField] private Color borderColor = new Color(0.85f, 0.72f, 0.2f);

    [Header("アイコン設定")]
    [SerializeField] private Color playerColor = new Color(1f, 0.9f, 0f);
    [SerializeField] private Color cpuColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color goalColor = new Color(0f, 0.9f, 1f);
    [SerializeField] private float playerIconSize = 16f;
    [SerializeField] private float cpuIconSize = 10f;
    [SerializeField] private float goalIconSize = 12f;
    [Tooltip("ゴールが視野外のとき、円周上に方向マーカーを表示する")]
    [SerializeField] private bool showGoalDirectionOnEdge = true;
    [SerializeField] private float edgeMarkerSize = 10f;

    [Header("マーカー更新間隔（秒）")]
    [SerializeField] private float markerRefreshInterval = 1f;

    private Camera minimapCam;
    private RenderTexture renderTexture;
    private Canvas canvas;
    private RectTransform minimapRoot;
    // プレイヤーも他マーカーと同じく動くアイコンとして管理
    private RectTransform playerIcon;
    private readonly List<(MinimapMarker marker, RectTransform icon, RectTransform edgeIcon)> trackedIcons = new();
    private float nextRefreshTime;
    private float radius;

    private void Awake()
    {
        radius = minimapDiameter * 0.5f;
        CreateCamera();
        CreateUI();
    }

    private void Start()
    {
        if (autoFit)
            FitCameraToMarkers();
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void LateUpdate()
    {
        UpdateIconPositions();

        if (Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + markerRefreshInterval;
            RefreshMarkers();
            if (autoFit)
                FitCameraToMarkers();
        }
    }

    // ────────── 初期化 ──────────

    private void CreateCamera()
    {
        renderTexture = new RenderTexture(512, 512, 16) { name = "MinimapRT", antiAliasing = 1 };
        renderTexture.Create();

        GameObject camGO = new GameObject("MinimapCamera");
        camGO.transform.SetParent(transform);

        minimapCam = camGO.AddComponent<Camera>();
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = orthographicSize;
        minimapCam.transform.position = new Vector3(mapCenter.x, cameraHeight, mapCenter.z);
        minimapCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0.08f, 0.12f, 0.08f);
        minimapCam.cullingMask = ~0;
        minimapCam.targetTexture = renderTexture;
        minimapCam.depth = -10f;
        minimapCam.farClipPlane = cameraHeight + 100f;
        minimapCam.nearClipPlane = 0.1f;
    }

    private void CreateUI()
    {
        GameObject canvasGO = new GameObject("MinimapCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // ボーダーリング
        float borderDiameter = minimapDiameter + borderWidth * 2f;
        GameObject borderGO = new GameObject("MinimapBorder");
        borderGO.transform.SetParent(canvasGO.transform, false);
        RectTransform borderRect = borderGO.AddComponent<RectTransform>();
        SetTopRightAnchor(borderRect);
        borderRect.sizeDelta = new Vector2(borderDiameter, borderDiameter);
        borderRect.anchoredPosition = new Vector2(-(margin + radius), -(margin + radius));
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.sprite = CreateCircleSprite(128, true, borderWidth / (borderDiameter * 0.5f));
        borderImg.color = borderColor;
        borderImg.raycastTarget = false;

        // 円形マスク
        GameObject maskGO = new GameObject("MinimapMask");
        maskGO.transform.SetParent(canvasGO.transform, false);
        RectTransform maskRect = maskGO.AddComponent<RectTransform>();
        SetTopRightAnchor(maskRect);
        maskRect.sizeDelta = new Vector2(minimapDiameter, minimapDiameter);
        maskRect.anchoredPosition = new Vector2(-(margin + radius), -(margin + radius));
        Image maskImg = maskGO.AddComponent<Image>();
        maskImg.sprite = CreateCircleSprite(128, false, 0f);
        maskImg.color = Color.white;
        maskImg.raycastTarget = false;
        Mask mask = maskGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // RawImage（マスク内）
        GameObject mapGO = new GameObject("MinimapImage");
        mapGO.transform.SetParent(maskGO.transform, false);
        RectTransform mapRect = mapGO.AddComponent<RectTransform>();
        mapRect.anchorMin = Vector2.zero;
        mapRect.anchorMax = Vector2.one;
        mapRect.offsetMin = Vector2.zero;
        mapRect.offsetMax = Vector2.zero;
        RawImage rawImg = mapGO.AddComponent<RawImage>();
        rawImg.texture = renderTexture;
        rawImg.raycastTarget = false;

        // アイコン用ルートRect
        GameObject rootGO = new GameObject("MinimapRoot");
        rootGO.transform.SetParent(canvasGO.transform, false);
        minimapRoot = rootGO.AddComponent<RectTransform>();
        SetTopRightAnchor(minimapRoot);
        minimapRoot.sizeDelta = new Vector2(minimapDiameter, minimapDiameter);
        minimapRoot.anchoredPosition = new Vector2(-(margin + radius), -(margin + radius));

        // プレイヤーアイコン（三角形・動く）
        playerIcon = CreateDotIcon(minimapRoot, playerColor, playerIconSize, true);
        playerIcon.gameObject.SetActive(false);
    }

    // ────────── AutoFit ──────────

    private void FitCameraToMarkers()
    {
        if (minimapCam == null) return;

        // MinimapMarker 全体の AABB を計算
        MinimapMarker[] all = FindObjectsByType<MinimapMarker>(FindObjectsSortMode.None);
        if (all.Length == 0) return;

        Vector3 min = new Vector3(float.MaxValue, 0, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, 0, float.MinValue);
        foreach (var m in all)
        {
            Vector3 p = m.transform.position;
            if (p.x < min.x) min.x = p.x;
            if (p.z < min.z) min.z = p.z;
            if (p.x > max.x) max.x = p.x;
            if (p.z > max.z) max.z = p.z;
        }

        Vector3 center = (min + max) * 0.5f;
        float halfW = (max.x - min.x) * 0.5f * autoFitPadding;
        float halfH = (max.z - min.z) * 0.5f * autoFitPadding;
        float size = Mathf.Max(halfW, halfH, 10f);

        minimapCam.transform.position = new Vector3(center.x, cameraHeight, center.z);
        minimapCam.orthographicSize = size;
    }

    // ────────── 毎フレーム更新 ──────────

    private void UpdateIconPositions()
    {
        if (minimapCam == null || minimapRoot == null) return;

        // プレイヤーアイコン
        if (playerIcon != null && playerTarget != null)
        {
            Vector3 vp = minimapCam.WorldToViewportPoint(playerTarget.position);
            bool inBounds = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            Vector2 localPos = new Vector2((vp.x - 0.5f) * minimapDiameter, (vp.y - 0.5f) * minimapDiameter);

            if (inBounds && localPos.magnitude <= radius - playerIconSize * 0.5f)
            {
                playerIcon.gameObject.SetActive(true);
                playerIcon.anchoredPosition = localPos;
                float angle = -playerTarget.eulerAngles.y;
                playerIcon.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                playerIcon.gameObject.SetActive(false);
            }
        }

        // その他マーカー
        foreach (var (marker, icon, edgeIcon) in trackedIcons)
        {
            if (marker == null) continue;

            Vector3 vp = minimapCam.WorldToViewportPoint(marker.transform.position);
            bool inFront = vp.z > 0f;
            bool inBounds = inFront && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

            if (inBounds)
            {
                Vector2 localPos = new Vector2(
                    (vp.x - 0.5f) * minimapDiameter,
                    (vp.y - 0.5f) * minimapDiameter);

                bool withinCircle = localPos.magnitude <= radius - goalIconSize * 0.5f;
                icon.gameObject.SetActive(withinCircle);
                if (withinCircle) icon.anchoredPosition = localPos;
                if (edgeIcon != null) edgeIcon.gameObject.SetActive(false);
            }
            else
            {
                icon.gameObject.SetActive(false);

                if (edgeIcon != null && showGoalDirectionOnEdge &&
                    marker.markerType == MinimapMarker.MarkerType.Goal)
                {
                    Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                    if (!inFront) dir = -dir;
                    dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.up;

                    edgeIcon.gameObject.SetActive(true);
                    edgeIcon.anchoredPosition = dir * (radius - edgeMarkerSize * 0.5f - 2f);
                    float edgeAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                    edgeIcon.localRotation = Quaternion.Euler(0f, 0f, edgeAngle);
                }
                else if (edgeIcon != null)
                {
                    edgeIcon.gameObject.SetActive(false);
                }
            }
        }
    }

    // ────────── マーカー管理 ──────────

    private void RefreshMarkers()
    {
        if (canvas == null || minimapRoot == null) return;

        MinimapMarker[] allMarkers = FindObjectsByType<MinimapMarker>(FindObjectsSortMode.None);

        for (int i = trackedIcons.Count - 1; i >= 0; i--)
        {
            if (trackedIcons[i].marker == null)
            {
                if (trackedIcons[i].icon != null) Destroy(trackedIcons[i].icon.gameObject);
                if (trackedIcons[i].edgeIcon != null) Destroy(trackedIcons[i].edgeIcon.gameObject);
                trackedIcons.RemoveAt(i);
            }
        }

        HashSet<MinimapMarker> existing = new HashSet<MinimapMarker>();
        foreach (var (m, _, _) in trackedIcons)
            if (m != null) existing.Add(m);

        foreach (MinimapMarker marker in allMarkers)
        {
            if (marker == null) continue;
            if (marker.markerType == MinimapMarker.MarkerType.Player) continue;
            if (existing.Contains(marker)) continue;

            bool isGoal = marker.markerType == MinimapMarker.MarkerType.Goal;
            Color color = isGoal ? goalColor : cpuColor;
            float size = isGoal ? goalIconSize : cpuIconSize;

            RectTransform icon = CreateDotIcon(minimapRoot, color, size, false);
            RectTransform edgeIcon = (isGoal && showGoalDirectionOnEdge)
                ? CreateArrowIcon(minimapRoot, goalColor, edgeMarkerSize)
                : null;

            trackedIcons.Add((marker, icon, edgeIcon));
        }
    }

    // ────────── アイコン生成 ──────────

    private RectTransform CreateDotIcon(RectTransform parent, Color color, float size, bool isTriangle)
    {
        GameObject go = new GameObject(isTriangle ? "PlayerIcon" : "DotIcon");
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = isTriangle ? CreateTriangleSprite() : CreateCircleSprite(32, false, 0f);
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    private RectTransform CreateArrowIcon(RectTransform parent, Color color, float size)
    {
        GameObject go = new GameObject("EdgeArrow");
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = CreateTriangleSprite();
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.gameObject.SetActive(false);
        return rt;
    }

    // ────────── テクスチャ生成 ──────────

    private static Sprite CreateCircleSprite(int texSize, bool ringOnly, float ringRatio)
    {
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = texSize * 0.5f;
        float outerR = center - 1f;
        float innerR = ringOnly ? outerR * (1f - ringRatio) : 0f;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                float alpha = 0f;
                if (dist <= outerR && (!ringOnly || dist >= innerR))
                {
                    float aaOuter = Mathf.Clamp01(outerR - dist);
                    float aaInner = ringOnly ? Mathf.Clamp01(dist - innerR) : 1f;
                    alpha = Mathf.Min(aaOuter, aaInner);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateTriangleSprite()
    {
        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)(size - 1);
                float ny = y / (float)(size - 1);
                float halfWidth = (1f - ny) * 0.5f;
                bool inside = nx >= 0.5f - halfWidth && nx <= 0.5f + halfWidth;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static void SetTopRightAnchor(RectTransform rt)
    {
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
