#ifndef __CPU_PROXIMITY_DATA_HLSL__
#define __CPU_PROXIMITY_DATA_HLSL__

uint _CpuPointPositions_Length;
StructuredBuffer<float3> _CpuPointPositions;

float3 CpuPointPosition(uint index) {
    return _CpuPointPositions[index];
}

#endif
