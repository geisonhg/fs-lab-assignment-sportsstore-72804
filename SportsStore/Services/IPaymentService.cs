using SportsStore.Models;
using Stripe.Checkout;

namespace SportsStore.Services {

    public interface IPaymentService {
        Task<Session> CreateCheckoutSessionAsync(Cart cart, string successUrl, string cancelUrl);
        Task<Session> GetSessionAsync(string sessionId);
    }
}
