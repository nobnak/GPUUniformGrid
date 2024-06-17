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
        var gridParams = new UniformGridParams(
            tuner.gridOffset, 
            tuner.cellSize, 
            tuner.bitsPerAxis, 
            tuner.elementCapacity);
        grid = new GPUUniformGrid(gridParams);

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

            Debug.Log(cpuGrid);
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