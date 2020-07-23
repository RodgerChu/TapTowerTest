using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utils
{
	[CreateAssetMenu(fileName = "VoxelsImporter", menuName = "Data/VoxelsImporter")]
	public class VoxelsImporter : ScriptableObject
	{
#if UNITY_EDITOR
		[System.Serializable]
		public class Element
		{
			public string name;

			public Color[] colors = System.Array.Empty<Color>();
		}

		[SerializeField]
		private Texture2D slices = null;

		[SerializeField]
		[Tooltip("Found unused colors in slices texture")]
		private Color[] forgottenColors = System.Array.Empty<Color>();

		[SerializeField]
		private Element[] elements = System.Array.Empty<Element>();

		private void OnValidate()
		{
			if (slices)
			{
				var slicesPath = AssetDatabase.GetAssetPath(slices);
				TextureImporter importer = AssetImporter.GetAtPath(slicesPath) as TextureImporter;
				
				Debug.Assert(importer);
				
				importer.textureType = TextureImporterType.Default;
				importer.isReadable = true;
				importer.npotScale = TextureImporterNPOTScale.None;
				importer.mipmapEnabled = false;
				importer.alphaSource = TextureImporterAlphaSource.FromInput;
				importer.alphaIsTransparency = true;
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				importer.wrapMode = TextureWrapMode.Clamp;
				importer.filterMode = FilterMode.Point;
				importer.SaveAndReimport();
			}

			UpdateForgottenColors();
		}

		public Element FindElement(Color color, out Color foundColor)
		{
			foreach (var coloredElement in elements)
				foreach (var elementColor in coloredElement.colors)
					if (Same(elementColor, color))
					{
						foundColor = elementColor;
						return coloredElement;
					}

			foundColor = color;
			return null;
		}

		public void ImportSlicedImage(Transform place)
		{
			var size = new Vector3Int(slices.width, slices.height / slices.width, slices.width);

			Debug.Log($"Importing image of size: {size}");

			bool[,,] visited = new bool[size.x, size.y, size.z];
			
			foreach (var child in place.Cast<Transform>().ToArray())
				DestroyImmediate(child.gameObject);

			Vector3Int from = Vector3Int.zero;
			for (from.y = 0; from.y < size.y; ++from.y)
				for (from.x = 0; from.x < size.x; ++from.x)
					for (from.z = 0; from.z < size.z; ++from.z)
					{
						if (visited[from.x, from.y, from.z])
							continue;

						var fromColor = GetColor(from, size);
						if (fromColor.a < 0.1f)
							continue;

						var fromElement = FindElement(fromColor, out fromColor);

						System.Func<Vector3Int, bool> checkSame = (coord) =>
						{
							if (visited[coord.x, coord.y, coord.z])
								return false;

							var curColor = GetColor(coord, size);
							if (curColor.a < 0.1f)
								return false;

							var curElement = FindElement(curColor, out _);
							return fromElement == curElement && Same(fromColor, curColor);
						};

						Vector3Int to = FindBlockFIFO(from, size, checkSame);

						Vector3Int mid = from;
						for (mid.x = from.x; mid.x <= to.x; ++mid.x)
							for (mid.y = from.y; mid.y <= to.y; ++mid.y)
								for (mid.z = from.z; mid.z <= to.z; ++mid.z)
									visited[mid.x, mid.y, mid.z] = true;

						// TODO: place a block at found placement
						Debug.LogError("Block placement is not implemented");
					}

			EditorUtility.SetDirty(place);
		}

		private static Vector3Int FindBlockFIFO(Vector3Int from, Vector3Int size, System.Func<Vector3Int, bool> checkSame)
		{
			bool[] axisAvailable = new bool[3] { true, true, true };

			int axisIndex = 1;
			var to = from;
			Vector3Int checkedTo = from;
			Vector3Int mid = Vector3Int.zero;
			while (axisAvailable.Any(b => b))
			{

				do
				{ axisIndex = (axisIndex + 1) % axisAvailable.Length; }
				while (!axisAvailable[axisIndex]);

				++to[axisIndex];

				bool isOk = to.x < size.x && to.y < size.y && to.z < size.z;

				for (mid.x = from.x; mid.x <= to.x && isOk; ++mid.x)
					for (mid.y = from.y; mid.y <= to.y && isOk; ++mid.y)
						for (mid.z = from.z; mid.z <= to.z && isOk; ++mid.z)
							isOk = checkSame(mid);

				if (isOk)
					checkedTo = to;
				else
				{
					axisAvailable[axisIndex] = false;
					to = checkedTo;
				}
			}

			return checkedTo;
		}

		private Color GetColor(Vector3Int cubeCoord, Vector3Int size)
			=> slices.GetPixel(cubeCoord.x, cubeCoord.z + cubeCoord.y * size.z);

		private static bool Same(Color l, Color r)
			=> Mathf.Abs(l.r - r.r) + Mathf.Abs(l.g - r.g) + Mathf.Abs(l.b - r.b) < 0.02f;

		private void UpdateForgottenColors()
		{
			List<Color> usedColors = new List<Color>();

			foreach(var element in elements)
				usedColors.AddRange(element.colors);

			forgottenColors
				= slices.GetPixels()
				.Where(x => x.a > 0)
				.Distinct()
				.Where(ac => !usedColors.Any(uc => Same(ac, uc)))
				.ToArray();
		}
#endif
	}
}