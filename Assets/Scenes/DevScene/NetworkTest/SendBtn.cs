using UnityEngine;

public class SendBtn : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("SendBtn Start");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnBtn()
    {
        Debug.Log("On SendBtn");
    }
}
