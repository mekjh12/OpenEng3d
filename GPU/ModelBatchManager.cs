using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using ZetaExt;

namespace GPUDriven
{
    /// <summary>
    /// 임시 인스턴스 데이터 (정렬 전)
    /// </summary>
    internal struct TempInstance
    {
        public uint ModelID;            // 어느 모델인지 (0~63)
        public Matrix4x4f Transform;    // Transform 행렬

        public TempInstance(uint modelID, Matrix4x4f transform)
        {
            ModelID = modelID;
            Transform = transform;
        }
    }

    /// <summary>
    /// 모델 정보 (메타데이터)
    /// </summary>
    public class ModelInfo
    {
        public uint ModelID { get; set; }              // 모델 ID (0~63)
        public string ModelName { get; set; }          // 모델 이름
        public float LODDistance { get; set; }         // LOD 전환 거리
        public AABB LocalModelAABB { get; set; }        // 로컬 공간 AABB
        public UnifiedTexturedModel Model { get; set; }    // 메시 배열
        public UnifiedTexturedModel Model_LOD1 { get; set; }    // 메시 배열
        public uint VAO { get; set; }               // VAO 배열
        public uint VertexCount { get; set; }       // 정점 수 배열

        public ModelInfo()
        {
            ModelID = 0;
            ModelName = string.Empty;
            LODDistance = 60f;
            LocalModelAABB = new AABB();
            Model = null;
            VAO = 0;
            VertexCount = 0;
        }
    }

    /// <summary>
    /// Batch 설명자 (정렬 후 최종 정보)
    /// </summary>
    public class BatchDescriptor
    {
        public uint ModelID { get; set; }              // 원본 모델 ID
        public string ModelName { get; set; }          // 모델 이름
        public uint StartIndex { get; set; }           // 정렬 후 시작 인덱스
        public uint Count { get; set; }                // 인스턴스 개수
        public float LODDistance { get; set; }         // LOD 거리
        public AABB LocalModelAABB { get; set; }        // 기준 AABB
        public UnifiedTexturedModel Model { get; set; }    // 메시 배열
        public UnifiedTexturedModel Model_LOD1 { get; set; }    // 메시 배열
        public uint VAO { get; set; }               // VAO 배열
        public uint VertexCount { get; set; }       // 정점 수 배열

        public BatchDescriptor()
        {
            ModelID = 0;
            ModelName = string.Empty;
            StartIndex = 0;
            Count = 0;
            LODDistance = 60f;
            LocalModelAABB = new AABB();
            Model = null;
            Model_LOD1 = null;
            VAO = 0;
            VertexCount = 0;
        }
    }

    /// <summary>
    /// 모델 Batch 관리자 - 동적 추가 + 정렬 방식
    /// </summary>
    public class ModelBatchManager
    {
        private const uint MAX_INSTANCES = 100000;
        private const uint MAX_BATCHES = 64;

        // 모델 정보 (등록된 모델들)
        private List<ModelInfo> _models;
        private uint _nextModelID;

        // 임시 인스턴스 저장 (정렬 전)
        private List<TempInstance> _tempInstances;

        // 최종 정렬된 데이터 (Finalize 후)
        private Matrix4x4f[] _finalTransforms;
        private AABB[] _finalAABBs;
        private uint[] _finalBatchIDs;

        // Batch 메타데이터 (정렬 후)
        private BatchDescriptor[] _batches;
        private uint _actualBatchCount;

        // 상태
        private bool _isFinalized;

        public IReadOnlyList<ModelInfo> Models => _models.AsReadOnly();
        public IReadOnlyList<BatchDescriptor> Batches =>
            _batches != null ? _batches.Take((int)_actualBatchCount).ToList().AsReadOnly() : null;
        public uint TotalInstances => (uint)_tempInstances.Count;
        public uint ActualBatchCount => _actualBatchCount;
        public bool IsFinalized => _isFinalized;

        // 최적화: GPU 업로드용 배열 미리 할당
        float[] _lods = new float[MAX_BATCHES];
        uint[] _counts = new uint[MAX_BATCHES];
        uint[] _starts = new uint[MAX_BATCHES];

        public ModelBatchManager()
        {
            _models = new List<ModelInfo>();
            _tempInstances = new List<TempInstance>();
            _nextModelID = 0;
            _actualBatchCount = 0;
            _isFinalized = false;

            Console.WriteLine("=== ModelBatchManager Initialized (v2) ===");
            Console.WriteLine($"Max Instances: {MAX_INSTANCES}");
            Console.WriteLine($"Max Batches: {MAX_BATCHES}");
        }

        /// <summary>
        /// 모델 추가 (개수 지정 없음)
        /// </summary>
        public uint AddModel(
            string modelName,
            float lodDistance,
            UnifiedTexturedModel model, 
            UnifiedTexturedModel lod1 = null)
        {
            if (_isFinalized)
            {
                throw new InvalidOperationException(
                    "Cannot add model after Finalize() has been called");
            }

            if (_models.Count >= MAX_BATCHES)
            {
                throw new InvalidOperationException(
                    $"Maximum batch count ({MAX_BATCHES}) exceeded");
            }

            // 모델 ID 할당
            uint modelID = _nextModelID++;

            // 모델 정보 생성
            ModelInfo modelInfo = new ModelInfo
            {
                ModelID = modelID,
                ModelName = modelName,
                LODDistance = lodDistance,
                Model = model
            };

            // VAO 및 정점 수 추출
            modelInfo.VAO = 0;
            modelInfo.VertexCount = 0;
            modelInfo.VAO = model.VaoID;
            modelInfo.VertexCount = (uint)model.VertexCount;
            modelInfo.Model_LOD1 = lod1;

            // 기준 AABB 계산
            modelInfo.LocalModelAABB = CalculateModelAABB(model);

            // 모델 등록
            _models.Add(modelInfo);

            Console.WriteLine($"[Model {modelID}] Added: {modelName}");
            Console.WriteLine($"  LOD Distance: {lodDistance}m");
            Console.WriteLine($"  AABB: Min{modelInfo.LocalModelAABB.Min} Max{modelInfo.LocalModelAABB.Max}");

            return modelID;
        }

        /// <summary>
        /// 인스턴스 추가 (순서 무관)
        /// </summary>
        public void AddInstance(uint modelID, Matrix4x4f transform)
        {
            if (_isFinalized)
            {
                throw new InvalidOperationException(
                    "Cannot add instances after Finalize()");
            }

            // 모델 ID 유효성 검사
            if (modelID >= _models.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(modelID),
                    $"Invalid model ID: {modelID}. Valid range: 0~{_models.Count - 1}");
            }

            // 용량 체크
            if (_tempInstances.Count >= MAX_INSTANCES)
            {
                throw new InvalidOperationException(
                    $"Instance pool exhausted! Max: {MAX_INSTANCES}");
            }

            // 임시 버퍼에 추가 (정렬되지 않음)
            _tempInstances.Add(new TempInstance(modelID, transform));
        }

        /// <summary>
        /// 여러 인스턴스 일괄 추가
        /// </summary>
        public void AddInstances(uint modelID, IEnumerable<Matrix4x4f> transforms)
        {
            foreach (var transform in transforms)
            {
                AddInstance(modelID, transform);
            }
        }

        /// <summary>
        /// 최종화 및 정렬
        /// </summary>
        public void Finalized()
        {
            if (_isFinalized)
            {
                Console.WriteLine("Already finalized");
                return;
            }

            Console.WriteLine("\n=== Starting Finalization ===");
            Console.WriteLine($"Total Models: {_models.Count}");
            Console.WriteLine($"Total Instances (unsorted): {_tempInstances.Count}");

            if (_tempInstances.Count == 0)
            {
                Console.WriteLine("WARNING: No instances added!");
                _isFinalized = true;
                return;
            }

            // 1. ModelID 기준으로 정렬
            Console.WriteLine("Step 1: Sorting instances by ModelID...");
            var sortedInstances = _tempInstances.OrderBy(x => x.ModelID).ToArray();

            // 2. 최종 버퍼 할당
            Console.WriteLine("Step 2: Allocating final buffers...");
            _finalTransforms = new Matrix4x4f[MAX_INSTANCES];
            _finalAABBs = new AABB[MAX_INSTANCES];
            _finalBatchIDs = new uint[MAX_INSTANCES];
            _batches = new BatchDescriptor[MAX_BATCHES];

            // 3. Batch 경계 계산 및 데이터 복사
            Console.WriteLine("Step 3: Computing batch boundaries...");
            uint currentIndex = 0;
            uint currentModelID = sortedInstances[0].ModelID;
            uint batchStart = 0;
            uint batchCount = 0;
            List<uint> usedModelIDs = new List<uint>();

            for (uint i = 0; i < sortedInstances.Length; i++)
            {
                TempInstance inst = sortedInstances[i];

                // 모델 ID가 바뀌면 이전 Batch 저장
                if (inst.ModelID != currentModelID)
                {
                    SaveBatch(currentModelID, batchStart, batchCount, usedModelIDs);

                    currentModelID = inst.ModelID;
                    batchStart = currentIndex;
                    batchCount = 0;
                }

                // Transform 복사
                _finalTransforms[currentIndex] = inst.Transform;

                // AABB 계산
                ModelInfo modelInfo = _models[(int)inst.ModelID];
                _finalAABBs[currentIndex] = TransformAABB(
                    modelInfo.LocalModelAABB,
                    inst.Transform);

                // Batch ID 저장
                _finalBatchIDs[currentIndex] = inst.ModelID;

                currentIndex++;
                batchCount++;
            }

            // 마지막 Batch 저장
            SaveBatch(currentModelID, batchStart, batchCount, usedModelIDs);

            _actualBatchCount = (uint)usedModelIDs.Count;

            // 4. 요약 출력
            Console.WriteLine("\n=== Finalization Complete ===");
            Console.WriteLine($"Total Instances (sorted): {currentIndex}");
            Console.WriteLine($"Active Batches: {_actualBatchCount}");
            Console.WriteLine("\n=== Batch Summary ===");

            foreach (var modelID in usedModelIDs)
            {
                BatchDescriptor batch = _batches[modelID];
                Console.WriteLine($"Batch {modelID}: {batch.ModelName}");
                Console.WriteLine($"  Range: [{batch.StartIndex} ~ {batch.StartIndex + batch.Count - 1}]");
                Console.WriteLine($"  Count: {batch.Count}");
                Console.WriteLine($"  LOD: {batch.LODDistance}m");
            }

            _isFinalized = true;
        }

        /// <summary>
        /// Transform 배열 반환 (GPU 업로드용)
        /// </summary>
        public Matrix4x4f[] GetTransforms()
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting transforms");
            }
            return _finalTransforms;
        }

        /// <summary>
        /// AABB 배열 반환 (GPU 업로드용)
        /// </summary>
        public AABB[] GetAABBs()
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting AABBs");
            }
            return _finalAABBs;
        }

        /// <summary>
        /// Batch ID 배열 반환 (GPU 업로드용)
        /// </summary>
        public uint[] GetBatchIDs()
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting batch IDs");
            }
            return _finalBatchIDs;
        }

        /// <summary>
        /// Batch 시작 인덱스 배열 반환 (GPU Uniform 전달용)
        /// </summary>
        public uint[] GetBatchStarts()
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting batch starts");
            }

            for (uint i = 0; i < _actualBatchCount; i++)
            {
                if (_batches[i] != null)
                {
                    _starts[i] = _batches[i].StartIndex;
                }
            }
            return _starts;
        }

        /// <summary>
        /// Batch 개수 배열 반환 (GPU Uniform 전달용)
        /// </summary>
        public uint[] GetBatchCounts()
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting batch counts");
            }

            for (uint i = 0; i < _actualBatchCount; i++)
            {
                if (_batches[i] != null)
                {
                    _counts[i] = _batches[i].Count;
                }
            }
            return _counts;
        }

        /// <summary>
        /// Batch LOD 거리 배열 반환 (GPU Uniform 전달용)
        /// </summary>
        public float[] GetBatchLODs()
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting batch LODs");
            }

            
            for (uint i = 0; i < _actualBatchCount; i++)
            {
                if (_batches[i] != null)
                {
                    _lods[i] = _batches[i].LODDistance;
                }
            }
            return _lods;
        }

        /// <summary>
        /// 특정 ModelID의 Batch 정보 가져오기
        /// </summary>
        public BatchDescriptor GetBatch(uint modelID)
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting batches");
            }

            if (modelID >= MAX_BATCHES || _batches[modelID] == null)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(modelID),
                    $"Invalid or unused model ID: {modelID}");
            }

            return _batches[modelID];
        }

        #region Private Methods

        private void SaveBatch(uint modelID, uint start, uint count, List<uint> usedModelIDs)
        {
            ModelInfo modelInfo = _models[(int)modelID];

            _batches[modelID] = new BatchDescriptor
            {
                ModelID = modelID,
                ModelName = modelInfo.ModelName,
                StartIndex = start,
                Count = count,
                LODDistance = modelInfo.LODDistance,
                LocalModelAABB = modelInfo.LocalModelAABB,
                Model = modelInfo.Model,
                VAO = modelInfo.VAO,
                VertexCount = modelInfo.VertexCount,
                Model_LOD1 = modelInfo.Model_LOD1
            };

            usedModelIDs.Add(modelID);
        }

        private AABB CalculateModelAABB(UnifiedTexturedModel model)
        {
            return new AABB(model.AABB.Min, model.AABB.Max);
        }

        private AABB TransformAABB(AABB local, Matrix4x4f transform)
        {
            // 8개 코너 변환
            Vertex3f[] corners = new Vertex3f[8];
            corners[0] = new Vertex3f(local.Min.x, local.Min.y, local.Min.z);
            corners[1] = new Vertex3f(local.Max.x, local.Min.y, local.Min.z);
            corners[2] = new Vertex3f(local.Min.x, local.Max.y, local.Min.z);
            corners[3] = new Vertex3f(local.Max.x, local.Max.y, local.Min.z);
            corners[4] = new Vertex3f(local.Min.x, local.Min.y, local.Max.z);
            corners[5] = new Vertex3f(local.Max.x, local.Min.y, local.Max.z);
            corners[6] = new Vertex3f(local.Min.x, local.Max.y, local.Max.z);
            corners[7] = new Vertex3f(local.Max.x, local.Max.y, local.Max.z);

            Vertex3f min = new Vertex3f(float.MaxValue);
            Vertex3f max = new Vertex3f(float.MinValue);

            for (int i = 0; i < 8; i++)
            {
                Vertex3f transformed = transform.Transform(corners[i]);
                min = Vertex3f.Min(min, transformed);
                max = Vertex3f.Max(max, transformed);
            }

            return new AABB(min, max);
        }

        #endregion
    }
}