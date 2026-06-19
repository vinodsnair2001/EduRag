using EduRAG.API.Extensions;
using EduRAG.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduRAG.API.Controllers;

[ApiController, Route("api/student"), Authorize(Roles = "Student")]
public class StudentController : ControllerBase
{
    private readonly IClassQueries               _classQ;
    private readonly ISubjectQueries             _subjectQ;
    private readonly IChapterQueries             _chapterQ;
    private readonly IStudentPermissionQueries   _permQ;

    public StudentController(
        IClassQueries classQ, ISubjectQueries subjectQ,
        IChapterQueries chapterQ, IStudentPermissionQueries permQ)
    {
        _classQ   = classQ;
        _subjectQ = subjectQ;
        _chapterQ = chapterQ;
        _permQ    = permQ;
    }

    [HttpGet("my-class")]
    public async Task<IActionResult> GetMyClass()
    {
        var studentId = User.GetUserId();
        var result = await _permQ.GetStudentClassAsync(studentId);
        return result is null ? NotFound(new { message = "No class assigned." }) : Ok(result);
    }

    [HttpGet("my-subjects")]
    public async Task<IActionResult> GetMySubjects()
    {
        var studentId = User.GetUserId();
        return Ok(await _permQ.GetPermittedSubjectsAsync(studentId));
    }

    [HttpGet("subjects/{subjectId:int}/chapters")]
    public async Task<IActionResult> GetChapters(int subjectId)
        => Ok(await _chapterQ.GetBySubjectIdAsync(subjectId));
}
