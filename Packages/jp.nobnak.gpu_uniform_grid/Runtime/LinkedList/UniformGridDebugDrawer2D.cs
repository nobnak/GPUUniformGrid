using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// <see cref="GPUUniformGrid2D"/> の可視化のみを担当する（グリッドの生成・破棄は行わない）。
    /// </summary>
    public static class UniformGridDebugDrawer2D {

        public static void DrawGrid(GPUUniformGrid2D grid, ref RenderParams renderParams) {
            if (grid == null || renderParams.material == null)
                return;
            var gp = grid.gridParams;
            renderParams.matProps ??= new MaterialPropertyBlock();
            grid.SetParams(renderParams.matProps);
            Graphics.RenderPrimitives(renderParams,
                MeshTopology.Lines,
                2, (int)gp.TotalNumberOfCells);
        }

        public static void DrawVolumeGizmos(GPUUniformGrid2D grid) {
            if (grid == null)
                return;
            var p = grid.gridParams;
            var c = new float3(p.gridCenter.x, p.gridCenter.y, p.planeZ);
            var s = new float3(p.gridSize.x, p.gridSize.y, 0.02f);
            Gizmos.color = Color.grey;
            Gizmos.DrawWireCube(c, s);
        }
    }
}
