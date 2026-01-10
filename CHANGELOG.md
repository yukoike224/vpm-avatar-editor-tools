# 変更履歴

このプロジェクトの全ての重要な変更はこのファイルに記録されます。

## [1.1.0] - 2025-01-10

### 追加機能
- **lilEmo × TriturboFT Integration**: lilEmoとTriturbo Face Tracking Frameworkを統合するNDMFプラグイン
  - FacialExpressionsDisabledパラメータによる表情制御の切り替えに対応
  - lilEmoレイヤーのMenu、GestureLeft、GestureRight遷移に自動的に条件を追加
  - Neutralステートからの遷移にも対応
  - BuildPhase.Transforming + AfterPlugin("nadena.dev.modular-avatar")で実行
  - Assembly definition (YuKoike.Editor.asmdef) を追加し、適切なパッケージ参照を設定

### 機能拡張
- **Create Kaihen**: AnimatorController、AnimationClip、VRCExpressions対応を追加
  - AnimatorController、AnimationClipのバリアント作成に対応
  - VRCExpressionsMenu、VRCExpressionParametersのバリアント作成に対応
  - 各アセットタイプに応じた適切な出力先フォルダを自動選択

## [1.0.0] - 2025-12-15

### 追加機能
- **Copy Asset Full Path**: アセットのフルパスをクリップボードにコピー
- **GameObject Component Remover**: GameObjectからコンポーネントを再帰的に削除
- **GameObject Component Copier**: コンポーネントを複数のオブジェクトに一括コピー
- **Create Work Folders**: 作業用フォルダ構造を自動生成
- **FT Avatar Modifier**: FtAvatarの一括処理ツール
- **FT Avatar Parameter Optimizer**: VRChatアバターのパラメータ最適化（256bit制限対応）
- **Skybox Changer**: 複数シーンのSkyboxを一括変更
- **Create Kaihen**: マテリアルバリアントを改変フォルダに作成
- **KoikeEditorUtility**: 各ツールで使用する共通ユーティリティクラス

### 主な機能
- Modular Avatar Scale Adjusterの削除・コピーに対応
- BOOTH構造のアセットを自動認識してフォルダ構造を生成
- FtAvatarのFaceEmoPrefab自動無効化
- Light Limit Changerパラメータの段階的削減
- バッチ処理とプログレスバー表示
- Undo対応で安全な編集作業

[1.0.0]: https://github.com/yukoike224/vpm-avatar-editor-tools/releases/tag/v1.0.0
