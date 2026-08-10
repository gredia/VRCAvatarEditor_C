# VPM配布手順

このリポジトリには、公式VPM Package Templateを基にしたリリース・Listing生成ワークフローが含まれています。

## 初回だけ行う設定

1. GitHubリポジトリの`Settings > Pages`を開きます。
2. `Build and deployment > Source`を`GitHub Actions`に設定します。
3. Actionsの実行が許可されていることを確認します。

パッケージIDはワークフロー内で`com.gredia.vrc-avatar-editor`に固定しているため、Repository Variableの追加は不要です。

## リリース

1. `Packages/com.gredia.vrc-avatar-editor/package.json`の`version`を更新します。
2. `Editor/VRCAvatarEditorGUI.cs`の`TOOL_VERSION`と`CHANGELOG.md`も同じバージョンへ更新します。
3. 変更を`master`へpushします。
4. GitHubの`Actions > Build Release > Run workflow`を実行します。
5. `com.gredia.vrc-avatar-editor-<version>.zip`、`.unitypackage`、`package.json`を含むGitHub Releaseが作成されます。
6. `Build Repo Listing`が続けて実行され、GitHub Pagesへ`index.json`とインストールページが公開されます。

公開済みのタグやReleaseは削除・差し替えず、修正時は必ずバージョンを上げてください。

## 公開前チェック

- Unity 2022.3.22f1の新規Avatarプロジェクトでエラーなくコンパイルできる
- NDMF 1.14.4のプレビューを有効・無効にできる
- Modular Avatarによる位置・構造変更がアバターモニターへ反映される
- 旧`Assets/VRCAvatarEditor`版からVCC経由で更新できる
- 作成したAnimationClipが「編集」を押したステートへ設定される
- パッケージZIP直下に`package.json`が存在する
