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

    // تغییر pageSize
    pageSizeSelect?.addEventListener("change", () => {
        pageSize = Number(pageSizeSelect.value) || 20;
        currentPage = 1;
        loadUsers();
    });

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
        td.colSpan = 8;
        td.className = "text-center text-muted py-4";
        td.textContent = "هیچ کاربری یافت نشد";
        tr.appendChild(td);
        tbody.appendChild(tr);
        return;
    }

    for (const user of data.items) {
        const tr = document.createElement("tr");

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
                 <a href="#" data-user-id="${user.id}" class="btn btn-info btn-xs edit" data-action="details">
                    <i class="fa fa-edit"></i> جزئیات
                 </a>
                 <a href="#" data-user-id="${user.id}" class="btn btn-danger btn-xs delete" data-action="toggle-lock">
                    <i class="fa fa-lock"></i> ${user.isLocked ? "آنلاک" : "لاک"}
                 </a>
            </td>
        `;

        tbody.appendChild(tr);
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

    const createItem = (page, text, { active = false, disabled = false } = {}) => {
        const li = document.createElement("li");
        li.className = "page-item";
        if (active) li.classList.add("active");
        if (disabled) li.classList.add("disabled");

        const link = document.createElement("a");
        link.className = "page-link";
        link.href = "#";
        link.textContent = text;

        link.addEventListener("click", (event) => {
            event.preventDefault();
            if (disabled || page === currentPage) return;
            currentPage = page;
            loadUsers();
        });

        li.appendChild(link);
        return li;
    };

    pagination.appendChild(createItem(currentPage - 1, "قبلی", { disabled: currentPage === 1 }));

    for (let page = 1; page <= data.totalPages; page += 1) {
        pagination.appendChild(createItem(page, page.toString(), { active: page === currentPage }));
    }

    pagination.appendChild(createItem(currentPage + 1, "بعدی", { disabled: currentPage === data.totalPages }));
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
