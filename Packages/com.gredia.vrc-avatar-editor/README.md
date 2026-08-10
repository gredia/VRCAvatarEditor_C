# VRC Avatar Editor (Community Fork)

VRChatアバターの編集を支援するUnityエディター拡張です。
オリジナルのVRC Avatar EditorをUnity 2022.3向けに更新し、NDMFプレビュー連携を追加した改変版です。

## 対応環境

- Unity 2022.3.22f1
- VRChat SDK - Avatars 3.10.x
- NDMF 1.14.4以上、2.0.0未満

NDMFは必須依存です。Modular Avatar自体への直接依存はありませんが、Modular Avatarが導入されている場合は、そのNDMFプレビュー処理を通して位置・構造の変更をアバターモニターへ反映します。

## VCCからインストール

1. Nadena VPM RepositoryをVCCへ追加します。
   `https://vpm.nadena.dev/vpm.json`
2. このパッケージのVPM RepositoryをVCCへ追加します。
   `https://gredia.github.io/VRCAvatarEditor_C/index.json`
3. VCCのManage Projectから`VRC Avatar Editor (Community Fork)`を追加します。

旧版の`Assets/VRCAvatarEditor`がある場合、VCCによるインストール時に旧フォルダーが削除され、Packages版へ置き換わります。プロジェクトのバックアップを作成してから更新してください。

## 主な変更点

- Unity 2022.3.22f1およびVPMパッケージへ対応
- NDMF 1.14.4のプレビューをアバターモニターへ反映
- Modular AvatarなどによるNDMFプレビュー上の位置・構造変更へ対応
- MA Move Independentlyを含むアバターをScene間移動せず、安全にNDMFプレビューへ反映
- MA Move Independentlyを含む編集用アバターを目的のPreview Scene内へ直接複製
- 表情アニメーションの「編集」で選んだステートへ、作成したAnimationClipを設定
- 新規FX Controllerの先頭AvatarMaskに起因するハンドアニメーション不具合を修正
- フォーク元v0.7.1〜v0.8.0の変更を監査し、VRCSDK2対応コードと旧更新チェックを削除
- ユーザー設定をパッケージ外の`UserSettings/VRCAvatarEditorSettings.json`へ保存

## ライセンスと原作者

Original VRC Avatar Editor Copyright (C) 2019 gatosyocora.

この改変版はオリジナルと同じくzlib Licenseの条件で配布します。改変版であり、オリジナルそのものではありません。詳細は`LICENSE.txt`および`USING_SOFTWARE_LICENSES.txt`を参照してください。

- Original repository: https://github.com/gatosyocora/VRCAvatarEditor
- Maintained fork: https://github.com/gredia/VRCAvatarEditor_C
