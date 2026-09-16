using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DLJ_PiggyBankEffect))]
public sealed class DLJ_PiggyBankEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox(
            "Deposit Ease는 저금 구멍으로 들어가는 이동, Collection Ease는 코스트 케이스로 회수하는 이동 그래프야. " +
            "그래프를 클릭해서 프리셋을 고르거나 직접 편집해. X는 시간, Y는 진행도(0~1)이고 시작 (0,0), 끝 (1,1)을 권장해. " +
            "사망 직후 흩어지고 떨어지는 움직임은 중력 계산을 사용해.", MessageType.Info);
        EditorGUILayout.HelpBox(
            "자기 턴마다 최대 3개까지 저금해. 사망 금화는 다음 우리 턴에 추가 코스트 케이스로 이동하고, " +
            "10을 넘는 금화는 사라져. Scene 뷰의 노란 표시가 저금 구멍 위치야. " +
            "Deposit Slot을 직접 연결하거나 Slot Offset으로 맞출 수 있어.", MessageType.Info);
        var effect = (DLJ_PiggyBankEffect)target;
        EditorGUILayout.HelpBox("Play 모드에서 사망 연출을 바로 볼 수 있어. 금화 3개로 폭발·낙하를 재생하고 본체는 자동 복구해. 실제 HP와 코스트는 유지해.", MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !effect.isActiveAndEnabled))
        {
            if (GUILayout.Button("사망 연출 바로 보기 / 다시 재생", GUILayout.Height(30f)))
                effect.PreviewDeath();
            using (new EditorGUI.DisabledScope(!effect.IsPreviewingDeath))
                if (GUILayout.Button("사망 미리보기 중지 / 본체 복구")) effect.StopDeathPreview();
            if (GUILayout.Button("금화 저금 미리보기 (실제 저장량 유지)"))
                effect.PreviewDeposit();
        }
    }
}
