using System;

namespace MathRunner.Core.Update
{
    /// <summary>Compares the running game's version with a release tag like "v0.2.0".</summary>
    public static class ReleaseVersion
    {
        /// <summary>Reads "0.2.0" or "v0.2.0". Pre-release tags such as "v0.3.0-beta" are rejected.</summary>
        public static bool TryParse(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            if (text.StartsWith("v") || text.StartsWith("V")) text = text.Substring(1);
            if (text.IndexOf('-') >= 0 || text.IndexOf('+') >= 0) return false;
            if (!Version.TryParse(text, out version)) return false;
            return version.Major >= 0 && version.Minor >= 0;
        }

        /// <summary>True only when both sides parse and the latest tag is higher than the current version.</summary>
        public static bool IsNewer(string current, string latest)
        {
            if (!TryParse(current, out var have) || !TryParse(latest, out var want)) return false;
            return Normalize(want).CompareTo(Normalize(have)) > 0;
        }

        // "0.2" and "0.2.0" are the same version; Version treats the missing parts as -1.
        private static Version Normalize(Version v)
        {
            return new Version(v.Major, v.Minor, System.Math.Max(v.Build, 0), System.Math.Max(v.Revision, 0));
        }
    }
}
