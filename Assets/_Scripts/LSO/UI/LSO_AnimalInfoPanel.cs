using _Scripts.LDY;
using TMPro;
using UnityEngine;

public class LSO_AnimalInfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI animalName;
    [SerializeField] private TextMeshProUGUI health;
    [SerializeField] private TextMeshProUGUI damage;
    [SerializeField] private TextMeshProUGUI range;
    [SerializeField] private TextMeshProUGUI ability;
    [SerializeField] private TextMeshProUGUI will;
    [SerializeField] private TextMeshProUGUI desc;

    public void SetText(LDY_Animal animal)
    {
        if (animal == null) return;
        
        animalName.color = animal.team == LDY_Team.Enemy ? Color.red : Color.white;
        animalName.text = animal.name;
        
        health.text = "Hp: " + animal.health;
        damage.text =   "Atk: "+animal.GetAtk();
        
        range.text = animal.data.range.ToString();
        ability.text = animal.data.ability.ToString();
        will.text = animal.WillType.ToString();
        
        desc.text = animal.data.description;
    }
}
