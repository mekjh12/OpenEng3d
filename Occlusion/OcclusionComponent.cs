using Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Occlusion
{
    internal class OcclusionComponent: IOcclusionable
    {
        private BoxOccluder _boxOccluder;
        private bool _isOccluder;

        public bool IsOccluder
        {
            get => _boxOccluder != null;
        }

        public BoxOccluder BoxOccluder
        {
            get => _boxOccluder;
            set => _boxOccluder = value;
        }

        /// <summary>
        /// 월드 공간의 박스오클루더를 계산한다.
        /// </summary>
        public void GenBoxOccluder(OBB obb)
        {
            // OBB를 계산하지 않기 때문에 OBB의 프레임만 가져오는 계산만 한다.
            _boxOccluder = new BoxOccluder(obb);
        }

    }
}
