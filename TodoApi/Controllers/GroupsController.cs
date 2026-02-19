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
    private readonly IListService _lists;

    public GroupsController(IGroupService groups, IListService lists, ICurrentUserAccessor currentUser)
    {
        _groups = groups;
        _lists = lists;
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

    [HttpGet("{groupId:guid}/lists")]
    public async Task<ActionResult<IReadOnlyList<TodoListResponse>>> GetGroupLists(Guid groupId)
    {
        var userId = _currentUser.GetRequiredUserId();
        var lists = await _lists.GetGroupLists(userId, groupId);
        if (lists.Count == 0)
        {
            return NotFound();
        }

        return Ok(lists);
    }

    [HttpPost("{groupId:guid}/lists")]
    public async Task<ActionResult<TodoListResponse>> CreateGroupList(Guid groupId, CreateGroupListRequest request)
    {
        var userId = _currentUser.GetRequiredUserId();
        var list = await _lists.CreateGroupList(userId, groupId, request);
        return CreatedAtAction(nameof(GetGroupLists), new { groupId }, list);
    }

    [HttpPut("{groupId:guid}/lists/{listId:guid}/assign")]
    public async Task<IActionResult> AssignGroupList(Guid groupId, Guid listId, AssignListRequest request)
    {
        var userId = _currentUser.GetRequiredUserId();
        var ok = await _lists.AssignGroupList(userId, groupId, listId, request.AssignedUserId);
        return ok ? NoContent() : NotFound();
    }
}
