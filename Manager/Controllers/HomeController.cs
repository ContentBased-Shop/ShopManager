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
        public JsonResult TaoHangHoa(string TenHangHoa, string MaDanhMuc, string MaThuongHieu, string MoTa)
        {
            try
            {
                // Tạo mã hàng hóa tự động
                string MaHangHoa;
                do
                {
                    MaHangHoa = "HH" + GenerateRandomString(6);
                } while (data.HangHoas.Any(h => h.MaHangHoa == MaHangHoa));

                var hangHoa = new HangHoa
                {
                    MaHangHoa = MaHangHoa,
                    TenHangHoa = TenHangHoa,
                    MaDanhMuc = MaDanhMuc,
                    MaThuongHieu = MaThuongHieu,
                    MoTa = MoTa,
                    DanhGiaTrungBinh = 0,
                    NgayTao = DateTime.Now
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
        public JsonResult SuaHangHoa(string MaHangHoa, string TenHangHoa, string MaDanhMuc, string MaThuongHieu, string MoTa)
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

                // Cập nhật thông tin mới
                hangHoa.TenHangHoa = TenHangHoa;
                hangHoa.MaDanhMuc = MaDanhMuc;
                hangHoa.MaThuongHieu = MaThuongHieu;
                hangHoa.MoTa = MoTa;

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
                    DiaChi = DiaChi,
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
                khachHang.DiaChi = DiaChi;
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
            return khachHang?.DiaChi ?? "";
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

            var viewModel = new ChiTietDonHangViewModel
            {
                DonHang = donHang,
                ChiTietDonHangs = chiTiet,
                ThanhToan = thanhToan,
                GiaoHang = giaoHang
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

                // Tạo phiếu nhập mới
                var nhapHang = new NhapHang
                {
                    MaNhapHang = maNhapHang,
                    MaNhaCungCap = model.MaNhaCungCap,
                    MaNhanVien = maNhanVien,
                    TongTien = (double)model.TongTien,
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
                    NgayNhap = nhapHang.NgayNhap?.ToString("dd/MM/yyyy HH:mm")
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CapNhatNhapHang(string MaNhapHang, string MaNhaCungCap)
        {
            try
            {
                var nhapHang = data.NhapHangs.FirstOrDefault(n => n.MaNhapHang == MaNhapHang);
                if (nhapHang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phiếu nhập" });
                }

                nhapHang.MaNhaCungCap = MaNhaCungCap;
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
        public JsonResult TaoBienTheHangHoa(string MaHangHoa, string MauSac, string DungLuong, decimal Gia, int SoLuongTonKho)
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
                    GiaGoc = (double)Gia,
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
        public JsonResult SuaBienTheHangHoa(string MaBienThe, string MauSac, string DungLuong, decimal Gia, int SoLuongTonKho)
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
                bienThe.GiaGoc = (double)Gia;
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
    }
}
