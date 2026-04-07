#ifndef __CPU_PROXIMITY_QUERY_HLSL__
#define __CPU_PROXIMITY_QUERY_HLSL__

#include "Assets/Samples/ShaderLibrary/CpuProximityData.hlsl"
#define GET_PARTICLE_POSITION(i) CpuPointPosition(i)
#include "Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/UniformGrid-hl.hlsl"

void GetNearestPointSqDistanceAtPosition(float3 worldPos, float searchRadiusWorld, out float minSqDist) {
    minSqDist = 1e38;
    if (!UniformGrid_IsValid())
        return;

    float3 cellPosition = UniformGrid_GetCellPosition(worldPos);
    float3 cs = UniformGrid_cellSize.xyz;
    // 球 R を必ず覆うセル幅（切り捨て int3 だと欠けるため ceil）。軸ごとに上限はグリッド解像度。
    int3 span = (int3)ceil(searchRadiusWorld / cs);
    uint n = UniformGrid_cellCount;
    span = min(span, int3((int)n, (int)n, (int)n));

    int3 cellIndex0, cellIndex1;
    UniformGrid_GetCellRange(cellPosition, span, cellIndex0, cellIndex1);

    for (int z = cellIndex0.z; z < cellIndex1.z; z++) {
        for (int y = cellIndex0.y; y < cellIndex1.y; y++) {
            for (int x = cellIndex0.x; x < cellIndex1.x; x++) {
                uint3 cellIndex = uint3(x, y, z);
                uint cellId = MortonCode_Encode3(cellIndex);
                uint elementId = UniformGrid_GetHeadElementID(cellId);
                uint l = 0;
                while (elementId != UniformGrid_InitValue) {
                    if (elementId < _CpuPointPositions_Length) {
                        float3 p = GET_PARTICLE_POSITION(elementId);
                        float3 d = p - worldPos;
                        float sqd = dot(d, d);
                        if (sqd < minSqDist)
                            minSqDist = sqd;
                    }
                    if (l++ >= UniformGrid_cellNext_Len)
                        break;
                    elementId = UniformGrid_GetNextElementID(elementId);
                }
            }
        }
    }
}

#endif
