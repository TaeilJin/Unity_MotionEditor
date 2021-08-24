using System;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using System.Collections.Generic;

public class Agent : MonoBehaviour
{
    public GameObject goal;
    public float velocity;
    public Actor actor;
    private RequestSocket _requestSocket;

    private List<string> input_frame_list;

    // Start is called before the first frame update
    void Start()
    {
        _requestSocket = new RequestSocket();
        _requestSocket.Connect("tcp://localhost:12345");
        Application.targetFrameRate = 10;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        input_frame_list = new List<string>();

        Vector3 position = actor.FindBone("RightHand").Transform.position;

        input_frame_list.Add(position.ToString("F6"));
        //sending
        string send_input = string.Join(", ", input_frame_list);

        _requestSocket.SendFrame(send_input);

        //receiving
        string response = _requestSocket.ReceiveFrameString();
        var splittedStrings = response.Split(' ');

        // vector index ∫∞ data ¿Ã∏ß
        Debug.Log("x " + splittedStrings[0] + "y "+ splittedStrings[1] + "z " + splittedStrings[2]);


        //for (int i = 0; i < 3; i++)
        //{
        //    input_frame_list.Add(transform.position[i].ToString("F6"));
        //}

        //for (int i = 0; i < 3; i++)
        //{
        //    input_frame_list.Add(goal.transform.position[i].ToString("F6"));
        //}

        //string send_input = string.Join(", ", input_frame_list);

        //_requestSocket.SendFrame(send_input);

        //string response = _requestSocket.ReceiveFrameString();
        //var splittedStrings = response.Split(' ');

        //Vector3 action = velocity * new Vector3(float.Parse(splittedStrings[0]), float.Parse(splittedStrings[1]), float.Parse(splittedStrings[2]));
        //transform.position += action;

        //Vector3 dist = transform.position - goal.transform.position;
        
        //if (dist.magnitude > 4f)
        //    transform.position = Vector3.zero;
    }

    private void OnDisable()
    {
        _requestSocket.Dispose();
        _requestSocket.Close();
        NetMQConfig.Cleanup(false);
    }
}