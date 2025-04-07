using System.Web.Mvc;

namespace Manager.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        #region TRANG-CHU
        public ActionResult Dashboard()
        {
            return View();
        }
        #endregion

        #region DANGNHAP
        public ActionResult Index()
        {
            return View();
        }
        #endregion

        #region HANGHOA
        public ActionResult DanhSachHangHoa()
        {
            return View();
        }

        public ActionResult TaoHangHoa()
        {
            return View();
        }
        #endregion

        #region KHACH-HANG
        public ActionResult DanhSachKhachHang()
        {
            return View();
        }
        public ActionResult ChiTietKhachHang()
        {
            return View();
        }
        public ActionResult CapNhatKhachHang()
        {
            return View();
        }
        #endregion

        #region DonBanHang
        public ActionResult DanhSachDonBanHang()
        {
            return View();
        }
        public ActionResult ChiTietDonBanHang()
        {
            return View();
        }
        #endregion
    }
}
