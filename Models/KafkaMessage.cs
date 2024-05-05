namespace trb_auth.Models;

public record KafkaMessage(
    string State,
    Transaction? Transaction
);