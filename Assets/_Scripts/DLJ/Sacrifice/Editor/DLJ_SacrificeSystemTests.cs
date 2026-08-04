using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.Will;
using NUnit.Framework;
using UnityEngine;

public class DLJ_SacrificeSystemTests
{
    [Test]
    public void InvokeWill_BuffsOnlyAdjacentAllies()
    {
        GameObject root = new GameObject("SacrificeTestRoot");

        try
        {
            LDY_BoardManager board =
                root.AddComponent<LDY_BoardManager>();

            LDY_Animal owner = CreateAnimal(
                root.transform, board, "Owner", new Vector3Int(3, 0, 3),
                LDY_Team.Player, 2, 2, 1);
            LDY_Animal adjacentAlly = CreateAnimal(
                root.transform, board, "AdjacentAlly", new Vector3Int(4, 0, 3),
                LDY_Team.Player, 5, 3, 2);
            LDY_Animal diagonalAlly = CreateAnimal(
                root.transform, board, "DiagonalAlly", new Vector3Int(4, 0, 4),
                LDY_Team.Player, 2, 2, 1);
            LDY_Animal adjacentEnemy = CreateAnimal(
                root.transform, board, "AdjacentEnemy", new Vector3Int(2, 0, 3),
                LDY_Team.Enemy, 4, 4, 3);
            LDY_Animal distantAlly = CreateAnimal(
                root.transform, board, "DistantAlly", new Vector3Int(0, 0, 0),
                LDY_Team.Player, 6, 6, 4);

            // 실제 사망 처리처럼 소유 기물을 보드에서 먼저 제거한다.
            board.Remove(owner);

            var context = new DLJ_WillContext
            {
                owner = owner.gameObject,
                animal = owner,
                board = board
            };

            LSO_IWill sacrifice = LSO_WillFactory.Create(
                LSO_WillType.Sacrifice,
                context,
                new DLJ_WillData { willType = LSO_WillType.Sacrifice });

            Assert.That(sacrifice, Is.Not.Null);
            sacrifice.InvokeWill();

            AssertStats(adjacentAlly, 6, 4, 3);
            AssertStats(diagonalAlly, 3, 3, 2);
            AssertStats(adjacentEnemy, 4, 4, 3);
            AssertStats(distantAlly, 6, 6, 4);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static LDY_Animal CreateAnimal(
        Transform parent,
        LDY_BoardManager board,
        string objectName,
        Vector3Int position,
        LDY_Team team,
        int maxHealth,
        int currentHealth,
        int attack)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent);

        LDY_Animal animal = gameObject.AddComponent<LDY_Animal>();
        animal.health = gameObject.GetComponent<Health>();
        animal.modelTransform = gameObject.transform;
        animal.team = team;
        animal.baseAtk = attack;
        animal.health.Init(maxHealth);
        animal.health.Value = currentHealth;

        board.Place(animal, position);
        return animal;
    }

    private static void AssertStats(
        LDY_Animal animal,
        int expectedMaxHealth,
        int expectedCurrentHealth,
        int expectedAttack)
    {
        Assert.That(animal.health.MaxValue, Is.EqualTo(expectedMaxHealth));
        Assert.That(animal.health.Value, Is.EqualTo(expectedCurrentHealth));
        Assert.That(animal.baseAtk, Is.EqualTo(expectedAttack));
    }
}
