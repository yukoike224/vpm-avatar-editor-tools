using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace YuKoike.Tools
{
    /// <summary>
    /// Koikeエディタ拡張ツール群で使用する共通ユーティリティクラス
    /// </summary>
    public static class KoikeEditorUtility
    {
        #region GameObject検索関連

        /// <summary>
        /// ルートオブジェクトから指定した名前の子オブジェクトを検索
        /// </summary>
        /// <param name="root">検索開始するルートオブジェクト</param>
        /// <param name="name">検索する名前</param>
        /// <param name="exactMatch">完全一致かどうか（false時は部分一致）</param>
        /// <returns>見つかったGameObject、見つからない場合はnull</returns>
        public static GameObject FindChildByName(GameObject root, string name, bool exactMatch = true)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            if (exactMatch)
            {
                return FindChildByExactNameRecursive(root.transform, name);
            }
            else
            {
                return FindChildContainingNameRecursive(root.transform, name.ToLower());
            }
        }

        /// <summary>
        /// GameObjectの階層構造を再帰的に収集し、辞書として格納
        /// </summary>
        /// <param name="obj">収集開始するルートオブジェクト</param>
        /// <param name="result">結果を格納する辞書</param>
        /// <param name="currentPath">現在のパス</param>
        /// <param name="isRoot">ルートオブジェクトかどうか</param>
        /// <param name="allowRootNameMismatch">ルート名不一致を許容するか</param>
        public static void CollectGameObjectsRecursively(GameObject obj, Dictionary<string, GameObject> result, 
            string currentPath, bool isRoot = false, bool allowRootNameMismatch = false)
        {
            if (obj == null || result == null)
                return;

            string objectPath;

            if (isRoot && allowRootNameMismatch)
            {
                objectPath = "";
            }
            else
            {
                objectPath = string.IsNullOrEmpty(currentPath) ? obj.name : currentPath + "/" + obj.name;
            }

            result[objectPath] = obj;

            foreach (Transform child in obj.transform)
            {
                CollectGameObjectsRecursively(child.gameObject, result, objectPath, false, allowRootNameMismatch);
            }
        }

        private static GameObject FindChildByExactNameRecursive(Transform parent, string exactName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == exactName)
                {
                    return child.gameObject;
                }

                GameObject found = FindChildByExactNameRecursive(child, exactName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindChildContainingNameRecursive(Transform parent, string searchName)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(searchName))
                {
                    return child.gameObject;
                }

                GameObject found = FindChildContainingNameRecursive(child, searchName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        #endregion

        #region ModularAvatar関連

        /// <summary>
        /// オブジェクトからModularAvatarScaleAdjusterコンポーネントを取得
        /// </summary>
        /// <param name="target">対象オブジェクト</param>
        /// <returns>見つかったコンポーネント、見つからない場合はnull</returns>
        public static Component GetModularAvatarScaleAdjuster(GameObject target)
        {
            if (target == null)
                return null;

            // ModularAvatarScaleAdjusterコンポーネントを検索
            Component scaleAdjuster = target.GetComponent("ModularAvatarScaleAdjuster");
            if (scaleAdjuster == null)
            {
                // 名前空間付きでも検索
                scaleAdjuster = target.GetComponent("nadena.dev.modular_avatar.core.ModularAvatarScaleAdjuster");
            }

            return scaleAdjuster;
        }

        /// <summary>
        /// ModularAvatarScaleAdjusterをソースからターゲットにコピー
        /// </summary>
        /// <param name="source">コピー元オブジェクト</param>
        /// <param name="target">コピー先オブジェクト</param>
        /// <param name="overwrite">既存コンポーネントを上書きするか</param>
        /// <returns>コピーが実行されたかどうか</returns>
        public static bool CopyModularAvatarScaleAdjuster(GameObject source, GameObject target, bool overwrite = true)
        {
            if (source == null || target == null)
                return false;

            Component sourceScaleAdjuster = GetModularAvatarScaleAdjuster(source);
            if (sourceScaleAdjuster == null)
                return false;

            Type scaleAdjusterType = sourceScaleAdjuster.GetType();
            Component targetScaleAdjuster = target.GetComponent(scaleAdjusterType);

            if (targetScaleAdjuster != null)
            {
                if (overwrite)
                {
                    Undo.RecordObject(targetScaleAdjuster, "Overwrite MA Scale Adjuster");
                    EditorUtility.CopySerialized(sourceScaleAdjuster, targetScaleAdjuster);
                    return true;
                }
                return false;
            }
            else
            {
                Undo.AddComponent(target, scaleAdjusterType);
                targetScaleAdjuster = target.GetComponent(scaleAdjusterType);
                EditorUtility.CopySerialized(sourceScaleAdjuster, targetScaleAdjuster);
                return true;
            }
        }

        #endregion

        #region プログレスバー関連

        /// <summary>
        /// プログレスバーを表示
        /// </summary>
        /// <param name="title">タイトル</param>
        /// <param name="info">詳細情報</param>
        /// <param name="progress">進行状況（0.0-1.0）</param>
        public static void ShowProgressBar(string title, string info, float progress)
        {
            EditorUtility.DisplayProgressBar(title, info, Mathf.Clamp01(progress));
        }

        /// <summary>
        /// プログレスバーをクリア
        /// </summary>
        public static void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }

        #endregion

        #region プレハブ処理関連

        /// <summary>
        /// プレハブを安全に処理する
        /// </summary>
        /// <param name="prefab">処理対象のプレハブ</param>
        /// <param name="processAction">実行する処理</param>
        /// <returns>処理が成功したかどうか</returns>
        public static bool SafeProcessPrefab(GameObject prefab, Action<GameObject> processAction)
        {
            if (prefab == null || processAction == null)
                return false;

            GameObject instance = null;
            bool wasModified = false;

            try
            {
                // プレハブかどうかチェック
                if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
                {
                    Debug.LogWarning($"{prefab.name} is not a prefab. Processing as scene object.");
                    processAction(prefab);
                    return true;
                }

                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance == null)
                {
                    Debug.LogError($"Failed to instantiate prefab: {prefab.name}");
                    return false;
                }

                processAction(instance);
                wasModified = true;

                if (wasModified)
                {
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.UserAction);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing prefab {prefab.name}: {e.Message}");
                return false;
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        #endregion

        #region ファイル・アセット関連

        /// <summary>
        /// アセットのフルパスを取得
        /// </summary>
        /// <param name="asset">対象アセット</param>
        /// <returns>フルパス、取得できない場合は空文字</returns>
        public static string GetAssetFullPath(UnityEngine.Object asset)
        {
            if (asset == null)
                return string.Empty;

            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            // Assets/... を絶対パスに変換
            string fullPath = System.IO.Path.GetFullPath(assetPath);

            // ディレクトリの場合は末尾にスラッシュを追加
            if (System.IO.Directory.Exists(fullPath))
            {
                return fullPath + "/";
            }
            else
            {
                return fullPath;
            }
        }

        #endregion

        #region 結果表示関連

        /// <summary>
        /// 処理結果をダイアログで表示
        /// </summary>
        /// <param name="success">成功したかどうか</param>
        /// <param name="title">タイトル</param>
        /// <param name="message">メッセージ</param>
        /// <param name="processedCount">処理数</param>
        /// <param name="successCount">成功数</param>
        public static void ShowResultDialog(bool success, string title, string message, int processedCount = 0, int successCount = 0)
        {
            string fullTitle = success ? $"✓ {title}" : $"⚠ {title}";
            string fullMessage = message;

            if (processedCount > 0)
            {
                fullMessage += $"\n\n処理数: {successCount}/{processedCount}";
            }

            EditorUtility.DisplayDialog(fullTitle, fullMessage, "OK");
        }

        #endregion
    }
}