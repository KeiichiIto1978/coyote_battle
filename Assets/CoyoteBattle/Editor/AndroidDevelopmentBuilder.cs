using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace CoyoteBattle.Editor
{
    /// <summary>
    /// ローカル検証用のAndroid Development APKを生成します。
    /// </summary>
    public static class AndroidDevelopmentBuilder
    {
        private const string OutputArgument = "-androidOutputPath";

        /// <summary>
        /// 有効なBuild Settingsのシーンを入力として、指定先へDevelopment APKを出力します。
        /// ビルド失敗時は例外を送出し、batchmodeの終了コードへ失敗を伝えます。
        /// </summary>
        public static void Build()
        {
            string outputPath = GetOutputPath(Environment.GetCommandLineArgs());
            string[] scenePaths = EditorBuildSettings
                .scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenePaths.Length == 0)
            {
                throw new InvalidOperationException("Androidビルド対象のシーンがありません。");
            }

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOperationException("APK出力先のディレクトリを特定できません。");
            }

            Directory.CreateDirectory(outputDirectory);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Android Developmentビルドに失敗しました: {report.summary.result}"
                );
            }
        }

        /// <summary>
        /// コマンドライン引数を入力として、絶対パスへ正規化したAPK出力先を返します。
        /// 必須引数がない場合や値が空の場合は例外を送出します。
        /// </summary>
        private static string GetOutputPath(string[] arguments)
        {
            int argumentIndex = Array.IndexOf(arguments, OutputArgument);
            if (argumentIndex < 0 || argumentIndex + 1 >= arguments.Length)
            {
                throw new ArgumentException($"必須引数がありません: {OutputArgument}");
            }

            string outputPath = arguments[argumentIndex + 1];
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException($"出力先が空です: {OutputArgument}");
            }

            return Path.GetFullPath(outputPath);
        }
    }
}
