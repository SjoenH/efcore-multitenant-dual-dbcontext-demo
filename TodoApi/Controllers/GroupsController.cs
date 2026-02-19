using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TodoApi.Dtos;
using TodoApi.Infrastructure;
using TodoApi.Services;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class GroupsController : ControllerBase
{
    private readonly IGroupService _groups;
    private readonly ICurrentUserAccessor _currentUser;

    public GroupsController(IGroupService groups, ICurrentUserAccessor currentUser)
    {
        _groups = groups;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupResponse>>> GetMyGroups()
    {
        var userId = _currentUser.GetRequiredUserId();
        return Ok(await _groups.GetMyGroups(userId));
    }

    [HttpPost]
    public async Task<ActionResult<CreateGroupResponse>> CreateGroup(CreateGroupRequest request)
    {
        var userId = _currentUser.GetRequiredUserId();
        var created = await _groups.CreateGroup(userId, request);
        return CreatedAtAction(nameof(GetMembers), new { groupId = created.Group.Id }, created);
    }

    [HttpGet("{groupId:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<GroupMemberResponse>>> GetMembers(Guid groupId)
    {
        var userId = _currentUser.GetRequiredUserId();
        var members = await _groups.GetMembers(userId, groupId);
        if (members.Count == 0)
        {
            // ambiguous (no members vs not a member), but good enough for demo
            return NotFound();
        }

        return Ok(members);
    }

    [HttpPost("{groupId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid groupId, AddGroupMemberRequest request)
    {
        var userId = _currentUser.GetRequiredUserId();
        var ok = await _groups.AddMember(userId, groupId, request.UserId);
        return ok ? NoContent() : NotFound();
    }
}
