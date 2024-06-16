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
        grid = new GPUUniformGrid();

        grid.InitializeGrid(tuner.gridOffset, tuner.cellSize, tuner.bitsPerAxis);
        grid.InitializeElements(tuner.elementCapacity);

        StartCoroutine(CoReadbackUniformGrid(grid));
    }
    void OnDisable() {
        if (grid != null) {
            grid.Dispose();
            grid = null;
        }
    }
    #endregion

    #region routine
    IEnumerator CoReadbackUniformGrid(GPUUniformGrid grid) {
        CPUUniformGrid cpuGrid = null;
        foreach (var _ in grid.ToCPU(v => cpuGrid = v))
            yield return null;

        try {
            if (cpuGrid == null) {
                Debug.LogWarning("Failed to readback");
                yield break;
            }

            var log = new StringBuilder();

            var cellHead = cpuGrid.CellHead;
            var cellNext = cpuGrid.CellNext;
            log.AppendLine($"Cell head: len={cellHead.Length}");
            for (int i = 0; i < cellHead.Length; i++) {
                log.Append($"{(int)cellHead[i]}, ");
                if (i >= 10) {
                    log.AppendLine("...");
                    break;
                }
            }

            log.AppendLine($"Cell next: len{cellNext.Length}");
            for (int i = 0; i < cellNext.Length; i++) {
                log.Append($"{(int)cellNext[i]}, ");
                if (i >= 10) {
                    log.AppendLine("...");
                    break;
                }
            }

            log.AppendLine($"About NativeArray");

            Debug.Log(log);
        } finally {
            if (cpuGrid != null)
                cpuGrid.Dispose();
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
}