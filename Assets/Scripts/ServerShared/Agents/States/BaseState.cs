using System;
using System.Collections.Generic;

public class BaseState
{
    public List<StateTransition> Transitions { get; } = new List<StateTransition>();
    protected Agent _agent;
    

    public BaseState(Agent agent)
    {
        _agent = agent;
    }

    private static List<BaseState> _lastVisitedStates = new List<BaseState>();
    private static List<BaseState> _leafStates = new List<BaseState>();
    private static List<BaseState> _visitedStates = new List<BaseState>();
    private static HashSet<BaseState> _ignoreStates = new HashSet<BaseState>();
    public void AddTransition(BaseState targetState, Func<bool> condition, Action onTransition = null, bool includeChildren = false, params BaseState[] ignoreStates)
    {
        Transitions.Add(new StateTransition(targetState, condition, onTransition));
        
        // Traverse the state graph, adding every state that is reachable from this state without traversing over ignored nodes
        if (includeChildren)
        {
            _visitedStates.Clear();
            _ignoreStates.Clear();
            if(ignoreStates!=null)
                foreach (var ignoreState in ignoreStates)
                    _ignoreStates.Add(ignoreState);
            _leafStates.Clear();
            _leafStates.Add(this);
            _ignoreStates.Add(this);
            _ignoreStates.Add(targetState);
            while (_leafStates.Count > 0)
            {
                _lastVisitedStates.Clear();
                _lastVisitedStates.AddRange(_leafStates);
                _leafStates.Clear();
                foreach (var state in _lastVisitedStates)
                {
                    foreach (var transition in state.Transitions)
                    {
                        if(!_ignoreStates.Contains(transition.TargetState))
                        {
                            _visitedStates.Add(transition.TargetState);
                            _ignoreStates.Add(transition.TargetState);
                            _leafStates.Add(transition.TargetState);
                        }
                    }
                }
            }
            foreach (var state in _visitedStates)
            {
                state.AddTransition(targetState, condition, onTransition);
            }
        }
    }
    
    public virtual void Update(float delta){}
    public virtual void OnEnterState(){}
    public virtual void OnExitState(){}
}