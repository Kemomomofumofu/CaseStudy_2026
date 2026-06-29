using UnityEngine;

public class MinimapMarker : MonoBehaviour
{
    public enum MarkerType { Player, CPU, Goal }

    [Tooltip("マーカーの種類")]
    [SerializeField] public MarkerType markerType = MarkerType.CPU;
}