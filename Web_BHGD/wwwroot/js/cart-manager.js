/**
 * Cart Manager - Quản lý giỏ hàng toàn cục
 * Sử dụng: Tích hợp vào _Layout.cshtml để áp dụng cho tất cả trang
 */

window.CartManager = {
    // Configuration
    config: {
        addToCartUrl: '',
        getCartCountUrl: '',
        toastDuration: 3000
    },

    // Khởi tạo CartManager
    init: function (addToCartUrl, getCartCountUrl) {
        this.config.addToCartUrl = addToCartUrl;
        this.config.getCartCountUrl = getCartCountUrl;

        // Cập nhật số lượng giỏ hàng khi khởi tạo
        this.updateCartCount();

        // Bind events
        this.bindEvents();

        console.log('CartManager initialized');
    },

    // Bind các events
    bindEvents: function () {
        var self = this;

        // Xử lý click nút "Thêm vào giỏ"
        $(document).on('click', '.add-to-cart-btn', function (e) {
            e.preventDefault();
            self.handleAddToCartClick($(this));
        });

        // Xử lý form tìm kiếm
        $(document).on('submit', '.search-box', function (e) {
            self.handleSearchSubmit($(this), e);
        });
    },

    // Xử lý click nút thêm vào giỏ
    handleAddToCartClick: function (btn) {
        var productId = btn.data('product-id');
        var productName = btn.data('product-name');
        var quantity = btn.data('quantity') || 1;

        if (!productId || productId <= 0) {
            this.showToast('error', 'ID sản phẩm không hợp lệ.');
            return;
        }

        var originalHtml = btn.html();
        this.setButtonLoading(btn, true);

        this.addToCart(productId, productName, quantity)
            .always(function () {
                CartManager.setButtonLoading(btn, false, originalHtml);
            });
    },

    // Xử lý submit form tìm kiếm
    handleSearchSubmit: function (form, event) {
        var searchTerm = form.find('input[name="searchTerm"]').val().trim();
        if (!searchTerm) {
            event.preventDefault();
            this.showToast('error', 'Vui lòng nhập từ khóa tìm kiếm.');
        }
    },

    // Set trạng thái loading cho button
    setButtonLoading: function (btn, isLoading, originalHtml) {
        if (isLoading) {
            btn.prop('disabled', true)
                .html('<i class="spinner-border spinner-border-sm me-1"></i> Đang thêm...');
        } else {
            btn.prop('disabled', false)
                .html(originalHtml || '<i class="bi bi-cart-plus me-1"></i>Thêm vào giỏ');
        }
    },

    // Cập nhật số lượng giỏ hàng
    updateCartCount: function () {
        var self = this;

        if (!this.config.getCartCountUrl) {
            console.warn('getCartCountUrl not configured');
            return;
        }

        $.get(this.config.getCartCountUrl)
            .done(function (data) {
                $('.cart-count').text(data.count || 0);
            })
            .fail(function (xhr, status, error) {
                console.error('Failed to update cart count:', { status, error });
                $('.cart-count').text('0');
            });
    },

    // Thêm sản phẩm vào giỏ hàng
    addToCart: function (productId, productName, quantity) {
        var self = this;
        quantity = quantity || 1;

        if (!this.config.addToCartUrl) {
            console.error('addToCartUrl not configured');
            this.showToast('error', 'Cấu hình hệ thống không đúng.');
            return $.Deferred().reject();
        }

        return $.ajax({
            url: this.config.addToCartUrl,
            type: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            },
            data: {
                productId: productId,
                quantity: quantity,
                __RequestVerificationToken: this.getAntiForgeryToken()
            }
        })
            .done(function (response) {
                self.handleAddToCartSuccess(response, productName);
            })
            .fail(function (xhr, status, error) {
                self.handleAddToCartError(xhr, status, error);
            });
    },

    // Xử lý khi thêm vào giỏ thành công
    handleAddToCartSuccess: function (response, productName) {
        if (typeof response === 'object' && response.success === false) {
            this.showToast('error', response.message || 'Có lỗi xảy ra khi thêm sản phẩm.');
            return;
        }

        var message = (typeof response === 'object' && response.message)
            ? response.message
            : `Đã thêm ${productName} vào giỏ hàng.`;

        this.showToast('success', message);
        this.updateCartCount();
    },

    // Xử lý khi thêm vào giỏ lỗi
    handleAddToCartError: function (xhr, status, error) {
        console.error('Add to cart error:', { status, error, responseText: xhr.responseText });

        var errorMessage = this.getErrorMessage(xhr.status);
        this.showToast('error', errorMessage);
    },

    // Lấy thông báo lỗi dựa trên status code
    getErrorMessage: function (statusCode) {
        var messages = {
            400: 'Dữ liệu không hợp lệ.',
            404: 'Không tìm thấy sản phẩm.',
            500: 'Lỗi server. Vui lòng thử lại sau.',
            0: 'Mất kết nối mạng.'
        };

        return messages[statusCode] || 'Lỗi khi thêm sản phẩm vào giỏ hàng.';
    },

    // Lấy CSRF token
    getAntiForgeryToken: function () {
        return $('input[name="__RequestVerificationToken"]').val() || '';
    },

    // Hiển thị thông báo toast
    showToast: function (type, message) {
        var toastClass = type === 'success' ? 'alert-success' : 'alert-danger';
        var icon = type === 'success' ? 'bi-check-circle' : 'bi-exclamation-triangle';
        var toastId = 'toast-' + Date.now();

        var toast = `
            <div id="${toastId}" class="alert ${toastClass} alert-dismissible fade show position-fixed"
                 style="top: 20px; right: 20px; z-index: 9999; min-width: 300px;" role="alert">
                <i class="bi ${icon} me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>`;

        $('body').append(toast);

        // Tự động ẩn
        setTimeout(function () {
            $('#' + toastId).alert('close');
        }, this.config.toastDuration);
    },

    // Utility methods
    utils: {
        // Format số tiền
        formatCurrency: function (amount) {
            return new Intl.NumberFormat('vi-VN').format(amount) + '₫';
        },

        // Validate email
        isValidEmail: function (email) {
            var re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return re.test(email);
        },

        // Validate phone
        isValidPhone: function (phone) {
            var re = /^[0-9]{10,11}$/;
            return re.test(phone.replace(/\s/g, ''));
        }
    }
};