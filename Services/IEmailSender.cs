namespace Hotel_MVC.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage);
    }
}