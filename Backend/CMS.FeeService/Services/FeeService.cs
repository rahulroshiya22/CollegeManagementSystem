using CMS.FeeService.Data;
using CMS.FeeService.DTOs;
using CMS.FeeService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.FeeService.Services
{
    public interface IFeeService
    {
        Task<IEnumerable<Fee>> GetAllAsync();
        Task<Fee?> GetByIdAsync(int id);
        Task<IEnumerable<Fee>> GetByStudentAsync(int studentId);
        Task<Fee> CreateAsync(CreateFeeDto dto);
        Task<Fee?> UpdateAsync(int id, UpdateFeeDto dto);
        Task<Fee?> PayFeeAsync(int id, PayFeeDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class FeeManagementService : IFeeService
    {
        private readonly FeeDbContext _context;

        public FeeManagementService(FeeDbContext context) => _context = context;

        public async Task<IEnumerable<Fee>> GetAllAsync() =>
            await _context.Fees.OrderByDescending(f => f.CreatedAt).ToListAsync();

        public async Task<Fee?> GetByIdAsync(int id) =>
            await _context.Fees.FindAsync(id);

        public async Task<IEnumerable<Fee>> GetByStudentAsync(int studentId) =>
            await _context.Fees.Where(f => f.StudentId == studentId)
                .OrderByDescending(f => f.CreatedAt).ToListAsync();

        public async Task<Fee> CreateAsync(CreateFeeDto dto)
        {
            var fee = new Fee
            {
                StudentId = dto.StudentId,
                Amount = dto.Amount,
                Description = dto.Description,
                DueDate = dto.DueDate
            };
            _context.Fees.Add(fee);
            await _context.SaveChangesAsync();
            return fee;
        }

        public async Task<Fee?> UpdateAsync(int id, UpdateFeeDto dto)
        {
            var fee = await _context.Fees.FindAsync(id);
            if (fee == null) return null;

            if (dto.Amount.HasValue) fee.Amount = dto.Amount.Value;
            if (dto.Description != null) fee.Description = dto.Description;
            if (dto.Status != null) fee.Status = dto.Status;
            if (dto.DueDate.HasValue) fee.DueDate = dto.DueDate.Value;
            if (dto.PaidDate.HasValue) fee.PaidDate = dto.PaidDate.Value;

            await _context.SaveChangesAsync();
            return fee;
        }

        public async Task<Fee?> PayFeeAsync(int id, PayFeeDto dto)
        {
            var fee = await _context.Fees.FindAsync(id);
            if (fee == null) return null;

            fee.Status = "Paid";
            fee.PaidDate = dto.PaidDate;
            await _context.SaveChangesAsync();
            return fee;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var fee = await _context.Fees.FindAsync(id);
            if (fee == null) return false;
            _context.Fees.Remove(fee);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
