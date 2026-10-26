using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// 배치된 스폰 지점 중 플레이어와 충분히 떨어진 곳에서 일정 간격으로 좀비를 스폰한다.
public class ZombieSpawner : MonoBehaviour
{
    [Header("스폰 대상")]
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private Transform[] _spawnPoints;

    [Header("스폰 사이클")]
    [SerializeField] private float _spawnInterval = 3f; // 스폰 간격
    [SerializeField] private int _maxAliveCount = 10; // 동시에 살아 있을 수 있는 최대 수
    [SerializeField] private float _minDistanceFromPlayer = 15f; // 플레이어와의 최소 거리

    private readonly List<Zombie> _aliveZombies = new();
    private readonly List<Transform> _candidatePoints = new();
    private Transform _player;
    private float _spawnTimer;

    public event Action OnAliveCountChanged;

    public int AliveCount => _aliveZombies.Count;
    public int MaxAliveCount => _maxAliveCount;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
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

        if (_aliveZombies.Count >= _maxAliveCount)
        {
            return;
        }

        // 모든 지점이 플레이어와 가까우면 이번 사이클은 건너뛴다.
        Transform spawnPoint = PickSpawnPoint();
        if (spawnPoint == null)
        {
            return;
        }

        Vector3 toPlayer = _player.position - spawnPoint.position;
        toPlayer.y = 0f;

        Zombie zombie = Instantiate(_zombiePrefab, spawnPoint.position, Quaternion.LookRotation(toPlayer));
        zombie.OnDied += OnZombieDied;
        _aliveZombies.Add(zombie);
        OnAliveCountChanged?.Invoke();
    }

    private void OnZombieDied(Zombie zombie)
    {
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
