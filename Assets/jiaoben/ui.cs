using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ui : MonoBehaviour
{
    [Header("格子配置")]
    public GameObject slotPrefab; 
    public Transform gridParent;  

    [Header("物品数据 (按顺序拖入)")]
    public List<Sprite> itemIcons = new List<Sprite>();
    public List<int> itemCounts = new List<int>();

    void Start()
    {
    
        for (int i = 0; i < 35; i++)
        {
            GameObject slot = Instantiate(slotPrefab, gridParent);
            if (i < itemIcons.Count && itemIcons[i] != null)
            {
                Transform iconTrans = slot.transform.Find("Icon");
                if (iconTrans != null)
                {
                    iconTrans.gameObject.SetActive(true); 
                    iconTrans.GetComponent<Image>().sprite = itemIcons[i]; 
                }

                Transform countTrans = slot.transform.Find("CountText");
                if (countTrans != null && itemCounts[i] > 1) 
                {
                    countTrans.gameObject.SetActive(true);
                    countTrans.GetComponent<TextMeshProUGUI>().text = itemCounts[i].ToString();
                }
            }
        }
    }
}
