using System.Collections.Generic;
using UnityEngine;

namespace Televator200
{
    internal static class StoneBacking
    {
        private const float Front = -3.4704f;

        internal static Mesh Create()
        {
            // Match the lower obelisk's footprint, corner bevels and stone atlas coordinates.
            const float front = Front;
            const float back = -4.7157f;
            const float halfWidth = 1.3006f;
            const float bevel = 0.0622f;
            const float shoulderHeight = 0.9814f;
            const float innerWidth = halfWidth - bevel;
            Vector2[] outline =
            {
                new Vector2(0f, front), new Vector2(innerWidth, front),
                new Vector2(halfWidth, front - bevel), new Vector2(halfWidth, back + bevel),
                new Vector2(innerWidth, back), new Vector2(0f, back),
                new Vector2(-innerWidth, back), new Vector2(-halfWidth, back + bevel),
                new Vector2(-halfWidth, front - bevel), new Vector2(-innerWidth, front)
            };
            float[] atlasU = { 0.4474f, 0.3367f, 0.3253f, 0.2227f, 0.2113f, 0.0980f, 0.2113f, 0.2227f, 0.3253f, 0.3367f };
            var surface = new Surface();

            Vector3 CapPoint(Vector2 point) => new Vector3(Mathf.Clamp(point.x, -innerWidth, innerWidth),
                1f, Mathf.Clamp(point.y, back + bevel, front - bevel));
            Vector2 CapUV(Vector3 point) => new Vector2(Mathf.Lerp(0.44995f, 0.33667f, Mathf.Abs(point.x) / innerWidth),
                Mathf.Lerp(0.87646f, 0.979f, Mathf.InverseLerp(front - bevel, back + bevel, point.z)));

            var center = new Vector3(0f, 1f, (front + back) * 0.5f);
            for (int i = 0; i < outline.Length; i++)
            {
                int next = (i + 1) % outline.Length;
                Vector2 a = outline[i];
                Vector2 b = outline[next];
                var lowA = new Vector3(a.x, 0f, a.y);
                var lowB = new Vector3(b.x, 0f, b.y);
                var highA = new Vector3(a.x, shoulderHeight, a.y);
                var highB = new Vector3(b.x, shoulderHeight, b.y);
                Vector3 topA = CapPoint(a);
                Vector3 topB = CapPoint(b);
                var lowUvA = new Vector2(atlasU[i], 0.3991f);
                var lowUvB = new Vector2(atlasU[next], 0.3991f);
                var highUvA = new Vector2(atlasU[i], 0.671f);
                var highUvB = new Vector2(atlasU[next], 0.671f);
                var topUvA = new Vector2(atlasU[i], 0.677f);
                var topUvB = new Vector2(atlasU[next], 0.677f);

                surface.AddQuad(lowA, lowB, highB, highA, lowUvA, lowUvB, highUvB, highUvA);
                surface.AddTriangle(highA, highB, topB, highUvA, highUvB, topUvB);
                if (topA == topB) continue;
                surface.AddTriangle(highA, topB, topA, highUvA, topUvB, topUvA);
                surface.AddTriangle(center, topA, topB, CapUV(center), CapUV(topA), CapUV(topB));
            }

            return surface.CreateMesh("Televator Stone Backing");
        }

        internal static Mesh CreateStoneCourse()
        {
            // The old rung shares the combined ruins mesh. Continue the stone face over it,
            // joining the surface again at the lower edge and inside the new upper wall.
            float[] columns = { -1.300f, -1.228f, 0f, 1.228f, 1.300f };
            float[] heights = { 4.80f, 5.24f, 5.70f, 5.86f };
            float[] depths = { Front + 0.009f, -3.253f, -3.253f, Front - 0.020f };
            float[] mapping = { 0.3253f, 0.3367f, 0.44995f, 0.3367f, 0.3253f };
            var surface = new Surface();

            Vector3 Point(int column, int row) => new Vector3(columns[column], heights[row],
                column == 0 || column == columns.Length - 1 ? Front - 0.066f : depths[row]);
            Vector2 UV(int column, int row) => new Vector2(mapping[column], 0.39917f + (heights[row] - 0.3395f) * 0.08547f);

            for (int row = 0; row < heights.Length - 1; row++)
            for (int col = 0; col < columns.Length - 1; col++)
            {
                surface.AddQuad(Point(col, row), Point(col + 1, row), Point(col + 1, row + 1), Point(col, row + 1),
                    UV(col, row), UV(col + 1, row), UV(col + 1, row + 1), UV(col, row + 1));
            }

            return surface.CreateMesh("Televator Restored Stone Course");
        }

        internal static Mesh CreateTopRung()
        {
            const float halfWidth = 0.85298f;
            const float halfHeight = 0.15382f;
            const float face = -3.27539f;
            const float back = -3.75546f;
            var leftBottom = new Vector3(-halfWidth, -halfHeight, face);
            var rightBottom = new Vector3(halfWidth, -halfHeight, face);
            var leftTop = new Vector3(-halfWidth, halfHeight, face);
            var rightTop = new Vector3(halfWidth, halfHeight, face);
            var leftBottomBack = new Vector3(-halfWidth, -halfHeight, back);
            var rightBottomBack = new Vector3(halfWidth, -halfHeight, back);
            var leftTopBack = new Vector3(-halfWidth, halfHeight, back);
            var rightTopBack = new Vector3(halfWidth, halfHeight, back);
            var surface = new Surface();

            const float leftU = 0.73926f;
            const float rightU = 0.07196f;
            const float bottomV = 0.33203f;
            const float topV = 0.24475f;
            var uvLeftBottom = new Vector2(leftU, bottomV);
            var uvRightBottom = new Vector2(rightU, bottomV);
            var uvLeftTop = new Vector2(leftU, topV);
            var uvRightTop = new Vector2(rightU, topV);
            surface.AddQuad(leftBottom, rightBottom, rightTop, leftTop, uvLeftBottom, uvRightBottom, uvRightTop, uvLeftTop);
            surface.AddQuad(leftTop, rightTop, rightTopBack, leftTopBack, uvLeftTop, uvRightTop, uvRightBottom, uvLeftBottom);
            surface.AddQuad(rightBottom, leftBottom, leftBottomBack, rightBottomBack, uvRightBottom, uvLeftBottom, uvLeftTop, uvRightTop);
            surface.AddQuad(leftTop, leftTopBack, leftBottomBack, leftBottom,
                uvLeftTop, new Vector2(0.83984f, topV), new Vector2(0.83984f, bottomV), uvLeftBottom);
            surface.AddQuad(rightBottom, rightBottomBack, rightTopBack, rightTop,
                new Vector2(0.58252f, 0.23779f), new Vector2(0.50537f, 0.23804f),
                new Vector2(0.50537f, 0.31494f), new Vector2(0.58252f, 0.31470f));
            return surface.CreateMesh("Televator Top Rung");
        }

        private sealed class Surface
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();

            internal void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
            {
                int start = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                uvs.Add(uvA);
                uvs.Add(uvB);
                uvs.Add(uvC);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }

            internal void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
            {
                AddTriangle(a, b, c, uvA, uvB, uvC);
                AddTriangle(a, c, d, uvA, uvC, uvD);
            }

            internal Mesh CreateMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                return mesh;
            }
        }
    }
}
