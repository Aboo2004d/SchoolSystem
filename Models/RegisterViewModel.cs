using System.ComponentModel.DataAnnotations;
namespace SchoolSystem.Models{
    public class RegisterViewModel
    {
        public string FullName { get; set; }

        [EmailAddress(ErrorMessage = "Email is not valid.")]
        public string Email { get; set; }

        //[Phone(ErrorMessage = "Phone number is not valid.")]
        public int Phone { get; set; }
    }
}