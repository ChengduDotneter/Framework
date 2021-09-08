using System.IO;

namespace Common
{
    public static class PathExtend
    {
        /// <summary>
        /// ×Ö·û´®Ìæ»»
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string TranslatePath(this string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}