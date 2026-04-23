using System.Runtime.InteropServices;
using Nobnak.GPU.UniformGrid;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 2D グリッド版: <see cref="CPUUniformGrid2D"/> でランダム点を格納し <see cref="GPUUniformGrid2D.UploadFrom"/> で GPU に載せる。
/// 同一シーンの <see cref="IGPUUniformGridProvider2D"/>（例: <see cref="UniformGridView2D"/> や <see cref="GPUUniformGridBehaviour2D"/>）が <c>SetParamsGlobal</c> する想定（実行順は本コンポーネントを後に）。
/// シェーダ <c>Unlit/CpuGridProximity2D</c> はクエリ位置に <c>worldPosition.xy</c> を使う（論理平面 = ワールド XY の例）。
/// </summary>
[DefaultExecutionOrder(20)]
public class CpuGridProximitySample2D : MonoBehaviour {

    [FormerlySerializedAs("uniformGridView2D")]
    [SerializeField] MonoBehaviour gpuUniformGridProvider;
    [SerializeField] int pointCount = 256;
    [SerializeField] bool regenerateEveryFrame = true;
    [SerializeField] uint randomSeed = 0xC0FFEEu;
    [SerializeField] bool setGlobalPointBuffers = true;

    CPUUniformGrid2D cpuGrid;
    NativeArray<float2> positions;
    GraphicsBuffer positionsBuffer;
    GPUUniformGrid2D boundGpu;
    bool staticLayoutUploaded;

    void OnDisable() {
        DisposeBuffers();
    }

    void Update() {
        var provider = gpuUniformGridProvider as IGPUUniformGridProvider2D;
        var gpu = provider?.Grid;
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
            Shader.SetGlobalInteger(CpuProximityShaderGlobals2D.LengthId, n);
            Shader.SetGlobalBuffer(CpuProximityShaderGlobals2D.BufferId, positionsBuffer);
        }
    }

    void EnsureBuffers(UniformGridParams2D p) {
        int cap = (int)p.elementCapacity;
        if (cpuGrid == null)
            cpuGrid = new CPUUniformGrid2D(p);
        if (!positions.IsCreated || positions.Length != cap) {
            if (positions.IsCreated)
                positions.Dispose();
            positions = new NativeArray<float2>(cap, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
        if (positionsBuffer == null || positionsBuffer.count != cap) {
            positionsBuffer?.Dispose();
            positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cap, Marshal.SizeOf<float2>());
        }
    }

    void FillRandomPositionsInGrid(UniformGridParams2D p, int n) {
        var rng = Unity.Mathematics.Random.CreateFromIndex(randomSeed ^ (uint)(Time.frameCount * 0x9E3779B9u));
        float2 o = p.GridOffset;
        float2 s = p.gridSize;
        for (int i = 0; i < n; i++)
            positions[i] = o + rng.NextFloat2() * s;
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
        if (setGlobalPointBuffers)
            CpuProximityShaderGlobals2D.Clear();
    }
}
