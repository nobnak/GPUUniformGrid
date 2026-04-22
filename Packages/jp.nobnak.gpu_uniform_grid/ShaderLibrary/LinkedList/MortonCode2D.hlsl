#ifndef __MORTON_CODE_2D_HLSL__
#define __MORTON_CODE_2D_HLSL__

// 各軸 16bit（0..65535）まで。uint 32bit に 2 軸 interleave。

uint MortonCode2D_Expand16(uint v) {
	v &= 0xFFFFu;
	v = (v | (v << 8)) & 0x00FF00FFu;
	v = (v | (v << 4)) & 0x0F0F0F0Fu;
	v = (v | (v << 2)) & 0x33333333u;
	v = (v | (v << 1)) & 0x55555555u;
	return v;
}

uint MortonCode_Encode2(uint2 i) {
	uint x = MortonCode2D_Expand16(i.x);
	uint y = MortonCode2D_Expand16(i.y);
	return x | (y << 1);
}

#endif
