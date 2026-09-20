using Service.Application.DTOs.Note;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Application.Services
{
    public class NoteService : BaseService<Note, NoteGetDto, NotePostDto, NotePutDto>, INoteService
    {
        private readonly INoteRepository _noteRepository;
        private readonly ICenterProductRepository _centerProductRepository;
        private readonly ICurrentUserService _currentUser;

        public NoteService(INoteRepository repository, ICenterProductRepository centerProductRepository, ICurrentUserService currentUser)
            : base(repository)
        {
            _noteRepository = repository;
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
                CreatedAt = entity.createdAt,
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

        public async Task<PagedList<NoteGetDto>> GetByCenterProductAsync(int idCenterProduct, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (!await _centerProductRepository.Exists(idCenterProduct, cancellationToken))
                throw new NotFoundException("Center product not found.");

            var paged = await _noteRepository.GetByCenterProductAsync(idCenterProduct, pageNumber, pageSize, cancellationToken);
            var items = paged.Select(ToGetDTO).ToList();
            return new PagedList<NoteGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }
    }
}
