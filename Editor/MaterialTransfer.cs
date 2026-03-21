using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace YuKoike.Tools
{
    /// <summary>
    /// カラバリPrefabからマテリアル・色設定を再帰的に転送するエディタ拡張
    /// </summary>
    public class MaterialTransfer : EditorWindow
    {
        private GameObject sourcePrefab;
        private GameObject target;
        private bool allowRootNameMismatch = true;
        private bool showPreview = false;

        // 転送対象の選択
        private bool transferMaterials = true;
        private bool transferParticleStartColor = true;
        private bool transferLightColor = true;

        private Vector2 mainScrollPosition;
        private Vector2 previewScrollPosition;
        private Vector2 resultScrollPosition;

        private List<PreviewEntry> previewEntries = new List<PreviewEntry>();
        private TransferResult lastResult;
        private bool showResults = false;

        /// <summary>
        /// プレビュー表示用のエントリ
        /// </summary>
        private class PreviewEntry
        {
            public string objectPath;
            public string description;
            public bool hasDifference;
            // マテリアル用
            public Material[] sourceMaterials;
            public Material[] targetMaterials;
            // ParticleSystem StartColor用
            public ParticleSystem.MinMaxGradient? sourceStartColor;
            public ParticleSystem.MinMaxGradient? targetStartColor;
            // Light Color用
            public Color? sourceLightColor;
            public Color? targetLightColor;
        }

        /// <summary>
        /// オブジェクトごとの転送可能な情報
        /// </summary>
        private class NodeInfo
        {
            public Material[] materials;
            public string rendererTypeName;
            public ParticleSystem.MinMaxGradient? startColor;
            public Color? lightColor;
        }

        private class TransferResult
        {
            public GameObject target;
            public bool success;
            public int transferredCount;
            public string message;
            public List<string> details = new List<string>();
        }

        [MenuItem("Tools/Koike's Utils/Material Transfer")]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialTransfer>("Material Transfer");
            window.minSize = new Vector2(500, 500);
        }

        private void OnGUI()
        {
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);

            GUILayout.Label("Material Transfer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "カラバリPrefabをテンプレートとして、マテリアル・色設定を再帰的に転送します。\n" +
                "Projectウィンドウからの直接指定、シーン上のオブジェクト指定の両方に対応しています。",
                MessageType.Info);
            EditorGUILayout.Space();

            // ソース設定
            DrawSourceSection();
            EditorGUILayout.Space();

            // ターゲット設定
            DrawTargetSection();
            EditorGUILayout.Space();

            // 転送対象・オプション
            DrawOptionsSection();
            EditorGUILayout.Space();

            // プレビュー・実行ボタン
            DrawActionButtons();

            // プレビュー表示
            if (showPreview && previewEntries.Count > 0)
            {
                DrawPreview();
            }

            // 結果表示
            if (showResults && lastResult != null)
            {
                DrawResults();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceSection()
        {
            GUILayout.Label("ソース（テンプレート）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "マテリアル設定のコピー元となるカラバリPrefabを指定してください。\n" +
                "Projectウィンドウから直接ドラッグ&ドロップ、またはシーン上のオブジェクトを指定できます。",
                MessageType.None);

            sourcePrefab = EditorGUILayout.ObjectField("ソース:", sourcePrefab, typeof(GameObject), true) as GameObject;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Projectの選択アセットをソースに設定"))
            {
                SetSelectedAssetAsSource();
            }

            if (GUILayout.Button("シーンの選択オブジェクトをソースに設定"))
            {
                SetSelectedSceneObjectAsSource();
            }

            EditorGUILayout.EndHorizontal();

            // ソースの情報表示
            if (sourcePrefab != null)
            {
                bool isPrefab = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sourcePrefab));
                string sourceType = isPrefab ? "Prefabアセット" : "シーンオブジェクト";
                EditorGUILayout.LabelField($"  種類: {sourceType}", EditorStyles.miniLabel);
            }
        }

        private void DrawTargetSection()
        {
            GUILayout.Label("ターゲット（転送先）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "マテリアルを適用したいシーン上の衣装オブジェクトを指定してください。\n" +
                "ソースを切り替えることで複数のカラバリを順番に適用できます。",
                MessageType.None);

            target = EditorGUILayout.ObjectField("ターゲット:", target, typeof(GameObject), true) as GameObject;

            if (GUILayout.Button("ヒエラルキーの選択オブジェクトをターゲットに設定"))
            {
                GameObject[] selected = Selection.gameObjects;
                if (selected.Length == 0)
                {
                    EditorUtility.DisplayDialog("警告", "ヒエラルキーでオブジェクトを選択してください。", "OK");
                }
                else if (selected.Length > 1)
                {
                    EditorUtility.DisplayDialog("警告", "ターゲットには1つのオブジェクトのみ選択してください。", "OK");
                }
                else
                {
                    target = selected[0];
                }
            }
        }

        private void DrawOptionsSection()
        {
            GUILayout.Label("転送対象", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            transferMaterials = EditorGUILayout.Toggle("Rendererのマテリアル", transferMaterials);
            transferParticleStartColor = EditorGUILayout.Toggle("ParticleSystem Start Color", transferParticleStartColor);
            transferLightColor = EditorGUILayout.Toggle("Light Color", transferLightColor);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.Label("オプション", EditorStyles.boldLabel);
            allowRootNameMismatch = EditorGUILayout.Toggle("ルート名の不一致を許容", allowRootNameMismatch);

            if (allowRootNameMismatch)
            {
                EditorGUILayout.HelpBox(
                    "ルート名が異なる場合でも、子階層のパスでマッチングを行います。\n" +
                    "カラバリPrefabは通常ルート名が異なるため、有効推奨です。",
                    MessageType.Info);
            }
        }

        private void DrawActionButtons()
        {
            bool canExecute = sourcePrefab != null &&
                              target != null &&
                              (transferMaterials || transferParticleStartColor || transferLightColor);

            GUI.enabled = canExecute;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("プレビュー", GUILayout.Height(30)))
            {
                GeneratePreview();
            }

            if (GUILayout.Button("転送を実行", GUILayout.Height(30)))
            {
                ExecuteTransfer();
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space();
            GUILayout.Label("プレビュー", EditorStyles.boldLabel);

            int diffCount = previewEntries.Count(e => e.hasDifference);
            EditorGUILayout.HelpBox(
                $"マッチしたオブジェクト: {previewEntries.Count}個 / 差分あり: {diffCount}個",
                MessageType.Info);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            previewScrollPosition = EditorGUILayout.BeginScrollView(previewScrollPosition, GUILayout.MaxHeight(300));

            foreach (var entry in previewEntries)
            {
                if (!entry.hasDifference)
                    continue;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(entry.objectPath, EditorStyles.boldLabel);

                // マテリアル差分
                if (entry.sourceMaterials != null && entry.targetMaterials != null)
                {
                    EditorGUILayout.LabelField($"  マテリアル ({entry.description}):");
                    for (int i = 0; i < entry.sourceMaterials.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"    [{i}]", GUILayout.Width(40));

                        Material currentMat = i < entry.targetMaterials.Length ? entry.targetMaterials[i] : null;
                        EditorGUILayout.ObjectField(currentMat, typeof(Material), false, GUILayout.Width(170));
                        GUILayout.Label("→", GUILayout.Width(20));
                        EditorGUILayout.ObjectField(entry.sourceMaterials[i], typeof(Material), false, GUILayout.Width(170));

                        EditorGUILayout.EndHorizontal();
                    }
                }

                // ParticleSystem StartColor差分
                if (entry.sourceStartColor.HasValue && entry.targetStartColor.HasValue)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("  Particle Start Color:", GUILayout.Width(160));
                    DrawColorPreview(GetGradientDisplayColor(entry.targetStartColor.Value));
                    GUILayout.Label("→", GUILayout.Width(20));
                    DrawColorPreview(GetGradientDisplayColor(entry.sourceStartColor.Value));
                    EditorGUILayout.EndHorizontal();
                }

                // Light Color差分
                if (entry.sourceLightColor.HasValue && entry.targetLightColor.HasValue)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("  Light Color:", GUILayout.Width(160));
                    EditorGUILayout.ColorField(GUIContent.none, entry.targetLightColor.Value, false, true, false, GUILayout.Width(80));
                    GUILayout.Label("→", GUILayout.Width(20));
                    EditorGUILayout.ColorField(GUIContent.none, entry.sourceLightColor.Value, false, true, false, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawResults()
        {
            EditorGUILayout.Space();
            GUILayout.Label("処理結果", EditorStyles.boldLabel);

            string icon = lastResult.success ? "✓" : "✗";
            EditorGUILayout.HelpBox(
                $"{icon} {lastResult.message}",
                lastResult.success ? MessageType.Info : MessageType.Warning);

            if (lastResult.details.Count > 0)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                resultScrollPosition = EditorGUILayout.BeginScrollView(resultScrollPosition, GUILayout.MaxHeight(200));

                foreach (var detail in lastResult.details)
                {
                    EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// MinMaxGradientの代表色を取得
        /// </summary>
        private static Color GetGradientDisplayColor(ParticleSystem.MinMaxGradient gradient)
        {
            switch (gradient.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return gradient.color;
                case ParticleSystemGradientMode.TwoColors:
                    return Color.Lerp(gradient.colorMin, gradient.colorMax, 0.5f);
                case ParticleSystemGradientMode.Gradient:
                case ParticleSystemGradientMode.TwoGradients:
                case ParticleSystemGradientMode.RandomColor:
                    return gradient.gradient != null ? gradient.gradient.Evaluate(0f) : Color.white;
                default:
                    return Color.white;
            }
        }

        private static void DrawColorPreview(Color color)
        {
            EditorGUILayout.ColorField(GUIContent.none, color, false, true, false, GUILayout.Width(80));
        }

        #region ソース・ターゲット設定

        private void SetSelectedAssetAsSource()
        {
            Object[] selected = Selection.GetFiltered(typeof(GameObject), SelectionMode.Assets);

            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("警告", "ProjectウィンドウでPrefabを選択してください。", "OK");
                return;
            }

            if (selected.Length > 1)
            {
                EditorUtility.DisplayDialog("警告", "ソースには1つのPrefabのみ選択してください。", "OK");
                return;
            }

            sourcePrefab = selected[0] as GameObject;
        }

        private void SetSelectedSceneObjectAsSource()
        {
            GameObject[] selected = Selection.gameObjects;

            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("警告", "シーンでオブジェクトを選択してください。", "OK");
                return;
            }

            if (selected.Length > 1)
            {
                EditorUtility.DisplayDialog("警告", "ソースには1つのオブジェクトのみ選択してください。", "OK");
                return;
            }

            sourcePrefab = selected[0];
        }

        #endregion

        #region プレビュー・転送処理

        private void GeneratePreview()
        {
            previewEntries.Clear();
            showPreview = true;

            if (target == null)
                return;

            var sourceNodes = CollectNodeInfo(sourcePrefab);
            var targetNodes = CollectNodeInfo(target);

            foreach (var sourcePair in sourceNodes)
            {
                if (!targetNodes.TryGetValue(sourcePair.Key, out NodeInfo targetNode))
                    continue;

                var sourceNode = sourcePair.Value;
                var entry = new PreviewEntry
                {
                    objectPath = sourcePair.Key,
                    hasDifference = false
                };

                // マテリアル
                if (transferMaterials && sourceNode.materials != null && targetNode.materials != null)
                {
                    if (!MaterialArraysEqual(sourceNode.materials, targetNode.materials))
                    {
                        entry.sourceMaterials = sourceNode.materials;
                        entry.targetMaterials = targetNode.materials;
                        entry.description = sourceNode.rendererTypeName;
                        entry.hasDifference = true;
                    }
                }

                // ParticleSystem StartColor
                if (transferParticleStartColor && sourceNode.startColor.HasValue && targetNode.startColor.HasValue)
                {
                    if (!MinMaxGradientEqual(sourceNode.startColor.Value, targetNode.startColor.Value))
                    {
                        entry.sourceStartColor = sourceNode.startColor;
                        entry.targetStartColor = targetNode.startColor;
                        entry.hasDifference = true;
                    }
                }

                // Light Color
                if (transferLightColor && sourceNode.lightColor.HasValue && targetNode.lightColor.HasValue)
                {
                    if (sourceNode.lightColor.Value != targetNode.lightColor.Value)
                    {
                        entry.sourceLightColor = sourceNode.lightColor;
                        entry.targetLightColor = targetNode.lightColor;
                        entry.hasDifference = true;
                    }
                }

                previewEntries.Add(entry);
            }
        }

        private void ExecuteTransfer()
        {
            showResults = true;
            showPreview = false;

            var sourceNodes = CollectNodeInfo(sourcePrefab);
            lastResult = ProcessTarget(target, sourceNodes);

            string icon = lastResult.success ? "✓" : "⚠";
            EditorUtility.DisplayDialog($"{icon} 転送完了", lastResult.message, "OK");
        }

        private TransferResult ProcessTarget(GameObject target, Dictionary<string, NodeInfo> sourceNodes)
        {
            TransferResult result = new TransferResult { target = target };

            try
            {
                int transferredCount = 0;

                // ターゲットのコンポーネントをパスごとに収集
                var targetMap = new Dictionary<string, GameObject>();
                CollectGameObjectsByPath(target, targetMap, "", true);

                foreach (var sourcePair in sourceNodes)
                {
                    if (!targetMap.TryGetValue(sourcePair.Key, out GameObject targetObj))
                        continue;

                    var sourceNode = sourcePair.Value;

                    // マテリアル転送
                    if (transferMaterials && sourceNode.materials != null)
                    {
                        Renderer targetRenderer = targetObj.GetComponent<Renderer>();
                        if (targetRenderer != null && !MaterialArraysEqual(sourceNode.materials, targetRenderer.sharedMaterials))
                        {
                            Undo.RecordObject(targetRenderer, "Transfer Materials");
                            targetRenderer.sharedMaterials = sourceNode.materials;
                            transferredCount++;
                            result.details.Add($"  {sourcePair.Key}: マテリアル転送");
                        }
                    }

                    // ParticleSystem StartColor転送
                    if (transferParticleStartColor && sourceNode.startColor.HasValue)
                    {
                        ParticleSystem targetPS = targetObj.GetComponent<ParticleSystem>();
                        if (targetPS != null)
                        {
                            var targetMain = targetPS.main;
                            if (!MinMaxGradientEqual(sourceNode.startColor.Value, targetMain.startColor))
                            {
                                Undo.RecordObject(targetPS, "Transfer Particle Start Color");
                                targetMain.startColor = sourceNode.startColor.Value;
                                transferredCount++;
                                result.details.Add($"  {sourcePair.Key}: Particle Start Color転送");
                            }
                        }
                    }

                    // Light Color転送
                    if (transferLightColor && sourceNode.lightColor.HasValue)
                    {
                        Light targetLight = targetObj.GetComponent<Light>();
                        if (targetLight != null && targetLight.color != sourceNode.lightColor.Value)
                        {
                            Undo.RecordObject(targetLight, "Transfer Light Color");
                            targetLight.color = sourceNode.lightColor.Value;
                            transferredCount++;
                            result.details.Add($"  {sourcePair.Key}: Light Color転送");
                        }
                    }
                }

                result.success = true;
                result.transferredCount = transferredCount;
                result.message = $"転送: {transferredCount}件";
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"エラー: {e.Message}";
                Debug.LogError($"Error processing {target.name}: {e}");
            }

            return result;
        }

        #endregion

        #region ノード情報収集

        /// <summary>
        /// GameObjectの階層を再帰的に走査し、パスをキーにしてノード情報を収集
        /// </summary>
        private Dictionary<string, NodeInfo> CollectNodeInfo(GameObject root)
        {
            var result = new Dictionary<string, NodeInfo>();
            CollectNodeInfoRecursive(root, result, "", true);
            return result;
        }

        private void CollectNodeInfoRecursive(GameObject obj, Dictionary<string, NodeInfo> result, string currentPath, bool isRoot)
        {
            if (obj == null)
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

            var info = new NodeInfo();
            bool hasAny = false;

            // Renderer
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                info.materials = renderer.sharedMaterials;
                info.rendererTypeName = renderer.GetType().Name;
                hasAny = true;
            }

            // ParticleSystem
            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                info.startColor = ps.main.startColor;
                hasAny = true;
            }

            // Light
            Light light = obj.GetComponent<Light>();
            if (light != null)
            {
                info.lightColor = light.color;
                hasAny = true;
            }

            if (hasAny)
            {
                result[objectPath] = info;
            }

            foreach (Transform child in obj.transform)
            {
                CollectNodeInfoRecursive(child.gameObject, result, objectPath, false);
            }
        }

        /// <summary>
        /// GameObjectの階層をパスをキーにして収集（転送時のUndo用）
        /// </summary>
        private void CollectGameObjectsByPath(GameObject obj, Dictionary<string, GameObject> result, string currentPath, bool isRoot)
        {
            if (obj == null)
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
                CollectGameObjectsByPath(child.gameObject, result, objectPath, false);
            }
        }

        #endregion

        #region ユーティリティ

        private static bool MaterialArraysEqual(Material[] a, Material[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static bool MinMaxGradientEqual(ParticleSystem.MinMaxGradient a, ParticleSystem.MinMaxGradient b)
        {
            if (a.mode != b.mode) return false;

            switch (a.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return a.color == b.color;
                case ParticleSystemGradientMode.TwoColors:
                    return a.colorMin == b.colorMin && a.colorMax == b.colorMax;
                default:
                    // Gradient系はオブジェクト参照で比較（完全な比較は困難）
                    return false;
            }
        }


        #endregion
    }
}
