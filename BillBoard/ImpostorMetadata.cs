using System;
using Newtonsoft.Json;

namespace BillBoard
{
    /// <summary>
    /// 임포스터 메타데이터 (JSON 저장용)
    /// </summary>
    public class ImpostorMetadata
    {
        [JsonProperty("modelName")]
        public string ModelName { get; set; }

        [JsonProperty("generatedAt")]
        public DateTime GeneratedAt { get; set; }

        // ========== AABB 정보 ==========
        [JsonProperty("aabbCenter")]
        public Vector3f AABBCenter { get; set; }

        [JsonProperty("aabbSize")]
        public Vector3f AABBSize { get; set; }

        [JsonProperty("boundingSphereRadius")]
        public float BoundingSphereRadius { get; set; }

        // ========== 아틀라스 정보 ==========
        [JsonProperty("atlasSize")]
        public int AtlasSize { get; set; }

        [JsonProperty("individualSize")]
        public int IndividualSize { get; set; }

        [JsonProperty("horizontalAngles")]
        public int HorizontalAngles { get; set; }

        [JsonProperty("verticalAngles")]
        public int VerticalAngles { get; set; }

        [JsonProperty("verticalAngleMin")]
        public float VerticalAngleMin { get; set; }

        [JsonProperty("verticalAngleMax")]
        public float VerticalAngleMax { get; set; }

        // ========== 렌더링 가이드 ==========
        [JsonProperty("atlasUVScale")]
        public float AtlasUVScale { get; set; }

        [JsonProperty("totalFrames")]
        public int TotalFrames { get; set; }

        public override string ToString()
        {
            return $"ImpostorMetadata[{ModelName}, {TotalFrames} frames, Radius={BoundingSphereRadius:F2}]";
        }
    }

    /// <summary>
    /// Vector3f JSON 직렬화용 (class로 변경)
    /// </summary>
    public class Vector3f
    {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("z")]
        public float Z { get; set; }

        public Vector3f()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public Vector3f(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return $"({X:F3}, {Y:F3}, {Z:F3})";
        }
    }
}