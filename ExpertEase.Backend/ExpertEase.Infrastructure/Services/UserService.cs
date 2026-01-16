using System.Net;
using System.Text;
using System.Text.Json;
using ExpertEase.Application.DataTransferObjects.LoginDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Errors;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Application.Specifications;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;
using ExpertEase.Domain.Specifications;
using ExpertEase.Infrastructure.Configurations;
using ExpertEase.Infrastructure.Database;
using ExpertEase.Infrastructure.Repositories;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;


namespace ExpertEase.Infrastructure.Services;

public class UserService(
    IRepository<WebAppDatabaseContext> repository,
    ILoginService loginService,
    HttpClient httpClient,
    IStripeAccountService stripeAccountService,
    IOptions<GoogleOAuthConfiguration> googleOAuthConfig) : IUserService
{

    private readonly GoogleOAuthConfiguration _googleOAuthConfig = googleOAuthConfig.Value;
    public async Task<ServiceResponse<UserDto>> GetUser(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new UserProjectionSpec(id), cancellationToken);

        return result != null
            ? ServiceResponse.CreateSuccessResponse(result)
            : ServiceResponse.CreateErrorResponse<UserDto>(CommonErrors.UserNotFound);
    }
    
    public async Task<ServiceResponse<UserDetailsDto>> GetUserDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new UserDetailsProjectionSpec(id), cancellationToken);
        
        if (result == null)
            return ServiceResponse.CreateErrorResponse<UserDetailsDto>(CommonErrors.UserNotFound);
        
        var reviews = await repository.ListAsync(new ReviewProjectionSpec(id), cancellationToken);
        result = result with { Reviews = reviews };

        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse<UserProfileDto>> GetUserProfile(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new UserProfileProjectionSpec(id), cancellationToken);
        
        if (result == null)
            return ServiceResponse.CreateErrorResponse<UserProfileDto>(CommonErrors.UserNotFound);

        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse<UserPaymentDetailsDto>> GetUserPaymentDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new UserPaymentDetailsProjectionSpec(id), cancellationToken);
        
        return result == null ? 
            ServiceResponse.CreateErrorResponse<UserPaymentDetailsDto>(CommonErrors.UserNotFound) : 
            ServiceResponse.CreateSuccessResponse(result);
    }

    public async Task<ServiceResponse<UserDto>> GetUserAdmin(Guid id, Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new AdminUserProjectionSpec(id, adminId), cancellationToken);

        return result != null
            ? ServiceResponse.CreateSuccessResponse(result)
            : ServiceResponse.CreateErrorResponse<UserDto>(CommonErrors.UserNotFound);
    }

    public async Task<ServiceResponse<PagedResponse<UserDto>>> GetUsers(Guid adminId,
        PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var result = await repository.PageAsync(pagination, new AdminUserProjectionSpec(pagination.Search, adminId),
            cancellationToken); // Use the specification and pagination API to get only some entities from the database.

        return ServiceResponse.CreateSuccessResponse(result);
    }

    public async Task<ServiceResponse<int>> GetUserCount(CancellationToken cancellationToken = default)
    {
        return ServiceResponse.CreateSuccessResponse(await repository.GetCountAsync<User>(cancellationToken));
    }

    public async Task<ServiceResponse<LoginResponseDto>> Login(LoginDto login,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new UserSpec(login.Email), cancellationToken);

        if (result == null) // Verify if the user is found in the database.
            return ServiceResponse.CreateErrorResponse<LoginResponseDto>(CommonErrors
                .UserNotFound); // Pack the proper error as the response.

        if (result.Password !=
            login.Password) // Verify if the password hash of the request is the same as the one in the database.
            return ServiceResponse.CreateErrorResponse<LoginResponseDto>(new ErrorMessage(HttpStatusCode.BadRequest,
                "Wrong password!", ErrorCodes.WrongPassword));

        var user = new UserDto
        {
            Id = result.Id,
            Email = result.Email,
            FullName = result.FullName,
            Role = result.Role,
            AuthProvider = result.AuthProvider,
        };

        return ServiceResponse.CreateSuccessResponse(new LoginResponseDto
        {
            User = user,
            Token = loginService.GetToken(user, DateTime.UtcNow,
                new TimeSpan(7, 0, 0, 0)) // Get a JWT for the user issued now and that expires in 7 days.
        });
    }

    public async Task<ServiceResponse<LoginResponseDto>> SocialLogin(SocialLoginDto loginDto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Token))
            return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                new ErrorMessage(HttpStatusCode.BadRequest, "Missing token", ErrorCodes.Invalid));

        var provider = loginDto.Provider.ToLower();
        SocialUserInfo? userInfo;

        try
        {
            userInfo = provider switch
            {
                "google" => await ValidateGoogleToken(loginDto.Token),
                "facebook" => await ValidateFacebookToken(loginDto.Token),
                _ => null
            };
        }
        catch (Exception ex)
        {
            // Log the actual exception for debugging
            Console.WriteLine($"Social login validation error for {provider}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                new ErrorMessage(HttpStatusCode.BadRequest, $"Invalid {provider} token: {ex.Message}", ErrorCodes.Invalid));
        }

        if (userInfo == null)
            return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                new ErrorMessage(HttpStatusCode.BadRequest, "Unsupported provider or invalid token", ErrorCodes.Invalid));

        // Check if email is provided
        if (string.IsNullOrWhiteSpace(userInfo.Email))
            return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                new ErrorMessage(HttpStatusCode.BadRequest, "Email not provided by social provider", ErrorCodes.Invalid));

        var result = await repository.GetAsync(new UserSpec(userInfo.Email), cancellationToken);

        if (result == null)
        {
            var authProvider = provider switch
            {
                "google" => AuthProvider.Google,
                "facebook" => AuthProvider.Facebook,
                _ => AuthProvider.Local
            };

            var newUser = new User
            {
                Email = userInfo.Email,
                FullName = userInfo.Name,
                Role = userInfo.Email.EndsWith("@admin.com", StringComparison.OrdinalIgnoreCase)
                    ? UserRoleEnum.Admin
                    : UserRoleEnum.Client,
                AuthProvider = authProvider,
                ProfilePictureUrl = userInfo.Picture
            };

            try
            {
                await repository.AddAsync(newUser, cancellationToken);
                
                // Create Stripe customer for new social user
                var stripeCustomerId = await stripeAccountService.CreateCustomer(newUser.Email, newUser.FullName, newUser.Id);
                Console.WriteLine("Stripe customer id: " + stripeCustomerId.Result);

                if (stripeCustomerId.Result != null)
                {
                    newUser.StripeCustomerId = stripeCustomerId.Result;
                    await repository.UpdateAsync(newUser, cancellationToken);
                }
                
                result = newUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating social user: {ex.Message}");
                return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                    new ErrorMessage(HttpStatusCode.InternalServerError, "Failed to create user account"));
            }
        }
        
        var user = new UserDto
        {
            Id = result.Id,
            Email = result.Email,
            FullName = result.FullName,
            Role = result.Role,
            AuthProvider = result.AuthProvider,
        };
        
        return ServiceResponse.CreateSuccessResponse(new LoginResponseDto
        {
            User = user,
            Token = loginService.GetToken(user, DateTime.UtcNow, new TimeSpan(7, 0, 0, 0))
        });
    }
    
    public async Task<ServiceResponse<LoginResponseDto>> ExchangeOAuthCode(OAuthCodeExchangeDto exchangeDto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exchangeDto.Code))
            return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                new ErrorMessage(HttpStatusCode.BadRequest, "Missing authorization code", ErrorCodes.Invalid));

        if (exchangeDto.Provider.ToLower() != "google")
            return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                new ErrorMessage(HttpStatusCode.BadRequest, "Unsupported provider", ErrorCodes.Invalid));

        try
        {
            // Exchange code for access token
            var tokenResponse = await ExchangeCodeForToken(exchangeDto.Code, exchangeDto.RedirectUri);
            
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                    new ErrorMessage(HttpStatusCode.BadRequest, "Failed to exchange code for token", ErrorCodes.Invalid));

            // Get user info from Google
            var userInfo = await GetGoogleUserInfo(tokenResponse.AccessToken);
            
            if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                    new ErrorMessage(HttpStatusCode.BadRequest, "Failed to get user info from Google", ErrorCodes.Invalid));

            // Find or create user
            var result = await repository.GetAsync(new UserSpec(userInfo.Email), cancellationToken);

            if (result == null)
            {
                var newUser = new User
                {
                    Email = userInfo.Email,
                    FullName = userInfo.Name,
                    Role = userInfo.Email.EndsWith("@admin.com", StringComparison.OrdinalIgnoreCase)
                        ? UserRoleEnum.Admin
                        : UserRoleEnum.Client,
                    AuthProvider = AuthProvider.Google,
                    ProfilePictureUrl = userInfo.Picture
                };

                await repository.AddAsync(newUser, cancellationToken);
                
                // Create Stripe customer
                var stripeCustomerId = await stripeAccountService.CreateCustomer(newUser.Email, newUser.FullName, newUser.Id);
                if (stripeCustomerId.Result != null)
                {
                    newUser.StripeCustomerId = stripeCustomerId.Result;
                    await repository.UpdateAsync(newUser, cancellationToken);
                }
                
                result = newUser;
            }
            
            var user = new UserDto
            {
                Id = result.Id,
                Email = result.Email,
                FullName = result.FullName,
                Role = result.Role,
                AuthProvider = result.AuthProvider,
            };
            
            return ServiceResponse.CreateSuccessResponse(new LoginResponseDto
            {
                User = user,
                Token = loginService.GetToken(user, DateTime.UtcNow, new TimeSpan(7, 0, 0, 0))
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OAuth code exchange error: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                new ErrorMessage(HttpStatusCode.InternalServerError, "OAuth exchange failed"));
        }
    }

    private async Task<GoogleTokenResponse?> ExchangeCodeForToken(string code, string redirectUrl)
    {
        var tokenRequest = new
        {
            client_id = _googleOAuthConfig.ClientId, // Add this to your configuration
            client_secret = _googleOAuthConfig.ClientSecret, // Add this to your configuration
            code,
            grant_type = "authorization_code",
            redirect_uri = redirectUrl
        };

        var content = new StringContent(JsonSerializer.Serialize(tokenRequest), Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Google token exchange error: {errorContent}");
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
    }

    private async Task<GoogleUserInfo?> GetGoogleUserInfo(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        var response = await httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Google user info error: {errorContent}");
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleUserInfo>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
    }

    private async Task<SocialUserInfo?> ValidateGoogleToken(string token)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(token);
            
            Console.WriteLine($"Google token validated successfully for email: {payload.Email}");
            
            return new SocialUserInfo
            {
                Email = payload.Email,
                Name = payload.Name,
                Picture = payload.Picture
            };
        }
        catch (InvalidJwtException ex)
        {
            Console.WriteLine($"Invalid Google JWT: {ex.Message}");
            throw new Exception($"Invalid Google token: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Google token validation error: {ex.Message}");
            throw new Exception($"Google token validation failed: {ex.Message}");
        }
    }

    private async Task<SocialUserInfo?> ValidateFacebookToken(string token)
    {
        try
        {
            // First, validate the token against Facebook's debug endpoint
            var debugResponse = await httpClient.GetAsync($"https://graph.facebook.com/debug_token?input_token={token}&access_token={token}");
            
            if (!debugResponse.IsSuccessStatusCode)
            {
                var debugContent = await debugResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Facebook token debug failed: {debugContent}");
                throw new Exception("Facebook token validation failed");
            }

            // Get user info from Facebook Graph API
            var response = await httpClient.GetAsync($"https://graph.facebook.com/me?fields=id,name,email,picture&access_token={token}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Facebook Graph API error: {errorContent}");
                throw new Exception($"Failed to get Facebook user info: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Facebook user data received: {content}");
            
            var facebookUser = JsonSerializer.Deserialize<FacebookUserResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (facebookUser == null)
            {
                Console.WriteLine("Failed to deserialize Facebook user data");
                throw new Exception("Invalid Facebook user data format");
            }

            if (string.IsNullOrEmpty(facebookUser.Email))
            {
                Console.WriteLine("Facebook user email is missing");
                throw new Exception("Email not provided by Facebook. Please ensure email permission is granted.");
            }

            Console.WriteLine($"Facebook token validated successfully for email: {facebookUser.Email}");

            return new SocialUserInfo
            {
                Email = facebookUser.Email,
                Name = facebookUser.Name,
                Picture = facebookUser.Picture?.Data?.Url
            };
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Facebook JSON parsing error: {ex.Message}");
            throw new Exception($"Invalid Facebook response format: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Facebook HTTP request error: {ex.Message}");
            throw new Exception($"Facebook API request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Facebook token validation error: {ex.Message}");
            throw;
        }
    }

    public async Task<ServiceResponse> AddUser(UserAddDto user, UserDto? requestingUser,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser != null &&
            requestingUser.Role !=
            UserRoleEnum.Admin) // Verify who can add the user, you can change this however you se fit.
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Forbidden,
                "Only the admin can add users!",
                ErrorCodes.CannotAdd));

        var result = await repository.GetAsync(new UserSpec(user.Email), cancellationToken);

        if (result != null)
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Conflict,
                "The user already exists!",
                ErrorCodes.UserAlreadyExists));

        var newUser = new User
        {
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Password = user.Password
        };

        await repository.AddAsync(newUser, cancellationToken);
        
        var stripeCustomerId = await stripeAccountService.CreateCustomer(newUser.Email, newUser.FullName, newUser.Id);
        Console.WriteLine("Stripe customer id: " + stripeCustomerId.Result);

        if (stripeCustomerId.Result == null)
        {
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Forbidden,
                "Stripe customer id doesn't exist"));
        }
        
        // Update user with Stripe customer ID
        newUser.StripeCustomerId = stripeCustomerId.Result;
        await repository.UpdateAsync(newUser, cancellationToken);

        // var fullName = $"{user.LastName} {user.FirstName}";
        // await mailService.SendMail(user.Email, "Welcome!", MailTemplates.UserAddTemplate(fullName), true, "ExpertEase Team", cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse<UserUpdateResponseDto>> UpdateUser(UserUpdateDto user, UserDto? requestingUser,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser != null && requestingUser.Role != UserRoleEnum.Admin &&
            requestingUser.Id != user.Id) // Verify who can add the user, you can change this however you se fit.
            return ServiceResponse.CreateErrorResponse<UserUpdateResponseDto>(new ErrorMessage(HttpStatusCode.Forbidden,
                "Only the admin or the own user can update the user!", ErrorCodes.CannotUpdate));

        var entity = await repository.GetAsync(new UserSpec(user.Id), cancellationToken);

        if (entity == null)
            return ServiceResponse.CreateErrorResponse<UserUpdateResponseDto>(
                new ErrorMessage(HttpStatusCode.NotFound, "User not found", ErrorCodes.EntityNotFound));

        if (!string.IsNullOrWhiteSpace(entity.FullName))
        {
            var nameParts = entity.FullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : user.FirstName;
            var lastName = nameParts.Length > 1 ? nameParts[1] : user.LastName;
            entity.FullName = $"{firstName} {lastName}";
        }

        entity.Password = user.Password ?? entity.Password;
        entity.ContactInfo ??= new ContactInfo
        {
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Address = user.Address ?? string.Empty
        };

        await repository.UpdateAsync(entity, cancellationToken); // Update the entity and persist the changes.
        
        var userDto = new UserDto
        {
            Id = entity.Id,
            Email = entity.Email,
            FullName = entity.FullName,
            Role = entity.Role,
            AuthProvider = entity.AuthProvider,
        };

        return ServiceResponse.CreateSuccessResponse(new UserUpdateResponseDto
        {
            User = user,
            Token = loginService.GetToken(userDto, DateTime.UtcNow,
                new TimeSpan(7, 0, 0, 0)) // Get a JWT for the user issued now and that expires in 7 days.
        });
    }

    public async Task<ServiceResponse> AdminUpdateUser(AdminUserUpdateDto user, UserDto? requestingUser,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser != null && requestingUser.Role != UserRoleEnum.Admin &&
            requestingUser.Id != user.Id) // Verify who can add the user, you can change this however you se fit.
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Forbidden,
                "Only the admin or the own user can update the user!", ErrorCodes.CannotUpdate));

        var entity = await repository.GetAsync(new UserSpec(user.Id), cancellationToken);

        if (entity == null)
            return ServiceResponse.CreateErrorResponse(
                new ErrorMessage(HttpStatusCode.NotFound, "User not found", ErrorCodes.EntityNotFound));

        entity.FullName = user.FullName ?? entity.FullName;
        entity.Role = user.Role ?? entity.Role;

        await repository.UpdateAsync(entity, cancellationToken); // Update the entity and persist the changes.

        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse> DeleteUser(Guid id, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser != null && requestingUser.Role != UserRoleEnum.Admin &&
            requestingUser.Id != id) // Verify who can add the user, you can change this however you se fit.
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Forbidden,
                "Only the admin or the own user can delete the user!", ErrorCodes.CannotDelete));

        await repository.DeleteAsync<User>(id, cancellationToken); // Delete the entity.

        return ServiceResponse.CreateSuccessResponse();
    }
}