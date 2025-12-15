using UnityEditor;
using UnityEngine;
using System.IO;

namespace YuKoike.Tools
{
    public static class CopyAssetFullPath
    {
        [MenuItem("Tools/Koike's Utils/Copy Asset Full Path")]
        private static void CopyAssetPath()
        {
            if (Selection.activeObject == null)
            {
                Debug.LogWarning("アセットが選択されていません。");
                return;
            }

            string fullPath = KoikeEditorUtility.GetAssetFullPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(fullPath))
            {
                EditorGUIUtility.systemCopyBuffer = fullPath;
                Debug.Log($"アセットのフルパスをクリップボードにコピーしました: {fullPath}");
            }
            else
            {
                Debug.LogError("アセットのパスを取得できませんでした。");
            }
        }
    }
}
