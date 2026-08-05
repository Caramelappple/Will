using _Scripts.LSO.Deck.Data;
using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 카드로 기물을 만들어 보드에 올리는 역할의 추상화.
    /// 배치를 요청하는 쪽(스테이지 스텝 등)이 생성 방식(팩토리/풀링 등)에 직접 묶이지 않도록 한다.
    /// </summary>
    public interface LDY_IUnitSpawner
    {
        /// <summary>실패하면 null을 돌려준다(칸이 차 있거나 카드가 유효하지 않은 경우 등).</summary>
        LDY_Animal Spawn(LSO_CardSO card, LDY_Team team, Vector3Int pos);
    }
}
