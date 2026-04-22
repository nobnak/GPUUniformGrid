using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// <see cref="GPUUniformGrid"/> の可視化のみを担当する（グリッドの生成・破棄は行わない）。
    /// </summary>
    public static class UniformGridDebugDrawer {

        public static void DrawGrid(GPUUniformGrid grid, ref RenderParams renderParams) {
            if (grid == null || renderParams.material == null)
                return;
            var gp = grid.gridParams;
            renderParams.matProps ??= new MaterialPropertyBlock();
            grid.SetParams(renderParams.matProps);
            Graphics.RenderPrimitives(renderParams,
                MeshTopology.Lines,
                2, (int)gp.TotalNumberOfCells);
        }

        public static void DrawVolumeGizmos(GPUUniformGrid grid) {
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
    }
}
