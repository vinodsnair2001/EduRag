using EduRAG.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduRAG.API.Controllers;

[ApiController, Route("api/student"), Authorize(Roles = "Student")]
public class StudentController : ControllerBase
{
    private readonly IClassQueries   _classQ;
    private readonly ISubjectQueries _subjectQ;
    private readonly IChapterQueries _chapterQ;

    public StudentController(IClassQueries classQ, ISubjectQueries subjectQ, IChapterQueries chapterQ)
    {
        _classQ   = classQ;
        _subjectQ = subjectQ;
        _chapterQ = chapterQ;
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
        => Ok(await _classQ.GetAllActiveAsync());

    [HttpGet("classes/{classId:int}/subjects")]
    public async Task<IActionResult> GetSubjects(int classId)
        => Ok(await _subjectQ.GetByClassIdAsync(classId));

    [HttpGet("subjects/{subjectId:int}/chapters")]
    public async Task<IActionResult> GetChapters(int subjectId)
        => Ok(await _chapterQ.GetBySubjectIdAsync(subjectId));
}
