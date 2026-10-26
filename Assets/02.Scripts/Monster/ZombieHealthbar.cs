using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthbar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponent<Zombie>();
    }

    private void Start()
    {
        RefreshHealth();
        _zombie.OnDamaged += RefreshHealth;
    }

    private void OnDestroy()
    {
        _zombie.OnDamaged -= RefreshHealth;
    }

    private void LateUpdate()
    {
        // 빌보드 : 체력바가 카메라를 쳐다보게 한다.
        _slider.transform.forward = Camera.main.transform.forward;
    }

    private void RefreshHealth()
    {
        _slider.value = _zombie.Health / _zombie.MaxHealth;
    }
}
