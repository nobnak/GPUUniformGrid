#ifndef __PARTICLE_DATA_HLSL__
#define __PARTICLE_DATA_HLSL__

uint _ParticlePositions_Length;
StructuredBuffer<float3> _ParticlePositions;

float3 GetParticlePosition(uint index) {
    return _ParticlePositions[index];
}

#endif