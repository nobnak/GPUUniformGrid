#ifndef __COMPACT_UNIFORM_GRID_3D_HLSL__
#define __COMPACT_UNIFORM_GRID_3D_HLSL__

cbuffer CompactUniformGrid3D_Params {
    uint _NumParticles;
    uint _TotalCells;
    uint _Nx;
    uint _Ny;
    uint _Nz;
    uint _M;
    uint _pad0;
    uint _pad1;
    uint _pad2;
    float3 _GridOffset;
    float _CellSize;
};

bool CompactUniformGrid3D_CellID(float3 p, out uint cell) {
    float3 q = p - _GridOffset;
    int3 c = (int3)floor(q / _CellSize);
    if (any(c < 0) || c.x >= (int)_Nx || c.y >= (int)_Ny || c.z >= (int)_Nz) {
        cell = 0;
        return false;
    }
    cell = (uint)c.x + (uint)_Nx * ((uint)c.y + (uint)_Ny * (uint)c.z);
    return true;
}

#endif
