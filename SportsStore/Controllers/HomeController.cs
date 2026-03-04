using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;
using SportsStore.Models.ViewModels;

namespace SportsStore.Controllers {
    public class HomeController : Controller {
        private readonly IStoreRepository repository;
        private readonly ILogger<HomeController> logger;
        public int PageSize = 4;

        public HomeController(IStoreRepository repo, ILogger<HomeController> logger) {
            repository = repo;
            this.logger = logger;
        }

        public ViewResult Index(int productPage = 1) {
            logger.LogInformation(
                "Product listing requested — Page: {Page}, PageSize: {PageSize}",
                productPage, PageSize);

            var totalItems = repository.Products.Count();

            var viewModel = new ProductsListViewModel {
                Products = repository.Products
                    .OrderBy(p => p.ProductID)
                    .Skip((productPage - 1) * PageSize)
                    .Take(PageSize),
                PagingInfo = new PagingInfo {
                    CurrentPage = productPage,
                    ItemsPerPage = PageSize,
                    TotalItems = totalItems
                }
            };

            logger.LogDebug(
                "Returning {ProductCount} products out of {TotalItems} total",
                viewModel.Products.Count(), totalItems);

            return View(viewModel);
        }
    }
}
