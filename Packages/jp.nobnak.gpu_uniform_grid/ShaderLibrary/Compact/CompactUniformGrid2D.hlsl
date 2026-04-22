#ifndef __COMPACT_UNIFORM_GRID_2D_HLSL__
#define __COMPACT_UNIFORM_GRID_2D_HLSL__

cbuffer CompactUniformGrid2D_Params {
    uint _NumParticles;
    uint _TotalCells;
    uint _GridWidth;
    uint _GridHeight;
    uint _M;
    uint _pad0;
    uint _pad1;
    uint _pad2;
    float4 _GridOffset_CellSize;
};

bool CompactUniformGrid2D_CellID(float2 p, out uint cell) {
    float2 q = p - _GridOffset_CellSize.xy;
    int2 c = (int2)floor(q / _GridOffset_CellSize.zw);
    if (any(c < 0) || c.x >= (int)_GridWidth || c.y >= (int)_GridHeight) {
        cell = 0;
        return false;
    }
    cell = (uint)c.x + (uint)c.y * _GridWidth;
    return true;
}

#endif
