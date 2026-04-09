using System.Collections;
using Nobnak.GPU.UniformGrid;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 2D 均一グリッドの可視化（セルに要素があれば枠線）。平面の向き・3D 埋め込みは
/// <see cref="UniformGridParams2D.planeZ"/> とゲームオブジェクトの配置で調整する。
/// </summary>
public class UniformGridView2D : MonoBehaviour, IGPUUniformGridProvider2D {

    [SerializeField] protected Events events = new();
    [SerializeField] protected Links links = new();
    [SerializeField] protected Tuner tuner = new();
    [SerializeField] protected MonoBehaviour gridProvider;

    protected GPUUniformGrid2D ownedGrid;
    protected bool needRebuild;
    protected RenderParams renderParams;
    IGPUUniformGridProvider2D runtimeProvider;

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

    public GPUUniformGrid2D Grid => ActiveGrid;

    public GPUUniformGrid2D ActiveGrid => tuner.srcMode == SrcMode.Provider
        ? ResolveProviderGrid()
        : ownedGrid;

    /// <summary>Inspector モード時のみ <see cref="Tuner.gridTuner"/> を更新して再構築する。Provider モードでは無視。</summary>
    public void SetGridParams(UniformGridParams2D gridParams) {
        if (tuner.srcMode == SrcMode.Provider)
            return;
        tuner.gridTuner.Apply(gridParams);
        needRebuild = true;
    }

    public void SetRuntimeProvider(IGPUUniformGridProvider2D provider) {
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
                $"[UniformGridView2D] '{name}': SrcMode Setter is removed; normalized to Inspector. Use SetGridParams while in Inspector mode.",
                this);
            return;
        }
        tuner.srcMode = SrcMode.Inspector;
        Debug.LogWarning(
            $"[UniformGridView2D] '{name}': Invalid SrcMode value {v}; normalized to Inspector.",
            this);
    }

    IGPUUniformGridProvider2D ResolveProviderForEvent() {
        if (tuner.srcMode == SrcMode.Provider)
            return (gridProvider as IGPUUniformGridProvider2D) ?? runtimeProvider;
        return this;
    }

    GPUUniformGrid2D ResolveProviderGrid() {
        if (gridProvider != null) {
            var p = gridProvider as IGPUUniformGridProvider2D;
            if (p == null) {
                Debug.LogWarning(
                    $"[UniformGridView2D] '{name}': gridProvider ({gridProvider.GetType().Name}) does not implement {nameof(IGPUUniformGridProvider2D)}.",
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

            var gridParams = (UniformGridParams2D)tuner.gridTuner;
            if (gridParams.IsValid())
                ownedGrid = new GPUUniformGrid2D(gridParams);
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

        UniformGridDebugDrawer2D.DrawGrid(g, ref renderParams);
    }

    void VisualizeVolume() {
        UniformGridDebugDrawer2D.DrawVolumeGizmos(ActiveGrid);
    }

    #endregion

    #region declarations

    enum LegacySrcModeValue {
        Setter = 1,
    }

    [System.Serializable]
    public class Events {
        public UnityEngine.Events.UnityEvent<IGPUUniformGridProvider2D> OnGridChanged;
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

    public enum SrcMode {
        Inspector = 0,
        Provider = 2,
    }

    [System.Serializable]
    public class GridTuner2D {
        public float2 gridCenter;
        public float2 gridSize = new float2(20f, 20f);
        public float planeZ;
        [Range(0, 16)]
        public uint bitsPerAxis = 5;
        public uint elementCapacity = 1024;

        public GridTuner2D Apply(UniformGridParams2D p) {
            gridCenter = p.gridCenter;
            gridSize = p.gridSize;
            planeZ = p.planeZ;
            bitsPerAxis = p.bitsPerAxis;
            elementCapacity = p.elementCapacity;
            return this;
        }

        public static implicit operator UniformGridParams2D(GridTuner2D t) {
            return new UniformGridParams2D(t.gridCenter, t.gridSize, t.planeZ, t.bitsPerAxis, t.elementCapacity);
        }

        public static explicit operator GridTuner2D(UniformGridParams2D p) {
            return new GridTuner2D().Apply(p);
        }
    }

    [System.Serializable]
    public class Tuner {
        public SrcMode srcMode = SrcMode.Inspector;
        public bool setGlobalParams = true;
        public VisualizeFlags visualize;
        public GridTuner2D gridTuner = new();
    }

    #endregion

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UniformGridView2D))]
    public class UniformGridView2DEditor : UnityEditor.Editor {
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

            var view = (UniformGridView2D)target;
            EditorGUI.BeginDisabledGroup(view.ActiveGrid == null);
            if (GUILayout.Button("Readback")) {
                view.StartCoroutine(CoReadback(view.ActiveGrid));
            }
            EditorGUI.EndDisabledGroup();
        }

        static IEnumerator CoReadback(GPUUniformGrid2D gpu) {
            CPUUniformGrid2D cpu = null;
            try {
                var task = gpu.ToCPU();
                while (!task.IsCompleted)
                    yield return null;
                if (task.IsFaulted) {
                    Debug.LogError("Readback 2D grid failed");
                    yield break;
                }
                if (task.IsCompletedSuccessfully) {
                    cpu = task.Result;
                    Debug.Log(cpu != null ? cpu.ToString() : "null");
                }
            }
            finally {
                cpu?.Dispose();
            }
        }
    }
#endif
}
