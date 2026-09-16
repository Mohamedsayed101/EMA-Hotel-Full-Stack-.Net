namespace Hotel_MVC.ViewModels.AccountVM
{
    public class ConfirmEmailVM
    {
        public string UserId { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;
    }
}