using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace YuKoike.Tools
{
    public class CreateWorkFolders : EditorWindow
{
    Object sourceFolder;
    Object outputFolder;

    // フォルダ名と選択状態を管理する辞書
    private Dictionary<string, bool> folderOptions = new Dictionary<string, bool>()
    {
        { "Animation", false },
        { "Blender", true },    // デフォルトで選択
        { "Controller", false },
        { "Expression", false },
        { "FBX", true },        // デフォルトで選択
        { "Material", true },   // デフォルトで選択
        { "Prefab", false },
        { "PSD", false },
        { "Texture", false }
    };

    [MenuItem("Tools/Koike's Utils/Create Work Folders")]
    static void Init()
    {
        CreateWorkFolders window = (CreateWorkFolders)EditorWindow.GetWindow(typeof(CreateWorkFolders));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Create Work Folders", EditorStyles.boldLabel);

        GUILayout.Space(10);

        sourceFolder = EditorGUILayout.ObjectField("Source Folder:", sourceFolder, typeof(Object), false);
        outputFolder = EditorGUILayout.ObjectField("Output Folder:", outputFolder, typeof(Object), false);

        GUILayout.Space(10);

        GUILayout.Label("Select folders to create:", EditorStyles.boldLabel);

        // チェックボックスを表示
        List<string> keys = new List<string>(folderOptions.Keys);
        foreach (string folderName in keys)
        {
            folderOptions[folderName] = EditorGUILayout.Toggle(folderName, folderOptions[folderName]);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Create Folders"))
        {
            CreateFolders();
        }
    }

    void CreateFolders()
    {
        if (sourceFolder == null || outputFolder == null)
        {
            Debug.LogError("Please select both source and output folders.");
            return;
        }

        string sourceFolderPath = AssetDatabase.GetAssetPath(sourceFolder);
        string sourceFolderName = new DirectoryInfo(sourceFolderPath).Name;
        string outputFolderPath = AssetDatabase.GetAssetPath(outputFolder);

        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        // Vendorフォルダのパスを取得（Assets/_ImportedAssets/BOOTH/[Vendor]/[Asset] の場合）
        string vendorFolderName = null;
        DirectoryInfo sourceDir = new DirectoryInfo(sourceFolderPath);
        if (sourceDir.Parent != null && sourceDir.Parent.Parent != null && sourceDir.Parent.Parent.Name == "BOOTH")
        {
            vendorFolderName = sourceDir.Parent.Name;
        }

        // 出力パスの構築：Assets/_MyWork/Kaihen/[Vendor]/[Asset]
        string targetPath;
        if (!string.IsNullOrEmpty(vendorFolderName))
        {
            // 出力フォルダが既にKaihenフォルダかチェック
            bool isKaihenFolder = outputFolderPath.EndsWith("Kaihen") || outputFolderPath.EndsWith("Kaihen/") || outputFolderPath.EndsWith("Kaihen\\");
            
            string basePath;
            if (isKaihenFolder)
            {
                // 出力フォルダが既にKaihenフォルダの場合はそのまま使用
                basePath = outputFolderPath;
            }
            else
            {
                // Kaihenフォルダを作成
                basePath = Path.Combine(outputFolderPath, "Kaihen");
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }
            }

            // Vendorフォルダのパスを構築
            string vendorPath = Path.Combine(basePath, vendorFolderName);
            if (!Directory.Exists(vendorPath))
            {
                Directory.CreateDirectory(vendorPath);
            }

            targetPath = Path.Combine(vendorPath, sourceFolderName);
        }
        else
        {
            // 従来の動作（Vendorフォルダが特定できない場合）
            targetPath = Path.Combine(outputFolderPath, sourceFolderName);
        }

        // 選択されたフォルダのみ作成
        foreach (KeyValuePair<string, bool> option in folderOptions)
        {
            if (option.Value) // チェックされている場合のみフォルダを作成
            {
                string folderPath = Path.Combine(targetPath, option.Key);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
            }
        }

        AssetDatabase.Refresh();

        Debug.Log("Folders created successfully!");
    }
    }
}
