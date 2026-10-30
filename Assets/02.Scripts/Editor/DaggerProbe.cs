using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FpsGame.Editor
{
    public static class DaggerProbe
    {
        private const int BaseLayerIndex = 4;
        private const int AdditiveLayerIndex = 6;
        private const int TipVertex = 2073;

        public static string Measure(string stateName, int frame)
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("플레이 모드에서는 애니메이션 자세를 측정할 수 없습니다.");
            }

            if (string.IsNullOrWhiteSpace(stateName))
            {
                throw new ArgumentException("상태 이름이 비어 있습니다.", nameof(stateName));
            }

            GameObject player = Require(GameObject.Find("Player"), "Player");
            Transform model = Require(player.transform.Find("Model"), "Player/Model");
            Animator animator = Require(model.GetComponent<Animator>(), "Player/Model의 Animator");
            AnimatorController controller = Require(animator.runtimeAnimatorController as AnimatorController, "PlayerDaggerAim AnimatorController");

            if (controller.layers.Length <= AdditiveLayerIndex)
            {
                throw new InvalidOperationException("Animator의 Dagger 또는 Dagger Additive Layer를 찾을 수 없습니다.");
            }

            AnimatorState state = FindState(controller.layers[BaseLayerIndex].stateMachine, stateName);
            if (state == null)
            {
                throw new InvalidOperationException($"Dagger 레이어에서 상태 {stateName}을 찾을 수 없습니다.");
            }

            AnimationClip baseClip = Require(state.motion as AnimationClip, $"상태 {stateName}의 원본 AnimationClip");
            AnimatorControllerLayer additiveLayer = controller.layers[AdditiveLayerIndex];
            AnimationClip additiveClip = additiveLayer.GetOverrideMotion(state) as AnimationClip;
            AvatarMask additiveMask = additiveLayer.avatarMask;
            if (additiveClip != null)
            {
                Require(additiveMask, "Dagger Additive Layer의 AvatarMask");
            }

            Transform hand = Require(animator.GetBoneTransform(HumanBodyBones.RightHand), "오른손 뼈");
            Transform dagger = Require(hand.Find("Dagger"), "RightHand/Dagger");
            MeshFilter weaponMesh = Require(dagger.GetComponent<MeshFilter>(), "RightHand/Dagger의 MeshFilter");
            Transform eye = Require(FindDescendant(player.transform, "CameraPosition"), "CameraPosition");
            Transform reference = Require(animator.GetBoneTransform(HumanBodyBones.Chest), "Chest 뼈 Spine1");

            Vector3 weaponLocalPosition = dagger.localPosition;
            Vector3 weaponLocalEulerAngles = dagger.localEulerAngles;
            if (string.Equals(stateName, "Stab", StringComparison.Ordinal))
            {
                ReadReverseGrip(dagger, out weaponLocalPosition, out weaponLocalEulerAngles);
            }

            if (baseClip.frameRate <= 0f)
            {
                throw new InvalidOperationException($"상태 {stateName}의 원본 클립 frameRate가 0 이하입니다.");
            }

            float time = frame / baseClip.frameRate;
            string result = AnimationProbe.Measure(
                animator,
                baseClip,
                additiveClip,
                additiveMask,
                time,
                dagger,
                weaponLocalPosition,
                weaponLocalEulerAngles,
                weaponMesh,
                TipVertex,
                eye,
                reference);

            return $"state={stateName} frame={frame} {result}";
        }

        private static void ReadReverseGrip(Transform dagger, out Vector3 localPosition, out Vector3 localEulerAngles)
        {
            MonoBehaviour daggerGrip = null;
            MonoBehaviour[] behaviours = dagger.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "DaggerGrip")
                {
                    daggerGrip = behaviour;
                    break;
                }
            }

            if (daggerGrip == null)
            {
                throw new InvalidOperationException("RightHand/Dagger에서 DaggerGrip을 찾을 수 없습니다.");
            }

            SerializedObject serializedGrip = new SerializedObject(daggerGrip);
            SerializedProperty positionProperty = serializedGrip.FindProperty("_reverseLocalPosition");
            SerializedProperty eulerProperty = serializedGrip.FindProperty("_reverseLocalEulerAngles");
            if (positionProperty == null || eulerProperty == null)
            {
                throw new InvalidOperationException("DaggerGrip에서 역수 위치 또는 회전 값을 찾을 수 없습니다.");
            }

            localPosition = positionProperty.vector3Value;
            localEulerAngles = eulerProperty.vector3Value;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (string.Equals(childState.state.name, stateName, StringComparison.Ordinal))
                {
                    return childState.state;
                }
            }

            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            {
                AnimatorState state = FindState(childStateMachine.stateMachine, stateName);
                if (state != null)
                {
                    return state;
                }
            }

            return null;
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            if (string.Equals(root.name, targetName, StringComparison.Ordinal))
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform result = FindDescendant(child, targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static T Require<T>(T value, string targetName)
            where T : UnityEngine.Object
        {
            if (value == null)
            {
                throw new InvalidOperationException($"필수 대상 {targetName}을 찾을 수 없습니다.");
            }

            return value;
        }
    }
}
