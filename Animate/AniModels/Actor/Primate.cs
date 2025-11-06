using Microsoft.SqlServer.Server;
using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 영장류 클래스 - 인간, 원숭이, 유인원 등을 포함하는 포유류 목
    /// <code>
    /// 영장류의 주요 특징:
    /// - 발달된 뇌와 높은 지능, 복잡한 사회 구조
    /// - 정교한 손가락 움직임과 엄지손가락 대립 가능
    /// - 양안시(입체시)가 가능한 전방향 눈
    /// - 직립보행 또는 팔을 이용한 나무 위 이동
    /// - 도구 사용 능력과 학습 능력
    /// - 긴 수명과 느린 성장 과정
    /// 
    /// 3D 그래픽스 구현 완료:
    /// - 손가락 관절 애니메이션 (접기/펴기)
    /// - 눈동자 추적 시스템
    /// - 신체 부위별 아이템 장착 시스템
    /// 
    /// TODO: 표정 변화 시스템, 입 모양 변형, 헤어/털 시뮬레이션,
    /// 근육 변형 애니메이션, 걸음걸이 패턴 다양화
    /// </code>
    /// </summary>
    public abstract class Primate<TAction> : AnimActor<TAction> where TAction : struct, Enum
    {
        protected Dictionary<string, Action> _actions;

        public Primate(string name, AnimRig aniRig, TAction defaultAction) : base(name, aniRig, defaultAction)
        {
            _actions = new Dictionary<string, Action>();
        }


    }
}