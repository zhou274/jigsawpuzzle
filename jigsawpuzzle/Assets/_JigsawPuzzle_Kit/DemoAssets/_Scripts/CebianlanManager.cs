using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarkSDKSpace;
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
using static StarkSDKSpace.StarkGridGamePanelManager;

public class CebianlanManager : MonoBehaviour
{
    public GameObject CebainlanUI;
    public string clickid;
    private StarkGridGamePanelManager mStarkGridGamePanelManager;


    private void Start()
    {


        

        showGridGame();

    }

    public void showGridGame()
    {
        mStarkGridGamePanelManager = StarkSDK.API.GetStarkGridGamePanelManager();
        if (mStarkGridGamePanelManager != null)
        {
            JsonData query = new JsonData();
            query["tta5e7d35a3ad42e8802"] = "";
            JsonData position = new JsonData();
            position["top"] = 150;
            position["left"] = 30;
            StarkGridGamePanel mStarkGridGamePanel = mStarkGridGamePanelManager.CreateGridGamePanel(GridGamePanelCount.One, query, GridGamePanelSize.Large, position);
            mStarkGridGamePanel.Show();
        }
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
