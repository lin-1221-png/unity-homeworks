using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftInventory : MonoBehaviour
{
    [Header("格子配置")]
    public GameObject slotPrefab; 
    public Transform gridParent;  

    [Header("首尾物品图片")]
    public Sprite firstItemSprite;  
    public Sprite lastItemSprite;   

    void Start()
    {
        for (int i = 0; i < 21; i++)
        {
            GameObject slot = Instantiate(slotPrefab, gridParent);
            Transform iconTrans = slot.transform.Find("Icon");
            Transform countTrans = slot.transform.Find("CountText");
            if (iconTrans != null) iconTrans.gameObject.SetActive(false);
            if (countTrans != null) countTrans.gameObject.SetActive(false);
            if (i == 0)
            {
                if (iconTrans != null)
                {
                    iconTrans.gameObject.SetActive(true);
                    iconTrans.GetComponent<Image>().sprite = firstItemSprite;
                }
                if (countTrans != null)
                {
                    countTrans.gameObject.SetActive(true);
                    countTrans.GetComponent<TextMeshProUGUI>().text = "1";
                }
            }
            else if (i == 20)
            {
                if (iconTrans != null)
                {
                    iconTrans.gameObject.SetActive(true);
                    iconTrans.GetComponent<Image>().sprite = lastItemSprite;
                }
                if (countTrans != null)
                {
                    countTrans.gameObject.SetActive(true);
                    countTrans.GetComponent<TextMeshProUGUI>().text = "1";
                }
            }
        }
    }
}