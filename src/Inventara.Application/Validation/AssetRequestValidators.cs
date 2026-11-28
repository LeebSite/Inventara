using FluentValidation;
using Inventara.Application.Assets;

namespace Inventara.Application.Validation;

public sealed class AssetListQueryValidator : AbstractValidator<AssetListQuery>
{
    private static readonly string[] SortFields =
    [
        "id",
        "assetCode",
        "name",
        "purchaseDate",
        "purchasePrice",
        "condition",
        "status",
        "createdAt"
    ];

    public AssetListQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.SortDir)
            .Must(direction => direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
                || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
        RuleFor(query => query.Search).MaximumLength(200);
        RuleFor(query => query.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy)
                || SortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage("The requested sort field is not available.");
        RuleFor(query => query.Status).IsInEnum();
    }
}

public sealed class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.CategoryId).NotEmpty();
        RuleFor(request => request.LocationId).NotEmpty();
        RuleFor(request => request.SerialNumber).MaximumLength(200);
        RuleFor(request => request.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Condition).IsInEnum();
        RuleFor(request => request.Notes).MaximumLength(2000);
    }
}

public sealed class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.CategoryId).NotEmpty();
        RuleFor(request => request.LocationId).NotEmpty();
        RuleFor(request => request.SerialNumber).MaximumLength(200);
        RuleFor(request => request.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Condition).IsInEnum();
        RuleFor(request => request.Notes).MaximumLength(2000);
    }
}

public sealed class UploadAssetPhotoRequestValidator : AbstractValidator<UploadAssetPhotoRequest>
{
    public const long MaximumFileSize = 2 * 1024 * 1024;

    public UploadAssetPhotoRequestValidator()
    {
        RuleFor(request => request.Length).InclusiveBetween(1, MaximumFileSize);
        RuleFor(request => request)
            .Must(HasMatchingExtensionAndContentType)
            .WithMessage("Only JPG and PNG image files are supported.");
        RuleFor(request => request)
            .MustAsync(HasValidImageSignatureAsync)
            .WithMessage("The uploaded file is not a valid JPG or PNG image.");
    }

    private static bool HasMatchingExtensionAndContentType(UploadAssetPhotoRequest request)
    {
        var extension = Path.GetExtension(request.FileName);
        return (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            ? request.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
            : extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                && request.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> HasValidImageSignatureAsync(
        UploadAssetPhotoRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Content.CanSeek)
        {
            return false;
        }

        var originalPosition = request.Content.Position;
        var header = new byte[8];
        try
        {
            request.Content.Position = 0;
            var bytesRead = 0;
            while (bytesRead < header.Length)
            {
                var read = await request.Content.ReadAsync(
                    header.AsMemory(bytesRead),
                    cancellationToken);
                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }

            var extension = Path.GetExtension(request.FileName);
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? bytesRead == header.Length
                    && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                : bytesRead >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255;
        }
        finally
        {
            request.Content.Position = originalPosition;
        }
    }
}
