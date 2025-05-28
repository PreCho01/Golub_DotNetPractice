using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Models
{
    public class UserProfile
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public List<string>? Roles { get; set; }
        public Dictionary<string, string>? Preferences { get; set; }
    }
}
