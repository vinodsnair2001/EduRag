using EduRAG.API.Extensions;
using EduRAG.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduRAG.API.Controllers;

[ApiController, Route("api/student"), Authorize(Roles = "Student")]
public class StudentController : ControllerBase
{
    private readonly ISubjectQueries             _subjectQ;
    private readonly IChapterQueries             _chapterQ;
    private readonly IStudentPermissionQueries   _permQ;

    public StudentController(
        ISubjectQueries subjectQ,
        IChapterQueries chapterQ,
        IStudentPermissionQueries permQ)
    {
        _subjectQ = subjectQ;
        _chapterQ = chapterQ;
        _permQ    = permQ;
    }

    [HttpGet("my-class")]
    public async Task<IActionResult> GetMyClass()
    {
        var result = await _permQ.GetStudentClassAsync(User.GetUserId());
        return result is null ? NotFound(new { message = "No class assigned." }) : Ok(result);
    }

    [HttpGet("classes/{classId:int}/subjects")]
    public async Task<IActionResult> GetSubjects(int classId)
        => Ok(await _subjectQ.GetByClassIdForStudentAsync(classId, User.GetUserId()));

    [HttpGet("subjects/{subjectId:int}/chapters")]
    public async Task<IActionResult> GetChapters(int subjectId)
        => Ok(await _chapterQ.GetBySubjectIdAsync(subjectId));
}
