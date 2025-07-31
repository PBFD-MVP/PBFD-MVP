using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VisitorLog_PBFD.Data;

namespace VisitorLog_PBFD.Services
{
    public class LocationResetService:ILocationResetService
    {
        private readonly ApplicationDbContext _context;

        public LocationResetService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task ResetTableColumnsAsync(string parentTable, List<(int ChildId, int LocationId)> currSelectedContinents, int personId)
        {
            List<string> prevSelectedContinents = await GetColumnsNotNullAsync(parentTable);
            List<string> currSelectedContinuents = await GetSelectedLocation(currSelectedContinents);

            var filteredContinents = prevSelectedContinents
                                    .Where(continent => !currSelectedContinuents.Contains(continent))
                                    .ToList();

            if (filteredContinents.Any())
                await ResetTableFilteredColumnsAsync(personId, parentTable, filteredContinents);
        }
        private async Task<List<string>> GetSelectedLocation(List<(int ContinentID, int LocationID)> currSelectedContinents)
        {
            var locationIds = currSelectedContinents.Select(data => data.LocationID).ToList();

            var locationNames = await _context.Locations
                .Where(location => locationIds.Contains(location.Id))
                .Select(location => location.Name)
                .ToListAsync();

            return locationNames;
        }
        private async Task<List<string>> ExecuteSqlReaderAsync(string query, SqlParameter[]? parameters = null)
        {
            var results = new List<string>();

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }
                await _context.Database.OpenConnectionAsync();

                try
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(reader.GetString(0));
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }

            return results;
        }

        private async Task<List<string>> GetColumnsNotNullAsync(string tableName)
        {
            // Fetch column names dynamically
            string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME NOT IN ('personId', 'isDeleted')";
            var columnParam = new SqlParameter("@tableName", tableName);
            var columnNames = await ExecuteSqlReaderAsync(query, new[] { columnParam });

            var columns = new List<string>();

            // Check for columns with not null value
            foreach (var columnName in columnNames)
            {
                string checkQuery = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{columnName}] is not null";
                var countParam = new SqlParameter("@columnName", columnName);
                var result = await ExecuteScalarQueryAsync(checkQuery);

                if (result > 0)
                {
                    columns.Add(columnName);
                }
            }

            return columns;
        }

        private async Task<int> ExecuteScalarQueryAsync(string query, SqlParameter[]? parameters = null)
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }
                await _context.Database.OpenConnectionAsync();

                try
                {
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
                catch (Exception ex)
                {
                    return -1; // Or any appropriate default value
                }
                finally
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }
        }
        private async Task ResetTableFilteredColumnsAsync(int personId, string tableName, List<string>? knownColumns = null)
        {
            List<string> columnsToUpdate = new List<string>();

            if (knownColumns == null || knownColumns.Count == 0)
            {
                // Fetch column names dynamically
                string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME NOT IN ('personId', 'isDeleted')";
                var columnNames = new List<string>();

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.Add(new SqlParameter("@tableName", tableName));
                    await _context.Database.OpenConnectionAsync();

                    try
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                await _context.Database.CloseConnectionAsync();
                                return; // Exit if table does not exist
                            }

                            while (await reader.ReadAsync())
                            {
                                columnNames.Add(reader.GetString(0));
                            }
                        }
                    }
                    catch (SqlException)
                    {
                        await _context.Database.CloseConnectionAsync();
                        return; // Exit if table does not exist
                    }
                    finally
                    {
                        await _context.Database.CloseConnectionAsync();
                    }
                }

                columnsToUpdate = columnNames;
            }
            else
            {
                columnsToUpdate = knownColumns;
            }

            // Build the update statement
            string updateFields = "";
            foreach (var column in columnsToUpdate)
            {
                if (!string.IsNullOrEmpty(updateFields))
                {
                    updateFields += ", ";
                }
                updateFields += $"[{column}] = DEFAULT";
            }

            // Update the table
            if (!string.IsNullOrEmpty(updateFields))
            {
                string updateQuery = $"UPDATE [{tableName}] SET {updateFields} WHERE personId = {personId}";
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(updateQuery);
                }
                catch (SqlException ex)
                {
                    return;
                }
            }

            // Recursively call for the next table (using each column name as the next table name)
            foreach (var column in columnsToUpdate)
            {
                await ResetTableFilteredColumnsAsync(personId, column, null);
            }
        }
    }
}
