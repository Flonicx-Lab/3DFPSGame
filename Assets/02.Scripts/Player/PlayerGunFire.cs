using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;
    private float _range = 100f;
    private float _damage = 10f;

    private void Update()
    {
        // 1. 마우스 왼쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire1")) // 0~2
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Transform cameraTransform = Camera.main.transform;
        // 2. 레이(광선)을 생성하고 발사될 위치와 진행 방향 설정
        var ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딪힌 대상의 정보를 저장할 변수 생성
        var hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딪힌 물체가 있다면 이펙트 표시
        var isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            Debug.Log($"Hit {hitInfo.collider.name}이 총알을 맞았다. 데미지 : {_damage}");
            //Todo. 이펙트 생성
        }
    }
}
