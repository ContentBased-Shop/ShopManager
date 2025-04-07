using Manager.Models;
using System.Linq;
using System.Web.Mvc;

namespace Manager.Controllers
{
    public class HomeController : Controller
    {
        SHOPDataContext data = new SHOPDataContext("Data Source=ACERNITRO5;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
                  "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");
        #region TRANG-CHU
        public ActionResult Dashboard()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        #endregion

        #region DANGNHAP
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View("Index");
            }

            // Tìm người dùng với tên đăng nhập và vai trò Admin
            var user = data.NguoiDungs.FirstOrDefault(u => 
                u.TenDangNhap == username && 
                u.VaiTro == "Admin" && 
                u.TrangThai == "HoatDong");

            if (user != null)
            {
                // Kiểm tra mật khẩu (trong thực tế nên sử dụng hash)
                if (user.MatKhauHash == password)
                {
                    // Lưu thông tin người dùng vào Session
                    Session["UserID"] = user.MaNguoiDung;
                    Session["UserName"] = user.HoTen;
                    Session["Role"] = user.VaiTro;

                    // Chuyển hướng đến trang Dashboard
                    return RedirectToAction("Dashboard");
                }
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
            return View("Index");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index");
        }
        #endregion

        #region HANGHOA
        public ActionResult DanhSachHangHoa()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        public ActionResult TaoHangHoa()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        #endregion

        #region KHACH-HANG
        public ActionResult DanhSachKhachHang()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        public ActionResult ChiTietKhachHang()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        public ActionResult CapNhatKhachHang()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        #endregion

        #region DonBanHang
        public ActionResult DanhSachDonBanHang()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        public ActionResult ChiTietDonBanHang()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        #endregion
    }
}
