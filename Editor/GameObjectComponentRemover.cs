using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace YuKoike.Tools
{
    public class GameObjectComponentRemover : EditorWindow
{
    private GameObject targetRoot;
    private bool removeScaleAdjuster = true;
    // 将来追加するコンポーネント用（例）
    // private bool removeOtherComponent = false;
    private string statusMessage = "";
    private bool isSuccess = false;

    [MenuItem("Tools/Koike's Utils/GameObject Component Remover")]
    public static void ShowWindow()
    {
        GetWindow<GameObjectComponentRemover>("GameObject Component Remover");
    }

    private void OnGUI()
    {
        GUILayout.Label("GameObject Component Remover", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("このツールは指定したゲームオブジェクトとその子階層から、" +
            "選択されたコンポーネントを再帰的に削除します。", MessageType.Info);
        EditorGUILayout.Space();

        targetRoot = EditorGUILayout.ObjectField("ターゲットのルートオブジェクト:", targetRoot, typeof(GameObject), true) as GameObject;

        EditorGUILayout.Space();

        GUILayout.Label("削除するコンポーネントを選択:", EditorStyles.boldLabel);
        removeScaleAdjuster = EditorGUILayout.Toggle("MA Scale Adjuster を削除する", removeScaleAdjuster);

        // 将来追加するコンポーネント用（例）
        // removeOtherComponent = EditorGUILayout.Toggle("Other Component を削除する", removeOtherComponent);

        EditorGUILayout.Space();

        // 削除前の確認メッセージ
        if (targetRoot != null && (removeScaleAdjuster /* || removeOtherComponent */))
        {
            EditorGUILayout.HelpBox("⚠️ 注意: この操作は元に戻すことができます（Ctrl+Z）が、" +
                "削除されたコンポーネントは復元されます。慎重に実行してください。", MessageType.Warning);
        }

        GUI.enabled = (targetRoot != null && (removeScaleAdjuster /* || removeOtherComponent */));
        if (GUILayout.Button("選択されたコンポーネントを削除"))
        {
            if (EditorUtility.DisplayDialog("コンポーネント削除の確認",
                "選択されたコンポーネントを削除しますか？\nこの操作はUndo（Ctrl+Z）で元に戻すことができます。",
                "削除する", "キャンセル"))
            {
                RemoveComponents();
            }
        }
        GUI.enabled = true;

        // 選択されたコンポーネントがない場合の警告
        if (targetRoot != null && !removeScaleAdjuster /* && !removeOtherComponent */)
        {
            EditorGUILayout.HelpBox("削除するコンポーネントを少なくとも1つ選択してください。", MessageType.Warning);
        }

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, isSuccess ? MessageType.Info : MessageType.Error);
        }
    }

    private void RemoveComponents()
    {
        if (targetRoot == null)
        {
            statusMessage = "ターゲットのルートオブジェクトを指定してください。";
            isSuccess = false;
            return;
        }

        if (!removeScaleAdjuster /* && !removeOtherComponent */)
        {
            statusMessage = "削除するコンポーネントを少なくとも1つ選択してください。";
            isSuccess = false;
            return;
        }

        // ターゲットオブジェクトの全階層を収集
        List<GameObject> targetObjects = new List<GameObject>();
        CollectGameObjectsRecursively(targetRoot, targetObjects);

        int removedComponentCount = 0;
        int processedObjectCount = 0;

        // 各オブジェクトに対して処理
        foreach (GameObject targetObj in targetObjects)
        {

            // 選択されたコンポーネントを削除
            int removedFromThisObject = RemoveSelectedComponentsFrom(targetObj);

            if (removedFromThisObject > 0)
            {
                processedObjectCount++;
                removedComponentCount += removedFromThisObject;
            }
        }

        if (removedComponentCount > 0)
        {
            statusMessage = $"処理完了: {processedObjectCount}個のオブジェクトから{removedComponentCount}個のコンポーネントを削除しました。";
            isSuccess = true;
        }
        else
        {
            statusMessage = "削除対象のコンポーネントが見つかりませんでした。";
            isSuccess = false;
        }
    }

    private void CollectGameObjectsRecursively(GameObject obj, List<GameObject> result)
    {
        result.Add(obj);

        foreach (Transform child in obj.transform)
        {
            CollectGameObjectsRecursively(child.gameObject, result);
        }
    }

    private int RemoveSelectedComponentsFrom(GameObject target)
    {
        int removedCount = 0;

        // MA Scale Adjusterの削除
        if (removeScaleAdjuster)
        {
            Component scaleAdjuster = KoikeEditorUtility.GetModularAvatarScaleAdjuster(target);
            if (scaleAdjuster != null)
            {
                Undo.DestroyObjectImmediate(scaleAdjuster);
                removedCount++;
            }
        }

        // 将来追加するコンポーネントの削除処理（例）
        /*
        if (removeOtherComponent)
        {
            Component otherComponent = target.GetComponent("OtherComponentName");
            if (otherComponent != null)
            {
                Undo.DestroyObjectImmediate(otherComponent);
                removedCount++;
            }
        }
        */

        return removedCount;
    }
    }
}
