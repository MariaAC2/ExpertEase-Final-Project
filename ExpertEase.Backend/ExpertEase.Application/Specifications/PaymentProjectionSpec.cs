using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects.PaymentDTOs;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpertEase.Application.Specifications;

/// <summary>
/// ✅ UPDATED: Payment projection specification with new escrow fields
/// </summary>
public sealed class PaymentProjectionSpec : Specification<Payment, PaymentDetailsDto>
{
    public PaymentProjectionSpec(Guid id)
    {
        Query.Where(x => x.Id == id);
        Query.Include(x => x.Reply)
             .ThenInclude(r => r.Request)
             .ThenInclude(req => req.SenderUser);
        Query.Include(x => x.Reply)
             .ThenInclude(r => r.Request)
             .ThenInclude(req => req.ReceiverUser);
        
        Query.Select(x => new PaymentDetailsDto
        {
            Id = x.Id,
            ReplyId = x.ReplyId,
            ServiceAmount = x.ServiceAmount,           // ✅ NEW: Separate service amount
            ProtectionFee = x.ProtectionFee,          // ✅ NEW: Separate protection fee  
            TotalAmount = x.TotalAmount,              // ✅ NEW: Total amount
            Currency = x.Currency ?? "RON",
            Status = x.Status.ToString(),
            PaidAt = x.PaidAt,
            EscrowReleasedAt = x.EscrowReleasedAt,    // ✅ NEW: Escrow release timestamp
            CreatedAt = x.CreatedAt,
            StripePaymentIntentId = x.StripePaymentIntentId,
            StripeTransferId = x.StripeTransferId,    // ✅ NEW: Transfer ID
            StripeRefundId = x.StripeRefundId,        // ✅ NEW: Refund ID
            ServiceDescription = x.Reply.Request.Description,
            ServiceAddress = x.Reply.Request.Address,
            ServiceStartDate = x.Reply.StartDate,
            ServiceEndDate = x.Reply.EndDate,
            SpecialistName = x.Reply.Request.ReceiverUser.FullName,
            ClientName = x.Reply.Request.SenderUser.FullName,
            TransferredAmount = x.TransferredAmount,  // ✅ NEW: Amount transferred to specialist
            RefundedAmount = x.RefundedAmount,        // ✅ NEW: Amount refunded to client
            PlatformRevenue = x.FeeCollected ? x.ProtectionFee : 0, // ✅ NEW: Platform's revenue
            IsEscrowed = (x.Status == PaymentStatusEnum.Escrowed || x.Status == PaymentStatusEnum.Completed) &&
                        x.TransferredAmount == 0 && x.RefundedAmount < x.TotalAmount && x.PaidAt != null,
            ProtectionFeeDetails = null // Will be populated by helper if needed
        });
    }
    
    public PaymentProjectionSpec(string paymentIntentId)
    {
        Query.Where(p => p.StripePaymentIntentId == paymentIntentId);
        Query.Include(x => x.Reply)
             .ThenInclude(r => r.Request);
        
        Query.Select(x => new PaymentDetailsDto
        {
            Id = x.Id,
            ReplyId = x.ReplyId,
            ServiceAmount = x.ServiceAmount,
            ProtectionFee = x.ProtectionFee,
            TotalAmount = x.TotalAmount,
            Currency = x.Currency ?? "RON",
            Status = x.Status.ToString(),
            PaidAt = x.PaidAt,
            EscrowReleasedAt = x.EscrowReleasedAt,
            CreatedAt = x.CreatedAt,
            StripePaymentIntentId = x.StripePaymentIntentId,
            StripeTransferId = x.StripeTransferId,
            StripeRefundId = x.StripeRefundId,
            ServiceDescription = x.Reply.Request.Description,
            ServiceAddress = x.Reply.Request.Address,
            ServiceStartDate = x.Reply.StartDate,
            ServiceEndDate = x.Reply.EndDate,
            SpecialistName = x.Reply.Request.ReceiverUser.FullName,
            ClientName = x.Reply.Request.SenderUser.FullName,
            TransferredAmount = x.TransferredAmount,
            RefundedAmount = x.RefundedAmount,
            PlatformRevenue = x.FeeCollected ? x.ProtectionFee : 0,
            IsEscrowed = (x.Status == PaymentStatusEnum.Escrowed || x.Status == PaymentStatusEnum.Completed) &&
                        x.TransferredAmount == 0 && x.RefundedAmount < x.TotalAmount && x.PaidAt != null,
            ProtectionFeeDetails = null
        });
    }
    
    public PaymentProjectionSpec(Guid replyId, string? search)
    {
        Query.Where(x => x.ReplyId == replyId);
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            var searchExpr = $"%{search.Replace(" ", "%")}%";
            
            Query.Where(p =>
                EF.Functions.ILike(p.Reply.Request.Description, searchExpr) ||
                EF.Functions.ILike(p.Reply.Request.Address, searchExpr) ||
                EF.Functions.ILike(p.Status.ToString(), searchExpr) ||
                EF.Functions.ILike(p.TotalAmount.ToString(), searchExpr) || // ✅ UPDATED: Use TotalAmount
                EF.Functions.ILike(p.ServiceAmount.ToString(), searchExpr)   // ✅ NEW: Search service amount
            );
        }
        
        Query.Include(x => x.Reply)
             .ThenInclude(r => r.Request)
             .ThenInclude(req => req.SenderUser);
        Query.Include(x => x.Reply)
             .ThenInclude(r => r.Request)
             .ThenInclude(req => req.ReceiverUser);
        
        Query.Select(x => new PaymentDetailsDto
        {
            Id = x.Id,
            ReplyId = x.ReplyId,
            ServiceAmount = x.ServiceAmount,
            ProtectionFee = x.ProtectionFee,
            TotalAmount = x.TotalAmount,
            Currency = x.Currency ?? "RON",
            Status = x.Status.ToString(),
            PaidAt = x.PaidAt,
            EscrowReleasedAt = x.EscrowReleasedAt,
            CreatedAt = x.CreatedAt,
            StripePaymentIntentId = x.StripePaymentIntentId,
            StripeTransferId = x.StripeTransferId,
            StripeRefundId = x.StripeRefundId,
            ServiceDescription = x.Reply.Request.Description,
            ServiceAddress = x.Reply.Request.Address,
            ServiceStartDate = x.Reply.StartDate,
            ServiceEndDate = x.Reply.EndDate,
            SpecialistName = x.Reply.Request.ReceiverUser.FullName,
            ClientName = x.Reply.Request.SenderUser.FullName,
            TransferredAmount = x.TransferredAmount,
            RefundedAmount = x.RefundedAmount,
            PlatformRevenue = x.FeeCollected ? x.ProtectionFee : 0,
            IsEscrowed = (x.Status == PaymentStatusEnum.Escrowed || x.Status == PaymentStatusEnum.Completed) &&
                        x.TransferredAmount == 0 && x.RefundedAmount < x.TotalAmount && x.PaidAt != null,
            ProtectionFeeDetails = null
        });
    }
}

/// <summary>
/// ✅ UPDATED: Payment history projection with new escrow fields
/// </summary>
public sealed class PaymentHistoryProjectionSpec : Specification<Payment, PaymentHistoryDto>
{
    public PaymentHistoryProjectionSpec(Guid userId, string? search)
    {
        Query.Where(x => x.Reply.Request.SenderUserId == userId || x.Reply.Request.ReceiverUserId == userId);
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            var searchExpr = $"%{search.Replace(" ", "%")}%";
            
            Query.Where(p =>
                EF.Functions.ILike(p.Reply.Request.Description, searchExpr) ||
                EF.Functions.ILike(p.Reply.Request.Address, searchExpr) ||
                EF.Functions.ILike(p.Status.ToString(), searchExpr) ||
                EF.Functions.ILike(p.TotalAmount.ToString(), searchExpr) || // ✅ UPDATED: Use TotalAmount
                EF.Functions.ILike(p.ServiceAmount.ToString(), searchExpr)   // ✅ NEW: Search service amount
            );
        }
        
        Query.Include(x => x.Reply)
             .ThenInclude(r => r.Request)
             .ThenInclude(req => req.SenderUser);
        Query.Include(x => x.Reply)
             .ThenInclude(r => r.Request)
             .ThenInclude(req => req.ReceiverUser);
        
        Query.Select(x => new PaymentHistoryDto
        {
            Id = x.Id,
            ReplyId = x.ReplyId,
            ServiceAmount = x.ServiceAmount,           // ✅ NEW: Separate service amount
            ProtectionFee = x.ProtectionFee,          // ✅ NEW: Separate protection fee
            TotalAmount = x.TotalAmount,              // ✅ NEW: Total amount
            Currency = x.Currency ?? "RON",
            Status = x.Status.ToString(),
            PaidAt = x.PaidAt,
            EscrowReleasedAt = x.EscrowReleasedAt,    // ✅ NEW: Escrow release timestamp
            ServiceDescription = x.Reply.Request.Description,
            ServiceAddress = x.Reply.Request.Address,
            SpecialistName = x.Reply.Request.ReceiverUser.FullName,
            ClientName = x.Reply.Request.SenderUser.FullName,
            TransferredAmount = x.TransferredAmount,  // ✅ NEW: Amount transferred to specialist
            RefundedAmount = x.RefundedAmount,        // ✅ NEW: Amount refunded to client
            IsEscrowed = (x.Status == PaymentStatusEnum.Escrowed || x.Status == PaymentStatusEnum.Completed) &&
                        x.TransferredAmount == 0 && x.RefundedAmount < x.TotalAmount && x.PaidAt != null
        });
        
        // Order by most recent first
        Query.OrderByDescending(x => x.CreatedAt);
    }
}

/// <summary>
/// ✅ NEW: Payment report projection specification for efficient reporting
/// Projects payments into a lightweight DTO for analytics and reporting
/// </summary>
public sealed class PaymentReportProjectionSpec : Specification<Payment, PaymentReportItemDto>
{
    public PaymentReportProjectionSpec(DateTime fromDate, DateTime toDate)
    {
        Query.Where(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate);
        
        Query.Select(x => new PaymentReportItemDto
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            ServiceAmount = x.ServiceAmount,
            ProtectionFee = x.ProtectionFee,
            TotalAmount = x.TotalAmount,
            Status = x.Status,
            TransferredAmount = x.TransferredAmount,
            RefundedAmount = x.RefundedAmount,
            FeeCollected = x.FeeCollected,
            PaidAt = x.PaidAt,
            EscrowReleasedAt = x.EscrowReleasedAt,
            // Calculate if in escrow without complex logic in projection
            IsEscrowed = (x.Status == PaymentStatusEnum.Escrowed || x.Status == PaymentStatusEnum.Completed) &&
                        x.TransferredAmount == 0 && x.RefundedAmount < x.TotalAmount && x.PaidAt != null
        });
        
        Query.OrderBy(x => x.CreatedAt);
    }
}

/// <summary>
/// ✅ NEW: Simple payment specification for basic queries (used by PaymentService)
/// </summary>
public sealed class PaymentReportSpec : Specification<Payment>
{
    public PaymentReportSpec(DateTime fromDate, DateTime toDate)
    {
        Query.Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate);
        Query.OrderBy(p => p.CreatedAt);
    }
}