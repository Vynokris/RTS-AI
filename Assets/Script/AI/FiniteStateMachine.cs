using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FiniteStateMachine : MonoBehaviour
{
    private List<Node> states = new List<Node>();
    private Node currentState = null;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //for (int i = 0; i < currentState.GetConnections().Count; i++)
        //{
        //    if (currentState.GetConnections()[i].Trigger())
        //    {
        //        currentState.GetExitAction()?.Invoke(); // Call the state exit function as we are about to exit the current state
        //        currentState = currentState.GetConnections()[i].GetDestinationNode();
        //        currentState.GetEnterAction()?.Invoke(); // Call the state enter function of the new current state
        //        break;
        //    }
        //}

        //currentState.GetUpdateAction()?.Invoke();
    }

    public void CreateState(string stateName, Action updateFunction = null, Action onStateEnter = null, Action onStateExit = null)
    {
        states.Add(new Node(stateName, updateFunction, onStateEnter, onStateExit));

        if (states.Count == 1)
        {
            currentState = states[0];
        }
    }

    public void CreateConnection(string fromNodeName, string toNodeName, Func<bool> transitionFunction)
    {
        Node from = GetStateByName(fromNodeName);

        if (from == null)
            return;

        Connection newConnection = new Connection(GetStateByName(toNodeName), transitionFunction);

        from.GetConnections().Add(newConnection);
    }

    public void CreateConnection(int fromNodeId, int toNodeId, Func<bool> transitionFunction)
    {
        Node from = GetState(fromNodeId);

        if (from == null)
            return;

        Connection newConnection = new Connection(GetState(toNodeId), transitionFunction);

        from.GetConnections().Add(newConnection);
    }

    public string GetCurrentStateName()
    {
        return currentState.GetName();
    }

    protected Node GetStateByName(string stateName)
    {
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i].GetName() == stateName)
            {
                return states[i];
            }
        }

        return null;
    }

    protected Node GetState(int id)
    {
        return id < states.Count ? states[id] : null;
    }
}

public class Node
{
    protected string stateName;
    private List<Connection> connections = new List<Connection>();

    protected Action updateFunction;
    protected Action onStateEnter;
    protected Action onStateExit;

    public Node(string stateName, Action updateFunction, Action onStateEnter, Action onStateExit)
    {
        this.stateName = stateName;
        this.updateFunction = updateFunction;
        this.onStateEnter = onStateEnter;
        this.onStateExit = onStateExit;
    }

    public string GetName()
    {
        return stateName;
    }

    public List<Connection> GetConnections()
    {
        return connections;
    }

    public Action GetUpdateAction()
    {
        return updateFunction;
    }

    public Action GetEnterAction()
    {
        return onStateEnter;
    }

    public Action GetExitAction()
    {
        return onStateExit;
    }
}

public class Connection
{
    private Node toNode;

    protected Func<bool> triggerFunction;

    public Connection(Node toNode, Func<bool> triggerFunction)
    {
        this.toNode = toNode;
        this.triggerFunction = triggerFunction;
    }

    public bool Trigger()
    {
        return triggerFunction();
    }

    public Node GetDestinationNode()
    {
        return toNode;
    }
}
