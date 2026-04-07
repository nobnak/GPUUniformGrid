using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nobnak.GPU.UniformGrid {

    public static class UniformGridConverter {

        public static async Task<CPUUniformGrid> ToCPU(this GPUUniformGrid gpu) {
            var naCellHead = new NativeArray<uint>(gpu.cellHead.count, Allocator.Persistent);
            var naCellNext = new NativeArray<uint>(gpu.cellNext.count, Allocator.Persistent);
            var reqCellHead = AsyncGPUReadback.RequestIntoNativeArray(ref naCellHead, gpu.cellHead);
            var reqCellNext = AsyncGPUReadback.RequestIntoNativeArray(ref naCellNext, gpu.cellNext);

            while (!reqCellHead.done || !reqCellNext.done)
                await Task.Yield();

            if (reqCellHead.hasError) {
                Debug.LogError("Error in reading cell head buffer");
                naCellHead.Dispose();
                naCellNext.Dispose();
                return null;
            }
            if (reqCellNext.hasError) {
                Debug.LogError("Error in reading cell next buffer");
                naCellHead.Dispose();
                naCellNext.Dispose();
                return null;
            }

            return new CPUUniformGrid(gpu.gridParams, naCellHead, naCellNext);
        }

        public static async Task<CPUUniformGrid2D> ToCPU(this GPUUniformGrid2D gpu) {
            var naCellHead = new NativeArray<uint>(gpu.cellHead.count, Allocator.Persistent);
            var naCellNext = new NativeArray<uint>(gpu.cellNext.count, Allocator.Persistent);
            var reqCellHead = AsyncGPUReadback.RequestIntoNativeArray(ref naCellHead, gpu.cellHead);
            var reqCellNext = AsyncGPUReadback.RequestIntoNativeArray(ref naCellNext, gpu.cellNext);

            while (!reqCellHead.done || !reqCellNext.done)
                await Task.Yield();

            if (reqCellHead.hasError) {
                Debug.LogError("Error in reading 2D cell head buffer");
                naCellHead.Dispose();
                naCellNext.Dispose();
                return null;
            }
            if (reqCellNext.hasError) {
                Debug.LogError("Error in reading 2D cell next buffer");
                naCellHead.Dispose();
                naCellNext.Dispose();
                return null;
            }

            return new CPUUniformGrid2D(gpu.gridParams, naCellHead, naCellNext);
        }
    }
}
