using Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Occlusion
{
    public interface IOcclusionable
    {
        bool IsOccluder { get; }

        BoxOccluder BoxOccluder { get; set; }

        void GenBoxOccluder(OBB obb);
    }
}
