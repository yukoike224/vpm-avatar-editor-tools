# YuKoike Avatar Editor Tools

VRChatアバター制作・改変を効率化するUnityエディタ拡張ツール集です。

## 機能

### 1. Copy Asset Full Path
選択したアセットのフルパスをクリップボードにコピーします。

**使い方:**
- アセットを選択
- メニュー: `Tools/Koike's Utils/Copy Asset Full Path`

### 2. GameObject Component Remover
GameObjectとその子階層から、指定したコンポーネントを再帰的に削除します。

**使い方:**
- メニュー: `Tools/Koike's Utils/GameObject Component Remover`
- ターゲットのルートオブジェクトを指定
- 削除するコンポーネントを選択
- 「選択されたコンポーネントを削除」ボタンをクリック

**対応コンポーネント:**
- Modular Avatar Scale Adjuster

### 3. GameObject Component Copier
1つのソースオブジェクトから複数のターゲットオブジェクトへコンポーネントを一括コピーします。VRChatアバターの素体Armatureから複数の衣装プレファブへの一括コピーに最適です。

**使い方:**
- メニュー: `Tools/Koike's Utils/GameObject Component Copier`
- ソースのルートオブジェクトを指定
- シーンでターゲットオブジェクトを選択し、「シーンの選択オブジェクトを追加」をクリック
- コピーするコンポーネントを選択
- 「バッチ処理を実行」ボタンをクリック

**対応コンポーネント:**
- Transform (位置、回転、スケール)
- Modular Avatar Scale Adjuster

### 4. Create Work Folders
BOOTHなどからインポートしたアセット用の作業フォルダ構造を自動生成します。

**使い方:**
- メニュー: `Tools/Koike's Utils/Create Work Folders`
- Source Folder: 元のアセットフォルダを指定
- Output Folder: 出力先フォルダ（`Assets/_MyWork` など）を指定
- 作成するフォルダを選択（Animation, Blender, Controller, Expression, FBX, Material, Prefab, PSD, Texture）
- 「Create Folders」ボタンをクリック

**フォルダ構造:**
```
Assets/_MyWork/Kaihen/[Vendor]/[Asset]/
  ├── Animation/
  ├── Blender/
  ├── Controller/
  ├── Expression/
  ├── FBX/
  ├── Material/
  ├── Prefab/
  ├── PSD/
  └── Texture/
```

### 5. FT Avatar Modifier
FtAvatar（FaceEmo Templateが適用されたアバター）を一括処理します。

**使い方:**
- メニュー: `Tools/Koike's Utils/FT Avatar Modifier`
- FtAvatarListアセットを作成または選択
- リストにFtAvatarを追加
- 「Modify FtAvatars」ボタンをクリック

**機能:**
- FaceEmoPrefabの自動無効化
- EditorOnlyタグの自動設定

### 6. FT Avatar Parameter Optimizer
FtAvatar適用済みアバターのパラメータを最適化し、256bit制限に収めます。

**使い方:**
- メニュー: `Tools/Koike's Utils/FT Avatar Parameter Optimizer`
- ヒエラルキーでアバターを選択
- 「ヒエラルキーで選択中のアバターを取得」ボタンをクリック
- 「最適化を実行」ボタンをクリック

**最適化内容:**
1. FaceEmoPrefabを無効化
2. パラメータコストが256bitを超える場合、LightLimitChangerパラメータを段階的に削減

**削減優先順位:**
1. `_LLC_MonochromeBlend` (ライトのモノクロ化調整)
2. `_LLC_UnlitIntensity` (Unlit調整)
3. `_LLC_ColorTempControl` (色温度調整)
4. `_LLC_EmissionIntensity` (エミッション調整)
5. `_LLC_Saturation` (彩度調整)

### 7. Skybox Changer
複数のシーンのSkyboxを一括で変更します。

**使い方:**
- メニュー: `Tools/Koike's Utils/Skybox Changer`
- New Skybox Material: 新しいSkyboxマテリアルを指定
- シーンをドラッグ&ドロップまたは手動追加
- 「Apply Skybox to All Scenes」ボタンをクリック

### 8. Create Kaihen (マテリアルバリアント作成)
選択したマテリアルのバリアントを改変フォルダに自動作成します。

**使い方:**
- Projectビューでマテリアルを選択
- 右クリック → `Create/Kaihen`

**出力先:**
- BOOTH構造のアセット: `Assets/_MyWork/Kaihen/[Vendor]/[Asset]/Material/`
- その他: `Assets/_MyWork/Kaihen/[Asset]/Material/`

**ファイル名:** `[元のマテリアル名]_Kaihen.mat`

## インストール方法

### VPM経由（推奨）
1. VRChat Creator Companion (VCC) を開く
2. Settings → Packages → Add Repository
3. 以下のURLを追加: `https://yukoike224.github.io/vpm-avatar-editor-tools/index.json`
4. プロジェクトを選択
5. "YuKoike Avatar Editor Tools" をインストール

### 手動インストール
1. [Releases](https://github.com/yukoike224/vpm-avatar-editor-tools/releases)から最新版をダウンロード
2. ZIPを解凍
3. `Packages`フォルダにコピー

## 動作環境

- Unity 2022.3 以降
- VRChat SDK 3.x（一部機能で使用）
- Modular Avatar（一部機能で使用）

## ライセンス

MIT License

## 作者

YuKoike
- GitHub: [@yukoike224](https://github.com/yukoike224)

## サポート

問題が発生した場合は、[GitHubのIssues](https://github.com/yukoike224/vpm-avatar-editor-tools/issues)に報告してください。
