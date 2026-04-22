namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// 外部が保持する <see cref="GPUUniformGrid"/> をデバッグ表示などに渡すための契約。
    /// </summary>
    public interface IGPUUniformGridProvider {
        GPUUniformGrid Grid { get; }
    }
}
