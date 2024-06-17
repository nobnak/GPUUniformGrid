using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    public struct UniformGridParams {
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
            if (bitsPerAxis < 1 || 10 < bitsPerAxis) {
                Debug.LogWarning($"bitsPerAxis is too small or too large. bitsPerAxis={bitsPerAxis}");
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
            return log.ToString();
        }
        #endregion
    }
}