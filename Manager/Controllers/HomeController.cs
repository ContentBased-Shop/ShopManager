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

                // Mã hóa mật khẩu (trong thực tế nên sử dụng thư viện mã hóa tốt hơn)
                string matKhauHash = MatKhau; // Trong thực tế, đây là nơi bạn sẽ hash mật khẩu

                var khachHang = new KhachHang
                {
                    MaKhachHang = MaKhachHang,
                    TenDangNhap = TenDangNhap,
                    HoTen = HoTen,
                    MatKhauHash = matKhauHash,
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
                    // Mã hóa mật khẩu (trong thực tế nên sử dụng thư viện mã hóa tốt hơn)
                    khachHang.MatKhauHash = MatKhau; // Trong thực tế, đây là nơi bạn sẽ hash mật khẩu
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
                    Gia = Gia,
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
                bienThe.Gia = Gia;
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

    }
}
