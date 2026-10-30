using System;
using UnityEditor;
using UnityEngine;

namespace FpsGame.Editor
{
    // 원본 Root에 자세 변화량을 더하고 지정 구간의 관절 사슬 제약을 근육에 적용해 새 클립으로 저장한다.
    public static class ConstraintClipBaker
    {
        private const float MaximumAngleError = 0.5f;
        private const int MaximumIterations = 20;

        public static void Bake(
            AnimationClip source,
            string outputPath,
            Animator animator,
            Transform effector,
            Vector3 effectorLocalPoint,
            Transform eye,
            Vector3 targetDirection,
            HumanBodyBones[] chain,
            float[] weights,
            int rampInStart,
            int holdStart,
            int holdEnd,
            int rampOutEnd)
        {
            ValidateArguments(
                source,
                outputPath,
                animator,
                effector,
                eye,
                targetDirection,
                chain,
                weights,
                rampInStart,
                holdStart,
                holdEnd,
                rampOutEnd);

            AnimationCurve[] sourceRootPositionCurves = GetRequiredRootCurves(source, "RootT", 3);
            AnimationCurve[] sourceRootRotationCurves = GetRequiredRootCurves(source, "RootQ", 4);
            Transform[] bones = GetBones(animator, chain);
            Transform[] transforms = animator.GetComponentsInChildren<Transform>(true);
            TransformState[] originalStates = CaptureStates(transforms);
            var startedAnimationMode = !AnimationMode.InAnimationMode();

            var lastFrame = Mathf.RoundToInt(source.length * source.frameRate);
            var keyCount = lastFrame + 1;
            Keyframe[][] muscleKeys = CreateKeyArrays(HumanTrait.MuscleCount, keyCount);
            Keyframe[][] rootPositionKeys = CreateKeyArrays(3, keyCount);
            Keyframe[][] rootRotationKeys = CreateKeyArrays(4, keyCount);
            var originPose = new HumanPose();
            var editedPose = new HumanPose();

            using (var poseHandler = new HumanPoseHandler(animator.avatar, animator.transform))
            {
                try
                {
                    if (startedAnimationMode)
                    {
                        AnimationMode.StartAnimationMode();
                    }

                    for (var frame = 0; frame <= lastFrame; frame++)
                    {
                        var time = Mathf.Min(frame / source.frameRate, source.length);
                        Sample(animator, source, time);
                        poseHandler.GetHumanPose(ref originPose);

                        var timeWeight = GetTimeWeight(
                            frame,
                            rampInStart,
                            holdStart,
                            holdEnd,
                            rampOutEnd);
                        var angleError = ApplyConstraint(
                            animator.transform,
                            effector,
                            effectorLocalPoint,
                            eye,
                            targetDirection,
                            bones,
                            weights,
                            timeWeight,
                            frame >= rampInStart && frame <= rampOutEnd);

                        poseHandler.GetHumanPose(ref editedPose);
                        RecordPose(editedPose, time, frame, muscleKeys);
                        RecordRootPose(
                            originPose,
                            editedPose,
                            sourceRootPositionCurves,
                            sourceRootRotationCurves,
                            time,
                            frame,
                            rootPositionKeys,
                            rootRotationKeys);
                        Debug.Log($"ConstraintClipBaker frame={frame}, w={timeWeight:F3}, angle={angleError:F3}");
                    }
                }
                finally
                {
                    if (startedAnimationMode && AnimationMode.InAnimationMode())
                    {
                        AnimationMode.StopAnimationMode();
                    }

                    RestoreStates(transforms, originalStates);
                }
            }

            AnimationClip output = CreateOutputClip(
                source,
                muscleKeys,
                rootPositionKeys,
                rootRotationKeys);
            SaveOutputClip(output, outputPath);
        }

        public static void BakeLimbOffset(
            AnimationClip source,
            string outputPath,
            Animator animator,
            HumanBodyBones upperBone,
            HumanBodyBones lowerBone,
            HumanBodyBones endBone,
            Transform reference,
            Vector3 referenceOffset,
            int rampInStart,
            int holdStart,
            int holdEnd,
            int rampOutEnd)
        {
            ValidateLimbOffsetArguments(
                source,
                outputPath,
                animator,
                upperBone,
                lowerBone,
                endBone,
                reference,
                rampInStart,
                holdStart,
                holdEnd,
                rampOutEnd);

            AnimationCurve[] sourceRootPositionCurves = GetRequiredRootCurves(source, "RootT", 3);
            AnimationCurve[] sourceRootRotationCurves = GetRequiredRootCurves(source, "RootQ", 4);
            Transform upper = animator.GetBoneTransform(upperBone);
            Transform lower = animator.GetBoneTransform(lowerBone);
            Transform end = animator.GetBoneTransform(endBone);
            Transform[] transforms = animator.GetComponentsInChildren<Transform>(true);
            TransformState[] originalStates = CaptureStates(transforms);
            var startedAnimationMode = !AnimationMode.InAnimationMode();

            var lastFrame = Mathf.RoundToInt(source.length * source.frameRate);
            var keyCount = lastFrame + 1;
            Keyframe[][] muscleKeys = CreateKeyArrays(HumanTrait.MuscleCount, keyCount);
            Keyframe[][] rootPositionKeys = CreateKeyArrays(3, keyCount);
            Keyframe[][] rootRotationKeys = CreateKeyArrays(4, keyCount);
            var originPose = new HumanPose();
            var editedPose = new HumanPose();

            using (var poseHandler = new HumanPoseHandler(animator.avatar, animator.transform))
            {
                try
                {
                    if (startedAnimationMode)
                    {
                        AnimationMode.StartAnimationMode();
                    }

                    for (var frame = 0; frame <= lastFrame; frame++)
                    {
                        var time = Mathf.Min(frame / source.frameRate, source.length);
                        Sample(animator, source, time);
                        poseHandler.GetHumanPose(ref originPose);

                        var timeWeight = GetTimeWeight(
                            frame,
                            rampInStart,
                            holdStart,
                            holdEnd,
                            rampOutEnd);
                        Vector3 targetPosition = end.position
                            + reference.TransformVector(referenceOffset) * timeWeight;
                        if (timeWeight > 0f)
                        {
                            SolveTwoBoneIk(upper, lower, end, targetPosition);
                        }

                        var positionError = Vector3.Distance(targetPosition, end.position);
                        poseHandler.GetHumanPose(ref editedPose);
                        RecordPose(editedPose, time, frame, muscleKeys);
                        RecordRootPose(
                            originPose,
                            editedPose,
                            sourceRootPositionCurves,
                            sourceRootRotationCurves,
                            time,
                            frame,
                            rootPositionKeys,
                            rootRotationKeys);
                        Debug.Log(
                            $"ConstraintClipBaker frame={frame}, w={timeWeight:F3}, positionError={positionError:F6}m");
                    }
                }
                finally
                {
                    if (startedAnimationMode && AnimationMode.InAnimationMode())
                    {
                        AnimationMode.StopAnimationMode();
                    }

                    RestoreStates(transforms, originalStates);
                }
            }

            AnimationClip output = CreateOutputClip(
                source,
                muscleKeys,
                rootPositionKeys,
                rootRotationKeys);
            SaveOutputClip(output, outputPath);
        }

        private static void ValidateLimbOffsetArguments(
            AnimationClip source,
            string outputPath,
            Animator animator,
            HumanBodyBones upperBone,
            HumanBodyBones lowerBone,
            HumanBodyBones endBone,
            Transform reference,
            int rampInStart,
            int holdStart,
            int holdEnd,
            int rampOutEnd)
        {
            HumanBodyBones[] chain = { upperBone, lowerBone, endBone };
            float[] weights = { 1f, 1f, 1f };
            ValidateArguments(
                source,
                outputPath,
                animator,
                reference,
                reference,
                Vector3.one,
                chain,
                weights,
                rampInStart,
                holdStart,
                holdEnd,
                rampOutEnd);

            Transform[] bones = GetBones(animator, chain);
            if (!bones[1].IsChildOf(bones[0]) || !bones[2].IsChildOf(bones[1]))
            {
                throw new ArgumentException("위, 아래, 끝 뼈는 순서대로 이어진 계층이어야 한다.");
            }
        }

        private static void SolveTwoBoneIk(
            Transform upper,
            Transform lower,
            Transform end,
            Vector3 targetPosition)
        {
            Vector3 upperPosition = upper.position;
            Vector3 lowerPosition = lower.position;
            Vector3 endPosition = end.position;
            Quaternion originalEndLocalRotation = end.localRotation;
            var upperLength = Vector3.Distance(upperPosition, lowerPosition);
            var lowerLength = Vector3.Distance(lowerPosition, endPosition);
            if (upperLength <= Mathf.Epsilon || lowerLength <= Mathf.Epsilon)
            {
                throw new InvalidOperationException("두 관절 IK의 뼈 길이는 0보다 커야 한다.");
            }

            Vector3 targetDirection = targetPosition - upperPosition;
            if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                targetDirection = endPosition - upperPosition;
            }

            if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                targetDirection = lowerPosition - upperPosition;
            }

            targetDirection.Normalize();
            var minimumDistance = Mathf.Abs(upperLength - lowerLength);
            var maximumDistance = (upperLength + lowerLength) * 0.999f;
            var targetDistance = Mathf.Clamp(
                Vector3.Distance(upperPosition, targetPosition),
                minimumDistance,
                maximumDistance);
            Vector3 reachableTarget = upperPosition + targetDirection * targetDistance;
            Vector3 desiredLowerPosition;
            if (targetDistance <= Mathf.Epsilon)
            {
                desiredLowerPosition = upperPosition
                    + (lowerPosition - upperPosition).normalized * upperLength;
            }
            else
            {
                var elbowDistance = (
                    upperLength * upperLength
                    - lowerLength * lowerLength
                    + targetDistance * targetDistance) / (2f * targetDistance);
                var elbowHeight = Mathf.Sqrt(Mathf.Max(
                    0f,
                    upperLength * upperLength - elbowDistance * elbowDistance));
                Vector3 elbowHintDirection = lowerPosition - upperPosition;
                elbowHintDirection -= Vector3.Dot(elbowHintDirection, targetDirection) * targetDirection;
                if (elbowHintDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    elbowHintDirection = Vector3.Cross(targetDirection, upper.up);
                }

                if (elbowHintDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    elbowHintDirection = Vector3.Cross(targetDirection, upper.right);
                }

                elbowHintDirection.Normalize();
                desiredLowerPosition = upperPosition
                    + targetDirection * elbowDistance
                    + elbowHintDirection * elbowHeight;
            }

            upper.rotation = Quaternion.FromToRotation(
                lowerPosition - upperPosition,
                desiredLowerPosition - upperPosition) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(
                end.position - lower.position,
                reachableTarget - lower.position) * lower.rotation;
            end.localRotation = originalEndLocalRotation;
        }

        private static AnimationCurve[] GetRequiredRootCurves(
            AnimationClip source,
            string propertyPrefix,
            int curveCount)
        {
            string[] componentNames = { "x", "y", "z", "w" };
            var curves = new AnimationCurve[curveCount];
            for (var index = 0; index < curveCount; index++)
            {
                var propertyName = $"{propertyPrefix}.{componentNames[index]}";
                var binding = EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(Animator),
                    propertyName);
                curves[index] = AnimationUtility.GetEditorCurve(source, binding);
                if (curves[index] == null)
                {
                    throw new InvalidOperationException($"원본 클립에 {propertyName} 커브가 필요하다.");
                }
            }

            return curves;
        }

        private static void ValidateArguments(
            AnimationClip source,
            string outputPath,
            Animator animator,
            Transform effector,
            Transform eye,
            Vector3 targetDirection,
            HumanBodyBones[] chain,
            float[] weights,
            int rampInStart,
            int holdStart,
            int holdEnd,
            int rampOutEnd)
        {
            if (source == null || animator == null || effector == null || eye == null)
            {
                throw new ArgumentNullException("클립, Animator, 칼끝 Transform, 눈 Transform은 필수다.");
            }

            if (animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
            {
                throw new ArgumentException("Animator에 유효한 휴머노이드 Avatar가 필요하다.");
            }

            if (string.IsNullOrWhiteSpace(outputPath)
                || !outputPath.StartsWith("Assets/", StringComparison.Ordinal)
                || !outputPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("outputPath는 Assets 아래의 .anim 경로여야 한다.");
            }

            if (AssetDatabase.GetAssetPath(source) == outputPath)
            {
                throw new ArgumentException("원본 클립 경로에는 저장할 수 없다.");
            }

            if (targetDirection.sqrMagnitude == 0f)
            {
                throw new ArgumentException("targetDirection은 0이 아니어야 한다.");
            }

            if (chain == null || weights == null || chain.Length == 0 || chain.Length != weights.Length)
            {
                throw new ArgumentException("chain과 weights는 길이가 같은 비어 있지 않은 배열이어야 한다.");
            }

            if (rampInStart < 0
                || rampInStart > holdStart
                || holdStart > holdEnd
                || holdEnd > rampOutEnd)
            {
                throw new ArgumentException("시간 구간은 rampInStart부터 rampOutEnd까지 순서대로 지정해야 한다.");
            }

            var lastFrame = Mathf.RoundToInt(source.length * source.frameRate);
            if (rampOutEnd > lastFrame)
            {
                throw new ArgumentOutOfRangeException(nameof(rampOutEnd), "시간 구간이 원본 클립을 벗어났다.");
            }
        }

        private static Transform[] GetBones(Animator animator, HumanBodyBones[] chain)
        {
            var bones = new Transform[chain.Length];
            for (var index = 0; index < chain.Length; index++)
            {
                bones[index] = animator.GetBoneTransform(chain[index]);
                if (bones[index] == null)
                {
                    throw new ArgumentException($"Animator에서 {chain[index]} 뼈를 찾을 수 없다.");
                }
            }

            return bones;
        }

        private static TransformState[] CaptureStates(Transform[] transforms)
        {
            var states = new TransformState[transforms.Length];
            for (var index = 0; index < transforms.Length; index++)
            {
                Transform transform = transforms[index];
                states[index] = new TransformState(
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale);
            }

            return states;
        }

        private static void RestoreStates(Transform[] transforms, TransformState[] states)
        {
            for (var index = 0; index < transforms.Length; index++)
            {
                transforms[index].SetLocalPositionAndRotation(states[index].Position, states[index].Rotation);
                transforms[index].localScale = states[index].Scale;
            }
        }

        private static Keyframe[][] CreateKeyArrays(int curveCount, int keyCount)
        {
            var keys = new Keyframe[curveCount][];
            for (var index = 0; index < curveCount; index++)
            {
                keys[index] = new Keyframe[keyCount];
            }

            return keys;
        }

        private static void Sample(Animator animator, AnimationClip source, float time)
        {
            AnimationMode.BeginSampling();
            try
            {
                AnimationMode.SampleAnimationClip(animator.gameObject, source, time);
            }
            finally
            {
                AnimationMode.EndSampling();
            }
        }

        private static float ApplyConstraint(
            Transform root,
            Transform effector,
            Vector3 effectorLocalPoint,
            Transform eye,
            Vector3 targetDirection,
            Transform[] bones,
            float[] weights,
            float timeWeight,
            bool solve)
        {
            Vector3 originalEffectorPosition = effector.TransformPoint(effectorLocalPoint);
            Vector3 eyePosition = eye.position;
            var distance = Vector3.Distance(originalEffectorPosition, eyePosition);
            Vector3 worldTargetDirection = root.TransformDirection(targetDirection.normalized);
            Vector3 targetPosition = eyePosition + worldTargetDirection * distance;
            var originalRotations = new Quaternion[bones.Length];

            for (var index = 0; index < bones.Length; index++)
            {
                originalRotations[index] = bones[index].localRotation;
            }

            if (solve)
            {
                for (var iteration = 0; iteration < MaximumIterations; iteration++)
                {
                    var angleError = GetAngleError(
                        eyePosition,
                        effector.TransformPoint(effectorLocalPoint),
                        targetPosition);
                    if (angleError < MaximumAngleError)
                    {
                        break;
                    }

                    for (var index = 0; index < bones.Length; index++)
                    {
                        Transform bone = bones[index];
                        Vector3 effectorDirection = effector.TransformPoint(effectorLocalPoint) - bone.position;
                        Vector3 targetPointDirection = targetPosition - bone.position;
                        var correction = Quaternion.FromToRotation(
                            effectorDirection,
                            targetPointDirection);
                        bone.rotation = Quaternion.Slerp(
                            Quaternion.identity,
                            correction,
                            weights[index]) * bone.rotation;
                    }
                }
            }

            for (var index = 0; index < bones.Length; index++)
            {
                bones[index].localRotation = Quaternion.Slerp(
                    originalRotations[index],
                    bones[index].localRotation,
                    timeWeight);
            }

            return GetAngleError(
                eyePosition,
                effector.TransformPoint(effectorLocalPoint),
                targetPosition);
        }

        private static float GetAngleError(
            Vector3 eyePosition,
            Vector3 effectorPosition,
            Vector3 targetPosition)
        {
            return Vector3.Angle(
                effectorPosition - eyePosition,
                targetPosition - eyePosition);
        }

        private static float GetTimeWeight(
            int frame,
            int rampInStart,
            int holdStart,
            int holdEnd,
            int rampOutEnd)
        {
            if (frame < rampInStart || frame > rampOutEnd)
            {
                return 0f;
            }

            if (frame < holdStart)
            {
                return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(rampInStart, holdStart, frame));
            }

            if (frame <= holdEnd)
            {
                return 1f;
            }

            return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(holdEnd, rampOutEnd, frame));
        }

        private static void RecordPose(
            HumanPose pose,
            float time,
            int frame,
            Keyframe[][] muscleKeys)
        {
            for (var muscle = 0; muscle < pose.muscles.Length; muscle++)
            {
                muscleKeys[muscle][frame] = new Keyframe(time, pose.muscles[muscle]);
            }
        }

        private static void RecordRootPose(
            HumanPose originPose,
            HumanPose editedPose,
            AnimationCurve[] sourcePositionCurves,
            AnimationCurve[] sourceRotationCurves,
            float time,
            int frame,
            Keyframe[][] positionKeys,
            Keyframe[][] rotationKeys)
        {
            var sourcePosition = new Vector3(
                sourcePositionCurves[0].Evaluate(time),
                sourcePositionCurves[1].Evaluate(time),
                sourcePositionCurves[2].Evaluate(time));
            Vector3 position = sourcePosition + editedPose.bodyPosition - originPose.bodyPosition;
            positionKeys[0][frame] = new Keyframe(time, position.x);
            positionKeys[1][frame] = new Keyframe(time, position.y);
            positionKeys[2][frame] = new Keyframe(time, position.z);

            var sourceRotation = new Quaternion(
                sourceRotationCurves[0].Evaluate(time),
                sourceRotationCurves[1].Evaluate(time),
                sourceRotationCurves[2].Evaluate(time),
                sourceRotationCurves[3].Evaluate(time));
            Quaternion rotationDelta = Quaternion.Inverse(originPose.bodyRotation) * editedPose.bodyRotation;
            Quaternion rotation = (sourceRotation * rotationDelta).normalized;
            rotationKeys[0][frame] = new Keyframe(time, rotation.x);
            rotationKeys[1][frame] = new Keyframe(time, rotation.y);
            rotationKeys[2][frame] = new Keyframe(time, rotation.z);
            rotationKeys[3][frame] = new Keyframe(time, rotation.w);
        }

        private static AnimationClip CreateOutputClip(
            AnimationClip source,
            Keyframe[][] muscleKeys,
            Keyframe[][] rootPositionKeys,
            Keyframe[][] rootRotationKeys)
        {
            var output = new AnimationClip
            {
                frameRate = source.frameRate,
                wrapMode = source.wrapMode,
                name = source.name + "_Constrained"
            };

            CopySourceCurves(source, output);

            for (var muscle = 0; muscle < HumanTrait.MuscleCount; muscle++)
            {
                SetCurve(output, HumanTrait.MuscleName[muscle], muscleKeys[muscle]);
            }

            SetCurve(output, "RootT.x", rootPositionKeys[0]);
            SetCurve(output, "RootT.y", rootPositionKeys[1]);
            SetCurve(output, "RootT.z", rootPositionKeys[2]);
            SetCurve(output, "RootQ.x", rootRotationKeys[0]);
            SetCurve(output, "RootQ.y", rootRotationKeys[1]);
            SetCurve(output, "RootQ.z", rootRotationKeys[2]);
            SetCurve(output, "RootQ.w", rootRotationKeys[3]);

            output.EnsureQuaternionContinuity();
            AnimationUtility.SetAnimationClipSettings(
                output,
                AnimationUtility.GetAnimationClipSettings(source));
            AnimationUtility.SetAnimationEvents(output, AnimationUtility.GetAnimationEvents(source));
            return output;
        }

        private static void CopySourceCurves(AnimationClip source, AnimationClip output)
        {
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(source);
            foreach (EditorCurveBinding binding in curveBindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                AnimationUtility.SetEditorCurve(output, binding, curve);
            }

            EditorCurveBinding[] referenceBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            foreach (EditorCurveBinding binding in referenceBindings)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                AnimationUtility.SetObjectReferenceCurve(output, binding, keys);
            }
        }

        private static void SetCurve(AnimationClip clip, string propertyName, Keyframe[] keys)
        {
            var binding = EditorCurveBinding.FloatCurve(
                string.Empty,
                typeof(Animator),
                propertyName);
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < keys.Length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void SaveOutputClip(AnimationClip output, string outputPath)
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(output, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(output, outputPath);
            }

            AssetDatabase.SaveAssets();
        }

        private readonly struct TransformState
        {
            public TransformState(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public Vector3 Position { get; }

            public Quaternion Rotation { get; }

            public Vector3 Scale { get; }
        }
    }
}
