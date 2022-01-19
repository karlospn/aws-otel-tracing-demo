using System.Threading.Tasks;

namespace App3.WebApi.Repository
{
    public interface IS3Repository
    {
        Task Persist(string message);
    }
}
