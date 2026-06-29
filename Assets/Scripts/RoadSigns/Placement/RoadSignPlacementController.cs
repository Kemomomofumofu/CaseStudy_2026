using UnityEngine;

/// <summary>
/// 選択中の道路標識をグリッド上へ配置する
/// </summary>
public sealed class RoadSignPlacementController : MonoBehaviour
{
    [Header("配置設定")]
    [SerializeField] private Transform placementForwardSource = null;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;
    [SerializeField] private float gridSize = 1.0f;
    [SerializeField] private float groundRaycastHeight = 5.0f;

    [Header("UI連携")]
    [SerializeField] private RoadSignHandController handController = null;
        
    [Header("Gizmos")]
    [SerializeField] private int gridGizmoExtent = 5;
    [SerializeField] private float gridGizmoHeight = 0.02f;
    [SerializeField] private float gridGizmoRadius = 0.05f;

    /// <summary>
    /// 選択中の標識を所有者の前方グリッドへ配置する
    /// </summary>
    public void PlaceSelectedAtForwardGrid()
    {   
        if (gridSize <= 0f)
        {
            return;
        }

        if (!TryResolveSelectedSign(out RoadSignDefinition definition, out RoadSign signPrefab))
        {
            return;
        }

        Vector3 basePoint = placementForwardSource != null ? placementForwardSource.position : transform.position;

        Vector3 snappedPosition = ResolvePlacementPosition(basePoint);

        Quaternion rotation = ResolvePlacementRotation(signPrefab);

        RoadSign instance = Instantiate(signPrefab, snappedPosition, rotation);
        instance.SetDefinition(definition);

        GameObject owner = ResolveOwner();
        instance.SetOwner(owner);
    }

    /// <summary>
    /// 前方位置をグリッドと地面の高さに合わせて配置位置を決定する
    /// </summary>
    private Vector3 ResolvePlacementPosition(Vector3 hitPoint)
    {
        Vector3 target = hitPoint;

        if (TryGetForwardDirection(out Vector3 forward))
        {
            target = placementForwardSource.position + forward.normalized * gridSize;
            target.y = hitPoint.y;
        }

        Vector3 snapped = SnapToGrid(target);
        snapped.y = ResolveGroundHeight(snapped, hitPoint.y);
        return snapped;
    }

    /// <summary>
    /// レイキャストで配置位置の地面の高さを取得する
    /// </summary>
    private float ResolveGroundHeight(Vector3 position, float fallbackHeight)
    {
        float rayHeight = Mathf.Max(0.1f, groundRaycastHeight);
        Vector3 origin = position + Vector3.up * rayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayHeight * 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return fallbackHeight;
    }

    /// <summary>
    /// ワールド座標を設定されたグリッド間隔へ揃える
    /// </summary>
    private Vector3 SnapToGrid(Vector3 worldPosition)
    {
        Vector3 offset = worldPosition - gridOrigin;
        float snappedX = Mathf.Round(offset.x / gridSize) * gridSize + gridOrigin.x;
        float snappedZ = Mathf.Round(offset.z / gridSize) * gridSize + gridOrigin.z;
        Vector3 snapped = new Vector3(snappedX, worldPosition.y, snappedZ);
        return snapped;
    }

    /// <summary>
    /// 配置元の前方を基準に標識の向きを決定する
    /// </summary>
    private Quaternion ResolvePlacementRotation(RoadSign signPrefab)
    {
        if (signPrefab == null)
        {
            return Quaternion.identity;
        }

        if (!TryGetForwardDirection(out Vector3 forward))
        {
            return signPrefab.transform.rotation;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    /// <summary>
    /// 標識を配置したプレイヤーを所有者として取得する
    /// </summary>
    private GameObject ResolveOwner()
    {
        if (placementForwardSource != null)
        {
            PlayerController forwardOwner = placementForwardSource.GetComponentInParent<PlayerController>();
            if (forwardOwner != null)
            {
                return forwardOwner.gameObject;
            }
        }

        PlayerController owner = GetComponentInParent<PlayerController>();
        if (owner != null)
        {
            return owner.gameObject;
        }

        return null;
    }

    /// <summary>
    /// 配置元から水平な前方方向を取得する
    /// </summary>
    private bool TryGetForwardDirection(out Vector3 forward)
    {
        forward = Vector3.zero;
        if (placementForwardSource == null)
        {
            return false;
        }

        forward = placementForwardSource.forward;
        forward.y = 0f;
        bool isValid = forward.sqrMagnitude > 1e-6f;
        return isValid;
    }

    /// <summary>
    /// 選択中の手札を消費して標識定義とPrefabを取得する
    /// </summary>
    private bool TryResolveSelectedSign(out RoadSignDefinition definition, out RoadSign signPrefab)
    {
        definition = null;
        signPrefab = null;

        if (handController == null)
        {
            return false;
        }

        bool resolved = handController.TryConsumeSelected(out definition, out signPrefab);
        return resolved;
    }

    /// <summary>
    /// 選択中のオブジェクト周辺へ配置グリッドのGizmoを描画する
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Color prevColor = Gizmos.color;

        if (gridSize > 0f && gridGizmoExtent > 0)
        {
            Gizmos.color = Color.cyan;
            for (int x = -gridGizmoExtent; x <= gridGizmoExtent; x++)
            {
                for (int z = -gridGizmoExtent; z <= gridGizmoExtent; z++)
                {
                    Vector3 position = new Vector3(
                        gridOrigin.x + x * gridSize,
                        gridOrigin.y,
                        gridOrigin.z + z * gridSize);
                    Vector3 size = new Vector3(gridGizmoRadius, gridGizmoHeight, gridGizmoRadius);
                    Gizmos.DrawCube(position, size);
                }
            }
        }

        Gizmos.color = prevColor;
    }
}
