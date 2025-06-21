using Geometry;
using OpenGL;
using Physics.Collision;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;
using static Physics.Ballistic;

namespace Physics
{
    public class PhysicEngine
    {
        SpatialGrid _grid;
        CollisionDetector _collisionDetector;

        const int MAX_RIGIDBODY_COUNT = 1024;
        const int MAX_RIGIDBODY_FORCE_COUNT = 1024 * 3; // 강체에 기본 중력, 힘, 댐핑이 작용하여
        int _nextRigidBody = 0;
        int _nextRigidBodyForce = 0;
        RigidBody[] _rigidBodies;
        ForceRegistration[] _forceRegistrations;
        int _forceCount = 0;
        int _rigidCount = 0;

        Dictionary<RigidBody, List<int>> _addrForceList; // 강체에 따른 강체의 힘 주소, _forceRegistrations의 강체 주소를 빠르게 가져온다.

        Ballistic _ballistic;

        public int ForceCount => _forceCount;

        public int RigidbodyCount => _rigidCount;

        public RigidBody FirstRigidBody => _rigidCount == 0 ? null : _rigidBodies[0];

        /// <summary>
        /// 생성자
        /// </summary>
        public PhysicEngine()
        {
            // 강체와 힘을 보관할 리스트를 생성한다.
            _rigidBodies = new RigidBody[MAX_RIGIDBODY_COUNT];
            _forceRegistrations = new ForceRegistration[MAX_RIGIDBODY_FORCE_COUNT];

            // 강체의 힘 주소 딕셔너리를 생성한다.
            _addrForceList = new Dictionary<RigidBody, List<int>>();

            // 탄도시스템을 생성한다.
            _ballistic = new Ballistic();
        }

        public void AddParticle(ShotType shotType, Vertex3f position, Vertex3f direction)
        {
            _ballistic.AddParticle(shotType, position, direction);
        }

        /// <summary>
        /// 강체를 추가한다.
        /// </summary>
        /// <param name="rigidBody"></param>
        public void AddRigidBody(RigidBody rigidBody)
        {
            rigidBody.IsRegisty = true;

            // 강체가 지정될 위치가 비워 있지 않으면 이전에 위치한 강체를 제거한다.
            if (_rigidBodies[_nextRigidBody] != null)
            {
                RemoveRigidBody(_nextRigidBody);
            }

            // 강체를 등록한다.
            _rigidBodies[_nextRigidBody] = rigidBody;
            _rigidCount++;
            _nextRigidBody = (_nextRigidBody + 1) % MAX_RIGIDBODY_COUNT;

            // 강체의 힘 주소 리스트를 등록한다.
            if (!_addrForceList.ContainsKey(rigidBody))
            {
                _addrForceList.Add(rigidBody, new List<int>());
            }

            // 강체 충돌쌍 검사를 위하여 그리드에 강체를 등록한다.
            if (_grid != null) _grid.Add(rigidBody);
        }

        /// <summary>
        /// 강체에 힘을 준다.
        /// </summary>
        /// <param name="rigidBody"></param>
        /// <param name="fg"></param>
        public void AddForce(RigidBody rigidBody, IForceGenerator fg)
        {
            // 강체가 등록되어 있는지 검사한다.
            if (!rigidBody.IsRegisty)
            {
                throw new Exception("강체에 힘을 주기 위해서는 강체를 먼저 등록해야 합니다.");
            }

            // 강체와 힘의 순서쌍을 만들어 등록한다.
            ForceRegistration forceRegistration = new ForceRegistration();
            forceRegistration.RigidBody = rigidBody;
            forceRegistration.ForceGenerator = fg;
            _forceRegistrations[_nextRigidBodyForce] = forceRegistration;

            // 강체에 힘을 추가하여 강체 힘 주소에 이를 저장한다.
            if (_addrForceList.ContainsKey(rigidBody))
            {
                _addrForceList[rigidBody].Add(_nextRigidBodyForce);
            }

            // 힘 주소를 다음으로 넘긴다.
            _nextRigidBodyForce = (_nextRigidBodyForce + 1) % MAX_RIGIDBODY_FORCE_COUNT;
        }

        /// <summary>
        /// 강체를 제거한다.
        /// </summary>
        /// <param name="index"></param>
        protected void RemoveRigidBody(int index)
        {
            RigidBody rigidBody = _rigidBodies[index];

            // 힘 등록기에서 입자에 해당하는 힘을 모두 제거한다.
            foreach (int forceRegistration in _addrForceList[rigidBody])
            {
                _forceRegistrations[forceRegistration] = null;
            }

            // 입자를 제거한다.
            _rigidBodies[index] = null;
        }

        protected void RemoveParticle(int index)
        {

        }

        /// <summary>
        /// 강체 목록과 힘 목록을 모두 초기화한다.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _rigidBodies.Length; i++)
            {
                _rigidBodies[i] = null;
            }

            for (int i = 0; i < _forceRegistrations.Length; i++)
            {
                _forceRegistrations[i] = null;
            }

            _rigidCount = 0;
            _forceCount = 0;
        }

        /// <summary>
        /// 물리 엔진을 초기화한다.
        /// </summary>
        public void Init(float min, float max, int cellSize)
        {
            _grid = new SpatialGrid(min, min, max, max);
            _grid.CellSize = new Vertex2f(cellSize, cellSize);
            _collisionDetector = new CollisionDetector();
        }

        /// <summary>
        /// 물리 엔진을 업데이트한다.
        /// </summary>
        /// <param name="duration"></param>
        public void Update(float duration)
        {
            // 강체에 작용하는 힘을 모두 누적기에 계산한다.
            UpdateForces(duration);

            // 탄도시스템을 업데이트한다.
            _ballistic.Update(duration);

            // 누적된 힘으로 강체를 업데이트한다.
            // 누적된 힘을 가지고 강체를 업데이트하는 순서를 지킨다.
            UpdateRigidBodies(duration);

            // 그리드를 업데이트하여 광역충돌쌍을 검출한다.
            _grid.Update(duration);

            // 미세 충돌 검사
            _collisionDetector.Update(_grid.CollisedRigidBodyPaired, duration);
        }

        /// <summary>
        /// 힘을 업데이트한다.
        /// </summary>
        /// <param name="duration"></param>
        private void UpdateForces(float duration)
        {
            _forceCount = 0;

            // 강체에 힘을 준다.
            foreach (ForceRegistration item in _forceRegistrations)
            {
                if (item == null) continue;
                RigidBody rigidBody = item.RigidBody;
                IForceGenerator fg = item.ForceGenerator;
                fg.UpdateForce(rigidBody, duration);
                _forceCount++;
            }
        }

        /// <summary>
        /// 강체들을 모두 업데이트한다.
        /// </summary>
        /// <param name="duration"></param>
        private void UpdateRigidBodies(float duration)
        {
            _rigidCount = 0;

            for (int i = 0; i < _rigidBodies.Length; i++)
            {
                RigidBody rigidBody = _rigidBodies[i];
                if (rigidBody == null) continue;

                // 강체에 누적된 힘을 적분한다.
                rigidBody.Integrate(duration);
                rigidBody.Life -= duration;
                _rigidCount++;

                // 강체의 수명이 다하면 제거한다.
                if (rigidBody.Life < 0)
                {
                    RemoveRigidBody(i);
                }
            }
        }

        public void Render(InertiaShader shader, ColorShader cshader, Matrix4x4f proj, Matrix4x4f view, bool isAxisVisible = false, bool isVisibleAABB = false)
        {
            shader.Bind();
            shader.LoadProjMatrix(proj);
            shader.LoadViewMatrix(view);

            // 강체들을 모두 순회한다.
            foreach (RigidBody rigidBody in _rigidBodies)
            {
                if (rigidBody == null) continue;

                uint vao = 0;
                int vertexCount = 0;
                Matrix4x4f model = Matrix4x4f.Identity;

                Gl.Enable(EnableCap.CullFace); // 반평면으로 인하여

                // 모델과 모델행렬을 설정한다.
                if (rigidBody is RigidCube)
                {
                    RigidCube cube = (RigidCube)rigidBody;
                    vao = Renderer3d.Cube.VAO;
                    vertexCount = Renderer3d.Cube.VertexCount;
                    model.Scale(cube.Xscale, cube.Yscale, cube.Zscale);
                }
                else if (rigidBody is RigidCylinder)
                {
                    RigidCylinder cylinder = (RigidCylinder)rigidBody;
                    vao = Renderer3d.Cylinder.VAO;
                    vertexCount = Renderer3d.Cylinder.VertexCount;
                    model.Scale(cylinder.Radius, cylinder.Radius, cylinder.Height);
                }
                else if (rigidBody is RigidSphere)
                {
                    RigidSphere sphere = (RigidSphere)rigidBody;
                    vao = Renderer3d.Sphere.VAO;
                    vertexCount = Renderer3d.Sphere.VertexCount;
                    model.Scale(sphere.Radius, sphere.Radius, sphere.Radius);
                }
                else if (rigidBody is RigidPlane)
                {
                    RigidPlane plane = (RigidPlane)rigidBody;
                    vao = Renderer3d.Rect.VAO;
                    vertexCount = Renderer3d.Rect.VertexCount;
                    model.Scale(plane.Width * 0.5f, plane.Height * 0.5f, 0.01f);

                    // 반평면과 평면을 구별하여 렌더링한다.
                    if (!plane.IsHalfPlane)
                    {
                        Gl.Disable(EnableCap.CullFace);
                    }
                }

                // 강체를 렌더링한다.
                shader.LoadInverseInertia(rigidBody.InverseInertiaTensorWorld);
                shader.LoadRotationAxis(rigidBody.Rotation);
                model = rigidBody.TransformMatrix * model;
                shader.LoadModelMatrix(model);
                Gl.BindVertexArray(vao);
                Gl.EnableVertexAttribArray(0);
                Gl.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }
            shader.Unbind();

            // 
            cshader.Bind();

            Matrix4x4f vp = proj * view;

            foreach (RigidBody rigidBody in _rigidBodies)
            {
                if (rigidBody == null) continue;
                Matrix4x4f model = rigidBody.TransformMatrix;

                // axis를 렌더링한다.
                if (isAxisVisible)
                {
                    Gl.BindVertexArray(Renderer3d.Line.VAO);
                    Gl.EnableVertexAttribArray(0);

                    model = Matrix4x4f.Identity;
                    model.Scale(1.3f, 1.3f, 1.3f);
                    model = rigidBody.TransformMatrix * model;

                    cshader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, vp * model);
                    cshader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0));
                    Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                    cshader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, vp * model * Matrix4x4f.RotatedZ(90));
                    cshader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0));
                    Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                    cshader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, vp * model * Matrix4x4f.RotatedY(-90));
                    cshader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1));
                    Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }
            }
            cshader.Unbind();
            
        }

        /// <summary>
        /// 물리 엔진의 강체들을 모두 렌더링한다.
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="proj"></param>
        /// <param name="view"></param>
        public void Render(ColorShader shader, Matrix4x4f proj, Matrix4x4f view, bool isAxisVisible = false, bool isVisibleAABB = false)
        {
            // 탄도시스템을 렌더링한다.
            _ballistic.Render(Renderer3d.Cube.VAO, Renderer3d.Cube.VertexCount, shader, proj, view);

            shader.Bind();

            Matrix4x4f vp = proj * view;

            // 강체들을 모두 순회한다.
            foreach (RigidBody rigidBody in _rigidBodies)
            {
                if (rigidBody == null) continue;

                uint vao = 0;
                int vertexCount = 0;
                Matrix4x4f model = Matrix4x4f.Identity;

                Gl.Enable(EnableCap.CullFace); // 반평면으로 인하여

                // 모델과 모델행렬을 설정한다.
                if (rigidBody is RigidCube)
                {
                    RigidCube cube = (RigidCube)rigidBody;
                    vao = Renderer3d.Cube.VAO;
                    vertexCount = Renderer3d.Cube.VertexCount;
                    model.Scale(cube.Xscale, cube.Yscale, cube.Zscale);
                }
                else if (rigidBody is RigidCylinder)
                {
                    RigidCylinder cylinder = (RigidCylinder)rigidBody;
                    vao = Renderer3d.Cylinder.VAO;
                    vertexCount = Renderer3d.Cylinder.VertexCount;
                    model.Scale(cylinder.Radius, cylinder.Radius, cylinder.Height);
                }
                else if (rigidBody is RigidSphere)
                {
                    RigidSphere sphere = (RigidSphere)rigidBody;
                    vao = Renderer3d.Sphere.VAO;
                    vertexCount = Renderer3d.Sphere.VertexCount;
                    model.Scale(sphere.Radius, sphere.Radius, sphere.Radius);
                }
                else if (rigidBody is RigidPlane)
                {
                    RigidPlane plane = (RigidPlane)rigidBody;
                    vao = Renderer3d.Rect.VAO;
                    vertexCount = Renderer3d.Rect.VertexCount;
                    model.Scale(plane.Width * 0.5f, plane.Height * 0.5f, 0.01f);

                    // 반평면과 평면을 구별하여 렌더링한다.
                    if (!plane.IsHalfPlane)
                    {
                        Gl.Disable(EnableCap.CullFace);
                    }
                }

                // 강체를 렌더링한다.
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, rigidBody.Color);
                model = rigidBody.TransformMatrix * model;
                shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, vp * model);
                Gl.BindVertexArray(vao);
                Gl.EnableVertexAttribArray(0);
                Gl.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);

                // 강체의 AABB를 계산한다.
                if (rigidBody.AABB != null && isVisibleAABB)
                {
                    AABB aabb = rigidBody.AABB;
                    model = Matrix4x4f.Identity;
                    model[0, 0] = aabb.HalfSize.x * 1.1f;
                    model[1, 1] = aabb.HalfSize.y * 1.1f;
                    model[2, 2] = aabb.HalfSize.z * 1.1f;
                    model[3, 0] = rigidBody.Position.x;
                    model[3, 1] = rigidBody.Position.y;
                    model[3, 2] = rigidBody.Position.z;
                    shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(1, 1, 0, 0.3f));
                    shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, vp * model);
                    Gl.BindVertexArray(Renderer3d.Cube.VAO);
                    Gl.EnableVertexAttribArray(0);
                    Gl.DrawArrays(PrimitiveType.Triangles, 0, Renderer3d.Cube.VertexCount);
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }
                
                // axis를 렌더링한다.
                if (isAxisVisible)
                {
                    Gl.BindVertexArray(Renderer3d.Line.VAO);
                    Gl.EnableVertexAttribArray(0);

                    model = Matrix4x4f.Identity;
                    model.Scale(1.3f, 1.3f, 1.3f);
                    model = rigidBody.TransformMatrix * model;

                    shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, vp * model);
                    shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(1, 0, 0));
                    Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                    shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, vp * model * Matrix4x4f.RotatedZ(90));
                    shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 1, 0));
                    Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                    shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, vp * model * Matrix4x4f.RotatedY(-90));
                    shader.LoadUniform(ColorShader.UNIFORM_NAME.color, new Vertex4f(0, 0, 1));
                    Gl.DrawArrays(PrimitiveType.Lines, 0, Renderer3d.Line.VertexCount);

                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }

            }
            shader.Unbind();
        }

        /// <summary>
        /// 그리드맵의 디버깅을 위한 출력이다.
        /// </summary>
        public void DebugGridMap()
        {
            _grid.ToMap();
        }
    }
}
