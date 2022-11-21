using UnityEngine;

public class FSMWalkAwayState : IState
{
    /// <summary>
    /// UpdateState for the Walk Away state
    /// We walk away from the agent we saw
    /// We check if we are too far from the agent, in which case we go back to the Follow state
    /// </summary>
    /// <param name="controller">FSMController</param>
    public void UpdateState(FSMController controller)
    {
        controller.WalkAwayFromAgent();
        float distance = controller.DistanceToAgent();
        if (controller.CheckDistance(distance, false))
        {
            controller.ChangeToState(controller.FollowState);
        }
    }

    public void OnTrigger(FSMController controller, Collider other) { }
}
