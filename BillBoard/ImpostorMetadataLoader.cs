using System;
using System.IO;
using Newtonsoft.Json;

namespace BillBoard
{
    /// <summary>
    /// 임포스터 메타데이터 로더
    /// </summary>
    public static class ImpostorMetadataLoader
    {
        /// <summary>
        /// JSON 파일에서 메타데이터 로드
        /// </summary>
        public static ImpostorMetadata LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"메타데이터 파일을 찾을 수 없습니다: {filePath}");
                }

                string jsonText = File.ReadAllText(filePath);
                ImpostorMetadata metadata = JsonConvert.DeserializeObject<ImpostorMetadata>(jsonText);

                if (metadata == null)
                {
                    throw new InvalidOperationException("메타데이터 역직렬화 실패");
                }

                Console.WriteLine($"메타데이터 로드 완료: {metadata}");
                return metadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"메타데이터 로드 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// JSON 문자열에서 메타데이터 로드
        /// </summary>
        public static ImpostorMetadata LoadFromJson(string jsonText)
        {
            try
            {
                ImpostorMetadata metadata = JsonConvert.DeserializeObject<ImpostorMetadata>(jsonText);

                if (metadata == null)
                {
                    throw new InvalidOperationException("메타데이터 역직렬화 실패");
                }

                return metadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON 파싱 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 메타데이터를 JSON 파일로 저장
        /// </summary>
        public static void SaveToFile(ImpostorMetadata metadata, string filePath)
        {
            try
            {
                string jsonText = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                File.WriteAllText(filePath, jsonText);
                Console.WriteLine($"메타데이터 저장 완료: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"메타데이터 저장 실패: {ex.Message}");
                throw;
            }
        }
    }
}