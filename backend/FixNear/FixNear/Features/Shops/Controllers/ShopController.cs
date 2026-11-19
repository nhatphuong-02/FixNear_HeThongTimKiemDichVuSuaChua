
using FixNear.Features.Shops.DTOs;
using FixNear.Features.Shops.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FixNear.Features.Shops.Controllers;

[Route("api/shops")]
[ApiController]
[Authorize]
public class ShopController : ControllerBase
{
    private readonly IShopService _shopService;

    public ShopController(IShopService shopService)
    {
        _shopService = shopService;
    }

    // Lấy UserId từ JWT
    private int GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var id))
            throw new UnauthorizedAccessException("Không xác định được tài khoản đăng nhập.");

        return id;
    }

    // Xử lý exception dùng chung
    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    // Shop List
    [HttpGet("me")]
    public async Task<IActionResult> GetMyShops()
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _shopService.GetMyShopsAsync(GetCurrentUserId());

            return Ok(new
            {
                message = "Lấy danh sách Shop thành công.",
                data = result
            });
        });
    }

    // Shop profile
    [HttpGet("{shopId:int}/profile")]
    public async Task<IActionResult> GetProfile(int shopId)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _shopService.GetShopProfileAsync(GetCurrentUserId(), shopId);

            return Ok(new
            {
                message = "Lấy hồ sơ Shop thành công.",
                data = result
            });
        });
    }

    [HttpPut("{shopId:int}/profile")]
    public async Task<IActionResult> UpdateProfile(
        int shopId,
        [FromBody] UpdateShopProfileRequestDto request)
    {
        return await ExecuteAsync(async () =>
        {
            await _shopService.UpdateShopProfileAsync(GetCurrentUserId(), shopId, request);

            return Ok(new
            {
                message = "Cập nhật hồ sơ Shop thành công."
            });
        });
    }

    // Shop member
    [HttpGet("{shopId:int}/members")]
    public async Task<IActionResult> GetMembers(int shopId)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _shopService.GetShopMembersAsync(GetCurrentUserId(), shopId);

            return Ok(new
            {
                message = "Lấy danh sách thành viên Shop thành công.",
                data = result
            });
        });
    }

    [HttpGet("{shopId:int}/members/{shopMemberId:int}")]
    public async Task<IActionResult> GetMemberDetail(int shopId, int shopMemberId)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _shopService.GetShopMemberDetailAsync(GetCurrentUserId(), shopId, shopMemberId);

            return Ok(new
            {
                message = "Lấy thông tin thành viên thành công.",
                data = result
            });
        });
    }

    [HttpPut("{shopId:int}/members/{shopMemberId:int}")]
    public async Task<IActionResult> UpdateMember(int shopId, int shopMemberId, [FromBody] UpdateShopMemberRequestDto request)
    {
        return await ExecuteAsync(async () =>
        {
            await _shopService.UpdateShopMemberAsync(GetCurrentUserId(), shopId, shopMemberId, request);

            return Ok(new
            {
                message = "Cập nhật vai trò thành viên thành công."
            });
        });
    }

    [HttpDelete("{shopId:int}/members/{shopMemberId:int}")]
    public async Task<IActionResult> RemoveMember(int shopId, int shopMemberId)
    {
        return await ExecuteAsync(async () =>
        {
            await _shopService.RemoveShopMemberAsync(GetCurrentUserId(), shopId, shopMemberId);

            return Ok(new
            {
                message = "Loại bỏ thành viên khỏi Shop thành công."
            });
        });
    }

    // Shop technician

    [HttpGet("{shopId:int}/technicians")]
    public async Task<IActionResult> GetTechnicians(int shopId)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _shopService.GetShopTechniciansAsync(GetCurrentUserId(), shopId);

            return Ok(new
            {
                message = "Lấy danh sách kỹ thuật viên thành công.",
                data = result
            });
        });
    }

    [HttpGet("{shopId:int}/technicians/{technicianShopId:int}")]
    public async Task<IActionResult> GetTechnicianDetail(int shopId, int technicianShopId)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _shopService.GetShopTechnicianDetailAsync(GetCurrentUserId(), shopId, technicianShopId);

            return Ok(new
            {
                message = "Lấy thông tin kỹ thuật viên thành công.",
                data = result
            });
        });
    }

    [HttpDelete("{shopId:int}/technicians/{technicianShopId:int}")]
    public async Task<IActionResult> RemoveTechnician(int shopId, int technicianShopId)
    {
        return await ExecuteAsync(async () =>
        {
            await _shopService.RemoveTechnicianAsync(
                GetCurrentUserId(),
                shopId,
                technicianShopId);

            return Ok(new
            {
                message = "Loại bỏ kỹ thuật viên khỏi Shop thành công."
            });
        });
    }

    // Technician Invitation  
    [HttpPost("{shopId:int}/technician-invitations")]
    public async Task<IActionResult> InviteTechnician(int shopId, [FromBody] InviteTechnicianRequestDto request)
    {
        return await ExecuteAsync(async () =>
        {
            await _shopService.InviteTechnicianAsync(GetCurrentUserId(), shopId, request);

            return Ok(new
            {
                message = "Gửi lời mời hợp tác đến kỹ thuật viên thành công."
            });
        });
    }

    [HttpGet("{shopId:int}/technician-invitations")]
    public async Task<IActionResult> GetTechnicianInvitations(int shopId)
    {
        return await ExecuteAsync(async () =>
        {
            var result = await _shopService.GetTechnicianInvitationsAsync(GetCurrentUserId(), shopId);

            return Ok(new
            {
                message = "Lấy danh sách lời mời Technician thành công.",
                data = result
            });
        });
    }

    // DELETE: api/shops/{shopId}/technician-invitations/{invitationId}
    [HttpDelete("{shopId:int}/technician-invitations/{invitationId:int}")]
    public async Task<IActionResult> CancelTechnicianInvitation(int shopId, int invitationId)
    {
        return await ExecuteAsync(async () =>
        {
            await _shopService.CancelTechnicianInvitationAsync(GetCurrentUserId(), shopId, invitationId);

            return Ok(new
            {
                message = "Hủy lời mời Technician thành công."
            });
        });
    }
}
