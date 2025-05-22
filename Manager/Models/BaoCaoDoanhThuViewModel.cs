using System;
using System.Collections.Generic;

namespace Manager.Models
{
    public class BaoCaoDoanhThuViewModel
    {
        public List<ThongKeDoanhThuModel> ChiTietDoanhThuTheoDanhMuc { get; set; }
        public double DoanhThuHomNay { get; set; }
        public double LoiNhuanHomNay { get; set; }
        public double DoanhThuThangNay { get; set; }
        public double LoiNhuanThangNay { get; set; }
        public List<double> DoanhThuTheoThang { get; set; }
        public List<double> LoiNhuanTheoThang { get; set; }
        public List<double> TySuatLoiNhuanTheoThang { get; set; }
    }
} 