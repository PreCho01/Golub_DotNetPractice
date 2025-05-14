using System.ComponentModel.DataAnnotations.Schema;

namespace WorkerService.Models
{
    [Table("emp")]
    public class Emp
    {       
        public int Emp_id { get; set; }
        public string? Emp_name  { get; set; }
        public string? Emp_department { get; set; }
    }
}
