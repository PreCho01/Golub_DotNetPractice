using System.Collections.Generic;

namespace WorkerService.Models
{
    public class StudentProfile
    {
        public int StudentId { get; set; }
        public string? Name { get; set; }
        public string? Department { get; set; }

        public List<string>? EnrolledCourses { get; set; }
        public Dictionary<string, int>? SubjectScores { get; set; }
    }
}
