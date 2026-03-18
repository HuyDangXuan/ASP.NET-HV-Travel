(function () {
    'use strict';

    var modalState = {
        isOpen: false,
        isUploading: false,
        options: {},
        onSuccess: null,
        files: [],
        activeTab: 'upload'
    };

    var modalElements = null;
    var objectUrls = [];

    function getConfig() {
        if (window.CloudinaryConfig && window.CloudinaryConfig.cloudName && window.CloudinaryConfig.uploadPreset) {
            return window.CloudinaryConfig;
        }

        var host = document.querySelector('[data-cloudinary-cloud-name]');
        if (host) {
            return {
                cloudName: host.getAttribute('data-cloudinary-cloud-name') || '',
                uploadPreset: host.getAttribute('data-cloudinary-upload-preset') || ''
            };
        }

        return { cloudName: '', uploadPreset: '' };
    }

    function ensureConfig() {
        var config = getConfig();
        if (!config.cloudName || !config.uploadPreset) {
            alert('Thiếu cấu hình Cloudinary. Hãy kiểm tra Cloudinary:CloudName và Cloudinary:UploadPreset.');
            return null;
        }

        return config;
    }

    function normalizeOptions(options) {
        var safeOptions = options || {};

        return {
            resourceType: safeOptions.resourceType || 'image',
            multiple: !!safeOptions.multiple,
            folder: safeOptions.folder || '',
            allowedFormats: Array.isArray(safeOptions.allowedFormats) && safeOptions.allowedFormats.length
                ? safeOptions.allowedFormats.slice()
                : ['png', 'jpg', 'jpeg', 'webp', 'gif'],
            maxFileSize: safeOptions.maxFileSize || 5 * 1024 * 1024
        };
    }

    function releaseObjectUrls() {
        objectUrls.forEach(function (url) {
            URL.revokeObjectURL(url);
        });
        objectUrls = [];
    }

    function formatBytes(bytes) {
        if (!bytes) return '0 B';
        var units = ['B', 'KB', 'MB', 'GB'];
        var index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
        var value = bytes / Math.pow(1024, index);
        return value.toFixed(index === 0 ? 0 : 1) + ' ' + units[index];
    }

    function escapeHtml(text) {
        return String(text || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function injectModal() {
        if (modalElements) return modalElements;

        var wrapper = document.createElement('div');
        wrapper.id = 'admin-cloudinary-modal';
        wrapper.className = 'fixed inset-0 z-[250] hidden';
        wrapper.innerHTML = [
            '<div class="absolute inset-0 bg-slate-950/45 backdrop-blur-sm opacity-0 transition-opacity duration-300" data-cloudinary-overlay></div>',
            '<div class="absolute inset-0 overflow-y-auto p-4 sm:p-8 pointer-events-none">',
            '  <div class="pointer-events-auto mx-auto my-4 w-full max-w-6xl overflow-hidden rounded-[2rem] border border-slate-200/80 bg-white shadow-[0_24px_90px_rgba(15,23,42,0.28)] opacity-0 translate-y-8 transition-all duration-300 sm:my-8 max-h-[calc(100vh-2rem)] sm:max-h-[calc(100vh-4rem)]" data-cloudinary-panel>',
            '    <div class="relative overflow-hidden bg-[radial-gradient(circle_at_top_left,_rgba(14,165,233,0.16),_transparent_34%),linear-gradient(135deg,#0f172a_0%,#102b44_42%,#0f7cc0_100%)] px-6 py-6 text-white sm:px-8">',
            '      <div class="absolute -right-14 -top-10 size-40 rounded-full bg-white/10 blur-2xl"></div>',
            '      <div class="absolute -left-10 bottom-0 size-28 rounded-full bg-cyan-300/20 blur-2xl"></div>',
            '      <div class="relative flex items-start justify-between gap-6">',
            '        <div>',
            '          <p class="mb-2 inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/10 px-3 py-1 text-[11px] font-bold uppercase tracking-[0.28em] text-cyan-100">Cloud Upload Studio</p>',
            '          <h3 class="text-2xl font-black tracking-tight sm:text-3xl">Tải hình ảnh lên</h3>',
            '          <p class="mt-2 max-w-2xl text-sm text-slate-200/90 sm:text-[15px]">Kéo thả file, chọn ảnh từ máy, hoặc dán liên kết ảnh có sẵn. Mọi thứ sẽ được đưa lên Cloudinary và trả URL ngay sau khi hoàn tất.</p>',
            '        </div>',
            '        <button type="button" data-cloudinary-close class="inline-flex size-11 items-center justify-center rounded-2xl border border-white/15 bg-white/10 text-white transition hover:bg-white/20" aria-label="Đóng modal">',
            '          <span class="material-symbols-outlined text-[24px]">close</span>',
            '        </button>',
            '      </div>',
            '      <div class="relative mt-6 flex flex-wrap gap-3">',
            '        <button type="button" data-cloudinary-tab="upload" class="cloudinary-tab inline-flex items-center gap-2 rounded-2xl px-4 py-3 text-sm font-bold transition"></button>',
            '        <button type="button" data-cloudinary-tab="url" class="cloudinary-tab inline-flex items-center gap-2 rounded-2xl px-4 py-3 text-sm font-bold transition"></button>',
            '      </div>',
            '    </div>',
            '    <div class="grid min-h-0 gap-0 xl:grid-cols-[minmax(0,1fr)_320px]">',
            '      <div class="min-w-0 overflow-y-auto border-b border-slate-200/80 p-6 sm:p-8 xl:border-b-0 xl:border-r">',
            '        <section data-cloudinary-view="upload" class="space-y-5">',
            '          <div data-cloudinary-dropzone class="group relative overflow-hidden rounded-[1.75rem] border-2 border-dashed border-sky-200 bg-[linear-gradient(180deg,#f8fcff_0%,#edf7ff_100%)] p-8 text-center transition hover:border-sky-400 hover:bg-[linear-gradient(180deg,#ffffff_0%,#eff8ff_100%)] sm:p-12">',
            '            <div class="mx-auto flex size-20 items-center justify-center rounded-[1.75rem] bg-white shadow-sm ring-1 ring-sky-100">',
            '              <span class="material-symbols-outlined text-[38px] text-sky-600">cloud_upload</span>',
            '            </div>',
            '            <h4 class="mt-5 text-xl font-black tracking-tight text-slate-900">Thả file vào đây</h4>',
            '            <p class="mx-auto mt-2 max-w-xl text-sm leading-6 text-slate-500">Hỗ trợ PNG, JPG, JPEG, WEBP, GIF. Bạn có thể kéo nhiều ảnh cùng lúc cho form tour.</p>',
            '            <div class="mt-6 flex flex-wrap items-center justify-center gap-3">',
            '              <button type="button" data-cloudinary-browse class="inline-flex items-center gap-2 rounded-2xl bg-sky-600 px-5 py-3 text-sm font-extrabold text-white shadow-lg shadow-sky-600/25 transition hover:-translate-y-0.5 hover:bg-sky-500">',
            '                <span class="material-symbols-outlined text-[20px]">folder_open</span> Chọn ảnh từ máy',
            '              </button>',
            '              <button type="button" data-cloudinary-camera class="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-5 py-3 text-sm font-bold text-slate-700 transition hover:border-sky-200 hover:text-sky-700">',
            '                <span class="material-symbols-outlined text-[20px]">photo_camera</span> Mở camera',
            '              </button>',
            '            </div>',
            '            <input type="file" data-cloudinary-file-input class="hidden" accept="image/*" />',
            '            <input type="file" data-cloudinary-camera-input class="hidden" accept="image/*" capture="environment" />',
            '          </div>',
            '          <div>',
            '            <div class="mb-3 flex items-center justify-between">',
            '              <div>',
            '                <h4 class="text-base font-black text-slate-900">Hàng đợi tải lên</h4>',
            '                <p class="text-sm text-slate-500">Ảnh sẽ được đưa lên Cloudinary ngay trong modal này.</p>',
            '              </div>',
            '              <button type="button" data-cloudinary-clear class="text-sm font-bold text-slate-400 transition hover:text-rose-500">Xóa hết</button>',
            '            </div>',
            '            <div data-cloudinary-file-list class="space-y-3 overflow-hidden"></div>',
            '          </div>',
            '        </section>',
            '        <section data-cloudinary-view="url" class="hidden space-y-5">',
            '          <div class="rounded-[1.75rem] border border-slate-200 bg-slate-50/80 p-6 sm:p-8">',
            '            <div class="mb-4">',
            '              <h4 class="text-xl font-black tracking-tight text-slate-900">Thêm bằng liên kết</h4>',
            '              <p class="mt-2 text-sm leading-6 text-slate-500">Dán link ảnh công khai. Hệ thống sẽ chèn thẳng vào danh sách hình của tour mà không cần tải lại qua popup gốc của Cloudinary.</p>',
            '            </div>',
            '            <label class="mb-2 block text-xs font-bold uppercase tracking-[0.24em] text-slate-400">URL hình ảnh</label>',
            '            <div class="flex flex-col gap-3 sm:flex-row">',
            '              <input type="url" data-cloudinary-url-input placeholder="https://example.com/image.jpg" class="h-14 flex-1 rounded-2xl border border-slate-200 bg-white px-4 text-[15px] font-medium text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-sky-400 focus:ring-4 focus:ring-sky-100" />',
            '              <button type="button" data-cloudinary-add-url class="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-5 py-3 text-sm font-extrabold text-white transition hover:bg-slate-800">',
            '                <span class="material-symbols-outlined text-[20px]">add_link</span> Chèn vào tour',
            '              </button>',
            '            </div>',
            '            <p class="mt-3 text-xs text-slate-400">Phù hợp khi bạn đã có URL ảnh từ Cloudinary, Unsplash hoặc CDN khác.</p>',
            '          </div>',
            '        </section>',
            '      </div>',
            '      <aside class="overflow-y-auto border-t border-slate-200/80 bg-slate-50/80 p-6 sm:p-8 xl:border-t-0">',
            '        <div class="rounded-[1.75rem] border border-slate-200 bg-white p-5 shadow-sm">',
            '          <div class="flex items-center gap-3">',
            '            <div class="flex size-12 items-center justify-center rounded-2xl bg-sky-50 text-sky-600">',
            '              <span class="material-symbols-outlined text-[24px]">bolt</span>',
            '            </div>',
            '            <div>',
            '              <p class="text-sm font-black text-slate-900">Upload nhanh hơn</p>',
            '              <p class="text-xs text-slate-500">Không còn popup gốc của Cloudinary.</p>',
            '            </div>',
            '          </div>',
            '          <div class="mt-5 grid gap-3">',
            '            <div class="rounded-2xl bg-slate-50 px-4 py-3">',
            '              <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-slate-400">Thư mục</p>',
            '              <p data-cloudinary-folder class="mt-1 text-sm font-bold text-slate-800">hv-travel/tours</p>',
            '            </div>',
            '            <div class="rounded-2xl bg-slate-50 px-4 py-3">',
            '              <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-slate-400">Định dạng</p>',
            '              <p data-cloudinary-formats class="mt-1 text-sm font-bold text-slate-800">PNG, JPG, JPEG, WEBP, GIF</p>',
            '            </div>',
            '            <div class="rounded-2xl bg-slate-50 px-4 py-3">',
            '              <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-slate-400">Trạng thái</p>',
            '              <p data-cloudinary-status class="mt-1 text-sm font-bold text-slate-800">Sẵn sàng để tải lên</p>',
            '            </div>',
            '          </div>',
            '        </div>',
            '        <div class="mt-5 rounded-[1.75rem] border border-slate-200 bg-white p-5 shadow-sm">',
            '          <h4 class="text-sm font-black uppercase tracking-[0.22em] text-slate-400">Hướng dẫn</h4>',
            '          <ul class="mt-4 space-y-3 text-sm leading-6 text-slate-500">',
            '            <li class="flex gap-3"><span class="mt-1 size-2 rounded-full bg-sky-500"></span><span>Nếu kéo nhiều ảnh, hệ thống sẽ upload lần lượt và trả URL ngay khi xong.</span></li>',
            '            <li class="flex gap-3"><span class="mt-1 size-2 rounded-full bg-sky-500"></span><span>Có thể dán URL có sẵn mà không cần upload lại.</span></li>',
            '            <li class="flex gap-3"><span class="mt-1 size-2 rounded-full bg-sky-500"></span><span>Modal này tự động đóng sau khi upload thành công.</span></li>',
            '          </ul>',
            '        </div>',
            '      </aside>',
            '    </div>',
            '  </div>',
            '</div>'
        ].join('');

        document.body.appendChild(wrapper);

        modalElements = {
            root: wrapper,
            overlay: wrapper.querySelector('[data-cloudinary-overlay]'),
            panel: wrapper.querySelector('[data-cloudinary-panel]'),
            close: wrapper.querySelector('[data-cloudinary-close]'),
            tabs: Array.prototype.slice.call(wrapper.querySelectorAll('[data-cloudinary-tab]')),
            views: Array.prototype.slice.call(wrapper.querySelectorAll('[data-cloudinary-view]')),
            dropzone: wrapper.querySelector('[data-cloudinary-dropzone]'),
            browseButton: wrapper.querySelector('[data-cloudinary-browse]'),
            cameraButton: wrapper.querySelector('[data-cloudinary-camera]'),
            fileInput: wrapper.querySelector('[data-cloudinary-file-input]'),
            cameraInput: wrapper.querySelector('[data-cloudinary-camera-input]'),
            fileList: wrapper.querySelector('[data-cloudinary-file-list]'),
            clearButton: wrapper.querySelector('[data-cloudinary-clear]'),
            folderLabel: wrapper.querySelector('[data-cloudinary-folder]'),
            formatsLabel: wrapper.querySelector('[data-cloudinary-formats]'),
            statusLabel: wrapper.querySelector('[data-cloudinary-status]'),
            urlInput: wrapper.querySelector('[data-cloudinary-url-input]'),
            addUrlButton: wrapper.querySelector('[data-cloudinary-add-url]')
        };

        bindEvents();
        return modalElements;
    }

    function bindEvents() {
        modalElements.overlay.addEventListener('click', closeModal);
        modalElements.close.addEventListener('click', closeModal);

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && modalState.isOpen) {
                closeModal();
            }
        });

        modalElements.tabs.forEach(function (tabButton) {
            tabButton.addEventListener('click', function () {
                setActiveTab(tabButton.getAttribute('data-cloudinary-tab'));
            });
        });

        modalElements.browseButton.addEventListener('click', function () {
            modalElements.fileInput.click();
        });

        modalElements.cameraButton.addEventListener('click', function () {
            modalElements.cameraInput.click();
        });

        modalElements.fileInput.addEventListener('change', function () {
            handleFiles(modalElements.fileInput.files);
            modalElements.fileInput.value = '';
        });

        modalElements.cameraInput.addEventListener('change', function () {
            handleFiles(modalElements.cameraInput.files);
            modalElements.cameraInput.value = '';
        });

        modalElements.dropzone.addEventListener('dragover', function (event) {
            event.preventDefault();
            modalElements.dropzone.classList.add('border-sky-500', 'bg-sky-50');
        });

        modalElements.dropzone.addEventListener('dragleave', function () {
            modalElements.dropzone.classList.remove('border-sky-500', 'bg-sky-50');
        });

        modalElements.dropzone.addEventListener('drop', function (event) {
            event.preventDefault();
            modalElements.dropzone.classList.remove('border-sky-500', 'bg-sky-50');
            handleFiles(event.dataTransfer && event.dataTransfer.files);
        });

        modalElements.clearButton.addEventListener('click', function () {
            if (modalState.isUploading) return;
            modalState.files = [];
            renderFileList();
            setStatus('Sẵn sàng để tải lên');
        });

        modalElements.addUrlButton.addEventListener('click', submitUrl);
        modalElements.urlInput.addEventListener('keydown', function (event) {
            if (event.key === 'Enter') {
                event.preventDefault();
                submitUrl();
            }
        });
    }

    function setActiveTab(tab) {
        modalState.activeTab = tab === 'url' ? 'url' : 'upload';

        modalElements.tabs.forEach(function (button) {
            var isActive = button.getAttribute('data-cloudinary-tab') === modalState.activeTab;
            button.className = isActive
                ? 'cloudinary-tab inline-flex items-center gap-2 rounded-2xl bg-white text-slate-900 shadow-lg shadow-slate-950/10 px-4 py-3 text-sm font-extrabold transition'
                : 'cloudinary-tab inline-flex items-center gap-2 rounded-2xl border border-white/15 bg-white/10 text-slate-100/85 px-4 py-3 text-sm font-bold transition hover:bg-white/15';
            button.innerHTML = button.getAttribute('data-cloudinary-tab') === 'upload'
                ? '<span class="material-symbols-outlined text-[20px]">upload</span>Tải từ máy'
                : '<span class="material-symbols-outlined text-[20px]">link</span>Dán liên kết';
        });

        modalElements.views.forEach(function (view) {
            var isVisible = view.getAttribute('data-cloudinary-view') === modalState.activeTab;
            view.classList.toggle('hidden', !isVisible);
        });
    }

    function setStatus(text) {
        modalElements.statusLabel.textContent = text;
    }

    function renderFileList() {
        if (!modalState.files.length) {
            modalElements.fileList.innerHTML = [
                '<div class="rounded-[1.5rem] border border-dashed border-slate-200 bg-white px-5 py-6 text-center">',
                '  <p class="text-sm font-bold text-slate-700">Chưa có file nào trong hàng đợi</p>',
                '  <p class="mt-1 text-sm text-slate-400">Thêm ảnh từ máy, camera, hoặc kéo thả vào vùng upload.</p>',
                '</div>'
            ].join('');
            return;
        }

        modalElements.fileList.innerHTML = modalState.files.map(function (item, index) {
            var detailMarkup = '';

            if (item.message) {
                detailMarkup = item.status === 'done'
                    ? '<div class="mt-3 rounded-xl border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-500"><span class="block truncate" title="' + escapeHtml(item.message) + '">' + escapeHtml(item.message) + '</span></div>'
                    : '<p class="mt-2 truncate text-xs text-slate-400" title="' + escapeHtml(item.message) + '">' + escapeHtml(item.message) + '</p>';
            }

            return [
                '<div class="flex min-w-0 items-center gap-4 rounded-[1.5rem] border border-slate-200 bg-white p-3 shadow-sm">',
                '  <div class="size-16 shrink-0 overflow-hidden rounded-2xl bg-slate-100 ring-1 ring-slate-200">',
                item.previewUrl
                    ? '    <img src="' + escapeHtml(item.previewUrl) + '" alt="" class="h-full w-full object-cover" />'
                    : '    <div class="flex h-full w-full items-center justify-center text-slate-400"><span class="material-symbols-outlined">image</span></div>',
                '  </div>',
                '  <div class="min-w-0 flex-1 overflow-hidden">',
                '    <p class="truncate text-sm font-black text-slate-900">' + escapeHtml(item.file.name) + '</p>',
                '    <p class="mt-1 text-xs font-medium text-slate-400">' + escapeHtml(formatBytes(item.file.size)) + '</p>',
                '    <div class="mt-2 flex min-w-0 flex-wrap items-center gap-2 overflow-hidden">',
                '      <span class="inline-flex shrink-0 items-center rounded-full px-2.5 py-1 text-[11px] font-bold ' + statusBadgeClass(item.status) + '">' + escapeHtml(statusLabel(item.status)) + '</span>',
                '    </div>',
                detailMarkup,
                '  </div>',
                '  <button type="button" data-cloudinary-remove="' + index + '" class="inline-flex size-10 items-center justify-center rounded-2xl text-slate-400 transition hover:bg-rose-50 hover:text-rose-500"' + (modalState.isUploading ? ' disabled' : '') + '>',
                '    <span class="material-symbols-outlined text-[20px]">delete</span>',
                '  </button>',
                '</div>'
            ].join('');
        }).join('');

        Array.prototype.slice.call(modalElements.fileList.querySelectorAll('[data-cloudinary-remove]')).forEach(function (button) {
            button.addEventListener('click', function () {
                if (modalState.isUploading) return;
                var index = parseInt(button.getAttribute('data-cloudinary-remove'), 10);
                modalState.files.splice(index, 1);
                renderFileList();
            });
        });
    }

    function statusBadgeClass(status) {
        if (status === 'done') return 'bg-emerald-50 text-emerald-700';
        if (status === 'error') return 'bg-rose-50 text-rose-700';
        if (status === 'uploading') return 'bg-sky-50 text-sky-700';
        return 'bg-slate-100 text-slate-600';
    }

    function statusLabel(status) {
        if (status === 'done') return 'Đã tải xong';
        if (status === 'error') return 'Thất bại';
        if (status === 'uploading') return 'Đang tải';
        return 'Chờ tải';
    }

    function handleFiles(fileList) {
        if (!fileList || !fileList.length) return;

        var options = modalState.options;
        var incoming = Array.prototype.slice.call(fileList).filter(function (file) {
            var extension = '';
            var segments = (file.name || '').split('.');
            if (segments.length > 1) extension = segments.pop().toLowerCase();

            if (file.size > options.maxFileSize) {
                alert('File "' + file.name + '" vượt quá giới hạn ' + Math.round(options.maxFileSize / (1024 * 1024)) + 'MB.');
                return false;
            }

            if (options.allowedFormats.indexOf(extension) === -1) {
                alert('File "' + file.name + '" không đúng định dạng được phép.');
                return false;
            }

            return true;
        });

        if (!incoming.length) return;

        if (!options.multiple) {
            modalState.files = [];
        }

        incoming.forEach(function (file) {
            var previewUrl = URL.createObjectURL(file);
            objectUrls.push(previewUrl);

            modalState.files.push({
                file: file,
                previewUrl: previewUrl,
                status: 'queued',
                message: 'Sẵn sàng tải lên'
            });
        });

        if (!options.multiple && modalState.files.length > 1) {
            modalState.files = [modalState.files[modalState.files.length - 1]];
        }

        renderFileList();
        setStatus('Đã thêm ' + modalState.files.length + ' file vào hàng đợi');
        uploadQueuedFiles();
    }

    function uploadQueuedFiles() {
        if (modalState.isUploading) return;
        if (!modalState.files.some(function (item) { return item.status === 'queued'; })) return;

        modalState.isUploading = true;
        setStatus('Đang tải lên Cloudinary...');
        renderFileList();
        processNextUpload();
    }

    function processNextUpload() {
        var nextItem = modalState.files.find(function (item) { return item.status === 'queued'; });

        if (!nextItem) {
            modalState.isUploading = false;
            renderFileList();
            setStatus('Tải lên hoàn tất');
            return;
        }

        nextItem.status = 'uploading';
        nextItem.message = 'Đang gửi file lên Cloudinary';
        renderFileList();

        uploadFile(nextItem.file, modalState.options).then(function (info) {
            nextItem.status = 'done';
            nextItem.message = info.secure_url || info.url || 'Tải lên thành công';
            renderFileList();

            if (typeof modalState.onSuccess === 'function') {
                modalState.onSuccess(info.secure_url || info.url, info);
            }

            processNextUpload();
        }).catch(function (error) {
            nextItem.status = 'error';
            nextItem.message = error && error.message ? error.message : 'Không thể tải lên file';
            modalState.isUploading = false;
            renderFileList();
            setStatus('Có file tải lên thất bại');
        });
    }

    function uploadFile(file, options) {
        var config = ensureConfig();
        if (!config) {
            return Promise.reject(new Error('Thiếu cấu hình Cloudinary.'));
        }

        var formData = new FormData();
        formData.append('file', file);
        formData.append('upload_preset', config.uploadPreset);
        if (options.folder) {
            formData.append('folder', options.folder);
        }

        return fetch('https://api.cloudinary.com/v1_1/' + encodeURIComponent(config.cloudName) + '/' + encodeURIComponent(options.resourceType) + '/upload', {
            method: 'POST',
            body: formData
        }).then(function (response) {
            if (!response.ok) {
                return response.json().catch(function () { return {}; }).then(function (payload) {
                    var message = payload && payload.error && payload.error.message
                        ? payload.error.message
                        : 'Cloudinary trả về lỗi khi tải file.';
                    throw new Error(message);
                });
            }

            return response.json();
        });
    }

    function submitUrl() {
        var url = (modalElements.urlInput.value || '').trim();
        if (!url) {
            modalElements.urlInput.focus();
            return;
        }

        try {
            new URL(url);
        } catch (error) {
            alert('Liên kết ảnh không hợp lệ.');
            return;
        }

        if (typeof modalState.onSuccess === 'function') {
            modalState.onSuccess(url, { secure_url: url, source: 'manual_url' });
        }

        modalElements.urlInput.value = '';
        setStatus('Đã chèn liên kết ảnh vào tour');
    }

    function openModal(options, onSuccess) {
        injectModal();
        releaseObjectUrls();

        modalState.options = normalizeOptions(options);
        modalState.onSuccess = onSuccess;
        modalState.files = [];
        modalState.isUploading = false;
        modalState.isOpen = true;
        modalState.activeTab = 'upload';

        modalElements.fileInput.multiple = modalState.options.multiple;
        modalElements.cameraInput.multiple = modalState.options.multiple;
        modalElements.folderLabel.textContent = modalState.options.folder || 'Mặc định';
        modalElements.formatsLabel.textContent = modalState.options.allowedFormats.join(', ').toUpperCase();
        modalElements.urlInput.value = '';

        setActiveTab('upload');
        renderFileList();
        setStatus('Sẵn sàng để tải lên');

        modalElements.root.classList.remove('hidden');
        document.body.classList.add('overflow-hidden');

        requestAnimationFrame(function () {
            modalElements.overlay.classList.remove('opacity-0');
            modalElements.panel.classList.remove('opacity-0', 'translate-y-8');
        });
    }

    function closeModal() {
        if (!modalElements || !modalState.isOpen) return;
        if (modalState.isUploading) return;

        modalState.isOpen = false;
        modalElements.overlay.classList.add('opacity-0');
        modalElements.panel.classList.add('opacity-0', 'translate-y-8');

        window.setTimeout(function () {
            modalElements.root.classList.add('hidden');
            document.body.classList.remove('overflow-hidden');
            modalState.files = [];
            modalState.onSuccess = null;
            releaseObjectUrls();
        }, 220);
    }

    function prewarmCloudinaryWidget() {
        injectModal();
        return modalElements;
    }

    window.openCloudinaryWidget = openModal;
    window.prewarmCloudinaryWidget = prewarmCloudinaryWidget;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', prewarmCloudinaryWidget, { once: true });
    } else {
        prewarmCloudinaryWidget();
    }
})();
