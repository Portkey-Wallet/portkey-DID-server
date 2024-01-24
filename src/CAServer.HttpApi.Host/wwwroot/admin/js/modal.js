let Modal = (function () {

    function showModal(idSelection, onConfirm, onCancel) {
        let $modal = $(idSelection);
        let $confirmBtn = $modal.find(".confirm-btn");
        let $cancelBtn = $modal.find(".cancel-btn");
        let $closeBtn = $modal.find(".close");
        var $inputs = $modal.find(".modal-input");

        function clearInputs() {
            setTimeout(() => {
                $inputs.each(function () {
                    $(this).val('');
                    $(this).removeClass("error")
                })
            }, 500);
        }

        $modal.show();

        $confirmBtn.off('click').on('click', function (event) {
            if ($.isFunction(onConfirm) && onConfirm(event) !== false) {
                $modal.hide();
                clearInputs();
            }
        });

        $cancelBtn.off('click').on('click', function (event) {
            if (!$.isFunction(onCancel) || onCancel(event) !== false) {
                $modal.hide();
                clearInputs();
            }
        });

        $closeBtn.off('click').on('click', function () {
            $modal.hide();
            clearInputs();
        });

        $(window).off('click').on('click', function (event) {
            if ($(event.target).is($modal)) {
                $modal.hide();
                clearInputs();
            }
        });
    }

    return {
        showModal: showModal
    }
})();