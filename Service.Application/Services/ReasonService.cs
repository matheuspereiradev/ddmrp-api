using Service.Application.DTOs.Reason;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class ReasonService : BaseService<Reason, ReasonGetDto, ReasonPostDto, ReasonPutDto>, IReasonService
    {
        private readonly IReasonRepository _reasonRepository;
        private readonly IReasonGroupRepository _reasonGroupRepository;

        public ReasonService(IReasonRepository repository, IReasonGroupRepository reasonGroupRepository) : base(repository)
        {
            _reasonRepository = repository;
            _reasonGroupRepository = reasonGroupRepository;
        }

        protected override ReasonGetDto ToGetDTO(Reason entity) => entity.ToGetDto();

        protected override Reason ToEntity(ReasonPostDto postDTO)
        {
            return new Reason
            {
                Name = postDTO.Name,
                Description = postDTO.Description,
                IdReasonGroup = postDTO.IdReasonGroup,
                IsFromSystem = false
            };
        }

        protected override void ApplyUpdate(Reason entity, ReasonPutDto putDTO)
        {
            if (entity.IsFromSystem)
                throw new HttpException("System reasons cannot be edited.", 403);

            entity.Name = putDTO.Name;
            entity.Description = putDTO.Description;
            entity.IdReasonGroup = putDTO.IdReasonGroup;
        }

        public override async Task<ReasonGetDto> AddAsync(ReasonPostDto postDTO, CancellationToken cancellationToken = default)
        {
            await ValidateReasonGroupAsync(postDTO.IdReasonGroup, cancellationToken);
            return await base.AddAsync(postDTO, cancellationToken);
        }

        public override async Task<ReasonGetDto> UpdateAsync(int id, ReasonPutDto putDTO, CancellationToken cancellationToken = default)
        {
            await ValidateReasonGroupAsync(putDTO.IdReasonGroup, cancellationToken);
            return await base.UpdateAsync(id, putDTO, cancellationToken);
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

        private async Task ValidateReasonGroupAsync(int idReasonGroup, CancellationToken cancellationToken)
        {
            if (!await _reasonGroupRepository.Exists(idReasonGroup, cancellationToken))
                throw new BadRequestException("Reason group not found.");
        }
    }
}
