using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

public class MotionLoader : MonoBehaviour
{
	public string Folder = string.Empty;
	public Actor Character = null;
	public MotionData[] Files = new MotionData[0];
	public bool Save = true;

	public float TargetFramerate = 60f;
	public int RandomSeed = 0;

	public bool Callbacks = true;
	public bool Mirror = false;
	public bool Visualise = true;
	public bool Settings = false;

	private bool CameraFocus = false;
	private float FocusHeight = 1f;
	private float FocusDistance = 1f;
	private float FocusAngle = 90f;
	private float FocusSmoothing = 0.05f;

	private bool Playing = false;
	private float Timescale = 1f;
	private float Timestamp = 0f;
	private float Zoom = 1f;

	private int[] BoneMapping = new int[0];

	[SerializeField] private MotionData Data = null;
	[SerializeField] private Actor Actor = null;

	public static MotionLoader GetInstance()
	{
		return GameObject.FindObjectOfType<MotionLoader>();
	}

	public void Refresh()
	{
		for (int i = 0; i < Files.Length; i++)
		{
			if (Files[i] == null)
			{
				Debug.Log("Removing missing file from editor.");
				ArrayExtensions.RemoveAt(ref Files, i);
				for (int j = 0; j < EditorSceneManager.sceneCount; j++)
				{
					Scene scene = EditorSceneManager.GetSceneAt(j);
					if (scene != EditorSceneManager.GetActiveScene())
					{
						if (!System.Array.Find(Files, x => x != null && x.GetName() == scene.name))
						{
							EditorSceneManager.CloseScene(scene, true);
							break;
						}
					}
				}
				i--;
			}
		}
		if (Data == null && Files.Length > 0)
		{
			LoadData(Files[0]);
		}
	}

	public void LoadBoneMapping()
	{
		BoneMapping = new int[GetActor().Bones.Length];
		for (int i = 0; i < GetActor().Bones.Length; i++)
		{
			MotionData.Hierarchy.Bone bone = Data.Source.FindBone(GetActor().Bones[i].GetName());
			BoneMapping[i] = bone == null ? -1 : bone.Index;
		}
	}

	public void UpdateBoneMapping()
	{
		if (Data == null)
		{
			BoneMapping = new int[0];
		}
		else
		{
			if (BoneMapping == null || BoneMapping.Length != GetActor().Bones.Length)
			{
				LoadBoneMapping();
			}
		}
	}

	public void SetCallbacks(bool value)
	{
		if (Callbacks != value)
		{
			Callbacks = value;
			LoadFrame(Timestamp);
		}
	}

	public void SetMirror(bool value)
	{
		if (Mirror != value)
		{
			Mirror = value;
			LoadFrame(Timestamp);
		}
	}

	public void SetOffset(Vector3 value)
	{
		if (Data.Offset != value)
		{
			Data.Offset = value;
			LoadFrame(Timestamp);
		}
	}

	public void SetScale(float value)
	{
		if (Data.Scale != value)
		{
			Data.Scale = value;
			LoadFrame(Timestamp);
		}
	}

	public void SetTargetFramerate(float value)
	{
		TargetFramerate = Data == null ? 1 : Mathf.Clamp(value, 1, Data.Framerate);
	}

	public void SetRandomSeed(int value)
	{
		RandomSeed = Mathf.Max(value, 0);
	}

	public int GetCurrentSeed()
	{
		if (RandomSeed == 0)
		{
			Frame frame = GetCurrentFrame();
			return frame == null ? 0 : frame.Index;
		}
		else
		{
			return RandomSeed;
		}
	}

	public void SetCameraFocus(bool value)
	{
		if (CameraFocus != value)
		{
			CameraFocus = value;
			if (!CameraFocus)
			{
				Vector3 position = SceneView.lastActiveSceneView.camera.transform.position;
				Quaternion rotation = Quaternion.Euler(0f, SceneView.lastActiveSceneView.camera.transform.rotation.eulerAngles.y, 0f);
				SceneView.lastActiveSceneView.LookAtDirect(position, rotation, 0f);
			}
			LoadFrame(Timestamp);
		}
	}

	public float GetWindow()
	{
		return Data == null ? 0f : Zoom * Data.GetTotalTime();
	}

	public Vector3Int GetView()
	{
		float startTime = GetCurrentFrame().Timestamp - GetWindow() / 2f;
		float endTime = GetCurrentFrame().Timestamp + GetWindow() / 2f;
		if (startTime < 0f)
		{
			endTime -= startTime;
			startTime = 0f;
		}
		if (endTime > Data.GetTotalTime())
		{
			startTime -= endTime - Data.GetTotalTime();
			endTime = Data.GetTotalTime();
		}
		startTime = Mathf.Max(0f, startTime);
		endTime = Mathf.Min(Data.GetTotalTime(), endTime);
		int start = Data.GetFrame(startTime).Index;
		int end = Data.GetFrame(endTime).Index;
		int elements = end - start + 1;
		return new Vector3Int(start, end, elements);
	}

	public void SetCharacter(Actor character)
	{
		if (Character == null && character != null)
		{
			if (Actor != null)
			{
				Utility.Destroy(Actor.gameObject);
				Actor = null;
			}
			Character = character;
			LoadFrame(Timestamp);
		}
		else
		{
			Character = character;
		}
	}

	public MotionData GetData()
	{
		return Data;
	}

	public Actor GetActor()
	{
		if (Character != null)
		{
			return Character;
		}
		if (Actor == null)
		{
			Actor = Data.CreateActor();
			Actor.transform.SetParent(transform);
		}
		return Actor;
	}

	public float RoundToTargetTime(float time)
	{
		return Mathf.RoundToInt(time * TargetFramerate) / TargetFramerate;
	}

	public float CeilToTargetTime(float time)
	{
		return Mathf.CeilToInt(time * TargetFramerate) / TargetFramerate;
	}

	public float FloorToTargetTime(float time)
	{
		return Mathf.FloorToInt(time * TargetFramerate) / TargetFramerate;
	}


	public Frame GetCurrentFrame()
	{
		return Data == null ? null : Data.GetFrame(Timestamp);
	}

	public void Import()
	{
		LoadData((MotionData)null);
		string[] assets = AssetDatabase.FindAssets("t:MotionData", new string[1] { Folder });
		Files = new MotionData[assets.Length];
		for (int i = 0; i < assets.Length; i++)
		{
			Files[i] = (MotionData)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[i]), typeof(MotionData));
		}
	}

	public void LoadData(string name)
	{
		if (Data != null && Data.GetName() == name)
		{
			return;
		}
		MotionData data = System.Array.Find(Files, x => x.GetName() == name);
		if (data == null)
		{
			Debug.Log("Data " + name + " could not be found.");
			return;
		}
		LoadData(data);
	}

	public void LoadData(MotionData data)
	{
		if (Data != data)
		{
			if (Data != null)
			{
				if (Save)
				{
					Data.Save();
				}
				Data.Unload();
			}
			if (Character == null && Actor != null)
			{
				Utility.Destroy(Actor.gameObject);
			}
			Data = data;
			if (Data != null)
			{
				Data.Load();
				LoadBoneMapping();
				LoadFrame(0f);
			}
		}
	}

	public void LoadPreviousData()
	{
		if (Data == null)
		{
			return;
		}
		LoadData(Files[Mathf.Max(System.Array.FindIndex(Files, x => x == Data) - 1, 0)]);
	}

	public void LoadNextData()
	{
		if (Data == null)
		{
			return;
		}
		LoadData(Files[Mathf.Min(System.Array.FindIndex(Files, x => x == Data) + 1, Files.Length - 1)]);
	}

	public void LoadFrame(float timestamp)
	{
		Timestamp = timestamp;
		Actor actor = GetActor();
		Scene scene = Data.GetScene();
		Frame frame = GetCurrentFrame();
		RootModule module = (RootModule)Data.GetModule(Module.ID.Root);
		Matrix4x4 root = module == null ? frame.GetBoneTransformation(0, Mirror) : module.GetRootTransformation(frame, Mirror);
		actor.transform.position = root.GetPosition();
		actor.transform.rotation = root.GetRotation();

		UpdateBoneMapping();
		for (int i = 0; i < actor.Bones.Length; i++)
		{
			if (BoneMapping[i] == -1)
			{
				Debug.Log("Bone " + actor.Bones[i].GetName() + " could not be mapped.");
			}
			else
			{
				Matrix4x4 transformation = frame.GetBoneTransformation(BoneMapping[i], Mirror);
				Vector3 velocity = frame.GetBoneVelocity(BoneMapping[i], Mirror, 1f / TargetFramerate);
				Vector3 acceleration = frame.GetBoneAcceleration(BoneMapping[i], Mirror, 1f / TargetFramerate);
				Vector3 force = frame.GetBoneMass(BoneMapping[i], Mirror) * acceleration;
				actor.Bones[i].Transform.position = transformation.GetPosition();
				actor.Bones[i].Transform.rotation = transformation.GetRotation();
				actor.Bones[i].Velocity = velocity;
				actor.Bones[i].Acceleration = acceleration;
				actor.Bones[i].Force = force;
			}
		}
		foreach (GameObject instance in scene.GetRootGameObjects())
		{
			instance.transform.localScale = Vector3.one.GetMirror(Mirror ? Data.MirrorAxis : Axis.None);
			foreach (SceneEvent e in instance.GetComponentsInChildren<SceneEvent>(true))
			{
			
			}
		}
		foreach (Module m in Data.Modules)
		{
		}
		if (CameraFocus)
		{
			if (SceneView.lastActiveSceneView != null)
			{
				/*
				Vector3 lastPosition = SceneView.lastActiveSceneView.camera.transform.position;
				Quaternion lastRotation = SceneView.lastActiveSceneView.camera.transform.rotation;
				Vector3 position = GetActor().GetRoot().position;
				position.y += FocusHeight;
				Quaternion rotation = GetActor().GetRoot().rotation;
				rotation.x = 0f;
				rotation.z = 0f;
				rotation = Quaternion.Euler(0f, Mirror ? Mathf.Repeat(FocusAngle + 0f, 360f) : FocusAngle, 0f) * rotation;
				position += FocusOffset * (rotation * Vector3.right);
				SceneView.lastActiveSceneView.LookAtDirect(Vector3.Lerp(lastPosition, position, 1f-FocusSmoothing), Quaternion.Slerp(lastRotation, rotation, (1f-FocusSmoothing)), FocusDistance*(1f-FocusSmoothing));
				*/

				Vector3 lastPosition = SceneView.lastActiveSceneView.camera.transform.position;
				Quaternion lastRotation = SceneView.lastActiveSceneView.camera.transform.rotation;
				Vector3 position = GetActor().GetRoot().position;
				position += Quaternion.Euler(0f, FocusAngle, 0f) * (FocusDistance * Vector3.forward);
				position.y += FocusHeight;
				Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(GetActor().GetRoot().position - position, Vector3.up).normalized, Vector3.up);
				SceneView.lastActiveSceneView.LookAtDirect(Vector3.Lerp(lastPosition, position, 1f - FocusSmoothing), Quaternion.Slerp(lastRotation, rotation, (1f - FocusSmoothing)), FocusDistance * (1f - FocusSmoothing));
			}
		}
	}

	public void LoadFrame(int index)
	{
		LoadFrame(Data.GetFrame(index).Timestamp);
	}

	public void LoadFrame(Frame frame)
	{
		LoadFrame(frame.Index);
	}

	public void PlayAnimation()
	{
		if (Playing)
		{
			return;
		}
		Playing = true;
		EditorCoroutines.StartCoroutine(Play(), this);
	}

	public void StopAnimation()
	{
		if (!Playing)
		{
			return;
		}
		Playing = false;
		EditorCoroutines.StopCoroutine(Play(), this);
	}

	private IEnumerator Play()
	{
		System.DateTime previous = Utility.GetTimestamp();
		while (Data != null)
		{
			float delta = Timescale * (float)Utility.GetElapsedTime(previous);
			if (delta > 1f / TargetFramerate)
			{
				previous = Utility.GetTimestamp();
				LoadFrame(Mathf.Repeat(Timestamp + delta, Data.GetTotalTime()));
			}
			yield return new WaitForSeconds(0f);
		}
	}

	public void Draw()
	{
		
	}

	void OnRenderObject()
	{
		Draw();
	}

	void OnDrawGizmos()
	{
		if (!Application.isPlaying)
		{
			OnRenderObject();
		}
	}

	public void DrawPivot(Rect rect)
	{
		UltiDraw.Begin();
		Frame frame = GetCurrentFrame();
		Vector3 view = GetView();
		Vector3 bottom = new Vector3(0f, rect.yMax, 0f);
		Vector3 top = new Vector3(0f, rect.yMax - rect.height, 0f);
		float pStart = (float)(Data.GetFrame(Mathf.Clamp(frame.Timestamp - 1f, 0f, Data.GetTotalTime())).Index - view.x) / (view.z - 1);
		float pEnd = (float)(Data.GetFrame(Mathf.Clamp(frame.Timestamp + 1f, 0f, Data.GetTotalTime())).Index - view.x) / (view.z - 1);
		float pLeft = rect.x + pStart * rect.width;
		float pRight = rect.x + pEnd * rect.width;
		Vector3 pA = new Vector3(pLeft, rect.y, 0f);
		Vector3 pB = new Vector3(pRight, rect.y, 0f);
		Vector3 pC = new Vector3(pLeft, rect.y + rect.height, 0f);
		Vector3 pD = new Vector3(pRight, rect.y + rect.height, 0f);
		UltiDraw.DrawTriangle(pA, pC, pB, UltiDraw.White.Transparent(0.1f));
		UltiDraw.DrawTriangle(pB, pC, pD, UltiDraw.White.Transparent(0.1f));
		top.x = rect.xMin + (float)(frame.Index - view.x) / (view.z - 1) * rect.width;
		bottom.x = rect.xMin + (float)(frame.Index - view.x) / (view.z - 1) * rect.width;
		UltiDraw.DrawLine(top, bottom, UltiDraw.Yellow);
		UltiDraw.End();
	}
}
