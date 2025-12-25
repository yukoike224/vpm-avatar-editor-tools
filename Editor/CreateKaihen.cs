using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace YuKoike.Tools
{
    public static class CreateKaihen
    {
        [MenuItem("Assets/Create/Kaihen", false, 0)]
        private static void CreateKaihenVariant()
        {
            // 選択されたアセットを取得
            Object[] selectedAssets = Selection.objects;
            
            if (selectedAssets.Length == 0)
            {
                EditorUtility.DisplayDialog("エラー", "アセットが選択されていません。", "OK");
                return;
            }

            int successCount = 0;
            int errorCount = 0;
            List<string> createdPaths = new List<string>();

            foreach (Object selectedAsset in selectedAssets)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedAsset);
                
                if (string.IsNullOrEmpty(assetPath))
                {
                    errorCount++;
                    continue;
                }

                // マテリアルの場合
                if (selectedAsset is Material)
                {
                    string newPath = CreateMaterialVariant(selectedAsset as Material, assetPath);
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        createdPaths.Add(newPath);
                        successCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                // 将来的に他のアセットタイプにも対応予定
                else
                {
                    Debug.LogWarning($"未対応のアセットタイプです: {selectedAsset.GetType().Name}");
                    errorCount++;
                }
            }

            // 結果を表示
            if (successCount > 0)
            {
                AssetDatabase.Refresh();
                
                // 作成されたアセットを選択
                Object[] newAssets = createdPaths.Select(path => AssetDatabase.LoadAssetAtPath<Object>(path)).ToArray();
                Selection.objects = newAssets;
                
                EditorUtility.DisplayDialog("完了", 
                    $"Kaihenバリアントを作成しました。\n成功: {successCount}個\nエラー: {errorCount}個", 
                    "OK");
            }
            else if (errorCount > 0)
            {
                EditorUtility.DisplayDialog("エラー", "Kaihenバリアントの作成に失敗しました。", "OK");
            }
        }

        private static string CreateMaterialVariant(Material originalMaterial, string assetPath)
        {
            try
            {
                // ベンダーとアセット名を取得
                string vendorName = null;
                string assetName = null;
                
                // パスを解析してBOOTH構造を認識
                string[] pathParts = assetPath.Split('/');
                int boothIndex = -1;
                
                for (int i = 0; i < pathParts.Length; i++)
                {
                    if (pathParts[i] == "BOOTH")
                    {
                        boothIndex = i;
                        break;
                    }
                }

                if (boothIndex >= 0 && boothIndex + 2 < pathParts.Length)
                {
                    vendorName = pathParts[boothIndex + 1];
                    assetName = pathParts[boothIndex + 2];
                }
                else
                {
                    // BOOTH構造でない場合は、親フォルダ名を使用
                    DirectoryInfo parentDir = Directory.GetParent(assetPath);
                    if (parentDir != null)
                    {
                        assetName = parentDir.Name;
                    }
                }

                // 出力パスを構築
                string outputBasePath = "Assets/_MyWork/Kaihen";
                string outputPath;

                if (!string.IsNullOrEmpty(vendorName) && !string.IsNullOrEmpty(assetName))
                {
                    outputPath = Path.Combine(outputBasePath, vendorName, assetName, "Material");
                }
                else if (!string.IsNullOrEmpty(assetName))
                {
                    outputPath = Path.Combine(outputBasePath, assetName, "Material");
                }
                else
                {
                    outputPath = Path.Combine(outputBasePath, "Material");
                }

                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // 新しいファイル名を生成
                string originalName = Path.GetFileNameWithoutExtension(assetPath);
                string newFileName = $"{originalName}_Kaihen.mat";
                string newAssetPath = Path.Combine(outputPath, newFileName);

                // 既存ファイルの確認
                if (File.Exists(newAssetPath))
                {
                    if (!EditorUtility.DisplayDialog("確認", 
                        $"'{newFileName}' は既に存在します。上書きしますか？", 
                        "上書き", "キャンセル"))
                    {
                        return null;
                    }
                }

                // Material Variant を作成
                // 元のマテリアルのコピーを作成してから親を設定
                Material materialVariant = new Material(originalMaterial);
                materialVariant.parent = originalMaterial;

                // アセットとして保存
                AssetDatabase.CreateAsset(materialVariant, newAssetPath);

                Debug.Log($"マテリアルバリアントを作成しました: {newAssetPath}");
                return newAssetPath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"マテリアルバリアントの作成中にエラーが発生しました: {e.Message}");
                return null;
            }
        }

        // メニューアイテムの有効/無効を制御
        [MenuItem("Assets/Create/Kaihen", true)]
        private static bool ValidateCreateKaihenVariant()
        {
            // 選択されたオブジェクトがある場合のみ有効
            if (Selection.objects.Length == 0)
                return false;

            // 現在はマテリアルのみサポート
            foreach (Object obj in Selection.objects)
            {
                if (obj is Material)
                    return true;
            }

            return false;
        }
    }
}