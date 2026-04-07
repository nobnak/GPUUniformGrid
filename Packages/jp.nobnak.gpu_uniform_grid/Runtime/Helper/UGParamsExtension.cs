using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {
    public static class UGParamsExtension {

        public static bool IsValid(this UniformGridParams param) {
            return param.gridSize > 0f
                && math.all(math.isfinite(param.gridCenter))
                && param.elementCapacity > 0
                && param.bitsPerAxis <= UniformGridParams.MaxSupportedBitsPerAxis;
        }

        public static bool IsValid(this UniformGridParams2D param) {
            return math.all(param.gridSize > 0f)
                && math.all(math.isfinite(param.gridCenter))
                && math.isfinite(param.planeZ)
                && param.elementCapacity > 0
                && param.bitsPerAxis <= UniformGridParams2D.MaxSupportedBitsPerAxis;
        }
    }
}