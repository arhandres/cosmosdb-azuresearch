using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApplicationComosDbSearch.Models;

namespace WebApplicationComosDbSearch.Controllers
{
    public class ProductController : Controller
    {
        [HttpPost]
        public async Task<ActionResult> Create(Product product)
        {
            var document = await DocumentDBRepository<Product>.CreateItemAsync(product);

            return Json(new
            {
                Success = true,
                Id = document.Id
            }, JsonRequestBehavior.AllowGet);
        }
    }
}