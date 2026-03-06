using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcCorePaginacionRegistros.Models
{
    [Table("DEPT")]
    public class Departamento
    {
        [Key]
        [Column("DEPT_NO")]
        public int IdDepartamento { get; set; }

        [Required]
        [StringLength(100)]
        [Column("DNOMBRE")]
        public string Nombre { get; set; }

        [Column("LOC")]
        public string? Localidad { get; set; }

        public ICollection<Empleado>? Empleados { get; set; }
    }
}
