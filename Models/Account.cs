namespace trb_auth.Models;

public record Account(
    string Id,
    string Type,
    decimal Balance,
    string Currency,
    string ClientFullName,
    string ExternalClientId,
    long CreationDate,
    long? ClosingDate,
    bool IsClosed
);