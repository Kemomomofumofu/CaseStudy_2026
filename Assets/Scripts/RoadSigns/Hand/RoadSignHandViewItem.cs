using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoadSignHandViewItem : MonoBehaviour
{
    [SerializeField] private Button button = null;
    [SerializeField] private Image iconImage = null;
    [SerializeField] private TMP_Text nameLabel = null;
    [SerializeField] private TMP_Text countLabel = null;
    [SerializeField] private GameObject selectionMarker = null;
    [SerializeField] private RoadSignPlacementController placementController = null;

    private RoadSignHandController controller = null;
    private int index = -1;

    /// <summary>
    /// UIアイテムを初期化する
    /// </summary>
    public void Setup(RoadSignHandController _controller, int _index, RoadSignHandEntry _entry)
    {
        controller = _controller;
        index = _index;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        UpdateView(_entry, controller != null && controller.SelectedIndex == index);
    }

    public void SetPlacementController(RoadSignPlacementController controller)
    {
        placementController = controller;
    }

    /// <summary>
    /// 表示内容を更新する
    /// </summary>
    public void UpdateView(RoadSignHandEntry _entry, bool _isSelected)
    {
        if (nameLabel != null)
        {
            nameLabel.text = _entry.DisplayName;
        }

        if (countLabel != null)
        {
            countLabel.text = _entry.Count.ToString();
        }

        if (iconImage != null)
        {
            iconImage.sprite = _entry.Icon;
            iconImage.enabled = _entry.Icon != null;
        }

        if (selectionMarker != null)
        {
            selectionMarker.SetActive(_isSelected);
        }

        if (button != null)
        {
            button.interactable = _entry.Count > 0;
        }
    }

    /// <summary>
    /// クリックで選択する
    /// </summary>
    private void HandleClick()
    {

        if (controller == null)
        {
            return;
        }

        controller.SelectIndex(index);

        if (placementController == null)
        {
            return;
        }

        placementController.PlaceSelectedAtForwardGrid();
    }
}
