using System.Text.RegularExpressions;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Core
{
    public class ContractorService
    {
        private readonly AppDbContext _db;
        public ContractorService(AppDbContext db) => _db = db;

        public List<DBContractor> GetCompanyContractors(int companyId)
        {
            return _db.Contractors
                .Where(c => c.CompanyId == companyId && !c.IsDeleted)
                .ToList();
        }

        public DBContractor AddContractor(int companyId, string name, string taxId, string street, string city, string zipCode, string email)
        {
            // 1. Walidacja NIP (tylko 10 cyfr)
    string cleanNip = taxId.Replace("-", "").Replace(" ", "").Trim();
    if (!Regex.IsMatch(cleanNip, @"^\d{10}$"))
        throw new Exception("NIP musi składać się z dokładnie 10 cyfr.");

    // 2. Walidacja Email (format nazwa@coś.coś)
    if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        throw new Exception("Podaj poprawny adres e-mail (np. biuro@firma.pl).");

    // 3. Walidacja Kodu Pocztowego (format 00-000)
    if (!string.IsNullOrWhiteSpace(zipCode) && !Regex.IsMatch(zipCode, @"^\d{2}-\d{3}$"))
        throw new Exception("Nieprawidłowy kod pocztowy. Wymagany format: 00-000.");

    // Sugestia co do Miasta: Czyścimy białe znaki i pilnujemy, by nie było puste
    string cleanCity = city?.Trim() ?? "";
    if (string.IsNullOrWhiteSpace(cleanCity))
        throw new Exception("Nazwa miasta nie może być pusta.");
        
            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(email, emailPattern))
                    throw new Exception("Niepoprawny format adresu e-mail.");
            }
            if (string.IsNullOrWhiteSpace(name)) throw new Exception("Nazwa kontrahenta nie może być pusta.");
            if (string.IsNullOrWhiteSpace(taxId)) throw new Exception("NIP nie może być pusty.");

            var contractor = new DBContractor
            {
                CompanyId = companyId,
                Name = name.Trim(),
                TaxId = taxId.Trim(),
                Street = street?.Trim() ?? "",
                City = city?.Trim() ?? "",
                ZipCode = zipCode?.Trim() ?? "",
                Email = email?.Trim() ?? ""
            };

            _db.Contractors.Add(contractor);
            _db.SaveChanges();
            return contractor;
        }

        public string DeleteContractor(int id, int companyId)
        {
            var contractor = _db.Contractors.FirstOrDefault(c => c.Id == id && c.CompanyId == companyId);
            if (contractor == null) return "Nie znaleziono kontrahenta.";

            contractor.IsDeleted = true;
            _db.SaveChanges();
            
            return "Pomyślnie usunięto kontrahenta (zarchiwizowano).";
        }
    }
}