// ---------------------
// Helpers: Phone Format
// ---------------------
function normalizePhone(phone) {
    if (!phone) return null;
    let s = String(phone).trim();

    if (s.startsWith('+98')) {
        s = '0' + s.substring(3);
    } else if (s.startsWith('0098')) {
        s = '0' + s.substring(4);
    }

    s = s.replace(/[^\d]/g, '');
    if (!/^09\d{9}$/.test(s)) return null;

    return s;
}

function formatPhone(phone) {
    const normalized = normalizePhone(phone);
    if (!normalized) return '—';
    return normalized;
}

// ---------------------
// Helpers: Badges & Date
// ---------------------
function renderStatusBadge(isLocked) {
    return isLocked
        ? `<span class="badge badge-danger">قفل شده</span>`
        : `<span class="badge badge-success">فعال</span>`;
}

function renderPhoneConfirmBadge(isConfirmed) {
    return isConfirmed
        ? `<span class="badge badge-success ms-1">تأیید شده</span>`
        : `<span class="badge badge-warning ms-1">تأیید نشده</span>`;
}

function renderRolesBadges(roles) {
    if (!Array.isArray(roles) || roles.length === 0) {
        return '<span class="text-muted">—</span>';
    }

    return roles
        .map(r => `<span class="badge badge-info ms-1">${r}</span>`)
        .join(' ');
}

function formatDate(value) {
    if (!value) return "—";
    const date = new Date(value);
    return date.toLocaleString("fa-IR", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit"
    });
}

// ---------------------
// API & Paging State
// ---------------------
const API_URL = "/api/v1/admin/users";

let currentPage = 1;
let pageSize = 20;

// debounce helper
function debounce(fn, delay) {
    let timerId;
    return function (...args) {
        clearTimeout(timerId);
        timerId = setTimeout(() => fn.apply(this, args), delay);
    };
}

document.addEventListener("DOMContentLoaded", () => {
    const searchInput = document.getElementById("search");
    const statusSelect = document.getElementById("status");
    const pageSizeSelect = document.getElementById("pageSize");
    const createdAtFromInput = document.getElementById("createdAtFrom");
    const createdAtToInput = document.getElementById("createdAtTo");
    const lastLoginAtFromInput = document.getElementById("lastLoginAtFrom");
    const lastLoginAtToInput = document.getElementById("lastLoginAtTo");
    const form = document.getElementById("userFilterForm");

    // جلوگیری از submit فرم (برای Enter)
    form?.addEventListener("submit", (event) => {
        event.preventDefault();
    });

    // 🔍 سرچ لایو با debounce
    const MIN_SEARCH_LENGTH = 3;

    if (searchInput) {
        const debouncedSearch = debounce(() => {
            const term = searchInput.value.trim();

            // ۱) اگر کلاً خالی شد → یعنی سرچ پاک شده → کل لیست رو بیار
            if (term.length === 0) {
                currentPage = 1;
                loadUsers();
                return;
            }

            // ۲) اگر کمتر از ۳ کاراکتر بود → هیچ درخواستی نفرست
            if (term.length < MIN_SEARCH_LENGTH) {
                // اینجا عمداً هیچ کاری نمی‌کنیم
                return;
            }

            // ۳) از ۳ به بالا → سرچ کن
            currentPage = 1;
            loadUsers();
        }, 400);

        searchInput.addEventListener("input", debouncedSearch);
    }

    // تغییر وضعیت
    statusSelect?.addEventListener("change", () => {
        currentPage = 1;
        loadUsers();
    });

    // تغییر فیلترهای تاریخ
    const dateInputs = [createdAtFromInput, createdAtToInput, lastLoginAtFromInput, lastLoginAtToInput];
    dateInputs.forEach(input => {
        input?.addEventListener("change", () => {
            currentPage = 1;
            loadUsers();
        });
    });

    // تغییر pageSize
    pageSizeSelect?.addEventListener("change", () => {
        pageSize = Number(pageSizeSelect.value) || 20;
        currentPage = 1;
        loadUsers();
    });

    // Setup action handlers (event delegation - یکبار)
    setupRowActions();

    // اولین بار
    loadUsers();
});

// ---------------------
// Load Users from API
// ---------------------
let controller = null;

async function loadUsers() {
    const searchEl = document.getElementById("search");
    const statusEl = document.getElementById("status");
    const createdAtFromEl = document.getElementById("createdAtFrom");
    const createdAtToEl = document.getElementById("createdAtTo");
    const lastLoginAtFromEl = document.getElementById("lastLoginAtFrom");
    const lastLoginAtToEl = document.getElementById("lastLoginAtTo");

    const searchValueRaw = searchEl?.value ?? "";
    const searchValue = searchValueRaw.trim();
    const statusValue = statusEl?.value ?? "";

    const params = new URLSearchParams({
        page: currentPage,
        pageSize: pageSize
    });

    // فقط وقتی سرچ رو بفرست که یا خالیه (بالا هندل کردیم) یا طولش >= 3 باشه
    if (searchValue.length >= 3) {
        params.set("search", searchValue);
    }

    if (statusValue) params.set("status", statusValue);

    // فیلترهای تاریخ
    if (createdAtFromEl?.value) {
        params.set("createdAtFrom", createdAtFromEl.value);
    }
    if (createdAtToEl?.value) {
        params.set("createdAtTo", createdAtToEl.value);
    }
    if (lastLoginAtFromEl?.value) {
        params.set("lastLoginAtFrom", lastLoginAtFromEl.value);
    }
    if (lastLoginAtToEl?.value) {
        params.set("lastLoginAtTo", lastLoginAtToEl.value);
    }

    // بقیه همون کدی که خودت نوشتی 👇
    if (controller) controller.abort();
    controller = new AbortController();

    try {
        const response = await fetch(`${API_URL}?${params.toString()}`, {
            method: "GET",
            credentials: "same-origin",
            headers: { "X-Requested-With": "XMLHttpRequest" },
            signal: controller.signal
        });

        if (!response.ok) {
            console.error("Load failed", response.status);
            return;
        }

        const payload = await response.json();
        const data = payload?.data ?? payload;

        renderTable(data);
        renderPagination(data);
        updateInfo(data);
    } catch (error) {
        if (error.name === "AbortError") {
            return;
        }
        console.error("Error loading users", error);
    }
}

// ---------------------
// Render Table
// ---------------------
function renderTable(data) {
    const tbody = document.querySelector("#usersTable tbody");
    if (!tbody) return;

    tbody.innerHTML = "";

    if (!data || !Array.isArray(data.items) || data.items.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 9;
        td.className = "text-center text-muted py-4";
        td.textContent = "هیچ کاربری یافت نشد";
        tr.appendChild(td);
        tbody.appendChild(tr);
        return;
    }

    // محاسبه شماره ردیف بر اساس صفحه فعلی
    const startRowNumber = (currentPage - 1) * pageSize + 1;

    data.items.forEach((user, index) => {
        const tr = document.createElement("tr");
        const rowNumber = startRowNumber + index;

        const phoneFormatted = formatPhone(user.phone);
        const hasName = !!user.fullName;
        const displayName = hasName
            ? user.fullName
            : (phoneFormatted !== "—" ? phoneFormatted : "کاربر بدون نام");

        const email = user.email || "—";
        const rolesHtml = renderRolesBadges(user.roles);
        const createdAt = formatDate(user.createdAtUtc);
        const lastLogin = user.lastLoginAtUtc ? formatDate(user.lastLoginAtUtc) : "—";
        const statusHtml = renderStatusBadge(user.isLocked);
        const phoneConfirmHtml = renderPhoneConfirmBadge(user.isPhoneConfirmed);

        tr.innerHTML = `
            <td class="text-center"><span class="fa-num">${rowNumber}</span></td>
            <td>
                <div class="d-flex flex-column">
                    <span class="fw-bold">${displayName}</span>
                </div>
            </td>
            <td><span class="fa-num">${phoneFormatted}</span></td>
            <td>${email}</td>
            <td>${rolesHtml}</td>
            <td>
                ${statusHtml}
                ${phoneConfirmHtml}
            </td>
            <td><span class="fa-num">${createdAt}</span></td>
            <td><span class="fa-num">${lastLogin}</span></td>
            <td class="center text-center">
                 <a href="#" data-user-id="${user.id}" class="btn btn-info btn-xs" data-action="details">
                    <i class="fa fa-edit"></i> جزئیات
                 </a>
             
                 <a href="#" data-user-id="${user.id}" data-is-locked="${user.isLocked}" class="btn btn-danger btn-xs" data-action="toggle-lock">
                    <i class="fa ${user.isLocked ? 'fa-unlock' : 'fa-lock'}"></i> ${user.isLocked ? "آنلاک" : "لاک"}
                 </a>
            </td>
        `;

        tbody.appendChild(tr);
    });
}

// ---------------------
// Setup Row Actions (Event Delegation)
// ---------------------
function setupRowActions() {
    const table = document.getElementById("usersTable");
    if (!table) return;

    // Event delegation: یکبار setup می‌شود و برای همه ردیف‌ها کار می‌کند
    table.addEventListener("click", async (event) => {
        const link = event.target.closest("a[data-action]");
        if (!link) return;

        event.preventDefault();

        const userId = link.getAttribute("data-user-id");
        const action = link.getAttribute("data-action");

        if (!userId || !action) return;

        if (action === "details") {
            await openUserDetailsModal(userId);
        } else if (action === "toggle-lock") {
            await toggleUserLock(userId, link);
        }
    });
}

// ---------------------
// Update User Row Directly (after lock/unlock)
// ---------------------
async function updateUserRowDirectly(userId, buttonEl) {
    try {
        // گرفتن اطلاعات به‌روز شده کاربر از API
        const response = await fetch(`/api/v1/admin/users/${userId}`, {
            method: "GET",
            credentials: "same-origin",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        if (!response.ok) {
            console.warn("Failed to fetch updated user, refreshing full table...");
            // Fallback: refresh کامل جدول
            await loadUsers();
            return;
        }

        const payload = await response.json();
        const user = payload?.data ?? payload;

        // پیدا کردن ردیف کاربر در جدول
        const row = document.querySelector(`#usersTable tbody tr a[data-user-id="${userId}"]`)?.closest('tr');
        if (!row) {
            console.warn("User row not found, refreshing full table...");
            await loadUsers();
            return;
        }

        // به‌روزرسانی محتوای ردیف
        const phoneFormatted = formatPhone(user.phone);
        const hasName = !!user.fullName;
        const displayName = hasName
            ? user.fullName
            : (phoneFormatted !== "—" ? phoneFormatted : "کاربر بدون نام");

        const email = user.email || "—";
        const rolesHtml = renderRolesBadges(user.roles);
        const createdAt = formatDate(user.createdAtUtc);
        const lastLogin = user.lastLoginAtUtc ? formatDate(user.lastLoginAtUtc) : "—";
        const statusHtml = renderStatusBadge(user.isLocked);
        const phoneConfirmHtml = renderPhoneConfirmBadge(user.isPhoneConfirmed);

        // به‌روزرسانی سلول‌های ردیف (شماره ردیف تغییر نمی‌کند)
        const cells = row.querySelectorAll('td');
        if (cells.length >= 9) {
            // cells[0] = شماره ردیف (تغییر نمی‌کند)
            cells[1].innerHTML = `
                <div class="d-flex flex-column">
                    <span class="fw-bold">${displayName}</span>
                </div>
            `;
            cells[2].innerHTML = `<span class="fa-num">${phoneFormatted}</span>`;
            cells[3].textContent = email;
            cells[4].innerHTML = rolesHtml;
            cells[5].innerHTML = `${statusHtml} ${phoneConfirmHtml}`;
            cells[6].innerHTML = `<span class="fa-num">${createdAt}</span>`;
            cells[7].innerHTML = `<span class="fa-num">${lastLogin}</span>`;
            cells[8].innerHTML = `
                 <a href="#" data-user-id="${user.id}" class="btn btn-info btn-xs" data-action="details">
                    <i class="fa fa-edit"></i> جزئیات
                 </a>
             
                 <a href="#" data-user-id="${user.id}" data-is-locked="${user.isLocked}" class="btn btn-danger btn-xs" data-action="toggle-lock">
                    <i class="fa ${user.isLocked ? 'fa-unlock' : 'fa-lock'}"></i> ${user.isLocked ? "آنلاک" : "لاک"}
                 </a>
            `;
        }
    } catch (err) {
        console.error("Error updating user row directly:", err);
        // Fallback: refresh کامل جدول
        await loadUsers();
    }
}

// ---------------------
// Open User Details Modal
// ---------------------
async function openUserDetailsModal(userId) {
    const modalBody = document.getElementById("userDetailsContent");
    if (!modalBody) return;

    modalBody.innerHTML = `<p class="text-muted">در حال بارگذاری...</p>`;

    try {
        const response = await fetch(`/api/v1/admin/users/${userId}`, {
            method: "GET",
            credentials: "same-origin",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        if (!response.ok) {
            modalBody.innerHTML = `<p class="text-danger">خطا در دریافت اطلاعات کاربر</p>`;
            $("#userDetailsModal").modal("show");
            return;
        }

        const payload = await response.json();
        const data = payload.data ?? payload;

        const createdAt = formatDate(data.createdAtUtc);
        const lastLogin = data.lastLoginAtUtc ? formatDate(data.lastLoginAtUtc) : "—";
        const phone = formatPhone(data.phone);
        const rolesHtml = renderRolesBadges(data.roles);

        modalBody.innerHTML = `
            <dl class="dl-horizontal">
                <dt>نام کاربر</dt>
                <dd>${data.fullName || "—"}</dd>

                <dt>شماره موبایل</dt>
                <dd><span class="fa-num">${phone}</span></dd>

                <dt>ایمیل</dt>
                <dd>${data.email || "—"}</dd>

                <dt>نقش‌ها</dt>
                <dd>${rolesHtml}</dd>

                <dt>تأیید موبایل</dt>
                <dd>${data.isPhoneConfirmed ? "بله" : "خیر"}</dd>

                <dt>وضعیت قفل</dt>
                <dd>${data.isLocked ? "قفل شده" : "فعال"}</dd>

                <dt>تاریخ ایجاد</dt>
                <dd><span class="fa-num">${createdAt}</span></dd>

                <dt>آخرین ورود</dt>
                <dd><span class="fa-num">${lastLogin}</span></dd>
            </dl>
        `;

        $("#userDetailsModal").modal("show");
    } catch (err) {
        console.error(err);
        modalBody.innerHTML = `<p class="text-danger">خطای غیرمنتظره</p>`;
        $("#userDetailsModal").modal("show");
    }
}

// ---------------------
// Toggle User Lock/Unlock
// ---------------------
async function toggleUserLock(userId, buttonEl) {
    // استفاده از data attribute به جای textContent
    const isCurrentlyLocked = buttonEl.getAttribute("data-is-locked") === "true";
    
    const action = isCurrentlyLocked ? "باز کردن قفل" : "قفل کردن";
    const actionText = isCurrentlyLocked ? "باز کردن قفل کاربر" : "قفل کردن کاربر";
    const confirmText = isCurrentlyLocked 
        ? "آیا مطمئن هستید که می‌خواهید قفل این کاربر را باز کنید؟"
        : "آیا مطمئن هستید که می‌خواهید این کاربر را قفل کنید؟";

    // نمایش confirmation dialog با Custom Popup
    const confirmResult = await CustomPopup.confirm(
        actionText,
        confirmText,
        'بله، ادامه بده',
        'انصراف'
    );

    // اگر کاربر انصراف داد
    if (!confirmResult || !confirmResult.isConfirmed) {
        return;
    }

    // Disable button during operation
    const originalText = buttonEl.innerHTML;
    buttonEl.disabled = true;
    buttonEl.innerHTML = '<i class="fa fa-spinner fa-spin"></i> در حال انجام...';

    const url = isCurrentlyLocked
        ? `/api/v1/admin/users/${userId}/unlock`
        : `/api/v1/admin/users/${userId}/lock`;

    try {
        const response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: "{}"
        });

        if (!response.ok) {
            console.error("Lock/unlock failed", response.status);
            buttonEl.disabled = false;
            buttonEl.innerHTML = originalText;
            
            // نمایش خطا با Toast (غیرمزاحم)
            await showToastFromApiResponse(response, {
                errorMessage: "عملیات قفل/باز کردن کاربر با خطا مواجه شد."
            });
            return;
        }

        // نمایش پیام موفقیت با Toast (غیرمزاحم)
        Toast.success(`کاربر با موفقیت ${isCurrentlyLocked ? 'باز شد' : 'قفل شد'}.`);

        // به‌روزرسانی مستقیم ردیف کاربر از API (بدون refresh کامل)
        // این سریع‌تر است و نیازی به sync Elasticsearch ندارد
        await updateUserRowDirectly(userId, buttonEl);
        
        // Re-enable button (will be re-rendered by updateUserRowDirectly)
    } catch (err) {
        console.error(err);
        buttonEl.disabled = false;
        buttonEl.innerHTML = originalText;
        
        // نمایش خطا با Toast (غیرمزاحم)
        Toast.error("خطای غیرمنتظره در قفل/باز کردن کاربر.");
    }
}

// ---------------------
// Pagination UI
// ---------------------
function renderPagination(data) {
    const pagination = document.getElementById("pagination");
    if (!pagination) return;

    pagination.innerHTML = "";

    if (!data || data.totalPages <= 1) return;

    const createItem = (page, text, { active = false, disabled = false, ellipsis = false } = {}) => {
        const li = document.createElement("li");
        li.className = "page-item";
        if (active) li.classList.add("active");
        if (disabled) li.classList.add("disabled");
        if (ellipsis) li.classList.add("disabled");

        const link = document.createElement("a");
        link.className = "page-link";
        link.href = "#";
        link.textContent = text;

        if (!ellipsis && !disabled) {
            link.addEventListener("click", (event) => {
                event.preventDefault();
                if (page === currentPage) return;
                currentPage = page;
                loadUsers();
            });
        }

        li.appendChild(link);
        return li;
    };

    // دکمه "قبلی"
    pagination.appendChild(createItem(currentPage - 1, "قبلی", { disabled: currentPage === 1 }));

    const totalPages = data.totalPages;
    const current = currentPage;
    const maxVisible = 7; // حداکثر تعداد دکمه‌های قابل مشاهده

    if (totalPages <= maxVisible) {
        // اگر صفحات کم هستند، همه را نمایش بده
        for (let page = 1; page <= totalPages; page += 1) {
            pagination.appendChild(createItem(page, page.toString(), { active: page === current }));
        }
    } else {
        // صفحات زیاد هستند - pagination هوشمند
        // همیشه صفحه اول
        pagination.appendChild(createItem(1, "1", { active: current === 1 }));

        let startPage = Math.max(2, current - 1);
        let endPage = Math.min(totalPages - 1, current + 1);

        // اگر نزدیک به ابتدا هستیم
        if (current <= 3) {
            startPage = 2;
            endPage = Math.min(5, totalPages - 1);
        }
        // اگر نزدیک به انتها هستیم
        else if (current >= totalPages - 2) {
            startPage = Math.max(2, totalPages - 4);
            endPage = totalPages - 1;
        }

        // اگر بین startPage و صفحه اول فاصله هست، "..." بذار
        if (startPage > 2) {
            pagination.appendChild(createItem(null, "...", { ellipsis: true }));
        }

        // صفحات میانی
        for (let page = startPage; page <= endPage; page += 1) {
            pagination.appendChild(createItem(page, page.toString(), { active: page === current }));
        }

        // اگر بین endPage و صفحه آخر فاصله هست، "..." بذار
        if (endPage < totalPages - 1) {
            pagination.appendChild(createItem(null, "...", { ellipsis: true }));
        }

        // همیشه صفحه آخر
        pagination.appendChild(createItem(totalPages, totalPages.toString(), { active: current === totalPages }));
    }

    // دکمه "بعدی"
    pagination.appendChild(createItem(currentPage + 1, "بعدی", { disabled: currentPage === totalPages }));
}

// ---------------------
// Info Texts
// ---------------------
function updateInfo(data) {
    const info = document.getElementById("paginationInfo");
    const count = document.getElementById("usersCount");

    if (!data) {
        if (info) info.textContent = "";
        if (count) count.textContent = "";
        return;
    }

    if (info) info.textContent = `صفحه ${data.page} از ${data.totalPages}`;
    if (count) count.textContent = `تعداد کل کاربران: ${data.totalCount ?? 0}`;
}
