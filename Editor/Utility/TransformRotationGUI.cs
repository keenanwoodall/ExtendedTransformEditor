using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Beans.Unity.ETE
{
	/// <summary>
	/// Abstracts the reflection required to use the internal TransformRotationGUI class.
	/// </summary>
	public class TransformRotationGUI
	{
		private object transformRotationGUI;
		private MethodInfo onEnable;
		private MethodInfo rotationField;
		private MethodInfo setLocalEulerAngles;

		private SerializedProperty property;

		public TransformRotationGUI ()
		{
			if (transformRotationGUI == null)
			{
				var unityEditorType = Type.GetType ("UnityEditor.TransformRotationGUI,UnityEditor");
				var transformType = typeof (Transform);

				onEnable = unityEditorType.GetMethod ("OnEnable");
				rotationField = unityEditorType.GetMethod ("RotationField", new Type[] { });
				setLocalEulerAngles = transformType.GetMethod ("SetLocalEulerAngles", BindingFlags.Instance | BindingFlags.NonPublic);

				transformRotationGUI = Activator.CreateInstance (unityEditorType);
			}
		}

		/// <summary>
		/// Initializes the GUI.
		/// </summary>
		/// <param name="property">A serialized quaternion property.</param>
		/// <param name="content">The content to draw the property with.</param>
		public void Initialize (SerializedProperty property, GUIContent content)
		{
			this.property = property;
			onEnable.Invoke (transformRotationGUI, new object[] { property, content });
		}

		/// <summary>
		/// Draws the rotation GUI.
		/// </summary>
		public void Draw ()
		{
			rotationField.Invoke (transformRotationGUI, null);
		}

		public void Reset ()
		{
			var targets = property.serializedObject.targetObjects;
			var parameters = new object[] { Vector3.zero, 0 };

			Undo.RecordObjects (targets, "Reset Rotation");
			foreach (var target in targets)
				setLocalEulerAngles.Invoke (target, parameters);
		}
	}
}