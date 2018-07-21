using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplicationComosDbSearch.Models;

namespace WebApplicationComosDbSearch.Controllers
{
    public class SearchController : Controller
    {
        public ActionResult Search(string phrase)
        {
            phrase = string.IsNullOrEmpty(phrase) ? "*" : phrase;

            var result = SearchRepository<Product>.SearchPhrase(phrase);

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}