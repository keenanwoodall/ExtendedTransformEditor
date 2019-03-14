using UnityEngine;
using UnityEditor;

namespace Beans.Unity.ETE
{
	[CustomEditor (typeof (Transform)), CanEditMultipleObjects]
	public class ExtendedTransformEditor : Editor
	{
		private class Content
		{
			public static Texture2D ResetTexture = AssetDatabasex.LoadAssetOfType<Texture2D> (EditorGUIUtility.isProSkin ? "ETE_Pro_Reset" : "ETE_Personal_Reset");

			public static readonly GUIContent Position	= new GUIContent ("Position", "The local position of this GameObject relative to the parent.");
			public static readonly GUIContent Rotation	= new GUIContent ("Rotation", "The local rotation of this Game Object relative to the parent.");
			public static readonly GUIContent Scale		= new GUIContent ("Scale", "The local scaling of this GameObject relative to the parent.");
			public static readonly GUIContent ResetPosition = new GUIContent (ResetTexture, "Reset the position.");
			public static readonly GUIContent ResetRotation = new GUIContent (ResetTexture, "Reset the rotation.");
			public static readonly GUIContent ResetScale = new GUIContent (ResetTexture, "Reset the scale.");

			public const string FloatingPointWarning = "Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.";
		}

		private class Styles
		{
			public static GUIStyle ResetButton;

			static Styles ()
			{
				ResetButton = new GUIStyle ()
				{
					margin = new RectOffset (0, 0, 2, 0),
					fixedWidth = 15,
					fixedHeight = 15
				};
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
				using (new EditorGUI.DisabledGroupScope (properties.Position.vector3Value == Vector3.zero))
					if (GUILayout.Button (Content.ResetPosition, Styles.ResetButton))
						properties.Position.vector3Value = Vector3.zero;
			}
			using (new EditorGUILayout.HorizontalScope ())
			{
				rotationGUI.Draw ();
				using (new EditorGUI.DisabledGroupScope (rotationGUI.eulerAngles == Vector3.zero))
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
				using (new EditorGUI.DisabledGroupScope (properties.Scale.vector3Value == Vector3.one))
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
	}
}