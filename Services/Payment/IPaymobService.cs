namespace Hotel_MVC.Services.Payment
{
    public interface IPaymobService
    {
        Task<string> CreateCardSavingCheckoutAsync(
            string userId,
            string email,
            string phoneNumber,
            string firstName,
            string lastName);
    }
}