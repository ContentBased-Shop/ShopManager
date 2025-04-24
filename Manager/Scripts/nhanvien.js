$(document).ready(function () {
    // Xử lý tìm kiếm
    function searchEmployees() {
        var searchText = $('#searchInput').val().toLowerCase();
        var roleFilter = $('#roleFilter').val();
        var statusFilter = $('#statusFilter').val();

        $('#employeeTable tbody tr').each(function () {
            var maNhanVien = $(this).find('.ma-nhanvien').text().toLowerCase();
            var tenDangNhap = $(this).find('.ten-dangnhap').text().toLowerCase();
            var hoTen = $(this).find('.ho-ten').text().toLowerCase();
            var email = $(this).find('.email').text().toLowerCase();
            var soDienThoai = $(this).find('.so-dien-thoai').text().toLowerCase();

            var vaiTro = $(this).find('td:nth-child(7) .badge').text();
            var trangThai = $(this).find('td:nth-child(8) .badge').text() == "Hoạt động" ? "Cam" : "HoatDong";

            var matchSearch = maNhanVien.includes(searchText) ||
                tenDangNhap.includes(searchText) ||
                hoTen.includes(searchText) ||
                email.includes(searchText) ||
                soDienThoai.includes(searchText);

            var matchRole = roleFilter === "" || vaiTro === roleFilter;
            var matchStatus = statusFilter === "" || trangThai === statusFilter;

            if (matchSearch && matchRole && matchStatus) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    }

    // Sự kiện khi nhập vào ô tìm kiếm
    $('#searchInput').on('input', function () {
        searchEmployees();
    });

    // Sự kiện khi thay đổi bộ lọc vai trò
    $('#roleFilter').change(function () {
        searchEmployees();
    });

    // Sự kiện khi thay đổi bộ lọc trạng thái
    $('#statusFilter').change(function () {
        searchEmployees();
    });

    // Sự kiện khi nhấn nút tìm kiếm
    $('#btnSearch').click(function () {
        searchEmployees();
    });

    // Sự kiện khi nhấn nút đặt lại
    $('#btnReset').click(function () {
        $('#searchInput').val('');
        $('#roleFilter').val('');
        $('#statusFilter').val('');
        searchEmployees();
    });

    // Xử lý checkbox "Chọn tất cả"
    $('#checkAll').change(function () {
        $('.check-item').prop('checked', $(this).prop('checked'));
        toggleDeleteButton();
    });

    // Xử lý khi checkbox đơn lẻ thay đổi
    $(document).on('change', '.check-item', function () {
        toggleDeleteButton();
        // Nếu tất cả checkbox con được chọn, checkbox "Chọn tất cả" cũng được chọn
        $('#checkAll').prop('checked', $('.check-item:checked').length === $('.check-item').length);
    });

    // Hàm hiển thị/ẩn nút xóa
    function toggleDeleteButton() {
        if ($('.check-item:checked').length > 0) {
            $('#btnXoaNhieu').removeClass('d-none');
        } else {
            $('#btnXoaNhieu').addClass('d-none');
        }
    }

    // Hàm xóa nhiều nhân viên
    window.xoaNhieu = function () {
        var selectedIds = [];
        $('.check-item:checked').each(function () {
            selectedIds.push($(this).val());
        });

        if (selectedIds.length === 0) {
            alert('Vui lòng chọn ít nhất một nhân viên để xóa!');
            return;
        }

        $('#deleteMessage').text("Bạn có chắc chắn muốn xóa " + selectedIds.length + " nhân viên đã chọn?");
        $('#confirmDeleteModal').modal('show');

        $('#confirmDelete').off('click').on('click', function () {
            $.ajax({
                url: '/Home/XoaNhieuNhanVien',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ ids: selectedIds }),
                success: function (response) {
                    $('#confirmDeleteModal').modal('hide');
                    if (response.success) {
                        $('#deleteSuccessModal').modal('show');
                        setTimeout(function () {
                            location.reload();
                        }, 1500);
                    } else {
                        if (response.reason === "coDonNhap") {
                            $('#employeeInUseList').html(response.message);
                            $('#employeeInUseModal').modal('show');
                        } else {
                            alert(response.message || "Xóa thất bại");
                        }
                    }
                },
                error: function () {
                    alert("Lỗi khi gọi server.");
                }
            });
        });
    }

    // Xử lý nút xóa (một nhân viên)
    $('.btn-delete').click(function () {
        var maNV = $(this).data('id');
        var tenNV = $(this).data('name');

        $('#deleteMessage').text("Bạn có chắc chắn muốn xóa nhân viên '" + tenNV + "'?");
        $('#confirmDeleteModal').modal('show');

        $('#confirmDelete').off('click').on('click', function () {
            $.ajax({
                url: '/Home/XoaMotNhanVien',
                type: 'POST',
                data: { id: maNV },
                success: function (response) {
                    $('#confirmDeleteModal').modal('hide');
                    if (response.success) {
                        $('#deleteSuccessModal').modal('show');
                        setTimeout(function () {
                            location.reload();
                        }, 1500);
                    } else {
                        if (response.reason === "coDonNhap") {
                            $('#cannotDeleteModal').modal('show');
                        } else {
                            alert(response.message || "Xóa thất bại");
                        }
                    }
                },
                error: function () {
                    alert("Lỗi khi gọi server.");
                }
            });
        });
    });

    // Xử lý nút xem thông tin
    $('.btn-detail').click(function () {
        var maNV = $(this).data('id');
        $.ajax({
            url: '/Home/LayThongTinNhanVien',
            type: 'GET',
            data: { id: maNV },
            success: function (data) {
                if (data) {
                    $('#viewMaNhanVien').text(data.MaNhanVien);
                    $('#viewTenDangNhap').text(data.TenDangNhap);
                    $('#viewHoTen').text(data.HoTen);
                    $('#viewEmail').text(data.Email);
                    $('#viewSoDienThoai').text(data.SoDienThoai || "Chưa cung cấp");
                    $('#viewDiaChi').text(data.DiaChi || "Chưa cung cấp");
                    $('#viewVaiTro').text(data.VaiTro);
                    $('#viewTrangThai').text(data.TrangThai === "HoatDong" ? "Hoạt động" : "Cấm");
                    $('#viewNgayTao').text(new Date(parseInt(data.NgayTao.substr(6))).toLocaleDateString());
                } else {
                    alert("Không tìm thấy thông tin nhân viên");
                }
            },
            error: function () {
                alert("Lỗi khi tải thông tin nhân viên");
            }
        });
    });

    // Kiểm tra mật khẩu mạnh
    function isStrongPassword(password) {
        if (password.length < 8) return false;
        // Kiểm tra có ít nhất 1 chữ hoa
        if (!/[A-Z]/.test(password)) return false;
        // Kiểm tra có ít nhất 1 chữ thường
        if (!/[a-z]/.test(password)) return false;
        // Kiểm tra có ít nhất 1 số
        if (!/[0-9]/.test(password)) return false;
        // Kiểm tra có ít nhất 1 ký tự đặc biệt
        if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) return false;
        return true;
    }

    // Xử lý sự kiện lưu nhân viên mới
    $('#saveEmployee').click(function () {
        // Kiểm tra dữ liệu
        var isValid = true;

        // Kiểm tra tên đăng nhập
        if ($('#tenDangNhap').val().trim() === "") {
            $('#tenDangNhapError').show();
            isValid = false;
        } else {
            $('#tenDangNhapError').hide();
        }

        // Kiểm tra họ tên
        if ($('#hoTen').val().trim() === "") {
            $('#hoTenError').show();
            isValid = false;
        } else {
            $('#hoTenError').hide();
        }

        // Kiểm tra email
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test($('#email').val().trim())) {
            $('#emailError').show();
            isValid = false;
        } else {
            $('#emailError').hide();
        }

        // Kiểm tra mật khẩu
        if ($('#matKhau').val() === "") {
            $('#matKhauError').text("Vui lòng nhập mật khẩu.");
            $('#matKhauError').show();
            isValid = false;
        } else if (!isStrongPassword($('#matKhau').val())) {
            $('#matKhauError').text("Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
            $('#matKhauError').show();
            isValid = false;
        } else {
            $('#matKhauError').hide();
        }

        // Kiểm tra nhập lại mật khẩu
        if ($('#matKhau').val() !== $('#nhapLaiMatKhau').val()) {
            $('#nhapLaiMatKhauError').show();
            isValid = false;
        } else {
            $('#nhapLaiMatKhauError').hide();
        }

        if (!isValid) return;

        $.ajax({
            url: '/Home/TaoNhanVien',
            type: 'POST',
            data: {
                TenDangNhap: $('#tenDangNhap').val(),
                HoTen: $('#hoTen').val(),
                DiaChi: $('#diaChi').val(),
                MatKhau: $('#matKhau').val(),
                Email: $('#email').val(),
                SoDienThoai: $('#soDienThoai').val(),
                TrangThai: $('#trangThai').val(),
                VaiTro: $('#vaiTro').val()
            },
            success: function (response) {
                if (response.success) {
                    $('#createEmployeeModal').modal('hide');
                    $('#addSuccessModal').modal('show');
                    setTimeout(function () {
                        location.reload();
                    }, 1500);
                } else {
                    alert(response.message || "Thêm nhân viên thất bại");
                }
            },
            error: function () {
                alert("Lỗi khi gọi server.");
            }
        });
    });

    // Xử lý sự kiện khi nhấn nút Sửa
    $('.btn-edit').click(function () {
        var maNhanVien = $(this).data('id');
        var tenDangNhap = $(this).data('username');
        var hoTen = $(this).data('name');
        var diaChi = $(this).data('address');
        var email = $(this).data('email');
        var soDienThoai = $(this).data('phone');
        var trangThai = $(this).data('status');
        var vaiTro = $(this).data('role');

        $('#editMaNhanVien').val(maNhanVien);
        $('#editTenDangNhap').val(tenDangNhap);
        $('#editHoTen').val(hoTen);
        $('#editDiaChi').val(diaChi);
        $('#editEmail').val(email);
        $('#editSoDienThoai').val(soDienThoai);
        $('#editTrangThai').val(trangThai);
        $('#editVaiTro').val(vaiTro);

        // Xóa giá trị mật khẩu
        $('#editMatKhau').val('');
        $('#editNhapLaiMatKhau').val('');
        $('#editNhapLaiMatKhauError').hide();
    });

    // Xử lý sự kiện khi nhấn nút Lưu trong modal chỉnh sửa
    $('#updateEmployee').click(function () {
        // Kiểm tra dữ liệu
        var isValid = true;

        // Kiểm tra họ tên
        if ($('#editHoTen').val().trim() === "") {
            $('#editHoTenError').show();
            isValid = false;
        } else {
            $('#editHoTenError').hide();
        }

        // Kiểm tra email
        var emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test($('#editEmail').val().trim())) {
            $('#editEmailError').show();
            isValid = false;
        } else {
            $('#editEmailError').hide();
        }

        // Kiểm tra mật khẩu mới nếu đã nhập
        if ($('#editMatKhau').val() !== '') {
            if (!isStrongPassword($('#editMatKhau').val())) {
                $('#editNhapLaiMatKhauError').text("Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
                $('#editNhapLaiMatKhauError').show();
                isValid = false;
            } else if ($('#editMatKhau').val() !== $('#editNhapLaiMatKhau').val()) {
                $('#editNhapLaiMatKhauError').text("Mật khẩu không khớp.");
                $('#editNhapLaiMatKhauError').show();
                isValid = false;
            } else {
                $('#editNhapLaiMatKhauError').hide();
            }
        }

        if (!isValid) return;

        $.ajax({
            url: '/Home/SuaNhanVien',
            type: 'POST',
            data: {
                MaNhanVien: $('#editMaNhanVien').val(),
                HoTen: $('#editHoTen').val(),
                DiaChi: $('#editDiaChi').val(),
                Email: $('#editEmail').val(),
                SoDienThoai: $('#editSoDienThoai').val(),
                TrangThai: $('#editTrangThai').val(),
                VaiTro: $('#editVaiTro').val(),
                MatKhau: $('#editMatKhau').val()
            },
            success: function (response) {
                if (response.success) {
                    $('#editEmployeeModal').modal('hide');
                    $('#editSuccessModal').modal('show');
                    setTimeout(function () {
                        location.reload();
                    }, 1500);
                } else {
                    alert(response.message || "Cập nhật nhân viên thất bại");
                }
            },
            error: function () {
                alert("Lỗi khi gọi server.");
            }
        });
    });
});