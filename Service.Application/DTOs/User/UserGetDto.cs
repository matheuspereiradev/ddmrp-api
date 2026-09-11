using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Application.DTOs.User
{
    public class UserGetDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int IdRole { get; set; }
    }
}
