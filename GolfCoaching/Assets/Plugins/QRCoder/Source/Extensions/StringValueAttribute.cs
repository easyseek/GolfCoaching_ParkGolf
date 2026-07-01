#nullable enable
// QRCoder Unity compatibility usings
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace QRCoder.Extensions
{
/// <summary>
/// Used to represent a string value for a value in an enum
/// </summary>
[Obsolete("This attribute will be removed in a future version of QRCoder.")]
[AttributeUsage(AttributeTargets.Field)]
public class StringValueAttribute : Attribute
{

    #region Properties

    /// <summary>
    /// Holds the alue in an enum
    /// </summary>
    public string StringValue { get; protected set; }

    #endregion

    /// <summary>
    /// Init a StringValue Attribute
    /// </summary>
    /// <param name="value"></param>
    public StringValueAttribute(string value)
    {
        StringValue = value;
    }
}

/// <summary>
/// Enumeration extension methods.
/// </summary>
[Obsolete("This class will be removed in a future version of QRCoder.")]
public static class CustomExtensions
{
    /// <summary>
    /// Will get the string value for a given enum's value
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("This method uses reflection to examine the provided enum value.")]
#endif
    public static string? GetStringValue(this Enum value)
    {
#if NETSTANDARD1_3
        var fieldInfo = value.GetType().GetRuntimeField(value.ToString());
#else
        var fieldInfo = value.GetType().GetField(value.ToString())!;
#endif
        var attr = fieldInfo.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];
        return attr!.Length > 0 ? attr[0].StringValue : null;
    }
}
}
