using System;
using System;
using System.Collections.Generic;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

/// <summary>
/// CardSO 목록을 보관하고, 보드 기물의 AnimalSO와 같은 카드를 찾아준다.
/// 기물 사진은 여기서 별도로 복사하지 않고 CardSO.Image를 그대로 사용한다.
/// </summary>
[CreateAssetMenu(fileName = "DLJ_InfoPanelCatalog", menuName = "DLJ/UI/Info Panel Catalog")]
public sealed class DLJ_InfoPanelCatalogSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private LSO_CardSO card;

        public LSO_CardSO Card => card;
    }

    [SerializeField] private List<Entry> entries = new();

    public bool TryGetCard(LSO_AnimalSO animal, out LSO_CardSO result)
    {
        if (animal != null && entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                LSO_CardSO card = entry?.Card;

                if (card != null &&
                    card.IsValid &&
                    card.Animal == animal)
                {
                    result = card;
                    return true;
                }
            }
        }

        result = null;
        return false;
    }
}
