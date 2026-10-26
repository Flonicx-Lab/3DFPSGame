using UnityEngine;
using UnityEngine.UI;

public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private MinimapCamera _minimapCamera;
    [SerializeField] private Button _zoomInButton;
    [SerializeField] private Button _zoomOutButton;

    private void Start()
    {
        _zoomInButton.onClick.AddListener(_minimapCamera.ZoomIn);
        _zoomOutButton.onClick.AddListener(_minimapCamera.ZoomOut);
    }

    private void OnDestroy()
    {
        _zoomInButton.onClick.RemoveListener(_minimapCamera.ZoomIn);
        _zoomOutButton.onClick.RemoveListener(_minimapCamera.ZoomOut);
    }
}
