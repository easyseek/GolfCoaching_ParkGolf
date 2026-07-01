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


namespace QRCoder
{
/// <summary>
/// Contains classes and methods for generating payloads for QR codes.
/// </summary>
public static partial class PayloadGenerator
{
    /// <summary>
    /// Represents the base class for all QR code payloads.
    /// </summary>
    public abstract class Payload
    {
        /// <summary>
        /// Gets the version of the QR code payload.
        /// </summary>
        public virtual int Version => -1;

        /// <summary>
        /// Gets the error correction level of the QR code payload.
        /// </summary>
        public virtual QRCodeGenerator.ECCLevel EccLevel => QRCodeGenerator.ECCLevel.Default;

        /// <summary>
        /// Gets the ECI mode of the QR code payload.
        /// </summary>
        public virtual QRCodeGenerator.EciMode EciMode => QRCodeGenerator.EciMode.Default;

        /// <summary>
        /// Returns a string representation of the QR code payload.
        /// </summary>
        /// <returns>A string representation of the QR code payload.</returns>
        public abstract override string ToString();
    }
}
}
