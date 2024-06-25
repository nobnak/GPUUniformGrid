#ifndef __PARTICLE_DENSITY_HLSL__
#define __PARTICLE_DENSITY_HLSL__

//#pragma target 5.0

#include "Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid.hlsl"
#include "Assets/Samples/ShaderLibrary/ParticleData.hlsl"

void GetParticleDensity(float3 Position, float Distance, int limit, out float4 Count) {
    float3 cellPosition = UniformGrid_GetCellPosition(Position);
    int3 cellSpan = clamp(int3(Distance / UniformGrid_cellSize.xyz), 0, limit);
    int3 cellIndex0 = int3(clamp(cellPosition - cellSpan, 0, UniformGrid_cellCount));
    int3 cellIndex1 = int3(clamp(cellPosition + cellSpan + 1, 0, UniformGrid_cellCount));
    
    Count = 0;
    
    for (int z = cellIndex0.z; z < cellIndex1.z; z++) {
        for (int y = cellIndex0.y; y < cellIndex1.y; y++) {
            for (int x = cellIndex0.x; x < cellIndex1.x; x++) {
                uint3 cellIndex = uint3(x, y, z);
                uint cellId = MortonCode_Encode3(cellIndex);
                uint elementId = UniformGrid_GetHeadElementID(cellId);                
                uint l = 0;
                while (elementId != UniformGrid_InitValue) {
                    float3 p = _ParticlePositions[elementId];
                    float3 d = p - Position;
                    float sqd = dot(d, d);
                    float w = saturate(1.0 - sqd / (Distance * Distance));
                    if (1e-3 < sqd && w > 0) {
                        Count.x++;
                        Count.w += w;
                    }
                    if (l++ >= UniformGrid_cellNext_Len)
                        break;
                    elementId = UniformGrid_GetNextElementID(elementId);
                }
            }
        }
    }
}
void GetParticleDensity(float3 Position, float Distance, out float4 Count) {
    GetParticleDensity(Position, Distance, 1, Count);
}


void GetParticleDensity_float(float3 Position, float Distance, int Limit, out float4 Count) {
    GetParticleDensity(Position, Distance, Limit, Count);
}

#endif