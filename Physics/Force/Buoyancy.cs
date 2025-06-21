using OpenGL;

namespace Physics
{
    /// <summary>
    /// 부력에 작용하는 힘 발생기
    /// </summary>
    public class Buoyancy : BaseForce, IForceGenerator
    {
        /// <summary>
        /// 최대 부력을 발생하기 전 개체의 최대 침수 깊이
        /// </summary>
        float _maxDepth = 1.0f;

        /// <summary>
        /// 물체의 부피
        /// </summary>
        float _volume = 1.0f;

        /// <summary>
        /// 수면이 z=0 평면으로부터 이동한 높이
        /// </summary>
        float _waterHeight = 0.0f;

        /// <summary>
        /// 액체의 밀도, 순수한 물의 밀도는 1000kg/m^3
        /// </summary>
        float _liquidDensity = 1000.0f;

        /// <summary>
        /// The linear Drag of Water
        /// </summary>
        float _linearDrag = 5.0f;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="maxDepth"></param>
        /// <param name="volume"></param>
        /// <param name="waterHeight"></param>
        /// <param name="liquidDensity">물의 밀도 1000kg/m^3, 바닷물의 밀도 1020~1030kg/m^3, 사해의 밀도 1250kg/m^3</param>
        public Buoyancy(float maxDepth, float volume, float waterHeight, float liquidDensity = 1000.0f)
        {
            _maxDepth = maxDepth;
            _volume = volume;
            _waterHeight = waterHeight;
            _liquidDensity = liquidDensity;
        }   

        public override void UpdateForce(RigidBody rigidBody, float duration)
        {
            if (!rigidBody.HasFiniteMass) return;

            // 물속에 잠긴 깊이를 계산한다.
            float depth = rigidBody.Position.z;

            // 물 속의 밖이면 부력이 발생하지 않는다. 
            if (depth >= _waterHeight + _maxDepth) return;

            Vertex3f force = Vertex3f.Zero;

            // 최대 깊이인지 확인한다.
            if (depth <= _waterHeight - _maxDepth)
            {
                force.z = _liquidDensity * _volume * 10.0f; // gravity 10
                rigidBody.AddForce(force);
                return;
            }

            // 아니면, 부분적으로 잠겨 있음
            force.z = _liquidDensity * _volume * ((_maxDepth + _waterHeight - depth) / (2 * _maxDepth)) * 10.0f; // gravity 10

            // http://www.iforce2d.net/b2dtut/buoyancy 
            // Refer to the tutorial of buoyancy. the drag is approximation.
            // water velocity is assumed by (0, 0, 0)
            // https://box2d.org/downloads/
            // Refer to the buoyancy demo of Eric Catto
            // the water linear drag is normally 5.0, according to the demo
            float dragForce = (rigidBody.Mass * _linearDrag) * (-rigidBody.Velocity.z);
            force.z += dragForce;

            rigidBody.AddForce(force);
        }
    }
}
