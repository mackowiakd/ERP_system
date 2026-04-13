using System;
using System.Collections.Generic;
using System.Linq;
using ERP_System.Core.DBTables;

namespace ERP_System.Core
{
    public class ContractorService
    {
        private readonly AppDbContext _db;

        public ContractorService(AppDbContext db)
        {
            _db = db;
        }

        public List<DBContractor> GetCompanyContractors(int companyId)
        {
            return _db.Contractors
                      .Where(c => c.CompanyId == companyId)
                      .ToList();
        }

        public string AddContractor(int companyId, string name, string taxId, string Address)
        {
            try
            {
                var newContractor = new DBContractor
                {
                    CompanyId = companyId,
                    Name = name,
                    Address = Address,
                    TaxId = taxId 
                };

                _db.Contractors.Add(newContractor);
                _db.SaveChanges();

                return "Pomyślnie dodano kontrahenta";
            }
            catch (Exception ex)
            {
                return $"Błąd podczas dodawania kontrahenta: {ex.Message}";
            }
        }

        public string DeleteContractor(int contractorId, int companyId)
        {
            
            var contractor = _db.Contractors
                                .FirstOrDefault(c => c.Id == contractorId && c.CompanyId == companyId);

            if (contractor == null)
            {
                return "Nie znaleziono kontrahenta lub brak uprawnień.";
            }

            try
            {
                _db.Contractors.Remove(contractor);
                _db.SaveChanges();
                return "Pomyślnie usunięto kontrahenta";
            }
            catch (Exception ex)
            {
                return $"Błąd podczas usuwania: {ex.Message}";
            }
        }
    }
}