using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>近接サンプル用シェーダグローバル（<c>CpuProximityData2D.hlsl</c> と対応）。</summary>
    public static class CpuProximityShaderGlobals2D {

        public static readonly int LengthId = Shader.PropertyToID("_CpuPointPositions2D_Length");
        public static readonly int BufferId = Shader.PropertyToID("_CpuPointPositions2D");

        public static void Clear() {
            Shader.SetGlobalInteger(LengthId, 0);
            Shader.SetGlobalBuffer(BufferId, (GraphicsBuffer)null);
        }
    }
}
