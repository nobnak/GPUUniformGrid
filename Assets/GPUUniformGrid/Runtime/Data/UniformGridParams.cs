using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    public struct UniformGridParams {
        public readonly float3 gridOffset;
        public readonly float cellSize;
        public readonly uint bitsPerAxis;
        public readonly uint elementCapacity;

        public uint NumberOfCellsPerAxis => (uint)(1 << (int)bitsPerAxis);
        public uint TotalNumberOfCells => NumberOfCellsPerAxis * NumberOfCellsPerAxis * NumberOfCellsPerAxis;

        public UniformGridParams(float3 gridOffset, float cellSize, uint bitsPerAxis, uint elementCapacity) {
            this.gridOffset = gridOffset;
            this.cellSize = cellSize;
            this.bitsPerAxis = bitsPerAxis;
            this.elementCapacity = elementCapacity;

            if (cellSize < 1e-3f || 1e3f < cellSize) {
                Debug.LogWarning($"cellSize is too small or too large. cellSize={cellSize}");
            }
            if (bitsPerAxis < 1 || 10 < bitsPerAxis) {
                Debug.LogWarning($"bitsPerAxis is too small or too large. bitsPerAxis={bitsPerAxis}");
            }
            if (elementCapacity < 1 || 1e9 < elementCapacity) {
                Debug.LogWarning($"elementCapacity is too small or too large. elementCapacity={elementCapacity}");
            }
        }
    }
}