using System;
using System.Collections.Generic;
using System.Globalization;

namespace HPDcePro.Core.XData
{
    /// <summary>
    /// Represents an AutoCAD Extended Entity Data (XData) record independent of host AutoCAD assemblies.
    /// </summary>
    public class XDataPacket
    {
        public string RegAppName { get; set; }
        public List<(int DxfCode, object Value)> Values { get; }

        public XDataPacket(string regAppName)
        {
            RegAppName = regAppName ?? throw new ArgumentNullException(nameof(regAppName));
            Values = new List<(int, object)>();
        }

        public void AddString(string value) => Values.Add((DxfCodeConstants.ExtendedDataAsciiString, value ?? string.Empty));
        public void AddHandle(string handle) => Values.Add((DxfCodeConstants.ExtendedDataHandle, handle ?? "0"));
        public void AddReal(double value) => Values.Add((DxfCodeConstants.ExtendedDataReal, value));
        public void AddInt16(short value) => Values.Add((DxfCodeConstants.ExtendedDataInteger16, value));
        public void AddInt32(int value) => Values.Add((DxfCodeConstants.ExtendedDataInteger32, value));
        public void AddControlString(string ctrl) => Values.Add((DxfCodeConstants.ExtendedDataControlString, ctrl ?? string.Empty));

        public string GetString(int index, string defaultValue = "")
        {
            if (index < 0 || index >= Values.Count) return defaultValue;
            var val = Values[index].Value;
            return val?.ToString() ?? defaultValue;
        }

        public double GetDouble(int index, double defaultValue = 0.0)
        {
            if (index < 0 || index >= Values.Count) return defaultValue;
            object val = Values[index].Value;
            if (val is double d) return d;
            if (val is float f) return f;
            if (val is int i) return i;
            if (val is short s) return s;
            if (val is string str && double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }
            return defaultValue;
        }

        public short GetInt16(int index, short defaultValue = 0)
        {
            if (index < 0 || index >= Values.Count) return defaultValue;
            object val = Values[index].Value;
            if (val is short s) return s;
            if (val is int i) return (short)i;
            if (val is double d) return (short)d;
            if (val is string str && short.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out short parsed))
            {
                return parsed;
            }
            return defaultValue;
        }

        public int GetInt32(int index, int defaultValue = 0)
        {
            if (index < 0 || index >= Values.Count) return defaultValue;
            object val = Values[index].Value;
            if (val is int i) return i;
            if (val is short s) return s;
            if (val is long l) return (int)l;
            if (val is double d) return (int)d;
            if (val is string str && int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }
            return defaultValue;
        }

        public string GetHandle(int index, string defaultValue = "0")
        {
            if (index < 0 || index >= Values.Count) return defaultValue;
            return Values[index].Value?.ToString() ?? defaultValue;
        }
    }
}
