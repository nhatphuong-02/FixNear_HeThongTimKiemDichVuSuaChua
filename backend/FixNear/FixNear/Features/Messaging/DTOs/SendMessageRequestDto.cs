
using System.ComponentModel.DataAnnotations;

namespace FixNear.Features.Messaging.DTOs;

public sealed class SendMessageRequestDto
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = "";
}
