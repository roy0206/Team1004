using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Settings
{
    public sealed class ConfigFieldRow : MonoBehaviour
    {
        public enum ValueKind
        {
            Float = 0,
            Int = 1,
            Bool = 2,
            FloatArray = 3
        }

        private static readonly Color NormalColor = new Color(0.92f, 0.95f, 0.97f, 1f);
        private static readonly Color InvalidColor = new Color(1f, 0.45f, 0.45f, 1f);

        [SerializeField] private string key;
        [SerializeField] private ValueKind kind;
        [SerializeField] private Text labelText;
        [SerializeField] private Text keyHintText;
        [SerializeField] private InputField valueField;
        [SerializeField] private Toggle valueToggle;

        public string Key => key;

        public ValueKind Kind => kind;

        public string Label => labelText != null && !string.IsNullOrEmpty(labelText.text) ? labelText.text : key;

        public void Bind(JToken token)
        {
            if (token == null)
                return;

            switch (kind)
            {
                case ValueKind.Bool:
                    if (valueToggle != null)
                        valueToggle.SetIsOnWithoutNotify(token.Type == JTokenType.Boolean && token.Value<bool>());
                    break;

                case ValueKind.Int:
                    SetFieldText(token.Value<int>().ToString(CultureInfo.InvariantCulture));
                    break;

                case ValueKind.FloatArray:
                    SetFieldText(FormatArray(token as JArray));
                    break;

                default:
                    SetFieldText(FormatFloat(token.Value<float>()));
                    break;
            }

            SetInvalid(false);
        }

        public bool TryBuild(out JToken token, out string error)
        {
            token = null;
            error = null;

            switch (kind)
            {
                case ValueKind.Bool:
                    token = new JValue(valueToggle != null && valueToggle.isOn);
                    return true;

                case ValueKind.Int:
                {
                    var text = GetFieldText();
                    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    {
                        error = $"{Label}: 정수만 넣을 수 있습니다 (\"{text}\")";
                        return false;
                    }

                    token = new JValue(parsed);
                    return true;
                }

                case ValueKind.FloatArray:
                {
                    var text = GetFieldText();
                    var array = new JArray();
                    var parts = text.Split(',');

                    for (var i = 0; i < parts.Length; i++)
                    {
                        var part = parts[i].Trim();
                        if (part.Length == 0)
                        {
                            if (parts.Length == 1)
                                break;

                            error = $"{Label}: {i + 1}번째 값이 비어 있습니다";
                            return false;
                        }

                        if (!float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                        {
                            error = $"{Label}: {i + 1}번째 값이 숫자가 아닙니다 (\"{part}\")";
                            return false;
                        }

                        array.Add(value);
                    }

                    token = array;
                    return true;
                }

                default:
                {
                    var text = GetFieldText();
                    if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    {
                        error = $"{Label}: 숫자만 넣을 수 있습니다 (\"{text}\")";
                        return false;
                    }

                    token = new JValue(value);
                    return true;
                }
            }
        }

        public void SetInvalid(bool invalid)
        {
            if (labelText != null)
                labelText.color = invalid ? InvalidColor : NormalColor;
        }

        public string KeyHint => keyHintText != null ? keyHintText.text : string.Empty;

        private string GetFieldText()
        {
            return valueField != null && valueField.text != null ? valueField.text.Trim() : string.Empty;
        }

        private void SetFieldText(string text)
        {
            if (valueField != null)
                valueField.text = text;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string FormatArray(JArray array)
        {
            if (array == null)
                return string.Empty;

            var builder = new StringBuilder();
            for (var i = 0; i < array.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(FormatFloat(array[i].Value<float>()));
            }

            return builder.ToString();
        }
    }
}
