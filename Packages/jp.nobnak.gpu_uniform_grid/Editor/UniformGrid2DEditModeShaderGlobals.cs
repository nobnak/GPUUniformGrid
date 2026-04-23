using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid.Editor {

    /// <summary>
    /// Edit 時に Scene ビュー等でシェーダが動いても、Play 由来のグローバル束縛が残らないようにする（3D / 2D 連結リスト＋近接サンプル）。
    /// </summary>
    static class UniformGrid2DEditModeShaderGlobals {

        [InitializeOnLoadMethod]
        static void Register() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            if (!EditorApplication.isPlaying)
                ClearAll();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.EnteredEditMode)
                ClearAll();
        }

        static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode) {
            if (EditorApplication.isPlaying)
                return;
            ClearAll();
        }

        static void ClearAll() {
            GPUUniformGrid.ClearParamsGlobal();
            GPUUniformGrid2D.ClearParamsGlobal();
            CpuProximityShaderGlobals2D.Clear();
        }
    }
}
