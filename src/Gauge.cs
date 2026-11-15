using System.Reflection;
using Alta.Caves;
using UnityEngine;

namespace Televator200
{
    internal static class Gauge
    {
        private const string ReadoutName = "Televator Floor Readout";
        private static readonly FieldInfo PipParent = TelevatorMod.Field<LevelSelector>("pipParent");
        private static readonly FieldInfo LastPip = TelevatorMod.Field<LevelSelector>("lastPipPosition");
        private static readonly FieldInfo Gauges = TelevatorMod.Field<LevelSelector>("gauges");
        private static readonly FieldInfo RenderGroup = TelevatorMod.Field<LevelSelector>("baseRendererGroup");
        private static readonly FieldInfo Cost = TelevatorMod.Field<CaveTeleporter>("costRenderer");
        private static Mesh backingMesh;
        private static Mesh stoneCourseMesh;
        private static Mesh topRungMesh;
        private static Mesh sigilMesh;

        internal static void Extend(LevelSelector selector)
        {
            var first = (Transform)PipParent.GetValue(selector);
            var last = (Transform)LastPip.GetValue(selector);
            Vector3 extension = (first.position - last.position) *
                (TelevatorMod.Stops - TelevatorMod.OriginalStops) / (TelevatorMod.OriginalStops - 1);
            // The original pip shells still meet the stone at this offset and clear its front lip.
            Vector3 clearance = selector.transform.TransformVector(Vector3.forward * 0.28f);
            first.position += extension + clearance;
            last.position += clearance;

            // The stock rail and all its LODs share a parent and a vertical axis.
            // Keep the original lower lip fixed and add exactly the length gained by the pips.
            var renderers = (Renderer[])Gauges.GetValue(selector);
            var near = renderers[0].GetComponent<MeshFilter>();
            var rail = near.transform.parent;
            Bounds bounds = near.sharedMesh.bounds;
            Vector3 bottom = near.transform.TransformPoint(bounds.min);
            Vector3 top = near.transform.TransformPoint(bounds.min + Vector3.up * bounds.size.y);
            Vector3 localBottom = rail.InverseTransformPoint(bottom);
            float addedHeight = Vector3.Dot(extension, (top - bottom).normalized);
            float factor = 1f + addedHeight / Vector3.Distance(top, bottom);
            rail.localScale = Vector3.Scale(rail.localScale, new Vector3(1f, factor, 1f));
            rail.position += bottom - rail.TransformPoint(localBottom) + clearance;

            ExtendBacking(selector, first.position);
        }

        private static void ExtendBacking(LevelSelector selector, Vector3 topPip)
        {
            var stock = selector.transform.Find("Mesh/caveTeleporter_obelisk");
            var stockRenderer = selector.transform.Find("Mesh/Joined Mesh for Ruins_02_glow_m_0 LOD0")?.GetComponent<MeshRenderer>();
            if (stock == null || stockRenderer == null) return;

            // Join the extension just inside the original obelisk's top bevel.
            const float seamHeight = 5.76f;
            float firstStopHeight = stock.InverseTransformPoint(topPip).y;
            if (stoneCourseMesh == null) stoneCourseMesh = StoneBacking.CreateStoneCourse();
            if (topRungMesh == null) topRungMesh = StoneBacking.CreateTopRung();
            if (sigilMesh == null) sigilMesh = TelelevatorSigil.Create();

            float capHeight = firstStopHeight + 0.32f;
            if (backingMesh == null) backingMesh = StoneBacking.Create();
            var group = (RendererGroup)RenderGroup.GetValue(selector);
            AddStone(backingMesh, new Vector3(0f, seamHeight, -0.01f), new Vector3(0.998f, capHeight - seamHeight, 1f));
            AddStone(stoneCourseMesh, Vector3.zero, Vector3.one);
            AddStone(topRungMesh, Vector3.up * firstStopHeight, Vector3.one);
            if (sigilMesh != null)
            {
                // Seat the bottom spoke inside the cap so the sigil stands on the stone.
                float sigilHeight = capHeight - sigilMesh.bounds.min.y - 0.055f;
                AddStone(sigilMesh, new Vector3(0f, sigilHeight, -3.78f), new Vector3(1f, 1f, 0.60f));
            }

            void AddStone(Mesh mesh, Vector3 placement, Vector3 scale)
            {
                var piece = new GameObject(mesh.name);
                piece.layer = stockRenderer.gameObject.layer;
                piece.transform.SetParent(stock, false);
                piece.transform.localPosition = placement;
                piece.transform.localScale = scale;
                piece.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = piece.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = stockRenderer.sharedMaterial;
                group.Components.Add(renderer);
            }
        }

        internal static void AddReadout(LevelSelectCaveTeleporter owner)
        {
            var cost = (TextRenderer)Cost.GetValue(owner);
            var wheel = owner.GetComponentInChildren<ScrollWheel>();
            if (cost == null || wheel == null) return;
            var floor = Object.Instantiate(cost, cost.transform.parent, false);
            floor.name = ReadoutName;
            floor.Scale *= 1.6f;
            floor.transform.position = wheel.transform.position + owner.transform.TransformVector(Vector3.up * 0.53f);
            floor.Text = TelevatorMod.FloorsPerStop.ToString();
        }

        internal static void UpdateReadout(LevelSelectCaveTeleporter owner, int floorNumber)
        {
            var cost = (TextRenderer)Cost.GetValue(owner);
            if (cost == null) return;
            var floor = cost.transform.parent.Find(ReadoutName)?.GetComponent<TextRenderer>();
            string number = floorNumber.ToString();
            if (floor != null && floor.Text != number) floor.Text = number;
        }
    }
}
