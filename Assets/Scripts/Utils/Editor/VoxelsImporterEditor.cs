using UnityEditor;
using UnityEngine;

namespace Utils
{
	[CustomEditor(typeof(VoxelsImporter))]
	public class VoxelsImporterEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 5);

			rect.y += EditorGUIUtility.singleLineHeight;
			rect.height *= 0.6f;

			EditorGUI.DrawRect(rect, new Color(0, 1, 0.25f, 0.25f));

			var place = EditorGUI.ObjectField(
				rect,
				"Drag & drop",
				null,
				typeof(Transform),
				true
			) as Transform;

			rect.height /= 2;
			rect.y += rect.height;
			GUI.Label(rect, "to import ->");

			if (place)
				(target as VoxelsImporter).ImportSlicedImage(place);
		}
	}
}
