using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : Graphic
{
    [Tooltip("List of points (in local space) that form the line.")]
    public List<Vector2> Points = new List<Vector2>();
    
    [Tooltip("Line thickness in pixels.")]
    public float Thickness = 2f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (Points == null || Points.Count < 2)
            return;
        
        for (int i = 0; i < Points.Count - 1; i++)
        {
            Vector2 start = Points[i];
            Vector2 end = Points[i + 1];
            Vector2 direction = (end - start).normalized;
            // Get normal perpendicular to the segment.
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Vector2 offset = normal * (Thickness / 2f);

            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;
            
            // Four vertices (quad) per line segment.
            vert.position = start - offset;
            vh.AddVert(vert);
            vert.position = start + offset;
            vh.AddVert(vert);
            vert.position = end + offset;
            vh.AddVert(vert);
            vert.position = end - offset;
            vh.AddVert(vert);

            int startIndex = i * 4;
            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }
    }
} 