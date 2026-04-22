using UnityEngine;
using UnityEngine.Rendering;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// コンパクトグリッドの <see cref="GPUCompactUniformGrid2D.cellCounts"/> / 3D 版を読み戻し、
    /// <c>count &gt; M</c> のセルがあれば <see cref="Debug.LogWarning"/> する（デバッグ専用）。
    /// </summary>
    public static class CompactUniformGridOverflowDebug {

        public static bool TryLogOverflowCells2D(GraphicsBuffer cellCounts, uint gridWidth, uint gridHeight, uint m, string tag = null) {
            if (cellCounts == null || cellCounts.count <= 0 || m < 1)
                return false;
            var data = new uint[cellCounts.count];
            cellCounts.GetData(data);
            var prefix = string.IsNullOrEmpty(tag) ? "[CompactGrid2D]" : $"[CompactGrid2D:{tag}]";
            var any = false;
            for (var i = 0; i < data.Length; i++) {
                if (data[i] <= m)
                    continue;
                any = true;
                var cx = i % (int)gridWidth;
                var cy = i / (int)gridWidth;
                Debug.LogWarning(
                    $"{prefix} Cell ({cx},{cy}) arrival count={data[i]} > M={m}. " +
                    "Consider smaller cells (finer grid), larger M, lower particle density, or gridCenter/gridSize.");
            }
            return any;
        }

        public static bool TryLogOverflowCells3D(GraphicsBuffer cellCounts, uint nx, uint ny, uint nz, uint m, string tag = null) {
            if (cellCounts == null || cellCounts.count <= 0 || m < 1)
                return false;
            var data = new uint[cellCounts.count];
            cellCounts.GetData(data);
            var prefix = string.IsNullOrEmpty(tag) ? "[CompactGrid3D]" : $"[CompactGrid3D:{tag}]";
            var any = false;
            var strideY = (int)nx;
            var strideZ = (int)(nx * ny);
            for (var i = 0; i < data.Length; i++) {
                if (data[i] <= m)
                    continue;
                any = true;
                var cz = i / strideZ;
                var rem = i % strideZ;
                var cy = rem / strideY;
                var cx = rem % strideY;
                Debug.LogWarning(
                    $"{prefix} Cell ({cx},{cy},{cz}) arrival count={data[i]} > M={m}. " +
                    "Consider smaller cells (more cells per axis), larger M, lower particle density, or gridCenter/gridSize.");
            }
            return any;
        }
    }
}
