using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarkSDKSpace;
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;

public class CebianlanManager : MonoBehaviour
{
    public GameObject CebainlanUI;
    public string clickid;


    private void Start()
    {

        if (!PlayerPrefs.HasKey("FirstStart"))
        {
            // 执行首次启动时的操作
            Debug.Log("首次启动应用程序");

            // 在这里添加你需要在首次启动时执行的代码

            // 设置键"FirstStart"，表示不是首次启动了
            PlayerPrefs.SetInt("FirstStart", 1);
            PlayerPrefs.Save(); // 确保保存设置
            CebainlanUI.SetActive(true);
        }
        else
        {
            Debug.Log("不是首次启动应用程序");
            CebainlanUI.SetActive(false);
        }





        clickid = "";


        getClickid();


        Debug.Log("<-clickid-> " + clickid);

        apiSend("active", clickid);



    }


    public void getClickid()
    {
        var launchOpt = StarkSDK.API.GetLaunchOptionsSync();
        if (launchOpt.Query != null)
        {
            foreach (KeyValuePair<string, string> kv in launchOpt.Query)
                if (kv.Value != null)
                {
                    Debug.Log(kv.Key + "<-参数-> " + kv.Value);
                    if (kv.Key.ToString() == "clickid")
                    {
                        clickid = kv.Value.ToString();
                    }
                }
                else
                {
                    Debug.Log(kv.Key + "<-参数-> " + "null ");
                }
        }
    }

    public void apiSend(string eventname, string clickid)
    {
        TTRequest.InnerOptions options = new TTRequest.InnerOptions();
        options.Header["content-type"] = "application/json";
        options.Method = "POST";

        JsonData data1 = new JsonData();

        data1["event_type"] = eventname;
        data1["context"] = new JsonData();
        data1["context"]["ad"] = new JsonData();
        data1["context"]["ad"]["callback"] = clickid;

        Debug.Log("<-data1-> " + data1.ToJson());

        options.Data = data1.ToJson();

        TT.Request("https://analytics.oceanengine.com/api/v2/conversion", options,
           response => { Debug.Log(response); },
           response => { Debug.Log(response); });
    }

    public void CancelPan()
    {
        CebainlanUI.SetActive(false);
    }

    public void OkPan()
    {
        CebainlanUI.SetActive(false);

        TT.InitSDK((code, env) =>
        {
            Debug.Log("Unity message init sdk callback");

            StarkSDK.API.GetStarkSideBarManager().NavigateToScene(StarkSideBar.SceneEnum.SideBar, () =>
            {
                Debug.Log("navigate to scene success");
            }, () =>
            {
                Debug.Log("navigate to scene complete");
            }, (errCode, errMsg) =>
            {
                Debug.Log($"navigate to scene error, errCode:{errCode}, errMsg:{errMsg}");
            });

        });

    }
}
