$(document).ready(function () {
    // Biến và hằng số
    const chatbotToggle = $("#chatbot-toggle");
    const chatbotSidebarToggle = $("#chatbot-sidebar-toggle");
    const chatbotWidget = $("#chatbot-widget");
    const chatbotClose = $("#chatbot-close");
    const chatbotSend = $("#chatbot-send");
    const chatbotText = $("#chatbot-text");
    const chatbotMessages = $("#chatbot-messages");
    let scriptData = null;

    // Load kịch bản chatbot từ file JSON
    $.ajax({
        url: '/Scripts/chatbot.json',
        dataType: 'json',
        async: false,
        success: function (data) {
            scriptData = data;
        },
        error: function (xhr, status, error) {
            console.error("Không thể tải kịch bản chatbot: " + error);
            // Fallback khi không tải được file JSON
            scriptData = {
                "greetings": ["xin chào", "hello", "hi", "chào", "hey"],
                "responses": [],
                "fallback": "Xin lỗi, không thể tải kịch bản. Vui lòng liên hệ quản trị viên."
            };
        }
    });

    // Hiện/ẩn chatbot bằng nút nổi
    chatbotToggle.click(function () {
        chatbotWidget.fadeToggle(300);
    });

    // Hiện/ẩn chatbot từ sidebar
    chatbotSidebarToggle.click(function (e) {
        e.preventDefault();
        chatbotWidget.fadeToggle(300);
    });

    // Đóng chatbot
    chatbotClose.click(function () {
        chatbotWidget.fadeOut(300);
    });

    // Xử lý gửi tin nhắn
    chatbotSend.click(sendMessage);
    chatbotText.keypress(function (e) {
        if (e.which === 13) {
            sendMessage();
        }
    });

    // Hàm gửi tin nhắn
    function sendMessage() {
        const message = chatbotText.val().trim();
        if (message === '') return;

        // Hiển thị tin nhắn của người dùng
        addUserMessage(message);
        chatbotText.val('');

        // Xử lý phản hồi
        processResponse(message);
    }

    // Thêm tin nhắn người dùng vào chatbot
    function addUserMessage(message) {
        chatbotMessages.append(`<div class="message user">${message}</div>`);
        scrollToBottom();
    }

    // Thêm tin nhắn bot vào chatbot
    function addBotMessage(message) {
        chatbotMessages.append(`<div class="message bot">${message}</div>`);
        scrollToBottom();
    }

    // Thêm link vào chatbot
    function addBotLinks(links) {
        let linksHTML = '';
        links.forEach(link => {
            linksHTML += `<a href="${link.url}" class="chatbot-link">${link.text}</a>`;
        });
        chatbotMessages.append(`<div class="message bot">${linksHTML}</div>`);
        scrollToBottom();
    }

    // Cuộn xuống dưới cùng của chatbot
    function scrollToBottom() {
        chatbotMessages.scrollTop(chatbotMessages[0].scrollHeight);
    }

    // Xử lý phản hồi dựa trên tin nhắn người dùng
    function processResponse(message) {
        if (!scriptData) {
            addBotMessage('Xin lỗi, kịch bản chưa được tải. Vui lòng thử lại sau.');
            return;
        }

        // Chuyển tin nhắn về chữ thường để dễ so sánh
        const lowerMessage = message.toLowerCase();

        // Kiểm tra xem có phải là lời chào
        for (const greeting of scriptData.greetings) {
            if (lowerMessage.includes(greeting)) {
                addBotMessage('Xin chào! Tôi có thể giúp gì cho bạn?');
                return;
            }
        }

        // Kiểm tra xem có khớp với kịch bản nào không
        let matched = false;
        for (const response of scriptData.responses) {
            for (const keyword of response.keywords) {
                if (lowerMessage.includes(keyword)) {
                    addBotMessage(response.reply);
                    addBotLinks(response.links);
                    matched = true;
                    break;
                }
            }
            if (matched) break;
        }

        // Nếu không khớp với kịch bản nào, trả về tin nhắn mặc định
        if (!matched) {
            addBotMessage(scriptData.fallback);
        }
    }

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

    // Xử lý đổi mật khẩu
    $('#saveNewPassword').click(function () {
        var isValid = true;

        // Kiểm tra mật khẩu cũ
        if ($('#oldPassword').val().trim() === '') {
            $('#oldPasswordError').show();
            isValid = false;
        } else {
            $('#oldPasswordError').hide();
        }

        // Kiểm tra mật khẩu mới
        if ($('#newPassword').val().trim() === '') {
            $('#newPasswordError').text('Vui lòng nhập mật khẩu mới.');
            $('#newPasswordError').show();
            isValid = false;
        } else if (!isStrongPassword($('#newPassword').val())) {
            $('#newPasswordError').text('Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.');
            $('#newPasswordError').show();
            isValid = false;
        } else {
            $('#newPasswordError').hide();
        }

        // Kiểm tra xác nhận mật khẩu
        if ($('#newPassword').val() !== $('#confirmNewPassword').val()) {
            $('#confirmNewPasswordError').show();
            isValid = false;
        } else {
            $('#confirmNewPasswordError').hide();
        }

        if (!isValid) return;

        // Gửi yêu cầu đổi mật khẩu đến server
        $.ajax({
            url: '/Home/DoiMatKhau',
            type: 'POST',
            data: {
                oldPassword: $('#oldPassword').val(),
                newPassword: $('#newPassword').val()
            },
            success: function (response) {
                if (response.success) {
                    $('#changePasswordModal').modal('hide');
                    $('#changePasswordSuccessModal').modal('show');
                } else {
                    $('#oldPasswordError').text(response.message || 'Mật khẩu hiện tại không đúng.');
                    $('#oldPasswordError').show();
                }
            },
            error: function () {
                alert('Có lỗi xảy ra khi đổi mật khẩu.');
            }
        });
    });

    // Đăng xuất sau khi đổi mật khẩu thành công
    $('#btnLogoutAfterChangePassword').click(function () {
        window.location.href = '/Home/Logout';
    });
});