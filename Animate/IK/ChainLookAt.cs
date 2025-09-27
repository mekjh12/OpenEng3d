using OpenGL;
using System;
using System.Collections.Generic;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 여러 본이 연쇄적으로 특정 방향을 바라보도록 하는 Chain Look At IK
    /// 목-어깨-척추가 자연스럽게 함께 움직인다
    /// </summary>
    public class ChainLookAt
    {
        private List<ChainBone> _boneChain;
        private Vertex3f _localForward;
        private Vertex3f _localUp;

        /// <summary>
        /// 체인 내 본 정보
        /// </summary>
        private struct ChainBone
        {
            public Bone Bone;
            public float Weight;         // 0~1: 이 본이 받을 영향의 비율
            public float MaxAngle;       // 최대 회전 각도 (도)

            public ChainBone(Bone bone, float weight, float maxAngle = 180f)
            {
                Bone = bone;
                Weight = weight;
                MaxAngle = maxAngle;
            }
        }

        public ChainLookAt(Vertex3f localForward = default, Vertex3f localUp = default)
        {
            _boneChain = new List<ChainBone>();
            _localForward = localForward == default ? Vertex3f.UnitY : localForward.Normalized;
            _localUp = localUp == default ? Vertex3f.UnitZ : localUp.Normalized;
        }

        /// <summary>
        /// 체인에 본 추가 (루트에서 끝까지 순서대로 추가)
        /// </summary>
        /// <param name="bone">추가할 본</param>
        /// <param name="weight">영향 비율 (0~1)</param>
        /// <param name="maxAngleDegrees">최대 회전 각도</param>
        public void AddBone(Bone bone, float weight, float maxAngleDegrees = 180f)
        {
            _boneChain.Add(new ChainBone(bone, weight, maxAngleDegrees));
        }

        /// <summary>
        /// 인간형 캐릭터의 기본 Look At 체인 설정
        /// </summary>
        public void SetupHumanLookChain(Armature armature, float intensity = 1.0f)
        {
            // 척추부터 머리까지 설정 (아래에서 위로)
            var spine2 = armature["mixamorig_Spine2"];
            var spine1 = armature["mixamorig_Spine1"];
            var neck = armature["mixamorig_Neck"];
            var head = armature["mixamorig_Head"];

            // 강도를 조절하여 체인 구성
            if (spine2 != null) AddBone(spine2, 0.15f * intensity, 15f);  // 상부 척추: 약간만
            if (spine1 != null) AddBone(spine1, 0.25f * intensity, 20f);  // 중부 척추: 조금 더
            if (neck != null) AddBone(neck, 0.6f * intensity, 45f);       // 목: 주요 회전
            if (head != null) AddBone(head, 1.0f * intensity, 60f);       // 머리: 최대 회전
        }

        /// <summary>
        /// 특정 월드 위치를 바라보도록 설정
        /// </summary>
        public Vertex3f LookAt(Vertex3f worldTargetPosition, Matrix4x4f model, Animator animator, Vertex3f worldUpHint = default)
        {
            if (_boneChain.Count == 0) return Vertex3f.Zero;

            // 마지막 본(보통 머리)의 위치를 기준으로 방향 계산
            Bone endBone = _boneChain[_boneChain.Count - 1].Bone;
            Matrix4x4f endWorldTransform = model * animator.GetRootTransform(endBone);
            Vertex3f currentWorldPosition = endWorldTransform.Position;

            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;
            Vertex3f toTarget = (worldTargetPosition - currentWorldPosition).Normalized;

            // 각 본에 가중치를 적용하여 Look At 적용
            ApplyChainLookAt(toTarget, worldUpHint, model, animator);

            return currentWorldPosition;
        }

        /// <summary>
        /// 특정 방향을 바라보도록 설정
        /// </summary>
        public Vertex3f LookDirection(Vertex3f worldDirection, Matrix4x4f model, Animator animator, Vertex3f worldUpHint = default)
        {
            if (_boneChain.Count == 0) return Vertex3f.Zero;

            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;
            Vertex3f normalizedDirection = worldDirection.Normalized;

            ApplyChainLookAt(normalizedDirection, worldUpHint, model, animator);

            // 마지막 본의 위치 반환
            Bone endBone = _boneChain[_boneChain.Count - 1].Bone;
            Matrix4x4f endWorldTransform = model * animator.GetRootTransform(endBone);
            return endWorldTransform.Position;
        }

        /// <summary>
        /// 체인 전체에 Look At 적용
        /// </summary>
        private void ApplyChainLookAt(Vertex3f worldTargetDirection, Vertex3f worldUpHint, Matrix4x4f model, Animator animator)
        {
            // 각 본에 순차적으로 가중치 적용된 Look At 적용
            for (int i = 0; i < _boneChain.Count; i++)
            {
                ChainBone chainBone = _boneChain[i];

                // 가중치만큼 방향 조정
                Vertex3f weightedDirection = ApplyWeight(worldTargetDirection, chainBone.Weight);

                // 각도 제한 적용
                weightedDirection = ApplyAngleLimit(chainBone.Bone, weightedDirection, chainBone.MaxAngle, model, animator);

                // Look At 계산 및 적용
                Matrix4x4f lookAtRotation = CalculateLookAtRotation(
                    chainBone.Bone, weightedDirection, worldUpHint, model, animator);

                chainBone.Bone.BoneMatrixSet.LocalTransform = lookAtRotation;
                chainBone.Bone.UpdateAnimatorTransforms(animator, true);
            }
        }

        /// <summary>
        /// 가중치를 적용한 방향 계산
        /// </summary>
        private Vertex3f ApplyWeight(Vertex3f targetDirection, float weight)
        {
            // 원래 전방 방향(보통 Y축)과 목표 방향 사이를 가중치로 보간
            Vertex3f originalDirection = Vertex3f.UnitY; // 또는 본의 원래 방향
            return (originalDirection * (1 - weight) + targetDirection * weight).Normalized;
        }

        /// <summary>
        /// 각도 제한 적용
        /// </summary>
        private Vertex3f ApplyAngleLimit(Bone bone, Vertex3f targetDirection, float maxAngleDegrees, Matrix4x4f model, Animator animator)
        {
            if (maxAngleDegrees >= 180f) return targetDirection;

            // 원래 방향과 목표 방향 사이의 각도 계산
            Vertex3f originalDirection = _localForward;
            float angle = (float)(Math.Acos(Math.Max(-1f, Math.Min(1f, originalDirection.Dot(targetDirection)))) * 180f / Math.PI);

            if (angle > maxAngleDegrees)
            {
                // 최대 각도로 제한
                float t = maxAngleDegrees / angle;
                return (originalDirection * (1 - t) + targetDirection * t).Normalized;
            }

            return targetDirection;
        }

        /// <summary>
        /// 단일 본에 대한 Look At 회전 계산
        /// </summary>
        private Matrix4x4f CalculateLookAtRotation(Bone bone, Vertex3f worldTargetDirection, Vertex3f worldUpHint, Matrix4x4f model, Animator animator)
        {
            // 현재 본의 월드 변환
            Matrix4x4f currentWorldTransform = animator.GetRootTransform(bone);
            Matrix4x4f finalWorldTransform = model * currentWorldTransform;

            // 로컬 공간으로 변환
            Matrix4x4f parentWorldTransform = Matrix4x4f.Identity;
            if (bone.Parent != null)
            {
                parentWorldTransform = finalWorldTransform * bone.BoneMatrixSet.LocalTransform.Inversed();
            }

            Matrix4x4f worldToParentLocal = parentWorldTransform.Inversed();
            Vertex4f localDir = worldToParentLocal * new Vertex4f(worldTargetDirection.x, worldTargetDirection.y, worldTargetDirection.z, 0);
            Vertex4f localUp = worldToParentLocal * new Vertex4f(worldUpHint.x, worldUpHint.y, worldUpHint.z, 0);

            Vertex3f targetDir = new Vertex3f(localDir.x, localDir.y, localDir.z).Normalized;
            Vertex3f upHint = new Vertex3f(localUp.x, localUp.y, localUp.z).Normalized;

            // From 좌표계 (원래 본의 로컬 축들)
            Vertex3f fromForward = _localForward;
            Vertex3f fromRight = fromForward.Cross(_localUp).Normalized;
            Vertex3f fromUp = fromRight.Cross(fromForward).Normalized;

            // To 좌표계 (목표 방향)
            Vertex3f toForward = targetDir;
            Vertex3f toRight = toForward.Cross(upHint).Normalized;
            if (toRight.Length() < 0.001f)
            {
                Vertex3f altUp = Math.Abs(toForward.Dot(Vertex3f.UnitX)) < 0.9f ? Vertex3f.UnitX : Vertex3f.UnitY;
                toRight = toForward.Cross(altUp).Normalized;
            }
            Vertex3f toUp = toRight.Cross(toForward).Normalized;

            // 변환 행렬 계산
            Matrix4x4f fromMatrix = new Matrix4x4f(
                fromRight.x, fromRight.y, fromRight.z, 0,
                fromForward.x, fromForward.y, fromForward.z, 0,
                fromUp.x, fromUp.y, fromUp.z, 0,
                0, 0, 0, 1);

            Matrix4x4f toMatrix = new Matrix4x4f(
                toRight.x, toRight.y, toRight.z, 0,
                toForward.x, toForward.y, toForward.z, 0,
                toUp.x, toUp.y, toUp.z, 0,
                0, 0, 0, 1);

            Matrix4x4f transform = toMatrix * fromMatrix.Inversed();

            // 위치 유지
            Vertex3f pos = bone.BoneMatrixSet.LocalTransform.Position;
            transform[3, 0] = pos.x;
            transform[3, 1] = pos.y;
            transform[3, 2] = pos.z;

            return transform;
        }

        /// <summary>
        /// 체인 초기화 (원래 포즈로 복원)
        /// </summary>
        public void Reset(Animator animator)
        {
            foreach (var chainBone in _boneChain)
            {
                chainBone.Bone.BoneMatrixSet.LocalTransform = chainBone.Bone.BoneMatrixSet.LocalBindTransform;
                chainBone.Bone.UpdateAnimatorTransforms(animator, true);
            }
        }
    }
}