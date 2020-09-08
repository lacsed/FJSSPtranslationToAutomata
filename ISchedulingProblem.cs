using System;
using System.Linq;
using System.Collections.Generic;
using UltraDES;

namespace Programa
{
    interface ISchedulingProblem
    {

        DeterministicFiniteAutomaton Supervisor { get; }
        int QuantMachine { get; }
        int QuantBuffer { get; }
        int Depth { get; }
        int id { get; }
        Dictionary<AbstractEvent, float> InitialScheduler { get; }
        AbstractState InitialState { get; }
        AbstractState TargetState { get; }
        Func<Dictionary<AbstractEvent, float>, AbstractEvent, Dictionary<AbstractEvent, float>> UpdateFunction { get; }
        Dictionary<AbstractEvent, uint> InitialRestrition();
    }

    public static class Extensions
    {
        public static uint ActiveTasks(this AbstractState state)
        {
            if (state is AbstractCompoundState)
                return (uint)(state as AbstractCompoundState).S.OfType<ExpandedState>().Sum(s => s.Tasks);
            if (state is ExpandedState)
                return (state as ExpandedState).Tasks;
            return 0;
        }
    }
}
