using UnityEngine;
using DeepLearning;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class RealTimeAnimation : MonoBehaviour {

	public enum FPS {Thirty, Sixty}

	public Actor _actor;
    public HelloRequester _helloRequester;
	public KinectManager _kinectManager;
	
	public float AnimationTime {get; private set;}
	public float PostprocessingTime {get; private set;}
	public FPS Framerate = FPS.Sixty;

	protected abstract void Setup();
	protected abstract void Feed();
	protected abstract void Read();
	protected abstract void Postprocess();
    //protected abstract void OnGUIDerived();
    //protected abstract void OnRenderObjectDerived();

    private void Awake()
    {
		_kinectManager.awakeKinectSetting();
    }
    void Start() {
		Setup();
    }

    void FixedUpdate() {
		Utility.SetFPS(Mathf.RoundToInt(GetFramerate()));

		System.DateTime t1 = Utility.GetTimestamp();

		// update Kinect Pose
		//_kinectManager.updateKinect();
		// send target information 
		Feed();
		// get modified pose
		Read();
		// post processing
		AnimationTime = (float)Utility.GetElapsedTime(t1);
		System.DateTime t2 = Utility.GetTimestamp();
		Postprocess();
		PostprocessingTime = (float)Utility.GetElapsedTime(t2);

    }

    void OnGUI()
    {
        if (_kinectManager != null && _kinectManager.IsInitialized())
        {
			_kinectManager.onGUIKinectSetting();

		}
    }

    //void OnRenderObject() {
    //	if(NeuralNetwork != null && NeuralNetwork.Setup) {
    //		if(Application.isPlaying) {
    //			OnRenderObjectDerived();
    //		}
    //	}
    //}

    private void OnApplicationQuit()
    {
		OnDestroy();
		_kinectManager.destoryKinectSetting();
    }
    private void OnDestroy()
    {
        _helloRequester.Stop();
    }

    public float GetFramerate() {
		switch(Framerate) {
			case FPS.Thirty:
			return 30f;
			case FPS.Sixty:
			return 60f;
		}
		return 1f;
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(RealTimeAnimation), true)]
	public class RealTimeAnimation_Editor : Editor {

		public RealTimeAnimation Target;

		void Awake() {
			Target = (RealTimeAnimation)target;
		}

		public override void OnInspectorGUI() {
			Undo.RecordObject(Target, Target.name);

			DrawDefaultInspector();

			EditorGUILayout.HelpBox("Animation: " + 1000f*Target.AnimationTime + "ms", MessageType.None);
			EditorGUILayout.HelpBox("Postprocessing: " + 1000f*Target.PostprocessingTime + "ms", MessageType.None);

			if(GUI.changed) {
				EditorUtility.SetDirty(Target);
			}
		}

	}
	#endif

}
