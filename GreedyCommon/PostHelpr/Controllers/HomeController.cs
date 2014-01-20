using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PostHelpr.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "欢迎使用 ASP.NET MVC!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }


        public ActionResult Save(string username, string userpwd)
        {
            return View();
            // <input type="file" name="imgfile" />   <input type="file" name="icofile" />
        }
        [HttpPost]
        public JsonResult Upload(string username, string userpwd, HttpPostedFileBase imgfile, HttpPostedFileBase icofile)
        {
            imgfile.SaveAs(Server.MapPath("~/cc.jpg"));
            icofile.SaveAs(Server.MapPath("~/cd.jpg"));
            return new JsonResult()
            {
                ContentEncoding = System.Text.Encoding.UTF8,
                ContentType = "application/json",
                Data = new
                {
                    name = username,
                    pwd = userpwd,
                    img = imgfile.FileName,
                    ico = icofile.FileName
                },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
        [HttpPost]
        public JsonResult PSave(string username, string userpwd)
        {
            return new JsonResult()
             {
                 ContentEncoding = System.Text.Encoding.UTF8,
                 ContentType = "application/json",
                 Data = new
                 {
                     name = username,
                     pwd = userpwd
                 },
                 JsonRequestBehavior = JsonRequestBehavior.AllowGet
             };
        }
    }
}
