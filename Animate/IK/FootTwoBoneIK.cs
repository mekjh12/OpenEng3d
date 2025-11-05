using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    public class FootTwoBoneIK : TwoBoneIK
    {
        Vertex3f _poleVectorTemp = default;


        public FootTwoBoneIK(Bone upperBone, Bone lowerBone, Bone endBone) :
            base(upperBone, lowerBone, endBone)
        {

        }

        protected override Vertex3f SolveInternal(Vertex3f targetPositionWorld, Vertex3f poleVector, Matrix4x4f modelMatrix, Animator animator)
        {
            // 폴벡터 임시 저장
            _poleVectorTemp = poleVector;

            // 기본 폴벡터 방향 (외부에서 전달받은 값)
            Vertex3f forward = poleVector;

            // 위쪽 성분이 강화된 폴벡터 (타겟 방향 + 위쪽)
            // 계수 3.0f는 Z 성분을 강조하여 무릎이 더 위를 향하게 만든다
            Vertex3f up = _targetDir + Vertex3f.UnitZ * 3.0f;

            // 폴벡터와 타겟 방향의 유사도 계산 (-1: 반대, 0: 수직, 1: 동일)
            float dot = poleVector.Normalized.Dot(_targetDir.Normalized);

            // 타겟이 엉덩이보다 위에 있는 경우 (계단 오르기, 점프 착지 등)
            if (_targetDir.z > 0)
            {
                // 무조건 위쪽을 향하는 폴벡터 사용
                return up;
            }
            else
            {
                // 타겟이 아래에 있는 경우 (평지 걷기, 계단 내려가기)
                if (dot < 0)
                {
                    // 폴벡터가 타겟과 반대 방향이면 원래 폴벡터 유지
                    // (이미 올바른 방향이므로 조정 불필요)
                    return forward;
                }
                else
                {
                    // 폴벡터가 타겟과 비슷한 방향이면 위쪽 성분 혼합
                    // dot 값에 비례해서 up 방향으로 보간
                    // dot=0: forward, dot=1: up
                    return up * dot + forward * (1 - dot);
                }
            }
        }
    }
}
