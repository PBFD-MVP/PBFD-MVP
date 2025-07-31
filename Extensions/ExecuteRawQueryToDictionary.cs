using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace VisitorLog_PBFD.Extensions
{
    public static class DbContextExtensions
    {
        /// <summary>
        /// Executes a raw SQL query asynchronously and returns the result as a list of dictionaries.
        /// Each dictionary represents a row with column names as keys.
        /// </summary>
        public static async Task<List<Dictionary<string, object>>> ExecuteRawQueryToDictionaryAsync(this DbContext context, string query, params SqlParameter[] parameters)
        {
            var result = new List<Dictionary<string, object>>();

            // Create command
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;

            // Add parameters
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
            }

            // Ensure connection is open
            if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
            {
                await context.Database.OpenConnectionAsync();
            }

            // Execute the reader asynchronously
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = await Task.FromResult(reader.GetValue(i)); // Ensures async, though GetValue is synchronous
                }
                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Executes a raw SQL query asynchronously and maps the result to a list of objects of type T.
        /// </summary>
        public static async Task<List<T>> ExecuteRawQueryToListAsync<T>(this DbContext context, string query, Dictionary<string, object>? parameters)
        {
            var list = new List<T>();

            // Create command
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;

            // Add parameters
            if(parameters!=null)
            foreach (var parameter in parameters)
            {
                var dbParameter = command.CreateParameter();
                dbParameter.ParameterName = parameter.Key;
                dbParameter.Value = parameter.Value ?? DBNull.Value;
                command.Parameters.Add(dbParameter);
            }

            // Ensure the connection is open
            if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
            {
                await context.Database.OpenConnectionAsync();
            }

            try
            {
                // Execute reader asynchronously
                await using var reader = await command.ExecuteReaderAsync();

                var properties = typeof(T).GetProperties();
                while (await reader.ReadAsync())
                {
                    var obj = Activator.CreateInstance<T>();
                    foreach (var property in properties)
                    {
                        if (!reader.IsDBNull(reader.GetOrdinal(property.Name)))
                        {
                            property.SetValue(obj, reader[property.Name]);
                        }
                    }
                    list.Add(obj);
                }
            }
            finally
            {
                // Ensure the connection is closed
                if (context.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
                {
                    await context.Database.CloseConnectionAsync();
                }
            }

            return list;
        }

        /// <summary>
        /// Executes a raw SQL query and returns the result as a list of dictionaries.
        /// Each dictionary represents a row with column names as keys.
        /// </summary>
        public static List<Dictionary<string, object>> ExecuteRawQueryToDictionary(this DbContext context, string query, params SqlParameter[] parameters)
        {
            var result = new List<Dictionary<string, object>>();

            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;

            // Add parameters
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
            }

            context.Database.OpenConnection();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }

            return result;
        }

        public static List<T> ExecuteRawQueryToList<T>(this DbContext context, string query, Dictionary<string, object> parameters)
        {
            var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;

            foreach (var parameter in parameters)
            {
                var dbParameter = command.CreateParameter();
                dbParameter.ParameterName = parameter.Key;
                dbParameter.Value = parameter.Value ?? DBNull.Value;
                command.Parameters.Add(dbParameter);
            }

            context.Database.OpenConnection();
            try
            {
                using (var result = command.ExecuteReader())
                {
                    var list = new List<T>();
                    var properties = typeof(T).GetProperties();
                    while (result.Read())
                    {
                        var obj = Activator.CreateInstance<T>();
                        foreach (var property in properties)
                        {
                            if (!result.IsDBNull(result.GetOrdinal(property.Name)))
                            {
                                property.SetValue(obj, result[property.Name]);
                            }
                        }
                        list.Add(obj);
                    }
                    return list;
                }
            }
            finally
            {
                context.Database.CloseConnection();
            }
        }

    }

}
