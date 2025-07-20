using Model3d;
using OpenGL;

namespace Animate.AniModels
{
    /// <summary>
    /// 아이템 부착 정보를 담는 구조체
    /// </summary>
    public readonly struct ItemAttachment
    {
        public readonly TexturedModel Model; // 텍스쳐 모델
        public readonly string Name; // 아이템 이름 (예: "axe", "sword" 등)
        public readonly int BoneIndex; // 부착할 뼈대 인덱스
        public readonly Matrix4x4f LocalTransform; // 아이템 로컬 변환 행렬

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="model">텍스쳐 모델</param>
        /// <param name="name">아이템 이름 (예: "axe", "helmet" 등)</param>
        /// <param name="boneIndex">부착할 뼈대 인덱스</param>
        /// <param name="localTransform">아이템 로컬 변환 행렬</param>
        public ItemAttachment(TexturedModel model, string name, int boneIndex, Matrix4x4f localTransform)
        {
            Model = model;
            Name = name;
            BoneIndex = boneIndex;
            LocalTransform = localTransform;
        }
    }
}
