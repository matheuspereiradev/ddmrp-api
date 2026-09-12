using Service.Application.DTOs.Tag;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface ITagService : IBaseService<Tag, TagGetDto, TagPostDto, TagPutDto>
    {
    }
}
