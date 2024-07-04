using UnityEngine;
using UnityEditor;

namespace ResourceCheckerPlus
{
    public class AudioChecker : ObjectChecker
    {
        public class AudioDetail : ObjectDetail
        {
            public AudioDetail(Object obj, AudioChecker checker) : base(obj, checker)
            {

            }

            public override void InitDetailCheckObject(Object obj)
            {
                AudioClip clip = obj as AudioClip;
                AudioChecker checker = currentChecker as AudioChecker;
                string compression = buildInType;
                int quality = 0;
                string sampleRateSetting = buildInType;
                int overrideSampleRate = 0;

                string androidOverride = buildInType;
                string androidLoadType = buildInType;
                string androidCompression = buildInType;
                int androidQuality = 0;
                string androidSampleRateSetting = buildInType;
                int androidSampleRate = 0;

                string iosOverride = buildInType;
                string iosLoadType = buildInType;
                string iosCompression = buildInType;
                int iosQuality = 0;
                string iosSampleRateSetting = buildInType;
                int iosSampleRate = 0;

                AudioImporter importer = AudioImporter.GetAtPath(assetPath) as AudioImporter;
                if (importer != null)
                {
                    compression = importer.defaultSampleSettings.compressionFormat.ToString();
                    quality = Mathf.Clamp((int)(importer.defaultSampleSettings.quality * 100), 1, 100);
                    sampleRateSetting = importer.defaultSampleSettings.sampleRateSetting.ToString();
                    overrideSampleRate = (int)importer.defaultSampleSettings.sampleRateOverride;

                    AudioImporterSampleSettings androidSettings = importer.GetOverrideSampleSettings(platformAndroid);
                    androidOverride = importer.ContainsSampleSettingsOverride(platformAndroid).ToString();
                    androidLoadType = androidSettings.loadType.ToString();
                    androidCompression = androidSettings.compressionFormat.ToString();
                    androidQuality = Mathf.Clamp((int)(androidSettings.quality * 100), 1, 100);
                    androidSampleRateSetting = androidSettings.sampleRateSetting.ToString();
                    androidSampleRate = (int)androidSettings.sampleRateOverride;

                    AudioImporterSampleSettings iosSettings = importer.GetOverrideSampleSettings(platformIOS);
                    iosOverride = importer.ContainsSampleSettingsOverride(platformIOS).ToString();
                    iosLoadType = iosSettings.loadType.ToString();
                    iosCompression = iosSettings.compressionFormat.ToString();
                    iosQuality = Mathf.Clamp((int)(iosSettings.quality * 100), 1, 100);
                    iosSampleRateSetting = iosSettings.sampleRateSetting.ToString();
                    iosSampleRate = (int)iosSettings.sampleRateOverride;

                }
                AddOrSetCheckValue(checker.audioLength, clip.length);
                AddOrSetCheckValue(checker.audioType, clip.loadType.ToString());
                AddOrSetCheckValue(checker.audioChannel, clip.channels);
                AddOrSetCheckValue(checker.audioCompression, compression);
                AddOrSetCheckValue(checker.audioQuality, quality);
                AddOrSetCheckValue(checker.audioSampleRateSetting, sampleRateSetting);
                AddOrSetCheckValue(checker.audioSampleRate, overrideSampleRate);
                AddOrSetCheckValue(checker.audioPostfix, ResourceCheckerHelper.GetAssetPostfix(assetPath));

                AddOrSetCheckValue(checker.audioAndroidOverride, androidOverride);
                AddOrSetCheckValue(checker.audioAndroidLoadType, androidLoadType);
                AddOrSetCheckValue(checker.audioAndroidCompressionFormat, androidCompression);
                AddOrSetCheckValue(checker.audioAndroidQuality, androidQuality);
                AddOrSetCheckValue(checker.audioAndroidSampleRateSetting, androidSampleRateSetting);
                AddOrSetCheckValue(checker.audioAndroidSampleRate, androidSampleRate);

                AddOrSetCheckValue(checker.audioIOSOverride, iosOverride);
                AddOrSetCheckValue(checker.audioIOSLoadType, iosLoadType);
                AddOrSetCheckValue(checker.audioIOSCompressionFormat, iosCompression);
                AddOrSetCheckValue(checker.audioIOSQuality, iosQuality);
                AddOrSetCheckValue(checker.audioIOSSampleRateSetting, iosSampleRateSetting);
                AddOrSetCheckValue(checker.audioIOSSampleRate, iosSampleRate);
            }
        }

        CheckItem audioLength;
        CheckItem audioType;
        CheckItem audioChannel;
        CheckItem audioCompression;
        CheckItem audioQuality;
        CheckItem audioSampleRate;
        CheckItem audioSampleRateSetting;
        CheckItem audioPostfix;

        CheckItem audioAndroidOverride;
        CheckItem audioAndroidLoadType;
        CheckItem audioAndroidCompressionFormat;
        CheckItem audioAndroidQuality;
        CheckItem audioAndroidSampleRateSetting;
        CheckItem audioAndroidSampleRate;

        CheckItem audioIOSOverride;
        CheckItem audioIOSLoadType;
        CheckItem audioIOSCompressionFormat;
        CheckItem audioIOSQuality;
        CheckItem audioIOSSampleRateSetting;
        CheckItem audioIOSSampleRate;

        public override void InitChecker()
        {
            checkerName = "AudioClip";
            checkerFilter = "t:AudioClip";
            enableReloadCheckItem = true;
            audioLength = new CheckItem(this, "长度", CheckType.Float);
            audioChannel = new CheckItem(this, "声道", CheckType.Int);
            audioType = new CheckItem(this, "加载类型", CheckType.String);
            audioCompression = new CheckItem(this, "压缩");
            audioQuality = new CheckItem(this, "质量", CheckType.Int);
            audioSampleRateSetting = new CheckItem(this, "采样率设置");
            audioSampleRate = new CheckItem(this, "自定义采样率", CheckType.Int);
            audioPostfix = new CheckItem(this, "后缀");

            audioAndroidOverride = new CheckItem(this, "安卓开启");
            audioAndroidLoadType = new CheckItem(this, "安卓加载类型");
            audioAndroidCompressionFormat = new CheckItem(this, "安卓压缩");
            audioAndroidQuality = new CheckItem(this, "安卓质量", CheckType.Int);
            audioAndroidSampleRateSetting = new CheckItem(this, "安卓采样率设置");
            audioAndroidSampleRate = new CheckItem(this, "安卓自定义采样率", CheckType.Int);

            audioIOSOverride = new CheckItem(this, "IOS开启");
            audioIOSLoadType = new CheckItem(this, "IOS加载类型");
            audioIOSCompressionFormat = new CheckItem(this, "IOS压缩");
            audioIOSQuality = new CheckItem(this, "IOS质量", CheckType.Int);
            audioIOSSampleRateSetting = new CheckItem(this, "IOS采样率设置");
            audioIOSSampleRate = new CheckItem(this, "IOS自定义采样率", CheckType.Int);
        }

        public override ObjectDetail AddObjectDetail(object obj, Object refObj, Object detailRefObj)
        {
            AudioClip clip = obj as AudioClip;
            if (clip == null)
                return null;
            ObjectDetail detail = null;
            //先查缓存
            foreach (var checker in CheckList)
            {
                if (checker.checkObject == clip)
                    detail = checker;
            }
            if (detail == null)
            {
                detail = new AudioDetail(clip, this);
            }
            detail.AddObjectReference(refObj, detailRefObj);
            return detail;
        }

        public override void AddObjectDetailRef(GameObject rootObj)
        {
            AudioSource[] audios = rootObj.GetComponentsInChildren<AudioSource>(true);
            foreach (var audio in audios)
            {
                AddObjectWithRef(audio.clip, audio.gameObject, rootObj);
            }
        }

        public override void BatchSetResConfig()
        {
            BatchAudioClipSettingEditor.Init(GetBatchOptionList());
        }
    }
}
