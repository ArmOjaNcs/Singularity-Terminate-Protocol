using System.IO;
using UnityEditor;
using UnityEngine;

public class SpriteExporter
{
    [MenuItem("Assets/Export Selected Sprite to PNG", false, 10)]
    public static void SaveSpriteToPNG()
    {
        Object selectedObj = Selection.activeObject;
        
        if (selectedObj is Sprite sprite)
        {
            Texture2D texture = sprite.texture;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            bool wasReadable = ti.isReadable;
        
            if (!wasReadable)
            {
                ti.isReadable = true;
                ti.SaveAndReimport();
            }

            Rect fullRect = sprite.rect;
            int width = Mathf.RoundToInt(fullRect.width);
            int height = Mathf.RoundToInt(fullRect.height);

            Texture2D newTex = new Texture2D(width, height);
            Color[] clearPixels = new Color[width * height];

            newTex.SetPixels(clearPixels);

            int sourceX = Mathf.RoundToInt(fullRect.x);
            int sourceY = Mathf.RoundToInt(fullRect.y);

            Color[] pixels = texture.GetPixels(sourceX, sourceY, width, height);

            newTex.SetPixels(pixels);
            newTex.Apply();

            byte[] bytes = newTex.EncodeToPNG();
            string path = EditorUtility.SaveFilePanel("Сохранить спрайт", "", sprite.name + ".png", "png");

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllBytes(path, bytes);
                AssetDatabase.Refresh();
                Debug.Log($"[Успех] Спрайт сохранен: {path}");
            }
                        
            if (!wasReadable)
            {
                ti.isReadable = false;
                ti.SaveAndReimport();
            }
        }
        else
        {
            Debug.LogError("Выберите НАРЕЗАННЫЙ кадр внутри Sprite Sheet (раскройте его стрелочкой в окне Project)!");
        }
    }
}