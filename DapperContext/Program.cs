using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperContextExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Save();
        }

        static async void Save()
        {
            var connection = new SqlConnection();
            var context = new DapperContext();

            context.Query("Insert into tblClient values (@clientId,2,3)", new { clientId = 123 });

            context.Query("Insert into tblClient values (@clientId,4,2)", new { clientId = 1223 });

            await context.Save(connection);
        }
    }
}
