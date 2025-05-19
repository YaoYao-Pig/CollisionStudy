using UnityEditor;
using UnityEngine;

public struct GizmosDrawer {
    public bool enable;
        public GizmosDrawer(bool enable)
        {
            this.enable = enable;
        }

        public void DrawTriangle2D(Triangle2D triangle, Color color, bool showLabel = false)
        {
            if (!enable)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawLine(triangle.a, triangle.b);
            Gizmos.DrawLine(triangle.c, triangle.b);
            Gizmos.DrawLine(triangle.a, triangle.c);

            if (showLabel)
            {
                Handles.Label(triangle.a, "a");
                Handles.Label(triangle.b, "b");
                Handles.Label(triangle.c, "c");
            }
        }

        public void DrawArrow(Vector3 position, Vector3 direction, Color color, string label = null)
        {
            if (!enable)
            {
                return;
            }

            using (new Handles.DrawingScope(color))
            {
                var size = 0.12f;
                var endPoint = position + direction * size;
                if (label != null)
                {
                    Handles.Label(endPoint, label);
                }
                Handles.ArrowHandleCap(-1, position, Quaternion.FromToRotation(Vector3.forward, direction), size, EventType.Repaint);
            }
        }

        public void DrawDottedLine(Vector3 a, Vector3 b, Color color, float interval = 3.5f)
        {
            if (!enable)
            {
                return;
            }

            using (new Handles.DrawingScope(color))
            {
                Handles.DrawDottedLine(a, b, interval);
            }
        }

        public void DrawPoint(Vector3 p, Color color, float radius = 0.02f, string label = null)
        {
            if (!enable)
            {
                return;
            }
            Gizmos.color = color;
            Gizmos.DrawWireSphere(p, radius);
            if (label != null)
            {
                Handles.Label(p, label);
            }
        }

        public void DrawSimplex2D(Simplex2D simplex, Color color)
        {
            if (!enable)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawWireSphere(simplex.newPoint, 0.02f);
            Gizmos.DrawWireSphere(simplex.b, 0.02f);
            Gizmos.DrawWireSphere(simplex.c, 0.02f);

            DrawArrow(simplex.newPoint, simplex.normal, color);

            Gizmos.DrawLine(simplex.newPoint, simplex.b);
            Gizmos.DrawLine(simplex.c, simplex.b);
            Gizmos.DrawLine(simplex.newPoint, simplex.c);
        }
}