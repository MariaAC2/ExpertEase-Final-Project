using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.LoginDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;

namespace ExpertEase.Application.Services;

public interface IUserService
{
    Task<ServiceResponse<UserDto>> GetUser(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserDto>> GetUserAdmin(Guid id, Guid adminId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserPaymentDetailsDto>> GetUserPaymentDetails(Guid id,
        CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserDetailsDto>> GetUserDetails(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserProfileDto>> GetUserProfile(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<UserDto>>> GetUsers(Guid adminId, PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default);
    Task<ServiceResponse<int>> GetUserCount(CancellationToken cancellationToken = default);
    Task<ServiceResponse<LoginResponseDto>> Login(LoginDto login, CancellationToken cancellationToken = default);

    Task<ServiceResponse<LoginResponseDto>> SocialLogin(SocialLoginDto loginDto,
        CancellationToken cancellationToken = default);
    Task<ServiceResponse> AddUser(UserAddDto user, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<UserUpdateResponseDto>> UpdateUser(UserUpdateDto user, UserDto? requestingUser,
        CancellationToken cancellationToken = default);
    Task<ServiceResponse> AdminUpdateUser(AdminUserUpdateDto user, UserDto? requestingUser,
        CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteUser(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<LoginResponseDto>> ExchangeOAuthCode(OAuthCodeExchangeDto exchangeDto,
        CancellationToken cancellationToken = default);
}
