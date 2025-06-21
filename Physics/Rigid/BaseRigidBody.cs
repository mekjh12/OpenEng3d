using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZetaExt;

namespace Physics.Rigid
{
    public abstract class BaseRigidBody
    {
        static int GUID = 0;                                           // 강체의 고유번호 생성을 위한 전역번호

        protected Vertex3f _color = Vertex3f.Zero;                     // 렌더링 색상
        protected int _guid;                                           // 고유 식별자
        protected float _life = float.MaxValue;                        // 수명

        public BaseRigidBody()
        {
            GUID++;
            _guid = GUID;
            _color = Rand.NextColor3f;
        }

        /// <summary>
        /// 강체의 고유번호
        /// </summary>
        public int Guid => _guid;

        /// <summary>
        /// 강체가 지속될 시간(단위는 초)
        /// </summary>
        public float Life
        {
            get => _life;
            set => _life = value;
        }

        /// <summary>
        /// 강체의 렌더링 디버깅을 위한 색상
        /// </summary>
        public Vertex3f Color
        {
            get => _color;
            set => _color = value;
        }
    }
}
