using System.Linq.Expressions;
using FluentValidation;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;

namespace Inventara.Application.Categories;

public sealed class CategoryService(
    IEntityRepository<Category> categories,
    IValidator<ListQuery> listQueryValidator,
    IValidator<CreateCategoryRequest> createValidator,
    IValidator<UpdateCategoryRequest> updateValidator) : ICategoryService
{
    public async Task<PagedResponse<CategoryResponse>> GetPageAsync(
        ListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var search = query.Search?.Trim();
        Expression<Func<Category, bool>> predicate = string.IsNullOrEmpty(search)
            ? category => true
            : category => category.Name.Contains(search) || category.Description.Contains(search);
        var sortBy = ServiceHelpers.ResolveSortField(
            query.SortBy,
            nameof(Category.Name),
            ("id", nameof(Category.Id)),
            ("name", nameof(Category.Name)),
            ("description", nameof(Category.Description)),
            ("createdAt", nameof(Category.CreatedAt)));

        var result = await categories.GetPageAsync(
            predicate,
            query.Page,
            query.PageSize,
            sortBy,
            query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        return ServiceHelpers.MapPage(result, ToResponse);
    }

    public async Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);
        return category is null
            ? throw new EntityNotFoundException("Category not found.")
            : ToResponse(category);
    }

    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var name = request.Name.Trim();
        if (await HasNameAsync(name, null, cancellationToken))
        {
            throw new ConflictException("A category with this name already exists.");
        }

        var category = new Category { Name = name, Description = request.Description.Trim() };
        await categories.AddAsync(category, cancellationToken);
        await categories.SaveChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var category = await categories.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            throw new EntityNotFoundException("Category not found.");
        }

        var name = request.Name.Trim();
        if (await HasNameAsync(name, category.Id, cancellationToken))
        {
            throw new ConflictException("A category with this name already exists.");
        }

        category.Name = name;
        category.Description = request.Description.Trim();
        categories.Update(category);
        await categories.SaveChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            throw new EntityNotFoundException("Category not found.");
        }

        category.IsDeleted = true;
        categories.Update(category);
        await categories.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> HasNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        Expression<Func<Category, bool>> predicate = category =>
            category.Name.ToLower() == name.ToLower()
            && (exceptId == null || category.Id != exceptId.Value);

        return categories.AnyIncludingDeletedAsync(predicate, cancellationToken);
    }

    private static CategoryResponse ToResponse(Category category) =>
        new(category.Id, category.Name, category.Description);
}
