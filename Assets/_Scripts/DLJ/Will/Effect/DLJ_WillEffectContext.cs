using System;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>유언 시스템에서 연출로 넘기는 공통 데이터.</summary>
public sealed class DLJ_WillEffectContext
{
    public DLJ_WillDataSO data;
    public GameObject owner;
    public GameObject target;
    public Vector3 origin;
    public Vector3 targetPosition;
    public Vector3 areaSize;
    public Action onStarted;
}
