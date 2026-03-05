using Microsoft.AspNetCore.Mvc;
using SportsStore.Infrastructure;
using SportsStore.Models;
using SportsStore.Services;

namespace SportsStore.Controllers {

    public class OrderController : Controller {
        private readonly StoreDbContext context;
        private readonly IPaymentService paymentService;
        private readonly ILogger<OrderController> logger;

        public OrderController(StoreDbContext ctx, IPaymentService paymentService,
            ILogger<OrderController> logger) {
            context = ctx;
            this.paymentService = paymentService;
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult Checkout() {
            var cart = HttpContext.Session.GetJson<Cart>("Cart") ?? new Cart();
            if (!cart.Lines.Any()) {
                return RedirectToAction("Index", "Cart");
            }
            return View(new Order());
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order) {
            var cart = HttpContext.Session.GetJson<Cart>("Cart") ?? new Cart();

            if (!cart.Lines.Any()) {
                ModelState.AddModelError("", "Your cart is empty");
            }

            if (!ModelState.IsValid) {
                return View(order);
            }

            HttpContext.Session.SetJson("PendingOrder", order);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/Order/PaymentSuccess?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl  = $"{baseUrl}/Order/PaymentCancelled";

            try {
                logger.LogInformation(
                    "Initiating Stripe checkout for customer: {Name}", order.Name);

                var session = await paymentService.CreateCheckoutSessionAsync(
                    cart, successUrl, cancelUrl);

                return Redirect(session.Url);
            }
            catch (Exception ex) {
                logger.LogError(ex,
                    "Failed to create Stripe checkout session for customer: {Name}", order.Name);
                ModelState.AddModelError("", "Payment service is temporarily unavailable. Please try again.");
                return View(order);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(string session_id) {
            if (string.IsNullOrEmpty(session_id)) {
                return RedirectToAction("Index", "Home");
            }

            try {
                var stripeSession = await paymentService.GetSessionAsync(session_id);

                if (stripeSession.PaymentStatus != "paid") {
                    logger.LogWarning(
                        "PaymentSuccess called but payment not complete — SessionId: {SessionId}, Status: {Status}",
                        session_id, stripeSession.PaymentStatus);
                    return RedirectToAction("PaymentFailed");
                }

                var pendingOrder = HttpContext.Session.GetJson<Order>("PendingOrder");
                var cart = HttpContext.Session.GetJson<Cart>("Cart") ?? new Cart();

                if (pendingOrder == null) {
                    logger.LogWarning(
                        "No pending order found in session for SessionId: {SessionId}", session_id);
                    return RedirectToAction("Index", "Home");
                }

                pendingOrder.StripeSessionId = stripeSession.Id;
                pendingOrder.PaymentIntentId = stripeSession.PaymentIntentId;

                foreach (var line in cart.Lines) {
                    pendingOrder.Lines.Add(new OrderLine {
                        ProductId  = line.Product.ProductID,
                        Quantity   = line.Quantity,
                        UnitPrice  = line.Product.Price
                    });
                }

                context.Orders.Add(pendingOrder);
                await context.SaveChangesAsync();

                HttpContext.Session.Remove("Cart");
                HttpContext.Session.Remove("PendingOrder");

                logger.LogInformation(
                    "Order saved successfully — OrderId: {OrderId}, Customer: {Name}, PaymentIntent: {PaymentIntentId}",
                    pendingOrder.OrderId, pendingOrder.Name, pendingOrder.PaymentIntentId);

                return View(pendingOrder);
            }
            catch (Exception ex) {
                logger.LogError(ex,
                    "Error processing payment success for SessionId: {SessionId}", session_id);
                return RedirectToAction("PaymentFailed");
            }
        }

        [HttpGet]
        public IActionResult PaymentCancelled() {
            logger.LogInformation("Payment cancelled by customer — returning to cart");
            return View();
        }

        [HttpGet]
        public IActionResult PaymentFailed() {
            logger.LogWarning("Payment failed or could not be verified");
            return View();
        }
    }
}
