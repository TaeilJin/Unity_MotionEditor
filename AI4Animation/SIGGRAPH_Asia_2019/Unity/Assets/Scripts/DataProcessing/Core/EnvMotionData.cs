#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class EnvMotionData : ScriptableObject {

	Matrix4x4 chairRoot;
	Vector3 seat_center;
	Vector3 seat_normal;

}
#endif