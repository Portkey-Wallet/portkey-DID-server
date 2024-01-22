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
                })
            }, 500);
        }

        $modal.show();

        $confirmBtn.off('click').on('click', function () {
            $modal.hide();
            clearInputs();
            if ($.isFunction(onConfirm)) {
                onConfirm();
            }
        });

        $cancelBtn.off('click').on('click', function () {
            $modal.hide();
            clearInputs();
            if ($.isFunction(onCancel)) {
                onCancel();
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