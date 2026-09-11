using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Service.Application.DTOs.User
{
    public class UserPostDto
    {
        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        [EmailAddress(ErrorMessage = "Invalid Email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Field {0} required.")]
        [MaxLength(100, ErrorMessage = "The field {0} has the maxlength 100")]
        public string Password { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Field {0} required.")]
        public int IdRole { get; set; }

    }
}
