using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace Common
{
    /// <summary>
    /// ZIP压缩帮助类
    /// </summary>
    public class GZipHelper
    {
        /// <summary>
        /// 文件压缩
        /// </summary>
        /// <param name="sourceFilePath">待压缩文件路径</param>
        /// <param name="saveFilePath">待保存的压缩文件路径</param>
        /// <param name="isDelete">是否删除待压缩文件</param>
        /// <param name="encryptPassword">加密密码</param>
        /// <param name="compressionLevel">压缩等级(1-9，默认为1)</param>
        public static void FileToGZip(string sourceFilePath, string saveFilePath, int compressionLevel = 1, bool isDelete = false, string encryptPassword = "")
        {
            FileCompression(sourceFilePath, saveFilePath, Path.GetFileName(sourceFilePath), compressionLevel, isDelete, encryptPassword);
        }

        /// <summary>
        /// 文件压缩
        /// </summary>
        /// <param name="sourceFilePath">待压缩文件路径</param>
        /// <param name="saveFilePath">待保存的压缩文件路径</param>
        /// <param name="isDelete">是否删除待压缩文件</param>
        /// <param name="encryptPassword">加密密码</param>
        /// <param name="compressionedName">待压缩文件在压缩包中文件名</param>
        /// <param name="compressionLevel">压缩等级(1-9，默认为1)</param>
        public static void FileToGZip(string sourceFilePath, string saveFilePath, string compressionedName, int compressionLevel = 1, bool isDelete = false, string encryptPassword = "")
        {
            FileCompression(sourceFilePath, saveFilePath, compressionedName, compressionLevel, isDelete, encryptPassword);
        }

        /// <summary>
        /// 文件带文件夹压缩
        /// </summary>
        /// <param name="sourceFilePath">待压缩文件路径</param>
        /// <param name="saveFilePath">待保存的压缩文件路径</param>
        /// <param name="isDelete">是否删除待压缩文件</param>
        /// <param name="encryptPassword">加密密码</param>
        /// <param name="compressionLevel">压缩等级(1-9，默认为1)</param>
        public static void FileWithPackageToGZip(string sourceFilePath, string saveFilePath, int compressionLevel = 1, bool isDelete = false, string encryptPassword = "")
        {
            FileCompression(sourceFilePath, saveFilePath, sourceFilePath, compressionLevel, isDelete, encryptPassword);
        }

        /// <summary>
        /// 流压缩
        /// </summary>
        /// <param name="sourceStream">待压缩数据流</param>
        /// <param name="saveFilePath">待保存的压缩文件路径</param>
        /// <param name="encryptPassword">加密密码</param>
        /// <param name="compressionedName">待压缩文件在压缩包中文件名</param>
        /// <param name="compressionLevel">压缩等级(1-9，默认为1)</param>
        public static void StreamToGZip(Stream sourceStream, string saveFilePath, string compressionedName, int compressionLevel = 1, string encryptPassword = "")
        {
            SteamCompression(sourceStream, saveFilePath, compressionedName, compressionLevel, encryptPassword);
        }

        private static void FileCompression(string sourceFilePath, string saveFilePath, string compressionDirectory, int compressionLevel = 1, bool isDelete = false, string encryptPassword = "")
        {
            using (FileStream readFileStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            {
                SteamCompression(readFileStream, saveFilePath, compressionDirectory, compressionLevel, encryptPassword);
            }

            if (isDelete)
                File.Delete(sourceFilePath);
        }

        private static void SteamCompression(Stream sourceStream, string saveFilePath, string compressionDirectory, int compressionLevel = 1, string encryptPassword = "")
        {
            using (FileStream writeFileStream = File.Create(saveFilePath))
            {
                using (ZipOutputStream outStream = new ZipOutputStream(writeFileStream))
                {
                    //zip文档的一个条目
                    ZipEntry entry = new ZipEntry(compressionDirectory);

                    //压缩加密
                    if (!string.IsNullOrWhiteSpace(encryptPassword))
                        outStream.Password = encryptPassword;

                    //开始一个新的zip条目
                    outStream.PutNextEntry(entry);

                    //设置压缩级别
                    outStream.SetLevel(compressionLevel);

                    sourceStream.Seek(0, SeekOrigin.Begin);

                    ///缓冲区对象
                    byte[] buffer = SteamHelper.ReadSteamToBuffer(sourceStream, sourceStream.Length);

                    outStream.Write(buffer, 0, buffer.Length);

                    outStream.Finish();
                }
            }
        }
    }
}