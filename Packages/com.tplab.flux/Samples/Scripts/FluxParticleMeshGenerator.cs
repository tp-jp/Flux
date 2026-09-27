using UnityEngine;

namespace TpLab.Flux.Samples.Scripts
{
    [RequireComponent(typeof(MeshFilter))]
    public class FluxParticleMeshGenerator : MonoBehaviour
    {
        [SerializeField]
        int count = 16;

        [SerializeField]
        float size = 0.1f;

        void Awake()
        {
            var mesh = new Mesh();

            var vertices = new Vector3[count * 4];
            var uv = new Vector2[count * 4];
            var uv2 = new Vector2[count * 4];
            var triangles = new int[count * 6];

            var halfSize = size * 0.5f;

            for (var i = 0; i < count; i++)
            {
                var vertexIndex = i * 4;
                var triangleIndex = i * 6;

                vertices[vertexIndex] = new Vector3(-halfSize, -halfSize, 0);
                vertices[vertexIndex + 1] = new Vector3(halfSize, -halfSize, 0);
                vertices[vertexIndex + 2] = new Vector3(halfSize, halfSize, 0);
                vertices[vertexIndex + 3] = new Vector3(-halfSize, halfSize, 0);

                uv[vertexIndex] = new Vector2(0, 0);
                uv[vertexIndex + 1] = new Vector2(1, 0);
                uv[vertexIndex + 2] = new Vector2(1, 1);
                uv[vertexIndex + 3] = new Vector2(0, 1);

                uv2[vertexIndex] = new Vector2(i, 0);
                uv2[vertexIndex + 1] = new Vector2(i, 0);
                uv2[vertexIndex + 2] = new Vector2(i, 0);
                uv2[vertexIndex + 3] = new Vector2(i, 0);

                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 1;
                triangles[triangleIndex + 2] = vertexIndex + 2;

                triangles[triangleIndex + 3] = vertexIndex;
                triangles[triangleIndex + 4] = vertexIndex + 2;
                triangles[triangleIndex + 5] = vertexIndex + 3;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.uv2 = uv2;
            mesh.triangles = triangles;

            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}