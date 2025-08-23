using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using VisitorLog_PBFD.Data;
using VisitorLog_PBFD.ViewModels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace VisitorLog_PBFD.Services
{
    public class LocationReportService : ILocationReportService
    {
        private readonly ApplicationDbContext _context;
        private int _personId;

        // RESEARCH IMPLEMENTATION: In-memory caches for performance optimization.
        // In a production environment, these would be replaced by a distributed cache
        // (e.g., Redis) for scalability and consistency across multiple application instances.
        private Dictionary<string, List<string>> _tableColumnCache = new();
        private Dictionary<string, List<NodeViewModel>> _hierarchyPathChildrenCache = new();
        private Dictionary<string, NodeViewModel> _hierarchyPathSingleNodeCache = new();
        private Dictionary<string, Dictionary<string, object>> _tableColumnBitmapCache = new();

        // Constructor: Initializes the processor with database context and person ID
        public LocationReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Entry point: Generates all paths asynchronously
        public async Task<HashSet<string>> GetPathsAsync(int personId)
        {
            _personId = personId;
            await InitializeCachesAsync(); // Prepare caches for efficient processing
            var paths = new HashSet<string>();
            var processingQueue = new Queue<ProcessingItem>();

            // Initialize the queue with root items
            processingQueue.Enqueue(new ProcessingItem("ContinentGrandparent", ""));

            // Process items iteratively
            while (processingQueue.Count > 0)
            {
                var currentTable = processingQueue.Dequeue();
                ProcessItemAsync(currentTable, processingQueue, paths);
            }

            return paths;
        }

        // Initializes caches for columns, children, and column values
        private async Task InitializeCachesAsync()
        {
            await using var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            await CacheColumnMetadataAsync((SqlConnection)connection);
            await CacheNodeHierachyPath((SqlConnection)connection); // Hierarchy implementation
            await CacheColumnValuesAsync((SqlConnection)connection);
        }

        // Caches metadata for columns present in tables with a 'PersonId' column
        private async Task CacheColumnMetadataAsync(SqlConnection connection)
        {
            var query = @"
                SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME IN (
                    SELECT DISTINCT TABLE_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE COLUMN_NAME = 'PersonId'
                ) AND COLUMN_NAME NOT IN ('PersonId', 'IsDeleted')";

            await using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var table = reader.GetString(0);
                var column = reader.GetString(1);

                // Cache column names for each table
                if (!_tableColumnCache.ContainsKey(table))
                    _tableColumnCache[table] = new List<string>();

                _tableColumnCache[table].Add(column);
            }
        }

        // Caches children nodes using a recursive CTE to build a hierarchy
        private async Task CacheNodeHierachyPath(SqlConnection connection)
        {
            var query = @"
                WITH RecursiveCTE AS (
                    SELECT 
                        Id, 
                        Name, 
                        ParentId,
                        ChildId,
                        CAST(Name AS NVARCHAR(MAX)) AS HierarchyPath,
                        CAST(NULL AS NVARCHAR(MAX)) AS ParentName,
                        Level
                    FROM Locations
                    WHERE ParentId IS NULL

                    UNION ALL

                    SELECT 
                        l.Id, 
                        l.Name, 
                        l.ParentId,
                        l.ChildId,
                        CAST(r.HierarchyPath + ' > ' + l.Name AS NVARCHAR(MAX)),
                        r.Name AS ParentName,
                        l.Level
                    FROM Locations l
                    INNER JOIN RecursiveCTE r ON l.ParentId = r.Id
                )
                SELECT 
                    --COALESCE(Name, 'ContinentGrandparent') AS Key,
                    Id,
                    ChildId,
                    Name,
                    ParentName,
                    HierarchyPath,
                    Level
                FROM RecursiveCTE
                ORDER BY Level, HierarchyPath";

            await using var command = new SqlCommand(query, connection);
            await BuildHierarchyPathSingleNodeCache(command);

            //if level is greater than 5, then build a cache to holder the leaf nodes.
            await BuildHierarchyPathChildrenCache(command);

        }

        private async Task BuildHierarchyPathSingleNodeCache(SqlCommand command)
        {
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var key = reader.GetString(2) ?? "ContinentParent";
                var node = new NodeViewModel(
                    Id: reader.GetInt32(0),
                    ChildId: reader.GetInt32(1),
                    Name: reader.GetString(2),
                    ParentName: reader.IsDBNull(3) ? "ContinentGrandparent" : reader.GetString(3),
                    HierarchyPath: reader.GetString(4),
                    Level: reader.GetInt32(5)
                );

                // Cache node by their name
                if (!_hierarchyPathSingleNodeCache.ContainsKey(key))
                    _hierarchyPathSingleNodeCache[key] = node;
            }
        }

        private async Task BuildHierarchyPathChildrenCache(SqlCommand command)
        {
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                //only need to consider the last two levels.
                if (reader.GetInt32(5) < 6)
                    continue;

                var key = reader.GetString(3) ?? "ContinentGrandparent";
                var child = new NodeViewModel(
                    Id: reader.GetInt32(0),
                    ChildId: reader.GetInt32(1),
                    Name: reader.GetString(2),
                    ParentName: reader.IsDBNull(3) ? "ContinentGrandparent" : reader.GetString(3),
                    HierarchyPath: reader.GetString(4),
                    Level: reader.GetInt32(5)
                );

                // Cache children by their parent name
                if (!_hierarchyPathChildrenCache.ContainsKey(key))
                    _hierarchyPathChildrenCache[key] = new List<NodeViewModel>();

                _hierarchyPathChildrenCache[key].Add(child);
                
            }
        }

        // Caches column values for each table
        private async Task CacheColumnValuesAsync(SqlConnection connection)
        {
            foreach (var table in _tableColumnCache.Keys)
            {
                var columns = _tableColumnCache[table];
                var query = $@"
                    SELECT {string.Join(", ", columns.Select(c => $"[{c}]"))}
                    FROM [{table}]
                    WHERE PersonId = {_personId}";

                await using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    _tableColumnBitmapCache[table] = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        _tableColumnBitmapCache[table][columnName] = reader.GetValue(i);
                    }
                }
            }
        }

        // Processes items from the queue, updating paths as needed
        private void ProcessItemAsync(ProcessingItem item, Queue<ProcessingItem> queue, HashSet<string> paths)
        {
            if (!_tableColumnCache.TryGetValue(item.TableName, out var columns))
                return;

            foreach (var column in columns)
            {
                if (!_tableColumnBitmapCache.TryGetValue(item.TableName, out var columnValues) ||
                    !columnValues.TryGetValue(column, out var bitmap))
                {
                    continue;
                }

                //if the column contains 0, it means that no child is selected.  The path terminates here.
                if(NodeSelectedButNotChildrenSelected(bitmap))
                    paths.Add(_hierarchyPathSingleNodeCache[column].HierarchyPath);

                //if the column contains a value, it means that child is selected. Continue to process the table having the same name as this column.
                if (_hierarchyPathSingleNodeCache[column].Level < 6)
                {
                    queue.Enqueue(new ProcessingItem(column, _hierarchyPathSingleNodeCache[column].HierarchyPath));
                    continue;
                }
                

                // Determine children that match the current column's value.  This is for level 6 which is for counties only.  The grandparent is in the state level.
                var children = GetLeafNodes(column, bitmap);

                foreach (var child in children)
                {
                    paths.Add(child.HierarchyPath);
                }
            }
        }

        private bool NodeSelectedButNotChildrenSelected(object? bitmap)
        {
            if (bitmap is int intBitmap)
            {
                // Check if the integer is 0
                return intBitmap == 0;
            }
            else if (bitmap is long longBitmap)
            {
                // Check if the long is 0
                return longBitmap == 0L;
            }
            else if (bitmap is string stringBitmap)
            {
                // Check if the string is empty or equals "0"
                return stringBitmap=="" || stringBitmap == "0";
            }

            // Return true if bitmap is null or an unsupported type
            return false;
        }

        // Finds children nodes based on a given column value
        private IEnumerable<NodeViewModel> GetLeafNodes(
            string column, object bitmap)
        {
            if (!_hierarchyPathChildrenCache.TryGetValue(column, out var allChildren))
                yield break;

            foreach (var child in allChildren)
            {
                bool isMatch = bitmap switch
                {
                    string strValue when BigInteger.TryParse(strValue, out var bigInt)
                        => (bigInt & (BigInteger.One << child.ChildId)) != 0,
                    int intValue
                        => (intValue & (1 << child.ChildId)) != 0,
                    long longValue
                        => (longValue & (1L << child.ChildId)) != 0,
                    _ => false
                };

                if (isMatch)
                    yield return child;
            }
        }
        // ViewModel representing a location child
        private record NodeViewModel(
            int Id,
            int ChildId,
            string Name,
            string ParentName,
            string HierarchyPath,
            int Level
        );

        // Record for tracking items during processing
        private record ProcessingItem(string TableName, string CurrentPath);
    }
}
