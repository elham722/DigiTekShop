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
        const fullName = user.fullName || "بدون نام";
        const phone = user.phone || "—";
        const email = user.email || "—";
        const roles = Array.isArray(user.roles) && user.roles.length
            ? user.roles.join("، ")
            : "—";
        const createdAt = formatDate(user.createdAtUtc);
        const lastLogin = user.lastLoginAtUtc ? formatDate(user.lastLoginAtUtc) : "—";
        const status = user.isLocked ? "قفل شده" : "فعال";

        tr.innerHTML = `
            <td>${fullName}</td>
            <td>${phone}</td>
            <td>${email}</td>
            <td>${roles}</td>
            <td>${createdAt}</td>
            <td>${lastLogin}</td>
            <td>${status}</td>
            <td class="text-center">
                <button class="btn btn-sm btn-outline-primary" data-user-id="${user.id}" data-action="details">
                    جزئیات
                </button>
                <button class="btn btn-sm btn-outline-warning" data-user-id="${user.id}" data-action="toggle-lock">
                    ${user.isLocked ? "آنلاک" : "لاک"}
                </button>
            </td>
        `;

        tbody.appendChild(tr);
    }
}

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

function formatDate(value) {
    if (!value) return "—";
    const date = new Date(value);
    return date.toLocaleString("fa-IR");
}

