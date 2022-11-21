using UnityEngine;

/// <summary>
/// interface that defines the different FSM states
/// </summary>
public interface IState
{
    public void UpdateState(FSMController controller);
    public void OnTrigger(FSMController controller, Collider other);
}
