using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// コンパクト格納グリッド（2D）。座標は <see cref="UniformGridParams2D"/> と同じ矩形（<see cref="GridOffset"/>・<see cref="gridSize"/>）。
    /// </summary>
    public struct CompactUniformGridParams2D {
        public readonly float2 gridCenter;
        public readonly float2 gridSize;
        public readonly float planeZ;
        public readonly uint gridWidth;
        public readonly uint gridHeight;
        public readonly uint elementCapacityPerCell;

        public readonly float2 GridOffset;
        public readonly float2 CellSize;
        public readonly uint TotalCells;

        public CompactUniformGridParams2D(
            float2 gridCenter,
            float2 gridSize,
            float planeZ,
            uint gridWidth,
            uint gridHeight,
            uint elementCapacityPerCell) {
            this.gridCenter = gridCenter;
            this.gridSize = gridSize;
            this.planeZ = planeZ;
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            this.elementCapacityPerCell = elementCapacityPerCell;

            GridOffset = gridCenter - gridSize * 0.5f;
            CellSize = gridSize / new float2(gridWidth, gridHeight);
            TotalCells = gridWidth * gridHeight;

            if (gridWidth < 1 || gridHeight < 1)
                Debug.LogWarning($"{nameof(CompactUniformGridParams2D)}: grid dimensions must be positive.");
            if (elementCapacityPerCell < 1)
                Debug.LogWarning($"{nameof(CompactUniformGridParams2D)}: elementCapacityPerCell should be >= 1.");
            if (math.any(CellSize < 1e-7f) || math.any(CellSize > 1e3f))
                Debug.LogWarning($"{nameof(CompactUniformGridParams2D)}: CellSize out of usual range. CellSize={CellSize}");
        }

        public override string ToString() {
            var log = new StringBuilder();
            log.AppendLine($"{nameof(CompactUniformGridParams2D)}:");
            log.AppendLine($"  gridCenter={gridCenter}, gridSize={gridSize}, planeZ={planeZ}");
            log.AppendLine($"  gridWidth={gridWidth}, gridHeight={gridHeight}, M={elementCapacityPerCell}");
            log.AppendLine($"  GridOffset={GridOffset}, CellSize={CellSize}, TotalCells={TotalCells}");
            return log.ToString();
        }
    }
}
