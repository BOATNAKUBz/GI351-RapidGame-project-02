using UnityEngine;

[ExecuteAlways] // ทำให้โค้ดทำงานและอัปเดตใน Inspector ได้ทันทีโดยไม่ต้องกด Play
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class EditableTriangle : MonoBehaviour
{
    [Header("Triangle Vertices (พิกัดจุดยอด 3 จุด)")]
    public Vector3 topVertex = new Vector3(0f, 1f, 0f);
    public Vector3 bottomRightVertex = new Vector3(1f, 0f, 0f);
    public Vector3 bottomLeftVertex = new Vector3(-1f, 0f, 0f);

    [Header("Material & Color Settings")]
    public Color triangleColor = Color.cyan;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;

    private void OnEnable()
    {
        InitComponents();
        UpdateTriangle();
    }

    // เรียกทำงานอัตโนมัติเมื่อมีการแก้ไขค่าใน Inspector
    private void OnValidate()
    {
        InitComponents();
        UpdateTriangle();
    }

    private void InitComponents()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "CustomTriangleMesh";
            meshFilter.sharedMesh = mesh;
        }

        // สร้าง Material พื้นฐานหากยังไม่มี
        if (meshRenderer.sharedMaterial == null)
        {
            meshRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        if (propBlock == null) propBlock = new MaterialPropertyBlock();
    }

    public void UpdateTriangle()
    {
        if (mesh == null) return;

        // 1. กำหนดพิกัดจุดยอดจากค่าใน Inspector
        Vector3[] vertices = new Vector3[]
        {
            topVertex,          // จุดที่ 0
            bottomRightVertex,   // จุดที่ 1
            bottomLeftVertex    // จุดที่ 2
        };

        // 2. ลำดับการลากเส้นตามเข็มนาฬิกา
        int[] triangles = new int[] { 0, 1, 2 };

        // 3. กำหนดค่า UV
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0.5f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f)
        };

        // 4. อัปเดตข้อมูลเข้า Mesh
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 5. ปรับเปลี่ยนสีผ่าน MaterialPropertyBlock
        if (meshRenderer != null)
        {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", triangleColor);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }
}