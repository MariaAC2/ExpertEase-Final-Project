using ExpertEase.Application.DataTransferObjects.CustomerPaymentMethodDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Responses;

namespace ExpertEase.Application.Services;

public interface ICustomerPaymentMethodService
{
    Task<ServiceResponse<CustomerPaymentMethodDto>> SaveCustomerPaymentMethod(
        SaveCustomerPaymentMethodDto dto,
        UserDto? user = null,
        CancellationToken cancellationToken = default);
}