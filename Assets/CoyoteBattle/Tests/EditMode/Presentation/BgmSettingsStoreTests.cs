using CoyoteBattle.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// BGM設定の既定値、保存、および破損値からの復旧を保証します。
    /// </summary>
    public sealed class BgmSettingsStoreTests
    {
        private const string TestKey = "CoyoteBattle.Tests.BgmEnabled";

        /// <summary>
        /// テスト間でPlayerPrefsの値が引き継がれないよう削除します。
        /// </summary>
        [SetUp]
        [TearDown]
        public void ClearTestSetting()
        {
            PlayerPrefs.DeleteKey(TestKey);
        }

        /// <summary>
        /// 初回起動ではBGMがONになることを保証します。
        /// </summary>
        [Test]
        public void LoadEnabled_設定が未保存_既定値ONを返す()
        {
            var store = new PlayerPrefsBgmSettingsStore(TestKey);

            var enabled = store.LoadEnabled();

            Assert.That(enabled, Is.True);
        }

        /// <summary>
        /// ユーザーが選択したOFF設定を再起動相当の別インスタンスでも復元できることを保証します。
        /// </summary>
        [Test]
        public void SaveEnabled_OFFを保存_別インスタンスでOFFを返す()
        {
            var store = new PlayerPrefsBgmSettingsStore(TestKey);
            store.SaveEnabled(false);

            var restored = new PlayerPrefsBgmSettingsStore(TestKey).LoadEnabled();

            Assert.That(restored, Is.False);
        }

        /// <summary>
        /// OFFからONへ戻した設定を再起動相当の別インスタンスでも復元できることを保証します。
        /// </summary>
        [Test]
        public void SaveEnabled_OFFからONを保存_別インスタンスでONを返す()
        {
            var store = new PlayerPrefsBgmSettingsStore(TestKey);
            store.SaveEnabled(false);
            store.SaveEnabled(true);

            var restored = new PlayerPrefsBgmSettingsStore(TestKey).LoadEnabled();

            Assert.That(restored, Is.True);
        }

        /// <summary>
        /// 保存値が破損しても安全な既定値ONへ戻ることを保証します。
        /// </summary>
        [Test]
        public void LoadEnabled_保存値が不正_既定値ONへ戻す()
        {
            PlayerPrefs.SetString(TestKey, "invalid");
            var store = new PlayerPrefsBgmSettingsStore(TestKey);

            var enabled = store.LoadEnabled();

            Assert.That(enabled, Is.True);
        }
    }
}
