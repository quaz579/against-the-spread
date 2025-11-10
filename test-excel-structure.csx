#!/usr/bin/env dotnet-script
#r "nuget: EPPlus, 7.5.1"

using OfficeOpenXml;
using System.IO;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var filePath = args.Length > 0 ? args[0] : "/Users/Ben.Grossman/Code/against-the-spread/reference-docs/Week 11 Lines.xlsx";

using var package = new ExcelPackage(new FileInfo(filePath));
var worksheet = package.Workbook.Worksheets[0];

Console.WriteLine($"Worksheet: {worksheet.Name}");
Console.WriteLine($"Dimensions: {worksheet.Dimension?.Address ?? "null"}");
Console.WriteLine($"End Row: {worksheet.Dimension?.End.Row}");
Console.WriteLine($"End Col: {worksheet.Dimension?.End.Column}");
Console.WriteLine();

// Show first 10 rows and up to 10 columns
for (int row = 1; row <= Math.Min(10, worksheet.Dimension?.End.Row ?? 0); row++)
{
    Console.Write($"Row {row}: ");
    for (int col = 1; col <= Math.Min(10, worksheet.Dimension?.End.Column ?? 0); col++)
    {
        var value = worksheet.Cells[row, col].Text?.Trim();
        if (!string.IsNullOrEmpty(value))
        {
            Console.Write($"[{col}:{value}] ");
        }
    }
    Console.WriteLine();
}
