using UnityEngine;
using UnityEngine.AI;

public class Wander : MonoBehaviour
{
    private NavMeshAgent navMeshAgent; // NavMeshAgent component
    private Vector3 randomDestination; // random wander point
    private Vector3 center; // center of the wander sphere
    private float randomAngle; // angle for the direction of the random wander point
    private bool onSwitchPoint; // reached a switch point

    [SerializeField] private float radius = 1; // radius of the wander sphere
    [SerializeField] private float distance = 5; // distance of the wander sphere
    [Range(0, 360)][SerializeField] private float maxAngle = 120; // max angle for switch points

    /// <summary>
    /// We get the NavMeshAgent component and initialize a random rotation for the agent
    /// </summary>
    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        int randomRotationMultiplier = Random.Range(0, 4);
        transform.Rotate(0, 90 * randomRotationMultiplier, 0);
    }

    /// <summary>
    /// We check whether we should wander or not
    /// </summary>
    private void Update()
    {
        if(!onSwitchPoint)
        {
            DoWander();
        }
    }

    /// <summary>
    /// Wander method
    /// We define the center of the wander sphere
    /// We get a random angle
    /// We get the direction for that angle
    /// We get the destination from the center and the radius of the sphere and the calculated direction
    /// </summary>
    private void DoWander()
    {
        center = transform.position + navMeshAgent.velocity.normalized * distance;
        randomAngle = Random.Range(0f, 2 * Mathf.PI);
        Vector3 direction = new Vector3(Mathf.Sin(randomAngle), 0, Mathf.Cos(randomAngle));
        randomDestination = center + direction * radius;
        navMeshAgent.destination = randomDestination;
    }

    /// <summary>
    /// Gizmos to help see what is happening
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, radius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(randomDestination, 0.5f);
        if(navMeshAgent != null)
            Gizmos.DrawLine(transform.position, navMeshAgent.destination);
    }


    /// <summary>
    /// Switch Point control
    /// If we enter a switch point we choose one of its possible exit points, if it's not behind us, we set it as a destination
    /// </summary>
    /// <param name="other">Collider of the trigger we entered</param>
    private void OnTriggerEnter(Collider other)
    {
        if (WanderSwitchButton.SwitchButton)
        {
            onSwitchPoint = true;
            SwitchPoint point = other.GetComponent<SwitchPoint>();
            if (point != null)
            {
                int randomInt = Random.Range(0, point.OpenPoints.Length);
                Vector3 destination = point.OpenPoints[randomInt].position;
                Vector3 direction = (destination - transform.position).normalized;

                if (Vector3.Angle(transform.forward, direction) < maxAngle)
                {
                    navMeshAgent.destination = destination;
                }
                else
                {
                    onSwitchPoint = false;
                }
            }
        }
    }

    /// <summary>
    /// If we exit a trigger, we're out of the switch point
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        onSwitchPoint = false;
    }
}
