using Unity.Mathematics;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    /// <summary>
    /// Inspector から <see cref="GPUUniformGrid"/> を生成・保持する（デバッグ・Drawer 検証用）。
    /// </summary>
    public sealed class GPUUniformGridBehaviour : MonoBehaviour, IGPUUniformGridProvider {

        [SerializeField] Events events = new();
        [SerializeField] float3 gridCenter;
        [SerializeField] float gridSize = 20f;
        [Range(0, 10)]
        [SerializeField] uint bitsPerAxis = 5;
        [SerializeField] uint elementCapacity = 1024;

        GPUUniformGrid grid;
        bool gridRebuildPending;

        public GPUUniformGrid Grid => grid;

        #region unity
        void OnEnable() {
            Rebuild();
        }

        void OnDisable() {
            ReleaseGrid();
            events.OnGridChanged?.Invoke(null!);
        }

        void OnValidate() {
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
        /// <summary>ランタイムでパラメータを差し替えて再構築する。</summary>
        public void SetGridParams(UniformGridParams gridParams) {
            gridCenter = gridParams.gridCenter;
            gridSize = gridParams.gridSize;
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
            var p = new UniformGridParams(gridCenter, gridSize, bitsPerAxis, elementCapacity);
            if (p.IsValid())
                grid = new GPUUniformGrid(p);
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
            public UnityEngine.Events.UnityEvent<IGPUUniformGridProvider> OnGridChanged;
        }
        #endregion
    }
}
