using System.Collections.Concurrent;
using System.Threading;
using NetMQ;
using UnityEngine;
using NetMQ.Sockets;

public class NetMqListener
{
    private readonly Thread _listenerWorker;

    private bool _listenerCancelled;

    public delegate void MessageDelegate(string message);

    private readonly MessageDelegate _messageDelegate;

    private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

    public bool bool_req;
    public string messageQe;
    private void ListenerWork()
    {
        AsyncIO.ForceDotNet.Force();
       
        using (var subSocket = new RequestSocket())
        {
            //setup
            subSocket.Options.ReceiveHighWatermark = 1000;
            subSocket.Connect("tcp://localhost:12345");
            
            while (!_listenerCancelled)
            {
                //update
                string frameString;
                // if bool requset
                if (bool_req == true)
                {
                    string msg = "hi";
                    if (!subSocket.TrySendFrame(msg)) continue;
                    // try to request the trained pose 
                    if (!subSocket.TryReceiveFrameString(out frameString)) continue;
                    //subSocket.TryReceiveFrameString(out frameString);
                    Debug.Log(frameString);
                    _messageQueue.Enqueue(frameString);
                }

                

            }
            //delete
            subSocket.Close();
        }
        //delete
        NetMQConfig.Cleanup();
    }

    public void Update()
    {
        //while(!_messageQueue.IsEmpty)
        if (!_messageQueue.IsEmpty)
        {
            string message;
            if (_messageQueue.TryDequeue(out message))
            {
                _messageDelegate(message);
            }
            //else
            //{
            //    break;
            //}
        }
    }

    public NetMqListener(MessageDelegate messageDelegate)
    {
        _messageDelegate = messageDelegate;
        _listenerWorker = new Thread(ListenerWork);
    }

    public void Start()
    {
        _listenerCancelled = false;
        _listenerWorker.Start();
    }

    public void Stop()
    {
        _listenerCancelled = true;
        _listenerWorker.Join();
    }
}

public class ClientObject : MonoBehaviour
{
    private NetMqListener _netMqListener;

    private void HandleMessage(string message)
    {
        var splittedStrings = message.Split(' ');
        if (splittedStrings.Length != 3) return;
        var x = float.Parse(splittedStrings[0]);
        var y = float.Parse(splittedStrings[1]);
        var z = float.Parse(splittedStrings[2]);
        transform.position = new Vector3(x, y, z);
        
        // update pose
        
        // post processing
        
    }

    private void Start()
    {
        _netMqListener = new NetMqListener(HandleMessage);
        _netMqListener.Start();
    }

    private void Update()
    {
        // control input 을 통해서, request 전 처리
        
        // 사용자 입력 혹은, event 로 bool_req 가 활성화
        _netMqListener.bool_req = !_netMqListener.bool_req;
        _netMqListener.Update(); 
    }

    private void OnDestroy()
    {
        _netMqListener.Stop();
    }
}
