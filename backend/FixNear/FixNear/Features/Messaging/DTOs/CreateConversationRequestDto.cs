using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Messaging.DTOs;

public sealed class CreateConversationRequestDto
{
    [Range(1, int.MaxValue)]
    public int RepairRequestId { get; set; }
}
