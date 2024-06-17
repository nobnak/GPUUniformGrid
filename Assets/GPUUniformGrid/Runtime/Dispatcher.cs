using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    public class Dispatcher : MonoBehaviour {

        public static readonly ConcurrentQueue<System.Action> Actions = new ();

        #region static
        static Dispatcher _instance;
        public static Dispatcher GetInstance() {
            lock (Actions) {
                if (_instance == null) {
                    var go = new GameObject("Dispatcher");
                    go.hideFlags = HideFlags.DontSave;
                    _instance = go.AddComponent<Dispatcher>();
                }
                return _instance;
            }
        }
        #endregion

        #region interface
        public Task Enque(System.Action action) {
            var tcs = new TaskCompletionSource<bool>();
            Actions.Enqueue(() => {
                try {
                    action();
                    tcs.SetResult(true);
                } catch (System.Exception e) {
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }
        #endregion

        #region unity
        void Awake() {
            lock (Actions) {
                if (_instance != null) {
                    Destroy(gameObject);
                    return;
                }
            }

            DontDestroyOnLoad(this);
        }
        void OnDestroy() {
            lock (Actions) {
                if (_instance == this)
                    _instance = null;
            }
        }
        void Update() {
            while (Actions.TryDequeue(out var action)) {
                action();
            }
        }
        #endregion
    }
}