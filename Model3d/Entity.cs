using Common.Abstractions;
using Common.Mathematics;
using Geometry;
using OpenGL;
using System.Collections.Generic;
using ZetaExt;

namespace Model3d
{
    /// <summary>
    /// 3D 엔터티를 표현하는 클래스입니다.
    /// 모델 데이터, 월드 변환, 충돌 감지 기능을 결합하여 제공합니다.
    /// </summary>
    public class Entity : BaseEntity, ITransformable, IBoundBoxable
    {
        readonly string _modelName;
        readonly List<BaseModel3d> _models;
        readonly bool _textured = false;

        protected Material _material;
        protected bool _isAxisVisible = false;
        protected PolygonMode _polygonMode = PolygonMode.Fill;
        protected bool _isOneside;

        // 컴포넌트
        protected TransformComponent _transform;
        protected BoundBoxComponent _boundingBox;

        public Entity(string entityName, string modelName, BaseModel3d rawModel3D) : this(entityName, modelName, new BaseModel3d[] { rawModel3D }) { }

        public Entity(string entityName, string modelName, BaseModel3d[] rawModel3D) : base(entityName)
        {
            // Transform 컴포넌트 초기화
            _transform = new TransformComponent();

            // 모델
            _modelName = modelName;
            _models = new List<BaseModel3d>();
            _models.AddRange(rawModel3D);

            // 재질
            _textured = (_models[0] is TexturedModel);
            _material = Material.White;

            // BoundBox 컴포넌트 초기화
            if (_models.Count > 0)
            {
                // 첫 번째 모델의 AABB와 OBB를 생성하여 BoundBoxComponent 초기화
                RawModel3d firstRawModel = _models[0] as RawModel3d;
                firstRawModel.GenerateBoundingBox();

                // AABB와 OBB를 합쳐서 BoundBoxComponent 생성
                AABB unionAABB =  (_models[0] as RawModel3d).AABB;
                OBB unionOBB = (_models[0] as RawModel3d).OBB;

                // AABB와 OBB가 여러 모델에 걸쳐 있다면 합칩니다.
                if (_models.Count > 1)
                {
                    for (int i = 0; i < _models.Count; i++)
                    {
                        RawModel3d rawModel = (RawModel3d)_models[i];
                        unionAABB = (AABB)rawModel.AABB.Union(unionAABB);
                        unionOBB = (OBB)rawModel.OBB.Union(unionOBB);
                    }
                    _boundingBox = new BoundBoxComponent(this, unionAABB, unionOBB);
                }
            }
            else
            {
                throw new System.Exception("Entity를 설정하려면 원형모델이 있어야 합니다.");
            }
        }

        public bool IsDrawOneSide
        {
            get => _isOneside;
            set => _isOneside = value;
        }

        public string ModelName
        {
            get => _modelName;
            //set => _modelName = value;
        }

        public bool IsAxisVisible
        {
            get => _isAxisVisible;
            set => _isAxisVisible = value;
        }

        public PolygonMode PolygonMode
        {
            get => _polygonMode;
            set => _polygonMode = value;
        }

        public virtual List<BaseModel3d> Models
        {
            get => _models;
        }

        public BaseModel3d[] Model
        {
            get => _models.ToArray();
        }

        public Material Material
        {
            get => _material;
            set => _material = value;
        }

        public bool IsTextured
        {
            get => _textured;
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // 위임된 속성과 함수
        //-------------------------------------------------------------------------------------------------------------------------
        public Matrix4x4f LocalBindMatrix => ((ITransformable)_transform).LocalBindMatrix;

        public Vertex3f Size { get => ((ITransformable)_transform).Size; set => ((ITransformable)_transform).Size = value; }

        public Vertex3f Position { get => ((ITransformable)_transform).Position; set => ((ITransformable)_transform).Position = value; }

        public Matrix4x4f ModelMatrix => ((ITransformable)_transform).ModelMatrix;

        public Pose Pose
        {
            get => _transform.Pose;
        }

        public AABB AABB
        {
            get => _boundingBox.AABB;
            set => _boundingBox.AABB = value;
        }

        public OBB OBB
        {
            get => _boundingBox.OBB;
            set => _boundingBox.OBB = value;
        }
        
        public bool IsVisibleOBB
        {
            get => _boundingBox.OBB.IsVisible;
            set => _boundingBox.OBB.IsVisible = value;
        }

        public bool IsMoved
        {
            get => _transform.IsMoved;
            set => _transform.IsMoved = value;
        }

        public bool IsVisibleAABB
        {
            get => _boundingBox.AABB.IsVisible;
            set => _boundingBox.AABB.IsVisible = value;
        }

        public AABB ModelAABB 
        {
            get => _boundingBox.ModelAABB;
        }

        public OBB ModelOBB
        {
            get => _boundingBox.ModelOBB;
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public void LocalBindTransform(float sx = 1, float sy = 1, float sz = 1, float rotx = 0, float roty = 0, float rotz = 0, float x = 0, float y = 0, float z = 0)
        {
            ((ITransformable)_transform).LocalBindTransform(sx, sy, sz, rotx, roty, rotz, x, y, z);
        }

        public void Pitch(float deltaDegree)
        {
            ((ITransformable)_transform).Pitch(deltaDegree);
            _transform.IsMoved = true;
        }

        public void Roll(float deltaDegree)
        {
            ((ITransformable)_transform).Roll(deltaDegree);
            _transform.IsMoved = true;
        }

        public void Scale(float scaleX, float scaleY, float scaleZ)
        {
            ((ITransformable)_transform).Scale(scaleX, scaleY, scaleZ);
            _transform.IsMoved = true;
        }

        public void SetRollPitchAngle(float pitch, float yaw, float roll)
        {
            ((ITransformable)_transform).SetRollPitchAngle(pitch, yaw, roll);
            _transform.IsMoved = true;
        }

        public void Translate(float dx, float dy, float dz)
        {
            ((ITransformable)_transform).Translate(dx, dy, dz);
            _transform.IsMoved = true;
        }

        public void Yaw(float deltaDegree)
        {
            ((ITransformable)_transform).Yaw(deltaDegree);
            _transform.IsMoved = true;
        }

        /// <summary>
        /// 엔터티의 상태를 업데이트합니다.
        /// </summary>
        public override void Update(Camera camera)
        {
            UpdateBoundingBox();
        }

        /// <summary>
        /// 엔터티의 바운딩 박스를 업데이트합니다.
        /// </summary>
        /// <remarks>이전 프레임에서 엔터티가 이동한 경우에만 수행됩니다.</remarks>
        public void UpdateBoundingBox()
        {
            // 초기 AABB가 없는 경우 모델의 AABB를 복제하여 생성
            if (_boundingBox.AABB == null)
                _boundingBox.AABB = (AABB)_boundingBox.ModelAABB.Clone();

            // 초기 OBB가 없는 경우 모델의 OBB를 복제하여 생성 
            if (_boundingBox.OBB == null)
                _boundingBox.OBB = (OBB)_boundingBox.ModelOBB.Clone();

            // 이전 프레임에서 물체가 이동한 경우에만 바운딩 박스 업데이트를 수행
            if (_transform.IsMoved)
            {
                // 월드 변환 행렬 계산 (모델 행렬 * 로컬 바인딩 행렬)
                Matrix4x4f model = _transform.ModelMatrix * _transform.LocalBindMatrix;

                // AABB(Axis-Aligned Bounding Box) 업데이트
                // 계산의 효율성을 위해 8개의 꼭지점을 직접 변환하는 대신 하드코딩된 방식 사용
                Vertex3f low = _boundingBox.ModelAABB.LowerBound;      // AABB의 최소점
                Vertex3f size = _boundingBox.ModelAABB.Size;           // AABB의 크기
                Vertex3f s = model.Column0.Vertex3f() * size.x;        // X축 방향 벡터
                Vertex3f t = model.Column1.Vertex3f() * size.y;        // Y축 방향 벡터
                Vertex3f u = model.Column2.Vertex3f() * size.z;        // Z축 방향 벡터
                Vertex3f l = model.Transform(low);                      // 변환된 최소점

                // AABB의 8개 꼭지점 계산
                Vertex3f[] vertices = new Vertex3f[8];
                vertices[0] = l;                  // 최소점
                vertices[1] = l + s;              // 최소점 + X축
                vertices[2] = l + s + t;          // 최소점 + X축 + Y축
                vertices[3] = l + t;              // 최소점 + Y축
                vertices[4] = l + u;              // 최소점 + Z축
                vertices[5] = l + u + s;          // 최소점 + Z축 + X축
                vertices[6] = l + u + s + t;      // 최소점 + Z축 + X축 + Y축
                vertices[7] = l + u + t;          // 최소점 + Z축 + Y축

                // 새로운 AABB의 최소/최대 경계점 계산
                _boundingBox.AABB.LowerBound = Vertex3f.Min(vertices);
                _boundingBox.AABB.UpperBound = Vertex3f.Max(vertices);

                // OBB(Oriented Bounding Box) 업데이트
                Vertex3f center = model.Transform(_boundingBox.ModelOBB.Center);    // OBB 중심점 변환
                Matrix3x3f rot = model.Rot3x3f();                                   // 회전 행렬 추출

                // OBB의 3개 주축 방향 계산
                Vertex3f[] newAxis = new Vertex3f[3];
                newAxis[0] = (rot * _boundingBox.ModelOBB.Axis[0]).Normalized;     // X축 방향
                newAxis[1] = (rot * _boundingBox.ModelOBB.Axis[1]).Normalized;     // Y축 방향
                newAxis[2] = (rot * _boundingBox.ModelOBB.Axis[2]).Normalized;     // Z축 방향

                // 새로운 OBB 정보 설정
                _boundingBox.OBB.Axis = newAxis;
                _boundingBox.OBB.Center = center;

                // 업데이트 완료 후 이동 플래그 초기화
                _transform.IsMoved = false;
            }
        }

    }
}