using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ERP_System.Core
{
    public class CategoryService
    {
        private readonly AppDbContext db;

        public CategoryService(AppDbContext db)
        {
            this.db = db;
        }

        private bool companyHasCategory(int? companyId, string name)
        {
            return db.Categories.Any(c => (c.CompanyId == companyId || c.CompanyId == null) && c.Name.ToLower() == name.ToLower());
        }

        public string addCategory(int? companyId, string name, string? description)
        {
            if (companyHasCategory(companyId, name))
            {
                return "Posiadasz już kategorię o tej samej nazwie";
            }

            try
            {
                var newCategory = new DBTransactionCategories { Name = name, Description = description, CompanyId = companyId };
                db.Categories.Add(newCategory);
                db.SaveChanges();
                return "Poprawnie dodano kategorię";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public string deleteCategory(int? companyId, int categoryId)
        {
            // POPRAWKA BŁĘDU: Dodano sprawdzanie c.Id == categoryId
            var category = db.Categories.FirstOrDefault(c => c.CompanyId == companyId && c.Id == categoryId);

            if (category == null)
            {
                return "Błąd: nie można usunąć kategorii. Nie znaleziono w bazie";
            }

            try
            {
                db.Categories.Remove(category);
                db.SaveChanges();
                return "Pomyślnie usunięto kategorię";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public List<DBTransactionCategories> listAllCompanyCategories(int? companyId)
        {
            return db.Categories.Where(c => c.CompanyId == companyId || c.CompanyId == null).OrderByDescending(c => c.CompanyId).ToList();
        }

        public string modifyCategory(int? companyId, int categoryId, string newName, string newDescription)
        {
            var category = db.Categories.FirstOrDefault(c => c.CompanyId == companyId && c.Id == categoryId);

            if (category == null)
            {
                return "Błąd: firma nie posiada takiej kategorii";
            }

            category.Name = newName;
            category.Description = newDescription;
            db.SaveChanges();

            return "Pomyślnie zedytowano kategorię";
        }

        public string addDefaultCategories()
        {
            var zakupy = new DBTransactionCategories { CompanyId = null, Name = "Zakupy spożywcze", Description = "Opłaty za codzienne zakupy domowe" };
            var rachunki = new DBTransactionCategories { CompanyId = null, Name = "Rachunki", Description = "Opłaty za wodę, gaz, prąd itp." };
            var transport = new DBTransactionCategories { CompanyId = null, Name = "Transport", Description = "Opłaty za komunikację miejską lub paliwo" };
            var finanse = new DBTransactionCategories { CompanyId = null, Name = "Finanse", Description = "Kategoria dla finansów" };
            var rozrywka = new DBTransactionCategories { CompanyId = null, Name = "Rozrywka", Description = "Kategoria dla rozrywki" };
            var inne = new DBTransactionCategories { CompanyId = null, Name = "Inne", Description = "Kategoria dla innych wydatków" };

            List<DBTransactionCategories> categories = new List<DBTransactionCategories> { zakupy, rachunki, transport, finanse, rozrywka, inne };

            try
            {
                foreach (DBTransactionCategories category in categories)
                {
                    if (!db.Categories.Any(c => c.CompanyId == null && c.Name == category.Name))
                    {
                        db.Categories.Add(category);
                        Console.WriteLine("Dodano domyślną kategorię: " + category.Name);
                    }
                }

                db.SaveChanges();
                return "Pomyślnie dodano domyślne transakcje";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ex.ToString();
            }
        }
    }
}