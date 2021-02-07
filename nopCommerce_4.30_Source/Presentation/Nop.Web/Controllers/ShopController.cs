using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class ShopController : BasePluginController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}