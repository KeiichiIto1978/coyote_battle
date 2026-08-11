using System.Globalization;
using System.Text.RegularExpressions;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// 数字宣言欄の文字列をApplicationへ渡せる整数へ変換します。
    /// </summary>
    public static class NumberDeclarationInputValidator
    {
        private static readonly Regex HalfWidthDigits = new Regex(@"^[0-9]+$");

        /// <summary>
        /// 入力形式、整数範囲、宣言の単調増加を検証します。
        /// </summary>
        /// <param name="input">画面へ入力された文字列です。</param>
        /// <param name="previousValue">直前宣言値。履歴がなければnullです。</param>
        /// <param name="value">成功時の宣言値です。</param>
        /// <param name="error">失敗時に画面へ表示する理由です。</param>
        /// <returns>Applicationへ送信可能な場合はtrueです。</returns>
        public static bool TryValidate(
            string input,
            int? previousValue,
            out int value,
            out string error
        )
        {
            value = default;
            var trimmed = input?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                error = "数字を入力してください。";
                return false;
            }

            if (!HalfWidthDigits.IsMatch(trimmed))
            {
                error = "半角の正の整数を入力してください。";
                return false;
            }

            if (!int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                error = $"{int.MaxValue}以下の整数を入力してください。";
                return false;
            }

            if (value <= 0)
            {
                error = "1以上の整数を入力してください。";
                return false;
            }

            if (previousValue.HasValue && value <= previousValue.Value)
            {
                error = $"直前の宣言 {previousValue.Value} より大きい数字を入力してください。";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
