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
    private readonly IMaterialQueries            _materialQ;
    private readonly IFileStorageService         _fileStorage;

    public StudentController(
        ISubjectQueries subjectQ,
        IChapterQueries chapterQ,
        IStudentPermissionQueries permQ,
        IMaterialQueries materialQ,
        IFileStorageService fileStorage)
    {
        _subjectQ    = subjectQ;
        _chapterQ    = chapterQ;
        _permQ       = permQ;
        _materialQ   = materialQ;
        _fileStorage = fileStorage;
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

    [HttpGet("chapters/{chapterId:int}/pdf")]
    public async Task<IActionResult> GetChapterPdf(int chapterId)
    {
        var file = await _materialQ.GetFileByChapterIdAsync(chapterId);
        if (file is null)
            return NotFound(new { message = "No PDF uploaded for this chapter." });
        var stream = _fileStorage.OpenRead(file.StoredFilePath);
        return File(stream, "application/pdf", file.OriginalFileName);
    }
}
