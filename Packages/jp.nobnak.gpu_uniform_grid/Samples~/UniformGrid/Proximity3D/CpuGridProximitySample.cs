using System.Runtime.InteropServices;
using Nobnak.GPU.UniformGrid;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// <see cref="CPUUniformGrid"/> でランダム点を格納し <see cref="GPUUniformGrid.UploadFrom"/> で GPU に載せるサンプル。
/// 同一シーンの <see cref="UniformGridView"/> がグリッド定数を <c>SetParamsGlobal</c> する想定（実行順はこのコンポーネントを後に）。
/// </summary>
[DefaultExecutionOrder(20)]
public class CpuGridProximitySample : MonoBehaviour {

    [SerializeField] UniformGridView uniformGridView;
    [SerializeField] int pointCount = 256;
    [SerializeField] bool regenerateEveryFrame = true;
    [SerializeField] uint randomSeed = 0xC0FFEEu;
    [SerializeField] bool setGlobalPointBuffers = true;

    CPUUniformGrid cpuGrid;
    NativeArray<float3> positions;
    GraphicsBuffer positionsBuffer;
    GPUUniformGrid boundGpu;
    bool staticLayoutUploaded;

    public static readonly int P_CpuPointPositions_Length = Shader.PropertyToID("_CpuPointPositions_Length");
    public static readonly int P_CpuPointPositions = Shader.PropertyToID("_CpuPointPositions");

    void OnDisable() {
        DisposeBuffers();
    }

    void Update() {
        var gpu = uniformGridView != null ? uniformGridView.ActiveGrid : null;
        if (gpu == null)
            return;

        if (boundGpu != gpu) {
            DisposeBuffers();
            boundGpu = gpu;
        }

        var p = gpu.gridParams;
        int n = math.min(pointCount, (int)p.elementCapacity);
        if (n <= 0)
            return;

        EnsureBuffers(p);

        if (regenerateEveryFrame || !staticLayoutUploaded) {
            FillRandomPositionsInGrid(p, n);
            cpuGrid.RebuildFromPositions(positions, n);
            gpu.UploadFrom(cpuGrid);
            if (setGlobalPointBuffers)
                positionsBuffer.SetData(positions, 0, 0, n);
            staticLayoutUploaded = true;
        }

        if (setGlobalPointBuffers) {
            Shader.SetGlobalInteger(P_CpuPointPositions_Length, n);
            Shader.SetGlobalBuffer(P_CpuPointPositions, positionsBuffer);
        }
    }

    void EnsureBuffers(UniformGridParams p) {
        int cap = (int)p.elementCapacity;
        if (cpuGrid == null)
            cpuGrid = new CPUUniformGrid(p);
        if (!positions.IsCreated || positions.Length != cap) {
            if (positions.IsCreated)
                positions.Dispose();
            positions = new NativeArray<float3>(cap, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
        if (positionsBuffer == null || positionsBuffer.count != cap) {
            positionsBuffer?.Dispose();
            positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cap, Marshal.SizeOf<float3>());
        }
    }

    void FillRandomPositionsInGrid(UniformGridParams p, int n) {
        var rng = Unity.Mathematics.Random.CreateFromIndex(randomSeed ^ (uint)(Time.frameCount * 0x9E3779B9u));
        float3 o = p.GridOffset;
        float s = p.gridSize;
        for (int i = 0; i < n; i++) {
            float3 t = rng.NextFloat3();
            positions[i] = o + t * s;
        }
    }

    void DisposeBuffers() {
        if (cpuGrid != null) {
            cpuGrid.Dispose();
            cpuGrid = null;
        }
        if (positions.IsCreated)
            positions.Dispose();
        if (positionsBuffer != null) {
            positionsBuffer.Dispose();
            positionsBuffer = null;
        }
        boundGpu = null;
        staticLayoutUploaded = false;
    }
}
