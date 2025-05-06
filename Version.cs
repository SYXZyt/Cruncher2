namespace Cruncher
{
    public struct Version(byte major, byte minor, byte patch)
    {
        public byte major = major;
        public byte minor = minor;
        public byte patch = patch;

        public static Version Current =>
            new(2, 1, 0);

        public readonly uint PackedVersion =>
            (uint)((major << 16) | (minor << 8) | patch);

        public static bool operator<(Version a, Version b)
        {
            if (a.major != b.major)
                return a.major < b.major;

            if (a.minor != b.minor)
                return a.minor < b.minor;

            return a.patch < b.patch;
        }

        public static bool operator >(Version a, Version b)
        {
            if (a.major != b.major)
                return a.major > b.major;

            if (a.minor != b.minor)
                return a.minor > b.minor;

            return a.patch > b.patch;
        }

        public static bool operator <=(Version a, Version b)
        {
            return a < b || a == b;
        }

        public static bool operator >=(Version a, Version b)
        {
            return a > b || a == b;
        }

        public static bool operator ==(Version a, Version b)
        {
            return a.PackedVersion == b.PackedVersion;
        }

        public static bool operator !=(Version a, Version b)
        {
            return !(a == b);
        }

        public readonly override bool Equals(object obj)
        {
            if (obj is null || obj is not Version)
                return false;

            else
                return this == (Version)obj;
        }

        public readonly override int GetHashCode() =>
            PackedVersion.GetHashCode();

        public readonly override string ToString() =>
            $"{major}.{minor}.{patch}";
    }
}