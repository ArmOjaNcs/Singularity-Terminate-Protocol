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

            Rect rect = sprite.textureRect;
            Texture2D newTex = new Texture2D((int)rect.width, (int)rect.height);
            Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

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