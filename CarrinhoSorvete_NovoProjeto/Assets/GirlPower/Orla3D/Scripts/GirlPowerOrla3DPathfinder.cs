using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fast, zero-NavMesh-bake 2D Oriented-Box (OBB) Collision & Smoothed A* Pathfinder for Orla das Fadas:
/// - Evaluates EVERY BoxCollider in its TRUE oriented local space (OBB) so rotated stalls, benches,
///   crates, and umbrellas NEVER block open walkway corners with inflated world-axis AABBs!
/// - Computes smooth, obstacle-bypassing walkable paths (grid A* + line-of-sight string pulling)
///   for BOTH mobile Tap-to-Move deliveries (e.g. tapping Florist across the Fair!) AND strolling villagers!
/// </summary>
public sealed class GirlPowerOrla3DPathfinder : MonoBehaviour
{
    public static GirlPowerOrla3DPathfinder Instance { get; private set; }

    public BoxCollider[] blockingObstacles;
    public float minX = -17.5f;
    public float maxX = 36.8f;
    public float minZ = -22.2f;
    public float maxZ = 21.5f;
    public float cellSize = 0.65f;
    public float bakeAgentRadius = 0.42f;

    private int cols;
    private int rows;
    private bool[] blocked;
    private bool gridReady;

    // Reusable A* scratch buffers (zero per-query GC allocation!)
    private float[] gScore;
    private float[] fScore;
    private int[] cameFrom;
    private int[] stamp;
    private int[] closedStamp;
    private int currentStamp = 1;
    private readonly List<int> openSet = new List<int>(256);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if ((blockingObstacles == null || blockingObstacles.Length == 0))
        {
            var story = FindObjectOfType<GirlPowerOrla3DFirstDelivery>();
            if (story != null) blockingObstacles = story.blockingObstacles;
        }
        RebuildGrid();
    }

    public void RebuildGrid()
    {
        cols = Mathf.Max(8, Mathf.CeilToInt((maxX - minX) / cellSize) + 1);
        rows = Mathf.Max(8, Mathf.CeilToInt((maxZ - minZ) / cellSize) + 1);
        int total = cols * rows;
        blocked = new bool[total];
        gScore = new float[total];
        fScore = new float[total];
        cameFrom = new int[total];
        stamp = new int[total];
        closedStamp = new int[total];

        float radSq = bakeAgentRadius * bakeAgentRadius;
        for (int r = 0; r < rows; r++)
        {
            float wz = minZ + r * cellSize;
            for (int c = 0; c < cols; c++)
            {
                float wx = minX + c * cellSize;
                Vector3 p = new Vector3(wx, 0.5f, wz);
                blocked[r * cols + c] = IsPointBlockedOBB(p, blockingObstacles, radSq);
            }
        }
        gridReady = true;
    }

    /// <summary>
    /// Exact Oriented Bounding Box (OBB) planar distance squared from world point p to BoxCollider box.
    /// Eliminates phantom corner blocking caused by axis-aligned BoxCollider.bounds on rotated props!
    /// </summary>
    public static float DistanceSqToOrientedBoxXZ(Vector3 worldPoint, BoxCollider box)
    {
        Transform tr = box.transform;
        Vector3 local = tr.InverseTransformPoint(worldPoint);
        Vector3 half = box.size * 0.5f;
        Vector3 c = box.center;

        Vector3 clamped = new Vector3(
            Mathf.Clamp(local.x, c.x - half.x, c.x + half.x),
            Mathf.Clamp(local.y, c.y - half.y, c.y + half.y),
            Mathf.Clamp(local.z, c.z - half.z, c.z + half.z));

        Vector3 closestWorld = tr.TransformPoint(clamped);
        float dx = worldPoint.x - closestWorld.x;
        float dz = worldPoint.z - closestWorld.z;
        return dx * dx + dz * dz;
    }

    public static bool IsPointBlockedOBB(Vector3 worldPoint, BoxCollider[] obstacles, float radiusSq)
    {
        if (obstacles == null) return false;
        for (int i = 0; i < obstacles.Length; i++)
        {
            BoxCollider box = obstacles[i];
            if (box == null || !box.enabled || box.isTrigger || !box.gameObject.activeInHierarchy) continue;
            if (DistanceSqToOrientedBoxXZ(worldPoint, box) < radiusSq)
                return true;
        }
        return false;
    }

    public static bool IsSegmentClearOBB(Vector3 a, Vector3 b, BoxCollider[] obstacles, float radius)
    {
        float dist = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        if (dist < 0.05f) return true;
        int steps = Mathf.Clamp(Mathf.CeilToInt(dist / 0.35f), 2, 96);
        float radSq = radius * radius;
        for (int i = 1; i < steps; i++)
        {
            float t = (float)i / steps;
            Vector3 p = new Vector3(Mathf.Lerp(a.x, b.x, t), 0.5f, Mathf.Lerp(a.z, b.z, t));
            if (IsPointBlockedOBB(p, obstacles, radSq))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Computes a clean, obstacle-avoiding smoothed path on XZ from start to goal.
    /// Automatically snaps blocked goal points (like tapping directly on a stall or Florist)
    /// to the nearest open walkable spot in front of it!
    /// </summary>
    public bool FindPath(Vector3 start, Vector3 goal, List<Vector3> outWaypoints, float clearanceRadius = 0.42f)
    {
        outWaypoints.Clear();
        if (!gridReady) RebuildGrid();

        // Fast path: straight line is 100% clear!
        Vector3 clampedGoal = new Vector3(Mathf.Clamp(goal.x, minX + 0.4f, maxX - 0.4f), 0f, Mathf.Clamp(goal.z, minZ + 0.4f, maxZ - 0.4f));
        if (!IsPointBlockedOBB(new Vector3(clampedGoal.x, 0.5f, clampedGoal.z), blockingObstacles, clearanceRadius * clearanceRadius)
            && IsSegmentClearOBB(start, clampedGoal, blockingObstacles, clearanceRadius))
        {
            outWaypoints.Add(clampedGoal);
            return true;
        }

        int startIdx = FindNearestWalkableCell(WorldToCol(start.x), WorldToRow(start.z));
        int goalIdx = FindNearestWalkableCell(WorldToCol(clampedGoal.x), WorldToRow(clampedGoal.z));
        if (startIdx < 0 || goalIdx < 0)
        {
            outWaypoints.Add(clampedGoal);
            return false;
        }
        if (startIdx == goalIdx)
        {
            outWaypoints.Add(CellToWorld(goalIdx));
            return true;
        }

        currentStamp++;
        if (currentStamp >= int.MaxValue - 4)
        {
            System.Array.Clear(stamp, 0, stamp.Length);
            System.Array.Clear(closedStamp, 0, closedStamp.Length);
            currentStamp = 1;
        }

        openSet.Clear();
        openSet.Add(startIdx);
        stamp[startIdx] = currentStamp;
        gScore[startIdx] = 0f;
        fScore[startIdx] = Heuristic(startIdx, goalIdx);
        cameFrom[startIdx] = -1;

        int[] dCol = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dRow = { 0, 0, 1, -1, 1, -1, 1, -1 };
        float[] dCost = { 1f, 1f, 1f, 1f, 1.4142f, 1.4142f, 1.4142f, 1.4142f };

        int bestSoFar = startIdx;
        float bestH = fScore[startIdx];
        int expanded = 0;

        while (openSet.Count > 0 && expanded < 2800)
        {
            expanded++;
            int bestOpenPos = 0;
            float lowestF = fScore[openSet[0]];
            for (int i = 1; i < openSet.Count; i++)
            {
                float f = fScore[openSet[i]];
                if (f < lowestF) { lowestF = f; bestOpenPos = i; }
            }

            int cur = openSet[bestOpenPos];
            openSet[bestOpenPos] = openSet[openSet.Count - 1];
            openSet.RemoveAt(openSet.Count - 1);

            if (cur == goalIdx)
            {
                bestSoFar = cur;
                break;
            }

            closedStamp[cur] = currentStamp;
            int cc = cur % cols;
            int cr = cur / cols;

            for (int n = 0; n < 8; n++)
            {
                int nc = cc + dCol[n];
                int nr = cr + dRow[n];
                if (nc < 0 || nc >= cols || nr < 0 || nr >= rows) continue;
                int ni = nr * cols + nc;
                if (blocked[ni] || closedStamp[ni] == currentStamp) continue;

                // Prevent diagonal corner cutting through blocked edges!
                if (n >= 4 && (blocked[cr * cols + nc] || blocked[nr * cols + cc]))
                    continue;

                float tentativeG = gScore[cur] + dCost[n];
                if (stamp[ni] != currentStamp || tentativeG < gScore[ni])
                {
                    cameFrom[ni] = cur;
                    gScore[ni] = tentativeG;
                    float h = Heuristic(ni, goalIdx);
                    fScore[ni] = tentativeG + h;
                    if (h < bestH) { bestH = h; bestSoFar = ni; }
                    if (stamp[ni] != currentStamp)
                    {
                        stamp[ni] = currentStamp;
                        openSet.Add(ni);
                    }
                }
            }
        }

        // Reconstruct raw grid path
        List<Vector3> raw = new List<Vector3>(64);
        int trace = bestSoFar;
        int guard = 0;
        while (trace >= 0 && trace != startIdx && guard++ < 512)
        {
            raw.Add(CellToWorld(trace));
            trace = cameFrom[trace];
        }
        raw.Reverse();

        if (raw.Count == 0)
        {
            outWaypoints.Add(CellToWorld(goalIdx));
            return false;
        }

        // String-pulling line-of-sight smoothing (turns blocky grid steps into natural smooth curves!)
        Vector3 anchor = new Vector3(start.x, 0f, start.z);
        int idx = 0;
        while (idx < raw.Count)
        {
            int furthest = idx;
            for (int probe = raw.Count - 1; probe > idx; probe--)
            {
                if (IsSegmentClearOBB(anchor, raw[probe], blockingObstacles, clearanceRadius))
                {
                    furthest = probe;
                    break;
                }
            }
            outWaypoints.Add(raw[furthest]);
            anchor = raw[furthest];
            idx = furthest + 1;
        }
        return bestSoFar == goalIdx;
    }

    private int FindNearestWalkableCell(int c, int r)
    {
        c = Mathf.Clamp(c, 0, cols - 1);
        r = Mathf.Clamp(r, 0, rows - 1);
        if (!blocked[r * cols + c]) return r * cols + c;
        for (int rad = 1; rad <= 8; rad++)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int dr = -rad; dr <= rad; dr++)
            {
                for (int dc = -rad; dc <= rad; dc++)
                {
                    if (Mathf.Abs(dr) != rad && Mathf.Abs(dc) != rad) continue;
                    int nc = c + dc;
                    int nr = r + dr;
                    if (nc < 0 || nc >= cols || nr < 0 || nr >= rows) continue;
                    int ni = nr * cols + nc;
                    if (!blocked[ni])
                    {
                        float d = dc * dc + dr * dr;
                        if (d < bestD) { bestD = d; best = ni; }
                    }
                }
            }
            if (best >= 0) return best;
        }
        return -1;
    }

    private float Heuristic(int a, int b)
    {
        int dx = Mathf.Abs((a % cols) - (b % cols));
        int dy = Mathf.Abs((a / cols) - (b / cols));
        return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
    }

    private int WorldToCol(float x) => Mathf.Clamp(Mathf.RoundToInt((x - minX) / cellSize), 0, cols - 1);
    private int WorldToRow(float z) => Mathf.Clamp(Mathf.RoundToInt((z - minZ) / cellSize), 0, rows - 1);
    private Vector3 CellToWorld(int idx) => new Vector3(minX + (idx % cols) * cellSize, 0f, minZ + (idx / cols) * cellSize);
}
