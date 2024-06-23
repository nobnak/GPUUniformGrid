#ifndef __PARTICLE_DENSITY_HLSL__
#define __PARTICLE_DENSITY_HLSL__

//#pragma target 5.0

#include "Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid.hlsl"

void GetParticleDensity(float3 Position, float Distance, out int Count) {
    float3 cellPosition = UniformGrid_GetCellPosition(Position);
    int3 cellIndex0 = int3(clamp(cellPosition - Distance, 0, UniformGrid_cellCount));
    int3 cellIndex1 = int3(clamp(cellPosition + Distance + 1, 0, UniformGrid_cellCount));
    
    Count = 0;
    #if false
    [branch]
    if (!UniformGrid_IsValid())
        return;
    #endif
    
    for (int z = cellIndex0.z; z < cellIndex1.z; z++) {
        for (int y = cellIndex0.y; y < cellIndex1.y; y++) {
            for (int x = cellIndex0.x; x < cellIndex1.x; x++) {
                uint3 cellIndex = uint3(x, y, z);
                uint cellId = MortonCode_Encode3(cellIndex);
                uint elementId = UniformGrid_GetHeadElementID(cellId);
                for (uint l = 0; elementId != UniformGrid_InitValue && l < UniformGrid_cellNext_Len; l++) {
                        Count++;
                        elementId = UniformGrid_GetNextElementID(elementId);
                    }
            }
        }
    }
}



void GetParticleDensity_float(float3 Position, float Distance, out int Count) {
    GetParticleDensity(Position, Distance, Count);
}

#endif