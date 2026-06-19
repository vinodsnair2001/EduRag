using EduRAG.API.Extensions;
using EduRAG.Application.DTOs;
using EduRAG.Application.Interfaces;
using EduRAG.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduRAG.API.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ManageClassUseCase    _classes;
    private readonly ManageSubjectUseCase  _subjects;
    private readonly ManageChapterUseCase  _chapters;
    private readonly ManageStudentUseCase  _students;
    private readonly UploadMaterialUseCase _upload;
    private readonly IClassQueries               _classQ;
    private readonly ISubjectQueries             _subjectQ;
    private readonly IChapterQueries             _chapterQ;
    private readonly IMaterialQueries            _materialQ;
    private readonly IUserQueries                _userQ;
    private readonly IStudentPermissionQueries   _permQ;

    public AdminController(
        ManageClassUseCase classes, ManageSubjectUseCase subjects,
        ManageChapterUseCase chapters, ManageStudentUseCase students,
        UploadMaterialUseCase upload,
        IClassQueries classQ, ISubjectQueries subjectQ,
        IChapterQueries chapterQ, IMaterialQueries materialQ,
        IUserQueries userQ, IStudentPermissionQueries permQ)
    {
        _classes   = classes;   _subjects  = subjects;
        _chapters  = chapters;  _students  = students;
        _upload    = upload;
        _classQ    = classQ;    _subjectQ  = subjectQ;
        _chapterQ  = chapterQ;  _materialQ = materialQ;
        _userQ     = userQ;     _permQ     = permQ;
    }

    // ── Classes ────────────────────────────────────────────────────────────
    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
        => Ok(await _classQ.GetAllActiveAsync());

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest req)
    {
        var r = await _classes.CreateAsync(req);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(new { message = r.Error });
    }

    [HttpPut("classes/{id:int}")]
    public async Task<IActionResult> UpdateClass(int id, [FromBody] UpdateClassRequest req)
    {
        var r = await _classes.UpdateAsync(id, req);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(new { message = r.Error });
    }

    [HttpDelete("classes/{id:int}")]
    public async Task<IActionResult> DeleteClass(int id)
    {
        var r = await _classes.DeleteAsync(id);
        return r.IsSuccess ? NoContent() : NotFound(new { message = r.Error });
    }

    // ── Subjects ───────────────────────────────────────────────────────────
    [HttpGet("classes/{classId:int}/subjects")]
    public async Task<IActionResult> GetSubjects(int classId)
        => Ok(await _subjectQ.GetByClassIdAsync(classId));

    [HttpPost("classes/{classId:int}/subjects")]
    public async Task<IActionResult> CreateSubject(int classId, [FromBody] CreateSubjectRequest req)
    {
        var r = await _subjects.CreateAsync(classId, req);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(new { message = r.Error });
    }

    [HttpPut("subjects/{id:int}")]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpdateSubjectRequest req)
    {
        var r = await _subjects.UpdateAsync(id, req);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(new { message = r.Error });
    }

    [HttpDelete("subjects/{id:int}")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var r = await _subjects.DeleteAsync(id);
        return r.IsSuccess ? NoContent() : NotFound(new { message = r.Error });
    }

    // ── Chapters ───────────────────────────────────────────────────────────
    [HttpGet("subjects/{subjectId:int}/chapters")]
    public async Task<IActionResult> GetChapters(int subjectId)
        => Ok(await _chapterQ.GetBySubjectIdAsync(subjectId));

    [HttpPost("subjects/{subjectId:int}/chapters")]
    public async Task<IActionResult> CreateChapter(int subjectId, [FromBody] CreateChapterRequest req)
    {
        var r = await _chapters.CreateAsync(subjectId, req);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(new { message = r.Error });
    }

    [HttpPut("chapters/{id:int}")]
    public async Task<IActionResult> UpdateChapter(int id, [FromBody] UpdateChapterRequest req)
    {
        var r = await _chapters.UpdateAsync(id, req);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(new { message = r.Error });
    }

    [HttpDelete("chapters/{id:int}")]
    public async Task<IActionResult> DeleteChapter(int id)
    {
        var r = await _chapters.DeleteAsync(id);
        return r.IsSuccess ? NoContent() : NotFound(new { message = r.Error });
    }

    // ── Materials ──────────────────────────────────────────────────────────
    [HttpGet("materials")]
    public async Task<IActionResult> GetMaterials()
        => Ok(await _materialQ.GetAllAsync());

    [HttpPost("upload"), DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(
        IFormFile file, [FromForm] int classId, [FromForm] int subjectId, [FromForm] int? chapterId)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var userId = User.GetUserId();
        await using var stream = file.OpenReadStream();
        var r = await _upload.ExecuteAsync(stream, file.FileName, file.Length, classId, subjectId, chapterId, userId);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(new { message = r.Error });
    }

    [HttpDelete("materials/{id:guid}")]
    public async Task<IActionResult> DeleteMaterial(Guid id)
    {
        var r = await _upload.DeleteAsync(id);
        return r.IsSuccess ? NoContent() : NotFound(new { message = r.Error });
    }

    // ── Users ──────────────────────────────────────────────────────────────
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
        => Ok(await _userQ.GetAllAsync());

    // ── Students ───────────────────────────────────────────────────────────
    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest req)
    {
        var r = await _students.CreateStudentAsync(req);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(new { message = r.Error });
    }

    [HttpPut("students/{studentId:guid}")]
    public async Task<IActionResult> UpdateStudent(Guid studentId, [FromBody] UpdateStudentRequest req)
    {
        var r = await _students.UpdateStudentAsync(studentId, req);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(new { message = r.Error });
    }

    [HttpDelete("students/{studentId:guid}")]
    public async Task<IActionResult> DeactivateStudent(Guid studentId)
    {
        var r = await _students.DeactivateStudentAsync(studentId);
        return r.IsSuccess ? NoContent() : BadRequest(new { message = r.Error });
    }

    [HttpGet("students/{studentId:guid}/permissions")]
    public async Task<IActionResult> GetStudentPermissions(Guid studentId)
        => Ok(await _permQ.GetByStudentIdAsync(studentId));

    [HttpPut("students/{studentId:guid}/permissions")]
    public async Task<IActionResult> SetStudentPermissions(
        Guid studentId, [FromBody] SetStudentPermissionsRequest req)
    {
        var r = await _students.SetPermissionsAsync(studentId, req);
        return r.IsSuccess ? NoContent() : BadRequest(new { message = r.Error });
    }
}
