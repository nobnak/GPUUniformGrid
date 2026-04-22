using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nobnak.GPU.UniformGrid {

    public sealed class GPUCompactUniformGrid2D : IDisposable {

        public readonly CompactUniformGridParams2D gridParams;

        readonly ComputeShader compute;
        readonly int kernelInit;
        readonly int kernelBuild;

        public GraphicsBuffer cellCounts { get; private set; }
        public GraphicsBuffer cellParticles { get; private set; }

        public GPUCompactUniformGrid2D(CompactUniformGridParams2D gridParams) {
            this.gridParams = gridParams;
            compute = Resources.Load<ComputeShader>(CS_ResourcePath);
            if (compute == null)
                throw new InvalidOperationException(
                    $"ComputeShader not found at Resources/{CS_ResourcePath}.compute");
            kernelInit = compute.FindKernel(K_Init);
            kernelBuild = compute.FindKernel(K_Build);

            ulong tc = gridParams.TotalCells;
            ulong slots = tc * gridParams.elementCapacityPerCell;
            if (tc > (uint)int.MaxValue || slots > (uint)int.MaxValue)
                throw new ArgumentException("Grid dimensions too large for GraphicsBuffer count.");

            cellCounts = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)tc, sizeof(uint));
            cellParticles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)slots, sizeof(uint));
            Reset();
        }

        public void Reset() => DispatchInit();

        public void DispatchInit() {
            ApplyParams(0);
            compute.SetBuffer(kernelInit, P_CellCounts, cellCounts);
            var groups = UniformGridGpuDispatch.Groups1DCompact((int)gridParams.TotalCells);
            if (groups > 0)
                compute.Dispatch(kernelInit, groups, 1, 1);
        }

        public void DispatchBuild(GraphicsBuffer positions2D, int numParticles) {
            if (positions2D == null)
                throw new ArgumentNullException(nameof(positions2D));
            if (positions2D.stride != sizeof(float) * 2)
                throw new ArgumentException("positions2D must be float2 per element (stride 8).", nameof(positions2D));
            ApplyParams(numParticles);
            compute.SetBuffer(kernelBuild, P_Positions2D, positions2D);
            compute.SetBuffer(kernelBuild, P_CellCounts, cellCounts);
            compute.SetBuffer(kernelBuild, P_CellParticles, cellParticles);
            var groups = UniformGridGpuDispatch.Groups1DCompact(numParticles);
            if (groups > 0)
                compute.Dispatch(kernelBuild, groups, 1, 1);
        }

        void ApplyParams(int numParticles) {
            var p = gridParams;
            compute.SetInt(P_NumParticles, numParticles);
            compute.SetInt(P_TotalCells, (int)p.TotalCells);
            compute.SetInt(P_GridWidth, (int)p.gridWidth);
            compute.SetInt(P_GridHeight, (int)p.gridHeight);
            compute.SetInt(P_M, (int)p.elementCapacityPerCell);
            compute.SetVector(P_GridOffset_CellSize, new Vector4(p.GridOffset.x, p.GridOffset.y, p.CellSize.x, p.CellSize.y));
        }

        public void Dispose() {
            if (cellCounts != null) {
                cellCounts.Dispose();
                cellCounts = null;
            }
            if (cellParticles != null) {
                cellParticles.Dispose();
                cellParticles = null;
            }
        }

        public const string K_Init = "CS_Init";
        public const string K_Build = "CS_Build";
        const string CS_ResourcePath = "Shader/Compact/CompactUniformGrid2D";

        public static readonly int P_NumParticles = Shader.PropertyToID("_NumParticles");
        public static readonly int P_TotalCells = Shader.PropertyToID("_TotalCells");
        public static readonly int P_GridWidth = Shader.PropertyToID("_GridWidth");
        public static readonly int P_GridHeight = Shader.PropertyToID("_GridHeight");
        public static readonly int P_M = Shader.PropertyToID("_M");
        public static readonly int P_GridOffset_CellSize = Shader.PropertyToID("_GridOffset_CellSize");
        public static readonly int P_Positions2D = Shader.PropertyToID("Positions2D");
        public static readonly int P_CellCounts = Shader.PropertyToID("CellCounts");
        public static readonly int P_CellParticles = Shader.PropertyToID("CellParticles");
    }
}
