using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class PlayerPathDebugOverlay : MonoBehaviour
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private bool showHud = true;
    [SerializeField] private bool logLaneChanges = true;
    [SerializeField] private float jumpWarningDistance = 8.0f;
    [SerializeField] private Vector2 hudPosition = new(12f, 12f);
    [SerializeField] private Vector2 hudSize = new(620f, 230f);

    private static FieldInfo pathStateField;
    private static FieldInfo laneLinksField;

    private Lane lastLane;
    private float lastS;
    private bool hasLastSnapshot;
    private string lastChangeText = "(none)";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeOverlay()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (FindFirstObjectByType<PlayerPathDebugOverlay>() != null)
        {
            return;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            return;
        }

        GameObject go = new("PlayerPathDebugOverlay");
        DontDestroyOnLoad(go);
        go.AddComponent<PlayerPathDebugOverlay>().playerController = player;
#endif
    }

    private void Update()
    {
        if (playerController == null && autoFindPlayer)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerController == null)
        {
            return;
        }

        PlayerPathState pathState = GetPathState(playerController);
        Lane currentLane = pathState != null ? pathState.CurrentLane : null;
        float currentS = pathState != null ? pathState.CurrentS : 0f;

        if (hasLastSnapshot && currentLane != lastLane)
        {
            LogLaneChange(lastLane, currentLane, lastS, currentS);
        }

        lastLane = currentLane;
        lastS = currentS;
        hasLastSnapshot = true;
    }

    private void OnGUI()
    {
        if (!showHud)
        {
            return;
        }

        if (playerController == null && autoFindPlayer)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerController == null)
        {
            return;
        }

        Rect area = new(hudPosition.x, hudPosition.y, hudSize.x, hudSize.y);
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label("Player Path Debug");

        PlayerPathState pathState = GetPathState(playerController);
        Lane currentLane = pathState != null ? pathState.CurrentLane : null;
        float currentS = pathState != null ? pathState.CurrentS : 0f;
        Lane queuedNextLane = currentLane != null ? currentLane.GetNextLane(playerController.QueuedTurnDirection) : null;

        GUILayout.Label($"Player: {playerController.name}");
        GUILayout.Label($"Way: {FormatComponentPath(currentLane != null ? currentLane.ParentWay : null)}");
        GUILayout.Label($"Lane: {FormatLane(currentLane)}");
        GUILayout.Label($"S: {currentS:0.00} / {(currentLane != null ? currentLane.Length : 0f):0.00}");
        GUILayout.Label($"Queued Turn: {playerController.QueuedTurnDirection}");
        GUILayout.Label($"Next For Queued: {FormatLane(queuedNextLane)}");
        GUILayout.Label($"Position: {FormatVector3(playerController.transform.position)}");
        GUILayout.Label($"Links: {FormatLaneLinks(currentLane)}");
        GUILayout.Label($"Last Change: {lastChangeText}");
        GUILayout.EndArea();
    }

    private void LogLaneChange(Lane fromLane, Lane toLane, float fromS, float toS)
    {
        if (!logLaneChanges)
        {
            return;
        }

        Vector3 fromPosition = fromLane != null ? fromLane.GetPositionByS(fromS) : playerController.transform.position;
        Vector3 toPosition = toLane != null ? toLane.GetPositionByS(toS) : playerController.transform.position;
        float distance = Vector3.Distance(fromPosition, toPosition);

        lastChangeText = $"{FormatLane(fromLane)} -> {FormatLane(toLane)} ({distance:0.00}m)";

        string message =
            $"Player lane changed ({distance:0.00}m)\n" +
            $"FROM: {FormatLane(fromLane)} s={fromS:0.00} pos={FormatVector3(fromPosition)}\n" +
            $"TO: {FormatLane(toLane)} s={toS:0.00} pos={FormatVector3(toPosition)}\n" +
            $"TO Links: {FormatLaneLinks(toLane)}";

        if (distance >= jumpWarningDistance)
        {
            Debug.LogWarning(message, playerController);
        }
        else
        {
            Debug.Log(message, playerController);
        }
    }

    private static PlayerPathState GetPathState(PlayerController controller)
    {
        if (controller == null)
        {
            return null;
        }

        pathStateField ??= typeof(PlayerController).GetField("pathState", InstanceFlags);
        return pathStateField != null ? pathStateField.GetValue(controller) as PlayerPathState : null;
    }

    private static IReadOnlyList<LaneLink> GetLaneLinks(Lane lane)
    {
        if (lane == null)
        {
            return null;
        }

        laneLinksField ??= typeof(Lane).GetField("nextLaneLinks", InstanceFlags);
        return laneLinksField != null ? laneLinksField.GetValue(lane) as IReadOnlyList<LaneLink> : null;
    }

    private static string FormatLaneLinks(Lane lane)
    {
        IReadOnlyList<LaneLink> links = GetLaneLinks(lane);
        if (links == null || links.Count == 0)
        {
            return "(none)";
        }

        string result = string.Empty;
        for (int i = 0; i < links.Count; ++i)
        {
            LaneLink link = links[i];
            if (link == null)
            {
                continue;
            }

            if (result.Length > 0)
            {
                result += ", ";
            }

            result += $"{link.TurnDirection}->{FormatLane(link.NextLane)}";
        }

        return result.Length > 0 ? result : "(none)";
    }

    private static string FormatLane(Lane lane)
    {
        if (lane == null)
        {
            return "(none)";
        }

        string wayName = lane.ParentWay != null ? lane.ParentWay.name : "(no Way)";
        return $"{wayName}/{lane.name} [index {lane.LaneIndex}]";
    }

    private static string FormatComponentPath(Component component)
    {
        if (component == null)
        {
            return "(none)";
        }

        Transform current = component.transform;
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    private static string FormatVector3(Vector3 value)
    {
        return $"({value.x:0.00}, {value.y:0.00}, {value.z:0.00})";
    }
}
