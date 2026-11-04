using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Catalog")]
public class Catalog : ScriptableObject
{
    public enum Kind { Wing, Skill }

    [System.Serializable]
    public class Item
    {
        public string id;
        public Kind kind;
        public int price;
        public Sprite sprite;
    }

    public Item[] items;

    public void Apply(SpriteRenderer r)
    {
        var it = System.Array.Find(items, i => i.id == RunData.Wing);
        r.sprite = it?.sprite;
        r.enabled = it != null;
    }
}
