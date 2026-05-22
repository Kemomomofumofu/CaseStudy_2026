using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class RoadSignPlacementController : MonoBehaviour
{
    [Header("配置設定")]
    [SerializeField] private Camera targetCamera = null;
    [SerializeField] private LayerMask placementLayers = ~0;
    [SerializeField] private float placementYOffset = 0.0f;
    [SerializeField] private Transform placementForwardSource = null;

    [Header("手札管理")]
    [SerializeField] private RoadSignHandController handController = null;

    /// <summary>
    /// カメラ未指定時に MainCamera を取得する
    /// </summary>
    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    /// <summary>
    /// クリック位置に標識を配置する
    /// </summary>  
    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (targetCamera == null) return;

        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, placementLayers, QueryTriggerInteraction.Ignore)) return;

        if (!TryResolveSelectedSign(out RoadSignDefinition definition, out RoadSign signPrefab)) return;

        Vector3 position = hit.point + Vector3.up * placementYOffset;   
        Quaternion rotation = ResolvePlacementRotation(signPrefab);

        RoadSign instance = Instantiate(signPrefab, position, rotation);
        instance.SetDefinition(definition);
    }

    /// <summary>
    /// 配置する標識の回転を決定する
    /// </summary>
    private Quaternion ResolvePlacementRotation(RoadSign _signPrefab)
    {
        if (placementForwardSource == null)
        {
            return _signPrefab.transform.rotation;
        }

        Vector3 forward = placementForwardSource.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 1e-6f)
        {
            return _signPrefab.transform.rotation;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    /// <summary>
    /// 選択中の標識を取得する
    /// </summary>
    private bool TryResolveSelectedSign(out RoadSignDefinition _definition, out RoadSign _signPrefab)
    {
        _definition = null;
        _signPrefab = null;

        if (handController == null) return false;

        return handController.TryConsumeSelected(out _definition, out _signPrefab);
    }
}