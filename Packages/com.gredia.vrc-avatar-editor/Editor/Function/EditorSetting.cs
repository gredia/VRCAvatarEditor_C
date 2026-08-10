using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRCAvatarEditor.Base;
using static VRCAvatarEditor.VRCAvatarEditorGUI;
using VRCAvatar = VRCAvatarEditor.Base.VRCAvatarBase;

namespace VRCAvatarEditor
{
    public class EditorSetting : ScriptableSingleton<EditorSetting>
    {
        private const string SettingsFileName = "VRCAvatarEditorSettings.json";

        private SettingData _data;

        public SettingData Data
        {
            get
            {
                if (_data == null) LoadSettingData();
                return _data;
            }
            private set => _data = value;
        }

        private static string SettingsFilePath
        {
            get
            {
                var projectFolder = Path.GetDirectoryName(Application.dataPath);
                return Path.Combine(projectFolder ?? string.Empty, "UserSettings", SettingsFileName);
            }
        }

        public void LoadSettingData()
        {
            ReleaseSettingData();

            var defaultSetting = Resources.Load<SettingData>("DefaultSettingData");
            Data = defaultSetting != null
                ? Instantiate(defaultSetting)
                : CreateInstance<SettingData>();
            Data.hideFlags = HideFlags.HideAndDontSave;

            if (!File.Exists(SettingsFilePath)) return;

            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsFilePath), Data);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[VRCAvatarEditor] Could not load settings from '{SettingsFilePath}'. " +
                    $"The default settings will be used.\n{exception}");
            }
        }

        /// <summary>
        /// 設定情報を読み込む
        /// </summary>
        public (LayoutType, string) LoadSettingDataFromScriptableObject(
            string editorFolderPath,
            string language,
            AvatarMonitorGUI avatarMonitorGUI,
            FaceEmotionGUIBase faceEmotionGUI)
        {
            LoadSettingData();

            LocalizeText.instance.LoadLanguageTypesFromLocal(editorFolderPath);

            if (LocalizeText.instance.langPair == null)
            {
                LocalizeText.instance.FirstLoad();
            }

            if (string.IsNullOrEmpty(language) || Data.language != LocalizeText.instance.langPair.name)
            {
                // awaitするとUIスレッドが止まっておかしくなるのでawaitしない
                _ = LocalizeText.instance.LoadLanguage(Data.language);
            }

            var layoutType = Data.layoutType;
            language = Data.language;

            avatarMonitorGUI.LoadSettingData(Data);
            faceEmotionGUI.LoadSettingData(Data);

            return (layoutType, language);
        }

        /// <summary>
        /// 設定情報をUserSettingsへ保存する。
        /// パッケージ内へ書き込まないため、VPM更新時にも設定が維持される。
        /// </summary>
        public void SaveSettingDataToScriptableObject(
            LayoutType layoutType,
            string language,
            AvatarMonitorGUI avatarMonitorGUI,
            FaceEmotionGUIBase faceEmotionGUI)
        {
            var settingData = Data;

            avatarMonitorGUI.SaveSettingData(ref settingData);
            faceEmotionGUI.SaveSettingData(ref settingData);

            settingData.layoutType = layoutType;
            settingData.language = language;

            try
            {
                var settingsFolder = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(settingsFolder))
                {
                    Directory.CreateDirectory(settingsFolder);
                }

                File.WriteAllText(SettingsFilePath, JsonUtility.ToJson(settingData, true));
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[VRCAvatarEditor] Could not save settings to '{SettingsFilePath}'.\n{exception}");
            }
        }

        /// <summary>
        /// 自分の設定情報を削除する
        /// </summary>
        public void DeleteMySettingData()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    File.Delete(SettingsFilePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[VRCAvatarEditor] Could not delete settings at '{SettingsFilePath}'.\n{exception}");
            }
            finally
            {
                ReleaseSettingData();
            }
        }

        /// <summary>
        /// 設定を反映する
        /// </summary>
        public void ApplySettingsToEditorGUI(VRCAvatar edittingAvatar, FaceEmotionGUIBase faceEmotionGUI)
        {
            if (edittingAvatar.Animator == null) return;

            foreach (var skinnedMesh in edittingAvatar.SkinnedMeshList)
            {
                if (skinnedMesh.BlendShapeCount <= 0) continue;

                if (edittingAvatar.LipSyncShapeKeyNames != null && edittingAvatar.LipSyncShapeKeyNames.Count > 0)
                {
                    // TODO: 別のところにも同じコードがあるのでひとつにしたい
                    var exclusionsBlendShapes = faceEmotionGUI.blendshapeExclusions
                        .Select(name => new ExclusionBlendShape(name, ExclusionMatchType.Contain))
                        .Union(
                            edittingAvatar.LipSyncShapeKeyNames
                                .Select(name => new ExclusionBlendShape(name, ExclusionMatchType.Perfect)));
                    skinnedMesh.SetExclusionBlendShapesByContains(exclusionsBlendShapes);
                }

                if (faceEmotionGUI.selectedSortType == FaceEmotionGUIBase.SortType.AToZ)
                    skinnedMesh.SortBlendShapesToAscending();
                else
                    skinnedMesh.ResetDefaultSort();
            }
        }

        private void ReleaseSettingData()
        {
            if (_data != null)
            {
                DestroyImmediate(_data);
                _data = null;
            }
        }
    }
}
