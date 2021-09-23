using Aliyun.OSS;
using System;
using System.IO;

namespace Common
{
    /// <summary>
    /// 阿里云文件服务器帮助实体
    /// </summary>
    public static class AliYunFileHelper
    {
        private static string m_accessKeyId = Convert.ToString(ConfigManager.Configuration["AccessKeyId"]);
        private static string m_endpoint = Convert.ToString(ConfigManager.Configuration["Endpoint"]);
        private static string m_accessKeySecret = Convert.ToString(ConfigManager.Configuration["AccessKeySecret"]);

        private static OssClient client;

        static AliYunFileHelper()
        {
            client = new OssClient(m_endpoint, m_accessKeyId, m_accessKeySecret);
        }

        private static void CreatureBucketName(string bucketName)
        {
            if (!client.DoesBucketExist(bucketName))
                client.CreateBucket(bucketName);
        }

        /// <summary>
        /// 上传本地文件
        /// </summary>
        /// <param name="bucketName">阿里云服务器BUCKET</param>
        /// <param name="objectFilePath">阿里云文件路径</param>
        /// <param name="localFileName">本地文件地址</param>
        /// <returns></returns>
        public static PutObjectResult UploadLocalFile(string bucketName, string localFileName, string objectFilePath)
        {
            try
            {
                CreatureBucketName(bucketName);

                return client.PutObject(bucketName, objectFilePath, localFileName);
            }
            catch (Exception exception)
            {
                throw new DealException($"上传文件失败,ERROR:{exception.Message}");
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        ///  <param name="bucketName">阿里云服务器BUCKET</param>
        /// <param name="binaryData">文件流</param>
        /// <param name="objectFilePath">阿里云文件路径</param>
        /// <returns></returns>
        public static PutObjectResult UploadFile(string bucketName, byte[] binaryData, string objectFilePath)
        {
            return UploadFile(bucketName, new MemoryStream(binaryData), objectFilePath);
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="bucketName">阿里云服务器BUCKET</param>
        /// <param name="requestContent">文件流</param>
        /// <param name="objectFilePath">阿里云文件路径</param>
        /// <returns></returns>
        public static PutObjectResult UploadFile(string bucketName, Stream requestContent, string objectFilePath)
        {
            try
            {
                CreatureBucketName(bucketName);

                // 上传文件。
                return client.PutObject(bucketName, objectFilePath, requestContent);
            }
            catch (Exception exception)
            {
                throw new DealException($"上传文件失败,ERROR:{exception.Message}");
            }
        }

        /// <summary>
        /// 将文件上传到指定的URL路径
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestContent"></param>
        /// <returns></returns>
        public static PutObjectResult UploadFileAppointUrl(string url, Stream requestContent)
        {
            return UploadFileAppointUrl(new Uri(url), requestContent);
        }

        /// <summary>
        /// 将文件上传到指定的URL路径
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="binaryData"></param>
        /// <returns></returns>
        public static PutObjectResult UploadFileAppointUrl(Uri uri, byte[] binaryData)
        {
            return UploadFileAppointUrl(uri, new MemoryStream(binaryData));
        }

        /// <summary>
        /// 将文件上传到指定的URL路径
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="requestContent"></param>
        /// <returns></returns>
        public static PutObjectResult UploadFileAppointUrl(Uri uri, Stream requestContent)
        {
            using (var stream = new MemoryStream())
            {
                requestContent.CopyTo(stream);
                return UploadFileAppointUrl(uri, stream);
            }
        }

        /// <summary>
        /// 指定位置上传文件
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="memoryStream"></param>
        /// <returns></returns>
        public static PutObjectResult UploadFileAppointUrl(Uri uri, MemoryStream memoryStream)
        {
            try
            {
                memoryStream.Position = 0;
                return client.PutObject(uri, memoryStream);
            }
            catch (Exception exception)
            {
                throw new DealException($"上传文件失败,ERROR:{exception.Message}");
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="bucketName">阿里云服务器BUCKET</param>
        /// <param name="objectFilePath">阿里云文件路径</param>
        /// <returns></returns>
        public static Stream DownLoadFile(string bucketName, string objectFilePath)
        {
            try
            {
                CreatureBucketName(bucketName);

                var obj = client.GetObject(bucketName, objectFilePath);

                return obj.Content;
            }
            catch (Exception exception)
            {
                throw new DealException($"下载文件失败,ERROR:{exception.Message}");
            }
        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="bucketName">阿里云服务器BUCKET</param>
        /// <param name="objectFilePath">阿里云文件路径</param>
        /// <returns></returns>
        public static bool FileExist(string bucketName, string objectFilePath)
        {
            try
            {
                CreatureBucketName(bucketName);
                return client.DoesObjectExist(bucketName, objectFilePath);
            }
            catch (Exception exception)
            {
                throw new DealException($"判断文件是否存在出错,ERROR:{exception.Message}");
            }
        }
    }
}