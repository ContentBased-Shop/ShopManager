﻿using System.Web.Mvc;

namespace Manager.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult DanhSachHangHoa()
        {
            return View();
        }

        public ActionResult TaoHangHoa()
        {
            return View();
        }
        public ActionResult Dashboard()
        {
            return View();
        }
    }
}
