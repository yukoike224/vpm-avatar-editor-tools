using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using VRC.SDK3.Avatars.ScriptableObjects;

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
                // AnimatorControllerの場合
                else if (selectedAsset is AnimatorController)
                {
                    string newPath = CreateAnimatorControllerVariant(selectedAsset as AnimatorController, assetPath);
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
                // AnimationClipの場合
                else if (selectedAsset is AnimationClip)
                {
                    string newPath = CreateAnimationClipVariant(selectedAsset as AnimationClip, assetPath);
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
                // VRCExpressionsMenuの場合
                else if (selectedAsset is VRCExpressionsMenu)
                {
                    string newPath = CreateVRCExpressionsAssetVariant(selectedAsset, assetPath, "Expressions");
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
                // VRCExpressionParametersの場合
                else if (selectedAsset is VRCExpressionParameters)
                {
                    string newPath = CreateVRCExpressionsAssetVariant(selectedAsset, assetPath, "Expressions");
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
                // Texture2Dの場合
                else if (selectedAsset is Texture2D)
                {
                    string newPath = CreateTextureVariant(selectedAsset as Texture2D, assetPath);
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

        private static string CreateAnimatorControllerVariant(AnimatorController originalController, string assetPath)
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
                    outputPath = Path.Combine(outputBasePath, vendorName, assetName, "Controller");
                }
                else if (!string.IsNullOrEmpty(assetName))
                {
                    outputPath = Path.Combine(outputBasePath, assetName, "Controller");
                }
                else
                {
                    outputPath = Path.Combine(outputBasePath, "Controller");
                }

                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // 新しいファイル名を生成
                string originalName = Path.GetFileNameWithoutExtension(assetPath);
                string newFileName = $"{originalName}_Kaihen.controller";
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

                    // 既存のアセットを削除
                    AssetDatabase.DeleteAsset(newAssetPath);
                }

                // AnimatorControllerのコピーを作成
                // AssetDatabase.CopyAssetを使用してディープコピー
                if (!AssetDatabase.CopyAsset(assetPath, newAssetPath))
                {
                    Debug.LogError($"AnimatorControllerのコピーに失敗しました: {assetPath}");
                    return null;
                }

                // コピーしたコントローラーを読み込み
                AnimatorController copiedController = AssetDatabase.LoadAssetAtPath<AnimatorController>(newAssetPath);
                if (copiedController == null)
                {
                    Debug.LogError($"コピーしたAnimatorControllerの読み込みに失敗しました: {newAssetPath}");
                    return null;
                }

                Debug.Log($"AnimatorControllerバリアントを作成しました: {newAssetPath}");
                return newAssetPath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AnimatorControllerバリアントの作成中にエラーが発生しました: {e.Message}");
                return null;
            }
        }

        private static string CreateAnimationClipVariant(AnimationClip originalClip, string assetPath)
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
                    outputPath = Path.Combine(outputBasePath, vendorName, assetName, "Animation");
                }
                else if (!string.IsNullOrEmpty(assetName))
                {
                    outputPath = Path.Combine(outputBasePath, assetName, "Animation");
                }
                else
                {
                    outputPath = Path.Combine(outputBasePath, "Animation");
                }

                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // 新しいファイル名を生成
                string originalName = Path.GetFileNameWithoutExtension(assetPath);
                string newFileName = $"{originalName}_Kaihen.anim";
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

                    // 既存のアセットを削除
                    AssetDatabase.DeleteAsset(newAssetPath);
                }

                // AnimationClipのコピーを作成
                // AssetDatabase.CopyAssetを使用してディープコピー
                if (!AssetDatabase.CopyAsset(assetPath, newAssetPath))
                {
                    Debug.LogError($"AnimationClipのコピーに失敗しました: {assetPath}");
                    return null;
                }

                // コピーしたクリップを読み込み
                AnimationClip copiedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newAssetPath);
                if (copiedClip == null)
                {
                    Debug.LogError($"コピーしたAnimationClipの読み込みに失敗しました: {newAssetPath}");
                    return null;
                }

                Debug.Log($"AnimationClipバリアントを作成しました: {newAssetPath}");
                return newAssetPath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AnimationClipバリアントの作成中にエラーが発生しました: {e.Message}");
                return null;
            }
        }

        private static string CreateVRCExpressionsAssetVariant(Object originalAsset, string assetPath, string folderName)
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
                    outputPath = Path.Combine(outputBasePath, vendorName, assetName, folderName);
                }
                else if (!string.IsNullOrEmpty(assetName))
                {
                    outputPath = Path.Combine(outputBasePath, assetName, folderName);
                }
                else
                {
                    outputPath = Path.Combine(outputBasePath, folderName);
                }

                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // ファイル拡張子を取得
                string extension = Path.GetExtension(assetPath);

                // 新しいファイル名を生成
                string originalName = Path.GetFileNameWithoutExtension(assetPath);
                string newFileName = $"{originalName}_Kaihen{extension}";
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

                    // 既存のアセットを削除
                    AssetDatabase.DeleteAsset(newAssetPath);
                }

                // アセットのコピーを作成
                if (!AssetDatabase.CopyAsset(assetPath, newAssetPath))
                {
                    Debug.LogError($"アセットのコピーに失敗しました: {assetPath}");
                    return null;
                }

                // コピーしたアセットを読み込み
                Object copiedAsset = AssetDatabase.LoadAssetAtPath<Object>(newAssetPath);
                if (copiedAsset == null)
                {
                    Debug.LogError($"コピーしたアセットの読み込みに失敗しました: {newAssetPath}");
                    return null;
                }

                string assetTypeName = originalAsset.GetType().Name;
                Debug.Log($"{assetTypeName}バリアントを作成しました: {newAssetPath}");
                return newAssetPath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"アセットバリアントの作成中にエラーが発生しました: {e.Message}");
                return null;
            }
        }

        private static string CreateTextureVariant(Texture2D originalTexture, string assetPath)
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
                    outputPath = Path.Combine(outputBasePath, vendorName, assetName, "Texture");
                }
                else if (!string.IsNullOrEmpty(assetName))
                {
                    outputPath = Path.Combine(outputBasePath, assetName, "Texture");
                }
                else
                {
                    outputPath = Path.Combine(outputBasePath, "Texture");
                }

                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // ファイル拡張子を取得
                string extension = Path.GetExtension(assetPath);

                // 新しいファイル名を生成
                string originalName = Path.GetFileNameWithoutExtension(assetPath);
                string newFileName = $"{originalName}_Kaihen{extension}";
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

                    // 既存のアセットを削除
                    AssetDatabase.DeleteAsset(newAssetPath);
                }

                // テクスチャファイルをコピー
                if (!AssetDatabase.CopyAsset(assetPath, newAssetPath))
                {
                    Debug.LogError($"テクスチャのコピーに失敗しました: {assetPath}");
                    return null;
                }

                // コピーしたテクスチャを読み込み
                Texture2D copiedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newAssetPath);
                if (copiedTexture == null)
                {
                    Debug.LogError($"コピーしたテクスチャの読み込みに失敗しました: {newAssetPath}");
                    return null;
                }

                Debug.Log($"テクスチャバリアントを作成しました: {newAssetPath}");
                return newAssetPath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"テクスチャバリアントの作成中にエラーが発生しました: {e.Message}");
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

            // サポートされているアセットタイプをチェック
            foreach (Object obj in Selection.objects)
            {
                if (obj is Material ||
                    obj is AnimatorController ||
                    obj is AnimationClip ||
                    obj is VRCExpressionsMenu ||
                    obj is VRCExpressionParameters ||
                    obj is Texture2D)
                    return true;
            }

            return false;
        }
    }
}