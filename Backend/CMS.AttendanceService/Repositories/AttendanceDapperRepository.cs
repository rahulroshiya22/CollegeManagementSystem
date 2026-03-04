using CMS.AttendanceService.Models;
using Dapper;
using Npgsql;
using System.Data;

namespace CMS.AttendanceService.Repositories
{
    public interface IAttendanceDapperRepository
    {
        Task<IEnumerable<Attendance>> GetAllAsync();
        Task<Attendance?> GetByIdAsync(int id);
        Task<IEnumerable<Attendance>> GetByStudentIdAsync(int studentId);
        Task<IEnumerable<Attendance>> GetByCourseIdAsync(int courseId);
        Task<int> CreateAsync(Attendance attendance);
        Task<bool> UpdateAsync(Attendance attendance);
        Task<bool> DeleteAsync(int id);
        Task<int> BulkInsertAsync(IEnumerable<Attendance> attendances);
    }

    public class AttendanceDapperRepository : IAttendanceDapperRepository
    {
        private readonly string _connectionString;

        public AttendanceDapperRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<Attendance>> GetAllAsync()
        {
            using var connection = GetConnection();
            const string sql = @"SELECT ""AttendanceId"", ""StudentId"", ""CourseId"", ""Date"", ""IsPresent"", ""Remarks"" FROM ""Attendances""";
            return await connection.QueryAsync<Attendance>(sql);
        }

        public async Task<Attendance?> GetByIdAsync(int id)
        {
            using var connection = GetConnection();
            const string sql = @"SELECT ""AttendanceId"", ""StudentId"", ""CourseId"", ""Date"", ""IsPresent"", ""Remarks"" FROM ""Attendances"" WHERE ""AttendanceId"" = @Id";
            return await connection.QueryFirstOrDefaultAsync<Attendance>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Attendance>> GetByStudentIdAsync(int studentId)
        {
            using var connection = GetConnection();
            const string sql = @"SELECT ""AttendanceId"", ""StudentId"", ""CourseId"", ""Date"", ""IsPresent"", ""Remarks"" FROM ""Attendances"" WHERE ""StudentId"" = @StudentId";
            return await connection.QueryAsync<Attendance>(sql, new { StudentId = studentId });
        }

        public async Task<IEnumerable<Attendance>> GetByCourseIdAsync(int courseId)
        {
            using var connection = GetConnection();
            const string sql = @"SELECT ""AttendanceId"", ""StudentId"", ""CourseId"", ""Date"", ""IsPresent"", ""Remarks"" FROM ""Attendances"" WHERE ""CourseId"" = @CourseId";
            return await connection.QueryAsync<Attendance>(sql, new { CourseId = courseId });
        }

        public async Task<int> CreateAsync(Attendance attendance)
        {
            using var connection = GetConnection();
            const string sql = @"
                INSERT INTO ""Attendances"" (""StudentId"", ""CourseId"", ""Date"", ""IsPresent"", ""Remarks"")
                VALUES (@StudentId, @CourseId, @Date, @IsPresent, @Remarks)
                RETURNING ""AttendanceId""";
            return await connection.ExecuteScalarAsync<int>(sql, attendance);
        }

        public async Task<bool> UpdateAsync(Attendance attendance)
        {
            using var connection = GetConnection();
            const string sql = @"
                UPDATE ""Attendances"" 
                SET ""StudentId"" = @StudentId, ""CourseId"" = @CourseId, ""Date"" = @Date, 
                    ""IsPresent"" = @IsPresent, ""Remarks"" = @Remarks
                WHERE ""AttendanceId"" = @AttendanceId";
            var affected = await connection.ExecuteAsync(sql, attendance);
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            const string sql = @"DELETE FROM ""Attendances"" WHERE ""AttendanceId"" = @Id";
            var affected = await connection.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        /// <summary>
        /// Bulk insert using Dapper batch for PostgreSQL
        /// </summary>
        public async Task<int> BulkInsertAsync(IEnumerable<Attendance> attendances)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO ""Attendances"" (""StudentId"", ""CourseId"", ""Date"", ""IsPresent"", ""Remarks"")
                VALUES (@StudentId, @CourseId, @Date, @IsPresent, @Remarks)";

            var count = await connection.ExecuteAsync(sql, attendances);
            return count;
        }
    }
}
