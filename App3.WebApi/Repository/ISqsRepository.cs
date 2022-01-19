using App3.WebApi.Events;
using System.Threading.Tasks;

namespace App3.WebApi.Repository
{
    public interface ISqsRepository
    {
        Task Publish(IEvent evt);
    }
}
