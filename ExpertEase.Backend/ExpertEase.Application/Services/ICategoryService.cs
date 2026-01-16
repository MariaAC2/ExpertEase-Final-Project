using ExpertEase.Application.DataTransferObjects.CategoryDTOs;
using ExpertEase.Application.DataTransferObjects.SpecialistDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;

namespace ExpertEase.Application.Services;

public interface ICategoryService
{
    Task<ServiceResponse<CategoryAdminDto>> GetCategory(Guid id, CancellationToken cancellationToken = default); 
    Task<ServiceResponse<PagedResponse<CategoryAdminDto>>> GetCategoriesAdmin(PaginationSearchQueryParams pagination,
        CancellationToken cancellationToken = default);
    Task<ServiceResponse<List<CategoryDto>>> GetCategories(string? search = null,
        CancellationToken cancellationToken = default);
    Task<ServiceResponse<CategoryDto>> GetCategoryForSpecialist(Guid categoryId, Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<List<CategoryDto>>> GetCategoriesForSpecialist(Guid specialistId, string? search = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> AddCategory(CategoryAddDto category, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> AddCategoryToSpecialist(CategorySpecialistAddDto category, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateCategory(CategoryUpdateDto category, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteCategory(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteCategoryFromSpecialist(Guid categoryId, UserDto? requestingUser = null, CancellationToken cancellationToken = default);

}