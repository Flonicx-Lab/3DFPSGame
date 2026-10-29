using System;
using UnityEditor;
using UnityEngine;

namespace FpsGame.Editor
{
    // 휴머노이드 클립의 지정 구간에 관절 사슬 제약을 적용해 새 클립으로 저장한다.
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

            Transform[] bones = GetBones(animator, chain);
            Transform[] transforms = animator.GetComponentsInChildren<Transform>(true);
            TransformState[] originalStates = CaptureStates(transforms);
            bool startedAnimationMode = !AnimationMode.InAnimationMode();

            int lastFrame = Mathf.RoundToInt(source.length * source.frameRate);
            int keyCount = lastFrame + 1;
            Keyframe[][] muscleKeys = CreateKeyArrays(HumanTrait.MuscleCount, keyCount);
            Keyframe[][] rootPositionKeys = CreateKeyArrays(3, keyCount);
            Keyframe[][] rootRotationKeys = CreateKeyArrays(4, keyCount);
            HumanPose pose = new HumanPose();

            using (HumanPoseHandler poseHandler = new HumanPoseHandler(animator.avatar, animator.transform))
            {
                try
                {
                    if (startedAnimationMode)
                    {
                        AnimationMode.StartAnimationMode();
                    }

                    for (int frame = 0; frame <= lastFrame; frame++)
                    {
                        float time = Mathf.Min(frame / source.frameRate, source.length);
                        Sample(animator, source, time);

                        float timeWeight = GetTimeWeight(
                            frame,
                            rampInStart,
                            holdStart,
                            holdEnd,
                            rampOutEnd);
                        float angleError = ApplyConstraint(
                            animator.transform,
                            effector,
                            effectorLocalPoint,
                            eye,
                            targetDirection,
                            bones,
                            weights,
                            timeWeight,
                            frame >= rampInStart && frame <= rampOutEnd);

                        poseHandler.GetHumanPose(ref pose);
                        RecordPose(
                            pose,
                            time,
                            frame,
                            muscleKeys,
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

            int lastFrame = Mathf.RoundToInt(source.length * source.frameRate);
            if (rampOutEnd > lastFrame)
            {
                throw new ArgumentOutOfRangeException(nameof(rampOutEnd), "시간 구간이 원본 클립을 벗어났다.");
            }
        }

        private static Transform[] GetBones(Animator animator, HumanBodyBones[] chain)
        {
            Transform[] bones = new Transform[chain.Length];
            for (int index = 0; index < chain.Length; index++)
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
            TransformState[] states = new TransformState[transforms.Length];
            for (int index = 0; index < transforms.Length; index++)
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
            for (int index = 0; index < transforms.Length; index++)
            {
                transforms[index].SetLocalPositionAndRotation(states[index].Position, states[index].Rotation);
                transforms[index].localScale = states[index].Scale;
            }
        }

        private static Keyframe[][] CreateKeyArrays(int curveCount, int keyCount)
        {
            Keyframe[][] keys = new Keyframe[curveCount][];
            for (int index = 0; index < curveCount; index++)
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
            float distance = Vector3.Distance(originalEffectorPosition, eyePosition);
            Vector3 worldTargetDirection = root.TransformDirection(targetDirection.normalized);
            Vector3 targetPosition = eyePosition + worldTargetDirection * distance;
            Quaternion[] originalRotations = new Quaternion[bones.Length];

            for (int index = 0; index < bones.Length; index++)
            {
                originalRotations[index] = bones[index].localRotation;
            }

            if (solve)
            {
                for (int iteration = 0; iteration < MaximumIterations; iteration++)
                {
                    float angleError = GetAngleError(
                        eyePosition,
                        effector.TransformPoint(effectorLocalPoint),
                        targetPosition);
                    if (angleError < MaximumAngleError)
                    {
                        break;
                    }

                    for (int index = 0; index < bones.Length; index++)
                    {
                        Transform bone = bones[index];
                        Vector3 effectorDirection = effector.TransformPoint(effectorLocalPoint) - bone.position;
                        Vector3 targetPointDirection = targetPosition - bone.position;
                        Quaternion correction = Quaternion.FromToRotation(
                            effectorDirection,
                            targetPointDirection);
                        bone.rotation = Quaternion.Slerp(
                            Quaternion.identity,
                            correction,
                            weights[index]) * bone.rotation;
                    }
                }
            }

            for (int index = 0; index < bones.Length; index++)
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
            Keyframe[][] muscleKeys,
            Keyframe[][] rootPositionKeys,
            Keyframe[][] rootRotationKeys)
        {
            for (int muscle = 0; muscle < pose.muscles.Length; muscle++)
            {
                muscleKeys[muscle][frame] = new Keyframe(time, pose.muscles[muscle]);
            }

            rootPositionKeys[0][frame] = new Keyframe(time, pose.bodyPosition.x);
            rootPositionKeys[1][frame] = new Keyframe(time, pose.bodyPosition.y);
            rootPositionKeys[2][frame] = new Keyframe(time, pose.bodyPosition.z);
            rootRotationKeys[0][frame] = new Keyframe(time, pose.bodyRotation.x);
            rootRotationKeys[1][frame] = new Keyframe(time, pose.bodyRotation.y);
            rootRotationKeys[2][frame] = new Keyframe(time, pose.bodyRotation.z);
            rootRotationKeys[3][frame] = new Keyframe(time, pose.bodyRotation.w);
        }

        private static AnimationClip CreateOutputClip(
            AnimationClip source,
            Keyframe[][] muscleKeys,
            Keyframe[][] rootPositionKeys,
            Keyframe[][] rootRotationKeys)
        {
            AnimationClip output = new AnimationClip
            {
                frameRate = source.frameRate,
                wrapMode = source.wrapMode,
                name = source.name + "_Constrained"
            };

            CopySourceCurves(source, output);

            for (int muscle = 0; muscle < HumanTrait.MuscleCount; muscle++)
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
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                string.Empty,
                typeof(Animator),
                propertyName);
            var curve = new AnimationCurve(keys);
            for (int index = 0; index < keys.Length; index++)
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
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputPath) != null)
            {
                AssetDatabase.DeleteAsset(outputPath);
            }

            AssetDatabase.CreateAsset(output, outputPath);
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
