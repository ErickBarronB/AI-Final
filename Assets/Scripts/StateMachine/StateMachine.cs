using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    private IState currentState;
    private Dictionary<System.Type, IState> states = new Dictionary<System.Type, IState>();
    
    public IState CurrentState => currentState;
    
    public string GetCurrentStateName()
    {
        if (currentState == null) return "None";
        
        string stateName = currentState.GetType().Name;
        // Remove "State" suffix for cleaner display
        if (stateName.EndsWith("State"))
            stateName = stateName.Substring(0, stateName.Length - 5);
        
        return stateName;
    }
    
    public void AddState<T>(T state) where T : IState
    {
        states[typeof(T)] = state;
    }
    
    public void ChangeState<T>() where T : IState
    {
        if (states.TryGetValue(typeof(T), out IState newState))
        {
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }
    }
    
    public bool IsInState<T>() where T : IState
    {
        return currentState?.GetType() == typeof(T);
    }
    
    void Update()
    {
        currentState?.Update();
    }
}
