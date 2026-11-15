using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Televator200
{
    internal static class TelelevatorSigil
    {
        private const string RuinsPrefab = "Network Prefabs/Environment/Mining Environment Prefabs/Cave Teleporter Ruins Static";

        internal static Mesh Create()
        {
            var ruins = Resources.Load<GameObject>(RuinsPrefab);
            if (ruins == null) return null;

            Mesh FindMesh(string objectName) => ruins.transform.Find(objectName + "/" + objectName + "_LOD0")
                ?.GetComponent<MeshFilter>()?.sharedMesh;

            var ring = FindMesh("caveTeleporterStation_sigil_circle");
            var core = FindMesh("caveTeleporterStation_sigil_core");
            var center = FindMesh("caveTeleporterStation_sigil_coreCircle");
            var looseSpokes = FindMesh("caveTeleporterBrokenStation_sigil_points");
            if (ring == null || core == null || center == null || looseSpokes == null || !looseSpokes.isReadable)
                return null;

            var spoke = FirstSpoke(looseSpokes);
            if (spoke == null) return null;

            // The repaired station uses these same pieces. Normalize the original ring pivot,
            // rotate the salvaged spoke into its upright position, and set the other five around it.
            var origin = Matrix4x4.Translate(new Vector3(0f, -5.606238f, 4.146308f));
            var parts = new List<CombineInstance>
            {
                new CombineInstance { mesh = ring, transform = origin },
                new CombineInstance { mesh = core, transform = origin },
                new CombineInstance { mesh = center, transform = origin }
            };
            var upright = Quaternion.LookRotation(new Vector3(-0.599907f, -0.800045f, -0.006311f),
                new Vector3(-0.023603f, 0.009813f, 0.999673f));
            var uprightSpoke = origin * Matrix4x4.TRS(new Vector3(-2.629581f, 6.016824f, -4.275171f), upright, Vector3.one);
            for (int i = 0; i < 6; i++)
            {
                parts.Add(new CombineInstance
                {
                    mesh = spoke,
                    transform = Matrix4x4.Rotate(Quaternion.AngleAxis(i * 60f, Vector3.forward)) * uprightSpoke
                });
            }

            var sigil = new Mesh { name = "Televator Restored Sigil" };
            sigil.CombineMeshes(parts.ToArray());
            Object.Destroy(spoke);
            return sigil;
        }

        private static Mesh FirstSpoke(Mesh source)
        {
            const int firstSpokeVertices = 47;
            if (source.vertexCount < firstSpokeVertices) return null;
            int[] indices = source.GetTriangles(0);
            var triangles = new List<int>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                if (indices[i] >= firstSpokeVertices || indices[i + 1] >= firstSpokeVertices || indices[i + 2] >= firstSpokeVertices)
                    continue;
                triangles.Add(indices[i]);
                triangles.Add(indices[i + 1]);
                triangles.Add(indices[i + 2]);
            }
            if (triangles.Count != 44 * 3) return null;

            var spoke = new Mesh
            {
                vertices = source.vertices.Take(firstSpokeVertices).ToArray(),
                normals = source.normals.Take(firstSpokeVertices).ToArray(),
                tangents = source.tangents.Take(firstSpokeVertices).ToArray(),
                uv = source.uv.Take(firstSpokeVertices).ToArray()
            };
            spoke.SetTriangles(triangles, 0);
            return spoke;
        }
    }
}
