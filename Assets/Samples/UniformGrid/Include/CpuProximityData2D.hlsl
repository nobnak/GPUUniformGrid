#ifndef __CPU_PROXIMITY_DATA_2D_HLSL__
#define __CPU_PROXIMITY_DATA_2D_HLSL__

uint _CpuPointPositions2D_Length;
StructuredBuffer<float2> _CpuPointPositions2D;

float2 CpuPointPosition2D(uint index) {
    return _CpuPointPositions2D[index];
}

#endif
