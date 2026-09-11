using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int IdRole { get; set; }
        public Role Role { get; set; }
    }
}
