using SchoolSystem.Data;
namespace SchoolSystem.Controllers{
    public class ErrorLoggerService : IErrorLoggerService
    {
        private readonly SystemSchoolDbContext _context;

        public ErrorLoggerService(SystemSchoolDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(Exception ex, string source)
        {
            var error = new ErrorLog
            {
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                Source = source
            };

            _context.Add(error);
            await _context.SaveChangesAsync();
        }
        
    }
}