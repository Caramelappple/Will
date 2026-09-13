using UnityEngine;
using System.Collections.Generic;

/// <summary>한 메시로 합쳐진 좌우 날개를 중심 부착부 기준으로 아래로 접고 위로 펼침.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_CorvoKingWingMotion : MonoBehaviour
{
    private sealed class WingPart
    {
        public MeshFilter source;
        public Mesh originalMesh, animatedMesh;
        public Vector3[] restVertices, restNormals, vertices, normals;
        public Vector4[] restTangents, tangents;
        public Matrix4x4 toRoot, fromRoot, normalToRoot, normalFromRoot;
    }
    private readonly List<WingPart> parts = new List<WingPart>();
    private Vector3[] unfoldedEmissionVertices;
    private Vector3 spanAxis, upAxis, hingeAxis, center;
    private float halfSpan, halfHeight, foldedAngle, riseRatio;
    private float elapsed, duration;
    private bool playing;
    public float WorldHalfSpan => transform.TransformVector(spanAxis * halfSpan).magnitude;

    // 변형 중인 날개 표면에서 방출해 접힌 날개와 펼쳐진 날개 모두 정확히 따라감.
    public bool TryGetFeatherOrigin(int seed, out Vector3 position, out Vector3 outward, int wingSide = 0)
    {
        position = transform.position;
        outward = transform.right;
        var part = parts.Count > 0 ? parts[0] : null;
        var emissionVertices = part != null ? part.restVertices : unfoldedEmissionVertices;
        if (emissionVertices == null) return false;
        int count = emissionVertices.Length;
        if (count == 0) return false;
        int index = (seed & int.MaxValue) % count;
        for (int i = 0; i < count; i++)
        {
            int candidate = (index + i) % count;
            float span = Vector3.Dot(emissionVertices[candidate] - center, spanAxis);
            if (Mathf.Abs(span) < halfSpan * 0.45f) continue;
            if (wingSide != 0 && Mathf.Sign(span) != Mathf.Sign(wingSide)) continue;
            position = part != null
                ? part.source.transform.TransformPoint(part.vertices[candidate])
                : transform.TransformPoint(emissionVertices[candidate]);
            outward = transform.TransformDirection(spanAxis * Mathf.Sign(span)).normalized;
            return true;
        }
        return false;
    }
    private AnimationCurve unfoldCurve, riseCurve;

    public bool SetFolded(float angle, float lowerDistanceRatio, AnimationCurve unfold, AnimationCurve rise)
    {
        playing = false;
        if (!PrepareMesh()) return false;
        foldedAngle = angle;
        riseRatio = lowerDistanceRatio;
        unfoldCurve = unfold;
        riseCurve = rise;
        ApplyPose(EvaluateCurve(unfoldCurve, 0f), EvaluateCurve(riseCurve, 0f));
        return true;
    }

    public void PlayUnfold(float seconds)
    {
        if (parts.Count == 0) return;
        elapsed = 0f;
        duration = Mathf.Max(0.1f, seconds);
        playing = true;
    }

    private bool PrepareMesh()
    {
        if (parts.Count > 0) return true;
        var source = GetComponent<MeshFilter>();
        if (source == null || source.sharedMesh == null) return false;
        var filters = GetComponentsInChildren<MeshFilter>(true);
        // 자식 장식도 원본 메시를 복제해서 움직임. 누락된 파트가 있으면 부분 변형하지 않음.
        foreach (var filter in filters)
        {
            if (filter.sharedMesh == null) continue;
            if (!filter.sharedMesh.isReadable)
            {
                Debug.LogWarning($"{filter.name}: 날개를 접으려면 모델 Import Settings의 Read/Write가 필요해.", this);
                return false;
            }
        }

        Bounds bounds = source.sharedMesh.bounds;
        center = bounds.center;
        // 부모 기물의 회전과 모델 축 변환을 반영해 실제 월드 위쪽으로 펼침.
        upAxis = transform.InverseTransformVector(Vector3.up).normalized;
        Vector3 extents = bounds.extents;
        Vector3 candidate = extents.x >= extents.y && extents.x >= extents.z ? Vector3.right
            : extents.y >= extents.z ? Vector3.up : Vector3.forward;
        spanAxis = Vector3.ProjectOnPlane(candidate, upAxis).normalized;
        if (spanAxis.sqrMagnitude < 0.5f)
        {
            candidate = Mathf.Abs(upAxis.x) < 0.8f ? Vector3.right : Vector3.forward;
            spanAxis = Vector3.ProjectOnPlane(candidate, upAxis).normalized;
        }
        hingeAxis = Vector3.Cross(upAxis, spanAxis).normalized;
        halfSpan = ProjectExtent(extents, spanAxis);
        halfHeight = ProjectExtent(extents, upAxis);

        foreach (var filter in filters)
        {
            if (filter.sharedMesh == null) continue;
            var part = new WingPart { source = filter, originalMesh = filter.sharedMesh };
            part.toRoot = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            part.fromRoot = part.toRoot.inverse;
            part.normalToRoot = part.fromRoot.transpose;
            part.normalFromRoot = part.toRoot.transpose;
            part.restVertices = part.originalMesh.vertices;
            part.restNormals = part.originalMesh.normals;
            part.restTangents = part.originalMesh.tangents;
            part.vertices = new Vector3[part.restVertices.Length];
            part.normals = new Vector3[part.restNormals.Length];
            part.tangents = new Vector4[part.restTangents.Length];
            // 모든 파트를 루트 좌표로 변형해 피벗이 달라도 날개와 장식의 움직임을 맞춤.
            for (int i = 0; i < part.restVertices.Length; i++)
                part.restVertices[i] = part.toRoot.MultiplyPoint3x4(part.restVertices[i]);
            part.animatedMesh = Instantiate(part.originalMesh);
            part.animatedMesh.name = "DLJ_CorvoKing_Unfold_" + filter.name;
            part.animatedMesh.hideFlags = HideFlags.DontSave;
            part.animatedMesh.MarkDynamic();
            filter.sharedMesh = part.animatedMesh;
            parts.Add(part);
        }
        // 임시 메시 정리 후에도 완전히 펼쳐진 표면에서 지연 방출할 수 있게 원본 좌표만 보관.
        unfoldedEmissionVertices = parts[0].restVertices;
        return true;
    }

    private static float ProjectExtent(Vector3 extents, Vector3 axis)
        => Mathf.Abs(axis.x) * extents.x + Mathf.Abs(axis.y) * extents.y + Mathf.Abs(axis.z) * extents.z;

    private void Update()
    {
        if (!playing) return;
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        ApplyPose(EvaluateCurve(unfoldCurve, progress), EvaluateCurve(riseCurve, progress));
        if (progress >= 1f) ShowUnfolded();
    }

    // Y를 제한하지 않아 1을 넘겨 살짝 더 펼쳤다가 되돌아오는 곡선도 사용 가능.
    private static float EvaluateCurve(AnimationCurve curve, float progress)
        => curve != null && curve.length > 0 ? curve.Evaluate(progress) : Mathf.SmoothStep(0f, 1f, progress);

    private void ApplyPose(float openness, float rise)
    {
        float folded = 1f - openness;
        foreach (var part in parts)
            ApplyPartPose(part, folded, rise);
    }

    private void ApplyPartPose(WingPart part, float folded, float rise)
    {
        var restVertices = part.restVertices;
        var vertices = part.vertices;
        var normals = part.normals;
        var tangents = part.tangents;
        for (int i = 0; i < restVertices.Length; i++)
        {
            Vector3 position = restVertices[i];
            float alongSpan = Vector3.Dot(position - center, spanAxis);
            float side = alongSpan < 0f ? -1f : 1f;
            // 가운데 부착부는 고정하고 바깥쪽 날개만 부드럽게 접음.
            float weight = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(halfSpan * 0.04f, halfSpan * 0.22f, Mathf.Abs(alongSpan)));
            Vector3 hinge = center + upAxis * (halfHeight * 0.72f)
                + spanAxis * (side * halfSpan * 0.08f);
            Quaternion rotation = Quaternion.AngleAxis(side * foldedAngle * folded * weight, hingeAxis);
            Vector3 moved = hinge + rotation * (position - hinge)
                - upAxis * (halfSpan * riseRatio * (1f - rise) * weight);
            vertices[i] = part.fromRoot.MultiplyPoint3x4(moved);
            if (i < normals.Length)
                normals[i] = part.normalFromRoot.MultiplyVector(rotation *
                    part.normalToRoot.MultiplyVector(part.restNormals[i])).normalized;
            if (i < tangents.Length)
            {
                Vector4 tangent = part.restTangents[i];
                Vector3 direction = part.fromRoot.MultiplyVector(rotation *
                    part.toRoot.MultiplyVector(new Vector3(tangent.x, tangent.y, tangent.z))).normalized;
                tangents[i] = new Vector4(direction.x, direction.y, direction.z, tangent.w);
            }
        }
        part.animatedMesh.vertices = vertices;
        if (normals.Length == vertices.Length) part.animatedMesh.normals = normals;
        else part.animatedMesh.RecalculateNormals();
        if (tangents.Length == vertices.Length) part.animatedMesh.tangents = tangents;
        part.animatedMesh.RecalculateBounds();
    }

    public void ShowUnfolded()
    {
        playing = false;
        foreach (var part in parts)
        {
            if (part.source != null && part.source.sharedMesh == part.animatedMesh)
                part.source.sharedMesh = part.originalMesh;
            if (part.animatedMesh != null) Destroy(part.animatedMesh);
        }
        parts.Clear();
    }

    private void OnDisable() => ShowUnfolded();
    private void OnDestroy() => ShowUnfolded();
}
