using System.Collections.Concurrent;
using System.Threading;
using NetMQ;
using UnityEngine;
using NetMQ.Sockets;

namespace DeepLearning
{
    public class NetMqListener
    {
        private readonly Thread _listenerWorker;

        private bool _listenerCancelled;

        public delegate void MessageDelegate(string message);

        private readonly MessageDelegate _messageDelegate;

        private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

        public bool bool_req;
        
        private void ListenerWork()
        {
            AsyncIO.ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
            using (RequestSocket client = new RequestSocket())
            {
                client.Connect("tcp://localhost:5555");

                for (int i = 0; i < 10 && !_listenerCancelled; i++)
                {
                    Debug.Log("Sending Hello");
                    client.SendFrame("Hello");
                    // ReceiveFrameString() blocks the thread until you receive the string, but TryReceiveFrameString()
                    // do not block the thread, you can try commenting one and see what the other does, try to reason why
                    // unity freezes when you use ReceiveFrameString() and play and stop the scene without running the server
                    //                string message = client.ReceiveFrameString();
                    //                Debug.Log("Received: " + message);
                    string message = null;
                    bool gotMessage = false;
                    while (_listenerCancelled)
                    {
                        gotMessage = client.TryReceiveFrameString(out message); // this returns true if it's successful
                        _messageQueue.Enqueue(message);
                        if (gotMessage) break;
                    }

                    if (gotMessage) Debug.Log("Received " + message);
                }
                client.Close();
            }

            NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
        }

        public string DataPostProcessing()
        {
            //3. messageQue 에 값을 넣는다.
            while (!_messageQueue.IsEmpty)
            {
                string message;
                bool test = _messageQueue.TryDequeue(out message);
                if (test && message != null)
                {
                    //_messageDelegate(message);
                    return message;

                }
                else
                {
                    break;

                }
            }
            return "F";
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

    public class SocketNetZMQ : NeuralNetwork
    {
        private NetMqListener _netMqListener;
        
        private void HandleMessage(string message)
        {
            //4. 받은 값을 if 문으로 처리해서, send 했을때의 값을 출력했는지 검사한다.
            var splittedStrings = message.Split(' ');
            if (splittedStrings[0] != "Hello" && splittedStrings.Length != 3) return; // send 했을때의 index 와 같은 경우에만 한다.
            var x = float.Parse(splittedStrings[0]);
            var y = float.Parse(splittedStrings[1]);
            var z = float.Parse(splittedStrings[2]);
            transform.position = new Vector3(x, y, z);

            
        }

        protected override bool SetupDerived()
        {
            if (Setup)
            {
                return true;
            }
            try
            {
                _netMqListener = new NetMqListener(HandleMessage);
                _netMqListener.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override bool ShutdownDerived()
        {
            if (Setup)
            {
                _netMqListener.Stop();
                DeleteMatrices();
                ResetPredictionTime();
                ResetPivot();
            }
            return false;
        }

        protected override void PredictDerived()
        {
            try
            {
                messageQe = _netMqListener.DataPostProcessing();
            }
            catch
            {
                //Debug.Log("Neural network socket was setup but prediction failed.");
                Setup = ShutdownDerived();
            }
        }
    }

    //public static class SocketExtensionsTJ
    //{
    //    public static void ReceiveAll(this Socket socket, byte[] buffer)
    //    {
    //        int dataRead = 0;
    //        int dataleft = buffer.Length;
    //        while (dataRead < buffer.Length)
    //        {
    //            int recv = socket.Receive(buffer, dataRead, dataleft, SocketFlags.None);
    //            if (recv == 0)
    //            {
    //                break;
    //            }
    //            else
    //            {
    //                dataRead += recv;
    //                dataleft -= recv;
    //            }
    //        }
    //    }
    //}

}