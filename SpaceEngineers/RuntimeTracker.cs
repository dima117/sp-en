using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineers
{
    #region Copy

    public class RuntimeTracker
    {
        public int Capacity { get; set; }
        public double MaxRuntime { get; private set; }
        public double MaxInstructions { get; private set; }
        public double AverageRuntime { get; private set; }
        public double AverageInstructions { get; private set; }
        public double LastRuntime { get; private set; }
        public double LastInstructions { get; private set; }

        readonly Queue<double> runtimes = new Queue<double>();
        readonly Queue<double> instructions = new Queue<double>();
        readonly int instructionLimit;
        readonly MyGridProgram program;

        double runtimeSum = 0;
        double instructionsSum = 0;

        public RuntimeTracker(MyGridProgram program, int capacity = 120)
        {
            this.program = program;
            Capacity = capacity;
            instructionLimit = program.Runtime.MaxInstructionCount;
        }

        public void AddRuntime()
        {
            LastRuntime = program.Runtime.LastRunTimeMs;

            runtimes.Enqueue(LastRuntime);
            runtimeSum += LastRuntime;

            if (runtimes.Count > Capacity)
            {
                var firstRuntime = runtimes.Dequeue();
                runtimeSum -= firstRuntime;
            }

            AverageRuntime = runtimeSum / runtimes.Count();

            MaxRuntime = runtimes.Max();
        }

        public void AddInstructions()
        {
            LastInstructions = program.Runtime.CurrentInstructionCount;

            instructions.Enqueue(LastInstructions);
            instructionsSum += LastInstructions;

            if (instructions.Count > Capacity)
            {
                var firstInstructions = instructions.Dequeue();
                instructionsSum -= firstInstructions;
            }

            AverageInstructions = instructionsSum / instructions.Count();

            MaxInstructions = instructions.Max();
        }

        public override string ToString()
        {
            return $"Runtime: {LastRuntime:0.00}ms/{AverageRuntime:0.00}ms/{MaxRuntime:0.00}ms\n" +
                $"Instructions: {LastInstructions:0}/{AverageInstructions:0}/{MaxInstructions:0}\n" +
                $"Complexity: {AverageInstructions:0}/{instructionLimit:0} ({AverageInstructions / instructionLimit:0.00}%)";
        }
    }

    #endregion
}
