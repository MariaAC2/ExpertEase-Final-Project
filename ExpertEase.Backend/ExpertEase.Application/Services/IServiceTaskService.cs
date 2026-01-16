using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.ServiceTaskDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Domain.Entities;

namespace ExpertEase.Application.Services;

public interface IServiceTaskService
{
    Task<ServiceResponse> CreateServiceTaskFromPayment(Guid paymentId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ServiceTask>> AddServiceTask(ServiceTaskAddDto service, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ServiceTaskDto>> GetServiceTask(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ServiceTaskDto>> GetCurrentServiceTask(Guid otherUserId,
        UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<ServiceTaskDto>>> GetServiceTasks(PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateServiceTask(ServiceTaskUpdateDto serviceTask, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateServiceTaskStatus(JobStatusUpdateDto serviceTask, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteServiceTask(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
}