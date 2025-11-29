using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class ConnBtn : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDisconn(hunt.Net.NetModule.ERROR e, string msg)
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
        hunt.Net.NetworkManager.Shared.ConnLoginServerSync(OnDisconn, OnConnSucc, OnConnFail);
    }
}
