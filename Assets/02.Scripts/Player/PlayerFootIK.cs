using UnityEngine;

namespace FpsGame.Player
{
    // 골반과 양발의 IK 목표를 지면에 맞춘다.
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerFootIK : MonoBehaviour
    {
        private const int InjuredLayerIndex = 2;
        private static readonly int s_moveMotionHash = Animator.StringToHash("MoveMotion");

        [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private float _rayStartHeight = 0.5f;
        [SerializeField] private float _rayDistance = 1.0f;
        [SerializeField] private float _maxBodyOffset = 0.2f;
        [SerializeField] private float _bodyOffsetSpeed = 1.0f;
        [SerializeField] private float _ikBlendSpeed = 4f;

        private Animator _animator;
        private float _bodyOffset;
        private float _standWeight = 1.0f;
        private int _lastOffsetUpdateFrame = -1;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (layerIndex != InjuredLayerIndex)
            {
                return;
            }

            FootSample leftFoot = SampleFoot(AvatarIKGoal.LeftFoot, _animator.leftFeetBottomHeight);
            FootSample rightFoot = SampleFoot(AvatarIKGoal.RightFoot, _animator.rightFeetBottomHeight);

            if (_lastOffsetUpdateFrame != Time.frameCount)
            {
                var targetStandWeight = 1.0f - Mathf.Clamp01(_animator.GetFloat(s_moveMotionHash));
                _standWeight = Mathf.MoveTowards(
                    _standWeight,
                    targetStandWeight,
                    _ikBlendSpeed * Time.deltaTime);

                var targetOffset = GetTargetBodyOffset(leftFoot, rightFoot) * _standWeight;
                _bodyOffset = Mathf.MoveTowards(
                    _bodyOffset,
                    targetOffset,
                    _bodyOffsetSpeed * Time.deltaTime);
                _lastOffsetUpdateFrame = Time.frameCount;
            }

            _animator.bodyPosition -= Vector3.up * _bodyOffset;

            ApplyFoot(leftFoot);
            ApplyFoot(rightFoot);
        }

        private FootSample SampleFoot(AvatarIKGoal goal, float bottomHeight)
        {
            Vector3 position = _animator.GetIKPosition(goal);
            Quaternion rotation = _animator.GetIKRotation(goal);
            Vector3 rayOrigin = position + Vector3.up * _rayStartHeight;

            if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    _rayDistance,
                    _groundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return new FootSample(goal, position, rotation, bottomHeight);
            }

            var animatedBottomHeight = position.y - bottomHeight;
            var heightDifference = animatedBottomHeight - hit.point.y;
            return new FootSample(goal, position, rotation, bottomHeight, hit, heightDifference);
        }

        private float GetTargetBodyOffset(FootSample leftFoot, FootSample rightFoot)
        {
            if (leftFoot.HasGround && rightFoot.HasGround)
            {
                return Mathf.Clamp(
                    Mathf.Min(leftFoot.HeightDifference, rightFoot.HeightDifference),
                    -_maxBodyOffset,
                    _maxBodyOffset);
            }

            if (leftFoot.HasGround)
            {
                return Mathf.Clamp(leftFoot.HeightDifference, -_maxBodyOffset, _maxBodyOffset);
            }

            if (rightFoot.HasGround)
            {
                return Mathf.Clamp(rightFoot.HeightDifference, -_maxBodyOffset, _maxBodyOffset);
            }

            return 0.0f;
        }

        private void ApplyFoot(FootSample foot)
        {
            if (!foot.HasGround)
            {
                _animator.SetIKPositionWeight(foot.Goal, 0.0f);
                _animator.SetIKRotationWeight(foot.Goal, 0.0f);
                return;
            }

            Vector3 position = foot.Position - Vector3.up * _bodyOffset;
            position.y = Mathf.Max(position.y, foot.Hit.point.y + foot.BottomHeight);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, foot.Hit.normal) * foot.Rotation;

            _animator.SetIKPositionWeight(foot.Goal, _standWeight);
            _animator.SetIKRotationWeight(foot.Goal, _standWeight);
            _animator.SetIKPosition(foot.Goal, position);
            _animator.SetIKRotation(foot.Goal, rotation);
        }

        private readonly struct FootSample
        {
            public FootSample(
                AvatarIKGoal goal,
                Vector3 position,
                Quaternion rotation,
                float bottomHeight,
                RaycastHit hit = default,
                float heightDifference = 0.0f)
            {
                Goal = goal;
                Position = position;
                Rotation = rotation;
                BottomHeight = bottomHeight;
                Hit = hit;
                HeightDifference = heightDifference;
                HasGround = hit.collider != null;
            }

            public AvatarIKGoal Goal { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public float BottomHeight { get; }
            public RaycastHit Hit { get; }
            public float HeightDifference { get; }
            public bool HasGround { get; }
        }
    }
}
