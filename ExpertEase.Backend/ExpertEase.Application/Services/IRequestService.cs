using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.RequestDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Domain.Entities;

namespace ExpertEase.Application.Services;

public interface IRequestService
{
    Task<ServiceResponse<RequestDto>> GetRequest(Guid requestId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<RequestDto>>> GetRequests(Specification<Request, RequestDto> spec, PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default);
    public Task<ServiceResponse<int>> GetRequestCount(CancellationToken cancellationToken = default);
    Task<ServiceResponse> AddRequest(RequestAddDto request, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateRequest(RequestUpdateDto request, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateRequestStatus(StatusUpdateDto request, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteRequest(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
}