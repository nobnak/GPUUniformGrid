using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    public struct UniformGridParams {
        /// <summary>
        /// セル ID は uint の Morton コード（各軸 10bit まで）で保持するため、これを超えると衝突する。
        /// 11bit 以上を扱うには cell ID を uint2（64bit Morton）等に拡張する必要がある。
        /// </summary>
        public const uint MaxSupportedBitsPerAxis = 10;
        public readonly float3 gridCenter;
        public readonly float gridSize;
        public readonly uint bitsPerAxis;
        public readonly uint elementCapacity;

        public readonly float3 GridOffset;
        public readonly float CellSize;
        public readonly uint NumberOfCellsPerAxis;
        public readonly uint TotalNumberOfCells;

        public UniformGridParams(float3 gridCenter, float gridSize, uint bitsPerAxis, uint elementCapacity) {
            this.gridCenter = gridCenter;
            this.gridSize = gridSize;
            this.bitsPerAxis = bitsPerAxis;
            this.elementCapacity = elementCapacity;

            GridOffset = gridCenter - gridSize * 0.5f;
            NumberOfCellsPerAxis = GetNumberofCellsPerAxis(bitsPerAxis);
            TotalNumberOfCells = GetTotalNumberOfCells(bitsPerAxis);
            CellSize = gridSize / NumberOfCellsPerAxis;

            if (CellSize < 1e-3f || 1e3f < CellSize) {
                Debug.LogWarning($"cellSize is too small or too large. cellSize={CellSize}");
            }
            if (bitsPerAxis > MaxSupportedBitsPerAxis) {
                Debug.LogWarning(
                    $"bitsPerAxis exceeds Morton uint limit ({MaxSupportedBitsPerAxis}). bitsPerAxis={bitsPerAxis}");
            }
            if (elementCapacity < 1 || 1e9 < elementCapacity) {
                Debug.LogWarning($"elementCapacity is too small or too large. elementCapacity={elementCapacity}");
            }
        }

        #region static
        public static uint GetNumberofCellsPerAxis(uint bitsPerAxis) => (uint)(1 << (int)bitsPerAxis);
        public static uint GetTotalNumberOfCells(uint bitsPerAxis) { 
            var numberOfCellsPerAxis = GetNumberofCellsPerAxis(bitsPerAxis);
            return numberOfCellsPerAxis* numberOfCellsPerAxis *numberOfCellsPerAxis;
        }
        #endregion

        #region object
        public override string ToString() {
            var log = new StringBuilder();
            log.AppendLine($"{nameof(UniformGridParams)}:");
            log.AppendLine("- Parameters:");
            log.AppendLine($"  - gridCenter={gridCenter}");
            log.AppendLine($"  - gridSize={gridSize}");
            log.AppendLine($"  - bitsPerAxis={bitsPerAxis}");
            log.AppendLine($"  - elementCapacity={elementCapacity}");
            log.AppendLine("- Derived:");
            log.AppendLine($"  - GridOffset={GridOffset}");
            log.AppendLine($"  - CellSize={CellSize}");
            log.AppendLine($"  - NumberOfCellsPerAxis={NumberOfCellsPerAxis}");
            log.AppendLine($"  - TotalNumberOfCells={TotalNumberOfCells}");
            return log.ToString();
        }
        #endregion
    }
}