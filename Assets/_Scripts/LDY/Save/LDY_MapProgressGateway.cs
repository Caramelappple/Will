using _Scripts.LSO.Stage;
using UnityEngine;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 런 진행도를 세이브와 주고받는다.
    ///
    /// 예전에는 별자리 맵의 노드 상태(클리어·해금 목록, 노드 번호)를 통째로 담았다.
    /// 고르는 화면이 없어지고 진행이 한 줄이 되면서 담을 것이 챕터·스테이지 둘로 줄었다.
    ///
    /// 노드 번호 필드는 LDY_RunSaveData에 남아 있지만 채우지 않는다.
    /// 지우면 옛 세이브 파일을 읽을 때 형식이 어긋나므로, 값만 비워둔다.
    /// </summary>
    public sealed class LDY_MapProgressGateway
    {
        public void Capture(LDY_RunSaveData data)
        {
            data.clearedNodeIndices.Clear();
            data.unlockedNodeIndices.Clear();
            data.currentNodeIndex = -1;

            if (!LSO_StageProgression.HasInstance)
            {
                Debug.LogWarning("[LDY_MapProgressGateway] LSO_StageProgression이 없어 진행도를 읽지 못했습니다.");
                return;
            }

            LSO_StageProgression progression = LSO_StageProgression.Instance;

            // 번호가 아니라 자리를 저장한다. 화면의 "1-1"은 (0, 0)이다.
            data.chapter = progression.ChapterIndex;
            data.stage = progression.StageIndex;
        }

        public void Restore(LDY_RunSaveData data)
        {
            if (!LSO_StageProgression.HasInstance)
            {
                Debug.LogWarning("[LDY_MapProgressGateway] LSO_StageProgression이 없어 진행도를 되돌리지 못했습니다.");
                return;
            }

            LSO_StageProgression.Instance.SetPosition(data.chapter, data.stage);
        }
    }
}
