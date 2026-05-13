using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class SplineCreator : MonoBehaviour
{
    #region Definition

    [Serializable]public class SplineNode
    {
        #region Field

                          public Vector3 startPosition;
                          public Vector3 startControlPosition;
                          public Vector3 endControlPosition;
                          public Vector3 endPosition;
        [HideInInspector] public float   curveLength;

        #endregion
    }

    #endregion


    #region Field

    // Values
    [Header("Setting")]
    [SerializeField, Range(1, 100)] public int           curveSmoothness = 50;
    [SerializeField]                private SplineNode[] nodes;
    [HideInInspector]               public  float        totalCurveLength;

    #endregion


    #region Method

    #region Event

    private void OnDrawGizmosSelected()
    {
        DrawCurves();
        DrawControllers();
    }

    #endregion


    public SplineNode[] GetNodes() { return nodes; }

    public Vector3 GetPoint(float rate)
    {
        foreach (var node in nodes)
        {
            float lengthRate = node.curveLength / totalCurveLength;

            if (rate < lengthRate)
            {
                float subRate = rate / lengthRate;

                return _GetPoint(node, subRate);
            }

            rate -= lengthRate;
        }

        return _GetPoint(nodes.Last(), 1f);
    }

    private Vector3 _GetPoint(SplineNode node, float nodeRate)
    {
        Vector3 startPosition        = node.startPosition;
        Vector3 startControlPosition = node.startControlPosition;
        Vector3 endControlPosition   = node.endControlPosition;
        Vector3 endPosition          = node.endPosition;

        Vector3 movePoint1   = Vector3.Lerp(startPosition,        startControlPosition, nodeRate);
        Vector3 movePoint2   = Vector3.Lerp(startControlPosition, endControlPosition,   nodeRate);
        Vector3 movePoint3   = Vector3.Lerp(endControlPosition,   endPosition,          nodeRate);
        Vector3 movePoint1_2 = Vector3.Lerp(movePoint1,           movePoint2,           nodeRate);
        Vector3 movePoint2_3 = Vector3.Lerp(movePoint2,           movePoint3,           nodeRate);
        Vector3 movePoint    = Vector3.Lerp(movePoint1_2,         movePoint2_3,         nodeRate);

        return movePoint;
    }

    #endregion


    #region Gizmos

    private void DrawCurves()
    {
        if (nodes is null) return;

        float totalLength = 0f;

        for (int i = 0; i < nodes.Length; i++)
        {
            DrawCurve(nodes[i]);

            totalLength += nodes[i].curveLength;
        }

        totalCurveLength = totalLength;
    }

    private void DrawCurve(SplineNode node)
    {
        Gizmos.color = Color.white;

        float length = 0f;

        for (int i = 0; i < curveSmoothness; i++)
        {
            float   prevRatio    = i       / (float)curveSmoothness;
            float   nextRatio    = (i + 1) / (float)curveSmoothness;
            Vector3 prevPosition = _GetPoint(node, prevRatio);
            Vector3 nextPosition = _GetPoint(node, nextRatio);

            Gizmos.DrawLine(prevPosition, nextPosition);

            length += Vector3.Distance(prevPosition, nextPosition);
        }

        node.curveLength = length;
    }

    private void DrawControllers()
    {
        if (nodes is null) return;

        for (int i = 0; i < nodes.Length; i++)
        {
            DrawPoints(nodes[i], i);
            DrawLines(nodes[i]);
        }
    }

    private void DrawPoints(SplineNode node, int count)
    {
        float radius = 1f;

        Gizmos.color = Color.red;

        if (count == 0) Gizmos.DrawSphere(node.startPosition, radius);
                        Gizmos.DrawSphere(node.endPosition, radius);

        Gizmos.color = Color.blue;

        Gizmos.DrawSphere(node.startControlPosition, radius);

        Gizmos.color = Color.yellow;

        Gizmos.DrawSphere(node.endControlPosition, radius);
    }

    private void DrawLines(SplineNode node)
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(node.startPosition, node.startControlPosition);
        Gizmos.DrawLine(node.endPosition,   node.endControlPosition);
    }

    #endregion
}


#if UNITY_EDITOR

[CustomEditor(typeof(SplineCreator))]
public class SplineEditor : Editor
{
    #region Field

    private SplineCreator spline;

    #endregion


    #region Event Method

    private void OnEnable()
    {
        spline = (SplineCreator)target;
    }

    private void OnSceneGUI()
    {
        MovePoints();
    }

    #endregion


    #region GUI Method

    private void MovePoints()
    {
        SplineCreator.SplineNode[] nodes  = spline.GetNodes();

        if (nodes is null) return;

        int count = nodes.Length;

        // Start/Control point
        for (int i = 0; i < count; i++)
        {
            nodes[i].startPosition        = Handles.PositionHandle(nodes[i].startPosition,        Quaternion.identity);
            nodes[i].startControlPosition = Handles.PositionHandle(nodes[i].startControlPosition, Quaternion.identity);
        }

        // End control point
        for (int i = 0; i < count; i++)
        {
            if (i != count - 1) nodes[i].endControlPosition         = GetMirrorPoint(nodes[i + 1].startControlPosition, nodes[i + 1].startPosition);
            else                nodes[count - 1].endControlPosition = Handles.PositionHandle(nodes[count - 1].endControlPosition, Quaternion.identity);
        }

        // End point
        for (int i = 0; i < count; i++)
        {
            if (i != count - 1) nodes[i].endPosition         = nodes[i + 1].startPosition;
            else                nodes[count - 1].endPosition = Handles.PositionHandle(nodes[count - 1].endPosition, Quaternion.identity);
        }
    }

    private Vector3 GetMirrorPoint(Vector3 origin, Vector3 center)
    {
        Vector3 mirror = center - (origin - center);

        return mirror;
    }

    #endregion
}

#endif
