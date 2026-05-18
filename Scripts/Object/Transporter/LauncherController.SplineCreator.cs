// //////////////////////////////////////////////////////////////////////////////
// * 요약
//    - Launcher 오브젝트의 궤적(경로)를 생성하는 클래스
//    - 에디터 상에서 궤적을 수정
//
// * 목차
//    1. 클래스 ... Line 29
//        1) 정의 ..... Line 34
//        2) 필드 ..... Line 46
//        3) 메서드 ... Line 54
//            1- 이벤트 함수 ... Line 57
//            2- 겟(Get) ...... Line 67
//            3- 표시(Draw) ... Line 109
//    2. 에디터 ... Line 187
//        1) 필드 ..... Line 195
//        2) 메서드 ... Line 200
//            1- 이벤트 함수 ... Line 203
//            2- 노드 이동 ..... Line 217
// //////////////////////////////////////////////////////////////////////////////
using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// //////////////////////////////////////////////////////////////////////////////
// 1. 클래스
// //////////////////////////////////////////////////////////////////////////////
public class SplineCreator : MonoBehaviour
{
    // ==============================================================================
    // 1) 정의
    // ==============================================================================
    [Serializable]public class SplineNode
    {
                          public Vector3 startPosition;
                          public Vector3 startControlPosition;
                          public Vector3 endControlPosition;
                          public Vector3 endPosition;
        [HideInInspector] public float   curveLength;
    }

    // ==============================================================================
    // 2) 필드
    // ==============================================================================
    [Header("Setting")]
    [SerializeField, Range(1, 100)] public int           curveSmoothness = 50;
    [SerializeField]                private SplineNode[] nodes;
    [HideInInspector]               public  float        totalCurveLength;

    // ==============================================================================
    // 3) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 3-1) 메서드 -> 이벤트 함수
    //    - 에디터상에 경로 표시
    // ------------------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        DrawCurves();
        DrawControllers();
    }

    // ------------------------------------------------------------------------------
    // 3-2) 메서드 -> 겟(Get)
    //    - 비율에 따른 경로의 위치(좌표)를 반환
    // ------------------------------------------------------------------------------
    public SplineNode[] GetNodes() { return nodes; }

    public Vector3 GetPoint(float rate)
    {     
        foreach (var node in nodes)
        {
            float lengthRate = node.curveLength / totalCurveLength;    // 각 노드(길이)의 비율

            if (rate < lengthRate)    // 현재 비율(이전 노드들의 비율을 뺀 값)이 노드의 비율보다 작으면
            {
                float subRate = rate / lengthRate;    // 현재 노드 상의 비율

                return _GetPoint(node, subRate);    // 노드 상의 비율에 따른 위치(좌표)를 반환
            }

            rate -= lengthRate;    // 현재 비율이 노드의 비율을 넘어서면 현재 비율에 노드의 비율을 감소
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

    // ------------------------------------------------------------------------------
    // 3-3) 메서드 -> 표시(Draw)
    //    - 에디터 상에 노드들을 연결하여 기즈모(Gizmos)를 표시
    // ------------------------------------------------------------------------------
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
}

// //////////////////////////////////////////////////////////////////////////////
// 2. 에디터
// //////////////////////////////////////////////////////////////////////////////
#if UNITY_EDITOR

[CustomEditor(typeof(SplineCreator))]
public class SplineEditor : Editor
{
    // ==============================================================================
    // 1) 필드
    // ==============================================================================
    private SplineCreator spline;

    // ==============================================================================
    // 2) 메서드
    // ==============================================================================
    // ------------------------------------------------------------------------------
    // 2-1) 메서드 -> 이벤트 함수
    //    - 에디터 상의 노드 이동
    // ------------------------------------------------------------------------------
    private void OnEnable()
    {
        spline = (SplineCreator)target;
    }

    private void OnSceneGUI()
    {
        MovePoints();
    }

    // ------------------------------------------------------------------------------
    // 2-2) 메서드 -> 노드 이동
    //    - 각 노드 내의 포인트들에 대한 이동
    //    - Start Point, Start Control Point, End Point, End Control Point
    //    - 각 노드의 End Point(마지막 노드 제외)와 그 다음 노드의 Start Point(첫 노드 제외)는 같은 위치를 공유(Control Point도 공유)
    //    - Control Point를 이용해 각 노드의 방향 및 곡률을 조정
    // ------------------------------------------------------------------------------
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
}

#endif
