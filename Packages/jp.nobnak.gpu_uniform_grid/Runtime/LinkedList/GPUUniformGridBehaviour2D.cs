using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// Inspector から <see cref="GPUUniformGrid2D"/> を生成・保持する（デバッグ・Drawer 検証用）。
    /// </summary>
    public sealed class GPUUniformGridBehaviour2D : MonoBehaviour, IGPUUniformGridProvider2D {

        [SerializeField] Events events = new();
        [SerializeField] float2 gridCenter;
        [SerializeField] float2 gridSize = new float2(20f, 20f);
        [SerializeField] float planeZ;
        [Range(0, 16)]
        [SerializeField] uint bitsPerAxis = 5;
        [SerializeField] uint elementCapacity = 1024;

        GPUUniformGrid2D grid;
        bool gridRebuildPending;

        public GPUUniformGrid2D Grid => grid;

        #region unity
        void OnEnable() {
            Rebuild();
        }

        void OnDisable() {
            ReleaseGrid();
            events.OnGridChanged?.Invoke(null!);
        }

        void OnValidate() {
            // エディタ非再生では GraphicsBuffer を一切確保しない（delayCall もドメインリロードと競合しリークしやすい）。
            if (!Application.isPlaying)
                return;
            gridRebuildPending = true;
        }

        void Update() {
            if (Application.isPlaying)
                ProcessPendingGridRebuildFromValidate();
        }

        void OnDestroy() {
            ReleaseGrid();
        }
        #endregion

        #region interface
        public void SetGridParams(UniformGridParams2D gridParams) {
            gridCenter = gridParams.gridCenter;
            gridSize = gridParams.gridSize;
            planeZ = gridParams.planeZ;
            bitsPerAxis = gridParams.bitsPerAxis;
            elementCapacity = gridParams.elementCapacity;
            Rebuild();
        }
        #endregion

        #region methods
        void ProcessPendingGridRebuildFromValidate() {
            if (!Application.isPlaying || !gridRebuildPending)
                return;
            if (!enabled || !gameObject.activeInHierarchy)
                return;
            Rebuild();
        }

        void Rebuild() {
            gridRebuildPending = false;
            if (!Application.isPlaying) {
                ReleaseGrid();
                events.OnGridChanged?.Invoke(this);
                return;
            }
            ReleaseGrid();
            var p = new UniformGridParams2D(gridCenter, gridSize, planeZ, bitsPerAxis, elementCapacity);
            if (p.IsValid())
                grid = new GPUUniformGrid2D(p);
            events.OnGridChanged?.Invoke(this);
        }

        void ReleaseGrid() {
            if (grid != null) {
                grid.Dispose();
                grid = null;
            }
        }
        #endregion

        #region declarations
        [System.Serializable]
        public class Events {
            public UnityEngine.Events.UnityEvent<IGPUUniformGridProvider2D> OnGridChanged;
        }
        #endregion
    }
}
