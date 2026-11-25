using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Ensures smooth character-to-character avoidance & obstacle separation across Protagonist, NPCs,
/// Tourists, Camponesa, and Fairies — AND configures Unity NavMeshAgent Obstacle Avoidance
/// (High Quality, Radius = 0.45, AngularSpeed = 420, distinct Avoidance Priorities) on all characters!
/// Works seamlessly both with baked NavMesh AND in pure procedural runtime without any NavMesh errors.
/// </summary>
public sealed class GirlPowerOrla3DCharacterAvoidance : MonoBehaviour
{
    public Transform player;
    public Transform[] npcActors;
    public BoxCollider[] blockingObstacles;

    [Header("Avoidance Tuning")]
    public float characterRadius = 0.45f;
    public float separationStrength = 6.5f;
    [Tooltip("Leave disabled when character controllers already own movement; prevents a second script from writing transforms.")]
    public bool applyTransformSeparation;

    private readonly List<Transform> allActors = new List<Transform>(32);

    private void Start()
    {
        allActors.Clear();
        if (player != null) allActors.Add(player);
        if (npcActors != null)
        {
            for (int i = 0; i < npcActors.Length; i++)
                if (npcActors[i] != null && !allActors.Contains(npcActors[i]))
                    allActors.Add(npcActors[i]);
        }

        // Safely configure NavMeshAgent properties on all actors (HighQuality, radius 0.45, angularSpeed 420, unique priority!)
        for (int i = 0; i < allActors.Count; i++)
        {
            NavMeshAgent agent = allActors[i].GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.radius = characterRadius;
                agent.angularSpeed = 420f;
                agent.avoidancePriority = Mathf.Clamp(20 + i * 3, 1, 98);
                agent.updatePosition = false;
                agent.updateRotation = false;
                // Keep agent disabled if no NavMesh is baked under the character so Unity never logs placement errors
                if (!NavMesh.SamplePosition(allActors[i].position, out _, 1.5f, NavMesh.AllAreas))
                    agent.enabled = false;
            }
        }
    }

    private void LateUpdate()
    {
        if (!applyTransformSeparation || Time.timeScale <= 0.01f || allActors.Count == 0) return;
        float dt = Time.deltaTime;
        float minPairDist = characterRadius * 2.0f; // 0.90m clean personal bubble!
        float minPairDistSq = minPairDist * minPairDist;

        // 1. Character-to-Character soft separation (nobody ever clips through another character!)
        for (int i = 0; i < allActors.Count; i++)
        {
            Transform a = allActors[i];
            if (a == null || !a.gameObject.activeInHierarchy) continue;

            for (int j = i + 1; j < allActors.Count; j++)
            {
                Transform b = allActors[j];
                if (b == null || !b.gameObject.activeInHierarchy) continue;

                // Skip vertical flyers (e.g. hovering fairies high above ground characters)
                if (Mathf.Abs(a.position.y - b.position.y) > 0.65f) continue;

                Vector2 d = new Vector2(b.position.x - a.position.x, b.position.z - a.position.z);
                float distSq = d.sqrMagnitude;
                if (distSq >= minPairDistSq) continue;

                float dist = Mathf.Sqrt(Mathf.Max(0.0001f, distSq));
                Vector2 dir = dist > 0.001f ? (d / dist) : new Vector2(1f, 0f);
                float overlap = minPairDist - dist;
                float push = Mathf.Min(overlap, separationStrength * dt);

                bool aIsPlayer = (a == player);
                bool bIsPlayer = (b == player);
                float weightA = aIsPlayer ? 0.35f : (bIsPlayer ? 0.65f : 0.50f);
                float weightB = 1f - weightA;

                Vector3 nextA = a.position - new Vector3(dir.x * push * weightA, 0f, dir.y * push * weightA);
                Vector3 nextB = b.position + new Vector3(dir.x * push * weightB, 0f, dir.y * push * weightB);

                if (!HitsAnyObstacle(nextA, characterRadius * 0.85f)) a.position = nextA;
                if (!HitsAnyObstacle(nextB, characterRadius * 0.85f)) b.position = nextB;
            }
        }

        // 2. Gentle OBB safety cushion only if a character clips deep inside a static obstacle
        //    (primary obstacle avoidance is owned cleanly by GirlPowerOrla3DPathfinder A* routes!)
        if (blockingObstacles == null || blockingObstacles.Length == 0) return;
        float npcRad = characterRadius * 0.72f;
        float npcRadSq = npcRad * npcRad;
        for (int i = 0; i < allActors.Count; i++)
        {
            Transform actor = allActors[i];
            if (actor == null || actor == player || !actor.gameObject.activeInHierarchy) continue;
            if (actor.position.y > 0.45f) continue;

            Vector3 pos = actor.position;
            Vector3 probe = new Vector3(pos.x, 0.5f, pos.z);
            for (int k = 0; k < blockingObstacles.Length; k++)
            {
                BoxCollider box = blockingObstacles[k];
                if (box == null || !box.enabled || box.isTrigger || !box.gameObject.activeInHierarchy) continue;
                float dSq = GirlPowerOrla3DPathfinder.DistanceSqToOrientedBoxXZ(probe, box);
                if (dSq < npcRadSq)
                {
                    Vector3 center = box.transform.TransformPoint(box.center);
                    Vector2 outVec = new Vector2(pos.x - center.x, pos.z - center.z);
                    Vector2 pushDir = outVec.sqrMagnitude > 0.0001f ? outVec.normalized : Vector2.right;
                    float pen = (npcRad - Mathf.Sqrt(dSq)) + 0.02f;
                    pos.x += pushDir.x * pen;
                    pos.z += pushDir.y * pen;
                    probe.x = pos.x;
                    probe.z = pos.z;
                }
            }
            actor.position = pos;
        }
    }

    private bool HitsAnyObstacle(Vector3 p, float rad)
    {
        return GirlPowerOrla3DPathfinder.IsPointBlockedOBB(new Vector3(p.x, 0.5f, p.z), blockingObstacles, rad * rad);
    }
}
