using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// コンパクト格納グリッド（3D 立方体領域）。座標は <see cref="UniformGridParams"/> と同型（スカラー一辺・<see cref="GridOffset"/>）。
    /// </summary>
    public struct CompactUniformGridParams3D {
        public readonly float3 gridCenter;
        public readonly float gridSize;
        public readonly uint cellsPerAxis;
        public readonly uint elementCapacityPerCell;

        public readonly float3 GridOffset;
        public readonly float CellSize;
        public readonly uint TotalCells;

        public CompactUniformGridParams3D(
            float3 gridCenter,
            float gridSize,
            uint cellsPerAxis,
            uint elementCapacityPerCell) {
            this.gridCenter = gridCenter;
            this.gridSize = gridSize;
            this.cellsPerAxis = cellsPerAxis;
            this.elementCapacityPerCell = elementCapacityPerCell;

            GridOffset = gridCenter - gridSize * 0.5f;
            CellSize = gridSize / cellsPerAxis;
            ulong tc = (ulong)cellsPerAxis * cellsPerAxis * cellsPerAxis;
            if (tc > int.MaxValue) {
                Debug.LogError($"{nameof(CompactUniformGridParams3D)}: TotalCells exceed {int.MaxValue}. Reduce cellsPerAxis.");
                tc = int.MaxValue;
            }
            TotalCells = (uint)tc;

            if (cellsPerAxis < 1)
                Debug.LogWarning($"{nameof(CompactUniformGridParams3D)}: cellsPerAxis must be positive.");
            if (elementCapacityPerCell < 1)
                Debug.LogWarning($"{nameof(CompactUniformGridParams3D)}: elementCapacityPerCell should be >= 1.");
            if (CellSize < 1e-7f || CellSize > 1e3f)
                Debug.LogWarning($"{nameof(CompactUniformGridParams3D)}: CellSize out of usual range. CellSize={CellSize}");
        }

        public override string ToString() {
            var log = new StringBuilder();
            log.AppendLine($"{nameof(CompactUniformGridParams3D)}:");
            log.AppendLine($"  gridCenter={gridCenter}, gridSize={gridSize}, N={cellsPerAxis}, M={elementCapacityPerCell}");
            log.AppendLine($"  GridOffset={GridOffset}, CellSize={CellSize}, TotalCells={TotalCells}");
            return log.ToString();
        }
    }
}
