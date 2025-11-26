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

    // فعلاً ساده
    return normalized;

    // اگر خواستی بعداً خوشگل‌ترش کنی:
    // return normalized.replace(/^(\d{4})(\d{3})(\d{4})$/, '$1 $2 $3');
}

// ---------------------
// Helpers: Badges & Date
// ---------------------
function renderStatusBadge(isLocked) {
    if (isLocked) {
        return `<span class="badge badge-danger">قفل شده</span>`;
    }
    return `<span class="badge badge-success">فعال</span>`;
}

function renderPhoneConfirmBadge(isConfirmed) {
    if (isConfirmed) {
        return `<span class="badge badge-success ms-1">تأیید شده</span>`;
    }
    return `<span class="badge badge-warning ms-1">تأیید نشده</span>`;
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

document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("userFilterForm");
    const pageSizeSelect = document.getElementById("pageSize");

    form?.addEventListener("submit", (event) => {
        event.preventDefault();
        currentPage = 1;
        loadUsers();
    });

    pageSizeSelect?.addEventListener("change", () => {
        pageSize = Number(pageSizeSelect.value) || 20;
        currentPage = 1;
        loadUsers();
    });

    loadUsers();
});

// ---------------------
// Load Users from API
// ---------------------
async function loadUsers() {
    const searchValue = document.getElementById("search")?.value.trim();
    const statusValue = document.getElementById("status")?.value;

    const params = new URLSearchParams({
        page: currentPage.toString(),
        pageSize: pageSize.toString()
    });

    if (searchValue) params.set("search", searchValue);
    if (statusValue) params.set("status", statusValue);

    try {
        const response = await fetch(`${API_URL}?${params.toString()}`, {
            method: "GET",
            credentials: "same-origin",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        if (!response.ok) {
            console.error("Failed to load users", response.status);
            renderTable();
            renderPagination();
            updateInfo();
            return;
        }

        const payload = await response.json();
        const data = payload?.data ?? payload;

        renderTable(data);
        renderPagination(data);
        updateInfo(data);
    } catch (error) {
        console.error("Error loading users", error);
        renderTable();
        renderPagination();
        updateInfo();
    }
}

// ---------------------
// Render Table (کاملاً هماهنگ با thead)
// ---------------------
function renderTable(data) {
    const tbody = document.querySelector("#usersTable tbody");
    if (!tbody) return;

    tbody.innerHTML = "";

    // حالت بدون داده
    if (!data || !Array.isArray(data.items) || data.items.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 7; // ✅ چون 7 ستون داریم
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

        // ✅ دقیقاً 7 ستون مطابق thead
        tr.innerHTML = `
            <td>
                <div class="d-flex flex-column">
                    <span class="fw-bold">${displayName}</span>
                    <small class="text-muted">
                        موبایل:
                        <span class="fa-num">${phoneFormatted}</span>
                        ${hasName
                ? ""
                : `<span class="badge badge-warning ms-1">نام ثبت نشده</span>`
            }
                    </small>
                </div>
            </td>
            <td>${email}</td>
            <td>${rolesHtml}</td>
            <td>
                ${statusHtml}
                ${phoneConfirmHtml}
            </td>
            <td><span class="fa-num">${createdAt}</span></td>
            <td><span class="fa-num">${lastLogin}</span></td>
            <td class="text-center">
                <div class="btn-group btn-group-sm" role="group">
                    <button class="btn btn-outline-primary" data-user-id="${user.id}" data-action="details">
                        جزئیات
                    </button>
                    <button class="btn btn-outline-warning" data-user-id="${user.id}" data-action="toggle-lock">
                        ${user.isLocked ? "آنلاک" : "لاک"}
                    </button>
                </div>
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
