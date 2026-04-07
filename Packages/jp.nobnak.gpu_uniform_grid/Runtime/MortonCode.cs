using Unity.Mathematics;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// 3D Morton (Z-order) encode matching <c>MortonCode.hlsl</c> (10 bits per axis).
    /// </summary>
    public static class MortonCode {

        public static uint Encode3_x(uint x) {
            x &= 0x000003ffu;
            x = (x ^ (x << 16)) & 0xff0000ffu;
            x = (x ^ (x << 8)) & 0x0300f00fu;
            x = (x ^ (x << 4)) & 0x030c30c3u;
            x = (x ^ (x << 2)) & 0x09249249u;
            return x;
        }

        public static uint Encode3(uint3 v) {
            uint x = Encode3_x(v.x);
            uint y = Encode3_x(v.y);
            uint z = Encode3_x(v.z);
            return x | (y << 1) | (z << 2);
        }
    }
}
