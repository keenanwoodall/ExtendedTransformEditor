using UnityEngine;
using UnityEditor;

namespace ExtendedTransformInspector
{
	[CustomEditor (typeof (Transform)), CanEditMultipleObjects]
	public class ExtendedTransformEditor : Editor
	{
		private class Content
		{
			public static Texture2D ResetTexture = Resources.Load<Texture2D> (EditorGUIUtility.isProSkin ? "Textures/ETI_Pro_Reset" : "Textures/ETI_Personal_Reset");

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
			public static GUISkin Skin;
			public static GUIStyle ResetButton;

			static Styles ()
			{
				Skin = Resources.Load<GUISkin> ("ETI");
				ResetButton = Skin.GetStyle ("button");
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
					properties.Rotation.quaternionValue = Quaternion.identity;
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
	}
}