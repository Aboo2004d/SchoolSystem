using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Data;

public partial class Menegar
{
    
    public int Id { get; set; }

    [Display(Name="Name")]
    public string Name { get; set; } = null!;

    [Display(Name="Phone")]
    public int Phone { get; set; }

    [Display(Name="Email Address")]
    [EmailAddress(ErrorMessage = "Email Address is not valid")]
    public string? Email { get; set; }
}
