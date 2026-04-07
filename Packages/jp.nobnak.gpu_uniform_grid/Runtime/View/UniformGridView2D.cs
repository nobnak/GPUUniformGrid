using System.Collections;
using Nobnak.GPU.UniformGrid;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 2D 均一グリッドの可視化（セルに要素があれば枠線）。平面の向き・3D 埋め込みは
/// <see cref="UniformGridParams2D.planeZ"/> とゲームオブジェクトの配置で調整する。
/// </summary>
public class UniformGridView2D : MonoBehaviour {

    [SerializeField] protected Events events = new();
    [SerializeField] protected Links links = new();
    [SerializeField] protected Tuner tuner = new();

    protected GPUUniformGrid2D grid;
    protected bool needRebuild;
    protected RenderParams renderParams;
    protected UniformGridParams2D setterGridParams;

    #region unity

    void OnEnable() {
        needRebuild = true;
    }

    void OnDisable() {
        DisposeGrid();
    }

    void OnValidate() {
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

        if (grid != null && tuner.setGlobalParams)
            grid.SetParamsGlobal();
    }

    #endregion

    #region interface

    public GPUUniformGrid2D ActiveGrid => grid;

    public void SetGridParams(UniformGridParams2D gridParams) {
        setterGridParams = gridParams;
        needRebuild = true;
    }

    #endregion

    #region methods

    void Validate() {
        if (!needRebuild)
            return;
        needRebuild = false;

        if (grid != null)
            DisposeGrid();

        var gridParams = tuner.srcMode == SrcMode.Inspector
            ? (UniformGridParams2D)tuner.gridTuner
            : setterGridParams;
        if (gridParams.IsValid())
            grid = new GPUUniformGrid2D(gridParams);
        events.OnGridChanged?.Invoke(grid);

        renderParams = new RenderParams(links.cellDensity);
        renderParams.worldBounds = new Bounds(float3.zero, new float3(1000));
        renderParams.matProps ??= new();
        renderParams.layer = gameObject.layer;
    }

    void DisposeGrid() {
        if (grid != null) {
            grid.Dispose();
            grid = null;
            events.OnGridChanged?.Invoke(null);
        }
    }

    void VisualizeGrid() {
        Validate();
        if (grid == null)
            return;

        var gp = grid.gridParams;
        grid.SetParams(renderParams.matProps);
        Graphics.RenderPrimitives(renderParams,
            MeshTopology.Lines,
            2, (int)gp.TotalNumberOfCells);
    }

    void VisualizeVolume() {
        if (grid == null)
            return;

        var p = grid.gridParams;
        var c = new float3(p.gridCenter.x, p.gridCenter.y, p.planeZ);
        var s = new float3(p.gridSize.x, p.gridSize.y, 0.02f);
        Gizmos.color = Color.grey;
        Gizmos.DrawWireCube(c, s);
    }

    #endregion

    #region declarations

    [System.Serializable]
    public class Events {
        public UnityEngine.Events.UnityEvent<GPUUniformGrid2D> OnGridChanged;
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
        Setter,
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
            base.OnInspectorGUI();
            var view = (UniformGridView2D)target;
            UnityEngine.GUI.enabled = view.ActiveGrid != null;
            if (GUILayout.Button("Readback")) {
                view.StartCoroutine(CoReadback(view.ActiveGrid));
            }
            UnityEngine.GUI.enabled = true;
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
