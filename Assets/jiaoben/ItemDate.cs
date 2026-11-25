using UnityEngine;
[CreateAssetMenu(menuName ="ItemDate/Item")]

public class ItemDate:ScriptableObject 
{
    public int id;
    public string type;
    public Sprite icon;
    [Header("堆叠")]
    public bool canstack = true;
    public int max = 99;
    public int min = 99;
}
public enum Itemtype
{
    xiaohao,
    wuqi,
    cailiao
}

