using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers
{
    public class BlockArray<T> where T : class, IMyTerminalBlock
    {
        string prefix;
        MyGridProgram program;
        Action<T> init;

        private List<T> list = new List<T>();
        private int index = 0;

        public int Count => list.Count;

        public BlockArray(MyGridProgram program, string prefix, Action<T> init = null)
        {
            this.program = program;
            this.prefix = prefix;
            this.init = init;

            UpdateBlocks();
        }

        public void UpdateBlocks()
        {
            list = new List<T>();
            index = 0;

            program.GridTerminalSystem.GetBlocksOfType(list, b => b.Name.StartsWith(prefix));
            ForEach(init);
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
}
