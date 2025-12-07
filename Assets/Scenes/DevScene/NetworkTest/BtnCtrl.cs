using System.Net.Sockets;
using UnityEngine;

public class BtnCtrl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnDisconn(Hunt.Net.NetModule.ERROR e, string msg)
    {
        Debug.Log(msg);
    }

    void OnConnSucc()
    {
        Debug.Log("Conn Succ");
    }

    void OnConnFail(SocketException e)
    {
        Debug.Log(e.Message);
    }
    public void OnConnBtn()
    {
        Debug.Log("On ConnBtn");
        Hunt.Net.NetworkManager.Shared.ConnLoginServerSync(OnDisconn, OnConnSucc, OnConnFail);
        Hunt.Net.NetworkManager.Shared.StartLoginServer();
    }

    public void OnDisConnBtn()
    {
        Debug.Log("On DisConnBtn");
        Hunt.Net.NetworkManager.Shared.DisConnLoginServer();
    }
    public void OnSendBtn()
    {
        Debug.Log("On SendBtn");
        Hunt.Login.LoginTestReq req = new Hunt.Login.LoginTestReq();
        req.Data = "안녕하세요";
        req.Num = 1;
        Hunt.Net.NetworkManager.Shared.SendToLogin(Hunt.Common.MsgId.LoginTestReq, req);
    }

    public void OnSendLoginReq()
    {
        Hunt.Login.LoginReq req = new Hunt.Login.LoginReq();
        req.Id = "t1";
        req.Pw = "hle";
        Hunt.Net.NetworkManager.Shared.SendToLogin(Hunt.Common.MsgId.LoginReq, req);
    }

}
