using Amazon.SQS;
using Amazon.SQS.Model;
using App3.WebApi.Events;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace App3.WebApi.Repository
{
    public class SqsRepository: ISqsRepository
    {
        private readonly IAmazonSQS _client;
        private readonly IConfiguration _configuration;

        public SqsRepository(IAmazonSQS client, 
            IConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
        }

        public async Task Publish(IEvent evt)
        {
            await _client.SendMessageAsync(new SendMessageRequest
            {
                MessageBody = (evt as MessagePersistedEvent)?.Message,
                QueueUrl =  _configuration["Sqs:Uri"]
            });
        }
    }
}
