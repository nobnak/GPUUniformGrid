using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Nobnak.GPU.UniformGrid;
using Unity.Collections;
using System.Text;

public class UniformGridView : MonoBehaviour {

    [SerializeField] protected Events events = new();
    [SerializeField] protected Links links = new();
    [SerializeField] protected Tuner tuner = new();

    protected GPUUniformGrid grid;

    protected bool needRebuild;
    protected RenderParams renderParams;
    protected UniformGridParams setterGridParams;

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

        if (grid != null && tuner.setGlobalParams) {
            grid.SetParamsGlobal();
        }
    }
    #endregion

    #region interface
    public void SetGridParams(UniformGridParams gridParams) { 
        setterGridParams = gridParams;
        needRebuild = true;
    }
    #endregion

    #region methods
    private void Validate() {
        if (!needRebuild)
            return;
        needRebuild = false;

        if (grid != null)
            DisposeGrid();

        var gridParams = tuner.srcMode == SrcMode.Inspector ? tuner.gridTuner : setterGridParams;
        if (gridParams.IsValid())
            grid = new GPUUniformGrid(gridParams);
        events.OnGridChanged?.Invoke(grid);

        renderParams = new RenderParams(links.cellDensity);
        renderParams.worldBounds = new Bounds(float3.zero, new float3(1000));
        renderParams.matProps ??= new();
        renderParams.layer = gameObject.layer;
        
    }
    private void DisposeGrid() {
        if (grid != null) {
            grid.Dispose();
            grid = null;
            events.OnGridChanged?.Invoke(null);
        }
    }

    private void VisualizeGrid() {
        Validate();

        if (grid == null)
            return;

        var gridParams = grid.gridParams;
        grid.SetParams(renderParams.matProps);
        Graphics.RenderPrimitives(renderParams,
            MeshTopology.Lines,
            2, (int)gridParams.TotalNumberOfCells);
    }

    private void VisualizeVolume() {
        if (grid == null)
            return;

        var gridParams = grid.gridParams;
        var gridSize = gridParams.gridSize;

        var gridEnd0 = gridParams.GridOffset;
        var gridEnd1 = gridParams.GridOffset + gridSize;
        var gridCenter = (gridEnd0 + gridEnd1) * 0.5f;

        Gizmos.color = Color.grey;
        Gizmos.DrawWireCube(gridCenter, new float3(gridSize));
    }
    #endregion

    #region declarations
    [System.Serializable]
    public class Events {
        public UnityEngine.Events.UnityEvent<GPUUniformGrid> OnGridChanged;
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
        Setter
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
            gridCenter = gridParams.GridOffset;
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
            base.OnInspectorGUI();
            var view = target as UniformGridView;
            var isEnabled = view.grid != null && view.grid != null;
            GUI.enabled = isEnabled;
            if (GUILayout.Button("Readback")) {
                view.StartCoroutine(CoReadbackUniformGrid(view.grid));
            }
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