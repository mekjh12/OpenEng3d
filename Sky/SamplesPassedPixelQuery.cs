using OpenGL;
using System;

namespace Sky
{
    public class SamplesPassedPixelQuery
    {
        // 쿼리 오브젝트를 위한 필드 선언
        private uint _atmospherePixelQuery;
        private bool _queryActive = false;
        private int _lastPixelCount = 0;

        /// <summary>
        /// 픽셀 쿼리 생성자
        /// </summary>
        public SamplesPassedPixelQuery()
        {
            // 쿼리 오브젝트 생성
            _atmospherePixelQuery = Gl.GenQuery();
        }

        /// <summary>
        /// 마지막으로 측정된 픽셀 수
        /// </summary>
        public int LastPixelCount { get => _lastPixelCount; }

        /// <summary>
        /// 쿼리 시작
        /// </summary>
        public void BeginQuery()
        {
            // 이전 쿼리가 활성화되어 있는지 확인
            if (_queryActive)
            {
                // 쿼리 결과가 사용 가능한지 확인
                Gl.GetQueryObject(_atmospherePixelQuery, QueryObjectParameterName.QueryResultAvailable, out int available);

                if (available != 0)
                {
                    // 결과 가져오기
                    Gl.GetQueryObject(_atmospherePixelQuery, QueryObjectParameterName.QueryResult, out int pixelCount);
                    _queryActive = false;
                    _lastPixelCount = pixelCount;
                }
            }

            // 새 쿼리 시작
            if (!_queryActive)
            {
                Gl.BeginQuery(QueryTarget.SamplesPassed, _atmospherePixelQuery);
                _queryActive = true;
            }

        }

        /// <summary>
        /// 쿼리 종료
        /// </summary>
        public void EndQuery()
        {
            // 쿼리 종료
            if (_queryActive)
            {
                Gl.EndQuery(QueryTarget.SamplesPassed);
            }
        }

    }
}