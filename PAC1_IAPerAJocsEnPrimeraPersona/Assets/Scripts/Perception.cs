using System.Collections.Generic;
using UnityEngine;

public class Perception : MonoBehaviour
{
    [SerializeField] private float perceptionRadius = 5; // radius of perception
    [Range(0, 360)][SerializeField] private float perceptionAngle = 30; // full angle of perception
    [SerializeField] private LayerMask agentMask; // layer mask for the agents
    [SerializeField] private LayerMask obstacleMask; // layer mask for the obstacles
    [SerializeField] private GameObject agentSeenIndicator; // perception indicator
    [SerializeField] private int numberOfRaysPerDegree; // number of rays per degree, used for the visual cone
    [SerializeField] private MeshFilter perceptionMeshFilter; // MeshFilter used for the visual cone

    private Mesh perceptionMesh; // Mesh component for the visual cone

    /// <summary>
    /// We get the Wander component and initialize the visual cone mesh
    /// </summary>
    private void Start()
    {
        perceptionMesh = new Mesh();
        perceptionMesh.name = "Perception Mesh";
        perceptionMeshFilter.mesh = perceptionMesh;
    }

    /// <summary>
    /// Update method where we call Perceive()
    /// </summary>
    private void Update()
    {
        Perceive();
    }

    /// <summary>
    /// Method where we tell the Wander component whether we are seeing an agent or not
    /// We find the agents in the radius
    /// We check if they are in the perception angle
    /// We check if there's any obstacle in between
    /// </summary>
    private void Perceive()
    {
        AgentSeen(false);
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
                    AgentSeen(true);
                }
            }
        }
    }

    /// <summary>
    /// Method to control whether we have seen an agent or not
    /// </summary>
    /// <param name="seen">bool that defines if we've seen an agent</param>
    private void AgentSeen(bool seen)
    {
        agentSeenIndicator.SetActive(seen);
    }

    /// <summary>
    /// LateUpdate to call DrawPerceptionCone()
    /// </summary>
    private void LateUpdate()
    {
        DrawPerceptionCone();
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

        for(int i = 0; i <= numberOfRays; i++)
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
                castInfo = new CastInfo(false, transform.position + direction*perceptionRadius);
            }
            castPoints.Add(castInfo.point);
        }

        int vertexCount = castPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        //We paint in relative position to the character
        vertices[0] = Vector3.zero;
        for(int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(castPoints[i]);

            if(i < vertexCount - 2)
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
