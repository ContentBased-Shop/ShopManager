using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Manager.Models
{
    public class ChiTietDonHangViewModel
    {
        public DonHang DonHang { get; set; }
        public List<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public ThanhToan ThanhToan { get; set; }
        public GiaoHang GiaoHang { get; set; }
        public DiaChiKhachHang DiaChiGiaoHang { get; set; }
    }


}