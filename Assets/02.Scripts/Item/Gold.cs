using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Gold : MonoBehaviour
{
    [SerializeField] private int _amount = 10;
    [SerializeField] private float _popPower = 3f;
    [SerializeField] private float _pickupDelay = 1f;
    [SerializeField] private float _magnetDistance = 3f;
    [SerializeField] private float _flyTime = 0.4f;

    private Rigidbody _rb;
    private Player _player;
    private float _timer;
    private bool _isFlying;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    private void Start()
    {
        var direction = new Vector3(Random.Range(-1, 1f), 1.5f, Random.Range(-1, 1f));
        _rb.AddForce(direction * _popPower, ForceMode.Impulse);
    }

    private void Update()
    {
        if (_isFlying)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < _pickupDelay)
        {
            return;
        }

        if (Vector3.Distance(transform.position, _player.transform.position) <= _magnetDistance)
        {
            StartCoroutine(FlyToPlayer_Routine());
        }
    }

    private IEnumerator FlyToPlayer_Routine()
    {
        _isFlying = true;
        _rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;

        Vector3 start = transform.position;
        var elapsedTime = 0f;
        while (elapsedTime < _flyTime)
        {
            Vector3 end = _player.transform.position;
            Vector3 control = (start + end) / 2f + Vector3.up * 2f; // 베지어 컨트롤
            transform.position = QuadraticBezier(start, control, end, elapsedTime / _flyTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _player.AddGold(_amount);
        Destroy(gameObject);
    }

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        var p0 = Vector3.Lerp(start, control, t);
        var p1 = Vector3.Lerp(control, end, t);
        return Vector3.Lerp(p0, p1, t);
    }
}
