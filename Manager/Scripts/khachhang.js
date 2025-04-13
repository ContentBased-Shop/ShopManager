$(document).ready(function () {
    // Xử lý tìm kiếm
    function searchCustomers() {
        var searchText = $('#searchInput').val().toLowerCase();
        $('#customerTable tbody tr').each(function () {
            var maKH = $(this).find('.ma-khach-hang').text().toLowerCase();
            var username = $(this).find('.ten-dang-nhap').text().toLowerCase();
            var hoTen = $(this).find('.ho-ten').text().toLowerCase();
            var email = $(this).find('.email').text().toLowerCase();
            var sdt = $(this).find('.sdt').text().toLowerCase();

            if (maKH.includes(searchText) || username.includes(searchText) ||
                hoTen.includes(searchText) || email.includes(searchText) ||
                sdt.includes(searchText)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    }

    // Sự kiện khi nhập vào ô tìm kiếm
    $('#searchInput').on('input', function () {
        searchCustomers();
    });

    // Xử lý checkbox "Chọn tất cả"
    $('#checkAll').change(function () {
        $('.check-item').prop('checked', $(this).prop('checked'));
        toggleDeleteButton();
    });

    // Xử lý khi checkbox đơn lẻ thay đổi
    $('.check-item').change(function () {
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

    // Hàm xóa nhiều khách hàng
    window.xoaNhieu = function () {
        var selectedIds = [];
        $('.check-item:checked').each(function () {
            selectedIds.push($(this).val());
        });

        if (selectedIds.length === 0) {
            alert('Vui lòng chọn ít nhất một khách hàng để xóa!');
            return;
        }

        $('#deleteMessage').text("Bạn có chắc chắn muốn xóa " + selectedIds.length + " khách hàng đã chọn?");
        $('#confirmDeleteModal').modal('show');

        $('#confirmDelete').off('click').on('click', function () {
            $.ajax({
                url: '/Home/XoaNhieuKhachHang',
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
                        if (response.reason === "coDonHang") {
                            $('#customerInUseList').html(response.message);
                            $('#customerInUseModal').modal('show');
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

    // Xóa một khách hàng
    $('.btn-delete').click(function () {
        var maKH = $(this).data('id');
        var hoTen = $(this).data('name');

        if (!maKH) return;

        $('#deleteMessage').text("Bạn có chắc chắn muốn xóa khách hàng " + hoTen + "?");
        $('#confirmDeleteModal').modal('show');

        $('#confirmDelete').off('click').on('click', function () {
            $.ajax({
                url: '/Home/XoaMotKhachHang',
                type: 'POST',
                data: { id: maKH },
                success: function (response) {
                    $('#confirmDeleteModal').modal('hide');
                    if (response.success) {
                        $('#deleteSuccessModal').modal('show');
                        setTimeout(function () {
                            location.reload();
                        }, 1500);
                    } else {
                        if (response.reason === "coDonHang") {
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

    // Thêm khách hàng mới
    $('#saveCustomer').click(function () {
        // Kiểm tra dữ liệu
        var isValid = true;

        const tenDangNhapInput = $('#tenDangNhap');
        if (tenDangNhapInput.val().trim() === "") {
            $('#tenDangNhapError').show();
            tenDangNhapInput.addClass("is-invalid");
            isValid = false;
        } else {
            $('#tenDangNhapError').hide();
            tenDangNhapInput.removeClass("is-invalid");
        }

        const matKhauInput = $('#matKhau');
        if (matKhauInput.val().trim() === "") {
            $('#matKhauError').show();
            matKhauInput.addClass("is-invalid");
            isValid = false;
        } else {
            $('#matKhauError').hide();
            matKhauInput.removeClass("is-invalid");
        }

        const emailInput = $('#email');
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (emailInput.val().trim() === "" || !emailRegex.test(emailInput.val().trim())) {
            $('#emailError').show();
            emailInput.addClass("is-invalid");
            isValid = false;
        } else {
            $('#emailError').hide();
            emailInput.removeClass("is-invalid");
        }

        if (!isValid) return;

        $.ajax({
            url: '/Home/TaoKhachHang',
            type: 'POST',
            data: {
                TenDangNhap: $('#tenDangNhap').val(),
                HoTen: $('#hoTen').val(),
                MatKhau: $('#matKhau').val(),
                Email: $('#email').val(),
                SoDienThoai: $('#soDienThoai').val(),
                DiaChi: $('#diaChi').val(),
                TrangThai: $('#trangThai').val()
            },
            success: function (response) {
                if (response.success) {
                    $('#createCustomerModal').modal('hide');
                    $('#addSuccessModal').modal('show');
                    setTimeout(function () {
                        location.reload();
                    }, 1500);
                } else {
                    alert(response.message || "Thêm khách hàng thất bại");
                }
            },
            error: function () {
                alert("Lỗi khi gọi server.");
            }
        });
    });

    // Xử lý sự kiện khi nhấn nút Sửa
    $('.btn-edit').click(function () {
        var maKhachHang = $(this).data('id');
        var tenDangNhap = $(this).data('username');
        var hoTen = $(this).data('name');
        var email = $(this).data('email');
        var soDienThoai = $(this).data('phone');
        var trangThai = $(this).data('status');

        $('#editMaKhachHang').val(maKhachHang);
        $('#editTenDangNhap').val(tenDangNhap);
        $('#editHoTen').val(hoTen);
        $('#editEmail').val(email);
        $('#editSoDienThoai').val(soDienThoai);
        $('#editTrangThai').val(trangThai);

        // Lấy địa chỉ của khách hàng
        $.ajax({
            url: '/Home/LayDiaChiKhachHang',
            type: 'GET',
            data: { id: maKhachHang },
            success: function (diaChi) {
                $('#editDiaChi').val(diaChi);
            }
        });
    });

    // Cập nhật khách hàng
    $('#updateCustomer').click(function () {
        // Kiểm tra dữ liệu
        var isValid = true;

        const emailInput = $('#editEmail');
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (emailInput.val().trim() === "" || !emailRegex.test(emailInput.val().trim())) {
            $('#editEmailError').show();
            emailInput.addClass("is-invalid");
            isValid = false;
        } else {
            $('#editEmailError').hide();
            emailInput.removeClass("is-invalid");
        }

        if (!isValid) return;

        $.ajax({
            url: '/Home/SuaKhachHang',
            type: 'POST',
            data: {
                MaKhachHang: $('#editMaKhachHang').val(),
                HoTen: $('#editHoTen').val(),
                Email: $('#editEmail').val(),
                SoDienThoai: $('#editSoDienThoai').val(),
                DiaChi: $('#editDiaChi').val(),
                TrangThai: $('#editTrangThai').val(),
                MatKhau: $('#editMatKhau').val()
            },
            success: function (response) {
                if (response.success) {
                    $('#editCustomerModal').modal('hide');
                    $('#editSuccessModal').modal('show');
                    setTimeout(function () {
                        location.reload();
                    }, 1500);
                } else {
                    alert(response.message || "Cập nhật khách hàng thất bại");
                }
            },
            error: function () {
                alert("Lỗi khi gọi server.");
            }
        });
    });
});