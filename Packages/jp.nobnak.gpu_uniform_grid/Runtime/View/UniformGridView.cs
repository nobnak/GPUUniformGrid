using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using Nobnak.GPU.UniformGrid;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UniformGridView : MonoBehaviour, IGPUUniformGridProvider {

    [SerializeField] protected Events events = new();
    [SerializeField] protected Links links = new();
    [SerializeField] protected Tuner tuner = new();
    /// <summary><see cref="SrcMode.Provider"/> かつ <see cref="gridProvider"/> が null のとき <see cref="runtimeProvider"/> を参照する。</summary>
    [SerializeField] protected MonoBehaviour gridProvider;

    protected GPUUniformGrid ownedGrid;

    protected bool needRebuild;
    protected RenderParams renderParams;
    IGPUUniformGridProvider runtimeProvider;

    #region unity
    void OnEnable() {
        NormalizeSrcMode();
        needRebuild = true;
    }
    void OnDisable() {
        DisposeOwnedGrid();
    }
    void OnValidate() {
        NormalizeSrcMode();
        needRebuild = true;
    }
    void OnDrawGizmos() {
        if ((tuner.visualize & VisualizeFlags.Volume) != 0)
            VisualizeVolume();
    }

    void Update() {
        Validate();

        if ((tuner.visualize & VisualizeFlags.Grid) != 0)
            VisualizeGrid();

        var g = ActiveGrid;
        if (g != null && tuner.setGlobalParams)
            g.SetParamsGlobal();
    }
    #endregion

    #region interface
    public GPUUniformGrid Grid => ActiveGrid;

    public GPUUniformGrid ActiveGrid => tuner.srcMode == SrcMode.Provider
        ? ResolveProviderGrid()
        : ownedGrid;

    /// <summary>Inspector モード時のみ <see cref="Tuner.gridTuner"/> を更新して再構築する。Provider モードでは無視。</summary>
    public void SetGridParams(UniformGridParams gridParams) {
        if (tuner.srcMode == SrcMode.Provider)
            return;
        tuner.gridTuner.Apply(gridParams);
        needRebuild = true;
    }

    public void SetRuntimeProvider(IGPUUniformGridProvider provider) {
        runtimeProvider = provider;
        needRebuild = true;
    }

    public void ClearRuntimeProvider() {
        runtimeProvider = null;
        needRebuild = true;
    }
    #endregion

    #region methods
    void NormalizeSrcMode() {
        var v = (int)tuner.srcMode;
        if (v == (int)SrcMode.Inspector || v == (int)SrcMode.Provider)
            return;
        if (v == (int)LegacySrcModeValue.Setter) {
            tuner.srcMode = SrcMode.Inspector;
            Debug.LogWarning(
                $"[UniformGridView] '{name}': SrcMode Setter is removed; normalized to Inspector. Use SetGridParams while in Inspector mode.",
                this);
            return;
        }
        tuner.srcMode = SrcMode.Inspector;
        Debug.LogWarning(
            $"[UniformGridView] '{name}': Invalid SrcMode value {v}; normalized to Inspector.",
            this);
    }

    IGPUUniformGridProvider ResolveProviderForEvent() {
        if (tuner.srcMode == SrcMode.Provider)
            return (gridProvider as IGPUUniformGridProvider) ?? runtimeProvider;
        return this;
    }

    GPUUniformGrid ResolveProviderGrid() {
        if (gridProvider != null) {
            var p = gridProvider as IGPUUniformGridProvider;
            if (p == null) {
                Debug.LogWarning(
                    $"[UniformGridView] '{name}': gridProvider ({gridProvider.GetType().Name}) does not implement {nameof(IGPUUniformGridProvider)}.",
                    this);
                return null;
            }
            return p.Grid;
        }
        return runtimeProvider?.Grid;
    }

    void Validate() {
        NormalizeSrcMode();
        if (!needRebuild)
            return;
        needRebuild = false;

        if (tuner.srcMode == SrcMode.Inspector) {
            if (ownedGrid != null)
                DisposeOwnedGrid();

            var gridParams = (UniformGridParams)tuner.gridTuner;
            if (gridParams.IsValid())
                ownedGrid = new GPUUniformGrid(gridParams);
        } else {
            if (ownedGrid != null)
                DisposeOwnedGrid();
        }
        events.OnGridChanged?.Invoke(ResolveProviderForEvent());

        renderParams = new RenderParams(links.cellDensity);
        renderParams.worldBounds = new Bounds(float3.zero, new float3(1000));
        renderParams.matProps ??= new();
        renderParams.layer = gameObject.layer;
        
    }
    void DisposeOwnedGrid() {
        if (ownedGrid != null) {
            ownedGrid.Dispose();
            ownedGrid = null;
        }
    }

    void VisualizeGrid() {
        Validate();

        var g = ActiveGrid;
        if (g == null)
            return;

        UniformGridDebugDrawer.DrawGrid(g, ref renderParams);
    }

    void VisualizeVolume() {
        UniformGridDebugDrawer.DrawVolumeGizmos(ActiveGrid);
    }
    #endregion

    #region declarations
    /// <summary>旧 <see cref="SrcMode"/> の Setter は削除済み。シリアライズ互換用の数値。</summary>
    enum LegacySrcModeValue {
        Setter = 1,
    }

    [System.Serializable]
    public class Events {
        public UnityEngine.Events.UnityEvent<IGPUUniformGridProvider> OnGridChanged;
    }
    [System.Serializable]
    public class Links {
        public Material cellDensity;
    }
    [System.Flags]
    public enum VisualizeFlags {
        Default = 0,
        Volume = 1 << 0,
        Grid = 1 << 1,
    }
    /// <summary>
    /// <see cref="Provider"/> は旧シーン互換のため 2（以前の enum と同じ値）。
    /// </summary>
    public enum SrcMode {
        Inspector = 0,
        Provider = 2,
    }
    [System.Serializable]
    public class GridTuner {
        public float3 gridCenter;
        public float gridSize = 20f;
        [Range(0, 10)]
        public uint bitsPerAxis = 5;
        public uint elementCapacity = 1024;

        #region methods
        public GridTuner Apply(UniformGridParams gridParams) {
            gridCenter = gridParams.gridCenter;
            gridSize = gridParams.gridSize;
            bitsPerAxis = gridParams.bitsPerAxis;
            elementCapacity = gridParams.elementCapacity;
            return this;
        }
        public static implicit operator UniformGridParams(GridTuner tuner) {
            return new UniformGridParams(
                tuner.gridCenter,
                tuner.gridSize,
                tuner.bitsPerAxis,
                tuner.elementCapacity);
        }
        public static explicit operator GridTuner(UniformGridParams gridParams) {
            return new GridTuner().Apply(gridParams);
        }
        #endregion
    }
    [System.Serializable]
    public class Tuner {
        public SrcMode srcMode = SrcMode.Inspector;
        public bool setGlobalParams = true;
        public VisualizeFlags visualize;
        public GridTuner gridTuner = new();
    }

    #endregion

    #region inspector
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UniformGridView))]
    public class UniformGridViewEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("events"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("links"));
            var tunerProp = serializedObject.FindProperty("tuner");
            EditorGUILayout.PropertyField(tunerProp.FindPropertyRelative("srcMode"));
            EditorGUILayout.PropertyField(tunerProp.FindPropertyRelative("setGlobalParams"));
            EditorGUILayout.PropertyField(tunerProp.FindPropertyRelative("visualize"));
            var modeInt = tunerProp.FindPropertyRelative("srcMode").intValue;
            if (modeInt == (int)SrcMode.Inspector)
                EditorGUILayout.PropertyField(tunerProp.FindPropertyRelative("gridTuner"));
            else if (modeInt == (int)SrcMode.Provider) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gridProvider"));
                EditorGUILayout.HelpBox(
                    "runtimeProvider はコードから SetRuntimeProvider / ClearRuntimeProvider で設定します。gridProvider が非 null のときはそちらを優先します。",
                    UnityEditor.MessageType.Info);
            } else
                EditorGUILayout.HelpBox("無効な SrcMode です。保存時・実行時に Inspector に正規化されます。", UnityEditor.MessageType.Warning);
            serializedObject.ApplyModifiedProperties();

            var view = (UniformGridView)target;
            EditorGUI.BeginDisabledGroup(view.ActiveGrid == null);
            if (GUILayout.Button("Readback")) {
                view.StartCoroutine(CoReadbackUniformGrid(view.ActiveGrid));
            }
            EditorGUI.EndDisabledGroup();
        }

        #region routine
        IEnumerator CoReadbackUniformGrid(GPUUniformGrid grid) {
            CPUUniformGrid cpuGrid = null;
            try {
                var task = grid.ToCPU();
                while (!task.IsCompleted) {
                    yield return null;
                }
                if (task.IsFaulted) {
                    Debug.LogError($"Error in reading uniform grid");
                    yield break;
                }
                if (task.IsCompletedSuccessfully) {
                    cpuGrid = task.Result;
                    Debug.Log(cpuGrid);
                }
            } finally {
                if (cpuGrid != null) {
                    cpuGrid.Dispose();
                }
            }
        }
        #endregion
    }
#endif
    #endregion
}
