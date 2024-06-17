using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using UnityEngine;

namespace Nobnak.GPU.UniformGrid {

    public class CPUUniformGrid : System.IDisposable {

        public readonly UniformGridParams gridParams;

        public NativeArray<uint> CellHead { get; protected set; }
        public NativeArray<uint> CellNext { get; protected set; }

        public CPUUniformGrid(UniformGridParams gridParams)
            : this(gridParams, 
                  new NativeArray<uint>((int)gridParams.TotalNumberOfCells, Allocator.Persistent),
                  new NativeArray<uint>((int)gridParams.elementCapacity, Allocator.Persistent)) 
        {

        }
        public CPUUniformGrid(UniformGridParams gridParams, NativeArray<uint> cellhead, NativeArray<uint> cellnext) {
            this.gridParams = gridParams;
            this.CellHead = cellhead;
            this.CellNext = cellnext;
        }

        #region IDisposable
        public void Dispose() {
            CellHead.Dispose();
            CellNext.Dispose();
        }
        #endregion

        #region object
        public override string ToString() {
            var log = new StringBuilder();

            log.AppendLine($"Cell head: len={CellHead.Length}");
            for (int i = 0; i < CellHead.Length; i++) {
                log.Append($"{(int)CellHead[i]}, ");
                if (i >= 10) {
                    log.AppendLine("...");
                    break;
                }
            }
            log.AppendLine();

            log.AppendLine($"Cell next: len={CellNext.Length}");
            for (int i = 0; i < CellNext.Length; i++) {
                log.Append($"{(int)CellNext[i]}, ");
                if (i >= 10) {
                    log.AppendLine("...");
                    break;
                }
            }

            return log.ToString();
        }
        #endregion
    }
}