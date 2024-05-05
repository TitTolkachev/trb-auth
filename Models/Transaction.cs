namespace trb_auth.Models;

public record Transaction(
    string Id,
    string ExternalId,
    long Date,
    string? PayerAccountId,
    string? PayeeAccountId,
    double Amount,
    string Currency,
    string Type
);