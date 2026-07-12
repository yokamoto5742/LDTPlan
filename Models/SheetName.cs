using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LDTPlan.Models;

[Table("sheet_names")]
public class SheetName
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("main_disease_id")] public int? MainDiseaseId { get; set; }
    [Column("name")] public string? Name { get; set; }
}
