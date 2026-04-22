using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
namespace Nobnak.GPU.UniformGrid {

    public class GPUUniformGrid : System.IDisposable {

        public readonly UniformGridParams gridParams;

        protected ComputeShader compute;
        protected int kernelInitializeGrid;

        public GraphicsBuffer cellHead { get; protected set; }
        public GraphicsBuffer cellNext { get; protected set; }

        public GPUUniformGrid(UniformGridParams gridParams) {
            this.gridParams = gridParams;

            this.compute = Resources.Load<ComputeShader>(CS_UNIFORM_GRID);
            if (compute == null)
                throw new System.InvalidOperationException(
                    $"Compute shader '{CS_UNIFORM_GRID}' not found. Expected under Resources/Shader/LinkedList/UniformGrid.compute.");
            this.kernelInitializeGrid = compute.FindKernel(K_InitializeGrid);

            GraphicsBuffer h = null, n = null;
            try {
                h = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)gridParams.TotalNumberOfCells, 4);
                n = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)gridParams.elementCapacity, 4);
            } catch {
                if (h != null)
                    h.Dispose();
                if (n != null)
                    n.Dispose();
                throw;
            }
            this.cellHead = h;
            this.cellNext = n;
            Reset();
        }
        public void Reset() {
            DispatchInitializeGrid();
        }
        public void SetParams(ComputeShader compute, int kernel = -1) {
            compute.SetInt(P_UniformGrid_cellHead_Len, cellHead != null ? cellHead.count : 0);
            compute.SetInt(P_UniformGrid_cellNext_Len, cellNext != null ? cellNext.count : 0);
            compute.SetBuffer(kernel, P_UniformGrid_cellHead_rw, cellHead);
            compute.SetBuffer(kernel, P_UniformGrid_cellNext_rw, cellNext);
            compute.SetBuffer(kernel, P_UniformGrid_cellHead_r, cellHead);
            compute.SetBuffer(kernel, P_UniformGrid_cellNext_r, cellNext);

            compute.SetVector(P_UniformGrid_cellOffset, new float4(gridParams.GridOffset, 0));
            compute.SetVector(P_UniformGrid_cellSize, new float4(gridParams.CellSize));
            compute.SetInt(P_UniformGrid_cellCount, (int)gridParams.NumberOfCellsPerAxis);

        }
        public void SetParams(MaterialPropertyBlock block) {
            block.SetInteger(P_UniformGrid_cellHead_Len, cellHead != null ? cellHead.count : 0);
            block.SetInteger(P_UniformGrid_cellNext_Len, cellNext != null ? cellNext.count : 0);
            block.SetBuffer(P_UniformGrid_cellHead_rw, cellHead);
            block.SetBuffer(P_UniformGrid_cellNext_rw, cellNext);
            block.SetBuffer(P_UniformGrid_cellHead_r, cellHead);
            block.SetBuffer(P_UniformGrid_cellNext_r, cellNext);

            block.SetVector(P_UniformGrid_cellOffset, new float4(gridParams.GridOffset, 0));
            block.SetVector(P_UniformGrid_cellSize, new float4(gridParams.CellSize));
            block.SetInteger(P_UniformGrid_cellCount, (int)gridParams.NumberOfCellsPerAxis);
        }
        public void SetParamsGlobal() {
            Shader.SetGlobalInteger(P_UniformGrid_cellHead_Len, cellHead != null ? cellHead.count : 0);
            Shader.SetGlobalInteger(P_UniformGrid_cellNext_Len, cellNext != null ? cellNext.count : 0);
            Shader.SetGlobalBuffer(P_UniformGrid_cellHead_rw, cellHead);
            Shader.SetGlobalBuffer(P_UniformGrid_cellNext_rw, cellNext);
            Shader.SetGlobalBuffer(P_UniformGrid_cellHead_r, cellHead);
            Shader.SetGlobalBuffer(P_UniformGrid_cellNext_r, cellNext);

            Shader.SetGlobalVector(P_UniformGrid_cellOffset, new float4(gridParams.GridOffset, 0));
            Shader.SetGlobalVector(P_UniformGrid_cellSize, new float4(gridParams.CellSize));
            Shader.SetGlobalInteger(P_UniformGrid_cellCount, (int)gridParams.NumberOfCellsPerAxis);
        }

        /// <summary>
        /// <see cref="CPUUniformGrid"/> で構築した連結リストをそのまま GPU バッファへ転送する。
        /// </summary>
        public void UploadFrom(CPUUniformGrid cpu) {
            if (cpu == null)
                throw new ArgumentNullException(nameof(cpu));
            if (cellHead == null || cellNext == null)
                throw new InvalidOperationException("GPU buffers are not allocated.");
            if (!SameGridParams(gridParams, cpu.gridParams))
                throw new ArgumentException("UniformGridParams must match between CPU and GPU grid.");
            if (cellHead.count != cpu.CellHead.Length || cellNext.count != cpu.CellNext.Length)
                throw new ArgumentException("Buffer lengths must match between CPU and GPU grid.");
            cellHead.SetData(cpu.CellHead);
            cellNext.SetData(cpu.CellNext);
        }

        #region IDisposable
        public void Dispose() {
            DisposeCellHeadBuffer();
            DisposeCellNextBuffer();
        }
        #endregion

        #region methods
        protected void DispatchInitializeGrid() {
            if (cellHead == null) {
                Debug.LogWarning("cellHead is null.");
                return;
            }
            if (cellNext == null) {
                Debug.LogWarning("cellNext is null.");
                return;
            }
            SetParams(compute, kernelInitializeGrid);
            var total = cellHead.count + cellNext.count;
            if (total <= 0)
                return;
            compute.Dispatch(kernelInitializeGrid,
                UniformGridGpuDispatch.Groups1D(total), 1, 1);
        }
        protected void DisposeCellHeadBuffer() {
            if (cellHead != null) {
                cellHead.Dispose();
                cellHead = null;
            }
        }
        protected void DisposeCellNextBuffer() {
            if (cellNext == null) {
                return;
            }
            cellNext.Dispose();
            cellNext = null;
        }

        static bool SameGridParams(UniformGridParams a, UniformGridParams b) {
            return a.bitsPerAxis == b.bitsPerAxis
                && a.elementCapacity == b.elementCapacity
                && math.all(a.gridCenter == b.gridCenter)
                && math.abs(a.gridSize - b.gridSize) <= 1e-6f;
        }

        #endregion

        #region declarations
        public const string K_InitializeGrid = "InitializeGrid";
        private const string CS_UNIFORM_GRID = "Shader/LinkedList/UniformGrid";

        public static readonly uint3 ThreadGroupSize = new uint3(64, 1, 1);

        public static readonly int P_UniformGrid_cellHead_Len = Shader.PropertyToID("UniformGrid_cellHead_Len");
        public static readonly int P_UniformGrid_cellNext_Len = Shader.PropertyToID("UniformGrid_cellNext_Len");
        public static readonly int P_UniformGrid_cellHead_rw = Shader.PropertyToID("UniformGrid_cellHead_rw");
        public static readonly int P_UniformGrid_cellNext_rw = Shader.PropertyToID("UniformGrid_cellNext_rw");
        public static readonly int P_UniformGrid_cellHead_r = Shader.PropertyToID("UniformGrid_cellHead_r");
        public static readonly int P_UniformGrid_cellNext_r = Shader.PropertyToID("UniformGrid_cellNext_r");

        public static readonly int P_UniformGrid_cellOffset = Shader.PropertyToID("UniformGrid_cellOffset");
        public static readonly int P_UniformGrid_cellSize = Shader.PropertyToID("UniformGrid_cellSize");
        public static readonly int P_UniformGrid_cellCount = Shader.PropertyToID("UniformGrid_cellCount");
        #endregion
    }

}