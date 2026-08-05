using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 스테이지에 배치할 적 하나. 기획자가 인스펙터에서 채우는 데이터 한 줄이며 로직은 갖지 않는다.
    /// 기물 생성은 카드(LSO_CardSO)를 통해서만 하므로 프리팹이 아니라 카드를 지정한다.
    /// </summary>
    [System.Serializable]
    public class LDY_StageEnemyEntry
    {
        [Tooltip("소환할 동물 카드. 스탯은 카드가 가리키는 동물SO가, 유언은 카드가 정한다.")]
        public LSO_CardSO card;

        [Tooltip("배치할 격자 좌표. x/z는 0~7, y는 모델 표시용 높이값.")]
        public Vector3Int pos;
    }
}
