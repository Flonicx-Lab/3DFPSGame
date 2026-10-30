using UnityEngine;

// 총구에서 끝점까지 레이저 선을 짧게 보여준다.
[RequireComponent(typeof(LineRenderer))]
public class GunLaser : MonoBehaviour
{
    [SerializeField] private float _showDuration = 0.05f; // 선이 보이는 시간
    [SerializeField] private float _fadeInLength = 0.15f; // 총구에서 선명해질 때까지의 거리, 단위 m
    [SerializeField] private float _fadeOutStart = 0.35f; // 흐려지기 시작하는 선 길이 비율
    [SerializeField] private float _fadeOutEnd = 0.75f; // 완전히 흐려지는 선 길이 비율
    [SerializeField] private float _hitGlowLength = 0.3f; // 탄착점에서 다시 밝아지는 구간의 거리, 단위 m

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
        Vector3 start = transform.position;
        var length = Vector3.Distance(start, end);
        var fadeInRatio = Mathf.Clamp01(_fadeInLength / length);
        var fadeOutStart = Mathf.Max(fadeInRatio, Mathf.Clamp01(_fadeOutStart));
        var fadeOutEnd = Mathf.Max(fadeOutStart, Mathf.Clamp01(_fadeOutEnd));
        var hitGlowStartRatio = 1f - Mathf.Clamp01(_hitGlowLength / length);
        var hitGlowFadeStartRatio = Mathf.Max(fadeOutEnd, Mathf.Clamp01(hitGlowStartRatio - 0.001f));
        hitGlowStartRatio = Mathf.Max(hitGlowFadeStartRatio, Mathf.Clamp01(hitGlowStartRatio));
        Gradient gradient = _line.colorGradient;
        GradientAlphaKey[] alphaKeys =
        {
            new GradientAlphaKey(0.0f, 0.0f),
            new GradientAlphaKey(1.0f, fadeInRatio),
            new GradientAlphaKey(1.0f, fadeOutStart),
            new GradientAlphaKey(0.0f, fadeOutEnd),
            new GradientAlphaKey(0.0f, hitGlowFadeStartRatio),
            new GradientAlphaKey(1.0f, hitGlowStartRatio),
        };
        gradient.SetKeys(gradient.colorKeys, alphaKeys);
        _line.colorGradient = gradient;

        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
        _line.enabled = true;
        _hideTimer = _showDuration;
    }
}
