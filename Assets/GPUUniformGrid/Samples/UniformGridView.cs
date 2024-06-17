using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Nobnak.GPU.UniformGrid;
using Unity.Collections;
using System.Text;

public class UniformGridView : MonoBehaviour {

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
        if (grid == null || !isActiveAndEnabled) return;

        var gridParams = grid.gridParams;
        var cellSize = gridParams.cellSize;
        var cellCount = gridParams.NumberOfCellsPerAxis;
        var gridSize = cellSize * cellCount;

        var gridEnd0 = gridParams.gridOffset;
        var gridEnd1 = gridParams.gridOffset + gridSize;
        var gridCenter = (gridEnd0 + gridEnd1) * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(gridCenter, new float3(gridSize));
    }
    void Update() {
        if (grid == null) {
            var gridParams = new UniformGridParams(
                tuner.gridOffset,
                tuner.cellSize,
                tuner.bitsPerAxis,
                tuner.elementCapacity);
            grid = new GPUUniformGrid(gridParams);
        }

        if (grid != null) {
            grid.Reset();
            grid.SetParamsGlobal();
        }
    }
    #endregion

    #region methods
    private void DisposeGrid() {
        if (grid != null) {
            grid.Dispose();
            grid = null;
        }
    }
    #endregion

    #region declarations
    [System.Serializable]
    public class Tuner {
        public float3 gridOffset;
        public float cellSize;
        [Range(1, 10)]
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