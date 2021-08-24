#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public class BVHImporterENVRH : EditorWindow {

	public static EditorWindow Window;
	public static Vector2 Scroll;

	public bool Flip = false;
	public Axis Axis = Axis.XPositive;

	public string Source = string.Empty;
	public string Destination = string.Empty;
	public string Filter = string.Empty;
	public File[] Files = new File[0];
	public File[] Instances = new File[0];
	public File[] EnvFiles = new File[0];
	public bool Importing = false;
	
	public int Page = 1;
	public const int Items = 25;

	[MenuItem ("AI4Animation/BVH Importer ENVRH")]
	static void Init() {
		Window = EditorWindow.GetWindow(typeof(BVHImporterENVRH));
		Scroll = Vector3.zero;
	}
	
	void OnGUI() {
		Scroll = EditorGUILayout.BeginScrollView(Scroll);

		Utility.SetGUIColor(UltiDraw.Black);
		using(new EditorGUILayout.VerticalScope ("Box")) {
			Utility.ResetGUIColor();

			Utility.SetGUIColor(UltiDraw.Grey);
			using(new EditorGUILayout.VerticalScope ("Box")) {
				Utility.ResetGUIColor();

				Utility.SetGUIColor(UltiDraw.Orange);
				using(new EditorGUILayout.VerticalScope ("Box")) {
					Utility.ResetGUIColor();
					EditorGUILayout.LabelField("BVH Importer");
				}
		
				if(!Importing) {
					if(Utility.GUIButton("Import Motion Data", UltiDraw.DarkGrey, UltiDraw.White)) {
						this.StartCoroutine(ImportMotionData());
					}
				} else {
					if(Utility.GUIButton("Stop", UltiDraw.DarkRed, UltiDraw.White)) {
						this.StopAllCoroutines();
						Importing = false;
					}
				}

				using(new EditorGUILayout.VerticalScope ("Box")) {
					EditorGUILayout.LabelField("Source");
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("<Path>", GUILayout.Width(50));
					Source = EditorGUILayout.TextField(Source);
					GUI.skin.button.alignment = TextAnchor.MiddleCenter;
					if(GUILayout.Button("O", GUILayout.Width(20))) {
						Source = EditorUtility.OpenFolderPanel("BVH Importer", Source == string.Empty ? Application.dataPath : Source, "");
						GUIUtility.ExitGUI();
					}
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.LabelField("Destination");
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Assets/", GUILayout.Width(50));
					Destination = EditorGUILayout.TextField(Destination);
					EditorGUILayout.EndHorizontal();

					string filter = EditorGUILayout.TextField("Filter", Filter);
					if(Filter != filter) {
						Filter = filter;
						ApplyFilter();
					}

					Flip = EditorGUILayout.Toggle("Flip", Flip);
					Axis = (Axis)EditorGUILayout.EnumPopup("Axis", Axis);

					if(Utility.GUIButton("Load Directory", UltiDraw.DarkGrey, UltiDraw.White)) {
						LoadDirectory();
					}

					int start = (Page-1)*Items;
					int end = Mathf.Min(start+Items, Instances.Length);
					int pages = Mathf.CeilToInt(Instances.Length/Items)+1;
					Utility.SetGUIColor(UltiDraw.Orange);
					using(new EditorGUILayout.VerticalScope ("Box")) {
						Utility.ResetGUIColor();
						EditorGUILayout.BeginHorizontal();
						if(Utility.GUIButton("<", UltiDraw.DarkGrey, UltiDraw.White)) {
							Page = Mathf.Max(Page-1, 1);
						}
						EditorGUILayout.LabelField("Page " + Page + "/" + pages);
						if(Utility.GUIButton(">", UltiDraw.DarkGrey, UltiDraw.White)) {
							Page = Mathf.Min(Page+1, pages);
						}
						EditorGUILayout.EndHorizontal();
					}
					EditorGUILayout.BeginHorizontal();
					if(Utility.GUIButton("Enable All", UltiDraw.DarkGrey, UltiDraw.White)) {
						for(int i=0; i<Instances.Length; i++) {
							Instances[i].Import = true;
						}
					}
					if(Utility.GUIButton("Disable All", UltiDraw.DarkGrey, UltiDraw.White)) {
						for(int i=0; i<Instances.Length; i++) {
							Instances[i].Import = false;
						}
					}
					EditorGUILayout.EndHorizontal();
					for(int i=start; i<end; i++) {
						if(Instances[i].Import) {
							Utility.SetGUIColor(UltiDraw.DarkGreen);
						} else {
							Utility.SetGUIColor(UltiDraw.DarkRed);
						}
						using(new EditorGUILayout.VerticalScope ("Box")) {
							Utility.ResetGUIColor();
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.LabelField((i+1).ToString(), GUILayout.Width(20f));
							Instances[i].Import = EditorGUILayout.Toggle(Instances[i].Import, GUILayout.Width(20f));
							EditorGUILayout.LabelField(Instances[i].Object.Name);
							EditorGUILayout.EndHorizontal();
						}
					}
					
                }
		
			}
		}

		EditorGUILayout.EndScrollView();
	}
	private void LoadENVDirectory()
	{
		if (Directory.Exists(Source))
		{
			DirectoryInfo info = new DirectoryInfo(Source);
			FileInfo[] items = info.GetFiles("*.txt");
			EnvFiles = new File[items.Length];
			for (int i = 0; i < items.Length; i++)
			{
				EnvFiles[i] = new File();
				EnvFiles[i].Object = items[i];
				EnvFiles[i].Import = true;
			}
		}
		else
		{
			EnvFiles = new File[0];
		}
		Page = 1;
	}
	private void LoadDirectory() {
		if(Directory.Exists(Source)) {
			DirectoryInfo info = new DirectoryInfo(Source);
			FileInfo[] items = info.GetFiles("*.bvh");
			Files = new File[items.Length];
			for(int i=0; i<items.Length; i++) {
				Files[i] = new File();
				Files[i].Object = items[i];
				Files[i].Import = true;
			}
		} else {
			Files = new File[0];
		}
		ApplyFilter();
		Page = 1;
	}
	
	private void ApplyFilter() {
		if(Filter == string.Empty) {
			Instances = Files;
		} else {
			List<File> instances = new List<File>();
			for(int i=0; i<Files.Length; i++) {
				if(Files[i].Object.Name.ToLowerInvariant().Contains(Filter.ToLowerInvariant())) {
					instances.Add(Files[i]);
				}
			}
			Instances = instances.ToArray();
		}
	}

	public Vector4 loadVector(string sx, string sy, string sz, float scale, bool b_MW)
    {
		Vector3 pos1 = new Vector3();
		if (b_MW == true)
			pos1.x = FileUtility.ReadFloat(sx) * -1.0f * scale;
		else
			pos1.x = FileUtility.ReadFloat(sx) * scale;

		pos1.y = FileUtility.ReadFloat(sy) * scale;
		pos1.z = FileUtility.ReadFloat(sz) * scale;
		
		return pos1;
	}
	private IEnumerator ImportMotionData() {
		string destination = "Assets/" + Destination;
		if(!AssetDatabase.IsValidFolder(destination)) {
			Debug.Log("Folder " + "'" + destination + "'" + " is not valid.");
		} else {
			Importing = true;
			for(int f=0; f<Files.Length; f++) {
				if(Files[f].Import) {
					string fileName = Files[f].Object.Name.Replace(".bvh", "");
										
					if (!Directory.Exists(destination+"/"+fileName) ) {
						AssetDatabase.CreateFolder(destination, fileName);
						MotionData data = ScriptableObject.CreateInstance<MotionData>();
						data.name = fileName;
						AssetDatabase.CreateAsset(data, destination+"/"+fileName+"/"+data.name+".asset");

						string[] lines = System.IO.File.ReadAllLines(Files[f].Object.FullName);
						char[] whitespace = new char[] {' '};
						int index = 0;

						//Create Source Data
						List<Vector3> offsets = new List<Vector3>();
						List<int[]> channels = new List<int[]>();
						List<float[]> motions = new List<float[]>();
						data.Source = new MotionData.Hierarchy();
						string name = string.Empty;
						string parent = string.Empty;
						Vector3 offset = Vector3.zero;
						int[] channel = null;
						for(index = 0; index<lines.Length; index++) {
							if(lines[index] == "MOTION") {
								break;
							}
							string[] entries = lines[index].Split(whitespace);
							for(int entry=0; entry<entries.Length; entry++) {
								if(entries[entry].Contains("ROOT")) {
									parent = "None";
									name = entries[entry+1];
									break;
								} else if(entries[entry].Contains("JOINT")) {
									parent = name;
									name = entries[entry+1];
									break;
								} else if(entries[entry].Contains("End")) {
									parent = name;
									name = name+entries[entry+1];
									string[] subEntries = lines[index+2].Split(whitespace);
									for(int subEntry=0; subEntry<subEntries.Length; subEntry++) {
										if(subEntries[subEntry].Contains("OFFSET")) {
											offset.x = FileUtility.ReadFloat(subEntries[subEntry+1]);
											offset.y = FileUtility.ReadFloat(subEntries[subEntry+2]);
											offset.z = FileUtility.ReadFloat(subEntries[subEntry+3]);
											break;
										}
									}
									data.Source.AddBone(name, parent);
									offsets.Add(offset);
									channels.Add(new int[0]);
									index += 2;
									break;
								} else if(entries[entry].Contains("OFFSET")) {
									offset.x = FileUtility.ReadFloat(entries[entry+1]);
									offset.y = FileUtility.ReadFloat(entries[entry+2]);
									offset.z = FileUtility.ReadFloat(entries[entry+3]);
									break;
								} else if(entries[entry].Contains("CHANNELS")) {
									channel = new int[FileUtility.ReadInt(entries[entry+1])];
									for(int i=0; i<channel.Length; i++) {
										if(entries[entry+2+i] == "Xposition") {
											channel[i] = 1;
										} else if(entries[entry+2+i] == "Yposition") {
											channel[i] = 2;
										} else if(entries[entry+2+i] == "Zposition") {
											channel[i] = 3;
										} else if(entries[entry+2+i] == "Xrotation") {
											channel[i] = 4;
										} else if(entries[entry+2+i] == "Yrotation") {
											channel[i] = 5;
										} else if(entries[entry+2+i] == "Zrotation") {
											channel[i] = 6;
										}
									}
									data.Source.AddBone(name, parent);
									offsets.Add(offset);
									channels.Add(channel);
									break;
								} else if(entries[entry].Contains("}")) {
									name = parent;
									parent = name == "None" ? "None" : data.Source.FindBone(name).Parent;
									break;
								}
							}
						}

						//Set Frames
						index += 1;
						while(lines[index].Length == 0) {
							index += 1;
						}
						ArrayExtensions.Resize(ref data.Frames, FileUtility.ReadInt(lines[index].Substring(8)));

						//Set Framerate
						index += 1;
						data.Framerate = Mathf.RoundToInt(1f / FileUtility.ReadFloat(lines[index].Substring(12)));

						//Compute Frames
						index += 1;
						for(int i=index; i<lines.Length; i++) {
							motions.Add(FileUtility.ReadArray(lines[i]));
						}
						for(int k=0; k<data.GetTotalFrames(); k++) {
							data.Frames[k] = new Frame(data, k+1, (float)k / data.Framerate);
							int idx = 0;
							for(int i=0; i<data.Source.Bones.Length; i++) {
								MotionData.Hierarchy.Bone info = data.Source.Bones[i];
								Vector3 position = Vector3.zero;
								Quaternion rotation = Quaternion.identity;
								
								for (int j=0; j<channels[i].Length; j++) {
									if(channels[i][j] == 1) {
										position.x = motions[k][idx]; idx += 1;
									}
									if(channels[i][j] == 2) {
										position.y = motions[k][idx]; idx += 1;
									}
									if(channels[i][j] == 3) {
										position.z = motions[k][idx]; idx += 1;
									}
									if(channels[i][j] == 4) {
										rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.right); idx += 1;
									}
									if(channels[i][j] == 5) {
										rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.up); idx += 1;
									}
									if(channels[i][j] == 6) {
										rotation *= Quaternion.AngleAxis(motions[k][idx], Vector3.forward); idx += 1;
									}
								}
								
								position = (position == Vector3.zero ? offsets[i] : position) / 100f; //unit scale
								Matrix4x4 local = Matrix4x4.TRS(position, rotation, Vector3.one);
								local = local.GetMirror(Axis.XPositive);
								
								data.Frames[k].World[i] = info.Parent == "None" ? local : data.Frames[k].World[data.Source.FindBone(info.Parent).Index] * local;
							}
							
							/*
							for(int i=0; i<data.Source.Bones.Length; i++) {
								data.Frames[k].Local[i] *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(data.Corrections[i]), Vector3.one);
								data.Frames[k].World[i] *= Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(data.Corrections[i]), Vector3.one);
							}
							*/
						}

						if(data.GetTotalFrames() == 1) {
							Frame reference = data.Frames.First();
							ArrayExtensions.Resize(ref data.Frames, Mathf.RoundToInt(data.Framerate));
							for(int k=0; k<data.GetTotalFrames(); k++) {
								data.Frames[k] = new Frame(data, k+1, (float)k / data.Framerate);
								data.Frames[k].World = (Matrix4x4[])reference.World.Clone();
							}
						}


						//data 에 root 를 입력한다.
						LoadENVDirectory();
						for (int e = 0; e < EnvFiles.Length; e++)
						{
							string fileNameENV = EnvFiles[e].Object.Name.Replace(".txt", "");
							if (fileNameENV == fileName)
							{
								Debug.Log(" bvh file name " + fileName + " env file name " + fileNameENV);

								string[] lines_env = System.IO.File.ReadAllLines(EnvFiles[e].Object.FullName);
								char[] whitespace_env = new char[] { ' ' };
								for (int line = 0; line < lines_env.Length; line++)
								{
									string[] entries = lines_env[line].Split(whitespace_env);
									for (int entry = 0; entry < entries.Length; entry++)
									{
										if (entries[entry] == "RootMatPOSXYZ")
										{
											float scale = 0.01f;
											Vector4 pos = new Vector4();
											pos.Set(FileUtility.ReadFloat(entries[entry + 1]) * scale, FileUtility.ReadFloat(entries[entry + 2] ) * scale, FileUtility.ReadFloat(entries[entry + 3] ) * scale, 1.0f);
											Vector4 rotX = new Vector4();
											rotX.Set(FileUtility.ReadFloat(entries[entry + 4]) , FileUtility.ReadFloat(entries[entry + 5]) , FileUtility.ReadFloat(entries[entry + 6]) , 0.0f);
											Vector4 rotY = new Vector4();
											rotY.Set(FileUtility.ReadFloat(entries[entry + 7]) , FileUtility.ReadFloat(entries[entry + 8]) , FileUtility.ReadFloat(entries[entry + 9]) , 0.0f);
											Vector4 rotZ = new Vector4();
											rotZ.Set(FileUtility.ReadFloat(entries[entry + 10]) , FileUtility.ReadFloat(entries[entry + 11]) , FileUtility.ReadFloat(entries[entry + 12]) , 0.0f);
											
											Matrix4x4 chairroot = new Matrix4x4();
											chairroot.SetColumn(0, rotX); chairroot.SetColumn(1, rotY); chairroot.SetColumn(2, rotZ); chairroot.SetColumn(3, pos);
											data.chairroot = chairroot.GetMirror(Axis.XPositive);
											Debug.Log(data.chairroot.GetPosition());
											break;
										}
										if(entries[entry] == "seat")
                                        {
											float scale = 0.01f;
											data.seat_center = loadVector(entries[entry + 1], entries[entry + 2], entries[entry + 3], scale, true);
											data.seat_normal = loadVector(entries[entry + 4], entries[entry + 5], entries[entry + 6], scale, true);
											//Vector4 rotY = new Vector4();
											//rotY.Set(FileUtility.ReadFloat(entries[entry + 4]), FileUtility.ReadFloat(entries[entry + 5]), FileUtility.ReadFloat(entries[entry + 6]), 0.0f);
											//rotY = rotY * scale;
											Debug.Log(data.seat_center);
											break;
										}

                                        if (entries[entry] == "backrest")
                                        {
                                            float scale = 0.01f;
											data.backrest_center = loadVector(entries[entry + 1], entries[entry + 2], entries[entry + 3], scale, true);
											data.backrest_normal = loadVector(entries[entry + 4], entries[entry + 5], entries[entry + 6], scale, true);
											//Vector4 rotY = new Vector4();
											//rotY.Set(FileUtility.ReadFloat(entries[entry + 4]), FileUtility.ReadFloat(entries[entry + 5]), FileUtility.ReadFloat(entries[entry + 6]), 0.0f);
											//rotY = rotY * scale;
											Debug.Log(data.backrest_center);
                                            break;
                                        }
                                        if (entries[entry] == "armrest_L")
                                        {
                                            float scale = 0.01f;
                                            data.armrest_L_center = loadVector(entries[entry + 1], entries[entry + 2], entries[entry + 3], scale, true);
											data.armrest_L_normal = loadVector(entries[entry + 4], entries[entry + 5], entries[entry + 6], scale, true);
											//Vector4 rotY = new Vector4();
											//rotY.Set(FileUtility.ReadFloat(entries[entry + 4]), FileUtility.ReadFloat(entries[entry + 5]), FileUtility.ReadFloat(entries[entry + 6]), 0.0f);
											//rotY = rotY * scale;

											Debug.Log(data.armrest_L_center);
                                            break;
                                        }
                                        if (entries[entry] == "armrest_R")
                                        {
                                            float scale = 0.01f;
                                            data.armrest_R_center = loadVector(entries[entry + 1], entries[entry + 2], entries[entry + 3], scale, true);
											data.armrest_R_normal = loadVector(entries[entry + 4], entries[entry + 5], entries[entry + 6], scale, true);
											
											Debug.Log(data.armrest_R_center);
                                            break;
                                        }
                                        if (entries[entry] == "handrest_L")
                                        {
                                            float scale = 0.01f;
                                            data.handrest_L_center = loadVector(entries[entry + 1], entries[entry + 2], entries[entry + 3], scale, true);
											data.handrest_L_normal = loadVector(entries[entry + 4], entries[entry + 5], entries[entry + 6], scale, true);
											//Vector4 rotY = new Vector4();
											//rotY.Set(FileUtility.ReadFloat(entries[entry + 4]), FileUtility.ReadFloat(entries[entry + 5]), FileUtility.ReadFloat(entries[entry + 6]), 0.0f);
											//rotY = rotY * scale;
											Debug.Log(data.handrest_L_center);
                                            break;
                                        }
                                        if (entries[entry] == "handrest_R")
                                        {
                                            float scale = 0.01f;
											data.handrest_R_center = loadVector(entries[entry + 1], entries[entry + 2], entries[entry + 3], scale, true);
											data.handrest_R_normal = loadVector(entries[entry + 4], entries[entry + 5], entries[entry + 6], scale, true);
											//Vector4 rotY = new Vector4();
											//rotY.Set(FileUtility.ReadFloat(entries[entry + 4]), FileUtility.ReadFloat(entries[entry + 5]), FileUtility.ReadFloat(entries[entry + 6]), 0.0f);
											//rotY = rotY * scale;											
											Debug.Log(data.handrest_R_center);
                                            break;
                                        }
                                        if (entries[entry] == "footrest_L")
                                        {
                                            float scale = 0.01f;
                                            data.footrest_L_center = loadVector(entries[entry + 1], entries[entry + 2], entries[entry + 3], scale, true);
											data.footrest_L_normal = loadVector(entries[entry + 4], entries[entry + 5], entries[entry + 6], scale, true);
											//Vector4 rotY = new Vector4();
											//rotY.Set(FileUtility.ReadFloat(entries[entry + 4]), FileUtility.ReadFloat(entries[entry + 5]), FileUtility.ReadFloat(entries[entry + 6]), 0.0f);
											//rotY = rotY * scale;
											Debug.Log(data.footrest_L_center);
                                            break;
                                        }
                                        if (entries[entry] == "footrest_R")
                                        {
                                            float scale = 0.01f;
                                            data.footrest_R_center = loadVector(entries[entry + 1], entries[entry + 2], entries[entry + 3], scale, true);
											data.footrest_R_normal = loadVector(entries[entry + 4], entries[entry + 5], entries[entry + 6], scale, true);

											//Vector4 rotY = new Vector4();
											//rotY.Set(FileUtility.ReadFloat(entries[entry + 4]), FileUtility.ReadFloat(entries[entry + 5]), FileUtility.ReadFloat(entries[entry + 6]), 0.0f);
											//rotY = rotY * scale;

											Debug.Log(data.footrest_R_center);
                                            break;

                                        }

                                    }
								}
							}
						}


						//Detect Symmetry
						data.DetectSymmetry();

						//Add Scene
						data.CreateScene();
						data.AddSequence();

						//
						//Save
						EditorUtility.SetDirty(data);
					} else {
						Debug.Log("File with name " + fileName + " already exists.");
					}

					yield return new WaitForSeconds(0f);
				}
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Importing = false;
		}
		yield return new WaitForSeconds(0f);
	}

	[System.Serializable]
	public class File {
		public FileInfo Object;
		public bool Import;
	}

}
#endif

/*
string[] presets = new string[4] {"Select preset...", "Dan", "Dog", "Interaction"};
switch(EditorGUILayout.Popup(0, presets)) {
	case 0:
	break;
	case 1:
	Target.GetData().DepthMapAxis = MotionData.Axis.ZPositive;
	Target.GetData().MirrorAxis = MotionData.Axis.XPositive;
	for(int i=0; i<Target.GetData().Corrections.Length; i++) {
		Target.GetData().SetCorrection(i, Vector3.zero);
	}
	Target.GetData().ClearStyles();
	Target.GetData().AddStyle("Idle");
	Target.GetData().AddStyle("Walk");
	Target.GetData().AddStyle("Run");
	Target.GetData().AddStyle("Jump");
	Target.GetData().AddStyle("Crouch");
	break;

	case 2:
	Target.GetData().DepthMapAxis = MotionData.Axis.XPositive;
	Target.GetData().MirrorAxis = MotionData.Axis.ZPositive;
	for(int i=0; i<Target.GetData().Corrections.Length; i++) {
		if(i==4 || i==5 || i==6 || i==11) {
			Target.GetData().SetCorrection(i, new Vector3(90f, 90f, 90f));
		} else if(i==24) {
			Target.GetData().SetCorrection(i, new Vector3(-45f, 0f, 0f));
		} else {
			Target.GetData().SetCorrection(i, new Vector3(0f, 0f, 0f));
		}
	}
	Target.GetData().ClearStyles();
	Target.GetData().AddStyle("Idle");
	Target.GetData().AddStyle("Walk");
	Target.GetData().AddStyle("Pace");
	Target.GetData().AddStyle("Trot");
	Target.GetData().AddStyle("Canter");
	Target.GetData().AddStyle("Jump");
	Target.GetData().AddStyle("Sit");
	Target.GetData().AddStyle("Stand");
	Target.GetData().AddStyle("Lie");
	break;

	case 3:
	Target.GetData().DepthMapAxis = MotionData.Axis.ZPositive;
	Target.GetData().MirrorAxis = MotionData.Axis.XPositive;							
	for(int i=0; i<Target.GetData().Corrections.Length; i++) {
		Target.GetData().SetCorrection(i, Vector3.zero);
	}
	Target.GetData().ClearStyles();
	Target.GetData().AddStyle("Idle");
	Target.GetData().AddStyle("Walk");
	Target.GetData().AddStyle("Run");
	Target.GetData().AddStyle("Jump");
	Target.GetData().AddStyle("Crouch");
	Target.GetData().AddStyle("Sit");
	break;
}
*/