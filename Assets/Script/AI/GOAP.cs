using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using Debug = UnityEngine.Debug;

public class GOAP : MonoBehaviour
{
    public WorldState currentWorldState;

    [SerializeField] public List<GOAPAction> availableActions = new();
    [SerializeField] public List<GOAPAction> currentPlan = new();

    protected void Start()
    {
        InitializeWorldState();
        InitializeActions();
        Plan();
    }

    void InitializeWorldState()
    {
        currentWorldState = new WorldState();
        currentWorldState.state[0] = true;
    }

    void InitializeActions()
    {
        //availableActions = new List<Action>();

        ////Ore Available, Has Ore, Iron Available, Has Iron
        //Action action1 = new CollectOre("CollectOre", new WorldState(), new WorldState());
        //action1.preconditions.state[0] = true;

        //action1.effects.state[0] = true;
        //action1.effects.state[1] = true;
        //availableActions.Add(action1);

        //Action action2 = new DepositOre("DepositOre", new WorldState(), new WorldState());
        //action2.preconditions.state[0] = true;
        //action2.preconditions.state[1] = true;

        //action2.effects.state[0] = true;
        //action2.effects.state[2] = true;
        //action2.effects.state[1] = false;
        //availableActions.Add(action2);

        //Action action3 = new CollectIron("CollectIron", new WorldState(), new WorldState());
        //action3.preconditions.state[0] = true;
        //action3.preconditions.state[2] = true;

        //action3.effects.state[0] = true;
        //action3.effects.state[3] = true;
        //availableActions.Add(action3);

        //Action action4 = new DepositIron("DepositIron", new WorldState(), new WorldState());
        //action4.preconditions.state[0] = true;
        //action4.preconditions.state[3] = true;
        //action4.effects.state[0] = true;
        //action4.effects.state[3] = false;
        //availableActions.Add(action4);
    }

    void Plan()
    {
        WorldState goalState = new WorldState();
        goalState.state[0] = true;
        goalState.state[3] = true;

        // Find a plan using A*
        var timer = new Stopwatch();
        timer.Start();
        currentPlan = AStarPlan(currentWorldState, goalState);
        timer.Stop();
        Debug.Log("Plan constructed in " + timer.Elapsed.TotalMilliseconds * 1000 + " Microseconds");
    }

    List<GOAPAction> AStarPlan(WorldState start, WorldState goal)
    {
        // A* search algorithm
        PriorityQueue<Node> openSet = new PriorityQueue<Node>();
        HashSet<int> closedSet = new HashSet<int>();

        Node startNode = new Node(start, null, 0, Heuristic(start, goal));
        openSet.Enqueue(startNode, startNode.Cost + startNode.Heuristic);

        while (openSet.Count > 0)
        {
            Node current = openSet.Dequeue();

            if (current.State.state.value == goal.state.value)
            {
                List<GOAPAction> plan = new List<GOAPAction>();
                while (current.Action != null)
                {
                    plan.Insert(0, current.Action);
                    current = current.Parent;
                }
                return plan;
            }

            closedSet.Add(current.State.state.value);

            foreach (GOAPAction action in availableActions)
            {
                if (action.CheckPreconditions(current.State) && !closedSet.Contains(action.effects.state.value))
                {
                    WorldState newState = action.effects.Clone();
                    Node newNode = new Node(newState, current, current.Cost + 1, Heuristic(newState, goal), action);

                    if (!openSet.Contains(newNode) && !closedSet.Contains(newState.state.value))
                    {
                        openSet.Enqueue(newNode, newNode.Cost + newNode.Heuristic);
                    }
                }
            }
        }

        return null; // No plan found
    }

    float Heuristic(WorldState a, WorldState b)
    {
        int differences = 0;

        int xorResult = a.state.value ^ b.state.value;

        while (xorResult != 0)
        {
            differences += xorResult & 1;
            xorResult >>= 1;
        }

        return differences;
    }

    void ExecutePlan()
    {
        if (currentPlan != null && currentPlan.Count > 0)
        {
            GOAPAction nextAction = currentPlan[0];

            if (!nextAction.CheckPreconditions(currentWorldState))
            {
                Plan();
                return;
            }

            if (nextAction.Execute(this))
            {
                currentWorldState = nextAction.effects.Clone();
                currentPlan.RemoveAt(0);
            }
        }

        if (currentPlan != null && currentPlan.Count == 0)
        {
            GOAPAction action = availableActions[^1];

            if (!action.CheckPreconditions(currentWorldState))
            {
                Plan();
                return;
            }

            if (action.Execute(this))
            {
                currentWorldState = action.effects.Clone();
                Plan();
            }
        }
    }

    void Update()
    {
        //ExecutePlan();
    }

    private class Node
    {
        public WorldState State { get; }
        public Node Parent { get; }
        public int Cost { get; }
        public float Heuristic { get; }
        public GOAPAction Action { get; }

        public Node(WorldState state, Node parent, int cost, float heuristic, GOAPAction action = null)
        {
            State = state;
            Parent = parent;
            Cost = cost;
            Heuristic = heuristic;
            Action = action;
        }
    }

    private class PriorityQueue<T>
    {
        private readonly SortedDictionary<float, Queue<T>> _dict = new SortedDictionary<float, Queue<T>>();

        public void Enqueue(T item, float priority)
        {
            if (!_dict.ContainsKey(priority))
                _dict[priority] = new Queue<T>();

            _dict[priority].Enqueue(item);
        }

        public T Dequeue()
        {
            var pair = _dict.First();
            var queue = pair.Value;
            var item = queue.Dequeue();

            if (queue.Count == 0)
                _dict.Remove(pair.Key);

            return item;
        }

        public int Count
        {
            get { return _dict.Sum(pair => pair.Value.Count); }
        }

        public bool Contains(T item)
        {
            return _dict.Any(pair => pair.Value.Contains(item));
        }
    }
}
