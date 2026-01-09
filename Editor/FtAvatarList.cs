using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace YuKoike.Tools
{
    [CreateAssetMenu(fileName = "FtAvatarList", menuName = "FtAvatar Modifier/FtAvatarList", order = 1)]
    public class FtAvatarList : ScriptableObject
{
    [Header("FtAvatar Settings")]
    public List<GameObject> ftAvatars = new List<GameObject>();

    [Header("Detection Settings")]
    [Tooltip("FtAvatarの識別に使用するサフィックス")]
    public string avatarSuffix = "--FT";

    [Header("Options")]
    [Tooltip("自動でプロジェクト内のFtAvatarを検索して追加")]
    public bool autoFindAvatars = false;

    [Tooltip("処理前にバックアップを作成")]
    public bool createBackup = true;

    private void OnValidate()
    {
        // null参照を自動で削除
        if (ftAvatars != null)
        {
            ftAvatars.RemoveAll(avatar => avatar == null);
        }
    }

    // アバター自動検索機能
    public void AutoFindAvatars()
    {
        if (ftAvatars == null)
            ftAvatars = new List<GameObject>();

        // プロジェクト内の全プレハブを検索
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int addedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && IsFtAvatar(prefab) && !ftAvatars.Contains(prefab))
            {
                ftAvatars.Add(prefab);
                addedCount++;
            }
        }

        Debug.Log($"自動検索完了: {addedCount}個のFtAvatarを追加しました。");

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    // FtAvatarかどうかを判定（サフィックスによる判定のみ）
    private bool IsFtAvatar(GameObject prefab)
    {
        if (string.IsNullOrEmpty(avatarSuffix))
        {
            Debug.LogWarning("Avatar suffix is not set. Cannot identify FtAvatars.");
            return false;
        }

        return prefab.name.EndsWith(avatarSuffix);
    }

    // 重複削除
    public void RemoveDuplicates()
    {
        if (ftAvatars == null) return;

        int originalCount = ftAvatars.Count;
        var uniqueAvatars = new List<GameObject>();
        var addedPaths = new HashSet<string>();

        foreach (var avatar in ftAvatars)
        {
            if (avatar == null) continue;

            string path = AssetDatabase.GetAssetPath(avatar);
            if (!addedPaths.Contains(path))
            {
                uniqueAvatars.Add(avatar);
                addedPaths.Add(path);
            }
        }

        ftAvatars = uniqueAvatars;

        Debug.Log($"重複削除完了: {originalCount - ftAvatars.Count}個の重複を削除しました。");

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    // 統計情報取得
    public AvatarStats GetStats()
    {
        var stats = new AvatarStats();

        if (ftAvatars == null) return stats;

        stats.totalCount = ftAvatars.Count;
        stats.validCount = 0;
        stats.withFtSuffix = 0;

        foreach (var avatar in ftAvatars)
        {
            if (avatar == null) continue;

            stats.validCount++;

            if (IsFtAvatar(avatar))
            {
                stats.withFtSuffix++;
            }
        }

        return stats;
    }

    [System.Serializable]
    public class AvatarStats
    {
        public int totalCount;
        public int validCount;
        public int withFtSuffix;
    }
}

#if UNITY_EDITOR
// カスタムエディター
[CustomEditor(typeof(FtAvatarList))]
public class FtAvatarListEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        FtAvatarList ftAvatarList = (FtAvatarList)target;

        // デフォルトのインスペクターを描画
        DrawDefaultInspector();

        GUILayout.Space(10);

        // 統計情報表示
        var stats = ftAvatarList.GetStats();
        EditorGUILayout.LabelField("統計情報", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"総数: {stats.totalCount}");
        EditorGUILayout.LabelField($"有効: {stats.validCount}");
        EditorGUILayout.LabelField($"FtAvatar: {stats.withFtSuffix}");

        // 検出設定の表示
        if (!string.IsNullOrEmpty(ftAvatarList.avatarSuffix))
        {
            EditorGUILayout.LabelField($"検出サフィックス: '{ftAvatarList.avatarSuffix}'");
        }
        else
        {
            EditorGUILayout.HelpBox("サフィックスが設定されていません。FtAvatarを正しく識別できません。", MessageType.Warning);
        }

        GUILayout.Space(10);

        // ボタン群
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("自動検索"))
        {
            ftAvatarList.AutoFindAvatars();
        }

        if (GUILayout.Button("重複削除"))
        {
            ftAvatarList.RemoveDuplicates();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // メイン実行ボタン
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("FtAvatar Modifier を実行", GUILayout.Height(30)))
        {
            if (stats.validCount == 0)
            {
                EditorUtility.DisplayDialog("エラー", "有効なFtAvatarがありません。", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("確認",
                $"{stats.withFtSuffix}個のFtAvatarを変更します。続行しますか？",
                "実行", "キャンセル"))
            {
                FTAvatarModifier.ModifyFromList(ftAvatarList);
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);

        if (GUILayout.Button("FtAvatar Modifier ウィンドウを開く"))
        {
            var window = EditorWindow.GetWindow<FTAvatarModifier>("FT Avatar Modifier");
            // ウィンドウにこのリストを設定
            var field = typeof(FTAvatarModifier).GetField("ftAvatarList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(window, ftAvatarList);
        }
    }
    }
}
#endif
