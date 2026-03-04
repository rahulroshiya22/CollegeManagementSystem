using CMS.AIService.Models;

namespace CMS.AIService.Services;

public static class FunctionDefinitions
{
    public static Tool GetAllFunctions()
    {
        return new Tool
        {
            FunctionDeclarations = new List<FunctionDeclaration>
            {
                // Student Functions
                GetStudentsFunction(),
                GetStudentByIdFunction(),
                CreateStudentFunction(),
                UpdateStudentFunction(),
                DeleteStudentFunction(),
                
                // Course Functions
                GetCoursesFunction(),
                GetCourseByIdFunction(),
                CreateCourseFunction(),
                UpdateCourseFunction(),
                DeleteCourseFunction(),
                
                // Enrollment Functions
                GetEnrollmentsFunction(),
                EnrollStudentFunction(),
                DropEnrollmentFunction(),
                
                // Fee Functions
                GetFeesFunction(),
                GetPendingFeesFunction(),
                RecordPaymentFunction(),
                CreateFeeFunction(),
                
                // Attendance Functions
                GetAttendanceFunction(),
                MarkAttendanceFunction()
            }
        };
    }

    // ======================== STUDENT FUNCTIONS ========================
    private static FunctionDeclaration GetStudentsFunction()
    {
        return new FunctionDeclaration
        {
            Name = "get_students",
            Description = "Get all students or search students. Use this when the user asks about students, how many students, show students, list students, etc.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["search"] = new PropertyDefinition { Type = "string", Description = "Optional search query for student name or roll number" },
                    ["departmentId"] = new PropertyDefinition { Type = "integer", Description = "Optional filter by department ID" }
                }
            }
        };
    }

    private static FunctionDeclaration GetStudentByIdFunction()
    {
        return new FunctionDeclaration
        {
            Name = "get_student_by_id",
            Description = "Get a specific student by their ID. Use when user asks about a particular student.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["studentId"] = new PropertyDefinition { Type = "integer", Description = "The ID of the student" }
                },
                Required = new List<string> { "studentId" }
            }
        };
    }

    private static FunctionDeclaration CreateStudentFunction()
    {
        return new FunctionDeclaration
        {
            Name = "create_student",
            Description = "Create a new student. Use when user wants to add or register a new student.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["firstName"] = new PropertyDefinition { Type = "string", Description = "Student's first name" },
                    ["lastName"] = new PropertyDefinition { Type = "string", Description = "Student's last name" },
                    ["email"] = new PropertyDefinition { Type = "string", Description = "Student's email address" },
                    ["phone"] = new PropertyDefinition { Type = "string", Description = "Student's phone number" },
                    ["rollNumber"] = new PropertyDefinition { Type = "string", Description = "Student's roll number" },
                    ["dateOfBirth"] = new PropertyDefinition { Type = "string", Description = "Date of birth in YYYY-MM-DD format" },
                    ["gender"] = new PropertyDefinition { Type = "string", Description = "Gender (Male/Female/Other)" },
                    ["address"] = new PropertyDefinition { Type = "string", Description = "Student's address" },
                    ["departmentId"] = new PropertyDefinition { Type = "integer", Description = "Department ID" },
                    ["admissionYear"] = new PropertyDefinition { Type = "integer", Description = "Year of admission" }
                },
                Required = new List<string> { "firstName", "lastName", "email", "rollNumber", "dateOfBirth", "gender", "departmentId", "admissionYear" }
            }
        };
    }

    private static FunctionDeclaration UpdateStudentFunction()
    {
        return new FunctionDeclaration
        {
            Name = "update_student",
            Description = "Update an existing student's information. Use when user wants to modify or change student details.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["studentId"] = new PropertyDefinition { Type = "integer", Description = "The ID of the student to update" },
                    ["firstName"] = new PropertyDefinition { Type = "string", Description = "Updated first name" },
                    ["lastName"] = new PropertyDefinition { Type = "string", Description = "Updated last name" },
                    ["email"] = new PropertyDefinition { Type = "string", Description = "Updated email address" },
                    ["phone"] = new PropertyDefinition { Type = "string", Description = "Updated phone number" },
                    ["address"] = new PropertyDefinition { Type = "string", Description = "Updated address" }
                },
                Required = new List<string> { "studentId" }
            }
        };
    }

    private static FunctionDeclaration DeleteStudentFunction()
    {
        return new FunctionDeclaration
        {
            Name = "delete_student",
            Description = "Delete a student from the system. Use when user wants to remove or delete a student. THIS IS A DESTRUCTIVE ACTION.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["studentId"] = new PropertyDefinition { Type = "integer", Description = "The ID of the student to delete" }
                },
                Required = new List<string> { "studentId" }
            }
        };
    }

    // ======================== COURSE FUNCTIONS ========================
    private static FunctionDeclaration GetCoursesFunction()
    {
        return new FunctionDeclaration
        {
            Name = "get_courses",
            Description = "Get all courses or filter courses. Use when user asks about courses, list courses, show courses, etc.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["departmentId"] = new PropertyDefinition { Type = "integer", Description = "Optional filter by department ID" },
                    ["semester"] = new PropertyDefinition { Type = "integer", Description = "Optional filter by semester number" }
                }
            }
        };
    }

    private static FunctionDeclaration GetCourseByIdFunction()
    {
        return new FunctionDeclaration
        {
            Name = "get_course_by_id",
            Description = "Get a specific course by its ID.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["courseId"] = new PropertyDefinition { Type = "integer", Description = "The ID of the course" }
                },
                Required = new List<string> { "courseId" }
            }
        };
    }

    private static FunctionDeclaration CreateCourseFunction()
    {
        return new FunctionDeclaration
        {
            Name = "create_course",
            Description = "Create a new course. Use when user wants to add a new course.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["courseCode"] = new PropertyDefinition { Type = "string", Description = "Course code (e.g., CS101)" },
                    ["courseName"] = new PropertyDefinition { Type = "string", Description = "Name of the course" },
                    ["description"] = new PropertyDefinition { Type = "string", Description = "Course description" },
                    ["credits"] = new PropertyDefinition { Type = "integer", Description = "Number of credits" },
                    ["semester"] = new PropertyDefinition { Type = "integer", Description = "Semester number (1-8)" },
                    ["departmentId"] = new PropertyDefinition { Type = "integer", Description = "Department ID" }
                },
                Required = new List<string> { "courseCode", "courseName", "credits", "semester", "departmentId" }
            }
        };
    }

    private static FunctionDeclaration UpdateCourseFunction()
    {
        return new FunctionDeclaration
        {
            Name = "update_course",
            Description = "Update an existing course. Use when user wants to modify course details.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["courseId"] = new PropertyDefinition { Type = "integer", Description = "The ID of the course to update" },
                    ["courseCode"] = new PropertyDefinition { Type = "string", Description = "Updated course code" },
                    ["courseName"] = new PropertyDefinition { Type = "string", Description = "Updated course name" },
                    ["description"] = new PropertyDefinition { Type = "string", Description = "Updated description" },
                    ["credits"] = new PropertyDefinition { Type = "integer", Description = "Updated credits" }
                },
                Required = new List<string> { "courseId" }
            }
        };
    }

    private static FunctionDeclaration DeleteCourseFunction()
    {
        return new FunctionDeclaration
        {
            Name = "delete_course",
            Description = "Delete a course from the system. THIS IS A DESTRUCTIVE ACTION.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["courseId"] = new PropertyDefinition { Type = "integer", Description = "The ID of the course to delete" }
                },
                Required = new List<string> { "courseId" }
            }
        };
    }

    // ======================== ENROLLMENT FUNCTIONS ========================
    private static FunctionDeclaration GetEnrollmentsFunction()
    {
        return new FunctionDeclaration
        {
            Name = "get_enrollments",
            Description = "Get enrollments, optionally filtered by student or course.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["studentId"] = new PropertyDefinition { Type = "integer", Description = "Optional filter by student ID" },
                    ["courseId"] = new PropertyDefinition { Type = "integer", Description = "Optional filter by course ID" }
                }
            }
        };
    }

    private static FunctionDeclaration EnrollStudentFunction()
    {
        return new FunctionDeclaration
        {
            Name = "enroll_student",
            Description = "Enroll a student in a course. Use when user wants to register a student for a course.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["studentId"] = new PropertyDefinition { Type = "integer", Description = "The student ID" },
                    ["courseId"] = new PropertyDefinition { Type = "integer", Description = "The course ID" },
                    ["semester"] = new PropertyDefinition { Type = "string", Description = "Enrollment semester (e.g., 'Fall 2024')" }
                },
                Required = new List<string> { "studentId", "courseId", "semester" }
            }
        };
    }

    private static FunctionDeclaration DropEnrollmentFunction()
    {
        return new FunctionDeclaration
        {
            Name = "drop_enrollment",
            Description = "Drop a student from a course. Use when user wants to unenroll or remove a student from a course.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["enrollmentId"] = new PropertyDefinition { Type = "integer", Description = "The enrollment ID to drop" }
                },
                Required = new List<string> { "enrollmentId" }
            }
        };
    }

    // ======================== FEE FUNCTIONS ========================
    private static FunctionDeclaration GetFeesFunction()
    {
        return new FunctionDeclaration
        {
            Name = "get_fees",
            Description = "Get all fee records, optionally filtered by student.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["studentId"] = new PropertyDefinition { Type = "integer", Description = "Optional filter by student ID" }
                }
            }
        };
    }

    private static FunctionDeclaration GetPendingFeesFunction()
    {
        return new FunctionDeclaration
        {
            Name = "get_pending_fees",
            Description = "Get all pending (unpaid) fees. Use when user asks about pending payments or outstanding fees.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>()
            }
        };
    }

    private static FunctionDeclaration RecordPaymentFunction()
    {
        return new FunctionDeclaration
        {
            Name = "record_payment",
            Description = "Record a fee payment. Use when user wants to mark a fee as paid.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["feeId"] = new PropertyDefinition { Type = "integer", Description = "The fee ID to mark as paid" }
                },
                Required = new List<string> { "feeId" }
            }
        };
    }

    private static FunctionDeclaration CreateFeeFunction()
    {
        return new FunctionDeclaration
        {
            Name = "create_fee",
            Description = "Create a new fee record for a student.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["studentId"] = new PropertyDefinition { Type = "integer", Description = "The student ID" },
                    ["amount"] = new PropertyDefinition { Type = "number", Description = "Fee amount" },
                    ["feeType"] = new PropertyDefinition { Type = "string", Description = "Type of fee (e.g., 'Tuition', 'Library')" },
                    ["dueDate"] = new PropertyDefinition { Type = "string", Description = "Due date in YYYY-MM-DD format" }
                },
                Required = new List<string> { "studentId", "amount", "feeType", "dueDate" }
            }
        };
    }

    // ======================== ATTENDANCE FUNCTIONS ========================
    private static FunctionDeclaration GetAttendanceFunction()
    {
        return new FunctionDeclaration
        {
            Name = "get_attendance",
            Description = "Get attendance records, optionally filtered by student or course.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["studentId"] = new PropertyDefinition { Type = "integer", Description = "Optional filter by student ID" },
                    ["courseId"] = new PropertyDefinition { Type = "integer", Description = "Optional filter by course ID" }
                }
            }
        };
    }

    private static FunctionDeclaration MarkAttendanceFunction()
    {
        return new FunctionDeclaration
        {
            Name = "mark_attendance",
            Description = "Mark attendance for a student in a course.",
            Parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, PropertyDefinition>
                {
                    ["studentId"] = new PropertyDefinition { Type = "integer", Description = "The student ID" },
                    ["courseId"] = new PropertyDefinition { Type = "integer", Description = "The course ID" },
                    ["date"] = new PropertyDefinition { Type = "string", Description = "Date in YYYY-MM-DD format" },
                    ["isPresent"] = new PropertyDefinition { Type = "boolean", Description = "Whether student is present (true) or absent (false)" }
                },
                Required = new List<string> { "studentId", "courseId", "date", "isPresent" }
            }
        };
    }
}
