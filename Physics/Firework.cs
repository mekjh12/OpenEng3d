using OpenGL;
using ZetaExt;

namespace Physics
{
    public class Firework: Particle
    {
        /// <summary>
        /// 불꽃놀이 규칙에 의해 심지의 지속 시간과 어떤 입자로 바뀌는지를 제어한다.
        /// </summary>
        public struct FireWorkRule
        {
            /// <summary>
            /// 페이로드는 심지가 다 타서 화약이 폭발하면 발생할 새로운 불꽃이다.
            /// </summary>
            public struct PayLoad
            {
                /// <summary>
                /// 생성할 새로운 입자의 종류
                /// </summary>
                public uint Type;

                /// <summary>
                /// 페이로드의 입자의 개수
                /// </summary>
                public uint Count;

                /// <summary>
                /// 생성자
                /// </summary>
                /// <param name="type"></param>
                /// <param name="count"></param>
                public void Set(uint type, uint count)
                {
                    Type = type;
                    Count = count;
                }
            }

            /// <summary>
            /// 규칙이 적용되는 불꽃의 종류
            /// </summary>
            public uint Type;

            /// <summary>
            /// 심지의 최소 지속시간
            /// </summary>
            public float MinAge;

            /// <summary>
            /// 심지의 최대 지속시간
            /// </summary>
            public float MaxAge;

            /// <summary>
            /// 불꽃 입자의 최소 상대 속도
            /// </summary>
            public Vertex3f MinVelocity;

            /// <summary>
            /// 불꽃 입자의 최대 상대 속도
            /// </summary>
            public Vertex3f MaxVelocity;

            /// <summary>
            /// 불꽃의 댐핑 계수
            /// </summary>
            public float Damping;

            /// <summary>
            /// 불꽃 종류에 대한 페이로드의 개수
            /// </summary>
            public int PayloadCount => Payloads == null ? 0 : Payloads.Length;

            /// <summary>
            /// 페이로드의 집합
            /// </summary>
            public PayLoad[] Payloads;

            public void Init(uint payloadCount)
            {
                Payloads = new PayLoad[payloadCount];
            }

            public void SetParameters(uint type, float minAge, float maxAge, Vertex3f minVelocity, Vertex3f maxVelocity, float damping)
            {
                Type = type;
                MinAge = minAge;
                MaxAge = maxAge;
                MinVelocity = minVelocity;
                MaxVelocity = maxVelocity;
                Damping = damping;
            }

            public void Create(Firework firework, Vertex3f start, Firework parent = null)
            {
                firework.Type = Type;
                firework.Life = Rand.Next(MinAge, MaxAge);
                Vertex3f vel = Vertex3f.Zero;
                if (parent != null)
                {
                    // The position and velocity are based on the parent.
                    firework.Position = parent.Position;
                    vel += parent.Velocity;
                }
                else
                {
                    firework.Position = start;
                }

                vel += Rand.NextVector3(MinVelocity, MaxVelocity);
                firework.Velocity = vel;

                firework.Mass = 1.0f;
                firework.Damping = Damping;
                firework.Acceleration = new Vertex3f(0, 0, -1);
                firework.ClearAccumulator();
            }
        }

        uint _type;

        public uint Type
        {
            get => _type;
            set => _type = value;
        }

        public bool Update(float duration)
        {
            Integrate(duration);
            _life -= duration;
            return (_life < 0.0f);
        }
    }
}
