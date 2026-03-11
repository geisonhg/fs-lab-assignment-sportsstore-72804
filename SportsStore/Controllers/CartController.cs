using Microsoft.AspNetCore.Mvc;
using SportsStore.Infrastructure;
using SportsStore.Models;

namespace SportsStore.Controllers {

    public class CartController : Controller {
        private readonly IStoreRepository repository;
        private readonly ILogger<CartController> logger;

        public CartController(IStoreRepository repo, ILogger<CartController> logger) {
            repository = repo;
            this.logger = logger;
        }

        public IActionResult Index() {
            var cart = HttpContext.Session.GetJson<Cart>("Cart") ?? new Cart();
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(long productId, string returnUrl = "/") {
            var product = repository.Products
                .FirstOrDefault(p => p.ProductID == productId);

            if (product == null) {
                logger.LogWarning("AddToCart called with unknown ProductId: {ProductId}", productId);
                return NotFound();
            }

            var cart = HttpContext.Session.GetJson<Cart>("Cart") ?? new Cart();
            cart.AddItem(product, 1);
            HttpContext.Session.SetJson("Cart", cart);

            logger.LogInformation(
                "Product added to cart — ProductId: {ProductId}, Name: {Name}, CartTotal: {Total:C}",
                product.ProductID, product.Name, cart.ComputeTotalValue());

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveFromCart(long productId) {
            var product = repository.Products
                .FirstOrDefault(p => p.ProductID == productId);

            if (product != null) {
                var cart = HttpContext.Session.GetJson<Cart>("Cart") ?? new Cart();
                cart.RemoveLine(product);
                HttpContext.Session.SetJson("Cart", cart);

                logger.LogInformation(
                    "Product removed from cart — ProductId: {ProductId}, Name: {Name}",
                    product.ProductID, product.Name);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(long productId, int quantity) {
            var product = repository.Products
                .FirstOrDefault(p => p.ProductID == productId);

            if (product != null) {
                var cart = HttpContext.Session.GetJson<Cart>("Cart") ?? new Cart();
                cart.UpdateQuantity(product, quantity);
                HttpContext.Session.SetJson("Cart", cart);

                logger.LogInformation(
                    "Cart quantity updated — ProductId: {ProductId}, Name: {Name}, NewQuantity: {Quantity}",
                    product.ProductID, product.Name, quantity);
            }

            return RedirectToAction("Index");
        }
    }
}
