using System;
using UnityEngine;


[Serializable]
public class AceItem
{
    public string id { get; private set; }
    public string name { get; private set; }
    public string description { get; private set; }
    public Sprite icon { get; private set; }


    public void Initialize(string itemID, string itemName, string itemDescription, Sprite itemIcon)
    {
        id = itemID;
        name = itemName;
        description = itemDescription;
        icon = itemIcon;
    }
}