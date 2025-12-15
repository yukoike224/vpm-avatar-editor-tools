# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-12-15

### Added
- **Copy Asset Full Path**: アセットのフルパスをクリップボードにコピー
- **GameObject Component Remover**: GameObjectからコンポーネントを再帰的に削除
- **GameObject Component Copier**: コンポーネントを複数のオブジェクトに一括コピー
- **Create Work Folders**: 作業用フォルダ構造を自動生成
- **FT Avatar Modifier**: FtAvatarの一括処理ツール
- **FT Avatar Parameter Optimizer**: VRChatアバターのパラメータ最適化（256bit制限対応）
- **Skybox Changer**: 複数シーンのSkyboxを一括変更
- **Create Kaihen**: マテリアルバリアントを改変フォルダに作成
- **KoikeEditorUtility**: 各ツールで使用する共通ユーティリティクラス

### Features
- Modular Avatar Scale Adjusterの削除・コピーに対応
- BOOTH構造のアセットを自動認識してフォルダ構造を生成
- FtAvatarのFaceEmoPrefab自動無効化
- Light Limit Changerパラメータの段階的削減
- バッチ処理とプログレスバー表示
- Undo対応で安全な編集作業

[1.0.0]: https://github.com/yukoike224/vpm-avatar-editor-tools/releases/tag/v1.0.0
