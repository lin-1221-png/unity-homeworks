using UnityEngine;

public class uimanager : MonoBehaviour
{
    [Header("配置界面")]
    public GameObject beibao;
    public GameObject hecheng;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 题目要求：进入播放模式直接显示背包（基础题验收界面）
        beibao.SetActive(true);
        hecheng.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.B) && beibao.activeSelf!=true)
        {
            openbeibao();
        }
        else if (Input.GetKeyUp(KeyCode.B)&&beibao.activeSelf==true) 
        {
            closebeibao();
        }
        if (Input.GetKeyUp(KeyCode.C) && hecheng.activeSelf != true)
        {
            openhecheng();
        }
        else if (Input.GetKeyUp(KeyCode.C) && hecheng.activeSelf==true)
        {
            closehecheng();
        }
    }
    public void openbeibao()
    {
        beibao.SetActive (true);
    }
    public void openhecheng()
    {
        hecheng.SetActive(true);
    }
    public void closebeibao()
    {
        beibao.SetActive(false);
    }
    public void closehecheng()
    {
        hecheng.SetActive(false);
    }
}

