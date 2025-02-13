using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Json.Controllers
{
    public class UmlController : Controller
    {
        public IActionResult VistaGraf()
        {
            return View();
        }

    }
}
