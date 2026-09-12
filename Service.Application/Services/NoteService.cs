using Service.Application.DTOs.Note;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class NoteService : BaseService<Note, NoteGetDto, NotePostDto, NotePutDto>, INoteService
    {
        private readonly ICenterProductRepository _centerProductRepository;
        private readonly ICurrentUserService _currentUser;

        public NoteService(INoteRepository repository, ICenterProductRepository centerProductRepository, ICurrentUserService currentUser)
            : base(repository)
        {
            _centerProductRepository = centerProductRepository;
            _currentUser = currentUser;
        }

        protected override NoteGetDto ToGetDTO(Note entity)
        {
            return new NoteGetDto
            {
                Id = entity.Id,
                Content = entity.Content,
                CenterProductId = entity.CenterProductId,
                CreatedBy = entity.createdBy,
                CenterProduct = entity.CenterProduct?.ToGetDto(),
                CreatedByUser = entity.CreatedByUser?.ToGetDto()
            };
        }

        protected override Note ToEntity(NotePostDto postDTO)
        {
            return new Note
            {
                Content = postDTO.Content,
                CenterProductId = postDTO.CenterProductId
            };
        }

        protected override void ApplyUpdate(Note entity, NotePutDto putDTO)
        {
            if (entity.createdBy != _currentUser.UserId)
                throw new HttpException("Only the user who created this note can edit it.", 403);

            entity.Content = putDTO.Content;
        }

        public override async Task<NoteGetDto> AddAsync(NotePostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _centerProductRepository.Exists(postDTO.CenterProductId, cancellationToken))
                throw new BadRequestException("Center product not found.");

            return await base.AddAsync(postDTO, cancellationToken);
        }
    }
}
