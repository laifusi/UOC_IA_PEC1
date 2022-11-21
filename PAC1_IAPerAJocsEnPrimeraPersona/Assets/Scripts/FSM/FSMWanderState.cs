using UnityEngine;

public class FSMWanderState : IState
{
    /// <summary>
    /// UpdateState for the Wander state
    /// We wander and we check if we see an agent
    /// If we do, we change to the follow state
    /// </summary>
    /// <param name="controller">FSMController</param>
    public void UpdateState(FSMController controller)
    {
        controller.Wander();
        if(controller.Perceive())
        {
            controller.ChangeToState(controller.FollowState);
        }
    }

    /// <summary>
    /// If we hit a trigger, we call the method that controls the switch points
    /// </summary>
    /// <param name="controller">FSMController</param>
    /// <param name="other">Collider we triggered</param>
    public void OnTrigger(FSMController controller, Collider other)
    {
        controller.SwitchPoint(other);
    }
}
