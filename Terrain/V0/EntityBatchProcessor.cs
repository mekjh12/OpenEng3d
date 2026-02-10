using Model3d;
using Occlusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZetaExt;

namespace Terrain
{
    public class EntityBatchProcessor
    {
        private Queue _entityQueue;
        private int _processedCount;
        private Task _processingTask;

        private readonly int BATCH_SIZE = 100;
        private const int LOG_INTERVAL = 1000;      // 1000개마다 로그를 출력하는 상수

        private Action<Entity> _action;
        private Action _completed;

        public bool IsProcessing => _processingTask != null && !_processingTask.IsCompleted;
        public int RemainingCount => _entityQueue?.Count ?? 0;
        public Action Completed { get=>_completed; set => _completed = value; }

        public EntityBatchProcessor(int batchSize = 100, Action<Entity> action = null)
        {
            BATCH_SIZE = batchSize;
            _entityQueue = new Queue();
            _processedCount = 0;
            _action = action;
        }

        public void EnqueueEntity(Entity entity)
        {
            _entityQueue.Enqueue(entity);
        }

        public async Task StartProcessing()
        {
            if (IsProcessing)
            {
                Debug.PrintLine("이미 처리 중입니다.");
                return;
            }

            _processingTask = ProcessEntitiesAsync();
            await _processingTask;
        }

        private async Task ProcessEntitiesAsync()
        {
            while (_entityQueue.Count > 0)
            {
                var entities = new List<Entity>();

                // BATCH_SIZE만큼 묶어서 처리
                for (int i = 0; i < BATCH_SIZE && _entityQueue.Count > 0; i++)
                {
                    entities.Add((Entity)_entityQueue.Dequeue());
                }

                await Task.Run(() =>
                {
                    foreach (var entity in entities)
                    {
                        _action(entity);
                        _processedCount++;
                    }
                });

                if (_processedCount >= LOG_INTERVAL)
                {
                    Debug.PrintLine($"{_entityQueue.Count}개 남음");
                    _processedCount = 0;
                }
            }

            _completed();
            //Debug.PrintLine($"엔티티 등록 완료! {DateTime.Now}");
        }

    }
}
