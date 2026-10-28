using UnityEngine;

// 점프 : 링크 시작에서 웅크렸다가 포물선으로 끝점까지 날아가 착지한다.
public class ZombieJumpState : IZombieState
{
    private const float Gravity = 20f; // 체공 시간 계산용. 플레이어 기본 중력 크기와 같게 맞춘다.
    private const float CrouchDuration = 0.15f;
    private const float LandDuration = 0.15f;
    private const float MaxBodyDip = 0.15f; // 웅크림, 착지 때 모델이 내려가는 최대 거리
    private const float AirAnimationSpeed = 0.3f; // 점프 중 이동 클립 재생 속도

    private readonly Zombie _zombie;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private float _airDuration;
    private float _jumpTimer;

    public ZombieJumpState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _jumpTimer = 0f;
        _startPosition = _zombie.transform.position;
        _endPosition = _zombie.GetLinkEndPosition();
        // 정점 높이 h를 중력 g로 오르내리는 시간 T = √(8h/g)
        _airDuration = Mathf.Sqrt(8f * _zombie.JumpHeight / Gravity);
        _zombie.SetAnimationSpeed(AirAnimationSpeed);
    }

    // 코루틴은 피격 때 StopAllCoroutines로 끊기므로 타이머로 구간을 나눈다.
    public void Update()
    {
        _jumpTimer += Time.deltaTime;

        if (_jumpTimer < CrouchDuration)
        {
            ApplyBodyDip(_jumpTimer / CrouchDuration);
            return;
        }

        var airTimer = _jumpTimer - CrouchDuration;
        if (airTimer < _airDuration)
        {
            ApplyArcPosition(airTimer / _airDuration);
            return;
        }

        var landTimer = airTimer - _airDuration;
        if (landTimer < LandDuration)
        {
            _zombie.transform.position = _endPosition;
            ApplyBodyDip(landTimer / LandDuration);
            return;
        }

        Land();
    }

    // 어떤 경로로 나가도 애니메이션 속도와 모델 위치를 원래대로 돌린다.
    public void Exit()
    {
        _zombie.SetAnimationSpeed(1f);
        _zombie.SetBodyDip(0f);
    }

    // 0에서 내려갔다가 0으로 돌아온다.
    private void ApplyBodyDip(float progressRatio)
    {
        _zombie.SetBodyDip(MaxBodyDip * Mathf.Sin(Mathf.PI * progressRatio));
    }

    // 직선 경로 위에 4t(1 - t) 포물선을 더한다. t = 0.5에서 JumpHeight만큼 올라간다.
    private void ApplyArcPosition(float progressRatio)
    {
        var arcHeight = _zombie.JumpHeight * 4f * progressRatio * (1f - progressRatio);
        _zombie.transform.position =
            Vector3.Lerp(_startPosition, _endPosition, progressRatio) + Vector3.up * arcHeight;
    }

    private void Land()
    {
        _zombie.CompleteLink();

        if (_zombie.IsDead)
        {
            _zombie.Die();
            return;
        }

        _zombie.ChangeState(_zombie.PreviousState);
    }
}
