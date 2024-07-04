#ifndef __UNIFORM_GRID_HL_HLSL__
#define __UNIFORM_GRID_HL_HLSL__

#include "UniformGrid.hlsl"

#ifndef GET_PARTICLE_POSITION
#define GET_PARTICLE_POSITION(i) 0;
#endif

void GetParticleDensityAtPosition(float3 Position, float Distance, int limit, out float4 Count) {
    if (!UniformGrid_IsValid())
        return;
    
    float3 cellPosition = UniformGrid_GetCellPosition(Position);
    int3 cellIndex0, cellIndex1;
    UniformGrid_GetCellRange(cellPosition, Distance, limit, cellIndex0, cellIndex1);
    
    Count = 0;
    
    for (int z = cellIndex0.z; z < cellIndex1.z; z++) {
        for (int y = cellIndex0.y; y < cellIndex1.y; y++) {
            for (int x = cellIndex0.x; x < cellIndex1.x; x++) {
                uint3 cellIndex = uint3(x, y, z);
                uint cellId = MortonCode_Encode3(cellIndex);
                uint elementId = UniformGrid_GetHeadElementID(cellId);
                uint l = 0;
                while (elementId != UniformGrid_InitValue) {
                    float3 p = GET_PARTICLE_POSITION(elementId);
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
void InsertElementIdAtPosition(float3 position, uint elementId) {
    float3 cellPosition = UniformGrid_GetCellPosition(position);
    if (!UniformGrid_IsCellPositionValid(cellPosition))
        return;

    uint cellID = MortonCode_Encode3(uint3(cellPosition));
    UniformGrid_InsertElementIDAtCellID(cellID, elementId);
}

#endif