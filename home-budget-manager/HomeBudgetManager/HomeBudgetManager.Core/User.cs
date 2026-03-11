using HomeBudgetManager.Core.DBTables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeBudgetManager.Core
{
    internal class User
    {
        private int id;
        private String username;
        private String email;
        private String password;
        private List<DBFinancialOperations> transactions;
        private List<DBTransactionCategories> categories;
        private int? houseId;

        public User()
        {
            this.id = 0;
            this.username = "guest";
            this.transactions = new List<DBFinancialOperations>();
            this.categories = new List<DBTransactionCategories>();
        }

        public User(String username)
        {
            // Add id generator
            this.id = 0;
            this.username = username;
            this.transactions = new List<DBFinancialOperations>();
            // Add default categories
            this.categories = new List<DBTransactionCategories>();
        }

        public User(String username, List<DBFinancialOperations> transactions, List<DBTransactionCategories> categories)
        {
            this.id = 0;
            this.username = username;
            this.transactions = transactions;
            this.categories = categories;
        }

        public User(int id, string username, List<DBFinancialOperations> transactions, List<DBTransactionCategories> categories)
        {
            this.id = id;
            this.username = username;
            this.transactions = transactions;
            this.categories = categories;
        }
    }
}
