using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor.Utilities
{
    public class VRCSDKUtility
    {
        /// <summary>
        /// VRCSDKのバージョンを取得する
        /// </summary>
        /// <returns></returns>
        public static string GetVRCSDKVersion()
        {
            string path = GetVRCSDKFilePath("version");
            return FileUtility.GetFileTexts(path);
        }

        /// <summary>
        /// VRCSDKに含まれるファイルを取得する
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetVRCSDKFilePath(string fileName)
        {
            // VPM版SDKはPackages/com.vrchat.*配下にあるため、フォルダ名ではなく
            // 完全一致するファイル名を優先して検索する。
            return AssetDatabase.FindAssets(fileName)
                        .Select(g => AssetDatabase.GUIDToAssetPath(g))
                        .Where(path => string.Equals(
                            Path.GetFileNameWithoutExtension(path),
                            fileName,
                            StringComparison.OrdinalIgnoreCase))
                        .OrderBy(path => path.StartsWith(
                            "Packages/com.vrchat.",
                            StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                        .ThenBy(path => path.Length)
                        .FirstOrDefault();
        }

        /// <summary>
        /// VRCSDKが新しいUIかどうか
        /// </summary>
        /// <returns></returns>
        public static bool IsNewSDKUI()
        {
            var sdkVersion = GetVRCSDKVersion();
            // 新UI以降のバージョンにはファイルが存在するため何かしらは返ってくる
            if (string.IsNullOrEmpty(sdkVersion)) return false;

            var dotChar = '.';
            var zero = '0';
            var versions = sdkVersion.Split(dotChar);
            var version =
                    versions[0].PadLeft(4, zero) + dotChar +
                    versions[1].PadLeft(2, zero) + dotChar +
                    versions[2].PadLeft(2, zero);
            var newVersion = "2019.08.23";

            return newVersion.CompareTo(version) <= 0;
        }

        public static void UploadAvatar(bool newSDKUI)
        {
            if (newSDKUI)
            {
                EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
            }
            else
            {
                EditorApplication.ExecuteMenuItem("VRChat SDK/Show Build Control Panel");
            }
        }
    }
}
