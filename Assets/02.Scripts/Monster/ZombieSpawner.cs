using System;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using Random = UnityEngine.Random;

// 배치된 스폰 지점 중 플레이어와 충분히 떨어진 곳에서 일정 간격으로 좀비를 스폰한다.
public class ZombieSpawner : MonoBehaviour
{
    [Header("스폰 대상")]
    [SerializeField] private LeanGameObjectPool _zombiePool; // 동시에 존재하는 좀비의 최대 수는 풀 용량이 정한다.
    [SerializeField] private Transform[] _spawnPoints;

    [Header("스폰 사이클")]
    [SerializeField] private float _spawnInterval = 3f; // 스폰 간격
    [SerializeField] private float _minDistanceFromPlayer = 15f; // 플레이어와의 최소 거리

    [Header("개발")]
    [SerializeField] private bool _spawnEnabled = true; // 개발 중 좀비 스폰을 임시로 끈다.

    private readonly List<Zombie> _aliveZombies = new();
    private readonly List<Transform> _candidatePoints = new();
    private Transform _player;
    private float _spawnTimer;

    public event Action OnAliveCountChanged;

    public int AliveCount => _aliveZombies.Count;
    public int MaxAliveCount => _zombiePool.Capacity;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;

        // 용량 0은 상한 없음이므로 최대 수가 사라진다.
        if (_zombiePool.Capacity <= 0)
        {
            Debug.LogError("좀비 풀의 Capacity가 0 이하입니다. 최대 수를 정하려면 1 이상이어야 합니다.", _zombiePool);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!_spawnEnabled)
        {
            return;
        }

        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < _spawnInterval)
        {
            return;
        }

        _spawnTimer = 0f;

        // 모든 지점이 플레이어와 가까우면 이번 사이클은 건너뛴다.
        Transform spawnPoint = PickSpawnPoint();
        if (spawnPoint == null)
        {
            return;
        }

        Vector3 toPlayer = _player.position - spawnPoint.position;
        toPlayer.y = 0f;

        // 풀에 빈 자리가 없으면 이번 사이클은 건너뛴다.
        GameObject clone = null;
        if (!_zombiePool.TrySpawn(ref clone, spawnPoint.position, Quaternion.LookRotation(toPlayer)))
        {
            return;
        }

        Zombie zombie = clone.GetComponent<Zombie>();
        zombie.OnDied += OnZombieDied;
        _aliveZombies.Add(zombie);
        OnAliveCountChanged?.Invoke();
    }

    private void OnZombieDied(Zombie zombie)
    {
        // 같은 좀비가 풀에서 다시 나올 때 구독이 쌓이지 않도록 한 생명이 끝나면 해제한다.
        zombie.OnDied -= OnZombieDied;
        _aliveZombies.Remove(zombie);
        OnAliveCountChanged?.Invoke();
    }

    private Transform PickSpawnPoint()
    {
        _candidatePoints.Clear();

        foreach (Transform point in _spawnPoints)
        {
            if (Vector3.Distance(point.position, _player.position) >= _minDistanceFromPlayer)
            {
                _candidatePoints.Add(point);
            }
        }

        if (_candidatePoints.Count == 0)
        {
            return null;
        }

        return _candidatePoints[Random.Range(0, _candidatePoints.Count)];
    }
}
