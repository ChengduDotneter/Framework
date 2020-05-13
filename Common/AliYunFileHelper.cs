using Aliyun.OSS;
using System;
using System.IO;

namespace Common
{
    public static class AliYunFileHelper
    {
        private static string m_accessKeyId = Convert.ToString(ConfigManager.Configuration["AccessKeyId"]);
        private static string m_endpoint = Convert.ToString(ConfigManager.Configuration["Endpoint"]) ;
        private static string m_accessKeySecret = Convert.ToString(ConfigManager.Configuration["AccessKeySecret"]) ;
        private static string m_bucketName = Convert.ToString(ConfigManager.Configuration["BucketName"]);//Bucket路径

        private static OssClient client;

        static AliYunFileHelper()
        {
            if (client == null)
            {
                client = new OssClient(m_endpoint, m_accessKeyId, m_accessKeySecret);
                var exist = client.DoesBucketExist(m_bucketName);

                if (!exist)
                {
                    client.CreateBucket(m_bucketName);
                }
            }
        }

        public static PutObjectResult UploadLocalFile(string fileName, string filePath,string currentFilePath)
        {
            try
            {
                return client.PutObject(m_bucketName,$"{currentFilePath}/{fileName}" , filePath);
            }
            catch
            {
                throw new DealException("上传文件失败");
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="binaryData">上传文件流</param>
        /// <param name="objectName">上传文件名</param>
        /// <returns></returns>
        public static PutObjectResult UploadFile(byte[] binaryData, string objectName, string currentFilePath)
        {
            try
            {
                MemoryStream requestContent = new MemoryStream(binaryData);
                // 上传文件。
                return client.PutObject(m_bucketName, $"{currentFilePath}/{objectName}", requestContent);
            }
            catch
            {
                throw new DealException("上传文件失败");
            }
        }

        public static Stream DownLoadFile(string objectName, string currentFilePath)
        {
            try
            {
                var obj = client.GetObject(m_bucketName, $"{currentFilePath}/{objectName}");

                return obj.Content;
            }
            catch 
            {
                throw new DealException("下载失败，请检查文件是否存在");
            }
        }

        public static bool FileExist(string objectName, string currentFilePath)
        {
            return client.DoesObjectExist(m_bucketName, $"{currentFilePath}/{objectName}");
        }

    }
}
