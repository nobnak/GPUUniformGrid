using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    public struct UniformGridParams {
        public readonly float3 gridOffset;
        public readonly float gridSize;
        public readonly uint bitsPerAxis;
        public readonly uint elementCapacity;

        public readonly float CellSize;
        public readonly uint NumberOfCellsPerAxis;
        public readonly uint TotalNumberOfCells;

        public UniformGridParams(float3 gridOffset, float gridSize, uint bitsPerAxis, uint elementCapacity) {
            this.gridOffset = gridOffset;
            this.gridSize = gridSize;
            this.bitsPerAxis = bitsPerAxis;
            this.elementCapacity = elementCapacity;

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
    }
}