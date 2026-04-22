namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// 外部が保持する <see cref="GPUUniformGrid2D"/> をデバッグ表示などに渡すための契約。
    /// </summary>
    public interface IGPUUniformGridProvider2D {
        GPUUniformGrid2D Grid { get; }
    }
}
