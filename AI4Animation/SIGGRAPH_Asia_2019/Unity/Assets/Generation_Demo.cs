using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Generation_Demo : RealTimeAnimation 
{
    public Transform chairroot;
    public Transform hip_desired;
    public Transform head_desired;
    public Transform rightarm_desired;
    public Transform leftarm_desired;
    public Transform rightFoot_desired;
    public Transform leftFoot_desired;
    
    private int index_TCP = 0;
    public Toggle toggle_connection;
    public Button button_label_sit;
    public Button button_label_stand;
    public Button button_newZ;
    private bool bool_send = false;
    private bool bool_label_sit = true;
    private bool bool_label_stand = false;
    private bool bool_send_newZ = false;
    public void OnClickButton()
    {
        Debug.Log("Button click!");
        bool_send = !bool_send;
        index_TCP = 0;
    }
    public void OnClickLabelStand()
    {
        Debug.Log("label is Standing");
        bool_label_sit = false;
        bool_label_stand = true;
    }
    public void OnClickLabelSit()
    {
        Debug.Log("label is Sitting");
        bool_label_sit = true;
        bool_label_stand = false;
    }
    public void OnClickNewZvalue()
    {
        Debug.Log("new Z value!");
        bool_send_newZ = true;
    }
    public void onToggle(Toggle tgValue)
    {
        Debug.Log("connection state is: " + tgValue.isOn);
        bool_send = tgValue.isOn;
    }
    //---event end---//
    protected override void Setup()
    {
        _helloRequester = new HelloRequester();
        _helloRequester.Start(); //Thread 실행
        // connection click input
         toggle_connection.onValueChanged.AddListener(delegate
        {
            onToggle(toggle_connection);
        });
        button_label_sit.onClick.AddListener(OnClickLabelSit);
        button_label_stand.onClick.AddListener(OnClickLabelStand);
        button_newZ.onClick.AddListener(OnClickNewZvalue);


        // sending 
        // setting desired hip_desired;
        //hip_desired.transform.position = new Vector3(0.0f,0.54705f,0.0f);
        //head_desired.transform.position = new Vector3(0.00029f, 1.07261f, -0.19339f);
        //
        //hip_desired.transform.position = chairroot.TransformPoint(hip_desired.transform.position);
        //head_desired.transform.position = chairroot.TransformPoint(head_desired.transform.position);
    }
    
    string getPoseData(int n_dim)
    {
        string st_copy = " ";
        string space = " ";
      
        // write string 
        for (int j = 0; j < _actor.Bones.Length; j++){
            if (n_dim == 3)
            {
                //get position data
                Vector3 joint_pos = _actor.Bones[j].Transform.position;
                joint_pos = chairroot.InverseTransformPoint(joint_pos);
                for (int i = 0; i < n_dim; i++)
                {
                    st_copy += space + joint_pos[i].ToString();
                }
            }
            if(n_dim == 9)
            {
                Vector3 joint_pos = _actor.Bones[j].Transform.position;
                Vector3 joint_forward = _actor.Bones[j].Transform.forward;
                Vector3 joint_up = _actor.Bones[j].Transform.up;
                for (int i=0; i < 3; i++)
                    st_copy += space + joint_pos[i].ToString();
                for (int i = 0; i < 3; i++)
                    st_copy += space + joint_forward[i].ToString();
                for (int i = 0; i < 3; i++)
                    st_copy += space + joint_up[i].ToString();

            }
        }
        //
        st_copy = st_copy.TrimStart();
        return st_copy;
    }
    string getControlInput(int RotorNot, int n_dim)
    {
        string st_copy = " ";
        string space = " ";
        // control input 

        Vector3 hip_des = chairroot.InverseTransformPoint(hip_desired.transform.position);
        Vector3 head_des = chairroot.InverseTransformPoint(head_desired.transform.position);
        Vector3 rhand_des = chairroot.InverseTransformPoint(rightarm_desired.transform.position);
        Vector3 lhand_des = chairroot.InverseTransformPoint(leftarm_desired.transform.position);
        Vector3 rfoot_des = chairroot.InverseTransformPoint(rightFoot_desired.transform.position);
        Vector3 lfoot_des = chairroot.InverseTransformPoint(leftFoot_desired.transform.position);

        Vector3 head_des_forward = chairroot.InverseTransformPoint(head_desired.transform.forward);
        Vector3 rhand_des_forward = chairroot.InverseTransformPoint(rightarm_desired.transform.forward);
        Vector3 lhand_des_forward = chairroot.InverseTransformPoint(leftarm_desired.transform.forward);

        Vector3 head_des_up = chairroot.InverseTransformPoint(head_desired.transform.up);
        Vector3 rhand_des_up = chairroot.InverseTransformPoint(rightarm_desired.transform.up);
        Vector3 lhand_des_up = chairroot.InverseTransformPoint(leftarm_desired.transform.up);

        

        //head
        for (int i = 0; i < n_dim; i++)
        {
            st_copy += space + head_des[i].ToString(); // position
        }
        if (RotorNot == 1)
        {
            for (int i = 0; i < n_dim; i++)
            {
                st_copy += space + head_des_forward[i].ToString(); // forward
            }
            for (int i = 0; i < n_dim; i++)
            {
                st_copy += space + head_des_up[i].ToString(); // up
            }
        }
        

        //lefthand
        for (int i = 0; i < n_dim; i++)
        {
            st_copy += space + lhand_des[i].ToString(); //position
        }
        if (RotorNot == 1)
        {
            for (int i = 0; i < n_dim; i++)
            {
                st_copy += space + lhand_des_forward[i].ToString(); // forward
            }
            for (int i = 0; i < n_dim; i++)
            {
                st_copy += space + lhand_des_up[i].ToString(); // up
            }
        }

        //right hand 
        for (int i = 0; i < n_dim; i++)
        {
            st_copy += space + rhand_des[i].ToString();
        }
        if (RotorNot == 1)
        {
            for (int i = 0; i < n_dim; i++)
            {
                st_copy += space + rhand_des_forward[i].ToString(); // forward
            }
            for (int i = 0; i < n_dim; i++)
            {
                st_copy += space + rhand_des_up[i].ToString(); // up
            }
        }
        st_copy = st_copy.TrimStart();

        return st_copy;
    }
    int g_cnt_autoreg = 0;
    int g_cnt_control = 0;
    private Queue<string> que_pose = new Queue<string>();
    private Queue<string> que_control = new Queue<string>();
    public void FeedStep_stackPose()
    {
        if( g_cnt_autoreg < 11)
        {
            que_pose.Enqueue(getPoseData(3)); // position
            //que_pose.Enqueue(getPoseData(9)); // rotation
            g_cnt_autoreg++;
        }
        else
        {
            // pop leftmost 
            que_pose.Dequeue();
            // push new one
            que_pose.Enqueue(getPoseData(3)); //position
            //que_pose.Enqueue(getPoseData(9)); // rotation
        }
          

        if (que_pose.Count != 11)
            Debug.Log("pose enqueue and dequeue is wrong! ");
    }
    public void FeedStep_stackControl()
    {
        if (g_cnt_control < 11)
        {
            que_control.Enqueue(getControlInput(0,3)); // position
            //que_control.Enqueue(getControlInput(1, 3)); // rotation
            g_cnt_control++;
        }
        else
        {
            // pop leftmost 
            que_control.Dequeue();
            // push new one
            que_control.Enqueue(getControlInput(0, 3)); // position
            //que_control.Enqueue(getControlInput(1, 3)); // rotation
        }


        if (que_control.Count != 11)
            Debug.Log("control enqueue and dequeue is wrong! ");
    }
    public string send_stackpose(string start, Queue<string> quepose)
    {
        string st_send = start;
        string space = " ";
        string[] que_send = quepose.ToArray();
        //for que pose
        for(int i =0; i < quepose.Count; i++)
        {
            st_send += space + que_send[i];
        }
        // 
        return st_send;
    }
    public string send_sitlabel(string start)
    {
        string st_send = start;
        string space = " ";
        st_send += space + "0";
        return st_send;
    }
    public string send_standlabel(string start)
    {
        string st_send = start;
        string space = " ";
        st_send += space + "1";
        return st_send;
    }
    public string send_int(string start,int a)
    {
        string st_send = start;
        string space = " ";
        st_send += space + a.ToString();
        return st_send;
    }
    protected override void Feed()
    {
        string st_pos = "none";
        FeedStep_stackPose();
        FeedStep_stackControl();

        if (que_control.Count == 11 && bool_send)
        {
            st_pos = index_TCP.ToString();
            //send autoregression
            st_pos = send_stackpose(st_pos, que_pose);
            //send control
            st_pos = send_stackpose(st_pos, que_control);

            //send label
            if (bool_label_sit == true)
                st_pos = send_sitlabel(st_pos);
            else if (bool_label_stand == true)
                st_pos = send_standlabel(st_pos);

            //send newZ
            if (bool_send_newZ == true)
                st_pos = send_int(st_pos, 1);
            else
                st_pos = send_int(st_pos, 0);

            bool_send_newZ = false;
            //st_pos = send_stackpose(st_pos, que_pose);
            //st_pos = send_stackpose(st_pos, que_control);

                //Debug.Log("send " + st_pos);
                //send position and bool trigger
                //bool_send = false;

            _helloRequester.bool_RecvComplete = true;
            _helloRequester.bool_SendComplete = true;
            _helloRequester.str_message_send = st_pos;

            //index_TCP++;
        }

        //bool_send = false;
        bool dim_rot = false;
        if (bool_send == true && _helloRequester.bool_SendComplete == true && _helloRequester.bool_RecvComplete == true)
        {
            string message = _helloRequester.DataPostProcessing();
            var splittedStrings = message.Split(' ');
            Debug.Log(" get " + splittedStrings[0] + " " + splittedStrings.Length);

            if (dim_rot == false) // position (soon be erased)
            {
                if (splittedStrings[0] == "Hello" && splittedStrings.Length == (22 * 3) + 1)
                {
                    for (int p = 0; p < 22; p++)
                    {
                        var x = float.Parse(splittedStrings[3 * (p) + 1]);
                        var y = float.Parse(splittedStrings[3 * (p) + 2]);
                        var z = float.Parse(splittedStrings[3 * (p) + 3]);
                        Vector3 position = new Vector3(x, y, z);
                        position = chairroot.GetWorldMatrix().MultiplyPoint(position);
                        _actor.Bones[p].Transform.position = position;

                    }

                }
            }
            if (dim_rot == true) // rotation (soon be erased)
            {
                if (splittedStrings[0] == "Hello" && splittedStrings.Length == (22 * 9) + 1)
                {
                    for (int p = 0; p < 22; p++)
                    {
                        var x = float.Parse(splittedStrings[9 * (p) + 1]);
                        var y = float.Parse(splittedStrings[9 * (p) + 2]);
                        var z = float.Parse(splittedStrings[9 * (p) + 3]);
                        Vector3 position = new Vector3(x, y, z);
                        position = chairroot.GetWorldMatrix().MultiplyPoint(position);
                        _actor.Bones[p].Transform.position = position;

                        var x_f = float.Parse(splittedStrings[9 * (p) + 4]);
                        var y_f = float.Parse(splittedStrings[9 * (p) + 5]);
                        var z_f = float.Parse(splittedStrings[9 * (p) + 6]);
                        Vector3 forward = new Vector3(x_f, y_f, z_f);
                        forward = chairroot.GetWorldMatrix().MultiplyPoint(forward);
                        _actor.Bones[p].Transform.forward = forward;

                        var x_u = float.Parse(splittedStrings[9 * (p) + 7]);
                        var y_u = float.Parse(splittedStrings[9 * (p) + 8]);
                        var z_u = float.Parse(splittedStrings[9 * (p) + 9]);
                        Vector3 up = new Vector3(x_u, y_u, z_u);
                        up = chairroot.GetWorldMatrix().MultiplyPoint(up);
                        _actor.Bones[p].Transform.up = up;
                    }

                }
            }
        }
        else
        {
            _helloRequester.str_message_send = null;
        }
    }

    protected override void Read()
    {
        

        //if (splittedStrings[0] == "Hello" && splittedStrings.Length == (22*3) + 1)
        //{
        //    for (int p = 0; p < 22; p++)
        //    {
        //        var x = float.Parse(splittedStrings[9 * (p) + 1]);
        //        var y = float.Parse(splittedStrings[9 * (p) + 2]);
        //        var z = float.Parse(splittedStrings[9 * (p) + 3]);
        //        Vector3 position = new Vector3(x, y, z);
        //        position = chairroot.GetWorldMatrix().MultiplyPoint(position);
        //        _actor.Bones[p].Transform.position = position;

        //        var up_x = float.Parse(splittedStrings[9 * (p) + 7]);
        //        var up_y = float.Parse(splittedStrings[9 * (p) + 8]);
        //        var up_z = float.Parse(splittedStrings[9 * (p) + 9]);
        //        Vector3 up_vector = new Vector3(up_x, up_y, up_z);
        //        up_vector = chairroot.GetWorldMatrix().MultiplyVector(up_vector);
        //        //_actor.Bones[p].Transform.up = up_vector;

        //        var forward_x = float.Parse(splittedStrings[9 * (p) + 4]);
        //        var forward_y = float.Parse(splittedStrings[9 * (p) + 5]);
        //        var forward_z = float.Parse(splittedStrings[9 * (p) + 6]);
        //        Vector3 forward_vector = new Vector3(forward_x, forward_y, forward_z);
        //        forward_vector = chairroot.GetWorldMatrix().MultiplyVector(forward_vector);
        //        //_actor.Bones[p].Transform.forward = forward_vector;

        //        _actor.Bones[p].Transform.rotation = Quaternion.LookRotation(forward_vector, up_vector);
        //        //_actor.Bones[p].ApplyLength();


        //    }

        //}

    }

    protected override void Postprocess()
    {
        
    }
}
