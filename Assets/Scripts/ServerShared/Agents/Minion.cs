
using System.Linq;
using UniRx;

public class Minion : Agent
{
    public Minion(Ship ship) : base(ship)
    {
        var patrolState = new PatrolOrbitsState(this);
        _rootState.AddTransition(patrolState, 
            () => Task is PatrolOrbitsTask,
            () => patrolState.Task = Task as PatrolOrbitsTask);

        Ship.VisibleEnemies.ObserveAdd().Where(_ => Ship.Target.Value == null).Subscribe(add => Ship.Target.Value = add.Value);

        var combatState = new CombatState(this);
        _rootState.AddTransition(combatState,
            () => Ship.Target.Value != null, null, true, _rootState);
        
        combatState.AddTransition(_rootState,
            () => Ship.Target.Value == null, null, true, _rootState);
    }
}