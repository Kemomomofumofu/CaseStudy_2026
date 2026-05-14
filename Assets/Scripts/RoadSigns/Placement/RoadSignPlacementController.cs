using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class RoadSignPlacementController : MonoBehaviour
{
    [Header("配置設定")]
    [SerializeField] private Camera targetCamera = null;
    [SerializeField] private LayerMask placementLayers = ~0;
    [SerializeField] private float placementYOffset = 0.0f;

    [Header("手札管理")]
    [SerializeField] private RoadSignHandController handController = null;

    private readonly Dictionary<Type, RoadSignBase> placedByType = new();

    /// <summary>
    /// カメラ未指定時に MainCamera を取得する
    /// </summary>
    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    /// <summary>
    /// クリック位置に標識を配置する
    /// </summary>
    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (targetCamera == null)
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, placementLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (!TryResolveSelectedSign(out RoadSignDefinition definition, out RoadSignBase signPrefab, out bool overrideDirection, out TurnDirection direction))
        {
            return;
        }

        Vector3 position = hit.point + Vector3.up * placementYOffset;
        Quaternion rotation = signPrefab.transform.rotation;

        RoadSignBase instance = Instantiate(signPrefab, position, rotation);
        ApplyDefinition(instance, definition, overrideDirection, direction);

        Type signType = signPrefab.GetType();
        if (placedByType.TryGetValue(signType, out RoadSignBase existing) && existing != null)
        {
            Destroy(existing.gameObject);
        }

        placedByType[signType] = instance;
    }

    private static void ApplyDefinition(RoadSignBase _instance, RoadSignDefinition _definition, bool _overrideDirection, TurnDirection _direction)
    {
        if (_overrideDirection)
        {
            if (_instance.TryGetComponent(out ForceDirectionSign forceSign))
            {
                forceSign.Configure(_direction);
            }

            if (_instance.TryGetComponent(out BlockDirectionSign blockSign))
            {
                blockSign.Configure(_direction);
            }
        }

        var data = _definition.EffectData;
        if (data == null)
        {
            return;
        }

        if (_instance.TryGetComponent(out SpeedLimitSign speedLimitSign))
        {
            speedLimitSign.Configure(data.FloatValue);
        }

        if (_instance.TryGetComponent(out AccelerationSign accelerationSign))
        {
            accelerationSign.Configure(data.FloatValue);
        }

        if (_instance.TryGetComponent(out LaneReductionSign laneReductionSign))
        {
            laneReductionSign.Configure(data.IntValue);
        }
    }

    /// <summary>
    /// 選択中の標識を取得する
    /// </summary>
    private bool TryResolveSelectedSign(out RoadSignDefinition _definition, out RoadSignBase _signPrefab, out bool _overrideDirection, out TurnDirection _direction)
    {
        _definition = null;
        _signPrefab = null;
        _overrideDirection = false;
        _direction = TurnDirection.Straight;

        if (handController == null)
        {
            return false;
        }

        return handController.TryConsumeSelected(out _definition, out _signPrefab, out _overrideDirection, out _direction);
    }
}