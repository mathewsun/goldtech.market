using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Web.Models.Home;

namespace Nop.Web.Controllers
{
    public class PartnersController : Controller
    {
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        public PartnersController(
            IWorkContext workContext,
            ICustomerService customerService)
        {
            _workContext = workContext;
            _customerService = customerService;
        }

        public IActionResult Index(string name)
        {
            var ttt = name;

            var partnerUrl = string.Empty;

            switch (name)
            {
                case "lanta":
                    partnerUrl = "https://lanta.ru/metals/coins/zolotie-monety/";
                    break;
                case "sber":
                    partnerUrl = "https://www.sberbank.ru/proxy/services/coin-catalog/coins/5216-0060?region=38&condition=1";
                    break;
                case "rshb":
                    partnerUrl = "https://www.rshb.ru/natural/coins/";
                    break;
                default:
                    break;
            }

            PartnersModel model = new PartnersModel();

            var isAuthentificated = _customerService.IsRegistered(_workContext.CurrentCustomer);

            model.IsAuthetihicated = isAuthentificated;
            model.PartnerUrl = partnerUrl;


            return View(model);
        }
    }
}
