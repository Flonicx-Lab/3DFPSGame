using System;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using UnityEngine.AI;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - 상태 : 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이 : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 좀비가 가질 수 있는 상태. 상태별 동작은 Monster/State 폴더의 상태 객체가 맡는다.
public enum ZombieState
{
    Idle, // 대기 : 순찰 지점에서 잠시 쉰다.
    Patrol, // 순찰 : 시작 지점 주변 두 지점을 왕복한다.
    Trace, //  추격 : 플레이어를 쫒아간다.
    Comeback, // 복귀 : 처음 자리로 돌아간다.
    Attack, // 공격 : 일정 시간마다 플레이어를 때린다.
    Hit, // 피격 : 공격을 받아 잠깐 멈칫한다.
    Die, // 사망 : 쓰러지고 잠시 뒤 사라진다.
    Jump // 점프 : 링크를 포물선으로 넘어간다.
}

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamagable, IPoolable
{
    [Header("스텟")]
    [SerializeField] private float _detectDistance = 8f; // 플레이어를 발견하는 탐지거리
    [SerializeField] private float _comebackDistance = 12f; // 되돌아가기 시작하는 거리
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackRange = 2f; // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _patrolDistance = 3f; // 순찰 반경 거리
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _jumpHeight = 1f; // 링크 직선 경로 위로 더하는 포물선 높이

    private float _health;
    public float MaxHealth => _maxHealth;
    public float Health => _health;
    public float AttackInterval => _attackInterval;
    public float JumpHeight => _jumpHeight;
    public bool IsDead => _health <= 0f;
    public ZombieState PreviousState { get; private set; }

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;
    [SerializeField] private float _bloodSprayReturnDelay = 1f;
    private GameObject _player;
    private Vector3 _startPosition;

    private readonly Dictionary<ZombieState, IZombieState> _states = new();
    private ZombiePatrolState _patrolState;
    private IZombieState _currentState;
    private ZombieState _state;
    private NavMeshAgent _agent;
    private Collider _collider;
    private ZombieHitbox[] _hitboxes;
    private Animator _animator;
    private Vector3 _modelBasePosition;

    public event Action OnDamaged;
    public event Action<Zombie> OnDied;
    public event Action OnSpawned;

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _agent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();
        _hitboxes = GetComponentsInChildren<ZombieHitbox>();
        foreach (ZombieHitbox hitbox in _hitboxes)
        {
            hitbox.SetOwner(this);
        }

        _animator = GetComponentInChildren<Animator>();
        _modelBasePosition = _animator.transform.localPosition;
        // 링크 통과는 점프 상태가 직접 한다. 켜져 있으면 직선으로 미끄러져 넘어간다.
        _agent.autoTraverseOffMeshLink = false;

        _patrolState = new ZombiePatrolState(this);
        _states[ZombieState.Idle] = new ZombieIdleState(this);
        _states[ZombieState.Patrol] = _patrolState;
        _states[ZombieState.Trace] = new ZombieTraceState(this);
        _states[ZombieState.Comeback] = new ZombieComebackState(this);
        _states[ZombieState.Attack] = new ZombieAttackState(this);
        _states[ZombieState.Hit] = new ZombieHitState(this);
        _states[ZombieState.Die] = new ZombieDieState(this);
        _states[ZombieState.Jump] = new ZombieJumpState(this);
    }

    // 풀에서 꺼내질 때마다 처음 상태로 되돌린다.
    public void OnSpawn()
    {
        // 사망 때 끈 에이전트와 콜라이더는 오브젝트를 다시 켜도 꺼진 채로 남는다.
        _agent.enabled = true;
        _collider.enabled = true;
        SetHitboxesEnabled(true);
        _health = _maxHealth;
        _startPosition = transform.position;

        Vector3 patrolPointA = _startPosition + transform.forward * _patrolDistance;
        Vector3 patrolPointB = _startPosition - transform.forward * _patrolDistance;
        _patrolState.SetPatrolPoints(patrolPointA, patrolPointB);

        _state = ZombieState.Patrol;
        _currentState = _patrolState;
        // 애니메이터는 켜질 때 기본 상태 Idle에서 시작하므로 순찰에 맞춰 이동 동작으로 넘긴다.
        _animator.SetTrigger(ZombieAnimatorParams.IdleToMove);

        OnSpawned?.Invoke();
    }

    public void OnDespawn()
    {
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        _currentState.Update();
    }

    public void ChangeState(ZombieState nextState)
    {
        Debug.Log($"{_state} -> {nextState}");

        _currentState.Exit();
        PreviousState = _state;
        _state = nextState;
        _currentState = _states[nextState];
        _currentState.Enter();
    }

    // 부위 정보가 없는 범위 데미지는 루트 캡슐로 들어온다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        TakeDamage(damageInfo, transform);
    }

    public void TakeDamage(DamageInfo damageInfo, Transform hitPart)
    {
        // 공중에서 죽은 좀비는 착지할 때까지 사망 상태가 아니므로 체력으로도 막는다.
        if (_state == ZombieState.Die || IsDead)
        {
            return;
        }

        _health -= damageInfo.Value;

        OnDamaged?.Invoke();

        GameObject bloodSpray = LeanPool.Spawn(
            _bloodSprayPrefab, damageInfo.HitPoint, Quaternion.LookRotation(damageInfo.HitNormal), hitPart);
        LeanPool.Despawn(bloodSpray, _bloodSprayReturnDelay);

        // 공중에서는 상태를 바꾸지 않는다. 사망은 점프 상태가 착지할 때 처리한다.
        if (_state == ZombieState.Jump)
        {
            return;
        }

        StopAllCoroutines();

        if (_health > 0f)
        {
            ChangeState(ZombieState.Hit);
            return;
        }

        Die();
    }

    public void Die()
    {
        ChangeState(ZombieState.Die);
        OnDied?.Invoke(this);
    }

    public bool IsPlayerDetected()
    {
        return GetDistanceTo(_player.transform.position) < _detectDistance;
    }

    public bool IsPlayerInAttackRange()
    {
        return GetDistanceTo(_player.transform.position) <= _attackRange;
    }

    public bool IsOnNavMeshLink()
    {
        return _agent.isOnOffMeshLink;
    }

    public bool IsFarFromStart()
    {
        return GetDistanceTo(_startPosition) > _comebackDistance;
    }

    public bool IsNearStart()
    {
        return HasArrivedAt(_startPosition);
    }

    // 에이전트는 남은 거리가 정지 거리 이하가 되면 스스로 멈추므로, 더 짧은 거리로 판정하면 도착하지 못한다.
    public bool HasArrivedAt(Vector3 point)
    {
        return GetHorizontalDistanceTo(point) <= _agent.stoppingDistance;
    }

    private float GetDistanceTo(Vector3 point)
    {
        return Vector3.Distance(transform.position, point);
    }

    // 도착 판정용. 이동은 수평으로만 하므로 높이 차이는 무시한다.
    private float GetHorizontalDistanceTo(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    public void MoveToPlayer()
    {
        MoveTo(_player.transform.position, 1f);
    }

    public void MoveToStart()
    {
        MoveTo(_startPosition, 1f);
    }

    // speedRatio : 이동 속도 대비 실제 속도 비율
    public void MoveTo(Vector3 destination, float speedRatio)
    {
        _agent.isStopped = false;
        _agent.speed = _moveSpeed * speedRatio;
        _agent.SetDestination(destination);
    }

    // 남은 속도를 감속에 맡기면 정지 거리를 지나 미끄러져 들어오므로 바로 0으로 만든다.
    public void StopMovement()
    {
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    // 링크 끝점은 바닥 기준이라 Base Offset을 더해야 루트 높이가 된다.
    public Vector3 GetLinkEndPosition()
    {
        return _agent.currentOffMeshLinkData.endPos + Vector3.up * _agent.baseOffset;
    }

    public void CompleteLink()
    {
        _agent.CompleteOffMeshLink();
    }

    // 멈춘 에이전트는 회전하지 않으므로 직접 돌린다. 회전 속도는 에이전트 설정을 따른다.
    public void TurnToPlayer()
    {
        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        if (direction == Vector3.zero)
        {
            return;
        }

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(direction), _agent.angularSpeed * Time.deltaTime);
    }

    // 애니메이션 이벤트가 부른다. 이벤트는 다른 상태로 넘어가는 전환 중에도 실행되므로 공격 중이고 범위 안일 때만 때린다.
    public void AttackAction()
    {
        if (_state != ZombieState.Attack || !IsPlayerInAttackRange())
        {
            return;
        }

        DamageInfo damageInfo = new()
        {
            Value = _attackDamage,
            HitPoint = _player.transform.position,
            HitNormal = (transform.position - _player.transform.position).normalized
        };
        _player.GetComponent<IDamagable>().TakeDamage(damageInfo);
        Debug.Log("플레이어를 때렸습니다");
    }

    public void SetAnimationTrigger(int triggerId)
    {
        _animator.SetTrigger(triggerId);
    }

    public void ResetAnimationTrigger(int triggerId)
    {
        _animator.ResetTrigger(triggerId);
    }

    public void SetAnimationSpeed(float speed)
    {
        _animator.speed = speed;
    }

    // 모델만 내려 콜라이더와 에이전트 위치는 그대로 둔다.
    public void SetBodyDip(float dip)
    {
        _animator.transform.localPosition = _modelBasePosition + Vector3.down * dip;
    }

    // 사망 동작 동안 서 있던 자리의 콜라이더가 남으면 뒤에 있는 좀비 대신 총알을 맞는다.
    public void DisableBody()
    {
        _agent.enabled = false;
        _collider.enabled = false;
        SetHitboxesEnabled(false);
    }

    private void SetHitboxesEnabled(bool isEnabled)
    {
        foreach (ZombieHitbox hitbox in _hitboxes)
        {
            hitbox.SetEnabled(isEnabled);
        }
    }
}
