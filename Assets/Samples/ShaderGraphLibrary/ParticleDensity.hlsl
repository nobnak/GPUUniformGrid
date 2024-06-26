#ifndef __PARTICLE_DENSITY_HLSL__
#define __PARTICLE_DENSITY_HLSL__

//#pragma target 5.0

#include "Assets/Samples/ShaderLibrary/ParticleData.hlsl"
#define GET_PARTICLE_POSITION(i) GetParticlePosition(i)
#include "Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid-hl.hlsl"

void GetParticleDensity_float(float3 Position, float Distance, int Limit, out float4 Count) {
    GetParticleDensityAtPosition(Position, Distance, Limit, Count);
}

#endif