#ifndef __MORTON_CODE_HLSL__
#define __MORTON_CODE_HLSL__


uint MortonCode_Encode3_x(uint x) {
	x = (x | (x << 16)) & 0x030000FF;
	x = (x | (x <<  8)) & 0x0300F00F;
	x = (x | (x <<  4)) & 0x030C30C3;
	x = (x | (x <<  2)) & 0x09249249;
	return x;
}
uint MortonCode_Encode3(uint3 v) {
	uint x = MortonCode_Encode3_x(v.x);
	uint y = MortonCode_Encode3_x(v.y);
	uint z = MortonCode_Encode3_x(v.z);
	return x | (y << 1) | (z << 2);
}


#endif