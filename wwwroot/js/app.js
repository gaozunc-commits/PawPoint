// Initiate GET request (AJAX-supported)
$(document).on('click', '[data-get]', e => {
    e.preventDefault();
    const url = e.currentTarget.dataset.get;
    if (url) location.href = url;
});

// Initiate POST request (AJAX-supported)
$(document).on('click', '[data-post]', e => {
    e.preventDefault();
    const url = e.currentTarget.dataset.post;
    const f = $('<form>').appendTo(document.body)[0];
    f.method = 'post';
    f.action = url || location.href;
    const token = document.querySelector('meta[name="csrf-token"]');
    if (token) {
        $('<input>').attr({ type: 'hidden', name: '__RequestVerificationToken', value: token.content }).appendTo(f);
    }
    f.submit();
});

// Trim input
$(document).on('change', '[data-trim]', e => {
    e.target.value = e.target.value.trim();
});

// Auto uppercase
$(document).on('input', '[data-upper]', e => {
    const a = e.target.selectionStart;
    const b = e.target.selectionEnd;
    e.target.value = e.target.value.toUpperCase();
    e.target.setSelectionRange(a, b);
});

// RESET form
$(document).on('click', '[type=reset]', e => {
    e.preventDefault();
    location.href = location.pathname;
});

// Check all checkboxes
$(document).on('click', '[data-check]', e => {
    e.preventDefault();
    const name = e.target.dataset.check;
    $(`[name="${name}"]`).prop('checked', true);
});

// Uncheck all checkboxes
$(document).on('click', '[data-uncheck]', e => {
    e.preventDefault();
    const name = e.target.dataset.uncheck;
    $(`[name="${name}"]`).prop('checked', false);
});

// Row checkable (AJAX-supported)
$(document).on('click', '[data-checkable]', e => {
    if ($(e.target).is(':input,a,button')) return;
    $(e.currentTarget)
        .find(':checkbox')
        .prop('checked', (i, v) => !v);
});

// Photo preview on file select
$(document).on('change', '.upload input[type="file"]', e => {
    const f = e.target.files[0];
    const img = $(e.target).siblings('img')[0] || $(e.target).closest('.upload').find('img')[0];

    if (img) {
        img.dataset.src ??= img.src;
        if (f && f.type.startsWith('image/')) {
            img.onload = () => URL.revokeObjectURL(img.src);
            img.src = URL.createObjectURL(f);
        } else {
            img.src = img.dataset.src;
            e.target.value = '';
        }
    }

    if (typeof $(e.target).valid === 'function') {
        $(e.target).valid();
    }
});

// ----------------------------------------------------------------------------
// HTML5 Webcam Capture Integration (Practical 06 AF)
// ----------------------------------------------------------------------------

let webcamStream = null;
let currentWebcamTargetImg = null;
let currentWebcamHiddenInput = null;

$(document).on('click', '.btn-webcam', function (e) {
    e.preventDefault();
    currentWebcamTargetImg = $(this.dataset.target)[0];
    currentWebcamHiddenInput = $(this.dataset.hidden)[0];

    const modal = document.getElementById('webcamModal');
    const video = document.getElementById('webcamVideo');

    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        navigator.mediaDevices.getUserMedia({ video: { width: 400, height: 300 } })
            .then(function (stream) {
                webcamStream = stream;
                video.srcObject = stream;
                video.play();
                modal.style.display = 'flex';
            })
            .catch(function (err) {
                alert('Webcam access error: ' + err.message + '\nPlease check browser permissions or use file upload.');
            });
    } else {
        alert('Webcam is not supported in this browser environment.');
    }
});

function stopWebcam() {
    if (webcamStream) {
        webcamStream.getTracks().forEach(track => track.stop());
        webcamStream = null;
    }
    const modal = document.getElementById('webcamModal');
    if (modal) modal.style.display = 'none';
}

$(document).on('click', '#closeWebcamBtn', function () {
    stopWebcam();
});

$(document).on('click', '#snapBtn', function () {
    const video = document.getElementById('webcamVideo');
    const canvas = document.getElementById('webcamCanvas');

    if (video && canvas) {
        canvas.width = video.videoWidth || 400;
        canvas.height = video.videoHeight || 300;
        const ctx = canvas.getContext('2d');
        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

        const dataUrl = canvas.toDataURL('image/jpeg', 0.9);

        if (currentWebcamTargetImg) {
            currentWebcamTargetImg.src = dataUrl;
        }
        if (currentWebcamHiddenInput) {
            currentWebcamHiddenInput.value = dataUrl;
        }
    }

    stopWebcam();
});
