using System;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// CPU 側の <c>cellHead</c> / <c>cellNext</c>（GPU の <see cref="GPUUniformGrid"/> と同じレイアウト）。
    /// 構築後 <see cref="UploadTo"/> で GPU バッファに転送できる。
    /// </summary>
    public class CPUUniformGrid : IDisposable {
        private const int LOG_LENGTH_LIMIT = 100;
        /// <summary>シェーダの <c>(uint)-1</c> と同じ「無効」値。</summary>
        public const uint InvalidElementId = uint.MaxValue;

        public readonly UniformGridParams gridParams;

        NativeArray<uint> cellHead;
        NativeArray<uint> cellNext;

        /// <summary>セル先頭要素 ID（Morton セルインデックス）。</summary>
        public NativeArray<uint> CellHead => cellHead;
        /// <summary>要素ごとの次 ID（連結リスト）。</summary>
        public NativeArray<uint> CellNext => cellNext;

        public CPUUniformGrid(UniformGridParams gridParams)
            : this(gridParams,
                new NativeArray<uint>((int)gridParams.TotalNumberOfCells, Allocator.Persistent),
                new NativeArray<uint>((int)gridParams.elementCapacity, Allocator.Persistent)) {
        }

        public CPUUniformGrid(UniformGridParams gridParams, NativeArray<uint> cellhead, NativeArray<uint> cellnext) {
            this.gridParams = gridParams;
            cellHead = cellhead;
            cellNext = cellnext;
        }

        #region public interface

        /// <summary>全セル・全要素スロットを <see cref="InvalidElementId"/> で埋める。</summary>
        public void Clear() {
            for (var i = 0; i < cellHead.Length; i++)
                cellHead[i] = InvalidElementId;
            for (var i = 0; i < cellNext.Length; i++)
                cellNext[i] = InvalidElementId;
        }

        /// <summary>ワールド座標からセルインデックス（整数格子）を求め、Morton セル ID を返す。範囲外なら false。</summary>
        public bool TryGetCellId(float3 worldPosition, out uint cellId) {
            cellId = 0;
            float3 cellPosition = (worldPosition - gridParams.GridOffset) / gridParams.CellSize;
            if (!math.all(cellPosition >= 0f))
                return false;
            var limit = (float)gridParams.NumberOfCellsPerAxis;
            if (!math.all(cellPosition < limit))
                return false;
            var cellIndex = new uint3((uint)cellPosition.x, (uint)cellPosition.y, (uint)cellPosition.z);
            cellId = MortonCode.Encode3(cellIndex);
            return true;
        }

        /// <summary>GPU の <c>UniformGrid_InsertElementIDAtCellID</c> と同じ「先頭プッシュ」。</summary>
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

        /// <summary>ワールド座標がグリッド内なら挿入。範囲外・無効 ID は false。</summary>
        public bool TryInsertElementAtPosition(float3 worldPosition, uint elementId) {
            return TryGetCellId(worldPosition, out var cellId) && TryInsertElementAtCellId(cellId, elementId);
        }

        /// <summary><paramref name="positions"/>[i] を要素 ID <paramref name="i"/> として挿入（<paramref name="count"/> 件）。事前に <see cref="Clear"/> すること。</summary>
        public void InsertFromPositions(NativeArray<float3> positions, int count) {
            if (count > positions.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            for (var i = 0; i < count; i++) {
                if (!TryInsertElementAtPosition(positions[i], (uint)i))
                    Debug.LogWarning($"CPUUniformGrid: skipped insert for element {i} (out of grid or invalid).");
            }
        }

        /// <summary><see cref="Clear"/> 後に <see cref="InsertFromPositions"/> する。</summary>
        public void RebuildFromPositions(NativeArray<float3> positions, int count) {
            Clear();
            InsertFromPositions(positions, count);
        }

        /// <summary>構築済みバッファを GPU に書き込む。<paramref name="gpu"/> のパラメータとバッファ長が一致している必要がある。</summary>
        public void UploadTo(GPUUniformGrid gpu) {
            if (gpu == null)
                throw new ArgumentNullException(nameof(gpu));
            gpu.UploadFrom(this);
        }

        #endregion

        #region IDisposable

        public void Dispose() {
            if (cellHead.IsCreated)
                cellHead.Dispose();
            if (cellNext.IsCreated)
                cellNext.Dispose();
        }

        #endregion

        #region object

        public override string ToString() {
            var log = new StringBuilder();

            log.AppendLine($"Cell head: len={cellHead.Length}");
            for (int i = 0; i < cellHead.Length; i++) {
                log.Append($"{(int)cellHead[i]}, ");
                if (i >= LOG_LENGTH_LIMIT) {
                    log.AppendLine("...");
                    break;
                }
            }
            log.AppendLine();

            log.AppendLine($"Cell next: len={cellNext.Length}");
            for (int i = 0; i < cellNext.Length; i++) {
                log.Append($"{(int)cellNext[i]}, ");
                if (i >= LOG_LENGTH_LIMIT) {
                    log.AppendLine("...");
                    break;
                }
            }

            return log.ToString();
        }

        #endregion
    }
}
