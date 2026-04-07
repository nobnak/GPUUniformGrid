using System;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    public class CPUUniformGrid2D : IDisposable {
        private const int LOG_LENGTH_LIMIT = 100;
        public const uint InvalidElementId = uint.MaxValue;

        public readonly UniformGridParams2D gridParams;

        NativeArray<uint> cellHead;
        NativeArray<uint> cellNext;

        public NativeArray<uint> CellHead => cellHead;
        public NativeArray<uint> CellNext => cellNext;

        public CPUUniformGrid2D(UniformGridParams2D gridParams)
            : this(gridParams,
                new NativeArray<uint>((int)gridParams.TotalNumberOfCells, Allocator.Persistent),
                new NativeArray<uint>((int)gridParams.elementCapacity, Allocator.Persistent)) {
        }

        public CPUUniformGrid2D(UniformGridParams2D gridParams, NativeArray<uint> cellhead, NativeArray<uint> cellnext) {
            this.gridParams = gridParams;
            cellHead = cellhead;
            cellNext = cellnext;
        }

        public void Clear() {
            for (var i = 0; i < cellHead.Length; i++)
                cellHead[i] = InvalidElementId;
            for (var i = 0; i < cellNext.Length; i++)
                cellNext[i] = InvalidElementId;
        }

        public bool TryGetCellId(float2 planePosition, out uint cellId) {
            cellId = 0;
            float2 cellPosition = (planePosition - gridParams.GridOffset) / gridParams.CellSize;
            if (!math.all(cellPosition >= 0f))
                return false;
            var limit = (float)gridParams.NumberOfCellsPerAxis;
            if (!math.all(cellPosition < limit))
                return false;
            var cellIndex = new uint2((uint)cellPosition.x, (uint)cellPosition.y);
            cellId = MortonCode2D.Encode2(cellIndex);
            return true;
        }

        public bool TryInsertElementAtCellId(uint cellId, uint elementId) {
            if (elementId >= (uint)cellNext.Length)
                return false;
            if (cellId >= (uint)cellHead.Length)
                return false;
            var oldHead = cellHead[(int)cellId];
            cellHead[(int)cellId] = elementId;
            cellNext[(int)elementId] = oldHead;
            return true;
        }

        public bool TryInsertElementAtPosition(float2 planePosition, uint elementId) {
            return TryGetCellId(planePosition, out var cellId) && TryInsertElementAtCellId(cellId, elementId);
        }

        public void UploadTo(GPUUniformGrid2D gpu) {
            if (gpu == null)
                throw new ArgumentNullException(nameof(gpu));
            gpu.UploadFrom(this);
        }

        public void Dispose() {
            if (cellHead.IsCreated)
                cellHead.Dispose();
            if (cellNext.IsCreated)
                cellNext.Dispose();
        }

        public override string ToString() {
            var log = new StringBuilder();
            log.AppendLine($"Cell head: len={cellHead.Length}");
            for (int i = 0; i < cellHead.Length && i < LOG_LENGTH_LIMIT; i++)
                log.Append($"{(int)cellHead[i]}, ");
            if (cellHead.Length > LOG_LENGTH_LIMIT)
                log.AppendLine("...");
            log.AppendLine();
            log.AppendLine($"Cell next: len={cellNext.Length}");
            for (int i = 0; i < cellNext.Length && i < LOG_LENGTH_LIMIT; i++)
                log.Append($"{(int)cellNext[i]}, ");
            if (cellNext.Length > LOG_LENGTH_LIMIT)
                log.AppendLine("...");
            return log.ToString();
        }
    }
}
