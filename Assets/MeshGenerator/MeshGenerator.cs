using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
  public float width = 10;
  public float height = 10;
  public float depth = 10;
  public int subdivisions = 10;
  [Range(0.0f, 1.0f)]
  public float ciel = 0.5f;

  public bool useSeed = true;
  [ConditionalHide(nameof(useSeed), true)]
  public int seed = 3527;
  public float scale = 0.5f;

  Mesh mesh;
  List<Vector3> vertecies;
  List<Vector2> uvs;
  List<int> triangles;

  GameObject[,,] spheres;
  bool settingsUpdated = false;

  // Start is called before the first frame update
  void Start()
  {
    GenerateMesh();
    UpdateMesh();
  }


  void OnValidate()
  {
    settingsUpdated = true;
  }


  void Update()
  {
    if (settingsUpdated)
    {
      GenerateMesh();
      UpdateMesh();
      settingsUpdated = false;
    }
  }

  void GenerateMesh()
  {
    if (useSeed)
    {
      PerlinNoise.GeneratePermutation(seed);
    }
    else
    {
      PerlinNoise.GenerateOriginialPerlinPermutation();
    }

    GetComponent<MeshFilter>().mesh = mesh = new Mesh();

    vertecies = new List<Vector3>();
    uvs = new List<Vector2>();
    triangles = new List<int>();

    Vector3 start = new Vector3(-(float)width / 2, -(float)height / 2, -(float)depth / 2);
    Matrix4x4 oneRelativeSpace = new Matrix4x4(
      new Vector4(1 / width, 0, 0, 0),
      new Vector4(0, 1 / height, 0, 0),
      new Vector4(0, 0, 1 / depth, 0),
      new Vector4(0, 0, 0, 0)
    );

    for (int x = 0; x < subdivisions; x++)
    {
      for (int y = 0; y < subdivisions; y++)
      {
        for (int z = 0; z < subdivisions; z++)
        {
          Vector3[] points = {
            start + new Vector3( x      * width / subdivisions,  y      * height / subdivisions,  z      * depth / subdivisions),
            start + new Vector3((x + 1) * width / subdivisions,  y      * height / subdivisions,  z      * depth / subdivisions),
            start + new Vector3((x + 1) * width / subdivisions, (y + 1) * height / subdivisions,  z      * depth / subdivisions),
            start + new Vector3( x      * width / subdivisions, (y + 1) * height / subdivisions,  z      * depth / subdivisions),

            start + new Vector3( x      * width / subdivisions,  y      * height / subdivisions, (z + 1) * depth / subdivisions),
            start + new Vector3((x + 1) * width / subdivisions,  y      * height / subdivisions, (z + 1) * depth / subdivisions),
            start + new Vector3((x + 1) * width / subdivisions, (y + 1) * height / subdivisions, (z + 1) * depth / subdivisions),
            start + new Vector3( x      * width / subdivisions, (y + 1) * height / subdivisions, (z + 1) * depth / subdivisions)
          };

          float[] pointValues = new float[8];
          for (int i = 0; i < 8; i++)
          {
            var point = points[i] - start;
            if (point.x < 0.0001 || point.y < 0.0001 || point.z < 0.0001 || Mathf.Abs(point.x - width) < 0.0001 || Mathf.Abs(point.y - height) < 0.0001 || Mathf.Abs(point.z - depth) < 0.0001)
              pointValues[i] = 0;
            else
              pointValues[i] = (PerlinNoise.Get3D(points[i] * scale) * (1 - point.y / depth) + (1 - point.y / depth)) / 2;
          }

          /*
            Determine the index into the edge table which
            tells us which vertices are inside of the surface
          */
          int cubeindex = 0;
          if (pointValues[0] < ciel) cubeindex |= 1;
          if (pointValues[1] < ciel) cubeindex |= 2;
          if (pointValues[2] < ciel) cubeindex |= 4;
          if (pointValues[3] < ciel) cubeindex |= 8;
          if (pointValues[4] < ciel) cubeindex |= 16;
          if (pointValues[5] < ciel) cubeindex |= 32;
          if (pointValues[6] < ciel) cubeindex |= 64;
          if (pointValues[7] < ciel) cubeindex |= 128;

          /* Cube is entirely in/out of the surface */
          if (CubeConfigurationTables.Edges[cubeindex] == 0)
            continue;

          Vector3[] vertlist = new Vector3[12];
          /* Find the vertices where the surface intersects the cube */
          if ((CubeConfigurationTables.Edges[cubeindex] & 1) == 1)
            vertlist[0] =
               VertexInterp(points[0], points[1], pointValues[0], pointValues[1]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 2) == 2)
            vertlist[1] =
               VertexInterp(points[1], points[2], pointValues[1], pointValues[2]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 4) == 4)
            vertlist[2] =
               VertexInterp(points[2], points[3], pointValues[2], pointValues[3]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 8) == 8)
            vertlist[3] =
               VertexInterp(points[3], points[0], pointValues[3], pointValues[0]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 16) == 16)
            vertlist[4] =
               VertexInterp(points[4], points[5], pointValues[4], pointValues[5]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 32) == 32)
            vertlist[5] =
               VertexInterp(points[5], points[6], pointValues[5], pointValues[6]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 64) == 64)
            vertlist[6] =
               VertexInterp(points[6], points[7], pointValues[6], pointValues[7]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 128) == 128)
            vertlist[7] =
               VertexInterp(points[7], points[4], pointValues[7], pointValues[4]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 256) == 256)
            vertlist[8] =
               VertexInterp(points[0], points[4], pointValues[0], pointValues[4]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 512) == 512)
            vertlist[9] =
               VertexInterp(points[1], points[5], pointValues[1], pointValues[5]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 1024) == 1024)
            vertlist[10] =
               VertexInterp(points[2], points[6], pointValues[2], pointValues[6]);
          if ((CubeConfigurationTables.Edges[cubeindex] & 2048) == 2048)
            vertlist[11] =
               VertexInterp(points[3], points[7], pointValues[3], pointValues[7]);

          /* Create the triangle */
          for (int i = 0; CubeConfigurationTables.triangles[cubeindex, i] != -1; i += 3)
          {
            Vector3[] triangle = {
              vertlist[CubeConfigurationTables.triangles[cubeindex, i    ]],
              vertlist[CubeConfigurationTables.triangles[cubeindex, i + 1]],
              vertlist[CubeConfigurationTables.triangles[cubeindex, i + 2]],
            };

            foreach (var triangleVertecie in triangle)
            {
              Vector3 vertecie = oneRelativeSpace * (triangleVertecie - start);
              vertecies.Add(triangleVertecie);
              uvs.Add(new Vector2(vertecie.x, vertecie.y));
              triangles.Add(vertecies.Count - 1);
            }
          }
        }
      }
    }
  }

  /*
     Linearly interpolate the position where an isosurface cuts
     an edge between two vertices, each with their own scalar value
  */
  Vector3 VertexInterp(Vector3 p1, Vector3 p2, float valp1, float valp2)
  {
    float mu;
    Vector3 p;

    if (Mathf.Abs(ciel - valp1) < 0.00001)
      return (p1);
    if (Mathf.Abs(ciel - valp2) < 0.00001)
      return (p2);
    if (Mathf.Abs(valp1 - valp2) < 0.00001)
      return (p1);
    mu = (ciel - valp1) / (valp2 - valp1);
    p.x = p1.x + mu * (p2.x - p1.x);
    p.y = p1.y + mu * (p2.y - p1.y);
    p.z = p1.z + mu * (p2.z - p1.z);

    return (p);
  }

  void UpdateMesh()
  {
    mesh.Clear();

    mesh.vertices = vertecies.ToArray();
    mesh.triangles = triangles.ToArray();
    mesh.uv = uvs.ToArray();
    mesh.RecalculateNormals();
    mesh.RecalculateUVDistributionMetrics();
    GetComponent<MeshCollider>().sharedMesh = mesh;
  }
}
