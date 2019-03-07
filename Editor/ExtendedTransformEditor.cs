using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Beans.Unity.ETE
{
	[CustomEditor (typeof (Transform)), CanEditMultipleObjects]
	public class ExtendedTransformEditor : Editor
	{
		private class Content
		{
			public static Texture2D ResetTexture = Resources.Load<Texture2D> (EditorGUIUtility.isProSkin ? "Textures/ETE_Pro_Reset" : "Textures/ETE_Personal_Reset");

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
				Skin = Resources.Load<GUISkin> ("ETE");
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

		[MenuItem ("CONTEXT/Transform/Snap to Ground (Bounds)")]
		private static void SnapToGround (MenuCommand command)
		{
			var transform = command.context as Transform;
			var origin = transform.position;

			var mf = transform.GetComponent<MeshFilter> ();
			if (mf != null)
				origin = transform.TransformPoint (mf.sharedMesh.bounds.ClosestPoint (transform.InverseTransformPoint (transform.position + Vector3.down * 1000)));
			else
			{
				var smr = transform.GetComponent<SkinnedMeshRenderer> ();
				if (smr != null)
					origin = transform.TransformPoint (smr.localBounds.ClosestPoint (transform.InverseTransformPoint (transform.position + Vector3.down * 1000)));
			}

			RaycastHit hit;
			if (Physics.Raycast (origin, Vector3.down, out hit))
			{
				Undo.RecordObject (transform, "Snapped To Ground");
				transform.position += hit.point - origin;
			}
		}

		[MenuItem ("CONTEXT/Transform/Snap to Ground (Physics)")]
		private static void SnapToGroundPhysics (MenuCommand command)
		{
			// Get the selected transform
			var transform = command.context as Transform;
			var origin = transform.position;

			RaycastHit hit;
			// Shoot a ray directly down
			if (Physics.Raycast (origin, Vector3.down, out hit))
			{
				Undo.RecordObject (transform, "Snapped To Ground");

				// Draw a point where the ray hit for debugging
				DrawDebugPoint (hit.point);

				// If the selected transform has a collider
				var collider = transform.GetComponent<Collider> ();
				if (collider != null)
				{
					// Compute the movement required to resolve any penetration
					var direction = Vector3.up;
					var distance = 0f;
					if (Physics.ComputePenetration
					(
						colliderA: collider,
						positionA: hit.point,
						rotationA: transform.rotation,
						colliderB: hit.collider,
						positionB: hit.transform.position,
						rotationB: hit.transform.rotation,
						direction: out direction,
						distance: out distance
					))
					{
						// Draw the movement vector
						Debug.DrawRay (hit.point, direction * distance, Color.yellow, 5f);

						// Apply the movement
						transform.position = hit.point + (direction * distance);
					}
					else
						// There wasn't any penetration so the transform cant jump directly to the hit point
						transform.position = hit.point;
				}
			}
		}

		private static void DrawDebugPoint (Vector3 point)
		{
			Debug.DrawRay (point, Vector3.forward * 0.05f, Color.blue, 5f, true);
			Debug.DrawRay (point, Vector3.right * 0.05f, Color.red, 5f, true);
			Debug.DrawRay (point, Vector3.up * 0.05f, Color.green, 5f, true);
		}
	}
}