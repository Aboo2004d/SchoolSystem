namespace SchoolSystem.Models{
    public class LoginViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string UserName { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string Password { get; set; }
    }
}
