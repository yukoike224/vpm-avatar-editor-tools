using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Avatars.Components;

namespace YuKoike.Tools
{
    public class FTAvatarParameterOptimizer : EditorWindow
    {
        private static readonly string[] LIGHT_LIMIT_CHANGER_PARAMS = new[]
        {
            "_LLC_Saturation",          // 1. 彩度調整
            "_LLC_EmissionIntensity",    // 2. エミッション調整
            "_LLC_ColorTempControl",     // 3. 色温度調整
            "_LLC_UnlitIntensity",       // 4. Unlit調整
            "_LLC_MonochromeBlend"       // 5. ライトのモノクロ化調整
        };

        private List<GameObject> selectedAvatars = new List<GameObject>();
        private Vector2 scrollPos;
        private bool processingComplete = false;
        private string statusMessage = "";

        [MenuItem("Tools/Koike's Utils/FT Avatar Parameter Optimizer")]
        private static void ShowWindow()
        {
            var window = GetWindow<FTAvatarParameterOptimizer>();
            window.titleContent = new GUIContent("FT Avatar Parameter Optimizer");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("FT Avatar Parameter Optimizer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "選択したFT適用済みアバターに対して:\n" +
                "1. FaceEmoPrefabを無効化\n" +
                "2. 256bitを超える場合、LightLimitChangerパラメータを段階的に削減",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("ヒエラルキーで選択中のアバターを取得", GUILayout.Height(30)))
            {
                GetSelectedAvatars();
            }

            EditorGUILayout.Space();

            if (selectedAvatars.Count > 0)
            {
                EditorGUILayout.LabelField($"選択されたアバター: {selectedAvatars.Count}個");

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(150));
                foreach (var avatar in selectedAvatars)
                {
                    if (avatar != null)
                    {
                        EditorGUILayout.LabelField($"  • {avatar.name}");
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("最適化を実行", GUILayout.Height(40)))
                {
                    ProcessSelectedAvatars();
                }
                GUI.backgroundColor = Color.white;
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space();
                var messageType = processingComplete ? MessageType.Info : MessageType.Warning;
                EditorGUILayout.HelpBox(statusMessage, messageType);
            }
        }

        private void GetSelectedAvatars()
        {
            selectedAvatars.Clear();

            foreach (var obj in Selection.gameObjects)
            {
                if (obj != null && obj.GetComponent<VRCAvatarDescriptor>() != null)
                {
                    selectedAvatars.Add(obj);
                }
            }

            if (selectedAvatars.Count == 0)
            {
                statusMessage = "VRCAvatarDescriptorを持つオブジェクトを選択してください";
            }
            else
            {
                statusMessage = "";
            }
        }

        private void ProcessSelectedAvatars()
        {
            int processedCount = 0;
            var results = new List<string>();

            foreach (var avatar in selectedAvatars)
            {
                if (avatar == null) continue;

                var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
                if (descriptor == null) continue;

                results.Add($"\n[{avatar.name}]");

                // 1. FaceEmoPrefabを無効化
                DisableFaceEmoPrefab(avatar, results);

                // 2. パラメータコストを計算
                int paramCost = CalculateParameterCost(descriptor);
                results.Add($"  現在のパラメータコスト: {paramCost}/256 bits");

                // 3. 256bitを超えている場合、LightLimitChangerパラメータを段階的に削減
                if (paramCost > 256)
                {
                    OptimizeLightLimitChangerParams(descriptor, ref paramCost, results);
                }

                processedCount++;
            }

            statusMessage = $"処理完了: {processedCount}個のアバターを最適化しました\n" + string.Join("\n", results);
            processingComplete = true;

            // シーンに変更をマーク
            EditorUtility.SetDirty(this);
            foreach (var avatar in selectedAvatars)
            {
                if (avatar != null)
                {
                    EditorUtility.SetDirty(avatar);
                }
            }
        }

        private void DisableFaceEmoPrefab(GameObject avatar, List<string> results)
        {
            var faceEmoPrefabs = avatar.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "FaceEmoPrefab");

            int disabledCount = 0;
            foreach (var prefab in faceEmoPrefabs)
            {
                // GameObject を無効化
                prefab.gameObject.SetActive(false);

                // EditorOnlyタグを適用
                prefab.gameObject.tag = "EditorOnly";

                disabledCount++;

                // Prefabへの変更をマーク
                EditorUtility.SetDirty(prefab.gameObject);
            }

            if (disabledCount > 0)
            {
                results.Add($"  FaceEmoPrefab {disabledCount}個を無効化");
            }
        }

        private int CalculateParameterCost(VRCAvatarDescriptor descriptor)
        {
            if (descriptor.expressionParameters == null) return 0;

            int totalCost = 0;
            foreach (var param in descriptor.expressionParameters.parameters)
            {
                if (string.IsNullOrEmpty(param.name)) continue;

                switch (param.valueType)
                {
                    case VRCExpressionParameters.ValueType.Int:
                        totalCost += 8;
                        break;
                    case VRCExpressionParameters.ValueType.Float:
                        totalCost += 8;
                        break;
                    case VRCExpressionParameters.ValueType.Bool:
                        totalCost += 1;
                        break;
                }
            }

            return totalCost;
        }

        private void OptimizeLightLimitChangerParams(VRCAvatarDescriptor descriptor, ref int paramCost, List<string> results)
        {
            if (descriptor.expressionParameters == null) return;

            var parameters = descriptor.expressionParameters.parameters.ToList();
            var removedParams = new List<string>();

            // LightLimitChangerパラメータを段階的に削除
            foreach (var llcParam in LIGHT_LIMIT_CHANGER_PARAMS)
            {
                if (paramCost <= 256) break;

                for (int i = parameters.Count - 1; i >= 0; i--)
                {
                    if (parameters[i].name == llcParam)
                    {
                        // パラメータのコストを減算
                        switch (parameters[i].valueType)
                        {
                            case VRCExpressionParameters.ValueType.Int:
                            case VRCExpressionParameters.ValueType.Float:
                                paramCost -= 8;
                                break;
                            case VRCExpressionParameters.ValueType.Bool:
                                paramCost -= 1;
                                break;
                        }

                        removedParams.Add(parameters[i].name);
                        parameters.RemoveAt(i);

                        // Expression Menuからも削除が必要な場合はここで処理
                        RemoveFromExpressionMenu(descriptor, llcParam);

                        break;
                    }
                }
            }

            // 更新されたパラメータリストを反映
            descriptor.expressionParameters.parameters = parameters.ToArray();
            EditorUtility.SetDirty(descriptor.expressionParameters);

            if (removedParams.Count > 0)
            {
                results.Add($"  削除したパラメータ: {string.Join(", ", removedParams)}");
                results.Add($"  最終パラメータコスト: {paramCost}/256 bits");
            }
        }

        private void RemoveFromExpressionMenu(VRCAvatarDescriptor descriptor, string paramName)
        {
            if (descriptor.expressionsMenu == null) return;

            // メインメニューとサブメニューから該当パラメータを削除
            RemoveControlFromMenu(descriptor.expressionsMenu, paramName);
            EditorUtility.SetDirty(descriptor.expressionsMenu);
        }

        private void RemoveControlFromMenu(VRCExpressionsMenu menu, string paramName)
        {
            if (menu == null || menu.controls == null) return;

            for (int i = menu.controls.Count - 1; i >= 0; i--)
            {
                var control = menu.controls[i];

                // パラメータ名が一致するコントロールを削除
                if (control.parameter != null && control.parameter.name == paramName)
                {
                    menu.controls.RemoveAt(i);
                }
                // サブメニューがある場合は再帰的に処理
                else if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null)
                {
                    RemoveControlFromMenu(control.subMenu, paramName);
                }
            }
        }
    }
}