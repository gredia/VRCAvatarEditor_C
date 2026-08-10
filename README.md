# VRC Avatar Editor Community Fork

Unity 2022.3.22f1向けのVRC Avatar Editor改変版です。パッケージ本体は[`Packages/com.gredia.vrc-avatar-editor`](Packages/com.gredia.vrc-avatar-editor)にあります。

機能面はオリジナル版とほぼ同様です。  
NDMFのプレビューなどを対応して現環境で扱いやすくします。  


## VCCでインストール
**導入前に必ずオリジナル版（Unitypackage版,VPM版）を削除してから行ってください**  
次の順にVPMリポジトリをVCCへ追加してください。

1. `https://vpm.nadena.dev/vpm.json`
2. `https://gredia.github.io/VRCAvatarEditor_C/index.json`

その後、対象プロジェクトのManage Projectから`Non-Destructive Modular Framework`と`VRC Avatar Editor (Community Fork)`を追加します。

## 開発環境

- Unity 2022.3.22f1
- VRChat SDK - Avatars 3.10.4
- NDMF 1.14.4

VCCでこのプロジェクトを開く前にNadena VPM Repositoryを登録し、`Packages/vpm-manifest.json`の依存関係を解決してください。

## 公開

GitHub ReleaseとVPM Listingの作成手順は[`DISTRIBUTION.md`](DISTRIBUTION.md)を参照してください。

## ライセンス

Original VRC Avatar Editor Copyright (C) 2019 gatosyocora. This is a modified community fork distributed under the zlib License. See the package `LICENSE.txt` and `USING_SOFTWARE_LICENSES.txt` files.
