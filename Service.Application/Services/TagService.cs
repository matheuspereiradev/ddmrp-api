using Service.Application.DTOs.Tag;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class TagService : BaseService<Tag, TagGetDto, TagPostDto, TagPutDto>, ITagService
    {
        public TagService(ITagRepository repository) : base(repository)
        {
        }

        protected override TagGetDto ToGetDTO(Tag entity) => entity.ToGetDto();

        protected override Tag ToEntity(TagPostDto postDTO)
        {
            return new Tag
            {
                Name = postDTO.Name,
                Description = postDTO.Description
            };
        }

        protected override void ApplyUpdate(Tag entity, TagPutDto putDTO)
        {
            entity.Name = putDTO.Name;
            entity.Description = putDTO.Description;
        }
    }
}
