using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    private WeaponMode _weaponMode;
    [SerializeField] private TextMeshProUGUI _goldText;

    private int _gold;
    public int Gold => _gold;

    public void AddGold(int amount)
    {
        _gold += amount;
        _goldText.text = $"골드 : {_gold}";
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _weaponMode = WeaponMode.Normal;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _weaponMode = WeaponMode.Sniper;
        }
    }
}
