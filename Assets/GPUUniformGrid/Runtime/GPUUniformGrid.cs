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
        protected int kernelInitializeCells;
        protected int kernelInitializeElements;

        public GraphicsBuffer cellHead { get; protected set; }
        public GraphicsBuffer cellNext { get; protected set; }

        public GPUUniformGrid(UniformGridParams gridParams) {
            this.gridParams = gridParams;

            this.compute = Resources.Load<ComputeShader>(CS_UNIFORM_GRID);
            this.kernelInitializeCells = compute.FindKernel(K_InitializeCells);
            this.kernelInitializeElements = compute.FindKernel(K_InitializeElements);

            this.cellHead = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)gridParams.TotalNumberOfCells, 4);
            this.cellNext = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)gridParams.elementCapacity, 4);
            Reset();
        }
        public void Reset() {
            ResetCellHeadBuffer();
            ResetCellNextBuffer();
        }
        public void SetParams(int kernel = -1) => SetParams(compute, gridParams, kernel);
        public void SetParamsGlobal() {
            Shader.SetGlobalBuffer(P_UniformGrid_cellHead, cellHead);
            Shader.SetGlobalInteger(P_UniformGrid_nCells, cellHead.count);
            Shader.SetGlobalBuffer(P_UniformGrid_cellNext, cellNext);
            Shader.SetGlobalInteger(P_UniformGrid_nElements, cellNext.count);

            var cellSize = gridParams.CellSize;
            var gridOffset = gridParams.GridOffset;
            Shader.SetGlobalVector(P_UniformGrid_cellOffset, new float4(gridOffset, 0));
            Shader.SetGlobalVector(P_UniformGrid_cellSize, new float4(cellSize));
        }

        #region IDisposable
        public void Dispose() {
            DisposeCellHeadBuffer();
            DisposeCellNextBuffer();
        }
        #endregion

        #region methods
        protected void ResetCellHeadBuffer() {
            if (cellHead == null) {
                Debug.LogWarning("cellHead is null. Please call InitializeGrid first.");
                return;
            }
            SetParams(compute, gridParams, kernelInitializeCells);
            compute.Dispatch(kernelInitializeCells,
                (cellHead.count - 1) / (int)ThreadGroupSize.x + 1, 1, 1);
        }
        protected void ResetCellNextBuffer() {
            if (cellNext == null) {
                Debug.LogWarning("cellNext is null. Please call InitializeElements first.");
                return;
            }
            SetParams(compute, gridParams, kernelInitializeElements);
            compute.Dispatch(kernelInitializeElements,
                (cellNext.count - 1) / (int)ThreadGroupSize.x + 1, 1, 1);
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

        private void SetParams(ComputeShader compute, UniformGridParams gridParams, int kernel = -1) {
            if (cellHead != null) {
                if (kernel >= 0)
                    compute.SetBuffer(kernel, P_UniformGrid_cellHead, cellHead);
                compute.SetInt(P_UniformGrid_nCells, cellHead.count);
            }
            if (cellNext != null) {
                if(kernel >= 0)
                    compute.SetBuffer(kernel, P_UniformGrid_cellNext, cellNext);
                compute.SetInt(P_UniformGrid_nElements, cellNext.count);
            }

            var cellSize = gridParams.CellSize;
            var gridOffset = gridParams.GridOffset;
            compute.SetFloats(P_UniformGrid_cellOffset, gridOffset.x, gridOffset.y, gridOffset.z);
            compute.SetFloats(P_UniformGrid_cellSize, cellSize, cellSize, cellSize);
        }

        #endregion

        #region declarations
        public const string K_InitializeCells = "InitializeCells";
        public const string K_InitializeElements = "InitializeElements";
        private const string CS_UNIFORM_GRID = "Shader/UniformGrid";

        public static readonly uint3 ThreadGroupSize = new uint3(64, 1, 1);

        public static readonly int P_UniformGrid_cellHead = Shader.PropertyToID("UniformGrid_cellHead");
        public static readonly int P_UniformGrid_cellNext = Shader.PropertyToID("UniformGrid_cellNext");
        public static readonly int P_UniformGrid_nCells = Shader.PropertyToID("UniformGrid_nCells");
        public static readonly int P_UniformGrid_nElements = Shader.PropertyToID("UniformGrid_nElements");

        public static readonly int P_UniformGrid_cellOffset = Shader.PropertyToID("UniformGrid_cellOffset");
        public static readonly int P_UniformGrid_cellSize = Shader.PropertyToID("UniformGrid_cellSize");
        public static readonly int P_UniformGrid_CellIDMask = Shader.PropertyToID("UniformGrid_CellIDMask");
        #endregion
    }

}