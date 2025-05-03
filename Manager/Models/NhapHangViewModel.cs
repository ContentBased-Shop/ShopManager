using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Manager.Models
{
    public class NhapHangViewModel
    {
        public string MaNhapHang { get; set; }
        public string MaNhaCungCap { get; set; }
        public string MaNhanVien { get; set; }
        public decimal TongTien { get; set; }
        public decimal? DaThanhToan { get; set; }
        public string TrangThai { get; set; }
        public DateTime? NgayNhap { get; set; }
        public List<ChiTietNhapHangViewModel> ChiTietNhapHangs { get; set; }
    }

    public class ChiTietNhapHangViewModel
    {
        public string MaChiTietNhapHang { get; set; }
        public string MaNhapHang { get; set; }
        public string MaBienThe { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }
} 