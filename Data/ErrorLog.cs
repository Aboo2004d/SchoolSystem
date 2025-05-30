using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class ErrorLog
{
    public int Id { get; set; }

    public string Message { get; set; } = null!;

    public string? StackTrace { get; set; }

    public string? Source { get; set; }

    public DateTime LoggedAt { get; set; }
}
