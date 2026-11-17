using ClosedXML.Excel;
using Inventara.Application.Assets;
using Inventara.Application.Persistence;

namespace Inventara.Infrastructure.Storage;

public sealed class ClosedXmlAssetExporter : IAssetExcelExporter
{
    public byte[] Export(IReadOnlyList<AssetExportRow> assets)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Assets");
        var headers = new[]
        {
            "Asset Code",
            "Name",
            "Category",
            "Location",
            "Vendor",
            "Serial Number",
            "Purchase Date",
            "Purchase Price",
            "Condition",
            "Status",
            "Photo URL",
            "Notes"
        };

        for (var column = 0; column < headers.Length; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var index = 0; index < assets.Count; index++)
        {
            var row = index + 2;
            var asset = assets[index];
            worksheet.Cell(row, 1).Value = asset.AssetCode;
            worksheet.Cell(row, 2).Value = asset.Name;
            worksheet.Cell(row, 3).Value = asset.Category;
            worksheet.Cell(row, 4).Value = asset.Location;
            worksheet.Cell(row, 5).Value = asset.Vendor ?? string.Empty;
            worksheet.Cell(row, 6).Value = asset.SerialNumber ?? string.Empty;
            worksheet.Cell(row, 7).Value = asset.PurchaseDate;
            worksheet.Cell(row, 8).Value = asset.PurchasePrice;
            worksheet.Cell(row, 9).Value = asset.Condition.ToString();
            worksheet.Cell(row, 10).Value = asset.Status.ToString();
            worksheet.Cell(row, 11).Value = asset.PhotoUrl ?? string.Empty;
            worksheet.Cell(row, 12).Value = asset.Notes ?? string.Empty;
        }

        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.SheetView.FreezeRows(1);
        if (assets.Count > 0)
        {
            worksheet.Range(1, 1, assets.Count + 1, headers.Length).SetAutoFilter();
        }

        worksheet.Columns().AdjustToContents();
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }
}
