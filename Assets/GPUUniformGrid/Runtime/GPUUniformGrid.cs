using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nobnak.GPU.UniformGrid {

    public class GPUUniformGrid : System.IDisposable {

        public float3 GridOffset { get; protected set; }
        public float CellSize { get; protected set; }
        public uint BitsPerAxis { get; protected set; }
        public uint TotalNumberOfCells { get; protected set; }
        public uint ElementCapacity { get; protected set; }

        protected ComputeShader compute;
        protected int kernelInitializeCells;
        protected int kernelInitializeElements;

        GraphicsBuffer cellHead;
        GraphicsBuffer cellNext;

        public GPUUniformGrid() {
            this.compute = Resources.Load<ComputeShader>(CS_UNIFORM_GRID);
            this.kernelInitializeCells = compute.FindKernel(K_InitializeCells);
            this.kernelInitializeElements = compute.FindKernel(K_InitializeElements);
        }

        public void InitializeGrid(float3 gridOffset, float cellSize, uint bitsPerAxis) {
            DisposeCellHeadBuffer();

            this.GridOffset = gridOffset;
            this.CellSize = cellSize;
            this.BitsPerAxis = bitsPerAxis;

            if (cellSize < 1e-3f || 1e3f < cellSize) {
                Debug.LogWarning($"cellSize is too small or too large. cellSize={cellSize}");
                return;
            }
            if (bitsPerAxis < 1 || 10 < bitsPerAxis) {
                Debug.LogWarning($"bitsPerAxis is too small or too large. bitsPerAxis={bitsPerAxis}");
                return;
            }

            var nCellsPerAxis = 1 << (int)bitsPerAxis;
            TotalNumberOfCells = (uint)(nCellsPerAxis * nCellsPerAxis * nCellsPerAxis);
            cellHead = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)TotalNumberOfCells, 4);
            ResetCellHeadBuffer();
        }
        public void InitializeElements(uint elementCapacity) {
            DisposeCellNextBuffer();

            if (elementCapacity < 1 || 1e9 < elementCapacity) {
                Debug.LogWarning($"elementCapacity is too small or too large. elementCapacity={elementCapacity}");
                return;
            }

            this.ElementCapacity = elementCapacity;
            cellNext = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)ElementCapacity, 4);
            ResetCellNextBuffer();
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

        private void SetParams(ComputeShader compute, int kernel = -1) {
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

            compute.SetVector(P_UniformGrid_cellOffset, new float4(GridOffset, 0));
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