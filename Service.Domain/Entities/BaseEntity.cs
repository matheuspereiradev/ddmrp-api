using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Domain.Entities
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime? updatedAt { get; set; }
        public DateTime? deletedAt { get; set; }
        public int createdBy { get; set; }
        public int updatedBy { get; set; }
        public int deletedBy { get; set; }
    }
}
