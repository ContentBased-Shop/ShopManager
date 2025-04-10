using Manager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Configuration;
using Newtonsoft.Json.Linq;
using System.Web.Configuration;

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
            var user = data.NhanViens.FirstOrDefault(u => 
                u.TenDangNhap == username && 
                u.TrangThai == "HoatDong");

            if (user != null)
            {
                // Kiểm tra mật khẩu (trong thực tế nên sử dụng hash)
                if (user.MatKhau == password)
                {
                    // Lưu thông tin người dùng vào Session
                    Session["UserID"] = user.MaNhanVien;
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

        #region DanhMuc
        public ActionResult DanhSachDanhMuc()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            var danhMucs = data.DanhMucs.ToList(); 
            return View(danhMucs);
        }
        public ActionResult TaoDanhMuc()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        #endregion

        #region ThuongHieu
        public ActionResult DanhSachThuongHieu()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            var thuongHieus = data.ThuongHieus.ToList();
            return View(thuongHieus);
        }

        public ActionResult TaoThuongHieu()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public JsonResult XoaMotThuongHieu(string id)
        {
            var thuongHieu = data.ThuongHieus.FirstOrDefault(t => t.MaThuongHieu == id);

            if (thuongHieu == null)
            {
                return Json(new { success = false, message = "Thương hiệu không tồn tại" });
            }

            var coSanPham = data.HangHoas.Any(h => h.MaThuongHieu == id);
            if (coSanPham)
            {
                return Json(new { success = false, reason = "coSanPham" });
            }

            data.ThuongHieus.DeleteOnSubmit(thuongHieu);
            data.SubmitChanges();

            return Json(new { success = true });
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
            var danhSachHangHoa = data.HangHoas.ToList();

            return View(danhSachHangHoa);
        }
        public ActionResult DSBienTheHangHoa(string maHangHoa)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
             // Lấy danh sách biến thể dựa trên mã hàng hóa
            var bienTheList = data.BienTheHangHoas
                                 .Where(bt => bt.MaHangHoa == maHangHoa)
                                 .ToList();

            var hangHoa = data.HangHoas.FirstOrDefault(hh => hh.MaHangHoa == maHangHoa);

            if (hangHoa != null)
            {
                ViewBag.TenHangHoa = hangHoa.TenHangHoa;
            }
            else
            {
                ViewBag.TenHangHoa = "Không xác định";
            }


            return View(bienTheList);
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
            var khachHangs = data.KhachHangs.ToList();
            return View(khachHangs);
        }
        public ActionResult DiaChiKhach(string id)
        {
            var diaChiList = data.DiaChiKhachHangs
                                 .Where(dc => dc.MaKhachHang == id)
                                 .ToList();

            ViewBag.TenKhachHang = data.KhachHangs
                                       .Where(k => k.MaKhachHang == id)
                                       .Select(k => k.HoTen)
                                       .FirstOrDefault() ?? "Không xác định";

            return View(diaChiList);
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
            var donHangs = data.DonHangs.ToList();  // data là DbContext
            return View(donHangs);
        }
        public ActionResult ChiTietDonBanHang(string id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }

            var donHang = data.DonHangs.FirstOrDefault(d => d.MaDonHang == id);
            var chiTiet = data.ChiTietDonHangs.Where(c => c.MaDonHang == id).ToList();
            var thanhToan = data.ThanhToans.FirstOrDefault(t => t.MaDonHang == id);
            var giaoHang = data.GiaoHangs.FirstOrDefault(g => g.MaDonHang == id);

            var viewModel = new ChiTietDonHangViewModel
            {
                DonHang = donHang,
                ChiTietDonHangs = chiTiet,
                ThanhToan = thanhToan,
                GiaoHang = giaoHang
            };

            return View(viewModel);
        }

        #endregion

        #region DonNhapHang
        public ActionResult DanhSachNhapHang()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            var nhapHangs = data.NhapHangs.ToList(); 
            return View(nhapHangs);
        }
        public ActionResult ChiTietNhapHang(string id)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            // Lấy thông tin phiếu nhập
            var nhapHang = data.NhapHangs.FirstOrDefault(n => n.MaNhapHang == id);
            if (nhapHang == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách chi tiết nhập hàng theo mã phiếu nhập
            var chiTiet = data.ChiTietNhapHangs.Where(c => c.MaNhapHang == id).ToList();

            ViewBag.NhapHang = nhapHang;
            return View(chiTiet);
        }
        #endregion

        #region Voucher
        public ActionResult DanhSachVoucher()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            var vouchers = data.Vouchers.ToList(); 
            return View(vouchers);
        }
        public ActionResult TaoVoucher()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        #endregion

        public ActionResult DanhSachNhanVien()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            var nhanViens = data.NhanViens.ToList();
            return View(nhanViens);
        }
        public ActionResult DanhSachNhaCungCap()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            var nhaCungCaps = data.NhaCungCaps.ToList(); 
            return View(nhaCungCaps);
        }

    }
}
