using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface IWorkspaceRepository
    {
        Task<Workspace> GetByKeyAsync(int idCenter, int idProduct, int idUser, CancellationToken cancellationToken = default);
        Task<Workspace> AddAsync(Workspace entity, CancellationToken cancellationToken = default);
        Task<Workspace> UpdateAsync(Workspace entity, CancellationToken cancellationToken = default);
        Task ClearByUserAsync(int idUser, CancellationToken cancellationToken = default);
    }
}
