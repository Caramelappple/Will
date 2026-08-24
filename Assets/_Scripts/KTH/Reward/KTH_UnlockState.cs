using System.Collections.Generic;
using _Scripts.LSO;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Will;

public class KTH_UnlockState
{
    private readonly HashSet<LSO_AnimalSO> _unlockedPieces = new();
    private readonly HashSet<DLJ_WillDataSO> _unlockedWills = new();

    public IReadOnlyCollection<LSO_AnimalSO> Pieces => _unlockedPieces;
    public IReadOnlyCollection<DLJ_WillDataSO> Wills => _unlockedWills;

    public bool IsPieceUnlocked(LSO_AnimalSO piece) => piece != null && _unlockedPieces.Contains(piece);
    public bool IsWillUnlocked(DLJ_WillDataSO will) => will != null && _unlockedWills.Contains(will);

    public bool UnlockPiece(LSO_AnimalSO piece)
    {
        if (piece == null) return false;
        return _unlockedPieces.Add(piece);
    }

    public bool UnlockWill(DLJ_WillDataSO will)
    {
        if (will == null) return false;
        return _unlockedWills.Add(will);
    }

    public void Clear()
    {
        _unlockedPieces.Clear();
        _unlockedWills.Clear();
    }
}