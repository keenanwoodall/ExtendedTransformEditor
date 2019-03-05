using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System;

namespace ExtendedTransformInspector
{
	[CustomEditor (typeof (Transform)), CanEditMultipleObjects]
	public class ExtendedTransformEditor : Editor
	{
		private class Content
		{
			public static readonly GUIContent Position	= new GUIContent ("Position", "The local position of this GameObject relative to the parent.");
			public static readonly GUIContent Rotation	= new GUIContent ("Rotation", "The local rotation of this Game Object relative to the parent.");
			public static readonly GUIContent Scale		= new GUIContent ("Scale", "The local scaling of this GameObject relative to the parent.");

			public const string FloatingPointWarning = "Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.";
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

		private Properties properties;

		private object rotationGUI;
		private MethodInfo rotationGUIOnEnable;
		private MethodInfo rotationGUIRotationField;

		private void OnEnable ()
		{
			properties = new Properties (serializedObject);

			if (rotationGUI == null)
			{
				var type = Type.GetType ("UnityEditor.TransformRotationGUI,UnityEditor");

				rotationGUIOnEnable = type.GetMethod ("OnEnable");
				rotationGUIRotationField = type.GetMethod ("RotationField", new Type[] { });

				rotationGUI = Activator.CreateInstance (type);
			}

			rotationGUIOnEnable.Invoke (rotationGUI, new object[] { properties.Rotation, Content.Rotation });
		}

		public override void OnInspectorGUI ()
		{
			if (!EditorGUIUtility.wideMode)
			{
				EditorGUIUtility.wideMode = true;
				EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 212;
			}

			serializedObject.UpdateIfRequiredOrScript ();

			EditorGUILayout.PropertyField (properties.Position, Content.Position);
			rotationGUIRotationField.Invoke (rotationGUI, null);
			EditorGUILayout.PropertyField (properties.Scale, Content.Scale);

			serializedObject.ApplyModifiedProperties ();

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