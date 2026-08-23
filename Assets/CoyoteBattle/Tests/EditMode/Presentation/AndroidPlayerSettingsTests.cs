using CoyoteBattle.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// Issue #11で確定したAndroid実機・限定配布用Player設定を保証します。
    /// </summary>
    public sealed class AndroidPlayerSettingsTests
    {
        /// <summary>
        /// Application ID、API Level、ARM64、バージョンを再現可能な値に固定します。
        /// </summary>
        [Test]
        public void AndroidPlayerSettings_限定配布設定を確認する_API25から35のARM64APKとなる()
        {
            Assert.That(PlayerSettings.productName, Is.EqualTo("Coyote Battle"));
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                Is.EqualTo("com.keiichiito.coyotebattle")
            );
            Assert.That(
                PlayerSettings.Android.minSdkVersion,
                Is.EqualTo(AndroidSdkVersions.AndroidApiLevel25)
            );
            Assert.That(
                PlayerSettings.Android.targetSdkVersion,
                Is.EqualTo(AndroidSdkVersions.AndroidApiLevel35)
            );
            Assert.That(
                PlayerSettings.Android.targetArchitectures,
                Is.EqualTo(AndroidArchitecture.ARM64)
            );
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("1.0"));
            Assert.That(PlayerSettings.Android.bundleVersionCode, Is.EqualTo(1));
        }

        /// <summary>
        /// 縦画面や実行中回転によるUI崩れを防ぎ、横向き左だけを許可します。
        /// </summary>
        [Test]
        public void AndroidPlayerSettings_画面向きを確認する_横向き左に固定される()
        {
            Assert.That(
                PlayerSettings.defaultInterfaceOrientation,
                Is.EqualTo(UIOrientation.LandscapeLeft)
            );
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.True);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
        }

        /// <summary>
        /// ローカル実機用ビルドがReleaseや別プラットフォームへ変わる回帰を防ぎます。
        /// </summary>
        [Test]
        public void AndroidDevelopmentBuilder_ビルド設定を確認する_AndroidDevelopmentAPKとなる()
        {
            bool originalBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = true;
                AndroidDevelopmentBuilder.ConfigureAndroidBuild();
                var options = AndroidDevelopmentBuilder.CreateBuildPlayerOptions(
                    "CoyoteBattle-development.apk",
                    new[] { "Assets/CoyoteBattle/Scenes/Bootstrap.unity" }
                );

                Assert.That(EditorUserBuildSettings.buildAppBundle, Is.False);
                Assert.That(options.target, Is.EqualTo(BuildTarget.Android));
                Assert.That(options.options & BuildOptions.Development, Is.Not.EqualTo(0));
                Assert.That(options.locationPathName, Does.EndWith(".apk"));
                Assert.That(options.scenes, Has.Length.EqualTo(1));
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = originalBuildAppBundle;
            }
        }
    }
}
