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
using System.Web.Helpers;

namespace Manager.Controllers
{
    public class HomeController : Controller
    {
        SHOPDataContext data = new SHOPDataContext("Data Source=ACERNITRO5;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
                  "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");

        // Khai báo thông tin email
        private readonly string _emailAddress = "managertask34@gmail.com";
        private readonly string _emailPassword = "veaq dwhq oico jlzc";
        private readonly string _emailDisplayName = "PrimeTech Admin";
        private readonly string _emailHost = "smtp.gmail.com";
        private readonly int _emailPort = 587;
        
        #region TRANG-CHU
        public ActionResult Dashboard()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }

            // Lấy dữ liệu thống kê
            ViewBag.TongSanPham = data.HangHoas.Count();
            ViewBag.DonHangMoi = data.DonHangs.Count(d => d.NgayTao.Value.Date == DateTime.Today);
            ViewBag.TongKhachHang = data.KhachHangs.Count();
            ViewBag.DoanhThu = Math.Max(data.DonHangs
                .Where(d => d.TrangThaiDonHang == "HoanThanh")
                .Sum(d => d.TongTien) ?? 0.0, 0.0);


            // Lấy danh sách đơn hàng gần đây kèm theo thông tin khách hàng
            var donHangGanDay = data.DonHangs
                .OrderByDescending(d => d.NgayTao)
                .Take(4)
                .ToList();
                
            // Lấy thông tin khách hàng cho mỗi đơn hàng
            foreach (var donHang in donHangGanDay)
            {
                donHang.KhachHang = data.KhachHangs.FirstOrDefault(k => k.MaKhachHang == donHang.MaKhachHang);
            }
            
            ViewBag.DonHangGanDay = donHangGanDay;

            // Lấy sản phẩm bán chạy
            var sanPhamBanChay = data.ChiTietDonHangs
     .GroupBy(ct => ct.BienTheHangHoa.HangHoa)
     .Select(g => new SanPhamBanChayModel
     {
         TenHangHoa = g.Key.TenHangHoa,
         SoLuongBan = g.Sum(ct => ct.SoLuong) ?? 0,
         DoanhThu = (double)g.Sum(ct => ct.SoLuong * ct.DonGia)
     })
     .OrderByDescending(x => x.SoLuongBan)
     .Take(4)
     .ToList();
            ViewBag.SanPhamBanChay = sanPhamBanChay;

            return View();
        }
        #endregion

        #region DANGNHAP
        public ActionResult Index()
        {
            return View();
        }
        public static string EncryptPassword(string plainText, string key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }
        //Giai ma
        public static string DecryptPassword(string encryptedText, string key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    try
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                    catch (CryptographicException ex)
                    {
                        throw new Exception("Giải mã thất bại: Dữ liệu không hợp lệ hoặc khóa sai.", ex);
                    }
                }
            }
        }
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View("Index");
            }

            // Tìm người dùng với tên đăng nhập
            var user = data.NhanViens.FirstOrDefault(u => 
                u.TenDangNhap == username && 
                u.TrangThai == "HoatDong");

            if (user != null)
            {
                try
                {
                    // Giải mã mật khẩu và so sánh
                    string decryptedPassword = DecryptPassword(user.MatKhau, "mysecretkey");
                    
                    if (decryptedPassword == password)
                    {
                        // Kiểm tra thời gian hết hạn mật khẩu tạm thời
                        if (user.ExpiryTime != null)
                        {
                            // Nếu đã quá thời hạn 5 phút
                            if (user.ExpiryTime < DateTime.Now)
                            {
                                ViewBag.Error = "Mật khẩu tạm thời đã hết hạn. Vui lòng yêu cầu mật khẩu mới.";
                                return View("Index");
                            }
                            
                            // Mật khẩu tạm thời còn hạn, yêu cầu đổi mật khẩu
                            Session["UserID"] = user.MaNhanVien;
                            Session["UserName"] = user.HoTen;
                            Session["Role"] = user.VaiTro;
                            Session["RequirePasswordChange"] = true;
                            return RedirectToAction("ChangePasswordRequired");
                        }

                        // Lưu thông tin người dùng vào Session
                        Session["UserID"] = user.MaNhanVien;
                        Session["UserName"] = user.HoTen;
                        Session["Role"] = user.VaiTro;

                        // Chuyển hướng đến trang Dashboard
                        return RedirectToAction("Dashboard");
                    }
                }
                catch (Exception)
                {
                    // Xử lý lỗi giải mã (có thể là mật khẩu đã được lưu dạng text trước đó)
                    if (user.MatKhau == password)
                    {
                        // Kiểm tra thời gian hết hạn mật khẩu tạm thời
                        if (user.ExpiryTime != null)
                        {
                            // Nếu đã quá thời hạn 5 phút
                            if (user.ExpiryTime < DateTime.Now)
                            {
                                ViewBag.Error = "Mật khẩu tạm thời đã hết hạn. Vui lòng yêu cầu mật khẩu mới.";
                                return View("Index");
                            }
                            
                            // Mật khẩu tạm thời còn hạn, yêu cầu đổi mật khẩu
                            Session["UserID"] = user.MaNhanVien;
                            Session["UserName"] = user.HoTen;
                            Session["Role"] = user.VaiTro;
                            Session["RequirePasswordChange"] = true;
                            return RedirectToAction("ChangePasswordRequired");
                        }

                        // Lưu thông tin người dùng vào Session
                        Session["UserID"] = user.MaNhanVien;
                        Session["UserName"] = user.HoTen;
                        Session["Role"] = user.VaiTro;

                        // Chuyển hướng đến trang Dashboard
                        return RedirectToAction("Dashboard");
                    }
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

        [HttpPost]
        public JsonResult DoiMatKhau(string oldPassword, string newPassword)
        {
            try
            {
                // Kiểm tra người dùng đã đăng nhập chưa
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này" });
                }

                string maNhanVien = Session["UserID"].ToString();
                var nhanVien = data.NhanViens.FirstOrDefault(nv => nv.MaNhanVien == maNhanVien);

                if (nhanVien == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                // Kiểm tra mật khẩu cũ có đúng không
                string decryptedPassword;
                try
                {
                    decryptedPassword = DecryptPassword(nhanVien.MatKhau, "mysecretkey");
                }
                catch (Exception)
                {
                    // Xử lý trường hợp không giải mã được (có thể mật khẩu chưa được mã hóa)
                    decryptedPassword = nhanVien.MatKhau;
                }

                if (decryptedPassword != oldPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });
                }

                // Mã hóa mật khẩu mới
                string matKhauMoiMaHoa = EncryptPassword(newPassword, "mysecretkey");

                // Cập nhật mật khẩu mới
                nhanVien.MatKhau = matKhauMoiMaHoa;
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi đổi mật khẩu: " + ex.Message });
            }
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

        [HttpPost]
        public JsonResult ThemDanhMuc(string TenDanhMuc)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(TenDanhMuc))
                {
                    return Json(new { success = false, message = "Vui lòng nhập tên danh mục" });
                }

                // Tạo mã danh mục tự động
                string MaDanhMuc;
                do
                {
                    MaDanhMuc = "DM" + GenerateRandomString(6);
                } while (data.DanhMucs.Any(d => d.MaDanhMuc == MaDanhMuc));

                // Tạo danh mục mới
                var danhMuc = new DanhMuc
                {
                    MaDanhMuc = MaDanhMuc,
                    TenDanhMuc = TenDanhMuc
                };

                data.DanhMucs.InsertOnSubmit(danhMuc);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm danh mục: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaDanhMuc(string MaDanhMuc, string TenDanhMuc)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(MaDanhMuc) || string.IsNullOrEmpty(TenDanhMuc))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
                }

                // Tìm danh mục cần cập nhật
                var danhMuc = data.DanhMucs.FirstOrDefault(d => d.MaDanhMuc == MaDanhMuc);
                if (danhMuc == null)
                {
                    return Json(new { success = false, message = "Danh mục không tồn tại" });
                }

                // Cập nhật thông tin
                danhMuc.TenDanhMuc = TenDanhMuc;
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật danh mục: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaDanhMuc(string id)
        {
            try
            {
                // Tìm danh mục cần xóa
                var danhMuc = data.DanhMucs.FirstOrDefault(d => d.MaDanhMuc == id);
                if (danhMuc == null)
                {
                    return Json(new { success = false, message = "Danh mục không tồn tại" });
                }

                // Kiểm tra xem danh mục có đang được sử dụng trong bảng Hàng Hóa không
                var coSanPham = data.HangHoas.Any(h => h.MaDanhMuc == id);
                if (coSanPham)
                {
                    return Json(new { success = false, reason = "coSanPham" });
                }

                // Xóa danh mục
                data.DanhMucs.DeleteOnSubmit(danhMuc);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa danh mục: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuDanhMuc(List<string> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return Json(new { success = false, message = "Không có danh mục nào được chọn để xóa" });
                }

                // Kiểm tra và lưu danh sách danh mục có sản phẩm
                var danhMucCoSanPham = new List<string>();
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    var coSanPham = data.HangHoas.Any(h => h.MaDanhMuc == id);
                    if (coSanPham)
                    {
                        var tenDanhMuc = data.DanhMucs
                            .Where(d => d.MaDanhMuc == id)
                            .Select(d => d.TenDanhMuc)
                            .FirstOrDefault();
                        danhMucCoSanPham.Add($"{id} ({tenDanhMuc})");
                    }
                }

                // Nếu có danh mục đang được sử dụng
                if (danhMucCoSanPham.Any())
                {
                    var danhSachDM = string.Join("<br/>", danhMucCoSanPham);
                    return Json(new { 
                        success = false, 
                        reason = "coSanPham", 
                        message = danhSachDM
                    });
                }

                // Xóa các danh mục nếu không có vấn đề
                var danhMucCanXoa = data.DanhMucs.Where(d => ids.Contains(d.MaDanhMuc));
                if (danhMucCanXoa.Any())
                {
                    data.DanhMucs.DeleteAllOnSubmit(danhMucCanXoa);
                    data.SubmitChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục cần xóa" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa danh mục: " + ex.Message });
            }
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

        [HttpPost]
        public JsonResult TaoThuongHieu(string TenThuongHieu, string MoTa, HttpPostedFileBase Logo, bool TrangThai)
        {
            try
            {
                // Tạo mã thương hiệu tự động
                string MaThuongHieu;
                do
                {
                    MaThuongHieu = "TH" + GenerateRandomString(6);
                } while (data.ThuongHieus.Any(t => t.MaThuongHieu == MaThuongHieu));

                var thuongHieu = new ThuongHieu
                {
                    MaThuongHieu = MaThuongHieu,
                    TenThuongHieu = TenThuongHieu,
                    MoTa = MoTa,
                    TrangThai = TrangThai
                };

                if (Logo != null && Logo.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(Logo.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/img/thuonghieu"), fileName);
                    Logo.SaveAs(path);
                    thuongHieu.Logo = fileName;
                }

                data.ThuongHieus.InsertOnSubmit(thuongHieu);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm thương hiệu: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaThuongHieu(string MaThuongHieu, string TenThuongHieu, string MoTa, HttpPostedFileBase Logo, bool TrangThai)
        {
            try
            {
                var thuongHieu = data.ThuongHieus.FirstOrDefault(t => t.MaThuongHieu == MaThuongHieu);
                if (thuongHieu == null)
                {
                    return Json(new { success = false, message = "Thương hiệu không tồn tại" });
                }

                thuongHieu.TenThuongHieu = TenThuongHieu;
                thuongHieu.MoTa = MoTa;
                thuongHieu.TrangThai = TrangThai;

                if (Logo != null && Logo.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(Logo.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/img/thuonghieu"), fileName);
                    Logo.SaveAs(path);
                    thuongHieu.Logo = fileName;
                }

                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi chỉnh sửa thương hiệu: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuThuongHieu(string[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "Không có thương hiệu nào được chọn để xóa" });
                }

                // Kiểm tra và lưu danh sách thương hiệu có sản phẩm
                var thuongHieuCoSanPham = new List<string>();
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    var coSanPham = data.HangHoas.Any(h => h.MaThuongHieu == id);
                    if (coSanPham)
                    {
                        var tenThuongHieu = data.ThuongHieus
                            .Where(t => t.MaThuongHieu == id)
                            .Select(t => t.TenThuongHieu)
                            .FirstOrDefault();
                        thuongHieuCoSanPham.Add($"{id} ({tenThuongHieu})");
                    }
                }

                // Nếu có thương hiệu đang được sử dụng
                if (thuongHieuCoSanPham.Any())
                {
                    var danhSachTH = string.Join(", ", thuongHieuCoSanPham);
                    return Json(new { 
                        success = false, 
                        reason = "coSanPham", 
                        message = $"Không thể xóa vì các thương hiệu sau đang được sử dụng trong bảng Hàng Hóa: {danhSachTH}" 
                    });
                }

                // Nếu không có thương hiệu nào có sản phẩm, tiến hành xóa
                var thuongHieuCanXoa = data.ThuongHieus.Where(t => ids.Contains(t.MaThuongHieu));
                if (thuongHieuCanXoa.Any())
                {
                    data.ThuongHieus.DeleteAllOnSubmit(thuongHieuCanXoa);
                    data.SubmitChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy thương hiệu cần xóa" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa thương hiệu: " + ex.Message });
            }
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
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
                ViewBag.MaHangHoa = hangHoa.MaHangHoa;
            }
            else
            {
                ViewBag.TenHangHoa = "Không xác định";
                ViewBag.MaHangHoa = "";
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
            
            // Lấy danh sách thương hiệu và danh mục có trạng thái true
            ViewBag.DanhSachThuongHieu = data.ThuongHieus
                .Where(th => th.TrangThai == true)
                .ToList();
            
            ViewBag.DanhSachDanhMuc = data.DanhMucs
               
                .ToList();
                
            return View();
        }
        
        public ActionResult SuaHangHoa(string id)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }
            
            var hangHoa = data.HangHoas.FirstOrDefault(h => h.MaHangHoa == id);
            if (hangHoa == null)
            {
                return HttpNotFound();
            }
            
            // Lấy danh sách thương hiệu và danh mục có trạng thái true
            ViewBag.DanhSachThuongHieu = data.ThuongHieus
                .Where(th => th.TrangThai == true)
                .ToList();
            
            ViewBag.DanhSachDanhMuc = data.DanhMucs
                
                .ToList();
                
            return View(hangHoa);
        }

        [HttpPost]
        public JsonResult TaoHangHoa(string TenHangHoa, string MaDanhMuc, string MaThuongHieu, string MoTa, string MoTaDai, string TrangThai, HttpPostedFileBase HinhAnh)
        {
            try
            {
                // Tạo mã hàng hóa tự động
                string MaHangHoa;
                do
                {
                    MaHangHoa = "HH" + GenerateRandomString(6);
                } while (data.HangHoas.Any(h => h.MaHangHoa == MaHangHoa));

                // Xử lý hình ảnh nếu có
                string hinhAnhFileName = null;
                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    string extension = Path.GetExtension(HinhAnh.FileName);
                    hinhAnhFileName = Guid.NewGuid().ToString() + extension;
                    string filePath = Path.Combine(Server.MapPath("~/Content/img/hanghoa/"), hinhAnhFileName);
                    
                    // Đảm bảo thư mục tồn tại
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    
                    // Lưu file
                    HinhAnh.SaveAs(filePath);
                }

                var hangHoa = new HangHoa
                {
                    MaHangHoa = MaHangHoa,
                    TenHangHoa = TenHangHoa,
                    MaDanhMuc = string.IsNullOrEmpty(MaDanhMuc) ? null : MaDanhMuc,
                    MaThuongHieu = string.IsNullOrEmpty(MaThuongHieu) ? null : MaThuongHieu,
                    MoTa = MoTa,
                    MoTaDai = MoTaDai,
                    HinhAnh = hinhAnhFileName,
                    DanhGiaTrungBinh = 0,
                    NgayTao = DateTime.Now,
                    TrangThai = TrangThai
                };

                data.HangHoas.InsertOnSubmit(hangHoa);
                data.SubmitChanges();

                // Tính toán điểm tương đồng với các sản phẩm khác và lưu vào ContentBasedFiltering
                TinhDiemTuongDongChoHangHoa(MaHangHoa);

                return Json(new { success = true, maHangHoa = MaHangHoa });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm hàng hóa: " + ex.Message });
            }
        }

        // Hàm tính điểm tương đồng cho hàng hóa mới
        private void TinhDiemTuongDongChoHangHoa(string maHangHoaMoi)
        {
            try
            {
                // Lấy hàng hóa mới vừa thêm
                var hangHoaMoi = data.HangHoas.FirstOrDefault(h => h.MaHangHoa == maHangHoaMoi);
                if (hangHoaMoi == null) return;

                // Lấy tất cả hàng hóa khác để tính điểm tương đồng
                var danhSachHangHoa = data.HangHoas.Where(h => h.MaHangHoa != maHangHoaMoi).ToList();

                // Nếu không có sản phẩm nào khác để so sánh thì dừng lại
                if (!danhSachHangHoa.Any()) return;

                // Tính điểm tương đồng với từng sản phẩm
                foreach (var hangHoaKhac in danhSachHangHoa)
                {
                    // Đảm bảo MaHangHoa1 < MaHangHoa2 theo ràng buộc của bảng
                    string maHangHoa1 = maHangHoaMoi;
                    string maHangHoa2 = hangHoaKhac.MaHangHoa;

                    // Đổi chỗ nếu cần để thỏa mãn ràng buộc
                    if (String.Compare(maHangHoa1, maHangHoa2) > 0)
                    {
                        string temp = maHangHoa1;
                        maHangHoa1 = maHangHoa2;
                        maHangHoa2 = temp;
                    }

                    // Điểm tương đồng dựa trên 3 yếu tố: DanhMuc, ThuongHieu và MoTa
                    double diemTuongDong = 0;
                    double tongTrongSo = 0;

                    // Trọng số cho mỗi yếu tố
                    double trongSoDanhMuc = 0.3;
                    double trongSoThuongHieu = 0.3;
                    double trongSoMoTa = 0.4;

                    // 1. So sánh Danh Mục (30%)
                    if (!string.IsNullOrEmpty(hangHoaMoi.MaDanhMuc) && !string.IsNullOrEmpty(hangHoaKhac.MaDanhMuc))
                    {
                        if (hangHoaMoi.MaDanhMuc == hangHoaKhac.MaDanhMuc)
                        {
                            diemTuongDong += trongSoDanhMuc;
                        }
                        tongTrongSo += trongSoDanhMuc;
                    }

                    // 2. So sánh Thương Hiệu (30%)
                    if (!string.IsNullOrEmpty(hangHoaMoi.MaThuongHieu) && !string.IsNullOrEmpty(hangHoaKhac.MaThuongHieu))
                    {
                        if (hangHoaMoi.MaThuongHieu == hangHoaKhac.MaThuongHieu)
                        {
                            diemTuongDong += trongSoThuongHieu;
                        }
                        tongTrongSo += trongSoThuongHieu;
                    }

                    // 3. So sánh Mô Tả sử dụng TF-IDF (40%)
                    if (!string.IsNullOrEmpty(hangHoaMoi.MoTa) && !string.IsNullOrEmpty(hangHoaKhac.MoTa))
                    {
                        double diemMoTa = TinhDiemTuongDongVanBan(hangHoaMoi.MoTa, hangHoaKhac.MoTa);
                        diemTuongDong += diemMoTa * trongSoMoTa;
                        tongTrongSo += trongSoMoTa;
                    }

                    // Chuẩn hóa điểm tương đồng (nếu có ít nhất một yếu tố để so sánh)
                    double diemTuongDongChuanHoa = tongTrongSo > 0 ? diemTuongDong / tongTrongSo : 0;

                    // Kiểm tra xem đã có bản ghi tương đồng này chưa
                    var existingRecord = data.ContentBasedFilterings.FirstOrDefault(
                        c => (c.MaHangHoa1 == maHangHoa1 && c.MaHangHoa2 == maHangHoa2));

                    if (existingRecord != null)
                    {
                        // Nếu đã có thì cập nhật
                        existingRecord.DiemTuongDong = diemTuongDongChuanHoa;
                    }
                    else
                    {
                        // Nếu chưa có thì tạo mới
                        var contentBasedFiltering = new ContentBasedFiltering
                        {
                            MaHangHoa1 = maHangHoa1,
                            MaHangHoa2 = maHangHoa2,
                            DiemTuongDong = diemTuongDongChuanHoa
                        };

                        data.ContentBasedFilterings.InsertOnSubmit(contentBasedFiltering);
                    }
                }

                // Lưu các thay đổi vào cơ sở dữ liệu
                data.SubmitChanges();
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý lỗi ở đây nếu cần
                System.Diagnostics.Debug.WriteLine("Lỗi khi tính điểm tương đồng: " + ex.Message);
            }
        }

        // Hàm tính điểm tương đồng văn bản sử dụng TF-IDF đơn giản
        private double TinhDiemTuongDongVanBan(string vanBan1, string vanBan2)
        {
            try
            {
                // Chuẩn hóa văn bản (chuyển về chữ thường, loại bỏ dấu câu)
                string normalizedText1 = NormalizeText(vanBan1);
                string normalizedText2 = NormalizeText(vanBan2);

                // Tách từ
                HashSet<string> tuVanBan1 = new HashSet<string>(normalizedText1.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));
                HashSet<string> tuVanBan2 = new HashSet<string>(normalizedText2.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

                // Tính tổng số từ giống nhau
                int soTuGiongNhau = tuVanBan1.Intersect(tuVanBan2).Count();

                // Tính tổng số từ khác nhau
                int tongSoTu = tuVanBan1.Union(tuVanBan2).Count();

                // Tính hệ số Jaccard - tỷ lệ từ chung trên tổng số từ
                double diemTuongDong = tongSoTu > 0 ? (double)soTuGiongNhau / tongSoTu : 0;

                return diemTuongDong;
            }
            catch
            {
                return 0;
            }
        }

        // Hàm chuẩn hóa văn bản
        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Chuyển thành chữ thường
            string result = text.ToLower();

            // Loại bỏ dấu câu và ký tự đặc biệt
            result = System.Text.RegularExpressions.Regex.Replace(result, @"[^\p{L}\p{N}\s]", " ");

            // Loại bỏ khoảng trắng thừa
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();

            return result;
        }

        [HttpPost]
        public JsonResult SuaHangHoa(string MaHangHoa, string TenHangHoa, string MaDanhMuc, string MaThuongHieu, string MoTa, string MoTaDai, string TrangThai, HttpPostedFileBase HinhAnh)
        {
            try
            {
                var hangHoa = data.HangHoas.FirstOrDefault(h => h.MaHangHoa == MaHangHoa);
                if (hangHoa == null)
                {
                    return Json(new { success = false, message = "Hàng hóa không tồn tại" });
                }

                // Lưu thông tin cũ để so sánh
                string maDanhMucCu = hangHoa.MaDanhMuc;
                string maThuongHieuCu = hangHoa.MaThuongHieu;
                string moTaCu = hangHoa.MoTa;

                // Xử lý hình ảnh nếu có
                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    // Xóa hình cũ nếu có
                    if (!string.IsNullOrEmpty(hangHoa.HinhAnh))
                    {
                        string oldImagePath = Path.Combine(Server.MapPath("~/Content/img/hanghoa/"), hangHoa.HinhAnh);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    string extension = Path.GetExtension(HinhAnh.FileName);
                    string hinhAnhFileName = Guid.NewGuid().ToString() + extension;
                    string filePath = Path.Combine(Server.MapPath("~/Content/img/hanghoa/"), hinhAnhFileName);
                    
                    // Đảm bảo thư mục tồn tại
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    
                    // Lưu file
                    HinhAnh.SaveAs(filePath);
                    
                    // Cập nhật tên file
                    hangHoa.HinhAnh = hinhAnhFileName;
                }
                
                // Cập nhật thông tin mới
                hangHoa.TenHangHoa = TenHangHoa;
                hangHoa.MaDanhMuc = string.IsNullOrEmpty(MaDanhMuc) ? null : MaDanhMuc;
                hangHoa.MaThuongHieu = string.IsNullOrEmpty(MaThuongHieu) ? null : MaThuongHieu;
                hangHoa.MoTa = MoTa;
                hangHoa.MoTaDai = MoTaDai;
                hangHoa.TrangThai = TrangThai;

                data.SubmitChanges();

                // Kiểm tra nếu có thay đổi về danh mục, thương hiệu hoặc mô tả thì mới cập nhật lại điểm tương đồng
                if (maDanhMucCu != MaDanhMuc || maThuongHieuCu != MaThuongHieu || moTaCu != MoTa)
                {
                    // Cập nhật lại điểm tương đồng
                    CapNhatDiemTuongDongChoHangHoa(MaHangHoa);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi chỉnh sửa hàng hóa: " + ex.Message });
            }
        }

        // Hàm cập nhật điểm tương đồng khi sửa hàng hóa
        private void CapNhatDiemTuongDongChoHangHoa(string maHangHoa)
        {
            try
            {
                // Lấy tất cả bản ghi có liên quan đến hàng hóa này
                var dsFiltering = data.ContentBasedFilterings.Where(c => 
                    c.MaHangHoa1 == maHangHoa || c.MaHangHoa2 == maHangHoa).ToList();

                // Xóa tất cả bản ghi cũ
                if (dsFiltering.Any())
                {
                    data.ContentBasedFilterings.DeleteAllOnSubmit(dsFiltering);
                    data.SubmitChanges();
                }

                // Tính toán lại điểm tương đồng
                TinhDiemTuongDongChoHangHoa(maHangHoa);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi khi cập nhật điểm tương đồng: " + ex.Message);
            }
        }

        [HttpPost]
        public JsonResult XoaMotHangHoa(string id)
        {
            try
            {
                var hangHoa = data.HangHoas.FirstOrDefault(h => h.MaHangHoa == id);
                
                if (hangHoa == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // Kiểm tra xem có biến thể nào đang được sử dụng trong đơn hàng không
                var coBienTheTrongDonHang = false;
                var bienThes = data.BienTheHangHoas.Where(bt => bt.MaHangHoa == id).ToList();
                
                foreach (var bienThe in bienThes)
                {
                    var coTrongChiTietDonHang = data.ChiTietDonHangs.Any(ct => ct.MaBienThe == bienThe.MaBienThe);
                    if (coTrongChiTietDonHang)
                    {
                        coBienTheTrongDonHang = true;
                        break;
                    }
                }

                if (coBienTheTrongDonHang)
                {
                    return Json(new { success = false, reason = "coBienTheTrongDonHang" });
                }

                // Xóa tất cả bản ghi điểm tương đồng liên quan đến hàng hóa này
                XoaDiemTuongDongCuaHangHoa(id);

                // Xóa tất cả biến thể của hàng hóa
                var bienTheCanXoa = data.BienTheHangHoas.Where(bt => bt.MaHangHoa == id);
                if (bienTheCanXoa.Any())
                {
                    data.BienTheHangHoas.DeleteAllOnSubmit(bienTheCanXoa);
                }

                // Xóa hàng hóa
                data.HangHoas.DeleteOnSubmit(hangHoa);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm: " + ex.Message });
            }
        }

        // Hàm xóa điểm tương đồng khi xóa hàng hóa
        private void XoaDiemTuongDongCuaHangHoa(string maHangHoa)
        {
            try
            {
                // Lấy tất cả bản ghi có liên quan đến hàng hóa này
                var dsFiltering = data.ContentBasedFilterings.Where(c => 
                    c.MaHangHoa1 == maHangHoa || c.MaHangHoa2 == maHangHoa).ToList();

                // Xóa tất cả bản ghi
                if (dsFiltering.Any())
                {
                    data.ContentBasedFilterings.DeleteAllOnSubmit(dsFiltering);
                    data.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi khi xóa điểm tương đồng: " + ex.Message);
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuHangHoa(string[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "Không có sản phẩm nào được chọn để xóa" });
                }

                // Kiểm tra và lưu danh sách hàng hóa có biến thể trong đơn hàng
                var hangHoaCoTrongDonHang = new List<string>();
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    var bienThes = data.BienTheHangHoas.Where(bt => bt.MaHangHoa == id).ToList();
                    var coBienTheTrongDonHang = false;
                    
                    foreach (var bienThe in bienThes)
                    {
                        var coTrongChiTietDonHang = data.ChiTietDonHangs.Any(ct => ct.MaBienThe == bienThe.MaBienThe);
                        if (coTrongChiTietDonHang)
                        {
                            coBienTheTrongDonHang = true;
                            break;
                        }
                    }
                    
                    if (coBienTheTrongDonHang)
                    {
                        var tenHangHoa = data.HangHoas
                            .Where(h => h.MaHangHoa == id)
                            .Select(h => h.TenHangHoa)
                            .FirstOrDefault();
                        hangHoaCoTrongDonHang.Add($"{id} ({tenHangHoa})");
                    }
                }

                // Nếu có hàng hóa có biến thể đang được sử dụng trong đơn hàng
                if (hangHoaCoTrongDonHang.Any())
                {
                    var danhSachHH = string.Join("<br/>", hangHoaCoTrongDonHang);
                    return Json(new { 
                        success = false, 
                        reason = "coBienTheTrongDonHang", 
                        message = danhSachHH
                    });
                }

                // Xóa các biến thể và hàng hóa nếu không có vấn đề
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    // Xóa tất cả bản ghi điểm tương đồng liên quan đến hàng hóa này
                    XoaDiemTuongDongCuaHangHoa(id);

                    // Xóa tất cả biến thể của hàng hóa
                    var bienTheCanXoa = data.BienTheHangHoas.Where(bt => bt.MaHangHoa == id);
                    if (bienTheCanXoa.Any())
                    {
                        data.BienTheHangHoas.DeleteAllOnSubmit(bienTheCanXoa);
                    }
                }

                // Xóa các hàng hóa
                var hangHoaCanXoa = data.HangHoas.Where(h => ids.Contains(h.MaHangHoa));
                if (hangHoaCanXoa.Any())
                {
                    data.HangHoas.DeleteAllOnSubmit(hangHoaCanXoa);
                    data.SubmitChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm cần xóa" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm: " + ex.Message });
            }
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
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }

            var diaChiList = data.DiaChiKhachHangs
                                 .Where(dc => dc.MaKhachHang == id)
                                 .ToList();

            ViewBag.TenKhachHang = data.KhachHangs
                                       .Where(k => k.MaKhachHang == id)
                                       .Select(k => k.HoTen)
                                       .FirstOrDefault() ?? "Không xác định";
            
            ViewBag.MaKhachHang = id;

            return View(diaChiList);
        }

        [HttpPost]
        public JsonResult ThemDiaChiKhachHang(string MaKhachHang, string TenNguoiNhan, string SoDienThoai, string DiaChiDayDu, bool LaMacDinh)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(MaKhachHang) || string.IsNullOrEmpty(TenNguoiNhan) || 
                    string.IsNullOrEmpty(SoDienThoai) || string.IsNullOrEmpty(DiaChiDayDu))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
                }

                // Kiểm tra khách hàng có tồn tại không
                var khachHang = data.KhachHangs.FirstOrDefault(k => k.MaKhachHang == MaKhachHang);
                if (khachHang == null)
                {
                    return Json(new { success = false, message = "Khách hàng không tồn tại" });
                }

                // Tạo mã địa chỉ tự động
                string MaDiaChi;
                do
                {
                    MaDiaChi = "DC" + GenerateRandomString(6);
                } while (data.DiaChiKhachHangs.Any(d => d.MaDiaChi == MaDiaChi));

                // Nếu địa chỉ mới là mặc định, hủy mặc định của các địa chỉ khác
                if (LaMacDinh)
                {
                    var diaChiMacDinh = data.DiaChiKhachHangs
                        .Where(d => d.MaKhachHang == MaKhachHang && d.LaMacDinh == true);
                    
                    foreach (var dc in diaChiMacDinh)
                    {
                        dc.LaMacDinh = false;
                    }
                }

                // Tạo địa chỉ mới
                var diaChiMoi = new DiaChiKhachHang
                {
                    MaDiaChi = MaDiaChi,
                    MaKhachHang = MaKhachHang,
                    TenNguoiNhan = TenNguoiNhan,
                    SoDienThoai = SoDienThoai,
                    DiaChiDayDu = DiaChiDayDu,
                    LaMacDinh = LaMacDinh
                };

                data.DiaChiKhachHangs.InsertOnSubmit(diaChiMoi);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm địa chỉ: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaDiaChiKhachHang(string MaDiaChi, string TenNguoiNhan, string SoDienThoai, string DiaChiDayDu, bool LaMacDinh)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(MaDiaChi) || string.IsNullOrEmpty(TenNguoiNhan) || 
                    string.IsNullOrEmpty(SoDienThoai) || string.IsNullOrEmpty(DiaChiDayDu))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
                }

                // Tìm địa chỉ cần cập nhật
                var diaChi = data.DiaChiKhachHangs.FirstOrDefault(d => d.MaDiaChi == MaDiaChi);
                if (diaChi == null)
                {
                    return Json(new { success = false, message = "Địa chỉ không tồn tại" });
                }

                // Nếu địa chỉ là mặc định và chưa được đặt làm mặc định trước đó
                if (LaMacDinh && diaChi.LaMacDinh == false)
                {
                    // Hủy mặc định của các địa chỉ khác
                    var diaChiMacDinh = data.DiaChiKhachHangs
                        .Where(d => d.MaKhachHang == diaChi.MaKhachHang && d.LaMacDinh == true);
                    
                    foreach (var dc in diaChiMacDinh)
                    {
                        dc.LaMacDinh = false;
                    }
                }

                // Cập nhật thông tin
                diaChi.TenNguoiNhan = TenNguoiNhan;
                diaChi.SoDienThoai = SoDienThoai;
                diaChi.DiaChiDayDu = DiaChiDayDu;
                diaChi.LaMacDinh = LaMacDinh;

                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật địa chỉ: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaDiaChiKhachHang(string MaDiaChi)
        {
            try
            {
                // Tìm địa chỉ cần xóa
                var diaChi = data.DiaChiKhachHangs.FirstOrDefault(d => d.MaDiaChi == MaDiaChi);
                if (diaChi == null)
                {
                    return Json(new { success = false, message = "Địa chỉ không tồn tại" });
                }

                // Kiểm tra xem địa chỉ có đang được sử dụng trong đơn hàng không
                var daDung = data.GiaoHangs.Any(g => g.MaDiaChi == MaDiaChi);
                if (daDung)
                {
                    return Json(new { success = false, reason = "daDung" });
                }

                // Kiểm tra xem địa chỉ này có phải địa chỉ mặc định không
                bool? laMacDinh = diaChi.LaMacDinh;
                string maKhachHang = diaChi.MaKhachHang;

                // Xóa địa chỉ
                data.DiaChiKhachHangs.DeleteOnSubmit(diaChi);
                data.SubmitChanges();

                // Nếu là địa chỉ mặc định và có các địa chỉ khác, trả về thông tin để yêu cầu chọn địa chỉ mặc định mới
                if (laMacDinh == true)
                {
                    var diaChiKhac = data.DiaChiKhachHangs
                        .Where(d => d.MaKhachHang == maKhachHang)
                        .Select(d => new { d.MaDiaChi, d.TenNguoiNhan, d.DiaChiDayDu })
                        .ToList();

                    if (diaChiKhac.Any())
                    {
                        return Json(new { 
                            success = true, 
                            canChonMacDinh = true, 
                            danhSachDiaChi = diaChiKhac 
                        });
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa địa chỉ: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DatDiaChiMacDinh(string MaDiaChi)
        {
            try
            {
                // Tìm địa chỉ cần đặt làm mặc định
                var diaChi = data.DiaChiKhachHangs.FirstOrDefault(d => d.MaDiaChi == MaDiaChi);
                if (diaChi == null)
                {
                    return Json(new { success = false, message = "Địa chỉ không tồn tại" });
                }

                // Hủy mặc định của các địa chỉ khác
                var diaChiMacDinh = data.DiaChiKhachHangs
                    .Where(d => d.MaKhachHang == diaChi.MaKhachHang && d.LaMacDinh == true);
                
                foreach (var dc in diaChiMacDinh)
                {
                    dc.LaMacDinh = false;
                }

                // Đặt địa chỉ mới làm mặc định
                diaChi.LaMacDinh = true;
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi đặt địa chỉ mặc định: " + ex.Message });
            }
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

        [HttpPost]
        public JsonResult TaoKhachHang(string TenDangNhap, string HoTen, string MatKhau, string Email, string SoDienThoai, string DiaChi, string TrangThai)
        {
            try
            {
                // Kiểm tra tên đăng nhập đã tồn tại chưa
                if (data.KhachHangs.Any(k => k.TenDangNhap == TenDangNhap))
                {
                    return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });
                }

                // Kiểm tra email đã tồn tại chưa
                if (data.KhachHangs.Any(k => k.Email == Email))
                {
                    return Json(new { success = false, message = "Email đã tồn tại" });
                }

                // Tạo mã khách hàng tự động
                string MaKhachHang;
                do
                {
                    MaKhachHang = "KH" + GenerateRandomString(6);
                } while (data.KhachHangs.Any(k => k.MaKhachHang == MaKhachHang));

                // Mã hóa mật khẩu
                string matKhauMaHoa = EncryptPassword(MatKhau, "mysecretkey");

                var khachHang = new KhachHang
                {
                    MaKhachHang = MaKhachHang,
                    TenDangNhap = TenDangNhap,
                    HoTen = HoTen,
                    MatKhauHash = matKhauMaHoa,
                    Email = Email,
                    SoDienThoai = SoDienThoai,
    
                    NgayTao = DateTime.Now,
                    TrangThai = TrangThai
                };

                data.KhachHangs.InsertOnSubmit(khachHang);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm khách hàng: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaKhachHang(string MaKhachHang, string HoTen, string Email, string SoDienThoai, string DiaChi, string TrangThai, string MatKhau)
        {
            try
            {
                var khachHang = data.KhachHangs.FirstOrDefault(k => k.MaKhachHang == MaKhachHang);
                if (khachHang == null)
                {
                    return Json(new { success = false, message = "Khách hàng không tồn tại" });
                }

                // Kiểm tra email đã tồn tại với khách hàng khác chưa
                if (khachHang.Email != Email && data.KhachHangs.Any(k => k.Email == Email))
                {
                    return Json(new { success = false, message = "Email đã tồn tại với khách hàng khác" });
                }

                // Cập nhật thông tin
                khachHang.HoTen = HoTen;
                khachHang.Email = Email;
                khachHang.SoDienThoai = SoDienThoai;
   
                khachHang.TrangThai = TrangThai;

                // Nếu có mật khẩu mới thì cập nhật
                if (!string.IsNullOrEmpty(MatKhau))
                {
                    // Mã hóa mật khẩu
                    string matKhauMaHoa = EncryptPassword(MatKhau, "mysecretkey");
                    khachHang.MatKhauHash = matKhauMaHoa;
                }

                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật khách hàng: " + ex.Message });
            }
        }

        [HttpGet]
        public string LayDiaChiKhachHang(string id)
        {
            var khachHang = data.KhachHangs.FirstOrDefault(k => k.MaKhachHang == id);
            return khachHang?.HoTen ?? "";
        }

        [HttpPost]
        public JsonResult XoaMotKhachHang(string id)
        {
            try
            {
                var khachHang = data.KhachHangs.FirstOrDefault(k => k.MaKhachHang == id);
                if (khachHang == null)
                {
                    return Json(new { success = false, message = "Khách hàng không tồn tại" });
                }

                // Kiểm tra xem khách hàng có đơn hàng không
                var coDonHang = data.DonHangs.Any(d => d.MaKhachHang == id);
                if (coDonHang)
                {
                    return Json(new { success = false, reason = "coDonHang" });
                }

                // Xóa tất cả địa chỉ của khách hàng
                var diaChiList = data.DiaChiKhachHangs.Where(dc => dc.MaKhachHang == id);
                if (diaChiList.Any())
                {
                    data.DiaChiKhachHangs.DeleteAllOnSubmit(diaChiList);
                }

                // Xóa khách hàng
                data.KhachHangs.DeleteOnSubmit(khachHang);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khách hàng: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuKhachHang(string[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "Không có khách hàng nào được chọn để xóa" });
                }

                // Kiểm tra và lưu danh sách khách hàng có đơn hàng
                var khachHangCoDonHang = new List<string>();
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    var coDonHang = data.DonHangs.Any(d => d.MaKhachHang == id);
                    if (coDonHang)
                    {
                        var tenKhachHang = data.KhachHangs
                            .Where(k => k.MaKhachHang == id)
                            .Select(k => k.HoTen)
                            .FirstOrDefault();
                        khachHangCoDonHang.Add($"{id} ({tenKhachHang})");
                    }
                }

                // Nếu có khách hàng đang có đơn hàng
                if (khachHangCoDonHang.Any())
                {
                    var danhSachKH = string.Join(", ", khachHangCoDonHang);
                    return Json(new { 
                        success = false, 
                        reason = "coDonHang", 
                        message = danhSachKH
                    });
                }

                // Xóa các địa chỉ và khách hàng nếu không có vấn đề
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    // Xóa tất cả địa chỉ của khách hàng
                    var diaChiList = data.DiaChiKhachHangs.Where(dc => dc.MaKhachHang == id);
                    if (diaChiList.Any())
                    {
                        data.DiaChiKhachHangs.DeleteAllOnSubmit(diaChiList);
                    }
                }

                // Xóa các khách hàng
                var khachHangCanXoa = data.KhachHangs.Where(k => ids.Contains(k.MaKhachHang));
                if (khachHangCanXoa.Any())
                {
                    data.KhachHangs.DeleteAllOnSubmit(khachHangCanXoa);
                    data.SubmitChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy khách hàng cần xóa" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa khách hàng: " + ex.Message });
            }
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

            // Lấy danh sách địa chỉ của khách hàng
            if (donHang != null && donHang.MaKhachHang != null)
            {
                ViewBag.DiaChiKhachHang = data.DiaChiKhachHangs
                    .Where(d => d.MaKhachHang == donHang.MaKhachHang)
                    .ToList();
            }
            
            // Lấy đối tượng địa chỉ khách hàng cho đơn giao hàng nếu có
            DiaChiKhachHang diaChiGiaoHang = null;
            if (giaoHang != null && !string.IsNullOrEmpty(giaoHang.MaDiaChi))
            {
                diaChiGiaoHang = data.DiaChiKhachHangs.FirstOrDefault(d => d.MaDiaChi == giaoHang.MaDiaChi);
            }

            var viewModel = new ChiTietDonHangViewModel
            {
                DonHang = donHang,
                ChiTietDonHangs = chiTiet,
                ThanhToan = thanhToan,
                GiaoHang = giaoHang,
                DiaChiGiaoHang = diaChiGiaoHang
            };

            return View(viewModel);
        }
        
        [HttpGet]
        public JsonResult LayThongTinDonHang(string id)
        {
            try
            {
                var donHang = data.DonHangs.FirstOrDefault(d => d.MaDonHang == id);
                if (donHang == null)
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    donHang.MaDonHang,
                    donHang.MaKhachHang,
                    donHang.TongTien,
                    donHang.TrangThaiDonHang,
                    donHang.TrangThaiThanhToan,
                    NgayTao = donHang.NgayTao?.ToString("dd/MM/yyyy HH:mm")
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }
        
        [HttpPost]
        public JsonResult CapNhatDonHang(string MaDonHang, string TrangThaiDonHang, string TrangThaiThanhToan)
        {
            try
            {
                var donHang = data.DonHangs.FirstOrDefault(d => d.MaDonHang == MaDonHang);
                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                donHang.TrangThaiDonHang = TrangThaiDonHang;
                donHang.TrangThaiThanhToan = TrangThaiThanhToan;
                
                // Cập nhật trạng thái thanh toán nếu đơn hàng đã thanh toán
                if (TrangThaiThanhToan == "DaThanhToan")
                {
                    var thanhToan = data.ThanhToans.FirstOrDefault(t => t.MaDonHang == MaDonHang);
                    if (thanhToan != null)
                    {
                        thanhToan.TrangThai = "ThanhCong";
                        thanhToan.NgayThanhToan = DateTime.Now;
                    }
                }
                
                // Cập nhật trạng thái giao hàng nếu đơn hàng đã giao
                if (TrangThaiDonHang == "DaGiao" || TrangThaiDonHang == "HoanThanh")
                {
                    var giaoHang = data.GiaoHangs.FirstOrDefault(g => g.MaDonHang == MaDonHang);
                    if (giaoHang != null)
                    {
                        giaoHang.TrangThaiGiaoHang = TrangThaiDonHang == "DaGiao" ? "DaGiao" : "DaGiao";
                        giaoHang.NgayNhanHang = DateTime.Now;
                    }
                }

                data.SubmitChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        
        [HttpPost]
        public JsonResult XoaDonHang(string id)
        {
            try
            {
                var donHang = data.DonHangs.FirstOrDefault(d => d.MaDonHang == id);
                if (donHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Xóa thanh toán liên quan
                var thanhToan = data.ThanhToans.FirstOrDefault(t => t.MaDonHang == id);
                if (thanhToan != null)
                {
                    data.ThanhToans.DeleteOnSubmit(thanhToan);
                }

                // Xóa giao hàng liên quan
                var giaoHang = data.GiaoHangs.FirstOrDefault(g => g.MaDonHang == id);
                if (giaoHang != null)
                {
                    data.GiaoHangs.DeleteOnSubmit(giaoHang);
                }

                // Xóa chi tiết đơn hàng
                var chiTiet = data.ChiTietDonHangs.Where(c => c.MaDonHang == id).ToList();
                if (chiTiet.Any())
                {
                    data.ChiTietDonHangs.DeleteAllOnSubmit(chiTiet);
                }

                // Xóa đơn hàng
                data.DonHangs.DeleteOnSubmit(donHang);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        
        [HttpPost]
        public JsonResult XoaNhieuDonHang(string[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "Không có đơn hàng nào được chọn" });
                }

                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    // Xóa thanh toán liên quan
                    var thanhToan = data.ThanhToans.FirstOrDefault(t => t.MaDonHang == id);
                    if (thanhToan != null)
                    {
                        data.ThanhToans.DeleteOnSubmit(thanhToan);
                    }

                    // Xóa giao hàng liên quan
                    var giaoHang = data.GiaoHangs.FirstOrDefault(g => g.MaDonHang == id);
                    if (giaoHang != null)
                    {
                        data.GiaoHangs.DeleteOnSubmit(giaoHang);
                    }

                    // Xóa chi tiết đơn hàng
                    var chiTiet = data.ChiTietDonHangs.Where(c => c.MaDonHang == id).ToList();
                    if (chiTiet.Any())
                    {
                        data.ChiTietDonHangs.DeleteAllOnSubmit(chiTiet);
                    }
                }

                // Xóa đơn hàng
                var donHangs = data.DonHangs.Where(d => ids.Contains(d.MaDonHang)).ToList();
                data.DonHangs.DeleteAllOnSubmit(donHangs);
                
                data.SubmitChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
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
            
            // Lấy danh sách nhà cung cấp và biến thể để hiển thị trong dropdown
            ViewBag.NhaCungCaps = data.NhaCungCaps.ToList();
            ViewBag.BienThes = data.BienTheHangHoas.ToList();
            // Lấy danh sách nhân viên cho bộ lọc
            ViewBag.NhanViens = data.NhanViens.ToList();
            
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
        
        [HttpPost]
        public JsonResult TaoNhapHang(NhapHangViewModel model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.MaNhaCungCap) || model.ChiTietNhapHangs == null || !model.ChiTietNhapHangs.Any())
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Tạo mã nhập hàng tự động
                string maNhapHang;
                do
                {
                    maNhapHang = "NH" + GenerateRandomString(6);
                } while (data.NhapHangs.Any(n => n.MaNhapHang == maNhapHang));
                
                // Lấy mã nhân viên đang đăng nhập
                string maNhanVien = Session["UserID"].ToString();

                // Đảm bảo giá trị DaThanhToan và xác định TrangThai
                double daThanhToan = model.DaThanhToan.HasValue ? (double)model.DaThanhToan.Value : 0;
                string trangThai = "Chuathanhtoan";
                
                if (daThanhToan >= (double)model.TongTien)
                {
                    trangThai = "Dathanhtoanhet";
                    daThanhToan = (double)model.TongTien; // Đảm bảo không thanh toán quá tổng tiền
                }
                else if (daThanhToan > 0)
                {
                    trangThai = "Dathanhtoanmotphan";
                }

                // Tạo phiếu nhập mới
                var nhapHang = new NhapHang
                {
                    MaNhapHang = maNhapHang,
                    MaNhaCungCap = model.MaNhaCungCap,
                    MaNhanVien = maNhanVien,
                    TongTien = (double)model.TongTien,
                    DaThanhToan = daThanhToan,
                    TrangThai = trangThai,
                    NgayNhap = DateTime.Now
                };
                data.NhapHangs.InsertOnSubmit(nhapHang);

                // Tạo chi tiết nhập hàng
                foreach (var item in model.ChiTietNhapHangs)
                {
                    string maChiTiet;
                    do
                    {
                        maChiTiet = "CTNH" + GenerateRandomString(6);
                    } while (data.ChiTietNhapHangs.Any(c => c.MaChiTietNhapHang == maChiTiet));

                    var chiTiet = new ChiTietNhapHang
                    {
                        MaChiTietNhapHang = maChiTiet,
                        MaNhapHang = maNhapHang,
                        MaBienThe = item.MaBienThe,
                        SoLuong = item.SoLuong,
                        DonGia = (double)item.DonGia
                    };
                    data.ChiTietNhapHangs.InsertOnSubmit(chiTiet);
                    
                    // Cập nhật số lượng tồn kho của biến thể
                    var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == item.MaBienThe);
                    if (bienThe != null)
                    {
                        bienThe.SoLuongTonKho += item.SoLuong;
                    }
                }

                data.SubmitChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult LayThongTinNhapHang(string id)
        {
            try
            {
                var nhapHang = data.NhapHangs.FirstOrDefault(n => n.MaNhapHang == id);
                if (nhapHang == null)
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    nhapHang.MaNhapHang,
                    nhapHang.MaNhaCungCap,
                    nhapHang.MaNhanVien,
                    nhapHang.TongTien,
                    nhapHang.DaThanhToan,
                    nhapHang.TrangThai,
                    NgayNhap = nhapHang.NgayNhap?.ToString("dd/MM/yyyy HH:mm")
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CapNhatNhapHang(string MaNhapHang, string MaNhaCungCap, double DaThanhToan, string TrangThai)
        {
            try
            {
                var nhapHang = data.NhapHangs.FirstOrDefault(n => n.MaNhapHang == MaNhapHang);
                if (nhapHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu nhập" });
                }

                // Cập nhật thông tin phiếu nhập
                nhapHang.MaNhaCungCap = MaNhaCungCap;
                
                // Đảm bảo giá trị DaThanhToan hợp lệ
                if (DaThanhToan >= nhapHang.TongTien)
                {
                    nhapHang.DaThanhToan = nhapHang.TongTien;
                    nhapHang.TrangThai = "Dathanhtoanhet";
                }
                else if (DaThanhToan > 0)
                {
                    nhapHang.DaThanhToan = DaThanhToan;
                    nhapHang.TrangThai = "Dathanhtoanmotphan";
                }
                else
                {
                    nhapHang.DaThanhToan = 0;
                    nhapHang.TrangThai = "Chuathanhtoan";
                }
                
                // Nếu trạng thái được chỉ định rõ ràng, sử dụng giá trị đó thay vì tính toán
                if (!string.IsNullOrEmpty(TrangThai))
                {
                    nhapHang.TrangThai = TrangThai;
                }

                data.SubmitChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhapHang(string id)
        {
            try
            {
                var nhapHang = data.NhapHangs.FirstOrDefault(n => n.MaNhapHang == id);
                if (nhapHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu nhập" });
                }
                
                // Lấy chi tiết nhập hàng để điều chỉnh lại số lượng tồn kho
                var chiTiet = data.ChiTietNhapHangs.Where(c => c.MaNhapHang == id).ToList();
                foreach (var item in chiTiet)
                {
                    var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == item.MaBienThe);
                    if (bienThe != null)
                    {
                        bienThe.SoLuongTonKho -= item.SoLuong;
                        // Đảm bảo số lượng tồn không âm
                        if (bienThe.SoLuongTonKho < 0)
                            bienThe.SoLuongTonKho = 0;
                    }
                }

                // Xóa chi tiết nhập hàng (các ràng buộc CASCADE sẽ tự động xóa)
                data.ChiTietNhapHangs.DeleteAllOnSubmit(chiTiet);
                
                // Xóa phiếu nhập
                data.NhapHangs.DeleteOnSubmit(nhapHang);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuNhapHang(string[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "Không có phiếu nhập nào được chọn" });
                }

                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    // Lấy chi tiết nhập hàng để điều chỉnh lại số lượng tồn kho
                    var chiTiet = data.ChiTietNhapHangs.Where(c => c.MaNhapHang == id).ToList();
                    foreach (var item in chiTiet)
                    {
                        var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == item.MaBienThe);
                        if (bienThe != null)
                        {
                            bienThe.SoLuongTonKho -= item.SoLuong;
                            // Đảm bảo số lượng tồn không âm
                            if (bienThe.SoLuongTonKho < 0)
                                bienThe.SoLuongTonKho = 0;
                        }
                    }

                    // Xóa chi tiết nhập hàng (các ràng buộc CASCADE sẽ tự động xóa)
                    data.ChiTietNhapHangs.DeleteAllOnSubmit(chiTiet);
                }

                // Xóa phiếu nhập
                var nhapHangs = data.NhapHangs.Where(n => ids.Contains(n.MaNhapHang)).ToList();
                data.NhapHangs.DeleteAllOnSubmit(nhapHangs);
                
                data.SubmitChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
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

        [HttpPost]
        public JsonResult TaoNhanVien(string TenDangNhap, string HoTen, string DiaChi, string MatKhau, string Email, string SoDienThoai, string TrangThai, string VaiTro)
        {
            try
            {
                // Kiểm tra tên đăng nhập đã tồn tại chưa
                if (data.NhanViens.Any(nv => nv.TenDangNhap == TenDangNhap))
                {
                    return Json(new { success = false, message = "Tên đăng nhập đã tồn tại" });
                }

                // Kiểm tra email đã tồn tại chưa
                if (data.NhanViens.Any(nv => nv.Email == Email))
                {
                    return Json(new { success = false, message = "Email đã tồn tại" });
                }

                // Tạo mã nhân viên tự động
                string MaNhanVien;
                do
                {
                    MaNhanVien = "NV" + GenerateRandomString(6);
                } while (data.NhanViens.Any(nv => nv.MaNhanVien == MaNhanVien));

                // Mã hóa mật khẩu
                string matKhauMaHoa = EncryptPassword(MatKhau, "mysecretkey");
                
                var nhanVien = new NhanVien
                {
                    MaNhanVien = MaNhanVien,
                    TenDangNhap = TenDangNhap,
                    HoTen = HoTen,
                    DiaChi = DiaChi,
                    MatKhau = matKhauMaHoa,
                    Email = Email,
                    SoDienThoai = SoDienThoai,
                    NgayTao = DateTime.Now,
                    TrangThai = TrangThai,
                    VaiTro = VaiTro
                };

                data.NhanViens.InsertOnSubmit(nhanVien);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm nhân viên: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaNhanVien(string MaNhanVien, string HoTen, string DiaChi, string Email, string SoDienThoai, string TrangThai, string VaiTro, string MatKhau = null)
        {
            try
            {
                var nhanVien = data.NhanViens.FirstOrDefault(nv => nv.MaNhanVien == MaNhanVien);
                if (nhanVien == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên" });
                }

                // Kiểm tra email đã tồn tại chưa (nếu có thay đổi)
                if (nhanVien.Email != Email && data.NhanViens.Any(nv => nv.Email == Email))
                {
                    return Json(new { success = false, message = "Email đã tồn tại" });
                }

                // Cập nhật thông tin nhân viên
                nhanVien.HoTen = HoTen;
                nhanVien.DiaChi = DiaChi;
                nhanVien.Email = Email;
                nhanVien.SoDienThoai = SoDienThoai;
                nhanVien.TrangThai = TrangThai;
                nhanVien.VaiTro = VaiTro;

                // Cập nhật mật khẩu nếu có
                if (!string.IsNullOrEmpty(MatKhau))
                {
                    // Mã hóa mật khẩu
                    string matKhauMaHoa = EncryptPassword(MatKhau, "mysecretkey");
                    nhanVien.MatKhau = matKhauMaHoa;
                }

                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi sửa nhân viên: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaMotNhanVien(string id)
        {
            try
            {
                // Kiểm tra nhân viên có tồn tại không
                var nhanVien = data.NhanViens.FirstOrDefault(nv => nv.MaNhanVien == id);
                if (nhanVien == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên" });
                }

                // Kiểm tra nhân viên có liên quan đến đơn nhập hàng không
                if (data.NhapHangs.Any(nh => nh.MaNhanVien == id))
                {
                    return Json(new { success = false, reason = "coDonNhap", message = "Nhân viên này đã thực hiện đơn nhập hàng. Không thể xóa." });
                }

                data.NhanViens.DeleteOnSubmit(nhanVien);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa nhân viên: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuNhanVien(string[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "Không có nhân viên nào được chọn để xóa" });
                }

                // Kiểm tra nhân viên có liên quan đến đơn nhập hàng không
                var nhanVienCoDonNhap = new List<string>();
                foreach (var id in ids)
                {
                    if (data.NhapHangs.Any(nh => nh.MaNhanVien == id))
                    {
                        var tenNhanVien = data.NhanViens
                            .Where(nv => nv.MaNhanVien == id)
                            .Select(nv => nv.HoTen)
                            .FirstOrDefault();
                        nhanVienCoDonNhap.Add(tenNhanVien + " (" + id + ")");
                    }
                }

                if (nhanVienCoDonNhap.Any())
                {
                    var danhSach = string.Join(", ", nhanVienCoDonNhap);
                    return Json(new {
                        success = false,
                        reason = "coDonNhap",
                        message = $"Không thể xóa vì các nhân viên sau đã thực hiện đơn nhập hàng: {danhSach}"
                    });
                }

                var nhanVienCanXoa = data.NhanViens.Where(nv => ids.Contains(nv.MaNhanVien));
                if (nhanVienCanXoa.Any())
                {
                    data.NhanViens.DeleteAllOnSubmit(nhanVienCanXoa);
                    data.SubmitChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên cần xóa" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa nhân viên: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult LayThongTinNhanVien(string id)
        {
            try
            {
                var nhanVien = data.NhanViens.FirstOrDefault(nv => nv.MaNhanVien == id);
                if (nhanVien == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhân viên" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    MaNhanVien = nhanVien.MaNhanVien,
                    TenDangNhap = nhanVien.TenDangNhap,
                    HoTen = nhanVien.HoTen,
                    DiaChi = nhanVien.DiaChi,
                    Email = nhanVien.Email,
                    SoDienThoai = nhanVien.SoDienThoai,
                    TrangThai = nhanVien.TrangThai,
                    VaiTro = nhanVien.VaiTro,
                    NgayTao = nhanVien.NgayTao
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult XoaVoucher(int id)
        {
            try
            {
                // Kiểm tra xem voucher có được sử dụng trong đơn hàng không
                var coTrongDonHang = data.DonHangs.Any(d => d.MaVoucher == id);
                if (coTrongDonHang)
                {
                    return Json(new { success = false, reason = "daDung", message = "Voucher này đã được sử dụng trong đơn hàng" });
                }

                var voucher = data.Vouchers.FirstOrDefault(v => v.MaVoucher == id);
                if (voucher == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy voucher" });
                }

                data.Vouchers.DeleteOnSubmit(voucher);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa voucher: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuVoucher(int[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "Không có voucher nào được chọn để xóa" });
                }

                // Kiểm tra xem có voucher nào đang được sử dụng không
                var voucherDaDung = new List<string>();
                foreach (var id in ids)
                {
                    var coTrongDonHang = data.DonHangs.Any(d => d.MaVoucher == id);
                    if (coTrongDonHang)
                    {
                        var tenVoucher = data.Vouchers
                            .Where(v => v.MaVoucher == id)
                            .Select(v => v.MaVoucherCode + " (" + v.TenVoucher + ")")
                            .FirstOrDefault();
                        voucherDaDung.Add(tenVoucher);
                    }
                }

                if (voucherDaDung.Any())
                {
                    var danhSach = string.Join(", ", voucherDaDung);
                    return Json(new { 
                        success = false, 
                        reason = "daDung", 
                        message = $"Không thể xóa vì các voucher sau đã được sử dụng trong đơn hàng: {danhSach}" 
                    });
                }

                var vouchersCanXoa = data.Vouchers.Where(v => ids.Contains(v.MaVoucher));
                if (vouchersCanXoa.Any())
                {
                    data.Vouchers.DeleteAllOnSubmit(vouchersCanXoa);
                    data.SubmitChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy voucher cần xóa" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa voucher: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaVoucher(Voucher voucher)
        {
            try
            {
                var existingVoucher = data.Vouchers.FirstOrDefault(v => v.MaVoucher == voucher.MaVoucher);
                if (existingVoucher == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy voucher" });
                }

                // Kiểm tra mã code mới có trùng với mã code khác không
                if (existingVoucher.MaVoucherCode != voucher.MaVoucherCode &&
                    data.Vouchers.Any(v => v.MaVoucherCode == voucher.MaVoucherCode))
                {
                    return Json(new { success = false, message = "Mã voucher code đã tồn tại" });
                }

                existingVoucher.MaVoucherCode = voucher.MaVoucherCode;
                existingVoucher.TenVoucher = voucher.TenVoucher;
                existingVoucher.MoTa = voucher.MoTa;
                existingVoucher.LoaiGiamGia = voucher.LoaiGiamGia;
                existingVoucher.GiaTriGiamGia = voucher.GiaTriGiamGia;
                existingVoucher.DonHangToiThieu = voucher.DonHangToiThieu;
                existingVoucher.SoLuong = voucher.SoLuong;
                existingVoucher.NgayBatDau = voucher.NgayBatDau;
                existingVoucher.NgayKetThuc = voucher.NgayKetThuc;
                existingVoucher.TrangThai = voucher.TrangThai;

                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật voucher: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult TaoBienTheHangHoa(string MaHangHoa, string MauSac, string DungLuong, string CPU, 
            string RAM, string KichThuocManHinh, string LoaiBoNho, decimal GiaNhap, decimal GiaBan, decimal? GiaKhuyenMai, int SoLuongTonKho)
        {
            try
            {
                // Kiểm tra hàng hóa có tồn tại
                var hangHoa = data.HangHoas.FirstOrDefault(h => h.MaHangHoa == MaHangHoa);
                if (hangHoa == null)
                {
                    return Json(new { success = false, message = "Hàng hóa không tồn tại" });
                }
                
                // Tạo mã biến thể tự động
                string MaBienThe;
                do
                {
                    MaBienThe = "BT" + GenerateRandomString(6);
                } while (data.BienTheHangHoas.Any(b => b.MaBienThe == MaBienThe));

                var bienThe = new BienTheHangHoa
                {
                    MaBienThe = MaBienThe,
                    MaHangHoa = MaHangHoa,
                    MauSac = MauSac,
                    DungLuong = DungLuong,
                    CPU = CPU,
                    RAM = RAM,
                    KichThuocManHinh = KichThuocManHinh,
                    LoaiBoNho = LoaiBoNho,
                    GiaNhap = (double)GiaNhap,
                    GiaBan = (double)GiaBan,
                    GiaKhuyenMai = GiaKhuyenMai.HasValue ? (double)GiaKhuyenMai.Value : (double?)null,
                    SoLuongTonKho = SoLuongTonKho
                };

                data.BienTheHangHoas.InsertOnSubmit(bienThe);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm biến thể: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaBienTheHangHoa(string MaBienThe, string MauSac, string DungLuong, string CPU, 
            string RAM, string KichThuocManHinh, string LoaiBoNho, decimal GiaNhap, decimal GiaBan, decimal? GiaKhuyenMai, int SoLuongTonKho)
        {
            try
            {
                var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == MaBienThe);
                if (bienThe == null)
                {
                    return Json(new { success = false, message = "Biến thể không tồn tại" });
                }

                bienThe.MauSac = MauSac;
                bienThe.DungLuong = DungLuong;
                bienThe.CPU = CPU;
                bienThe.RAM = RAM;
                bienThe.KichThuocManHinh = KichThuocManHinh;
                bienThe.LoaiBoNho = LoaiBoNho;
                bienThe.GiaNhap = (double)GiaNhap;
                bienThe.GiaBan = (double)GiaBan;
                bienThe.GiaKhuyenMai = GiaKhuyenMai.HasValue ? (double)GiaKhuyenMai.Value : (double?)null;
                bienThe.SoLuongTonKho = SoLuongTonKho;

                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi chỉnh sửa biến thể: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaMotBienTheHangHoa(string id)
        {
            try 
            {
                var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == id);
                if (bienThe == null)
                {
                    return Json(new { success = false, message = "Biến thể không tồn tại" });
                }

                // Kiểm tra xem biến thể có trong đơn hàng không
                var coTrongDonHang = data.ChiTietDonHangs.Any(ct => ct.MaBienThe == id);
                if (coTrongDonHang)
                {
                    return Json(new { success = false, reason = "coTrongDonHang" });
                }

                data.BienTheHangHoas.DeleteOnSubmit(bienThe);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex) 
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa biến thể: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuBienTheHangHoa(string[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return Json(new { success = false, message = "Không có biến thể nào được chọn để xóa" });
                }

                // Kiểm tra và lưu danh sách biến thể có trong đơn hàng
                var bienTheCoTrongDonHang = new List<string>();
                
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    var coTrongDonHang = data.ChiTietDonHangs.Any(ct => ct.MaBienThe == id);
                    if (coTrongDonHang)
                    {
                        var bienThe = data.BienTheHangHoas
                            .Where(b => b.MaBienThe == id)
                            .Select(b => new { 
                                MaBienThe = b.MaBienThe, 
                                MauSac = b.MauSac ?? "Không có", 
                                DungLuong = b.DungLuong ?? "Không có" 
                            })
                            .FirstOrDefault();
                            
                        if (bienThe != null)
                        {
                            bienTheCoTrongDonHang.Add($"{bienThe.MaBienThe} ({bienThe.MauSac} - {bienThe.DungLuong})");
                        }
                    }
                }

                // Nếu có biến thể đang được sử dụng trong đơn hàng
                if (bienTheCoTrongDonHang.Any())
                {
                    var danhSachBT = string.Join("<br>", bienTheCoTrongDonHang);
                    return Json(new { 
                        success = false, 
                        reason = "coTrongDonHang", 
                        message = danhSachBT
                    });
                }

                // Nếu không có biến thể nào có trong đơn hàng, tiến hành xóa
                var bienTheCanXoa = data.BienTheHangHoas.Where(b => ids.Contains(b.MaBienThe)).ToList();
                if (bienTheCanXoa.Any())
                {
                    data.BienTheHangHoas.DeleteAllOnSubmit(bienTheCanXoa);
                    data.SubmitChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể cần xóa" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa biến thể: " + ex.Message });
            }
        }
        #region 1
        #endregion

        #region NhaCungCap
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

        [HttpPost]
        public JsonResult ThemNhaCungCap(string TenNhaCungCap, string LienHe, string Email, string SoDienThoai)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(TenNhaCungCap))
                {
                    return Json(new { success = false, message = "Vui lòng nhập tên nhà cung cấp" });
                }

                // Tạo mã nhà cung cấp tự động
                string MaNhaCungCap;
                do
                {
                    MaNhaCungCap = "NCC" + GenerateRandomString(6);
                } while (data.NhaCungCaps.Any(n => n.MaNhaCungCap == MaNhaCungCap));

                // Tạo nhà cung cấp mới
                var nhaCungCap = new NhaCungCap
                {
                    MaNhaCungCap = MaNhaCungCap,
                    TenNhaCungCap = TenNhaCungCap,
                    LienHe = LienHe,
                    Email = Email,
                    SoDienThoai = SoDienThoai
                };

                data.NhaCungCaps.InsertOnSubmit(nhaCungCap);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm nhà cung cấp: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaNhaCungCap(string MaNhaCungCap, string TenNhaCungCap, string LienHe, string Email, string SoDienThoai)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(MaNhaCungCap) || string.IsNullOrEmpty(TenNhaCungCap))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
                }

                // Tìm nhà cung cấp cần cập nhật
                var nhaCungCap = data.NhaCungCaps.FirstOrDefault(n => n.MaNhaCungCap == MaNhaCungCap);
                if (nhaCungCap == null)
                {
                    return Json(new { success = false, message = "Nhà cung cấp không tồn tại" });
                }

                // Cập nhật thông tin
                nhaCungCap.TenNhaCungCap = TenNhaCungCap;
                nhaCungCap.LienHe = LienHe;
                nhaCungCap.Email = Email;
                nhaCungCap.SoDienThoai = SoDienThoai;
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật nhà cung cấp: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhaCungCap(string id)
        {
            try
            {
                // Tìm nhà cung cấp cần xóa
                var nhaCungCap = data.NhaCungCaps.FirstOrDefault(n => n.MaNhaCungCap == id);
                if (nhaCungCap == null)
                {
                    return Json(new { success = false, message = "Nhà cung cấp không tồn tại" });
                }

                // Kiểm tra xem nhà cung cấp có đang được sử dụng trong bảng NhapHang không
                var coDonNhap = data.NhapHangs.Any(nh => nh.MaNhaCungCap == id);
                if (coDonNhap)
                {
                    return Json(new { success = false, reason = "coDonNhap" });
                }

                // Xóa nhà cung cấp
                data.NhaCungCaps.DeleteOnSubmit(nhaCungCap);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa nhà cung cấp: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuNhaCungCap(List<string> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return Json(new { success = false, message = "Không có nhà cung cấp nào được chọn để xóa" });
                }

                // Kiểm tra và lưu danh sách nhà cung cấp có đơn nhập hàng
                var nhaCungCapCoDonNhap = new List<string>();
                foreach (var id in ids)
                {
                    if (string.IsNullOrEmpty(id)) continue;

                    var coDonNhap = data.NhapHangs.Any(nh => nh.MaNhaCungCap == id);
                    if (coDonNhap)
                    {
                        var tenNhaCungCap = data.NhaCungCaps
                            .Where(n => n.MaNhaCungCap == id)
                            .Select(n => n.TenNhaCungCap)
                            .FirstOrDefault();
                        nhaCungCapCoDonNhap.Add($"{id} ({tenNhaCungCap})");
                    }
                }

                // Nếu có nhà cung cấp đang được sử dụng
                if (nhaCungCapCoDonNhap.Any())
                {
                    var danhSachNCC = string.Join("<br/>", nhaCungCapCoDonNhap);
                    return Json(new { 
                        success = false, 
                        reason = "coDonNhap", 
                        message = danhSachNCC
                    });
                }

                // Xóa các nhà cung cấp nếu không có vấn đề
                var nhaCungCapCanXoa = data.NhaCungCaps.Where(n => ids.Contains(n.MaNhaCungCap));
                if (nhaCungCapCanXoa.Any())
                {
                    data.NhaCungCaps.DeleteAllOnSubmit(nhaCungCapCanXoa);
                    data.SubmitChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy nhà cung cấp cần xóa" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa nhà cung cấp: " + ex.Message });
            }
        }
        #endregion

        [HttpPost]
        public JsonResult TaoVoucher(Voucher voucher)
        {
            try
            {
                // Kiểm tra mã code đã tồn tại chưa
                if (data.Vouchers.Any(v => v.MaVoucherCode == voucher.MaVoucherCode))
                {
                    return Json(new { success = false, message = "Mã voucher code đã tồn tại" });
                }

                // Kiểm tra ngày bắt đầu phải lớn hơn ngày hiện tại
                if (voucher.NgayBatDau <= DateTime.Now.Date)
                {
                    return Json(new { success = false, message = "Ngày bắt đầu phải lớn hơn ngày hiện tại" });
                }

                // Kiểm tra ngày kết thúc phải lớn hơn ngày bắt đầu
                if (voucher.NgayKetThuc <= voucher.NgayBatDau)
                {
                    return Json(new { success = false, message = "Ngày kết thúc phải lớn hơn ngày bắt đầu" });
                }

                // Lấy mã voucher lớn nhất và tăng thêm 1
                int maxMaVoucher = data.Vouchers.Any() ? data.Vouchers.Max(v => v.MaVoucher) : 0;
                voucher.MaVoucher = maxMaVoucher + 1;

                // Thiết lập các giá trị mặc định
                voucher.SoLuongDaDung = 0;
                voucher.NgayTao = DateTime.Now;

                data.Vouchers.InsertOnSubmit(voucher);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm voucher: " + ex.Message });
            }
        }

        // Thêm controller để quản lý nhiều hình ảnh cho hàng hóa
        public ActionResult QuanLyHinhAnh(string maBienThe)
        {
            BienTheHangHoa bienThe = data.BienTheHangHoas.SingleOrDefault(bt => bt.MaBienThe == maBienThe);
            if (bienThe == null)
            {
                return RedirectToAction("DanhSachHangHoa");
            }

            ViewBag.BienThe = bienThe;
            ViewBag.HangHoa = bienThe.HangHoa;
            
            // Lấy danh sách hình ảnh bổ sung từ bảng HinhAnhHangHoa cho biến thể
            var danhSachHinhAnh = data.HinhAnhHangHoas.Where(h => h.MaBienThe == maBienThe).ToList();
            ViewBag.DanhSachHinhAnh = danhSachHinhAnh;

            return View();
        }

        [HttpPost]
        public JsonResult UploadHinhAnh(string MaBienThe, HttpPostedFileBase HinhAnh)
        {
            try
            {
                if (HinhAnh == null || HinhAnh.ContentLength <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn hình ảnh" });
                }

                BienTheHangHoa bienThe = data.BienTheHangHoas.SingleOrDefault(bt => bt.MaBienThe == MaBienThe);
                if (bienThe == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể sản phẩm" });
                }

                // Xử lý hình ảnh
                string extension = Path.GetExtension(HinhAnh.FileName);
                string fileName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(Server.MapPath("~/Content/img/hanghoa/"), fileName);
                
                // Đảm bảo thư mục tồn tại
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                
                // Lưu file
                HinhAnh.SaveAs(filePath);

                // Lưu vào bảng HinhAnhHangHoa cho biến thể
                HinhAnhHangHoa hinhAnh = new HinhAnhHangHoa
                {
                    MaHangHoa = bienThe.MaHangHoa,
                    MaBienThe = MaBienThe,
                    UrlAnh = fileName
                };
                data.HinhAnhHangHoas.InsertOnSubmit(hinhAnh);
                data.SubmitChanges();

                return Json(new { success = true, fileName = fileName, maHinhAnh = hinhAnh.MaHinhAnh });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UploadMultipleHinhAnh(string MaBienThe)
        {
            try
            {
                if (Request.Files.Count == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một hình ảnh" });
                }

                BienTheHangHoa bienThe = data.BienTheHangHoas.SingleOrDefault(bt => bt.MaBienThe == MaBienThe);
                if (bienThe == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể sản phẩm" });
                }

                // Đảm bảo thư mục tồn tại
                string uploadDir = Server.MapPath("~/Content/img/hanghoa/");
                Directory.CreateDirectory(uploadDir);
                
                // Đếm số lượng hình ảnh thành công
                int successCount = 0;
                List<string> failedFiles = new List<string>();
                
                // Xử lý từng file
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    try {
                        if (file != null && file.ContentLength > 0)
                        {
                            string extension = Path.GetExtension(file.FileName);
                            string fileName = Guid.NewGuid().ToString() + extension;
                            string filePath = Path.Combine(uploadDir, fileName);
                            
                            // Lưu file
                            file.SaveAs(filePath);
                            successCount++;
                            
                            // Lưu vào bảng HinhAnhHangHoa
                            var hinhAnh = new HinhAnhHangHoa
                            {
                                MaHangHoa = bienThe.MaHangHoa,
                                MaBienThe = MaBienThe,
                                UrlAnh = fileName
                            };
                            data.HinhAnhHangHoas.InsertOnSubmit(hinhAnh);
                        }
                    } catch (Exception ex) {
                        failedFiles.Add(file.FileName + " (" + ex.Message + ")");
                    }
                }
                
                // Lưu tất cả thay đổi vào database
                data.SubmitChanges();
                
                string message = "Đã tải lên " + successCount + " hình ảnh thành công";
                if (failedFiles.Count > 0) {
                    message += ". " + failedFiles.Count + " tệp không thành công: " + string.Join(", ", failedFiles);
                }
                
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaHinhAnh(string MaBienThe, string fileName, int? maHinhAnh = null)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return Json(new { success = false, message = "Tên file không hợp lệ" });
                }

                BienTheHangHoa bienThe = data.BienTheHangHoas.SingleOrDefault(bt => bt.MaBienThe == MaBienThe);
                if (bienThe == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể sản phẩm" });
                }

                string filePath = Path.Combine(Server.MapPath("~/Content/img/hanghoa/"), fileName);
                
                // Xóa trong bảng HinhAnhHangHoa
                if (maHinhAnh.HasValue)
                {
                    var hinhAnh = data.HinhAnhHangHoas.SingleOrDefault(h => h.MaHinhAnh == maHinhAnh.Value);
                    if (hinhAnh != null)
                    {
                        data.HinhAnhHangHoas.DeleteOnSubmit(hinhAnh);
                        data.SubmitChanges();
                    }
                }

                // Xóa file vật lý
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DatHinhAnhChinh(string MaBienThe, string fileName, int maHinhAnh)
        {
            try
            {
                BienTheHangHoa bienThe = data.BienTheHangHoas.SingleOrDefault(bt => bt.MaBienThe == MaBienThe);
                if (bienThe == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể sản phẩm" });
                }

                var hinhAnh = data.HinhAnhHangHoas.SingleOrDefault(h => h.MaHinhAnh == maHinhAnh);
                if (hinhAnh == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hình ảnh" });
                }

                string filePath = Path.Combine(Server.MapPath("~/Content/img/hanghoa/"), fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return Json(new { success = false, message = "Không tìm thấy file hình ảnh" });
                }

                // Lấy tất cả hình ảnh của biến thể
                var dsHinhAnh = data.HinhAnhHangHoas.Where(h => h.MaBienThe == MaBienThe).ToList();
                
                // Thiết lập hình ảnh được chọn là hình chính bằng cách đặt nó lên đầu danh sách
                // Trong thực tế, bạn có thể cần thêm một cột 'IsMainImage' vào bảng HinhAnhHangHoa và cập nhật nó
                hinhAnh.MaHinhAnh = maHinhAnh; // Đảm bảo mã hình ảnh không thay đổi
                
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Quản lý mô tả chi tiết hàng hóa
        public ActionResult MoTaChiTietHangHoa(string maHangHoa)
        {
            HangHoa hangHoa = data.HangHoas.SingleOrDefault(h => h.MaHangHoa == maHangHoa);
            if (hangHoa == null)
            {
                return RedirectToAction("DanhSachHangHoa");
            }
            
            ViewBag.HangHoa = hangHoa;
            
            // Lấy thông tin mô tả chi tiết của hàng hóa nếu có
            var moTaChiTiet = data.MoTaChiTietHangHoas.FirstOrDefault(m => m.MaHangHoa == maHangHoa);
            if (moTaChiTiet != null)
            {
                ViewBag.MoTaChiTiet = moTaChiTiet;
            }
            
            return View();
        }
        
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult LuuMoTaChiTiet(string MaHangHoa, string TieuDe, string NoiDung)
        {
            try
            {
                if (string.IsNullOrEmpty(MaHangHoa))
                {
                    return Json(new { success = false, message = "Mã sản phẩm không hợp lệ" });
                }
                
                HangHoa hangHoa = data.HangHoas.SingleOrDefault(h => h.MaHangHoa == MaHangHoa);
                if (hangHoa == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }
                
                // Kiểm tra xem đã có mô tả chi tiết chưa
                var moTaChiTiet = data.MoTaChiTietHangHoas.FirstOrDefault(m => m.MaHangHoa == MaHangHoa);
                
                if (moTaChiTiet == null)
                {
                    // Thêm mới
                    moTaChiTiet = new MoTaChiTietHangHoa
                    {
                        MaHangHoa = MaHangHoa,
                        TieuDe = TieuDe,
                        NoiDung = NoiDung,
                        NgayTao = DateTime.Now,
                        NgayCapNhat = DateTime.Now
                    };
                    
                    data.MoTaChiTietHangHoas.InsertOnSubmit(moTaChiTiet);
                }
                else
                {
                    // Cập nhật
                    moTaChiTiet.TieuDe = TieuDe;
                    moTaChiTiet.NoiDung = NoiDung;
                    moTaChiTiet.NgayCapNhat = DateTime.Now;
                }
                
                data.SubmitChanges();
                
                return Json(new { success = true, message = "Đã lưu mô tả chi tiết thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        
        [HttpPost]
        public JsonResult XoaMoTaChiTiet(string MaHangHoa)
        {
            try
            {
                if (string.IsNullOrEmpty(MaHangHoa))
                {
                    return Json(new { success = false, message = "Mã sản phẩm không hợp lệ" });
                }
                
                var moTaChiTiet = data.MoTaChiTietHangHoas.FirstOrDefault(m => m.MaHangHoa == MaHangHoa);
                if (moTaChiTiet == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy mô tả chi tiết" });
                }
                
                data.MoTaChiTietHangHoas.DeleteOnSubmit(moTaChiTiet);
                data.SubmitChanges();
                
                return Json(new { success = true, message = "Đã xóa mô tả chi tiết thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public ActionResult BaoCaoDoanhThu()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }

            // Lấy dữ liệu doanh thu
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Doanh thu hôm nay
            var doanhThuHomNay = data.DonHangs
                .Where(d => d.NgayTao.Value.Date == today && d.TrangThaiDonHang == "HoanThanh")
                .Sum(d => d.TongTien) ?? 0;

            // Doanh thu tuần này
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(6);
            var doanhThuTuanNay = data.DonHangs
                .Where(d => d.NgayTao.Value.Date >= startOfWeek && d.NgayTao.Value.Date <= endOfWeek && d.TrangThaiDonHang == "HoanThanh")
                .Sum(d => d.TongTien) ?? 0;

            // Doanh thu tháng này
            var doanhThuThangNay = data.DonHangs
                .Where(d => d.NgayTao.Value.Date >= startOfMonth && d.NgayTao.Value.Date <= endOfMonth && d.TrangThaiDonHang == "HoanThanh")
                .Sum(d => d.TongTien) ?? 0;

            // Doanh thu theo từng tháng trong năm nay
            var doanhThuTheoThang = new double[12];
            for (int i = 0; i < 12; i++)
            {
                var startOfMonthI = new DateTime(today.Year, i + 1, 1);
                var endOfMonthI = startOfMonthI.AddMonths(1).AddDays(-1);
                doanhThuTheoThang[i] = (double)(data.DonHangs
                    .Where(d => d.NgayTao.Value.Date >= startOfMonthI && d.NgayTao.Value.Date <= endOfMonthI && d.TrangThaiDonHang == "HoanThanh")
                    .Sum(d => d.TongTien) ?? 0);
            }

            // Doanh thu theo danh mục sản phẩm
            var doanhThuTheoDanhMuc = data.ChiTietDonHangs
                .Where(ct => ct.DonHang.TrangThaiDonHang == "HoanThanh")
                .GroupBy(ct => ct.BienTheHangHoa.HangHoa.DanhMuc)
                .Select(g => new
                {
                    TenDanhMuc = g.Key.TenDanhMuc,
                    DoanhThu = (double)g.Sum(ct => ct.SoLuong * ct.DonGia)
                })
                .OrderByDescending(x => x.DoanhThu)
                .Take(5)
                .ToList();

            // Truyền dữ liệu vào view
            ViewBag.DoanhThuHomNay = doanhThuHomNay;
            ViewBag.DoanhThuTuanNay = doanhThuTuanNay;
            ViewBag.DoanhThuThangNay = doanhThuThangNay;
            ViewBag.DoanhThuTheoThang = doanhThuTheoThang;
            ViewBag.DoanhThuTheoDanhMuc = doanhThuTheoDanhMuc;

            return View();
        }

        public ActionResult BaoCaoSanPham()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }

            // Thống kê tổng số sản phẩm
            ViewBag.TongSoSanPham = data.HangHoas.Count();
            
            // Thống kê số biến thể
            ViewBag.TongSoBienThe = data.BienTheHangHoas.Count();
            
            // Thống kê sản phẩm theo danh mục
            ViewBag.SanPhamTheoDanhMuc = data.HangHoas
                .GroupBy(h => h.DanhMuc)
                .Select(g => new
                {
                    TenDanhMuc = g.Key.TenDanhMuc,
                    SoLuong = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .ToList();
            
            // Thống kê sản phẩm theo thương hiệu
            ViewBag.SanPhamTheoThuongHieu = data.HangHoas
                .GroupBy(h => h.ThuongHieu)
                .Select(g => new
                {
                    TenThuongHieu = g.Key.TenThuongHieu,
                    SoLuong = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .ToList();
            
            // Top 10 sản phẩm bán chạy nhất
            ViewBag.SanPhamBanChayNhat = data.ChiTietDonHangs
                .GroupBy(ct => ct.BienTheHangHoa.HangHoa)
                .Select(g => new SanPhamBanChayModel
                {
                    TenHangHoa = g.Key.TenHangHoa,
                    SoLuongBan = g.Sum(ct => ct.SoLuong) ?? 0,
                    DoanhThu = (double)g.Sum(ct => ct.SoLuong * ct.DonGia)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(10)
                .ToList();
            
            // Sản phẩm tồn kho nhiều nhất
            ViewBag.SanPhamTonKhoNhieuNhat = data.BienTheHangHoas
                .OrderByDescending(bt => bt.SoLuongTonKho)
                .Take(10)
                .ToList();
            
            return View();
        }

        public ActionResult BaoCaoKhachHang()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index");
            }

            // Thống kê tổng số khách hàng
            ViewBag.TongSoKhachHang = data.KhachHangs.Count();
            
            // Khách hàng mới trong tháng
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            ViewBag.KhachHangMoiThangNay = data.KhachHangs
                .Count(k => k.NgayTao >= startOfMonth);
            
            // Khách hàng hoạt động và bị cấm
            ViewBag.KhachHangHoatDong = data.KhachHangs.Count(k => k.TrangThai == "HoatDong");
            ViewBag.SoKhachHangHoatDong = data.KhachHangs.Count(k => k.TrangThai == "HoatDong");
            ViewBag.SoKhachHangBiCam = data.KhachHangs.Count(k => k.TrangThai == "Cam");
            
            // Tỷ lệ chuyển đổi (khách hàng có ít nhất 1 đơn hàng / tổng số khách hàng)
            var khachHangCoDonHang = data.DonHangs
                .Select(d => d.MaKhachHang)
                .Distinct()
                .Count();
            var tongSoKhachHang = data.KhachHangs.Count();
            ViewBag.TyLeChuyenDoi = tongSoKhachHang > 0 
                ? Math.Round((double)khachHangCoDonHang / tongSoKhachHang * 100, 1) 
                : 0;
            
            // Top 10 khách hàng mua nhiều nhất - Sử dụng model TopKhachHangViewModel
            var topKhachHang = new List<TopKhachHangViewModel>();
            var donHangGrouped = data.DonHangs
                .Where(d => d.TrangThaiDonHang == "HoanThanh")
                .GroupBy(d => d.MaKhachHang)
                .Select(g => new
                {
                    MaKhachHang = g.Key,
                    TongDonHang = g.Count(),
                    TongChiTieu = (double)g.Sum(d => d.TongTien ?? 0)
                })
                .OrderByDescending(x => x.TongChiTieu)
                .Take(10)
                .ToList();

            foreach (var item in donHangGrouped)
            {
                var khachHang = data.KhachHangs.FirstOrDefault(k => k.MaKhachHang == item.MaKhachHang);
                if (khachHang != null)
                {
                    topKhachHang.Add(new TopKhachHangViewModel
                    {
                        MaKhachHang = item.MaKhachHang,
                        HoTen = khachHang.HoTen,
                        Email = khachHang.Email,
                        SoDienThoai = khachHang.SoDienThoai,
                        TongDonHang = item.TongDonHang,
                        TongChiTieu = item.TongChiTieu
                    });
                }
            }
            ViewBag.TopKhachHang = topKhachHang;
            
            // Thống kê số lượng khách hàng theo tháng trong năm nay
            var khachHangTheoThang = new int[12];
            for (int i = 0; i < 12; i++)
            {
                var startOfMonthI = new DateTime(DateTime.Today.Year, i + 1, 1);
                var endOfMonthI = startOfMonthI.AddMonths(1).AddDays(-1);
                khachHangTheoThang[i] = data.KhachHangs.Count(k => k.NgayTao >= startOfMonthI && k.NgayTao <= endOfMonthI);
            }
            ViewBag.KhachHangTheoThang = khachHangTheoThang;
            
            // Thống kê tương tác khách hàng
            ViewBag.TongSoDanhGia = data.DanhGias.Count();
            
            // Tính điểm đánh giá trung bình
            ViewBag.DiemDanhGiaTrungBinh = data.DanhGias.Any() 
                ? data.DanhGias.Average(d => d.SoSao) 
                : 0;
            
            // Số lượng sản phẩm yêu thích
            ViewBag.TongSoYeuThich = data.YeuThiches.Count();
            
            // Số lượt xem sản phẩm
            ViewBag.TongSoLuotXem = data.LichSuXems.Count();
            
            return View();
        }

        #region QUEN-MAT-KHAU
        // Hiển thị trang quên mật khẩu
        public ActionResult ForgotPassword()
        {
            return View();
        }
        
        // Tạo mật khẩu ngẫu nhiên 12 ký tự
        private string GenerateRandomPassword(int length = 12)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return result;
        }
        
        // Gửi email
        private bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MailMessage();
                message.From = new MailAddress(_emailAddress, _emailDisplayName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.IsBodyHtml = true;
                message.Body = body;

                var client = new SmtpClient(_emailHost, _emailPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_emailAddress, _emailPassword)
                };

                client.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi gửi email: " + ex.Message);
                return false;
            }
        }
        
        // Xử lý form yêu cầu đặt lại mật khẩu
        [HttpPost]
        public ActionResult RequestPasswordReset(string employeeId, string email)
        {
            // Kiểm tra mã nhân viên và email có khớp không
            var employee = data.NhanViens.FirstOrDefault(e => 
                e.MaNhanVien == employeeId && 
                e.Email == email && 
                e.TrangThai == "HoatDong");
                
            if (employee == null)
            {
                ViewBag.Error = "Mã nhân viên hoặc email không đúng. Vui lòng kiểm tra lại.";
                return View("ForgotPassword");
            }
            
            // Tạo mật khẩu tạm thời ngẫu nhiên 12 ký tự
            string tempPassword = GenerateRandomPassword(12);
            
            // Cập nhật mật khẩu tạm thời và thời gian hết hạn (5 phút)
            employee.MatKhau = EncryptPassword(tempPassword, "mysecretkey");
            employee.ExpiryTime = DateTime.Now.AddMinutes(5);
            data.SubmitChanges();
            
            // Tạo nội dung email
            string emailSubject = "PrimeTech - Mã xác nhận đặt lại mật khẩu";
            string emailBody = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                        .header {{ background-color: #2e7d32; color: white; padding: 10px 20px; border-radius: 5px 5px 0 0; }}
                        .content {{ padding: 20px; }}
                        .code {{ font-size: 24px; font-weight: bold; background-color: #f5f5f5; padding: 10px; border-radius: 5px; letter-spacing: 2px; text-align: center; }}
                        .footer {{ margin-top: 30px; font-size: 12px; color: #777; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>PrimeTech - Đặt lại mật khẩu</h2>
                        </div>
                        <div class='content'>
                            <p>Xin chào {employee.HoTen},</p>
                            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu từ tài khoản của bạn.</p>
                            <p>Đây là mã xác nhận để đặt lại mật khẩu của bạn:</p>
                            <div class='code'>{tempPassword}</div>
                            <p>Mã này sẽ hết hạn sau 5 phút.</p>
                            <p>Sau khi đăng nhập, hệ thống sẽ yêu cầu bạn đổi mật khẩu ngay lập tức.</p>
                            <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                            <p>Trân trọng,<br>Đội ngũ PrimeTech</p>
                        </div>
                        <div class='footer'>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
            
            // Gửi email
            bool emailSent = SendEmail(email, emailSubject, emailBody);
            
            if (!emailSent)
            {
                ViewBag.Error = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau.";
                return View("ForgotPassword");
            }
            
            // Hiển thị thông báo thành công và chuyển hướng về trang đăng nhập sau 5 giây
            ViewBag.Success = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư. Hệ thống sẽ tự động chuyển về trang đăng nhập sau 5 giây.";
            ViewBag.RedirectToLogin = true; // Cờ để kích hoạt chuyển hướng
            return View("ForgotPassword");
        }
        
        // Xử lý form đặt mật khẩu mới
        [HttpPost]
        public ActionResult SetNewPassword(string employeeId, string tempPassword, string newPassword, string confirmPassword)
        {
            // Kiểm tra các trường
            if (string.IsNullOrEmpty(tempPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                ViewBag.EmployeeId = employeeId;
                return View("ResetPassword");
            }
            
            // Kiểm tra mật khẩu mới và nhập lại có khớp không
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu nhập lại không khớp.";
                ViewBag.EmployeeId = employeeId;
                return View("ResetPassword");
            }
            
            // Tìm nhân viên
            var employee = data.NhanViens.FirstOrDefault(e => e.MaNhanVien == employeeId);
            if (employee == null)
            {
                ViewBag.Error = "Không tìm thấy thông tin nhân viên.";
                return View("ResetPassword");
            }
            
            // Kiểm tra mã xác nhận và thời gian hết hạn
            try
            {
                string decryptedStoredPassword = DecryptPassword(employee.MatKhau, "mysecretkey");
                
                if (decryptedStoredPassword != tempPassword)
                {
                    ViewBag.Error = "Mã xác nhận không đúng.";
                    ViewBag.EmployeeId = employeeId;
                    return View("ResetPassword");
                }
                
                // Kiểm tra thời gian hết hạn
                if (employee.ExpiryTime == null || employee.ExpiryTime < DateTime.Now)
                {
                    ViewBag.Error = "Mã xác nhận đã hết hạn. Vui lòng yêu cầu mã mới.";
                    return View("ForgotPassword");
                }
                
                // Cập nhật mật khẩu mới
                employee.MatKhau = EncryptPassword(newPassword, "mysecretkey");
                employee.ExpiryTime = null; // Xóa thời gian hết hạn
                data.SubmitChanges();
                
                ViewBag.Success = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.";
                return View("ResetPassword");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                ViewBag.EmployeeId = employeeId;
                return View("ResetPassword");
            }
        }
        #endregion

        // Trang yêu cầu đổi mật khẩu khi đăng nhập bằng mật khẩu tạm thời
        public ActionResult ChangePasswordRequired()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null || Session["RequirePasswordChange"] == null)
            {
                return RedirectToAction("Index");
            }
            
            return View();
        }
        
        // Xử lý đổi mật khẩu bắt buộc
        [HttpPost]
        public ActionResult ChangePasswordRequired(string newPassword, string confirmPassword)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null || Session["RequirePasswordChange"] == null)
            {
                return RedirectToAction("Index");
            }
            
            // Kiểm tra mật khẩu nhập vào
            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }
            
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu nhập lại không khớp";
                return View();
            }
            
            // Cập nhật mật khẩu mới
            string userId = Session["UserID"].ToString();
            var user = data.NhanViens.FirstOrDefault(u => u.MaNhanVien == userId);
            
            if (user != null)
            {
                user.MatKhau = EncryptPassword(newPassword, "mysecretkey");
                user.ExpiryTime = null; // Xóa thời gian hết hạn
                data.SubmitChanges();
                
                // Xóa cờ yêu cầu đổi mật khẩu trong session
                Session["RequirePasswordChange"] = null;
                
                ViewBag.Success = "Đổi mật khẩu thành công. Bạn sẽ được chuyển hướng đến trang chính trong 3 giây.";
                return View();
            }
            
            ViewBag.Error = "Có lỗi xảy ra. Vui lòng thử lại sau.";
            return View();
        }

        public ActionResult DanhSachTangKem()
        {
            List<KhuyenMaiTangKem> danhSachTangKem = data.KhuyenMaiTangKems.ToList();
            // Lấy danh sách hàng hóa để hiển thị trong dropdown
            ViewBag.DanhSachHangHoa = data.HangHoas.ToList();
            return View(danhSachTangKem);
        }

        [HttpPost]
        public JsonResult ThemTangKem(float GiaTriDonHangToiThieu, string MaHangHoaTangKem, int SoLuongTang, DateTime NgayBatDau, DateTime NgayKetThuc)
        {
            try
            {
                // Kiểm tra hàng hóa tặng có tồn tại không
                var hangHoaTang = data.HangHoas.FirstOrDefault(h => h.MaHangHoa == MaHangHoaTangKem);

                if (hangHoaTang == null)
                {
                    return Json(new { success = false, message = "Mã hàng hóa tặng kèm không tồn tại" });
                }

                // Kiểm tra số lượng và giá trị
                if (SoLuongTang <= 0 || GiaTriDonHangToiThieu <= 0)
                {
                    return Json(new { success = false, message = "Số lượng tặng và giá trị đơn hàng tối thiểu phải lớn hơn 0" });
                }

                // Kiểm tra ngày
                if (NgayBatDau >= NgayKetThuc)
                {
                    return Json(new { success = false, message = "Ngày kết thúc phải sau ngày bắt đầu" });
                }

                // Tạo mới khuyến mãi tặng kèm
                KhuyenMaiTangKem tangKem = new KhuyenMaiTangKem
                {
                    GiaTriDonHangToiThieu = GiaTriDonHangToiThieu,
                    MaHangHoaTangKem = MaHangHoaTangKem,
                    SoLuongTang = SoLuongTang,
                    NgayBatDau = NgayBatDau,
                    NgayKetThuc = NgayKetThuc
                };

                data.KhuyenMaiTangKems.InsertOnSubmit(tangKem);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SuaTangKem(int IDKM, float GiaTriDonHangToiThieu, string MaHangHoaTangKem, int SoLuongTang, DateTime NgayBatDau, DateTime NgayKetThuc)
        {
            try
            {
                // Kiểm tra hàng hóa tặng có tồn tại không
                var hangHoaTang = data.HangHoas.FirstOrDefault(h => h.MaHangHoa == MaHangHoaTangKem);

                if (hangHoaTang == null)
                {
                    return Json(new { success = false, message = "Mã hàng hóa tặng kèm không tồn tại" });
                }

                // Kiểm tra số lượng và giá trị
                if (SoLuongTang <= 0 || GiaTriDonHangToiThieu <= 0)
                {
                    return Json(new { success = false, message = "Số lượng tặng và giá trị đơn hàng tối thiểu phải lớn hơn 0" });
                }

                // Kiểm tra ngày
                if (NgayBatDau >= NgayKetThuc)
                {
                    return Json(new { success = false, message = "Ngày kết thúc phải sau ngày bắt đầu" });
                }

                // Cập nhật thông tin
                var tangKem = data.KhuyenMaiTangKems.FirstOrDefault(km => km.IDKM == IDKM);
                if (tangKem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });
                }

                tangKem.GiaTriDonHangToiThieu = GiaTriDonHangToiThieu;
                tangKem.MaHangHoaTangKem = MaHangHoaTangKem;
                tangKem.SoLuongTang = SoLuongTang;
                tangKem.NgayBatDau = NgayBatDau;
                tangKem.NgayKetThuc = NgayKetThuc;

                data.SubmitChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaTangKem(int id)
        {
            try
            {
                var tangKem = data.KhuyenMaiTangKems.FirstOrDefault(km => km.IDKM == id);
                if (tangKem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi" });
                }

                data.KhuyenMaiTangKems.DeleteOnSubmit(tangKem);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult XoaNhieuTangKem(List<int> ids)
        {
            try
            {
                var tangKemList = data.KhuyenMaiTangKems.Where(km => ids.Contains(km.IDKM)).ToList();
                if (tangKemList.Count == 0)
                {
                    return Json(new { success = false, message = "Không tìm thấy khuyến mãi nào để xóa" });
                }

                data.KhuyenMaiTangKems.DeleteAllOnSubmit(tangKemList);
                data.SubmitChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CapNhatThanhToanNhapHang(string MaNhapHang, string MaNhaCungCap, double ThanhToanThem, string TrangThai)
        {
            try
            {
                var nhapHang = data.NhapHangs.FirstOrDefault(n => n.MaNhapHang == MaNhapHang);
                if (nhapHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu nhập" });
                }

                // Cập nhật thông tin phiếu nhập
                nhapHang.MaNhaCungCap = MaNhaCungCap;
                
                // Cập nhật số tiền đã thanh toán bằng cách cộng thêm số tiền thanh toán mới
                double daThanhToan = nhapHang.DaThanhToan.HasValue ? nhapHang.DaThanhToan.Value : 0;
                double tongDaThanhToan = daThanhToan + ThanhToanThem;
                
                // Đảm bảo giá trị DaThanhToan hợp lệ
                if (tongDaThanhToan >= nhapHang.TongTien)
                {
                    nhapHang.DaThanhToan = nhapHang.TongTien;
                    nhapHang.TrangThai = "Dathanhtoanhet";
                }
                else if (tongDaThanhToan > 0)
                {
                    nhapHang.DaThanhToan = tongDaThanhToan;
                    nhapHang.TrangThai = "Dathanhtoanmotphan";
                }
                else
                {
                    nhapHang.DaThanhToan = 0;
                    nhapHang.TrangThai = "Chuathanhtoan";
                }
                
                // Nếu trạng thái được chỉ định rõ ràng, sử dụng giá trị đó thay vì tính toán
                if (!string.IsNullOrEmpty(TrangThai))
                {
                    nhapHang.TrangThai = TrangThai;
                }

                data.SubmitChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }


        // Cập nhật thông tin giao hàng
        [HttpPost]
        public ActionResult CapNhatGiaoHang(GiaoHang model)
        {
            try
            {
                var giaoHang = data.GiaoHangs.FirstOrDefault(g => g.MaGiaoHang == model.MaGiaoHang);
                if (giaoHang != null)
                {
                    // Kiểm tra xem đơn vị vận chuyển có thay đổi không
                    bool donViVanChuyenThayDoi = !string.IsNullOrEmpty(giaoHang.DonViVanChuyen) && 
                                                !string.IsNullOrEmpty(model.DonViVanChuyen) && 
                                                giaoHang.DonViVanChuyen != model.DonViVanChuyen;
                    
                    // Ghi log các giá trị trước khi cập nhật
                    System.Diagnostics.Debug.WriteLine("MaDiaChi trước khi cập nhật: " + giaoHang.MaDiaChi);
                    System.Diagnostics.Debug.WriteLine("MaDiaChi từ model: " + model.MaDiaChi);
                    System.Diagnostics.Debug.WriteLine("NgayGuiHang trước khi cập nhật: " + giaoHang.NgayGuiHang);
                    System.Diagnostics.Debug.WriteLine("NgayGuiHang từ model: " + model.NgayGuiHang);
                    System.Diagnostics.Debug.WriteLine("NgayNhanHang trước khi cập nhật: " + giaoHang.NgayNhanHang);
                    System.Diagnostics.Debug.WriteLine("NgayNhanHang từ model: " + model.NgayNhanHang);
                    System.Diagnostics.Debug.WriteLine("TrangThaiGiaoHang trước khi cập nhật: " + giaoHang.TrangThaiGiaoHang);
                    System.Diagnostics.Debug.WriteLine("TrangThaiGiaoHang từ model: " + model.TrangThaiGiaoHang);
                    
                    // Kiểm tra thay đổi trạng thái để cập nhật ngày tương ứng
                    bool trangThaiThayDoi = giaoHang.TrangThaiGiaoHang != model.TrangThaiGiaoHang;
                    
                    // Chỉ cập nhật đơn vị vận chuyển và trạng thái giao hàng
                    giaoHang.DonViVanChuyen = model.DonViVanChuyen;
                    giaoHang.TrangThaiGiaoHang = model.TrangThaiGiaoHang;
                    
                    // Nếu trạng thái thay đổi thành "Đang vận chuyển", cập nhật ngày gửi hàng là ngày hôm nay
                    if (trangThaiThayDoi && model.TrangThaiGiaoHang == "DangVanChuyen")
                    {
                        giaoHang.NgayGuiHang = DateTime.Now;
                    }
                    
                    // Nếu trạng thái thay đổi thành "Đã giao", cập nhật ngày nhận hàng là ngày hôm nay
                    if (trangThaiThayDoi && model.TrangThaiGiaoHang == "DaGiao")
                    {
                        giaoHang.NgayNhanHang = DateTime.Now;
                    }
                    
                    // Đảm bảo giữ nguyên các thông tin khác
                    // Giữ nguyên MaDiaChi từ database
                    if (!string.IsNullOrEmpty(model.MaDiaChi))
                    {
                        giaoHang.MaDiaChi = model.MaDiaChi;
                    }
                    
                    // Giữ nguyên các ngày từ model nếu được gửi lên (trừ trường hợp cập nhật tự động ở trên)
                    if (model.NgayGuiHang.HasValue && !(trangThaiThayDoi && model.TrangThaiGiaoHang == "DangVanChuyen"))
                    {
                        giaoHang.NgayGuiHang = model.NgayGuiHang;
                    }
                    
                    if (model.NgayNhanHang.HasValue && !(trangThaiThayDoi && model.TrangThaiGiaoHang == "DaGiao"))
                    {
                        giaoHang.NgayNhanHang = model.NgayNhanHang;
                    }
                    
                    // Kiểm tra và cập nhật trạng thái đơn hàng nếu cần
                    var donHang = data.DonHangs.FirstOrDefault(d => d.MaDonHang == model.MaDonHang);
                    if (donHang != null)
                    {
                        // Cập nhật trạng thái đơn hàng khi trạng thái giao hàng thay đổi
                        if (model.TrangThaiGiaoHang == "DangVanChuyen" && donHang.TrangThaiDonHang == "DangXuLy")
                        {
                            donHang.TrangThaiDonHang = "DangGiao";
                        }
                        else if (model.TrangThaiGiaoHang == "DaGiao" && donHang.TrangThaiThanhToan == "DaThanhToan")
                        {
                            // Nếu đã giao hàng và đã thanh toán thì cập nhật trạng thái đơn hàng thành "HoanThanh"
                            donHang.TrangThaiDonHang = "HoanThanh";
                        }
                    }
                    
                    // Ghi log giá trị sau khi cập nhật
                    System.Diagnostics.Debug.WriteLine("MaDiaChi sau khi cập nhật: " + giaoHang.MaDiaChi);
                    System.Diagnostics.Debug.WriteLine("NgayGuiHang sau khi cập nhật: " + giaoHang.NgayGuiHang);
                    System.Diagnostics.Debug.WriteLine("NgayNhanHang sau khi cập nhật: " + giaoHang.NgayNhanHang);
                    
                    data.SubmitChanges();
                    
                    TempData["SuccessMessage"] = "Cập nhật thông tin giao hàng thành công";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin giao hàng";
                }
                
                return RedirectToAction("ChiTietDonBanHang", new { id = model.MaDonHang });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi cập nhật thông tin giao hàng: " + ex.Message;
                return RedirectToAction("ChiTietDonBanHang", new { id = model.MaDonHang });
            }
        }

    }
}
