using UnityEngine;

public class PlayerZoom : MonoBehaviour
{
    private PlayerWeaponMode _weaponMode;

    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private float _normalFow = 60f;
    [SerializeField] private float _zoomFov = 15f;

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        var isZooming = GameManager.Instance.State == GameState.Playing
                        && _weaponMode.Mode == WeaponMode.Sniper
                        && Input.GetMouseButton(1);
        Camera.main.fieldOfView = isZooming ? _zoomFov : _normalFow;
        _crosshair.SetActive(!isZooming);
        _sniperZoom.SetActive(isZooming);
    }
}
