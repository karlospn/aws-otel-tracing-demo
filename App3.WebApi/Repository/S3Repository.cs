using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace App3.WebApi.Repository
{
    public class S3Repository : IS3Repository
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3Repository> _logger;

        public S3Repository(IAmazonS3 s3Client, 
            ILogger<S3Repository> logger)
        {
            _s3Client = s3Client;
            _logger = logger;
        }

        public async Task Persist(string message)
        {
            try
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(message));
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = ms,
                    Key = $"file-{DateTime.UtcNow}",
                    BucketName = "vytestevent"
                };

                var fileTransferUtility = new TransferUtility(_s3Client);
                await fileTransferUtility.UploadAsync(uploadRequest);
            } 
            catch (Exception ex)
            {
                _logger.LogError("Error trying to upload file to S3 bucket", ex);
                throw;          
            }
        }
    }
}
