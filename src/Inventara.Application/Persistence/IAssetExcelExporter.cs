using Inventara.Application.Assets;

namespace Inventara.Application.Persistence;

public interface IAssetExcelExporter
{
    byte[] Export(IReadOnlyList<AssetExportRow> assets);
}
