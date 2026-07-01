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
public partial class QRCodeGenerator
{
    /// <summary>
    /// Represents the alignment pattern used in QR codes, which helps ensure the code remains readable even if it is somewhat distorted.
    /// Each QR code version has its own specific alignment pattern locations which this struct encapsulates.
    /// </summary>
    private struct AlignmentPattern
    {
        /// <summary>
        /// The version of the QR code. Higher versions have more complex and numerous alignment patterns.
        /// </summary>
        public int Version;

        /// <summary>
        /// A list of points where alignment patterns are located within the QR code matrix.
        /// Each point represents the center of an alignment pattern.
        /// </summary>
        public List<Point> PatternPositions;
    }
}
}
