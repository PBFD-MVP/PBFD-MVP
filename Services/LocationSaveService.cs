using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using VisitorLog_PBFD.Data;
using VisitorLog_PBFD.Extensions;
using VisitorLog_PBFD.ViewModels;

namespace VisitorLog_PBFD.Services
{
    public class LocationSaveService : ILocationSaveService
    {
        private readonly ApplicationDbContext _context;

        public LocationSaveService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<LocationViewModel>> GetLocationViewModelsAsync(int personId, string selectedLocationIds)
        {
            // The grandparent table has parent entries as columns, while the parent column contains the child selection bitmap.            
            // Step 1: Get child, parent, and grandparent's relationships
            var childParentGrandparent = await GetChildParentGrandparent(selectedLocationIds);

            if (!childParentGrandparent.Any())
                return new List<LocationViewModel>();

            // Step 2: Get grandparent table's all columns (parent nodes) and each column's bitmap value
            var grandparentTableColumns = await GetGrandparentTableColumns(personId, childParentGrandparent);

            // Step 3: Keep the parent columns which have valid bitmap value
            var parentswithValidBitmap = GetSelectedParentsAndBitmap(childParentGrandparent, grandparentTableColumns);

            // Step 4: Build the view models using the valid parents and children 
            return BuildLocationViewModels(childParentGrandparent, parentswithValidBitmap);
        }

        // Step 1: Get parent, grandparent, and child table names
        private async Task<List<LocationMappingViewModel>> GetChildParentGrandparent(string selectedLocationIds)
        {
            if (selectedLocationIds == null || selectedLocationIds == string.Empty)
                return await GetChildParentGrandparentWithOutParameter();

            return await GetChildParentGrandparentWithParameter(selectedLocationIds);
        }
        private async Task<List<LocationMappingViewModel>> GetChildParentGrandparentWithOutParameter()
        {
            var query = @"
                SELECT child.Id AS ChildLocationId, child.ChildId AS ChildId, child.NameTypeId, 
                       parent.Id AS ParentId, child.Name AS ChildNode, 
                       parent.Name AS ParentNode, grandparent.Name AS GrandparentNode
                FROM Locations parent
                INNER JOIN Locations grandparent ON parent.ParentId = grandparent.Id
                LEFT OUTER JOIN Locations child ON parent.ID = child.ParentId
                WHERE parent.Id = 1";

            return await _context.ExecuteRawQueryToListAsync<LocationMappingViewModel>(query, null);
        }

        private async Task<List<LocationMappingViewModel>> GetChildParentGrandparentWithParameter(string selectedLocationIds)
        {
            var query = @"
                SELECT child.Id AS ChildLocationId, child.ChildId AS ChildId, child.NameTypeId, 
                       parent.Id AS ParentId, child.Name AS ChildNode, 
                       parent.Name AS ParentNode, grandparent.Name AS GrandparentNode
                FROM Locations parent
                INNER JOIN Locations grandparent ON parent.ParentId = grandparent.Id
                LEFT OUTER JOIN Locations child ON parent.ID = child.ParentId
                WHERE parent.Id IN (SELECT value FROM STRING_SPLIT(@selectedLocationIds, ','))";

            var parameters = new Dictionary<string, object>
            {
                { "@selectedLocationIds", selectedLocationIds }
            };

            return await _context.ExecuteRawQueryToListAsync<LocationMappingViewModel>(query, parameters);
        }

        // Step 2: Fetch grandparent table columns (parent tables) and values (children's bitmap)
        private async Task<Dictionary<string, List<(string ColumnName, object ColumnValue)>>> GetGrandparentTableColumns(int personId,
            List<LocationMappingViewModel> childParentGrandparent)
        {
            var grandparentTableColumns = new Dictionary<string, List<(string ColumnName, object ColumnValue)>>();

            var distinctGrandparentNodes = childParentGrandparent
                .Select(x => x.GrandparentNode)
                .Distinct();

            foreach (var grandparentNode in distinctGrandparentNodes)
            {
                var query = $@"IF OBJECT_ID('{grandparentNode}', 'U') IS NOT NULL BEGIN SELECT * FROM [{grandparentNode}] where PersonId={personId} END";
                var results = await _context.ExecuteRawQueryToDictionaryAsync(query);

                grandparentTableColumns[grandparentNode] = results.SelectMany(row =>
                    row.Where(kv => kv.Key != "PersonId" && kv.Key != "IsDeleted")
                       .Select(kv => (ColumnName: kv.Key, ColumnValue: kv.Value)))
                    .ToList();
            }

            return grandparentTableColumns;
        }

        // Step 3: Map grandparent (key), parent (column name), and child (column value)
        private Dictionary<string, object> GetSelectedParentsAndBitmap(
            List<LocationMappingViewModel> childParentGrandparent,
            Dictionary<string, List<(string ColumnName, object ColumnValue)>> grandparentTableColumns)
        {
            var parentNodes = childParentGrandparent
                .Select(x => x.ParentNode)
                .Distinct();

            return grandparentTableColumns
                .SelectMany(kv => kv.Value
                    .Where(column => parentNodes.Contains(column.ColumnName))
                    .Select(column => new { TableName = column.ColumnName, column.ColumnValue }))
                .ToDictionary(x => x.TableName, x => x.ColumnValue);
        }

        // Step 4: Construct the view model
        private List<LocationViewModel> BuildLocationViewModels(
            List<LocationMappingViewModel> childParentGrandparent,
            Dictionary<string, object> mappedTableValues)
        {
            return (
                from c in childParentGrandparent
                join nt in _context.NameTypes
                    on c.NameTypeId equals nt.NameTypeId into nameTypeGroup
                from nt in nameTypeGroup.DefaultIfEmpty()
                select new LocationViewModel
                {
                    ChildId = c.ChildId,
                    ChildNode = c.ChildNode,
                    ParentNode = c.ParentNode,
                    ChildLocationId = c.ChildLocationId,
                    ParentId = c.ParentId,
                    NameTypeName = nt?.Name ?? "Unknown NameType",
                    IsSelected = mappedTableValues.TryGetValue(c.ParentNode, out var value) &&
                     TryGetBitmask(value, out BigInteger bitmask) &&
                     (bitmask & (BigInteger.One << c.ChildId)) != 0
                }).ToList();
        }

        public bool TryGetBitmask(object value, out BigInteger bitmask)
        {
            bitmask = BigInteger.Zero; // Default to zero if conversion fails

            if (value is int intValue)
            {
                bitmask = new BigInteger(intValue);
                return true;
            }
            if (value is long longValue)
            {
                bitmask = new BigInteger(longValue);
                return true;
            }
            if (value is string stringValue)
            {
                return BigInteger.TryParse(stringValue, out bitmask);
            }

            return false; // Unsupported type
        }

        public string SaveLocationAsync(int personId, Dictionary<int, int[]> selectedLocations)
        {
            // Dictionary to store bitmasked values
            var bitmaskedDictionary = new Dictionary<int, BigInteger>();

            // Get bitmaskedDictionary
            GetBitmaskedDictionary(bitmaskedDictionary, selectedLocations);

            // Concatenate all parentIds and all childIds into strings
            string concatenatedParentIds = string.Join(",", bitmaskedDictionary.Keys);

            UpdateParentBitmapInGrandparentTable(personId, bitmaskedDictionary, concatenatedParentIds);

            // Concatenate all childIds from all parentId entries
            string concatenatedChildIds = string.Join(",", selectedLocations.SelectMany(entry => entry.Value));

            return concatenatedChildIds;

        }

        private void GetBitmaskedDictionary(Dictionary<int, BigInteger> bitmaskedDictionary, Dictionary<int, int[]> selectedLocations)
        {
            // Iterate through selectedLocations to process each entry
            foreach (var entry in selectedLocations)
            {
                int parentId = entry.Key; // Current location ID
                int[] childIds = entry.Value; // Selected child IDs

                // Calculate bitmask for childIds
                BigInteger bitmask = childIds.Aggregate(
                    BigInteger.Zero,
                    (acc, childId) => acc | (BigInteger.One << childId)
                );
                // Store the bitmask in the dictionary with parentId as the key
                bitmaskedDictionary[parentId] = bitmask;
            }
        }

        private void UpdateParentBitmapInGrandparentTable(int personId, Dictionary<int, BigInteger> bitmaskedDictionary, string selectedLocationIds)
        {
            // Step 1: Retrieve current and parent table names
            var parentAndGrandparentNodes = GetParentAndGrandparentNodes(selectedLocationIds);

            // Step 2: Iterate through bitmaskedDictionary to update parent tables
            foreach (var entry in bitmaskedDictionary)
            {
                int parentId = entry.Key; // Parent location ID
                BigInteger bitmask = entry.Value; // Bitmask value

                // Get the corresponding GrandparentNode and ParentNode
                var tableMapping = parentAndGrandparentNodes.FirstOrDefault(
                    t => t.ParentId == parentId
                );

                if (tableMapping != null)
                {
                    string grandparentNode = tableMapping.GrandparentNode;
                    string parentNode = tableMapping.ParentNode;

                    // Step 3: Construct the INSERT or UPDATE query
                    var updateOrInsertQuery = $@"
                        IF OBJECT_ID('{grandparentNode}', 'U') IS NOT NULL 
                        BEGIN
                            IF EXISTS (SELECT 1 FROM [{grandparentNode}] WHERE PersonId = @PersonId)
                            BEGIN
                                UPDATE [{grandparentNode}]
                                SET [{parentNode}] = @Bitmask, IsDeleted = 0
                                WHERE PersonId = @PersonId;
                            END
                            ELSE
                            BEGIN
                                INSERT INTO [{grandparentNode}] (PersonId, [{parentNode}], IsDeleted)
                                VALUES (@PersonId, @Bitmask, 0);
                            END
                        END";

                    // Step 4: Execute the SQL update command
                    _context.Database.ExecuteSqlRaw(updateOrInsertQuery,
                        new SqlParameter("@Bitmask", bitmask.ToString()),
                        new SqlParameter("@PersonId", personId)
                    );
                }
            }

        }

        // Get current and parent table names
        private List<LocationParentAndGrandparentViewModel> GetParentAndGrandparentNodes(string selectedLocationIds)
        {
            var query = @"
                SELECT parent.Id AS ParentId, parent.Name AS ParentNode, grandparent.Name AS GrandparentNode
                FROM Locations parent
                INNER JOIN Locations grandparent ON parent.ParentId = grandparent.Id
                WHERE parent.Id IN (SELECT value FROM STRING_SPLIT(@selectedLocationIds, ','))";

            var parameters = new Dictionary<string, object>
            {
                { "@selectedLocationIds", selectedLocationIds }
            };

            return _context.ExecuteRawQueryToList<LocationParentAndGrandparentViewModel>(query, parameters);
        }
    }
}
