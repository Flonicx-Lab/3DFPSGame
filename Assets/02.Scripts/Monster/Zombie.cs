using System.Collections;
using UnityEngine;

// FSM(유한 상태 머신) : 유한개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - 상태 : 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이 : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 좀비가 가질 수 있는 상태

public enum ZombieState
{
    Idle, // 대기 : 제자리에서 플레이어를 기다린다.
    Trace, //  추격 : 플레이어를 쫒아간다.
    Comeback, // 복귀 : 처음 자리로 돌아간다.
    Attack, // 공격 : 일정 시간마다 플레이어를 때린다.
    Hit, // 피격 : 공격을 받아 잠깐 멈칫한다.
    Die // 사망 : 쓰러지고 잠시 뒤 사라진다.
}

public class Zombie : MonoBehaviour
{
    [Header("스텟")]
    [SerializeField] private float _detectDistance = 8f; //플레이어를 발견하는 탐지거리
    [SerializeField] private float _comebackDistance = 12f; // 되돌아가기 시작하는 거리
    [SerializeField] private float _attackRange = 2f; // 공격 범위
    [SerializeField] private float _attackInterval = 1.2f; // 공격 간격
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _health = 30f;

    private float _attackTimer = 0f;
    private GameObject _player;
    private CharacterController _controller;
    private Vector3 _startPosition;

    private ZombieState _state = ZombieState.Idle;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _controller = GetComponent<CharacterController>();
        _startPosition = transform.position;
    }

    private void Update()
    {
        switch (_state)
        {
            case ZombieState.Idle:
                Idle();
                break;

            case ZombieState.Trace:
                Trace();
                break;

            case ZombieState.Comeback:
                Comeback();
                break;
            case ZombieState.Attack:
                Attack();
                break;
        }
    }

    // 데미지 받는다.
    public void TakeDamage(float damage)
    {
        if (_state == ZombieState.Die)
        {
            return;
        }

        _health -= damage;

        StopAllCoroutines();

        if (_health > 0f)
        {
            Debug.Log($"{_state} -> Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }

        else
        {
            Debug.Log($"{_state} -> Die");
            _state = ZombieState.Die;
            StartCoroutine(Die_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        yield return new WaitForSeconds(0.3f);
        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }

    private IEnumerator Die_Coroutine()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    private void Idle()
    {
        // 대기 : 제자리에서 플레이어를 기다린다.
        // 플레이어와 가까워지면 Trace 상태로 전이.

        var distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance < _detectDistance)
        {
            Debug.Log($"{_state} -> trace");
            _state = ZombieState.Trace;
            return;
        }
    }

    private void Trace()
    {
        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (Vector3.Distance(transform.position, _startPosition) > _comebackDistance)
        {
            Debug.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다.
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;
            Debug.Log($"{_state} -> Attack");
            _state = ZombieState.Attack;
            return;
        }

        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Comeback()
    {
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            Debug.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Attack()
    {
        // 플레이어가 공격 사정거리 밖으로 나가면 쫒아간다.
        var distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            Debug.Log($"{_state} -> trace");
            _state = ZombieState.Trace;
            return;
        }

        _attackTimer += Time.deltaTime;

        if (_attackTimer >= _attackInterval)
        {
            //일정 시간마다 플레이어를 때린다.
            Debug.Log("플레이어를 때렸습니다");
            _attackTimer = 0f;
        }
    }
}
