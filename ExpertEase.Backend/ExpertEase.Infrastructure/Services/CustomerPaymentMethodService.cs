using System.Net;
using ExpertEase.Application.DataTransferObjects.CustomerPaymentMethodDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Errors;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Specifications;
using ExpertEase.Infrastructure.Database;
using ExpertEase.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace ExpertEase.Infrastructure.Services;

/// <summary>
/// Service for managing customer saved payment methods
/// </summary>
public class CustomerPaymentMethodService(
    IRepository<WebAppDatabaseContext> repository,
    ILogger<CustomerPaymentMethodService> logger)
    : ICustomerPaymentMethodService
{
    /// <summary>
    /// Save customer payment method for future use
    /// </summary>
    public async Task<ServiceResponse<CustomerPaymentMethodDto>> SaveCustomerPaymentMethod(
        SaveCustomerPaymentMethodDto dto,
        UserDto? user = null,
        CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            return ServiceResponse.CreateErrorResponse<CustomerPaymentMethodDto>(new(
                HttpStatusCode.Forbidden,
                "User not found",
                ErrorCodes.CannotAdd));
        }

        try
        {
            logger.LogInformation("💳 Saving payment method for user: {UserId}", user.Id);

            // Verify the payment method exists in Stripe
            var paymentMethodService = new Stripe.PaymentMethodService();
            var stripePaymentMethod = await paymentMethodService.GetAsync(dto.PaymentMethodId, cancellationToken: cancellationToken);
            
            if (stripePaymentMethod == null)
            {
                return ServiceResponse.CreateErrorResponse<CustomerPaymentMethodDto>(new(
                    HttpStatusCode.BadRequest,
                    "Invalid payment method ID",
                    ErrorCodes.EntityNotFound));
            }

            // Check if payment method already exists
            var existingPaymentMethod = await repository.GetAsync(
                new CustomerPaymentMethodSpec(dto.PaymentMethodId), 
                cancellationToken);

            if (existingPaymentMethod != null)
            {
                return ServiceResponse.CreateErrorResponse<CustomerPaymentMethodDto>(new(
                    HttpStatusCode.Conflict,
                    "Payment method already saved",
                    ErrorCodes.EntityAlreadyExists));
            }

            // If this is set as default, remove default from other payment methods
            if (dto.IsDefault)
            {
                var existingDefaults = await repository.ListAsync(
                    new CustomerPaymentMethodSpec(user.Id, isDefault: true), 
                    cancellationToken);

                foreach (var pm in existingDefaults)
                {
                    pm.IsDefault = false;
                    await repository.UpdateAsync(pm, cancellationToken);
                }
            }

            // Create new payment method record
            var newPaymentMethod = new CustomerPaymentMethod
            {
                CustomerId = user.Id,
                StripeCustomerId = user.StripeCustomerId,
                StripePaymentMethodId = dto.PaymentMethodId,
                CardLast4 = dto.CardLast4,
                CardBrand = dto.CardBrand.ToUpper(),
                CardholderName = dto.CardholderName,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(newPaymentMethod, cancellationToken);

            var resultDto = new CustomerPaymentMethodDto
            {
                Id = newPaymentMethod.Id,
                UserId = newPaymentMethod.CustomerId,
                StripeCustomerId = newPaymentMethod.StripeCustomerId,
                StripePaymentMethodId = newPaymentMethod.StripePaymentMethodId,
                CardLast4 = newPaymentMethod.CardLast4,
                CardBrand = newPaymentMethod.CardBrand,
                CardholderName = newPaymentMethod.CardholderName,
                IsDefault = newPaymentMethod.IsDefault,
                CreatedAt = newPaymentMethod.CreatedAt
            };

            logger.LogInformation("Payment method saved for user {UserId}", user.Id);
            return ServiceResponse.CreateSuccessResponse(resultDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error saving payment method for user {UserId}", user.Id);
            return ServiceResponse.CreateErrorResponse<CustomerPaymentMethodDto>(new ErrorMessage(
                HttpStatusCode.InternalServerError,
                "Failed to save payment method",
                ErrorCodes.TechnicalError));
        }
    }

    // Other methods for getting, deleting, updating payment methods...
}