using System;
using System.Collections.Generic;
using _Scripts.LSO;

/// <summary>
/// 지금까지 해금한 기물과 유언. 뽑기나 테이블은 모른다.
///
/// MonoBehaviour가 아니므로 저장 담당자가 이 객체만 통째로 직렬화하면 된다.
/// (아직 저장은 연결되지 않았다 — Import/Export만 열어둔다)
/// </summary>
[Serializable]
public class KTH_UnlockState
{
    //리스트 대신 해쉬셋을 써 중복 방지
    private readonly HashSet<string> _pieces = new();
    private readonly HashSet<LSO_WillType> _wills = new();

    // 해금 알림은 KTH_Reward.Unlocked 하나로 충분하다.
    // 여기에도 이벤트를 두면 같은 사건이 두 경로로 전파되고,
    // Import에서까지 발생해 "불러오기 → 알림 → 저장" 고리가 생긴다.
    
    public bool IsPieceUnlocked(string animalName)
    {
        return !string.IsNullOrEmpty(animalName) && _pieces.Contains(animalName);
    }

    public bool IsWillUnlocked(LSO_WillType willType)
    {
        return _wills.Contains(willType);
    }

    /// <returns>새로 해금됐으면 true. 이미 갖고 있었으면 false.</returns>
    public bool UnlockPiece(string animalName)
    {
        if (string.IsNullOrEmpty(animalName)) return false;

        return _pieces.Add(animalName);
    }

    public bool UnlockWill(LSO_WillType willType)
    {
        return _wills.Add(willType);
    }
    
    //읽기 전용
    public IReadOnlyCollection<string> Pieces => _pieces;
    public IReadOnlyCollection<LSO_WillType> Wills => _wills;

    public void Clear()
    {
        _pieces.Clear();
        _wills.Clear();
    }

    // 해쉬셋 변환을 위한 메서드-------------

    /// <summary>세이브에 적을 형태로 꺼낸다.</summary>
    public void Export(out string[] pieces, out LSO_WillType[] wills)
    {
        pieces = new string[_pieces.Count];
        _pieces.CopyTo(pieces);

        wills = new LSO_WillType[_wills.Count];
        _wills.CopyTo(wills);
    }

    /// <summary>세이브를 되돌린다. 기존 내용은 버린다.</summary>
    public void Import(IEnumerable<string> pieces, IEnumerable<LSO_WillType> wills)
    {
        _pieces.Clear();
        _wills.Clear();

        if (pieces != null)
        {
            foreach (string piece in pieces)
            {
                if (!string.IsNullOrEmpty(piece)) _pieces.Add(piece);
            }
        }

        if (wills != null)
        {
            foreach (LSO_WillType will in wills) _wills.Add(will);
        }
    }
}
