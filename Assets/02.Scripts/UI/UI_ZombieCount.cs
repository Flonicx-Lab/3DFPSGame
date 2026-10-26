using TMPro;
using UnityEngine;

public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countText;

    private ZombieSpawner _spawner;

    private void Awake()
    {
        _spawner = FindAnyObjectByType<ZombieSpawner>();
    }

    private void Start()
    {
        RefreshCount();
        _spawner.OnAliveCountChanged += RefreshCount;
    }

    private void OnDestroy()
    {
        _spawner.OnAliveCountChanged -= RefreshCount;
    }

    private void RefreshCount()
    {
        _countText.text = $"남은 좀비 수 {_spawner.AliveCount} / {_spawner.MaxAliveCount}";
    }
}
