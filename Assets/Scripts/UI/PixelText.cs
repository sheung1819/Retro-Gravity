using System.Collections.Generic;
using UnityEngine;

namespace Pulse
{
    /// <summary>
    /// Renders a string with <see cref="PixelFont"/> as a mesh of glowing dots,
    /// like an old LED/vector display. Rebuilds only when the text changes.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PixelText : MonoBehaviour
    {
        public enum Align { Left, Center, Right }

        private string text = "";
        private float pixelSize = 0.12f;
        private Align align = Align.Center;
        private Color color = Color.white;
        private Mesh mesh;
        private MeshRenderer meshRenderer;

        private readonly List<Vector3> verts = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> tris = new List<int>();

        public static PixelText Create(string name, Transform parent, float pixelSize, Align align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GlowKit.DotMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 20;
            var pt = go.AddComponent<PixelText>();
            pt.pixelSize = pixelSize;
            pt.align = align;
            pt.color = color;
            return pt;
        }

        private void Awake()
        {
            mesh = new Mesh { name = "PixelText" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }

        public string Text
        {
            get => text;
            set
            {
                value = value ?? "";
                if (value == text) return;
                text = value;
                Rebuild();
            }
        }

        public Color Color
        {
            get => color;
            set
            {
                if (value == color) return;
                color = value;
                Rebuild();
            }
        }

        public bool Visible
        {
            get => meshRenderer.enabled;
            set => meshRenderer.enabled = value;
        }

        /// <summary>Width of a string in world units at this text's pixel size.</summary>
        public float MeasureWidth(string s) => s.Length == 0 ? 0f : (s.Length * (PixelFont.Width + 1) - 1) * pixelSize;

        private void Rebuild()
        {
            if (mesh == null) return;
            verts.Clear(); uvs.Clear(); colors.Clear(); tris.Clear();

            float width = MeasureWidth(text);
            float startX = align == Align.Left ? 0f : align == Align.Center ? -width * 0.5f : -width;
            float top = PixelFont.Height * pixelSize * 0.5f;
            Color glow = new Color(color.r, color.g, color.b, color.a * 0.35f);

            for (int ci = 0; ci < text.Length; ci++)
            {
                string[] rows = PixelFont.Get(text[ci]);
                if (rows == null) continue;
                float gx = startX + ci * (PixelFont.Width + 1) * pixelSize;
                for (int row = 0; row < PixelFont.Height; row++)
                for (int col = 0; col < PixelFont.Width; col++)
                {
                    if (rows[row][col] != '1') continue;
                    var c = new Vector3(gx + (col + 0.5f) * pixelSize, top - (row + 0.5f) * pixelSize, 0f);
                    AddQuad(c, pixelSize * 2.6f, glow);  // halo
                    AddQuad(c, pixelSize * 1.25f, color); // hot core
                }
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        private void AddQuad(Vector3 c, float size, Color col)
        {
            float h = size * 0.5f;
            int i = verts.Count;
            verts.Add(c + new Vector3(-h, -h)); uvs.Add(new Vector2(0, 0));
            verts.Add(c + new Vector3(-h, h)); uvs.Add(new Vector2(0, 1));
            verts.Add(c + new Vector3(h, h)); uvs.Add(new Vector2(1, 1));
            verts.Add(c + new Vector3(h, -h)); uvs.Add(new Vector2(1, 0));
            for (int k = 0; k < 4; k++) colors.Add(col);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }
    }
}
