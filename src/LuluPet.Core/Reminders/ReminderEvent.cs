namespace LuluPet.Core.Reminders;

public sealed record ReminderEvent(
    ReminderKind Kind,
    string Message,
    string InteractionType,
    string PreferredAction);
