#ifndef __MORTON_CODE_HLSL__
#define __MORTON_CODE_HLSL__

// https://fgiesen.wordpress.com/2009/12/13/decoding-morton-codes/

uint MortonCode_Encode3_x(uint x) { 
	x &= 0x000003ff;                  // x = ---- ---- ---- ---- ---- --98 7654 3210
	x = (x ^ (x << 16)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
	x = (x ^ (x <<  8)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
	x = (x ^ (x <<  4)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
	x = (x ^ (x <<  2)) & 0x09249249; // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
	return x;
}
uint MortonCode_Decode3_x(uint x) {
	x &= 0x09249249;                  // x = ---- 9--8 --7- -6-- 5--4 --3- -2-- 1--0
	x = (x ^ (x >>  2)) & 0x030c30c3; // x = ---- --98 ---- 76-- --54 ---- 32-- --10
	x = (x ^ (x >>  4)) & 0x0300f00f; // x = ---- --98 ---- ---- 7654 ---- ---- 3210
	x = (x ^ (x >>  8)) & 0xff0000ff; // x = ---- --98 ---- ---- ---- ---- 7654 3210
	x = (x ^ (x >> 16)) & 0x000003ff; // x = ---- ---- ---- ---- ---- --98 7654 3210
	return x;
}

uint MortonCode_Encode3(uint3 v) {
	uint x = MortonCode_Encode3_x(v.x);
	uint y = MortonCode_Encode3_x(v.y);
	uint z = MortonCode_Encode3_x(v.z);
	return x | (y << 1) | (z << 2);
}
uint3 MortonCode_Decode3(uint v) {
	uint x = MortonCode_Decode3_x(v);
	uint y = MortonCode_Decode3_x(v >> 1);
	uint z = MortonCode_Decode3_x(v >> 2);
	return uint3(x, y, z);
}


#endif