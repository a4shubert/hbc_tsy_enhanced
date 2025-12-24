using HbcRest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

public class HbcContextFactory : IDesignTimeDbContextFactory<HbcContext>
{
    public HbcContext CreateDbContext(string[] args)
    {
        var envPath = Environment.GetEnvironmentVariable("HBC_DB_PATH");
        string dbPath;
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            dbPath = envPath;
        }
        else
        {
            // Default to repo-root/hbc_db/hbc.db relative to this project folder
            var basePath = Directory.GetCurrentDirectory();
            dbPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "hbc_db", "hbc.db"));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var options = new DbContextOptionsBuilder<HbcContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new HbcContext(options);
    }
}
