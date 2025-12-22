using System.Net.Sockets;
using UnityEngine;

public class BtnCtrl : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private string id = "t10";
    [SerializeField] private string pw = "abc";
    [SerializeField] private string nickName = "nickName123";
    [SerializeField] private string confirmId = "t1";
    [SerializeField] private string confirmName = "박승호122";
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

    public void OnSelectWorldReq()
    {
        Hunt.Login.SelectWorldReq req = new Hunt.Login.SelectWorldReq();
        req.WorldId = 11;
        Hunt.Net.NetworkManager.Shared.SendToLogin(Hunt.Common.MsgId.SelectWorldReq, req);
    }

    public void OnCreateAccountReq()
    {
        Hunt.Login.CreateAccountReq req = new Hunt.Login.CreateAccountReq();
        req.Id = id;
        req.Pw = pw;
        Hunt.Net.NetworkManager.Shared.SendToLogin(Hunt.Common.MsgId.CreateAccountReq, req);
    }

    public void OnCreateCharReq()
    {
        Hunt.Login.CreateCharReq req = new Hunt.Login.CreateCharReq();
        req.ClassType = 1;
        req.WorldId = 11;
        req.Name = nickName;
        Hunt.Net.NetworkManager.Shared.SendToLogin(Hunt.Common.MsgId.CreateCharReq, req);
    }

    public void OnConfirmDupIdReq()
    {
        Hunt.Login.ConfirmIdReq req = new Hunt.Login.ConfirmIdReq();
        req.Id = confirmId;
        Hunt.Net.NetworkManager.Shared.SendToLogin(Hunt.Common.MsgId.ConfirmIdReq, req);
    }

    public void OnConfirmDupNameReq()
    {
        Hunt.Login.ConfirmNameReq req = new Hunt.Login.ConfirmNameReq();
        req.Name = confirmName;
        Hunt.Net.NetworkManager.Shared.SendToLogin(Hunt.Common.MsgId.ConfirmNameReq, req);
    }

    public void OnGameStartReq()
    {

    }

}
