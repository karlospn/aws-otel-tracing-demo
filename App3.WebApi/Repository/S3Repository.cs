using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace App3.WebApi.Repository
{
    public class S3Repository : IS3Repository
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3Repository> _logger;
        private readonly IConfiguration _configuration;

        public S3Repository(IAmazonS3 s3Client, 
            ILogger<S3Repository> logger, 
            IConfiguration configuration)
        {
            _s3Client = s3Client;
            _logger = logger;
            _configuration = configuration;
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
                    BucketName = _configuration["S3:BucketName"]
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
