using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using System;
using StarkSDKSpace;
using UnityEngine.SceneManagement;

public class InitAd : MonoBehaviour
{
    public string clickid;
    void Start()
    {
        StartCoroutine(MyCoroutine());
    }
    IEnumerator MyCoroutine()
    {
        // 等待5秒
        yield return new WaitForSeconds(0.5f);
        Debug.Log("协程定时器触发，5秒已过去！");
        Debug.Log("事件上传active");
        clickid = "";
        getClickid();
        apiSend("active", clickid);
        
    }
    public void apiSend(string eventname, string clickid)
    {
        TTRequest.InnerOptions options = new TTRequest.InnerOptions();
        options.Header["content-type"] = "application/json";
        options.Method = "POST";
        options.DataType = "JSON";
        options.ResponseType = "text";

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

    public void getClickid()
    {
        var launchOpt = StarkSDK.API.GetLaunchOptionsSync();
        if (launchOpt.Query != null)
        {
            foreach (KeyValuePair<string, string> kv in launchOpt.Query)
                if (kv.Value != null)
                {
                    Debug.Log(kv.Key + "<-����-> " + kv.Value);
                    if (kv.Key.ToString() == "clickid")
                    {
                        clickid = kv.Value.ToString();
                    }
                }
                else
                {
                    Debug.Log(kv.Key + "<-����-> " + "null ");
                }
        }
    }
}
