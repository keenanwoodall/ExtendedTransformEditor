using UnityEngine;
using UnityEditor;

namespace Beans.Unity.ETE
{
	[CustomEditor (typeof (Transform)), CanEditMultipleObjects]
	public class ExtendedTransformEditor : Editor
	{
		private class Content
		{
			public static Texture2D ResetTexture = EditorGUIUtility.isProSkin ? CreateTexture2D(Styles.iconWidth, Styles.iconHeight, Styles.iconResetPro) : CreateTexture2D(Styles.iconWidth, Styles.iconHeight, Styles.iconResetPersonal);

			public static readonly GUIContent Position	= new GUIContent ("Position", "The local position of this GameObject relative to the parent.");
			public static readonly GUIContent Rotation	= new GUIContent ("Rotation", "The local rotation of this Game Object relative to the parent.");
			public static readonly GUIContent Scale		= new GUIContent ("Scale", "The local scaling of this GameObject relative to the parent.");
			public static readonly GUIContent ResetPosition = new GUIContent (ResetTexture, "Reset the position.");
			public static readonly GUIContent ResetRotation = new GUIContent (ResetTexture, "Reset the rotation.");
			public static readonly GUIContent ResetScale = new GUIContent (ResetTexture, "Reset the scale.");

			public const string FloatingPointWarning = "Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.";

            private static Texture2D CreateTexture2D(int width, int height, string base64) {
                Texture2D tex = new Texture2D(width, height);
                tex.LoadImage(System.Convert.FromBase64String(base64));
                return tex;
            }
		}

		private class Styles
		{
			public static GUIStyle ResetButton;
			public const int iconWidth = 15;
			public const int iconHeight = 15;
			public static readonly string iconResetPersonal = "iVBORw0KGgoAAAANSUhEUgAAACQAAAAkCAYAAADhAJiYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAI/SURBVFhH7ZdLq05RGIA/9zoxYUDhmGCmMBBSlB/gkt/gHyhEkdxSSpkbSX6A20CSGblnwBwZMHArd89z2u9pddp7n7W/vQ6T76mn1be+s9/v3Wu/6137DEaM6MmsahyGRbgJ1+BiJ+ADvsL7+NmJmWY27sNb+AP/NOh3N3Ev9rnpVrbhc6xLoM2nuBWzyM3+CB5HVyj4jU8q3zkBS3EDrsc09i88iqcnPvXkIqZ3/BFP4ApsYiWexE+YXnsBe+HKpAFv43LMxaTvYBrjIA6FNeNSR6DLOBe7Mg+vYMT5iZuxE9ZKWsCuzDDJBCZ1FyPeY+y0+9zacbE10+UxNWFd2Zsi7h7Mxj4TF1rApTiFEfe6EznYgaPpWUNtu6kr42i7MPZ3HMNp2YlxFw+daGFBNXbBvhXxdziRkja6wLMp8OI2vlVjFyzoYG01TlKXUByUEh24JG+rUZZU4yQmdAxjCdUOGxzC9Dv/ti9Z231qUnWWSEYuYcTc70QTbUk1JTO/Grvg6R9xtzvRRl1SpVZGVmFsezdE1rZPkyqZjJzBiH3NiVxMpHQyrs4XjIR24X/Dw/UeRjI23Bl7tZ0Ok7mKkYzHkv8cFGE1eh7l4mNKV0YPYBE8Ul6jdXAW/bEm/M4CTmtGz2Mruc9xIb7AdHX8gWf4CN/gHFyGG3EdprF9SzyM5yY+FWILejCmd5yjBVysZqbiXe/GG+j7TF0CatOzz7i1O+2mPlvPx+id+wrhqW0Hfo8v8QF+xREj/jGDwV/bjcvhi8awDAAAAABJRU5ErkJggg==";
			public static readonly string iconResetPro = "iVBORw0KGgoAAAANSUhEUgAAACQAAAAkCAYAAADhAJiYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAKESURBVFhH7ZfLahRBFIano27EbHShYEw2xp2gLkRFUPABYiTP4BsIKglEQrwgCIJ7VyI+QLwsRMSdkosRF7pXcaELEwU1yfj9VaeGwpme7p6uZDUffBR9qvr0me6q6p5Gnz41yaytTLPZHKQ5jqO4WzH4jh/xdZZlqy6ymVDEAE7gM/yLeajvKV7Ann90V0h8Gt9hVd7iKUtTSKnqSThJcw0HXMCzgUvmVwVgLx7FIxjnXscpHuMNf1gDirmnnxnxA2dwyIa0Qd8BnMUVjLlrQ3qDBJM+T4vnuN+6C2HsEL7QiRGXrbsanKg5s+5SeB7gdusuDefswIdKYKzhCesuBydoNcUTWHemcjEBzlVRL5XIWMTyq4/BWtoBzZnSjykPcmherSqhMW5dxTBY+0xgxsK1Idd1n9Lx2MLdYeAghk1Pcyh3NVWFXMO4ocTwB3daVz4MOueGe+YtnAxyLvnUjrMWbhFvdAG9mwLa9FKzaK04ZG2LTgWFF6UIO3BKvlgr9ljbQst72t89D7FZ3+W4YuHAtMXrULzcdSF/va6kKEbXuu/TOS5auB06uxWVpBhBLr39A2cs3BkGdCoqZTEjGJb9byy17OOikhUjyHfTp3XMWbgYBquo1MXo7vzEwJh1bT1cXC/XV64MzzxuzqdtEVxYxTxSFYZeS/pzUB8SHcRhOyyEsXpM8Z0Rl6y7HiQaxU+oeXALR6yrDfWhJnA8Z8QdG5JL2Y/8XTTvMb472tWXcQE/4zbch8fwMMa51/AqH/m3/WECKOok6kuvKprAaebM/5A4w/P4BPU9k4c2vTkcw0qrqeelx4X0GPXL9Qmht7b+p33DD/iGx/OLtk+fLabR+AeZYZ4+2Q2vaAAAAABJRU5ErkJggg==";

			static Styles ()
			{
                ResetButton = new GUIStyle { fixedWidth = iconWidth, fixedHeight = iconHeight, margin = new RectOffset(0, 0, 2, 0) };
			}
		}

		private class Properties
		{
			public SerializedProperty Position;
			public SerializedProperty Rotation;
			public SerializedProperty Scale;

			public Properties (SerializedObject obj)
			{
				Position	= obj.FindProperty ("m_LocalPosition");
				Rotation	= obj.FindProperty ("m_LocalRotation");
				Scale		= obj.FindProperty ("m_LocalScale");
			}
		}

		private const int MaxDistanceFromOrigin = 100000;
		private const int ContentWidth = 60;

		private float xyRatio, xzRatio;

		private Properties properties;
		private TransformRotationGUI rotationGUI;

		private void OnEnable ()
		{
			properties = new Properties (serializedObject);

			if (rotationGUI == null)
				rotationGUI = new TransformRotationGUI ();
			rotationGUI.Initialize (properties.Rotation, Content.Rotation);
		}

		public override void OnInspectorGUI ()
		{
			if (!EditorGUIUtility.wideMode)
			{
				EditorGUIUtility.wideMode = true;
				EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 212;
			}

			serializedObject.UpdateIfRequiredOrScript ();

			using (new EditorGUILayout.HorizontalScope ())
			{
				EditorGUILayout.PropertyField (properties.Position, Content.Position);
				if (GUILayout.Button (Content.ResetPosition, Styles.ResetButton))
					properties.Position.vector3Value = Vector3.zero;
			}
			using (new EditorGUILayout.HorizontalScope ())
			{
				rotationGUI.Draw ();
				if (GUILayout.Button (Content.ResetRotation, Styles.ResetButton))
				{
					rotationGUI.Reset ();
					if (Tools.current == Tool.Rotate)
					{
						if (Tools.pivotRotation == PivotRotation.Global)
						{
							Tools.handleRotation = Quaternion.identity;
							SceneView.RepaintAll ();
						}
					}
				}
			}
			using (new EditorGUILayout.HorizontalScope ())
			{
				EditorGUILayout.PropertyField (properties.Scale, Content.Scale);
				if (GUILayout.Button (Content.ResetScale, Styles.ResetButton))
					properties.Scale.vector3Value = Vector3.one;
			}

			// I can hard code this b/c the transform inspector is always drawn in the same spot lmao
			var dragRect = new Rect (16, 105, EditorGUIUtility.labelWidth - 10, 10);

			var e = Event.current;
			if (dragRect.Contains (e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
			{
				var currentScale = properties.Scale.vector3Value;
				xyRatio = currentScale.y / currentScale.x;
				xzRatio = currentScale.z / currentScale.x;
			}

			using (var check = new EditorGUI.ChangeCheckScope ())
			{
				var c = GUI.color;
				GUI.color = Color.clear;
				var newScaleX = CustomFloatField.Draw (new Rect (), dragRect, properties.Scale.vector3Value.x, EditorStyles.numberField);

				if (check.changed)
				{
					var currentScale = properties.Scale.vector3Value;

					var delta = newScaleX - properties.Scale.vector3Value.x;

					currentScale.x += delta;
					currentScale.y += delta * xyRatio;
					currentScale.z += delta * xzRatio;

					properties.Scale.vector3Value = currentScale;
				}

				GUI.color = c;
			}

			serializedObject.ApplyModifiedProperties ();

			EditorGUIUtility.labelWidth = 0;

			var transform = target as Transform;
			var position = transform.position;

			if
			(
				Mathf.Abs (position.x) > MaxDistanceFromOrigin ||
				Mathf.Abs (position.y) > MaxDistanceFromOrigin ||
				Mathf.Abs (position.z) > MaxDistanceFromOrigin
			)
				EditorGUILayout.HelpBox (Content.FloatingPointWarning, MessageType.Warning);
		}

		[MenuItem ("CONTEXT/Transform/Set Random Rotation")]
		private static void RandomRotation (MenuCommand command)
		{
			var transform = command.context as Transform;

			Undo.RecordObject (transform, "Set Random Rotation");
			transform.rotation = Random.rotation;
		}

		[MenuItem ("CONTEXT/Transform/Snap to Ground")]
		private static void SnapToGround (MenuCommand command)
		{
			var transform = command.context as Transform;

			RaycastHit hit;
			if (Physics.Raycast (transform.position, Vector3.down, out hit))
			{
				Undo.RecordObject (transform, "Snapped To Ground");
				transform.position = hit.point;
			}
		}

		[MenuItem ("CONTEXT/Transform/Snap to Ground (Physics)", true)]
		private static bool ValidateSnapToGroundPhysics (MenuCommand command)
		{
			return ((Transform)command.context).GetComponent<Collider> () != null;
		}

		private static void DrawDebugPoint (Vector3 point)
		{
			Debug.DrawRay (point, Vector3.forward * 0.05f, Color.blue, 5f, true);
			Debug.DrawRay (point, Vector3.right * 0.05f, Color.red, 5f, true);
			Debug.DrawRay (point, Vector3.up * 0.05f, Color.green, 5f, true);
		}

        private static T LoadResource<T>(string path) where T : Object
        {
            return (T) EditorGUIUtility.Load(path);
        }
	}
}
