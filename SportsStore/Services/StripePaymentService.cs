using SportsStore.Models;
using Stripe;
using Stripe.Checkout;

namespace SportsStore.Services {

    public class StripePaymentService : IPaymentService {
        private readonly ILogger<StripePaymentService> logger;

        public StripePaymentService(IConfiguration configuration,
            ILogger<StripePaymentService> logger) {
            this.logger = logger;
            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"]
                ?? throw new InvalidOperationException(
                    "Stripe SecretKey is not configured. Use user-secrets or environment variables.");
        }

        public async Task<Session> CreateCheckoutSessionAsync(
            Cart cart, string successUrl, string cancelUrl) {

            logger.LogInformation(
                "Creating Stripe checkout session for {LineCount} line(s), total {Total:C}",
                cart.Lines.Count, cart.ComputeTotalValue());

            var lineItems = cart.Lines.Select(line => new SessionLineItemOptions {
                PriceData = new SessionLineItemPriceDataOptions {
                    Currency = "eur",
                    ProductData = new SessionLineItemPriceDataProductDataOptions {
                        Name = line.Product.Name,
                        Description = line.Product.Description
                    },
                    UnitAmount = (long)(line.Product.Price * 100)
                },
                Quantity = line.Quantity
            }).ToList();

            var options = new SessionCreateOptions {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            logger.LogInformation(
                "Stripe session created — SessionId: {SessionId}", session.Id);

            return session;
        }

        public async Task<Session> GetSessionAsync(string sessionId) {
            logger.LogInformation(
                "Retrieving Stripe session — SessionId: {SessionId}", sessionId);

            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            logger.LogInformation(
                "Stripe session status: {Status}, PaymentStatus: {PaymentStatus}",
                session.Status, session.PaymentStatus);

            return session;
        }
    }
}
