using UnityEngine;

// 총구에서 끝점까지 레이저 선을 짧게 보여준다.
[RequireComponent(typeof(LineRenderer))]
public class GunLaser : MonoBehaviour
{
    [SerializeField] private float _showDuration = 0.05f; // 선이 보이는 시간

    private LineRenderer _line;
    private float _hideTimer;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.useWorldSpace = true;
        _line.enabled = false;
    }

    private void LateUpdate()
    {
        if (!_line.enabled)
        {
            return;
        }

        _line.SetPosition(0, transform.position);
        _hideTimer -= Time.deltaTime;
        if (_hideTimer <= 0f)
        {
            _line.enabled = false;
        }
    }

    public void Show(Vector3 end)
    {
        _line.SetPosition(0, transform.position);
        _line.SetPosition(1, end);
        _line.enabled = true;
        _hideTimer = _showDuration;
    }
}
