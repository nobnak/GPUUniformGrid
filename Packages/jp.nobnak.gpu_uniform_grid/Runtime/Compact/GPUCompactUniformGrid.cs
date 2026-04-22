using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nobnak.GPU.UniformGrid {

    public sealed class GPUCompactUniformGrid : IDisposable {

        public readonly CompactUniformGridParams3D gridParams;

        readonly ComputeShader compute;
        readonly int kernelInit;
        readonly int kernelBuild;

        public GraphicsBuffer cellCounts { get; private set; }
        public GraphicsBuffer cellParticles { get; private set; }

        public GPUCompactUniformGrid(CompactUniformGridParams3D gridParams) {
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

        public void DispatchBuild(GraphicsBuffer positions3D, int numParticles) {
            if (positions3D == null)
                throw new ArgumentNullException(nameof(positions3D));
            if (positions3D.stride != sizeof(float) * 3)
                throw new ArgumentException("positions3D must be float3 per element (stride 12).", nameof(positions3D));
            ApplyParams(numParticles);
            compute.SetBuffer(kernelBuild, P_Positions3D, positions3D);
            compute.SetBuffer(kernelBuild, P_CellCounts, cellCounts);
            compute.SetBuffer(kernelBuild, P_CellParticles, cellParticles);
            var groups = UniformGridGpuDispatch.Groups1DCompact(numParticles);
            if (groups > 0)
                compute.Dispatch(kernelBuild, groups, 1, 1);
        }

        void ApplyParams(int numParticles) {
            var p = gridParams;
            var n = (int)p.cellsPerAxis;
            compute.SetInt(P_NumParticles, numParticles);
            compute.SetInt(P_TotalCells, (int)p.TotalCells);
            compute.SetInt(P_Nx, n);
            compute.SetInt(P_Ny, n);
            compute.SetInt(P_Nz, n);
            compute.SetInt(P_M, (int)p.elementCapacityPerCell);
            compute.SetVector(P_GridOffset, new Vector4(p.GridOffset.x, p.GridOffset.y, p.GridOffset.z, 0f));
            compute.SetFloat(P_CellSize, p.CellSize);
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
        const string CS_ResourcePath = "Shader/Compact/CompactUniformGrid";

        public static readonly int P_NumParticles = Shader.PropertyToID("_NumParticles");
        public static readonly int P_TotalCells = Shader.PropertyToID("_TotalCells");
        public static readonly int P_Nx = Shader.PropertyToID("_Nx");
        public static readonly int P_Ny = Shader.PropertyToID("_Ny");
        public static readonly int P_Nz = Shader.PropertyToID("_Nz");
        public static readonly int P_M = Shader.PropertyToID("_M");
        public static readonly int P_GridOffset = Shader.PropertyToID("_GridOffset");
        public static readonly int P_CellSize = Shader.PropertyToID("_CellSize");
        public static readonly int P_Positions3D = Shader.PropertyToID("Positions3D");
        public static readonly int P_CellCounts = Shader.PropertyToID("CellCounts");
        public static readonly int P_CellParticles = Shader.PropertyToID("CellParticles");
    }
}
