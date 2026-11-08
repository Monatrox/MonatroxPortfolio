namespace MonatroxPortfolio.Contact.Public;

public sealed record ContactRequestDto(string Name, string Email, string Message, string Website = "");
public sealed record ContactDeliveryInfoDto(bool Available, bool CapturedLocally);
public sealed record ContactReceiptDto(bool CapturedLocally);
