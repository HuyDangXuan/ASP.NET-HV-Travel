(function () {
    'use strict';

    var modalState = {
        isOpen: false,
        isUploading: false,
        options: {},
        onSuccess: null,
        files: [],
        activeTab: 'upload',
        library: {
            loaded: false,
            loading: false,
            items: [],
            selected: {},
            nextCursor: '',
            search: '',
            error: ''
        }
    };

    var modalElements = null;
    var objectUrls = [];

    function getConfig() {
        var host = document.querySelector('[data-cloudinary-cloud-name]');
        var config = {
            cloudName: '',
            uploadPreset: '',
            assetsUrl: ''
        };

        if (host) {
            config.cloudName = host.getAttribute('data-cloudinary-cloud-name') || '';
            config.uploadPreset = host.getAttribute('data-cloudinary-upload-preset') || '';
            config.assetsUrl = host.getAttribute('data-cloudinary-assets-url') || '';
        }

        if (window.CloudinaryConfig) {
            config.cloudName = window.CloudinaryConfig.cloudName || config.cloudName;
            config.uploadPreset = window.CloudinaryConfig.uploadPreset || config.uploadPreset;
            config.assetsUrl = window.CloudinaryConfig.assetsUrl || config.assetsUrl;
        }

        return config;
    }

    function ensureUploadConfig() {
        var config = getConfig();
        if (!config.cloudName || !config.uploadPreset) {
            alert('Thiếu cấu hình tải ảnh lên Cloudinary. Hãy kiểm tra Cloudinary:CloudName và Cloudinary:UploadPreset.');
            return null;
        }

        return config;
    }

    function ensureAssetsConfig() {
        var config = getConfig();
        if (!config.assetsUrl) {
            alert('Thiếu cấu hình API duyệt ảnh Cloudinary. Hãy kiểm tra data-cloudinary-assets-url trong layout admin.');
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
            maxFileSize: safeOptions.maxFileSize || 5 * 1024 * 1024,
            selectedUrls: normalizeSelectedUrls(safeOptions.selectedUrls),
            syncSelection: !!safeOptions.syncSelection
        };
    }

    function normalizeSelectedUrls(urls) {
        var seen = {};
        return (Array.isArray(urls) ? urls : []).map(function (url) {
            return String(url || '').trim();
        }).filter(function (url) {
            if (!url || seen[url]) {
                return false;
            }

            seen[url] = true;
            return true;
        });
    }

    function createSelectedAssetMap(urls) {
        return normalizeSelectedUrls(urls).reduce(function (selected, url) {
            selected[url] = {
                secureUrl: url,
                publicId: '',
                thumbnailUrl: url,
                format: '',
                sizeLabel: '',
                width: 0,
                height: 0,
                createdAt: '',
                folder: ''
            };
            return selected;
        }, {});
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
            '          <p class="mb-2 inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/10 px-3 py-1 text-[11px] font-bold uppercase tracking-[0.28em] text-cyan-100">Cloudinary Asset Studio</p>',
            '          <h3 data-cloudinary-title class="text-2xl font-black tracking-tight sm:text-3xl">Quản lý hình ảnh</h3>',
            '          <p data-cloudinary-subtitle class="mt-2 max-w-2xl text-sm text-slate-200/90 sm:text-[15px]">Tải ảnh mới, dán URL hoặc chọn ảnh đã có trong Cloudinary.</p>',
            '        </div>',
            '        <button type="button" data-cloudinary-close class="inline-flex size-11 items-center justify-center rounded-2xl border border-white/15 bg-white/10 text-white transition hover:bg-white/20" aria-label="Đóng modal">',
            '          <span class="material-symbols-outlined text-[24px]">close</span>',
            '        </button>',
            '      </div>',
            '      <div class="relative mt-6 flex flex-wrap gap-3">',
            '        <button type="button" data-cloudinary-tab="upload" class="cloudinary-tab inline-flex items-center gap-2 rounded-2xl px-4 py-3 text-sm font-bold transition"></button>',
            '        <button type="button" data-cloudinary-tab="url" class="cloudinary-tab inline-flex items-center gap-2 rounded-2xl px-4 py-3 text-sm font-bold transition"></button>',
            '        <button type="button" data-cloudinary-tab="library" class="cloudinary-tab inline-flex items-center gap-2 rounded-2xl px-4 py-3 text-sm font-bold transition"></button>',
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
            '                <p class="text-sm text-slate-500">Ảnh sẽ được đưa lên Cloudinary ngay trong cửa sổ này.</p>',
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
            '              <p class="mt-2 text-sm leading-6 text-slate-500">Dán link ảnh công khai để chèn trực tiếp vào form hiện tại.</p>',
            '            </div>',
            '            <label class="mb-2 block text-xs font-bold uppercase tracking-[0.24em] text-slate-400">URL hình ảnh</label>',
            '            <div class="flex flex-col gap-3 sm:flex-row">',
            '              <input type="url" data-cloudinary-url-input placeholder="https://example.com/image.jpg" class="h-14 flex-1 rounded-2xl border border-slate-200 bg-white px-4 text-[15px] font-medium text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-sky-400 focus:ring-4 focus:ring-sky-100" />',
            '              <button type="button" data-cloudinary-add-url class="inline-flex items-center justify-center gap-2 rounded-2xl bg-slate-900 px-5 py-3 text-sm font-extrabold text-white transition hover:bg-slate-800">',
            '                <span class="material-symbols-outlined text-[20px]">add_link</span> Chèn vào form',
            '              </button>',
            '            </div>',
            '            <p class="mt-3 text-xs text-slate-400">Phù hợp khi bạn đã có URL ảnh từ Cloudinary hoặc CDN khác.</p>',
            '          </div>',
            '        </section>',
            '        <section data-cloudinary-view="library" class="hidden space-y-5">',
            '          <div class="rounded-[1.75rem] border border-slate-200 bg-slate-50/80 p-5 sm:p-6">',
            '            <div class="flex flex-col gap-3 md:flex-row md:items-center">',
            '              <div class="flex-1">',
            '                <label class="mb-2 block text-xs font-bold uppercase tracking-[0.24em] text-slate-400">Tìm ảnh theo tên hoặc public id</label>',
            '                <input type="search" data-cloudinary-library-search placeholder="Ví dụ: hạ long" class="h-12 w-full rounded-2xl border border-slate-200 bg-white px-4 text-sm font-medium text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-sky-400 focus:ring-4 focus:ring-sky-100" />',
            '              </div>',
            '              <div class="flex gap-3 md:self-end">',
            '                <button type="button" data-cloudinary-library-search-button class="inline-flex items-center gap-2 rounded-2xl bg-sky-600 px-4 py-3 text-sm font-extrabold text-white transition hover:bg-sky-500">',
            '                  <span class="material-symbols-outlined text-[20px]">search</span> Tìm',
            '                </button>',
            '                <button type="button" data-cloudinary-library-refresh class="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 transition hover:border-sky-200 hover:text-sky-700">',
            '                  <span class="material-symbols-outlined text-[20px]">refresh</span> Tải lại',
            '                </button>',
            '              </div>',
            '            </div>',
            '          </div>',
            '          <div class="mb-4 rounded-[1.5rem] border border-slate-200 bg-white/95 px-4 py-4 shadow-sm backdrop-blur">',
            '            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">',
            '              <div class="space-y-1">',
            '                <p class="text-sm font-black text-slate-900">Xác nhận ảnh sẽ dùng cho tour</p>',
            '                <p data-cloudinary-library-selection class="text-sm font-medium text-slate-500">Chưa có thay đổi nào</p>',
            '              </div>',
            '              <div class="flex flex-wrap gap-3">',
            '                <button type="button" data-cloudinary-library-load-more class="hidden inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-bold text-slate-700 transition hover:border-sky-200 hover:text-sky-700">',
            '                  <span class="material-symbols-outlined text-[20px]">expand_more</span> Tải thêm',
            '                </button>',
            '                <button type="button" data-cloudinary-library-confirm-button class="hidden inline-flex items-center gap-2 rounded-2xl bg-slate-900 px-4 py-3 text-sm font-extrabold text-white transition hover:bg-slate-800">',
            '                  <span class="material-symbols-outlined text-[20px]">check_circle</span> Xác nhận ảnh đã chọn',
            '                </button>',
            '              </div>',
            '            </div>',
            '          </div>',
            '          <div data-cloudinary-library-scroll class="max-h-[min(52vh,30rem)] overflow-y-auto pr-1 sm:max-h-[min(58vh,36rem)] sm:pr-2"><div data-cloudinary-library-grid class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"></div></div>',
            '        </section>',
            '      </div>',
            '      <aside class="overflow-y-auto border-t border-slate-200/80 bg-slate-50/80 p-6 sm:p-8 xl:border-t-0">',
            '        <div class="rounded-[1.75rem] border border-slate-200 bg-white p-5 shadow-sm">',
            '          <div class="flex items-center gap-3">',
            '            <div class="flex size-12 items-center justify-center rounded-2xl bg-sky-50 text-sky-600">',
            '              <span class="material-symbols-outlined text-[24px]">bolt</span>',
            '            </div>',
            '            <div>',
            '              <p class="text-sm font-black text-slate-900">Làm việc trong cùng một modal</p>',
            '              <p class="text-xs text-slate-500">Tải ảnh mới và duyệt ảnh có sẵn trong cùng một giao diện.</p>',
            '            </div>',
            '          </div>',
            '          <div class="mt-5 grid gap-3">',
            '            <div class="rounded-2xl bg-slate-50 px-4 py-3">',
            '              <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-slate-400">Thư mục</p>',
            '              <p data-cloudinary-folder class="mt-1 text-sm font-bold text-slate-800">HV-Travel ASP.NET</p>',
            '            </div>',
            '            <div class="rounded-2xl bg-slate-50 px-4 py-3">',
            '              <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-slate-400">Định dạng</p>',
            '              <p data-cloudinary-formats class="mt-1 text-sm font-bold text-slate-800">PNG, JPG, JPEG, WEBP, GIF</p>',
            '            </div>',
            '            <div class="rounded-2xl bg-slate-50 px-4 py-3">',
            '              <p class="text-[11px] font-bold uppercase tracking-[0.2em] text-slate-400">Trạng thái</p>',
            '              <p data-cloudinary-status class="mt-1 text-sm font-bold text-slate-800">Sẵn sàng</p>',
            '            </div>',
            '          </div>',
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
            title: wrapper.querySelector('[data-cloudinary-title]'),
            subtitle: wrapper.querySelector('[data-cloudinary-subtitle]'),
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
            addUrlButton: wrapper.querySelector('[data-cloudinary-add-url]'),
            librarySearchInput: wrapper.querySelector('[data-cloudinary-library-search]'),
            librarySearchButton: wrapper.querySelector('[data-cloudinary-library-search-button]'),
            libraryRefreshButton: wrapper.querySelector('[data-cloudinary-library-refresh]'),
            libraryGrid: wrapper.querySelector('[data-cloudinary-library-grid]'),
            libraryLoadMoreButton: wrapper.querySelector('[data-cloudinary-library-load-more]'),
            libraryConfirmButton: wrapper.querySelector('[data-cloudinary-library-confirm-button]'),
            librarySelectionLabel: wrapper.querySelector('[data-cloudinary-library-selection]')
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

        modalElements.librarySearchButton.addEventListener('click', function () {
            modalState.library.search = (modalElements.librarySearchInput.value || '').trim();
            loadLibraryAssets(true);
        });

        modalElements.libraryRefreshButton.addEventListener('click', function () {
            loadLibraryAssets(true);
        });

        modalElements.librarySearchInput.addEventListener('keydown', function (event) {
            if (event.key === 'Enter') {
                event.preventDefault();
                modalState.library.search = (modalElements.librarySearchInput.value || '').trim();
                loadLibraryAssets(true);
            }
        });

        modalElements.libraryLoadMoreButton.addEventListener('click', function () {
            loadLibraryAssets(false);
        });

        modalElements.libraryConfirmButton.addEventListener('click', submitSelectedAssets);
    }

    function setActiveTab(tab) {
        var safeTab = tab === 'url' || tab === 'library' ? tab : 'upload';
        modalState.activeTab = safeTab;

        modalElements.tabs.forEach(function (button) {
            var buttonTab = button.getAttribute('data-cloudinary-tab');
            var isActive = buttonTab === modalState.activeTab;
            button.className = isActive
                ? 'cloudinary-tab inline-flex items-center gap-2 rounded-2xl bg-white text-slate-900 shadow-lg shadow-slate-950/10 px-4 py-3 text-sm font-extrabold transition'
                : 'cloudinary-tab inline-flex items-center gap-2 rounded-2xl border border-white/15 bg-white/10 text-slate-100/85 px-4 py-3 text-sm font-bold transition hover:bg-white/15';

            if (buttonTab === 'upload') {
                button.innerHTML = '<span class="material-symbols-outlined text-[20px]">upload</span>Tải từ máy';
            } else if (buttonTab === 'url') {
                button.innerHTML = '<span class="material-symbols-outlined text-[20px]">link</span>Dán URL';
            } else {
                button.innerHTML = '<span class="material-symbols-outlined text-[20px]">photo_library</span>Thư viện ảnh';
            }
        });

        modalElements.views.forEach(function (view) {
            var isVisible = view.getAttribute('data-cloudinary-view') === modalState.activeTab;
            view.classList.toggle('hidden', !isVisible);
        });

        if (modalState.activeTab === 'upload') {
            modalElements.title.textContent = 'Tải hình ảnh mới';
            modalElements.subtitle.textContent = 'Kéo thả file, chọn ảnh từ máy hoặc mở camera để tải lên Cloudinary.';
            setStatus(modalState.isUploading ? 'Đang tải lên Cloudinary...' : 'Sẵn sàng để tải lên');
        } else if (modalState.activeTab === 'url') {
            modalElements.title.textContent = 'Chèn ảnh bằng URL';
            modalElements.subtitle.textContent = 'Dán liên kết công khai để chèn trực tiếp vào biểu mẫu hiện tại mà không cần tải lên lại.';
            setStatus('Chờ URL hình ảnh');
        } else {
            modalElements.title.textContent = 'Chọn ảnh đã có trong Cloudinary';
            modalElements.subtitle.textContent = 'Duyệt, tìm kiếm và chọn lại ảnh đã tải lên để tránh tải trùng lặp.';
            setStatus(modalState.library.loading ? 'Đang tải danh sách ảnh...' : 'Sẵn sàng chọn ảnh đã có');
            if (!modalState.library.loaded && !modalState.library.loading) {
                loadLibraryAssets(true);
            } else {
                renderLibrary();
            }
        }
    }

    function setStatus(text) {
        modalElements.statusLabel.textContent = text;
    }

    function renderFileList() {
        if (!modalState.files.length) {
            modalElements.fileList.innerHTML = [
                '<div class="rounded-[1.5rem] border border-dashed border-slate-200 bg-white px-5 py-6 text-center">',
                '  <p class="text-sm font-bold text-slate-700">Chưa có file nào trong hàng đợi</p>',
                '  <p class="mt-1 text-sm text-slate-400">Thêm ảnh từ máy, camera hoặc kéo thả vào vùng upload.</p>',
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
        var config = ensureUploadConfig();
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
        setStatus('Đã chèn liên kết ảnh vào form');
    }

    function normalizeAsset(item) {
        if (!item) return null;

        var secureUrl = item.secureUrl || item.secure_url || item.url || '';
        if (!secureUrl) return null;

        return {
            secureUrl: secureUrl,
            publicId: item.publicId || item.public_id || '',
            thumbnailUrl: item.thumbnailUrl || item.thumbnail_url || secureUrl,
            format: (item.format || '').toUpperCase(),
            sizeLabel: item.sizeLabel || item.size_label || '',
            width: item.width || 0,
            height: item.height || 0,
            createdAt: item.createdAt || item.created_at || '',
            folder: item.folder || ''
        };
    }

    function getSelectionCount() {
        return Object.keys(modalState.library.selected).length;
    }
    function getSelectedAssetUrls() {
        return normalizeSelectedUrls(Object.keys(modalState.library.selected));
    }
    function getInitialSelectedAssetUrls() {
        return normalizeSelectedUrls(Object.keys(modalState.library.initialSelectedUrls || {}));
    }
    function getLibraryChangeCount() {
        var selectedMap = modalState.library.selected || {};
        var initialMap = modalState.library.initialSelectedUrls || {};
        var changeCount = 0;
        Object.keys(selectedMap).forEach(function (url) {
            if (!initialMap[url]) {
                changeCount += 1;
            }
        });
        Object.keys(initialMap).forEach(function (url) {
            if (!selectedMap[url]) {
                changeCount += 1;
            }
        });
        return changeCount;
    }
    function hasLibrarySelectionChanges() {
        return getLibraryChangeCount() > 0;
    }
    function renderLibrary() {
        var library = modalState.library;
        var itemsMarkup = '';

        if (library.loading && !library.items.length) {
            itemsMarkup = new Array(6).fill(0).map(function () {
                return '<div class="overflow-hidden rounded-[1.5rem] border border-slate-200 bg-white p-3 shadow-sm"><div class="aspect-[4/3] rounded-2xl bg-slate-100 animate-pulse"></div><div class="mt-3 h-4 rounded bg-slate-100 animate-pulse"></div><div class="mt-2 h-3 w-2/3 rounded bg-slate-100 animate-pulse"></div></div>';
            }).join('');
        } else if (library.error) {
            itemsMarkup = '<div class="sm:col-span-2 xl:col-span-3 rounded-[1.5rem] border border-rose-200 bg-rose-50 px-5 py-6 text-sm text-rose-700">' + escapeHtml(library.error) + '</div>';
        } else if (!library.items.length) {
            itemsMarkup = '<div class="sm:col-span-2 xl:col-span-3 rounded-[1.5rem] border border-dashed border-slate-200 bg-white px-5 py-10 text-center"><p class="text-base font-black text-slate-900">Không tìm thấy ảnh phù hợp</p><p class="mt-2 text-sm text-slate-500">Thử đổi từ khóa tìm kiếm hoặc tải thêm ảnh mới.</p></div>';
        } else {
            itemsMarkup = library.items.map(function (item, index) {
                var isSelected = !!library.selected[item.secureUrl];
                var meta = [];
                if (item.format) meta.push(item.format);
                if (item.sizeLabel) meta.push(item.sizeLabel);
                if (item.width && item.height) meta.push(item.width + 'x' + item.height);

                return [
                    '<button type="button" data-cloudinary-asset-index="' + index + '" class="group overflow-hidden rounded-[1.5rem] border p-3 text-left shadow-sm transition ' + (isSelected ? 'border-sky-400 bg-sky-50/70 ring-2 ring-sky-200' : 'border-slate-200 bg-white hover:-translate-y-0.5 hover:border-sky-200 hover:shadow-lg') + '">',
                    '  <div class="relative aspect-[4/3] overflow-hidden rounded-2xl bg-slate-100">',
                    '    <img src="' + escapeHtml(item.thumbnailUrl) + '" alt="" class="h-full w-full object-cover transition duration-300 group-hover:scale-[1.02]" loading="lazy" />',
                    '    <span class="absolute right-3 top-3 inline-flex size-8 items-center justify-center rounded-full ' + (isSelected ? 'bg-sky-600 text-white' : 'bg-white/90 text-slate-400') + '">',
                    '      <span class="material-symbols-outlined text-[18px]">' + (isSelected ? 'check' : 'add') + '</span>',
                    '    </span>',
                    '  </div>',
                    '  <div class="mt-3">',
                    '    <p class="truncate text-sm font-black text-slate-900" title="' + escapeHtml(item.publicId || item.secureUrl) + '">' + escapeHtml(item.publicId || item.secureUrl) + '</p>',
                    '    <p class="mt-1 text-xs font-medium text-slate-500">' + escapeHtml(meta.join(' • ') || 'Ảnh') + '</p>',
                    '  </div>',
                    '</button>'
                ].join('');
            }).join('');
        }

        var selectionCount = getSelectionCount();
        var hasChanges = hasLibrarySelectionChanges();
        var changeCount = getLibraryChangeCount();
        modalElements.libraryGrid.innerHTML = itemsMarkup;
        if (modalState.options.syncSelection) {
            modalElements.librarySelectionLabel.textContent = hasChanges
                ? 'Có ' + changeCount + ' thay đổi chưa xác nhận. Đang chọn ' + selectionCount + ' ảnh.'
                : (selectionCount > 0 ? 'Đang dùng ' + selectionCount + ' ảnh cho tour.' : 'Chưa có ảnh nào được chọn cho tour.');
        } else {
            modalElements.librarySelectionLabel.textContent = selectionCount > 0
                ? 'Đã chọn ' + selectionCount + ' ảnh'
                : 'Chưa chọn ảnh nào';
        }
        modalElements.libraryLoadMoreButton.classList.toggle('hidden', !library.nextCursor || library.loading);
        modalElements.libraryLoadMoreButton.disabled = library.loading;
        modalElements.libraryConfirmButton.classList.toggle('hidden', !modalState.options.syncSelection || !hasChanges);
        modalElements.libraryConfirmButton.disabled = !modalState.options.syncSelection || !hasChanges;
        modalElements.libraryConfirmButton.classList.toggle('opacity-60', !modalState.options.syncSelection || !hasChanges);
        Array.prototype.slice.call(modalElements.libraryGrid.querySelectorAll('[data-cloudinary-asset-index]')).forEach(function (button) {
            button.addEventListener('click', function () {
                var index = parseInt(button.getAttribute('data-cloudinary-asset-index'), 10);
                toggleSelectedAsset(modalState.library.items[index]);
            });
        });
    }

    function toggleSelectedAsset(item) {
        if (!item) return;

        if (!modalState.options.multiple) {
            modalState.library.selected = {};
            modalState.library.selected[item.secureUrl] = item;
            renderLibrary();
            submitSelectedAssets();
            return;
        }

        if (modalState.library.selected[item.secureUrl]) {
            delete modalState.library.selected[item.secureUrl];
        } else {
            modalState.library.selected[item.secureUrl] = item;
        }

        renderLibrary();
    }

    function submitSelectedAssets() {
        var selectedItems = Object.keys(modalState.library.selected).map(function (key) {
            return modalState.library.selected[key];
        }).filter(function (item) {
            return item && item.secureUrl;
        });
        if (typeof modalState.onSuccess !== 'function') {
            closeModal();
            return;
        }
        if (modalState.options.syncSelection) {
            var selectedUrls = getSelectedAssetUrls();
            modalState.onSuccess(selectedUrls, {
                selectedUrls: selectedUrls.slice(),
                source: 'cloudinary_library',
                syncSelection: true
            });
            closeModal();
            return;
        }
        if (!selectedItems.length) {
            closeModal();
            return;
        }
        var newSelectedItems = selectedItems.filter(function (item) {
            return item && item.secureUrl && !modalState.library.initialSelectedUrls[item.secureUrl];
        });
        newSelectedItems.forEach(function (item) {
            modalState.onSuccess(item.secureUrl, {
                secure_url: item.secureUrl,
                public_id: item.publicId,
                source: 'cloudinary_library'
            });
        });
        closeModal();
    }
    function loadLibraryAssets(reset) {
        var config = ensureAssetsConfig();
        if (!config) {
            return Promise.resolve();
        }

        if (modalState.library.loading) {
            return Promise.resolve();
        }

        if (reset) {
            modalState.library.items = [];
            modalState.library.nextCursor = '';
            modalState.library.error = '';
            modalState.library.loaded = false;
            if (modalElements.librarySearchInput) {
                modalState.library.search = (modalElements.librarySearchInput.value || '').trim();
            }
        }

        modalState.library.loading = true;
        setStatus('Đang tải danh sách ảnh...');
        renderLibrary();

        var query = new URLSearchParams();
        if (modalState.options.folder) {
            query.set('folder', modalState.options.folder);
        }
        if (modalState.library.search) {
            query.set('search', modalState.library.search);
        }
        if (!reset && modalState.library.nextCursor) {
            query.set('cursor', modalState.library.nextCursor);
        }
        query.set('maxResults', modalState.options.multiple ? '24' : '18');

        return fetch(config.assetsUrl + '?' + query.toString(), {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        }).then(function (response) {
            if (!response.ok) {
                return response.json().catch(function () { return {}; }).then(function (payload) {
                    var message = payload && payload.message
                        ? payload.message
                        : 'Không thể tải danh sách ảnh từ Cloudinary.';
                    throw new Error(message);
                });
            }

            return response.json();
        }).then(function (payload) {
            var incoming = Array.isArray(payload.items) ? payload.items : [];
            var mappedItems = incoming.map(normalizeAsset).filter(function (item) { return !!item; });
            var seen = {};
            var merged = (reset ? [] : modalState.library.items.slice()).concat(mappedItems).filter(function (item) {
                if (seen[item.secureUrl]) {
                    return false;
                }

                seen[item.secureUrl] = true;
                return true;
            });

            modalState.library.items = merged;
            modalState.library.nextCursor = payload.nextCursor || payload.next_cursor || '';
            modalState.library.loaded = true;
            modalState.library.error = '';
            setStatus(merged.length ? 'Đã tải ' + merged.length + ' ảnh từ Cloudinary' : 'Không có ảnh nào phù hợp');
        }).catch(function (error) {
            modalState.library.error = error && error.message
                ? error.message
                : 'Không thể tải danh sách ảnh từ Cloudinary.';
            setStatus('Tải danh sách ảnh thất bại');
        }).finally(function () {
            modalState.library.loading = false;
            renderLibrary();
        });
    }

    function openModal(options, onSuccess, initialTab) {
        injectModal();
        releaseObjectUrls();

        modalState.options = normalizeOptions(options);
        modalState.onSuccess = onSuccess;
        modalState.files = [];
        modalState.isUploading = false;
        modalState.isOpen = true;
        modalState.activeTab = initialTab === 'library' ? 'library' : (initialTab === 'url' ? 'url' : 'upload');
        var initialSelectedAssets = createSelectedAssetMap(modalState.options.selectedUrls);
        modalState.library = {
            loaded: false,
            loading: false,
            items: [],
            initialSelectedUrls: initialSelectedAssets,
            selected: Object.assign({}, initialSelectedAssets),
            nextCursor: '',
            search: '',
            error: ''
        };

        modalElements.fileInput.multiple = modalState.options.multiple;
        modalElements.cameraInput.multiple = modalState.options.multiple;
        modalElements.folderLabel.textContent = modalState.options.folder || 'Mặc định';
        modalElements.formatsLabel.textContent = modalState.options.allowedFormats.join(', ').toUpperCase();
        modalElements.urlInput.value = '';
        modalElements.librarySearchInput.value = '';

        renderFileList();
        renderLibrary();
        setActiveTab(modalState.activeTab);

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
            modalState.library.selected = {};
            releaseObjectUrls();
        }, 220);
    }

    function openCloudinaryWidget(options, onSuccess) {
        openModal(options, onSuccess, 'upload');
    }

    function openCloudinaryAssetBrowser(options, onSuccess) {
        if (!ensureAssetsConfig()) {
            return;
        }

        openModal(options, onSuccess, 'library');
    }

    function prewarmCloudinaryWidget() {
        injectModal();
        return modalElements;
    }

    function prewarmCloudinaryAssetBrowser() {
        injectModal();
        return modalElements;
    }

    window.openCloudinaryWidget = openCloudinaryWidget;
    window.prewarmCloudinaryWidget = prewarmCloudinaryWidget;
    window.openCloudinaryAssetBrowser = openCloudinaryAssetBrowser;
    window.prewarmCloudinaryAssetBrowser = prewarmCloudinaryAssetBrowser;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            prewarmCloudinaryWidget();
            prewarmCloudinaryAssetBrowser();
        }, { once: true });
    } else {
        prewarmCloudinaryWidget();
        prewarmCloudinaryAssetBrowser();
    }
})();


