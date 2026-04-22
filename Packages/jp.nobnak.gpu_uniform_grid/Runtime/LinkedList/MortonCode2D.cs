using Unity.Mathematics;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>2D Morton エンコード（<c>MortonCode2D.hlsl</c> と一致、各軸 16bit）。</summary>
    public static class MortonCode2D {

        public static uint Expand16(uint v) {
            v &= 0xFFFFu;
            v = (v | (v << 8)) & 0x00FF00FFu;
            v = (v | (v << 4)) & 0x0F0F0F0Fu;
            v = (v | (v << 2)) & 0x33333333u;
            v = (v | (v << 1)) & 0x55555555u;
            return v;
        }

        public static uint Encode2(uint2 i) {
            uint x = Expand16(i.x);
            uint y = Expand16(i.y);
            return x | (y << 1);
        }
    }
}
