using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace FpsGame.Editor
{
    // 에디트 모드에서 PlayableGraph를 통해 지정 시간의 애니메이션 자세와 무기 위치를 측정한다.
    public static class AnimationProbe
    {
        private static readonly Dictionary<Mesh, Dictionary<int, int>> s_hiltVertexCache =
            new Dictionary<Mesh, Dictionary<int, int>>();

        public static string Measure(
            Animator animator,
            AnimationClip baseClip,
            AnimationClip additiveClip,
            AvatarMask additiveMask,
            float time,
            Transform weapon,
            Vector3 weaponLocalPosition,
            Vector3 weaponLocalEulerAngles,
            MeshFilter weaponMesh,
            int tipVertex,
            Transform eye,
            Transform reference)
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("플레이 모드에서는 애니메이션 자세를 측정할 수 없습니다.");
            }

            Require(animator, nameof(animator));
            Require(baseClip, nameof(baseClip));
            Require(weapon, nameof(weapon));
            Require(weaponMesh, nameof(weaponMesh));
            Require(weaponMesh.sharedMesh, "weaponMesh.sharedMesh");
            Require(eye, nameof(eye));
            Require(reference, nameof(reference));

            if (additiveClip != null && additiveMask == null)
            {
                throw new ArgumentNullException(nameof(additiveMask));
            }

            Mesh mesh = weaponMesh.sharedMesh;
            if (tipVertex < 0 || tipVertex >= mesh.vertexCount)
            {
                throw new ArgumentOutOfRangeException(nameof(tipVertex), tipVertex, "칼끝 정점 번호가 메시 범위를 벗어났습니다.");
            }

            Vector3 originalLocalPosition = weapon.localPosition;
            Quaternion originalLocalRotation = weapon.localRotation;
            PlayableGraph graph = default;

            try
            {
                graph = CreateGraph(animator, baseClip, additiveClip, additiveMask, time);
                graph.Evaluate(0f);

                weapon.localPosition = weaponLocalPosition;
                weapon.localEulerAngles = weaponLocalEulerAngles;

                return CreateResult(animator, weaponMesh, mesh, tipVertex, eye, reference);
            }
            finally
            {
                if (graph.IsValid())
                {
                    graph.Destroy();
                }

                weapon.localPosition = originalLocalPosition;
                weapon.localRotation = originalLocalRotation;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static PlayableGraph CreateGraph(
            Animator animator,
            AnimationClip baseClip,
            AnimationClip additiveClip,
            AvatarMask additiveMask,
            float time)
        {
            var graph = PlayableGraph.Create("AnimationProbe");
            try
            {
                var output = AnimationPlayableOutput.Create(graph, "AnimationProbeOutput", animator);

                var inputCount = additiveClip == null ? 1 : 2;
                var mixer = AnimationLayerMixerPlayable.Create(graph, inputCount);

                var basePlayable = AnimationClipPlayable.Create(graph, baseClip);
                graph.Connect(basePlayable, 0, mixer, 0);
                mixer.SetInputWeight(0, 1f);

                AnimationClipPlayable additivePlayable = default;
                if (additiveClip != null)
                {
                    additivePlayable = AnimationClipPlayable.Create(graph, additiveClip);
                    graph.Connect(additivePlayable, 0, mixer, 1);
                    mixer.SetInputWeight(1, 1f);
                    mixer.SetLayerAdditive(1, true);
                    mixer.SetLayerMaskFromAvatarMask(1, additiveMask);
                }

                output.SetSourcePlayable(mixer);

                basePlayable.SetTime(time);
                basePlayable.SetTime(time);

                if (additiveClip != null)
                {
                    additivePlayable.SetTime(time);
                    additivePlayable.SetTime(time);
                }

                return graph;
            }
            catch
            {
                if (graph.IsValid())
                {
                    graph.Destroy();
                }

                throw;
            }
        }

        private static string CreateResult(
            Animator animator,
            MeshFilter weaponMesh,
            Mesh mesh,
            int tipVertex,
            Transform eye,
            Transform reference)
        {
            Transform shoulder = RequireBone(animator, HumanBodyBones.RightUpperArm);
            Transform elbow = RequireBone(animator, HumanBodyBones.RightLowerArm);
            Transform hand = RequireBone(animator, HumanBodyBones.RightHand);

            var hiltVertex = GetHiltVertex(mesh, tipVertex);
            Vector3[] vertices = mesh.vertices;
            Vector3 tipPosition = weaponMesh.transform.TransformPoint(vertices[tipVertex]);
            Vector3 hiltPosition = weaponMesh.transform.TransformPoint(vertices[hiltVertex]);

            Vector3 forward = tipPosition - eye.position;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new InvalidOperationException("눈과 칼끝의 위치가 같아 시선 기준을 만들 수 없습니다.");
            }

            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new InvalidOperationException("시선 방향이 월드 위 방향과 나란해 시선 기준을 만들 수 없습니다.");
            }

            right.Normalize();
            Vector3 up = Vector3.Cross(forward, right).normalized;
            Vector3 forearmDirection = (hand.position - elbow.position).normalized;
            Vector3 bladeDirection = (tipPosition - hiltPosition).normalized;
            var forearmUp = Vector3.Dot(forearmDirection, up);
            var bladeLeft = -Vector3.Dot(bladeDirection, right);
            var elbowAngle = Vector3.Angle(shoulder.position - elbow.position, hand.position - elbow.position);

            Vector3 eyeToHand = hand.position - eye.position;
            var handRight = Vector3.Dot(eyeToHand, right);
            var handUp = Vector3.Dot(eyeToHand, up);
            var handForward = Vector3.Dot(eyeToHand, forward);
            var yaw = Mathf.Atan2(handRight, handForward) * Mathf.Rad2Deg;
            var pitch = Mathf.Atan2(handUp, Mathf.Sqrt((handRight * handRight) + (handForward * handForward))) * Mathf.Rad2Deg;
            Vector3 tipLocal = reference.InverseTransformPoint(tipPosition);

            return string.Format(
                CultureInfo.InvariantCulture,
                "forearmUp={0:0.00} bladeLeft={1:0.00} elbow={2:000} hand(yaw={3:0.0},pitch={4:0.0}) tipLocal=({5:0.000},{6:0.000},{7:0.000})",
                forearmUp,
                bladeLeft,
                elbowAngle,
                yaw,
                pitch,
                tipLocal.x,
                tipLocal.y,
                tipLocal.z);
        }

        private static int GetHiltVertex(Mesh mesh, int tipVertex)
        {
            if (!s_hiltVertexCache.TryGetValue(mesh, out Dictionary<int, int> meshCache))
            {
                meshCache = new Dictionary<int, int>();
                s_hiltVertexCache.Add(mesh, meshCache);
            }

            if (meshCache.TryGetValue(tipVertex, out var cachedVertex))
            {
                return cachedVertex;
            }

            Vector3[] vertices = mesh.vertices;
            Vector3 tip = vertices[tipVertex];
            var farthestVertex = 0;
            var farthestDistance = -1f;

            for (var index = 0; index < vertices.Length; index++)
            {
                var distance = (vertices[index] - tip).sqrMagnitude;
                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestVertex = index;
                }
            }

            meshCache.Add(tipVertex, farthestVertex);
            return farthestVertex;
        }

        private static Transform RequireBone(Animator animator, HumanBodyBones bone)
        {
            Transform transform = animator.GetBoneTransform(bone);
            if (transform == null)
            {
                throw new InvalidOperationException($"Animator에서 {bone} 뼈를 찾을 수 없습니다.");
            }

            return transform;
        }

        private static void Require(UnityEngine.Object value, string targetName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(targetName, $"필수 대상 {targetName}을 찾을 수 없습니다.");
            }
        }
    }
}
