using System.Threading.Tasks;
using App3.WebApi.Events;
using App3.WebApi.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace App3.WebApi.Controllers
{
    [ApiController]
    [Route("s3-to-event")]
    public class MessageToS3Controller : ControllerBase
    {
        private readonly IS3Repository _repository;
        private readonly ISqsRepository _eventPublisher;
        private readonly ILogger<MessageToS3Controller> _logger;

        public MessageToS3Controller(IS3Repository repository, 
            ISqsRepository eventPublisher, 
            ILogger<MessageToS3Controller> logger)
        {
            _repository = repository;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        [HttpPost]
        public async Task PostMessage([FromBody]string message)
        {
          _logger.LogTrace("You called the s3 save message endpoint");
           if (!string.IsNullOrEmpty(message))
           {
               await _repository.Persist(message);
               await _eventPublisher.Publish(new MessagePersistedEvent {Message = message});
           }

        }
    }
}
