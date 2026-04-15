document.addEventListener("DOMContentLoaded", () => {
    // Modal elements
    const invoiceModal = document.getElementById("invoice-modal");
    const closeInvoiceModalBtn = document.getElementById("close-invoice-modal");
    const invoiceDetailsView = document.getElementById("invoice-details-view");
    const invoiceEditForm = document.getElementById("invoice-edit-form");
    const editInvoiceBtn = document.getElementById("edit-invoice-btn");
    const deleteInvoiceBtn = document.getElementById("delete-invoice-btn");
    const cancelEditBtn = document.getElementById("cancel-edit-btn");

    let currentInvoice = null;

    // Attach modal close event
    if (closeInvoiceModalBtn) {
        closeInvoiceModalBtn.addEventListener("click", closeInvoiceModal);
    }
    // Close modal when clicking outside
    window.addEventListener("click", (e) => {
        if (e.target === invoiceModal) closeInvoiceModal();
    });

    // Cancel edit
    if (cancelEditBtn) {
        cancelEditBtn.addEventListener("click", (e) => {
            e.preventDefault();
            showInvoiceDetailsView();
        });
    }

    // Edit button
    if (editInvoiceBtn) {
        editInvoiceBtn.addEventListener("click", () => {
            showInvoiceEditForm();
        });
    }

    // Delete button
    if (deleteInvoiceBtn) {
        deleteInvoiceBtn.addEventListener("click", async () => {
            if (!currentInvoice) return;
            if (!confirm("Czy na pewno chcesz usunąć tę fakturę?")) return;
            const res = await fetch(`/api/invoices/${currentInvoice.id}`, { method: "DELETE" });
            const result = await res.json();
            if (result.success) {
                closeInvoiceModal();
                loadInvoices();
            } else {
                alert("Błąd: " + result.message);
            }
        });
    }

    // Edit form submit
    if (invoiceEditForm) {
        invoiceEditForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            if (!currentInvoice) return;

            // Gather form data
            const payload = {
                invoiceNumber: document.getElementById("edit-invoice-number").value,
                contractorName: document.getElementById("edit-contractor").value,
                issueDate: document.getElementById("edit-issue-date").value,
                dueDate: document.getElementById("edit-due-date").value,
                totalGross: parseFloat(document.getElementById("edit-total-gross").value),
                type: document.getElementById("edit-type").value,
                status: document.getElementById("edit-status").value,
                notes: document.getElementById("edit-notes").value
            };

            const res = await fetch(`/api/invoices/${currentInvoice.id}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });
            const result = await res.json();
            if (result.success) {
                closeInvoiceModal();
                loadInvoices();
            } else {
                alert("Błąd: " + result.message);
            }
        });
    }

    // Expose showInvoiceModal globally for inline onclick
    window.showInvoiceModal = showInvoiceModal;

    // Helper: Show details view
    function showInvoiceDetailsView() {
        invoiceDetailsView.style.display = "";
        invoiceEditForm.style.display = "none";
    }
    // Helper: Show edit form
    function showInvoiceEditForm() {
        if (!currentInvoice) return;
        // Fill form fields
        document.getElementById("edit-invoice-number").value = currentInvoice.invoiceNumber || "";
        document.getElementById("edit-contractor").value = currentInvoice.contractorName || "";
        document.getElementById("edit-issue-date").value = currentInvoice.issueDate || "";
        document.getElementById("edit-due-date").value = currentInvoice.dueDate || "";
        document.getElementById("edit-total-gross").value = currentInvoice.totalGross || 0;
        document.getElementById("edit-type").value = currentInvoice.type || "Sales";
        document.getElementById("edit-status").value = currentInvoice.status || "Unpaid";
        document.getElementById("edit-notes").value = currentInvoice.notes || "";

        invoiceDetailsView.style.display = "none";
        invoiceEditForm.style.display = "";
    }
    // Helper: Show modal with invoice details
    function showInvoiceModal(id) {
        fetch(`/api/invoices/${id}`)
            .then(res => res.json())
            .then(inv => {
                currentInvoice = inv;
                // Fill details
                document.getElementById("modal-invoice-number").textContent = inv.invoiceNumber || "";
                document.getElementById("modal-contractor").textContent = inv.contractorName || "";
                document.getElementById("modal-issue-date").textContent = inv.issueDate || "";
                document.getElementById("modal-due-date").textContent = inv.dueDate || "";
                document.getElementById("modal-total-gross").textContent = inv.totalGross?.toFixed(2) + " PLN" || "";
                document.getElementById("modal-type").textContent = inv.type === "Sales" ? "Sprzedażowa" : "Kosztowa";
                let statusPl = inv.status;
                if (inv.status === "Paid") statusPl = "Opłacona";
                if (inv.status === "Unpaid") statusPl = "Nieopłacona";
                if (inv.status === "PartiallyPaid") statusPl = "Częściowo opłacona";
                document.getElementById("modal-status").textContent = statusPl;
                document.getElementById("modal-notes").textContent = inv.notes || "";

                showInvoiceDetailsView();
                invoiceModal.style.display = "flex";
            })
            .catch(() => {
                alert("Nie udało się pobrać szczegółów faktury.");
            });
    }
    // Helper: Close modal
    function closeInvoiceModal() {
        invoiceModal.style.display = "none";
        currentInvoice = null;
    }
    async function deleteInvoice(id) {
        if (!confirm("Czy na pewno chcesz usunąć tę fakturę?")) return;

        try {
            const res = await fetch(`/api/invoices/${id}`, { method: "DELETE" });
            const result = await res.json();

            if (result.success) {
                if (typeof loadInvoices === 'function') {
                    await loadInvoices();
                } else if (window.loadInvoices) {
                    await window.loadInvoices();
                }
            } else {
                alert("Błąd: " + result.message);
            }
        } catch (error) {
            console.error("Delete error:", error);
            alert("Wystąpił błąd podczas usuwania faktury.");
        }
    }

    window.deleteInvoice = deleteInvoice;

    // Patch loadInvoices to add details button
    window.loadInvoices = async function () {
        const tbody = document.getElementById("invoices-list");
        try {
            const res = await fetch("/api/invoices");
            const data = await res.json();

            tbody.innerHTML = "";

            if (data.length === 0) {
                tbody.innerHTML =
                    '<tr><td colspan="8" style="text-align:center;">Brak wystawionych faktur.</td></tr>';
                return;
            }

            data.forEach((inv) => {
                const typePl = inv.type === "Sales" ? "Sprzedażowa" : "Kosztowa";
                let statusPl = inv.status;
                if (inv.status === "Paid") statusPl = "Opłacona";
                if (inv.status === "Unpaid") statusPl = "Nieopłacona";
                if (inv.status === "PartiallyPaid") statusPl = "Częściowo opłacona";

                tbody.innerHTML += `
                    <tr>
                        <td><strong>${inv.invoiceNumber}</strong></td>
                        <td>${inv.contractorName}</td>
                        <td>${inv.issueDate}</td>
                        <td>${inv.dueDate}</td>
                        <td>${inv.totalGross.toFixed(2)} PLN</td>
                        <td>${typePl}</td>
                        <td>${statusPl}</td>
                        <td style="text-align: right;">
                            <button onclick="showInvoiceModal(${inv.id})" class="btn-details"><i class="fas fa-eye"></i> Szczegóły</button>
                            <button onclick="deleteInvoice('${inv.id}')" class="btn-delete"><i class="fas fa-trash"></i> Usuń</button>
                        </td>
                    </tr>
                `;
            });
        } catch (error) {
            tbody.innerHTML =
                '<tr><td colspan="8" style="text-align:center; color: red;">Błąd połączenia z serwerem.</td></tr>';
        }
    };

    // Initial load
    loadInvoices();
});