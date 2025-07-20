using Animate.AniModels;
using Model3d;
using OpenGL;

namespace Animate
{
	public abstract partial class AniActor
	{
        /// <summary>
        /// 아이템을 장착한다.
        /// </summary>
        /// <param name="itemUniqueName">아이템의 고유 식별자</param>
        /// <param name="itemName">아이템 타입 이름</param>
        /// <param name="model">아이템의 3D 모델</param>
        /// <param name="boneIndex">아이템을 부착할 뼈대 인덱스</param>
        /// <param name="size">아이템 크기 (균등 스케일링)</param>
        /// <param name="positionX">X축 위치 오프셋</param>
        /// <param name="positionY">Y축 위치 오프셋</param>
        /// <param name="positionZ">Z축 위치 오프셋</param>
        /// <param name="pitch">X축 회전 (피치, 도 단위)</param>
        /// <param name="yaw">Y축 회전 (요, 도 단위)</param>
        /// <param name="roll">Z축 회전 (롤, 도 단위)</param>
        public void EquipItem(string itemUniqueName, string itemName, TexturedModel model,
            int boneIndex, float size = 1.0f,
            float positionX = 0.0f, float positionY = 0.0f, float positionZ = 0.0f,
            float pitch = 0.0f, float yaw = 0.0f, float roll = 0.0f)
        {
            // 변환 행렬 조합: Scale -> Rotate -> Translate 순서
            Matrix4x4f scaleMatrix = Matrix4x4f.Scaled(size, size, size);
            Matrix4x4f rotationMatrix = Matrix4x4f.RotatedX(pitch) *
                                       Matrix4x4f.RotatedY(yaw) *
                                       Matrix4x4f.RotatedZ(roll);
            Matrix4x4f translationMatrix = Matrix4x4f.Translated(positionX, positionY, positionZ);

            // 최종 변환: T * R * S (오른쪽부터 먼저 적용)
            Matrix4x4f attachmentTransform = translationMatrix * rotationMatrix * scaleMatrix;

            var itemAttachment = new ItemAttachment(model, itemName, boneIndex, attachmentTransform);
            _items[itemUniqueName] = itemAttachment;
        }

        /// <summary>
        /// 벡터를 사용하는 오버로드 버전
        /// </summary>
        /// <param name="itemUniqueName">아이템의 고유 식별자</param>
        /// <param name="itemName">아이템 타입 이름</param>
        /// <param name="model">아이템의 3D 모델</param>
        /// <param name="boneIndex">아이템을 부착할 뼈대 인덱스</param>
        /// <param name="size">아이템 크기</param>
        /// <param name="position">위치 벡터 (x, y, z)</param>
        /// <param name="rotation">회전 벡터 (pitch, yaw, roll, 도 단위)</param>
        public void EquipItem(string itemUniqueName, string itemName, TexturedModel model,
            int boneIndex, float size, Vertex3f position, Vertex3f rotation)
        {
            EquipItem(itemUniqueName, itemName, model, boneIndex, size,
                     position.x, position.y, position.z,
                     rotation.x, rotation.y, rotation.z);
        }

        /// <summary>
        /// 완전한 변환 행렬을 직접 전달하는 버전 (고급 사용자용)
        /// </summary>
        /// <param name="itemUniqueName">아이템의 고유 식별자</param>
        /// <param name="itemName">아이템 타입 이름</param>
        /// <param name="model">아이템의 3D 모델</param>
        /// <param name="boneIndex">아이템을 부착할 뼈대 인덱스</param>
        /// <param name="attachmentTransform">완전한 부착 변환 행렬</param>
        public void EquipItem(string itemUniqueName, string itemName, TexturedModel model,
            int boneIndex, Matrix4x4f attachmentTransform)
        {
            var itemAttachment = new ItemAttachment(model, itemName, boneIndex, attachmentTransform);
            _items[itemUniqueName] = itemAttachment;
        }

        /// <summary>
        /// 아이템을 장착해제한다.
        /// </summary>
        /// <param name="name">아이템 이름</param>
        /// <returns>장착해제 성공 여부</returns>
        protected bool UnequipItem(string name)
        {
            return _items.Remove(name);
        }

    }
}