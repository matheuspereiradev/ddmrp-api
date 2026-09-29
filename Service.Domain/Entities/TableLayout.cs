using System;
using System.Collections.Generic;

namespace Service.Domain.Entities
{
    public class TableLayout
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string TableName { get; set; }
        public List<TableLayoutColumn> Columns { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
