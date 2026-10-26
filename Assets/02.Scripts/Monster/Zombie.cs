using System;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - 상태 : 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이 : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 좀비가 가질 수 있는 상태. 상태별 동작은 Monster/State 폴더의 상태 객체가 맡는다.
public enum ZombieState
{
    Patrol, // 순찰 : 시작 지점 주변 두 지점을 왕복한다.
    Trace, //  추격 : 플레이어를 쫒아간다.
    Comeback, // 복귀 : 처음 자리로 돌아간다.
    Attack, // 공격 : 일정 시간마다 플레이어를 때린다.
    Hit, // 피격 : 공격을 받아 잠깐 멈칫한다.
    Die // 사망 : 쓰러지고 잠시 뒤 사라진다.
}

public class Zombie : MonoBehaviour, IDamagable, IPoolable
{
    private const float HomeArrivalDistance = 1f; // 시작 지점 도착으로 보는 거리

    [Header("스텟")]
    [SerializeField] private float _detectDistance = 8f; // 플레이어를 발견하는 탐지거리
    [SerializeField] private float _comebackDistance = 12f; // 되돌아가기 시작하는 거리
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackRange = 2f; // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _patrolDistance = 3f; // 순찰 반경 거리
    [SerializeField] private float _maxHealth = 30f;

    private float _health;
    public float MaxHealth => _maxHealth;
    public float Health => _health;
    public float AttackInterval => _attackInterval;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;
    [SerializeField] private float _bloodSprayTime = 3f;
    private GameObject _player;
    private CharacterController _controller;
    private Vector3 _startPosition;

    private readonly Dictionary<ZombieState, IZombieState> _states = new();
    private ZombiePatrolState _patrolState;
    private IZombieState _currentState;
    private ZombieState _state;

    public event Action OnDamaged;
    public event Action<Zombie> OnDied;
    public event Action OnSpawned;

    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _controller = GetComponent<CharacterController>();

        _patrolState = new ZombiePatrolState(this);
        _states[ZombieState.Patrol] = _patrolState;
        _states[ZombieState.Trace] = new ZombieTraceState(this);
        _states[ZombieState.Comeback] = new ZombieComebackState(this);
        _states[ZombieState.Attack] = new ZombieAttackState(this);
        _states[ZombieState.Hit] = new ZombieHitState(this);
        _states[ZombieState.Die] = new ZombieDieState(this);
    }

    // 풀에서 꺼내질 때마다 처음 상태로 되돌린다.
    public void OnSpawn()
    {
        _health = _maxHealth;
        _controller.enabled = true;
        _startPosition = transform.position;

        Vector3 patrolPointA = _startPosition + transform.forward * _patrolDistance;
        Vector3 patrolPointB = _startPosition - transform.forward * _patrolDistance;
        _patrolState.SetPatrolPoints(patrolPointA, patrolPointB);

        _state = ZombieState.Patrol;
        _currentState = _patrolState;

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
        _state = nextState;
        _currentState = _states[nextState];
        _currentState.Enter();
    }

    // 데미지 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Die)
        {
            return;
        }

        _health -= damageInfo.Value;

        OnDamaged?.Invoke();

        GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
        bloodSpray.transform.position = damageInfo.HitPoint;
        bloodSpray.transform.forward = damageInfo.HitNormal;

        Destroy(bloodSpray, _bloodSprayTime);


        StopAllCoroutines();

        if (_health > 0f)
        {
            ChangeState(ZombieState.Hit);
        }
        else
        {
            ChangeState(ZombieState.Die);
            OnDied?.Invoke(this);
        }
    }

    public bool IsPlayerDetected()
    {
        return GetDistanceTo(_player.transform.position) < _detectDistance;
    }

    public bool IsPlayerInAttackRange()
    {
        return GetDistanceTo(_player.transform.position) <= _attackRange;
    }

    public bool IsFarFromStart()
    {
        return GetDistanceTo(_startPosition) > _comebackDistance;
    }

    public bool IsNearStart()
    {
        return GetHorizontalDistanceTo(_startPosition) < HomeArrivalDistance;
    }

    private float GetDistanceTo(Vector3 point)
    {
        return Vector3.Distance(transform.position, point);
    }

    // 도착 판정용. 이동은 수평으로만 하므로 높이 차이는 무시한다.
    public float GetHorizontalDistanceTo(Vector3 point)
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
    public void MoveTo(Vector3 point, float speedRatio)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        direction.Normalize();

        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }

        _controller.SimpleMove(direction * (_moveSpeed * speedRatio));
    }

    public void AttackPlayer()
    {
        DamageInfo damageInfo = new()
        {
            Value = _attackDamage,
            HitPoint = _player.transform.position,
            HitNormal = (transform.position - _player.transform.position).normalized
        };
        _player.GetComponent<IDamagable>().TakeDamage(damageInfo);
        Debug.Log("플레이어를 때렸습니다");
    }

    public void DisableMovement()
    {
        _controller.enabled = false;
    }
}
