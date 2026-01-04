using Model3d;
using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ZetaExt;

namespace GPUDriven
{
    /// <summary>
    /// GPU로 업로드할 인스턴스 데이터 (ModelMatrix + NormalMatrix)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceModelMatrixData
    {
        public Matrix4x4f Model;      // 64 bytes
        public Matrix4x4f Normal;     // 64 bytes
                                            // Total: 128 bytes per instance

        /// <summary>
        /// ModelMatrix로부터 InstanceModelMatrixData 생성 (CPU에서 NormalMatrix 계산)
        /// </summary>
        public static InstanceModelMatrixData Create(Matrix4x4f modelMatrix)
        {
            // Normal Matrix 계산: transpose(inverse(upper 3x3))
            Matrix3x3f upper3x3 = modelMatrix.Rot3x3f();

            Matrix3x3f normalMat3 = upper3x3.Inversed().Transposed;

            // 4x4로 확장
            Matrix4x4f normalMatrix = Matrix4x4f.Identity;
            normalMatrix[0, 0] = normalMat3[0, 0];
            normalMatrix[0, 1] = normalMat3[0, 1];
            normalMatrix[0, 2] = normalMat3[0, 2];
            normalMatrix[1, 0] = normalMat3[1, 0];
            normalMatrix[1, 1] = normalMat3[1, 1];
            normalMatrix[1, 2] = normalMat3[1, 2];
            normalMatrix[2, 0] = normalMat3[2, 0];
            normalMatrix[2, 1] = normalMat3[2, 1];
            normalMatrix[2, 2] = normalMat3[2, 2];

            return new InstanceModelMatrixData
            {
                Model = modelMatrix,
                Normal = normalMatrix
            };
        }
    }

    /// <summary>
    /// 임시 인스턴스 데이터 (정렬 전) 모델행렬만 있어도 됨.
    /// </summary>
    internal struct TempInstance
    {
        public uint ModelID;
        public Matrix4x4f Transform;

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
        public uint ModelID { get; set; }
        public string ModelName { get; set; }
        public float LODDistance { get; set; }
        public AABB LocalModelAABB { get; set; }
        public UnifiedTexturedModel Model { get; set; }
        public UnifiedTexturedModel Model_LOD1 { get; set; }
        public uint VAO { get; set; }
        public uint VertexCount { get; set; }

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
        public uint ModelID { get; set; }
        public string ModelName { get; set; }
        public uint StartIndex { get; set; }
        public uint Count { get; set; }
        public float LODDistance { get; set; }
        public AABB LocalModelAABB { get; set; }
        public UnifiedTexturedModel Model { get; set; }
        public UnifiedTexturedModel Model_LOD1 { get; set; }
        public uint VAO { get; set; }
        public uint VertexCount { get; set; }

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

        private List<ModelInfo> _models;
        private uint _nextModelID;
        private List<TempInstance> _tempInstances;

        // ✅ 최종 데이터 (InstanceModelMatrixData 배열)
        private InstanceModelMatrixData[] _finalInstanceData;
        private AABB[] _finalAABBs;
        private uint[] _finalBatchIDs;

        private BatchDescriptor[] _batches;
        private uint _actualBatchCount;
        private bool _isFinalized;
        private uint _finalInstanceCount;

        public IReadOnlyList<ModelInfo> Models => _models.AsReadOnly();
        public IReadOnlyList<BatchDescriptor> Batches =>
            _batches != null ? _batches.Take((int)_actualBatchCount).ToList().AsReadOnly() : null;
        public uint TotalInstances => _isFinalized ? _finalInstanceCount : (uint)_tempInstances.Count;
        public uint ActualBatchCount => _actualBatchCount;
        public bool IsFinalized => _isFinalized;

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
            _finalInstanceCount = 0;

            Console.WriteLine("--------------------------------------------------------------------");
            Console.WriteLine("# 모델 배치매니저");
            Console.WriteLine("--------------------------------------------------------------------");
            Console.WriteLine("ModelBatchManager Initialized (v3 - with NormalMatrix)");
            Console.WriteLine($"Max Instances: {MAX_INSTANCES}");
            Console.WriteLine($"Max Batches: {MAX_BATCHES}");
            Console.WriteLine($"Instance Size: {Marshal.SizeOf<InstanceModelMatrixData>()} bytes");
        }

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

            uint modelID = _nextModelID++;

            ModelInfo modelInfo = new ModelInfo
            {
                ModelID = modelID,
                ModelName = modelName,
                LODDistance = lodDistance,
                Model = model,
                VAO = model.VaoID,
                VertexCount = (uint)model.VertexCount,
                Model_LOD1 = lod1,
                LocalModelAABB = CalculateModelAABB(model)
            };

            _models.Add(modelInfo);

            Console.WriteLine($"[Model {modelID}] Added: {modelName}"
                + $"  AABB: Min{modelInfo.LocalModelAABB.Min} Max{modelInfo.LocalModelAABB.Max}");

            return modelID;
        }

        public void AddInstance(uint modelID, Matrix4x4f transform)
        {
            if (_isFinalized)
            {
                throw new InvalidOperationException("Cannot add instances after Finalize()");
            }

            if (modelID >= _models.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(modelID),
                    $"Invalid model ID: {modelID}. Valid range: 0~{_models.Count - 1}");
            }

            if (_tempInstances.Count >= MAX_INSTANCES)
            {
                throw new InvalidOperationException(
                    $"Instance pool exhausted! Max: {MAX_INSTANCES}");
            }

            _tempInstances.Add(new TempInstance(modelID, transform));
        }

        public void AddInstances(uint modelID, IEnumerable<Matrix4x4f> transforms)
        {
            foreach (var transform in transforms)
            {
                AddInstance(modelID, transform);
            }
        }

        /// <summary>
        /// 최종화 및 정렬 (NormalMatrix 계산 포함)
        /// </summary>
        public void Finalized()
        {
            if (_isFinalized)
            {
                Console.WriteLine("Already finalized");
                return;
            }

            Console.WriteLine("\n=== Starting Finalization (with NormalMatrix) ===");
            Console.WriteLine($"Total Models: {_models.Count}");
            Console.WriteLine($"Total Instances (unsorted): {_tempInstances.Count}");

            if (_tempInstances.Count == 0)
            {
                Console.WriteLine("WARNING: No instances added!");
                _isFinalized = true;
                return;
            }

            var sortedInstances = _tempInstances.OrderBy(x => x.ModelID).ToArray();

            _finalInstanceData = new InstanceModelMatrixData[MAX_INSTANCES];
            _finalAABBs = new AABB[MAX_INSTANCES];
            _finalBatchIDs = new uint[MAX_INSTANCES];
            _batches = new BatchDescriptor[MAX_BATCHES];

            uint currentIndex = 0;
            uint currentModelID = sortedInstances[0].ModelID;
            uint batchStart = 0;
            uint batchCount = 0;
            List<uint> usedModelIDs = new List<uint>();

            for (uint i = 0; i < sortedInstances.Length; i++)
            {
                TempInstance inst = sortedInstances[i];

                if (inst.ModelID != currentModelID)
                {
                    SaveBatch(currentModelID, batchStart, batchCount, usedModelIDs);
                    currentModelID = inst.ModelID;
                    batchStart = currentIndex;
                    batchCount = 0;
                }

                _finalInstanceData[currentIndex] = InstanceModelMatrixData.Create(inst.Transform);

                ModelInfo modelInfo = _models[(int)inst.ModelID];
                _finalAABBs[currentIndex] = TransformAABB(modelInfo.LocalModelAABB, inst.Transform);
                _finalBatchIDs[currentIndex] = inst.ModelID;

                currentIndex++;
                batchCount++;
            }

            SaveBatch(currentModelID, batchStart, batchCount, usedModelIDs);
            _actualBatchCount = (uint)usedModelIDs.Count;

            // ✅ 핵심: 실제 인스턴스 수 저장
            _finalInstanceCount = currentIndex;

            Console.WriteLine("\n=== Finalization Complete ===");
            Console.WriteLine($"Total Instances (sorted): {_finalInstanceCount}");  // ✅ 수정
            Console.WriteLine($"Active Batches: {_actualBatchCount}");
            Console.WriteLine($"Total GPU Memory: {_finalInstanceCount * Marshal.SizeOf<InstanceModelMatrixData>() / 1024.0 / 1024.0:F2} MB");

            // ✅ 메모리 낭비 경고
            if (_finalInstanceCount < MAX_INSTANCES / 2)
            {
                Console.WriteLine($"⚠️ Warning: Using only {_finalInstanceCount}/{MAX_INSTANCES} slots ({_finalInstanceCount * 100.0 / MAX_INSTANCES:F1}%)");
            }

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
        /// ✅ InstanceModelMatrixData 배열 반환 (GPU 업로드용)
        /// </summary>
        public InstanceModelMatrixData[] GetInstanceData()
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting instance data");
            }
            return _finalInstanceData;
        }

        public AABB[] GetAABBs()
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting AABBs");
            }
            return _finalAABBs;
        }

        public uint[] GetBatchIDs()
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting batch IDs");
            }
            return _finalBatchIDs;
        }

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

        public BatchDescriptor GetBatch(uint modelID)
        {
            if (!_isFinalized)
            {
                throw new InvalidOperationException(
                    "Must call Finalize() before getting batches");
            }

            if (modelID >= MAX_BATCHES || _batches[modelID] == null)
            {
                throw new ArgumentOutOfRangeException(nameof(modelID),
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