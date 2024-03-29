﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers
{
    #region Copy
    public class BlockArray<T> where T : class, IMyTerminalBlock
    {
        MyGridProgram program;
        Action<T> init;

        private List<T> list = new List<T>();
        private int index = 0;

        public int Count => list.Count;

        public T1 Aggregate<T1>(T1 a, Func<T1, T, T1> fn) => list.Aggregate(a, fn);

        public BlockArray(MyGridProgram program, Action<T> init = null)
        {
            this.program = program;
            this.init = init;

            UpdateBlocks();
        }

        public void UpdateBlocks()
        {
            index = 0;
            list = new List<T>();
            program.GridTerminalSystem.GetBlocksOfType(list);

            list.ForEach(init);
        }

        public void ForEach(Action<T> fn = null)
        {
            if (fn != null)
            {
                list.ForEach(fn);
            }
        }

        public T GetNext(Func<T, bool> filter = null)
        {
            for (var count = 0; count < list.Count; count++)
            {
                index++;

                if (index >= list.Count)
                {
                    index = 0;
                }

                T block = list[index];

                if (filter == null || filter(block))
                {
                    return block;
                }
            }

            return null;
        }
    }

    #endregion
}
