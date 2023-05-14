using System;

namespace GBookEdit.WPF
{
    public sealed class RecentFile
    {
        public string Name { get; }

        public string FullName { get; }


        public RecentFile(string path)
        {
            FullName = path;

            if (path.Length < 50)
            {
                Name = path;
            }
            else
            {
                Name = string.Concat("...", path.AsSpan(path.Length - 50));
            }
        }

        public override bool Equals(object? other)
        {
            if (other == null) return false;
            if (other == this) return true;
            if (other.GetType() != this.GetType()) return false;
            return ((RecentFile)other).FullName == FullName;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }
    }
}
