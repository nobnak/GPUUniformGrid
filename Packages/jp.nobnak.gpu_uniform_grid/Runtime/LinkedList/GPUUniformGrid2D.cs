using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nobnak.GPU.UniformGrid {

    public class GPUUniformGrid2D : IDisposable {

        public readonly UniformGridParams2D gridParams;

        protected ComputeShader compute;
        protected int kernelInitializeGrid;

        public GraphicsBuffer cellHead { get; protected set; }
        public GraphicsBuffer cellNext { get; protected set; }

        public GPUUniformGrid2D(UniformGridParams2D gridParams) {
            this.gridParams = gridParams;
            compute = Resources.Load<ComputeShader>(CS_UNIFORM_GRID_2D);
            if (compute == null)
                throw new InvalidOperationException(
                    $"Compute shader '{CS_UNIFORM_GRID_2D}' not found. Expected Resources/Shader/LinkedList/UniformGrid2D.compute.");
            kernelInitializeGrid = compute.FindKernel(K_InitializeGrid);
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
            cellHead = h;
            cellNext = n;
            Reset();
        }

        public void Reset() => DispatchInitializeGrid();

        public void SetParams(ComputeShader target, int kernel = -1) {
            target.SetInt(P_cellHead_Len, cellHead != null ? cellHead.count : 0);
            target.SetInt(P_cellNext_Len, cellNext != null ? cellNext.count : 0);
            target.SetBuffer(kernel, P_cellHead_rw, cellHead);
            target.SetBuffer(kernel, P_cellNext_rw, cellNext);
            target.SetBuffer(kernel, P_cellHead_r, cellHead);
            target.SetBuffer(kernel, P_cellNext_r, cellNext);
            var p = gridParams;
            target.SetVector(P_cellOffset, new float4(p.GridOffset.x, p.GridOffset.y, p.planeZ, 0f));
            target.SetVector(P_cellSize, new float4(p.CellSize.x, p.CellSize.y, 0f, 0f));
            target.SetInt(P_cellCount, (int)p.NumberOfCellsPerAxis);
        }

        public void SetParams(MaterialPropertyBlock block) {
            block.SetInteger(P_cellHead_Len, cellHead != null ? cellHead.count : 0);
            block.SetInteger(P_cellNext_Len, cellNext != null ? cellNext.count : 0);
            block.SetBuffer(P_cellHead_rw, cellHead);
            block.SetBuffer(P_cellNext_rw, cellNext);
            block.SetBuffer(P_cellHead_r, cellHead);
            block.SetBuffer(P_cellNext_r, cellNext);
            var p = gridParams;
            block.SetVector(P_cellOffset, new float4(p.GridOffset.x, p.GridOffset.y, p.planeZ, 0f));
            block.SetVector(P_cellSize, new float4(p.CellSize.x, p.CellSize.y, 0f, 0f));
            block.SetInteger(P_cellCount, (int)p.NumberOfCellsPerAxis);
        }

        public void SetParamsGlobal() {
            Shader.SetGlobalInteger(P_cellHead_Len, cellHead != null ? cellHead.count : 0);
            Shader.SetGlobalInteger(P_cellNext_Len, cellNext != null ? cellNext.count : 0);
            Shader.SetGlobalBuffer(P_cellHead_rw, cellHead);
            Shader.SetGlobalBuffer(P_cellNext_rw, cellNext);
            Shader.SetGlobalBuffer(P_cellHead_r, cellHead);
            Shader.SetGlobalBuffer(P_cellNext_r, cellNext);
            var p = gridParams;
            Shader.SetGlobalVector(P_cellOffset, new float4(p.GridOffset.x, p.GridOffset.y, p.planeZ, 0f));
            Shader.SetGlobalVector(P_cellSize, new float4(p.CellSize.x, p.CellSize.y, 0f, 0f));
            Shader.SetGlobalInteger(P_cellCount, (int)p.NumberOfCellsPerAxis);
        }

        /// <summary>Edit モード等で <see cref="SetParamsGlobal"/> が呼ばれないときの残留バインドを外す。</summary>
        public static void ClearParamsGlobal() {
            Shader.SetGlobalInteger(P_cellHead_Len, 0);
            Shader.SetGlobalInteger(P_cellNext_Len, 0);
            Shader.SetGlobalBuffer(P_cellHead_rw, (GraphicsBuffer)null);
            Shader.SetGlobalBuffer(P_cellNext_rw, (GraphicsBuffer)null);
            Shader.SetGlobalBuffer(P_cellHead_r, (GraphicsBuffer)null);
            Shader.SetGlobalBuffer(P_cellNext_r, (GraphicsBuffer)null);
            Shader.SetGlobalVector(P_cellOffset, Vector4.zero);
            Shader.SetGlobalVector(P_cellSize, Vector4.zero);
            Shader.SetGlobalInteger(P_cellCount, 0);
        }

        public void UploadFrom(CPUUniformGrid2D cpu) {
            if (cpu == null)
                throw new ArgumentNullException(nameof(cpu));
            if (cellHead == null || cellNext == null)
                throw new InvalidOperationException("GPU buffers are not allocated.");
            if (!SameGridParams(gridParams, cpu.gridParams))
                throw new ArgumentException("UniformGridParams2D must match between CPU and GPU grid.");
            if (cellHead.count != cpu.CellHead.Length || cellNext.count != cpu.CellNext.Length)
                throw new ArgumentException("Buffer lengths must match between CPU and GPU grid.");
            cellHead.SetData(cpu.CellHead);
            cellNext.SetData(cpu.CellNext);
        }

        #region IDisposable

        public void Dispose() {
            if (cellHead != null) {
                cellHead.Dispose();
                cellHead = null;
            }
            if (cellNext != null) {
                cellNext.Dispose();
                cellNext = null;
            }
        }

        #endregion

        protected void DispatchInitializeGrid() {
            if (cellHead == null || cellNext == null)
                return;
            SetParams(compute, kernelInitializeGrid);
            var total = cellHead.count + cellNext.count;
            var groups = UniformGridGpuDispatch.Groups1D(total);
            if (groups <= 0)
                return;
            compute.Dispatch(kernelInitializeGrid, groups, 1, 1);
        }

        static bool SameGridParams(UniformGridParams2D a, UniformGridParams2D b) {
            return a.bitsPerAxis == b.bitsPerAxis
                && a.elementCapacity == b.elementCapacity
                && math.all(a.gridCenter == b.gridCenter)
                && math.all(a.gridSize == b.gridSize)
                && math.abs(a.planeZ - b.planeZ) <= 1e-6f;
        }

        #region declarations

        public const string K_InitializeGrid = "InitializeGrid";
        const string CS_UNIFORM_GRID_2D = "Shader/LinkedList/UniformGrid2D";

        public static readonly int P_cellHead_Len = Shader.PropertyToID("UniformGrid2D_cellHead_Len");
        public static readonly int P_cellNext_Len = Shader.PropertyToID("UniformGrid2D_cellNext_Len");
        public static readonly int P_cellHead_rw = Shader.PropertyToID("UniformGrid2D_cellHead_rw");
        public static readonly int P_cellNext_rw = Shader.PropertyToID("UniformGrid2D_cellNext_rw");
        public static readonly int P_cellHead_r = Shader.PropertyToID("UniformGrid2D_cellHead_r");
        public static readonly int P_cellNext_r = Shader.PropertyToID("UniformGrid2D_cellNext_r");
        public static readonly int P_cellOffset = Shader.PropertyToID("UniformGrid2D_cellOffset");
        public static readonly int P_cellSize = Shader.PropertyToID("UniformGrid2D_cellSize");
        public static readonly int P_cellCount = Shader.PropertyToID("UniformGrid2D_cellCount");

        #endregion
    }
}
