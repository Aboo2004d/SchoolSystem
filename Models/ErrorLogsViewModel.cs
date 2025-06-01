using SchoolSystem.Data;
using System;
namespace SchoolSystem.Models
{
    public class ErrorLogsViewModel
    {
        public List<ErrorLog> Logs { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
    
}
