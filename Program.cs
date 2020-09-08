using System;
using System.Collections.Generic;
using System.Linq;
using UltraDES;
using Scheduler = System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>;
using Restriction = System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, uint>;
using Update = System.Func<System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>, UltraDES.AbstractEvent, System.Collections.Generic.Dictionary<UltraDES.AbstractEvent, float>>;
using DFA = UltraDES.DeterministicFiniteAutomaton;

namespace Programa
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                string[] files = new string[0];

                if (args.Length == 0)
                    files = new string[] { "Hurink_sdata_mt06.fjs" };
                else
                    files = args;

                for (int i = 0; i < files.Length; i++)
                    try
                    {
                        ISchedulingProblem plant = new FJSSP(files[i]);
                    }
                    catch (Exception erro)
                    {
                        Console.WriteLine(erro.Message);
                    }
            }
            catch (Exception erro)
            {
                Console.WriteLine(erro.Message);
            }
            Console.ReadLine();
        }
    }

    internal class FJSSP : ISchedulingProblem
    {
        public List<Event> e = new List<Event>();

        public Dictionary<Event, int> time = new Dictionary<Event, int>();

        public FJSSP(string file)
        {
            try
            {
                string[] lines_str = System.IO.File.ReadAllLines(file);

                var matriz = new List<List<int>>();

                for (int i = 0; i < lines_str.Count(); i++)
                {
                    matriz.Add(new List<int>());
                    var line = lines_str[i].Split(' ');
                    for (int j = 0; j < line.Count(); j++)
                    {
                        if (line[j] != "")
                        {
                            int.TryParse(line[j], out int aux);
                            matriz[i].Add(aux);
                        }
                    }
                }

                var jobs = matriz[0][0]; var job_names = new string[jobs]; for (int i = 1; i <= jobs; i++) job_names[i - 1] = $"j{i}";
                var maquinas = matriz[0][1]; var maq_names = new string[maquinas]; for (int i = 1; i <= maquinas; i++) maq_names[i - 1] = $"m{i}";
                var op = 0;

                for (int i = 1; i < matriz.Count(); i++)
                    op += matriz[i][0];
                Depth = op * 2;
                var states_maq = new Dictionary<int, State>();
                states_maq[0] = new State($"s{0}", Marking.Marked); states_maq[1] = new State($"s{1}", Marking.Unmarked); states_maq[2] = new State($"s{2}", Marking.Unmarked);

                List<Transition> transitions_jobs = new List<Transition>();
                List<List<Transition>> transitions_maq = new List<List<Transition>>(); for (int i = 0; i < maquinas; i++) transitions_maq.Add(new List<Transition>());
                Dictionary<string, DFA> DFA_jobs = new Dictionary<string, DFA>();
                Dictionary<string, DFA> DFA_maq = new Dictionary<string, DFA>();
                List<int> next = new List<int>();

                List<int> atual = new List<int> { };
                List<int> anterior = atual.ToList();

                for (int i = 1; i < jobs + 1; i++) // cada job
                {
                    atual.Clear();
                    anterior.Clear();
                    int idx = 1;
                    for (int j = 1; j < matriz[i][0] + 1; j++) // cada operação
                    {
                        for (int k = 0; k < matriz[i][idx]; k++) // cada máquina
                        {
                            if (j != matriz[i][0]) // não é a última operação
                            {
                                if (anterior.Count == 0)
                                {
                                    e.Add(new Event($"a{i}{j}{matriz[i][idx + (2 * k) + 1]}{0}", Controllability.Controllable)); // cria evento a[job,op,maq,orig]
                                    transitions_jobs.Add(new Transition(new State($"s{2 * (j - 1)}.{0}", Marking.Unmarked), e.Last(), new State($"s{2 * j - 1}.{k}", Marking.Unmarked))); // inclui evento a no job
                                    transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[0], e.Last(), states_maq[1])); // inclui evento a na maq de 0 para 1
                                    time.Add(e.Last(), matriz[i][idx + (2 * k) + 2]);
                                    e.Add(new Event($"b{i}{j}{matriz[i][idx + (2 * k) + 1]}{0}", Controllability.Uncontrollable)); // cria evento b[job,op,maq,orig]
                                    transitions_jobs.Add(new Transition(new State($"s{2 * j - 1}.{k}", Marking.Unmarked), e.Last(), new State($"s{2 * j}.{0}", Marking.Unmarked))); // inclui evento b no job
                                    transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[1], e.Last(), states_maq[2])); // inclui evento b na maq de 1 para 2
                                    atual.Add(matriz[i][idx + (2 * k) + 1]); // inclui máquina atual
                                }

                                else if (anterior.Count == 1)
                                {
                                    if (matriz[i][idx + (2 * k) + 1] == anterior[0]) // se máquina atual é igual a anterior
                                    {
                                        e.Add(new Event($"a{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[0]}", Controllability.Controllable)); // cria evento a[job,op,maq,orig]
                                        transitions_jobs.Add(new Transition(new State($"s{2 * (j - 1)}.{0}", Marking.Unmarked), e.Last(), new State($"s{2 * j - 1}.{k}", Marking.Unmarked))); // inclui evento a no job
                                        transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[2], e.Last(), states_maq[1])); // inclui evento a na maq de 2 para 1
                                        time.Add(e.Last(), matriz[i][idx + (2 * k) + 2]);
                                        e.Add(new Event($"b{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[0]}", Controllability.Uncontrollable)); // cria evento b[job,op,maq,orig]
                                        transitions_jobs.Add(new Transition(new State($"s{2 * j - 1}.{k}", Marking.Unmarked), e.Last(), new State($"s{2 * j}.{0}", Marking.Unmarked))); // inclui evento b no job
                                        transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[1], e.Last(), states_maq[2])); // inclui evento b na maq de 1 para 2
                                        atual.Add(matriz[i][idx + (2 * k) + 1]); // inclui máquina atual
                                    }
                                    else
                                    {
                                        e.Add(new Event($"a{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[0]}", Controllability.Controllable)); // cria evento a[job,op,maq,orig]
                                        transitions_jobs.Add(new Transition(new State($"s{2 * (j - 1)}.{0}", Marking.Unmarked), e.Last(), new State($"s{2 * j - 1}.{k}", Marking.Unmarked))); // inclui evento a no job
                                        transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[0], e.Last(), states_maq[1])); // inclui evento a na maq de 0 para 1
                                        transitions_maq[anterior[0] - 1].Add(new Transition(states_maq[2], e.Last(), states_maq[0])); // inclui evento a na maq anterior de 2 para 0
                                        time.Add(e.Last(), matriz[i][idx + (2 * k) + 2]);
                                        e.Add(new Event($"b{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[0]}", Controllability.Uncontrollable)); // cria evento b[job,op,maq,orig]
                                        transitions_jobs.Add(new Transition(new State($"s{2 * j - 1}.{k}", Marking.Unmarked), e.Last(), new State($"s{2 * j}.{0}", Marking.Unmarked))); // inclui evento b no job
                                        transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[1], e.Last(), states_maq[2])); // inclui evento b na maq de 1 para 2
                                        atual.Add(matriz[i][idx + (2 * k) + 1]); // inclui máquina atual
                                    }
                                }

                                else
                                {
                                    for (int m = 0; m < anterior.Count; m++) // para cada máquina que processa a operação anterior
                                    {
                                        if (matriz[i][idx + (2 * k) + 1] == anterior[m]) // se máquina atual é igual a anterior
                                        {
                                            e.Add(new Event($"a{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[m]}", Controllability.Controllable)); // cria evento a[job,op,maq,orig]
                                            transitions_jobs.Add(new Transition(new State($"s{2 * (j - 1)}.{0}", Marking.Unmarked), e.Last(), new State($"s{2 * j - 1}.{k}", Marking.Unmarked))); // inclui evento a no job
                                            transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[2], e.Last(), states_maq[1])); // inclui evento a na maq de 2 para 1
                                            time.Add(e.Last(), matriz[i][idx + (2 * k) + 2]);
                                            e.Add(new Event($"b{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[m]}", Controllability.Uncontrollable)); // cria evento b[job,op,maq,orig]
                                            transitions_jobs.Add(new Transition(new State($"s{2 * j - 1}.{k}", Marking.Unmarked), e.Last(), new State($"s{2 * j}.{0}", Marking.Unmarked))); // inclui evento b no job
                                            transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[1], e.Last(), states_maq[2])); // inclui evento b na maq de 1 para 2
                                            atual.Add(matriz[i][idx + (2 * k) + 1]); // inclui máquina atual
                                        }
                                        else
                                        {
                                            e.Add(new Event($"a{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[m]}", Controllability.Controllable)); // cria evento a[job,op,maq,orig]
                                            transitions_jobs.Add(new Transition(new State($"s{2 * (j - 1)}.{0}", Marking.Unmarked), e.Last(), new State($"s{2 * j - 1}.{k}", Marking.Unmarked))); // inclui evento a no job
                                            transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[0], e.Last(), states_maq[1])); // inclui evento a na maq de 0 para 1
                                            transitions_maq[anterior[m] - 1].Add(new Transition(states_maq[2], e.Last(), states_maq[0])); // inclui evento a na maq anterior de 2 para 0
                                            time.Add(e.Last(), matriz[i][idx + (2 * k) + 2]);
                                            e.Add(new Event($"b{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[m]}", Controllability.Uncontrollable)); // cria evento b[job,op,maq,orig]
                                            transitions_jobs.Add(new Transition(new State($"s{2 * j - 1}.{k}", Marking.Unmarked), e.Last(), new State($"s{2 * j}.{0}", Marking.Unmarked))); // inclui evento b no job
                                            transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[1], e.Last(), states_maq[2])); // inclui evento b na maq de 1 para 2
                                            atual.Add(matriz[i][idx + (2 * k) + 1]); // inclui máquina atual
                                        }
                                    }
                                }

                            }
                            else
                            {
                                if (anterior.Count == 1)
                                {
                                    if (matriz[i][idx + (2 * k) + 1] == anterior[0]) // se máquina atual é igual a anterior
                                    {
                                        e.Add(new Event($"a{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[0]}", Controllability.Controllable)); // cria evento a[job,op,maq,orig]
                                        transitions_jobs.Add(new Transition(new State($"s{2 * (j - 1)}.{0}", Marking.Unmarked), e.Last(), new State($"s{2 * j - 1}.{k}", Marking.Unmarked))); // inclui evento a no job
                                        transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[2], e.Last(), states_maq[1])); // inclui evento a na maq de 2 para 1
                                        time.Add(e.Last(), matriz[i][idx + (2 * k) + 2]);
                                        e.Add(new Event($"b{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[0]}", Controllability.Uncontrollable)); // cria evento b[job,op,maq,orig]
                                        transitions_jobs.Add(new Transition(new State($"s{2 * j - 1}.{k}", Marking.Unmarked), e.Last(), new State($"s{2 * j}.{0}", Marking.Marked))); // inclui evento b no job
                                        transitions_jobs.Add(new Transition(new State($"s{2 * j}.{0}", Marking.Marked), new Event("e", Controllability.Uncontrollable), new State($"s{2 * j}.{0}", Marking.Marked))); // inclui evento e no job
                                        transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[1], e.Last(), states_maq[0])); // inclui evento b na maq de 1 para 0
                                        atual.Add(matriz[i][idx + (2 * k) + 1]); // inclui máquina atual
                                    }
                                    else
                                    {
                                        e.Add(new Event($"a{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[0]}", Controllability.Controllable)); // cria evento a[job,op,maq,orig]
                                        transitions_jobs.Add(new Transition(new State($"s{2 * (j - 1)}.{0}", Marking.Unmarked), e.Last(), new State($"s{2 * j - 1}.{k}", Marking.Unmarked))); // inclui evento a no job
                                        transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[0], e.Last(), states_maq[1])); // inclui evento a na maq de 0 para 1
                                        transitions_maq[anterior[0] - 1].Add(new Transition(states_maq[2], e.Last(), states_maq[0])); // inclui evento a na maq anterior de 2 para 0
                                        time.Add(e.Last(), matriz[i][idx + (2 * k) + 2]);
                                        e.Add(new Event($"b{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[0]}", Controllability.Uncontrollable)); // cria evento b[job,op,maq,orig]
                                        transitions_jobs.Add(new Transition(new State($"s{2 * j - 1}.{k}", Marking.Unmarked), e.Last(), new State($"s{2 * j}.{0}", Marking.Marked))); // inclui evento b no job
                                        transitions_jobs.Add(new Transition(new State($"s{2 * j}.{0}", Marking.Marked), new Event("e", Controllability.Uncontrollable), new State($"s{2 * j}.{0}", Marking.Marked))); // inclui evento e no job
                                        transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[1], e.Last(), states_maq[0])); // inclui evento b na maq de 1 para 0
                                        atual.Add(matriz[i][idx + (2 * k) + 1]); // inclui máquina atual
                                    }
                                }

                                else
                                {
                                    for (int m = 0; m < anterior.Count; m++) // para cada máquina que processa a operação anterior
                                    {
                                        if (matriz[i][idx + (2 * k) + 1] == anterior[m]) // se máquina atual é igual a anterior
                                        {
                                            e.Add(new Event($"a{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[m]}", Controllability.Controllable)); // cria evento a[job,op,maq,orig]
                                            transitions_jobs.Add(new Transition(new State($"s{2 * (j - 1)}.{0}", Marking.Unmarked), e.Last(), new State($"s{2 * j - 1}.{k}", Marking.Unmarked))); // inclui evento a no job
                                            transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[2], e.Last(), states_maq[1])); // inclui evento a na maq de 2 para 1
                                            time.Add(e.Last(), matriz[i][idx + (2 * k) + 2]);
                                            e.Add(new Event($"b{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[m]}", Controllability.Uncontrollable)); // cria evento b[job,op,maq,orig]
                                            transitions_jobs.Add(new Transition(new State($"s{2 * j - 1}.{k}", Marking.Unmarked), e.Last(), new State($"s{2 * j}.{0}", Marking.Marked))); // inclui evento b no job
                                            transitions_jobs.Add(new Transition(new State($"s{2 * j}.{0}", Marking.Marked), new Event("e", Controllability.Uncontrollable), new State($"s{2 * j}.{0}", Marking.Marked))); // inclui evento e no job
                                            transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[1], e.Last(), states_maq[0])); // inclui evento b na maq de 1 para 0
                                            atual.Add(matriz[i][idx + (2 * k) + 1]); // inclui máquina atual
                                        }
                                        else
                                        {
                                            e.Add(new Event($"a{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[m]}", Controllability.Controllable)); // cria evento a[job,op,maq,orig]
                                            transitions_jobs.Add(new Transition(new State($"s{2 * (j - 1)}.{0}", Marking.Unmarked), e.Last(), new State($"s{2 * j - 1}.{k}", Marking.Unmarked))); // inclui evento a no job
                                            transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[0], e.Last(), states_maq[1])); // inclui evento a na maq de 0 para 1
                                            transitions_maq[anterior[0] - 1].Add(new Transition(states_maq[2], e.Last(), states_maq[0])); // inclui evento a na maq anterior de 2 para 0
                                            time.Add(e.Last(), matriz[i][idx + (2 * k) + 2]);
                                            e.Add(new Event($"b{i}{j}{matriz[i][idx + (2 * k) + 1]}{anterior[m]}", Controllability.Uncontrollable)); // cria evento b[job,op,maq,orig]
                                            transitions_jobs.Add(new Transition(new State($"s{2 * j - 1}.{k}", Marking.Unmarked), e.Last(), new State($"s{2 * j}.{0}", Marking.Marked))); // inclui evento b no job
                                            transitions_jobs.Add(new Transition(new State($"s{2 * j}.{0}", Marking.Marked), new Event("e", Controllability.Uncontrollable), new State($"s{2 * j}.{0}", Marking.Marked))); // inclui evento e no job
                                            transitions_maq[matriz[i][idx + (2 * k) + 1] - 1].Add(new Transition(states_maq[1], e.Last(), states_maq[0])); // inclui evento b na maq de 1 para 0
                                            atual.Add(matriz[i][idx + (2 * k) + 1]); // inclui máquina atual
                                        }
                                    }
                                }
                            }
                        }

                        idx = idx + matriz[i][idx] * 2 + 1;
                        atual = atual.Distinct().ToList();
                        anterior = atual.ToList();
                        atual.Clear();
                    }

                    DFA_jobs.Add(job_names[i - 1], new DFA(transitions_jobs, new State($"s{0}.{0}", Marking.Unmarked), job_names[i - 1]));
                    transitions_jobs.Clear();
                }

                e.Add(new Event("e", Controllability.Uncontrollable));

                for (int i = 0; i < transitions_maq.Count(); i++)
                {
                    transitions_maq[i].Add(new Transition(states_maq[0], new Event("e", Controllability.Uncontrollable), states_maq[0]));
                    DFA_maq.Add(maq_names[i], new DFA(transitions_maq[i], states_maq[0], maq_names[i]));
                }

                Supervisor = DFA.MonolithicSupervisor(DFA_jobs.Values, DFA_maq.Values, true);

                e = e.Distinct().ToList();
            }
            catch (Exception erro)
            {
                Console.WriteLine("Erro: " + erro.Message);
            }
        }

        public DFA Supervisor { get; }

        public int QuantMachine => 1;

        public int QuantBuffer => 1;

        public int Depth { get; }

        public int id => 7;

        public AbstractState InitialState => Supervisor.InitialState;

        public AbstractState TargetState => Supervisor.States.Where(s => s.IsMarked).First();

        public Restriction InitialRestrition()
        {
            Restriction InitialRes = new Restriction();

            for (int i = 0; i < e.Count() - 1; i++)
                InitialRes.Add(e.ToList()[i], (uint)1);

            InitialRes.Add(new Event("e", Controllability.Uncontrollable), (uint)0);

            return InitialRes;
        }

        public Scheduler InitialScheduler =>
                e.ToDictionary(alias => alias as AbstractEvent,
                    alias => alias.IsControllable ? 0.0f : float.PositiveInfinity);

        public Update UpdateFunction => (old, ev) =>
        {
            var sch = old.ToDictionary(kvp => kvp.Key, kvp =>
            {
                var v = kvp.Value - old[ev];

                if (kvp.Key.IsControllable) return v < 0 ? 0 : v;
                if (v < 0) return float.NaN;
                return v;
            });

            if (!ev.IsControllable)
                sch[ev] = float.PositiveInfinity;
            else
            {
                var idx = old.Keys.ToList().FindIndex(i => i == ev);
                sch[e[idx + 1]] = time[e[idx]];
            }

            return sch;
        };
    }
}
