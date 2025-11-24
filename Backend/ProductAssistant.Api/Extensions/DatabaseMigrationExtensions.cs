using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ProductAssistant.Core.Data;

namespace ProductAssistant.Api.Extensions;

/// <summary>
/// Extension methods for automatic database schema migrations
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Automatically ensures all tables have the required columns based on EF Core model
    /// This method compares the EF Core model with the actual database schema and adds missing columns
    /// </summary>
    public static async Task EnsureSchemaUpToDateAsync(this AppDbContext context)
    {
        try
        {
            var model = context.Model;
            var connection = context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            
            if (!wasOpen)
            {
                await connection.OpenAsync();
            }
            
            try
            {
                // Get all entity types from the model
                foreach (var entityType in model.GetEntityTypes())
                {
                    var tableName = entityType.GetTableName();
                    if (string.IsNullOrEmpty(tableName))
                        continue;
                    
                    // Check if table exists
                    if (!await TableExistsAsync(connection, tableName))
                    {
                        Console.WriteLine($"Table {tableName} does not exist - will be created by EnsureCreatedAsync");
                        continue;
                    }
                    
                    // Get all properties for this entity
                    foreach (var property in entityType.GetProperties())
                    {
                        var columnName = property.GetColumnName();
                        if (string.IsNullOrEmpty(columnName))
                            continue;
                        
                        // Skip if it's a navigation property or part of a foreign key that's stored elsewhere
                        if (property.IsForeignKey() && property.GetContainingForeignKeys().Any(fk => 
                            fk.Properties.Count > 1 || fk.PrincipalKey.Properties.Count > 1))
                            continue;
                        
                        // Check if column exists
                        if (!await ColumnExistsAsync(connection, tableName, columnName))
                        {
                            // Get SQLite column type from property
                            var columnType = GetSqliteColumnType(property);
                            
                            // Validate table and column names (they come from EF Core metadata, so should be safe)
                            // But add basic validation to prevent SQL injection
                            if (IsValidIdentifier(tableName) && IsValidIdentifier(columnName))
                            {
                                try
                                {
                                    // Note: ALTER TABLE ADD COLUMN doesn't support parameters for identifiers
                                    // Table and column names come from EF Core metadata, so they're safe
                                    #pragma warning disable EF1002 // SQL injection warning - names come from EF Core metadata
                                    await context.Database.ExecuteSqlRawAsync(
                                        $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnType}");
                                    #pragma warning restore EF1002
                                    Console.WriteLine($"Added column {columnName} ({columnType}) to table {tableName}");
                                }
                                catch (Exception ex)
                                {
                                    // Ignore "duplicate column" errors (race condition)
                                    if (!ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase) &&
                                        !ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Console.WriteLine($"Warning: Could not add column {columnName} to {tableName}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (!wasOpen)
                {
                    await connection.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error during schema migration: {ex.Message}");
        }
    }

    private static async Task<bool> TableExistsAsync(System.Data.Common.DbConnection connection, string tableName)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);
            
            var count = await command.ExecuteScalarAsync();
            return Convert.ToInt32(count) > 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> ColumnExistsAsync(System.Data.Common.DbConnection connection, string tableName, string columnName)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info(@tableName) WHERE name = @columnName";
            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@tableName";
            tableParam.Value = tableName;
            command.Parameters.Add(tableParam);
            
            var columnParam = command.CreateParameter();
            columnParam.ParameterName = "@columnName";
            columnParam.Value = columnName;
            command.Parameters.Add(columnParam);
            
            var count = await command.ExecuteScalarAsync();
            return Convert.ToInt32(count) > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string GetSqliteColumnType(IProperty property)
    {
        var clrType = property.ClrType;
        var isNullable = property.IsNullable;
        
        // Handle nullable types
        if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            clrType = Nullable.GetUnderlyingType(clrType)!;
        }
        
        // Map CLR types to SQLite types
        if (clrType == typeof(int) || clrType == typeof(long) || clrType == typeof(short) || clrType == typeof(byte))
            return "INTEGER";
        
        if (clrType == typeof(bool))
            return "INTEGER"; // SQLite uses INTEGER for boolean
        
        if (clrType == typeof(decimal) || clrType == typeof(double) || clrType == typeof(float))
            return "REAL";
        
        if (clrType == typeof(DateTime) || clrType == typeof(DateTimeOffset))
            return "TEXT";
        
        if (clrType == typeof(string))
        {
            // Check if there's a specific column type configured
            var columnType = property.GetColumnType();
            if (!string.IsNullOrEmpty(columnType))
            {
                // Convert EF Core types to SQLite types
                if (columnType.Contains("TEXT", StringComparison.OrdinalIgnoreCase))
                    return "TEXT";
                if (columnType.Contains("VARCHAR", StringComparison.OrdinalIgnoreCase))
                    return "TEXT";
            }
            return "TEXT";
        }
        
        if (clrType == typeof(byte[]))
            return "BLOB";
        
        // Default to TEXT for unknown types
        return "TEXT";
    }

    private static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return false;
        
        // SQLite identifiers can contain letters, digits, underscore, and must start with letter or underscore
        // This is a basic check - EF Core metadata should already provide valid identifiers
        return identifier.All(c => char.IsLetterOrDigit(c) || c == '_') &&
               (char.IsLetter(identifier[0]) || identifier[0] == '_');
    }
}


