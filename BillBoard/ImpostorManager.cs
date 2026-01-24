using Common;
using Common.Abstractions;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace BillBoard
{
    public class ImpostorManager
    {
        private readonly string _defaultDir;
        private ImpostorBaker _impostorBaker;
        private ImpostorBakingShader _shader;
        private bool _finalized = false;
        private uint _baseInfoSSBO;         // 임포스터 타입별 기본 정보

        // ✅ 수정: Dictionary 대신 고정 크기 배열 사용
        private const int MAX_IMPOSTOR_TYPES = Constants.MAX_BATCHES;
        private ImpostorBaseInfo[] _impostorArray;  // Dictionary 대신
        private bool[] _slotUsed;  // 슬롯 사용 여부
        private int _count;

        private const int BASE_INFO_BINDING = 3;  // SSBO 바인딩 포인트

        // 베이킹 관련
        private ImpostorBakeResult _result;
        private ImpostorSettings _settings;

        // ---------------------------------------------------
        // 속성
        // ---------------------------------------------------

        public int ImpostorTypeCount => _count;
        public bool IsFinalized => _finalized;
        public uint BaseInfoSSBO => _baseInfoSSBO;


        // ---------------------------------------------------
        // 생성자
        // ---------------------------------------------------
        public ImpostorManager(string defaultDir)
        {
            _defaultDir = defaultDir;
            _impostorBaker = new ImpostorBaker();
            _shader = new ImpostorBakingShader(StrRes.PROJECT_PATH);

            // ✅ 배열 초기화
            _impostorArray = new ImpostorBaseInfo[MAX_IMPOSTOR_TYPES];
            _slotUsed = new bool[MAX_IMPOSTOR_TYPES];
            _count = 0;

            // SSBO 생성
            _baseInfoSSBO = Gl.GenBuffer();
        }

        public void AddImpostors(UnifiedTexturedModel model, uint modelId)
        {
            if (modelId >= MAX_IMPOSTOR_TYPES)
            {
                throw new System.ArgumentException($"ModelID {modelId} exceeds MAX_IMPOSTOR_TYPES {MAX_IMPOSTOR_TYPES}");
            }

            string baseName = Path.GetFileNameWithoutExtension(model.Name);
            string metadataPath = Path.Combine(_defaultDir, baseName + ".json");

            // 텍스처 저장
            string albedoPath = Path.Combine(_defaultDir, baseName + "_albedo.png");
            string normalPath = Path.Combine(_defaultDir, baseName + "_normal.png");
            string depthPath = Path.Combine(_defaultDir, baseName + "_depth.png");

            // 이미 베이킹된 파일이 존재하면 로드, 없으면 새로 베이킹
            if (File.Exists(albedoPath) && File.Exists(normalPath)
                && File.Exists(depthPath) && File.Exists(metadataPath))
            {
                if (_result == null) _result = new ImpostorBakeResult();
                _result.AlbedoTextureID = new Texture(albedoPath, flipY: true).TextureID;
                _result.NormalTextureID = new Texture(normalPath, flipY: true).TextureID;
                _result.DepthTextureID = new Texture(depthPath, flipY: true).TextureID;
                _result.Metadata = ImpostorMetadataLoader.LoadFromFile(metadataPath);
                Console.WriteLine($"- {baseName} AlbedoTextureID/NormalTextureID/DepthTextureID 로딩 완료! ");
            }
            else
            {
                _settings = ImpostorSettings.CreateHighQuality(model.Name);
                _result = _impostorBaker.BakeAtlas(model, _settings, _shader, metadataPath);
            }

            ImpostorBaseInfo baseInfo = ImpostorBaseInfo.FromBakeResult(_result);

            // ✅ modelId를 배열 인덱스로 직접 사용
            _impostorArray[modelId] = baseInfo;

            if (!_slotUsed[modelId])
            {
                _slotUsed[modelId] = true;
                _count++;
            }
        }

        /// <summary>
        /// GPU에 업로드된 BaseInfo SSBO 데이터를 다시 읽어와서 검증 및 출력
        /// </summary>
        public void VerifyBaseInfoSSBO()
        {
            if (!_finalized || _baseInfoSSBO == 0)
            {
                System.Console.WriteLine("[ImpostorManager] SSBO not finalized yet!");
                return;
            }

            int count = _count;
            int size = Marshal.SizeOf<ImpostorBaseInfo>() * count;

            // GPU에서 데이터 읽어오기
            ImpostorBaseInfo[] gpuData = new ImpostorBaseInfo[count];

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _baseInfoSSBO);
            System.IntPtr ptr = Gl.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadOnly);

            if (ptr != System.IntPtr.Zero)
            {
                // 메모리 복사
                for (int i = 0; i < count; i++)
                {
                    System.IntPtr offset = System.IntPtr.Add(ptr, i * Marshal.SizeOf<ImpostorBaseInfo>());
                    gpuData[i] = Marshal.PtrToStructure<ImpostorBaseInfo>(offset);
                }

                Gl.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

                // 검증 및 출력
                System.Console.WriteLine("========================================");
                System.Console.WriteLine($"[ImpostorManager] SSBO Verification");
                System.Console.WriteLine($"Total Impostor Types: {count}");
                System.Console.WriteLine("========================================");

                for (int i = 0; i < count; i++)
                {
                    System.Console.WriteLine($"\n--- Impostor #{i} ---");
                    System.Console.WriteLine($"AABB Center: ({gpuData[i].AABBCenterX:F3}, {gpuData[i].AABBCenterY:F3}, {gpuData[i].AABBCenterZ:F3})");
                    System.Console.WriteLine($"AABB Size: ({gpuData[i].AABBSizeX:F3}, {gpuData[i].AABBSizeY:F3}, {gpuData[i].AABBSizeZ:F3})");
                    System.Console.WriteLine($"Bounding Sphere Radius: {gpuData[i].BoundingSphereRadius:F3}");
                    System.Console.WriteLine($"Atlas Size: {gpuData[i].AtlasSize}");
                    System.Console.WriteLine($"Individual Size: {gpuData[i].IndividualSize}");
                    System.Console.WriteLine($"Horizontal Angles: {gpuData[i].HorizontalAngles}");
                    System.Console.WriteLine($"Vertical Angles: {gpuData[i].VerticalAngles}");
                    System.Console.WriteLine($"Vertical Angle Range: [{gpuData[i].VerticalAngleMin:F3}, {gpuData[i].VerticalAngleMax:F3}]");
                    System.Console.WriteLine($"Atlas UV Scale: {gpuData[i].AtlasUVScale:F3}");
                    System.Console.WriteLine($"Total Frames: {gpuData[i].TotalFrames}");
                    System.Console.WriteLine($"Albedo Texture ID: {gpuData[i].AlbedoTextureID}");
                    System.Console.WriteLine($"Normal Texture ID: {gpuData[i].NormalTextureID}");
                    System.Console.WriteLine($"Depth Texture ID: {gpuData[i].DepthTextureID}");
                }

                System.Console.WriteLine("\n========================================");
            }
            else
            {
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                System.Console.WriteLine("[ImpostorManager] Failed to map SSBO buffer!");
            }
        }

        /// <summary>
        /// 모든 임포스터 BaseInfo를 GPU에 업로드
        /// </summary>
        public void Finalized()
        {
            if (_finalized)
            {
                UpdateBaseInfoSSBO();
                return;
            }

            if (_count == 0)
            {
                throw new System.Exception("No impostor data to finalize!");
            }

            // ✅ 사용된 슬롯 개수 확인
            uint maxUsedIndex = 0;
            for (uint i = 0; i < MAX_IMPOSTOR_TYPES; i++)
            {
                if (_slotUsed[i]) maxUsedIndex = i;
            }

            // ✅ 0 ~ maxUsedIndex+1 범위만 업로드 (메모리 절약)
            int uploadCount = (int)maxUsedIndex + 1;
            uint size = (uint)(Marshal.SizeOf<ImpostorBaseInfo>() * uploadCount);

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _baseInfoSSBO);

            unsafe
            {
                fixed (ImpostorBaseInfo* ptr = _impostorArray)
                {
                    Gl.BufferData(BufferTarget.ShaderStorageBuffer, size, (IntPtr)ptr, BufferUsage.StaticDraw);
                }
            }

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, BASE_INFO_BINDING, _baseInfoSSBO);
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            _finalized = true;

            Console.WriteLine($"\n[ImpostorManager] Finalized:");
            Console.WriteLine($"  Total Types: {_count}");
            Console.WriteLine($"  SSBO Size: {uploadCount} slots ({size / 1024.0:F1} KB)");
            Console.WriteLine($"  Struct Size: {Marshal.SizeOf<ImpostorBaseInfo>()} bytes");  // ✅ 80 확인

            // ✅ CPU 데이터 출력
            for (uint i = 0; i <= maxUsedIndex; i++)
            {
                if (_slotUsed[i])
                {
                    var info = _impostorArray[i];
                    Console.WriteLine($"\n  SSBO[{i}] ✓");
                    Console.WriteLine($"    AABB Center: ({info.AABBCenterX:F2}, {info.AABBCenterY:F2}, {info.AABBCenterZ:F2})");
                    Console.WriteLine($"    AABB Size: ({info.AABBSizeX:F2}, {info.AABBSizeY:F2}, {info.AABBSizeZ:F2})");
                    Console.WriteLine($"    Radius: {info.BoundingSphereRadius:F2}");
                    Console.WriteLine($"    Atlas: {info.AtlasSize}x{info.AtlasSize}, Individual: {info.IndividualSize}");
                    Console.WriteLine($"    Angles: H={info.HorizontalAngles}, V={info.VerticalAngles}");
                    Console.WriteLine($"    Textures: Albedo={info.AlbedoTextureID}, Normal={info.NormalTextureID}, Depth={info.DepthTextureID}");
                }
            }

            // ✅ GPU 데이터 검증 (즉시 확인)
            VerifyBaseInfoSSBO();

        }
        /// <summary>
        /// BaseInfo SSBO 업데이트 (런타임 중 추가 시)
        /// </summary>
        private void UpdateBaseInfoSSBO()
        {
            if (_baseInfoSSBO == 0 || _count == 0)
                return;

            uint maxUsedIndex = 0;
            for (uint i = 0; i < MAX_IMPOSTOR_TYPES; i++)
            {
                if (_slotUsed[i]) maxUsedIndex = i;
            }

            int uploadCount = (int)maxUsedIndex + 1;
            uint size = (uint)(Marshal.SizeOf<ImpostorBaseInfo>() * uploadCount);

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _baseInfoSSBO);

            unsafe
            {
                fixed (ImpostorBaseInfo* ptr = _impostorArray)
                {
                    Gl.BufferData(BufferTarget.ShaderStorageBuffer, size, (IntPtr)ptr, BufferUsage.StaticDraw);
                }
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            Console.WriteLine($"[ImpostorManager] Updated SSBO with {uploadCount} slots");
        }

        /// <summary>
        /// 렌더링 전 BaseInfo SSBO 바인딩
        /// </summary>
        public void BindBaseInfoSSBO()
        {
            if (!_finalized)
            {
                throw new System.Exception("ImpostorManager not finalized! Call Finalized() first.");
            }

            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, BASE_INFO_BINDING, _baseInfoSSBO);
        }

        /// <summary>
        /// modelId로 BaseInfo 조회
        /// </summary>
        public ImpostorBaseInfo GetBaseInfo(uint modelId)
        {
            if (modelId >= MAX_IMPOSTOR_TYPES || !_slotUsed[modelId])
            {
                throw new System.ArgumentException($"Invalid ModelID {modelId}");
            }

            return _impostorArray[modelId];
        }

        public void Dispose()
        {
            if (_baseInfoSSBO != 0)
            {
                Gl.DeleteBuffers(_baseInfoSSBO);
                _baseInfoSSBO = 0;
            }

            _impostorBaker?.Dispose();
            _finalized = false;
        }
    }
}