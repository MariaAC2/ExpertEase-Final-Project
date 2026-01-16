using ExpertEase.Application.DataTransferObjects.SpecialistDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;

namespace ExpertEase.Application.Services;

public interface ISpecialistService
{
    Task<ServiceResponse> AddSpecialist(SpecialistAddDto user, UserDto? requestingUser, CancellationToken cancellationToken = default);
    Task<ServiceResponse<SpecialistDto>> GetSpecialist(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<SpecialistDto>>> GetSpecialists(SpecialistPaginationQueryParams pagination, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<SpecialistDto>>> SearchSpecialistsByCategory(Guid categoryId, PaginationQueryParams pagination, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<SpecialistDto>>> SearchSpecialistsByRatingRange(int minRating, int maxRating, PaginationQueryParams pagination, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<SpecialistDto>>> GetTopRatedSpecialists(PaginationQueryParams pagination, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<SpecialistDto>>> SearchSpecialistsByExperienceRange(string experienceRange, PaginationQueryParams pagination, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateSpecialist(SpecialistUpdateDto user, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteSpecialist(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
}