using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Enemy_VisionCone : MonoBehaviour
{
    [Header("Link (optional)")]
    public Enemy_ChaseAttack3 sourceAI;            // 비워두면 부모에서 자동 탐색

    [Header("Shape")]
    public float range = 10f;
    [Range(1f, 180f)] public float angle = 120f;  // 전체 각도
    [Min(3)] public int segments = 36;
    public float yOffset = 0.02f;                 // 바닥 깜빡임 방지

    [Header("Look")]
    public Material material;                     // 투명 머티리얼(권장: Legacy Transparent)
    public bool visible = true;

    Mesh mesh;
    MeshFilter mf;
    MeshRenderer mr;

    // 최적화: 값이 바뀔 때만 메쉬 재생성
    float lastRange, lastAngle;
    int lastSegments;
    bool lastVisible;

    void Awake()
    {
        Init();
        SyncFromAI(force: true);
        RebuildIfNeeded(force: true);
    }

    void OnEnable()
    {
        Init();
        SyncFromAI(force: true);
        RebuildIfNeeded(force: true);
    }

    void OnValidate()
    {
        if (segments < 3) segments = 3;
        if (range < 0.01f) range = 0.01f;

        Init();
        SyncFromAI(force: false);
        RebuildIfNeeded(force: true);
    }

    void LateUpdate()
    {
        Init();
        SyncFromAI(force: false);
        RebuildIfNeeded(force: false);
    }

    void Init()
    {
        if (!mf) mf = GetComponent<MeshFilter>();
        if (!mr) mr = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "VisionConeMesh";
            mf.sharedMesh = mesh;
        }
        else if (mf.sharedMesh != mesh)
        {
            mf.sharedMesh = mesh;
        }

        if (material && mr.sharedMaterial != material)
            mr.sharedMaterial = material;

        // visible 토글
        if (mr.enabled != visible)
            mr.enabled = visible;
    }

    void SyncFromAI(bool force)
    {
        if (!sourceAI)
            sourceAI = GetComponentInParent<Enemy_ChaseAttack3>();

        if (!sourceAI) return;

        // AI 값을 따라가도록
        float newRange = sourceAI.detectRange;
        float newAngle = sourceAI.viewAngle;

        if (force || !Mathf.Approximately(range, newRange)) range = newRange;
        if (force || !Mathf.Approximately(angle, newAngle)) angle = newAngle;
    }

    void RebuildIfNeeded(bool force)
    {
        if (!mesh) return;

        
        if (!force)
        {
            if (Mathf.Approximately(lastRange, range) &&
                Mathf.Approximately(lastAngle, angle) &&
                lastSegments == segments &&
                lastVisible == visible)
            {
                return;
            }
        }

        lastRange = range;
        lastAngle = angle;
        lastSegments = segments;
        lastVisible = visible;

        RebuildMesh();
    }

    void RebuildMesh()
    {
        int vertCount = segments + 2; // center + arc points
        Vector3[] verts = new Vector3[vertCount];
        int[] tris = new int[segments * 3];

        // center
        verts[0] = new Vector3(0f, yOffset, 0f);

        float half = angle * 0.5f;
        float step = angle / segments;

        // arc points (local space, Z forward)
        for (int i = 0; i <= segments; i++)
        {
            float a = -half + step * i;
            float rad = a * Mathf.Deg2Rad;

            float x = Mathf.Sin(rad) * range;
            float z = Mathf.Cos(rad) * range;

            verts[i + 1] = new Vector3(x, yOffset, z);
        }

        // triangles (fan)
        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            tris[t++] = 0;
            tris[t++] = i + 1;
            tris[t++] = i + 2;
        }

        Vector2[] uvs = new Vector2[segments + 2];
        uvs[0] = new Vector2(0f, 0f);
        for (int i = 0; i <= segments; i++)
            uvs[i + 1] = new Vector2(1f, 0f);

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}