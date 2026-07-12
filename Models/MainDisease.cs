using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LDTPlan.Models;

[Table("main_diseases")]
public class MainDisease
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("name")] public string? Name { get; set; }
}
