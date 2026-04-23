#ifndef __CPU_PROXIMITY_QUERY_2D_HLSL__
#define __CPU_PROXIMITY_QUERY_2D_HLSL__

#include "Assets/Samples/LinkedList2D/Include/CpuProximityData2D.hlsl"
#include "Packages/jp.nobnak.gpu_uniform_grid/ShaderLibrary/LinkedList/UniformGrid2D.hlsl"

void GetNearestPointSqDistanceAtPosition2D(float2 planePos, float searchRadiusWorld, out float minSqDist) {
    minSqDist = 1e38;
    if (_CpuPointPositions2D_Length == 0u)
        return;
    if (!UniformGrid2D_IsValid())
        return;

    float2 cellPosition = UniformGrid2D_GetCellPosition(planePos);
    int2 cellIndex0, cellIndex1;
    UniformGrid2D_GetCellRangeForRadius(cellPosition, searchRadiusWorld, cellIndex0, cellIndex1);

    for (int y = cellIndex0.y; y < cellIndex1.y; y++) {
        for (int x = cellIndex0.x; x < cellIndex1.x; x++) {
            uint2 cellIndex = uint2(x, y);
            uint cellId = MortonCode_Encode2(cellIndex);
            uint elementId = UniformGrid2D_GetHeadElementID(cellId);
            uint l = 0;
            while (elementId != UniformGrid2D_InitValue) {
                if (elementId < _CpuPointPositions2D_Length) {
                    float2 p = CpuPointPosition2D(elementId);
                    float2 d = p - planePos;
                    float sqd = dot(d, d);
                    if (sqd < minSqDist)
                        minSqDist = sqd;
                }
                if (l++ >= UniformGrid2D_cellNext_Len)
                    break;
                elementId = UniformGrid2D_GetNextElementID(elementId);
            }
        }
    }
}

#endif
