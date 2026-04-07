namespace Nobnak.GPU.UniformGrid {

    internal static class UniformGridGpuDispatch {
        public const int ThreadGroupWidth = 64;

        public static int Groups1D(int totalCount) {
            if (totalCount <= 0)
                return 0;
            return (totalCount + ThreadGroupWidth - 1) / ThreadGroupWidth;
        }
    }
}
