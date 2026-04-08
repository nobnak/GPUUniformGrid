using Nobnak.GPU.UniformGrid;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ParticleDataUploader : MonoBehaviour {

    [SerializeField] protected Tuner tuner = new();

    public GPUUniformGrid grid { get; set; }

    protected ParticleSystem ps;
    protected ComputeShader cs;
    protected int k_InsertParticle;
    protected uint gs_InsertParticle;

    protected NativeArray<ParticleSystem.Particle> particles;
    protected NativeArray<float3> particlePositions;
    protected GraphicsBuffer particlePositionsBuffer;

    #region unity
    void OnEnable() {
        ps = GetComponent<ParticleSystem>();
        cs = Resources.Load<ComputeShader>(CS);
        if (cs == null) {
            Debug.LogError($"Compute shader '{CS}' not found.");
            return;
        }
        k_InsertParticle = cs.FindKernel(K_InsertParticle);
        cs.GetKernelThreadGroupSizes(k_InsertParticle, out gs_InsertParticle, out _, out _);
    }
    void OnDisable() {
        ClearParticleBuffers();
    }

    void Update() {
        if (ps == null || grid == null || cs == null) return;

        var gridParams = grid.gridParams;
        var gridElementCap = (int)gridParams.elementCapacity;
        var particleCount = ps.particleCount;

        if (particleCount == 0) {
            grid.Reset();
            return;
        }

        if (particleCount > gridElementCap)
            Debug.LogWarning(
                $"Particle count ({particleCount}) exceeds grid elementCapacity ({gridElementCap}). Only the first {gridElementCap} particles are inserted.");

        var activeCount = math.min(particleCount, gridElementCap);
        if (activeCount <= 0) {
            grid.Reset();
            return;
        }

        var bufCap = NextBufferCapacity(particleCount);
        if (particles == default || particles.Length < particleCount
            || !particlePositions.IsCreated || particlePositions.Length < bufCap) {
            ClearParticleBuffers();
            particles = new NativeArray<ParticleSystem.Particle>(particleCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            particlePositions = new NativeArray<float3>(bufCap, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            particlePositionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufCap, Marshal.SizeOf<float3>());
        }

        ps.GetParticles(particles);
        for (var i = 0; i < particleCount; i++)
            particlePositions[i] = particles[i].position;
        particlePositionsBuffer.SetData(particlePositions);

        grid.Reset();
        grid.SetParams(cs, k_InsertParticle);
        SetParams(cs, activeCount);
        cs.Dispatch(k_InsertParticle, (activeCount - 1) / (int)gs_InsertParticle + 1, 1, 1);

        if (tuner.setGlobalParams)
            SetParamsGlobal(activeCount);
    }

    #endregion

    #region methods
    public void SetParams(ComputeShader cs, int positionsLength) {
        cs.SetInt(P_ParticlePositions_Length, positionsLength);
        cs.SetBuffer(k_InsertParticle, P_ParticlePositions, particlePositionsBuffer);
    }
    public void SetParamsGlobal(int positionsLength) {
        Shader.SetGlobalInteger(P_ParticlePositions_Length, positionsLength);
        Shader.SetGlobalBuffer(P_ParticlePositions, particlePositionsBuffer);
    }

    static int NextBufferCapacity(int particleCount) {
        if (particleCount <= 0) return 0;
        var m = math.max(particleCount, 16);
        return (m + 15) / 16 * 16;
    }

    private void ClearParticleBuffers() {
        if (particles != default)
            particles.Dispose();
        if (particlePositions != default)
            particlePositions.Dispose();
        if (particlePositionsBuffer != null) {
            particlePositionsBuffer.Dispose();
            particlePositionsBuffer = null;
        }
    }
    private void DumpParticleBuffer(int elemtCapacity, int particleCount) {
        var log = new StringBuilder();
        log.AppendLine($"Particle Count : {particleCount}");
        log.AppendLine($"Element Capacity : {elemtCapacity}");
        for (var i = 0; i < particlePositionsBuffer.count; i++) {
            log.Append($"{i} : {particlePositions[i]}, ");
        }
        Debug.Log(log);
    }
    #endregion

    #region declarations
    public const string CS = "Shaders/ParticleDataUploader";
    public const string K_InsertParticle = "InsertParticle";

    public static readonly int P_ParticlePositions_Length = Shader.PropertyToID("_ParticlePositions_Length");
    public static readonly int P_ParticlePositions = Shader.PropertyToID("_ParticlePositions");

    [System.Serializable]
    public class Tuner {
        public bool setGlobalParams = true;
    }
    #endregion
}