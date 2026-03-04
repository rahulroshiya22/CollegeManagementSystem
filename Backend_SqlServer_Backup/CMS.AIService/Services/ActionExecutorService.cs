using System.Text.Json;
using CMS.AIService.Models;

namespace CMS.AIService.Services;

public class ActionExecutorService
{
    private readonly ServiceIntegrationService _serviceIntegration;
    private readonly ILogger<ActionExecutorService> _logger;
    private static readonly Dictionary<string, PendingAction> _pendingActions = new();

    public ActionExecutorService(
        ServiceIntegrationService serviceIntegration,
        ILogger<ActionExecutorService> logger)
    {
        _serviceIntegration = serviceIntegration;
        _logger = logger;
    }

    public async Task<(string result, bool requiresConfirmation, string? actionId)> ExecuteFunctionAsync(
        FunctionCall functionCall,
        string userId)
    {
        _logger.LogInformation("Executing function: {FunctionName}", functionCall.Name);

        // Check if function requires confirmation
        if (RequiresConfirmation(functionCall.Name))
        {
            var actionId = Guid.NewGuid().ToString();
            var description = await GenerateActionDescription(functionCall);
            
            var pendingAction = new PendingAction
            {
                ActionId = actionId,
                UserId = userId,
                FunctionName = functionCall.Name,
                Arguments = functionCall.Args,
                ActionType = GetActionType(functionCall.Name),
                Description = description
            };

            _pendingActions[actionId] = pendingAction;
            
            return (description, true, actionId);
        }

        // Execute immediately if no confirmation needed
        return (await ExecuteActionAsync(functionCall), false, null);
    }

    public async Task<string> ConfirmAndExecuteAsync(string actionId)
    {
        if (!_pendingActions.TryGetValue(actionId, out var action))
        {
            throw new KeyNotFoundException($"No pending action found with ID: {actionId}");
        }

        var functionCall = new FunctionCall
        {
            Name = action.FunctionName,
            Args = action.Arguments
        };

        var result = await ExecuteActionAsync(functionCall);
        _pendingActions.Remove(actionId);
        
        return result;
    }

    public void CancelAction(string actionId)
    {
        _pendingActions.Remove(actionId);
    }

    private async Task<string> ExecuteActionAsync(FunctionCall functionCall)
    {
        try
        {
            return functionCall.Name switch
            {
                // Student Functions
                "get_students" => await GetStudentsAsync(functionCall.Args),
                "get_student_by_id" => await GetStudentByIdAsync(functionCall.Args),
                "create_student" => await CreateStudentAsync(functionCall.Args),
                "update_student" => await UpdateStudentAsync(functionCall.Args),
                "delete_student" => await DeleteStudentAsync(functionCall.Args),

                // Course Functions
                "get_courses" => await GetCoursesAsync(functionCall.Args),
                "get_course_by_id" => await GetCourseByIdAsync(functionCall.Args),
                "create_course" => await CreateCourseAsync(functionCall.Args),
                "update_course" => await UpdateCourseAsync(functionCall.Args),
                "delete_course" => await DeleteCourseAsync(functionCall.Args),

                // Enrollment Functions
                "get_enrollments" => await GetEnrollmentsAsync(functionCall.Args),
                "enroll_student" => await EnrollStudentAsync(functionCall.Args),
                "drop_enrollment" => await DropEnrollmentAsync(functionCall.Args),

                // Fee Functions
                "get_fees" => await GetFeesAsync(functionCall.Args),
                "get_pending_fees" => await GetPendingFeesAsync(),
                "record_payment" => await RecordPaymentAsync(functionCall.Args),
                "create_fee" => await CreateFeeAsync(functionCall.Args),

                // Attendance Functions
                "get_attendance" => await GetAttendanceAsync(functionCall.Args),
                "mark_attendance" => await MarkAttendanceAsync(functionCall.Args),

                _ => $"Unknown function: {functionCall.Name}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionCall.Name);
            return $"Error: {ex.Message}";
        }
    }

    private bool RequiresConfirmation(string functionName)
    {
        return functionName.Contains("delete") || functionName.Contains("update") || functionName.Contains("drop");
    }

    private string GetActionType(string functionName)
    {
        if (functionName.Contains("delete") || functionName.Contains("drop")) return "delete";
        if (functionName.Contains("update")) return "update";
        if (functionName.Contains("create") || functionName.Contains("enroll") || functionName.Contains("mark") || functionName.Contains("record")) return "create";
        return "read";
    }

    private async Task<string> GenerateActionDescription(FunctionCall functionCall)
    {
        var args = functionCall.Args;
        
        return functionCall.Name switch
        {
            "delete_student" => $"Delete Student ID {GetIntArg(args, "studentId")}",
            "update_student" => $"Update Student ID {GetIntArg(args, "studentId")}",
            "delete_course" => $"Delete Course ID {GetIntArg(args, "courseId")}",
            "update_course" => $"Update Course ID {GetIntArg(args, "courseId")}",
            "drop_enrollment" => $"Drop Enrollment ID {GetIntArg(args, "enrollmentId")}",
            _ => $"Execute {functionCall.Name}"
        };
    }

    // ======================= STUDENT OPERATIONS =======================
    private async Task<string> GetStudentsAsync(Dictionary<string, object> args)
    {
        var data = await _serviceIntegration.GetStudentDataAsync();
        return JsonSerializer.Serialize(new { 
            totalStudents = data.TotalStudents,
            students = data.StudentNames 
        });
    }

    private async Task<string> GetStudentByIdAsync(Dictionary<string, object> args)
    {
        var id = GetIntArg(args, "studentId");
        var result = await _serviceIntegration.GetStudentByIdAsync(id);
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> CreateStudentAsync(Dictionary<string, object> args)
    {
        var dto = new CreateStudentDto
        {
            FirstName = GetStringArg(args, "firstName"),
            LastName = GetStringArg(args, "lastName"),
            Email = GetStringArg(args, "email"),
            Phone = GetStringArg(args, "phone", ""),
            RollNumber = GetStringArg(args, "rollNumber"),
            DateOfBirth = DateTime.Parse(GetStringArg(args, "dateOfBirth")),
            Gender = GetStringArg(args, "gender"),
            Address = GetStringArg(args, "address", ""),
            DepartmentId = GetIntArg(args, "departmentId"),
            AdmissionYear = GetIntArg(args, "admissionYear")
        };

        var result = await _serviceIntegration.CreateStudentAsync(dto);
        return JsonSerializer.Serialize(new { success = true, message = "Student created successfully", studentId = result });
    }

    private async Task<string> UpdateStudentAsync(Dictionary<string, object> args)
    {
        var id = GetIntArg(args, "studentId");
        var dto = new UpdateStudentDto
        {
            FirstName = GetStringArg(args, "firstName", null),
            LastName = GetStringArg(args, "lastName", null),
            Email = GetStringArg(args, "email", null),
            Phone = GetStringArg(args, "phone", null),
            Address = GetStringArg(args, "address", null)
        };

        await _serviceIntegration.UpdateStudentAsync(id, dto);
        return JsonSerializer.Serialize(new { success = true, message = $"Student {id} updated successfully" });
    }

    private async Task<string> DeleteStudentAsync(Dictionary<string, object> args)
    {
        var id = GetIntArg(args, "studentId");
        await _serviceIntegration.DeleteStudentAsync(id);
        return JsonSerializer.Serialize(new { success = true, message = $"Student {id} deleted successfully" });
    }

    // ======================= COURSE OPERATIONS =======================
    private async Task<string> GetCoursesAsync(Dictionary<string, object> args)
    {
        var data = await _serviceIntegration.GetCourseDataAsync();
        return JsonSerializer.Serialize(new { 
            totalCourses = data.TotalCourses,
            courses = data.CourseNames 
        });
    }

    private async Task<string> GetCourseByIdAsync(Dictionary<string, object> args)
    {
        var id = GetIntArg(args, "courseId");
        var result = await _serviceIntegration.GetCourseByIdAsync(id);
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> CreateCourseAsync(Dictionary<string, object> args)
    {
        var dto = new CreateCourseDto
        {
            CourseCode = GetStringArg(args, "courseCode"),
            CourseName = GetStringArg(args, "courseName"),
            Description = GetStringArg(args, "description", ""),
            Credits = GetIntArg(args, "credits"),
            Semester = GetIntArg(args, "semester"),
            DepartmentId = GetIntArg(args, "departmentId"),
            IsActive = true
        };

        var result = await _serviceIntegration.CreateCourseAsync(dto);
        return JsonSerializer.Serialize(new { success = true, message = "Course created successfully", courseId = result });
    }

    private async Task<string> UpdateCourseAsync(Dictionary<string, object> args)
    {
        var id = GetIntArg(args, "courseId");
        var dto = new UpdateCourseDto
        {
            CourseId = id,
            CourseCode = GetStringArg(args, "courseCode", ""),
            CourseName = GetStringArg(args, "courseName", ""),
            Description = GetStringArg(args, "description", ""),
            Credits = GetIntArg(args, "credits", 0),
            Semester = GetIntArg(args, "semester", 1),
            DepartmentId = GetIntArg(args, "departmentId", 1),
            IsActive = true
        };

        await _serviceIntegration.UpdateCourseAsync(id, dto);
        return JsonSerializer.Serialize(new { success = true, message = $"Course {id} updated successfully" });
    }

    private async Task<string> DeleteCourseAsync(Dictionary<string, object> args)
    {
        var id = GetIntArg(args, "courseId");
        await _serviceIntegration.DeleteCourseAsync(id);
        return JsonSerializer.Serialize(new { success = true, message = $"Course {id} deleted successfully" });
    }

    // ======================= ENROLLMENT OPERATIONS =======================
    private async Task<string> GetEnrollmentsAsync(Dictionary<string, object> args)
    {
        var result = await _serviceIntegration.GetEnrollmentsAsync();
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> EnrollStudentAsync(Dictionary<string, object> args)
    {
        var dto = new CreateEnrollmentDto
        {
            StudentId = GetIntArg(args, "studentId"),
            CourseId = GetIntArg(args, "courseId"),
            Semester = GetStringArg(args, "semester")
        };

        var result = await _serviceIntegration.EnrollStudentAsync(dto);
        return JsonSerializer.Serialize(new { success = true, message = "Student enrolled successfully", enrollmentId = result });
    }

    private async Task<string> DropEnrollmentAsync(Dictionary<string, object> args)
    {
        var id = GetIntArg(args, "enrollmentId");
        await _serviceIntegration.DropEnrollmentAsync(id);
        return JsonSerializer.Serialize(new { success = true, message = $"Enrollment {id} dropped successfully" });
    }

    // ======================= FEE OPERATIONS =======================
    private async Task<string> GetFeesAsync(Dictionary<string, object> args)
    {
        var data = await _serviceIntegration.GetFeeDataAsync();
        return JsonSerializer.Serialize(data);
    }

    private async Task<string> GetPendingFeesAsync()
    {
        var data = await _serviceIntegration.GetFeeDataAsync();
        return JsonSerializer.Serialize(new {
            totalPending = data.TotalPending,
            studentsWithPending = data.StudentsWithPending
        });
    }

    private async Task<string> RecordPaymentAsync(Dictionary<string, object> args)
    {
        var feeId = GetIntArg(args, "feeId");
        await _serviceIntegration.RecordPaymentAsync(feeId);
        return JsonSerializer.Serialize(new { success = true, message = $"Payment recorded for Fee {feeId}" });
    }

    private async Task<string> CreateFeeAsync(Dictionary<string, object> args)
    {
        var dto = new CreateFeeDto
        {
            StudentId = GetIntArg(args, "studentId"),
            Amount = GetDecimalArg(args, "amount"),
            FeeType = GetStringArg(args, "feeType"),
            DueDate = DateTime.Parse(GetStringArg(args, "dueDate"))
        };

        var result = await _serviceIntegration.CreateFeeAsync(dto);
        return JsonSerializer.Serialize(new { success = true, message = "Fee created successfully", feeId = result });
    }

    // ======================= ATTENDANCE OPERATIONS =======================
    private async Task<string> GetAttendanceAsync(Dictionary<string, object> args)
    {
        var data = await _serviceIntegration.GetAttendanceDataAsync();
        return JsonSerializer.Serialize(data);
    }

    private async Task<string> MarkAttendanceAsync(Dictionary<string, object> args)
    {
        var dto = new MarkAttendanceDto
        {
            StudentId = GetIntArg(args, "studentId"),
            CourseId = GetIntArg(args, "courseId"),
            Date = DateTime.Parse(GetStringArg(args, "date")),
            IsPresent = GetBoolArg(args, "isPresent")
        };

        var result = await _serviceIntegration.MarkAttendanceAsync(dto);
        return JsonSerializer.Serialize(new { success = true, message = "Attendance marked successfully", attendanceId = result });
    }

    // ======================= HELPER METHODS =======================
    private string GetStringArg(Dictionary<string, object> args, string key, string? defaultValue = null)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement jsonElement)
                return jsonElement.GetString() ?? defaultValue ?? string.Empty;
            return value?.ToString() ?? defaultValue ?? string.Empty;
        }
        return defaultValue ?? string.Empty;
    }

    private int GetIntArg(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement jsonElement)
                return jsonElement.TryGetInt32(out var intValue) ? intValue : defaultValue;
            if (int.TryParse(value?.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private decimal GetDecimalArg(Dictionary<string, object> args, string key)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement jsonElement)
                return jsonElement.TryGetDecimal(out var decimalValue) ? decimalValue : 0;
            if (decimal.TryParse(value?.ToString(), out var result))
                return result;
        }
        return 0;
    }

    private bool GetBoolArg(Dictionary<string, object> args, string key)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement jsonElement)
                return jsonElement.GetBoolean();
            if (bool.TryParse(value?.ToString(), out var result))
                return result;
        }
        return false;
    }
}
