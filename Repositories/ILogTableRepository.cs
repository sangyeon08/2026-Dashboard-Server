using Dashboard_server.Models.Storage;

namespace Dashboard_server.Repositories;

public interface ILogTableRepository
{
    Task AddAsync(LogEntity entity);

    Task<List<LogEntity>> ListByDateAsync(string dateKey, int maxCount = 200);
}
