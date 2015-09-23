using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace DapperContextExample
{
    public class SqlStack
    {
        public string Sql { get; set; }

        public object Parameters { get; set; }
    }
    public class DapperContext
    {
        private readonly List<SqlStack> _sqlStack = new List<SqlStack>();
    
        public void Query(string sql, object model)
        {
            _sqlStack.Add(new SqlStack { Sql = sql, Parameters = model});
        }

        public async Task Save(IDbConnection connection)
        {
            var transaction = connection.BeginTransaction();

            try
            {
                var stack = MergeStack();
                await
                    connection.ExecuteAsync(stack.Sql, stack.Parameters, transaction, null, CommandType.StoredProcedure);
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                transaction.Commit();
            }

        }

        private SqlStack MergeStack()
        {
            var stringBuilder = new StringBuilder();
            dynamic parameters = new ExpandoObject();

            foreach (var sqlStack in _sqlStack)
            {
                stringBuilder.AppendFormat("{0};", sqlStack.Sql);
                parameters = Merge(parameters, sqlStack.Parameters);
            }

            return new SqlStack {Parameters = parameters, Sql = stringBuilder.ToString()};
        }

        private static dynamic Merge(dynamic item1, object item2)
        {
            if (item1 == null || item2 == null)
                return item1 ?? item2 ?? new ExpandoObject();

            var result = (IDictionary<string, object>)item1;
                        
            foreach (var fi in item2.GetType().GetProperties())
            {
                result[fi.Name] = CheckForExistingProperty(item2, fi, result);
            }
            return result;
        }

        private static object CheckForExistingProperty(object item2, PropertyInfo fi, IDictionary<string, object> result)
        {
            var newValue = fi.GetValue(item2, null);
            if (result.All(x => x.Key != fi.Name))
            {
                return newValue;
            }

            var foundProperty = result.Single(x => x.Key == fi.Name);

            if (!foundProperty.Value.Equals(newValue))
            {
                throw new Exception("Merging Property with different values - " + fi.Name);
            }
            return newValue;
        }
    }
}