using System;
using System.Collections.Generic;

public class VoucherModel
{
    public int? MaVoucher { get; set; }
    public string MaVoucherCode { get; set; }
    public string TenVoucher { get; set; }
    public string MoTa { get; set; }
    public string LoaiGiamGia { get; set; }
    public double? GiaTriGiamGia { get; set; }
    public double? DonHangToiThieu { get; set; }
    public int SoLuong { get; set; }
    public int SoLuongDaDung { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string TrangThai { get; set; }
    public bool? IsPublic { get; set; }
    public DateTime? NgayTao { get; set; }
    public List<string> DanhSachKhachHang { get; set; }
} 