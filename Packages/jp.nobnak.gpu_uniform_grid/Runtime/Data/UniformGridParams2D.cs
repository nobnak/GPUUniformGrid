using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// 2D 均一グリッド（平面座標系）。3D 空間への埋め込み（向き・位置）は利用側が
    /// <see cref="planeZ"/> や座標変換で行う。
    /// </summary>
    public struct UniformGridParams2D {
        /// <summary>Morton uint は各軸 16bit まで。</summary>
        public const uint MaxSupportedBitsPerAxis = 16;

        public readonly float2 gridCenter;
        public readonly float2 gridSize;
        /// <summary>ワールド可視化用: グリッドを XY 平面上に置くときの Z（論理座標は float2 のまま）。</summary>
        public readonly float planeZ;
        public readonly uint bitsPerAxis;
        public readonly uint elementCapacity;

        public readonly float2 GridOffset;
        public readonly float2 CellSize;
        public readonly uint NumberOfCellsPerAxis;
        public readonly uint TotalNumberOfCells;

        public UniformGridParams2D(float2 gridCenter, float2 gridSize, float planeZ, uint bitsPerAxis, uint elementCapacity) {
            this.gridCenter = gridCenter;
            this.gridSize = gridSize;
            this.planeZ = planeZ;
            this.bitsPerAxis = bitsPerAxis;
            this.elementCapacity = elementCapacity;

            GridOffset = gridCenter - gridSize * 0.5f;
            NumberOfCellsPerAxis = GetNumberOfCellsPerAxis(bitsPerAxis);
            TotalNumberOfCells = GetTotalNumberOfCells(bitsPerAxis);
            CellSize = gridSize / NumberOfCellsPerAxis;

            if (math.any(CellSize < 1e-5f) || math.any(CellSize > 1e3f))
                Debug.LogWarning($"UniformGridParams2D: cellSize out of usual range. CellSize={CellSize}");
            if (bitsPerAxis > MaxSupportedBitsPerAxis)
                Debug.LogWarning($"bitsPerAxis exceeds 2D Morton uint limit ({MaxSupportedBitsPerAxis}). bitsPerAxis={bitsPerAxis}");
            if (elementCapacity < 1 || 1e9 < elementCapacity)
                Debug.LogWarning($"elementCapacity is too small or too large. elementCapacity={elementCapacity}");
        }

        #region static
        public static uint GetNumberOfCellsPerAxis(uint bitsPerAxis) => (uint)(1 << (int)bitsPerAxis);
        public static uint GetTotalNumberOfCells(uint bitsPerAxis) {
            var n = GetNumberOfCellsPerAxis(bitsPerAxis);
            return n * n;
        }
        #endregion

        #region object
        public override string ToString() {
            var log = new StringBuilder();
            log.AppendLine($"{nameof(UniformGridParams2D)}:");
            log.AppendLine($"  gridCenter={gridCenter}, gridSize={gridSize}, planeZ={planeZ}");
            log.AppendLine($"  bitsPerAxis={bitsPerAxis}, elementCapacity={elementCapacity}");
            log.AppendLine($"  GridOffset={GridOffset}, CellSize={CellSize}, N={NumberOfCellsPerAxis}, TotalCells={TotalNumberOfCells}");
            return log.ToString();
        }
        #endregion
    }
}
