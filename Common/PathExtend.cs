using System.IO;

namespace Common
{
    public static class PathExtend
    {
        public static string TranslatePath(this string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}