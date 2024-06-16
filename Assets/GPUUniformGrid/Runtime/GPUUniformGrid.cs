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

        GraphicsBuffer cellHead;
        GraphicsBuffer cellNext;

        public GPUUniformGrid(UniformGridParams gridParams) {
            this.gridParams = gridParams;

            this.compute = Resources.Load<ComputeShader>(CS_UNIFORM_GRID);
            this.kernelInitializeCells = compute.FindKernel(K_InitializeCells);
            this.kernelInitializeElements = compute.FindKernel(K_InitializeElements);

            InitializeGrid(gridParams);
            InitializeElements(gridParams);
        }
        public void Reset() {
            ResetCellHeadBuffer();
            ResetCellNextBuffer();
        }

        public IEnumerable ToCPU(System.Action<CPUUniformGrid> callback) {
            var naCellHead = new NativeArray<uint>(cellHead.count, Allocator.Persistent);
            var naCellNext = new NativeArray<uint>(cellNext.count, Allocator.Persistent);
            var reqCellHead = AsyncGPUReadback.RequestIntoNativeArray(ref naCellHead, cellHead);
            var reqCellNext = AsyncGPUReadback.RequestIntoNativeArray(ref naCellNext, cellNext);
            while (true) {
                yield return null;

                reqCellHead.Update();
                reqCellNext.Update();

                if (reqCellHead.hasError) {
                    Debug.LogError($"GPU readback error detected.");
                    yield break;
                }
                if (reqCellNext.hasError) {
                    Debug.LogError($"GPU readback error detected.");
                    yield break;
                }

                if (reqCellHead.done && reqCellNext.done) {
                    callback?.Invoke(new CPUUniformGrid(naCellHead, naCellNext));
                    yield break;
                }
            }
        }


        #region IDisposable
        public void Dispose() {
            DisposeCellHeadBuffer();
            DisposeCellNextBuffer();
        }
        #endregion

        #region methods
        protected void InitializeGrid(UniformGridParams gridParams) {
            DisposeCellHeadBuffer();
            cellHead = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)gridParams.TotalNumberOfCells, 4);
            ResetCellHeadBuffer();
        }
        protected void InitializeElements(UniformGridParams gridParams) {
            DisposeCellNextBuffer();
            cellNext = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)gridParams.elementCapacity, 4);
            ResetCellNextBuffer();
        }
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

            var CellSize = gridParams.cellSize;
            compute.SetVector(P_UniformGrid_cellOffset, new float4(gridParams.gridOffset, 0));
            compute.SetVector(P_UniformGrid_cellSize, new float4(CellSize, CellSize, CellSize, 0));
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