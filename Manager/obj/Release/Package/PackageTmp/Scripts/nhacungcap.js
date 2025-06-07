$(document).ready(function () {
    // Xử lý tìm kiếm
    $("#searchInput").on("keyup", function () {
        var value = $(this).val().toLowerCase();
        $("#supplierTable tbody tr").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });

    // Xử lý checkbox "Chọn tất cả"
    $("#checkAll").change(function () {
        $(".check-item").prop("checked", $(this).prop("checked"));
        toggleDeleteButton();
    });

    // Xử lý khi checkbox đơn lẻ thay đổi
    $(".check-item").change(function () {
        toggleDeleteButton();
        // Nếu tất cả checkbox con được chọn, checkbox "Chọn tất cả" cũng được chọn
        $("#checkAll").prop("checked", $(".check-item:checked").length === $(".check-item").length);
    });

    // Hàm hiển thị/ẩn nút xóa
    function toggleDeleteButton() {
        if ($(".check-item:checked").length > 0) {
            $("#btnXoaNhieu").removeClass("d-none");
        } else {
            $("#btnXoaNhieu").addClass("d-none");
        }
    }

    // Hàm xóa nhiều nhà cung cấp
    window.xoaNhieu = function () {
        var selectedIds = [];
        $(".check-item:checked").each(function () {
            selectedIds.push($(this).val());
        });

        if (selectedIds.length === 0) {
            alert("Vui lòng chọn ít nhất một nhà cung cấp để xóa!");
            return;
        }

        $("#deleteMessage").text("Bạn có chắc chắn muốn xóa " + selectedIds.length + " nhà cung cấp đã chọn?");
        $("#confirmDeleteModal").modal("show");

        $("#confirmDelete").off("click").on("click", function () {
            $.ajax({
                url: '/Home/XoaNhieuNhaCungCap',
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ ids: selectedIds }),
                success: function (response) {
                    $("#confirmDeleteModal").modal("hide");
                    if (response.success) {
                        $("#successMessage").text("Xóa nhà cung cấp thành công!");
                        $("#successModal").modal("show");
                        setTimeout(function () {
                            location.reload();
                        }, 1500);
                    } else {
                        if (response.reason === "coDonNhap") {
                            $("#supplierInUseList").html(response.message);
                            $("#supplierInUseModal").modal("show");
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
    };

    // Xử lý khi nhấn nút xóa
    $(".btn-delete").click(function () {
        var maNhaCungCap = $(this).data("id");
        var tenNhaCungCap = $(this).data("name");

        $("#deleteMessage").text("Bạn có chắc chắn muốn xóa nhà cung cấp '" + tenNhaCungCap + "'?");

        $("#confirmDelete").off("click").on("click", function () {
            $.ajax({
                url: '/Home/XoaNhaCungCap',
                type: "POST",
                data: { id: maNhaCungCap },
                success: function (response) {
                    $("#confirmDeleteModal").modal("hide");
                    if (response.success) {
                        $("#successMessage").text("Xóa nhà cung cấp thành công!");
                        $("#successModal").modal("show");
                        setTimeout(function () {
                            location.reload();
                        }, 1500);
                    } else {
                        if (response.reason === "coDonNhap") {
                            $("#cannotDeleteModal").modal("show");
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

    // Xử lý khi nhấn nút Lưu trong form thêm mới
    $("#saveSupplier").click(function () {
        // Kiểm tra form hợp lệ
        if (!validateCreateForm()) {
            return;
        }

        var tenNhaCungCap = $("#tenNhaCungCap").val().trim();
        var lienHe = $("#lienHe").val().trim();
        var email = $("#email").val().trim();
        var soDienThoai = $("#soDienThoai").val().trim();

        // Gửi dữ liệu về server
        $.ajax({
            url: '/Home/ThemNhaCungCap',
            type: "POST",
            data: {
                TenNhaCungCap: tenNhaCungCap,
                LienHe: lienHe,
                Email: email,
                SoDienThoai: soDienThoai
            },
            success: function (response) {
                if (response.success) {
                    $("#createSupplierModal").modal("hide");
                    $("#successMessage").text("Thêm nhà cung cấp thành công!");
                    $("#successModal").modal("show");
                    setTimeout(function () {
                        location.reload();
                    }, 1500);
                } else {
                    alert(response.message || "Thêm nhà cung cấp thất bại");
                }
            },
            error: function () {
                alert("Lỗi khi gọi server.");
            }
        });
    });

    // Xử lý khi nhấn nút Sửa
    $(".btn-edit").click(function () {
        var maNhaCungCap = $(this).data("id");
        var tenNhaCungCap = $(this).data("name");
        var lienHe = $(this).data("contact");
        var email = $(this).data("email");
        var soDienThoai = $(this).data("phone");

        $("#editMaNhaCungCap").val(maNhaCungCap);
        $("#displayMaNhaCungCap").val(maNhaCungCap);
        $("#editTenNhaCungCap").val(tenNhaCungCap);
        $("#editLienHe").val(lienHe);
        $("#editEmail").val(email);
        $("#editSoDienThoai").val(soDienThoai);
    });

    // Xử lý khi nhấn nút Lưu trong form chỉnh sửa
    $("#updateSupplier").click(function () {
        // Kiểm tra form hợp lệ
        if (!validateEditForm()) {
            return;
        }

        var maNhaCungCap = $("#editMaNhaCungCap").val();
        var tenNhaCungCap = $("#editTenNhaCungCap").val().trim();
        var lienHe = $("#editLienHe").val().trim();
        var email = $("#editEmail").val().trim();
        var soDienThoai = $("#editSoDienThoai").val().trim();

        // Gửi dữ liệu về server
        $.ajax({
            url: '/Home/SuaNhaCungCap',
            type: "POST",
            data: {
                MaNhaCungCap: maNhaCungCap,
                TenNhaCungCap: tenNhaCungCap,
                LienHe: lienHe,
                Email: email,
                SoDienThoai: soDienThoai
            },
            success: function (response) {
                if (response.success) {
                    $("#editSupplierModal").modal("hide");
                    $("#successMessage").text("Cập nhật nhà cung cấp thành công!");
                    $("#successModal").modal("show");
                    setTimeout(function () {
                        location.reload();
                    }, 1500);
                } else {
                    alert(response.message || "Cập nhật nhà cung cấp thất bại");
                }
            },
            error: function () {
                alert("Lỗi khi gọi server.");
            }
        });
    });

    // Validate form thêm mới
    function validateCreateForm() {
        var isValid = true;
        var tenNhaCungCap = $("#tenNhaCungCap").val().trim();
        var lienHe = $("#lienHe").val().trim();
        var email = $("#email").val().trim();
        var soDienThoai = $("#soDienThoai").val().trim();
        var emailPattern = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$/;
        var phonePattern = /^[0-9]+$/;

        // Kiểm tra tên nhà cung cấp
        if (tenNhaCungCap === "") {
            $("#tenNhaCungCap").addClass("is-invalid");
            isValid = false;
        } else {
            $("#tenNhaCungCap").removeClass("is-invalid");
        }

        // Kiểm tra người liên hệ
        if (lienHe === "") {
            $("#lienHe").addClass("is-invalid");
            isValid = false;
        } else {
            $("#lienHe").removeClass("is-invalid");
        }

        // Kiểm tra email
        if (email === "" || !emailPattern.test(email)) {
            $("#emailError").show();
            isValid = false;
        } else {
            $("#emailError").hide();
        }

        // Kiểm tra số điện thoại
        if (soDienThoai === "" || !phonePattern.test(soDienThoai)) {
            $("#soDienThoai").addClass("is-invalid");
            isValid = false;
        } else {
            $("#soDienThoai").removeClass("is-invalid");
        }

        return isValid;
    }

    // Validate form sửa
    function validateEditForm() {
        var isValid = true;
        var tenNhaCungCap = $("#editTenNhaCungCap").val().trim();
        var lienHe = $("#editLienHe").val().trim();
        var email = $("#editEmail").val().trim();
        var soDienThoai = $("#editSoDienThoai").val().trim();
        var emailPattern = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$/;
        var phonePattern = /^[0-9]+$/;

        // Kiểm tra tên nhà cung cấp
        if (tenNhaCungCap === "") {
            $("#editTenNhaCungCap").addClass("is-invalid");
            isValid = false;
        } else {
            $("#editTenNhaCungCap").removeClass("is-invalid");
        }

        // Kiểm tra người liên hệ
        if (lienHe === "") {
            $("#editLienHe").addClass("is-invalid");
            isValid = false;
        } else {
            $("#editLienHe").removeClass("is-invalid");
        }

        // Kiểm tra email
        if (email === "" || !emailPattern.test(email)) {
            $("#editEmailError").show();
            isValid = false;
        } else {
            $("#editEmailError").hide();
        }

        // Kiểm tra số điện thoại
        if (soDienThoai === "" || !phonePattern.test(soDienThoai)) {
            $("#editSoDienThoai").addClass("is-invalid");
            isValid = false;
        } else {
            $("#editSoDienThoai").removeClass("is-invalid");
        }

        return isValid;
    }

    // Reset form khi đóng modal
    $("#createSupplierModal").on("hidden.bs.modal", function () {
        $("#createSupplierForm")[0].reset();
        $("#tenNhaCungCap, #lienHe, #soDienThoai").removeClass("is-invalid");
        $("#emailError").hide();
    });

    $("#editSupplierModal").on("hidden.bs.modal", function () {
        $("#editTenNhaCungCap, #editLienHe, #editSoDienThoai").removeClass("is-invalid");
        $("#editEmailError").hide();
    });
});