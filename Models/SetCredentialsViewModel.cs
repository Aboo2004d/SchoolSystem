using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models{
    public class SetCredentialsViewModel
    {
        public string Email { get; set; }
        public string Role { get; set; }
        [Required]
        public string UserName { get; set; }
        public Guid IdUser { get; set; }
        public Guid School { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }
        public string name { get; set; }
    }
}
