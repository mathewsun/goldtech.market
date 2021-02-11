using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Web.Factories;
using Nop.Web.Models.Home;

namespace Nop.Web.Controllers
{
    public partial class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        public HomeController(
            IProductService productService,
            IWorkContext workContext,
            ICustomerService customerService)
        {
            _productService = productService;
            _workContext = workContext;
            _customerService = customerService;            
        }
        public virtual IActionResult Index()
        {
            HomeModel model = new HomeModel();

            var isAuthentificated = _customerService.IsRegistered(_workContext.CurrentCustomer);

            model.IsAuthetihicated = isAuthentificated;

            var products = _productService.GetProductsByIds(new[] {46, 49, 48, 51, 61});

            foreach (var product in products)
            {
                switch (product.Id)
                {
                    case 46:
                        model.StGeorgeTheVictorious2019CoinCost = (double) product.Price;
                        model.StGeorgeTheVictorious2020CoinCost = (double) product.Price;
                        break;
                    case 49:
                        model.ChessCoinCost = (double) product.Price;
                        break;
                    case 48:
                        model.TheViennaPhilharmonicCoinCost = (double) product.Price;
                        break;
                    case 51:
                        model.CanadianMapleLeafCoinCost = (double) product.Price;
                        break;
                    case 61:
                        model.AustralianKangarooCoinCost = (double) product.ProductCost;
                        break;
                }
            }
            
            return View(model);
        }
    }
}