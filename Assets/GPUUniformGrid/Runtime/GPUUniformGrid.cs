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
        public void SetParams(ComputeShader compute, int kernel = -1) {
            compute.SetBuffer(kernel, P_UniformGrid_cellHead, cellHead);
            compute.SetInt(P_UniformGrid_cellHead_Len, cellHead != null ? cellHead.count : 0);
            compute.SetBuffer(kernel, P_UniformGrid_cellNext, cellNext);
            compute.SetInt(P_UniformGrid_cellNext_Len, cellNext != null ? cellNext.count : 0);

            compute.SetVector(P_UniformGrid_cellOffset, new float4(gridParams.GridOffset, 0));
            compute.SetVector(P_UniformGrid_cellSize, new float4(gridParams.CellSize));
            compute.SetInt(P_UniformGrid_cellCount, (int)gridParams.NumberOfCellsPerAxis);

        }
        public void SetParams(MaterialPropertyBlock block) {
            block.SetBuffer(P_UniformGrid_cellHead, cellHead);
            block.SetInteger(P_UniformGrid_cellHead_Len, cellHead != null ? cellHead.count : 0);
            block.SetBuffer(P_UniformGrid_cellNext, cellNext);
            block.SetInteger(P_UniformGrid_cellNext_Len, cellNext != null ? cellNext.count : 0);

            block.SetVector(P_UniformGrid_cellOffset, new float4(gridParams.GridOffset, 0));
            block.SetVector(P_UniformGrid_cellSize, new float4(gridParams.CellSize));
            block.SetInteger(P_UniformGrid_cellCount, (int)gridParams.NumberOfCellsPerAxis);
        }
        public void SetParamsGlobal() {
            Shader.SetGlobalBuffer(P_UniformGrid_cellHead, cellHead);
            Shader.SetGlobalInteger(P_UniformGrid_cellHead_Len, cellHead != null ? cellHead.count : 0);
            Shader.SetGlobalBuffer(P_UniformGrid_cellNext, cellNext);
            Shader.SetGlobalInteger(P_UniformGrid_cellNext_Len, cellNext != null ? cellNext.count : 0);

            Shader.SetGlobalVector(P_UniformGrid_cellOffset, new float4(gridParams.GridOffset, 0));
            Shader.SetGlobalVector(P_UniformGrid_cellSize, new float4(gridParams.CellSize));
            Shader.SetGlobalInteger(P_UniformGrid_cellCount, (int)gridParams.NumberOfCellsPerAxis);
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
            SetParams(compute, kernelInitializeCells);
            compute.Dispatch(kernelInitializeCells,
                (cellHead.count - 1) / (int)ThreadGroupSize.x + 1, 1, 1);
        }
        protected void ResetCellNextBuffer() {
            if (cellNext == null) {
                Debug.LogWarning("cellNext is null. Please call InitializeElements first.");
                return;
            }
            SetParams(compute, kernelInitializeElements);
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

        #endregion

        #region declarations
        public const string K_InitializeCells = "InitializeCells";
        public const string K_InitializeElements = "InitializeElements";
        private const string CS_UNIFORM_GRID = "Shader/UniformGrid";

        public static readonly uint3 ThreadGroupSize = new uint3(64, 1, 1);

        public static readonly int P_UniformGrid_cellHead = Shader.PropertyToID("UniformGrid_cellHead");
        public static readonly int P_UniformGrid_cellNext = Shader.PropertyToID("UniformGrid_cellNext");
        public static readonly int P_UniformGrid_cellHead_Len = Shader.PropertyToID("UniformGrid_cellHead_Len");
        public static readonly int P_UniformGrid_cellNext_Len = Shader.PropertyToID("UniformGrid_cellNext_Len");

        public static readonly int P_UniformGrid_cellOffset = Shader.PropertyToID("UniformGrid_cellOffset");
        public static readonly int P_UniformGrid_cellSize = Shader.PropertyToID("UniformGrid_cellSize");
        public static readonly int P_UniformGrid_cellCount = Shader.PropertyToID("UniformGrid_cellCount");
        #endregion
    }

}