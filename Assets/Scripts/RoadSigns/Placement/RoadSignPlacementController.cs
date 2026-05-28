using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class RoadSignPlacementController : MonoBehaviour
{
    [Header("配置設定")]
    [SerializeField] private Camera targetCamera = null;
    [SerializeField] private LayerMask placementLayers = ~0;
    [SerializeField] private Transform placementForwardSource = null;
    [SerializeField] private Transform snapPointRoot = null;
    [SerializeField] private float maxSnapDistance = 2.0f;

    [Header("UI連携")]
    [SerializeField] private RoadSignHandController handController = null;

    [Header("Gizmos")]
    [SerializeField] private float snapPointGizmoRadius = 0.15f;
    [SerializeField] private bool drawSnapRange = true;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (targetCamera == null || snapPointRoot == null) return;

        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, placementLayers, QueryTriggerInteraction.Ignore)) return;
        if (!TryResolveSelectedSign(out RoadSignDefinition definition, out RoadSign signPrefab)) return;
        if (!TryFindNearestSnapPoint(hit.point, out Transform nearestSnapPoint)) return;

        Quaternion rotation = ResolvePlacementRotation(signPrefab);
        RoadSign instance = Instantiate(signPrefab, nearestSnapPoint.position, rotation);
        instance.SetDefinition(definition);
    }

    private bool TryFindNearestSnapPoint(Vector3 worldPosition, out Transform nearest)
    {
        nearest = null;
        if (snapPointRoot == null) return false;

        float maxDistanceSqr = maxSnapDistance * maxSnapDistance;
        float nearestDistanceSqr = maxDistanceSqr;

        int childCount = snapPointRoot.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = snapPointRoot.GetChild(i);
            Vector3 delta = child.position - worldPosition;
            float distanceSqr = delta.sqrMagnitude;
            if (distanceSqr > nearestDistanceSqr) continue;

            nearest = child;
            nearestDistanceSqr = distanceSqr;
        }

        return nearest != null;
    }

    private Quaternion ResolvePlacementRotation(RoadSign signPrefab)
    {
        if (signPrefab == null) return Quaternion.identity;

        if (placementForwardSource == null)
        {
            return signPrefab.transform.rotation;
        }

        Vector3 forward = placementForwardSource.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 1e-6f)
        {
            return signPrefab.transform.rotation;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private bool TryResolveSelectedSign(out RoadSignDefinition definition, out RoadSign signPrefab)
    {
        definition = null;
        signPrefab = null;

        if (handController == null) return false;

        return handController.TryConsumeSelected(out definition, out signPrefab);
    }

    private void OnDrawGizmosSelected()
    {
        if (snapPointRoot == null) return;

        Color prevColor = Gizmos.color;

        int childCount = snapPointRoot.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = snapPointRoot.GetChild(i);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(child.position, snapPointGizmoRadius);

            if (!drawSnapRange) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(child.position, maxSnapDistance);
        }

        Gizmos.color = prevColor;
    }
}
