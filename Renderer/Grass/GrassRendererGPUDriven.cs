using Common;
using Common.Abstractions;
using Geometry;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terrain;
using ZetaExt;

namespace Renderer
{
    /// <summary>
    /// GPU-Driven 풀 렌더링 시스템
    /// </summary>
    public class GrassRendererGPUDriven
    {
        // 타일 설정
        private const int TILE_RADIUS = 10;
        private const float TILE_SIZE = 10.0f;
        private const float JITTER_RATIO = 0.4f;
        private const int MAX_CANDIDATE_TILES = (TILE_RADIUS * 2 + 1) * (TILE_RADIUS * 2 + 1);

        // LOD 설정
        private const int LOD_COUNT = 3;           // LOD 0, 1, 2 (LOD 3은 컬링)

        // LOD별 밀도
        private const int LOD0_WIDTH_HEIGHT = 99;  // 99×99 = 9,801개
        private const int LOD1_WIDTH_HEIGHT = 49;  // 49×49 = 2,401개
        private const int LOD2_WIDTH_HEIGHT = 24;  // 24×24 = 576개

        // LOD별 거리 (미터)
        private const float LOD0_DISTANCE = 40.0f;   // 0~40m
        private const float LOD1_DISTANCE = 80.0f;   // 40~80m
        private const float LOD2_DISTANCE = 320.0f;  // 80~120m
                                                     // 120m 이상은 LOD 3 (컬링)

        // LOD별 풀 개수
        private static readonly int[] GRASS_PER_TILE_LOD = new int[]
        {
        LOD0_WIDTH_HEIGHT * LOD0_WIDTH_HEIGHT,  // 9,801
        LOD1_WIDTH_HEIGHT * LOD1_WIDTH_HEIGHT,  // 2,401
        LOD2_WIDTH_HEIGHT * LOD2_WIDTH_HEIGHT   // 576
        };

        // ============================================================
        // GPU 버퍼
        // ============================================================
        private uint[] _templateSSBOs = new uint[LOD_COUNT];           // LOD별 Template (3개)
        private uint _candidateTilesSSBO;                              // 후보 타일 (121개)

        // LOD별 분리! (기존 1개 → 3개)
        private uint[] _visibleTilesSSBOs = new uint[LOD_COUNT];       // LOD별 가시 타일 (3개)
        private uint[] _indirectCommandBuffers = new uint[LOD_COUNT];  // LOD별 Indirect Command (3개)
        private uint[] _atomicCounterBuffers = new uint[LOD_COUNT];    // LOD별 Atomic Counter (3개)

        private uint _dummyVAO;

        // ============================================================
        // 데이터
        // ============================================================
        private List<GrassLocalTemplate>[] _localTemplates = new List<GrassLocalTemplate>[LOD_COUNT];  
                                                    // LOD별 Template (3개)
        private List<CandidateTileData> _candidateTiles;  // CPU에서 관리

        // 셰이더
        private GrassShaderGPUDriven _renderShader;
        private GrassCullingComputeShader _cullingComputeShader;
        private GrassCullingFinalizeComputeShader _finalizeComputeShader;

        private uint _grassTextureID;
        private string _projectPath;

        private int _lastCenterTileX = int.MinValue;
        private int _lastCenterTileY = int.MinValue;

        public GrassRendererGPUDriven(string projectPath)
        {
            // 초기화
            _projectPath = projectPath;
            _candidateTiles = new List<CandidateTileData>();

            // 셰이더 로드
            _renderShader = new GrassShaderGPUDriven(projectPath);
            _cullingComputeShader = new GrassCullingComputeShader(projectPath);
            _finalizeComputeShader = new GrassCullingFinalizeComputeShader(projectPath);

            // 텍스처 로드
            _grassTextureID = new Texture(projectPath + @"\Res\PT_Grass_02.png").TextureID;

            // 로컬 템플릿 생성 (한번만 하고 3개 버퍼)
            GenerateLocalTemplates();  // 함수명 변경!

            // GPU 버퍼 생성
            CreateTemplateSSBOs();          // 템플릿 SSBO (3개 생성)
            CreateCandidateTilesSSBO();     // 후보 타일 SSBO
            CreateVisibleTilesSSBOs();      // 가시 타일 SSBO
            CreateIndirectCommandBuffers(); // Indirect Command 버퍼
            CreateAtomicCounterBuffers();   // Atomic Counter 버퍼
            CreateDummyVAO();               // 더미 VAO

            Console.WriteLine($"[GrassRendererGPUDriven] Initialized with GPU LOD");
            Console.WriteLine($"[Max Tiles] {MAX_CANDIDATE_TILES} candidates");
        }

        /// <summary>
        /// LOD별 Atomic Counter Buffer 생성 (3개, binding 0/1/2)
        /// </summary>
        private void CreateAtomicCounterBuffers()
        {
            uint zero = 0;

            for (int lod = 0; lod < LOD_COUNT; lod++)
            {
                _atomicCounterBuffers[lod] = Gl.GenBuffer();
                Gl.BindBuffer(BufferTarget.AtomicCounterBuffer, _atomicCounterBuffers[lod]);

                unsafe
                {
                    Gl.BufferData(
                        BufferTarget.AtomicCounterBuffer,
                        sizeof(uint),
                        new IntPtr(&zero),
                        BufferUsage.DynamicDraw
                    );
                }
            }

            Gl.BindBuffer(BufferTarget.AtomicCounterBuffer, 0);
            Console.WriteLine($"[Atomic Counters] {LOD_COUNT} buffers created");
        }

        /// <summary>
        /// LOD별 가시 타일 SSBO 생성 (3개, binding 3/4/5)
        /// </summary>
        private void CreateVisibleTilesSSBOs()
        {
            for (int lod = 0; lod < LOD_COUNT; lod++)
            {
                _visibleTilesSSBOs[lod] = Gl.GenBuffer();
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleTilesSSBOs[lod]);

                // 최대 121개 타일 (최악의 경우 모두 같은 LOD)
                uint maxSize = MAX_CANDIDATE_TILES * (uint)Marshal.SizeOf<VisibleTileData>();

                Gl.BufferData(
                    BufferTarget.ShaderStorageBuffer,
                    maxSize,
                    IntPtr.Zero,
                    BufferUsage.DynamicDraw
                );

                Console.WriteLine($"[Visible Tiles LOD{lod}] {maxSize / 1024.0:F1} KB allocated");
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        /// <summary>
        /// 후보 타일 SSBO 생성 (Compute Shader 입력, binding = 0)
        /// </summary>
        private void CreateCandidateTilesSSBO()
        {
            _candidateTilesSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _candidateTilesSSBO);

            // 최대 121개 타일
            uint maxSize = MAX_CANDIDATE_TILES * (uint)Marshal.SizeOf<CandidateTileData>();

            Gl.BufferData(
                BufferTarget.ShaderStorageBuffer,
                maxSize,
                IntPtr.Zero,
                BufferUsage.DynamicDraw  // 타일 변경 시 업데이트
            );

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        public void Render(Camera camera, Vertex3f sunDirection, uint heightmapTexture, uint normalMapTexture)
        {
            Gl.Disable(EnableCap.Blend);

            _renderShader.Bind();

            // ============================================================
            // 공통 Uniforms (한 번만 설정)
            // ============================================================

            // Camera vectors
            _renderShader.LoadCameraVectors(camera.Right, new Vertex3f(0, 0, 1));

            // Grass properties
            _renderShader.LoadGrassSize(1.0f, 1.0f);
            _renderShader.LoadGrassColors(
                new Vertex3f(0.5f, 0.8f, 0.3f),
                new Vertex3f(0.2f, 0.4f, 0.1f)
            );

            // Textures
            _renderShader.LoadGrassTexture(_grassTextureID);
            _renderShader.LoadHeightmap(heightmapTexture);
            _renderShader.LoadNormalMap(normalMapTexture);

            // Terrain properties
            _renderShader.LoadHeightScale(TerrainConstants.DEFAULT_VERTICAL_SCALE);
            _renderShader.LoadTerrainWorldSize(1000.0f, 1000.0f);

            // Lighting
            _renderShader.LoadSunDirection(sunDirection);

            // ============================================================
            // Template SSBO 바인딩 (3개, 모든 Draw에서 공통)
            // ============================================================
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _templateSSBOs[0]);  // LOD 0
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _templateSSBOs[1]);  // LOD 1
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 2, _templateSSBOs[2]);  // LOD 2

            // VAO 바인딩 (공통)
            Gl.BindVertexArray(_dummyVAO);

            // ============================================================
            // LOD별로 3번 Draw! (핵심!)
            // ============================================================
            for (int lod = 0; lod < LOD_COUNT; lod++)
            {
                // 1. 현재 LOD의 Visible Tiles SSBO 바인딩 (binding 3)
                Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, _visibleTilesSSBOs[lod]);

                // 2. 현재 LOD 정보 Uniform 설정
                _renderShader.LoadCurrentLOD(lod, GRASS_PER_TILE_LOD[lod]);

                // 3. 해당 LOD의 Indirect Command Buffer 바인딩
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffers[lod]);

                // 4. DrawIndirect 호출
                Gl.DrawArraysIndirect(PrimitiveType.TriangleStrip, IntPtr.Zero);
            }

            // ============================================================
            // 언바인딩
            // ============================================================
            _renderShader.Unbind();
            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, 0);
        }

        /// <summary>
        /// 매 프레임 호출 - GPU에서 Frustum Culling 수행
        /// </summary>
        public void Update(Camera camera, Polyhedron viewFrustum)
        {
            // 1. 현재 타일 좌표 계산
            int centerTileX = (int)(camera.PivotPosition.x / TILE_SIZE);
            int centerTileY = (int)(camera.PivotPosition.y / TILE_SIZE);

            // 2. 타일 중심이 바뀌었으면 후보 타일 업로드 (타일 변경 시 1회만!)
            if (centerTileX != _lastCenterTileX || centerTileY != _lastCenterTileY)
            {
                _lastCenterTileX = centerTileX;
                _lastCenterTileY = centerTileY;
                UpdateCandidateTiles(centerTileX, centerTileY);
                UploadCandidateTiles();

                Console.WriteLine($"[GPU-Driven] Tile center changed to ({centerTileX}, {centerTileY}), " +
                                $"{_candidateTiles.Count} candidates uploaded");
            }

            // 3. Frustum이 없으면 스킵
            if (viewFrustum == null || viewFrustum.Planes == null)
                return;

            // 4. GPU Compute Culling 실행
            DispatchGPUCulling(camera, viewFrustum);
        }

        private void DispatchGPUCulling(Camera camera, Polyhedron viewFrustum)
        {
            // 1. 모든 Atomic Counter 초기화 (3개)
            uint zero = 0;
            for (int lod = 0; lod < LOD_COUNT; lod++)
            {
                Gl.BindBuffer(BufferTarget.AtomicCounterBuffer, _atomicCounterBuffers[lod]);
                unsafe
                {
                    Gl.BufferSubData(BufferTarget.AtomicCounterBuffer, IntPtr.Zero, sizeof(uint), new IntPtr(&zero));
                }
            }
            Gl.BindBuffer(BufferTarget.AtomicCounterBuffer, 0);

            // 2. SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _candidateTilesSSBO);  // 입력

            // LOD별 가시 타일 SSBO (출력)
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 3, _visibleTilesSSBOs[0]);  // LOD 0
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 4, _visibleTilesSSBOs[1]);  // LOD 1
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 5, _visibleTilesSSBOs[2]);  // LOD 2

            // LOD별 Atomic Counter (3개)
            Gl.BindBufferBase(BufferTarget.AtomicCounterBuffer, 0, _atomicCounterBuffers[0]);  // LOD 0
            Gl.BindBufferBase(BufferTarget.AtomicCounterBuffer, 1, _atomicCounterBuffers[1]);  // LOD 1
            Gl.BindBufferBase(BufferTarget.AtomicCounterBuffer, 2, _atomicCounterBuffers[2]);  // LOD 2

            // LOD별 Indirect Command (Finalize에서 사용)
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 6, _indirectCommandBuffers[0]);  // LOD 0
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 7, _indirectCommandBuffers[1]);  // LOD 1
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 8, _indirectCommandBuffers[2]);  // LOD 2

            // 3. Culling Compute Shader 실행
            _cullingComputeShader.Bind();
            _cullingComputeShader.LoadFrustumPlanes(viewFrustum.Planes);
            _cullingComputeShader.LoadCandidateCount(_candidateTiles.Count);
            _cullingComputeShader.LoadTileSize(TILE_SIZE);
            _cullingComputeShader.LoadCameraPosition(camera.PivotPosition);
            _cullingComputeShader.LoadLODDistances(LOD0_DISTANCE, LOD1_DISTANCE, LOD2_DISTANCE);
            _cullingComputeShader.Dispatch(_candidateTiles.Count);
            _cullingComputeShader.Unbind();

            // 4. Memory Barrier
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit |
                             MemoryBarrierMask.AtomicCounterBarrierBit);

            // 5. Finalize Compute Shader 실행 (LOD별로 3번!)
            for (int lod = 0; lod < LOD_COUNT; lod++)
            {
                _finalizeComputeShader.Bind();
                _finalizeComputeShader.LoadGrassPerTile(GRASS_PER_TILE_LOD[lod]);
                _finalizeComputeShader.LoadLODIndex(lod);  // LOD 인덱스 전달 (추가!)
                _finalizeComputeShader.Dispatch();
                _finalizeComputeShader.Unbind();
            }

            // 6. Memory Barrier
            Gl.MemoryBarrier(MemoryBarrierMask.CommandBarrierBit);

            // 디버깅 (선택)
            // LogVisibleCounts();
        }

        /// <summary>
        /// 카메라 주변 후보 타일 생성 (타일 변경 시에만)
        /// </summary>
        private void UpdateCandidateTiles(int centerTileX, int centerTileY)
        {
            _candidateTiles.Clear();

            for (int ty = centerTileY - TILE_RADIUS; ty <= centerTileY + TILE_RADIUS; ty++)
            {
                for (int tx = centerTileX - TILE_RADIUS; tx <= centerTileX + TILE_RADIUS; tx++)
                {
                    float worldX = tx * TILE_SIZE;
                    float worldY = ty * TILE_SIZE;

                    _candidateTiles.Add(new CandidateTileData
                    {
                        WorldX = worldX,
                        WorldY = worldY,
                        MinZ = 0.0f,
                        MaxZ = TerrainConstants.DEFAULT_VERTICAL_SCALE  // 200.0
                    });
                }
            }
        }

        /// <summary>
        /// 후보 타일을 GPU에 업로드
        /// </summary>
        private void UploadCandidateTiles()
        {
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _candidateTilesSSBO);

            int sizeInBytes = _candidateTiles.Count * Marshal.SizeOf<CandidateTileData>();

            unsafe
            {
                fixed (CandidateTileData* ptr = _candidateTiles.ToArray())
                {
                    Gl.BufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        (uint)sizeInBytes,
                        (IntPtr)ptr
                    );
                }
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        /// <summary>
        /// 디버깅: LOD별 가시 타일 개수 읽기
        /// </summary>
        private void LogVisibleCounts()
        {
            for (int lod = 0; lod < LOD_COUNT; lod++)
            {
                Gl.BindBuffer(BufferTarget.AtomicCounterBuffer, _atomicCounterBuffers[lod]);

                uint count = 0;
                unsafe
                {
                    Gl.GetBufferSubData(
                        BufferTarget.AtomicCounterBuffer,
                        IntPtr.Zero,
                        sizeof(uint),
                        new IntPtr(&count)
                    );
                }

                int totalInstances = (int)count * GRASS_PER_TILE_LOD[lod];
                Console.WriteLine($"[LOD {lod}] {count} tiles, {totalInstances:N0} instances");
            }

            Gl.BindBuffer(BufferTarget.AtomicCounterBuffer, 0);
        }

        /// <summary>
        /// LOD별 로컬 템플릿 생성 (한 번만!)
        /// </summary>
        private void GenerateLocalTemplates()
        {
            Random rand = new Random(42);  // 고정 시드

            for (int lod = 0; lod < LOD_COUNT; lod++)
            {
                _localTemplates[lod] = new List<GrassLocalTemplate>();

                int gridSize = GetGridSizeForLOD(lod);
                float spacing = TILE_SIZE / gridSize;

                for (int iy = 0; iy < gridSize; iy++)
                {
                    for (int ix = 0; ix < gridSize; ix++)
                    {
                        float gridCenterX = ix * spacing + spacing * 0.5f;
                        float gridCenterY = iy * spacing + spacing * 0.5f;

                        float jitterX = ((float)rand.NextDouble() - 0.5f) * 2.0f * JITTER_RATIO * spacing;
                        float jitterY = ((float)rand.NextDouble() - 0.5f) * 2.0f * JITTER_RATIO * spacing;

                        _localTemplates[lod].Add(new GrassLocalTemplate
                        {
                            LocalX = gridCenterX + jitterX,
                            LocalY = gridCenterY + jitterY,
                            Rotation = (float)(rand.NextDouble() * Math.PI * 2),
                            Scale = 0.8f + (float)rand.NextDouble() * 0.4f
                        });
                    }
                }

                Console.WriteLine($"[Template LOD{lod}] {_localTemplates[lod].Count} grass per tile " +
                                $"({gridSize}×{gridSize})");
            }
        }

        /// <summary>
        /// LOD별 Template SSBO 생성 (binding 0, 1, 2)
        /// </summary>
        private void CreateTemplateSSBOs()
        {
            for (int lod = 0; lod < LOD_COUNT; lod++)
            {
                _templateSSBOs[lod] = Gl.GenBuffer();
                Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _templateSSBOs[lod]);

                int sizeInBytes = _localTemplates[lod].Count * Marshal.SizeOf<GrassLocalTemplate>();

                unsafe
                {
                    fixed (GrassLocalTemplate* ptr = _localTemplates[lod].ToArray())
                    {
                        Gl.BufferData(
                            BufferTarget.ShaderStorageBuffer,
                            (uint)sizeInBytes,
                            (IntPtr)ptr,
                            BufferUsage.StaticDraw  // 한 번만 업로드!
                        );
                    }
                }

                Console.WriteLine($"[Template LOD{lod}] {sizeInBytes / 1024.0:F1} KB uploaded");
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        /// <summary>
        /// LOD별 Indirect Command Buffer 생성 (3개)
        /// </summary>
        private void CreateIndirectCommandBuffers()
        {
            for (int lod = 0; lod < LOD_COUNT; lod++)
            {
                _indirectCommandBuffers[lod] = Gl.GenBuffer();
                Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffers[lod]);

                DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
                {
                    VertexCount = 4,
                    InstanceCount = 0,
                    First = 0,
                    BaseInstance = 0
                };

                unsafe
                {
                    Gl.BufferData(
                        BufferTarget.DrawIndirectBuffer,
                        (uint)Marshal.SizeOf<DrawArraysIndirectCommand>(),
                        new IntPtr(&cmd),
                        BufferUsage.DynamicDraw
                    );
                }
            }

            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, 0);
            Console.WriteLine($"[Indirect Commands] {LOD_COUNT} buffers created");
        }

        private void CreateDummyVAO()
        {
            _dummyVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_dummyVAO);
            Gl.BindVertexArray(0);
        }

        public void Dispose()
        {
            // Template SSBO 정리
            for (int i = 0; i < LOD_COUNT; i++)
            {
                if (_templateSSBOs[i] != 0) Gl.DeleteBuffers(_templateSSBOs[i]);
            }

            // Visible Tiles SSBO 정리
            for (int i = 0; i < LOD_COUNT; i++)
            {
                if (_visibleTilesSSBOs[i] != 0) Gl.DeleteBuffers(_visibleTilesSSBOs[i]);
            }

            // Indirect Command Buffer 정리
            for (int i = 0; i < LOD_COUNT; i++)
            {
                if (_indirectCommandBuffers[i] != 0) Gl.DeleteBuffers(_indirectCommandBuffers[i]);
            }

            // Atomic Counter Buffer 정리
            for (int i = 0; i < LOD_COUNT; i++)
            {
                if (_atomicCounterBuffers[i] != 0) Gl.DeleteBuffers(_atomicCounterBuffers[i]);
            }

            if (_candidateTilesSSBO != 0) Gl.DeleteBuffers(_candidateTilesSSBO);
            if (_dummyVAO != 0) Gl.DeleteVertexArrays(_dummyVAO);

            // 셰이더 정리
            //_renderShader?.Dispose();
            //_cullingComputeShader?.Dispose();
            //_finalizeComputeShader?.Dispose();

            Console.WriteLine("[GrassRendererGPUDriven] Disposed");
        }

        /// <summary>
        /// LOD 레벨에 해당하는 풀 개수 반환
        /// </summary>
        private int GetGrassCountForLOD(int lod)
        {
            if (lod < 0 || lod >= LOD_COUNT)
                return 0;

            return GRASS_PER_TILE_LOD[lod];
        }

        /// <summary>
        /// LOD 레벨에 해당하는 그리드 크기 반환
        /// </summary>
        private int GetGridSizeForLOD(int lod)
        {
            switch (lod)
            {
                case 0: return LOD0_WIDTH_HEIGHT;
                case 1: return LOD1_WIDTH_HEIGHT;
                case 2: return LOD2_WIDTH_HEIGHT;
                default: return 0;
            }
        }

        /// <summary>
        /// 거리에 따른 LOD 레벨 계산 (C# 디버깅용)
        /// </summary>
        private int CalculateLODLevel(float distance)
        {
            if (distance < LOD0_DISTANCE) return 0;
            if (distance < LOD1_DISTANCE) return 1;
            if (distance < LOD2_DISTANCE) return 2;
            return 3;  // 컬링
        }
    }

}