using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class FSMController : MonoBehaviour
{
    private IState currentState; // IState for the current state
    public FSMWanderState WanderState; // Wander state
    public FSMFollowState FollowState; // Follow state
    public FSMWalkAwayState WalkAwayState; // Walk Away state

    [SerializeField] private Text stateText; // Text to show what state we are in

    private NavMeshAgent navMeshAgent; // NavMeshAgent component
    private Vector3 randomWanderDestination; // random wander point
    private Vector3 centerWanderSphere; // center of the wander sphere
    private float randomWanderAngle; // random angle for the wander point
    private bool onSwitchPoint; // reached a switch point
    
    [Header("Wander")]
    [SerializeField] private float radius = 1; // wander sphere radius
    [SerializeField] private float distance = 5; // wander sphere distance
    [Range(0, 360)] [SerializeField] private float maxAngle = 120; // switch points max angle

    [Header("Perception")]
    [SerializeField] private GameObject agentSeenIndicator; // indicator for perception

    [SerializeField] private float perceptionRadius = 5; // radius of perception
    [Range(0, 360)] [SerializeField] private float perceptionAngle = 30; // angle of perception
    [SerializeField] private LayerMask agentMask; // layer mask for the agent
    [SerializeField] private LayerMask obstacleMask; // layer mask for the obstacles
    [SerializeField] private int numberOfRaysPerDegree; // number of rays per degree, used for the visual cone
    [SerializeField] private MeshFilter perceptionMeshFilter; // MeshFilter used for the visual cone

    private Mesh perceptionMesh; // Mesh used for the visual cone

    private Vector3 lastKnownPosition = new Vector3(); // last position known of the other agent
    private Vector3 lastKnownDirection = new Vector3(); // last direction known of the other agent
    private float lastKnownSpeed; // last speed known of the other agent
    private Transform otherAgent; // Transform of the other agent

    [Header("Follow and Walk Away")]
    [SerializeField] private float minFollowDistance = 2; // minimum distance for the follow state
    [SerializeField] private float maxWalkAwayDistance = 8; // maximum distance for the walk away state
    [SerializeField] private float reachedDestinationDistanceThreshold = 0f; // threshold for the reached last known destination method


    /// <summary>
    /// We get the NavMeshAgent component, initialize the states, initialize a random rotation and initialize the perception cone mesh
    /// </summary>
    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        WanderState = new FSMWanderState();
        FollowState = new FSMFollowState();
        WalkAwayState = new FSMWalkAwayState();

        ChangeToState(WanderState);

        int randomRotationMultiplier = Random.Range(0, 4);
        transform.Rotate(0, 90 * randomRotationMultiplier, 0);

        perceptionMesh = new Mesh();
        perceptionMesh.name = "Perception Mesh";
        perceptionMeshFilter.mesh = perceptionMesh;
    }
    
    /// <summary>
    /// Method to change from one state to another
    /// </summary>
    /// <param name="state">IState we have to switch to</param>
    public void ChangeToState(IState state)
    {
        if (state == WanderState)
            stateText.text = "Wander";
        else if (state == FollowState)
            stateText.text = "Follow";
        else if (state == WalkAwayState)
            stateText.text = "Walk away";

        currentState = state;
    }

    /// <summary>
    /// Update to call the currentState's UpdateState
    /// </summary>
    private void Update()
    {
        currentState.UpdateState(this);
    }

    /// <summary>
    /// LateUpdate to call the DrawPerceptionCone method
    /// </summary>
    private void LateUpdate()
    {
        DrawPerceptionCone();
    }

    /// <summary>
    /// Wander method
    /// We define the center of the wander sphere
    /// We get a random angle
    /// We get the direction for that angle
    /// We get the destination from the center and the radius of the sphere and the calculated direction
    /// </summary>
    public void Wander()
    {
        if(!onSwitchPoint)
        {
            centerWanderSphere = transform.position + navMeshAgent.velocity.normalized * distance;
            randomWanderAngle = Random.Range(0f, 2 * Mathf.PI);
            Vector3 direction = new Vector3(Mathf.Sin(randomWanderAngle), 0, Mathf.Cos(randomWanderAngle));
            randomWanderDestination = centerWanderSphere + direction * radius;
            navMeshAgent.destination = randomWanderDestination;
        }
    }

    /// <summary>
    /// OnTriggerEnter to call the currentState's OnTrigger
    /// </summary>
    /// <param name="other">Collider we triggered</param>
    private void OnTriggerEnter(Collider other)
    {
        currentState.OnTrigger(this, other);
    }

    /// <summary>
    /// Switch Point control
    /// If we enter a switch point we choose one of its possible exit points, if it's not behind us, we set it as a destination
    /// </summary>
    /// <param name="other">Collider of the trigger we entered</param>
    public void SwitchPoint(Collider other)
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

    /// <summary>
    /// Method where we check whether we are seeing an agent or not
    /// We find the agents in the radius
    /// We check if they are in the perception angle
    /// We check if there's any obstacle in between
    /// If we see an agent, we save its location, direction, speed and Transform
    /// </summary>
    /// <returns>true if we see an agent, false if we don't</returns>
    public bool Perceive()
    {
        ActivateAgentSeenIndicator(false);

        Collider[] agentsInPerceptionRadius = Physics.OverlapSphere(transform.position, perceptionRadius, agentMask);
        for (int i = 0; i < agentsInPerceptionRadius.Length; i++)
        {
            Transform agent = agentsInPerceptionRadius[i].transform;
            Vector3 direction = (agent.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, direction) <= perceptionAngle / 2)
            {
                float distance = Vector3.Distance(transform.position, agent.position);
                if (!Physics.Raycast(transform.position, direction, distance, obstacleMask))
                {
                    lastKnownPosition = agent.position;
                    lastKnownDirection = agent.forward;
                    lastKnownSpeed = agent.GetComponent<NavMeshAgent>().speed;
                    otherAgent = agent;
                    ActivateAgentSeenIndicator(true);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Method to activate or deactivate the perception indicator
    /// </summary>
    /// <param name="activate">bool that defines if we activate or deactivate</param>
    public void ActivateAgentSeenIndicator(bool activate)
    {
        agentSeenIndicator.SetActive(activate);
    }

    /// <summary>
    /// Method to follow an agent we see
    /// We calculate the direction and the future position to set our destination
    /// </summary>
    public void FollowAgent()
    {
        Vector3 direction = otherAgent.position - transform.position;
        float lookAhead = direction.magnitude / otherAgent.GetComponent<NavMeshAgent>().speed;
        Vector3 futurePosition = otherAgent.transform.position + otherAgent.transform.forward * lookAhead;
        navMeshAgent.destination = futurePosition;
    }

    /// <summary>
    /// Method to know the distance between the two agents
    /// </summary>
    /// <returns>distance to the agent seen</returns>
    public float DistanceToAgent()
    {
        return Vector3.Distance(lastKnownPosition, transform.position);
    }

    /// <summary>
    /// Method to check if we are too far or too close from the agent
    /// </summary>
    /// <param name="distance">distance to the agent's last known position</param>
    /// <param name="isFollowing">bool that tells us if we are in following state or in walk away state</param>
    /// <returns>true if we are too close or too far away</returns>
    public bool CheckDistance(float distance, bool isFollowing)
    {
        if(isFollowing)
        {
            return distance < minFollowDistance;
        }
        else
        {
            return distance > maxWalkAwayDistance;
        }
    }

    /// <summary>
    /// Method to know if we reached the last known location
    /// </summary>
    /// <returns>true if we reached the location, false if we didn't</returns>
    public bool InLastLocationKnown()
    {
        return navMeshAgent.remainingDistance < reachedDestinationDistanceThreshold;
    }

    /// <summary>
    /// Method to set the last location known as the NavMeshAgent destination
    /// </summary>
    public void GoToLastLocationKnown()
    {
        navMeshAgent.destination = lastKnownPosition;
    }

    /// <summary>
    /// Method to walk away from the agent
    /// We define the direction as the opposite of the agent's
    /// We calculate the lookAhead of the agent and the opposite position
    /// We set the opposite position as our destination
    /// </summary>
    public void WalkAwayFromAgent()
    {
        Vector3 direction = lastKnownDirection * -1;
        float lookAhead = direction.magnitude / lastKnownSpeed;
        Vector3 oppositePosition = transform.position + direction * lookAhead;
        oppositePosition.y = 0;
        navMeshAgent.destination = oppositePosition;
    }

    /// <summary>
    /// Gizmos to help see what's happening
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(centerWanderSphere, radius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(randomWanderDestination, 0.5f);
        if (navMeshAgent != null)
            Gizmos.DrawLine(transform.position, navMeshAgent.destination);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastKnownPosition, 1f);
    }

    /// <summary>
    /// Method that draws the perception cone
    /// For the amount of rays defined, we calculate the direction and check whether it hits an obstacle or not
    /// We define the different points and the triangles they form
    /// </summary>
    void DrawPerceptionCone()
    {
        int numberOfRays = (int)(numberOfRaysPerDegree * perceptionAngle);
        float degreesPerRay = perceptionAngle / numberOfRays;
        List<Vector3> castPoints = new List<Vector3>();

        for (int i = 0; i <= numberOfRays; i++)
        {
            float angle = transform.eulerAngles.y - perceptionAngle / 2 + degreesPerRay * i;
            Vector3 direction = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
            RaycastHit hit;
            CastInfo castInfo;
            if (Physics.Raycast(transform.position, direction, out hit, perceptionRadius, obstacleMask))
            {
                castInfo = new CastInfo(true, hit.point);
            }
            else
            {
                castInfo = new CastInfo(false, transform.position + direction * perceptionRadius);
            }
            castPoints.Add(castInfo.point);
        }

        int vertexCount = castPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        //We paint in relative position to the character
        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(castPoints[i]);

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        perceptionMesh.Clear();
        perceptionMesh.vertices = vertices;
        perceptionMesh.triangles = triangles;
        perceptionMesh.RecalculateNormals();
    }

    /// <summary>
    /// struct that defines the point of a ray for the perception cone and whether it hit an obstacle or not
    /// </summary>
    public struct CastInfo
    {
        public bool hit;
        public Vector3 point;

        public CastInfo(bool hit, Vector3 point)
        {
            this.hit = hit;
            this.point = point;
        }
    }
}
