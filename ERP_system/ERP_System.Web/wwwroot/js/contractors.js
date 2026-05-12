      document.addEventListener("DOMContentLoaded", () => {
        const toggleBtn = document.getElementById("sidebar-toggle");
        const sidebar = document.querySelector(".sidebar");
        if (toggleBtn && sidebar) {
          toggleBtn.addEventListener("click", () =>
            sidebar.classList.toggle("open"),
          );
        }

        const zipInput = document.getElementById("zipCode");
        if (zipInput) {
          zipInput.addEventListener("input", (e) => {
            let val = e.target.value.replace(/\D/g, ""); // Numbers only
            if (val.length > 2) {
              val = val.substring(0, 2) + "-" + val.substring(2, 5);
            }
            e.target.value = val;
          });
        }

        if (document.getElementById("contractors-list")) {
          loadContractors();
        }

        const contractorForm = document.getElementById("add-contractor-form");
        if (contractorForm) {
          contractorForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            const name = document.getElementById("name").value.trim();
            const taxId = document.getElementById("taxId").value.trim();
            const emailInput = document.getElementById("email");
            const email = emailInput ? emailInput.value.trim() : "";
            const street = document.getElementById("street").value.trim();
            const zip = document.getElementById("zipCode").value.trim();
            const city = document.getElementById("city").value.trim();

            // NIP validation
            if (!/^\d{10}$/.test(taxId)) {
              alert("Błąd: NIP musi składać się z dokładnie 10 cyfr.");
              return;
            }

            // Email validation
            if (emailInput && !emailInput.checkValidity()) {
              emailInput.reportValidity();
              return;
            }
            if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
              alert("Błąd: Niepoprawny format adresu e-mail.");
              return;
            }

            // postal code validation
            if (zip && !/^\d{2}-\d{3}$/.test(zip)) {
              alert("Błąd: Kod pocztowy musi mieć format 00-000.");
              return;
            }

            const res = await fetch("/api/contractors", {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({
                Name: name,
                TaxId: taxId,
                Email: email,
                Street: street,
                ZipCode: zip,
                City: city,
              }),
            });

            const result = await res.json();

            if (result.success || result.id) {
              contractorForm.reset();
              if (document.getElementById("contractors-list")) {
                loadContractors();
              }
              alert("Kontrahent zapisany!");
            } else {
              alert("Błąd: " + result.message);
            }
          });
        }
      });

      async function loadContractors() {
        const tbody = document.getElementById("contractors-list");
        try {
          const res = await fetch("/api/contractors");
          const data = await res.json();

          tbody.innerHTML = "";

          if (data.length === 0) {
            tbody.innerHTML =
              '<tr><td colspan="5" style="text-align:center;">Brak dodanych kontrahentów.</td></tr>';
            return;
          }

          data.forEach((c) => {
            tbody.innerHTML += `
            <tr>
                <td>${c.id}</td>
                <td>${c.name}</td>
                <td>${c.taxId}</td>
                <td>${c.street || ""} ${c.city || ""}</td>
                <td style="text-align: right;">
                    <button onclick="deleteContractor(${c.id})" class="btn-delete"><i class="fas fa-trash"></i> Usuń</button>
                </td>
            </tr>
        `;
          });
        } catch (error) {
          tbody.innerHTML =
            '<tr><td colspan="5" style="text-align:center; color: red;">Błąd połączenia z serwerem.</td></tr>';
        }
      }

      async function deleteContractor(id) {
        if (!confirm("Czy na pewno chcesz usunąć tego kontrahenta?")) return;

        const res = await fetch(`/api/contractors/${id}`, { method: "DELETE" });
        const result = await res.json();

        if (result.success) {
          loadContractors();
        } else {
          alert("Błąd: " + result.message);
        }
      }