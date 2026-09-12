using Service.Application.DTOs.Reason;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class ReasonService : BaseService<Reason, ReasonGetDto, ReasonPostDto, ReasonPutDto>, IReasonService
    {
        private readonly IReasonRepository _reasonRepository;

        public ReasonService(IReasonRepository repository) : base(repository)
        {
            _reasonRepository = repository;
        }

        protected override ReasonGetDto ToGetDTO(Reason entity)
        {
            return new ReasonGetDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                IsFromSystem = entity.IsFromSystem
            };
        }

        protected override Reason ToEntity(ReasonPostDto postDTO)
        {
            return new Reason
            {
                Name = postDTO.Name,
                Description = postDTO.Description,
                IsFromSystem = false
            };
        }

        protected override void ApplyUpdate(Reason entity, ReasonPutDto putDTO)
        {
            if (entity.IsFromSystem)
                throw new HttpException("System reasons cannot be edited.", 403);

            entity.Name = putDTO.Name;
            entity.Description = putDTO.Description;
        }

        public override async Task<ReasonGetDto> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _reasonRepository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                throw new NotFoundException("Not found");

            if (entity.IsFromSystem)
                throw new HttpException("System reasons cannot be deleted.", 403);

            return await base.DeleteAsync(id, cancellationToken);
        }
    }
}
