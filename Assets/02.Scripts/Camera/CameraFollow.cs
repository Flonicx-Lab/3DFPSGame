using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;

    private void Awake()
    {
        if (_target == null)
        {
            Debug.LogWarning("[CameraFollow] 카메라 타겟이 없어요", this);
        }
    }

    // 카메라는 캐릭터가 움직인 후로 업데이트를 고정
    private void LateUpdate()
    {
        transform.position = _target.position;
    }
}
