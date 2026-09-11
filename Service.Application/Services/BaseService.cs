using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Application.Services
{
    public abstract class BaseService<TEntity, TGetDTO, TPostDTO, TPutDTO> : IBaseService<TEntity, TGetDTO, TPostDTO, TPutDTO>
    where TEntity : BaseEntity
    {
        protected readonly IBaseRepository<TEntity> _repository;

        protected BaseService(IBaseRepository<TEntity> repository)
        {
            _repository = repository;
        }

        protected abstract TGetDTO ToGetDTO(TEntity entity);
        protected abstract TEntity ToEntity(TPostDTO postDTO);
        protected abstract void ApplyUpdate(TEntity entity, TPutDTO putDTO);

        public virtual async Task<TGetDTO> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                throw new NotFoundException("Not found");
            return ToGetDTO(entity);
        }

        public virtual async Task<PagedList<TGetDTO>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _repository.GetAllAsync(pageNumber, pageSize, cancellationToken);
            var items = paged.Select(ToGetDTO).ToList();
            return new PagedList<TGetDTO>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }

        public virtual async Task<TGetDTO> AddAsync(TPostDTO postDTO, CancellationToken cancellationToken = default)
        {
            var entity = ToEntity(postDTO);
            var created = await _repository.AddAsync(entity, cancellationToken);
            return ToGetDTO(created);
        }

        public virtual async Task<TGetDTO> UpdateAsync(int id, TPutDTO putDTO, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                throw new NotFoundException("Not found");

            ApplyUpdate(entity, putDTO);
            var updated = await _repository.UpdateAsync(entity, cancellationToken);
            return ToGetDTO(updated);
        }

        public virtual async Task<TGetDTO> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var deleted = await _repository.DeleteAsync(id, cancellationToken);
            if (deleted == null)
                throw new NotFoundException("Not found");
            return ToGetDTO(deleted);
        }
    }
}
