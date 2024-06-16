using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    public class CPUUniformGrid : System.IDisposable {

        public NativeArray<uint> CellHead { get; protected set; }
        public NativeArray<uint> CellNext { get; protected set; }

        public CPUUniformGrid(NativeArray<uint> cellhead, NativeArray<uint> cellnext) {
            this.CellHead = cellhead;
            this.CellNext = cellnext;
        }

        public void Dispose() {
            CellHead.Dispose();
            CellNext.Dispose();
        }
    }
}