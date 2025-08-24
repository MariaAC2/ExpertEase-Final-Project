using System.Net;
using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.PaymentDTOs;
using ExpertEase.Application.DataTransferObjects.StripeAccountDTOs;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using Stripe;

namespace ExpertEase.Infrastructure.Services;

public class StripeAccountService : IStripeAccountService
{
    private readonly StripeSettings _stripeConfiguration;
    private readonly bool _isTestMode;

    public StripeAccountService(IOptions<StripeSettings> stripeConfiguration)
    {
        _stripeConfiguration = stripeConfiguration.Value;
        StripeConfiguration.ApiKey = _stripeConfiguration.SecretKey;
        _isTestMode = _stripeConfiguration.SecretKey.StartsWith("sk_test_");
    }

    public async Task<string> CreateConnectedAccount(string email)
    {
        var service = new AccountService();

        var options = new AccountCreateOptions
        {
            Type = "express",
            Country = "RO",
            Email = email,
            Capabilities = new AccountCapabilitiesOptions
            {
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true }
            }
        };

        var account = await service.CreateAsync(options);
        return account.Id;
    }

    public async Task<ServiceResponse<StripeAccountLinkResponseDTO>> GenerateOnboardingLink(string accountId)
    {
        var linkService = new AccountLinkService();
        var link = await linkService.CreateAsync(new AccountLinkCreateOptions
        {
            Account = accountId,
            ReturnUrl = "http://localhost:4200/profile/stripe-account?status=onboarding-complete",
            RefreshUrl = "http://localhost:4200/profile/stripe-account?status=onboarding-refresh",
            Type = "account_onboarding"
        });
        
        if (link == null || string.IsNullOrEmpty(link.Url))
        {
            return ServiceResponse.CreateErrorResponse<StripeAccountLinkResponseDTO>(
                new(HttpStatusCode.Forbidden, "Failed to create account link. Please try again later."));
        }

        return ServiceResponse.CreateSuccessResponse(new StripeAccountLinkResponseDTO
        {
            Url = link.Url
        });
    }
    
    public async Task<ServiceResponse<StripeAccountLinkResponseDTO>> GenerateDashboardLink(string accountId)
    {
        var linkService = new AccountLinkService();
        var link = await linkService.CreateAsync(new AccountLinkCreateOptions
        {
            Account = accountId,
            ReturnUrl = "http://localhost:4200/profile/stripe-account?status=dashboard-complete",
            RefreshUrl = "http://localhost:4200/profile/stripe-account?status=dashboard-refresh",
            Type = "account_update"
        });
        
        if (link == null || string.IsNullOrEmpty(link.Url))
        {
            return ServiceResponse.CreateErrorResponse<StripeAccountLinkResponseDTO>(
                new(HttpStatusCode.Forbidden, "Failed to create dashboard link. Please try again later."));
        }

        return ServiceResponse.CreateSuccessResponse(new StripeAccountLinkResponseDTO
        {
            Url = link.Url
        });
    }

    public async Task<ServiceResponse<StripeAccountStatusDTO>> GetAccountStatus(string accountId)
    {
        try
        {
            var service = new AccountService();
            var account = await service.GetAsync(accountId);
        
            if (account == null)
            {
                return ServiceResponse.CreateErrorResponse<StripeAccountStatusDTO>(
                    new(HttpStatusCode.NotFound, "Stripe account not found"));
            }

            // For test accounts, consider enabled accounts as "complete" for payment purposes
            var isReadyForPayments = account.ChargesEnabled && 
                (_isTestMode || account is { PayoutsEnabled: true, DetailsSubmitted: true });

            var status = new StripeAccountStatusDTO
            {
                AccountId = account.Id,
                IsActive = isReadyForPayments,
                ChargesEnabled = account.ChargesEnabled,
                PayoutsEnabled = account.PayoutsEnabled,
                DetailsSubmitted = account.DetailsSubmitted,
                RequirementsCurrentlyDue = account.Requirements?.CurrentlyDue?.ToList() ?? new List<string>(),
                RequirementsEventuallyDue = account.Requirements?.EventuallyDue?.ToList() ?? new List<string>(),
                DisabledReason = account.Requirements?.DisabledReason,
                IsTestMode = _isTestMode,
                CanReceivePayments = account.ChargesEnabled
            };

            return ServiceResponse.CreateSuccessResponse(status);
        }
        catch (Exception ex)
        {
            return ServiceResponse.CreateErrorResponse<StripeAccountStatusDTO>(
                new(HttpStatusCode.InternalServerError, $"Error checking account status: {ex.Message}"));
        }
    }

    public async Task<ServiceResponse<string>> CreateCustomer(string email, string fullName, Guid userId)
    {
        try
        {
            Console.WriteLine($"👤 Creating Stripe customer for: {email}");
        
            var customerService = new CustomerService();
        
            var customerOptions = new CustomerCreateOptions
            {
                Email = email,
                Name = fullName,
                Description = $"ExpertEase platform user: {fullName}",
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId.ToString() },
                    { "platform", "ExpertEase" },
                    { "created_at", DateTime.UtcNow.ToString("O") }
                }
            };

            var customer = await customerService.CreateAsync(customerOptions);
        
            Console.WriteLine($"✅ Stripe customer created successfully: {customer.Id}");
            
            return ServiceResponse.CreateSuccessResponse(customer.Id);
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"❌ Stripe error creating customer: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<string>(
                new(HttpStatusCode.BadRequest, $"Failed to create Stripe customer: {ex.Message}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ General error creating customer: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<string>(
                new(HttpStatusCode.InternalServerError, $"Error creating customer: {ex.Message}"));
        }
    }

    /// <summary>
    /// DEPRECATED: Keep for backward compatibility
    /// </summary>
    [Obsolete("Use CreatePaymentIntent(CreatePaymentIntentDTO) instead")]
    public async Task<string> CreatePaymentIntent(decimal amount, string stripeAccountId)
    {
        var dto = new CreatePaymentIntentDTO
        {
            TotalAmount = amount,
            ServiceAmount = amount,
            ProtectionFee = 0,
            SpecialistAccountId = stripeAccountId,
            Description = "Plată pentru serviciu ExpertEase"
        };

        var result = await CreatePaymentIntent(dto);
        return result.ClientSecret;
    }

    /// <summary>
    /// Enhanced payment intent creation with escrow support
    /// Creates payment intent that holds money on platform account until released
    /// </summary>
    public async Task<PaymentIntentResponseDTO> CreatePaymentIntent(CreatePaymentIntentDTO dto)
    {
        try
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(dto.TotalAmount * 100), // Total amount in cents (service + fee)
                Currency = dto.Currency?.ToLower() ?? "ron",
                PaymentMethodTypes = new List<string> { "card" },
                Description = dto.Description ?? "Plată pentru serviciu ExpertEase",
                Metadata = new Dictionary<string, string>
                {
                    { "platform", "ExpertEase" },
                    { "specialist_account_id", dto.SpecialistAccountId },
                    { "service_amount", (dto.ServiceAmount * 100).ToString() },
                    { "protection_fee", (dto.ProtectionFee * 100).ToString() },
                    { "payment_type", "escrow" },
                    { "created_at", DateTime.UtcNow.ToString("O") }
                }
            };

            Console.WriteLine($"💳 Creating Stripe PaymentIntent:");
            Console.WriteLine($"   - Total Amount: {dto.TotalAmount} RON");
            Console.WriteLine($"   - Service Amount: {dto.ServiceAmount} RON");
            Console.WriteLine($"   - Protection Fee: {dto.ProtectionFee} RON");
            Console.WriteLine($"   - Mode: ESCROW (money held on platform)");

            var paymentIntent = await service.CreateAsync(options);

            var response = new PaymentIntentResponseDTO
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                StripeAccountId = dto.SpecialistAccountId,
                ServiceAmount = dto.ServiceAmount,
                ProtectionFee = dto.ProtectionFee,
                TotalAmount = dto.TotalAmount,
                ProtectionFeeDetails = null
            };

            Console.WriteLine($"✅ PaymentIntent created successfully: {paymentIntent.Id}");
            
            return response;
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"❌ Stripe error creating PaymentIntent: {ex.Message}");
            throw new InvalidOperationException($"Stripe payment intent creation failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ General error creating PaymentIntent: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Transfer money to specialist when service is completed
    /// Automatically creates test funds in test mode if needed
    /// </summary>
    public async Task<ServiceResponse<string>> TransferToSpecialist(
        string paymentIntentId, 
        string specialistAccountId, 
        decimal amount, 
        string reason = "Service completed")
    {
        try
        {
            // In test mode, ensure we have sufficient funds
            if (_isTestMode)
            {
                Console.WriteLine($"🧪 Test mode: Ensuring sufficient funds for transfer...");
                var fundsResult = await EnsureTestFunds(amount * 2); // 2x amount for safety
                if (!fundsResult.IsSuccess)
                {
                    return ServiceResponse.CreateErrorResponse<string>(fundsResult.Error);
                }
                
                // Wait for funds to become available
                await Task.Delay(1500);
            }

            Console.WriteLine($"🔄 Transferring {amount} RON to specialist {specialistAccountId}");
            Console.WriteLine($"📝 Reason: {reason}");

            var transferService = new TransferService();
            var transferOptions = new TransferCreateOptions
            {
                Amount = (long)(amount * 100), // Amount in cents
                Currency = "ron",
                Destination = specialistAccountId,
                Description = reason,
                Metadata = new Dictionary<string, string>
                {
                    { "payment_intent_id", paymentIntentId },
                    { "transfer_reason", reason },
                    { "platform", "ExpertEase" },
                    { "transfer_type", "service_completion" },
                    { "test_mode", _isTestMode.ToString() },
                    { "created_at", DateTime.UtcNow.ToString("O") }
                }
            };

            var transfer = await transferService.CreateAsync(transferOptions);
            
            Console.WriteLine($"✅ Transfer successful: {transfer.Id}");
            Console.WriteLine($"💰 Amount: {amount} RON transferred to {specialistAccountId}");
            
            return ServiceResponse.CreateSuccessResponse(transfer.Id);
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"❌ Stripe transfer failed: {ex.Message}");
            
            // Handle insufficient funds with helpful error message
            if (ex.Message.Contains("insufficient available funds"))
            {
                var errorMessage = _isTestMode 
                    ? "Insufficient test funds. The system attempted to create test funds but failed. Try again or create test payments manually in Stripe Dashboard."
                    : "Insufficient platform funds. This indicates a problem with the escrow system.";
                    
                return ServiceResponse.CreateErrorResponse<string>(
                    new(HttpStatusCode.BadRequest, errorMessage));
            }
            
            return ServiceResponse.CreateErrorResponse<string>(
                new(HttpStatusCode.BadRequest, $"Transfer failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Transfer error: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<string>(
                new(HttpStatusCode.InternalServerError, $"Transfer error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Refund money to client if service fails or is cancelled
    /// </summary>
    public async Task<ServiceResponse<string>> RefundPayment(
        string paymentIntentId, 
        decimal refundAmount, 
        string reason = "Service cancelled")
    {
        try
        {
            Console.WriteLine($"💸 Processing refund: {refundAmount} RON");
            Console.WriteLine($"📝 Reason: {reason}");

            var refundService = new RefundService();
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = (long)(refundAmount * 100),
                Reason = "requested_by_customer",
                Metadata = new Dictionary<string, string>
                {
                    { "refund_reason", reason },
                    { "platform", "ExpertEase" },
                    { "refund_type", "service_cancellation" },
                    { "created_at", DateTime.UtcNow.ToString("O") }
                }
            };

            var refund = await refundService.CreateAsync(refundOptions);
            
            Console.WriteLine($"✅ Refund successful: {refund.Id}");
            Console.WriteLine($"💰 Amount: {refundAmount} RON refunded");
            
            return ServiceResponse.CreateSuccessResponse(refund.Id);
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"❌ Stripe refund failed: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<string>(
                new(HttpStatusCode.BadRequest, $"Refund failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Refund error: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<string>(
                new(HttpStatusCode.InternalServerError, $"Refund error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Create test funds for transfers in test mode
    /// Uses simple charge creation to add money to platform balance
    /// </summary>
    private async Task<ServiceResponse<string>> EnsureTestFunds(decimal amount)
    {
        if (!_isTestMode)
        {
            return ServiceResponse.CreateSuccessResponse("Production mode - no test funds needed");
        }

        try
        {
            Console.WriteLine($"💳 Creating test funds: {amount} RON");
            
            var chargeService = new ChargeService();
            var charge = await chargeService.CreateAsync(new ChargeCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = "ron",
                Source = "tok_visa", // Simple test token that always works
                Description = "Test funds for ExpertEase platform transfers",
                Metadata = new Dictionary<string, string>
                {
                    { "purpose", "test_platform_funds" },
                    { "platform", "ExpertEase" },
                    { "test_mode", "true" },
                    { "created_at", DateTime.UtcNow.ToString("O") }
                }
            });

            Console.WriteLine($"✅ Test funds created: {charge.Id}");
            Console.WriteLine($"💰 Amount: {amount} RON added to platform balance");
            Console.WriteLine($"📊 Status: {charge.Status}");
            
            return ServiceResponse.CreateSuccessResponse(charge.Id);
        }
        catch (StripeException ex)
        {
            Console.WriteLine($"❌ Test funds creation failed: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<string>(
                new(HttpStatusCode.BadRequest, $"Test funds creation failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating test funds: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<string>(
                new(HttpStatusCode.InternalServerError, $"Error creating test funds: {ex.Message}"));
        }
    }

    /// <summary>
    /// Extract PaymentIntent ID from client secret
    /// </summary>
    public static string ExtractPaymentIntentIdFromClientSecret(string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new ArgumentException("Client secret cannot be null or empty", nameof(clientSecret));

        // Client secret format: "pi_1234567890_secret_abcdef"
        // We want: "pi_1234567890"
        var parts = clientSecret.Split('_');
        
        if (parts.Length < 2 || !parts[0].StartsWith("pi"))
            throw new ArgumentException("Invalid client secret format", nameof(clientSecret));

        return $"{parts[0]}_{parts[1]}";
    }
}