using Service.Application.DTOs.CenterProduct;
using Service.Application.DTOs.User;

namespace Service.Application.DTOs.Note
{
    public class NoteGetDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int CenterProductId { get; set; }
        public int? CreatedBy { get; set; }
        public CenterProductGetDto? CenterProduct { get; set; }
        public UserGetDto? CreatedByUser { get; set; }
    }
}
