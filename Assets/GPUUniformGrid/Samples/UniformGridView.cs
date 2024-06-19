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

    #region unity
    void OnEnable() {
    }
    void OnDisable() {
        DisposeGrid();
    }
    void OnValidate() {
        DisposeGrid();
    }
    void OnDrawGizmos() {
        if (grid == null || !isActiveAndEnabled) {
            return;
        }

        var gridParams = grid.gridParams;
        var gridSize = gridParams.gridSize;

        var gridEnd0 = gridParams.GridOffset;
        var gridEnd1 = gridParams.GridOffset + gridSize;
        var gridCenter = (gridEnd0 + gridEnd1) * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(gridCenter, new float3(gridSize));
    }
    void Update() {
        if (grid == null) {
            var gridParams = new UniformGridParams(
                tuner.gridCenter,
                tuner.gridSize,
                tuner.bitsPerAxis,
                tuner.elementCapacity);
            grid = new GPUUniformGrid(gridParams);
            events.OnGridChanged?.Invoke(grid);
        }
        if (grid != null) {
            var cellDensityRp = new RenderParams(links.cellDensity);
            cellDensityRp.worldBounds = new Bounds(float3.zero, new float3(1000));
            cellDensityRp.matProps = new();

            var gridParams = grid.gridParams;
            grid.SetParams(cellDensityRp.matProps);
            Graphics.RenderPrimitives(cellDensityRp,
                MeshTopology.Lines,
                2, (int)gridParams.TotalNumberOfCells);
        }
    }
    #endregion

    #region methods
    private void DisposeGrid() {
        if (grid != null) {
            grid.Dispose();
            grid = null;
            events.OnGridChanged?.Invoke(null);
        }
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
    [System.Serializable]
    public class Tuner {
        public float3 gridCenter;
        public float gridSize;
        [Range(0, 10)]
        public uint bitsPerAxis;

        public uint elementCapacity = 1024;
    }

    #endregion

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
}