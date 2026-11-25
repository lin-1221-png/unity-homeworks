using UnityEngine;
using System;

[System.Serializable]
public class ItemSlot
{
    public ItemDate item;
    public int count;
    public bool IsEmpty => item == null;
    public ItemSlot(ItemDate item, int count)
    {
        this.item = item;
        this.count = count;
    }
    public ItemSlot() 
    {
        item = null;
        count = 0;
    }
}

