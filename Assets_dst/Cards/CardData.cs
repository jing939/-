using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "Cards/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite cardImage;
    public int value; // 1 to 13
    public Suit suit;
    public int extraDamage;
    public float successChance = 1.0f; // 1.0 = 100%

    public enum Suit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }
}
