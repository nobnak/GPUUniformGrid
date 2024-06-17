using Nobnak.GPU.UniformGrid;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ParticleDataUploader : MonoBehaviour {

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
        k_InsertParticle = cs.FindKernel(K_InsertParticle);
        cs.GetKernelThreadGroupSizes(k_InsertParticle, out gs_InsertParticle, out _, out _);
    }
    void OnDisable() {
        ClearParticleBuffers();
    }

    void Update() {
        if (ps == null || grid == null) return;

        var gridParams = grid.gridParams;
        var elemtCapacity = (int)gridParams.elementCapacity;
        var particleCount = ps.particleCount;

        if (particleCount == 0) {
            grid.Reset();
            return;
        }

        if (particles == default || particles.Length != elemtCapacity) {
            ClearParticleBuffers();
            particles = new NativeArray<ParticleSystem.Particle>(particleCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            particlePositions = new NativeArray<float3>(elemtCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            particlePositionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, elemtCapacity, Marshal.SizeOf<float3>());
        }
        ps.GetParticles(particles);
        for (var i = 0; i < particleCount; i++) {
            particlePositions[i] = particles[i].position;
        }
        particlePositionsBuffer.SetData(particlePositions);

        //DumpParticleBuffer(elemtCapacity, particleCount);
        //Debug.Log($"{grid.gridParams}");

        grid.Reset();
        grid.SetParamsGlobal();
        cs.SetInt(P_ParticlePositions_Length, particleCount);
        cs.SetBuffer(k_InsertParticle, P_ParticlePositions, particlePositionsBuffer);
        cs.Dispatch(k_InsertParticle, (particleCount - 1) / (int)gs_InsertParticle + 1, 1, 1);
    }
    #endregion

    #region methods
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
    public const string CS = "Shaders/ParticeOnGrid";
    public const string K_InsertParticle = "InsertParticle";

    public static readonly int P_ParticlePositions_Length = Shader.PropertyToID("_ParticlePositions_Length");
    public static readonly int P_ParticlePositions = Shader.PropertyToID("_ParticlePositions");
    #endregion
}