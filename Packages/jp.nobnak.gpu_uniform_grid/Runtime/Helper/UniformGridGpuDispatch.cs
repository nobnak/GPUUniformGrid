namespace Nobnak.GPU.UniformGrid {

    internal static class UniformGridGpuDispatch {
        public const int ThreadGroupWidth = 64;
        public const int CompactThreadGroupWidth = 256;

        public static int Groups1D(int totalCount) {
            if (totalCount <= 0)
                return 0;
            return (totalCount + ThreadGroupWidth - 1) / ThreadGroupWidth;
        }

        public static int Groups1DCompact(int totalCount) {
            if (totalCount <= 0)
                return 0;
            return (totalCount + CompactThreadGroupWidth - 1) / CompactThreadGroupWidth;
        }
    }
}
