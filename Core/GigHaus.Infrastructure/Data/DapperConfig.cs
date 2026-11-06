using Dapper;
using System.Text.RegularExpressions;

namespace GigHaus.Infrastructure.Data;

public static class DapperConfig
{
    public static void Configure()
    {
        // Map snake_case columns to PascalCase properties
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }
}

